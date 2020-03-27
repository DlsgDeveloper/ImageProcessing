using System;
using System.Drawing ;
using System.Drawing.Imaging ;

namespace ImageProcessing
{
	public class Bookfold1D
	{
		#region constructor
		public Bookfold1D()
		{
		}
		#endregion

		//	PUBLIC METHODS
		#region GetCorrectedImage()	
		public static Bitmap GetCorrectedImage(Bitmap source, Lines lines)
		{
			return GetCorrectedImage(source, lines, new Rectangle(0, 0, 0, 0)) ;
		}
		
		public static Bitmap GetCorrectedImage(Bitmap bmpSource, Lines lines, Rectangle cropRect)
		{
			//DateTime	start = DateTime.Now ;
			if(bmpSource == null)
				return null ;

			const int TEN_ROWS = 10 ;
			const int TEN_COLS = 10 ;

			if(cropRect.IsEmpty)
				cropRect = new Rectangle(new Point(0, 0), bmpSource.Size) ;

			Bitmap		bmpResult = null ;
			
			int					rowPointsCount = PointsCount(cropRect.Height, TEN_ROWS) ;
			int					colPointsCount = PointsCount(cropRect.Width, TEN_COLS) ;
			int					destWidth = 0 ;
			RescalePoint[,]		points = new RescalePoint[rowPointsCount, colPointsCount] ;

			try
			{
				destWidth = GetRescalePoints(lines, ref points, cropRect, TEN_ROWS, TEN_COLS) ;
			}
			catch(Exception ex)
			{
				throw new Exception("Unexpected Error: " + ex.Message + "\n GetRescalePoints()") ;
			}

			PixelFormat	pixelFormat = (bmpSource.PixelFormat == PixelFormat.Format1bppIndexed || bmpSource.PixelFormat == PixelFormat.Format8bppIndexed) ?  bmpSource.PixelFormat : PixelFormat.Format24bppRgb ;
			bmpResult = new Bitmap(destWidth, cropRect.Height, pixelFormat) ;

			bmpResult.SetResolution(bmpSource.VerticalResolution, bmpSource.HorizontalResolution) ;
			if(pixelFormat == PixelFormat.Format8bppIndexed)
				bmpResult.Palette =  bmpSource.Palette ;

			//foreach(PropertyItem propertyItem in bmpSource.PropertyItems)
			//	bmpResult.SetPropertyItem( propertyItem ) ;
					
			BitmapData	sourceData = null ; 
			BitmapData	resultData = null ; 

			try
			{
				sourceData = bmpSource.LockBits(new Rectangle(0, 0, bmpSource.Width, bmpSource.Height), ImageLockMode.ReadOnly, pixelFormat ); 
				resultData = bmpResult.LockBits(new Rectangle(0, 0, bmpResult.Width, bmpResult.Height), ImageLockMode.WriteOnly, pixelFormat ); 

				switch(pixelFormat)
				{
					case PixelFormat.Format1bppIndexed :				
						Rescale1bpp(sourceData, resultData, points, cropRect.Location, TEN_ROWS, TEN_COLS) ;
						break ;
					case PixelFormat.Format8bppIndexed :				
						Rescale8bpp(bmpSource.Palette.Entries, sourceData, resultData, points, cropRect.Location, TEN_ROWS, TEN_COLS) ;
						break ;
					default :				
						Rescale24bpp(sourceData, resultData, points, cropRect.Location, TEN_ROWS, TEN_COLS) ;
						break ;
				}
			}
			catch(Exception ex)
			{
				throw new Exception("Unexpected Error: " + ex.Message + "\n Rescale()") ;
			}
			finally
			{
				if( bmpSource != null )
					bmpSource.UnlockBits(sourceData) ;
					
				if( bmpResult != null )
					bmpResult.UnlockBits(resultData); 
			}

			//Console.WriteLine("GetCorrectedImage(): " + DateTime.Now.Subtract(start).ToString()) ;
			return bmpResult ;
		}
		#endregion

