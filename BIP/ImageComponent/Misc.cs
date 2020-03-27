using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

using ImageComponent.InteropServices;


namespace ImageComponent
{
	#region enum PixelsFormat
	/*public enum PixelsFormat
	{
		Format32bppRgb = 14,
		Format24bppRgb = 12,
		Format8bppIndexed = 5,
		Format8bppGray = 8,
		Format4bppGray = 7,
		//Format1bppIndexed = PixelFormat.Format1bppIndexed,
		FormatBlackWhite = 1
	}*/
	#endregion

	#region enum  PixelFormat
	public enum PixelFormat
	{
		FormatDontCare,
		FormatBlackWhite,
		Format1bppIndexed,
		Format2bppIndexed,
		Format4bppIndexed,
		Format8bppIndexed,
		Format2bppGray,
		Format4bppGray,
		Format8bppGray,
		Format16bppBGR555,
		Format16bppBGR565,
		Format16bppGray,
		Format24bppBGR,
		Format24bppRGB,
		Format32bppBGR,
		Format32bppBGRA,
		Format32bppPBGRA,
		Format32bppGrayFloat,
		Format48bppRGBFixedPoint,
		Format16bppGrayFixedPoint,
		Format32bppBGR101010,
		Format48bppRGB,
		Format64bppRGBA,
		Format64bppPRGBA,
		Format96bppRGBFixedPoint,
		Format128bppRGBAFloat,
		Format128bppPRGBAFloat,
		Format128bppRGBFloat,
		Format32bppCMYK,
		Format64bppRGBAFixedPoint,
		Format64bppRGBFixedPoint,
		Format128bppRGBAFixedPoint,
		Format128bppRGBFixedPoint,
		Format64bppRGBAHalf,
		Format64bppRGBHalf,
		Format48bppRGBHalf,
		Format32bppRGBE,
		Format16bppGrayHalf,
		Format32bppGrayFixedPoint,
		Format64bppCMYK,
		Format24bpp3Channels,
		Format32bpp4Channels,
		Format40bpp5Channels,
		Format48bpp6Channels,
		Format56bpp7Channels,
		Format64bpp8Channels,
		Format48bpp3Channels,
		Format64bpp4Channels,
		Format80bpp5Channels,
		Format96bpp6Channels,
		Format112bpp7Channels,
		Format128bpp8Channels,
		Format40bppCMYKAlpha,
		Format80bppCMYKAlpha,
		Format32bpp3ChannelsAlpha,
		Format40bpp4ChannelsAlpha,
		Format48bpp5ChannelsAlpha,
		Format56bpp6ChannelsAlpha,
		Format64bpp7ChannelsAlpha,
		Format72bpp8ChannelsAlpha,
		Format64bpp3ChannelsAlpha,
		Format80bpp4ChannelsAlpha,
		Format96bpp5ChannelsAlpha,
		Format112bpp6ChannelsAlpha,
		Format128bpp7ChannelsAlpha,
		Format144bpp8ChannelsAlpha
	}
	#endregion

	#region enum  TiffCompression
	public enum TiffCompression
	{
		WICTiffCompressionDontCare = 0,
		WICTiffCompressionNone = 0x1,
		WICTiffCompressionCCITT3 = 0x2,
		WICTiffCompressionCCITT4 = 0x3,
		WICTiffCompressionLZW = 0x4,
		WICTiffCompressionRLE = 0x5,
		WICTiffCompressionZIP = 0x6,
		WICTIFFCOMPRESSIONOPTION_FORCE_DWORD = 0x7fffffff
	}
	#endregion

