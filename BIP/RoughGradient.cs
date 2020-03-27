using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	public class RoughGradient
	{
		public byte[] Gradient;
		public RoughGradient.Operator Type;

		static byte arraySize = 60;

		#region constructor
		private RoughGradient(byte[] gratient, RoughGradient.Operator type)
		{
			this.Gradient = gratient;
			this.Type = type;
		}
		#endregion

		#region enum Operator
		public enum Operator
		{
			Vertical
		}
		#endregion
		
		//	PUBLIC METHODS
		#region public methods

		#region Get()
		public static RoughGradient Get(Bitmap bitmap, RoughGradient.Operator type)
		{
			if(bitmap == null)
				return null ;

			RoughGradient roughGradient = null;
			
			try
			{
				switch(bitmap.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed : roughGradient = GetFrom8bppGrayscale(bitmap, type); break;
					case PixelFormat.Format24bppRgb : roughGradient = GetFrom24bpp(bitmap, type); break;
					case PixelFormat.Format32bppRgb :
					case PixelFormat.Format32bppArgb :
						roughGradient = GetFrom32bpp(bitmap, type); break;
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}
			}
			catch(Exception ex)
			{
				throw new Exception("RoughGradient, Get(): " + ex.Message ) ;
			}

			SmoothGradient(roughGradient.Gradient);
			return roughGradient;
		}
		#endregion
		
		#endregion

		
		//PRIVATE METHODS
		
		#region GetFrom32bpp()
		private static RoughGradient GetFrom32bpp(Bitmap bitmap, RoughGradient.Operator type)
		{
			/*Bitmap		result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed); 
			BitmapData	bitmapData = null;
			BitmapData	resultData = null;
					
			try
			{
				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadOnly, bitmap.PixelFormat); 
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat); 

#if DEBUG
				DateTime	start = DateTime.Now ;
#endif

				//histogram.Show() ;
				int			delta;
				int			sStride = bitmapData.Stride; 
				int			rStride = resultData.Stride; 
				int			clipHeight = result.Height - 1;
				int			clipWidth = result.Width - 1;
				int			x, y;
				int[,]		maskArray = GetConvolutionMask(mask);;
				int[,]		edgeMaskArray = GetEdgeMask(mask);
			 
				unsafe
				{
					byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 
					byte*		pResult = (byte*)resultData.Scan0.ToPointer(); 
					byte*		pCurrentS ;
					byte*		pCurrentR ;

					for (y = 1; y < clipHeight; y++)
					{
						pCurrentS = pSource + y * sStride + 4;
						pCurrentR = pResult + y * rStride;

						for (x = 1; x < clipWidth; x++)
						{
							if (*pCurrentS < bThreshold || pCurrentS[1] < gThreshold || pCurrentS[2] < rThreshold)
							{
								delta = maskArray[0, 0] * pCurrentS[-sStride - 3] + maskArray[0, 1] * pCurrentS[-sStride] + maskArray[0, 2] * pCurrentS[-sStride + 3] +
									maskArray[1, 0] * pCurrentS[-3] + maskArray[1, 1] * pCurrentS[0] + maskArray[1, 2] * pCurrentS[3] +
									maskArray[2, 0] * pCurrentS[sStride - 3] + maskArray[2, 1] * pCurrentS[sStride] + maskArray[2, 2] * pCurrentS[sStride + 3];

								if (delta > minDelta)
								{
									pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									pCurrentS += 4;
								}
								else
								{
									pCurrentS++;
									delta = maskArray[0, 0] * pCurrentS[-sStride - 3] + maskArray[0, 1] * pCurrentS[-sStride] + maskArray[0, 2] * pCurrentS[-sStride + 3] +
										maskArray[1, 0] * pCurrentS[-3] + maskArray[1, 1] * pCurrentS[0] + maskArray[1, 2] * pCurrentS[3] +
										maskArray[2, 0] * pCurrentS[sStride - 3] + maskArray[2, 1] * pCurrentS[sStride] + maskArray[2, 2] * pCurrentS[sStride + 3];

									if (delta > minDelta)
									{
										pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
										pCurrentS += 3;
									}
									else
									{
										pCurrentS++;
										delta = maskArray[0, 0] * pCurrentS[-sStride - 3] + maskArray[0, 1] * pCurrentS[-sStride] + maskArray[0, 2] * pCurrentS[-sStride + 3] +
											maskArray[1, 0] * pCurrentS[-3] + maskArray[1, 1] * pCurrentS[0] + maskArray[1, 2] * pCurrentS[3] +
											maskArray[2, 0] * pCurrentS[sStride - 3] + maskArray[2, 1] * pCurrentS[sStride] + maskArray[2, 2] * pCurrentS[sStride + 3];

										if (delta > minDelta)
											pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
										else if (highlightObjects && (((x + y) % 2) == 0) && (x > 6) && (x < clipWidth - 7) && (y > 6) && (y < clipHeight - 7))
										{
											if ((pCurrentS[-2] < ObjectThreshold || pCurrentS[-1] < ObjectThreshold || pCurrentS[0] < ObjectThreshold) &&
												(pCurrentS[-sStride * 3 - 2] < ObjectThreshold && pCurrentS[-sStride * 3 - 1] < ObjectThreshold && pCurrentS[-sStride * 3] < ObjectThreshold) &&
												(pCurrentS[-sStride * 6 - 2] < ObjectThreshold && pCurrentS[-sStride * 6 - 1] < ObjectThreshold && pCurrentS[-sStride * 6] < ObjectThreshold) &&
												(pCurrentS[sStride * 3 - 2] < ObjectThreshold && pCurrentS[sStride * 3 - 1] < ObjectThreshold && pCurrentS[sStride * 3] < ObjectThreshold) &&
												(pCurrentS[sStride * 6 - 2] < ObjectThreshold && pCurrentS[sStride * 6 - 1] < ObjectThreshold && pCurrentS[sStride * 6] < ObjectThreshold) &&
												(pCurrentS[-20] < ObjectThreshold && pCurrentS[-19] < ObjectThreshold && pCurrentS[-18] < ObjectThreshold) &&
												(pCurrentS[-11] < ObjectThreshold && pCurrentS[-10] < ObjectThreshold && pCurrentS[-9] < ObjectThreshold) &&
												(pCurrentS[7] < ObjectThreshold && pCurrentS[8] < ObjectThreshold && pCurrentS[9] < ObjectThreshold) &&
												(pCurrentS[16] < ObjectThreshold && pCurrentS[17] < ObjectThreshold && pCurrentS[18] < ObjectThreshold))
												pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
										}

										pCurrentS += 2;
									}
								}
							}
							else
							{
								pCurrentS += 4;
							}
						}
					}
				}

#if DEBUG
				Console.WriteLine("RoughGradient GetFrom8bppGrayscale():" + (DateTime.Now.Subtract(start)).ToString()) ;
#endif
			
				return null; 
			}
			finally
			{
				if(bitmap != null && bitmapData != null)
					bitmap.UnlockBits(bitmapData);
				if(result != null && resultData != null)
					result.UnlockBits(resultData); 
			}*/

			return null;
		}
		#endregion

		#region GetFrom24bpp()
		private static RoughGradient GetFrom24bpp(Bitmap bitmap, RoughGradient.Operator mask)
		{
			/*Bitmap result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);
			BitmapData bitmapData = null;
			BitmapData resultData = null;

			try
			{
				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadOnly, bitmap.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

#if DEBUG
				DateTime start = DateTime.Now;
#endif

				//histogram.Show() ;
				int delta;
				int sStride = bitmapData.Stride;
				int rStride = resultData.Stride;
				int clipHeight = result.Height - 1;
				int clipWidth = result.Width - 1;
				int x, y;
				int[,] maskArray = GetConvolutionMask(mask);
				int[,] edgeMaskArray = GetEdgeMask(mask);

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS;
					byte* pCurrentR;

					for (y = 1; y < clipHeight; y++)
					{
						pCurrentS = pSource + y * sStride + 3;
						pCurrentR = pResult + y * rStride;

						for (x = 1; x < clipWidth; x++)
						{
							if (*pCurrentS < bThreshold || pCurrentS[1] < gThreshold || pCurrentS[2] < rThreshold)
							{
								delta = maskArray[0, 0] * pCurrentS[-sStride - 3] + maskArray[0, 1] * pCurrentS[-sStride] + maskArray[0, 2] * pCurrentS[-sStride + 3] +
									maskArray[1, 0] * pCurrentS[-3] + maskArray[1, 1] * pCurrentS[0] + maskArray[1, 2] * pCurrentS[3] +
									maskArray[2, 0] * pCurrentS[sStride - 3] + maskArray[2, 1] * pCurrentS[sStride] + maskArray[2, 2] * pCurrentS[sStride + 3];

								if (delta > minDelta )
								{
									pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								}
								else
								{
									delta = maskArray[0, 0] * pCurrentS[-sStride - 2] + maskArray[0, 1] * pCurrentS[-sStride + 1] + maskArray[0, 2] * pCurrentS[-sStride + 4] +
										maskArray[1, 0] * pCurrentS[-2] + maskArray[1, 1] * pCurrentS[1] + maskArray[1, 2] * pCurrentS[4] +
										maskArray[2, 0] * pCurrentS[sStride - 2] + maskArray[2, 1] * pCurrentS[sStride + 1] + maskArray[2, 2] * pCurrentS[sStride + 4];

									if (delta > minDelta )
									{
										pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									}
									else
									{
										delta = maskArray[0, 0] * pCurrentS[-sStride - 1] + maskArray[0, 1] * pCurrentS[-sStride + 2] + maskArray[0, 2] * pCurrentS[-sStride + 5] +
											maskArray[1, 0] * pCurrentS[-1] + maskArray[1, 1] * pCurrentS[2] + maskArray[1, 2] * pCurrentS[5] +
											maskArray[2, 0] * pCurrentS[sStride - 1] + maskArray[2, 1] * pCurrentS[sStride + 2] + maskArray[2, 2] * pCurrentS[sStride + 5];

										if (delta > minDelta )
										{
											pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
										}
										else if (highlightObjects && (x > 6) && (x < clipWidth - 7) && (y > 6) && (y < clipHeight - 7))
										{
											if ((pCurrentS[0] < ObjectThreshold || pCurrentS[1] < ObjectThreshold || pCurrentS[2] < ObjectThreshold) &&
												(pCurrentS[-sStride] < ObjectThreshold && pCurrentS[-sStride + 1] < ObjectThreshold && pCurrentS[-sStride + 2] < ObjectThreshold) &&
												(pCurrentS[sStride] < ObjectThreshold && pCurrentS[sStride + 1] < ObjectThreshold && pCurrentS[sStride + 2] < ObjectThreshold) &&
												(pCurrentS[-3] < ObjectThreshold && pCurrentS[-2] < ObjectThreshold && pCurrentS[-1] < ObjectThreshold) &&
												(pCurrentS[3] < ObjectThreshold && pCurrentS[4] < ObjectThreshold && pCurrentS[5] < ObjectThreshold))
												pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
										}

									}
								}
							}

							pCurrentS += 3;
						}
					}
				}

#if DEBUG
				Console.WriteLine("RoughGradient GetFrom24bpp():" + (DateTime.Now.Subtract(start)).ToString());
#endif

				return null;
			}
			finally
			{
				if (bitmap != null && bitmapData != null)
					bitmap.UnlockBits(bitmapData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}*/
			return null;
		}
		#endregion
		
		#region GetFrom8bppGrayscale()
		private static RoughGradient GetFrom8bppGrayscale(Bitmap bitmap, RoughGradient.Operator type)
		{
			BitmapData	bitmapData = null;
					
			try
			{					
				bitmapData = bitmap.LockBits(new Rectangle(0,0,bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat); 

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

				//histogram.Show() ;
				byte[]		gradient = new byte[bitmapData.Height];
				int			stride = bitmapData.Stride; 
				int			height = bitmapData.Height;
				int			width = bitmapData.Width;
				int			x, y;
				byte[]		roughArray = new byte[60];
				byte		delta;

				unsafe
				{
					byte* pBitmap = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					for (y = 0; y < height; y++)
					{
						pCurrent = pBitmap + y * stride + 1;

						for (int i = 0; i < arraySize; i++)
							roughArray[i] = 0;

						for (x = width / 4; x < (width * 3 / 4); x++)
						{
							if (pCurrent[x - 1] > 170 && pCurrent[x] > 170)
							{
								delta = (byte)((pCurrent[x] > pCurrent[x - 1]) ? pCurrent[x] - pCurrent[x - 1] : pCurrent[x - 1] - pCurrent[x]);

								if (delta < arraySize)
									roughArray[delta]++;
							}
						}

						gradient[y] = GetMaximumIndex(roughArray);
					}
				}

#if DEBUG
			Console.WriteLine("RoughGradient GetFrom8bppGrayscale():" + (DateTime.Now.Subtract(start)).ToString()) ;
#endif
				return new RoughGradient(gradient, type); 
			}			
			finally
			{
				if(bitmap != null && bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region GetMaximumIndex()	
		private static byte GetMaximumIndex(byte[] byteArray)
		{
			byte maxIndex = byte.MaxValue;
			int max = byteArray[0] + byteArray[1] + byteArray[2] + byteArray[3] + byteArray[4];

			for (byte i = 3; i < arraySize - 2; i++)
			{
				if (max < (byteArray[i - 2] + byteArray[i - 1] + byteArray[i] + byteArray[i + 1] + byteArray[i + 2]))
				{
					maxIndex = i;
					max = byteArray[i - 2] + byteArray[i - 1] + byteArray[i] + byteArray[i + 1] + byteArray[i + 2];
				}
			}

			return maxIndex;
		}
		#endregion

		#region SmoothGradient()
		private static void SmoothGradient(byte[] gradient)
		{
			int minIndexUpperPortion = 0;
			int minIndexLowerPortion = gradient.Length - 1;

			for (int i = 1; i <= gradient.Length; i++)
				if (gradient[i] < 5)
					gradient[i] = 5;

			for (int i = 1; i <= gradient.Length / 5; i++)
				if (gradient[minIndexUpperPortion] > gradient[i])
					minIndexUpperPortion = i;

			for (int i = gradient.Length * 4 / 5; i < gradient.Length; i++)
				if (gradient[minIndexLowerPortion] > gradient[i])
					minIndexLowerPortion = i;

			double jump = (gradient[minIndexLowerPortion] - gradient[minIndexUpperPortion]) / (minIndexLowerPortion - minIndexUpperPortion);
			double start = gradient[minIndexUpperPortion] - (jump * minIndexUpperPortion);
			double stop = gradient[minIndexLowerPortion] + (jump * (gradient.Length - minIndexLowerPortion));

			start = Math.Max(5, start);
			stop = Math.Max(5, stop);
			jump = (stop * gradient.Length / start);

			for (int i = 1; i <= gradient.Length; i++)
				gradient[i] = (byte)(start + i * jump);
		}
		#endregion

	}
}
