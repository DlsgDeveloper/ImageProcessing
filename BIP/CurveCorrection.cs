using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using ImageProcessing.PageObjects;


namespace ImageProcessing
{
	public class CurveCorrection
	{
		
		#region class WeightedCurve
		/// <summary>
		/// Weighted curve is 2-dimensional curve made out of one or more curves merged together. 
		/// Instead of storing curve values or bamps, it stores y-pixel difference for each valid 
		/// pixel in the curve. Valid pixel is pixel that at least 1 curve adding to be merged contains.
		/// Position of the curve is unknown.
		/// Curves can have different weights - importances. In page curve search, objects closer 
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

		#region GetFromParams()
		public static Bitmap GetFromParams(Bitmap bitmap, ItPage page, int flags)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			try
			{
				Bitmap result = null;

				switch (bitmap.PixelFormat)
				{
					case PixelFormat.Format32bppRgb:
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format8bppIndexed:
					case PixelFormat.Format1bppIndexed:
						result = Stretch(bitmap, page);
						break;					
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}

				if (result != null)
				{
					Misc.SetBitmapResolution(result, bitmap.HorizontalResolution, bitmap.VerticalResolution);
					
					if ((result.PixelFormat == PixelFormat.Format1bppIndexed) || (result.PixelFormat == PixelFormat.Format8bppIndexed))
						result.Palette = bitmap.Palette;
				}

				return result;
			}
			catch (Exception ex)
			{
				throw new Exception("CurveCorrection, Get(): " + ex.Message);
			}
			finally
			{
#if DEBUG
				Console.WriteLine(string.Format("CurveCorrection: {0}", DateTime.Now.Subtract(start).ToString()));
#endif
			}
		}
		#endregion

