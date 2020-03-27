using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class BrightnessContrast
	{
		//	PUBLIC METHODS
		#region public methods

		#region Go()
		/// <summary>
		/// histogram mean is computed
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="brightness"></param>
		/// <param name="contrast"></param>
		public static void Go(Bitmap bitmap, double brightness, double contrast)
		{
			ImageProcessing.ColorD histogramMean = Histogram.GetHistogramMean(bitmap);

			Go(bitmap, brightness, contrast, histogramMean);
		}

		public static void Go(Bitmap bitmap, double brightness, double contrast, ColorD histogramMean)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			try
			{
				switch (bitmap.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed:
						{
							if (Misc.IsPaletteGrayscale(bitmap.Palette.Entries))
								Go8bppGrayscale(bitmap, brightness, contrast, histogramMean);
							else
								throw new IpException(ErrorCode.ErrorUnsupportedFormat);
						} break;
					case PixelFormat.Format24bppRgb:
						Go24bpp(bitmap, brightness, contrast, histogramMean);
						break;
					case PixelFormat.Format32bppRgb:
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppPArgb:
						Go32bpp(bitmap, brightness, contrast, histogramMean);
						break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("BrightnessContrast, Go(): " + ex.Message);
			}
			finally
			{
#if DEBUG
				Console.WriteLine("BrightnessContrast Go():" + (DateTime.Now.Subtract(start)).ToString());
#endif
			}
		}
		#endregion

		#region GetBitmap()
		/// <summary>
		/// histogram mean is computed
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="brightness"></param>
		/// <param name="contrast"></param>
		/// <returns></returns>
		public static Bitmap GetBitmap(Bitmap bitmap, double brightness, double contrast)
		{
			ImageProcessing.ColorD histogramMean = Histogram.GetHistogramMean(bitmap);

			return GetBitmap(bitmap, brightness, contrast, histogramMean);
		}

		public static Bitmap GetBitmap(Bitmap bitmap, double brightness, double contrast, ColorD histogramMean)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			try
			{
				switch (bitmap.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed:
						{
							if (Misc.IsPaletteGrayscale(bitmap.Palette.Entries))
								return Get8bppGrayscale(bitmap, brightness, contrast, histogramMean);
							else
								throw new IpException(ErrorCode.ErrorUnsupportedFormat);
						}
					case PixelFormat.Format24bppRgb:
						return Get24bpp(bitmap, brightness, contrast, histogramMean);
					case PixelFormat.Format32bppRgb:
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppPArgb:
						return Get32bpp(bitmap, brightness, contrast, histogramMean);
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("BrightnessContrast, GetBitmap(): " + ex.Message);
			}
			finally
			{
#if DEBUG
				Console.WriteLine("BrightnessContrast GetBitmap():" + (DateTime.Now.Subtract(start)).ToString());
#endif
			}
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region Get32bpp()
		private static Bitmap Get32bpp(Bitmap bitmap, double brightness, double contrast, ColorD histogramMean)
		{
			Bitmap result = null;
			BitmapData bitmapData = null;
			BitmapData resultData = null;

			try
			{
				double meanR = histogramMean.Red;
				double meanG = histogramMean.Green;
				double meanB = histogramMean.Blue;

				int b = (int)(brightness * 255);

				int width = bitmap.Width;
				int height = bitmap.Height;

				contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
				contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

				result = new Bitmap(width, height, bitmap.PixelFormat);
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

				int strideS = bitmapData.Stride;
				int strideR = resultData.Stride;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentR;

					int x, y;
					double color;

					for (y = 0; y < height; y++)
					{
						pCurrentS = pSource + (y * strideS);
						pCurrentR = pResult + (y * strideR);

						for (x = 0; x < width; x++)
						{
							//blue
							color = (((pCurrentS[x * 4] + b) - meanB) * contrast) + meanB;

							if (color <= 0)
								pCurrentR[x * 4] = 0;
							else if (color >= 255)
								pCurrentR[x * 4] = 255;
							else
								pCurrentR[x * 4] = (byte)(color);

							//green
							color = (((pCurrentS[x * 4 + 1] + b) - meanG) * contrast) + meanG;

							if (color <= 0)
								pCurrentR[x * 4 + 1] = (byte)0;
							else if (color >= 255)
								pCurrentR[x * 4 + 1] = (byte)255;
							else
								pCurrentR[x * 4 + 1] = (byte)(color);

							//red
							color = (((pCurrentS[x * 4 + 2] + b) - meanR) * contrast) + meanR;

							if (color <= 0)
								pCurrentR[x * 4 + 2] = (byte)0;
							else if (color >= 255)
								pCurrentR[x * 4 + 2] = (byte)255;
							else
								pCurrentR[x * 4 + 2] = (byte)(color);

							//alpha
							pCurrentR[x * 4 + 3] = pCurrentS[x * 4 + 3];
						}
					}
				}

				return result;
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region Go32bpp()
		private static void Go32bpp(Bitmap bitmap, double brightness, double contrast, ColorD histogramMean)
		{
			BitmapData bitmapData = null;

			try
			{
				double meanR = histogramMean.Red;
				double meanG = histogramMean.Green;
				double meanB = histogramMean.Blue;

				int b = (int)(brightness * 255);

				int width = bitmap.Width;
				int height = bitmap.Height;

				contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
				contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int strideS = bitmapData.Stride;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrentS;

					int x, y;
					double color;

					for (y = 0; y < height; y++)
					{
						pCurrentS = pSource + (y * strideS);

						for (x = 0; x < width; x++)
						{
							//blue
							color = (((pCurrentS[x * 4] + b) - meanB) * contrast) + meanB;

							if (color <= 0)
								pCurrentS[x * 4] = 0;
							else if (color >= 255)
								pCurrentS[x * 4] = 255;
							else
								pCurrentS[x * 4] = (byte)(color);

							//green
							color = (((pCurrentS[x * 4 + 1] + b) - meanG) * contrast) + meanG;

							if (color <= 0)
								pCurrentS[x * 4 + 1] = (byte)0;
							else if (color >= 255)
								pCurrentS[x * 4 + 1] = (byte)255;
							else
								pCurrentS[x * 4 + 1] = (byte)(color);

							//red
							color = (((pCurrentS[x * 4 + 2] + b) - meanR) * contrast) + meanR;

							if (color <= 0)
								pCurrentS[x * 4 + 2] = (byte)0;
							else if (color >= 255)
								pCurrentS[x * 4 + 2] = (byte)255;
							else
								pCurrentS[x * 4 + 2] = (byte)(color);
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region Get24bpp()
		private static Bitmap Get24bpp(Bitmap bitmap, double brightness, double contrast, ColorD histogramMean)
		{
			Bitmap result = null;
			BitmapData bitmapData = null;
			BitmapData resultData = null;

			try
			{
				double meanR = histogramMean.Red;
				double meanG = histogramMean.Green;
				double meanB = histogramMean.Blue;

				int b = (int)(brightness * 255);

				int width = bitmap.Width;
				int height = bitmap.Height;

				contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
				contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

				result = new Bitmap(width, height, bitmap.PixelFormat);
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

				int strideS = bitmapData.Stride;
				int strideR = resultData.Stride;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentR;

					int x, y;
					double color;

					for (y = 0; y < height; y++)
					{
						pCurrentS = pSource + (y * strideS);
						pCurrentR = pResult + (y * strideR);

						for (x = 0; x < width; x++)
						{
							//blue
							color = (((pCurrentS[x * 3] + b) - meanB) * contrast) + meanB;

							if (color <= 0)
								pCurrentR[x * 3] = 0;
							else if (color >= 255)
								pCurrentR[x * 3] = 255;
							else
								pCurrentR[x * 3] = (byte)(color);

							//green
							color = (((pCurrentS[x * 3 + 1] + b) - meanG) * contrast) + meanG;

							if (color <= 0)
								pCurrentR[x * 3 + 1] = (byte)0;
							else if (color >= 255)
								pCurrentR[x * 3 + 1] = (byte)255;
							else
								pCurrentR[x * 3 + 1] = (byte)(color);

							//red
							color = (((pCurrentS[x * 3 + 2] + b) - meanR) * contrast) + meanR;

							if (color <= 0)
								pCurrentR[x * 3 + 2] = (byte)0;
							else if (color >= 255)
								pCurrentR[x * 3 + 2] = (byte)255;
							else
								pCurrentR[x * 3 + 2] = (byte)(color);
						}
					}
				}

				return result;
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region Go24bpp()
		private static void Go24bpp(Bitmap bitmap, double brightness, double contrast, ColorD histogramMean)
		{
			BitmapData bitmapData = null;

			try
			{
				double meanR = histogramMean.Red;
				double meanG = histogramMean.Green;
				double meanB = histogramMean.Blue;

				int b = (int)(brightness * 255);

				int width = bitmap.Width;
				int height = bitmap.Height;

				contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
				contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int strideS = bitmapData.Stride;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrentS;

					int x, y;
					double color;

					for (y = 0; y < height; y++)
					{
						pCurrentS = pSource + (y * strideS);

						for (x = 0; x < width; x++)
						{
							//blue
							color = (((pCurrentS[x * 3] + b) - meanB) * contrast) + meanB;

							if (color <= 0)
								pCurrentS[x * 3] = 0;
							else if (color >= 255)
								pCurrentS[x * 3] = 255;
							else
								pCurrentS[x * 3] = (byte)(color);

							//green
							color = (((pCurrentS[x * 3 + 1] + b) - meanG) * contrast) + meanG;

							if (color <= 0)
								pCurrentS[x * 3 + 1] = (byte)0;
							else if (color >= 255)
								pCurrentS[x * 3 + 1] = (byte)255;
							else
								pCurrentS[x * 3 + 1] = (byte)(color);

							//red
							color = (((pCurrentS[x * 3 + 2] + b) - meanR) * contrast) + meanR;

							if (color <= 0)
								pCurrentS[x * 3 + 2] = (byte)0;
							else if (color >= 255)
								pCurrentS[x * 3 + 2] = (byte)255;
							else
								pCurrentS[x * 3 + 2] = (byte)(color);
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region Get8bppGrayscale()
		private static Bitmap Get8bppGrayscale(Bitmap bitmap, double brightness, double contrast, ColorD histogramMean)
		{
			Bitmap result = null;
			BitmapData bitmapData = null;
			BitmapData resultData = null;

			try
			{
				double mean = histogramMean.Red;

				int b = (int)(brightness * 255);

				int width = bitmap.Width;
				int height = bitmap.Height;

				contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
				contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

				result = new Bitmap(width, height, bitmap.PixelFormat);
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

				int strideS = bitmapData.Stride;
				int strideR = resultData.Stride;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentR;

					int x, y;
					double color;

					for (y = 0; y < height; y++)
					{
						pCurrentS = pSource + (y * strideS);
						pCurrentR = pResult + (y * strideR);

						for (x = 0; x < width; x++)
						{
							color = (((pCurrentS[x] + b) - mean) * contrast) + mean;

							if (color <= 0)
								pCurrentR[x] = 0;
							else if (color >= 255)
								pCurrentR[x] = 255;
							else
								pCurrentR[x] = (byte)(color);
						}
					}
				}

				return result;
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region Go8bppGrayscale()
		private static void Go8bppGrayscale(Bitmap bitmap, double brightness, double contrast, ColorD histogramMean)
		{
			BitmapData bitmapData = null;

			try
			{
				double mean = histogramMean.Red;

				int b = (int)(brightness * 255);

				int width = bitmap.Width;
				int height = bitmap.Height;

				contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
				contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int strideS = bitmapData.Stride;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrentS;

					int x, y;
					double color;

					for (y = 0; y < height; y++)
					{
						pCurrentS = pSource + (y * strideS);

						for (x = 0; x < width; x++)
						{
							color = (((pCurrentS[x] + b) - mean) * contrast) + mean;

							if (color <= 0)
								pCurrentS[x] = 0;
							else if (color >= 255)
								pCurrentS[x] = 255;
							else
								pCurrentS[x] = (byte)(color);
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#endregion

	}
}
