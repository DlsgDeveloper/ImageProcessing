using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing
{
	class Transactions
	{
		#region GetPixelFormat()
		public static System.Drawing.Imaging.PixelFormat GetPixelFormat(ImageProcessing.PixelsFormat pixelsFormat)
		{
			switch (pixelsFormat)
			{
				case ImageProcessing.PixelsFormat.FormatBlackWhite: return System.Drawing.Imaging.PixelFormat.Format1bppIndexed;
				case ImageProcessing.PixelsFormat.Format4bppGray: return System.Drawing.Imaging.PixelFormat.Format4bppIndexed;
				case ImageProcessing.PixelsFormat.Format8bppIndexed: return System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
				case ImageProcessing.PixelsFormat.Format8bppGray: return System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
				case ImageProcessing.PixelsFormat.Format32bppRgb: return System.Drawing.Imaging.PixelFormat.Format32bppRgb;
				default: return System.Drawing.Imaging.PixelFormat.Format24bppRgb;
			}
		}
		#endregion

		#region GetPixelsFormat()
		public static ImageProcessing.PixelsFormat GetPixelsFormat(System.Drawing.Imaging.PixelFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				case System.Drawing.Imaging.PixelFormat.Format1bppIndexed: return ImageProcessing.PixelsFormat.FormatBlackWhite;
				case System.Drawing.Imaging.PixelFormat.Format4bppIndexed: return ImageProcessing.PixelsFormat.Format4bppGray;
				case System.Drawing.Imaging.PixelFormat.Format8bppIndexed: return ImageProcessing.PixelsFormat.Format8bppIndexed;
				case System.Drawing.Imaging.PixelFormat.Format32bppRgb: return ImageProcessing.PixelsFormat.Format32bppRgb;
				case System.Drawing.Imaging.PixelFormat.Format32bppArgb: return ImageProcessing.PixelsFormat.Format32bppRgb;
				default: return ImageProcessing.PixelsFormat.Format24bppRgb;
			}
		}
		#endregion

		#region GetPixelsFormat()
		public static ImageComponent.PixelFormat GetImageComponentPixelFormat(ImageProcessing.PixelsFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				case PixelsFormat.FormatBlackWhite: return ImageComponent.PixelFormat.FormatBlackWhite;
				//case PixelsFormat.Format1bppIndexed: return ImageComponent.PixelFormat.Format1bppIndexed;
				case PixelsFormat.Format4bppGray: return ImageComponent.PixelFormat.Format4bppIndexed;
				case PixelsFormat.Format8bppGray: return ImageComponent.PixelFormat.Format8bppIndexed;
				case PixelsFormat.Format8bppIndexed: return ImageComponent.PixelFormat.Format8bppIndexed;
				case PixelsFormat.Format24bppRgb: return ImageComponent.PixelFormat.Format24bppBGR;
				case PixelsFormat.Format32bppRgb: return ImageComponent.PixelFormat.Format32bppBGRA;
				default: throw new ImageProcessing.IpException(ErrorCode.ErrorUnsupportedFormat);
			}
		}
		#endregion

	}
}
