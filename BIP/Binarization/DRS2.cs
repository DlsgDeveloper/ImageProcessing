using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using ImageProcessing.Languages;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class DRS2
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


		//	PUBLIC METHODS
		#region public methods

		#region Get()
		/// <summary>
		/// same as Get in full image region
		/// </summary>
		/// <param name="source"></param>
		/// <param name="bThresholdDelta"></param>
		/// <param name="wThresholdDelta"></param>
		/// <param name="textPreferred"></param>
		/// <returns></returns>
		public static Bitmap Get(Bitmap source, int bThresholdDelta, int wThresholdDelta, bool textPreferred)
		{
			return Get(source, Rectangle.Empty, bThresholdDelta, wThresholdDelta, true, textPreferred);
		}

		/// <summary>
		/// It figures out the white and black thresholds by examining histogram.\n\n
		/// wThreshold = Math.Max(10, Math.Min(255, Math.Max(Histogram.ToGray(histogram.Threshold), Histogram.ToGray(histogram.SecondExtreme)) + wThresholdDelta))
		/// bThreshold = Math.Max(10, Math.Min(wThreshold - 1, Math.Min(Histogram.ToGray(histogram.Extreme), Histogram.ToGray(histogram.SecondExtreme)) + bThresholdDelta + 20));
		/// </summary>
		/// <param name="bmpSource"></param>
		/// <param name="clip"></param>
		/// <param name="bThresholdDelta"></param>
		/// <param name="wThresholdDelta"></param>
		/// <param name="resultManaged"></param>
		/// <param name="textPreferred"></param>
		/// <returns></returns>
		public static Bitmap Get(Bitmap bmpSource, Rectangle clip, int bThresholdDelta, int wThresholdDelta, bool resultManaged, bool textPreferred)
		{
			if(bmpSource == null)
				return null ;

			if(clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, bmpSource.Width, bmpSource.Height) ;

			byte wThreshold, bThreshold;

			if (bmpSource.PixelFormat == PixelFormat.Format24bppRgb)
			{
				Bitmap histogramBitmap;

				if (clip.Width < 800 || clip.Height < 800)
					histogramBitmap = Resampling.Resample(bmpSource, PixelsFormat.Format8bppGray);
				else
					histogramBitmap = Interpolation.Interpolate24bppTo8bpp2to1(bmpSource);

				Histogram histogram = new Histogram(histogramBitmap);
				histogramBitmap.Dispose();

				wThreshold = (byte)(Math.Max(10, Math.Min(255, Math.Max(Histogram.ToGray(histogram.Threshold), Histogram.ToGray(histogram.SecondExtreme)) + wThresholdDelta)));
				bThreshold = (byte)(Math.Max(10, Math.Min(wThreshold - 1, Math.Min(Histogram.ToGray(histogram.Extreme), Histogram.ToGray(histogram.SecondExtreme)) + bThresholdDelta + 20)));
			}
			else if (bmpSource.PixelFormat == PixelFormat.Format8bppIndexed)
			{
				Histogram histogram;

				if (clip.Width < 800 || clip.Height < 800)
					histogram = new Histogram(bmpSource, clip);
				else
					histogram = new Histogram(bmpSource, Rectangle.Inflate(clip, -100, -100));

				wThreshold = (byte)(Math.Max(10, Math.Min(255, Math.Max(histogram.Threshold.R, histogram.SecondExtreme.R) + wThresholdDelta)));
				bThreshold = (byte)(Math.Max(10, Math.Min(wThreshold - 1, Math.Min(Histogram.ToGray(histogram.Extreme), histogram.SecondExtreme.R) + bThresholdDelta + 20)));
			}
			else
				throw new IpException(ErrorCode.ErrorUnsupportedFormat);

			return GetUsingSolidThresholds(bmpSource, clip, bThreshold, wThreshold, resultManaged, textPreferred);
		}
		#endregion

		#region GetUsingSolidThresholds()
		/// <summary>
		/// same as GetUsingSolidThresholds, just for full image
		/// </summary>
		/// <param name="bmpSource"></param>
		/// <param name="bThreshold"></param>
		/// <param name="wThreshold"></param>
		/// <param name="textPreferred"></param>
		/// <returns></returns>
		public static Bitmap GetUsingSolidThresholds(Bitmap bmpSource, byte bThreshold, byte wThreshold, bool textPreferred)
		{
			return GetUsingSolidThresholds(bmpSource, Rectangle.Empty, bThreshold, wThreshold, true, textPreferred);
		}

		/// <summary>
		/// dithers pixels with value between bThreshold and wThreshold
		/// </summary>
		/// <param name="bmpSource"></param>
		/// <param name="clip"></param>
		/// <param name="bThreshold">black threshold. if pixel is darker, it makes 4 black pixels</param>
		/// <param name="wThreshold">white threshold. if pixel is lighter, it makes 4 white pixels</param>
		/// <param name="resultManaged"></param>
		/// <param name="textPreferred"></param>
		/// <returns></returns>
		public static Bitmap GetUsingSolidThresholds(Bitmap bmpSource, Rectangle clip, byte bThreshold, byte wThreshold, bool resultManaged, bool textPreferred)
		{
			if (bmpSource == null)
				return null;

			if (clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, bmpSource.Width, bmpSource.Height);

			Bitmap bmpResult = null;

			try
			{
				switch (bmpSource.PixelFormat)
				{
					case PixelFormat.Format1bppIndexed:
						{
							bmpResult = ImageCopier.Copy(bmpSource, clip);
						} break;
					case PixelFormat.Format8bppIndexed:
						bmpResult = Get8bpp(bmpSource, clip, bThreshold, wThreshold, resultManaged, textPreferred);
						break;
					case PixelFormat.Format24bppRgb:
						bmpResult = Get24bpp(bmpSource, clip, bThreshold, wThreshold, resultManaged, textPreferred);
						break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}

				if (bmpResult != null)
				{
					Misc.SetBitmapResolution(bmpResult, bmpSource.HorizontalResolution * 2, bmpSource.VerticalResolution * 2);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("DRS2, Get(): " + ex.Message);
			}

			return bmpResult;
		}
		#endregion

		#region GetStream()
		public unsafe static int GetStream(byte** firstByte, int* length, Rectangle clip, int bThresholdDelta,
			int wThresholdDelta, ResultFormat resultFormat, bool textPreferred) 
		{ 			
#if DEBUG
			DateTime		enterTime = DateTime.Now ;
#endif
			byte[]			array = new byte[*length];
			Bitmap			bitmap;


			Marshal.Copy(new IntPtr(*firstByte), array, 0, (int) *length);

			MemoryStream	stream = new MemoryStream(array);

			try
			{
				bitmap = new Bitmap(stream) ;
			}
			catch(Exception ex)
			{
				throw new Exception(BIPStrings.CanTGenerateBitmap_STR+".\nException: " + ex);
			}

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			Bitmap result = Get(bitmap, clip, bThresholdDelta, wThresholdDelta, true, textPreferred);
#if DEBUG
			TimeSpan	time = DateTime.Now.Subtract(start) ;

			Console.Write(string.Format("DRS2: {0}",  time.ToString())) ;
#endif
				
			ImageCodecInfo		codecInfo = Encoding.GetCodecInfo(resultFormat);
			EncoderParameters	encoderParams = Encoding.GetEncoderParams(resultFormat, Encoding.GetColorDepth(result));
				
			bitmap.Dispose();
			stream.Close();

			MemoryStream	resultStream = new MemoryStream();
			try
			{
				result.Save(resultStream, codecInfo, encoderParams);
			}
			catch(Exception ex)
			{
				throw new Exception(string.Format(BIPStrings.CanTSaveBitmapToStream_STR+".\nException: {0}\nStream : {1}\n" + 
					"Codec Info: {2}\nEncoder: {3}", ex.Message, (resultStream != null) ? "Exists": "null", 
					(codecInfo != null) ? codecInfo.CodecName : "null",
					(encoderParams != null) ? encoderParams.Param[0].ToString() : "null") );
			}

			*length = (int) resultStream.Length;
			*firstByte = (byte*) HeapAlloc(ph, HEAP_ZERO_MEMORY, (int) resultStream.Length);
			Marshal.Copy(resultStream.ToArray(), 0, new IntPtr(*firstByte), (int) resultStream.Length);

			result.Dispose() ;
			resultStream.Close();
#if DEBUG
			Console.WriteLine(string.Format(" Total Time: {0}",  DateTime.Now.Subtract(enterTime).ToString())) ;
#endif

			return 0;
		}
		#endregion
		
		#region GetMem()
		public unsafe static int GetMem(ref int width, ref int height, ref int stride, PixelFormat pixelFormat,
			byte** firstByte, ColorPalette palette, Rectangle clip, int bThresholdDelta, int wThresholdDelta, bool textPreferred) 
		{ 			
			SecurityPermission sp = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);

			sp.Assert();

			Bitmap		bitmap = new Bitmap(width, height, stride, pixelFormat, new IntPtr(*firstByte));			
			
			if(bitmap == null)
				throw new Exception("DRS2(): "+BIPStrings.CanTCreateBitmapFromPresentParameters_STR) ;

			if(palette != null)
				bitmap.Palette = palette;
			
			Bitmap			rBitmap = null;	

			if(clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, width, height);
			else if(clip.Width == 0 || clip.Height == 0)
				clip = Rectangle.FromLTRB(clip.X, clip.Y, bitmap.Width - clip.X * 2, bitmap.Height - clip.Y * 2);

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif
			
			try
			{
				rBitmap = Get(bitmap, clip, bThresholdDelta, wThresholdDelta, false, textPreferred);

				BitmapData	bmpData = rBitmap.LockBits( new Rectangle(0, 0, rBitmap.Width, rBitmap.Height), ImageLockMode.ReadOnly, rBitmap.PixelFormat);
			
				width = bmpData.Width;
				height = bmpData.Height;
				stride = bmpData.Stride;
				*firstByte = (byte*) bmpData.Scan0.ToPointer();

				rBitmap.UnlockBits(bmpData);
				rBitmap.Dispose();
				rBitmap = null;
				bitmap.Dispose();
				return 0;
			}
			catch(Exception ex)
			{
				throw new Exception("DRS2(): " + ex.Message) ;
			}
			finally
			{
#if DEBUG
				TimeSpan	time = DateTime.Now.Subtract(start) ;
				Console.WriteLine(string.Format("RAM Image: {0}", time.ToString()));	
#endif
			}
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region GetDir()
		private static void GetDir(DirectoryInfo sourceDir, DirectoryInfo destDir, Rectangle clip,
			ResultFormat format, int bThresholdDelta, int wThresholdDelta, bool textPreferred)
		{
			if(!sourceDir.Exists)
				throw new Exception(BIPStrings.SourceDirectoryOrFileDoesnTExist_STR);
			
			ArrayList	sources = new ArrayList(); 
			Bitmap		bitmap;
#if DEBUG
			TimeSpan	span = new TimeSpan(0);
			DateTime	totalTimeStart = DateTime.Now ;
#endif

			destDir.Create();

			sources.AddRange(sourceDir.GetFiles("*.tif"));
			sources.AddRange(sourceDir.GetFiles("*.jpg"));
			sources.AddRange(sourceDir.GetFiles("*.png"));
			sources.AddRange(sourceDir.GetFiles("*.bmp"));
			sources.AddRange(sourceDir.GetFiles("*.gif"));

			foreach(FileInfo file in sources)
			{
				bitmap = new Bitmap(file.FullName) ;
#if DEBUG
				DateTime	start = DateTime.Now ;
#endif
				Bitmap result = Get(bitmap, clip, bThresholdDelta, wThresholdDelta, true, textPreferred);

#if DEBUG
				TimeSpan	time = DateTime.Now.Subtract(start) ;
				span = span.Add(time);
				Console.WriteLine(string.Format("{0}: {1}",  file.FullName, time.ToString())) ;
#endif

				ImageCodecInfo		codecInfo = Encoding.GetCodecInfo(format);
				EncoderParameters	encoderParams = Encoding.GetEncoderParams(format, Encoding.GetColorDepth(result)) ;

				bitmap.Dispose() ;

				if(File.Exists(destDir.FullName +  @"\" + file.Name))
					File.Delete(destDir.FullName +  @"\" + file.Name);
				
				result.Save(destDir.FullName +  @"\" + file.Name, codecInfo, encoderParams) ;
				result.Dispose() ;
			}

#if DEBUG
			Console.WriteLine("Total time: " + span.ToString());
			Console.WriteLine("Total all time: " + DateTime.Now.Subtract(totalTimeStart).ToString());
#endif
		}
		#endregion

		#region GetFile()
		private static void GetFile(FileInfo sourceFile, FileInfo resultFile, Rectangle clip,
			ResultFormat format, int bThresholdDelta, int wThresholdDelta, bool textPreferred)
		{			
			Bitmap			bitmap = new Bitmap(sourceFile.FullName) ;

#if DEBUG
			DateTime	start = DateTime.Now;
#endif

			Bitmap	result = Get(bitmap, clip, bThresholdDelta, wThresholdDelta, true, textPreferred);
				
			ImageCodecInfo		codecInfo = Encoding.GetCodecInfo(format);
			EncoderParameters	encoderParams = Encoding.GetEncoderParams(format, Encoding.GetColorDepth(result)) ;
				
			bitmap.Dispose() ;

			if(resultFile.Exists)
				resultFile.Delete();
				
			result.Save(resultFile.FullName, codecInfo, encoderParams) ;

			result.Dispose() ;

#if DEBUG
			TimeSpan	time = DateTime.Now.Subtract(start) ;
			Console.WriteLine(string.Format("{0}: {1}",  sourceFile.FullName, time.ToString())) ;
#endif
		}
		#endregion
						
		#region Get24bpp()
		//it creates grayscale histogram and it renders pixels in between histogram peaks. 
		private static Bitmap Get24bpp(Bitmap sourceBmp, Rectangle clip, byte bThreshold, byte wThreshold, bool resultManaged, bool textPreferred)
		{
			Bitmap	resultBmp;
			int width = clip.Width;
			int height = clip.Height;
			
			if(resultManaged)
				resultBmp = new Bitmap(width * 2, height * 2, PixelFormat.Format1bppIndexed); 
			else
			{
				unsafe
				{
					int		stride = Misc.GetStride(clip.Width * 2, PixelFormat.Format1bppIndexed);
					byte*	scan0 = (byte*)HeapAlloc(ph, HEAP_ZERO_MEMORY, stride * clip.Height * 2);

					resultBmp = new Bitmap(clip.Width * 2, clip.Height * 2, stride, PixelFormat.Format1bppIndexed, new IntPtr(scan0)); 
				}
			}

			BitmapData	sourceData = sourceBmp.LockBits(clip, ImageLockMode.ReadOnly, sourceBmp.PixelFormat); 
			BitmapData	resultData = resultBmp.LockBits(new Rectangle(0, 0, resultBmp.Width, resultBmp.Height), ImageLockMode.WriteOnly, resultBmp.PixelFormat); 

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			byte		gray ;
			float		ratio;

			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride;

			unsafe
			{
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				byte* pCurrent;

				int ulcorner;
				int urcorner;
				int llcorner;
				int lrcorner;

				int clipHeight2 = clip.Height - 1;
				int clipWidth2 = clip.Width - 1;
				int x, y;
				int maxCorner;
				int maxIndex;
				int yAdd;
				float thresholdsDistance = (float)(wThreshold - bThreshold);

				if (thresholdsDistance < 1)
					thresholdsDistance = 1;

				for (y = 1; y < clipHeight2; y++)
				{
					pCurrent = pSource + (y * sStride) + 3;

					for (x = 1; x < clipWidth2; x++)
					{
						gray = (byte)(*pCurrent * 0.114F + pCurrent[1] * 0.587F + pCurrent[2] * 0.299F);

						if (gray >= wThreshold)
						{
							//pResult[y       * 2 * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
							//pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
							pResult[(y * rStride << 1) + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) << 1));
							pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) << 1));
						}
						else if (gray > bThreshold)
						{
							ratio = (gray - bThreshold) / (float)thresholdsDistance;

							if (ratio < .50F)
							{
								ulcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + ((pCurrent[-sStride - 3] + pCurrent[-sStride - 2] + pCurrent[-sStride - 1]) >> 1);
								urcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + ((pCurrent[-sStride + 3] + pCurrent[-sStride + 4] + pCurrent[-sStride + 5]) >> 1);
								llcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + ((pCurrent[+sStride - 3] + pCurrent[+sStride - 2] + pCurrent[+sStride - 1]) >> 1);
								lrcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + ((pCurrent[+sStride + 3] + pCurrent[+sStride + 4] + pCurrent[+sStride + 5]) >> 1);
								yAdd = 0;

								if (ulcorner > urcorner)
								{
									maxCorner = ulcorner;
									maxIndex = 0;
								}
								else
								{
									maxCorner = urcorner;
									maxIndex = 1;
								}

								if (llcorner > maxCorner)
								{
									maxCorner = llcorner;
									maxIndex = 0;
									yAdd = 1;
								}
								if (lrcorner > maxCorner)
								{
									maxIndex = 1;
									yAdd = 1;
								}

								pResult[((y << 1) + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
							}
							else if (ratio < .75F)
							{
								/*if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
									pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));
								if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
									pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));
								if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
									pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));
								if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
									pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));
								*/
								pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));
								pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));
							}
							else
							{
								ulcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + ((pCurrent[-sStride - 3] + pCurrent[-sStride - 2] + pCurrent[-sStride - 1]) >> 1);
								urcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + ((pCurrent[-sStride + 3] + pCurrent[-sStride + 4] + pCurrent[-sStride + 5]) >> 1);
								llcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + ((pCurrent[+sStride - 3] + pCurrent[+sStride - 2] + pCurrent[+sStride - 1]) >> 1);
								lrcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + ((pCurrent[+sStride + 3] + pCurrent[+sStride + 4] + pCurrent[+sStride + 5]) >> 1);

								if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
								{
									pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));

									if (urcorner > llcorner || urcorner > lrcorner)
									{
										pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));

										if (llcorner > lrcorner)
											pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));
										else
											pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));
									}
									else
									{
										pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) << 1));
									}
								}
								else
								{
									pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));
									pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) << 1));
								}
							}
						}

						pCurrent += 3;
					}
				}

				//borders
				//TOP
				y = 0;
				pCurrent = pSource;

				for (x = 0; x < width; x++)
				{
					gray = (byte)(*pCurrent * 0.114F + pCurrent[1] * 0.587F + pCurrent[2] * 0.299F);

					if (gray > wThreshold)
					{
						pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					}
					else if (gray > bThreshold)
					{
						ratio = (gray - bThreshold) / (float)thresholdsDistance;

						if (ratio < .50F)
						{
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else if (ratio < .75F)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}

					pCurrent += 3;
				}

				//LEFT
				x = 0;
				pCurrent = pSource + sStride;

				for (y = 1; y < clipHeight2; y++)
				{
					gray = (byte)(*pCurrent * 0.114F + pCurrent[1] * 0.587F + pCurrent[2] * 0.299F);

					if (gray > wThreshold)
					{
						pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					}
					else if (gray > bThreshold)
					{
						ratio = (gray - bThreshold) / (float)thresholdsDistance;

						if (ratio < .50F)
						{
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else if (ratio < .75F)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}

					pCurrent += sStride;
				}

				//BOTTOM
				y = height - 1;
				pCurrent = pSource + (y * sStride);

				for (x = 0; x < width; x++)
				{
					gray = (byte)(*pCurrent * 0.114F + pCurrent[1] * 0.587F + pCurrent[2] * 0.299F);

					if (gray > wThreshold)
					{
						pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					}
					else if (gray > bThreshold)
					{
						ratio = (gray - bThreshold) / (float)thresholdsDistance;

						if (ratio < .50F)
						{
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else if (ratio < .75F)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}

					pCurrent += 3;
				}

				//RIGHT
				x = width - 1;
				pCurrent = pSource + sStride + (width - 1) * 3;

				for (y = 1; y < clipHeight2; y++)
				{
					gray = (byte)(*pCurrent * 0.114F + pCurrent[1] * 0.587F + pCurrent[2] * 0.299F);

					if (gray > wThreshold)
					{
						pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					}
					else if (gray > bThreshold)
					{
						ratio = (gray - bThreshold) / (float)thresholdsDistance;

						if (ratio < .50F)
						{
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else if (ratio < .75F)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}

					pCurrent += sStride;
				}
			}

#if DEBUG
			Console.WriteLine("DRS2 Get24bpp():" + (DateTime.Now.Subtract(start)).ToString()) ;
#endif

			if (textPreferred)
				SqueezePixels24bpp(sourceData, resultData, wThreshold * 3, bThreshold * 3);
			
			sourceBmp.UnlockBits(sourceData);
			resultBmp.UnlockBits(resultData);

			return resultBmp; 
		}
		#endregion

		#region SqueezePixels24bpp()
		private static void  SqueezePixels24bpp(BitmapData sourceData, BitmapData resultData, int wThreshold, int bThreshold)
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			byte		toChangeRange = (byte) (.1F * ((wThreshold - bThreshold) / 3));

			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride; 
			int			x, y;
			 
			unsafe
			{
				byte*		pSourceGray = (byte*)sourceData.Scan0.ToPointer(); 
				byte*		pResultCopy = (byte*)resultData.Scan0.ToPointer();
				byte*		pCurrent ;
				byte*		pResult;

				int			height = resultData.Height - 2;
				int			width = resultData.Width / 8 - 1;
				byte*		lineUp, line1, line2, lineDown;
				int			gray, corner1, corner2, corner3;

				for(y = 2; y < height; y = y + 2)
				{
					for(x = 1; x < width; x++)
					{						
						pCurrent = pSourceGray + (y * sStride / 2) + x * 4 * 3;
						pResult = pResultCopy + y * rStride + x;

						lineUp = pResultCopy + (y-1) * rStride + x;
						line1 = lineUp + rStride;
						line2 = line1 + rStride;
						lineDown = line2 + rStride;

						gray = *pCurrent + pCurrent[1] + pCurrent[2];
						//pixel1 UL
						if(gray < wThreshold && gray >= bThreshold)
						{ 
							if((*line1 & 0x80) == 0)
							{
								corner1 = pCurrent[-sStride-3] + pCurrent[-sStride-2] + pCurrent[-sStride-1];
								corner2 = pCurrent[-sStride] + pCurrent[-sStride+1] + pCurrent[-sStride+2];
								corner3 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp - 1) & 0x01) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x80) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1 - 1) & 0x01) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x80;
									pResult[-rStride-1] &= 0xFE;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x80;
									pResult[-rStride] &= 0x7F;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x80;
									pResult[-1] &= 0xFE;
								}
							}

							//pixel1 UR
							if((*line1 & 0x40) == 0)
							{
								corner1 = pCurrent[-sStride] + pCurrent[-sStride+1] + pCurrent[-sStride+2];
								corner2 = pCurrent[-sStride+3] + pCurrent[-sStride+4] + pCurrent[-sStride+5];
								corner3 = pCurrent[3] + pCurrent[4] + pCurrent[5];
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x40) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x20) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1) & 0x20) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x40;
									pResult[-rStride] &= 0xBF;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x40;
									pResult[-rStride] &= 0xDF;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x40;
									*pResult &= 0xDF;
								}
							}

							//pixel1 LL
							pResult += rStride;

							if((*line2 & 0x80) == 0)
							{
								corner1 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);
								corner2 = pCurrent[sStride-3] + pCurrent[sStride-2] + pCurrent[sStride-1];
								corner3 = pCurrent[sStride] + pCurrent[sStride+1] + pCurrent[sStride+2];
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2 - 1) & 0x01) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown - 1) & 0x01) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x80) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x80;
									pResult[-1] &= 0xFE;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x80;
									pResult[+rStride-1] &= 0xFE;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x80;
									pResult[+rStride] &= 0x7F;
								}
							}

							//pixel1 LR
							if((*line2 & 0x40) == 0)
							{
								corner1 = pCurrent[3] + pCurrent[4] + pCurrent[5];
								corner2 = pCurrent[sStride] + pCurrent[sStride+1] + pCurrent[sStride+2];
								corner3 = pCurrent[sStride+3] + pCurrent[sStride+4] + pCurrent[sStride+5];
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2) & 0x20) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x40) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x20) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x40;
									*pResult &= 0xBF;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x40;
									pResult[rStride] &= 0xBF;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x40;
									pResult[rStride] &= 0xDF;
								}
							}

							pResult -= rStride;
						}					

						//pixel2 UL
						pCurrent += 3;
						gray = *pCurrent + pCurrent[1] + pCurrent[2];

						if(gray < wThreshold && gray >= bThreshold)
						{ 
							if((*line1 & 0x20) == 0)
							{
								corner1 = (pCurrent[-sStride-3] + pCurrent[-sStride-2] + pCurrent[-sStride-1]);
								corner2 = pCurrent[-sStride] + pCurrent[-sStride+1] + pCurrent[-sStride+2];
								corner3 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x40) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x20) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1) & 0x40) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x20;
									pResult[-rStride] &= 0xBF;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x20;
									pResult[-rStride] &= 0xDF;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x20;
									*pResult &= 0xBF;
								}
							}

							//pixel2 UR
							if((*line1 & 0x10) == 0)
							{
								corner1 = pCurrent[-sStride] + pCurrent[-sStride+1] + pCurrent[-sStride+2];
								corner2 = pCurrent[-sStride+3] + pCurrent[-sStride+4] + pCurrent[-sStride+5];
								corner3 = pCurrent[3] + pCurrent[4] + pCurrent[5];
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x10) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x08) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1) & 0x08) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x10;
									pResult[-rStride] &= 0xEF;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x10;
									pResult[-rStride] &= 0xF7;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x10;
									*pResult &= 0xF7;
								}
							}
				
							//pixel2 LL
							pResult += rStride;

							if((*line2 & 0x20) == 0)
							{
								corner1 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);
								corner2 = pCurrent[sStride-3] + pCurrent[sStride-2] + pCurrent[sStride-1];
								corner3 = pCurrent[sStride] + pCurrent[sStride+1] + pCurrent[sStride+2];
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2) & 0x40) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x40) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x20) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x20;
									*pResult &= 0xBF;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x20;
									pResult[rStride] &= 0xBF;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x20;
									pResult[rStride] &= 0xDF;
								}
							}

							//pixel2 LR
							if((*line2 & 0x10) == 0)
							{
								corner1 = pCurrent[3] + pCurrent[4] + pCurrent[5];
								corner2 = pCurrent[sStride] + pCurrent[sStride+1] + pCurrent[sStride+2];
								corner3 = pCurrent[sStride+3] + pCurrent[sStride+4] + pCurrent[sStride+5];
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2) & 0x08) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x10) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x08) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x10;
									*pResult &= 0xF7;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x10;
									pResult[rStride] &= 0xEF;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x10;
									pResult[rStride] &= 0xF7;
								}
							}

							pResult -= rStride;
						}
								
						//pixel3 UL
						pCurrent += 3;
						gray = *pCurrent + pCurrent[1] + pCurrent[2];

						if(gray < wThreshold && gray >= bThreshold)
						{ 
							if((*line1 & 0x08) == 0)
							{
								corner1 = (pCurrent[-sStride-3] + pCurrent[-sStride-2] + pCurrent[-sStride-1]);
								corner2 = pCurrent[-sStride] + pCurrent[-sStride+1] + pCurrent[-sStride+2];
								corner3 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x10) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x08) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1) & 0x10) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x08;
									pResult[-rStride] &= 0xEF;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x08;
									pResult[-rStride] &= 0xF7;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x08;
									*pResult &= 0xEF;
								}
							}

							//pixel3 UR
							if((*line1 & 0x04) == 0)
							{
								corner1 = pCurrent[-sStride] + pCurrent[-sStride+1] + pCurrent[-sStride+2];
								corner2 = pCurrent[-sStride+3] + pCurrent[-sStride+4] + pCurrent[-sStride+5];
								corner3 = pCurrent[3] + pCurrent[4] + pCurrent[5];
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x04) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x02) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1) & 0x02) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x04;
									pResult[-rStride] &= 0xFB;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x04;
									pResult[-rStride] &= 0xFD;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x04;
									*pResult &= 0xFD;
								}
							}

							//pixel3 LL
							pResult += rStride;

							if((*line2 & 0x08) == 0)
							{
								corner1 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);
								corner2 = pCurrent[sStride-3] + pCurrent[sStride-2] + pCurrent[sStride-1];
								corner3 = pCurrent[sStride] + pCurrent[sStride+1] + pCurrent[sStride+2];
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2) & 0x10) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x10) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x08) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x08;
									*pResult &= 0xEF;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x08;
									pResult[rStride] &= 0xEF;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x08;
									pResult[rStride] &= 0xF7;
								}
							}

							//pixel3 LR
							if((*line2 & 0x04) == 0)
							{
								corner1 = pCurrent[3] + pCurrent[4] + pCurrent[5];
								corner2 = pCurrent[sStride] + pCurrent[sStride+1] + pCurrent[sStride+2];
								corner3 = pCurrent[sStride+3] + pCurrent[sStride+4] + pCurrent[sStride+5];
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2) & 0x02) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x04) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x02) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x04;
									*pResult &= 0xFD;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x04;
									pResult[rStride] &= 0xFB;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x04;
									pResult[rStride] &= 0xFD;
								}
							}

							pResult -= rStride;
						}
								
						//pixel4 UL
						pCurrent += 3;
						gray = *pCurrent + pCurrent[1] + pCurrent[2];

						if(gray < wThreshold && gray >= bThreshold)
						{ 
							if((*line1 & 0x02) == 0)
							{
								corner1 = (pCurrent[-sStride-3] + pCurrent[-sStride-2] + pCurrent[-sStride-1]);
								corner2 = pCurrent[-sStride] + pCurrent[-sStride+1] + pCurrent[-sStride+2];
								corner3 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x04) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x02) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1) & 0x04) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x02;
									pResult[-rStride] &= 0xFB;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x02;
									pResult[-rStride] &= 0xFD;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x02;
									*pResult &= 0xFB;
								}
							}

							//pixel4 UR
							if((*line1 & 0x01) == 0)
							{
								corner1 = pCurrent[-sStride] + pCurrent[-sStride+1] + pCurrent[-sStride+2];
								corner2 = pCurrent[-sStride+3] + pCurrent[-sStride+4] + pCurrent[-sStride+5];
								corner3 = pCurrent[3] + pCurrent[4] + pCurrent[5];
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x01) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp + 1) & 0x80) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1 + 1) & 0x80) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x01;
									pResult[-rStride] &= 0xFE;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x01;
									pResult[-rStride+1] &= 0x7F;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x01;
									pResult[1] &= 0x7F;
								}
							}

							//pixel4 LL
							pResult += rStride;

							if((*line2 & 0x02) == 0)
							{
								corner1 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);
								corner2 = pCurrent[sStride-3] + pCurrent[sStride-2] + pCurrent[sStride-1];
								corner3 = pCurrent[sStride] + pCurrent[sStride+1] + pCurrent[sStride+2];
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2) & 0x04) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x04) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x02) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x02;
									*pResult &= 0xFB;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x02;
									pResult[rStride] &= 0xFB;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x02;
									pResult[rStride] &= 0xFD;
								}
							}

							//pixel4 LR
							if((*line2 & 0x01) == 0)
							{
								corner1 = pCurrent[3] + pCurrent[4] + pCurrent[5];
								corner2 = pCurrent[sStride] + pCurrent[sStride+1] + pCurrent[sStride+2];
								corner3 = pCurrent[sStride+3] + pCurrent[sStride+4] + pCurrent[sStride+5];
									
								if(corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2 + 1) & 0x80) == 0) )
									corner1 = int.MaxValue;

								if(corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x01) == 0) )
									corner2 = int.MaxValue;
								
								if(corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown + 1) & 0x80) == 0) )
									corner3 = int.MaxValue;

								if(corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x01;
									pResult[1] &= 0x7F;
								}
								else if(corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x01;
									pResult[rStride] &= 0xFE;
								}
								else if(corner3 < 768)
								{
									*pResult |= 0x01;
									pResult[rStride + 1] &= 0x7F;
								}
							}
						}
					}
				}
			}

