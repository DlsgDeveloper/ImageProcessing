using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace TestApp.BitmapOperations
{
	class BitmapFiller
	{

		#region Go()
		public unsafe static void Go()
		{
			string result = @"C:\delete\BitmapFiller.png";

			using (Bitmap b = new Bitmap(1000, 1000, PixelFormat.Format24bppRgb))
			{
				ImageProcessing.BitmapOperations.BitmapFiller.FillBitmap(b, System.Drawing.Color.Gold, new Rectangle(-200, 900, 250, 250));
				ImageProcessing.BitmapOperations.BitmapFiller.FillBitmap(b, System.Drawing.Color.Silver, new Rectangle(900, -200, 250, 250));
				ImageProcessing.BitmapOperations.BitmapFiller.FillBitmap(b, System.Drawing.Color.SaddleBrown, new Rectangle(500, 900, 250, 250));
				ImageProcessing.BitmapOperations.BitmapFiller.FillBitmap(b, System.Drawing.Color.SeaGreen, new Rectangle(900, 900, 250, 250));

				b.Save(result, ImageFormat.Png);
			}
		}
		#endregion

	}
}
