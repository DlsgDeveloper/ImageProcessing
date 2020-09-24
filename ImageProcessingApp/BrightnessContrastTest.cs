using ImageProcessing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace TestApp
{
	class BrightnessContrastTest
	{

		public static void Test()
		{
			FileInfo source = new FileInfo(@"C:\Users\jirka.stybnar\TestRun\BrightnessContrast\32bpp.png");

			using (Bitmap bitmap = new Bitmap(source.FullName))
			{
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

			}
		}

	}
}
