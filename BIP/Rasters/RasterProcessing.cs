using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	public class RasterProcessing
	{
		#region class Pair
		internal class Pair
		{
			public readonly int Key;
			public readonly List<int> List = new List<int>();

			public Pair(int key, int value)
			{
				this.Key = key;
				this.List.Add(value);
			}

			public void AddValue(int value)
			{
				if (!this.List.Contains(value))
					this.List.Add(value);
			}

			public void AddValues(List<int> values)
			{
				foreach (int value in values)
					if (!this.List.Contains(value))
						this.List.Add(value);
			}
		}
		#endregion

		#region class Pairs
		internal class Pairs : List<RasterProcessing.Pair>
		{
			public void Add(int key, int value)
			{
				foreach (RasterProcessing.Pair pair in this)
					if (pair.Key == key)
					{
						pair.AddValue(value);
						return;
					}

				foreach (RasterProcessing.Pair pair in this)
					if (pair.Key == value)
					{
						pair.AddValue(key);
						return;
					}

				base.Add(new RasterProcessing.Pair(key, value));
			}

			public void Compact()
			{
				int i;
				int j;

				for (i = base.Count - 2; i >= 0; i--)
				{
					j = base.Count - 1;
					while (j > i)
					{
						if (base[i].Key == base[j].Key)
						{
							base[i].AddValues(base[j].List);
							base.RemoveAt(j);
						}
						j--;
					}
				}

				for (i = base.Count - 1; i >= 0; i--)
				{
					for (j = base.Count - 1; j >= 0; j--)
					{
						if ((i != j) && base[i].List.Contains(base[j].Key))
						{
							base[i].AddValue(base[j].Key);
							base[i].AddValues(base[j].List);
							base.RemoveAt(j);
							if (j < i)
							{
								break;
							}
						}
					}
				}

				for (i = base.Count - 2; i >= 0; i--)
				{
					for (j = base.Count - 1; j > i; j--)
					{
						for (int k = 0; k < base[j].List.Count; k++)
						{
							if (base[i].List.Contains(base[j].List[k]))
							{
								base[i].AddValue(base[j].Key);
								base[i].AddValues(base[j].List);
								base.RemoveAt(j);
								break;
							}
						}
					}
				}
			}

			public SortedList<int, int> GetSortedList()
			{
				SortedList<int, int> sortedList = new SortedList<int, int>();

				foreach (RasterProcessing.Pair pair in this)
					foreach (int value in pair.List)
						sortedList.Add(value, pair.Key);

				return sortedList;
			}
		}
		#endregion

		#region struct ObjectPoint
		struct ObjectPoint
		{
			public ushort X;
			public ushort Y;

			public ObjectPoint(int x, int y)
			{
				this.X = (ushort)x;
				this.Y = (ushort)y;
			}
		}
		#endregion


		// PUBLIC METHODS
		#region public methods

		#region FindBackgroundFromLeft()
		public static unsafe int FindBackgroundFromLeft(byte* pOrig, int stride, int y, int xFrom, int xTo, int blockWidth, int blockHeight, float percentageWhite)
		{
			int jump = (blockWidth / 3 < 1) ? 1 : blockWidth / 3;

			for (int x = xFrom; x < (xTo - blockWidth); x += jump)
				if (RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight) <= percentageWhite)
					return x;

			return int.MaxValue;
		}
		#endregion

		#region FindBackgroundFromRight()
		public static unsafe int FindBackgroundFromRight(byte* pOrig, int stride, int y, int xFrom, int xTo, int blockWidth, int blockHeight, float percentageWhite)
		{
			int jump = (blockWidth / 3 < 1) ? 1 : blockWidth / 3;

			for (int x = (xTo - blockWidth); x > xFrom; x -= jump)
				if (RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight) <= percentageWhite)
					return (x + blockWidth);

			return int.MinValue;
		}
		#endregion

		#region FindBackgroundFromTop()
		public static unsafe int FindBackgroundFromTop(byte* pOrig, int stride, int x, int yFrom, int yTo, int blockWidth, int blockHeight, float percentageWhite)
		{
			int jump = (blockHeight / 3 < 1) ? 1 : blockHeight / 3;

			for (int y = yFrom; y < (yTo - blockHeight); y += jump)
				if (RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight) <= percentageWhite)
					return y;

			return int.MaxValue;
		}
		#endregion

		#region FindBackgroundFromBottom()
		public static unsafe int FindBackgroundFromBottom(byte* pOrig, int stride, int x, int yFrom, int yTo, int blockWidth, int blockHeight, float percentageWhite)
		{
			int jump = (blockHeight / 3 < 1) ? 1 : blockHeight / 3;

			for (int y = (yTo - blockHeight); y > yFrom; y -= jump)
				if (RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight) <= percentageWhite)
					return (y + blockHeight);

			return int.MinValue;
		}
		#endregion

		#region FindContentFromLeft()
		public static unsafe int FindContentFromLeft(byte* pOrig, int stride, int y, int xFrom, int xTo,
			int blockWidth, int blockHeight, float percentageWhite)
		{
			int jump = (blockWidth / 3 < 1) ? 1 : blockWidth / 3;

			for (int x = xFrom; x < (xTo - blockWidth); x += jump)
				if (RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight) > percentageWhite)
					return x;

			return int.MaxValue;
		}
		#endregion

		#region FindContentFromRight()
		public static unsafe int FindContentFromRight(byte* pOrig, int stride, int y, int xFrom, int xTo, int blockWidth, int blockHeight, float percentageWhite)
		{
			int jump = (blockWidth / 3 < 1) ? 1 : blockWidth / 3;

			for (int x = (xTo - blockWidth); x > xFrom; x -= jump)
				if (RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight) > percentageWhite)
					return (x + blockWidth);

			return int.MinValue;
		}
		#endregion

		#region FindContentFromTop()
		public static unsafe int FindContentFromTop(byte* pOrig, int stride, int x, int yFrom, int yTo, int blockWidth, int blockHeight, float percentageWhite)
		{
			int jump = (blockHeight / 3 < 1) ? 1 : blockHeight / 3;

			for (int y = yFrom; y < (yTo - blockHeight); y += jump)
				if (RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight) > percentageWhite)
					return y;

			return int.MaxValue;
		}
		#endregion

		#region FindContentFromBottom()
		public static unsafe int FindContentFromBottom(byte* pOrig, int stride, int x, int yFrom, int yTo, int blockWidth, int blockHeight, float percentageWhite)
		{
			int jump = (blockHeight / 3 < 1) ? 1 : blockHeight / 3;

			for (int y = (yTo - blockHeight); y > yFrom; y -= jump)
				if (RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight) > percentageWhite)
					return (y + blockHeight);

			return int.MinValue;
		}
		#endregion

		#region FindObjectInClip()
		/*public static unsafe int[,] FindObjectInClip(BitmapData bmpData, Rectangle clip, Point objectPoint)
		{
			int[,] array = new int[clip.Height, clip.Width];
			int x, y;

			int stride = bmpData.Stride;
			int width = clip.Width;
			int height = clip.Height;
			byte* pSource = (byte*)bmpData.Scan0.ToPointer();

			for (y = 0; y < height; y++)
				for (x = 0; x < width; x++)
					array[y, x] = ((*(pSource + (y + clip.Y) * stride + (x + clip.X) / 8) & (0x80 >> ((x + clip.X) & 0x07))) > 0) ? 1 : 0;

			objectPoint.Offset(-clip.X, -clip.Y);

			if (array[objectPoint.Y, objectPoint.X] != 0)
				WorkOutPoints(array, width, height, objectPoint);

			return array;
		}*/
		#endregion

		#region GetObjectMap()
		public static unsafe byte[,] GetObjectMap(BitmapData bmpData, Rectangle clip, Point objectPoint)
		{
			byte[,] array = new byte[clip.Height, ((clip.Width & 0x07) == 0) ? clip.Width / 8 : clip.Width / 8 + 1];

			if(clip.Width * clip.Height < 1000000)
				WorkOutPointsSmallObjects((byte*)bmpData.Scan0.ToPointer(), bmpData.Stride, array, clip, objectPoint);
			else
				WorkOutPointsLargeObjects((byte*)bmpData.Scan0.ToPointer(), bmpData.Stride, array, clip, objectPoint);

			return array;
		}

		public static unsafe byte[,] GetObjectMap(byte* scan0, int stride, Rectangle clip, Point objectPoint)
		{
			byte[,] array = new byte[clip.Height, ((clip.Width & 0x07) == 0) ? clip.Width / 8 : clip.Width / 8 + 1];

			if (clip.Width * clip.Height < 1000000)
				WorkOutPointsSmallObjects(scan0, stride, array, clip, objectPoint);
			else
				WorkOutPointsLargeObjects(scan0, stride, array, clip, objectPoint);

			return array;
		}

		public static unsafe byte[,] GetObjectMap(byte[,] imageBitArray, int width, Rectangle clip, Point objectPoint)
		{
			byte[,] array = new byte[clip.Height, ((clip.Width & 0x07) == 0) ? clip.Width / 8 : clip.Width / 8 + 1];

			if (clip.Width * clip.Height < 1000000)
				WorkOutPointsSmallObjects(imageBitArray, width, array, clip, objectPoint);
			else
				WorkOutPointsLargeObjects(imageBitArray, width, array, clip, objectPoint);

			return array;
		}
		#endregion

		#region PercentageWhite()
		public unsafe static double PercentageWhite(byte* pOrig, int stride, int clipX, int clipY, int clipW, int clipH)
		{
			int clipRight = (int)(clipX + clipW);
			int clipBottom = (int)(clipY + clipH);
			int x, y, i;
			int whitePoints = 0;
			int xMin, xMax;

			unsafe
			{
				byte* pCurrent;

				for (y = clipY; y < clipBottom; y++)
				{
					pCurrent = pOrig + y * stride + clipX / 8;
					xMin = clipX % 8;
					xMax = ((clipX / 8) != (clipRight / 8)) ? 8 : clipRight % 8;

					x = clipX / 8;

					//first byte
					if (*pCurrent > 0)
					{
						for (i = xMin; i < xMax; i++)
						{
							if ((*pCurrent & (0x80 >> i)) > 0)
								whitePoints++;
						}
					}

					pCurrent++;

					for (x = (clipX + 8) / 8; x < clipRight / 8; x++)
					{
						if (*pCurrent > 0)
						{
							for (i = 0; i < 8; i++)
								if ((*pCurrent & (0x80 >> i)) > 0)
									whitePoints++;
						}

						pCurrent++;
					}

					x = clipRight / 8;
					xMin = 0;
					xMax = clipRight % 8;

					//last byte
					if (*pCurrent > 0)
					{
						for (i = xMin; i < xMax; i++)
							if ((*pCurrent & (0x80 >> i)) > 0)
								whitePoints++;
					}
				}
			}

			return whitePoints / (double)(clipW * clipH);
		}
		#endregion

		#region RemovePointsWhereDistanceBiggerThanX()
		/*public static int RemovePointsWhereDistanceBiggerThanX(ref ArrayList sweepPoints, int distance, Rectangle imageRect)
		{
			int pointsRemoved = 0;

			if (sweepPoints.Count > 0)
			{
				long center = RasterProcessing.GetCenterX(sweepPoints, imageRect);

				for (int i = sweepPoints.Count - 1; i >= 0; i--)
					if ((((Point)sweepPoints[i]).X - center > distance) || (((Point)sweepPoints[i]).X - center < -distance))
					{
						sweepPoints.RemoveAt(i);
						pointsRemoved++;
					}
			}

			return pointsRemoved;
		}*/
		#endregion

		#region RemovePointsWhereDistanceBiggerThanY()
		/*public static int RemovePointsWhereDistanceBiggerThanY(ref ArrayList sweepPoints, int distance, Rectangle imageRect)
		{
			int pointsRemoved = 0;

			if (sweepPoints.Count > 0)
			{
				long center = RasterProcessing.GetCenterY(sweepPoints, imageRect);

				for (int i = sweepPoints.Count - 1; i >= 0; i--)
					if ((((Point)sweepPoints[i]).Y - center > distance) || (((Point)sweepPoints[i]).Y - center < -distance))
					{
						sweepPoints.RemoveAt(i);
						pointsRemoved++;
					}
			}

			return pointsRemoved;
		}*/
		#endregion

		#region RemoveWorstSweepPointsLeft()
		/*public static void RemoveWorstSweepPointsLeft(ref ArrayList sweepPoints, float percentsToRemove)
		{
			if (sweepPoints.Count > Convert.ToInt32(sweepPoints.Count * percentsToRemove))
			{
				int shouldRemove = Convert.ToInt32(sweepPoints.Count * percentsToRemove);

				if (shouldRemove == 0)
					return;

				ArrayList distancesList = new ArrayList();

				foreach (Point sweepPoint in sweepPoints)
					distancesList.Add((int)Math.Abs(sweepPoint.X));

				distancesList.Sort();
				int breakDistance = (int)distancesList[shouldRemove - 1];

				for (int i = sweepPoints.Count - 1; i >= 0; i--)
					if (((Point)sweepPoints[i]).X < breakDistance)
					{
						sweepPoints.RemoveAt(i);
						shouldRemove--;
					}
				for (int i = sweepPoints.Count - 1; i >= 0 && shouldRemove > 0; i--)
					if (((Point)sweepPoints[i]).X == breakDistance)
					{
						sweepPoints.RemoveAt(i);
						shouldRemove--;
					}
			}
		}*/
		#endregion

		#region RemoveWorstSweepPointsRight()
		/*public static void RemoveWorstSweepPointsRight(ref ArrayList sweepPoints, float percentsToRemove)
		{
			if (sweepPoints.Count > Convert.ToInt32(sweepPoints.Count * (1 - percentsToRemove)))
			{
				int shouldRemove = Convert.ToInt32(sweepPoints.Count * percentsToRemove);

				if (shouldRemove == 0)
					return;

				ArrayList distancesList = new ArrayList();

				foreach (Point sweepPoint in sweepPoints)
					distancesList.Add((int)Math.Abs(sweepPoint.X));

				distancesList.Sort();
				int breakDistance = (int)distancesList[sweepPoints.Count - shouldRemove - 1];

				for (int i = sweepPoints.Count - 1; i >= 0; i--)
					if (((Point)sweepPoints[i]).X > breakDistance)
					{
						sweepPoints.RemoveAt(i);
						shouldRemove--;
					}

				for (int i = sweepPoints.Count - 1; i >= 0 && shouldRemove > 0; i--)
					if (((Point)sweepPoints[i]).X == breakDistance)
					{
						sweepPoints.RemoveAt(i);
						shouldRemove--;
					}
			}
		}*/
		#endregion

		#region RemoveWorstSweepPointsTop()
		/*public static void RemoveWorstSweepPointsTop(ref ArrayList sweepPoints, float percentsToRemove)
		{
			if (sweepPoints.Count > Convert.ToInt32(sweepPoints.Count * percentsToRemove))
			{
				int shouldRemove = Convert.ToInt32(sweepPoints.Count * percentsToRemove);

				if (shouldRemove == 0)
					return;

				ArrayList distancesList = new ArrayList();

				foreach (Point sweepPoint in sweepPoints)
					distancesList.Add((int)Math.Abs(sweepPoint.Y));

				distancesList.Sort();
				int breakDistance = (int)distancesList[shouldRemove - 1];

				for (int i = sweepPoints.Count - 1; i >= 0; i--)
					if ((((Point)sweepPoints[i]).Y < breakDistance))
					{
						sweepPoints.RemoveAt(i);
						shouldRemove--;
					}
				for (int i = sweepPoints.Count - 1; i >= 0 && shouldRemove > 0; i--)
					if (((Point)sweepPoints[i]).Y == breakDistance)
					{
						sweepPoints.RemoveAt(i);
						shouldRemove--;
					}
			}
		}*/
		#endregion

		#region RemoveWorstSweepPointsBottom()
		/*public static void RemoveWorstSweepPointsBottom(ref ArrayList sweepPoints, float percentsToRemove)
		{
			if (sweepPoints.Count > Convert.ToInt32(sweepPoints.Count * (1 - percentsToRemove)))
			{
				int shouldRemove = Convert.ToInt32(sweepPoints.Count * percentsToRemove);

				if (shouldRemove == 0)
					return;

				ArrayList distancesList = new ArrayList();

				foreach (Point sweepPoint in sweepPoints)
					distancesList.Add((int)Math.Abs(sweepPoint.Y));

				distancesList.Sort();
				int breakDistance = (int)distancesList[sweepPoints.Count - shouldRemove - 1];

				for (int i = sweepPoints.Count - 1; i >= 0; i--)
					if ((((Point)sweepPoints[i]).Y > breakDistance))
					{
						sweepPoints.RemoveAt(i);
						shouldRemove--;
					}
				for (int i = sweepPoints.Count - 1; i >= 0 && shouldRemove > 0; i--)
					if (((Point)sweepPoints[i]).Y == breakDistance)
					{
						sweepPoints.RemoveAt(i);
						shouldRemove--;
					}
			}
		}*/
		#endregion

		#region RemoveWorstSweepPointsX()
		/*public static void RemoveWorstSweepPointsX(ref ArrayList sweepPoints, float percentsToRemove, Rectangle imageRect)
		{
			if (sweepPoints.Count > Convert.ToInt32(sweepPoints.Count * (1 - percentsToRemove)))
			{
				long center = RasterProcessing.GetCenterX(sweepPoints, imageRect);
				ArrayList distancesList = new ArrayList();

				foreach (Point sweepPoint in sweepPoints)
					distancesList.Add((int)Math.Abs(center - sweepPoint.X));

				distancesList.Sort();
				int breakDistance = (int)distancesList[Convert.ToInt32(sweepPoints.Count * (1 - percentsToRemove))];

				for (int i = sweepPoints.Count - 1; i >= 0; i--)
					if ((((Point)sweepPoints[i]).X - center > breakDistance) || (((Point)sweepPoints[i]).X - center < -breakDistance))
						sweepPoints.RemoveAt(i);
			}
		}*/
		#endregion

		#region RemoveWorstSweepPointsY()
		/*public static void RemoveWorstSweepPointsY(ref ArrayList sweepPoints, float percentsToRemove, Rectangle imageRect)
		{
			if (sweepPoints.Count > Convert.ToInt32(sweepPoints.Count * (1 - percentsToRemove)))
			{
				long center = RasterProcessing.GetCenterY(sweepPoints, imageRect);
				ArrayList distancesList = new ArrayList();

				foreach (Point sweepPoint in sweepPoints)
					distancesList.Add((int)Math.Abs(center - sweepPoint.Y));

				distancesList.Sort();
				int breakDistance = (int)distancesList[Convert.ToInt32(sweepPoints.Count * (1 - percentsToRemove))];

				for (int i = sweepPoints.Count - 1; i >= 0; i--)
					if ((((Point)sweepPoints[i]).Y - center > breakDistance) || (((Point)sweepPoints[i]).Y - center < -breakDistance))
						sweepPoints.RemoveAt(i);
			}
		}*/
		#endregion

		#region DrawToFile()
		public static void DrawToFile(string filePath, byte[,] array)
		{
			Bitmap result = null;
			BitmapData bmpData = null;

			try
			{
				int width = array.GetLength(1) * 8;
				int height = array.GetLength(0);

				result = Debug.GetBitmap(new Size(width, height));

				Graphics g = Graphics.FromImage(result);
				SolidBrush brush = new SolidBrush(Color.FromArgb(100, 120, 120, 120));
				Color color = Color.Yellow;

				bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				int stride = bmpData.Stride;

				unsafe
				{
					byte* scan0 = (byte*)bmpData.Scan0.ToPointer();

					for (int x = 0; x < width; x++)
						for (int y = 0; y < height; y++)
						{
							if ((array[y, x / 8] & (0x80 >> (x & 0x07))) > 0)
							{
								scan0[y * stride + x * 3] = 128;
								scan0[y * stride + x * 3+1] = 255;
								scan0[y * stride + x * 3+2] = 255;
							}
						}
				}
			}
			finally
			{
				if (bmpData != null)
				{
					result.UnlockBits(bmpData);
					result.Save(filePath, ImageFormat.Png);
					result.Dispose();
				}
			}
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region WorkOutPointsSmallObjects()
		static unsafe void WorkOutPointsSmallObjects(byte* scan0, int stride, byte[,] array, Rectangle clip, Point objectPoint)
		{
			int x, y;
			int left = clip.X;
			int top = clip.Y;
			int right = clip.Right;
			int bottom = clip.Bottom;
			Queue<Point> points = new Queue<Point>();

			points.Enqueue(objectPoint);

			while (points.Count > 0)
			{
				Point p = points.Dequeue();

				array[(p.Y - top), (p.X - left) / 8] |= (byte)(0x80 >> ((p.X - left) & 0x07));

				//go to the right
				x = p.X + 1;
				while (x < right && ((scan0[p.Y * stride + x / 8] & (0x80 >> (x & 0x07))) > 0))
				{
					//array[p.Y, x] = 2;
					scan0[p.Y * stride + x / 8] &= (byte)(0xFF7F >> (x & 0x07));
					points.Enqueue(new Point(x, p.Y));
					x++;
				}

				//go to the left
				x = p.X - 1;
				while (x >= left && ((scan0[p.Y * stride + x / 8] & (0x80 >> (x & 0x07))) > 0))
				{
					//array[p.Y, x] = 2;
					scan0[p.Y * stride + x / 8] &= (byte)(0xFF7F >> (x & 0x07));
					points.Enqueue(new Point(x, p.Y));
					x--;
				}

				//go up
				y = p.Y - 1;
				while (y >= top && ((scan0[y * stride + p.X / 8] & (0x80 >> (p.X & 0x07))) > 0))
				{
					//array[y, p.X] = 2;
					scan0[y * stride + p.X / 8] &= (byte)(0xFF7F >> (p.X & 0x07));
					points.Enqueue(new Point(p.X, y));
					y--;
				}

				//go down
				y = p.Y + 1;
				while (y < bottom && ((scan0[y * stride + p.X / 8] & (0x80 >> (p.X & 0x07))) > 0))
				{
					//array[y, p.X] = 2;
					scan0[y * stride + p.X / 8] &= (byte)(0xFF7F >> (p.X & 0x07));
					points.Enqueue(new Point(p.X, y));
					y++;
				}

				//go up left
				x = p.X - 1;
				y = p.Y - 1;
				while (x >= left && y >= top && ((scan0[y * stride + x / 8] & (0x80 >> (x & 0x07))) > 0))
				{
					//array[y, x] = 2;
					scan0[y * stride + x / 8] &= (byte)(0xFF7F >> (x & 0x07));
					points.Enqueue(new Point(x, y));
					x--;
					y--;
				}

				//go up right
				x = p.X + 1;
				y = p.Y - 1;
				while (x < right && y >= top && ((scan0[y * stride + x / 8] & (0x80 >> (x & 0x07))) > 0))
				{
					//array[y, x] = 2;
					scan0[y * stride + x / 8] &= (byte)(0xFF7F >> (x & 0x07));
					points.Enqueue(new Point(x, y));
					x++;
					y--;
				}

				//go down left
				x = p.X - 1;
				y = p.Y + 1;
				while (x >= left && y < bottom && ((scan0[y * stride + x / 8] & (0x80 >> (x & 0x07))) > 0))
				{
					//array[y, x] = 2;
					scan0[y * stride + x / 8] &= (byte)(0xFF7F >> (x & 0x07));
					points.Enqueue(new Point(x, y));
					x--;
					y++;
				}

				//go down right
				x = p.X + 1;
				y = p.Y + 1;
				while (x < right && y < bottom && ((scan0[y * stride + x / 8] & (0x80 >> (x & 0x07))) > 0))
				{
					//array[y, x] = 2;
					scan0[y * stride + x / 8] &= (byte)(0xFF7F >> (x & 0x07));
					points.Enqueue(new Point(x, y));
					x++;
					y++;
				}
			}
		}
		#endregion

		#region WorkOutPointsSmallObjects()
		static unsafe void WorkOutPointsSmallObjects(byte[,] imageBitArray, int imageBitArrayWidth, byte[,] array, Rectangle clip, Point objectPoint)
		{
			int x, y;
			int left = clip.X;
			int top = clip.Y;
			int right = clip.Right;
			int bottom = clip.Bottom;
			Queue<Point> points = new Queue<Point>();

			points.Enqueue(objectPoint);

			while (points.Count > 0)
			{
				Point p = points.Dequeue();

				array[(p.Y - top), (p.X - left) / 8] |= (byte)(0x80 >> ((p.X - left) & 0x07));

				//go to the right
				x = p.X + 1;
				while (x < right && ((imageBitArray[p.Y, x / 8] & (0x80 >> (x & 0x07))) > 0))
				{
					//array[p.Y, x] = 2;
					imageBitArray[p.Y, x / 8] &= (byte)(0xFF7F >> (x & 0x07));
					points.Enqueue(new Point(x, p.Y));
					x++;
				}

				//go to the left
				x = p.X - 1;
				while (x >= left && ((imageBitArray[p.Y, x / 8] & (0x80 >> (x & 0x07))) > 0))
				{
					//array[p.Y, x] = 2;
					imageBitArray[p.Y, x / 8] &= (byte)(0xFF7F >> (x & 0x07));
					points.Enqueue(new Point(x, p.Y));
					x--;
				}

				//go up
				y = p.Y - 1;
				while (y >= top && ((imageBitArray[y, p.X / 8] & (0x80 >> (p.X & 0x07))) > 0))
				{
					//array[y, p.X] = 2;
					imageBitArray[y, p.X / 8] &= (byte)(0xFF7F >> (p.X & 0x07));
					points.Enqueue(new Point(p.X, y));
					y--;
				}

				//go down
				y = p.Y + 1;
				while (y < bottom && ((imageBitArray[y, p.X / 8] & (0x80 >> (p.X & 0x07))) > 0))
				{
					//array[y, p.X] = 2;
					imageBitArray[y, p.X / 8] &= (byte)(0xFF7F >> (p.X & 0x07));
					points.Enqueue(new Point(p.X, y));
					y++;
				}

				//go up left
				x = p.X - 1;
				y = p.Y - 1;
				while (x >= left && y >= top && ((imageBitArray[y, x / 8] & (0x80 >> (x & 0x07))) > 0))
				{
					//array[y, x] = 2;
					imageBitArray[y, x / 8] &= (byte)(0xFF7F >> (x & 0x07));
					points.Enqueue(new Point(x, y));
					x--;
					y--;
				}

				//go up right
				x = p.X + 1;
				y = p.Y - 1;
				while (x < right && y >= top && ((imageBitArray[y, x / 8] & (0x80 >> (x & 0x07))) > 0))
				{
					//array[y, x] = 2;
					imageBitArray[y, x / 8] &= (byte)(0xFF7F >> (x & 0x07));
					points.Enqueue(new Point(x, y));
					x++;
					y--;
				}

				//go down left
				x = p.X - 1;
				y = p.Y + 1;
				while (x >= left && y < bottom && ((imageBitArray[y, x / 8] & (0x80 >> (x & 0x07))) > 0))
				{
					//array[y, x] = 2;
					imageBitArray[y, x / 8] &= (byte)(0xFF7F >> (x & 0x07));
					points.Enqueue(new Point(x, y));
					x--;
					y++;
				}

				//go down right
				x = p.X + 1;
				y = p.Y + 1;
				while (x < right && y < bottom && ((imageBitArray[y, x / 8] & (0x80 >> (x & 0x07))) > 0))
				{
					//array[y, x] = 2;
					imageBitArray[y, x / 8] &= (byte)(0xFF7F >> (x & 0x07));
					points.Enqueue(new Point(x, y));
					x++;
					y++;
				}
			}
		}
		#endregion

		#region WorkOutPointsLargeObjects()
		static unsafe void WorkOutPointsLargeObjects(byte* scan0, int stride, byte[,] array, Rectangle clip, Point objectPoint)
		{
			bool change = true;
			int left = clip.X;
			int top = clip.Y;
			int width = clip.Width;
			int height = clip.Height;

			array[objectPoint.Y - clip.Y, (objectPoint.X - clip.X) / 8] |= (byte)(0x80 >> ((objectPoint.X - clip.X) & 0x07));

#if DEBUG
			DateTime start = DateTime.Now;
			int iterations = 0;
#endif
			while (change)
			{
				change = false;

#if DEBUG
				iterations++;
#endif

				List<ObjectPoint> objectPoints = FindPointsBrutalWay(scan0, stride, array, left, top, width, height, ref change);

				while (objectPoints.Count > 0 && objectPoints.Count < 100000)
				{
					objectPoints = FindPointsEasyWay(scan0, stride, array, left, top, width, height, ref change, objectPoints);
				}
			}

#if DEBUG
			Console.WriteLine("RasterProcessing, WorkOutPointsLargeObjects(): " + DateTime.Now.Subtract(start).ToString() + ", Iterations: " + iterations);
#endif

#if SAVE_RESULTS
			DrawToFile(Debug.SaveToDir + "Tmp.png", array);
#endif
		}
		#endregion

		#region WorkOutPointsLargeObjects()
		static unsafe void WorkOutPointsLargeObjects(byte[,] imageBitArray, int imageBitArrayWidth, byte[,] array, Rectangle clip, Point objectPoint)
		{
			bool change = true;
			int left = clip.X;
			int top = clip.Y;
			int width = clip.Width;
			int height = clip.Height;

			array[objectPoint.Y - clip.Y, (objectPoint.X - clip.X) / 8] |= (byte)(0x80 >> ((objectPoint.X - clip.X) & 0x07));

#if DEBUG
			DateTime start = DateTime.Now;
			int iterations = 0;
#endif
			while (change)
			{
				change = false;

#if DEBUG
				iterations++;
#endif

				List<ObjectPoint> objectPoints = FindPointsBrutalWay(imageBitArray, imageBitArrayWidth, array, left, top, width, height, ref change);

				while (objectPoints.Count > 0 && objectPoints.Count < 100000)
				{
					objectPoints = FindPointsEasyWay(imageBitArray, imageBitArrayWidth, array, left, top, width, height, ref change, objectPoints);
				}

#if DEBUG
				//DrawToFile(Debug.SaveToDir + "Tmp.png", array);
#endif
			}


#if DEBUG
			Console.WriteLine("RasterProcessing, WorkOutPointsLargeObjects(): " + DateTime.Now.Subtract(start).ToString() + ", Iterations: " + iterations);
#endif

#if SAVE_RESULTS
			DrawToFile(Debug.SaveToDir + "Tmp.png", array);
#endif
		}
		#endregion

		#region FindPointsBrutalWay()
		static unsafe List<ObjectPoint> FindPointsBrutalWay(byte* scan0, int stride, byte[,] array, int left, int top, int width, int height, ref bool change)
		{
			List<ObjectPoint> objectPoints = new List<ObjectPoint>();
			int x, y;

			for (int xArray = 0; xArray < width; xArray++)
			{
				for (int yArray = 0; yArray < height; yArray++)
				{
					if (((array[yArray, xArray / 8] & (0x80 >> (xArray & 0x07))) > 0) && ((scan0[(yArray + top) * stride + (xArray + left) / 8] & (0x80 >> ((xArray + left) & 0x07))) > 0))
					{
						//go to the right
						x = xArray + 1;
						y = yArray;
						while (x < width && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							x++;
						}

						//go to the left
						x = xArray - 1;
						y = yArray;
						while (x >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							x--;
						}

						//go up
						x = xArray;
						y = yArray - 1;
						while (y >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							y--;
						}

						//go down
						x = xArray;
						y = yArray + 1;
						while (y < height && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							y++;
						}

						//go NW
						x = xArray - 1;
						y = yArray - 1;
						while (x >= 0 && y >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							x--;
							y--;
						}

						//go NE
						x = xArray + 1;
						y = yArray - 1;
						while (x < width && y >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							x++;
							y--;
						}

						//go SW
						x = xArray - 1;
						y = yArray + 1;
						while (x >= 0 && y < height && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							x--;
							y++;
						}

						//go SE
						x = xArray + 1;
						y = yArray + 1;
						while (x < width && y < height && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							x++;
							y++;
						}

						scan0[(yArray + top) * stride + (xArray + left) / 8] &= (byte)(0xFF7F >> ((xArray + left) & 0x07));
					}
				}

				while (objectPoints.Count > 100000)
				{
					objectPoints = FindPointsEasyWay(scan0, stride, array, left, top, width, height, ref change, objectPoints);
				}
			}

			return objectPoints;
		}
		#endregion

		#region FindPointsBrutalWay()
		static unsafe List<ObjectPoint> FindPointsBrutalWay(byte[,] imageBitArray, int imageBitArrayWidth, byte[,] array, int left, int top, int width, int height, ref bool change)
		{
			List<ObjectPoint> objectPoints = new List<ObjectPoint>();
			int x, y;

			for (int xArray = 0; xArray < width; xArray++)
			{
				for (int yArray = 0; yArray < height; yArray++)
				{
					if (((array[yArray, xArray / 8] & (0x80 >> (xArray & 0x07))) > 0) && ((imageBitArray[(yArray + top), (xArray + left) / 8] & (0x80 >> ((xArray + left) & 0x07))) > 0))
					{
						//go to the right
						x = xArray + 1;
						y = yArray;
						while (x < width && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							x++;
						}

						//go to the left
						x = xArray - 1;
						y = yArray;
						while (x >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							x--;
						}

						//go up
						x = xArray;
						y = yArray - 1;
						while (y >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							y--;
						}

						//go down
						x = xArray;
						y = yArray + 1;
						while (y < height && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							y++;
						}

						//go NW
						x = xArray - 1;
						y = yArray - 1;
						while (x >= 0 && y >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							x--;
							y--;
						}

						//go NE
						x = xArray + 1;
						y = yArray - 1;
						while (x < width && y >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							x++;
							y--;
						}

						//go SW
						x = xArray - 1;
						y = yArray + 1;
						while (x >= 0 && y < height && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							x--;
							y++;
						}

						//go SE
						x = xArray + 1;
						y = yArray + 1;
						while (x < width && y < height && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
							change = true;
							objectPoints.Add(new ObjectPoint(x, y));
							x++;
							y++;
						}

						imageBitArray[(yArray + top), (xArray + left) / 8] &= (byte)(0xFF7F >> ((xArray + left) & 0x07));
					}
				}

				while (objectPoints.Count > 100000)
				{
					objectPoints = FindPointsEasyWay(imageBitArray, imageBitArrayWidth, array, left, top, width, height, ref change, objectPoints);
				}
			}

			return objectPoints;
		}
		#endregion

		#region FindPointsEasyWay()
		static unsafe List<ObjectPoint> FindPointsEasyWay(byte* scan0, int stride, byte[,] array, int left, int top, int width, int height, ref bool change, List<ObjectPoint> objectPts)
		{
			List<ObjectPoint> objectPoints = new List<ObjectPoint>();
			int x, y;
			int i;

			for (i = 0; i < objectPts.Count; i++)
			{
				int xArray = objectPts[i].X;
				int yArray = objectPts[i].Y;

				if (((array[yArray, xArray / 8] & (0x80 >> (xArray & 0x07))) > 0) && ((scan0[(yArray + top) * stride + (xArray + left) / 8] & (0x80 >> ((xArray + left) & 0x07))) > 0))
				{
					//go to the right
					x = xArray + 1;
					y = yArray;
					while (x < width && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						x++;
					}

					//go to the left
					x = xArray - 1;
					y = yArray;
					while (x >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						x--;
					}

					//go up
					x = xArray;
					y = yArray - 1;
					while (y >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						y--;
					}

					//go down
					x = xArray;
					y = yArray + 1;
					while (y < height && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						y++;
					}

					//go NW
					x = xArray - 1;
					y = yArray - 1;
					while (x >= 0 && y >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						objectPoints.Add(new ObjectPoint(x, y));
						change = true;
						x--;
						y--;
					}

					//go NE
					x = xArray + 1;
					y = yArray - 1;
					while (x < width && y >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						x++;
						y--;
					}

					//go SW
					x = xArray - 1;
					y = yArray + 1;
					while (x >= 0 && y < height && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						x--;
						y++;
					}

					//go SE
					x = xArray + 1;
					y = yArray + 1;
					while (x < width && y < height && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((scan0[(y + top) * stride + (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						x++;
						y++;
					}

					scan0[(yArray + top) * stride + (xArray + left) / 8] &= (byte)(0xFF7F >> ((xArray + left) & 0x07));
				}
			}

			return objectPoints;
		}
		#endregion

		#region FindPointsEasyWay()
		static unsafe List<ObjectPoint> FindPointsEasyWay(byte[,] imageBitArray, int imageBitArrayWidth, byte[,] array, int left, int top, int width, int height, ref bool change, List<ObjectPoint> objectPts)
		{
			List<ObjectPoint> objectPoints = new List<ObjectPoint>();
			int x, y;
			int i;

			for (i = 0; i < objectPts.Count; i++)
			{
				int xArray = objectPts[i].X;
				int yArray = objectPts[i].Y;

				if (((array[yArray, xArray / 8] & (0x80 >> (xArray & 0x07))) > 0) && ((imageBitArray[(yArray + top), (xArray + left) / 8] & (0x80 >> ((xArray + left) & 0x07))) > 0))
				{
					//go to the right
					x = xArray + 1;
					y = yArray;
					while (x < width && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						x++;
					}

					//go to the left
					x = xArray - 1;
					y = yArray;
					while (x >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						x--;
					}

					//go up
					x = xArray;
					y = yArray - 1;
					while (y >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						y--;
					}

					//go down
					x = xArray;
					y = yArray + 1;
					while (y < height && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						y++;
					}

					//go NW
					x = xArray - 1;
					y = yArray - 1;
					while (x >= 0 && y >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						objectPoints.Add(new ObjectPoint(x, y));
						change = true;
						x--;
						y--;
					}

					//go NE
					x = xArray + 1;
					y = yArray - 1;
					while (x < width && y >= 0 && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						x++;
						y--;
					}

					//go SW
					x = xArray - 1;
					y = yArray + 1;
					while (x >= 0 && y < height && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						x--;
						y++;
					}

					//go SE
					x = xArray + 1;
					y = yArray + 1;
					while (x < width && y < height && ((array[y, x / 8] & (0x80 >> (x & 0x07))) == 0) && ((imageBitArray[(y + top), (x + left) / 8] & (0x80 >> ((x + left) & 0x07))) > 0))
					{
						array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						change = true;
						objectPoints.Add(new ObjectPoint(x, y));
						x++;
						y++;
					}

					imageBitArray[(yArray + top), (xArray + left) / 8] &= (byte)(0xFF7F >> ((xArray + left) & 0x07));
				}
			}

			return objectPoints;
		}
		#endregion

		#region GetCenterX()
		private static int GetCenterX(ArrayList sweepPoints, Rectangle rect)
		{
			if (sweepPoints.Count == 0)
				return 0;

			double numOfPoints = sweepPoints.Count;
			double xTop, xBottom, a, b;
			long sumXY = 0, sumX = 0, sumY = 0, sumYxY = 0;

			foreach (Point sweepPoint in sweepPoints)
			{
				sumXY += sweepPoint.X * sweepPoint.Y;
				sumX += sweepPoint.X;
				sumY += sweepPoint.Y;
				sumYxY += sweepPoint.Y * sweepPoint.Y;
			}

			if ((sumYxY - sumY * sumY / numOfPoints) > 0)
				b = ((sumXY - sumX * sumY / numOfPoints) / (sumYxY - sumY * sumY / numOfPoints));
			else
				b = 0;

			a = sumX / numOfPoints - b * sumY / numOfPoints;

			//xTop = a;
			//xBottom = a + b * rect.Height;

			int yMin = ((Point)sweepPoints[0]).Y;
			int yMax = ((Point)sweepPoints[0]).Y;

			foreach (Point sweepPoint in sweepPoints)
			{
				if (yMin > sweepPoint.Y)
					yMin = sweepPoint.Y;
				if (yMax < sweepPoint.Y)
					yMax = sweepPoint.Y;
			}

			xTop = a;
			xBottom = a + b * (yMax - yMin);

			return (int)(xTop + (xBottom - xTop) / 2);
		}
		#endregion

		#region GetCenterY()
		private static int GetCenterY(ArrayList sweepPoints, Rectangle rect)
		{
			if (sweepPoints.Count == 0)
				return 0;

			double numOfPoints = sweepPoints.Count;
			double yLeft, yRight, a, b;
			long sumXY = 0, sumX = 0, sumY = 0, sumXxX = 0;

			foreach (Point sweepPoint in sweepPoints)
			{
				sumXY += sweepPoint.X * sweepPoint.Y;
				sumX += sweepPoint.X;
				sumY += sweepPoint.Y;
				sumXxX += sweepPoint.X * sweepPoint.X;
			}

			if ((sumXxX - sumX * sumX / numOfPoints) > 0)
				b = ((sumXY - sumX * sumY / numOfPoints) / (sumXxX - sumX * sumX / numOfPoints));
			else
				b = 0;
			a = sumY / numOfPoints - b * sumX / numOfPoints;

			//yLeft = a;
			//yRight = a + b * rect.Width;

			int xMin = ((Point)sweepPoints[0]).X;
			int xMax = ((Point)sweepPoints[0]).X;
			foreach (Point sweepPoint in sweepPoints)
			{
				if (xMin > sweepPoint.X)
					xMin = sweepPoint.X;
				if (xMax < sweepPoint.X)
					xMax = sweepPoint.X;
			}

			yLeft = a;
			yRight = a + b * (xMax - xMin);

			return (int)(yLeft + (yRight - yLeft) / 2);
		}
		#endregion

		#endregion

	}

}