		//	PRIVATE METHODS
		#region PointsCount()
		private static int PointsCount( int imageDimension, int TEN_ROWS )
		{
			return (int) Math.Ceiling( (float) imageDimension / TEN_ROWS ) + 1 ;
		}
		#endregion

		#region GetRescalePoints()
		private static int GetRescalePoints(Lines lines, ref RescalePoint[,] rescalePoints, Rectangle cropRect, int TEN_ROWS, int TEN_COLS)
		{
			int				sX, sY ;
			float			dX ;
			int				destWidth = 0 ;
			int				index;
			RescalePoint	tmpPoint = new RescalePoint(100,100,1) ;
  
			for ( int j = 0 ; j < rescalePoints.GetLength(0); j++ )
			{
				sX = cropRect.X ;
				sY = Math.Min( cropRect.Y + j * TEN_COLS, cropRect.Bottom ) ;
				dX = 0 ;
				index = 0 ;
      
				for ( int i = 0 ; i < ( lines.Size - 1 ) ; i++ )
				{
					Line		line1 = lines[i] ;
					Line		line2 = lines[i + 1] ;
      
					// Get fromX, toX, fR1, fR2
					float	fromX = line1.Point0.X + (( float ) ( line1.PointN.X - line1.Point0.X ) * sX / ( line1.PointN.Y - line1.Point0.Y )) ;
					float	toX = line2.Point0.X + (( float ) ( line2.PointN.X - line2.Point0.X ) * ( sX + line2.PointN.X ) / ( line2.PointN.Y - line2.Point0.Y )) ;
					toX = Math.Min(toX, cropRect.Right) ;

					// Get the formula parameters
					while ( (sX < toX) || (sX == cropRect.Right) )
					{
						// Calculate source coordinates and scale factor
						tmpPoint.X  = dX ;
						tmpPoint.Y  = sY ;
						if(sX < toX)
							tmpPoint.Scale = line1.Scale + ( line2.Scale - line1.Scale ) * ( sX - fromX ) / ( toX - fromX ) ;
						else
							tmpPoint.Scale = line2.Scale ; 

						dX +=  TEN_COLS / ((float) (line1.Scale + line2.Scale) / 2) ;	
						destWidth = Math.Max(destWidth, Convert.ToInt32(tmpPoint.X)) ;
						rescalePoints[j,index] = tmpPoint.Clone() ;
						index++ ;
         
						sX += TEN_COLS ;
					}   
					if ( (sX < toX + TEN_COLS) && (toX == cropRect.Right) )	
					{
						tmpPoint.X = dX ; //+ (- TEN_COLS + (toX % TEN_COLS)) / ((float) (line1.Scale + line2.Scale) / 2) ;
						tmpPoint.Y = sY ;
						tmpPoint.Scale = line2.Scale ; 

						destWidth = Math.Max(destWidth, Convert.ToInt32(dX + (- TEN_COLS + (toX % TEN_COLS)) / ((float) (line1.Scale + line2.Scale) / 2))) ;
						rescalePoints[j,index] = tmpPoint.Clone() ;
						break ;
					}
				}
			}

			return destWidth ;
		}	
		#endregion
		
		#region Rescale24bpp()
		// Rescale using calculated #s on one in TEN_COLS on one in TEN_ROWS.
		// The current and next row are interpolated to floating point arrays (x, y, x-scale)
		// accuracy and the data is translated.  Then, all pixels in between are interpolated
		// and the data is translated.  Use two *pArrays to hold the floating point data.

