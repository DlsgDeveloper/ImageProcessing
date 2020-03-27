using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

using BIP.Geometry;


namespace ImageProcessing.BigImages
{
	public class CurveCorrectionAndRotation
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public CurveCorrectionAndRotation()
		{
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Execute()
		public void Execute(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, ImageProcessing.IpSettings.ItPage page)
		{
			Execute(itDecoder, destPath, imageFormat, page, 0, 0, 0);
		}

		public void Execute(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, ImageProcessing.IpSettings.ItPage page, byte r, byte g, byte b)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			ImageProcessing.BigImages.ItEncoder itEncoder = null;

			try
			{
				itEncoder = new ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat, Convert.ToInt32(page.ClipRect.Width * itDecoder.Width), Convert.ToInt32(page.ClipRect.Height * itDecoder.Height),
				 itDecoder.DpiX, itDecoder.DpiY);
				
				switch (itDecoder.PixelFormat)
				{
					case PixelFormat.Format32bppRgb:
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format8bppIndexed:
					case PixelFormat.Format1bppIndexed:
						Stretch(itDecoder, itEncoder, page);
						break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			catch (Exception ex)
			{
				try 
				{
					if (itEncoder != null)
						itEncoder.Dispose(); 
				}
				catch { }

				itEncoder = null;

				try
				{
					if (File.Exists(destPath))
						File.Delete(destPath);
				}
				catch { }

				throw new Exception("Big Images, CurveCorrectionAndRotation, Execute(): " + ex.Message);
			}
			finally
			{
				if (itEncoder != null)
					itEncoder.Dispose();
#if DEBUG
				Console.WriteLine(string.Format("Big Images, CurveCorrectionAndRotation: {0}", DateTime.Now.Subtract(start).ToString()));
#endif
			}
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region Stretch()
		private unsafe void Stretch(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.IpSettings.ItPage page)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			int sourceW = itDecoder.Width;
			int sourceH = itDecoder.Height;
			int resultW = itEncoder.Width;
			int resultH = itEncoder.Height;

			int totalSourceSize = (int)(ImageProcessing.Misc.BytesPerPixel(itDecoder.PixelFormat) * sourceW * sourceH);
			int stripWidth = (totalSourceSize < 200000000) ? resultW : 20000;
			int stripHeight = (totalSourceSize < 200000000) ? resultH : 4096;

			Rectangle	resultRectImageCoords = new Rectangle((int)(page.ClipRect.X * sourceW), (int)(page.ClipRect.Y * itDecoder.Height), resultW, resultH);
			int			globalLensCenter = (int)(Math.Max(0, Math.Min(1, page.GlobalOpticsCenter)) * sourceH);
			PointF		centroid = new PointF(resultRectImageCoords.X + resultRectImageCoords.Width / 2.0F, globalLensCenter);

			double[] arrayT;
			double[] arrayB;

			int[] resultLensArray = new int[resultRectImageCoords.Width + 1];
			PointF p1 = new PointF(resultRectImageCoords.X, globalLensCenter);
			PointF p2 = new PointF(resultRectImageCoords.Right, globalLensCenter);

			double widthHeightRatio = itDecoder.Width / (double)itDecoder.Height;
			p1 = Rotation.RotatePoint(p1, centroid, page.Skew);
			p2 = Rotation.RotatePoint(p2, centroid, page.Skew);

			for (int i = 0; i < resultLensArray.Length; i++)
				resultLensArray[i] = Convert.ToInt32(p1.Y + (p2.Y - p1.Y) * (i / (double)(resultLensArray.Length - 1)));

			GetCurves(page, out arrayT, out arrayB, itDecoder.Size, resultLensArray);

			//strip top in image coordinates
			for (int stripY = resultRectImageCoords.Top; stripY < resultRectImageCoords.Top + resultH; stripY += stripHeight)
			{
				int stripB = Math.Min(resultRectImageCoords.Bottom, stripY + stripHeight);

				try
				{
					result = new Bitmap(resultW, stripB - stripY, itDecoder.PixelFormat);
					resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					//strip left in image coordinates
					for (int stripX = resultRectImageCoords.X; stripX < resultRectImageCoords.X + resultW; stripX += stripWidth)
					{
						int stripR = Math.Min(resultRectImageCoords.Right, stripX + stripWidth);

						Rectangle resultClip = Rectangle.FromLTRB(stripX, stripY, stripR, stripB);
						Rectangle sourceRect = GetSourceRect(page.Skew, resultLensArray, centroid, resultClip, arrayT, arrayB, new Size(sourceW, sourceH), resultRectImageCoords);

						if (sourceRect.Width > 0 && sourceRect.Height > 0)
						{
							using (Bitmap source = itDecoder.GetClip(sourceRect))
							{
#if DEBUG
								source.Save(@"C:\delete\source.png", ImageFormat.Png);
#endif
								try
								{
									sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

									byte* pSource = (byte*)sourceData.Scan0.ToPointer();
									byte* pResult = (byte*)resultData.Scan0.ToPointer();

									int strideS = sourceData.Stride;
									int strideR = resultData.Stride;

									byte* pS, pR;

									#region 24 bit
									if (itDecoder.PixelFormat == PixelFormat.Format24bppRgb)
									{
										int bytesPerPixel = (itDecoder.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

										//top part
										if (stripY < resultLensArray[0] || stripY < resultLensArray[resultLensArray.Length - 1])
										{
											for (int x = stripX; x < stripR; x++)
											{
												int xLocal = x - resultRectImageCoords.X;

												//unrotated top point in image coordinates
												PointF pointT = new PointF(x, (float)(resultRectImageCoords.Top + arrayT[x - resultRectImageCoords.X]));
												PointF pointB = new PointF(x, resultLensArray[xLocal]);

												//rotate point in image coordinates
												PointF pointTRotated = Rotation.RotatePoint(pointT, centroid, page.Skew);
												PointF pointBRotated = Rotation.RotatePoint(pointB, centroid, page.Skew);

												double yJump = (pointBRotated.Y - pointTRotated.Y) / (resultLensArray[xLocal] - resultRectImageCoords.Top);
												double xJump = (pointBRotated.X - pointTRotated.X) / (resultLensArray[xLocal] - resultRectImageCoords.Top);

												for (int y = stripY; y < stripB && y <= resultLensArray[xLocal]; y++)
												{
													double sourceX = (pointTRotated.X + (y - resultRectImageCoords.Top) * xJump);
													double sourceY = (pointTRotated.Y + (y - resultRectImageCoords.Top) * yJump);

													double xRest = sourceX - (int)sourceX;
													double yRest = sourceY - (int)sourceY;

													if (sourceX >= sourceRect.Left && sourceY >= sourceRect.Top)
													{
														if (sourceX < sourceRect.Right - 1 && sourceY < sourceRect.Bottom - 1)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = (byte)((1 - xRest) * (1 - yRest) * pS[0] + (xRest) * (1 - yRest) * pS[3] + (1 - xRest) * yRest * pS[strideS + 0] + xRest * yRest * pS[strideS + 3]);
															pR[1] = (byte)((1 - xRest) * (1 - yRest) * pS[1] + (xRest) * (1 - yRest) * pS[4] + (1 - xRest) * yRest * pS[strideS + 1] + xRest * yRest * pS[strideS + 4]);
															pR[2] = (byte)((1 - xRest) * (1 - yRest) * pS[2] + (xRest) * (1 - yRest) * pS[5] + (1 - xRest) * yRest * pS[strideS + 2] + xRest * yRest * pS[strideS + 5]);
														}
														else if (sourceX < sourceRect.Right - 1 && sourceY < sourceRect.Bottom)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = (byte)((1 - xRest) * pS[0] + (xRest) * pS[3]);
															pR[1] = (byte)((1 - xRest) * pS[1] + (xRest) * pS[4]);
															pR[2] = (byte)((1 - xRest) * pS[2] + (xRest) * pS[5]);
														}
														else if (sourceX < sourceRect.Right && sourceY < sourceRect.Bottom - 1)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = (byte)((1 - yRest) * pS[0] + yRest * pS[strideS + 0]);
															pR[1] = (byte)((1 - yRest) * pS[1] + yRest * pS[strideS + 1]);
															pR[2] = (byte)((1 - yRest) * pS[2] + yRest * pS[strideS + 2]);
														}
														else if (sourceX < sourceRect.Right && sourceY < sourceRect.Bottom)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = pS[0];
															pR[1] = pS[1];
															pR[2] = pS[2];
														}
														else
														{
														}
													}
													else
													{
													}
												}
											}
										}

										//bottom part
										if (stripB > resultLensArray[0] || stripB < resultLensArray[resultLensArray.Length - 1])
										{
											for (int x = stripX; x < stripR; x++)
											{
												int xLocal = x - resultRectImageCoords.X;

												//unrotated top point in image coordinates
												PointF pointT = new PointF(x, resultLensArray[xLocal]);
												PointF pointB = new PointF(x, (float)(resultRectImageCoords.Bottom - arrayB[x - resultRectImageCoords.X]));

												//rotate point in image coordinates
												PointF pointTRotated = Rotation.RotatePoint(pointT, centroid, page.Skew);
												PointF pointBRotated = Rotation.RotatePoint(pointB, centroid, page.Skew);

												double yJump = (pointBRotated.Y - pointTRotated.Y) / (resultRectImageCoords.Bottom - resultLensArray[xLocal]);
												double xJump = (pointBRotated.X - pointTRotated.X) / (resultRectImageCoords.Bottom - resultLensArray[xLocal]);

												for (int y = Math.Max(stripY, resultLensArray[xLocal]); y < stripB; y++)
												{
													double sourceX = (pointTRotated.X + (y - resultLensArray[xLocal]) * xJump);
													double sourceY = (pointTRotated.Y + (y - resultLensArray[xLocal]) * yJump);

													double xRest = sourceX - (int)sourceX;
													double yRest = sourceY - (int)sourceY;

													if (sourceX >= sourceRect.Left && sourceY >= sourceRect.Top)
													{
														if (sourceX < sourceRect.Right - 1 && sourceY < sourceRect.Bottom - 1)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = (byte)((1 - xRest) * (1 - yRest) * pS[0] + (xRest) * (1 - yRest) * pS[3] + (1 - xRest) * yRest * pS[strideS + 0] + xRest * yRest * pS[strideS + 3]);
															pR[1] = (byte)((1 - xRest) * (1 - yRest) * pS[1] + (xRest) * (1 - yRest) * pS[4] + (1 - xRest) * yRest * pS[strideS + 1] + xRest * yRest * pS[strideS + 4]);
															pR[2] = (byte)((1 - xRest) * (1 - yRest) * pS[2] + (xRest) * (1 - yRest) * pS[5] + (1 - xRest) * yRest * pS[strideS + 2] + xRest * yRest * pS[strideS + 5]);
														}
														else if (sourceX < sourceRect.Right - 1 && sourceY < sourceRect.Bottom)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = (byte)((1 - xRest) * pS[0] + (xRest) * pS[3]);
															pR[1] = (byte)((1 - xRest) * pS[1] + (xRest) * pS[4]);
															pR[2] = (byte)((1 - xRest) * pS[2] + (xRest) * pS[5]);
														}
														else if (sourceX < sourceRect.Right && sourceY < sourceRect.Bottom - 1)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = (byte)((1 - yRest) * pS[0] + yRest * pS[strideS + 0]);
															pR[1] = (byte)((1 - yRest) * pS[1] + yRest * pS[strideS + 1]);
															pR[2] = (byte)((1 - yRest) * pS[2] + yRest * pS[strideS + 2]);
														}
														else if (sourceX < sourceRect.Right && sourceY < sourceRect.Bottom)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = pS[0];
															pR[1] = pS[1];
															pR[2] = pS[2];
														}
														else
														{
														}
													}
													else
													{
													}
												}
											}

										}
									}
									#endregion

									#region 32 bit
									if (itDecoder.PixelFormat == PixelFormat.Format32bppArgb || itDecoder.PixelFormat == PixelFormat.Format32bppRgb)
									{
										int bytesPerPixel = (itDecoder.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

										//top part
										if (stripY < resultLensArray[0] || stripY < resultLensArray[resultLensArray.Length - 1])
										{
											for (int x = stripX; x < stripR; x++)
											{
												int xLocal = x - resultRectImageCoords.X;

												//unrotated top point in image coordinates
												PointF pointT = new PointF(x, (float)(resultRectImageCoords.Top + arrayT[x - resultRectImageCoords.X]));
												PointF pointB = new PointF(x, resultLensArray[xLocal]);

												//rotate point in image coordinates
												PointF pointTRotated = Rotation.RotatePoint(pointT, centroid, page.Skew);
												PointF pointBRotated = Rotation.RotatePoint(pointB, centroid, page.Skew);

												double yJump = (pointBRotated.Y - pointTRotated.Y) / (resultLensArray[xLocal] - resultRectImageCoords.Top);
												double xJump = (pointBRotated.X - pointTRotated.X) / (resultLensArray[xLocal] - resultRectImageCoords.Top);

												for (int y = stripY; y < stripB && y <= resultLensArray[xLocal]; y++)
												{
													double sourceX = (pointTRotated.X + (y - resultRectImageCoords.Top) * xJump);
													double sourceY = (pointTRotated.Y + (y - resultRectImageCoords.Top) * yJump);

													double xRest = sourceX - (int)sourceX;
													double yRest = sourceY - (int)sourceY;

													if (sourceX >= sourceRect.Left && sourceY >= sourceRect.Top)
													{
														if (sourceX < sourceRect.Right - 1 && sourceY < sourceRect.Bottom - 1)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = (byte)((1 - xRest) * (1 - yRest) * pS[0] + (xRest) * (1 - yRest) * pS[4] + (1 - xRest) * yRest * pS[strideS + 0] + xRest * yRest * pS[strideS + 4]);
															pR[1] = (byte)((1 - xRest) * (1 - yRest) * pS[1] + (xRest) * (1 - yRest) * pS[5] + (1 - xRest) * yRest * pS[strideS + 1] + xRest * yRest * pS[strideS + 5]);
															pR[2] = (byte)((1 - xRest) * (1 - yRest) * pS[2] + (xRest) * (1 - yRest) * pS[6] + (1 - xRest) * yRest * pS[strideS + 2] + xRest * yRest * pS[strideS + 6]);
															pR[3] = 255;
														}
														else if (sourceX < sourceRect.Right - 1 && sourceY < sourceRect.Bottom)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = (byte)((1 - xRest) * pS[0] + (xRest) * pS[4]);
															pR[1] = (byte)((1 - xRest) * pS[1] + (xRest) * pS[5]);
															pR[2] = (byte)((1 - xRest) * pS[2] + (xRest) * pS[6]);
															pR[3] = 255;
														}
														else if (sourceX < sourceRect.Right && sourceY < sourceRect.Bottom - 1)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = (byte)((1 - yRest) * pS[0] + yRest * pS[strideS + 0]);
															pR[1] = (byte)((1 - yRest) * pS[1] + yRest * pS[strideS + 1]);
															pR[2] = (byte)((1 - yRest) * pS[2] + yRest * pS[strideS + 2]);
															pR[3] = 255;
														}
														else if (sourceX < sourceRect.Right && sourceY < sourceRect.Bottom)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = pS[0];
															pR[1] = pS[1];
															pR[2] = pS[2];
															pR[3] = 255;
														}
														else
														{
														}
													}
													else
													{
													}
												}
											}
										}

										//bottom part
										if (stripB > resultLensArray[0] || stripB < resultLensArray[resultLensArray.Length - 1])
										{
											for (int x = stripX; x < stripR; x++)
											{
												int xLocal = x - resultRectImageCoords.X;

												//unrotated top point in image coordinates
												PointF pointT = new PointF(x, resultLensArray[xLocal]);
												PointF pointB = new PointF(x, (float)(resultRectImageCoords.Bottom - arrayB[x - resultRectImageCoords.X]));

												//rotate point in image coordinates
												PointF pointTRotated = Rotation.RotatePoint(pointT, centroid, page.Skew);
												PointF pointBRotated = Rotation.RotatePoint(pointB, centroid, page.Skew);

												double yJump = (pointBRotated.Y - pointTRotated.Y) / (resultRectImageCoords.Bottom - resultLensArray[xLocal]);
												double xJump = (pointBRotated.X - pointTRotated.X) / (resultRectImageCoords.Bottom - resultLensArray[xLocal]);

												for (int y = Math.Max(stripY, resultLensArray[xLocal]); y < stripB; y++)
												{
													double sourceX = (pointTRotated.X + (y - resultLensArray[xLocal]) * xJump);
													double sourceY = (pointTRotated.Y + (y - resultLensArray[xLocal]) * yJump);

													double xRest = sourceX - (int)sourceX;
													double yRest = sourceY - (int)sourceY;

													if (sourceX >= sourceRect.Left && sourceY >= sourceRect.Top)
													{
														if (sourceX < sourceRect.Right - 1 && sourceY < sourceRect.Bottom - 1)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = (byte)((1 - xRest) * (1 - yRest) * pS[0] + (xRest) * (1 - yRest) * pS[4] + (1 - xRest) * yRest * pS[strideS + 0] + xRest * yRest * pS[strideS + 4]);
															pR[1] = (byte)((1 - xRest) * (1 - yRest) * pS[1] + (xRest) * (1 - yRest) * pS[5] + (1 - xRest) * yRest * pS[strideS + 1] + xRest * yRest * pS[strideS + 5]);
															pR[2] = (byte)((1 - xRest) * (1 - yRest) * pS[2] + (xRest) * (1 - yRest) * pS[6] + (1 - xRest) * yRest * pS[strideS + 2] + xRest * yRest * pS[strideS + 6]);
															pR[3] = 255;
														}
														else if (sourceX < sourceRect.Right - 1 && sourceY < sourceRect.Bottom)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = (byte)((1 - xRest) * pS[0] + (xRest) * pS[4]);
															pR[1] = (byte)((1 - xRest) * pS[1] + (xRest) * pS[5]);
															pR[2] = (byte)((1 - xRest) * pS[2] + (xRest) * pS[6]);
															pR[3] = 255;
														}
														else if (sourceX < sourceRect.Right && sourceY < sourceRect.Bottom - 1)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = (byte)((1 - yRest) * pS[0] + yRest * pS[strideS + 0]);
															pR[1] = (byte)((1 - yRest) * pS[1] + yRest * pS[strideS + 1]);
															pR[2] = (byte)((1 - yRest) * pS[2] + yRest * pS[strideS + 2]);
															pR[3] = 255;
														}
														else if (sourceX < sourceRect.Right && sourceY < sourceRect.Bottom)
														{
															pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) * bytesPerPixel;
															pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X) * bytesPerPixel;

															pR[0] = pS[0];
															pR[1] = pS[1];
															pR[2] = pS[2];
															pR[3] = 255;
														}
														else
														{
														}
													}
													else
													{
													}
												}
											}
										}
									}
									#endregion

									#region 8 bit
									//8 bit indexed bitmaps
									else if (itDecoder.PixelFormat == PixelFormat.Format8bppIndexed)
									{
										for (int x = stripX; x < stripR; x++)
										{
											int xLocal = x - resultRectImageCoords.X;

											//unrotated top point in image coordinates
											PointF pointT = new PointF(x, (float)(resultRectImageCoords.Top + arrayT[x - resultRectImageCoords.X]));
											PointF pointB = new PointF(x, resultLensArray[xLocal]);

											//rotate point in image coordinates
											PointF pointTRotated = Rotation.RotatePoint(pointT, centroid, page.Skew);
											PointF pointBRotated = Rotation.RotatePoint(pointB, centroid, page.Skew);

											double yJump = (pointBRotated.Y - pointTRotated.Y) / (resultLensArray[xLocal] - resultRectImageCoords.Top);
											double xJump = (pointBRotated.X - pointTRotated.X) / (resultLensArray[xLocal] - resultRectImageCoords.Top);

											for (int y = stripY; y < stripB && y <= resultLensArray[xLocal]; y++)
											{
												double sourceX = (pointTRotated.X + (y - resultRectImageCoords.Top) * xJump);
												double sourceY = (pointTRotated.Y + (y - resultRectImageCoords.Top) * yJump);

												double xRest = sourceX - (int)sourceX;
												double yRest = sourceY - (int)sourceY;

												if (sourceX >= sourceRect.Left && sourceY >= sourceRect.Top)
												{
													if (sourceX < sourceRect.Right - 1 && sourceY < sourceRect.Bottom - 1)
													{
														pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left);
														pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X);

														*pR = (byte)((1 - xRest) * (1 - yRest) * pS[0] + (xRest) * (1 - yRest) * pS[1] + (1 - xRest) * yRest * pS[strideS] + xRest * yRest * pS[strideS + 1]);
													}
													else if (sourceX < sourceRect.Right - 1 && sourceY < sourceRect.Bottom)
													{
														pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left);
														pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X);

														*pR = (byte)((1 - xRest) * pS[0] + (xRest) * pS[1]);
													}
													else if (sourceX < sourceRect.Right && sourceY < sourceRect.Bottom - 1)
													{
														pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left);
														pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X);

														*pR = (byte)((1 - yRest) * pS[0] + yRest * pS[strideS]);
													}
													else if (sourceX < sourceRect.Right && sourceY < sourceRect.Bottom)
													{
														pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left);
														pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X);

														*pR = *pS;
													}
													else
													{
													}
												}
												else
												{
												}
											}
										}

										//bottom
										for (int x = stripX; x < stripR; x++)
										{
											int xLocal = x - resultRectImageCoords.X;

											//unrotated top point in image coordinates
											PointF pointT = new PointF(x, resultLensArray[xLocal]);
											PointF pointB = new PointF(x, (float)(resultRectImageCoords.Bottom - arrayB[x - resultRectImageCoords.X]));

											//rotate point in image coordinates
											PointF pointTRotated = Rotation.RotatePoint(pointT, centroid, page.Skew);
											PointF pointBRotated = Rotation.RotatePoint(pointB, centroid, page.Skew);

											double yJump = (pointBRotated.Y - pointTRotated.Y) / (resultRectImageCoords.Bottom - resultLensArray[xLocal]);
											double xJump = (pointBRotated.X - pointTRotated.X) / (resultRectImageCoords.Bottom - resultLensArray[xLocal]);

											for (int y = Math.Max(stripY, resultLensArray[xLocal]); y < stripB; y++)
											{
												double sourceX = (pointTRotated.X + (y - resultLensArray[xLocal]) * xJump);
												double sourceY = (pointTRotated.Y + (y - resultLensArray[xLocal]) * yJump);

												double xRest = sourceX - (int)sourceX;
												double yRest = sourceY - (int)sourceY;

												if (sourceX >= sourceRect.Left && sourceY >= sourceRect.Top)
												{
													if (sourceX < sourceRect.Right - 1 && sourceY < sourceRect.Bottom - 1)
													{
														pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left);
														pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X);

														*pR = (byte)((1 - xRest) * (1 - yRest) * pS[0] + (xRest) * (1 - yRest) * pS[1] + (1 - xRest) * yRest * pS[strideS] + xRest * yRest * pS[strideS + 1]);
													}
													else if (sourceX < sourceRect.Right - 1 && sourceY < sourceRect.Bottom)
													{
														pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left);
														pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X);

														*pR = (byte)((1 - xRest) * pS[0] + (xRest) * pS[1]);
													}
													else if (sourceX < sourceRect.Right && sourceY < sourceRect.Bottom - 1)
													{
														pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left);
														pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X);

														*pR = (byte)((1 - yRest) * pS[0] + yRest * pS[strideS]);
													}
													else if (sourceX < sourceRect.Right && sourceY < sourceRect.Bottom)
													{
														pS = pSource + ((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left);
														pR = pResult + (y - stripY) * strideR + (x - resultRectImageCoords.X);

														*pR = *pS;
													}
													else
													{
													}
												}
												else
												{
												}
											}
										}
									}
									#endregion

									#region 1 bit
									//1 bit indexed bitmaps
									else if (itDecoder.PixelFormat == PixelFormat.Format1bppIndexed)
									{
										for (int x = stripX; x < stripR; x++)
										{
											int xLocal = x - resultRectImageCoords.X;

											//unrotated top point in image coordinates
											PointF pointT = new PointF(x, (float)(resultRectImageCoords.Top + arrayT[x - resultRectImageCoords.X]));
											PointF pointB = new PointF(x, resultLensArray[xLocal]);

											//rotate point in image coordinates
											PointF pointTRotated = Rotation.RotatePoint(pointT, centroid, page.Skew);
											PointF pointBRotated = Rotation.RotatePoint(pointB, centroid, page.Skew);

											double yJump = (pointBRotated.Y - pointTRotated.Y) / (resultLensArray[xLocal] - resultRectImageCoords.Top);
											double xJump = (pointBRotated.X - pointTRotated.X) / (resultLensArray[xLocal] - resultRectImageCoords.Top);

											for (int y = stripY; y < stripB && y <= resultLensArray[xLocal]; y++)
											{
												double sourceX = (pointTRotated.X + (y - resultRectImageCoords.Top) * xJump);
												double sourceY = (pointTRotated.Y + (y - resultRectImageCoords.Top) * yJump);

												double xRest = sourceX - (int)sourceX;
												double yRest = sourceY - (int)sourceY;

												if (sourceX >= sourceRect.Left && sourceY >= sourceRect.Top && sourceX < sourceRect.Right && sourceY < sourceRect.Bottom)
												{
													if ((pSource[((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) / 8] & (0x80 >> (((int)sourceX - sourceRect.Left) & 0x07))) > 0)
														pResult[(y - stripY) * strideR + (xLocal) / 8] |= (byte)(0x80 >> (xLocal & 0x7));
												}
												/*else
												{
													y = y;
												}*/
											}
										}
										//bottom
										for (int x = stripX; x < stripR; x++)
										{
											int xLocal = x - resultRectImageCoords.X;

											//unrotated top point in image coordinates
											PointF pointT = new PointF(x, resultLensArray[xLocal]);
											PointF pointB = new PointF(x, (float)(resultRectImageCoords.Bottom - arrayB[x - resultRectImageCoords.X]));

											//rotate point in image coordinates
											PointF pointTRotated = Rotation.RotatePoint(pointT, centroid, page.Skew);
											PointF pointBRotated = Rotation.RotatePoint(pointB, centroid, page.Skew);

											double yJump = (pointBRotated.Y - pointTRotated.Y) / (resultRectImageCoords.Bottom - resultLensArray[xLocal]);
											double xJump = (pointBRotated.X - pointTRotated.X) / (resultRectImageCoords.Bottom - resultLensArray[xLocal]);

											for (int y = Math.Max(stripY, resultLensArray[xLocal]); y < stripB; y++)
											{
												double sourceX = (pointTRotated.X + (y - resultLensArray[xLocal]) * xJump);
												double sourceY = (pointTRotated.Y + (y - resultLensArray[xLocal]) * yJump);

												double xRest = sourceX - (int)sourceX;
												double yRest = sourceY - (int)sourceY;

												if (sourceX >= sourceRect.Left && sourceY >= sourceRect.Top && sourceX < sourceRect.Right && sourceY < sourceRect.Bottom)
												{
													if ((pSource[((int)sourceY - sourceRect.Top) * strideS + ((int)sourceX - sourceRect.Left) / 8] & (0x80 >> (((int)sourceX - sourceRect.Left) & 0x07))) > 0)
														pResult[(y - stripY) * strideR + (x - resultRectImageCoords.X) / 8] |= (byte)(0x80 >> (xLocal & 0x7));
												}
												/*else
												{
													y = y;
												}*/
											}
										}
									}
									#endregion
								}
								finally
								{
									if (sourceData != null)
									{
										source.UnlockBits(sourceData);
										sourceData = null;
									}
								}
							}
						}
					}
				}
				finally
				{
					if (resultData != null)
					{
						itEncoder.Write(stripB - stripY, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
						result.UnlockBits(resultData);
						resultData = null;
					}
#if DEBUG
					result.Save(@"C:\delete\result.png", ImageFormat.Png);
#endif			
					if (result != null)
					{
						result.Dispose();
						result = null;
					}
				}

				if (ProgressChanged != null)
					ProgressChanged((stripB - resultRectImageCoords.Top) / (float)resultH);
			}
		}
		#endregion

		#region GetCurves()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="arrayT">Values are positive, smallest number is 0</param>
		/// <param name="arrayB">Values are negative, biggest number is 0</param>
		/// <param name="sourceSize"></param>
		/// <param name="resultSize"></param>
		private static void GetCurves(ImageProcessing.IpSettings.ItPage page, out double[] arrayT, out double[] arrayB, Size sourceSize, int[] lensArray)
		{
			int x;
			Rectangle imageRect = new Rectangle(Convert.ToInt32(page.Clip.RectangleNotSkewed.X * sourceSize.Width), Convert.ToInt32(page.Clip.RectangleNotSkewed.Y * sourceSize.Height),
				Convert.ToInt32(page.Clip.RectangleNotSkewed.Width * sourceSize.Width), Convert.ToInt32(page.Clip.RectangleNotSkewed.Height * sourceSize.Height));

			arrayT = page.Bookfolding.TopCurve.GetNotAngledArray(sourceSize, lensArray.Length);
			arrayB = page.Bookfolding.BottomCurve.GetNotAngledArray(sourceSize, lensArray.Length);

			double smallestTop = double.MaxValue;

			for (x = 0; x < arrayT.Length; x++)
			{
				if (smallestTop > arrayT[x])
					smallestTop = arrayT[x];
			}

			if (smallestTop != double.MaxValue)
				for (x = 0; x < arrayT.Length; x++)
					arrayT[x] -= smallestTop;

			for (x = 0; x < arrayT.Length; x++)
				if ((lensArray[x] - smallestTop) > 10.0)
					arrayT[x] = arrayT[x] * (lensArray[x] - imageRect.Y) / (lensArray[x] - smallestTop);


			double biggestNumber = double.MinValue;
			for (x = 0; x < arrayB.Length; x++)
				if (biggestNumber < arrayB[x])
					biggestNumber = arrayB[x];

			for (x = 0; x < arrayB.Length; x++)
				arrayB[x] = biggestNumber - arrayB[x];

			for (x = 0; x < arrayB.Length; x++)
				if ((biggestNumber - lensArray[x]) > 10.0)
					arrayB[x] = (arrayB[x] * (imageRect.Bottom - lensArray[x])) / (biggestNumber - lensArray[x]);
		}
		#endregion

		#region GetSourceRect()
		private Rectangle GetSourceRect(double angle, int[] lensArray, PointF centroidR, Rectangle resultClip, double[] arrayT, double[] arrayB, Size sourceSize, Rectangle resultRect)
		{
			float		y;
			float		left = float.MaxValue;
			float		top = float.MaxValue;
			float		right = float.MinValue;
			float		bottom = float.MinValue;
			Rectangle	sourceRect = new Rectangle(0, 0, sourceSize.Width, sourceSize.Height);
			PointF		centroid = new PointF((float)centroidR.X, (float)centroidR.Y);
			PointF		aPoint;

			//top
			for (int x = 0; x < resultClip.Width; x++)
			{
				if (resultClip.Top <= lensArray[x])
					y = (float)(arrayT[x + resultClip.X - resultRect.X] * ((lensArray[x + resultClip.X - resultRect.X] - resultClip.Top) / (double)(lensArray[x + resultClip.X - resultRect.X] - resultRect.Top)));
				else
					y = (float)(arrayB[x + resultClip.X - resultRect.X] * ((resultClip.Top - lensArray[x + resultClip.X - resultRect.X]) / (double)(resultRect.Bottom - lensArray[x + resultClip.X - resultRect.X])));
					
				aPoint = Rotation.RotatePoint(new PointF(x + resultClip.Left, resultClip.Top - y), centroid, angle);

				if (left > aPoint.X)
					left = aPoint.X;
				if (top > aPoint.Y)
					top = aPoint.Y;
				if (right < aPoint.X)
					right = aPoint.X;
				if (bottom < aPoint.Y)
					bottom = aPoint.Y;

				if (resultClip.Bottom <= lensArray[x])
					y = (float)(arrayT[x + resultClip.X - resultRect.X] * ((lensArray[x + resultClip.X - resultRect.X] - resultClip.Bottom) / (double)(lensArray[x + resultClip.X - resultRect.X] - resultRect.Top)));
				else
					y = (float)(arrayB[x + resultClip.X - resultRect.X] * ((resultClip.Bottom - lensArray[x + resultClip.X - resultRect.X]) / (double)(resultRect.Bottom - lensArray[x + resultClip.X - resultRect.X])));

				aPoint = Rotation.RotatePoint(new PointF(x + resultClip.Left, resultClip.Bottom + y), centroid, angle);

				if (left > aPoint.X)
					left = aPoint.X;
				if (top > aPoint.Y)
					top = aPoint.Y;
				if (right < aPoint.X)
					right = aPoint.X;
				if (bottom < aPoint.Y)
					bottom = aPoint.Y;
			}

			Rectangle rect = Rectangle.FromLTRB((int)left, (int)top, (int)Math.Ceiling(right), (int)Math.Ceiling(bottom));
			rect.Intersect(sourceRect);

			return rect;
		}
		#endregion

		#endregion

	}
}
