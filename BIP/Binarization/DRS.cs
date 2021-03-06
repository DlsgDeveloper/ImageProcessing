using System;
using System.Drawing ;
using System.Drawing.Imaging ;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class DRS
	{		
		private DRS()
		{
		}

		//	PUBLIC METHODS

		#region Binorize()	
		public static Bitmap Binorize(Bitmap source, int bThresholdDelta, int wThresholdDelta)
		{
			return Binorize(source, Rectangle.Empty, bThresholdDelta, wThresholdDelta) ;
		}
		
		public static Bitmap Binorize(Bitmap bmpSource, Rectangle clip, int bThresholdDelta, int wThresholdDelta)
		{
			if(bmpSource == null)
				return null ;

			if(clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, bmpSource.Width, bmpSource.Height) ;

			Bitmap		bmpResult = null ;
			
			try
			{
				switch(bmpSource.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed :				
						bmpResult = Binorize8bpp(bmpSource, clip, bThresholdDelta, wThresholdDelta) ;
						break ;
					case PixelFormat.Format24bppRgb :				
						bmpResult = Binorize24bpp(bmpSource, clip, bThresholdDelta, wThresholdDelta) ;
						break ;
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}

				Misc.SetBitmapResolution(bmpResult, bmpSource.HorizontalResolution, bmpSource.VerticalResolution);
			}
			catch(Exception ex)
			{
				throw new Exception(ex.Message + "\nBinorize()") ;
			}

			return bmpResult ;
		}
		#endregion

		//PRIVATE METHODS		
		#region Binorize24bpp()
		private static Bitmap Binorize24bpp( Bitmap sourceBmp, Rectangle clip, int bThresholdDelta, int wThresholdDelta)
		{
			byte			red, green, blue ;
			Histogram	histogram;

			if(clip.Width < 800 || clip.Height < 800)
				histogram = new Histogram(sourceBmp);
			else
				histogram = new Histogram(sourceBmp, Rectangle.Inflate(new Rectangle(0, 0, sourceBmp.Width, sourceBmp.Height), -100, -100));

			Bitmap		resultBmp = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed); 
			BitmapData	sourceData = sourceBmp.LockBits(clip, ImageLockMode.ReadOnly, sourceBmp.PixelFormat ); 
			BitmapData	resultData = resultBmp.LockBits(new Rectangle(0, 0, resultBmp.Width, resultBmp.Height), ImageLockMode.ReadWrite, resultBmp.PixelFormat ); 

			

			byte			wThresholdR = (byte) (Math.Max(histogram.ExtremeR, histogram.SecondExtremeR) + wThresholdDelta);		
			byte			wThresholdG = (byte) (Math.Max(histogram.ExtremeG, histogram.SecondExtremeG) + wThresholdDelta);		
			byte			wThresholdB = (byte) (Math.Max(histogram.ExtremeB, histogram.SecondExtremeB) + wThresholdDelta);		
			short			bThresholdR = (byte) (Math.Min(histogram.ExtremeR, histogram.SecondExtremeR) + bThresholdDelta);
			short			bThresholdG = (byte) (Math.Min(histogram.ExtremeG, histogram.SecondExtremeG) + bThresholdDelta);
			short			bThresholdB = (byte) (Math.Min(histogram.ExtremeB, histogram.SecondExtremeB) + bThresholdDelta);
			short			bThreshold = (short) (bThresholdR + bThresholdG + bThresholdB);

			wThresholdR = (byte) (Math.Max(0, Math.Min(255, (int) wThresholdR)));
			wThresholdG = (byte) (Math.Max(0, Math.Min(255, (int) wThresholdG)));
			wThresholdB = (byte) (Math.Max(0, Math.Min(255, (int) wThresholdB)));
			bThresholdR = (byte) (Math.Max(0, Math.Min(wThresholdR, (int) bThresholdR)));
			bThresholdG = (byte) (Math.Max(0, Math.Min(wThresholdG, (int) bThresholdG)));
			bThresholdB = (byte) (Math.Max(0, Math.Min(wThresholdB, (int) bThresholdB)));
			bThreshold = (short) (bThresholdR + bThresholdG + bThresholdB);
			
			int			sourceStride = sourceData.Stride; 
			int			resultStride = resultData.Stride; 
			
			Rectangle	mask = Rectangle.Empty ;
			uint		sumR = 0 ;
			uint		sumG = 0 ;
			uint		sumB = 0 ;
			int			maskSize;
			
			unsafe
			{
				byte*		pSource = (byte*)sourceData.Scan0.ToPointer(); 
				byte*		pResult = (byte*)resultData.Scan0.ToPointer(); 
				ushort*		columnSums = stackalloc ushort[resultData.Width * 3] ;
				ushort*		columnSumsTmp = columnSums ;
				ushort*		columnSumsTmp2 ;
				byte*		pCurrent ;

				for(int i = 0; i < resultData.Width * 3; i++)
					*(columnSumsTmp++) = 0 ;

				for(int y = 0; y < clip.Height; y++) 
				{ 
					sumR = 0 ;
					sumG = 0 ;
					sumB = 0 ;
					mask.X = clip.X ;
					mask.Y = Math.Max(clip.Y, clip.Y + y - 4) ;
					mask.Width = 5 ;
					mask.Height = Math.Max(5, Math.Min(9, Math.Min(clip.Bottom - y + 4, y + 5))) ;
					
					GetColumnSums24Bpp(columnSums, pSource, sourceStride, clip, (ushort) (y + 4)) ;
					
					pCurrent = pSource + ((y + clip.Y) * sourceStride) + clip.X * 3 ;

					ushort		*columnSumsPtr = columnSums ;

					for(int i = mask.X; i < mask.Right; i++)
					{
						sumB += *(columnSumsPtr++) ;
						sumG += *(columnSumsPtr++) ;
						sumR += *(columnSumsPtr++) ;
					}

					for(int x = 0; x < clip.Width; x++) 
					{ 
						blue = *(pCurrent++) ;
						green = *(pCurrent++) ;
						red = *(pCurrent++) ;
						
						//gray = (byte) (blue * 0.114F + green * 0.587F + red * 0.299F) ;

						if((blue > wThresholdB) && (green > wThresholdG) && (red > wThresholdR)) //(gray > wThreshold)
						{ 
							pResult[y * resultStride + ( x >> 3 )] |= (byte) (0x80 >> (x & 0x07)); //set the appropriate bit to 1
						}
						else if ((blue > bThresholdB) || (green > bThresholdG) || (red > bThresholdR)) //(gray >= bThreshold)
						{
							maskSize = mask.Width * mask.Height;

							if((red * maskSize) > sumR + (6*maskSize))
								pResult[y * resultStride + ( x >> 3 )] |= (byte) (0x80 >> (x & 0x07)); 
							else
							{
								if((green * maskSize) > sumG + (6*maskSize))
									pResult[y * resultStride + ( x >> 3 )] |= (byte) (0x80 >> (x & 0x07)); 
								else
								{
									if((blue * maskSize) > sumB + (6*maskSize))
										pResult[y * resultStride + ( x >> 3 )] |= (byte) (0x80 >> (x & 0x07)); 
								}
							}
						}

						if(x < clip.X + 4)
						{
							columnSumsTmp = columnSums + ((x+5) * 3) ;
							
							sumB += *(columnSumsTmp++) ;
							sumG += *(columnSumsTmp++) ;
							sumR += *(columnSumsTmp++) ;
							mask.Width ++  ;
						}
						else if(x >= clip.Right - 5)
						{
							columnSumsTmp = columnSums + ((x-4) * 3) ;
							
							sumB -= *(columnSumsTmp++) ;
							sumG -= *(columnSumsTmp++) ;
							sumR -= *(columnSumsTmp++) ;
							mask.X ++  ;
							mask.Width --  ;
						}
						else
						{
							columnSumsTmp = columnSums + ((x + 5) * 3) ;
							columnSumsTmp2 = columnSums + (x - 4) * 3 ;
							
							sumB += (uint) *(columnSumsTmp++) - *(columnSumsTmp2++) ;
							sumG += (uint) *(columnSumsTmp++) - *(columnSumsTmp2++) ;
							sumR += (uint) *(columnSumsTmp++) - *(columnSumsTmp2++) ;
							mask.X ++  ;
						}
					}				
				}
			}

			//ImageProcessing.NoiseReduction.Despeckle(resultData, clip, 1) ; 
			//ImageProcessing.NoiseReduction.Despeckle(resultData, clip, 2) ; 
			//ImageProcessing.NoiseReduction.Despeckle(resultData, clip, 3) ; 

			sourceBmp.UnlockBits(sourceData) ;
			resultBmp.UnlockBits(resultData); 

			return resultBmp; 
		}
		#endregion
		
		#region Binorize8bpp()
		private static Bitmap Binorize8bpp(Bitmap sourceBmp, Rectangle clip, int bThresholdDelta, int wThresholdDelta)
		{
			Histogram	histogram;

			if(clip.Width < 800 || clip.Height < 800)
				histogram = new Histogram(sourceBmp, clip);
			else
				histogram = new Histogram(sourceBmp, Rectangle.Inflate(clip, -100, -100));
			
			Bitmap		resultBmp = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed); 
			BitmapData	sourceData = sourceBmp.LockBits(clip, ImageLockMode.ReadOnly, sourceBmp.PixelFormat ); 
			BitmapData	resultData = resultBmp.LockBits(new Rectangle(0, 0, resultBmp.Width, resultBmp.Height), ImageLockMode.WriteOnly, resultBmp.PixelFormat ); 
			Color[]		colorArray = sourceBmp.Palette.Entries ;


			byte			wThreshold = (byte) (Math.Max(Histogram.ToGray(histogram.Extreme), Histogram.ToGray(histogram.SecondExtreme)) + wThresholdDelta);		
			short			bThreshold = (byte) (Math.Min(Histogram.ToGray(histogram.Extreme), Histogram.ToGray(histogram.SecondExtreme)) + bThresholdDelta);

			wThreshold = (byte) (Math.Max(0, Math.Min(255, (int) wThreshold)));
			bThreshold = (byte) (Math.Max(0, Math.Min(wThreshold, (int) bThreshold)));

			byte		gray ;

			int			sourceStride = sourceData.Stride; 
			int			resultStride = resultData.Stride; 
			
			Rectangle	mask = Rectangle.Empty ;
			uint		sum = 0 ;
			byte		threshold ;
 
			int			clipXPlus4 = clip.X + 4 ;
			int			clipRightMinus5 = clip.Right - 5 ;

			unsafe
			{
				byte*		pSource = (byte*)sourceData.Scan0.ToPointer(); 
				byte*		pResult = (byte*)resultData.Scan0.ToPointer(); 
				byte*		pCurrent ;
				ushort*		columnSums = stackalloc ushort[resultData.Width] ;
				ushort*		columnSumsTmp = columnSums ;

				for(int i = 0; i < resultData.Width; i++)
					*(columnSumsTmp++) = 0 ;

				for(int y = 0; y < clip.Height; y++) 
				{ 
					sum = 0 ;
					mask.X = clip.X ;
					mask.Y = Math.Max(clip.Y, clip.Y + y - 4) ;
					mask.Width = 5 ;
					mask.Height = Math.Max(5, Math.Min(9, Math.Min(clip.Bottom - y + 4, y + 5))) ;
					
					GetColumnSums8Bpp(columnSums, pSource, colorArray, sourceStride, clip, (ushort) (y + 4)) ;
					
					pCurrent = pSource + ((y + clip.Y) * sourceStride) + clip.X ;

					for(int i = mask.X; i < mask.Right; i++)
						sum += columnSums[i] ;

					for(int x = 0; x < clip.Width; x++) 
					{ 		
						gray = colorArray[*pCurrent].R ;

						if(gray > wThreshold)
						{ 
							pResult[y * resultStride + ( x >> 3 )] |= (byte) (0x80 >> (x & 0x07)); //set the appropriate bit to 1
						}
						else if(gray >= bThreshold)
						{
							threshold = (byte) (sum / (mask.Width * mask.Height)) ;

							if(gray > threshold + 4)
								pResult[y * resultStride + ( x >> 3 )] |= (byte) (0x80 >> (x & 0x07)); 
						}

						if(x < clipXPlus4)
						{
							sum += columnSums[x + 5] ;
							mask.Width ++  ;
						}
						else if(x >= clipRightMinus5)
						{
							sum -= columnSums[x - 4] ;
							mask.X ++  ;
							mask.Width --  ;
						}
						else
						{
							sum += (uint) columnSums[x + 5] - columnSums[x - 4] ;
							mask.X ++  ;
						}

						pCurrent ++ ;
					}
				}
			}

			//ImageProcessing.NoiseReduction.Despeckle(resultData, clip, 1) ; 
			//ImageProcessing.NoiseReduction.Despeckle(resultData, clip, 2) ; 
			//ImageProcessing.NoiseReduction.Despeckle(resultData, clip, 3) ; 

			sourceBmp.UnlockBits(sourceData);
			resultBmp.UnlockBits(resultData); 

			return resultBmp; 
		}
		#endregion
		
		#region GetColumnSums32Bpp()
		private unsafe static void GetColumnSums32Bpp(ushort *columnSums, byte* pSource, int stride, Rectangle clip, ushort lastRow)
		{
			//for the first row
			if(lastRow <= 4)
			{
				byte*		pCurrent ;
				ushort		*columnSumsTmp ;

				for(int y = 0; y < 5; y++) 
				{
					pCurrent = pSource + ((y + clip.Y) * stride) + clip.X * 4 ;
					columnSumsTmp = columnSums ;

					for(int x = clip.X; x < clip.Right; x++) 
					{
						*(columnSumsTmp++) += *(pCurrent++) ;
						*(columnSumsTmp++) += *(pCurrent++) ;
						*(columnSumsTmp++) += *(pCurrent++) ;
						pCurrent++ ;
					}
				}
			}
			else
			{
				ushort*		columnSumsTmp ;

				//add last row to sum
				if(lastRow < clip.Bottom)
				{
					byte*		pCurrent = pSource + (lastRow * stride) + clip.X * 4 ;
					columnSumsTmp = columnSums ;

					for(int x = clip.X; x < clip.Right; x++) 
					{
						*(columnSumsTmp++) += *(pCurrent++) ;
						*(columnSumsTmp++) += *(pCurrent++) ;
						*(columnSumsTmp++) += *(pCurrent++) ;
						pCurrent++ ;
					}
				}

				//substract first row from sum
				if(lastRow > 8)
				{
					byte*		pCurrent = pSource + ((lastRow - 9) * stride) + clip.X * 4 ;
					columnSumsTmp = columnSums ;

					for(int x = clip.X; x < clip.Right; x++)
					{
						*(columnSumsTmp++) -= *(pCurrent++) ;
						*(columnSumsTmp++) -= *(pCurrent++) ;
						*(columnSumsTmp++) -= *(pCurrent++) ;
						pCurrent++ ;
					}
				}
			}
		}
		#endregion
		
		#region GetColumnSums24Bpp()
		private unsafe static void GetColumnSums24Bpp(ushort *columnSums, byte* pSource, int stride, Rectangle clip, ushort lastRow)
		{
			//for the first row
			if(lastRow <= 4)
			{
				byte*		pCurrent ;
				ushort		*columnSumsTmp ;

				for(int y = 0; y < 5; y++) 
				{
					pCurrent = pSource + ((y + clip.Y) * stride) + clip.X * 3 ;
					columnSumsTmp = columnSums ;

					for(int x = clip.X; x < clip.Right; x++) 
					{
						*(columnSumsTmp++) += *(pCurrent++) ;
						*(columnSumsTmp++) += *(pCurrent++) ;
						*(columnSumsTmp++) += *(pCurrent++) ;
					}
				}
			}
			else
			{
				ushort*		columnSumsTmp ;

				//add last row to sum
				if(lastRow < clip.Bottom)
				{
					byte*		pCurrent = pSource + (lastRow * stride) + clip.X * 3 ;
					columnSumsTmp = columnSums ;

					for(int x = clip.X; x < clip.Right; x++) 
					{
						*(columnSumsTmp++) += *(pCurrent++) ;
						*(columnSumsTmp++) += *(pCurrent++) ;
						*(columnSumsTmp++) += *(pCurrent++) ;
					}
				}

				//substract first row from sum
				if(lastRow > 8)
				{
					byte*		pCurrent = pSource + ((lastRow - 9) * stride) + clip.X * 3 ;
					columnSumsTmp = columnSums ;

					for(int x = clip.X; x < clip.Right; x++)
					{
						*(columnSumsTmp++) -= *(pCurrent++) ;
						*(columnSumsTmp++) -= *(pCurrent++) ;
						*(columnSumsTmp++) -= *(pCurrent++) ;
					}
				}
			}
		}
		#endregion
		
		#region GetColumnSums8Bpp()
		public unsafe static void GetColumnSums8Bpp(ushort *columnSums, byte* pSource, Color[] colorArray, int stride, Rectangle clip, ushort lastRow)
		{
			ushort*		pColSumsTmp ;

			//for the first row
			if(lastRow <= 4)
			{
				byte*		pCurrent ;

				for(int y = 0; y < 5; y++) 
				{ 					
					pCurrent = pSource + ((y + clip.Y) * stride) + clip.X ;
					pColSumsTmp = columnSums ;

					for(int x = clip.X; x < clip.Right; x++) 
						*(pColSumsTmp++) += colorArray[*(pCurrent++)].R ;
				}
			}
			else
			{
				//add last row to sum
				if(lastRow < clip.Bottom)
				{
					byte*		pCurrent = pSource + (lastRow * stride) + clip.X ;
					pColSumsTmp = columnSums ;

					for(int x = clip.X; x < clip.Right; x++) 
						*(pColSumsTmp++) += colorArray[*(pCurrent++)].R ;
				}

				//substract first row from sum
				if(lastRow > 8)
				{
					byte*		pCurrent = pSource + ((lastRow - 9) * stride) + clip.X ;
					pColSumsTmp = columnSums ;

					for(int x = clip.X; x < clip.Right; x++) 
						*(pColSumsTmp++) -= colorArray[*(pCurrent++)].R ;
				}
			}
		}

		public unsafe static void GetColumnSums8Bpp(ref ushort[] columnSums, byte* pSource, Color[] colorArray, int stride, Rectangle clip, ushort lastRow)
		{
			ushort*		pColSumsTmp ;

			//for the first row
			if(lastRow <= 4)
			{
				byte*		pCurrent ;

				fixed(ushort* columnSumsFixed = &(columnSums[0]))
				{
					for(int y = 0; y < 5; y++) 
					{ 					
						pCurrent = pSource + ((y + clip.Y) * stride) + clip.X ;
						pColSumsTmp = columnSumsFixed ;

						for(int x = clip.X; x < clip.Right; x++) 
							*(pColSumsTmp++) += colorArray[*(pCurrent++)].R ;
					}
				}
			}
			else
			{
				//add last row to sum
				if(lastRow < clip.Bottom)
				{
					fixed(ushort* columnSumsFixed = &(columnSums[0]))
					{
						byte*		pCurrent = pSource + (lastRow * stride) + clip.X ;
						pColSumsTmp = columnSumsFixed ;

						for(int x = clip.X; x < clip.Right; x++) 
							*(pColSumsTmp++) += colorArray[*(pCurrent++)].R ;
					}
				}

				//substract first row from sum
				if(lastRow > 8)
				{
					fixed(ushort* columnSumsFixed = &(columnSums[0]))
					{
						byte*		pCurrent = pSource + ((lastRow - 9) * stride) + clip.X ;
						pColSumsTmp = columnSumsFixed ;

						for(int x = clip.X; x < clip.Right; x++) 
							*(pColSumsTmp++) -= colorArray[*(pCurrent++)].R ;
					}
				}
			}
		}
		#endregion

	}
}