	internal class Misc
	{
		#region GetPixelFormat()
		internal static ImageProcessing.PixelsFormat GetPixelsFormat(Guid guid)
		{
			if (guid == Consts.GUID_WICPixelFormat24bppBGR)
				return ImageProcessing.PixelsFormat.Format24bppRgb;
			if (guid == Consts.GUID_WICPixelFormat1bppIndexed || guid == Consts.GUID_WICPixelFormatBlackWhite)
				return ImageProcessing.PixelsFormat.FormatBlackWhite;
			if (guid == Consts.GUID_WICPixelFormat8bppGray)
				return ImageProcessing.PixelsFormat.Format8bppGray;
			if (guid == Consts.GUID_WICPixelFormat8bppIndexed)
				return ImageProcessing.PixelsFormat.Format8bppIndexed;
			if (guid == Consts.GUID_WICPixelFormat32bppBGR || guid == Consts.GUID_WICPixelFormat32bppBGRA)
				return ImageProcessing.PixelsFormat.Format32bppRgb;
			if (guid == Consts.GUID_WICPixelFormat4bppIndexed || guid == Consts.GUID_WICPixelFormat4bppGray)
				return ImageProcessing.PixelsFormat.Format4bppGray;

			throw new Exception("Unsupported pixels format '" + guid.ToString() + "'! ");
		}
		#endregion

		#region GetPixelFormatGUID()
		internal static Guid GetPixelFormatGUID(ImageProcessing.PixelsFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				case ImageProcessing.PixelsFormat.Format24bppRgb:		return Consts.GUID_WICPixelFormat24bppBGR;
				case ImageProcessing.PixelsFormat.FormatBlackWhite:		return Consts.GUID_WICPixelFormatBlackWhite;
				case ImageProcessing.PixelsFormat.Format8bppGray:		return Consts.GUID_WICPixelFormat8bppGray;
				case ImageProcessing.PixelsFormat.Format8bppIndexed:	return Consts.GUID_WICPixelFormat8bppIndexed;
				case ImageProcessing.PixelsFormat.Format32bppRgb:		return Consts.GUID_WICPixelFormat32bppBGR;
				case ImageProcessing.PixelsFormat.Format4bppGray:		return Consts.GUID_WICPixelFormat4bppGray;
			}

			throw new Exception("Unsupported pixel format '" + pixelFormat.ToString() + "'! ");
		}
		#endregion
	
