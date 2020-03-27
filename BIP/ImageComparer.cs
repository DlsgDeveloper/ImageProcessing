using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class ImageComparer
	{

		#region constructor
		private ImageComparer()
		{
		}
		#endregion

		//	PUBLIC METHODS

		#region Compare()
		public static List<Point> Compare(Bitmap bitmap1, Bitmap bitmap2)
		{
			switch (bitmap1.PixelFormat)
			{
				case PixelFormat.Format32bppRgb: 
				case PixelFormat.Format32bppArgb:
					return Compare32bpp(bitmap1, bitmap2);
				case PixelFormat.Format24bppRgb: return Compare24bpp(bitmap1, bitmap2);
				case PixelFormat.Format8bppIndexed: return Compare8bpp(bitmap1, bitmap2);
				case PixelFormat.Format1bppIndexed: return Compare1bpp(bitmap1, bitmap2);
				default:
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}
		}
		#endregion

		//PRIVATE METHODS

		#region Compare32bpp()
		private static List<Point> Compare32bpp(Bitmap bitmap1, Bitmap bitmap2)
		{
			int width = (bitmap1.Width < bitmap2.Width) ? bitmap1.Width : bitmap2.Width;
			int height = (bitmap1.Height < bitmap2.Height) ? bitmap1.Height : bitmap2.Height;
			List<Point> differentPoints = new List<Point>();
			BitmapData bitmap1Data = null;
			BitmapData bitmap2Data = null;
			int x, y;

			try
			{
				bitmap1Data = bitmap1.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bitmap1.PixelFormat);
				bitmap2Data = bitmap2.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bitmap2.PixelFormat);

				int stride1 = bitmap1Data.Stride;
				int stride2 = bitmap2Data.Stride;

				unsafe
				{
					byte* pOrig1 = (byte*)bitmap1Data.Scan0.ToPointer();
					byte* pOrig2 = (byte*)bitmap2Data.Scan0.ToPointer();
					byte* pCurrent1, pCurrent2;

					for (y = 0; y < height; y++)
					{
						pCurrent1 = pOrig1 + y * stride1;
						pCurrent2 = pOrig2 + y * stride2;

						for (x = 0; x < width; x++)
						{
							if (pCurrent1[0] != pCurrent2[0] || pCurrent1[1] != pCurrent2[1] || pCurrent1[2] != pCurrent2[2] || pCurrent1[3] != pCurrent2[3])
								differentPoints.Add(new Point(x, y));

							pCurrent1 += 4;
							pCurrent2 += 4;
						}
					}
				}
			}
			finally
			{
				if (bitmap1Data != null)
					bitmap1.UnlockBits(bitmap1Data);
				if (bitmap2Data != null)
					bitmap2.UnlockBits(bitmap2Data);
			}

			return differentPoints;
		}
		#endregion

		#region Compare24bpp()
		private static List<Point> Compare24bpp(Bitmap bitmap1, Bitmap bitmap2)
		{
			int width = (bitmap1.Width < bitmap2.Width) ? bitmap1.Width : bitmap2.Width;
			int height = (bitmap1.Height < bitmap2.Height) ? bitmap1.Height : bitmap2.Height;
			List<Point> differentPoints = new List<Point>();
			BitmapData bitmap1Data = null;
			BitmapData bitmap2Data = null;
			int x, y;

			try
			{
				bitmap1Data = bitmap1.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bitmap1.PixelFormat);
				bitmap2Data = bitmap2.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bitmap2.PixelFormat);

				int stride1 = bitmap1Data.Stride;
				int stride2 = bitmap2Data.Stride;

				unsafe
				{
					byte* pOrig1 = (byte*)bitmap1Data.Scan0.ToPointer();
					byte* pOrig2 = (byte*)bitmap2Data.Scan0.ToPointer();
					byte* pCurrent1, pCurrent2;

					for (y = 0; y < height; y++)
					{
						pCurrent1 = pOrig1 + y * stride1;
						pCurrent2 = pOrig2 + y * stride2;

						for (x = 0; x < width; x++)
						{
							if (pCurrent1[0] != pCurrent2[0] || pCurrent1[1] != pCurrent2[1] || pCurrent1[2] != pCurrent2[2])
								differentPoints.Add(new Point(x, y));

							pCurrent1 += 3;
							pCurrent2 += 3;
						}
					}
				}
			}
			finally
			{
				if (bitmap1Data != null)
					bitmap1.UnlockBits(bitmap1Data);
				if (bitmap2Data != null)
					bitmap2.UnlockBits(bitmap2Data);
			}

			return differentPoints;
		}
		#endregion

		#region Compare8bpp()
		private static List<Point> Compare8bpp(Bitmap bitmap1, Bitmap bitmap2)
		{
			int width = (bitmap1.Width < bitmap2.Width) ? bitmap1.Width : bitmap2.Width;
			int height = (bitmap1.Height < bitmap2.Height) ? bitmap1.Height : bitmap2.Height;
			List<Point> differentPoints = new List<Point>();
			BitmapData bitmap1Data = null;
			BitmapData bitmap2Data = null;
			int x, y;

			try
			{
				bitmap1Data = bitmap1.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bitmap1.PixelFormat);
				bitmap2Data = bitmap2.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bitmap2.PixelFormat);

				int stride1 = bitmap1Data.Stride;
				int stride2 = bitmap2Data.Stride;

				unsafe
				{
					byte* pOrig1 = (byte*)bitmap1Data.Scan0.ToPointer();
					byte* pOrig2 = (byte*)bitmap2Data.Scan0.ToPointer();

					for (y = 0; y < height; y++)
						for (x = 0; x < width; x++)
							if (*(pOrig1 + y * stride1 + x) != *(pOrig2 + y * stride2 + x))
								differentPoints.Add(new Point(x, y));
				}
			}
			finally
			{
				if (bitmap1Data != null)
					bitmap1.UnlockBits(bitmap1Data);
				if (bitmap2Data != null)
					bitmap2.UnlockBits(bitmap2Data);
			}

			return differentPoints;
		}
		#endregion

		#region Compare1bpp()
		private static List<Point> Compare1bpp(Bitmap bitmap1, Bitmap bitmap2)
		{
			int width = (bitmap1.Width < bitmap2.Width) ? bitmap1.Width : bitmap2.Width;
			int height = (bitmap1.Height < bitmap2.Height) ? bitmap1.Height : bitmap2.Height;
			List<Point> differentPoints = new List<Point>();
			BitmapData bitmap1Data = null;
			BitmapData bitmap2Data = null;
			int x, y;

			try
			{
				bitmap1Data = bitmap1.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bitmap1.PixelFormat);
				bitmap2Data = bitmap2.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bitmap2.PixelFormat);

				int stride1 = bitmap1Data.Stride;
				int stride2 = bitmap2Data.Stride;

				unsafe
				{
					byte* pOrig1 = (byte*)bitmap1Data.Scan0.ToPointer();
					byte* pOrig2 = (byte*)bitmap2Data.Scan0.ToPointer();

					for (y = 0; y < height; y++)
						for (x = 0; x < width; x++)
							if ((*(pOrig1 + y * stride1 + x / 8) & (0x80 >> (x & 0x7))) != (*(pOrig2 + y * stride2 + x / 8) & (0x80 >> (x & 0x7))))
								differentPoints.Add(new Point(x, y));
				}
			}
			finally
			{
				if (bitmap1Data != null)
					bitmap1.UnlockBits(bitmap1Data);
				if (bitmap2Data != null)
					bitmap2.UnlockBits(bitmap2Data);
			}

			return differentPoints;
		}
		#endregion
	
	}
}
