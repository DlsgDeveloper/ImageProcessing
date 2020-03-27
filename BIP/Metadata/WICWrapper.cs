
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

// Hiding Interface members in WIC is intentional in this MIDL compiler generated code.
#pragma warning disable 108

namespace BIP.Metadata
{
	// WIC Generated and Wrapped Classes
	public static partial class WICWrapper
	{
		// This allows us to remove CustomMarshallers from our dependencies. Otherwise we need to
		// using System.Runtime.InteropServices.CustomMarshalers;
		public class TypeToTypeInfoMarshaler : ICustomMarshaler
		{
			public void CleanUpManagedData(object ManagedObj)
			{
				throw new NotImplementedException();
			}
			public void CleanUpNativeData(IntPtr pNativeData)
			{
				throw new NotImplementedException();
			}
			public int GetNativeDataSize()
			{
				throw new NotImplementedException();
			}
			public IntPtr MarshalManagedToNative(object ManagedObj)
			{
				throw new NotImplementedException();
			}
			public object MarshalNativeToManaged(IntPtr pNativeData)
			{
				throw new NotImplementedException();
			}
		}

		// WIC Pixel Formats
		public static readonly Guid WICPixelFormat1bppIndexed = new Guid("6fddc324-4e03-4bfe-b185-3d77768dc901");
		public static readonly Guid WICPixelFormatBlackWhite = new Guid("6fddc324-4e03-4bfe-b185-3d77768dc905");
		public static readonly Guid WICPixelFormat8bppIndexed = new Guid("6fddc324-4e03-4bfe-b185-3d77768dc904");
		public static readonly Guid WICPixelFormatDontCare = new Guid("6fddc324-4e03-4bfe-b185-3d77768dc900");
		public static readonly Guid WICPixelFormat24bppBGR = new Guid("6fddc324-4e03-4bfe-b185-3d77768dc90c");
		public static readonly Guid WICPixelFormat32bppBGRA = new Guid("6fddc324-4e03-4bfe-b185-3d77768dc90e");
		public static readonly Guid WICPixelFormat48bppRGB = new Guid("6fddc324-4e03-4bfe-b185-3d77768dc915");

		// Registry Entry CLSID for Categories, CATIDs
		public static readonly string CLSID_WICImagingCategories = "{FAE3D380-FEA4-4623-8C75-C6B61110B681}";
		public static readonly string CATID_WICBitmapDecoders = "{7ED96837-96F0-4812-B211-F13C24117ED3}";
		public static readonly string CATID_WICBitmapEncoders = "{AC757296-3522-4e11-9862-C17BE5A1767E}";
		public static readonly string CATID_WICPixelFormats = "{2B46E70F-CDA7-473e-89F6-DC9630A2390B}";
		public static readonly string CATID_WICFormatConverters = "{7835EAE8-BF14-49d1-93CE-533A407B2248}";
		public static readonly string CATID_WICMetadataHandlers = "{E6A2D7EF-35E6-4f2b-9114-AC36B16C842C}";

		// Encoder CLSIDs
		public static readonly string CLSID_WICBmpEncoder = "{69BE8BB4-D66D-47C8-865A-ED1589433782}";
		public static readonly string CLSID_WICPngEncoder = "{27949969-876A-41D7-9447-568F6A35A4DC}";
		public static readonly string CLSID_WICJpegEncoder = "{1A34F5C1-4A5A-46DC-B644-1F4567E7A676}";
		public static readonly string CLSID_WICGifEncoder = "{114F5598-0B22-40A0-86A1-C83EA495ADBD}";
		public static readonly string CLSID_WICTiffEncoder = "{0131BE10-2001-4C5F-A9B0-CC88FAB64CE8}";
		public static readonly string CLSID_WICWmpEncoder = "{AC4CE3CB-E1C1-44CD-8215-5A1665509EC2}";


		public static Bitmap GetBitmap(string file)
		{
			System.IO.FileInfo fileInfo = new System.IO.FileInfo(file);
			if (WICTools.GDIPlusExtensions.Contains(fileInfo.Extension.ToUpperInvariant()))
				return new Bitmap(file);
			IWICBitmapDecoder decoder = MakeDecoderForFile(fileInfo);
			IWICBitmapFrameDecode frameDecode;
			decoder.GetFrame(0, out frameDecode);
			return CreateBitmapFromBitmapSource(frameDecode);
		}

		private static Bitmap CreateBitmapFromBitmapSource(IWICBitmapSource source)
		{
			Guid pixelFormat;
			uint width, height;
			source.GetPixelFormat(out pixelFormat);
			source.GetSize(out width, out height);

			IWICBitmapSource convertedSource;
			if (!pixelFormat.Equals(WICPixelFormat24bppBGR))
			{
				Guid dstFormat = WICPixelFormat24bppBGR;
				IWICFormatConverter converter;
				WICTools.GetImagingFactory().CreateFormatConverter(out converter);
				converter.Initialize(source, ref dstFormat, WICBitmapDitherType.WICBitmapDitherTypeNone,
									  null, 0.0, WICBitmapPaletteType.WICBitmapPaletteTypeCustom);
				convertedSource = converter;
			}
			else
				convertedSource = source;

			WICRect srcRect = new WICRect();
			srcRect.Width = (int)width;
			srcRect.Height = (int)height;
			srcRect.X = 0;
			srcRect.Y = 0;

			Bitmap bitmap = new Bitmap((int)width, (int)height, PixelFormat.Format24bppRgb);
			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, (int)width, (int)height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
			uint stride = (uint)bitmapData.Stride;
			uint bufferSize = stride * height;
			convertedSource.CopyPixels(ref srcRect, stride, bufferSize, bitmapData.Scan0);
			bitmap.UnlockBits(bitmapData);
			return bitmap;
		}

		private static IWICBitmapDecoder MakeDecoderForFile(System.IO.FileInfo fileInfo)
		{
			IWICStream stream;
			try
			{
				WICTools.GetImagingFactory().CreateStream(out stream);
				stream.InitializeFromFilename(fileInfo.FullName, (uint)WICTools.EFileAccess.GenericRead);
			}
			catch (COMException exc)
			{
				throw new WICException(exc);
			}

			try
			{
				// Attempt to use first decoder matching extension
				IWICBitmapDecoder decoder = WICTools.CreateDecoder(fileInfo.Extension);
				decoder.Initialize(stream, WICDecodeOptions.WICDecodeMetadataCacheOnDemand);
				return decoder;
			}
			catch (COMException)
			{
				// First decoder failed to initialize, try all decoders
				foreach (IWICBitmapDecoder decoder in WICTools.CreateAllDecoders(fileInfo.Extension))
				{
					try
					{
						decoder.Initialize(stream, WICDecodeOptions.WICDecodeMetadataCacheOnDemand);
						return decoder;
					}
					catch (COMException exc)
					{
						throw new WICException(exc);
					}
				}
			}

			throw new Exception(string.Format("Could not find decoder for file of type: {0}", fileInfo.Extension));
		}

		//public static void SaveBitmap( Bitmap bitmap, string file )
		//{
		//   System.IO.FileInfo fileInfo = new System.IO.FileInfo( file );
		//   if( WICTools.GDIPlusExtensions.Contains( fileInfo.Extension.ToUpperInvariant() ) )
		//   {
		//      bitmap.Save( file );
		//      return;
		//   }
		//   IWICBitmapEncoder encoder = MakeEncoderForFile( fileInfo );
		//   IWICBitmapFrameEncode frameEncode;
		//   IPropertyBag2 encoderOptions = null;
		//   encoder.CreateNewFrame( out frameEncode, ref encoderOptions );
		//   return SaveBitmap( frameEncode );
		//}

		//private static IWICBitmapFrameEncode( IWICBitmapFrameEncode source )
		//{
		//   Guid pixelFormat;
		//   uint width, height;
		//   source.GetPixelFormat( out pixelFormat );
		//   source.GetSize( out width, out height );

		//   IWICBitmapSource convertedSource;
		//   if( !pixelFormat.Equals( WICPixelFormat24bppBGR ) )
		//   {
		//      Guid dstFormat = WICPixelFormat24bppBGR;
		//      IWICFormatConverter converter;
		//      WICTools.GetImagingFactory().CreateFormatConverter( out converter );
		//      converter.Initialize( source, ref dstFormat, WICBitmapDitherType.WICBitmapDitherTypeNone,
		//                            null, 0.0, WICBitmapPaletteType.WICBitmapPaletteTypeCustom );
		//      convertedSource = converter;
		//   }
		//   else
		//      convertedSource = source;

		//   WICRect srcRect = new WICRect();
		//   srcRect.Width = (int) width;
		//   srcRect.Height = (int) height;
		//   srcRect.X = 0;
		//   srcRect.Y = 0;

		//   Bitmap bitmap = new Bitmap( (int) width, (int) height, PixelFormat.Format24bppRgb );
		//   BitmapData bitmapData = bitmap.LockBits( new Rectangle( 0, 0, (int) width, (int) height ), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb );
		//   uint stride = (uint) bitmapData.Stride;
		//   uint bufferSize = stride * height;
		//   convertedSource.CopyPixels( ref srcRect, stride, bufferSize, bitmapData.Scan0 );
		//   bitmap.UnlockBits( bitmapData );
		//   return bitmap;
		//}

		private static IWICBitmapEncoder MakeEncoderForFile(System.IO.FileInfo fileInfo)
		{
			IWICStream stream;
			try
			{
				WICTools.GetImagingFactory().CreateStream(out stream);
				stream.InitializeFromFilename(fileInfo.FullName, (uint)WICTools.EFileAccess.GenericWrite);
			}
			catch (COMException exc)
			{
				throw new WICException(exc);
			}

			try
			{
				// Attempt to use first encoder matching extension
				IWICBitmapEncoder encoder = WICTools.CreateEncoder(fileInfo.Extension);
				encoder.Initialize(stream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);
				return encoder;
			}
			catch (COMException)
			{
				// First encoder failed to initialize, try all decoders
				foreach (IWICBitmapEncoder encoder in WICTools.CreateAllEncoders(fileInfo.Extension))
				{
					try
					{
						encoder.Initialize(stream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);
						return encoder;
					}
					catch (COMException exc)
					{
						throw new WICException(exc);
					}
				}
			}

			throw new Exception(string.Format("Could not find decoder for file of type: {0}", fileInfo.Extension));
		}

		public static class WICTools
		{
			// Access dwords for dwDesiredAccess courtesy of:
			// http://www.pinvoke.net/default.aspx/kernel32/CreateFile.html
			[Flags]
			public enum EFileAccess : uint
			{
				GenericRead = 0x80000000,
				GenericWrite = 0x40000000,
				GenericExecute = 0x20000000,
				GenericAll = 0x10000000
			}

			public static readonly List<string> GDIPlusExtensions = new List<string>() { ".JPG", ".JPEG", ".TIF", ".TIFF", ".GIF" };

			private static string s_fileOpenImageFilter = null;
			public static string FileOpenImageFilter
			{
				get
				{
					if (s_fileOpenImageFilter == null)
					{
						System.Text.StringBuilder allImageTypesFilter = new System.Text.StringBuilder("All WIC Image Types|");
						foreach (string extension in GDIPlusExtensions)
							allImageTypesFilter.AppendFormat("*{0};", extension);
						allImageTypesFilter.Remove(allImageTypesFilter.Length - 1, 1);

						System.Text.StringBuilder individualFilters = new System.Text.StringBuilder();
						DecoderInfo[] decoders = GetDecoders();
						for (int iDecoder = 0; iDecoder < decoders.Length; ++iDecoder)
						{
							DecoderInfo decoderInfo = decoders[iDecoder];
							string decoderFilter = decoderInfo.DecoderFileFilter;
							individualFilters.AppendFormat("|{0}", decoderFilter);
							string decoderFilterExtensions = decoderFilter.Substring(decoderFilter.IndexOf("|") + 1);
							allImageTypesFilter.AppendFormat(iDecoder > 0 ? ";{0}" : "{0}", decoderFilterExtensions);
						}
						allImageTypesFilter.Append(individualFilters);
						s_fileOpenImageFilter = allImageTypesFilter.ToString();
					}
					return s_fileOpenImageFilter;
				}
			}

			private static List<string> s_allDecoderExtensions;
			public static List<string> GetExtensionsList()
			{
				if (s_allDecoderExtensions != null)
					return s_allDecoderExtensions;
				s_allDecoderExtensions = new List<string>();
				foreach (string ext in GDIPlusExtensions)
					s_allDecoderExtensions.Add(ext);
				foreach (DecoderInfo decoderInfo in GetDecoders())
					foreach (string extension in decoderInfo.ExtensionList)
						if (!s_allDecoderExtensions.Contains(extension))
							s_allDecoderExtensions.Add(extension);
				return s_allDecoderExtensions;
			}

			[DebuggerDisplay("DecoderInfo[{Name}, {Extensions}]")]
			public class DecoderInfo
			{
				private string m_clsid;
				private string m_name;
				private string m_extList;

				public string CLSID { get { return m_clsid; } }
				public string Name { get { return m_name; } }
				public string Extensions { get { return m_extList; } }

				public string DecoderFileFilter
				{
					get
					{
						System.Text.StringBuilder list = new System.Text.StringBuilder();
						foreach (string extension in ExtensionList)
							list.AppendFormat("*{0};", extension);
						list.Remove(list.Length - 1, 1);
						return string.Format("{0}|{1}", m_name, list.ToString());
					}
				}

				public List<string> ExtensionList
				{
					get
					{
						return new List<string>(m_extList.ToUpperInvariant().Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
					}
				}

				public DecoderInfo(string clsid, RegistryKey key)
				{
					m_clsid = clsid;
					m_extList = (string)key.GetValue("FileExtensions");
					m_name = (string)key.GetValue("FriendlyName");
				}
			}

