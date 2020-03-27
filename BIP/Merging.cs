using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Diagnostics;

namespace ImageProcessing
{
	public class Merging
	{

		//PUBLIC METHODS
		#region public methods

		#region MergeVertically()
		/// <summary>
		/// Bitmaps must be in the same PixelFormat, no validation
		/// </summary>
		/// <param name="bitmaps"></param>
		/// <returns></returns>
		public static Bitmap MergeVertically(List<Bitmap> bitmaps)
		{
			Bitmap result = null;

			switch (bitmaps[0].PixelFormat)
			{
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb: result = MergeVerticallyInternal(bitmaps); break;
				case PixelFormat.Format24bppRgb: result = MergeVerticallyInternal(bitmaps); break;
				case PixelFormat.Format8bppIndexed: result = MergeVerticallyInternal(bitmaps); break;
				case PixelFormat.Format4bppIndexed: result = MergeVerticallyInternal(bitmaps); break;
				case PixelFormat.Format1bppIndexed: result = MergeVerticallyInternal(bitmaps); break;
				default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}

			if (result != null)
			{
				Misc.SetBitmapResolution(result, (float)(bitmaps[0].HorizontalResolution), (float)(bitmaps[0].VerticalResolution));
			}

			return result;
		}
		#endregion

		#region MergeHorizontally()
		/// <summary>
		/// Bitmaps must be in the same PixelFormat, no validation
		/// </summary>
		/// <param name="bitmaps"></param>
		/// <returns></returns>
		public static Bitmap MergeHorizontally(List<Bitmap> bitmaps)
		{			
			Bitmap result = null;

			switch (bitmaps[0].PixelFormat)
			{
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:  
				case PixelFormat.Format24bppRgb: 
				case PixelFormat.Format8bppIndexed: 
				case PixelFormat.Format4bppIndexed: 
				case PixelFormat.Format1bppIndexed: 
					result = MergeHorizontallyInternal(bitmaps); break;
				default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}

			if (result != null)
			{
				Misc.SetBitmapResolution(result, (float)(bitmaps[0].HorizontalResolution), (float)(bitmaps[0].VerticalResolution));
			}

			return result;
		}
		#endregion
	
