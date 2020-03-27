using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	public class CurveCorrectionOld
	{		 

		#region constructor
		private CurveCorrectionOld()
		{
		}
		#endregion

		//	PUBLIC METHODS

		#region Get()
		public static Bitmap Get(Bitmap bitmap, Rectangle clip, int middleHoriz, int wThresholdDelta, int minDelta, int clipOffset, int flags)
		{
			byte			confidence;
			
			Bitmap		raster = ImagePreprocessing.Go(bitmap, wThresholdDelta, minDelta);
#if(DEBUG)
			raster.Save(@"c:\temp\bf\001 raster.png", ImageFormat.Png);
#endif
			Rectangle	rawClip = PageContentLocator.GetRawClip( raster, clip, 20, new RectangleF(.4F, .1F, .2F, .8F), 
				.25F, .01F, 100, 4, out confidence);

			if(confidence > 30)
			{
				ObjectLocator.PageObjects	allObjects = ObjectLocator.FindObjects(raster, rawClip, 1);
				raster.Dispose();
				int						minSize = Convert.ToInt32(allObjects.GetAverageLetterHeight() * .80F);
				ObjectLocator.PageObjects	objects = ObjectLocator.SiftList(allObjects, minSize / 3, minSize, 0);
				Rectangle				contentClip = objects.GetClip();

				if(contentClip.IsEmpty)
					contentClip = rawClip;
		
				contentClip.Inflate((int) clipOffset, (int) clipOffset);
				contentClip = Rectangle.Intersect(contentClip, clip);

				Curve	curveT, curveB;
				Get1bpp(objects, contentClip, middleHoriz, flags, out curveT, out curveB);
				PageParams	pageParams = new PageParams(bitmap.Size, curveT, curveB, clip);

				return GetFromParams(bitmap, pageParams, flags);
			}
			else
				return null;
		}
		#endregion

		#region FindCurvesFromRaster()
		public static void FindCurvesFromRaster(Bitmap raster, Rectangle clip, int middleHoriz, int flags, 
			out Curve curveT, out Curve curveB)
		{
			Get1bpp(raster, clip, middleHoriz, flags, out curveT, out curveB);
		}
		#endregion
		
		#region GetFromRasterImage()
		public static Bitmap GetFromRasterImage(Bitmap bitmap, Rectangle clip, Bitmap raster, int middleHoriz, int flags)
		{
			Curve	curveT, curveB;
			
			Get1bpp(raster, clip, middleHoriz, flags, out curveT, out curveB);
			PageParams	pageParams = new PageParams(bitmap.Size, curveT, curveB, clip);

			return GetFromParams(bitmap, pageParams, flags);
		}
		#endregion
						
		#region GetFromLines()
		public static Bitmap GetFromLines(Bitmap bitmap, Rectangle clip, ObjectLocator.Lines lines, int middleHoriz, int flags)
		{						 
			Curve		curveT, curveB;
			GetCurvesFromLines(clip, lines, middleHoriz, flags, out curveT, out curveB);

			PageParams	pageParams = new PageParams(bitmap.Size, curveT, curveB, clip);

			return GetFromParams(bitmap, pageParams, flags);
		}
		#endregion

		#region GetCurvesFromLines()
		public static void GetCurvesFromLines(Rectangle clip, ObjectLocator.Lines lines, int middleHoriz, int flags,
			out Curve curveT, out Curve curveB)
		{						 
			ArrayList	pointsTop = GetTopBorder(lines, clip, middleHoriz, flags);
			Point[]		pointsT = PointsToGlobalTop(pointsTop, middleHoriz, clip);
			curveT = new Curve(new Clip(clip), pointsT, true);
				
			ArrayList	pointsBottom = GetBottomBorder(lines, clip, middleHoriz, flags);
			Point[]		pointsB = PointsToGlobalBottom(pointsBottom, middleHoriz, clip);				
			curveB = new Curve(new Clip(clip), pointsB, false);
		}
		#endregion

		#region GetFromParams()
		public static Bitmap GetFromParams(Bitmap bitmap, PageParams pageParams, int flags)
		{
#if DEBUG
			DateTime	now = DateTime.Now ;
#endif
			if(pageParams.IsCurved == false)
			{
				if(pageParams.Clip.IsAngled)
					return Rotation.Rotate(bitmap, pageParams.Angle, 255, 255, 255);
			else
				return CopyImage.Copy(bitmap, null, pageParams.Clip.RectangleNotAngled);
			}

			try
			{
				switch(bitmap.PixelFormat)
				{
					case PixelFormat.Format1bppIndexed:
						return Stretch1bpp(bitmap, pageParams);
					case PixelFormat.Format8bppIndexed:
						return Stretch8bpp(bitmap, pageParams);
					case PixelFormat.Format24bppRgb:
						return Stretch24bpp(bitmap, pageParams);
					case PixelFormat.Format32bppRgb:
					case PixelFormat.Format32bppArgb:
						return Stretch32bpp(bitmap, pageParams);
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}
			}
			catch(Exception ex)
			{
				throw new Exception("CurveCorrectionOld, Get(): " + ex.Message ) ;
			}
			finally
			{
#if DEBUG
				Console.WriteLine(string.Format("CurveCorrectionOld: {0}" , DateTime.Now.Subtract(now).ToString()));
#endif
			}
		}
		#endregion
		
		//PRIVATE METHODS

		#region Get1bpp()
		private static void Get1bpp(Bitmap bitmap, Rectangle clip, int middleHoriz, int flags,
			out Curve curveT, out Curve curveB)
		{			
			ObjectLocator.PageObjects	allObjects = ObjectLocator.FindObjects(bitmap, clip, 1);
			ObjectLocator.PageObjects	objects = ObjectLocator.SiftList(allObjects, 6, 12, 60, 60, 0);

			Get1bpp(objects, clip, middleHoriz, flags, out curveT, out curveB);
		}
		#endregion
		
		#region Get1bpp()
		private static void Get1bpp(ObjectLocator.PageObjects objects, Rectangle clip, int middleHoriz, int flags,
			out Curve curveT, out Curve curveB)
		{									 
			ObjectLocator.Words		words = ObjectLocator.FindWords(objects);
			ObjectLocator.Lines		lines = ObjectLocator.FindLines(words);
#if DEBUG
			objects.DrawToFile(@"c:\temp\bf\002 Letters.png", new Size(clip.Right, clip.Bottom));
			words.DrawToFile(@"C:\Temp\bf\004 words.png", new Size(clip.Right, clip.Bottom));
			lines.DrawToFile(@"C:\Temp\bf\005 lines.png", new Size(clip.Right, clip.Bottom));
#endif

			ArrayList	pointsTop = GetTopBorder(lines, clip, middleHoriz, flags);
			//curveT = PointsToGlobalTop(pointsTop, middleHoriz, clip);
			curveT = new Curve(new Clip(clip), PointsToGlobalTop(pointsTop, middleHoriz, clip), true);
				
			ArrayList	pointsBottom = GetBottomBorder(lines, clip, middleHoriz, flags);
			curveB = new Curve(new Clip(clip), PointsToGlobalBottom(pointsBottom, middleHoriz, clip), false);			
		}
		#endregion
		
		#region GetTopBorder()
		private static ArrayList GetTopBorder(ObjectLocator.Lines lines, Rectangle clip, int middleHoriz, int flags)
		{						 		
			foreach(ObjectLocator.Line line in lines)
				if(line.Y < middleHoriz && line.Width > clip.Width * 3 / 4)
					return CompletePoints(clip, line.GetBfPoints());

			ArrayList	pointSet = new ArrayList();
			pointSet.Add(new Point(clip.X,0));
			pointSet.Add(new Point(clip.X + clip.Width / 6,0));
			pointSet.Add(new Point(clip.X + clip.Width * 2 / 6,0));
			pointSet.Add(new Point(clip.X + clip.Width * 3 / 6,0));
			pointSet.Add(new Point(clip.X + clip.Width * 4 / 6,0));
			pointSet.Add(new Point(clip.X + clip.Width * 5 / 6,0));
			pointSet.Add(new Point(clip.X + clip.Width,0));

			return pointSet;
		}
		#endregion

		#region GetBottomBorder()
		private static ArrayList GetBottomBorder(ObjectLocator.Lines lines, Rectangle clip, int middleHoriz, int flags)
		{		
			for(int i = lines.Count - 1; i >= 0; i--)
			{
				ObjectLocator.Line line = lines[i];

				if(line.Y > middleHoriz && line.Width > clip.Width * 3 / 4)
					return CompletePoints(clip, line.GetBfPoints());
			}

			ArrayList	pointSet = new ArrayList();
			pointSet.Add(new Point(clip.X, clip.Bottom));
			pointSet.Add(new Point(clip.X + clip.Width / 6, clip.Bottom));
			pointSet.Add(new Point(clip.X + clip.Width * 2 / 6, clip.Bottom));
			pointSet.Add(new Point(clip.X + clip.Width * 3 / 6, clip.Bottom));
			pointSet.Add(new Point(clip.X + clip.Width * 4 / 6, clip.Bottom));
			pointSet.Add(new Point(clip.X + clip.Width * 5 / 6, clip.Bottom));
			pointSet.Add(new Point(clip.X + clip.Width, clip.Bottom));

			return pointSet;
		}
		#endregion
						
		#region PointsToGlobalTop()
		private static Point[] PointsToGlobalTop(ArrayList points, int middlePoint, Rectangle clip)
		{
			ArrayList	pointsGlobal = new ArrayList();
			
			if(points.Count > 0)
			{
				int		smallestY = ((Point) points[0]).Y;
			
				foreach(Point point in points)
					if(smallestY > point.Y)
						smallestY = point.Y;

				for(int i = 0; i < points.Count; i++)
				{
					Point	point = (Point) points[i];
					pointsGlobal.Add(new Point(point.X, (int) ((point.Y - smallestY) * (middlePoint - clip.Y) / (float)(middlePoint - point.Y))));
				}
			}

			return (Point[]) pointsGlobal.ToArray(typeof(Point));
		}
		#endregion

		#region PointsToGlobalBottom()
		private static Point[] PointsToGlobalBottom(ArrayList points, int middlePoint, Rectangle clip)
		{
			ArrayList	pointsGlobal = new ArrayList();
			
			if(points.Count > 0)
			{
				int		biggestY = ((Point) points[0]).Y;
			
				foreach(Point point in points)
					if(biggestY < point.Y)
						biggestY = point.Y;

				for(int i = 0; i < points.Count; i++)
				{
					Point	point = (Point) points[i];
					pointsGlobal.Add(new Point(point.X, (int) ((biggestY - point.Y) * (clip.Bottom - middlePoint) / (float) (point.Y - middlePoint))));
				}
			}

			return (Point[]) pointsGlobal.ToArray(typeof(Point));
		}
		#endregion
		
		#region Stretch32bpp()
		private static Bitmap Stretch32bpp(Bitmap source, PageParams pageParams)
		{
			BitmapData	sourceData = null;
			BitmapData	resultData = null;
			Bitmap		result = null;

			try
			{			
				Rectangle	clip = pageParams.Clip.RectangleNotAngled;
				result = new Bitmap(clip.Width, clip.Height, source.PixelFormat); 
					
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat); 
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat); 

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 
				int			clipHeight = clip.Height;
				int			clipWidth = clip.Width;
				int			x, y;
				double		stretchLength = 1;
				double		firstPixelPortion;
				double		currentVal;

				double[]	arrayT, arrayB;
				int			lensCenter = GetLensCenter(pageParams);
				
				GetCurves(pageParams, out arrayT, out arrayB);
			 
				unsafe
				{
					byte*		pSource = (byte*)sourceData.Scan0.ToPointer(); 
					byte*		pResult = (byte*)resultData.Scan0.ToPointer(); 
					byte*		pCurrentS ;
					byte*		pCurrentR ;

					for(x = 0; x < clipWidth; x++) 
					{					
						stretchLength = 1 - ((arrayB[x] + arrayT[x]) / clip.Height);
						firstPixelPortion = 1 - (arrayT[x] - (int) arrayT[x]);
						
						pCurrentS = pSource + (int) arrayT[x] * sStride + x * 4;
						pCurrentR = pResult + x * 4;
					
						for(y = 0; y < clipHeight; y++) 
						{ 	
							if(firstPixelPortion - stretchLength > 0.00000001)
							{
								*pCurrentR = *pCurrentS;
								pCurrentR[1] = pCurrentS[1];
								pCurrentR[2] = pCurrentS[2];
								firstPixelPortion -= stretchLength;
							}
							else if (firstPixelPortion - stretchLength < -0.00000001)
							{
								currentVal = (firstPixelPortion * *pCurrentS + (stretchLength - firstPixelPortion) * pCurrentS[sStride]);
								*pCurrentR = Convert.ToByte(currentVal / stretchLength);
								currentVal = (firstPixelPortion * pCurrentS[1] + (stretchLength - firstPixelPortion) * pCurrentS[1+sStride]);
								pCurrentR[1] = Convert.ToByte(currentVal / stretchLength);
								currentVal = (firstPixelPortion * pCurrentS[2] + (stretchLength - firstPixelPortion) * pCurrentS[2+sStride]);
								pCurrentR[2] = Convert.ToByte(currentVal / stretchLength);

								pCurrentS = pCurrentS + sStride;
								firstPixelPortion += 1 - stretchLength;
							}
							else
							{
								*pCurrentR = *pCurrentS;
								pCurrentR[1] = pCurrentS[1];
								pCurrentR[2] = pCurrentS[2];
								pCurrentS = pCurrentS + sStride;
								firstPixelPortion = 1;
							}
							
							pCurrentR = pCurrentR + rStride;
						}
					}
				}
			
				return result; 	
			}
			finally
			{
				if(source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if(result != null && resultData != null)
					result.UnlockBits(resultData); 

				if(result != null)
					result.SetResolution(source.HorizontalResolution, source.VerticalResolution);
			}
		}
		#endregion

		#region Stretch24bpp()
		private static Bitmap Stretch24bpp(Bitmap source, PageParams pageParams)
		{
			BitmapData	sourceData = null;
			BitmapData	resultData = null;
			Bitmap		result = null;

			try
			{			
				Rectangle	clip = pageParams.Clip.RectangleNotAngled;
				result = new Bitmap(clip.Width, clip.Height, source.PixelFormat); 
					
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat); 
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat); 

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 
				int			clipHeight = clip.Height;
				int			clipWidth = clip.Width;
				int			x, y;
				double		stretchLength = 1;
				double		firstPixelPortion;
				double		currentVal;

				double[]	arrayT, arrayB;
				int			lensCenter = GetLensCenter(pageParams);
				
				GetCurves(pageParams, out arrayT, out arrayB);
			 
				unsafe
				{
					try
					{
						byte*		pSource = (byte*)sourceData.Scan0.ToPointer(); 
						byte*		pResult = (byte*)resultData.Scan0.ToPointer(); 
						byte*		pCurrentS ;
						byte*		pCurrentR ;

						if(lensCenter > 0)
						{
							for(x = 0; x < clipWidth; x++) 
							{					
								stretchLength = 1 - arrayT[x] / lensCenter;
								firstPixelPortion = 1 - (arrayT[x] - (int) arrayT[x]);
						
								pCurrentS = pSource + (int) arrayT[x] * sStride + x * 3;
								pCurrentR = pResult + x * 3;
					
								for(y = 0; y < lensCenter; y++) 
								{ 	
									if(firstPixelPortion - stretchLength > 0.00000001)
									{
										*pCurrentR = *pCurrentS;
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
										firstPixelPortion -= stretchLength;
									}
									else if (firstPixelPortion - stretchLength < -0.00000001)
									{
										currentVal = (firstPixelPortion * *pCurrentS + (stretchLength - firstPixelPortion) * pCurrentS[sStride]);
										*pCurrentR = Convert.ToByte(currentVal / stretchLength);
										currentVal = (firstPixelPortion * pCurrentS[1] + (stretchLength - firstPixelPortion) * pCurrentS[1+sStride]);
										pCurrentR[1] = Convert.ToByte(currentVal / stretchLength);
										currentVal = (firstPixelPortion * pCurrentS[2] + (stretchLength - firstPixelPortion) * pCurrentS[2+sStride]);
										pCurrentR[2] = Convert.ToByte(currentVal / stretchLength);

										pCurrentS = pCurrentS + sStride;
										firstPixelPortion += 1 - stretchLength;
									}
									else
									{
										*pCurrentR = *pCurrentS;
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
										pCurrentS = pCurrentS + sStride;
										firstPixelPortion = 1;
									}
							
									pCurrentR = pCurrentR + rStride;
								}
							}
						}
						
						if(lensCenter < clipHeight - 1)
						{
							for(x = 0; x < clipWidth; x++) 
							{					

								stretchLength = 1 - arrayB[x] / (clipHeight - lensCenter);
								firstPixelPortion = 1;
						
								pCurrentS = pSource + lensCenter * sStride + x * 3;
								pCurrentR = pResult + lensCenter * rStride + x * 3;
					
								for(y = lensCenter; y < clipHeight; y++) 
								{ 	
									if(firstPixelPortion - stretchLength > 0.00000001)
									{
										*pCurrentR = *pCurrentS;
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
										firstPixelPortion -= stretchLength;
									}
									else if (firstPixelPortion - stretchLength < -0.00000001)
									{
										currentVal = (firstPixelPortion * *pCurrentS + (stretchLength - firstPixelPortion) * pCurrentS[sStride]);
										*pCurrentR = Convert.ToByte(currentVal / stretchLength);
										currentVal = (firstPixelPortion * pCurrentS[1] + (stretchLength - firstPixelPortion) * pCurrentS[1+sStride]);
										pCurrentR[1] = Convert.ToByte(currentVal / stretchLength);
										currentVal = (firstPixelPortion * pCurrentS[2] + (stretchLength - firstPixelPortion) * pCurrentS[2+sStride]);
										pCurrentR[2] = Convert.ToByte(currentVal / stretchLength);

										pCurrentS = pCurrentS + sStride;
										firstPixelPortion += 1 - stretchLength;
									}
									else
									{
										*pCurrentR = *pCurrentS;
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
										pCurrentS = pCurrentS + sStride;
										firstPixelPortion = 1;
									}
							
									pCurrentR = pCurrentR + rStride;
								}
							}
						}
					}
					catch(Exception ex)
					{
						ex = ex;
					}
				}
			
				return result; 	
			}
			finally
			{
				if(source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if(result != null && resultData != null)
					result.UnlockBits(resultData); 

				if(result != null)
					result.SetResolution(source.HorizontalResolution, source.VerticalResolution);
			}
		}
		#endregion
				
		#region Stretch24bpp()
		/*private static Bitmap Stretch24bpp(Bitmap source, PageParams pageParams)
		{
			BitmapData	sourceData = null;
			BitmapData	resultData = null;
			Bitmap		result = null;

			try
			{			
				Rectangle	clip = pageParams.Clip.Rectangle;
				result = new Bitmap(clip.Width, clip.Height, source.PixelFormat); 
					
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat); 
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat); 

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 
				int			clipHeight = clip.Height;
				int			clipWidth = clip.Width;
				int			x, y;
				double		stretchLength = 1;
				double		firstPixelPortion;
				double		currentVal;

				double[]	arrayT, arrayB;
				GetCurves(pageParams, out arrayT, out arrayB);
			 
				unsafe
				{
					byte*		pSource = (byte*)sourceData.Scan0.ToPointer(); 
					byte*		pResult = (byte*)resultData.Scan0.ToPointer(); 
					byte*		pCurrentS ;
					byte*		pCurrentR ;

					for(x = 0; x < clipWidth; x++) 
					{					
						stretchLength = 1 - ((arrayB[x] + arrayT[x]) / clip.Height);
						firstPixelPortion = 1 - (arrayT[x] - (int) arrayT[x]);
						
						pCurrentS = pSource + (int) arrayT[x] * sStride + x * 3;
						pCurrentR = pResult + x * 3;
					
						for(y = 0; y < clipHeight; y++) 
						{ 	
							if(firstPixelPortion - stretchLength > 0.00000001)
							{
								*pCurrentR = *pCurrentS;
								pCurrentR[1] = pCurrentS[1];
								pCurrentR[2] = pCurrentS[2];
								firstPixelPortion -= stretchLength;
							}
							else if (firstPixelPortion - stretchLength < -0.00000001)
							{
								currentVal = (firstPixelPortion * *pCurrentS + (stretchLength - firstPixelPortion) * pCurrentS[sStride]);
								*pCurrentR = Convert.ToByte(currentVal / stretchLength);
								currentVal = (firstPixelPortion * pCurrentS[1] + (stretchLength - firstPixelPortion) * pCurrentS[1+sStride]);
								pCurrentR[1] = Convert.ToByte(currentVal / stretchLength);
								currentVal = (firstPixelPortion * pCurrentS[2] + (stretchLength - firstPixelPortion) * pCurrentS[2+sStride]);
								pCurrentR[2] = Convert.ToByte(currentVal / stretchLength);

								pCurrentS = pCurrentS + sStride;
								firstPixelPortion += 1 - stretchLength;
							}
							else
							{
								*pCurrentR = *pCurrentS;
								pCurrentR[1] = pCurrentS[1];
								pCurrentR[2] = pCurrentS[2];
								pCurrentS = pCurrentS + sStride;
								firstPixelPortion = 1;
							}
							
							pCurrentR = pCurrentR + rStride;
						}
					}

				}
			
				return result; 	
			}
			finally
			{
				if(source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if(result != null && resultData != null)
					result.UnlockBits(resultData); 

				if(result != null)
					result.SetResolution(source.HorizontalResolution, source.VerticalResolution);
			}
		}*/
		#endregion

		#region Stretch8bpp()
		private static Bitmap Stretch8bpp(Bitmap source, PageParams pageParams)
		{
			BitmapData	sourceData = null;
			BitmapData	resultData = null;
			Bitmap		result = null;

			try
			{			
				Rectangle clip = pageParams.Clip.RectangleNotAngled;
				result = new Bitmap(clip.Width, clip.Height, source.PixelFormat); 
					
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat); 
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat); 

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 
				int			clipHeight = clip.Height;
				int			clipWidth = clip.Width;
				int			x, y;
				
				double[]	arrayT, arrayB;
				int			lensCenter = GetLensCenter(pageParams);
				
				GetCurves(pageParams, out arrayT, out arrayB);

				double		stretchLength = 1;
				double		firstPixelPortion;
				double		currentVal;
			 
				unsafe
				{
					byte*		pSource = (byte*)sourceData.Scan0.ToPointer(); 
					byte*		pResult = (byte*)resultData.Scan0.ToPointer(); 
					byte*		pCurrentS ;
					byte*		pCurrentR ;

					for(x = 0; x < clipWidth; x++) 
					{
						//find the stretch lenght in x					
						stretchLength = 1 - ((arrayB[x] + arrayT[x]) / clip.Height);
						firstPixelPortion = 1 - (arrayT[x] - (int) arrayT[x]);
						
						pCurrentS = pSource + (int) arrayT[x] * sStride + x;
						pCurrentR = pResult + x;
					
						for(y = 0; y < clipHeight; y++) 
						{ 	
							if(firstPixelPortion - stretchLength > 0.00000001)
							{
								*pCurrentR = *pCurrentS;
								firstPixelPortion -= stretchLength;
							}
							else if (firstPixelPortion - stretchLength < -0.00000001)
							{
								currentVal = (firstPixelPortion * *pCurrentS + (stretchLength - firstPixelPortion) * pCurrentS[sStride]);
							
								*pCurrentR = Convert.ToByte(currentVal / stretchLength);
								pCurrentS = pCurrentS + sStride;
								firstPixelPortion += 1 - stretchLength;
							}
							else
							{
								*pCurrentR = *pCurrentS;
								pCurrentS = pCurrentS + sStride;
								firstPixelPortion = 1;
							}
							
							pCurrentR = pCurrentR + rStride;
						}
					}
				}
			
				return result; 		
			}
			finally
			{
				if(source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if(result != null && resultData != null)
					result.UnlockBits(resultData); 

				if(result != null)
				{
					result.Palette = source.Palette;
					result.SetResolution(source.HorizontalResolution, source.VerticalResolution);
				}
			}
		}
		#endregion

		#region Stretch1bpp()
		private static Bitmap Stretch1bpp(Bitmap source, PageParams pageParams)
		{
			BitmapData	sourceData = null;
			BitmapData	resultData = null;
			Bitmap		result = null;

			try
			{			
				Rectangle clip = pageParams.Clip.RectangleNotAngled;
				result = new Bitmap(clip.Width, clip.Height, source.PixelFormat); 
					
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat); 
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat); 

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 
				int			clipHeight = clip.Height;
				int			clipWidth = clip.Width;
				int			x, y;
				
				double[]	arrayT, arrayB;
				int			lensCenter = GetLensCenter(pageParams);
				
				GetCurves(pageParams, out arrayT, out arrayB);
				
				double		stretchLength = 1;
				double		firstPixelPortion;
				byte		biteIndex;
			 
				unsafe
				{
					byte*		pSource = (byte*)sourceData.Scan0.ToPointer(); 
					byte*		pResult = (byte*)resultData.Scan0.ToPointer(); 
					byte*		pCurrentS ;
					byte*		pCurrentR ;

					for(x = 0; x < clipWidth; x++) 
					{
						//find the stretch lenght in x					
						stretchLength = 1 - ((arrayB[x] + arrayT[x]) / clip.Height);
						firstPixelPortion = 1 - (arrayT[x] - (int) arrayT[x]);
						biteIndex = (byte) (0x80 >> (x % 8));
						
						pCurrentS = pSource + (int) arrayT[x] * sStride + x / 8;
						pCurrentR = pResult + x / 8;
					
						for(y = 0; y < clipHeight; y++) 
						{ 	
							if(firstPixelPortion - stretchLength > 0.00000001)
							{
								*pCurrentR |= (byte) (*pCurrentS & biteIndex);
								firstPixelPortion -= stretchLength;
							}
							else if (firstPixelPortion - stretchLength < -0.00000001)
							{
								if(firstPixelPortion > -firstPixelPortion - stretchLength)
									*pCurrentR |= (byte) (*pCurrentS  & biteIndex);
								else
									*pCurrentR |= (byte) (pCurrentS[sStride] & biteIndex);

								pCurrentS = pCurrentS + sStride;
								firstPixelPortion += 1 - stretchLength;
							}
							else
							{
								*pCurrentR |= (byte) (*pCurrentS & biteIndex);
								pCurrentS = pCurrentS + sStride;
								firstPixelPortion = 1;
							}
							
							pCurrentR = pCurrentR + rStride;
						}
					}
				}
			
				return result; 		
			}
			finally
			{
				if(source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if(result != null && resultData != null)
					result.UnlockBits(resultData); 

				if(result != null)
				{
					result.Palette = source.Palette;
					result.SetResolution(source.HorizontalResolution, source.VerticalResolution);
				}
			}
		}
		#endregion

		#region CompletePoints()
		private static ArrayList CompletePoints(Rectangle clip, Point[] points)
		{
			ArrayList	pointSet = new ArrayList();

			pointSet.AddRange(points);
			
			Point	p1, p2;
			double	derivation;

			p1 = (Point) pointSet[0];
			p2 = (Point) pointSet[1];
			derivation = (p2.X - p1.X != 0) ? ((p2.Y - p1.Y) / (double) (p2.X - p1.X)) : 0;
							
			pointSet.Insert(0, new Point(clip.X, p1.Y - Convert.ToInt32(derivation * (p1.X - clip.X))));

			p1 = (Point) pointSet[pointSet.Count - 2];
			p2 = (Point) pointSet[pointSet.Count - 1];
			derivation = (p2.X - p1.X != 0) ? ((p2.Y - p1.Y) / (double) (p2.X - p1.X)) : 0;
							
			pointSet.Add(new Point(clip.Right, p2.Y + Convert.ToInt32(derivation * (clip.Right - p2.X))));

			return pointSet;
		}
		#endregion

		#region GetCurve()
		/*private static double[] GetCurve(Point[] points)
		{
			Point[]	p = new Point[] {points[1], points[2], points[3], points[4], points[5]};
			Point	p0 = p[0];
			Point	p1 = p[1];
			Point	p2 = p[2];
			Point	p3 = p[3];
			Point	p4 = p[4];
			int		h0 = p1.X - p0.X;
			int		h1 = p2.X - p1.X;
			int		h2 = p3.X - p2.X;
			int		h3 = p4.X - p3.X;
			int[]	h = new int[] {h0,h1,h2,h3};
			double	b0 = (p1.Y - p0.Y) / (double) h0;
			double	b1 = (p2.Y - p1.Y) / (double) h1;
			double	b2 = (p3.Y - p2.Y) / (double) h2;
			double	b3 = (p4.Y - p3.Y) / (double) h3;
			double	z0 = 0;
			double	z1 = ( 6*(b2-b1) -((12*(h1+h2)*(b1-b0))/((double)h1)) - ((3*h3*(b3-b2))/((double)h2+h3)) + ((3*h2*h2*(b1-b0))/(h1*(h2+h3))) ) / 
				( h1 - ((4*(h0+h1)*(h1+h2)) / ((double)h1)) + ((h2*h2*(h0+h1))/(double)(h1*(h2+h3))) );

			double	z2 = (6*(b1-b0)-2*z1*(h0+h1)) / (double) (h1);
			double	z3 = (6*(b3-b2) - h2*z2) / (double)(2*(h2+h3));
			double	z4 = 0;
			double[]	z = new double[] {z0,z1,z2,z3,z4};
			double[]	array = new double[points[6].X - points[0].X + 1];
			double	r1,r2,r3,r4;

			for(int i = 0; i < 4; i++)
			{
				double	c = (p[i+1].Y / (double) h[i]) - (h[i] * z[i+1] / (double) 6);
				double	d = (p[i].Y / (double) h[i]) - (h[i] * z[i] / (double) 6);
				
				for(int x = p[i].X; x < p[i+1].X; x++)
				{
					r1 = ((z[i+1] * (x-p[i].X)*(x-p[i].X)*(x-p[i].X)) / ((double) (6*h[i])));
					r2 = ((z[i] * (p[i+1].X - x)*(p[i+1].X - x)*(p[i+1].X - x)) / ((double) (6*h[i])));
					r3 = c*(x-p[i].X);
					r4 = d*(p[i+1].X - x);
					
					array[x] = r1+r2+r3+r4;
				}
			}


			for(int x = points[0].X; x < points[1].X; x++)
				array[x] = points[0].Y + (points[1].Y - points[0].Y)*(x - points[0].X) / ((double)(points[1].X - points[0].X));

			for(int x = points[5].X; x <= points[6].X; x++)
				array[x] = points[5].Y + (points[6].Y - points[5].Y)*(x - points[5].X) / ((double)(points[6].X - points[5].X));
	
			double	smallestNumber = 0;
			for(int x = 0; x <= points[6].X; x++)
				if(smallestNumber > array[x])
					smallestNumber = array[x];

			if(smallestNumber < 0)
				for(int x = 0; x <= points[6].X; x++)
					array[x] -= smallestNumber;

			return array;
		}*/
		#endregion

		#region GetCurves()
		private static void GetCurves(PageParams pageParams, out double[] arrayT, out double[] arrayB)
		{	
			int			lensCenter = GetLensCenter(pageParams);
			arrayT = pageParams.Bookfolding.TopCurve.GetNotAngledArray();
			arrayB = pageParams.Bookfolding.BottomCurve.GetNotAngledArray();

			if(pageParams.Clip.IsAngled)
			{
				for(int x = 0; x < arrayT.Length; x++)
					arrayT[x] = arrayT[x] - Math.Tan(pageParams.Clip.Angle) * x;

				for(int x = 0; x < arrayT.Length; x++)
					arrayB[x] = arrayB[x] - Math.Tan(pageParams.Clip.Angle) * x;
			}
			
			double	smallestNumber = int.MaxValue;
			for(int x = 0; x < arrayT.Length; x++)
				if(smallestNumber > arrayT[x])
					smallestNumber = arrayT[x];

			if(smallestNumber != 0)
				for(int x = 0; x < arrayT.Length; x++)
					arrayT[x] = arrayT[x] - smallestNumber;

			if(lensCenter - smallestNumber > 10)
				for(int x = 0; x < arrayT.Length; x++)
					arrayT[x] = arrayT[x] * (lensCenter) / (lensCenter - smallestNumber);
		
		
			double	biggestNumber = int.MinValue;
			for(int x = 0; x < arrayB.Length; x++)
				if(biggestNumber < arrayB[x])
					biggestNumber = arrayB[x];

			for(int x = 0; x < arrayB.Length; x++)
				arrayB[x] = biggestNumber - arrayB[x];

			if(biggestNumber - lensCenter > 10)
				for(int x = 0; x < arrayB.Length; x++)
					arrayB[x] = arrayB[x] * (pageParams.Clip.RectangleNotAngled.Bottom - lensCenter) / (biggestNumber - lensCenter);
		}
		#endregion

		#region GetLensCenter()
		// c = yT + ((yB - yT) * dB) / (2 * dT)
		private static int GetLensCenter(PageParams pageParams)
		{	
			int		tMinY = pageParams.Bookfolding.TopCurve.Points[1].Y;
			int		tMaxY = pageParams.Bookfolding.TopCurve.Points[1].Y;
			int		bMinY = pageParams.Bookfolding.BottomCurve.Points[1].Y;
			int		bMaxY = pageParams.Bookfolding.BottomCurve.Points[1].Y;

			for(int i = 2; i < 6; i++)
			{
				if(tMinY > pageParams.Bookfolding.TopCurve.Points[i].Y)
					tMinY = pageParams.Bookfolding.TopCurve.Points[i].Y;

				if(tMaxY < pageParams.Bookfolding.TopCurve.Points[i].Y)
					tMaxY = pageParams.Bookfolding.TopCurve.Points[i].Y;
			}
			
			for(int i = 2; i < 6; i++)
			{
				if(bMinY > pageParams.Bookfolding.BottomCurve.Points[i].Y)
					bMinY = pageParams.Bookfolding.BottomCurve.Points[i].Y;

				if(bMaxY < pageParams.Bookfolding.BottomCurve.Points[i].Y)
					bMaxY = pageParams.Bookfolding.BottomCurve.Points[i].Y;
			}

			int		dT = tMaxY - tMinY;
			int		dB = bMaxY - bMinY;
			int		lensCenter;

			if(dT < dB)
				lensCenter = tMaxY + ((bMinY - tMaxY) * dT) / (2 * dB);
			else if(dT > dB)
				lensCenter = bMinY - ((bMinY - tMaxY) * dB) / (2 * dT);
			else
				lensCenter = bMinY - (bMinY - tMaxY) / 2;

			return lensCenter;
		}
		#endregion

	}
}