		#region GetPixelsFormat()
		internal static PixelFormat GetPixelFormat(Guid guid)
		{
			if(guid ==  Consts.GUID_WICPixelFormatDontCare            ) return PixelFormat.FormatDontCare            	;
			if(guid ==  Consts.GUID_WICPixelFormat1bppIndexed         ) return PixelFormat.Format1bppIndexed         	;
			if(guid ==  Consts.GUID_WICPixelFormat2bppIndexed         ) return PixelFormat.Format2bppIndexed         	;
			if(guid ==  Consts.GUID_WICPixelFormat4bppIndexed         ) return PixelFormat.Format4bppIndexed         	;
			if(guid ==  Consts.GUID_WICPixelFormat8bppIndexed         ) return PixelFormat.Format8bppIndexed         	;
			if(guid ==  Consts.GUID_WICPixelFormatBlackWhite          ) return PixelFormat.FormatBlackWhite          	;
			if(guid ==  Consts.GUID_WICPixelFormat2bppGray            ) return PixelFormat.Format2bppGray            	;
			if(guid ==  Consts.GUID_WICPixelFormat4bppGray            ) return PixelFormat.Format4bppGray            	;
			if(guid ==  Consts.GUID_WICPixelFormat8bppGray            ) return PixelFormat.Format8bppGray            	;
			if(guid ==  Consts.GUID_WICPixelFormat16bppBGR555         ) return PixelFormat.Format16bppBGR555         	;
			if(guid ==  Consts.GUID_WICPixelFormat16bppBGR565         ) return PixelFormat.Format16bppBGR565         	;
			if(guid ==  Consts.GUID_WICPixelFormat16bppGray           ) return PixelFormat.Format16bppGray           	;
			if(guid ==  Consts.GUID_WICPixelFormat24bppBGR            ) return PixelFormat.Format24bppBGR            	;
			if(guid ==  Consts.GUID_WICPixelFormat24bppRGB            ) return PixelFormat.Format24bppRGB            	;
			if(guid ==  Consts.GUID_WICPixelFormat32bppBGR            ) return PixelFormat.Format32bppBGR            	;
			if(guid ==  Consts.GUID_WICPixelFormat32bppBGRA           ) return PixelFormat.Format32bppBGRA           	;
			if(guid ==  Consts.GUID_WICPixelFormat32bppPBGRA          ) return PixelFormat.Format32bppPBGRA          	;
			if(guid ==  Consts.GUID_WICPixelFormat32bppGrayFloat      ) return PixelFormat.Format32bppGrayFloat      	;
			if(guid ==  Consts.GUID_WICPixelFormat48bppRGBFixedPoint  ) return PixelFormat.Format48bppRGBFixedPoint  	;
			if(guid ==  Consts.GUID_WICPixelFormat16bppGrayFixedPoint ) return PixelFormat.Format16bppGrayFixedPoint 	;
			if(guid ==  Consts.GUID_WICPixelFormat32bppBGR101010      ) return PixelFormat.Format32bppBGR101010      	;
			if(guid ==  Consts.GUID_WICPixelFormat48bppRGB            ) return PixelFormat.Format48bppRGB            	;
			if(guid ==  Consts.GUID_WICPixelFormat64bppRGBA           ) return PixelFormat.Format64bppRGBA           	;
			if(guid ==  Consts.GUID_WICPixelFormat64bppPRGBA          ) return PixelFormat.Format64bppPRGBA          	;
			if(guid ==  Consts.GUID_WICPixelFormat96bppRGBFixedPoint  ) return PixelFormat.Format96bppRGBFixedPoint  	;
			if(guid ==  Consts.GUID_WICPixelFormat128bppRGBAFloat     ) return PixelFormat.Format128bppRGBAFloat     	;
			if(guid ==  Consts.GUID_WICPixelFormat128bppPRGBAFloat    ) return PixelFormat.Format128bppPRGBAFloat    	;
			if(guid ==  Consts.GUID_WICPixelFormat128bppRGBFloat      ) return PixelFormat.Format128bppRGBFloat      	;
			if(guid ==  Consts.GUID_WICPixelFormat32bppCMYK           ) return PixelFormat.Format32bppCMYK           	;
			if(guid ==  Consts.GUID_WICPixelFormat64bppRGBAFixedPoint ) return PixelFormat.Format64bppRGBAFixedPoint 	;
			if(guid ==  Consts.GUID_WICPixelFormat64bppRGBFixedPoint  ) return PixelFormat.Format64bppRGBFixedPoint  	;
			if(guid ==  Consts.GUID_WICPixelFormat128bppRGBAFixedPoint) return PixelFormat.Format128bppRGBAFixedPoint	;
			if(guid ==  Consts.GUID_WICPixelFormat128bppRGBFixedPoint ) return PixelFormat.Format128bppRGBFixedPoint 	;
			if(guid ==  Consts.GUID_WICPixelFormat64bppRGBAHalf       ) return PixelFormat.Format64bppRGBAHalf       	;
			if(guid ==  Consts.GUID_WICPixelFormat64bppRGBHalf        ) return PixelFormat.Format64bppRGBHalf        	;
			if(guid ==  Consts.GUID_WICPixelFormat48bppRGBHalf        ) return PixelFormat.Format48bppRGBHalf        	;
			if(guid ==  Consts.GUID_WICPixelFormat32bppRGBE           ) return PixelFormat.Format32bppRGBE           	;
			if(guid ==  Consts.GUID_WICPixelFormat16bppGrayHalf       ) return PixelFormat.Format16bppGrayHalf       	;
			if(guid ==  Consts.GUID_WICPixelFormat32bppGrayFixedPoint ) return PixelFormat.Format32bppGrayFixedPoint 	;
			if(guid ==  Consts.GUID_WICPixelFormat64bppCMYK           ) return PixelFormat.Format64bppCMYK           	;
			if(guid ==  Consts.GUID_WICPixelFormat24bpp3Channels      ) return PixelFormat.Format24bpp3Channels      	;
			if(guid ==  Consts.GUID_WICPixelFormat32bpp4Channels      ) return PixelFormat.Format32bpp4Channels      	;
			if(guid ==  Consts.GUID_WICPixelFormat40bpp5Channels      ) return PixelFormat.Format40bpp5Channels      	;
			if(guid ==  Consts.GUID_WICPixelFormat48bpp6Channels      ) return PixelFormat.Format48bpp6Channels      	;
			if(guid ==  Consts.GUID_WICPixelFormat56bpp7Channels      ) return PixelFormat.Format56bpp7Channels      	;
			if(guid ==  Consts.GUID_WICPixelFormat64bpp8Channels      ) return PixelFormat.Format64bpp8Channels      	;
			if(guid ==  Consts.GUID_WICPixelFormat48bpp3Channels      ) return PixelFormat.Format48bpp3Channels      	;
			if(guid ==  Consts.GUID_WICPixelFormat64bpp4Channels      ) return PixelFormat.Format64bpp4Channels      	;
			if(guid ==  Consts.GUID_WICPixelFormat80bpp5Channels      ) return PixelFormat.Format80bpp5Channels      	;
			if(guid ==  Consts.GUID_WICPixelFormat96bpp6Channels      ) return PixelFormat.Format96bpp6Channels      	;
			if(guid ==  Consts.GUID_WICPixelFormat112bpp7Channels     ) return PixelFormat.Format112bpp7Channels     	;
			if(guid ==  Consts.GUID_WICPixelFormat128bpp8Channels     ) return PixelFormat.Format128bpp8Channels     	;
			if(guid ==  Consts.GUID_WICPixelFormat40bppCMYKAlpha      ) return PixelFormat.Format40bppCMYKAlpha      	;
			if(guid ==  Consts.GUID_WICPixelFormat80bppCMYKAlpha      ) return PixelFormat.Format80bppCMYKAlpha      	;
			if(guid ==  Consts.GUID_WICPixelFormat32bpp3ChannelsAlpha ) return PixelFormat.Format32bpp3ChannelsAlpha 	;
			if(guid ==  Consts.GUID_WICPixelFormat40bpp4ChannelsAlpha ) return PixelFormat.Format40bpp4ChannelsAlpha 	;
			if(guid ==  Consts.GUID_WICPixelFormat48bpp5ChannelsAlpha ) return PixelFormat.Format48bpp5ChannelsAlpha 	;
			if(guid ==  Consts.GUID_WICPixelFormat56bpp6ChannelsAlpha ) return PixelFormat.Format56bpp6ChannelsAlpha 	;
			if(guid ==  Consts.GUID_WICPixelFormat64bpp7ChannelsAlpha ) return PixelFormat.Format64bpp7ChannelsAlpha 	;
			if(guid ==  Consts.GUID_WICPixelFormat72bpp8ChannelsAlpha ) return PixelFormat.Format72bpp8ChannelsAlpha 	;
			if(guid ==  Consts.GUID_WICPixelFormat64bpp3ChannelsAlpha ) return PixelFormat.Format64bpp3ChannelsAlpha 	;
			if(guid ==  Consts.GUID_WICPixelFormat80bpp4ChannelsAlpha ) return PixelFormat.Format80bpp4ChannelsAlpha 	;
			if(guid ==  Consts.GUID_WICPixelFormat96bpp5ChannelsAlpha ) return PixelFormat.Format96bpp5ChannelsAlpha 	;
			if(guid ==  Consts.GUID_WICPixelFormat112bpp6ChannelsAlpha) return PixelFormat.Format112bpp6ChannelsAlpha	;
			if(guid ==  Consts.GUID_WICPixelFormat128bpp7ChannelsAlpha) return PixelFormat.Format128bpp7ChannelsAlpha	;
            if(guid ==  Consts.GUID_WICPixelFormat144bpp8ChannelsAlpha ) return PixelFormat.Format144bpp8ChannelsAlpha ;                 			
			return PixelFormat.FormatDontCare;				throw new Exception("Unsupported pixel format '" + guid.ToString() + "'! ");
		}
		#endregion