		#region MergeBitonalAndEdge()
		public static Bitmap MergeBitonalAndEdge(Bitmap bitmap, Bitmap edgeBitmap)
		{
			if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed && edgeBitmap.PixelFormat == PixelFormat.Format1bppIndexed)
			{
				Bitmap result = MergeBitonalAndEdgeInternal(bitmap, edgeBitmap);
				Misc.SetBitmapResolution(result, bitmap.HorizontalResolution, bitmap.VerticalResolution);
				return result;
			}
			else
			{
				throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region MergeVerticallyInternal()
		private unsafe static Bitmap MergeVerticallyInternal(List<Bitmap> bitmaps)
		{
			Bitmap result = null;
			BitmapData resultData = null;

			int resultWidth = int.MaxValue;
			int resultHeight = 0;

			foreach (Bitmap b in bitmaps)
			{
				resultHeight += b.Height;

				if (resultWidth > b.Width)
					resultWidth = b.Width;
			}

			try
			{
				result = new Bitmap(resultWidth, resultHeight, bitmaps[0].PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				int top = 0;
			
				if(bitmaps[0].Palette != null && bitmaps[0].Palette.Entries.Length > 0)
					result.Palette = bitmaps[0].Palette;

				for (int i = 0; i < bitmaps.Count; i++)
				{
					Bitmap source = null;
					BitmapData sourceData = null;
					
					try
					{
						source = bitmaps[i];
						sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

						int width = resultData.Width;
						int height = sourceData.Height;

						int strideS = sourceData.Stride;
						int strideR = resultData.Stride;

						byte* pSource = (byte*)sourceData.Scan0.ToPointer();
						byte* pResult = (byte*)resultData.Scan0.ToPointer();
						byte* pCurrentS, pCurrentR;

						if (result.PixelFormat == PixelFormat.Format32bppArgb || result.PixelFormat == PixelFormat.Format32bppRgb)
						{
							for (int y = 0; y < height; y++)
							{
								pCurrentS = pSource + y * strideS;
								pCurrentR = pResult + (y + top) * strideR;

								for (int x = 0; x < width; x++)
								{
									pCurrentR[0] = pCurrentS[0];
									pCurrentR[1] = pCurrentS[1];
									pCurrentR[2] = pCurrentS[2];
									pCurrentR[3] = pCurrentS[3];

									pCurrentS += 4;
									pCurrentR += 4;
								}
							}
						}
						else if (result.PixelFormat == PixelFormat.Format24bppRgb)
						{
							for (int y = 0; y < height; y++)
							{
								pCurrentS = pSource + y * strideS;
								pCurrentR = pResult + (y + top) * strideR;

								for (int x = 0; x < width; x++)
								{
									pCurrentR[0] = pCurrentS[0];
									pCurrentR[1] = pCurrentS[1];
									pCurrentR[2] = pCurrentS[2];

									pCurrentS += 3;
									pCurrentR += 3;
								}
							}
						}
						else if (result.PixelFormat == PixelFormat.Format8bppIndexed)
						{
							if (Misc.IsGrayscale(result) && Misc.IsGrayscale(source))
							{
								for (int y = 0; y < height; y++)
									for (int x = 0; x < width; x++)
										pResult[(y + top) * strideR + x] = pSource[y * strideS + x];
							}
							else
							{
								Color[] entries = source.Palette.Entries;
								byte[, ,] inversePalette = ImageProcessing.ColorPalettes.PaletteBuilder.GetInversePalette32x32x32(entries);

								for (int y = 0; y < height; y++)
								{
									pCurrentS = pSource + y * strideS;
									pCurrentR = pResult + (y + top) * strideR;

									for (int x = 0; x < width; x++)
									{
										Color c = entries[*pCurrentS++];

										*pCurrentR++ = inversePalette[c.R / 8, c.G / 8, c.B / 8];
									}
								}
							}
						}
						else if (result.PixelFormat == PixelFormat.Format4bppIndexed)
						{
							Color[] entries = source.Palette.Entries;
							byte[, ,] inversePalette = ImageProcessing.ColorPalettes.PaletteBuilder.GetInversePalette32x32x32(entries);
							Color c;

							int x, y;
							for (y = 0; y < height; y++)
							{
								pCurrentS = pSource + y * strideS;
								pCurrentR = pResult + (y + top) * strideR;

								for (x = 0; x < width; x++)
								{
									if ((x % 2) == 0)
									{
										c = entries[pSource[y * strideS + x / 2] >> 4];
										*pCurrentR |= (byte)(inversePalette[c.R / 8, c.G / 8, c.B / 8] << 4);
									}
									else
									{
										c = entries[pSource[y * strideS + x / 2] & 0xF];
										*pCurrentR |= (byte)(inversePalette[c.R / 8, c.G / 8, c.B / 8] & 0xF);
										pCurrentR++;
									}

								}
							}
						}
						else if (result.PixelFormat == PixelFormat.Format1bppIndexed)
						{
							for (int y = 0; y < height; y++)
								for (int x = 0; x < width; x = x + 8)
									pResult[(y + top) * strideR + x / 8] = pSource[y * strideS + x / 8];
						}
						else
							throw new IpException(ErrorCode.ErrorUnsupportedFormat);
					}
					finally
					{
						if (source != null && sourceData != null)
						{
							source.UnlockBits(sourceData);
							sourceData = null;
						}
					}

					top += source.Height;
				}
			}
			finally
			{
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region MergeHorizontallyInternal()
		private unsafe static Bitmap MergeHorizontallyInternal(List<Bitmap> sources)
		{
			Bitmap result = null;
			BitmapData resultData = null;

			BitmapData sourceData = null;

			int resultWidth = 0;
			int resultHeight = sources[0].Height;

			foreach (Bitmap b in sources)
			{
				resultWidth += b.Width;
				resultHeight = Math.Min(resultHeight, b.Height);
			}

			try
			{
				result = new Bitmap(resultWidth, resultHeight, sources[0].PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				if (sources[0].Palette != null && sources[0].Palette.Entries.Length > 0)
					result.Palette = sources[0].Palette;

				int xFrom = 0;

				foreach (Bitmap source in sources)
				{
					try
					{
						sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

						int strideS = sourceData.Stride;
						int strideR = resultData.Stride;

						int widthS = source.Width;

						byte* pSourceS = (byte*)sourceData.Scan0.ToPointer();
						byte* pResult = (byte*)resultData.Scan0.ToPointer();
						byte* pCurrentS, pCurrentR;

						if (result.PixelFormat == PixelFormat.Format32bppArgb || result.PixelFormat == PixelFormat.Format32bppRgb)
						{
							for (int y = 0; y < resultHeight; y++)
							{
								pCurrentS = pSourceS + y * strideS;
								pCurrentR = pResult + y * strideR + (xFrom * 4);

								for (int x = 0; x < widthS; x++)
								{
									pCurrentR[x * 4] = pCurrentS[x * 4];
									pCurrentR[x * 4 + 1] = pCurrentS[x * 4 + 1];
									pCurrentR[x * 4 + 2] = pCurrentS[x * 4 + 2];
									pCurrentR[x * 4 + 3] = pCurrentS[x * 4 + 3];
								}
							}
						}
						else if (result.PixelFormat == PixelFormat.Format24bppRgb)
						{
							for (int y = 0; y < resultHeight; y++)
							{
								pCurrentS = pSourceS + y * strideS;
								pCurrentR = pResult + y * strideR + (xFrom * 3);

								for (int x = 0; x < widthS; x++)
								{
									pCurrentR[x * 3] = pCurrentS[x * 3];
									pCurrentR[x * 3 + 1] = pCurrentS[x * 3 + 1];
									pCurrentR[x * 3 + 2] = pCurrentS[x * 3 + 2];
								}
							}
						}
						else if (result.PixelFormat == PixelFormat.Format8bppIndexed)
						{
							for (int y = 0; y < resultHeight; y++)
							{
								pCurrentS = pSourceS + y * strideS;
								pCurrentR = pResult + y * strideR + xFrom;

								for (int x = 0; x < widthS; x++)
								{
									pCurrentR[x] = pCurrentS[x];
								}
							}
						}
						else if (result.PixelFormat == PixelFormat.Format1bppIndexed)
						{
							for (int y = 0; y < resultHeight; y++)
								for (int x = 0; x < widthS; x = x + 8)
									pResult[y * strideR + (x + xFrom) / 8] = pSourceS[y * strideS + x / 8];
						}
						else
							throw new IpException(ErrorCode.ErrorUnsupportedFormat);
					}
					finally
					{
						if (sourceData != null)
						{
							source.UnlockBits(sourceData);
							sourceData = null;
						}
					}

					xFrom += source.Width;
				}
			}
			finally
			{
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region MergeBitonalAndEdgeInternal()
		private unsafe static Bitmap MergeBitonalAndEdgeInternal(Bitmap bitmap, Bitmap edgeBitmap)
		{
			Bitmap result = null;
			BitmapData resultData = null;
			BitmapData b1Data = null;
			BitmapData b2Data = null;

			int width = Math.Min(bitmap.Width, edgeBitmap.Width);
			int height = Math.Min(bitmap.Height, edgeBitmap.Height);

			try
			{
				result = new Bitmap(width, height, PixelFormat.Format1bppIndexed);
				resultData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, result.PixelFormat);
				b1Data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				b2Data = edgeBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, edgeBitmap.PixelFormat);

				int stride1 = b1Data.Stride;
				int stride2 = b2Data.Stride;
				int strideR = resultData.Stride;

				byte* pSource1 = (byte*)b1Data.Scan0.ToPointer();
				byte* pSource2 = (byte*)b2Data.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				byte* pCurrent1, pCurrent2, pCurrentR;


				for (int y = 0; y < height; y++)
				{
					pCurrent1 = pSource1 + y * stride1;
					pCurrent2 = pSource2 + y * stride2;
					pCurrentR = pResult + y * strideR;

					for (int x = 0; x < strideR; x++)
					{
						pCurrentR[x] = (byte)(pCurrent1[x] & ((~((uint)pCurrent2[x])) & 0xFF));
					}
				}

			}
			finally
			{
				if (b1Data != null)
					bitmap.UnlockBits(b1Data);
				if (b2Data != null)
					edgeBitmap.UnlockBits(b2Data);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion
	
		#endregion

	}
}
