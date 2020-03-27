using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using ImageProcessing.PageObjects;

namespace ImageProcessing
{
	public static class Rotation
	{
		//PUBLIC METHODS
		#region public methods

		#region GetAngleFromObjects()
		/*public static double GetAngleFromObjects(Size clipSize, float resolution, Symbols objects)
		{
			Lines lines = ObjectLocator.FindLines(Words.FindWords(objects), objects);
			ArrayList validAngles = new ArrayList();
			foreach (Line line in lines)
			{
				if (line.Width > (clipSize.Width / 2))
				{
					validAngles.Add(line.Angle);
				}
			}
			if (validAngles.Count > 0)
			{
				validAngles.Sort();
				double angle = (double)validAngles[validAngles.Count / 2];
				if ((angle > 0.005) || (angle < -0.005))
				{
					return angle;
				}
			}
			return 0.0;
		}*/
		#endregion

		#region GetAngleFromObjects()
		/// <summary>
		/// Returns angle and confidence. Angle is negative in the counter-clockvise direction.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="words"></param>
		/// <param name="pictures"></param>
		/// <param name="delimiters"></param>
		/// <param name="confidence"></param>
		/// <returns></returns>
		public static double GetAngleFromObjects(ItPage page, Words words, Pictures pictures, Delimiters delimiters, out float confidence)
		{
			double a;
			double w;
			double angle = 0.0;
			double weightSum = 0.0;

			if (pictures.GetSkew(page.ClipRect.Size, out a, out w))
			{
				angle += a * w;
				weightSum += w;
			}

			if (words.GetSkew(out a, out w))
			{
				angle += a * w;
				weightSum += w;
			}

			if (delimiters.GetSkew(page.ClipRect.Size, out a, out w))
			{
				angle += a * w;
				weightSum += w;
			}

			if (weightSum != 0.0)
				angle /= weightSum;

			confidence = (float) Math.Min(1, weightSum);
			return angle;
		}
		#endregion

		#region Rotate()
		/// <summary>
		/// Returns bitmap rotated by 'angle' clockwise with "blind" corners filled by r,g,b color.
		/// </summary>
		/// <param name="bitmat">32bpp, 24bpp 8bpp grayscale or 1bpp image</param>
		/// <param name="angle">Angle in radians, clockwise</param>
		/// <param name="r">Color image - red background component; Grayscale image - gray level. Bitonal image - 1 if r > 0.</param>
		/// <param name="g">Color image - green background component.</param>
		/// <param name="b">Color image - blue background component.</param>
		/// <returns></returns>
		public static Bitmap Rotate(Bitmap bitmap, double angle, byte r, byte g, byte b)
		{
			return RotateClip(bitmap, angle, new Rectangle(0, 0, bitmap.Width, bitmap.Height), r, g, b);
		}

		/// <summary>
		/// Returns bitmap rotated by 'angle' clockwise with "blind" corners filled by r,g,b color.
		/// </summary>
		/// <param name="bitmat">32bpp, 24bpp 8bpp grayscale or 1bpp image</param>
		/// <param name="angle">Angle in radians, clockwise</param>
		/// <param name="r">Color image - red background component; Grayscale image - gray level. Bitonal image - 1 if r > 0.</param>
		/// <param name="g">Color image - green background component.</param>
		/// <param name="b">Color image - blue background component.</param>
		/// <param name="rotationCenter">Center point of rotation.</param>
		/// <returns></returns>
		public static Bitmap Rotate(Bitmap bitmap, double angle, byte r, byte g, byte b, Point rotationCenter)
		{
			return RotateClip(bitmap, angle, new Rectangle(0, 0, bitmap.Width, bitmap.Height), r, g, b, rotationCenter);
		}
		#endregion

		#region RotateClip()
		/// <summary>
		/// Returns bitmap rotated by 'angle' clockwise with "blind" corners filled by r,g,b color.
		/// </summary>
		/// <param name="bitmat">32bpp, 24bpp 8bpp grayscale or 1bpp image</param>
		/// <param name="angle">Angle in radians, clockwise</param>
		/// <param name="clip">Clip of bitmap to rotate. Rectangle.Empty to rotate entire image.</param>
		/// <param name="r">Color image - red background component; Grayscale image - gray level. Bitonal image - 1 if r > 0.</param>
		/// <param name="g">Color image - green background component.</param>
		/// <param name="b">Color image - blue background component.</param>
		/// <returns></returns>
		public static Bitmap RotateClip(Bitmap bitmap, double angle, Rectangle clip, byte r, byte g, byte b)
		{
			return RotateClip(bitmap, angle, clip, r, g, b, new Point(clip.X + clip.Width / 2, clip.Y + clip.Height / 2));
		}
	
		/// <summary>
		/// Returns bitmap rotated by 'angle' clockwise with "blind" corners filled by r,g,b color.
		/// </summary>
		/// <param name="bitmat">32bpp, 24bpp 8bpp grayscale or 1bpp image</param>
		/// <param name="angle">Angle in radians, clockwise</param>
		/// <param name="clip">Clip of bitmap to rotate. Rectangle.Empty to rotate entire image.</param>
		/// <param name="r">Color image - red background component; Grayscale image - gray level. Bitonal image - 1 if r > 0.</param>
		/// <param name="g">Color image - green background component.</param>
		/// <param name="b">Color image - blue background component.</param>
		/// <param name="rotationCenter">Center point of rotation.</param>
		/// <returns></returns>
		public static Bitmap RotateClip(Bitmap bitmap, double angle, Rectangle clip, byte r, byte g, byte b, Point rotationCenter)
		{
			Bitmap result;
			if (clip.IsEmpty)
			{
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			}

			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format24bppRgb:
					{
						/*if (((r == 0) && (g == 0)) && (b == 0))
							result = Rotate24bpp(bitmap, clip, angle);
						else*/
						result = Rotate24bppSetBack(bitmap, clip, angle, r, g, b, rotationCenter);
					} break;

				case PixelFormat.Format32bppRgb:
				case PixelFormat.Format32bppArgb:
					{
						/*if (((r == 0) && (g == 0)) && (b == 0))
							result = Rotate32bpp(bitmap, clip, angle);
						else*/
						result = Rotate32bppSetBack(bitmap, clip, angle, r, g, b, rotationCenter);
					} break;

				case PixelFormat.Format8bppIndexed:
					{
						/*if (r == 0)
							result = Rotate8bpp(bitmap, clip, angle);
						else*/
						result = Rotate8bppSetBack(bitmap, clip, angle, r, rotationCenter);
					} break;

				case PixelFormat.Format1bppIndexed:
					{
						if (r == 0)
							result = Rotate1bppBlackBack(bitmap, clip, angle, rotationCenter);
						else
							result = Rotate1bppWhiteBack(bitmap, clip, angle, rotationCenter);
					} break;

				default:
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}

