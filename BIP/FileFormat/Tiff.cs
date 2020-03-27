using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageProcessing;
using ImageProcessing.BigImages;

namespace ImageProcessing.FileFormat
{
	public class Tiff : ImageProcessing.FileFormat.IImageFormat
	{
		public ImageComponent.TiffCompression Compression;

		public Tiff(ImageProcessing.IpSettings.ItImage.TiffCompression compression)
		{
			this.Compression = GetTiffCompression(compression);
		}

		/*public Tiff(ImageComponent.TiffCompression compression)
		{
			this.Compression = compression;
		}*/

		#region GetTiffCompression()
		public static ImageComponent.TiffCompression GetTiffCompression(ImageProcessing.IpSettings.ItImage.TiffCompression tiffCompression)
		{
			if (tiffCompression == ImageProcessing.IpSettings.ItImage.TiffCompression.G4)
				return ImageComponent.TiffCompression. WICTiffCompressionCCITT4;
			else if (tiffCompression == ImageProcessing.IpSettings.ItImage.TiffCompression.LZW)
				return ImageComponent.TiffCompression.WICTiffCompressionLZW;
			else
				return ImageComponent.TiffCompression.WICTiffCompressionNone;
		}
		#endregion

	}
}
