using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.Miscelaneous
{
	class BitmapMisc
	{
		#region GetMinAndMax()
		internal static void GetMinAndMax(Bitmap bitmap, out byte min, out byte max)
		{
			BitmapData bitmapData = null;

			min = 255;
			max = 0;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				int stride = bitmapData.Stride;

				unsafe
				{
					byte*	pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte*	pCurrent;
					int		x, y;

					#region 24, 32 bpp
					if (bitmapData.PixelFormat == PixelFormat.Format24bppRgb || bitmapData.PixelFormat == PixelFormat.Format32bppArgb || bitmapData.PixelFormat == PixelFormat.Format32bppRgb)
					{
						int pixelBytes = (bitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
						double gray;

						for (y = 0; y < height; y++)
						{
							pCurrent = pSource + (y * stride);

							for (x = 0; x < width; x++)
							{
								gray = (0.299 * pCurrent[x * pixelBytes + 2] + 0.587 * pCurrent[x * pixelBytes + 1] + 0.114 * pCurrent[x * pixelBytes]);

								if (min > gray)
									min = (byte)gray;

								if (max < gray)
									max = (byte)gray;
							}

							if (min == 0 && max == 255)
								return;
						}
					}
					#endregion
					#region 8 bpp
					else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						if (ImageProcessing.Misc.IsGrayscale(bitmap))
						{
							byte gray;
							
							for (y = 0; y < height; y++)
							{
								pCurrent = pSource + (y * stride);

								for (x = 0; x < width; x++)
								{
									gray = pCurrent[x];

									if (min > gray)
										min = (byte)gray;

									if (max < gray)
										max = (byte)gray;
								}

								if (min == 0 && max == 255)
									return;
							}
						}
						else
						{
							Color[]		entries = bitmap.Palette.Entries;
							byte		gray;
							Color		color;

							for (y = 0; y < height; y++)
							{
								pCurrent = pSource + (y * stride);

								for (x = 0; x < width; x++)
								{
									color = entries[pCurrent[x]];
									gray = (byte)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);

									if (min > gray)
										min = (byte)gray;

									if (max < gray)
										max = (byte)gray;
								}

								if (min == 0 && max == 255)
									return;
							}
						}
					}
					#endregion

					#region 1 bpp
					else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						min = 0;
						max = 255;
					}
					#endregion
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
