using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	public class AutoColor
	{

		//	PUBLIC METHODS

		#region Get()
		public static void Get(Bitmap bitmap)
		{
			Get(bitmap, Rectangle.Empty);
		}

		public static void Get(Bitmap bitmap, Rectangle clip)
		{
			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			else
				clip.Intersect(new Rectangle(0, 0, bitmap.Width, bitmap.Height));

			try
			{
#if DEBUG
				DateTime start = DateTime.Now;
#endif
				HistogramSaturation histogram = new HistogramSaturation(bitmap, clip);
#if DEBUG
				Console.WriteLine("AutoColor Get Histogram():" + (DateTime.Now.Subtract(start)).ToString());
#endif

				if (histogram.Maximum > histogram.Minimum)
				{
					switch (bitmap.PixelFormat)
					{
						case PixelFormat.Format24bppRgb:
						case PixelFormat.Format32bppArgb:
						case PixelFormat.Format32bppRgb:
							RunAlgorithm(bitmap, clip, histogram.Minimum, histogram.Maximum);
							break;
						default:
							break;
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("AutoColor, Get(): " + ex.Message);
			}
		}
		#endregion

		//PRIVATE METHODS

		#region RunAlgorithm()
		private static void RunAlgorithm(Bitmap bitmap, Rectangle clip, byte minimumB, byte maximumB)
		{
			BitmapData bitmapData = null;

			try
			{
				float minimum = minimumB / 255F, maximum = maximumB / 255F;

				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat);
				int stride = bitmapData.Stride;
				int jump = bitmap.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;
				float ratio = 1F / (maximum - minimum);

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					int clipWidth = clip.Width;
					int clipHeight = clip.Height;
					int x, y;

					for (y = 0; y < clipHeight; y++)
					{
						pCurrent = pSource + (y * stride);

						for (x = 0; x < clipWidth; x++)
						{
							//Hsl hsl = new Hsl(pCurrent[2], pCurrent[1], pCurrent[0]);
							//hsl.Saturation = ((hsl.Saturation - minimum) * ratio);

							/*Color color = hsl.GetColor();

							pCurrent[0] = color.B;
							pCurrent[1] = color.G;
							pCurrent[2] = color.R;*/
							//hsl.GetColor(out r, out g, out b);

							Hsl.IncreaseSaturation(ref pCurrent[2], ref pCurrent[1], ref pCurrent[0], minimum, ratio);

							pCurrent += jump;
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion
	}
}
