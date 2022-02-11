using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ImageProcessing
{
	public static class UnsharpMask
	{
		// PUBLIC METHODS
		#region public methods

		#region UnsharpGaussian3x3()
		/// <summary>
		/// needs performance optimalization
		/// </summary>
		/// <param name="sourceBitmap"></param>
		/// <param name="factor"></param>
		/// <returns></returns>
		public static Bitmap UnsharpGaussian3x3(Bitmap sourceBitmap, float factor = 1.0f)
		{
			Bitmap resultBitmap;

			using (Bitmap blurBitmap = GetConvolutionFilter(sourceBitmap, UnsharpMaskMatrix.Gaussian3x3, 1.0 / 16.0))
			{
#if DEBUG
				blurBitmap.Save(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\Sharpening\Gaussian3x3.png", ImageFormat.Png);
#endif

				resultBitmap = SubtractAddFactorImage(sourceBitmap, blurBitmap, factor);
			}

			resultBitmap.SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);

			return resultBitmap;
		}
		#endregion

		#region UnsharpGaussian5x5()
		/// <summary>
		///  needs performance optimalization
		/// </summary>
		/// <param name="sourceBitmap"></param>
		/// <param name="factor"></param>
		/// <returns></returns>
		public static Bitmap UnsharpGaussian5x5(Bitmap sourceBitmap, float factor = 1.0f)
		{
			Bitmap resultBitmap;

			using (Bitmap blurBitmap = GetConvolutionFilter(sourceBitmap, UnsharpMaskMatrix.Gaussian5x5Type1, 1.0 / 159.0))
			{
#if DEBUG
				blurBitmap.Save(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\Sharpening\Gaussian5x5Type1.png", ImageFormat.Png);
#endif
				resultBitmap = SubtractAddFactorImage(sourceBitmap, blurBitmap, factor);
			}

			resultBitmap.SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);

			return resultBitmap;
		}
		#endregion

		#region UnsharpMean3x3()
		public static Bitmap UnsharpMean3x3(Bitmap sourceBitmap, double factor = 1.0)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			Bitmap resultBitmap = GetConvolutionFilterUnsharpMean3x3(sourceBitmap, factor, 0);

			resultBitmap.SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);

#if DEBUG
			Console.WriteLine("GetConvolutionFilter(): " + DateTime.Now.Subtract(start).ToString());
#endif

			/*Bitmap blurBitmap = GetConvolutionFilter(sourceBitmap, UnsharpMaskMatrix.Mean3x3, 1.0 / 9.0);

#if DEBUG
			Console.WriteLine("GetConvolutionFilter(): " + DateTime.Now.Subtract(start).ToString());
			blurBitmap.Save(@"c:\delete\del\BlurBitmap.png", ImageFormat.Png);
			start = DateTime.Now;
#endif

			Bitmap resultBitmap = SubtractAddFactorImage(sourceBitmap, blurBitmap, factor);

#if DEBUG
			Console.WriteLine("SubtractAddFactorImage(): " + DateTime.Now.Subtract(start).ToString());
#endif*/

			return resultBitmap;
		}
		#endregion

		#region UnsharpMean5x5()
		/// <summary>
		///  needs performance optimalization
		/// </summary>
		/// <param name="sourceBitmap"></param>
		/// <param name="factor"></param>
		/// <returns></returns>
		public static Bitmap UnsharpMean5x5(this Bitmap sourceBitmap, float factor = 1.0f)
		{
			Bitmap resultBitmap;

			using (Bitmap blurBitmap = GetConvolutionFilter(sourceBitmap, UnsharpMaskMatrix.Mean5x5, 1.0 / 25.0))
			{
				resultBitmap = SubtractAddFactorImage(sourceBitmap, blurBitmap, factor);
			}

			resultBitmap.SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);

			return resultBitmap;
		}
		#endregion

		#endregion


		// PRIVATE METHODS
		#region private methods

		#region GetConvolutionFilterUnsharpMean3x3()
		private static unsafe Bitmap GetConvolutionFilterUnsharpMean3x3(Bitmap sourceBitmap, double factor = 1, int bias = 0)
		{
			BitmapData sourceData = null;
			Bitmap resultBitmap = null;
			BitmapData resultData = null;

			try
			{
				int width = sourceBitmap.Width;
				int height = sourceBitmap.Height;

				sourceData = sourceBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
				resultBitmap = new Bitmap(width, height, sourceData.PixelFormat);
				resultData = resultBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

				double blue = 0.0;
				double green = 0.0;
				double red = 0.0;

				int byteOffset = 0;
				int x, y;

				byte* scan0Source = (byte*)sourceData.Scan0.ToPointer();
				byte* scan0Result = (byte*)resultData.Scan0.ToPointer();

				int strideS = sourceData.Stride;
				int heightMinus1 = height - 1;
				int widthMinus1 = width - 1;
				int Xx3;
				double divider = 1 / 9.0;

				try
				{
					for (y = 1; y < heightMinus1; y++)
					{
						byte* pS = scan0Source + strideS * y;

						for (x = 1; x < widthMinus1; x++)
						{
							byteOffset = y * strideS + x * 3;
							Xx3 = x * 3;

							blue = (double)(pS[-strideS + Xx3 - 3] + pS[-strideS + Xx3] + pS[-strideS + Xx3 + 3] + pS[Xx3 - 3] + pS[Xx3] + pS[Xx3 + 3] + pS[strideS + Xx3 - 3] + pS[strideS + Xx3] + pS[strideS + Xx3 + 3]);
							green = (double)(pS[-strideS + Xx3 - 2] + pS[-strideS + Xx3 + 1] + pS[-strideS + Xx3 + 4] + pS[Xx3 - 2] + pS[Xx3 + 1] + pS[Xx3 + 4] + pS[strideS + Xx3 - 2] + pS[strideS + Xx3 + 1] + pS[strideS + Xx3 + 4]);
							red = (double)(pS[-strideS + Xx3 - 1] + pS[-strideS + Xx3 + 2] + pS[-strideS + Xx3 + 5] + pS[Xx3 - 1] + pS[Xx3 + 2] + pS[Xx3 + 5] + pS[strideS + Xx3 - 1] + pS[strideS + Xx3 + 2] + pS[strideS + Xx3 + 5]);

							blue = divider * blue + bias;
							green = divider * green + bias;
							red = divider * red + bias;

							if ((pS[Xx3] + pS[Xx3 + 1] + pS[Xx3 + 2] - blue - green - red) < 0)
							{
								blue = pS[Xx3] + (pS[Xx3] - blue) * factor;
								green = pS[Xx3 + 1] + (pS[Xx3 + 1] - green) * factor;
								red = pS[Xx3 + 2] + (pS[Xx3 + 2] - red) * factor;

								scan0Result[byteOffset] = (byte)((blue < 0) ? 0 : ((blue > 255) ? 255 : blue));
								scan0Result[byteOffset + 1] = (byte)((green < 0) ? 0 : ((green > 255) ? 255 : green));
								scan0Result[byteOffset + 2] = (byte)((red < 0) ? 0 : ((red > 255) ? 255 : red));
							}
							else
							{
								scan0Result[byteOffset] = (byte)pS[Xx3];
								scan0Result[byteOffset + 1] = (byte)pS[Xx3 + 1];
								scan0Result[byteOffset + 2] = (byte)pS[Xx3 + 2];
							}
						}
					}
				}
				catch (Exception)
				{
					throw;
				}
			}
			finally
			{
				if (sourceData != null)
					sourceBitmap.UnlockBits(sourceData);
				if (resultData != null)
					resultBitmap.UnlockBits(resultData);
			}

			return resultBitmap;
		}
		#endregion

		#region GetConvolutionFilter()
		private static unsafe Bitmap GetConvolutionFilter(Bitmap sourceBitmap, double[,] filterMatrix, double factor = 1, int bias = 0, bool grayscale = false)
		{
			BitmapData sourceData = null;
			Bitmap resultBitmap = null;
			BitmapData resultData = null;

			try
			{
				int width = sourceBitmap.Width;
				int height = sourceBitmap.Height;

				sourceData = sourceBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
				resultBitmap = new Bitmap(width, height, sourceData.PixelFormat);
				resultData = resultBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

				double blue = 0.0;
				double green = 0.0;
				double red = 0.0;

				int filterWidth = filterMatrix.GetLength(1);
				int filterHeight = filterMatrix.GetLength(0);
				int filterOffset = (filterWidth - 1) / 2;
				int calcOffset = 0;
				int byteOffset = 0;
				int x, y;

				byte* scan0Source = (byte*)sourceData.Scan0.ToPointer();
				byte* scan0Result = (byte*)resultData.Scan0.ToPointer();

				int strideS = sourceData.Stride;

				for (y = filterOffset; y < sourceBitmap.Height - filterOffset; y++)
				{
					for (x = filterOffset; x < sourceBitmap.Width - filterOffset; x++)
					{
						blue = 0;
						green = 0;
						red = 0;

						byteOffset = y * strideS + x * 3;

						for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
						{
							for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
							{
								calcOffset = byteOffset + (filterX * 3) + (filterY * strideS);

								blue += (double)(scan0Source[calcOffset]) * filterMatrix[filterY + filterOffset, filterX + filterOffset];
								green += (double)(scan0Source[calcOffset + 1]) * filterMatrix[filterY + filterOffset, filterX + filterOffset];
								red += (double)(scan0Source[calcOffset + 2]) * filterMatrix[filterY + filterOffset, filterX + filterOffset];
							}
						}

						blue = factor * blue + bias;
						green = factor * green + bias;
						red = factor * red + bias;

						scan0Result[byteOffset] = (byte)((blue < 0) ? 0 : ((blue > 255) ? 255 : blue));
						scan0Result[byteOffset + 1] = (byte)((green < 0) ? 0 : ((green > 255) ? 255 : green));
						scan0Result[byteOffset + 2] = (byte)((red < 0) ? 0 : ((red > 255) ? 255 : red));
					}
				}
			}
			finally
			{
				if (sourceData != null)
					sourceBitmap.UnlockBits(sourceData);
				if (resultData != null)
					resultBitmap.UnlockBits(resultData);
			}

			return resultBitmap;
		}
		#endregion

		#region SubtractAddFactorImage()
		private static Bitmap SubtractAddFactorImage(this Bitmap subtractFrom, Bitmap subtractValue, float factor = 1.0f)
		{
			BitmapData sourceData = subtractFrom.LockBits(new Rectangle(0, 0, subtractFrom.Width, subtractFrom.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);


			byte[] sourceBuffer = new byte[sourceData.Stride * sourceData.Height];

			Marshal.Copy(sourceData.Scan0, sourceBuffer, 0, sourceBuffer.Length);

			byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];
			BitmapData subtractData = subtractValue.LockBits(new Rectangle(0, 0, subtractValue.Width, subtractValue.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
			byte[] subtractBuffer = new byte[subtractData.Stride * subtractData.Height];

			Marshal.Copy(subtractData.Scan0, subtractBuffer, 0, subtractBuffer.Length);

			subtractFrom.UnlockBits(sourceData);
			subtractValue.UnlockBits(subtractData);

			double blue = 0;
			double green = 0;
			double red = 0;

			for (int k = 0; k < resultBuffer.Length && k < subtractBuffer.Length; k += 3)
			{
				blue = sourceBuffer[k] + (sourceBuffer[k] - subtractBuffer[k]) * factor;
				green = sourceBuffer[k + 1] + (sourceBuffer[k + 1] - subtractBuffer[k + 1]) * factor;
				red = sourceBuffer[k + 2] + (sourceBuffer[k + 2] - subtractBuffer[k + 2]) * factor;

				blue = (blue < 0 ? 0 : (blue > 255 ? 255 : blue));
				green = (green < 0 ? 0 : (green > 255 ? 255 : green));
				red = (red < 0 ? 0 : (red > 255 ? 255 : red));

				resultBuffer[k] = (byte)blue;
				resultBuffer[k + 1] = (byte)green;
				resultBuffer[k + 2] = (byte)red;
			}

			Bitmap resultBitmap = new Bitmap(subtractFrom.Width, subtractFrom.Height);
			BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0, resultBitmap.Width, resultBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
			Marshal.Copy(resultBuffer, 0, resultData.Scan0, resultBuffer.Length);

			resultBitmap.UnlockBits(resultData);

			return resultBitmap;
		}
		#endregion

		#endregion

	}
}
