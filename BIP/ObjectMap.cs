using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing.PageObjects;

namespace ImageProcessing
{
	/// <summary>
	/// 
	/// </summary>
	public class ObjectMap
	{
		Rectangle	clip;
		byte[,]		array = null;
		List<SymbolShape> symbolShapes = new List<SymbolShape>();


		#region ObjectMap()
		/// <summary>
		/// First, it decides if new ObjectMap has to be created or existing one is big enough
		/// Then, it copies data into an array
		/// </summary>
		/// <param name="pictures"></param>
		public ObjectMap(List<ObjectMap> objectMaps)
		{
			int l = int.MaxValue, t = int.MaxValue, r = int.MinValue, b = int.MinValue;

			foreach (ObjectMap p in objectMaps)
			{
				if (l > p.X)
					l = p.X;
				if (t > p.Y)
					t = p.Y;
				if (r < p.Rectangle.Right)
					r = p.Rectangle.Right;
				if (b < p.Rectangle.Bottom)
					b = p.Rectangle.Bottom;
			}

			//if there is a picture with big enough object map to hold all pictures, use it
			foreach (ObjectMap p in objectMaps)
			{
				if (p.Rectangle.X == l && p.Rectangle.Y == t && p.Rectangle.Right == r && p.Rectangle.Bottom == b)
				{
					this.clip = p.clip;
					this.array = p.array;

					foreach (SymbolShape shape in p.symbolShapes)
						this.symbolShapes.Add(shape);

					foreach (ObjectMap q in objectMaps)
						if (q != p)
						{
							MergeArrays(this.array, q.array, q.Width, q.X - this.X, q.Y - this.Y);

							for (int i = q.symbolShapes.Count - 1; i >= 0; i-- )
							{
								this.symbolShapes.Add(q.symbolShapes[i]);
								q.symbolShapes.RemoveAt(i);
							}
						}

					return;
				}
			}

			this.clip = Rectangle.FromLTRB(l, t, r, b);
			this.array = new byte[this.clip.Height, (int)Math.Ceiling(this.clip.Width / 8.0)];

			foreach (ObjectMap p in objectMaps)
			{
				MergeArrays(this.array, p.array, p.Width, p.X - this.X, p.Y - this.Y);

				for (int i = p.symbolShapes.Count - 1; i >= 0; i--)
				{
					this.symbolShapes.Add(p.symbolShapes[i]);
					p.symbolShapes.RemoveAt(i);
				}
			}
		}

		public ObjectMap(Rectangle clip, byte[,] array)
		{
			this.clip = clip;
			this.array = array;

			this.symbolShapes.Add(new SymbolShape(this));
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties
		public byte[,]		Array { get { return array; } }
		public Rectangle	Rectangle { get { return clip; } }
		public Point		Location { get { return clip.Location; } }
		public Size			Size { get { return clip.Size; } }
		public int			X { get { return clip.X; } }
		public int			Y { get { return clip.Y; } }
		public int			Width { get { return clip.Width; } }
		public int			Height { get { return clip.Height; } }
		public int			Stride { get { return (int)Math.Ceiling(this.clip.Width / 8.0); } }
		public List<SymbolShape>	SymbolShapes { get { return this.symbolShapes; } }

		#region ObjectPixels
		public int ObjectPixels
		{
			get
			{
				int objectPixelsCount = 0;

				for (int y = 0; y < clip.Height; y++)
					for (int x = 0; x < clip.Width; x++)
						if (GetPoint(x, y))
							objectPixelsCount++;

				return objectPixelsCount;
			}
		}
		#endregion

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region GetPoint()
		public bool GetPoint(int x, int y)
		{
			return ((array[y, x / 8] & (0x80 >> (x & 0x07))) > 0);
		}

		public static bool GetPoint(byte[,] array, int x, int y)
		{
			return ((array[y, x / 8] & (0x80 >> (x & 0x07))) > 0);
		}
		#endregion

		#region Merge()
		public void Merge(ObjectMap objectMap)
		{
			Rectangle originalClip = clip;

			this.clip = Rectangle.Union(clip, objectMap.Rectangle);

			if (originalClip != clip)
			{
				byte[,] oldArray = this.array;
				this.array = new byte[this.clip.Height, (int)Math.Ceiling(this.clip.Width / 8.0)];

				MergeArrays(this.array, oldArray, originalClip.Width, originalClip.X - this.X, originalClip.Y - this.Y);
			}

			MergeArrays(this.array, objectMap.array, objectMap.Width, objectMap.X - this.X, objectMap.Y - this.Y);

			for (int i = objectMap.symbolShapes.Count - 1; i >= 0; i--)
			{
				this.symbolShapes.Add(objectMap.symbolShapes[i]);
				objectMap.symbolShapes.RemoveAt(i);
			}
		}
		#endregion
	
		#region DrawToImage()
		public void DrawToImage(Color color, BitmapData bmpData)
		{
			unsafe
			{
				byte* scan0 = (byte*)bmpData.Scan0.ToPointer();
				int stride = bmpData.Stride;
				int x, y;

				for (y = 0; y < clip.Height; y++)
					for (x = 0; x < clip.Width; x++)
						if (GetPoint(x, y))
						{
							*(scan0 + stride * (y + clip.Y) + (x + clip.X) * 3) = color.B;
							*(scan0 + stride * (y + clip.Y) + (x + clip.X) * 3 + 1) = color.G;
							*(scan0 + stride * (y + clip.Y) + (x + clip.X) * 3 + 2) = color.R;
						}
			}
		}
		#endregion

		#region DrawToBitArray()
		public void DrawToBitArray(byte[,] array, int width)
		{
			int x, y;

			for (y = clip.Y; y < clip.Bottom; y++)
				for (x = clip.X; x < clip.Right; x++)
					if (GetPoint(x - clip.X, y - clip.Y))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
					}
		}
		#endregion
	
