using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing.BigImages;
using System.Collections.Generic;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Misc.
	/// </summary>
	public class Misc
	{
		internal static int minBufferSize = (int)Math.Pow(2, 25);
		internal static int bufferSize = (int)Math.Pow(2, 26);
		
		static ColorPalette grayscalePalette = GetGrayscalePalette();		
		static public ColorPalette		GrayscalePalette	{ get{return grayscalePalette;} }

		#region BytesPerPixel()
		public static float BytesPerPixel(PixelFormat pixelFormat)
		{		
			switch(pixelFormat)
			{
				case PixelFormat.Format8bppIndexed: return 1;
				case PixelFormat.Format1bppIndexed: return 0.125F;
				case PixelFormat.Format24bppRgb: return 3;
				case PixelFormat.Format32bppArgb: return 4;
				case PixelFormat.Format32bppPArgb: return 4;
				case PixelFormat.Format32bppRgb: return 4;
				case PixelFormat.Format16bppArgb1555: return 2;
				case PixelFormat.Format16bppGrayScale: return 2;
				case PixelFormat.Format16bppRgb555: return 2;
				case PixelFormat.Format16bppRgb565: return 2;
				case PixelFormat.Format48bppRgb: return 6;
				case PixelFormat.Format4bppIndexed: return .5F;
				case PixelFormat.Format64bppArgb: return 8;
				case PixelFormat.Format64bppPArgb: return 8;
				default: return 3;
			}
		}
		#endregion

		#region GetEmptyPalette()
		public static ColorPalette GetEmptyPalette()
		{
			Bitmap bitmap = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);

			ColorPalette palette = bitmap.Palette;

			bitmap.Dispose();
			return palette;
		}
		#endregion

		#region GetGrayscalePalette()
		public static ColorPalette	GetGrayscalePalette()
		{
			Bitmap	bitmap = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);

			ColorPalette	palette = bitmap.Palette;

			for(int i = 0; i < 256; i++)
				palette.Entries[i] = Color.FromArgb(i, i, i);

			return palette;
		}
		#endregion

		#region GetColorPalette()
		public static ColorPalette GetColorPalette(Color[] entries)
		{
			Bitmap bitmap = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);

			ColorPalette palette = bitmap.Palette;

			for (int i = 0; i < palette.Entries.Length && i < entries.Length; i++)
				palette.Entries[i] = entries[i];

			return palette;
		}
		#endregion

		#region GetGrayscalePalette()
		public static ColorPalette GetGrayscalePalette(PixelFormat pixelFormat)
		{
			if (pixelFormat == PixelFormat.Format8bppIndexed)
			{
				return GetGrayscalePalette();
			}
			else if (pixelFormat == PixelFormat.Format4bppIndexed)
			{
				Bitmap bitmap = new Bitmap(1, 1, PixelFormat.Format4bppIndexed);

				ColorPalette palette = bitmap.Palette;

				for (int i = 0; i < 16; i++)
					palette.Entries[i] = Color.FromArgb(i * 17, i * 17, i * 17);

				return palette;
			}
			else
				throw new IpException(ErrorCode.ErrorUnsupportedFormat);
		}
		#endregion

		#region GetGrayscaleBitmap()
		public static Bitmap GetGrayscaleBitmap(int width, int height)
		{
			Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed)
			{
				Palette = GetGrayscalePalette()
			};

			return bitmap;
		}
		#endregion

		#region IsGrayscale()
		public static bool IsGrayscale(Bitmap bitmap)
		{
			if (bitmap.Palette == null)
				return true;

			return IsPaletteGrayscale(bitmap.Palette.Entries);
		}
		#endregion

		#region IsPaletteGrayscale()
		public static bool IsPaletteGrayscale(Color[] palette)
		{		
			for (int i = 0; i < palette.Length; i++)
				if (palette[i].R != i || palette[i].G != i || palette[i].B != i)
					return false;

			return true;
		}
		#endregion

		#region GetStride()
		public unsafe static int GetStride(int width, PixelFormat pixelFormat)
		{
			Bitmap bitmap = null;
			BitmapData bmpData = null;
			int stride = 0;

			try
			{
				bitmap = new Bitmap(width, 1, pixelFormat);
				bmpData = bitmap.LockBits(Rectangle.FromLTRB(0, 0, width, 1), ImageLockMode.WriteOnly, pixelFormat);
				stride = bmpData.Stride;
			}
			finally
			{
				if (bitmap != null && bmpData != null)
					bitmap.UnlockBits(bmpData);

				if (bitmap != null)
					bitmap.Dispose();
			}

			return stride;
		}
		#endregion

		#region SetBitmapResolution()
		public static void SetBitmapResolution(Bitmap bitmap, float xDpi, float yDpi)
		{
			if (bitmap != null && xDpi > 0 && yDpi > 0)
				bitmap.SetResolution(xDpi, yDpi);
		}

		public static void SetBitmapResolution(Bitmap bitmap, int xDpi, int yDpi)
		{
			if (bitmap != null && xDpi > 0 && yDpi > 0)
				bitmap.SetResolution(xDpi, yDpi);
		}
		#endregion

		#region GetStripHeightMax()
		public static int GetStripHeightMax(ItDecoder itDecoder)
		{
			return (int)(bufferSize / (itDecoder.Width * BytesPerPixel(itDecoder.PixelsFormat))) / 8 * 8;
		}

		public static int GetStripHeightMax(ItDecoder itDecoder, double zoom)
		{
			if (zoom <= 1)
				return GetStripHeightMax(itDecoder);
			else
				return (int)(bufferSize / (itDecoder.Width * BytesPerPixel(itDecoder.PixelsFormat) * zoom * zoom)) / 8 * 8;
		}

		public static int GetStripHeightMax(ItDecoder itDecoder, ItEncoder itEncoder)
		{
			int decoderStripHeight = (int)(bufferSize / (itDecoder.Width * BytesPerPixel(itDecoder.PixelsFormat))) / 8 * 8;
			int encoderStripHeight = (int)(bufferSize / (itEncoder.Width * BytesPerPixel(itEncoder.PixelsFormat))) / 8 * 8;

			return Math.Max(decoderStripHeight, encoderStripHeight);
		}
		#endregion

		#region BytesPerPixel()
		private static float BytesPerPixel(PixelsFormat pixelsFormat)
		{
			switch (pixelsFormat)
			{
				case PixelsFormat.Format8bppGray: return 1;
				case PixelsFormat.Format8bppIndexed: return 1;
				case PixelsFormat.FormatBlackWhite: return 0.125F;
				case PixelsFormat.Format24bppRgb: return 3;
				case PixelsFormat.Format32bppRgb: return 4;
				case PixelsFormat.Format4bppGray: return .5F;
				default: return 3;
			}
		}
		#endregion

		#region GetPixelFormat()
		public static System.Drawing.Imaging.PixelFormat GetPixelFormat(PixelsFormat pixelsFormat)
		{
			switch (pixelsFormat)
			{
				case PixelsFormat.Format32bppRgb: return PixelFormat.Format32bppRgb;
				case PixelsFormat.Format24bppRgb: return PixelFormat.Format24bppRgb;
				case PixelsFormat.Format8bppGray:
				case PixelsFormat.Format8bppIndexed:
					return PixelFormat.Format8bppIndexed;
				case PixelsFormat.Format4bppGray: return PixelFormat.Format4bppIndexed;
				case PixelsFormat.FormatBlackWhite: return PixelFormat.Format1bppIndexed;
				default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}
		}

		public static System.Drawing.Imaging.PixelFormat GetPixelFormat(ImageComponent.PixelFormat pixelFormat)
		{
			if (pixelFormat.ToString().Contains("Format32bpp"))
				return PixelFormat.Format32bppRgb;
			else if (pixelFormat.ToString().Contains("Format24bpp"))
				return PixelFormat.Format24bppRgb;
			else if (pixelFormat.ToString().Contains("Format8bpp"))
				return PixelFormat.Format8bppIndexed;
			else if (pixelFormat.ToString().Contains("FormatBlackWhite") || pixelFormat.ToString().Contains("Format1bppIndexed"))
				return PixelFormat.Format1bppIndexed;

			throw new IpException(ErrorCode.ErrorUnsupportedFormat);
		}
		#endregion

		#region GetMedianIndex()
		public static uint GetMedianIndex(uint[] array)
		{
			uint items = 0;
			uint sum = 0;

			for (int i = 0; i < array.Length; i++)
				items += array[i];

			for (uint i = 0; i < array.Length; i++)
			{
				sum += array[i];

				if (sum >= items / 2)
					return i;
			}

			return (uint)(array.Length - 1);
		}
		#endregion

		#region GetMedianValue()
		public static double GetMedianValue(List<double> list)
		{
			
			if(list.Count == 0)
				return 0;
			if(list.Count == 1)
				return list[0];
			
			List<double> clone = new List<double>(list);
			clone.Sort();

			return clone[list.Count / 2];
		}
		#endregion

		#region GetAverage()
		public static double GetAverage(double[] array)
		{
			return array.Sum() / array.Length;
		}
		#endregion

		#region GetErrorMessage()
		public static string GetErrorMessage(Exception ex)
		{
			string message = ex.Message;

			while ((ex = ex.InnerException) != null)
				message = Environment.NewLine + ex.Message;

			return "Image Component: Unexpected Error! " + message;
		}
		#endregion
	}

	#region enum ResultFormat
	public enum ResultFormat
	{
		TiffNone = 0,
		TiffG4 = 1,
		TiffLZW = 2,
		Png = 3
	}
	#endregion

	#region enum PixelsFormat
	public enum PixelsFormat
	{
		Format32bppRgb = ImageComponent.PixelFormat.Format32bppBGR,
		Format24bppRgb = ImageComponent.PixelFormat.Format24bppBGR,
		Format8bppIndexed = ImageComponent.PixelFormat.Format8bppIndexed,
		Format8bppGray = ImageComponent.PixelFormat.Format8bppGray,
		Format4bppGray = ImageComponent.PixelFormat.Format4bppGray,
		//Format1bppIndexed = ImageComponent.PixelFormat.Format1bppIndexed,
		FormatBlackWhite = ImageComponent.PixelFormat.FormatBlackWhite
	}
	#endregion

	public delegate void ProgressHnd(float progress);

}
