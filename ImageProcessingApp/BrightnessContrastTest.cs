using ImageProcessing;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TestApp
{
	class BrightnessContrastTest : TestBase
	{

		#region Go()
		public static void Go()
		{
			FileInfo source = new FileInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\BrightnessContrast\50.jpg");

			using (Bitmap bitmap = new Bitmap(source.FullName))
			{
				/*
				DateTime start = DateTime.Now;

				ImageProcessing.Histogram histogram = new ImageProcessing.Histogram(bitmap);
				Bitmap result = ImageProcessing.BrightnessContrast.GetBitmap(bitmap, -0.2, .2, histogram.Mean);
				Console.WriteLine("Total time B+C: " + DateTime.Now.Subtract(start).ToString());
				result.Save(@"C:\Users\jirka.stybnar\TestRun\BrightnessContrast\resultBC.png", ImageFormat.Png);
				result.Dispose();

				start = DateTime.Now;
				result = ImageProcessing.Brightness.GetBitmap(bitmap, 0.2);
				Console.WriteLine("Total time B: " + DateTime.Now.Subtract(start).ToString());
				result.Save(@"C:\Users\jirka.stybnar\TestRun\BrightnessContrast\resultB.png", ImageFormat.Png);
				result.Dispose();

				start = DateTime.Now;
				result = ImageProcessing.Contrast.GetBitmap(bitmap,-0.5, histogram.Mean);
				Console.WriteLine("Total time B: " + DateTime.Now.Subtract(start).ToString());
				result.Save(@"C:\Users\jirka.stybnar\TestRun\BrightnessContrast\resultC.png", ImageFormat.Png);
				result.Dispose();
				*/

				DateTime start = DateTime.Now;

				Bitmap result = ImageProcessing.BrightnessContrast.GetBitmapV2(bitmap, -0.2, .2);
				Console.WriteLine("Total time B+C: " + DateTime.Now.Subtract(start).ToString());
				result.Save(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\BrightnessContrast\resultBC.png", ImageFormat.Png);
				result.Dispose();

				start = DateTime.Now;
				result = ImageProcessing.Brightness.GetBitmapV2(bitmap, 0.6);
				Console.WriteLine("Total time B: " + DateTime.Now.Subtract(start).ToString());
				result.Save(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\BrightnessContrast\resultB.png", ImageFormat.Png);
				result.Dispose();

				start = DateTime.Now;
				result = ImageProcessing.Contrast.GetBitmapV2(bitmap, 1);
				Console.WriteLine("Total time B: " + DateTime.Now.Subtract(start).ToString());
				result.Save(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\BrightnessContrast\resultC.png", ImageFormat.Png);
				result.Dispose();

			}
		}
		#endregion

		#region ChangeBrightnessContrast()
		private static void ChangeBrightnessContrast()
		{
			Bitmap bitmap = new Bitmap(@"C:\Users\jirka.stybnar\TestRun\BrightnessContrast\127.jpg");
			DateTime start = DateTime.Now;

			ImageProcessing.Histogram histogram = new Histogram(bitmap);
			Bitmap result = ImageProcessing.BrightnessContrast.GetBitmap(bitmap, 50 / 255.0, 1, histogram.Mean);

			Console.WriteLine("Total time: " + DateTime.Now.Subtract(start).ToString());

			result.Save(@"C:\Users\jirka.stybnar\TestRun\BrightnessContrast\result.png", ImageFormat.Png);
			result.Dispose();
		}
		#endregion

		#region ChangeBrightnessContrastBigImage()
		private static void ChangeBrightnessContrastBigImage()
		{
			try
			{
				string source = @"C:\Users\jirka.stybnar\TestRun\Big Images\24 bpp 1200dpi.jpg";
				string dest = @"C:\delete\result.jpg";
				DateTime start = DateTime.Now;

				ImageProcessing.Histogram histogram = new ImageProcessing.Histogram();
				histogram.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

				ImageProcessing.BigImages.BrightnessContrast contrast = new ImageProcessing.BigImages.BrightnessContrast();
				contrast.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

				using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source))
				{
					DateTime now = DateTime.Now;

					histogram.Compute(itDecoder);

					Console.WriteLine("Histogram: " + DateTime.Now.Subtract(now).ToString());
					now = DateTime.Now;

					contrast.ChangeBrightnessAndContrast(itDecoder, dest, new ImageProcessing.FileFormat.Jpeg(80), 0.5, 0.5, histogram.Mean);

					Console.WriteLine("ChangeBrightnessAndContrast: " + DateTime.Now.Subtract(now).ToString());
				}

				Console.WriteLine("Total time: " + DateTime.Now.Subtract(start).ToString());
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion
	}
}