		#region FindCuring()
		public static void FindCuring(ItPage page)
		{
			float		confidenceT = 0, confidenceB = 0;
			Curve		curveT, curveB;

			if (TopCurveShouldExist(page))
				curveT = GetTopCurve(page, out confidenceT);
			else
			{
				curveT = new Curve(page.Clip, true);
				confidenceT = 1.0F;
			}

			if (BottomCurveShouldExist(page))
				curveB = GetBottomCurve(page, out confidenceB);
			else
			{
				curveB = new Curve(page.Clip, false);
				confidenceB = 1.0F;
			}

			page.Bookfolding.SetCurves(curveT, curveB, Math.Min(confidenceT, confidenceB));
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region Stretch()
		private static unsafe Bitmap Stretch(Bitmap source, ItPage page)
		{
			BitmapData sourceData = null;
			BitmapData resultData = null;
			Bitmap result = null;

			try
			{
				int sourceW = source.Width, sourceH = source.Height;
				int x;
				int y;
				double firstPixelPortion;
				double currentVal;
				double[] arrayT;
				double[] arrayB;
				byte* pCurrentS;
				byte* pCurrentR;

				Rectangle clip = Rectangle.Intersect(page.Clip.RectangleNotSkewed, new Rectangle(0, 0, sourceW, sourceH));
				result = new Bitmap(clip.Width, clip.Height, source.PixelFormat);
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				int resultH = clip.Height;
				int resultW = clip.Width;
				double yJump = 1.0;
				int lensCenter = Math.Max(0, Math.Min(page.Clip.RectangleNotSkewed.Bottom, page.LocalOpticsCenter));

				GetCurves(page, out arrayT, out arrayB);

				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();

				if (source.PixelFormat == PixelFormat.Format24bppRgb)
				{
					int pixelBytes = (source.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
					
					if (lensCenter > 0)
					{
						for (x = 0; x < resultW; x++)
						{
							yJump = 1.0 - (arrayT[x] / ((double)lensCenter));
							firstPixelPortion = 1.0 - (arrayT[x] - ((int)arrayT[x]));
							pCurrentS = (pSource + (((int)arrayT[x]) * sStride)) + (x * pixelBytes);
							pCurrentR = pResult + (x * pixelBytes);
							y = 0;

							while (y < lensCenter)
							{
								if ((firstPixelPortion - yJump) > 0.00000008)
								{
									if (y < resultH && pCurrentS < pSource + resultH * sStride)
									{
										pCurrentR[0] = pCurrentS[0];
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
									}
									firstPixelPortion -= yJump;
								}
								else if ((firstPixelPortion - yJump) < -0.00000008)
								{
									if (y < resultH && pCurrentS < pSource + resultH * sStride - sStride)
									{
										currentVal = (firstPixelPortion * pCurrentS[0]) + ((yJump - firstPixelPortion) * pCurrentS[sStride]);
										pCurrentR[0] = Convert.ToByte((double)(currentVal / yJump));
										currentVal = (firstPixelPortion * pCurrentS[1]) + ((yJump - firstPixelPortion) * pCurrentS[1 + sStride]);
										pCurrentR[1] = Convert.ToByte((double)(currentVal / yJump));
										currentVal = (firstPixelPortion * pCurrentS[2]) + ((yJump - firstPixelPortion) * pCurrentS[2 + sStride]);
										pCurrentR[2] = Convert.ToByte((double)(currentVal / yJump));
									}
									pCurrentS += sStride;
									firstPixelPortion += 1.0 - yJump;
								}
								else
								{
									if (y < resultH && pCurrentS < pSource + resultH * sStride)
									{
										pCurrentR[0] = pCurrentS[0];
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
									}
									firstPixelPortion = 1.0;
									pCurrentS += sStride;
								}
								pCurrentR += rStride;
								y++;
							}
						}
					}
					if (lensCenter < (resultH - 1))
					{
						for (x = 0; x < resultW; x++)
						{
							yJump = 1.0 - (arrayB[x] / ((double)(resultH - lensCenter)));
							firstPixelPortion = 1.0;
							pCurrentS = (pSource + (lensCenter * sStride)) + (x * pixelBytes);
							pCurrentR = (pResult + (lensCenter * rStride)) + (x * pixelBytes);

							for (y = lensCenter; y < resultH; y++)
							{
								if ((firstPixelPortion - yJump) > 0.00000001)
								{
									if (y >= 0 && pCurrentS >= pSource)
									{
										pCurrentR[0] = pCurrentS[0];
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
									}
									firstPixelPortion -= yJump;
								}
								else if ((firstPixelPortion - yJump) < -0.00000001)
								{
									if (y >= 0 && pCurrentS >= pSource)
									{
										currentVal = (firstPixelPortion * pCurrentS[0]) + ((yJump - firstPixelPortion) * pCurrentS[sStride]);
										pCurrentR[0] = Convert.ToByte((double)(currentVal / yJump));
										currentVal = (firstPixelPortion * pCurrentS[1]) + ((yJump - firstPixelPortion) * pCurrentS[1 + sStride]);
										pCurrentR[1] = Convert.ToByte((double)(currentVal / yJump));
										currentVal = (firstPixelPortion * pCurrentS[2]) + ((yJump - firstPixelPortion) * pCurrentS[2 + sStride]);
										pCurrentR[2] = Convert.ToByte((double)(currentVal / yJump));
									}
									pCurrentS += sStride;
									firstPixelPortion += 1.0 - yJump;
								}
								else
								{
									if (y >= 0 && pCurrentS >= pSource)
									{
										pCurrentR[0] = pCurrentS[0];
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
									}
									pCurrentS += sStride;
									firstPixelPortion = 1.0;
								}
								pCurrentR += rStride;
							}
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format32bppArgb || source.PixelFormat == PixelFormat.Format32bppRgb)
				{
					int pixelBytes = (source.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

					if (lensCenter > 0)
					{
						for (x = 0; x < resultW; x++)
						{
							yJump = 1.0 - (arrayT[x] / ((double)lensCenter));
							firstPixelPortion = 1.0 - (arrayT[x] - ((int)arrayT[x]));
							pCurrentS = (pSource + (((int)arrayT[x]) * sStride)) + (x * pixelBytes);
							pCurrentR = pResult + (x * pixelBytes);
							y = 0;

							while (y < lensCenter)
							{
								if ((firstPixelPortion - yJump) > 0.00000008)
								{
									if (y < resultH && pCurrentS < pSource + resultH * sStride)
									{
										pCurrentR[0] = pCurrentS[0];
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
										pCurrentR[3] = 255;
									}
									firstPixelPortion -= yJump;
								}
								else if ((firstPixelPortion - yJump) < -0.00000008)
								{
									if (y < resultH && pCurrentS < pSource + resultH * sStride - sStride)
									{
										currentVal = (firstPixelPortion * pCurrentS[0]) + ((yJump - firstPixelPortion) * pCurrentS[sStride]);
										pCurrentR[0] = Convert.ToByte((double)(currentVal / yJump));
										currentVal = (firstPixelPortion * pCurrentS[1]) + ((yJump - firstPixelPortion) * pCurrentS[1 + sStride]);
										pCurrentR[1] = Convert.ToByte((double)(currentVal / yJump));
										currentVal = (firstPixelPortion * pCurrentS[2]) + ((yJump - firstPixelPortion) * pCurrentS[2 + sStride]);
										pCurrentR[2] = Convert.ToByte((double)(currentVal / yJump));
										pCurrentR[3] = 255;
									}
									pCurrentS += sStride;
									firstPixelPortion += 1.0 - yJump;
								}
								else
								{
									if (y < resultH && pCurrentS < pSource + resultH * sStride)
									{
										pCurrentR[0] = pCurrentS[0];
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
										pCurrentR[3] = 255;
									}
									firstPixelPortion = 1.0;
									pCurrentS += sStride;
								}
								pCurrentR += rStride;
								y++;
							}
						}
					}
					if (lensCenter < (resultH - 1))
					{
						for (x = 0; x < resultW; x++)
						{
							yJump = 1.0 - (arrayB[x] / ((double)(resultH - lensCenter)));
							firstPixelPortion = 1.0;
							pCurrentS = (pSource + (lensCenter * sStride)) + (x * pixelBytes);
							pCurrentR = (pResult + (lensCenter * rStride)) + (x * pixelBytes);

							for (y = lensCenter; y < resultH; y++)
							{
								if ((firstPixelPortion - yJump) > 0.00000001)
								{
									if (y >= 0 && pCurrentS >= pSource)
									{
										pCurrentR[0] = pCurrentS[0];
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
										pCurrentR[3] = 255;
									}
									firstPixelPortion -= yJump;
								}
								else if ((firstPixelPortion - yJump) < -0.00000001)
								{
									if (y >= 0 && pCurrentS >= pSource)
									{
										currentVal = (firstPixelPortion * pCurrentS[0]) + ((yJump - firstPixelPortion) * pCurrentS[sStride]);
										pCurrentR[0] = Convert.ToByte((double)(currentVal / yJump));
										currentVal = (firstPixelPortion * pCurrentS[1]) + ((yJump - firstPixelPortion) * pCurrentS[1 + sStride]);
										pCurrentR[1] = Convert.ToByte((double)(currentVal / yJump));
										currentVal = (firstPixelPortion * pCurrentS[2]) + ((yJump - firstPixelPortion) * pCurrentS[2 + sStride]);
										pCurrentR[2] = Convert.ToByte((double)(currentVal / yJump));
										pCurrentR[3] = 255;
									}
									pCurrentS += sStride;
									firstPixelPortion += 1.0 - yJump;
								}
								else
								{
									if (y >= 0 && pCurrentS >= pSource)
									{
										pCurrentR[0] = pCurrentS[0];
										pCurrentR[1] = pCurrentS[1];
										pCurrentR[2] = pCurrentS[2];
										pCurrentR[3] = 255;
									}
									pCurrentS += sStride;
									firstPixelPortion = 1.0;
								}
								pCurrentR += rStride;
							}
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					if (lensCenter > 0)
					{
						for (x = 0; x < resultW; x++)
						{
							yJump = 1.0 - (arrayT[x] / ((double)lensCenter));
							firstPixelPortion = 1.0 - (arrayT[x] - ((int)arrayT[x]));
							pCurrentS = (pSource + (((int)arrayT[x]) * sStride)) + x;
							pCurrentR = pResult + x;

							for (y = 0; y < lensCenter; y++)
							{
								if ((firstPixelPortion - yJump) > 0.00000008)
								{
									if (y < resultH && pCurrentS < pSource + resultH * sStride)
										pCurrentR[0] = pCurrentS[0];

									firstPixelPortion -= yJump;
								}
								else if ((firstPixelPortion - yJump) < -0.00000008)
								{
									if (y < resultH && pCurrentS < pSource + resultH * sStride - sStride)
									{
										currentVal = (firstPixelPortion * pCurrentS[0]) + ((yJump - firstPixelPortion) * pCurrentS[sStride]);
										pCurrentR[0] = Convert.ToByte((double)(currentVal / yJump));
									}
									
									pCurrentS += sStride;
									firstPixelPortion += 1.0 - yJump;
								}
								else
								{
									if (y < resultH && pCurrentS < pSource + resultH * sStride)
										pCurrentR[0] = pCurrentS[0];

									firstPixelPortion = 1.0;
									pCurrentS += sStride;
								}
								
								pCurrentR += rStride;
							}
						}
					}
					if (lensCenter < (resultH - 1))
					{
						for (x = 0; x < resultW; x++)
						{
							yJump = 1.0 - (arrayB[x] / ((double)(resultH - lensCenter)));
							firstPixelPortion = 1.0;
							pCurrentS = (pSource + (lensCenter * sStride)) + x;
							pCurrentR = (pResult + (lensCenter * rStride)) + x;

							for (y = lensCenter; y < resultH; y++)
							{
								if ((firstPixelPortion - yJump) > 0.00000001)
								{
									if (y >= 0 && pCurrentS >= pSource)
									{
										pCurrentR[0] = pCurrentS[0];
									}
									firstPixelPortion -= yJump;
								}
								else if ((firstPixelPortion - yJump) < -0.00000001)
								{
									if (y >= 0 && pCurrentS >= pSource)
									{
										currentVal = (firstPixelPortion * pCurrentS[0]) + ((yJump - firstPixelPortion) * pCurrentS[sStride]);
										pCurrentR[0] = Convert.ToByte((double)(currentVal / yJump));
									}
									pCurrentS += sStride;
									firstPixelPortion += 1.0 - yJump;
								}
								else
								{
									if (y >= 0 && pCurrentS >= pSource)
									{
										pCurrentR[0] = pCurrentS[0];
									}
									pCurrentS += sStride;
									firstPixelPortion = 1.0;
								}
								pCurrentR += rStride;
							}
						}
					}
				}
				else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					double sourceY;
					int sourceYInt;
					int i;

					if (lensCenter > 0)
					{
						for (x = 0; x < resultW; x++)
						{
							yJump = 1.0 - (arrayT[x] / ((double)lensCenter));
							sourceY = lensCenter;
							pCurrentR = (pResult + (lensCenter * rStride)) + (x / 8);
							sourceYInt = (int)sourceY;

							for (y = lensCenter; y >= 0; y--)
							{
								//pCurrentR = (pResult + (y * rStride)) + (x / 8);
								pCurrentS = (pSource + (sourceYInt * sStride)) + (x / 8);
								i = ((int)0x80) >> (x & 7);

								if (pCurrentS < pSource + resultH * sStride - sStride && (pCurrentS[0] & i) > 0)
									pCurrentR[0] = (byte)(pCurrentR[0] | ((byte)(((int)0x80) >> (x & 7))));

								sourceY -= yJump;
								sourceYInt = Convert.ToInt32(sourceY);
								pCurrentR -= rStride;
							}
						}
					}
					if (lensCenter < (resultH - 1))
					{
						for (x = 0; x < resultW; x++)
						{
							yJump = 1.0 - (arrayB[x] / ((double)(resultH - lensCenter)));
							sourceY = lensCenter;
							pCurrentR = (pResult + (lensCenter * rStride)) + (x / 8);
							sourceYInt = Convert.ToInt32(sourceY);

							for (y = lensCenter; y < resultH; y++)
							{
								//pCurrentR = (pResult + (y * rStride)) + (x / 8);
								pCurrentS = (pSource + (sourceYInt * sStride)) + (x / 8);
								i = ((int)0x80) >> (x & 7);

								if ((pCurrentS[0] & i) > 0 && pCurrentS >= pSource)
									pCurrentR[0] = (byte)(pCurrentR[0] | ((byte)(((int)0x80) >> (x & 7))));

								sourceY += yJump;
								sourceYInt = Convert.ToInt32(sourceY);
								pCurrentR += rStride;
							}
						}
					}
				}
				
				return result;
			}
			finally
			{
				if ((source != null) && (sourceData != null))
					source.UnlockBits(sourceData);
				if ((result != null) && (resultData != null))
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region GetCurves()
		private static void GetCurves(ItPage page, out double[] arrayT, out double[] arrayB)
		{
			int x;
			int lensCenter = page.LocalOpticsCenter;
			arrayT = page.Bookfolding.TopCurve.GetNotAngledArray();
			arrayB = page.Bookfolding.BottomCurve.GetNotAngledArray();
			
			if (page.Clip.IsSkewed)
			{
				for (x = 0; x < arrayT.Length; x++)
					arrayT[x] -= Math.Tan(page.Clip.Skew) * x;
				for (x = 0; x < arrayT.Length; x++)
					arrayB[x] -= Math.Tan(page.Clip.Skew) * x;
			}
			
			double smallestNumber = 2147483647.0;
			for (x = 0; x < arrayT.Length; x++)
				if (smallestNumber > arrayT[x])
					smallestNumber = arrayT[x];
			
			if (smallestNumber != 0.0)
				for (x = 0; x < arrayT.Length; x++)
					arrayT[x] -= smallestNumber;
			
			if ((lensCenter - smallestNumber) > 10.0)
				for (x = 0; x < arrayT.Length; x++)
					arrayT[x] = (arrayT[x] * lensCenter) / (lensCenter - smallestNumber);
			
			double biggestNumber = -2147483648.0;
			for (x = 0; x < arrayB.Length; x++)
				if (biggestNumber < arrayB[x])
					biggestNumber = arrayB[x];
			
			for (x = 0; x < arrayB.Length; x++)
				arrayB[x] = biggestNumber - arrayB[x];
			
			if ((biggestNumber - lensCenter) > 10.0)
				for (x = 0; x < arrayB.Length; x++)
					arrayB[x] = (arrayB[x] * (page.Clip.RectangleNotSkewed.Bottom - lensCenter)) / (biggestNumber - lensCenter);
		}
		#endregion

		#region GetDelimiters()
		private static void GetDelimiters(ItPage page, out Delimiter topDelimiter, out Delimiter bottomDelimiter)
		{
			Delimiters delimiters = page.Delimiters;
			
			topDelimiter = null;
			bottomDelimiter = null;

			foreach (Delimiter delimiter in delimiters)
			{
				if (((topDelimiter == null) || (topDelimiter.Y > delimiter.Y)) && IsObjectCurvePositionValid(page, delimiter.Width, delimiter.Bottom, true))
					topDelimiter = delimiter;
				else if (((bottomDelimiter == null) || (bottomDelimiter.Bottom < delimiter.Bottom)) && IsObjectCurvePositionValid(page, delimiter.Width, delimiter.Y, false))
					bottomDelimiter = delimiter;
			}
		}
		#endregion

		#region GetPictures()
		private static void GetPictures(ItPage page, out Picture pictureT, out Picture pictureB)
		{
			pictureT = null;
			pictureB = null;
			Pictures pictures = page.Pictures;

			foreach (Picture picture in pictures)
			{
				if (((pictureT == null) || (pictureT.Y > picture.Y)) && IsObjectCurvePositionValid(page, picture.Width, picture.Y, true))
					pictureT = picture;
				if (((pictureB == null) || (pictureB.Bottom < picture.Bottom)) && IsObjectCurvePositionValid(page, picture.Width, picture.Bottom, false))
					pictureB = picture;
			}
		}
		#endregion

		#region GetTopCurve()
		private static Curve GetTopCurve(ItPage page, out float confidence)
		{
			WeightedCurve	weightCurve = new WeightedCurve(page.Clip.RectangleNotSkewed.Width);
			int?			upmostY = null;

			foreach (Picture picture in page.Pictures)
				if (IsObjectPositionValid(page, picture.Y, true) && IsWideEnought(page, picture.Width) && picture.TopCurveExists)
				{
					double weight;
					Point[] bfPoints = picture.GetTopBfPoints();

					if (bfPoints != null)
					{
						double[]	curveArray = Curve.GetArray(bfPoints);
						double		biggestY = GetBiggestValue(curveArray);
						
						ShiftCurveToThePageEdge(page, curveArray, true);

						weight = (page.GlobalOpticsCenter - biggestY) / (double)(page.GlobalOpticsCenter - page.Clip.RectangleNotSkewed.Top);

						weightCurve.AddCurve(bfPoints[0].X - page.Clip.RectangleNotSkewed.X, weight, curveArray);

						if (upmostY.HasValue == false || upmostY > biggestY)
							upmostY = (int)biggestY;
					}
				}

			foreach (Delimiter delimiter in page.Delimiters)
				if (IsObjectPositionValid(page, delimiter.Y, true) && IsWideEnought(page, delimiter.Width) && delimiter.IsHorizontal && delimiter.CurveExists)
				{
					double weight;
					Point[] bfPoints = delimiter.GetBfPoints();

					if (bfPoints != null)
					{
						double[]	curveArray = Curve.GetArray(bfPoints);
						double		biggestY = GetBiggestValue(curveArray);

						ShiftCurveToThePageEdge(page, curveArray, true);

						weight = (page.GlobalOpticsCenter - biggestY) / (double)(page.GlobalOpticsCenter - page.Clip.RectangleNotSkewed.Top);

						weightCurve.AddCurve(bfPoints[0].X - page.Clip.RectangleNotSkewed.X, weight, curveArray);

						if (upmostY.HasValue == false || upmostY > biggestY)
							upmostY = (int)biggestY;
					}
				}

			foreach (Line line in page.Lines)
				if (IsObjectPositionValid(page, line.Y, true) && (line.IsValidBfLine) && IsWideEnought(page, line.Width))
				{
					double		weight;
					Point[]		bfPoints = line.GetBfPoints();

					if (bfPoints != null)
					{
						double[]	curveArray = Curve.GetArray(bfPoints);
						double		biggestY = GetBiggestValue(curveArray);

						ShiftCurveToThePageEdge(page, curveArray, true);

						weight = (page.GlobalOpticsCenter - biggestY) / (double)(page.GlobalOpticsCenter - page.Clip.RectangleNotSkewed.Top);

						weightCurve.AddCurve(bfPoints[0].X - page.Clip.RectangleNotSkewed.X, weight, curveArray);

						foreach(Word word in line.Words)
							if (upmostY.HasValue == false || upmostY > word.Seat)
								upmostY = (int)word.Seat;
					}
				}

			Point[] points = weightCurve.GetCurve(10);
			for (int i = 0; i < points.Length; i++)
			{
				points[i].X += page.Clip.RectangleNotSkewed.X;
				points[i].Y += page.Clip.RectangleNotSkewed.Top;
			}

			if (upmostY != null)
				ShiftCurveToTheY(page, upmostY.Value, points, true);

			Curve curve = new Curve(page.Clip, points, true);
			confidence = weightCurve.Confidence;
			return curve;
		}	
		#endregion

		#region GetBottomCurve()
		private static Curve GetBottomCurve(ItPage page, out float confidence)
		{
			WeightedCurve	weightCurve = new WeightedCurve(page.Clip.RectangleNotSkewed.Width);
			int?			downmostY = null;

			foreach (Picture picture in page.Pictures)
				if (IsObjectPositionValid(page, picture.Bottom, false) && IsWideEnought(page, picture.Width) && picture.BottomCurveExists)
				{
					double weight;
					Point[] bfPoints = picture.GetBottomBfPoints();

					if (bfPoints != null)
					{
						double[]	curveArray = Curve.GetArray(bfPoints);
						double		smallestY = GetSmallestValue(curveArray);
						
						ShiftCurveToThePageEdge(page, curveArray, false);

						weight = (smallestY - page.GlobalOpticsCenter) / (double)(page.Clip.RectangleNotSkewed.Bottom - page.GlobalOpticsCenter);

						weightCurve.AddCurve(bfPoints[0].X - page.Clip.RectangleNotSkewed.X, weight, curveArray);

						if (downmostY.HasValue == false || downmostY < smallestY)
							downmostY = (int)smallestY;
					}
				}

			foreach (Delimiter delimiter in page.Delimiters)
				if (IsObjectPositionValid(page, delimiter.Bottom, false) && IsWideEnought(page, delimiter.Width) && delimiter.IsHorizontal && delimiter.CurveExists)
				{
					double weight;
					Point[] bfPoints = delimiter.GetBfPoints();

					if (bfPoints != null)
					{
						double[]	curveArray = Curve.GetArray(bfPoints);
						double		smallestY = GetSmallestValue(curveArray);

						ShiftCurveToThePageEdge(page, curveArray, false);

						weight = (smallestY - page.GlobalOpticsCenter) / (double)(page.Clip.RectangleNotSkewed.Bottom - page.GlobalOpticsCenter);

						weightCurve.AddCurve(bfPoints[0].X - page.Clip.RectangleNotSkewed.X, weight, curveArray);

						if (downmostY.HasValue == false || downmostY < smallestY)
							downmostY = (int)smallestY;
					}
				}

			foreach (Line line in page.Lines)
				if (IsObjectPositionValid(page, line.Bottom, false) && (line.IsValidBfLine) && IsWideEnought(page, line.Width))
				{
					double		weight;
					Point[]		bfPoints = line.GetBfPoints();

					if (bfPoints != null)
					{
						double[]	curveArray = Curve.GetArray(bfPoints);
						double		smallestY = GetSmallestValue(curveArray);

						ShiftCurveToThePageEdge(page, curveArray, false);

						weight = (smallestY - page.GlobalOpticsCenter) / (double)(page.Clip.RectangleNotSkewed.Bottom - page.GlobalOpticsCenter);

						weightCurve.AddCurve(bfPoints[0].X - page.Clip.RectangleNotSkewed.X, weight, curveArray);

						if (downmostY.HasValue == false || downmostY < smallestY)
							downmostY = (int)smallestY;
					}
				}

			Point[] points = weightCurve.GetCurve(10);
			for (int i = 0; i < points.Length; i++)
			{
				points[i].X += page.Clip.RectangleNotSkewed.X;
				points[i].Y += page.Clip.RectangleNotSkewed.Bottom;
			}

			if(downmostY != null)
				ShiftCurveToTheY(page, downmostY.Value, points, false);

			Curve curve = new Curve(page.Clip, points, false);
			confidence = weightCurve.Confidence;
			return curve;
		}	
		#endregion

		#region GetTopCurveFrom2Columns()
		/*private static Curve GetTopCurveFrom2Columns(Lines lines, ItPage page)
		{
			Line line1 = null;
			Line line2 = null;
			List<Line2D> insuitableLines = new List<Line2D>();
			
			Get2ColumnTopLines(lines, page, out line1, out line2);

			if (line1 != null && line2 != null && line1.IsValidBfLine && line2.IsValidBfLine)
			{
				if (line1.Right < line2.X)
				{
					if (line1.Width + line2.Width + page.ItImage.ImageInfo.DpiH > page.Clip.RectangleNotSkewed.Width * 3 / 4)
					{
						if ((line2.X - line1.Right < page.ItImage.ImageInfo.DpiH) && (line1.LastWord != null && line2.FirstWord != null))
						{
							List<Point> points = new List<Point>();
							points.AddRange(line1.GetBfPoints());
							points.AddRange(line2.GetBfPoints());

							Curve curve = new Curve(page.Clip, points.ToArray(), false);
							curve.FinishCurve(0);
							return curve;
						}
					}
				}
				if (line2.Right < line1.X)
				{
					if (line2.Width + line1.Width + page.ItImage.ImageInfo.DpiH > page.Clip.RectangleNotSkewed.Width * 3 / 4)
					{
						if ((line1.X - line2.Right < page.ItImage.ImageInfo.DpiH) && (line2.LastWord != null && line1.FirstWord != null))
						{
							List<Point> points = new List<Point>();
							points.AddRange(line2.GetBfPoints());
							points.AddRange(line1.GetBfPoints());

							Curve curve = new Curve(page.Clip, points.ToArray(), false);
							curve.FinishCurve(0);
							return curve;
						}
					}
				}
			}

			return null;
		}*/
		#endregion

		#region IsObjectCurvePositionValid()
		private static bool IsObjectCurvePositionValid(ItPage page, int width, int y, bool topCurve)
		{
			if (topCurve)
				return ((y < (page.GlobalOpticsCenter - (page.GlobalOpticsCenter - page.Clip.RectangleNotSkewed.Top) / 2)) && (width > page.Clip.RectangleNotSkewed.Width * 0.7F));
			else
				return ((y > page.Clip.RectangleNotSkewed.Bottom - page.Clip.RectangleNotSkewed.Height / 3) && (width > page.Clip.RectangleNotSkewed.Width * 0.7F));
		}
		#endregion

		#region IsObjectPositionValid()
		private static bool IsObjectPositionValid(ItPage page, int y, bool topCurve)
		{
			if (topCurve)
				return (y < (page.GlobalOpticsCenter - (page.GlobalOpticsCenter - page.Clip.RectangleNotSkewed.Y) * 0.66));
			else
				return (y > (page.GlobalOpticsCenter + (page.Clip.RectangleNotSkewed.Bottom - page.GlobalOpticsCenter) * 0.66));
		}
		#endregion

		#region IsWideEnought()
		private static bool IsWideEnought(ItPage page, int width)
		{
			return (width > page.Clip.RectangleNotSkewed.Width * 0.2F && width > 100);
		}
		#endregion

		#region MergeLines()
		private static Point[] MergeLines(ItPage page, Lines lines, bool topCurve)
		{
			//while (lines.Count > 2)
			//	lines.RemoveAt((lines[1].Bottom < lines[0].Bottom) ? ((lines[1].Bottom < lines[2].Bottom) ? 1 : 2) : ((lines[0].Bottom < lines[2].Bottom) ? 0 : 2));
			//while (lines.Count > 1)
				//lines.RemoveAt((lines[1].Bottom < lines[0].Bottom) ? 0 : 1);			
			
			WeightedCurve		weightCurve = new WeightedCurve(page.Clip.RectangleNotSkewed.Width);

			foreach (Line line in lines)
			{
				double			weight;
				Point[]			linePoints = line.GetBfPoints();

				if (linePoints != null)
				{
					List<PointF> linePointsF = new List<PointF>();

					foreach (Point linePoint in linePoints)
						linePointsF.Add(linePoint);

					double[] curve = Curve.GetArray(linePointsF.ToArray());
					ShiftCurveToThePageEdge(page, curve, topCurve);

					if (topCurve)
					{
						double biggestY = GetBiggestValue(curve);
						weight = (page.GlobalOpticsCenter - biggestY) / (double)(page.GlobalOpticsCenter - page.Clip.RectangleNotSkewed.Top);
					}
					else
					{
						double smallestY = GetSmallestValue(curve);
						weight = (smallestY - page.GlobalOpticsCenter) / (double)(page.Clip.RectangleNotSkewed.Bottom - page.GlobalOpticsCenter);
					}


					weightCurve.AddCurve(linePoints[0].X - page.Clip.RectangleNotSkewed.X, weight, curve);
				}
			}

			Point[]		points = weightCurve.GetCurve(10);
			for (int i = 0; i < points.Length; i++)
			{
				points[i].X += page.Clip.RectangleNotSkewed.X;
				points[i].Y += page.Clip.RectangleNotSkewed.Bottom;
			}

			Line		downmostLine = GetDownmostLine(lines);

			ShiftCurveToTheY(page, (topCurve) ? downmostLine.Bottom : downmostLine.Y, points, topCurve);

			return points;
		}
		#endregion

		#region ShiftCurveToThePageEdge()
		/// <summary>
		/// Multiply curve array by position - page edge ratio.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="line"></param>
		/// <param name="curve">Curve array</param>
		/// <param name="topCurve"></param>
		private static void ShiftCurveToThePageEdge(ItPage page, double[] curve, bool topCurve)
		{
			double ratio;

			if (topCurve)
			{
				double biggestY = GetBiggestValue(curve);
				
				ratio = (page.GlobalOpticsCenter - page.Clip.RectangleNotSkewed.Top) / (double)(page.GlobalOpticsCenter - biggestY);
			}
			else
			{
				double smallestY = GetSmallestValue(curve);

				ratio = (page.Clip.RectangleNotSkewed.Bottom - page.GlobalOpticsCenter) / (double)(smallestY - page.GlobalOpticsCenter);
			}

			for (int i = 0; i < curve.Length; i++)
				curve[i] *= ratio;
		}
		#endregion

		#region ShiftCurveToTheY()
		private static void ShiftCurveToTheY(ItPage page, int y, Point[] points, bool topCurve)
		{
			double		ratio;

			if (topCurve)
			{
				double smallestY = points[0].Y;

				for (int i = 1; i < points.Length; i++)
					if (smallestY > points[i].Y)
						smallestY = points[i].Y;

				ratio = (page.GlobalOpticsCenter - y) / (double)(page.GlobalOpticsCenter - smallestY);

				for (int i = 0; i < points.Length; i++)
					points[i].Y = Convert.ToInt32(page.GlobalOpticsCenter - (page.GlobalOpticsCenter - points[i].Y) * ratio);
			}
			else
			{
				double biggestY = points[0].Y;

				for (int i = 1; i < points.Length; i++)
					if (biggestY < points[i].Y)
						biggestY = points[i].Y;

				ratio = (y - page.GlobalOpticsCenter) / (double)(biggestY - page.GlobalOpticsCenter);

				for (int i = 0; i < points.Length; i++)
					points[i].Y = Convert.ToInt32(page.GlobalOpticsCenter + (points[i].Y - page.GlobalOpticsCenter) * ratio);
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
		/// <param name="page"></param>
		/// <returns></returns>
		private static bool TopCurveShouldExist(ItPage page)
		{
			return (((page.GlobalOpticsCenter - page.Clip.RectangleNotSkewed.Top) / 3.0) > page.ItImage.ImageInfo.DpiH);
		}
		#endregion

		#region BottomCurveShouldExist()
		/// <summary>
		/// Returns true if there is at least 1 inch of space where line, picture or delimiter should be.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		private static bool BottomCurveShouldExist(ItPage page)
		{
			return (((page.Clip.RectangleNotSkewed.Bottom - page.GlobalOpticsCenter) / 3.0) > page.ItImage.ImageInfo.DpiH);
		}
		#endregion

		#endregion

	}

}
