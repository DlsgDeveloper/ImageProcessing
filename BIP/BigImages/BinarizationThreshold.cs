using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Diagnostics;
using System.IO;

namespace ImageProcessing.BigImages
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public partial class Binarization
	{
		//	PUBLIC METHODS
		#region public methods

		#region Threshold()
		public void Threshold(string file, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			Threshold(file, destFile, imageFormat, new BinarizationParameters());
		}

		public void Threshold(string file, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat, BinarizationParameters parameters)
		{
			Threshold(file, destFile, imageFormat, Rectangle.Empty, parameters);
		}

		public void Threshold(string file, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat, Rectangle clip, BinarizationParameters parameters)
		{
			using (ImageProcessing.BigImages.ItDecoder itDecoder = new ItDecoder(file))
			{
				Threshold(itDecoder, destFile, imageFormat, clip, parameters);
			}
		}

		public void Threshold(ImageProcessing.BigImages.ItDecoder itDecoder, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			Threshold(itDecoder, destFile, imageFormat, new BinarizationParameters());
		}

		public void Threshold(ImageProcessing.BigImages.ItDecoder itDecoder, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat, BinarizationParameters parameters)
		{
			Threshold(itDecoder, destFile, imageFormat, Rectangle.Empty, parameters);
		}

		public void Threshold(ImageProcessing.BigImages.ItDecoder itDecoder, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat, Rectangle clip, BinarizationParameters parameters)
		{
			using (Bitmap bitonalBitmap = ThresholdToBitmap(itDecoder, clip, parameters))
			{
				SaveBitonalBitmapToFile(bitonalBitmap, destFile, imageFormat);
			}
		}
		#endregion

		#region ThresholdToBitmap()
		public Bitmap ThresholdToBitmap(ImageProcessing.BigImages.ItDecoder itDecoder)
		{
			return ThresholdToBitmap(itDecoder, new BinarizationParameters());
		}

		public Bitmap ThresholdToBitmap(ImageProcessing.BigImages.ItDecoder itDecoder, BinarizationParameters parameters)
		{
			return ThresholdToBitmap(itDecoder, Rectangle.Empty, parameters);
		}

		public Bitmap ThresholdToBitmap(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip, BinarizationParameters parameters)	//byte thresR, byte thresG, byte thresB, double contrast, ColorD histogramMean)
		{
			if (clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, itDecoder.Width, itDecoder.Height);
			else
				clip.Intersect(Rectangle.FromLTRB(0, 0, itDecoder.Width, itDecoder.Height));

			byte thresR, thresG, thresB;

			//get threshold if necessary
			if (parameters.ThresholdR == null || parameters.ThresholdG == null || parameters.ThresholdB == null)
			{
				switch (itDecoder.PixelFormat)
				{
					case PixelFormat.Format4bppIndexed:
						{
							thresR = thresG = thresB = (byte)(127 + parameters.ThresholdDelta);
						} break;
					case PixelFormat.Format8bppIndexed:
						{
							ImageProcessing.Histogram histogram = new ImageProcessing.Histogram(itDecoder);
							thresR = thresG = thresB = (byte)Math.Max(1, Math.Min(254, histogram.Threshold.R + parameters.ThresholdDelta));
						} break;
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
						{
							ImageProcessing.Histogram histogram = new ImageProcessing.Histogram(itDecoder);

							thresR = (byte)Math.Max(1, Math.Min(254, (int)(histogram.ThresholdR + parameters.ThresholdDelta)));
							thresG = (byte)Math.Max(1, Math.Min(254, histogram.ThresholdG + parameters.ThresholdDelta));
							thresB = (byte)Math.Max(1, Math.Min(254, histogram.ThresholdB + parameters.ThresholdDelta));
						} break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			else
			{
				thresR = parameters.ThresholdR.Value;
				thresG = parameters.ThresholdG.Value;
				thresB = parameters.ThresholdB.Value;
			} 
			
			try
			{
				switch (itDecoder.PixelFormat)
				{
					case PixelFormat.Format4bppIndexed:
					case PixelFormat.Format8bppIndexed:
					case PixelFormat.Format24bppRgb :
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
						return BinarizeInternal(itDecoder, clip, thresR, thresG, thresB, parameters.Contrast, parameters.HistogramMean);
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}
			}
			catch(Exception ex)
			{
				throw new Exception("BinorizationThreshold, ThresholdToBitmap(): " + ex.Message);
			}
		}

		public Bitmap ThresholdToBitmap(Bitmap source, Rectangle clip, BinarizationParameters parameters)
		{
			if (clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, source.Width, source.Height);
			else
				clip.Intersect(Rectangle.FromLTRB(0, 0, source.Width, source.Height));

			byte thresR, thresG, thresB;

			//get threshold if necessary
			if (parameters.ThresholdR == null || parameters.ThresholdG == null || parameters.ThresholdB == null)
			{
				switch (source.PixelFormat)
				{
					case PixelFormat.Format4bppIndexed:
						{
							thresR = thresG = thresB = (byte)(127 + parameters.ThresholdDelta);
						} break;
					case PixelFormat.Format8bppIndexed:
						{
							ImageProcessing.Histogram histogram = new ImageProcessing.Histogram(source);
							thresR = thresG = thresB = (byte)Math.Max(1, Math.Min(254, histogram.Threshold.R + parameters.ThresholdDelta));
						} break;
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
						{
							ImageProcessing.Histogram histogram = new ImageProcessing.Histogram(source);

							thresR = (byte)Math.Max(1, Math.Min(254, histogram.ThresholdR + parameters.ThresholdDelta));
							thresG = (byte)Math.Max(1, Math.Min(254, histogram.ThresholdG + parameters.ThresholdDelta));
							thresB = (byte)Math.Max(1, Math.Min(254, histogram.ThresholdB + parameters.ThresholdDelta));
						} break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			else
			{
				thresR = parameters.ThresholdR.Value;
				thresG = parameters.ThresholdG.Value;
				thresB = parameters.ThresholdB.Value;
			}

			try
			{
				switch (source.PixelFormat)
				{
					case PixelFormat.Format4bppIndexed:
					case PixelFormat.Format8bppIndexed:
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
						return BinarizeInternal(source, clip, thresR, thresG, thresB, parameters.Contrast, parameters.HistogramMean);
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("BinorizationThreshold, ThresholdToBitmap(): " + ex.Message);
			}
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region BinarizeInternal()
		private Bitmap BinarizeInternal(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip, byte thresR, byte thresG, byte thresB, double contrast, ColorD histogramMean)
		{
			Bitmap result = null;
			BitmapData resultData = null;

			try
			{
				int stripHeightMax = Misc.GetStripHeightMax(itDecoder);
				
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);

				for (int stripY = clip.Top; stripY < clip.Bottom; stripY += stripHeightMax)
				{
					Bitmap source = null;
					BitmapData sourceData = null;

					int stripH = Math.Min(stripHeightMax, clip.Bottom - stripY);
					
					try
					{
						source = itDecoder.GetClip(new Rectangle(clip.X, stripY, clip.Width, stripH));

						sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
						resultData = result.LockBits(new Rectangle(0, stripY - clip.Top, result.Width, stripH), ImageLockMode.WriteOnly, result.PixelFormat);

						int sourceStride = sourceData.Stride;
						int resultStride = resultData.Stride;
						

						int width = sourceData.Width;
						int height = sourceData.Height;
						int x, y;

						unsafe
						{
							byte* pOrig = (byte*)sourceData.Scan0.ToPointer();
							byte* pCopy = (byte*)resultData.Scan0.ToPointer();
							byte* pCurrent;

							if (itDecoder.PixelsFormat == PixelsFormat.Format32bppRgb)
							{
								int threshold = thresR + thresG + thresB;
								
								if (contrast == 0)
								{
									for (y = 0; y < height; y++)
									{
										pCurrent = pOrig + (y * sourceStride);

										for (x = 0; x < width; x++)
										{
											if ((pCurrent[x * 4] + pCurrent[x * 4 + 1] + pCurrent[x * 4 + 2]) > threshold)
												pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
										}
									}
								}
								else
								{
									double red, green, blue;
									double meanR = histogramMean.Red;
									double meanG = histogramMean.Green;
									double meanB = histogramMean.Blue;

									contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
									contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

									for (y = 0; y < height; y++)
									{
										pCurrent = pOrig + (y * sourceStride);

										for (x = 0; x < width; x++)
										{
											//blue
											blue = ((pCurrent[x * 4] - meanB) * contrast) + meanB;

											if (blue <= 0)
												blue = 0;
											else if (blue >= 255)
												blue = 255;

											//green
											green = ((pCurrent[x * 4 + 1] - meanG) * contrast) + meanG;

											if (green <= 0)
												green = 0;
											else if (green >= 255)
												green = 255;

											//red
											red = ((pCurrent[x * 4 + 2] - meanR) * contrast) + meanR;

											if (red <= 0)
												red = 0;
											else if (red >= 255)
												red = 255;

											if ((red + green + blue) > threshold)
												pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
										}
									}
								}
							}
							else if (itDecoder.PixelsFormat == PixelsFormat.Format24bppRgb)
							{
								int threshold = thresR + thresG + thresB;
								
								if (contrast == 0)
								{
									for (y = 0; y < height; y++)
									{
										pCurrent = pOrig + (y * sourceStride);

										for (x = 0; x < width; x++)
										{
											if ((pCurrent[x * 3] + pCurrent[x * 3 + 1] + pCurrent[x * 3 + 2]) > threshold)
												pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
										}
									}
								}
								else
								{
									double red, green, blue;
									double meanR = histogramMean.Red;
									double meanG = histogramMean.Green;
									double meanB = histogramMean.Blue;

									contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
									contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

									for (y = 0; y < height; y++)
									{
										pCurrent = pOrig + (y * sourceStride);

										for (x = 0; x < width; x++)
										{
											//blue
											blue = ((pCurrent[x * 3] - meanB) * contrast) + meanB;

											if (blue <= 0)
												blue = 0;
											else if (blue >= 255)
												blue = 255;

											//green
											green = ((pCurrent[x * 3 + 1] - meanG) * contrast) + meanG;

											if (green <= 0)
												green = 0;
											else if (green >= 255)
												green = 255;

											//red
											red = ((pCurrent[x * 3 + 2] - meanR) * contrast) + meanR;

											if (red <= 0)
												red = 0;
											else if (red >= 255)
												red = 255;

											if ((red + green + blue) > threshold)
												pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
										}
									}
								}
							}
							else if (itDecoder.PixelsFormat == PixelsFormat.Format8bppIndexed)
							{
								int threshold = thresR + thresG + thresB;
								Color[] palette = source.Palette.Entries;
								
								if (contrast == 0)
								{
									for (y = 0; y < height; y++)
									{
										pCurrent = pOrig + (y * sourceStride);

										for (x = 0; x < width; x++)
										{
											if ((palette[pCurrent[x]].R + palette[pCurrent[x]].G + palette[pCurrent[x]].B) > threshold)
												pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
										}
									}
								}
								else
								{
									double mean = histogramMean.Red;
									double gray;

									contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
									contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

									for (y = 0; y < height; y++)
									{
										pCurrent = pOrig + (y * sourceStride);

										for (x = 0; x < width; x++)
										{
											//gray
											gray = (palette[pCurrent[x]].R + palette[pCurrent[x]].G + palette[pCurrent[x]].B) / 3;

											if ((((gray - mean) * contrast) + mean) > threshold)
												pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
										}
									}
								}
							}
							else if (itDecoder.PixelsFormat == PixelsFormat.Format8bppGray)
							{
								int threshold = thresR;

								if (contrast == 0)
								{
									for (y = 0; y < height; y++)
									{
										pCurrent = pOrig + (y * sourceStride);

										for (x = 0; x < width; x++)
										{
											if (pCurrent[x] > threshold)
												pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
										}
									}
								}
								else
								{
									double mean = histogramMean.Red;

									contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
									contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

									for (y = 0; y < height; y++)
									{
										pCurrent = pOrig + (y * sourceStride);

										for (x = 0; x < width; x++)
										{
											//gray
											if ((((pCurrent[x] - mean) * contrast) + mean) > threshold)
												pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
										}
									}
								}
							}
							else if (itDecoder.PixelsFormat == PixelsFormat.Format4bppGray)
							{
								int threshold = thresR;
								byte[] palette = new byte[16];

								for (int i = 0; i < 16; i++)
									palette[i] = (byte)(source.Palette.Entries[i].R * 0.299F + source.Palette.Entries[i].G * 0.587F + source.Palette.Entries[i].B * 0.114F);

								if (contrast == 0)
								{
									for (y = 0; y < height; y++)
									{
										pCurrent = pOrig + y * sourceStride;

										for (x = 0; x < width; x = x + 2)
										{
											if ((palette[*pCurrent >> 4]) > threshold)
												pCopy[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 7));
											if ((palette[*pCurrent & 0x0F]) > threshold)
												pCopy[y * resultStride + (x + 1) / 8] |= (byte)(0x80 >> ((x + 1) & 0x7));

											pCurrent++;
										}
									}
								}
								else
								{
									double mean = histogramMean.Red;

									contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
									contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

									for (y = 0; y < height; y++)
									{
										pCurrent = pOrig + (y * sourceStride);

										for (x = 0; x < width; x = x + 2)
										{
											//gray
											if (((((palette[*pCurrent >> 4]) - mean) * contrast) + mean) > threshold)
												pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));

											//gray
											if (((((palette[*pCurrent & 0x0F]) - mean) * contrast) + mean) > threshold)
												pCopy[y * resultStride + ((x + 1) >> 3)] |= (byte)(0x80 >> ((x + 1) & 0x7));

											pCurrent++;
										}
									}
								}
							}
						}
					}
					finally
					{
						if (resultData != null)
						{
							result.UnlockBits(resultData);
							resultData = null;
						}
						if (source != null && sourceData != null)
						{
							source.UnlockBits(sourceData);
							sourceData = null;
						}
						
						itDecoder.ReleaseAllocatedMemory(source);
						
						if (source != null)
						{
							source.Dispose();
							source = null;
						}
					}

					FireProgressEvent((stripY + stripH) / (float)itDecoder.Height);
				}
				
				if (result != null)
					ImageProcessing.Misc.SetBitmapResolution(result, itDecoder.DpiX, itDecoder.DpiY);
			
				return result;
			}
			catch (Exception ex)
			{
				if (result != null)
				{
					result.Dispose();
					result = null;
				}

				throw ex;
			}
			finally
			{
			}
		}
		#endregion

		#region BinarizeInternal()
		private Bitmap BinarizeInternal(Bitmap source, Rectangle clip, byte thresR, byte thresG, byte thresB, double contrast, ColorD histogramMean)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);
				int width = result.Width;
				int height = result.Height;

				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sourceStride = sourceData.Stride;
				int resultStride = resultData.Stride;


				int x, y;

				unsafe
				{
					byte* pOrig = (byte*)sourceData.Scan0.ToPointer();
					byte* pCopy = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrent;

					if (source.PixelFormat == PixelFormat.Format32bppRgb || source.PixelFormat == PixelFormat.Format32bppArgb)
					{
						int threshold = thresR + thresG + thresB;

						if (contrast == 0)
						{
							for (y = 0; y < height; y++)
							{
								pCurrent = pOrig + (y * sourceStride);

								for (x = 0; x < width; x++)
								{
									if ((pCurrent[x * 4] + pCurrent[x * 4 + 1] + pCurrent[x * 4 + 2]) > threshold)
										pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
								}
							}
						}
						else
						{
							double red, green, blue;
							double meanR = histogramMean.Red;
							double meanG = histogramMean.Green;
							double meanB = histogramMean.Blue;

							contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
							contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

							for (y = 0; y < height; y++)
							{
								pCurrent = pOrig + (y * sourceStride);

								for (x = 0; x < width; x++)
								{
									//blue
									blue = ((pCurrent[x * 4] - meanB) * contrast) + meanB;

									if (blue <= 0)
										blue = 0;
									else if (blue >= 255)
										blue = 255;

									//green
									green = ((pCurrent[x * 4 + 1] - meanG) * contrast) + meanG;

									if (green <= 0)
										green = 0;
									else if (green >= 255)
										green = 255;

									//red
									red = ((pCurrent[x * 4 + 2] - meanR) * contrast) + meanR;

									if (red <= 0)
										red = 0;
									else if (red >= 255)
										red = 255;

									if ((red + green + blue) > threshold)
										pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
								}
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						int threshold = thresR + thresG + thresB;

						if (contrast == 0)
						{
							for (y = 0; y < height; y++)
							{
								pCurrent = pOrig + (y * sourceStride);

								for (x = 0; x < width; x++)
								{
									if ((pCurrent[x * 3] + pCurrent[x * 3 + 1] + pCurrent[x * 3 + 2]) > threshold)
										pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
								}
							}
						}
						else
						{
							double red, green, blue;
							double meanR = histogramMean.Red;
							double meanG = histogramMean.Green;
							double meanB = histogramMean.Blue;

							contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
							contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

							for (y = 0; y < height; y++)
							{
								pCurrent = pOrig + (y * sourceStride);

								for (x = 0; x < width; x++)
								{
									//blue
									blue = ((pCurrent[x * 3] - meanB) * contrast) + meanB;

									if (blue <= 0)
										blue = 0;
									else if (blue >= 255)
										blue = 255;

									//green
									green = ((pCurrent[x * 3 + 1] - meanG) * contrast) + meanG;

									if (green <= 0)
										green = 0;
									else if (green >= 255)
										green = 255;

									//red
									red = ((pCurrent[x * 3 + 2] - meanR) * contrast) + meanR;

									if (red <= 0)
										red = 0;
									else if (red >= 255)
										red = 255;

									if ((red + green + blue) > threshold)
										pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
								}
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed && ImageProcessing.Misc.IsGrayscale(source) == false)
					{
						int threshold = thresR + thresG + thresB;
						Color[] palette = source.Palette.Entries;

						if (contrast == 0)
						{
							for (y = 0; y < height; y++)
							{
								pCurrent = pOrig + (y * sourceStride);

								for (x = 0; x < width; x++)
								{
									if ((palette[pCurrent[x]].R + palette[pCurrent[x]].G + palette[pCurrent[x]].B) > threshold)
										pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
								}
							}
						}
						else
						{
							double mean = histogramMean.Red;
							double gray;

							contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
							contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

							for (y = 0; y < height; y++)
							{
								pCurrent = pOrig + (y * sourceStride);

								for (x = 0; x < width; x++)
								{
									//gray
									gray = (palette[pCurrent[x]].R + palette[pCurrent[x]].G + palette[pCurrent[x]].B) / 3;

									if ((((gray - mean) * contrast) + mean) > threshold)
										pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
								}
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						int threshold = thresR;

						if (contrast == 0)
						{
							for (y = 0; y < height; y++)
							{
								pCurrent = pOrig + (y * sourceStride);

								for (x = 0; x < width; x++)
								{
									if (pCurrent[x] > threshold)
										pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
								}
							}
						}
						else
						{
							double mean = histogramMean.Red;

							contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
							contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

							for (y = 0; y < height; y++)
							{
								pCurrent = pOrig + (y * sourceStride);

								for (x = 0; x < width; x++)
								{
									//gray
									if ((((pCurrent[x] - mean) * contrast) + mean) > threshold)
										pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
								}
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format4bppIndexed)
					{
						int threshold = thresR;
						byte[] palette = new byte[16];

						for (int i = 0; i < 16; i++)
							palette[i] = (byte)(source.Palette.Entries[i].R * 0.299F + source.Palette.Entries[i].G * 0.587F + source.Palette.Entries[i].B * 0.114F);

						if (contrast == 0)
						{
							for (y = 0; y < height; y++)
							{
								pCurrent = pOrig + y * sourceStride;

								for (x = 0; x < width; x = x + 2)
								{
									if ((palette[*pCurrent >> 4]) > threshold)
										pCopy[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 7));
									if ((palette[*pCurrent & 0x0F]) > threshold)
										pCopy[y * resultStride + (x + 1) / 8] |= (byte)(0x80 >> ((x + 1) & 0x7));

									pCurrent++;
								}
							}
						}
						else
						{
							double mean = histogramMean.Red;

							contrast = ((contrast > 1) ? 1 : ((contrast < -1) ? -1 : contrast));
							contrast = Math.Tan((contrast + 1.0) * Math.PI / 4);

							for (y = 0; y < height; y++)
							{
								pCurrent = pOrig + (y * sourceStride);

								for (x = 0; x < width; x = x + 2)
								{
									//gray
									if (((((palette[*pCurrent >> 4]) - mean) * contrast) + mean) > threshold)
										pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));

									//gray
									if (((((palette[*pCurrent & 0x0F]) - mean) * contrast) + mean) > threshold)
										pCopy[y * resultStride + ((x + 1) >> 3)] |= (byte)(0x80 >> ((x + 1) & 0x7));

									pCurrent++;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (result != null)
				{
					if (resultData != null)
					{
						result.UnlockBits(resultData);
						resultData = null;
					}

					result.Dispose();
					result = null;
				}

				throw ex;
			}
			finally
			{
				if (resultData != null)
					result.UnlockBits(resultData);
				if (sourceData != null)
					source.UnlockBits(sourceData);
			}

			if (result != null)
				ImageProcessing.Misc.SetBitmapResolution(result, source.HorizontalResolution, source.VerticalResolution);

			return result;
		}
		#endregion
	
		#endregion

	}
}
