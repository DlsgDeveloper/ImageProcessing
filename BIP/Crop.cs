using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	
	public class Crop
	{
		Point left;
		Point top;
		Point right;
		Point bottom;


		#region constructor
		public Crop(Point left, Point top, Point right, Point bottom)
		{
			this.left = left;
			this.top = top;
			this.right = right;
			this.bottom = bottom;
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties
		public Point Left { get { return left; } set { this.left = value; } }
		public Point Top { get { return top; } set { this.top = value; } }
		public Point Right { get { return right; } set { this.right = value; } }
		public Point Bottom { get { return bottom; } set { this.bottom = value; } }

		public Rectangle TangentialRectangle { get { return Rectangle.FromLTRB(left.X, top.Y, right.X, bottom.Y); } }			
		#endregion


		//PUBLIC METHODS
		#region public methods

		#region GetCrop()
		public static Crop GetCrop(BitmapData bmpData, double angle)
		{
			int stride = bmpData.Stride;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();

				//top
				Point pT = FindTopEdge(pSource, stride, new Size(bmpData.Width, bmpData.Height), angle);
				Point pB = FindBottomEdge(pSource, stride, new Size(bmpData.Width, bmpData.Height), angle);
				Point pL = FindLeftEdge(pSource, stride, new Size(bmpData.Width, bmpData.Height), angle);
				Point pR = FindRightEdge(pSource, stride, new Size(bmpData.Width, bmpData.Height), angle);

				return new Crop(pL, pT, pR, pB);
			}
		}

		public static Crop GetCrop(int[,] array, double angle)
		{
			//top
			Point pT = FindTopEdge(array, angle);
			Point pB = FindBottomEdge(array, angle);
			Point pL = FindLeftEdge(array, angle);
			Point pR = FindRightEdge(array, angle);

			return new Crop(pL, pT, pR, pB);
		}
		#endregion

		#region GetCrop()
		public static Crop GetCrop(BitmapData bmpData)
		{
			Point pL = new Point(0, bmpData.Height / 2);
			Point pT = new Point(bmpData.Width / 2, 0);
			Point pR = new Point(bmpData.Width, bmpData.Height / 2);
			Point pB = new Point(bmpData.Width / 2, bmpData.Height);
			int width = bmpData.Width;
			int height = bmpData.Height;
			int stride = bmpData.Stride;
			int x, y;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				byte* pCurrent;

				//top
				for (y = 0; y < height; y++)
				{
					pCurrent = pSource + (y * stride);

					for (x = 0; x < width; x++)
					{
						pCurrent = pSource + y * stride + (x >> 3);

						if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
						{
							if ((x < 24) || (x >= width - 24) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
								(pCurrent[3] != 0) || (pCurrent[-3 * stride] != 0) || (pCurrent[3 * stride] != 0))
							{
								pT = new Point(x, y);
								y = height;
								break;
							}
						}
					}
				}

				//bottom
				for (y = height - 1; y > pT.Y; y--)
				{
					pCurrent = pSource + (y * stride);

					for (x = 0; x < width; x++)
					{
						pCurrent = pSource + y * stride + (x >> 3);

						if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
						{
							if ((x < 24) || (x >= width - 24) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
								(pCurrent[3] != 0) || (pCurrent[-3 * stride] != 0) || (pCurrent[3 * stride] != 0))
							{
								pB = new Point(x, y + 1);
								y = -1;
								break;
							}
						}
					}
				}

				int bottom = (height - 8 < pB.Y) ? height - 8 : pB.Y;
				//left
				for (x = 0; x < width; x++)
				{
					for (y = pT.Y; y < bottom; y++)
					{
						pCurrent = pSource + y * stride + (x >> 3);

						if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
						{
							if ((x < 24) || (x >= width - 24) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
								(pCurrent[3] != 0) || (pCurrent[-3 * stride] != 0) || (pCurrent[3 * stride] != 0))
							{
								pL = new Point(x, y);
								x = width;
								break;
							}
						}
					}
				}

				//right
				for (x = width - 1; x > pL.X; x--)
				{
					for (y = pT.Y; y < bottom; y++)
					{
						pCurrent = pSource + y * stride + (x >> 3);

						if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
						{
							if ((x < 24) || (x >= width - 24) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
								(pCurrent[3] != 0) || (pCurrent[-3 * stride] != 0) || (pCurrent[3 * stride] != 0))
							{
								pR = new Point(x + 1, y);
								x = -1;
								break;
							}
						}
					}
				}

				return new Crop(pL, pT, pR, pB);
			}
		}
		#endregion

		#endregion


		// PRIVATE METHODS
		#region private methods

		#region FindTopEdge()
		private unsafe static Point FindTopEdge(byte* pSource, int stride, Size size, double angle)
		{
			int width = size.Width / 8 * 8;
			int height = size.Height;

			int x, y;
			int yCurrent;
			double jump = Math.Tan(angle);

			unsafe
			{
				byte* pCurrent;

				if (angle < 0)
				{
					for (y = 0; y < height; y++)
					{
						for (x = 0; x < width; x++)
						{
							yCurrent = Convert.ToInt32(y + x * jump);

							if (yCurrent >= 0 && yCurrent < height)
							{
								pCurrent = pSource + (yCurrent * stride) + (x >> 3);

								if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
								{
									if ((x < 24) || (x >= width - 24) || (yCurrent < 3) || (yCurrent >= height - 3) || (pCurrent[-3] != 0) ||
										(pCurrent[3] != 0) || (pCurrent[-3 * stride - 3] != 0) || (pCurrent[+3 * stride - 3] != 0) ||
										(pCurrent[-3 * stride + 3] != 0) || (pCurrent[+3 * stride + 3] != 0))
									{
										return new Point(x, yCurrent);
									}
								}
							}
							else
								break;
						}
					}
				}
				else
				{
					for (y = 0; y < height; y++)
					{
						for (x = width - 1; x >= 0; x--)
						{
							yCurrent = Convert.ToInt32(y - (width - x) * jump);

							if (yCurrent >= 0 && yCurrent < height)
							{
								pCurrent = pSource + (yCurrent * stride) + (x >> 3);

								if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
								{
									if ((x < 24) || (x >= width - 24) || (yCurrent < 3) || (yCurrent >= height - 3) || (pCurrent[-3] != 0) ||
										(pCurrent[3] != 0) || (pCurrent[-3 * stride - 3] != 0) || (pCurrent[+3 * stride - 3] != 0) ||
										(pCurrent[-3 * stride + 3] != 0) || (pCurrent[+3 * stride + 3] != 0))
									{
										return new Point(x, yCurrent);
									}
								}
							}
							else
								break;
						}
					}
				}

				return new Point(size.Width / 2, 0);
			}
		}

		private static Point FindTopEdge(int[,] array, double angle)
		{
			int width = array.GetLength(1);
			int height = array.GetLength(0);

			int x, y;
			double jump = Math.Tan(angle);
			int yCurrent;

			if (angle < 0)
			{
				for (y = 0; y < height; y++)
				{
					for (x = 0; x < width; x++)
					{
						yCurrent = Convert.ToInt32(y + x * jump);

						if (yCurrent >= 0 && yCurrent < height)
						{
							if (array[yCurrent, x] == -1)
								return new Point(x, yCurrent);
						}
						else
							break;
					}
				}
			}
			else
			{
				for (y = 0; y < height; y++)
				{
					for (x = width - 1; x >= 0; x--)
					{
						yCurrent = Convert.ToInt32((double)y - (width - x) * jump);

						if (yCurrent >= 0 && yCurrent < height)
						{
							if (array[yCurrent, x] == -1)
							{
								return new Point(x, yCurrent);
							}
						}
						else
							break;
					}
				}
			}

			return new Point(width / 2, 0);

		}
		#endregion

		#region FindBottomEdge()
		private unsafe static Point FindBottomEdge(byte* pSource, int stride, Size size, double angle)
		{
			int width = size.Width / 8 * 8;
			int height = size.Height;

			int x, y;
			int yCurrent;
			double jump = Math.Tan(angle);

			unsafe
			{
				byte* pCurrent;

				//top
				if (angle > 0)
				{
					for (y = height - 1; y >= 0; y--)
					{
						for (x = 0; x < width; x++)
						{
							yCurrent = Convert.ToInt32(y + x * jump);

							if (yCurrent >= 0 && yCurrent < height)
							{
								pCurrent = pSource + (yCurrent * stride) + (x >> 3);

								if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
								{
									if ((x < 24) || (x >= width - 24) || (yCurrent < 3) || (yCurrent >= height - 3) || (pCurrent[-3] != 0) ||
										(pCurrent[3] != 0) || (pCurrent[-3 * stride - 3] != 0) || (pCurrent[+3 * stride - 3] != 0) ||
										(pCurrent[-3 * stride + 3] != 0) || (pCurrent[+3 * stride + 3] != 0))
									{
										return new Point(x, yCurrent);
									}
								}
							}
							else
								break;
						}
					}
				}
				else
				{
					for (y = height - 1; y >= 0; y--)
					{
						for (x = width - 1; x >= 0; x--)
						{
							yCurrent = Convert.ToInt32(y - (width - x) * jump);

							if (yCurrent >= 0 && yCurrent < height)
							{
								pCurrent = pSource + (yCurrent * stride) + (x >> 3);

								if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
								{
									if ((x < 24) || (x >= width - 24) || (yCurrent < 3) || (yCurrent >= height - 3) || (pCurrent[-3] != 0) ||
										(pCurrent[3] != 0) || (pCurrent[-3 * stride - 3] != 0) || (pCurrent[+3 * stride - 3] != 0) ||
										(pCurrent[-3 * stride + 3] != 0) || (pCurrent[+3 * stride + 3] != 0))
									{
										return new Point(x, yCurrent);
									}
								}
							}
							else
								break;
						}
					}
				}

				return new Point(size.Width / 2, size.Height);
			}
		}

		private static Point FindBottomEdge(int[,] array, double angle)
		{
			int width = array.GetLength(1);
			int height = array.GetLength(0);

			int x, y;
			double jump = Math.Tan(angle);
			int yCurrent;

			if (angle > 0)
			{
				for (y = height - 1; y >= 0; y--)
				{
					for (x = 0; x < width; x++)
					{
						yCurrent = Convert.ToInt32(y + x * jump);

						if (yCurrent >= 0 && yCurrent < height)
						{
							if (array[yCurrent, x] == -1)
								return new Point(x, Convert.ToInt32(yCurrent));
						}
						else
							break;
					}
				}
			}
			else
			{
				for (y = height - 1; y >= 0; y--)
				{
					for (x = width - 1; x >= 0; x--)
					{
						yCurrent = Convert.ToInt32((double)y - (width - x) * jump);

						if (yCurrent >= 0 && yCurrent < height)
						{
							if (array[yCurrent, x] == -1)
							{
								return new Point(x, Convert.ToInt32(yCurrent));
							}
						}
						else
							break;
					}
				}
			}

			return new Point(width / 2, height);

		}
		#endregion

		#region FindLeftEdge()
		private unsafe static Point FindLeftEdge(byte* pSource, int stride, Size size, double angle)
		{
			int width = size.Width / 8 * 8;
			int height = size.Height;

			int x, y;
			int xCurrent;
			double jump = Math.Tan(angle);

			unsafe
			{
				byte* pCurrent;

				//top
				if (angle > 0)
				{
					for (x = 0; x < width; x++)
					{
						for (y = 0; y < height; y++)
						{
							xCurrent = Convert.ToInt32(x - y * jump);

							if (xCurrent >= 0 && xCurrent < width)
							{
								pCurrent = pSource + y * stride + (xCurrent >> 3);

								if ((*pCurrent & (byte)(0x80 >> (xCurrent & 0x7))) > 0)
								{
									if ((xCurrent < 24) || (xCurrent >= width - 24) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
										(pCurrent[3] != 0) || (pCurrent[-3 * stride - 3] != 0) || (pCurrent[+3 * stride - 3] != 0) ||
										(pCurrent[-3 * stride + 3] != 0) || (pCurrent[+3 * stride + 3] != 0))
									{
										return new Point(xCurrent, y);
									}
								}
							}
							else
								break;
						}
					}
				}
				else
				{
					for (x = 0; x < width; x++)
					{
						for (y = height - 1; y >= 0; y--)
						{
							xCurrent = Convert.ToInt32(x + (height - y) * jump);

							if (xCurrent >= 0 && xCurrent < width)
							{
								pCurrent = pSource + y * stride + (xCurrent >> 3);

								if ((*pCurrent & (byte)(0x80 >> (xCurrent & 0x7))) > 0)
								{
									if ((xCurrent < 24) || (xCurrent >= width - 24) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
										(pCurrent[3] != 0) || (pCurrent[-3 * stride - 3] != 0) || (pCurrent[+3 * stride - 3] != 0) ||
										(pCurrent[-3 * stride + 3] != 0) || (pCurrent[+3 * stride + 3] != 0))
									{
										return new Point(xCurrent, y);
									}
								}
							}
							else
								break;
						}
					}
				}
			}

			return new Point(0, height / 2);
		}

		private static Point FindLeftEdge(int[,] array, double angle)
		{
			int width = array.GetLength(1);
			int height = array.GetLength(0);

			int x, y;
			double jump = Math.Tan(angle);
			int xCurrent;

			if (angle > 0)
			{
				for (x = 0; x < width; x++)
				{
					for (y = 0; y < height; y++)
					{
						xCurrent = Convert.ToInt32(x - y * jump);

						if (xCurrent >= 0 && xCurrent < width)
						{
							if (array[y, xCurrent] == -1)
								return new Point(Convert.ToInt32(xCurrent), y);
						}
						else
							break;
					}
				}
			}
			else
			{
				for (x = 0; x < width; x++)
				{
					for (y = height - 1; y >= 0; y--)
					{
						xCurrent = Convert.ToInt32(x + (height - y) * jump);

						if (xCurrent >= 0 && xCurrent < width)
						{
							if (array[y, xCurrent] == -1)
								return new Point(Convert.ToInt32(xCurrent), y);
						}
						else
							break;
					}
				}
			}

			return new Point(0, height / 2);
		}
		#endregion

		#region FindRightEdge()
		private unsafe static Point FindRightEdge(byte* pSource, int stride, Size size, double angle)
		{
			int width = size.Width;
			int height = size.Height;

			int x, y;
			int xCurrent;
			double jump = Math.Tan(angle);

			unsafe
			{
				byte* pCurrent;

				//top
				if (angle < 0)
				{
					for (x = width - 1; x >= 0; x--)
					{
						for (y = 0; y < height; y++)
						{
							xCurrent = Convert.ToInt32(x - y * jump);

							if (xCurrent >= 0 && xCurrent < width)
							{
								pCurrent = pSource + y * stride + (xCurrent >> 3);

								if ((*pCurrent & (byte)(0x80 >> (xCurrent & 0x7))) > 0)
								{
									if ((xCurrent < 24) || (xCurrent >= width - 24) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
										(pCurrent[3] != 0) || (pCurrent[-3 * stride - 3] != 0) || (pCurrent[+3 * stride - 3] != 0) ||
										(pCurrent[-3 * stride + 3] != 0) || (pCurrent[+3 * stride + 3] != 0))
									{
										return new Point(xCurrent, y);
									}
								}
							}
							else
								break;
						}
					}
				}
				else
				{
					int xLimit = - Convert.ToInt32(height * jump);

					for (x = width - 1; x >= xLimit; x--)
					{
						for (y = height - 1; y >= 0; y--)
						{
							xCurrent = Convert.ToInt32(x + (height - y) * jump);

							if (xCurrent >= 0 && xCurrent < width)
							{
								pCurrent = pSource + y * stride + (xCurrent >> 3);

								if ((*pCurrent & (byte)(0x80 >> ((int)xCurrent & 0x7))) > 0)
								{
									if ((xCurrent < 24) || (xCurrent >= width - 24) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
										(pCurrent[3] != 0) || (pCurrent[-3 * stride - 3] != 0) || (pCurrent[+3 * stride - 3] != 0) ||
										(pCurrent[-3 * stride + 3] != 0) || (pCurrent[+3 * stride + 3] != 0))
									{
										return new Point(xCurrent, y);
									}
								}
							}
							else if (xCurrent >= width)
								break;
						}
					}
				}
			}

			return new Point(size.Width, height / 2);
		}

		private static Point FindRightEdge(int[,] array, double angle)
		{
			int width = array.GetLength(1);
			int height = array.GetLength(0);

			int x, y;
			double jump = Math.Tan(angle);
			int xCurrent;

			//top
			if (angle < 0)
			{
				for (x = width - 1; x >= 0; x--)
				{
					for (y = 0; y < height; y++)
					{
						xCurrent = Convert.ToInt32(x - y * jump);

						if (xCurrent >= 0 && xCurrent < width)
						{
							if (array[y, xCurrent] == -1)
								return new Point(xCurrent, y);
						}
						else
							break;
					}
				}
			}
			else
			{
				for (x = width - 1; x >= 0; x--)
				{
					for (y = height - 1; y >= 0; y--)
					{
						xCurrent = Convert.ToInt32(x + (height - y) * jump);

						if (xCurrent >= 0 && xCurrent < width)
						{
							if (array[y, xCurrent] == -1)
								return new Point(xCurrent, y);
						}
						else
							break;
					}
				}
			}

			return new Point(width, height / 2);
		}
		#endregion

		#endregion

	}

}
