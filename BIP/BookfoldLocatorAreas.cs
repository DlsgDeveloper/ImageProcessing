using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	public class BookfoldLocatorAreas
	{		

		#region constructor
		private BookfoldLocatorAreas()
		{
		}
		#endregion

		//	PUBLIC METHODS

		#region Get()
		public static short Get(Bitmap bitmap, byte sweepLinesCount, short offsetTop,
			short offsetBottom, float offsetLeft, float offsetRight, byte bookfoldWidth, byte distanceBetweenCheckPoints,
			short maxSweepPointDistanceFromAxle, byte maxColumnDelta, byte maxPixelDelta, 
			float bookfoldSideAreaWidthInch, out byte confidence)
		{	
			short		result = 0;
			confidence = 0;
#if DEBUG
			DateTime	now = DateTime.Now ;
#endif

			try
			{			
				switch(bitmap.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed :
					{
						if(ImageInfo.IsPaletteGrayscale(bitmap.Palette.Entries))
						{
							result = Get8bppGray(bitmap, sweepLinesCount, offsetTop, (short) (bitmap.Height - offsetBottom), 
								Convert.ToInt16(bitmap.Width * offsetLeft), Convert.ToInt16(bitmap.Width * (offsetRight - offsetLeft)),
								bookfoldWidth, distanceBetweenCheckPoints, maxSweepPointDistanceFromAxle, 
								maxColumnDelta, maxPixelDelta, (short) (bitmap.VerticalResolution * bookfoldSideAreaWidthInch),
								out confidence);
							return result;
						}
						else
							throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
					}
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}
			}
			catch(Exception ex)
			{
				throw new Exception("BookfoldLocatorAreas, Get(): " + ex.Message ) ;
			}
			finally
			{
#if DEBUG
				Console.WriteLine(string.Format("BookfoldLocatorAreas: {0}, Confidence:{1}, Result: {2}" , DateTime.Now.Subtract(now).ToString(), confidence, result));
#endif
			}
		}
		#endregion
						
		//PRIVATE METHODS

		#region Get8bppGray()
		private static short Get8bppGray(Bitmap bitmap, byte sweepLinesCount, short yFrom,
			short yTo, short xStart, short xLength, byte bookfoldWidth, byte distanceBetweenCheckPoints,
			short maxSweepPointDistanceFromAxle, byte maxColumnDelta, byte maxPixelDelta,
			short checkAreaWidth, out byte confidence)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			short		sweepLineDistance = Convert.ToInt16((yTo - yFrom) / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			short		bookfold = (short) (bitmapRect.Width / 2);
			byte		threshold;
			float		delta;
			ArrayList	sweepPointsTmp;
			double		distance;
			
			BitmapData	bitmapData = bitmap.LockBits(bitmapRect, ImageLockMode.ReadOnly, bitmap.PixelFormat); 
			int			stride = bitmapData.Stride; 

			for(int i = 0; i < sweepLinesCount; i++)
				verticalPositions[i] = Convert.ToInt16(yFrom + ((yTo - yFrom) / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(short sweepLine in verticalPositions)
				{
					threshold = ContentLocator.GetThresholdGray(bitmapData, new Rectangle(xStart, sweepLine, xLength, sweepLineDistance));
					delta = GetDeltaGray(bitmapData, new Rectangle(xStart, sweepLine, xLength, sweepLineDistance));
					sweepPointsTmp = GetSweepPointsGray(bitmapData, pSource, stride, threshold, delta, sweepLine, xStart, (short) (xStart + xLength), bookfoldWidth, checkAreaWidth, sweepLineDistance);

					if(sweepPointsTmp.Count > 0)
						sweepPointsTmp = ValidateSweepPointsGray(bitmapData, pSource, stride, sweepPointsTmp, distanceBetweenCheckPoints, 
							sweepLineDistance, bookfoldWidth, maxColumnDelta, maxPixelDelta);

					if(sweepPointsTmp.Count > 0)
						sweepPoints.AddRange(sweepPointsTmp);
				}

				RemoveWorstSweepPoints(ref sweepPoints, .2F, bitmapRect);
				RemoveWorstSweepPoints(ref sweepPoints, .2F, bitmapRect);
				RemovePointsWhereDistanceBiggerThan(ref sweepPoints, maxSweepPointDistanceFromAxle, bitmapRect);

				bookfold = GetCenter(sweepPoints, bitmapRect);
				distance = GetAverageDistance(sweepPoints, bitmapRect);
				distance = (maxSweepPointDistanceFromAxle - distance) / maxSweepPointDistanceFromAxle;
				distance = (distance < 0) ? 0 : ((distance > 1) ? 1 : distance);

				int		iConfidence = (int) (((int) (sweepPoints.Count * 156 / sweepLinesCount)) * distance);
				confidence = (byte) ((iConfidence > 0) ? ((iConfidence < 100) ? iConfidence : 100) : 0);
			}
			
			bitmap.UnlockBits(bitmapData);
			return bookfold;
		}
		#endregion
		
		#region GetSweepPointsGray()
		private static unsafe ArrayList GetSweepPointsGray(BitmapData bitmapData, byte* pOrig, int stride, byte threshold, float delta,
			short y, short xFrom, short xTo, short bookfoldWidth, short checkAreaWidth, short checkAreaHeight)
		{
			ArrayList		sweepPoints = new ArrayList();
			short			checkAreaTop = (short) (y - checkAreaHeight / 2);
			int				x, i;
			byte			weight;
			byte			areaMax;
			short			deltaCoefficient = 10;
			byte			backgroundness = 70;
			Rectangle		zone = zone = new Rectangle(xFrom, checkAreaTop, xTo - xFrom, checkAreaHeight);
			byte			averageGray = ContentLocator.GetAverageGray(bitmapData, zone);
			byte			*pCurrent;

			int				leftLimita = xFrom;
			int				rightLimita = xTo;
			int				bestX = 0;
			int				bestBookfold = 0;;

			zone.Width = checkAreaWidth;

			for(x = xFrom; x < xTo - checkAreaWidth; x += 20)
			{
				zone.X = x;
				weight = GetBackgroundWeight(bitmapData, zone, averageGray, deltaCoefficient);
				
				if(weight > backgroundness)
				{
					leftLimita = x;
					break;
				}
			}

			for(x = xTo - checkAreaWidth; x > xFrom; x -= 20)
			{
				zone.X = x;
				weight = GetBackgroundWeight(bitmapData, zone, averageGray, deltaCoefficient);
				
				if(weight > backgroundness)
				{
					rightLimita = x;
					break;
				}
			}

			zone.Width = bookfoldWidth;

			for(x = leftLimita; x < rightLimita; x += bookfoldWidth / 3)
			{
				zone.X = x;
				weight = GetBookfoldWeight(bitmapData, zone, averageGray, delta);

				if(weight > bestBookfold)
				{
					bestBookfold = weight;
					bestX = x;
					
					pCurrent = pOrig + y * stride + x;
					areaMax = *(pCurrent++);

					for(i = x; i < x + 2 * bookfoldWidth; i++)
					{
						if(areaMax < *pCurrent)
						{
							areaMax = *pCurrent;
							bestX = i;
							pCurrent++;
						}						
					}	
				}
			}

			if(bestBookfold > 0)
				sweepPoints.Add(new Point(bestX, y));
			return sweepPoints;
		}
		#endregion
				
		#region ValidateSweepPointsGray()
		private static unsafe ArrayList ValidateSweepPointsGray(BitmapData bitmapData, byte* pOrig, int stride, ArrayList sweepPoints,
			short distanceBetweenCheckPoints, short checkAreaHeight, short bookfoldWidth, byte maxColumnDelta, byte maxPixelDelta)
		{
			ArrayList		newSweepPoints = new ArrayList();
			bool			validSweepPoint;
			byte			*pCurrent;
			short			checkPointsCount = Convert.ToInt16(checkAreaHeight * 2 / distanceBetweenCheckPoints);
			short			checkAreaTop = Convert.ToInt16(((Point)sweepPoints[0]).Y - checkAreaHeight);
			short			checkAreaBottom = Convert.ToInt16(checkAreaTop + 2 * checkAreaHeight + 1);
			int				i, x;
			short			sweepPointValue;
			short			minColumnValue, maxColumnValue, delta, maxDelta;

			if(checkAreaTop < 0)
				checkAreaTop = 0;
			if(checkAreaBottom > bitmapData.Height)
				checkAreaBottom = (short) bitmapData.Height;
			
			foreach(Point sweepPoint in sweepPoints)
			{
				validSweepPoint = true;

				for(x = sweepPoint.X - bookfoldWidth / 2; x < sweepPoint.X + bookfoldWidth / 2; x++)
				{
					pCurrent = pOrig + (sweepPoint.Y - checkAreaHeight) * stride + x;
					sweepPointValue = *(pCurrent);
					
					minColumnValue = sweepPointValue;
					maxColumnValue = sweepPointValue;
					maxDelta = 0;

					for(i = checkAreaTop; i < checkAreaBottom; i += distanceBetweenCheckPoints)
					{
						if(minColumnValue > *pCurrent)
							minColumnValue = *pCurrent;

						if(maxColumnValue < *pCurrent)
							maxColumnValue = *pCurrent;

						delta = (short) (*pCurrent - pCurrent[stride]);

						if(maxDelta < delta)
							maxDelta = delta;

						if(maxDelta < -delta)
							maxDelta = (short) -delta;

						pCurrent += (distanceBetweenCheckPoints * stride);
					}

					if((maxColumnValue - minColumnValue > maxColumnDelta) || (maxDelta > maxPixelDelta))
					{
						validSweepPoint = false;
						break;
					}				
				}

				if(validSweepPoint)
					newSweepPoints.Add(sweepPoint);
			}

			return newSweepPoints;
		}
		#endregion
		
		#region CheckNeighbourGray()
		private static unsafe ArrayList CheckNeighbourGray(BitmapData bitmapData, ArrayList sweepPoints, short bookfoldWidth, 
			short checkAreaHeight, short checkAreaWidth)
		{
			ArrayList		newSweepPoints = new ArrayList();
			float[]			threeBestValues = new float[3];
			short			checkAreaTop;
			byte			area;
			byte			averageGray;
			short			deltaCoefficient = 10;
			byte			backgroundness = 60;
			Rectangle		zone;
			
			foreach(Point sweepPoint in sweepPoints)
			{
				checkAreaTop = Convert.ToInt16(sweepPoint.Y - checkAreaHeight / 2);
				zone = new Rectangle(sweepPoint.X - 120, checkAreaTop, 240, checkAreaHeight);
				averageGray = ContentLocator.GetAverageGray(bitmapData, zone);

				zone.X = sweepPoint.X - bookfoldWidth / 2;
				zone.Width = bookfoldWidth;
				area = GetBackgroundWeight(bitmapData, zone, averageGray, deltaCoefficient);
				
				if(area < backgroundness)
				{
					zone.X = sweepPoint.X - checkAreaWidth - bookfoldWidth;
					zone.Width = checkAreaWidth;
					area = GetBackgroundWeight(bitmapData, zone, averageGray, deltaCoefficient);

					if( area > backgroundness )
					{
						zone.X = sweepPoint.X + bookfoldWidth;
						area = GetBackgroundWeight(bitmapData, zone, averageGray, deltaCoefficient);
							
						if( area > backgroundness )
						{
							newSweepPoints.Add(sweepPoint);
						}
					}
				}
			}

			return newSweepPoints;
		}
		#endregion

		#region RemoveWorstSweepPoints()
		private static void RemoveWorstSweepPoints(ref ArrayList sweepPoints, float percentsToRemove, Rectangle imageRect)
		{
			if(sweepPoints.Count > Convert.ToInt32(sweepPoints.Count * (1 - percentsToRemove)))
			{
				long			center = GetCenter(sweepPoints, imageRect);			
				ArrayList		distancesList = new ArrayList();

				foreach(Point sweepPoint in sweepPoints)
					distancesList.Add((short) Math.Abs(center - sweepPoint.X));

				distancesList.Sort();
				short		breakDistance = (short) distancesList[Convert.ToInt32(sweepPoints.Count * (1 - percentsToRemove))];

				for(int i = sweepPoints.Count - 1; i >= 0; i--)
					if( (((Point)sweepPoints[i]).X - center > breakDistance) || (((Point)sweepPoints[i]).X - center < -breakDistance) )
						sweepPoints.RemoveAt(i);
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

		#region GetCenter()
		private static short GetCenter(ArrayList sweepPoints, Rectangle rect)
		{
			if(sweepPoints.Count == 0)
				return 0;
			
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
		
		#region GetAverageDistance()
		public static float GetAverageDistance(ArrayList sweepPoints, Rectangle rect)
		{
			if(sweepPoints.Count == 0)
				return 0;
			
			double		numOfPoints = sweepPoints.Count;
			double		a, b;
			long		sumXY = 0, sumX = 0, sumY = 0, sumYxY = 0;
			float		distance = 0;

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

			PointF	A = new PointF((float) a, 0);
			PointF	B = new PointF((float) (a + b * rect.Height), rect.Height - 1);

			foreach(Point C in sweepPoints)
			{
				if(B.X != A.X && B.Y != A.Y)
					distance += Math.Abs( (C.X - A.X) + ((A.X - B.X) * (A.Y - C.Y) / (A.Y - B.Y)) );
				else
					distance += Math.Abs(A.X - C.X);
			}

			return (float) (distance / numOfPoints);
		}
		#endregion
		
		#region GetDeltaGray()
		public static float GetDeltaGray(BitmapData bitmapData, Rectangle clip)
		{
			int			stride = bitmapData.Stride; 
			short		clipX = (short) clip.X ;
			short		clipY = (short) clip.Y ;
			short		clipRight = (short) (clip.Right - 1);
			short		clipBottom = (short) clip.Bottom ;
			uint[]		array = new uint[256];
			int			x, y;
			ulong		delta = 0;
			int			deltaLocal;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer() + clipX; 
				byte*	pCurrent ; 

				for(y = clipY; y < clipBottom; y++) 
				{ 
					pCurrent = pOrig + y * stride;

					for(x = clipX; x < clipRight - 1; x++) 
					{ 
						deltaLocal = *pCurrent - pCurrent[1];
						if(deltaLocal < 0)
							deltaLocal = - deltaLocal;
						
						if(deltaLocal > 3 && deltaLocal < 20)
							delta += (uint) deltaLocal;
						//delta += (*pCurrent > pCurrent[1]) ? (uint) (*pCurrent - pCurrent[1]) : (uint) (pCurrent[1] - *pCurrent);
						pCurrent++;
					} 
				}
			}

			return delta / (float) ((clip.Width - 1) * clip.Height);
		}
		#endregion

		#region GetBackgroundWeight()
		public static byte GetBackgroundWeight(BitmapData bitmapData, Rectangle clip, byte averageGray, short deltaCoeff)
		{
			int			stride = bitmapData.Stride; 
			short		clipX = (short) clip.X ;
			short		clipY = (short) clip.Y ;
			short		clipRight = (short) (clip.Right - 1);
			short		clipBottom = (short) clip.Bottom ;
			int			x, y;
			ulong		delta = 0;
			int			deltaLocal;
			ulong		color = 0;
			

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 

				for(y = clipY; y < clipBottom; y++) 
				{ 
					pCurrent = pOrig + y * stride + clipX;

					for(x = clipX; x < clipRight - 1; x++) 
					{ 
						color += *pCurrent;
						deltaLocal = *pCurrent - pCurrent[1];
						if(deltaLocal > 3 || deltaLocal < -3)
							delta += (uint) (deltaLocal * deltaLocal);
						pCurrent++;
					} 
				}
			
				pCurrent = pOrig + y * stride + clipRight - 1;

				for(y = clipY; y < clipBottom ; y++) 
				{ 
					color += *pCurrent;
					pCurrent += stride;
				}
			}

			float	colorAverage = color / (float) (clip.Width * clip.Height);
			float	deltaAverage = delta / (float) ((clip.Width - 1) * clip.Height);
			float	total = 100 * (colorAverage / averageGray - deltaAverage / (deltaCoeff * deltaCoeff));

			if(total > 100)
				total = 100;
			if(total < 0)
				total = 0;

			return (byte) total;
		}
		#endregion

		#region GetBookfoldWeight()
		public static byte GetBookfoldWeight(BitmapData bitmapData, Rectangle clip, byte averageGray, float deltaCoeff)
		{
			int			stride = bitmapData.Stride; 
			short		x, y;
			float		delta = 0;
			int			deltaLocal;
			float[]		columns = new float[clip.Width];
			
			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer(); 
				byte*	pCurrent ; 

				for(y = (short) clip.Y; y < clip.Bottom; y++) 
				{ 
					pCurrent = pOrig + y * stride + clip.X;

					for(x = 0; x < clip.Width - 1; x++) 
					{ 
						deltaLocal = *pCurrent - pCurrent[1];
						if(deltaLocal < 0)
							deltaLocal = - deltaLocal;
						if(deltaLocal > 3 && deltaLocal < 20)
							delta += (uint) deltaLocal;

						columns[x] += *pCurrent;
						pCurrent++;
					} 
				}
			
				pCurrent = pOrig + clip.Y * stride + clip.Right - 1;

				for(y = (short) clip.Y; y < clip.Bottom; y++) 
				{ 
					columns[clip.Width-1] += *pCurrent;
					pCurrent += stride;
				}
			}

			delta = delta / (uint) (((clip.Width - 1) * clip.Height));

			for(x = 0; x < clip.Width; x++)
				columns[x] = columns[x] / clip.Height;

			int		darkestColumn = 0;
			int		leftColumn, rightColumn;

			for(x = 1; x < clip.Width; x++)
				if(columns[x] < columns[darkestColumn])
					darkestColumn = x;

			leftColumn = rightColumn = darkestColumn;

			for(x = (short) (darkestColumn - 1); (x >= 0) && (x >= clip.Width / 2 - darkestColumn); x--)
				if(columns[x] > columns[x+1])
					leftColumn = x;
				else
					break;

			for(x = (short) (darkestColumn + 1); (x < clip.Width) && (x <= darkestColumn + clip.Width / 2); x++)
				if(columns[x] > columns[x-1])
					rightColumn = x;
				else
					break;

			if(rightColumn - leftColumn < clip.Width / 2)
				return 0;

			if(deltaCoeff > 0 && delta / deltaCoeff < 2)
				return 0;

			if(columns[rightColumn] + columns[leftColumn] - 2 * columns[darkestColumn] < 20)
				return 0;

			if(columns[darkestColumn] >= averageGray)
				return 0;

			float	bookfoldWidth = (rightColumn - leftColumn) / (float) clip.Width;
			float	bookfoldDelta = (columns[rightColumn] + columns[leftColumn] - 2 * columns[darkestColumn]) / 40.0F;
			float	sidesDelta = (columns[rightColumn] - columns[leftColumn] == 0) ? 1.0F : 1.0F / (columns[rightColumn] - columns[leftColumn]);
			float	averageColor = (columns[rightColumn] + columns[leftColumn] - 2 * averageGray == 0) ? 1.0F : 1.0F / (columns[rightColumn] + columns[leftColumn] - 2 * averageGray);
			float	averageDelta = ( delta / deltaCoeff);

			if(bookfoldDelta > 1.0F)
				bookfoldDelta = 1.0F;

			if(sidesDelta < 0)
				sidesDelta = - sidesDelta;
			if(sidesDelta > 1.0F)
				sidesDelta = 1.0F;
			if(averageColor > 1.0F)
				averageColor = 1.0F;
			if(averageDelta > 1.0F)
				averageDelta = 1.0F;

			float	result = 30 * bookfoldWidth + 25 * bookfoldDelta + 10 * sidesDelta + 10 * averageColor + 25 * averageDelta;

			return (byte) result;
		}
		#endregion

	}
}
