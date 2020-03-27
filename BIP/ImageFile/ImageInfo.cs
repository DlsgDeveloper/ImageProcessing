using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageProcessing.Languages;


namespace ImageProcessing.ImageFile
{
    public class ImageInfo
    {
        public readonly int					Width;
        public readonly int					Height;
        public readonly PixelsFormat		PixelsFormat;
        public readonly int					DpiH;
        public readonly int					DpiV;
		public readonly ImageCodecInfo		CodecInfo;
		public readonly EncoderParameters	EncoderParameters;

		static string szTiffHeaderForMotorola = "\x4D\x4D\x00\x2a";
		static string tiffHeaderForIntel = "II*";
		static string szPNGHeader = "\x89PNG\r\n\x1a\n";
		static string szGIF87aHeader = "GIF87a";
		static string szGIF89aHeader = "GIF89a";
		// this part of the header 
		static string szJPEGCommonHeader = "\xFF\xD8\xFF";
		// for future use
		//static string		szJPEGCommonEOI			= "\xFF\xD9";
		// followinf 4 bytes will be size and the 4 will be
		static string szBMPHeader = "\x42\x4D";

		#region constructor
		internal ImageInfo(int width, int height, int dpiH, int dpiV, PixelsFormat pixelsFormat, ImageCodecInfo codecInfo, EncoderParameters encoderParams)
        {
            this.Width = width;
            this.Height = height;
            this.DpiH = dpiH;
            this.DpiV = dpiV;
            this.PixelsFormat = pixelsFormat;
			this.CodecInfo = codecInfo;
			this.EncoderParameters = encoderParams;
		}

		public ImageInfo(FileInfo file)
			: this(file.FullName)
		{
		}

		public ImageInfo(string filePath)
		{
			/*using (ImageProcessing.BigImages.ItDecoder itDecoder = new BigImages.ItDecoder(filePath))
			{
				EncoderParameters encoderParams = new EncoderParameters(1);

				encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, 24L);

				ImageInfo imageInfo = new ImageInfo(itDecoder.Width, itDecoder.Height, itDecoder.DpiX, itDecoder.DpiY, itDecoder.PixelsFormat,
					Encoding.GetCodecInfo(System.Drawing.Imaging.ImageFormat.Jpeg), encoderParams);
			}*/


			using (FileStream reader = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				byte[] buffer = new byte[10];
				reader.Read(buffer, 0, 10);

				ImageType imageType = GetImageType(buffer);
				if (imageType == ImageType.UnsupportedType)
					throw new Exception(BIPStrings.UnsupportedImageFormat_STR);

				ImageInfo imageInfo = GetImageInfo(reader, imageType);

				this.Width = imageInfo.Width;
				this.Height = imageInfo.Height;
				this.DpiH = imageInfo.DpiH;
				this.DpiV = imageInfo.DpiV;
				this.PixelsFormat = imageInfo.PixelsFormat;
				this.CodecInfo = imageInfo.CodecInfo;
				this.EncoderParameters = imageInfo.EncoderParameters;
			}


		}

		public ImageInfo(Bitmap bitmap)
		{
			this.Width = bitmap.Width;
			this.Height = bitmap.Height;
			this.DpiH = Convert.ToInt32(bitmap.HorizontalResolution);
			this.DpiV = Convert.ToInt32(bitmap.VerticalResolution);
			this.CodecInfo = Encoding.GetCodecInfo(bitmap);
			this.EncoderParameters = Encoding.GetEncoderParams(bitmap, 80);

			switch (bitmap.PixelFormat)
			{
				case System.Drawing.Imaging.PixelFormat.Format1bppIndexed: this.PixelsFormat = PixelsFormat.FormatBlackWhite; break;
				case System.Drawing.Imaging.PixelFormat.Format4bppIndexed: this.PixelsFormat = PixelsFormat.Format4bppGray; break;
				case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
					if (Misc.IsGrayscale(bitmap))
						this.PixelsFormat = PixelsFormat.Format8bppGray;
					else
						this.PixelsFormat = PixelsFormat.Format8bppIndexed;
					break;
				case System.Drawing.Imaging.PixelFormat.Format32bppRgb: this.PixelsFormat = PixelsFormat.Format32bppRgb; break;
				default: this.PixelsFormat = PixelsFormat.Format24bppRgb; break;
			}
		}		
		#endregion

