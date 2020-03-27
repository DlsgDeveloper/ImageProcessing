using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	public class ThumbnailCreator
	{

		//PUBLIC METHODS
		#region public methods

		#region Get()
		public static Bitmap Get(Bitmap bmpSource, Size desiredSize)
		{
			Bitmap resizedImage = Resizing.GetThumbnail(bmpSource, desiredSize);

			return resizedImage;
		}

		public static Bitmap Get(Bitmap bmpSource, Size desiredSize, PixelFormat desiredPixelFormat)
		{
			Bitmap resizedImage =	Resizing.GetThumbnail(bmpSource, desiredSize);
			Bitmap thumbnail =		Get(resizedImage, desiredPixelFormat);

			resizedImage.Dispose();
			return thumbnail;
		}

		public static Bitmap Get(Bitmap bmpSource, PixelFormat desiredPixelFormat)
		{
			if (bmpSource == null)
				return null;

			Rectangle clip = Rectangle.FromLTRB(0, 0, bmpSource.Width, bmpSource.Height);

			Bitmap bmpResult = null;

#if DEBUG
			try
			{
#endif
				switch (bmpSource.PixelFormat)
				{
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
						bmpResult = GetFrom32bpp(bmpSource, clip, desiredPixelFormat); break;
					case PixelFormat.Format24bppRgb: bmpResult = GetFrom24bpp(bmpSource, clip, desiredPixelFormat); break;
					case PixelFormat.Format8bppIndexed: bmpResult = GetFrom8bpp(bmpSource, clip, desiredPixelFormat); break;
					case PixelFormat.Format1bppIndexed: bmpResult = GetFrom1bpp(bmpSource, clip, desiredPixelFormat); break;
					default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}

				if (bmpResult != null)
				{
					Misc.SetBitmapResolution(bmpResult, (float)(bmpSource.HorizontalResolution), (float)(bmpSource.VerticalResolution));

					if (bmpResult.PixelFormat == PixelFormat.Format8bppIndexed || bmpResult.PixelFormat == PixelFormat.Format4bppIndexed)
						bmpResult.Palette = Misc.GetGrayscalePalette(bmpResult.PixelFormat);
				}
#if DEBUG
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
#endif

			return bmpResult;
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region GetFrom32bpp()
		private static Bitmap GetFrom32bpp(Bitmap source, Rectangle clip, PixelFormat desiredPixelFormat)
		{
			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y;

			try
			{
				int width = clip.Width;
				int height = clip.Height;

				if (desiredPixelFormat == PixelFormat.Format1bppIndexed)
					result = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				else
					result = new Bitmap(width, height, desiredPixelFormat);

				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;


				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();
					byte* currentS, currentR;

					if (desiredPixelFormat == PixelFormat.Format32bppRgb || desiredPixelFormat == PixelFormat.Format32bppArgb)
					{
						for (y = 0; y < height; y++)
						{
							currentS = scan0S + y * strideS;
							currentR = scan0R + y * strideR;

							for (x = 0; x < width; x++)
							{
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);
							}
						}
					}
					else if (desiredPixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < height; y++)
						{
							currentS = scan0S + y * strideS;
							currentR = scan0R + y * strideR;

							for (x = 0; x < width; x++)
							{
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);

								currentS++;
							}
						}
					}
					else if (desiredPixelFormat == PixelFormat.Format8bppIndexed || desiredPixelFormat == PixelFormat.Format1bppIndexed)
					{
						for (y = 0; y < height; y++)
						{
							currentS = scan0S + y * strideS;
							currentR = scan0R + y * strideR;

							for (x = 0; x < width; x++)
							{
								*(currentR++) = (byte)(0.299 * currentS[2] + 0.587 * currentS[1] + 0.114 * currentS[0]);

								currentS = currentS + 4;
							}
						}

						if (desiredPixelFormat == PixelFormat.Format1bppIndexed)
							MakeGrayscaleLookBitonal(resultData);
					}
				}

				return result;
			}
#if DEBUG
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return null;
			}
#endif
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region GetFrom24bpp()
		private static Bitmap GetFrom24bpp(Bitmap source, Rectangle clip, PixelFormat desiredPixelFormat)
		{
			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y;

			try
			{
				int width = clip.Width;
				int height = clip.Height;

				if (desiredPixelFormat == PixelFormat.Format1bppIndexed)
					result = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				else
					result = new Bitmap(width, height, desiredPixelFormat);

				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;


				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();
					byte* currentS, currentR;

					if (desiredPixelFormat == PixelFormat.Format32bppRgb || desiredPixelFormat == PixelFormat.Format32bppArgb)
					{
						for (y = 0; y < height; y++)
						{
							currentS = scan0S + y * strideS;
							currentR = scan0R + y * strideR;

							for (x = 0; x < width; x++)
							{
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);
								*(currentR++) = 255;
							}
						}
					}
					else if (desiredPixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < height; y++)
						{
							currentS = scan0S + y * strideS;
							currentR = scan0R + y * strideR;

							for (x = 0; x < width; x++)
							{
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);
							}
						}
					}
					else if (desiredPixelFormat == PixelFormat.Format8bppIndexed || desiredPixelFormat == PixelFormat.Format1bppIndexed)
					{
						for (y = 0; y < height; y++)
						{
							currentS = scan0S + y * strideS;
							currentR = scan0R + y * strideR;

							for (x = 0; x < width; x++)
							{
								*(currentR++) = (byte)(0.299 * currentS[2] + 0.587 * currentS[1] + 0.114 * currentS[0]);

								currentS = currentS + 3;
							}
						}

						if (desiredPixelFormat == PixelFormat.Format1bppIndexed)
							MakeGrayscaleLookBitonal(resultData);
					}
				}

				return result;
			}
