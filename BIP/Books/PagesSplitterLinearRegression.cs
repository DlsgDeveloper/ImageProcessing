using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing.BigImages;

namespace BIP.Books
{
	/// <summary>
	/// Ted's algorithm implementation.
	/// </summary>
	public class PagesSplitterLinearRegression
	{
		int _goodWeight = 10;

		//events
		public ImageProcessing.ProgressHnd		ProgressChanged;


		#region constructor
		public PagesSplitterLinearRegression()
		{
		}
		#endregion


		// PUBLIC METHODS
		#region public methods

		#region FindBookfoldLine()
		public SplitterLine FindBookfoldLine(FileInfo file, Rectangle clip, int rowsToSkip, int shortScanLength, int longScanLength)
		{
			using (Bitmap bitmap = new Bitmap(file.FullName))
			{				
				return FindBookfoldLine(bitmap, clip, rowsToSkip, shortScanLength, longScanLength);
			}
		}

		public SplitterLine FindBookfoldLine(Bitmap bitmap, Rectangle clip, int rowsToSkip, int shortScanLength, int longScanLength)
		{
			clip.Intersect(new Rectangle(1, 0, bitmap.Width - 2, bitmap.Height));

			List<RegressionPoint> regressionPoints = FindRegressionPoints(bitmap, clip, rowsToSkip, shortScanLength, longScanLength);

			LinearRegression linearRegression = new LinearRegression(regressionPoints);

#if DEBUG
			if (linearRegression.PointsCount > 0)
				DrawResult(bitmap, linearRegression.GetSplitterLine(bitmap.Width, bitmap.Height), 255, 0, 0);

			DrawPoints(bitmap, regressionPoints, 255, 0, 0);
#endif

			linearRegression.DeletePointsFurtherThan(100);
#if DEBUG
			if (linearRegression.PointsCount > 0)
				DrawResult(bitmap, linearRegression.GetSplitterLine(bitmap.Width, bitmap.Height), 0, 255, 0);

			DrawPoints(bitmap, regressionPoints, 0, 255, 0);
#endif
			linearRegression.DeletePointsFurtherThan(20);

#if DEBUG
			if (linearRegression.PointsCount > 0)
				DrawResult(bitmap, linearRegression.GetSplitterLine(bitmap.Width, bitmap.Height), 0, 0, 255);

			DrawPoints(bitmap, regressionPoints, 0, 0, 255);
#endif

			return linearRegression.GetSplitterLine(bitmap.Width, bitmap.Height);
		}
		#endregion