		#region GetAnyObjectPixel()
		public Point? GetAnyObjectPixel()
		{
			for (int y = 0; y < clip.Height; y++)
				for (int x = 0; x < clip.Width; x++)
					if (GetPoint(x, y))
						return new Point(x + clip.X, y + clip.Y);

			return null;
		}
		#endregion

		#region GetObjectMap()
		public static ObjectMap GetObjectMap(BitmapData bmpData, Rectangle clip, Point objectPoint)
		{
			byte[,] array = RasterProcessing.GetObjectMap(bmpData, clip, objectPoint);

			return new ObjectMap(clip, array);
		}

		public static ObjectMap GetObjectMap(byte[,] bitArray, int width, Rectangle clip, Point objectPoint)
		{
			byte[,] array = RasterProcessing.GetObjectMap(bitArray, width, clip, objectPoint);

			return new ObjectMap(clip, array);
		}
		#endregion

		#region GetDistance()
		public int GetDistance(ObjectMap objectMap)
		{
			int smallestDistance = int.MaxValue;

			/*if(this.Rectangle.IntersectsWith(objectMap.Rectangle))
			{
				Rectangle intersect = Rectangle.Intersect(this.Rectangle, objectMap.Rectangle);
				int x, y;

				for(y = intersect.Y; y < intersect.Bottom; y++)
					for()
			}
			
			foreach (SymbolShape objectSymbolShape in objectMap.SymbolShapes)
				if (objectSymbolShape.EdgePoints.Count > 0)
					if (GetPoint(objectSymbolShape.EdgePoints[0].X, objectSymbolShape.EdgePoints[0].Y))
						return 0;*/
			
			
			foreach(SymbolShape symbolShape in this.SymbolShapes)
				foreach(SymbolShape objectSymbolShape in objectMap.SymbolShapes)
				{
					foreach(SymbolShapePoint p in symbolShape.EdgePoints)
						foreach(SymbolShapePoint pp in objectSymbolShape.EdgePoints)
						{
							int distance = Math.Min(Math.Abs(p.X - pp.X), Math.Abs(p.Y - pp.Y));

							if (distance == 0)
								return 0;
							else if (smallestDistance > distance)
								smallestDistance = distance;
						}
				}

			return smallestDistance;
		}
		#endregion
	
		#endregion


		//PRIVATE METHODS
		#region private methods

		#region SetPoint()
		private void SetPoint(int x, int y, bool isObjectPoint)
		{
			if (isObjectPoint)
				array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
			else
				array[y, x / 8] &= (byte)(0xFF7F >> (x & 0x07));
		}
		#endregion

		#region MergeArrays()
		public void MergeArrays(byte[,] masterMap, byte[,] mapToCopy, int mapToCopyWidth, int shiftX, int shiftY)
		{
			int width = mapToCopy.GetLength(1); ;
			int height = mapToCopy.GetLength(0);
			int shiftXByte = shiftX / 8;

			if (shiftX / 8.0 == 0)
			{
				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						masterMap[(y + shiftY), (x + shiftXByte)] |= mapToCopy[y, x];
					}
				}
			}
			else
			{
				int masterWidth = masterMap.GetLength(1);
				int shift1 = shiftX % 8;
				int shift2 = 8 - shift1;

				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width - 1; x++)
					{
						masterMap[(y + shiftY), (x + shiftXByte)] |= (byte)(mapToCopy[y, x] >> shift2);
						masterMap[(y + shiftY), (x + shiftXByte + 1)] |= (byte)(mapToCopy[y, x] << shift1);
					}

					//last column
					masterMap[(y + shiftY), ((width - 1) + shiftXByte)] |= (byte)(mapToCopy[y, (width - 1)] >> shift2);

					if (masterWidth > ((width - 1) + shiftXByte + 1))
						masterMap[(y + shiftY), ((width - 1) + shiftXByte + 1)] |= (byte)(mapToCopy[y, (width - 1)] << shift1);
				}
			}
		}
		#endregion

		#endregion

	}
}
