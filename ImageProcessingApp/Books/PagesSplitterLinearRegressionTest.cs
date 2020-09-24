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
			/*
			List<BIP.Books.RegressionPoint> regressionPoints = new List<BIP.Books.RegressionPoint>()
			{
				new RegressionPoint(50,0,1),
				new RegressionPoint(50,50,1),
				new RegressionPoint(45,100,1)
			};

			BIP.Books.LinearRegression linearRegression = new BIP.Books.LinearRegression(regressionPoints);

			linearRegression.GetSplitterLine(100, 100);
			*/
			DirectoryInfo dir = new DirectoryInfo(Path.Combine(TestApp.Misc.TestRunDir.FullName, @"PagesSplitter"));
			DirectoryInfo resultDir = new DirectoryInfo(dir.FullName + @"\result");
			List<FileInfo> files = dir.GetFiles("*.*").ToList();

			BIP.Books.PagesSplitterLinearRegression splitter = new BIP.Books.PagesSplitterLinearRegression();

			resultDir.Create();

			foreach(FileInfo file in files)
			{
				if (file.Name == "KIC Document 18.jpg")
				{
					using (Bitmap bitmap = new Bitmap(file.FullName))
					{
						int dpi = Convert.ToInt32(bitmap.HorizontalResolution);

						DateTime start = DateTime.Now;
						BIP.Books.SplitterLine line = splitter.FindBookfoldLine(bitmap, new Rectangle(bitmap.Width / 2 - 3 * dpi, dpi / 2, 6 * dpi, bitmap.Height - dpi), 10, 10, 0);
						Console.WriteLine(string.Format("File: {0}, Time: {1}, Result: {2}", file.Name, DateTime.Now.Subtract(start), line));

						splitter.DrawResult(bitmap, line);

						bitmap.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + ".jpg"), ImageFormat.Jpeg);
					}
				}
			}
		}
		#endregion

	}
}
