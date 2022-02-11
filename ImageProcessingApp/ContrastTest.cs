using ImageProcessing;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TestApp
{
	class ContrastTest : TestBase
	{
		#region Go()
		public static void Go()
		{
			FileInfo[] sourceFiles = new DirectoryInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\Contrast").GetFiles("*.*");
			DirectoryInfo resultDir = new DirectoryInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\Contrast\results");

			resultDir.Create();

			for (int i = 0; i < sourceFiles.Length; i++)
			{
				//if (sourceFiles[i].Name == "SL2 KIC2.png")
				{
					using (Bitmap b = new Bitmap(sourceFiles[i].FullName))
					{
						DateTime start = DateTime.Now;

						ImageProcessing.Contrast.Go(b, new Rectangle(100, 100, b.Width / 2 - 200, b.Height - 200), 0.5);
						ImageProcessing.Contrast.Go(b, new Rectangle(b.Width / 2 + 100, 100, b.Width / 2 - 200, b.Height - 200), 0.8);

						Console.WriteLine("Contrast.GetBitmap(): " + DateTime.Now.Subtract(start).ToString());

						b.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(sourceFiles[i].Name) + ".png"), ImageFormat.Png);

						/*
						Bitmap result = ImageProcessing.Contrast.GetBitmap(b, new Rectangle(100, 100, b.Width / 2 - 200, b.Height - 200), 0.5);

						Console.WriteLine("Contrast.GetBitmap(): " + DateTime.Now.Subtract(start).ToString());

						result.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(sourceFiles[i].Name) + ".png"), ImageFormat.Png);
						*/
					}
				}
			}
		}
		#endregion

		#region ChangeContrast()
		private static void ChangeContrast()
		{
			Bitmap bitmap = new Bitmap(@"C:\Users\jirka.stybnar\TestRun\BrightnessContrast\127.jpg");
			DateTime start = DateTime.Now;

			ImageProcessing.Histogram histogram = new Histogram(bitmap);
			ImageProcessing.Contrast.Go(bitmap, 0.5, histogram.Mean);
			//Bitmap result = ImageProcessing.Contrast.GetBitmap(bitmap, -0.5, histogram.Mean);

			Console.WriteLine("Total time: " + DateTime.Now.Subtract(start).ToString());

			bitmap.Save(@"C:\Users\jirka.stybnar\TestRun\BrightnessContrast\result.png", ImageFormat.Png);
			bitmap.Dispose();
		}
		#endregion

		#region ChangeContrastBigImage()
		private static void ChangeContrastBigImage()
		{
			string source = @"C:\Users\jirka.stybnar\TestRun\Big Images\24 bpp 1200dpi.jpg";
			string dest = @"C:\delete\result.jpg";
			DateTime start = DateTime.Now;

			ImageProcessing.Histogram histogram = new ImageProcessing.Histogram();
			histogram.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

			ImageProcessing.BigImages.Contrast contrast = new ImageProcessing.BigImages.Contrast();
			contrast.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

			using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source))
			{
				histogram.Compute(itDecoder);
				contrast.ChangeContrast(itDecoder, dest, new ImageProcessing.FileFormat.Jpeg(80), -0.5, histogram.Mean);
			}
		}
		#endregion

	}
}
