using System;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing.Languages;

namespace ImageProcessing
{
	public class Resizing
	{
		#region enum ResizeMode
		public enum ResizeMode
		{
			Quality,
			Fast
		}
		#endregion


		//PUBLIC METHODS
		#region public methods

		#region GetThumbnail()
		/// <summary>
		/// Returns low quality image, bitonal image is resized to 8 bit grayscale image.
		/// </summary>
		/// <param name="bmpSource"></param>
		/// <param name="desiredSize"></param>
		/// <returns></returns>
		public static Bitmap GetThumbnail(Bitmap bmpSource, Size desiredSize)
		{
			return GetThumbnail(bmpSource, Rectangle.Empty, desiredSize);
		}

		public static Bitmap GetThumbnail(Bitmap bmpSource, Rectangle clip, Size desiredSize)
		{
			if (bmpSource == null)
				return null;

			double zoom = (desiredSize.Width / (double)bmpSource.Width) < (desiredSize.Height / (double)bmpSource.Height) ? (desiredSize.Width / (double)bmpSource.Width) : (desiredSize.Height / (double)bmpSource.Height);

			if (zoom == 1)
				return ImageCopier.Copy(bmpSource, clip);

			if (clip.IsEmpty || clip == new Rectangle(0, 0, bmpSource.Width, bmpSource.Height))
			{
				switch (bmpSource.PixelFormat)
				{
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format8bppIndexed:
						{
							if (zoom > 0.4999 && zoom < 0.5001)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom2to1);
							if (zoom > 1.9999 && zoom < 2.0001)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom1to2);
							if (zoom > 1.4999 && zoom < 1.5001)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom2to3);
							if (zoom > 0.3333 && zoom < 0.3334)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom3to1);
							if (zoom > 0.6666 && zoom < 0.6667)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom3to2);
							if (zoom > 1.3333 && zoom < 1.3334)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom3to4);
							if (zoom > 0.2499 && zoom < 0.2501)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom4to1);
							if (zoom > 0.7499 && zoom <0.7501)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom4to3);
							if (zoom >0.1666 && zoom < 0.1667)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom6to1);
							if (zoom > 0.1249 && zoom < 0.1251)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom8to1);
						} break;
					case PixelFormat.Format1bppIndexed:
						{
							if (zoom > 0.2499 && zoom < 0.2501)
								return Interpolation.Interpolate1bppTo8bpp4to1(bmpSource);
							if (zoom > 0.4999 && zoom < 0.5001)
								return Interpolation.Interpolate1bppTo8bpp2to1(bmpSource);
						} break;
				}
			}
			
			if(clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, bmpSource.Width, bmpSource.Height);
			else
				Rectangle.Intersect(clip, new Rectangle(0, 0, bmpSource.Width, bmpSource.Height));

			Bitmap bmpResult = null;

			switch (bmpSource.PixelFormat)
			{
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:
				case PixelFormat.Format24bppRgb:
				case PixelFormat.Format8bppIndexed:
					{
						if(zoom < 1)
							bmpResult = ResizeZoomOutFast(bmpSource, clip, zoom);
						else
							bmpResult = ResizeZoomInFast(bmpSource, clip, zoom);
					} break;
				case PixelFormat.Format1bppIndexed: bmpResult = GetThumbnail1bpp(bmpSource, clip, zoom); break;
				default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}

			if (bmpResult != null)
			{
				Misc.SetBitmapResolution(bmpResult, (float)(bmpSource.HorizontalResolution * zoom), (float)(bmpSource.VerticalResolution * zoom));

				if (bmpSource.Palette != null && (bmpSource.PixelFormat == PixelFormat.Format8bppIndexed || bmpSource.PixelFormat == PixelFormat.Format4bppIndexed))
					bmpResult.Palette = bmpSource.Palette;
			}

			return bmpResult;
		}
		#endregion

		#region Resize()
		/// <summary>
		/// If input image is bitonal, result is bitonal
		/// </summary>
		/// <param name="bmpSource"></param>
		/// <param name="clip"></param>
		/// <param name="zoom"></param>
		/// <returns></returns>
		public static Bitmap Resize(Bitmap bmpSource, Rectangle clip, double zoom, ResizeMode mode)
		{
			if (bmpSource == null)
				return null;

			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bmpSource.Width, bmpSource.Height);

			if (zoom == 1)
				return ImageCopier.Copy(bmpSource, clip);

			if (clip.IsEmpty || clip == new Rectangle(0, 0, bmpSource.Width, bmpSource.Height))
			{
				switch (bmpSource.PixelFormat)
				{
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format8bppIndexed:
						{
							if (zoom > 0.4999 && zoom < 0.5001)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom2to1);
							if (zoom > 1.9999 && zoom < 2.0001)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom1to2);
							if (zoom > 1.4999 && zoom < 1.5001)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom2to3);
							if (zoom >= 0.3333 && zoom < 0.3334)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom3to1);
							if (zoom >= 0.6666 && zoom < 0.6667)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom3to2);
							if (zoom >= 1.3333 && zoom < 1.3334)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom3to4);
							if (zoom > 0.2499 && zoom < 0.2501)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom4to1);
							if (zoom > 0.7499 && zoom < 0.7501)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom4to3);
							if (zoom > 0.1666 && zoom < 0.1667)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom6to1);
							if (zoom > 0.1249 && zoom < 0.1251)
								return Interpolation.Interpolate(bmpSource, Interpolation.Zoom.Zoom8to1);
						} break;
					case PixelFormat.Format1bppIndexed:
						{
							if (zoom > 0.2499 && zoom < 0.2501)
								return Interpolation.Interpolate1bpp4to1(bmpSource);
							if (zoom > 0.4999 && zoom < 0.5001)
								return Interpolation.Interpolate1bpp2to1(bmpSource);
						} break;
				}
			}

			try
			{
				if (clip.IsEmpty)
					clip = new Rectangle(0, 0, bmpSource.Width, bmpSource.Height);
				else
					clip = Rectangle.Intersect(clip, new Rectangle(0, 0, bmpSource.Width, bmpSource.Height));

				Bitmap bmpResult = null;

				switch (bmpSource.PixelFormat)
				{
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format8bppIndexed:
						{
							if (mode == ResizeMode.Fast)
							{
								if (zoom < 1)
									bmpResult = ResizeZoomOutFast(bmpSource, clip, zoom);
								else
									bmpResult = ResizeZoomInFast(bmpSource, clip, zoom);
							}
							else
							{
								if (zoom < 1)
									bmpResult = ResizeZoomOutQuality(bmpSource, clip, zoom);
								else
									bmpResult = ResizeZoomInQuality(bmpSource, clip, zoom);
							}
						}
						break;
					case PixelFormat.Format1bppIndexed: bmpResult = Resize1bpp(bmpSource, clip, zoom); break;
					default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}

				if (bmpResult != null)
				{
					Misc.SetBitmapResolution(bmpResult, (float)(bmpSource.HorizontalResolution * zoom), (float)(bmpSource.VerticalResolution * zoom));

					if (bmpSource.Palette != null && bmpSource.PixelFormat == PixelFormat.Format8bppIndexed && bmpSource.Palette.Entries.Length > 0)
						bmpResult.Palette = bmpSource.Palette;
				}

				return bmpResult;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		/// <summary>
		/// If input image is black and white, result is black and white.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="dpiX"></param>
		/// <param name="dpiY"></param>
		/// <returns></returns>
		public static Bitmap Resize(Bitmap source, float dpiX, float dpiY, ResizeMode mode)
		{
			return Resize(source, Rectangle.Empty, dpiX / source.HorizontalResolution, mode);
		}
		#endregion
	
		#region GetResultBitmapSize()
		public static Size GetResultBitmapSize(Size sourceSize, double zoom, PixelFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				case PixelFormat.Format24bppRgb:
				case PixelFormat.Format8bppIndexed:
					{
						if (zoom > 0.4999 && zoom < 0.5001)
							return Interpolation.GetSize(sourceSize.Width, sourceSize.Height, Interpolation.Zoom.Zoom2to1);
						if (zoom > 1.9999 && zoom < 2.0001)
							return Interpolation.GetSize(sourceSize.Width, sourceSize.Height, Interpolation.Zoom.Zoom1to2);
						if (zoom > 1.4999 && zoom < 1.5001)
							return Interpolation.GetSize(sourceSize.Width, sourceSize.Height, Interpolation.Zoom.Zoom2to3);
						if (zoom > 0.3333 && zoom < 0.3334)
							return Interpolation.GetSize(sourceSize.Width, sourceSize.Height, Interpolation.Zoom.Zoom3to1);
						if (zoom > 0.6666 && zoom < 0.6667)
							return Interpolation.GetSize(sourceSize.Width, sourceSize.Height, Interpolation.Zoom.Zoom3to2);
						if (zoom > 1.3333 && zoom < 1.3334)
							return Interpolation.GetSize(sourceSize.Width, sourceSize.Height, Interpolation.Zoom.Zoom3to4);
						if (zoom > 0.2499 && zoom < 0.2501)
							return Interpolation.GetSize(sourceSize.Width, sourceSize.Height, Interpolation.Zoom.Zoom4to1);
						if (zoom > 0.7499 && zoom <0.7501)
							return Interpolation.GetSize(sourceSize.Width, sourceSize.Height, Interpolation.Zoom.Zoom4to3);
						if (zoom >0.1666 && zoom < 0.1667)
							return Interpolation.GetSize(sourceSize.Width, sourceSize.Height, Interpolation.Zoom.Zoom6to1);
						if (zoom > 0.1249 && zoom < 0.1251)
							return Interpolation.GetSize(sourceSize.Width, sourceSize.Height, Interpolation.Zoom.Zoom8to1);
					} break;
				case PixelFormat.Format1bppIndexed:
					{
						if (zoom > 0.2499 && zoom < 0.2501)
							return Interpolation.GetSize(sourceSize.Width, sourceSize.Height, Interpolation.Zoom.Zoom4to1);
						if (zoom > 0.4999 && zoom < 0.5001)
							return Interpolation.GetSize(sourceSize.Width, sourceSize.Height, Interpolation.Zoom.Zoom2to1);
					} break;
			} 
			
			int resultW = Math.Max(1, (int)(sourceSize.Width * zoom));
			int resultH = Math.Max(1, (int)(sourceSize.Height * zoom));

			return new Size(resultW, resultH);
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region ResizeZoomInFast()
		private static Bitmap ResizeZoomInFast(Bitmap source, Rectangle clip, double zoom)
		{
			if (source == null)
				return null;

			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y;

			try
			{
				int sourceW = clip.Width;
				int sourceH = clip.Height;
				int resultW = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Width;
				int resultH = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Height;

				try
				{
					result = new Bitmap(resultW, resultH, source.PixelFormat);
				}
				catch (Exception ex)
				{
					long totalAllocatedMemory = GC.GetTotalMemory(false);
					throw new Exception(BIPStrings.ResizeCanTAllocateBitmap_STR+"! Width: " + resultW + " , Height: " + resultH + ", Pixels: " + 
						source.PixelFormat.ToString() + ", "+BIPStrings.TotalAllocatedMemory_STR+" "+ totalAllocatedMemory.ToString() + 
						Environment.NewLine + ex.Message, ex);
				}

				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, resultW, resultH), ImageLockMode.WriteOnly, result.PixelFormat);

				double sBlockLength = (1 / zoom);

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;


				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();
					byte* currentS, currentR;

					if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < resultH; y++)
						{
							currentR = scan0R + y * strideR;

							for (x = 0; x < resultW; x++)
							{
								currentS = scan0S + (int)(y * sBlockLength) * strideS + (int)(x * sBlockLength) * 3;
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);
								*(currentR++) = *currentS;
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format32bppArgb || source.PixelFormat == PixelFormat.Format32bppRgb)
					{
						for (y = 0; y < resultH; y++)
						{
							currentR = scan0R + y * strideR;

							for (x = 0; x < resultW; x++)
							{
								currentS = scan0S + (int)(y * sBlockLength) * strideS + (int)(x * sBlockLength) * 4;
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);
								*(currentR++) = *(currentS++);
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < resultH; y++)
						{
							currentR = scan0R + y * strideR;

							for (x = 0; x < resultW; x++)
							{
								currentS = scan0S + (int)(y * sBlockLength) * strideS + (int)(x * sBlockLength);

								*(currentR++) = (byte)(*currentS);
							}
						}
					}
				}

				return result;
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region ResizeZoomOutFast()
		private static Bitmap ResizeZoomOutFast(Bitmap source, Rectangle clip, double zoom)
		{
			if (source == null)
				return null;

			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y, i, j;

			try
			{
				int sourceW = clip.Width;
				int sourceH = clip.Height;
				int resultW = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Width;
				int resultH = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Height;

				result = new Bitmap(resultW, resultH, source.PixelFormat);

				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, resultW, resultH), ImageLockMode.WriteOnly, result.PixelFormat);

				double sBlockLength = (1 / zoom);
				int sBlockLengthInt = (int)sBlockLength;

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;
				int r, g, b, a;
				int limit;


				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();
					byte* currentS, currentR;

					#region 24 bpp
					if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < resultH - 1; y++)
						{
							currentR = scan0R + y * strideR;

							for (x = 0; x < resultW - 1; x++)
							{
								r = 0;
								g = 0;
								b = 0;

								for (j = sBlockLengthInt; j > 0; j--)
								{
									currentS = scan0S + (int)(y * sBlockLength + j) * strideS + (int)(x * sBlockLength) * 3;

									for (i = sBlockLengthInt; i > 0; i--)
									{
										b += *(currentS++);
										g += *(currentS++);
										r += *(currentS++);
									}
								}

								*(currentR++) = (byte)(b / (sBlockLengthInt * sBlockLengthInt));
								*(currentR++) = (byte)(g / (sBlockLengthInt * sBlockLengthInt));
								*(currentR++) = (byte)(r / (sBlockLengthInt * sBlockLengthInt));
							}
						}

						//last row;
						y = resultH - 1;
						currentR = scan0R + y * strideR;
						for (x = 0; x < resultW; x++)
						{
							r = 0;
							g = 0;
							b = 0;
							limit = ((int)((x + 1) * sBlockLength) < sourceW) ? (int)((x + 1) * sBlockLength) : sourceW;

							for (j = (int)(y * sBlockLength); j < sourceH; j++)
							{
								currentS = scan0S + j * strideS + (int)(x * sBlockLength) * 3;

								for (i = limit - (int)(x * sBlockLength); i > 0; i--)
								{
									b += *(currentS++);
									g += *(currentS++);
									r += *(currentS++);
								}
							}

							*(currentR++) = (byte)(b / ((limit - ((int)(x * sBlockLength))) * (sourceH - (int)(y * sBlockLength))));
							*(currentR++) = (byte)(g / ((limit - ((int)(x * sBlockLength))) * (sourceH - (int)(y * sBlockLength))));
							*(currentR++) = (byte)(r / ((limit - ((int)(x * sBlockLength))) * (sourceH - (int)(y * sBlockLength))));
						}

						//last column
						x = resultW - 1;
						for (y = 0; y < resultH; y++)
						{
							currentR = scan0R + y * strideR + x * 3;
							r = 0;
							g = 0;
							b = 0;
							limit = ((int)((y + 1) * sBlockLength) < sourceH) ? (int)((y + 1) * sBlockLength) : sourceH;

							for (j = (int)(y * sBlockLength); j < limit; j++)
							{
								currentS = scan0S + j * strideS + (int)(x * sBlockLength) * 3;

								for (i = sourceW - (int)(x * sBlockLength); i > 0; i--)
								{
									b += *(currentS++);
									g += *(currentS++);
									r += *(currentS++);
								}
							}

							*(currentR++) = (byte)(b / ((limit - (int)(y * sBlockLength)) * (sourceW - (int)(x * sBlockLength))));
							*(currentR++) = (byte)(g / ((limit - (int)(y * sBlockLength)) * (sourceW - (int)(x * sBlockLength))));
							*(currentR++) = (byte)(r / ((limit - (int)(y * sBlockLength)) * (sourceW - (int)(x * sBlockLength))));
						}
					}
					#endregion

					#region 8bpp gray
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed && Misc.IsGrayscale(source))
					{
						for (y = 0; y < resultH - 1; y++)
						{
							currentR = scan0R + y * strideR;

							for (x = 0; x < resultW - 1; x++)
							{
								g = 0;

								//try
								//{
									for (j = sBlockLengthInt; j > 0; j--)
									{
										currentS = scan0S + (int)(y * sBlockLength + j) * strideS + (int)(x * sBlockLength);

										for (i = sBlockLengthInt; i > 0; i--)
											g += *(currentS++);
									}

									*(currentR++) = (byte)(g / (sBlockLengthInt * sBlockLengthInt));
								/*}
								catch(Exception ex)
								{
									ex = ex;
								}*/
							}
						}

						//last row
						y = resultH - 1;
						currentR = scan0R + y * strideR;
						for (x = 0; x < resultW; x++)
						{
							g = 0;
							limit = ((int)((x + 1) * sBlockLength) < sourceW) ? (int)((x + 1) * sBlockLength) : sourceW;

							for (j = (int)(y * sBlockLength); j < sourceH; j++)
							{
								currentS = scan0S + j * strideS + (int)(x * sBlockLength);

								for (i = limit - (int)(x * sBlockLength); i > 0; i--)
									g += *(currentS++);
							}

							*(currentR++) = (byte)(g / ((limit - ((int)(x * sBlockLength))) * (sourceH - (int)(y * sBlockLength))));
						}

						//last column
						x = resultW - 1;
						for (y = 0; y < resultH; y++)
						{
							limit = ((int)((y + 1) * sBlockLength) < sourceH) ? (int)((y + 1) * sBlockLength) : sourceH;
							currentR = scan0R + y * strideR + x;
							g = 0;

							for (j = (int)(y * sBlockLength); j < limit; j++)
							{
								currentS = scan0S + j * strideS + (int)(x * sBlockLength);

								for (i = sourceW - (int)(x * sBlockLength); i > 0; i--)
									g += *(currentS++);
							}

							*(currentR++) = (byte)(g / ((limit - (int)(y * sBlockLength)) * (sourceW - (int)(x * sBlockLength))));
						}
					}
					#endregion

					#region Format8bppIndexed
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						Color[] entries = source.Palette.Entries;
						byte[, ,] inversePalette = ImageProcessing.ColorPalettes.PaletteBuilder.GetInversePalette32x32x32(source.Palette.Entries);

						for (y = 0; y < resultH - 1; y++)
						{
							currentR = scan0R + y * strideR;

							for (x = 0; x < resultW - 1; x++)
							{
								r = 0; g = 0; b = 0;

								for (j = sBlockLengthInt; j > 0; j--)
								{
									currentS = scan0S + (int)(y * sBlockLength + j) * strideS + (int)(x * sBlockLength);

									for (i = sBlockLengthInt; i > 0; i--)
									{
										r += entries[*currentS].R;
										g += entries[*currentS].G;
										b += entries[*currentS].B;

										currentS++;
									}
								}

								*(currentR++) = inversePalette[(byte)(r / (sBlockLengthInt * sBlockLengthInt)) / 8, (byte)(g / (sBlockLengthInt * sBlockLengthInt)) / 8, (byte)(b / (sBlockLengthInt * sBlockLengthInt)) / 8];
							}
						}

						//last row
						y = resultH - 1;
						currentR = scan0R + y * strideR;
						for (x = 0; x < resultW; x++)
						{
							r = 0; g = 0; b = 0;
							limit = ((int)((x + 1) * sBlockLength) < sourceW) ? (int)((x + 1) * sBlockLength) : sourceW;

							for (j = (int)(y * sBlockLength); j < sourceH; j++)
							{
								currentS = scan0S + j * strideS + (int)(x * sBlockLength);

								for (i = limit - (int)(x * sBlockLength); i > 0; i--)
								{
									r += entries[*currentS].R;
									g += entries[*currentS].G;
									b += entries[*currentS].B;

									currentS++;
								}
							}

							*(currentR++) = inversePalette[(byte)(r / ((limit - ((int)(x * sBlockLength))) * (sourceH - (int)(y * sBlockLength)))) / 8, (byte)(g / ((limit - ((int)(x * sBlockLength))) * (sourceH - (int)(y * sBlockLength)))) / 8, (byte)(b / ((limit - ((int)(x * sBlockLength))) * (sourceH - (int)(y * sBlockLength)))) / 8];
						}

						//last column
						x = resultW - 1;
						for (y = 0; y < resultH; y++)
						{
							limit = ((int)((y + 1) * sBlockLength) < sourceH) ? (int)((y + 1) * sBlockLength) : sourceH;
							currentR = scan0R + y * strideR + x;
							r = 0; g = 0; b = 0;

							for (j = (int)(y * sBlockLength); j < limit; j++)
							{
								currentS = scan0S + j * strideS + (int)(x * sBlockLength);

								for (i = sourceW - (int)(x * sBlockLength); i > 0; i--)
								{
									r += entries[*currentS].R;
									g += entries[*currentS].G;
									b += entries[*currentS].B;

									currentS++;
								}
							}

							*(currentR++) = inversePalette[(byte)(r / ((limit - (int)(y * sBlockLength)) * (sourceW - (int)(x * sBlockLength)))) / 8, (byte)(g / ((limit - (int)(y * sBlockLength)) * (sourceW - (int)(x * sBlockLength)))) / 8, (byte)(b / ((limit - (int)(y * sBlockLength)) * (sourceW - (int)(x * sBlockLength)))) / 8];
						}
					}
					#endregion

					#region 32 bpp
					else if (source.PixelFormat == PixelFormat.Format32bppArgb || source.PixelFormat == PixelFormat.Format32bppRgb)
					{
						for (y = 0; y < resultH - 1; y++)
						{
							currentR = scan0R + y * strideR;

							for (x = 0; x < resultW - 1; x++)
							{
								r = 0;
								g = 0;
								b = 0;
								a = 0;

								for (j = sBlockLengthInt; j > 0; j--)
								{
									currentS = scan0S + (int)(y * sBlockLength + j) * strideS + (int)(x * sBlockLength) * 4;

									for (i = sBlockLengthInt; i > 0; i--)
									{
										b += *(currentS++);
										g += *(currentS++);
										r += *(currentS++);
										a += *(currentS++);
									}
								}

								*(currentR++) = (byte)(b / (sBlockLengthInt * sBlockLengthInt));
								*(currentR++) = (byte)(g / (sBlockLengthInt * sBlockLengthInt));
								*(currentR++) = (byte)(r / (sBlockLengthInt * sBlockLengthInt));
								*(currentR++) = (byte)(a / (sBlockLengthInt * sBlockLengthInt));
							}
						}

						//last row
						y = resultH - 1;
						currentR = scan0R + y * strideR;
						for (x = 0; x < resultW; x++)
						{
							r = 0;
							g = 0;
							b = 0;
							a = 0;
							limit = ((int)((x + 1) * sBlockLength) < sourceW) ? (int)((x + 1) * sBlockLength) : sourceW;

							for (j = (int)(y * sBlockLength); j < sourceH; j++)
							{
								currentS = scan0S + j * strideS + (int)(x * sBlockLength) * 4;

								for (i = limit - (int)(x * sBlockLength); i > 0; i--)
								{
									b += *(currentS++);
									g += *(currentS++);
									r += *(currentS++);
									a += *(currentS++);
								}
							}

							*(currentR++) = (byte)(b / ((limit - ((int)(x * sBlockLength))) * (sourceH - (int)(y * sBlockLength))));
							*(currentR++) = (byte)(g / ((limit - ((int)(x * sBlockLength))) * (sourceH - (int)(y * sBlockLength))));
							*(currentR++) = (byte)(r / ((limit - ((int)(x * sBlockLength))) * (sourceH - (int)(y * sBlockLength))));
							*(currentR++) = (byte)(a / ((limit - ((int)(x * sBlockLength))) * (sourceH - (int)(y * sBlockLength))));
						}

						//last column
						x = resultW - 1;
						for (y = 0; y < resultH; y++)
						{
							currentR = scan0R + y * strideR + x * 4;
							r = 0;
							g = 0;
							b = 0;
							a = 0;
							limit = ((int)((y + 1) * sBlockLength) < sourceH) ? (int)((y + 1) * sBlockLength) : sourceH;

							for (j = (int)(y * sBlockLength); j < limit; j++)
							{
								currentS = scan0S + j * strideS + (int)(x * sBlockLength) * 4;

								for (i = sourceW - (int)(x * sBlockLength); i > 0; i--)
								{
									b += *(currentS++);
									g += *(currentS++);
									r += *(currentS++);
									a += *(currentS++);
								}
							}

							*(currentR++) = (byte)(b / ((limit - (int)(y * sBlockLength)) * (sourceW - (int)(x * sBlockLength))));
							*(currentR++) = (byte)(g / ((limit - (int)(y * sBlockLength)) * (sourceW - (int)(x * sBlockLength))));
							*(currentR++) = (byte)(r / ((limit - (int)(y * sBlockLength)) * (sourceW - (int)(x * sBlockLength))));
							*(currentR++) = (byte)(a / ((limit - (int)(y * sBlockLength)) * (sourceW - (int)(x * sBlockLength))));
						}
					}
					#endregion
				}

				return result;
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region Resize1bpp()
		private static Bitmap Resize1bpp(Bitmap source, Rectangle clip, double zoom)
		{
			if (zoom == 1)
				return ImageCopier.Copy(source, clip);
			else if (zoom > 1)
				return Resize1bppZoomIn(source, clip, zoom);
			else
				return Resize1bppZoomOut(source, clip, zoom);
		}
		#endregion

		#region Resize1bppZoomIn()
		private static Bitmap Resize1bppZoomIn(Bitmap source, Rectangle clip, double zoom)
		{
			if (source == null)
				return null;

			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y, i;

			try
			{
				int sourceW = clip.Width;
				int sourceH = clip.Height;
				int resultW = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Width;
				int resultH = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Height;

				result = new Bitmap(resultW, resultH, source.PixelFormat);

				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, resultW, resultH), ImageLockMode.WriteOnly, result.PixelFormat);

				double sBlockLength = (1 / zoom);

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();
					byte gray;

					int[] sLine;
					int sourceY;
					int[] transformTable = new int[resultW];

					for(i = 0; i < resultW; i++)
						transformTable[i] = (int)(i * sBlockLength);

					for (y = 0; y < resultH; y++)
					{
						sourceY = (int)(y * sBlockLength) * strideS;
						sLine = new int[strideS * 8];

						for (i = 0; i < sourceW; i += 8)
						{
							gray = scan0S[sourceY + i / 8];

							if ((gray & 0x80) != 0)
								sLine[i] = 1;
							if ((gray & 0x40) != 0)
								sLine[i+1] = 1;
							if ((gray & 0x20) != 0)
								sLine[i+2] = 1;
							if ((gray & 0x10) != 0)
								sLine[i+3] = 1;
							if ((gray & 0x8) != 0)
								sLine[i+4] = 1;
							if ((gray & 0x4) != 0)
								sLine[i+5] = 1;
							if ((gray & 0x2) != 0)
								sLine[i+6] = 1;
							if ((gray & 0x1) != 0)
								sLine[i+7] = 1;
						}

						for (x = 0; x < resultW - 8; x += 8)
						{
								scan0R[y * strideR + (x >> 3)] = (byte)(
									sLine[transformTable[x]] << 7 |
									sLine[transformTable[x+1]] << 6 |
									sLine[transformTable[x+2]] << 5 |
									sLine[transformTable[x+3]] << 4 |
									sLine[transformTable[x+4]] << 3 |
									sLine[transformTable[x+5]] << 2 |
									sLine[transformTable[x+6]] << 1 |
									sLine[transformTable[x+7]]
									);
						}
						for (x = resultW - 8; x < resultW; x++)
							if (sLine[transformTable[x]] == 1)
								scan0R[y * strideR + (x >> 3)] |= (byte)(0x80 >> (x & 0x07));

					}
				}

				return result;
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region Resize1bppZoomOut()
		private static Bitmap Resize1bppZoomOut(Bitmap source, Rectangle clip, double zoom)
		{
			if (source == null)
				return null;

			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y, i, j;

			try
			{
				int sourceW = clip.Width;
				int sourceH = clip.Height;
				int resultW = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Width;
				int resultH = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Height;

				result = new Bitmap(resultW, resultH, source.PixelFormat);

				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, resultW, resultH), ImageLockMode.WriteOnly, result.PixelFormat);

				double sBlockLength = (1 / zoom);
				int limit;

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();

					int[] transformTable = new int[sourceW > sourceH ? sourceW : sourceH];
					int whitePixelsCount;

					for (i = transformTable.Length - 1; i >= 0; i--)
						transformTable[i] = (int)(i * sBlockLength);

					for (y = 0; y < resultH - 1; y++)
					{
						for (x = 0; x < resultW - 1; x++)
						{
							whitePixelsCount = 0;

							for (j = transformTable[y]; j < transformTable[y] + sBlockLength; j++)
								for (i = transformTable[x]; i < transformTable[x] + sBlockLength; i++)
									if ((scan0S[j * strideS + (i >> 3)] & (0x80 >> (i & 0x07))) > 0)
										whitePixelsCount++;

							if (whitePixelsCount >= (sBlockLength * sBlockLength * 3 / 4))
								scan0R[y * strideR + (x >> 3)] |= (byte)(0x80 >> (x & 0x07));
						}
					}

					//last row
					y = resultH - 1;
					for (x = 0; x < resultW; x++)
					{
						whitePixelsCount = 0;
						limit = (int)((transformTable[x] + sBlockLength < sourceW) ? transformTable[x] + sBlockLength : sourceW);

						for (j = transformTable[y]; j < sourceH; j++)
							for (i = transformTable[x]; i < limit; i++)
								if ((scan0S[j * strideS + (i >> 3)] & (0x80 >> (i & 0x07))) > 0)
									whitePixelsCount++;

						if (whitePixelsCount >= ((limit - transformTable[x]) * (sourceH - transformTable[y]) / 2))
							scan0R[y * strideR + (x >> 3)] |= (byte)(0x80 >> (x & 0x07));
					}

					//last column
					x = resultW - 1;
					for (y = 0; y < resultH; y++)
					{
						whitePixelsCount = 0;
						limit = (int)((transformTable[y] + sBlockLength < sourceH) ? transformTable[y] + sBlockLength : sourceH);

						for (j = transformTable[y]; j < limit; j++)
							for (i = transformTable[x]; i < sourceW; i++)
								if ((scan0S[j * strideS + (i >> 3)] & (0x80 >> (i & 0x07))) > 0)
									whitePixelsCount++;

						if (whitePixelsCount >= ((sourceW - transformTable[x]) * (limit - transformTable[y]) / 2))
							scan0R[y * strideR + (x >> 3)] |= (byte)(0x80 >> (x & 0x07));
					}
				}

				return result;
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region GetThumbnail1bpp()
		private static Bitmap GetThumbnail1bpp(Bitmap source, Rectangle clip, double zoom)
		{
			if (source == null)
				return null;

			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y, i, j;

			try
			{
				int sourceW = clip.Width;
				int sourceH = clip.Height;
				int resultW = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Width;
				int resultH = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Height;

				result = new Bitmap(resultW, resultH, PixelFormat.Format8bppIndexed);

				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, resultW, resultH), ImageLockMode.WriteOnly, result.PixelFormat);

				double sBlockLength = (1 / zoom);
				double sBlockLengthInt = ((int)sBlockLength > 0) ? ((int)sBlockLength) : 1;

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();

					int[] transformTable = new int[resultW > resultH ? resultW : resultH];
					int whitePixelsCount;

					for (i = transformTable.Length - 1; i >= 0; i--)
						transformTable[i] = (int)(i * sBlockLength);

					if (zoom > 0.5 && zoom < 1.0)
					{
						double zoomR = 1 / zoom;
						double delimiter = zoom * zoom - 4 * zoom + 4;
						double gray;
						
						for (y = 0; y < resultH - 1; y++)
						{
							for (x = 0; x < resultW - 1; x++)
							{
								j = transformTable[y];
								i = transformTable[x];
								gray = 0;

								if ((scan0S[j * strideS + (i >> 3)] & (0x80 >> (i & 0x07))) > 0)
									gray += + 1;
								if ((scan0S[(j+1) * strideS + (i >> 3)] & (0x80 >> (i & 0x07))) > 0)
									gray += + (1 - zoom);
								if ((scan0S[(j) * strideS + ((i+1) >> 3)] & (0x80 >> ((i+1) & 0x07))) > 0)
									gray += + (1 - zoom);
								if ((scan0S[(j+1) * strideS + ((i + 1) >> 3)] & (0x80 >> ((i + 1) & 0x07))) > 0)
									gray += + (1 - zoom) * (1 - zoom);

								if(gray > 0)
									scan0R[y * strideR + x] = (byte)(gray * 255 / (delimiter));
							}
						}
					}
					else
					{
						for (y = 0; y < resultH - 1; y++)
						{
							for (x = 0; x < resultW - 1; x++)
							{
								whitePixelsCount = 0;

								for (j = transformTable[y]; j < transformTable[y] + sBlockLengthInt; j++)
									for (i = transformTable[x]; i < transformTable[x] + sBlockLengthInt; i++)
										if ((scan0S[j * strideS + (i >> 3)] & (0x80 >> (i & 0x07))) > 0)
											whitePixelsCount++;

								scan0R[y * strideR + x] = (byte)((whitePixelsCount * 255) / (sBlockLengthInt * sBlockLengthInt));
							}
						}
					}

					//last row
					y = resultH - 1;
					for (x = 0; x < resultW; x++)
					{
						whitePixelsCount = 0;
						int pixels = 0;

						for (j = transformTable[y]; j < transformTable[y] + sBlockLengthInt && j < sourceH; j++)
							for (i = transformTable[x]; i < transformTable[x] + sBlockLengthInt && i < sourceW; i++)
							{
								if ((scan0S[j * strideS + (i >> 3)] & (0x80 >> (i & 0x07))) > 0)
									whitePixelsCount++;

								pixels++;
							}

						if(pixels > 0 && whitePixelsCount > 0)
							scan0R[y * strideR + x] = (byte)((whitePixelsCount * 255) / (pixels));
					}


					//last column
					x = resultW - 1;
					for (y = 0; y < resultH - 1; y++)
					{
						whitePixelsCount = 0;
						int pixels = 0;

						for (j = transformTable[y]; j < transformTable[y] + sBlockLengthInt && j < sourceH; j++)
							for (i = transformTable[x]; i < transformTable[x] + sBlockLengthInt && i < sourceW; i++)
							{
								if ((scan0S[j * strideS + (i >> 3)] & (0x80 >> (i & 0x07))) > 0)
									whitePixelsCount++;

								pixels++;
							}

						if (pixels > 0 && whitePixelsCount > 0)
							scan0R[y * strideR + x] = (byte)((whitePixelsCount * 255) / (pixels));
					}
				}

				result.Palette = Misc.GrayscalePalette;
				return result;
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region ResizeZoomOutQuality()
		private static Bitmap ResizeZoomOutQuality(Bitmap source, Rectangle clip, double zoom)
		{
			if (source == null)
				return null;

			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y;

			try
			{
				int sourceW = clip.Width;
				int sourceH = clip.Height;
				int resultW = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Width;
				int resultH = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Height;

				result = new Bitmap(resultW, resultH, source.PixelFormat);

				if (source.Palette != null && source.Palette.Entries.Length > 0)
					result.Palette = Misc.GetColorPalette(source.Palette.Entries);

				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;
				double r, g, b;

				double xPosition, yPosition;
				double xLimit, yLimit;
				double xPixelPortion, yPixelPortion;
				double xFrom, yFrom;

				zoom = 1 / zoom;

				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();

					if (source.PixelFormat == PixelFormat.Format8bppIndexed && Misc.IsGrayscale(source) == false)
					{
						Color[] entries = source.Palette.Entries;
						byte[, ,] inversePalette = ImageProcessing.ColorPalettes.PaletteBuilder.GetInversePalette32x32x32(source.Palette.Entries);

						for (y = 0; y < resultH; y++)
						{
							yFrom = zoom * y;
							yLimit = (yFrom + zoom < sourceH - 1) ? (yFrom + zoom) : sourceH - 1;

							for (x = 0; x < resultW; x++)
							{
								xFrom = zoom * x;
								xLimit = (xFrom + zoom < sourceW - 1) ? (xFrom + zoom) : sourceW - 1;
								r = g = b = 0;

								for (yPosition = yFrom; yPosition < (int)yLimit; yPosition = (int)yPosition + 1)
								{
									yPixelPortion = 1 - (yPosition - (int)yPosition);

									//first column
									xPixelPortion = 1 - (xFrom - (int)xFrom);
									r += entries[scan0S[(int)yPosition * strideS + (int)xFrom]].R * xPixelPortion * yPixelPortion;
									g += entries[scan0S[(int)yPosition * strideS + (int)xFrom]].G * xPixelPortion * yPixelPortion;
									b += entries[scan0S[(int)yPosition * strideS + (int)xFrom]].B * xPixelPortion * yPixelPortion;

									//the rest of columns
									for (xPosition = (int)xFrom + 1; xPosition < xLimit; xPosition = (int)xPosition + 1)
									{
										if (xLimit - xPosition > 1)
											xPixelPortion = 1;
										else
											xPixelPortion = xLimit - xPosition;

										r += entries[scan0S[(int)yPosition * strideS + (int)xPosition]].R * xPixelPortion * yPixelPortion;
										g += entries[scan0S[(int)yPosition * strideS + (int)xPosition]].G * xPixelPortion * yPixelPortion;
										b += entries[scan0S[(int)yPosition * strideS + (int)xPosition]].B * xPixelPortion * yPixelPortion;
									}
								}

								//last row
								{
									yPosition = (int)yLimit;
									yPixelPortion = yLimit - yPosition;

									//first column
									xPixelPortion = 1 - (xFrom - (int)xFrom);
									r += entries[scan0S[(int)yPosition * strideS + (int)xFrom]].R * xPixelPortion * yPixelPortion;
									g += entries[scan0S[(int)yPosition * strideS + (int)xFrom]].G * xPixelPortion * yPixelPortion;
									b += entries[scan0S[(int)yPosition * strideS + (int)xFrom]].B * xPixelPortion * yPixelPortion;

									//the rest of columns
									for (xPosition = (int)xFrom + 1; xPosition < xLimit; xPosition = (int)xPosition + 1)
									{
										if (xLimit - xPosition > 1)
											xPixelPortion = 1;
										else
											xPixelPortion = xLimit - xPosition;

										r += entries[scan0S[(int)yPosition * strideS + (int)xPosition]].R * xPixelPortion * yPixelPortion;
										g += entries[scan0S[(int)yPosition * strideS + (int)xPosition]].G * xPixelPortion * yPixelPortion;
										b += entries[scan0S[(int)yPosition * strideS + (int)xPosition]].B * xPixelPortion * yPixelPortion;
									}
								}

								//scan0R[y * strideR + x] = (byte)(g / (zoom * zoom));
								scan0R[y * strideR + x] = inversePalette[(byte)((r / (zoom * zoom))) / 8, (byte)(g / (zoom * zoom)) / 8, (byte)(b / (zoom*zoom)) / 8];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < resultH; y++)
						{
							yFrom = zoom * y;
							yLimit = (yFrom + zoom < sourceH - 1) ? (yFrom + zoom) : sourceH - 1;

							for (x = 0; x < resultW; x++)
							{
								xFrom = zoom * x;
								xLimit = (xFrom + zoom < sourceW - 1) ? (xFrom + zoom) : sourceW - 1;
								g = 0;

								for (yPosition = yFrom; yPosition < (int)yLimit; yPosition = (int)yPosition + 1)
								{
									yPixelPortion = 1 - (yPosition - (int)yPosition);

									//first column
									xPixelPortion = 1 - (xFrom - (int)xFrom);
									g += scan0S[(int)yPosition * strideS + (int)xFrom] * xPixelPortion * yPixelPortion;

									//the rest of columns
									for (xPosition = (int)xFrom + 1; xPosition < xLimit; xPosition = (int)xPosition + 1)
									{
										if (xLimit - xPosition > 1)
											xPixelPortion = 1;
										else
											xPixelPortion = xLimit - xPosition;

										g += scan0S[(int)yPosition * strideS + (int)xPosition] * xPixelPortion * yPixelPortion;
									}
								}

								//last row
								{
									yPosition = (int)yLimit;
									yPixelPortion = yLimit - yPosition;

									//first column
									xPixelPortion = 1 - (xFrom - (int)xFrom);
									g += scan0S[(int)yPosition * strideS + (int)xFrom] * xPixelPortion * yPixelPortion;

									//the rest of columns
									for (xPosition = (int)xFrom + 1; xPosition < xLimit; xPosition = (int)xPosition + 1)
									{
										if (xLimit - xPosition > 1)
											xPixelPortion = 1;
										else
											xPixelPortion = xLimit - xPosition;

										g += scan0S[(int)yPosition * strideS + (int)xPosition] * xPixelPortion * yPixelPortion;
									}
								}

								scan0R[y * strideR + x] = (byte)(g / (zoom * zoom));
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						try
						{
							for (y = 0; y < resultH; y++)
							{
								yFrom = zoom * y;
								yLimit = (yFrom + zoom < sourceH - 1) ? (yFrom + zoom) : sourceH - 1;

								for (x = 0; x < resultW; x++)
								{
									xFrom = zoom * x;
									xLimit = (xFrom + zoom < sourceW - 1) ? (xFrom + zoom) : sourceW - 1;
									b = g = r = 0;

									for (yPosition = yFrom; yPosition < (int)yLimit; yPosition = (int)yPosition + 1)
									{
										yPixelPortion = 1 - (yPosition - (int)yPosition);

										//first column
										xPixelPortion = 1 - (xFrom - (int)xFrom);
										b += scan0S[(int)yPosition * strideS + (int)xFrom * 3] * xPixelPortion * yPixelPortion;
										g += scan0S[(int)yPosition * strideS + (int)xFrom * 3 + 1] * xPixelPortion * yPixelPortion;
										r += scan0S[(int)yPosition * strideS + (int)xFrom * 3 + 2] * xPixelPortion * yPixelPortion;

										//the rest of columns
										for (xPosition = (int)xFrom + 1; xPosition < xLimit; xPosition = (int)xPosition + 1)
										{
											if (xLimit - xPosition > 1)
												xPixelPortion = 1;
											else
												xPixelPortion = xLimit - xPosition;

											b += scan0S[(int)yPosition * strideS + (int)xPosition * 3] * xPixelPortion * yPixelPortion;
											g += scan0S[(int)yPosition * strideS + (int)xPosition * 3 + 1] * xPixelPortion * yPixelPortion;
											r += scan0S[(int)yPosition * strideS + (int)xPosition * 3 + 2] * xPixelPortion * yPixelPortion;
										}
									}

									//last row
									{
										yPosition = (int)yLimit;
										yPixelPortion = yLimit - yPosition;

										//first column
										xPixelPortion = 1 - (xFrom - (int)xFrom);
										b += scan0S[(int)yPosition * strideS + (int)xFrom * 3] * xPixelPortion * yPixelPortion;
										g += scan0S[(int)yPosition * strideS + (int)xFrom * 3 + 1] * xPixelPortion * yPixelPortion;
										r += scan0S[(int)yPosition * strideS + (int)xFrom * 3 + 2] * xPixelPortion * yPixelPortion;

										//the rest of columns
										for (xPosition = (int)xFrom + 1; xPosition < xLimit; xPosition = (int)xPosition + 1)
										{
											if (xLimit - xPosition > 1)
												xPixelPortion = 1;
											else
												xPixelPortion = xLimit - xPosition;

											b += yPixelPortion * xPixelPortion * scan0S[(int)yPosition * strideS + (int)xPosition * 3];
											g += yPixelPortion * xPixelPortion * scan0S[(int)yPosition * strideS + (int)xPosition * 3 + 1];
											r += yPixelPortion * xPixelPortion * scan0S[(int)yPosition * strideS + (int)xPosition * 3 + 2];
										}
									}

									scan0R[y * strideR + x * 3] = (byte)(b / (zoom * zoom));
									scan0R[y * strideR + x * 3 + 1] = (byte)(g / (zoom * zoom));
									scan0R[y * strideR + x * 3 + 2] = (byte)(r / (zoom * zoom));
								}
							}
						}
						catch (Exception ex)
						{
							throw ex;
						}
					}
					else if (source.PixelFormat == PixelFormat.Format32bppArgb || source.PixelFormat == PixelFormat.Format32bppRgb)
					{
						double a;
						try
						{
							for (y = 0; y < resultH; y++)
							{
								yFrom = zoom * y;
								yLimit = (yFrom + zoom < sourceH) ? (yFrom + zoom) : sourceH - 1;

								for (x = 0; x < resultW; x++)
								{
									xFrom = zoom * x;
									xLimit = (xFrom + zoom < sourceW) ? (xFrom + zoom) : sourceW - 1;
									b = g = r = a = 0;

									for (yPosition = yFrom; yPosition < (int)yLimit; yPosition = (int)yPosition + 1)
									{
										yPixelPortion = 1 - (yPosition - (int)yPosition);

										//first column
										xPixelPortion = 1 - (xFrom - (int)xFrom);
										b += scan0S[(int)yPosition * strideS + (int)xFrom * 4] * xPixelPortion * yPixelPortion;
										g += scan0S[(int)yPosition * strideS + (int)xFrom * 4 + 1] * xPixelPortion * yPixelPortion;
										r += scan0S[(int)yPosition * strideS + (int)xFrom * 4 + 2] * xPixelPortion * yPixelPortion;
										a += scan0S[(int)yPosition * strideS + (int)xFrom * 4 + 3] * xPixelPortion * yPixelPortion;

										//the rest of columns
										for (xPosition = (int)xFrom + 1; xPosition < xLimit; xPosition = (int)xPosition + 1)
										{
											if (xLimit - xPosition > 1)
												xPixelPortion = 1;
											else
												xPixelPortion = xLimit - xPosition;

											b += scan0S[(int)yPosition * strideS + (int)xPosition * 4] * xPixelPortion * yPixelPortion;
											g += scan0S[(int)yPosition * strideS + (int)xPosition * 4 + 1] * xPixelPortion * yPixelPortion;
											r += scan0S[(int)yPosition * strideS + (int)xPosition * 4 + 2] * xPixelPortion * yPixelPortion;
											a += scan0S[(int)yPosition * strideS + (int)xPosition * 4 + 3] * xPixelPortion * yPixelPortion;
										}
									}

									//last row
									{
										yPosition = (int)yLimit;
										yPixelPortion = yLimit - yPosition;

										//first column
										xPixelPortion = 1 - (xFrom - (int)xFrom);
										b += scan0S[(int)yPosition * strideS + (int)xFrom * 4] * xPixelPortion * yPixelPortion;
										g += scan0S[(int)yPosition * strideS + (int)xFrom * 4 + 1] * xPixelPortion * yPixelPortion;
										r += scan0S[(int)yPosition * strideS + (int)xFrom * 4 + 2] * xPixelPortion * yPixelPortion;
										a += scan0S[(int)yPosition * strideS + (int)xFrom * 4 + 3] * xPixelPortion * yPixelPortion;

										//the rest of columns
										for (xPosition = (int)xFrom + 1; xPosition < xLimit; xPosition = (int)xPosition + 1)
										{
											if (xLimit - xPosition > 1)
												xPixelPortion = 1;
											else
												xPixelPortion = xLimit - xPosition;

											b += yPixelPortion * xPixelPortion * scan0S[(int)yPosition * strideS + (int)xPosition * 4];
											g += yPixelPortion * xPixelPortion * scan0S[(int)yPosition * strideS + (int)xPosition * 4 + 1];
											r += yPixelPortion * xPixelPortion * scan0S[(int)yPosition * strideS + (int)xPosition * 4 + 2];
											a += yPixelPortion * xPixelPortion * scan0S[(int)yPosition * strideS + (int)xPosition * 4 + 3];
										}
									}

									scan0R[y * strideR + x * 4] = (byte)(b / (zoom * zoom));
									scan0R[y * strideR + x * 4 + 1] = (byte)(g / (zoom * zoom));
									scan0R[y * strideR + x * 4 + 2] = (byte)(r / (zoom * zoom));
									scan0R[y * strideR + x * 4 + 3] = (byte)(a / (zoom * zoom));
								}
							}
						}
						catch(Exception)
						{
							throw;
						}
					}
					return result;
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region ResizeZoomInQuality()
		private static Bitmap ResizeZoomInQuality(Bitmap source, Rectangle clip, double zoom)
		{
			if (source == null)
				return null;

			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y;

			try
			{
				int sourceW = clip.Width;
				int sourceH = clip.Height;
				int resultW = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Width;
				int resultH = GetResultBitmapSize(clip.Size, zoom, source.PixelFormat).Height;

				result = new Bitmap(resultW, resultH, source.PixelFormat);

				if (source.Palette != null && source.Palette.Entries.Length > 0)
					result.Palette = Misc.GetColorPalette(source.Palette.Entries);

				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;
				//double r, g, b;

				double xLimit, yLimit;
				double xFrom, yFrom;

				zoom = 1 / zoom;
				double zoom2 = zoom * zoom;

				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();

					if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < resultH; y++)
						{
							yFrom = zoom * y;
							yLimit = (yFrom + zoom < sourceH) ? (yFrom + zoom) : sourceH - 1;

							if ((int)yLimit > (int)yFrom)
							{
								for (x = 0; x < resultW; x++)
								{
									xFrom = zoom * x;
									xLimit = (xFrom + zoom < sourceW) ? (xFrom + zoom) : sourceW - 1;

									if ((int)xLimit > (int)xFrom)
									{
										scan0R[y * strideR + x] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom] * (1 - (xFrom - (int)xFrom)) * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xFrom] * (1 - (xFrom - (int)xFrom)) * (yLimit - (int)yLimit) +
											scan0S[(int)yFrom * strideS + (int)xLimit] * (xLimit - (int)xLimit) * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xLimit] * (xLimit - (int)xLimit) * (yLimit - (int)yLimit)
											) / zoom2);
									}
									else
									{
										scan0R[y * strideR + x] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom] * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xFrom] * (yLimit - (int)yLimit)
											) / zoom);
									}
								}
							}
							else
							{
								for (x = 0; x < resultW; x++)
								{
									xFrom = zoom * x;
									xLimit = (xFrom + zoom < sourceW) ? (xFrom + zoom) : sourceW - 1;

									if ((int)xLimit > (int)xFrom)
									{
										scan0R[y * strideR + x] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom] * (1 - (xFrom - (int)xFrom))  +
											scan0S[(int)yFrom * strideS + (int)xLimit] * (xLimit - (int)xLimit) 
											) / zoom);
									}
									else
									{
										scan0R[y * strideR + x] = scan0S[(int)yFrom * strideS + (int)xFrom];
									}
								}
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						try
						{
							for (y = 0; y < resultH; y++)
							{
								yFrom = zoom * y;
								yLimit = (yFrom + zoom < sourceH) ? (yFrom + zoom) : sourceH - 1;

								if ((int)yLimit > (int)yFrom)
								{
									for (x = 0; x < resultW; x++)
									{
										xFrom = zoom * x;
										xLimit = (xFrom + zoom < sourceW) ? (xFrom + zoom) : sourceW - 1;

										if ((int)xLimit > (int)xFrom)
										{
											scan0R[y * strideR + x * 3] = (byte)((
												scan0S[(int)yFrom * strideS + (int)xFrom * 3] * (1 - (xFrom - (int)xFrom)) * (1 - (yFrom - (int)yFrom)) +
												scan0S[(int)yLimit * strideS + (int)xFrom * 3] * (1 - (xFrom - (int)xFrom)) * (yLimit - (int)yLimit) +
												scan0S[(int)yFrom * strideS + (int)xLimit * 3] * (xLimit - (int)xLimit) * (1 - (yFrom - (int)yFrom)) +
												scan0S[(int)yLimit * strideS + (int)xLimit * 3] * (xLimit - (int)xLimit) * (yLimit - (int)yLimit)
												) / zoom2);
											scan0R[y * strideR + x * 3 + 1] = (byte)((
												scan0S[(int)yFrom * strideS + (int)xFrom * 3 + 1] * (1 - (xFrom - (int)xFrom)) * (1 - (yFrom - (int)yFrom)) +
												scan0S[(int)yLimit * strideS + (int)xFrom * 3 + 1] * (1 - (xFrom - (int)xFrom)) * (yLimit - (int)yLimit) +
												scan0S[(int)yFrom * strideS + (int)xLimit * 3 + 1] * (xLimit - (int)xLimit) * (1 - (yFrom - (int)yFrom)) +
												scan0S[(int)yLimit * strideS + (int)xLimit * 3 + 1] * (xLimit - (int)xLimit) * (yLimit - (int)yLimit)
												) / zoom2);
											scan0R[y * strideR + x * 3 + 2] = (byte)((
												scan0S[(int)yFrom * strideS + (int)xFrom * 3 + 2] * (1 - (xFrom - (int)xFrom)) * (1 - (yFrom - (int)yFrom)) +
												scan0S[(int)yLimit * strideS + (int)xFrom * 3 + 2] * (1 - (xFrom - (int)xFrom)) * (yLimit - (int)yLimit) +
												scan0S[(int)yFrom * strideS + (int)xLimit * 3 + 2] * (xLimit - (int)xLimit) * (1 - (yFrom - (int)yFrom)) +
												scan0S[(int)yLimit * strideS + (int)xLimit * 3 + 2] * (xLimit - (int)xLimit) * (yLimit - (int)yLimit)
												) / zoom2);
										}
										else
										{
											scan0R[y * strideR + x * 3] = (byte)((
												scan0S[(int)yFrom * strideS + (int)xFrom * 3] * (1 - (yFrom - (int)yFrom)) +
												scan0S[(int)yLimit * strideS + (int)xFrom * 3] * (yLimit - (int)yLimit)
												) / zoom);
											scan0R[y * strideR + x * 3 + 1] = (byte)((
												scan0S[(int)yFrom * strideS + (int)xFrom * 3 + 1] * (1 - (yFrom - (int)yFrom)) +
												scan0S[(int)yLimit * strideS + (int)xFrom * 3 + 1] * (yLimit - (int)yLimit)
												) / zoom);
											scan0R[y * strideR + x * 3 + 2] = (byte)((
												scan0S[(int)yFrom * strideS + (int)xFrom * 3 + 2] * (1 - (yFrom - (int)yFrom)) +
												scan0S[(int)yLimit * strideS + (int)xFrom * 3 + 2] * (yLimit - (int)yLimit)
												) / zoom);
										}
									}
								}
								else
								{
									for (x = 0; x < resultW; x++)
									{
										xFrom = zoom * x;
										xLimit = (xFrom + zoom < sourceW) ? (xFrom + zoom) : sourceW - 1;

										if ((int)xLimit > (int)xFrom)
										{
											scan0R[y * strideR + x * 3] = (byte)((
												scan0S[(int)yFrom * strideS + (int)xFrom * 3] * (1 - (xFrom - (int)xFrom)) +
												scan0S[(int)yFrom * strideS + (int)xLimit * 3] * (xLimit - (int)xLimit)
												) / zoom);
											scan0R[y * strideR + x * 3 + 1] = (byte)((
												scan0S[(int)yFrom * strideS + (int)xFrom * 3 + 1] * (1 - (xFrom - (int)xFrom)) +
												scan0S[(int)yFrom * strideS + (int)xLimit * 3 + 1] * (xLimit - (int)xLimit)
												) / zoom);
											scan0R[y * strideR + x * 3 + 2] = (byte)((
												scan0S[(int)yFrom * strideS + (int)xFrom * 3 + 2] * (1 - (xFrom - (int)xFrom)) +
												scan0S[(int)yFrom * strideS + (int)xLimit * 3 + 2] * (xLimit - (int)xLimit)
												) / zoom);
										}
										else
										{
											scan0R[y * strideR + x * 3] = scan0S[(int)yFrom * strideS + (int)xFrom * 3];
											scan0R[y * strideR + x * 3 + 1] = scan0S[(int)yFrom * strideS + (int)xFrom * 3 + 1];
											scan0R[y * strideR + x * 3 + 2] = scan0S[(int)yFrom * strideS + (int)xFrom * 3 + 2];
										}
									}
								}
							}
						}
						catch (Exception ex)
						{
							throw ex;
						}
					}
					else if (source.PixelFormat == PixelFormat.Format32bppArgb || source.PixelFormat == PixelFormat.Format32bppRgb)
					{
						for (y = 0; y < resultH; y++)
						{
							yFrom = zoom * y;
							yLimit = (yFrom + zoom < sourceH) ? (yFrom + zoom) : sourceH - 1;

							if ((int)yLimit > (int)yFrom)
							{
								for (x = 0; x < resultW; x++)
								{
									xFrom = zoom * x;
									xLimit = (xFrom + zoom < sourceW) ? (xFrom + zoom) : sourceW - 1;

									if ((int)xLimit > (int)xFrom)
									{
										scan0R[y * strideR + x * 4] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom * 4] * (1 - (xFrom - (int)xFrom)) * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xFrom * 4] * (1 - (xFrom - (int)xFrom)) * (yLimit - (int)yLimit) +
											scan0S[(int)yFrom * strideS + (int)xLimit * 4] * (xLimit - (int)xLimit) * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xLimit * 4] * (xLimit - (int)xLimit) * (yLimit - (int)yLimit)
											) / zoom2);
										scan0R[y * strideR + x * 4 + 1] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom * 4 + 1] * (1 - (xFrom - (int)xFrom)) * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xFrom * 4 + 1] * (1 - (xFrom - (int)xFrom)) * (yLimit - (int)yLimit) +
											scan0S[(int)yFrom * strideS + (int)xLimit * 4 + 1] * (xLimit - (int)xLimit) * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xLimit * 4 + 1] * (xLimit - (int)xLimit) * (yLimit - (int)yLimit)
											) / zoom2);
										scan0R[y * strideR + x * 4 + 2] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom * 4 + 2] * (1 - (xFrom - (int)xFrom)) * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xFrom * 4 + 2] * (1 - (xFrom - (int)xFrom)) * (yLimit - (int)yLimit) +
											scan0S[(int)yFrom * strideS + (int)xLimit * 4 + 2] * (xLimit - (int)xLimit) * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xLimit * 4 + 2] * (xLimit - (int)xLimit) * (yLimit - (int)yLimit)
											) / zoom2);
										scan0R[y * strideR + x * 4 + 3] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom * 4 + 3] * (1 - (xFrom - (int)xFrom)) * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xFrom * 4 + 3] * (1 - (xFrom - (int)xFrom)) * (yLimit - (int)yLimit) +
											scan0S[(int)yFrom * strideS + (int)xLimit * 4 + 3] * (xLimit - (int)xLimit) * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xLimit * 4 + 3] * (xLimit - (int)xLimit) * (yLimit - (int)yLimit)
											) / zoom2);
									}
									else
									{
										scan0R[y * strideR + x * 4] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom * 4] * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xFrom * 4] * (yLimit - (int)yLimit)
											) / zoom);
										scan0R[y * strideR + x * 4 + 1] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom * 4 + 1] * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xFrom * 4 + 1] * (yLimit - (int)yLimit)
											) / zoom);
										scan0R[y * strideR + x * 4 + 2] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom * 4 + 2] * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xFrom * 4 + 2] * (yLimit - (int)yLimit)
											) / zoom);
										scan0R[y * strideR + x * 4 + 3] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom * 4 + 3] * (1 - (yFrom - (int)yFrom)) +
											scan0S[(int)yLimit * strideS + (int)xFrom * 4 + 3] * (yLimit - (int)yLimit)
											) / zoom);
									}
								}
							}
							else
							{
								for (x = 0; x < resultW; x++)
								{
									xFrom = zoom * x;
									xLimit = (xFrom + zoom < sourceW) ? (xFrom + zoom) : sourceW - 1;

									if ((int)xLimit > (int)xFrom)
									{
										scan0R[y * strideR + x * 4] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom * 4] * (1 - (xFrom - (int)xFrom)) +
											scan0S[(int)yFrom * strideS + (int)xLimit * 4] * (xLimit - (int)xLimit)
											) / zoom);
										scan0R[y * strideR + x * 4 + 1] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom * 4 + 1] * (1 - (xFrom - (int)xFrom)) +
											scan0S[(int)yFrom * strideS + (int)xLimit * 4 + 1] * (xLimit - (int)xLimit)
											) / zoom);
										scan0R[y * strideR + x * 4 + 2] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom * 4 + 2] * (1 - (xFrom - (int)xFrom)) +
											scan0S[(int)yFrom * strideS + (int)xLimit * 4 + 2] * (xLimit - (int)xLimit)
											) / zoom);
										scan0R[y * strideR + x * 4 + 3] = (byte)((
											scan0S[(int)yFrom * strideS + (int)xFrom * 4 + 3] * (1 - (xFrom - (int)xFrom)) +
											scan0S[(int)yFrom * strideS + (int)xLimit * 4 + 3] * (xLimit - (int)xLimit)
											) / zoom);
									}
									else
									{
										scan0R[y * strideR + x * 4] = scan0S[(int)yFrom * strideS + (int)xFrom * 4];
										scan0R[y * strideR + x * 4 + 1] = scan0S[(int)yFrom * strideS + (int)xFrom * 4 + 1];
										scan0R[y * strideR + x * 4 + 2] = scan0S[(int)yFrom * strideS + (int)xFrom * 4 + 2];
										scan0R[y * strideR + x * 4 + 3] = scan0S[(int)yFrom * strideS + (int)xFrom * 4 + 3];
									}
								}
							}
						}
					}
					return result;
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region GetRequiredBufferSize()
		private static int GetRequiredBufferSize(int bitmapWidth, int bitmapHeight, PixelFormat pixelFormat)
		{
			int requiredBufferSize = Convert.ToInt32((bitmapWidth * bitmapHeight * Misc.BytesPerPixel(pixelFormat)) / (1024 * 1024));

			if (requiredBufferSize > 10)
			{
				try
				{
					GC.Collect();
					System.Runtime.MemoryFailPoint memoryFailPoint = new System.Runtime.MemoryFailPoint(requiredBufferSize);
					memoryFailPoint.Dispose();
				}
				catch (OutOfMemoryException exception)
				{
					ImageProcessing.Misc.bufferSize = Math.Max(ImageProcessing.Misc.minBufferSize, ImageProcessing.Misc.bufferSize / 2);
					throw new Exception(BIPStrings.Resizing1ThereIsNotSufficientMemoryToResize_STR + string.Format(" {0:0,0}", requiredBufferSize) + "MB, Error: " + exception.Message);
				}
				catch (Exception exception)
				{
					ImageProcessing.Misc.bufferSize = Math.Max(ImageProcessing.Misc.minBufferSize, ImageProcessing.Misc.bufferSize / 2);
					throw new Exception(BIPStrings.Resizing2ThereIsNotSufficientMemoryToResize_STR + string.Format("{0:0,0}", requiredBufferSize) + "MB, Error: " + exception.Message);
				}
			}

			return requiredBufferSize;
		}
		#endregion

		#region GetBitmap()
		private static Bitmap GetBitmap(int bitmapWidth, int bitmapHeight, PixelFormat pixelFormat)
		{
			try
			{
				return new Bitmap(bitmapWidth, bitmapHeight, pixelFormat);
			}
			catch (Exception ex)
			{
				long totalAllocatedMemory = GC.GetTotalMemory(false);
				throw new Exception(BIPStrings.ResizeCanTAllocateBitmap_STR + "! Width: " + bitmapWidth + " , Height: " + bitmapHeight + ", Pixels: " +
					pixelFormat.ToString() + ", " + BIPStrings.TotalAllocatedMemory_STR + " " + totalAllocatedMemory.ToString() +
					Environment.NewLine + ex.Message, ex);
			}
		}
		#endregion

		#endregion

	}
}
