using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.CropDeskew
{
	
	public class Crop
	{
		PointF cornerUl, cornerUr, cornerLl, cornerLr;
		double bestAngle = 0;

		int?[] surfaceL;
		int?[] surfaceT;
		int?[] surfaceR;
		int?[] surfaceB;

		Rectangle tangentRect = Rectangle.Empty;

	
		#region constructor
		public Crop(Bitmap bitmap)
		{
			BitmapData bmpData = null;

			try
			{
				bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				FillInSurfaces(bmpData);
			}
			finally
			{
				if (bmpData != null)
					bitmap.UnlockBits(bmpData);
			}

			Point pL, pT, pR, pB;
			PointF cUL, cUR, cLL, cLR;
			double smallestDiagonal = double.MaxValue;

			for (double angle = -0.767944; angle <= 0.767944; angle += 0.043633)
			{
				GetCrop(angle, out pL, out pT, out pR, out pB);
				GetCorners(bmpData, angle, pL, pT, pR, pB, out cUL, out cUR, out cLL, out cLR);

				double diagonal = ((cUL.X - cLR.X) * (cUL.X - cLR.X) + (cUL.Y - cLR.Y) * (cUL.Y - cLR.Y)) + ((cLL.X - cUR.X) * (cLL.X - cUR.X) + (cLL.Y - cUR.Y) * (cLL.Y - cUR.Y));

				if (diagonal < smallestDiagonal)
				{
					bestAngle = angle;
					smallestDiagonal = diagonal;
					cornerUl = cUL;
					cornerUr = cUR;
					cornerLl = cLL;
					cornerLr = cLR;
				}
			}

			for (double angle = bestAngle - 0.043633; angle <= bestAngle + 0.043633; angle += 0.00872664)
			{
				GetCrop(angle, out pL, out pT, out pR, out pB);
				GetCorners(bmpData, angle, pL, pT, pR, pB, out cUL, out cUR, out cLL, out cLR);

				double diagonal = ((cUL.X - cLR.X) * (cUL.X - cLR.X) + (cUL.Y - cLR.Y) * (cUL.Y - cLR.Y)) + ((cLL.X - cUR.X) * (cLL.X - cUR.X) + (cLL.Y - cUR.Y) * (cLL.Y - cUR.Y));

				if (diagonal < smallestDiagonal)
				{
					bestAngle = angle;
					smallestDiagonal = diagonal;
					cornerUl = cUL;
					cornerUr = cUR;
					cornerLl = cLL;
					cornerLr = cLR;
				}
			}

			for (double angle = bestAngle - 0.00087266; angle <= bestAngle + 0.00087266; angle += 0.000218165)
			{
				GetCrop(angle, out pL, out pT, out pR, out pB);
				GetCorners(bmpData, angle, pL, pT, pR, pB, out cUL, out cUR, out cLL, out cLR);

				double diagonal = ((cUL.X - cLR.X) * (cUL.X - cLR.X) + (cUL.Y - cLR.Y) * (cUL.Y - cLR.Y)) + ((cLL.X - cUR.X) * (cLL.X - cUR.X) + (cLL.Y - cUR.Y) * (cLL.Y - cUR.Y));

				if (diagonal < smallestDiagonal)
				{
					bestAngle = angle;
					smallestDiagonal = diagonal;
					cornerUl = cUL;
					cornerUr = cUR;
					cornerLl = cLL;
					cornerLr = cLR;
				}
			}
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties
		
		public Rectangle TangentialRectangle { get { return tangentRect; } }
		public double Angle { get { return bestAngle; } }
		public PointF Cul { get { return cornerUl; } }
		public PointF Cur { get { return cornerUr; } }
		public PointF Cll { get { return cornerLl; } }
		public PointF Clr { get { return cornerLr; } }
	
		#endregion


		//PUBLIC METHODS
		#region public methods


		#endregion


		//PRIVATE METHODS

		#region GetCrop()
		private void GetCrop(double angle, out Point pL, out Point pT, out Point pR, out Point pB)
		{
			pT = FindTopEdge(angle);
			pB = FindBottomEdge(angle);
			pL = FindLeftEdge(angle);
			pR = FindRightEdge(angle);
		}
		#endregion

		#region FindTopEdge()
		private Point FindTopEdge(double angle)
		{
			int		width = surfaceT.Length;
			double	y;
			double	jump = Math.Tan(angle);
			int		xMin = 0;
			double	yMin = double.MaxValue;

			for (int x = 0; x < width; x++)
			{
				if (surfaceT[x] != null)
				{
					y = surfaceT[x].Value - x * jump;

					if (yMin > y)
					{
						yMin = y;
						xMin = x;
					}
				}
			}

			return new Point(xMin, surfaceT[xMin].Value);
		}
		#endregion
	
		#region FindBottomEdge()
		private Point FindBottomEdge(double angle)
		{
			int		width = surfaceB.Length;
			double	y;
			double	jump = Math.Tan(angle);
			int		xMin = 0;
			double	yMin = -double.MaxValue;

			for (int x = 0; x < width; x++)
			{
				if (surfaceT[x] != null)
				{
					y = surfaceB[x].Value - x * jump;

					if (yMin < y)
					{
						yMin = y;
						xMin = x;
					}
				}
			}

			return new Point(xMin, surfaceB[xMin].Value);
		}
		#endregion

		#region FindLeftEdge()
		private Point FindLeftEdge(double angle)
		{
			int height = surfaceL.Length;
			double x;
			double jump = Math.Tan(angle);
			int yMin = 0;
			double xMin = + double.MaxValue;

			for (int y = 0; y < height; y++)
			{
				if (surfaceL[y] != null)
				{
					x = surfaceL[y].Value + y * jump;

					if (xMin > x)
					{
						xMin = x;
						yMin = y;
					}
				}
			}

			return new Point(surfaceL[yMin].Value, yMin);
		}
		#endregion

		#region FindRightEdge()
		private Point FindRightEdge(double angle)
		{
			int height = surfaceR.Length;
			double x;
			double jump = Math.Tan(angle);
			int yMin = 0;
			double xMin = -double.MaxValue;

			for (int y = 0; y < height; y++)
			{
				if (surfaceR[y] != null)
				{
					x = surfaceR[y].Value + y * jump;

					if (xMin < x)
					{
						xMin = x;
						yMin = y;
					}
				}
			}

			return new Point(surfaceR[yMin].Value, yMin);
		}
		#endregion

		#region GetCorners()
		private static void GetCorners(BitmapData bmpData, double angle, Point pL, Point pT, Point pR, Point pB, out PointF cUL, out PointF cUR, out PointF cLL, out PointF cLR)
		{
			Line2D lineL = new Line2D(pL, new PointF(pL.X - (float)Math.Tan(angle) * 5000, pL.Y + 5000));
			Line2D lineT = new Line2D(pT, new PointF(pT.X + 5000, pT.Y + (float)Math.Tan(angle) * 5000));
			Line2D lineR = new Line2D(pR, new PointF(pR.X - (float)Math.Tan(angle) * 5000, pR.Y + 5000));
			Line2D lineB = new Line2D(pB, new PointF(pB.X + 5000, pB.Y + (float)Math.Tan(angle) * 5000));

			double interceptX = 0, interceptY = 0;

			lineL.InterceptPoint(lineT, ref interceptX, ref interceptY);
			cUL = new PointF((float)interceptX, (float)interceptY);

			lineL.InterceptPoint(lineB, ref interceptX, ref interceptY);
			cLL = new PointF((float)interceptX, (float)interceptY);

			lineT.InterceptPoint(lineR, ref interceptX, ref interceptY);
			cUR = new PointF((float)interceptX, (float)interceptY);

			lineB.InterceptPoint(lineR, ref interceptX, ref interceptY);
			cLR = new PointF((float)interceptX, (float)interceptY);
		}
		#endregion

		#region FillInSurfaces()
		private void FillInSurfaces(BitmapData bmpData)
		{
			surfaceL = new int?[bmpData.Height];
			surfaceT = new int?[bmpData.Width];
			surfaceR = new int?[bmpData.Height];
			surfaceB = new int?[bmpData.Width];

			int width = bmpData.Width;
			int height = bmpData.Height;
			int stride = bmpData.Stride;
			int x, y;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				byte* pCurrent;

				//top
				for (x = 0; x < width; x++)
				{
					for (y = 0; y < height; y++)
					{
						pCurrent = pSource + y * stride + (x >> 3);

						if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
						{
							if ((x < 24) || (x >= width - 24) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
								(pCurrent[3] != 0) || (pCurrent[-3 * stride - 3] != 0) || (pCurrent[+3 * stride - 3] != 0) ||
								(pCurrent[-3 * stride + 3] != 0) || (pCurrent[+3 * stride + 3] != 0))
							{
								surfaceT[x] = y;
								break;
							}
						}
					}
				}

				//bottom
				for (x = 0; x < width; x++)
				{
					for (y = height - 1; y >= 0; y--)
					{
						pCurrent = pSource + y * stride + (x >> 3);

						if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
						{
							if ((x < 24) || (x >= width - 24) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
								(pCurrent[3] != 0) || (pCurrent[-3 * stride - 3] != 0) || (pCurrent[+3 * stride - 3] != 0) ||
								(pCurrent[-3 * stride + 3] != 0) || (pCurrent[+3 * stride + 3] != 0))
							{
								surfaceB[x] = y;
								break;
							}
						}
					}
				}

				//left
				for (y = 0; y < height; y++)
				{
					for (x = 0; x < width; x++)
					{
						pCurrent = pSource + y * stride + (x >> 3);

						if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
						{
							if ((x < 24) || (x >= width - 24) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
								(pCurrent[3] != 0) || (pCurrent[-3 * stride - 3] != 0) || (pCurrent[+3 * stride - 3] != 0) ||
								(pCurrent[-3 * stride + 3] != 0) || (pCurrent[+3 * stride + 3] != 0))
							{
								surfaceL[y] = x;
								break;
							}
						}
					}
				}

				//right
				for (y = 0; y < height; y++)
				{
					for (x = width - 1; x >= 0; x--)
					{
						pCurrent = pSource + y * stride + (x >> 3);

						if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
						{
							if ((x < 24) || (x >= width - 24) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
								(pCurrent[3] != 0) || (pCurrent[-3 * stride - 3] != 0) || (pCurrent[+3 * stride - 3] != 0) ||
								(pCurrent[-3 * stride + 3] != 0) || (pCurrent[+3 * stride + 3] != 0))
							{
								surfaceR[y] = x;
								break;
							}
						}
					}
				}
			}

			//tangent rect
			int l = int.MaxValue, t = int.MaxValue, r = int.MinValue, b = int.MinValue;

			for (x = 0; x < width; x++)
			{
				if (surfaceT[x].HasValue && surfaceT[x].Value < t)
					t = surfaceT[x].Value;
				if (surfaceB[x].HasValue && surfaceB[x].Value > b)
					b = surfaceB[x].Value;
			}

			for (y = 0; y < height; y++)
			{
				if (surfaceL[y].HasValue && surfaceL[y].Value < l)
					l = surfaceL[y].Value;
				if (surfaceR[y].HasValue && surfaceR[y].Value > r)
					r = surfaceR[y].Value;
			}
		}
		#endregion

	}

}