		#region GetStride()
		internal static uint GetStride(uint width, uint bitCount)
		{
			int stride = (int)Math.Ceiling((width * bitCount) / 8.0);
			stride = ((stride + sizeof(uint) - 1) / sizeof(uint)) * sizeof(uint);

			return (uint)stride;
			//return ((width * bitCount + 7) / 8);
		}
		#endregion

		#region GetBitsPerPixel()
		internal static uint GetBitsPerPixel(ImageProcessing.PixelsFormat pixelsFormat)
		{
			switch (pixelsFormat)
			{
				case ImageProcessing.PixelsFormat.Format32bppRgb: return 32;
				case ImageProcessing.PixelsFormat.Format24bppRgb: return 24;
				case ImageProcessing.PixelsFormat.Format8bppIndexed: return 8;
				case ImageProcessing.PixelsFormat.Format8bppGray: return 8;
				case ImageProcessing.PixelsFormat.Format4bppGray: return 4;
				//Format1bppIndexed = PixelFormat.Format1bppIndexed,
				case ImageProcessing.PixelsFormat.FormatBlackWhite: return 1;
			}

			return 24;
		}
		#endregion

		#region IsIndexedBitmap()
		internal static bool IsIndexedBitmap(ImageProcessing.PixelsFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				//case ImageComponent.PixelFormat.FormatBlackWhite:
				case ImageProcessing.PixelsFormat.Format4bppGray:
				case ImageProcessing.PixelsFormat.Format8bppIndexed:
				case ImageProcessing.PixelsFormat.Format8bppGray:
					return true;
				default:
					return false;
			}
		}
		#endregion

		#region GetPalette()
		/*internal static BitmapPalette GetPalette(ImageProcessing.PixelsFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				case ImageProcessing.PixelsFormat.FormatBlackWhite: return BitmapPalettes.BlackAndWhite;
				case ImageProcessing.PixelsFormat.Format4bppGray: return BitmapPalettes.Gray4;
				case ImageProcessing.PixelsFormat.Format8bppIndexed: return BitmapPalettes.Gray256;
				case ImageProcessing.PixelsFormat.Format8bppGray: return BitmapPalettes.Gray256;
				default: return BitmapPalettes.Halftone256;
			}
		}*/
		#endregion

		#region GetPalette()
		internal static IWICPalette GetPalette(IWICImagingFactory piFactory, PixelFormat pixelFormat) 
		{ 
			IWICPalette		palette = piFactory.CreatePalette();

			switch(pixelFormat)
			{
				case PixelFormat.Format1bppIndexed : 
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedBW, false); break;
				case PixelFormat.FormatBlackWhite:
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedBW, false); break;
				case PixelFormat.Format2bppIndexed :
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedGray4, false); break;
				case PixelFormat.Format2bppGray:
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedGray4, false); break;
				case PixelFormat.Format4bppIndexed :
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedGray16, false); break;
				case PixelFormat.Format4bppGray:
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedGray16, false); break;
				case PixelFormat.Format8bppIndexed :
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedGray256, false); break;
				case PixelFormat.Format8bppGray :
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedGray256, false); break;
				default:
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedGray256, false); break;
			}

			return palette;
		}
		#endregion
	
		#region GetPalette()
		internal static IWICPalette GetPalette(IWICImagingFactory piFactory, IWICBitmapFrameDecode piBitmapFrame, PixelFormat pixelFormat) 
		{
			try
			{
				IWICPalette palette = piFactory.CreatePalette();

				try
				{
					piBitmapFrame.CopyPalette(palette);

					if (palette.GetColorCount() == 0)
						return GetPalette(piFactory, pixelFormat);
					else
						piBitmapFrame.CopyPalette(palette);
				
					return palette;
				}
				catch
				{
					return GetPalette(piFactory, pixelFormat);
				}

			}
			catch
			{
				return null;
			}
		}
		#endregion

		#region GetPalette()
		internal static IWICPalette GetPalette(IWICImagingFactory piFactory, IWICBitmapFrameEncode piBitmapFrame, ImageProcessing.PixelsFormat pixelsFormat)
		{
			IWICPalette palette = piFactory.CreatePalette();

			switch (pixelsFormat)
			{
				case ImageProcessing.PixelsFormat.FormatBlackWhite:
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedBW, false); break;
				case ImageProcessing.PixelsFormat.Format4bppGray:
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedGray16, false); break;
				case ImageProcessing.PixelsFormat.Format8bppIndexed:
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedGray256, false); break;
				case ImageProcessing.PixelsFormat.Format8bppGray:
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedGray256, false); break;
				default:
					palette.InitializePredefined(WICBitmapPaletteType.WICBitmapPaletteTypeFixedGray256, false); break;
			}

			return palette;
		}
		#endregion

		#region enum ErrorCodes
		private enum ErrorCodes : uint
		{
			WINCODEC_ERR_GENERIC_ERROR						= 0x80004005,
			WINCODEC_ERR_INVALIDPARAMETER					= 0x80070057,
			WINCODEC_ERR_OUTOFMEMORY						= 0x8007000E,
			WINCODEC_ERR_NOTIMPLEMENTED						= 0x80004001,
			WINCODEC_ERR_ABORTED							= 0x80004004,
			WINCODEC_ERR_ACCESSDENIED						= 0x80070005,
			WINCODEC_ERR_VALUEOVERFLOW						= 0x80070216,
			WINCODEC_ERR_WRONGSTATE							= 0x88982f93,
			WINCODEC_ERR_VALUEOUTOFRANGE					= 0x88982f05,
			WINCODEC_ERR_UNKNOWNIMAGEFORMAT					= 0x88982f07,
			WINCODEC_ERR_UNSUPPORTEDVERSION					= 0x88982f0B,
			WINCODEC_ERR_NOTINITIALIZED						= 0x88982f0C,
			WINCODEC_ERR_ALREADYLOCKED						= 0x88982f0D,
			WINCODEC_ERR_PROPERTYNOTFOUND					= 0x88982f40,
			WINCODEC_ERR_PROPERTYNOTSUPPORTED				= 0x88982f41,
			WINCODEC_ERR_PROPERTYSIZE						= 0x88982f42,
			WINCODEC_ERR_CODECPRESENT						= 0x88982f43,
			WINCODEC_ERR_CODECNOTHUMBNAIL					= 0x88982f44,
			WINCODEC_ERR_PALETTEUNAVAILABLE					= 0x88982f45,
			WINCODEC_ERR_CODECTOOMANYSCANLINES				= 0x88982f46,
			WINCODEC_ERR_INTERNALERROR						= 0x88982f48,
			WINCODEC_ERR_SOURCERECTDOESNOTMATCHDIMENSIONS	= 0x88982f49,
			WINCODEC_ERR_COMPONENTNOTFOUND					= 0x88982f50,
			WINCODEC_ERR_IMAGESIZEOUTOFRANGE				= 0x88982f51,
			WINCODEC_ERR_TOOMUCHMETADATA					= 0x88982f52,
			WINCODEC_ERR_BADIMAGE							= 0x88982f60,
			WINCODEC_ERR_BADHEADER							= 0x88982f61,
			WINCODEC_ERR_FRAMEMISSING						= 0x88982f62,
			WINCODEC_ERR_BADMETADATAHEADER					= 0x88982f63,
			WINCODEC_ERR_BADSTREAMDATA						= 0x88982f70,
			WINCODEC_ERR_STREAMWRITE						= 0x88982f71,
			WINCODEC_ERR_STREAMREAD							= 0x88982f72,
			WINCODEC_ERR_STREAMNOTAVAILABLE					= 0x88982f73,
			WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT				= 0x88982f80,
			WINCODEC_ERR_UNSUPPORTEDOPERATION				= 0x88982f81,
			WINCODEC_ERR_INVALIDREGISTRATION				= 0x88982f8A,
			WINCODEC_ERR_COMPONENTINITIALIZEFAILURE			= 0x88982f8B,
			WINCODEC_ERR_INSUFFICIENTBUFFER					= 0x88982f8C,
			WINCODEC_ERR_DUPLICATEMETADATAPRESENT			= 0x88982f8D,
			WINCODEC_ERR_PROPERTYUNEXPECTEDTYPE				= 0x88982f8E,
			WINCODEC_ERR_UNEXPECTEDSIZE						= 0x88982f8F,
			WINCODEC_ERR_INVALIDQUERYREQUEST				= 0x88982f90,
			WINCODEC_ERR_UNEXPECTEDMETADATATYPE				= 0x88982f91,
			WINCODEC_ERR_REQUESTONLYVALIDATMETADATAROOT		= 0x88982f92,
			WINCODEC_ERR_INVALIDQUERYCHARACTER				= 0x88982f93
		}
		#endregion

		#region GetErrorMessage()
		static string GetErrorMessage(ErrorCodes errorCode)
		{
			/*ErrorCodes errorCode;
			
			try
			{
				errorCode = (ErrorCodes) error;
			}
			catch { return "Image Component: Unexpected Error!"; }*/

			switch(errorCode)
			{
				case ErrorCodes.WINCODEC_ERR_GENERIC_ERROR: return "Image Component: Generic error!";
				case ErrorCodes.WINCODEC_ERR_INVALIDPARAMETER: return "Image Component: Invalid parameter!";
				case ErrorCodes.WINCODEC_ERR_OUTOFMEMORY: return "Image Component: Out of memory!";
				case ErrorCodes.WINCODEC_ERR_NOTIMPLEMENTED: return "Image Component: Not implemented!";
				case ErrorCodes.WINCODEC_ERR_ABORTED: return "Image Component: Aborted!";
				case ErrorCodes.WINCODEC_ERR_ACCESSDENIED: return "Image Component: Access denied!";
				case ErrorCodes.WINCODEC_ERR_VALUEOVERFLOW: return "Image Component: Value overflow!";
				case ErrorCodes.WINCODEC_ERR_WRONGSTATE: return "Image Component: Wrong state!";
				case ErrorCodes.WINCODEC_ERR_VALUEOUTOFRANGE: return "Image Component: Value out of range!";
				case ErrorCodes.WINCODEC_ERR_UNKNOWNIMAGEFORMAT: return "Image Component: Unknown image format!";
				case ErrorCodes.WINCODEC_ERR_UNSUPPORTEDVERSION: return "Image Component: Unsupported version!";
				case ErrorCodes.WINCODEC_ERR_NOTINITIALIZED: return "Image Component: Not initialized!";
				case ErrorCodes.WINCODEC_ERR_ALREADYLOCKED: return "Image Component: Already locked!";
				case ErrorCodes.WINCODEC_ERR_PROPERTYNOTFOUND: return "Image Component: Property not found!";
				case ErrorCodes.WINCODEC_ERR_PROPERTYNOTSUPPORTED: return "Image Component: Property not supported!";
				case ErrorCodes.WINCODEC_ERR_PROPERTYSIZE: return "Image Component: Property size!";
				case ErrorCodes.WINCODEC_ERR_CODECPRESENT: return "Image Component: Codec present!";
				case ErrorCodes.WINCODEC_ERR_CODECNOTHUMBNAIL: return "Image Component: Codec no thumbnail!";
				case ErrorCodes.WINCODEC_ERR_PALETTEUNAVAILABLE: return "Image Component: Palette unavailable!";
				case ErrorCodes.WINCODEC_ERR_CODECTOOMANYSCANLINES: return "Image Component: Codec to many scan lines!";
				case ErrorCodes.WINCODEC_ERR_INTERNALERROR: return "Image Component: Internal error!";
				case ErrorCodes.WINCODEC_ERR_SOURCERECTDOESNOTMATCHDIMENSIONS: return "Image Component: Source rectangle doedn't match dimensions!";
				case ErrorCodes.WINCODEC_ERR_COMPONENTNOTFOUND: return "Image Component: Component not found!";
				case ErrorCodes.WINCODEC_ERR_IMAGESIZEOUTOFRANGE: return "Image Component: Image size out of range!";
				case ErrorCodes.WINCODEC_ERR_TOOMUCHMETADATA: return "Image Component: Too much metadata!";
				case ErrorCodes.WINCODEC_ERR_BADIMAGE: return "Image Component: Bad image!";
				case ErrorCodes.WINCODEC_ERR_BADHEADER: return "Image Component: Bad header!";
				case ErrorCodes.WINCODEC_ERR_FRAMEMISSING: return "Image Component: Frame missing!";
				case ErrorCodes.WINCODEC_ERR_BADMETADATAHEADER: return "Image Component: Bad metadata header!";
				case ErrorCodes.WINCODEC_ERR_BADSTREAMDATA: return "Image Component: Bad stream data!";
				case ErrorCodes.WINCODEC_ERR_STREAMWRITE: return "Image Component: Error while stream write!";
				case ErrorCodes.WINCODEC_ERR_STREAMREAD: return "Image Component: Error while stream read!";
				case ErrorCodes.WINCODEC_ERR_STREAMNOTAVAILABLE: return "Image Component: Stream not available!";
				case ErrorCodes.WINCODEC_ERR_UNSUPPORTEDPIXELFORMAT: return "Image Component: Unsupported pixel format!";
				case ErrorCodes.WINCODEC_ERR_UNSUPPORTEDOPERATION: return "Image Component: Unsupported operation!";
				case ErrorCodes.WINCODEC_ERR_INVALIDREGISTRATION: return "Image Component: Invalid registration!";
				case ErrorCodes.WINCODEC_ERR_COMPONENTINITIALIZEFAILURE: return "Image Component: Component initialize failure!";
				case ErrorCodes.WINCODEC_ERR_INSUFFICIENTBUFFER: return "Image Component: Insufficient buffer!";
				case ErrorCodes.WINCODEC_ERR_DUPLICATEMETADATAPRESENT: return "Image Component: Duplicate metadata present!";
				case ErrorCodes.WINCODEC_ERR_PROPERTYUNEXPECTEDTYPE: return "Image Component: Property unexpected type!";
				case ErrorCodes.WINCODEC_ERR_UNEXPECTEDSIZE: return "Image Component: Unexpected size!";
				case ErrorCodes.WINCODEC_ERR_INVALIDQUERYREQUEST: return "Image Component: Invalid query request!";
				case ErrorCodes.WINCODEC_ERR_UNEXPECTEDMETADATATYPE: return "Image Component: Unexpected metadata type!";
				case ErrorCodes.WINCODEC_ERR_REQUESTONLYVALIDATMETADATAROOT: return "Image Component: Request only valid at metadata root!";
				//case ErrorCodes.WINCODEC_ERR_INVALIDQUERYCHARACTER: return "Image Component: Invalid query character!";
				default: return "Image Component: Unexpected Error " + errorCode.ToString() + "!";
			}
		}
		#endregion

		#region GetErrorMessage()
		public static string GetErrorMessage(Exception ex)
		{
			foreach (ErrorCodes codes in Enum.GetValues(typeof(ErrorCodes)))
				if (ex.Message.ToLower().Contains(string.Format("0x{0:x8}", (int)codes)))
					return GetErrorMessage(codes);

			string message = ex.Message;

			while ((ex = ex.InnerException) != null)
				message = Environment.NewLine + ex.Message;

			return "Image Component: Unexpected Error! " + message;
		}
		#endregion


	}
}
