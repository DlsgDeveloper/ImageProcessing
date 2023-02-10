using ImageProcessing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TestApp
{
	class ContentLocatorTest : TestBase
	{

		// PUBLIC METHODS
		#region public methods

		#region Go()
		public static void Go()
		{
			DirectoryInfo				sourceDir = new DirectoryInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\ImageProcessing\ContentLocator");
			DirectoryInfo				destinationDir = new DirectoryInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\ImageProcessing\ContentLocator\results");
			FileInfo[]					sourceFiles = sourceDir.GetFiles("*.jpg");
			ImageProcessing.Operations	operations = new ImageProcessing.Operations(true, 0.2F, true, false, false);

			destinationDir.Create();

			foreach (FileInfo sourceFile in sourceFiles)
			{
				using (ImageProcessing.IpSettings.ItImage itImage = new ImageProcessing.IpSettings.ItImage(sourceFile) { IsFixed = false })
				{
					itImage.OpticsCenter = Math.Max(0, itImage.InchSize.Height - 8) / itImage.InchSize.Height;

					DateTime	itStart = DateTime.Now;
					float		confidence = itImage.Find(sourceFile.FullName, operations);

					if (confidence > 0)
					{
						if (itImage.TwoPages)
						{
							BIP.Geometry.InchSize clipsSize = new BIP.Geometry.InchSize(Math.Max(itImage.PageL.ClipRectInch.Width, itImage.PageR.ClipRectInch.Width), Math.Max(itImage.PageL.ClipRectInch.Height, itImage.PageR.ClipRectInch.Height));
							itImage.SetClipsSize(clipsSize);

							string clip1File = Path.Combine(destinationDir.FullName, Path.GetFileNameWithoutExtension(sourceFile.Name) + "_01.jpg");
							string clip2File = Path.Combine(destinationDir.FullName, Path.GetFileNameWithoutExtension(sourceFile.Name) + "_02.jpg");

							itImage.Execute(sourceFile.FullName, 0, clip1File, new ImageProcessing.FileFormat.Jpeg(85));
							itImage.Execute(sourceFile.FullName, 1, clip2File, new ImageProcessing.FileFormat.Jpeg(85));
						}
						else
						{
							string clipFile = Path.Combine(destinationDir.FullName, Path.GetFileNameWithoutExtension(sourceFile.Name) + ".jpg");

							itImage.Execute(sourceFile.FullName, 0, clipFile, new ImageProcessing.FileFormat.Jpeg(85));
						}
					}
						
					Console.WriteLine($"Image {sourceFile.Name}: Confidence is {Convert.ToInt32(confidence * 100)}%.");
				}
			}
		}
		#endregion

		#region ContentLocatorBigImage()
		private static void ContentLocatorBigImage()
		{
			string source = @"C:\delete\00000009.tif";
			string destL = @"C:\delete\resultL.png";
			string destR = @"C:\delete\resultR.png";

			using (ImageProcessing.IpSettings.ItImage itImage = new ImageProcessing.IpSettings.ItImage(new FileInfo(source)))
			{
				itImage.ExecutionProgressChanged += new ProgressHnd(ProgressChanged);
				itImage.IsFixed = false;
				itImage.IsIndependent = true;

				itImage.Find(source, new Operations(new Operations.ContentLocationParams(true, 0.1F, 0.1F, false), false, false, false));

				itImage.Execute(source, 0, destL, new ImageProcessing.FileFormat.Png());

				if (itImage.TwoPages)
					itImage.Execute(source, 1, destR, new ImageProcessing.FileFormat.Png());
			}
		}
		#endregion

		#region ContentLocator()
		private static void ContentLocator()
		{
			string destinationDir = @"C:\delete\";
			string source = @"C:\delete\00000009.tif";

			DateTime start = DateTime.Now;
			float confidence;

			Directory.CreateDirectory(destinationDir);

			using (ItImage itImage = new ItImage(new FileInfo(source), ItImage.ScannerType.Bookeye2))
			{
				itImage.IsFixed = false;

				confidence = itImage.Find(new Operations(true, 0.2F, true, false, false));
				Console.WriteLine("Total Time: " + DateTime.Now.Subtract(start).ToString());

				itImage.ReleasePageObjects();
				GC.Collect();

				Bitmap r1 = itImage.GetResult(0);
				r1.Save(destinationDir + @"\88 Result1.png", ImageFormat.Png);
				r1.Dispose();

				if (itImage.TwoPages)
				{
					Bitmap r2 = itImage.GetResult(1);
					r2.Save(destinationDir + @"\88 Result2.png", ImageFormat.Png);
					r2.Dispose();
				}
			}
		}
		#endregion

		#region ContentLocatorDir()
		private unsafe static void ContentLocatorDir()
		{
			DirectoryInfo			sourceDir = new DirectoryInfo(@"C:\delete\it");
			DirectoryInfo			destDir = new DirectoryInfo(@"C:\delete\it\results");
			List<FileInfo>			sources = new List<FileInfo>();
			System.Drawing.Color	color = System.Drawing.Color.FromArgb(90, 90, 90);
			TimeSpan				span = new TimeSpan(0);
			DateTime				totalTimeStart = DateTime.Now;
			float					confidence = 0;

			sources.AddRange(sourceDir.GetFiles("*.tif"));
			sources.AddRange(sourceDir.GetFiles("*.jpg"));
			sources.AddRange(sourceDir.GetFiles("*.png"));
			sources.AddRange(sourceDir.GetFiles("*.bmp"));
			sources.AddRange(sourceDir.GetFiles("*.gif"));

			destDir.Create();

			ItImages itImages = new ItImages();

			foreach (FileInfo file in sources)
			{
				ItImage itImage = new ItImage(file, ItImage.ScannerType.Bookeye2);
				itImage.IsFixed = false;
				itImage.IsIndependent = false;

				itImages.Add(itImage);
			}

			//itImages[0].IsFixed = true;

			foreach (ItImage itImage in itImages)
			{
				DateTime start = DateTime.Now;

				confidence = itImage.Find(new Operations(true, 0.2F, true, true, false));

				TimeSpan time = DateTime.Now.Subtract(start);
				Console.WriteLine(string.Format("{0}: {1}, Confidence:{2}%", itImage.File.Name, time.ToString(), confidence));

				itImage.DisposeBitmap();
				itImage.ReleasePageObjects();
				GC.Collect();
			}

			//itImages.MakeClipsSameSize(0.2f);

			foreach (ItImage itImage in itImages)
			{
				Bitmap r1 = itImage.GetResult(0);
				r1.Save(destDir + @"\" + Path.GetFileNameWithoutExtension(itImage.File.Name) + "_L.jpg", ImageFormat.Jpeg);
				r1.Dispose();

				if (itImage.TwoPages)
				{
					Bitmap r2 = itImage.GetResult(1);
					r2.Save(destDir + @"\" + Path.GetFileNameWithoutExtension(itImage.File.Name) + "_R.jpg", ImageFormat.Jpeg);
					r2.Dispose();
				}

				itImage.Dispose();
			}

			Console.WriteLine("Total time: " + span.ToString());
			Console.WriteLine("Total all time: " + DateTime.Now.Subtract(totalTimeStart).ToString());
		}
		#endregion

		#region ContentLocatorNew()
		private static void ContentLocatorNew()
		{
			string source = @"C:\OpusFreeFlowWorkingData\00000\083\ScanImages\Reduced\00000083_000003.jpg";
			//string source = @"C:\delete\scan_2009-12-22_18-20-49.jpg";				
			DateTime start = DateTime.Now;

			ImageProcessing.BigImages.ContentLocator contentLocator = new ImageProcessing.BigImages.ContentLocator();
			ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source);

			contentLocator.ProgressChanged += new ProgressHnd(ProgressChanged);

			contentLocator.GetContent(itDecoder);
		}
		#endregion

		#endregion

	}
}
