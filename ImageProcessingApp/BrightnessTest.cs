using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TestApp
{
	class BrightnessTest : TestBase
	{
		#region Go()
		public static void Go()
		{
			FileInfo[] sourceFiles = new DirectoryInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\Brightness").GetFiles("*.*");
			DirectoryInfo resultDir = new DirectoryInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\Brightness\results");

			resultDir.Create();

			for (int i = 0; i < sourceFiles.Length; i++)
			{
				if (sourceFiles[i].Name == "00.jpg")
				{
					using (Bitmap b = new Bitmap(sourceFiles[i].FullName))
					{
						DateTime start = DateTime.Now;
						//Bitmap result = ImageProcessing.Brightness.GetBitmapV2(b, .03);
						ImageProcessing.Brightness.GoV2(b, .03);

						Console.WriteLine("BrightnessTest.GetBitmap(): " + DateTime.Now.Subtract(start).ToString());

						b.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(sourceFiles[i].Name) + ".png"), ImageFormat.Png);
					}
				}
			}
		}
		#endregion

		#region ChangeBrightness()
		private static void ChangeBrightness()
		{
			Bitmap bitmap = new Bitmap(@"C:\delete\kic\01.jpg");
			DateTime start = DateTime.Now;

			Bitmap result = ImageProcessing.Brightness.GetBitmapS2N(bitmap, 1.0);

			Console.WriteLine("Total time: " + DateTime.Now.Subtract(start).ToString());

			result.Save(@"C:\delete\kic\result.png", ImageFormat.Png);
			result.Dispose();
		}
		#endregion

		#region ChangeBrightnessBigImage()
		private static void ChangeBrightnessBigImage()
		{
			string source = @"C:\Users\jirka.stybnar\TestRun\Big Images\24 bpp 1200dpi.jpg";
			string dest = @"C:\delete\result.jpg";
			DateTime start = DateTime.Now;

			ImageProcessing.BigImages.Brightness brightness = new ImageProcessing.BigImages.Brightness();
			brightness.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

			using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source))
			{
				brightness.ChangeBrightness(itDecoder, dest, new ImageProcessing.FileFormat.Jpeg(80), -0.2);
			}
		}
		#endregion


	}
}