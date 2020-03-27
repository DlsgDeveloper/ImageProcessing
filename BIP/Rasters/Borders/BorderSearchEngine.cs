using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing.Rasters.Borders
{
	public class BorderSearchEngine
	{
		#region enum EdgeAngle
		enum EdgeAngle
		{
			N, E, S, W, NE, SE, SW, NW
		}
		#endregion


		//PUBLIC METHODS
		#region public methods

		#region GetBorder()
		/// <summary>
		/// For single object withing array. The object doesn't have to touch the edges.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public static ObjectBorder GetBorder(byte[,] array, int width)
		{
			int height = array.GetLength(0);
			int objectX = 0, objectY = 0;

			if(GetEdgePoint(array, width, ref objectX, ref objectY) == false)
				return null;

			BorderPoint edgePoint = new BorderPoint(objectX, objectY);
			ObjectBorder shape = new ObjectBorder(edgePoint);

			EdgeAngle angle = EdgeAngle.E;
			int x = objectX;
			int y = objectY;
			BorderPoint newPoint = null;

			shape.BorderPoints.Add(edgePoint);

			do
			{
				switch (angle)
				{
					case EdgeAngle.E:
						{
							if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y - 1);
								angle = EdgeAngle.N;
							}
							else if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
							else if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y);
								angle = EdgeAngle.E;
							}
							else if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
							else if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y + 1);
								angle = EdgeAngle.S;
							}
							else if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
							else if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y);
								angle = EdgeAngle.W;
							}
						} break;
					case EdgeAngle.SE:
						{
							if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
							else if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y);
								angle = EdgeAngle.E;
							}
							else if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
							else if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y + 1);
								angle = EdgeAngle.S;
							}
							else if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
							else if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y);
								angle = EdgeAngle.W;
							}
							else if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
						} break;
					case EdgeAngle.S:
						{
							if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y);
								angle = EdgeAngle.E;
							}
							else if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
							else if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y + 1);
								angle = EdgeAngle.S;
							}
							else if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
							else if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y);
								angle = EdgeAngle.W;
							}
							else if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
							else if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y - 1);
								angle = EdgeAngle.N;
							}
						} break;
					case EdgeAngle.SW:
						{
							if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
							else if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y + 1);
								angle = EdgeAngle.S;
							}
							else if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
							else if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y);
								angle = EdgeAngle.W;
							}
							else if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
							else if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y - 1);
								angle = EdgeAngle.N;
							}
							else if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
						} break;
					case EdgeAngle.W:
						{
							if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y + 1);
								angle = EdgeAngle.S;
							}
							else if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
							else if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y);
								angle = EdgeAngle.W;
							}
							else if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
							else if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y - 1);
								angle = EdgeAngle.N;
							}
							else if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
							else if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y);
								angle = EdgeAngle.E;
							}
						} break;
					case EdgeAngle.NW:
						{
							if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
							else if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y);
								angle = EdgeAngle.W;
							}
							else if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
							else if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y - 1);
								angle = EdgeAngle.N;
							}
							else if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
							else if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y);
								angle = EdgeAngle.E;
							}
							else if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
						} break;
					case EdgeAngle.N:
						{
							if (x - 1 >= 0 && ((array[y, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y);
								angle = EdgeAngle.W;
							}
							else if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
							else if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y - 1);
								angle = EdgeAngle.N;
							}
							else if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
							else if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y);
								angle = EdgeAngle.E;
							}
							else if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
							else if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y + 1);
								angle = EdgeAngle.S;
							}
						} break;
					case EdgeAngle.NE:
						{
							if (x - 1 >= 0 && y - 1 >= 0 && ((array[y - 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y - 1);
								angle = EdgeAngle.NW;
							}
							else if (y - 1 >= 0 && ((array[y - 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y - 1);
								angle = EdgeAngle.N;
							}
							else if (x + 1 < width && y - 1 >= 0 && ((array[y - 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y - 1);
								angle = EdgeAngle.NE;
							}
							else if (x + 1 < width && ((array[y, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y);
								angle = EdgeAngle.E;
							}
							else if (x + 1 < width && y + 1 < height && ((array[y + 1, (x + 1) / 8] & (byte)(0x80 >> ((x + 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x + 1, y + 1);
								angle = EdgeAngle.SE;
							}
							else if (y + 1 < height && ((array[y + 1, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x, y + 1);
								angle = EdgeAngle.S;
							}
							else if (x - 1 >= 0 && y + 1 < height && ((array[y + 1, (x - 1) / 8] & (byte)(0x80 >> ((x - 1) & 0x07))) > 0))
							{
								newPoint = new BorderPoint(x - 1, y + 1);
								angle = EdgeAngle.SW;
							}
						} break;
				}

				if (newPoint == null)
					break;
				else if (newPoint.X == edgePoint.X && newPoint.Y == edgePoint.Y)
				{
					if ((edgePoint.X > 0) && (edgePoint.Y < height - 1) && ((array[edgePoint.Y + 1, (edgePoint.X - 1) / 8] & (byte)(0x80 >> ((edgePoint.X - 1) & 0x07))) > 0))
					{
						if (shape.BorderPoints.Contains(edgePoint.X - 1, edgePoint.Y + 1))
							break;
						else
						{
							newPoint = new BorderPoint(edgePoint.X - 1, edgePoint.Y + 1);
							shape.BorderPoints.Add(newPoint);
							x = newPoint.X;
							y = newPoint.Y;
							angle = EdgeAngle.SW;
						}
					}
					else
						break;
				}
				else
				{
					shape.BorderPoints.Add(newPoint);
					x = newPoint.X;
					y = newPoint.Y;
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
