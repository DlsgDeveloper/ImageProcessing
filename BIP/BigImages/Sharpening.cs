using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageProcessing.Languages;

namespace ImageProcessing.BigImages
{
	public class Sharpening
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;


		public Sharpening()
		{
		}


		//PUBLIC METHODS
		#region public methods

		#region Laplacian3x3()
		/// <summary>
		/// sharpens image by applying Laplacian mask
		/// [-1 -1 -1]
		/// [-1 +8 -1]
		/// [-1 -1 -1]
		/// </summary>
		/// <param name="itDecoder"></param>
		/// <param name="destPath"></param>
		/// <param name="fileFormat"></param>
		public void Laplacian3x3(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat fileFormat)
		{
			ImageProcessing.BigImages.ItEncoder itEncoder = null;

			try
			{
				itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, fileFormat, itDecoder.PixelsFormat, itDecoder.Width, itDecoder.Height, itDecoder.DpiX, itDecoder.DpiY);

				itEncoder.SetPalette(itDecoder);

				Laplacian3x3Internal(itDecoder, itEncoder);
			}
			catch (Exception ex)
			{
				if (itEncoder != null)
				{
					itEncoder.Dispose();
					itEncoder = null;
				}

				try
				{
					if (File.Exists(destPath))
						File.Delete(destPath);
				}
				catch { }

				throw ex;
			}
			finally
			{
				if (itEncoder != null)
					itEncoder.Dispose();
			}
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region Laplacian3x3Internal()
		private unsafe void Laplacian3x3Internal(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder)
		{
			Bitmap source = null;
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			int resultW = itEncoder.Width;
			int resultH = itEncoder.Height;

			int stripHeightMax = Misc.GetStripHeightMax(itDecoder);

			for (int stripY = 0; stripY < resultH; stripY += stripHeightMax)
			{
				int stripHeight = Math.Min(resultH - stripY, stripHeightMax);

				try
				{
					result = new Bitmap(resultW, stripHeight, itDecoder.PixelFormat);
					resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					Rectangle resultClip = new Rectangle(0, stripY, itDecoder.Width, stripHeight);
					Rectangle sourceClip = Rectangle.FromLTRB(0, Math.Max(0, stripY - 1), itDecoder.Width, Math.Min(itDecoder.Height, stripY + stripHeight + 1));

					bool interpolateTop = (sourceClip.Y > 0);
					bool interpolateBottom = (sourceClip.Bottom < itDecoder.Height - 1);

					source = itDecoder.GetClip(sourceClip);
					sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

					byte* pS;
					byte* pR;

					int sStride = sourceData.Stride;
					int rStride = resultData.Stride;

					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					#region 24 bpp
					if (source.PixelFormat == PixelFormat.Format24bppRgb || source.PixelFormat == PixelFormat.Format32bppArgb || source.PixelFormat == PixelFormat.Format32bppRgb)
					{
						int r, g, b;
						int pixelBytes = (source.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
						int sourceYShuffle = (interpolateTop) ? 1 : 0;

						for (int y = 1; y < resultData.Height - 1; y++)
						{
							pS = pSource + ((y + sourceYShuffle) * sStride) + (pixelBytes);
							pR = pResult + (y * rStride) + (pixelBytes);

							for (int x = 1; x < sourceData.Width - 1; x++)
							{
								/*b = (+8 * pS[0] - pS[-sStride - 3] - pS[-sStride + 0] - pS[-sStride + 3] - pS[-3] - pS[+3] - pS[+sStride - 3] - pS[+sStride + 0] - pS[+sStride + 3]);
								g = (+8 * pS[1] - pS[-sStride - 2] - pS[-sStride + 1] - pS[-sStride + 4] - pS[-2] - pS[+4] - pS[+sStride - 2] - pS[+sStride + 1] - pS[+sStride + 4]);
								r = (+8 * pS[2] - pS[-sStride - 1] - pS[-sStride + 2] - pS[-sStride + 5] - pS[-1] - pS[+5] - pS[+sStride - 1] - pS[+sStride + 2] - pS[+sStride + 5]);

								double delta = (0.299 * b + 0.587 * g + 0.114 * r);

								pR[0] = (byte)((pS[0] + delta > 255) ? 255 : ((pS[0] + delta < 0) ? 0 : pS[0] + delta));
								pR[1] = (byte)((pS[1] + delta > 255) ? 255 : ((pS[1] + delta < 0) ? 0 : pS[1] + delta));
								pR[2] = (byte)((pS[2] + delta > 255) ? 255 : ((pS[2] + delta < 0) ? 0 : pS[2] + delta));
								*/
								b = pS[0] + (+8 * pS[0] - pS[-sStride - 3] - pS[-sStride + 0] - pS[-sStride + 3] - pS[-3] - pS[+3] - pS[+sStride - 3] - pS[+sStride + 0] - pS[+sStride + 3]);
								g = pS[1] +(+8 * pS[1] - pS[-sStride - 2] - pS[-sStride + 1] - pS[-sStride + 4] - pS[-2] - pS[+4] - pS[+sStride - 2] - pS[+sStride + 1] - pS[+sStride + 4]);
								r = pS[2] + (+8 * pS[2] - pS[-sStride - 1] - pS[-sStride + 2] - pS[-sStride + 5] - pS[-1] - pS[+5] - pS[+sStride - 1] - pS[+sStride + 2] - pS[+sStride + 5]);

								pR[0] = (byte)((b > 255) ? 255 : ((b < 0) ? 0 : b));
								pR[1] = (byte)((g > 255) ? 255 : ((g < 0) ? 0 : g));
								pR[2] = (byte)((r > 255) ? 255 : ((r < 0) ? 0 : r));

								pS += pixelBytes;
								pR += pixelBytes;
							}
						}

						//first and last column
						for (int y = 0; y < resultData.Height; y++)
						{
							pResult[y * rStride + 0] = pSource[(y + sourceYShuffle) * sStride + 0];
							pResult[y * rStride + 1] = pSource[(y + sourceYShuffle) * sStride + 1];
							pResult[y * rStride + 2] = pSource[(y + sourceYShuffle) * sStride + 2];

							pResult[y * rStride + (resultW - 1) * pixelBytes + 0] = pSource[(y + sourceYShuffle) * sStride + (resultW - 1) * pixelBytes + 0];
							pResult[y * rStride + (resultW - 1) * pixelBytes + 1] = pSource[(y + sourceYShuffle) * sStride + (resultW - 1) * pixelBytes + 1];
							pResult[y * rStride + (resultW - 1) * pixelBytes + 2] = pSource[(y + sourceYShuffle) * sStride + (resultW - 1) * pixelBytes + 2];
						}
						//first row
						if (interpolateTop)
						{
							pS = pSource + pixelBytes;
							pR = pResult + pixelBytes;
							
							for (int x = 1; x < sourceData.Width - 1; x++)
							{
								b = pS[0] +(+8 * pS[0] - pS[-sStride - 3] - pS[-sStride + 0] - pS[-sStride + 3] - pS[-3] - pS[+3] - pS[+sStride - 3] - pS[+sStride + 0] - pS[+sStride + 3]);
								g = pS[1] +(+8 * pS[1] - pS[-sStride - 2] - pS[-sStride + 1] - pS[-sStride + 4] - pS[-2] - pS[+4] - pS[+sStride - 2] - pS[+sStride + 1] - pS[+sStride + 4]);
								r = pS[2] +(+8 * pS[2] - pS[-sStride - 1] - pS[-sStride + 2] - pS[-sStride + 5] - pS[-1] - pS[+5] - pS[+sStride - 1] - pS[+sStride + 2] - pS[+sStride + 5]);

								pR[0] = (byte)((b > 255) ? 255 : ((b < 0) ? 0 : b));
								pR[1] = (byte)((g > 255) ? 255 : ((g < 0) ? 0 : g));
								pR[2] = (byte)((r > 255) ? 255 : ((r < 0) ? 0 : r));

								pS += pixelBytes;
								pR += pixelBytes;
							}
						}
						else
						{
							for (int x = 1; x < sourceData.Width - 1; x++)
							{
								pResult[x * pixelBytes + 0] = pSource[x * pixelBytes + 0];
								pResult[x * pixelBytes + 1] = pSource[x * pixelBytes + 1];
								pResult[x * pixelBytes + 2] = pSource[x * pixelBytes + 2];
							}
						}
						// last row
						if (interpolateBottom)
						{
							pS = pSource + (sourceData.Height - 3) + sStride + pixelBytes;
							pR = pResult + (resultH - 1) * rStride + pixelBytes;

							for (int x = 1; x < sourceData.Width - 1; x++)
							{
								b = pS[0] + (+8 * pS[0] - pS[-sStride - 3] - pS[-sStride + 0] - pS[-sStride + 3] - pS[-3] - pS[+3] - pS[+sStride - 3] - pS[+sStride + 0] - pS[+sStride + 3]);
								g = pS[1] + (+8 * pS[1] - pS[-sStride - 2] - pS[-sStride + 1] - pS[-sStride + 4] - pS[-2] - pS[+4] - pS[+sStride - 2] - pS[+sStride + 1] - pS[+sStride + 4]);
								r = pS[2] + (+8 * pS[2] - pS[-sStride - 1] - pS[-sStride + 2] - pS[-sStride + 5] - pS[-1] - pS[+5] - pS[+sStride - 1] - pS[+sStride + 2] - pS[+sStride + 5]);

								pR[0] = (byte)((b > 255) ? 255 : ((b < 0) ? 0 : b));
								pR[1] = (byte)((g > 255) ? 255 : ((g < 0) ? 0 : g));
								pR[2] = (byte)((r > 255) ? 255 : ((r < 0) ? 0 : r));

								pS += pixelBytes;
								pR += pixelBytes;
							}
						}
						else
						{
							for (int x = 1; x < sourceData.Width - 1; x++)
							{
								pResult[(resultH - 1) * rStride + x * pixelBytes + 0] = pSource[(sourceData.Height - 1) * sStride + x * pixelBytes + 0];
								pResult[(resultH - 1) * rStride + x * pixelBytes + 1] = pSource[(sourceData.Height - 1) * sStride + x * pixelBytes + 1];
								pResult[(resultH - 1) * rStride + x * pixelBytes + 2] = pSource[(sourceData.Height - 1) * sStride + x * pixelBytes + 2];
							}
						}
					}
					#endregion

					#region 8 bpp
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						int g;
						int sourceYShuffle = (interpolateTop) ? 1 : 0;

						for (int y = 1; y < resultData.Height - 1; y++)
						{
							pS = pSource + ((y + sourceYShuffle) * sStride) + 1;
							pR = pResult + (y * rStride) + 1;

							for (int x = 1; x < sourceData.Width - 1; x++)
							{
								g = pS[0] + (+8 * pS[1] - pS[-sStride - 1] - pS[-sStride] - pS[-sStride + 1] - pS[-1] - pS[+1] - pS[+sStride - 1] - pS[+sStride] - pS[+sStride + 1]);

								pR[0] = (byte)((g > 255) ? 255 : ((g < 0) ? 0 : g));

								pS ++;
								pR ++;
							}
						}

						//first and last column
						for (int y = 0; y < resultData.Height; y++)
						{
							pResult[y * rStride + 0] = pSource[(y + sourceYShuffle) * sStride];
							pResult[y * rStride + (resultW - 1)] = pSource[(y + sourceYShuffle) * sStride + (resultW - 1)];
						}
						//first row
						if (interpolateTop)
						{
							pS = pSource + 1;
							pR = pResult + 1;

							for (int x = 1; x < sourceData.Width - 1; x++)
							{
								g = pS[0] + (+8 * pS[0] - pS[-sStride - 1] - pS[-sStride + 0] - pS[-sStride + 1] - pS[-1] - pS[+1] - pS[+sStride - 1] - pS[+sStride + 0] - pS[+sStride + 1]);

								pR[0] = (byte)((g > 255) ? 255 : ((g < 0) ? 0 : g));

								pS ++;
								pR ++;
							}
						}
						else
						{
							for (int x = 1; x < sourceData.Width - 1; x++)
							{
								pResult[x] = pSource[x];
							}
						}
						// last row
						if (interpolateBottom)
						{
							pS = pSource + (sourceData.Height - 3) + sStride + 1;
							pR = pResult + (resultH - 1) * rStride + 1;

							for (int x = 1; x < sourceData.Width - 1; x++)
							{
								g = pS[0] + (+8 * pS[0] - pS[-sStride - 1] - pS[-sStride + 0] - pS[-sStride + 1] - pS[-1] - pS[+1] - pS[+sStride - 1] - pS[+sStride + 0] - pS[+sStride + 1]);

								pR[0] = (byte)((g > 255) ? 255 : ((g < 0) ? 0 : g));

								pS ++;
								pR ++;
							}
						}
						else
						{
							for (int x = 1; x < sourceData.Width - 1; x++)
							{
								pResult[(resultH - 1) * rStride + x] = pSource[(sourceData.Height - 1) * sStride + x];
							}
						}
					}
					#endregion

					#region 1 bpp
					else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						throw new Exception(BIPStrings.CanTSharpen1BitPerPixelImage_STR);
					}
					#endregion

				}
				finally
				{
					if ((source != null) && (sourceData != null))
						source.UnlockBits(sourceData);

					if (resultData != null)
					{
						itEncoder.Write(stripHeight, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
						result.UnlockBits(resultData);
						resultData = null;
					}
					if (result != null)
					{
						result.Dispose();
						result = null;
					}
				}

				if (this.ProgressChanged != null)
					this.ProgressChanged((stripY + stripHeight) / (float)resultH);
			}
		}
		#endregion

		#endregion
	
	}
}
