using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing.PageObjects;

namespace ImageProcessing
{
	public static class Debug
	{
		public static string SaveToDir { get { return @"C:\Users\jirka.stybnar\temp\IP\"; } }

		#region GetColor()
		public static Color GetColor(int index)
		{
			switch (index % 20)
			{
				case 0: return Color.LightSeaGreen;
				case 1: return Color.Yellow;
				case 2: return Color.Green;
				case 3: return Color.Blue;
				case 4: return Color.Red;
				case 5: return Color.Pink;
				case 6: return Color.LightBlue;
				case 7: return Color.LightGreen;
				case 8: return Color.Gray;
				case 9: return Color.LightGray;
				case 10: return Color.LightCoral;
				case 11: return Color.Brown;
				case 12: return Color.LightYellow;
				case 13: return Color.Orange;
				case 14: return Color.DarkGreen;
				case 15: return Color.DarkGray;
				case 16: return Color.DarkRed;
				case 17: return Color.DarkOrange;
				case 18: return Color.DarkBlue;
				case 19: return Color.DarkTurquoise;
				default: return Color.White;
			}

		}
		#endregion

		#region GetBitmap()
		public static Bitmap GetBitmap(Size imageSize)
		{
			Bitmap bitmap = new Bitmap(imageSize.Width, imageSize.Height, PixelFormat.Format24bppRgb);
			Graphics g = Graphics.FromImage(bitmap);
			
			g.Clear(Color.Black);
			g.Dispose();

			return bitmap;
		}
		#endregion

		#region DrawToFile()
		public static void DrawToFile(Pictures pictures, string fileName, Size imageSize)
		{
#if SAVE_RESULTS
			try
			{
				pictures.DrawToFile(Debug.SaveToDir + fileName, imageSize);
			}
			catch { }
			finally
			{
				GC.Collect();
			}
#endif
		}
		#endregion

	}
}
