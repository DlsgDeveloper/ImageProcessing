using System;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing.Languages;

namespace ImageProcessing.BitmapOperations
{
	public class ResizingDisproportional
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

		#region Resize()
		/// <summary>
		/// unproportional. Not tested for bitonal images
		/// </summary>
		/// <param name="bmpSource"></param>
		/// <param name="desiredWidth"></param>
		/// <param name="desiredHeight"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public static Bitmap Resize(Bitmap bmpSource, int desiredWidth, int desiredHeight, ResizeMode mode)
		{
			if (bmpSource == null)
				return null;

			Rectangle clip = new Rectangle(0, 0, bmpSource.Width, bmpSource.Height);

			if (bmpSource.Width == desiredWidth && bmpSource.Height == desiredHeight)
				return ImageCopier.Copy(bmpSource, clip);

			Bitmap bmpResult = null;

			switch (bmpSource.PixelFormat)
			{
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:
				case PixelFormat.Format24bppRgb:
				case PixelFormat.Format8bppIndexed:
					{
						if (mode == ResizeMode.Fast)
							bmpResult = UniversalFast(bmpSource, desiredWidth, desiredHeight);
						else
							bmpResult = UniversalQuality(bmpSource, desiredWidth, desiredHeight);
					}
					break;
				case PixelFormat.Format1bppIndexed: 
					if (bmpSource.Width > desiredWidth)
						bmpResult = Shorten1bpp(bmpSource, desiredWidth, desiredHeight);
					else
						bmpResult = Extend1bpp(bmpSource, desiredWidth, desiredHeight);
					break;
				default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}

			if (bmpResult != null)
			{
				Misc.SetBitmapResolution(bmpResult, (float)(bmpSource.HorizontalResolution), (float)(bmpSource.VerticalResolution));

				if (bmpSource.Palette != null && bmpSource.PixelFormat == PixelFormat.Format8bppIndexed && bmpSource.Palette.Entries.Length > 0)
					bmpResult.Palette = bmpSource.Palette;
			}

			return bmpResult;
		}
		#endregion
	
		#endregion

		//PRIVATE METHODS
		#region private methods

