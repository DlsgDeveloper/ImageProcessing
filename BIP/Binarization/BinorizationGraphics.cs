using System;
using System.Drawing ;
using System.Drawing.Imaging ;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class BinorizationGraphics
	{		
		private BinorizationGraphics()
		{
		}

		//	PUBLIC METHODS

		#region Binorize()	
		/*public static Bitmap Binorize(Bitmap source, short wThreshold, short bThreshold)
		{
			return Binorize(source, new Rectangle(0, 0, source.Width, source.Height), wThreshold, bThreshold) ;
		}
		
		public static Bitmap Binorize(Bitmap bmpSource, Rectangle clip, short wThreshold, short bThreshold)
		{
			if(bmpSource == null)
				return null ;

			if(clip.IsEmpty)
				clip = new Rectangle(new Point(0, 0), bmpSource.Size) ;

			Bitmap		bmpResult = null ;
			
			try
			{
				switch(bmpSource.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed :				
						bmpResult = Binorize8bpp(bmpSource, clip, wThreshold, bThreshold) ;
						break ;
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}

				Misc.SetBitmapResolution(bmpResult, bmpSource.HorizontalResolution, bmpSource.VerticalResolution);
			}
			catch(Exception ex)
			{
				throw new Exception("Unexpected Error: " + ex.Message + "\n Rescale()") ;
			}

			return bmpResult ;
		}*/
		#endregion

		//PRIVATE METHODS
		
		#region Binorize8bpp()
		private static Bitmap Binorize8bpp(Bitmap sourceBmp, Rectangle clip, short wThreshold, short bThreshold)
		{
			byte		gray ;
			Color[]		colorArray = sourceBmp.Palette.Entries ;

			Bitmap		resultBmp = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed); 
			BitmapData	sourceData = sourceBmp.LockBits(clip, ImageLockMode.ReadOnly, sourceBmp.PixelFormat ); 
			BitmapData	resultData = resultBmp.LockBits(new Rectangle(0, 0, resultBmp.Width, resultBmp.Height), ImageLockMode.WriteOnly, resultBmp.PixelFormat ); 

			int			sourceStride = sourceData.Stride; 
			int			resultStride = resultData.Stride; 
			
			ushort[]	columnSums = new ushort[resultData.Width] ;

			unsafe
			{
				byte*		pSource = (byte*)sourceData.Scan0.ToPointer(); 
				byte*		pResult = (byte*)resultData.Scan0.ToPointer(); 
				byte*		pCurrent ;
				Rectangle	mask = Rectangle.Empty ;
				uint		sum = 0 ;
				byte		threshold ;

				for(int y = 0; y < clip.Height; y++) 
				{ 
					sum = 0 ;
					mask.X = clip.X ;
					mask.Y = Math.Max(clip.Y, clip.Y + y - 4) ;
					mask.Width = 5 ;
					mask.Height = Math.Max(5, Math.Min(9, Math.Min(clip.Bottom - y + 4, y + 5))) ;
					
					DRS.GetColumnSums8Bpp(ref columnSums, pSource, colorArray, sourceStride, clip, (ushort) (y + 4)) ;
					
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
						else if(gray > bThreshold)
						{
							threshold = (byte) (sum / (mask.Width * mask.Height)) ;

							if(Math.Abs(gray - threshold) > 5)
							{
								if(gray > threshold)
								{
									pResult[y * resultStride + ( x >> 3 )] |= (byte) (0x80 >> (x & 0x07)); 
								}
							}
							else
							{
								if( ( ((gray * x * y) & 0x07) << 5) < gray )		// (((x * y) % 8) * 32) < gray
								{
									pResult[y * resultStride + ( x >> 3 )] |= (byte) (0x80 >> (x & 0x07)); 
								}
							}
						}

						if(x < clip.X + 4)
						{
							sum += columnSums[x + 5] ;
							mask.Width ++  ;
						}
						else if(x >= clip.Right - 5)
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

			resultBmp.UnlockBits(resultData); 

			return resultBmp; 
		}
		#endregion

	}
}
