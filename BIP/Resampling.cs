using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Diagnostics;

namespace ImageProcessing
{
	public class Resampling
	{

		//PUBLIC METHODS
		#region public methods

		#region Resample()
		public static Bitmap Resample(Bitmap bmpSource, ImageProcessing.PixelsFormat pixelFormat)
		{
			return Resample(bmpSource, Rectangle.Empty, pixelFormat);
		}

		public static Bitmap Resample(Bitmap bmpSource, Rectangle clip, ImageProcessing.PixelsFormat pixelFormat)
		{
			if (bmpSource == null)
				return null;

			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bmpSource.Width, bmpSource.Height);
			else
				clip.Intersect(new Rectangle(0, 0, bmpSource.Width, bmpSource.Height));

			Bitmap bmpResult = null;

			switch (bmpSource.PixelFormat)
			{
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:
					{
						switch (pixelFormat)
						{
							case PixelsFormat.Format24bppRgb: bmpResult = ResampleTo24bpp(bmpSource, clip); break;
							case PixelsFormat.Format8bppIndexed: bmpResult = ResampleTo8bppIndexed(bmpSource, clip); break;
							case PixelsFormat.Format8bppGray: bmpResult = ResampleTo8bppGray(bmpSource, clip); break;
							case PixelsFormat.Format4bppGray: bmpResult = ResampleTo4bppGray(bmpSource, clip); break;
							case PixelsFormat.FormatBlackWhite: bmpResult = ResampleTo1bpp(bmpSource, clip); break;
							default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
						} break;	
					}
				case PixelFormat.Format24bppRgb:
					{
						switch (pixelFormat)
						{
							case PixelsFormat.Format32bppRgb: bmpResult = ResampleTo32bpp(bmpSource, clip); break;
							case PixelsFormat.Format8bppIndexed: bmpResult = ResampleTo8bppIndexed(bmpSource, clip); break;
							case PixelsFormat.Format8bppGray: bmpResult = ResampleTo8bppGray(bmpSource, clip); break;
							case PixelsFormat.Format4bppGray: bmpResult = ResampleTo4bppGray(bmpSource, clip); break;
							case PixelsFormat.FormatBlackWhite: bmpResult = ResampleTo1bpp(bmpSource, clip); break;
							default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
						} break;
					}
				case PixelFormat.Format8bppIndexed:
					{
						switch (pixelFormat)
						{
							case PixelsFormat.Format32bppRgb: bmpResult = ResampleTo32bpp(bmpSource, clip); break;
							case PixelsFormat.Format24bppRgb: bmpResult = ResampleTo24bpp(bmpSource, clip); break;
							case PixelsFormat.Format8bppIndexed: bmpResult = ResampleTo8bppIndexed(bmpSource, clip); break;
							case PixelsFormat.Format8bppGray: bmpResult = ResampleTo8bppGray(bmpSource, clip); break;
							case PixelsFormat.Format4bppGray: bmpResult = ResampleTo4bppGray(bmpSource, clip); break;
							case PixelsFormat.FormatBlackWhite: bmpResult = ResampleTo1bpp(bmpSource, clip); break;
							default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
						} break;
					}
				case PixelFormat.Format4bppIndexed:
					{
						switch (pixelFormat)
						{
							case PixelsFormat.Format32bppRgb: bmpResult = ResampleTo32bpp(bmpSource, clip); break;
							case PixelsFormat.Format24bppRgb: bmpResult = ResampleTo24bpp(bmpSource, clip); break;
							case PixelsFormat.Format8bppIndexed: bmpResult = ResampleTo8bppIndexed(bmpSource, clip); break;
							case PixelsFormat.Format8bppGray: bmpResult = ResampleTo8bppGray(bmpSource, clip); break;
							case PixelsFormat.FormatBlackWhite: bmpResult = ResampleTo1bpp(bmpSource, clip); break;
							default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
						}
					} break;
				case PixelFormat.Format1bppIndexed:
					{
						switch (pixelFormat)
						{
							case PixelsFormat.Format32bppRgb: bmpResult = ResampleTo32bpp(bmpSource, clip); break;
							case PixelsFormat.Format24bppRgb: bmpResult = ResampleTo24bpp(bmpSource, clip); break;
							case PixelsFormat.Format8bppIndexed: bmpResult = ResampleTo8bppIndexed(bmpSource, clip); break;
							case PixelsFormat.Format8bppGray: bmpResult = ResampleTo8bppGray(bmpSource, clip); break;
							case PixelsFormat.Format4bppGray: bmpResult = ResampleTo4bppGray(bmpSource, clip); break;
							case PixelsFormat.FormatBlackWhite: bmpResult = ResampleTo4bppGray(bmpSource, clip); break;
							default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
						}
					} break;
				default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}

