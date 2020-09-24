using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace TestApp
{
	class EdgeDetection
	{

		public static void Go()
		{
			DirectoryInfo sourceDir = new DirectoryInfo(@"C:\Users\jirka.stybnar\TestRun\Edge Detection");
			DirectoryInfo resultDir = new DirectoryInfo(@"C:\Users\jirka.stybnar\TestRun\Edge Detection");

			FileInfo[] files = sourceDir.GetFiles("*.jpg");
			resultDir.Create();

			foreach (FileInfo file in files)
			{
				using (Bitmap b = new Bitmap(file.FullName))
				{
					DateTime start = DateTime.Now;

					using (Bitmap result = ImageProcessing.EdgeDetector.BinarizeLaplacian(b, 220, 220, 220, 30, true))
					{
						Console.WriteLine("BinarizeLaplacian: " + DateTime.Now.Subtract(start).ToString());

						string resultFile = Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + "laplacian.png");
						result.Save(resultFile, ImageFormat.Png);
					}
					/*
					using (Bitmap result = ImageProcessing.EdgeDetector.Binarize(b, Rectangle.Empty, ImageProcessing.EdgeDetector.RotatingMaskType.Kirsch ))
					{
						Console.WriteLine("Kirsch: " + DateTime.Now.Subtract(start).ToString());

						string resultFile = Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + "kirsch.png");
						result.Save(resultFile, ImageFormat.Png);
					}

					using (Bitmap result = ImageProcessing.EdgeDetector.Binarize(b, Rectangle.Empty, ImageProcessing.EdgeDetector.RotatingMaskType.Jirka))
					{
						Console.WriteLine("Jirka: " + DateTime.Now.Subtract(start).ToString());

						string resultFile = Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + "jirka.png");
						result.Save(resultFile, ImageFormat.Png);
					}
					*/
					using (Bitmap result = ImageProcessing.EdgeDetector.Get(b, Rectangle.Empty, ImageProcessing.EdgeDetector.Operator.Sobel))
					{
						Console.WriteLine("Sobel: " + DateTime.Now.Subtract(start).ToString());

						string resultFile = Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + "sobel.png");
						result.Save(resultFile, ImageFormat.Png);
					}
					/*
					using (Bitmap result = ImageProcessing.EdgeDetector.Get(b, Rectangle.Empty, ImageProcessing.EdgeDetector.Operator.Laplacian446a))
					{
						Console.WriteLine("Laplacian446a: " + DateTime.Now.Subtract(start).ToString());

						string resultFile = Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + "Laplacian446a.png");
						result.Save(resultFile, ImageFormat.Png);
					}

					using (Bitmap result = ImageProcessing.EdgeDetector.Get(b, Rectangle.Empty, ImageProcessing.EdgeDetector.Operator.MexicanHat5x5))
					{
						Console.WriteLine("MexicanHat5x5: " + DateTime.Now.Subtract(start).ToString());

						string resultFile = Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + "MexicanHat5x5.png");
						result.Save(resultFile, ImageFormat.Png);
					}

					using (Bitmap result = ImageProcessing.EdgeDetector.Get(b, Rectangle.Empty, ImageProcessing.EdgeDetector.Operator.MexicanHat7x7))
					{
						Console.WriteLine("MexicanHat7x7: " + DateTime.Now.Subtract(start).ToString());

						string resultFile = Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + "MexicanHat7x7.png");
						result.Save(resultFile, ImageFormat.Png);
					}

					using (Bitmap result = ImageProcessing.EdgeDetector.Get(b, Rectangle.Empty, ImageProcessing.EdgeDetector.Operator.MexicanHat17x17))
					{
						Console.WriteLine("MexicanHat17x17: " + DateTime.Now.Subtract(start).ToString());

						string resultFile = Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + "MexicanHat17x17.png");
						result.Save(resultFile, ImageFormat.Png);
					}*/

				}
			}
		}
	}
}
