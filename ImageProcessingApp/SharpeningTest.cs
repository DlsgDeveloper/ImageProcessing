using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TestApp
{
	class SharpeningTest
	{

		#region Go()
		public static void Go()
		{
			FileInfo[] sourceFiles = new DirectoryInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\Sharpening").GetFiles("*.*");
			DirectoryInfo resultDir = new DirectoryInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\Sharpening\results");

			resultDir.Create();

			for (int i = 0; i < sourceFiles.Length; i++)
			{
				//if (sourceFiles[i].Name == "01.png")
				{
					using (Bitmap b = new Bitmap(sourceFiles[i].FullName))
					{
						for (float factor = 0.5F; factor < 3.0F; factor = factor + 0.5F)
						{
							DateTime start = DateTime.Now;
							using (Bitmap result = ImageProcessing.UnsharpMask.UnsharpGaussian5x5(b, factor))
							{
								Console.WriteLine("SharpeningTest.Go(): " + DateTime.Now.Subtract(start).ToString());

								result.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(sourceFiles[i].Name) + "_" + factor.ToString("0.00") + ".png"), ImageFormat.Png);
							}
						}
					}
				}
			}

		}
		#endregion
		
		#region GoBigImage()
		public static void GoBigImage()
		{
			string source = @"C:\delete\del\IMG_0012.JPG";
			string result = @"C:\delete\del\Sharpened.png";

			ImageProcessing.BigImages.Sharpening sharpening = new ImageProcessing.BigImages.Sharpening();

			sharpening.ProgressChanged = delegate(float progress) { Misc.LogProgressChanged(progress); };
			sharpening.Laplacian3x3(new ImageProcessing.BigImages.ItDecoder(source), result, new ImageProcessing.FileFormat.Png());
		}
		#endregion

	}
}