			if (bmpResult != null)
			{
				Misc.SetBitmapResolution(bmpResult, (float)(bmpSource.HorizontalResolution), (float)(bmpSource.VerticalResolution));
			}

			return bmpResult;
		}
		#endregion

		#endregion

		
		//PRIVATE METHODS
		#region private methods

		#region ResampleTo32bpp()
		private unsafe static Bitmap ResampleTo32bpp(Bitmap source, Rectangle clip)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format32bppArgb);
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int width = sourceData.Width;
				int height = sourceData.Height;

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				byte* pCurrentS, pCurrentR;

				if (source.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (int y = 0; y < height; y++)
					{
						pCurrentS = pSource + y * strideS;
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x++)
						{
							pCurrentR[0] = pCurrentS[0];
							pCurrentR[1] = pCurrentS[1];
							pCurrentR[2] = pCurrentS[2];
							pCurrentR[3] = 255;

							pCurrentS += 3;
							pCurrentR += 4;
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					if (Misc.IsGrayscale(source) == false)
					{
						Color[] entries = source.Palette.Entries;
						
						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								Color c = entries[pCurrentS[0]];
								
								pCurrentR[0] = c.B;
								pCurrentR[1] = c.G;
								pCurrentR[2] = c.R;
								pCurrentR[3] = 255;

								pCurrentS++;
								pCurrentR += 4;
							}
						}
					}
					else
					{
						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								pCurrentR[0] = pCurrentS[0];
								pCurrentR[1] = pCurrentS[0];
								pCurrentR[2] = pCurrentS[0];
								pCurrentR[3] = 255;

								pCurrentS++;
								pCurrentR += 4;
							}
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format4bppIndexed)
				{
					if (Misc.IsGrayscale(source) == false)
					{
						Color[] entries = source.Palette.Entries;
						Color c;

						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								if ((x % 2) == 0)
									c = entries[pSource[y * strideS + x / 2] >> 4];
								else
									c = entries[pSource[y * strideS + x / 2] & 0xF];

								pCurrentR[0] = c.B;
								pCurrentR[1] = c.G;
								pCurrentR[2] = c.R;
								pCurrentR[3] = 255;

								pCurrentR += 4;
							}
						}
					}
					else
					{
						byte g;
						
						for (int y = 0; y < height; y++)
						{
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								if ((x % 2) == 0)
									g = (byte)(pSource[y * strideS + x / 2] >> 4);
								else
									g = (byte)(pSource[y * strideS + x / 2] & 0xF);

								pCurrentR[0] = (byte)(g * 8);
								pCurrentR[1] = (byte)(g * 8);
								pCurrentR[2] = (byte)(g * 8);

								pCurrentR += 3;
							}
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					for (int y = 0; y < height; y++)
					{
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x++)
						{
							if ((pSource[y * strideS + x / 8] & (0x80 >> (x & 0x07))) > 0)
							{
								pCurrentR[0] = 255;
								pCurrentR[1] = 255;
								pCurrentR[2] = 255;
								pCurrentR[3] = 255;
							}
							else
							{
								pCurrentR[0] = 0;
								pCurrentR[1] = 0;
								pCurrentR[2] = 0;
								pCurrentR[3] = 255;
							}

							pCurrentR += 4;
						}
					}
				}
				else
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion
	
		#region ResampleTo24bpp()
		private unsafe static Bitmap ResampleTo24bpp(Bitmap source, Rectangle clip)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format24bppRgb);
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int width = sourceData.Width;
				int height = sourceData.Height;

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				byte* pCurrentS, pCurrentR;

				if (source.PixelFormat == PixelFormat.Format32bppArgb || source.PixelFormat == PixelFormat.Format32bppRgb)
				{
					for (int y = 0; y < height; y++)
					{
						pCurrentS = pSource + y * strideS;
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x++)
						{
							pCurrentR[0] = pCurrentS[0];
							pCurrentR[1] = pCurrentS[1];
							pCurrentR[2] = pCurrentS[2];

							pCurrentS += 4;
							pCurrentR += 3;
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					if (Misc.IsGrayscale(source) == false)
					{
						Color[] entries = source.Palette.Entries;
						
						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								Color c = entries[pCurrentS[0]];
								
								pCurrentR[0] = c.B;
								pCurrentR[1] = c.G;
								pCurrentR[2] = c.R;

								pCurrentS++;
								pCurrentR += 3;
							}
						}
					}
					else
					{
						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								pCurrentR[0] = pCurrentS[0];
								pCurrentR[1] = pCurrentS[0];
								pCurrentR[2] = pCurrentS[0];

								pCurrentS++;
								pCurrentR += 3;
							}
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format4bppIndexed)
				{
					if (Misc.IsGrayscale(source) == false)
					{
						Color[] entries = source.Palette.Entries;
						Color c;

						for (int y = 0; y < height; y++)
						{
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								if ((x % 2) == 0)
									c = entries[pSource[y * strideS + x / 2] >> 4];
								else
									c = entries[pSource[y * strideS + x / 2] & 0xF];

								pCurrentR[0] = c.B;
								pCurrentR[1] = c.G;
								pCurrentR[2] = c.R;

								pCurrentR += 3;
							}
						}
					}
					else
					{
						byte g;
						
						for (int y = 0; y < height; y++)
						{
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								if ((x % 2) == 0)
									g = (byte)(pSource[y * strideS + x / 2] >> 4);
								else
									g = (byte)(pSource[y * strideS + x / 2] & 0xF);

								pCurrentR[0] = (byte)(g * 8);
								pCurrentR[1] = (byte)(g * 8);
								pCurrentR[2] = (byte)(g * 8);

								pCurrentR += 3;
							}
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					for (int y = 0; y < height; y++)
					{
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x++)
						{
							if ((pSource[y * strideS + x / 8] & (0x80 >> (x & 0x07))) > 0)
							{
								pCurrentR[0] = 255;
								pCurrentR[1] = 255;
								pCurrentR[2] = 255;
							}

							pCurrentR += 3;
						}
					}
				}
				else
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region ResampleTo8bppGray()
		private unsafe static Bitmap ResampleTo8bppGray(Bitmap source, Rectangle clip)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format8bppIndexed);
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int width = sourceData.Width;
				int height = sourceData.Height;

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				byte* pCurrentS, pCurrentR;

				if (source.PixelFormat == PixelFormat.Format32bppRgb || source.PixelFormat == PixelFormat.Format32bppArgb)
				{
					for (int y = 0; y < height; y++)
					{
						pCurrentS = pSource + y * strideS;
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x++)
						{
							*pCurrentR = (byte)(pCurrentS[2] * 0.299F + pCurrentS[1] * 0.587F + pCurrentS[0] * 0.114F);

							pCurrentS += 4;
							pCurrentR++;
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (int y = 0; y < height; y++)
					{
						pCurrentS = pSource + y * strideS;
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x++)
						{
							pCurrentR[x] = (byte)(pCurrentS[(x * 3)] * 0.114F + pCurrentS[(x * 3) + 1] * 0.587F + pCurrentS[(x * 3) + 2] * 0.299F);
																	
							/**pCurrentR = (byte)(pCurrentS[0] * 0.114F + pCurrentS[1] * 0.587F + pCurrentS[2] * 0.299F);

							pCurrentS += 3;
							pCurrentR++;*/
							 
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					if (Misc.IsGrayscale(source) == false)
					{
						Color[] entries = source.Palette.Entries;
						
						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								*pCurrentR = (byte)(entries[pCurrentS[0]].B * 0.299F + entries[pCurrentS[0]].G * 0.587F + entries[pCurrentS[0]].R * 0.114F);

								pCurrentS++;
								pCurrentR++;
							}
						}
					}
					else
					{
						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								*(pCurrentR ++)= *(pCurrentS++);
							}
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format4bppIndexed)
				{
					if (Misc.IsGrayscale(source) == false)
					{
						Color[] entries = source.Palette.Entries;

						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								if ((x % 2) == 0)
								{
									*pCurrentR = (byte)(entries[*pCurrentS >> 4].B * 0.299F + entries[*pCurrentS >> 4].G * 0.587F + entries[*pCurrentS >> 4].R * 0.114F);
								}
								else
								{
									*pCurrentR = (byte)(entries[*pCurrentS & 0xF].B * 0.299F + entries[*pCurrentS & 0xF].G * 0.587F + entries[*pCurrentS & 0xF].R * 0.114F);
									pCurrentS++;
								}

								pCurrentR++;
							}
						}
					}
					else
					{
						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								*pCurrentR = (byte)(*pCurrentS * 16);

								pCurrentR++;
								pCurrentS++;
							}
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					for (int y = 0; y < height; y++)
					{
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x++)
						{
							if ((pSource[y * strideS + x / 8] & (0x80 >> (x & 0x07))) > 0)
							{
								pCurrentR[0] = 255;
							}

							pCurrentR ++;
						}
					}
				}
				else
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);

				result.Palette = Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region ResampleTo8bppIndexed()
		private unsafe static Bitmap ResampleTo8bppIndexed(Bitmap source, Rectangle clip)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				ImageProcessing.ColorPalettes.PaletteBuilder paletteBuilder = new ImageProcessing.ColorPalettes.PaletteBuilder();
				Color[] palette	= null;
				byte[, ,] inversePalette = null;

				if (source.PixelFormat == PixelFormat.Format24bppRgb || source.PixelFormat == PixelFormat.Format32bppArgb || source.PixelFormat == PixelFormat.Format32bppRgb || source.PixelFormat == PixelFormat.Format4bppIndexed)
				{
					palette = paletteBuilder.GetPalette256(source);

					inversePalette = ImageProcessing.ColorPalettes.PaletteBuilder.GetInversePalette32x32x32(palette);
				}

				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format8bppIndexed);
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int width = sourceData.Width;
				int height = sourceData.Height;

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				byte* pCurrentS, pCurrentR;

				if (source.PixelFormat == PixelFormat.Format24bppRgb || source.PixelFormat == PixelFormat.Format32bppRgb || source.PixelFormat == PixelFormat.Format32bppArgb)
				{
					int bytesPerPixel = (source.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

					for (int y = 0; y < height; y++)
					{
						pCurrentS = pSource + y * strideS;
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x++)
						{
							*pCurrentR = inversePalette[pCurrentS[2] / 8, pCurrentS[1] / 8, pCurrentS[0] / 8];

							pCurrentS += bytesPerPixel;
							pCurrentR++;
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					for (int y = 0; y < height; y++)
					{
						pCurrentS = pSource + y * strideS;
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x++)
						{
							*(pCurrentR++) = *(pCurrentS++);
						}
					}

					palette = source.Palette.Entries;
				}
				else if (source.PixelFormat == PixelFormat.Format4bppIndexed)
				{
					if (Misc.IsGrayscale(source) == false)
					{
						Color[] entries = source.Palette.Entries;

						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								if ((x % 2) == 0)
								{
									pCurrentR[0] = inversePalette[entries[*pCurrentS >> 4].R / 8, entries[*pCurrentS >> 4].G / 8, entries[*pCurrentS >> 4].B / 8];
								}
								else
								{
									pCurrentR[0] = inversePalette[entries[*pCurrentS & 0xF].R / 8, entries[*pCurrentS & 0xF].G / 8, entries[*pCurrentS & 0xF].B / 8];
									pCurrentS++;
								}

								pCurrentR++;
							}
						}
					}
					else
					{
						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								*pCurrentR = (byte)(*pCurrentS * 16);

								pCurrentR++;
								pCurrentS++;
							}
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					for (int y = 0; y < height; y++)
					{
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x++)
						{
							if ((pSource[y * strideS + x / 8] & (0x80 >> (x & 0x07))) > 0)
								pCurrentR[0] = 255;

							pCurrentR++;
						}
					}
				}
				else
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);

				if(palette != null)
					result.Palette = Misc.GetColorPalette(palette);
				else
					result.Palette = Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region ResampleTo4bppGray()
		private unsafe static Bitmap ResampleTo4bppGray(Bitmap source, Rectangle clip)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format4bppIndexed);
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int width = sourceData.Width;
				int height = sourceData.Height;

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				byte* pCurrentS, pCurrentR;

				if (source.PixelFormat == PixelFormat.Format32bppArgb || source.PixelFormat == PixelFormat.Format32bppRgb)
				{
					for (int y = 0; y < height; y++)
					{
						pCurrentS = pSource + y * strideS;
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x = x + 2)
						{
							*pCurrentR = (byte)(((byte)(pCurrentS[2] * 0.299F + pCurrentS[1] * 0.587F + pCurrentS[0] * 0.114F) & 0xF0) | (((byte)(pCurrentS[6] * 0.299F + pCurrentS[5] * 0.587F + pCurrentS[4] * 0.114F) >> 4)));

							pCurrentS = pCurrentS + 8;
							pCurrentR++;
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (int y = 0; y < height; y++)
					{
						pCurrentS = pSource + y * strideS;
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x = x + 2)
						{
							*pCurrentR = (byte)(((byte)(pCurrentS[2] * 0.299F + pCurrentS[1] * 0.587F + pCurrentS[0] * 0.114F) & 0xF0) | (((byte)(pCurrentS[5] * 0.299F + pCurrentS[4] * 0.587F + pCurrentS[3] * 0.114F) >> 4)));

							pCurrentS = pCurrentS + 6;
							pCurrentR++;
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					if (Misc.IsGrayscale(source) == false)
					{
						Color[] palette = source.Palette.Entries;

						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x = x + 2)
							{
								*pCurrentR = (byte)(((byte)(palette[pCurrentS[0]].R * 0.299F + palette[pCurrentS[0]].G * 0.587F + palette[pCurrentS[0]].B * 0.114F) & 0xF0) | (((byte)(palette[pCurrentS[1]].R * 0.299F + palette[pCurrentS[1]].G * 0.587F + palette[pCurrentS[1]].B * 0.114F) >> 4)));

								pCurrentS = pCurrentS + 2;
								pCurrentR++;
							}
						}
					}
					else
					{
						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x = x + 2)
							{
								*pCurrentR = (byte)(((byte)pCurrentS[0] & 0xF0) | ((byte)(pCurrentS[1]) >> 4));

								pCurrentS = pCurrentS + 2;
								pCurrentR++;
							}
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					for (int y = 0; y < height; y++)
					{
						pCurrentR = pResult + y * strideR;

						for (int x = 0; x < width; x = x + 2)
						{
							if ((pSource[y * strideS + x / 8] & (0x80 >> (x & 0x07))) > 0)
								pCurrentR[0] = 0xF0;
							if ((pSource[y * strideS + (x + 1) / 8] & (0x80 >> ((x + 1) & 0x07))) > 0)
								pCurrentR[0] |= 0xF;

							pCurrentR++;
						}
					}
				}
				else
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);

				result.Palette = Misc.GetGrayscalePalette(PixelFormat.Format4bppIndexed);
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region ResampleTo1bpp()
		private static unsafe Bitmap ResampleTo1bpp(Bitmap source, Rectangle clip)
		{
			return BinorizationThreshold.Binorize(source);
		}
		#endregion

		#endregion


	}
}
