using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ImageProcessingApp.BitmapOperations
{
	class Insertor
	{
		public static void Go32To24bpp()
		{
			string source = @"C:\Users\jirka.stybnar\TestRun\IT\BitmapOperations\Insertor\source01.jpg";
			string icon = @"C:\Users\jirka.stybnar\TestRun\IT\BitmapOperations\Insertor\icon01.png";
			string dest = @"C:\Users\jirka.stybnar\TestRun\IT\BitmapOperations\Insertor\dest.jpg";

			using (Bitmap sourceBitmap = new Bitmap(source))
			{
				using (Bitmap iconBitmap = new Bitmap(icon))
				{
					ImageProcessing.BitmapOperations.Insertor.Insert(sourceBitmap, iconBitmap, new Point(100,100));

					sourceBitmap.Save(dest, System.Drawing.Imaging.ImageFormat.Jpeg);
				}
			}
		}

		public static void Go32To32bpp()
		{
			string source = @"C:\Users\jirka.stybnar\TestRun\IT\BitmapOperations\Insertor\gray.png";
			string icon = @"C:\Users\jirka.stybnar\TestRun\IT\BitmapOperations\Insertor\icon02.png";
			string dest = @"C:\Users\jirka.stybnar\TestRun\IT\BitmapOperations\Insertor\dest.png";

			using (Bitmap sourceBitmap = new Bitmap(source))
			{
				using (Bitmap iconBitmap = new Bitmap(icon))
				{
					ImageProcessing.BitmapOperations.Insertor.Insert(sourceBitmap, iconBitmap, new Point(20, 100));

					sourceBitmap.Save(dest, System.Drawing.Imaging.ImageFormat.Png);
				}
			}
		}

		public static void Go32To8bpp()
		{
			string source = @"C:\Users\jirka.stybnar\TestRun\IT\BitmapOperations\Insertor\gray.jpg";
			string icon = @"C:\Users\jirka.stybnar\TestRun\IT\BitmapOperations\Insertor\gray.jpg";
			string dest = @"C:\Users\jirka.stybnar\TestRun\IT\BitmapOperations\Insertor\dest.png";

			using (Bitmap sourceBitmap = new Bitmap(source))
			{
				using (Bitmap iconBitmap = new Bitmap(icon))
				{
					using (Bitmap resampled = ImageProcessing.Resampling.Resample(iconBitmap, ImageProcessing.PixelsFormat.Format32bppRgb))
					{
						ImageProcessing.BitmapOperations.Insertor.Insert(sourceBitmap, resampled, new Point(300, 300));

						sourceBitmap.Save(dest, System.Drawing.Imaging.ImageFormat.Png);
					}
				}
			}
		}

	}
}
