using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using ImageComponent.InteropServices;


namespace ImageComponent
{
	public class ImageDecoder : IDisposable
	{
		IWICImagingFactory				factory = null;
		IWICBitmapDecoder				decoder = null;
		IWICBitmapFrameDecode			frame = null;
		uint							frameCount;
		uint							width, height ;
		double							dpiX, dpiY;
		uint							stride ;
		ImageProcessing.PixelsFormat	gPixelsFormat;
		uint							bitsPerPixel;
		IWICPalette						gPalette = null;
		byte[]							gBuffer = null;
		Guid							pixelFormatGuid;

		#region constructor
		public ImageDecoder(string imageFile)
		{
			int index = 0;
			
			try
			{
				this.factory = (IWICImagingFactory)new WICImagingFactory();
				index = 1;
				decoder = factory.CreateDecoderFromFilename(imageFile, null, NativeMethods.GenericAccessRights.GENERIC_READ, WICDecodeOptions.WICDecodeMetadataCacheOnLoad);

				index = 2;
				if (decoder != null)
				{
					frameCount = decoder.GetFrameCount();
					index = 3;

					if (frameCount > 0)
					{
						frame = decoder.GetFrame(0);
						index = 4;
					}
				}

				index = 5;
				frame.GetSize(out width, out height);
				index = 6;
				frame.GetPixelFormat(out pixelFormatGuid);
				index = 7;
				gPixelsFormat = Misc.GetPixelsFormat(pixelFormatGuid);
				index = 8;
				frame.GetResolution(out dpiX, out dpiY);
				index = 9;

				Guid guidContainerFormat;
				decoder.GetContainerFormat(out guidContainerFormat);

				index = 10;

				//gPalette
				if (Misc.IsIndexedBitmap(gPixelsFormat))
				{
					index = 11;
					if (guidContainerFormat == Consts.GUID_ContainerFormatJpeg)
					{
						index = 12;
						gPalette = Misc.GetPalette(this.factory, Misc.GetPixelFormat(this.pixelFormatGuid));
						index = 13;
					}
					else
					{
						index = 14;
						gPalette = Misc.GetPalette(this.factory, this.frame, Misc.GetPixelFormat(this.pixelFormatGuid));
						index = 15;
					}
				}

				index = 16;
				bitsPerPixel = Misc.GetBitsPerPixel(this.gPixelsFormat);
				index = 17;
				stride = Misc.GetStride(width, bitsPerPixel);
				index = 18;
			}
			catch (Exception ex)
			{
				throw new Exception("ImageDecoder, constructor: " + Misc.GetErrorMessage(ex) + Environment.NewLine + "Index: " + index.ToString(), ex);
			}
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties

		public uint							Stride { get { return (uint)stride; } }
		public uint							Width { get { return this.width; } }
		public uint							Height { get { return this.height; } }
		public ImageProcessing.PixelsFormat PixelsFormat { get { return this.gPixelsFormat; } }
		public ImageComponent.PixelFormat	PixelFormat { get { return Misc.GetPixelFormat(this.pixelFormatGuid); } }
		public double						DpiX { get { return this.dpiX; } }
		public double						DpiY { get { return this.dpiY; } }

		//public byte[]						Buffer { get { return gBuffer; } }
		//public long							BufferSize { get { return gBuffer.Length; } }
		public bool							IsIndexedBitmap { get { return Misc.IsIndexedBitmap(this.gPixelsFormat); } }
		public uint							FramesCount { get { return frameCount; } }

		#region PaletteColors
		public System.Drawing.Imaging.ColorPalette ColorPalette
		{
			get
			{
				if(this.gPalette == null)
				{
					return null;
				}
				else
				{
					System.Drawing.Imaging.ColorPalette p = ImageProcessing.Misc.GetEmptyPalette();
					
					uint[]		pColors = new uint[256];
					uint		actualColors = gPalette.GetColors(256, pColors);

					for(uint i = 0; i < actualColors; i++)
						p.Entries[i] = System.Drawing.Color.FromArgb((int)pColors[i]);

					return p;
				}
			}
		}
		#endregion

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region Read()
		public byte[] Read(uint x, uint y, uint w, uint h, out uint stride)
		{
			int index = 0;
			uint bufferSize = 0;

			try
			{
				index = 1;
				ReleaseBuffer();

				index = 2;
				stride = Misc.GetStride(w, bitsPerPixel);
				index = 3;
				bufferSize = stride * h;

				index = 4;
			}
			catch (Exception ex)
			{
				throw new Exception("ImageDecoder, Read(): " + Misc.GetErrorMessage(ex) + Environment.NewLine + "Index: " + index.ToString(), ex);
			}

			try
			{
				gBuffer = new byte[bufferSize];
			}
			catch (Exception ex)
			{
				try
				{
					GC.Collect();
					gBuffer = new byte[bufferSize];
				}
				catch (Exception)
				{
					throw new Exception(string.Format("ImageDecoder, Read(): stride={0}, height={1}, error {2}", stride, h, Misc.GetErrorMessage(ex)), ex);
				}
			}

			try
			{
				index = 5;
				if (gBuffer != null)
				{
					index = 6;
					WICRect rc = new WICRect() { X = (int)x, Y = (int)y, Width = (int)w, Height = (int)h };

					index = 7;
					frame.CopyPixels(rc, stride, bufferSize, gBuffer);
					index = 8;
				}
				else
				{
					index = 999;
					throw new OutOfMemoryException("Image Component: Out of memory!");
				}

				index = 9;
				return gBuffer;
			}
			catch (Exception ex)
			{
				throw new Exception("ImageDecoder, Read(): " + Misc.GetErrorMessage(ex) + Environment.NewLine + "Index: " + index.ToString(), ex);
			}
		}
		#endregion

		#region ReleaseBuffer()
		public void ReleaseBuffer()
		{
			if (this.gBuffer != null)
			{
				gBuffer = null;
				//GC.Collect();
			}
		}
		#endregion

		#region Close()
		public void Close()
		{
			try
			{
				ReleaseBuffer();

				if (frame != null)
				{
					frame.ReleaseComObject();
					frame = null;
				}

				if (factory != null)
				{
					factory.ReleaseComObject();
					factory = null;
				}

				if (decoder != null)
				{
					decoder.ReleaseComObject();
					decoder = null;
				}
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region destructor
		public void Dispose()
		{
			Close();
		}
		#endregion

		#region GetMetadata()
		public List<string> GetMetadata(string metadataPath)
		{
			try
			{
				IWICMetadataQueryReader pQueryReader = null;
				IWICMetadataQueryReader pIFDReader = null;
				List<string> list = new List<string>();
				PropVariant propVariant = null;

				try
				{
					pQueryReader = frame.GetMetadataQueryReader();
					propVariant = new PropVariant();

					if (pQueryReader != null)
					{
						try
						{
							pQueryReader.GetMetadataByName(metadataPath, propVariant);
						}
						catch (Exception ex)
						{
							if (ex.Message.ToLower().Contains("0x88982f40"))
								return list;
							else
								throw ex;
						}

						if (propVariant.Value != null && propVariant.GetUnmanagedType() == System.Runtime.InteropServices.VarEnum.VT_UNKNOWN)
						{
							IntPtr reference = IntPtr.Zero;
							Guid guid = Consts.IID_IWICMetadataQueryReader;
							IntPtr ptr = Marshal.GetIUnknownForObject(propVariant.Value);
							Marshal.QueryInterface(ptr, ref guid, out reference);

							if (reference != IntPtr.Zero)
							{
								pIFDReader = (IWICMetadataQueryReader)Marshal.GetObjectForIUnknown(reference);

								System.Runtime.InteropServices.ComTypes.IEnumString metadataItems = pIFDReader.GetEnumerator();
								uint pceltFetched = 0;

								string[] rgelt = new string[1];
								int hovno;

								do
								{
									unsafe
									{
										hovno = metadataItems.Next(1, rgelt, new IntPtr(&pceltFetched));

										if (hovno == 0)
											list.Add(rgelt[0]);
									}
								} while (hovno == 0);

								if (metadataItems != null)
								{
									metadataItems.ReleaseComObject();
									metadataItems = null;
								}
							}
						}
					}
				}
#if DEBUG
				catch (Exception ex)
				{
					throw ex;
				}
#endif
				finally
				{
					if (pQueryReader != null)
					{
						pQueryReader.ReleaseComObject();
						pQueryReader = null;
					}

					if (pIFDReader != null)
					{
						pIFDReader.ReleaseComObject();
						pIFDReader = null;
					}

					/*if (propVariant != null)
					{
						propVariant.ReleaseComObject();
						propVariant = null;
					}*/
				}

				return list;
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region GetTiffMetadata()
		public Metadata.ExifMetadata GetTiffMetadata()
		{
			try
			{
				List<string> list = GetMetadata(@"/ifd");
				Metadata.ExifMetadata exif = new Metadata.ExifMetadata();

				for (int i = 0; i < list.Count; i++)
				{
					IWICMetadataQueryReader pItemReader = null;

					try
					{
						pItemReader = frame.GetMetadataQueryReader();

						if (pItemReader != null)
						{
							PropVariant propVariant = new PropVariant();
							string pwzItem = @"/ifd" + list[i];

							pItemReader.GetMetadataByName(pwzItem, propVariant);

							string itemString = list[i];
							int indexOf = itemString.IndexOf("=");

							if (indexOf > 0)
							{
								string itemIdString = itemString.Substring(indexOf + 1, itemString.Length - indexOf - 2);
								int propertyId;

								if (System.Int32.TryParse(itemIdString, out propertyId))
								{
									Metadata.PropertyBase propertyBase = exif.GetProperty(propertyId);

									if (propertyBase != null)
										propertyBase.ImportFromPROPVARIANT(propVariant);
								}
							}

							//propVariant.ReleaseComObject();
						}
					}
					finally
					{
						if (pItemReader != null)
						{
							pItemReader.ReleaseComObject();
							pItemReader = null;
						}
					}
				}

				return exif;
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region GetJpegMetadata()
		public Metadata.ExifMetadata GetJpegMetadata()
		{
			try
			{
				Metadata.ExifMetadata exif = new Metadata.ExifMetadata();
				string pwzRatingQuery;

				for (int j = 0; j < 2; j++)
				{
					if (j == 0)
						pwzRatingQuery = @"/app1/ifd";
					else
						pwzRatingQuery = @"/app1/ifd/exif";

					List<string> list = GetMetadata(pwzRatingQuery);

					for (int i = 0; i < list.Count; i++)
					{
						IWICMetadataQueryReader pItemReader = null;

						try
						{
							pItemReader = frame.GetMetadataQueryReader();
							PropVariant propVariant = new PropVariant();
							pItemReader.GetMetadataByName(pwzRatingQuery + list[i], propVariant);

							string itemString = list[i];
							int indexOf = itemString.IndexOf("=");

							if (indexOf > 0)
							{
								string itemIdString = itemString.Substring(indexOf + 1, itemString.Length - indexOf - 2);
								int propertyId;

								if (System.Int32.TryParse(itemIdString, out propertyId))
								{
									Metadata.PropertyBase propertyBase = exif.GetProperty(propertyId);

									if (propertyBase != null)
										propertyBase.ImportFromPROPVARIANT(propVariant);
								}
							}

							//propVariant.ReleaseComObject();
						}
						finally
						{
							if (pItemReader != null)
							{
								pItemReader.ReleaseComObject();
								pItemReader = null;
							}
						}
					}
				}

				return exif;
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region GetPngMetadata()
		public Metadata.ExifMetadata GetPngMetadata()
		{
			try
			{
				//string				pwzRatingQuery = @"/[%d]tEXt";
				List<string> list = new List<string>();// GetMetadata(pwzRatingQuery);
				Metadata.ExifMetadata exif = new Metadata.ExifMetadata();
				List<string> returnedKeys;
				int index = 0;

				do
				{
					string key = string.Format(@"/[{0}]tEXt", index);
					returnedKeys = GetMetadata(key);

					foreach (string returnedKey in returnedKeys)
						list.Add(key + returnedKey);
					index++;
				} while (returnedKeys != null && returnedKeys.Count > 0);

				IWICMetadataQueryReader pItemReader = null;

				try
				{
					pItemReader = frame.GetMetadataQueryReader();

					for (int i = 0; i < list.Count; i++)
					{
						if (pItemReader != null)
						{
							PropVariant propVariant = new PropVariant();
							pItemReader.GetMetadataByName(list[i], propVariant);

							if (propVariant != null)
							{
								if (list[i].Contains("Comment"))
									exif.UserComment.ImportFromPROPVARIANT(propVariant);
								else if (list[i].Contains("Author"))
									exif.Artist.ImportFromPROPVARIANT(propVariant);
								else if (list[i].Contains("Copyright"))
									exif.Copyright.ImportFromPROPVARIANT(propVariant);
								else if (list[i].Contains("Description"))
									exif.ImageDescription.ImportFromPROPVARIANT(propVariant);
								else if (list[i].Contains("Software"))
									exif.Software.ImportFromPROPVARIANT(propVariant);
								else if (list[i].Contains("Source"))
									exif.Model.ImportFromPROPVARIANT(propVariant);
								else if (list[i].Contains("CreationTime"))
									exif.DateTimeOriginal.ImportFromPROPVARIANT(propVariant);
								//else if(list[i].Contains("Disclaimer"))
								//	exif.Disclaime.ImportFromPROPVARIANT(value);
								else if (list[i].Contains("Warning"))
									exif.MakerNote.ImportFromPROPVARIANT(propVariant);
								else if (list[i].Contains("Title"))
									exif.Make.ImportFromPROPVARIANT(propVariant);
							}
						}
					}
				}
				finally
				{
					if (pItemReader != null)
					{
						pItemReader.ReleaseComObject();
						pItemReader = null;
					}
				}

				return exif;
			}
			catch (Exception ex)
			{
				throw new Exception(Misc.GetErrorMessage(ex));
			}
		}
		#endregion

		#region SelectFrame()
		public void SelectFrame(uint frameIndex)
		{
			if (frameIndex < 0 || frameIndex >= this.FramesCount)
				throw new ArgumentException("Invalid frame index!");
			
			frame = decoder.GetFrame(frameIndex);

			frame.GetSize(out width, out height);
			frame.GetPixelFormat(out pixelFormatGuid);
			gPixelsFormat = Misc.GetPixelsFormat(pixelFormatGuid);
			frame.GetResolution(out dpiX, out dpiY);

			Guid guidContainerFormat;
			decoder.GetContainerFormat(out guidContainerFormat);

			//gPalette
			if (Misc.IsIndexedBitmap(gPixelsFormat))
			{
				if (guidContainerFormat == Consts.GUID_ContainerFormatJpeg)
				{
					gPalette = Misc.GetPalette(this.factory, Misc.GetPixelFormat(this.pixelFormatGuid));
				}
				else
				{
					gPalette = Misc.GetPalette(this.factory, this.frame, Misc.GetPixelFormat(this.pixelFormatGuid));
				}
			}

			bitsPerPixel = Misc.GetBitsPerPixel(this.gPixelsFormat);
			stride = Misc.GetStride(width, bitsPerPixel);
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#endregion

	}
}
