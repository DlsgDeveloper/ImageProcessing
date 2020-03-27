using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	public class PageContentLocatorNew
	{		

		#region constructor
		private PageContentLocatorNew()
		{
		}
		#endregion

		//	PUBLIC METHODS

		#region Get()
		/// <summary>
		/// Finds the content of page and retirns it's rectangle
		/// </summary>
		/// <param name="bitmap">1 bit per pixel despeckled image</param>
		/// <param name="sweepLinesCount">Number of vertical lines to look for content </param>
		/// <param name="searchLimit">Offset, in percents, where to search for content</param>
		/// <param name="blockSize">Block size in inches - minimum size of background around the content</param>
		/// <param name="percentageWhite">Threshold to consider area as background or object</param>
		/// <param name="maxValidDistanceToMedian">When sweep points are found, remove points farrer from the median more than this parameter</param>
		/// <param name="flag">1 - left page, 2 - right page, 3 both</param>
		/// <returns></returns>
		public static Rectangle GetRawClip(Bitmap bitmap, Rectangle clip, byte sweepLinesCount, RectangleF searchLimit, 
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
							percentageWhite, maxValidDistanceToMedian, flag, out confidence);
						return result;
					}
					default :
						throw IpException.Create(ErrorCode.ErrorUnsupportedFormat) ;
				}
			}
			catch(Exception ex)
			{
				throw new Exception("PageContentLocatorNew, Get(): " + ex.Message ) ;
			}
			finally
			{
#if DEBUG
				Console.WriteLine(string.Format("PageContentLocatorNew: {0}, Clip: {1}" , DateTime.Now.Subtract(now).ToString(), result.ToString()));
#endif
			}
		}
		#endregion
						
		#region Get()
		public static Rectangle Get(Bitmap bitmap, Rectangle clip, byte sweepLinesCount, RectangleF searchLimit, 
			float blockSize, float percentageWhite, int maxValidPointDistanceToMedian, int flag, out byte confidence)
		{
			Rectangle	rawClip = GetRawClip(bitmap, clip, sweepLinesCount, searchLimit, 
				blockSize, percentageWhite, maxValidPointDistanceToMedian, flag, out confidence);

			if(confidence > 0)
				rawClip = clip;

			BitmapData	bmpData = null;
			
			try
			{			
				bmpData = bitmap.LockBits(rawClip, ImageLockMode.ReadWrite, bitmap.PixelFormat); 

				ObjectLocator.Objects	allObjects = ObjectLocator.FindObjects(bitmap, Rectangle.Empty, false, 1);
				ObjectLocator.Objects	objects = ObjectLocator.SiftList(allObjects, 6, 12, 40, 40, 0);
				ImageProcessing.ObjectLocator.SortList(ref objects);

				ObjectLocator.Words	words = ObjectLocator.FindWords(objects);

				Rectangle	contentClip = words.GetClip();

				if(contentClip.IsEmpty == false)
					return contentClip;
				else
					return rawClip;
			}
			finally
			{
				if(bitmap != null && bmpData != null)
					bitmap.UnlockBits(bmpData);
			}
		}
		#endregion
		
		//PRIVATE METHODS

		#region Get1bpp()
		private static Rectangle Get1bpp(Bitmap bitmap, Rectangle clip, byte sweepLinesCount, RectangleF searchLimit, 
			float blockSize, float percentageWhite, int maxValidDistanceToMedian, int flag, out byte confidence)
		{
			BitmapData	bmpData = null;
			float		confL = 0, confR = 0;
			
			try
			{
				if(clip.IsEmpty)
					clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			
				bmpData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat); 
				int			stride = bmpData.Stride; 
						 
				int	left = 0, right = 0;

					left = GetLeftBorder(bmpData, sweepLinesCount, Convert.ToInt16(bmpData.Width * searchLimit.Left), 
						Convert.ToInt16(blockSize * bitmap.HorizontalResolution), percentageWhite, 
						maxValidDistanceToMedian, Convert.ToInt32(bitmap.HorizontalResolution), out confL);
				
					right = GetRightBorder(bmpData, sweepLinesCount, Convert.ToInt16(bmpData.Width * searchLimit.Right), 
						Convert.ToInt16(blockSize * bitmap.HorizontalResolution), percentageWhite, 
						maxValidDistanceToMedian, Convert.ToInt32(bitmap.HorizontalResolution), out confR);

				int	top = GetTopBorder( bmpData, sweepLinesCount, Convert.ToInt16(bmpData.Height * searchLimit.Top), 
					Convert.ToInt16(blockSize * bitmap.VerticalResolution), 
					percentageWhite, maxValidDistanceToMedian);
				
				int	bottom = GetBottomBorder( bmpData, sweepLinesCount,
					Convert.ToInt16(bmpData.Height * searchLimit.Bottom), Convert.ToInt16(blockSize * bitmap.VerticalResolution), 
					percentageWhite, maxValidDistanceToMedian);

				if(bmpData != null)
					bitmap.UnlockBits(bmpData);

				clip = Rectangle.FromLTRB(clip.X + left, clip.Y + top, clip.X + right, clip.Y + bottom);			
				confidence = (byte) ((confL + confR) / 2);
					
				confidence = Convert.ToByte((confidence > 0) ? ((confidence < 100) ? confidence : (byte) 100) : (byte) 0);
				return clip;
			}
			finally
			{
				if(bmpData != null)
					bitmap.UnlockBits(bmpData);
			}
		}
		#endregion
		
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
					sweepPoint = GetSweepPointRight(pSource, stride, sweepLine, 0, xLimit, blockLength, sweepLineDistance, percentageWhite);
					
					if(sweepPoint.IsEmpty == false)
						sweepPoints.Add(sweepPoint);
				}

				
				if(sweepPoints.Count > sweepLinesCount / 2)
				{
					confidence = confidence * sweepPoints.Count / (float) sweepLinesCount;

					BookfoldShared.RemoveWorstSweepPointsRight(ref sweepPoints, .6F);
					BookfoldShared.RemoveWorstSweepPointsLeft(ref sweepPoints, .2F);
					
					int	pointsRemoved = BookfoldShared.RemovePointsWhereDistanceBiggerThanX(ref sweepPoints, maxValidDistanceToMedian, bitmapRect);
					confidence = confidence - (pointsRemoved * 100) / (float) sweepPoints.Count;

					border = BookfoldShared.GetCenterX(sweepPoints, bitmapRect);
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

					BookfoldShared.RemoveWorstSweepPointsLeft(ref sweepPoints, .6F);
					BookfoldShared.RemoveWorstSweepPointsRight(ref sweepPoints, .2F);
					
					int	pointsRemoved = BookfoldShared.RemovePointsWhereDistanceBiggerThanX(ref sweepPoints, maxValidDistanceToMedian, bitmapRect);
					confidence = confidence - (pointsRemoved * 100) / (float) sweepPoints.Count;

					border = BookfoldShared.GetCenterX(sweepPoints, bitmapRect);
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
						blockLength, sweepLineDistance, percentageWhite);
					
					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
				}

				if(sweepPoints.Count > sweepLinesCount / 2)
				{
					BookfoldShared.RemoveWorstSweepPointsTop(ref sweepPoints, .5F);
					BookfoldShared.RemoveWorstSweepPointsBottom(ref sweepPoints, .2F);
					BookfoldShared.RemovePointsWhereDistanceBiggerThanY(ref sweepPoints, maxValidDistanceToMedian, bitmapRect);

					border = BookfoldShared.GetCenterY(sweepPoints, bitmapRect);
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
						blockLength, sweepLineDistance, percentageWhite);
					
					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
				}

				if(sweepPoints.Count > sweepLinesCount / 2)
				{
					BookfoldShared.RemoveWorstSweepPointsBottom(ref sweepPoints, .5F);
					BookfoldShared.RemoveWorstSweepPointsTop(ref sweepPoints, .2F);
					BookfoldShared.RemovePointsWhereDistanceBiggerThanY(ref sweepPoints, maxValidDistanceToMedian, bitmapRect);

					border = BookfoldShared.GetCenterY(sweepPoints, bitmapRect);
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
			double	bestAreaValue = BookfoldShared.PercentageWhite(pOrig, stride, xFrom, y, blockWidth, blockHeight);
			
			for(x = xFrom + jump; x < (xTo - blockWidth); x += jump)
			{
				areaValue = BookfoldShared.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight);
				
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
			double	bestAreaValue = BookfoldShared.PercentageWhite(pOrig, stride, xTo - blockWidth, y, blockWidth, blockHeight);
			
			for(x = xTo - blockWidth - jump; x > xFrom; x -= jump)
			{
				areaValue = BookfoldShared.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight);
				
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
			double	bestAreaValue = BookfoldShared.PercentageWhite(pOrig, stride, x, yFrom, blockWidth, blockHeight);

			for(y = yFrom + jump; y < yTo - blockHeight; y += jump)
			{
				areaValue = BookfoldShared.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight);
				
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
			double	bestAreaValue = BookfoldShared.PercentageWhite(pOrig, stride, x, yTo - blockHeight, blockWidth, blockHeight);
			
			for(y = yTo - blockHeight - jump; y > yFrom; y -= jump)
			{
				areaValue = BookfoldShared.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight);
				
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
				
	}
}
