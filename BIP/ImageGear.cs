using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Runtime.InteropServices;
using System.IO;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	/*public unsafe class ImageGear
	{		
		static Bitmap		image = null;
		static IntPtr		hiGear = IntPtr.Zero;
		
		// values for IG_IP_rotate_ function 
		static UInt16	IG_ROTATE_CLIP     = 0;
		//static UInt16	IG_ROTATE_EXPAND   = 1;

		//static uint		IG_DIB_AREA_RAW = 0;			// all pixels as they are found
		static uint		IG_DIB_AREA_DIB = 1;			// pads rows to long boundries
		//static uint		IG_DIB_AREA_UNPACKED = 2;		// 1 pixel per byte or 3 bytes (24)

		// Pixel Access data format
		//static uint		IG_PIXEL_PACKED		= 1;
		//static uint		IG_PIXEL_UNPACKED	= 2;
		//static uint		IG_PIXEL_RLE		= 3;

		
		
		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_load_file(String path, IntPtr *hiGear);
		
		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_load_raw_FD(IntPtr firstByte, Int32 width, Int32 height,
			UInt32 bitsPerPixel, UInt16 fillOrder, UInt16 compression, IntPtr* hIGear) ;

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_load_raw_mem(IntPtr firstByte, int imageSize, Int32 width, Int32 height,
			UInt32 bitsPerPixel, UInt16 fillOrder, UInt16 compression, IntPtr* hIGear) ;

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_load_mem(IntPtr imagePtr, int imageSize, uint clip,uint reserved, IntPtr* hiGear);

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_image_import_DDB(IntPtr hBitmap, IntPtr palette, IntPtr* hiGear);

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_image_delete(IntPtr hiGear);

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_save_file(IntPtr hIGear, String path, UInt16 format) ;

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_image_import_DIB(IntPtr lpDIB, IntPtr *hiGear) ;

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_DIB_area_get(IntPtr hIGear, AT_RECT *lpRect, IntPtr buffer, uint format);

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_DIB_area_size_get(IntPtr hIGear, AT_RECT *lpRect, uint format, int *lpSize);

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_DIB_raster_get(IntPtr hiGear, uint rowIndex, IntPtr buffer, uint format);

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_DIB_raster_size_get(IntPtr hIGear, uint format, int *lpSize);

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_image_rect_get(IntPtr hiGear, AT_RECT* rect);

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_IP_rotate_any_angle_bkgrnd(IntPtr hIGear, double angle, 
			UInt16 rotateMode, IntPtr bkgrndColor) ;

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_error_clear( ) ;

		[DllImport("Gear32pd.dll")]
		private unsafe static extern UInt32 IG_error_get( int errorIndex, String fileName, int fileSize, int *lineNumber, int *errorCode,
			long *value1, long *value2) ;

		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);


		// Declares a managed structure for the unmanaged structure.
		[ StructLayout( LayoutKind.Sequential )]
			public struct AT_RGB
		{
			public byte		Blue ;
			public byte		Green ;
			public byte		Red ;

			public AT_RGB(byte red, byte green, byte blue)
			{
				this.Red = red;
				this.Green = green;
				this.Blue = blue;
			}
		}

		// Declares a managed structure for the unmanaged structure.
		[ StructLayout( LayoutKind.Sequential )]
			public struct AT_RECT
		{
			public int		left ;
			public int		top ;
			public int		right ;
			public int		bottom ;
		}

		// Declares a managed structure for the unmanaged structure.
		[ StructLayout( LayoutKind.Sequential )]
			public struct AT_POINT
		{
			public int		X ;
			public int		Y ;
		}

		
		public ImageGear()
		{
		}

		//	PUBLIC METHODS
		#region LoadImage(Bitmap image)
		static public void LoadImage(Bitmap bitmap)
		{
			if(image != bitmap)
			{
				if(image != null)
					UnloadImage();

				image = bitmap;
			}
		}
		#endregion

		#region UnloadImage()
		static public void UnloadImage()
		{
			if(hiGear != IntPtr.Zero)
			{
				IG_image_delete(hiGear);
				hiGear = IntPtr.Zero;
			}

			image = null;
		}
		#endregion

		#region Rotate()
		static public void Rotate(double angle)
		{
			if(image == null)
				throw new Exception("Rotate(): no image loaded!");
			
			BitmapData	sourceData = null;

			try
			{				
				sourceData = image.LockBits(Rectangle.FromLTRB(0, 0, image.Width, image.Height),
					ImageLockMode.ReadWrite, image.PixelFormat);
			
				UInt32		errCount = 0;
				UInt32		bitsPerPixel = BitsPerPixel(sourceData.PixelFormat);
				IntPtr		bkgrndColor;

				unsafe
				{
					AT_RECT			rect;

					if(hiGear == IntPtr.Zero)
					{
						switch(bitsPerPixel)
						{
							case 24:
							{
								IntPtr		hiGearTmp ;
								IntPtr		hBitmap = image.GetHbitmap();

								errCount = IG_image_import_DDB(hBitmap, IntPtr.Zero,  &hiGearTmp);

								if(errCount > 0)
									throw new Exception(IG_ErrorText(errCount));

								hiGear = hiGearTmp;
							} break;
							case 8:
							{
								MemoryStream	stream = new MemoryStream();
								IntPtr			hiGearTmp ;

								image.Save(stream, ImageFormat.Bmp);
								IntPtr		imageMem = Marshal.AllocHGlobal((int) stream.Length);
								Marshal.Copy(stream.GetBuffer(), 0, imageMem, (int) stream.Length);

								errCount = IG_load_mem(imageMem, (int) stream.Length, (uint) 1, (uint) 1, &hiGearTmp);

								if(errCount > 0)
									throw new Exception(IG_ErrorText(errCount));

								hiGear = hiGearTmp;
							}break;
							default:
							{
								MemoryStream	stream = new MemoryStream();
								IntPtr			hiGearTmp ;

								image.Save(stream, ImageFormat.Bmp);
								IntPtr		imageMem = Marshal.AllocHGlobal((int) stream.Length);
								Marshal.Copy(stream.GetBuffer(), 0, imageMem, (int) stream.Length);

								errCount = IG_load_mem(imageMem, (int) stream.Length, (uint) 1, (uint) 1, &hiGearTmp);

								stream.Close();
								Marshal.FreeHGlobal(imageMem);

								if(errCount > 0)
									throw new Exception(IG_ErrorText(errCount));

								hiGear = hiGearTmp;
							}break;
						}
					}

					if(errCount > 0)
						throw new Exception(IG_ErrorText(errCount));

					errCount = IG_image_rect_get(hiGear, &rect) ;
				
					if(errCount > 0)
						throw new Exception(IG_ErrorText(errCount));

					if(bitsPerPixel > 8)
					{
						AT_RGB	rgb = new AT_RGB(255, 255, 255);
						bkgrndColor = Marshal.AllocHGlobal(3);
						Marshal.StructureToPtr(rgb, bkgrndColor, true);
					}
					else
					{
						bkgrndColor = Marshal.AllocHGlobal(1);
						Marshal.WriteByte(bkgrndColor, 255);
					}

					errCount = IG_IP_rotate_any_angle_bkgrnd(hiGear, angle, IG_ROTATE_CLIP, bkgrndColor) ;

					if(errCount > 0)
						throw new Exception(IG_ErrorText(errCount));

		
					switch(bitsPerPixel)
					{
						case 24:
						{
							errCount = IG_DIB_area_get(hiGear, &rect, sourceData.Scan0, IG_DIB_AREA_DIB) ;
						}break;
						case 8:
						{
							errCount = IG_DIB_area_get(hiGear, &rect, sourceData.Scan0, IG_DIB_AREA_DIB) ;

							if(errCount > 0)
								throw new Exception("IG_DIB_raster_size_get(): " + IG_ErrorText(errCount));
						}break;
						default:
						{
							errCount = IG_DIB_area_get(hiGear, &rect, sourceData.Scan0, IG_DIB_AREA_DIB) ;

							if(errCount > 0)
								throw new Exception("IG_DIB_raster_size_get(): " + IG_ErrorText(errCount));
						}break;
					}

					if(errCount > 0)
						throw new Exception(IG_ErrorText(errCount));
				}
			}
			finally
			{
				if(sourceData != null)
					image.UnlockBits(sourceData);
			}
		}
		#endregion
		
		//PRIVATE METHODS
		#region BitsPerPixel()
		public static ushort BitsPerPixel(PixelFormat pixelFormat)
		{
			switch(pixelFormat)
			{
				case PixelFormat.Format24bppRgb:
					return 24;
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppPArgb:
				case PixelFormat.Format32bppRgb:
					return 32;
				case PixelFormat.Format8bppIndexed:
					return 8;
				case PixelFormat.Format16bppArgb1555:
				case PixelFormat.Format16bppGrayScale:
				case PixelFormat.Format16bppRgb555:
				case PixelFormat.Format16bppRgb565:
					return 16;
				case PixelFormat.Format48bppRgb:
					return 48;
				case PixelFormat.Format4bppIndexed:
					return 4;
				case PixelFormat.Format64bppArgb:
				case PixelFormat.Format64bppPArgb:
					return 64;
				default:
					return 1;
			}
		}
		#endregion

		#region IG_ErrorText()
		private static unsafe string IG_ErrorText(uint errorCount)
		{
			string		error = "" ;
							
			for(int i = 0; i < errorCount; i++)
			{
				int		errorCode = 0 ;

				IG_error_get(i, null, 0, null, &errorCode, null, null) ;
				error += string.Format("Error {0}:{1}\n", i + 1, errorCode) ;
			}

			return error ;
		}
		#endregion

	}*/
}
