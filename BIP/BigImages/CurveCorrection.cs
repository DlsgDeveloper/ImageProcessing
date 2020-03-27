using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using ImageProcessing.PageObjects;
using BIP.Geometry;



namespace ImageProcessing.BigImages
{
	public class CurveCorrection
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public CurveCorrection()
		{
		}
		#endregion


		#region class WeightedCurve
		/// <summary>
		/// Weighted curve is 2-dimensional curve made out of one or more curves merged together. 
		/// Instead of storing curve values or bamps, it stores y-pixel difference for each valid 
		/// pixel in the curve. Valid pixel is pixel that at least 1 curve adding to be merged contains.
		/// Position of the curve is unknown.
		/// Curves can have different weights - importances. In itPage curve search, objects closer 
		/// to the top or bottom edges are more important that the ones close to the middle of the book. 
		/// </summary>
		class WeightedCurve
		{
			WeightedCurveItem[] curve;

			#region constructor
			public WeightedCurve(int dimension)
			{
				curve = new WeightedCurveItem[dimension];
			}
			#endregion

			#region class WeightedCurveItem
			class WeightedCurveItem
			{
				double weight = 0;
				double value = 0;

				public WeightedCurveItem(double weight, double value)
				{
					this.value = value * weight;
					this.weight = weight;
				}

				public double Value { get { return value / weight; } }
				public double Weight { get { return weight; } }

				public void Add(double weight, double value)
				{
					this.value += value * weight;
					this.weight += weight;
				}
			}
			#endregion

			//PUBLIC PROPERTIES			
			#region public properties

			#region CurveExists
			/// <summary>
			/// Returns true if distance between first and last valid curve item is bigger than 0
			/// </summary>
			public bool CurveExists { get { return (ValidTo > ValidFrom); } }
			#endregion

			#region ValidFrom
			/// <summary>
			/// Returns first valid curve index
			/// </summary>
			public int ValidFrom
			{
				get
				{
					for (int i = 0; i < this.curve.Length; i++)
						if (this.curve[i] != null)
							return i;

					return int.MaxValue;
				}
			}
			#endregion

			#region ValidTo
			/// <summary>
			/// Returns last valid curve index
			/// </summary>
			public int ValidTo
			{
				get
				{
					for (int i = this.curve.Length - 1; i >= 0; i--)
						if (this.curve[i] != null)
							return i;

					return int.MinValue;
				}
			}
			#endregion

			#region Confidence
			public float Confidence
			{
				get
				{
					double confidence = 0;
					
					for (int i = 0; i < curve.Length; i++)
						if (this.curve[i] != null)
							confidence += this.curve[i].Weight;

					return (float) Math.Max(0, Math.Min(1, confidence / (double)curve.Length));
				}
			}
			#endregion

			#endregion

			//PUBLIC METHODS
			#region public methods

			#region AddCurve()
			/// <summary>
			/// Inserts or merges curve array with existing curve
			/// </summary>
			/// <param name="x">First index of curveArray</param>
			/// <param name="weight">Importance of curve</param>
			/// <param name="curveArray">Curve array doesn't have to be zero-based since only pixel differences are stored</param>
			public void AddCurve(int x, double weight, double[] curveArray)
			{
				for (int i = 1; i < curveArray.Length; i++)
					if (x + i >= 0 && x + i < this.curve.Length)
					{
						if (this.curve[x + i] == null)
							this.curve[x + i] = new WeightedCurveItem(weight, curveArray[i] - curveArray[i - 1]);
						else
							this.curve[x + i].Add(weight, curveArray[i] - curveArray[i - 1]);
					}

				if (x >= 0 && x < this.curve.Length)
				{
					if (this.curve[x] == null)
						this.curve[x] = new WeightedCurveItem(weight, curveArray[1] - curveArray[0]);
					else
						this.curve[x].Add(weight, curveArray[1] - curveArray[0]);
				}
			}
			#endregion

			#region GetCurve()
			/// <summary>
			/// Returns y-zero-based curve points. Points are in interval betveen x-axis valid points 
			/// only. If first valid pixel is at X=0, first point's X component is 0.
			/// Number of points is equal or less than numberOfPoints, not 2 points have the same X. 
			/// Points are sorted based on X axis.
			/// </summary>
			/// <param name="numberOfPoints"></param>
			/// <returns></returns>
			public Point[] GetCurve(int numberOfPoints)
			{
				if (CurveExists)
				{
					int from = ValidFrom;
					int to = ValidTo;
					double interval = (to - from) / (numberOfPoints - 1.0);
					List<Point> points = new List<Point>();
					double[] curveArray = GetCurve();

					points.Add(new Point(from, Convert.ToInt32(curveArray[from])));
					for (int i = 1; i < numberOfPoints - 1; i++)
					{
						int closestValidIndex = GetClosestValidIndex(Convert.ToInt32(from + i * interval));
						points.Add(new Point(closestValidIndex, Convert.ToInt32(curveArray[closestValidIndex])));
					}

					points.Add(new Point(to, Convert.ToInt32(curveArray[to])));

					for (int i = points.Count - 1; i > 0; i--)
						if (points[i].X == points[i - 1].X)
							points.RemoveAt(i);

					return points.ToArray();
				}
				else
				{
					List<Point> points = new List<Point>();

					points.Add(new Point(0, 0));
					points.Add(new Point(this.curve.Length, 0));

					return points.ToArray();
				}
			}
			#endregion

			#endregion

			//PRIVATE METHODS
			#region private methods

			#region GetCurve()
			private double[] GetCurve()
			{
				double[] curveArray = new double[this.curve.Length];
				double value = 0;

				for (int i = 1; i < this.curve.Length; i++)
					if (this.curve[i] != null)
					{
						value += this.curve[i].Value;
						curveArray[i] = value;
					}

				//shift array so that smallest y is 0
				double smallest = curveArray[0];
				for (int i = 1; i < this.curve.Length; i++)
					if (smallest > curveArray[i])
						smallest = curveArray[i];

				for (int i = 0; i < this.curve.Length; i++)
					curveArray[i] -= smallest;

				return curveArray;
			}
			
