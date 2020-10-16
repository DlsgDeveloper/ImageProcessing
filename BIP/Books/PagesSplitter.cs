using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing.BigImages;

namespace BIP.Books
{
	public class PagesSplitter
	{
		//events
		public ImageProcessing.ProgressHnd		ProgressChanged;


		#region constructor
		public PagesSplitter()
		{
		}
		#endregion


		// PUBLIC METHODS
		#region public methods

		#region FindPagesSplitter()
		/// <summary>
		/// It finds the middle part of the book and returns the confidence. It works on 3" wide strip in the middle of the image. It creates 10 strips
		/// per inch and compares vertical deltas - less deltas means less objects. It selects the lowest delta strip and tries to expand it a 
		/// little bit to the left and right to get entire bookfold area. Then it picks the middle line between region left and right sides.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="splitterL">in pixels, left side of the middle part</param>
		/// <param name="splitterR">in pixels, right side of the middle part</param>
		/// <returns></returns>
		public double FindPagesSplitter(FileInfo file, out int splitterL, out int splitterR)
		{
			int[,]		verticalDifferences;

			using (ItDecoder itDecoder = new ItDecoder(file.FullName))
			{
				splitterL = (int)(itDecoder.Width / 2 - itDecoder.DpiX * 0.5);
				splitterR = (int)(itDecoder.Width / 2 + itDecoder.DpiX * 0.5);

				//width/height ratio must be <1.2, 1.8>
				if ((itDecoder.Width < itDecoder.Height * 1.2) || (itDecoder.Width > itDecoder.Height * 1.9))
					return 0;
				//book size must be at least 9" x 7"
				if ((itDecoder.Width / (double)itDecoder.DpiX < 9) || (itDecoder.Height / (double) itDecoder.DpiY < 7))
					return 0;
				
				int sourceW = itDecoder.Width;
				int sourceH = itDecoder.Height;

				int dpiX = itDecoder.DpiX;
				int dpiY = itDecoder.DpiY;
				int pixelsPerStripe = dpiX / 10;

				int clipWidth = (int)Math.Max(sourceW * 0.1, 3 * dpiX);
				Rectangle clip = Rectangle.FromLTRB((int)Math.Max(0, (sourceW / 2) - clipWidth / 2), 0, (int)Math.Min(sourceW, (sourceW / 2) + clipWidth / 2), sourceH);
				clip.Width = (clip.Width / pixelsPerStripe) * pixelsPerStripe;

				using (Bitmap bitmap = itDecoder.GetClip(clip))
				{
					verticalDifferences = GetVerticalDifferences(bitmap, pixelsPerStripe);
				}

				double[] verticalDifferencesTop10Percent = GetAverageVerticalDifferenceOfTopXPercent(itDecoder.PixelsFormat, verticalDifferences, 10);
				//double[] verticalDifferencesAll = GetAverageVerticalDifferenceOfTopXPercent(itDecoder.PixelsFormat, verticalDifferences, 100);

				int		minVerticalIndex, minVerticalIndexL, minVerticalIndexR;

				GetRange(verticalDifferencesTop10Percent, out minVerticalIndex, out minVerticalIndexL, out minVerticalIndexR, 1.5);

				int splitBasedOnVerticalDifferences = Convert.ToInt32(clip.X + (minVerticalIndex * pixelsPerStripe) + (pixelsPerStripe / 2.0));
				int splitBasedOnVerticalDifferencesL = Convert.ToInt32(clip.X + (minVerticalIndexL * pixelsPerStripe) + (pixelsPerStripe / 2.0));
				int splitBasedOnVerticalDifferencesR = Convert.ToInt32(clip.X + (minVerticalIndexR * pixelsPerStripe) + (pixelsPerStripe / 2.0));

				splitterL = splitBasedOnVerticalDifferencesL;
				splitterR = splitBasedOnVerticalDifferencesR;
				//splitter = (splitBasedOnVerticalDifferencesL + splitBasedOnVerticalDifferencesR) / 2;

				double median = GetMedian(verticalDifferencesTop10Percent);
				double confidence = Math.Abs(verticalDifferencesTop10Percent[minVerticalIndex] / median) ;
				return 1 - confidence;
			}
		}
		#endregion

