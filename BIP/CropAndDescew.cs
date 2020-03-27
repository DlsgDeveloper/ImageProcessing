using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class CropAndDeskew
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

		#region constructor
		private CropAndDeskew()
		{
		}
		#endregion

		#region enum ActionType
		enum ActionType
		{
			Nothing,
			Crop,
			CropAndDeskew
		}
		#endregion 


		//	PUBLIC METHODS
		#region public methods

		#region GoMem()
		public unsafe static int GoMem(ref int width, ref int height, ref int stride, PixelFormat pixelFormat, 
			byte** firstByte, ColorPalette palette, Color threshold,
			float minAngleToDeskew, Rectangle clip, short marginX, short marginY) 
		{ 	
			return GoMem(ref width, ref height, ref stride, pixelFormat, firstByte, palette, threshold,
				minAngleToDeskew, clip, false, 10, 60, 20, 12, marginX, marginY);
		}

		public unsafe static int GoMem(ref int width, ref int height, ref int stride, PixelFormat pixelFormat,
			byte** firstByte, ColorPalette palette, Color threshold,
			float minAngleToDeskew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, short marginX, short marginY)
		{
			byte confidence = 0;

			try
			{
				Bitmap bitmap = new Bitmap(width, height, stride, pixelFormat, new IntPtr(*firstByte));
				if (bitmap == null)
					return 0;

				if (clip.IsEmpty)
					clip = Rectangle.FromLTRB(0, 0, width, height);
				else if (clip.Width == 0 || clip.Height == 0)
					clip = Rectangle.FromLTRB(clip.X, clip.Y, bitmap.Width - clip.X, bitmap.Height - clip.Y);

#if DEBUG
				DateTime start = DateTime.Now;
#endif

				Bitmap result = Go(bitmap, threshold, true, out confidence, minAngleToDeskew, clip, removeGhostLines,
					lowThreshold, highThreshold, linesToCheck, maxDelta, marginX, marginY, 1);
				
				bitmap.Dispose();
				bitmap = null;
				GC.Collect();
							
				BitmapData resultData = result.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, result.PixelFormat);
				width = resultData.Width;
				height = resultData.Height;
				stride = resultData.Stride;
				pixelFormat = resultData.PixelFormat;
				
				int		length = stride * height;
				byte[]	byteArray = new byte[length];
				
				unsafe
				{
					byte* pResult = (byte*) resultData.Scan0.ToPointer();
						
					for(int i = 0; i < length; i++)
						byteArray[i] = *(pResult++);
				}

				*firstByte = (byte*)HeapAlloc(ph, HEAP_ZERO_MEMORY, length);
				Marshal.Copy(byteArray, 0, new IntPtr(*firstByte), length);

				result.Dispose();
				result = null;

#if DEBUG
				Console.WriteLine(string.Format("RAM Image: {0}, Confidence:{1}%", DateTime.Now.Subtract(start).ToString(), confidence));
#endif
			}
			catch (Exception ex)
			{
				throw new Exception("CropAndDeskew(): " + ex.Message);
			}

			return confidence;
		}
		#endregion
		
		#region GoStream() 
		public unsafe static int GoStream(byte** firstByte, int* length, Color threshold, short jpegCompression, 
			float minAngleToDeskew, Rectangle clip, short marginX, short marginY) 
		{
			return GoStream(firstByte, length, threshold, jpegCompression, minAngleToDeskew, clip, false, 10, 60, 20, 12, marginX, marginY);
		}
		
		public unsafe static int GoStream(byte** firstByte, int* length, Color threshold, short jpegCompression, 
			float minAngleToDeskew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, short marginX, short marginY) 
		{
#if DEBUG
			DateTime enterTime = DateTime.Now;
#endif
			byte confidence = 0;

			byte[]			array = new byte[*length];
			Marshal.Copy(new IntPtr(*firstByte), array, 0, (int) *length);
			Bitmap			bitmap;

			MemoryStream	stream = new MemoryStream(array);
			try
			{
				bitmap = new Bitmap(stream) ;

				if((bitmap.Width <= clip.X * 2) || (bitmap.Height <= clip.Y * 2))
					throw new Exception("Pased bitmap size is smaller than offset region!");
			}
			catch(Exception ex)
			{
				throw new Exception("Can't generate bitmap.\nException " + ex);
			}

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			Bitmap		result = Go(bitmap, threshold, true, out confidence, minAngleToDeskew, clip, removeGhostLines, 
				lowThreshold, highThreshold, linesToCheck, maxDelta, marginX, marginY, 1);

			//result.Save(@"C:\Users\jirka.stybnar\TestRun\CropAndDeskew\results\0006-orig.png", ImageFormat.Png);

			
#if DEBUG
			TimeSpan	time = DateTime.Now.Subtract(start) ;
			Console.Write(string.Format("Crop & Deskew: {0}, Confidence:{1}%",  time.ToString(), confidence)) ;
#endif
	
			ImageCodecInfo		codecInfo = Encoding.GetCodecInfo(bitmap);
			EncoderParameters	encoderParams = Encoding.GetEncoderParams(bitmap, jpegCompression) ;
				
			bitmap.Dispose();
			stream.Close();
			array = null;
			GC.Collect();

			MemoryStream	resultStream = new MemoryStream();
			try
			{
				result.Save(resultStream, codecInfo, encoderParams);
			}
			catch(Exception ex)
			{
				throw new Exception(string.Format("Can't save bitmap to stream.\nException: {0}\nStream : {1}\n" + 
					"Codec Info: {2}\nEncoder: {3}", ex.Message, (resultStream != null) ? "Exists": "null", 
					(codecInfo != null) ? codecInfo.CodecName : "null",
					(encoderParams != null) ? encoderParams.Param[0].ToString() : "null") );
			}
			GC.Collect();

			*length = (int) resultStream.Length;
			*firstByte = (byte*) HeapAlloc(ph, HEAP_ZERO_MEMORY, (int) resultStream.Length);
			Marshal.Copy(resultStream.ToArray(), 0, new IntPtr(*firstByte), (int) resultStream.Length);

			result.Dispose() ;
			resultStream.Close();
#if DEBUG
			Console.WriteLine(string.Format(" Total Time: {0}",  DateTime.Now.Subtract(enterTime).ToString())) ;
#endif
			GC.Collect();
			return confidence;
		}
		#endregion
				
		#region Go()
		public static int Go(string source, string dest, Color threshold, short jpegCompression,
			float minAngleToDeskew, Rectangle clip, short marginX, short marginY, int flags) 
		{ 			
			int result = Go(source, dest, threshold, jpegCompression, minAngleToDeskew, clip, false, 10, 50, 20, 12, marginX, marginY, flags);
			GC.Collect();
			return result;
		}

		public static int Go(string source, string dest, Color threshold, short jpegCompression, 
			float minAngleToDeskew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, short marginX, short marginY, int flags) 
		{ 			
			FileInfo	sourceFile = new FileInfo(source);
			FileInfo	destFile = new FileInfo(dest);

			if((sourceFile.Attributes & FileAttributes.Directory) > 0)
				return CropAndDeskewDir(new DirectoryInfo(sourceFile.FullName), new DirectoryInfo(destFile.FullName), 
					threshold, jpegCompression, minAngleToDeskew, clip, removeGhostLines, lowThreshold, highThreshold, 
					linesToCheck, maxDelta, marginX, marginY, flags);
			else
				return CropAndDeskewFile(sourceFile, destFile, threshold, jpegCompression, minAngleToDeskew, 
					clip, removeGhostLines, lowThreshold, highThreshold, linesToCheck, maxDelta, marginX, marginY, flags);
		}

		public static Bitmap Go(Bitmap bitmap, Color threshold, bool backDark, out byte confidence,
			float minAngleToDeskew, Rectangle clip, short marginX, short marginY, int flags)
		{
			return Go(bitmap, threshold, backDark, out confidence, minAngleToDeskew, clip, false, 10, 60, 20, 12, marginX, marginY, flags);
		}
		
		public static Bitmap Go(Bitmap bmpSource, Color threshold, bool backDark, out byte confidence, 
			float minAngleToDeskew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, short marginX, short marginY, int flags)
		{
			confidence = 0;
			
			if(bmpSource == null)
				return null ;

			if(clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, bmpSource.Width, bmpSource.Height);
			else if(clip.Width <= 0 || clip.Height <= 0)
				clip = Rectangle.FromLTRB(clip.X, clip.Y, bmpSource.Width - clip.X, bmpSource.Height - clip.Y);

			Bitmap		bmpResult = null ;

			ObjectByCorners objectByCorners = GetParams(bmpSource, threshold, backDark, out confidence, minAngleToDeskew, clip, removeGhostLines, 
				lowThreshold, highThreshold, linesToCheck, maxDelta, marginX, marginY);

			if (objectByCorners.Inclined == false)
			{
				Rectangle rect = new Rectangle(objectByCorners.UlCorner.X, objectByCorners.UlCorner.Y, objectByCorners.Width, objectByCorners.Height);

				if (rect != clip)
				{
					//rect.Inflate(-marginX, -marginY);
					
					rect = Rectangle.FromLTRB((rect.X == clip.X) ? 0 : rect.X, (rect.Y == clip.Y) ? 0 : rect.Y,
						(rect.Right >= clip.Right) ? bmpSource.Width : rect.Right, (rect.Bottom >= clip.Bottom) ? bmpSource.Height : rect.Bottom);

					bmpResult = ImageProcessing.ImageCopier.Copy(bmpSource, rect);
				}
				else
					bmpResult = ImageProcessing.ImageCopier.Copy(bmpSource);
			}
			else
			{
				try
				{
					//objectByCorners.Inflate(-marginX, -marginY);
					
					switch (bmpSource.PixelFormat)
					{
						case PixelFormat.Format32bppRgb:
						case PixelFormat.Format32bppArgb:
							bmpResult = CropAndDeskew32bpp(bmpSource, objectByCorners, flags);
							break;
						case PixelFormat.Format24bppRgb:
							bmpResult = CropAndDeskew24bpp(bmpSource, objectByCorners, flags);
							break;
						case PixelFormat.Format8bppIndexed:
							bmpResult = CropAndDeskew8bpp(bmpSource, objectByCorners, flags);
							break;
						case PixelFormat.Format1bppIndexed:
							bmpResult = CropAndDeskew1bpp(bmpSource, objectByCorners);
							break;
						default:
							throw new IpException(ErrorCode.ErrorUnsupportedFormat);
					}

					if (bmpResult != null)
					{
						Misc.SetBitmapResolution(bmpResult, bmpSource.HorizontalResolution, bmpSource.VerticalResolution);

						if (bmpSource.Palette != null && bmpSource.Palette.Entries.Length > 0)
							bmpResult.Palette = bmpSource.Palette;
					}
				}
				catch (Exception ex)
				{
					string error = string.Format("{0}\nPixel Format: {1}\nThreshold: {2}\nConfidence: {3}\nMin. Angle: {4}\nClip: {5}\nException: {6}",
						(bmpSource != null) ? "Bitmap: Exists" : "Bitmap: null", bmpSource.PixelFormat.ToString(), 
						threshold.ToString(), confidence.ToString(), minAngleToDeskew.ToString(), clip.ToString(), 
						ex.Message);

					throw new Exception("CropAndDeskew(): " + error + "\n");
				}
			}

			return bmpResult ;
		}
		#endregion

		#region GetParams()
		public static ObjectByCorners GetParams(Bitmap bmpSource, Color threshold, bool backDark, out byte confidence,
			float minAngleToDeskew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, short marginX, short marginY)
		{
			confidence = 0;

			if (bmpSource == null)
				return null;

			if (clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, bmpSource.Width, bmpSource.Height);
			else if (clip.Width <= 0 || clip.Height <= 0)
				clip = Rectangle.FromLTRB(clip.X, clip.Y, bmpSource.Width - clip.X * 2, bmpSource.Height - clip.Y * 2);

			try
			{
				Bitmap bwBitmap;

				if (bmpSource.PixelFormat != PixelFormat.Format1bppIndexed)
					bwBitmap = BinorizationThreshold.Binorize(bmpSource, clip, threshold.R, threshold.G, threshold.B);
				else
					bwBitmap = ImageCopier.Copy(bmpSource, clip);

#if SAVE_RESULTS
				bwBitmap.Save(Debug.SaveToDir + "CropAndDescewBinorized.png", ImageFormat.Png);
#endif

				if (removeGhostLines && bmpSource.PixelFormat != PixelFormat.Format1bppIndexed)
				{
					int[] ghostLines = GhostLinesRemoval.Get(bmpSource, lowThreshold, highThreshold, linesToCheck, maxDelta);

					for (int i = 0; i < ghostLines.Length; i++)
						ghostLines[i] -= clip.X;

					if (ghostLines.Length > 0)
						RemoveGhostLines(bwBitmap, ghostLines);
				}

				//bwBitmap.Save(@"C:\delete\results\b.png", ImageFormat.Png);
				/*NoiseReduction.Despeckle1x11bpp(bwData, imageRect);
				NoiseReduction.Despeckle2x21bpp(bwData, imageRect);
				NoiseReduction.Despeckle3x31bpp(bwData, imageRect);
				NoiseReduction.Despeckle4x41bpp(bwData, imageRect);
				NoiseReduction.Despeckle2x21bpp(bwData, imageRect);

				ObjectByCorners clip = FindClip(bwData, minAngleToDeskew);*/

				NoiseReduction.Despeckle(bwBitmap, NoiseReduction.DespeckleSize.Size6x6, NoiseReduction.DespeckleMode.WhiteSpecklesOnly, NoiseReduction.DespeckleMethod.Objects);

#if SAVE_RESULTS
				bwBitmap.Save(Debug.SaveToDir + "CropAndDescewBinorized.png", ImageFormat.Png);
#endif
				
				ObjectByCorners objectByCorners = FindObjectByCorners(bwBitmap, minAngleToDeskew);
				objectByCorners.Offset(clip.Location.X, clip.Location.Y);
				objectByCorners.Inflate(-marginX, -marginY);
				//bwBitmap.Save(@"C:\Users\jirka.stybnar\TestRun\CropAndDeskew2\results\CoverBW.tif", ImageFormat.Tiff);

#if SAVE_RESULTS
				bwBitmap.Save(Debug.SaveToDir + "CropAndDescewBinorized.png", ImageFormat.Png);
#endif

				if (objectByCorners.Inclined)
				{
					if (IsEntireClipInsideSource(bmpSource.Size, objectByCorners))
					{
						switch (objectByCorners.ValidCorners)
						{
							case 0: confidence = 0; break;
							case 1: confidence = 30; break;
							case 2:
							case 3: confidence = 80; break;
							default: confidence = 100; break;
						}
					}
					else
					{
						switch (objectByCorners.ValidCorners)
						{
							case 0: confidence = 0; break;
							case 1: confidence = 20; break;
							default: confidence = 50; break;
						}
					}
				}
				else
				{
					switch (objectByCorners.ValidCorners)
					{
						case 0: confidence = 0; break;
						case 1: confidence = 30; break;
						case 2:
						case 3: confidence = 80; break;
						default: confidence = 100; break;
					}
				}

				return objectByCorners;
			}
			catch (Exception ex)
			{
				string error = (bmpSource != null) ? "Bitmap: Exists, " : "Bitmap: null, ";

				error += (bmpSource != null) ? "Pixel Format: " + bmpSource.PixelFormat.ToString() : "";
				error += ", Threshold: " + threshold.ToString();
				error += ", Confidence: " + confidence.ToString();
				error += ", Angle: " + minAngleToDeskew.ToString();
				error += ", Clip: " + clip.ToString();
				error += ", Background black: " + backDark.ToString();
				error += ", Remove Ghost Lines: " + removeGhostLines.ToString();
				error += ", Low Threshold: " + lowThreshold.ToString();
				error += ", High Threshold: " + highThreshold.ToString();
				error += ", Lines to check: " + linesToCheck.ToString();
				error += ", Max Delta: " + maxDelta.ToString();
				error += ", Exception: " + ex.Message;

				throw new Exception("CropAndDeskew, GetParams(): " + error + "\n");
			}
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region CropAndDeskewDir()
		private static int CropAndDeskewDir(DirectoryInfo sourceDir, DirectoryInfo destDir, Color color, 
			short jpegCompression, float minAngleToDeskew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, short marginX, short marginY, int flags)
		{
			ArrayList	sources = new ArrayList(); 
			Bitmap		bitmap;
			TimeSpan	span = new TimeSpan(0);
#if DEBUG
			DateTime totalTimeStart = DateTime.Now;
#endif
			byte		confidence = 0;

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
				DateTime start = DateTime.Now;
#endif
				Bitmap result = Go(bitmap, color, true, out confidence, minAngleToDeskew, clip, removeGhostLines, lowThreshold, highThreshold, linesToCheck, maxDelta, marginX, marginY, flags);
#if DEBUG
				TimeSpan time = DateTime.Now.Subtract(start);
				span = span.Add(time);
#endif
				ImageCodecInfo		codecInfo = Encoding.GetCodecInfo(bitmap);
				EncoderParameters	encoderParams = Encoding.GetEncoderParams(bitmap, jpegCompression) ;

#if DEBUG
				Console.WriteLine(string.Format("{0}: {1}, Confidence:{2}%",  file.FullName, time.ToString(), confidence)) ;
#endif
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
			return confidence;
		}
		#endregion

		#region CropAndDeskewFile()
		private static int CropAndDeskewFile(FileInfo sourceFile, FileInfo resultFile, Color color, 
			short jpegCompression, float minAngleToDeskew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, short marginX, short marginY, int flags)
		{			
			byte			confidence;
			Bitmap			bitmap = new Bitmap(sourceFile.FullName) ;

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			Bitmap		result = Go(bitmap, color, true, out confidence, minAngleToDeskew, clip, removeGhostLines, lowThreshold, 
				highThreshold, linesToCheck, maxDelta, marginX, marginY, flags);

#if DEBUG
			TimeSpan time = DateTime.Now.Subtract(start);
			Console.WriteLine(string.Format("{0}: {1}, Confidence:{2}%",  sourceFile.FullName, time.ToString(), confidence)) ;
#endif
				
			ImageCodecInfo		codecInfo = Encoding.GetCodecInfo(bitmap);
			EncoderParameters	encoderParams = Encoding.GetEncoderParams(bitmap, jpegCompression) ;
				
			bitmap.Dispose() ;

			if(resultFile.Exists)
				resultFile.Delete();
				
			result.Save(resultFile.FullName, codecInfo, encoderParams) ;

			result.Dispose() ;
			return confidence;
		}
		#endregion

		#region CropAndDeskew32bpp()
		private static Bitmap CropAndDeskew32bpp(Bitmap sourceBmp, ObjectByCorners clip, int flags)
		{
			Bitmap resultBmp = null;

			if (IsEntireClipInsideSource(sourceBmp.Size, clip))
			{
				if ((flags & 1) == 0)
					resultBmp = GetClip32bpp(sourceBmp, clip);
				else
					resultBmp = GetClip32bppQuality(sourceBmp, clip);
			}
			else
			{
				if ((flags & 1) == 0)
					resultBmp = GetClipCheckBorders32bpp(sourceBmp, clip);
				else
					resultBmp = GetClipCheckBorders32bppQuality(sourceBmp, clip);
			}

			return resultBmp;
		}
		#endregion

		#region CropAndDeskew24bpp()
		private static Bitmap CropAndDeskew24bpp(Bitmap sourceBmp, ObjectByCorners clip, int flags)
		{
			Bitmap resultBmp = null;

			if (IsEntireClipInsideSource(sourceBmp.Size, clip))
			{
				if ((flags & 1) == 0)
					resultBmp = GetClip24bpp(sourceBmp, clip);
				else
					resultBmp = GetClip24bppQuality(sourceBmp, clip);
			}
			else
			{
				if ((flags & 1) == 0)
					resultBmp = GetClipCheckBorders24bpp(sourceBmp, clip);
				else
					resultBmp = GetClipCheckBorders24bppQuality(sourceBmp, clip);
			}

			return resultBmp;
		}
		#endregion

		#region CropAndDeskew8bpp()
		private static Bitmap CropAndDeskew8bpp(Bitmap sourceBmp, ObjectByCorners clip, int flags)
		{
			Bitmap resultBmp = null;

			if (IsEntireClipInsideSource(sourceBmp.Size, clip))
			{
				if ((flags & 1) == 0)
					resultBmp = GetClip8bpp(sourceBmp, clip);
				else
					resultBmp = GetClip8bppQuality(sourceBmp, clip);
			}
			else
			{
				if ((flags & 1) == 0)
					resultBmp = GetClipCheckBorders8bpp(sourceBmp, clip);
				else
					resultBmp = GetClipCheckBorders8bppQuality(sourceBmp, clip);
			}

			resultBmp.Palette = sourceBmp.Palette;
			return resultBmp;
		}
		#endregion

		#region CropAndDeskew1bpp()
		private static Bitmap CropAndDeskew1bpp(Bitmap sourceBmp, ObjectByCorners clip)
		{
			Bitmap resultBmp = null;

			resultBmp = GetClipCheckBorders1bpp(sourceBmp, clip);

			resultBmp.Palette = sourceBmp.Palette;
			return resultBmp;
		}
		#endregion

		#region FindObjectByCorners()
		private static ObjectByCorners FindObjectByCorners(Bitmap bitmap, float minAngleToDeskew)
		{
			BitmapData bmpData = null;
			try
			{
				bmpData = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				return FindObjectByCorners(bmpData, minAngleToDeskew);
			}
			finally
			{
				if (bmpData != null)
					bitmap.UnlockBits(bmpData);
			}
		}

		private static ObjectByCorners FindObjectByCorners(BitmapData bmpData, float minAngleToDeskew)
		{
			Crop		clip = Crop.GetCrop(bmpData);
			Rectangle	rect = clip.TangentialRectangle;
			rect.Inflate(8, 8);
			rect.X = Math.Max(0, rect.X);
			rect.Y = Math.Max(0, rect.Y);
			rect.Width = Math.Min(bmpData.Width - rect.X, rect.Width);
			rect.Height = Math.Min(bmpData.Height - rect.Y, rect.Height);

			Point		ulCorner = FindUlCorner(bmpData, rect.Location);
			Point		urCorner = FindUrCorner(bmpData, new Point(rect.Right, rect.Top));
			Point		llCorner = FindLlCorner(bmpData, new Point(rect.Left, rect.Bottom));
			Point		lrCorner = FindLrCorner(bmpData, new Point(rect.Right, rect.Bottom));

			ObjectByCorners crop = new ObjectByCorners(bmpData, ulCorner, urCorner, llCorner, lrCorner, clip, minAngleToDeskew);

			return crop;
		}
		#endregion
		
		#region FindSmallestClip()
		/*private static Crop FindSmallestClip(BitmapData bmpData)
		{
			Point		pL = new Point(0, bmpData.Height / 2);
			Point		pT = new Point(bmpData.Width / 2, 0); 
			Point		pR = new Point(bmpData.Width, bmpData.Height / 2); 
			Point		pB = new Point(bmpData.Width / 2, bmpData.Height);
			int			width = bmpData.Width;
			int			height = bmpData.Height;			
			int			stride = bmpData.Stride; 
			int			x, y;

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte*	pCurrent ;

				//top
				for (y = 0; y < height; y++)
				{
					pCurrent = pSource + (y * stride);

					for (x = 0; x < width; x++)
					{
						pCurrent = pSource + y * stride + (x >> 3);

						if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
						{
							if ((x < 3) || (x >= width - 3) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) || 
								(pCurrent[3] != 0) || (pCurrent[-3*stride] != 0) ||(pCurrent[3*stride] != 0))
							{
								pT = new Point(x, y);
								y = height;
								break;
							}
						}
					}
				}

				//bottom
				for(y = height - 1; y > pT.Y; y--) 
				{ 
					pCurrent = pSource + (y * stride);
					
					for(x = 0; x < width; x++)
					{
						pCurrent = pSource + y * stride + (x >> 3);

						if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
						{
							if ((x < 3) || (x >= width - 3) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
								(pCurrent[3] != 0) || (pCurrent[-3 * stride] != 0) || (pCurrent[3 * stride] != 0))
							{
								pB = new Point(x, y+1);
								y = -1;
								break;
							}
						}
					}
				}

				int		bottom = (height - 8 < pB.Y) ? height - 8 : pB.Y;
				//left
				for(x = 0; x < width; x++) 
				{ 					
					for(y = pT.Y; y < bottom; y++)
					{
						pCurrent = pSource + y * stride + (x >> 3);

						if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
						{
							if ((x < 3) || (x >= width - 3) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
								(pCurrent[3] != 0) || (pCurrent[-3 * stride] != 0) || (pCurrent[3 * stride] != 0))
							{
								pL = new Point(x, y);
								x = width;
								break;
							}
						}
					}
				}

				//right
				for(x = width - 1; x > pL.X; x--) 
				{ 					
					for(y = pT.Y; y < bottom; y++)
					{
						pCurrent = pSource + y * stride + (x >> 3);

						if ((*pCurrent & (byte)(0x80 >> (x & 0x7))) > 0)
						{
							if ((x < 3) || (x >= width - 3) || (y < 3) || (y >= height - 3) || (pCurrent[-3] != 0) ||
								(pCurrent[3] != 0) || (pCurrent[-3 * stride] != 0) || (pCurrent[3 * stride] != 0))
							{
								pR = new Point(x+1, y);
								x = -1;
								break;
							}
						}
					}
				}
	
				return new Crop(pL, pT, pR, pB); 
			}
		}*/
		#endregion

		#region FindSmallestClip()
		/*private static Crop FindSmallestClip(BitmapData bmpData, double angle)
		{
			int			stride = bmpData.Stride; 

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 

				//top
				Point pT = FindTopEdge(pSource, stride, new Size(bmpData.Width, bmpData.Height), angle);
				Point pB = FindBottomEdge(pSource, stride, new Size(bmpData.Width, bmpData.Height), angle);
				Point pL = FindLeftEdge(pSource, stride, new Size(bmpData.Width, bmpData.Height), angle);
				Point pR = FindRightEdge(pSource, stride, new Size(bmpData.Width, bmpData.Height), angle);
	
				return new Crop(pL, pT, pR, pB); 
			}
		}

		private static Crop FindSmallestClip(int[,] array, double angle)
		{
			//top
			Point pT = FindTopEdge(array, angle);
			Point pB = FindBottomEdge(array, angle);
			Point pL = FindLeftEdge(array, angle);
			Point pR = FindRightEdge(array, angle);

			return new Crop(pL, pT, pR, pB);
		}*/
		#endregion

		#region IsClipDeskewed()
		private static bool IsClipDeskewed(BitmapData bmpData, Crop clip)
		{
			return false;			
		}
		#endregion

		#region FindUlCorner()
		private static Point FindUlCorner(BitmapData bmpData, Point startPoint)
		{
			int			maxSteps = Math.Min(bmpData.Width - startPoint.X, bmpData.Height - startPoint.Y);
			int			stride = bmpData.Stride; 
			int			x, y;

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte	mask;
				int		i = 0, j = 0;

				while(i < maxSteps)
				{

					for(j = 0; j <= i; j++)
					{
						x = startPoint.X + j;
						y = startPoint.Y + (i - j);

						mask = (byte) (0x80 >> (x % 8));
						if( (x >= 0) && (y >= 0) && (x < bmpData.Width - 8) && (y < bmpData.Height) && (*(pSource + (y * stride) + (x >> 3)) & mask) > 0)
							return new Point(x, y);
					}

					i++;
				}
			}
			
			return startPoint; 
		}
		#endregion

		#region FindUrCorner()
		private static Point FindUrCorner(BitmapData bmpData, Point startPoint)
		{
			int			maxSteps = Math.Min(startPoint.X, bmpData.Height - startPoint.Y);
			int			stride = bmpData.Stride; 
			int			x, y;

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte	mask;
				int		i = 0, j = 0;

				while(i < maxSteps)
				{

					for(j = 0; j <= i; j++)
					{
						x = startPoint.X - j;
						y = startPoint.Y + (i - j);

						mask = (byte) (0x80 >> (x % 8));
						if ((x >= 0) && (y >= 0) && (x < bmpData.Width) && (y < bmpData.Height) && (pSource[(y * stride) + (x >> 3)] & mask) > 0)
							return new Point(x, y);
					}

					i++;
				}
			}
			
			return startPoint; 
		}
		#endregion

		#region FindLlCorner()
		private static Point FindLlCorner(BitmapData bmpData, Point startPoint)
		{
			int			maxSteps = Math.Min(bmpData.Width - startPoint.X, startPoint.Y);
			int			stride = bmpData.Stride; 
			int			x, y;

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte	mask;
				int		i = 0, j = 0;

				while(i < maxSteps)
				{

					for(j = 0; j <= i; j++)
					{
						x = startPoint.X + j;
						y = startPoint.Y - (i - j);

						mask = (byte) (0x80 >> (x % 8));
						if( (x >= 0) && (y >= 0) && (x < bmpData.Width - 8) && (y < bmpData.Height) && (*(pSource + (y * stride) + (x >> 3)) & mask) > 0)
							return new Point(x, y);
					}

					i++;
				}
			}
			
			return startPoint; 
		}
		#endregion

		#region FindLrCorner()
		private static Point FindLrCorner(BitmapData bmpData, Point startPoint)
		{
			int			maxSteps = Math.Min(startPoint.X, startPoint.Y);
			int			stride = bmpData.Stride; 
			int			x, y;

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte	mask;
				int		i = 0, j = 0;

				while(i < maxSteps)
				{

					for(j = 0; j <= i; j++)
					{
						x = startPoint.X - j;
						y = startPoint.Y - (i - j);

						mask = (byte) (0x80 >> (x % 8));
						if ((x >= 0) && (y >= 0) && (x < bmpData.Width) && (y < bmpData.Height) && (pSource[(y * stride) + (x >> 3)] & mask) > 0)
							return new Point(x, y);
					}

					i++;
				}
			}
			
			return startPoint; 
		}
		#endregion

		#region GetClip32bpp()
		private static Bitmap GetClip32bpp(Bitmap source, ObjectByCorners clip)
		{
			int x, y;
			int width = clip.Width;
			int height = clip.Height;
			int ulCornerX = clip.UlCorner.X;
			int ulCornerY = clip.UlCorner.Y;

			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					int sourceXOffset = ulCornerX;
					int sourceYOffset = clip.UlCorner.Y;
					int currentXOffset = sourceXOffset;
					int currentYOffset = sourceYOffset;
					double xJump = (clip.LlCorner.X - ulCornerX) / (double)clip.Height;
					double yJump = Math.Tan(clip.Skew);
					int yTmp;

					byte* pOrigCurrent;
					byte* pCopyCurrent;

					for (y = 0; y < height; y++)
					{
						currentXOffset = (int)(y * xJump);
						pOrigCurrent = pSource + (y + ulCornerY) * sStride + (ulCornerX + currentXOffset) * 4;
						pCopyCurrent = pResult + y * rStride;

						for (x = 0; x < width; x++)
						{
							*(pCopyCurrent++) = *(pOrigCurrent++);
							*(pCopyCurrent++) = *(pOrigCurrent++);
							*(pCopyCurrent++) = *(pOrigCurrent++);
							*(pCopyCurrent++) = *(pOrigCurrent++);

							yTmp = Math.Max(0, (y + ulCornerY + ((int)(yJump * x))));
							pOrigCurrent = pSource + yTmp * sStride + (ulCornerX + currentXOffset + x) * 4;
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region GetClip32bppQuality()
		private static Bitmap GetClip32bppQuality(Bitmap source, ObjectByCorners clip)
		{
			int x, y;
			int width = clip.Width;
			int height = clip.Height;
			int ulCornerX = clip.UlCorner.X;
			int ulCornerY = clip.UlCorner.Y;

			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
			result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
			resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					double sourceX;
					double sourceY;
					double xJump = (clip.LlCorner.X - ulCornerX) / (double)clip.Height;
					double yJump = (Math.Tan(clip.Skew));
					double xRest, yRest;

					byte* pOrigCurrent;
					byte* pCopyCurrent;

					for (y = 0; y < height; y++)
					{
						sourceX = ulCornerX + y * xJump;
						sourceY = ulCornerY + y;

						xRest = sourceX - (int)sourceX;
						if (xRest < 0)
							xRest += 1;

						if (xRest < 0.000001)
							xRest = 0;
						if (xRest > .999999)
						{
							sourceX = (int)sourceX + 1;
							xRest = 0;
						}

						pCopyCurrent = pResult + y * rStride;

						for (x = 0; x < width; x++)
						{
							yRest = sourceY - (int)sourceY;
							if (yRest < 0)
								yRest += 1;

							if (yRest < 0.000001)
								yRest = 0;
							if (yRest > .999999)
							{
								sourceY = (int)sourceY + 1;
								yRest = 0;
							}

							pOrigCurrent = pSource + (int)sourceY * sStride + ((int)sourceX) * 4;

							if (xRest == 0)
							{
								if (yRest == 0)
								{
									*(pCopyCurrent++) = *(pOrigCurrent++);
									*(pCopyCurrent++) = *(pOrigCurrent++);
									*(pCopyCurrent++) = *(pOrigCurrent++);
									*(pCopyCurrent++) = *(pOrigCurrent++);
								}
								else
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
									pOrigCurrent++;
								}
							}
							else
							{
								if (yRest == 0)
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - xRest) + pOrigCurrent[4] * xRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - xRest) + pOrigCurrent[4] * xRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - xRest) + pOrigCurrent[4] * xRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - xRest) + pOrigCurrent[4] * xRest);
									pOrigCurrent++;
								}
								else
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) * (1 - xRest) +
										pOrigCurrent[4] * xRest * (1 - yRest) +
										pOrigCurrent[sStride] * (1 - xRest) * yRest +
										pOrigCurrent[sStride + 4] * xRest * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) * (1 - xRest) +
										pOrigCurrent[4] * xRest * (1 - yRest) +
										pOrigCurrent[sStride] * (1 - xRest) * yRest +
										pOrigCurrent[sStride + 4] * xRest * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) * (1 - xRest) +
										pOrigCurrent[4] * xRest * (1 - yRest) +
										pOrigCurrent[sStride] * (1 - xRest) * yRest +
										pOrigCurrent[sStride + 4] * xRest * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) * (1 - xRest) +
										pOrigCurrent[4] * xRest * (1 - yRest) +
										pOrigCurrent[sStride] * (1 - xRest) * yRest +
										pOrigCurrent[sStride + 4] * xRest * yRest);
									pOrigCurrent++;
								}
							}

							sourceX += 1;
							sourceY += yJump;
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region GetClip24bpp()
		private static Bitmap GetClip24bpp(Bitmap source, ObjectByCorners clip)
		{
			int			x, y;
			int			width = clip.Width;
			int			height = clip.Height;			
			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			Bitmap		result = null;
			BitmapData	resultData = null;
			BitmapData	sourceData = null;
			
			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				
				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					int		sourceXOffset = ulCornerX;
					int		sourceYOffset = clip.UlCorner.Y;
					int		currentXOffset = sourceXOffset;
					int		currentYOffset = sourceYOffset;
					double	xJump =  (clip.LlCorner.X - ulCornerX) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Skew));
					int		yTmp;
				
					byte*	pOrigCurrent;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						currentXOffset = (int) (y * xJump);
						pOrigCurrent = pSource + (y + ulCornerY) * sStride + (ulCornerX + currentXOffset) * 3;
						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							*(pCopyCurrent++) = *(pOrigCurrent++);
							*(pCopyCurrent++) = *(pOrigCurrent++);
							*(pCopyCurrent++) = *(pOrigCurrent++);

							yTmp = Math.Max(0, (y + ulCornerY + ((int) (yJump * x))));
							pOrigCurrent = pSource + yTmp * sStride + (ulCornerX + currentXOffset + x) * 3;
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if(result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion
		
		#region GetClip24bppQuality()
		private static Bitmap GetClip24bppQuality(Bitmap source, ObjectByCorners clip)
		{
			int x, y;
			int width = clip.Width;
			int height = clip.Height;
			int ulCornerX = clip.UlCorner.X;
			int ulCornerY = clip.UlCorner.Y;

			Bitmap result = null;
			BitmapData resultData = null;
			BitmapData sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					double sourceX;
					double sourceY;
					double xJump = (clip.LlCorner.X - ulCornerX) / (double)clip.Height;
					double yJump = (Math.Tan(clip.Skew));
					double xRest, yRest;

					byte* pOrigCurrent;
					byte* pCopyCurrent;

					for (y = 0; y < height; y++)
					{
						sourceX = ulCornerX + y * xJump;
						sourceY = ulCornerY + y;

						xRest = sourceX - (int)sourceX;
						if (xRest < 0)
							xRest += 1;

						if (xRest < 0.000001)
							xRest = 0;
						if (xRest > .999999)
						{
							sourceX = (int)sourceX + 1;
							xRest = 0;
						}

						pCopyCurrent = pResult + y * rStride;

						for (x = 0; x < width; x++)
						{
							yRest = sourceY - (int)sourceY;
							if (yRest < 0)
								yRest += 1;

							if (yRest < 0.000001)
								yRest = 0;
							if (yRest > .999999)
							{
								sourceY = (int)sourceY + 1;
								yRest = 0;
							}

							pOrigCurrent = pSource + (int)sourceY * sStride + ((int)sourceX) * 3;

							if (xRest == 0)
							{
								if (yRest == 0)
								{
									*(pCopyCurrent++) = *(pOrigCurrent++);
									*(pCopyCurrent++) = *(pOrigCurrent++);
									*(pCopyCurrent++) = *(pOrigCurrent++);
								}
								else
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
									pOrigCurrent++;
								}
							}
							else
							{
								if (yRest == 0)
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - xRest) + pOrigCurrent[3] * xRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - xRest) + pOrigCurrent[3] * xRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - xRest) + pOrigCurrent[3] * xRest);
									pOrigCurrent++;
								}
								else
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) * (1 - xRest) +
										pOrigCurrent[3] * xRest * (1 - yRest) +
										pOrigCurrent[sStride] * (1 - xRest) * yRest +
										pOrigCurrent[sStride + 3] * xRest * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) * (1 - xRest) +
										pOrigCurrent[3] * xRest * (1 - yRest) +
										pOrigCurrent[sStride] * (1 - xRest) * yRest +
										pOrigCurrent[sStride + 3] * xRest * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) * (1 - xRest) +
										pOrigCurrent[3] * xRest * (1 - yRest) +
										pOrigCurrent[sStride] * (1 - xRest) * yRest +
										pOrigCurrent[sStride + 3] * xRest * yRest);
									pOrigCurrent++;
								}
							}

							sourceX += 1;
							sourceY += yJump;
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion
		
		#region GetClip8bpp()
		private static Bitmap GetClip8bpp(Bitmap source, ObjectByCorners clip)
		{
			int x, y;
			int width = clip.Width;
			int height = clip.Height;
			int ulCornerX = clip.UlCorner.X;
			int ulCornerY = clip.UlCorner.Y;

			Bitmap result = null;
			BitmapData resultData = null;
			BitmapData sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					int sourceXOffset = ulCornerX;
					int sourceYOffset = ulCornerY;
					int currentXOffset = sourceXOffset;
					int currentYOffset = sourceYOffset;
					double xJump = (clip.LlCorner.X - ulCornerX) / (double)clip.Height;
					double yJump = (Math.Tan(clip.Skew));
					int yTmp;

					byte* pOrigCurrent;
					byte* pCopyCurrent;

					for (y = 0; y < height; y++)
					{
						currentXOffset = (int)(y * xJump);
						pOrigCurrent = pSource + (y + ulCornerY) * sStride + (ulCornerX + currentXOffset);
						pCopyCurrent = pResult + y * rStride;

						for (x = 0; x < width; x++)
						{
							*(pCopyCurrent++) = *(pOrigCurrent++);

							yTmp = y + ulCornerY + ((int)(yJump * x));
							pOrigCurrent = pSource + yTmp * sStride + (ulCornerX + currentXOffset + x);
						}

					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region GetClip8bppQuality()
		private static Bitmap GetClip8bppQuality(Bitmap source, ObjectByCorners clip)
		{
			int x, y;
			int width = clip.Width;
			int height = clip.Height;
			int ulCornerX = clip.UlCorner.X;
			int ulCornerY = clip.UlCorner.Y;

			Bitmap result = null;
			BitmapData resultData = null;
			BitmapData sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					double sourceX;
					double sourceY;
					double xJump = (clip.LlCorner.X - ulCornerX) / (double)clip.Height;
					double yJump = (Math.Tan(clip.Skew));
					double xRest, yRest;

					byte* pOrigCurrent;
					byte* pCopyCurrent;

					for (y = 0; y < height; y++)
					{
						sourceX = ulCornerX + y * xJump;
						sourceY = ulCornerY + y;

						xRest = sourceX - (int)sourceX;
						if (xRest < 0)
							xRest += 1;

						if (xRest < 0.000001)
							xRest = 0;
						if (xRest > .999999)
						{
							sourceX = (int)sourceX + 1;
							xRest = 0;
						}

						pCopyCurrent = pResult + y * rStride;

						for (x = 0; x < width; x++)
						{
							yRest = sourceY - (int)sourceY;
							if (yRest < 0)
								yRest += 1;

							if (yRest < 0.000001)
								yRest = 0;
							if (yRest > .999999)
							{
								sourceY = (int)sourceY + 1;
								yRest = 0;
							}

							pOrigCurrent = pSource + (int)sourceY * sStride + (int)sourceX;

							if (xRest == 0)
							{
								if (yRest == 0)
								{
									*(pCopyCurrent++) = *pOrigCurrent;
								}
								else
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
								}
							}
							else
							{
								if (yRest == 0)
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - xRest) + pOrigCurrent[1] * xRest);
								}
								else
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) * (1 - xRest) +
										pOrigCurrent[1] * xRest * (1 - yRest) +
										pOrigCurrent[sStride] * (1 - xRest) * yRest +
										pOrigCurrent[sStride + 1] * xRest * yRest);
								}
							}

							sourceX += 1;
							sourceY += yJump;
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region GetClipCheckBorders32bpp()
		private static Bitmap GetClipCheckBorders32bpp(Bitmap source, ObjectByCorners clip)
		{
			int x, y;
			int width = clip.Width;
			int height = clip.Height;
			int ulCornerX = clip.UlCorner.X;
			int ulCornerY = clip.UlCorner.Y;

			Bitmap result = null;
			BitmapData resultData = null;
			BitmapData sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
			
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;

			int sourceWidth = sourceData.Width;
			int sourceHeight = sourceData.Height;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					int sourceXOffset = ulCornerX;
					int sourceYOffset = ulCornerY;
					int currentXOffset = sourceXOffset;
					int currentYOffset = sourceYOffset;
					double xJump = (clip.LlCorner.X - clip.UlCorner.X) / (double)clip.Height;
					double yJump = (Math.Tan(clip.Skew));
					int yTmp, xTmp;
					bool canReadFromSource;

					byte* pOrigCurrent = pSource;
					byte* pCopyCurrent;

					for (y = 0; y < height; y++)
					{
						currentXOffset = (int)(y * xJump);

						yTmp = y + ulCornerY;
						xTmp = ulCornerX + currentXOffset;

						if ((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight))
						{
							pOrigCurrent = pSource + yTmp * sStride + xTmp * 3;
							canReadFromSource = true;
						}
						else
							canReadFromSource = false;

						pCopyCurrent = pResult + y * rStride;

						for (x = 0; x < width; x++)
						{
							if (canReadFromSource)
							{
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
							}
							else
								pCopyCurrent += 4;

							yTmp = y + ulCornerY + ((int)(yJump * x));
							xTmp = ulCornerX + currentXOffset + x;
							//pOrigCurrent = pSource + yTmp * sStride + (clip.UlCorner.X + currentXOffset + x) * 3;

							if ((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight))
							{
								pOrigCurrent = pSource + yTmp * sStride + xTmp * 4;
								canReadFromSource = true;
							}
							else
							{
								canReadFromSource = false;
							}
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region GetClipCheckBorders32bppQuality()
		private static Bitmap GetClipCheckBorders32bppQuality(Bitmap source, ObjectByCorners clip)
		{
			int x, y;
			int width = clip.Width;
			int height = clip.Height;
			int ulCornerX = clip.UlCorner.X;
			int ulCornerY = clip.UlCorner.Y;

			Bitmap result = null;
			BitmapData resultData = null;
			BitmapData sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				
			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					double sourceX;
					double sourceY;
					double xJump = (clip.LlCorner.X - ulCornerX) / (double)clip.Height;
					double yJump = (Math.Tan(clip.Skew));
					double xRest, yRest;

					byte* pOrigCurrent;
					byte* pCopyCurrent;

					for (y = 0; y < height; y++)
					{
						sourceX = ulCornerX + y * xJump;
						sourceY = ulCornerY + y;

						xRest = sourceX - (int)sourceX;
						if (xRest < 0)
							xRest += 1;

						if (xRest < 0.000001)
							xRest = 0;
						if (xRest > .999999)
						{
							sourceX = (int)sourceX + 1;
							xRest = 0;
						}

						pCopyCurrent = pResult + y * rStride;

						for (x = 0; x < width; x++)
						{
							if (sourceX >= 0 && sourceX < sourceData.Width && sourceY >= 0 && sourceY <= sourceData.Height - 1)
							{
								yRest = sourceY - (int)sourceY;
								if (yRest < 0)
									yRest += 1;

								if (yRest < 0.000001)
									yRest = 0;
								if (yRest > .999999)
								{
									sourceY = (int)sourceY + 1;
									yRest = 0;
								}

								pOrigCurrent = pSource + (int)sourceY * sStride + ((int)sourceX) * 4;

								if (xRest == 0 || sourceX >= sourceData.Width - 1)
								{
									if (yRest == 0 || sourceY >= sourceData.Height - 1)
									{
										*(pCopyCurrent++) = *(pOrigCurrent++);
										*(pCopyCurrent++) = *(pOrigCurrent++);
										*(pCopyCurrent++) = *(pOrigCurrent++);
										*(pCopyCurrent++) = *(pOrigCurrent++);
									}
									else
									{
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
										pOrigCurrent++;
									}
								}
								else
								{
									if (yRest == 0 || sourceY >= sourceData.Height - 1)
									{
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - xRest) + pOrigCurrent[4] * xRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - xRest) + pOrigCurrent[4] * xRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - xRest) + pOrigCurrent[4] * xRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - xRest) + pOrigCurrent[4] * xRest);
										pOrigCurrent++;
									}
									else
									{
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) * (1 - xRest) +
											pOrigCurrent[4] * xRest * (1 - yRest) +
											pOrigCurrent[sStride] * (1 - xRest) * yRest +
											pOrigCurrent[sStride + 4] * xRest * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) * (1 - xRest) +
											pOrigCurrent[4] * xRest * (1 - yRest) +
											pOrigCurrent[sStride] * (1 - xRest) * yRest +
											pOrigCurrent[sStride + 4] * xRest * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) * (1 - xRest) +
											pOrigCurrent[4] * xRest * (1 - yRest) +
											pOrigCurrent[sStride] * (1 - xRest) * yRest +
											pOrigCurrent[sStride + 4] * xRest * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) * (1 - xRest) +
											pOrigCurrent[4] * xRest * (1 - yRest) +
											pOrigCurrent[sStride] * (1 - xRest) * yRest +
											pOrigCurrent[sStride + 4] * xRest * yRest);
										pOrigCurrent++;
									}
								}
							}
							else
							{
								pCopyCurrent += 4;
							}

							sourceX += 1;
							sourceY += yJump;
						}

					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region GetClipCheckBorders24bpp()
		private static Bitmap GetClipCheckBorders24bpp(Bitmap source, ObjectByCorners clip)
		{
			int x, y;
			int width = clip.Width;
			int height = clip.Height;
			int ulCornerX = clip.UlCorner.X;
			int ulCornerY = clip.UlCorner.Y;

			Bitmap result = null;
			BitmapData resultData = null;
			BitmapData sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;

				int sourceWidth = sourceData.Width;
				int sourceHeight = sourceData.Height;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					int sourceXOffset = ulCornerX;
					int sourceYOffset = ulCornerY;
					int currentXOffset = sourceXOffset;
					int currentYOffset = sourceYOffset;
					double xJump = (clip.LlCorner.X - clip.UlCorner.X) / (double)clip.Height;
					double yJump = (Math.Tan(clip.Skew));
					int yTmp, xTmp;
					bool canReadFromSource;

					byte* pOrigCurrent = pSource;
					byte* pCopyCurrent;

					for (y = 0; y < height; y++)
					{
						currentXOffset = (int)(y * xJump);

						yTmp = y + ulCornerY;
						xTmp = ulCornerX + currentXOffset;

						if ((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight))
						{
							pOrigCurrent = pSource + yTmp * sStride + xTmp * 3;
							canReadFromSource = true;
						}
						else
							canReadFromSource = false;

						pCopyCurrent = pResult + y * rStride;

						for (x = 0; x < width; x++)
						{
							if (canReadFromSource)
							{
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
							}
							else
								pCopyCurrent += 3;

							yTmp = y + ulCornerY + ((int)(yJump * x));
							xTmp = ulCornerX + currentXOffset + x;
							//pOrigCurrent = pSource + yTmp * sStride + (clip.UlCorner.X + currentXOffset + x) * 3;

							if ((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight))
							{
								pOrigCurrent = pSource + yTmp * sStride + xTmp * 3;
								canReadFromSource = true;
							}
							else
							{
								canReadFromSource = false;
							}
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region GetClipCheckBorders24bppQuality()
		private static Bitmap GetClipCheckBorders24bppQuality(Bitmap source, ObjectByCorners clip)
		{
			int			x, y;
			int			width = clip.Width;
			int			height = clip.Height;
			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			Bitmap result = null;
			BitmapData resultData = null;
			BitmapData sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
			
				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride;

				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					double	sourceX;
					double	sourceY;
					double	xJump =  (clip.LlCorner.X - ulCornerX) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Skew));
					double	xRest, yRest;
				
					byte*	pOrigCurrent;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						sourceX = ulCornerX + y * xJump;
						sourceY = ulCornerY + y;

						xRest = sourceX - (int) sourceX;
						if(xRest < 0)
							xRest += 1;

						if(xRest < 0.000001)
							xRest = 0;
						if(xRest > .999999)
						{
							sourceX = (int) sourceX + 1;
							xRest = 0;
						}

						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							if(sourceX >= 0 && sourceX < sourceData.Width && sourceY >= 0 && sourceY <= sourceData.Height - 1)
							{
								yRest = sourceY - (int) sourceY;
								if(yRest < 0)
									yRest += 1;

								if(yRest < 0.000001)
									yRest = 0;
								if(yRest > .999999)
								{
									sourceY = (int) sourceY + 1;
									yRest = 0;
								}
							
								pOrigCurrent = pSource + (int) sourceY * sStride + ((int) sourceX) * 3;

								if (xRest == 0 || sourceX >= sourceData.Width - 1)
								{
									if (yRest == 0 || sourceY >= sourceData.Height - 1)
									{
										*(pCopyCurrent++) = *(pOrigCurrent++);
										*(pCopyCurrent++) = *(pOrigCurrent++);
										*(pCopyCurrent++) = *(pOrigCurrent++);
									}
									else
									{
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
										pOrigCurrent++;
									}
								}
								else
								{
									if (yRest == 0 || sourceY >= sourceData.Height - 1)
									{
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-xRest) + pOrigCurrent[3] * xRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-xRest) + pOrigCurrent[3] * xRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-xRest) + pOrigCurrent[3] * xRest);
										pOrigCurrent++;
									}
									else
									{
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-yRest) * (1-xRest) + 
											pOrigCurrent[3] * xRest * (1-yRest) +
											pOrigCurrent[sStride] * (1-xRest) * yRest + 
											pOrigCurrent[sStride+3] * xRest * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-yRest) * (1-xRest) + 
											pOrigCurrent[3] * xRest * (1-yRest) +
											pOrigCurrent[sStride] * (1-xRest) * yRest + 
											pOrigCurrent[sStride+3] * xRest * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-yRest) * (1-xRest) + 
											pOrigCurrent[3] * xRest * (1-yRest) +
											pOrigCurrent[sStride] * (1-xRest) * yRest + 
											pOrigCurrent[sStride+3] * xRest * yRest);
										pOrigCurrent++;
									}
								}
							}
							else
							{
								pCopyCurrent += 3;
							}
							
							sourceX += 1;
							sourceY += yJump;
						}

					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region GetClipCheckBorders8bpp()
		private static Bitmap GetClipCheckBorders8bpp(Bitmap source, ObjectByCorners clip)
		{
			int			x, y;
			int			width = clip.Width;
			int			height = clip.Height;
			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			Bitmap result = null;
			BitmapData resultData = null;
			BitmapData sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;

			int			sourceWidth = sourceData.Width;
			int			sourceHeight = sourceData.Height;

				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					int		sourceXOffset = clip.UlCorner.X;
					int		sourceYOffset = clip.UlCorner.Y;
					int		currentXOffset = sourceXOffset;
					int		currentYOffset = sourceYOffset;
					double	xJump =  (clip.LlCorner.X - clip.UlCorner.X) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Skew));
					int		yTmp, xTmp;
					bool	canReadFromSource;
				
					byte*	pOrigCurrent = pSource;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						currentXOffset = (int) (y * xJump);
						
						yTmp = y + ulCornerY;
						xTmp = ulCornerX + currentXOffset;
						
						if((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight)) 
						{
							pOrigCurrent = pSource + yTmp * sStride + xTmp;
							canReadFromSource = true;
						}
						else
							canReadFromSource = false;

						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							if(canReadFromSource)
								*(pCopyCurrent++) = *(pOrigCurrent++);
							else
								pCopyCurrent++;

							yTmp = y + ulCornerY + ((int) (yJump * x));
							xTmp = ulCornerX + currentXOffset + x;
							//pOrigCurrent = pSource + yTmp * sStride + (clip.UlCorner.X + currentXOffset + x) * 3;

							if((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight))
							{
								pOrigCurrent = pSource + yTmp * sStride + xTmp;
								canReadFromSource = true;
							}
							else
							{
								canReadFromSource = false;
							}
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region GetClipCheckBorders8bppQuality()
		private static Bitmap GetClipCheckBorders8bppQuality(Bitmap source, ObjectByCorners clip)
		{
			int			x, y;
			int			width = clip.Width;
			int			height = clip.Height;
			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			Bitmap result = null;
			BitmapData resultData = null;
			BitmapData sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;

				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					double	sourceX;
					double	sourceY;
					double	xJump =  (clip.LlCorner.X - ulCornerX) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Skew));
					double	xRest, yRest;
				
					byte*	pOrigCurrent;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						sourceX = ulCornerX + y * xJump;
						sourceY = ulCornerY + y;

						xRest = sourceX - (int) sourceX;
						if(xRest < 0)
							xRest += 1;

						if(xRest < 0.000001)
							xRest = 0;
						if(xRest > .999999)
						{
							sourceX = (int) sourceX + 1;
							xRest = 0;
						}

						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							if (sourceX >= 0 && sourceX < sourceData.Width && sourceY >= 0 && sourceY <= sourceData.Height - 1)
							{
								yRest = sourceY - (int) sourceY;
								if(yRest < 0)
									yRest += 1;

								if(yRest < 0.000001)
									yRest = 0;
								if(yRest > .999999)
								{
									sourceY = (int) sourceY + 1;
									yRest = 0;
								}
							
								pOrigCurrent = pSource + (int) sourceY * sStride + (int) sourceX;

								if (xRest == 0 || sourceX >= sourceData.Width - 1)
								{
									if (yRest == 0 || sourceY >= sourceData.Height - 1)
									{
										*(pCopyCurrent++) = *pOrigCurrent;
									}
									else
									{
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
									}
								}
								else
								{
									if (yRest == 0 || sourceY >= sourceData.Height - 1)
									{
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-xRest) + pOrigCurrent[1] * xRest);
									}
									else
									{
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-yRest) * (1-xRest) + 
											pOrigCurrent[1] * xRest * (1-yRest) +
											pOrigCurrent[sStride] * (1-xRest) * yRest + 
											pOrigCurrent[sStride+1] * xRest * yRest);
									}
								}
							}
							else
							{
								pCopyCurrent++;
							}
							

							sourceX += 1;
							sourceY += yJump;
						}

					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region GetClipCheckBorders1bpp()
		private static Bitmap GetClipCheckBorders1bpp(Bitmap source, ObjectByCorners clip)
		{
			int x, y;
			int width = clip.Width;
			int height = clip.Height;
			int ulCornerX = clip.UlCorner.X;
			int ulCornerY = clip.UlCorner.Y;

			Bitmap result = null;
			BitmapData resultData = null;
			BitmapData sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					double sourceX;
					double sourceY;
					double xJump = (clip.LlCorner.X - ulCornerX) / (double)clip.Height;
					double yJump = (Math.Tan(clip.Skew));
					double xRest, yRest;

					for (y = 0; y < height; y++)
					{
						sourceX = ulCornerX + y * xJump;
						sourceY = ulCornerY + y;

						xRest = sourceX - (int)sourceX;
						if (xRest < 0)
							xRest += 1;

						if (xRest < 0.000001)
							xRest = 0;
						if (xRest > .999999)
						{
							sourceX = (int)sourceX + 1;
							xRest = 0;
						}

						for (x = 0; x < width; x++)
						{
													
							if (sourceX >= 0 && sourceX < sourceData.Width && sourceY >= 0 && sourceY < sourceData.Height)
							{
								yRest = sourceY - (int)sourceY;
								if (yRest < 0)
									yRest += 1;

								if (yRest < 0.000001)
									yRest = 0;
								if (yRest > .999999)
								{
									sourceY = (int)sourceY + 1;
									yRest = 0;
								}

								if ((pSource[(int)sourceY * sStride + (int)sourceX / 8] & (0x80 >> ((int)sourceX & 0x07))) > 0)
									pResult[y * rStride + x / 8] |= (byte) (0x80 >> (x & 0x07));
							}

							sourceX += 1;
							sourceY += yJump;
						}

					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region IsEntireClipInsideSource()
		private static bool IsEntireClipInsideSource(Size size, ObjectByCorners clip)
		{
			if(clip.UlCorner.X < 0 || clip.UlCorner.Y < 0)
				return false;
			
			if(clip.LlCorner.X < 0 || clip.LlCorner.Y < 0 || clip.LlCorner.X >= size.Width || clip.LlCorner.Y >= size.Height)
				return false;

			Point	urCorner = new Point(clip.UlCorner.X + clip.Width, Convert.ToInt32(clip.UlCorner.Y + Math.Tan(clip.Skew) * clip.Width));

			if(urCorner.X < 0 || urCorner.X >= size.Width || urCorner.Y < 0 || urCorner.Y >= size.Height)
				return false;

			Point	lrCorner = new Point(urCorner.X + clip.LlCorner.X - clip.UlCorner.X, urCorner.Y + clip.LlCorner.Y - clip.UlCorner.Y);

			if(lrCorner.X < 0 || lrCorner.X >= size.Width || lrCorner.Y < 0 || lrCorner.Y >= size.Height)
				return false;

			return true;
		}
		#endregion

		#region AllocHeapMemory()
		private unsafe static void* AllocHeapMemory(int size) 
		{
			void*	result = HeapAlloc(ph, HEAP_ZERO_MEMORY, size);
			
			if (result == null) 
				throw new OutOfMemoryException();

			return result;
		}
		#endregion

		#region RemoveGhostLines
		private static unsafe void RemoveGhostLines(Bitmap bitmap, int[] ghostLines)
		{
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
				byte* pCurrent;
				byte tmp;

				foreach (int ghostLine in ghostLines)
				{
					if (ghostLine >= 0 && ghostLine < bitmap.Width)
					{
						pCurrent = pSource + ghostLine / 8;

						for (int y = 0; y < bitmapData.Height; y++)
						{
							if (*pCurrent > 0)
							{
								tmp = (byte)(0x80 >> (ghostLine & 0x07));
								*pCurrent = (byte)(*pCurrent & (~tmp));
							}

							pCurrent += bitmapData.Stride;
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#endregion
	}
}