		// RGBRGBRGB...
		unsafe static private void Rescale24bpp( BitmapData sourceData, BitmapData destData, 
			RescalePoint[,] rescalePoints, Point sourceOffset, int TEN_ROWS, int TEN_COLS )
		{
			RescalePoint	point00 ; // PointRC
			RescalePoint	point01 ;
			RescalePoint	point10 ;
			int				lastRow ;
			int				lastColumn ;
			float			fFactor ;
			byte			*pS ;
			byte			*pD ;
			int				iIndexToNextRow = sourceData.Stride ; 			
			int				rowPoints = rescalePoints.GetLength(0) ;
			int				colPoints = rescalePoints.GetLength(1) ;

			byte*			pSource = (byte*)sourceData.Scan0.ToPointer(); 
			byte*			pDest = (byte*)destData.Scan0.ToPointer(); 

			float			scale ;
			float			fR, fG, fB ;

			float			fToNextCol ; 
			float			fCol ;

			//sharpening: Pout = (Pint - histogram.FifthMinRange) * (histogram.Size / (histogram.FifthMaxRange - histogram.FifthMinRange))
			Rectangle		histClip = new Rectangle( (int) (( sourceData.Width / 2) - (sourceData.Width * 0.05F)),
				0, (int) (sourceData.Width * 0.1F), sourceData.Height) ;
			Histogram		histogram = new Histogram(sourceData, null, histClip) ;
			//Histogram		histogram2 = new Histogram(sourceData, null) ;
			short			histMinValue = histogram.FifthMinRange ;
			double			histRatio = ((double) histogram.Size / (histogram.FifthMaxRange - histogram.FifthMinRange)) ;
			//double			histRatio = ((double) histogram.Size / (histogram.FifthMaxRange - histogram.FifthMinRange)) * 0.3F ;
			
			//histogram.Show() ;
          
			//DateTime	start = DateTime.Now ;

			// Loop on points in destination pixel coordinates (integers)
			for ( int iRowPoint = 0 ; iRowPoint < rowPoints - 1 ; iRowPoint++ )
			{
				for ( int iColPoint = 0 ; iColPoint < colPoints - 1 ; iColPoint++ )
				{
					// Get Rescale Points for this block of pixels     
					point00 = rescalePoints[iRowPoint, iColPoint] ;
					point01 = rescalePoints[iRowPoint, iColPoint + 1] ;
					point10 = rescalePoints[iRowPoint + 1, iColPoint] ;
      
					fToNextCol = TEN_ROWS / (point01.X - point00.X) ; 
					lastRow = Math.Min( (iRowPoint + 1) * TEN_ROWS, destData.Height ) ; 
      
					for ( int row = iRowPoint * TEN_ROWS ; row < lastRow ; row++ ) 
					{
						iIndexToNextRow = (row < (destData.Height - 1)) ? sourceData.Stride : 0;


						// Get current R, C, S and row, col deltas         
						pD = pDest + (row * destData.Stride ) + Convert.ToInt32(3 * Math.Round(point00.X) ) ; 

						fCol = iColPoint * TEN_COLS ;

						// Get LastCol  for this block of pixels (an integer)
						lastColumn = (point01.X < destData.Width) ? (int) Math.Round(point01.X) : destData.Width ; 

						for ( int column = (int) Math.Round(point00.X) ; column < lastColumn ; column++ )
						{
							fFactor = ( float ) ( column % TEN_ROWS) / TEN_ROWS ;
							scale	= point00.Scale + ( point01.Scale - point00.Scale ) * fFactor ; 

							// Compose dst Pixel from src pixels
							pS = pSource + ( (sourceOffset.Y + row) * sourceData.Stride ) + (3 * ((int) Math.Round(fCol + sourceOffset.X)) ) ;

							fR = ( float ) *pS ;
							fG = ( float ) *(pS+1) ;
							fB = ( float ) *(pS+2) ;
							pS = pS + 3 ;

							if ( scale > 1 )
							{
								float		scaleTmp = scale - 1;

								while ( scaleTmp >= 1 )	
								{
									fR += *pS	  ;
									fG += *(pS+1) ;
									fB += *(pS+2) ;
									pS = pS + 3 ;
									scaleTmp -= 1 ;									
								}
								if ( scaleTmp > 0.01 )
								{
									fR += (float) *pS	  * scaleTmp ;
									fG += (float) *(pS+1) * scaleTmp ;
									fB += (float) *(pS+2) * scaleTmp ;
									pS = pS + 3 ;
								}
               
								fR /= scale ;
								fG /= scale ;
								fB /= scale ;
							}

							// Write Destination Pixel
							if(scale >= 1)
							{
								*pD++ = ( byte ) fR ; // R 
								*pD++ = ( byte ) fG ; // G 
								*pD++ = ( byte ) fB ; // B 
							}
							//sharpening
							else
							{
								//gray = 0.299Red + 0.587Gray + 0.114Blue
								//pixels are stored in order: blue, green, red
								if( (fB * 0.299F + fG * 0.587F + fR * 0.114F) < histogram.DarkerThreshold )
									{
									fR = fR + (float) (((double) (fR - histMinValue) * histRatio) - fR) * (2 * Math.Abs(scale - 1)) ;								
									*pD++ = ( byte ) (Math.Max(0, Math.Min(255, fR))) ; 

									fG = fG + (float) (((double) (fG - histMinValue) * histRatio) - fG) * (2 * Math.Abs(scale - 1)) ;								
									*pD++ = ( byte ) (Math.Max(0, Math.Min(255, fG))) ; 
							
									fB = fB + (float) (((double) (fB - histMinValue) * histRatio) - fB) * (2 * Math.Abs(scale - 1)) ;								
									*pD++ = ( byte ) (Math.Max(0, Math.Min(255, fB))) ; 
								}
								else
								{
									*pD++ = ( byte ) fR ; // R 
									*pD++ = ( byte ) fG ; // G 
									*pD++ = ( byte ) fB ; // B 
								}
							}

							// Go to next pixel
							fCol += fToNextCol ;
						}
					}
				}
			}
		}
		#endregion