		#region UniversalFast()
		private static Bitmap UniversalFast(Bitmap source, int desiredWidth, int desiredHeight)
		{
			if (source == null)
				return null;

			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y;

			try
			{
				int sourceW = source.Width;
				int sourceH = source.Height;
				int resultW = desiredWidth;
				int resultH = desiredHeight;

				result = GetBitmap(resultW, resultH, source.PixelFormat);

				if (source.Palette != null && source.Palette.Entries.Length > 0)
					result.Palette = Misc.GetColorPalette(source.Palette.Entries);

				sourceData = source.LockBits(new Rectangle(0, 0, sourceW, sourceH), ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, resultW, resultH), ImageLockMode.WriteOnly, result.PixelFormat);

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;
				//double r, g, b;

				double yFrom;
				int yStride;

				double zoomX = sourceW / (double)resultW;
				double zoomY = sourceH / (double)resultH;
				double zoom2 = zoomX * zoomY;

				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();

					#region 8bpp
					if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < resultH; y++)
						{
							yFrom = zoomY * y;
							yStride = (int)yFrom * strideS;

							for (x = 0; x < resultW; x++)
								scan0R[y * strideR + x] = scan0S[yStride + (int)(zoomX * x)];
						}
					}
					#endregion

					#region 24bpp
					if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < resultH; y++)
						{
							yFrom = zoomY * y;
							yStride = (int)yFrom * strideS;

							for (x = 0; x < resultW; x++)
							{
								scan0R[y * strideR + x * 3] = scan0S[yStride + (int)(zoomX * x) * 3];
								scan0R[y * strideR + x * 3 + 1] = scan0S[yStride + (int)(zoomX * x) * 3 + 1];
								scan0R[y * strideR + x * 3 + 2] = scan0S[yStride + (int)(zoomX * x) * 3 + 2];
							}
						}
					}
					#endregion

					#region 32bpp
					if (source.PixelFormat == PixelFormat.Format32bppArgb)
					{
						for (y = 0; y < resultH; y++)
						{
							yFrom = zoomY * y;
							yStride = (int)yFrom * strideS;

							for (x = 0; x < resultW; x++)
							{
								scan0R[y * strideR + x * 4] = scan0S[yStride + (int)(zoomX * x) * 4];
								scan0R[y * strideR + x * 4 + 1] = scan0S[yStride + (int)(zoomX * x) * 4 + 1];
								scan0R[y * strideR + x * 4 + 2] = scan0S[yStride + (int)(zoomX * x) * 4 + 2];
								scan0R[y * strideR + x * 4 + 3] = scan0S[yStride + (int)(zoomX * x) * 4 + 3];
							}
						}
					}
					#endregion
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

		#region UniversalQuality()
		private static Bitmap UniversalQuality(Bitmap source, int desiredWidth, int desiredHeight)
		{
			if (source == null)
				return null;

			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y;

			try
			{
				int sourceW = source.Width;
				int sourceH = source.Height;
				int resultW = desiredWidth;
				int resultH = desiredHeight;

				result = GetBitmap(resultW, resultH, source.PixelFormat);

				if (source.Palette != null && source.Palette.Entries.Length > 0)
					result.Palette = Misc.GetColorPalette(source.Palette.Entries);

				sourceData = source.LockBits(new Rectangle(0, 0, sourceW, sourceH), ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, resultW, resultH), ImageLockMode.WriteOnly, result.PixelFormat);

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;
				//double r, g, b;

				double xFrom, yFrom, xTo, yTo;

				double xPixelPortion, yPixelPortion;
				double r, g, b, a;

				double zoomX = sourceW / (double)resultW;
				double zoomY = sourceH / (double)resultH;
				double zoom2 = zoomX * zoomY;

				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();

					#region 8bpp
					if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < resultH; y++)
						{
							yFrom = zoomY * y;
							yTo = (yFrom + zoomY < sourceH) ? (yFrom + zoomY) : sourceH;

							for (x = 0; x < resultW; x++)
							{
								g = 0;
								
								xFrom = zoomX * x;
								xTo = (xFrom + zoomX < sourceW) ? (xFrom + zoomX) : sourceW;

								for (int yFromInt = (int)yFrom; yFromInt < yTo; yFromInt++)
									for (int xFromInt = (int)xFrom; xFromInt < xTo; xFromInt++)
									{
										//first row
										if (xFromInt == (int)xFrom)
										{
											if (xTo >= xFromInt + 1)
												xPixelPortion = 1 - (xFrom - xFromInt);
											else
												xPixelPortion = (xTo - xFrom);
										}
										else
											xPixelPortion = (xTo - xFromInt > 1) ? 1 : xTo - xFromInt;
										
										//firstColumn
										if (yFromInt == (int)yFrom)
										{
											if (yTo >= yFromInt + 1)
												yPixelPortion = 1 - (yFrom - yFromInt);
											else
												yPixelPortion = (yTo - yFrom);
										}
										else
											yPixelPortion = (yTo - yFromInt > 1) ? 1 : yTo - yFromInt;


										g += scan0S[yFromInt * strideS + xFromInt] * xPixelPortion * yPixelPortion;
									}

								scan0R[y * strideR + x] = Convert.ToByte(g / zoom2);
							}
						}
					}
					#endregion

					#region 24bpp
					if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < resultH; y++)
						{
							yFrom = zoomY * y;
							yTo = (yFrom + zoomY < sourceH) ? (yFrom + zoomY) : sourceH;

							for (x = 0; x < resultW; x++)
							{
								r = g = b = 0;

								xFrom = zoomX * x;
								xTo = (xFrom + zoomX < sourceW) ? (xFrom + zoomX) : sourceW;

								for (int yFromInt = (int)yFrom; yFromInt < yTo; yFromInt++)
									for (int xFromInt = (int)xFrom; xFromInt < xTo; xFromInt++)
									{
										//first row
										if (xFromInt == (int)xFrom)
										{
											if (xTo >= xFromInt + 1)
												xPixelPortion = 1 - (xFrom - xFromInt);
											else
												xPixelPortion = (xTo - xFrom);
										}
										else
											xPixelPortion = (xTo - xFromInt > 1) ? 1 : xTo - xFromInt;

										//firstColumn
										if (yFromInt == (int)yFrom)
										{
											if (yTo >= yFromInt + 1)
												yPixelPortion = 1 - (yFrom - yFromInt);
											else
												yPixelPortion = (yTo - yFrom);
										}
										else
											yPixelPortion = (yTo - yFromInt > 1) ? 1 : yTo - yFromInt;


										b += scan0S[yFromInt * strideS + xFromInt * 3] * xPixelPortion * yPixelPortion;
										g += scan0S[yFromInt * strideS + xFromInt * 3 + 1] * xPixelPortion * yPixelPortion;
										r += scan0S[yFromInt * strideS + xFromInt * 3 + 2] * xPixelPortion * yPixelPortion;
									}

								scan0R[y * strideR + x * 3] = (byte)(b / zoom2);
								scan0R[y * strideR + x * 3 + 1] = (byte)(g / zoom2);
								scan0R[y * strideR + x * 3 + 2] = (byte)(r / zoom2);
							}
						}
					}
					#endregion

					#region 32bpp
					if (source.PixelFormat == PixelFormat.Format32bppArgb)
					{
						for (y = 0; y < resultH; y++)
						{
							yFrom = zoomY * y;
							yTo = (yFrom + zoomY < sourceH) ? (yFrom + zoomY) : sourceH;

							for (x = 0; x < resultW; x++)
							{
								r = g = b = a = 0;

								xFrom = zoomX * x;
								xTo = (xFrom + zoomX < sourceW) ? (xFrom + zoomX) : sourceW;

								for (int yFromInt = (int)yFrom; yFromInt < yTo; yFromInt++)
									for (int xFromInt = (int)xFrom; xFromInt < xTo; xFromInt++)
									{
										//first row
										if (xFromInt == (int)xFrom)
										{
											if (xTo >= xFromInt + 1)
												xPixelPortion = 1 - (xFrom - xFromInt);
											else
												xPixelPortion = (xTo - xFrom);
										}
										else
											xPixelPortion = (xTo - xFromInt > 1) ? 1 : xTo - xFromInt;

										//firstColumn
										if (yFromInt == (int)yFrom)
										{
											if (yTo >= yFromInt + 1)
												yPixelPortion = 1 - (yFrom - yFromInt);
											else
												yPixelPortion = (yTo - yFrom);
										}
										else
											yPixelPortion = (yTo - yFromInt > 1) ? 1 : yTo - yFromInt;


										b += scan0S[yFromInt * strideS + xFromInt * 4] * xPixelPortion * yPixelPortion;
										g += scan0S[yFromInt * strideS + xFromInt * 4 + 1] * xPixelPortion * yPixelPortion;
										r += scan0S[yFromInt * strideS + xFromInt * 4 + 2] * xPixelPortion * yPixelPortion;
										a += scan0S[yFromInt * strideS + xFromInt * 4 + 3] * xPixelPortion * yPixelPortion;
									}

								scan0R[y * strideR + x * 4] = (byte)(b / zoom2);
								scan0R[y * strideR + x * 4 + 1] = (byte)(g / zoom2);
								scan0R[y * strideR + x * 4 + 2] = (byte)(r / zoom2);
								scan0R[y * strideR + x * 4 + 3] = (byte)(a / zoom2);
							}
						}
					}
					#endregion

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

		#region Shorten1bpp()
		private static Bitmap Shorten1bpp(Bitmap source, int desiredWidth, int desiredHeight)
		{
			if (source == null)
				return null;

			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y, i;

			try
			{
				int sourceW = source.Width;
				int sourceH = source.Height;
				int resultW = desiredWidth;
				int resultH = desiredHeight;

				result = new Bitmap(resultW, resultH, source.PixelFormat);

				sourceData = source.LockBits(new Rectangle(0, 0, sourceW, sourceH), ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, resultW, resultH), ImageLockMode.WriteOnly, result.PixelFormat);

				double sBlockLengthX = sourceW / (double)resultW;
				double sBlockLengthY = sourceH / (double)resultH;

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

					for (i = 0; i < resultW; i++)
						transformTable[i] = (int)(i * sBlockLengthX);

					for (y = 0; y < resultH; y++)
					{
						sourceY = (int)(y * sBlockLengthY) * strideS;
						sLine = new int[strideS * 8];

						for (i = 0; i < sourceW; i += 8)
						{
							gray = scan0S[sourceY + i / 8];

							if ((gray & 0x80) != 0)
								sLine[i] = 1;
							if ((gray & 0x40) != 0)
								sLine[i + 1] = 1;
							if ((gray & 0x20) != 0)
								sLine[i + 2] = 1;
							if ((gray & 0x10) != 0)
								sLine[i + 3] = 1;
							if ((gray & 0x8) != 0)
								sLine[i + 4] = 1;
							if ((gray & 0x4) != 0)
								sLine[i + 5] = 1;
							if ((gray & 0x2) != 0)
								sLine[i + 6] = 1;
							if ((gray & 0x1) != 0)
								sLine[i + 7] = 1;
						}

						for (x = 0; x < resultW - 8; x += 8)
						{
							scan0R[y * strideR + (x >> 3)] = (byte)(
								sLine[transformTable[x]] << 7 |
								sLine[transformTable[x + 1]] << 6 |
								sLine[transformTable[x + 2]] << 5 |
								sLine[transformTable[x + 3]] << 4 |
								sLine[transformTable[x + 4]] << 3 |
								sLine[transformTable[x + 5]] << 2 |
								sLine[transformTable[x + 6]] << 1 |
								sLine[transformTable[x + 7]]
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

		#region Extend1bpp()
		private static Bitmap Extend1bpp(Bitmap source, int desiredWidth, int desiredHeight)
		{
			if (source == null)
				return null;

			Bitmap result = null;

			BitmapData sourceData = null;
			BitmapData resultData = null;

			int x, y, i, j;

			try
			{
				int sourceW = source.Width;
				int sourceH = source.Height;
				int resultW = desiredWidth;
				int resultH = desiredHeight;

				result = new Bitmap(resultW, resultH, source.PixelFormat);

				sourceData = source.LockBits(new Rectangle(0, 0, sourceW, sourceH), ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, resultW, resultH), ImageLockMode.WriteOnly, result.PixelFormat);

				double sBlockLengthX = sourceW / (double)resultW;
				double sBlockLengthY = sourceH / (double)resultH;
				int limit;

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				unsafe
				{
					byte* scan0S = (byte*)sourceData.Scan0.ToPointer();
					byte* scan0R = (byte*)resultData.Scan0.ToPointer();

					int[] transformTableX = new int[sourceW];
					int[] transformTableY = new int[sourceH];
					int whitePixelsCount;

					for (i = transformTableX.Length - 1; i >= 0; i--)
						transformTableX[i] = (int)(i * sBlockLengthX);
					
					for (i = transformTableY.Length - 1; i >= 0; i--)
						transformTableY[i] = (int)(i * sBlockLengthY);

					for (y = 0; y < resultH - 1; y++)
					{
						for (x = 0; x < resultW - 1; x++)
						{
							whitePixelsCount = 0;

							for (j = transformTableY[y]; j < transformTableY[y] + sBlockLengthY; j++)
								for (i = transformTableX[x]; i < transformTableX[x] + sBlockLengthX; i++)
									if ((scan0S[j * strideS + (i >> 3)] & (0x80 >> (i & 0x07))) > 0)
										whitePixelsCount++;

							if (whitePixelsCount >= (sBlockLengthX * sBlockLengthY * 3 / 4))
								scan0R[y * strideR + (x >> 3)] |= (byte)(0x80 >> (x & 0x07));
						}
					}

					//last row
					y = resultH - 1;
					for (x = 0; x < resultW; x++)
					{
						whitePixelsCount = 0;
						limit = (int)((transformTableX[x] + sBlockLengthX < sourceW) ? transformTableX[x] + sBlockLengthX : sourceW);

						for (j = transformTableY[y]; j < sourceH; j++)
							for (i = transformTableX[x]; i < limit; i++)
								if ((scan0S[j * strideS + (i >> 3)] & (0x80 >> (i & 0x07))) > 0)
									whitePixelsCount++;

						if (whitePixelsCount >= ((limit - transformTableX[x]) * (sourceH - transformTableY[y]) / 2))
							scan0R[y * strideR + (x >> 3)] |= (byte)(0x80 >> (x & 0x07));
					}

					//last column
					x = resultW - 1;
					for (y = 0; y < resultH; y++)
					{
						whitePixelsCount = 0;
						limit = (int)((transformTableY[y] + sBlockLengthY < sourceH) ? transformTableY[y] + sBlockLengthY : sourceH);

						for (j = transformTableY[y]; j < limit; j++)
							for (i = transformTableX[x]; i < sourceW; i++)
								if ((scan0S[j * strideS + (i >> 3)] & (0x80 >> (i & 0x07))) > 0)
									whitePixelsCount++;

						if (whitePixelsCount >= ((sourceW - transformTableX[x]) * (limit - transformTableY[y]) / 2))
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
