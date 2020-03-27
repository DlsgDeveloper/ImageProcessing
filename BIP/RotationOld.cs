using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	/*public class RotationOld
	{
		double	rotation;
		
		#region constructor
		public RotationOld(double rotation)
		{
			this.rotation = rotation;
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties
		public double	Value	{get{return this.rotation;}set{this.rotation = value;}}
		public bool		IsRotated	{ get{return this.rotation != 0;}}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region GetPoints()
		public void GetPoints(out Point pointL, out Point pointC, out Point pointR, Rectangle clip)
		{
			//rotation
			if(this.rotation == 0)
			{
				pointL = new Point(clip.X, clip.Y + clip.Height / 2);
				pointC = new Point(clip.X + clip.Width / 2, clip.Y + clip.Height / 2);
				pointR = new Point(clip.Right, clip.Y + clip.Height / 2);
			}
			else
			{
				int	dy = Convert.ToInt32(Math.Tan(this.rotation) * clip.Width / 2);

				pointL = new Point(clip.X, clip.Y + clip.Height / 2 - dy);
				pointC = new Point(clip.X + clip.Width / 2, clip.Y + clip.Height / 2);
				pointR = new Point(clip.Right, clip.Y + clip.Height / 2 + dy);
			}
		}
		#endregion

		#region GetCoveringRectangle()
		public Rectangle GetCoveringRectangle(Rectangle clip)
		{
			Point	pL, pC, pR;

			GetPoints(out pL, out pC, out pR, clip);

			return Rectangle.FromLTRB(Min(pL.X, pR.X), Min(pL.Y, pR.Y), Max(pL.X, pR.X), Max(pL.Y, pR.Y));
		}
		#endregion

		#region RotatePoint()
		public static Point RotatePoint(Point p, Point centerPoint, double angle)
		{			
			angle = -angle;

			double		beta = Math.Atan2(centerPoint.Y - p.Y, centerPoint.X - p.X) * (180 / Math.PI);
			double		m = Math.Sqrt((centerPoint.X - p.X)*(centerPoint.X - p.X) + (centerPoint.Y - p.Y)*(centerPoint.Y - p.Y));
			double		xShifted = centerPoint.X - Math.Cos((beta + angle) * Math.PI / 180) * m;
			double		yShifted = centerPoint.Y - Math.Sin((beta + angle) * Math.PI / 180) * m;	
				
			return new Point(Convert.ToInt32(xShifted), Convert.ToInt32(yShifted));
		}
		#endregion

		#region Rotate()
		/// <summary>
		/// Returns bitmap rotated by 'angle' clockwise with "blind" corners filled by r,g,b color.
		/// </summary>
		/// <param name="bitmat">32bpp, 24bpp 8bpp grayscale or 1bpp image</param>
		/// <param name="angle">Angle in degrees, clockwise</param>
		/// <param name="r">Color image - red background component; Grayscale image - gray level. Bitonal image - 1 if r > 0.</param>
		/// <param name="g">Color image - green background component.</param>
		/// <param name="b">Color image - blue background component.</param>
		/// <returns></returns>
		public static Bitmap Rotate(Bitmap bitmap, double angle, byte r, byte g, byte b)
		{
			return RotateClip(bitmap, angle, new Rectangle(0, 0, bitmap.Width, bitmap.Height), 0, 0, 0);
		}
		#endregion
		
		#region RotateClip()
		/// <summary>
		/// Returns bitmap rotated by 'angle' clockwise with "blind" corners filled by r,g,b color.
		/// </summary>
		/// <param name="bitmat">32bpp, 24bpp 8bpp grayscale or 1bpp image</param>
		/// <param name="angle">Angle in degrees, clockwise</param>
		/// <param name="clip">Clip of bitmap to rotate. Rectangle.Empty to rotate entire image.</param>
		/// <param name="r">Color image - red background component; Grayscale image - gray level. Bitonal image - 1 if r > 0.</param>
		/// <param name="g">Color image - green background component.</param>
		/// <param name="b">Color image - blue background component.</param>
		/// <returns></returns>
		public static Bitmap RotateClip(Bitmap bitmap, double angle, Rectangle	clip, byte r, byte g, byte b)
		{
			Bitmap		result;
			
			if(clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			
			switch(bitmap.PixelFormat)
			{
				case PixelFormat.Format1bppIndexed:
				{
					if(r == 0)
						result = Rotate1bppBlackBack(bitmap, clip, angle);
					else 
						result = Rotate1bppWhiteBack(bitmap, clip, angle);
				} break;
				case PixelFormat.Format8bppIndexed:
				{
					if(r == 0)
						result = Rotate8bpp(bitmap, clip, angle);
					else 
						result = Rotate8bppSetBack(bitmap, clip, angle, r);
				} break;
				case PixelFormat.Format24bppRgb:
				{
					if(r == 0 && g == 0 && b == 0)
						result = Rotate24bpp(bitmap, clip, angle);
					else 
						result = Rotate24bppSetBack(bitmap, clip, angle, r, g, b);
				} break;
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:
				{
					if(r == 0 && g == 0 && b == 0)
						result = Rotate32bpp(bitmap, clip, angle);
					else 
						result = Rotate32bppSetBack(bitmap, clip, angle, r, g, b);
				} break;
				default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}

			if(result != null)
			{
				result.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

				if(result.PixelFormat == PixelFormat.Format1bppIndexed || result.PixelFormat == PixelFormat.Format8bppIndexed)
					result.Palette = bitmap.Palette;
			}

			return result;
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region Min()
		private int Min(int v1, int v2)
		{
			return (v1 < v2) ? v1 : v2;
		}
		#endregion

		#region Max()
		private int Max(int v1, int v2)
		{
			return (v1 > v2) ? v1 : v2;
		}
		#endregion
		
		#region Rotate24bpp()
		private static Bitmap Rotate24bpp(Bitmap source, Rectangle clip, double angle)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			angle = -angle;

			try
			{
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X) * (180 / Math.PI);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				double		xShifted = sx - Math.Cos((beta + angle) * Math.PI / 180) * m;
				double		yShifted = sy - Math.Sin((beta + angle) * Math.PI / 180) * m;	
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32(xShifted);
				int			ulCornerY = Convert.ToInt32(yShifted);

				double	sourceX, sourceY;
				int		sourceXInt, sourceYInt;
				double	xRest, yRest;
				double	xJump = Math.Cos(angle * Math.PI / 180);
				double	yJump = Math.Sin(angle * Math.PI / 180);
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pSourceCurrent;
					byte*	pResultCurrent;

					for(y = 0; y < resultH; y++)
					{
						sourceX = ulCornerX - y * yJump;
						sourceXInt = (int) sourceX;
						sourceY = ulCornerY + y * xJump;
						sourceYInt = (int) sourceY;
						
						pResultCurrent = pResult + y * rStride;

						for(x = 0; x < resultW; x++)
						{
							xRest = sourceX - sourceXInt;

							if(xRest < 0)
								xRest += 1;
							if(xRest < 0.000001)
								xRest = 0;
							if(xRest > .999999)
							{
								sourceXInt += 1;
								sourceX = sourceXInt;
								xRest = 0;
							}

							if(sourceXInt >= 0 && sourceXInt < sourceW && sourceYInt >= 0 && sourceYInt < sourceH)
							{
								yRest = sourceY - sourceYInt;
								if(yRest < 0)
									yRest += 1;
								else if(yRest < 0.000001)
									yRest = 0;
								else if(yRest > .999999)
								{
									sourceYInt += 1;
									sourceY = sourceYInt;
									yRest = 0;
								}
							
								if(sourceYInt < sourceH)
								{
									pSourceCurrent = pSource + sourceYInt * sStride + sourceXInt * 3;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pResultCurrent = *pSourceCurrent;
											pResultCurrent[1] = pSourceCurrent[1];
											pResultCurrent[2] = pSourceCurrent[2];
										}
										else
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1 - yRest) + pSourceCurrent[sStride] * yRest);
											pResultCurrent[1] = (byte) (pSourceCurrent[1] * (1 - yRest) + pSourceCurrent[sStride+1] * yRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[2] * (1 - yRest) + pSourceCurrent[sStride+2] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pResultCurrent = *pSourceCurrent;
											pResultCurrent[1] = pSourceCurrent[1];
											pResultCurrent[2] = pSourceCurrent[2];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1-xRest) + pSourceCurrent[3] * xRest);
											pResultCurrent[1] = (byte) (pSourceCurrent[1] * (1-xRest) + pSourceCurrent[4] * xRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[2] * (1-xRest) + pSourceCurrent[5] * xRest);
										}
										else
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1-yRest) * (1-xRest) + pSourceCurrent[3] * xRest * (1-yRest) + pSourceCurrent[sStride] * (1-xRest) * yRest + pSourceCurrent[sStride+3] * xRest * yRest);
											pResultCurrent[1] = (byte) (pSourceCurrent[1] * (1-yRest) * (1-xRest) + pSourceCurrent[4] * xRest * (1-yRest) + pSourceCurrent[sStride+1] * (1-xRest) * yRest + pSourceCurrent[sStride+4] * xRest * yRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[2] * (1-yRest) * (1-xRest) + pSourceCurrent[5] * xRest * (1-yRest) + pSourceCurrent[sStride+2] * (1-xRest) * yRest + pSourceCurrent[sStride+5] * xRest * yRest);
										}
									}
								}
							}

							pResultCurrent += 3;
							
							sourceX += xJump;
							sourceXInt = (int) sourceX;
							sourceY += yJump;
							sourceYInt = (int) sourceY;
						}
					}
				}
			}
			finally
			{
				if(sourceData != null)
					source.UnlockBits(sourceData);
				if(resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion
		
		#region Rotate24bppSetBack()
		private static Bitmap Rotate24bppSetBack(Bitmap source, Rectangle clip, double angle, byte r, byte g, byte b)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			angle = -angle;

			try
			{
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X) * (180 / Math.PI);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				double		xShifted = sx - Math.Cos((beta + angle) * Math.PI / 180) * m;
				double		yShifted = sy - Math.Sin((beta + angle) * Math.PI / 180) * m;	
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32(xShifted);
				int			ulCornerY = Convert.ToInt32(yShifted);

				double	sourceX, sourceY;
				int		sourceXInt, sourceYInt;
				double	xRest, yRest;
				double	xJump = Math.Cos(angle * Math.PI / 180);
				double	yJump = Math.Sin(angle * Math.PI / 180);
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pSourceCurrent;
					byte*	pResultCurrent;

					for(y = 0; y < resultH; y++)
					{
						sourceX = ulCornerX - y * yJump;
						sourceXInt = (int) sourceX;
						sourceY = ulCornerY + y * xJump;
						sourceYInt = (int) sourceY;
						
						pResultCurrent = pResult + y * rStride;

						for(x = 0; x < resultW; x++)
						{
							xRest = sourceX - sourceXInt;

							if(xRest < 0)
								xRest += 1;
							if(xRest < 0.000001)
								xRest = 0;
							if(xRest > .999999)
							{
								sourceXInt += 1;
								sourceX = sourceXInt;
								xRest = 0;
							}

							if(sourceXInt >= 0 && sourceXInt < sourceW && sourceYInt >= 0 && sourceYInt < sourceH)
							{
								yRest = sourceY - sourceYInt;
								if(yRest < 0)
									yRest += 1;
								else if(yRest < 0.000001)
									yRest = 0;
								else if(yRest > .999999)
								{
									sourceYInt += 1;
									sourceY = sourceYInt;
									yRest = 0;
								}
							
								if(sourceYInt < sourceH)
								{
									pSourceCurrent = pSource + sourceYInt * sStride + sourceXInt * 3;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pResultCurrent = *pSourceCurrent;
											pResultCurrent[1] = pSourceCurrent[1];
											pResultCurrent[2] = pSourceCurrent[2];
										}
										else
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1 - yRest) + pSourceCurrent[sStride] * yRest);
											pResultCurrent[1] = (byte) (pSourceCurrent[1] * (1 - yRest) + pSourceCurrent[sStride+1] * yRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[2] * (1 - yRest) + pSourceCurrent[sStride+2] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pResultCurrent = *pSourceCurrent;
											pResultCurrent[1] = pSourceCurrent[1];
											pResultCurrent[2] = pSourceCurrent[2];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1-xRest) + pSourceCurrent[3] * xRest);
											pResultCurrent[1] = (byte) (pSourceCurrent[1] * (1-xRest) + pSourceCurrent[4] * xRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[2] * (1-xRest) + pSourceCurrent[5] * xRest);
										}
										else
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1-yRest) * (1-xRest) + pSourceCurrent[3] * xRest * (1-yRest) + pSourceCurrent[sStride] * (1-xRest) * yRest + pSourceCurrent[sStride+3] * xRest * yRest);
											pResultCurrent[1] = (byte) (pSourceCurrent[1] * (1-yRest) * (1-xRest) + pSourceCurrent[4] * xRest * (1-yRest) + pSourceCurrent[sStride+1] * (1-xRest) * yRest + pSourceCurrent[sStride+4] * xRest * yRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[2] * (1-yRest) * (1-xRest) + pSourceCurrent[5] * xRest * (1-yRest) + pSourceCurrent[sStride+2] * (1-xRest) * yRest + pSourceCurrent[sStride+5] * xRest * yRest);
										}
									}
								}
								else
								{
									*pResultCurrent = b;
									pResultCurrent[1] = g;
									pResultCurrent[2] = r;
								}
							}
							else
							{
								*pResultCurrent = b;
								pResultCurrent[1] = g;
								pResultCurrent[2] = r;
							}

							pResultCurrent += 3;
							
							sourceX += xJump;
							sourceXInt = (int) sourceX;
							sourceY += yJump;
							sourceYInt = (int) sourceY;
						}
					}
				}
			}
			finally
			{
				if(sourceData != null)
					source.UnlockBits(sourceData);
				if(resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region Rotate8bpp()
		private static Bitmap Rotate8bpp(Bitmap source, Rectangle clip, double angle)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			angle = -angle;

			try
			{
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X) * (180 / Math.PI);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				double		xShifted = sx - Math.Cos((beta + angle) * Math.PI / 180) * m;
				double		yShifted = sy - Math.Sin((beta + angle) * Math.PI / 180) * m;	
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32(xShifted);
				int			ulCornerY = Convert.ToInt32(yShifted);

				double	sourceX, sourceY;
				int		sourceXInt, sourceYInt;
				double	xRest, yRest;
				double	xJump = Math.Cos(angle * Math.PI / 180);
				double	yJump = Math.Sin(angle * Math.PI / 180);
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pSourceCurrent;
					byte*	pResultCurrent;

					for(y = 0; y < resultH; y++)
					{
						sourceX = ulCornerX - y * yJump;
						sourceXInt = (int) sourceX;
						sourceY = ulCornerY + y * xJump;
						sourceYInt = (int) sourceY;
						
						pResultCurrent = pResult + y * rStride;

						for(x = 0; x < resultW; x++)
						{
							xRest = sourceX - sourceXInt;

							if(xRest < 0)
								xRest += 1;
							else if(xRest < 0.000001)
								xRest = 0;

							if(xRest > .999999)
							{
								sourceXInt += 1;
								sourceX = sourceXInt;
								xRest = 0;
							}

							if(sourceXInt >= 0 && sourceXInt < sourceW && sourceYInt >= 0 && sourceYInt < sourceH)
							{
								yRest = sourceY - sourceYInt;
								if(yRest < 0)
									yRest += 1;
								else if(yRest < 0.000001)
									yRest = 0;
								else if(yRest > .999999)
								{
									sourceYInt += 1;
									sourceY = sourceYInt;
									yRest = 0;
								}
							
								if(sourceYInt < sourceH)
								{
									pSourceCurrent = pSource + sourceYInt * sStride + sourceXInt;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
											*pResultCurrent = *pSourceCurrent;
										else
											*pResultCurrent = (byte) (*pSourceCurrent * (1 - yRest) + pSourceCurrent[sStride] * yRest);
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pResultCurrent = (byte) (*pSourceCurrent);
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1-xRest) + pSourceCurrent[1] * xRest);
										}
										else
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1-yRest) * (1-xRest) + 
												pSourceCurrent[1] * xRest * (1-yRest) +
												pSourceCurrent[sStride] * (1-xRest) * yRest + 
												pSourceCurrent[sStride+1] * xRest * yRest);
										}
									}
								}
							}
							
							pResultCurrent++;
							
							sourceX += xJump;
							sourceXInt = (int) sourceX;
							sourceY += yJump;
							sourceYInt = (int) sourceY;
						}
					}
				}
			}
			finally
			{
				if(sourceData != null)
					source.UnlockBits(sourceData);
				if(resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region Rotate8bppSetBack()
		private static Bitmap Rotate8bppSetBack(Bitmap source, Rectangle clip, double angle, byte backColor)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			angle = -angle;

			try
			{
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X) * (180 / Math.PI);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				double		xShifted = sx - Math.Cos((beta + angle) * Math.PI / 180) * m;
				double		yShifted = sy - Math.Sin((beta + angle) * Math.PI / 180) * m;	
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32(xShifted);
				int			ulCornerY = Convert.ToInt32(yShifted);

				double	sourceX, sourceY;
				int		sourceXInt, sourceYInt;
				double	xRest, yRest;
				double	xJump = Math.Cos(angle * Math.PI / 180);
				double	yJump = Math.Sin(angle * Math.PI / 180);
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pSourceCurrent;
					byte*	pResultCurrent;

					for(y = 0; y < resultH; y++)
					{
						sourceX = ulCornerX - y * yJump;
						sourceXInt = (int) sourceX;
						sourceY = ulCornerY + y * xJump;
						sourceYInt = (int) sourceY;
						
						pResultCurrent = pResult + y * rStride;

						for(x = 0; x < resultW; x++)
						{
							xRest = sourceX - sourceXInt;

							if(xRest < 0)
								xRest += 1;
							if(xRest < 0.000001)
								xRest = 0;
							if(xRest > .999999)
							{
								sourceXInt += 1;
								sourceX = sourceXInt;
								xRest = 0;
							}

							if(sourceXInt >= 0 && sourceXInt < sourceW && sourceYInt >= 0 && sourceYInt < sourceH)
							{
								yRest = sourceY - sourceYInt;
								if(yRest < 0)
									yRest += 1;
								else if(yRest < 0.000001)
									yRest = 0;
								else if(yRest > .999999)
								{
									sourceYInt += 1;
									sourceY = sourceYInt;
									yRest = 0;
								}
							
								if(sourceYInt < sourceH)
								{
									pSourceCurrent = pSource + sourceYInt * sStride + sourceXInt;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
											*pResultCurrent = *pSourceCurrent;
										else
											*pResultCurrent = (byte) (*pSourceCurrent * (1 - yRest) + pSourceCurrent[sStride] * yRest);
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pResultCurrent = (byte) (*pSourceCurrent);
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1-xRest) + pSourceCurrent[1] * xRest);
										}
										else
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1-yRest) * (1-xRest) + 
												pSourceCurrent[1] * xRest * (1-yRest) +
												pSourceCurrent[sStride] * (1-xRest) * yRest + 
												pSourceCurrent[sStride+1] * xRest * yRest);
										}
									}
								}
								else
								{
									*pResultCurrent = backColor;
								}
							}
							else
							{
								*pResultCurrent = backColor;
							}
							pResultCurrent++;
							
							sourceX += xJump;
							sourceXInt = (int) sourceX;
							sourceY += yJump;
							sourceYInt = (int) sourceY;
						}
					}
				}
			}
			finally
			{
				if(sourceData != null)
					source.UnlockBits(sourceData);
				if(resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region Rotate32bppSetBack()
		private static Bitmap Rotate32bpp(Bitmap source, Rectangle clip, double angle)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			angle = -angle;

			try
			{
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X) * (180 / Math.PI);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				double		xShifted = sx - Math.Cos((beta + angle) * Math.PI / 180) * m;
				double		yShifted = sy - Math.Sin((beta + angle) * Math.PI / 180) * m;	
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32(xShifted);
				int			ulCornerY = Convert.ToInt32(yShifted);

				double	sourceX, sourceY;
				int		sourceXInt, sourceYInt;
				double	xRest, yRest;
				double	xJump = Math.Cos(angle * Math.PI / 180);
				double	yJump = Math.Sin(angle * Math.PI / 180);
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pSourceCurrent;
					byte*	pResultCurrent;

					for(y = 0; y < resultH; y++)
					{
						sourceX = ulCornerX - y * yJump;
						sourceXInt = (int) sourceX;
						sourceY = ulCornerY + y * xJump;
						sourceYInt = (int) sourceY;
						
						pResultCurrent = pResult + y * rStride;

						for(x = 0; x < resultW; x++)
						{
							xRest = sourceX - sourceXInt;

							if(xRest < 0)
								xRest += 1;
							if(xRest < 0.000001)
								xRest = 0;
							if(xRest > .999999)
							{
								sourceXInt += 1;
								sourceX = sourceXInt;
								xRest = 0;
							}

							if(sourceXInt >= 0 && sourceXInt < sourceW && sourceYInt >= 0 && sourceYInt < sourceH)
							{
								yRest = sourceY - sourceYInt;
								if(yRest < 0)
									yRest += 1;
								else if(yRest < 0.000001)
									yRest = 0;
								else if(yRest > .999999)
								{
									sourceYInt += 1;
									sourceY = sourceYInt;
									yRest = 0;
								}
							
								if(sourceYInt < sourceH)
								{
									pSourceCurrent = pSource + sourceYInt * sStride + sourceXInt * 4;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pResultCurrent = *pSourceCurrent;
											pResultCurrent[1] = pSourceCurrent[1];
											pResultCurrent[2] = pSourceCurrent[2];
											pResultCurrent[3] = pSourceCurrent[3];
										}
										else
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1 - yRest) + pSourceCurrent[sStride] * yRest);
											pResultCurrent[1] = (byte) (pSourceCurrent[1] * (1 - yRest) + pSourceCurrent[sStride+1] * yRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[2] * (1 - yRest) + pSourceCurrent[sStride+2] * yRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[3] * (1 - yRest) + pSourceCurrent[sStride+3] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pResultCurrent = *pSourceCurrent;
											pResultCurrent[1] = pSourceCurrent[1];
											pResultCurrent[2] = pSourceCurrent[2];
											pResultCurrent[3] = pSourceCurrent[3];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1-xRest) + pSourceCurrent[4] * xRest);
											pResultCurrent[1] = (byte) (pSourceCurrent[1] * (1-xRest) + pSourceCurrent[5] * xRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[2] * (1-xRest) + pSourceCurrent[6] * xRest);
											pResultCurrent[3] = (byte) (pSourceCurrent[3] * (1-xRest) + pSourceCurrent[7] * xRest);
										}
										else
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1-yRest) * (1-xRest) + pSourceCurrent[4] * xRest * (1-yRest) + pSourceCurrent[sStride] * (1-xRest) * yRest + pSourceCurrent[sStride+4] * xRest * yRest);
											pResultCurrent[1] = (byte) (pSourceCurrent[1] * (1-yRest) * (1-xRest) + pSourceCurrent[5] * xRest * (1-yRest) + pSourceCurrent[sStride+1] * (1-xRest) * yRest + pSourceCurrent[sStride+5] * xRest * yRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[2] * (1-yRest) * (1-xRest) + pSourceCurrent[6] * xRest * (1-yRest) + pSourceCurrent[sStride+2] * (1-xRest) * yRest + pSourceCurrent[sStride+6] * xRest * yRest);
											pResultCurrent[3] = (byte) (pSourceCurrent[3] * (1-yRest) * (1-xRest) + pSourceCurrent[7] * xRest * (1-yRest) + pSourceCurrent[sStride+3] * (1-xRest) * yRest + pSourceCurrent[sStride+7] * xRest * yRest);
										}
									}
								}
							}

							pResultCurrent += 4;
							
							sourceX += xJump;
							sourceXInt = (int) sourceX;
							sourceY += yJump;
							sourceYInt = (int) sourceY;
						}
					}
				}
			}
			finally
			{
				if(sourceData != null)
					source.UnlockBits(sourceData);
				if(resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion
		
		#region Rotate32bppSetBack()
		private static Bitmap Rotate32bppSetBack(Bitmap source, Rectangle clip, double angle, byte r, byte g, byte b)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			angle = -angle;

			try
			{
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X) * (180 / Math.PI);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				double		xShifted = sx - Math.Cos((beta + angle) * Math.PI / 180) * m;
				double		yShifted = sy - Math.Sin((beta + angle) * Math.PI / 180) * m;	
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32(xShifted);
				int			ulCornerY = Convert.ToInt32(yShifted);

				double	sourceX, sourceY;
				int		sourceXInt, sourceYInt;
				double	xRest, yRest;
				double	xJump = Math.Cos(angle * Math.PI / 180);
				double	yJump = Math.Sin(angle * Math.PI / 180);
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pSourceCurrent;
					byte*	pResultCurrent;

					for(y = 0; y < resultH; y++)
					{
						sourceX = ulCornerX - y * yJump;
						sourceXInt = (int) sourceX;
						sourceY = ulCornerY + y * xJump;
						sourceYInt = (int) sourceY;
						
						pResultCurrent = pResult + y * rStride;

						for(x = 0; x < resultW; x++)
						{
							xRest = sourceX - sourceXInt;

							if(xRest < 0)
								xRest += 1;
							if(xRest < 0.000001)
								xRest = 0;
							if(xRest > .999999)
							{
								sourceXInt += 1;
								sourceX = sourceXInt;
								xRest = 0;
							}

							if(sourceXInt >= 0 && sourceXInt < sourceW && sourceYInt >= 0 && sourceYInt < sourceH)
							{
								yRest = sourceY - sourceYInt;
								if(yRest < 0)
									yRest += 1;
								else if(yRest < 0.000001)
									yRest = 0;
								else if(yRest > .999999)
								{
									sourceYInt += 1;
									sourceY = sourceYInt;
									yRest = 0;
								}
							
								if(sourceYInt < sourceH)
								{
									pSourceCurrent = pSource + sourceYInt * sStride + sourceXInt * 4;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pResultCurrent = *pSourceCurrent;
											pResultCurrent[1] = pSourceCurrent[1];
											pResultCurrent[2] = pSourceCurrent[2];
											pResultCurrent[3] = pSourceCurrent[3];
										}
										else
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1 - yRest) + pSourceCurrent[sStride] * yRest);
											pResultCurrent[1] = (byte) (pSourceCurrent[1] * (1 - yRest) + pSourceCurrent[sStride+1] * yRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[2] * (1 - yRest) + pSourceCurrent[sStride+2] * yRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[3] * (1 - yRest) + pSourceCurrent[sStride+3] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pResultCurrent = *pSourceCurrent;
											pResultCurrent[1] = pSourceCurrent[1];
											pResultCurrent[2] = pSourceCurrent[2];
											pResultCurrent[3] = pSourceCurrent[3];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1-xRest) + pSourceCurrent[4] * xRest);
											pResultCurrent[1] = (byte) (pSourceCurrent[1] * (1-xRest) + pSourceCurrent[5] * xRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[2] * (1-xRest) + pSourceCurrent[6] * xRest);
											pResultCurrent[3] = (byte) (pSourceCurrent[3] * (1-xRest) + pSourceCurrent[7] * xRest);
										}
										else
										{
											*pResultCurrent = (byte) (*pSourceCurrent * (1-yRest) * (1-xRest) + pSourceCurrent[4] * xRest * (1-yRest) + pSourceCurrent[sStride] * (1-xRest) * yRest + pSourceCurrent[sStride+4] * xRest * yRest);
											pResultCurrent[1] = (byte) (pSourceCurrent[1] * (1-yRest) * (1-xRest) + pSourceCurrent[5] * xRest * (1-yRest) + pSourceCurrent[sStride+1] * (1-xRest) * yRest + pSourceCurrent[sStride+5] * xRest * yRest);
											pResultCurrent[2] = (byte) (pSourceCurrent[2] * (1-yRest) * (1-xRest) + pSourceCurrent[6] * xRest * (1-yRest) + pSourceCurrent[sStride+2] * (1-xRest) * yRest + pSourceCurrent[sStride+6] * xRest * yRest);
											pResultCurrent[3] = (byte) (pSourceCurrent[3] * (1-yRest) * (1-xRest) + pSourceCurrent[7] * xRest * (1-yRest) + pSourceCurrent[sStride+3] * (1-xRest) * yRest + pSourceCurrent[sStride+7] * xRest * yRest);
										}
									}
								}
								else
								{
									*pResultCurrent = b;
									pResultCurrent[1] = g;
									pResultCurrent[2] = r;
								}
							}
							else
							{
								*pResultCurrent = b;
								pResultCurrent[1] = g;
								pResultCurrent[2] = r;
							}

							pResultCurrent += 4;
							
							sourceX += xJump;
							sourceXInt = (int) sourceX;
							sourceY += yJump;
							sourceYInt = (int) sourceY;
						}
					}
				}
			}
			finally
			{
				if(sourceData != null)
					source.UnlockBits(sourceData);
				if(resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region Rotate1bppBlackBack()
		private static Bitmap Rotate1bppBlackBack(Bitmap source, Rectangle clip, double angle)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			angle = -angle;

			try
			{
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X) * (180 / Math.PI);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				double		xShifted = sx - Math.Cos((beta + angle) * Math.PI / 180) * m;
				double		yShifted = sy - Math.Sin((beta + angle) * Math.PI / 180) * m;	
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32(xShifted);
				int			ulCornerY = Convert.ToInt32(yShifted);

				double	sourceX, sourceY;
				int		sourceXInt, sourceYInt;
				double	xJump = Math.Cos(angle * Math.PI / 180);
				double	yJump = Math.Sin(angle * Math.PI / 180);
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pSourceCurrent;
					byte*	pResultCurrent;
					int		i;

					for(y = 0; y < resultH; y++)
					{
						sourceX = ulCornerX - y * yJump;
						sourceXInt = (int) sourceX;
						sourceY = ulCornerY + y * xJump;
						sourceYInt = (int) sourceY;
						
						for(x = 0; x < resultW; x++)
						{
							pResultCurrent = pResult + y * rStride + x / 8;

							if(sourceXInt >= 0 && sourceXInt < sourceW && sourceYInt >= 0 && sourceYInt < sourceH)
							{
								pSourceCurrent = pSource + sourceYInt * sStride + sourceXInt / 8;
								//i = 0x80 >> (sourceXInt % 8);
								i = 0x80 >> (sourceXInt & 0x07);
						
								if((*pSourceCurrent & i) > 0)
									*pResultCurrent |= (byte) (0x80 >> (x & 0x07));
							}
														
							sourceX += xJump;
							sourceXInt = (int) sourceX;
							sourceY += yJump;
							sourceYInt = (int) sourceY;
						}
					}
				}
			}
			finally
			{
				if(sourceData != null)
					source.UnlockBits(sourceData);
				if(resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region Rotate1bppWhiteBack()
		private static Bitmap Rotate1bppWhiteBack(Bitmap source, Rectangle clip, double angle)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			angle = -angle;

			try
			{
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X) * (180 / Math.PI);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				double		xShifted = sx - Math.Cos((beta + angle) * Math.PI / 180) * m;
				double		yShifted = sy - Math.Sin((beta + angle) * Math.PI / 180) * m;	
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32(xShifted);
				int			ulCornerY = Convert.ToInt32(yShifted);

				double	sourceX, sourceY;
				int		sourceXInt, sourceYInt;
				double	xJump = Math.Cos(angle * Math.PI / 180);
				double	yJump = Math.Sin(angle * Math.PI / 180);
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pSourceCurrent;
					byte*	pResultCurrent;
					int		i;

					for(y = 0; y < resultH; y++)
					{
						sourceX = ulCornerX - y * yJump;
						sourceXInt = (int) sourceX;
						sourceY = ulCornerY + y * xJump;
						sourceYInt = (int) sourceY;
						
						for(x = 0; x < resultW; x++)
						{
							pResultCurrent = pResult + y * rStride + x / 8;

							if(sourceXInt >= 0 && sourceXInt < sourceW && sourceYInt >= 0 && sourceYInt < sourceH)
							{
								pSourceCurrent = pSource + sourceYInt * sStride + sourceXInt / 8;
								//i = 0x80 >> (sourceXInt % 8);
								i = 0x80 >> (sourceXInt & 0x07);
						
								if((*pSourceCurrent & i) > 0)
									*pResultCurrent |= (byte) (0x80 >> (x & 0x07));
							}
							else
								*pResultCurrent |= (byte) (0x80 >> (x & 0x07));
														
							sourceX += xJump;
							sourceXInt = (int) sourceX;
							sourceY += yJump;
							sourceYInt = (int) sourceY;
						}
					}
				}
			}
			finally
			{
				if(sourceData != null)
					source.UnlockBits(sourceData);
				if(resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion
	
		#endregion

	}*/
}
