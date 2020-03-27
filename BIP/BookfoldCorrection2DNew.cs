using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;



namespace ImageProcessing
{
	/// <summary>
	/// Summary description for BookfoldCorrection2DNew.
	/// </summary>
	public class BookfoldCorrection2DNew
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
		
		public BookfoldCorrection2DNew()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		#region FindCurvingOnPage()
		public static BookfoldParams FindCurvingOnPage(Bitmap bitmap, Rectangle clip, int minDelta, int middleHoriz, int flag)
		{
			Bitmap		bmpEdgeDetect;
			
			try
			{
				if(clip.IsEmpty)
					clip = new Rectangle(0,0,bitmap.Width, bitmap.Height);
				
				bmpEdgeDetect = ImageProcessing.BookeyeImagePreprocessing.Go(bitmap, 0, minDelta);
									
				BookfoldParams	bookfoldParams = PageCurveCorrection.FindPoints( bmpEdgeDetect, clip, middleHoriz, flag);
				
				bmpEdgeDetect.Dispose();
				return bookfoldParams;
			}
			catch(Exception ex)
			{
				ex = ex;
				return null;
			}
		}
		#endregion
		
		#region FindCurvingOnImage()
		public static int FindCurvingOnImage(Bitmap sourceBmp, ref BookfoldParams bfParams1, ref BookfoldParams bfParams2, int minDelta)
		{
			Bitmap		bmpEdgeDetect = null;
			byte		confidence = 0;
			int			middleHoriz = (int) (sourceBmp.Height - Convert.ToInt32(sourceBmp.HorizontalResolution) * 7);
			
			try
			{				
				bmpEdgeDetect = ImageProcessing.BookeyeImagePreprocessing.Go(sourceBmp, 0, minDelta);
				
				//book fold center
				int			bookfold = BookeyePageSplitter.Get(bmpEdgeDetect, 20, new RectangleF(.35F, .15F, .3F, .70F), .125F, .02F, out confidence);
				
				if(bookfold > 0)
				{
					byte confL, confR;
					
					//find content
					Rectangle	clipLeft = PageContentLocator.Get(bmpEdgeDetect, new Rectangle(0, 0, bookfold, bmpEdgeDetect.Height), 20, 
						new RectangleF(.4F, .10F, .2F, .80F), .25F, .01F, 100, 1, sourceBmp.HorizontalResolution * .5, out confL);
					Rectangle	clipRight = PageContentLocator.Get(bmpEdgeDetect, new Rectangle(bookfold, 0, bmpEdgeDetect.Width - bookfold, bmpEdgeDetect.Height),
						20, new RectangleF(.4F, .10F, .2F, .80F), .25F, .01F, 100, 2, sourceBmp.HorizontalResolution * .5, out confR);

					bfParams1 = ImageProcessing.PageCurveCorrection.FindPoints(bmpEdgeDetect, clipLeft, middleHoriz, 1);
					bfParams2 = ImageProcessing.PageCurveCorrection.FindPoints(bmpEdgeDetect, clipRight, middleHoriz, 1);
				
					return confidence;
				}
				else
				{
					bmpEdgeDetect.Dispose();
					return confidence;
				}
			}
			catch(Exception ex)
			{
				ex = ex;
				return confidence;
			}
			finally
			{
				if(bmpEdgeDetect != null)
					bmpEdgeDetect.Dispose();
			}
		}
		#endregion
		
