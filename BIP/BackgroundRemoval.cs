using ImageProcessing.Languages;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for BackgroundRemoval.
	/// </summary>
	public class BackgroundRemoval
	{
		#region constructor
		private BackgroundRemoval()
		{
		}
		#endregion

		//	PUBLIC METHODS				
		#region Go()
		public static void Go(Bitmap bitmap)
		{
			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format8bppIndexed: Go8bpp(bitmap); break;
				case PixelFormat.Format24bppRgb:
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:
					GoColor(bitmap);
					break;
				default: throw new Exception(BIPStrings.UnsupportedImageFormat_STR);
			}
		}
		#endregion

		//PRIVATE METHODS

		#region GoColor()
		private static void GoColor(Bitmap bitmap)
		{
			int x, y;
			int width = bitmap.Width;
			int height = bitmap.Height;

			BitmapData bitmapData = null;

			Histogram h = new Histogram(bitmap);
			Color background = h.GetOtsuBackground();
			int backR = (int)Math.Max(100, (int)background.R - 00);
			int backG = (int)Math.Max(100, (int)background.G - 00);
			int backB = (int)Math.Max(100, (int)background.B - 00);
			int thresR = h.ThresholdR;
			int thresG = h.ThresholdG;
			int thresB = h.ThresholdB;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < height; y++)
						{
							pCurrent = pSource + y * stride;

							for (x = 0; x < width; x++)
							{
								if (pCurrent[0] > backR)
									pCurrent[0] = (byte)(backR + (pCurrent[0] - backR) / 4);
								else if (pCurrent[0] > thresR)
									pCurrent[0] = (byte)(backR - (backR - thresR) * (((backR - pCurrent[0]) * (backR - pCurrent[0])) / (double)((backR - thresR) * (backR - thresR))));
								
								if (pCurrent[1] > backG)
									pCurrent[1] = (byte)(backG + (pCurrent[1] - backG) / 4);
								else if (pCurrent[1] > thresG)
									pCurrent[1] = (byte)(backG - (backG - thresG) * (((backG - pCurrent[1]) * (backG - pCurrent[1])) / (double)((backG - thresG) * (backG - thresG))));
								
								if (pCurrent[2] > backB)
									pCurrent[2] = (byte)(backB + (pCurrent[2] - backB) / 4);
								else if (pCurrent[2] > thresB)
									pCurrent[2] = (byte)(backB - (backB - thresB) * (((backB - pCurrent[2]) * (backB - pCurrent[2])) / (double)((backB - thresB) * (backB - thresB))));

								pCurrent += 3;
							}
						}
					}
					else
					{
						for (y = 0; y < height; y++)
						{
							pCurrent = pSource + y * stride;

							for (x = 0; x < width; x++)
							{
								if (pCurrent[0] > backR)
									pCurrent[0] = (byte)(backR + (pCurrent[0] - backR) / 4);
								else if (pCurrent[0] > thresR)
									pCurrent[0] = (byte)(backR - (backR - thresR) * (((backR - pCurrent[0]) * (backR - pCurrent[0])) / (double)((backR - thresR) * (backR - thresR))));

								if (pCurrent[1] > backG)
									pCurrent[1] = (byte)(backG + (pCurrent[1] - backG) / 4);
								else if (pCurrent[1] > thresG)
									pCurrent[1] = (byte)(backG - (backG - thresG) * (((backG - pCurrent[1]) * (backG - pCurrent[1])) / (double)((backG - thresG) * (backG - thresG))));

								if (pCurrent[2] > backB)
									pCurrent[2] = (byte)(backB + (pCurrent[2] - backB) / 4);
								else if (pCurrent[2] > thresB)
									pCurrent[2] = (byte)(backB - (backB - thresB) * (((backB - pCurrent[2]) * (backB - pCurrent[2])) / (double)((backB - thresB) * (backB - thresB))));

								pCurrent += 4;
							}
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

		#region Go8bpp()
		private static void Go8bpp(Bitmap bitmap)
		{
			int x, y;
			int width = bitmap.Width;
			int height = bitmap.Height;

			BitmapData bitmapData = null;

			Histogram h = new Histogram(bitmap);
			Color background = h.GetOtsuBackground();
			int back = (int)Math.Max(100, (int)background.R - 0);
			int thres = h.ThresholdR;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					for (y = 0; y < height; y++)
					{
						pCurrent = pSource + y * stride;

						for (x = 0; x < width; x++)
						{
							if (pCurrent[x] > back)
								pCurrent[x] = (byte)(back + (pCurrent[x] - back) / 4);
							else if (pCurrent[x] > thres)
								pCurrent[x] = (byte)(back - (back - thres) * (((back - pCurrent[x]) * (back - pCurrent[x])) / (double)((back - thres) * (back - thres))));
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

	}
}
