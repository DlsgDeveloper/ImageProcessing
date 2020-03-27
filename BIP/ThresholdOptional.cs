using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	public class ThresholdOptional
	{
		
		#region constructor
		private ThresholdOptional()
		{
		}
		#endregion

		//	PUBLIC PROPERTIES
		#region public properties
		#endregion

		
		//	PUBLIC METHODS
		#region public methods

		#region GetThreshold()
		public static byte GetThreshold(Bitmap image, Rectangle clip)
		{
			BitmapData	bitmapData = null ;
			
			try
			{
				bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat); 
				return GetThreshold( bitmapData, image.Palette.Entries, Rectangle.FromLTRB(0, 0, image.Width, image.Height) ) ;
			}
			finally
			{
				image.UnlockBits(bitmapData) ;
			}
		}

		public static byte GetThreshold(BitmapData bitmapData, Color[] palette, Rectangle clip)
		{
			switch(bitmapData.PixelFormat)
			{
				case PixelFormat.Format32bppRgb :
					return GetThreshold32bppRgb(bitmapData, clip) ;
				case PixelFormat.Format32bppArgb :
					return GetThreshold32bppArgb(bitmapData, clip) ;
				case PixelFormat.Format24bppRgb :
					return GetThreshold24bpp(bitmapData, clip) ;
				case PixelFormat.Format8bppIndexed :
					return GetThreshold8bpp(bitmapData, palette, clip) ;
				case PixelFormat.Format1bppIndexed :
					return 128 ;
				default :
					throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
			}
		}
		#endregion

		#endregion

		//	PRIVATE METHODS
		#region private methods

		#region GetThreshold32bppRgb()
		private static byte GetThreshold32bppRgb(BitmapData bitmapData, Rectangle clip)
		{
			/*int			gray ;
			int			stride = bitmapData.Stride; 
			//DateTime	start = DateTime.Now ;

			for(int i = 0; i < array.Length ; i++)
				array[i] = 0 ;

			unsafe
			{
				byte*	pOrig = (byte*)bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 

				for(int y = clip.Y; y < clip.Bottom; y++) 
				{ 
					pCurrent = pOrig + (y * stride) + (clip.X * 4) ;

					for(int x = clip.X; x < clip.Right; x++) 
					{ 
						//gray = 0.299Red + 0.587Gray + 0.114Blue
						//pixels are stored in order: blue, green, red, alpha
						gray = (int) (pCurrent[2] * 0.299F + pCurrent[1] * 0.587F + pCurrent[0] * 0.114F) ;
						pCurrent += 4 ;

						array[gray] ++ ;
					} 
				}
			}

			//Console.WriteLine("GetHistogram24bpp() total time: " +  DateTime.Now.Subtract(start).ToString()) ;*/
			return 128 ;
		}
		#endregion
		
		#region GetThreshold32bppArgb()
		private static byte GetThreshold32bppArgb(BitmapData bitmapData, Rectangle clip)
		{
			/*int			gray ;
			int			stride = bitmapData.Stride; 
			//DateTime	start = DateTime.Now ;

			for(int i = 0; i < array.Length ; i++)
				array[i] = 0 ;

			unsafe
			{
				byte*	pOrig = (byte*)bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 

				for(int y = clip.Y; y < clip.Bottom; y++) 
				{ 
					pCurrent = pOrig + (y * stride) + (clip.X * 4) ;

					for(int x = clip.X; x < clip.Right; x++) 
					{ 
						//gray = 0.299Red + 0.587Gray + 0.114Blue
						//pixels are stored in order: blue, green, red, alpha
						gray = (int) (pCurrent[2] * 0.299F + pCurrent[1] * 0.587F + pCurrent[0] * 0.114F) ;
						pCurrent += 4 ;

						array[gray] ++ ;
					} 
				}
			}

			//Console.WriteLine("GetHistogram24bpp() total time: " +  DateTime.Now.Subtract(start).ToString()) ;*/
			return 128 ;
		}
		#endregion

		#region GetThreshold24bpp()
		private static byte GetThreshold24bpp(BitmapData bitmapData, Rectangle clip)
		{
			/*int			gray ;
			int			stride = bitmapData.Stride; 
			//DateTime	start = DateTime.Now ;

			for(int i = 0; i < array.Length ; i++)
				array[i] = 0 ;

			unsafe
			{
				byte*	pOrig = (byte*)bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 

				for(int y = clip.Y; y < clip.Bottom; y++) 
				{ 
					pCurrent = pOrig + (y * stride) + (clip.X * 3) ;

					for(int x = clip.X; x < clip.Right; x++) 
					{ 
						//gray = 0.299Red + 0.587Gray + 0.114Blue
						//pixels are stored in order: blue, green, red
						gray = (int) ((*(pCurrent++)) * 0.114F + (*(pCurrent++)) * 0.587F + (*(pCurrent++)) * 0.299F) ;

						array[gray] ++ ;
					} 
				}
			}

			//Console.WriteLine("GetHistogram24bpp() total time: " +  DateTime.Now.Subtract(start).ToString()) ;*/
			return 128 ;
		}
		#endregion

		#region GetThreshold8bpp()
		private static byte GetThreshold8bpp(BitmapData bitmapData, Color[] palette, Rectangle clip)
		{
			int			gray ;
			int			stride = bitmapData.Stride; 
			ulong		sumObjects ;
			ulong		sumBankground = 220 ;
			uint		countObjects ;
			uint		countBackground = 1 ;
			byte		threshold = 0 ;
			byte		thresholdNew = 255 ;
			//DateTime	start = DateTime.Now ;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 

				while(Math.Abs(threshold - thresholdNew) > 1)
				{				
					threshold = thresholdNew ;
					sumObjects = 0 ;
					sumBankground = 0 ;
					countObjects = 0 ;
					countBackground = 0 ;
					
					for(int y = clip.Y; y < clip.Bottom; y++) 
					{ 
						pCurrent = pOrig + (y * stride) + clip.X ;

						for(int x = clip.X; x < clip.Right; x++) 
						{ 
							gray = palette[ *(pCurrent++) ].R ;
							
							if(gray < threshold) 
							{
								sumObjects += (ulong) gray ;
								countObjects ++ ;
							}
							else
							{
								sumBankground += (ulong) gray ;
								countBackground ++ ;
							}
						} 
					}

					if(countObjects > 0 && countBackground > 0)
						thresholdNew = (byte) (((sumObjects / (float) countObjects) + (sumBankground / (float) countBackground)) / 2) ;
					else
						thresholdNew = (byte) ((sumObjects / (float) Math.Max(1, countObjects)) + (sumBankground / (float) Math.Max(1, countBackground))) ;
				}
			}

			//Console.WriteLine("GetThreshold8bpp() Time: " +  DateTime.Now.Subtract(start).ToString()) ;

			return (byte) (sumBankground / (float) Math.Max(1, countBackground)) ; //threshold ;
		}
		#endregion

		#endregion
	}
}
