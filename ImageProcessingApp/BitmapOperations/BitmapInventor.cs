using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace TestApp.BitmapOperations
{
	public static class BitmapInventor
	{

		#region Invert()
		public unsafe static void Invert()
		{
			/*string source = @"C:\Users\jirka.stybnar\TestRun\Click\Cropping\01.jpg";
			string result = string.Format(@"C:\Users\jirka.stybnar\TestRun\Click\Cropping\{0}_result.png", System.IO.Path.GetFileNameWithoutExtension(source));

			for (int i = 0; i < 1; i++)
			{
				using (Bitmap b = new Bitmap(source))
				{
					ImageProcessing.Scanning.BackgroundInvertor.MakeWhiteScannerBed(b);
					b.Save(result, ImageFormat.Png);
				}
			}*/
		}
		#endregion

	}
}
