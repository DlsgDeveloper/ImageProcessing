using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for BookeyeBookLocator.
	/// </summary>
	public class PagesLocator
	{
		#region GoMem()
		public unsafe static Rectangle[] GoMem(int width, int height, int stride, PixelFormat pixelFormat, 
			byte* firstByte, ColorPalette palette, bool cropAndDeskew, out byte confidence) 
		{ 	
#if DEBUG
			confidence = 0;
			DateTime	start = DateTime.Now ;
#endif
			IntPtr		scan0 = new IntPtr(firstByte);
			Bitmap		bitmap = new Bitmap(width, height, stride, pixelFormat, scan0);			
			
			if(bitmap == null)
				throw new IpException(ErrorCode.ErrorNoImageLoaded) ;

			
			try
			{
				return FindPages(bitmap, cropAndDeskew, 30, .5F, true, out confidence);
			}
			catch
			{
				throw new IpException(ErrorCode.ErrorUnexpected) ;
			}
			finally
			{
				if(bitmap != null)
					bitmap.Dispose();

#if DEBUG
			Console.WriteLine(string.Format("GoMem(): {0}, Confidence:{1}%", DateTime.Now.Subtract(start).ToString(), confidence));			
#endif
			}
		}
		#endregion
		
		#region GoStream() 
		public unsafe static Rectangle[] GoStream(byte* firstByte, int length, bool cropAndDeskew, out byte confidence) 
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif
			byte[]			array = new byte[length];
			Marshal.Copy(new IntPtr(firstByte), array, 0, length);

			Bitmap			bitmap;
			MemoryStream	stream = new MemoryStream(array);

			try
			{
				bitmap = new Bitmap(stream) ;
			}
			catch(Exception ex)
			{
				throw new Exception("Can't generate bitmap.\nException " + ex);
			}

			Rectangle[] pages = FindPages(bitmap, cropAndDeskew, 30, .5F, true, out confidence);
			
					
			bitmap.Dispose();
			stream.Close();

#if DEBUG
			Console.Write(string.Format("GoStream(): {0}, Confidence:{1}%",  DateTime.Now.Subtract(start).ToString(), confidence)) ;
#endif
			return pages;
		}
		#endregion
				
		#region GoFile() 
		public unsafe static Rectangle[] GoFile(string filePath, bool cropAndDeskew, out byte confidence) 
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif
			Bitmap			bitmap;

			try
			{
				bitmap = new Bitmap(filePath) ;
			}
			catch(Exception ex)
			{
				throw new Exception("Can't generate bitmap.\nException " + ex);
			}

			Rectangle[] pages = FindPages(bitmap, cropAndDeskew, 30, .5F, true, out confidence);
					
			if(bitmap != null)
				bitmap.Dispose();

#if DEBUG
			Console.Write(string.Format("GoFile(): {0}, Confidence:{1}%",  DateTime.Now.Subtract(start).ToString(), confidence)) ;
#endif
			return pages;
		}
		#endregion

		#region FindPages()
		public static Rectangle[] FindPages(Bitmap bitmap, bool cropAndDeskew, int minDelta, float offsetF, bool darkBookfold, out byte confidence)
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			ItImage itImage = new ItImage(bitmap);
			Operations operations = new Operations(true, offsetF, false, false, false);
			
			confidence = Convert.ToByte(itImage.Find(operations) * 100.0);

			Rectangle pageL = itImage.PageL.Clip.RectangleNotSkewed;
			Rectangle pageR = itImage.PageR.Clip.RectangleNotSkewed;

#if DEBUG
			Console.Write(string.Format("GoFile(): {0}, Confidence:{1}%",  DateTime.Now.Subtract(start).ToString(), confidence)) ;
#endif
			return new Rectangle[] { pageL, pageR };
		}
		#endregion
	}
}
