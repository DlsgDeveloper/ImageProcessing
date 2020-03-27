using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	public class BookfoldLocatorNew
	{		

		#region constructor
		private BookfoldLocatorNew()
		{
		}
		#endregion

		//	PUBLIC METHODS

		#region Get()
		public static short Get(Bitmap bitmap, byte sweepLinesCount, short offsetTop,
			short offsetBottom, float offsetLeft, float offsetRight, byte bookfoldWidth, byte distanceBetweenCheckPoints,
			short maxSweepPointDistanceFromAxle, bool checkSizeAreasForBackground, byte maxColumnDelta, byte maxPixelDelta,
			float bookfoldSideAreaWidthInch, out byte confidence)
		{
#if DEBUG
			DateTime	now = DateTime.Now ;
#endif
			short	result1 = 0, result2 = 0;
			byte	confidence1 = 0, confidence2 = 0;

			confidence = 0;
			
			try
			{
				switch(bitmap.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed :
					{
						if(ImageInfo.IsPaletteGrayscale(bitmap.Palette.Entries))
						{
							result1 = Get8bppGray(bitmap, sweepLinesCount, offsetTop, (short) (bitmap.Height - offsetBottom), 
								Convert.ToInt16(bitmap.Width * offsetLeft), Convert.ToInt16(bitmap.Width * (offsetRight - offsetLeft)),
								bookfoldWidth, distanceBetweenCheckPoints, maxSweepPointDistanceFromAxle, 
								checkSizeAreasForBackground, maxColumnDelta, (short) (bitmap.VerticalResolution * bookfoldSideAreaWidthInch),
								out confidence1);
						}
						else
							throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
					} break;
					case PixelFormat.Format24bppRgb :	
					{
						confidence = 0;
					} break;
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}

				if(confidence1 < 35)
				{
					result2 = BookfoldLocatorAreas.Get(bitmap, sweepLinesCount, offsetTop,
						offsetBottom, offsetLeft, offsetRight, bookfoldWidth, distanceBetweenCheckPoints,
						maxSweepPointDistanceFromAxle, maxColumnDelta, maxPixelDelta,
						bookfoldSideAreaWidthInch, out confidence2);

					if(confidence1 > confidence2)
					{
						confidence = confidence1;
						return result1;
					}
					else
					{
						confidence = confidence2;
						return result2;
					}
				}
				else
				{
					confidence = confidence1;
					return result1;
				}
			}
			catch(Exception ex)
			{
				throw new Exception("BookfoldLocatorNew, Get(): " + ex.Message ) ;
			}
			finally
			{
#if DEBUG
				Console.WriteLine(string.Format("BookfoldLocatorNew: {0}, Confidence:{1}, Result: {2}" , DateTime.Now.Subtract(now).ToString(), confidence, (confidence1 > confidence2) ? result1 : result2));
#endif
			}
		}
		#endregion
						
		//PRIVATE METHODS
						
		#region Get24bpp()
		private static short Get24bpp(Bitmap bitmap, byte sweepLinesCount, short offsetTop,
			short offsetBottom, float offsetLeft, float offsetRight, byte pixelBlockSize, byte distanceBetweenCheckPoints,
			short maxSweepPointDistanceFromAxle, out byte confidence)
		{
#if DEBUG
			DateTime	now = DateTime.Now ;
#endif
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			short		sweepLineDistance = Convert.ToInt16((offsetBottom - offsetTop) / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			short		xOffset = Convert.ToInt16(bitmapRect.Width * offsetLeft);
			short		xLength = Convert.ToInt16((offsetRight - offsetLeft) * bitmapRect.Width);
			short		bookfold = (short) -1;
			
			BitmapData	bitmapData = bitmap.LockBits(bitmapRect, ImageLockMode.ReadWrite, bitmap.PixelFormat); 
			int			stride = bitmapData.Stride; 

			for(int i = 0; i < sweepLinesCount; i++)
				verticalPositions[i] = Convert.ToInt16(offsetTop + ((offsetBottom - offsetTop) / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(short sweepLine in verticalPositions)
				{
					ArrayList	sweepPointsTmp = GetSweepPoints24bpp(pSource, stride, sweepLine, xOffset, xLength, pixelBlockSize);
					
					if(sweepPointsTmp.Count > 0)
						sweepPointsTmp = ValuateSweepPoints24bpp(pSource, stride, sweepPointsTmp, distanceBetweenCheckPoints, (short) (sweepLineDistance / 2));
						
					if(sweepPointsTmp.Count > 3)
						sweepPointsTmp = Pick3BestSweepPoints24bpp(pSource, stride, sweepPointsTmp, xOffset, xLength);

					if(sweepPointsTmp.Count > 0)
						sweepPoints.AddRange(sweepPointsTmp);
				}

				RemoveWorstSweepPoints(ref sweepPoints, .2F, bitmapRect);
				RemoveWorstSweepPoints(ref sweepPoints, .2F, bitmapRect);
				RemovePointsWhereDistanceBiggerThan(ref sweepPoints, maxSweepPointDistanceFromAxle, bitmapRect);

				bookfold = GetCenter(sweepPoints, bitmapRect);
				confidence = Convert.ToByte(sweepPoints.Count / sweepLinesCount);
			}
			
			bitmap.UnlockBits(bitmapData);

#if DEBUG
			Console.WriteLine("BookfoldLocatorNew Get24bpp():" + (DateTime.Now.Subtract(now)).ToString()) ;
#endif

			return bookfold;
		}
		#endregion

		#region Get8bppGray()
		private static short Get8bppGray(Bitmap bitmap, byte sweepLinesCount, short yFrom,
			short yTo, short xStart, short xLength, byte bookfoldWidth, byte distanceBetweenCheckPoints,
			short maxSweepPointDistanceFromAxle, bool checkSizeAreasForBackground, byte maxColumnDelta, short checkAreaWidth, out byte confidence)
		{
#if DEBUG
			DateTime	now = DateTime.Now ;
#endif
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			short		sweepLineDistance = Convert.ToInt16((yTo - yFrom) / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			short		bookfold = (short) (bitmapRect.Width / 2);
			byte		threshold;
			ArrayList	sweepPointsTmp;
			
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
					sweepPointsTmp = GetSweepPointsGray(pSource, stride, threshold, sweepLine, xStart, xLength, bookfoldWidth, checkSizeAreasForBackground);
					
					if(sweepPointsTmp.Count > 0)
						sweepPointsTmp = ValidateSweepPointsGray(pSource, stride, sweepPointsTmp, distanceBetweenCheckPoints, 
							(short) (sweepLineDistance / 2), bookfoldWidth, maxColumnDelta);
						
					if(checkSizeAreasForBackground && sweepPointsTmp.Count > 0)
						sweepPointsTmp = CheckNeighbourGray(bitmapData, sweepPointsTmp, bookfoldWidth, sweepLineDistance, checkAreaWidth);

					if(sweepPointsTmp.Count > 0)
						sweepPoints.AddRange(sweepPointsTmp);
				}

				RemoveWorstSweepPoints(ref sweepPoints, .2F, bitmapRect);
				RemoveWorstSweepPoints(ref sweepPoints, .2F, bitmapRect);
				RemovePointsWhereDistanceBiggerThan(ref sweepPoints, maxSweepPointDistanceFromAxle, bitmapRect);

				bookfold = GetCenter(sweepPoints, bitmapRect);
				float			iConfidence = 100 * (sweepPoints.Count / (sweepLinesCount * 0.64F));
				confidence = (byte) ((iConfidence > 0) ? ((iConfidence < 100) ? iConfidence : 100) : 0);
			}
			
			bitmap.UnlockBits(bitmapData);

#if DEBUG
			Console.WriteLine("BookfoldLocatorNew Get8bppGray():" + (DateTime.Now.Subtract(now)).ToString()) ;
#endif

			return bookfold;
		}
		#endregion

		#region GetSweepPoints24bpp()
		private static unsafe ArrayList GetSweepPoints24bpp(byte* pOrig, int stride, short y, short xOffset, short xLength, short blockLength)
		{
			ArrayList		sweepPoints = new ArrayList();
			Circle			blocks = new Circle();
			short			localValue;
			int				i, j;
			short			numOfBlocks = 5;
			byte			*pCurrent;
			byte			*pLocal;
			short			localMin;
			int				localIndex;
			int				x;


			pCurrent = pOrig + y * stride + xOffset * 3;

			for(i = 0; i < numOfBlocks; i++)
			{
				localValue = 0;
				
				for(j = 0; j < blockLength; j++)
				{
					//localValue += *(pCurrent++);
					localValue += (short) (*(pCurrent++) * 0.114F + *(pCurrent++) * 0.587F + *(pCurrent++) * 0.299F);
				}

				blocks.Add(localValue);
			}

			for(i = xOffset + numOfBlocks * blockLength; i < xOffset + xLength; i += blockLength)
			{
				if(blocks.MayBeBookfold)
				{
					x = Convert.ToInt32(i - (blockLength * (numOfBlocks - numOfBlocks / 2)));
					pLocal = pOrig + y * stride + x * 3;
					localMin = (short) (*pLocal * 0.114F + pLocal[1] * 0.587F + pLocal[2] * 0.299F);
					localIndex = x;

					for(j = 1; j < blockLength; j++)
					{
						pLocal += 3 ;
						localValue = (short) (*pLocal * 0.114F + pLocal[1] * 0.587F + pLocal[2] * 0.299F);

						if(localMin > localValue)
						{
							localMin = localValue;
							localIndex = x + j;
						}
					}

					sweepPoints.Add(new Point(localIndex, y));
					//sweepPoints.Add(new Point((int) (i - (blockLength * 2.5F)), y));
				}
				
				localValue = 0;
				
				for(j = 0; j < blockLength; j++)
					localValue += (short) (*(pCurrent++) * 0.114F + *(pCurrent++) * 0.587F + *(pCurrent++) * 0.299F);

				blocks.Add(localValue);
			}

			return sweepPoints;
		}
		#endregion
		
		#region GetSweepPointsGray()
		private static unsafe ArrayList GetSweepPointsGray(byte* pOrig, int stride, byte threshold, short y, 
			short xOffset, short xLength, short bookfoldWidth, bool checkSizeAreasForBackground)
		{
			ArrayList		sweepPoints = new ArrayList();
			int				i;
			byte			*pCurrent;
			int				x;
			short			bookfoldPointsToCheck = (short) (bookfoldWidth / 2);
			short			xFrom =  (short) (xOffset + bookfoldPointsToCheck);
			short			xTo = (short) (xOffset + xLength - bookfoldPointsToCheck);
			bool			isThresholdPoint;

			pCurrent = pOrig + y * stride + xFrom;

			for(x = xFrom; x < xTo; x++)
			{
				if( (*pCurrent < threshold - 10) && (pCurrent[-bookfoldPointsToCheck] - *pCurrent > 14) && (pCurrent[bookfoldPointsToCheck] - *pCurrent > 14))
				{
					//check if point is in between of 2 decreasing gradients
					isThresholdPoint = true;

					for(i = - bookfoldPointsToCheck; i < 0; i++)
						if(pCurrent[i] < pCurrent[i+1])
						{
							isThresholdPoint = false;
							break;
						}

					if(isThresholdPoint)
					{
						for(i = 0; i < bookfoldPointsToCheck; i++)
							if(pCurrent[i] > pCurrent[i+1])
							{
								isThresholdPoint = false;
								break;
							}
					}

					//check if columns are solid
					if(isThresholdPoint)
					{
						sweepPoints.Add(new Point(x, y));	
						x += bookfoldPointsToCheck;
						pCurrent += bookfoldPointsToCheck;
					}
				}
				
				pCurrent++; 
			}

			return sweepPoints;
		}
		#endregion

		#region ValuateSweepPoints24bpp()
		private static unsafe ArrayList ValuateSweepPoints24bpp(byte* pOrig, int stride, ArrayList sweepPoints,
			short distanceBetweenCheckPoints, short checkAreaHeight)
		{
			ArrayList		newSweepPoints = new ArrayList();
			bool			validSweepPoint;
			byte			*pCurrent;
			short			checkPointsCount = Convert.ToInt16(checkAreaHeight * 2 / distanceBetweenCheckPoints);
			short			checkAreaTop = Convert.ToInt16(((Point)sweepPoints[0]).Y - checkAreaHeight);
			short			checkAreaBottom = Convert.ToInt16(checkAreaTop + 2 * checkAreaHeight + 1);
			short			i;
			short			sweepPointValue;
			short			currentValue;

			foreach(Point sweepPoint in sweepPoints)
			{
				pCurrent = pOrig + sweepPoint.Y * stride + sweepPoint.X * 3;
				sweepPointValue = (short) (*pCurrent * 0.114F + pCurrent[1] * 0.587F + pCurrent[2] * 0.299F);

				pCurrent = pOrig + (sweepPoint.Y - checkAreaHeight) * stride + sweepPoint.X * 3;
				currentValue = (short) (*pCurrent * 0.114F + pCurrent[1] * 0.587F + pCurrent[2] * 0.299F);
				validSweepPoint = true;

				for(i = checkAreaTop; i < checkAreaBottom; i += distanceBetweenCheckPoints)
				{
					if(currentValue - sweepPointValue > 50 || currentValue - sweepPointValue < -50)
					{
						validSweepPoint = false;
						break;
					}

					pCurrent += (distanceBetweenCheckPoints * stride);
					currentValue = (short) (*pCurrent * 0.114F + pCurrent[1] * 0.587F + pCurrent[2] * 0.299F);
				}
				
				if(validSweepPoint)
					newSweepPoints.Add(sweepPoint);
			}

			return newSweepPoints;
		}
		#endregion
		
		#region ValidateSweepPointsGray()
		private static unsafe ArrayList ValidateSweepPointsGray(byte* pOrig, int stride, ArrayList sweepPoints,
			short distanceBetweenCheckPoints, short checkAreaHeight, short bookfoldWidth, byte maxColumnDelta)
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

					if((maxColumnValue - minColumnValue > maxColumnDelta) || (maxDelta > 20))
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
		
		#region Pick3BestSweepPoints24bpp()
		private static unsafe ArrayList Pick3BestSweepPoints24bpp(byte* pOrig, int stride, ArrayList sweepPoints, short xOffset, short xLength)
		{
			Point[]			newSweepPoints = new Point[3];
			byte			*pCurrent;
			int				i;
			float[]			threeBestValues = new float[3];
			float			currentValue;
			short			pixel1;
			short			pixel2;

			if(sweepPoints.Count > 3)
			{
				threeBestValues[0] = -1;
				threeBestValues[1] = -1;
				threeBestValues[2] = -1;
				
				foreach(Point sweepPoint in sweepPoints)
				{
					currentValue = 0;

					pCurrent = pOrig + sweepPoint.Y * stride + xOffset * 3;
					pixel1 = (short) (*(pCurrent++) * 0.114F + *(pCurrent++) * 0.587F + *(pCurrent++) * 0.299F);
					pixel2 = (short) (*(pCurrent++) * 0.114F + *(pCurrent++) * 0.587F + *(pCurrent++) * 0.299F);

					for(i = xOffset; i < sweepPoint.X; i++)
					{
						if((pixel1 - pixel2) < 20 && (pixel1 - pixel2) > -20)
							currentValue += (pixel1 - pixel2) / (float) (sweepPoint.X - i);

						pixel1 = pixel2;
						pixel2 = (short) (*(pCurrent++) * 0.114F + *(pCurrent++) * 0.587F + *(pCurrent++) * 0.299F);
					}
					
					pCurrent = pOrig + sweepPoint.Y * stride + (sweepPoint.X + 1) * 3;
					pixel1 = (short) (*(pCurrent++) * 0.114F + *(pCurrent++) * 0.587F + *(pCurrent++) * 0.299F);
					pixel2 = (short) (*(pCurrent++) * 0.114F + *(pCurrent++) * 0.587F + *(pCurrent++) * 0.299F);

					for(i = sweepPoint.X + 1; i < xOffset + xLength; i++)
					{
						if((pixel1 - pixel2) < 20 && (pixel1 - pixel2) > -20)
							currentValue += (pixel1 - pixel2) / (float) (sweepPoint.X - i);

						pixel1 = pixel2;
						pixel2 = (short) (*(pCurrent++) * 0.114F + *(pCurrent++) * 0.587F + *(pCurrent++) * 0.299F);
					}

					if((threeBestValues[0] <= threeBestValues[1]) && (threeBestValues[0] <= threeBestValues[2]))
					{
						if(threeBestValues[0] < currentValue)
						{
							threeBestValues[0] = currentValue;
							newSweepPoints[0] = sweepPoint;
						}
					}
					else if(threeBestValues[1] <= threeBestValues[2])
					{
						if(threeBestValues[1] < currentValue)
						{
							threeBestValues[1] = currentValue;
							newSweepPoints[1] = sweepPoint;
						}
					}
					else
					{
						if(threeBestValues[2] < currentValue)
						{
							threeBestValues[2] = currentValue;
							newSweepPoints[2] = sweepPoint;
						}
					}
				}
			}
			else
				return sweepPoints;

			return new ArrayList(newSweepPoints);
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
				area = BookfoldLocatorAreas.GetBackgroundWeight(bitmapData, zone, averageGray, deltaCoefficient);
				
				if(area < backgroundness)
				{
					zone.X = sweepPoint.X - checkAreaWidth;
					zone.Width = checkAreaWidth  - bookfoldWidth;
					area = BookfoldLocatorAreas.GetBackgroundWeight(bitmapData, zone, averageGray, deltaCoefficient);

					if( area > backgroundness )
					{
						zone.X = sweepPoint.X + bookfoldWidth;
						area = BookfoldLocatorAreas.GetBackgroundWeight(bitmapData, zone, averageGray, deltaCoefficient);
							
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

		#region Circle
		private unsafe class Circle
		{
			CircleItem		item1 = new CircleItem();
			CircleItem		item2 = new CircleItem();
			CircleItem		item3 = new CircleItem();
			CircleItem		item4 = new CircleItem();
			CircleItem		item5 = new CircleItem();
			CircleItem		current;

			public Circle()
			{
				item1.Next = item2;
				item2.Next = item3;
				item3.Next = item4;
				item4.Next = item5;
				item5.Next = item1;

				current = item1;
			}

			public void Add(short val)
			{
				current.Value = val;
				current = current.Next;
			}

			public bool MayBeBookfold
			{
				get
				{
					if((current.Value - current.Next.Value > -30) && (current.Next.Value - current.Next.Next.Value > 50) && 
						(current.Next.Next.Value - current.Next.Next.Next.Value < - 50) && 
						(current.Next.Next.Next.Value - current.Next.Next.Next.Next.Value < 30) && 
						(current.Value - current.Next.Next.Value > 100) && 
						(current.Next.Next.Next.Next.Value - current.Next.Next.Value > 100))
						return true;

					return false;
				}
			}

		}
		#endregion
		
		#region class CircleItem
		private unsafe class CircleItem
		{
			short		itemValue;
			CircleItem	next;

			public CircleItem()
			{
			}

			public short		Value	{ get{return this.itemValue;} set{this.itemValue = value;} }
			public CircleItem	Next	{ get{return this.next;} set{this.next = value;} }
		}
		#endregion
		
	}
}