			if (result != null)
			{
				Misc.SetBitmapResolution(result, bitmap.HorizontalResolution, bitmap.VerticalResolution);

				if ((result.PixelFormat == PixelFormat.Format1bppIndexed) || (result.PixelFormat == PixelFormat.Format8bppIndexed) || (result.PixelFormat == PixelFormat.Format4bppIndexed))
					result.Palette = bitmap.Palette;
			}

			return result;
		}
		#endregion

		#region RotatePoint()
		/// <summary>
		/// Rotates point around center point, positive angle is in clockwise direction.
		/// </summary>
		/// <param name="p"></param>
		/// <param name="centerPoint"></param>
		/// <param name="angle"></param>
		/// <returns></returns>
		public static Point RotatePoint(Point p, Point centerPoint, double angle)
		{
			double beta = Math.Atan2((double)(centerPoint.Y - p.Y), (double)(centerPoint.X - p.X));
			double m = Math.Sqrt((double)(((centerPoint.X - p.X) * (centerPoint.X - p.X)) + ((centerPoint.Y - p.Y) * (centerPoint.Y - p.Y))));
			double xShifted = centerPoint.X - (Math.Cos(beta + angle) * m);
			double yShifted = centerPoint.Y - (Math.Sin(beta + angle) * m);
			return new Point(Convert.ToInt32(xShifted), Convert.ToInt32(yShifted));
		}
		#endregion

