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
	public class BookeyePagesLocator
	{
		#region constructor
		private BookeyePagesLocator()
		{
		}
		#endregion

		#region GoMem()
		public unsafe static Rectangle[] GoMem(int width, int height, int stride, PixelFormat pixelFormat, 
			byte* firstByte, ColorPalette palette, bool cropAndDescew, out byte confidence) 
		{ 	
#if DEBUG
			confidence = 0;
			DateTime	start = DateTime.Now ;
#endif
			IntPtr		scan0 = new IntPtr(firstByte);
			Bitmap		bitmap = new Bitmap(width, height, stride, pixelFormat, scan0);			
			
			if(bitmap == null)
				throw IpException.Create(ErrorCode.ErrorNoImageLoaded) ;

			
			try
			{
				return FindPages(bitmap, cropAndDescew, out confidence, 30);
			}
			catch(Exception ex)
			{
				ex = ex;
				throw IpException.Create(ErrorCode.ErrorUnexpected) ;
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
		public unsafe static Rectangle[] GoStream(byte* firstByte, int length, bool cropAndDescew, out byte confidence) 
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

			Rectangle[]		pages = FindPages(bitmap, cropAndDescew, out confidence, 30);
			
					
			bitmap.Dispose();
			stream.Close();

#if DEBUG
			Console.Write(string.Format("GoStream(): {0}, Confidence:{1}%",  DateTime.Now.Subtract(start).ToString(), confidence)) ;
#endif
			return pages;
		}
		#endregion
				
		#region GoFile() 
		public unsafe static Rectangle[] GoFile(string filePath, bool cropAndDescew, out byte confidence) 
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

			Rectangle[]		pages = FindPages(bitmap, cropAndDescew, out confidence, 30);
					
			if(bitmap != null)
				bitmap.Dispose();

#if DEBUG
			TimeSpan	time = DateTime.Now.Subtract(start) ;
			Console.Write(string.Format("GoFile(): {0}, Confidence:{1}%",  time.ToString(), confidence)) ;
#endif
			return pages;
		}
		#endregion

		#region FindPages()
		public static Rectangle[] FindPages(Bitmap bitmap, bool cropAndDescew, out byte confidence, int minDelta)
		{
#if DEBUG
			DateTime	now = DateTime.Now ;
#endif					
			
			Bitmap		bmpEdgeDetect;
			Rectangle[]		pages = null;
			confidence = 0;
			
			try
			{
				bmpEdgeDetect = ImageProcessing.BookeyeImagePreprocessing.Go(bitmap, 0, minDelta);
									
				//book fold center
				int			bookfold = BookeyePageSplitter.Get(bmpEdgeDetect, 20, new RectangleF(.35F, .15F, .3F, .70F), .125F, .02F, out confidence);
				
				if(confidence > 30)
				{
					//find content
					byte	confL, confR;
					pages = new Rectangle[2];
					pages[0] = PageContentLocator.Get(bmpEdgeDetect, new Rectangle(0, 0, bookfold, bmpEdgeDetect.Height), 20, 
						new RectangleF(.4F, .10F, .2F, .80F), .25F, .01F, 100, 1, bitmap.HorizontalResolution * .5, out confL);
					pages[1] = PageContentLocator.Get(bmpEdgeDetect, new Rectangle(bookfold, 0, bmpEdgeDetect.Width - bookfold, bmpEdgeDetect.Height), 
						20, new RectangleF(.4F, .10F, .2F, .80F), .25F, .01F, 100, 2, bitmap.HorizontalResolution * .5, out confR);
					confidence = (byte) ((confidence * 50 + confL * 25 + confR * 25) / 100);
				}
				else
				{
					confidence = 0;
				}
					
				bmpEdgeDetect.Dispose();
				return pages;
			}
			catch(Exception ex)
			{
				ex = ex;
				confidence = 0;
				return null;
			}
		}
		#endregion
	}
}
