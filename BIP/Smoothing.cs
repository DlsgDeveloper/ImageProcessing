using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class Smoothing
	{
		
		#region constructor
		private Smoothing()
		{
		}
		#endregion

		#region enum RotatingMaskType
		public enum RotatingMaskType
		{
			Mask_5x5,
			Mask_3x3
		}
		#endregion

		#region enum MedianNeighbourhood
		public enum MedianNeighbourhood
		{
			Cross5x5
		}
		#endregion

		//	PUBLIC METHODS
		#region public methods

		#region RotatingMask()
		/// <summary>
		/// It rotates the mask and picks region with the smallest color value spread.
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="clip"></param>
		/// <param name="maskType"></param>
		public static void RotatingMask(Bitmap bitmap, Rectangle clip, RotatingMaskType maskType)
		{
			if (bitmap == null)
				throw new IpException(ErrorCode.ErrorNoImageLoaded);

			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

			if (clip.Width < 3 || clip.Height < 3)
				throw new IpException(ErrorCode.InvalidParameter);

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			if (maskType == RotatingMaskType.Mask_5x5)
			{
				RotatingMask5x5_Process(bitmap, clip);
			}
			else
			{
				RotatingMask3x3_Process(bitmap, clip);
			}

#if DEBUG
			Console.WriteLine("Smoothing, RotatingMask(): " + DateTime.Now.Subtract(start).ToString());
#endif

#if SAVE_RESULTS
			bitmap.Save(Debug.SaveToDir + @"02 Smoothing.png", ImageFormat.Png);
#endif
		}
		#endregion

		#region Averaging3x3()
		/// <summary>
		/// It averages the 3x3 neighbour for each pixel
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="clip"></param>
		public static void Averaging3x3(Bitmap bitmap, Rectangle clip)
		{
			if (bitmap == null)
				throw new IpException(ErrorCode.ErrorNoImageLoaded);

			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

			if (clip.Width < 3 || clip.Height < 3)
				throw new IpException(ErrorCode.InvalidParameter);

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			Averaging3x3_Process(bitmap, clip);

#if DEBUG
			Console.WriteLine("Smoothing, Averaging3x3(): " + DateTime.Now.Subtract(start).ToString());
#endif

#if SAVE_RESULTS
			//bitmap.Save(Debug.SaveToDir + @"02 Smoothing.png", ImageFormat.Png);
#endif
		}
		#endregion

		#region Averaging5x5()
		/// <summary>
		/// It averages each pixel from 5x5 neighbour, but it picks only neighbour pixels in certain pixel "maxDelta" range.
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="clip"></param>
		public static void Averaging5x5(Bitmap bitmap, Rectangle clip)
		{
			if (bitmap == null)
				throw new IpException(ErrorCode.ErrorNoImageLoaded);

			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

			if (clip.Width < 3 || clip.Height < 3)
				throw new IpException(ErrorCode.InvalidParameter);

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format24bppRgb:
					{
						Averaging5x5_24bpp(bitmap, clip);
					} break;
				/*case PixelFormat.Format8bppIndexed:
					{
						Averaging5x5_8bpp(bitmap, clip, maxDelta);
					} break;*/
				default:
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}

#if DEBUG
			Console.WriteLine("Smoothing, Averaging5x5(): " + DateTime.Now.Subtract(start).ToString());
#endif
		}
		#endregion

		#region UnsharpMasking()
		public static void UnsharpMasking(Bitmap bitmap, Rectangle clip, int iterations)
		{
			if (bitmap == null)
				throw new IpException(ErrorCode.ErrorNoImageLoaded);

			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			for (int i = 0; i < iterations; i++)
			{
				UnsharpMasking_Process(bitmap, clip);
			}

#if DEBUG
			Console.WriteLine("Smoothing, UnsharpMasking(): " + DateTime.Now.Subtract(start).ToString());
#endif

#if SAVE_RESULTS
			bitmap.Save(Debug.SaveToDir + @"02 Smoothing.png", ImageFormat.Png);
#endif
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region RotatingMask5x5_Process()
		private static void RotatingMask5x5_Process(Bitmap bitmap, Rectangle clip)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = clip.Width;
				int height = clip.Height;

				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;

				unsafe
				{
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();

					if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
					{
						int[,] arrayR = new int[3, bitmapData.Width];
						int[,] arrayG = new int[3, bitmapData.Width];
						int[,] arrayB = new int[3, bitmapData.Width];
						int[,] arrayD = new int[3, bitmapData.Width];

						//first row
						GetRotatingMaskRow5x5_24bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);
						GetRotatingMaskRow5x5_24bpp(1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);

						SetRotatingMaskRow5x5_24bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);

						//Set middle part
						for (int y = 1; y < height - 1; y++)
						{
							GetRotatingMaskRow5x5_24bpp(y + 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);
							SetRotatingMaskRow5x5_24bpp(y, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);
						}

						//last row
						SetRotatingMaskRow5x5_24bpp(height - 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);
					}
					else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						int[,] array = new int[3, bitmapData.Width];
						int[,] arrayD = new int[3, bitmapData.Width];

						//first row
						GetRotatingMaskRow5x5_8bpp(0, clip.Size, pOrig, stride, array, arrayD);
						GetRotatingMaskRow5x5_8bpp(1, clip.Size, pOrig, stride, array, arrayD);

						SetRotatingMaskRow5x5_8bpp(0, clip.Size, pOrig, stride, array, arrayD);

						//Set middle part
						for (int y = 1; y < height - 1; y++)
						{
							GetRotatingMaskRow5x5_8bpp(y + 1, clip.Size, pOrig, stride, array, arrayD);
							SetRotatingMaskRow5x5_8bpp(y, clip.Size, pOrig, stride, array, arrayD);
						}

						//last row
						SetRotatingMaskRow5x5_8bpp(height - 1, clip.Size, pOrig, stride, array, arrayD);
					}
					else if (bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format32bppArgb)
					{
						int[,] arrayR = new int[3, bitmapData.Width];
						int[,] arrayG = new int[3, bitmapData.Width];
						int[,] arrayB = new int[3, bitmapData.Width];
						int[,] arrayD = new int[3, bitmapData.Width];

						//first row
						GetRotatingMaskRow5x5_32bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);
						GetRotatingMaskRow5x5_32bpp(1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);

						SetRotatingMaskRow5x5_32bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);

						//Set middle part
						for (int y = 1; y < height - 1; y++)
						{
							GetRotatingMaskRow5x5_32bpp(y + 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);
							SetRotatingMaskRow5x5_32bpp(y, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);
						}

						//last row
						SetRotatingMaskRow5x5_32bpp(height - 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);
					}
					else
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region GetRotatingMaskRow5x5_32bpp()
		private unsafe static void GetRotatingMaskRow5x5_32bpp(int y, Size imageSize, byte* pOrig, int stride, int[,] arrayR, int[,] arrayG, int[,] arrayB, int[,] arrayD)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			byte* pCurrent;

			int arrayRow = y % 3;
			int povR, povG, povB;

			//get UL corner
			if (y == 0)
			{
				//left
				x = 0;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = pCurrent[0] + pCurrent[4] + pCurrent[stride] + pCurrent[stride + 4];
				povB = (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);
				pCurrent++;
				arrayG[arrayRow, x] = pCurrent[0] + pCurrent[4] + pCurrent[stride] + pCurrent[stride + 4];
				povG = (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);
				pCurrent++;
				arrayR[arrayRow, x] = pCurrent[0] + pCurrent[4] + pCurrent[stride] + pCurrent[stride + 4];
				povR = (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);

				arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);

				//right
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = pCurrent[-4] + pCurrent[0] + pCurrent[stride - 4] + pCurrent[stride];
				povB = (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 4] * pCurrent[stride - 4]) + (pCurrent[stride] * pCurrent[stride]);
				pCurrent++;
				arrayG[arrayRow, x] = pCurrent[-4] + pCurrent[0] + pCurrent[stride - 4] + pCurrent[stride];
				povG = (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 4] * pCurrent[stride - 4]) + (pCurrent[stride] * pCurrent[stride]);
				pCurrent++;
				arrayR[arrayRow, x] = pCurrent[-4] + pCurrent[0] + pCurrent[stride - 4] + pCurrent[stride];
				povR = (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 4] * pCurrent[stride - 4]) + (pCurrent[stride] * pCurrent[stride]);

				arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);

				pCurrent = pOrig + y * stride + 4;

				for (x = 1; x < width - 1; x++)
				{
					arrayB[arrayRow, x] = pCurrent[-4] + pCurrent[0] + pCurrent[4] + pCurrent[stride - 4] + pCurrent[stride] + pCurrent[stride + 4];
					povB = (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride - 4] * pCurrent[stride - 4]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);
					pCurrent++;
					arrayG[arrayRow, x] = pCurrent[-4] + pCurrent[0] + pCurrent[4] + pCurrent[stride - 4] + pCurrent[stride] + pCurrent[stride + 4];
					povG = (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride - 4] * pCurrent[stride - 4]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);
					pCurrent++;
					arrayR[arrayRow, x] = pCurrent[-4] + pCurrent[0] + pCurrent[4] + pCurrent[stride - 4] + pCurrent[stride] + pCurrent[stride + 4];
					povR = (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride - 4] * pCurrent[stride - 4]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);

					arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);
					pCurrent++;
				}
			}
			else if (y == imageSize.Height - 1)
			{
				//left
				x = 0;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 4] + pCurrent[0] + pCurrent[4];
				povB = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 4] * pCurrent[-stride + 4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]);
				pCurrent++;
				arrayG[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 4] + pCurrent[0] + pCurrent[4];
				povG = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 4] * pCurrent[-stride + 4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]);
				pCurrent++;
				arrayR[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 4] + pCurrent[0] + pCurrent[4];
				povR = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 4] * pCurrent[-stride + 4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]);

				arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);

				//right
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = pCurrent[-4] + pCurrent[0] + pCurrent[-stride - 4] + pCurrent[-stride];
				povB = (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[-stride - 4] * pCurrent[-stride - 4]) + (pCurrent[-stride] * pCurrent[-stride]);
				pCurrent++;
				arrayG[arrayRow, x] = pCurrent[-4] + pCurrent[0] + pCurrent[-stride - 4] + pCurrent[-stride];
				povG = (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[-stride - 4] * pCurrent[-stride - 4]) + (pCurrent[-stride] * pCurrent[-stride]);
				pCurrent++;
				arrayR[arrayRow, x] = pCurrent[-4] + pCurrent[0] + pCurrent[-stride - 4] + pCurrent[-stride];
				povR = (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[-stride - 4] * pCurrent[-stride - 4]) + (pCurrent[-stride] * pCurrent[-stride]);

				arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);

				pCurrent = pOrig + y * stride + 4;

				for (x = 1; x < width - 1; x++)
				{
					arrayB[arrayRow, x] = pCurrent[-4] + pCurrent[0] + pCurrent[4] + pCurrent[-stride - 4] + pCurrent[-stride] + pCurrent[-stride + 4];
					povB = (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[-stride - 4] * pCurrent[-stride - 4]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 4] * pCurrent[-stride + 4]);
					pCurrent++;
					arrayG[arrayRow, x] = pCurrent[-4] + pCurrent[0] + pCurrent[4] + pCurrent[-stride - 4] + pCurrent[-stride] + pCurrent[-stride + 4];
					povG = (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[-stride - 4] * pCurrent[-stride - 4]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 4] * pCurrent[-stride + 4]);
					pCurrent++;
					arrayR[arrayRow, x] = pCurrent[-4] + pCurrent[0] + pCurrent[4] + pCurrent[-stride - 4] + pCurrent[-stride] + pCurrent[-stride + 4];
					povR = (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[-stride - 4] * pCurrent[-stride - 4]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 4] * pCurrent[-stride + 4]);

					arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);
					pCurrent++;
				}
			}
			else
			{
				//column 0
				x = 0;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 4] + pCurrent[0] + pCurrent[4] + pCurrent[stride] + pCurrent[stride + 4];
				povB = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 4] * pCurrent[-stride + 4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);
				pCurrent++;
				arrayG[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 4] + pCurrent[0] + pCurrent[4] + pCurrent[stride] + pCurrent[stride + 4];
				povG = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 4] * pCurrent[-stride + 4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);
				pCurrent++;
				arrayR[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 4] + pCurrent[0] + pCurrent[4] + pCurrent[stride] + pCurrent[stride + 4];
				povR = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 4] * pCurrent[-stride + 4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);
				arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);

				//last column
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = pCurrent[-stride - 4] + pCurrent[-stride] + pCurrent[-4] + pCurrent[0] + pCurrent[stride - 4] + pCurrent[stride];
				povB = (pCurrent[-stride - 4] * pCurrent[-stride - 4]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 4] * pCurrent[stride - 4]) + (pCurrent[stride] * pCurrent[stride]);
				pCurrent++;
				arrayG[arrayRow, x] = pCurrent[-stride - 4] + pCurrent[-stride] + pCurrent[-4] + pCurrent[0] + pCurrent[stride - 4] + pCurrent[stride];
				povG = (pCurrent[-stride - 4] * pCurrent[-stride - 4]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 4] * pCurrent[stride - 4]) + (pCurrent[stride] * pCurrent[stride]);
				pCurrent++;
				arrayR[arrayRow, x] = pCurrent[-stride - 4] + pCurrent[-stride] + pCurrent[-4] + pCurrent[0] + pCurrent[stride - 4] + pCurrent[stride];
				povR = (pCurrent[-stride - 4] * pCurrent[-stride - 4]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-4] * pCurrent[-4]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 4] * pCurrent[stride - 4]) + (pCurrent[stride] * pCurrent[stride]);
				arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);

				pCurrent = pOrig + y * stride + 4;

				int sumB = (pCurrent[-stride] + pCurrent[0] + pCurrent[+stride]) * 65536 + pCurrent[-stride - 4] + pCurrent[-4] + pCurrent[stride - 4];
				int sum3B = pCurrent[-stride + 4] + pCurrent[+4] + pCurrent[stride + 4];
				int sumG = (pCurrent[-stride + 1] + pCurrent[1] + pCurrent[stride + 1]) * 65536 + pCurrent[-stride - 3] + pCurrent[-3] + pCurrent[stride - 3];
				int sum3G = pCurrent[-stride + 5] + pCurrent[5] + pCurrent[stride + 5];
				int sumR = (pCurrent[-stride + 2] + pCurrent[2] + pCurrent[stride + 2]) * 65536 + pCurrent[-stride - 2] + pCurrent[-2] + pCurrent[stride - 2];
				int sum3R = pCurrent[-stride + 6] + pCurrent[6] + pCurrent[stride + 6];

				int pov1B = (pCurrent[-stride - 4] * pCurrent[-stride - 4]) + (pCurrent[-4] * pCurrent[-4]) + (pCurrent[stride - 4] * pCurrent[stride - 4]);
				int pov2B = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride] * pCurrent[stride]);
				int pov3B = (pCurrent[-stride + 4] * pCurrent[-stride + 4]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);
				int pov1G = (pCurrent[-stride - 3] * pCurrent[-stride - 3]) + (pCurrent[-3] * pCurrent[-3]) + (pCurrent[stride - 3] * pCurrent[stride - 3]);
				int pov2G = (pCurrent[-stride + 1] * pCurrent[-stride + 1]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[stride + 1] * pCurrent[stride + 1]);
				int pov3G = (pCurrent[-stride + 5] * pCurrent[-stride + 5]) + (pCurrent[5] * pCurrent[5]) + (pCurrent[stride + 5] * pCurrent[stride + 5]);
				int pov1R = (pCurrent[-stride - 2] * pCurrent[-stride - 2]) + (pCurrent[-2] * pCurrent[-2]) + (pCurrent[stride - 2] * pCurrent[stride - 2]);
				int pov2R = (pCurrent[-stride + 2] * pCurrent[-stride + 2]) + (pCurrent[2] * pCurrent[2]) + (pCurrent[stride + 2] * pCurrent[stride + 2]);
				int pov3R = (pCurrent[-stride + 6] * pCurrent[-stride + 6]) + (pCurrent[6] * pCurrent[6]) + (pCurrent[stride + 6] * pCurrent[stride + 6]);

				for (x = 1; x < width - 1; x++)
				{
					arrayB[arrayRow, x] = (sumB & 0xFFFF) + (sumB >> 16) + sum3B;
					arrayG[arrayRow, x] = (sumG & 0xFFFF) + (sumG >> 16) + sum3G;
					arrayR[arrayRow, x] = (sumR & 0xFFFF) + (sumR >> 16) + sum3R;

					arrayD[arrayRow, x] = (int)(((pov1R + pov2R + pov3R) + (pov1G + pov2G + pov3G) + (pov1B + pov2B + pov3B)) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]));
					pCurrent += 4;

					sumB = (sumB >> 16) | (sum3B << 16);
					sumG = (sumG >> 16) | (sum3G << 16);
					sumR = (sumR >> 16) | (sum3R << 16);
					sum3B = pCurrent[-stride + 4] + pCurrent[+4] + pCurrent[+stride + 4];
					sum3G = pCurrent[-stride + 5] + pCurrent[+5] + pCurrent[+stride + 5];
					sum3R = pCurrent[-stride + 6] + pCurrent[+6] + pCurrent[+stride + 6];

					pov1B = pov2B;
					pov2B = pov3B;
					pov3B = (pCurrent[-stride + 4] * pCurrent[-stride + 4]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);
					pov1G = pov2G;
					pov2G = pov3G;
					pov3G = (pCurrent[-stride + 5] * pCurrent[-stride + 5]) + (pCurrent[5] * pCurrent[5]) + (pCurrent[stride + 5] * pCurrent[stride + 5]);
					pov1R = pov2R;
					pov2R = pov3R;
					pov3R = (pCurrent[-stride + 6] * pCurrent[-stride + 6]) + (pCurrent[6] * pCurrent[6]) + (pCurrent[stride + 6] * pCurrent[stride + 6]);
				}
			}
		}
		#endregion

		#region SetRotatingMaskRow5x5_32bpp()
		private unsafe static void SetRotatingMaskRow5x5_32bpp(int y, Size imageSize, byte* pOrig, int stride, int[,] arrayR, int[,] arrayG, int[,] arrayB, int[,] arrayD)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			int minDisperse, resultR, resultG, resultB;
			int bestOption;
			int previousDispense = 0;

			if (y == 0)
			{
				//first row
				for (x = 1; x < width - 1; x++)
				{
					minDisperse = arrayD[0, x - 1];
					resultR = arrayR[0, x - 1];
					resultG = arrayG[0, x - 1];
					resultB = arrayB[0, x - 1];

					if (arrayD[0, x] < minDisperse)
					{
						minDisperse = arrayD[0, x];
						resultR = arrayR[0, x];
						resultG = arrayG[0, x];
						resultB = arrayB[0, x];
					}

					if (arrayD[0, x + 1] < minDisperse)
					{
						minDisperse = arrayD[0, x + 1];
						resultR = arrayR[0, x + 1];
						resultG = arrayG[0, x + 1];
						resultB = arrayB[0, x + 1];
					}

					if (arrayD[1, x + 1] < minDisperse)
					{
						minDisperse = arrayD[1, x - 1];
						resultR = arrayR[1, x - 1];
						resultG = arrayG[1, x - 1];
						resultB = arrayB[1, x - 1];
					}

					if (arrayD[1, x + 1] < minDisperse)
					{
						minDisperse = arrayD[1, x];
						resultR = arrayR[1, x];
						resultG = arrayG[1, x];
						resultB = arrayB[1, x];
					}
					if (arrayD[1, x + 1] < minDisperse)
					{
						minDisperse = arrayD[1, x + 1];
						resultR = arrayR[1, x + 1];
						resultG = arrayG[1, x + 1];
						resultB = arrayB[1, x + 1];
					}

					pOrig[y * stride + x * 4] = (byte)(resultB / 9);
					pOrig[y * stride + x * 4 + 1] = (byte)(resultG / 9);
					pOrig[y * stride + x * 4 + 2] = (byte)(resultR / 9);
				}
			}
			else if (y == imageSize.Height - 1)
			{
				int row1Index = ((y - 1) % 3);
				int row2Index = (y) % 3;

				//last row
				for (x = 1; x < width - 1; x++)
				{
					minDisperse = arrayD[row1Index, x - 1];
					resultR = arrayR[row1Index, x - 1];
					resultG = arrayG[row1Index, x - 1];
					resultB = arrayB[row1Index, x - 1];

					if (arrayD[row1Index, x] < minDisperse)
					{
						minDisperse = arrayD[row1Index, x];
						resultR = arrayR[row1Index, x];
						resultG = arrayG[row1Index, x];
						resultB = arrayB[row1Index, x];
					}

					if (arrayD[row1Index, x + 1] < minDisperse)
					{
						minDisperse = arrayD[row1Index, x + 1];
						resultR = arrayR[row1Index, x + 1];
						resultG = arrayG[row1Index, x + 1];
						resultB = arrayB[row1Index, x + 1];
					}

					if (arrayD[1, x + 1] < minDisperse)
					{
						minDisperse = arrayD[row2Index, x - 1];
						resultR = arrayR[row2Index, x - 1];
						resultG = arrayG[row2Index, x - 1];
						resultB = arrayB[row2Index, x - 1];
					}

					if (arrayD[row2Index, x + 1] < minDisperse)
					{
						minDisperse = arrayD[row2Index, x];
						resultR = arrayR[row2Index, x];
						resultG = arrayG[row2Index, x];
						resultB = arrayB[row2Index, x];
					}
					if (arrayD[row2Index, x + 1] < minDisperse)
					{
						minDisperse = arrayD[row2Index, x + 1];
						resultR = arrayR[row2Index, x + 1];
						resultG = arrayG[row2Index, x + 1];
						resultB = arrayB[row2Index, x + 1];
					}

					pOrig[y * stride + x * 4] = (byte)(resultB / 9);
					pOrig[y * stride + x * 4 + 1] = (byte)(resultG / 9);
					pOrig[y * stride + x * 4 + 2] = (byte)(resultR / 9);
				}
			}
			else
			{
				//first pixel
				x = 0;

				minDisperse = arrayD[0, x];
				resultR = arrayR[0, x];
				resultG = arrayG[0, x];
				resultB = arrayB[0, x];

				if (arrayD[0, x + 1] < minDisperse)
				{
					minDisperse = arrayD[0, x + 1];
					resultR = arrayR[0, x + 1];
					resultG = arrayG[0, x + 1];
					resultB = arrayB[0, x + 1];
				}

				if (arrayD[1, x] < minDisperse)
				{
					minDisperse = arrayD[1, x];
					resultR = arrayR[1, x];
					resultG = arrayG[1, x];
					resultB = arrayB[1, x];
				}
				if (arrayD[1, x + 1] < minDisperse)
				{
					minDisperse = arrayD[1, x + 1];
					resultR = arrayR[1, x + 1];
					resultG = arrayG[1, x + 1];
					resultB = arrayB[1, x + 1];
				}

				if (arrayD[2, x] < minDisperse)
				{
					minDisperse = arrayD[2, x];
					resultR = arrayR[2, x];
					resultG = arrayG[2, x];
					resultB = arrayB[2, x];
				}

				if (arrayD[2, x + 1] < minDisperse)
				{
					minDisperse = arrayD[2, x + 1];
					resultR = arrayR[2, x + 1];
					resultG = arrayG[2, x + 1];
					resultB = arrayB[2, x + 1];
				}

				pOrig[y * stride + x * 4] = (byte)(resultB / 9);
				pOrig[y * stride + x * 4 + 1] = (byte)(resultG / 9);
				pOrig[y * stride + x * 4 + 2] = (byte)(resultR / 9);

				//last pixels
				x = width - 1;

				minDisperse = arrayD[0, x - 1];
				resultR = arrayR[0, x - 1];
				resultG = arrayG[0, x - 1];
				resultB = arrayB[0, x - 1];

				if (arrayD[0, x] < minDisperse)
				{
					minDisperse = arrayD[0, x];
					resultR = arrayR[0, x];
					resultG = arrayG[0, x];
					resultB = arrayB[0, x];
				}

				if (arrayD[1, x - 1] < minDisperse)
				{
					minDisperse = arrayD[1, x - 1];
					resultR = arrayR[1, x - 1];
					resultG = arrayG[1, x - 1];
					resultB = arrayB[1, x - 1];
				}

				if (arrayD[1, x] < minDisperse)
				{
					minDisperse = arrayD[1, x];
					resultR = arrayR[1, x];
					resultG = arrayG[1, x];
					resultB = arrayB[1, x];
				}

				if (arrayD[2, x - 1] < minDisperse)
				{
					minDisperse = arrayD[2, x - 1];
					resultR = arrayR[2, x - 1];
					resultG = arrayG[2, x - 1];
					resultB = arrayB[2, x - 1];
				}

				if (arrayD[2, x] < minDisperse)
				{
					minDisperse = arrayD[2, x];
					resultR = arrayR[2, x];
					resultG = arrayG[2, x];
					resultB = arrayB[2, x];
				}

				pOrig[y * stride + x * 4] = (byte)(resultB / 9);
				pOrig[y * stride + x * 4 + 1] = (byte)(resultG / 9);
				pOrig[y * stride + x * 4 + 2] = (byte)(resultR / 9);

				bestOption = -1;

				//middle pixels
				for (x = 1; x < width - 1; x++)
				{
					if ((bestOption % 65536) >= x - 1)
					{
						minDisperse = previousDispense;

						if (arrayD[0, x + 1] < minDisperse)
						{
							minDisperse = arrayD[0, x + 1];
							bestOption = x + 1;
						}

						if (arrayD[1, x + 1] < minDisperse)
						{
							minDisperse = arrayD[1, x + 1];
							bestOption = 65536 + x + 1;
						}

						if (arrayD[2, x + 1] < minDisperse)
						{
							minDisperse = arrayD[2, x + 1];
							bestOption = 131072 + x + 1;
						}
					}
					else
					{
						if (arrayD[0, x] < arrayD[0, x - 1])
						{
							minDisperse = arrayD[0, x];
							bestOption = x;
						}
						else
						{
							minDisperse = arrayD[0, x - 1];
							bestOption = x - 1;
						}

						if (arrayD[0, x + 1] < minDisperse)
						{
							minDisperse = arrayD[0, x + 1];
							bestOption = x + 1;
						}

						if (arrayD[1, x - 1] < minDisperse)
						{
							minDisperse = arrayD[1, x - 1];
							bestOption = 65536 + x - 1;
						}

						if (arrayD[1, x] < minDisperse)
						{
							minDisperse = arrayD[1, x];
							bestOption = 65536 + x;
						}
						if (arrayD[1, x + 1] < minDisperse)
						{
							minDisperse = arrayD[1, x + 1];
							bestOption = 65536 + x + 1;
						}

						if (arrayD[2, x - 1] < minDisperse)
						{
							minDisperse = arrayD[2, x - 1];
							bestOption = 131072 + x - 1;
						}

						if (arrayD[2, x] < minDisperse)
						{
							minDisperse = arrayD[2, x];
							bestOption = 131072 + x;
						}

						if (arrayD[2, x + 1] < minDisperse)
						{
							minDisperse = arrayD[2, x + 1];
							bestOption = 131072 + x + 1;
						}
					}

					previousDispense = minDisperse;

					pOrig[y * stride + x * 4] = (byte)(arrayB[bestOption / 65536, bestOption & 0xFFFF] / 9);
					pOrig[y * stride + x * 4 + 1] = (byte)(arrayG[bestOption / 65536, bestOption & 0xFFFF] / 9);
					pOrig[y * stride + x * 4 + 2] = (byte)(arrayR[bestOption / 65536, bestOption & 0xFFFF] / 9);
				}
			}
		}
		#endregion

		#region GetRotatingMaskRow5x5_24bpp()
		private unsafe static void GetRotatingMaskRow5x5_24bpp(int y, Size imageSize, byte* pOrig, int stride, int[,] arrayR, int[,] arrayG, int[,] arrayB, int[,] arrayD)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			byte* pCurrent;

			int arrayRow = y % 3;
			int povR, povG, povB;

			//get UL corner
			if (y == 0)
			{
				//left
				x = 0;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = pCurrent[0] + pCurrent[3] + pCurrent[stride] + pCurrent[stride + 3];
				povB = (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 3] * pCurrent[stride + 3]);
				pCurrent++;
				arrayG[arrayRow, x] = pCurrent[0] + pCurrent[3] + pCurrent[stride] + pCurrent[stride + 3];
				povG = (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 3] * pCurrent[stride + 3]);
				pCurrent++;
				arrayR[arrayRow, x] = pCurrent[0] + pCurrent[3] + pCurrent[stride] + pCurrent[stride + 3];
				povR = (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 3] * pCurrent[stride + 3]);

				arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);

				//right
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = pCurrent[-3] + pCurrent[0] + pCurrent[stride - 3] + pCurrent[stride];
				povB = (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 3] * pCurrent[stride - 3]) + (pCurrent[stride] * pCurrent[stride]);
				pCurrent++;
				arrayG[arrayRow, x] = pCurrent[-3] + pCurrent[0] + pCurrent[stride - 3] + pCurrent[stride];
				povG = (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 3] * pCurrent[stride - 3]) + (pCurrent[stride] * pCurrent[stride]);
				pCurrent++;
				arrayR[arrayRow, x] = pCurrent[-3] + pCurrent[0] + pCurrent[stride - 3] + pCurrent[stride];
				povR = (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 3] * pCurrent[stride - 3]) + (pCurrent[stride] * pCurrent[stride]);

				arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);

				pCurrent = pOrig + y * stride + 3;

				for (x = 1; x < width - 1; x++)
				{
					arrayB[arrayRow, x] = pCurrent[-3] + pCurrent[0] + pCurrent[3] + pCurrent[stride - 3] + pCurrent[stride] + pCurrent[stride + 3];
					povB = (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[stride - 3] * pCurrent[stride - 3]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 3] * pCurrent[stride + 3]);
					pCurrent++;
					arrayG[arrayRow, x] = pCurrent[-3] + pCurrent[0] + pCurrent[3] + pCurrent[stride - 3] + pCurrent[stride] + pCurrent[stride + 3];
					povG = (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[stride - 3] * pCurrent[stride - 3]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 3] * pCurrent[stride + 3]);
					pCurrent++;
					arrayR[arrayRow, x] = pCurrent[-3] + pCurrent[0] + pCurrent[3] + pCurrent[stride - 3] + pCurrent[stride] + pCurrent[stride + 3];
					povR = (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[stride - 3] * pCurrent[stride - 3]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 3] * pCurrent[stride + 3]);

					arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);
					pCurrent++;
				}
			}
			else if (y == imageSize.Height - 1)
			{
				//left
				x = 0;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[0] + pCurrent[3];
				povB = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 3] * pCurrent[-stride + 3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]);
				pCurrent++;
				arrayG[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[0] + pCurrent[3];
				povG = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 3] * pCurrent[-stride + 3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]);
				pCurrent++;
				arrayR[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[0] + pCurrent[3];
				povR = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 3] * pCurrent[-stride + 3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]);

				arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);

				//right
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = pCurrent[-3] + pCurrent[0] + pCurrent[-stride - 3] + pCurrent[-stride];
				povB = (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[-stride - 3] * pCurrent[-stride - 3]) + (pCurrent[-stride] * pCurrent[-stride]);
				pCurrent++;
				arrayG[arrayRow, x] = pCurrent[-3] + pCurrent[0] + pCurrent[-stride - 3] + pCurrent[-stride];
				povG = (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[-stride - 3] * pCurrent[-stride - 3]) + (pCurrent[-stride] * pCurrent[-stride]);
				pCurrent++;
				arrayR[arrayRow, x] = pCurrent[-3] + pCurrent[0] + pCurrent[-stride - 3] + pCurrent[-stride];
				povR = (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[-stride - 3] * pCurrent[-stride - 3]) + (pCurrent[-stride] * pCurrent[-stride]);

				arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);

				pCurrent = pOrig + y * stride + 3;

				for (x = 1; x < width - 1; x++)
				{
					arrayB[arrayRow, x] = pCurrent[-3] + pCurrent[0] + pCurrent[3] + pCurrent[-stride - 3] + pCurrent[-stride] + pCurrent[-stride + 3];
					povB = (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[-stride - 3] * pCurrent[-stride - 3]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 3] * pCurrent[-stride + 3]);
					pCurrent++;
					arrayG[arrayRow, x] = pCurrent[-3] + pCurrent[0] + pCurrent[3] + pCurrent[-stride - 3] + pCurrent[-stride] + pCurrent[-stride + 3];
					povG = (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[-stride - 3] * pCurrent[-stride - 3]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 3] * pCurrent[-stride + 3]);
					pCurrent++;
					arrayR[arrayRow, x] = pCurrent[-3] + pCurrent[0] + pCurrent[3] + pCurrent[-stride - 3] + pCurrent[-stride] + pCurrent[-stride + 3];
					povR = (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[-stride - 3] * pCurrent[-stride - 3]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 3] * pCurrent[-stride + 3]);

					arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);
					pCurrent++;
				}
			}
			else
			{
				//column 0
				x = 0;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[0] + pCurrent[3] + pCurrent[stride] + pCurrent[stride + 3];
				povB = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 3] * pCurrent[-stride + 3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 3] * pCurrent[stride + 3]);
				pCurrent++;
				arrayG[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[0] + pCurrent[3] + pCurrent[stride] + pCurrent[stride + 3];
				povG = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 3] * pCurrent[-stride + 3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 3] * pCurrent[stride + 3]);
				pCurrent++;
				arrayR[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[0] + pCurrent[3] + pCurrent[stride] + pCurrent[stride + 3];
				povR = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 3] * pCurrent[-stride + 3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 3] * pCurrent[stride + 3]);
				arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);

				//last column
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = pCurrent[-stride - 3] + pCurrent[-stride] + pCurrent[-3] + pCurrent[0] + pCurrent[stride - 3] + pCurrent[stride];
				povB = (pCurrent[-stride - 3] * pCurrent[-stride - 3]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 3] * pCurrent[stride - 3]) + (pCurrent[stride] * pCurrent[stride]);
				pCurrent++;
				arrayG[arrayRow, x] = pCurrent[-stride - 3] + pCurrent[-stride] + pCurrent[-3] + pCurrent[0] + pCurrent[stride - 3] + pCurrent[stride];
				povG = (pCurrent[-stride - 3] * pCurrent[-stride - 3]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 3] * pCurrent[stride - 3]) + (pCurrent[stride] * pCurrent[stride]);
				pCurrent++;
				arrayR[arrayRow, x] = pCurrent[-stride - 3] + pCurrent[-stride] + pCurrent[-3] + pCurrent[0] + pCurrent[stride - 3] + pCurrent[stride];
				povR = (pCurrent[-stride - 3] * pCurrent[-stride - 3]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-3] * pCurrent[-3]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 3] * pCurrent[stride - 3]) + (pCurrent[stride] * pCurrent[stride]);
				arrayD[arrayRow, x] = (povR + povG + povB) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);

				pCurrent = pOrig + y * stride + 3;

				int sumB = (pCurrent[-stride] + pCurrent[0] + pCurrent[+stride]) * 65536 + pCurrent[-stride - 3] + pCurrent[-3] + pCurrent[stride - 3];
				int sum3B = pCurrent[-stride + 3] + pCurrent[+3] + pCurrent[stride + 3];
				int sumG = (pCurrent[-stride + 1] + pCurrent[1] + pCurrent[stride + 1]) * 65536 + pCurrent[-stride - 2] + pCurrent[-2] + pCurrent[stride - 2];
				int sum3G = pCurrent[-stride + 4] + pCurrent[4] + pCurrent[stride + 4];
				int sumR = (pCurrent[-stride + 2] + pCurrent[2] + pCurrent[stride + 2]) * 65536 + pCurrent[-stride - 1] + pCurrent[-1] + pCurrent[stride - 1];
				int sum3R = pCurrent[-stride + 5] + pCurrent[5] + pCurrent[stride + 5];

				int pov1B = (pCurrent[-stride - 3] * pCurrent[-stride - 3]) + (pCurrent[-3] * pCurrent[-3]) + (pCurrent[stride - 3] * pCurrent[stride - 3]);
				int pov2B = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride] * pCurrent[stride]);
				int pov3B = (pCurrent[-stride + 3] * pCurrent[-stride + 3]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[stride + 3] * pCurrent[stride + 3]);
				int pov1G = (pCurrent[-stride - 2] * pCurrent[-stride - 2]) + (pCurrent[-2] * pCurrent[-2]) + (pCurrent[stride - 2] * pCurrent[stride - 2]);
				int pov2G = (pCurrent[-stride + 1] * pCurrent[-stride + 1]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[stride + 1] * pCurrent[stride + 1]);
				int pov3G = (pCurrent[-stride + 4] * pCurrent[-stride + 4]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);
				int pov1R = (pCurrent[-stride - 1] * pCurrent[-stride - 1]) + (pCurrent[-1] * pCurrent[-1]) + (pCurrent[stride - 1] * pCurrent[stride - 1]);
				int pov2R = (pCurrent[-stride + 2] * pCurrent[-stride + 2]) + (pCurrent[2] * pCurrent[2]) + (pCurrent[stride + 2] * pCurrent[stride + 2]);
				int pov3R = (pCurrent[-stride + 5] * pCurrent[-stride + 5]) + (pCurrent[5] * pCurrent[5]) + (pCurrent[stride + 5] * pCurrent[stride + 5]);

				for (x = 1; x < width - 1; x++)
				{
					arrayB[arrayRow, x] = (sumB & 0xFFFF) + (sumB >> 16) + sum3B;
					arrayG[arrayRow, x] = (sumG & 0xFFFF) + (sumG >> 16) + sum3G;
					arrayR[arrayRow, x] = (sumR & 0xFFFF) + (sumR >> 16) + sum3R;

					arrayD[arrayRow, x] = (int)(((pov1R + pov2R + pov3R) + (pov1G + pov2G + pov3G) + (pov1B + pov2B + pov3B)) * 9 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]));
					pCurrent += 3;

					sumB = (sumB >> 16) | (sum3B << 16);
					sumG = (sumG >> 16) | (sum3G << 16);
					sumR = (sumR >> 16) | (sum3R << 16);
					sum3B = pCurrent[-stride + 3] + pCurrent[+3] + pCurrent[+stride + 3];
					sum3G = pCurrent[-stride + 4] + pCurrent[+4] + pCurrent[+stride + 4];
					sum3R = pCurrent[-stride + 5] + pCurrent[+5] + pCurrent[+stride + 5];

					pov1B = pov2B;
					pov2B = pov3B;
					pov3B = (pCurrent[-stride + 3] * pCurrent[-stride + 3]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[stride + 3] * pCurrent[stride + 3]);
					pov1G = pov2G;
					pov2G = pov3G;
					pov3G = (pCurrent[-stride + 4] * pCurrent[-stride + 4]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[stride + 4] * pCurrent[stride + 4]);
					pov1R = pov2R;
					pov2R = pov3R;
					pov3R = (pCurrent[-stride + 5] * pCurrent[-stride + 5]) + (pCurrent[5] * pCurrent[5]) + (pCurrent[stride + 5] * pCurrent[stride + 5]);
				} 
			}
		}
		#endregion

		#region SetRotatingMaskRow5x5_24bpp()
		private unsafe static void SetRotatingMaskRow5x5_24bpp(int y, Size imageSize, byte* pOrig, int stride, int[,] arrayR, int[,] arrayG, int[,] arrayB, int[,] arrayD)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			int minDisperse, resultR, resultG, resultB;
			int bestOption;
			int previousDispense = 0;

			if (y == 0)
			{
				//first row
				for (x = 1; x < width - 1; x++)
				{
					minDisperse = arrayD[0, x - 1];
					resultR = arrayR[0, x - 1];
					resultG = arrayG[0, x - 1];
					resultB = arrayB[0, x - 1];

					if (arrayD[0, x] < minDisperse)
					{
						minDisperse = arrayD[0, x];
						resultR = arrayR[0, x];
						resultG = arrayG[0, x];
						resultB = arrayB[0, x];
					}

					if (arrayD[0, x + 1] < minDisperse)
					{
						minDisperse = arrayD[0, x + 1];
						resultR = arrayR[0, x + 1];
						resultG = arrayG[0, x + 1];
						resultB = arrayB[0, x + 1];
					}

					if (arrayD[1, x - 1] < minDisperse)
					{
						minDisperse = arrayD[1, x - 1];
						resultR = arrayR[1, x - 1];
						resultG = arrayG[1, x - 1];
						resultB = arrayB[1, x - 1];
					}

					if (arrayD[1, x] < minDisperse)
					{
						minDisperse = arrayD[1, x];
						resultR = arrayR[1, x];
						resultG = arrayG[1, x];
						resultB = arrayB[1, x];
					}
					if (arrayD[1, x + 1] < minDisperse)
					{
						minDisperse = arrayD[1, x + 1];
						resultR = arrayR[1, x + 1];
						resultG = arrayG[1, x + 1];
						resultB = arrayB[1, x + 1];
					}

					pOrig[y * stride + x * 3] = (byte)(resultB / 9);
					pOrig[y * stride + x * 3 + 1] = (byte)(resultG / 9);
					pOrig[y * stride + x * 3 + 2] = (byte)(resultR / 9);
				}
			}
			else if (y == imageSize.Height - 1)
			{
				int row1Index = ((y - 1) % 3);
				int row2Index = (y) % 3;

				//last row
				for (x = 1; x < width - 1; x++)
				{
					minDisperse = arrayD[row1Index, x - 1];
					resultR = arrayR[row1Index, x - 1];
					resultG = arrayG[row1Index, x - 1];
					resultB = arrayB[row1Index, x - 1];

					if (arrayD[row1Index, x] < minDisperse)
					{
						minDisperse = arrayD[row1Index, x];
						resultR = arrayR[row1Index, x];
						resultG = arrayG[row1Index, x];
						resultB = arrayB[row1Index, x];
					}

					if (arrayD[row1Index, x + 1] < minDisperse)
					{
						minDisperse = arrayD[row1Index, x + 1];
						resultR = arrayR[row1Index, x + 1];
						resultG = arrayG[row1Index, x + 1];
						resultB = arrayB[row1Index, x + 1];
					}

					if (arrayD[row2Index, x - 1] < minDisperse)
					{
						minDisperse = arrayD[row2Index, x - 1];
						resultR = arrayR[row2Index, x - 1];
						resultG = arrayG[row2Index, x - 1];
						resultB = arrayB[row2Index, x - 1];
					}

					if (arrayD[row2Index, x] < minDisperse)
					{
						minDisperse = arrayD[row2Index, x];
						resultR = arrayR[row2Index, x];
						resultG = arrayG[row2Index, x];
						resultB = arrayB[row2Index, x];
					}
					if (arrayD[row2Index, x + 1] < minDisperse)
					{
						minDisperse = arrayD[row2Index, x + 1];
						resultR = arrayR[row2Index, x + 1];
						resultG = arrayG[row2Index, x + 1];
						resultB = arrayB[row2Index, x + 1];
					}

					pOrig[y * stride + x * 3] = (byte)(resultB / 9);
					pOrig[y * stride + x * 3 + 1] = (byte)(resultG / 9);
					pOrig[y * stride + x * 3 + 2] = (byte)(resultR / 9);
				}
			}
			else
			{
				//first pixel
				x = 0;

				minDisperse = arrayD[0, x];
				resultR = arrayR[0, x];
				resultG = arrayG[0, x];
				resultB = arrayB[0, x];

				if (arrayD[0, x + 1] < minDisperse)
				{
					minDisperse = arrayD[0, x + 1];
					resultR = arrayR[0, x + 1];
					resultG = arrayG[0, x + 1];
					resultB = arrayB[0, x + 1];
				}

				if (arrayD[1, x] < minDisperse)
				{
					minDisperse = arrayD[1, x];
					resultR = arrayR[1, x];
					resultG = arrayG[1, x];
					resultB = arrayB[1, x];
				}
				if (arrayD[1, x + 1] < minDisperse)
				{
					minDisperse = arrayD[1, x + 1];
					resultR = arrayR[1, x + 1];
					resultG = arrayG[1, x + 1];
					resultB = arrayB[1, x + 1];
				}

				if (arrayD[2, x] < minDisperse)
				{
					minDisperse = arrayD[2, x];
					resultR = arrayR[2, x];
					resultG = arrayG[2, x];
					resultB = arrayB[2, x];
				}

				if (arrayD[2, x + 1] < minDisperse)
				{
					minDisperse = arrayD[2, x + 1];
					resultR = arrayR[2, x + 1];
					resultG = arrayG[2, x + 1];
					resultB = arrayB[2, x + 1];
				}

				pOrig[y * stride + x * 3] = (byte)(resultB / 9);
				pOrig[y * stride + x * 3 + 1] = (byte)(resultG / 9);
				pOrig[y * stride + x * 3 + 2] = (byte)(resultR / 9);

				//last pixels
				x = width - 1;

				minDisperse = arrayD[0, x - 1];
				resultR = arrayR[0, x - 1];
				resultG = arrayG[0, x - 1];
				resultB = arrayB[0, x - 1];

				if (arrayD[0, x] < minDisperse)
				{
					minDisperse = arrayD[0, x];
					resultR = arrayR[0, x];
					resultG = arrayG[0, x];
					resultB = arrayB[0, x];
				}

				if (arrayD[1, x - 1] < minDisperse)
				{
					minDisperse = arrayD[1, x - 1];
					resultR = arrayR[1, x - 1];
					resultG = arrayG[1, x - 1];
					resultB = arrayB[1, x - 1];
				}

				if (arrayD[1, x] < minDisperse)
				{
					minDisperse = arrayD[1, x];
					resultR = arrayR[1, x];
					resultG = arrayG[1, x];
					resultB = arrayB[1, x];
				}

				if (arrayD[2, x - 1] < minDisperse)
				{
					minDisperse = arrayD[2, x - 1];
					resultR = arrayR[2, x - 1];
					resultG = arrayG[2, x - 1];
					resultB = arrayB[2, x - 1];
				}

				if (arrayD[2, x] < minDisperse)
				{
					minDisperse = arrayD[2, x];
					resultR = arrayR[2, x];
					resultG = arrayG[2, x];
					resultB = arrayB[2, x];
				}

				pOrig[y * stride + x * 3] = (byte)(resultB / 9);
				pOrig[y * stride + x * 3 + 1] = (byte)(resultG / 9);
				pOrig[y * stride + x * 3 + 2] = (byte)(resultR / 9);

				bestOption = -1;

				//middle pixels
				for (x = 1; x < width - 1; x++)
				{
					if ((bestOption % 65536) >= x - 1)
					{
						minDisperse = previousDispense;

						if (arrayD[0, x + 1] < minDisperse)
						{
							minDisperse = arrayD[0, x + 1];
							bestOption = x + 1;
						}

						if (arrayD[1, x + 1] < minDisperse)
						{
							minDisperse = arrayD[1, x + 1];
							bestOption = 65536 + x + 1;
						}

						if (arrayD[2, x + 1] < minDisperse)
						{
							minDisperse = arrayD[2, x + 1];
							bestOption = 131072 + x + 1;
						}
					}
					else
					{
						if (arrayD[0, x] < arrayD[0, x - 1])
						{
							minDisperse = arrayD[0, x];
							bestOption = x;
						}
						else
						{
							minDisperse = arrayD[0, x - 1];
							bestOption = x - 1;
						}

						if (arrayD[0, x + 1] < minDisperse)
						{
							minDisperse = arrayD[0, x + 1];
							bestOption = x + 1;
						}

						if (arrayD[1, x - 1] < minDisperse)
						{
							minDisperse = arrayD[1, x - 1];
							bestOption = 65536 + x - 1;
						}

						if (arrayD[1, x] < minDisperse)
						{
							minDisperse = arrayD[1, x];
							bestOption = 65536 + x;
						}
						if (arrayD[1, x + 1] < minDisperse)
						{
							minDisperse = arrayD[1, x + 1];
							bestOption = 65536 + x + 1;
						}

						if (arrayD[2, x - 1] < minDisperse)
						{
							minDisperse = arrayD[2, x - 1];
							bestOption = 131072 + x - 1;
						}

						if (arrayD[2, x] < minDisperse)
						{
							minDisperse = arrayD[2, x];
							bestOption = 131072 + x;
						}

						if (arrayD[2, x + 1] < minDisperse)
						{
							minDisperse = arrayD[2, x + 1];
							bestOption = 131072 + x + 1;
						}
					}

					previousDispense = minDisperse;

					pOrig[y * stride + x * 3] = (byte)(arrayB[bestOption / 65536, bestOption & 0xFFFF] / 9);
					pOrig[y * stride + x * 3 + 1] = (byte)(arrayG[bestOption / 65536, bestOption & 0xFFFF] / 9);
					pOrig[y * stride + x * 3 + 2] = (byte)(arrayR[bestOption / 65536, bestOption & 0xFFFF] / 9); 
				}
			}
		}
		#endregion

		#region GetRotatingMaskRow5x5_8bpp()
		private unsafe static void GetRotatingMaskRow5x5_8bpp(int y, Size imageSize, byte* pOrig, int stride, int[,] arrayG, int[,] arrayD)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			byte* pCurrent;

			int arrayRow = y % 3;
			int povG;

			//get UL corner
			if (y == 0)
			{
				//left
				x = 0;
				pCurrent = pOrig + y * stride + x;

				arrayG[arrayRow, x] = pCurrent[0] + pCurrent[1] + pCurrent[stride] + pCurrent[stride + 1];
				povG = (pCurrent[0] * pCurrent[0]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 1] * pCurrent[stride + 1]);

				arrayD[arrayRow, x] = (povG) * 9 - (arrayG[arrayRow, x] * arrayG[arrayRow, x]);

				//right
				x = width - 1;
				pCurrent = pOrig + y * stride + x;

				arrayG[arrayRow, x] = pCurrent[-1] + pCurrent[0] + pCurrent[stride - 1] + pCurrent[stride];
				povG = (pCurrent[-1] * pCurrent[-1]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 1] * pCurrent[stride - 1]) + (pCurrent[stride] * pCurrent[stride]);

				arrayD[arrayRow, x] = (povG) * 9 - (arrayG[arrayRow, x] * arrayG[arrayRow, x]);

				pCurrent = pOrig + y * stride + 1;

				for (x = 1; x < width - 1; x++)
				{
					arrayG[arrayRow, x] = pCurrent[-1] + pCurrent[0] + pCurrent[1] + pCurrent[stride - 1] + pCurrent[stride] + pCurrent[stride + 1];
					povG = (pCurrent[-1] * pCurrent[-1]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[stride - 1] * pCurrent[stride - 1]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 1] * pCurrent[stride + 1]);
					pCurrent++;

					arrayD[arrayRow, x] = (povG) * 9 - (arrayG[arrayRow, x] * arrayG[arrayRow, x]);
				}
			}
			else if (y == imageSize.Height - 1)
			{
				//left
				x = 0;
				pCurrent = pOrig + y * stride + x;

				arrayG[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 1] + pCurrent[0] + pCurrent[1];
				povG = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 1] * pCurrent[-stride + 1]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[1] * pCurrent[1]);

				arrayD[arrayRow, x] = (povG) * 9 - (arrayG[arrayRow, x] * arrayG[arrayRow, x]);

				//right
				x = width - 1;
				pCurrent = pOrig + y * stride + x;

				arrayG[arrayRow, x] = pCurrent[-1] + pCurrent[0] + pCurrent[-stride - 1] + pCurrent[-stride];
				povG = (pCurrent[-1] * pCurrent[-1]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[-stride - 1] * pCurrent[-stride - 1]) + (pCurrent[-stride] * pCurrent[-stride]);

				arrayD[arrayRow, x] = (povG) * 9 - (arrayG[arrayRow, x] * arrayG[arrayRow, x]);

				pCurrent = pOrig + y * stride + 1;

				for (x = 1; x < width - 1; x++)
				{
					arrayG[arrayRow, x] = pCurrent[-1] + pCurrent[0] + pCurrent[1] + pCurrent[-stride - 1] + pCurrent[-stride] + pCurrent[-stride + 1];
					povG = (pCurrent[-1] * pCurrent[-1]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[-stride - 1] * pCurrent[-stride - 1]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 1] * pCurrent[-stride + 1]);
					pCurrent++;

					arrayD[arrayRow, x] = (povG) * 9 - (arrayG[arrayRow, x] * arrayG[arrayRow, x]);
				}
			}
			else
			{
				//column 0
				x = 0;
				pCurrent = pOrig + y * stride + x;

				arrayG[arrayRow, x] = pCurrent[-stride] + pCurrent[-stride + 1] + pCurrent[0] + pCurrent[1] + pCurrent[stride] + pCurrent[stride + 1];
				povG = (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-stride + 1] * pCurrent[-stride + 1]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 1] * pCurrent[stride + 1]);
				arrayD[arrayRow, x] = (povG) * 9 - (arrayG[arrayRow, x] * arrayG[arrayRow, x]);

				//last column
				x = width - 1;
				pCurrent = pOrig + y * stride + x;

				arrayG[arrayRow, x] = pCurrent[-stride - 1] + pCurrent[-stride] + pCurrent[-1] + pCurrent[0] + pCurrent[stride - 1] + pCurrent[stride];
				povG = (pCurrent[-stride - 1] * pCurrent[-stride - 1]) + (pCurrent[-stride] * pCurrent[-stride]) + (pCurrent[-1] * pCurrent[-1]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride - 1] * pCurrent[stride - 1]) + (pCurrent[stride] * pCurrent[stride]);
				arrayD[arrayRow, x] = (povG) * 9 - (arrayG[arrayRow, x] * arrayG[arrayRow, x]);

				pCurrent = pOrig + y * stride + 1;

				int sumG = (pCurrent[-stride + 0] + pCurrent[0] + pCurrent[stride + 0]) * 65536 + pCurrent[-stride - 1] + pCurrent[-1] + pCurrent[stride - 1];
				int sum3G = pCurrent[-stride + 1] + pCurrent[1] + pCurrent[stride + 1];

				int pov1G = (pCurrent[-stride - 1] * pCurrent[-stride - 1]) + (pCurrent[-1] * pCurrent[-1]) + (pCurrent[stride - 1] * pCurrent[stride - 1]);
				int pov2G = (pCurrent[-stride + 0] * pCurrent[-stride + 0]) + (pCurrent[0] * pCurrent[0]) + (pCurrent[stride + 0] * pCurrent[stride + 0]);
				int pov3G = (pCurrent[-stride + 1] * pCurrent[-stride + 1]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[stride + 1] * pCurrent[stride + 1]);

				for (x = 1; x < width - 1; x++)
				{
					arrayG[arrayRow, x] = (sumG & 0xFFFF) + (sumG >> 16) + sum3G;

					arrayD[arrayRow, x] = (int)(((pov1G + pov2G + pov3G)) * 9 - (arrayG[arrayRow, x] * arrayG[arrayRow, x]));
					pCurrent += 1;

					sumG = (sumG >> 16) | (sum3G << 16);
					sum3G = pCurrent[-stride + 1] + pCurrent[+1] + pCurrent[+stride + 1];

					pov1G = pov2G;
					pov2G = pov3G;
					pov3G = (pCurrent[-stride + 1] * pCurrent[-stride + 1]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[stride + 1] * pCurrent[stride + 1]);
				}
			}
		}
		#endregion

		#region SetRotatingMaskRow5x5_8bpp()
		private unsafe static void SetRotatingMaskRow5x5_8bpp(int y, Size imageSize, byte* pOrig, int stride, int[,] arrayG, int[,] arrayD)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			int minDisperse, resultG;
			int bestOption;
			int previousDispense = 0;

			if (y == 0)
			{
				//first row
				for (x = 1; x < width - 1; x++)
				{
					minDisperse = arrayD[0, x - 1];
					resultG = arrayG[0, x - 1];

					if (arrayD[0, x] < minDisperse)
					{
						minDisperse = arrayD[0, x];
						resultG = arrayG[0, x];
					}

					if (arrayD[0, x + 1] < minDisperse)
					{
						minDisperse = arrayD[0, x + 1];
						resultG = arrayG[0, x + 1];
					}

					if (arrayD[1, x + 1] < minDisperse)
					{
						minDisperse = arrayD[1, x - 1];
						resultG = arrayG[1, x - 1];
					}

					if (arrayD[1, x + 1] < minDisperse)
					{
						minDisperse = arrayD[1, x];
						resultG = arrayG[1, x];
					}
					if (arrayD[1, x + 1] < minDisperse)
					{
						minDisperse = arrayD[1, x + 1];
						resultG = arrayG[1, x + 1];
					}

					pOrig[y * stride + x] = (byte)(resultG / 9);
				}
			}
			else if (y == imageSize.Height - 1)
			{
				int row1Index = ((y - 1) % 3);
				int row2Index = (y) % 3;

				//last row
				for (x = 1; x < width - 1; x++)
				{
					minDisperse = arrayD[row1Index, x - 1];
					resultG = arrayG[row1Index, x - 1];

					if (arrayD[row1Index, x] < minDisperse)
					{
						minDisperse = arrayD[row1Index, x];
						resultG = arrayG[row1Index, x];
					}

					if (arrayD[row1Index, x + 1] < minDisperse)
					{
						minDisperse = arrayD[row1Index, x + 1];
						resultG = arrayG[row1Index, x + 1];
					}

					if (arrayD[1, x + 1] < minDisperse)
					{
						minDisperse = arrayD[row2Index, x - 1];
						resultG = arrayG[row2Index, x - 1];
					}

					if (arrayD[row2Index, x + 1] < minDisperse)
					{
						minDisperse = arrayD[row2Index, x];
						resultG = arrayG[row2Index, x];
					}
					if (arrayD[row2Index, x + 1] < minDisperse)
					{
						minDisperse = arrayD[row2Index, x + 1];
						resultG = arrayG[row2Index, x + 1];
					}

					pOrig[y * stride + x] = (byte)(resultG / 9);
				}
			}
			else
			{
				//first pixel
				x = 0;

				minDisperse = arrayD[0, x];
				resultG = arrayG[0, x];

				if (arrayD[0, x + 1] < minDisperse)
				{
					minDisperse = arrayD[0, x + 1];
					resultG = arrayG[0, x + 1];
				}

				if (arrayD[1, x] < minDisperse)
				{
					minDisperse = arrayD[1, x];
					resultG = arrayG[1, x];
				}
				if (arrayD[1, x + 1] < minDisperse)
				{
					minDisperse = arrayD[1, x + 1];
					resultG = arrayG[1, x + 1];
				}

				if (arrayD[2, x] < minDisperse)
				{
					minDisperse = arrayD[2, x];
					resultG = arrayG[2, x];
				}

				if (arrayD[2, x + 1] < minDisperse)
				{
					minDisperse = arrayD[2, x + 1];
					resultG = arrayG[2, x + 1];
				}

				pOrig[y * stride + x] = (byte)(resultG / 9);

				//last pixels
				x = width - 1;

				minDisperse = arrayD[0, x - 1];
				resultG = arrayG[0, x - 1];

				if (arrayD[0, x] < minDisperse)
				{
					minDisperse = arrayD[0, x];
					resultG = arrayG[0, x];
				}

				if (arrayD[1, x - 1] < minDisperse)
				{
					minDisperse = arrayD[1, x - 1];
					resultG = arrayG[1, x - 1];
				}

				if (arrayD[1, x] < minDisperse)
				{
					minDisperse = arrayD[1, x];
					resultG = arrayG[1, x];
				}

				if (arrayD[2, x - 1] < minDisperse)
				{
					minDisperse = arrayD[2, x - 1];
					resultG = arrayG[2, x - 1];
				}

				if (arrayD[2, x] < minDisperse)
				{
					minDisperse = arrayD[2, x];
					resultG = arrayG[2, x];
				}

				pOrig[y * stride + x] = (byte)(resultG / 9);

				bestOption = -1;

				//middle pixels
				for (x = 1; x < width - 1; x++)
				{
					if ((bestOption % 65536) >= x - 1)
					{
						minDisperse = previousDispense;

						if (arrayD[0, x + 1] < minDisperse)
						{
							minDisperse = arrayD[0, x + 1];
							bestOption = x + 1;
						}

						if (arrayD[1, x + 1] < minDisperse)
						{
							minDisperse = arrayD[1, x + 1];
							bestOption = 65536 + x + 1;
						}

						if (arrayD[2, x + 1] < minDisperse)
						{
							minDisperse = arrayD[2, x + 1];
							bestOption = 131072 + x + 1;
						}
					}
					else
					{
						if (arrayD[0, x] < arrayD[0, x - 1])
						{
							minDisperse = arrayD[0, x];
							bestOption = x;
						}
						else
						{
							minDisperse = arrayD[0, x - 1];
							bestOption = x - 1;
						}

						if (arrayD[0, x + 1] < minDisperse)
						{
							minDisperse = arrayD[0, x + 1];
							bestOption = x + 1;
						}

						if (arrayD[1, x - 1] < minDisperse)
						{
							minDisperse = arrayD[1, x - 1];
							bestOption = 65536 + x - 1;
						}

						if (arrayD[1, x] < minDisperse)
						{
							minDisperse = arrayD[1, x];
							bestOption = 65536 + x;
						}
						if (arrayD[1, x + 1] < minDisperse)
						{
							minDisperse = arrayD[1, x + 1];
							bestOption = 65536 + x + 1;
						}

						if (arrayD[2, x - 1] < minDisperse)
						{
							minDisperse = arrayD[2, x - 1];
							bestOption = 131072 + x - 1;
						}

						if (arrayD[2, x] < minDisperse)
						{
							minDisperse = arrayD[2, x];
							bestOption = 131072 + x;
						}

						if (arrayD[2, x + 1] < minDisperse)
						{
							minDisperse = arrayD[2, x + 1];
							bestOption = 131072 + x + 1;
						}
					}

					previousDispense = minDisperse;

					pOrig[y * stride + x] = (byte)(arrayG[bestOption / 65536, bestOption & 0xFFFF] / 9);
				}
			}
		}
		#endregion

		#region RotatingMask3x3_Process()
		private static void RotatingMask3x3_Process(Bitmap bitmap, Rectangle clip)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = clip.Width;
				int height = clip.Height;

				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;

				unsafe
				{
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();

					if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
					{
						int[,] arrayR = new int[2, bitmapData.Width];
						int[,] arrayG = new int[2, bitmapData.Width];
						int[,] arrayB = new int[2, bitmapData.Width];
						int[,] arrayD = new int[2, bitmapData.Width];

						for (int y = 0; y < height; y++)
						{
							GetRotatingMaskRow3x3_24bpp(y, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);
							SetRotatingMaskRow3x3_24bpp(y, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);
						}
					}
					else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						int[,] arrayG = new int[2, bitmapData.Width];
						int[,] arrayD = new int[2, bitmapData.Width];

						for (int y = 0; y < height; y++)
						{
							GetRotatingMaskRow3x3_8bpp(y, clip.Size, pOrig, stride, arrayG, arrayD);
							SetRotatingMaskRow3x3_8bpp(y, clip.Size, pOrig, stride, arrayG, arrayD);
						}
					}
					else if (bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format32bppArgb)
					{
						int[,] arrayR = new int[2, bitmapData.Width];
						int[,] arrayG = new int[2, bitmapData.Width];
						int[,] arrayB = new int[2, bitmapData.Width];
						int[,] arrayD = new int[2, bitmapData.Width];

						for (int y = 0; y < height; y++)
						{
							GetRotatingMaskRow3x3_32bpp(y, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);
							SetRotatingMaskRow3x3_32bpp(y, clip.Size, pOrig, stride, arrayR, arrayG, arrayB, arrayD);
						}
					}
					else
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region GetRotatingMaskRow3x3_32bpp()
		private unsafe static void GetRotatingMaskRow3x3_32bpp(int y, Size imageSize, byte* pOrig, int stride, int[,] arrayR, int[,] arrayG, int[,] arrayB, int[,] arrayD)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			byte* pCurrent;

			int arrayRow = y % 2;
			int pov;

			if (y == imageSize.Height - 1)
			{
				int previousArrayRow = (y - 1) % 2;

				for (x = 0; x < width; x++)
				{
					arrayB[arrayRow, x] = arrayB[previousArrayRow, x];
					arrayG[arrayRow, x] = arrayG[previousArrayRow, x];
					arrayR[arrayRow, x] = arrayR[previousArrayRow, x];
					arrayD[arrayRow, x] = arrayD[previousArrayRow, x];
				}
			}
			else
			{
				pCurrent = pOrig + y * stride;

				for (x = 0; x < width - 1; x++)
				{
					arrayB[arrayRow, x] = pCurrent[0] + pCurrent[4] + pCurrent[stride] + pCurrent[stride + 4];
					arrayG[arrayRow, x] = pCurrent[1] + pCurrent[5] + pCurrent[stride + 1] + pCurrent[stride + 5];
					arrayR[arrayRow, x] = pCurrent[2] + pCurrent[6] + pCurrent[stride + 2] + pCurrent[stride + 6];
					pov = (pCurrent[0] * pCurrent[0]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[2] * pCurrent[2]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[5] * pCurrent[5]) + (pCurrent[6] * pCurrent[6]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 1] * pCurrent[stride + 1]) + (pCurrent[stride + 2] * pCurrent[stride + 2]) + (pCurrent[stride + 4] * pCurrent[stride + 4]) + (pCurrent[stride + 5] * pCurrent[stride + 5]) + (pCurrent[stride + 6] * pCurrent[stride + 6]);

					arrayD[arrayRow, x] = (pov * 4) - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);
					pCurrent += 4;
				}

				//last column
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = pCurrent[0] + pCurrent[stride];
				arrayG[arrayRow, x] = pCurrent[1] + pCurrent[stride + 1];
				arrayR[arrayRow, x] = pCurrent[2] + pCurrent[stride + 2];
				pov = (pCurrent[0] * pCurrent[0]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[2] * pCurrent[2]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 1] * pCurrent[stride + 1]) + (pCurrent[stride + 2] * pCurrent[stride + 2]);
				arrayD[arrayRow, x] = pov * 4 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);
			}
		}
		#endregion

		#region SetRotatingMaskRow3x3_32bpp()
		private unsafe static void SetRotatingMaskRow3x3_32bpp(int y, Size imageSize, byte* pOrig, int stride, int[,] arrayR, int[,] arrayG, int[,] arrayB, int[,] arrayD)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			int minDisperse, resultR, resultG, resultB;

			if (y == 0)
			{
				//first row
				for (x = 1; x < width; x++)
				{
					if (arrayD[0, x - 1] < arrayD[0, x])
					{
						pOrig[y * stride + x * 4] = (byte)(arrayB[0, x - 1] / 4);
						pOrig[y * stride + x * 4 + 1] = (byte)(arrayG[0, x - 1] / 4);
						pOrig[y * stride + x * 4 + 2] = (byte)(arrayR[0, x - 1] / 4);
					}
					else
					{
						pOrig[y * stride + x * 4] = (byte)(arrayB[0, x] / 4);
						pOrig[y * stride + x * 4 + 1] = (byte)(arrayG[0, x] / 4);
						pOrig[y * stride + x * 4 + 2] = (byte)(arrayR[0, x] / 4);
					}
				}

				pOrig[y * stride] = pOrig[y * stride + 4];
				pOrig[y * stride + 1] = pOrig[y * stride + 5];
				pOrig[y * stride + 2] = pOrig[y * stride + 6];
			}
			else
			{
				//first pixel
				x = 0;

				if (arrayD[0, x] < arrayD[1, x])
				{
					pOrig[y * stride] = (byte)(arrayB[0, x] / 4);
					pOrig[y * stride + 1] = (byte)(arrayG[0, x] / 4);
					pOrig[y * stride + 2] = (byte)(arrayR[0, x] / 4);
				}
				else
				{
					pOrig[y * stride] = (byte)(arrayB[1, x] / 4);
					pOrig[y * stride + 1] = (byte)(arrayG[1, x] / 4);
					pOrig[y * stride + 2] = (byte)(arrayR[1, x] / 4);
				}

				//middle pixels
				for (x = 1; x < width; x++)
				{
					if (arrayD[0, x] < arrayD[0, x - 1])
					{
						minDisperse = arrayD[0, x];
						resultR = arrayR[0, x];
						resultG = arrayG[0, x];
						resultB = arrayB[0, x];
					}
					else
					{
						minDisperse = arrayD[0, x - 1];
						resultR = arrayR[0, x - 1];
						resultG = arrayG[0, x - 1];
						resultB = arrayB[0, x - 1];
					}

					if (arrayD[1, x - 1] < minDisperse)
					{
						minDisperse = arrayD[1, x - 1];
						resultR = arrayR[1, x - 1];
						resultG = arrayG[1, x - 1];
						resultB = arrayB[1, x - 1];
					}

					if (arrayD[1, x] < minDisperse)
					{
						minDisperse = arrayD[1, x];
						resultR = arrayR[1, x];
						resultG = arrayG[1, x];
						resultB = arrayB[1, x];
					}

					pOrig[y * stride + x * 4] = (byte)(resultB / 4);
					pOrig[y * stride + x * 4 + 1] = (byte)(resultG / 4);
					pOrig[y * stride + x * 4 + 2] = (byte)(resultR / 4);
				}
			}
		}
		#endregion

		#region GetRotatingMaskRow3x3_24bpp()
		private unsafe static void GetRotatingMaskRow3x3_24bpp(int y, Size imageSize, byte* pOrig, int stride, int[,] arrayR, int[,] arrayG, int[,] arrayB, int[,] arrayD)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			byte* pCurrent;

			int arrayRow = y % 2;
			int pov;

			if (y == imageSize.Height - 1)
			{
				int previousArrayRow = (y - 1) % 2;
				
				for (x = 0; x < width; x++)
				{
					arrayB[arrayRow, x] = arrayB[previousArrayRow, x];
					arrayG[arrayRow, x] = arrayG[previousArrayRow, x];
					arrayR[arrayRow, x] = arrayR[previousArrayRow, x];
					arrayD[arrayRow, x] = arrayD[previousArrayRow, x];
				}
			}
			else
			{
				pCurrent = pOrig + y * stride;

				for (x = 0; x < width - 1; x++)
				{
					arrayB[arrayRow, x] = pCurrent[0] + pCurrent[3] + pCurrent[stride] + pCurrent[stride + 3];
					arrayG[arrayRow, x] = pCurrent[1] + pCurrent[4] + pCurrent[stride + 1] + pCurrent[stride + 4];
					arrayR[arrayRow, x] = pCurrent[2] + pCurrent[5] + pCurrent[stride + 2] + pCurrent[stride + 5];
					pov = (pCurrent[0] * pCurrent[0]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[2] * pCurrent[2]) + (pCurrent[3] * pCurrent[3]) + (pCurrent[4] * pCurrent[4]) + (pCurrent[5] * pCurrent[5]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 1] * pCurrent[stride + 1]) + (pCurrent[stride + 2] * pCurrent[stride + 2]) + (pCurrent[stride + 3] * pCurrent[stride + 3]) + (pCurrent[stride + 4] * pCurrent[stride + 4]) + (pCurrent[stride + 5] * pCurrent[stride + 5]);

					arrayD[arrayRow, x] = (pov * 4) - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);
					pCurrent += 3;
				}
				
				//last column
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = pCurrent[0] + pCurrent[stride];
				arrayG[arrayRow, x] = pCurrent[1] + pCurrent[stride+1];
				arrayR[arrayRow, x] = pCurrent[2] + pCurrent[stride+2];
				pov = (pCurrent[0] * pCurrent[0]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[2] * pCurrent[2]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride+1] * pCurrent[stride+1]) + (pCurrent[stride+2] * pCurrent[stride+2]);
				arrayD[arrayRow, x] = pov * 4 - (arrayR[arrayRow, x] * arrayR[arrayRow, x]) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]) - (arrayB[arrayRow, x] * arrayB[arrayRow, x]);
			}
		}
		#endregion

		#region SetRotatingMaskRow3x3_24bpp()
		private unsafe static void SetRotatingMaskRow3x3_24bpp(int y, Size imageSize, byte* pOrig, int stride, int[,] arrayR, int[,] arrayG, int[,] arrayB, int[,] arrayD)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			int minDisperse, resultR, resultG, resultB;

			if (y == 0)
			{
				//first row
				for (x = 1; x < width; x++)
				{
					if (arrayD[0, x - 1] < arrayD[0, x])
					{
						pOrig[y * stride + x * 3] = (byte)(arrayB[0, x - 1] / 4);
						pOrig[y * stride + x * 3 + 1] = (byte)(arrayG[0, x - 1] / 4);
						pOrig[y * stride + x * 3 + 2] = (byte)(arrayR[0, x - 1] / 4);
					}
					else
					{
						pOrig[y * stride + x * 3] = (byte)(arrayB[0, x] / 4);
						pOrig[y * stride + x * 3 + 1] = (byte)(arrayG[0, x] / 4);
						pOrig[y * stride + x * 3 + 2] = (byte)(arrayR[0, x] / 4);
					}
				}

				pOrig[y * stride] = pOrig[y * stride + 3];
				pOrig[y * stride + 1] = pOrig[y * stride + 4];
				pOrig[y * stride + 2] = pOrig[y * stride + 5];
			}
			else
			{
				//first pixel
				x = 0;

				if (arrayD[0, x] < arrayD[1, x])
				{
					pOrig[y * stride] = (byte)(arrayB[0, x] / 4);
					pOrig[y * stride + 1] = (byte)(arrayG[0, x] / 4);
					pOrig[y * stride + 2] = (byte)(arrayR[0, x] / 4);
				}
				else
				{
					pOrig[y * stride] = (byte)(arrayB[1, x] / 4);
					pOrig[y * stride + 1] = (byte)(arrayG[1, x] / 4);
					pOrig[y * stride + 2] = (byte)(arrayR[1, x] / 4);
				}

				//middle pixels
				for (x = 1; x < width; x++)
				{
					if (arrayD[0, x] < arrayD[0, x - 1])
					{
						minDisperse = arrayD[0, x];
						resultR = arrayR[0, x];
						resultG = arrayG[0, x];
						resultB = arrayB[0, x];
					}
					else
					{
						minDisperse = arrayD[0, x - 1];
						resultR = arrayR[0, x - 1];
						resultG = arrayG[0, x - 1];
						resultB = arrayB[0, x - 1];
					}

					if (arrayD[1, x - 1] < minDisperse)
					{
						minDisperse = arrayD[1, x - 1];
						resultR = arrayR[1, x - 1];
						resultG = arrayG[1, x - 1];
						resultB = arrayB[1, x - 1];
					}

					if (arrayD[1, x] < minDisperse)
					{
						minDisperse = arrayD[1, x];
						resultR = arrayR[1, x];
						resultG = arrayG[1, x];
						resultB = arrayB[1, x];
					}

					pOrig[y * stride + x * 3] = (byte)(resultB / 4);
					pOrig[y * stride + x * 3 + 1] = (byte)(resultG / 4);
					pOrig[y * stride + x * 3 + 2] = (byte)(resultR / 4);
				}
			}
		}
		#endregion

		#region GetRotatingMaskRow3x3_8bpp()
		private unsafe static void GetRotatingMaskRow3x3_8bpp(int y, Size imageSize, byte* pOrig, int stride, int[,] arrayG, int[,] arrayD)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			byte* pCurrent;

			int arrayRow = y % 2;
			int pov;

			if (y == imageSize.Height - 1)
			{
				int previousArrayRow = (y - 1) % 2;

				for (x = 0; x < width; x++)
				{
					arrayG[arrayRow, x] = arrayG[previousArrayRow, x];
					arrayD[arrayRow, x] = arrayD[previousArrayRow, x];
				}
			}
			else
			{
				pCurrent = pOrig + y * stride;

				for (x = 0; x < width - 1; x++)
				{
					arrayG[arrayRow, x] = pCurrent[0] + pCurrent[1] + pCurrent[stride + 0] + pCurrent[stride + 1];
					pov = (pCurrent[0] * pCurrent[0]) + (pCurrent[1] * pCurrent[1]) + (pCurrent[stride] * pCurrent[stride]) + (pCurrent[stride + 1] * pCurrent[stride + 1]);

					arrayD[arrayRow, x] = (pov * 4) - (arrayG[arrayRow, x] * arrayG[arrayRow, x]);
					pCurrent ++;
				}

				//last column
				x = width - 1;
				pCurrent = pOrig + y * stride + x;

				arrayG[arrayRow, x] = pCurrent[0] + pCurrent[stride];
				pov = (pCurrent[0] * pCurrent[0]) + (pCurrent[stride] * pCurrent[stride]);
				arrayD[arrayRow, x] = pov * 4 - (arrayG[arrayRow, x] * arrayG[arrayRow, x]);
			}
		}
		#endregion

		#region SetRotatingMaskRow3x3_8bpp()
		private unsafe static void SetRotatingMaskRow3x3_8bpp(int y, Size imageSize, byte* pOrig, int stride, int[,] arrayG, int[,] arrayD)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			int minDisperse, resultG;

			if (y == 0)
			{
				//first row
				for (x = 1; x < width; x++)
				{
					if (arrayD[0, x - 1] < arrayD[0, x])
					{
						pOrig[y * stride + x] = (byte)(arrayG[0, x - 1] / 4);
					}
					else
					{
						pOrig[y * stride + x] = (byte)(arrayG[0, x] / 4);
					}
				}

				pOrig[y * stride] = pOrig[y * stride + 1];
			}
			else
			{
				//first pixel
				x = 0;

				if (arrayD[0, x] < arrayD[1, x])
				{
					pOrig[y * stride] = (byte)(arrayG[0, x] / 4);
				}
				else
				{
					pOrig[y * stride] = (byte)(arrayG[1, x] / 4);
				}

				//middle pixels
				for (x = 1; x < width; x++)
				{
					if (arrayD[0, x] < arrayD[0, x - 1])
					{
						minDisperse = arrayD[0, x];
						resultG = arrayG[0, x];
					}
					else
					{
						minDisperse = arrayD[0, x - 1];
						resultG = arrayG[0, x - 1];
					}

					if (arrayD[1, x - 1] < minDisperse)
					{
						minDisperse = arrayD[1, x - 1];
						resultG = arrayG[1, x - 1];
					}

					if (arrayD[1, x] < minDisperse)
					{
						minDisperse = arrayD[1, x];
						resultG = arrayG[1, x];
					}

					pOrig[y * stride + x] = (byte)(resultG / 4);
				}
			}
		}
		#endregion

		#region Averaging3x3_Process()
		private static void Averaging3x3_Process(Bitmap bitmap, Rectangle clip)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = clip.Width;
				int height = clip.Height;

				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;

				unsafe
				{
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();

					if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
					{
						byte[,] arrayR = new byte[3, bitmapData.Width];
						byte[,] arrayG = new byte[3, bitmapData.Width];
						byte[,] arrayB = new byte[3, bitmapData.Width];

						//first row
						GetAveragingRow3x3_24bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
						GetAveragingRow3x3_24bpp(1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);

						SetAveragingRow3x3_24bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);

						//Set middle part
						for (int y = 1; y < height - 1; y++)
						{
							GetAveragingRow3x3_24bpp(y + 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
							SetAveragingRow3x3_24bpp(y, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
						}

						//last row
						SetAveragingRow3x3_24bpp(height - 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
					}
					else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						byte[,] array = new byte[3, bitmapData.Width];

						//first row
						GetAveragingRow3x3_8bpp(0, clip.Size, pOrig, stride, array);
						GetAveragingRow3x3_8bpp(1, clip.Size, pOrig, stride, array);

						SetAveragingRow3x3_8bpp(0, clip.Size, pOrig, stride, array);

						//Set middle part
						for (int y = 1; y < height - 1; y++)
						{
							GetAveragingRow3x3_8bpp(y + 1, clip.Size, pOrig, stride, array);
							SetAveragingRow3x3_8bpp(y, clip.Size, pOrig, stride, array);
						}

						//last row
						SetAveragingRow3x3_8bpp(height - 1, clip.Size, pOrig, stride, array);
					}
					else if (bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format32bppArgb)
					{
						byte[,] arrayR = new byte[3, bitmapData.Width];
						byte[,] arrayG = new byte[3, bitmapData.Width];
						byte[,] arrayB = new byte[3, bitmapData.Width];

						//first row
						GetAveragingRow3x3_32bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
						GetAveragingRow3x3_32bpp(1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);

						SetAveragingRow3x3_32bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);

						//Set middle part
						for (int y = 1; y < height - 1; y++)
						{
							GetAveragingRow3x3_32bpp(y + 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
							SetAveragingRow3x3_32bpp(y, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
						}

						//last row
						SetAveragingRow3x3_32bpp(height - 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
					}
					else
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region GetAveragingRow3x3_32bpp()
		private unsafe static void GetAveragingRow3x3_32bpp(int y, Size imageSize, byte* pOrig, int stride, byte[,] arrayR, byte[,] arrayG, byte[,] arrayB)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			byte* pCurrent;

			int arrayRow = y % 3;

			//get UL corner
			if (y == 0)
			{
				//left
				x = 0;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = (byte)((pCurrent[0] + pCurrent[4] + pCurrent[stride] + pCurrent[stride + 4]) >> 2);
				arrayG[arrayRow, x] = (byte)((pCurrent[1] + pCurrent[5] + pCurrent[stride + 1] + pCurrent[stride + 5]) >> 2);
				arrayR[arrayRow, x] = (byte)((pCurrent[2] + pCurrent[6] + pCurrent[stride + 2] + pCurrent[stride + 6]) >> 2);

				//right
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = (byte)((pCurrent[-4] + pCurrent[0] + pCurrent[stride - 4] + pCurrent[stride]) >> 2);
				arrayG[arrayRow, x] = (byte)((pCurrent[-3] + pCurrent[1] + pCurrent[stride - 3] + pCurrent[stride + 1]) >> 2);
				arrayR[arrayRow, x] = (byte)((pCurrent[-2] + pCurrent[2] + pCurrent[stride - 2] + pCurrent[stride + 2]) >> 2);

				//get first row
				pCurrent = pOrig + y * stride + 4;

				for (x = 1; x < width - 1; x++)
				{
					arrayB[arrayRow, x] = (byte)((pCurrent[-4] + pCurrent[0] + pCurrent[4] + pCurrent[stride - 4] + pCurrent[stride] + pCurrent[stride + 4]) / 6);
					arrayG[arrayRow, x] = (byte)((pCurrent[-3] + pCurrent[1] + pCurrent[5] + pCurrent[stride - 3] + pCurrent[stride + 1] + pCurrent[stride + 5]) / 6);
					arrayR[arrayRow, x] = (byte)((pCurrent[-2] + pCurrent[2] + pCurrent[6] + pCurrent[stride - 2] + pCurrent[stride + 2] + pCurrent[stride + 6]) / 6);

					pCurrent += 4;
				}
			}
			else if (y == imageSize.Height - 1)
			{
				//left
				x = 0;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = (byte)((pCurrent[-stride] + pCurrent[-stride + 4] + pCurrent[0] + pCurrent[4]) >> 2);
				arrayG[arrayRow, x] = (byte)((pCurrent[-stride + 1] + pCurrent[-stride + 5] + pCurrent[1] + pCurrent[5]) >> 2);
				arrayR[arrayRow, x] = (byte)((pCurrent[-stride + 2] + pCurrent[-stride + 6] + pCurrent[2] + pCurrent[6]) >> 2);

				//right
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = (byte)((pCurrent[-4] + pCurrent[0] + pCurrent[-stride - 4] + pCurrent[-stride]) >> 2);
				arrayG[arrayRow, x] = (byte)((pCurrent[-3] + pCurrent[1] + pCurrent[-stride - 3] + pCurrent[-stride + 1]) >> 2);
				arrayR[arrayRow, x] = (byte)((pCurrent[-2] + pCurrent[2] + pCurrent[-stride - 2] + pCurrent[-stride + 2]) >> 2);

				//get first row
				pCurrent = pOrig + y * stride + 4;

				for (x = 1; x < width - 1; x++)
				{
					arrayB[arrayRow, x] = (byte)((pCurrent[-4] + pCurrent[0] + pCurrent[4] + pCurrent[-stride - 4] + pCurrent[-stride] + pCurrent[-stride + 4]) / 6);
					arrayG[arrayRow, x] = (byte)((pCurrent[-3] + pCurrent[1] + pCurrent[5] + pCurrent[-stride - 3] + pCurrent[-stride + 1] + pCurrent[-stride + 5]) / 6);
					arrayR[arrayRow, x] = (byte)((pCurrent[-2] + pCurrent[2] + pCurrent[6] + pCurrent[-stride - 2] + pCurrent[-stride + 2] + pCurrent[-stride + 6]) / 6);

					pCurrent += 4;
				}
			}
			else
			{
				//column 0
				x = 0;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = (byte)((pCurrent[-stride] + pCurrent[-stride + 4] + pCurrent[0] + pCurrent[4] + pCurrent[stride] + pCurrent[stride + 4]) / 6);
				arrayG[arrayRow, x] = (byte)((pCurrent[-stride + 1] + pCurrent[-stride + 5] + pCurrent[1] + pCurrent[5] + pCurrent[stride + 1] + pCurrent[stride + 5]) / 6);
				arrayR[arrayRow, x] = (byte)((pCurrent[-stride + 2] + pCurrent[-stride + 6] + pCurrent[2] + pCurrent[6] + pCurrent[stride + 2] + pCurrent[stride + 6]) / 6);

				//last column
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 4;

				arrayB[arrayRow, x] = (byte)((pCurrent[-stride - 4] + pCurrent[-stride] + pCurrent[-4] + pCurrent[0] + pCurrent[stride - 4] + pCurrent[stride]) / 6);
				arrayG[arrayRow, x] = (byte)((pCurrent[-stride - 3] + pCurrent[-stride + 1] + pCurrent[-3] + pCurrent[1] + pCurrent[stride - 3] + pCurrent[stride + 1]) / 6);
				arrayR[arrayRow, x] = (byte)((pCurrent[-stride - 2] + pCurrent[-stride + 2] + pCurrent[-2] + pCurrent[2] + pCurrent[stride - 2] + pCurrent[stride + 2]) / 6);

				//get second row
				pCurrent = pOrig + y * stride + 4;

				int sumB = ((pCurrent[-stride] + pCurrent[0] + pCurrent[+stride])) * 65536 + (pCurrent[-stride - 4] + pCurrent[-4] + pCurrent[stride - 4]);
				int sumG = ((pCurrent[-stride + 1] + pCurrent[1] + pCurrent[stride + 1])) * 65536 + (pCurrent[-stride - 3] + pCurrent[-3] + pCurrent[stride - 3]);
				int sumR = ((pCurrent[-stride + 2] + pCurrent[2] + pCurrent[stride + 2])) * 65536 + (pCurrent[-stride - 2] + pCurrent[-2] + pCurrent[stride - 2]);
				int sum3B, sum3G, sum3R;

				for (x = 1; x < width - 1; x++)
				{
					sum3B = (pCurrent[-stride + 4] + pCurrent[+4] + pCurrent[+stride + 4]);
					sum3G = (pCurrent[-stride + 5] + pCurrent[+5] + pCurrent[+stride + 5]);
					sum3R = (pCurrent[-stride + 6] + pCurrent[+6] + pCurrent[+stride + 6]);

					arrayB[arrayRow, x] = (byte)(((sumB & 0xFFFF) + (sumB >> 16) + sum3B) / 9);
					arrayG[arrayRow, x] = (byte)(((sumG & 0xFFFF) + (sumG >> 16) + sum3G) / 9);
					arrayR[arrayRow, x] = (byte)(((sumR & 0xFFFF) + (sumR >> 16) + sum3R) / 9);

					sumB = (sumB >> 16) | (sum3B << 16);
					sumG = (sumG >> 16) | (sum3G << 16);
					sumR = (sumR >> 16) | (sum3R << 16);

					pCurrent += 4;
				}
			}
		}
		#endregion

		#region SetAveragingRow3x3_32bpp()
		private unsafe static void SetAveragingRow3x3_32bpp(int y, Size imageSize, byte* pOrig, int stride, byte[,] arrayR, byte[,] arrayG, byte[,] arrayB)
		{
			int width = imageSize.Width;
			int arrayRow = y % 3;

			for (int x = 0; x < width; x++)
			{
				pOrig[y * stride + x * 4] = arrayB[arrayRow, x];
				pOrig[y * stride + x * 4 + 1] = arrayG[arrayRow, x];
				pOrig[y * stride + x * 4 + 2] = arrayR[arrayRow, x];
			}
		}
		#endregion

		#region GetAveragingRow3x3_24bpp()
		private unsafe static void GetAveragingRow3x3_24bpp(int y, Size imageSize, byte* pOrig, int stride, byte[,] arrayR, byte[,] arrayG, byte[,] arrayB)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			byte* pCurrent;

			int arrayRow = y % 3;

			//get UL corner
			if (y == 0)
			{
				//left
				x = 0;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = (byte)((pCurrent[0] + pCurrent[3] + pCurrent[stride] + pCurrent[stride + 3]) >> 2);
				arrayG[arrayRow, x] = (byte)((pCurrent[1] + pCurrent[4] + pCurrent[stride + 1] + pCurrent[stride + 4]) >> 2);
				arrayR[arrayRow, x] = (byte)((pCurrent[2] + pCurrent[5] + pCurrent[stride + 2] + pCurrent[stride + 5]) >> 2);

				//right
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = (byte)((pCurrent[-3] + pCurrent[0] + pCurrent[stride - 3] + pCurrent[stride]) >> 2);
				arrayG[arrayRow, x] = (byte)((pCurrent[-2] + pCurrent[1] + pCurrent[stride - 2] + pCurrent[stride + 1]) >> 2);
				arrayR[arrayRow, x] = (byte)((pCurrent[-1] + pCurrent[2] + pCurrent[stride - 1] + pCurrent[stride + 2]) >> 2);

				//get first row
				pCurrent = pOrig + y * stride + 3;

				for (x = 1; x < width - 1; x++)
				{
					arrayB[arrayRow, x] = (byte)((pCurrent[-3] + pCurrent[0] + pCurrent[3] + pCurrent[stride - 3] + pCurrent[stride] + pCurrent[stride + 3]) / 6);
					arrayG[arrayRow, x] = (byte)((pCurrent[-2] + pCurrent[1] + pCurrent[4] + pCurrent[stride - 2] + pCurrent[stride + 1] + pCurrent[stride + 4]) / 6);
					arrayR[arrayRow, x] = (byte)((pCurrent[-1] + pCurrent[2] + pCurrent[5] + pCurrent[stride - 1] + pCurrent[stride + 2] + pCurrent[stride + 5]) / 6);

					pCurrent += 3;
				}
			}
			else if (y == imageSize.Height - 1)
			{
				//left
				x = 0;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = (byte)((pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[0] + pCurrent[3]) >> 2);
				arrayG[arrayRow, x] = (byte)((pCurrent[-stride + 1] + pCurrent[-stride + 4] + pCurrent[1] + pCurrent[4]) >> 2);
				arrayR[arrayRow, x] = (byte)((pCurrent[-stride + 2] + pCurrent[-stride + 5] + pCurrent[2] + pCurrent[5]) >> 2);

				//right
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = (byte)((pCurrent[-3] + pCurrent[0] + pCurrent[-stride - 3] + pCurrent[-stride]) >> 2);
				arrayG[arrayRow, x] = (byte)((pCurrent[-2] + pCurrent[1] + pCurrent[-stride - 2] + pCurrent[-stride + 1]) >> 2);
				arrayR[arrayRow, x] = (byte)((pCurrent[-1] + pCurrent[2] + pCurrent[-stride - 1] + pCurrent[-stride + 2]) >> 2);

				//get first row
				pCurrent = pOrig + y * stride + 3;

				for (x = 1; x < width - 1; x++)
				{
					arrayB[arrayRow, x] = (byte)((pCurrent[-3] + pCurrent[0] + pCurrent[3] + pCurrent[-stride - 3] + pCurrent[-stride] + pCurrent[-stride + 3]) / 6);
					arrayG[arrayRow, x] = (byte)((pCurrent[-2] + pCurrent[1] + pCurrent[4] + pCurrent[-stride - 2] + pCurrent[-stride + 1] + pCurrent[-stride + 4]) / 6);
					arrayR[arrayRow, x] = (byte)((pCurrent[-1] + pCurrent[2] + pCurrent[5] + pCurrent[-stride - 1] + pCurrent[-stride + 2] + pCurrent[-stride + 5]) / 6);

					pCurrent += 3;
				}
			}
			else
			{
				//column 0
				x = 0;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = (byte)((pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[0] + pCurrent[3] + pCurrent[stride] + pCurrent[stride + 3]) / 6);
				arrayG[arrayRow, x] = (byte)((pCurrent[-stride + 1] + pCurrent[-stride + 4] + pCurrent[1] + pCurrent[4] + pCurrent[stride + 1] + pCurrent[stride + 4]) / 6);
				arrayR[arrayRow, x] = (byte)((pCurrent[-stride + 2] + pCurrent[-stride + 5] + pCurrent[2] + pCurrent[5] + pCurrent[stride + 2] + pCurrent[stride + 5]) / 6);

				//last column
				x = width - 1;
				pCurrent = pOrig + y * stride + x * 3;

				arrayB[arrayRow, x] = (byte)((pCurrent[-stride - 3] + pCurrent[-stride] + pCurrent[-3] + pCurrent[0] + pCurrent[stride - 3] + pCurrent[stride]) / 6);
				arrayG[arrayRow, x] = (byte)((pCurrent[-stride - 2] + pCurrent[-stride + 1] + pCurrent[-2] + pCurrent[1] + pCurrent[stride - 2] + pCurrent[stride + 1]) / 6);
				arrayR[arrayRow, x] = (byte)((pCurrent[-stride - 1] + pCurrent[-stride + 2] + pCurrent[-1] + pCurrent[2] + pCurrent[stride - 1] + pCurrent[stride + 2]) / 6);

				//get second row
				pCurrent = pOrig + y * stride + 3;

				int sumB = ((pCurrent[-stride] + pCurrent[0] + pCurrent[+stride])) * 65536 + (pCurrent[-stride - 3] + pCurrent[-3] + pCurrent[stride - 3]);
				int sumG = ((pCurrent[-stride + 1] + pCurrent[1] + pCurrent[stride + 1])) * 65536 + (pCurrent[-stride - 2] + pCurrent[-2] + pCurrent[stride - 2]);
				int sumR = ((pCurrent[-stride + 2] + pCurrent[2] + pCurrent[stride + 2])) * 65536 + (pCurrent[-stride - 1] + pCurrent[-1] + pCurrent[stride - 1]);
				int sum3B, sum3G, sum3R;

				for (x = 1; x < width - 1; x++)
				{
					sum3B = (pCurrent[-stride + 3] + pCurrent[+3] + pCurrent[+stride + 3]);
					sum3G = (pCurrent[-stride + 4] + pCurrent[+4] + pCurrent[+stride + 4]);
					sum3R = (pCurrent[-stride + 5] + pCurrent[+5] + pCurrent[+stride + 5]);
					
					arrayB[arrayRow, x] = (byte)(((sumB & 0xFFFF) + (sumB >> 16) + sum3B)/9);
					arrayG[arrayRow, x] = (byte)(((sumG & 0xFFFF) + (sumG >> 16) + sum3G)/9);
					arrayR[arrayRow, x] = (byte)(((sumR & 0xFFFF) + (sumR >> 16) + sum3R)/9);

					sumB = (sumB >> 16) | (sum3B << 16);
					sumG = (sumG >> 16) | (sum3G << 16);
					sumR = (sumR >> 16) | (sum3R << 16);
					
					pCurrent += 3;
				}
			}
		}
		#endregion

		#region SetAveragingRow3x3_24bpp()
		private unsafe static void SetAveragingRow3x3_24bpp(int y, Size imageSize, byte* pOrig, int stride, byte[,] arrayR, byte[,] arrayG, byte[,] arrayB)
		{
			int width = imageSize.Width;
			int arrayRow = y % 3;

			for (int x = 0; x < width; x++)
			{
				pOrig[y * stride + x * 3] = arrayB[arrayRow, x];
				pOrig[y * stride + x * 3 + 1] = arrayG[arrayRow, x];
				pOrig[y * stride + x * 3 + 2] = arrayR[arrayRow, x];
			}
		}
		#endregion

		#region GetAveragingRow3x3_8bpp()
		private unsafe static void GetAveragingRow3x3_8bpp(int y, Size imageSize, byte* pOrig, int stride, byte[,] array)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			byte* pCurrent;

			int arrayRow = y % 3;

			//get UL corner
			if (y == 0)
			{
				//left
				x = 0;
				pCurrent = pOrig + y * stride + x;

				array[arrayRow, x] = (byte)((pCurrent[0] + pCurrent[1] + pCurrent[stride] + pCurrent[stride + 1]) >> 2);

				//right
				x = width - 1;
				pCurrent = pOrig + y * stride + x;
				array[arrayRow, x] = (byte)((pCurrent[-1] + pCurrent[0] + pCurrent[stride - 1] + pCurrent[stride]) >> 2);

				//get first row
				pCurrent = pOrig + y * stride + 1;

				for (x = 1; x < width - 1; x++)
				{
					array[arrayRow, x] = (byte)((pCurrent[-1] + pCurrent[0] + pCurrent[1] + pCurrent[stride - 1] + pCurrent[stride] + pCurrent[stride + 1]) / 6);
					pCurrent += 1;
				}
			}
			else if (y == imageSize.Height - 1)
			{
				//left
				x = 0;
				pCurrent = pOrig + y * stride + x;
				array[arrayRow, x] = (byte)((pCurrent[-stride] + pCurrent[-stride + 1] + pCurrent[0] + pCurrent[1]) >> 2);

				//right
				x = width - 1;
				pCurrent = pOrig + y * stride + x;
				array[arrayRow, x] = (byte)((pCurrent[-1] + pCurrent[0] + pCurrent[-stride - 1] + pCurrent[-stride]) >> 2);

				//get first row
				pCurrent = pOrig + y * stride + 1;

				for (x = 1; x < width - 1; x++)
				{
					array[arrayRow, x] = (byte)((pCurrent[-1] + pCurrent[0] + pCurrent[1] + pCurrent[-stride - 1] + pCurrent[-stride] + pCurrent[-stride + 1]) / 6);
					pCurrent += 1;
				}
			}
			else
			{
				//column 0
				x = 0;
				pCurrent = pOrig + y * stride + x;
				array[arrayRow, x] = (byte)((pCurrent[-stride] + pCurrent[-stride + 1] + pCurrent[0] + pCurrent[1] + pCurrent[stride] + pCurrent[stride + 1]) / 6);

				//last column
				x = width - 1;
				pCurrent = pOrig + y * stride + x;

				array[arrayRow, x] = (byte)((pCurrent[-stride - 1] + pCurrent[-stride] + pCurrent[-1] + pCurrent[0] + pCurrent[stride - 1] + pCurrent[stride]) / 6);

				//get second row
				pCurrent = pOrig + y * stride + 1;

				int sumB = ((pCurrent[-stride] + pCurrent[0] + pCurrent[+stride])) * 65536 + (pCurrent[-stride - 1] + pCurrent[-1] + pCurrent[stride - 1]);
				int sum3B;

				for (x = 1; x < width - 1; x++)
				{
					sum3B = (pCurrent[-stride + 1] + pCurrent[+1] + pCurrent[+stride + 1]);
					array[arrayRow, x] = (byte)(((sumB & 0xFFFF) + (sumB >> 16) + sum3B) / 9);

					sumB = (sumB >> 16) | (sum3B << 16);
					pCurrent += 1;
				}
			}
		}
		#endregion

		#region SetAveragingRow3x3_8bpp()
		private unsafe static void SetAveragingRow3x3_8bpp(int y, Size imageSize, byte* pOrig, int stride, byte[,] array)
		{
			int width = imageSize.Width;
			int arrayRow = y % 3;

			for (int x = 0; x < width; x++)
				pOrig[y * stride + x] = array[arrayRow, x];
		}
		#endregion

		#region Averaging5x5_24bpp()
		private static void Averaging5x5_24bpp(Bitmap bitmap, Rectangle clip)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = clip.Width;
				int height = clip.Height;

				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;

				unsafe
				{
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();

					byte[,] arrayR = new byte[5, bitmapData.Width];
					byte[,] arrayG = new byte[5, bitmapData.Width];
					byte[,] arrayB = new byte[5, bitmapData.Width];

					//first row
					GetAveragingRow5x5_24bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
					GetAveragingRow5x5_24bpp(1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
					GetAveragingRow5x5_24bpp(2, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
					GetAveragingRow5x5_24bpp(3, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
					GetAveragingRow5x5_24bpp(4, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);

					SetAveragingRow5x5_24bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
					SetAveragingRow5x5_24bpp(1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);

					//Set middle part
					for (int y = 3; y < height - 2; y++)
					{
						GetAveragingRow5x5_24bpp(y + 2, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
						SetAveragingRow5x5_24bpp(y, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
					}

					//last row
					SetAveragingRow5x5_24bpp(height - 2, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
					SetAveragingRow5x5_24bpp(height - 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region GetAveragingRow5x5_24bpp()
		private unsafe static void GetAveragingRow5x5_24bpp(int y, Size imageSize, byte* pOrig, int stride, byte[,] arrayR, byte[,] arrayG, byte[,] arrayB)
		{
			int x;
			int width = imageSize.Width;
			int height = imageSize.Height;

			byte* pCurrent;

			int arrayRow = y % 5;
			byte r, g, b;
			byte r0, g0, b0;
			int rSum, gSum, bSum;
			byte validPointNeighbours;

			//get UL corner
			if (y < 2)
			{
				pCurrent = pOrig + y * stride;

				for (x = 0; x < width; x++)
				{
					rSum = 0; gSum = 0; bSum = 0;
					validPointNeighbours = 0;

					b0 = pOrig[y * stride + x * 3];
					g0 = pOrig[y * stride + x * 3 + 1];
					r0 = pOrig[y * stride + x * 3 + 2];

					for (int i = -2; i <= 2; i++)
						for (int j = -2; j <= 2; j++)
						{
							if (y + i >= 0 && x + j >= 0 && x + j < width)
							{
								b = pOrig[(y + i) * stride + (x + j) * 3];
								g = pOrig[(y + i) * stride + (x + j) * 3 + 1];
								r = pOrig[(y + i) * stride + (x + j) * 3 + 2];

								//if ((r - maxDelta < r0 && r + maxDelta > r0) && (g - maxDelta < g0 && g + maxDelta > g0) && (b - maxDelta < b0 && b + maxDelta > b0))
								{
									rSum += r;
									gSum += g;
									bSum += b;
									validPointNeighbours++;
								}
							}

						}

					arrayB[arrayRow, x] = (byte)(bSum / validPointNeighbours);
					arrayG[arrayRow, x] = (byte)(gSum / validPointNeighbours);
					arrayR[arrayRow, x] = (byte)(rSum / validPointNeighbours);
					pCurrent += 3;
				}
			}
			else if (y >= imageSize.Height - 2)
			{
				pCurrent = pOrig + y * stride;

				for (x = 0; x < width; x++)
				{
					rSum = 0; gSum = 0; bSum = 0;
					validPointNeighbours = 0;

					b0 = pOrig[y * stride + x * 3];
					g0 = pOrig[y * stride + x * 3 + 1];
					r0 = pOrig[y * stride + x * 3 + 2];

					for (int i = -2; i <= 2; i++)
						for (int j = -2; j <= 2; j++)
						{
							if (y + i < height && x + j >= 0 && x + j < width)
							{
								b = pOrig[(y + i) * stride + (x + j) * 3];
								g = pOrig[(y + i) * stride + (x + j) * 3 + 1];
								r = pOrig[(y + i) * stride + (x + j) * 3 + 2];

								//if ((r - maxDelta < r0 && r + maxDelta > r0) && (g - maxDelta < g0 && g + maxDelta > g0) && (b - maxDelta < b0 && b + maxDelta > b0))
								{
									rSum += r;
									gSum += g;
									bSum += b;
									validPointNeighbours++;
								}
							}

						}

					arrayB[arrayRow, x] = (byte)(bSum / validPointNeighbours);
					arrayG[arrayRow, x] = (byte)(gSum / validPointNeighbours);
					arrayR[arrayRow, x] = (byte)(rSum / validPointNeighbours);
					pCurrent += 3;
				}
			}
			else
			{
				//column 0, 1
				x = 0;
				pCurrent = pOrig + y * stride;

				arrayB[arrayRow, x] = (byte)((pCurrent[-2 * stride] + pCurrent[-2 * stride + 3] + pCurrent[-2 * stride + 6] + pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[-stride + 6] + pCurrent[0] + pCurrent[3] + pCurrent[6] + pCurrent[stride] + pCurrent[stride + 3] + pCurrent[stride + 6] + pCurrent[2*stride] + pCurrent[2*stride + 3] + pCurrent[2*stride + 6]) / 15);
				pCurrent++;
				arrayG[arrayRow, x] = (byte)((pCurrent[-2 * stride] + pCurrent[-2 * stride + 3] + pCurrent[-2 * stride + 6] + pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[-stride + 6] + pCurrent[0] + pCurrent[3] + pCurrent[6] + pCurrent[stride] + pCurrent[stride + 3] + pCurrent[stride + 6] + pCurrent[2 * stride] + pCurrent[2 * stride + 3] + pCurrent[2 * stride + 6]) / 15);
				pCurrent++;
				arrayR[arrayRow, x] = (byte)((pCurrent[-2 * stride] + pCurrent[-2 * stride + 3] + pCurrent[-2 * stride + 6] + pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[-stride + 6] + pCurrent[0] + pCurrent[3] + pCurrent[6] + pCurrent[stride] + pCurrent[stride + 3] + pCurrent[stride + 6] + pCurrent[2 * stride] + pCurrent[2 * stride + 3] + pCurrent[2 * stride + 6]) / 15);

				pCurrent = pOrig + y * stride;
				arrayB[arrayRow, x] = (byte)((pCurrent[-2 * stride] + pCurrent[-2 * stride + 3] + pCurrent[-2 * stride + 6] + pCurrent[-2 * stride + 9] + pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[-stride + 6] + pCurrent[-stride + 9] + pCurrent[0] + pCurrent[3] + pCurrent[6] + pCurrent[9] + pCurrent[stride] + pCurrent[stride + 3] + pCurrent[stride + 6] + pCurrent[stride + 9] + pCurrent[2 * stride] + pCurrent[2 * stride + 3] + pCurrent[2 * stride + 6] + pCurrent[2 * stride + 9]) / 20);
				pCurrent++;
				arrayG[arrayRow, x] = (byte)((pCurrent[-2 * stride] + pCurrent[-2 * stride + 3] + pCurrent[-2 * stride + 6] + pCurrent[-2 * stride + 9] + pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[-stride + 6] + pCurrent[-stride + 9] + pCurrent[0] + pCurrent[3] + pCurrent[6] + pCurrent[9] + pCurrent[stride] + pCurrent[stride + 3] + pCurrent[stride + 6] + pCurrent[stride + 9] + pCurrent[2 * stride] + pCurrent[2 * stride + 3] + pCurrent[2 * stride + 6] + pCurrent[2 * stride + 9]) / 20);
				pCurrent++;
				arrayR[arrayRow, x] = (byte)((pCurrent[-2 * stride] + pCurrent[-2 * stride + 3] + pCurrent[-2 * stride + 6] + pCurrent[-2 * stride + 9] + pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[-stride + 6] + pCurrent[-stride + 9] + pCurrent[0] + pCurrent[3] + pCurrent[6] + pCurrent[9] + pCurrent[stride] + pCurrent[stride + 3] + pCurrent[stride + 6] + pCurrent[stride + 9] + pCurrent[2 * stride] + pCurrent[2 * stride + 3] + pCurrent[2 * stride + 6] + pCurrent[2 * stride + 9]) / 20);

				//last column
				x = width - 1;
				pCurrent = pOrig + y * stride + (x-2) * 3;

				arrayB[arrayRow, x] = (byte)((pCurrent[-2 * stride] + pCurrent[-2 * stride + 3] + pCurrent[-2 * stride + 6] + pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[-stride + 6] + pCurrent[0] + pCurrent[3] + pCurrent[6] + pCurrent[stride] + pCurrent[stride + 3] + pCurrent[stride + 6] + pCurrent[2 * stride] + pCurrent[2 * stride + 3] + pCurrent[2 * stride + 6]) / 15);
				pCurrent++;
				arrayG[arrayRow, x] = (byte)((pCurrent[-2 * stride] + pCurrent[-2 * stride + 3] + pCurrent[-2 * stride + 6] + pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[-stride + 6] + pCurrent[0] + pCurrent[3] + pCurrent[6] + pCurrent[stride] + pCurrent[stride + 3] + pCurrent[stride + 6] + pCurrent[2 * stride] + pCurrent[2 * stride + 3] + pCurrent[2 * stride + 6]) / 15);
				pCurrent++;
				arrayR[arrayRow, x] = (byte)((pCurrent[-2 * stride] + pCurrent[-2 * stride + 3] + pCurrent[-2 * stride + 6] + pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[-stride + 6] + pCurrent[0] + pCurrent[3] + pCurrent[6] + pCurrent[stride] + pCurrent[stride + 3] + pCurrent[stride + 6] + pCurrent[2 * stride] + pCurrent[2 * stride + 3] + pCurrent[2 * stride + 6]) / 15);

				pCurrent = pOrig + y * stride + (x - 3) * 3;
				arrayB[arrayRow, x] = (byte)((pCurrent[-2 * stride] + pCurrent[-2 * stride + 3] + pCurrent[-2 * stride + 6] + pCurrent[-2 * stride + 9] + pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[-stride + 6] + pCurrent[-stride + 9] + pCurrent[0] + pCurrent[3] + pCurrent[6] + pCurrent[9] + pCurrent[stride] + pCurrent[stride + 3] + pCurrent[stride + 6] + pCurrent[stride + 9] + pCurrent[2 * stride] + pCurrent[2 * stride + 3] + pCurrent[2 * stride + 6] + pCurrent[2 * stride + 9]) / 20);
				pCurrent++;
				arrayG[arrayRow, x] = (byte)((pCurrent[-2 * stride] + pCurrent[-2 * stride + 3] + pCurrent[-2 * stride + 6] + pCurrent[-2 * stride + 9] + pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[-stride + 6] + pCurrent[-stride + 9] + pCurrent[0] + pCurrent[3] + pCurrent[6] + pCurrent[9] + pCurrent[stride] + pCurrent[stride + 3] + pCurrent[stride + 6] + pCurrent[stride + 9] + pCurrent[2 * stride] + pCurrent[2 * stride + 3] + pCurrent[2 * stride + 6] + pCurrent[2 * stride + 9]) / 20);
				pCurrent++;
				arrayR[arrayRow, x] = (byte)((pCurrent[-2 * stride] + pCurrent[-2 * stride + 3] + pCurrent[-2 * stride + 6] + pCurrent[-2 * stride + 9] + pCurrent[-stride] + pCurrent[-stride + 3] + pCurrent[-stride + 6] + pCurrent[-stride + 9] + pCurrent[0] + pCurrent[3] + pCurrent[6] + pCurrent[9] + pCurrent[stride] + pCurrent[stride + 3] + pCurrent[stride + 6] + pCurrent[stride + 9] + pCurrent[2 * stride] + pCurrent[2 * stride + 3] + pCurrent[2 * stride + 6] + pCurrent[2 * stride + 9]) / 20);

				//get second row
				pCurrent = pOrig + y * stride + 6;

				long sumB = ((pCurrent[-2 * stride + 3] + pCurrent[-stride + 3] + pCurrent[3] + pCurrent[stride + 3] + pCurrent[2 * stride + 3]) << 48) + (((pCurrent[-2 * stride] + pCurrent[-stride] + pCurrent[0] + pCurrent[stride] + pCurrent[2 * stride])) << 32) + ((pCurrent[-2 * stride - 3] + pCurrent[-stride - 3] + pCurrent[-3] + pCurrent[stride - 3] + pCurrent[2 * stride - 3]) << 16) + (pCurrent[-2 * stride - 6] + pCurrent[-stride - 6] + pCurrent[-6] + pCurrent[stride - 6] + pCurrent[2 * stride - 6]);
				long sum3B = (pCurrent[-2 * stride + 3] + pCurrent[-stride + 3] + pCurrent[+3] + pCurrent[stride + 3] + pCurrent[2 * stride + 3]);
				pCurrent++;
				long sumG = ((pCurrent[-2 * stride + 3] + pCurrent[-stride + 3] + pCurrent[3] + pCurrent[stride + 3] + pCurrent[2 * stride + 3]) << 48) + (((pCurrent[-2 * stride] + pCurrent[-stride] + pCurrent[0] + pCurrent[stride] + pCurrent[2 * stride])) << 32) + ((pCurrent[-2 * stride - 3] + pCurrent[-stride - 3] + pCurrent[-3] + pCurrent[stride - 3] + pCurrent[2 * stride - 3]) << 16) + (pCurrent[-2 * stride - 6] + pCurrent[-stride - 6] + pCurrent[-6] + pCurrent[stride - 6] + pCurrent[2 * stride - 6]);
				long sum3G = (pCurrent[-2 * stride + 3] + pCurrent[-stride + 3] + pCurrent[+3] + pCurrent[stride + 3] + pCurrent[2 * stride + 3]);
				pCurrent++;
				long sumR = ((pCurrent[-2 * stride + 3] + pCurrent[-stride + 3] + pCurrent[3] + pCurrent[stride + 3] + pCurrent[2 * stride + 3]) << 48) + (((pCurrent[-2 * stride] + pCurrent[-stride] + pCurrent[0] + pCurrent[stride] + pCurrent[2 * stride])) << 32) + ((pCurrent[-2 * stride - 3] + pCurrent[-stride - 3] + pCurrent[-3] + pCurrent[stride - 3] + pCurrent[2 * stride - 3]) << 16) + (pCurrent[-2 * stride - 6] + pCurrent[-stride - 6] + pCurrent[-6] + pCurrent[stride - 6] + pCurrent[2 * stride - 6]);
				long sum3R = (pCurrent[-2 * stride + 3] + pCurrent[-stride + 3] + pCurrent[+3] + pCurrent[stride + 3] + pCurrent[2 * stride + 3]);

				pCurrent = pOrig + y * stride + 6;

				for (x = 2; x < width - 2; x++)
				{
					arrayB[arrayRow, x] = (byte)(((sumB & 0xFFFF) + ((sumB >> 16) & 0xFFFF) + ((sumB >> 32) & 0xFFFF) + ((sumB >> 48) & 0xFFFF) + sum3B) / 25);
					arrayG[arrayRow, x] = (byte)(((sumG & 0xFFFF) + ((sumG >> 16) & 0xFFFF) + ((sumG >> 32) & 0xFFFF) + ((sumG >> 48) & 0xFFFF) + sum3G) / 25);
					arrayR[arrayRow, x] = (byte)(((sumR & 0xFFFF) + ((sumR >> 16) & 0xFFFF) + ((sumR >> 32) & 0xFFFF) + ((sumR >> 48) & 0xFFFF) + sum3R) / 25);

					pCurrent += 3;

					sumB = (sumB >> 16) | (sum3B << 48);
					sumG = (sumG >> 16) | (sum3G << 48);
					sumR = (sumR >> 16) | (sum3R << 48);
					sum3B = (pCurrent[-2 * stride + 3] + pCurrent[-stride + 3] + pCurrent[3] + pCurrent[stride + 3] + pCurrent[2*stride + 3]);
					sum3G = (pCurrent[-2 * stride + 4] + pCurrent[-stride + 4] + pCurrent[4] + pCurrent[stride + 4] + pCurrent[2*stride + 4]);
					sum3R = (pCurrent[-2 * stride + 4] + pCurrent[-stride + 5] + pCurrent[5] + pCurrent[stride + 5] + pCurrent[2*stride + 4]);
				}
			}
		}
		#endregion

		#region SetAveragingRow5x5_24bpp()
		private unsafe static void SetAveragingRow5x5_24bpp(int y, Size imageSize, byte* pOrig, int stride, byte[,] arrayR, byte[,] arrayG, byte[,] arrayB)
		{
			int width = imageSize.Width;
			int arrayRow = y % 5;

			for (int x = 0; x < width; x++)
			{
				pOrig[y * stride + x * 3] = arrayB[arrayRow, x];
				pOrig[y * stride + x * 3 + 1] = arrayG[arrayRow, x];
				pOrig[y * stride + x * 3 + 2] = arrayR[arrayRow, x];
			}
		}
		#endregion

		#region UnsharpMasking_Process()
		private static void UnsharpMasking_Process(Bitmap bitmap, Rectangle clip)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = clip.Width;
				int height = clip.Height;

				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;

				unsafe
				{
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();

					if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
					{
						byte[,] arrayR = new byte[3, bitmapData.Width];
						byte[,] arrayG = new byte[3, bitmapData.Width];
						byte[,] arrayB = new byte[3, bitmapData.Width];

						//first row
						GetAveragingRow3x3_24bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
						GetAveragingRow3x3_24bpp(1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);

						SetUnsharpMaskingRow3x3_24bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);

						//Set middle part
						for (int y = 1; y < height - 1; y++)
						{
							GetAveragingRow3x3_24bpp(y + 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
							SetUnsharpMaskingRow3x3_24bpp(y, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
						}

						//last row
						SetUnsharpMaskingRow3x3_24bpp(height - 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
					}
					else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						byte[,] array = new byte[3, bitmapData.Width];

						//first row
						GetAveragingRow3x3_8bpp(0, clip.Size, pOrig, stride, array);
						GetAveragingRow3x3_8bpp(1, clip.Size, pOrig, stride, array);

						SetUnsharpMaskingRow3x3_8bpp(0, clip.Size, pOrig, stride, array);

						//Set middle part
						for (int y = 1; y < height - 1; y++)
						{
							GetAveragingRow3x3_8bpp(y + 1, clip.Size, pOrig, stride, array);
							SetUnsharpMaskingRow3x3_8bpp(y, clip.Size, pOrig, stride, array);
						}

						//last row
						SetUnsharpMaskingRow3x3_8bpp(height - 1, clip.Size, pOrig, stride, array);
					}
					else if (bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format32bppArgb)
					{
						byte[,] arrayR = new byte[3, bitmapData.Width];
						byte[,] arrayG = new byte[3, bitmapData.Width];
						byte[,] arrayB = new byte[3, bitmapData.Width];

						//first row
						GetAveragingRow3x3_32bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
						GetAveragingRow3x3_32bpp(1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);

						SetUnsharpMaskingRow3x3_32bpp(0, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);

						//Set middle part
						for (int y = 1; y < height - 1; y++)
						{
							GetAveragingRow3x3_32bpp(y + 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
							SetUnsharpMaskingRow3x3_32bpp(y, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
						}

						//last row
						SetUnsharpMaskingRow3x3_32bpp(height - 1, clip.Size, pOrig, stride, arrayR, arrayG, arrayB);
					}
					else
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region SetUnsharpMaskingRow3x3_32bpp()
		private unsafe static void SetUnsharpMaskingRow3x3_32bpp(int y, Size imageSize, byte* pOrig, int stride, byte[,] arrayR, byte[,] arrayG, byte[,] arrayB)
		{
			int width = imageSize.Width;
			int arrayRow = y % 3;

			for (int x = 0; x < width; x++)
			{
				pOrig[y * stride + x * 4] = (byte)((pOrig[y * stride + x * 4] + arrayB[arrayRow, x]) >> 1);
				pOrig[y * stride + x * 4 + 1] = (byte)((pOrig[y * stride + x * 4 + 1] + arrayG[arrayRow, x]) >> 1);
				pOrig[y * stride + x * 4 + 2] = (byte)((pOrig[y * stride + x * 4 + 2] + arrayR[arrayRow, x]) >> 1);
			}
		}
		#endregion

		#region SetUnsharpMaskingRow3x3_24bpp()
		private unsafe static void SetUnsharpMaskingRow3x3_24bpp(int y, Size imageSize, byte* pOrig, int stride, byte[,] arrayR, byte[,] arrayG, byte[,] arrayB)
		{
			int width = imageSize.Width;
			int arrayRow = y % 3;

			for (int x = 0; x < width; x++)
			{
				pOrig[y * stride + x * 3] = (byte)((pOrig[y * stride + x * 3] + arrayB[arrayRow, x]) / 2);
				pOrig[y * stride + x * 3 + 1] = (byte)((pOrig[y * stride + x * 3 + 1] + arrayG[arrayRow, x]) / 2);
				pOrig[y * stride + x * 3 + 2] = (byte)((pOrig[y * stride + x * 3 + 2] + arrayR[arrayRow, x]) / 2);
			}
		}
		#endregion

		#region SetUnsharpMaskingRow3x3_8bpp()
		private unsafe static void SetUnsharpMaskingRow3x3_8bpp(int y, Size imageSize, byte* pOrig, int stride, byte[,] array)
		{
			int width = imageSize.Width;
			int arrayRow = y % 3;

			for (int x = 0; x < width; x++)
				pOrig[y * stride + x] = (byte)((pOrig[y * stride + x] + array[arrayRow, x]) >> 1);
		}
		#endregion

		#endregion

	}
}
