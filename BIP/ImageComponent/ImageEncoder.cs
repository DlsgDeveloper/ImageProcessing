using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.IO;

using ImageComponent.InteropServices;


namespace ImageComponent
{
	public class ImageEncoder : IDisposable
	{
		string							file = null;
		IWICImagingFactory				factory = null;
		IWICBitmapEncoder				encoder = null;
		IWICBitmapFrameEncode			frame = null;
		IWICStream						stream = null;
		IWICPalette						palette = null;
		uint							bitsPerPixel = 8;
		uint							stride = 1;
		Guid							gPixelFormatGUID;
		Guid							gFileFormatGUID;
		ImageProcessing.PixelsFormat	gPixelsFormat = ImageProcessing.PixelsFormat.Format24bppRgb;
		uint							width, height;

		
		#region constructor
		public ImageEncoder()
		{
			try
			{
				factory = (IWICImagingFactory)new WICImagingFactory();
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		public  uint							Stride { get { return (uint)stride; } }		
		public  uint							Width { get { return this.width; } }		
		public  uint							Height { get { return this.height; } }
		public ImageProcessing.PixelsFormat		PixelsFormat { get { return this.gPixelsFormat; } }
		public ImageComponent.PixelFormat		PixelFormat { get { return Misc.GetPixelFormat(this.gPixelFormatGUID); } }

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region OpenTiff()
		public void OpenTiff(string imageFile, uint width, uint height, ImageProcessing.PixelsFormat pixelsFormat, double dpiX, double dpiY, TiffCompression tiffCompression)
		{
			try
			{
				IPropertyBag2[] propertyBags = new IPropertyBag2[1];

				this.file = imageFile;
				this.gFileFormatGUID = Consts.GUID_ContainerFormatTiff;
				this.width = width;
				this.height = height;
				this.gPixelsFormat = pixelsFormat;

				stream = factory.CreateStream();
				stream.InitializeFromFilename(imageFile, ImageComponent.InteropServices.NativeMethods.GenericAccessRights.GENERIC_WRITE);

				encoder = factory.CreateEncoder(this.gFileFormatGUID, null);
				encoder.Initialize(stream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);
				encoder.CreateNewFrame(out frame, propertyBags);

				IPropertyBag2 propertyBag = propertyBags[0];

				PROPBAG2[] propertyBagOptions = new PROPBAG2[1];
				propertyBagOptions[0].dwType = (uint)PROPBAG2_TYPE.PROPBAG2_TYPE_DATA;
				propertyBagOptions[0].vt = System.Runtime.InteropServices.VarEnum.VT_UI1;
				propertyBagOptions[0].pstrName = "TiffCompressionMethod";
				propertyBag.Write(1, propertyBagOptions, new object[] { (byte)tiffCompression });

				frame.Initialize(propertyBag);
				frame.SetSize(width, height);
				frame.SetResolution(dpiX, dpiY);

				this.gPixelFormatGUID = Misc.GetPixelFormatGUID(gPixelsFormat);
				frame.SetPixelFormat(ref this.gPixelFormatGUID);

				this.bitsPerPixel = Misc.GetBitsPerPixel(pixelsFormat);
				this.stride = Misc.GetStride(width, bitsPerPixel);

				if (Misc.IsIndexedBitmap(gPixelsFormat))
				{
					this.palette = Misc.GetPalette(factory, frame, gPixelsFormat);
					frame.SetPalette(palette);
				}

				propertyBag.ReleaseComObject();
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region OpenPng()
		public void OpenPng(string imageFile, uint width, uint height, ImageProcessing.PixelsFormat pixelsFormat, double dpiX, double dpiY)
		{
			try
			{
				IPropertyBag2[] pPropertyBag = new IPropertyBag2[1];

				this.file = imageFile;
				this.gFileFormatGUID = Consts.GUID_ContainerFormatPng;
				this.width = width;
				this.height = height;
				this.gPixelsFormat = pixelsFormat;

				stream = factory.CreateStream();
				stream.InitializeFromFilename(imageFile, ImageComponent.InteropServices.NativeMethods.GenericAccessRights.GENERIC_WRITE);

				encoder = factory.CreateEncoder(this.gFileFormatGUID, null);
				encoder.Initialize(stream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);
				encoder.CreateNewFrame(out frame, pPropertyBag);

				frame.Initialize(pPropertyBag[0]);

				frame.SetSize(width, height);
				frame.SetResolution(dpiX, dpiY);

				this.gPixelFormatGUID = Misc.GetPixelFormatGUID(gPixelsFormat);
				frame.SetPixelFormat(ref gPixelFormatGUID);

				this.bitsPerPixel = Misc.GetBitsPerPixel(pixelsFormat);
				this.stride = Misc.GetStride(width, bitsPerPixel);

				if (Misc.IsIndexedBitmap(gPixelsFormat))
				{
					this.palette = Misc.GetPalette(factory, frame, gPixelsFormat);
					this.frame.SetPalette(palette);
				}

				pPropertyBag[0].ReleaseComObject();
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region OpenJpeg()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="imageFile"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="pixelsFormat"></param>
		/// <param name="dpiX"></param>
		/// <param name="dpiY"></param>
		/// <param name="jpegCompression">from 0 to 255, 255 is lossless</param>
		public void OpenJpeg(string imageFile, uint width, uint height, ImageProcessing.PixelsFormat pixelsFormat, double dpiX, double dpiY, byte jpegCompression)
		{
			try
			{
				IPropertyBag2[] pPropertyBag = new IPropertyBag2[1];

				this.file = imageFile;
				this.gFileFormatGUID = Consts.GUID_ContainerFormatJpeg;
				this.width = width;
				this.height = height;
				this.gPixelsFormat = pixelsFormat;

				stream = factory.CreateStream();
				stream.InitializeFromFilename(imageFile, ImageComponent.InteropServices.NativeMethods.GenericAccessRights.GENERIC_WRITE);

				encoder = factory.CreateEncoder(this.gFileFormatGUID, null);
				encoder.Initialize(stream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);
				encoder.CreateNewFrame(out frame, pPropertyBag);

				PROPBAG2[] propertyBagOptions = new PROPBAG2[1];
				propertyBagOptions[0].pstrName = @"ImageQuality";
				pPropertyBag[0].Write(1, propertyBagOptions, new object[] { (float)jpegCompression / 100.0F });

				frame.Initialize(pPropertyBag[0]);
				frame.SetSize(width, height);

				if (gPixelsFormat == ImageProcessing.PixelsFormat.Format8bppGray || gPixelsFormat == ImageProcessing.PixelsFormat.Format8bppIndexed)
					this.gPixelFormatGUID = Consts.GUID_WICPixelFormat8bppGray;
				else
					this.gPixelFormatGUID = Consts.GUID_WICPixelFormat24bppBGR;

				frame.SetPixelFormat(ref gPixelFormatGUID);
				frame.SetResolution(dpiX, dpiY);

				//stride = (width * 24 + 7)/8;//WICGetStride
				this.bitsPerPixel = Misc.GetBitsPerPixel(pixelsFormat);
				this.stride = Misc.GetStride(width, bitsPerPixel);

				pPropertyBag[0].ReleaseComObject();
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region OpenGif()
		public void OpenGif(string imageFile, uint width, uint height, ImageProcessing.PixelsFormat pixelsFormat, double dpiX, double dpiY)
		{
			try
			{
				IPropertyBag2[] pPropertyBag = new IPropertyBag2[1];
				this.width = width;
				this.height = height;
				this.gPixelsFormat = pixelsFormat;

				this.file = imageFile;
				stream = factory.CreateStream();
				stream.InitializeFromFilename(imageFile, ImageComponent.InteropServices.NativeMethods.GenericAccessRights.GENERIC_WRITE);

				encoder = factory.CreateEncoder(Consts.GUID_ContainerFormatGif, null);
				encoder.Initialize(stream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);
				encoder.CreateNewFrame(out frame, pPropertyBag);

				frame.Initialize(pPropertyBag[0]);
				frame.SetSize(width, height);

				this.gPixelFormatGUID = Consts.GUID_WICPixelFormat8bppGray;
				frame.SetPixelFormat(ref this.gPixelFormatGUID);

				frame.SetResolution(dpiX, dpiY);

				bitsPerPixel = Misc.GetBitsPerPixel(pixelsFormat);
				stride = Misc.GetStride(width, bitsPerPixel);

				this.palette = Misc.GetPalette(factory, frame, gPixelsFormat);
				frame.SetPalette(this.palette);

				pPropertyBag[0].ReleaseComObject();
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region OpenHdImage()
		public void OpenHdImage(string imageFile, uint width, uint height, ImageProcessing.PixelsFormat pixelsFormat, double dpiX, double dpiY, byte compression)
		{
			/*IPropertyBag2[] pPropertyBag = new IPropertyBag2[1];

			this.file = imageFile;
			stream = factory.CreateStream();
			stream.InitializeFromFilename(imageFile, ImageComponent.InteropServices.NativeMethods.GenericAccessRights.GENERIC_WRITE);
			this.width = width;
			this.height = height;
			this.gPixelsFormat = pixelsFormat;

			encoder = factory.CreateEncoder(Consts.GUID_ContainerFormatWmp, null);
			encoder.Initialize(stream, WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);
			encoder.CreateNewFrame(out frame, pPropertyBag);

			PROPBAG2 name = new PROPBAG2();
			name.dwType = (uint)PROPBAG2_TYPE.PROPBAG2_TYPE_DATA;
			name.vt = System.Runtime.InteropServices.VarEnum.VT_R4;
			name.pstrName = @"ImageQuality";
			float f = (float)compression / 100.0F;
			PropVariant varValue = new PropVariant(f, PropVariantMarshalType.Automatic);
			PROPBAG2[] options = new PROPBAG2[] { name };
			PropVariant[] varValues = new PropVariant[] { varValue };
			pPropertyBag[0].Write(1, options, (object[])varValues);
			//hr = pPropertyBag.Write(1, &name, &value);

			frame.Initialize(pPropertyBag[0]);
			frame.SetSize(width, height);

			this.gPixelFormatGUID = Misc.GetPixelFormatGUID(gPixelsFormat);
			frame.SetPixelFormat(ref gPixelFormatGUID);

			frame.SetResolution(dpiX, dpiY);

			this.bitsPerPixel = Misc.GetBitsPerPixel(pixelsFormat);
			this.stride = Misc.GetStride(width, bitsPerPixel);*/
		}
		#endregion

		#region Write()
		/*public void Write(uint height, uint str, byte[] buffer)
		{
			frame.WritePixels(height, stride, (uint)buffer.Length, buffer);
		}*/

		public unsafe void Write(uint height, uint str, byte* buffer)
		{
			try
			{
				frame.WritePixels(height, stride, (uint)str * height, buffer);
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region SetPalette()
		public void SetPalette(ImageComponent.PixelFormat pixelFormat, List<uint> paletteColors)
		{
			try
			{
				IWICPalette palette = Misc.GetPalette(factory, frame, this.gPixelsFormat);// Misc.GetPixelFormat(pixelFormat));
				uint[] colors = new uint[paletteColors.Count];

				for (int i = 0; i < paletteColors.Count; i++)
					colors[i] = paletteColors[i];

				palette.InitializeCustom(colors, (uint)paletteColors.Count);

				frame.SetPalette(palette);
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region Close()
		public void Close()
		{
			try
			{
				if (frame != null)
				{					
					frame.Commit();
				}

				if (encoder != null)
				{
					encoder.Commit();
				}

				if (factory != null)
				{
					factory.ReleaseComObject();
					factory = null;
				}

				if (this.palette != null)
				{
					this.palette.ReleaseComObject();
					this.palette = null;
				}
				
				if (frame != null)
				{
					frame.ReleaseComObject();
					frame = null;
				}

				if (encoder != null)
				{
					encoder.ReleaseComObject();
					encoder = null;
				}

				if (stream != null)
				{
					stream.ReleaseComObject();
					stream = null;
				}

				SeekAndDestroyPngTransparentTable();
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion
	
		#region Dispose()
		public void Dispose()
		{
			Close();
		}
		#endregion

		#region WriteMetadata()
		public void WriteMetadata(string metadataPath)
		{
			try
			{
				IWICMetadataQueryWriter pQueryWriter = factory.CreateQueryWriter(Consts.GUID_MetadataFormatXMP, null);
				IWICMetadataQueryWriter pFrameQWriter = frame.GetMetadataQueryWriter();

				PropVariant propVariant = new PropVariant(@"DLSG at Image Access Title.", PropVariantMarshalType.Automatic);

				pQueryWriter.SetMetadataByName(@"/dc:title", propVariant);

				propVariant.value = @"DLSG at Image Access creator.";
				pQueryWriter.SetMetadataByName(@"/dc:creator", propVariant);

				propVariant.value = @"DLSG at Image Access Description.";
				pQueryWriter.SetMetadataByName(@"/dc:description", propVariant);

				propVariant.value = @"DLSG at Image Access Publisher.";
				pQueryWriter.SetMetadataByName(@"/dc:publisher", propVariant);

				propVariant.value = @"DLSG at Image Access Subject.";
				pQueryWriter.SetMetadataByName(@"/dc:subject", propVariant);

				propVariant.value = @"DLSG at Image Access SpectralSensitivity.";
				pQueryWriter.SetMetadataByName(@"/exif:SpectralSensitivity", propVariant);

				propVariant.ReleaseComObject();

				PropVariant value = new PropVariant(pQueryWriter, PropVariantMarshalType.Automatic);
				/*PropVariantInit(&value);
				value.vt = VT_UNKNOWN;
				value.punkVal = pQueryWriter;
				value.punkVal.AddRef();*/

				pFrameQWriter.SetMetadataByName(@"/", value);

				value.ReleaseComObject();
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region WriteJpegMetadata()
		public void WriteJpegMetadata(Metadata.ExifMetadata exif)
		{
			try
			{
				IWICMetadataQueryWriter pFrameQWriter = frame.GetMetadataQueryWriter();

				if (pFrameQWriter != null)
				{
					for (int i = 0; i < exif.Properties.Count; i++)
					{
						if (exif.Properties[i].Defined == true)
						{
							string path = exif.GetJpegPropertyPath(exif.Properties[i]);

							if (path != null)
							{
								PropVariant propVariant = exif.Properties[i].ExportToPROPVARIANT();
								pFrameQWriter.SetMetadataByName(path, propVariant);

								propVariant.ReleaseComObject();
							}
						}
					}
				}

				if (pFrameQWriter != null)
				{
					pFrameQWriter.ReleaseComObject();
					pFrameQWriter = null;
				}
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region WriteTiffMetadata()
		public void WriteTiffMetadata(Metadata.ExifMetadata exif)
		{
			try
			{
				IWICMetadataQueryWriter pFrameQWriter = frame.GetMetadataQueryWriter();

				if (pFrameQWriter != null)
				{
					for (int i = 0; i < exif.Properties.Count; i++)
					{
						if (exif.Properties[i].Defined == true)
						{
							string path = exif.GetTiffPropertyPath(exif.Properties[i]);

							if (path != null)
							{
								PropVariant propVariant = exif.Properties[i].ExportToPROPVARIANT();
								pFrameQWriter.SetMetadataByName(path, propVariant);

								propVariant.ReleaseComObject();
							}
						}
					}
				}

				if (pFrameQWriter != null)
				{
					pFrameQWriter.ReleaseComObject();
					pFrameQWriter = null;
				}
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region WritePngMetadata()
		/*void WritePngMetadata(ImageComponent.Metadata.ExifMetadata exif)
		{
			HRESULT						hr = 0;
			IWICMetadataQueryWriter*	pFrameQWriter = null;

			hr = frame.GetMetadataQueryWriter(&pFrameQWriter);

			if (SUCCEEDED(hr))
			{			
				for(int i = 0; i < exif.Properties.Count; i++)
				{
					if(exif.Properties[i].Defined == true)
					{	
						string path = exif.GetPngPropertyPath(exif.Properties[i]);

						if(path != null)
						{						
							PROPVARIANT value;
							PropVariantInit(&value);
														
							exif.Properties[i].ExportToPROPVARIANT(&value);
							hr = pFrameQWriter.SetMetadataByName(CString(path), &value);

							PropVariantClear(&value);
						}
					}
				}
			}

			//IWICMetadataQueryWriter		*pQueryWriter = null;
			//if (SUCCEEDED(hr))
			//{
			//	hr = frame.GetMetadataQueryWriter(&pQueryWriter);
			//	if (SUCCEEDED(hr))
			//	{
			//		PROPVARIANT value;
			//		PropVariantInit(&value);
			//		value.vt = VT_UNKNOWN;
			//		value.punkVal = pTEXTQWriter;
			//		value.punkVal.AddRef();
			//		hr = pQueryWriter.SetMetadataByName(@"/", &value);
			//		PropVariantClear(&value);
			//	}
			//}

			if(pFrameQWriter != null)
			{
				(*pFrameQWriter).Release();
				pFrameQWriter.Release();
				pFrameQWriter = null;
			}
		}*/
		#endregion

		#region WriteGifMetadata()
		/*void WriteGifMetadata(List<string> list)
		{
			HRESULT						hr = 0;
			IWICMetadataQueryWriter*	pFrameQWriter = null;

			hr = frame.GetMetadataQueryWriter(&pFrameQWriter);

			if (SUCCEEDED(hr))
			{			
				for(int i = 0; i < list.Count; i++)
				{
					string path = "/commenttext/TextEntry";

					if(path != null)
					{						
						PROPVARIANT value;
						PropVariantInit(&value);

						array<byte> bytes = System.Text.Encoding.ASCII.GetBytes(list[i]);

						value.vt = VT_LPSTR;
						value.pszVal = (LPSTR) Marshal.StringToHGlobalAnsi (list[i]).ToPointer();
							
						hr = pFrameQWriter.SetMetadataByName(CString(path), &value);

						PropVariantClear(&value);
					}
				}
			}

			if(pFrameQWriter != null)
			{
				(*pFrameQWriter).Release();
				pFrameQWriter.Release();
				pFrameQWriter = null;
			}
		}*/
		#endregion

#endregion

		//PRIVATE METHODS
		#region private methods

		#region SeekAndDestroyPngTransparentTable()
		private void SeekAndDestroyPngTransparentTable()
		{					
			if((gFileFormatGUID == Consts.GUID_ContainerFormatPng) && (gPixelFormatGUID == Consts.GUID_WICPixelFormat8bppIndexed || gPixelFormatGUID == Consts.GUID_WICPixelFormat8bppGray || 
				gPixelFormatGUID == Consts.GUID_WICPixelFormat4bppIndexed || gPixelFormatGUID == Consts.GUID_WICPixelFormat4bppGray || 
				gPixelFormatGUID == Consts.GUID_WICPixelFormat1bppIndexed )) //|| *gPixelFormatGUID == GUID_WICPixelFormatBlackWhite))
			{
				FileStream	stream = new FileStream(file, FileMode.Open);
				byte[]		buffer = new byte[26];
				string		tagName;		
				int			tagStart = 8;
				int			tagLength;

				stream.Position = 0;
				stream.Read(buffer, 0, 26);
			
				do
				{
					stream.Position = tagStart;
					stream.Read(buffer, 0, 8);
					
					tagLength = (buffer[0] << 24) + (buffer[1] << 16) + (buffer[2] << 8) + buffer[3];
					tagName = string.Format("{0}{1}{2}{3}", Convert.ToChar(buffer[4]), Convert.ToChar(buffer[5]), Convert.ToChar(buffer[6]), Convert.ToChar(buffer[7]));

					if (tagName.ToUpper() == "TRNS")
					{
						byte[]		tagBuffer = new byte[2];
						tagBuffer[0] = 0x0070;
						
						stream.Position = tagStart + 4;
						stream.Write(tagBuffer, 0, 1);	
						break;
					}

					tagStart += tagLength + 12;
				}
				while (tagName != "IEND" && tagStart < (int)stream.Length);

				stream.Close();
			}
		}
		#endregion

		#region Equals()
		/*bool Equals(Guid guid1, Guid guid2)
		{			
			if(((long)guid1.Data1 != (long)guid2.Data1) || (guid1.Data2 != guid2.Data2) ||(guid1.Data3 != guid2.Data3))
				return false;

			for(int i = 0; i < 8; i++)
				if(guid1.Data4[i] != guid2.Data4[i])
					return false;

			return true;
		}*/
		#endregion

		#endregion

	}
}
