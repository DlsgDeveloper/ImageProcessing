using BIP.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;


namespace ImageProcessing.BigImages
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class CropAndDeskew
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public CropAndDeskew()
		{
		}
		#endregion


		//	PUBLIC METHODS
		#region public methods

		#region GetCropAndDescewObject()
		public ImageProcessing.CropDeskew.CdObject GetCdObject(ImageProcessing.BigImages.ItDecoder itDecoder, Color threshold, bool backDark, out byte confidence,
			float minAngleToDeskew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, short marginX, short marginY)
		{
			confidence = 0;

			if (clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, itDecoder.Width, itDecoder.Height);
			else if (clip.Width <= 0 || clip.Height <= 0)
				clip = Rectangle.FromLTRB(clip.X, clip.Y, itDecoder.Width - clip.X * 2, itDecoder.Height - clip.Y * 2);

			try
			{
				Bitmap bwBitmap;

				if (itDecoder.PixelFormat != PixelFormat.Format1bppIndexed)
				{
					ImageProcessing.BigImages.Binarization binorization = new ImageProcessing.BigImages.Binarization();

					binorization.ProgressChanged += delegate(float progress)
					{
						if (this.ProgressChanged != null)
							this.ProgressChanged(progress * 0.4F);
					};

					bwBitmap = binorization.ThresholdToBitmap(itDecoder, clip, new Binarization.BinarizationParameters(threshold.R, threshold.G, threshold.B));
				}
				else
					bwBitmap = itDecoder.GetClip(clip);

				if (this.ProgressChanged != null)
					this.ProgressChanged(0.4F);

#if SAVE_RESULTS
				bwBitmap.Save(Debug.SaveToDir + "CropAndDescewBinorized.png", ImageFormat.Png);
#endif

				if (removeGhostLines && itDecoder.PixelFormat != PixelFormat.Format1bppIndexed)
				{
					List<int> ghostLines = ImageProcessing.BigImages.GhostLinesRemoval.Get(itDecoder, lowThreshold, highThreshold, linesToCheck, maxDelta);

					for (int i = 0; i < ghostLines.Count; i++)
						ghostLines[i] -= clip.X;

					if (ghostLines.Count > 0)
						RemoveGhostLines(bwBitmap, ghostLines);
				}

				if (this.ProgressChanged != null)
					this.ProgressChanged(0.5F);

/*#if DEBUG
				DateTime start = DateTime.Now;
#endif*/
				ImageProcessing.NoiseReduction noiseReduction = new ImageProcessing.NoiseReduction();

				noiseReduction.Despeckle(bwBitmap, NoiseReduction.DespeckleSize.Size6x6, NoiseReduction.DespeckleMethod.Regions, NoiseReduction.DespeckleMode.WhiteSpecklesOnly);
/*#if DEBUG
				Console.WriteLine("NoiseReduction(): " + DateTime.Now.Subtract(start).ToString());
#endif*/

				if (this.ProgressChanged != null)
					this.ProgressChanged(0.6F);

#if SAVE_RESULTS
				bwBitmap.Save(Debug.SaveToDir + "CropAndDescewBinorized.png", ImageFormat.Png);
#endif

				ImageProcessing.CropDeskew.Crop crop = new ImageProcessing.CropDeskew.Crop(bwBitmap);
				ImageProcessing.CropDeskew.CdObject cdObject = new ImageProcessing.CropDeskew.CdObject(crop.Angle,
					new RatioPoint(crop.Cul.X / (double)bwBitmap.Width, crop.Cul.Y / (double)bwBitmap.Height),
					new RatioPoint(crop.Cur.X / (double)bwBitmap.Width, crop.Cur.Y / (double)bwBitmap.Height),
					new RatioPoint(crop.Cll.X / (double)bwBitmap.Width, crop.Cll.Y / (double)bwBitmap.Height),
					new RatioPoint(crop.Clr.X / (double)bwBitmap.Width, crop.Clr.Y / (double)bwBitmap.Height),
					bwBitmap.Width / (double)bwBitmap.Height);

				cdObject.Offset(clip.Location.X / (double)bwBitmap.Width, clip.Location.Y / (double)bwBitmap.Height);
				cdObject.Inflate(-marginX / (double)bwBitmap.Width);

				if (this.ProgressChanged != null)
					this.ProgressChanged(0.8F);

#if SAVE_RESULTS
				bwBitmap.Save(Debug.SaveToDir + "CropAndDescewBinorized.png", ImageFormat.Png);
#endif

				if (cdObject.Inclined)
				{
					if (IsEntireClipInsideSource(itDecoder.Size, cdObject))
					{
						switch (cdObject.ValidCorners)
						{
							case 0: confidence = 0; break;
							case 1: confidence = 30; break;
							case 2:
							case 3: confidence = 80; break;
							default: confidence = 100; break;
						}
					}
					else
					{
						switch (cdObject.ValidCorners)
						{
							case 0: confidence = 0; break;
							case 1: confidence = 20; break;
							default: confidence = 50; break;
						}
					}
				}
				else
				{
					switch (cdObject.ValidCorners)
					{
						case 0: confidence = 0; break;
						case 1: confidence = 30; break;
						case 2:
						case 3: confidence = 80; break;
						default: confidence = 100; break;
					}
				}

				if (this.ProgressChanged != null)
					this.ProgressChanged(1F);

				return cdObject;
			}
			catch (Exception ex)
			{
				string error = (itDecoder != null) ? "Bitmap: Exists, " : "Bitmap: null, ";

				error += (itDecoder != null) ? "Pixel Format: " + itDecoder.PixelFormat.ToString() : "";
				error += ", Threshold: " + threshold.ToString();
				error += ", Confidence: " + confidence.ToString();
				error += ", Angle: " + minAngleToDeskew.ToString();
				error += ", Clip: " + clip.ToString();
				error += ", Background black: " + backDark.ToString();
				error += ", Remove Ghost Lines: " + removeGhostLines.ToString();
				error += ", Low Threshold: " + lowThreshold.ToString();
				error += ", High Threshold: " + highThreshold.ToString();
				error += ", Lines to check: " + linesToCheck.ToString();
				error += ", Max Delta: " + maxDelta.ToString();
				error += ", Exception: " + ex.Message;

				throw new Exception("CropAndDeskew, GetParams(): " + error + "\n");
			}
		}
		#endregion

		#region Execute()
		public void Execute(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.CropDeskew.CdObject cdObject, string destPath, 
			ImageProcessing.FileFormat.IImageFormat imageFormat, bool highQuality)
		{
			if (cdObject.Inclined == false)
			{
				Rectangle rect = new Rectangle(Convert.ToInt32(cdObject.CornerUl.X * itDecoder.Width), Convert.ToInt32(cdObject.CornerUl.Y * itDecoder.Height), Convert.ToInt32(cdObject.Width * itDecoder.Width), Convert.ToInt32(cdObject.Height * itDecoder.Height));
				rect.Intersect(new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));

				ImageProcessing.BigImages.ImageCopier copier = new ImageProcessing.BigImages.ImageCopier();
				copier.ProgressChanged += delegate(float progress)
				{
					if (this.ProgressChanged != null)
						this.ProgressChanged(progress);
				};

				copier.Copy(itDecoder, destPath, imageFormat, rect);
			}
			else
			{
				ImageProcessing.BigImages.ItEncoder itEncoder = null;

				try
				{
					int width = (int)(cdObject.Width * itDecoder.Width / Math.Cos(cdObject.Skew));
					int height = (int)(cdObject.Height * itDecoder.Height / Math.Cos(cdObject.Skew));

					itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat, width, height, itDecoder.DpiX, itDecoder.DpiY);

					switch (itDecoder.PixelFormat)
					{
						case PixelFormat.Format32bppRgb:
						case PixelFormat.Format32bppArgb:
							CropAndDeskew32bpp(itDecoder, itEncoder, cdObject, highQuality);
							break;
						case PixelFormat.Format24bppRgb:
							CropAndDeskew24bpp(itDecoder, itEncoder, cdObject, highQuality);
							break;
						case PixelFormat.Format8bppIndexed:
							CropAndDeskew8bpp(itDecoder, itEncoder, cdObject, highQuality);
							break;
						case PixelFormat.Format1bppIndexed:
							CropAndDeskew1bpp(itDecoder, itEncoder, cdObject);
							break;
						default:
							throw new IpException(ErrorCode.ErrorUnsupportedFormat);
					}

					if (itEncoder != null)
					{
						itEncoder.Dispose();
						itEncoder = null;
					}
				}
				catch (Exception ex)
				{
					try
					{
						if (itEncoder != null)
						{
							itEncoder.Dispose();
							itEncoder = null;
						}
					}
					catch { }

					try
					{
						if (File.Exists(destPath))
							File.Delete(destPath);
					}
					catch { }

					throw new Exception("CropAndDeskew, Execute(): " + ex.Message);
				}
			}
		}
		#endregion
	
		#endregion


		//PRIVATE METHODS
		#region private methods

		#region CropAndDeskew32bpp()
		private void CropAndDeskew32bpp(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.CropDeskew.CdObject cdObject, bool highQuality)
		{
			if (highQuality == false)
				Go32bppFast(itDecoder, itEncoder, cdObject);
			else
				Go32bppQuality(itDecoder, itEncoder, cdObject);
		}
		#endregion

		#region CropAndDeskew24bpp()
		private void CropAndDeskew24bpp(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.CropDeskew.CdObject cdObject, bool highQuality)
		{
			if (highQuality == false)
				Go24bppFast(itDecoder, itEncoder, cdObject);
			else
				Go24bppQuality(itDecoder, itEncoder, cdObject);
		}
		#endregion

		#region CropAndDeskew8bpp()
		private void CropAndDeskew8bpp(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.CropDeskew.CdObject cdObject, bool highQuality)
		{
			if (highQuality == false)
				Go8bppFast(itDecoder, itEncoder, cdObject);
			else
				Go8bppQuality(itDecoder, itEncoder, cdObject);
		}
		#endregion

		#region CropAndDeskew1bpp()
		private void CropAndDeskew1bpp(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.CropDeskew.CdObject cdObject)
		{
			Go1bpp(itDecoder, itEncoder, cdObject);
		}
		#endregion

		#region Go32bppFast()
		private void Go32bppFast(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.CropDeskew.CdObject cdObject)
		{			
			int			x, y;
			Rectangle	decoderRect = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
			int			widthR = itEncoder.Width;
			int			heightR = itEncoder.Height;

			Bitmap		source = null;
			Bitmap		result = null;
			BitmapData	sourceData = null;
			BitmapData	resultData = null;

			double ulCornerX = 0;
			double ulCornerY = 0;

			//double xRowJump = (cdObject.CornerLl.X - cdObject.CornerUl.X) / (double)cdObject.Height;
			//double yRowJump = (Math.Tan(cdObject.Skew));
			double xRowJump = (cdObject.Width * itDecoder.Width) / (double)widthR;
			double yRowJump = (Math.Tan(cdObject.Skew) * (cdObject.Width * itDecoder.Width)) / widthR;
			
			unsafe
			{
				for (int stripY = 0; stripY < heightR; stripY += 2048)
				{
/*#if DEBUG
					DateTime start = DateTime.Now;
#endif*/
					int stripHeight = Math.Min(heightR - stripY, 2048);

					try
					{
						result = new Bitmap(widthR, stripHeight, PixelFormat.Format32bppRgb);
						resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

						for (int stripX = 0; stripX < widthR; stripX += 2048)
						{
							int stripWidth = Math.Min(widthR - stripX, 2048);
							bool isClipInsideSource;
							Rectangle sourceRect = GetSourceRect(new Rectangle(stripX, stripY, stripWidth, stripHeight), cdObject, ref ulCornerX, ref ulCornerY, decoderRect, out isClipInsideSource);

							try
							{
								source = itDecoder.GetClip(sourceRect);
								sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

								int sStride = sourceData.Stride;
								int rStride = resultData.Stride;

								byte* pSource = (byte*)sourceData.Scan0.ToPointer();
								byte* pResult = (byte*)resultData.Scan0.ToPointer();

								double xPixelLength = cdObject.Width / (double)widthR;
								double yPixelLength = cdObject.Height / (double)heightR;

								int xTmp, yTmp;

								byte* pOrigCurrent;
								byte* pCopyCurrent;

								if (isClipInsideSource)
								{
									for (y = 0; y < stripHeight; y++)
									{
										double currentXOffset = ulCornerX + (y * xRowJump);
										double currentYOffset = (y * yPixelLength) + ulCornerY;
										pCopyCurrent = pResult + y * rStride + stripX * 4;

										for (x = 0; x < stripWidth; x++)
										{
											xTmp = Convert.ToInt32(currentXOffset + (x * xPixelLength));
											yTmp = Convert.ToInt32(currentYOffset + (yRowJump * x));
											pOrigCurrent = pSource + yTmp * sStride + xTmp * 4;
											//pOrigCurrent = pSource + (int)(currentYOffset + (yRowJump * x)) * sStride + (int)(currentXOffset + (x * xPixelLength)) * 4;

											*(pCopyCurrent++) = *(pOrigCurrent++);
											*(pCopyCurrent++) = *(pOrigCurrent++);
											*(pCopyCurrent++) = *(pOrigCurrent);

											pCopyCurrent++;
										}
									}
								}
								else
								{
									for (y = 0; y < stripHeight; y++)
									{
										double currentXOffset = ulCornerX + (y * xRowJump);
										double currentYOffset = (y * yPixelLength) + ulCornerY;
										pCopyCurrent = pResult + y * rStride + stripX * 4;

										for (x = 0; x < stripWidth; x++)
										{
											xTmp = Convert.ToInt32(currentXOffset + (x * xPixelLength));
											yTmp = Convert.ToInt32(currentYOffset + (yRowJump * x));

											if (xTmp >= 0 && yTmp >= 0 && xTmp < sourceRect.Width && yTmp < sourceRect.Height)
											{
												pOrigCurrent = pSource + yTmp * sStride + xTmp * 4;

												*(pCopyCurrent++) = *(pOrigCurrent++);
												*(pCopyCurrent++) = *(pOrigCurrent++);
												*(pCopyCurrent++) = *(pOrigCurrent);

												pCopyCurrent++;
											}
											else
												pCopyCurrent += 4;
										}
									}
								}
							}
							finally
							{
								if (sourceData != null)
									source.UnlockBits(sourceData);
								if (source != null)
									source.Dispose();
							}
						}
					}
					finally
					{
						if (resultData != null)
						{
							itEncoder.Write(stripHeight, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
							result.UnlockBits(resultData);
							resultData = null;
						}
						if (result != null)
						{
							result.Dispose();
							result = null;
						}
					}

					if (this.ProgressChanged != null)
						this.ProgressChanged((stripY + stripHeight) / (float)heightR);

/*#if DEBUG
					Console.WriteLine("Crop & Deskew, Go32bppFast() strip processing time: " + DateTime.Now.Subtract(start).ToString());
#endif*/
				}
			}
		}
		#endregion

		#region Go32bppQuality()
		private void Go32bppQuality(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.CropDeskew.CdObject cdObject)
		{
			int			x, y;
			Rectangle	decoderRect = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
			int			widthR = itEncoder.Width;
			int			heightR = itEncoder.Height;

			Bitmap source = null;
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			double ulCornerX = 0;
			double ulCornerY = 0;

			//double xRowJump = (cdObject.CornerLl.X - cdObject.CornerUl.X) / (double)cdObject.Height;
			//double yRowJump = (Math.Tan(cdObject.Skew));
			double xRowJump = (cdObject.Width * itDecoder.Width) / (double)widthR;
			double yRowJump = (Math.Tan(cdObject.Skew) * (cdObject.Width * itDecoder.Width)) / widthR;
		
			unsafe
			{
				for (int stripY = 0; stripY < heightR; stripY += 2048)
				{
/*#if DEBUG
					DateTime start = DateTime.Now;
#endif*/

					int stripHeight = Math.Min(heightR - stripY, 2048);

					try
					{
						result = new Bitmap(widthR, stripHeight, PixelFormat.Format32bppRgb);
						resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

						for (int stripX = 0; stripX < widthR; stripX += 2048)
						{
							int stripWidth = Math.Min(widthR - stripX, 2048);
							bool isClipInsideSource;
							Rectangle sourceRect = GetSourceRect(new Rectangle(stripX, stripY, stripWidth, stripHeight), cdObject, ref ulCornerX, ref ulCornerY, decoderRect, out isClipInsideSource);

							try
							{
								source = itDecoder.GetClip(sourceRect);
								sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

								int sStride = sourceData.Stride;
								int rStride = resultData.Stride;

								byte* pSource = (byte*)sourceData.Scan0.ToPointer();
								byte* pResult = (byte*)resultData.Scan0.ToPointer();

								double xPixelLength = cdObject.Width / (double)widthR;
								double yPixelLength = cdObject.Height / (double)heightR;

								double xSource, ySource;
								double xRest, yRest;

								byte* pS;
								byte* pR;

								if (isClipInsideSource)
								{
									for (y = 0; y < stripHeight; y++)
									{
										double currentXOffset = ulCornerX + (y * xRowJump);
										double currentYOffset = (y * yPixelLength) + ulCornerY;
										pR = pResult + y * rStride + stripX * 4;

										xSource = currentXOffset;
										ySource = currentYOffset;

										for (x = 0; x < stripWidth; x++)
										{
											xRest = xSource - (int)xSource;
											yRest = ySource - (int)ySource;

											if (xRest < 0.0001)
												xRest = 0;
											else if (xRest > 0.9999)
											{
												xRest = 0;
												xSource = (int)xSource + 1;
											}

											if (yRest < 0.0001)
												yRest = 0;
											else if (yRest > 0.9999)
											{
												yRest = 0;
												ySource = (int)ySource + 1;
											}

											pS = pSource + (int)ySource * sStride + (int)xSource * 4;

											if (xRest == 0)
											{
												if (yRest == 0)
												{
													*(pR++) = *(pS++);
													*(pR++) = *(pS++);
													*(pR++) = *(pS);
												}
												else
												{
													*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
													pS++;
													*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
													pS++;
													*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
												}
											}
											else
											{
												if (yRest == 0)
												{
													*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[4] * xRest);
													pS++;
													*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[4] * xRest);
													pS++;
													*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[4] * xRest);
												}
												else
												{
													*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
														pS[4] * xRest * (1 - yRest) +
														pS[sStride] * (1 - xRest) * yRest +
														pS[sStride + 4] * xRest * yRest);
													pS++;
													*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
														pS[4] * xRest * (1 - yRest) +
														pS[sStride] * (1 - xRest) * yRest +
														pS[sStride + 4] * xRest * yRest);
													pS++;
													*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
														pS[4] * xRest * (1 - yRest) +
														pS[sStride] * (1 - xRest) * yRest +
														pS[sStride + 4] * xRest * yRest);
												}
											}

											pR++;
											xSource += xPixelLength;
											ySource += yRowJump;
										}
									}
								}
								else
								{
									for (y = 0; y < stripHeight; y++)
									{
										double currentXOffset = ulCornerX + (y * xRowJump);
										double currentYOffset = (y * yPixelLength) + ulCornerY;
										pR = pResult + y * rStride + stripX * 4;

										xSource = currentXOffset;
										ySource = currentYOffset;

										for (x = 0; x < stripWidth; x++)
										{
											xRest = xSource - (int)xSource;
											yRest = ySource - (int)ySource;

											if (xRest < 0.0001)
												xRest = 0;
											else if (xRest > 0.9999)
											{
												xRest = 0;
												xSource = (int)xSource + 1;
											}

											if (yRest < 0.0001)
												yRest = 0;
											else if (yRest > 0.9999)
											{
												yRest = 0;
												ySource = (int)ySource + 1;
											}

											if (ySource >= 0 && xSource >= 0 && xSource < sourceRect.Width - 1 && ySource < sourceRect.Height - 1)
											{
												pS = pSource + (int)ySource * sStride + (int)xSource * 4;

												if (xRest == 0)
												{
													if (yRest == 0)
													{
														*(pR++) = *(pS++);
														*(pR++) = *(pS++);
														*(pR++) = *(pS);
													}
													else
													{
														*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
														pS++;
														*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
														pS++;
														*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
													}
												}
												else
												{
													if (yRest == 0)
													{
														*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[4] * xRest);
														pS++;
														*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[4] * xRest);
														pS++;
														*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[4] * xRest);
													}
													else
													{
														*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
															pS[4] * xRest * (1 - yRest) +
															pS[sStride] * (1 - xRest) * yRest +
															pS[sStride + 4] * xRest * yRest);
														pS++;
														*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
															pS[4] * xRest * (1 - yRest) +
															pS[sStride] * (1 - xRest) * yRest +
															pS[sStride + 4] * xRest * yRest);
														pS++;
														*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
															pS[4] * xRest * (1 - yRest) +
															pS[sStride] * (1 - xRest) * yRest +
															pS[sStride + 4] * xRest * yRest);
													}
												}

												pR++;
											}
											else
												pR += 4;

											xSource += xPixelLength;
											ySource += yRowJump;
										}
									}
								}
							}
							finally
							{
								if (sourceData != null)
									source.UnlockBits(sourceData);
								if (source != null)
									source.Dispose();
							}
						}
					}
					finally
					{
						if (resultData != null)
						{
							itEncoder.Write(stripHeight, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
							result.UnlockBits(resultData);
							resultData = null;
						}
						if (result != null)
						{
							result.Dispose();
							result = null;
						}
					}

/*#if DEBUG
					Console.WriteLine("Crop & Deskew, Go32bppQuality() strip processing time: " + DateTime.Now.Subtract(start).ToString());
#endif*/
					if (this.ProgressChanged != null)
						this.ProgressChanged((stripY + stripHeight) / (float)heightR);
				}
			}
		}
		#endregion

		#region Go24bppFast()
		private void Go24bppFast(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.CropDeskew.CdObject cdObject)
		{
			int			x, y;
			Rectangle	decoderRect = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
			int			widthR = itEncoder.Width;
			int			heightR = itEncoder.Height;

			Bitmap source = null;
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			double ulCornerX = 0;
			double ulCornerY = 0;

			//double yRowJump = cdObject.Skew;
			double xRowJump = (cdObject.Width * itDecoder.Width) / (double)widthR;
			double yRowJump = (Math.Tan(cdObject.Skew) * (cdObject.Width * itDecoder.Width)) / widthR;

			unsafe
			{
				for (int stripY = 0; stripY < heightR; stripY += 2048)
				{
/*#if DEBUG
					DateTime start = DateTime.Now;
#endif*/
					int stripHeight = Math.Min(heightR - stripY, 2048);

					try
					{
						result = new Bitmap(widthR, stripHeight, PixelFormat.Format24bppRgb);
						resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

						for (int stripX = 0; stripX < widthR; stripX += 2048)
						{
							int stripWidth = Math.Min(widthR - stripX, 2048);
							bool isClipInsideSource;
							Rectangle sourceRect = GetSourceRect(new Rectangle(stripX, stripY, stripWidth, stripHeight), cdObject, ref ulCornerX, ref ulCornerY, decoderRect, out isClipInsideSource);

							if (sourceRect.Width > 0 && sourceRect.Height > 0)
							{
								try
								{
									source = itDecoder.GetClip(sourceRect);
									sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

									int sStride = sourceData.Stride;
									int rStride = resultData.Stride;

									byte* pSource = (byte*)sourceData.Scan0.ToPointer();
									byte* pResult = (byte*)resultData.Scan0.ToPointer();

									int xTmp, yTmp;

									byte* pS;
									byte* pR;

									if (isClipInsideSource)
									{
										for (y = 0; y < stripHeight; y++)
										{
											double currentXOffset = ulCornerX - (y * yRowJump);
											double currentYOffset = (y * xRowJump) + ulCornerY;
											pR = pResult + y * rStride + stripX * 3;

											for (x = 0; x < stripWidth; x++)
											{
												//xTmp = Convert.ToInt32(currentXOffset + (x * xRowJump));
												//yTmp = Convert.ToInt32(currentYOffset + (yRowJump * x));
												//pS = pSource + yTmp * sStride + xTmp * 3;
												pS = pSource + (int)(currentYOffset + (yRowJump * x)) * sStride + (int)(currentXOffset + (x * xRowJump)) * 3;

												*(pR++) = *(pS++);
												*(pR++) = *(pS++);
												*(pR++) = *(pS);
											}
										}
									}
									else
									{
										for (y = 0; y < stripHeight; y++)
										{
											double currentXOffset = ulCornerX - (y * yRowJump);
											double currentYOffset = (y * xRowJump) + ulCornerY;
											pR = pResult + y * rStride + stripX * 3;

											for (x = 0; x < stripWidth; x++)
											{
												//xTmp = Convert.ToInt32(currentXOffset + (x * xRowJump));
												//yTmp = Convert.ToInt32(currentYOffset + (yRowJump * x));
												xTmp = (int)(currentXOffset + (x * xRowJump));
												yTmp = (int)(currentYOffset + (yRowJump * x));

												if (xTmp >= 0 && yTmp >= 0 && xTmp < sourceRect.Width && yTmp < sourceRect.Height)
												{
													pS = pSource + yTmp * sStride + xTmp * 3;

													*(pR++) = *(pS++);
													*(pR++) = *(pS++);
													*(pR++) = *(pS);
												}
												else
													pR += 3;
											}
										}
									}
								}
								finally
								{
									if (sourceData != null)
										source.UnlockBits(sourceData);
									if (source != null)
										source.Dispose();
								}
							}
						}
					}
					finally
					{
						if (resultData != null)
						{
							itEncoder.Write(stripHeight, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
							result.UnlockBits(resultData);
							resultData = null;
						}
						if (result != null)
						{
							result.Dispose();
							result = null;
						}
					}

					if (this.ProgressChanged != null)
						this.ProgressChanged((stripY + stripHeight) / (float)heightR);

/*#if DEBUG
					Console.WriteLine("Crop & Deskew, Go24bppFast() strip processing time: " + DateTime.Now.Subtract(start).ToString());
#endif*/
				}
			}
		}
		#endregion

		#region Go24bppQuality()
		private void Go24bppQuality(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.CropDeskew.CdObject cdObject)
		{
			int			x, y;
			Rectangle	decoderRect = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
			int			widthR = itEncoder.Width;
			int			heightR = itEncoder.Height;

			Bitmap source = null;
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			//double xRowJump = (cdObject.CornerLl.X - cdObject.CornerUl.X) / (double)cdObject.Height;
			//double yRowJump = (Math.Tan(cdObject.Skew));
			double xRowJump = (cdObject.Width * itDecoder.Width) / (double)widthR;
			double yRowJump = (Math.Tan(cdObject.Skew) * (cdObject.Width * itDecoder.Width)) / widthR;

			double ulCornerX = 0;
			double ulCornerY = 0;

			unsafe
			{
				for (int stripY = 0; stripY < heightR; stripY += 2048)
				{
/*#if DEBUG
					DateTime start = DateTime.Now;
#endif*/
					
					int stripHeight = Math.Min(heightR - stripY, 2048);

					try
					{
						result = new Bitmap(widthR, stripHeight, PixelFormat.Format24bppRgb);
						resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

						//int stripX = 0;
						for (int stripX = 0; stripX < widthR; stripX += 2048)
						{
							//int stripWidth = widthR;
							int stripWidth = Math.Min(widthR - stripX, 2048);
							bool isClipInsideSource;
							Rectangle sourceRect = GetSourceRect(new Rectangle(stripX, stripY, stripWidth, stripHeight), cdObject, ref ulCornerX, ref ulCornerY, decoderRect, out isClipInsideSource);

							if (sourceRect.Width > 0 && sourceRect.Height > 0)
							{
								try
								{
									source = itDecoder.GetClip(sourceRect);
									sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

									int sStride = sourceData.Stride;
									int rStride = resultData.Stride;

									byte* pSource = (byte*)sourceData.Scan0.ToPointer();
									byte* pResult = (byte*)resultData.Scan0.ToPointer();

									double xSource, ySource;
									double xRest, yRest;

									byte* pS;
									byte* pR;

									if (isClipInsideSource)
									{
										try
										{
											for (y = 0; y < stripHeight; y++)
											{
												double currentXOffset = ulCornerX - (y * yRowJump);
												double currentYOffset = (y * xRowJump) + ulCornerY;
												pR = pResult + y * rStride + stripX * 3;

												xSource = currentXOffset;
												ySource = currentYOffset;

												for (x = 0; x < stripWidth; x++)
												{
													xRest = xSource - (int)xSource;
													yRest = ySource - (int)ySource;

													if (xRest < 0.0001)
														xRest = 0;
													else if (xRest > 0.9999)
													{
														xRest = 0;
														xSource = (int)xSource + 1;
													}

													if (yRest < 0.0001)
														yRest = 0;
													else if (yRest > 0.9999)
													{
														yRest = 0;
														ySource = (int)ySource + 1;
													}

													pS = pSource + (int)ySource * sStride + (int)xSource * 3;

													if (xRest == 0)
													{
														if (yRest == 0)
														{
															*(pR++) = *(pS++);
															*(pR++) = *(pS++);
															*(pR++) = *(pS);
														}
														else
														{
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
														}
													}
													else
													{
														if (yRest == 0)
														{
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[3] * xRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[3] * xRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[3] * xRest);
														}
														else
														{
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[3] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 3] * xRest * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[3] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 3] * xRest * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[3] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 3] * xRest * yRest);
														}
													}

													xSource += xRowJump;
													ySource += yRowJump;
												}
											}
										}
										catch (Exception ex)
										{
											throw ex;
										}
									}
									else
									{
										for (y = 0; y < stripHeight; y++)
										{
											double currentXOffset = ulCornerX - (y * yRowJump);
											double currentYOffset = (y * xRowJump) + ulCornerY;
											pR = pResult + y * rStride + stripX * 3;

											xSource = currentXOffset;
											ySource = currentYOffset;

											for (x = 0; x < stripWidth; x++)
											{
												xRest = xSource - (int)xSource;
												yRest = ySource - (int)ySource;

												if (xRest < 0.0001)
													xRest = 0;
												else if (xRest > 0.9999)
												{
													xRest = 0;
													xSource = (int)xSource + 1;
												}

												if (yRest < 0.0001)
													yRest = 0;
												else if (yRest > 0.9999)
												{
													yRest = 0;
													ySource = (int)ySource + 1;
												}

												if (ySource >= 0 && xSource >= 0 && xSource < sourceRect.Width - 1 && ySource < sourceRect.Height - 1)
												{
													pS = pSource + (int)ySource * sStride + (int)xSource * 3;

													if (xRest == 0)
													{
														if (yRest == 0)
														{
															*(pR++) = *(pS++);
															*(pR++) = *(pS++);
															*(pR++) = *(pS);
														}
														else
														{
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
														}
													}
													else
													{
														if (yRest == 0)
														{
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[3] * xRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[3] * xRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[3] * xRest);
														}
														else
														{
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[3] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 3] * xRest * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[3] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 3] * xRest * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[3] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 3] * xRest * yRest);
														}
													}
												}
												else
													pR += 3;

												xSource += xRowJump;
												ySource += yRowJump;
											}
										}
									}
								}
								finally
								{
									if (sourceData != null)
										source.UnlockBits(sourceData);
									if (source != null)
										source.Dispose();
								}
							}
						}
					}
					finally
					{
						if (resultData != null)
						{
							itEncoder.Write(stripHeight, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
							result.UnlockBits(resultData);
							resultData = null;
						}
						if (result != null)
						{
							result.Dispose();
							result = null;
						}
					}

/*#if DEBUG
					Console.WriteLine("Crop & Deskew, Go24bppQuality() strip processing time: " + DateTime.Now.Subtract(start).ToString());
#endif*/
					if (this.ProgressChanged != null)
						this.ProgressChanged((stripY + stripHeight) / (float)heightR);
				}
			}
		}
		#endregion

		#region Go24bppQuality()
		/*private void Go24bppQuality(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.CropDeskew.CdObject cdObject)
		{
			int x, y;
			Rectangle decoderRect = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
			int widthR = itEncoder.Width;
			int heightR = itEncoder.Height;

			Bitmap source = null;
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			double yRowJump = cdObject.Skew;
			double xRowJump = (cdObject.Width * itDecoder.Width) / (double)widthR;

			double ulCornerX = 0;
			double ulCornerY = 0;

			unsafe
			{
				for (int stripY = 0; stripY < heightR; stripY += 2048)
				{
#if DEBUG
					DateTime start = DateTime.Now;
#endif

					int stripHeight = Math.Min(heightR - stripY, 2048);

					try
					{
						result = new Bitmap(widthR, stripHeight, PixelFormat.Format24bppRgb);
						resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

						for (int stripX = 0; stripX < widthR; stripX += 2048)
						{
							int stripWidth = Math.Min(widthR - stripX, 2048);
							bool isClipInsideSource;
							Rectangle sourceRect = GetSourceRect(new Rectangle(stripX, stripY, stripWidth, stripHeight), cdObject, ref ulCornerX, ref ulCornerY, decoderRect, out isClipInsideSource);

							if (sourceRect.Width > 0 && sourceRect.Height > 0)
							{
								try
								{
									source = itDecoder.GetClip(sourceRect);
									sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

									int sStride = sourceData.Stride;
									int rStride = resultData.Stride;

									byte* pSource = (byte*)sourceData.Scan0.ToPointer();
									byte* pResult = (byte*)resultData.Scan0.ToPointer();

									double xSource, ySource;
									double xRest, yRest;

									byte* pS;
									byte* pR;

									if (isClipInsideSource)
									{
										try
										{
											for (y = 0; y < stripHeight; y++)
											{
												double currentXOffset = ulCornerX - (y * yRowJump);
												double currentYOffset = (y * xRowJump) + ulCornerY;
												pR = pResult + y * rStride + stripX * 3;

												xSource = currentXOffset;
												ySource = currentYOffset;

												for (x = 0; x < stripWidth; x++)
												{
													xRest = xSource - (int)xSource;
													yRest = ySource - (int)ySource;

													if (xRest < 0.0001)
														xRest = 0;
													else if (xRest > 0.9999)
													{
														xRest = 0;
														xSource = (int)xSource + 1;
													}

													if (yRest < 0.0001)
														yRest = 0;
													else if (yRest > 0.9999)
													{
														yRest = 0;
														ySource = (int)ySource + 1;
													}

													pS = pSource + (int)ySource * sStride + (int)xSource * 3;

													if (xRest == 0)
													{
														if (yRest == 0)
														{
															*(pR++) = *(pS++);
															*(pR++) = *(pS++);
															*(pR++) = *(pS);
														}
														else
														{
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
														}
													}
													else
													{
														if (yRest == 0)
														{
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[3] * xRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[3] * xRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[3] * xRest);
														}
														else
														{
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[3] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 3] * xRest * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[3] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 3] * xRest * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[3] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 3] * xRest * yRest);
														}
													}

													xSource += xRowJump;
													ySource += yRowJump;
												}
											}
										}
										catch (Exception ex)
										{
											throw ex;
										}
									}
									else
									{
										for (y = 0; y < stripHeight; y++)
										{
											double currentXOffset = ulCornerX - (y * yRowJump);
											double currentYOffset = (y * xRowJump) + ulCornerY;
											pR = pResult + y * rStride + stripX * 3;

											xSource = currentXOffset;
											ySource = currentYOffset;

											for (x = 0; x < stripWidth; x++)
											{
												xRest = xSource - (int)xSource;
												yRest = ySource - (int)ySource;

												if (xRest < 0.0001)
													xRest = 0;
												else if (xRest > 0.9999)
												{
													xRest = 0;
													xSource = (int)xSource + 1;
												}

												if (yRest < 0.0001)
													yRest = 0;
												else if (yRest > 0.9999)
												{
													yRest = 0;
													ySource = (int)ySource + 1;
												}

												if (ySource >= 0 && xSource >= 0 && xSource < sourceRect.Width - 1 && ySource < sourceRect.Height - 1)
												{
													pS = pSource + (int)ySource * sStride + (int)xSource * 3;

													if (xRest == 0)
													{
														if (yRest == 0)
														{
															*(pR++) = *(pS++);
															*(pR++) = *(pS++);
															*(pR++) = *(pS);
														}
														else
														{
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
														}
													}
													else
													{
														if (yRest == 0)
														{
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[3] * xRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[3] * xRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[3] * xRest);
														}
														else
														{
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[3] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 3] * xRest * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[3] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 3] * xRest * yRest);
															pS++;
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[3] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 3] * xRest * yRest);
														}
													}
												}
												else
													pR += 3;

												xSource += xRowJump;
												ySource += yRowJump;
											}
										}
									}
								}
								finally
								{
									if (sourceData != null)
										source.UnlockBits(sourceData);
									if (source != null)
										source.Dispose();
								}
							}
						}
					}
					finally
					{
						if (resultData != null)
						{
							itEncoder.Write(stripHeight, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
							result.UnlockBits(resultData);
							resultData = null;
						}
						if (result != null)
						{
							result.Dispose();
							result = null;
						}
					}

#if DEBUG
					Console.WriteLine("Crop & Deskew, Go24bppQuality() strip processing time: " + DateTime.Now.Subtract(start).ToString());
#endif
					if (this.ProgressChanged != null)
						this.ProgressChanged((stripY + stripHeight) / (float)heightR);
				}
			}
		}*/
		#endregion

		#region Go8bppFast()
		private void Go8bppFast(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.CropDeskew.CdObject cdObject)
		{
			int x, y;
			Rectangle decoderRect = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
			int widthR = itEncoder.Width;
			int heightR = itEncoder.Height;

			Bitmap source = null;
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			double ulCornerX = 0;
			double ulCornerY = 0;

			//double yRowJump = cdObject.Skew;
			//double xRowJump = (cdObject.Width * itDecoder.Width) / (double)widthR;
			double xRowJump = (cdObject.Width * itDecoder.Width) / (double)widthR;
			double yRowJump = (Math.Tan(cdObject.Skew) * (cdObject.Width * itDecoder.Width)) / widthR;

			unsafe
			{
				for (int stripY = 0; stripY < heightR; stripY += 4096)
				{
/*#if DEBUG
					DateTime start = DateTime.Now;
#endif*/
					int stripHeight = Math.Min(heightR - stripY, 4096);

					try
					{
						result = new Bitmap(widthR, stripHeight, PixelFormat.Format8bppIndexed);
						resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

						for (int stripX = 0; stripX < widthR; stripX += 4096)
						{
							int stripWidth = Math.Min(widthR - stripX, 4096);
							bool isClipInsideSource;
							Rectangle sourceRect = GetSourceRect(new Rectangle(stripX, stripY, stripWidth, stripHeight), cdObject, ref ulCornerX, ref ulCornerY, decoderRect, out isClipInsideSource);

							if (sourceRect.Width > 0 && sourceRect.Height > 0)
							{
								try
								{
									source = itDecoder.GetClip(sourceRect);
									sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

									int sStride = sourceData.Stride;
									int rStride = resultData.Stride;

									byte* pSource = (byte*)sourceData.Scan0.ToPointer();
									byte* pResult = (byte*)resultData.Scan0.ToPointer();

									int xTmp, yTmp;

									byte* pS;
									byte* pR;

									if (isClipInsideSource)
									{
										for (y = 0; y < stripHeight; y++)
										{
											double currentXOffset = ulCornerX - (y * yRowJump);
											double currentYOffset = (y * xRowJump) + ulCornerY;
											pR = pResult + y * rStride + stripX;

											for (x = 0; x < stripWidth; x++)
											{
												//xTmp = Convert.ToInt32(currentXOffset + (x * xRowJump));
												//yTmp = Convert.ToInt32(currentYOffset + (yRowJump * x));
												//pS = pSource + yTmp * sStride + xTmp * 3;
												pS = pSource + (int)(currentYOffset + (yRowJump * x)) * sStride + (int)(currentXOffset + (x * xRowJump));

												*(pR++) = *(pS);
											}
										}
									}
									else
									{
										for (y = 0; y < stripHeight; y++)
										{
											double currentXOffset = ulCornerX - (y * yRowJump);
											double currentYOffset = (y * xRowJump) + ulCornerY;
											pR = pResult + y * rStride + stripX;

											for (x = 0; x < stripWidth; x++)
											{
												//xTmp = Convert.ToInt32(currentXOffset + (x * xRowJump));
												//yTmp = Convert.ToInt32(currentYOffset + (yRowJump * x));
												xTmp = (int)(currentXOffset + (x * xRowJump));
												yTmp = (int)(currentYOffset + (yRowJump * x));

												if (xTmp >= 0 && yTmp >= 0 && xTmp < sourceRect.Width && yTmp < sourceRect.Height)
												{
													pS = pSource + yTmp * sStride + xTmp;

													*(pR++) = *(pS);
												}
												else
													pR++;
											}
										}
									}
								}
								finally
								{
									if (sourceData != null)
										source.UnlockBits(sourceData);
									if (source != null)
										source.Dispose();
								}
							}
						}
					}
					finally
					{
						if (resultData != null)
						{
							itEncoder.Write(stripHeight, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
							result.UnlockBits(resultData);
							resultData = null;
						}
						if (result != null)
						{
							result.Dispose();
							result = null;
						}
					}

					if (this.ProgressChanged != null)
						this.ProgressChanged((stripY + stripHeight) / (float)heightR);

/*#if DEBUG
					Console.WriteLine("Crop & Deskew, Go8bppFast() strip processing time: " + DateTime.Now.Subtract(start).ToString());
#endif*/
				}
			}
		}
		#endregion

		#region Go8bppQuality()
		private void Go8bppQuality(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.CropDeskew.CdObject cdObject)
		{
			int x, y;
			Rectangle decoderRect = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
			int widthR = itEncoder.Width;
			int heightR = itEncoder.Height;

			Bitmap source = null;
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			//double yRowJump = cdObject.Skew;
			//double xRowJump = (cdObject.Width * itDecoder.Width) / (double)widthR;
			double xRowJump = (cdObject.Width * itDecoder.Width) / (double)widthR;
			double yRowJump = (Math.Tan(cdObject.Skew) * (cdObject.Width * itDecoder.Width)) / widthR;

			double ulCornerX = 0;
			double ulCornerY = 0;

			unsafe
			{
				for (int stripY = 0; stripY < heightR; stripY += 4096)
				{
/*#if DEBUG
					DateTime start = DateTime.Now;
#endif*/

					int stripHeight = Math.Min(heightR - stripY, 4096);

					try
					{
						result = new Bitmap(widthR, stripHeight, PixelFormat.Format8bppIndexed);
						resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

						for (int stripX = 0; stripX < widthR; stripX += 4096)
						{
							int stripWidth = Math.Min(widthR - stripX, 4096);
							bool isClipInsideSource;
							Rectangle sourceRect = GetSourceRect(new Rectangle(stripX, stripY, stripWidth, stripHeight), cdObject, ref ulCornerX, ref ulCornerY, decoderRect, out isClipInsideSource);

							if (sourceRect.Width > 0 && sourceRect.Height > 0)
							{
								try
								{
									source = itDecoder.GetClip(sourceRect);
									sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

									int sStride = sourceData.Stride;
									int rStride = resultData.Stride;

									byte* pSource = (byte*)sourceData.Scan0.ToPointer();
									byte* pResult = (byte*)resultData.Scan0.ToPointer();

									double xSource, ySource;
									double xRest, yRest;

									byte* pS;
									byte* pR;

									if (isClipInsideSource)
									{
										for (y = 0; y < stripHeight; y++)
										{
											double currentXOffset = ulCornerX - (y * yRowJump);
											double currentYOffset = (y * xRowJump) + ulCornerY;
											pR = pResult + y * rStride + stripX;

											xSource = currentXOffset;
											ySource = currentYOffset;

											for (x = 0; x < stripWidth; x++)
											{
												xRest = xSource - (int)xSource;
												yRest = ySource - (int)ySource;

												if (xRest < 0.0001)
													xRest = 0;
												else if (xRest > 0.9999)
												{
													xRest = 0;
													xSource = (int)xSource + 1;
												}

												if (yRest < 0.0001)
													yRest = 0;
												else if (yRest > 0.9999)
												{
													yRest = 0;
													ySource = (int)ySource + 1;
												}

												pS = pSource + (int)ySource * sStride + (int)xSource;

												if (xRest == 0)
												{
													if (yRest == 0)
														*(pR++) = *(pS);
													else
														*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
												}
												else
												{
													if (yRest == 0)
													{
														*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[1] * xRest);
													}
													else
													{
														*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
															pS[1] * xRest * (1 - yRest) +
															pS[sStride] * (1 - xRest) * yRest +
															pS[sStride + 1] * xRest * yRest);
													}
												}

												xSource += xRowJump;
												ySource += yRowJump;
											}
										}
									}
									else
									{
										for (y = 0; y < stripHeight; y++)
										{
											double currentXOffset = ulCornerX - (y * yRowJump);
											double currentYOffset = (y * xRowJump) + ulCornerY;
											pR = pResult + y * rStride + stripX;

											xSource = currentXOffset;
											ySource = currentYOffset;

											for (x = 0; x < stripWidth; x++)
											{
												xRest = xSource - (int)xSource;
												yRest = ySource - (int)ySource;

												if (xRest < 0.0001)
													xRest = 0;
												else if (xRest > 0.9999)
												{
													xRest = 0;
													xSource = (int)xSource + 1;
												}

												if (yRest < 0.0001)
													yRest = 0;
												else if (yRest > 0.9999)
												{
													yRest = 0;
													ySource = (int)ySource + 1;
												}

												if (ySource >= 0 && xSource >= 0 && xSource < sourceRect.Width - 1 && ySource < sourceRect.Height - 1)
												{
													pS = pSource + (int)ySource * sStride + (int)xSource;

													if (xRest == 0)
													{
														if (yRest == 0)
														{
															*(pR++) = *(pS);
														}
														else
														{
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) + pS[sStride] * yRest);
														}
													}
													else
													{
														if (yRest == 0)
														{
															*(pR++) = Convert.ToByte(*pS * (1 - xRest) + pS[1] * xRest);
														}
														else
														{
															*(pR++) = Convert.ToByte(*pS * (1 - yRest) * (1 - xRest) +
																pS[1] * xRest * (1 - yRest) +
																pS[sStride] * (1 - xRest) * yRest +
																pS[sStride + 1] * xRest * yRest);
														}
													}
												}
												else
													pR ++;

												xSource += xRowJump;
												ySource += yRowJump;
											}
										}
									}
								}
								finally
								{
									if (sourceData != null)
										source.UnlockBits(sourceData);
									if (source != null)
										source.Dispose();
								}
							}
						}
					}
					finally
					{
						if (resultData != null)
						{
							itEncoder.Write(stripHeight, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
							result.UnlockBits(resultData);
							resultData = null;
						}
						if (result != null)
						{
							result.Dispose();
							result = null;
						}
					}

/*#if DEBUG
					Console.WriteLine("Crop & Deskew, Go8bppQuality() strip processing time: " + DateTime.Now.Subtract(start).ToString());
#endif*/
					if (this.ProgressChanged != null)
						this.ProgressChanged((stripY + stripHeight) / (float)heightR);
				}
			}
		}
		#endregion

		#region Go1bpp()
		private void Go1bpp(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.CropDeskew.CdObject cdObject)
		{
			int x, y;
			Rectangle decoderRect = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
			int widthR = itEncoder.Width;
			int heightR = itEncoder.Height;

			Bitmap source = null;
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			double ulCornerX = 0;
			double ulCornerY = 0;

			//double yRowJump = cdObject.Skew;
			//double xRowJump = (cdObject.Width * itDecoder.Width) / (double)widthR;
			double xRowJump = (cdObject.Width * itDecoder.Width) / (double)widthR;
			double yRowJump = (Math.Tan(cdObject.Skew) * (cdObject.Width * itDecoder.Width)) / widthR;

			unsafe
			{
				for (int stripY = 0; stripY < heightR; stripY += 4096)
				{
/*#if DEBUG
					DateTime start = DateTime.Now;
#endif*/
					int stripHeight = Math.Min(heightR - stripY, 4096);

					try
					{
						result = new Bitmap(widthR, stripHeight, PixelFormat.Format1bppIndexed);
						resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

						for (int stripX = 0; stripX < widthR; stripX += 4096)
						{
							int stripWidth = Math.Min(widthR - stripX, 4096);
							bool isClipInsideSource;
							Rectangle sourceRect = GetSourceRect(new Rectangle(stripX, stripY, stripWidth, stripHeight), cdObject, ref ulCornerX, ref ulCornerY, decoderRect, out isClipInsideSource);

							if (sourceRect.Width > 0 && sourceRect.Height > 0)
							{
								try
								{
									source = itDecoder.GetClip(sourceRect);
									sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

									int sStride = sourceData.Stride;
									int rStride = resultData.Stride;

									byte* pSource = (byte*)sourceData.Scan0.ToPointer();
									byte* pResult = (byte*)resultData.Scan0.ToPointer();

									int xTmp, yTmp;

									for (y = 0; y < stripHeight; y++)
									{
										double currentXOffset = ulCornerX - (y * yRowJump);
										double currentYOffset = (y * xRowJump) + ulCornerY;

										for (x = 0; x < stripWidth; x++)
										{
											xTmp =/*Convert.ToInt32*/(int)(currentXOffset + (x * xRowJump));
											yTmp =/*Convert.ToInt32*/(int)(currentYOffset + (yRowJump * x));

											if (xTmp >= 0 && yTmp >= 0 && xTmp < sourceRect.Width && yTmp < sourceRect.Height)
											{
												if ((pSource[(int)yTmp * sStride + (int)xTmp / 8] & (0x80 >> ((int)xTmp & 0x07))) > 0)
													pResult[y * rStride + (stripX + x) / 8] |= (byte)(0x80 >> (x & 0x07));
											}
										}
									}
								}
								finally
								{
									if (sourceData != null)
										source.UnlockBits(sourceData);
									if (source != null)
										source.Dispose();
								}
							}
						}
					}
					finally
					{
						if (resultData != null)
						{
							itEncoder.Write(stripHeight, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
							result.UnlockBits(resultData);
							resultData = null;
						}
						if (result != null)
						{
							result.Dispose();
							result = null;
						}
					}

					if (this.ProgressChanged != null)
						this.ProgressChanged((stripY + stripHeight) / (float)heightR);

/*#if DEBUG
					Console.WriteLine("Crop & Deskew, Go8bppFast() strip processing time: " + DateTime.Now.Subtract(start).ToString());
#endif*/
				}
			}
		}
		#endregion

		#region IsEntireClipInsideSource()
		private bool IsEntireClipInsideSource(Size size, ImageProcessing.CropDeskew.CdObject cdObject)
		{
			if (cdObject.CornerUl.X < 0 || cdObject.CornerUl.Y < 0)
				return false;

			if (cdObject.CornerLl.X < 0 || cdObject.CornerLl.Y > 1)
				return false;

			if (cdObject.CornerUr.X > 1 || cdObject.CornerUr.Y < 0)
				return false;

			if (cdObject.CornerLr.X > 1 || cdObject.CornerLr.Y > 1)
				return false;

			return true;
		}
		#endregion

		#region RemoveGhostLines
		private unsafe void RemoveGhostLines(Bitmap bitmap, List<int> ghostLines)
		{
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
				byte* pCurrent;
				byte tmp;

				foreach (int ghostLine in ghostLines)
				{
					if (ghostLine >= 0 && ghostLine < bitmap.Width)
					{
						pCurrent = pSource + ghostLine / 8;

						for (int y = 0; y < bitmapData.Height; y++)
						{
							if (*pCurrent > 0)
							{
								tmp = (byte)(0x80 >> (ghostLine & 0x07));
								*pCurrent = (byte)(*pCurrent & (~tmp));
							}

							pCurrent += bitmapData.Stride;
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region GetSourceRect()
		private Rectangle GetSourceRect(Rectangle resultRect, ImageProcessing.CropDeskew.CdObject cdObject, ref double ulCornerX, ref double ulCornerY, Rectangle sourceRect, out bool clipInsideSource)
		{
			PointF center = new PointF(0, 0);

			PointF pUL = ImageProcessing.BigImages.Rotation.RotatePoint(new Point(resultRect.X, resultRect.Y), center, cdObject.Skew);
			PointF pUR = ImageProcessing.BigImages.Rotation.RotatePoint(new Point(resultRect.Right, resultRect.Y), center, cdObject.Skew);
			PointF pLL = ImageProcessing.BigImages.Rotation.RotatePoint(new Point(resultRect.X, resultRect.Bottom), center, cdObject.Skew);
			PointF pLR = ImageProcessing.BigImages.Rotation.RotatePoint(new Point(resultRect.Right, resultRect.Bottom), center, cdObject.Skew);

			pUL.X += (float)(cdObject.CornerUl.X * sourceRect.Width);
			pUL.Y += (float)(cdObject.CornerUl.Y * sourceRect.Height);
			pUR.X += (float)(cdObject.CornerUl.X * sourceRect.Width);
			pUR.Y += (float)(cdObject.CornerUl.Y * sourceRect.Height);
			pLL.X += (float)(cdObject.CornerUl.X * sourceRect.Width);
			pLL.Y += (float)(cdObject.CornerUl.Y * sourceRect.Height);
			pLR.X += (float)(cdObject.CornerUl.X * sourceRect.Width);
			pLR.Y += (float)(cdObject.CornerUl.Y * sourceRect.Height);

			double x = Math.Min(Math.Min(pUL.X, pUR.X), Math.Min(pLL.X, pLR.X));
			double y = Math.Min(Math.Min(pUL.Y, pUR.Y), Math.Min(pLL.Y, pLR.Y));
			double right = Math.Max(Math.Max(pUL.X, pUR.X), Math.Max(pLL.X, pLR.X));
			double bottom = Math.Max(Math.Max(pUL.Y, pUR.Y), Math.Max(pLL.Y, pLR.Y));

			Rectangle rect = Rectangle.FromLTRB(Convert.ToInt32(x), Convert.ToInt32(y), Convert.ToInt32(right + (right - x) * 0.2), Convert.ToInt32(bottom + (bottom - y) * 0.2));
			rect.Inflate(1, 1);

			if (Rectangle.Union(sourceRect, rect) == sourceRect)
			{
				clipInsideSource = true;
			}
			else
			{
				clipInsideSource = false;
				rect.Intersect(sourceRect);
			}

			ulCornerX = pUL.X - rect.X;
			ulCornerY = pUL.Y - rect.Y;

			return rect;
		}
		#endregion

		#endregion

	}
}
