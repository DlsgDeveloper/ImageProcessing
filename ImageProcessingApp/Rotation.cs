using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing;
using System.IO;

namespace ImageProcessingApp
{
	class Rotation
	{

		#region Rotate()
		public static void Rotate()
		{
			string source32bpp = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Rotation\RotateClip\01.png";
			string result32bpp = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Rotation\RotateClip\result32bpp.png";

			using (Bitmap b = new Bitmap(source32bpp))
			{
				/*
				DateTime start = DateTime.Now;
				using (Bitmap result = ImageProcessing.Rotation.Rotate32bpp(b, Math.PI / 4))
				{
					Console.WriteLine("Rotation: " + DateTime.Now.Subtract(start).ToString());

					result.Save(result32bpp, ImageFormat.Png);
				}
				*/
				DateTime start = DateTime.Now;
				using (Bitmap result = ImageProcessing.Rotation.RotateClip(b, Math.PI / 4, new Rectangle(100, 100, 100, 100), 0, 0, 0))
				{
					Console.WriteLine("Rotation: " + DateTime.Now.Subtract(start).ToString());

					result.Save(result32bpp, ImageFormat.Png);
				}
			}
		}
		#endregion

		#region GetClip()
		public static void GetClip()
		{
			string sourcePath = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Rotation\GetClip\01.jpg";
			string resultPath = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Rotation\GetClip\result.png";

			using (Bitmap b = new Bitmap(sourcePath))
			{
				DateTime start = DateTime.Now;

				using (Bitmap result = ImageProcessing.Rotation.GetClip(b, new Point(2117, 542), new Point(2752, 703), new Point(1979, 1063)))
				{
					Console.WriteLine("GetClip: " + DateTime.Now.Subtract(start).ToString());

					result.Save(resultPath, ImageFormat.Png);
				}
			}
		}
		#endregion

	}
}