			public static IEnumerable<IWICBitmapDecoder> CreateAllDecoders(string extension)
			{
				List<IWICBitmapDecoder> decoders = new List<IWICBitmapDecoder>();

				string searchExtension = extension.ToUpperInvariant();
				foreach (DecoderInfo decoderInfo in GetDecoders())
				{
					if (decoderInfo.ExtensionList.Contains(searchExtension))
					{
						Type decoderType = Type.GetTypeFromCLSID(new Guid(decoderInfo.CLSID), true);
						decoders.Add((IWICBitmapDecoder)Activator.CreateInstance(decoderType));
					}
				}
				return decoders;
			}

			[DebuggerDisplay("EncoderInfo[{Name}, {Extensions}]")]
			public class EncoderInfo
			{
				private string m_clsid;
				private string m_name;
				private string m_extList;

				public string CLSID { get { return m_clsid; } }
				public string Name { get { return m_name; } }
				public string Extensions { get { return m_extList; } }

				public string EncoderFileFilter
				{
					get
					{
						System.Text.StringBuilder list = new System.Text.StringBuilder();
						foreach (string extension in ExtensionList)
							list.AppendFormat("*{0};", extension);
						list.Remove(list.Length - 1, 1);
						return string.Format("{0}|{1}", m_name, list.ToString());
					}
				}

				public List<string> ExtensionList
				{
					get
					{
						return new List<string>(m_extList.ToUpperInvariant().Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
					}
				}

				public EncoderInfo(string clsid, string name, string extList)
				{
					m_clsid = clsid;
					m_extList = extList;
					m_name = name;
				}
			}

			public static IEnumerable<IWICBitmapEncoder> CreateAllEncoders(string extension)
			{
				List<IWICBitmapEncoder> encoders = new List<IWICBitmapEncoder>();

				string searchExtension = extension.ToUpperInvariant();
				foreach (EncoderInfo encoderInfo in GetEncoders())
				{
					if (encoderInfo.ExtensionList.Contains(searchExtension))
					{
						Type encoderType = Type.GetTypeFromCLSID(new Guid(encoderInfo.CLSID), true);
						encoders.Add((IWICBitmapEncoder)Activator.CreateInstance(encoderType));
					}
				}
				return encoders;
			}

			public static IWICBitmapDecoder CreateDecoder(string extension)
			{
				string searchExtension = extension.ToUpperInvariant();
				foreach (DecoderInfo decoderInfo in GetDecoders())
				{
					if (decoderInfo.ExtensionList.Contains(searchExtension))
					{
						Type decoderType = Type.GetTypeFromCLSID(new Guid(decoderInfo.CLSID), true);
						return (IWICBitmapDecoder)Activator.CreateInstance(decoderType);
					}
				}
				return null;
			}

			public static IWICBitmapEncoder CreateEncoder(string extension)
			{
				throw new NotImplementedException();
			}

			private static DecoderInfo[] s_decoderInfo = null;
			public static DecoderInfo[] GetDecoders()
			{
				if (s_decoderInfo != null)
					return s_decoderInfo;

				RegistryKey clsid = Registry.ClassesRoot.OpenSubKey("CLSID", false);
				RegistryKey bitmapDecodersKey = clsid.OpenSubKey(CATID_WICBitmapDecoders, false).OpenSubKey("Instance");
				string[] bitmapDecoderNames = bitmapDecodersKey.GetSubKeyNames();
				s_decoderInfo = new DecoderInfo[bitmapDecoderNames.Length];
				for (int iDecoder = 0; iDecoder < s_decoderInfo.Length; ++iDecoder)
				{
					s_decoderInfo[iDecoder] = new DecoderInfo(bitmapDecoderNames[iDecoder],
					   clsid.OpenSubKey(bitmapDecoderNames[iDecoder]));
				}
				return s_decoderInfo;
			}

			private static EncoderInfo[] s_encoderInfo = null;
			private static EncoderInfo[] GetEncoders()
			{
				if (s_encoderInfo != null)
					return s_encoderInfo;

				//RegistryKey clsid = Registry.ClassesRoot.OpenSubKey( "CLSID", false);
				//RegistryKey bitmapEncodersKey = clsid.OpenSubKey( CATID_WICBitmapEncoders, false ).OpenSubKey( "Instance" );
				//string[] bitmapEncoderNames = bitmapEncodersKey.GetSubKeyNames();
				//s_encoderInfo = new EncoderInfo[ bitmapEncoderNames.Length ];
				//for( int iEncoder = 0; iEncoder < s_encoderInfo.Length; ++iEncoder )
				//{
				//   s_encoderInfo[ iEncoder ] = new EncoderInfo( bitmapEncoderNames[ iEncoder ],
				//      clsid.OpenSubKey( bitmapEncoderNames[ iEncoder ] ) );
				//}
				s_encoderInfo = new EncoderInfo[] {
               new EncoderInfo( CLSID_WICBmpEncoder , "Bitmap", "BMP" ),
               new EncoderInfo( CLSID_WICPngEncoder , "Portable Network Graphic", "PNG" ),
               new EncoderInfo( CLSID_WICJpegEncoder, "JPEG", "JPEG, JPG" ),
               new EncoderInfo( CLSID_WICGifEncoder , "Graphic Interchange Format", "GIF" ),
               new EncoderInfo( CLSID_WICTiffEncoder, "Tagged Image File Format", "TIFF" ),
               new EncoderInfo( CLSID_WICWmpEncoder , "Microsoft HD Photo", "WDP" ),
            };
				return s_encoderInfo;
			}


			private static IWICImagingFactory s_wicImagingFactory = null;
			public static IWICImagingFactory GetImagingFactory()
			{
				if (s_wicImagingFactory == null)
				{
					Guid CLSID_WICImagingFactory = new Guid("cacaf262-9370-4615-a13b-9f5539da4c0a");
					Type WICImagingFactoryType = Type.GetTypeFromCLSID(CLSID_WICImagingFactory, true);
					s_wicImagingFactory = (IWICImagingFactory)Activator.CreateInstance(WICImagingFactoryType);
				}
				return s_wicImagingFactory;
			}
		}

		public class WICException : Exception
		{
			public WICException(System.Runtime.InteropServices.COMException exc)
				: base(ErrorForCode(exc.ErrorCode), exc)
			{
			}

			#region Error code LUT
			private static Dictionary<uint, string> s_WICErrorLUT = new Dictionary<uint, string>()
      {
         { 0x88982f04, "WRONGSTATE" },
         { 0x88982f05, "VALUEOUTOFRANGE" },
         { 0x88982f07, "UNKNOWNIMAGEFORMAT" },
         { 0x88982f0B, "UNSUPPORTEDVERSION" },
         { 0x88982f0C, "NOTINITIALIZED" },
         { 0x88982f0D, "ALREADYLOCKED" },
         { 0x88982f40, "PROPERTYNOTFOUND" },
         { 0x88982f41, "PROPERTYNOTSUPPORTED" },
         { 0x88982f42, "PROPERTYSIZE" },
         { 0x88982f43, "CODECPRESENT" },
         { 0x88982f44, "CODECNOTHUMBNAIL" },
         { 0x88982f45, "PALETTEUNAVAILABLE" },
         { 0x88982f46, "CODECTOOMANYSCANLINES" },
         { 0x88982f48, "INTERNALERROR" },
         { 0x88982f49, "SOURCERECTDOESNOTMATCHDIMENSIONS" },
         { 0x88982f50, "COMPONENTNOTFOUND" },
         { 0x88982f51, "IMAGESIZEOUTOFRANGE" },
         { 0x88982f52, "TOOMUCHMETADATA" },
         { 0x88982f60, "BADIMAGE" },
         { 0x88982f61, "BADHEADER" },
         { 0x88982f62, "FRAMEMISSING" },
         { 0x88982f63, "BADMETADATAHEADER" },
         { 0x88982f70, "BADSTREAMDATA" },
         { 0x88982f71, "STREAMWRITE" },
         { 0x88982f72, "STREAMREAD" },
         { 0x88982f73, "STREAMNOTAVAILABLE" },
         { 0x88982f80, "UNSUPPORTEDPIXELFORMAT" },
         { 0x88982f81, "UNSUPPORTEDOPERATION" },
         { 0x88982f8A, "INVALIDREGISTRATION" },
         { 0x88982f8B, "COMPONENTINITIALIZEFAILURE" },
         { 0x88982f8C, "INSUFFICIENTBUFFER" },
         { 0x88982f8D, "DUPLICATEMETADATAPRESENT" },
         { 0x88982f8E, "PROPERTYUNEXPECTEDTYPE" },
         { 0x88982f8F, "UNEXPECTEDSIZE" },
         { 0x88982f90, "INVALIDQUERYREQUEST" },
         { 0x88982f91, "UNEXPECTEDMETADATATYPE" },
         { 0x88982f92, "REQUESTONLYVALIDATMETADATAROOT" },
         { 0x88982f93, "INVALIDQUERYCHARACTER" },
      };
			#endregion

			public static string ErrorForCode(int code)
			{
				string errorString = "WIC Error: " + s_WICErrorLUT[(uint)code];
				return errorString;
			}
		}

		// These are the classes from the Windows Platfork SDK IDL compiled to a TLB
		// using MIDL (Microsoft's Interface Definition Language compiler) and then
		// extracted using Lutz Roeder's .NET Reflector. Additionally, the structures
		// were cleaned up by hand in several passes to convert types calling
		// conventions, fix warnings, remove redundant definitions of interfaces, etc.

		#region Generated structs
		[StructLayout(LayoutKind.Explicit, Size = 8, Pack = 8)]
		public unsafe struct VariantUnion
		{
		}

		[StructLayout(LayoutKind.Explicit, Size = 0x18, Pack = 4)]
		public struct __MIDL_IOleAutomationTypes_0001
		{
		}

		[StructLayout(LayoutKind.Explicit, Size = 0x10, Pack = 8)]
		public struct __MIDL_IOleAutomationTypes_0004
		{
		}

		[StructLayout(LayoutKind.Explicit, Size = 4, Pack = 4)]
		public struct __MIDL_IOleAutomationTypes_0005
		{
		}

		[StructLayout(LayoutKind.Explicit, Size = 4, Pack = 4)]
		public struct __MIDL_IOleAutomationTypes_0006
		{
		}

		[StructLayout(LayoutKind.Explicit, Size = 4, Pack = 4)]
		public struct __MIDL_IWinTypes_0001
		{
		}

		[StructLayout(LayoutKind.Explicit, Size = 8, Pack = 8)]
		public struct __MIDL_IWinTypes_0007
		{
		}

		[StructLayout(LayoutKind.Explicit, Size = 8, Pack = 8)]
		public struct __MIDL_IWinTypes_0008
		{
		}

