using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class ColorReplacement
	{		
		#region variables
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
		#endregion

		#region constructor
		private ColorReplacement()
		{
		}
		#endregion

		//	PUBLIC METHODS
		
		#region ReplaceColorStream() 
		public unsafe static ErrorCode ReplaceColorStream(byte** firstByte, int* length, short jpegCompression, 
			byte colorR, byte colorG, byte colorB, byte range, byte deltaR, byte deltaG, byte deltaB) 
		{ 			
			try
			{
				byte[]			array = new byte[*length];
				Marshal.Copy(new IntPtr(*firstByte), array, 0, (int) *length);

				MemoryStream	stream = new MemoryStream(array);
				Bitmap			bitmap;
				try
				{
					bitmap = new Bitmap(stream) ;
				}
				catch
				{
					stream.Close();
					return ErrorCode.ErrorNoImageLoaded;
				}

				ErrorCode	result = ReplaceColor(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), colorR, colorG, colorB, range, deltaR, deltaG, deltaB);

				stream.Close();
				
				ImageCodecInfo		codecInfo = Encoding.GetCodecInfo(bitmap);
				EncoderParameters	encoderParams = Encoding.GetEncoderParams(bitmap, jpegCompression) ;
				
				MemoryStream	resultStream = new MemoryStream();
				bitmap.Save(resultStream, codecInfo, encoderParams) ;

				*length = (int) resultStream.Length;
				*firstByte = (byte*) HeapAlloc(ph, HEAP_ZERO_MEMORY, (int) resultStream.Length);
				Marshal.Copy(resultStream.ToArray(), 0, new IntPtr(*firstByte), (int) resultStream.Length);

				bitmap.Dispose() ;
				resultStream.Close();
				return result;
			}
#if DEBUG
			catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex.Message);
				return ErrorCode.ErrorUnexpected;
			}
#else
			catch
			{
				return ErrorCode.ErrorUnexpected;
			}
#endif
		}
		#endregion
		
		#region ReplaceColorPath()
		public static ErrorCode ReplaceColorPath(string source, string dest, short jpegCompression, byte colorR, byte colorG, byte colorB, 
			byte range, byte deltaR, byte deltaG, byte deltaB) 
		{ 			
			try
			{
				FileInfo	sourceFile = new FileInfo(source);
				FileInfo	destFile = new FileInfo(dest);

				if((sourceFile.Attributes & FileAttributes.Directory) > 0)
					return ReplaceColorDir(new DirectoryInfo(sourceFile.FullName), new DirectoryInfo(destFile.FullName), jpegCompression, colorR, colorG, colorB, range, deltaR, deltaG, deltaB);
				else
					return ReplaceColorFile(sourceFile, destFile, jpegCompression, colorR, colorG, colorB, range, deltaR, deltaG, deltaB);
			}
#if DEBUG
			catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex.Message);
				return ErrorCode.ErrorUnexpected;
			}
#else
			catch
			{
				return ErrorCode.ErrorUnexpected;
			}
#endif
		}
		#endregion
				
		#region ReplaceColor()	
		public static ErrorCode ReplaceColor(Bitmap bitmap, Rectangle clip, byte colorR, byte colorG, byte colorB, 
			byte range, byte deltaR, byte deltaG, byte deltaB)
		{
			try
			{
				if(bitmap == null)
					return ErrorCode.ErrorNoImageLoaded ;

				Rectangle	rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
				ErrorCode	returnCode = ErrorCode.OK;
			
				switch(bitmap.PixelFormat)
				{
					case PixelFormat.Format24bppRgb :	
					{
						returnCode = ReplaceColor_24bpp(bitmap, rect, 100, 185, colorR, colorG, colorB, 
							range, deltaR, deltaG, deltaB); 
					}break;
					case PixelFormat.Format8bppIndexed:	
					{
						returnCode = ReplaceColor_8bpp(bitmap, rect, 100, 185, colorR, range, deltaR); 
					}break;
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}

				return returnCode ;
			}
#if DEBUG
			catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex.Message);
				return ErrorCode.ErrorUnexpected;
			}
