using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections ;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for FingerRemovalOld.
	/// max finger size is 2" width by 3.5" height 
	/// </summary>
	/*public class FingerRemovalOld
	{
		static int				maxFingerWidth ; 
		static int				maxFingerHeight ; 
		
		private FingerRemovalOld()
		{
		}

		//PUBLIC METHODS
		#region public methods

		#region FindAndEraseFingers()
		public static void FindAndEraseFingers(Bitmap bitmap, Color[] palette, Paging paging)
		{
			BitmapData	bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat ); 

			maxFingerWidth = Convert.ToInt32(bitmap.HorizontalResolution * 2F) ;
			maxFingerHeight = Convert.ToInt32(bitmap.VerticalResolution * 2F) ;

			ArrayList	clips = FindAndEraseFingers(bitmapData, palette, paging) ;

			bitmap.UnlockBits(bitmapData) ;
		}
		#endregion

		#region EraseFinger()
		public static void EraseFinger(Bitmap bitmap, Rectangle clip)
		{
			BitmapData	bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat ); 

			EraseFinger(bitmapData, bitmap.Palette.Entries, clip) ;

			bitmap.UnlockBits(bitmapData) ;
		}

		public static void EraseFinger(BitmapData bitmapData, Color[] palette, Rectangle clip)
		{
			switch(bitmapData.PixelFormat)
			{
				case PixelFormat.Format32bppRgb :
				case PixelFormat.Format32bppArgb :
					Erase32bppRgb(bitmapData, clip) ;
					break ;
				case PixelFormat.Format24bppRgb :
					Erase24bpp(bitmapData, clip) ;
					break ;
				case PixelFormat.Format8bppIndexed :
					Erase8bpp(bitmapData, palette, clip) ;
					break ;
				case PixelFormat.Format1bppIndexed :
					Erase1bpp(bitmapData, clip) ;
					break ;
				default :
					throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
			}
		}
		#endregion
		
		#endregion

		
		//	PRIVATE METHODS
		#region private methods

		#region FindAndEraseFingers()
		private static ArrayList FindAndEraseFingers(BitmapData bitmapData, Color[] palette, Paging paging)
		{
			Histogram		histogramL = new Histogram(bitmapData, palette, Rectangle.FromLTRB(0, 0, maxFingerWidth, bitmapData.Height) ) ;
			Histogram		histogramR = new Histogram(bitmapData, palette, Rectangle.FromLTRB(bitmapData.Width - maxFingerWidth, 0, bitmapData.Width, bitmapData.Height) ) ;
			Histogram		histogramB = new Histogram(bitmapData, palette, Rectangle.FromLTRB(0, bitmapData.Height - maxFingerHeight, bitmapData.Width, bitmapData.Height) ) ;
			Rectangle		clipTmp = Rectangle.Empty ;
			ArrayList		clips = new ArrayList() ;

			//histogramL.Show() ;

			if( paging != Paging.Right && FindFingerOnLeft(bitmapData, palette, ref clipTmp, histogramL.LighterThreshold, 0) )
				clips.Add(clipTmp) ;
			if( paging != Paging.Left &&  FindFingerOnRight(bitmapData, palette, ref clipTmp, histogramR.LighterThreshold, 0) )
				clips.Add(clipTmp) ;

			//clips.Add(new Rectangle(958,465,30,90)) ;

			foreach(Rectangle clip in clips)
				EraseFinger(bitmapData, palette, clip) ;

			return clips ;
		}
		#endregion

		#region FindFingerOnLeft()
		private static bool FindFingerOnLeft(BitmapData bitmapData, Color[] palette, ref Rectangle clip, 
			short imageThreshold, int fromY)
		{
			Rectangle	bwRect = new Rectangle(0, fromY, 50, 30) ; 
			double		bwThreshold = 0.1F ;
			bool		found = false ;
			int			border = 0 ;
			
			if( FindObjectDown(bitmapData, palette, bwRect, imageThreshold, bwThreshold, ref border) )
			{
				clip.Y = border ;
				bwRect.Y = clip.Y + (bwRect.Height / 2) ;

				if( FindBackgroundDown(bitmapData, palette, bwRect, imageThreshold, bwThreshold, ref border) )
					clip.Height = border - clip.Y ;
				else
					clip.Height = bitmapData.Height - clip.Y ;
			
				//check if finger
				bwRect = Rectangle.FromLTRB(0, clip.Y, 30, clip.Bottom) ;

				if( (PercentageBlack.GetValue(bitmapData, palette, bwRect, imageThreshold) >= 0.6F) && (clip.Height < bitmapData.Height * 0.3F))
				{
					if(FindBackgroundFromRight(bitmapData, palette, bwRect, imageThreshold, bwThreshold, ref border, Convert.ToInt32(bitmapData.Width * 0.15F) ))
					{
						clip.X = 0;
						clip.Y = Math.Max(0, clip.Y - 40) ;
						clip.Width = border ;
						clip.Height = Math.Min(bitmapData.Height, clip.Height + 80) ;

						found = true ;
					}
				}
				else if(clip.Bottom < bitmapData.Height - 100)
					found = FindFingerOnLeft(bitmapData, palette, ref clip, imageThreshold, clip.Bottom) ;
			}

			if(found)
			{
				clip.X = Math.Max(0, clip.X); 
				clip.Y = Math.Max(0, clip.Y); 
				clip.Width = Math.Min(bitmapData.Width - clip.X, clip.Width);
				clip.Height = Math.Min(bitmapData.Height - clip.Y, clip.Height);
			}

			return found ;
		}
		#endregion

		#region FindFingerOnRight()
		private static bool FindFingerOnRight(BitmapData bitmapData, Color[] palette, ref Rectangle clip, 
			short imageThreshold, int fromY)
		{
			Rectangle	bwRect = new Rectangle(bitmapData.Width - 50, fromY, 50, 20) ; 
			double		bwThreshold = 0.08F ;
			bool		found = false ;
			int			border = 0 ;
			
			if( FindObjectDown(bitmapData, palette, bwRect, imageThreshold, bwThreshold, ref border) )
			{
				clip.Y = border ;
				bwRect.Y = clip.Y + (bwRect.Height / 2) ;

				if( FindBackgroundDown(bitmapData, palette, new Rectangle(bitmapData.Width - 30, bwRect.Y, 30, bwRect.Height), imageThreshold, bwThreshold, ref border) )
					clip.Height = border - clip.Y ;
				else
					clip.Height = bitmapData.Height - clip.Y ;
			
				//check if finger
				bwRect = new Rectangle(bitmapData.Width - 20, clip.Y, 20, clip.Height) ;

				if( (PercentageBlack.GetValue(bitmapData, palette, bwRect, imageThreshold) >= 0.6F) && (clip.Height < bitmapData.Height * 0.3F))
				{
					if(FindBackgroundFromLeft(bitmapData, palette, bwRect, imageThreshold, bwThreshold, ref border, Convert.ToInt32(bitmapData.Width - bitmapData.Width * 0.15F) ))
					{
						clip.X = border;
						clip.Y = Math.Max(0, clip.Y - 40) ;
						clip.Width = bitmapData.Width - clip.X ;
						clip.Height = Math.Min(bitmapData.Height - clip.Y, clip.Height + 80) ;

						found = true ;
					}
				}
				else if(clip.Bottom < bitmapData.Height - 100)
					found = FindFingerOnRight(bitmapData, palette, ref clip, imageThreshold, clip.Bottom) ;
			}

			if(found)
			{
				clip.X = Math.Max(0, clip.X); 
				clip.Y = Math.Max(0, clip.Y); 
				clip.Width = Math.Min(bitmapData.Width - clip.X, clip.Width);
				clip.Height = Math.Min(bitmapData.Height - clip.Y, clip.Height);
			}

			return found ;
		}
		#endregion

		#region FindFingerOnBottom()
		private static bool FindFingerOnBottom(BitmapData bitmapData, Color[] palette, ref Rectangle clip, 
			short imageThreshold, int fromX)
		{
			Rectangle	bwRect = Rectangle.FromLTRB(fromX, bitmapData.Height - 50, fromX + 30, bitmapData.Height) ; 
			double		bwThreshold = 0.1F ;
			bool		found = false ;
			int			border = 0 ;
			
			if( FindObjectRight(bitmapData, palette, bwRect, imageThreshold, bwThreshold, ref border) )
			{
				clip.X = border ;
				bwRect.X = clip.X + (bwRect.Width / 2) ;

				if( FindBackgroundFromRight(bitmapData, palette, bwRect, imageThreshold, bwThreshold, ref border, bitmapData.Width - bwRect.Width) )
					clip.Width = border - clip.X ;
				else
					clip.Width = bitmapData.Width - clip.X ;
			
				//check if finger
				bwRect = new Rectangle(clip.X, bitmapData.Height - 30, clip.Width, 30) ;

				if( (PercentageBlack.GetValue(bitmapData, palette, bwRect, imageThreshold) >= 0.4F) && (clip.Width < bitmapData.Width * 0.3F))
				{
					if(FindBackgroundTop(bitmapData, palette, bwRect, imageThreshold, bwThreshold, ref border, Convert.ToInt32(bitmapData.Height * 0.85F) ))
					{
						clip.X = Math.Max(0, clip.X - 40);
						clip.Y = border ;
						clip.Width = Math.Min(bitmapData.Width - clip.X, clip.Width + 80) ;
						clip.Height = bitmapData.Height - clip.Y ;

						found = true ;
					}
				}
				else if(clip.Left < bitmapData.Width - 100)
					found = FindFingerOnBottom(bitmapData, palette, ref clip, imageThreshold, clip.Left) ;
			}

			if(found)
			{
				clip.X = Math.Max(0, clip.X); 
				clip.Y = Math.Max(0, clip.Y); 
				clip.Width = Math.Min(bitmapData.Width - clip.X, clip.Width);
				clip.Height = Math.Min(bitmapData.Height - clip.Y, clip.Height);
			}

			return found ;
		}
		#endregion

		#region FindObjectDown()
		private static bool FindObjectDown(BitmapData bitmapData, Color[] palette, Rectangle bwRect, 
			short imageThreshold, double bwThreshold, ref int theValue)
		{
			int limit = bitmapData.Height - bwRect.Height ;
			
			for( ; bwRect.Y < limit; bwRect.Y += (bwRect.Height / 2))
			{
				if( PercentageBlack.GetValue(bitmapData, palette, bwRect, imageThreshold) > bwThreshold )
				{
					theValue = bwRect.Y + (bwRect.Height / 2) ;
					return true ;
				}
			}

			return false ;
		}
		#endregion
		
		#region FindBackgroundDown()
		private static bool FindBackgroundDown(BitmapData bitmapData, Color[] palette, Rectangle bwRect, 
			short imageThreshold, double bwThreshold, ref int theValue)
		{
			int limit = bitmapData.Height - bwRect.Height ;

			for( ; bwRect.Y < limit; bwRect.Y += (bwRect.Height / 2))
			{
				if( PercentageBlack.GetValue(bitmapData, palette, bwRect, imageThreshold) < bwThreshold )
				{
					//theValue = bwRect.Y + (bwRect.Height / 2) ;
					theValue = bwRect.Y ;
					return true ;
				}
			}

			return false ;
		}
		#endregion

		#region FindObjectRight()
		private static bool FindObjectRight(BitmapData bitmapData, Color[] palette, Rectangle bwRect, 
			short imageThreshold, double bwThreshold, ref int theValue)
		{
			int		limit = bitmapData.Width - bwRect.Width ;
			
			for( ; bwRect.X < limit; bwRect.X += (bwRect.Width / 2))
			{
				if( PercentageBlack.GetValue(bitmapData, palette, bwRect, imageThreshold) > bwThreshold )
				{
					theValue = bwRect.X + (bwRect.Width / 2) ;
					return true ;
				}
			}

			return false ;
		}
		#endregion
		
		#region FindBackgroundFromRight()
		private static bool FindBackgroundFromRight(BitmapData bitmapData, Color[] palette, Rectangle bwRect, 
			short imageThreshold, double bwThreshold, ref int theValue, int limit)
		{		
			for( ; bwRect.X < limit; bwRect.X += (bwRect.Width / 2))
			{
				if( PercentageBlack.GetValue(bitmapData, palette, bwRect, imageThreshold) < bwThreshold )
				{
					theValue = bwRect.X + (bwRect.Width / 2) ;
					return true ;
				}
			}

			return false ;
		}
		#endregion

		#region FindObjectLeft()
		private static bool FindObjectLeft(BitmapData bitmapData, Color[] palette, Rectangle bwRect, 
			short imageThreshold, double bwThreshold, ref int theValue, int limit)
		{
			for( ; bwRect.X > limit; bwRect.X -= (bwRect.Width / 2))
			{
				if( PercentageBlack.GetValue(bitmapData, palette, bwRect, imageThreshold) > bwThreshold )
				{
					theValue = bwRect.X + (bwRect.Width / 2) ;
					return true ;
				}
			}

			return false ;
		}
		#endregion

		#region FindBackgroundFromLeft()
		private static bool FindBackgroundFromLeft(BitmapData bitmapData, Color[] palette, Rectangle bwRect, 
			short imageThreshold, double bwThreshold, ref int theValue, int limit)
		{
			for( ; bwRect.X > limit; bwRect.X -= (bwRect.Width / 2))
			{
				if( PercentageBlack.GetValue(bitmapData, palette, bwRect, imageThreshold) < bwThreshold )
				{
					theValue = bwRect.X + (bwRect.Width / 2) ;
					return true ;
				}
			}

			return false ;
		}
		#endregion

		#region FindBackgroundTop()
		private static bool FindBackgroundTop(BitmapData bitmapData, Color[] palette, Rectangle bwRect, 
			short imageThreshold, double bwThreshold, ref int theValue, int limit)
		{
			for( ; bwRect.Y > limit; bwRect.Y -= (bwRect.Height / 2))
			{
				if( PercentageBlack.GetValue(bitmapData, palette, bwRect, imageThreshold) < bwThreshold )
				{
					theValue = bwRect.Y + (bwRect.Height / 2) ;
					return true ;
				}
			}

			return false ;
		}
		#endregion

		#region Erase32bppRgb()
		private static void Erase32bppRgb(BitmapData bitmapData, Rectangle clip)
		{
			int			stride = bitmapData.Stride; 

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 
				byte*	pBckgT ;
				byte*	pBckgB ;
				byte*	pBckgL ;
				byte*	pBckgR ;
				int		width = clip.Width ;
				int		height = clip.Height ;

				//top + bottom of clip has background color
				if(clip.Y > 0 && clip.Bottom < bitmapData.Height)
				{
					int		toNextColor = (clip.X == 0) ? 4 : -4 ;
					
					for(int y = 0; y < clip.Height; y++) 
					{ 
						pCurrent = pOrig + ((clip.Y + y) * stride) + clip.X * 4 ;

						pBckgT = pOrig + ((clip.Y + (y % 15)) * stride) + clip.X * 4 ;
						pBckgB = pOrig + ((clip.Bottom - (y % 15)) * stride) + clip.X * 4 ;

						for(int x = 0; x < clip.Width; x++) 
						{ 
							*(pCurrent++) = (byte) ((*(pBckgT++) * (height - y) / height) + (*(pBckgB++) * y / height)) ;
							*(pCurrent++) = (byte) ((*(pBckgT++) * (height - y) / height) + (*(pBckgB++) * y / height)) ;
							*(pCurrent++) = (byte) ((*(pBckgT++) * (height - y) / height) + (*(pBckgB++) * y / height)) ;
							pCurrent++ ;
							pBckgT++ ;
							pBckgB++ ;
						} 
					}
				}
				//top + bottom of clip has NOT background color
				else if (clip.Y <= 0 && clip.Bottom >= bitmapData.Height)
				{
					int		toNextColor = (clip.X == 0) ? 4 : -4 ;

					for(int y = 0; y < clip.Height; y++) 
					{ 
						pCurrent = pOrig + ((clip.Y + y) * stride) + clip.X * 4 ;
						pBckgL = (clip.X > 0) ? pCurrent : pCurrent + (clip.Width * 4) ;
						pBckgR = (clip.Right < bitmapData.Width) ?  pCurrent + (clip.Width * 4) : pBckgL ;

						for(int x = 0; x < clip.Width; x++) 
						{ 
							*(pCurrent++) = (byte) ((((pBckgL[0] + pBckgL[0 + toNextColor]) * (width - x) / width) + ((pBckgR[0] + pBckgR[0 + toNextColor]) * x / width)) >> 1 ) ;
							*(pCurrent++) = (byte) ((((pBckgL[1] + pBckgL[1 + toNextColor]) * (width - x) / width) + ((pBckgR[1] + pBckgR[1 + toNextColor]) * x / width)) >> 1 ) ;
							*(pCurrent++) = (byte) ((((pBckgL[2] + pBckgL[2 + toNextColor]) * (width - x) / width) + ((pBckgR[2] + pBckgR[2 + toNextColor]) * x / width)) >> 1 ) ;
							pCurrent++ ;
						} 
					}
				}
				//top or bottom of clip has NOT background color
				else
				{
					for(int y = 0; y < clip.Height; y++) 
					{ 
						pCurrent = pOrig + ((clip.Y + y) * stride) + clip.X * 4 ;
						pBckgL = (clip.X > 0) ? pCurrent : pCurrent + (clip.Width * 4) ;
						pBckgR = (clip.Right < bitmapData.Width) ?  pCurrent + (clip.Width * 4) : pBckgL ;
						pBckgT = (clip.Y > 0) ? pOrig + ((clip.Y + (y % 5)) * stride) + clip.X * 4 : pOrig + ((clip.Bottom - (y % 5)) * stride) + clip.X * 4 ;

						for(int x = 0; x < clip.Width; x++) 
						{ 
							*(pCurrent++) = (byte) (( (*(pBckgT++) * 3) + (pBckgL[0] * (width - x) / width) + (pBckgR[0] * x / width)) >> 2 ) ;
							*(pCurrent++) = (byte) (( (*(pBckgT++) * 3) + (pBckgL[1] * (width - x) / width) + (pBckgR[1] * x / width)) >> 2 ) ;
							*(pCurrent++) = (byte) (( (*(pBckgT++) * 3) + (pBckgL[2] * (width - x) / width) + (pBckgR[2] * x / width)) >> 2 ) ;
							pCurrent++ ;
							pBckgT++ ;
						} 
					}
				}
			}			
		}
		#endregion
		
		#region Erase24bpp()
		private static void Erase24bpp(BitmapData bitmapData, Rectangle clip)
		{
			int			stride = bitmapData.Stride; 

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 
				byte*	pBckgT ;
				byte*	pBckgB ;
				byte*	pBckgL ;
				byte*	pBckgR ;
				int		width = clip.Width ;
				int		height = clip.Height ;

				//top + bottom of clip has background color
				if(clip.Y > 0 && clip.Bottom < bitmapData.Height)
				{
					int		toNextColor = (clip.X == 0) ? 3 : -3 ;
					
					for(int y = 0; y < clip.Height; y++) 
					{ 
						pCurrent = pOrig + ((clip.Y + y) * stride) + clip.X * 3 ;

						pBckgT = pOrig + ((clip.Y + (y % 15)) * stride) + clip.X * 3 ;
						pBckgB = pOrig + ((clip.Bottom - (y % 15)) * stride) + clip.X * 3 ;

						for(int x = 0; x < clip.Width; x++) 
						{ 
							*(pCurrent++) = (byte) ((*(pBckgT++) * (height - y) / height) + (*(pBckgB++) * y / height)) ;
							*(pCurrent++) = (byte) ((*(pBckgT++) * (height - y) / height) + (*(pBckgB++) * y / height)) ;
							*(pCurrent++) = (byte) ((*(pBckgT++) * (height - y) / height) + (*(pBckgB++) * y / height)) ;
						} 
					}
				}
				//top + bottom of clip has NOT background color
				else if (clip.Y <= 0 && clip.Bottom >= bitmapData.Height)
				{
					int		toNextColor = (clip.X == 0) ? 3 : -3 ;

					for(int y = 0; y < clip.Height; y++) 
					{ 
						pCurrent = pOrig + ((clip.Y + y) * stride) + clip.X * 3 ;
						pBckgL = (clip.X > 0) ? pOrig + ((clip.Y + y) * stride) + (clip.X * 3) : pOrig + ((clip.Y + y) * stride) + (clip.Right * 3) ;
						pBckgR = (clip.Right < bitmapData.Width) ?  pOrig + ((clip.Y + y) * stride) + (clip.Right * 3) : pBckgL ;

						for(int x = 0; x < clip.Width; x++) 
						{ 
							*(pCurrent++) = (byte) ((((pBckgL[0] + pBckgL[toNextColor])     * (width - x) / width) + ((pBckgR[0] + pBckgR[toNextColor])     * x / width)) >> 1 ) ;
							*(pCurrent++) = (byte) ((((pBckgL[1] + pBckgL[1 + toNextColor]) * (width - x) / width) + ((pBckgR[1] + pBckgR[1 + toNextColor]) * x / width)) >> 1 ) ;
							*(pCurrent++) = (byte) ((((pBckgL[2] + pBckgL[2 + toNextColor]) * (width - x) / width) + ((pBckgR[2] + pBckgR[2 + toNextColor]) * x / width)) >> 1 ) ;
						} 
					}
				}
				//top or bottom of clip has NOT background color
				else
				{
					for(int y = 0; y < clip.Height; y++) 
					{ 
						pCurrent = pOrig + ((clip.Y + y) * stride) + clip.X * 3 ;
						pBckgL = (clip.X > 0) ? pOrig + ((clip.Y + y) * stride) + (clip.X * 3) : pOrig + ((clip.Y + y) * stride) + (clip.Right * 3) ;
						pBckgR = (clip.Right < bitmapData.Width) ?  pOrig + ((clip.Y + y) * stride) + (clip.Right * 3) : pBckgL ;
						pBckgT = (clip.Y > 0) ? pOrig + ((clip.Y + (y % 5)) * stride) + clip.X * 3 : pOrig + ((clip.Bottom - (y % 5)) * stride) + clip.X * 3 ;

						for(int x = 0; x < clip.Width; x++) 
						{ 
							*(pCurrent++) = (byte) (( (*(pBckgT++) * 3) + (*pBckgL   * (width - x) / width) + (*pBckgR   * x / width)) >> 2 ) ;
							*(pCurrent++) = (byte) (( (*(pBckgT++) * 3) + (pBckgL[1] * (width - x) / width) + (pBckgR[1] * x / width)) >> 2 ) ;
							*(pCurrent++) = (byte) (( (*(pBckgT++) * 3) + (pBckgL[2] * (width - x) / width) + (pBckgR[2] * x / width)) >> 2 ) ;
						} 
					}
				}
			}			
		}
		#endregion

		#region Erase8bpp()
		private static void Erase8bpp(BitmapData bitmapData, Color[] palette, Rectangle clip)
		{
			int			stride = bitmapData.Stride; 

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 
				byte*	pBckgT ;
				byte*	pBckgB ;
				byte*	pBckgL ;
				byte*	pBckgR ;
				int		width = clip.Width ;
				int		height = clip.Height ;

				byte[]			paletteInv = new byte[256] ;

				for(int i = 0; i < 256; i++)
					paletteInv[palette[i].R] = (byte) i ;

				//top + bottom of clip has background color
				if(clip.Y > 0 && clip.Bottom < bitmapData.Height)
				{
					for(int y = 0; y < clip.Height; y++) 
					{ 
						pCurrent = pOrig + ((clip.Y + y) * stride) + clip.X ;
						pBckgT = pOrig + ((clip.Y + (y % 15)) * stride) + clip.X ;
						pBckgB = pOrig + ((clip.Bottom - (y % 15)) * stride) + clip.X ;

						for(int x = 0; x < clip.Width; x++) 
							*(pCurrent++) = paletteInv[( palette[*(pBckgT++)].R * (height - y) / height) + (palette[*(pBckgB++)].R * y / height)] ;
					}
				}
				//top + bottom of clip has NOT background color
				else if (clip.Y <= 0 && clip.Bottom >= bitmapData.Height)
				{
					for(int y = 0; y < clip.Height; y++) 
					{ 
						pCurrent = pOrig + ((clip.Y + y) * stride) + clip.X ;
						pBckgL = (clip.X > 0) ? pOrig + ((clip.Y + y) * stride) + clip.X : pOrig + ((clip.Y + y) * stride) + clip.Right ;
						pBckgR = (clip.Right < bitmapData.Width) ?  pOrig + ((clip.Y + y) * stride) + clip.Right : pBckgL ;

						for(int x = 0; x < clip.Width; x++) 
							*(pCurrent++) = paletteInv[(palette[*(pBckgL++)].R * (width - x) / width) + (palette[*(pBckgR++)].R * x / width)] ;
					}
				}
				//top or bottom of clip has NOT background color
				else
				{
					for(int y = 0; y < clip.Height; y++) 
					{ 
						pCurrent = pOrig + ((clip.Y + y) * stride) + clip.X ;
						pBckgL = (clip.X > 0) ? pOrig + ((clip.Y + y) * stride) + clip.X : pOrig + ((clip.Y + y) * stride) + clip.Right ;
						pBckgR = (clip.Right < bitmapData.Width) ?  pOrig + ((clip.Y + y) * stride) + clip.Right : pBckgL ;
						pBckgT = (clip.Y > 0) ? pOrig + (clip.Y * stride) + clip.X : pOrig + (clip.Bottom * stride) + clip.X ;

						for(int x = 0; x < clip.Width; x++) 
							*(pCurrent++) = paletteInv[ ((palette[*(pBckgT++)].R * 3) + (palette[*pBckgL].R * (width - x) / width) + (palette[*pBckgR].R * x / width)) >> 2] ;
					}
				}
			}			
		}
		#endregion

		#region Erase1bpp()
		private static void Erase1bpp(BitmapData bitmapData, Rectangle clip)
		{
			int			stride = bitmapData.Stride ;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer(); 

				for(int y = clip.Y; y < clip.Bottom; y++) 
					for(int x = clip.X / 8; x <= clip.Right / 8; x++) 
					{ 
						pOrig[y * stride + x] = 255;
					} 
			}

		}
		#endregion

		#endregion

	}*/


}