#if DEBUG
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return null;
			}
#endif
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region GetFrom8bpp()
		private static Bitmap GetFrom8bpp(Bitmap source, Rectangle clip, PixelFormat desiredPixelFormat)
		{
			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y;

			try
			{
				int width = clip.Width;
				int height = clip.Height;

				if (desiredPixelFormat == PixelFormat.Format1bppIndexed)
					result = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				else
					result = new Bitmap(width, height, desiredPixelFormat);

				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;


				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();
					byte* currentS, currentR;

					if (desiredPixelFormat == PixelFormat.Format32bppRgb || desiredPixelFormat == PixelFormat.Format32bppArgb)
					{
						for (y = 0; y < height; y++)
						{
							currentS = scan0S + y * strideS;
							currentR = scan0R + y * strideR;

							for (x = 0; x < width; x++)
							{
								*(currentR++) = *currentS;
								*(currentR++) = *currentS;
								*(currentR++) = *currentS;
								*(currentR++) = 255;

								currentS++;
							}
						}
					}
					else if (desiredPixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < height; y++)
						{
							currentS = scan0S + y * strideS;
							currentR = scan0R + y * strideR;

							for (x = 0; x < width; x++)
							{
								*(currentR++) = *currentS;
								*(currentR++) = *currentS;
								*(currentR++) = *currentS;

								currentS++;
							}
						}
					}
					else if (desiredPixelFormat == PixelFormat.Format8bppIndexed || desiredPixelFormat == PixelFormat.Format1bppIndexed)
					{
						for (y = 0; y < height; y++)
						{
							currentS = scan0S + y * strideS;
							currentR = scan0R + y * strideR;

							for (x = 0; x < width; x++)
							{
								*(currentR++) = *(currentS++);
							}
						}

						if (desiredPixelFormat == PixelFormat.Format1bppIndexed)
							MakeGrayscaleLookBitonal(resultData);
					}
				}

				return result;
			}
#if DEBUG
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return null;
			}
#endif
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region GetFrom1bpp()
		private static Bitmap GetFrom1bpp(Bitmap source, Rectangle clip, PixelFormat desiredPixelFormat)
		{
			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y;

			try
			{
				int width = clip.Width;
				int height = clip.Height;

				if (desiredPixelFormat == PixelFormat.Format1bppIndexed)
					result = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				else
					result = new Bitmap(width, height, desiredPixelFormat);

				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;


				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();
					byte* currentR;

					if (desiredPixelFormat == PixelFormat.Format32bppRgb || desiredPixelFormat == PixelFormat.Format32bppArgb)
					{
						for (y = 0; y < height; y++)
						{
							currentR = scan0R + y * strideR;

							for (x = 0; x < width; x++)
							{
								if ((scan0S[y * strideS + x / 8] & (0x80 >> (x & 0x07))) > 0)
								{
									currentR[0] = 255;
									currentR[1] = 255;
									currentR[2] = 255;
								}

								currentR[3] = 255;
								currentR = currentR + 4;
							}
						}
					}
					else if (desiredPixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < height; y++)
						{
							currentR = scan0R + y * strideR;

							for (x = 0; x < width; x++)
							{
								if ((scan0S[y * strideS + x / 8] & (0x80 >> (x & 0x07))) > 0)
								{
									currentR[0] = 255;
									currentR[1] = 255;
									currentR[2] = 255;
								}

								currentR = currentR + 3;
							}
						}
					}
					else if (desiredPixelFormat == PixelFormat.Format8bppIndexed || desiredPixelFormat == PixelFormat.Format1bppIndexed)
					{
						for (y = 0; y < height; y++)
						{
							currentR = scan0R + y * strideR;

							for (x = 0; x < width; x++)
							{
								if ((scan0S[y * strideS + x / 8] & (0x80 >> (x & 0x07))) > 0)
								{
									currentR[0] = 255;
								}

								currentR ++;
							}
						}

						if (desiredPixelFormat == PixelFormat.Format1bppIndexed)
							MakeGrayscaleLookBitonal(resultData);
					}
				}

				return result;
			}
#if DEBUG
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				return null;
			}
#endif
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region MakeGrayscaleLookBitonal()
		private static unsafe void MakeGrayscaleLookBitonal(BitmapData bitmapData)
		{
			int x, y;
			byte min = 255, max = 0;
			int width = bitmapData.Width;
			int height = bitmapData.Height;
			int colorsCount = 16;
			int divider = 256 / colorsCount;

			byte* scan0 = (byte*)bitmapData.Scan0.ToPointer();
			int stride = bitmapData.Stride;
			byte g;

			for (y = 0; y < height; y++)
			{
				for (x = 0; x < width; x++)
				{
					g = scan0[y * stride + x];

					if (min > g)
						min = g;
					if (max < g)
						max = g;

					if (min == 0 && max == 255)
					{
						y = height;
						break;
					}
				}
			}

			if (max > min)
			{
				byte* pCurrent;

				for (y = 0; y < height; y++)
				{
					pCurrent = scan0 + y * stride;

					for (x = 0; x < width; x++)
					{
						*pCurrent = (byte)Math.Max(0, Math.Min(255, (((int)((((*pCurrent - min) * 255) / (max - min))) / divider) * divider)));

						pCurrent++;
					}
				}
			}
		}
		#endregion

		#endregion


	}
}