		[StructLayout(LayoutKind.Explicit, Pack = 4)]
		public struct __MIDL_IWinTypes_0009
		{
			// Fields
			[FieldOffset(0)]
			public int hInproc;
			[FieldOffset(0)]
			public int hRemote;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _BYTE_SIZEDARR
		{
			public uint clSize;
			[ComConversionLoss]
			public IntPtr pData;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct _FILETIME
		{
			public uint dwLowDateTime;
			public uint dwHighDateTime;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _FLAGGED_WORD_BLOB
		{
			public uint fFlags;
			public uint clSize;
			[ComConversionLoss]
			public IntPtr asData;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _HYPER_SIZEDARR
		{
			public uint clSize;
			[ComConversionLoss]
			public IntPtr pData;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct _LARGE_INTEGER
		{
			public long QuadPart;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _LONG_SIZEDARR
		{
			public uint clSize;
			[ComConversionLoss]
			public IntPtr pData;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct _RemotableHandle
		{
			public int fContext;
			public __MIDL_IWinTypes_0009 u;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _SHORT_SIZEDARR
		{
			public uint clSize;
			[ComConversionLoss]
			public IntPtr pData;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct _ULARGE_INTEGER
		{
			public ulong QuadPart;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _userBITMAP
		{
			public int bmType;
			public int bmWidth;
			public int bmHeight;
			public int bmWidthBytes;
			public ushort bmPlanes;
			public ushort bmBitsPixel;
			public uint cbSize;
			[ComConversionLoss]
			public IntPtr pBuffer;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct _userCLIPFORMAT
		{
			public int fContext;
			public __MIDL_IWinTypes_0001 u;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct _userHBITMAP
		{
			public int fContext;
			public __MIDL_IWinTypes_0007 u;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct _userHPALETTE
		{
			public int fContext;
			public __MIDL_IWinTypes_0008 u;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _wireBRECORD
		{
			public uint fFlags;
			public uint clSize;
			[MarshalAs(UnmanagedType.Interface)]
			public IRecordInfo pRecInfo;
			[ComConversionLoss]
			public IntPtr pRecord;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _wireSAFEARR_BRECORD
		{
			public uint Size;
			[ComConversionLoss]
			public IntPtr aRecord;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _wireSAFEARR_BSTR
		{
			public uint Size;
			[ComConversionLoss]
			public IntPtr aBstr;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _wireSAFEARR_DISPATCH
		{
			public uint Size;
			[ComConversionLoss]
			public IntPtr apDispatch;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _wireSAFEARR_HAVEIID
		{
			public uint Size;
			[ComConversionLoss]
			public IntPtr apUnknown;
			public Guid iid;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _wireSAFEARR_UNKNOWN
		{
			public uint Size;
			[ComConversionLoss]
			public IntPtr apUnknown;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _wireSAFEARR_VARIANT
		{
			public uint Size;
			[ComConversionLoss]
			public IntPtr aVariant;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct _wireSAFEARRAY
		{
			public ushort cDims;
			public ushort fFeatures;
			public uint cbElements;
			public uint cLocks;
			public _wireSAFEARRAY_UNION uArrayStructs;
			[ComConversionLoss]
			public IntPtr rgsabound;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct _wireSAFEARRAY_UNION
		{
			public uint sfType;
			public __MIDL_IOleAutomationTypes_0001 u;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct _wireVARIANT
		{
			public uint clSize;
			public uint rpcReserved;
			public ushort vt;
			public ushort wReserved1;
			public ushort wReserved2;
			public ushort wReserved3;
			public __MIDL_IOleAutomationTypes_0004 DUMMYUNIONNAME;
		}
		#endregion

		#region Generated Interfaces
		[ComImport, Guid("0000000D-0000-0000-C000-000000000046"), InterfaceType((short)1)]
		public interface IEnumSTATSTG
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteNext([In] uint celt, out tagSTATSTG rgelt, out uint pceltFetched);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Skip([In] uint celt);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Reset();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumSTATSTG ppenum);
		}

		[ComImport, InterfaceType((short)1), Guid("00000101-0000-0000-C000-000000000046")]
		public interface IEnumString
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteNext([In] uint celt, [MarshalAs(UnmanagedType.LPWStr)] out string rgelt, out uint pceltFetched);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Skip([In] uint celt);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Reset();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumString ppenum);
		}

		[ComImport, Guid("00000100-0000-0000-C000-000000000046"), InterfaceType((short)1)]
		public interface IEnumUnknown
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteNext([In] uint celt, [MarshalAs(UnmanagedType.IUnknown)] out object rgelt, out uint pceltFetched);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Skip([In] uint celt);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Reset();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumUnknown ppenum);
		}

		[ComImport, Guid("3127CA40-446E-11CE-8135-00AA004BB851"), InterfaceType((short)1)]
		public interface IErrorLog
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void AddError([In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName, [In] ref System.Runtime.InteropServices.ComTypes.EXCEPINFO pExcepInfo);
		}

		[ComImport, Guid("0000010C-0000-0000-C000-000000000046"), InterfaceType((short)1)]
		public interface IPersist
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetClassID(out Guid pClassID);
		}

		[ComImport, InterfaceType((short)1), Guid("00000109-0000-0000-C000-000000000046")]
		public interface IPersistStream : IPersist
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetClassID(out Guid pClassID);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void IsDirty();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Load([In, MarshalAs(UnmanagedType.Interface)] IStream pstm);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Save([In, MarshalAs(UnmanagedType.Interface)] IStream pstm, [In] int fClearDirty);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSizeMax(out _ULARGE_INTEGER pcbSize);
		}

		[ComImport, InterfaceType((short)1), Guid("22F55882-280B-11D0-A8A9-00A0C90C2004")]
		public interface IPropertyBag2
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Read([In] uint cProperties, [In] ref tagPROPBAG2 pPropBag, [In, MarshalAs(UnmanagedType.Interface)] IErrorLog pErrLog, [MarshalAs(UnmanagedType.Struct)] out object pvarValue, [In, Out, MarshalAs(UnmanagedType.Error)] ref int phrError);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Write([In] uint cProperties, [In] ref tagPROPBAG2 pPropBag, [In, MarshalAs(UnmanagedType.Struct)] ref object pvarValue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CountProperties(out uint pcProperties);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPropertyInfo([In] uint iProperty, [In] uint cProperties, out tagPROPBAG2 pPropBag, out uint pcProperties);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void LoadObject([In, MarshalAs(UnmanagedType.LPWStr)] string pstrName, [In] uint dwHint, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnkObject, [In, MarshalAs(UnmanagedType.Interface)] IErrorLog pErrLog);
		}

		[ComImport, InterfaceType((short)1), Guid("0000002F-0000-0000-C000-000000000046")]
		public interface IRecordInfo
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RecordInit([Out] IntPtr pvNew);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RecordClear([In] IntPtr pvExisting);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RecordCopy([In] IntPtr pvExisting, [Out] IntPtr pvNew);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetGuid(out Guid pguid);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetName([MarshalAs(UnmanagedType.BStr)] out string pbstrName);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSize(out uint pcbSize);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetTypeInfo([MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "", MarshalTypeRef = typeof(TypeToTypeInfoMarshaler), MarshalCookie = "")] out Type ppTypeInfo);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetField([In] IntPtr pvData, [In, MarshalAs(UnmanagedType.LPWStr)] string szFieldName, [MarshalAs(UnmanagedType.Struct)] out object pvarField);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFieldNoCopy([In] IntPtr pvData, [In, MarshalAs(UnmanagedType.LPWStr)] string szFieldName, [MarshalAs(UnmanagedType.Struct)] out object pvarField, out IntPtr ppvDataCArray);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void PutField([In] uint wFlags, [In, Out] IntPtr pvData, [In, MarshalAs(UnmanagedType.LPWStr)] string szFieldName, [In, MarshalAs(UnmanagedType.Struct)] ref object pvarField);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void PutFieldNoCopy([In] uint wFlags, [In, Out] IntPtr pvData, [In, MarshalAs(UnmanagedType.LPWStr)] string szFieldName, [In, MarshalAs(UnmanagedType.Struct)] ref object pvarField);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFieldNames([In, Out] ref uint pcNames, [MarshalAs(UnmanagedType.BStr)] out string rgBstrNames);
			[PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			int IsMatchingType([In, MarshalAs(UnmanagedType.Interface)] IRecordInfo pRecordInfo);
			[PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			IntPtr RecordCreate();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RecordCreateCopy([In] IntPtr pvSource, out IntPtr ppvDest);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RecordDestroy([In] IntPtr pvRecord);
		}

		[ComImport, InterfaceType((short)1), Guid("0C733A30-2A1C-11CE-ADE5-00AA0044773D")]
		public interface ISequentialStream
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteRead(out byte pv, [In] uint cb, out uint pcbRead);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteWrite([In] ref byte pv, [In] uint cb, out uint pcbWritten);
		}

		[ComImport, Guid("0000000B-0000-0000-C000-000000000046"), InterfaceType((short)1)]
		public interface IStorage
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateStream([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName, [In] uint grfMode, [In] uint reserved1, [In] uint reserved2, [MarshalAs(UnmanagedType.Interface)] out IStream ppstm);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteOpenStream([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName, [In] uint cbReserved1, [In] ref byte reserved1, [In] uint grfMode, [In] uint reserved2, [MarshalAs(UnmanagedType.Interface)] out IStream ppstm);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateStorage([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName, [In] uint grfMode, [In] uint reserved1, [In] uint reserved2, [MarshalAs(UnmanagedType.Interface)] out IStorage ppstg);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void OpenStorage([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName, [In, MarshalAs(UnmanagedType.Interface)] IStorage pstgPriority, [In] uint grfMode, [In, ComAliasName("WIC.wireSNB")] ref tagRemSNB snbExclude, [In] uint reserved, [MarshalAs(UnmanagedType.Interface)] out IStorage ppstg);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteCopyTo([In] uint ciidExclude, [In] ref Guid rgiidExclude, [In, ComAliasName("WIC.wireSNB")] ref tagRemSNB snbExclude, [In, MarshalAs(UnmanagedType.Interface)] IStorage pstgDest);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void MoveElementTo([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName, [In, MarshalAs(UnmanagedType.Interface)] IStorage pstgDest, [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsNewName, [In] uint grfFlags);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Commit([In] uint grfCommitFlags);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Revert();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteEnumElements([In] uint reserved1, [In] uint cbReserved2, [In] ref byte reserved2, [In] uint reserved3, [MarshalAs(UnmanagedType.Interface)] out IEnumSTATSTG ppenum);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DestroyElement([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RenameElement([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsOldName, [In, MarshalAs(UnmanagedType.LPWStr)] string pwcsNewName);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetElementTimes([In, MarshalAs(UnmanagedType.LPWStr)] string pwcsName, [In] ref _FILETIME pctime, [In] ref _FILETIME patime, [In] ref _FILETIME pmtime);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetClass([In] ref Guid clsid);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetStateBits([In] uint grfStateBits, [In] uint grfMask);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Stat(out tagSTATSTG pstatstg, [In] uint grfStatFlag);
		}

		[ComImport, Guid("0000000C-0000-0000-C000-000000000046"), InterfaceType((short)1)]
		public interface IStream : ISequentialStream
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteRead(out byte pv, [In] uint cb, out uint pcbRead);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteWrite([In] ref byte pv, [In] uint cb, out uint pcbWritten);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteSeek([In] _LARGE_INTEGER dlibMove, [In] uint dwOrigin, out _ULARGE_INTEGER plibNewPosition);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetSize([In] _ULARGE_INTEGER libNewSize);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteCopyTo([In, MarshalAs(UnmanagedType.Interface)] IStream pstm, [In] _ULARGE_INTEGER cb, out _ULARGE_INTEGER pcbRead, out _ULARGE_INTEGER pcbWritten);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Commit([In] uint grfCommitFlags);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Revert();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void LockRegion([In] _ULARGE_INTEGER libOffset, [In] _ULARGE_INTEGER cb, [In] uint dwLockType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void UnlockRegion([In] _ULARGE_INTEGER libOffset, [In] _ULARGE_INTEGER cb, [In] uint dwLockType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Stat(out tagSTATSTG pstatstg, [In] uint grfStatFlag);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Clone([MarshalAs(UnmanagedType.Interface)] out IStream ppstm);
		}

		[ComImport, Guid("00020403-0000-0000-C000-000000000046"), ComConversionLoss, InterfaceType((short)1)]
		public interface ITypeComp
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteBind([In, MarshalAs(UnmanagedType.LPWStr)] string szName, [In] uint lHashVal, [In] ushort wFlags, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "", MarshalTypeRef = typeof(TypeToTypeInfoMarshaler), MarshalCookie = "")] out Type ppTInfo, out tagDESCKIND pDescKind, [Out] IntPtr ppFuncDesc, [Out] IntPtr ppVarDesc, [MarshalAs(UnmanagedType.Interface)] out ITypeComp ppTypeComp, [ComAliasName("WIC.DWORD")] out uint pDummy);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteBindType([In, MarshalAs(UnmanagedType.LPWStr)] string szName, [In] uint lHashVal, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "", MarshalTypeRef = typeof(TypeToTypeInfoMarshaler), MarshalCookie = "")] out Type ppTInfo);
		}

		[ComImport, InterfaceType((short)1), ComConversionLoss, Guid("00020401-0000-0000-C000-000000000046")]
		public interface ITypeInfo
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteGetTypeAttr([Out] IntPtr ppTypeAttr, [ComAliasName("WIC.DWORD")] out uint pDummy);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetTypeComp([MarshalAs(UnmanagedType.Interface)] out ITypeComp ppTComp);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteGetFuncDesc([In] uint index, [Out] IntPtr ppFuncDesc, [ComAliasName("WIC.DWORD")] out uint pDummy);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteGetVarDesc([In] uint index, [Out] IntPtr ppVarDesc, [ComAliasName("WIC.DWORD")] out uint pDummy);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteGetNames([In] int memid, [MarshalAs(UnmanagedType.BStr)] out string rgBstrNames, [In] uint cMaxNames, out uint pcNames);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetRefTypeOfImplType([In] uint index, out uint pRefType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetImplTypeFlags([In] uint index, out int pImplTypeFlags);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void LocalGetIDsOfNames();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void LocalInvoke();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteGetDocumentation([In] int memid, [In] uint refPtrFlags, [MarshalAs(UnmanagedType.BStr)] out string pbstrName, [MarshalAs(UnmanagedType.BStr)] out string pBstrDocString, out uint pdwHelpContext, [MarshalAs(UnmanagedType.BStr)] out string pBstrHelpFile);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteGetDllEntry([In] int memid, [In] tagINVOKEKIND invkind, [In] uint refPtrFlags, [MarshalAs(UnmanagedType.BStr)] out string pBstrDllName, [MarshalAs(UnmanagedType.BStr)] out string pbstrName, out ushort pwOrdinal);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetRefTypeInfo([In] uint hreftype, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "", MarshalTypeRef = typeof(TypeToTypeInfoMarshaler), MarshalCookie = "")] out Type ppTInfo);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void LocalAddressOfMember();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteCreateInstance([In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObj);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMops([In] int memid, [MarshalAs(UnmanagedType.BStr)] out string pBstrMops);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteGetContainingTypeLib([MarshalAs(UnmanagedType.Interface)] out ITypeLib ppTLib, out uint pIndex);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void LocalReleaseTypeAttr();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void LocalReleaseFuncDesc();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void LocalReleaseVarDesc();
		}

		[ComImport, Guid("00020402-0000-0000-C000-000000000046"), InterfaceType((short)1), ComConversionLoss]
		public interface ITypeLib
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteGetTypeInfoCount(out uint pcTInfo);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetTypeInfo([In] uint index, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "", MarshalTypeRef = typeof(TypeToTypeInfoMarshaler), MarshalCookie = "")] out Type ppTInfo);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetTypeInfoType([In] uint index, out tagTYPEKIND pTKind);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetTypeInfoOfGuid([In] ref Guid guid, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "", MarshalTypeRef = typeof(TypeToTypeInfoMarshaler), MarshalCookie = "")] out Type ppTInfo);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteGetLibAttr([Out] IntPtr ppTLibAttr, [ComAliasName("WIC.DWORD")] out uint pDummy);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetTypeComp([MarshalAs(UnmanagedType.Interface)] out ITypeComp ppTComp);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteGetDocumentation([In] int index, [In] uint refPtrFlags, [MarshalAs(UnmanagedType.BStr)] out string pbstrName, [MarshalAs(UnmanagedType.BStr)] out string pBstrDocString, out uint pdwHelpContext, [MarshalAs(UnmanagedType.BStr)] out string pBstrHelpFile);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteIsName([In, MarshalAs(UnmanagedType.LPWStr)] string szNameBuf, [In] uint lHashVal, out int pfName, [MarshalAs(UnmanagedType.BStr)] out string pBstrLibName);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteFindName([In, MarshalAs(UnmanagedType.LPWStr)] string szNameBuf, [In] uint lHashVal, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "", MarshalTypeRef = typeof(TypeToTypeInfoMarshaler), MarshalCookie = "")] out Type ppTInfo, out int rgMemId, [In, Out] ref ushort pcFound, [MarshalAs(UnmanagedType.BStr)] out string pBstrLibName);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void LocalReleaseTLibAttr();
		}

		[ComImport, Guid("00000121-A8F2-4877-BA0A-FD2B6645FB94"), InterfaceType((short)1)]
		public interface IWICBitmap : IWICBitmapSource
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSize(out uint puiWidth, out uint puiHeight);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormat([ComAliasName("WIC.WICPixelFormatGUID")] out Guid pPixelFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetResolution(out double pDpiX, out double pDpiY);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPixels([In] ref WICRect prc, [In] uint cbStride, [In] uint cbBufferSize, IntPtr pBuffer);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Lock([In] ref WICRect prcLock, [In] uint flags, [MarshalAs(UnmanagedType.Interface)] out IWICBitmapLock ppILock);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetResolution([In] double dpiX, [In] double dpiY);
		}

		[ComImport, Guid("E4FBCF03-223D-4E81-9333-D635556DD1B5"), InterfaceType((short)1)]
		public interface IWICBitmapClipper : IWICBitmapSource
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSize(out uint puiWidth, out uint puiHeight);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormat([ComAliasName("WIC.WICPixelFormatGUID")] out Guid pPixelFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetResolution(out double pDpiX, out double pDpiY);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPixels([In] ref WICRect prc, [In] uint cbStride, [In] uint cbBufferSize, IntPtr pBuffer);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Initialize([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pISource, [In] ref WICRect prc);
		}

		[ComImport, InterfaceType((short)1), Guid("E87A44C4-B76E-4C47-8B09-298EB12A2714")]
		public interface IWICBitmapCodecInfo : IWICComponentInfo
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetComponentType(out WICComponentType pType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCLSID(out Guid pclsid);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSigningStatus(out uint pStatus);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetAuthor([In] uint cchAuthor, [In, Out] ref ushort wzAuthor, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVendorGUID(out Guid pguidVendor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVersion([In] uint cchVersion, [In, Out] ref ushort wzVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSpecVersion([In] uint cchSpecVersion, [In, Out] ref ushort wzSpecVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFriendlyName([In] uint cchFriendlyName, [In, Out] ref ushort wzFriendlyName, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContainerFormat(out Guid pguidContainerFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormats([In] uint cFormats, [In, Out] ref Guid pguidPixelFormats, out uint pcActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetColorManagementVersion([In] uint cchColorManagementVersion, [In, Out] ref ushort wzColorManagementVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDeviceManufacturer([In] uint cchDeviceManufacturer, [In, Out] ref ushort wzDeviceManufacturer, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDeviceModels([In] uint cchDeviceModels, [In, Out] ref ushort wzDeviceModels, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMimeTypes([In] uint cchMimeTypes, [In, Out] ref ushort wzMimeTypes, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFileExtensions([In] uint cchFileExtensions, [In, Out] ref ushort wzFileExtensions, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportAnimation(out int pfSupportAnimation);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportChromakey(out int pfSupportChromakey);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportLossless(out int pfSupportLossless);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportMultiframe(out int pfSupportMultiframe);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void MatchesMimeType([In, MarshalAs(UnmanagedType.LPWStr)] string wzMimeType, out int pfMatches);
		}

		[ComImport, InterfaceType((short)1), Guid("64C1024E-C3CF-4462-8078-88C2B11C46D9")]
		public interface IWICBitmapCodecProgressNotification
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void __MIDL__IWICBitmapCodecProgressNotification0000(IntPtr pvData, uint uFrameNum, WICProgressOperation operation, double dblProgress);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RegisterProgressNotification([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapCodecProgressNotification pfnProgressNotification, [In] IntPtr pvData, [In] uint dwProgressFlags);
		}

		[ComImport, InterfaceType((short)1), Guid("9EDDE9E7-8DEE-47EA-99DF-E6FAF2ED44BF")]
		public interface IWICBitmapDecoder
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void QueryCapability([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, out uint pdwCapability);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Initialize([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] WICDecodeOptions cacheOptions);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContainerFormat(out Guid pguidContainerFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDecoderInfo([MarshalAs(UnmanagedType.Interface)] out IWICBitmapDecoderInfo ppIDecoderInfo);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataQueryReader([MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryReader ppIMetadataQueryReader);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPreview([MarshalAs(UnmanagedType.Interface)] out IWICBitmapSource ppIBitmapSource);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetColorContexts([In] uint cCount, [In, Out, MarshalAs(UnmanagedType.Interface)] ref IWICColorContext ppIColorContexts, out uint pcActualCount);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetThumbnail([MarshalAs(UnmanagedType.Interface)] out IWICBitmapSource ppIThumbnail);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFrameCount(out uint pCount);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFrame([In] uint index, [MarshalAs(UnmanagedType.Interface)] out IWICBitmapFrameDecode ppIBitmapFrame);
		}

		[ComImport, Guid("D8CD007F-D08F-4191-9BFC-236EA7F0E4B5"), InterfaceType((short)1)]
		public interface IWICBitmapDecoderInfo : IWICBitmapCodecInfo
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetComponentType(out WICComponentType pType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCLSID(out Guid pclsid);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSigningStatus(out uint pStatus);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetAuthor([In] uint cchAuthor, [In, Out] ref ushort wzAuthor, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVendorGUID(out Guid pguidVendor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVersion([In] uint cchVersion, [In, Out] ref ushort wzVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSpecVersion([In] uint cchSpecVersion, [In, Out] ref ushort wzSpecVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFriendlyName([In] uint cchFriendlyName, [In, Out] ref ushort wzFriendlyName, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContainerFormat(out Guid pguidContainerFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormats([In] uint cFormats, [In, Out] ref Guid pguidPixelFormats, out uint pcActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetColorManagementVersion([In] uint cchColorManagementVersion, [In, Out] ref ushort wzColorManagementVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDeviceManufacturer([In] uint cchDeviceManufacturer, [In, Out] ref ushort wzDeviceManufacturer, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDeviceModels([In] uint cchDeviceModels, [In, Out] ref ushort wzDeviceModels, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMimeTypes([In] uint cchMimeTypes, [In, Out] ref ushort wzMimeTypes, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFileExtensions([In] uint cchFileExtensions, [In, Out] ref ushort wzFileExtensions, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportAnimation(out int pfSupportAnimation);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportChromakey(out int pfSupportChromakey);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportLossless(out int pfSupportLossless);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportMultiframe(out int pfSupportMultiframe);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void MatchesMimeType([In, MarshalAs(UnmanagedType.LPWStr)] string wzMimeType, out int pfMatches);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPatterns([In] uint cbSizePatterns, [In, Out] ref WICBitmapPattern pPatterns, [In, Out] ref uint pcPatterns, [In, Out] ref uint pcbPatternsActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void MatchesPattern([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, out int pfMatches);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateInstance([MarshalAs(UnmanagedType.Interface)] out IWICBitmapDecoder ppIBitmapDecoder);
		}

		[ComImport, Guid("00000103-A8F2-4877-BA0A-FD2B6645FB94"), InterfaceType((short)1)]
		public interface IWICBitmapEncoder
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Initialize([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] WICBitmapEncoderCacheOption cacheOption);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContainerFormat(out Guid pguidContainerFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetEncoderInfo([MarshalAs(UnmanagedType.Interface)] out IWICBitmapEncoderInfo ppIEncoderInfo);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetColorContexts([In] uint cCount, [In, MarshalAs(UnmanagedType.Interface)] ref IWICColorContext ppIColorContext);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetThumbnail([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pIThumbnail);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetPreview([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pIPreview);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateNewFrame([MarshalAs(UnmanagedType.Interface)] out IWICBitmapFrameEncode ppIFrameEncode, [In, Out, MarshalAs(UnmanagedType.Interface)] ref IPropertyBag2 ppIEncoderOptions);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Commit();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataQueryWriter([MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryWriter ppIMetadataQueryWriter);
		}

		[ComImport, InterfaceType((short)1), Guid("94C9B4EE-A09F-4F92-8A1E-4A9BCE7E76FB")]
		public interface IWICBitmapEncoderInfo : IWICBitmapCodecInfo
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetComponentType(out WICComponentType pType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCLSID(out Guid pclsid);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSigningStatus(out uint pStatus);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetAuthor([In] uint cchAuthor, [In, Out] ref ushort wzAuthor, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVendorGUID(out Guid pguidVendor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVersion([In] uint cchVersion, [In, Out] ref ushort wzVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSpecVersion([In] uint cchSpecVersion, [In, Out] ref ushort wzSpecVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFriendlyName([In] uint cchFriendlyName, [In, Out] ref ushort wzFriendlyName, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContainerFormat(out Guid pguidContainerFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormats([In] uint cFormats, [In, Out] ref Guid pguidPixelFormats, out uint pcActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetColorManagementVersion([In] uint cchColorManagementVersion, [In, Out] ref ushort wzColorManagementVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDeviceManufacturer([In] uint cchDeviceManufacturer, [In, Out] ref ushort wzDeviceManufacturer, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDeviceModels([In] uint cchDeviceModels, [In, Out] ref ushort wzDeviceModels, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMimeTypes([In] uint cchMimeTypes, [In, Out] ref ushort wzMimeTypes, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFileExtensions([In] uint cchFileExtensions, [In, Out] ref ushort wzFileExtensions, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportAnimation(out int pfSupportAnimation);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportChromakey(out int pfSupportChromakey);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportLossless(out int pfSupportLossless);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportMultiframe(out int pfSupportMultiframe);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void MatchesMimeType([In, MarshalAs(UnmanagedType.LPWStr)] string wzMimeType, out int pfMatches);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateInstance([MarshalAs(UnmanagedType.Interface)] out IWICBitmapEncoder ppIBitmapEncoder);
		}

		[ComImport, InterfaceType((short)1), Guid("5009834F-2D6A-41CE-9E1B-17C5AFF7A782")]
		public interface IWICBitmapFlipRotator : IWICBitmapSource
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSize(out uint puiWidth, out uint puiHeight);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormat([ComAliasName("WIC.WICPixelFormatGUID")] out Guid pPixelFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetResolution(out double pDpiX, out double pDpiY);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPixels([In] ref WICRect prc, [In] uint cbStride, [In] uint cbBufferSize, IntPtr pBuffer);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Initialize([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pISource, [In] WICBitmapTransformOptions options);
		}

		[ComImport, Guid("3B16811B-6A43-4EC9-A813-3D930C13B940"), InterfaceType((short)1)]
		public interface IWICBitmapFrameDecode : IWICBitmapSource
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSize(out uint puiWidth, out uint puiHeight);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormat([ComAliasName("WIC.WICPixelFormatGUID")] out Guid pPixelFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetResolution(out double pDpiX, out double pDpiY);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPixels([In] ref WICRect prc, [In] uint cbStride, [In] uint cbBufferSize, IntPtr pBuffer);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataQueryReader([MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryReader ppIMetadataQueryReader);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetColorContexts([In] uint cCount, [In, Out, MarshalAs(UnmanagedType.Interface)] ref IWICColorContext ppIColorContexts, out uint pcActualCount);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetThumbnail([MarshalAs(UnmanagedType.Interface)] out IWICBitmapSource ppIThumbnail);
		}

		[ComImport, InterfaceType((short)1), Guid("00000105-A8F2-4877-BA0A-FD2B6645FB94")]
		public interface IWICBitmapFrameEncode
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Initialize([In, MarshalAs(UnmanagedType.Interface)] IPropertyBag2 pIEncoderOptions);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetSize([In] uint uiWidth, [In] uint uiHeight);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetResolution([In] double dpiX, [In] double dpiY);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetPixelFormat([In, Out, ComAliasName("WIC.WICPixelFormatGUID")] ref Guid pPixelFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetColorContexts([In] uint cCount, [In, MarshalAs(UnmanagedType.Interface)] ref IWICColorContext ppIColorContext);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetThumbnail([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pIThumbnail);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void WritePixels([In] uint lineCount, [In] uint cbStride, [In] uint cbBufferSize, [In] ref byte pbPixels);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void WriteSource([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pIBitmapSource, [In] ref WICRect prc);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Commit();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataQueryWriter([MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryWriter ppIMetadataQueryWriter);
		}

		[ComImport, Guid("00000123-A8F2-4877-BA0A-FD2B6645FB94"), InterfaceType((short)1), ComConversionLoss]
		public interface IWICBitmapLock
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSize(out uint puiWidth, out uint puiHeight);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetStride(out uint pcbStride);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void remote_shouldnt_be_called_GetDataPointer(out uint pcbBufferSize, [Out] IntPtr ppbData);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormat([ComAliasName("WIC.WICPixelFormatGUID")] out Guid pPixelFormat);
		}

		[ComImport, InterfaceType((short)1), Guid("00000302-A8F2-4877-BA0A-FD2B6645FB94")]
		public interface IWICBitmapScaler : IWICBitmapSource
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSize(out uint puiWidth, out uint puiHeight);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormat([ComAliasName("WIC.WICPixelFormatGUID")] out Guid pPixelFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetResolution(out double pDpiX, out double pDpiY);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPixels([In] ref WICRect prc, [In] uint cbStride, [In] uint cbBufferSize, IntPtr pBuffer);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Initialize([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pISource, [In] uint uiWidth, [In] uint uiHeight, [In] WICBitmapInterpolationMode mode);
		}

		[ComImport, Guid("00000120-A8F2-4877-BA0A-FD2B6645FB94"), InterfaceType((short)1)]
		public interface IWICBitmapSource
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSize(out uint puiWidth, out uint puiHeight);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormat([ComAliasName("WIC.WICPixelFormatGUID")] out Guid pPixelFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetResolution(out double pDpiX, out double pDpiY);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPixels([In] ref WICRect prc, [In] uint cbStride, [In] uint cbBufferSize, IntPtr pBuffer);
		}

		[ComImport, Guid("3B16811B-6A43-4EC9-B713-3D5A0C13B940"), InterfaceType((short)1)]
		public interface IWICBitmapSourceTransform
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPixels([In] ref WICRect prcSrc, [In] uint uiWidth, [In] uint uiHeight, [In, ComAliasName("WIC.WICPixelFormatGUID")] ref Guid pguidDstFormat, [In] WICBitmapTransformOptions dstTransform, [In] uint nStride, [In] uint cbBufferSize, IntPtr pBuffer);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetClosestSize([In, Out] ref uint puiWidth, [In, Out] ref uint puiHeight);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetClosestPixelFormat([In, Out, ComAliasName("WIC.WICPixelFormatGUID")] ref Guid pguidDstFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportTransform([In] WICBitmapTransformOptions dstTransform, out int pfIsSupported);
		}

		[ComImport, InterfaceType((short)1), Guid("3C613A02-34B2-44EA-9A7C-45AEA9C6FD6D")]
		public interface IWICColorContext
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void InitializeFromFilename([In, MarshalAs(UnmanagedType.LPWStr)] string wzFilename);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void InitializeFromMemory([In] ref byte pbBuffer, [In] uint cbBufferSize);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void InitializeFromExifColorSpace([In] uint value);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetType(out WICColorContextType pType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetProfileBytes([In] uint cbBuffer, [In, Out] ref byte pbBuffer, out uint pcbActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetExifColorSpace(out uint pValue);
		}

		[ComImport, InterfaceType((short)1), Guid("B66F034F-D0E2-40AB-B436-6DE39E321A94")]
		public interface IWICColorTransform : IWICBitmapSource
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSize(out uint puiWidth, out uint puiHeight);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormat([ComAliasName("WIC.WICPixelFormatGUID")] out Guid pPixelFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetResolution(out double pDpiX, out double pDpiY);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPixels([In] ref WICRect prc, [In] uint cbStride, [In] uint cbBufferSize, IntPtr pBuffer);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Initialize([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pIBitmapSource, [In, MarshalAs(UnmanagedType.Interface)] IWICColorContext pIContextSource, [In, MarshalAs(UnmanagedType.Interface)] IWICColorContext pIContextDest, [In, ComAliasName("WIC.REFWICPixelFormatGUID")] ref Guid pixelFmtDest);
		}

		[ComImport, InterfaceType((short)1), Guid("412D0C3A-9650-44FA-AF5B-DD2A06C8E8FB")]
		public interface IWICComponentFactory : IWICImagingFactory
		{
			[return: MarshalAs(UnmanagedType.Interface)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			IWICBitmapDecoder CreateDecoderFromFilename([In, MarshalAs(UnmanagedType.LPWStr)] string wzFilename, [In] ref Guid pguidVendor, [In] uint dwDesiredAccess, [In] WICDecodeOptions metadataOptions);
			[return: MarshalAs(UnmanagedType.Interface)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			IWICBitmapDecoder CreateDecoderFromStream([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] ref Guid pguidVendor, [In] WICDecodeOptions metadataOptions);
			[return: MarshalAs(UnmanagedType.Interface)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			IWICBitmapDecoder CreateDecoderFromFileHandle([In, ComAliasName("WIC.ULONG_PTR")] uint hFile, [In] ref Guid pguidVendor, [In] WICDecodeOptions metadataOptions);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateComponentInfo([In] ref Guid clsidComponent, [MarshalAs(UnmanagedType.Interface)] out IWICComponentInfo ppIInfo);
			[return: MarshalAs(UnmanagedType.Interface)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			IWICBitmapDecoder CreateDecoder([In] ref Guid guidContainerFormat, [In] ref Guid pguidVendor);
			[return: MarshalAs(UnmanagedType.Interface)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			IWICBitmapEncoder CreateEncoder([In] ref Guid guidContainerFormat, [In] ref Guid pguidVendor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreatePalette([MarshalAs(UnmanagedType.Interface)] out IWICPalette ppIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateFormatConverter([MarshalAs(UnmanagedType.Interface)] out IWICFormatConverter ppIFormatConverter);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapScaler([MarshalAs(UnmanagedType.Interface)] out IWICBitmapScaler ppIBitmapScaler);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapClipper([MarshalAs(UnmanagedType.Interface)] out IWICBitmapClipper ppIBitmapClipper);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapFlipRotator([MarshalAs(UnmanagedType.Interface)] out IWICBitmapFlipRotator ppIBitmapFlipRotator);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateStream([MarshalAs(UnmanagedType.Interface)] out IWICStream ppIWICStream);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateColorContext([MarshalAs(UnmanagedType.Interface)] out IWICColorContext ppIWICColorContext);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateColorTransformer([MarshalAs(UnmanagedType.Interface)] out IWICColorTransform ppIWICColorTransform);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmap([In] uint uiWidth, [In] uint uiHeight, [In, ComAliasName("WIC.REFWICPixelFormatGUID")] ref Guid pixelFormat, [In] WICBitmapCreateCacheOption option, [MarshalAs(UnmanagedType.Interface)] out IWICBitmap ppIBitmap);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapFromSource([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pIBitmapSource, [In] WICBitmapCreateCacheOption option, [MarshalAs(UnmanagedType.Interface)] out IWICBitmap ppIBitmap);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapFromSourceRect([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pIBitmapSource, [In] uint X, [In] uint Y, [In] uint Width, [In] uint Height, [MarshalAs(UnmanagedType.Interface)] out IWICBitmap ppIBitmap);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapFromMemory([In] uint uiWidth, [In] uint uiHeight, [In, ComAliasName("WIC.REFWICPixelFormatGUID")] ref Guid pixelFormat, [In] uint cbStride, [In] uint cbBufferSize, [In] ref byte pbBuffer, [MarshalAs(UnmanagedType.Interface)] out IWICBitmap ppIBitmap);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapFromHBITMAP([In, ComAliasName("WIC.wireHBITMAP")] ref _userHBITMAP hBitmap, [In, ComAliasName("WIC.wireHPALETTE")] ref _userHPALETTE hPalette, [In] WICBitmapAlphaChannelOption options, [MarshalAs(UnmanagedType.Interface)] out IWICBitmap ppIBitmap);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapFromHICON([In, ComAliasName("WIC.wireHICON")] ref _RemotableHandle hIcon, [MarshalAs(UnmanagedType.Interface)] out IWICBitmap ppIBitmap);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateComponentEnumerator([In] uint componentTypes, [In] uint options, [MarshalAs(UnmanagedType.Interface)] out IEnumUnknown ppIEnumUnknown);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateFastMetadataEncoderFromDecoder([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapDecoder pIDecoder, [MarshalAs(UnmanagedType.Interface)] out IWICFastMetadataEncoder ppIFastEncoder);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateFastMetadataEncoderFromFrameDecode([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapFrameDecode pIFrameDecoder, [MarshalAs(UnmanagedType.Interface)] out IWICFastMetadataEncoder ppIFastEncoder);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateQueryWriter([In] ref Guid guidMetadataFormat, [In] ref Guid pguidVendor, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryWriter ppIQueryWriter);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateQueryWriterFromReader([In, MarshalAs(UnmanagedType.Interface)] IWICMetadataQueryReader pIQueryReader, [In] ref Guid pguidVendor, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryWriter ppIQueryWriter);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateMetadataReader([In] ref Guid guidMetadataFormat, [In] ref Guid pguidVendor, [In] uint dwOptions, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataReader ppIReader);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateMetadataReaderFromContainer([In] ref Guid guidContainerFormat, [In] ref Guid pguidVendor, [In] uint dwOptions, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataReader ppIReader);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateMetadataWriter([In] ref Guid guidMetadataFormat, [In] ref Guid pguidVendor, [In] uint dwMetadataOptions, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataWriter ppIWriter);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateMetadataWriterFromReader([In, MarshalAs(UnmanagedType.Interface)] IWICMetadataReader pIReader, [In] ref Guid pguidVendor, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataWriter ppIWriter);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateQueryReaderFromBlockReader([In, MarshalAs(UnmanagedType.Interface)] IWICMetadataBlockReader pIBlockReader, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryReader ppIQueryReader);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateQueryWriterFromBlockWriter([In, MarshalAs(UnmanagedType.Interface)] IWICMetadataBlockWriter pIBlockWriter, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryWriter ppIQueryWriter);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateEncoderPropertyBag([In] ref tagPROPBAG2 ppropOptions, [In] uint cCount, [MarshalAs(UnmanagedType.Interface)] out IPropertyBag2 ppIPropertyBag);
		}

		[ComImport, Guid("23BC3F0A-698B-4357-886B-F24D50671334"), InterfaceType((short)1)]
		public interface IWICComponentInfo
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetComponentType(out WICComponentType pType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCLSID(out Guid pclsid);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSigningStatus(out uint pStatus);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetAuthor([In] uint cchAuthor, [In, Out] ref ushort wzAuthor, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVendorGUID(out Guid pguidVendor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVersion([In] uint cchVersion, [In, Out] ref ushort wzVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSpecVersion([In] uint cchSpecVersion, [In, Out] ref ushort wzSpecVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFriendlyName([In] uint cchFriendlyName, [In, Out] ref ushort wzFriendlyName, out uint pcchActual);
		}

		[ComImport, Guid("FBEC5E44-F7BE-4B65-B7F8-C0C81FEF026D"), InterfaceType((short)1)]
		public interface IWICDevelopRaw : IWICBitmapFrameDecode
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSize(out uint puiWidth, out uint puiHeight);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormat([ComAliasName("WIC.WICPixelFormatGUID")] out Guid pPixelFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetResolution(out double pDpiX, out double pDpiY);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPixels([In] ref WICRect prc, [In] uint cbStride, [In] uint cbBufferSize, IntPtr pBuffer);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataQueryReader([MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryReader ppIMetadataQueryReader);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetColorContexts([In] uint cCount, [In, Out, MarshalAs(UnmanagedType.Interface)] ref IWICColorContext ppIColorContexts, out uint pcActualCount);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetThumbnail([MarshalAs(UnmanagedType.Interface)] out IWICBitmapSource ppIThumbnail);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void QueryRawCapabilitiesInfo(out WICRawCapabilitiesInfo pInfo);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void LoadParameterSet([In] WICRawParameterSet ParameterSet);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCurrentParameterSet([MarshalAs(UnmanagedType.Interface)] out IPropertyBag2 ppCurrentParameterSet);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetExposureCompensation([In] double ev);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetExposureCompensation(out double pEV);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetWhitePointRGB([In] uint Red, [In] uint Green, [In] uint Blue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetWhitePointRGB(out uint pRed, out uint pGreen, out uint pBlue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetNamedWhitePoint([In] WICNamedWhitePoint WhitePoint);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetNamedWhitePoint(out WICNamedWhitePoint pWhitePoint);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetWhitePointKelvin([In] uint WhitePointKelvin);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetWhitePointKelvin(out uint pWhitePointKelvin);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetKelvinRangeInfo(out uint pMinKelvinTemp, out uint pMaxKelvinTemp, out uint pKelvinTempStepValue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetContrast([In] double Contrast);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContrast(out double pContrast);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetGamma([In] double Gamma);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetGamma(out double pGamma);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetSharpness([In] double Sharpness);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSharpness(out double pSharpness);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetSaturation([In] double Saturation);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSaturation(out double pSaturation);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetTint([In] double Tint);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetTint(out double pTint);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetNoiseReduction([In] double NoiseReduction);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetNoiseReduction(out double pNoiseReduction);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetDestinationColorContext([In, MarshalAs(UnmanagedType.Interface)] IWICColorContext pColorContext);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetToneCurve([In] uint cbToneCurveSize, [In] ref WICRawToneCurve pToneCurve);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetToneCurve([In] uint cbToneCurveBufferSize, out WICRawToneCurve pToneCurve, out uint pcbActualToneCurveBufferSize);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetRotation([In] double Rotation);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetRotation(out double pRotation);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetRenderMode([In] WICRawRenderMode RenderMode);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetRenderMode(out WICRawRenderMode pRenderMode);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetNotificationCallback([In, MarshalAs(UnmanagedType.Interface)] IWICDevelopRawNotificationCallback pCallback);
		}

		[ComImport, InterfaceType((short)1), Guid("95C75A6E-3E8C-4EC2-85A8-AEBCC551E59B")]
		public interface IWICDevelopRawNotificationCallback
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Notify([In] uint NotificationMask);
		}

		[ComImport, InterfaceType((short)1), Guid("DC2BB46D-3F07-481E-8625-220C4AEDBB33")]
		public interface IWICEnumMetadataItem
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Next([In] uint celt, [In, Out] ref tag_inner_PROPVARIANT rgeltSchema, [In, Out] ref tag_inner_PROPVARIANT rgeltId, [In, Out, Optional] ref tag_inner_PROPVARIANT rgeltValue, [Optional] out uint pceltFetched);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Skip([In] uint celt);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Reset();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Clone([MarshalAs(UnmanagedType.Interface)] out IWICEnumMetadataItem ppIEnumMetadataItem);
		}

		[ComImport, InterfaceType((short)1), Guid("B84E2C09-78C9-4AC4-8BD3-524AE1663A2F")]
		public interface IWICFastMetadataEncoder
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Commit();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataQueryWriter([MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryWriter ppIMetadataQueryWriter);
		}

		[ComImport, Guid("00000301-A8F2-4877-BA0A-FD2B6645FB94"), InterfaceType((short)1)]
		public interface IWICFormatConverter : IWICBitmapSource
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSize(out uint puiWidth, out uint puiHeight);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormat([ComAliasName("WIC.WICPixelFormatGUID")] out Guid pPixelFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetResolution(out double pDpiX, out double pDpiY);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CopyPixels([In] ref WICRect prc, [In] uint cbStride, [In] uint cbBufferSize, IntPtr pBuffer);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Initialize([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pISource, [In, ComAliasName("WIC.REFWICPixelFormatGUID")] ref Guid dstFormat, [In] WICBitmapDitherType dither, [In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette, [In] double alphaThresholdPercent, [In] WICBitmapPaletteType paletteTranslate);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CanConvert([In, ComAliasName("WIC.REFWICPixelFormatGUID")] ref Guid srcPixelFormat, [In, ComAliasName("WIC.REFWICPixelFormatGUID")] ref Guid dstPixelFormat, out int pfCanConvert);
		}

		[ComImport, Guid("9F34FB65-13F4-4F15-BC57-3726B5E53D9F"), InterfaceType((short)1)]
		public interface IWICFormatConverterInfo : IWICComponentInfo
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetComponentType(out WICComponentType pType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCLSID(out Guid pclsid);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSigningStatus(out uint pStatus);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetAuthor([In] uint cchAuthor, [In, Out] ref ushort wzAuthor, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVendorGUID(out Guid pguidVendor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVersion([In] uint cchVersion, [In, Out] ref ushort wzVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSpecVersion([In] uint cchSpecVersion, [In, Out] ref ushort wzSpecVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFriendlyName([In] uint cchFriendlyName, [In, Out] ref ushort wzFriendlyName, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPixelFormats([In] uint cFormats, [In, Out, ComAliasName("WIC.WICPixelFormatGUID")] ref Guid pPixelFormatGUIDs, out uint pcActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateInstance([MarshalAs(UnmanagedType.Interface)] out IWICFormatConverter ppIConverter);
		}

		[ComImport, InterfaceType((short)1), Guid("EC5EC8A9-C395-4314-9C77-54D7A935FF70")]
		public interface IWICImagingFactory
		{
			[return: MarshalAs(UnmanagedType.Interface)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			IWICBitmapDecoder CreateDecoderFromFilename([In, MarshalAs(UnmanagedType.LPWStr)] string wzFilename, [In] ref Guid pguidVendor, [In] uint dwDesiredAccess, [In] WICDecodeOptions metadataOptions);
			[return: MarshalAs(UnmanagedType.Interface)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			IWICBitmapDecoder CreateDecoderFromStream([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] ref Guid pguidVendor, [In] WICDecodeOptions metadataOptions);
			[return: MarshalAs(UnmanagedType.Interface)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			IWICBitmapDecoder CreateDecoderFromFileHandle([In, ComAliasName("WIC.ULONG_PTR")] uint hFile, [In] ref Guid pguidVendor, [In] WICDecodeOptions metadataOptions);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateComponentInfo([In] ref Guid clsidComponent, [MarshalAs(UnmanagedType.Interface)] out IWICComponentInfo ppIInfo);
			[return: MarshalAs(UnmanagedType.Interface)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			IWICBitmapDecoder CreateDecoder([In] ref Guid guidContainerFormat, [In] ref Guid pguidVendor);
			[return: MarshalAs(UnmanagedType.Interface)]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			IWICBitmapEncoder CreateEncoder([In] ref Guid guidContainerFormat, [In] ref Guid pguidVendor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreatePalette([MarshalAs(UnmanagedType.Interface)] out IWICPalette ppIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateFormatConverter([MarshalAs(UnmanagedType.Interface)] out IWICFormatConverter ppIFormatConverter);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapScaler([MarshalAs(UnmanagedType.Interface)] out IWICBitmapScaler ppIBitmapScaler);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapClipper([MarshalAs(UnmanagedType.Interface)] out IWICBitmapClipper ppIBitmapClipper);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapFlipRotator([MarshalAs(UnmanagedType.Interface)] out IWICBitmapFlipRotator ppIBitmapFlipRotator);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateStream([MarshalAs(UnmanagedType.Interface)] out IWICStream ppIWICStream);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateColorContext([MarshalAs(UnmanagedType.Interface)] out IWICColorContext ppIWICColorContext);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateColorTransformer([MarshalAs(UnmanagedType.Interface)] out IWICColorTransform ppIWICColorTransform);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmap([In] uint uiWidth, [In] uint uiHeight, [In, ComAliasName("WIC.REFWICPixelFormatGUID")] ref Guid pixelFormat, [In] WICBitmapCreateCacheOption option, [MarshalAs(UnmanagedType.Interface)] out IWICBitmap ppIBitmap);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapFromSource([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pIBitmapSource, [In] WICBitmapCreateCacheOption option, [MarshalAs(UnmanagedType.Interface)] out IWICBitmap ppIBitmap);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapFromSourceRect([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pIBitmapSource, [In] uint X, [In] uint Y, [In] uint Width, [In] uint Height, [MarshalAs(UnmanagedType.Interface)] out IWICBitmap ppIBitmap);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapFromMemory([In] uint uiWidth, [In] uint uiHeight, [In, ComAliasName("WIC.REFWICPixelFormatGUID")] ref Guid pixelFormat, [In] uint cbStride, [In] uint cbBufferSize, [In] ref byte pbBuffer, [MarshalAs(UnmanagedType.Interface)] out IWICBitmap ppIBitmap);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapFromHBITMAP([In, ComAliasName("WIC.wireHBITMAP")] ref _userHBITMAP hBitmap, [In, ComAliasName("WIC.wireHPALETTE")] ref _userHPALETTE hPalette, [In] WICBitmapAlphaChannelOption options, [MarshalAs(UnmanagedType.Interface)] out IWICBitmap ppIBitmap);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateBitmapFromHICON([In, ComAliasName("WIC.wireHICON")] ref _RemotableHandle hIcon, [MarshalAs(UnmanagedType.Interface)] out IWICBitmap ppIBitmap);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateComponentEnumerator([In] uint componentTypes, [In] uint options, [MarshalAs(UnmanagedType.Interface)] out IEnumUnknown ppIEnumUnknown);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateFastMetadataEncoderFromDecoder([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapDecoder pIDecoder, [MarshalAs(UnmanagedType.Interface)] out IWICFastMetadataEncoder ppIFastEncoder);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateFastMetadataEncoderFromFrameDecode([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapFrameDecode pIFrameDecoder, [MarshalAs(UnmanagedType.Interface)] out IWICFastMetadataEncoder ppIFastEncoder);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateQueryWriter([In] ref Guid guidMetadataFormat, [In] ref Guid pguidVendor, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryWriter ppIQueryWriter);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateQueryWriterFromReader([In, MarshalAs(UnmanagedType.Interface)] IWICMetadataQueryReader pIQueryReader, [In] ref Guid pguidVendor, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataQueryWriter ppIQueryWriter);
		}

		[ComImport, InterfaceType((short)1), Guid("FEAA2A8D-B3F3-43E4-B25C-D1DE990A1AE1")]
		public interface IWICMetadataBlockReader
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContainerFormat(out Guid pguidContainerFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCount(out uint pcCount);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetReaderByIndex([In] uint nIndex, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataReader ppIMetadataReader);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetEnumerator([MarshalAs(UnmanagedType.Interface)] out IEnumUnknown ppIEnumMetadata);
		}

		[ComImport, Guid("08FB9676-B444-41E8-8DBE-6A53A542BFF1"), InterfaceType((short)1)]
		public interface IWICMetadataBlockWriter : IWICMetadataBlockReader
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContainerFormat(out Guid pguidContainerFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCount(out uint pcCount);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetReaderByIndex([In] uint nIndex, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataReader ppIMetadataReader);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetEnumerator([MarshalAs(UnmanagedType.Interface)] out IEnumUnknown ppIEnumMetadata);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void InitializeFromBlockReader([In, MarshalAs(UnmanagedType.Interface)] IWICMetadataBlockReader pIMDBlockReader);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetWriterByIndex([In] uint nIndex, [MarshalAs(UnmanagedType.Interface)] out IWICMetadataWriter ppIMetadataWriter);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void AddWriter([In, MarshalAs(UnmanagedType.Interface)] IWICMetadataWriter pIMetadataWriter);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetWriterByIndex([In] uint nIndex, [In, MarshalAs(UnmanagedType.Interface)] IWICMetadataWriter pIMetadataWriter);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoveWriterByIndex([In] uint nIndex);
		}

		[ComImport, Guid("ABA958BF-C672-44D1-8D61-CE6DF2E682C2"), InterfaceType((short)1)]
		public interface IWICMetadataHandlerInfo : IWICComponentInfo
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetComponentType(out WICComponentType pType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCLSID(out Guid pclsid);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSigningStatus(out uint pStatus);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetAuthor([In] uint cchAuthor, [In, Out] ref ushort wzAuthor, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVendorGUID(out Guid pguidVendor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVersion([In] uint cchVersion, [In, Out] ref ushort wzVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSpecVersion([In] uint cchSpecVersion, [In, Out] ref ushort wzSpecVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFriendlyName([In] uint cchFriendlyName, [In, Out] ref ushort wzFriendlyName, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataFormat(out Guid pguidMetadataFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContainerFormats([In] uint cContainerFormats, [In, Out] ref Guid pguidContainerFormats, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDeviceManufacturer([In] uint cchDeviceManufacturer, [In, Out] ref ushort wzDeviceManufacturer, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDeviceModels([In] uint cchDeviceModels, [In, Out] ref ushort wzDeviceModels, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesRequireFullStream(out int pfRequiresFullStream);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportPadding(out int pfSupportsPadding);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesRequireFixedSize(out int pfFixedSize);
		}

		[ComImport, InterfaceType((short)1), Guid("30989668-E1C9-4597-B395-458EEDB808DF")]
		public interface IWICMetadataQueryReader
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContainerFormat(out Guid pguidContainerFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetLocation([In] uint cchMaxLength, [In, Out] ref ushort wzNamespace, out uint pcchActualLength);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataByName([In, MarshalAs(UnmanagedType.LPWStr)] string wzName, [In, Out] ref tag_inner_PROPVARIANT pvarValue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataByName([In, MarshalAs(UnmanagedType.LPWStr)] string wzName, [In, Out] ref PROPVARIANT pvarValue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetEnumerator([MarshalAs(UnmanagedType.Interface)] out IEnumString ppIEnumString);
		}

		[ComImport, InterfaceType((short)1), Guid("A721791A-0DEF-4D06-BD91-2118BF1DB10B")]
		public interface IWICMetadataQueryWriter : IWICMetadataQueryReader
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContainerFormat(out Guid pguidContainerFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetLocation([In] uint cchMaxLength, [In, Out] ref ushort wzNamespace, out uint pcchActualLength);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataByName([In, MarshalAs(UnmanagedType.LPWStr)] string wzName, [In, Out] ref tag_inner_PROPVARIANT pvarValue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetEnumerator([MarshalAs(UnmanagedType.Interface)] out IEnumString ppIEnumString);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetMetadataByName([In, MarshalAs(UnmanagedType.LPWStr)] string wzName, [In] ref tag_inner_PROPVARIANT pvarValue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoveMetadataByName([In, MarshalAs(UnmanagedType.LPWStr)] string wzName);
		}

		[ComImport, Guid("9204FE99-D8FC-4FD5-A001-9536B067A899"), InterfaceType((short)1)]
		public interface IWICMetadataReader
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataFormat(out Guid pguidMetadataFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataHandlerInfo([MarshalAs(UnmanagedType.Interface)] out IWICMetadataHandlerInfo ppIHandler);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCount(out uint pcCount);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetValueByIndex([In] uint nIndex, [In, Out] ref tag_inner_PROPVARIANT pvarSchema, [In, Out] ref tag_inner_PROPVARIANT pvarId, [In, Out] ref tag_inner_PROPVARIANT pvarValue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetValue([In] ref tag_inner_PROPVARIANT pvarSchema, [In] ref tag_inner_PROPVARIANT pvarId, [In, Out] ref tag_inner_PROPVARIANT pvarValue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetEnumerator([MarshalAs(UnmanagedType.Interface)] out IWICEnumMetadataItem ppIEnumMetadata);
		}

		[ComImport, InterfaceType((short)1), Guid("EEBF1F5B-07C1-4447-A3AB-22ACAF78A804")]
		public interface IWICMetadataReaderInfo : IWICMetadataHandlerInfo
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetComponentType(out WICComponentType pType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCLSID(out Guid pclsid);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSigningStatus(out uint pStatus);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetAuthor([In] uint cchAuthor, [In, Out] ref ushort wzAuthor, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVendorGUID(out Guid pguidVendor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVersion([In] uint cchVersion, [In, Out] ref ushort wzVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSpecVersion([In] uint cchSpecVersion, [In, Out] ref ushort wzSpecVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFriendlyName([In] uint cchFriendlyName, [In, Out] ref ushort wzFriendlyName, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataFormat(out Guid pguidMetadataFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContainerFormats([In] uint cContainerFormats, [In, Out] ref Guid pguidContainerFormats, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDeviceManufacturer([In] uint cchDeviceManufacturer, [In, Out] ref ushort wzDeviceManufacturer, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDeviceModels([In] uint cchDeviceModels, [In, Out] ref ushort wzDeviceModels, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesRequireFullStream(out int pfRequiresFullStream);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportPadding(out int pfSupportsPadding);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesRequireFixedSize(out int pfFixedSize);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPatterns([In] ref Guid guidContainerFormat, [In] uint cbSize, [In, Out] ref WICMetadataPattern pPattern, [In, Out] ref uint pcCount, [In, Out] ref uint pcbActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void MatchesPattern([In] ref Guid guidContainerFormat, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, out int pfMatches);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateInstance([MarshalAs(UnmanagedType.Interface)] out IWICMetadataReader ppIReader);
		}

		[ComImport, InterfaceType((short)1), Guid("F7836E16-3BE0-470B-86BB-160D0AECD7DE")]
		public interface IWICMetadataWriter : IWICMetadataReader
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataFormat(out Guid pguidMetadataFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataHandlerInfo([MarshalAs(UnmanagedType.Interface)] out IWICMetadataHandlerInfo ppIHandler);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCount(out uint pcCount);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetValueByIndex([In] uint nIndex, [In, Out] ref tag_inner_PROPVARIANT pvarSchema, [In, Out] ref tag_inner_PROPVARIANT pvarId, [In, Out] ref tag_inner_PROPVARIANT pvarValue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetValue([In] ref tag_inner_PROPVARIANT pvarSchema, [In] ref tag_inner_PROPVARIANT pvarId, [In, Out] ref tag_inner_PROPVARIANT pvarValue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetEnumerator([MarshalAs(UnmanagedType.Interface)] out IWICEnumMetadataItem ppIEnumMetadata);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetValue([In] ref tag_inner_PROPVARIANT pvarSchema, [In] ref tag_inner_PROPVARIANT pvarId, [In] ref tag_inner_PROPVARIANT pvarValue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetValueByIndex([In] uint nIndex, [In] ref tag_inner_PROPVARIANT pvarSchema, [In] ref tag_inner_PROPVARIANT pvarId, [In] ref tag_inner_PROPVARIANT pvarValue);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoveValue([In] ref tag_inner_PROPVARIANT pvarSchema, [In] ref tag_inner_PROPVARIANT pvarId);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoveValueByIndex([In] uint nIndex);
		}

		[ComImport, InterfaceType((short)1), Guid("B22E3FBA-3925-4323-B5C1-9EBFC430F236")]
		public interface IWICMetadataWriterInfo : IWICMetadataHandlerInfo
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetComponentType(out WICComponentType pType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCLSID(out Guid pclsid);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSigningStatus(out uint pStatus);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetAuthor([In] uint cchAuthor, [In, Out] ref ushort wzAuthor, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVendorGUID(out Guid pguidVendor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVersion([In] uint cchVersion, [In, Out] ref ushort wzVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSpecVersion([In] uint cchSpecVersion, [In, Out] ref ushort wzSpecVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFriendlyName([In] uint cchFriendlyName, [In, Out] ref ushort wzFriendlyName, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetMetadataFormat(out Guid pguidMetadataFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetContainerFormats([In] uint cContainerFormats, [In, Out] ref Guid pguidContainerFormats, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDeviceManufacturer([In] uint cchDeviceManufacturer, [In, Out] ref ushort wzDeviceManufacturer, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDeviceModels([In] uint cchDeviceModels, [In, Out] ref ushort wzDeviceModels, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesRequireFullStream(out int pfRequiresFullStream);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesSupportPadding(out int pfSupportsPadding);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void DoesRequireFixedSize(out int pfFixedSize);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetHeader([In] ref Guid guidContainerFormat, [In] uint cbSize, [In, Out] ref WICMetadataHeader pHeader, [In, Out] ref uint pcbActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateInstance([MarshalAs(UnmanagedType.Interface)] out IWICMetadataWriter ppIWriter);
		}

		[ComImport, Guid("00000040-A8F2-4877-BA0A-FD2B6645FB94"), InterfaceType((short)1)]
		public interface IWICPalette
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void InitializePredefined([In] WICBitmapPaletteType ePaletteType, [In] int fAddTransparentColor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void InitializeCustom([In] ref uint pColors, [In] uint cCount);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void InitializeFromBitmap([In, MarshalAs(UnmanagedType.Interface)] IWICBitmapSource pISurface, [In] uint cCount, [In] int fAddTransparentColor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void InitializeFromPalette([In, MarshalAs(UnmanagedType.Interface)] IWICPalette pIPalette);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetType(out WICBitmapPaletteType pePaletteType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetColorCount(out uint pcCount);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetColors([In] uint cCount, out uint pColors, out uint pcActualColors);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void IsBlackWhite(out int pfIsBlackWhite);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void IsGrayscale(out int pfIsGrayscale);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void HasAlpha(out int pfHasAlpha);
		}

		[ComImport, Guid("00675040-6908-45F8-86A3-49C7DFD6D9AD"), InterfaceType((short)1)]
		public interface IWICPersistStream : IPersistStream
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetClassID(out Guid pClassID);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void IsDirty();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Load([In, MarshalAs(UnmanagedType.Interface)] IStream pstm);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Save([In, MarshalAs(UnmanagedType.Interface)] IStream pstm, [In] int fClearDirty);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSizeMax(out _ULARGE_INTEGER pcbSize);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void LoadEx([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] ref Guid pguidPreferredVendor, [In] uint dwPersistOptions);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SaveEx([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] uint dwPersistOptions, [In] int fClearDirty);
		}

		[ComImport, Guid("E8EDA601-3D48-431A-AB44-69059BE88BBE"), InterfaceType((short)1)]
		public interface IWICPixelFormatInfo : IWICComponentInfo
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetComponentType(out WICComponentType pType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetCLSID(out Guid pclsid);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSigningStatus(out uint pStatus);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetAuthor([In] uint cchAuthor, [In, Out] ref ushort wzAuthor, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVendorGUID(out Guid pguidVendor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetVersion([In] uint cchVersion, [In, Out] ref ushort wzVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetSpecVersion([In] uint cchSpecVersion, [In, Out] ref ushort wzSpecVersion, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFriendlyName([In] uint cchFriendlyName, [In, Out] ref ushort wzFriendlyName, out uint pcchActual);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetFormatGUID(out Guid pFormat);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetColorContext([MarshalAs(UnmanagedType.Interface)] out IWICColorContext ppIColorContext);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetBitsPerPixel(out uint puiBitsPerPixel);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetChannelCount(out uint puiChannelCount);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetChannelMask([In] uint uiChannelIndex, [In] uint cbMaskBuffer, [In, Out] ref byte pbMaskBuffer, out uint pcbActual);
		}

		[ComImport, Guid("135FF860-22B7-4DDF-B0F6-218F4F299A43"), InterfaceType((short)1)]
		public interface IWICStream : IStream
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteRead(out byte pv, [In] uint cb, out uint pcbRead);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteWrite([In] ref byte pv, [In] uint cb, out uint pcbWritten);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteSeek([In] _LARGE_INTEGER dlibMove, [In] uint dwOrigin, out _ULARGE_INTEGER plibNewPosition);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetSize([In] _ULARGE_INTEGER libNewSize);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RemoteCopyTo([In, MarshalAs(UnmanagedType.Interface)] IStream pstm, [In] _ULARGE_INTEGER cb, out _ULARGE_INTEGER pcbRead, out _ULARGE_INTEGER pcbWritten);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Commit([In] uint grfCommitFlags);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Revert();
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void LockRegion([In] _ULARGE_INTEGER libOffset, [In] _ULARGE_INTEGER cb, [In] uint dwLockType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void UnlockRegion([In] _ULARGE_INTEGER libOffset, [In] _ULARGE_INTEGER cb, [In] uint dwLockType);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Stat(out tagSTATSTG pstatstg, [In] uint grfStatFlag);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void Clone([MarshalAs(UnmanagedType.Interface)] out IStream ppstm);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void InitializeFromIStream([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void InitializeFromFilename([In, MarshalAs(UnmanagedType.LPWStr)] string wzFilename, [In] uint dwDesiredAccess);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void InitializeFromMemory([In] ref byte pbBuffer, [In] uint cbBufferSize);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void InitializeFromIStreamRegion([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] _ULARGE_INTEGER ulOffset, [In] _ULARGE_INTEGER ulMaxSize);
		}

		[ComImport, InterfaceType((short)1), Guid("449494BC-B468-4927-96D7-BA90D31AB505")]
		public interface IWICStreamProvider
		{
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetStream([MarshalAs(UnmanagedType.Interface)] out IStream ppIStream);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPersistOptions(out uint pdwPersistOptions);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetPreferredVendorGUID(out Guid pguidPreferredVendor);
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void RefreshStream();
		}
		#endregion

		#region tagStructs
		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct tag_inner_PROPVARIANT
		{
			public ushort vt;
			public byte wReserved1;
			public byte wReserved2;
			public uint wReserved3;
			public UnionVariable varUnion;
			//public VariantUnion varUnion;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagARRAYDESC
		{
			public tagTYPEDESC tdescElem;
			public ushort cDims;
			[ComConversionLoss]
			public IntPtr rgbounds;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagBLOB
		{
			public uint cbSize;
			[ComConversionLoss]
			public IntPtr pBlobData;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagBSTRBLOB
		{
			public uint cbSize;
			[ComConversionLoss]
			public IntPtr pData;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCABOOL
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCABSTR
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCABSTRBLOB
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCAC
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCACLIPDATA
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCACLSID
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCACY
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCADATE
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCADBL
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCAFILETIME
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCAFLT
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCAH
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCAI
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCAL
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		public enum tagCALLCONV
		{
			CC_CDECL = 1,
			CC_FASTCALL = 0,
			CC_FPFASTCALL = 5,
			CC_MACPASCAL = 3,
			CC_MAX = 9,
			CC_MPWCDECL = 7,
			CC_MPWPASCAL = 8,
			CC_MSCPASCAL = 2,
			CC_PASCAL = 2,
			CC_STDCALL = 4,
			CC_SYSCALL = 6
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCALPSTR
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCALPWSTR
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCAPROPVARIANT
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCASCODE
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCAUB
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCAUH
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCAUI
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCAUL
		{
			public uint cElems;
			[ComConversionLoss]
			public IntPtr pElems;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagCLIPDATA
		{
			public uint cbSize;
			public int ulClipFmt;
			[ComConversionLoss]
			public IntPtr pClipData;
		}

		public enum tagDESCKIND
		{
			DESCKIND_NONE,
			DESCKIND_FUNCDESC,
			DESCKIND_VARDESC,
			DESCKIND_TYPECOMP,
			DESCKIND_IMPLICITAPPOBJ,
			DESCKIND_MAX
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct tagELEMDESC
		{
			public tagTYPEDESC tdesc;
			public tagPARAMDESC paramdesc;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagFUNCDESC
		{
			public int memid;
			[ComConversionLoss]
			public IntPtr lprgscode;
			[ComConversionLoss]
			public IntPtr lprgelemdescParam;
			public tagFUNCKIND funckind;
			public tagINVOKEKIND invkind;
			public tagCALLCONV callconv;
			public short cParams;
			public short cParamsOpt;
			public short oVft;
			public short cScodes;
			public tagELEMDESC elemdescFunc;
			public ushort wFuncFlags;
		}

		public enum tagFUNCKIND
		{
			FUNC_VIRTUAL,
			FUNC_PUREVIRTUAL,
			FUNC_NONVIRTUAL,
			FUNC_STATIC,
			FUNC_DISPATCH
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct tagIDLDESC
		{
			[ComAliasName("WIC.ULONG_PTR")]
			public uint dwReserved;
			public ushort wIDLFlags;
		}

		public enum tagINVOKEKIND
		{
			INVOKE_FUNC = 1,
			INVOKE_PROPERTYGET = 2,
			INVOKE_PROPERTYPUT = 4,
			INVOKE_PROPERTYPUTREF = 8
		}

		[StructLayout(LayoutKind.Sequential, Pack = 2), ComConversionLoss]
		public struct tagLOGPALETTE
		{
			public ushort palVersion;
			public ushort palNumEntries;
			[ComConversionLoss]
			public IntPtr palPalEntry;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct tagPALETTEENTRY
		{
			public byte peRed;
			public byte peGreen;
			public byte peBlue;
			public byte peFlags;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagPARAMDESC
		{
			[ComConversionLoss]
			public IntPtr pparamdescex;
			public ushort wParamFlags;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct tagPARAMDESCEX
		{
			public uint cBytes;
			[MarshalAs(UnmanagedType.Struct)]
			public object varDefaultValue;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagPROPBAG2
		{
			public uint dwType;
			public ushort vt;
			[ComConversionLoss, ComAliasName("WIC.wireCLIPFORMAT")]
			public IntPtr cfType;
			public uint dwHint;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string pstrName;
			public Guid clsid;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4), ComConversionLoss]
		public struct tagRemSNB
		{
			public uint ulCntStr;
			public uint ulCntChar;
			[ComConversionLoss]
			public IntPtr rgString;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct tagSAFEARRAYBOUND
		{
			public uint cElements;
			public int lLbound;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct tagSTATSTG
		{
			[MarshalAs(UnmanagedType.LPWStr)]
			public string pwcsName;
			public uint type;
			public _ULARGE_INTEGER cbSize;
			public _FILETIME mtime;
			public _FILETIME ctime;
			public _FILETIME atime;
			public uint grfMode;
			public uint grfLocksSupported;
			public Guid clsid;
			public uint grfStateBits;
			public uint reserved;
		}

		public enum tagSYSKIND
		{
			SYS_WIN16,
			SYS_WIN32,
			SYS_MAC,
			SYS_WIN64
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct tagTLIBATTR
		{
			public Guid guid;
			public uint lcid;
			public tagSYSKIND syskind;
			public ushort wMajorVerNum;
			public ushort wMinorVerNum;
			public ushort wLibFlags;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct tagTYPEATTR
		{
			public Guid guid;
			public uint lcid;
			public uint dwReserved;
			public int memidConstructor;
			public int memidDestructor;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpstrSchema;
			public uint cbSizeInstance;
			public tagTYPEKIND typekind;
			public ushort cFuncs;
			public ushort cVars;
			public ushort cImplTypes;
			public ushort cbSizeVft;
			public ushort cbAlignment;
			public ushort wTypeFlags;
			public ushort wMajorVerNum;
			public ushort wMinorVerNum;
			public tagTYPEDESC tdescAlias;
			public tagIDLDESC idldescType;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct tagTYPEDESC
		{
			public __MIDL_IOleAutomationTypes_0005 DUMMYUNIONNAME;
			public ushort vt;
		}

		public enum tagTYPEKIND
		{
			TKIND_ENUM,
			TKIND_RECORD,
			TKIND_MODULE,
			TKIND_INTERFACE,
			TKIND_DISPATCH,
			TKIND_COCLASS,
			TKIND_ALIAS,
			TKIND_UNION,
			TKIND_MAX
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct tagVARDESC
		{
			public int memid;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpstrSchema;
			public __MIDL_IOleAutomationTypes_0006 DUMMYUNIONNAME;
			public tagELEMDESC elemdescVar;
			public ushort wVarFlags;
			public tagVARKIND varkind;
		}

		public enum tagVARKIND
		{
			VAR_PERINSTANCE,
			VAR_STATIC,
			VAR_CONST,
			VAR_DISPATCH
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct tagVersionedStream
		{
			public Guid guidVersion;
			[MarshalAs(UnmanagedType.Interface)]
			public IStream pStream;
		}
		#endregion

		#region WIC Enumerations and Structs
		public enum WICBitmapAlphaChannelOption
		{
			WICBITMAPALPHACHANNELOPTIONS_FORCE_DWORD = 0x7fffffff,
			WICBitmapIgnoreAlpha = 2,
			WICBitmapUseAlpha = 0,
			WICBitmapUsePremultipliedAlpha = 1
		}

		public enum WICBitmapCreateCacheOption
		{
			WICBitmapCacheOnDemand = 1,
			WICBitmapCacheOnLoad = 2,
			WICBITMAPCREATECACHEOPTION_FORCE_DWORD = 0x7fffffff,
			WICBitmapNoCache = 0
		}

		public enum WICBitmapDecoderCapabilities
		{
			WICBITMAPDECODERCAPABILITIES_FORCE_DWORD = 0x7fffffff,
			WICBitmapDecoderCapabilityCanDecodeAllImages = 2,
			WICBitmapDecoderCapabilityCanDecodeSomeImages = 4,
			WICBitmapDecoderCapabilityCanDecodeThumbnail = 0x10,
			WICBitmapDecoderCapabilityCanEnumerateMetadata = 8,
			WICBitmapDecoderCapabilitySameEncoder = 1
		}

		public enum WICBitmapDitherType
		{
			WICBITMAPDITHERTYPE_FORCE_DWORD = 0x7fffffff,
			WICBitmapDitherTypeDualSpiral4x4 = 6,
			WICBitmapDitherTypeDualSpiral8x8 = 7,
			WICBitmapDitherTypeErrorDiffusion = 8,
			WICBitmapDitherTypeNone = 0,
			WICBitmapDitherTypeOrdered16x16 = 3,
			WICBitmapDitherTypeOrdered4x4 = 1,
			WICBitmapDitherTypeOrdered8x8 = 2,
			WICBitmapDitherTypeSolid = 0,
			WICBitmapDitherTypeSpiral4x4 = 4,
			WICBitmapDitherTypeSpiral8x8 = 5
		}

		public enum WICBitmapEncoderCacheOption
		{
			WICBitmapEncoderCacheInMemory = 0,
			WICBITMAPENCODERCACHEOPTION_FORCE_DWORD = 0x7fffffff,
			WICBitmapEncoderCacheTempFile = 1,
			WICBitmapEncoderNoCache = 2
		}

		public enum WICBitmapInterpolationMode
		{
			WICBITMAPINTERPOLATIONMODE_FORCE_DWORD = 0x7fffffff,
			WICBitmapInterpolationModeCubic = 2,
			WICBitmapInterpolationModeFant = 3,
			WICBitmapInterpolationModeLinear = 1,
			WICBitmapInterpolationModeNearestNeighbor = 0
		}

		public enum WICBitmapLockFlags
		{
			WICBITMAPLOCKFLAGS_FORCE_DWORD = 0x7fffffff,
			WICBitmapLockRead = 1,
			WICBitmapLockWrite = 2
		}

		public enum WICBitmapPaletteType
		{
			WICBITMAPPALETTETYPE_FORCE_DWORD = 0x7fffffff,
			WICBitmapPaletteTypeCustom = 0,
			WICBitmapPaletteTypeFixedBW = 2,
			WICBitmapPaletteTypeFixedGray16 = 11,
			WICBitmapPaletteTypeFixedGray256 = 12,
			WICBitmapPaletteTypeFixedGray4 = 10,
			WICBitmapPaletteTypeFixedHalftone125 = 6,
			WICBitmapPaletteTypeFixedHalftone216 = 7,
			WICBitmapPaletteTypeFixedHalftone252 = 8,
			WICBitmapPaletteTypeFixedHalftone256 = 9,
			WICBitmapPaletteTypeFixedHalftone27 = 4,
			WICBitmapPaletteTypeFixedHalftone64 = 5,
			WICBitmapPaletteTypeFixedHalftone8 = 3,
			WICBitmapPaletteTypeFixedWebPalette = 7,
			WICBitmapPaletteTypeMedianCut = 1
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8), ComConversionLoss]
		public struct WICBitmapPattern
		{
			public _ULARGE_INTEGER Position;
			public uint Length;
			[ComConversionLoss]
			public IntPtr Pattern;
			[ComConversionLoss]
			public IntPtr Mask;
			public int EndOfStream;
		}

		public enum WICBitmapTransformOptions
		{
			WICBitmapTransformFlipHorizontal = 8,
			WICBitmapTransformFlipVertical = 0x10,
			WICBITMAPTRANSFORMOPTIONS_FORCE_DWORD = 0x7fffffff,
			WICBitmapTransformRotate0 = 0,
			WICBitmapTransformRotate180 = 2,
			WICBitmapTransformRotate270 = 3,
			WICBitmapTransformRotate90 = 1
		}

		public enum WICColorContextType
		{
			WICColorContextUninitialized,
			WICColorContextProfile,
			WICColorContextExifColorSpace
		}

		public enum WICComponentEnumerateOptions
		{
			WICComponentEnumerateDefault = 0,
			WICComponentEnumerateDisabled = -2147483648,
			WICCOMPONENTENUMERATEOPTIONS_FORCE_DWORD = 0x7fffffff,
			WICComponentEnumerateRefresh = 1,
			WICComponentEnumerateUnsigned = 0x40000000
		}

		public enum WICComponentSigning
		{
			WICComponentDisabled = -2147483648,
			WICComponentSafe = 4,
			WICComponentSigned = 1,
			WICCOMPONENTSIGNING_FORCE_DWORD = 0x7fffffff,
			WICComponentUnsigned = 2
		}

		public enum WICComponentType
		{
			WICAllComponents = 0x3f,
			WICCOMPONENTTYPE_FORCE_DWORD = 0x7fffffff,
			WICDecoder = 1,
			WICEncoder = 2,
			WICMetadataReader = 8,
			WICMetadataWriter = 0x10,
			WICPixelFormat = 0x20,
			WICPixelFormatConverter = 4
		}

		public enum WICDecodeOptions
		{
			WICDecodeMetadataCacheOnDemand = 0,
			WICDecodeMetadataCacheOnLoad = 1,
			WICMETADATACACHEOPTION_FORCE_DWORD = 0x7fffffff
		}

		public enum WICMetadataCreationOptions
		{
			WICMetadataCreationAllowUnknown = 0,
			WICMetadataCreationDefault = 0,
			WICMetadataCreationFailUnknown = 0x10000,
			WICMetadataCreationMask = -65536
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8), ComConversionLoss]
		public struct WICMetadataHeader
		{
			public _ULARGE_INTEGER Position;
			public uint Length;
			[ComConversionLoss]
			public IntPtr Header;
			public _ULARGE_INTEGER DataOffset;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8), ComConversionLoss]
		public struct WICMetadataPattern
		{
			public _ULARGE_INTEGER Position;
			public uint Length;
			[ComConversionLoss]
			public IntPtr Pattern;
			[ComConversionLoss]
			public IntPtr Mask;
			public _ULARGE_INTEGER DataOffset;
		}

		public enum WICNamedWhitePoint
		{
			WICNAMEDWHITEPOINT_FORCE_DWORD = 0x7fffffff,
			WICWhitePointAsShot = 1,
			WICWhitePointAutoWhiteBalance = 0x200,
			WICWhitePointCloudy = 4,
			WICWhitePointCustom = 0x100,
			WICWhitePointDaylight = 2,
			WICWhitePointDefault = 1,
			WICWhitePointFlash = 0x40,
			WICWhitePointFluorescent = 0x20,
			WICWhitePointShade = 8,
			WICWhitePointTungsten = 0x10,
			WICWhitePointUnderwater = 0x80
		}

		public enum WICPersistOptions
		{
			WICPersistOptionBigEndian = 1,
			WICPersistOptionDefault = 0,
			WICPersistOptionLittleEndian = 0,
			WICPersistOptionMask = 0xffff,
			WICPersistOptionNoCacheStream = 4,
			WICPersistOptionPreferUTF8 = 8,
			WICPersistOptionStrictFormat = 2
		}

		public enum WICProgressNotification
		{
			WICPROGRESSNOTIFICATION_FORCE_DWORD = 0x7fffffff,
			WICProgressNotificationAll = -65536,
			WICProgressNotificationBegin = 0x10000,
			WICProgressNotificationEnd = 0x20000,
			WICProgressNotificationFrequent = 0x40000
		}

		public enum WICProgressOperation
		{
			WICPROGRESSOPERATION_FORCE_DWORD = 0x7fffffff,
			WICProgressOperationAll = 0xffff,
			WICProgressOperationCopyPixels = 1,
			WICProgressOperationWritePixels = 2
		}

		public enum WICRawCapabilities
		{
			WICRAWCAPABILITIES_FORCE_DWORD = 0x7fffffff,
			WICRawCapabilityFullySupported = 2,
			WICRawCapabilityGetSupported = 1,
			WICRawCapabilityNotSupported = 0
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct WICRawCapabilitiesInfo
		{
			public uint cbSize;
			public uint CodecMajorVersion;
			public uint CodecMinorVersion;
			public WICRawCapabilities ExposureCompensationSupport;
			public WICRawCapabilities ContrastSupport;
			public WICRawCapabilities RGBWhitePointSupport;
			public WICRawCapabilities NamedWhitePointSupport;
			public uint NamedWhitePointSupportMask;
			public WICRawCapabilities KelvinWhitePointSupport;
			public WICRawCapabilities GammaSupport;
			public WICRawCapabilities TintSupport;
			public WICRawCapabilities SaturationSupport;
			public WICRawCapabilities SharpnessSupport;
			public WICRawCapabilities NoiseReductionSupport;
			public WICRawCapabilities DestinationColorProfileSupport;
			public WICRawCapabilities ToneCurveSupport;
			public WICRawRotationCapabilities RotationSupport;
			public WICRawCapabilities RenderModeSupport;
		}

		public enum WICRawParameterSet
		{
			WICAsShotParameterSet = 1,
			WICAutoAdjustedParameterSet = 3,
			WICRAWPARAMETERSET_FORCE_DWORD = 0x7fffffff,
			WICUserAdjustedParameterSet = 2
		}

		public enum WICRawRenderMode
		{
			WICRAWRENDERMODE_FORCE_DWORD = 0x7fffffff,
			WICRawRenderModeBestQuality = 3,
			WICRawRenderModeDraft = 1,
			WICRawRenderModeNormal = 2
		}

		public enum WICRawRotationCapabilities
		{
			WICRAWROTATIONCAPABILITIES_FORCE_DWORD = 0x7fffffff,
			WICRawRotationCapabilityFullySupported = 3,
			WICRawRotationCapabilityGetSupported = 1,
			WICRawRotationCapabilityNinetyDegreesSupported = 2,
			WICRawRotationCapabilityNotSupported = 0
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct WICRawToneCurve
		{
			public uint cPoints;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
			public WICRawToneCurvePoint[] aPoints;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct WICRawToneCurvePoint
		{
			public double Input;
			public double Output;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct WICRect
		{
			public int X;
			public int Y;
			public int Width;
			public int Height;
		}

		public enum WICTiffCompressionOption
		{
			WICTiffCompressionCCITT3 = 2,
			WICTiffCompressionCCITT4 = 3,
			WICTiffCompressionDontCare = 0,
			WICTiffCompressionLZW = 4,
			WICTiffCompressionNone = 1,
			WICTIFFCOMPRESSIONOPTION_FORCE_DWORD = 0x7fffffff,
			WICTiffCompressionRLE = 5,
			WICTiffCompressionZIP = 6
		}
		#endregion
	}
}