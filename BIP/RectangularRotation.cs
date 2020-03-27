using ImageProcessing.Languages;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for RectangularRotation.
	/// </summary>
	public class RectangularRotation
	{
		private RectangularRotation()
		{
		}

		#region AnalyzePath()
		public static int AnalyzePath(string source, ref int heads, ref int legs, ref int wordsCount, int flags) 
		{ 			
			try
			{
				Analyze(new Bitmap(source), ref heads, ref legs, ref wordsCount, flags);
				return (int) ErrorCode.OK;
			}
			catch(Exception ex)
			{
				throw new Exception(BIPStrings.CanTAnalyzeImage_STR+"!\nException: " + ex);
			}
		}
		#endregion
		
		#region AnalyzeStream() 
		public unsafe static int AnalyzeStream(byte** firstByte, int length, ref int heads, ref int legs, ref int wordsCount, int flags) 
		{ 			
			byte[]			array = new byte[length];
			Bitmap			bitmap;

			Marshal.Copy(new IntPtr(*firstByte), array, 0, length);

			MemoryStream	stream = new MemoryStream(array);

			try
			{
				bitmap = new Bitmap(stream) ;
			}
			catch(Exception ex)
			{
				throw new Exception(BIPStrings.CanTGenerateBitmap_STR+".\nException: " + ex);
			}
			
			try
			{
				Analyze(bitmap, ref heads, ref legs, ref wordsCount, flags);
			}
			catch(Exception ex)
			{
				throw new Exception(BIPStrings.CanTAnalyzeImage_STR+"!\nException: " + ex);
			}
								
			bitmap.Dispose();
			stream.Close();
			return (int) ErrorCode.OK;
		}
		#endregion		
		
		#region AnalyzeHandle() 
		public unsafe static int AnalyzeHandle(void* fileHandle, ref int heads, ref int legs, ref int wordsCount, int flags) 
		{
			Microsoft.Win32.SafeHandles.SafeFileHandle safeFileHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(fileHandle), false);
			FileStream		fileStream = new FileStream(safeFileHandle, FileAccess.Read);
			Bitmap			bitmap;

			try
			{
				bitmap = new Bitmap(fileStream) ;
			}
			catch(Exception ex)
			{
				throw new Exception("Can't generate bitmap.\nException: " + ex);
			}
			
			Analyze(new Bitmap(bitmap), ref heads, ref legs, ref wordsCount, flags);
								
			bitmap.Dispose();
			fileStream.Close();
			return (int) ErrorCode.OK;
		}
		#endregion
		
		#region IsUpsideDown()
		public static bool IsUpsideDown(Bitmap bitmap, int flag)
		{
			int		heads = 0;
			int		legs = 0;
			int		wordsCount = 0;

			Analyze(bitmap, ref heads, ref legs, ref wordsCount, flag);

			return (legs > heads);
		}
		#endregion

		#region Analyze()
		public static void Analyze(Bitmap bitmap, ref int heads, ref int legs, ref int wordsCount, int flag)
		{
			heads = 0;
			legs = 0;
			wordsCount = 0;

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif			
			try
			{
				ItImage itImage = new ItImage(bitmap);

				PageObjects.Words words = itImage.Words;
#if SAVE_RESULTS
				words.DrawToFile(Debug.SaveToDir + @"04 Words.png", bitmap.Size);
#endif				
				foreach(PageObjects.Word word in words)
					word.CountHeadsAndLegs(ref heads, ref legs);

				wordsCount = words.Count;
			}
			catch(Exception ex)
			{
				throw new Exception("RectangularRotation, Analyze(): " + ex.Message ) ;
			}
			finally
			{
#if DEBUG
				Console.WriteLine(string.Format("RectangularRotation, IsUpsideDown(): {0}, Words: {1}, Heads: {2}, Legs: {3}",
					DateTime.Now.Subtract(start).ToString(), wordsCount, heads, legs));
#endif
			}
		}

		/*public static void Analyze(Bitmap bitmap, ref int heads, ref int legs, ref int wordsCount, int flag)
		{
			heads = 0;
			legs = 0;
			wordsCount = 0;

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif
			try
			{
				//Bitmap source = EdgeDetector.Get(bitmap, Rectangle.Empty, 200, 200, 200, 30, true, EdgeDetector.Operator.Laplacian446a);	
				PageObjects.Symbols objects = ImagePreprocessing.GetPageObjects(bitmap, 0, 20);

				//ImageProcessing.NoiseReduction.Despeckle(source);
				GC.Collect();

				objects.Despeckle(Convert.ToInt32(bitmap.HorizontalResolution / 75.0F), Convert.ToInt32(bitmap.HorizontalResolution / 50.0F));
				GC.Collect();

				PageObjects.Words words = PageObjects.Words.FindWords(objects);
#if SAVE_RESULTS
				words.DrawToFile(Debug.SaveToDir + @"04 Words.png", bitmap.Size);
#endif
				foreach (PageObjects.Word word in words)
					word.CountHeadsAndLegs(ref heads, ref legs);

				wordsCount = words.Count;
			}
			catch (Exception ex)
			{
				throw new Exception("RectangularRotation, Analyze(): " + ex.Message);
			}
			finally
			{
#if DEBUG
				Console.WriteLine(string.Format("RectangularRotation, IsUpsideDown(): {0}, Words: {1}, Heads: {2}, Legs: {3}",
					DateTime.Now.Subtract(start).ToString(), wordsCount, heads, legs));
#endif
			}
		}*/
		#endregion

	}
}
