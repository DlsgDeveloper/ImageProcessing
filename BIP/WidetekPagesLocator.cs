using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections;


namespace ImageProcessing
{
	public class WidetekPagesLocator
{
    // Methods
    private static Rectangle[] FindPages(ImageFile.ImageInfo imageInfo, Bitmap bitmap, bool cropAndDeskew, out byte confidence)
    {
#if DEBUG
		DateTime start = DateTime.Now;
#endif
		try
        {
            confidence = 100;
            ArrayList rects = new ArrayList();
			ItImage itImage = new ItImage(bitmap);

			itImage.FindContent(bitmap, new Operations.ContentLocationParams(true, 0.2F, 0.2F));
            
			if (itImage.TwoPages)
            {
                rects.Add(itImage.PageL.Clip.RectangleNotAngled);
                rects.Add(itImage.PageR.Clip.RectangleNotAngled);
            }
            else
            {
                rects.Add(itImage.Page.Clip.RectangleNotAngled);
            }
            
			return (Rectangle[]) rects.ToArray(typeof(Rectangle));
        }
#if DEBUG
        catch (Exception ex)
        {
			Console.WriteLine("Exception: " + ex.Message);
            confidence = 0;
            return null;
        }
#else
		catch
		{
			confidence = 0;
			return null;
		}
#endif
        finally
        {
#if DEBUG
			System.Reflection.MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
			Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "(): " + DateTime.Now.Subtract(start).ToString());
#endif
		}
    }

    public static Rectangle[] GoFile(string filePath, bool cropAndDeskew, out byte confidence)
    {
        Bitmap bitmap;
#if DEBUG
		DateTime start = DateTime.Now;
#endif
		try
        {
            bitmap = new Bitmap(filePath);
        }
        catch (Exception ex)
        {
            throw new Exception("Can't generate bitmap.\nException " + ex);
        }
		Rectangle[] pages = FindPages(new ImageFile.ImageInfo(filePath), bitmap, cropAndDeskew, out confidence);
        if (bitmap != null)
        {
            bitmap.Dispose();
        }
#if DEBUG
        TimeSpan time = DateTime.Now.Subtract(start);
		Console.Write(string.Format("GoFile(): {0}, Confidence:{1}%", time.ToString(), (byte)confidence));
#endif
        return pages;
    }

    public static unsafe Rectangle[] GoMem(int width, int height, int stride, PixelFormat pixelFormat, byte* firstByte, ColorPalette palette, bool cropAndDeskew, out byte confidence)
    {
        confidence = 0;
#if DEBUG
		DateTime start = DateTime.Now;
#endif
		IntPtr scan0 = new IntPtr((void*)firstByte);
        Bitmap bitmap = new Bitmap(width, height, stride, pixelFormat, scan0);
        if (bitmap == null)
        {
            throw new IpException(ErrorCode.ErrorNoImageLoaded);
        }
        try
        {
			ImageFile.ImageInfo imageInfo = new ImageFile.ImageInfo(bitmap.Width, bitmap.Height, (int) bitmap.HorizontalResolution, (int) bitmap.VerticalResolution, bitmap.PixelFormat);
			return FindPages(imageInfo, bitmap, cropAndDeskew, out confidence);
        }
        catch
        {
            throw new IpException(ErrorCode.ErrorUnexpected);
        }
        finally
        {
            if (bitmap != null)
            {
                bitmap.Dispose();
            }
#if DEBUG
			Console.WriteLine(string.Format("GoMem(): {0}, Confidence:{1}%", DateTime.Now.Subtract(start).ToString(), (byte)confidence));
#endif
        }
    }

    public static unsafe Rectangle[] GoStream(byte* firstByte, int length, bool cropAndDeskew, out byte confidence)
    {
        Bitmap bitmap;
#if DEBUG
		DateTime start = DateTime.Now;
#endif
		byte[] array = new byte[length];
        Marshal.Copy(new IntPtr((void*) firstByte), array, 0, length);
        MemoryStream stream = new MemoryStream(array);
        try
        {
            bitmap = new Bitmap(stream);
        }
        catch (Exception ex)
        {
            throw new Exception("Can't generate bitmap.\nException " + ex);
        }

		ImageFile.ImageInfo imageInfo = new ImageFile.ImageInfo(bitmap.Width, bitmap.Height, (int)bitmap.HorizontalResolution, (int)bitmap.VerticalResolution, bitmap.PixelFormat);
		Rectangle[] pages = FindPages(imageInfo, bitmap, cropAndDeskew, out confidence);
        bitmap.Dispose();
        stream.Close();
#if DEBUG
        Console.Write(string.Format("GoStream(): {0}, Confidence:{1}%", DateTime.Now.Subtract(start).ToString(), (byte) confidence));
#endif
		return pages;
    }
}


	
	/*public class WidetekPagesLocator
	{
		private WidetekPagesLocator()
		{
		}

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
				return FindPages(bitmap, cropAndDeskew, out confidence);
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


			Rectangle[]		pages = FindPages(bitmap, cropAndDeskew, out confidence);
			
					
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

			Rectangle[]		pages = FindPages(bitmap, cropAndDeskew, out confidence);
					
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
		private static Rectangle[] FindPages(Bitmap bitmap, bool cropAndDeskew, out byte confidence)
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif					
			
			try
			{				
				confidence = 100;
				ArrayList	rects = new ArrayList();

				ImageFile.ImageInfo imageInfo = ImageFile.MainClass.GetImageInfo(bitmap);
				ImageParams itImage = new ImageParams(imageInfo, (bitmap.Width > bitmap.Height));

				itImage.FindContent(bitmap, Convert.ToInt32(bitmap.HorizontalResolution * .2F), 0, 20);

				if(itImage.TwoPages)
				{
					rects.Add(itImage.PageL.Clip.RectangleNotAngled);
					rects.Add(itImage.PageR.Clip.RectangleNotAngled);
				}
				else
					rects.Add(itImage.Page.Clip.RectangleNotAngled);

				return (Rectangle[]) rects.ToArray(typeof(Rectangle));

			}
#if DEBUG
			catch(Exception ex)
			{
				Console.WriteLine("Exception: " + ex.Message);
				confidence = 0;
				return null;
			}
#endif
			finally
			{
#if DEBUG
			Console.WriteLine("WidetekPagesLocator FindPages():" + (DateTime.Now.Subtract(start)).ToString()) ;
#endif
			}
		}
		#endregion
	}*/
}