#else
			catch
			{
				return ErrorCode.ErrorUnexpected;
			}
#endif
		}
		#endregion
		
		//PRIVATE METHODS
		
		#region ReplaceColorDir()
		private static ErrorCode ReplaceColorDir(DirectoryInfo sourceDir, DirectoryInfo destDir, short jpegCompression, 
			byte colorR, byte colorG, byte colorB, byte range, byte deltaR, byte deltaG, byte deltaB)
		{
			ArrayList	sources = new ArrayList(); 
			Bitmap		bitmap;
			ErrorCode	result = ErrorCode.OK;

			destDir.Create();

			sources.AddRange(sourceDir.GetFiles("*.tif"));
			sources.AddRange(sourceDir.GetFiles("*.jpg"));
			sources.AddRange(sourceDir.GetFiles("*.png"));
			sources.AddRange(sourceDir.GetFiles("*.bmp"));
			sources.AddRange(sourceDir.GetFiles("*.gif"));

			if(sources.Count == 0)
				return ErrorCode.OK;
			
			foreach(FileInfo file in sources)
			{				
				bitmap = new Bitmap(file.FullName) ;

				if(sourceDir.FullName == destDir.FullName)
				{
					Bitmap copy = ImageCopier.Copy(bitmap);
					bitmap.Dispose();
					bitmap = copy;
				}
						
				result = ReplaceColor(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), colorR, colorG, colorB, range, deltaR, deltaG, deltaB);

				ImageCodecInfo		codecInfo = Encoding.GetCodecInfo(bitmap);
				EncoderParameters	encoderParams = Encoding.GetEncoderParams(bitmap, jpegCompression) ;

				if(File.Exists(destDir.FullName +  @"\" + file.Name))
					File.Delete(destDir.FullName +  @"\" + file.Name);
				
				bitmap.Save(destDir.FullName +  @"\" + file.Name, codecInfo, encoderParams) ;
				bitmap.Dispose() ;
				bitmap = null;
			}

			return result;
		}
		#endregion

		#region ReplaceColorFile()
		private static ErrorCode ReplaceColorFile(FileInfo sourceFile, FileInfo resultFile, short jpegCompression, 
			byte colorR, byte colorG, byte colorB, byte range, byte deltaR, byte deltaG, byte deltaB)
		{			
			Bitmap			bitmap = new Bitmap(sourceFile.FullName) ;
			ErrorCode		result;

			if(sourceFile.FullName ==  resultFile.FullName)
			{
				Bitmap copy = ImageCopier.Copy(bitmap);
				bitmap.Dispose();
				bitmap = copy;
			}
			
			result = ReplaceColor(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), colorR, colorG, colorB, range, deltaR, deltaG, deltaB);
				
			ImageCodecInfo		codecInfo = Encoding.GetCodecInfo(bitmap);
			EncoderParameters	encoderParams = Encoding.GetEncoderParams(bitmap, jpegCompression) ;
							
			resultFile.Refresh();
			if(resultFile.Exists)
				resultFile.Delete();
				
			bitmap.Save(resultFile.FullName, codecInfo, encoderParams) ;

			bitmap.Dispose();
			bitmap = null;
			return result;
		}
		#endregion
				
		#region ReplaceColor_24bpp()
		private static ErrorCode ReplaceColor_24bpp(Bitmap bitmap, Rectangle clip, byte oThreshold, byte bThreshold, 
			byte centerR, byte centerG, byte centerB, byte range, byte deltaR, byte deltaG, byte deltaB)
		{
			BitmapData	bitmapData = null;

			try
			{				
				int		x, y;
				int		clipWidth = clip.Width;
				int		clipHeight = clip.Height;
				byte	red, green, blue;

				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat); 
			
				int			stride = bitmapData.Stride; 
			
				unsafe
				{
					byte*	pOrig = (byte*)bitmapData.Scan0.ToPointer();				
					byte*	pCurrent;

					for(y = 0; y < clipHeight; y++)
					{							
						pCurrent = pOrig + y * stride;

						for (x = 0; x < clipWidth; x++)
						{
							red = pCurrent[2];
							green = pCurrent[1];
							blue = *pCurrent;

							if ((red >= centerR - range) && (red <= centerR + range) &&
								(green >= centerG - range) && (green <= centerG + range) &&
								(blue >= centerB - range) && (blue <= centerB + range))
							{
								//blue
								if (centerB + deltaB + centerB - blue > 255)
									*pCurrent = 255;
								else if (centerB + deltaB + centerB - blue < 0)
									*pCurrent = 0;
								else
									*pCurrent = (byte)(centerB + deltaB + centerB - blue);

								//green
								if (centerG + deltaG + centerG - green > 255)
									pCurrent[1] = 255;
								else if (centerG + deltaG + centerG - green < 0)
									pCurrent[1] = 0;
								else
									pCurrent[1] = (byte)(centerG + deltaG + centerG - green);

								//red
								if (centerR + deltaR + centerR - red > 255)
									pCurrent[2] = 255;
								else if (centerR + deltaR + centerR - red < 0)
									pCurrent[2] = 0;
								else
									pCurrent[2] = (byte)(centerR + deltaR + centerR - red);
							}

							pCurrent += 3;
						}
					}
				}

				return ErrorCode.OK; 
			}
			finally
			{
				if(bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion
		
		#region ReplaceColor_8bpp()
		private static ErrorCode ReplaceColor_8bpp(Bitmap bitmap, Rectangle clip, byte oThreshold, byte bThreshold, 
			byte center, byte range, byte delta)
		{
			BitmapData	bitmapData = null;

			try
			{
				byte[]		palette = new byte[256];
				byte[]		invPalette = new byte[256];
				

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat); 
			
				int			stride = bitmapData.Stride; 
				int			x, y;
				int			clipWidth = clip.Width;
				int			clipHeight = clip.Height;
				byte		gray;
				int			newValue;

				for(int i = 0; i < 256; i++)
				{
					palette[i] = bitmap.Palette.Entries[i].R;
					invPalette[bitmap.Palette.Entries[i].R] = (byte) i;
				}
			
				unsafe
				{
					byte*	pOrig = (byte*)bitmapData.Scan0.ToPointer();			
					byte	*pCurrent;

					for (y = 0; y < clipHeight; y++)
					{
						pCurrent = pOrig + y * stride;

						for (x = 0; x < clipWidth; x++)
						{
							gray = palette[*pCurrent];

							if (gray >= center - range && gray <= center + range)
							{
								newValue = center + delta + center - gray;
								
								if (newValue > 255)
									*pCurrent = invPalette[255];
								else if (newValue < 0)
									*pCurrent = invPalette[0];
								else
									*pCurrent = invPalette[(byte)(newValue)];
							}

							pCurrent++;
						}
					}
				}

				return ErrorCode.OK; 
			}
			finally
			{
				if(bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion
		
		#region ReplaceColor_8bppGray()
		private static ErrorCode ReplaceColor_8bppGray(Bitmap bitmap, Rectangle clip, byte oThreshold, byte bThreshold, 
			byte center, byte range, byte delta)
		{
			BitmapData	bitmapData = null;

			try
			{				
				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat); 
			
				int			stride = bitmapData.Stride; 
				int			x, y;
				int			clipWidth = clip.Width;
				int			clipHeight = clip.Height;
				byte		gray;
			
				unsafe
				{
					byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer();				
					byte*	pCurrent;
					
					for(y = 0; y < clipHeight; y++)
					{							
						pCurrent = pOrig + y * stride;
							
						for(x = 0; x < clipWidth; x++)
						{
							gray = *pCurrent;

							if(gray >= center - range && gray <= center + range)
							{
								if (center + delta + center - gray > 255)
									*pCurrent = 255;
								else if (center + delta + center - gray < 0)
									*pCurrent = 0;
								else
									*pCurrent = (byte)(center + delta + center - gray);
							}

							pCurrent++;
						}
					}
				}

				return ErrorCode.OK; 
			}
			finally
			{
				if(bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion
		
	}
}