		#region enum ImageType()
		public enum ImageType
		{
			// compare file headers to determine the file type
			UnsupportedType,
			TIFFINTEL,
			TIFFMOTOROLA,
			PNG,
			GIF87a,
			GIF89a,
			BITMAP,
			JPEGJFIF,
			JPEGEXIF,
			JPEGAPP2,
			JPEGAPP3,
			JPEGAPP4,
			JPEGAPP5,
			JPEGAPP6,
			JPEGAPP7,
			JPEGAPP8,
			JPEGAPP9,
			JPEGAPPA,
			JPEGAPPB,
			JPEGAPPC,
			JPEGAPPD,
			JPEGAPPE,
			JPEGAPPF,
		}
		#endregion

        public Size			Size		{ get { return new Size(Width, Height); } }
		public Rectangle	Rectangle	{ get { return new Rectangle(0, 0, Width, Height); } }
		
		public System.Drawing.Imaging.PixelFormat PixelFormat
		{
			get { return Transactions.GetPixelFormat(this.PixelsFormat); }
		}

		public int BitsPerPixel
		{
			get
			{
				switch (PixelsFormat)
				{
					case PixelsFormat.Format24bppRgb: return 24; 
					case PixelsFormat.Format32bppRgb: return 32; 
					case PixelsFormat.Format4bppGray: return 4; 
					case PixelsFormat.Format8bppGray: return 8; 
					case PixelsFormat.Format8bppIndexed: return 8; 
					case PixelsFormat.FormatBlackWhite: return 1; 
					default: throw new ImageProcessing.IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
		}

		//PUBLIC METHODS
		#region public methods

		#region GetPixelFormat()
		public static PixelsFormat GetPixelFormat(int pixelDepth, bool grayscale)
		{
			switch (pixelDepth)
			{
				case 1: return PixelsFormat.FormatBlackWhite;
				case 4: return PixelsFormat.Format4bppGray;
				case 8:
					if (grayscale) 
						return PixelsFormat.Format8bppGray;
					else
						return PixelsFormat.Format8bppIndexed;
				case 32: return PixelsFormat.Format32bppRgb;
				default: return PixelsFormat.Format24bppRgb;
			}
		}
		#endregion

		public override string ToString()
		{
			string str = "";

			str += string.Format("Size: {0}, {1}", this.Width, this.Height) + Environment.NewLine;
			str += string.Format("Format: {0}", this.PixelsFormat) + Environment.NewLine;
			str += string.Format("BitsPerPixel: {0}", this.BitsPerPixel) + Environment.NewLine;
			str += string.Format("DPI: {0}, {1}", this.DpiH, this.DpiV) + Environment.NewLine;
			str += string.Format("Codec Info: {0}", this.CodecInfo.CodecName) + Environment.NewLine;
			
			return str;
		}

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region GetImageType()
		static ImageType GetImageType(byte[] buffer)
		{
			// compare file headers to determine the file type
			if (Compare(tiffHeaderForIntel, buffer))
				return ImageType.TIFFINTEL;
			else if (Compare(szTiffHeaderForMotorola, buffer))
				return ImageType.TIFFMOTOROLA;
			else if (Compare(szPNGHeader, buffer))
				return ImageType.PNG;
			else if (Compare(szGIF87aHeader, buffer))
				return ImageType.GIF87a;
			else if (Compare(szGIF89aHeader, buffer))
				return ImageType.GIF89a;
			else if (Compare(szBMPHeader, buffer))
			{
				// 7 to ten byte must be zero
				// 3 to 6 is size of image
				//if (header.Substring(6,4) == "    ")
				return ImageType.BITMAP;
			}
			else if (Compare(szJPEGCommonHeader, buffer))
			{
				switch (buffer[3])
				{
					case 0xE0: return ImageType.JPEGJFIF;
					case 0xDB: return ImageType.JPEGJFIF;
					case 0xE1: return ImageType.JPEGEXIF;
					case 0xE2: return ImageType.JPEGAPP2;
					case 0xE3: return ImageType.JPEGAPP3;
					case 0xE4: return ImageType.JPEGAPP4;
					case 0xE5: return ImageType.JPEGAPP5;
					case 0xE6: return ImageType.JPEGAPP6;
					case 0xE7: return ImageType.JPEGAPP7;
					case 0xE8: return ImageType.JPEGAPP8;
					case 0xE9: return ImageType.JPEGAPP9;
					case 0xEA: return ImageType.JPEGAPPA;
					case 0xEB: return ImageType.JPEGAPPB;
					case 0xEC: return ImageType.JPEGAPPC;
					case 0xED: return ImageType.JPEGAPPD;
					case 0xEE: return ImageType.JPEGAPPE;
					case 0xEF: return ImageType.JPEGAPPF;
					default: break;
				}
			}
			return ImageType.UnsupportedType;
		}
		#endregion

		#region GetString()
		public static string GetString(ImageType imageType)
		{
			switch (imageType)
			{
				case ImageType.TIFFINTEL: return "Tiff image for Intel processor";
				case ImageType.TIFFMOTOROLA: return "Tiff image for Motorola processor";
				case ImageType.GIF87a: return "GIF87a Image";
				case ImageType.GIF89a: return "GIF89a Image";
				case ImageType.PNG: return "PNG Image";
				case ImageType.JPEGJFIF: return "JPEG JFIF compliant image";
				case ImageType.JPEGEXIF: return "JPEG EXIF compliant image";
				case ImageType.JPEGAPP2: return "JPEG with APP2 marker";
				case ImageType.JPEGAPP3: return "JPEG with APP3 marker";
				case ImageType.JPEGAPP4: return "JPEG with APP4 marker";
				case ImageType.JPEGAPP5: return "JPEG with APP5 marker";
				case ImageType.JPEGAPP6: return "JPEG with APP6 marker";
				case ImageType.JPEGAPP7: return "JPEG with APP7 marker";
				case ImageType.JPEGAPP8: return "JPEG with APP8 marker";
				case ImageType.JPEGAPP9: return "JPEG with APP9 marker";
				case ImageType.JPEGAPPA: return "JPEG with APPA marker";
				case ImageType.JPEGAPPB: return "JPEG with APPB marker";
				case ImageType.JPEGAPPC: return "JPEG with APPC marker";
				case ImageType.JPEGAPPD: return "JPEG with APPD marker";
				case ImageType.JPEGAPPE: return "JPEG with APPE marker";
				case ImageType.JPEGAPPF: return "JPEG with APPF marker";
				case ImageType.BITMAP: return "Bitmap file";
				default: throw new Exception(BIPStrings.UnsupportedImageFormat_STR);
			}
		}
		#endregion

		#region Compare()
		static bool Compare(String str, byte[] buffer)
		{
			for (int i = 0; i < str.Length; i++)
				if (Convert.ToByte(str[i]) != buffer[i])
					return false;

			return true;
		}
		#endregion

		#region GetImageInfo()
		static ImageInfo GetImageInfo(FileStream reader, ImageType imageType)
		{
			switch (imageType)
			{
				case ImageType.JPEGEXIF:
					return Exif.GetImageInfo(reader);
				case ImageType.JPEGJFIF:
					return Jfif.GetImageInfo(reader);
				case ImageType.GIF87a:
				case ImageType.GIF89a:
					return Gif.GetImageInfo(reader);
				case ImageType.PNG:
					return Png.GetImageInfo(reader);
				case ImageType.TIFFINTEL:
				case ImageType.TIFFMOTOROLA:
					return Tiff.GetImageInfo(reader);
				case ImageType.BITMAP:
					return Bmp.GetImageInfo(reader);
				default:
					return null;
			}
		}
		#endregion

		#endregion

	}
}
