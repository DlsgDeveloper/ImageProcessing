using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TestApp
{
	class BrightnessContrastTest
	{

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

	}
}
