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
				//if (sourceFiles[i].Name == "00.jpg")
				{
					using (Bitmap b = new Bitmap(sourceFiles[i].FullName))
					{
						DateTime start = DateTime.Now;

						
						ImageProcessing.Brightness.Go(b, new Rectangle(100, 100, b.Width / 2 - 200, b.Height - 200), 0.5);
						ImageProcessing.Brightness.Go(b, new Rectangle(b.Width / 2 + 100, 100, b.Width / 2 - 200, b.Height - 200), 0.2);

						Console.WriteLine("BrightnessTest.Go(): " + DateTime.Now.Subtract(start).ToString());

						b.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(sourceFiles[i].Name) + ".png"), ImageFormat.Png);
						
						/*
						Bitmap result = ImageProcessing.Brightness.GetBitmap(b, new Rectangle(100, 100, b.Width / 2 - 200, b.Height - 200), 0.3);
						ImageProcessing.Brightness.Go(result, new Rectangle(100, 100, result.Width / 2 - 200, result.Height - 200), 0.3);

						Console.WriteLine("BrightnessTest.Go(): " + DateTime.Now.Subtract(start).ToString());

						result.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(sourceFiles[i].Name) + ".png"), ImageFormat.Png);
						*/
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