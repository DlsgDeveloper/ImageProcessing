using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	/*public class BookCenterArea
	{		

		#region constructor
		private BookCenterArea()
		{
		}
		#endregion

		//	PUBLIC METHODS

		#region Get()
		public static void Get(Bitmap bitmap, byte sweepLinesCount, short offsetT, short offsetB, float offsetL, float offsetR, 
			float checkAreaWidth, short areaCoeff, out int left, out int right, out byte confidence)
		{
#if DEBUG
			DateTime	now = DateTime.Now ;
#endif

			try
			{
				byte		confidenceL = 0, confidenceR = 0;
				
				confidence = 0;
				left = (int) (bitmap.Width * offsetL);
				right = (int) (bitmap.Width * offsetR);
			
				switch(bitmap.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed :
					{
						if(ImageInfo.IsPaletteGrayscale(bitmap.Palette.Entries))
						{
							left = Get8bppGray(bitmap, sweepLinesCount, 
								Rectangle.FromLTRB(left, offsetT, right, bitmap.Height - offsetB),
								(short) (bitmap.VerticalResolution * checkAreaWidth), areaCoeff, true, out confidenceL);

							right = Get8bppGray(bitmap, sweepLinesCount, 
								Rectangle.FromLTRB(left, offsetT, right, bitmap.Height - offsetB),
								(short) (bitmap.VerticalResolution * checkAreaWidth), areaCoeff, false, out confidenceR);

							confidence = (byte) ((confidenceL + confidenceR) / 2);

							if(right - left < bitmap.VerticalResolution * .5F)
								confidence = (byte) Math.Max(0, confidence - 50);

							//if(right - left >= bitmap.VerticalResolution * .5F && confidence > 50)
							//{
							//	byte	darkCenterPoints = CheckIfDarkInBetween(bitmap, sweepLinesCount, 
							//		Rectangle.FromLTRB(left + 80, offsetT, right - 80, bitmap.Height - offsetB),
							//		(short) (bitmap.VerticalResolution * checkAreaWidth), areaCoeff);
							//		
							//	if(darkCenterPoints < 75)
							//		confidence = (byte) Math.Max(0, confidence - 50);
							//}
						}
						else
							throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
					} break;
						//case PixelFormat.Format24bppRgb :	
						//{
						//	return Get24bpp(bitmap, sweepLinesCount, offsetTop, offsetBottom, offsetLeft, 
						//		offsetRight, pixelBlockSize, distanceBetweenCheckPoints, maxSweepPointDistanceFromAxle, out confidence);
						//}
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}

#if DEBUG
				Console.WriteLine(string.Format("BookCenterArea: {0}, Confidence:{1}, Left: {2}, Right: {3}" , DateTime.Now.Subtract(now).ToString(), confidence, left, right));
#endif
			}
			catch(Exception ex)
			{
				throw new Exception("BookCenterArea, Get(): " + ex.Message ) ;
			}
		}
		#endregion
						
		//PRIVATE METHODS

		#region Get8bppGray()
		private static int Get8bppGray(Bitmap bitmap, byte sweepLinesCount, Rectangle clip, short checkWidth,
			short areaCoeff, bool leftToRight, out byte confidence)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			short		sweepLineDistance = Convert.ToInt16(clip.Height / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			int			result = (leftToRight) ? clip.Left : clip.Right;
			byte		threshold;
			float		smoothness;
			ArrayList	sweepPoints = new ArrayList();
			Point		sweepPoint;
			BitmapData	bitmapData = bitmap.LockBits(bitmapRect, ImageLockMode.ReadOnly, bitmap.PixelFormat); 
			int			stride = bitmapData.Stride; 
			Rectangle	localRect = new Rectangle(clip.X, 0, clip.Width, sweepLineDistance);
			int			iConfidence;
			short		badSweepPoint = 0;

			for(int i = 0; i < sweepLinesCount; i++)
				verticalPositions[i] = Convert.ToInt16(clip.Y + (clip.Height / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 

				foreach(short y in verticalPositions)
				{
					localRect.Y = y;
					threshold = ContentLocator.GetThresholdGray(bitmapData, localRect);
					threshold = (byte) ((threshold - 40 > 100) ? threshold - 40 : 100);
					smoothness = ContentLocator.GetSmoothenessGray(bitmapData, localRect) * 1.1F;

					if(leftToRight)
						sweepPoint = GetSweepPointsLeftGray(bitmapData, pSource, stride, threshold, smoothness, localRect, checkWidth);
					else
						sweepPoint = GetSweepPointsRightGray(bitmapData, pSource, stride, threshold, smoothness, localRect, checkWidth);

					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
					else
						badSweepPoint++;
				}

				if(badSweepPoint * 4 < sweepLinesCount)
				{
					byte		sweepPointsCount = (byte) sweepPoints.Count;
					RemoveWorstPoints(ref sweepPoints, .33F, leftToRight);

					result = GetCenter(sweepPoints, bitmapRect);
					double	distance = BookfoldLocatorAreas.GetAverageDistance(sweepPoints, bitmapRect);
					distance = (200 - distance) / 200;
					distance = (distance < 0) ? 0 : ((distance > 1) ? 1 : distance);

					iConfidence = (int) ((100 * sweepPointsCount / (float) sweepLinesCount) * distance);

					confidence = (byte) ((iConfidence > 0) ? ((iConfidence < 100) ? iConfidence : 100) : 0);
				}
				else
					confidence = 0;
			}
			
			bitmap.UnlockBits(bitmapData);
			return result;
		}
		#endregion
		
		#region GetSweepPointsLeftGray()
		private static unsafe Point GetSweepPointsLeftGray(BitmapData bitmapData, byte* pOrig, int stride, byte threshold, 
			float smoothness, Rectangle clip, short checkWidth)
		{
			Point			result = Point.Empty;
			int				x, i;
			byte			backgroundness = 15;
			Rectangle		zone = new Rectangle(clip.X, clip.Y, checkWidth, clip.Height);
			//byte			averageGray = ContentLocator.GetAverageGray(bitmapData, zone);
			int				jump = checkWidth;
			short			area;

			for(x = clip.X; x < clip.Right - checkWidth; x += jump)
			{
				zone.X = x;
				area = GetBackgroundWeight(bitmapData, zone, threshold, smoothness);
				
				jump = checkWidth * area / 100;
				if(jump < 5)
					jump = 5;

				if(area < backgroundness)
				{
					zone.Width = 5;

					for(i = x + checkWidth; i > x - checkWidth * 2; i = i - 5)
					{
						zone.X = i;
						area = GetBackgroundWeight(bitmapData, zone, threshold, smoothness);

						if(area > backgroundness)
							return new Point(i, clip.Y);
					}
					
					return new Point(x, clip.Y);
				}
			}

			return Point.Empty;
		}
		#endregion
			
		#region GetSweepPointsRightGray()
		private static unsafe Point GetSweepPointsRightGray(BitmapData bitmapData, byte* pOrig, int stride, byte threshold, 
			float smoothness, Rectangle clip, short checkWidth)
		{
			Point			result = Point.Empty;
			int				x, i;
			short			deltaCoefficient = (short) (smoothness);
			byte			backgroundness = 15;
			Rectangle		zone = new Rectangle(clip.X, clip.Y, checkWidth, clip.Height);
			//byte			averageGray = ContentLocator.GetAverageGray(bitmapData, zone);
			int				jump = checkWidth;
			short			area;

			for(x = clip.Right; x > clip.X - checkWidth; x -= jump)
			{
				zone.X = x;
				area = GetBackgroundWeight(bitmapData, zone, threshold, smoothness);
				
				jump = checkWidth * area / 100;
				if(jump < 5)
					jump = 5;
				
				if(area < backgroundness)
				{
					zone.Width = 5;

					for(i = x; i < x + checkWidth * 2; i = i + 5)
					{
						zone.X = i;
						area = GetBackgroundWeight(bitmapData, zone, threshold, smoothness);

						if(area > backgroundness)
							return new Point(i, clip.Y);
					}
					
					return new Point(x, clip.Y);
				}
			}

			return Point.Empty;
		}
		#endregion
		
		#region RemoveWorstPoints()
		private static void RemoveWorstPoints(ref ArrayList sweepPoints, float percentsToRemove, bool leftToRight)
		{
			int			breakDistance = (leftToRight) ? Convert.ToInt16(sweepPoints.Count * (1 - percentsToRemove)) : Convert.ToInt16(sweepPoints.Count * percentsToRemove);

			if(sweepPoints.Count > breakDistance)
			{
				ArrayList	distancesList = new ArrayList();

				foreach(Point sweepPoint in sweepPoints)
					distancesList.Add(sweepPoint.X);

				distancesList.Sort();	
				breakDistance = (int) distancesList[breakDistance];

				for(int i = sweepPoints.Count - 1; i >= 0; i--)
				{
					if(leftToRight)
					{
						if( ((Point)sweepPoints[i]).X < breakDistance)
							sweepPoints.RemoveAt(i);
					}
					else
					{
						if(((Point)sweepPoints[i]).X > breakDistance)
							sweepPoints.RemoveAt(i);
					}
				}
			}
		}
		#endregion

		#region RemovePointsWhereDistanceBiggerThan()
		private static void RemovePointsWhereDistanceBiggerThan(ref ArrayList sweepPoints, short distance, Rectangle imageRect)
		{
			if(sweepPoints.Count > 1)
			{
				long			center = GetCenter(sweepPoints, imageRect);			
			
				for(int i = sweepPoints.Count - 1; i >= 0; i--)
					if( (((Point)sweepPoints[i]).X - center > distance) || (((Point)sweepPoints[i]).X - center < -distance) )
						sweepPoints.RemoveAt(i);
			}
		}
		#endregion

		#region CheckIfDarkInBetween()
		private static byte CheckIfDarkInBetween(Bitmap bitmap, byte sweepLinesCount, Rectangle clip, short checkWidth,
			short areaCoeff)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			short		sweepLineDistance = Convert.ToInt16(clip.Height / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			byte		threshold;
			float		smoothness;
			//ArrayList	sweepPoints = new ArrayList();
			BitmapData	bitmapData = bitmap.LockBits(bitmapRect, ImageLockMode.ReadOnly, bitmap.PixelFormat); 
			int			stride = bitmapData.Stride; 
			Rectangle	localRect = new Rectangle(clip.X, 0, clip.Width, sweepLineDistance);
			short		darkCenters = 0;
			byte		confidence;

			for(int i = 0; i < sweepLinesCount; i++)
				verticalPositions[i] = Convert.ToInt16(clip.Y + (clip.Height / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 

				foreach(short y in verticalPositions)
				{
					localRect.Y = y;
					threshold = ContentLocator.GetThresholdGray(bitmapData, localRect);
					threshold = (byte) ((threshold - 30 > 100) ? threshold - 30 : 100);
					smoothness = ContentLocator.GetSmoothenessGray(bitmapData, localRect) * 1.1F;

					if(CheckIfDarkInBetweenPoints(bitmapData, pSource, stride, threshold, smoothness, localRect, checkWidth))
						darkCenters ++ ;
				}

				confidence = (byte) (100 * darkCenters / (float) sweepLinesCount);
			}
			
			bitmap.UnlockBits(bitmapData);
			return confidence;
		}
		#endregion

		#region CheckIfDarkInBetweenPoints()
		private static unsafe bool CheckIfDarkInBetweenPoints(BitmapData bitmapData, byte* pOrig, int stride, byte threshold, 
			float smoothness, Rectangle clip, short checkWidth)
		{
			byte			backgroundness = 15;
			Rectangle		zone = new Rectangle(clip.X, clip.Y, checkWidth, clip.Height);
			//byte			averageGray = ContentLocator.GetAverageGray(bitmapData, zone);
			int				jump = checkWidth;
			short			area;

			for(int x = clip.X; x < clip.Right - checkWidth; x += jump)
			{
				zone.X = x;
				area = GetBackgroundWeight(bitmapData, zone, threshold, smoothness);			

				if(area > backgroundness)
					return true;
				
				jump = checkWidth * area / 100;
				if(jump < 5)
					jump = 5;
			}

			return false;
		}
		#endregion
		
		#region GetCenter()
		private static short GetCenter(ArrayList sweepPoints, Rectangle rect)
		{
			double		numOfPoints = sweepPoints.Count;
			double		xTop, xBottom, a, b;
			long		sumXY = 0, sumX = 0, sumY = 0, sumYxY = 0;

			foreach(Point sweepPoint in sweepPoints)
			{
				sumXY += sweepPoint.X * sweepPoint.Y;
				sumX += sweepPoint.X;
				sumY += sweepPoint.Y;
				sumYxY += sweepPoint.Y * sweepPoint.Y;
			}
			
			if((sumYxY - sumY * sumY / numOfPoints) > 0)
				b = ( (sumXY - sumX * sumY / numOfPoints) / (sumYxY - sumY * sumY / numOfPoints));
			else
				b = 0;
			a = sumX / numOfPoints - b * sumY / numOfPoints;

			xTop = a;
			xBottom = a + b * rect.Height;

			return (short) (xTop + (xBottom - xTop) / 2);
		}
		#endregion
		
		#region GetBackgroundWeight()
		public static byte GetBackgroundWeight(BitmapData bitmapData, Rectangle clip, byte threshold, float minDelta)
		{
			int			stride = bitmapData.Stride; 
			short		clipX = (short) clip.X ;
			short		clipY = (short) clip.Y ;
			short		clipRight = (short) clip.Right;
			short		clipBottom = (short) clip.Bottom ;
			uint[]		array = new uint[256];
			int			x, y;
			ulong		deltaPixels = 0;
			ulong		objectPixels = 0;
			
			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 

				for(y = clipY; y < clipBottom; y++) 
				{ 
					pCurrent = pOrig + y * stride + clipX;

					for(x = clipX; x < clipRight - 1; x++) 
					{ 
						if(*pCurrent < threshold)
							objectPixels ++;

						if( (*pCurrent - pCurrent[1] < - minDelta) || (*pCurrent - pCurrent[1] > minDelta))
							deltaPixels ++ ;

						pCurrent++;
					} 
				}
			
				pCurrent = pOrig + y * stride + clipRight - 1;

				for(y = clipY; y < clipBottom; y++) 
				{ 
					if(*pCurrent < threshold)
						objectPixels ++;
					pCurrent += stride;
				}
			}

			float	objectPixelsPerc = objectPixels / (float) (clip.Width * clip.Height);
			float	deltaPixelsPerc = deltaPixels / (float) ((clip.Width - 1) * clip.Height);
			float	total = 100 * (objectPixelsPerc * deltaPixelsPerc + objectPixelsPerc + deltaPixelsPerc);
			//float	total = 100 * objectPixelsPerc + 200 * deltaPixelsPerc;

			if(total > 100)
				total = 100;
			if(total < 0)
				total = 0;

			return (byte) total;
		}
		#endregion
	}*/
}
