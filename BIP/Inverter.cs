using ImageProcessing.Languages;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Inverter.
	/// </summary>
	public class Inverter
	{
		#region constructor
		private Inverter()
		{
		}
		#endregion

		//	PUBLIC METHODS				
		#region Invert()
		public static void Invert(Bitmap bitmap)
		{
			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format1bppIndexed: Invert1bpp(bitmap); break;
				case PixelFormat.Format8bppIndexed: Invert8bpp(bitmap); break;
				case PixelFormat.Format24bppRgb:
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:
					InvertColor(bitmap);
					break;
				default: throw new Exception(BIPStrings.UnsupportedImageFormat_STR);
			}
		}

		public static void Invert(BitmapData bitmapData)
		{
			switch (bitmapData.PixelFormat)
			{
				case PixelFormat.Format1bppIndexed: Invert1bpp(bitmapData); break;
				case PixelFormat.Format8bppIndexed: Invert8bpp(bitmapData); break;
				case PixelFormat.Format24bppRgb:
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:
					InvertColor(bitmapData);
					break;
				default: throw new Exception(BIPStrings.UnsupportedImageFormat_STR);
			}
		}
		#endregion

		//PRIVATE METHODS

		#region InvertColor()
		private static void InvertColor(Bitmap bitmap)
		{
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				InvertColor(bitmapData);
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}

		private static void InvertColor(BitmapData bitmapData)
		{
			int x, y;
			int width = bitmapData.Width;
			int height = bitmapData.Height;
			int stride = bitmapData.Stride;

			unsafe
			{
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
				byte* pCurrent;

				if (bitmapData.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (y = 0; y < height; y++)
					{
						pCurrent = pSource + y * stride;

						for (x = 0; x < width; x++)
						{
							pCurrent[0] = (byte)(255 - pCurrent[0]);
							pCurrent[1] = (byte)(255 - pCurrent[1]);
							pCurrent[2] = (byte)(255 - pCurrent[2]);
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
							pCurrent[0] = (byte)(255 - pCurrent[0]);
							pCurrent[1] = (byte)(255 - pCurrent[1]);
							pCurrent[2] = (byte)(255 - pCurrent[2]);
							pCurrent += 4;
						}
					}
				}
			}
		}
		#endregion

		#region Invert8bpp()
		private static void Invert8bpp(Bitmap bitmap)
		{
			ColorPalette palette = bitmap.Palette;

			if (palette.Entries.Length > 0)
			{

				for (int i = 0; i < bitmap.Palette.Entries.Length; i++)
					palette.Entries[i] = Color.FromArgb(255 - bitmap.Palette.Entries[i].R, 255 - bitmap.Palette.Entries[i].G, 255 - bitmap.Palette.Entries[i].B);

				bitmap.Palette = palette;
			}
			else
			{
				BitmapData bitmapData = null;

				try
				{
					bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
					Invert8bpp(bitmapData);
				}
				finally
				{
					if (bitmapData != null)
						bitmap.UnlockBits(bitmapData);
				}
			}
		}

		private static void Invert8bpp(BitmapData bitmapData)
		{
			int x, y;
			int width = bitmapData.Width;
			int height = bitmapData.Height;
			int stride = bitmapData.Stride;

			unsafe
			{
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();

				for (y = 0; y < height; y++)
					for (x = 0; x < width; x++)
						pSource[y * stride + x] = (byte)(255 - pSource[y * stride + x]);
			}
		}
		#endregion

		#region Invert1bpp()
		private static void Invert1bpp(Bitmap bitmap)
		{
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
				Invert1bpp(bitmapData);
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}

		private static void Invert1bpp(BitmapData bitmapData)
		{
			int x, y;
			int width = bitmapData.Width;
			int height = bitmapData.Height;

			int stride = bitmapData.Stride;

			unsafe
			{
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
				byte* pCurrent;

				for (y = 0; y < height; y++)
				{
					pCurrent = pSource + y * stride;

					for (x = 0; x < stride; x++)
					{
						*pCurrent = (byte)(255 - *pCurrent);
						pCurrent++;
					}
				}
			}
		}
		#endregion
	}
}
