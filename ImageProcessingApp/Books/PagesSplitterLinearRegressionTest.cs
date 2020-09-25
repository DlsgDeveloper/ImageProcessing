using BIP.Books;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp.Books
{
	class PagesSplitterLinearRegressionTest
	{

		#region Test()
		public unsafe static void Test()
		{
			
			List<BIP.Books.RegressionPoint> regressionPoints = new List<BIP.Books.RegressionPoint>()
			{
				new RegressionPoint(50,0,1),
				new RegressionPoint(50,50,1),
				new RegressionPoint(45,100,1)
			};

			BIP.Books.LinearRegression linearRegression = new BIP.Books.LinearRegression(regressionPoints);

			SplitterLine l = linearRegression.GetSplitterLine(100, 100);

			Console.WriteLine(string.Format("[{0},{1}]   [{2},{3}]", l.PointTop.X, l.PointTop.Y, l.PointBottom.X, l.PointBottom.Y));


			DirectoryInfo dir = new DirectoryInfo(Path.Combine(TestApp.Misc.TestRunDir.FullName, @"PagesSplitter"));
			DirectoryInfo resultDir = new DirectoryInfo(dir.FullName + @"\result");
			List<FileInfo> files = dir.GetFiles("*.*").ToList();

			BIP.Books.PagesSplitterLinearRegression splitter = new BIP.Books.PagesSplitterLinearRegression();

			resultDir.Create();

			foreach(FileInfo file in files)
			{
				//if (file.Name == "KIC Document 21.jpg")
				{
					DateTime start = DateTime.Now;
					/*int splitterL, splitterR;
					
					PagesSplitter.FindPagesSplitterStatic(file, out splitterL, out splitterR);
					Console.WriteLine(string.Format("Jirka's Split Method 1: File: {0}, Time: {1}, Result: {2},{3}", file.Name, DateTime.Now.Subtract(start), splitterL, splitterR));

					using (Bitmap bitmap = new Bitmap(file.FullName))
					{
						PagesSplitter.DrawResult(bitmap, splitterL, splitterR);
						bitmap.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + "_Jirka_1.png"), ImageFormat.Png);
					}

					start = DateTime.Now;
					PagesSplitter.FindPagesSplitterStatic2(file, out splitterL, out splitterR);
					Console.WriteLine(string.Format("Jirka's Split Method 2: File: {0}, Time: {1}, Result: {2},{3}", file.Name, DateTime.Now.Subtract(start), splitterL, splitterR));

					using (Bitmap bitmap = new Bitmap(file.FullName))
					{
						PagesSplitter.DrawResult(bitmap, splitterL, splitterR);
						bitmap.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + "_Jirka_2.png"), ImageFormat.Png);
					}
					*/

					using (Bitmap bitmap = new Bitmap(file.FullName))
					{
						int dpi = Convert.ToInt32(bitmap.HorizontalResolution);

						start = DateTime.Now;
						BIP.Books.SplitterLine line = splitter.FindBookfoldLine(bitmap, new Rectangle(bitmap.Width / 2 - Convert.ToInt32(1.5 * dpi), dpi / 2, 3 * dpi, bitmap.Height - dpi), 10, 15, 0);
						Console.WriteLine(string.Format("Ted's Split Method: File: {0}, Time: {1}, Result: {2}", file.Name, DateTime.Now.Subtract(start), line));

						//splitter.DrawResult(bitmap, line);

						bitmap.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + "_Ted.jpg"), ImageFormat.Jpeg);
					}
				}
			}
		}
		#endregion

	}
}
