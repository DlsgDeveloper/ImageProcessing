using ImageProcessing;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace TestApp
{
	class InsertorTest : TestBase
	{

		// PUBLIC METHODS
		#region public methods

		#region Go()
		public static void Go()
		{
			FileInfo source = new FileInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\Insert\Source.png");
			FileInfo insert = new FileInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\Insert\Insert.png");
			FileInfo result = new FileInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\Insert\Result.png");

			result.Directory.Create();

			using (Bitmap sourceBitmap = new Bitmap(source.FullName))
			using (Bitmap insertBitmap = new Bitmap(insert.FullName))
			{
				DateTime start = DateTime.Now;

				ImageProcessing.BitmapOperations.Insertor.Insert(sourceBitmap, insertBitmap, new System.Drawing.Point(0, 0));
				Console.WriteLine("Total Insert() time: " + DateTime.Now.Subtract(start).ToString());

				sourceBitmap.Save(result.FullName, ImageFormat.Png);
			}
		}
		#endregion

		#endregion


	}
}
