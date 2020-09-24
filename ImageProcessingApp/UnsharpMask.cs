using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TestApp
{
	class UnsharpMask
	{
		#region Sharpnen()
		public static void Sharpnen()
		{
			string source = @"C:\delete\del\IMG_0010.JPG";

			using(Bitmap b = new Bitmap(source))
			{
				for (float factor = 0.2F; factor < 5.0F; factor = factor + 0.2F)
				{
					using (Bitmap r = ImageProcessing.UnsharpMask.UnsharpMean3x3(b, factor))
					{
						r.Save(@"C:\delete\del\UnsharpMask "+factor.ToString("0.00")+" .png", System.Drawing.Imaging.ImageFormat.Png);
					}
				}
			}
		}
		#endregion
	
	}
}