		#region Rescale8bpp()
		// Rescale using calculated #s on one in TEN_COLS on one in TEN_ROWS.
		// The current and next row are interpolated to floating point arrays (x, y, x-scale)
		// accuracy and the data is translated.  Then, all pixels in between are interpolated
		// and the data is translated.  Use two *pArrays to hold the floating point data.

		// RGBRGBRGB...
		unsafe static private void Rescale8bpp(Color[] palette, BitmapData sourceData, BitmapData destData, 
			RescalePoint[,] rescalePoints, Point sourceOffset, int TEN_ROWS, int TEN_COLS )
		{
			RescalePoint	point00 ; // PointRC
			RescalePoint	point01 ;
			RescalePoint	point10 ;
			int				lastRow ;
			int				lastColumn ;
			float			fFactor ;
			byte			*pS ;
			byte			*pD ;
			int				iIndexToNextRow = sourceData.Stride ; 			
			int				rowPoints = rescalePoints.GetLength(0) ;
			int				colPoints = rescalePoints.GetLength(1) ;

			byte*			pSource = (byte*)sourceData.Scan0.ToPointer(); 
			byte*			pDest = (byte*)destData.Scan0.ToPointer(); 

			float			scale ;
			byte			colorIndex ;
			byte			color ;

			float			fToNextCol ; 
			float			fCol ;

			//sharpening: Pout = (Pint - histogram.FifthMinRange) * (histogram.Size / (histogram.FifthMaxRange - histogram.FifthMinRange))
			Histogram		histogram = new Histogram(sourceData, palette) ;
			short			histMinValue = histogram.FifthMinRange ;
			double			histRatio = ((double) histogram.Size / (histogram.FifthMaxRange - histogram.FifthMinRange)) ;
			//double			histRatio = ((double) histogram.Size / (225)) ;
			byte[]			indexesArray = new byte[256] ;

			//histogram.Show() ;

			for(int i = 0; i < 256; i++)
				indexesArray[palette[i].R] = (byte) i ;
          
			//DateTime	start = DateTime.Now ;
			
			// Loop on points in destination pixel coordinates (integers)
			for ( int iRowPoint = 0 ; iRowPoint < rowPoints - 1 ; iRowPoint++ )
			{
				for ( int iColPoint = 0 ; iColPoint < colPoints - 1 ; iColPoint++ )
				{
					// Get Rescale Points for this block of pixels     
					point00 = rescalePoints[iRowPoint, iColPoint] ;
					point01 = rescalePoints[iRowPoint, iColPoint + 1] ;
					point10 = rescalePoints[iRowPoint + 1, iColPoint] ;
      
					//fToNextCol = TEN_ROWS / (float) ( ((point01.X < destData.Width) ? Math.Round(point01.X) : destData.Width) - (Math.Round(point00.X)) ) ; 
					fToNextCol = TEN_ROWS / (point01.X - point00.X) ; 
					lastRow = Math.Min( (iRowPoint + 1) * TEN_ROWS, destData.Height ) ; 
      
					for ( int row = iRowPoint * TEN_ROWS ; row < lastRow ; row++ ) 
					{
						iIndexToNextRow = (row < (destData.Height - 1)) ? sourceData.Stride : 0;

						//fFactor = ( float ) ( row % TEN_ROWS) / TEN_ROWS ;
						//scale	= point00.Scale + ( point01.Scale - point00.Scale ) * fFactor ; 

						// Get current R, C, S and row, col deltas         
						pD = pDest + (row * destData.Stride ) + Convert.ToInt32(Math.Round(point00.X) ) ; 

						fCol = iColPoint * TEN_COLS ;

						// Get LastCol  for this block of pixels (an integer)
						lastColumn = (point01.X < destData.Width) ? (int) Math.Round(point01.X) : destData.Width ; 

						for ( int column = (int) Math.Round(point00.X) ; column < lastColumn ; column++ )
						{							
							fFactor = ( float ) ( column % TEN_ROWS) / TEN_ROWS ;
							scale	= point00.Scale + ( point01.Scale - point00.Scale ) * fFactor ; 
							
							// Compose dst Pixel from src pixels
							pS = pSource + ( (sourceOffset.Y + row) * sourceData.Stride ) + ((int) Math.Round(fCol + sourceOffset.X) ) ;

							colorIndex = *pS ;
							pS ++ ;

							//sharpening
							if( scale < 1 )
							{
								color = (byte) (palette[colorIndex].R * 0.299F + palette[colorIndex].G * 0.587F + palette[colorIndex].B * 0.114F) ;

								if( color < histogram.Threshold )
								{
									color = (byte) ( color + ((((double) (color - histMinValue) * histRatio) - color) * (2 * Math.Abs(scale - 1))) ) ;								
									
									color = (byte) Math.Max(0D, Math.Min(255D, (color - histMinValue) * histRatio)) ;
									colorIndex = (indexesArray[color] != 0) ? indexesArray[color] : colorIndex ;
								}
							}

							// Write Destination Pixel
							*pD++ = ( byte ) colorIndex ; 

							// Go to next pixel
							fCol += fToNextCol ;
						}
					}
				}
			}
		}
		#endregion

