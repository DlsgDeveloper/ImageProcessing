using ImageProcessing.Languages;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageProcessing.BigImages
{
	public class Resampling
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public Resampling()
		{
		}
		#endregion


		//PUBLIC METHODS
		#region public methods

		#region Resample()
		public void Resample(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat,
			ImageProcessing.PixelsFormat pixelFormat)
		{			
			Resample(itDecoder, destPath, imageFormat, pixelFormat, 0, 0);
		}

		public void Resample(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat,
			ImageProcessing.PixelsFormat pixelsFormat, double brightnessDelta, double contrastDelta)
		{
			if ((pixelsFormat == PixelsFormat.FormatBlackWhite) && (imageFormat is ImageProcessing.FileFormat.Jpeg))
				throw new Exception(BIPStrings.CanTCreate1BitPixelJPEGFile_STR);

			ImageProcessing.BigImages.ItEncoder itEncoder = null;
			int width = itDecoder.Width;
			int height = itDecoder.Height;

			try
			{
				itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, pixelsFormat,
					width, height, itDecoder.DpiX, itDecoder.DpiY);

				// 8 bit indexed is special case because of global color palette
				if (pixelsFormat == PixelsFormat.Format8bppIndexed)
				{
					ResampleTo8bppIndexed(itDecoder, itEncoder);
				}
				else
				{
					itEncoder.SetPalette(itDecoder);

					int topLine = 0;
					int stripHeightMax = Misc.GetStripHeightMax(itDecoder, itEncoder);

					for (int sourceTopLine = 0; sourceTopLine < itDecoder.Height; sourceTopLine += stripHeightMax)
					{
						int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - sourceTopLine);

						Bitmap resampled = null;

						using (Bitmap strip = itDecoder.GetClip(new Rectangle(0, sourceTopLine, itDecoder.Width, stripHeight)))
						{
							resampled = ImageProcessing.Resampling.Resample(strip, pixelsFormat);
							itDecoder.ReleaseAllocatedMemory(strip);
						}

						if (itDecoder.PixelsFormat != PixelsFormat.FormatBlackWhite)
						{
							if (brightnessDelta != 0 && contrastDelta != 0)
								ImageProcessing.BrightnessContrast.Go(resampled, brightnessDelta, contrastDelta);
							else if (brightnessDelta != 0)
								ImageProcessing.Brightness.Go(resampled, brightnessDelta);
							else if (contrastDelta != 0)
								ImageProcessing.Contrast.Go(resampled, contrastDelta);
						}

						unsafe
						{
							int resizeHeight = (int)Math.Min(resampled.Height, itEncoder.Height - topLine);
							BitmapData bitmapData = null;

							try
							{
								bitmapData = resampled.LockBits(new Rectangle(0, 0, resampled.Width, resampled.Height), ImageLockMode.ReadWrite, resampled.PixelFormat);
								itEncoder.Write(resizeHeight, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
							}
							finally
							{
								if (bitmapData != null)
									resampled.UnlockBits(bitmapData);
							}

							topLine += resizeHeight;
						}

						resampled.Dispose();
						resampled = null;

						if (ProgressChanged != null)
							ProgressChanged((sourceTopLine + stripHeight) / (float)itDecoder.Height);
					}
				}
			}
			catch (Exception ex)
			{
				try { if (itEncoder != null) itEncoder.Dispose(); }
				catch { }
				finally { itEncoder = null; }

				try { if (File.Exists(destPath)) File.Delete(destPath); }
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

		#region ResampleTo32bpp()
		private unsafe void ResampleTo32bpp(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder)
		{
			Bitmap source = null;
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			int		stripHeightMax = Misc.GetStripHeightMax(itDecoder, itEncoder);

			for (int stripY = 0; stripY < itDecoder.Height; stripY = stripY + stripHeightMax)
			{
				try
				{
					int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - stripY);
					source = itDecoder.GetClip(new Rectangle(0, stripY, itDecoder.Width, stripHeight));
					result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppRgb);

					sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
					resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					int width = sourceData.Width;
					int height = sourceData.Height;

					int strideS = sourceData.Stride;
					int strideR = resultData.Stride;

					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentR;

					if (itDecoder.PixelFormat == PixelFormat.Format24bppRgb)
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
					else if (itDecoder.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						ColorPalette palette = itDecoder.GetPalette();

						if (palette != null && palette.Entries.Length > 0)
						{
							for (int y = 0; y < height; y++)
							{
								pCurrentS = pSource + y * strideS;
								pCurrentR = pResult + y * strideR;

								for (int x = 0; x < width; x++)
								{
									pCurrentR[0] = palette.Entries[pCurrentS[0]].B;
									pCurrentR[1] = palette.Entries[pCurrentS[0]].G;
									pCurrentR[2] = palette.Entries[pCurrentS[0]].R;
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
					else if (itDecoder.PixelFormat == PixelFormat.Format4bppIndexed)
					{
						ColorPalette palette = itDecoder.GetPalette();

						if (palette != null && palette.Entries.Length > 0)
						{
							Color[] entries = palette.Entries;
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
								pCurrentS = pSource + y * strideS;
								pCurrentR = pResult + y * strideR;

								for (int x = 0; x < width; x++)
								{
									if ((x % 2) == 0)
										g = (byte)(pSource[y * strideS + x / 2] >> 4);
									else
										g = (byte)(pSource[y * strideS + x / 2] & 0xF);

									pCurrentR[0] = (byte)(g * 17);
									pCurrentR[1] = (byte)(g * 17);
									pCurrentR[2] = (byte)(g * 17);
									pCurrentR[3] = 255;

									pCurrentR += 4;
								}
							}
						}
					}
					else if (itDecoder.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						for (int y = 0; y < height; y++)
						{
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								if ((pSource[y * strideS + x / 8] & (0x80 >> (x & 0x07))) > 0)
								{
									pCurrentR[0] = 0xFF;
									pCurrentR[1] = 0xFF;
									pCurrentR[2] = 0xFF;
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

					itEncoder.Write(stripHeight, strideR, pResult);
					if (ProgressChanged != null)
						ProgressChanged((stripY + stripHeight) / (float)itDecoder.Height);
				}
				finally
				{
					if (sourceData != null)
					{
						source.UnlockBits(sourceData);
						sourceData = null;
					}
					if (resultData != null)
					{
						result.UnlockBits(resultData);
						resultData = null;
					}
					if (source != null)
					{
						source.Dispose();
						source = null;
					}
					itDecoder.ReleaseAllocatedMemory(source);
					if (result != null)
					{
						result.Dispose();
						result = null;
					}
				}
			}
		}
		#endregion

		#region ResampleTo24bpp()
		private unsafe void ResampleTo24bpp(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder)
		{
			Bitmap source = null;
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			int stripHeightMax = Misc.GetStripHeightMax(itDecoder, itEncoder);

			for (int stripY = 0; stripY < itDecoder.Height; stripY = stripY + stripHeightMax)
			{
				try
				{
					int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - stripY);
					source = itDecoder.GetClip(new Rectangle(0, stripY, itDecoder.Width, stripHeight));
					result = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);

					sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
					resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					int width = sourceData.Width;
					int height = sourceData.Height;

					int strideS = sourceData.Stride;
					int strideR = resultData.Stride;

					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentR;

					if (itDecoder.PixelsFormat == PixelsFormat.Format32bppRgb)
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
					else if (itDecoder.PixelsFormat == PixelsFormat.Format8bppIndexed)
					{
						ColorPalette palette = itDecoder.GetPalette();

						if (palette != null && palette.Entries.Length > 0)
						{		
							for (int y = 0; y < height; y++)
							{
								pCurrentS = pSource + y * strideS;
								pCurrentR = pResult + y * strideR;

								for (int x = 0; x < width; x++)
								{
									pCurrentR[0] = palette.Entries[pCurrentS[0]].B;
									pCurrentR[1] = palette.Entries[pCurrentS[0]].G;
									pCurrentR[2] = palette.Entries[pCurrentS[0]].R;

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
					else if (itDecoder.PixelsFormat == PixelsFormat.Format8bppGray)
					{
						byte gray;
						
						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = width - 1; x >= 0; x--)
							{
								gray = pCurrentS[x];
								
								pCurrentR[x * 3] = gray;
								pCurrentR[x * 3 + 1] = gray;
								pCurrentR[x * 3 + 2] = gray;
							}
						}
					}
					else if (itDecoder.PixelsFormat == PixelsFormat.Format4bppGray)
					{
						ColorPalette palette = itDecoder.GetPalette();

						if (palette != null && palette.Entries.Length > 0)
						{
							Color[] entries = palette.Entries;
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
								pCurrentS = pSource + y * strideS;
								pCurrentR = pResult + y * strideR;

								for (int x = 0; x < width; x++)
								{
									if ((x % 2) == 0)
										g = (byte)(pSource[y * strideS + x / 2] >> 4);
									else
										g = (byte)(pSource[y * strideS + x / 2] & 0xF);

									pCurrentR[0] = (byte)(g * 17);
									pCurrentR[1] = (byte)(g * 17);
									pCurrentR[2] = (byte)(g * 17);

									pCurrentR += 3;
								}
							}
						}
					}
					else if (itDecoder.PixelsFormat == PixelsFormat.FormatBlackWhite)
					{
						for (int y = 0; y < height; y++)
						{
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								if ((pSource[y * strideS + x / 8] & (0x80 >> (x & 0x07))) > 0)
								{
									pCurrentR[0] = 0xFF;
									pCurrentR[1] = 0xFF;
									pCurrentR[2] = 0xFF;
								}

								pCurrentR += 3;
							}
						}
					}
					else
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);

					itEncoder.Write(stripHeight, strideR, pResult);
					if (ProgressChanged != null)
						ProgressChanged((stripY + stripHeight) / (float)itDecoder.Height);
				}
				finally
				{
					if (sourceData != null)
					{
						source.UnlockBits(sourceData);
						sourceData = null;
					}
					if (resultData != null)
					{
						result.UnlockBits(resultData);
						resultData = null;
					}
					if (source != null)
					{
						source.Dispose();
						source = null;
					}
					itDecoder.ReleaseAllocatedMemory(source);
					if (result != null)
					{
						result.Dispose();
						result = null;
					}
				}
			}
		}
		#endregion

		#region ResampleTo8bppIndexed()
		private unsafe void ResampleTo8bppIndexed(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder)
		{
			Bitmap source = null;
			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;
			byte[, ,] inversePalette = null;

			if (itDecoder.PixelsFormat == PixelsFormat.Format24bppRgb || itDecoder.PixelsFormat == PixelsFormat.Format32bppRgb || itDecoder.PixelsFormat == PixelsFormat.Format4bppGray)
			{
				ImageProcessing.ColorPalettes.PaletteBuilder paletteBuilder = new ImageProcessing.ColorPalettes.PaletteBuilder();

				Color[] palette = paletteBuilder.GetPalette256(itDecoder);

				inversePalette = ImageProcessing.ColorPalettes.PaletteBuilder.GetInversePalette32x32x32(palette);
				itEncoder.SetPalette(System.Drawing.Imaging.PixelFormat.Format8bppIndexed, palette);
			}
			else if (itDecoder.PixelsFormat == PixelsFormat.Format8bppIndexed || itDecoder.PixelsFormat == PixelsFormat.Format8bppGray)
			{
				itEncoder.SetPalette(itDecoder);
			}

			int stripHeightMax = Misc.GetStripHeightMax(itDecoder, itEncoder);

			for (int stripY = 0; stripY < itDecoder.Height; stripY = stripY + stripHeightMax)
			{
				try
				{
					int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - stripY);
					source = itDecoder.GetClip(new Rectangle(0, stripY, itDecoder.Width, stripHeight));
					result = new Bitmap(source.Width, source.Height, PixelFormat.Format8bppIndexed);

					sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
					resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					int width = sourceData.Width;
					int height = sourceData.Height;

					int strideS = sourceData.Stride;
					int strideR = resultData.Stride;

					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentR;

					if (itDecoder.PixelsFormat == PixelsFormat.Format24bppRgb || itDecoder.PixelsFormat == PixelsFormat.Format32bppRgb)
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
					else if (itDecoder.PixelsFormat == PixelsFormat.Format8bppIndexed || itDecoder.PixelsFormat == PixelsFormat.Format8bppGray)
					{
						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								*pCurrentR = *pCurrentS;

								pCurrentS++;
								pCurrentR++;
							}
						}
					}
					else if (itDecoder.PixelsFormat == PixelsFormat.Format4bppGray)
					{
						if (ImageProcessing.Misc.IsGrayscale(source) == false)
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
					else if (itDecoder.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						for (int y = 0; y < height; y++)
						{
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								if ((pSource[y * strideS + x / 8] & (0x80 >> (x & 0x07))) > 0)
									pCurrentR[0] = 0xFF;

								pCurrentR ++;
							}
						}
					}
					else
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);

					itEncoder.Write(stripHeight, strideR, pResult);
					if (ProgressChanged != null)
						ProgressChanged(((stripY + stripHeight) / (float)itDecoder.Height) / 2.0F + 0.5F);
				}
				finally
				{
					if (sourceData != null)
					{
						source.UnlockBits(sourceData);
						sourceData = null;
					}
					if (resultData != null)
					{
						result.UnlockBits(resultData);
						resultData = null;
					}
					if (source != null)
					{
						source.Dispose();
						source = null;
					}
					itDecoder.ReleaseAllocatedMemory(source);
					if (result != null)
					{
						result.Dispose();
						result = null;
					}
				}
			}
		}
		#endregion
	
		#region ResampleTo8bppGray()
		private unsafe void ResampleTo8bppGray(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder)
		{
			Bitmap source = null;
			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int stripHeightMax = Misc.GetStripHeightMax(itDecoder, itEncoder);

			for (int stripY = 0; stripY < itDecoder.Height; stripY = stripY + stripHeightMax)
			{
				try
				{
					int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - stripY);
					source = itDecoder.GetClip(new Rectangle(0, stripY, itDecoder.Width, stripHeight));
					result = new Bitmap(source.Width, source.Height, PixelFormat.Format8bppIndexed);

					sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
					resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					int width = sourceData.Width;
					int height = sourceData.Height;

					int strideS = sourceData.Stride;
					int strideR = resultData.Stride;

					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentR;

					if (source.PixelFormat == PixelFormat.Format24bppRgb || itDecoder.PixelFormat == PixelFormat.Format32bppArgb || itDecoder.PixelFormat == PixelFormat.Format32bppRgb)
					{
						int bytesPerPixel = (source.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
						
						for (int y = 0; y < height; y++)
						{
							pCurrentS = pSource + y * strideS;
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								*pCurrentR = (byte)(pCurrentS[2] * 0.299F + pCurrentS[1] * 0.587F + pCurrentS[0] * 0.114F);

								pCurrentS += bytesPerPixel;
								pCurrentR++;
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						if (ImageProcessing.Misc.IsGrayscale(source))
						{
							for (int y = 0; y < height; y++)
							{
								pCurrentS = pSource + y * strideS;
								pCurrentR = pResult + y * strideR;

								for (int x = 0; x < width; x++)
								{
									*pCurrentR = *pCurrentS;

									pCurrentS++;
									pCurrentR++;
								}
							}
						}
						else
						{
							Color[] entries = source.Palette.Entries;

							for (int y = 0; y < height; y++)
							{
								pCurrentS = pSource + y * strideS;
								pCurrentR = pResult + y * strideR;

								for (int x = 0; x < width; x++)
								{
									*pCurrentR = (byte)(entries[*pCurrentS].B * 0.299F + entries[*pCurrentS].G * 0.587F + entries[*pCurrentS].R * 0.114F);

									pCurrentS++;
									pCurrentR++;
								}
							}
						}
					}
					else if (itDecoder.PixelFormat == PixelFormat.Format4bppIndexed)
					{
						ColorPalette palette = itDecoder.GetPalette();

						if (palette != null && palette.Entries.Length > 0)
						{
							Color[] entries = palette.Entries;
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

									*pCurrentR = (byte)(c.R * 0.299F + c.G * 0.587F + c.B * 0.114F);

									pCurrentR++;
								}
							}
						}
						else
						{
							byte g;

							for (int y = 0; y < height; y++)
							{
								pCurrentS = pSource + y * strideS;
								pCurrentR = pResult + y * strideR;

								for (int x = 0; x < width; x++)
								{
									if ((x % 2) == 0)
										g = (byte)(pSource[y * strideS + x / 2] >> 4);
									else
										g = (byte)(pSource[y * strideS + x / 2] & 0xF);

									pCurrentR[0] = (byte)(g * 17);

									pCurrentR++;
								}
							}
						}
					}
					else if (itDecoder.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						for (int y = 0; y < height; y++)
						{
							pCurrentR = pResult + y * strideR;

							for (int x = 0; x < width; x++)
							{
								if ((pSource[y * strideS + x / 8] & (0x80 >> (x & 0x07))) > 0)
									pCurrentR[0] = 0xFF;

								pCurrentR ++;
							}
						}
					}
					else
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);

					itEncoder.Write(stripHeight, strideR, pResult);
					if (ProgressChanged != null)
						ProgressChanged((stripY + stripHeight) / (float)itDecoder.Height);
				}
				finally
				{
					if (sourceData != null)
					{
						source.UnlockBits(sourceData);
						sourceData = null;
					}
					if (resultData != null)
					{
						result.UnlockBits(resultData);
						resultData = null;
					}
					if (source != null)
					{
						source.Dispose();
						source = null;
					}
					itDecoder.ReleaseAllocatedMemory(source);
					if (result != null)
					{
						result.Dispose();
						result = null;
					}
				}
			}
		}
		#endregion

		#region ResampleTo4bppGray()
		private unsafe void ResampleTo4bppGray(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder)
		{
			Bitmap source = null;
			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int stripHeightMax = Misc.GetStripHeightMax(itDecoder, itEncoder);

			for (int stripY = 0; stripY < itDecoder.Height; stripY = stripY + stripHeightMax)
			{
				try
				{
					int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - stripY);
					source = itDecoder.GetClip(new Rectangle(0, stripY, itDecoder.Width, stripHeight));
					result = new Bitmap(source.Width, source.Height, PixelFormat.Format4bppIndexed);

					sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
					resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					int width = sourceData.Width;
					int height = sourceData.Height;

					int strideS = sourceData.Stride;
					int strideR = resultData.Stride;

					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentR;

					if (itDecoder.PixelFormat == PixelFormat.Format32bppArgb || itDecoder.PixelFormat == PixelFormat.Format32bppRgb)
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
						if (ImageProcessing.Misc.IsGrayscale(source) == false)
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
					else if (itDecoder.PixelFormat == PixelFormat.Format1bppIndexed)
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

					itEncoder.Write(stripHeight, strideR, pResult);
					if (ProgressChanged != null)
						ProgressChanged((stripY + stripHeight) / (float)itDecoder.Height);
				}
				finally
				{
					if (sourceData != null)
					{
						source.UnlockBits(sourceData);
						sourceData = null;
					}
					if (resultData != null)
					{
						result.UnlockBits(resultData);
						resultData = null;
					}
					if (source != null)
					{
						source.Dispose();
						source = null;
					}
					itDecoder.ReleaseAllocatedMemory(source);
					if (result != null)
					{
						result.Dispose();
						result = null;
					}
				}
			}
		}
		#endregion
	
		#region ResampleTo1bpp()
		private void ResampleTo1bpp(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder)
		{
			ImageProcessing.BigImages.Binarization binorization = new ImageProcessing.BigImages.Binarization();

			binorization.ProgressChanged += delegate(float progress)
			{
				if (this.ProgressChanged != null)
					this.ProgressChanged(progress);
			};
			Bitmap result = binorization.ThresholdToBitmap(itDecoder);
			BitmapData	resultData = null;

			try
			{
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				unsafe
				{
					itEncoder.Write(resultData.Height, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
				}
			}
			finally
			{
				if (resultData != null)
					result.UnlockBits(resultData);

				if (result != null)
					result.Dispose();
			}
		}
		#endregion

		#endregion


	}
}
