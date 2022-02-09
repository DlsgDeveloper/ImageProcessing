using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TestApp
{
	class ContrastTest
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
						Bitmap result = ImageProcessing.Contrast.GetBitmapV2(b, 1);

						Console.WriteLine("Contrast.GetBitmap(): " + DateTime.Now.Subtract(start).ToString());

						result.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(sourceFiles[i].Name) + ".png"), ImageFormat.Png);
					}
				}
			}
		}
		#endregion
	}
}
