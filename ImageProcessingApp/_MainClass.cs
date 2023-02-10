using BIP.Geometry;
using ImageProcessing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TestApp
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class _MainClass
	{
		#region variables
		static int ph = GetProcessHeap();

		static ManualResetEvent wait = new ManualResetEvent(false);

		// Heap API flags
		const int HEAP_ZERO_MEMORY = 0x00000008;

		static object importReducedLocker = new object();
		static object importPreviewLocker = new object();
		static object importThumbsLocker = new object();

		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);
		
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


		#region Main()
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(ImageProcessing.Arithmetic)).Location);
			string version = fvi.FileVersion;
			//
			// TODO: Add code to start application here
			//
			Console.WriteLine("------------------------------------");
			Console.WriteLine("Image Access, Inc.");
			Console.WriteLine("Image Processing");
			Console.WriteLine("Version " + version);
			Console.WriteLine("------------------------------------");

			DateTime start = DateTime.Now;
			
			try
			{
				//TestApp.InsertorTest.Go();
				TestApp.ContentLocatorTest.Go();
			}
			catch (Exception ex)
			{
				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine("ERROR: ");

				do
				{
					Console.WriteLine(ex.Message);
				} while ((ex = ex.InnerException) != null);

				Console.ReadLine();
			}
			finally
			{
				Console.WriteLine();
				Console.WriteLine("Done. Total Time: " + DateTime.Now.Subtract(start).ToString());
				//Console.ReadLine();
			}
		}
		#endregion 


		#region AutoColor()
		private static void AutoColor()
		{
			Bitmap bitmap = new Bitmap(@"C:\delete\01.jpg");
			DateTime start = DateTime.Now;

			ImageProcessing.AutoColor.Get(bitmap);

			Console.WriteLine("Total time: " + DateTime.Now.Subtract(start).ToString());

			bitmap.Save(@"C:\delete\result.png", ImageFormat.Png);
			bitmap.Dispose();
		}
		#endregion

		#region AudioZoning()
		private unsafe static void AudioZoning()
		{
			string sourceFile = @"C:\ProgramData\DLSG\KIC\Images\Export\AudioZoning\2011-11-22 16-27-35 2.jpg";
			DateTime start = DateTime.Now;

			ImageProcessing.BigImages.AudioZoning zoning = new ImageProcessing.BigImages.AudioZoning();

			zoning.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);
			List<BIP.Geometry.RatioRect> zones = zoning.GetZones(sourceFile);

			foreach (BIP.Geometry.RatioRect zone in zones)
				Console.WriteLine(zone.ToString());

			Console.WriteLine(string.Format("Ripm: {0}, Zones: {1}", DateTime.Now.Subtract(start).ToString(), zones.Count.ToString()));
		}
		#endregion

		#region BackgroundRemoval()
		private static void BackgroundRemoval()
		{
			string source = @"C:\Documents and Settings\Jirka\My Documents\projects\Image Processing\Binorization\Bookeye Binorization\bi\058922_g.png";
			string dest = @"C:\Documents and Settings\Jirka\My Documents\projects\Image Processing\Binorization\Bookeye Binorization\bi\result.png";

			Bitmap bitmap = new Bitmap(source);
			DateTime start = DateTime.Now;

			ImageProcessing.BackgroundRemoval.Go(bitmap);

			Console.WriteLine("Total time: " + DateTime.Now.Subtract(start).ToString());

			bitmap.Save(dest, ImageFormat.Png);
			bitmap.Dispose();
		}
		#endregion

		#region BinarizeDynamicly()
		private static void BinarizeDynamicly()
		{
			//string source = @"C:\Documents and Settings\Jirka\My Documents\projects\Image Processing\Binorization\Bookeye Binorization\bi\058922_c.tif";
			string source = @"C:\Documents and Settings\Jirka\My Documents\projects\Image Processing\Binorization\Bookeye Binorization\bi\058922_g.png";
			string dest = @"C:\Documents and Settings\Jirka\My Documents\projects\Image Processing\Binorization\Bookeye Binorization\bi\result.png";

			ImageProcessing.BigImages.Binarization binarization = new ImageProcessing.BigImages.Binarization();
			//binarization.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);
			binarization.Dynamic(source, dest, new ImageProcessing.FileFormat.Png());
		}
		#endregion

		#region BinarizationThreshold()
		private static void BinarizationThreshold()
		{
			string source = @"C:\Documents and Settings\Jirka\My Documents\projects\Image Processing\Binorization\Bookeye Binorization\bi\058922_c.tif";
			string dest = @"C:\Documents and Settings\Jirka\My Documents\projects\Image Processing\Binorization\Bookeye Binorization\bi\result.png";

			Bitmap sourceBitmap = new Bitmap(source);
			Histogram h = new Histogram(sourceBitmap);
			Bitmap resultBitmap = ImageProcessing.BinorizationThreshold.Binorize(sourceBitmap, h.ThresholdR, h.ThresholdG, h.ThresholdB);

			resultBitmap.Save(dest, ImageFormat.Png);
		}
		#endregion

		#region BinarizeBigImage()
		private static void BinarizeBigImage()
		{
			string source = @"C:\Documents and Settings\Jirka\My Documents\projects\Image Processing\Binorization\Bookeye Binorization\bi\058922_c.tif";
			string dest = @"C:\Documents and Settings\Jirka\My Documents\projects\Image Processing\Binorization\Bookeye Binorization\bi\result.png";
			ImageProcessing.BigImages.Binarization binarization = new ImageProcessing.BigImages.Binarization();

			binarization.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);
			DateTime start = DateTime.Now;

			binarization.Threshold(source, dest, new ImageProcessing.FileFormat.Png());
		}
		#endregion

		#region BinorizationProductionText()
		private static void BinorizationProductionText(FileInfo sourceFile, FileInfo resultFile)
		{
			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionCCITT4);
			Bitmap bitmap = new Bitmap(sourceFile.FullName);
			ImageCodecInfo imageCodecInfo = GetEncoderInfo(bitmap);
			DateTime start = DateTime.Now;
			Bitmap result = ImageProcessing.BinorizationProductionText.Binorize(bitmap); // Basic.Binorize(bitmap) ;
			TimeSpan time = DateTime.Now.Subtract(start);

			Console.WriteLine(sourceFile.Name + ": " + time.ToString());

			result.Save(resultFile.FullName, imageCodecInfo, encoderParams);
			bitmap.Dispose();
			result.Dispose();
		}
		#endregion

		#region BookfoldCorrection()
		private static void BookfoldCorrection()
		{
			string source = @"C:\OpusLite\WorkingData\ActiveObjectHive\00\000\046\ScanImages\Full\00000046_000004.Jpg";
			string dest = @"C:\OpusLite\WorkingData\ActiveObjectHive\00\000\046\ScanImages\Full\result.Jpg";

			ImageProcessing.IpSettings.ItImage itImage = new ImageProcessing.IpSettings.ItImage(new FileInfo(source), false);
			itImage.PageL.Activate(RatioRect.FromLTRB(0.513, 0.020, 0.997, 0.980), true);
			itImage.PageL.SetSkew(-0.0081268265917242583, 1);
			itImage.PageL.Bookfolding.BottomCurve.SetPoints(new RatioPoint[] {
					new  RatioPoint(0.513, 0.811),
					new  RatioPoint(0.673, 0.811),
					new  RatioPoint(0.805, 0.823),
					new  RatioPoint(0.997, 0.980),
				});
			itImage.Execute(source, 0, dest, new ImageProcessing.FileFormat.Png());
		}
		#endregion

		#region ClipBigImage()
		private unsafe static void ClipBigImage()
		{
				DateTime total = DateTime.Now;
				string filePath = @"C:\Users\jirka.stybnar\TestRun\Big Images\trib04081934004.tif";
				ImageProcessing.BigImages.ItDecoder itBitmap = new ImageProcessing.BigImages.ItDecoder(filePath);

				for (int i = 0; i < 10; i++)
				{
					DateTime start = DateTime.Now;

					//itBitmap.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

					Bitmap result = itBitmap.GetClip(new Rectangle(i * 100, i * 100, 2000, 2000));

					Console.WriteLine(DateTime.Now.Subtract(start).ToString());

					if (result != null)
						result.Save(@"C:\Users\jirka.stybnar\TestRun\Big Images\result.png", ImageFormat.Png);
					else
						throw new Exception("Result bitmap is null!");

					result.Dispose();
				}
		}
		#endregion

		#region CompareFolders()
		private static void CompareFolders()
		{
			int files = 0;
			int differences = 0;
			string folderName = "Well defined Columns and pictures";
			FileInfo[] files1 = new DirectoryInfo(@"C:\Users\jirka.stybnar\TestRun\IT\" + folderName + @"\results").GetFiles();
			DirectoryInfo dir2 = new DirectoryInfo(@"C:\Users\jirka.stybnar\TestRun\IT\" + folderName + @"\results1");
			DateTime start = DateTime.Now;

			foreach (FileInfo file1 in files1)
			{
				FileInfo[] files2 = dir2.GetFiles(file1.Name);
				if (files2.Length > 0)
				{
					FileInfo file2 = files2[0];

					Bitmap bitmap1 = new Bitmap(file1.FullName);
					Bitmap bitmap2 = new Bitmap(file2.FullName);

					DateTime localStart = DateTime.Now;

					List<Point> differentPoints = ImageComparer.Compare(bitmap1, bitmap2);

					Console.WriteLine(string.Format("CompareFolders(): {0} File: {1}, Differences: {2}, Size: {3}", ""/*DateTime.Now.Subtract(localStart).ToString()*/, file1.Name, differentPoints.Count, (bitmap1.Size == bitmap2.Size) ? "OK" : "Different!!!"));
					differences += differentPoints.Count;
					files++;
				}
				else
					Console.WriteLine(string.Format("CompareFolders(): File {0} doesn't exist!", file1.Name));
			}

			Console.WriteLine(string.Format("CompareFolders: {0}, Files: {1}, Differences: {2}", DateTime.Now.Subtract(start).ToString(), files, differences));
			Console.ReadLine();
		}

		private static void CompareFolders(string directory1, string directory2)
		{
			FileInfo[] files1 = new DirectoryInfo(directory1).GetFiles();
			DirectoryInfo dir2 = new DirectoryInfo(directory2);
			//DateTime start = DateTime.Now;

			foreach (FileInfo file1 in files1)
			{
				FileInfo[] files2 = dir2.GetFiles(file1.Name);
				
				if (files2.Length > 0)
				{
					FileInfo file2 = files2[0];

					Bitmap bitmap1 = new Bitmap(file1.FullName);
					Bitmap bitmap2 = new Bitmap(file2.FullName);

					DateTime localStart = DateTime.Now;

					List<Point> differentPoints = ImageComparer.Compare(bitmap1, bitmap2);

					if (differentPoints.Count > 0)
					{
						Console.WriteLine(string.Format("File: {0}, Differences: {1}, Size: {2}", file1.Name, differentPoints.Count, (bitmap1.Size == bitmap2.Size) ? "OK" : "Different!!!"));
					}
					else
						Console.WriteLine(".");

					bitmap1.Dispose();
					bitmap2.Dispose();
				}
				else
					Console.WriteLine(string.Format("CompareFolders(): File {0} doesn't exist!", file1.Name));
			}

			//Console.WriteLine(string.Format("CompareFolders: {0}, Files: {1}, Differences: {2}", DateTime.Now.Subtract(start).ToString(), files, differences));
			Console.ReadLine();
		}
		#endregion

		#region CompareImages()
		private static void CompareImages()
		{
			Bitmap bitmap1 = new Bitmap(@"C:\delete\01\4 BW.png");
			Bitmap bitmap2 = new Bitmap(@"C:\delete\02\4 BW.png");
			DateTime start = DateTime.Now;

			List<Point> differentPoints = ImageComparer.Compare(bitmap1, bitmap2);

			Console.WriteLine(string.Format("CompareImages: {0}, Different points count: {1}", DateTime.Now.Subtract(start).ToString(), differentPoints.Count));
			Console.ReadLine();
		}
		#endregion

		#region CopyBigImage()
		private unsafe static void CopyBigImage()
		{
			DateTime total = DateTime.Now;

			string source = @"C:\Users\jirka.stybnar\TestRun\Big Images\24 bpp 300dpi.png";
			string dest = @"C:\delete\result.png";

			ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source);
			ImageProcessing.BigImages.ImageCopier copier = new ImageProcessing.BigImages.ImageCopier();
			copier.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

			copier.Copy(itDecoder, dest, new ImageProcessing.FileFormat.Png(), new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));
		}
		#endregion

		#region CreateBook()
		private static void CreateBook()
		{
			FileInfo[] filesArray = new DirectoryInfo(@"C:\Users\jirka.stybnar\personal\books\ASP.NET\ASP.NET MVC 5 with Bootstrap and Knockout").GetFiles("*.png");
			string destFolder = @"C:\Users\jirka.stybnar\personal\books\ASP.NET\ASP.NET MVC 5 with Bootstrap and Knockout\result";
			Directory.CreateDirectory(destFolder);
			//600dpi
			/*Rectangle clip = new Rectangle(500, 300, 3200, 4900);
			int width = 6850;
			int height = 5300;
			Point locationL = new Point(150, 150);
			Point locationR = new Point(3500, 150);*/
			//1200dpi
			Rectangle clip = new Rectangle(1000, 600, 6400, 9800);
			int width = 13700;
			int height = 10600;
			Point locationL = new Point(300, 300);
			Point locationR = new Point(7000, 300);

			BookMaker bookMaker = new BookMaker();
			bookMaker.ProgressChanged += delegate(double progress) { Console.WriteLine("Progress: " + (progress / 100).ToString("00.0") + "%"); };
			bookMaker.Run(width, height, new List<FileInfo>(filesArray), clip, destFolder, locationL, locationR);
		}
		#endregion

		#region CreateThumbnail()
		private unsafe static void CreateThumbnail()
		{
				FileInfo[] images = new DirectoryInfo(@"C:\KICStorage\ImagesH\Export").GetFiles();
				PixelFormat[] formats = new PixelFormat[] { PixelFormat.Format32bppArgb, PixelFormat.Format32bppRgb, PixelFormat.Format24bppRgb,
														PixelFormat.Format8bppIndexed, PixelFormat.Format1bppIndexed};

				foreach (FileInfo image in images)
				{
					Bitmap source = new Bitmap(image.FullName);

					foreach (PixelFormat format in formats)
					{
						//Bitmap result = ImageProcessing.Resizing.GetThumbnail(source, new Size(461, 800));
						//Bitmap thumb = ImageProcessing.ThumbnailCreator.Get(source, format);
						Bitmap thumb = ImageProcessing.Resizing.GetThumbnail(source, new Size(129, 98));


						Directory.CreateDirectory(image.Directory.FullName + @"\thumbs\");
						thumb.Save(image.Directory.FullName + @"\thumbs\" + Path.GetFileNameWithoutExtension(image.Name) + "_" + format.ToString() + ".png", ImageFormat.Png);

						thumb.Dispose();
						Console.Write(".");
					}

					source.Dispose();
				}
		}
		#endregion

		#region CreateThumbnailBigImages()
		private unsafe static void CreateThumbnailBigImages()
		{
				string source = @"C:\OpusFreeFlowWorkingData\00000\060\ITImages\Preview\00000060_000009_2.jpg";
				string dest = @"C:\Users\jirka.stybnar\TestRun\Big Images\result.png";
				DateTime start = DateTime.Now;

				ImageProcessing.BigImages.ThumbnailCreator thumbCreator = new ImageProcessing.BigImages.ThumbnailCreator();
				thumbCreator.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

				ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source);

				thumbCreator.Go(itDecoder, dest, new ImageProcessing.FileFormat.Png(), 0.4);
		}
		#endregion

		#region CropAndDeskew()
		private static void CropAndDeskew()
		{
			string sourceFile = @"C:\delete\0001.jpg";
			string resultFile = @"C:\delete\0001 r.jpg";			
			byte		confidence = 0;
			Bitmap		bitmap = new Bitmap(sourceFile) ;
			Color		color = Color.FromArgb(25, 25, 25); ;
			DateTime	start = DateTime.Now ;

			Bitmap		result = ImageProcessing.CropAndDeskew.Go(bitmap, color, true, out confidence, .1F, 
				Rectangle.Empty, false, 10, 70, 20, 20, 100, 100, 1);
			TimeSpan	time = DateTime.Now.Subtract(start) ;
				
			bitmap.Dispose() ;
			result.Save(resultFile, ImageFormat.Png) ;
			result.Dispose() ;
			
			Console.WriteLine(string.Format("{0}: {1}, Confidence:{2}%",  sourceFile, time.ToString(), confidence)) ;
		}
		#endregion

		#region CropAndDeskewBigImage()
		private unsafe static void CropAndDeskewBigImage()
		{
			byte confidence;
			DateTime start = DateTime.Now;
			string source = @"C:\\Opus4\\WorkingData\\ActiveObjectHive\\00060\\00\\000\\004\\ScanImages\\Full\\00000004_000009.jpg";
			string dest = @"C:\delete\result.png";

			ImageProcessing.BigImages.CropAndDeskew cropAndDescew = new ImageProcessing.BigImages.CropAndDeskew();
			cropAndDescew.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

			using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source))
			{
				ImageProcessing.CropDeskew.CdObject cdObject = cropAndDescew.GetCdObject(itDecoder, Color.FromArgb(100, 100, 100), 
					true, out confidence, 0.1F, Rectangle.Empty, true, 10, 80, 20, 20, 2, 2);

				cropAndDescew.Execute(itDecoder, cdObject, dest, new ImageProcessing.FileFormat.Png(), true);
			}

			Console.WriteLine("CropAndDeskewBigImage(): " + DateTime.Now.Subtract(start).ToString() + ", confidence: " + confidence.ToString());
		}
		#endregion

		#region CurveCorrection()
		private unsafe static void CurveCorrection()
		{
				string source = @"C:\OpusFreeFlowWorkingData\00000\083\ScanImages\Reduced\00000083_000003.jpg";
				float confidence;

				ImageProcessing.ItImage itImage = new ImageProcessing.ItImage(new FileInfo(source));
				itImage.IsFixed = false;
				DateTime totalTime = DateTime.Now;
				DateTime start = DateTime.Now;

				itImage.WhiteThresholdDelta = 0;
				itImage.MinDelta = 20;

				confidence = itImage.Find(new Operations(true, 0.2F, true, true, false));
				Console.WriteLine("CurveCorrection(), Computation time: " + DateTime.Now.Subtract(start).ToString());

				itImage.ReleasePageObjects();
				GC.Collect();

				//itImage.PageL.SetSkew(0, 1);
				//itImage.PageR.SetSkew(0, 1);
				//itImage.OpticsCenter = 0.8;

				Point[] points = new Point[]{new Point(8048,2169), new Point(8379, 2169), new Point(8986, 2160), new Point(9139, 2161), new Point(9780, 2165), new Point(10325, 2163), new Point(11000,2157), new Point(11590,2164), new Point(1200,2166), new Point(12609,2159)};

				itImage.PageR.Bookfolding.TopCurve.SetPoints(points);

				start = DateTime.Now;
				Bitmap bmp1 = itImage.GetResult(0);
				bmp1.Save(ImageProcessing.Debug.SaveToDir + @"Result1.png", ImageFormat.Png);
				bmp1.Dispose();
				Console.WriteLine("CurveCorrection(), Get+Save clip 1 time: " + DateTime.Now.Subtract(start).ToString());

				if (itImage.TwoPages)
				{
					start = DateTime.Now;
					Bitmap bmp2 = itImage.GetResult(0);
					bmp2.Save(ImageProcessing.Debug.SaveToDir + @"Result2.png", ImageFormat.Png);
					bmp2.Dispose();
					Console.WriteLine("CurveCorrection(), Get+Save clip 2 time: " + DateTime.Now.Subtract(start).ToString());
				}

				itImage.Dispose();
		}
		#endregion
	
		#region CurveCorrectionBigImage()
		private unsafe static void CurveCorrectionBigImage()
		{
			try
			{
				string source = @"C:\Users\jirka.stybnar\TestRun\Bookfolding\target 24bpp.png";
				string dest = @"C:\Users\jirka.stybnar\TestRun\Big Images\";

				ImageProcessing.IpSettings.ItImage itImage = new ImageProcessing.IpSettings.ItImage(new FileInfo(source));

				itImage.IsFixed = false;
				itImage.IsIndependent = true;
				itImage.PageL.Activate(new RatioRect(100.0 / 2300, 100.0 / 2200, 1000.0 / 2300, 2000.0 / 2200), false);
				itImage.PageR.Activate(new RatioRect(1200.0 / 2300, 100.0 / 2200, 1000.0 / 2300, 2000.0 / 2200), true);
				itImage.PageL.Bookfolding.SetCurves(new RatioPoint[] { new RatioPoint(100.0 / 2300, 200.0 / 2200), new RatioPoint(400.0 / 2300, 200.0 / 2200), new RatioPoint(900.0 / 2300, 290.0 / 2200), new RatioPoint(1100.0 / 2300, 380.0 / 2200) },
					new RatioPoint[] { new RatioPoint(100.0 / 2300, 2000.0 / 2200), new RatioPoint(400.0 / 2300, 2000.0 / 2200), new RatioPoint(900.0 / 2300, 1910.0 / 2200), new RatioPoint(1100.0 / 2300, 1820.0 / 2200) }, 1.0F);
				itImage.PageR.Bookfolding.SetCurves(new RatioPoint[] { new RatioPoint(1200.0 / 2300, 380.0 / 2200), new RatioPoint(1400.0 / 2300, 290.0 / 2200), new RatioPoint(1900.0 / 2300, 200.0 / 2200), new RatioPoint(2200.0 / 2300, 200.0 / 2200) },
					new RatioPoint[] { new RatioPoint(1200.0 / 2300, 1820.0 / 2200), new RatioPoint(1400.0 / 2300, 1910.0 / 2200), new RatioPoint(1900.0 / 2300, 2000.0 / 2200), new RatioPoint(2200.0 / 2300, 2000.0 / 2200) }, 1.0F);

				DateTime start = DateTime.Now;

				itImage.Execute(source, 0, dest + @"Result1.png", new ImageProcessing.FileFormat.Png());
				itImage.Execute(source, 1, dest + @"Result2.png", new ImageProcessing.FileFormat.Png());
				itImage.Dispose();

				Console.WriteLine("\n\nDone. " + DateTime.Now.Subtract(start).ToString());
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine("\n\n" + ex.Message + "\n\n" + ex.StackTrace);
				Console.ReadLine();
			}
		}	
		#endregion

		#region CurveCorrectionRotationBigImage()
		private unsafe static void CurveCorrectionRotationBigImage()
		{
			try
			{
				DateTime start = DateTime.Now;
				/*string source = @"C:\Users\jirka.stybnar\TestRun\Bookfolding\target skewed big 24bpp.png";
				string dest = @"C:\Users\jirka.stybnar\TestRun\Big Images\result.png";

				ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source);
				ImageProcessing.BigImages.CurveCorrectionAndRotation test = new ImageProcessing.BigImages.CurveCorrectionAndRotation();
				ImageProcessing.IpSettings.ItImage itImage = new ImageProcessing.IpSettings.ItImage(new FileInfo(source));
				
				itImage.PageL.Activate(new RatioRect(4800 / 9200.0, 400 / 8800.0, 4000 / 9200.0, 8000 / 8800.0), false);
				itImage.PageR.Deactivate();
				itImage.PageL.SetSkew(4 * Math.PI / 180.0, 1.0F);

				itImage.Page.Bookfolding.TopCurve.SetPoints(new RatioPoint[] {
					new RatioPoint( 5026 / 9200.0, 1068 / 8800.0),
					new RatioPoint( 5852 / 9200.0, 727 / 8800.0),
					new RatioPoint( 7473 / 9200.0, 468 / 8800.0),
					new RatioPoint( 7875 / 9200.0, 465 / 8800.0),
					new RatioPoint( 8274 / 9200.0, 493 / 8800.0),
					new RatioPoint( 9072 / 9200.0, 549 / 8800.0)
				});
				itImage.Page.Bookfolding.BottomCurve.SetPoints(new RatioPoint[] {
					new RatioPoint( 4580 / 9200.0, 7453 / 8800.0),
					new RatioPoint( 5350 / 9200.0, 7907 / 8800.0),
					new RatioPoint( 6131 / 9200.0, 8217 / 8800.0),
					new RatioPoint( 6921 / 9200.0, 8391 / 8800.0),
					new RatioPoint( 7318 / 9200.0, 8446 / 8800.0),
					new RatioPoint( 7717 / 9200.0, 8474 / 8800.0),
					new RatioPoint( 8515 / 9200.0, 8529 / 8800.0)
				});*/

				string source = @"C:\OpusFreeFlowWorkingData\ActiveObjectHive\00000\089\ScanImages\Reduced\00000089_000001.jpg";
				string dest = @"C:\Users\jirka.stybnar\TestRun\Big Images\result.png";

				ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source);
				ImageProcessing.BigImages.CurveCorrectionAndRotation test = new ImageProcessing.BigImages.CurveCorrectionAndRotation();
				ImageProcessing.IpSettings.ItImage itImage = new ImageProcessing.IpSettings.ItImage(new FileInfo(source));
				
				itImage.PageL.Activate(new RatioRect(1197 / 2300.0, 97 / 2200.0, 1003 / 2300.0, 2004 / 2200.0), false);
				itImage.PageL.SetSkew(4 * Math.PI / 180.0, 1.0F);
				itImage.PageR.Deactivate();

				itImage.Page.Bookfolding.TopCurve.SetPoints(new RatioPoint[] {
					new RatioPoint( .545, .120 ),
					new RatioPoint( .596, .097 ),
					new RatioPoint( .656, .075 ),
					new RatioPoint( .721, .060 ),
					new RatioPoint( .778, .053 ),
					new RatioPoint( .857, .052 ), 
					new RatioPoint( .986, .061 )
				});
				itImage.Page.Bookfolding.BottomCurve.SetPoints(new RatioPoint[] {
					new RatioPoint( .498, .847),
					new RatioPoint( .544, .876),
					new RatioPoint( .579, .897),
					new RatioPoint( .620, .916),
					new RatioPoint( .690, .940),
					new RatioPoint( .766, .955),
					new RatioPoint( .865, .963),
					new RatioPoint( .928, .955)
				});

				
				test.Execute(itDecoder, dest, new ImageProcessing.FileFormat.Png(), itImage.Page);

				itImage.Dispose();

				Console.WriteLine("\n\nDone. " + DateTime.Now.Subtract(start).ToString());
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine("\n\n" + ex.Message + "\n\n" + ex.StackTrace);
				Console.ReadLine();
			}
		}
		#endregion

		#region Despeckle()
		private unsafe static void Despeckle()
		{
			try
			{
				//string file = @"C:\Users\jirka.stybnar\TestRun\IT\Despeckle\noise.png";
				string file = @"C:\Users\jirka.stybnar\TestRun\IT\Despeckle\1bpp.png";
				//string file = @"C:\Users\jirka.stybnar\TestRun\IT\Despeckle\1bpp 300dpi.png";
				//string file = @"C:\Users\jirka.stybnar\TestRun\Big Images\Despeckle 1000x1000.png";
				//string file = @"C:\Users\jirka.stybnar\TestRun\IT\Despeckle\Despeckle.png";
				string dest = @"C:\Users\jirka.stybnar\TestRun\IT\Despeckle\result.png";


				ImageProcessing.NoiseReduction noiseReduction = new ImageProcessing.NoiseReduction();
				noiseReduction.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

				Bitmap bitmap = new Bitmap(file);

				DateTime start = DateTime.Now;
				noiseReduction.Despeckle(bitmap, NoiseReduction.DespeckleSize.Size6x6, NoiseReduction.DespeckleMethod.Regions, ImageProcessing.NoiseReduction.DespeckleMode.BothColors);
				Console.WriteLine("Time: " + DateTime.Now.Subtract(start).ToString());

				bitmap.Save(dest, ImageFormat.Png);
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region DetectEdges()
		//-c:DetectEdges -s:"C:\temp\0000.png" -d:"C:\temp\0000Result.jpg" -p:70,70,70
		private static void DetectEdges(FileInfo sourceFile, FileInfo resultFile, string parameters)
		{
			Bitmap		bitmap = new Bitmap(sourceFile.FullName) ;
			DateTime	start = DateTime.Now ;

			if(parameters == null || parameters.Length == 0)
			{
				//string[]	colors = parameters.Split(new char[] {','});
				//color = Color.FromArgb(Convert.ToInt32(colors[0]), Convert.ToInt32(colors[1]), Convert.ToInt32(colors[2]));
			}

			Bitmap result = ImageProcessing.EdgeDetector.BinarizeLaplacian(bitmap, Rectangle.Empty, 200, 200, 200, 50, true);

			Console.WriteLine(string.Format("{0}: {1}",  sourceFile.FullName, DateTime.Now.Subtract(start).ToString())) ;
				
			bitmap.Dispose() ;
			result.Save(resultFile.FullName, ImageFormat.Png) ;
			result.Dispose() ;
		}
		#endregion

		#region Drs()
		private static void Drs()
		{
			try
			{
				string				source = @"C:\delete\scan_2010-05-07_15-24-45.jpg";
				string				destination = @"C:\delete\result.png";

				Bitmap				bitmap = new Bitmap(source);
				
				DateTime	start = DateTime.Now ;
				Bitmap		result = ImageProcessing.DRS.Binorize(bitmap, 0, 0) ;
				Console.WriteLine(DateTime.Now.Subtract(start).ToString());

				result.Save(destination, ImageFormat.Png);
				bitmap.Dispose() ;
				result.Dispose() ;
			}
			catch(Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message) ;
				Console.ReadLine();
			}
		}
		#endregion
		
		#region Drs2()
		private unsafe static void Drs2()
		{
			FileInfo	source = new FileInfo(@"C:\delete\01.jpg");
			FileInfo	destination = new FileInfo(@"C:\delete\result.png");
			Bitmap		sourceBitmap = new Bitmap(source.FullName);
			DateTime	start = DateTime.Now ;

			Bitmap result = ImageProcessing.DRS2.Get(sourceBitmap, 0, 0, true);
			
			Console.WriteLine(string.Format("DRS2 on {0}: {1}", source.FullName, DateTime.Now.Subtract(start).ToString()));			
			destination.Directory.Create();
			result.Save(destination.FullName, ImageFormat.Png);

			Console.ReadLine();
		}
		#endregion

		#region EdgeDetection()
		private unsafe static void EdgeDetection()
		{
			try
			{
				string sourceFile = @"C:\Documents and Settings\Jirka\My Documents\projects\Image Processing\Binorization\Bookeye Binorization\bi\058922_c.tif";
				//string sourceFile = @"C:\Documents and Settings\Jirka\My Documents\projects\Image Processing\Binorization\Bookeye Binorization\bi\058922_g.png";
				Bitmap source = new Bitmap(sourceFile);
				DateTime start;

				start = DateTime.Now;
				Bitmap result = ImageProcessing.EdgeDetector.Get(source, Rectangle.Empty, EdgeDetector.Operator.MexicanHat7x7);
				Console.WriteLine(string.Format("Edge Detection on {0}: {1}", sourceFile, DateTime.Now.Subtract(start).ToString()));
				
				result.Save(@"C:\Documents and Settings\Jirka\My Documents\projects\Image Processing\Binorization\Bookeye Binorization\bi\result.png", ImageFormat.Png);
				result.Dispose();

				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region Erosion()
		private static void Erosion()
		{
			FileInfo imageFile = new FileInfo(@"C:\Documents and Settings\Jirka\My Documents\temp\IP\01 Preprocessing Dark.png");
			Bitmap image = new Bitmap(imageFile.FullName);
			Rectangle rect = new Rectangle(0, 0, image.Width, image.Height); ;

			/*FileInfo resultFile = new FileInfo(@"C:\Documents and Settings\Jirka\My Documents\temp\IP\01 Preprocessing Dark Get.png");
			Bitmap result1 = ImageProcessing.Dilation.Get(image, Rectangle.Empty, ImageProcessing.Dilation.Operator.Full);
			result1.Save(resultFile.FullName, ImageFormat.Png);*/

			/*FileInfo resultFile = new FileInfo(@"C:\Documents and Settings\Jirka\My Documents\temp\IP\01 Preprocessing Dark Go.png");
			ImageProcessing.Dilation.Go(image, Rectangle.Empty, ImageProcessing.Dilation.Operator.Full);
			image.Save(resultFile.FullName, ImageFormat.Png);*/

			FileInfo resultFile = new FileInfo(@"C:\Documents and Settings\Jirka\My Documents\temp\IP\01 Preprocessing Dark Get.png");
			Bitmap result1 = ImageProcessing.Erosion.Get(image, Rectangle.Empty, ImageProcessing.Erosion.Operator.Full);
			result1.Save(resultFile.FullName, ImageFormat.Png);

			/*FileInfo resultFile = new FileInfo(@"C:\Documents and Settings\Jirka\My Documents\temp\IP\01 Preprocessing Dark Go.png");
			ImageProcessing.Erosion.Go(image, Rectangle.Empty, ImageProcessing.Erosion.Operator.Full);
			image.Save(resultFile.FullName, ImageFormat.Png);*/

			Console.ReadLine();
		}
		#endregion

		#region ExampleChangedByte()
		private static void ExampleChangedByte()
		{
			try
			{
				string source = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\E\source.jpg";
				string result = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\E\result.jpg";
				DateTime start = DateTime.Now;

				if (File.Exists(result))
					File.Delete(result);

				File.Copy(source, result);

				//1 byte change produces image error
				/*using (FileStream fileStream = new FileStream(result, FileMode.Open, FileAccess.ReadWrite))
				{
					fileStream.Seek(fileStream.Length / 2 - 6, SeekOrigin.Begin);
					int b = fileStream.ReadByte();
					fileStream.WriteByte(255);
					//fileStream.WriteByte((byte)(b | 0x80));
				} */
				//1 bit change produces line error
				using (FileStream fileStream = new FileStream(result, FileMode.Open, FileAccess.ReadWrite))
				{
					fileStream.Seek(fileStream.Length / 2 + 2003, SeekOrigin.Begin);
					int b = fileStream.ReadByte();
					fileStream.WriteByte((byte)(b | 0x80));
				}

				Console.WriteLine(string.Format("\n\nDone. Time{0}", DateTime.Now.Subtract(start).ToString()));
				//Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region ExampleCompressionDataLoosing()
		private static void ExampleCompressionDataLoosing()
		{
			try
			{
				int steps = 100;
				long quality = 80;
				string source = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\D\source.png";
				string dest = string.Format(@"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\D\dest after {0} steps, quality {1}.jpg", steps, quality);
				string tempDir = string.Format(@"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\D\temp{0} steps, quality{1}\", steps, quality);
				DateTime start = DateTime.Now;

				ImageCodecInfo codecInfo = ImageProcessing.Encoding.GetCodecInfo(ImageFormat.Jpeg);
				EncoderParameters encoderParams = ImageProcessing.Encoding.GetEncoderParams(ImageFormat.Jpeg, quality);
				
				Bitmap s = new Bitmap(source);

				Directory.CreateDirectory(tempDir);

				for (int i = 0; i < steps - 1; i++)
				{
					string name = i.ToString() + ".jpg";
					s.Save(tempDir + name, codecInfo, encoderParams);

					s.Dispose();
					s = new Bitmap(tempDir + name);
					Console.Write(".");
				}

				s.Save(dest, ImageFormat.Jpeg);

				Console.WriteLine(string.Format("\n\nDone. Time{0}", DateTime.Now.Subtract(start).ToString()));
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region ExampleDoubleDpi()
		private unsafe static void ExampleDoubleDpi()
		{
			try
			{
				string source = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\G\rainbow small 2.tif";
				string result = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\G\rainbow big.png";
				DateTime start = DateTime.Now;

				Bitmap s = new Bitmap(source);
				BitmapData sbd = s.LockBits(new Rectangle(Point.Empty, s.Size), ImageLockMode.ReadOnly, s.PixelFormat);
				Bitmap d = new Bitmap(s.Width * 2, s.Height * 2, PixelFormat.Format24bppRgb);
				BitmapData dbd = d.LockBits(new Rectangle(Point.Empty, d.Size), ImageLockMode.ReadOnly, d.PixelFormat);

				unsafe
				{
					byte* pSource = (byte*)sbd.Scan0.ToPointer();
					byte* pResult = (byte*)dbd.Scan0.ToPointer();

					for (int y = 0; y < sbd.Height; y++)
						for (int x = 0; x < sbd.Width; x++)
						{
							pResult[y * 2 * dbd.Stride + x * 6 + 0] = pSource[y * sbd.Stride + x * 3 + 0];
							pResult[y * 2 * dbd.Stride + x * 6 + 1] = pSource[y * sbd.Stride + x * 3 + 1];
							pResult[y * 2 * dbd.Stride + x * 6 + 2] = pSource[y * sbd.Stride + x * 3 + 2];
							pResult[y * 2 * dbd.Stride + x * 6 + 3] = pSource[y * sbd.Stride + x * 3 + 0];
							pResult[y * 2 * dbd.Stride + x * 6 + 4] = pSource[y * sbd.Stride + x * 3 + 1];
							pResult[y * 2 * dbd.Stride + x * 6 + 5] = pSource[y * sbd.Stride + x * 3 + 2];
							pResult[(y * 2 + 1) * dbd.Stride + x * 6 + 0] = pSource[y * sbd.Stride + x * 3 + 0];
							pResult[(y * 2 + 1) * dbd.Stride + x * 6 + 1] = pSource[y * sbd.Stride + x * 3 + 1];
							pResult[(y * 2 + 1) * dbd.Stride + x * 6 + 2] = pSource[y * sbd.Stride + x * 3 + 2];
							pResult[(y * 2 + 1) * dbd.Stride + x * 6 + 3] = pSource[y * sbd.Stride + x * 3 + 0];
							pResult[(y * 2 + 1) * dbd.Stride + x * 6 + 4] = pSource[y * sbd.Stride + x * 3 + 1];
							pResult[(y * 2 + 1) * dbd.Stride + x * 6 + 5] = pSource[y * sbd.Stride + x * 3 + 2];
						}
				}

				d.Save(result, ImageFormat.Png);

				Console.WriteLine(string.Format("\n\nDone. Time{0}", DateTime.Now.Subtract(start).ToString()));
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion
	
		#region ExampleInterpolation()
		private static void ExampleInterpolation()
		{
			try
			{
				string source = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\G\Result 300dpi source.png";
				string result = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\G\Result 600dpi interpolated.png";
				DateTime start = DateTime.Now;

				Bitmap b = new Bitmap(source);
				Bitmap r = ImageProcessing.Interpolation.Interpolate(b, ImageProcessing.Interpolation.Zoom.Zoom2to1);

				r.Save(result, ImageFormat.Png);

				Console.WriteLine(string.Format("\n\nDone. Time{0}", DateTime.Now.Subtract(start).ToString()));
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region ExampleRotationLoosingData()
		private unsafe static void ExampleRotationLoosingData()
		{
			try
			{
				string source = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\C rotation\source.tif";
				int steps = 360;
				string dest = string.Format(@"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\C rotation\dest {0} degree inceremts.png", (int)(360 / steps));
				string tempDir = string.Format(@"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\C rotation\temp {0} degree inceremts\", (int)(360 / steps));
				double angle = (360 / steps) * Math.PI / 180.0;
				DateTime start = DateTime.Now;

				Bitmap s = new Bitmap(source);
				Bitmap d = null;

				Directory.CreateDirectory(tempDir);

				for (int i = 0; i < steps; i++)
				{
					d = ImageProcessing.Rotation.Rotate(s, angle, 255, 255, 255);
					d.Save(tempDir + i.ToString() + ".png", ImageFormat.Png);

					s.Dispose();
					s = d;
					Console.Write(".");
				}

				d.Save(dest, ImageFormat.Png);

				Console.WriteLine(string.Format("\n\nDone. Time{0}", DateTime.Now.Subtract(start).ToString()));
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region ExampleRotationStepsLoosingData()
		private unsafe static void ExampleRotationStepsLoosingData()
		{
			try
			{
				int steps = 100;
				int angle = 1;
				double angleRad = angle * Math.PI / 180.0;
				string source = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\J rotation couple of steps\source.tif";
				string dest = string.Format(@"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\J rotation couple of steps\angle {0} steps {1}.png", angle, steps);
				string tempDir = string.Format(@"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\J rotation couple of steps\angle {0} steps {1}\", angle, steps);
				DateTime start = DateTime.Now;

				Bitmap s = new Bitmap(source);
				Bitmap d = null;

				Directory.CreateDirectory(tempDir);

				for (int i = 0; i < steps; i++)
				{
					d = ImageProcessing.Rotation.Rotate(s, angleRad, 255, 255, 255);
					d.Save(tempDir + i.ToString() + ".png", ImageFormat.Png);

					s.Dispose();
					s = d;
					Console.Write(".");
				}

				d = ImageProcessing.Rotation.Rotate(s, -(steps * angleRad), 255, 255, 255);
				s.Dispose();

				d.Save(dest, ImageFormat.Png);

				Console.WriteLine(string.Format("\n\nDone. Time{0}", DateTime.Now.Subtract(start).ToString()));
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region ExampleShiftHalfPixel()
		private static void ExampleShiftHalfPixel()
		{
			try
			{
				string source = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\B stitching problems\sour.png";
				string result = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\B stitching problems\soura.png";
				DateTime start = DateTime.Now;

				Bitmap s = new Bitmap(source);
				BitmapData sbd = s.LockBits(new Rectangle(Point.Empty, s.Size), ImageLockMode.ReadOnly, s.PixelFormat);
				Bitmap d = new Bitmap(s.Width, s.Height, PixelFormat.Format24bppRgb);
				BitmapData dbd = d.LockBits(new Rectangle(Point.Empty, d.Size), ImageLockMode.ReadOnly, d.PixelFormat);

				unsafe
				{
					byte* pSource = (byte*)sbd.Scan0.ToPointer();
					byte* pResult = (byte*)dbd.Scan0.ToPointer();

					for (int y = 0; y < sbd.Height; y++)
						for (int x = 0; x < sbd.Width; x++)
						{
							if (x < sbd.Width - 1)
							{
								pResult[y * dbd.Stride + x * 3 + 0] = (byte)((pSource[y * sbd.Stride + x * 3 + 0] + pSource[y * sbd.Stride + (x + 1) * 3 + 0]) / 2);
								pResult[y * dbd.Stride + x * 3 + 1] = (byte)((pSource[y * sbd.Stride + x * 3 + 1] + pSource[y * sbd.Stride + (x + 1) * 3 + 1]) / 2);
								pResult[y * dbd.Stride + x * 3 + 2] = (byte)((pSource[y * sbd.Stride + x * 3 + 2] + pSource[y * sbd.Stride + (x + 1) * 3 + 2]) / 2);
							}
							else
							{
								pResult[y * dbd.Stride + x * 3 + 0] = pSource[y * sbd.Stride + x * 3 + 0];
								pResult[y * dbd.Stride + x * 3 + 1] = pSource[y * sbd.Stride + x * 3 + 1];
								pResult[y * dbd.Stride + x * 3 + 2] = pSource[y * sbd.Stride + x * 3 + 2];
							}
						}
				}

				d.Save(result, ImageFormat.Png);

				Console.WriteLine(string.Format("\n\nDone. Time{0}", DateTime.Now.Subtract(start).ToString()));
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region ExampleStitching()
		private static void ExampleStitching()
		{
			try
			{
				string source1 = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\B\stitching source 1.png";
				string source2 = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\B\stitching source 3.png";
				string result = @"C:\Documents and Settings\Jirka\My Documents\projects\DLSG book\B\stitching.png";
				int range = 60;
				DateTime start = DateTime.Now;

				Bitmap s1 = new Bitmap(source1);
				BitmapData sbd1 = s1.LockBits(new Rectangle(Point.Empty, s1.Size), ImageLockMode.ReadOnly, s1.PixelFormat);
				Bitmap s2 = new Bitmap(source2);
				BitmapData sbd2 = s2.LockBits(new Rectangle(Point.Empty, s2.Size), ImageLockMode.ReadOnly, s2.PixelFormat);
				Bitmap d = new Bitmap(s1.Width + s2.Width - range, s1.Height, PixelFormat.Format24bppRgb);
				BitmapData dbd = d.LockBits(new Rectangle(Point.Empty, d.Size), ImageLockMode.ReadOnly, d.PixelFormat);

				unsafe
				{
					byte* pSource1 = (byte*)sbd1.Scan0.ToPointer();
					byte* pSource2 = (byte*)sbd2.Scan0.ToPointer();
					byte* pResult = (byte*)dbd.Scan0.ToPointer();

					for (int y = 0; y < sbd1.Height; y++)
					{
						for (int x = 0; x < sbd1.Width - range; x++)
						{
							pResult[y * dbd.Stride + x * 3 + 0] = pSource1[y * sbd1.Stride + x * 3 + 0];
							pResult[y * dbd.Stride + x * 3 + 1] = pSource1[y * sbd1.Stride + x * 3 + 1];
							pResult[y * dbd.Stride + x * 3 + 2] = pSource1[y * sbd1.Stride + x * 3 + 2];
						}
						for (int x = dbd.Width - sbd2.Width + range; x < dbd.Width; x++)
						{
							pResult[y * dbd.Stride + x * 3 + 0] = pSource2[y * sbd2.Stride + (x - (dbd.Width - sbd2.Width)) * 3 + 0];
							pResult[y * dbd.Stride + x * 3 + 1] = pSource2[y * sbd2.Stride + (x - (dbd.Width - sbd2.Width)) * 3 + 1];
							pResult[y * dbd.Stride + x * 3 + 2] = pSource2[y * sbd2.Stride + (x - (dbd.Width - sbd2.Width)) * 3 + 2];
						}

						int xFrom = sbd1.Width - range;
						for (int x = sbd1.Width - range; x < sbd1.Width; x++)
						{
							double b1 = pSource1[y * sbd1.Stride + x * 3 + 0] * ( 1.0 - ((x - xFrom) / (double)range));
							double g1 = pSource1[y * sbd1.Stride + x * 3 + 1] * ( 1.0 - ((x - xFrom) / (double)range));
							double r1 = pSource1[y * sbd1.Stride + x * 3 + 2] * ( 1.0 - ((x - xFrom) / (double)range));
							double b2 = pSource2[y * sbd2.Stride + (x - xFrom) * 3 + 0] * ((x - xFrom) / (double)range);
							double g2 = pSource2[y * sbd2.Stride + (x - xFrom) * 3 + 1] * ((x - xFrom) / (double)range);
							double r2 = pSource2[y * sbd2.Stride + (x - xFrom) * 3 + 2] * ((x - xFrom) / (double)range);
							
							pResult[y * dbd.Stride + x * 3 + 0] = (byte)(b1 + b2);
							pResult[y * dbd.Stride + x * 3 + 1] = (byte)(g1 + g2);
							pResult[y * dbd.Stride + x * 3 + 2] = (byte)(r1 + r2);
						}
					}
				}

				d.Save(result, ImageFormat.Png);

				Console.WriteLine(string.Format("\n\nDone. Time{0}", DateTime.Now.Subtract(start).ToString()));
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region FindCrop()
		private unsafe static void FindCrop()
		{
			try
			{
				string file = @"C:\Users\jirka.stybnar\TestRun\CropAndDeskew BW\Rotate 5000x5000-.png";

				Bitmap bitmap = new Bitmap(file);
				DateTime start = DateTime.Now;

				ImageProcessing.CropDeskew.Crop crop = new ImageProcessing.CropDeskew.Crop(bitmap);

				bitmap.Dispose();
				Console.WriteLine("CropAndDeskewBigImage(): " + DateTime.Now.Subtract(start).ToString());
				Console.ReadLine();			
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			} 
		}
		#endregion

		#region FindPageSplitterDir()
		private unsafe static void FindPageSplitterDir()
		{
			try
			{
				string[] files = Directory.GetFiles(@"C:\Users\jirka.stybnar\TestRun\IT\PageSplitter");

				for (int i = 0; i < files.Length; i++)
				{
					try
					{
						int splitterL, splitterR;
						DateTime start = DateTime.Now;

						double confidence = BIP.Books.PagesSplitter.FindPagesSplitterStatic(new FileInfo(files[i]), out splitterL, out splitterR);

						Console.WriteLine(string.Format("File: {3}, Time: {0}, Left: {1}, Right: {2}, Confidence: {3}", 
							new DateTime(DateTime.Now.Subtract(start).Ticks).ToString("mm:ss,ff"), splitterL, splitterR, confidence, new FileInfo(files[i]).Name));
					}
					catch(Exception ex)
					{
						Console.WriteLine(string.Format("Error {0} in: {1}", ex.Message, files[i]));
					}
				}

				Console.WriteLine(Environment.NewLine + "Done.");
				Console.ReadLine();			
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			} 
		}
		#endregion

		#region FindPageSplitter()
		private unsafe static void FindPageSplitter()
		{
			try
			{
				string file = @"C:\Users\jirka.stybnar\TestRun\IT\PageSplitter\00.tif";

				int			splitterL, splitterR;
				DateTime	start = DateTime.Now;
				double		confidence = BIP.Books.PagesSplitter.FindPagesSplitterStatic(new FileInfo(file), out splitterL, out splitterR);

				Console.WriteLine(string.Format("FindPageSplitter(): {0}, Left: {1}, Right: {2}, Confidence: {3}", DateTime.Now.Subtract(start).ToString(), 
					splitterL, splitterR, confidence));
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion
	
		#region FingerRemoval()
		private static void FingerRemoval()
		{
			try
			{
				FileInfo sourceFile = new FileInfo(@"C:\Users\jirka.stybnar\TestRun\FingerRemoval\01.png");
				Bitmap bitmap = new Bitmap(sourceFile.FullName);
				DateTime start = DateTime.Now;

				ImageProcessing.FingerRemoval.EraseFinger(bitmap, Rectangle.Empty, new Rectangle(9,8, 22, 9));

				Console.WriteLine(string.Format("{0}: {1}", sourceFile.FullName, DateTime.Now.Subtract(start).ToString()));

				bitmap.Save(@"C:\Users\jirka.stybnar\TestRun\FingerRemoval\r.png", ImageFormat.Png);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		
		/*private static void FingerRemoval()
		{
			FileInfo sourceFile = new FileInfo(@"C:\Users\jirka.stybnar\TestRun\FingerRemoval\01.png");
			Bitmap bitmap = new Bitmap(sourceFile.FullName);
			DateTime start = DateTime.Now;

			ItImage itImage = new ItImage(sourceFile);
			itImage.SetTo1Clip(new Rectangle(330,200,1700,2300));
			//itImage.SetClip(new Rectangle(2000,400, 1560, 2400));
			itImage.Page.SetSkew(-0.122, 1.0F);
			byte confidence;
			Fingers fingers = ImageProcessing.FingerRemoval.FindFingers(bitmap, itImage.Page, Paging.Left, 20, 0, out confidence);
			itImage.Page.AddFinger(fingers[0]);

			Console.WriteLine(string.Format("{0}: {1}", sourceFile.FullName, DateTime.Now.Subtract(start).ToString()));

			Bitmap result = itImage.GetResult(0);

			result.Save(@"C:\bscanill\Apps\ILLScan\10000135\0009R.TIF", ImageFormat.Tiff);

			itImage.DisposeBitmap();
			//Console.ReadLine();
		}*/
		#endregion

		#region FingerRemovalBigImage()
		private unsafe static void FingerRemovalBigImage()
		{
			try
			{
				string source = @"C:\Users\jirka.stybnar\TestRun\DRS2\0005.TIF";
				string dest = ImageProcessing.Debug.SaveToDir;

				ImageProcessing.IpSettings.ItImage itImage = new ImageProcessing.IpSettings.ItImage(new FileInfo(source));
				itImage.ExecutionProgressChanged += new ProgressHnd(ProgressChanged);
				itImage.IsFixed = false;
				DateTime start = DateTime.Now;

				itImage.PageL.Activate(new RatioRect(388 / 5040.0, 83 / 3158.0, 2140 / 5040.0, 3016 / 3158.0), false);
				itImage.PageR.Activate(new RatioRect(2516 / 5040.0, 87 / 3158.0, 2160 / 5040.0, 3000 / 3158.0), false);
				//itImage.PageL.SetSkew(-5 * Math.PI / 180, 1.0F);
				//itImage.PageR.SetSkew(5 * Math.PI / 180, 1.0F);

				itImage.Find(source, new Operations(false, 0.2F, false, false, true));

				itImage.Execute(source, 0, dest + @"Result1.png", new ImageProcessing.FileFormat.Png());

				if (itImage.TwoPages)
					itImage.Execute(source, 1, dest + @"result2.png", new ImageProcessing.FileFormat.Png());

				itImage.Dispose();

				Console.WriteLine("\n\n FingerRemovalBigImage() Done. " + DateTime.Now.Subtract(start).ToString());
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine("\n\n" + ex.Message + "\n\n" + ex.StackTrace);
				Console.ReadLine();
			}
		}
		#endregion

		#region FourierTransform()
		private unsafe static void FourierTransform()
		{
			try
			{
				string file = @"C:\Documents and Settings\Jirka\My Documents\temp\IP\smooth edges highlited.png";

				Bitmap bitmap = new Bitmap(file);

				//ImageProcessing.FourierTransform fourierTransform = new ImageProcessing.FourierTransform();
				ImageProcessing.Transforms.Fourier fourierTransform = new ImageProcessing.Transforms.Fourier();
				fourierTransform.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

				DateTime start = DateTime.Now;
				fourierTransform.LoadBitmap(bitmap);

				Console.WriteLine("FourierTransform(): " + DateTime.Now.Subtract(start).ToString());

				fourierTransform.DrawToFile(Debug.SaveToDir + "Fourier Real.png", ImageProcessing.Transforms.Fourier.DrawingComponent.Real);
				fourierTransform.DrawToFile(Debug.SaveToDir + "Fourier Imaginary.png", ImageProcessing.Transforms.Fourier.DrawingComponent.Imaginary);
				fourierTransform.DrawToFile(Debug.SaveToDir + "Fourier.png", ImageProcessing.Transforms.Fourier.DrawingComponent.PowerSpectrum);

				//fourierTransform.Despeckle();
				
				fourierTransform.DrawToFile(Debug.SaveToDir + "Fourier Real after Despeckle.png", ImageProcessing.Transforms.Fourier.DrawingComponent.Real);
				fourierTransform.DrawToFile(Debug.SaveToDir + "Fourier Imaginary after Despeckle.png", ImageProcessing.Transforms.Fourier.DrawingComponent.Imaginary);
				fourierTransform.DrawToFile(Debug.SaveToDir + "Fourier after Despeckle.png", ImageProcessing.Transforms.Fourier.DrawingComponent.PowerSpectrum);

				Bitmap result = fourierTransform.GetBitmap();

				result.Save(Debug.SaveToDir + "Fourier Inverted.png", ImageFormat.Png);

				bitmap.Dispose();
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region GetEncoderInfo()
		public static ImageCodecInfo GetEncoderInfo(Bitmap image)
		{
			ImageFormat			imageFormat = new ImageFormat(ImageFormat.Tiff.Guid) ; // image.RawFormat.Guid) ;
			ImageCodecInfo[]	encoders = ImageCodecInfo.GetImageEncoders();

			for(int j = 0; j < encoders.Length; ++j)
			{
				if(encoders[j].FormatID == imageFormat.Guid)
					return encoders[j];
			}
			return null;
		}
		#endregion

		#region GetEncoderParameters()
		public static EncoderParameters GetEncoderParameters(Bitmap image)
		{
			ImageFormat			imageFormat = new ImageFormat(image.RawFormat.Guid) ;
			EncoderParameters	encoderParams ;

			if(imageFormat.Guid == ImageFormat.Tiff.Guid)
			{
				byte[]				compressionValue = image.GetPropertyItem(0x0103).Value ;
				Int32				compression = (Int32) compressionValue[1] * 32768 + compressionValue[0] ;
				
				encoderParams = new EncoderParameters(1) ; 

				switch(compression)
				{
					case 1 :
						encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionNone) ; break ;
					case 3 :
						encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionCCITT3) ; break ;
					case 4 :
						encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionCCITT4) ; break ;
					case 5 :
						encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionLZW) ; break ;
					case 6 :
						throw new Exception("Tiff image with Jpeg compression is not supported in GDI+ !") ;
					case 32773 :
						encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionRle) ; break ;
					default :
						throw new Exception("Unsupported Tiff Compression!") ;
				}			
			}
			else if (imageFormat.Guid == ImageFormat.Jpeg.Guid)
			{
				if(image.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					encoderParams = new EncoderParameters(1) ;
					encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 8L) ;
				}
				else
				{
					encoderParams = new EncoderParameters(1) ;
					encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 95L) ;
				}
			}
			else
			{
				encoderParams = new EncoderParameters(0) ;
			}
						
			return encoderParams ;
		}
		#endregion

		#region GetHistogram()
		private static void GetHistogram()
		{
			FileInfo sourceFile = new FileInfo(@"C:\delete\01.jpg");
			Bitmap bitmap = new Bitmap(sourceFile.FullName);
			DateTime start = DateTime.Now;

			Histogram h = new Histogram(bitmap, new System.Drawing.Rectangle(1353, 2119, 2, 2));
			Color textThreshold = h.Threshold;

			Console.WriteLine(string.Format("{0}: {1}, {2}", sourceFile.Name, DateTime.Now.Subtract(start).ToString(), textThreshold.ToString()));

			bitmap.Dispose();
		}
		#endregion

		#region GetPaletteColors()
		private unsafe static void GetPaletteColors()
		{
			string source = @"C:\Users\jirka.stybnar\TestRun\FileFormats\JPEG\24 bpp\0001.jpg";

			ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source);
			ImageProcessing.ColorPalettes.PaletteBuilder paletteBuilder = new ImageProcessing.ColorPalettes.PaletteBuilder();
			paletteBuilder.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

			Color[] palette = paletteBuilder.GetPalette256(itDecoder);
			byte[, ,] inversePalette = ImageProcessing.ColorPalettes.PaletteBuilder.GetInversePalette32x32x32(palette);

			inversePalette = null;
		}
		#endregion

		#region GetTextColor()
		private unsafe static void GetTextColor()
		{
			FileInfo sourceFile = new FileInfo(@"C:\Users\jirka.stybnar\TestRun\IT\Book Color Text\0001.TIF");
			Bitmap bitmap = new Bitmap(sourceFile.FullName);
			DateTime start = DateTime.Now;

			Histogram h = new Histogram(bitmap);
			Color textThreshold = h.Threshold;

			Console.WriteLine(string.Format("{0}: {1}, {2}", sourceFile.Name, DateTime.Now.Subtract(start).ToString(), textThreshold.ToString()));

			bitmap.Dispose();
		}
		#endregion

		#region ImageInfo()
		private unsafe static void ImageInfo()
		{
			DateTime start = DateTime.Now;

			foreach (string filePath in Directory.GetFiles(@"C:\delete\Kodak", "*.jpg"))
			{
				ImageProcessing.ImageFile.ImageInfo imageInfo = new ImageProcessing.ImageFile.ImageInfo(filePath);

				Console.WriteLine(filePath + Environment.NewLine +
					"Width = " + imageInfo.Width + Environment.NewLine +
					"Height = " + imageInfo.Height + Environment.NewLine +
					"DpiX = " + imageInfo.DpiH + Environment.NewLine +
					"DpiY = " + imageInfo.DpiV + Environment.NewLine +
					"PixelsFormat = " + imageInfo.PixelsFormat + Environment.NewLine +
					"PixelFormat = " + imageInfo.PixelFormat + Environment.NewLine + Environment.NewLine
					);
			}

			Console.WriteLine(DateTime.Now.Subtract(start).ToString());
		}
		#endregion

		#region Interpolation()
		private static void Interpolation()
		{
			FileInfo imageFile = new FileInfo(@"C:\Users\jirka.stybnar\TestRun\Interpolation\DRS 2.tif");
			FileInfo resultFile = new FileInfo(@"C:\Users\jirka.stybnar\TestRun\Interpolation\result.png");
			
			Bitmap image = new Bitmap(imageFile.FullName);
			Bitmap result = ImageProcessing.Interpolation.Interpolate1bppTo8bpp2to1(image);
			image.Dispose();
			result.Save(resultFile.FullName, ImageFormat.Png);

			Console.ReadLine();
		}
		#endregion

		#region LightingCorrection()
		private static void LightingCorrection()
		{
			FileInfo imageFile = new FileInfo(@"C:\Documents and Settings\Jirka\My Documents\projects\Vanderbilt\Scan-2007-06-28_16-49-04.jpg");
			FileInfo resultFile = new FileInfo(@"C:\Documents and Settings\Jirka\My Documents\projects\Vanderbilt\result.png");
			Bitmap image = new Bitmap(imageFile.FullName);
			DateTime superStart = DateTime.Now;
			DateTime start = DateTime.Now;

			start = DateTime.Now;
			Histogram h = new Histogram(image);
			Color background = h.GetOtsuBackground();
			Color threshold = Color.FromArgb(background.R - 20, background.G - 20, background.B - 20);
			Console.WriteLine(string.Format("LightingCorrection Get background: {0}", DateTime.Now.Subtract(start).ToString()));
			
			start = DateTime.Now;
			LightDistributor.ColorDelta[,] gradient = LightDistributor.AnalyzeImage(image, background, threshold, 20, LightDistributor.Gradient.PageOrientation.Unknown);
			Console.WriteLine(string.Format("LightingCorrection Analyze: {0}", DateTime.Now.Subtract(start).ToString()));

			start = DateTime.Now;
			ImageProcessing.LightDistributor.Fix(image, gradient, threshold, -20);
			Console.WriteLine(string.Format("LightingCorrection Fix: {0}", DateTime.Now.Subtract(start).ToString()));
			Console.WriteLine(string.Format("LightingCorrection Total Time: {0}", DateTime.Now.Subtract(superStart).ToString()));

			image.Save(resultFile.FullName, ImageFormat.Png);
			Console.ReadLine();
		}	
		#endregion

		#region LocateDocument()
		private static void LocateDocument()
		{
			FileInfo imageFile = new FileInfo(@"C:\delete\00.jpg");
			Bitmap image = new Bitmap(imageFile.FullName);
			DateTime start = DateTime.Now;
			Rectangle rect = new Rectangle(0, 0, image.Width, image.Height); ;

			//bool result = ImageProcessing.DocumentLocator.IsBlackAroundDocument(image, Rectangle.Empty, 20);
			//Console.WriteLine(string.Format("LocateDocument: {0}, Returned: {1}, Result: {2}", DateTime.Now.Subtract(start).ToString(), result.ToString(), rect.ToString()));

			Rectangle zone = new Rectangle(0, 0, image.Width, image.Height);
			zone.Inflate((int)-image.HorizontalResolution, (int)-image.VerticalResolution);

			bool result = ImageProcessing.DocumentLocator.SeekDocument(image, Rectangle.Empty, out rect);
			Console.WriteLine(string.Format("LocateDocument: {0}, Returned: {1}, Result: {2}", DateTime.Now.Subtract(start).ToString(), result.ToString(), rect.ToString()));
			
			Console.ReadLine();
		}
		#endregion

		#region Log()
		private static void Log(FileInfo image, string error)
		{
			try
			{
				FileStream stream = new FileStream(Application.StartupPath + @"\log.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
				StreamWriter writer = new StreamWriter(stream);

				stream.Seek(0, SeekOrigin.End);
				writer.WriteLine(image.FullName + "   Error: " + error);
				writer.Close();
				stream.Close();
			}
			catch (Exception ex)
			{
				throw new Exception(image.FullName + "     Error: " + ex.Message);
			}
		}
		#endregion

		#region ObjectMap()
		private static void ObjectMap()
		{
			try
			{
				Bitmap bitmap = new Bitmap(@"C:\Documents and Settings\Jirka.Stybnar\My Documents\temp\ip\02 Preprocessing No Borders.png");
				BitmapData bmpData = bitmap.LockBits(new Rectangle(0,0,bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				Size imageSize = bitmap.Size;

				DateTime start = DateTime.Now;
				ObjectMap objectMap = ImageProcessing.ObjectMap.GetObjectMap(bmpData, new Rectangle(468, 1966, 1163, 467), new Point(1200, 2400));
				Console.WriteLine(string.Format("Time: {0}", DateTime.Now.Subtract(start).ToString()));

				bitmap.UnlockBits(bmpData);
				bitmap.Dispose();
				
				bitmap = new Bitmap(imageSize.Width, imageSize.Height, PixelFormat.Format24bppRgb);
				bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

				objectMap.DrawToImage(Color.Blue, bmpData);

				bitmap.UnlockBits(bmpData);
				bitmap.Save(ImageProcessing.Debug.SaveToDir + @"\99 Object Map.png", ImageFormat.Png);
				bitmap.Dispose();
				
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region PageTurning()
		//-c:CD -s:"C:\temp\delete\1.tif" -d:"C:\temp\delete\1R.tif" -p:70,70,70,80,.1,16
		/*private unsafe static void PageTurning()
		{
			ItImage itImage = new ItImage(new FileInfo(@"C:\Opus3WorkingData\ImageHive\ScannedImages\00000003\test3_0001.jpg"), false);

			ImageProcessing.Operations operations = new Operations(true, 0, 0, Color.FromArgb(50, 50, 50), 0, 0, 0, false, 20, 20, 20, 20);

			itImage.Reset(false);
			itImage.CreatePageObjects(Rectangle.Empty);

			float confidence = itImage.Find(operations);
			float pagesConfidence = 1.0f;

			ImageProcessing.PageObjects.Pages pages = ImageProcessing.PageObjects.ObjectLocator.FindPages(
				itImage.Page.LoneObjects, itImage.Page.Words, itImage.Page.Pictures,
				itImage.Page.Delimiters, itImage.Page.Paragraphs, itImage.ImageSize, ref pagesConfidence);

			pages = pages;
		}*/
		#endregion

		#region PostProcessing()
		private static void PostProcessing()
		{
			try
			{
				string source = @"C:\Opus4\WorkingData\ActiveObjectHive\00\000\525\ScanImages\Full\00000525_000021.Jpg";
				string destination = @"C:\Opus4\WorkingData\ActiveObjectHive\00\000\525\ITImages\Full\00000525_000021_1 xxx.jpg";

				ImageProcessing.IpSettings.ItImage itImage = new ImageProcessing.IpSettings.ItImage(new FileInfo(source), false);
				itImage.PostProcessing.ItRotation.Angle = (PostProcessing.Rotation.RotationMode)90;
				//itImage.ChangedSinceLastReset = true;
				itImage.IsFixed = false;
				itImage.Execute(source, 0, destination, new ImageProcessing.FileFormat.Png());
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex.Message);
			}
		}
		#endregion

		#region ProgressChanged()
		private static void ProgressChanged(float progress)
		{
			Console.WriteLine(string.Format("{0}, {1:00.00}%", DateTime.Now.ToString("HH:mm:ss,ff"), progress * 100.0));
		}
		#endregion

		#region RemoveGhostLines()
		private static void RemoveGhostLines()
		{
			try
			{
				FileInfo file = new FileInfo(@"T:\ToJirka\crop problem\ADF\0024-orig.jpg");
				Console.WriteLine("\n*** Image " + file.Name + " ***");
				Bitmap bitmap = new Bitmap(file.FullName);
				DateTime start = DateTime.Now;
				int[] ghostLines = GhostLinesRemoval.Get(bitmap, 10, 60, 20, 12);

				Console.WriteLine("Total time: " + DateTime.Now.Subtract(start).ToString());

				string newFile = file.Directory.FullName + @"\result.png";

				if (File.Exists(newFile))
					File.Delete(newFile);

				bitmap.Save(newFile, ImageFormat.Png);

			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex.Message);
			}

			Console.ReadLine();
		}
		#endregion

		#region ReplaceColor()
		private static void ReplaceColor()
		{
			string source = @"C:\temp\00.tif";
			string destination = @"C:\temp\01.tif";
			byte colorR = 208, colorG = 202, colorB = 185;
			byte fuziness = 30;
			byte lightnessR = 33, lightnessG = 28, lightnessB = 23;

			Bitmap bitmap = new Bitmap(source);

			DateTime start = DateTime.Now;
			ImageProcessing.ColorReplacement.ReplaceColor(bitmap, Rectangle.Empty, colorR, colorG, colorB, fuziness, lightnessR, lightnessG, lightnessB);
			Console.WriteLine("Total time: " + DateTime.Now.Subtract(start).ToString());

			if (File.Exists(destination))
				File.Delete(destination);

			bitmap.Save(destination, ImageFormat.Jpeg);
		}
		#endregion

		#region ReplaceColor()
		//-c:RC -s:"P:\external\TNS\Recent Image Examples from TNS\0003a.jpg" -d:"P:\external\TNS\Recent Image Examples from TNS\clean\0003a.jpg" -p:232,224,222,9,18,26,28
		private static void ReplaceColor(FileInfo sourceFile, FileInfo resultFile, string parameters)
		{
			string[] colors = parameters.Split(new char[] { ',' });
			byte colorR = Convert.ToByte(colors[0]), colorG = Convert.ToByte(colors[1]), colorB = Convert.ToByte(colors[2]);
			byte fuziness = Convert.ToByte(colors[3]);
			byte lightnessR = Convert.ToByte(colors[4]), lightnessG = Convert.ToByte(colors[5]), lightnessB = Convert.ToByte(colors[6]);

			ImageProcessing.ColorReplacement.ReplaceColorPath(sourceFile.FullName, resultFile.FullName,
				75, colorR, colorG, colorB, fuziness, lightnessR, lightnessG, lightnessB);
		}
		#endregion

		#region Resample()
		private unsafe static void Resample()
		{
			try
			{
				string sourceFile = @"C:\Users\jirka.stybnar\TestRun\ColorModes\1bpp.png";
				string resultFile = @"c:\delete\result.png";

				Bitmap source = new Bitmap(sourceFile);
				DateTime total = DateTime.Now;
				Bitmap result = ImageProcessing.Resampling.Resample(source, PixelsFormat.Format4bppGray);

				result.Save(resultFile, ImageFormat.Png);
				source.Dispose();
				result.Dispose();

				Console.WriteLine("\n\nTotal: " + DateTime.Now.Subtract(total).ToString());
				//Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region ResampleBigImage()
		private unsafe static void ResampleBigImage()
		{
			try
			{
				FileInfo source = new FileInfo(@"C:\delete\00000009_000001.Jpg");
				string dest = @"C:\delete\";

				try
				{
					Directory.CreateDirectory(dest);

					DateTime start = DateTime.Now;
					ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source.FullName);
					ImageProcessing.BigImages.Resampling resampling = new ImageProcessing.BigImages.Resampling();
					resampling.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

					resampling.Resample(itDecoder, dest + "result.png", new ImageProcessing.FileFormat.Png(), PixelsFormat.Format8bppIndexed);
					Console.WriteLine(source.Name + ": " + DateTime.Now.Subtract(start).ToString());
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error in " + source.Name + ": " + ex.Message);
				}

				Console.WriteLine("\n\nThe End.");
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region ReshapeImage()
		private static unsafe void ReshapeImage()
		{
			DateTime start = DateTime.Now;
			FileInfo[] sourceImages = new DirectoryInfo(@"C:\Documents and Settings\Jirka\My Documents\personal\PHD\ns\kniha 1200 dpi\results").GetFiles("*.png");
			DirectoryInfo destDir = new DirectoryInfo(@"C:\Documents and Settings\Jirka\My Documents\personal\PHD\ns\kniha 1200 dpi\results\results");

			try
			{
				destDir.Create();

				for (int i = 11; i < sourceImages.Length; i++)
				{
					FileInfo file = sourceImages[i];
					Bitmap bitmap1 = new Bitmap(file.FullName);

					if (bitmap1.PixelFormat == PixelFormat.Format24bppRgb || bitmap1.PixelFormat == PixelFormat.Format32bppRgb || bitmap1.PixelFormat == PixelFormat.Format32bppArgb)
					{
						Bitmap bitmap = ImageProcessing.Resampling.Resample(bitmap1, PixelsFormat.Format8bppIndexed);

						bitmap.Save(destDir.FullName + @"\" + file.Name, ImageFormat.Png);
						bitmap.Dispose();
					}
					else
						bitmap1.Save(destDir.FullName + @"\" + file.Name, ImageFormat.Png);


					Console.Write(".");
					bitmap1.Dispose();
				}

				Console.WriteLine("\nTotal time: " + DateTime.Now.Subtract(start).ToString());
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion
	
		#region ResizeDir()
		public unsafe static void ResizeDir()
		{
			Resizing.ResizeDir();
		}
		#endregion

		#region SaveJpegWithMetadata()
		private static void SaveJpegWithMetadata()
		{
			try
			{
				string file = @"c:\temp\02.jpg";

				if (File.Exists(file))
					File.Delete(file);

				File.Copy(@"c:\temp\01.jpg", file);

				ImageComponent.ImageDecoder decoder = new ImageComponent.ImageDecoder(file);
				ImageComponent.Metadata.ExifMetadata metadat = decoder.GetJpegMetadata();


				Console.WriteLine();
				Console.WriteLine();

				Bitmap b = new Bitmap(file);

				foreach (PropertyItem pi in b.PropertyItems)
				{
					string s = System.Text.Encoding.ASCII.GetString(pi.Value);
					Console.WriteLine(string.Format("0x{0:X}   {1}", pi.Id, s));
				}

				Console.WriteLine();
				Console.WriteLine();

				BIP.Metadata.Metadata metadata = new BIP.Metadata.Metadata(b);
				BIP.Metadata.ExifMetadata exifOutput = metadata.GetExifMetadata();

				foreach (BIP.Metadata.PropertyBase property in exifOutput.Properties)
					if (property.Defined)
						Console.WriteLine(property.ToString());

				Console.WriteLine();
				Console.WriteLine();

				DateTime start = DateTime.Now;
				List<string> md = ImageProcessing.ImageFile.Gif.GetMetadata(new FileInfo(file));
				foreach (string comment in md)
					Console.WriteLine(comment);

				Console.WriteLine("Time: " + DateTime.Now.Subtract(start).ToString());

				Console.WriteLine("Done.");
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region SaveGifWithMetadata()
		private static void SaveGifWithMetadata()
		{
			try
			{
				string		file = @"c:\temp\01.gif";
				
				if (File.Exists(file))
					File.Delete(file);

				File.Copy(@"c:\temp\02.gif", file);

				List<string> chunks = new List<string>();

				for (int i = 0; i < 100; i++ )
					chunks.Add("comment " + i.ToString());
			
				ImageProcessing.ImageFile.Gif.SaveMetadata(new FileInfo(file), chunks);

				Console.WriteLine();
				Console.WriteLine();

				Bitmap b = new Bitmap(file);

				foreach (PropertyItem pi in b.PropertyItems)
				{
					string s = System.Text.Encoding.ASCII.GetString(pi.Value);
					Console.WriteLine(string.Format("0x{0:X}   {1}", pi.Id, s));
				}

				Console.WriteLine();
				Console.WriteLine();

				BIP.Metadata.Metadata metadata = new BIP.Metadata.Metadata(b);
				BIP.Metadata.ExifMetadata exifOutput = metadata.GetExifMetadata();

				foreach (BIP.Metadata.PropertyBase property in exifOutput.Properties)
					if (property.Defined)
						Console.WriteLine(property.ToString());

				Console.WriteLine();
				Console.WriteLine();

				DateTime start = DateTime.Now;
				List<string> md = ImageProcessing.ImageFile.Gif.GetMetadata(new FileInfo(file));
				foreach(string comment in md)
					Console.WriteLine(comment);

				Console.WriteLine("Time: " + DateTime.Now.Subtract(start).ToString());

				Console.WriteLine("Done.");
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion
	
		#region SavePngWithMetadata()
		private static void SavePngWithMetadata()
		{
			try
			{
				string file = @"c:\temp\04.png";

				//ImageProcessing.ImageFile.Png.GetMetadata(new FileInfo(@"c:\temp\03.png"));
				Bitmap bitmap = new Bitmap(1, 1, PixelFormat.Format24bppRgb);
				bitmap.SetPixel(0, 0, Color.Azure);
				bitmap.Save(file, ImageFormat.Png);
				bitmap.Dispose();

				//ImageProcessing.ImageFile.Png.GetMetadata(new FileInfo(file));

				List<BIP.Metadata.PngChunks.tEXtChunk> chunks = new List<BIP.Metadata.PngChunks.tEXtChunk>();

				chunks.Add(new BIP.Metadata.PngChunks.tEXtChunk(BIP.Metadata.PngChunks.tEXtChunkType.Author, "author"));
				chunks.Add(new BIP.Metadata.PngChunks.tEXtChunk(BIP.Metadata.PngChunks.tEXtChunkType.Comment, "comment"));
				chunks.Add(new BIP.Metadata.PngChunks.tEXtChunk(BIP.Metadata.PngChunks.tEXtChunkType.Copyright, "copyright"));
				chunks.Add(new BIP.Metadata.PngChunks.tEXtChunk(BIP.Metadata.PngChunks.tEXtChunkType.CreationTime, "06/06/2011"));
				chunks.Add(new BIP.Metadata.PngChunks.tEXtChunk(BIP.Metadata.PngChunks.tEXtChunkType.Description, "description"));
				chunks.Add(new BIP.Metadata.PngChunks.tEXtChunk(BIP.Metadata.PngChunks.tEXtChunkType.Disclaimer, "disclaimer"));
				chunks.Add(new BIP.Metadata.PngChunks.tEXtChunk(BIP.Metadata.PngChunks.tEXtChunkType.Software, "KIC"));
				chunks.Add(new BIP.Metadata.PngChunks.tEXtChunk(BIP.Metadata.PngChunks.tEXtChunkType.Source, "source"));
				chunks.Add(new BIP.Metadata.PngChunks.tEXtChunk(BIP.Metadata.PngChunks.tEXtChunkType.Title, "title"));
				chunks.Add(new BIP.Metadata.PngChunks.tEXtChunk(BIP.Metadata.PngChunks.tEXtChunkType.Warning, "warning"));

				ImageProcessing.ImageFile.Png.SaveMetadata(new FileInfo(file), chunks);

				using (ImageComponent.ImageDecoder decoder = new ImageComponent.ImageDecoder(file))
				{
					ImageComponent.Metadata.ExifMetadata md = decoder.GetPngMetadata();

					foreach (ImageComponent.Metadata.PropertyBase prop in md.Properties)
						if (prop.Defined)
							Console.WriteLine(prop.ToString());
				}

				Console.WriteLine();
				Console.WriteLine();

				Bitmap b = new Bitmap(file);

				foreach (PropertyItem pi in b.PropertyItems)
				{
					string s = System.Text.Encoding.ASCII.GetString(pi.Value);

					Console.WriteLine(string.Format("0x{0:X}   {1}", pi.Id, s));
				}

				Console.WriteLine();
				Console.WriteLine();

				BIP.Metadata.Metadata metadata = new BIP.Metadata.Metadata(b);
				BIP.Metadata.ExifMetadata exifOutput = metadata.GetExifMetadata();

				foreach (BIP.Metadata.PropertyBase property in exifOutput.Properties)
					if (property.Defined)
						Console.WriteLine(property.ToString());

				b.Dispose();

				Console.WriteLine();
				Console.WriteLine();

				Hashtable hashTable = ImageProcessing.ImageFile.Png.GetMetadata(new FileInfo(file));
				foreach (string key in hashTable.Keys)
					Console.WriteLine(key + "=" + (string)hashTable[key]);

				Console.WriteLine();
				Console.WriteLine("Done.");
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region SeekDocument()
		private unsafe static void SeekDocument()
		{
			try
			{
				DateTime start = DateTime.Now;
				string file = @"C:\Users\jirka.stybnar\TestRun\KIC\00.tif";
				string dest = @"C:\Users\jirka.stybnar\TestRun\KIC\00 R.png";
				Bitmap bitmap = new Bitmap(file);
				Rectangle rect;

				if (ImageProcessing.DocumentLocator.SeekDocument(bitmap, Rectangle.Empty, out rect, 100, 100, 100))
				{
					Bitmap clip = ImageProcessing.ImageCopier.Copy(bitmap, rect);
					clip.Save(dest, ImageFormat.Png);
					clip.Dispose();
				}

				bitmap.Dispose();

				Console.WriteLine("Despeckle(): " + DateTime.Now.Subtract(start).ToString());
				Console.ReadLine();

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region SeekDocumentBigImage()
		private unsafe static void SeekDocumentBigImage()
		{
			try
			{
				DateTime start = DateTime.Now;
				string file = @"C:\delete\03.jpg";
				Rectangle rect;

				if (ImageProcessing.BigImages.DocumentLocator.SeekDocument(file, new Rectangle(300, 300, 6653, 4836), 20, out rect))
				{

				}

				Console.WriteLine(rect.ToString());
				Console.WriteLine(DateTime.Now.Subtract(start).ToString());
				Console.ReadLine();

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region ShowHistogram()
		private unsafe static void ShowHistogram()
		{
			string sourceFile = @"C:\Temp\amazon\a000005.JPG";
			Bitmap source = new Bitmap(sourceFile);
			DateTime start = DateTime.Now;
			TimeSpan span = new TimeSpan(0);

			start = DateTime.Now;
			Histogram histogram = new Histogram(source, Rectangle.FromLTRB(1600, 2950, 1900, source.Height));
			histogram.Show();

			//Console.ReadLine();
		}
		#endregion

		#region Smoothing()
		private static void Smoothing()
		{
			FileInfo sourceFile = new FileInfo(@"C:\Users\jirka.stybnar\TestRun\Smoothing\0002_32.png");
			Bitmap bitmap = new Bitmap(sourceFile.FullName);

			DateTime start = DateTime.Now;
			//ImageProcessing.Smoothing.RotatingMask(bitmap, Rectangle.Empty, ImageProcessing.Smoothing.RotatingMaskType.Mask_5x5);
			//ImageProcessing.Smoothing.Kirsch(bitmap, Rectangle.Empty);
			ImageProcessing.Smoothing.UnsharpMasking(bitmap, Rectangle.Empty, 2);
			//ImageProcessing.Smoothing.Averaging3x3(bitmap, Rectangle.Empty); 
			Console.WriteLine(string.Format("Smoothing(): {0}", DateTime.Now.Subtract(start).ToString()));

			//bitmap.Save(@"C:\Users\jirka.stybnar\TestRun\Bookfold Images\result.png", ImageFormat.Png);
			bitmap.Dispose();
			Console.ReadLine();
		}
		#endregion

		#region Stitch()
		private static void Stitch()
		{
			try
			{
				Bitmap b1 = new Bitmap(@"C:\temp\Left.png");
				Bitmap b2 = new Bitmap(@"C:\temp\Right.png");
				DateTime start = DateTime.Now;

				Bitmap result = ImageProcessing.Stitching.Go(b1, b2);

				Console.WriteLine("Stitching: " + DateTime.Now.Subtract(start).ToString());

				result.Save(@"C:\Users\jirka.stybnar\TestRun\Stitching\result.png", ImageFormat.Png);
				b1.Dispose();
				b2.Dispose();
				result.Dispose();
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				Console.ReadLine();
			}
		}
		#endregion

		#region Test()
		private static void Test()
		{
		}
		#endregion
	
		#region WriteMetadata()
		private static void WriteMetadata()
		{
			string source = @"C:\delete\0000.tif";

			using (Bitmap b = new Bitmap(source))
			{
				PropertyItem propertyItem = b.PropertyItems[0];
				string someString = "HovnoKokot";

				byte[] a = System.Text.Encoding.ASCII.GetBytes(someString);
				byte[] array = new byte[a.Length + 1];

				for (int i = 0; i < a.Length; i++)
					array[i] = a[i];

				propertyItem.Id = 700;
				propertyItem.Len = array.Length;
				propertyItem.Type = 2;
				propertyItem.Value = array;

				b.SetPropertyItem(propertyItem);
				b.Save(@"C:\delete\0000a.tif", ImageFormat.Tiff);
			}
		}
		#endregion

		#region ReadMetadata()
		private static void ReadMetadata()
		{
			string source = @"C:\delete\0000a.tif";

			using (Bitmap b = new Bitmap(source))
			{
				foreach(PropertyItem item in b.PropertyItems)
				{
					if (item.Id == 700)
					{
						string str = "";

						for (int i = 0; i < item.Value.Length; i++)
						{
							if (item.Value[i] != 0)
								str += (char)item.Value[i];
							else
								break;
						}

						Console.WriteLine(str);
					}
				}
			}
		}
		#endregion

		#region GetBitmapSource()
		public static System.Windows.Media.Imaging.BitmapSource GetBitmapSource(System.Drawing.Bitmap bitmap)
		{
			IntPtr hBitmap = bitmap.GetHbitmap();

			System.Windows.Media.Imaging.BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
					hBitmap,
					IntPtr.Zero,
					System.Windows.Int32Rect.Empty,
					System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

			DeleteObject(hBitmap);

			return bitmapSource;
		}
		#endregion

		#region TestResizing()
		private static void TestResizing()
		{
			Random random = new Random();
			double zoom = 1.0;
			List<string> sources = new List<string>();
			string source = "";
			string dest = @"C:\delete\delete.JPG";
			ImageProcessing.BigImages.Resizing resizing = new ImageProcessing.BigImages.Resizing();

			sources.AddRange(Directory.GetFiles(@"C:\Users\jirka.stybnar\TestRun\Big Images\", "*.jpg"));
			sources.AddRange(Directory.GetFiles(@"C:\Users\jirka.stybnar\TestRun\Big Images\", "*.png"));
			sources.AddRange(Directory.GetFiles(@"C:\Users\jirka.stybnar\TestRun\Big Images\", "*.tiff"));

			try
			{
				while (true)
				{
					int fileIndex = random.Next(sources.Count - 1);
					source = sources[fileIndex];

					using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source))
					{
						DateTime start = DateTime.Now;
						zoom = random.NextDouble() * 0.97 + 0.03;

						Console.WriteLine(string.Format("Working on {0}, Zoom: {1}.", Path.GetFileName(source), zoom));
						resizing.Resize(itDecoder, dest, new ImageProcessing.FileFormat.Png(), zoom);
						Console.WriteLine(string.Format("{0}, Zoom: {1}, Time:{2}.", Path.GetFileName(source), zoom, DateTime.Now.Subtract(start).ToString()));
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format("\n\nError in file {0}, Zoom: {1}, Error{2}.\n\nex.StackTrace", Path.GetFileName(source), zoom, ex, ex.StackTrace));
				Console.ReadLine();
			}
		}
		#endregion

		#region UpsideDownFix()
		//-c:UDF -s:"C:\temp\delete" -d:"C:\temp\delete\Results" 
		private static void UpsideDownFix(DirectoryInfo sourceDir, DirectoryInfo destDir, string parameters)
		{
			ArrayList sources = new ArrayList();
			Bitmap bitmap;
			TimeSpan span = new TimeSpan(0);
			DateTime totalTimeStart = DateTime.Now;
			DateTime start;
			TimeSpan algTime;
			bool upsideDown;
			ImageFormat imageFormat;

			sources.AddRange(sourceDir.GetFiles("*.tif"));
			sources.AddRange(sourceDir.GetFiles("*.jpg"));
			sources.AddRange(sourceDir.GetFiles("*.png"));
			sources.AddRange(sourceDir.GetFiles("*.bmp"));
			sources.AddRange(sourceDir.GetFiles("*.gif"));

			destDir.Create();

			foreach (FileInfo file in sources)
			{
				bitmap = new Bitmap(file.FullName);
				imageFormat = bitmap.RawFormat;

				start = DateTime.Now;
				upsideDown = RectangularRotation.IsUpsideDown(bitmap, 0);

				if (upsideDown)
					bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);

				algTime = DateTime.Now.Subtract(start);
				span = span.Add(algTime);

				Console.WriteLine(string.Format("File: {0}, Time: {1}, Rotation: {2}",
					file.Name, algTime.ToString(), upsideDown.ToString()));

				if (file.FullName == destDir.FullName + @"\" + file.Name)
				{
					Bitmap copy = ImageProcessing.ImageCopier.Copy(bitmap);
					bitmap.Dispose();
					bitmap = copy;
				}

				if (File.Exists(destDir.FullName + @"\" + file.Name))
					File.Delete(destDir.FullName + @"\" + file.Name);

				bitmap.Save(destDir.FullName + @"\" + file.Name, imageFormat);
				bitmap.Dispose();
			}

			Console.WriteLine("Total algorithm time: " + span.ToString());
			Console.WriteLine("Total time: " + DateTime.Now.Subtract(totalTimeStart).ToString());
		}

		//-c:UDF -s:"C:\temp\0.jpg" -d:"C:\temp\0R.jpg"
		private static void UpsideDownFix(FileInfo sourceFile, FileInfo resultFile, string parameters)
		{
			if ((sourceFile.Attributes & FileAttributes.Directory) > 0)
			{
				UpsideDownFix(new DirectoryInfo(sourceFile.FullName), new DirectoryInfo(resultFile.FullName), parameters);
				return;
			}

			Bitmap bitmap = new Bitmap(sourceFile.FullName);
			DateTime start = DateTime.Now;
			ImageFormat imageFormat = bitmap.RawFormat;

			bool upsideDown = RectangularRotation.IsUpsideDown(bitmap, 0);

			if (upsideDown)
				bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);

			Console.WriteLine(string.Format("File: {0}, Time: {1}, Rotation: {2}",
				sourceFile.Name, DateTime.Now.Subtract(start).ToString(), upsideDown.ToString()));

			if (sourceFile.FullName == resultFile.FullName)
			{
				Bitmap copy = ImageProcessing.ImageCopier.Copy(bitmap);
				bitmap.Dispose();
				bitmap = copy;
			}

			if (resultFile.Exists)
				resultFile.Delete();

			resultFile.Directory.Create();
			bitmap.Save(resultFile.FullName, imageFormat);
			bitmap.Dispose();
			Console.ReadLine();
		}
		#endregion

	}
}