		#region FindPagesSplitter()
		/// <summary>
		/// It finds the middle part of the book and returns the confidence. It works on 4" wide strip in the middle of the image. It creates 10 strips
		/// per inch and compares vertical deltas - less deltas means less objects. It selects the lowest delta strip and tries to expand it a 
		/// little bit to the left and right to get entire bookfold area. Then it picks the middle line between region left and right sides.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="splitterL">in pixels, left side of the middle part</param>
		/// <param name="splitterR">in pixels, right side of the middle part</param>
		/// <returns></returns>
		public double FindPagesSplitter(Bitmap source, out int splitterL, out int splitterR)
		{
			int[,] verticalDifferences;

			splitterL = (int)(source.Width / 2 - source.HorizontalResolution * 0.5);
			splitterR = (int)(source.Width / 2 + source.HorizontalResolution * 0.5);

			int sourceW = source.Width;
			int sourceH = source.Height;

			int dpiX = Convert.ToInt32(source.HorizontalResolution);
			int dpiY = Convert.ToInt32(source.VerticalResolution);

			ImageProcessing.PixelsFormat pixelsFormat = ImageProcessing.Transactions.GetPixelsFormat(source.PixelFormat);

			//width/height ratio must be <1.2, 1.8>
			if ((sourceW < sourceH * 1.2) || (sourceW > sourceH * 1.9))
				return 0;
			//book size must be at least 9" x 7"
			if ((sourceW / (double)dpiX < 9) || (sourceH / (double)dpiY < 7))
				return 0;

			int pixelsPerStripe = dpiX / 10;

			Rectangle clip = Rectangle.FromLTRB((int)Math.Max(0, (sourceW / 2) - 1.5 * dpiX), 0, (int)Math.Min(sourceW, (sourceW / 2) + 1.5 * dpiX), sourceH);
			clip.Width = (clip.Width / pixelsPerStripe) * pixelsPerStripe;

			using (Bitmap bitmap = ImageProcessing.ImageCopier.Copy(source, clip))
			{
				verticalDifferences = GetVerticalDifferences(bitmap, pixelsPerStripe);
			}

			double[] verticalDifferencesTop10Percent = GetAverageVerticalDifferenceOfTopXPercent(pixelsFormat, verticalDifferences, 10);
			//double[] verticalDifferencesAll = GetAverageVerticalDifferenceOfTopXPercent(pixelsFormat, verticalDifferences, 100);

			int minVerticalIndex, minVerticalIndexL, minVerticalIndexR;

			GetRange(verticalDifferencesTop10Percent, out minVerticalIndex, out minVerticalIndexL, out minVerticalIndexR, 1.5);

			int splitBasedOnVerticalDifferences = Convert.ToInt32(clip.X + (minVerticalIndex * pixelsPerStripe) + (pixelsPerStripe / 2.0));
			int splitBasedOnVerticalDifferencesL = Convert.ToInt32(clip.X + (minVerticalIndexL * pixelsPerStripe) + (pixelsPerStripe / 2.0));
			int splitBasedOnVerticalDifferencesR = Convert.ToInt32(clip.X + (minVerticalIndexR * pixelsPerStripe) + (pixelsPerStripe / 2.0));

/*#if DEBUG
				DrawResult(file, itDecoder, splitBasedOnVerticalDifferences, splitBasedOnVerticalDifferencesL, splitBasedOnVerticalDifferencesR);
#endif*/

			splitterL = splitBasedOnVerticalDifferencesL;
			splitterR = splitBasedOnVerticalDifferencesR;

			double median = GetMedian(verticalDifferencesTop10Percent);
			double confidence = Math.Abs(verticalDifferencesTop10Percent[minVerticalIndex] / median);
			return 1 - confidence;
		}
		#endregion
	
