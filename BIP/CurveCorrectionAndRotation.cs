using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	public class CurveCorrectionAndRotation
	{
		#region constructor
		private CurveCorrectionAndRotation()
		{
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region StretchAndRotate()
		public static Bitmap StretchAndRotate(Bitmap bitmap, ItPage page)
		{
			return StretchAndRotate(bitmap, page, 0, 0, 0);
		}

		public static Bitmap StretchAndRotate(Bitmap bitmap, ItPage page, byte r, byte g, byte b)
		{
			Bitmap result = null;

			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format1bppIndexed:
					{
						result = StretchAndRotate1bpp(bitmap, page, r > 0);
					} break;
				case PixelFormat.Format8bppIndexed:
					{
						result = StretchAndRotate8bpp(bitmap, page, r);
					} break;
				case PixelFormat.Format24bppRgb:
					{
						result = StretchAndRotate24bpp(bitmap, page, r, g, b);
					} break;
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:
					{
						result = StretchAndRotate32bpp(bitmap, page, r, g, b);
					} break;
				default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}

			if (result != null)
			{
				Misc.SetBitmapResolution(result, bitmap.HorizontalResolution, bitmap.VerticalResolution);

				if ((result.PixelFormat == PixelFormat.Format1bppIndexed) || (result.PixelFormat == PixelFormat.Format8bppIndexed))
					result.Palette = bitmap.Palette;
			}
			return result;
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region StretchAndRotate24bpp()
		private static Bitmap StretchAndRotate24bpp(Bitmap source, ItPage page, byte r, byte g, byte b)
		{
			Bitmap		result = null;
			BitmapData	sourceData = null;
			BitmapData	resultData = null;
			
			try
			{
				double		angle = -page.Skew;
				Rectangle	clip = page.Clip.RectangleNotSkewed;

				int		sx = clip.X + (clip.Width / 2);
				int		sy = clip.Y + (clip.Height / 2);
				double	beta = Math.Atan2((double)(sy - clip.Y), (double)(sx - clip.X));
				double	m = Math.Sqrt(((sx - clip.X) * (sx - clip.X)) + ((sy - clip.Y) * (sy - clip.Y)));
				
				int resultW = clip.Width;
				int resultH = clip.Height;
				int sourceW = source.Width;
				int sourceH = source.Height;
				int ulCornerX = Convert.ToInt32(sx - (Math.Cos(beta - angle) * m));
				int ulCornerY = Convert.ToInt32(sy - (Math.Sin(beta - angle) * m));
				
				int		x, y;
				double	sourceX, sourceY;
				int		sourceXInt, sourceYInt;
				double	xRest, yRest;
				double	yJump;
				double[] arrayT, arrayB;
				double	cosAngle = Math.Cos(-angle);
				double	sinAngle = Math.Sin(-angle);

				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				int lensCenter = page.LocalOpticsCenter;
				
				int			topY = lensCenter - clip.Top;
				ulCornerX -= Convert.ToInt32((double)(topY * sinAngle));
				ulCornerY += Convert.ToInt32((double)(topY * cosAngle));
				double		xJump = sinAngle;
				
				GetCurves(page, out arrayT, out arrayB);

				unsafe
				{
					byte* pCurrentS, pCurrentR;
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					for (x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1.0 - (arrayT[x] / ((double)topY)));
						sourceX = ulCornerX + (x * cosAngle);
						sourceY = ulCornerY + (x * sinAngle);
						pCurrentR = (pResult + (topY * rStride)) + (x * 3);
						sourceXInt = (int)sourceX;
						sourceYInt = (int)sourceY;

						for (y = topY; y >= 0; y--)
						{
							if (y < resultH)
							{
								xRest = sourceX - sourceXInt;
								
								if (xRest < 0.0)
									xRest++;
								if (xRest < 0.000001)
									xRest = 0.0;
								if (xRest > 0.999999)
								{
									sourceXInt++;
									sourceX = sourceXInt;
									xRest = 0.0;
								}

								if ((sourceXInt >= 0) && (sourceXInt < sourceW) && (sourceYInt >= 0) && (sourceYInt < sourceH))
								{
									yRest = sourceY - sourceYInt;
									
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
										pCurrentS = (pSource + (sourceYInt * sStride)) + (sourceXInt * 3);
										
										if (xRest == 0.0)
										{
											if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
											{
												pCurrentR[0] = pCurrentS[0];
												pCurrentR[1] = pCurrentS[1];
												pCurrentR[2] = pCurrentS[2];
											}
											else
											{
												pCurrentR[0] = (byte)((pCurrentS[0] * (1.0 - yRest)) + (pCurrentS[sStride] * yRest));
												pCurrentR[1] = (byte)((pCurrentS[1] * (1.0 - yRest)) + (pCurrentS[sStride + 1] * yRest));
												pCurrentR[2] = (byte)((pCurrentS[2] * (1.0 - yRest)) + (pCurrentS[sStride + 2] * yRest));
											}
										}
										else if (sourceXInt >= (sourceW - 1))
										{
											pCurrentR[0] = pCurrentS[0];
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
										}
										else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
										{
											pCurrentR[0] = (byte)((pCurrentS[0] * (1.0 - xRest)) + (pCurrentS[3] * xRest));
											pCurrentR[1] = (byte)((pCurrentS[1] * (1.0 - xRest)) + (pCurrentS[4] * xRest));
											pCurrentR[2] = (byte)((pCurrentS[2] * (1.0 - xRest)) + (pCurrentS[5] * xRest));
										}
										else
										{
											pCurrentR[0] = (byte)(((((pCurrentS[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[3] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 3] * xRest) * yRest));
											pCurrentR[1] = (byte)(((((pCurrentS[1] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[4] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride + 1] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 4] * xRest) * yRest));
											pCurrentR[2] = (byte)(((((pCurrentS[2] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[5] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride + 2] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 5] * xRest) * yRest));
										}
									}
									else
									{
										pCurrentR[0] = b;
										pCurrentR[1] = g;
										pCurrentR[2] = r;
									}
								}
								else
								{
									pCurrentR[0] = b;
									pCurrentR[1] = g;
									pCurrentR[2] = r;
								}
							}

							pCurrentR -= rStride;
							sourceX += xJump;
							sourceY -= yJump;
							sourceXInt = (int)sourceX;
							sourceYInt = (int)sourceY;
						}
					}

					xJump = sinAngle;
					for (x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1.0 - (arrayB[x] / ((double)(clip.Bottom - topY))));
						sourceX = ulCornerX + (x * cosAngle);
						sourceY = ulCornerY + (x * sinAngle);
						pCurrentR = (pResult + (topY * rStride)) + (x * 3);
						sourceXInt = (int)sourceX;
						sourceYInt = (int)sourceY;

						for (y = topY; y < resultH; y++)
						{
							if (y >= 0)
							{
								xRest = sourceX - sourceXInt;
								if (xRest < 0.0)
								{
									xRest++;
								}
								if (xRest < 0.000001)
								{
									xRest = 0.0;
								}
								if (xRest > 0.999999)
								{
									sourceXInt++;
									sourceX = sourceXInt;
									xRest = 0.0;
								}
								if ((((sourceXInt >= 0) && (sourceXInt < sourceW)) && (((int)sourceY) >= 0)) && (((int)sourceY) < sourceH))
								{
									yRest = sourceY - sourceYInt;
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
									if (((int)sourceY) < sourceH)
									{
										pCurrentS = (pSource + (((int)sourceY) * sStride)) + (sourceXInt * 3);
										if (xRest == 0.0)
										{
											if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
											{
												pCurrentR[0] = pCurrentS[0];
												pCurrentR[1] = pCurrentS[1];
												pCurrentR[2] = pCurrentS[2];
											}
											else
											{
												pCurrentR[0] = (byte)((pCurrentS[0] * (1.0 - yRest)) + (pCurrentS[sStride] * yRest));
												pCurrentR[1] = (byte)((pCurrentS[1] * (1.0 - yRest)) + (pCurrentS[sStride + 1] * yRest));
												pCurrentR[2] = (byte)((pCurrentS[2] * (1.0 - yRest)) + (pCurrentS[sStride + 2] * yRest));
											}
										}
										else if (sourceXInt >= (sourceW - 1))
										{
											pCurrentR[0] = pCurrentS[0];
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
										}
										else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
										{
											pCurrentR[0] = (byte)((pCurrentS[0] * (1.0 - xRest)) + (pCurrentS[3] * xRest));
											pCurrentR[1] = (byte)((pCurrentS[1] * (1.0 - xRest)) + (pCurrentS[4] * xRest));
											pCurrentR[2] = (byte)((pCurrentS[2] * (1.0 - xRest)) + (pCurrentS[5] * xRest));
										}
										else
										{
											pCurrentR[0] = (byte)(((((pCurrentS[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[3] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 3] * xRest) * yRest));
											pCurrentR[1] = (byte)(((((pCurrentS[1] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[4] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride + 1] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 4] * xRest) * yRest));
											pCurrentR[2] = (byte)(((((pCurrentS[2] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[5] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride + 2] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 5] * xRest) * yRest));
										}
									}
									else
									{
										pCurrentR[0] = b;
										pCurrentR[1] = g;
										pCurrentR[2] = r;
									}
								}
								else
								{
									pCurrentR[0] = b;
									pCurrentR[1] = g;
									pCurrentR[2] = r;
								}
							}
							pCurrentR += rStride;
							sourceX -= xJump;
							sourceY += yJump;
							sourceXInt = (int)sourceX;
							sourceYInt = (int)sourceY;
						}
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

		#region StretchAndRotate32bpp()
		private static unsafe Bitmap StretchAndRotate32bpp(Bitmap source, ItPage page, byte r, byte g, byte b)
		{
			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;
			try
			{
				int x;
				int y;
				double sourceX;
				double sourceY;
				int sourceXInt;
				int sourceYInt;
				double xRest;
				double yRest;
				double yJump;
				double[] arrayT;
				double[] arrayB;
				byte* pCurrentS;
				byte* pCurrentR;
				double angle = -page.Skew;
				Rectangle clip = page.Clip.RectangleNotSkewed;
				int sx = clip.X + (clip.Width / 2);
				int sy = clip.Y + (clip.Height / 2);
				double beta = Math.Atan2((double)(sy - clip.Y), (double)(sx - clip.X));
				double m = Math.Sqrt((double)(((sx - clip.X) * (sx - clip.X)) + ((sy - clip.Y) * (sy - clip.Y))));
				int resultW = clip.Width;
				int resultH = clip.Height;
				int sourceW = source.Width;
				int sourceH = source.Height;
				int ulCornerX = Convert.ToInt32((double)(sx - (Math.Cos(beta - angle) * m)));
				int ulCornerY = Convert.ToInt32((double)(sy - (Math.Sin(beta - angle) * m)));
				double cosAngle = Math.Cos(-angle);
				double sinAngle = Math.Sin(-angle);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				int lensCenter = page.LocalOpticsCenter;
				GetCurves(page, out arrayT, out arrayB);
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				int topY = lensCenter - clip.Top;
				ulCornerX -= Convert.ToInt32((double)(topY * sinAngle));
				ulCornerY += Convert.ToInt32((double)(topY * cosAngle));
				double xJump = sinAngle;
				for (x = 0; x < resultW; x++)
				{
					yJump = cosAngle * (1.0 - (arrayT[x] / ((double)topY)));
					sourceX = ulCornerX + (x * cosAngle);
					sourceY = ulCornerY + (x * sinAngle);
					pCurrentR = (pResult + (topY * rStride)) + (x * 4);
					sourceXInt = (int)sourceX;
					sourceYInt = (int)sourceY;
					y = topY;
					while (y >= 0)
					{
						if (y < resultH)
						{
							xRest = sourceX - sourceXInt;
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
								yRest = sourceY - sourceYInt;
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
									pCurrentS = (pSource + (sourceYInt * sStride)) + (sourceXInt * 4);
									if (xRest == 0.0)
									{
										if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
										{
											pCurrentR[0] = pCurrentS[0];
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
											pCurrentR[3] = pCurrentS[3];
										}
										else
										{
											pCurrentR[0] = (byte)((pCurrentS[0] * (1.0 - yRest)) + (pCurrentS[sStride] * yRest));
											pCurrentR[1] = (byte)((pCurrentS[1] * (1.0 - yRest)) + (pCurrentS[sStride + 1] * yRest));
											pCurrentR[2] = (byte)((pCurrentS[2] * (1.0 - yRest)) + (pCurrentS[sStride + 2] * yRest));
											pCurrentR[3] = (byte)((pCurrentS[3] * (1.0 - yRest)) + (pCurrentS[sStride + 3] * yRest));
										}
									}
									else if (sourceXInt >= (sourceW - 1))
									{
										pCurrentR[0] = pCurrentS[0];
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
										pCurrentR[3] = pCurrentS[3];
									}
									else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
									{
										pCurrentR[0] = (byte)((pCurrentS[0] * (1.0 - xRest)) + (pCurrentS[4] * xRest));
										pCurrentR[1] = (byte)((pCurrentS[1] * (1.0 - xRest)) + (pCurrentS[5] * xRest));
										pCurrentR[2] = (byte)((pCurrentS[2] * (1.0 - xRest)) + (pCurrentS[6] * xRest));
										pCurrentR[3] = (byte)((pCurrentS[3] * (1.0 - xRest)) + (pCurrentS[7] * xRest));
									}
									else
									{
										pCurrentR[0] = (byte)(((((pCurrentS[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[4] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 4] * xRest) * yRest));
										pCurrentR[1] = (byte)(((((pCurrentS[1] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[5] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride + 1] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 5] * xRest) * yRest));
										pCurrentR[2] = (byte)(((((pCurrentS[2] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[6] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride + 2] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 6] * xRest) * yRest));
										pCurrentR[3] = (byte)(((((pCurrentS[3] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[7] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride + 3] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 7] * xRest) * yRest));
									}
								}
								else
								{
									pCurrentR[0] = b;
									pCurrentR[1] = g;
									pCurrentR[2] = r;
								}
							}
							else
							{
								pCurrentR[0] = b;
								pCurrentR[1] = g;
								pCurrentR[2] = r;
							}
						}
						pCurrentR -= rStride;
						sourceX += xJump;
						sourceY -= yJump;
						sourceXInt = (int)sourceX;
						sourceYInt = (int)sourceY;
						y--;
					}
				}
				xJump = sinAngle;
				for (x = 0; x < resultW; x++)
				{
					yJump = cosAngle * (1.0 - (arrayB[x] / ((double)(clip.Bottom - topY))));
					sourceX = ulCornerX + (x * cosAngle);
					sourceY = ulCornerY + (x * sinAngle);
					pCurrentR = (pResult + (topY * rStride)) + (x * 4);
					sourceXInt = (int)sourceX;
					sourceYInt = (int)sourceY;
					for (y = topY; y < resultH; y++)
					{
						if (y >= 0)
						{
							xRest = sourceX - sourceXInt;
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
							if ((((sourceXInt >= 0) && (sourceXInt < sourceW)) && (((int)sourceY) >= 0)) && (((int)sourceY) < sourceH))
							{
								yRest = sourceY - sourceYInt;
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
								if (((int)sourceY) < sourceH)
								{
									pCurrentS = (pSource + (((int)sourceY) * sStride)) + (sourceXInt * 4);
									if (xRest == 0.0)
									{
										if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
										{
											pCurrentR[0] = pCurrentS[0];
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
											pCurrentR[3] = pCurrentS[3];
										}
										else
										{
											pCurrentR[0] = (byte)((pCurrentS[0] * (1.0 - yRest)) + (pCurrentS[sStride] * yRest));
											pCurrentR[1] = (byte)((pCurrentS[1] * (1.0 - yRest)) + (pCurrentS[sStride + 1] * yRest));
											pCurrentR[2] = (byte)((pCurrentS[2] * (1.0 - yRest)) + (pCurrentS[sStride + 2] * yRest));
											pCurrentR[3] = (byte)((pCurrentS[3] * (1.0 - yRest)) + (pCurrentS[sStride + 3] * yRest));
										}
									}
									else if (sourceXInt >= (sourceW - 1))
									{
										pCurrentR[0] = pCurrentS[0];
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
										pCurrentR[3] = pCurrentS[3];
									}
									else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
									{
										pCurrentR[0] = (byte)((pCurrentS[0] * (1.0 - xRest)) + (pCurrentS[4] * xRest));
										pCurrentR[1] = (byte)((pCurrentS[1] * (1.0 - xRest)) + (pCurrentS[5] * xRest));
										pCurrentR[2] = (byte)((pCurrentS[2] * (1.0 - xRest)) + (pCurrentS[6] * xRest));
										pCurrentR[3] = (byte)((pCurrentS[3] * (1.0 - xRest)) + (pCurrentS[7] * xRest));
									}
									else
									{
										pCurrentR[0] = (byte)(((((pCurrentS[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[4] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 4] * xRest) * yRest));
										pCurrentR[1] = (byte)(((((pCurrentS[1] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[5] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride + 1] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 5] * xRest) * yRest));
										pCurrentR[2] = (byte)(((((pCurrentS[2] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[6] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride + 2] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 6] * xRest) * yRest));
										pCurrentR[3] = (byte)(((((pCurrentS[3] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[7] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride + 3] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 7] * xRest) * yRest));
									}
								}
								else
								{
									pCurrentR[0] = b;
									pCurrentR[1] = g;
									pCurrentR[2] = r;
								}
							}
							else
							{
								pCurrentR[0] = b;
								pCurrentR[1] = g;
								pCurrentR[2] = r;
							}
						}
						pCurrentR += rStride;
						sourceX -= xJump;
						sourceY += yJump;
						sourceXInt = (int)sourceX;
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

		#region StretchAndRotate8bpp()
		private static unsafe Bitmap StretchAndRotate8bpp(Bitmap source, ItPage page, byte backColor)
		{
			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;
			try
			{
				int x;
				int y;
				double sourceX;
				double sourceY;
				int sourceXInt;
				int sourceYInt;
				double xRest;
				double yRest;
				double yJump;
				double[] arrayT;
				double[] arrayB;
				byte* pCurrentS;
				byte* pCurrentR;
				double angle = -page.Skew;
				Rectangle clip = page.Clip.RectangleNotSkewed;
				int sx = clip.X + (clip.Width / 2);
				int sy = clip.Y + (clip.Height / 2);
				double beta = Math.Atan2((double)(sy - clip.Y), (double)(sx - clip.X));
				double m = Math.Sqrt((double)(((sx - clip.X) * (sx - clip.X)) + ((sy - clip.Y) * (sy - clip.Y))));
				int resultW = clip.Width;
				int resultH = clip.Height;
				int sourceW = source.Width;
				int sourceH = source.Height;
				int ulCornerX = Convert.ToInt32((double)(sx - (Math.Cos(beta - angle) * m)));
				int ulCornerY = Convert.ToInt32((double)(sy - (Math.Sin(beta - angle) * m)));
				double cosAngle = Math.Cos(-angle);
				double sinAngle = Math.Sin(-angle);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				int lensCenter = page.LocalOpticsCenter;
				GetCurves(page, out arrayT, out arrayB);
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				int topY = lensCenter - clip.Top;
				ulCornerX -= Convert.ToInt32((double)(topY * sinAngle));
				ulCornerY += Convert.ToInt32((double)(topY * cosAngle));
				double xJump = sinAngle;
				for (x = 0; x < resultW; x++)
				{
					yJump = cosAngle * (1.0 - (arrayT[x] / ((double)topY)));
					sourceX = ulCornerX + (x * cosAngle);
					sourceY = ulCornerY + (x * sinAngle);
					pCurrentR = (pResult + (topY * rStride)) + x;
					sourceXInt = (int)sourceX;
					sourceYInt = (int)sourceY;
					y = topY;
					while (y >= 0)
					{
						if (y < resultH)
						{
							xRest = sourceX - sourceXInt;
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
								yRest = sourceY - sourceYInt;
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
									pCurrentS = (pSource + (sourceYInt * sStride)) + sourceXInt;
									if (xRest == 0.0)
									{
										if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
										{
											pCurrentR[0] = pCurrentS[0];
										}
										else
										{
											pCurrentR[0] = (byte)((pCurrentS[0] * (1.0 - yRest)) + (pCurrentS[sStride] * yRest));
										}
									}
									else if (sourceXInt >= (sourceW - 1))
									{
										pCurrentR[0] = pCurrentS[0];
									}
									else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
									{
										pCurrentR[0] = (byte)((pCurrentS[0] * (1.0 - xRest)) + (pCurrentS[1] * xRest));
									}
									else
									{
										pCurrentR[0] = (byte)(((((pCurrentS[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[1] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 1] * xRest) * yRest));
									}
								}
								else
								{
									pCurrentR[0] = backColor;
								}
							}
							else
							{
								pCurrentR[0] = backColor;
							}
						}
						pCurrentR -= rStride;
						sourceX += xJump;
						sourceY -= yJump;
						sourceXInt = (int)sourceX;
						sourceYInt = (int)sourceY;
						y--;
					}
				}
				xJump = sinAngle;
				for (x = 0; x < resultW; x++)
				{
					yJump = cosAngle * (1.0 - (arrayB[x] / ((double)(clip.Bottom - topY))));
					sourceX = ulCornerX + (x * cosAngle);
					sourceY = ulCornerY + (x * sinAngle);
					pCurrentR = (pResult + (topY * rStride)) + x;
					sourceXInt = (int)sourceX;
					sourceYInt = (int)sourceY;
					for (y = topY; y < resultH; y++)
					{
						if (y >= 0)
						{
							xRest = sourceX - sourceXInt;
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
							if ((((sourceXInt >= 0) && (sourceXInt < sourceW)) && (((int)sourceY) >= 0)) && (((int)sourceY) < sourceH))
							{
								yRest = sourceY - sourceYInt;
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
								if (((int)sourceY) < sourceH)
								{
									pCurrentS = (pSource + (((int)sourceY) * sStride)) + sourceXInt;
									if (xRest == 0.0)
									{
										if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
										{
											pCurrentR[0] = pCurrentS[0];
										}
										else
										{
											pCurrentR[0] = (byte)((pCurrentS[0] * (1.0 - yRest)) + (pCurrentS[sStride] * yRest));
										}
									}
									else if (sourceXInt >= (sourceW - 1))
									{
										pCurrentR[0] = pCurrentS[0];
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
									}
									else if ((yRest == 0.0) || (sourceYInt >= (sourceH - 1)))
									{
										pCurrentR[0] = (byte)((pCurrentS[0] * (1.0 - xRest)) + (pCurrentS[1] * xRest));
									}
									else
									{
										pCurrentR[0] = (byte)(((((pCurrentS[0] * (1.0 - yRest)) * (1.0 - xRest)) + ((pCurrentS[1] * xRest) * (1.0 - yRest))) + ((pCurrentS[sStride] * (1.0 - xRest)) * yRest)) + ((pCurrentS[sStride + 1] * xRest) * yRest));
									}
								}
								else
								{
									pCurrentR[0] = backColor;
								}
							}
							else
							{
								pCurrentR[0] = backColor;
							}
						}
						pCurrentR += rStride;
						sourceX -= xJump;
						sourceY += yJump;
						sourceXInt = (int)sourceX;
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
	
		#region StretchAndRotate1bpp()
		private static Bitmap StretchAndRotate1bpp(Bitmap source, ItPage page, bool whiteBackground)
		{
			BitmapData sourceData = null;
			Bitmap result = null;
			BitmapData resultData = null;
			try
			{
				double angle = -page.Skew;
				Rectangle clip = page.Clip.RectangleNotSkewed;
				
				int sx = clip.X + (clip.Width / 2);
				int sy = clip.Y + (clip.Height / 2);
				double beta = Math.Atan2((double)(sy - clip.Y), (double)(sx - clip.X));
				double m = Math.Sqrt((double)(((sx - clip.X) * (sx - clip.X)) + ((sy - clip.Y) * (sy - clip.Y))));
				
				int x, y, i;
				int resultW = clip.Width;
				int resultH = clip.Height;
				int sourceW = source.Width;
				int sourceH = source.Height;
				int ulCornerX = Convert.ToInt32(sx - (Math.Cos(beta - angle) * m));
				int ulCornerY = Convert.ToInt32(sy - (Math.Sin(beta - angle) * m));
				
				double	sourceX, sourceY;
				int		sourceXInt, sourceYInt;
				double	cosAngle = Math.Cos(-angle);
				double	sinAngle = Math.Sin(-angle);
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				
				double[] arrayT, arrayB;
				int lensCenter = page.LocalOpticsCenter;

				GetCurves(page, out arrayT, out arrayB);

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					
					byte* pCurrentS;
					byte* pCurrentR;
					
					int sStride = sourceData.Stride;
					int rStride = resultData.Stride;
					
					int topY = lensCenter - clip.Top;
					ulCornerX -= Convert.ToInt32((double)(topY * sinAngle));
					ulCornerY += Convert.ToInt32((double)(topY * cosAngle));

					double xJump = sinAngle, yJump;

					for (x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1.0 - (arrayT[x] / ((double)topY)));
						sourceX = ulCornerX + (x * cosAngle);
						sourceY = ulCornerY + (x * sinAngle);
						pCurrentR = (pResult + (topY * rStride)) + x;
						sourceXInt = (int)sourceX;
						sourceYInt = (int)sourceY;

						for (y = topY; y >= 0; y--)
						{
							if (y < resultH)
							{
								pCurrentR = (pResult + (y * rStride)) + (x / 8);

								if ((sourceXInt >= 0) && (sourceXInt < sourceW) && (sourceYInt >= 0) && (sourceYInt < sourceH))
								{
									pCurrentS = (pSource + (sourceYInt * sStride)) + (sourceXInt / 8);
									i = ((int)0x80) >> (sourceXInt & 7);

									if ((pCurrentS[0] & i) > 0)
										pCurrentR[0] = (byte)(pCurrentR[0] | ((byte)(0x80 >> (x & 7))));
								}
								else if (whiteBackground)
									pCurrentR[0] = (byte)(pCurrentR[0] | ((byte)(0x80 >> (x & 7))));
							}

							sourceX += xJump;
							sourceY -= yJump;
							sourceXInt = (int)sourceX;
							sourceYInt = (int)sourceY;
						}
					}

					xJump = sinAngle;
					for (x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1.0 - (arrayB[x] / ((double)(clip.Bottom - topY))));
						sourceX = ulCornerX + (x * cosAngle);
						sourceY = ulCornerY + (x * sinAngle);
						pCurrentR = (pResult + (topY * rStride)) + x;
						sourceXInt = (int)sourceX;
						sourceYInt = (int)sourceY;

						for (y = topY; y < resultH; y++)
						{
							if (y >= 0)
							{
								pCurrentR = (pResult + (y * rStride)) + (x / 8);
								
								if ((((sourceXInt >= 0) && (sourceXInt < sourceW)) && (((int)sourceY) >= 0)) && (((int)sourceY) < sourceH))
								{
									pCurrentS = (pSource + (sourceYInt * sStride)) + (sourceXInt / 8);
									i = ((int)0x80) >> (sourceXInt & 7);
									
									if ((pCurrentS[0] & i) > 0)
										pCurrentR[0] = (byte)(pCurrentR[0] | ((byte)(((int)0x80) >> (x & 7))));
								}
								else if (whiteBackground)
									pCurrentR[0] = (byte)(pCurrentR[0] | ((byte)(((int)0x80) >> (x & 7))));
							}
							
							sourceX -= xJump;
							sourceY += yJump;
							sourceXInt = (int)sourceX;
							sourceYInt = (int)sourceY;
						}
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

		#region GetCurves()
		private static void GetCurves(ItPage itPage, out double[] arrayT, out double[] arrayB)
		{
			int x;
			int lensCenter = itPage.LocalOpticsCenter;
			arrayT = itPage.Bookfolding.TopCurve.GetNotAngledArray();
			arrayB = itPage.Bookfolding.BottomCurve.GetNotAngledArray();
			double smallestNumber = 2147483647.0;
			
			for (x = 0; x < arrayT.Length; x++)
				if (smallestNumber > arrayT[x])
					smallestNumber = arrayT[x];
			
			if (smallestNumber != 0.0)
				for (x = 0; x < arrayT.Length; x++)
					arrayT[x] -= smallestNumber;
			
			if ((lensCenter - smallestNumber) > 10.0)
				for (x = 0; x < arrayT.Length; x++)
					arrayT[x] = (arrayT[x] * (lensCenter - itPage.Clip.RectangleNotSkewed.Y)) / (lensCenter - smallestNumber);
			
			double biggestNumber = -2147483648.0;
			for (x = 0; x < arrayB.Length; x++)
				if (biggestNumber < arrayB[x])
					biggestNumber = arrayB[x];
			
			for (x = 0; x < arrayB.Length; x++)
				arrayB[x] = biggestNumber - arrayB[x];
			
			double coef = ((double)(itPage.Clip.RectangleNotSkewed.Bottom - lensCenter)) / (biggestNumber - lensCenter);
			if ((biggestNumber - lensCenter) > 10.0)
				for (x = 0; x < arrayB.Length; x++)
					arrayB[x] *= coef;
		}
		#endregion

		#endregion

	}

	#region old stuff
	/*public class CurveCorrectionAndRotation
	{		 	

		//PRIVATE METHODS

		#region StretchAndRotate32bpp()
		private static Bitmap StretchAndRotate32bpp(Bitmap source, ItPage page)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			try
			{
				double		angle = page.Skew;
				Rectangle	clip = page.Clip.RectangleNotSkewed;
				
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32( sx - Math.Cos(beta - angle) * m );
				int			ulCornerY = Convert.ToInt32( sy - Math.Sin(beta - angle) * m );

				double		sourceX, sourceY;
				int			sourceXInt, sourceYInt;
				double		xRest, yRest;
				double		cosAngle = Math.Cos(-angle);
				double		sinAngle = Math.Sin(-angle);
				double		xJump, yJump;
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				double[]	arrayT, arrayB;
				int			lensCenter = page.OpticsCenter; // GetLensCenter(page);
				
				GetCurves(page, out arrayT, out arrayB);

				unsafe 
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pCurrentS;
					byte*	pCurrentR;

					int		topY = lensCenter - clip.Top;
					ulCornerX -= Convert.ToInt32(topY * sinAngle);
					ulCornerY += Convert.ToInt32(topY * cosAngle);

					xJump = sinAngle;

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayT[x] / topY);
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x * 4;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y >= 0; y--)
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
									pCurrentS = pSource + sourceYInt * sStride + sourceXInt * 4;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
											pCurrentR[3] = pCurrentS[3];
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1 - yRest) + pCurrentS[sStride] * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1 - yRest) + pCurrentS[sStride+1] * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1 - yRest) + pCurrentS[sStride+2] * yRest);
											pCurrentR[3] = (byte) (pCurrentS[3] * (1 - yRest) + pCurrentS[sStride+3] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
											pCurrentR[3] = pCurrentS[3];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = (byte) (*pCurrentS * (1-xRest) + pCurrentS[4] * xRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-xRest) + pCurrentS[5] * xRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-xRest) + pCurrentS[6] * xRest);
											pCurrentR[3] = (byte) (pCurrentS[3] * (1-xRest) + pCurrentS[7] * xRest);
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1-yRest) * (1-xRest) + pCurrentS[4] * xRest * (1-yRest) + pCurrentS[sStride] * (1-xRest) * yRest + pCurrentS[sStride+4] * xRest * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-yRest) * (1-xRest) + pCurrentS[5] * xRest * (1-yRest) + pCurrentS[sStride+1] * (1-xRest) * yRest + pCurrentS[sStride+5] * xRest * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-yRest) * (1-xRest) + pCurrentS[6] * xRest * (1-yRest) + pCurrentS[sStride+2] * (1-xRest) * yRest + pCurrentS[sStride+6] * xRest * yRest);
											pCurrentR[3] = (byte) (pCurrentS[3] * (1-yRest) * (1-xRest) + pCurrentS[7] * xRest * (1-yRest) + pCurrentS[sStride+3] * (1-xRest) * yRest + pCurrentS[sStride+7] * xRest * yRest);
										}
									}							
								}
							}

							pCurrentR -= rStride;
							
							sourceX += xJump;
							sourceY -= yJump;
							sourceXInt = (int) sourceX;
							sourceYInt = (int) sourceY;
						}
					}

					xJump = sinAngle;// * (1 - arrayT[x] / topY);

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayB[x] / (clip.Bottom - topY));
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x * 4;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y < resultH; y++)
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

							if(sourceXInt >= 0 && sourceXInt < sourceW && (int) sourceY >= 0 && (int) sourceY < sourceH)
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
							
								if((int) sourceY < sourceH)
								{
									pCurrentS = pSource + (int) sourceY * sStride + sourceXInt * 4;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
											pCurrentR[3] = pCurrentS[3];
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1 - yRest) + pCurrentS[sStride] * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1 - yRest) + pCurrentS[sStride+1] * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1 - yRest) + pCurrentS[sStride+2] * yRest);
											pCurrentR[3] = (byte) (pCurrentS[3] * (1 - yRest) + pCurrentS[sStride+3] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
											pCurrentR[3] = pCurrentS[3];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = (byte) (*pCurrentS * (1-xRest) + pCurrentS[4] * xRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-xRest) + pCurrentS[5] * xRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-xRest) + pCurrentS[6] * xRest);
											pCurrentR[3] = (byte) (pCurrentS[3] * (1-xRest) + pCurrentS[7] * xRest);
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1-yRest) * (1-xRest) + pCurrentS[4] * xRest * (1-yRest) + pCurrentS[sStride] * (1-xRest) * yRest + pCurrentS[sStride+4] * xRest * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-yRest) * (1-xRest) + pCurrentS[5] * xRest * (1-yRest) + pCurrentS[sStride+1] * (1-xRest) * yRest + pCurrentS[sStride+5] * xRest * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-yRest) * (1-xRest) + pCurrentS[6] * xRest * (1-yRest) + pCurrentS[sStride+2] * (1-xRest) * yRest + pCurrentS[sStride+6] * xRest * yRest);
											pCurrentR[3] = (byte) (pCurrentS[3] * (1-yRest) * (1-xRest) + pCurrentS[7] * xRest * (1-yRest) + pCurrentS[sStride+3] * (1-xRest) * yRest + pCurrentS[sStride+7] * xRest * yRest);
										}
									}								
								}
							}

							pCurrentR += rStride;
							
							sourceX -= xJump;
							sourceY += yJump;
							sourceXInt = (int) sourceX;
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

		#region StretchAndRotate32bppSetBack()
		private static Bitmap StretchAndRotate32bppSetBack(Bitmap source, ItPage page, byte r, byte g, byte b)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			try
			{
				double		angle = page.Skew;
				Rectangle	clip = page.Clip.RectangleNotSkewed;
				
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32( sx - Math.Cos(beta - angle) * m );
				int			ulCornerY = Convert.ToInt32( sy - Math.Sin(beta - angle) * m );

				double		sourceX, sourceY;
				int			sourceXInt, sourceYInt;
				double		xRest, yRest;
				double		cosAngle = Math.Cos(-angle);
				double		sinAngle = Math.Sin(-angle);
				double		xJump, yJump;
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				double[]	arrayT, arrayB;
				int lensCenter = page.OpticsCenter;// GetLensCenter(page);
				
				GetCurves(page, out arrayT, out arrayB);

				unsafe 
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pCurrentS;
					byte*	pCurrentR;

					int		topY = lensCenter - clip.Top;
					ulCornerX -= Convert.ToInt32(topY * sinAngle);
					ulCornerY += Convert.ToInt32(topY * cosAngle);

					xJump = sinAngle;

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayT[x] / topY);
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x * 4;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y >= 0; y--)
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
									pCurrentS = pSource + sourceYInt * sStride + sourceXInt * 4;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
											pCurrentR[3] = pCurrentS[3];
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1 - yRest) + pCurrentS[sStride] * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1 - yRest) + pCurrentS[sStride+1] * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1 - yRest) + pCurrentS[sStride+2] * yRest);
											pCurrentR[3] = (byte) (pCurrentS[3] * (1 - yRest) + pCurrentS[sStride+3] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
											pCurrentR[3] = pCurrentS[3];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = (byte) (*pCurrentS * (1-xRest) + pCurrentS[4] * xRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-xRest) + pCurrentS[5] * xRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-xRest) + pCurrentS[6] * xRest);
											pCurrentR[3] = (byte) (pCurrentS[3] * (1-xRest) + pCurrentS[7] * xRest);
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1-yRest) * (1-xRest) + pCurrentS[4] * xRest * (1-yRest) + pCurrentS[sStride] * (1-xRest) * yRest + pCurrentS[sStride+4] * xRest * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-yRest) * (1-xRest) + pCurrentS[5] * xRest * (1-yRest) + pCurrentS[sStride+1] * (1-xRest) * yRest + pCurrentS[sStride+5] * xRest * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-yRest) * (1-xRest) + pCurrentS[6] * xRest * (1-yRest) + pCurrentS[sStride+2] * (1-xRest) * yRest + pCurrentS[sStride+6] * xRest * yRest);
											pCurrentR[3] = (byte) (pCurrentS[3] * (1-yRest) * (1-xRest) + pCurrentS[7] * xRest * (1-yRest) + pCurrentS[sStride+3] * (1-xRest) * yRest + pCurrentS[sStride+7] * xRest * yRest);
										}
									}							
								}
								else
								{
									*pCurrentR = b;
									pCurrentR[1] = g;
									pCurrentR[2] = r;
								}
							}
							else
							{
								*pCurrentR = b;
								pCurrentR[1] = g;
								pCurrentR[2] = r;
							}							

							pCurrentR -= rStride;
							
							sourceX += xJump;
							sourceY -= yJump;
							sourceXInt = (int) sourceX;
							sourceYInt = (int) sourceY;
						}
					}

					xJump = sinAngle;// * (1 - arrayT[x] / topY);

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayB[x] / (clip.Bottom - topY));
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x * 4;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y < resultH; y++)
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

							if(sourceXInt >= 0 && sourceXInt < sourceW && (int) sourceY >= 0 && (int) sourceY < sourceH)
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
							
								if((int) sourceY < sourceH)
								{
									pCurrentS = pSource + (int) sourceY * sStride + sourceXInt * 4;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
											pCurrentR[3] = pCurrentS[3];
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1 - yRest) + pCurrentS[sStride] * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1 - yRest) + pCurrentS[sStride+1] * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1 - yRest) + pCurrentS[sStride+2] * yRest);
											pCurrentR[3] = (byte) (pCurrentS[3] * (1 - yRest) + pCurrentS[sStride+3] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
											pCurrentR[3] = pCurrentS[3];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = (byte) (*pCurrentS * (1-xRest) + pCurrentS[4] * xRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-xRest) + pCurrentS[5] * xRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-xRest) + pCurrentS[6] * xRest);
											pCurrentR[3] = (byte) (pCurrentS[3] * (1-xRest) + pCurrentS[7] * xRest);
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1-yRest) * (1-xRest) + pCurrentS[4] * xRest * (1-yRest) + pCurrentS[sStride] * (1-xRest) * yRest + pCurrentS[sStride+4] * xRest * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-yRest) * (1-xRest) + pCurrentS[5] * xRest * (1-yRest) + pCurrentS[sStride+1] * (1-xRest) * yRest + pCurrentS[sStride+5] * xRest * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-yRest) * (1-xRest) + pCurrentS[6] * xRest * (1-yRest) + pCurrentS[sStride+2] * (1-xRest) * yRest + pCurrentS[sStride+6] * xRest * yRest);
											pCurrentR[3] = (byte) (pCurrentS[3] * (1-yRest) * (1-xRest) + pCurrentS[7] * xRest * (1-yRest) + pCurrentS[sStride+3] * (1-xRest) * yRest + pCurrentS[sStride+7] * xRest * yRest);
										}
									}
								}
								else
								{
									*pCurrentR = b;
									pCurrentR[1] = g;
									pCurrentR[2] = r;
								}
							}
							else
							{
								*pCurrentR = b;
								pCurrentR[1] = g;
								pCurrentR[2] = r;
							}

							pCurrentR += rStride;
							
							sourceX -= xJump;
							sourceY += yJump;
							sourceXInt = (int) sourceX;
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

		#region StretchAndRotate24bpp()
		private static Bitmap StretchAndRotate24bpp(Bitmap source, ItPage page)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			try
			{
				double		angle = page.Skew;
				Rectangle	clip = page.Clip.RectangleNotSkewed;
				
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32( sx - Math.Cos(beta - angle) * m );
				int			ulCornerY = Convert.ToInt32( sy - Math.Sin(beta - angle) * m );

				double		sourceX, sourceY;
				int			sourceXInt, sourceYInt;
				double		xRest, yRest;
				double		cosAngle = Math.Cos(-angle);
				double		sinAngle = Math.Sin(-angle);
				double		xJump, yJump;
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				double[]	arrayT, arrayB;
				int lensCenter = page.OpticsCenter;// GetLensCenter(page);
				
				GetCurves(page, out arrayT, out arrayB);

				unsafe 
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pCurrentS;
					byte*	pCurrentR;

					int		topY = lensCenter - clip.Top;
					ulCornerX -= Convert.ToInt32(topY * sinAngle);
					ulCornerY += Convert.ToInt32(topY * cosAngle);

					xJump = sinAngle;

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayT[x] / topY);
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x * 3;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y >= 0; y--)
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
									pCurrentS = pSource + sourceYInt * sStride + sourceXInt * 3;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1 - yRest) + pCurrentS[sStride] * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1 - yRest) + pCurrentS[sStride+1] * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1 - yRest) + pCurrentS[sStride+2] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = (byte) (*pCurrentS * (1-xRest) + pCurrentS[3] * xRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-xRest) + pCurrentS[4] * xRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-xRest) + pCurrentS[5] * xRest);
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1-yRest) * (1-xRest) + pCurrentS[3] * xRest * (1-yRest) + pCurrentS[sStride] * (1-xRest) * yRest + pCurrentS[sStride+3] * xRest * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-yRest) * (1-xRest) + pCurrentS[4] * xRest * (1-yRest) + pCurrentS[sStride+1] * (1-xRest) * yRest + pCurrentS[sStride+4] * xRest * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-yRest) * (1-xRest) + pCurrentS[5] * xRest * (1-yRest) + pCurrentS[sStride+2] * (1-xRest) * yRest + pCurrentS[sStride+5] * xRest * yRest);
										}
									}								
								}
							}

							pCurrentR -= rStride;
							
							sourceX += xJump;
							sourceY -= yJump;
							sourceXInt = (int) sourceX;
							sourceYInt = (int) sourceY;
						}
					}

					xJump = sinAngle;// * (1 - arrayT[x] / topY);

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayB[x] / (clip.Bottom - topY));
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x * 3;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y < resultH; y++)
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

							if(sourceXInt >= 0 && sourceXInt < sourceW && (int) sourceY >= 0 && (int) sourceY < sourceH)
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
							
								if((int) sourceY < sourceH)
								{
									pCurrentS = pSource + (int) sourceY * sStride + sourceXInt * 3;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1 - yRest) + pCurrentS[sStride] * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1 - yRest) + pCurrentS[sStride+1] * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1 - yRest) + pCurrentS[sStride+2] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = (byte) (*pCurrentS * (1-xRest) + pCurrentS[3] * xRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-xRest) + pCurrentS[4] * xRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-xRest) + pCurrentS[5] * xRest);
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1-yRest) * (1-xRest) + pCurrentS[3] * xRest * (1-yRest) + pCurrentS[sStride] * (1-xRest) * yRest + pCurrentS[sStride+3] * xRest * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-yRest) * (1-xRest) + pCurrentS[4] * xRest * (1-yRest) + pCurrentS[sStride+1] * (1-xRest) * yRest + pCurrentS[sStride+4] * xRest * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-yRest) * (1-xRest) + pCurrentS[5] * xRest * (1-yRest) + pCurrentS[sStride+2] * (1-xRest) * yRest + pCurrentS[sStride+5] * xRest * yRest);
										}
									}								
								}
							}

							pCurrentR += rStride;
							
							sourceX -= xJump;
							sourceY += yJump;
							sourceXInt = (int) sourceX;
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

		#region StretchAndRotate24bppSetBack()
		private static Bitmap StretchAndRotate24bppSetBack(Bitmap source, ItPage page, byte r, byte g, byte b)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			try
			{
				double		angle = page.Skew;
				Rectangle	clip = page.Clip.RectangleNotSkewed;
				
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32( sx - Math.Cos(beta - angle) * m );
				int			ulCornerY = Convert.ToInt32( sy - Math.Sin(beta - angle) * m );

				double		sourceX, sourceY;
				int			sourceXInt, sourceYInt;
				double		xRest, yRest;
				double		cosAngle = Math.Cos(-angle);
				double		sinAngle = Math.Sin(-angle);
				double		xJump, yJump;
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				double[]	arrayT, arrayB;
				int lensCenter = page.OpticsCenter;// GetLensCenter(page);
				
				GetCurves(page, out arrayT, out arrayB);

				unsafe 
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pCurrentS;
					byte*	pCurrentR;

					int		topY = lensCenter - clip.Top;
					ulCornerX -= Convert.ToInt32(topY * sinAngle);
					ulCornerY += Convert.ToInt32(topY * cosAngle);

					xJump = sinAngle;

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayT[x] / topY);
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x * 3;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y >= 0; y--)
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
									pCurrentS = pSource + sourceYInt * sStride + sourceXInt * 3;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1 - yRest) + pCurrentS[sStride] * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1 - yRest) + pCurrentS[sStride+1] * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1 - yRest) + pCurrentS[sStride+2] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = (byte) (*pCurrentS * (1-xRest) + pCurrentS[3] * xRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-xRest) + pCurrentS[4] * xRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-xRest) + pCurrentS[5] * xRest);
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1-yRest) * (1-xRest) + pCurrentS[3] * xRest * (1-yRest) + pCurrentS[sStride] * (1-xRest) * yRest + pCurrentS[sStride+3] * xRest * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-yRest) * (1-xRest) + pCurrentS[4] * xRest * (1-yRest) + pCurrentS[sStride+1] * (1-xRest) * yRest + pCurrentS[sStride+4] * xRest * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-yRest) * (1-xRest) + pCurrentS[5] * xRest * (1-yRest) + pCurrentS[sStride+2] * (1-xRest) * yRest + pCurrentS[sStride+5] * xRest * yRest);
										}
									}								
								}
								else
								{
									*pCurrentR = b;
									pCurrentR[1] = g;
									pCurrentR[2] = r;
								}
							}
							else
							{
								*pCurrentR = b;
								pCurrentR[1] = g;
								pCurrentR[2] = r;
							}

							pCurrentR -= rStride;
							
							sourceX += xJump;
							sourceY -= yJump;
							sourceXInt = (int) sourceX;
							sourceYInt = (int) sourceY;
						}
					}

					xJump = sinAngle;// * (1 - arrayT[x] / topY);

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayB[x] / (clip.Bottom - topY));
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x * 3;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y < resultH; y++)
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

							if(sourceXInt >= 0 && sourceXInt < sourceW && (int) sourceY >= 0 && (int) sourceY < sourceH)
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
							
								if((int) sourceY < sourceH)
								{
									pCurrentS = pSource + (int) sourceY * sStride + sourceXInt * 3;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1 - yRest) + pCurrentS[sStride] * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1 - yRest) + pCurrentS[sStride+1] * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1 - yRest) + pCurrentS[sStride+2] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = (byte) (*pCurrentS * (1-xRest) + pCurrentS[3] * xRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-xRest) + pCurrentS[4] * xRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-xRest) + pCurrentS[5] * xRest);
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1-yRest) * (1-xRest) + pCurrentS[3] * xRest * (1-yRest) + pCurrentS[sStride] * (1-xRest) * yRest + pCurrentS[sStride+3] * xRest * yRest);
											pCurrentR[1] = (byte) (pCurrentS[1] * (1-yRest) * (1-xRest) + pCurrentS[4] * xRest * (1-yRest) + pCurrentS[sStride+1] * (1-xRest) * yRest + pCurrentS[sStride+4] * xRest * yRest);
											pCurrentR[2] = (byte) (pCurrentS[2] * (1-yRest) * (1-xRest) + pCurrentS[5] * xRest * (1-yRest) + pCurrentS[sStride+2] * (1-xRest) * yRest + pCurrentS[sStride+5] * xRest * yRest);
										}
									}								
								}
								else
								{
									*pCurrentR = b;
									pCurrentR[1] = g;
									pCurrentR[2] = r;
								}
							}
							else
							{
								*pCurrentR = b;
								pCurrentR[1] = g;
								pCurrentR[2] = r;
							}

							pCurrentR += rStride;
							
							sourceX -= xJump;
							sourceY += yJump;
							sourceXInt = (int) sourceX;
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

		#region StretchAndRotate8bpp()
		private static Bitmap StretchAndRotate8bpp(Bitmap source, ItPage page)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			try
			{
				double		angle = page.Skew;
				Rectangle	clip = page.Clip.RectangleNotSkewed;
				
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32( sx - Math.Cos(beta - angle) * m );
				int			ulCornerY = Convert.ToInt32( sy - Math.Sin(beta - angle) * m );

				double		sourceX, sourceY;
				int			sourceXInt, sourceYInt;
				double		xRest, yRest;
				double		cosAngle = Math.Cos(-angle);
				double		sinAngle = Math.Sin(-angle);
				double		xJump, yJump;
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				double[]	arrayT, arrayB;
				int lensCenter = page.OpticsCenter;// GetLensCenter(page);
				
				GetCurves(page, out arrayT, out arrayB);

				unsafe 
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pCurrentS;
					byte*	pCurrentR;

					int		topY = lensCenter - clip.Top;
					ulCornerX -= Convert.ToInt32(topY * sinAngle);
					ulCornerY += Convert.ToInt32(topY * cosAngle);

					xJump = sinAngle;

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayT[x] / topY);
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y >= 0; y--)
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
									pCurrentS = pSource + sourceYInt * sStride + sourceXInt;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = *pCurrentS;
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1 - yRest) + pCurrentS[sStride] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pCurrentR = *pCurrentS;
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = (byte) (*pCurrentS * (1-xRest) + pCurrentS[1] * xRest);
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1-yRest) * (1-xRest) + pCurrentS[1] * xRest * (1-yRest) + pCurrentS[sStride] * (1-xRest) * yRest + pCurrentS[sStride+1] * xRest * yRest);
										}
									}								
								}
							}

							pCurrentR -= rStride;
							
							sourceX += xJump;
							sourceY -= yJump;
							sourceXInt = (int) sourceX;
							sourceYInt = (int) sourceY;
						}
					}

					xJump = sinAngle;// * (1 - arrayT[x] / topY);

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayB[x] / (clip.Bottom - topY));
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y < resultH; y++)
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

							if(sourceXInt >= 0 && sourceXInt < sourceW && (int) sourceY >= 0 && (int) sourceY < sourceH)
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
							
								if((int) sourceY < sourceH)
								{
									pCurrentS = pSource + (int) sourceY * sStride + sourceXInt;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = *pCurrentS;
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1 - yRest) + pCurrentS[sStride] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = (byte) (*pCurrentS * (1-xRest) + pCurrentS[1] * xRest);
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1-yRest) * (1-xRest) + pCurrentS[1] * xRest * (1-yRest) + pCurrentS[sStride] * (1-xRest) * yRest + pCurrentS[sStride+1] * xRest * yRest);
										}
									}								
								}
							}

							pCurrentR += rStride;
							
							sourceX -= xJump;
							sourceY += yJump;
							sourceXInt = (int) sourceX;
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
		
		#region StretchAndRotate8bppSetBack()
		private static Bitmap StretchAndRotate8bppSetBack(Bitmap source, ItPage page, byte backColor)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			try
			{
				double		angle = page.Skew;
				Rectangle	clip = page.Clip.RectangleNotSkewed;
				
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				
				int			x, y;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32( sx - Math.Cos(beta - angle) * m );
				int			ulCornerY = Convert.ToInt32( sy - Math.Sin(beta - angle) * m );

				double		sourceX, sourceY;
				int			sourceXInt, sourceYInt;
				double		xRest, yRest;
				double		cosAngle = Math.Cos(-angle);
				double		sinAngle = Math.Sin(-angle);
				double		xJump, yJump;
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				double[]	arrayT, arrayB;
				int lensCenter = page.OpticsCenter;// GetLensCenter(page);
				
				GetCurves(page, out arrayT, out arrayB);

				unsafe 
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pCurrentS;
					byte*	pCurrentR;

					int		topY = lensCenter - clip.Top;
					ulCornerX -= Convert.ToInt32(topY * sinAngle);
					ulCornerY += Convert.ToInt32(topY * cosAngle);

					xJump = sinAngle;

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayT[x] / topY);
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y >= 0; y--)
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
									pCurrentS = pSource + sourceYInt * sStride + sourceXInt;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = *pCurrentS;
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1 - yRest) + pCurrentS[sStride] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pCurrentR = *pCurrentS;
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = (byte) (*pCurrentS * (1-xRest) + pCurrentS[1] * xRest);
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1-yRest) * (1-xRest) + pCurrentS[1] * xRest * (1-yRest) + pCurrentS[sStride] * (1-xRest) * yRest + pCurrentS[sStride+1] * xRest * yRest);
										}
									}								
								}
								else
								{
									*pCurrentR = backColor;
								}
							}
							else
							{
								*pCurrentR = backColor;
							}

							pCurrentR -= rStride;
							
							sourceX += xJump;
							sourceY -= yJump;
							sourceXInt = (int) sourceX;
							sourceYInt = (int) sourceY;
						}
					}

					xJump = sinAngle;// * (1 - arrayT[x] / topY);

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayB[x] / (clip.Bottom - topY));
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y < resultH; y++)
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

							if(sourceXInt >= 0 && sourceXInt < sourceW && (int) sourceY >= 0 && (int) sourceY < sourceH)
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
							
								if((int) sourceY < sourceH)
								{
									pCurrentS = pSource + (int) sourceY * sStride + sourceXInt;
							
									if(xRest == 0)
									{
										if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = *pCurrentS;
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1 - yRest) + pCurrentS[sStride] * yRest);
										}
									}
									else
									{
										if(sourceXInt >= sourceW - 1)
										{
											*pCurrentR = *pCurrentS;
											pCurrentR[1] = pCurrentS[1];
											pCurrentR[2] = pCurrentS[2];
										}
										else if(yRest == 0 || sourceYInt >= sourceH - 1)
										{
											*pCurrentR = (byte) (*pCurrentS * (1-xRest) + pCurrentS[1] * xRest);
										}
										else
										{
											*pCurrentR = (byte) (*pCurrentS * (1-yRest) * (1-xRest) + pCurrentS[1] * xRest * (1-yRest) + pCurrentS[sStride] * (1-xRest) * yRest + pCurrentS[sStride+1] * xRest * yRest);
										}
									}								
								}
								else
								{
									*pCurrentR = backColor;
								}
							}
							else
							{
								*pCurrentR = backColor;
							}

							pCurrentR += rStride;
							
							sourceX -= xJump;
							sourceY += yJump;
							sourceXInt = (int) sourceX;
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

		#region StretchAndRotate1bppBlackBack()
		private static Bitmap StretchAndRotate1bppBlackBack(Bitmap source, ItPage page)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			try
			{
				double		angle = page.Skew;
				Rectangle	clip = page.Clip.RectangleNotSkewed;
				
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				
				int			x, y, i;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32( sx - Math.Cos(beta - angle) * m );
				int			ulCornerY = Convert.ToInt32( sy - Math.Sin(beta - angle) * m );

				double		sourceX, sourceY;
				int			sourceXInt, sourceYInt;
				double		cosAngle = Math.Cos(-angle);
				double		sinAngle = Math.Sin(-angle);
				double		xJump, yJump;
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				double[]	arrayT, arrayB;
				int lensCenter = page.OpticsCenter;// GetLensCenter(page);
				
				GetCurves(page, out arrayT, out arrayB);

				unsafe 
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pCurrentS;
					byte*	pCurrentR;

					int		topY = lensCenter - clip.Top;
					ulCornerX -= Convert.ToInt32(topY * sinAngle);
					ulCornerY += Convert.ToInt32(topY * cosAngle);

					xJump = sinAngle;

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayT[x] / topY);
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y >= 0; y--)
						{
							pCurrentR = pResult + y * rStride + x / 8;

							if(sourceXInt >= 0 && sourceXInt < sourceW && sourceYInt >= 0 && sourceYInt < sourceH)
							{
								pCurrentS = pSource + sourceYInt * sStride + sourceXInt / 8;
								i = 0x80 >> (sourceXInt & 0x07);
						
								if((*pCurrentS & i) > 0)
									*pCurrentR |= (byte) (0x80 >> (x & 0x07));
							}
							
							sourceX += xJump;
							sourceY -= yJump;
							sourceXInt = (int) sourceX;
							sourceYInt = (int) sourceY;
						}
					}

					xJump = sinAngle;// * (1 - arrayT[x] / topY);

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayB[x] / (clip.Bottom - topY));
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y < resultH; y++)
						{
							pCurrentR = pResult + y * rStride + x / 8;

							if(sourceXInt >= 0 && sourceXInt < sourceW && (int) sourceY >= 0 && (int) sourceY < sourceH)
							{
								pCurrentS = pSource + sourceYInt * sStride + sourceXInt / 8;
								i = 0x80 >> (sourceXInt & 0x07);
						
								if((*pCurrentS & i) > 0)
									*pCurrentR |= (byte) (0x80 >> (x & 0x07));
							}

							pCurrentR += rStride;
							
							sourceX -= xJump;
							sourceY += yJump;
							sourceXInt = (int) sourceX;
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

		#region StretchAndRotate1bppWhiteBack()
		private static Bitmap StretchAndRotate1bppWhiteBack(Bitmap source, ItPage page)
		{
			BitmapData	sourceData = null;
			Bitmap		result = null;
			BitmapData	resultData = null;
			
			try
			{
				double		angle = page.Skew;
				Rectangle	clip = page.Clip.RectangleNotSkewed;
				
				int			sx = clip.X + clip.Width / 2;
				int			sy = clip.Y + clip.Height / 2;
				double		beta = Math.Atan2(sy-clip.Y, sx-clip.X);
				double		m = Math.Sqrt((sx-clip.X)*(sx-clip.X) + (sy-clip.Y)*(sy-clip.Y));
				
				int			x, y, i;
				int			resultW = clip.Width;
				int			resultH = clip.Height;
				int			sourceW = source.Width;
				int			sourceH = source.Height;
				int			ulCornerX = Convert.ToInt32( sx - Math.Cos(beta - angle) * m );
				int			ulCornerY = Convert.ToInt32( sy - Math.Sin(beta - angle) * m );

				double		sourceX, sourceY;
				int			sourceXInt, sourceYInt;
				double		cosAngle = Math.Cos(-angle);
				double		sinAngle = Math.Sin(-angle);
				double		xJump, yJump;
				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int			sStride = sourceData.Stride; 
				int			rStride = resultData.Stride; 

				double[]	arrayT, arrayB;
				int lensCenter = page.OpticsCenter;// GetLensCenter(page);
				
				GetCurves(page, out arrayT, out arrayB);

				unsafe 
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					byte*	pCurrentS;
					byte*	pCurrentR;

					int		topY = lensCenter - clip.Top;
					ulCornerX -= Convert.ToInt32(topY * sinAngle);
					ulCornerY += Convert.ToInt32(topY * cosAngle);

					xJump = sinAngle;

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayT[x] / topY);
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y >= 0; y--)
						{
							pCurrentR = pResult + y * rStride + x / 8;

							if(sourceXInt >= 0 && sourceXInt < sourceW && sourceYInt >= 0 && sourceYInt < sourceH)
							{
								pCurrentS = pSource + sourceYInt * sStride + sourceXInt / 8;
								i = 0x80 >> (sourceXInt & 0x07);
						
								if((*pCurrentS & i) > 0)
									*pCurrentR |= (byte) (0x80 >> (x & 0x07));
							}
							else
								*pCurrentR |= (byte) (0x80 >> (x & 0x07));
				
							sourceX += xJump;
							sourceY -= yJump;
							sourceXInt = (int) sourceX;
							sourceYInt = (int) sourceY;
						}
					}

					xJump = sinAngle;// * (1 - arrayT[x] / topY);

					for(x = 0; x < resultW; x++)
					{
						yJump = cosAngle * (1 - arrayB[x] / (clip.Bottom - topY));
						sourceX = ulCornerX + x * cosAngle;
						sourceY = ulCornerY + x * sinAngle;
						pCurrentR = pResult + topY * rStride + x;
						sourceXInt = (int) sourceX;
						sourceYInt = (int) sourceY;

						for(y = topY; y < resultH; y++)
						{
							pCurrentR = pResult + y * rStride + x / 8;

							if(sourceXInt >= 0 && sourceXInt < sourceW && (int) sourceY >= 0 && (int) sourceY < sourceH)
							{
								pCurrentS = pSource + sourceYInt * sStride + sourceXInt / 8;
								i = 0x80 >> (sourceXInt & 0x07);
						
								if((*pCurrentS & i) > 0)
									*pCurrentR |= (byte) (0x80 >> (x & 0x07));
							}
							else
								*pCurrentR |= (byte) (0x80 >> (x & 0x07));

							pCurrentR += rStride;
							
							sourceX -= xJump;
							sourceY += yJump;
							sourceXInt = (int) sourceX;
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

		#region GetCurves()
		private static void GetCurves(ItPage itPage, out double[] arrayT, out double[] arrayB)
		{
			int			lensCenter = itPage.OpticsCenter;// GetLensCenter(itPage);
			arrayT = itPage.Bookfolding.TopCurve.GetNotAngledArray();
			arrayB = itPage.Bookfolding.BottomCurve.GetNotAngledArray();			
			
			double	smallestNumber = int.MaxValue;
			for(int x = 0; x < arrayT.Length; x++)
				if(smallestNumber > arrayT[x])
					smallestNumber = arrayT[x];

			if(smallestNumber != 0)
				for(int x = 0; x < arrayT.Length; x++)
					arrayT[x] = arrayT[x] - smallestNumber;

			if(lensCenter - smallestNumber > 10)
				for(int x = 0; x < arrayT.Length; x++)
					arrayT[x] = arrayT[x] * (lensCenter - itPage.Clip.RectangleNotSkewed.Y) / (lensCenter - smallestNumber);
		
		
			double	biggestNumber = int.MinValue;
			for(int x = 0; x < arrayB.Length; x++)
				if(biggestNumber < arrayB[x])
					biggestNumber = arrayB[x];

			for(int x = 0; x < arrayB.Length; x++)
				arrayB[x] = biggestNumber - arrayB[x];

			double		coef = (itPage.Clip.RectangleNotSkewed.Bottom - lensCenter) / (biggestNumber - lensCenter);
			if(biggestNumber - lensCenter > 10)
				for(int x = 0; x < arrayB.Length; x++)
					arrayB[x] = arrayB[x] * coef;
		}
		#endregion

	}*/
	#endregion
}
