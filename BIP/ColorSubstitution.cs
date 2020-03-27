using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	public class ColorSubstitution
	{
		//	PUBLIC METHODS
		#region public methods

		#region Go()
		public static void Go(Bitmap bitmap, byte[] redTable, byte[] greenTable, byte[] blueTable)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			try
			{

				switch (bitmap.PixelFormat)
				{
					/*case PixelFormat.Format8bppIndexed:
						{
							if (Misc.IsPaletteGrayscale(bitmap.Palette.Entries))
								Go8bppGrayscale(bitmap, clip, contrast, histogramMean);
							else
								throw new IpException(ErrorCode.ErrorUnsupportedFormat);
						} break;*/
					case PixelFormat.Format24bppRgb:
						Go24bpp(bitmap, redTable, greenTable, blueTable);
						break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("ColorSubstitution, Go(): " + ex.Message);
			}
			finally
			{
#if DEBUG
				Console.WriteLine("Contrast Go():" + (DateTime.Now.Subtract(start)).ToString());
#endif
			}
		}
		#endregion

		#endregion

	
		//PRIVATE METHODS
		#region private methods

		#region Go24bpp()
		private static void Go24bpp(Bitmap bitmap, byte[] redTable, byte[] greenTable, byte[] blueTable)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;

				bitmapData = bitmap.LockBits(new Rectangle(0,0,width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					int x, y;

					for (y = 0; y < height; y++)
					{
						pCurrent = pSource + (y * stride);

						for (x = 0; x < width; x++)
						{					
							pCurrent[x * 3] = blueTable[pCurrent[x * 3]];			//blue							
							pCurrent[x * 3 + 1] = greenTable[pCurrent[x * 3 + 1]];	//green
							pCurrent[x * 3 + 2] = redTable[pCurrent[x * 3 + 2]];	//red
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