		#region Rescale1bpp()
		// Rescale using calculated #s on one in TEN_COLS on one in TEN_ROWS.
		// The current and next row are interpolated to floating point arrays (x, y, x-scale)
		// accuracy and the data is translated.  Then, all pixels in between are interpolated
		// and the data is translated.  Use two *pArrays to hold the floating point data.

		// RGBRGBRGB...
		unsafe static private void Rescale1bpp( BitmapData sourceData, BitmapData destData, 
			RescalePoint[,] rescalePoints, Point sourceOffset, int TEN_ROWS, int TEN_COLS )
		{
			RescalePoint	point00 ; // PointRC
			RescalePoint	point01 ;
			RescalePoint	point10 ;
			int				lastRow ;
			int				lastColumn ;
			float			fFactor ;
			byte			*pS ;
			byte			*pD ;
			int				iIndexToNextRow = sourceData.Stride ; 			
			int				rowPoints = rescalePoints.GetLength(0) ;
			int				colPoints = rescalePoints.GetLength(1) ;

			byte*			pSource = (byte*)sourceData.Scan0.ToPointer(); 
			byte*			pDest = (byte*)destData.Scan0.ToPointer(); 

			float			scale ;
			float			color ;

			float			fToNextCol ; 
			float			fCol ;
          
			// Loop on points in destination pixel coordinates (integers)
			for ( int iRowPoint = 0 ; iRowPoint < rowPoints - 1 ; iRowPoint++ )
			{
				for ( int iColPoint = 0 ; iColPoint < colPoints - 1 ; iColPoint++ )
				{
					// Get Rescale Points for this block of pixels     
					point00 = rescalePoints[iRowPoint, iColPoint] ;
					point01 = rescalePoints[iRowPoint, iColPoint + 1] ;
					point10 = rescalePoints[iRowPoint + 1, iColPoint] ;
      
					//fToNextCol = TEN_ROWS / (float) ( ((point01.X < destData.Width) ? Math.Round(point01.X) : destData.Width) - (Math.Round(point00.X)) ) ; 
					fToNextCol = TEN_ROWS / (point01.X - point00.X) ; 
					lastRow = Math.Min( (iRowPoint + 1) * TEN_ROWS, destData.Height ) ; 
      
					for ( int row = iRowPoint * TEN_ROWS ; row < lastRow ; row++ ) 
					{
						iIndexToNextRow = (row < (destData.Height - 1)) ? sourceData.Stride : 0;

						//fFactor = ( float ) ( row % TEN_ROWS) / TEN_ROWS ;
						//scale	= point00.Scale + ( point01.Scale - point00.Scale ) * fFactor ; 

						fCol = iColPoint * TEN_COLS ;

						// Get LastCol  for this block of pixels (an integer)
						lastColumn = (point01.X < destData.Width) ? (int) Math.Round(point01.X) : destData.Width ; 

						for ( int column = (int) Math.Round(point00.X) ; column < lastColumn ; column++ )
						{
							fFactor = ( float ) ( column % TEN_ROWS) / TEN_ROWS ;
							scale	= point00.Scale + ( point01.Scale - point00.Scale ) * fFactor ; 

							// Compose dst Pixel from src pixels
							int		colSource = (int) Math.Round(fCol + sourceOffset.X) ;
							pS = pSource + ( (sourceOffset.Y + row) * sourceData.Stride ) + (colSource / 8) ;

							color = (*pS) & (0x80 >> (colSource % 8)) ;

							if ( scale > 1 )
							{
								float		scaleTmp = scale - 1;

								while ( scaleTmp >= 1 )	
								{
									colSource ++ ;
									pS = pSource + ( (sourceOffset.Y + row) * sourceData.Stride ) + (colSource / 8) ;
									color += (*pS) & (0x80 >> (colSource % 8)) ;
									scaleTmp -= 1 ;									
								}
								if ( scaleTmp > 0.01 )
								{
									colSource ++ ;
									pS = pSource + ( (sourceOffset.Y + row) * sourceData.Stride ) + (colSource / 8) ;
									color += (float) ((*pS) & (0x80 >> (colSource % 8))) * scaleTmp ;
								}
               
								color /= scale ;
							}

							if(color > 0)
							{
								// Write Destination Pixel
								pD = pDest + (row * destData.Stride ) + (column >> 3) ; 
								byte	mask = (byte) (0x80 >> (column & 0x7)) ;
 
								*pD |= mask;
							}

							// Go to next pixel
							fCol += fToNextCol ;
						}
					}
				}
			}
		}
		#endregion
		
		#region Rescale2Dimensions()
		// RGBRGBRGB...
		
		#endregion
						
	}
}
