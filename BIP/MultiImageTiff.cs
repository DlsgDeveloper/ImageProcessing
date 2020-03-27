using System;
using System.IO ;
using System.Drawing ;
using System.Drawing.Imaging ;
using ImageProcessing.Languages;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for MultiImageTiff.
	/// </summary>
	public class MultiImageTiff
	{
		private MultiImageTiff()
		{
		}

		//PUBLIC METHODS
		#region SplitImages()
		public static void SplitImages(string sourceFile, string destDir)
		{
			DirectoryInfo		dir = new DirectoryInfo(destDir) ;
			Bitmap				bmp = new Bitmap(sourceFile) ;
			FrameDimension		dimension = FrameDimension.Page ;
			int					count = bmp.GetFrameCount( dimension ) ;
			ImageCodecInfo		imageCodecInfo;
			EncoderParameters	encoderParams;
			
			for(int i = 0; i < count; i++)
			{
				bmp.SelectActiveFrame(dimension, i) ;

				imageCodecInfo = GetCodecInfo(bmp);
				encoderParams = GetEncoderParams(bmp);

				bmp.Save(dir.FullName + @"\" + i.ToString("D2") + ".tif", imageCodecInfo, encoderParams) ;
			}
		}
		#endregion

		#region MergeImages()
		public static void MergeImages(FileInfo[] files, FileInfo destinationFile)
		{
			Bitmap				bmp = new Bitmap(files[0].FullName) ;
			ImageCodecInfo		imageCodecInfo = GetCodecInfo(bmp) ;
			EncoderParameters	encoderParams = GetEncoderParams(bmp) ;
		   
			EncoderParameters parameters   = new EncoderParameters(2); 
			parameters.Param[0] = encoderParams.Param[0] ; 
			parameters.Param[1] = new EncoderParameter( Encoder.SaveFlag, (long) EncoderValue.MultiFrame ); 
			bmp.Save( destinationFile.FullName, imageCodecInfo, parameters ); 
   
			parameters.Param[0] = new EncoderParameter( Encoder.SaveFlag, (long) EncoderValue.FrameDimensionPage );

			for (int i = 1; i < files.Length; i++)
			{
				Bitmap bitmap = new Bitmap(files[i].FullName);

				encoderParams = GetEncoderParams(bitmap);
				parameters = new EncoderParameters(2);
				parameters.Param[0] = encoderParams.Param[0];
				parameters.Param[1] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);

				bmp.SaveAdd(bitmap, parameters);
			}

			parameters.Param[0] = new EncoderParameter( Encoder.SaveFlag, (long) EncoderValue.Flush ); 
			bmp.SaveAdd( parameters ); 
		}
		#endregion

		//PRIVATE METHODS
		#region GetCodecInfo()
		private static ImageCodecInfo GetCodecInfo(Bitmap image)
		{
			ImageFormat			imageFormat = new ImageFormat(image.RawFormat.Guid) ;
			ImageCodecInfo[]	encoders = ImageCodecInfo.GetImageEncoders();

			for(int j = 0; j < encoders.Length; ++j)
			{
				if(encoders[j].FormatID == imageFormat.Guid)
					return encoders[j];
			}
			return null;
		}
		#endregion

		#region GetEncoderParams()
		private static EncoderParameters GetEncoderParams(Bitmap image)
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
					encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 95L) ;
				}
			}
			else
			{
				encoderParams = new EncoderParameters(0) ;
			}
						
			return encoderParams ;
		}
		#endregion
	}
}
