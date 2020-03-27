using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	public class BookfoldLocator
	{		

		#region constructor
		private BookfoldLocator()
		{
		}
		#endregion

		//	PUBLIC METHODS

		#region Get()
		public static short Get(Bitmap bitmap, byte sweepLinesCount, short offsetTop,
			short offsetBottom, float offsetLeft, float offsetRight, byte bookfoldWidth, byte pixelBlockSize, byte distanceBetweenCheckPoints,
			short maxValidPointDistanceToMedian, byte maxColumnDelta, byte maxPixelDelta, float bookfoldSideAreaWidthInch, out byte confidence)
		{
			try
			{
				short	result1 = 0, result2 = 0;
				byte	confidence1 = 0, confidence2 = 0;

				confidence = 0;
				
				switch(bitmap.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed :
					{
						if(ImageInfo.IsPaletteGrayscale(bitmap.Palette.Entries))
						{
							result1 = Get8bppGray(bitmap, sweepLinesCount, offsetTop, (short) (bitmap.Height - offsetBottom), offsetLeft, 
								offsetRight, pixelBlockSize, distanceBetweenCheckPoints, maxValidPointDistanceToMedian, maxColumnDelta, out confidence1);
						}
						else
							throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
					} break;
					case PixelFormat.Format24bppRgb :	
					{
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
					}
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}

				if(confidence1 < 35)
				{
					result2 = BookfoldLocatorAreas.Get(bitmap, (byte) (sweepLinesCount / 2), offsetTop,
						offsetBottom, offsetLeft, offsetRight, bookfoldWidth, distanceBetweenCheckPoints,
						maxValidPointDistanceToMedian, maxColumnDelta, maxPixelDelta,
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
				throw new Exception("BookfoldLocator, Get(): " + ex.Message ) ;
			}
		}
		#endregion
						
		//PRIVATE METHODS
						
		#region Get8bppGray()
		private static short Get8bppGray(Bitmap bitmap, byte sweepLinesCount, short offsetTop,
			short offsetBottom, float offsetLeft, float offsetRight, byte pixelBlockSize, byte distanceBetweenCheckPoints,
			short maxValidPointDistanceToMedian, short maxColumnDelta, out byte confidence)
		{
#if DEBUG
			DateTime	now = DateTime.Now ;
#endif
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			short		sweepLineDistance = Convert.ToInt16((offsetBottom - offsetTop) / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			short		xOffset = Convert.ToInt16(bitmapRect.Width * offsetLeft);
			short		xLength = Convert.ToInt16((offsetRight - offsetLeft) * bitmapRect.Width);
			short		bookfold = (short) (bitmapRect.Width / 2);
			ArrayList	sweepPointsTmp;
			
			BitmapData	bitmapData = bitmap.LockBits(bitmapRect, ImageLockMode.ReadWrite, bitmap.PixelFormat); 
			int			stride = bitmapData.Stride; 

			for(int i = 0; i < sweepLinesCount; i++)
			{
				verticalPositions[i] = Convert.ToInt16(offsetTop + ((offsetBottom - offsetTop) / (float) sweepLinesCount) * i);
			}
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(short sweepLine in verticalPositions)
				{
					sweepPointsTmp = GetSweepPointsGray(pSource, stride, sweepLine, xOffset, xLength, pixelBlockSize);
					
					if(sweepPointsTmp.Count > 0)
						sweepPointsTmp = ValidateSweepPointsGray(pSource, stride, sweepPointsTmp, distanceBetweenCheckPoints, 
							(short) (sweepLineDistance / 2), maxColumnDelta);
						
					if(sweepPointsTmp.Count > 3)
						sweepPointsTmp = Pick3BestSweepPointsGray(pSource, stride, sweepPointsTmp, xOffset, xLength);

					if(sweepPointsTmp.Count > 0)
						sweepPoints.AddRange(sweepPointsTmp);
				}

				RemoveWorstSweepPoints(ref sweepPoints, .2F, bitmapRect);
				//RemoveWorstSweepPoints(ref sweepPoints, .2F, bitmapRect);
				RemovePointsWhereDistanceBiggerThan(ref sweepPoints, maxValidPointDistanceToMedian, bitmapRect);

				bookfold = GetCenter(sweepPoints, bitmapRect);
				int		iConfidence = sweepPoints.Count * 125 / sweepLinesCount;
				confidence = (byte) ((iConfidence > 0) ? ((iConfidence < 100) ? iConfidence : 100) : 0);
			}
			
			bitmap.UnlockBits(bitmapData);

#if DEBUG
			Console.WriteLine("BookfoldLocator Get8bppGray():" + (DateTime.Now.Subtract(now)).ToString()) ;
#endif

			return bookfold;
		}
		#endregion
		
		#region GetSweepPointsGray()
		private static unsafe ArrayList GetSweepPointsGray(byte* pOrig, int stride, short y, short xOffset, short xLength, short blockLength)
		{
			ArrayList		sweepPoints = new ArrayList();
			Circle			blocks = new Circle();
			short			localValue;
			int				i, j;
			short			numOfBlocks = 5;
			byte			*pCurrent;
			byte			*pLocal;
			byte			localMin;
			int				localIndex;
			int				x;


			pCurrent = pOrig + y * stride + xOffset;

			for(i = 0; i < numOfBlocks; i++)
			{
				localValue = 0;
				
				for(j = 0; j < blockLength; j++)
					localValue += *(pCurrent++);

				blocks.Add(localValue);
			}

			for(i = xOffset + numOfBlocks * blockLength; i < xOffset + xLength; i += blockLength)
			{
				if(blocks.MayBeBookfold)
				{
					x = Convert.ToInt32(i - (blockLength * (numOfBlocks - numOfBlocks / 2)));
					pLocal = pOrig + y * stride + x;
					localMin = *pLocal;
					localIndex = x;

					for(j = 1; j < blockLength; j++)
					{
						pLocal ++ ;
						if(localMin > *pLocal)
						{
							localMin = *pLocal;
							localIndex = x + j;
						}
					}

					sweepPoints.Add(new Point(localIndex, y));
					//sweepPoints.Add(new Point((int) (i - (blockLength * 2.5F)), y));
				}
				
				localValue = 0;
				
				for(j = 0; j < blockLength; j++)
					localValue += *(pCurrent++);

				blocks.Add(localValue);
			}

			return sweepPoints;
		}
		#endregion
		
		#region ValidateSweepPointsGray()
		private static unsafe ArrayList ValidateSweepPointsGray(byte* pOrig, int stride, ArrayList sweepPoints,
			short distanceBetweenCheckPoints, short checkAreaHeight, short maxColumnDelta)
		{
			ArrayList		newSweepPoints = new ArrayList();
			bool			validSweepPoint;
			byte			*pCurrent;
			short			checkPointsCount = Convert.ToInt16(checkAreaHeight * 2 / distanceBetweenCheckPoints);
			short			checkAreaTop = Convert.ToInt16(((Point)sweepPoints[0]).Y - checkAreaHeight);
			short			checkAreaBottom = Convert.ToInt16(checkAreaTop + 2 * checkAreaHeight + 1);
			short			i;
			short			sweepPointValue;

			foreach(Point sweepPoint in sweepPoints)
			{
				pCurrent = pOrig + (sweepPoint.Y - checkAreaHeight) * stride + sweepPoint.X;
				validSweepPoint = true;
				sweepPointValue = *(pOrig + sweepPoint.Y * stride + sweepPoint.X);

				for(i = checkAreaTop; i < checkAreaBottom; i += distanceBetweenCheckPoints)
				{
					if(*pCurrent - sweepPointValue > maxColumnDelta || *pCurrent - sweepPointValue < -maxColumnDelta)
					{
						validSweepPoint = false;
						break;
					}

					pCurrent += (distanceBetweenCheckPoints * stride);
				}
				
				if(validSweepPoint)
					newSweepPoints.Add(sweepPoint);
			}

			return newSweepPoints;
		}
		#endregion
		
		#region Pick3BestSweepPointsGray()
		private static unsafe ArrayList Pick3BestSweepPointsGray(byte* pOrig, int stride, ArrayList sweepPoints, short xOffset, short xLength)
		{
			Point[]			newSweepPoints = new Point[3];
			byte			*pCurrent;
			int				i;
			float[]			threeBestValues = new float[3];
			float			currentValue;

			if(sweepPoints.Count > 3)
			{
				threeBestValues[0] = -1;
				threeBestValues[1] = -1;
				threeBestValues[2] = -1;
				
				foreach(Point sweepPoint in sweepPoints)
				{
					currentValue = 0;

					pCurrent = pOrig + sweepPoint.Y * stride + xOffset;
					for(i = xOffset; i < sweepPoint.X; i++)
					{
						if((*pCurrent - pCurrent[1]) < 20 && (*pCurrent - pCurrent[1]) > -20)
							currentValue += (*pCurrent - pCurrent[1]) / (float) (sweepPoint.X - i);

						pCurrent ++ ;
					}
					pCurrent = pOrig + sweepPoint.Y * stride + sweepPoint.X + 1;
					for(i = sweepPoint.X + 1; i < xOffset + xLength; i++)
					{
						if((*pCurrent - pCurrent[1]) < 20 && (*pCurrent - pCurrent[1]) > -20)
							currentValue += (*pCurrent - pCurrent[1]) / (float) (sweepPoint.X - i);

						pCurrent ++ ;
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
					if((current.Value - current.Next.Value > -(current.Value * .02F)) && 
						(current.Next.Value - current.Next.Next.Value > current.Next.Value * .01F) && 
						(current.Next.Next.Next.Value - current.Next.Next.Value > current.Next.Next.Next.Value * .01F) && 
						(current.Next.Next.Next.Next.Value - current.Next.Next.Next.Value > -(current.Next.Next.Next.Next.Value * .02F)) && 
						(current.Value - current.Next.Next.Value > current.Value * .1F) && 
						(current.Next.Next.Next.Next.Value - current.Next.Next.Value > current.Next.Next.Next.Next.Value * .1F))
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
