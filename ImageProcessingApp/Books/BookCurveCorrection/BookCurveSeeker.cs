using BIP.Books;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

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
					FileInfo dataFile = new FileInfo(resultDir.FullName + @"\" + Path.GetFileNameWithoutExtension(file.Name) + ".xlsx");

					Excel.Application	excel = new Microsoft.Office.Interop.Excel.Application();
					Excel.Workbook		workbook = excel.Workbooks.Add(Type.Missing);
					Excel.Worksheet		sheet = null;

					if (dataFile.Exists)
						dataFile.Delete();

					foreach (Excel.Worksheet s in workbook.Worksheets)
						sheet = sheet ?? s;

					sheet.Name = "slopes";
					//sheet.Cells[1,1] = "hovno";

					using (Bitmap bitmap = new Bitmap(file.FullName))
					{
						DateTime start = DateTime.Now;
						int dpi = Convert.ToInt32(bitmap.HorizontalResolution);
						List<ImageProcessing.Books.TedsBookCurveCorrection.SingleLineRegion> regions = new List<ImageProcessing.Books.TedsBookCurveCorrection.SingleLineRegion>();
						int rowCounter;
						int columnCounter = 2;

						for (int i = Convert.ToInt32(dpi * 1.5); i < (bitmap.Width / 2) - dpi; i += dpi / 2)
						{
							sheet.Cells[1, columnCounter] = "Angle";
							sheet.Cells[1, columnCounter + 1] = "Y";

							rowCounter = 2;

							List<ImageProcessing.Books.TedsBookCurveCorrection.SingleLineRegion> verticalRegions = seeker.FindCurve(bitmap, i);
							seeker.DrawResults(bitmap, verticalRegions);
							regions.AddRange(verticalRegions);

							foreach (ImageProcessing.Books.TedsBookCurveCorrection.SingleLineRegion region in verticalRegions)
							{
								sheet.Cells[rowCounter, columnCounter].NumberFormat = "0.00";
								sheet.Cells[rowCounter, columnCounter] = (region.BestAngle * 180 / Math.PI);
								sheet.Cells[rowCounter, columnCounter + 1] = region.ImagePoint.Y;
								rowCounter++;
							}

							columnCounter += 3;
						}

						columnCounter += 2;

						for (int i = bitmap.Width / 2 + dpi; i < bitmap.Width - (dpi * 1.5); i += dpi / 2)
						{
							sheet.Cells[1, columnCounter] = "Angle";
							sheet.Cells[1, columnCounter + 1] = "Y";

							rowCounter = 2;

							List<ImageProcessing.Books.TedsBookCurveCorrection.SingleLineRegion> verticalRegions = seeker.FindCurve(bitmap, i);
							seeker.DrawResults(bitmap, verticalRegions);
							regions.AddRange(verticalRegions);

							foreach (ImageProcessing.Books.TedsBookCurveCorrection.SingleLineRegion region in verticalRegions)
							{
								sheet.Cells[rowCounter, columnCounter].NumberFormat = "0.00";
								sheet.Cells[rowCounter, columnCounter] = (region.BestAngle * 180 / Math.PI);
								sheet.Cells[rowCounter, columnCounter + 1] = region.ImagePoint.Y;
								rowCounter++;
							}

							columnCounter += 3;
						}

						Console.WriteLine(string.Format("Ted's Curve Method: File: {0}, Time: {1}, Data Points: {2}", file.Name, DateTime.Now.Subtract(start), regions.Count));

						bitmap.Save(Path.Combine(resultDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + ".png"), ImageFormat.Png);
					}

					workbook.SaveAs(dataFile);
					workbook.Close();
					excel.Quit();
				}
			}
		}
		#endregion

	}
}
