using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	public class BookPagesLocator
	{		

		#region constructor
		private BookPagesLocator()
		{
		}
		#endregion

		//	PUBLIC METHODS

		#region GetFromObjects()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="siftedObjects">objects without speckle</param>
		/// <param name="clip"></param>
		/// <param name="offset">offset in pixels from clip if only 1 clip was found</param>
		/// <returns></returns>
		/*public static byte GetFromObjects(PageObjects.Symbols symbols, Rectangle clip, int offset, 
			Size prefSize, out Rectangle clipL, out Rectangle clipR)
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif					

			PageObjects.Words words = PageObjects.ObjectLocator.FindWords(symbols);
			PageObjects.Paragraphs paragraphs = PageObjects.ObjectLocator.FindParagraphs(symbols, words);
			float						confidence = 0;
			PageObjects.Pages pages = PageObjects.ObjectLocator.FindPages(symbols, paragraphs, clip, 2, ref confidence);

#if SAVE_RESULTS
			symbols.DrawToFile(Debug.SaveToDir + @"06 Symbols with no lines.png", new Size(clip.Right, clip.Bottom)); 
			words.DrawToFile(Debug.SaveToDir + @"07 Words.png", new Size(clip.Right, clip.Bottom)); 
			pages.DrawToFile(Debug.SaveToDir + @"08 Pages.png",new Size(clip.Right, clip.Bottom)); 
#endif

			if(pages.Count >= 2)
			{
				if(pages[0].X < pages[1].X)
				{
					clipL = pages[0].Rectangle;
					clipR = pages[1].Rectangle;
				}
				else
				{
					clipL = pages[1].Rectangle;
					clipR = pages[0].Rectangle;
				}
					
				GetPagesConfidence(pages[0].Rectangle, pages[1].Rectangle, prefSize, clip, ref confidence);
				AdjustClips(ref clipL, ref clipR, offset, clip, prefSize, ref confidence);
			}
			else if(pages.Count == 1)
			{
				int	center = clip.X + (clip.Right - clip.Left) / 2;

				if(Math.Abs(pages[0].X - center) > Math.Abs(pages[0].Right - center))
				{
					clipL = pages[0].Rectangle;
					clipR = new Rectangle(clip.Right - (clipL.X - clip.X) - clipL.Width, clipL.Top, clipL.Width, clipL.Height);
				}
				else
				{
					clipR = pages[0].Rectangle;
					clipL = new Rectangle(clip.Right - (clipR.X - clip.X) - clipR.Width, clipR.Top, clipR.Width, clipR.Height);
				}

				AdjustClips(ref clipL, ref clipR, offset, clip, prefSize, ref confidence);
				confidence = 0;
			}
			else
			{
				if(prefSize.IsEmpty == false)
				{
					clipL = new Rectangle(clip.X, clip.Y + (clip.Height - prefSize.Height) / 2, prefSize.Width, prefSize.Height);
					clipR = new Rectangle(clip.Right - clip.Width, clip.Y + (clip.Height - prefSize.Height) / 2, prefSize.Width, prefSize.Height);
				}
				else
				{
					clipL = new Rectangle(clip.X, clip.Y, clip.Width / 2, clip.Height);
					clipR = new Rectangle(clip.X + clip.Width / 2, clip.Y, clip.Width / 2, clip.Height);
				}

				confidence = 0;
			}

			//ValidatePage(ref clipL, clip, ref confidence);
			//ValidatePage(ref clipR, clip, ref confidence);

			if(confidence < 0)
				confidence = 0;

#if DEBUG
			Console.WriteLine(string.Format("BookPagesLocator, GetFromObjects(): {0}, Confidence:{1}%, L:{2}, R:{3}",  
				DateTime.Now.Subtract(start).ToString(), confidence * 100, clipL.ToString(), clipR.ToString())) ;
#endif
			
			return Convert.ToByte(confidence * 100);
		}*/
		#endregion

		
		//PRIVATE METHODS

		#region GetRawClip()
		/// <summary>
		/// Finds the content of raster clip and returns it's rectangle
		/// </summary>
		/// <param name="bitmap">1 bit per pixel despeckled image</param>
		/// <param name="sweepLinesCount">Number of vertical lines to look for content </param>
		/// <param name="searchLimit">Offset, in percents, where to search for content</param>
		/// <param name="blockSize">Block size in inches - minimum size of background around the content</param>
		/// <param name="percentageWhite">Threshold to consider area as background or object</param>
		/// <param name="maxValidDistanceToMedian">When sweep points are found, remove points farrer from the median more than this parameter</param>
		/// <param name="flag">4 - recursive search</param>
		/// <returns></returns>
		/*public static Rectangle GetRawClip(Bitmap bitmap, Rectangle clip, byte sweepLinesCount, RectangleF searchLimit, 
			float blockSize, float percentageWhite, int maxValidDistanceToMedian, int flag, out byte confidence)
		{
#if DEBUG
			DateTime	now = DateTime.Now ;
#endif
			Rectangle	result = Rectangle.Empty;

			try
			{
				switch(bitmap.PixelFormat)
				{
					case PixelFormat.Format1bppIndexed :
					{
						result = Get1bpp(bitmap, clip, sweepLinesCount, searchLimit, blockSize, 
							percentageWhite, maxValidDistanceToMedian, flag, 3, out confidence);

						result.Inflate((int) (- bitmap.HorizontalResolution * .10), (int) (- bitmap.VerticalResolution * .10));
						return result;
					}
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}
			}
			catch(Exception ex)
			{
				throw new Exception("BookPagesLocator, Get(): " + ex.Message ) ;
			}
			finally
			{
#if DEBUG
				Console.WriteLine(string.Format("BookPagesLocator: {0}, Clip: {1}" , DateTime.Now.Subtract(now).ToString(), result.ToString()));
#endif
			}
		}*/
		#endregion
		
		#region Get1bpp()
		/*private static Rectangle Get1bpp(Bitmap bitmap, Rectangle clip, byte sweepLinesCount, RectangleF searchLimit, 
			float blockSize, float percentageWhite, int maxValidDistanceToMedian, int flag, int iterations, out byte confidence)
		{
			BitmapData	bmpData = null;
			float		confL = 0, confR = 0;
			
			try
			{
				if(clip.IsEmpty)
					clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			
				bmpData = bitmap.LockBits(clip, ImageLockMode.ReadOnly, bitmap.PixelFormat); 
				int			stride = bmpData.Stride; 
						 
				int	left = GetLeftBorder(bmpData, sweepLinesCount, Convert.ToInt16(bmpData.Width * searchLimit.Left), 
						Convert.ToInt16(blockSize * bitmap.HorizontalResolution), percentageWhite, 
						maxValidDistanceToMedian, Convert.ToInt32(bitmap.HorizontalResolution), out confL);
				
				int	right = GetRightBorder(bmpData, sweepLinesCount, Convert.ToInt16(bmpData.Width * searchLimit.Right), 
						Convert.ToInt16(blockSize * bitmap.HorizontalResolution), percentageWhite, 
						maxValidDistanceToMedian, Convert.ToInt32(bitmap.HorizontalResolution), out confR);

				int	top = GetTopBorder( bmpData, sweepLinesCount, Convert.ToInt16(bmpData.Height * searchLimit.Top), 
					Convert.ToInt16(blockSize * bitmap.VerticalResolution), 
					percentageWhite, maxValidDistanceToMedian);
				
				int	bottom = GetBottomBorder( bmpData, sweepLinesCount,
					Convert.ToInt16(bmpData.Height * searchLimit.Bottom), Convert.ToInt16(blockSize * bitmap.VerticalResolution), 
					percentageWhite, maxValidDistanceToMedian);

				Rectangle newClip = Rectangle.FromLTRB(clip.X + left, clip.Y + top, clip.X + right, clip.Y + bottom);			
				confidence = (byte) ((confL + confR) / 2);
					
				confidence = Convert.ToByte((confidence > 0) ? ((confidence < 100) ? confidence : (byte) 100) : (byte) 0);

				if((flag & 4) > 0 && newClip != clip && iterations > 0)
				{
					if(bmpData != null)
						bitmap.UnlockBits(bmpData);
					bmpData = null;

					newClip = Get1bpp(bitmap, newClip, sweepLinesCount, searchLimit, blockSize, percentageWhite, maxValidDistanceToMedian, flag, --iterations, out confidence);
				}

				return newClip;
			}
			finally
			{
				if(bmpData != null)
					bitmap.UnlockBits(bmpData);
			}
		}*/
		#endregion
	/*	
		#region GetLeftBorder()
		private static int GetLeftBorder(BitmapData bmpData, byte sweepLinesCount, int xLimit, 
			int blockLength, float percentageWhite, int maxValidDistanceToMedian, int resolution, out float confidence)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bmpData.Width, bmpData.Height);
			int			sweepLineDistance = Convert.ToInt16(bmpData.Height / (float) sweepLinesCount);
			int[]		sweepLines = new int[sweepLinesCount];
			int			border = 0;		
			int			stride = bmpData.Stride; 

			confidence = 100.0F;
			
			for(int i = 0; i < sweepLinesCount; i++)
				sweepLines[i] = Convert.ToInt16((bmpData.Height / (float) sweepLinesCount) * i);
					 
			unsafe
			{
				byte*		pSource = (byte*)bmpData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();
				Point		sweepPoint;

				foreach(int sweepLine in sweepLines)
				{
					sweepPoint = GetSweepPointLeft(pSource, stride, sweepLine, 0, xLimit, blockLength, sweepLineDistance, percentageWhite);
					
					if(sweepPoint.IsEmpty == false)
						sweepPoints.Add(sweepPoint);
				}

				
				if(sweepPoints.Count > sweepLinesCount / 2)
				{
					confidence = confidence * sweepPoints.Count / (float) sweepLinesCount;

					RasterProcessing.RemoveWorstSweepPointsLeft(ref sweepPoints, .6F);
					RasterProcessing.RemoveWorstSweepPointsRight(ref sweepPoints, .2F);
					
					int	pointsRemoved = RasterProcessing.RemovePointsWhereDistanceBiggerThanX(ref sweepPoints, maxValidDistanceToMedian, bitmapRect);
					confidence = confidence - (pointsRemoved * 100) / (float) sweepPoints.Count;

					border = RasterProcessing.GetCenterX(sweepPoints, bitmapRect);
					border = (int) ((border > 0) ? ((border < bmpData.Width) ? border : bmpData.Width) : 0);
				}
				else
					confidence = 0;
			}
			
			return border;
		}
		#endregion

		#region GetRightBorder()
		private static int GetRightBorder(BitmapData bmpData, byte sweepLinesCount, int xLimit, int blockLength,
			float percentageWhite, int maxValidDistanceToMedian, int resolution, out float confidence)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bmpData.Width, bmpData.Height);
			int			sweepLineDistance = Convert.ToInt16(bmpData.Height / (float) sweepLinesCount);
			int[]		sweepLines = new int[sweepLinesCount];
			int			border = (int) bmpData.Width;		
			int			stride = bmpData.Stride; 

			for(int i = 0; i < sweepLinesCount; i++)
				sweepLines[i] = Convert.ToInt16((bmpData.Height / (float) sweepLinesCount) * i);
						 
			confidence = 100.0F;

			unsafe
			{
				byte*		pSource = (byte*)bmpData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();
				Point		sweepPoint;

				foreach(int sweepLine in sweepLines)
				{					
					sweepPoint = GetSweepPointRight(pSource, stride, sweepLine, xLimit, bmpData.Width, blockLength, sweepLineDistance, percentageWhite);
					
					if(sweepPoint.IsEmpty == false)
						sweepPoints.Add(sweepPoint);
				}

				if(sweepPoints.Count > sweepLinesCount / 2)
				{
					confidence = confidence * sweepPoints.Count / (float) sweepLinesCount;

					RasterProcessing.RemoveWorstSweepPointsRight(ref sweepPoints, .6F);
					RasterProcessing.RemoveWorstSweepPointsLeft(ref sweepPoints, .2F);
					
					int	pointsRemoved = RasterProcessing.RemovePointsWhereDistanceBiggerThanX(ref sweepPoints, maxValidDistanceToMedian, bitmapRect);
					confidence = confidence - (pointsRemoved * 100) / (float) sweepPoints.Count;

					border = RasterProcessing.GetCenterX(sweepPoints, bitmapRect);
					border = (int) ((border > 0) ? ((border < bmpData.Width) ? border : bmpData.Width) : 0);
				}
				else
					confidence = 0;
			}
			
			return border;
		}
		#endregion

		#region GetTopBorder()
		private static int GetTopBorder(BitmapData bmpData, byte sweepLinesCount, int yLimit, int blockLength, 
			float percentageWhite, int maxValidDistanceToMedian)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bmpData.Width, bmpData.Height);
			int		sweepLineDistance = Convert.ToInt16(bmpData.Width / (float) sweepLinesCount);
			int[]		sweepLines = new int[sweepLinesCount];
			int		border = 0;		
			int			stride = bmpData.Stride; 
			Point		sweepPoint;

			for(int i = 0; i < sweepLinesCount; i++)
				sweepLines[i] = Convert.ToInt16((bmpData.Width / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bmpData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(int sweepLine in sweepLines)
				{
					sweepPoint = GetSweepPointTop(pSource, stride, sweepLine, 0, yLimit, 
						sweepLineDistance, blockLength, percentageWhite);
					
					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
				}

				if(sweepPoints.Count > sweepLinesCount / 2)
				{
					RasterProcessing.RemoveWorstSweepPointsTop(ref sweepPoints, .5F);
					RasterProcessing.RemoveWorstSweepPointsBottom(ref sweepPoints, .2F);
					RasterProcessing.RemovePointsWhereDistanceBiggerThanY(ref sweepPoints, maxValidDistanceToMedian, bitmapRect);

					border = RasterProcessing.GetCenterY(sweepPoints, bitmapRect);
					border = (int) ((border > 0) ? ((border < bmpData.Height) ? border : bmpData.Height) : 0);
				}
			}
			
			return border;
		}
		#endregion

		#region GetBottomBorder()
		private static int GetBottomBorder(BitmapData bmpData, byte sweepLinesCount, int yLimit, int blockLength, 
			float percentageWhite, int maxValidDistanceToMedian)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bmpData.Width, bmpData.Height);
			int		sweepLineDistance = Convert.ToInt16(bmpData.Width / (float) sweepLinesCount);
			int[]		sweepLines = new int[sweepLinesCount];
			int		border = (int) bmpData.Height;		
			int			stride = bmpData.Stride; 
			Point		sweepPoint;

			for(int i = 0; i < sweepLinesCount; i++)
				sweepLines[i] = Convert.ToInt16((bmpData.Width / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bmpData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(int sweepLine in sweepLines)
				{
					sweepPoint = GetSweepPointBottom(pSource, stride, sweepLine, yLimit, (int) bmpData.Height, 
						sweepLineDistance, blockLength, percentageWhite);
					
					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
				}

				if(sweepPoints.Count > sweepLinesCount / 2)
				{
					RasterProcessing.RemoveWorstSweepPointsBottom(ref sweepPoints, .5F);
					RasterProcessing.RemoveWorstSweepPointsTop(ref sweepPoints, .2F);
					RasterProcessing.RemovePointsWhereDistanceBiggerThanY(ref sweepPoints, maxValidDistanceToMedian, bitmapRect);

					border = RasterProcessing.GetCenterY(sweepPoints, bitmapRect);
					border = (int) ((border > 0) ? ((border < bmpData.Height) ? border : bmpData.Height) : 0);
				}
			}
			
			return border;
		}
		#endregion
		
		#region GetSweepPointLeft()
		private static unsafe Point GetSweepPointLeft(byte* pOrig, int stride, int y, int xFrom, int xTo, 
			int blockWidth, int blockHeight, float percentageWhite)
		{
			int		x;
			double	areaValue;
			int		jump = Convert.ToInt16(blockWidth / 3);
			int		bestX = xFrom;
			double	bestAreaValue = RasterProcessing.PercentageWhite(pOrig, stride, xFrom, y, blockWidth, blockHeight);
			
			for(x = xFrom; x < (xTo - blockWidth); x += jump)
			{
				areaValue = RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight);
				
				if(areaValue == 0)
					return new Point(x, y);

				if(areaValue < bestAreaValue * .9)
				{
					bestX = x;
					bestAreaValue = areaValue;
				}
			}

			if(bestAreaValue < percentageWhite)
				return new Point(bestX, y);
			else
				return Point.Empty;
		}
		#endregion
				
		#region GetSweepPointRight()
		private static unsafe Point GetSweepPointRight(byte* pOrig, int stride, int y, int xFrom, int xTo,
			int blockWidth, int blockHeight, float percentageWhite)
		{
			int		x;
			double	areaValue;
			int		jump = Convert.ToInt16(blockWidth / 3);
			int		bestX = xTo;
			double	bestAreaValue = RasterProcessing.PercentageWhite(pOrig, stride, xTo - blockWidth, y, blockWidth, blockHeight);
			
			for(x = xTo - blockWidth; x > xFrom; x -= jump)
			{
				areaValue = RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight);
				
				if(areaValue == 0)
					return new Point(x + blockWidth, y);

				if(areaValue < bestAreaValue * .9)
				{
					bestX = x + blockWidth;
					bestAreaValue = areaValue;
				}
			}

			if(bestAreaValue < percentageWhite)
				return new Point(bestX, y);
			else
				return Point.Empty;
		}
		#endregion
				
		#region GetSweepPointTop()
		private static unsafe Point GetSweepPointTop(byte* pOrig, int stride, int x, int yFrom, int yTo,
			int blockWidth, int blockHeight, float percentageWhite)
		{
			int		y;
			int		jump = Convert.ToInt16(blockHeight / 3);
			double	areaValue;
			int		bestY = yFrom;
			double	bestAreaValue = RasterProcessing.PercentageWhite(pOrig, stride, x, yFrom, blockWidth, blockHeight);

			for(y = yFrom; y < yTo - blockHeight; y += jump)
			{
				areaValue = RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight);
				
				if(areaValue == 0)
					return new Point(x, y);
				
				if(areaValue < bestAreaValue * .9)
				{
					bestY = y;
					bestAreaValue = areaValue;
				}
			}

			if(bestAreaValue < percentageWhite)
				return new Point(x, bestY);
			else
				return Point.Empty;
		}
		#endregion
				
		#region GetSweepPointBottom()
		private static unsafe Point GetSweepPointBottom(byte* pOrig, int stride, int x, int yFrom, int yTo,
			int blockWidth, int blockHeight, float percentageWhite)
		{
			int		y;
			int		jump = Convert.ToInt16(blockHeight / 3);
			double	areaValue;
			int		bestY = yTo;
			double	bestAreaValue = RasterProcessing.PercentageWhite(pOrig, stride, x, yTo - blockHeight, blockWidth, blockHeight);
			
			for(y = yTo - blockHeight; y > yFrom; y -= jump)
			{
				areaValue = RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight);
				
				if(areaValue == 0)
					return new Point(x, y + blockHeight);

				if(areaValue < bestAreaValue * .9)
				{
					bestY = y + blockHeight;
					bestAreaValue = areaValue;
				}
			}

			if(bestAreaValue < percentageWhite)
				return new Point(x, bestY);
			else
				return Point.Empty;
		}
		#endregion
*/				
/*		#region GetPagesConfidence()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="c1"></param>
		/// <param name="c2"></param>
		/// <param name="sameClipSize"></param>
		/// <param name="prefSize"></param>
		/// <param name="imageSize"></param>
		/// <returns></returns>
		private static float GetPagesConfidence(Rectangle c1, Rectangle c2, Size prefSize, 
			Rectangle imageClip, ref float confidence)
		{
			//if difference between clips top is bigger than 5% of image height, lower confidence
			if(Difference(c1.Top, c2.Top) > imageClip.Height * 0.1F)
				confidence -= Difference(c1.Top, c2.Top) / (float) (imageClip.Height / 2);
			
			//if difference between clips width is bigger than 10%, lower confidence
			if(Min(c1.Width, c2.Width) / Max(c1.Width, c2.Width) < 0.9F)
				confidence -= Min(c1.Width, c2.Width) * 2 / Max(c1.Width, c2.Width);

			//if difference between clips height is bigger than 10%, lower confidence
			if(Min(c1.Height, c2.Height) / Max(c1.Height, c2.Height) < 0.9F)
				confidence -= Min(c1.Height, c2.Height) * 2 / Max(c1.Height, c2.Height);

			if(prefSize.IsEmpty == false)
			{
				if(Min(c1.Width, prefSize.Width) / Max(c1.Width, prefSize.Width) < 0.9F)
					confidence -= Min(c1.Width, prefSize.Width) * 2 / Max(c1.Width, prefSize.Width);

				if(Min(c2.Width, prefSize.Width) / Max(c2.Width, prefSize.Width) < 0.9F)
					confidence -= Min(c2.Width, prefSize.Width) * 2 / Max(c2.Width, prefSize.Width);
				
				if(Min(c1.Height, prefSize.Height) / Max(c1.Height, prefSize.Height) < 0.9F)
					confidence -= Min(c1.Height, prefSize.Height) * 2 / Max(c1.Height, prefSize.Height);
				
				if(Min(c2.Height, prefSize.Height) / Max(c2.Height, prefSize.Height) < 0.9F)
					confidence -= Min(c2.Height, prefSize.Height) * 2 / Max(c2.Height, prefSize.Height);
			}

			//if difference between clips height is bigger than 10%, lower confidence
			if(c1.Width > imageClip.Width / 2 || c2.Width > imageClip.Width / 2 || 
				c1.Height > imageClip.Height || c2.Height > imageClip.Height)
				confidence = 0;

			return (confidence < 0) ? 0 : confidence;
		}
		#endregion

		#region Min()
		private static float Min(int x1, int x2)
		{
			return (x1 < x2) ? x1 : x2;
		}
		#endregion

		#region Max()
		private static float Max(int x1, int x2)
		{
			return (x1 > x2) ? x1 : x2;
		}
		#endregion

		#region Difference()
		private static float Difference(int x1, int x2)
		{
			return (x1 > x2) ? (x1 - x2) : (x2 - x1) ;
		}
		#endregion

		#region AdjustClipSize()
		private static void AdjustClipSize(ref Rectangle c1, Size size, ref float confidence)
		{
			if(c1.Width <= size.Width)
			{
				c1.X -= (int) Difference(c1.Width, size.Width) / 2;
				c1.Width = size.Width;
			}
			else
			{
				confidence = 0;
			}

			if(c1.Height < size.Height)
			{
				c1.Y -= (int) Difference(c1.Height, size.Height) / 2;
				c1.Height = size.Height;
			}
			else
			{
				confidence = 0;
			}
		}
		#endregion

		#region AdjustClips()
		private static void AdjustClips(ref Rectangle c1, ref Rectangle c2, int offset, Rectangle clip, Size prefSize, ref float conf)
		{
			MakeClipsSameSize(ref c1, ref c2);

			if(prefSize.IsEmpty == true)
			{
				c1.Inflate(offset, offset);
				c2.Inflate(offset, offset);
			}
			else
			{
				int		offsetX = (prefSize.Width - c1.Width) / 2;
				int		offsetY = (prefSize.Height - c1.Height) / 2;

				if(offsetX < 0 || offsetY < 0)
					conf = 0;
				else
				{
					c1.Inflate(offsetX, offsetY);
					c2.Inflate(offsetX, offsetY);
				}
			}
		
			if(Rectangle.Intersect(c1, clip) != c1)
			{
				c1.X = Math.Max(c1.X, clip.X);
				c1.Y = Math.Max(c1.Y, clip.Y);

				if(c1.Right > clip.Right)
					c1.X = clip.Right - c1.Width;
				if(c1.Bottom > clip.Bottom)
					c1.Y = clip.Bottom - c1.Height;
			}

			if(Rectangle.Intersect(c2, clip) != c2)
			{
				c2.X = Math.Max(c2.X, clip.X);
				c2.Y = Math.Max(c2.Y, clip.Y);

				if(c2.Right > clip.Right)
					c2.X = clip.Right - c2.Width;
				if(c2.Bottom > clip.Bottom)
					c2.Y = clip.Bottom - c2.Height;
			}

			if(Rectangle.Intersect(c1, clip) != c1)
				conf = 0;
			if(Rectangle.Intersect(c2, clip) != c2)
				conf = 0;
		}
		#endregion

		#region MakeClipsSameSize()
		private static void MakeClipsSameSize(ref Rectangle c1, ref Rectangle c2)
		{
			if(c1.Width < c2.Width)
			{
				c1.X -= (int) Difference(c1.Width, c2.Width) / 2;
				c1.Width = c2.Width;
			}
			else
			{
				c2.X -= (int) Difference(c1.Width, c2.Width) / 2;
				c2.Width = c1.Width;
			}
			
			if(c1.Height < c2.Height)
			{
				if (c1.Y >= c2.Y && c1.Bottom <= c2.Bottom)
					c1.Y = c2.Y;
				else if (c1.Bottom > c2.Bottom)
					c1.Y = c1.Bottom - c2.Height;
				
				c1.Height = c2.Height;
			}
			else if(c1.Height > c2.Height)
			{
				if (c2.Y >= c1.Y && c2.Bottom <= c1.Bottom)
					c2.Y = c1.Y;
				else if (c2.Bottom > c1.Bottom)
					c2.Y = c2.Bottom - c1.Height;
				
				c2.Height = c1.Height;
			}
		}
		#endregion
		*/
		
	}
}
