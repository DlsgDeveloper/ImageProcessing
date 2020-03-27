using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class DRS2Old
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


		#region Binorize()
		public static Bitmap Binorize(Bitmap source, int bThresholdDelta, int wThresholdDelta, bool textPreferred)
		{
			return Binorize(source, Rectangle.Empty, bThresholdDelta, wThresholdDelta, true, textPreferred);
		}

		public static Bitmap Binorize(Bitmap bmpSource, Rectangle clip, int bThresholdDelta, int wThresholdDelta, bool resultManaged, bool textPreferred)
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
						{
							if (Misc.IsGrayscale(bmpSource))
								bmpResult = Binorize8bppGrayscale(bmpSource, clip, bThresholdDelta, wThresholdDelta, resultManaged, textPreferred);
							else
								bmpResult = Binorize8bpp(bmpSource, clip, bThresholdDelta, wThresholdDelta, resultManaged, textPreferred);
						} break;
					case PixelFormat.Format24bppRgb:
						bmpResult = Binorize24bpp(bmpSource, clip, bThresholdDelta, wThresholdDelta, resultManaged, textPreferred);
						break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}

				if (bmpResult != null)
				{
					bmpResult.SetResolution(bmpSource.HorizontalResolution * 2, bmpSource.VerticalResolution * 2);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("DRS2, Binorize(): " + ex.Message);
			}

			return bmpResult;
		}
		#endregion

		#region Binorize()
		public static bool Binorize(string source, string dest, Rectangle clip, ResultFormat format, int bThresholdDelta, int wThresholdDelta, bool textPreferred)
		{
			try
			{
				FileInfo sourceFile = new FileInfo(source);
				FileInfo destFile = new FileInfo(dest);

				if ((sourceFile.Attributes & FileAttributes.Directory) > 0)
					BinorizeDir(new DirectoryInfo(sourceFile.FullName), new DirectoryInfo(destFile.FullName), clip, format, bThresholdDelta, wThresholdDelta, textPreferred);
				else
					BinorizeFile(sourceFile, destFile, clip, format, bThresholdDelta, wThresholdDelta, textPreferred);

				return true;
			}
			catch
			{
				return false;
			}

		}
		#endregion

		#region BinorizeStream()
		public unsafe static int BinorizeStream(byte** firstByte, int* length, Rectangle clip, int bThresholdDelta,
			int wThresholdDelta, ResultFormat resultFormat, bool textPreferred)
		{
#if DEBUG
			DateTime		enterTime = DateTime.Now ;
#endif
			byte[] array = new byte[*length];
			Bitmap bitmap;


			Marshal.Copy(new IntPtr(*firstByte), array, 0, (int)*length);

			MemoryStream stream = new MemoryStream(array);

			try
			{
				bitmap = new Bitmap(stream);
			}
			catch (Exception ex)
			{
				throw new Exception("Can't generate bitmap.\nException: " + ex);
			}

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			Bitmap result = Binorize(bitmap, clip, bThresholdDelta, wThresholdDelta, true, textPreferred);
#if DEBUG
			TimeSpan	time = DateTime.Now.Subtract(start) ;

			Console.Write(string.Format("DRS2: {0}",  time.ToString())) ;
#endif

			ImageCodecInfo codecInfo = Encoding.GetCodecInfo(resultFormat);
			EncoderParameters encoderParams = Encoding.GetEncoderParams(resultFormat, Encoding.GetColorDepth(result));

			bitmap.Dispose();
			stream.Close();

			MemoryStream resultStream = new MemoryStream();
			try
			{
				result.Save(resultStream, codecInfo, encoderParams);
			}
			catch (Exception ex)
			{
				throw new Exception(string.Format("Can't save bitmap to stream.\nException: {0}\nStream : {1}\n" +
					"Codec Info: {2}\nEncoder: {3}", ex.Message, (resultStream != null) ? "Exists" : "null",
					(codecInfo != null) ? codecInfo.CodecName : "null",
					(encoderParams != null) ? encoderParams.Param[0].ToString() : "null"));
			}

			*length = (int)resultStream.Length;
			*firstByte = (byte*)HeapAlloc(ph, HEAP_ZERO_MEMORY, (int)resultStream.Length);
			Marshal.Copy(resultStream.ToArray(), 0, new IntPtr(*firstByte), (int)resultStream.Length);

			result.Dispose();
			resultStream.Close();
#if DEBUG
			Console.WriteLine(string.Format(" Total Time: {0}",  DateTime.Now.Subtract(enterTime).ToString())) ;
#endif

			return 0;
		}
		#endregion

		#region BinorizeMem()
		public unsafe static int BinorizeMem(ref int width, ref int height, ref int stride, PixelFormat pixelFormat,
			byte** firstByte, ColorPalette palette, Rectangle clip, int bThresholdDelta, int wThresholdDelta, bool textPreferred)
		{
			SecurityPermission sp = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);

			sp.Assert();

			Bitmap bitmap = new Bitmap(width, height, stride, pixelFormat, new IntPtr(*firstByte));

			if (bitmap == null)
				throw new Exception("DRS2(): Can't create bitmap from present parameters!");

			if (palette != null)
				bitmap.Palette = palette;

			Bitmap rBitmap = null;

			if (clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, width, height);
			else if (clip.Width == 0 || clip.Height == 0)
				clip = Rectangle.FromLTRB(clip.X, clip.Y, bitmap.Width - clip.X * 2, bitmap.Height - clip.Y * 2);

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			try
			{
				rBitmap = Binorize(bitmap, clip, bThresholdDelta, wThresholdDelta, false, textPreferred);

				//rBitmap.Save(@"d:\temp\aaaaa.jpg", ImageFormat.Jpeg);

				BitmapData bmpData = rBitmap.LockBits(new Rectangle(0, 0, rBitmap.Width, rBitmap.Height), ImageLockMode.ReadOnly, rBitmap.PixelFormat);

				//int		length = (int) bmpData.Stride * bmpData.Height;
				//*firstByte = (byte*) HeapAlloc(ph, HEAP_ZERO_MEMORY, (int) length);

				//ImageProcessing.ImageCopier.CopyData(bmpData, *firstByte);

				width = bmpData.Width;
				height = bmpData.Height;
				stride = bmpData.Stride;
				*firstByte = (byte*)bmpData.Scan0.ToPointer();

				rBitmap.UnlockBits(bmpData);
				rBitmap.Dispose();
				rBitmap = null;
				bitmap.Dispose();
				return 0;
			}
			catch (Exception ex)
			{
				throw new Exception("DRS2(): " + ex.Message);
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

		//PRIVATE METHODS

		#region BinorizeDir()
		private static void BinorizeDir(DirectoryInfo sourceDir, DirectoryInfo destDir, Rectangle clip,
			ResultFormat format, int bThresholdDelta, int wThresholdDelta, bool textPreferred)
		{
			if (!sourceDir.Exists)
				throw new Exception("Source directory or file doesn't exist!");

			ArrayList sources = new ArrayList();
			Bitmap bitmap;
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

			foreach (FileInfo file in sources)
			{
				bitmap = new Bitmap(file.FullName);
#if DEBUG
				DateTime	start = DateTime.Now ;
#endif
				Bitmap result = Binorize(bitmap, clip, bThresholdDelta, wThresholdDelta, true, textPreferred);

#if DEBUG
				TimeSpan	time = DateTime.Now.Subtract(start) ;
				span = span.Add(time);
				Console.WriteLine(string.Format("{0}: {1}",  file.FullName, time.ToString())) ;
#endif

				ImageCodecInfo codecInfo = Encoding.GetCodecInfo(format);
				EncoderParameters encoderParams = Encoding.GetEncoderParams(format, Encoding.GetColorDepth(result));

				bitmap.Dispose();

				if (File.Exists(destDir.FullName + @"\" + file.Name))
					File.Delete(destDir.FullName + @"\" + file.Name);

				result.Save(destDir.FullName + @"\" + file.Name, codecInfo, encoderParams);
				result.Dispose();
			}

#if DEBUG
			Console.WriteLine("Total time: " + span.ToString());
			Console.WriteLine("Total all time: " + DateTime.Now.Subtract(totalTimeStart).ToString());
#endif
		}
		#endregion

		#region BinorizeFile()
		private static void BinorizeFile(FileInfo sourceFile, FileInfo resultFile, Rectangle clip,
			ResultFormat format, int bThresholdDelta, int wThresholdDelta, bool textPreferred)
		{
			Bitmap bitmap = new Bitmap(sourceFile.FullName);

#if DEBUG
			DateTime	start = DateTime.Now;
#endif

			Bitmap result = Binorize(bitmap, clip, bThresholdDelta, wThresholdDelta, true, textPreferred);

			ImageCodecInfo codecInfo = Encoding.GetCodecInfo(format);
			EncoderParameters encoderParams = Encoding.GetEncoderParams(format, Encoding.GetColorDepth(result));

			bitmap.Dispose();

			if (resultFile.Exists)
				resultFile.Delete();

			result.Save(resultFile.FullName, codecInfo, encoderParams);

			result.Dispose();

#if DEBUG
			TimeSpan	time = DateTime.Now.Subtract(start) ;
			Console.WriteLine(string.Format("{0}: {1}",  sourceFile.FullName, time.ToString())) ;
#endif
		}
		#endregion

		#region Binorize24bpp()
		//it creates grayscale histogram and it renders pixels in between histogram peaks. 
		private static Bitmap Binorize24bpp(Bitmap sourceBmp, Rectangle clip, int bThresholdDelta, int wThresholdDelta, bool resultManaged, bool textPreferred)
		{
			Bitmap resultBmp;
			int width = clip.Width;
			int height = clip.Height;

			if (resultManaged)
				resultBmp = new Bitmap(width * 2, height * 2, PixelFormat.Format1bppIndexed);
			else
			{
				unsafe
				{
					Bitmap bmp1Bpp = new Bitmap(clip.Width * 2, 1, PixelFormat.Format1bppIndexed);
					BitmapData bmp1BppData = bmp1Bpp.LockBits(new Rectangle(0, 0, bmp1Bpp.Width, bmp1Bpp.Height), ImageLockMode.ReadOnly, bmp1Bpp.PixelFormat);
					byte* scan0 = (byte*)HeapAlloc(ph, HEAP_ZERO_MEMORY, bmp1BppData.Stride * clip.Height * 2);

					resultBmp = new Bitmap(clip.Width * 2, clip.Height * 2, bmp1BppData.Stride, PixelFormat.Format1bppIndexed, new IntPtr(scan0));

					bmp1Bpp.UnlockBits(bmp1BppData);
					bmp1Bpp.Dispose();
				}
			}

			Bitmap histogramBitmap;

			if (clip.Width < 800 || clip.Height < 800)
				histogramBitmap = Interpolation.Interpolate24bppTo8bpp(sourceBmp);
			else
				histogramBitmap = Interpolation.Interpolate24bppTo8bpp2to1(sourceBmp);

			Histogram histogram = new Histogram(histogramBitmap);

			//byte			wThreshold = (byte) (Math.Max(10, Math.Min(255, Math.Max(histogram.Extreme, histogram.SecondExtreme) + wThresholdDelta)));
			byte wThreshold = (byte)(Math.Max(10, Math.Min(255, Math.Max(histogram.Threshold, histogram.SecondExtreme) + wThresholdDelta)));
			byte bThreshold = (byte)(Math.Max(10, Math.Min(255, Math.Min(histogram.Extreme, histogram.SecondExtreme) + bThresholdDelta)));

			BitmapData sourceData = sourceBmp.LockBits(clip, ImageLockMode.ReadOnly, sourceBmp.PixelFormat);
			BitmapData resultData = resultBmp.LockBits(new Rectangle(0, 0, resultBmp.Width, resultBmp.Height), ImageLockMode.WriteOnly, resultBmp.PixelFormat);

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif


			//histogram.Show() ;
			byte gray;
			float ratio;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

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

							ulcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + ((pCurrent[-sStride - 3] + pCurrent[-sStride - 2] + pCurrent[-sStride - 1]) >> 1);
							urcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + ((pCurrent[-sStride + 3] + pCurrent[-sStride + 4] + pCurrent[-sStride + 5]) >> 1);
							llcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + ((pCurrent[+sStride - 3] + pCurrent[+sStride - 2] + pCurrent[+sStride - 1]) >> 1);
							lrcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + ((pCurrent[+sStride + 3] + pCurrent[+sStride + 4] + pCurrent[+sStride + 5]) >> 1);

							if (ratio < .33F)
							{
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
							else if (ratio < .66F)
							{
								if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
									pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));
								if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
									pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));
								if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
									pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));
								if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
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

						if (x == 0)
						{
							ulcorner = (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
							urcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
							llcorner = (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) / 2;
							lrcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + (pCurrent[+sStride + 3] + pCurrent[+sStride + 4] + pCurrent[+sStride + 5]) / 2;
						}
						else if (x == width - 1)
						{
							ulcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
							urcorner = (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
							llcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + (pCurrent[+sStride - 3] + pCurrent[+sStride - 2] + pCurrent[+sStride - 1]) / 2;
							lrcorner = (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + (pCurrent[+sStride + 0] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) / 2;
						}
						else
						{
							ulcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
							urcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
							llcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + (pCurrent[+sStride - 3] + pCurrent[+sStride - 2] + pCurrent[+sStride - 1]) / 2;
							lrcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + (pCurrent[+sStride + 3] + pCurrent[+sStride + 4] + pCurrent[+sStride + 5]) / 2;
						}

						if (ratio < .33F)
						{
							maxCorner = ulcorner;
							maxIndex = 0;
							yAdd = 0;

							if (urcorner > maxCorner)
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

							pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
						}
						else if (ratio < .66F)
						{
							if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

								if (urcorner > llcorner || urcorner > lrcorner)
								{
									pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

									if (llcorner > lrcorner)
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
									else
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								}
								else
								{
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
								}
							}
							else
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
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

						ulcorner = (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
						urcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + (pCurrent[-sStride + 3] + pCurrent[-sStride + 4] + pCurrent[-sStride + 5]) / 2;
						llcorner = (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
						lrcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + (pCurrent[+sStride + 3] + pCurrent[+sStride + 4] + pCurrent[+sStride + 5]) / 2;

						if (ratio < .33F)
						{
							maxCorner = ulcorner;
							maxIndex = 0;
							yAdd = 0;

							if (urcorner > maxCorner)
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

							pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
						}
						else if (ratio < .66F)
						{
							if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

								if (urcorner > llcorner || urcorner > lrcorner)
								{
									pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

									if (llcorner > lrcorner)
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
									else
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								}
								else
								{
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
								}
							}
							else
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
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

						if (x == 0)
						{
							ulcorner = (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + (pCurrent[-sStride + 0] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) / 2;
							urcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + (pCurrent[-sStride + 3] + pCurrent[-sStride + 4] + pCurrent[-sStride + 5]) / 2;
							llcorner = (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
							lrcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
						}
						else if (x == width - 1)
						{
							ulcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + (pCurrent[-sStride - 3] + pCurrent[-sStride - 2] + pCurrent[-sStride - 1]) / 2;
							urcorner = (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + (pCurrent[-sStride + 0] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) / 2;
							llcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
							lrcorner = (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
						}
						else
						{
							ulcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + (pCurrent[-sStride - 3] + pCurrent[-sStride - 2] + pCurrent[-sStride - 1]) / 2;
							urcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + (pCurrent[-sStride + 3] + pCurrent[-sStride + 4] + pCurrent[-sStride + 5]) / 2;
							llcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
							lrcorner = (pCurrent[3] + pCurrent[4] + pCurrent[5]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
						}

						if (ratio < .33F)
						{
							maxCorner = ulcorner;
							maxIndex = 0;
							yAdd = 0;

							if (urcorner > maxCorner)
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

							pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
						}
						else if (ratio < .66F)
						{
							if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

								if (urcorner > llcorner || urcorner > lrcorner)
								{
									pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

									if (llcorner > lrcorner)
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
									else
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								}
								else
								{
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
								}
							}
							else
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
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

						ulcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + (pCurrent[-sStride - 3] + pCurrent[-sStride - 2] + pCurrent[-sStride - 1]) / 2;
						urcorner = (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;
						llcorner = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + (pCurrent[+sStride - 3] + pCurrent[+sStride - 2] + pCurrent[+sStride - 1]) / 2;
						lrcorner = (pCurrent[0] + pCurrent[1] + pCurrent[2]) + (pCurrent[+sStride] + pCurrent[+sStride + 1] + pCurrent[+sStride + 2]) + (pCurrent[0] + pCurrent[1] + pCurrent[2]) / 2;

						if (ratio < .33F)
						{
							maxCorner = ulcorner;
							maxIndex = 0;
							yAdd = 0;

							if (urcorner > maxCorner)
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

							pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
						}
						else if (ratio < .66F)
						{
							if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

								if (urcorner > llcorner || urcorner > lrcorner)
								{
									pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

									if (llcorner > lrcorner)
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
									else
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								}
								else
								{
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
								}
							}
							else
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
					}

					pCurrent += sStride;
				}
			}

#if DEBUG
			Console.WriteLine("DRS2 Binorize24bpp():" + (DateTime.Now.Subtract(start)).ToString()) ;
#endif

			//if (textPreferred)
			//SqueezePixels24bpp(sourceData, resultData, wThreshold, bThreshold);

			sourceBmp.UnlockBits(sourceData);
			resultBmp.UnlockBits(resultData);

			resultBmp.SetResolution(sourceBmp.HorizontalResolution, sourceBmp.VerticalResolution);

			if (textPreferred)
				HighlightText(sourceBmp, resultBmp);

			return resultBmp;
		}
		#endregion

		#region HighlightText()
		private static void HighlightText(Bitmap original, Bitmap bitmap)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			NoiseReduction.Despeckle(bitmap);
			Bitmap result1 = Erosion.Get(bitmap, Rectangle.Empty, Erosion.Operator.Full);
			Inverter.Invert(result1);
			ImagePreprocessing.GetRidOfBorders(result1);
			PageObjects.Symbols symbols = PageObjects.ObjectLocator.FindObjects(result1, Rectangle.Empty, 1);
			result1.Dispose();

			PageObjects.ObjectLocator.ExtractPictures(symbols);


			foreach (PageObjects.Symbol symbol in symbols)
				if (symbol.IsLetter || symbol.IsPunctuation || symbol.ObjectType == ImageProcessing.PageObjects.Symbol.Type.NotSure)
					Erosion.Go(bitmap, symbol.Rectangle, Erosion.Operator.Full);

#if DEBUG
			Console.WriteLine("DRS2 PrefferText():" + (DateTime.Now.Subtract(start)).ToString());
#endif
		}
		#endregion

		#region SqueezePixels24bpp()
		private static void SqueezePixels24bpp(BitmapData sourceData, BitmapData resultData, byte wThreshold, short bThreshold)
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			byte toChangeRange = (byte)(.1F * ((wThreshold - bThreshold) / 3));

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;
			int x, y;

			unsafe
			{
				byte* pSourceGray = (byte*)sourceData.Scan0.ToPointer();
				byte* pResultCopy = (byte*)resultData.Scan0.ToPointer();
				byte* pCurrent;
				byte* pResult;

				int height = resultData.Height - 2;
				int width = resultData.Width / 8 - 1;
				byte* lineUp, line1, line2, lineDown;
				int gray, corner1, corner2, corner3;

				for (y = 2; y < height; y = y + 2)
				{
					for (x = 1; x < width; x++)
					{
						pCurrent = pSourceGray + (y * sStride / 2) + x * 4 * 3;
						pResult = pResultCopy + y * rStride + x;

						lineUp = pResultCopy + (y - 1) * rStride + x;
						line1 = lineUp + rStride;
						line2 = line1 + rStride;
						lineDown = line2 + rStride;

						gray = *pCurrent + pCurrent[1] + pCurrent[2];
						//pixel1 UL
						if (gray < wThreshold && gray >= bThreshold)
						{
							if ((*line1 & 0x80) == 0)
							{
								corner1 = pCurrent[-sStride - 3] + pCurrent[-sStride - 2] + pCurrent[-sStride - 1];
								corner2 = pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2];
								corner3 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp - 1) & 0x01) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x80) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1 - 1) & 0x01) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x80;
									pResult[-rStride - 1] &= 0xFE;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x80;
									pResult[-rStride] &= 0x7F;
								}
								else if (corner3 < 768)
								{
									*pResult |= 0x80;
									pResult[-1] &= 0xFE;
								}
							}

							//pixel1 UR
							if ((*line1 & 0x40) == 0)
							{
								corner1 = pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2];
								corner2 = pCurrent[-sStride + 3] + pCurrent[-sStride + 4] + pCurrent[-sStride + 5];
								corner3 = pCurrent[3] + pCurrent[4] + pCurrent[5];

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x40) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x20) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1) & 0x20) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x40;
									pResult[-rStride] &= 0xBF;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x40;
									pResult[-rStride] &= 0xDF;
								}
								else if (corner3 < 768)
								{
									*pResult |= 0x40;
									*pResult &= 0xDF;
								}
							}

							//pixel1 LL
							pResult += rStride;

							if ((*line2 & 0x80) == 0)
							{
								corner1 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);
								corner2 = pCurrent[sStride - 3] + pCurrent[sStride - 2] + pCurrent[sStride - 1];
								corner3 = pCurrent[sStride] + pCurrent[sStride + 1] + pCurrent[sStride + 2];

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2 - 1) & 0x01) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown - 1) & 0x01) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x80) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x80;
									pResult[-1] &= 0xFE;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x80;
									pResult[+rStride - 1] &= 0xFE;
								}
								else if (corner3 < 768)
								{
									*pResult |= 0x80;
									pResult[+rStride] &= 0x7F;
								}
							}

							//pixel1 LR
							if ((*line2 & 0x40) == 0)
							{
								corner1 = pCurrent[3] + pCurrent[4] + pCurrent[5];
								corner2 = pCurrent[sStride] + pCurrent[sStride + 1] + pCurrent[sStride + 2];
								corner3 = pCurrent[sStride + 3] + pCurrent[sStride + 4] + pCurrent[sStride + 5];

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2) & 0x20) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x40) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x20) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x40;
									*pResult &= 0xBF;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x40;
									pResult[rStride] &= 0xBF;
								}
								else if (corner3 < 768)
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

						if (gray < wThreshold && gray >= bThreshold)
						{
							if ((*line1 & 0x20) == 0)
							{
								corner1 = (pCurrent[-sStride - 3] + pCurrent[-sStride - 2] + pCurrent[-sStride - 1]);
								corner2 = pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2];
								corner3 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x40) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x20) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1) & 0x40) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x20;
									pResult[-rStride] &= 0xBF;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x20;
									pResult[-rStride] &= 0xDF;
								}
								else if (corner3 < 768)
								{
									*pResult |= 0x20;
									*pResult &= 0xBF;
								}
							}

							//pixel2 UR
							if ((*line1 & 0x10) == 0)
							{
								corner1 = pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2];
								corner2 = pCurrent[-sStride + 3] + pCurrent[-sStride + 4] + pCurrent[-sStride + 5];
								corner3 = pCurrent[3] + pCurrent[4] + pCurrent[5];

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x10) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x08) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1) & 0x08) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x10;
									pResult[-rStride] &= 0xEF;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x10;
									pResult[-rStride] &= 0xF7;
								}
								else if (corner3 < 768)
								{
									*pResult |= 0x10;
									*pResult &= 0xF7;
								}
							}

							//pixel2 LL
							pResult += rStride;

							if ((*line2 & 0x20) == 0)
							{
								corner1 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);
								corner2 = pCurrent[sStride - 3] + pCurrent[sStride - 2] + pCurrent[sStride - 1];
								corner3 = pCurrent[sStride] + pCurrent[sStride + 1] + pCurrent[sStride + 2];

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2) & 0x40) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x40) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x20) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x20;
									*pResult &= 0xBF;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x20;
									pResult[rStride] &= 0xBF;
								}
								else if (corner3 < 768)
								{
									*pResult |= 0x20;
									pResult[rStride] &= 0xDF;
								}
							}

							//pixel2 LR
							if ((*line2 & 0x10) == 0)
							{
								corner1 = pCurrent[3] + pCurrent[4] + pCurrent[5];
								corner2 = pCurrent[sStride] + pCurrent[sStride + 1] + pCurrent[sStride + 2];
								corner3 = pCurrent[sStride + 3] + pCurrent[sStride + 4] + pCurrent[sStride + 5];

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2) & 0x08) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x10) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x08) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x10;
									*pResult &= 0xF7;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x10;
									pResult[rStride] &= 0xEF;
								}
								else if (corner3 < 768)
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

						if (gray < wThreshold && gray >= bThreshold)
						{
							if ((*line1 & 0x08) == 0)
							{
								corner1 = (pCurrent[-sStride - 3] + pCurrent[-sStride - 2] + pCurrent[-sStride - 1]);
								corner2 = pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2];
								corner3 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x10) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x08) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1) & 0x10) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x08;
									pResult[-rStride] &= 0xEF;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x08;
									pResult[-rStride] &= 0xF7;
								}
								else if (corner3 < 768)
								{
									*pResult |= 0x08;
									*pResult &= 0xEF;
								}
							}

							//pixel3 UR
							if ((*line1 & 0x04) == 0)
							{
								corner1 = pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2];
								corner2 = pCurrent[-sStride + 3] + pCurrent[-sStride + 4] + pCurrent[-sStride + 5];
								corner3 = pCurrent[3] + pCurrent[4] + pCurrent[5];

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x04) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x02) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1) & 0x02) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x04;
									pResult[-rStride] &= 0xFB;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x04;
									pResult[-rStride] &= 0xFD;
								}
								else if (corner3 < 768)
								{
									*pResult |= 0x04;
									*pResult &= 0xFD;
								}
							}

							//pixel3 LL
							pResult += rStride;

							if ((*line2 & 0x08) == 0)
							{
								corner1 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);
								corner2 = pCurrent[sStride - 3] + pCurrent[sStride - 2] + pCurrent[sStride - 1];
								corner3 = pCurrent[sStride] + pCurrent[sStride + 1] + pCurrent[sStride + 2];

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2) & 0x10) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x10) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x08) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x08;
									*pResult &= 0xEF;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x08;
									pResult[rStride] &= 0xEF;
								}
								else if (corner3 < 768)
								{
									*pResult |= 0x08;
									pResult[rStride] &= 0xF7;
								}
							}

							//pixel3 LR
							if ((*line2 & 0x04) == 0)
							{
								corner1 = pCurrent[3] + pCurrent[4] + pCurrent[5];
								corner2 = pCurrent[sStride] + pCurrent[sStride + 1] + pCurrent[sStride + 2];
								corner3 = pCurrent[sStride + 3] + pCurrent[sStride + 4] + pCurrent[sStride + 5];

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2) & 0x02) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x04) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x02) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x04;
									*pResult &= 0xFD;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x04;
									pResult[rStride] &= 0xFB;
								}
								else if (corner3 < 768)
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

						if (gray < wThreshold && gray >= bThreshold)
						{
							if ((*line1 & 0x02) == 0)
							{
								corner1 = (pCurrent[-sStride - 3] + pCurrent[-sStride - 2] + pCurrent[-sStride - 1]);
								corner2 = pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2];
								corner3 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x04) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp) & 0x02) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1) & 0x04) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x02;
									pResult[-rStride] &= 0xFB;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x02;
									pResult[-rStride] &= 0xFD;
								}
								else if (corner3 < 768)
								{
									*pResult |= 0x02;
									*pResult &= 0xFB;
								}
							}

							//pixel4 UR
							if ((*line1 & 0x01) == 0)
							{
								corner1 = pCurrent[-sStride] + pCurrent[-sStride + 1] + pCurrent[-sStride + 2];
								corner2 = pCurrent[-sStride + 3] + pCurrent[-sStride + 4] + pCurrent[-sStride + 5];
								corner3 = pCurrent[3] + pCurrent[4] + pCurrent[5];

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(lineUp) & 0x01) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineUp + 1) & 0x80) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(line1 + 1) & 0x80) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x01;
									pResult[-rStride] &= 0xFE;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x01;
									pResult[-rStride + 1] &= 0x7F;
								}
								else if (corner3 < 768)
								{
									*pResult |= 0x01;
									pResult[1] &= 0x7F;
								}
							}

							//pixel4 LL
							pResult += rStride;

							if ((*line2 & 0x02) == 0)
							{
								corner1 = (pCurrent[-3] + pCurrent[-2] + pCurrent[-1]);
								corner2 = pCurrent[sStride - 3] + pCurrent[sStride - 2] + pCurrent[sStride - 1];
								corner3 = pCurrent[sStride] + pCurrent[sStride + 1] + pCurrent[sStride + 2];

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2) & 0x04) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x04) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown) & 0x02) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x02;
									*pResult &= 0xFB;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x02;
									pResult[rStride] &= 0xFB;
								}
								else if (corner3 < 768)
								{
									*pResult |= 0x02;
									pResult[rStride] &= 0xFD;
								}
							}

							//pixel4 LR
							if ((*line2 & 0x01) == 0)
							{
								corner1 = pCurrent[3] + pCurrent[4] + pCurrent[5];
								corner2 = pCurrent[sStride] + pCurrent[sStride + 1] + pCurrent[sStride + 2];
								corner3 = pCurrent[sStride + 3] + pCurrent[sStride + 4] + pCurrent[sStride + 5];

								if (corner1 < bThreshold || corner1 > gray - toChangeRange || ((*(line2 + 1) & 0x80) == 0))
									corner1 = int.MaxValue;

								if (corner2 < bThreshold || corner2 > gray - toChangeRange || ((*(lineDown) & 0x01) == 0))
									corner2 = int.MaxValue;

								if (corner3 < bThreshold || corner3 > gray - toChangeRange || ((*(lineDown + 1) & 0x80) == 0))
									corner3 = int.MaxValue;

								if (corner1 < 768 && corner1 < corner2 && corner1 < corner3)
								{
									*pResult |= 0x01;
									pResult[1] &= 0x7F;
								}
								else if (corner2 < 768 && corner2 < corner3)
								{
									*pResult |= 0x01;
									pResult[rStride] &= 0xFE;
								}
								else if (corner3 < 768)
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

		#region Binorize8bpp()
		private static Bitmap Binorize8bpp(Bitmap sourceBmp, Rectangle clip, int bThresholdDelta, int wThresholdDelta, bool resultManaged, bool textPreferred)
		{
			Bitmap resultBmp;

			if (resultManaged)
				resultBmp = new Bitmap(clip.Width * 2, clip.Height * 2, PixelFormat.Format1bppIndexed);
			else
			{
				unsafe
				{
					Bitmap bmp1Bpp = new Bitmap(clip.Width * 2, 1, PixelFormat.Format1bppIndexed);
					BitmapData bmp1BppData = bmp1Bpp.LockBits(new Rectangle(0, 0, bmp1Bpp.Width, bmp1Bpp.Height), ImageLockMode.ReadOnly, bmp1Bpp.PixelFormat);
					byte* scan0 = (byte*)HeapAlloc(ph, HEAP_ZERO_MEMORY, bmp1BppData.Stride * clip.Height * 2);

					resultBmp = new Bitmap(clip.Width * 2, clip.Height * 2, bmp1BppData.Stride, PixelFormat.Format1bppIndexed, new IntPtr(scan0));

					bmp1Bpp.UnlockBits(bmp1BppData);
					bmp1Bpp.Dispose();
				}
			}

			BitmapData sourceData = sourceBmp.LockBits(clip, ImageLockMode.ReadOnly, sourceBmp.PixelFormat);
			BitmapData resultData = resultBmp.LockBits(new Rectangle(0, 0, resultBmp.Width, resultBmp.Height), ImageLockMode.WriteOnly, resultBmp.PixelFormat);
			Color[] colorPalette = sourceBmp.Palette.Entries;
			int[] palette = new int[colorPalette.Length];

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			Histogram histogram;

			if (clip.Width < 800 || clip.Height < 800)
				histogram = new Histogram(sourceData, colorPalette, clip);
			else
				histogram = new Histogram(sourceData, colorPalette, Rectangle.Inflate(clip, -100, -100));

			//Histogram	histogram = new Histogram(sourceData, colorPalette, Rectangle.Inflate(clip, -100, -100)) ;
			int whiteThreshold = (int)(Math.Max(histogram.Extreme, histogram.SecondExtreme) + wThresholdDelta) * 3;
			int blackThreshold = (int)(Math.Min(histogram.Extreme, histogram.SecondExtreme) + bThresholdDelta) * 3;

			if (blackThreshold > whiteThreshold)
				blackThreshold = whiteThreshold;

			//histogram.Show() ;
			int gray;
			float ratio;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			for (int i = 0; i < palette.Length; i++)
				palette[i] = colorPalette[i].R + colorPalette[i].G + colorPalette[i].B;

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
				int clipX = clip.X;
				int clipY = clip.Y;
				int x, y;
				int maxCorner;
				int maxIndex;
				int yAdd;
				float thresholdsDistance = (float)(whiteThreshold - blackThreshold);

				if (thresholdsDistance < 1)
					thresholdsDistance = 1;

				for (y = 1; y < clipHeight2; y++)
				{
					pCurrent = pSource + ((y + clipY) * sStride) + clipX + 1;

					for (x = 1; x < clipWidth2; x++)
					{
						gray = palette[*pCurrent];

						if (gray > whiteThreshold)
						{
							//pResult[y       * 2 * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
							//pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
							pResult[(y * rStride << 1) + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) << 1));
							pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) << 1));
						}
						else if (gray > blackThreshold)
						{
							ratio = (gray - blackThreshold) / (float)thresholdsDistance;

							ulcorner = palette[pCurrent[-1]] + palette[pCurrent[-sStride]] + (palette[pCurrent[-sStride - 1]] >> 1);
							urcorner = palette[pCurrent[+1]] + palette[pCurrent[-sStride]] + (palette[pCurrent[-sStride + 1]] >> 1);
							llcorner = palette[pCurrent[-1]] + palette[pCurrent[+sStride]] + (palette[pCurrent[+sStride - 1]] >> 1);
							lrcorner = palette[pCurrent[+1]] + palette[pCurrent[+sStride]] + (palette[pCurrent[+sStride + 1]] >> 1);

							if (ratio < .33F)
							{
								maxCorner = ulcorner;
								maxIndex = 0;
								yAdd = 0;

								if (urcorner > maxCorner)
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
							else if (ratio < .66F)
							{
								if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
									pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));
								if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
									pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));
								if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
									pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));
								if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
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

						pCurrent++;
					}
				}

				//	CORNERS
				//UL CORNER
				x = 0;
				y = 0;
				pCurrent = pSource + ((y + clipY) * sStride) + clipX;
				gray = palette[*pCurrent];

				if (gray > whiteThreshold)
				{
					pResult[0] |= (byte)0xC0;
					pResult[rStride] |= (byte)0xC0;
				}
				else if (gray > blackThreshold)
				{
					ratio = (gray - blackThreshold) / (float)thresholdsDistance;

					ulcorner = palette[pCurrent[0]] + palette[pCurrent[0]] + palette[pCurrent[0]] / 2;
					urcorner = palette[pCurrent[+1]] + palette[pCurrent[0]] + palette[pCurrent[0]] / 2;
					llcorner = palette[pCurrent[0]] + palette[pCurrent[+sStride]] + palette[pCurrent[0]] / 2;
					lrcorner = palette[pCurrent[+1]] + palette[pCurrent[+sStride]] + palette[pCurrent[+sStride + 1]] / 2;

					if (ratio < .33F)
					{
						maxCorner = ulcorner;
						maxIndex = 0;
						yAdd = 0;

						if (urcorner > maxCorner)
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

						pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
					}
					else if (ratio < .66F)
					{
						if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
					}
					else
					{
						if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

							if (urcorner > llcorner || urcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

								if (llcorner > lrcorner)
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
								else
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							}
							else
							{
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}
				}

				//UR CORNER
				x = clip.Width - 1;
				y = 0;
				pCurrent = pSource + ((y + clipY) * sStride) + clip.Right - 1;
				gray = palette[*pCurrent];

				if (gray > whiteThreshold)
				{
					pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
				}
				else if (gray > blackThreshold)
				{
					ratio = (gray - blackThreshold) / (float)thresholdsDistance;

					ulcorner = palette[pCurrent[-1]] + palette[pCurrent[0]] + palette[pCurrent[0]] / 2;
					urcorner = palette[pCurrent[0]] + palette[pCurrent[0]] + palette[pCurrent[0]] / 2;
					llcorner = palette[pCurrent[-1]] + palette[pCurrent[+sStride]] + palette[pCurrent[+sStride - 1]] / 2;
					lrcorner = palette[pCurrent[0]] + palette[pCurrent[+sStride]] + palette[pCurrent[0]] / 2;

					if (ratio < .33F)
					{
						maxCorner = ulcorner;
						maxIndex = 0;
						yAdd = 0;

						if (urcorner > maxCorner)
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

						pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
					}
					else if (ratio < .66F)
					{
						if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
					}
					else
					{
						if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

							if (urcorner > llcorner || urcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

								if (llcorner > lrcorner)
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
								else
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							}
							else
							{
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}
				}

				//LL CORNER
				x = 0;
				y = clip.Height - 1;
				pCurrent = pSource + ((y + clipY) * sStride) + clipX;
				gray = palette[*pCurrent];

				if (gray > whiteThreshold)
				{
					pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
				}
				else if (gray > blackThreshold)
				{
					ratio = (gray - blackThreshold) / (float)thresholdsDistance;

					ulcorner = palette[pCurrent[0]] + palette[pCurrent[-sStride]] + palette[pCurrent[0]] / 2;
					urcorner = palette[pCurrent[+1]] + palette[pCurrent[-sStride]] + palette[pCurrent[-sStride + 1]] / 2;
					llcorner = palette[pCurrent[0]] + palette[pCurrent[0]] + palette[pCurrent[0]] / 2;
					lrcorner = palette[pCurrent[+1]] + palette[pCurrent[0]] + palette[pCurrent[0]] / 2;

					if (ratio < .33F)
					{
						maxCorner = ulcorner;
						maxIndex = 0;
						yAdd = 0;

						if (urcorner > maxCorner)
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

						pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
					}
					else if (ratio < .66F)
					{
						if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
					}
					else
					{
						if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

							if (urcorner > llcorner || urcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

								if (llcorner > lrcorner)
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
								else
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							}
							else
							{
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}
				}

				//LR CORNER
				x = clip.Width - 1;
				y = clip.Height - 1;
				pCurrent = pSource + ((y + clipY) * sStride) + clip.Right - 1;
				gray = palette[*pCurrent];

				if (gray > whiteThreshold)
				{
					pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
				}
				else if (gray > blackThreshold)
				{
					ratio = (gray - blackThreshold) / (float)thresholdsDistance;

					ulcorner = palette[pCurrent[-1]] + palette[pCurrent[-sStride]] + palette[pCurrent[-sStride - 1]] / 2;
					urcorner = palette[pCurrent[0]] + palette[pCurrent[-sStride]] + palette[pCurrent[0]] / 2;
					llcorner = palette[pCurrent[-1]] + palette[pCurrent[0]] + palette[pCurrent[0]] / 2;
					lrcorner = palette[pCurrent[0]] + palette[pCurrent[0]] + palette[pCurrent[0]] / 2;

					if (ratio < .33F)
					{
						maxCorner = ulcorner;
						maxIndex = 0;
						yAdd = 0;

						if (urcorner > maxCorner)
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

						pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
					}
					else if (ratio < .66F)
					{
						if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
					}
					else
					{
						if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

							if (urcorner > llcorner || urcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

								if (llcorner > lrcorner)
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
								else
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							}
							else
							{
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}
				}


				//borders
				//TOP
				y = 0;
				pCurrent = pSource + ((y + clipY) * sStride) + clipX + 1;

				for (x = 1; x < clipWidth2; x++)
				{
					gray = palette[*pCurrent];

					if (gray > whiteThreshold)
					{
						pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					}
					else if (gray > blackThreshold)
					{
						ratio = (gray - blackThreshold) / (float)thresholdsDistance;

						ulcorner = palette[pCurrent[-1]] + palette[pCurrent[0]] + palette[pCurrent[0]] / 2;
						urcorner = palette[pCurrent[+1]] + palette[pCurrent[0]] + palette[pCurrent[0]] / 2;
						llcorner = palette[pCurrent[-1]] + palette[pCurrent[+sStride]] + palette[pCurrent[+sStride - 1]] / 2;
						lrcorner = palette[pCurrent[+1]] + palette[pCurrent[+sStride]] + palette[pCurrent[+sStride + 1]] / 2;

						if (ratio < .33F)
						{
							maxCorner = ulcorner;
							maxIndex = 0;
							yAdd = 0;

							if (urcorner > maxCorner)
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

							pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
						}
						else if (ratio < .66F)
						{
							if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

								if (urcorner > llcorner || urcorner > lrcorner)
								{
									pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

									if (llcorner > lrcorner)
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
									else
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								}
								else
								{
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
								}
							}
							else
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
					}

					pCurrent++;
				}

				//LEFT
				x = 0;
				pCurrent = pSource + ((1 + clipY) * sStride) + clipX;

				for (y = 1; y < clipHeight2; y++)
				{
					gray = palette[*pCurrent];

					if (gray > whiteThreshold)
					{
						pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					}
					else if (gray > blackThreshold)
					{
						ratio = (gray - blackThreshold) / (float)thresholdsDistance;

						ulcorner = palette[pCurrent[0]] + palette[pCurrent[-sStride]] + palette[pCurrent[0]] / 2;
						urcorner = palette[pCurrent[+1]] + palette[pCurrent[-sStride]] + palette[pCurrent[-sStride + 1]] / 2;
						llcorner = palette[pCurrent[0]] + palette[pCurrent[+sStride]] + palette[pCurrent[0]] / 2;
						lrcorner = palette[pCurrent[+1]] + palette[pCurrent[+sStride]] + palette[pCurrent[+sStride + 1]] / 2;

						if (ratio < .33F)
						{
							maxCorner = ulcorner;
							maxIndex = 0;
							yAdd = 0;

							if (urcorner > maxCorner)
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

							pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
						}
						else if (ratio < .66F)
						{
							if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

								if (urcorner > llcorner || urcorner > lrcorner)
								{
									pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

									if (llcorner > lrcorner)
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
									else
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								}
								else
								{
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
								}
							}
							else
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
					}

					pCurrent += sStride;
				}

				//BOTTOM
				y = clip.Height - 1;
				pCurrent = pSource + ((y + clipY) * sStride) + clipX + 1;

				for (x = 1; x < clipWidth2; x++)
				{
					gray = palette[*pCurrent];

					if (gray > whiteThreshold)
					{
						pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					}
					else if (gray > blackThreshold)
					{
						ratio = (gray - blackThreshold) / (float)thresholdsDistance;

						ulcorner = palette[pCurrent[-1]] + palette[pCurrent[-sStride]] + palette[pCurrent[-sStride - 1]] / 2;
						urcorner = palette[pCurrent[+1]] + palette[pCurrent[-sStride]] + palette[pCurrent[-sStride + 1]] / 2;
						llcorner = palette[pCurrent[-1]] + palette[pCurrent[0]] + palette[pCurrent[0]] / 2;
						lrcorner = palette[pCurrent[+1]] + palette[pCurrent[0]] + palette[pCurrent[0]] / 2;

						if (ratio < .33F)
						{
							maxCorner = ulcorner;
							maxIndex = 0;
							yAdd = 0;

							if (urcorner > maxCorner)
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

							pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
						}
						else if (ratio < .66F)
						{
							if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

								if (urcorner > llcorner || urcorner > lrcorner)
								{
									pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

									if (llcorner > lrcorner)
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
									else
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								}
								else
								{
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
								}
							}
							else
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
					}

					pCurrent++;
				}

				//RIGHT
				x = clip.Width - 1;
				pCurrent = pSource + ((1 + clipY) * sStride) + clip.Right - 1;

				for (y = 1; y < clipHeight2; y++)
				{
					gray = palette[*pCurrent];

					if (gray > whiteThreshold)
					{
						pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					}
					else if (gray > blackThreshold)
					{
						ratio = (gray - blackThreshold) / (float)thresholdsDistance;

						ulcorner = palette[pCurrent[-1]] + palette[pCurrent[-sStride]] + palette[pCurrent[-sStride - 1]] / 2;
						urcorner = palette[pCurrent[0]] + palette[pCurrent[-sStride]] + palette[pCurrent[0]] / 2;
						llcorner = palette[pCurrent[-1]] + palette[pCurrent[+sStride]] + palette[pCurrent[+sStride - 1]] / 2;
						lrcorner = palette[pCurrent[0]] + palette[pCurrent[+sStride]] + palette[pCurrent[0]] / 2;

						if (ratio < .33F)
						{
							maxCorner = ulcorner;
							maxIndex = 0;
							yAdd = 0;

							if (urcorner > maxCorner)
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

							pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
						}
						else if (ratio < .66F)
						{
							if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

								if (urcorner > llcorner || urcorner > lrcorner)
								{
									pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

									if (llcorner > lrcorner)
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
									else
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								}
								else
								{
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
								}
							}
							else
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
					}

					pCurrent += sStride;
				}
			}

#if DEBUG
			Console.WriteLine("DRS2 Binorize8bpp():" + (DateTime.Now.Subtract(start)).ToString()) ;
#endif

			if (textPreferred)
				SqueezePixels8bpp(sourceData, palette, resultData, whiteThreshold, blackThreshold, clip);

			sourceBmp.UnlockBits(sourceData);
			resultBmp.UnlockBits(resultData);

			return resultBmp;
		}
		#endregion

		#region SqueezePixels8bpp()
		private static void SqueezePixels8bpp(BitmapData sourceData, int[] palette, BitmapData resultData,
			int whiteThreshold, int blackThreshold, Rectangle clip)
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			//histogram.Show() ;
			byte toChangeRange = (byte)(.1F * ((whiteThreshold - blackThreshold) / 3));

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;
			int x, y;

			unsafe
			{
				byte* pSourceGray = (byte*)sourceData.Scan0.ToPointer();
				byte* pResultCopy = (byte*)resultData.Scan0.ToPointer();
				byte* pGrayCurrent;
				byte* pResult;

				int height = resultData.Height - 2;
				int width = resultData.Width / 8 - 1;
				int clipX = clip.X;
				int clipY = clip.Y;
				byte* lineUp, line1, line2, lineDown;
				int gray, grayCorner1, grayCorner2, grayCorner3;

				for (y = 2; y < height; y = y + 2)
				{
					for (x = 1; x < width; x++)
					{
						pGrayCurrent = pSourceGray + ((y + clipY) * sStride / 2) + (x + clipX) * 4;
						pResult = pResultCopy + y * rStride + x;

						lineUp = pResultCopy + (y - 1) * rStride + x;
						line1 = lineUp + rStride;
						line2 = line1 + rStride;
						lineDown = line2 + rStride;

						gray = palette[*pGrayCurrent];
						//pixel1 UL
						if (gray < whiteThreshold && gray > blackThreshold)
						{
							if ((*line1 & 0x80) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent - sStride - 1)];
								grayCorner2 = palette[*(pGrayCurrent - sStride)];
								grayCorner3 = palette[*(pGrayCurrent - 1)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp - 1) & 0x01) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x80) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1 - 1) & 0x01) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x80;
									pResult[-rStride - 1] &= 0xFE;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x80;
									pResult[-rStride] &= 0x7F;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x80;
									pResult[-1] &= 0xFE;
								}
							}

							//pixel1 UR
							if ((*line1 & 0x40) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent - sStride)];
								grayCorner2 = palette[*(pGrayCurrent - sStride + 1)];
								grayCorner3 = palette[*(pGrayCurrent + 1)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x40) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x20) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x20) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x40;
									pResult[-rStride] &= 0xBF;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x40;
									pResult[-rStride] &= 0xDF;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x40;
									*pResult &= 0xDF;
								}
							}

							//pixel1 LL
							pResult += rStride;

							if ((*line2 & 0x80) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent - 1)];
								grayCorner2 = palette[*(pGrayCurrent + sStride - 1)];
								grayCorner3 = palette[*(pGrayCurrent + sStride)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2 - 1) & 0x01) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown - 1) & 0x01) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x80) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x80;
									pResult[-1] &= 0xFE;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x80;
									pResult[+rStride - 1] &= 0xFE;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x80;
									pResult[+rStride] &= 0x7F;
								}
							}

							//pixel1 LR
							if ((*line2 & 0x40) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent + 1)];
								grayCorner2 = palette[*(pGrayCurrent + sStride)];
								grayCorner3 = palette[*(pGrayCurrent + sStride + 1)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x20) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x40) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x20) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x40;
									*pResult &= 0xBF;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x40;
									pResult[rStride] &= 0xBF;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x40;
									pResult[rStride] &= 0xDF;
								}
							}

							pResult -= rStride;
						}

						//pixel2 UL
						pGrayCurrent++;
						gray = palette[*pGrayCurrent];

						if (gray < whiteThreshold && gray > blackThreshold)
						{
							if ((*line1 & 0x20) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent - sStride - 1)];
								grayCorner2 = palette[*(pGrayCurrent - sStride)];
								grayCorner3 = palette[*(pGrayCurrent - 1)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x40) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x20) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x40) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x20;
									pResult[-rStride] &= 0xBF;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x20;
									pResult[-rStride] &= 0xDF;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x20;
									*pResult &= 0xBF;
								}
							}

							//pixel2 UR
							if ((*line1 & 0x10) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent - sStride)];
								grayCorner2 = palette[*(pGrayCurrent - sStride + 1)];
								grayCorner3 = palette[*(pGrayCurrent + 1)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x10) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x08) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x08) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x10;
									pResult[-rStride] &= 0xEF;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x10;
									pResult[-rStride] &= 0xF7;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x10;
									*pResult &= 0xF7;
								}
							}

							//pixel2 LL
							pResult += rStride;

							if ((*line2 & 0x20) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent - 1)];
								grayCorner2 = palette[*(pGrayCurrent + sStride - 1)];
								grayCorner3 = palette[*(pGrayCurrent + sStride)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x40) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x40) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x20) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x20;
									*pResult &= 0xBF;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x20;
									pResult[rStride] &= 0xBF;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x20;
									pResult[rStride] &= 0xDF;
								}
							}

							//pixel2 LR
							if ((*line2 & 0x10) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent + 1)];
								grayCorner2 = palette[*(pGrayCurrent + sStride)];
								grayCorner3 = palette[*(pGrayCurrent + sStride + 1)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x08) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x10) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x08) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x10;
									*pResult &= 0xF7;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x10;
									pResult[rStride] &= 0xEF;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x10;
									pResult[rStride] &= 0xF7;
								}
							}

							pResult -= rStride;
						}

						//pixel3 UL
						pGrayCurrent++;
						gray = palette[*pGrayCurrent];

						if (gray < whiteThreshold && gray > blackThreshold)
						{
							if ((*line1 & 0x08) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent - sStride - 1)];
								grayCorner2 = palette[*(pGrayCurrent - sStride)];
								grayCorner3 = palette[*(pGrayCurrent - 1)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x10) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x08) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x10) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x08;
									pResult[-rStride] &= 0xEF;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x08;
									pResult[-rStride] &= 0xF7;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x08;
									*pResult &= 0xEF;
								}
							}

							//pixel3 UR
							if ((*line1 & 0x04) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent - sStride)];
								grayCorner2 = palette[*(pGrayCurrent - sStride + 1)];
								grayCorner3 = palette[*(pGrayCurrent + 1)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x04) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x02) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x02) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x04;
									pResult[-rStride] &= 0xFB;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x04;
									pResult[-rStride] &= 0xFD;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x04;
									*pResult &= 0xFD;
								}
							}

							//pixel3 LL
							pResult += rStride;

							if ((*line2 & 0x08) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent - 1)];
								grayCorner2 = palette[*(pGrayCurrent + sStride - 1)];
								grayCorner3 = palette[*(pGrayCurrent + sStride)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x10) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x10) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x08) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x08;
									*pResult &= 0xEF;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x08;
									pResult[rStride] &= 0xEF;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x08;
									pResult[rStride] &= 0xF7;
								}
							}

							//pixel3 LR
							if ((*line2 & 0x04) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent + 1)];
								grayCorner2 = palette[*(pGrayCurrent + sStride)];
								grayCorner3 = palette[*(pGrayCurrent + sStride + 1)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x02) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x04) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x02) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x04;
									*pResult &= 0xFD;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x04;
									pResult[rStride] &= 0xFB;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x04;
									pResult[rStride] &= 0xFD;
								}
							}

							pResult -= rStride;
						}

						//pixel4 UL
						pGrayCurrent++;
						gray = palette[*pGrayCurrent];

						if (gray < whiteThreshold && gray > blackThreshold)
						{
							if ((*line1 & 0x02) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent - sStride - 1)];
								grayCorner2 = palette[*(pGrayCurrent - sStride)];
								grayCorner3 = palette[*(pGrayCurrent - 1)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x04) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x02) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x04) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x02;
									pResult[-rStride] &= 0xFB;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x02;
									pResult[-rStride] &= 0xFD;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x02;
									*pResult &= 0xFB;
								}
							}

							//pixel4 UR
							if ((*line1 & 0x01) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent - sStride)];
								grayCorner2 = palette[*(pGrayCurrent - sStride + 1)];
								grayCorner3 = palette[*(pGrayCurrent + 1)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x01) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp + 1) & 0x80) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1 + 1) & 0x80) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x01;
									pResult[-rStride] &= 0xFE;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x01;
									pResult[-rStride + 1] &= 0x7F;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x01;
									pResult[1] &= 0x7F;
								}
							}

							//pixel4 LL
							pResult += rStride;

							if ((*line2 & 0x02) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent - 1)];
								grayCorner2 = palette[*(pGrayCurrent + sStride - 1)];
								grayCorner3 = palette[*(pGrayCurrent + sStride)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x04) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x04) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x02) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x02;
									*pResult &= 0xFB;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x02;
									pResult[rStride] &= 0xFB;
								}
								else if (grayCorner3 < 768)
								{
									*pResult |= 0x02;
									pResult[rStride] &= 0xFD;
								}
							}

							//pixel4 LR
							if ((*line2 & 0x01) == 0)
							{
								grayCorner1 = palette[*(pGrayCurrent + 1)];
								grayCorner2 = palette[*(pGrayCurrent + sStride)];
								grayCorner3 = palette[*(pGrayCurrent + sStride + 1)];

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2 + 1) & 0x80) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x01) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown + 1) & 0x80) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 768 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x01;
									pResult[1] &= 0x7F;
								}
								else if (grayCorner2 < 768 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x01;
									pResult[rStride] &= 0xFE;
								}
								else if (grayCorner3 < 768)
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

		#region Binorize8bppGrayscale()
		private static Bitmap Binorize8bppGrayscale(Bitmap sourceBmp, Rectangle clip, int bThresholdDelta, int wThresholdDelta, bool resultManaged, bool textPreferred)
		{
			//Bitmap		resultBmp = new Bitmap(clip.Width * 2, clip.Height * 2, PixelFormat.Format1bppIndexed); 
			Bitmap resultBmp;

			if (resultManaged)
				resultBmp = new Bitmap(clip.Width * 2, clip.Height * 2, PixelFormat.Format1bppIndexed);
			else
			{
				unsafe
				{
					Bitmap bmp1Bpp = new Bitmap(clip.Width * 2, 1, PixelFormat.Format1bppIndexed);
					BitmapData bmp1BppData = bmp1Bpp.LockBits(new Rectangle(0, 0, bmp1Bpp.Width, bmp1Bpp.Height), ImageLockMode.ReadOnly, bmp1Bpp.PixelFormat);
					byte* scan0 = (byte*)HeapAlloc(ph, HEAP_ZERO_MEMORY, bmp1BppData.Stride * clip.Height * 2);

					resultBmp = new Bitmap(clip.Width * 2, clip.Height * 2, bmp1BppData.Stride, PixelFormat.Format1bppIndexed, new IntPtr(scan0));

					bmp1Bpp.UnlockBits(bmp1BppData);
					bmp1Bpp.Dispose();
				}
			}

			BitmapData sourceData = sourceBmp.LockBits(clip, ImageLockMode.ReadOnly, sourceBmp.PixelFormat);
			BitmapData resultData = resultBmp.LockBits(new Rectangle(0, 0, resultBmp.Width, resultBmp.Height), ImageLockMode.WriteOnly, resultBmp.PixelFormat);

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif
			Histogram histogram;

			if (clip.Width < 800 || clip.Height < 800)
				histogram = new Histogram(sourceData, sourceBmp.Palette.Entries, clip);
			else
				histogram = new Histogram(sourceData, sourceBmp.Palette.Entries, Rectangle.Inflate(clip, -100, -100));
			//byte		whiteThreshold = (byte) (Math.Max(histogram.Extreme, histogram.SecondExtreme) - 30);		
			//short		blackThreshold = (byte) (Math.Min(histogram.Extreme, histogram.SecondExtreme) + 30);
			int whiteThreshold = (int)(Math.Max(histogram.Extreme, histogram.SecondExtreme) + wThresholdDelta);
			int blackThreshold = (int)(Math.Min(histogram.Extreme, histogram.SecondExtreme) + bThresholdDelta);

			if (blackThreshold > whiteThreshold)
				blackThreshold = whiteThreshold;

			//histogram.Show() ;
			byte gray;
			float ratio;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

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
				int clipX = clip.X;
				int clipY = clip.Y;
				int x, y;
				int maxCorner;
				int maxIndex;
				int yAdd;
				float thresholdsDistance = (float)(whiteThreshold - blackThreshold);

				if (thresholdsDistance < 1)
					thresholdsDistance = 1;

				for (y = 1; y < clipHeight2; y++)
				{
					pCurrent = pSource + ((y + clipY) * sStride) + clipX + 1;

					for (x = 1; x < clipWidth2; x++)
					{
						gray = *pCurrent;

						if (gray > whiteThreshold)
						{
							//pResult[y       * 2 * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
							//pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte) (0xC0 >> ((x & 0x03) * 2));
							pResult[(y * rStride << 1) + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) << 1));
							pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) << 1));
						}
						else if (gray > blackThreshold)
						{
							ratio = (gray - blackThreshold) / (float)thresholdsDistance;

							ulcorner = pCurrent[-1] + pCurrent[-sStride] + (pCurrent[-sStride - 1] >> 1);
							urcorner = pCurrent[+1] + pCurrent[-sStride] + (pCurrent[-sStride + 1] >> 1);
							llcorner = pCurrent[-1] + pCurrent[+sStride] + (pCurrent[+sStride - 1] >> 1);
							lrcorner = pCurrent[+1] + pCurrent[+sStride] + (pCurrent[+sStride + 1] >> 1);

							if (ratio < .33F)
							{
								maxCorner = ulcorner;
								maxIndex = 0;
								yAdd = 0;

								if (urcorner > maxCorner)
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
							else if (ratio < .66F)
							{
								if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
									pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));
								if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
									pResult[(y << 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1) + 1));
								if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
									pResult[((y << 1) + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) << 1)));
								if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
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

						pCurrent++;
					}
				}

				//	CORNERS
				//UL CORNER
				x = 0;
				y = 0;
				pCurrent = pSource + ((y + clipY) * sStride) + clipX;
				gray = *pCurrent;

				if (gray > whiteThreshold)
				{
					pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
				}
				else if (gray > blackThreshold)
				{
					ratio = (gray - blackThreshold) / (float)thresholdsDistance;

					ulcorner = pCurrent[0] + pCurrent[0] + pCurrent[0] / 2;
					urcorner = pCurrent[+1] + pCurrent[0] + pCurrent[0] / 2;
					llcorner = pCurrent[0] + pCurrent[+sStride] + pCurrent[0] / 2;
					lrcorner = pCurrent[+1] + pCurrent[+sStride] + pCurrent[+sStride + 1] / 2;

					if (ratio < .33F)
					{
						maxCorner = ulcorner;
						maxIndex = 0;
						yAdd = 0;

						if (urcorner > maxCorner)
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

						pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
					}
					else if (ratio < .66F)
					{
						if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
					}
					else
					{
						if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

							if (urcorner > llcorner || urcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

								if (llcorner > lrcorner)
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
								else
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							}
							else
							{
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}
				}

				//UR CORNER
				x = clip.Width - 1;
				y = 0;
				pCurrent = pSource + ((y + clipY) * sStride) + clip.Right - 1;
				gray = *pCurrent;

				if (gray > whiteThreshold)
				{
					pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
				}
				else if (gray > blackThreshold)
				{
					ratio = (gray - blackThreshold) / (float)thresholdsDistance;

					ulcorner = pCurrent[-1] + pCurrent[0] + pCurrent[0] / 2;
					urcorner = pCurrent[0] + pCurrent[0] + pCurrent[0] / 2;
					llcorner = pCurrent[-1] + pCurrent[+sStride] + pCurrent[+sStride - 1] / 2;
					lrcorner = pCurrent[0] + pCurrent[+sStride] + pCurrent[0] / 2;

					if (ratio < .33F)
					{
						maxCorner = ulcorner;
						maxIndex = 0;
						yAdd = 0;

						if (urcorner > maxCorner)
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

						pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
					}
					else if (ratio < .66F)
					{
						if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
					}
					else
					{
						if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

							if (urcorner > llcorner || urcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

								if (llcorner > lrcorner)
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
								else
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							}
							else
							{
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}
				}

				//LL CORNER
				x = 0;
				y = clip.Height - 1;
				pCurrent = pSource + ((y + clipY) * sStride) + clipX;
				gray = *pCurrent;

				if (gray > whiteThreshold)
				{
					pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
				}
				else if (gray > blackThreshold)
				{
					ratio = (gray - blackThreshold) / (float)thresholdsDistance;

					ulcorner = pCurrent[0] + pCurrent[-sStride] + pCurrent[0] / 2;
					urcorner = pCurrent[+1] + pCurrent[-sStride] + pCurrent[-sStride + 1] / 2;
					llcorner = pCurrent[0] + pCurrent[0] + pCurrent[0] / 2;
					lrcorner = pCurrent[+1] + pCurrent[0] + pCurrent[0] / 2;

					if (ratio < .33F)
					{
						maxCorner = ulcorner;
						maxIndex = 0;
						yAdd = 0;

						if (urcorner > maxCorner)
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

						pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
					}
					else if (ratio < .66F)
					{
						if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
					}
					else
					{
						if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

							if (urcorner > llcorner || urcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

								if (llcorner > lrcorner)
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
								else
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							}
							else
							{
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}
				}

				//LR CORNER
				x = clip.Width - 1;
				y = clip.Height - 1;
				pCurrent = pSource + ((y + clipY) * sStride) + clip.Right - 1;
				gray = *pCurrent;

				if (gray > whiteThreshold)
				{
					pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
				}
				else if (gray > blackThreshold)
				{
					ratio = (gray - blackThreshold) / (float)thresholdsDistance;

					ulcorner = pCurrent[-1] + pCurrent[-sStride] + pCurrent[-sStride - 1] / 2;
					urcorner = pCurrent[0] + pCurrent[-sStride] + pCurrent[0] / 2;
					llcorner = pCurrent[-1] + pCurrent[0] + pCurrent[0] / 2;
					lrcorner = pCurrent[0] + pCurrent[0] + pCurrent[0] / 2;

					if (ratio < .33F)
					{
						maxCorner = ulcorner;
						maxIndex = 0;
						yAdd = 0;

						if (urcorner > maxCorner)
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

						pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
					}
					else if (ratio < .66F)
					{
						if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
						if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
					}
					else
					{
						if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

							if (urcorner > llcorner || urcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

								if (llcorner > lrcorner)
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
								else
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							}
							else
							{
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
						else
						{
							pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						}
					}
				}


				//borders
				//TOP
				y = 0;
				pCurrent = pSource + ((y + clipY) * sStride) + clipX + 1;

				for (x = 1; x < clipWidth2; x++)
				{
					gray = *pCurrent;

					if (gray > whiteThreshold)
					{
						pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					}
					else if (gray > blackThreshold)
					{
						ratio = (gray - blackThreshold) / (float)thresholdsDistance;

						ulcorner = pCurrent[-1] + pCurrent[0] + pCurrent[0] / 2;
						urcorner = pCurrent[+1] + pCurrent[0] + pCurrent[0] / 2;
						llcorner = pCurrent[-1] + pCurrent[+sStride] + pCurrent[+sStride - 1] / 2;
						lrcorner = pCurrent[+1] + pCurrent[+sStride] + pCurrent[+sStride + 1] / 2;

						if (ratio < .33F)
						{
							maxCorner = ulcorner;
							maxIndex = 0;
							yAdd = 0;

							if (urcorner > maxCorner)
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

							pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
						}
						else if (ratio < .66F)
						{
							if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

								if (urcorner > llcorner || urcorner > lrcorner)
								{
									pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

									if (llcorner > lrcorner)
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
									else
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								}
								else
								{
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
								}
							}
							else
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
					}

					pCurrent++;
				}

				//LEFT
				x = 0;
				pCurrent = pSource + ((1 + clipY) * sStride) + clipX;

				for (y = 1; y < clipHeight2; y++)
				{
					gray = *pCurrent;

					if (gray > whiteThreshold)
					{
						pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					}
					else if (gray > blackThreshold)
					{
						ratio = (gray - blackThreshold) / (float)thresholdsDistance;

						ulcorner = pCurrent[0] + pCurrent[-sStride] + pCurrent[0] / 2;
						urcorner = pCurrent[+1] + pCurrent[-sStride] + pCurrent[-sStride + 1] / 2;
						llcorner = pCurrent[0] + pCurrent[+sStride] + pCurrent[0] / 2;
						lrcorner = pCurrent[+1] + pCurrent[+sStride] + pCurrent[+sStride + 1] / 2;

						if (ratio < .33F)
						{
							maxCorner = ulcorner;
							maxIndex = 0;
							yAdd = 0;

							if (urcorner > maxCorner)
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

							pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
						}
						else if (ratio < .66F)
						{
							if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

								if (urcorner > llcorner || urcorner > lrcorner)
								{
									pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

									if (llcorner > lrcorner)
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
									else
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								}
								else
								{
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
								}
							}
							else
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
					}

					pCurrent += sStride;
				}

				//BOTTOM
				y = clip.Height - 1;
				pCurrent = pSource + ((y + clipY) * sStride) + clipX + 1;

				for (x = 1; x < clipWidth2; x++)
				{
					gray = *pCurrent;

					if (gray > whiteThreshold)
					{
						pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					}
					else if (gray > blackThreshold)
					{
						ratio = (gray - blackThreshold) / (float)thresholdsDistance;

						ulcorner = pCurrent[-1] + pCurrent[-sStride] + pCurrent[-sStride - 1] / 2;
						urcorner = pCurrent[+1] + pCurrent[-sStride] + pCurrent[-sStride + 1] / 2;
						llcorner = pCurrent[-1] + pCurrent[0] + pCurrent[0] / 2;
						lrcorner = pCurrent[+1] + pCurrent[0] + pCurrent[0] / 2;

						if (ratio < .33F)
						{
							maxCorner = ulcorner;
							maxIndex = 0;
							yAdd = 0;

							if (urcorner > maxCorner)
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

							pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
						}
						else if (ratio < .66F)
						{
							if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

								if (urcorner > llcorner || urcorner > lrcorner)
								{
									pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

									if (llcorner > lrcorner)
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
									else
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								}
								else
								{
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
								}
							}
							else
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
					}

					pCurrent++;
				}

				//RIGHT
				x = clip.Width - 1;
				pCurrent = pSource + ((1 + clipY) * sStride) + clip.Right - 1;

				for (y = 1; y < clipHeight2; y++)
				{

					gray = *pCurrent;

					if (gray > whiteThreshold)
					{
						pResult[y * 2 * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
						pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
					}
					else if (gray > blackThreshold)
					{
						ratio = (gray - blackThreshold) / (float)thresholdsDistance;

						ulcorner = pCurrent[-1] + pCurrent[-sStride] + pCurrent[-sStride - 1] / 2;
						urcorner = pCurrent[0] + pCurrent[-sStride] + pCurrent[0] / 2;
						llcorner = pCurrent[-1] + pCurrent[+sStride] + pCurrent[+sStride - 1] / 2;
						lrcorner = pCurrent[0] + pCurrent[+sStride] + pCurrent[0] / 2;

						if (ratio < .33F)
						{
							maxCorner = ulcorner;
							maxIndex = 0;
							yAdd = 0;

							if (urcorner > maxCorner)
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

							pResult[(y * 2 + yAdd) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + maxIndex));
						}
						else if (ratio < .66F)
						{
							if ((ulcorner > urcorner && (ulcorner > llcorner || ulcorner > lrcorner)) || (ulcorner > llcorner && ulcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((urcorner > ulcorner && (urcorner > llcorner || urcorner > lrcorner)) || (urcorner > llcorner && urcorner > lrcorner))
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
							if ((llcorner > ulcorner && (llcorner > urcorner || llcorner > lrcorner)) || (llcorner > urcorner && llcorner > lrcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
							if ((lrcorner > ulcorner && (lrcorner > urcorner || lrcorner > llcorner)) || (lrcorner > urcorner && lrcorner > llcorner))
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
						}
						else
						{
							if (ulcorner > urcorner || ulcorner > llcorner || ulcorner > lrcorner)
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));

								if (urcorner > llcorner || urcorner > lrcorner)
								{
									pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));

									if (llcorner > lrcorner)
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2)));
									else
										pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								}
								else
								{
									pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
								}
							}
							else
							{
								pResult[(y) * 2 * rStride + (x >> 2)] |= (byte)(0x80 >> (((x & 0x03) * 2) + 1));
								pResult[(y * 2 + 1) * rStride + (x >> 2)] |= (byte)(0xC0 >> ((x & 0x03) * 2));
							}
						}
					}

					pCurrent += sStride;
				}
			}