#if DEBUG
			Console.WriteLine("DRS2 SqueezePixels24bpp():" + (DateTime.Now.Subtract(start)).ToString()) ;
#endif
		}
		#endregion

		#region Get8bpp()
		private static Bitmap Get8bpp(Bitmap sourceBmp, Rectangle clip, int bThreshold, int wThreshold, bool resultManaged, bool textPreferred)
		{
			Bitmap		resultBmp;
			bool		isGrayscale = Misc.IsGrayscale(sourceBmp);
		
			if(resultManaged)
				resultBmp = new Bitmap(clip.Width * 2, clip.Height * 2, PixelFormat.Format1bppIndexed); 
			else
			{
				unsafe
				{
					int		stride = Misc.GetStride(clip.Width * 2, PixelFormat.Format1bppIndexed);
					byte*	scan0 = (byte*)HeapAlloc(ph, HEAP_ZERO_MEMORY, stride * clip.Height * 2);

					resultBmp = new Bitmap(clip.Width * 2, clip.Height * 2, stride, PixelFormat.Format1bppIndexed, new IntPtr(scan0)); 
				}
			}
			
			BitmapData	sourceData = sourceBmp.LockBits(clip, ImageLockMode.ReadOnly, sourceBmp.PixelFormat); 
			BitmapData	resultData = resultBmp.LockBits(new Rectangle(0, 0, resultBmp.Width, resultBmp.Height), ImageLockMode.WriteOnly, resultBmp.PixelFormat); 

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			if(bThreshold > wThreshold)
				bThreshold = wThreshold;

			byte		gray ;
			float		ratio;

			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride; 
			 
			unsafe
			{
				byte*		pSource = (byte*)sourceData.Scan0.ToPointer(); 
				byte*		pResult = (byte*)resultData.Scan0.ToPointer(); 
				byte*		pCurrent ;

				int			ulcorner;
				int			urcorner;
				int			llcorner;
				int			lrcorner;

				int			height = clip.Height;
				int			width = clip.Width;
				int			clipHeight2 = clip.Height - 1;
				int			clipWidth2 = clip.Width - 1;
				int			x, y;
				int			maxCorner;
				int			maxIndex;
				int			yAdd;
				float		thresholdsDistance = (float) (wThreshold - bThreshold);

				if(thresholdsDistance < 1)
					thresholdsDistance = 1;

				for(y = 1; y < clipHeight2; y++) 
				{ 										
					pCurrent = pSource + (y * sStride) + 1 ;

					for(x = 1; x < clipWidth2; x++) 
					{
						if (isGrayscale)
							gray = *pCurrent;
						else
							gray = (byte)(sourceBmp.Palette.Entries[*pCurrent].B * 0.114F + sourceBmp.Palette.Entries[*pCurrent].G * 0.587F + sourceBmp.Palette.Entries[*pCurrent].R * 0.299F);

						if(gray > wThreshold)
						{ 
							//pResult[y       * 2 * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
							//pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
							pResult[(y        * rStride << 1) + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) << 1));
							pResult[((y << 1) + 1) * rStride  + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) << 1));
						}
						else if(gray > bThreshold)
						{
							ratio = (gray - bThreshold) / (float) thresholdsDistance;
							
							ulcorner = pCurrent[-1] + pCurrent[-sStride] + (pCurrent[-sStride-1] >> 1);
							urcorner = pCurrent[+1] + pCurrent[-sStride] + (pCurrent[-sStride+1] >> 1);
							llcorner = pCurrent[-1] + pCurrent[+sStride] + (pCurrent[+sStride-1] >> 1);
							lrcorner = pCurrent[+1] + pCurrent[+sStride] + (pCurrent[+sStride+1] >> 1);

							if(ratio < .50F)
							{
								maxCorner = ulcorner;
								maxIndex = 0;
								yAdd = 0;

								if(urcorner > maxCorner)
								{
									maxCorner = urcorner;
									maxIndex = 1;
								}
								if(llcorner > maxCorner)
								{
									maxCorner = llcorner;
									maxIndex = 0;
									yAdd = 1;
								}
								if(lrcorner > maxCorner)
								{
									maxIndex = 1;
									yAdd = 1;
								}

								pResult[((y << 1) + yAdd) * rStride + (x >> 2)] |= (byte) (0x80 >> (((x & 0x03) * 2) + maxIndex));
							}
							else if (ratio < .75F)
							{
								pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));
								pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));
							}
							else
							{
								if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
								{
									pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));

									if (urcorner > llcorner || urcorner > lrcorner)
									{
										pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));

										if (llcorner > lrcorner)
											pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));
										else
											pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));
									}
									else
									{
										pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) << 1));
									}
								}
								else
								{
									pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));
									pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) << 1));
								}
							}
						}
							
						pCurrent ++ ;
					}
				}

				//borders
				//TOP
				y = 0;
				pCurrent = pSource;

				for(x = 0; x < width; x++) 
				{
					if (isGrayscale)
						gray = *pCurrent;
					else
						gray = (byte)(sourceBmp.Palette.Entries[*pCurrent].B * 0.114F + sourceBmp.Palette.Entries[*pCurrent].G * 0.587F + sourceBmp.Palette.Entries[*pCurrent].R * 0.299F);

					if(gray > wThreshold)
					{ 
						pResult[y       * 2 * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
					}
					else if(gray > bThreshold)
					{
						ratio = (gray - bThreshold) / (float) thresholdsDistance;

						if (ratio < .50F)
						{
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else if (ratio < .75F)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}
							
					pCurrent ++ ;
				}	

				//LEFT
				x = 0;
				pCurrent = pSource + sStride;

				for(y = 1; y < clipHeight2; y++) 
				{
					if (isGrayscale)
						gray = *pCurrent;
					else
						gray = (byte)(sourceBmp.Palette.Entries[*pCurrent].B * 0.114F + sourceBmp.Palette.Entries[*pCurrent].G * 0.587F + sourceBmp.Palette.Entries[*pCurrent].R * 0.299F);

					if(gray > wThreshold)
					{ 
						pResult[y       * 2 * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
					}
					else if(gray > bThreshold)
					{
						ratio = (gray - bThreshold) / (float) thresholdsDistance;

						if (ratio < .50F)
						{
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else if (ratio < .75F)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}
							
					pCurrent += sStride ;
				}

				//BOTTOM
				y = height - 1; 
				pCurrent = pSource + (y * sStride);

				for(x = 0; x < width; x++) 
				{
					if (isGrayscale)
						gray = *pCurrent;
					else
						gray = (byte)(sourceBmp.Palette.Entries[*pCurrent].B * 0.114F + sourceBmp.Palette.Entries[*pCurrent].G * 0.587F + sourceBmp.Palette.Entries[*pCurrent].R * 0.299F);

					if(gray > wThreshold)
					{ 
						pResult[y       * 2 * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
					}
					else if(gray > bThreshold)
					{
						ratio = (gray - bThreshold) / (float) thresholdsDistance;

						if (ratio < .50F)
						{
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else if (ratio < .75F)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}
							
					pCurrent ++ ;
				}

				//RIGHT
				x = width - 1;
				pCurrent = pSource + sStride + width - 1;

				for(y = 1; y < clipHeight2; y++) 
				{
					if (isGrayscale)
						gray = *pCurrent;
					else
						gray = (byte)(sourceBmp.Palette.Entries[*pCurrent].B * 0.114F + sourceBmp.Palette.Entries[*pCurrent].G * 0.587F + sourceBmp.Palette.Entries[*pCurrent].R * 0.299F);

					if(gray > wThreshold)
					{ 
						pResult[y       * 2 * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
					}
					else if(gray > bThreshold)
					{
						ratio = (gray - bThreshold) / (float) thresholdsDistance;

						if (ratio < .50F)
						{
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else if (ratio < .75F)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}
							
					pCurrent += sStride ;
				}
			}

#if DEBUG
			Console.WriteLine("DRS2 Get8bpp():" + (DateTime.Now.Subtract(start)).ToString()) ;
#endif

			if (textPreferred)
				SqueezePixels8bpp(sourceData, resultData, wThreshold, bThreshold, clip, isGrayscale);
			
			sourceBmp.UnlockBits(sourceData);
			resultBmp.UnlockBits(resultData); 

			return resultBmp; 
		}
		#endregion
		
		#region SqueezePixels8bpp()
		private static void  SqueezePixels8bpp(BitmapData sourceData, BitmapData	resultData, 
			int wThreshold, int bThreshold, Rectangle clip, bool isGrayscale)
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			byte	gray;
			byte	toChangeRange = (byte) (.1F * (wThreshold - bThreshold));

			int		sStride = sourceData.Stride; 
			int		rStride = resultData.Stride; 
			int		x, y;
			 
				unsafe
				{
					byte*		pSourceGray = (byte*)sourceData.Scan0.ToPointer(); 
					byte*		pResultCopy = (byte*)resultData.Scan0.ToPointer();
					byte*		pGrayCurrent ;
					byte*		pResult;

					int			height = resultData.Height - 2;
					int			width = resultData.Width / 8 - 1;
					int			clipX = clip.X;
					int			clipY = clip.Y;
					byte*		lineUp, line1, line2, lineDown;
					int			grayCorner1, grayCorner2, grayCorner3;

					for(y = 2; y < height; y = y + 2)
					{
						for(x = 1; x < width; x++)
						{						
							pGrayCurrent = pSourceGray + (y * sStride / 2) + (x + clipX) * 4;
							pResult = pResultCopy + y * rStride + x;

							lineUp = pResultCopy + (y-1) * rStride + x;
							line1 = lineUp + rStride;
							line2 = line1 + rStride;
							lineDown = line2 + rStride;

							gray = *pGrayCurrent ;
							//pixel1 UL
							if(gray < wThreshold && gray > bThreshold)
							{ 
								if((*line1 & 0x80) == 0)
								{
									grayCorner1 = *(pGrayCurrent - sStride - 1) ;
									grayCorner2 = *(pGrayCurrent - sStride) ;
									grayCorner3 = *(pGrayCurrent - 1) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp - 1) & 0x01) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x80) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(line1 - 1) & 0x01) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x80;
										pResult[-rStride-1] &= 0xFE;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x80;
										pResult[-rStride] &= 0x7F;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x80;
										pResult[-1] &= 0xFE;
									}
								}

								//pixel1 UR
								if((*line1 & 0x40) == 0)
								{
									grayCorner1 = *(pGrayCurrent - sStride) ;
									grayCorner2 = *(pGrayCurrent - sStride + 1) ;
									grayCorner3 = *(pGrayCurrent + 1) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x40) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x20) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x20) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x40;
										pResult[-rStride] &= 0xBF;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x40;
										pResult[-rStride] &= 0xDF;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x40;
										*pResult &= 0xDF;
									}
								}

								//pixel1 LL
								pResult += rStride;

								if((*line2 & 0x80) == 0)
								{
									grayCorner1 = *(pGrayCurrent - 1) ;
									grayCorner2 = *(pGrayCurrent + sStride - 1) ;
									grayCorner3 = *(pGrayCurrent + sStride) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(line2 - 1) & 0x01) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown - 1) & 0x01) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x80) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x80;
										pResult[-1] &= 0xFE;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x80;
										pResult[+rStride-1] &= 0xFE;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x80;
										pResult[+rStride] &= 0x7F;
									}
								}

								//pixel1 LR
								if((*line2 & 0x40) == 0)
								{
									grayCorner1 = *(pGrayCurrent + 1) ;
									grayCorner2 = *(pGrayCurrent + sStride) ;
									grayCorner3 = *(pGrayCurrent + sStride + 1) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x20) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x40) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x20) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x40;
										*pResult &= 0xBF;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x40;
										pResult[rStride] &= 0xBF;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x40;
										pResult[rStride] &= 0xDF;
									}
								}

								pResult -= rStride;
							}				

							//pixel2 UL
							pGrayCurrent++;
							gray = *pGrayCurrent ;

							if(gray < wThreshold && gray > bThreshold)
							{ 
								if((*line1 & 0x20) == 0)
								{
									grayCorner1 = *(pGrayCurrent - sStride - 1) ;
									grayCorner2 = *(pGrayCurrent - sStride) ;
									grayCorner3 = *(pGrayCurrent - 1) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x40) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x20) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x40) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x20;
										pResult[-rStride] &= 0xBF;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x20;
										pResult[-rStride] &= 0xDF;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x20;
										*pResult &= 0xBF;
									}
								}

								//pixel2 UR
								if((*line1 & 0x10) == 0)
								{
									grayCorner1 = *(pGrayCurrent - sStride) ;
									grayCorner2 = *(pGrayCurrent - sStride + 1) ;
									grayCorner3 = *(pGrayCurrent + 1) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x10) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x08) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x08) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x10;
										pResult[-rStride] &= 0xEF;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x10;
										pResult[-rStride] &= 0xF7;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x10;
										*pResult &= 0xF7;
									}
								}
				
								//pixel2 LL
								pResult += rStride;

								if((*line2 & 0x20) == 0)
								{
									grayCorner1 = *(pGrayCurrent - 1) ;
									grayCorner2 = *(pGrayCurrent + sStride - 1) ;
									grayCorner3 = *(pGrayCurrent + sStride) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x40) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x40) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x20) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x20;
										*pResult &= 0xBF;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x20;
										pResult[rStride] &= 0xBF;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x20;
										pResult[rStride] &= 0xDF;
									}
								}

								//pixel2 LR
								if((*line2 & 0x10) == 0)
								{
									grayCorner1 = *(pGrayCurrent + 1) ;
									grayCorner2 = *(pGrayCurrent + sStride) ;
									grayCorner3 = *(pGrayCurrent + sStride + 1) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x08) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x10) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x08) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x10;
										*pResult &= 0xF7;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x10;
										pResult[rStride] &= 0xEF;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x10;
										pResult[rStride] &= 0xF7;
									}
								}
								
								pResult -= rStride;
							}
								
							//pixel3 UL
							pGrayCurrent++;	
							gray = *pGrayCurrent ;

							if(gray < wThreshold && gray > bThreshold)
							{ 
								if((*line1 & 0x08) == 0)
								{
									grayCorner1 = *(pGrayCurrent - sStride - 1) ;
									grayCorner2 = *(pGrayCurrent - sStride) ;
									grayCorner3 = *(pGrayCurrent - 1) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x10) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x08) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x10) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x08;
										pResult[-rStride] &= 0xEF;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x08;
										pResult[-rStride] &= 0xF7;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x08;
										*pResult &= 0xEF;
									}
								}

								//pixel3 UR
								if((*line1 & 0x04) == 0)
								{
									grayCorner1 = *(pGrayCurrent - sStride) ;
									grayCorner2 = *(pGrayCurrent - sStride + 1) ;
									grayCorner3 = *(pGrayCurrent + 1) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x04) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x02) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x02) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x04;
										pResult[-rStride] &= 0xFB;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x04;
										pResult[-rStride] &= 0xFD;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x04;
										*pResult &= 0xFD;
									}
								}

								//pixel3 LL
								pResult += rStride;

								if((*line2 & 0x08) == 0)
								{
									grayCorner1 = *(pGrayCurrent - 1) ;
									grayCorner2 = *(pGrayCurrent + sStride - 1) ;
									grayCorner3 = *(pGrayCurrent + sStride) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x10) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x10) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x08) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x08;
										*pResult &= 0xEF;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x08;
										pResult[rStride] &= 0xEF;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x08;
										pResult[rStride] &= 0xF7;
									}
								}

								//pixel3 LR
								if((*line2 & 0x04) == 0)
								{
									grayCorner1 = *(pGrayCurrent + 1) ;
									grayCorner2 = *(pGrayCurrent + sStride) ;
									grayCorner3 = *(pGrayCurrent + sStride + 1) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x02) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x04) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x02) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x04;
										*pResult &= 0xFD;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x04;
										pResult[rStride] &= 0xFB;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x04;
										pResult[rStride] &= 0xFD;
									}
								}

								pResult -= rStride;
							}
								
							//pixel4 UL
							pGrayCurrent++;	
							gray = *pGrayCurrent ;

							if(gray < wThreshold && gray > bThreshold)
							{ 
								if((*line1 & 0x02) == 0)
								{
									grayCorner1 = *(pGrayCurrent - sStride - 1) ;
									grayCorner2 = *(pGrayCurrent - sStride) ;
									grayCorner3 = *(pGrayCurrent - 1) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x04) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x02) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x04) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x02;
										pResult[-rStride] &= 0xFB;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x02;
										pResult[-rStride] &= 0xFD;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x02;
										*pResult &= 0xFB;
									}
								}

								//pixel4 UR
								if((*line1 & 0x01) == 0)
								{
									grayCorner1 = *(pGrayCurrent - sStride) ;
									grayCorner2 = *(pGrayCurrent - sStride + 1) ;
									grayCorner3 = *(pGrayCurrent + 1) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x01) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp + 1) & 0x80) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(line1 + 1) & 0x80) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x01;
										pResult[-rStride] &= 0xFE;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x01;
										pResult[-rStride+1] &= 0x7F;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x01;
										pResult[1] &= 0x7F;
									}
								}

								//pixel4 LL
								pResult += rStride;

								if((*line2 & 0x02) == 0)
								{
									grayCorner1 = *(pGrayCurrent - 1) ;
									grayCorner2 = *(pGrayCurrent + sStride - 1) ;
									grayCorner3 = *(pGrayCurrent + sStride) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x04) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x04) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x02) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x02;
										*pResult &= 0xFB;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x02;
										pResult[rStride] &= 0xFB;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x02;
										pResult[rStride] &= 0xFD;
									}
								}

								//pixel4 LR
								if((*line2 & 0x01) == 0)
								{
									grayCorner1 = *(pGrayCurrent + 1) ;
									grayCorner2 = *(pGrayCurrent + sStride) ;
									grayCorner3 = *(pGrayCurrent + sStride + 1) ;
									
									if(grayCorner1 < bThreshold || grayCorner1 > gray - toChangeRange || ((*(line2 + 1) & 0x80) == 0) )
										grayCorner1 = int.MaxValue;

									if(grayCorner2 < bThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x01) == 0) )
										grayCorner2 = int.MaxValue;
								
									if(grayCorner3 < bThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown + 1) & 0x80) == 0) )
										grayCorner3 = int.MaxValue;

									if(grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
									{
										*pResult |= 0x01;
										pResult[1] &= 0x7F;
									}
									else if(grayCorner2 < 256 && grayCorner2 < grayCorner3)
									{
										*pResult |= 0x01;
										pResult[rStride] &= 0xFE;
									}
									else if(grayCorner3 < 256)
									{
										*pResult |= 0x01;
										pResult[rStride + 1] &= 0x7F;
									}
								}
							}
						}
					}
				}

#if DEBUG
			Console.WriteLine("DRS2 SqueezePixels8bpp():" + (DateTime.Now.Subtract(start)).ToString()) ;
#endif
		}
		#endregion

		#endregion

	}
}