			/*private double[] GetCurve()
			{
				double[] curveArray = new double[this.curve.Length];

				for (int i = 0; i < this.curve.Length; i++)
					if (this.curve[i] != null)
						curveArray[i] = this.curve[i].Value;

				return curveArray;
			}*/
			#endregion

			#region GetClosestValidIndex()
			public int GetClosestValidIndex(int index)
			{
				if (CurveExists)
				{
					if (index <= ValidFrom)
						return ValidFrom;
					else if (index >= ValidTo)
						return ValidTo;
					else
					{
						if (curve[index] != null)
							return index;

						else
						{
							for (int i = 1; i < Math.Max(ValidTo - index, index - ValidFrom); i++)
							{
								if (this.curve[index - i] != null)
									return index - i;
								if (this.curve[index + i] != null)
									return index + i;
							}
						}

						return (ValidFrom + ValidTo) / 2;
					}
				}
				else
					return 0;
			}
			#endregion

			#endregion

		}
		#endregion

		
		//PUBLIC METHODS
		#region public methods

		#region FindCuring()
		public static void FindCurving(ImageProcessing.IpSettings.ItPage itPage)
		{
			float		confidenceT = 0, confidenceB = 0;
			ImageProcessing.IpSettings.Curve		curveT, curveB;

			if (TopCurveShouldExist(itPage))
				curveT = GetTopCurve(itPage, out confidenceT);
			else
			{
				curveT = new ImageProcessing.IpSettings.Curve(itPage, true);
				confidenceT = 1.0F;
			}

			if (BottomCurveShouldExist(itPage))
				curveB = GetBottomCurve(itPage, out confidenceB);
			else
			{
				curveB = new ImageProcessing.IpSettings.Curve(itPage, false);
				confidenceB = 1.0F;
			}

			itPage.Bookfolding.SetCurves(curveT, curveB, Math.Min(confidenceT, confidenceB));
		}
		#endregion