#if DEBUG
			Console.WriteLine("DRS2 Binorize8bppGrayscale():" + (DateTime.Now.Subtract(start)).ToString()) ;
#endif

			if (textPreferred)
				SqueezePixels8bppGrayscale(sourceData, resultData, whiteThreshold, blackThreshold, clip);

			sourceBmp.UnlockBits(sourceData);
			resultBmp.UnlockBits(resultData);

			return resultBmp;
		}
		#endregion

		#region SqueezePixels8bppGrayscale()
		private static void SqueezePixels8bppGrayscale(BitmapData sourceData, BitmapData resultData,
			int whiteThreshold, int blackThreshold, Rectangle clip)
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			//histogram.Show() ;
			byte gray;
			byte toChangeRange = (byte)(.1F * (whiteThreshold - blackThreshold));

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;
			int x, y;

			unsafe
			{
				byte* pSourceGray = (byte*)sourceData.Scan0.ToPointer();
				byte* pResultCopy = (byte*)resultData.Scan0.ToPointer();
				byte* pGrayCurrent;
				byte* pResult;

				int height = resultData.Height - 2;
				int width = resultData.Width / 8 - 1;
				int clipX = clip.X;
				int clipY = clip.Y;
				byte* lineUp, line1, line2, lineDown;
				int grayCorner1, grayCorner2, grayCorner3;

				for (y = 2; y < height; y = y + 2)
				{
					for (x = 1; x < width; x++)
					{
						pGrayCurrent = pSourceGray + ((y + clipY) * sStride / 2) + (x + clipX) * 4;
						pResult = pResultCopy + y * rStride + x;

						lineUp = pResultCopy + (y - 1) * rStride + x;
						line1 = lineUp + rStride;
						line2 = line1 + rStride;
						lineDown = line2 + rStride;

						gray = *pGrayCurrent;
						//pixel1 UL
						if (gray < whiteThreshold && gray > blackThreshold)
						{
							if ((*line1 & 0x80) == 0)
							{
								grayCorner1 = *(pGrayCurrent - sStride - 1);
								grayCorner2 = *(pGrayCurrent - sStride);
								grayCorner3 = *(pGrayCurrent - 1);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp - 1) & 0x01) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x80) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1 - 1) & 0x01) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x80;
									pResult[-rStride - 1] &= 0xFE;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x80;
									pResult[-rStride] &= 0x7F;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x80;
									pResult[-1] &= 0xFE;
								}
							}

							//pixel1 UR
							if ((*line1 & 0x40) == 0)
							{
								grayCorner1 = *(pGrayCurrent - sStride);
								grayCorner2 = *(pGrayCurrent - sStride + 1);
								grayCorner3 = *(pGrayCurrent + 1);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x40) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x20) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x20) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x40;
									pResult[-rStride] &= 0xBF;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x40;
									pResult[-rStride] &= 0xDF;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x40;
									*pResult &= 0xDF;
								}
							}

							//pixel1 LL
							pResult += rStride;

							if ((*line2 & 0x80) == 0)
							{
								grayCorner1 = *(pGrayCurrent - 1);
								grayCorner2 = *(pGrayCurrent + sStride - 1);
								grayCorner3 = *(pGrayCurrent + sStride);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2 - 1) & 0x01) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown - 1) & 0x01) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x80) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x80;
									pResult[-1] &= 0xFE;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x80;
									pResult[+rStride - 1] &= 0xFE;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x80;
									pResult[+rStride] &= 0x7F;
								}
							}

							//pixel1 LR
							if ((*line2 & 0x40) == 0)
							{
								grayCorner1 = *(pGrayCurrent + 1);
								grayCorner2 = *(pGrayCurrent + sStride);
								grayCorner3 = *(pGrayCurrent + sStride + 1);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x20) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x40) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x20) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x40;
									*pResult &= 0xBF;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x40;
									pResult[rStride] &= 0xBF;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x40;
									pResult[rStride] &= 0xDF;
								}
							}

							pResult -= rStride;
						}

						//pixel2 UL
						pGrayCurrent++;
						gray = *pGrayCurrent;

						if (gray < whiteThreshold && gray > blackThreshold)
						{
							if ((*line1 & 0x20) == 0)
							{
								grayCorner1 = *(pGrayCurrent - sStride - 1);
								grayCorner2 = *(pGrayCurrent - sStride);
								grayCorner3 = *(pGrayCurrent - 1);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x40) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x20) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x40) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x20;
									pResult[-rStride] &= 0xBF;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x20;
									pResult[-rStride] &= 0xDF;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x20;
									*pResult &= 0xBF;
								}
							}

							//pixel2 UR
							if ((*line1 & 0x10) == 0)
							{
								grayCorner1 = *(pGrayCurrent - sStride);
								grayCorner2 = *(pGrayCurrent - sStride + 1);
								grayCorner3 = *(pGrayCurrent + 1);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x10) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x08) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x08) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x10;
									pResult[-rStride] &= 0xEF;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x10;
									pResult[-rStride] &= 0xF7;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x10;
									*pResult &= 0xF7;
								}
							}

							//pixel2 LL
							pResult += rStride;

							if ((*line2 & 0x20) == 0)
							{
								grayCorner1 = *(pGrayCurrent - 1);
								grayCorner2 = *(pGrayCurrent + sStride - 1);
								grayCorner3 = *(pGrayCurrent + sStride);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x40) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x40) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x20) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x20;
									*pResult &= 0xBF;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x20;
									pResult[rStride] &= 0xBF;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x20;
									pResult[rStride] &= 0xDF;
								}
							}

							//pixel2 LR
							if ((*line2 & 0x10) == 0)
							{
								grayCorner1 = *(pGrayCurrent + 1);
								grayCorner2 = *(pGrayCurrent + sStride);
								grayCorner3 = *(pGrayCurrent + sStride + 1);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x08) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x10) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x08) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x10;
									*pResult &= 0xF7;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x10;
									pResult[rStride] &= 0xEF;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x10;
									pResult[rStride] &= 0xF7;
								}
							}

							pResult -= rStride;
						}

						//pixel3 UL
						pGrayCurrent++;
						gray = *pGrayCurrent;

						if (gray < whiteThreshold && gray > blackThreshold)
						{
							if ((*line1 & 0x08) == 0)
							{
								grayCorner1 = *(pGrayCurrent - sStride - 1);
								grayCorner2 = *(pGrayCurrent - sStride);
								grayCorner3 = *(pGrayCurrent - 1);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x10) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x08) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x10) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x08;
									pResult[-rStride] &= 0xEF;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x08;
									pResult[-rStride] &= 0xF7;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x08;
									*pResult &= 0xEF;
								}
							}

							//pixel3 UR
							if ((*line1 & 0x04) == 0)
							{
								grayCorner1 = *(pGrayCurrent - sStride);
								grayCorner2 = *(pGrayCurrent - sStride + 1);
								grayCorner3 = *(pGrayCurrent + 1);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x04) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x02) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x02) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x04;
									pResult[-rStride] &= 0xFB;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x04;
									pResult[-rStride] &= 0xFD;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x04;
									*pResult &= 0xFD;
								}
							}

							//pixel3 LL
							pResult += rStride;

							if ((*line2 & 0x08) == 0)
							{
								grayCorner1 = *(pGrayCurrent - 1);
								grayCorner2 = *(pGrayCurrent + sStride - 1);
								grayCorner3 = *(pGrayCurrent + sStride);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x10) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x10) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x08) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x08;
									*pResult &= 0xEF;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x08;
									pResult[rStride] &= 0xEF;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x08;
									pResult[rStride] &= 0xF7;
								}
							}

							//pixel3 LR
							if ((*line2 & 0x04) == 0)
							{
								grayCorner1 = *(pGrayCurrent + 1);
								grayCorner2 = *(pGrayCurrent + sStride);
								grayCorner3 = *(pGrayCurrent + sStride + 1);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x02) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x04) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x02) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x04;
									*pResult &= 0xFD;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x04;
									pResult[rStride] &= 0xFB;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x04;
									pResult[rStride] &= 0xFD;
								}
							}

							pResult -= rStride;
						}

						//pixel4 UL
						pGrayCurrent++;
						gray = *pGrayCurrent;

						if (gray < whiteThreshold && gray > blackThreshold)
						{
							if ((*line1 & 0x02) == 0)
							{
								grayCorner1 = *(pGrayCurrent - sStride - 1);
								grayCorner2 = *(pGrayCurrent - sStride);
								grayCorner3 = *(pGrayCurrent - 1);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x04) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp) & 0x02) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1) & 0x04) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x02;
									pResult[-rStride] &= 0xFB;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x02;
									pResult[-rStride] &= 0xFD;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x02;
									*pResult &= 0xFB;
								}
							}

							//pixel4 UR
							if ((*line1 & 0x01) == 0)
							{
								grayCorner1 = *(pGrayCurrent - sStride);
								grayCorner2 = *(pGrayCurrent - sStride + 1);
								grayCorner3 = *(pGrayCurrent + 1);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(lineUp) & 0x01) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineUp + 1) & 0x80) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(line1 + 1) & 0x80) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x01;
									pResult[-rStride] &= 0xFE;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x01;
									pResult[-rStride + 1] &= 0x7F;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x01;
									pResult[1] &= 0x7F;
								}
							}

							//pixel4 LL
							pResult += rStride;

							if ((*line2 & 0x02) == 0)
							{
								grayCorner1 = *(pGrayCurrent - 1);
								grayCorner2 = *(pGrayCurrent + sStride - 1);
								grayCorner3 = *(pGrayCurrent + sStride);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2) & 0x04) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x04) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown) & 0x02) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x02;
									*pResult &= 0xFB;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x02;
									pResult[rStride] &= 0xFB;
								}
								else if (grayCorner3 < 256)
								{
									*pResult |= 0x02;
									pResult[rStride] &= 0xFD;
								}
							}

							//pixel4 LR
							if ((*line2 & 0x01) == 0)
							{
								grayCorner1 = *(pGrayCurrent + 1);
								grayCorner2 = *(pGrayCurrent + sStride);
								grayCorner3 = *(pGrayCurrent + sStride + 1);

								if (grayCorner1 < blackThreshold || grayCorner1 > gray - toChangeRange || ((*(line2 + 1) & 0x80) == 0))
									grayCorner1 = int.MaxValue;

								if (grayCorner2 < blackThreshold || grayCorner2 > gray - toChangeRange || ((*(lineDown) & 0x01) == 0))
									grayCorner2 = int.MaxValue;

								if (grayCorner3 < blackThreshold || grayCorner3 > gray - toChangeRange || ((*(lineDown + 1) & 0x80) == 0))
									grayCorner3 = int.MaxValue;

								if (grayCorner1 < 256 && grayCorner1 < grayCorner2 && grayCorner1 < grayCorner3)
								{
									*pResult |= 0x01;
									pResult[1] &= 0x7F;
								}
								else if (grayCorner2 < 256 && grayCorner2 < grayCorner3)
								{
									*pResult |= 0x01;
									pResult[rStride] &= 0xFE;
								}
								else if (grayCorner3 < 256)
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
			Console.WriteLine("DRS2 SqueezePixels8bppGrayscale():" + (DateTime.Now.Subtract(start)).ToString()) ;
#endif
		}
		#endregion

	}
}
