using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for PercentageBlack.
	/// </summary>
	public class PercentageBlack
	{		
		#region constructor
		private PercentageBlack()
		{
		}
		#endregion

		//	PUBLIC METHODS
		#region public methods
		
		#region GetValue()
		public static double GetValue(BitmapData bitmapData, Color[] palette, Rectangle clip, short threshold)
		{		
			double theValue = 0 ;
			
			switch(bitmapData.PixelFormat)
			{
				case PixelFormat.Format32bppRgb :
				case PixelFormat.Format32bppArgb :
					theValue = GetPercentage32bppRgb(bitmapData, clip, threshold) ;
					break ;
				case PixelFormat.Format24bppRgb :
					theValue = GetPercentage24bpp(bitmapData, clip, threshold) ;
					break ;
				case PixelFormat.Format8bppIndexed :
					theValue = GetPercentage8bpp(bitmapData, palette, clip, threshold) ;
					break ;
				case PixelFormat.Format1bppIndexed :
					theValue = GetPercentage1bpp(bitmapData, clip) ;
					break ;
				default :
					throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
			}

			return theValue ;
		}
		#endregion

		#endregion

		//	PRIVATE METHODS
		#region private methods

		#region GetPercentage32bppRgb()
		private static double GetPercentage32bppRgb(BitmapData bitmapData, Rectangle clip, short threshold)
		{
			int			gray ;
			int			stride = bitmapData.Stride; 
			int			blackCounter = 0 ;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 

				for(int y = clip.Y; y < clip.Bottom; y++) 
				{ 
					pCurrent = pOrig + (y * stride) + clip.X * 4 ;

					for(int x = clip.X; x < clip.Right; x++) 
					{ 
						//gray = 0.299Red + 0.587Gray + 0.114Blue
						//pixels are stored in order: blue, green, red, alpha
						gray = (int) (pCurrent[2] * 0.299F + pCurrent[1] * 0.587F + pCurrent[0] * 0.114F) ;
						pCurrent += 4 ;

						if(gray <= threshold)
							blackCounter ++ ;
					} 
				}
			}

			return (double) blackCounter / (clip.Width * clip.Height) ;
		}
		#endregion

		#region GetPercentage24bpp()
		private static double GetPercentage24bpp(BitmapData bitmapData, Rectangle clip, short threshold)
		{
			int			gray ;
			int			stride = bitmapData.Stride; 
			int			blackCounter = 0 ;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 

				for(int y = clip.Y; y < clip.Bottom; y++) 
				{ 
					pCurrent = pOrig + (y * stride) + clip.X * 3  ;

					for(int x = 0; x < clip.Width; x++) 
					{ 
						//gray = 0.299Red + 0.587Gray + 0.114Blue
						//pixels are stored in order: blue, green, red
						gray = (int) ((*(pCurrent++)) * 0.114F + (*(pCurrent++)) * 0.587F + (*(pCurrent++)) * 0.299F) ;

						if(gray <= threshold)
							blackCounter ++ ;
					} 
				}
			}

			return (double) blackCounter / (clip.Width * clip.Height) ;
		}
		#endregion

		#region GetPercentage8bpp()
		private static double GetPercentage8bpp(BitmapData bitmapData, Color[] palette, Rectangle clip, short threshold)
		{
			int			gray ;
			Color		color ;
			int			stride = bitmapData.Stride; 
			int			blackCounter = 0 ;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 

				for(int y = clip.Y; y < clip.Bottom; y++) 
				{ 
					pCurrent = pOrig + (y * stride) + clip.X ;

					for(int x = 0; x < clip.Width; x++) 
					{ 
						color = palette[ *(pCurrent++) ] ;
						gray = (int) (color.R * 0.299F + color.G * 0.587F + color.B * 0.114F) ;

						if(gray <= threshold)
							blackCounter ++ ;
					} 
				}
			}

			return (double) blackCounter / (clip.Width * clip.Height) ;
		}
		#endregion

		#region GetPercentage1bpp()
		private static double GetPercentage1bpp(BitmapData bitmapData, Rectangle clip)
		{
			byte		color ;
			int			stride = bitmapData.Stride ;
			int			blackCounter = 0 ;

			unsafe
			{
				byte*	pOrig = (byte*)bitmapData.Scan0.ToPointer(); 
				int		i ; 

				for(int y = clip.Y; y < clip.Bottom; y++) 
					for(int x = clip.X / 8; x < clip.Right / 8; x++) 
					{ 
						color = pOrig[y * stride + x] ;

						for(i = 0; i < 8; i++)	
						{
							if( ((color >> i) & 0x1) == 0)
								blackCounter ++ ;
						}
					} 
			}

			return (double) blackCounter / (clip.Width * clip.Height) ;
		}
		#endregion

		#endregion

	}
}
