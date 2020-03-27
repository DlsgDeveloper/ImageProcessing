using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.PageObjects
{
	public class Picture : IComparable<Picture>, IPageObject
	{
		// Fields
		private int x;
		private int y;
		private int right;
		private int bottom;
		private DelimiterZone zone;
		private ObjectMap		objectMap;
		private ObjectShape		objectShape = null;
		private ConvexEnvelope	convexEnvelope = null;

		#region constructor
		public Picture(Symbol symbol)
		{
			this.zone = null;
			this.x = symbol.X;
			this.y = symbol.Y;
			this.right = symbol.Right;
			this.bottom = symbol.Bottom;

			this.objectMap = symbol.ObjectMap;

			ComputeShape();
		}

		public Picture(Rectangle r, ObjectMap objectMap)
		{
			this.zone = null;
			this.x = r.X;
			this.y = r.Y;
			this.right = r.Right;
			this.bottom = r.Bottom;

			this.objectMap = objectMap;

			ComputeShape();
		}
		#endregion

		#region class HorizontalComparer
		public class HorizontalComparer : IComparer<Picture>
		{
			// Methods
			public int Compare(Picture picture1, Picture picture2)
			{
				if (picture1.X > picture2.X)
					return 1;
				
				if (picture1.X < picture2.X)
					return -1;

				return 0;
			}
		}
		#endregion

		#region class VerticalComparer
		public class VerticalComparer : IComparer<Picture>
		{
			// Methods
			public int Compare(Picture picture1, Picture picture2)
			{
				if (picture1.Y > picture2.Y)
					return 1;

				if (picture1.Y < picture2.Y)
					return -1;

				return 0;
			}
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties
		public int			Bottom{get{return this.bottom;}}
		public int			Height{get{return (this.bottom - this.y);}}
		public Point		Location{get{return new Point(this.x, this.y);}}
		public Rectangle	Rectangle{get{return Rectangle.FromLTRB(this.x, this.y, this.right, this.bottom);}}
		public int			Right{get{return this.right;}}
		public int			Width{get{return (this.right - this.x);}}
		public int			X{get{return this.x;}}
		public int			Y{get{return this.y;}}
		public DelimiterZone Zone{get{return this.zone;} set{this.zone = value;}}

		public bool				TopCurveExists { get { return this.objectShape.IsTopBfValid; } }
		public bool				BottomCurveExists { get { return this.objectShape.IsBottomBfValid; } }
		public ObjectMap		ObjectMap { get { return this.objectMap; } }
		public ObjectShape		ObjectShape { get { return objectShape; } }
		public ConvexEnvelope	ConvexEnvelope{get{return convexEnvelope;}}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region AddSymbol()
		public void AddSymbol(Symbol symbol)
		{			
			if (this.x > symbol.X)
				this.x = symbol.X;
			if (this.y > symbol.Y)
				this.y = symbol.Y;
			if (this.right < symbol.Right)
				this.right = symbol.Right;
			if (this.bottom < symbol.Bottom)
				this.bottom = symbol.Bottom;

			this.objectMap.Merge(symbol.ObjectMap);
			ComputeShape();
		}
		#endregion 

		#region AddSymbolFast()
		/// <summary>
		/// It adds symbol, changing objectMap, but it doesn't compute shape and convex envelope
		/// </summary>
		/// <param name="symbol"></param>
		public void AddSymbolFast(Symbol symbol)
		{
			if (this.x > symbol.X)
				this.x = symbol.X;
			if (this.y > symbol.Y)
				this.y = symbol.Y;
			if (this.right < symbol.Right)
				this.right = symbol.Right;
			if (this.bottom < symbol.Bottom)
				this.bottom = symbol.Bottom;

			this.objectMap.Merge(symbol.ObjectMap);
		}
		#endregion 

		#region AddObjectMapFast()
		/// <summary>
		/// It adds objectMap, but it doesn't compute shape and convex envelope
		/// </summary>
		/// <param name="symbol"></param>
		public void AddObjectMapFast(ObjectMap objectMap)
		{
			if (this.x > objectMap.X)
				this.x = objectMap.X;
			if (this.y > objectMap.Y)
				this.y = objectMap.Y;
			if (this.right < objectMap.Rectangle.Right)
				this.right = objectMap.Rectangle.Right;
			if (this.bottom < objectMap.Rectangle.Bottom)
				this.bottom = objectMap.Rectangle.Bottom;

			this.objectMap.Merge(objectMap);
		}
		#endregion 
		
		#region CompareTo()
		public int CompareTo(Picture picture)
		{
			if (this.Y < picture.Y)
				return -1;

			if (this.Y == picture.Y)
				return 0;
			
			return 1;
		}
		#endregion 

		#region ComputeShape()
		public void ComputeShape()
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			this.objectShape = new ObjectShape(this.Location, this.objectMap);
#if DEBUG
			//Console.WriteLine("Picture, ComputeShape() ObjectShape: " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif
			
			this.convexEnvelope = new ConvexEnvelope(this.Location, this.objectMap);
#if DEBUG
			//Console.WriteLine("Picture, ComputeShape() ConvexEnvelope: " + DateTime.Now.Subtract(start).ToString() + "\n");
#endif

			this.convexEnvelope.Inflate(2);
		}
		#endregion

		#region GetSkew()
		public bool GetSkew(Size pageSize, out double angle, out double weight)
		{
			if (this.objectShape != null)
			{
				angle = objectShape.GetSkew(pageSize, out weight);
				return true;
			}
			else
				throw new IpException(ErrorCode.ErrorNoImageLoaded, "Picture, GetSkew(): objectShape is not loaded!");
		}
		#endregion

		#region Merge()
		public void Merge(Picture picture)
		{
			if (this.x > picture.X)
				this.x = picture.X;
			if (this.y > picture.Y)
				this.y = picture.Y;
			if (this.right < picture.Right)
				this.right = picture.Right;
			if (this.bottom < picture.Bottom)
				this.bottom = picture.Bottom;

#if DEBUG
			DateTime start = DateTime.Now;
#endif
			this.objectMap.Merge(picture.objectMap);

#if DEBUG
			Console.WriteLine("Picture, Merge() Merge: " + DateTime.Now.Subtract(start).ToString());
#endif

#if DEBUG
			start = DateTime.Now;
#endif

			ComputeShape();

#if DEBUG
			Console.WriteLine(" Picture, Merge() ComputeShape: " + DateTime.Now.Subtract(start).ToString());
#endif

		}
		#endregion

		#region Merge()
		public static Picture Merge(List<Picture> pictures)
		{
			int l = int.MaxValue, t = int.MaxValue, r = int.MinValue, b = int.MinValue;

			foreach (Picture p in pictures)
			{
				if (l > p.X)
					l = p.X;
				if (t > p.Y)
					t = p.Y;
				if (r < p.Right)
					r = p.Right;
				if (b < p.Bottom)
					b = p.Bottom;
			}
			
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			List<ObjectMap> objectMaps = new List<ObjectMap>();

			foreach (Picture p in pictures)
				objectMaps.Add(p.ObjectMap);

			ObjectMap objectMap = new ObjectMap(objectMaps);

#if DEBUG
			//Console.WriteLine("Picture, Merge() ObjectMap: " + DateTime.Now.Subtract(start).ToString());
#endif

#if DEBUG
			start = DateTime.Now;
#endif
			Picture picture = new Picture(Rectangle.FromLTRB(l, t, r, b), objectMap);

#if DEBUG
			//Console.WriteLine("Picture, Merge() constructor: " + DateTime.Now.Subtract(start).ToString());
#endif
			return picture;
		}
		#endregion

		#region GetTopBfPoints()
		public Point[] GetTopBfPoints()
		{
			return this.objectShape.GetTopBfPoints();
		}
		#endregion

		#region GetBottomBfPoints()
		public Point[] GetBottomBfPoints()
		{
			return this.objectShape.GetBottomBfPoints();
		}
		#endregion

		#region Contains()
		public bool Contains(Point p)
		{
			return Contains(p.X, p.Y);
		}

		public bool Contains(int x, int y)
		{
			return this.convexEnvelope.Contains(x, y);
		}

		public bool Contains(Rectangle rect)
		{
			return this.convexEnvelope.Contains(rect);
		}
		#endregion

		#region InterceptsWith()
		public bool InterceptsWith(Symbol symbol)
		{
			if (this.Contains(symbol.APixelX, symbol.APixelY))
				return true;

			if (Rectangle.Intersect(Rectangle.Inflate(this.Rectangle, 3, 3), symbol.Rectangle) != Rectangle.Empty)
				return this.convexEnvelope.InterceptsWith(symbol.ConvexEnvelope);

			return false;
		}

		public bool InterceptsWith(ConvexEnvelope convexEnvelope)
		{
			if (Rectangle.Intersect(Rectangle.Inflate(this.Rectangle, 3, 3), convexEnvelope.Rectangle) != Rectangle.Empty)
				return this.convexEnvelope.InterceptsWith(convexEnvelope);

			return false;
		}

		public bool InterceptsWith(Delimiter delimiter)
		{
			if (Rectangle.Intersect(Rectangle.Inflate(this.Rectangle, 3, 3), delimiter.Rectangle) != Rectangle.Empty)
				return this.convexEnvelope.InterceptsWith(delimiter.ObjectShape);

			return false;
		}
		#endregion

		#region DrawToImage()
		public void DrawToImage(Color color, BitmapData bmpData)
		{
#if SAVE_RESULTS
			try
			{
				this.objectMap.DrawToImage(color, bmpData);
			}
			catch { }
			finally
			{
				GC.Collect();
			}
#endif
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region DilateArray()
		/*private void DilateArray(int[,] array)
		{
			int x;
			int y;
			int width = array.GetLength(1);
			int height = array.GetLength(0);
			
			for (y = 1; y < (height - 1); y++)
				for (x = 1; x < (width - 1); x++ )
					if ((array[y, x] != -1) && ((array[y - 1, x - 1] == -1) || (array[y - 1, x] == -1) || (array[y - 1, x + 1] == -1) || (array[y, x - 1] == -1) || (array[y, x + 1] == -1) || (array[y + 1, x - 1] == -1) || (array[y + 1, x] == -1) || (array[y + 1, x + 1] == -1)))
						array[y, x] = -2;
			
			y = 0;
			for (x = 1; x < (width - 1); x++)
				if ((array[y, x] != -1) && ((array[y, x - 1] == -1) || (array[y, x + 1] == -1) || (array[y + 1, x - 1] == -1) || (array[y + 1, x] == -1) || (array[y + 1, x + 1] == -1)))
					array[y, x] = -2;
			
			y = height - 1;
			for (x = 1; x < (width - 1); x++)
				if ((array[y, x] != -1) && ((array[y - 1, x - 1] == -1) || (array[y - 1, x] == -1) || (array[y - 1, x + 1] == -1) || (array[y, x - 1] == -1) || (array[y, x + 1] == -1)))
					array[y, x] = -2;
			
			x = 0;
			for (y = 1; y < (height - 1); y++)
				if ((array[y, x] != -1) && ((array[y - 1, x] == -1) || (array[y - 1, x + 1] == -1) || (array[y, x + 1] == -1) || (array[y + 1, x] == -1) || (array[y + 1, x + 1] == -1)))
					array[y, x] = -2;
			
			x = width - 1;
			for (y = 1; y < (height - 1); y++)
				if ((array[y, x] != -1) && ((array[y - 1, x - 1] == -1) || (array[y - 1, x] == -1) || (array[y, x - 1] == -1) || (array[y + 1, x - 1] == -1) || (array[y + 1, x] == -1)))
					array[y, x] = -2;

			for (y = 1; y < (height - 1); y++)
				for (x = 1; x < (width - 1); x++)
					if (array[y, x] == -2)
						array[y, x] = -1;
		}*/
		#endregion

		#region GetCrop()
		private static Crop GetCrop(int[,] array)
		{
			int		x,y;
			int		width = array.GetLength(1);
			int		height = array.GetLength(0);
			Point	pL = new Point(int.MaxValue, int.MaxValue);
			Point	pT = new Point(int.MaxValue, int.MaxValue);
			Point	pR = new Point(int.MinValue, int.MinValue);
			Point	pB = new Point(int.MinValue, int.MinValue);
			
			x = 0;
			for (y = 0; y < height; y++)
			{
				if (array[y, x] == -1)
				{
					pL = new Point(x, y);
					break;
				}
			}
			
			y = 0;
			for (x = 0; x < width; x++)
			{
				if (array[y, x] == -1)
				{
					pT = new Point(x, y);
					break;
				}
			}
			
			x = width - 1;
			for (y = 0; y < height; y++)
			{
				if (array[y, x] == -1)
				{
					pR = new Point(x, y);
					break;
				}
			}
			
			y = height - 1;
			for (x = 0; x < width; x++)
			{
				if (array[y, x] == -1)
				{
					pB = new Point(x, y);
					break;
				}
			}

			return new Crop(pL, pT, pR, pB);
		}
		#endregion

		#region GetObjectMap()
		/*private ObjectMap GetObjectMap(BitmapData bitmapData)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			ObjectMap objectMap = new ObjectMap(this.Rectangle);

			foreach (Symbol symbol in this.Symbols)
			{
				ObjectMap symbolObjectMap = symbol.GetObjectMap(bitmapData);

				objectMap.Merge(symbolObjectMap);
			}

			objectMap.Dilate();

#if DEBUG
			System.Reflection.MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
			Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "(): " + DateTime.Now.Subtract(start).ToString());
#endif
			return objectMap;
		}*/
		#endregion

		#region GetObjectShape()
		/*private int[,] GetObjectShape(BitmapData bitmapData)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			int[,] array = new int[this.Height, this.Width];

			foreach (Symbol symbol in this.Symbols)
			{
				int[,] symbolArray = symbol.GetObjectArray(bitmapData);

				for (int y = (symbol.Y > this.Y) ? symbol.Y : this.Y; y < ((symbol.Bottom < this.Bottom) ? symbol.Bottom : this.Bottom); y++)
					for (int x = (symbol.X > this.X) ? symbol.X : this.X; x < ((symbol.Right < this.Right) ? symbol.Right : this.Right); x++)
						if (symbolArray[y - symbol.Y, x - symbol.X] == -1)
							array[y - this.Y, x - this.X] = -1;
			}

			this.DilateArray(array);
#if DEBUG
			System.Reflection.MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
			Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "(): " + DateTime.Now.Subtract(start).ToString());
#endif
			return array;
		}*/
		#endregion

		#endregion

}

}