		#region DrawResult()
		public unsafe void DrawResult(Bitmap bitmap, SplitterLine line, byte r, byte g, byte b)
		{
			BitmapData bitmapData = null;

			int sourceW = bitmap.Width;
			int sourceH = bitmap.Height;

			int x, y;
			double xCurrent = line.PointTop.X;
			double xMove = (line.PointBottom.X - line.PointTop.X) / (double) sourceH;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

				int stride = bitmapData.Stride;
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();

				#region 24 or 32 bpp
				if (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format24bppRgb)
				{
					int pixelBytes = (bitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

					for (y = 0; y < sourceH; y++)
					{
						x = Convert.ToInt32(xCurrent);

						if (x >= 0 && x < sourceW)
						{
							pSource[y * stride + x * pixelBytes + 0] = b;
							pSource[y * stride + x * pixelBytes + 1] = g;
							pSource[y * stride + x * pixelBytes + 2] = r;
						}

						xCurrent += xMove;
					}
				}
				#endregion

				#region 8 bpp
				else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					if (ImageProcessing.Misc.IsGrayscale(bitmap))
					{
						for (y = 0; y < sourceH; y++)
						{
							x = Convert.ToInt32(xCurrent);

							if (x >= 1 && x < sourceW - 1)
							{
								pSource[y * stride + x - 1] = g;
								pSource[y * stride + x] = g;
								pSource[y * stride + x + 1] = g;
							}

							xCurrent += xMove;
						}
					}
					else
					{
						Color[] entries = bitmap.Palette.Entries;

						for (y = 0; y < sourceH; y++)
						{
							x = Convert.ToInt32(xCurrent);

							if (x >= 0 && x < sourceW)
								pSource[y * stride + x] = (byte)(entries.Length - 1);

							xCurrent += xMove;
						}
					}
				}
				#endregion

				#region 1 bpp
				/*else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					for (y = 0; y < sourceH; y++)
					{
						if ((y % 2) == 0)
						{
							pSource[y * stride + splitBasedOnVerticalDifferencesL / 8] &= 195;
							pSource[y * stride + splitBasedOnVerticalDifferencesL / 8] |= 36;

							pSource[y * stride + splitBasedOnVerticalDifferencesR / 8] &= 195;
							pSource[y * stride + splitBasedOnVerticalDifferencesR / 8] |= 36;
						}

						pSource[y * stride + splitBasedOnVerticalDifferences / 8] &= 195;
						pSource[y * stride + splitBasedOnVerticalDifferences / 8] |= 36;
					}
				}*/
				#endregion

			}
			finally
			{
				if ((bitmap != null) && (bitmapData != null))
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region DrawPoints()
		public unsafe void DrawPoints(Bitmap bitmap, List<RegressionPoint> regressionPoints, byte r, byte g, byte b)
		{
			BitmapData bitmapData = null;

			int sourceW = bitmap.Width;
			int sourceH = bitmap.Height;

			int x, y;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

				int stride = bitmapData.Stride;
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();


				#region 24 or 32 bpp
				if (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format24bppRgb)
				{
					int pixelBytes = (bitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

					foreach (RegressionPoint p in regressionPoints)
					{
						int pointSize = Math.Max(1, Convert.ToInt32(p.Confidence / 20));

						for (y = Math.Max(p.Y - pointSize, 0); y <= Math.Min(sourceH - 1, p.Y + pointSize); y++)
						{
							for (x = Math.Max(0, p.X - pointSize); x <= Math.Min(sourceW, p.X + pointSize); x++)
							{
									pSource[y * stride + x * pixelBytes + 0] = b;
									pSource[y * stride + x * pixelBytes + 1] = g;
									pSource[y * stride + x * pixelBytes + 2] = r;
							}
						}
					}
				}
				#endregion

				#region 8 bpp
				else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					foreach (RegressionPoint p in regressionPoints)
					{
						int pointSize = Math.Max(1, Convert.ToInt32(p.Confidence));

						for (y = Math.Max(p.Y - pointSize, 0); y <= Math.Min(sourceH - 1, p.Y + pointSize); y++)
						{
							for (x = Math.Max(0, p.X - pointSize); x <= Math.Min(sourceW, p.X + pointSize); x++)
							{
								pSource[y * stride + x] = g;
							}
						}
					}
				}
				#endregion

				#region 1 bpp
				/*else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					for (y = 0; y < sourceH; y++)
					{
						if ((y % 2) == 0)
						{
							pSource[y * stride + splitBasedOnVerticalDifferencesL / 8] &= 195;
							pSource[y * stride + splitBasedOnVerticalDifferencesL / 8] |= 36;

							pSource[y * stride + splitBasedOnVerticalDifferencesR / 8] &= 195;
							pSource[y * stride + splitBasedOnVerticalDifferencesR / 8] |= 36;
						}

						pSource[y * stride + splitBasedOnVerticalDifferences / 8] &= 195;
						pSource[y * stride + splitBasedOnVerticalDifferences / 8] |= 36;
					}
				}*/
				#endregion

			}
			finally
			{
				if ((bitmap != null) && (bitmapData != null))
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#endregion


		// PRIVATE METHODS
		#region private methods

		#region FindRegressionPoints()
		unsafe List<RegressionPoint> FindRegressionPoints(Bitmap bitmap, Rectangle clip, int rowsToSkip, int shortScanLength, int longScanLength)
		{
			BitmapData bitmapData = null;
			List<RegressionPoint> regressionPoints = new List<RegressionPoint>();

			int width = clip.Width;
			int height = clip.Height;

			int x, row, p;
			double gray1, gray2, gray3, gray4, weight;

			_goodWeight = shortScanLength;

			try
			{
				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadOnly, bitmap.PixelFormat);

				int			stride = bitmapData.Stride;
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer();

				#region 24 or 32 bpp
				if (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format24bppRgb)
				{
					int pixelBytes = (bitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

					for(row = 0; row < height; row += rowsToSkip)
					{
						RegressionPoint newRecessionPoint = null;

						for(x = shortScanLength; x < width - shortScanLength; x++)
						{
							weight = 0;

							for(p = 1; p <= shortScanLength; p++)
							{
								gray1 = (pSource[(row * stride) + (x - p) * pixelBytes + 2] + pSource[(row * stride) + (x - p) * pixelBytes + 1] + pSource[(row * stride) + (x - p) * pixelBytes]);
								gray2 = (pSource[(row * stride) + (x - p + 1) * pixelBytes + 2] + pSource[(row * stride) + (x - p + 1) * pixelBytes + 1] + pSource[(row * stride) + (x - p + 1) * pixelBytes]);

								if (gray1 > gray2)
									weight += 2;
								else if (gray1 < gray2)
									weight -= 3;
								else
									weight += 1;

								gray3 = (pSource[(row * stride) + (x + p - 1) * pixelBytes + 2] + pSource[(row * stride) + (x + p - 1) * pixelBytes + 1] + pSource[(row * stride) + (x + p - 1) * pixelBytes]);
								gray4 = (pSource[(row * stride) + (x + p) * pixelBytes + 2] + pSource[(row * stride) + (x + p) * pixelBytes + 1] + pSource[(row * stride) + (x + p) * pixelBytes]);

								if (gray4 > gray3)
									weight += 2;
								else if (gray4 < gray3)
									weight -= 3;
								else
									weight++;
							}

							if(weight > _goodWeight )
							{
								double pointWeight = Math.Pow(1.15, weight);

								if (newRecessionPoint == null || newRecessionPoint.Confidence < pointWeight)
								{
									newRecessionPoint = new RegressionPoint(clip.X + x, clip.Y + row, pointWeight);
								}
							}
						}

						if (newRecessionPoint != null)
							regressionPoints.Add(newRecessionPoint);
					}
				}
				#endregion

				#region 8 bpp
				else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					if (ImageProcessing.Misc.IsGrayscale(bitmap))
					{
						for (row = 0; row < height; row++)
						{
							RegressionPoint newRecessionPoint = null;

							for (x = shortScanLength; x < width - shortScanLength; x++)
							{
								weight = 0;

								for (p = 1; p <= shortScanLength; p++)
								{
									gray1 = pSource[(row * stride) + (x - p)];
									gray2 = pSource[(row * stride) + (x - p + 1)];

									if (gray1 > gray2)
										weight++;
									else if (gray1 < gray2)
										weight--;

									gray3 = pSource[(row * stride) + (x + p - 1)];
									gray4 = pSource[(row * stride) + (x + p)];

									if (gray4 > gray3)
										weight++;
									else if (gray4 < gray3)
										weight--;
								}

								if (weight > _goodWeight)
								{
									double pointWeight = Math.Pow(1.15, weight);

									if (newRecessionPoint == null || newRecessionPoint.Confidence < pointWeight)
									{
										newRecessionPoint = new RegressionPoint(clip.X + x, clip.Y + row, pointWeight);
									}
								}
							}

							if (newRecessionPoint != null)
								regressionPoints.Add(newRecessionPoint);
						}
					}
					else
					{
						Color[] entries = bitmap.Palette.Entries;
						Color c;

						for (row = 0; row < height; row++)
						{
							RegressionPoint newRecessionPoint = null;

							for (x = shortScanLength; x < width - shortScanLength; x++)
							{
								weight = 0;

								for (p = 1; p <= shortScanLength; p++)
								{
									c = entries[pSource[(row * stride) + (x - p)]];
									gray1 = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
									c = entries[pSource[(row * stride) + (x - p + 1)]];
									gray2 = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B);

									if (gray1 > gray2)
										weight++;
									else if (gray1 < gray2)
										weight--;

									c = entries[pSource[(row * stride) + (x + p - 1)]];
									gray3 = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
									c = entries[pSource[(row * stride) + (x + p)]];
									gray4 = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B);

									if (gray4 > gray3)
										weight++;
									else if (gray4 < gray3)
										weight--;
								}

								if (weight > _goodWeight)
								{
									double pointWeight = Math.Pow(1.15, weight);

									if (newRecessionPoint == null || newRecessionPoint.Confidence < pointWeight)
									{
										newRecessionPoint = new RegressionPoint(clip.X + x, clip.Y + row, pointWeight);
									}
								}
							}

							if (newRecessionPoint != null)
								regressionPoints.Add(newRecessionPoint);



							for (x = shortScanLength; x < width - shortScanLength; x++)
							{
								weight = 0;

								for (p = 1; p <= shortScanLength; p++)
								{
									c = entries[pSource[(row * stride) + (x - p)]];
									gray1 = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
									c = entries[pSource[(row * stride) + (x)]];
									gray2 = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
									c = entries[pSource[(row * stride) + (x + p)]];
									gray3 = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B);

									if (gray1 > gray2)
										weight++;
									else if (gray1 < gray2)
										weight--;

									if (gray3 > gray2)
										weight++;
									else if (gray3 < gray2)
										weight--;
								}

								if (weight > _goodWeight)
								{
									double pointWeight = Math.Pow(1.15, weight);

									regressionPoints.Add(new RegressionPoint(clip.X + x, clip.Y + row, pointWeight));
								}
							}
						}
					}
				}
				#endregion

				#region 1 bpp
				/*else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					byte g;

					try
					{
						for (y = 0; y < sourceH; y++)
							for (x = 0; x < sourceW; x++)
							{
								g = pSource[y * stride + x / 8];

								for (int i = 0; i < 8; i++)
								{
									if (((g >> i) & 0x1) == 1)
										columnColors[x / pixelsPerStripe, 255]++;
									else
										columnColors[x / pixelsPerStripe, 0]++;
								}
							}
					}
					catch (Exception ex)
					{
						throw ex;
					}
				}*/
				#endregion

			}
			finally
			{
				if ((bitmap != null) && (bitmapData != null))
					bitmap.UnlockBits(bitmapData);
			}

			return regressionPoints;
		}
		#endregion

		#endregion

	}
}
