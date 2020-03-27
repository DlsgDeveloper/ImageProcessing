using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	public class ManualLevels
	{

		//	PUBLIC METHODS

		#region Get()
		public static void Get(Bitmap bitmap, byte minimum, byte maximum)
		{
			Get(bitmap, Rectangle.Empty, minimum, maximum);
		}

		public static void Get(Bitmap bitmap, Rectangle clip, byte minimum, byte maximum)
		{
			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			else
				clip.Intersect(new Rectangle(0, 0, bitmap.Width, bitmap.Height));

			try
			{
				if (maximum > minimum)
				{
					switch (bitmap.PixelFormat)
					{
						case PixelFormat.Format8bppIndexed:
							{
								if (Misc.IsPaletteGrayscale(bitmap.Palette.Entries))
									RunAlgorithm(bitmap, clip, minimum, maximum);
								else
									throw new IpException(ErrorCode.ErrorUnsupportedFormat);
							} break;
						case PixelFormat.Format24bppRgb:
						case PixelFormat.Format32bppArgb:
						case PixelFormat.Format32bppRgb:
							RunAlgorithm(bitmap, clip, minimum, maximum);
							break;
						case PixelFormat.Format1bppIndexed:
							break;
						default:
							throw new IpException(ErrorCode.ErrorUnsupportedFormat);
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("ManualLevels, Get(): " + ex.Message);
			}
		}
		#endregion

		//PRIVATE METHODS

		#region RunAlgorithm()
		private static void RunAlgorithm(Bitmap bitmap, Rectangle clip, byte minimum, byte maximum)
		{
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat);
				int stride = bitmapData.Stride;
				int jump = bitmap.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;
				float ratio = 256.0F / (maximum - minimum);

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					int clipWidth = clip.Width;
					int clipHeight = clip.Height;
					int x, y;
					double gray;
					int difference;

					if (bitmapData.PixelFormat != PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < clipHeight; y++)
						{
							pCurrent = pSource + (y * stride);

							for (x = 0; x < clipWidth; x++)
							{
								gray = (0.299 * pCurrent[2] + 0.587 * pCurrent[1] + 0.114 * pCurrent[0]);
								difference = (int)(((gray - minimum) * ratio) - gray);

								if (pCurrent[0] + difference < 0)
									pCurrent[0] = 0;
								else if (pCurrent[0] + difference > 255)
									pCurrent[0] = 255;
								else
									pCurrent[0] = (byte)(pCurrent[0] + difference);

								if (pCurrent[1] + difference < 0)
									pCurrent[1] = 0;
								else if (pCurrent[1] + difference > 255)
									pCurrent[1] = 255;
								else
									pCurrent[1] = (byte)(pCurrent[1] + difference);

								if (pCurrent[2] + difference < 0)
									pCurrent[2] = 0;
								else if (pCurrent[2] + difference > 255)
									pCurrent[2] = 255;
								else
									pCurrent[2] = (byte)(pCurrent[2] + difference);

								pCurrent += jump;
							}
						}
					}
					else
					{
						for (y = 0; y < clipHeight; y++)
						{
							pCurrent = pSource + (y * stride);

							for (x = 0; x < clipWidth; x++)
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
