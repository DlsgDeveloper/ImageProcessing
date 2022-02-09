using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TestApp
{
	class AutoLevelsTest
	{
		#region Go()
		public static void Go()
		{
			FileInfo[]		sourceFiles = new DirectoryInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\AutoLevels").GetFiles("*.*");
			DirectoryInfo	resultDir = new DirectoryInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\AutoLevels\results");

			resultDir.Create();

			for (int i = 0; i < sourceFiles.Length; i++)
			{
				using (Bitmap b = new Bitmap(sourceFiles[i].FullName))
				{
					DateTime start = DateTime.Now;
					ImageProcessing.AutoLevels.GetSingleChannel(b, 0.01, 0.02);

					Console.WriteLine("AutoLevels.GetSingleChannel(): " + DateTime.Now.Subtract(start).ToString());

					b.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(sourceFiles[i].Name) + ".png"), ImageFormat.Png);
				}
			}
		}
		#endregion
	}
}
