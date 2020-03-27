using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using ImageProcessing.Languages;



namespace ImageProcessing
{
	/// <summary>
	/// Summary description for BookfoldCorrection2D.
	/// </summary>
	public class BookfoldCorrection2D
	{
		static int ph = GetProcessHeap();

		// Heap API flags
		const int HEAP_ZERO_MEMORY = 0x00000008;

		[DllImport("kernel32")]
		static extern int GetProcessHeap();
		[DllImport("kernel32")]
		static unsafe extern void* HeapAlloc(int hHeap, int flags, int size);
		[DllImport("kernel32")]
		static unsafe extern bool HeapFree(int hHeap, int flags, void* block);
		[DllImport("kernel32")]
		static unsafe extern void* HeapReAlloc(int hHeap, int flags, void* block, int size);
		[DllImport("kernel32")]
		static unsafe extern int HeapSize(int hHeap, int flags, void* block);
		
		private BookfoldCorrection2D()
		{
		}
		
		#region Get()
		private static int Get(Bitmap bitmap, out Bitmap clip1, out Bitmap clip2, int middleHoriz, float offsetF)
		{
			int					confidence = 0;
			//int					offset = Convert.ToInt32(offsetF * bitmap.HorizontalResolution);
			ItImage				itImage = new ItImage(bitmap, (bitmap.Width > bitmap.Height), ItImage.ScannerType.Bookeye2);

			clip1 = null;
			clip2 = null;

			Operations operations = new Operations(true, offsetF, true, true, false);

			confidence = Convert.ToInt32(100 * itImage.Find(operations));

			clip1 = itImage.GetResult(0);

			if (itImage.TwoPages)
				clip2 = itImage.GetResult(1);

			return confidence;
		}
		#endregion

		#region GetFile() 
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourcePath"></param>
		/// <param name="clip1Path"></param>
		/// <param name="clip2Path"></param>
		/// <param name="confidence"></param>
		/// <param name="jpegCompression"></param>
		/// <param name="flags">1 BW, 2 DRS2</param>
		/// <param name="offset">in inches</param>
		/// <returns></returns>
		public static ErrorCode GetFile(string sourcePath, string clip1Path, string clip2Path, ref short confidence,
			int jpegCompression, float offset, int middleHoriz, int flags) 
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif
			Bitmap		bitmap = null;
			Bitmap		clip1 = null;
			Bitmap		clip2 = null;

			try
			{
				try
				{
					bitmap = new Bitmap(sourcePath) ;
				}
				catch(Exception ex)
				{
					throw new Exception(BIPStrings.CanTGenerateBitmap_STR+".\nException: " + ex);
				}

				if(middleHoriz == 0)
					middleHoriz = bitmap.Height / 2;

				confidence = (short)Get(bitmap, out clip1, out clip2, middleHoriz, offset);

				if(confidence > 0)
				{
					try
					{
						if(File.Exists(clip1Path))
							File.Delete(clip1Path);
						if(File.Exists(clip2Path))
							File.Delete(clip2Path);
					}
					catch(Exception ex)
					{						
						throw new Exception("Can't delete old images from disk.\nException " + ex);
					}

					try
					{
						ImageCodecInfo		codec;
						EncoderParameters	encoderParams;
						
						if(flags == 1 && clip1.PixelFormat != PixelFormat.Format1bppIndexed)
						{
							Bitmap	copy = DRS.Binorize(clip1, 0, 40);
							clip1.Dispose();
							clip1 = copy;

							copy = DRS.Binorize(clip2, 0, 40);
							clip2.Dispose();
							clip2 = copy;

							if(Path.GetExtension(clip1Path).ToLower() == "png")
							{
								codec = Encoding.GetCodecInfo(ImageFormat.Png);
								encoderParams = new EncoderParameters(1) ;
								encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 1L) ;
							}
							else
							{
								codec = Encoding.GetCodecInfo(ImageFormat.Png);
								encoderParams = new EncoderParameters(1) ;
								encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionCCITT4);
							}
						}
						else if(flags == 2 && clip1.PixelFormat != PixelFormat.Format1bppIndexed)
						{
							Bitmap	copy = DRS2.Get(clip1, 0, 0, true);
							clip1.Dispose();
							clip1 = copy;

							copy = DRS2.Get(clip2, 0, 0, true);
							clip2.Dispose();
							clip2 = copy;
						
							if(Path.GetExtension(clip1Path).ToLower() == "png")
							{
								codec = Encoding.GetCodecInfo(ImageFormat.Png);
								encoderParams = new EncoderParameters(1) ;
								encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 1L);
							}
							else
							{
								codec = Encoding.GetCodecInfo(ImageFormat.Png);
								encoderParams = new EncoderParameters(1) ;
								encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionCCITT4);
							}
						}
						else
						{
							codec = Encoding.GetCodecInfo(bitmap);
							encoderParams = Encoding.GetEncoderParams(bitmap, jpegCompression);
						}
						