		#region GetClip()
		public static Bitmap GetClip(Bitmap source, Point pUL, Point pUR, Point pLL)
		{
			double angle = -Math.Atan2((pUR.Y - pUL.Y), (pUR.X - pUL.X));

			double width = Math.Sqrt((pUR.X - pUL.X) * (pUR.X - pUL.X) + (pUR.Y - pUL.Y) * (pUR.Y - pUL.Y));
			double height = Math.Sqrt((pLL.X - pUL.X) * (pLL.X - pUL.X) + (pLL.Y - pUL.Y) * (pLL.Y - pUL.Y));
			double centerX = pLL.X + (pUR.X - pLL.X) / 2.0;
			double centerY = pLL.Y + (pUR.Y - pLL.Y) / 2.0;

			Rectangle clip = new Rectangle(Convert.ToInt32(centerX - width / 2.0), Convert.ToInt32(centerY - height / 2.0), Convert.ToInt32(width), Convert.ToInt32(height));
			return RotateClip(source, angle, clip, 255, 255, 255);
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region Rotate1bppBlackBack()
		private static unsafe Bitmap Rotate1bppBlackBack(Bitmap source, Rectangle clip, double angle, Point rotationCenter)
		{
			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;
			try
			{
				int sx = rotationCenter.X;// clip.X + (clip.Width / 2);
				int sy = rotationCenter.Y;// clip.Y + (clip.Height / 2);
				double beta = Math.Atan2((double)(sy - clip.Y), (double)(sx - clip.X));
				double m = Math.Sqrt((double)(((sx - clip.X) * (sx - clip.X)) + ((sy - clip.Y) * (sy - clip.Y))));
				double xShifted = sx - (Math.Cos(beta - angle) * m);
				double yShifted = sy - (Math.Sin(beta - angle) * m);
				int resultW = clip.Width;
				int resultH = clip.Height;
				int sourceW = source.Width;
				int sourceH = source.Height;
				int ulCornerX = Convert.ToInt32(xShifted);
				int ulCornerY = Convert.ToInt32(yShifted);
				double xJump = Math.Cos(-angle);
				double yJump = Math.Sin(-angle);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				
				for (int y = 0; y < resultH; y++)
				{
					double sourceX = ulCornerX - (y * yJump);
					int sourceXInt = (int)sourceX;
					double sourceY = ulCornerY + (y * xJump);
					int sourceYInt = (int)sourceY;
					
					for (int x = 0; x < resultW; x++)
					{
						byte* pResultCurrent = (pResult + (y * rStride)) + (x / 8);
						if ((sourceXInt >= 0) && (sourceXInt < sourceW) && (sourceYInt >= 0) && (sourceYInt < sourceH))
						{
							byte* pSourceCurrent = (pSource + (sourceYInt * sStride)) + (sourceXInt / 8);
							int i = ((int)0x80) >> (sourceXInt & 7);
							
							if ((pSourceCurrent[0] & i) > 0)
								pResultCurrent[0] = (byte)(pResultCurrent[0] | ((byte)(((int)0x80) >> (x & 7))));
						}
						
						sourceX += xJump;
						sourceXInt = (int)sourceX;
						sourceY += yJump;
						sourceYInt = (int)sourceY;
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}
			return result;
		}
		#endregion

		#region Rotate1bppWhiteBack()
		private static unsafe Bitmap Rotate1bppWhiteBack(Bitmap source, Rectangle clip, double angle, Point rotationCenter)
		{
			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;
		
			try
			{
				int		sx = rotationCenter.X;// clip.X + (clip.Width / 2);
				int		sy = rotationCenter.Y;// clip.Y + (clip.Height / 2);
				double	beta = Math.Atan2((double)(sy - clip.Y), (double)(sx - clip.X));
				double	m = Math.Sqrt((double)(((sx - clip.X) * (sx - clip.X)) + ((sy - clip.Y) * (sy - clip.Y))));
				double	xShifted = sx - (Math.Cos(beta - angle) * m);
				double	yShifted = sy - (Math.Sin(beta - angle) * m);
				
				int resultW = clip.Width;
				int resultH = clip.Height;
				int sourceW = source.Width;
				int sourceH = source.Height;
				int ulCornerX = Convert.ToInt32(xShifted);
				int ulCornerY = Convert.ToInt32(yShifted);
				
				double xJump = Math.Cos(-angle);
				double yJump = Math.Sin(-angle);
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				
				for (int y = 0; y < resultH; y++)
				{
					double sourceX = ulCornerX - (y * yJump);
					int sourceXInt = (int)sourceX;
					double sourceY = ulCornerY + (y * xJump);
					int sourceYInt = (int)sourceY;
					
					for (int x = 0; x < resultW; x++)
					{
						byte* pResultCurrent = (pResult + (y * rStride)) + (x / 8);
						
						if ((sourceXInt >= 0) && (sourceXInt < sourceW) && (sourceYInt >= 0) && (sourceYInt < sourceH))
						{
							byte* pSourceCurrent = (pSource + (sourceYInt * sStride)) + (sourceXInt / 8);
							//i = 0x80 >> (sourceXInt % 8);
							int i = ((int)0x80) >> (sourceXInt & 7);
							
							if ((pSourceCurrent[0] & i) > 0)
							{
								pResultCurrent[0] = (byte)(pResultCurrent[0] | ((byte)(((int)0x80) >> (x & 7))));
							}
						}
						else
						{
							pResultCurrent[0] = (byte)(pResultCurrent[0] | ((byte)(((int)0x80) >> (x & 7))));
						}
						
						sourceX += xJump;
						sourceXInt = (int)sourceX;
						sourceY += yJump;
						sourceYInt = (int)sourceY;
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region Rotate24bpp()
		private static unsafe Bitmap Rotate24bpp(Bitmap source, Rectangle clip, double angle, Point rotationCenter)
		{
			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;
			try
			{
				int sx = rotationCenter.X;// clip.X + (clip.Width / 2);
				int sy = rotationCenter.Y;// clip.Y + (clip.Height / 2);
				double beta = Math.Atan2((double)(sy - clip.Y), (double)(sx - clip.X));
				double m = Math.Sqrt((double)(((sx - clip.X) * (sx - clip.X)) + ((sy - clip.Y) * (sy - clip.Y))));
				double xShifted = sx - (Math.Cos(beta - angle) * m);
				double yShifted = sy - (Math.Sin(beta - angle) * m);
				int resultW = clip.Width;
				int resultH = clip.Height;
				int sourceW = source.Width;
				int sourceH = source.Height;
				int ulCornerX = Convert.ToInt32(xShifted);
				int ulCornerY = Convert.ToInt32(yShifted);
				double xJump = Math.Cos(-angle);
				double yJump = Math.Sin(-angle);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				for (int y = 0; y < resultH; y++)
				{
					double sourceX = ulCornerX - (y * yJump);
					int sourceXInt = (int)sourceX;
					double sourceY = ulCornerY + (y * xJump);
					int sourceYInt = (int)sourceY;
					byte* pResultCurrent = pResult + (y * rStride);
					for (int x = 0; x < resultW; x++)
					{
						double xRest = sourceX - sourceXInt;
						if (xRest < 0.0)
						{
							xRest++;
						}
						if (xRest < 1E-06)
						{
							xRest = 0.0;
						}
						if (xRest > 0.999999)
						{
							sourceXInt++;
							sourceX = sourceXInt;
							xRest = 0.0;
						}
						if ((((sourceXInt >= 0) && (sourceXInt < sourceW)) && (sourceYInt >= 0)) && (sourceYInt < sourceH))
						{
							double yRest = sourceY - sourceYInt;
							if (yRest < 0.0)
							{
								yRest++;
							}
							else if (yRest < 1E-06)
							{
								yRest = 0.0;
							}
							else if (yRest > 0.999999)
							{
								sourceYInt++;
								sourceY = sourceYInt;
								yRest = 0.0;
							}
							if (sourceYInt < sourceH)
							{
								byte* pSourceCurrent = (pSource + (sourceYInt * sStride)) + (sourceXInt * 3);
								if (xRest == 0.0)
								{
									if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
									{
										pResultCurrent[0] = pSourceCurrent[0];
										pResultCurrent[1] = pSourceCurrent[1];
										pResultCurrent[2] = pSourceCurrent[2];
									}
									else
									{
										pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - yRest)) + (pSourceCurrent[sStride] * yRest));
										pResultCurrent[1] = (byte)((pSourceCurrent[1] * (1.0 - yRest)) + (pSourceCurrent[sStride + 1] * yRest));
										pResultCurrent[2] = (byte)((pSourceCurrent[2] * (1.0 - yRest)) + (pSourceCurrent[sStride + 2] * yRest));
									}
								}
								else if (sourceXInt >= (sourceW - 1))
								{
									pResultCurrent[0] = pSourceCurrent[0];
									pResultCurrent[1] = pSourceCurrent[1];
									pResultCurrent[2] = pSourceCurrent[2];
								}
								else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
								{
									pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - xRest)) + (pSourceCurrent[3] * xRest));
									pResultCurrent[1] = (byte)((pSourceCurrent[1] * (1.0 - xRest)) + (pSourceCurrent[4] * xRest));
									pResultCurrent[2] = (byte)((pSourceCurrent[2] * (1.0 - xRest)) + (pSourceCurrent[5] * xRest));
								}
								else
								{
									pResultCurrent[0] = (byte)(((((pSourceCurrent[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[3] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 3] * xRest) * yRest));
									pResultCurrent[1] = (byte)(((((pSourceCurrent[1] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[4] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 1] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 4] * xRest) * yRest));
									pResultCurrent[2] = (byte)(((((pSourceCurrent[2] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[5] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 2] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 5] * xRest) * yRest));
								}
							}
						}
						pResultCurrent += 3;
						sourceX += xJump;
						sourceXInt = (int)sourceX;
						sourceY += yJump;
						sourceYInt = (int)sourceY;
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}
			return result;
		}
		#endregion

		#region Rotate24bppSetBack()
		private static unsafe Bitmap Rotate24bppSetBack(Bitmap source, Rectangle clip, double angle, byte r, byte g, byte b, Point rotationCenter)
		{
			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;

			try
			{
				int sx = rotationCenter.X;// clip.X + (clip.Width / 2);
				int sy = rotationCenter.Y;// clip.Y + (clip.Height / 2);
				double beta = Math.Atan2((double)(sy - clip.Y), (double)(sx - clip.X));
				double	m = Math.Sqrt((double)(((sx - clip.X) * (sx - clip.X)) + ((sy - clip.Y) * (sy - clip.Y))));
				double	xShifted = sx - (Math.Cos(beta - angle) * m);
				double	yShifted = sy - (Math.Sin(beta - angle) * m);
				
				int		resultW = clip.Width;
				int		resultH = clip.Height;
				int		sourceW = source.Width;
				int		sourceH = source.Height;
				int		ulCornerX = Convert.ToInt32(xShifted);
				int		ulCornerY = Convert.ToInt32(yShifted);
				double	xJump = Math.Cos(-angle);
				double	yJump = Math.Sin(-angle);
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				
				int		sStride = sourceData.Stride;
				int		rStride = resultData.Stride;
				byte*	pSource = (byte*)sourceData.Scan0.ToPointer();
				byte*	pResult = (byte*)resultData.Scan0.ToPointer();
				
				for (int y = 0; y < resultH; y++)
				{
					double sourceX = ulCornerX - (y * yJump);
					int sourceXInt = (int)sourceX;
					double sourceY = ulCornerY + (y * xJump);
					int sourceYInt = (int)sourceY;
					byte* pResultCurrent = pResult + (y * rStride);
					
					for (int x = 0; x < resultW; x++)
					{
						double xRest = sourceX - sourceXInt;
						
						if (xRest < 0.0)
							xRest++;
						if (xRest < 1E-06)
							xRest = 0.0;
						if (xRest > 0.999999)
						{
							sourceXInt++;
							sourceX = sourceXInt;
							xRest = 0.0;
						}
						
						if ((((sourceXInt >= 0) && (sourceXInt < sourceW)) && (sourceYInt >= 0)) && (sourceYInt < sourceH))
						{
							double yRest = sourceY - sourceYInt;
							
							if (yRest < 0.0)
								yRest++;
							else if (yRest < 1E-06)
								yRest = 0.0;
							else if (yRest > 0.999999)
							{
								sourceYInt++;
								sourceY = sourceYInt;
								yRest = 0.0;
							}

							if (sourceYInt < sourceH)
							{
								byte* pSourceCurrent = (pSource + (sourceYInt * sStride)) + (sourceXInt * 3);
								
								if (xRest == 0.0)
								{
									if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
									{
										pResultCurrent[0] = pSourceCurrent[0];
										pResultCurrent[1] = pSourceCurrent[1];
										pResultCurrent[2] = pSourceCurrent[2];
									}
									else
									{
										pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - yRest)) + (pSourceCurrent[sStride] * yRest));
										pResultCurrent[1] = (byte)((pSourceCurrent[1] * (1.0 - yRest)) + (pSourceCurrent[sStride + 1] * yRest));
										pResultCurrent[2] = (byte)((pSourceCurrent[2] * (1.0 - yRest)) + (pSourceCurrent[sStride + 2] * yRest));
									}
								}
								else if (sourceXInt >= (sourceW - 1))
								{
									pResultCurrent[0] = pSourceCurrent[0];
									pResultCurrent[1] = pSourceCurrent[1];
									pResultCurrent[2] = pSourceCurrent[2];
								}
								else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
								{
									pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - xRest)) + (pSourceCurrent[3] * xRest));
									pResultCurrent[1] = (byte)((pSourceCurrent[1] * (1.0 - xRest)) + (pSourceCurrent[4] * xRest));
									pResultCurrent[2] = (byte)((pSourceCurrent[2] * (1.0 - xRest)) + (pSourceCurrent[5] * xRest));
								}
								else
								{
									pResultCurrent[0] = (byte)(((((pSourceCurrent[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[3] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 3] * xRest) * yRest));
									pResultCurrent[1] = (byte)(((((pSourceCurrent[1] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[4] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 1] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 4] * xRest) * yRest));
									pResultCurrent[2] = (byte)(((((pSourceCurrent[2] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[5] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 2] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 5] * xRest) * yRest));
								}
							}
							else
							{
								pResultCurrent[0] = b;
								pResultCurrent[1] = g;
								pResultCurrent[2] = r;
							}
						}
						else
						{
							pResultCurrent[0] = b;
							pResultCurrent[1] = g;
							pResultCurrent[2] = r;
						}

						pResultCurrent += 3;
						sourceX += xJump;
						sourceXInt = (int)sourceX;
						sourceY += yJump;
						sourceYInt = (int)sourceY;
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}
			return result;
		}
		#endregion

		#region Rotate32bpp()
		/// <summary>
		/// Angle in radians. Result bitmap size is equal to clip size.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="clip"></param>
		/// <param name="angle">in radians</param>
		/// <param name="rotationCenter"></param>
		/// <returns></returns>
		public static unsafe Bitmap Rotate32bpp(Bitmap source, Rectangle clip, double angle, Point rotationCenter)
		{
			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;
			try
			{
				int sx = rotationCenter.X;// clip.X + (clip.Width / 2);
				int sy = rotationCenter.Y;// clip.Y + (clip.Height / 2);
				double beta = Math.Atan2((double)(sy - clip.Y), (double)(sx - clip.X));
				double m = Math.Sqrt((double)(((sx - clip.X) * (sx - clip.X)) + ((sy - clip.Y) * (sy - clip.Y))));
				double xShifted = sx - (Math.Cos(beta - angle) * m);
				double yShifted = sy - (Math.Sin(beta - angle) * m);

				int resultW = clip.Width;
				int resultH = clip.Height;
				int sourceW = source.Width;
				int sourceH = source.Height;

				int ulCornerX = Convert.ToInt32(xShifted);
				int ulCornerY = Convert.ToInt32(yShifted);
				double xJump = Math.Cos(-angle);
				double yJump = Math.Sin(-angle);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();

				for (int y = 0; y < resultH; y++)
				{
					double sourceX = ulCornerX - (y * yJump);
					int sourceXInt = (int)sourceX;
					double sourceY = ulCornerY + (y * xJump);
					int sourceYInt = (int)sourceY;
					byte* pResultCurrent = pResult + (y * rStride);
					for (int x = 0; x < resultW; x++)
					{
						double xRest = sourceX - sourceXInt;
						if (xRest < 0.0)
						{
							xRest++;
						}
						if (xRest < 1E-06)
						{
							xRest = 0.0;
						}
						if (xRest > 0.999999)
						{
							sourceXInt++;
							sourceX = sourceXInt;
							xRest = 0.0;
						}
						if ((((sourceXInt >= 0) && (sourceXInt < sourceW)) && (sourceYInt >= 0)) && (sourceYInt < sourceH))
						{
							double yRest = sourceY - sourceYInt;
							if (yRest < 0.0)
							{
								yRest++;
							}
							else if (yRest < 1E-06)
							{
								yRest = 0.0;
							}
							else if (yRest > 0.999999)
							{
								sourceYInt++;
								sourceY = sourceYInt;
								yRest = 0.0;
							}
							if (sourceYInt < sourceH)
							{
								byte* pSourceCurrent = (pSource + (sourceYInt * sStride)) + (sourceXInt * 4);
								if (xRest == 0.0)
								{
									if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
									{
										pResultCurrent[0] = pSourceCurrent[0];
										pResultCurrent[1] = pSourceCurrent[1];
										pResultCurrent[2] = pSourceCurrent[2];
										pResultCurrent[3] = pSourceCurrent[3];
									}
									else
									{
										pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - yRest)) + (pSourceCurrent[sStride] * yRest));
										pResultCurrent[1] = (byte)((pSourceCurrent[1] * (1.0 - yRest)) + (pSourceCurrent[sStride + 1] * yRest));
										pResultCurrent[2] = (byte)((pSourceCurrent[2] * (1.0 - yRest)) + (pSourceCurrent[sStride + 2] * yRest));
										pResultCurrent[2] = (byte)((pSourceCurrent[3] * (1.0 - yRest)) + (pSourceCurrent[sStride + 3] * yRest));
									}
								}
								else if (sourceXInt >= (sourceW - 1))
								{
									pResultCurrent[0] = pSourceCurrent[0];
									pResultCurrent[1] = pSourceCurrent[1];
									pResultCurrent[2] = pSourceCurrent[2];
									pResultCurrent[3] = pSourceCurrent[3];
								}
								else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
								{
									pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - xRest)) + (pSourceCurrent[4] * xRest));
									pResultCurrent[1] = (byte)((pSourceCurrent[1] * (1.0 - xRest)) + (pSourceCurrent[5] * xRest));
									pResultCurrent[2] = (byte)((pSourceCurrent[2] * (1.0 - xRest)) + (pSourceCurrent[6] * xRest));
									pResultCurrent[3] = (byte)((pSourceCurrent[3] * (1.0 - xRest)) + (pSourceCurrent[7] * xRest));
								}
								else
								{
									pResultCurrent[0] = (byte)(((((pSourceCurrent[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[4] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 4] * xRest) * yRest));
									pResultCurrent[1] = (byte)(((((pSourceCurrent[1] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[5] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 1] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 5] * xRest) * yRest));
									pResultCurrent[2] = (byte)(((((pSourceCurrent[2] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[6] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 2] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 6] * xRest) * yRest));
									pResultCurrent[3] = (byte)(((((pSourceCurrent[3] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[7] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 3] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 7] * xRest) * yRest));
								}
							}
						}
						pResultCurrent += 4;
						sourceX += xJump;
						sourceXInt = (int)sourceX;
						sourceY += yJump;
						sourceYInt = (int)sourceY;
					}
				}
			}
			finally
			{
				if (sourceData != null)
				{
					source.UnlockBits(sourceData);
				}
				if (resultData != null)
				{
					result.UnlockBits(resultData);
				}
			}
			return result;
		}
		#endregion

		#region Rotate32bpp()
		/// <summary>
		/// Angle in radians. Rotates entire image, resulting bitmap is enlarged to contain the whole image. Empty areas are transparent.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="angle">in radians</param>
		/// <param name="rotationCenter"></param>
		/// <returns></returns>
		public static unsafe Bitmap Rotate32bpp(Bitmap source, double angle)
		{
			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;
			try
			{
				int sourceW = source.Width;
				int sourceH = source.Height;

				Point centerPoint = new Point(sourceW / 2, sourceH / 2);
				Point pUL = RotatePoint(new Point(0, 0), centerPoint, angle);
				Point pUR = RotatePoint(new Point(source.Width, 0), centerPoint, angle);
				Point pLL = RotatePoint(new Point(0, source.Height), centerPoint, angle);
				Point pLR = RotatePoint(new Point(source.Width, source.Height), centerPoint, angle);

				int minX = Math.Min(pLR.X, Math.Min(pLL.X, Math.Min(pUL.X, pUR.X)));
				int minY = Math.Min(pLR.Y, Math.Min(pLL.Y, Math.Min(pUL.Y, pUR.Y)));
				int maxX = Math.Max(pLR.X, Math.Max(pLL.X, Math.Max(pUL.X, pUR.X)));
				int maxY = Math.Max(pLR.Y, Math.Max(pLL.Y, Math.Max(pUL.Y, pUR.Y)));

				int resultW = maxX - minX;
				int resultH = maxY - minY;

				int sx = resultW / 2;
				int sy = resultH / 2;

				double beta = Math.Atan2((double)(sy - 0), (double)(sx - 0));
				double m = Math.Sqrt((double)(((sx - 0) * (sx - 0)) + ((sy - 0) * (sy - 0))));
				double xShifted = sx - (Math.Cos(beta - angle) * m);
				double yShifted = sy - (Math.Sin(beta - angle) * m);

				int ulCornerX = Convert.ToInt32(xShifted) - (resultW - sourceW) / 2;
				int ulCornerY = Convert.ToInt32(yShifted) - (resultH - sourceH) / 2;
				double xJump = Math.Cos(-angle);
				double yJump = Math.Sin(-angle);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

				result = new Bitmap(resultW, resultH, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();

				for (int y = 0; y < resultH; y++)
				{
					double		sourceX = ulCornerX - (y * yJump);
					int			sourceXInt = (int)sourceX;
					double		sourceY = ulCornerY + (y * xJump);
					int			sourceYInt = (int)sourceY;
					byte*		pResultCurrent = pResult + (y * rStride);

					for (int x = 0; x < resultW; x++)
					{
						double xRest = sourceX - sourceXInt;

						if (xRest < 0.0)
						{
							xRest++;
						}
						if (xRest < 1E-06)
						{
							xRest = 0.0;
						}
						if (xRest > 0.999999)
						{
							sourceXInt++;
							sourceX = sourceXInt;
							xRest = 0.0;
						}

						if ((((sourceXInt >= 0) && (sourceXInt < sourceW)) && (sourceYInt >= 0)) && (sourceYInt < sourceH))
						{
							double yRest = sourceY - sourceYInt;
							if (yRest < 0.0)
							{
								yRest++;
							}
							else if (yRest < 1E-06)
							{
								yRest = 0.0;
							}
							else if (yRest > 0.999999)
							{
								sourceYInt++;
								sourceY = sourceYInt;
								yRest = 0.0;
							}
							if (sourceYInt < sourceH)
							{
								byte* pSourceCurrent = (pSource + (sourceYInt * sStride)) + (sourceXInt * 4);
								if (xRest == 0.0)
								{
									if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
									{
										pResultCurrent[0] = pSourceCurrent[0];
										pResultCurrent[1] = pSourceCurrent[1];
										pResultCurrent[2] = pSourceCurrent[2];
										pResultCurrent[3] = pSourceCurrent[3];
									}
									else
									{
										pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - yRest)) + (pSourceCurrent[sStride] * yRest));
										pResultCurrent[1] = (byte)((pSourceCurrent[1] * (1.0 - yRest)) + (pSourceCurrent[sStride + 1] * yRest));
										pResultCurrent[2] = (byte)((pSourceCurrent[2] * (1.0 - yRest)) + (pSourceCurrent[sStride + 2] * yRest));
										pResultCurrent[2] = (byte)((pSourceCurrent[3] * (1.0 - yRest)) + (pSourceCurrent[sStride + 3] * yRest));
									}
								}
								else if (sourceXInt >= (sourceW - 1))
								{
									pResultCurrent[0] = pSourceCurrent[0];
									pResultCurrent[1] = pSourceCurrent[1];
									pResultCurrent[2] = pSourceCurrent[2];
									pResultCurrent[3] = pSourceCurrent[3];
								}
								else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
								{
									pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - xRest)) + (pSourceCurrent[4] * xRest));
									pResultCurrent[1] = (byte)((pSourceCurrent[1] * (1.0 - xRest)) + (pSourceCurrent[5] * xRest));
									pResultCurrent[2] = (byte)((pSourceCurrent[2] * (1.0 - xRest)) + (pSourceCurrent[6] * xRest));
									pResultCurrent[3] = (byte)((pSourceCurrent[3] * (1.0 - xRest)) + (pSourceCurrent[7] * xRest));
								}
								else
								{
									pResultCurrent[0] = (byte)(((((pSourceCurrent[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[4] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 4] * xRest) * yRest));
									pResultCurrent[1] = (byte)(((((pSourceCurrent[1] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[5] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 1] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 5] * xRest) * yRest));
									pResultCurrent[2] = (byte)(((((pSourceCurrent[2] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[6] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 2] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 6] * xRest) * yRest));
									pResultCurrent[3] = (byte)(((((pSourceCurrent[3] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[7] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 3] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 7] * xRest) * yRest));
								}
							}
						}

						pResultCurrent += 4;
						sourceX += xJump;
						sourceXInt = (int)sourceX;
						sourceY += yJump;
						sourceYInt = (int)sourceY;
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);

				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region Rotate32bppSetBack()
		private static unsafe Bitmap Rotate32bppSetBack(Bitmap source, Rectangle clip, double angle, byte r, byte g, byte b, Point rotationCenter)
		{
			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;
			try
			{
				int sx = rotationCenter.X;// clip.X + (clip.Width / 2);
				int sy = rotationCenter.Y;// clip.Y + (clip.Height / 2);
				double beta = Math.Atan2((double)(sy - clip.Y), (double)(sx - clip.X));
				double m = Math.Sqrt((double)(((sx - clip.X) * (sx - clip.X)) + ((sy - clip.Y) * (sy - clip.Y))));
				double xShifted = sx - (Math.Cos(beta - angle) * m);
				double yShifted = sy - (Math.Sin(beta - angle) * m);
				int resultW = clip.Width;
				int resultH = clip.Height;
				int sourceW = source.Width;
				int sourceH = source.Height;
				int ulCornerX = Convert.ToInt32(xShifted);
				int ulCornerY = Convert.ToInt32(yShifted);
				double xJump = Math.Cos(-angle);
				double yJump = Math.Sin(-angle);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				for (int y = 0; y < resultH; y++)
				{
					double sourceX = ulCornerX - (y * yJump);
					int sourceXInt = (int)sourceX;
					double sourceY = ulCornerY + (y * xJump);
					int sourceYInt = (int)sourceY;
					byte* pResultCurrent = pResult + (y * rStride);
					for (int x = 0; x < resultW; x++)
					{
						double xRest = sourceX - sourceXInt;
						if (xRest < 0.0)
						{
							xRest++;
						}
						if (xRest < 1E-06)
						{
							xRest = 0.0;
						}
						if (xRest > 0.999999)
						{
							sourceXInt++;
							sourceX = sourceXInt;
							xRest = 0.0;
						}
						if ((((sourceXInt >= 0) && (sourceXInt < sourceW)) && (sourceYInt >= 0)) && (sourceYInt < sourceH))
						{
							double yRest = sourceY - sourceYInt;
							if (yRest < 0.0)
							{
								yRest++;
							}
							else if (yRest < 1E-06)
							{
								yRest = 0.0;
							}
							else if (yRest > 0.999999)
							{
								sourceYInt++;
								sourceY = sourceYInt;
								yRest = 0.0;
							}
							if (sourceYInt < sourceH)
							{
								byte* pSourceCurrent = (pSource + (sourceYInt * sStride)) + (sourceXInt * 4);
								if (xRest == 0.0)
								{
									if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
									{
										pResultCurrent[0] = pSourceCurrent[0];
										pResultCurrent[1] = pSourceCurrent[1];
										pResultCurrent[2] = pSourceCurrent[2];
										pResultCurrent[3] = pSourceCurrent[3];
									}
									else
									{
										pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - yRest)) + (pSourceCurrent[sStride] * yRest));
										pResultCurrent[1] = (byte)((pSourceCurrent[1] * (1.0 - yRest)) + (pSourceCurrent[sStride + 1] * yRest));
										pResultCurrent[2] = (byte)((pSourceCurrent[2] * (1.0 - yRest)) + (pSourceCurrent[sStride + 2] * yRest));
										pResultCurrent[2] = (byte)((pSourceCurrent[3] * (1.0 - yRest)) + (pSourceCurrent[sStride + 3] * yRest));
									}
								}
								else if (sourceXInt >= (sourceW - 1))
								{
									pResultCurrent[0] = pSourceCurrent[0];
									pResultCurrent[1] = pSourceCurrent[1];
									pResultCurrent[2] = pSourceCurrent[2];
									pResultCurrent[3] = pSourceCurrent[3];
								}
								else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
								{
									pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - xRest)) + (pSourceCurrent[4] * xRest));
									pResultCurrent[1] = (byte)((pSourceCurrent[1] * (1.0 - xRest)) + (pSourceCurrent[5] * xRest));
									pResultCurrent[2] = (byte)((pSourceCurrent[2] * (1.0 - xRest)) + (pSourceCurrent[6] * xRest));
									pResultCurrent[3] = (byte)((pSourceCurrent[3] * (1.0 - xRest)) + (pSourceCurrent[7] * xRest));
								}
								else
								{
									pResultCurrent[0] = (byte)(((((pSourceCurrent[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[4] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 4] * xRest) * yRest));
									pResultCurrent[1] = (byte)(((((pSourceCurrent[1] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[5] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 1] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 5] * xRest) * yRest));
									pResultCurrent[2] = (byte)(((((pSourceCurrent[2] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[6] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 2] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 6] * xRest) * yRest));
									pResultCurrent[3] = (byte)(((((pSourceCurrent[3] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[7] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride + 3] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 7] * xRest) * yRest));
								}
							}
							else
							{
								pResultCurrent[0] = b;
								pResultCurrent[1] = g;
								pResultCurrent[2] = r;
							}
						}
						else
						{
							pResultCurrent[0] = b;
							pResultCurrent[1] = g;
							pResultCurrent[2] = r;
						}
						pResultCurrent += 4;
						sourceX += xJump;
						sourceXInt = (int)sourceX;
						sourceY += yJump;
						sourceYInt = (int)sourceY;
					}
				}
			}
			finally
			{
				if (sourceData != null)
				{
					source.UnlockBits(sourceData);
				}
				if (resultData != null)
				{
					result.UnlockBits(resultData);
				}
			}
			return result;
		}
		#endregion

		#region Rotate8bpp()
		/*private static unsafe Bitmap Rotate8bpp(Bitmap source, Rectangle clip, double angle)
		{
			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;
			try
			{
				int sx = clip.X + (clip.Width / 2);
				int sy = clip.Y + (clip.Height / 2);
				double beta = Math.Atan2((double)(sy - clip.Y), (double)(sx - clip.X));
				double m = Math.Sqrt((double)(((sx - clip.X) * (sx - clip.X)) + ((sy - clip.Y) * (sy - clip.Y))));
				double xShifted = sx - (Math.Cos(beta - angle) * m);
				double yShifted = sy - (Math.Sin(beta - angle) * m);
				int resultW = clip.Width;
				int resultH = clip.Height;
				int sourceW = source.Width;
				int sourceH = source.Height;
				int ulCornerX = Convert.ToInt32(xShifted);
				int ulCornerY = Convert.ToInt32(yShifted);
				double xJump = Math.Cos(-angle);
				double yJump = Math.Sin(-angle);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				for (int y = 0; y < resultH; y++)
				{
					double sourceX = ulCornerX - (y * yJump);
					int sourceXInt = (int)sourceX;
					double sourceY = ulCornerY + (y * xJump);
					int sourceYInt = (int)sourceY;
					byte* pResultCurrent = pResult + (y * rStride);
					for (int x = 0; x < resultW; x++)
					{
						double xRest = sourceX - sourceXInt;
						if (xRest < 0.0)
						{
							xRest++;
						}
						else if (xRest < 1E-06)
						{
							xRest = 0.0;
						}
						if (xRest > 0.999999)
						{
							sourceXInt++;
							sourceX = sourceXInt;
							xRest = 0.0;
						}
						if ((((sourceXInt >= 0) && (sourceXInt < sourceW)) && (sourceYInt >= 0)) && (sourceYInt < sourceH))
						{
							double yRest = sourceY - sourceYInt;
							if (yRest < 0.0)
							{
								yRest++;
							}
							else if (yRest < 1E-06)
							{
								yRest = 0.0;
							}
							else if (yRest > 0.999999)
							{
								sourceYInt++;
								sourceY = sourceYInt;
								yRest = 0.0;
							}
							if (sourceYInt < sourceH)
							{
								byte* pSourceCurrent = (pSource + (sourceYInt * sStride)) + sourceXInt;
								if (xRest == 0.0)
								{
									if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
									{
										pResultCurrent[0] = pSourceCurrent[0];
									}
									else
									{
										pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - yRest)) + (pSourceCurrent[sStride] * yRest));
									}
								}
								else if (sourceXInt >= (sourceW - 1))
								{
									pResultCurrent[0] = pSourceCurrent[0];
								}
								else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
								{
									pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - xRest)) + (pSourceCurrent[1] * xRest));
								}
								else
								{
									pResultCurrent[0] = (byte)(((((pSourceCurrent[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[1] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 1] * xRest) * yRest));
								}
							}
						}
						pResultCurrent++;
						sourceX += xJump;
						sourceXInt = (int)sourceX;
						sourceY += yJump;
						sourceYInt = (int)sourceY;
					}
				}
			}
			finally
			{
				if (sourceData != null)
				{
					source.UnlockBits(sourceData);
				}
				if (resultData != null)
				{
					result.UnlockBits(resultData);
				}
			}
			return result;
		}*/
		#endregion

		#region Rotate8bppSetBack()
		private static unsafe Bitmap Rotate8bppSetBack(Bitmap source, Rectangle clip, double angle, byte backColor, Point rotationCenter)
		{
			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;
			try
			{
				int sx = rotationCenter.X;// clip.X + (clip.Width / 2);
				int sy = rotationCenter.Y;// clip.Y + (clip.Height / 2);
				double beta = Math.Atan2((double)(sy - clip.Y), (double)(sx - clip.X));
				double m = Math.Sqrt((double)(((sx - clip.X) * (sx - clip.X)) + ((sy - clip.Y) * (sy - clip.Y))));
				double xShifted = sx - (Math.Cos(beta - angle) * m);
				double yShifted = sy - (Math.Sin(beta - angle) * m);
				int resultW = clip.Width;
				int resultH = clip.Height;
				int sourceW = source.Width;
				int sourceH = source.Height;
				int ulCornerX = Convert.ToInt32(xShifted);
				int ulCornerY = Convert.ToInt32(yShifted);
				double xJump = Math.Cos(-angle);
				double yJump = Math.Sin(-angle);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				
				for (int y = 0; y < resultH; y++)
				{
					double sourceX = ulCornerX - (y * yJump);
					int sourceXInt = (int)sourceX;
					double sourceY = ulCornerY + (y * xJump);
					int sourceYInt = (int)sourceY;
					byte* pResultCurrent = pResult + (y * rStride);
					
					for (int x = 0; x < resultW; x++)
					{
						double xRest = sourceX - sourceXInt;

						if (xRest < 0.0)
						{
							xRest++;
						}
						if (xRest < 1E-06)
						{
							xRest = 0.0;
						}
						if (xRest > 0.999999)
						{
							sourceXInt++;
							sourceX = sourceXInt;
							xRest = 0.0;
						}
						if ((((sourceXInt >= 0) && (sourceXInt < sourceW)) && (sourceYInt >= 0)) && (sourceYInt < sourceH))
						{
							double yRest = sourceY - sourceYInt;
							if (yRest < 0.0)
							{
								yRest++;
							}
							else if (yRest < 1E-06)
							{
								yRest = 0.0;
							}
							else if (yRest > 0.999999)
							{
								sourceYInt++;
								sourceY = sourceYInt;
								yRest = 0.0;
							}
							if (sourceYInt < sourceH)
							{
								byte* pSourceCurrent = (pSource + (sourceYInt * sStride)) + sourceXInt;
								if (xRest == 0.0)
								{
									if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
									{
										pResultCurrent[0] = pSourceCurrent[0];
									}
									else
									{
										pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - yRest)) + (pSourceCurrent[sStride] * yRest));
									}
								}
								else if (sourceXInt >= (sourceW - 1))
								{
									pResultCurrent[0] = pSourceCurrent[0];
								}
								else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
								{
									pResultCurrent[0] = (byte)((pSourceCurrent[0] * (1.0 - xRest)) + (pSourceCurrent[1] * xRest));
								}
								else
								{
									pResultCurrent[0] = (byte)(((((pSourceCurrent[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pSourceCurrent[1] * xRest) * (1.0 - yRest))) + ((pSourceCurrent[sStride] * (1.0 - xRest)) * yRest)) + ((pSourceCurrent[sStride + 1] * xRest) * yRest));
								}
							}
							else
							{
								pResultCurrent[0] = backColor;
							}
						}
						else
						{
							pResultCurrent[0] = backColor;
						}

						pResultCurrent++;
						sourceX += xJump;
						sourceXInt = (int)sourceX;
						sourceY += yJump;
						sourceYInt = (int)sourceY;
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}
			return result;
		}
		#endregion

		#endregion

	}

}