		#region Execute()
		public void Execute(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat,
			ImageProcessing.IpSettings.ItPage itPage)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			
			ImageProcessing.BigImages.ItEncoder itEncoder = new ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat,
				Convert.ToInt32(itPage.ClipRect.Width * itDecoder.Width), Convert.ToInt32(itPage.ClipRect.Height * itDecoder.Height),
				itDecoder.DpiX, itDecoder.DpiY);

			try
			{
				switch (itDecoder.PixelFormat)
				{
					case PixelFormat.Format32bppRgb:
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format8bppIndexed:
					case PixelFormat.Format1bppIndexed:
						Stretch(itDecoder, itEncoder, itPage);
						break;					
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			catch (Exception ex)
			{
				try { itEncoder.Dispose(); }
				catch { }
					
				itEncoder = null;

				try
				{
					if (File.Exists(destPath))
						File.Delete(destPath);
				}
				catch { }
				
				throw new Exception("Big Images, CurveCorrection, Execute(): " + ex.Message);
			}
			finally
			{
				if (itEncoder != null)
					itEncoder.Dispose();
#if DEBUG
				Console.WriteLine(string.Format("CurveCorrection: {0}", DateTime.Now.Subtract(start).ToString()));
#endif
			}
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region Stretch()
		private unsafe void Stretch(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.IpSettings.ItPage itPage)
		{
			Bitmap			source = null;
			Bitmap			result = null;
			BitmapData		sourceData = null;
			BitmapData		resultData = null;
			
			int sourceW = itDecoder.Width;
			int sourceH = itDecoder.Height;
			int resultW = itEncoder.Width;
			int resultH = itEncoder.Height;

			Rectangle resultRectImageCoords = new Rectangle((int)(itPage.ClipRect.X * sourceW), (int)(itPage.ClipRect.Y * itDecoder.Height), resultW, resultH);

			int x;
			int y;

			double[]	arrayT;
			double[]	arrayB;
			int			imageLensCenter = (int)(Math.Max(0, Math.Min(1, itPage.GlobalOpticsCenter)) * sourceH);

			GetCurves(itPage, out arrayT, out arrayB, itDecoder.Size);

			for (int stripY = 0; stripY < resultH; stripY += 2048)
			{
#if DEBUG
				DateTime start = DateTime.Now;
#endif

				int stripHeight = Math.Min(resultH - stripY, 2048);

				try
				{
					result = new Bitmap(resultW, stripHeight, itDecoder.PixelFormat);
					resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					Rectangle resultClipImageCoords = new Rectangle(resultRectImageCoords.X, resultRectImageCoords.Y + stripY, resultRectImageCoords.Width, resultRectImageCoords.Y + stripHeight);
					Rectangle sourceClipImageCoords = GetSourceRect(resultClipImageCoords, arrayT, arrayB, imageLensCenter, itDecoder.Size, resultRectImageCoords);

					source = itDecoder.GetClip(sourceClipImageCoords);
					sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

					byte* pS;
					byte* pR;

					int sStride = sourceData.Stride;
					int rStride = resultData.Stride;
					double yJump = 1.0;

					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					#region 32 bpp
					if (source.PixelFormat == PixelFormat.Format32bppArgb || source.PixelFormat == PixelFormat.Format32bppRgb)
					{
						int pixelBytes = (source.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

						if (resultClipImageCoords.Y <= imageLensCenter)
						{
							int yBottomClipCoords = Math.Min(stripHeight - 1, imageLensCenter - resultClipImageCoords.Y);
							int yBottomImageCoords = yBottomClipCoords + resultClipImageCoords.Top;

							for (x = 0; x < resultW; x++)
							{
								if (imageLensCenter - resultRectImageCoords.Top != 0)
									yJump = (imageLensCenter - resultRectImageCoords.Top - arrayT[x]) / (double)(imageLensCenter - resultRectImageCoords.Top);
								else
									yJump = 1;

								double sourceY = imageLensCenter - (imageLensCenter - yBottomImageCoords) * yJump - sourceClipImageCoords.Top;

								pR = pResult + (yBottomClipCoords * rStride) + (x * pixelBytes);

								for (y = yBottomClipCoords; y >= 0; y--)
								{
									if (sourceY >= 1)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + (x * pixelBytes);
										double yRest = sourceY - (int)sourceY;
										pR[0] = (byte)((yRest * pS[0]) + ((1 - yRest) * pS[-sStride]));
										pR[1] = (byte)((yRest * pS[1]) + ((1 - yRest) * pS[1 - sStride]));
										pR[2] = (byte)((yRest * pS[2]) + ((1 - yRest) * pS[2 - sStride]));
										pR[3] = 255;
									}
									else if (sourceY >= 0)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + (x * pixelBytes);

										pR[0] = pS[0];
										pR[1] = pS[1];
										pR[2] = pS[2];
										pR[3] = 255;
									}
									else
										break;

									sourceY -= yJump;
									pR -= rStride;
								}
							}
						}
						if (resultClipImageCoords.Bottom > imageLensCenter)
						{
							int yTopClipCoords;

							if (resultClipImageCoords.Top < imageLensCenter)
								yTopClipCoords = Math.Max(0, imageLensCenter - resultClipImageCoords.Top);
							else
								yTopClipCoords = 0;

							int yTopImageCoords = yTopClipCoords + resultClipImageCoords.Top;

							for (x = 0; x < resultW; x++)
							{
								if (resultRectImageCoords.Bottom - imageLensCenter != 0)
									yJump = (resultRectImageCoords.Bottom - imageLensCenter - arrayB[x]) / (double)(resultRectImageCoords.Bottom - imageLensCenter);
								else
									yJump = 1;

								double sourceY;

								if (yTopImageCoords > imageLensCenter)
									sourceY = imageLensCenter + ((yTopImageCoords - imageLensCenter) * yJump) - sourceClipImageCoords.Top;
								else
									sourceY = imageLensCenter - sourceClipImageCoords.Top;

								pR = pResult + (yTopClipCoords * rStride) + (x * pixelBytes);

								for (y = yTopClipCoords; y < stripHeight; y++)
								{
									if (sourceY < sourceClipImageCoords.Height - 1)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + (x * pixelBytes);
										double yRest = sourceY - (int)sourceY;
										pR[0] = (byte)(((1 - yRest) * pS[0]) + (yRest * pS[0 + sStride]));
										pR[1] = (byte)(((1 - yRest) * pS[1]) + (yRest * pS[1 + sStride]));
										pR[2] = (byte)(((1 - yRest) * pS[2]) + (yRest * pS[2 + sStride]));
										pR[3] = 255;
									}
									else if (sourceY < sourceClipImageCoords.Height)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + (x * pixelBytes);

										pR[0] = pS[0];
										pR[1] = pS[1];
										pR[2] = pS[2];
										pR[3] = 255;
									}
									else
										break;

									sourceY += yJump;
									pR += rStride;
								}
							}
						}
					}
					#endregion

					#region 24 bpp
					if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						int pixelBytes = (source.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

						if (resultClipImageCoords.Y <= imageLensCenter)
						{
							int yBottomClipCoords = Math.Min(stripHeight - 1, imageLensCenter - resultClipImageCoords.Y);
							int yBottomImageCoords = yBottomClipCoords + resultClipImageCoords.Top;

							for (x = 0; x < resultW; x++)
							{
								if (imageLensCenter - resultRectImageCoords.Top != 0)
									yJump = (imageLensCenter - resultRectImageCoords.Top - arrayT[x]) / (double)(imageLensCenter - resultRectImageCoords.Top);
								else
									yJump = 1;

								double sourceY = imageLensCenter - (imageLensCenter - yBottomImageCoords) * yJump - sourceClipImageCoords.Top;

								pR = pResult + (yBottomClipCoords * rStride) + (x * pixelBytes);

								for (y = yBottomClipCoords; y >= 0; y--)
								{
									if (sourceY >= 1)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + (x * pixelBytes);
										double yRest = sourceY - (int)sourceY;
										pR[0] = (byte)((yRest * pS[0]) + ((1 - yRest) * pS[-sStride]));
										pR[1] = (byte)((yRest * pS[1]) + ((1 - yRest) * pS[1 - sStride]));
										pR[2] = (byte)((yRest * pS[2]) + ((1 - yRest) * pS[2 - sStride]));
									}
									else if (sourceY >= 0)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + (x * pixelBytes);

										pR[0] = pS[0];
										pR[1] = pS[1];
										pR[2] = pS[2];
									}
									else
										break;

									sourceY -= yJump;
									pR -= rStride;
								}
							}
						}
						if (resultClipImageCoords.Bottom > imageLensCenter)
						{
							int yTopClipCoords;

							if (resultClipImageCoords.Top < imageLensCenter)
								yTopClipCoords = Math.Max(0, imageLensCenter - resultClipImageCoords.Top);
							else
								yTopClipCoords = 0;

							int yTopImageCoords = yTopClipCoords + resultClipImageCoords.Top;

							for (x = 0; x < resultW; x++)
							{
								if (resultRectImageCoords.Bottom - imageLensCenter != 0)
									yJump = (resultRectImageCoords.Bottom - imageLensCenter - arrayB[x]) / (double)(resultRectImageCoords.Bottom - imageLensCenter);
								else
									yJump = 1;

								double sourceY;

								if (yTopImageCoords > imageLensCenter)
									sourceY = imageLensCenter + ((yTopImageCoords - imageLensCenter) * yJump) - sourceClipImageCoords.Top;
								else
									sourceY = imageLensCenter - sourceClipImageCoords.Top;

								pR = pResult + (yTopClipCoords * rStride) + (x * pixelBytes);

								for (y = yTopClipCoords; y < stripHeight; y++)
								{
									if (sourceY < sourceClipImageCoords.Height - 1)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + (x * pixelBytes);
										double yRest = sourceY - (int)sourceY;
										pR[0] = (byte)(((1 - yRest) * pS[0]) + (yRest * pS[0 + sStride]));
										pR[1] = (byte)(((1 - yRest) * pS[1]) + (yRest * pS[1 + sStride]));
										pR[2] = (byte)(((1 - yRest) * pS[2]) + (yRest * pS[2 + sStride]));
									}
									else if (sourceY < sourceClipImageCoords.Height)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + (x * pixelBytes);

										pR[0] = pS[0];
										pR[1] = pS[1];
										pR[2] = pS[2];
									}
									else
										break;

									sourceY += yJump;
									pR += rStride;
								}
							}
						}
					}
					#endregion

					#region 8 bpp
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						if (resultClipImageCoords.Y <= imageLensCenter)
						{
							int yBottomClipCoords = Math.Min(stripHeight - 1, imageLensCenter - resultClipImageCoords.Y);
							int yBottomImageCoords = yBottomClipCoords + resultClipImageCoords.Top;

							for (x = 0; x < resultW; x++)
							{
								if (imageLensCenter - resultRectImageCoords.Top != 0)
									yJump = (imageLensCenter - resultRectImageCoords.Top - arrayT[x]) / (double)(imageLensCenter - resultRectImageCoords.Top);
								else
									yJump = 1;

								double sourceY = imageLensCenter - (imageLensCenter - yBottomImageCoords) * yJump - sourceClipImageCoords.Top;

								pR = pResult + (yBottomClipCoords * rStride) + x;

								for (y = yBottomClipCoords; y >= 0; y--)
								{
									if (sourceY >= 1)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + x;
										double yRest = sourceY - (int)sourceY;
										pR[0] = (byte)((yRest * pS[0]) + ((1 - yRest) * pS[-sStride]));
									}
									else if (sourceY >= 0)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + x;

										pR[0] = pS[0];
									}
									else
										break;

									sourceY -= yJump;
									pR -= rStride;
								}
							}
						}
						if (resultClipImageCoords.Bottom > imageLensCenter)
						{
							int yTopClipCoords;

							if (resultClipImageCoords.Top < imageLensCenter)
								yTopClipCoords = Math.Max(0, imageLensCenter - resultClipImageCoords.Top);
							else
								yTopClipCoords = 0;

							int yTopImageCoords = yTopClipCoords + resultClipImageCoords.Top;

							for (x = 0; x < resultW; x++)
							{
								if (resultRectImageCoords.Bottom - imageLensCenter != 0)
									yJump = (resultRectImageCoords.Bottom - imageLensCenter - arrayB[x]) / (double)(resultRectImageCoords.Bottom - imageLensCenter);
								else
									yJump = 1;

								double sourceY;

								if (yTopImageCoords > imageLensCenter)
									sourceY = imageLensCenter + ((yTopImageCoords - imageLensCenter) * yJump) - sourceClipImageCoords.Top;
								else
									sourceY = imageLensCenter - sourceClipImageCoords.Top;

								pR = pResult + (yTopClipCoords * rStride) + x;

								for (y = yTopClipCoords; y < stripHeight; y++)
								{
									if (sourceY < sourceClipImageCoords.Height - 1)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + x;
										double yRest = sourceY - (int)sourceY;
										pR[0] = (byte)(((1 - yRest) * pS[0]) + (yRest * pS[0 + sStride]));
									}
									else if (sourceY < sourceClipImageCoords.Height)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + x;

										pR[0] = pS[0];
									}
									else
										break;

									sourceY += yJump;
									pR += rStride;
								}
							}
						}
					}
					#endregion

					#region 1 bpp
					else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						if (resultClipImageCoords.Y <= imageLensCenter)
						{
							int yBottomClipCoords = Math.Min(stripHeight - 1, imageLensCenter - resultClipImageCoords.Y);
							int yBottomImageCoords = yBottomClipCoords + resultClipImageCoords.Top;

							for (x = 0; x < resultW; x++)
							{
								if (imageLensCenter - resultRectImageCoords.Top != 0)
									yJump = (imageLensCenter - resultRectImageCoords.Top - arrayT[x]) / (double)(imageLensCenter - resultRectImageCoords.Top);
								else
									yJump = 1;

								double sourceY = imageLensCenter - (imageLensCenter - yBottomImageCoords) * yJump - sourceClipImageCoords.Top;

								pR = pResult + (yBottomClipCoords * rStride) + (x / 8);

								for (y = yBottomClipCoords; y >= 0; y--)
								{
									if (sourceY >= 0)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + (x / 8);

										if ((pS[0] & (0x80 >> (x & 0x07))) > 0)
											pR[0] |= (byte)(0x80 >> (x & 0x7));
									}
									else
										break;

									sourceY -= yJump;
									pR -= rStride;
								}
							}
						}
						if (resultClipImageCoords.Bottom > imageLensCenter)
						{
							int yTopClipCoords;

							if (resultClipImageCoords.Top < imageLensCenter)
								yTopClipCoords = Math.Max(0, imageLensCenter - resultClipImageCoords.Top);
							else
								yTopClipCoords = 0;

							int yTopImageCoords = yTopClipCoords + resultClipImageCoords.Top;

							for (x = 0; x < resultW; x++)
							{
								if (resultRectImageCoords.Bottom - imageLensCenter != 0)
									yJump = (resultRectImageCoords.Bottom - imageLensCenter - arrayB[x]) / (double)(resultRectImageCoords.Bottom - imageLensCenter);
								else
									yJump = 1;

								double sourceY;

								if (yTopImageCoords > imageLensCenter)
									sourceY = imageLensCenter + ((yTopImageCoords - imageLensCenter) * yJump) - sourceClipImageCoords.Top;
								else
									sourceY = imageLensCenter - sourceClipImageCoords.Top;

								pR = pResult + (yTopClipCoords * rStride) + (x / 8);

								for (y = yTopClipCoords; y < stripHeight; y++)
								{
									if (sourceY < sourceClipImageCoords.Height)
									{
										pS = (pSource + ((int)(sourceY) * sStride)) + (x / 8);

										if ((pS[0] & (0x80 >> (x & 0x07))) > 0)
											pR[0] |= (byte)(0x80 >> (x & 0x7));
									}
									else
										break;

									sourceY += yJump;
									pR += rStride;
								}
							}
						}
					}
					#endregion

				}
				finally
				{
					if ((source != null) && (sourceData != null))
						source.UnlockBits(sourceData);

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
				Console.WriteLine("Curve Correction, Stretch() strip processing time: " + DateTime.Now.Subtract(start).ToString());
#endif
				if (this.ProgressChanged != null)
					this.ProgressChanged((stripY + stripHeight) / (float)resultH);
			}
		}
		#endregion
	
		#region GetCurves()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="itPage"></param>
		/// <param name="arrayT">Values are positive, smallest number is 0</param>
		/// <param name="arrayB">Values are negative, biggest number is 0</param>
		/// <param name="sourceSize"></param>
		/// <param name="resultSize"></param>
		private static void GetCurves(ImageProcessing.IpSettings.ItPage itPage, out double[] arrayT, out double[] arrayB, Size sourceSize)
		{
			int x;
			int lensCenter = (int)(itPage.GlobalOpticsCenter * sourceSize.Height);
			Rectangle imageRect = new Rectangle(Convert.ToInt32(itPage.Clip.RectangleNotSkewed.X * sourceSize.Width), Convert.ToInt32(itPage.Clip.RectangleNotSkewed.Y * sourceSize.Height),
				Convert.ToInt32(itPage.Clip.RectangleNotSkewed.Width * sourceSize.Width), Convert.ToInt32(itPage.Clip.RectangleNotSkewed.Height * sourceSize.Height));

			arrayT = itPage.Bookfolding.TopCurve.GetNotAngledArray(sourceSize, imageRect.Width);
			arrayB = itPage.Bookfolding.BottomCurve.GetNotAngledArray(sourceSize, imageRect.Width);

			double smallestTop = double.MaxValue;

			for (x = 0; x < arrayT.Length; x++)
				if (smallestTop > arrayT[x])
					smallestTop = arrayT[x];

			if (smallestTop != double.MaxValue)
				for (x = 0; x < arrayT.Length; x++)
					arrayT[x] -= smallestTop;
					
			if ((lensCenter - smallestTop) > 10.0)
				for (x = 0; x < arrayT.Length; x++)
					arrayT[x] = arrayT[x] * (lensCenter - imageRect.Y) / (lensCenter - smallestTop);


			double biggestNumber = double.MinValue;
			for (x = 0; x < arrayB.Length; x++)
				if (biggestNumber < arrayB[x])
					biggestNumber = arrayB[x];
			
			for (x = 0; x < arrayB.Length; x++)
				arrayB[x] = biggestNumber - arrayB[x];
			
			if ((biggestNumber - lensCenter) > 10.0)
				for (x = 0; x < arrayB.Length; x++)
					arrayB[x] = (arrayB[x] * (imageRect.Bottom - lensCenter)) / (biggestNumber - lensCenter);
		}
		#endregion

		#region GetDelimiters()
		private static void GetDelimiters(ImageProcessing.IpSettings.ItPage itPage, out Delimiter topDelimiter, out Delimiter bottomDelimiter)
		{
			Delimiters delimiters = itPage.Delimiters;
			
			topDelimiter = null;
			bottomDelimiter = null;

			foreach (Delimiter delimiter in delimiters)
			{
				if (((topDelimiter == null) || (topDelimiter.Y > delimiter.Y)) && IsObjectCurvePositionValid(itPage, delimiter.Width, delimiter.Bottom, true))
					topDelimiter = delimiter;
				else if (((bottomDelimiter == null) || (bottomDelimiter.Bottom < delimiter.Bottom)) && IsObjectCurvePositionValid(itPage, delimiter.Width, delimiter.Y, false))
					bottomDelimiter = delimiter;
			}
		}
		#endregion

		#region GetPictures()
		private static void GetPictures(ImageProcessing.IpSettings.ItPage itPage, out Picture pictureT, out Picture pictureB)
		{
			pictureT = null;
			pictureB = null;
			Pictures pictures = itPage.Pictures;

			foreach (Picture picture in pictures)
			{
				if (((pictureT == null) || (pictureT.Y > picture.Y)) && IsObjectCurvePositionValid(itPage, picture.Width, picture.Y, true))
					pictureT = picture;
				if (((pictureB == null) || (pictureB.Bottom < picture.Bottom)) && IsObjectCurvePositionValid(itPage, picture.Width, picture.Bottom, false))
					pictureB = picture;
			}
		}
		#endregion

		#region GetTopCurve()
		private static ImageProcessing.IpSettings.Curve GetTopCurve(ImageProcessing.IpSettings.ItPage itPage, out float confidence)
		{
			ImageProcessing.IpSettings.ClipInt	clipInt = new ImageProcessing.IpSettings.ClipInt(itPage.ClipRect, itPage.Skew, itPage.PageObjectsSize.Value);
			int									globalOpticsCenter = Convert.ToInt32(itPage.GlobalOpticsCenter * itPage.PageObjectsSize.Value.Height);
			WeightedCurve						weightCurve = new WeightedCurve(clipInt.RectangleNotSkewed.Width);
			int?								upmostY = null;

			foreach (Picture picture in itPage.Pictures)
				if (IsObjectPositionValid(clipInt.RectangleNotSkewed, globalOpticsCenter, picture.Y, true) && IsWideEnought(clipInt.RectangleNotSkewed, picture.Width) && picture.TopCurveExists)
				{
					double weight;
					Point[] bfPoints = picture.GetTopBfPoints();

					if (bfPoints != null)
					{
						double[]	curveArray = ImageProcessing.IpSettings.Curve.GetArray(bfPoints);
						double		biggestY = GetBiggestValue(curveArray);

						ShiftCurveToThePageEdge(clipInt, globalOpticsCenter, curveArray, true);

						weight = (itPage.GlobalOpticsCenter - biggestY) / (double)(itPage.GlobalOpticsCenter - itPage.Clip.RectangleNotSkewed.Top);

						weightCurve.AddCurve(bfPoints[0].X - clipInt.RectangleNotSkewed.X, weight, curveArray);

						if (upmostY.HasValue == false || upmostY > biggestY)
							upmostY = (int)biggestY;
					}
				}

			foreach (Delimiter delimiter in itPage.Delimiters)
				if (IsObjectPositionValid(clipInt.RectangleNotSkewed, globalOpticsCenter, delimiter.Y, true) && IsWideEnought(clipInt.RectangleNotSkewed, delimiter.Width) && delimiter.IsHorizontal && delimiter.CurveExists)
				{
					double weight;
					Point[] bfPoints = delimiter.GetBfPoints();

					if (bfPoints != null)
					{
						double[]	curveArray = ImageProcessing.IpSettings.Curve.GetArray(bfPoints);
						double		biggestY = GetBiggestValue(curveArray);

						ShiftCurveToThePageEdge(clipInt, globalOpticsCenter, curveArray, true);

						weight = (itPage.GlobalOpticsCenter - biggestY) / (double)(itPage.GlobalOpticsCenter - itPage.Clip.RectangleNotSkewed.Top);

						weightCurve.AddCurve(bfPoints[0].X - clipInt.RectangleNotSkewed.X, weight, curveArray);

						if (upmostY.HasValue == false || upmostY > biggestY)
							upmostY = (int)biggestY;
					}
				}

			foreach (Line line in itPage.Lines)
				if (IsObjectPositionValid(clipInt.RectangleNotSkewed, globalOpticsCenter, line.Y, true) && (line.IsValidBfLine) && IsWideEnought(clipInt.RectangleNotSkewed, line.Width))
				{
					double		weight;
					Point[]		bfPoints = line.GetBfPoints();

					if (bfPoints != null)
					{
						double[]	curveArray = ImageProcessing.IpSettings.Curve.GetArray(bfPoints);
						double		biggestY = GetBiggestValue(curveArray);

						ShiftCurveToThePageEdge(clipInt, globalOpticsCenter, curveArray, true);

						weight = (itPage.GlobalOpticsCenter - biggestY) / (double)(itPage.GlobalOpticsCenter - itPage.Clip.RectangleNotSkewed.Top);

						weightCurve.AddCurve(bfPoints[0].X - clipInt.RectangleNotSkewed.X, weight, curveArray);

						foreach(Word word in line.Words)
							if (upmostY.HasValue == false || upmostY > word.Seat)
								upmostY = (int)word.Seat;
					}
				}

			Point[] points = weightCurve.GetCurve(10);
			for (int i = 0; i < points.Length; i++)
			{
				points[i].X += clipInt.RectangleNotSkewed.X;
				points[i].Y += clipInt.RectangleNotSkewed.Top;
			}

			if (upmostY != null)
				ShiftCurveToTheY(globalOpticsCenter, upmostY.Value, points, true);

			RatioPoint[] ratioPoints = new RatioPoint[points.Length];

			for(int i = 0; i < points.Length; i++)
			{
				ratioPoints[i].X += points[i].X / (double)itPage.PageObjectsSize.Value.Width;
				ratioPoints[i].Y += points[i].Y / (double)itPage.PageObjectsSize.Value.Height;
			}

			ImageProcessing.IpSettings.Curve curve = new ImageProcessing.IpSettings.Curve(itPage, ratioPoints, true);
			confidence = weightCurve.Confidence;
			return curve;
		}	
		#endregion

		#region GetBottomCurve()
		private static ImageProcessing.IpSettings.Curve GetBottomCurve(ImageProcessing.IpSettings.ItPage itPage, out float confidence)
		{
			ImageProcessing.IpSettings.ClipInt	clipInt = new ImageProcessing.IpSettings.ClipInt(itPage.ClipRect, itPage.Skew, itPage.PageObjectsSize.Value);
			int									globalOpticsCenter = Convert.ToInt32(itPage.GlobalOpticsCenter * itPage.PageObjectsSize.Value.Height);
			WeightedCurve						weightCurve = new WeightedCurve(clipInt.RectangleNotSkewed.Width);
			int?								downmostY = null;

			foreach (Picture picture in itPage.Pictures)
				if (IsObjectPositionValid(clipInt.RectangleNotSkewed, globalOpticsCenter, picture.Bottom, false) && IsWideEnought(clipInt.RectangleNotSkewed, picture.Width) && picture.BottomCurveExists)
				{
					double weight;
					Point[] bfPoints = picture.GetBottomBfPoints();

					if (bfPoints != null)
					{
						double[]	curveArray = ImageProcessing.IpSettings.Curve.GetArray(bfPoints);
						double		smallestY = GetSmallestValue(curveArray);

						ShiftCurveToThePageEdge(clipInt, globalOpticsCenter, curveArray, false);

						weight = (smallestY - itPage.GlobalOpticsCenter) / (double)(itPage.Clip.RectangleNotSkewed.Bottom - itPage.GlobalOpticsCenter);

						weightCurve.AddCurve(bfPoints[0].X - clipInt.RectangleNotSkewed.X, weight, curveArray);

						if (downmostY.HasValue == false || downmostY < smallestY)
							downmostY = (int)smallestY;
					}
				}

			foreach (Delimiter delimiter in itPage.Delimiters)
				if (IsObjectPositionValid(clipInt.RectangleNotSkewed, globalOpticsCenter, delimiter.Bottom, false) && IsWideEnought(clipInt.RectangleNotSkewed, delimiter.Width) && delimiter.IsHorizontal && delimiter.CurveExists)
				{
					double weight;
					Point[] bfPoints = delimiter.GetBfPoints();

					if (bfPoints != null)
					{
						double[]	curveArray = ImageProcessing.IpSettings.Curve.GetArray(bfPoints);
						double		smallestY = GetSmallestValue(curveArray);

						ShiftCurveToThePageEdge(clipInt, globalOpticsCenter, curveArray, false);

						weight = (smallestY - itPage.GlobalOpticsCenter) / (double)(itPage.Clip.RectangleNotSkewed.Bottom - itPage.GlobalOpticsCenter);

						weightCurve.AddCurve(bfPoints[0].X - clipInt.RectangleNotSkewed.X, weight, curveArray);

						if (downmostY.HasValue == false || downmostY < smallestY)
							downmostY = (int)smallestY;
					}
				}

			foreach (Line line in itPage.Lines)
				if (IsObjectPositionValid(clipInt.RectangleNotSkewed, globalOpticsCenter, line.Bottom, false) && (line.IsValidBfLine) && IsWideEnought(clipInt.RectangleNotSkewed, line.Width))
				{
					double		weight;
					Point[]		bfPoints = line.GetBfPoints();

					if (bfPoints != null)
					{
						double[]	curveArray = ImageProcessing.IpSettings.Curve.GetArray(bfPoints);
						double		smallestY = GetSmallestValue(curveArray);

						ShiftCurveToThePageEdge(clipInt, globalOpticsCenter, curveArray, false);

						weight = (smallestY - itPage.GlobalOpticsCenter) / (double)(itPage.Clip.RectangleNotSkewed.Bottom - itPage.GlobalOpticsCenter);

						weightCurve.AddCurve(bfPoints[0].X - clipInt.RectangleNotSkewed.X, weight, curveArray);

						if (downmostY.HasValue == false || downmostY < smallestY)
							downmostY = (int)smallestY;
					}
				}

			Point[] points = weightCurve.GetCurve(10);
			for (int i = 0; i < points.Length; i++)
			{
				points[i].X += clipInt.RectangleNotSkewed.X;
				points[i].Y += clipInt.RectangleNotSkewed.Bottom;
			}

			if(downmostY != null)
				ShiftCurveToTheY(globalOpticsCenter, downmostY.Value, points, false);

			RatioPoint[] ratioPoints = new RatioPoint[points.Length];

			for (int i = 0; i < points.Length; i++)
			{
				ratioPoints[i].X += points[i].X / (double)itPage.PageObjectsSize.Value.Width;
				ratioPoints[i].Y += points[i].Y / (double)itPage.PageObjectsSize.Value.Height;
			}

			ImageProcessing.IpSettings.Curve curve = new ImageProcessing.IpSettings.Curve(itPage, ratioPoints, false);
			confidence = weightCurve.Confidence;
			return curve;
		}	
		#endregion

		#region IsObjectCurvePositionValid()
		private static bool IsObjectCurvePositionValid(ImageProcessing.IpSettings.ItPage itPage, int width, int y, bool topCurve)
		{
			if (topCurve)
				return ((y < (itPage.GlobalOpticsCenter - (itPage.GlobalOpticsCenter - itPage.Clip.RectangleNotSkewed.Top) / 2)) && (width > itPage.Clip.RectangleNotSkewed.Width * 0.7F));
			else
				return ((y > itPage.Clip.RectangleNotSkewed.Bottom - itPage.Clip.RectangleNotSkewed.Height / 3) && (width > itPage.Clip.RectangleNotSkewed.Width * 0.7F));
		}
		#endregion

		#region IsObjectPositionValid()
		private static bool IsObjectPositionValid(Rectangle itPage, int globalOpticsCenter, int y, bool topCurve)
		{
			if (topCurve)
				return (y < (globalOpticsCenter - (globalOpticsCenter - itPage.Y) * 0.66));
			else
				return (y > (globalOpticsCenter + (itPage.Bottom - globalOpticsCenter) * 0.66));
		}
		#endregion

		#region IsWideEnought()
		private static bool IsWideEnought(Rectangle itPage, int width)
		{
			return (width > itPage.Width * 0.2F && width > 100);
		}
		#endregion

		#region MergeLines()
		private static Point[] MergeLines(ImageProcessing.IpSettings.ClipInt itPage, int globalOpticsCenter, Lines lines, bool topCurve)
		{			
			WeightedCurve		weightCurve = new WeightedCurve(itPage.RectangleNotSkewed.Width);

			foreach (Line line in lines)
			{
				double			weight;
				Point[]			linePoints = line.GetBfPoints();

				if (linePoints != null)
				{
					double[] curve = ImageProcessing.IpSettings.Curve.GetArray(linePoints);
					ShiftCurveToThePageEdge(itPage, globalOpticsCenter, curve, topCurve);

					if (topCurve)
					{
						double biggestY = GetBiggestValue(curve);
						weight = (globalOpticsCenter - biggestY) / (double)(globalOpticsCenter - itPage.RectangleNotSkewed.Top);
					}
					else
					{
						double smallestY = GetSmallestValue(curve);
						weight = (smallestY - globalOpticsCenter) / (double)(itPage.RectangleNotSkewed.Bottom - globalOpticsCenter);
					}

					weightCurve.AddCurve(linePoints[0].X - itPage.RectangleNotSkewed.X, weight, curve);
				}
			}

			Point[]		points = weightCurve.GetCurve(10);
			for (int i = 0; i < points.Length; i++)
			{
				points[i].X += itPage.RectangleNotSkewed.X;
				points[i].Y += itPage.RectangleNotSkewed.Bottom;
			}

			Line		downmostLine = GetDownmostLine(lines);

			ShiftCurveToTheY(globalOpticsCenter, (topCurve) ? downmostLine.Bottom : downmostLine.Y, points, topCurve);

			return points;
		}
		#endregion

		#region ShiftCurveToThePageEdge()
		/// <summary>
		/// Multiply curve array by position - itPage edge ratio.
		/// </summary>
		/// <param name="itPage"></param>
		/// <param name="line"></param>
		/// <param name="curve">Curve array</param>
		/// <param name="topCurve"></param>
		private static void ShiftCurveToThePageEdge(ImageProcessing.IpSettings.ClipInt itPage, int globalOpticsCenter, double[] curve, bool topCurve)
		{
			double ratio;

			if (topCurve)
			{
				double biggestY = GetBiggestValue(curve);

				ratio = (globalOpticsCenter - itPage.RectangleNotSkewed.Top) / (double)(globalOpticsCenter - biggestY);
			}
			else
			{
				double smallestY = GetSmallestValue(curve);

				ratio = (itPage.RectangleNotSkewed.Bottom - globalOpticsCenter) / (double)(smallestY - globalOpticsCenter);
			}

			for (int i = 0; i < curve.Length; i++)
				curve[i] *= ratio;
		}
		#endregion

		#region ShiftCurveToTheY()
		private static void ShiftCurveToTheY(int globalOpticsCenter, int y, Point[] points, bool topCurve)
		{
			double		ratio;

			if (topCurve)
			{
				double smallestY = points[0].Y;

				for (int i = 1; i < points.Length; i++)
					if (smallestY > points[i].Y)
						smallestY = points[i].Y;

				ratio = (globalOpticsCenter - y) / (double)(globalOpticsCenter - smallestY);

				for (int i = 0; i < points.Length; i++)
					points[i].Y = Convert.ToInt32(globalOpticsCenter - (globalOpticsCenter - points[i].Y) * ratio);
			}
			else
			{
				double biggestY = points[0].Y;

				for (int i = 1; i < points.Length; i++)
					if (biggestY < points[i].Y)
						biggestY = points[i].Y;

				ratio = (y - globalOpticsCenter) / (double)(biggestY - globalOpticsCenter);

				for (int i = 0; i < points.Length; i++)
					points[i].Y = Convert.ToInt32(globalOpticsCenter + (points[i].Y - globalOpticsCenter) * ratio);
			}
		}
		#endregion

		#region GetLongestLine()
		private static Line GetDownmostLine(Lines lines)
		{
			if (lines != null && lines.Count > 0)
			{
				Line line = lines[0];

				for (int i = 1; i < lines.Count; i++)
					if (line.Bottom < lines[i].Bottom)
						line = lines[i];

				return line;
			}
			else
				return null;
		}
		#endregion

		#region GetSmallestValue()
		private static double GetSmallestValue(double[] curve)
		{
			double smallestY = curve[0];

			for (int i = 1; i < curve.Length; i++)
				if (smallestY > curve[i])
					smallestY = curve[i];

			return smallestY;
		}
		#endregion

		#region GetBiggestValue()
		private static double GetBiggestValue(double[] curve)
		{
			double biggestY = curve[0];

			for (int i = 1; i < curve.Length; i++)
				if (biggestY < curve[i])
					biggestY = curve[i];

			return biggestY;
		}
		#endregion

		#region TopCurveShouldExist()
		/// <summary>
		/// Returns true if there is at least 1 inch of space where line, picture or delimiter should be.
		/// </summary>
		/// <param name="itPage"></param>
		/// <returns></returns>
		private static bool TopCurveShouldExist(ImageProcessing.IpSettings.ItPage itPage)
		{
			//return (((itPage.GlobalOpticsCenter - itPage.Clip.RectangleNotSkewed.Top) / 3.0) > itPage.ItImage.ImageInfo.DpiH);
			return ((itPage.GlobalOpticsCenter - itPage.Clip.RectangleNotSkewed.Top) * itPage.ItImage.InchSize.Height > 1);
		}
		#endregion

		#region BottomCurveShouldExist()
		/// <summary>
		/// Returns true if there is at least 1 inch of space where line, picture or delimiter should be.
		/// </summary>
		/// <param name="itPage"></param>
		/// <returns></returns>
		private static bool BottomCurveShouldExist(ImageProcessing.IpSettings.ItPage itPage)
		{
			//return (((itPage.Clip.RectangleNotSkewed.Bottom - itPage.GlobalOpticsCenter) / 3.0) > itPage.ItImage.ImageInfo.DpiH);
			return ((itPage.Clip.RectangleNotSkewed.Bottom - itPage.GlobalOpticsCenter) * itPage.ItImage.InchSize.Height > 1);
		}
		#endregion

		#region GetSourceRect()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="resultRect"></param>
		/// <param name="arrayT"></param>
		/// <param name="arrayB">Negative array</param>
		/// <param name="lensCenterOnResult"></param>
		/// <param name="sourceSize"></param>
		/// <param name="resultSize"></param>
		/// <returns></returns>
		private Rectangle GetSourceRect(Rectangle resultClip, double[] arrayT, double[] arrayB, int lensCenterOnSource, Size sourceSize, Rectangle resultR)
		{
			double top = resultClip.Top;
			double bottom = resultClip.Bottom;
			Rectangle sourceRect = new Rectangle(0, 0, sourceSize.Width, sourceSize.Height);

			//top
			if (resultClip.Top > lensCenterOnSource)
			{
				double maxArrayY = arrayT[0];
				
				for (int x = 0; x < resultClip.Width; x++)
					if (maxArrayY < arrayB[x])
						maxArrayY = arrayB[x];

				top = resultClip.Top - (maxArrayY * ((resultClip.Top - lensCenterOnSource) / (double)(resultR.Bottom - lensCenterOnSource)));
				top = Math.Min(top, resultClip.Top);
			}

			//bottom
			if (resultClip.Bottom < lensCenterOnSource)
			{
				double maxArrayY = arrayT[0];
				
				for (int x = 0; x < resultClip.Width; x++)
					if (maxArrayY < arrayT[x])
						maxArrayY = arrayT[x];

				bottom = resultClip.Bottom - (maxArrayY * ((lensCenterOnSource - resultClip.Bottom) / (double)(lensCenterOnSource - resultR.Top)));
				bottom = Math.Max(bottom, resultClip.Bottom);
			}

			Rectangle rect = Rectangle.FromLTRB(resultClip.X, (int)Math.Ceiling(top), resultClip.Right, (int)Math.Floor(bottom));
			rect.Intersect(sourceRect);

			return rect;
		}
		#endregion

		#endregion

	}

}
