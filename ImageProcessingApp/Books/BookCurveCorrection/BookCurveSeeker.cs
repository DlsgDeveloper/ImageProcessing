using BIP.Books;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp.Books.BookCurveCorrection
{
	class BookCurveSeeker
	{
		#region Test()
		public unsafe static void Test()
		{
			DirectoryInfo dir = new DirectoryInfo(Path.Combine(TestApp.Misc.TestRunDir.FullName, @"IT\Book Color Text"));
			DirectoryInfo resultDir = new DirectoryInfo(dir.FullName + @"\result");
			List<FileInfo> files = dir.GetFiles("*.*").ToList();

			resultDir.Create();

			ImageProcessing.Books.TedsBookCurveCorrection.BookCurveSeeker seeker = new ImageProcessing.Books.TedsBookCurveCorrection.BookCurveSeeker();

			foreach (FileInfo file in files)
			{
				if (file.Name == "0001.TIF")
				{
					DateTime start = DateTime.Now;

					using (Bitmap bitmap = new Bitmap(file.FullName))
					{
						start = DateTime.Now;

						List<ImageProcessing.Books.TedsBookCurveCorrection.SingleLineRegion> regionsL = seeker.FindCurve(bitmap, bitmap.Width / 4);
						List<ImageProcessing.Books.TedsBookCurveCorrection.SingleLineRegion> regionsR = seeker.FindCurve(bitmap, bitmap.Width * 3 / 4);

						seeker.DrawResults(bitmap, regionsL);
						seeker.DrawResults(bitmap, regionsR);

						Console.WriteLine(string.Format("Ted's Curve Method: File: {0}, Time: {1}, Data Points: {2}", file.Name, DateTime.Now.Subtract(start), regionsL.Count + regionsR.Count));

						bitmap.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + ".jpg"), ImageFormat.Jpeg);
					}
				}
			}
		}
		#endregion

	}
}
