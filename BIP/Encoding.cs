using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.IO;
using ImageProcessing.Languages;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Encoder.
	/// </summary>
	public class Encoding
	{
		private Encoding()
		{
		}

		#region GetCodecInfo()
		public static ImageCodecInfo GetCodecInfo(Bitmap image)
		{
			ImageCodecInfo[]	encoders = ImageCodecInfo.GetImageEncoders();

			foreach(ImageCodecInfo encoder in encoders)
			{
				if(encoder.FormatID == image.RawFormat.Guid)
					return encoder;
			}

			if (image.PixelFormat == PixelFormat.Format1bppIndexed)
			{
				foreach (ImageCodecInfo encoder in encoders)
				{
					if (encoder.FormatID == ImageFormat.Tiff.Guid)
						return encoder;
				}
			}
			else
			{
				foreach (ImageCodecInfo encoder in encoders)
				{
					if (encoder.FormatID == ImageFormat.Jpeg.Guid)
						return encoder;
				}
			}

			return encoders[0];
		}

		public static ImageCodecInfo GetCodecInfo(ImageFormat imageFormat)
		{
			ImageCodecInfo[]	encoders = ImageCodecInfo.GetImageEncoders();

			foreach(ImageCodecInfo encoder in encoders)
			{
				if(encoder.FormatID == imageFormat.Guid)
					return encoder;
			}
			return encoders[0];
		}

		public static ImageCodecInfo GetCodecInfo(ResultFormat format)
		{
			switch(format)
			{
				case ResultFormat.Png: return Encoding.GetCodecInfo(ImageFormat.Png);
				default: return Encoding.GetCodecInfo(ImageFormat.Tiff);
			}
		}
		#endregion

		#region GetEncoderParams()
		public static EncoderParameters GetEncoderParams(Bitmap image, long jpegCompression)
		{
			ImageFormat			imageFormat = new ImageFormat(image.RawFormat.Guid) ;
			EncoderParameters	encoderParams ;

			if(imageFormat.Guid == ImageFormat.Tiff.Guid)
			{
				byte[]				compressionValue = image.GetPropertyItem(0x0103).Value ;
				Int32				compression = (Int32) compressionValue[1] * 32768 + compressionValue[0] ;
				
				encoderParams = new EncoderParameters(1) ; 

				switch(compression)
				{
					case 1 :
						encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionNone) ; break ;
					case 3 :
						encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionCCITT3) ; break ;
					case 4 :
						encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionCCITT4) ; break ;
					case 5 :
						encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionLZW) ; break ;
					case 6 :
						throw new Exception(BIPStrings.TiffImageWithJpegCompressionIsNotSupported_STR) ;
					case 32773 :
						encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionRle) ; break ;
					default :
						throw new Exception(BIPStrings.UnsupportedTiffCompression_STR) ;
				}			
			}
			else if (imageFormat.Guid == ImageFormat.Jpeg.Guid)
			{
				if(image.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					encoderParams = new EncoderParameters(1) ;
					encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 8L) ;
				}
				else
				{
					encoderParams = new EncoderParameters(1) ;
					encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, jpegCompression) ;
				}
			}
			else
			{
				if(image.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					encoderParams = new EncoderParameters(1) ;
					encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 1L) ;
				}
				else if(image.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					encoderParams = new EncoderParameters(1) ;
					encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 8L) ;
				}
				else
				{
					encoderParams = new EncoderParameters(1) ;
					encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 24L) ;
				}
			}
						
			return encoderParams ;
		}

		public static EncoderParameters GetEncoderParams(ImageFormat imageFormat, long jpegCompression)
		{
			EncoderParameters encoderParams;

			if (imageFormat.Guid == ImageFormat.Tiff.Guid)
			{
				encoderParams = new EncoderParameters(1);
				encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);

			}
			else if (imageFormat.Guid == ImageFormat.Jpeg.Guid)
			{
				encoderParams = new EncoderParameters(1);
				encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, jpegCompression);
			}
			else
			{
				encoderParams = new EncoderParameters(0);
			}

			return encoderParams;
		}
		
		public static EncoderParameters GetEncoderParams(ImageFormat imageFormat, PixelFormat pixelFormat, long jpegCompression)
		{
			EncoderParameters encoderParams;

			if (imageFormat.Guid == ImageFormat.Tiff.Guid)
			{
				encoderParams = new EncoderParameters(2);
				encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);

				switch (pixelFormat)
				{
					case PixelFormat.Format1bppIndexed: encoderParams.Param[1] = new EncoderParameter(Encoder.ColorDepth, 1L); break;
					case PixelFormat.Format8bppIndexed: encoderParams.Param[1] = new EncoderParameter(Encoder.ColorDepth, 8L); break;
					case PixelFormat.Format32bppRgb:
					case PixelFormat.Format32bppArgb:
						encoderParams.Param[1] = new EncoderParameter(Encoder.ColorDepth, 32L);
						break;
					default: encoderParams.Param[1] = new EncoderParameter(Encoder.ColorDepth, 24L); break;
				}
			}
			else if (imageFormat.Guid == ImageFormat.Jpeg.Guid)
			{
				switch (pixelFormat)
				{
					case PixelFormat.Format8bppIndexed:
						encoderParams = new EncoderParameters(2);
						encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, jpegCompression);
						encoderParams.Param[1] = new EncoderParameter(Encoder.ColorDepth, 8L);
						break;
					default:
						encoderParams = new EncoderParameters(1);
						encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, jpegCompression);
						break;
				}
			}
			else
			{
				switch (pixelFormat)
				{
					case PixelFormat.Format1bppIndexed:
						encoderParams = new EncoderParameters(1);
						encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 1L);
						break;
					case PixelFormat.Format8bppIndexed:
						encoderParams = new EncoderParameters(1);
						encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 8L);
						break;
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
						encoderParams = new EncoderParameters(1);
						encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 32L);
						break;
					default:
						encoderParams = new EncoderParameters(1);
						encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 24L);
						break;
				}
			}

			return encoderParams;
		}
		
		public static EncoderParameters GetEncoderParams(ResultFormat format, long colorDepth)
		{
			EncoderParameters	encoderParams;

			switch (format)
			{
				case ResultFormat.TiffG4: 
				{
					encoderParams = new EncoderParameters(2);
					encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long) EncoderValue.CompressionCCITT4);
					encoderParams.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, colorDepth);
				} break;
				case ResultFormat.TiffLZW: 
				{
					encoderParams = new EncoderParameters(2);
					encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long) EncoderValue.CompressionLZW);
					encoderParams.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, colorDepth);
				} break;
				case ResultFormat.Png: 
				{
					encoderParams = new EncoderParameters(1);
					encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, colorDepth);
				} break;
				default: 
				{
					encoderParams = new EncoderParameters(2);
					encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long) EncoderValue.CompressionNone);
					encoderParams.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, colorDepth);
				} break;			
			}

			return encoderParams;
		}
		#endregion

		#region GetColorDepth()
		public static long GetColorDepth(Bitmap bitmap)
		{
			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format1bppIndexed: return 1;
				case PixelFormat.Format4bppIndexed: return 4;
				case PixelFormat.Format8bppIndexed: return 8;
				case PixelFormat.Format16bppArgb1555:
				case PixelFormat.Format16bppGrayScale:
				case PixelFormat.Format16bppRgb555:
				case PixelFormat.Format16bppRgb565:
					return 16;
				case PixelFormat.Format24bppRgb: return 24;
				case PixelFormat.Format32bppArgb: 
				case PixelFormat.Format32bppRgb: 
				case PixelFormat.Format32bppPArgb: 
					return 32;
				case PixelFormat.Format48bppRgb: return 48;
				case PixelFormat.Format64bppArgb: 
				case PixelFormat.Format64bppPArgb:
					return 64;
				default: return 24;
			}
		}
		#endregion
	}
}