		#region Get()
		public static int Get(Bitmap sourceBmp, ref Bitmap clip1, ref Bitmap clip2, int minDelta)
		{
			Bitmap		bmpEdgeDetect = null;
			byte		confidence = 0;
			
			try
			{				
				bmpEdgeDetect = ImageProcessing.BookeyeImagePreprocessing.Go(sourceBmp, 0, minDelta);

				Rectangle clip = PageSplitter.GetRawClip(bmpEdgeDetect, Rectangle.Empty, 20, new RectangleF(.4F, .10F, .2F, .80F), .5F, .02F,
					100, 
				blockSize, percentageWhite, maxValidPointDistanceToMedian, flag, out confidence)
				
				//book fold center
				int			bookfold = PageSplitter.GetFromObjects(bmpEdgeDetect, 20, new RectangleF(.35F, .15F, .3F, .70F), .125F, .02F, out confidence);
				
				if(bookfold > 0)
				{
					byte confL, confR;
					
					//find content
					Rectangle	clipLeft = PageContentLocator.Get(bmpEdgeDetect, new Rectangle(0, 0, bookfold, bmpEdgeDetect.Height), 
						20, new RectangleF(.4F, .10F, .2F, .80F), .25F, .01F, 100, 1, sourceBmp.HorizontalResolution * .5, out confL);
					Rectangle	clipRight = PageContentLocator.Get(bmpEdgeDetect, new Rectangle(bookfold, 0, bmpEdgeDetect.Width - bookfold, bmpEdgeDetect.Height), 
						20, new RectangleF(.4F, .10F, .2F, .80F), .25F, .01F, 100, 2, sourceBmp.HorizontalResolution * .5, out confR);

					clip1 = PageCurveCorrection.GetFromRasterImage(sourceBmp, clipLeft, bmpEdgeDetect, (int) (sourceBmp.Height - Convert.ToInt32(sourceBmp.HorizontalResolution) * 7), 1);
					clip2 = PageCurveCorrection.GetFromRasterImage(sourceBmp, clipRight, bmpEdgeDetect, (int) (sourceBmp.Height - Convert.ToInt32(sourceBmp.HorizontalResolution) * 7), 1);
				
					return confidence;
				}
				else
				{
					bmpEdgeDetect.Dispose();
					return confidence;
				}
			}
			catch(Exception ex)
			{
				ex = ex;
				return 0;
			}
			finally
			{
				if(bmpEdgeDetect != null)
					bmpEdgeDetect.Dispose();
			}
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
		/// <returns></returns>
		public static ErrorCode GetFile(string sourcePath, string clip1Path, string clip2Path, ref short confidence,
			int jpegCompression, int flags) 
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
					throw new Exception("Can't generate bitmap.\nException " + ex);
				}

				confidence = (short) Get(bitmap, ref clip1, ref clip2, 30);

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
							Bitmap	copy = DRS2.Binorize(clip1, 0, 40, true);
							clip1.Dispose();
							clip1 = copy;

							copy = DRS2.Binorize(clip2, 0, 40, true);
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
			ResultFormat format, int jpegCompression, int flags, byte** page1FirstByte, 
			int* page1Length, byte** page2FirstByte, int* page2Length) 
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
					throw new Exception("Can't generate bitmap.\nException " + ex);
				}
			
					
				confidence = (short) Get(bitmap, ref clip1, ref clip2, 30);

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
							encoderParams = Encoding.GetEncoderParams(format);
						}
						else if(flags == 2 && clip1.PixelFormat != PixelFormat.Format1bppIndexed)
						{
							Bitmap	copy = DRS2.Binorize(clip1, 0, 40, true);
							clip1.Dispose();
							clip1 = copy;

							copy = DRS2.Binorize(clip2, 0, 40, true);
							clip2.Dispose();
							clip2 = copy;
						
							codec = Encoding.GetCodecInfo(format);
							encoderParams = Encoding.GetEncoderParams(format);
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

		//PRIVATE METHODS

		#region Get1BppImage()
		/*private static Bitmap Get1BppImage(Bitmap bitmap, int minDelta)
		{
			Bitmap		bmpPreprocessed = null;
			Bitmap		bmpEdgeDetect;
			
			switch(bitmap.PixelFormat)
			{
				case 		PixelFormat.Format8bppIndexed:	
					bmpPreprocessed = ImageProcessing.BookeyeImagePreprocessing.Go(bitmap, 0, minDelta);
					bmpEdgeDetect = ImageProcessing.EdgeDetector.Get(bmpPreprocessed, Rectangle.Empty, 200, 200, 200, minDelta, EdgeDetector.Operator.Laplacian446a);
					break;
				case 		PixelFormat.Format24bppRgb:	
					bmpPreprocessed = ImageProcessing.BookeyeImagePreprocessing.Go(bitmap, -20);
					bmpEdgeDetect = ImageProcessing.EdgeDetector.Get(bmpPreprocessed, Rectangle.Empty, 200, 200, 200, minDelta, EdgeDetector.Operator.Laplacian446a);
					break;
				default:	
					bmpEdgeDetect = ImageProcessing.EdgeDetector.Get(bitmap, Rectangle.Empty, 200, 200, 200, minDelta, EdgeDetector.Operator.Laplacian446a);
					break;
			}
					
			if(bmpPreprocessed != null)
				bmpPreprocessed.Dispose();

			ImageProcessing.NoiseReduction.Despeckle(bmpEdgeDetect, 1);
			ImageProcessing.NoiseReduction.Despeckle(bmpEdgeDetect, 2);
			ImageProcessing.NoiseReduction.Despeckle(bmpEdgeDetect, 3);
			ImageProcessing.NoiseReduction.Despeckle(bmpEdgeDetect, 4);
			ImageProcessing.NoiseReduction.Despeckle(bmpEdgeDetect, 3);
			ImageProcessing.NoiseReduction.Despeckle(bmpEdgeDetect, 2);
		
			return bmpEdgeDetect;
		}*/
		#endregion

	}
}