		#region FindPagesSplitter2()
		/// <summary>
		/// </summary>
		/// <param name="file"></param>
		/// <param name="splitterL">in pixels, left side of the middle part</param>
		/// <param name="splitterR">in pixels, right side of the middle part</param>
		/// <returns></returns>
		public double FindPagesSplitter2(FileInfo file, out int splitterL, out int splitterR)
		{
			int[,] verticalDifferences;

			using (ItDecoder itDecoder = new ItDecoder(file.FullName))
			{
				splitterL = (int)(itDecoder.Width / 2 - itDecoder.DpiX * 0.5);
				splitterR = (int)(itDecoder.Width / 2 + itDecoder.DpiX * 0.5);

				//width/height ratio must be <1.2, 1.8>
				if ((itDecoder.Width < itDecoder.Height * 1.2) || (itDecoder.Width > itDecoder.Height * 1.9))
					return 0;
				//book size must be at least 9" x 7"
				if ((itDecoder.Width / (double)itDecoder.DpiX < 9) || (itDecoder.Height / (double)itDecoder.DpiY < 7))
					return 0;

				int sourceW = itDecoder.Width;
				int sourceH = itDecoder.Height;

				int		dpiX = itDecoder.DpiX;
				int		dpiY = itDecoder.DpiY;
				int		pixelsPerStripe = dpiX / 10;

				int			clipWidth = (int)Math.Max(sourceW * 0.1, 3 * dpiX);
				Rectangle	clip = Rectangle.FromLTRB((int)Math.Max(0, (sourceW / 2) - clipWidth / 2), 0, (int)Math.Min(sourceW, (sourceW / 2) + clipWidth / 2), sourceH);
				clip.Width = (clip.Width / pixelsPerStripe) * pixelsPerStripe;

				using (Bitmap bitmap = itDecoder.GetClip(clip))
				{
					verticalDifferences = GetVerticalDifferences2(bitmap, pixelsPerStripe);
				}

				double[]	verticalDifferencesTop10Percent = GetAverageVerticalDifferenceOfTopXPercent(itDecoder.PixelsFormat, verticalDifferences, 10);				
				bool[]		possibleBookfoldValues = GetPossibleBookfoldArray(verticalDifferencesTop10Percent);
				int			minVerticalIndex, minVerticalIndexL, minVerticalIndexR;

/*#if DEBUG
				Console.WriteLine();
				Console.WriteLine(file.Name);

				for (int i = 0; i < verticalDifferencesTop10Percent.Length; i++)
					Console.WriteLine(verticalDifferencesTop10Percent[i]);
#endif*/

				GetRange(verticalDifferencesTop10Percent, possibleBookfoldValues, out minVerticalIndex, out minVerticalIndexL, out minVerticalIndexR, 1.5);

				int splitBasedOnVerticalDifferences = Convert.ToInt32(clip.X + (minVerticalIndex * pixelsPerStripe) + (pixelsPerStripe / 2.0));
				int splitBasedOnVerticalDifferencesL = Convert.ToInt32(clip.X + (minVerticalIndexL * pixelsPerStripe) + (pixelsPerStripe / 2.0));
				int splitBasedOnVerticalDifferencesR = Convert.ToInt32(clip.X + (minVerticalIndexR * pixelsPerStripe) + (pixelsPerStripe / 2.0));

				splitterL = splitBasedOnVerticalDifferencesL;
				splitterR = splitBasedOnVerticalDifferencesR;

				double median = GetMedian(verticalDifferencesTop10Percent);
				double confidence = Math.Abs(verticalDifferencesTop10Percent[minVerticalIndex] / median);
				return 1 - confidence;
			}
		}
		#endregion

		#region static FindPagesSplitterStatic()
		/// <summary>
		/// It finds the middle part of the book and returns the confidence. It works on 4" wide strip in the middle of the image. It creates 10 strips
		/// per inch and compares vertical deltas - less deltas means less objects. It selects the lowest delta strip and tries to expand it a 
		/// little bit to the left and right to get entire bookfold area. Then it picks the middle line between region left and right sides.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="splitterL">in pixels, left side of the middle part</param>
		/// <param name="splitterR">in pixels, right side of the middle part</param>
		/// <returns></returns>
		public static double FindPagesSplitterStatic(FileInfo file, out int splitterL, out int splitterR)
		{
			PagesSplitter ps = new PagesSplitter();

			return ps.FindPagesSplitter(file, out splitterL, out splitterR);
		}
		#endregion

		#region static FindPagesSplitterStatic2()
		/// <summary>
		/// It finds the middle part of the book and returns the confidence. It works on 10% or 3" in the middle of the image image. It creates 10 strips
		/// per inch and compares vertical deltas - less deltas means less objects. It selects the lowest delta strip and tries to expand it a 
		/// little bit to the left and right to get entire bookfold area. Then it picks the middle line between region left and right sides.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="splitterL">in pixels, left side of the middle part</param>
		/// <param name="splitterR">in pixels, right side of the middle part</param>
		/// <returns></returns>
		public static double FindPagesSplitterStatic2(FileInfo file, out int splitterL, out int splitterR)
		{
			PagesSplitter ps = new PagesSplitter();

			return ps.FindPagesSplitter2(file, out splitterL, out splitterR);
		}
		#endregion

		#region static FindPagesSplitterStatic()
		/// <summary>
		/// It finds the middle part of the book and returns the confidence. It works on 4" wide strip in the middle of the image. It creates 10 strips
		/// per inch and compares vertical deltas - less deltas means less objects. It selects the lowest delta strip and tries to expand it a 
		/// little bit to the left and right to get entire bookfold area. Then it picks the middle line between region left and right sides.
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="splitterL">in pixels, left side of the middle part</param>
		/// <param name="splitterR">in pixels, right side of the middle part</param>
		/// <returns></returns>
		public static double FindPagesSplitterStatic(Bitmap bitmap, out int splitterL, out int splitterR)
		{
			PagesSplitter ps = new PagesSplitter();

			return ps.FindPagesSplitter(bitmap, out splitterL, out splitterR);
		}
		#endregion

