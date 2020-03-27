using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.PageObjects
{
	#region struct SymbolShapePoint
	public struct SymbolShapePoint 
	{
		public ushort X;
		public ushort Y;

		public SymbolShapePoint(int x, int y)
		{
			this.X = (ushort)x;
			this.Y = (ushort)y;
		}

		public SymbolShapePoint(ushort x, ushort y)
		{
			this.X = x;
			this.Y = y;
		}

		public void Shift(int dx, int dy)
		{
			this.X = (ushort)((this.X + dx < 0) ? 0 : ((this.X + dx > 65535) ? 65535 : this.X + dx));
			this.Y = (ushort)((this.Y + dy < 0) ? 0 : ((this.Y + dy > 65535) ? 65535 : this.Y + dy));
		}

		public override string ToString()
		{
			return "X = " + X.ToString() + ", Y = " + Y.ToString();
		}
	}
	#endregion

	#region class SymbolShapePoints
	public class SymbolShapePoints : List<SymbolShapePoint>
	{

		//PUBLIC METHODS
		#region public methods

		#region Contains()
		public bool Contains(int x, int y)
		{
			foreach (SymbolShapePoint p in this)
				if (p.X == x && p.Y == y)
					return true;

			return false;
		}
		#endregion

		#endregion
	}
	#endregion

	/// <summary>
	/// EdgePoint and EdgePoints are stored in absolute coordinates!
	/// </summary>
	public class SymbolShape
	{
		public readonly SymbolShapePoint	EdgePoint;
		public readonly SymbolShapePoints	EdgePoints = new SymbolShapePoints();

		#region enum EdgeAngle
		enum EdgeAngle
		{
			N, E, S, W, NE, SE, SW, NW
		}
		#endregion


		#region constructor
		internal SymbolShape(SymbolShapePoint edgePoint)
		{
		}

		internal SymbolShape(ObjectMap objectMap)
		{
			int width = objectMap.Width;
			int height = objectMap.Height;

			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
					if (objectMap.GetPoint(x, y))
					{
						EdgePoint = new SymbolShapePoint(x, y);
						y = height;
						break;
					}

			EdgeAngle			angle = EdgeAngle.E;
			SymbolShapePoint	currentPoint = EdgePoint;
			SymbolShapePoint?	newPoint = null;

			EdgePoints.Add(EdgePoint);

			do
			{
				switch (angle)
				{
					case EdgeAngle.E:
						{
							if (currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y - 1);
								angle = EdgeAngle.N;
							}
							else if (currentPoint.X + 1 < width && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y - 1);
								angle = EdgeAngle.NE;
							}
							else if (currentPoint.X + 1 < width && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y);
								angle = EdgeAngle.E;
							}
							else if (currentPoint.X + 1 < width && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y + 1);
								angle = EdgeAngle.SE;
							}
							else if (currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y + 1);
								angle = EdgeAngle.S;
							}
							else if (currentPoint.X - 1 >= 0 && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y + 1);
								angle = EdgeAngle.SW;
							}
							else if (currentPoint.X - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y);
								angle = EdgeAngle.W;
							}
						} break;
					case EdgeAngle.SE:
						{
							if (currentPoint.X + 1 < width && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y - 1);
								angle = EdgeAngle.NE;
							}
							else if (currentPoint.X + 1 < width && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y);
								angle = EdgeAngle.E;
							}
							else if (currentPoint.X + 1 < width && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y + 1);
								angle = EdgeAngle.SE;
							}
							else if (currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y + 1);
								angle = EdgeAngle.S;
							}
							else if (currentPoint.X - 1 >= 0 && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y + 1);
								angle = EdgeAngle.SW;
							}
							else if (currentPoint.X - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y);
								angle = EdgeAngle.W;
							}
							else if (currentPoint.X - 1 >= 0 && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y - 1);
								angle = EdgeAngle.NW;
							}
						} break;
					case EdgeAngle.S:
						{
							if (currentPoint.X + 1 < width && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y);
								angle = EdgeAngle.E;
							}
							else if (currentPoint.X + 1 < width && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y + 1);
								angle = EdgeAngle.SE;
							}
							else if (currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y + 1);
								angle = EdgeAngle.S;
							}
							else if (currentPoint.X - 1 >= 0 && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y + 1);
								angle = EdgeAngle.SW;
							}
							else if (currentPoint.X - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y);
								angle = EdgeAngle.W;
							}
							else if (currentPoint.X - 1 >= 0 && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y - 1);
								angle = EdgeAngle.NW;
							}
							else if (currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y - 1);
								angle = EdgeAngle.N;
							}
						} break;
					case EdgeAngle.SW:
						{
							if (currentPoint.X + 1 < width && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y + 1);
								angle = EdgeAngle.SE;
							}
							else if (currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y + 1);
								angle = EdgeAngle.S;
							}
							else if (currentPoint.X - 1 >= 0 && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y + 1);
								angle = EdgeAngle.SW;
							}
							else if (currentPoint.X - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y);
								angle = EdgeAngle.W;
							}
							else if (currentPoint.X - 1 >= 0 && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y - 1);
								angle = EdgeAngle.NW;
							}
							else if (currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y - 1);
								angle = EdgeAngle.N;
							}
							else if (currentPoint.X + 1 < width && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y - 1);
								angle = EdgeAngle.NE;
							}
						} break;
					case EdgeAngle.W:
						{
							if (currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y + 1);
								angle = EdgeAngle.S;
							}
							else if (currentPoint.X - 1 >= 0 && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y + 1);
								angle = EdgeAngle.SW;
							}
							else if (currentPoint.X - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y);
								angle = EdgeAngle.W;
							}
							else if (currentPoint.X - 1 >= 0 && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y - 1);
								angle = EdgeAngle.NW;
							}
							else if (currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y - 1);
								angle = EdgeAngle.N;
							}
							else if (currentPoint.X + 1 < width && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y - 1);
								angle = EdgeAngle.NE;
							}
							else if (currentPoint.X + 1 < width && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y);
								angle = EdgeAngle.E;
							}
						} break;
					case EdgeAngle.NW:
						{
							if (currentPoint.X - 1 >= 0 && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y + 1);
								angle = EdgeAngle.SW;
							}
							else if (currentPoint.X - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y);
								angle = EdgeAngle.W;
							}
							else if (currentPoint.X - 1 >= 0 && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y - 1);
								angle = EdgeAngle.NW;
							}
							else if (currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y - 1);
								angle = EdgeAngle.N;
							}
							else if (currentPoint.X + 1 < width && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y - 1);
								angle = EdgeAngle.NE;
							}
							else if (currentPoint.X + 1 < width && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y);
								angle = EdgeAngle.E;
							}
							else if (currentPoint.X + 1 < width && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y + 1);
								angle = EdgeAngle.SE;
							}
						} break;
					case EdgeAngle.N:
						{
							if (currentPoint.X - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y);
								angle = EdgeAngle.W;
							}
							else if (currentPoint.X - 1 >= 0 && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y - 1);
								angle = EdgeAngle.NW;
							}
							else if (currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y - 1);
								angle = EdgeAngle.N;
							}
							else if (currentPoint.X + 1 < width && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y - 1);
								angle = EdgeAngle.NE;
							}
							else if (currentPoint.X + 1 < width && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y);
								angle = EdgeAngle.E;
							}
							else if (currentPoint.X + 1 < width && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y + 1);
								angle = EdgeAngle.SE;
							}
							else if (currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y + 1);
								angle = EdgeAngle.S;
							}
						} break;
					case EdgeAngle.NE:
						{
							if (currentPoint.X - 1 >= 0 && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y - 1);
								angle = EdgeAngle.NW;
							}
							else if (currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y - 1);
								angle = EdgeAngle.N;
							}
							else if (currentPoint.X + 1 < width && currentPoint.Y - 1 >= 0 && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y - 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y - 1);
								angle = EdgeAngle.NE;
							}
							else if (currentPoint.X + 1 < width && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y);
								angle = EdgeAngle.E;
							}
							else if (currentPoint.X + 1 < width && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X + 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X + 1, currentPoint.Y + 1);
								angle = EdgeAngle.SE;
							}
							else if (currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X, currentPoint.Y + 1);
								angle = EdgeAngle.S;
							}
							else if (currentPoint.X - 1 >= 0 && currentPoint.Y + 1 < height && objectMap.GetPoint(currentPoint.X - 1, currentPoint.Y + 1))
							{
								newPoint = new SymbolShapePoint(currentPoint.X - 1, currentPoint.Y + 1);
								angle = EdgeAngle.SW;
							}
						} break;
				}

				if (newPoint.HasValue == false)
					break;
				else if(newPoint.Value.X == EdgePoint.X && newPoint.Value.Y == EdgePoint.Y)
				{
					if (EdgePoint.X > 0 && EdgePoint.Y + 1 < height && objectMap.GetPoint(EdgePoint.X - 1, EdgePoint.Y + 1))
					{
						if (EdgePoints.Contains(EdgePoint.X - 1, EdgePoint.Y + 1))
							break;
						else
						{
							newPoint = new SymbolShapePoint(EdgePoint.X - 1, EdgePoint.Y + 1);
							EdgePoints.Add(newPoint.Value);
							currentPoint = newPoint.Value;
							angle = EdgeAngle.SW;
						}
					}
					else
						break;
				}
				else
				{
					EdgePoints.Add(newPoint.Value);
					currentPoint = newPoint.Value;
				}
			} while (true);

			EdgePoint.Shift(objectMap.Rectangle.X, objectMap.Rectangle.Y);

			for (int i = 0; i < this.EdgePoints.Count; i++)
			{
				this.EdgePoints[i] = new SymbolShapePoint(this.EdgePoints[i].X + objectMap.Rectangle.X, this.EdgePoints[i].Y + objectMap.Rectangle.Y);
			}
		}
		#endregion 


		//PUBLIC METHODS
		#region public methods

		#region DrawToImage()
		public void DrawToImage(Color color, BitmapData bmpData)
		{
			unsafe
			{
				byte* scan0 = (byte*)bmpData.Scan0.ToPointer();
				int stride = bmpData.Stride;

				foreach (SymbolShapePoint p in EdgePoints)
				{
					*(scan0 + stride * p.Y + p.X * 3) = color.B;
					*(scan0 + stride * p.Y + p.X * 3 + 1) = color.G;
					*(scan0 + stride * p.Y + p.X * 3 + 2) = color.R;
				}
			}
		}
		#endregion

		#region GetShape()
		public static SymbolShape GetShape(byte[,] array, int width)
		{
			int height = array.GetLength(0);
			int objectX = 0, objectY = 0;

			if(GetEdgePoint(array, width, ref objectX, ref objectY) == false)
				return null;

			SymbolShapePoint edgePoint = new SymbolShapePoint(objectX, objectY);
			SymbolShape shape = new SymbolShape(edgePoint);

			EdgeAngle angle = EdgeAngle.E;
			int x = objectX;
			int y = objectY;
			SymbolShapePoint? newPoint = null;

			shape.EdgePoints.Add(edgePoint);

			do
			{
				switch (angle)
				{
					case EdgeAngle.E:
						{
							if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y - 1);
								angle = EdgeAngle.N;
							}
							else if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
							else if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y);
								angle = EdgeAngle.E;
							}
							else if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
							else if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y + 1);
								angle = EdgeAngle.S;
							}
							else if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
							else if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y);
								angle = EdgeAngle.W;
							}
						} break;
					case EdgeAngle.SE:
						{
							if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
							else if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y);
								angle = EdgeAngle.E;
							}
							else if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
							else if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y + 1);
								angle = EdgeAngle.S;
							}
							else if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
							else if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y);
								angle = EdgeAngle.W;
							}
							else if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
						} break;
					case EdgeAngle.S:
						{
							if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y);
								angle = EdgeAngle.E;
							}
							else if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
							else if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y + 1);
								angle = EdgeAngle.S;
							}
							else if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
							else if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y);
								angle = EdgeAngle.W;
							}
							else if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
							else if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y - 1);
								angle = EdgeAngle.N;
							}
						} break;
					case EdgeAngle.SW:
						{
							if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
							else if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y + 1);
								angle = EdgeAngle.S;
							}
							else if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
							else if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y);
								angle = EdgeAngle.W;
							}
							else if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
							else if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y - 1);
								angle = EdgeAngle.N;
							}
							else if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
						} break;
					case EdgeAngle.W:
						{
							if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y + 1);
								angle = EdgeAngle.S;
							}
							else if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
							else if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y);
								angle = EdgeAngle.W;
							}
							else if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
							else if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y - 1);
								angle = EdgeAngle.N;
							}
							else if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
							else if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y);
								angle = EdgeAngle.E;
							}
						} break;
					case EdgeAngle.NW:
						{
							if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
							else if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y);
								angle = EdgeAngle.W;
							}
							else if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
							else if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y - 1);
								angle = EdgeAngle.N;
							}
							else if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
							else if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y);
								angle = EdgeAngle.E;
							}
							else if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
						} break;
					case EdgeAngle.N:
						{
							if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y);
								angle = EdgeAngle.W;
							}
							else if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
							else if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y - 1);
								angle = EdgeAngle.N;
							}
							else if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
							else if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y);
								angle = EdgeAngle.E;
							}
							else if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
							else if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y + 1);
								angle = EdgeAngle.S;
							}
						} break;
					case EdgeAngle.NE:
						{
							if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
							else if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y - 1);
								angle = EdgeAngle.N;
							}
							else if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
							else if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y);
								angle = EdgeAngle.E;
							}
							else if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
							else if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x, y + 1);
								angle = EdgeAngle.S;
							}
							else if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new SymbolShapePoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
						} break;
				}

				if (newPoint.HasValue == false)
					break;
				else if (newPoint.Value.X == edgePoint.X && newPoint.Value.Y == edgePoint.Y)
				{
					if ((edgePoint.X > 0) && (edgePoint.Y < height - 1) && ((array[edgePoint.Y + 1, (edgePoint.X - 1) / 8] & (byte)(0x80 >> ((edgePoint.X - 1) & 0x07))) > 0))
					{
						if (shape.EdgePoints.Contains(edgePoint.X - 1, edgePoint.Y + 1))
							break;
						else
						{
							newPoint = new SymbolShapePoint(edgePoint.X - 1, edgePoint.Y + 1);
							shape.EdgePoints.Add(newPoint.Value);
							x = newPoint.Value.X;
							y = newPoint.Value.Y;
							angle = EdgeAngle.SW;
						}
					}
					else
						break;
				}
				else
				{
					shape.EdgePoints.Add(newPoint.Value);
					x = newPoint.Value.X;
					y = newPoint.Value.Y;
				}
			} while (true);

			return shape;
		}
		#endregion 

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region GetEdgePoint()
		private static bool GetEdgePoint(byte[,] array, int width, ref int objectX, ref int objectY)
		{
			int x, y;
			int height = array.GetLength(0);

			for (y = 0; y < height; y++)
			{
				for (x = 0; x < width; x++)
				{
					if (array[y, x / 8] == 0)
						x += 7;
					else
					{
						for (int i = x; i < x + 8; i++)
							if ((array[y, i / 8] & (byte)(0x80 >> (i & 0x07))) > 0)
							{
								objectX = i;
								objectY = y;
								return true;
							}
					}
				}
			}

			return false;
		}
		#endregion
	
		#endregion


	}
}
