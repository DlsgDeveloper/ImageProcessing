using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace ImageProcessing
{
	public class AutoLevels
	{

		// PUBLIC METHODS
		#region public methods

		#region Get()
		/// <summary>
		/// It computes histogram's smallest and biggest RGB values and stretches the used pixel shades from 0 to 255. 
		/// Ignore parameters are used to ignore x percent of histogram pixels. Both ignore parameters start from 0 (Example 0.005, 0.02).
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="ignoreDarkPixelsPercent">Ignore parameters are used to ignore x percent of histogram pixels. Both ignore parameters start from 0 (Example 0.005, 0.02).</param>
		/// <param name="ignoreLightPixelsPercent">Ignore parameters are used to ignore x percent of histogram pixels. Both ignore parameters start from 0 (Example 0.005, 0.02).</param>
		public static void Get(Bitmap bitmap, double ignoreDarkPixelsPercent = 0, double ignoreLightPixelsPercent = 0)
		{
			try
			{
				switch (bitmap.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed:
						{
							if (Misc.IsPaletteGrayscale(bitmap.Palette.Entries))
								RunGray(bitmap, ignoreDarkPixelsPercent, ignoreLightPixelsPercent);
							else
								throw new IpException(ErrorCode.ErrorUnsupportedFormat);
						}
						break;
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
						RunColor(bitmap, ignoreDarkPixelsPercent, ignoreLightPixelsPercent);
						break;
					case PixelFormat.Format1bppIndexed:
						break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("AutoLevels, Get(): " + ex.Message);
			}
		}
		#endregion

		#region GetSingleChannel()
		/// <summary>
		/// It computes histogram's smallest and biggest RGB values and stretches the used pixel shades from 0 to 255. 
		/// Ignore parameters are used to ignore x percent of histogram pixels. Both ignore parameters start from 0 (Example 0.005, 0.02).
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="ignoreDarkPixelsPercent">Ignore parameters are used to ignore x percent of histogram pixels. Both ignore parameters start from 0 (Example 0.005, 0.02).</param>
		/// <param name="ignoreLightPixelsPercent">Ignore parameters are used to ignore x percent of histogram pixels. Both ignore parameters start from 0 (Example 0.005, 0.02).</param>
		public static void GetSingleChannel(Bitmap bitmap, double ignoreDarkPixelsPercent = 0, double ignoreLightPixelsPercent = 0)
		{
			try
			{
				switch (bitmap.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed:
						{
							if (Misc.IsPaletteGrayscale(bitmap.Palette.Entries))
								RunGray(bitmap, ignoreDarkPixelsPercent, ignoreLightPixelsPercent);
							else
								throw new IpException(ErrorCode.ErrorUnsupportedFormat);
						}
						break;
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
						RunColorSingleChannel(bitmap, ignoreDarkPixelsPercent, ignoreLightPixelsPercent);
						break;
					case PixelFormat.Format1bppIndexed:
						break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("AutoLevels, Get(): " + ex.Message);
			}
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region RunGray()
		private static void RunGray(Bitmap bitmap, double ignoreDarkPixelsPercent, double ignoreLightPixelsPercent)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;

				HistogramGrayscale histogram = new HistogramGrayscale(bitmap, Rectangle.Empty);

				GetHistogramMinAndMax(histogram.Array, ignoreDarkPixelsPercent, ignoreLightPixelsPercent, out int minimum, out int maximum);

				bitmapData = bitmap.LockBits(new Rectangle(0,0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
				int stride = bitmapData.Stride;
				float ratio = 256.0F / (maximum - minimum);

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					int x, y;
					double gray;

					for (y = 0; y < height; y++)
					{
						pCurrent = pSource + (y * stride);

						for (x = 0; x < width; x++)
						{
							gray = ((pCurrent[0] - minimum) * ratio);

							//*pCurrent = (byte)((gray < 0) ? 0 : ((gray > 255) ? 255 : gray));
							if (gray < 0)
								pCurrent[0] = 0;
							else if (gray > 255)
								pCurrent[0] = 255;
							else
								pCurrent[0] = (byte)(gray);

							pCurrent++;
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

		#region RunColor()
		private static void RunColor(Bitmap bitmap, double ignoreDarkPixelsPercent, double ignoreLightPixelsPercent)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;

				Histogram histogram = new Histogram(bitmap);

				GetHistogramMinAndMax(histogram.ArrayR, ignoreDarkPixelsPercent, ignoreLightPixelsPercent, out int minimumR, out int maximumR);
				GetHistogramMinAndMax(histogram.ArrayG, ignoreDarkPixelsPercent, ignoreLightPixelsPercent, out int minimumG, out int maximumG);
				GetHistogramMinAndMax(histogram.ArrayB, ignoreDarkPixelsPercent, ignoreLightPixelsPercent, out int minimumB, out int maximumB);

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
				int stride = bitmapData.Stride;
				int jump = bitmap.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;
				float ratioR = 256.0F / (maximumR - minimumR);
				float ratioG = 256.0F / (maximumG - minimumG);
				float ratioB = 256.0F / (maximumB - minimumB);

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					int x, y;
					double r, g, b;

					for (y = 0; y < height; y++)
					{
						pCurrent = pSource + (y * stride);

						for (x = 0; x < width; x++)
						{
							b = ((pCurrent[0] - minimumB) * ratioB);
							g = ((pCurrent[1] - minimumG) * ratioG);
							r = ((pCurrent[2] - minimumR) * ratioR);

							if (b < 0)
								pCurrent[0] = 0;
							else if (b > 255)
								pCurrent[0] = 255;
							else
								pCurrent[0] = (byte)b;

							if (g < 0)
								pCurrent[1] = 0;
							else if (g > 255)
								pCurrent[1] = 255;
							else
								pCurrent[1] = (byte)g;

							if (r < 0)
								pCurrent[2] = 0;
							else if (r > 255)
								pCurrent[2] = 255;
							else
								pCurrent[2] = (byte)r;

							pCurrent += jump;
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

		#region RunColorSingleChannel()
		private static void RunColorSingleChannel(Bitmap bitmap, double ignoreDarkPixelsPercent, double ignoreLightPixelsPercent)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;

				Histogram histogram = new Histogram(bitmap);

				GetHistogramMinAndMax(histogram, ignoreDarkPixelsPercent, ignoreLightPixelsPercent, out int minimum, out int maximum);

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
				int stride = bitmapData.Stride;
				int jump = bitmap.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;
				float ratio = 256.0F / (maximum - minimum);

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					int x, y;
					double r, g, b;

					for (y = 0; y < height; y++)
					{
						pCurrent = pSource + (y * stride);

						for (x = 0; x < width; x++)
						{
							b = ((pCurrent[0] - minimum) * ratio);
							g = ((pCurrent[1] - minimum) * ratio);
							r = ((pCurrent[2] - minimum) * ratio);

							if (b < 0)
								pCurrent[0] = 0;
							else if (b > 255)
								pCurrent[0] = 255;
							else
								pCurrent[0] = (byte)b;

							if (g < 0)
								pCurrent[1] = 0;
							else if (g > 255)
								pCurrent[1] = 255;
							else
								pCurrent[1] = (byte)g;

							if (r < 0)
								pCurrent[2] = 0;
							else if (r > 255)
								pCurrent[2] = 255;
							else
								pCurrent[2] = (byte)r;

							pCurrent += jump;
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

		#region GetHistogramMinAndMax()
		private static void GetHistogramMinAndMax(Histogram histogram, double ignoreDarkPixelsPercent, double ignoreLightPixelsPercent, out int min, out int max)
		{
			min = 0;
			max = 255;

			uint pixels = (uint)histogram.ArrayR.Sum(x => x);
			uint sum = 0;

			if (ignoreDarkPixelsPercent == 0)
			{
				for (int i = 0; i < 256; i++)
					if (histogram.ArrayR[i] > 0 || histogram.ArrayG[i] > 0 || histogram.ArrayB[i] > 0)
					{
						min = i;
						break;
					}
			}
			else
			{
				//jump over noise
				for (int i = 0; i < 256; i++)
				{
					sum += histogram.ArrayR[i] + histogram.ArrayG[i] + histogram.ArrayB[i];

					if (sum >= pixels * ignoreDarkPixelsPercent)
					{
						min = i;
						break;
					}
				}
			}

			sum = 0;

			if (ignoreLightPixelsPercent == 0)
			{
				for (int i = 255; i >= 0; i--)
					if (histogram.ArrayR[i] > 0 || histogram.ArrayG[i] > 0 || histogram.ArrayB[i] > 0)
					{
						max = i;
						break;
					}
			}
			else
			{
				for (int i = 255; i >= 0; i--)
				{
					sum += histogram.ArrayR[i] + histogram.ArrayG[i] + histogram.ArrayB[i];

					if (sum >= pixels * ignoreLightPixelsPercent)
					{
						max = i;
						break;
					}
				}
			}

			if(min >= max)
			{
				int middle = (min + max) / 2;

				min = Math.Max(0, middle - 1);
				max = Math.Min(255, middle + 1);
			}
		}

		private static void GetHistogramMinAndMax(uint[] array, double ignoreDarkPixelsPercent, double ignoreLightPixelsPercent, out int min, out int max)
		{
			min = 0;
			max = 255;

			uint pixels = 0;
			uint sum = 0;

			for (int i = 0; i < 256; i++)
				pixels += array[i];

			if (ignoreDarkPixelsPercent == 0)
			{
				for (int i = 0; i < 256; i++)
					if (array[i] > 0)
					{
						min = i;
						break;
					}
			}
			else
			{
				//jump over noise
				for (int i = 0; i < 256; i++)
				{
					sum += array[i];

					if (sum >= pixels * ignoreDarkPixelsPercent)
					{
						min = i;
						break;
					}
				}
			}

			sum = 0;

			if (ignoreLightPixelsPercent == 0)
			{
				for (int i = 255; i >= 0; i--)
					if (array[i] > 0)
					{
						max = i;
						break;
					}
			}
			else
			{
				for (int i = 255; i >= 0; i--)
				{
					sum += array[i];

					if (sum >= pixels * ignoreLightPixelsPercent)
					{
						max = i;
						break;
					}
				}
			}

			if (min >= max)
			{
				int middle = (min + max) / 2;

				min = Math.Max(0, middle - 1);
				max = Math.Min(255, middle + 1);
			}
		}
		#endregion

		#endregion

	}
}
