using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace ImageProcessing.BitmapOperations
{
	public class BitmapFiller
	{
		#region FillBitmap()
		/// <summary>
		/// Only for 24 bit color RGB.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="background"></param>
		public static void FillBitmap(System.Drawing.Bitmap source, Color background)
		{
			FillBitmap(source, background, Rectangle.Empty);
		}

		/// <summary>
		/// Only for 24 bit color RGB.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="background"></param>
		/// <param name="clip"></param>
		public static void FillBitmap(System.Drawing.Bitmap source, Color background, Rectangle clip)
		{
			BitmapData sourceData = null;

			int x, y;

			if (clip == Rectangle.Empty)
				clip = new Rectangle(0, 0, source.Width, source.Height);
			else
				clip.Intersect(new Rectangle(0, 0, source.Width, source.Height));

			if (clip != Rectangle.Empty)
			{
				try
				{
					sourceData = source.LockBits(clip, ImageLockMode.WriteOnly, source.PixelFormat);

					int width = sourceData.Width;
					int height = sourceData.Height;

					byte r = background.R;
					byte g = background.G;
					byte b = background.B;

					unsafe
					{
						int stride = sourceData.Stride;
						byte* scan0 = (byte*)sourceData.Scan0.ToPointer();
						byte* currentS;

						for (y = 0; y < height; y++)
						{
							currentS = scan0 + y * stride;

							for (x = 0; x < width; x++)
							{
								currentS[x * 3] = b;
								currentS[x * 3 + 1] = g;
								currentS[x * 3 + 2] = r;
							}
						}
					}
				}
				finally
				{
					if (source != null && sourceData != null)
						source.UnlockBits(sourceData);
				}
			}
		}
		#endregion

	}
}