						clip1.Save(clip1Path, codec, encoderParams);
						clip2.Save(clip2Path, codec, encoderParams);
					}
					catch(Exception ex)
					{
						throw new Exception("Can't save images to disk.\nException " + ex);
					}
				}
			}
			finally
			{
				if(bitmap != null)
					bitmap.Dispose();
				if(clip1 != null)
					clip1.Dispose();
				if(clip2 != null)
					clip2.Dispose();
			}

#if DEBUG
			Console.Write(string.Format("GetFile(): {0}, Confidence:{1}%",  DateTime.Now.Subtract(start).ToString(), confidence)) ;
#endif
			return ErrorCode.OK;
		}
		#endregion

		#region GetStream() 
		public unsafe static ErrorCode GetStream(byte* firstByte, int length, ref short confidence, 
			ResultFormat format, int jpegCompression, float offset, int flags, byte** page1FirstByte, 
			int* page1Length, byte** page2FirstByte, int* page2Length, int middleHoriz) 
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif
			Bitmap		bitmap = null;
			Bitmap		clip1 = null;
			Bitmap		clip2 = null;

			try
			{
				byte[]			array = new byte[length];
				Marshal.Copy(new IntPtr(firstByte), array, 0, length);

				MemoryStream	stream = new MemoryStream(array);

				try
				{
					bitmap = new Bitmap(stream) ;
				}
				catch(Exception ex)
				{
					throw new Exception(BIPStrings.CanTGenerateBitmap_STR+".\nException: " + ex);
				}

				if(middleHoriz == 0)
					middleHoriz = bitmap.Height / 2;

				confidence = (short)Get(bitmap, out clip1, out clip2, middleHoriz, offset);

				if(confidence > 0)
				{
					try
					{
						ImageCodecInfo		codec;
						EncoderParameters	encoderParams;
						
						if(flags == 1 && clip1.PixelFormat != PixelFormat.Format1bppIndexed)
						{
							Bitmap	copy = DRS.Binorize(clip1, 0, 40);
							clip1.Dispose();
							clip1 = copy;

							copy = DRS.Binorize(clip2, 0, 40);
							clip2.Dispose();
							clip2 = copy;

							codec = Encoding.GetCodecInfo(format);
							encoderParams = Encoding.GetEncoderParams(format, Encoding.GetColorDepth(bitmap));
						}
						else if(flags == 2 && clip1.PixelFormat != PixelFormat.Format1bppIndexed)
						{
							Bitmap	copy = DRS2.Get(clip1, 0, 0, true);
							clip1.Dispose();
							clip1 = copy;

							copy = DRS2.Get(clip2, 0, 0, true);
							clip2.Dispose();
							clip2 = copy;
						
							codec = Encoding.GetCodecInfo(format);
							encoderParams = Encoding.GetEncoderParams(format, Encoding.GetColorDepth(bitmap));
						}
						else
						{
							codec = Encoding.GetCodecInfo(bitmap);
							encoderParams = Encoding.GetEncoderParams(bitmap, jpegCompression);
						}
						
						MemoryStream	resultStream = new MemoryStream();
						clip1.Save(resultStream, codec, encoderParams);

						*page1Length = (int) resultStream.Length;
						*page1FirstByte = (byte*) HeapAlloc(ph, HEAP_ZERO_MEMORY, (int) resultStream.Length);
						Marshal.Copy(resultStream.ToArray(), 0, new IntPtr(*firstByte), (int) resultStream.Length);
						resultStream.Close();

						resultStream = new MemoryStream();
						clip2.Save(resultStream, codec, encoderParams);
						*page2Length = (int) resultStream.Length;
						*page2FirstByte = (byte*) HeapAlloc(ph, HEAP_ZERO_MEMORY, (int) resultStream.Length);
						Marshal.Copy(resultStream.ToArray(), 0, new IntPtr(*page2FirstByte), (int) resultStream.Length);
						resultStream.Close();
					}
					catch(Exception ex)
					{
						throw new Exception("Can't save images to disk.\nException " + ex);
					}
				}
			}
			finally
			{
				if(bitmap != null)
					bitmap.Dispose();
				if(clip1 != null)
					clip1.Dispose();
				if(clip2 != null)
					clip2.Dispose();
			}

#if DEBUG
			Console.Write(string.Format("GetStream(): {0}, Confidence:{1}%",  DateTime.Now.Subtract(start).ToString(), confidence)) ;
#endif
			return ErrorCode.OK;
		}
		#endregion

	}
}
