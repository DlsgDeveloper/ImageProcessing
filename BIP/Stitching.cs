using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections ;
using ImageProcessing.PageObjects;
using System.Collections.Generic;
using ImageProcessing.Languages;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Stitching.
	/// </summary>
	public class Stitching
	{		
		//PUBLIC METHODS
		#region public methods

		#region Go()
		public static Bitmap Go(Bitmap bitmap1, Bitmap bitmap2)
		{
			if (bitmap1 == null || bitmap2 == null)
				throw new Exception("Bitmap is null!");

			if (bitmap1.Width < bitmap1.HorizontalResolution * 2 || bitmap2.Width < bitmap2.HorizontalResolution * 2)
				throw new Exception(BIPStrings.BitmapMustBeAtLeast2InchesWide_STR);

			Bitmap result = null;

			switch (bitmap1.PixelFormat)
			{
				case PixelFormat.Format24bppRgb :
					result = Go24bpp(bitmap1, bitmap2);
					break ;
				/*case PixelFormat.Format8bppIndexed :
					if (Misc.IsGrayscale(bitmap))
						Erase8bpp(bitmap, imageClip, fingerClip);
					else
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
					break;
				case PixelFormat.Format1bppIndexed :
					Erase1bpp(bitmap, fingerClip);
					break ;*/
				default :
					throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
			}

			return result;
		}
		#endregion		
		
		#endregion

	
		//	PRIVATE METHODS
		#region private methods

		#region Go24bpp()
		private static Bitmap Go24bpp(Bitmap bitmap1, Bitmap bitmap2)
		{
			int dpi = Convert.ToInt32(bitmap1.HorizontalResolution);
			int jump = 16;
			double minStitchingValue = double.MaxValue;
			int xMin = 0, yMin = 0;
			BitmapData bmpData1 = null, bmpData2 = null;
			int x, y;

#if DEBUG 
			int totalComparisons = ((dpi / jump + 1) * (dpi / jump +1));
#endif

			try
			{
				bmpData1 = bitmap1.LockBits(new Rectangle(bitmap1.Width - 2 * dpi, 0, 2 * dpi, bitmap1.Height), ImageLockMode.ReadOnly, bitmap1.PixelFormat);
				bmpData2 = bitmap2.LockBits(new Rectangle(0, 0, 2 * dpi, bitmap2.Height), ImageLockMode.ReadOnly, bitmap2.PixelFormat);

				for (y = -dpi / 2; y <= dpi / 2; y = y + jump)
				{
					for (x = dpi / 2; x <= dpi + dpi / 2; x = x + jump)
					{
						double val = GetStitchingValue24bpp(bmpData1, bmpData2, x, y);

						if (minStitchingValue > val)
						{
							minStitchingValue = val;
							xMin = x;
							yMin = y;
						}
#if DEBUG
						totalComparisons--;
						Console.WriteLine(totalComparisons.ToString());
#endif
					}
				}

				jump = 8;
				do
				{
					while (BetterSpotFound24bpp(bmpData1, bmpData2, jump, ref minStitchingValue, ref xMin, ref yMin))
					{
					}

					jump = jump / 2;
				} while (jump > 0);
			}
			finally
			{
				if (bmpData1 != null)
					bitmap1.UnlockBits(bmpData1);
				if (bmpData2 != null)
					bitmap2.UnlockBits(bmpData2);
			}

			int sharedWidth = 2 * dpi - xMin;
			Rectangle r1 = Rectangle.FromLTRB(0, -yMin, bitmap1.Width, bitmap1.Height);
			Rectangle r2 = Rectangle.FromLTRB(0, yMin, bitmap2.Width, bitmap2.Height);

			r1.Intersect(new Rectangle(0, 0, bitmap1.Width,bitmap1.Height));
			r2.Intersect(new Rectangle(0, 0, bitmap2.Width,bitmap2.Height));
			Bitmap merge;

			try
			{
				bmpData1 = bitmap1.LockBits(r1, ImageLockMode.ReadOnly, bitmap1.PixelFormat);
				bmpData2 = bitmap2.LockBits(r2, ImageLockMode.ReadOnly, bitmap2.PixelFormat);

				merge = Merge24bpp(bmpData1, bmpData2, sharedWidth);
			}
			finally
			{
				if (bmpData1 != null)
					bitmap1.UnlockBits(bmpData1);
				if (bmpData2 != null)
					bitmap2.UnlockBits(bmpData2);
			}

			return merge;
		}
		#endregion

		#region BetterSpotFound24bpp()
		private static bool BetterSpotFound24bpp(BitmapData bmpData1, BitmapData bmpData2, int jump, ref double minStitchingValue, ref int xMin, ref int yMin)
		{
			int x, y;

			bool betterSpotFound = false;

			for (y = -jump; y <= jump; y = y + jump)
			{
				for (x = -jump; x <= jump; x = x + jump)
				{
					double val = GetStitchingValue24bpp(bmpData1, bmpData2, xMin + x, yMin + y);

					if (minStitchingValue > val)
					{
						minStitchingValue = val;
						xMin = xMin + x;
						yMin = yMin + y;
						betterSpotFound = true;
					}
				}
			}

			return betterSpotFound;
		}
		#endregion
	
		#region GetStitchingValue24bpp()
		private static double GetStitchingValue24bpp(BitmapData bmpData1, BitmapData bmpData2, int dx, int dy)
		{
			int jump = 4;
			int y1 = (dy < 0) ? -dy : 0;
			int y2 = (dy > 0) ? dy : 0;
			int h1 = bmpData1.Height - y1;
			int h2 = bmpData2.Height - y2;

			int width = bmpData1.Width - dx;
			int height = h1 < h2 ? h1 : h2;
			int stride1 = bmpData1.Stride;
			int stride2 = bmpData2.Stride;
			int deviation = 0;

			unsafe
			{
				byte* pOrig1 = (byte*)bmpData1.Scan0.ToPointer();
				byte* pOrig2 = (byte*)bmpData2.Scan0.ToPointer();
				byte* pCurrent1, pCurrent2;
				int x, y;

				for (y = 0; y < height; y = y + jump)
				{
					pCurrent1 = pOrig1 + (y + y1) * stride1 + dx * 3;
					pCurrent2 = pOrig2 + (y + y2) * stride2;

					for (x = 0; x < width; x = x + jump)
					{
						deviation += (pCurrent1[0] - pCurrent2[0] >= 0) ? pCurrent1[0] - pCurrent2[0] : pCurrent2[0] - pCurrent1[0];
						deviation += (pCurrent1[1] - pCurrent2[1] >= 0) ? pCurrent1[1] - pCurrent2[1] : pCurrent2[1] - pCurrent1[1];
						deviation += (pCurrent1[2] - pCurrent2[2] >= 0) ? pCurrent1[2] - pCurrent2[2] : pCurrent2[2] - pCurrent1[2];

						pCurrent1 += jump * 3;
						pCurrent2 += jump * 3;
					}
				}
			}

			return deviation / (double)((width / jump) * (height / jump));
		}
		#endregion

		#region Merge24bpp()
		private static Bitmap Merge24bpp(BitmapData bmpData1, BitmapData bmpData2, int sharedWidth)
		{
			int height = bmpData1.Height < bmpData2.Height ? bmpData1.Height : bmpData2.Height;
			int width = bmpData1.Width + bmpData2.Width - sharedWidth;

			Bitmap bitmap = new Bitmap(width, height, bmpData1.PixelFormat);
			BitmapData bmpData = null;

			try
			{
				bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

				int stride = bmpData.Stride;
				int stride1 = bmpData1.Stride;
				int stride2 = bmpData2.Stride;

				unsafe
				{
					byte* pOrig = (byte*)bmpData.Scan0.ToPointer();
					byte* pOrig1 = (byte*)bmpData1.Scan0.ToPointer();
					byte* pOrig2 = (byte*)bmpData2.Scan0.ToPointer();
					byte* pCurrent, pCurrent1, pCurrent2;

					for (int y = 0; y < height; y++)
					{
						pCurrent = pOrig + y * stride;
						pCurrent1 = pOrig1 + y * stride1;

						for (int x = 0; x < bmpData1.Width - sharedWidth; x++)
						{
							*(pCurrent++) = *(pCurrent1++);
							*(pCurrent++) = *(pCurrent1++);
							*(pCurrent++) = *(pCurrent1++);
						}
					}
					for (int y = 0; y < height; y++)
					{
						pCurrent = pOrig + y * stride + bmpData1.Width * 3;
						pCurrent2 = pOrig2 + y * stride2 + sharedWidth * 3;

						for (int x = sharedWidth; x < bmpData2.Width; x++)
						{
							*(pCurrent++) = *(pCurrent2++);
							*(pCurrent++) = *(pCurrent2++);
							*(pCurrent++) = *(pCurrent2++);
						}
					}
					for (int y = 0; y < height; y++)
					{
						pCurrent = pOrig + y * stride + (bmpData1.Width - sharedWidth) * 3;
						pCurrent1 = pOrig1 + y * stride1 + (bmpData1.Width - sharedWidth) * 3;
						pCurrent2 = pOrig2 + y * stride2;

						for (int x = 0; x < sharedWidth; x++)
						{
							*(pCurrent++) = (byte)(pCurrent1[0] * (sharedWidth - x) / (sharedWidth) + pCurrent2[0] * x / sharedWidth);
							*(pCurrent++) = (byte)(pCurrent1[1] * (sharedWidth - x) / (sharedWidth) + pCurrent2[1] * x / sharedWidth);
							*(pCurrent++) = (byte)(pCurrent1[2] * (sharedWidth - x) / (sharedWidth) + pCurrent2[2] * x / sharedWidth);

							pCurrent1 += 3;
							pCurrent2 += 3;
						}
					}
				}
			}
			finally
			{
				if (bmpData != null)
					bitmap.UnlockBits(bmpData);
			}

			return bitmap;
		}
		#endregion

		#endregion

	}

}