		#region DrawResult()
		public static unsafe void DrawResult(Bitmap bitmap, int splitL, int splitR)
		{
			BitmapData bitmapData = null;

			int sourceW = bitmap.Width;
			int sourceH = bitmap.Height;

			int dpiX = Convert.ToInt32(bitmap.HorizontalResolution);
			int dpiY = Convert.ToInt32(bitmap.VerticalResolution);

			int y;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

				int stride = bitmapData.Stride;
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();

				#region 32 bpp
				if (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format24bppRgb)
				{
					int pixelBytes = (bitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

					for (y = 0; y < sourceH; y++)
					{
						if ((y % 2) == 0)
						{
							pSource[y * stride + splitL * pixelBytes + 0] = 0;
							pSource[y * stride + splitL * pixelBytes + 1] = 0;
							pSource[y * stride + splitL * pixelBytes + 2] = 255;

							pSource[y * stride + splitR * pixelBytes + 0] = 0;
							pSource[y * stride + splitR * pixelBytes + 1] = 255;
							pSource[y * stride + splitR * pixelBytes + 2] = 0;
						}
					}
				}
				#endregion

				#region 8 bpp
				else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					if (ImageProcessing.Misc.IsGrayscale(bitmap))
					{
						for (y = 0; y < sourceH; y++)
						{
							if ((y % 2) == 0)
							{
								pSource[y * stride + splitL - 1] = 255;
								pSource[y * stride + splitL] = 128;
								pSource[y * stride + splitL + 1] = 255;

								pSource[y * stride + splitR - 1] = 255;
								pSource[y * stride + splitR] = 128;
								pSource[y * stride + splitR + 1] = 255;
							}
						}
					}
					else
					{
						Color[] entries = bitmap.Palette.Entries;

						for (y = 0; y < sourceH; y++)
						{
							if ((y % 2) == 0)
							{
								pSource[y * stride + splitL - 1] = (byte)(entries.Length - 1);
								pSource[y * stride + splitL] = 128;
								pSource[y * stride + splitL + 1] = (byte)(entries.Length - 1);

								pSource[y * stride + splitR - 1] = (byte)(entries.Length - 1);
								pSource[y * stride + splitR] = 128;
								pSource[y * stride + splitR + 1] = (byte)(entries.Length - 1);
							}
						}
					}
				}
				#endregion

				#region 1 bpp
				else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					for (y = 0; y < sourceH; y++)
					{
						if ((y % 2) == 0)
						{
							pSource[y * stride + splitL / 8] &= 195;
							pSource[y * stride + splitL / 8] |= 36;

							pSource[y * stride + splitR / 8] &= 195;
							pSource[y * stride + splitR / 8] |= 36;
						}
					}
				}
				#endregion

			}
			finally
			{
				if ((bitmap != null) && (bitmapData != null))
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion
		
		#endregion


		// PRIVATE METHODS
		#region private methods

		#region GetColumnsColors()
		unsafe int[,] GetColumnsGrayShades(Bitmap bitmap, int pixelsPerStripe)
		{
			int columns = bitmap.Width / pixelsPerStripe;
			int[,] columnColors = new int[columns, 256];

			BitmapData bitmapData = null;

			int sourceW = bitmap.Width;
			int sourceH = bitmap.Height;

			int dpiX = Convert.ToInt32(bitmap.HorizontalResolution);
			int dpiY = Convert.ToInt32(bitmap.VerticalResolution);

			int x;
			int y;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				byte* pCurrent;
				double gray;

				int stride = bitmapData.Stride;
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();

				#region 32 bpp
				if (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format24bppRgb)
				{
					int pixelBytes = (bitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

					for (y = 0; y < sourceH; y++)
					{
						pCurrent = (byte*)pSource + y * stride;

						for (int column = 0; column < columns; column++)
						{
							for (x = 0; x < pixelsPerStripe; x++)
							{
								gray = (0.299 * pCurrent[(column * pixelsPerStripe + x) * pixelBytes + 2] + 0.587 * pCurrent[(column * pixelsPerStripe + x) * pixelBytes + 1] + 0.114 * pCurrent[(column * pixelsPerStripe + x) * pixelBytes]);

								columnColors[column, (byte)gray]++;
							}
						}
					}
				}
				#endregion

				#region 8 bpp
				else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					if (ImageProcessing.Misc.IsGrayscale(bitmap))
					{
						for (y = 0; y < sourceH; y++)
						{
							pCurrent = (byte*)pSource + y * stride;

							for (int column = 0; column < columns; column++)
							{
								for (x = 0; x < pixelsPerStripe; x++)
								{
									gray = pCurrent[column * pixelsPerStripe + x];

									columnColors[column, (byte)gray]++;
								}
							}
						}
					}
					else
					{
						Color[] entries = bitmap.Palette.Entries;
						Color c;

						for (y = 0; y < sourceH; y++)
						{
							pCurrent = (byte*)pSource + y * stride;

							for (int column = 0; column < columns; column++)
							{
								for (x = 0; x < pixelsPerStripe; x++)
								{
									c = entries[pCurrent[column * pixelsPerStripe + x]];
									gray = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B);

									columnColors[column, (byte)gray]++;
								}
							}
						}
					}
				}
				#endregion

				#region 1 bpp
				else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					byte g;

					try
					{
						for (y = 0; y < sourceH; y++)
							for (x = 0; x < sourceW; x++)
							{
								g = pSource[y * stride + x / 8];

								for (int i = 0; i < 8; i++)
								{
									if (((g >> i) & 0x1) == 1)
										columnColors[x / pixelsPerStripe, 255]++;
									else
										columnColors[x / pixelsPerStripe, 0]++;
								}
							}
					}
					catch (Exception ex)
					{
						throw ex;
					}
				}
				#endregion

			}
			finally
			{
				if ((bitmap != null) && (bitmapData != null))
					bitmap.UnlockBits(bitmapData);
			}

			return columnColors;
		}
		#endregion

		#region GetAverages()
		double[] GetAverages(int[,] columnColors)
		{
			int			columns = columnColors.GetLength(0);
			double[] averages = new double[columns];

			for (int column = 0; column < columns; column++)
			{
				int sum = 0;
				int count = 0;

				for (int i = 0; i < 256; i++)
				{
					sum += i * columnColors[column, i];
					count += columnColors[column, i];
				}

				averages[column] = sum / (double)count;
			}

			return averages;
		}
		#endregion

		#region GetBackgroundThresholds()
		double[] GetBackgroundThresholds(int[,] columnColors)
		{
			int columns = columnColors.GetLength(0);
			double[] thresholds = new double[columns];

			for (int column = 0; column < columns; column++)
			{
				uint[] array = new uint[256];

				for (int i = 0; i < 256; i++)
					array[i] = (uint) columnColors[column, i];

				thresholds[column] = ImageProcessing.Histogram.GetOtsuBackground(array);
			}

			return thresholds;
		}
		#endregion

		#region GetVerticalDifferences()
		unsafe int[,] GetVerticalDifferences(Bitmap bitmap, int pixelsPerStripe)
		{
			int columns = bitmap.Width / pixelsPerStripe;
			int[,] columnColors = new int[columns, 256];

			BitmapData bitmapData = null;

			int sourceW = bitmap.Width;
			int sourceH = bitmap.Height;

			int dpiX = Convert.ToInt32(bitmap.HorizontalResolution);
			int dpiY = Convert.ToInt32(bitmap.VerticalResolution);

			int x, y;
			/*byte min, max;

			ImageProcessing.Miscelaneous.BitmapMisc.GetMinAndMax(bitmap, out min, out max);*/

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				byte* pCurrent;
				double gray1, gray2;

				int stride = bitmapData.Stride;
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();

				#region 32 and 24 bpp
				if (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format24bppRgb)
				{
					int pixelBytes = (bitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;


					for (y = 0; y < sourceH - 1; y++)
					{
						pCurrent = (byte*)pSource + y * stride;

						for (int column = 0; column < columns; column++)
						{
							for (x = 0; x < pixelsPerStripe; x++)
							{
								gray1 = (0.299 * pCurrent[(column * pixelsPerStripe + x) * pixelBytes + 2] + 0.587 * pCurrent[(column * pixelsPerStripe + x) * pixelBytes + 1] + 0.114 * pCurrent[(column * pixelsPerStripe + x) * pixelBytes]);
								gray2 = (0.299 * pCurrent[stride + (column * pixelsPerStripe + x) * pixelBytes + 2] + 0.587 * pCurrent[stride + (column * pixelsPerStripe + x) * pixelBytes + 1] + 0.114 * pCurrent[stride + (column * pixelsPerStripe + x) * pixelBytes]);

								if (gray1 > gray2)
								{
									columnColors[column, (byte)(gray1 - gray2) ]++;
								}
								else
								{
									columnColors[column, (byte)(gray2 - gray1)]++;
								}
							}
						}
					}
				}
				#endregion

				#region 8 bpp
				else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					if (ImageProcessing.Misc.IsGrayscale(bitmap))
					{
						for (y = 0; y < sourceH - 1; y++)
						{
							pCurrent = (byte*)pSource + y * stride;

							for (int column = 0; column < columns; column++)
							{
								for (x = 0; x < pixelsPerStripe; x++)
								{
									gray1 = pCurrent[column * pixelsPerStripe + x];
									gray2 = pCurrent[stride + column * pixelsPerStripe + x];

									if (gray1 > gray2)
										columnColors[column, (byte)(gray1 - gray2)]++;
									else
										columnColors[column, (byte)(gray2 - gray1)]++;
								}
							}
						}
					}
					else
					{
						Color[] entries = bitmap.Palette.Entries;
						Color c1, c2;

						for (y = 0; y < sourceH; y++)
						{
							pCurrent = (byte*)pSource + y * stride;

							for (int column = 0; column < columns; column++)
							{
								for (x = 0; x < pixelsPerStripe; x++)
								{
									c1 = entries[pCurrent[column * pixelsPerStripe + x]];
									c2 = entries[pCurrent[stride + column * pixelsPerStripe + x]];
									gray1 = (0.299 * c1.R + 0.587 * c1.G + 0.114 * c1.B);
									gray2 = (0.299 * c2.R + 0.587 * c2.G + 0.114 * c2.B);

									if (gray1 > gray2)
										columnColors[column, (byte)(gray1 - gray2)]++;
									else
										columnColors[column, (byte)(gray2 - gray1)]++;
								}
							}
						}
					}
				}
				#endregion

				#region 1 bpp
				else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					byte g1, g2;

					for (y = 0; y < sourceH - 1; y++)
						for (x = 0; x < sourceW; x++)
						{
							g1 = pSource[y * stride + x / 8];
							g2 = pSource[(y + 1) * stride + x / 8];

							for (int i = 0; i < 8; i++)
							{
								if (((g1 >> i) & 0x1) != ((g2 >> i) & 0x1))
									columnColors[x / pixelsPerStripe, 255]++;
								else
									columnColors[x / pixelsPerStripe, 0]++;
							}
						}
				}
				#endregion

			}
			finally
			{
				if ((bitmap != null) && (bitmapData != null))
					bitmap.UnlockBits(bitmapData);
			}

			return columnColors;
		}
		#endregion

		#region GetVerticalDifferences2()
		unsafe int[,] GetVerticalDifferences2(Bitmap bitmap, int pixelsPerStripe)
		{
			int columns = bitmap.Width / pixelsPerStripe;
			int[,] columnColors = new int[columns, 256];

			BitmapData bitmapData = null;

			int sourceW = bitmap.Width;
			int sourceH = bitmap.Height;

			int dpiX = Convert.ToInt32(bitmap.HorizontalResolution);
			int dpiY = Convert.ToInt32(bitmap.VerticalResolution);

			int x, y;
			byte min, max;

			ImageProcessing.Miscelaneous.BitmapMisc.GetMinAndMax(bitmap, out min, out max);
			double[] curve = GetCurve(min, max);

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				byte* pCurrent;
				double gray1, gray2;

				int stride = bitmapData.Stride;
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();

				#region 32 and 24 bpp
				if (bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format24bppRgb)
				{
					int pixelBytes = (bitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;


					for (y = 0; y < sourceH - 1; y++)
					{
						pCurrent = (byte*)pSource + y * stride;

						for (int column = 0; column < columns; column++)
						{
							for (x = 0; x < pixelsPerStripe; x++)
							{
								gray1 = (0.299 * pCurrent[(column * pixelsPerStripe + x) * pixelBytes + 2] + 0.587 * pCurrent[(column * pixelsPerStripe + x) * pixelBytes + 1] + 0.114 * pCurrent[(column * pixelsPerStripe + x) * pixelBytes]);
								gray2 = (0.299 * pCurrent[stride + (column * pixelsPerStripe + x) * pixelBytes + 2] + 0.587 * pCurrent[stride + (column * pixelsPerStripe + x) * pixelBytes + 1] + 0.114 * pCurrent[stride + (column * pixelsPerStripe + x) * pixelBytes]);

								if (gray1 > gray2)
								{
									double difference = Math.Min(255, (gray1 - gray2) / curve[(byte)gray2]);
									columnColors[column, (byte)difference]++;
								}
								else
								{
									double difference = Math.Min(255, (gray2 - gray1) / curve[(byte)gray1]);
									columnColors[column, (byte)difference]++;
								}
							}
						}
					}
				}
				#endregion

				#region 8 bpp
				else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					if (ImageProcessing.Misc.IsGrayscale(bitmap))
					{
						for (y = 0; y < sourceH - 1; y++)
						{
							pCurrent = (byte*)pSource + y * stride;

							for (int column = 0; column < columns; column++)
							{
								for (x = 0; x < pixelsPerStripe; x++)
								{
									gray1 = pCurrent[column * pixelsPerStripe + x];
									gray2 = pCurrent[stride + column * pixelsPerStripe + x];

									if (gray1 > gray2)
										columnColors[column, (byte)(gray1 - gray2)]++;
									else
										columnColors[column, (byte)(gray2 - gray1)]++;
								}
							}
						}
					}
					else
					{
						Color[] entries = bitmap.Palette.Entries;
						Color c1, c2;

						for (y = 0; y < sourceH; y++)
						{
							pCurrent = (byte*)pSource + y * stride;

							for (int column = 0; column < columns; column++)
							{
								for (x = 0; x < pixelsPerStripe; x++)
								{
									c1 = entries[pCurrent[column * pixelsPerStripe + x]];
									c2 = entries[pCurrent[stride + column * pixelsPerStripe + x]];
									gray1 = (0.299 * c1.R + 0.587 * c1.G + 0.114 * c1.B);
									gray2 = (0.299 * c2.R + 0.587 * c2.G + 0.114 * c2.B);

									if (gray1 > gray2)
										columnColors[column, (byte)(gray1 - gray2)]++;
									else
										columnColors[column, (byte)(gray2 - gray1)]++;
								}
							}
						}
					}
				}
				#endregion

				#region 1 bpp
				else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					byte g1, g2;

					for (y = 0; y < sourceH - 1; y++)
						for (x = 0; x < sourceW; x++)
						{
							g1 = pSource[y * stride + x / 8];
							g2 = pSource[(y + 1) * stride + x / 8];

							for (int i = 0; i < 8; i++)
							{
								if (((g1 >> i) & 0x1) != ((g2 >> i) & 0x1))
									columnColors[x / pixelsPerStripe, 255]++;
								else
									columnColors[x / pixelsPerStripe, 0]++;
							}
						}
				}
				#endregion

			}
			finally
			{
				if ((bitmap != null) && (bitmapData != null))
					bitmap.UnlockBits(bitmapData);
			}

			return columnColors;
		}
		#endregion

		#region GetTriplet()
		void GetTriplet(double[] array, out int x1, out int n, out int x2, int arrayMinDistance)
		{
			int arraySize = array.Length;
			
			x1 = 0;
			n = arraySize / 2;
			x2 = arraySize - 1;

			double bestDistance = (array[x1] - array[n]) + (array[x2] - array[n]);


			for (int i = 0; i < arraySize - 2; i++)
			{
				for (int j = i + arrayMinDistance; j < arraySize - 1; j++)
				{
					for (int k = j + arrayMinDistance; k < arraySize; k++)
					{
						double distance = (array[i] - array[j]) + (array[k] - array[j]);

						if (bestDistance < distance)
						{
							bestDistance = distance;
							x1 = i;
							n = j;
							x2 = k;
						}
					}
				}
			}
		}
		#endregion

		#region GetAverageVerticalDifferenceOfTopXPercent()
		double[] GetAverageVerticalDifferenceOfTopXPercent(ImageProcessing.PixelsFormat pixelsFormat, int[,] columnColors, int topPercentage)
		{
			int columns = columnColors.GetLength(0);
			double[] averages = new double[columns];

			if (pixelsFormat != ImageProcessing.PixelsFormat.FormatBlackWhite)
			{
				for (int column = 0; column < columns; column++)
				{
					double count = 0;

					for (int i = 0; i < 256; i++)
						count += columnColors[column, i];

					int partialCount = 0;
					int index = 0;

					for (int i = 255; i >= 0; i--)
					{
						partialCount += columnColors[column, i];

						if ((partialCount / count) >= (topPercentage / 100.0))
						{
							index = i;
							break;
						}
					}

					int sum = 0;
					count = 0;

					for (int i = index; i < 256; i++)
					{
						sum += i * columnColors[column, i];
						count += columnColors[column, i];
					}

					if (count > 0)
						averages[column] = sum / count;
				}
			}
			else
			{
				for (int column = 0; column < columns; column++)
				{
					averages[column] = columnColors[column, 255] / (double)(columnColors[column, 0] + columnColors[column, 255]);
				}
			}

			return averages;
		}
		#endregion

		#region GetMinimumIndex()
		int GetMinimumIndex(double[] array)
		{
			int minIndex = 0;

			for (int i = 1; i < array.Length; i++)
				if (array[minIndex] > array[i])
					minIndex = i;

			return minIndex;
		}
		#endregion

		#region GetMaximumIndex()
		int GetMaximumIndex(double[] array)
		{
			int arraySize = array.Length;

			int max = 0;

			for (int i = 1; i < arraySize; i++)
				if (array[max] < array[i])
					max = i;

			return max;
		}
		#endregion

		#region GetRange()
		void GetRange(double[] array, out int minVerticalIndex, out int minVerticalIndexL, out int minVerticalIndexR, double limitMultiplier)
		{
			int arraySize = array.Length;
			minVerticalIndex = GetMinimumIndex(array);
			minVerticalIndexL = minVerticalIndex;
			minVerticalIndexR = minVerticalIndex;
			
			for (int i = Math.Max(0, minVerticalIndex - 20); i < minVerticalIndex; i++)
			{
				if (array[minVerticalIndex] * limitMultiplier >= array[i] || (array[minVerticalIndex] + 10 >= array[i]))
				{
					minVerticalIndexL = i;
					break;
				}
			}

			for (int i = Math.Min(minVerticalIndex + 20, arraySize - 1); i > minVerticalIndex; i--)
			{
				if (array[minVerticalIndex] * limitMultiplier >= array[i] || (array[minVerticalIndex] + 10 >= array[i]))
				{
					minVerticalIndexR = i;
					break;
				}
			}
		}
		#endregion

		#region GetRange()
		void GetRange(double[] array, bool[] allowedValues, out int minVerticalIndex, out int minVerticalIndexL, out int minVerticalIndexR, double limitMultiplier)
		{
			int arraySize = array.Length;
			minVerticalIndex = GetMinimumIndex(array);
			minVerticalIndexL = minVerticalIndex;
			minVerticalIndexR = minVerticalIndex;

			for (int i = minVerticalIndex - 1; i >= 0; i--)
			{
				if (allowedValues[i] == false)
				{
					minVerticalIndexL = i + 1;
					break;
				}
			}

			for (int i = minVerticalIndex + 1; i < allowedValues.Length; i++)
			{
				if (allowedValues[i] == false)
				{
					minVerticalIndexR = i - 1;
					break;
				}
			}
		}
		#endregion

		#region GetMedian()
		double GetMedian(double[] array)
		{
			List<double> list = new List<double>(array);

			list.Sort();

			return list[list.Count / 2];
		}
		#endregion

		#region GetPossibleBookfoldArray()
		bool[] GetPossibleBookfoldArray(double[] verticalDifferences)
		{
			int			arrayLenght = verticalDifferences.Length;
			bool[]		possibleBookfoldValues = new bool[arrayLenght];

			double min = verticalDifferences.Min();
			double max = verticalDifferences.Max();
			double threshold = min + (max - min) / 4.0;

			//for (int i = 0; i < arrayLenght; i++)
			//	possibleBookfoldValues[i] = (verticalDifferences[i] <= threshold);

			List<double> list = new List<double>();

			for (int i = 0; i < arrayLenght; i++)
				if (verticalDifferences[i] < threshold)
					list.Add(verticalDifferences[i]);

			double median = ImageProcessing.Misc.GetMedianValue(list);

			for (int i = 0; i < arrayLenght; i++)
				possibleBookfoldValues[i] = (verticalDifferences[i] <= (median * 2));

			return possibleBookfoldValues;
		}
		#endregion

		#region GetCurve()
		/// <summary>
		/// 1 / x
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public static double[] GetCurve(int min, int max)
		{
			double[] curve = new double[256];
			double x = min + (max - min) / 2.0;
			double maxLocal = 0;

			for (int i = 0; i <= min; i++)
				curve[i] = 0;

		
			for (int i = min + 1; i < max; i++)
				curve[i] = Math.Atan(i / 128.0);

			for (int i = min + 1; i < max; i++)
				if (maxLocal < curve[i])
					maxLocal = curve[i];

			for (int i = min + 1; i < max; i++)
				curve[i] = curve[i]  / maxLocal;

	
			for (int i = max; i <= 255; i++)
				curve[i] = 1;

			return curve;
		}
		#endregion

		#endregion

	}
}
