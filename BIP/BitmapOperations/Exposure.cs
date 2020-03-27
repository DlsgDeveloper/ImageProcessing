using System;
using System.Drawing.Imaging;

namespace ImageProcessing.BitmapOperations
{
	public class Exposure
	{
		#region Go()
		/// <summary>
		/// X' = Math.Pow(X' * exposure + offset, 1 / gamma). For low quality scanners, try 0, 0.035, 0.6.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="exposure">Multiplier, from -20 to 20, default 0</param>
		/// <param name="offset">Adds, from -0.5 to 0.5, default 0</param>
		/// <param name="gamma">From 20 to 0.01, default 1</param>
		public static void Go(System.Drawing.Bitmap source, double exposure, double offset, double gamma)
		{
			BitmapData sourceData = null;

			int x, y;

			try
			{
				int width = source.Width;
				int height = source.Height;

				sourceData = source.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, source.PixelFormat);
				double r, g, b;

				unsafe
				{
					int		stride = sourceData.Stride;
					byte*	scan0 = (byte*)sourceData.Scan0.ToPointer();
					byte*	pCurrent;

					exposure = (exposure == 0) ? 1 : (exposure > 0) ? exposure + 1.0 : (1 / (1.0 - exposure)) ;
					offset = 256 * offset;
					gamma = 1 / gamma;

					if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < height; y++)
						{
							pCurrent = scan0 + y * stride;

							for (x = 0; x < width; x++)
							{
								b = Math.Pow((pCurrent[x * 3] * exposure + offset) / 255.0, gamma) * 255.0;
								g = Math.Pow((pCurrent[x * 3 + 1] * exposure + offset) / 255.0, gamma) * 255.0;
								r = Math.Pow((pCurrent[x * 3 + 2] * exposure + offset) / 255.0, gamma) * 255.0;

								if (b < 0)
									pCurrent[x * 3] = 0;
								else if (b > 255)
									pCurrent[x * 3] = 255;
								else
									pCurrent[x * 3] = (byte)b;

								if (g < 0)
									pCurrent[x * 3 + 1] = 0;
								else if (g > 255)
									pCurrent[x * 3 + 1] = 255;
								else
									pCurrent[x * 3 + 1] = (byte)g;

								if (r < 0)
									pCurrent[x * 3 + 2] = 0;
								else if (r > 255)
									pCurrent[x * 3 + 2] = 255;
								else
									pCurrent[x * 3 + 2] = (byte)r;
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format32bppArgb || source.PixelFormat == PixelFormat.Format32bppRgb)
					{
						for (y = 0; y < height; y++)
						{
							pCurrent = scan0 + y * stride;

							for (x = 0; x < width; x++)
							{
								b = Math.Pow((pCurrent[x * 4] * exposure + offset) / 255.0, gamma) * 255.0;
								g = Math.Pow((pCurrent[x * 4 + 1] * exposure + offset) / 255.0, gamma) * 255.0;
								r = Math.Pow((pCurrent[x * 4 + 2] * exposure + offset) / 255.0, gamma) * 255.0;

								if (b < 0)
									pCurrent[x * 4] = 0;
								else if (b > 255)
									pCurrent[x * 4] = 255;
								else
									pCurrent[x * 4] = (byte)b;

								if (g < 0)
									pCurrent[x * 4 + 1] = 0;
								else if (g > 255)
									pCurrent[x * 4 + 1] = 255;
								else
									pCurrent[x * 4 + 1] = (byte)g;

								if (r < 0)
									pCurrent[x * 4 + 2] = 0;
								else if (r > 255)
									pCurrent[x * 4 + 2] = 255;
								else
									pCurrent[x * 4 + 2] = (byte)r;
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < height; y++)
						{
							pCurrent = scan0 + y * stride;

							for (x = 0; x < width; x++)
							{
								b = Math.Pow((pCurrent[x] * exposure + offset) / 255.0, gamma) * 255.0;

								if (b < 0)
									pCurrent[x] = 0;
								else if (b > 255)
									pCurrent[x] = 255;
								else
									pCurrent[x] = (byte)b;
							}
						}
					}
					else
						throw new Exception("Exposure: Unsupported Pixel Format " + source.PixelFormat.ToString() + "!");
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
			}
		}
		#endregion

	}
}
