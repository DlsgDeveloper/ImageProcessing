using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	/*public class ContentLocator
	{		

		#region constructor
		private ContentLocator()
		{
		}
		#endregion

		//	PUBLIC METHODS

		#region Get()
		public static Rectangle Get(Bitmap bitmap, byte sweepLinesCount, Rectangle offsetRect,
			RectangleF searchLimit, float blockSize, 
			byte maxBackColorDifference, float maxBackDeltaDifference, 
			byte maxFanColorDifference, float maxFanDeltaDifference, short maxValidPointDistanceToMedian, int flag)
		{
#if DEBUG
			DateTime	now = DateTime.Now ;
#endif
			Rectangle	result = Rectangle.Empty;

			try
			{
				switch(bitmap.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed :
					{
						if(ImageInfo.IsPaletteGrayscale(bitmap.Palette.Entries))
						{
							result = Get8bppGray(bitmap, sweepLinesCount, offsetRect, searchLimit, blockSize, 
								maxBackColorDifference, maxBackDeltaDifference, 
								maxFanColorDifference, maxFanDeltaDifference, maxValidPointDistanceToMedian, flag);
							return result;
						}
						else
							throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
					}
					case PixelFormat.Format24bppRgb :	
					{
						result = Get24bpp(bitmap, sweepLinesCount, offsetRect, searchLimit, blockSize, 
							maxBackColorDifference, maxBackDeltaDifference, 
							maxFanColorDifference, maxFanDeltaDifference, maxValidPointDistanceToMedian, flag);
						return result;
					}
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}
			}
			catch(Exception ex)
			{
				throw new Exception("ContentLocator, Get(): " + ex.Message ) ;
			}
			finally
			{
#if DEBUG
				Console.WriteLine(string.Format("ContentLocator: {0}, Clip: {1}" , DateTime.Now.Subtract(now).ToString(), result.ToString()));
#endif
			}
		}
		#endregion
						
		//PRIVATE METHODS
						
		#region Get24bpp()
		public static Rectangle Get24bpp(Bitmap bitmap, byte sweepLinesCount, Rectangle offsetRect,
			RectangleF searchLimit, float blockSize, 
			byte maxBackColorDifference, float maxBackDeltaDifference, 
			byte maxFanColorDifference, float maxFanDeltaDifference, short maxValidPointDistanceToMedian, int flag)
		{
			Rectangle	clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			BitmapData	bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat); 
			int			stride = bitmapData.Stride; 
						 
			unsafe
			{
				short	left, right;

				if((flag & 1) == 1)
					left = GetLeftBorder24bpp(bitmapData, sweepLinesCount, Convert.ToInt16(bitmap.Width * searchLimit.Left), 
						Convert.ToInt16(blockSize * bitmap.HorizontalResolution), 
						maxFanColorDifference, maxFanDeltaDifference, maxValidPointDistanceToMedian, 0);
				else
					left = GetLeftBorder24bpp(bitmapData, sweepLinesCount, Convert.ToInt16(bitmap.Width * searchLimit.Left), 
						Convert.ToInt16(blockSize * bitmap.HorizontalResolution), 
						maxBackColorDifference, maxBackDeltaDifference, maxValidPointDistanceToMedian, 20);
				
				if((flag & 2) == 2)
					right = GetRightBorder24bpp(bitmapData, sweepLinesCount, Convert.ToInt16(searchLimit.Width * bitmap.Width), 
						Convert.ToInt16(blockSize * bitmap.HorizontalResolution), 
						maxFanColorDifference, maxFanDeltaDifference, maxValidPointDistanceToMedian, 0);
				else
					right = GetRightBorder24bpp(bitmapData, sweepLinesCount, Convert.ToInt16(searchLimit.Width * bitmap.Width), 
						Convert.ToInt16(blockSize * bitmap.HorizontalResolution), 
						maxBackColorDifference, maxBackDeltaDifference, maxValidPointDistanceToMedian, 20);

				short	top = GetTopBorder24bpp( bitmapData, sweepLinesCount, Convert.ToInt16(bitmap.Height * searchLimit.Top), 
					Convert.ToInt16(blockSize * bitmap.VerticalResolution), 
					maxBackColorDifference, maxBackDeltaDifference, maxValidPointDistanceToMedian);
				
				short	bottom = GetBottomBorder24bpp( bitmapData, sweepLinesCount, 
					Convert.ToInt16(searchLimit.Height * bitmap.Height), Convert.ToInt16(blockSize * bitmap.VerticalResolution), 
					maxBackColorDifference, maxBackDeltaDifference, maxValidPointDistanceToMedian);

				clip = Rectangle.FromLTRB(left, top, right, bottom);
			}
			
			bitmap.UnlockBits(bitmapData);
			return clip;
		}
		#endregion

		#region Get8bppGray()
		public static Rectangle Get8bppGray(Bitmap bitmap, byte sweepLinesCount, Rectangle offsetRect,
			RectangleF searchLimit, float blockSize, 
			byte maxBackColorDifference, float maxBackDeltaDifference, 
			byte maxFanColorDifference, float maxFanDeltaDifference, short maxValidPointDistanceToMedian, int flag)
		{
			Rectangle	clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			BitmapData	bitmapData = bitmap.LockBits(offsetRect, ImageLockMode.ReadWrite, bitmap.PixelFormat); 
			int			stride = bitmapData.Stride; 
						 
			unsafe
			{
				short	left, right;

				if((flag & 1) == 1)
					left = GetLeftBorderGray(bitmapData, sweepLinesCount, Convert.ToInt16(offsetRect.Width * searchLimit.Left), 
						Convert.ToInt16(blockSize * bitmap.HorizontalResolution), 
						maxFanColorDifference, maxFanDeltaDifference, maxValidPointDistanceToMedian);
				else
				{
					left = Convert.ToInt16(blockSize * bitmap.HorizontalResolution / 4);
				}
				
				if((flag & 2) == 2)
					right = GetRightBorderGray(bitmapData, sweepLinesCount, Convert.ToInt16(offsetRect.Width * searchLimit.Width), 
						Convert.ToInt16(blockSize * bitmap.HorizontalResolution), 
						maxFanColorDifference, maxFanDeltaDifference, maxValidPointDistanceToMedian);
				else
				{
					right = Convert.ToInt16(bitmapData.Width - blockSize * bitmap.HorizontalResolution / 4);
				}

				short	top = GetTopBorderGray( bitmapData, sweepLinesCount, Convert.ToInt16(offsetRect.Height * searchLimit.Top), 
					Convert.ToInt16(blockSize * bitmap.VerticalResolution), 
					maxBackColorDifference, maxBackDeltaDifference, maxValidPointDistanceToMedian);
				
				short	bottom = GetBottomBorderGray( bitmapData, sweepLinesCount,
					Convert.ToInt16(searchLimit.Height * offsetRect.Height), Convert.ToInt16(blockSize * bitmap.VerticalResolution), 
					maxBackColorDifference, maxBackDeltaDifference, maxValidPointDistanceToMedian);

				clip = Rectangle.FromLTRB(left, top, right, bottom);

				if(clip.Size == offsetRect.Size)
					clip = offsetRect;
				else
				{
					clip.X += offsetRect.X;
					clip.Y += offsetRect.Y;
				}
			}
			
			bitmap.UnlockBits(bitmapData);
			return clip;
		}
		#endregion

		#region GetLeftBorder24bpp()
		public static short GetLeftBorder24bpp(BitmapData bitmapData, byte sweepLinesCount, short xLimit, short blockLength, 
			byte maxColorDifference, float maxDeltaDifference, short maxValidPointDistanceToMedian, short subtrackFromMinValue)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmapData.Width, bitmapData.Height);
			short		sweepLineDistance = Convert.ToInt16(bitmapData.Height / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			short		border = 0;		
			int			stride = bitmapData.Stride; 
			byte[]		threshold;
			Point		sweepPoint;

			for(int i = 0; i < sweepLinesCount; i++)
				verticalPositions[i] = Convert.ToInt16((bitmapData.Height / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(short sweepLine in verticalPositions)
				{
					threshold = GetThreshold24bpp(bitmapData, Rectangle.FromLTRB(0, sweepLine, xLimit, sweepLine + sweepLineDistance));
					threshold[0] = (threshold[0] - subtrackFromMinValue > 0) ? (byte) (threshold[0] - subtrackFromMinValue) : threshold[0];
					threshold[1] = (threshold[1] - subtrackFromMinValue > 0) ? (byte) (threshold[1] - subtrackFromMinValue) : threshold[1];
					threshold[2] = (threshold[2] - subtrackFromMinValue > 0) ? (byte) (threshold[2] - subtrackFromMinValue) : threshold[2];
					
					sweepPoint = GetSweepPointLeft24bpp(pSource, stride, sweepLine, 0, xLimit, blockLength, sweepLineDistance, threshold, maxColorDifference, maxDeltaDifference);
					
					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
				}

				RemoveWorstSweepPointsX(ref sweepPoints, .2F, bitmapRect);
				RemoveWorstSweepPointsX(ref sweepPoints, .2F, bitmapRect);
				RemovePointsWhereDistanceBiggerThanX(ref sweepPoints, maxValidPointDistanceToMedian, bitmapRect);

				if(sweepPoints.Count * 3 > sweepLinesCount)
					border = GetCenterX(sweepPoints, bitmapRect);

				border = (short) ((border > 0) ? ((border < bitmapData.Width) ? border : bitmapData.Width) : 0);
			}
			
			return border;
		}
		#endregion

		#region GetRightBorder24bpp()
		public static short GetRightBorder24bpp(BitmapData bitmapData, byte sweepLinesCount, short xLimit, short blockLength, 
			byte maxColorDifference, float maxDeltaDifference, short maxValidPointDistanceToMedian, short subtrackFromMinValue)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmapData.Width, bitmapData.Height);
			short		sweepLineDistance = Convert.ToInt16(bitmapData.Height / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			short		border = (short) bitmapData.Height;		
			int			stride = bitmapData.Stride; 
			byte[]		threshold;
			Point		sweepPoint;

			for(int i = 0; i < sweepLinesCount; i++)
				verticalPositions[i] = Convert.ToInt16((bitmapData.Height / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(short sweepLine in verticalPositions)
				{
					threshold = GetThreshold24bpp(bitmapData, Rectangle.FromLTRB(xLimit, sweepLine, bitmapData.Width, sweepLine + sweepLineDistance));
					threshold[0] = (threshold[0] - subtrackFromMinValue > 0) ? (byte) (threshold[0] - subtrackFromMinValue) : threshold[0];
					threshold[1] = (threshold[1] - subtrackFromMinValue > 0) ? (byte) (threshold[1] - subtrackFromMinValue) : threshold[1];
					threshold[2] = (threshold[2] - subtrackFromMinValue > 0) ? (byte) (threshold[2] - subtrackFromMinValue) : threshold[2];

					sweepPoint = GetSweepPointRight24bpp(pSource, stride, sweepLine, xLimit, (short) bitmapData.Width,
						blockLength, sweepLineDistance, threshold, maxColorDifference, maxDeltaDifference);
					
					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
				}

				RemoveWorstSweepPointsX(ref sweepPoints, .2F, bitmapRect);
				RemoveWorstSweepPointsX(ref sweepPoints, .2F, bitmapRect);
				RemovePointsWhereDistanceBiggerThanX(ref sweepPoints, maxValidPointDistanceToMedian, bitmapRect);

				if(sweepPoints.Count * 3 > sweepLinesCount)
					border = GetCenterX(sweepPoints, bitmapRect);

				border = (short) ((border > 0) ? ((border < bitmapData.Width) ? border : bitmapData.Width) : 0);
			}
			
			return border;
		}
		#endregion

		#region GetTopBorder24bpp()
		public static short GetTopBorder24bpp(BitmapData bitmapData, byte sweepLinesCount, short yLimit, short blockLength, 
			byte maxColorDifference, float maxDeltaDifference, short maxValidPointDistanceToMedian)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmapData.Width, bitmapData.Height);
			short		sweepLineDistance = Convert.ToInt16(bitmapData.Width / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			short		border = 0;		
			int			stride = bitmapData.Stride; 
			byte[]		threshold;
			Point		sweepPoint;

			for(int i = 0; i < sweepLinesCount; i++)
				verticalPositions[i] = Convert.ToInt16((bitmapData.Width / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(short sweepLine in verticalPositions)
				{
					threshold = GetThreshold24bpp(bitmapData, Rectangle.FromLTRB(sweepLine, 0, sweepLine + sweepLineDistance, yLimit));
					sweepPoint = GetSweepPointTop24bpp(pSource, stride, sweepLine, 0, yLimit, 
						blockLength, sweepLineDistance, threshold, maxColorDifference, maxDeltaDifference);
					
					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
				}

				RemoveWorstSweepPointsY(ref sweepPoints, .2F, bitmapRect);
				RemoveWorstSweepPointsY(ref sweepPoints, .2F, bitmapRect);
				RemovePointsWhereDistanceBiggerThanY(ref sweepPoints, maxValidPointDistanceToMedian, bitmapRect);

				if(sweepPoints.Count * 3 > sweepLinesCount)
					border = GetCenterY(sweepPoints, bitmapRect);

				border = (short) ((border > 0) ? ((border < bitmapData.Width) ? border : bitmapData.Width) : 0);
			}
			
			return border;
		}
		#endregion

		#region GetBottomBorder24bpp()
		public static short GetBottomBorder24bpp(BitmapData bitmapData, byte sweepLinesCount, short yLimit, short blockLength, 
			byte maxColorDifference, float maxDeltaDifference, short maxValidPointDistanceToMedian)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmapData.Width, bitmapData.Height);
			short		sweepLineDistance = Convert.ToInt16((bitmapData.Width) / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			short		border = (short) bitmapData.Height;		
			int			stride = bitmapData.Stride; 
			byte[]		threshold;
			Point		sweepPoint;

			for(int i = 0; i < sweepLinesCount; i++)
				verticalPositions[i] = Convert.ToInt16((bitmapData.Width / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(short sweepLine in verticalPositions)
				{
					threshold = GetThreshold24bpp(bitmapData, Rectangle.FromLTRB(sweepLine, yLimit, sweepLine + sweepLineDistance, bitmapData.Height));
					sweepPoint = GetSweepPointBottom24bpp(pSource, stride, sweepLine, yLimit, (short) bitmapData.Height,
						blockLength, sweepLineDistance, threshold, maxColorDifference, maxDeltaDifference);
					
					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
				}

				RemoveWorstSweepPointsY(ref sweepPoints, .2F, bitmapRect);
				RemoveWorstSweepPointsY(ref sweepPoints, .2F, bitmapRect);
				RemovePointsWhereDistanceBiggerThanY(ref sweepPoints, maxValidPointDistanceToMedian, bitmapRect);

				if(sweepPoints.Count * 3 > sweepLinesCount)
					border = GetCenterY(sweepPoints, bitmapRect);

				border = (short) ((border > 0) ? ((border < bitmapData.Width) ? border : bitmapData.Width) : 0);
			}
			
			return border;
		}
		#endregion
		
		#region GetLeftBorderGray()
		public static short GetLeftBorderGray(BitmapData bitmapData, byte sweepLinesCount, short xLimit, 
			short blockLength, byte maxColorDifference, float maxDeltaDifference, short maxValidPointDistanceToMedian)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmapData.Width, bitmapData.Height);
			short		sweepLineDistance = Convert.ToInt16(bitmapData.Height / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			short		border = 0;		
			int			stride = bitmapData.Stride; 
			byte		threshold;
			Point		sweepPoint;

			for(int i = 0; i < sweepLinesCount; i++)
				verticalPositions[i] = Convert.ToInt16((bitmapData.Height / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(short sweepLine in verticalPositions)
				{
					threshold = GetThresholdGray(bitmapData, Rectangle.FromLTRB(0, sweepLine, xLimit, sweepLine + sweepLineDistance));
					sweepPoint = GetSweepPointLeftGray(pSource, stride, sweepLine, 0, xLimit, blockLength, sweepLineDistance, threshold, maxColorDifference, maxDeltaDifference);
					
					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
				}

				RemoveWorstSweepPointsX(ref sweepPoints, .2F, bitmapRect);
				//RemoveWorstSweepPointsX(ref sweepPoints, .2F, bitmapRect);
				RemovePointsWhereDistanceBiggerThanX(ref sweepPoints, maxValidPointDistanceToMedian, bitmapRect);

				if(sweepPoints.Count * 3 > sweepLinesCount)
					border = GetCenterX(sweepPoints, bitmapRect);

				border = (short) ((border > 0) ? ((border < bitmapData.Width) ? border : bitmapData.Width) : 0);
			}
			
			return border;
		}
		#endregion

		#region GetRightBorderGray()
		public static short GetRightBorderGray(BitmapData bitmapData, byte sweepLinesCount, short xLimit, short blockLength,
			byte maxColorDifference, float maxDeltaDifference, short maxValidPointDistanceToMedian)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmapData.Width, bitmapData.Height);
			short		sweepLineDistance = Convert.ToInt16(bitmapData.Height / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			short		border = (short) bitmapData.Width;		
			int			stride = bitmapData.Stride; 
			byte		threshold;
			Point		sweepPoint;

			for(int i = 0; i < sweepLinesCount; i++)
				verticalPositions[i] = Convert.ToInt16((bitmapData.Height / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(short sweepLine in verticalPositions)
				{
					threshold = GetThresholdGray(bitmapData, Rectangle.FromLTRB(xLimit, sweepLine, bitmapData.Width, sweepLine + sweepLineDistance));
					sweepPoint = GetSweepPointRightGray(pSource, stride, sweepLine, xLimit, (short) bitmapData.Width,
						blockLength, sweepLineDistance, threshold, maxColorDifference, maxDeltaDifference);
					
					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
				}

				RemoveWorstSweepPointsX(ref sweepPoints, .2F, bitmapRect);
				//RemoveWorstSweepPointsX(ref sweepPoints, .2F, bitmapRect);
				RemovePointsWhereDistanceBiggerThanX(ref sweepPoints, maxValidPointDistanceToMedian, bitmapRect);

				if(sweepPoints.Count * 3 > sweepLinesCount)
					border = GetCenterX(sweepPoints, bitmapRect);

				border = (short) ((border > 0) ? ((border < bitmapData.Width) ? border : bitmapData.Width) : 0);
			}
			
			return border;
		}
		#endregion

		#region GetTopBorderGray()
		public static short GetTopBorderGray(BitmapData bitmapData, byte sweepLinesCount, short yLimit, short blockLength, 
			byte maxColorDifference, float maxDeltaDifference, short maxValidPointDistanceToMedian)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmapData.Width, bitmapData.Height);
			short		sweepLineDistance = Convert.ToInt16(bitmapData.Width / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			short		border = 0;		
			int			stride = bitmapData.Stride; 
			byte		threshold;
			Point		sweepPoint;

			for(int i = 0; i < sweepLinesCount; i++)
				verticalPositions[i] = Convert.ToInt16((bitmapData.Width / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(short sweepLine in verticalPositions)
				{
					threshold = GetThresholdGray(bitmapData, Rectangle.FromLTRB(sweepLine, 0, sweepLine + sweepLineDistance, yLimit));
					sweepPoint = GetSweepPointTopGray(pSource, stride, sweepLine, 0, yLimit, 
						blockLength, sweepLineDistance, threshold, maxColorDifference, maxDeltaDifference);
					
					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
				}

				RemoveWorstSweepPointsY(ref sweepPoints, .2F, bitmapRect);
				RemoveWorstSweepPointsY(ref sweepPoints, .2F, bitmapRect);
				RemovePointsWhereDistanceBiggerThanY(ref sweepPoints, maxValidPointDistanceToMedian, bitmapRect);

				if(sweepPoints.Count * 3 > sweepLinesCount)
					border = GetCenterY(sweepPoints, bitmapRect);

				border = (short) ((border > 0) ? ((border < bitmapData.Height) ? border : bitmapData.Height) : 0);
			}
			
			return border;
		}
		#endregion

		#region GetBottomBorderGray()
		public static short GetBottomBorderGray(BitmapData bitmapData, byte sweepLinesCount, short yLimit, short blockLength, 
			byte maxColorDifference, float maxDeltaDifference, short maxValidPointDistanceToMedian)
		{
			Rectangle	bitmapRect = new Rectangle(0, 0, bitmapData.Width, bitmapData.Height);
			short		sweepLineDistance = Convert.ToInt16(bitmapData.Width / (float) sweepLinesCount);
			short[]		verticalPositions = new short[sweepLinesCount];
			short		border = (short) bitmapData.Height;		
			int			stride = bitmapData.Stride; 
			byte		threshold;
			Point		sweepPoint;

			for(int i = 0; i < sweepLinesCount; i++)
				verticalPositions[i] = Convert.ToInt16((bitmapData.Width / (float) sweepLinesCount) * i);
						 
			unsafe
			{
				byte*		pSource = (byte*)bitmapData.Scan0.ToPointer(); 
				ArrayList	sweepPoints = new ArrayList();

				foreach(short sweepLine in verticalPositions)
				{
					threshold = GetThresholdGray(bitmapData, Rectangle.FromLTRB(sweepLine, yLimit, sweepLine + sweepLineDistance, bitmapData.Height));
					sweepPoint = GetSweepPointBottomGray(pSource, stride, sweepLine, yLimit, (short) bitmapData.Height, 
						blockLength, sweepLineDistance, threshold, maxColorDifference, maxDeltaDifference);
					
					if(!sweepPoint.IsEmpty)
						sweepPoints.Add(sweepPoint);
				}

				RemoveWorstSweepPointsY(ref sweepPoints, .2F, bitmapRect);
				RemoveWorstSweepPointsY(ref sweepPoints, .2F, bitmapRect);
				RemovePointsWhereDistanceBiggerThanY(ref sweepPoints, maxValidPointDistanceToMedian, bitmapRect);

				if(sweepPoints.Count * 3 > sweepLinesCount)
					border = GetCenterY(sweepPoints, bitmapRect);

				border = (short) ((border > 0) ? ((border < bitmapData.Height) ? border : bitmapData.Height) : 0);
			}
			
			return border;
		}
		#endregion
				
		#region GetSweepPointLeft24bpp()
		private static unsafe Point GetSweepPointLeft24bpp(byte* pOrig, int stride, short y, short xFrom, short xTo, 
			short blockWidth, short blockHeight, byte[] threshold, byte maxColorDifference, float maxDeltaDifference)
		{
			byte			*pCurrent;
			short			x, i;

			ArrayList		sweepAreas = new ArrayList();
			SweepAreaColor	area1, area2, area3;
			byte[]			minValue = new byte[3];
			float[]			averageValue = new float[3];
			float[]			averageDelta = new float[3];
			short			jump = Convert.ToInt16(blockWidth / 3);
			
			pCurrent = pOrig + y * stride + xFrom * 3;
			GetColors(pCurrent, stride, jump, blockHeight, ref minValue, ref averageValue, ref averageDelta);
			area1 = new SweepAreaColor(xFrom, averageValue, minValue, averageDelta);
			pCurrent += jump * 3;

			GetColors(pCurrent, stride, jump, blockHeight, ref minValue, ref averageValue, ref averageDelta);
			area2 = new SweepAreaColor((short) (xFrom + jump), averageValue, minValue, averageDelta);
			pCurrent += jump * 3;

			for(x = (short) (xFrom + 2 * jump); x < (xTo - xFrom); x += jump)
			{
				GetColors(pCurrent, stride, jump, blockHeight, ref minValue, ref averageValue, ref averageDelta);
				area3 = new SweepAreaColor(x, averageValue, minValue, averageDelta);

				if(area1.MinColor[0] >= threshold[0] && area1.MinColor[1] >= threshold[1] && area1.MinColor[2] >= threshold[2] &&
					area2.MinColor[0] >= threshold[0] && area2.MinColor[1] >= threshold[1] && area2.MinColor[2] >= threshold[2] &&
					area3.MinColor[0] >= threshold[0] && area3.MinColor[1] >= threshold[1] && area3.MinColor[2] >= threshold[2])
				{
					averageValue = new float[] {(area1.AverageColor[0] + area2.AverageColor[0] + area3.AverageColor[0]) / 3, (area1.AverageColor[1] + area2.AverageColor[1] + area3.AverageColor[1]) / 3, (area1.AverageColor[2] + area2.AverageColor[2] + area3.AverageColor[2]) / 3};
					averageDelta = new float[] {(area1.AverageDelta[0] + area2.AverageDelta[0] + area3.AverageDelta[0]) / 3, (area1.AverageDelta[1] + area2.AverageDelta[1] + area3.AverageDelta[1]) / 3, (area1.AverageDelta[2] + area2.AverageDelta[2] + area3.AverageDelta[2]) / 3};

					sweepAreas.Add(new SweepAreaColor(area1.Dimension, 
						averageValue,
						area1.MinColor,
						averageDelta));	
				}
					
				area1 = area2;
				area2 = area3;
				pCurrent += jump * 3;
			}
			
			for(i = 0; i < 3; i++)
			{
				averageValue[i] = 0;
				averageDelta[i] = 255;
			
				foreach(SweepAreaColor sweepArea in sweepAreas)
				{
					if(averageValue[i] < sweepArea.AverageColor[i])
						averageValue[i] = sweepArea.AverageColor[i];
					if(averageDelta[i] > sweepArea.AverageDelta[i])
						averageDelta[i] = sweepArea.AverageDelta[i];
				}

				if(averageDelta[i] < .3)
					averageDelta[i] = .3F;
			}
			
			foreach(SweepAreaColor sweepArea in sweepAreas)
			{
				if(((averageValue[0] - sweepArea.AverageColor[0] > maxColorDifference) || (sweepArea.AverageDelta[0] / averageDelta[0] > maxDeltaDifference)) )
					sweepArea.Reset();
				else if(((averageValue[1] - sweepArea.AverageColor[1] > maxColorDifference) || (sweepArea.AverageDelta[1] / averageDelta[1] > maxDeltaDifference)) )
					sweepArea.Reset();
				else if(((averageValue[2] - sweepArea.AverageColor[2] > maxColorDifference) || (sweepArea.AverageDelta[2] / averageDelta[2] > maxDeltaDifference)) )
					sweepArea.Reset();
			}

			foreach(SweepAreaColor sweepArea in sweepAreas)
				if(sweepArea.AverageColor[0] > 0 && sweepArea.AverageDelta[0] >= 0)
					return new Point(sweepArea.Dimension, y);

			if(maxColorDifference >= 15)
				return new Point(xFrom, y);
			else
				return Point.Empty;
		}
		#endregion
				
		#region GetSweepPointRight24bpp()
		private static unsafe Point GetSweepPointRight24bpp(byte* pOrig, int stride, short y, short xFrom, short xTo, 
			short blockWidth, short blockHeight, byte[] threshold, byte maxColorDifference, float maxDeltaDifference)
		{
			byte			*pCurrent;
			short			x, i;

			ArrayList		sweepAreas = new ArrayList();
			SweepAreaColor	area1, area2, area3;
			byte[]			minValue = new byte[3];
			float[]			averageValue = new float[3];
			float[]			averageDelta = new float[3];
			short			jump = Convert.ToInt16(blockWidth / 3);
			
			pCurrent = pOrig + y * stride + (xTo - jump - 1) * 3;
			GetColors(pCurrent, stride, jump, blockHeight, ref minValue, ref averageValue, ref averageDelta);
			area1 = new SweepAreaColor(xTo, averageValue, minValue, averageDelta);
			pCurrent -= jump * 3;

			GetColors(pCurrent, stride, jump, blockHeight, ref minValue, ref averageValue, ref averageDelta);
			area2 = new SweepAreaColor((short) (xTo - jump), averageValue, minValue, averageDelta);
			pCurrent -= jump * 3;

			for(x = (short) (xTo - 2 * jump); x > (xFrom - jump); x -= jump)
			{
				GetColors(pCurrent, stride, jump, blockHeight, ref minValue, ref averageValue, ref averageDelta);
				area3 = new SweepAreaColor(x, averageValue, minValue, averageDelta);

				if(area1.MinColor[0] >= threshold[0] && area1.MinColor[1] >= threshold[1] && area1.MinColor[2] >= threshold[2] &&
					area2.MinColor[0] >= threshold[0] && area2.MinColor[1] >= threshold[1] && area2.MinColor[2] >= threshold[2] &&
					area3.MinColor[0] >= threshold[0] && area3.MinColor[1] >= threshold[1] && area3.MinColor[2] >= threshold[2])
				{
					averageValue = new float[] {(area1.AverageColor[0] + area2.AverageColor[0] + area3.AverageColor[0]) / 3, (area1.AverageColor[1] + area2.AverageColor[1] + area3.AverageColor[1]) / 3, (area1.AverageColor[2] + area2.AverageColor[2] + area3.AverageColor[2]) / 3};
					averageDelta = new float[] {(area1.AverageDelta[0] + area2.AverageDelta[0] + area3.AverageDelta[0]) / 3, (area1.AverageDelta[1] + area2.AverageDelta[1] + area3.AverageDelta[1]) / 3, (area1.AverageDelta[2] + area2.AverageDelta[2] + area3.AverageDelta[2]) / 3};

					sweepAreas.Add(new SweepAreaColor(area1.Dimension, 
						averageValue,
						area1.MinColor,
						averageDelta));	
				}
					
				area1 = area2;
				area2 = area3;
				pCurrent -= jump * 3;
			}
			
			for(i = 0; i < 3; i++)
			{
				averageValue[i] = 0;
				averageDelta[i] = 255;
			
				foreach(SweepAreaColor sweepArea in sweepAreas)
				{
					if(averageValue[i] < sweepArea.AverageColor[i])
						averageValue[i] = sweepArea.AverageColor[i];
					if(averageDelta[i] > sweepArea.AverageDelta[i])
						averageDelta[i] = sweepArea.AverageDelta[i];
				}

				if(averageDelta[i] < .3)
					averageDelta[i] = .3F;
			}
			
			foreach(SweepAreaColor sweepArea in sweepAreas)
			{
				if(((averageValue[0] - sweepArea.AverageColor[0] > maxColorDifference) || (sweepArea.AverageDelta[0] / averageDelta[0] > maxDeltaDifference)) )
					sweepArea.Reset();
				else if(((averageValue[1] - sweepArea.AverageColor[1] > maxColorDifference) || (sweepArea.AverageDelta[1] / averageDelta[1] > maxDeltaDifference)) )
					sweepArea.Reset();
				else if(((averageValue[2] - sweepArea.AverageColor[2] > maxColorDifference) || (sweepArea.AverageDelta[2] / averageDelta[2] > maxDeltaDifference)) )
					sweepArea.Reset();
			}

			foreach(SweepAreaColor sweepArea in sweepAreas)
				if(sweepArea.AverageColor[0] > 0 && sweepArea.AverageDelta[0] >= 0)
					return new Point(sweepArea.Dimension, y);

			if(maxColorDifference >= 15)
				return new Point(xTo, y);
			else
				return Point.Empty;
		}
		#endregion
		
		#region GetSweepPointTop24bpp()
		private static unsafe Point GetSweepPointTop24bpp(byte* pOrig, int stride, short x, short yFrom, short yTo, 
			short blockWidth, short blockHeight, byte[] threshold, byte maxColorDifference, float maxDeltaDifference)
		{
			byte			*pCurrent;
			short			y, i;

			ArrayList		sweepAreas = new ArrayList();
			SweepAreaColor	area1, area2, area3;
			byte[]			minValue = new byte[3];
			float[]			averageValue = new float[3];
			float[]			averageDelta = new float[3];
			short			jump = Convert.ToInt16(blockHeight / 3);
			
			pCurrent = pOrig + yFrom * stride + x * 3;
			GetColors(pCurrent, stride, blockWidth, jump, ref minValue, ref averageValue, ref averageDelta);
			area1 = new SweepAreaColor(yFrom, averageValue, minValue, averageDelta);
			pCurrent += jump * stride;

			GetColors(pCurrent, stride, blockWidth, jump, ref minValue, ref averageValue, ref averageDelta);
			area2 = new SweepAreaColor((short) (yFrom + jump), averageValue, minValue, averageDelta);
			pCurrent += jump * stride;

			for(y = (short) (yFrom + 2 * jump); y < (yTo - jump); y += jump)
			{
				GetColors(pCurrent, stride, blockWidth, jump, ref minValue, ref averageValue, ref averageDelta);
				area3 = new SweepAreaColor(y, averageValue, minValue, averageDelta);

				if(area1.MinColor[0] >= threshold[0] && area1.MinColor[1] >= threshold[1] && area1.MinColor[2] >= threshold[2] &&
					area2.MinColor[0] >= threshold[0] && area2.MinColor[1] >= threshold[1] && area2.MinColor[2] >= threshold[2] &&
					area3.MinColor[0] >= threshold[0] && area3.MinColor[1] >= threshold[1] && area3.MinColor[2] >= threshold[2])
				{
					averageValue = new float[] {(area1.AverageColor[0] + area2.AverageColor[0] + area3.AverageColor[0]) / 3, (area1.AverageColor[1] + area2.AverageColor[1] + area3.AverageColor[1]) / 3, (area1.AverageColor[2] + area2.AverageColor[2] + area3.AverageColor[2]) / 3};
					averageDelta = new float[] {(area1.AverageDelta[0] + area2.AverageDelta[0] + area3.AverageDelta[0]) / 3, (area1.AverageDelta[1] + area2.AverageDelta[1] + area3.AverageDelta[1]) / 3, (area1.AverageDelta[2] + area2.AverageDelta[2] + area3.AverageDelta[2]) / 3};

					sweepAreas.Add(new SweepAreaColor(area1.Dimension, 
						averageValue,
						area1.MinColor,
						averageDelta));	
				}
					
				area1 = area2;
				area2 = area3;
				pCurrent += jump * stride;
			}
			
			for(i = 0; i < 3; i++)
			{
				averageValue[i] = 0;
				averageDelta[i] = 255;
			
				foreach(SweepAreaColor sweepArea in sweepAreas)
				{
					if(averageValue[i] < sweepArea.AverageColor[i])
						averageValue[i] = sweepArea.AverageColor[i];
					if(averageDelta[i] > sweepArea.AverageDelta[i])
						averageDelta[i] = sweepArea.AverageDelta[i];
				}

				if(averageDelta[i] < .3)
					averageDelta[i] = .3F;
			}
			
			foreach(SweepAreaColor sweepArea in sweepAreas)
			{
				if(((averageValue[0] - sweepArea.AverageColor[0] > maxColorDifference) || (sweepArea.AverageDelta[0] / averageDelta[0] > maxDeltaDifference)) )
					sweepArea.Reset();
				else if(((averageValue[1] - sweepArea.AverageColor[1] > maxColorDifference) || (sweepArea.AverageDelta[1] / averageDelta[1] > maxDeltaDifference)) )
					sweepArea.Reset();
				else if(((averageValue[2] - sweepArea.AverageColor[2] > maxColorDifference) || (sweepArea.AverageDelta[2] / averageDelta[2] > maxDeltaDifference)) )
					sweepArea.Reset();
			}

			foreach(SweepAreaColor sweepArea in sweepAreas)
				if(sweepArea.AverageColor[0] > 0 && sweepArea.AverageDelta[0] >= 0)
					return new Point(x, sweepArea.Dimension);

			return new Point(x, yFrom);
		}
		#endregion
		
		#region GetSweepPointBottom24bpp()
		private static unsafe Point GetSweepPointBottom24bpp(byte* pOrig, int stride, short x, short yFrom, short yTo, 
			short blockWidth, short blockHeight, byte[] threshold, byte maxColorDifference, float maxDeltaDifference)
		{
			byte			*pCurrent;
			short			y, i;

			ArrayList		sweepAreas = new ArrayList();
			SweepAreaColor	area1, area2, area3;
			byte[]			minValue = new byte[3];
			float[]			averageValue = new float[3];
			float[]			averageDelta = new float[3];
			short			jump = Convert.ToInt16(blockHeight / 3);
			
			pCurrent = pOrig + (yTo - jump - 1) * stride + x * 3;
			GetColors(pCurrent, stride, blockWidth, jump, ref minValue, ref averageValue, ref averageDelta);
			area1 = new SweepAreaColor(yTo, averageValue, minValue, averageDelta);
			pCurrent -= jump * stride;

			GetColors(pCurrent, stride, blockWidth, jump, ref minValue, ref averageValue, ref averageDelta);
			area2 = new SweepAreaColor((short) (yTo - jump), averageValue, minValue, averageDelta);
			pCurrent -= jump * stride;

			for(y = (short) (yTo - 2 * jump); y > (yFrom + jump); y -= jump)
			{
				GetColors(pCurrent, stride, blockWidth, jump, ref minValue, ref averageValue, ref averageDelta);
				area3 = new SweepAreaColor(y, averageValue, minValue, averageDelta);

				if(area1.MinColor[0] >= threshold[0] && area1.MinColor[1] >= threshold[1] && area1.MinColor[2] >= threshold[2] &&
					area2.MinColor[0] >= threshold[0] && area2.MinColor[1] >= threshold[1] && area2.MinColor[2] >= threshold[2] &&
					area3.MinColor[0] >= threshold[0] && area3.MinColor[1] >= threshold[1] && area3.MinColor[2] >= threshold[2])
				{
					averageValue = new float[] {(area1.AverageColor[0] + area2.AverageColor[0] + area3.AverageColor[0]) / 3, (area1.AverageColor[1] + area2.AverageColor[1] + area3.AverageColor[1]) / 3, (area1.AverageColor[2] + area2.AverageColor[2] + area3.AverageColor[2]) / 3};
					averageDelta = new float[] {(area1.AverageDelta[0] + area2.AverageDelta[0] + area3.AverageDelta[0]) / 3, (area1.AverageDelta[1] + area2.AverageDelta[1] + area3.AverageDelta[1]) / 3, (area1.AverageDelta[2] + area2.AverageDelta[2] + area3.AverageDelta[2]) / 3};

					sweepAreas.Add(new SweepAreaColor(area1.Dimension, 
						averageValue,
						area1.MinColor,
						averageDelta));	
				}
					
				area1 = area2;
				area2 = area3;
				pCurrent -= jump * stride;
			}
			
			for(i = 0; i < 3; i++)
			{
				averageValue[i] = 0;
				averageDelta[i] = 255;
			
				foreach(SweepAreaColor sweepArea in sweepAreas)
				{
					if(averageValue[i] < sweepArea.AverageColor[i])
						averageValue[i] = sweepArea.AverageColor[i];
					if(averageDelta[i] > sweepArea.AverageDelta[i])
						averageDelta[i] = sweepArea.AverageDelta[i];
				}

				if(averageDelta[i] < .3)
					averageDelta[i] = .3F;
			}
			
			foreach(SweepAreaColor sweepArea in sweepAreas)
			{
				if(((averageValue[0] - sweepArea.AverageColor[0] > maxColorDifference) || (sweepArea.AverageDelta[0] / averageDelta[0] > maxDeltaDifference)) )
					sweepArea.Reset();
				else if(((averageValue[1] - sweepArea.AverageColor[1] > maxColorDifference) || (sweepArea.AverageDelta[1] / averageDelta[1] > maxDeltaDifference)) )
					sweepArea.Reset();
				else if(((averageValue[2] - sweepArea.AverageColor[2] > maxColorDifference) || (sweepArea.AverageDelta[2] / averageDelta[2] > maxDeltaDifference)) )
					sweepArea.Reset();
			}

			foreach(SweepAreaColor sweepArea in sweepAreas)
				if(sweepArea.AverageColor[0] > 0 && sweepArea.AverageDelta[0] >= 0)
					return new Point(x, sweepArea.Dimension);

			return new Point(x, yTo);
		}
		#endregion
		
		#region GetSweepPointLeftGray()
		private static unsafe Point GetSweepPointLeftGray(byte* pOrig, int stride, short y, short xFrom, short xTo, 
			short blockWidth, short blockHeight, byte threshold, byte maxColorDifference, float maxDeltaDifference)
		{
			byte			*pCurrent;
			short			x;

			ArrayList		sweepAreas = new ArrayList();
			ArrayList		sweepAreas2 = new ArrayList();
			byte			minValue;
			float			averageValue;
			float			averageDelta;
			short			jump = Convert.ToInt16(blockWidth / 3);
			
			pCurrent = pOrig + y * stride + xFrom;

			for(x = xFrom; x < (xTo - xFrom); x += jump)
			{
				GetGrays(pCurrent, stride, jump, blockHeight, out minValue, out averageValue, out averageDelta);

				sweepAreas2.Add(new SweepArea(x, averageValue, minValue, averageDelta));

				pCurrent += jump;
			}
			
			for(short j = 0; j < sweepAreas2.Count - 2; j++)
			{				
				if(((SweepArea)sweepAreas2[j]).MinColor >= threshold && ((SweepArea)sweepAreas2[j+1]).MinColor >= threshold && ((SweepArea)sweepAreas2[j+2]).MinColor >= threshold)
					sweepAreas.Add(new SweepArea( ((SweepArea)sweepAreas2[j]).Dimension, 
						(((SweepArea)sweepAreas2[j]).AverageColor+ ((SweepArea)sweepAreas2[j+1]).AverageColor + ((SweepArea)sweepAreas2[j+2]).AverageColor) / 3, 
						Math.Min( ((SweepArea)sweepAreas2[j]).MinColor, Math.Min( ((SweepArea)sweepAreas2[j+1]).MinColor, ((SweepArea)sweepAreas2[j+2]).MinColor)), 
						(((SweepArea)sweepAreas2[j]).AverageDelta + ((SweepArea)sweepAreas2[j+1]).AverageDelta + ((SweepArea)sweepAreas2[j+2]).AverageDelta) / 3));
			}

			averageValue = 0;
			averageDelta = 255;
			
			foreach(SweepArea sweepArea in sweepAreas)
			{
				if(averageValue < sweepArea.AverageColor)
					averageValue = sweepArea.AverageColor;
				if(averageDelta > sweepArea.AverageDelta)
					averageDelta = sweepArea.AverageDelta;
			}

			if(averageDelta < .3)
				averageDelta = .3F;
			
			foreach(SweepArea sweepArea in sweepAreas)
				if(((averageValue - sweepArea.AverageColor > maxColorDifference) || (sweepArea.AverageDelta / averageDelta > maxDeltaDifference)) )
					sweepArea.Reset();

			foreach(SweepArea sweepArea in sweepAreas)
				if(sweepArea.AverageColor > 0 && sweepArea.AverageDelta >= 0)
					return new Point(sweepArea.Dimension, y);

			return Point.Empty;
		}
		#endregion
				
		#region GetSweepPointRightGray()
		private static unsafe Point GetSweepPointRightGray(byte* pOrig, int stride, short y, short xFrom, short xTo,
			short blockWidth, short blockHeight, byte threshold, byte maxColorDifference, float maxDeltaDifference)
		{
			byte			*pCurrent;
			short			x;

			ArrayList		sweepAreas = new ArrayList();
			ArrayList		sweepAreas2 = new ArrayList();
			ArrayList		sweepAreas3 = new ArrayList();
			byte			minValue;
			float			averageValue;
			float			averageDelta;
			short			jump = Convert.ToInt16(blockWidth / 3);
			
			pCurrent = pOrig + y * stride + xTo - jump - 1;

			for(x = xTo; x > xFrom + blockWidth; x -= jump)
			{
				GetGrays(pCurrent, stride, jump, blockHeight, out minValue, out averageValue, out averageDelta);

				sweepAreas2.Add(new SweepArea(x, averageValue, minValue, averageDelta));

				pCurrent -= jump;
			}
			
			for(short j = 0; j < sweepAreas2.Count - 2; j++)
			{				
				if(((SweepArea)sweepAreas2[j]).MinColor >= threshold && ((SweepArea)sweepAreas2[j+1]).MinColor >= threshold && ((SweepArea)sweepAreas2[j+2]).MinColor >= threshold)
					sweepAreas.Add(new SweepArea( ((SweepArea)sweepAreas2[j]).Dimension, 
						(((SweepArea)sweepAreas2[j]).AverageColor+ ((SweepArea)sweepAreas2[j+1]).AverageColor + ((SweepArea)sweepAreas2[j+2]).AverageColor) / 3, 
						Math.Min( ((SweepArea)sweepAreas2[j]).MinColor, Math.Min( ((SweepArea)sweepAreas2[j+1]).MinColor, ((SweepArea)sweepAreas2[j+2]).MinColor)), 
						(((SweepArea)sweepAreas2[j]).AverageDelta + ((SweepArea)sweepAreas2[j+1]).AverageDelta + ((SweepArea)sweepAreas2[j+2]).AverageDelta) / 3));
			}

			averageValue = 0;
			averageDelta = 255;
			
			foreach(SweepArea sweepArea in sweepAreas)
			{
				if(averageValue < sweepArea.AverageColor)
					averageValue = sweepArea.AverageColor;
				if(averageDelta > sweepArea.AverageDelta)
					averageDelta = sweepArea.AverageDelta;
			}

			if(averageDelta < .3)
				averageDelta = .3F;
			
			foreach(SweepArea sweepArea in sweepAreas)
				if(((averageValue - sweepArea.AverageColor > maxColorDifference) || (sweepArea.AverageDelta / averageDelta > maxDeltaDifference)) )
					sweepArea.Reset();

			foreach(SweepArea sweepArea in sweepAreas)
				if(sweepArea.AverageColor > 0 && sweepArea.AverageDelta >= 0)
					return new Point(sweepArea.Dimension, y);

			return Point.Empty;
		}
		#endregion
				
		#region GetSweepPointTopGray()
		private static unsafe Point GetSweepPointTopGray(byte* pOrig, int stride, short x, short yFrom, short yTo,
			short blockWidth, short blockHeight, byte threshold, byte maxColorDifference, float maxDeltaDifference)
		{
			byte			*pCurrent;
			short			y;

			ArrayList		sweepAreas = new ArrayList();
			ArrayList		sweepAreas2 = new ArrayList();
			byte			minValue;
			float			averageValue;
			float			averageDelta;
			short			jump = Convert.ToInt16(blockHeight / 3);
			

			pCurrent = pOrig + yFrom * stride + x;

			for(y = yFrom; y < yTo - blockHeight; y += jump)
			{
				GetGrays(pCurrent, stride, blockWidth, jump, out minValue, out averageValue, out averageDelta);

				sweepAreas2.Add(new SweepArea(y, averageValue, minValue, averageDelta));

				pCurrent += jump * stride;
			}
			
			for(short j = 0; j < sweepAreas2.Count - 2; j++)
			{				
				if(((SweepArea)sweepAreas2[j]).MinColor >= threshold && ((SweepArea)sweepAreas2[j+1]).MinColor >= threshold && ((SweepArea)sweepAreas2[j+2]).MinColor >= threshold)
					sweepAreas.Add(new SweepArea( ((SweepArea)sweepAreas2[j]).Dimension, 
						(((SweepArea)sweepAreas2[j]).AverageColor+ ((SweepArea)sweepAreas2[j+1]).AverageColor + ((SweepArea)sweepAreas2[j+2]).AverageColor) / 3, 
						Math.Min( ((SweepArea)sweepAreas2[j]).MinColor, Math.Min( ((SweepArea)sweepAreas2[j+1]).MinColor, ((SweepArea)sweepAreas2[j+2]).MinColor)), 
						(((SweepArea)sweepAreas2[j]).AverageDelta + ((SweepArea)sweepAreas2[j+1]).AverageDelta + ((SweepArea)sweepAreas2[j+2]).AverageDelta) / 3));
			}

			averageValue = 0;
			averageDelta = 255;
			
			foreach(SweepArea sweepArea in sweepAreas)
			{
				if(averageValue < sweepArea.AverageColor)
					averageValue = sweepArea.AverageColor;
				if(averageDelta > sweepArea.AverageDelta)
					averageDelta = sweepArea.AverageDelta;
			}

			if(averageDelta < .3)
				averageDelta = .3F;
			
			foreach(SweepArea sweepArea in sweepAreas)
				if(((averageValue - sweepArea.AverageColor > maxColorDifference) || (sweepArea.AverageDelta / averageDelta > maxDeltaDifference)) )
					sweepArea.Reset();

			foreach(SweepArea sweepArea in sweepAreas)
				if(sweepArea.AverageColor > 0 && sweepArea.AverageDelta >= 0)
					return new Point(x, sweepArea.Dimension);

			return Point.Empty;
		}
		#endregion
				
		#region GetSweepPointBottomGray()
		private static unsafe Point GetSweepPointBottomGray(byte* pOrig, int stride, short x, short yFrom, short yTo,
			short blockWidth, short blockHeight, byte threshold, byte maxColorDifference, float maxDeltaDifference)
		{
			byte			*pCurrent;
			short			y;

			ArrayList		sweepAreas = new ArrayList();
			ArrayList		sweepAreas2 = new ArrayList();
			byte			minValue;
			float			averageValue;
			float			averageDelta;
			short			jump = Convert.ToInt16(blockHeight / 3);
			
			pCurrent = pOrig + (yTo - jump - 1) * stride + x;

			for(y = yTo; y > yFrom + blockHeight; y -= jump)
			{
				GetGrays(pCurrent, stride, blockWidth, jump, out minValue, out averageValue, out averageDelta);

				sweepAreas2.Add(new SweepArea(y, averageValue, minValue, averageDelta));

				pCurrent -= jump * stride;
			}
			
			for(short j = 0; j < sweepAreas2.Count - 2; j++)
			{				
				if(((SweepArea)sweepAreas2[j]).MinColor >= threshold && ((SweepArea)sweepAreas2[j+1]).MinColor >= threshold && ((SweepArea)sweepAreas2[j+2]).MinColor >= threshold)
					sweepAreas.Add(new SweepArea( ((SweepArea)sweepAreas2[j]).Dimension, 
						(((SweepArea)sweepAreas2[j]).AverageColor+ ((SweepArea)sweepAreas2[j+1]).AverageColor + ((SweepArea)sweepAreas2[j+2]).AverageColor) / 3, 
						Math.Min( ((SweepArea)sweepAreas2[j]).MinColor, Math.Min( ((SweepArea)sweepAreas2[j+1]).MinColor, ((SweepArea)sweepAreas2[j+2]).MinColor)), 
						(((SweepArea)sweepAreas2[j]).AverageDelta + ((SweepArea)sweepAreas2[j+1]).AverageDelta + ((SweepArea)sweepAreas2[j+2]).AverageDelta) / 3));
			}
			
			averageValue = 0;
			averageDelta = 255;
			
			foreach(SweepArea sweepArea in sweepAreas)
			{
				if(averageValue < sweepArea.AverageColor)
					averageValue = sweepArea.AverageColor;
				if(averageDelta > sweepArea.AverageDelta)
					averageDelta = sweepArea.AverageDelta;
			}

			if(averageDelta < .3)
				averageDelta = .3F;
			
			foreach(SweepArea sweepArea in sweepAreas)
				if(((averageValue - sweepArea.AverageColor > maxColorDifference) || (sweepArea.AverageDelta / averageDelta > maxDeltaDifference)) )
					sweepArea.Reset();

			foreach(SweepArea sweepArea in sweepAreas)
				if(sweepArea.AverageColor > 0 && sweepArea.AverageDelta >= 0)
					return new Point(x, sweepArea.Dimension);

			return Point.Empty;
		}
		#endregion
		
		#region GetColors()
		private static unsafe void GetColors(byte* pOrig, int stride, short columns, short rows, ref byte[] minValue, ref float[] averageValue, ref float[] averageDelta)
		{
			byte		color;
			byte*		pCurrent = pOrig;

			for(byte i = 0; i < 3; i++)
			{
				minValue[i] = 255;
				averageValue[i] = 0;
				averageDelta[i] = 0;
			}

			for(int y = 0; y < rows; y++)
			{
				pCurrent = pOrig + y * stride;
				
				for(int x = 0; x < columns; x++)
				{				
					color = *(pCurrent++);

					averageValue[2] += color;
				
					if(minValue[2] > color)
						minValue[2] = color;

					color = *(pCurrent++);

					averageValue[1] += color;
				
					if(minValue[1] > color)
						minValue[1] = color;
				
					color = *(pCurrent++);

					averageValue[0] += color;
				
					if(minValue[0] > color)
						minValue[0] = color;
				}
			}
			
			for(int y = 0; y < rows; y++)
			{
				pCurrent = pOrig + y * stride;
				
				for(int x = 0; x < columns - 1; x++)
				{				
					averageDelta[2] += (byte) ((pCurrent[0] - pCurrent[3]) * (pCurrent[0] - pCurrent[3]));
					averageDelta[1] += (byte) ((pCurrent[1] - pCurrent[4]) * (pCurrent[1] - pCurrent[4]));
					averageDelta[0] += (byte) ((pCurrent[2] - pCurrent[5]) * (pCurrent[2] - pCurrent[5]));
					pCurrent += 3;
				}
			}

			for(byte i = 0; i < 3; i++)
			{
				minValue[i] = (minValue[i] + 4 > 255) ? minValue[i] : (byte) (minValue[i] + 4);
				averageValue[i] = averageValue[i] / (float) (rows * columns);
				averageDelta[i] = averageDelta[i] / (float) (rows * (columns - 1));
			}
		}
		#endregion
		
		#region GetGrays()
		private static unsafe void GetGrays(byte* pOrig, int stride, short columns, short rows, out byte minValue, out float averageValue, out float averageDelta)
		{
			byte		color;
			byte*		pCurrent = pOrig;
			int[]		array = new int[256];

			minValue = 255;
			averageValue = 0;
			averageDelta = 0;

			for(int y = 0; y < rows; y++)
			{
				pCurrent = pOrig + y * stride;
				
				for(int x = 0; x < columns; x++)
				{				
					color = *(pCurrent++);
					array[color]++;

					averageValue += color;
				
					if(minValue > color)
						minValue = color;
				}
			}
			
			for(int y = 0; y < rows; y++)
			{
				pCurrent = pOrig + y * stride;
				
				for(int x = 0; x < columns - 1; x++)
				{				
					averageDelta += (byte) ((pCurrent[0] - pCurrent[1]) * (pCurrent[0] - pCurrent[1]));
					pCurrent++;
				}
			}

			byte	maxIndex = 0;
			for(short i = 1; i < 256; i++)
				if(array[maxIndex] < array[i])
					maxIndex = (byte) i;

			for(short i = 1; i < maxIndex; i++)
				if(array[i] * 50 > array[maxIndex])
				{
					minValue = (byte) i;
					break;
				}

			averageValue = averageValue / (float) (rows * columns);
			averageDelta = averageDelta / (float) (rows * (columns - 1));
		}
		#endregion
		
		#region RemoveWorstSweepPointsX()
		private static void RemoveWorstSweepPointsX(ref ArrayList sweepPoints, float percentsToRemove, Rectangle imageRect)
		{
			if(sweepPoints.Count > Convert.ToInt32(sweepPoints.Count * (1 - percentsToRemove)))
			{
				long			center = GetCenterX(sweepPoints, imageRect);			
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

		#region RemoveWorstSweepPointsY()
		private static void RemoveWorstSweepPointsY(ref ArrayList sweepPoints, float percentsToRemove, Rectangle imageRect)
		{
			if(sweepPoints.Count > Convert.ToInt32(sweepPoints.Count * (1 - percentsToRemove)))
			{
				long			center = GetCenterY(sweepPoints, imageRect);			
				ArrayList		distancesList = new ArrayList();

				foreach(Point sweepPoint in sweepPoints)
					distancesList.Add((short) Math.Abs(center - sweepPoint.Y));

				distancesList.Sort();
				short		breakDistance = (short) distancesList[Convert.ToInt32(sweepPoints.Count * (1 - percentsToRemove))];

				for(int i = sweepPoints.Count - 1; i >= 0; i--)
					if( (((Point)sweepPoints[i]).Y - center > breakDistance) || (((Point)sweepPoints[i]).Y - center < -breakDistance) )
						sweepPoints.RemoveAt(i);
			}
		}
		#endregion

		#region RemovePointsWhereDistanceBiggerThanX()
		private static void RemovePointsWhereDistanceBiggerThanX(ref ArrayList sweepPoints, short distance, Rectangle imageRect)
		{
			if(sweepPoints.Count > 0)
			{
				long			center = GetCenterX(sweepPoints, imageRect);			
			
				for(int i = sweepPoints.Count - 1; i >= 0; i--)
					if( (((Point)sweepPoints[i]).X - center > distance) || (((Point)sweepPoints[i]).X - center < -distance) )
						sweepPoints.RemoveAt(i);
			}
		}
		#endregion

		#region RemovePointsWhereDistanceBiggerThanY()
		private static void RemovePointsWhereDistanceBiggerThanY(ref ArrayList sweepPoints, short distance, Rectangle imageRect)
		{
			if(sweepPoints.Count > 0)
			{
				long			center = GetCenterY(sweepPoints, imageRect);			
			
				for(int i = sweepPoints.Count - 1; i >= 0; i--)
					if( (((Point)sweepPoints[i]).Y - center > distance) || (((Point)sweepPoints[i]).Y - center < -distance) )
						sweepPoints.RemoveAt(i);
			}
		}
		#endregion

		#region GetCenterX()
		private static short GetCenterX(ArrayList sweepPoints, Rectangle rect)
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
		
		#region GetCenterY()
		private static short GetCenterY(ArrayList sweepPoints, Rectangle rect)
		{
			if(sweepPoints.Count == 0)
				return 0;
			
			double		numOfPoints = sweepPoints.Count;
			double		yLeft, yRight, a, b;
			long			sumXY = 0, sumX = 0, sumY = 0, sumXxX = 0;

			foreach(Point sweepPoint in sweepPoints)
			{
				sumXY += sweepPoint.X * sweepPoint.Y;
				sumX += sweepPoint.X;
				sumY += sweepPoint.Y;
				sumXxX += sweepPoint.X * sweepPoint.X;
			}
			
			if((sumXxX - sumX * sumX / numOfPoints) > 0)
				b = ( (sumXY - sumX * sumY / numOfPoints) / (sumXxX - sumX * sumX / numOfPoints));
			else
				b = 0;
			a = sumY / numOfPoints - b * sumX / numOfPoints;

			yLeft = a;
			yRight = a + b * rect.Width;

			return (short) (yLeft + (yRight - yLeft) / 2);
		}
		#endregion

		#region class SweepArea
		class SweepArea
		{
			short	dimension;
			float	averageColor;
			byte	minColor;
			float	averageDelta;

			public SweepArea(short dimension, float averageColor, byte minColor, float averageDelta)
			{
				this.dimension = dimension;
				this.averageColor = averageColor;
				this.minColor = minColor;
				this.averageDelta = averageDelta;
			}

			public short	Dimension		{ get{return this.dimension;} set{this.dimension = value;}}
			public float	AverageColor	{ get{return this.averageColor;} set{this.averageColor = value;}}
			public byte		MinColor		{ get{return this.minColor;} set{this.minColor = value;}}
			public float	AverageDelta	{ get{return this.averageDelta;} set{this.averageDelta = value;} }

			public void Reset()
			{
				this.averageColor = -1;
				this.averageDelta = -1;
			}
		}
		#endregion

		#region class SweepAreaColor
		class SweepAreaColor
		{
			short		dimension;
			float[]		averageColor = new float[3];
			byte[]		minColor = new byte[3];
			float[]		averageDelta = new float[3];

			public SweepAreaColor(short dimension, float[] averageColor, byte[] minColor, float[] averageDelta)
			{
				this.dimension = dimension;
				for(byte i = 0; i < 3; i++)
				{
					this.averageColor[i] = averageColor[i];
					this.minColor[i] = minColor[i];
					this.averageDelta[i] = averageDelta[i];
				}
			}

			public short	Dimension		{ get{return this.dimension;} set{this.dimension = value;}}
			public float[]	AverageColor	{ get{return this.averageColor;} set{this.averageColor = value;}}
			public byte[]	MinColor		{ get{return this.minColor;} set{this.minColor = value;}}
			public float[]	AverageDelta	{ get{return this.averageDelta;} set{this.averageDelta = value;} }

			public void Reset()
			{
				for(byte i = 0; i < 3; i++)
				{
					this.averageColor[i] = -1;
					this.averageDelta[i] = -1;
				}
			}
		}
		#endregion

		#region GetThreshold24bpp()
		private static byte[] GetThreshold24bpp(BitmapData bitmapData, Rectangle clip)
		{
			int			stride = bitmapData.Stride; 
			short		clipX = (short) clip.X ;
			short		clipY = (short) clip.Y ;
			short		clipRight = (short) clip.Right ;
			short		clipBottom = (short) clip.Bottom ;
			uint[]		arrayR = new uint[256];
			uint[]		arrayG = new uint[256];
			uint[]		arrayB = new uint[256];
			int			i;
			uint[]		smoothArray = new uint[256];
			byte		extreme = 0;
			byte		secondExtreme = 0;
			byte[]		threshold = new byte[3];
			int			to;
			//int			minLocMinValue;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer() + clipX * 3; 
				byte*	pCurrent ; 

				for(int y = clipY; y < clipBottom; y++) 
				{ 
					pCurrent = pOrig + y * stride;

					for(int x = clipX; x < clipRight; x++) 
					{ 
						arrayB[*(pCurrent++)] ++ ;
						arrayG[*(pCurrent++)] ++ ;
						arrayR[*(pCurrent++)] ++ ;
					} 
				}
			}

			//smoothing red
			smoothArray[0] = (arrayR[0] + arrayR[1] + arrayR[2]) * 20;
			smoothArray[1] = (arrayR[0] + arrayR[1] + arrayR[2] + arrayR[3]) * 15 ;
			smoothArray[255] = (arrayR[253] + arrayR[254] + arrayR[255]) * 20;
			smoothArray[254] = (arrayR[252] + arrayR[253] + arrayR[254] + arrayR[255]) * 15 ;

			for(i = 2; i < 254; i++)
				smoothArray[i] = (arrayR[i-2] + arrayR[i-1] + arrayR[i] + arrayR[i+1] + arrayR[i+2]) * 12 ;

			for(i = 0; i < 256; i++)
				arrayR[i] = smoothArray[i];

			smoothArray[0] = (arrayR[0] + arrayR[1] + arrayR[2]) * 20;
			smoothArray[1] = (arrayR[0] + arrayR[1] + arrayR[2] + arrayR[3]) * 15;
			smoothArray[255] = (arrayR[253] + arrayR[254] + arrayR[255]) * 20;
			smoothArray[254] = (arrayR[252] + arrayR[253] + arrayR[254] + arrayR[255]) * 15;

			for(i = 2; i < 254; i++)
				smoothArray[i] = (arrayR[i-2] + arrayR[i-1] + arrayR[i] + arrayR[i+1] + arrayR[i+2]) * 12;

			for(i = 0; i < 256; i++)
				arrayR[i] = smoothArray[i];

			//local extremes
			extreme = 0;
			secondExtreme = 0;

			for(i = 1; i < 256; i++)
				if(arrayR[extreme] < arrayR[i])
					extreme = (byte) i ;

			for(i = 0; i < 256; i++)
				if( (i > extreme - 80) && (i < extreme + 80) )
					smoothArray[i] = 0;

			for(i = 0; i < 256; i++)
				if(smoothArray[secondExtreme] < smoothArray[i])
					secondExtreme = (byte) i ;

			//minLocMinValue = (arrayR[extreme] * .1F);

			//threshold			
			threshold[0] = (byte) ((extreme + secondExtreme) / 2) ;
			to = (extreme < secondExtreme) ? extreme + 10 : secondExtreme + 10;

			for(i = Math.Max(extreme, secondExtreme) - 10; i > to; i--)
			{
				if( arrayR[i] > arrayR[i + 1] )
				{
					threshold[0] = (byte) i ;
					break ;
				}
			}

			//smoothing green
			smoothArray[0] = (arrayG[0] + arrayG[1] + arrayG[2]) * 20;
			smoothArray[1] = (arrayG[0] + arrayG[1] + arrayG[2] + arrayG[3]) * 15 ;
			smoothArray[255] = (arrayG[253] + arrayG[254] + arrayG[255]) * 20;
			smoothArray[254] = (arrayG[252] + arrayG[253] + arrayG[254] + arrayG[255]) * 15 ;

			for(i = 2; i < 254; i++)
				smoothArray[i] = (arrayG[i-2] + arrayG[i-1] + arrayG[i] + arrayG[i+1] + arrayG[i+2]) * 12 ;

			for(i = 0; i < 256; i++)
				arrayG[i] = smoothArray[i];

			smoothArray[0] = (arrayG[0] + arrayG[1] + arrayG[2]) * 20;
			smoothArray[1] = (arrayG[0] + arrayG[1] + arrayG[2] + arrayG[3]) * 15;
			smoothArray[255] = (arrayG[253] + arrayG[254] + arrayG[255]) * 20;
			smoothArray[254] = (arrayG[252] + arrayG[253] + arrayG[254] + arrayG[255]) * 15;

			for(i = 2; i < 254; i++)
				smoothArray[i] = (arrayG[i-2] + arrayG[i-1] + arrayG[i] + arrayG[i+1] + arrayG[i+2]) * 12;

			for(i = 0; i < 256; i++)
				arrayG[i] = smoothArray[i];

			//local extremes
			extreme = 0;
			secondExtreme = 0;

			for(i = 1; i < 256; i++)
				if(arrayG[extreme] < arrayG[i])
					extreme = (byte) i ;

			for(i = 0; i < 256; i++)
				if( (i > extreme - 80) && (i < extreme + 80) )
					smoothArray[i] = 0;

			for(i = 0; i < 256; i++)
				if(smoothArray[secondExtreme] < smoothArray[i])
					secondExtreme = (byte) i ;

			//threshold			
			threshold[1] = (byte) ((extreme + secondExtreme) / 2) ;
			to = (extreme < secondExtreme) ? extreme + 10 : secondExtreme + 10;

			for(i = Math.Max(extreme, secondExtreme) - 10; i > to; i--)
			{
				if( arrayG[i] > arrayG[i + 1])
				{
					threshold[1] = (byte) i ;
					break ;
				}
			}

			//smoothing blue
			smoothArray[0] = (arrayB[0] + arrayB[1] + arrayB[2]) * 20;
			smoothArray[1] = (arrayB[0] + arrayB[1] + arrayB[2] + arrayB[3]) * 15 ;
			smoothArray[255] = (arrayB[253] + arrayB[254] + arrayB[255]) * 20;
			smoothArray[254] = (arrayB[252] + arrayB[253] + arrayB[254] + arrayB[255]) * 15 ;

			for(i = 2; i < 254; i++)
				smoothArray[i] = (arrayB[i-2] + arrayB[i-1] + arrayB[i] + arrayB[i+1] + arrayB[i+2]) * 12 ;

			for(i = 0; i < 256; i++)
				arrayB[i] = smoothArray[i];

			smoothArray[0] = (arrayB[0] + arrayB[1] + arrayB[2]) * 20;
			smoothArray[1] = (arrayB[0] + arrayB[1] + arrayB[2] + arrayB[3]) * 15;
			smoothArray[255] = (arrayB[253] + arrayB[254] + arrayB[255]) * 20;
			smoothArray[254] = (arrayB[252] + arrayB[253] + arrayB[254] + arrayB[255]) * 15;

			for(i = 2; i < 254; i++)
				smoothArray[i] = (arrayB[i-2] + arrayB[i-1] + arrayB[i] + arrayB[i+1] + arrayB[i+2]) * 12;

			for(i = 0; i < 256; i++)
				arrayB[i] = smoothArray[i];

			//local extremes
			extreme = 0;
			secondExtreme = 0;

			for(i = 1; i < 256; i++)
				if(arrayB[extreme] < arrayB[i])
					extreme = (byte) i ;

			for(i = 0; i < 256; i++)
				if( (i > extreme - 80) && (i < extreme + 80) )
					smoothArray[i] = 0;

			for(i = 0; i < 256; i++)
				if(smoothArray[secondExtreme] < smoothArray[i])
					secondExtreme = (byte) i ;

			//threshold			
			threshold[2] = (byte) ((extreme + secondExtreme) / 2) ;
			to = (extreme < secondExtreme) ? extreme + 10 : secondExtreme + 10;

			for(i = Math.Max(extreme, secondExtreme) - 10; i > to; i--)
			{
				if( arrayB[i] > arrayB[i + 1])
				{
					threshold[2] = (byte) i ;
					break ;
				}
			}

			return threshold;
		}
		#endregion

		#region GetThresholdGray()
		public static byte GetThresholdGray(BitmapData bitmapData, Rectangle clip)
		{
			int			stride = bitmapData.Stride; 
			short		clipX = (short) clip.X ;
			short		clipY = (short) clip.Y ;
			short		clipRight = (short) clip.Right ;
			short		clipBottom = (short) clip.Bottom ;
			uint[]		array = new uint[256];
			int			i;
			uint[]		smoothArray = new uint[256];
			byte		extreme = 0;
			byte		secondExtreme = 0;
			byte		threshold;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer() + clipX; 
				byte*	pCurrent ; 

				for(int y = clipY; y < clipBottom; y++) 
				{ 
					pCurrent = pOrig + y * stride;

					for(int x = clipX; x < clipRight; x++) 
					{ 
						array[*(pCurrent++)] ++ ;
					} 
				}
			}

			//smoothing
			smoothArray[0] = (array[0] + array[1] + array[2]) * 20;
			smoothArray[1] = (array[0] + array[1] + array[2] + array[3]) * 15 ;
			smoothArray[255] = (array[253] + array[254] + array[255]) * 20;
			smoothArray[254] = (array[252] + array[253] + array[254] + array[255]) * 15 ;

			for(i = 2; i < 254; i++)
				smoothArray[i] = (array[i-2] + array[i-1] + array[i] + array[i+1] + array[i+2]) * 12 ;

			for(i = 0; i < 256; i++)
				array[i] = smoothArray[i];

			//smoothing
			smoothArray[0] = (array[0] + array[1] + array[2]) * 20;
			smoothArray[1] = (array[0] + array[1] + array[2] + array[3]) * 15;
			smoothArray[255] = (array[253] + array[254] + array[255]) * 20;
			smoothArray[254] = (array[252] + array[253] + array[254] + array[255]) * 15;

			for(i = 2; i < 254; i++)
				smoothArray[i] = (array[i-2] + array[i-1] + array[i] + array[i+1] + array[i+2]) * 12;

			for(i = 0; i < 256; i++)
				array[i] = smoothArray[i];

			//local extremes
			for(i = 1; i < 256; i++)
				if(array[extreme] < array[i])
					extreme = (byte) i ;

			for(i = 0; i < 256; i++)
			{
				if( (i < extreme - 80) || (i > extreme + 80) )
					smoothArray[i] = array[i];
				else
					smoothArray[i] = 0;
			}

			for(i = 0; i < 256; i++)
				if(smoothArray[secondExtreme] < smoothArray[i])
					secondExtreme = (byte) i ;

			//threshold			
			threshold = (byte) ((extreme + secondExtreme) / 2) ;

			int			to = (extreme < secondExtreme) ? extreme + 10 : secondExtreme + 10;
			for(i = Math.Max(extreme, secondExtreme) - 10; i > to; i--)
			{
				if( (array[i] > array[i + 1]) || (array[i] == 0) || (array[extreme] / array[i] > 50))
				{
					threshold = (byte) i ;
					break ;
				}
			}

			return threshold;
		}
		#endregion

		#region GetSmoothenessGray()
		public static float GetSmoothenessGray(BitmapData bitmapData, Rectangle clip)
		{
			int			stride = bitmapData.Stride; 
			short		clipX = (short) clip.X ;
			short		clipY = (short) clip.Y ;
			short		clipRightMinus1 = (short) (clip.Right - 1) ;
			short		clipBottom = (short) clip.Bottom ;
			float		delta = 0;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer() + clipX; 
				byte*	pCurrent ; 

				for(int y = clipY; y < clipBottom; y++) 
				{ 
					pCurrent = pOrig + y * stride;

					for(int x = clipX; x < clipRightMinus1; x++) 
					{ 
						delta += (*pCurrent > pCurrent[1]) ? (*pCurrent - pCurrent[1]) : (pCurrent[1] - *pCurrent);
						pCurrent++;
					} 
				}
			}

			return delta / ((clip.Width - 1) * clip.Height);
		}
		#endregion

		#region GetAverageGray()
		public static byte GetAverageGray(BitmapData bitmapData, Rectangle clip)
		{
			int			stride = bitmapData.Stride; 
			short		clipX = (short) clip.X ;
			short		clipY = (short) clip.Y ;
			short		clipRight = (short) clip.Right ;
			short		clipBottom = (short) clip.Bottom ;
			uint[]		array = new uint[256];
			int			x, y;
			ulong		colors = 0;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer() + clipX; 
				byte*	pCurrent ; 

				for(y = clipY; y < clipBottom; y++) 
				{ 
					pCurrent = pOrig + y * stride;

					for(x = clipX; x < clipRight; x++) 
					{ 
						colors += *(pCurrent++);
					} 
				}
			}

			return Convert.ToByte(colors / (float) (clip.Width * clip.Height));
		}
		#endregion

		#region GetDeltaGray()
		public static byte GetDeltaGray(BitmapData bitmapData, Rectangle clip)
		{
			int			stride = bitmapData.Stride; 
			short		clipX = (short) clip.X ;
			short		clipY = (short) clip.Y ;
			short		clipRight = (short) (clip.Right - 1);
			short		clipBottom = (short) clip.Bottom ;
			uint[]		array = new uint[256];
			int			x, y;
			ulong		delta = 0;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer() + clipX; 
				byte*	pCurrent ; 

				for(y = clipY; y < clipBottom; y++) 
				{ 
					pCurrent = pOrig + y * stride;

					for(x = clipX; x < clipRight - 1; x++) 
					{ 
						delta += (*pCurrent > pCurrent[1]) ? (uint) (*pCurrent - pCurrent[1]) : (uint) (pCurrent[1] - *pCurrent);
						pCurrent++;
					} 
				}
			}

			return Convert.ToByte(delta / (float) ((clip.Width - 1) * clip.Height));
		}
		#endregion
	
	}*/
}
