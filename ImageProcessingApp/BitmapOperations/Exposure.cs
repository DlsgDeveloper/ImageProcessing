using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace TestApp.BitmapOperations
{
	public static class Exposure
	{

		#region Go()
		public unsafe static void Go()
		{
			FileInfo[] files = new DirectoryInfo(@"C:\Users\jirka.stybnar\TestRun\GammaCorrection").GetFiles("*.png");
			DirectoryInfo resultsDir = new DirectoryInfo(@"C:\Users\jirka.stybnar\TestRun\GammaCorrection\results");

			resultsDir.Create();

			foreach (FileInfo file in files)
			{
				using (Bitmap b = new Bitmap(file.FullName))
				{
					DateTime start = DateTime.Now;
					string result = string.Format(resultsDir.FullName + @"\" + file.Name);

					ImageProcessing.BitmapOperations.Exposure.Go(b, 0, 0.035, .60);

					Console.WriteLine("Exposure: " + DateTime.Now.Subtract(start).ToString());
					b.Save(result, ImageFormat.Png);
				}
			}
		}
		#endregion

	}
}
