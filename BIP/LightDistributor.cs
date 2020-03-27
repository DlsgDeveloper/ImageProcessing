using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Collections.Generic;
using ImageProcessing.PageObjects;

namespace ImageProcessing
{
	public class LightDistributor
	{

		#region constructor
		private LightDistributor()
		{
		}
		#endregion

		#region ColorDelta
		public class ColorDelta
		{
			public short R;
			public short G;
			public short B;

			public ColorDelta(short dr, short dg, short db)
			{
				this.R = dr;
				this.G = dg;
				this.B = db;
			}

			public ColorDelta(int dr, int dg, int db)
				:this((short) dr, (short) dg, (short) db)
			{
			}

			public void Set(int dr, int dg, int db)
			{
				this.R = (short) dr;
				this.G = (short)dg;
				this.B = (short)db;
			}
		}
		#endregion

		#region class Gradient
		public class Gradient
		{
			Size imageSize;
			ColorDelta[,] colorDeltaArray;
			PageOrientation pageOrientation;

			#region constructor
			public Gradient(Size imageSize, PageOrientation pageOrientation)
			{
				this.imageSize = imageSize;
				colorDeltaArray = new ColorDelta[imageSize.Height / 16, imageSize.Width / 16];
				this.pageOrientation = pageOrientation;
			}
			#endregion

			#region enum PageOrientation
			public enum PageOrientation
			{
				Regular,
				Rotated90Degrees,
				Unknown
			}
			#endregion

			#region Add()
			public void Add(int x, int y, ColorDelta colorDelta)
			{
				colorDeltaArray[y / 16, x / 16] = colorDelta;
			}
			#endregion

			#region Finish()
			public void Finish()
			{
				bool horizontalGradient;
				
				switch(pageOrientation)
				{
					case PageOrientation.Unknown: horizontalGradient = IsGradientHorizontal(); break;
					case PageOrientation.Rotated90Degrees: horizontalGradient = false; break;
					default: horizontalGradient = true; break;
				}
				
				//Validate(horizontalGradient, 10);
#if SAVE_RESULTS
				DrawToFile(Debug.SaveToDir + @"\gradient2.png");
#endif
				SmoothGradient();

				AddMissingPoints(4, horizontalGradient);
				AddMissingPoints(3, horizontalGradient);
				AddMissingPoints(2, horizontalGradient);
				AddMissingPoints(1, horizontalGradient);
				AddMissingPoints(4);
				AddMissingPoints(3);
				AddMissingPoints(2);
				AddMissingPoints(1);
			}
			#endregion

			#region Shift()
			public void Shift(int dR, int dG, int dB)
			{
				for (int y = 0; y < this.colorDeltaArray.GetLength(0); y++)
					for (int x = 0; x < this.colorDeltaArray.GetLength(1); x++)
						this.colorDeltaArray[y, x].Set(this.colorDeltaArray[y, x].R + dR, this.colorDeltaArray[y, x].G + dG, this.colorDeltaArray[y, x].B + dB);
			}
			#endregion

			#region GetPoint()
			public ColorDelta GetPoint(int x, int y)
			{
				return colorDeltaArray[y / 16, x / 16];
			}
			#endregion

			#region GetArray()
			public ColorDelta[,] GetArray()
			{
				int width = this.imageSize.Width;
				int height = this.imageSize.Height;
				ColorDelta[,] colorDelta = new ColorDelta[height, width];
				int r, g, b;

				if (colorDeltaArray[0, 0] != null)
				{
					for (int y = 0; y < colorDeltaArray.GetLength(0) - 1; y++)
					{
						for (int x = 0; x < colorDeltaArray.GetLength(1) - 1; x++)
						{
							ColorDelta ul = colorDeltaArray[y, x];
							ColorDelta ur = colorDeltaArray[y, x + 1];
							ColorDelta ll = colorDeltaArray[y + 1, x];
							ColorDelta lr = colorDeltaArray[y + 1, x + 1];

							for (int ySmall = 0; ySmall < 16; ySmall++)
								for (int xSmall = 0; xSmall < 16; xSmall++)
								{
									r = ((15 - xSmall) * (15 - ySmall) * ul.R +
										xSmall * (15 - ySmall) * ur.R +
										(15 - xSmall) * ySmall * ll.R +
										xSmall * ySmall * lr.R) / 225;
									g = ((15 - xSmall) * (15 - ySmall) * ul.G +
										xSmall * (15 - ySmall) * ur.G +
										(15 - xSmall) * ySmall * ll.G +
										xSmall * ySmall * lr.G) / 225;
									b = ((15 - xSmall) * (15 - ySmall) * ul.B +
										xSmall * (15 - ySmall) * ur.B +
										(15 - xSmall) * ySmall * ll.B +
										xSmall * ySmall * lr.B) / 225;

									colorDelta[(y * 16) + ySmall, (x * 16) + xSmall] = new ColorDelta(r, g, b);
								}
						}
					}

					//lastColumn
					int xFixed = (colorDeltaArray.GetLength(1) - 1) * 16 - 1;
					for (int y = 0; y < colorDeltaArray.GetLength(0) * 16; y++)
						for (int x = xFixed + 1; x < width; x++)
							colorDelta[y, x] = colorDelta[y, xFixed];

					//last row
					int yFixed = (colorDeltaArray.GetLength(0) - 1) * 16 - 1;
					for (int y = yFixed + 1; y < height; y++)
						for (int x = 0; x < width; x++)
							colorDelta[y, x] = colorDelta[yFixed, x];
				}

				return colorDelta;
			}
			#endregion

			#region DrawToFile()
			public void DrawToFile(string filePath)
			{
#if SAVE_RESULTS
				Bitmap result = null;
				BitmapData bitmapData = null;

				try
				{
					result = new Bitmap(colorDeltaArray.GetLength(1) * 16, colorDeltaArray.GetLength(0) * 16);
					bitmapData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

					unsafe
					{
						byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
						byte* pCurrent;

						for (int y = 0; y < colorDeltaArray.GetLength(0) - 1; y++)
						{
							pCurrent = pOrig + y * 16 * bitmapData.Stride;

							for (int x = 0; x < colorDeltaArray.GetLength(1) - 1; x++)
							{
								if (colorDeltaArray[y, x] != null)
								{
									for (int ySmall = 0; ySmall < 16; ySmall++)
										for (int xSmall = 0; xSmall < 16; xSmall++)
										{
											pCurrent[ySmall * bitmapData.Stride + xSmall * 3] = (byte)Math.Max(0, Math.Min(255, (colorDeltaArray[y, x].B + 128)));
											pCurrent[ySmall * bitmapData.Stride + xSmall * 3 + 1] = (byte)Math.Max(0, Math.Min(255, (colorDeltaArray[y, x].G + 128)));
											pCurrent[ySmall * bitmapData.Stride + xSmall * 3 + 2] = (byte)Math.Max(0, Math.Min(255, (colorDeltaArray[y, x].R + 128)));
										}
								}

								pCurrent += 48;
							}
						}
					}
				}
				catch { }
				finally
				{
					if (bitmapData != null)
					{
						result.UnlockBits(bitmapData);
						result.Save(filePath, ImageFormat.Png);
						result.Dispose();
					}
				}
#endif
			}
			#endregion

			//PRIVATE METHODS

			#region SmoothGradient()
			private void SmoothGradient()
			{
				ColorDelta[,] array = new ColorDelta[this.colorDeltaArray.GetLength(0), this.colorDeltaArray.GetLength(1)];

				for (int y = 0; y < array.GetLength(0); y++)
					for (int x = 0; x < array.GetLength(1); x++)
						array[y, x] = this.colorDeltaArray[y, x];

				for (int y = 1; y < array.GetLength(0) - 1; y++)
				{
					for (int x = 1; x < array.GetLength(1) - 1; x++)
					{
						if (array[y, x] != null)
						{
							int weightedSumR = array[y, x].R * 4;
							int weightedSumG = array[y, x].G * 4;
							int weightedSumB = array[y, x].B * 4;
							int sum = 4;

							for (int i = -1; i <= 1; i++)
								for (int j = -1; j <= 1; j++)
								{
									if ( (array[y + i, x + j] != null) && (i != 0 || j != 0) )
									{
										int coefficient = (i == 0 || j == 0) ? 2 : 1;
										weightedSumR += array[y + i, x + j].R * coefficient;
										weightedSumG += array[y + i, x + j].G * coefficient;
										weightedSumB += array[y + i, x + j].B * coefficient;
										sum += coefficient;
									}
								}

							if (sum < 16)
							{
								weightedSumR = array[y, x].R;
								weightedSumG = array[y, x].G;
								weightedSumB = array[y, x].B;
								sum = 1;

								for (int i = -1; i <= 1; i++)
									for (int j = -1; j <= 1; j++)
									{
										if ((array[y + i, x + j] != null) && (i != 0 || j != 0))
										{
											weightedSumR += array[y + i, x + j].R;
											weightedSumG += array[y + i, x + j].G;
											weightedSumB += array[y + i, x + j].B;
											sum ++;
										}
									}
							}

							this.colorDeltaArray[y, x].Set(weightedSumR / sum, weightedSumG / sum, weightedSumB / sum);
						}
					}
				}
			}
			#endregion

			#region AddMissingPoints()
			private void AddMissingPoints(int minValidNeighbours, bool horizontalGradient)
			{
				bool changed;

				do
				{
					changed = false;

					for (int y = 0; y < colorDeltaArray.GetLength(0); y++)
					{
						for (int x = 0; x < colorDeltaArray.GetLength(1); x++)
						{
							if (colorDeltaArray[y, x] == null)
							{
								double r = 0, g = 0, b = 0;
								double distanceRatio = 0;
								double distance;
								ColorDelta colorDelta;
								int validNeighbours = 0;

								if (((colorDelta = GetLeftPoint(x, y, out distance)) != null))
								{
									distance = (horizontalGradient) ? distance * 4 : distance;
									r += colorDelta.R / distance;
									g += colorDelta.G / distance;
									b += colorDelta.B / distance;
									distanceRatio += 1 / distance;
									validNeighbours++;
								}
								if (((colorDelta = GetTopPoint(x, y, out distance)) != null))
								{
									distance = (horizontalGradient == false) ? distance * 4 : distance;
									r += colorDelta.R / distance;
									g += colorDelta.G / distance;
									b += colorDelta.B / distance;
									distanceRatio += 1 / distance;
									validNeighbours++;
								}
								if (((colorDelta = GetRightPoint(x, y, out distance)) != null))
								{
									distance = (horizontalGradient) ? distance * 4 : distance;
									r += colorDelta.R / distance;
									g += colorDelta.G / distance;
									b += colorDelta.B / distance;
									distanceRatio += 1 / distance;
									validNeighbours++;
								}
								if (((colorDelta = GetBottomPoint(x, y, out distance)) != null))
								{
									distance = (horizontalGradient == false) ? distance * 4 : distance;
									r += colorDelta.R / distance;
									g += colorDelta.G / distance;
									b += colorDelta.B / distance;
									distanceRatio += 1 / distance;
									validNeighbours++;
								}

								if (distanceRatio != 0 && validNeighbours >= minValidNeighbours)
								{
									this.Add(x * 16, y * 16, new ColorDelta(Convert.ToInt16(r / distanceRatio), Convert.ToInt16(g / distanceRatio), Convert.ToInt16(b / distanceRatio)));
									changed = true;
								}
							}
						}
					}
				} while (changed);
			}

			private void AddMissingPoints(int minValidNeighbours)
			{
				bool changed;

				do
				{
					changed = false;

					for (int y = 0; y < colorDeltaArray.GetLength(0); y++)
					{
						for (int x = 0; x < colorDeltaArray.GetLength(1); x++)
						{
							if (colorDeltaArray[y, x] == null)
							{
								double r = 0, g = 0, b = 0;
								double distanceRatio = 0;
								double distance;
								ColorDelta colorDelta;
								int validNeighbours = 0;

								if (((colorDelta = GetLeftPoint(x, y, out distance)) != null))
								{
									r += colorDelta.R / distance;
									g += colorDelta.G / distance;
									b += colorDelta.B / distance;
									distanceRatio += 1 / distance;
									validNeighbours++;
								}
								if (((colorDelta = GetTopPoint(x, y, out distance)) != null))
								{
									r += colorDelta.R / distance;
									g += colorDelta.G / distance;
									b += colorDelta.B / distance;
									distanceRatio += 1 / distance;
									validNeighbours++;
								}
								if (((colorDelta = GetRightPoint(x, y, out distance)) != null))
								{
									r += colorDelta.R / distance;
									g += colorDelta.G / distance;
									b += colorDelta.B / distance;
									distanceRatio += 1 / distance;
									validNeighbours++;
								}
								if (((colorDelta = GetBottomPoint(x, y, out distance)) != null))
								{
									r += colorDelta.R / distance;
									g += colorDelta.G / distance;
									b += colorDelta.B / distance;
									distanceRatio += 1 / distance;
									validNeighbours++;
								}

								if (distanceRatio != 0 && validNeighbours >= minValidNeighbours)
								{
									this.Add(x * 16, y * 16, new ColorDelta(Convert.ToInt16(r / distanceRatio), Convert.ToInt16(g / distanceRatio), Convert.ToInt16(b / distanceRatio)));
									changed = true;
								}
							}
						}
					}
				} while (changed);
			}
			#endregion

			#region GetLeftPoint()
			private ColorDelta GetLeftPoint(int x, int y, out double distance)
			{
				for (int i = x - 1; i >= 0; i--)
					if (colorDeltaArray[y, i] != null)
					{
						distance = x - i;
						return colorDeltaArray[y, i];

						/*int r = colorDeltaArray[y, i].R;
						int g = colorDeltaArray[y, i].G;
						int b = colorDeltaArray[y, i].B;
						
						int iterations = 1;
						for(int j = i-1; j >= 0 && j > i - 3; j--)
						{
							if (colorDeltaArray[y, j] != null)
							{
								r += colorDeltaArray[y, j].R;
								g += colorDeltaArray[y, j].G;
								b += colorDeltaArray[y, j].B;
								iterations++;
							}
						}

						r /= iterations;
						g /= iterations;
						b /= iterations;

						return new ColorDelta(r,g,b);*/
					}

				distance = 0;
				return null;
			}
			#endregion

			#region GetRightPoint()
			private ColorDelta GetRightPoint(int x, int y, out double distance)
			{
				for (int i = x + 1; i < colorDeltaArray.GetLength(1); i++)
					if (colorDeltaArray[y, i] != null)
					{
						distance = i - x;
						return colorDeltaArray[y, i];

						/*int r = colorDeltaArray[y, i].R;
						int g = colorDeltaArray[y, i].G;
						int b = colorDeltaArray[y, i].B;

						int iterations = 1;
						for (int j = i + 1; j < colorDeltaArray.GetLength(1) && j < i + 3; j++)
						{
							if (colorDeltaArray[y, j] != null)
							{
								r += colorDeltaArray[y, j].R;
								g += colorDeltaArray[y, j].G;
								b += colorDeltaArray[y, j].B;
								iterations++;
							}
						}

						r /= iterations;
						g /= iterations;
						b /= iterations;

						return new ColorDelta(r, g, b);*/
					}

				distance = 0;
				return null;
			}
			#endregion

			#region GetTopPoint()
			private ColorDelta GetTopPoint(int x, int y, out double distance)
			{
				for (int i = y - 1; i >= 0; i--)
					if (colorDeltaArray[i, x] != null)
					{
						distance = y - i;
						return colorDeltaArray[i, x];

						/*int r = colorDeltaArray[i, x].R;
						int g = colorDeltaArray[i, x].G;
						int b = colorDeltaArray[i, x].B;

						int iterations = 1;
						for (int j = i - 1; j >= 0 && j > i - 3; j--)
						{
							if (colorDeltaArray[j, x] != null)
							{
								r += colorDeltaArray[j, x].R;
								g += colorDeltaArray[j, x].G;
								b += colorDeltaArray[j, x].B;
								iterations++;
							}
						}

						r /= iterations;
						g /= iterations;
						b /= iterations;

						return new ColorDelta(r, g, b);*/
					}

				distance = 0;
				return null;
			}
			#endregion

			#region GetBottomPoint()
			private ColorDelta GetBottomPoint(int x, int y, out double distance)
			{
				for (int i = y + 1; i < colorDeltaArray.GetLength(0); i++)
					if (colorDeltaArray[i, x] != null)
					{
						distance = i - y;
						return colorDeltaArray[i, x];

						/*int r = colorDeltaArray[i,x].R;
						int g = colorDeltaArray[i, x].G;
						int b = colorDeltaArray[i, x].B;

						int iterations = 1;
						for (int j = i + 1; j < colorDeltaArray.GetLength(0) && j < i + 3; j++)
						{
							if (colorDeltaArray[j, x] != null)
							{
								r += colorDeltaArray[j, x].R;
								g += colorDeltaArray[j, x].G;
								b += colorDeltaArray[j, x].B;
								iterations++;
							}
						}

						r /= iterations;
						g /= iterations;
						b /= iterations;

						return new ColorDelta(r, g, b);*/
					}

				distance = 0;
				return null;
			}
			#endregion

			#region IsGradientHorizontal()
			/// <summary>
			/// Returns if the book was scanned normaly(horizontal) or 90 or 270 degrees rotated (vertical)
			/// </summary>
			/// <returns></returns>
			private bool IsGradientHorizontal()
			{
				int sumH = 0, sumV = 0;
				double countH = 0, countV = 0;
				
				for (int y = 0; y < colorDeltaArray.GetLength(0) - 1; y++)
					for (int x = 0; x < colorDeltaArray.GetLength(1) - 1; x++)
						if (colorDeltaArray[y, x] != null)
						{
							if (colorDeltaArray[y, x + 1] != null)
							{
								sumH += Math.Abs(colorDeltaArray[y, x + 1].R - colorDeltaArray[y, x].R);
								countH++;
							}
							if (colorDeltaArray[y + 1, x] != null)
							{
								sumV += Math.Abs(colorDeltaArray[y + 1, x].R - colorDeltaArray[y, x].R);
								countV++;
							}
						}

				if (countH > 0 && countV > 0)
					return ((sumH / countH) < (sumV / countV));
				else
					return false;
			}
			#endregion

			#region Validate()
			/// <summary>
			/// Call to get rid of points, that don't belong to the horizontal or vertical line
			/// </summary>
			/// <param name="horizontalGradient"></param>
			/// <returns></returns>
			private void Validate(bool horizontalGradient, int tolerance)
			{
				List<int> listRG = new List<int>();
				List<int> listRB = new List<int>();
				List<int> listGB = new List<int>();
				double rg, rb, gb;

				if (horizontalGradient)
				{

					for (int y = 0; y < colorDeltaArray.GetLength(0); y++)
					{
						for (int x = 0; x < colorDeltaArray.GetLength(1); x++)
						{
							if (colorDeltaArray[y, x] != null)
							{
								listRG.Add(colorDeltaArray[y, x].R - colorDeltaArray[y, x].G);
								listRB.Add(colorDeltaArray[y, x].R - colorDeltaArray[y, x].B);
								listGB.Add(colorDeltaArray[y, x].G - colorDeltaArray[y, x].B);
							}
						}

						if (listRG.Count > 0)
						{
							listRG.Sort();
							listRB.Sort();
							listGB.Sort();
							rg = listRG[listRG.Count / 2];
							rb = listRB[listRB.Count / 2];
							gb = listGB[listGB.Count / 2];

							for (int x = 0; x < colorDeltaArray.GetLength(1); x++)
							{
								if (colorDeltaArray[y, x] != null)
								{
									if ((colorDeltaArray[y, x].R - colorDeltaArray[y, x].G < rg - tolerance) ||
										(colorDeltaArray[y, x].R - colorDeltaArray[y, x].G > rg + tolerance) ||
										(colorDeltaArray[y, x].R - colorDeltaArray[y, x].B < rb - tolerance) ||
										(colorDeltaArray[y, x].R - colorDeltaArray[y, x].B > rb + tolerance) ||
										(colorDeltaArray[y, x].G - colorDeltaArray[y, x].B < gb - tolerance) ||
										(colorDeltaArray[y, x].G - colorDeltaArray[y, x].B > gb + tolerance))
										colorDeltaArray[y, x] = null;
								}
							}
						}

						listRG.Clear();
						listRB.Clear();
						listGB.Clear();
					}
				}
				else
				{
					for (int x = 0; x < colorDeltaArray.GetLength(1); x++)
					{
						for (int y = 0; y < colorDeltaArray.GetLength(0); y++)
						{
							if (colorDeltaArray[y, x] != null)
							{
								listRG.Add(colorDeltaArray[y, x].R - colorDeltaArray[y, x].G);
								listRB.Add(colorDeltaArray[y, x].R - colorDeltaArray[y, x].B);
								listGB.Add(colorDeltaArray[y, x].G - colorDeltaArray[y, x].B);
							}
						}

						if (listRG.Count > 0)
						{
							listRG.Sort();
							listRB.Sort();
							listGB.Sort();
							rg = listRG[listRG.Count / 2];
							rb = listRB[listRB.Count / 2];
							gb = listGB[listGB.Count / 2];

							for (int y = 0; y < colorDeltaArray.GetLength(0); y++)
							{
								if (colorDeltaArray[y, x] != null)
								{
									if ((colorDeltaArray[y, x].R - colorDeltaArray[y, x].G < rg - tolerance) ||
										(colorDeltaArray[y, x].R - colorDeltaArray[y, x].G > rg + tolerance) ||
										(colorDeltaArray[y, x].R - colorDeltaArray[y, x].B < rb - tolerance) ||
										(colorDeltaArray[y, x].R - colorDeltaArray[y, x].B > rb + tolerance) ||
										(colorDeltaArray[y, x].G - colorDeltaArray[y, x].B < gb - tolerance) ||
										(colorDeltaArray[y, x].G - colorDeltaArray[y, x].B > gb + tolerance))
										colorDeltaArray[y, x] = null;
								}
							}
						}

						listRG.Clear();
						listRB.Clear();
						listGB.Clear();
					}
				}
			}
			#endregion

		}
		#endregion


		//PUBLIC METHODS
		#region public methods

		#region AnalyzeImage()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="background"></param>
		/// <param name="rgbBackground">True if to get gratient based on all r,g,b components. False to analyse based on red only </param>
		/// <returns></returns>
		public static ColorDelta[,] AnalyzeImage(Bitmap bitmap, Color background, Color threshold, byte backgroundSpread, Gradient.PageOrientation pageOrientation)
		{
			ColorDelta[,] gradient;
			
			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format1bppIndexed: gradient = null; break;
				case PixelFormat.Format24bppRgb: gradient = AnalyzeImage24bpp(bitmap, background, threshold, backgroundSpread, pageOrientation); break;
				case PixelFormat.Format8bppIndexed: gradient = AnalyzeImage8bpp(bitmap, background.R, threshold.R, pageOrientation); break;
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:
					gradient = AnalyzeImage32bpp(bitmap, background, threshold, backgroundSpread, pageOrientation);
					break;
				default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}
			
			return gradient;
		}
		#endregion

		#region AnalyzeSheet()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="background"></param>
		/// <param name="rgbBackground">True if to get gratient based on all r,g,b components. False to analyse based on red only </param>
		/// <returns></returns>
		public static ColorDelta[,] AnalyzeSheet(Bitmap bitmap, Color background, bool rgbBackground)
		{
			ColorDelta[,] gradient;

			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format24bppRgb:
					if (rgbBackground)
						gradient = AnalyzeSheet24bpp(bitmap, background);
					else
						gradient = AnalyzeSheet24bpp(bitmap, background.R);
					break;
				case PixelFormat.Format8bppIndexed: gradient = AnalyzeSheet8bpp(bitmap, background.R); break;
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:
					if (rgbBackground)
						gradient = AnalyzeSheet32bpp(bitmap, background);
					else
						gradient = AnalyzeSheet32bpp(bitmap, background.R); 
					break;
				default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}
			
			return gradient;
		}
		#endregion

		#region Fix()
		public static void Fix(Bitmap bitmap, ColorDelta[,] gradient, Color threshold, int brightnessDelta)
		{			
			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format1bppIndexed: break;
				case PixelFormat.Format24bppRgb: Fix24bpp(bitmap, gradient, threshold, brightnessDelta); break;
				case PixelFormat.Format8bppIndexed: Fix8bpp(bitmap, gradient, threshold.R, brightnessDelta); break;
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:
					Fix32bpp(bitmap, gradient, threshold, brightnessDelta); break;
				default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}
		}
		#endregion

		#region SmoothVertically()
		/// <summary>
		/// good for images of curved books, as the background lightness is higher on bulky part and lower close to edges
		/// </summary>
		/// <param name="bitmap">grayscale image</param>
		public static void SmoothVertically(Bitmap bitmap)
		{
			if (bitmap == null)
				throw new IpException(ErrorCode.ErrorNoImageLoaded);


#if DEBUG
			DateTime start = DateTime.Now;
#endif

			uint[][] histograms = GetLineHistograms(bitmap);
			uint[] thresholds = GetThresholds(histograms);
			uint[] averages = GetMedians(bitmap, thresholds);
			double average = 0;

			//get average
			for (int x = 0; x < averages.Length; x++)
				average += averages[x] * averages[x];

			average = average / averages.Length;
			average = Math.Sqrt(average);

			FixImageLightDistribution(bitmap, thresholds, averages, average);

#if DEBUG
			Console.WriteLine("Smoothing, SmoothVertically(): " + DateTime.Now.Subtract(start).ToString());
#endif

#if SAVE_RESULTS
			bitmap.Save(Debug.SaveToDir + @"02 Vertical Smoothing.png", ImageFormat.Png);
#endif
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region AnalyzeSheet32bpp()
		private static ColorDelta[,] AnalyzeSheet32bpp(Bitmap bitmap, Color background)
		{
			BitmapData bitmapData = null;
			ColorDelta[,] gradient = new ColorDelta[bitmap.Height, bitmap.Width];

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				int stride = bitmapData.Stride;
				int width = bitmap.Width;
				int height = bitmap.Height;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;
					int r,g,b;

					for (int y = 5; y < height - 10; y++)
					{
						pCurrent = pSource + y * stride + 20;

						for (int x = 5; x < width - 10; x++)
						{
							r = 0; g = 0; b = 0;
							for (int i = -5; i <= 5; i++)
							{
								b += pCurrent[-20 + i * stride] + pCurrent[-16 + i * stride] + pCurrent[-12 + i * stride] + pCurrent[-8 + i * stride] + pCurrent[-4 + i * stride] + pCurrent[0 + i * stride] + pCurrent[4 + i * stride] + pCurrent[8 + i * stride] + pCurrent[12 + i * stride] + pCurrent[16 + i * stride] + pCurrent[20 + i * stride];
								g += pCurrent[-19 + i * stride] + pCurrent[-15 + i * stride] + pCurrent[-11 + i * stride] + pCurrent[-7 + i * stride] + pCurrent[-3 + i * stride] + pCurrent[1 + i * stride] + pCurrent[5 + i * stride] + pCurrent[9 + i * stride] + pCurrent[13 + i * stride] + pCurrent[17 + i * stride] + pCurrent[21 + i * stride];
								r += pCurrent[-18 + i * stride] + pCurrent[-14 + i * stride] + pCurrent[-10 + i * stride] + pCurrent[-6 + i * stride] + pCurrent[-2 + i * stride] + pCurrent[2 + i * stride] + pCurrent[6 + i * stride] + pCurrent[10 + i * stride] + pCurrent[14 + i * stride] + pCurrent[18 + i * stride] + pCurrent[22 + i * stride];
							}

							r = background.R - r / 121;
							g = background.G - g / 121;
							b = background.B - b / 121;
							gradient[y, x].Set(r, g, b);
							pCurrent += 4;
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			return gradient;
		}

		private static ColorDelta[,] AnalyzeSheet32bpp(Bitmap bitmap, byte backgroundR)
		{
			BitmapData bitmapData = null;
			ColorDelta[,] gradient = new ColorDelta[bitmap.Height, bitmap.Width];

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				int stride = bitmapData.Stride;
				int width = bitmap.Width;
				int height = bitmap.Height;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;
					int r;

					for (int y = 5; y < height - 10; y++)
					{
						pCurrent = pSource + y * stride + 20;

						for (int x = 5; x < width - 10; x++)
						{
							r = 0;
							for (int i = -5; i <= 5; i++)
								r += pCurrent[-18 + i * stride] + pCurrent[-14 + i * stride] + pCurrent[-10 + i * stride] + pCurrent[-6 + i * stride] + pCurrent[-2 + i * stride] + pCurrent[2 + i * stride] + pCurrent[6 + i * stride] + pCurrent[10 + i * stride] + pCurrent[14 + i * stride] + pCurrent[18 + i * stride] + pCurrent[22 + i * stride];

							r = backgroundR - r / 121;
							gradient[y, x].Set(r, r, r);
							pCurrent += 4;
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			return gradient;
		}
		#endregion

		#region AnalyzeSheet24bpp()
		private static ColorDelta[,] AnalyzeSheet24bpp(Bitmap bitmap, Color background)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			BitmapData bitmapData = null;
			ColorDelta[,] gradient = new ColorDelta[bitmap.Height, bitmap.Width];

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				int stride = bitmapData.Stride;
				int width = bitmap.Width;
				int height = bitmap.Height;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;
					int r, g, b;

					for (int y = 10; y < height - 10; y = y + 2)
					{
						pCurrent = pSource + y * stride + 30;

						for (int x = 10; x < width - 10; x = x + 2)
						{
							r = 0; g = 0; b = 0;
							for (int i = -10; i <= 10; i++)
							{
								for (int j = -10; j <= 10; j++)
								{
									b += pCurrent[j * 3 + i * stride];
									g += pCurrent[j * 3 + 1 + i * stride];
									r += pCurrent[j * 3 + 2 + i * stride];
								}
							}

							r = background.R - (r / 441);
							g = background.G - g / 441;
							b = background.B - b / 441;
							gradient[y, x].Set(r, g, b);
							gradient[y, x+1].Set(r, g, b);
							gradient[y+1, x].Set(r, g, b);
							gradient[y+1, x + 1].Set(r, g, b);
							pCurrent += 6;
						}
					}

					//top
					for (int y = 0; y < 10; y++)
					{
						int minY = 0;
						int maxY = y + 10;

						for (int x = 0; x < width; x++)
						{
							r = 0; g = 0; b = 0;
							int minX = Math.Max(0, x - 10);
							int maxX = Math.Min(width - 1, x + 10);
							int iterations =  (maxX - minX+1) * (maxY - minY+1);

							for (int i = minY; i <= maxY; i++)
								for (int j = minX; j <= maxX; j++)
								{
									b += pSource[j * 3 + i * stride];
									g += pSource[j * 3 + 1 + i * stride];
									r += pSource[j * 3 + 2 + i * stride];
								}

							gradient[y, x].Set(background.R - r / iterations, background.G - g / iterations, background.B - b / iterations);
						}
					}
					//bottom
					for (int y = height - 10; y < height; y++)
					{
						int minY = y - 10;
						int maxY = height - 1;

						for (int x = 0; x < width; x++)
						{
							r = 0; g = 0; b = 0;
							int minX = Math.Max(0, x - 10);
							int maxX = Math.Min(width - 1, x + 10);
							int iterations = (maxX - minX + 1) * (maxY - minY + 1);

							for (int i = minY; i <= maxY; i++)
								for (int j = minX; j <= maxX; j++)
								{
									b += pSource[j * 3 + i * stride];
									g += pSource[j * 3 + 1 + i * stride];
									r += pSource[j * 3 + 2 + i * stride];
								}

							gradient[y, x].Set(background.R - r / iterations, background.G - g / iterations, background.B - b / iterations);
						}
					}
					//left
					for (int x = 0; x < 10; x++)
					{
						int minX = 0;
						int maxX = x + 10;
						
						for (int y = 0; y < height; y++)
						{
							r = 0; g = 0; b = 0;
							int minY = Math.Max(0, y - 10);
							int maxY = Math.Min(height - 1, y + 10);
							int iterations = (maxX - minX + 1) * (maxY - minY + 1);

							for (int i = minY; i <= maxY; i++)
								for (int j = minX; j <= maxX; j++)
								{
									b += pSource[j * 3 + i * stride];
									g += pSource[j * 3 + 1 + i * stride];
									r += pSource[j * 3 + 2 + i * stride];
								}

							gradient[y, x].Set(background.R - r / iterations, background.G - g / iterations, background.B - b / iterations);
						}
					}
					//right
					for (int x = width - 10; x < width; x++)
					{
						int minX = width - 10;
						int maxX = width - 1;

						for (int y = 0; y < height; y++)
						{
							r = 0; g = 0; b = 0;
							int minY = Math.Max(0, y - 10);
							int maxY = Math.Min(height - 1, y + 10);
							int iterations = (maxX - minX + 1) * (maxY - minY + 1);

							for (int i = minY; i <= maxY; i++)
								for (int j = minX; j <= maxX; j++)
								{
									b += pSource[j * 3 + i * stride];
									g += pSource[j * 3 + 1 + i * stride];
									r += pSource[j * 3 + 2 + i * stride];
								}

							gradient[y, x].Set(background.R - r / iterations, background.G - g / iterations, background.B - b / iterations);
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

#if DEBUG
			Console.WriteLine(DateTime.Now.Subtract(start).ToString());
#endif
			return gradient;
		}

		private static ColorDelta[,] AnalyzeSheet24bpp(Bitmap bitmap, byte backgroundR)
		{
			BitmapData bitmapData = null;
			ColorDelta[,] gradient = new ColorDelta[bitmap.Height, bitmap.Width];

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				int stride = bitmapData.Stride;
				int width = bitmap.Width;
				int height = bitmap.Height;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;
					int r;

					for (int y = 5; y < height - 10; y++)
					{
						pCurrent = pSource + y * stride + 15;

						for (int x = 5; x < width - 10; x++)
						{
							r = 0;
							for (int i = -5; i <= 5; i++)
								r += pCurrent[-13 + i * stride] + pCurrent[-10 + i * stride] + pCurrent[-7 + i * stride] + pCurrent[-4 + i * stride] + pCurrent[-1 + i * stride] + pCurrent[2 + i * stride] + pCurrent[5 + i * stride] + pCurrent[8 + i * stride] + pCurrent[11 + i * stride] + pCurrent[14 + i * stride] + pCurrent[17 + i * stride];

							r = backgroundR - (r / 121);
							gradient[y, x].Set(r,r,r);
							pCurrent += 3;
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			return gradient;
		}
		#endregion

		#region AnalyzeSheet8bpp()
		private static ColorDelta[,] AnalyzeSheet8bpp(Bitmap bitmap, byte background)
		{
			BitmapData	bitmapData = null;
			ColorDelta[,] gradient = new ColorDelta[bitmap.Height, bitmap.Width];
			
			try
			{			
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				
				int		stride = bitmapData.Stride;
				int		width = bitmap.Width;
				int		height = bitmap.Height;

				unsafe
				{
					byte*	pSource = (byte*) bitmapData.Scan0.ToPointer();
					byte*	pCurrent;
					int		g;

					for (int y = 5; y < height - 10; y++)
					{
						pCurrent = pSource + y * stride + 5;

						for (int x = 5; x < width - 10; x++)
						{
							g = 0;
							for (int i = -5; i <= 5; i++)
								g += pCurrent[-5 + i * stride] + pCurrent[-4 + i * stride] + pCurrent[-3 + i * stride] + pCurrent[-2 + i * stride] + pCurrent[-1 + i * stride] + pCurrent[0 + i * stride] + pCurrent[1 + i * stride] + pCurrent[2 + i * stride] + pCurrent[3 + i * stride] + pCurrent[4 + i * stride] + pCurrent[5 + i * stride];

							g = background - g / 121;
							gradient[y, x].Set(g, g,g);
							pCurrent++;
						}
					}
				}
			}
			finally
			{
				if(bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			return gradient;
		}
		#endregion

		#region AnalyzeImage32bpp()
		private static ColorDelta[,] AnalyzeImage32bpp(Bitmap bitmap, Color background, Color threshold, int backgroundSpread, Gradient.PageOrientation pageOrientation)
		{
			BitmapData bitmapData = null;
			Bitmap edBitmap = null;
			BitmapData edData = null;
			Gradient gradient = new Gradient(bitmap.Size, pageOrientation);
			int x, y;

			try
			{
				edBitmap = ImageProcessing.ImagePreprocessing.Go(bitmap, Rectangle.Empty, 0, 50, true, false);
				ImageProcessing.ImagePreprocessing.GetRidOfBorders(edBitmap);
				Symbols symbols = ObjectLocator.FindObjects(edBitmap, Rectangle.Empty);
				MergeObjectsNestedInPictures(symbols);
				symbols.Despeckle(7, 7);
#if SAVE_RESULTS
				symbols.DrawToFile(Debug.SaveToDir + @"\03 Symbols.png", bitmap.Size);
#endif

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				edData = edBitmap.LockBits(new Rectangle(0, 0, edBitmap.Width, edBitmap.Height), ImageLockMode.ReadOnly, edBitmap.PixelFormat);

				int stride = bitmapData.Stride;
				int strideEd = edData.Stride;
				int width = bitmap.Width;
				int height = bitmap.Height;

				int backgroundDifferenceGB = (background.G - background.B);
				int backgroundDifferenceRG = (background.R - background.G);
				
				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pEdSource = (byte*)edData.Scan0.ToPointer();
					byte* pCurrent;
					int b, g, r;

					for (y = 16; y < height - 16; y = y + 16)
					{
						pCurrent = pSource + y * stride + 64;

						for (x = 16; x < width - 16; x = x + 16)
						{
							if (RasterProcessing.PercentageWhite(pEdSource, strideEd, x - 16, y - 16, 32, 32) == 0)
							{
								if (IsBackgroundArea32bpp(pCurrent, stride, threshold))
								{
									if (AreaIsNotInsideSymbol(symbols, x, y))
									{
										b = GetAreaAverage32bpp(pCurrent, stride);
										g = GetAreaAverage32bpp(pCurrent + 1, stride);
										r = GetAreaAverage32bpp(pCurrent + 2, stride);

										int difference = (g - b) - (r - g);
										if (((g - b) > backgroundDifferenceGB - backgroundSpread) && ((g - b) < backgroundDifferenceGB + backgroundSpread) &&
											((r - g) > backgroundDifferenceRG - backgroundSpread) && ((r - g) < backgroundDifferenceRG + backgroundSpread))
											gradient.Add(x, y, new ColorDelta(background.R - r, background.G - g, background.B - b));
									}
								}
							}
							pCurrent += 64;
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
				if (edData != null)
					edBitmap.UnlockBits(edData);
				if (edBitmap != null)
					edBitmap.Dispose();
			}

#if SAVE_RESULTS
			gradient.DrawToFile(Debug.SaveToDir + @"\gradient1.png");
#endif
			gradient.Finish();
#if SAVE_RESULTS
			gradient.DrawToFile(Debug.SaveToDir + @"\gradient2.png");
#endif
			return gradient.GetArray();
		}
		#endregion

		#region AnalyzeImage24bpp()
		private static ColorDelta[,] AnalyzeImage24bpp(Bitmap bitmap, Color background, Color threshold, int backgroundSpread, Gradient.PageOrientation pageOrientation)
		{
			BitmapData bitmapData = null;
			Bitmap edBitmap = null;
			BitmapData edData = null;
			Gradient gradient = new Gradient(bitmap.Size, pageOrientation);
			int x, y;

			try
			{
				edBitmap = ImageProcessing.ImagePreprocessing.Go(bitmap, Rectangle.Empty, 0, 50, true, false);
				NoiseReduction.Despeckle(edBitmap, NoiseReduction.DespeckleSize.Size4x4, NoiseReduction.DespeckleMode.WhiteSpecklesOnly, NoiseReduction.DespeckleMethod.Regions);
				ImageProcessing.ImagePreprocessing.GetRidOfBorders(edBitmap);
				Symbols symbols = ObjectLocator.FindObjects(edBitmap, Rectangle.Empty);
				MergeObjectsNestedInPictures(symbols);
				symbols.Despeckle(7, 7);

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				edData = edBitmap.LockBits(new Rectangle(0, 0, edBitmap.Width, edBitmap.Height), ImageLockMode.ReadOnly, edBitmap.PixelFormat);

				int stride = bitmapData.Stride;
				int strideEd = edData.Stride;
				int width = bitmap.Width;
				int height = bitmap.Height;

				int backgroundDifferenceGB = (background.G - background.B);
				int backgroundDifferenceRG = (background.R - background.G);
				
				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pEdSource = (byte*)edData.Scan0.ToPointer();
					byte* pCurrent;
					int b, g, r;

					for (y = 16; y < height - 16; y = y + 16)
					{
						pCurrent = pSource + y * stride + 48;

						for (x = 16; x < width - 16; x = x + 16)
						{
							if (RasterProcessing.PercentageWhite(pEdSource, strideEd, x - 16, y - 16, 32, 32) == 0)
							{
								if (IsBackgroundArea24bpp(pCurrent, stride, threshold))
								{
									if (AreaIsNotInsideSymbol(symbols, x, y))
									{
										b = GetAreaAverage24bpp(pCurrent, stride);
										g = GetAreaAverage24bpp(pCurrent + 1, stride);
										r = GetAreaAverage24bpp(pCurrent + 2, stride);

										int difference = (g - b) - (r - g);
										if (((g - b) > backgroundDifferenceGB - backgroundSpread) && ((g - b) < backgroundDifferenceGB + backgroundSpread) &&
											((r - g) > backgroundDifferenceRG - backgroundSpread) && ((r - g) < backgroundDifferenceRG + backgroundSpread))
											gradient.Add(x, y, new ColorDelta(background.R - r, background.G - g, background.B - b));
									}
								}
							}
							pCurrent += 48;
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
				if (edData != null)
					edBitmap.UnlockBits(edData);
				if (edBitmap != null)
					edBitmap.Dispose();
			}

#if SAVE_RESULTS
			gradient.DrawToFile(Debug.SaveToDir + @"\gradient1.png");
#endif
			gradient.Finish();
#if SAVE_RESULTS
			gradient.DrawToFile(Debug.SaveToDir + @"\gradient3.png");
#endif
			return gradient.GetArray();
		}
		#endregion

		#region AnalyzeImage8bpp()
		private static ColorDelta[,] AnalyzeImage8bpp(Bitmap bitmap, byte background, byte threshold, Gradient.PageOrientation pageOrientation)
		{
			BitmapData bitmapData = null;
			Bitmap edBitmap = null;
			BitmapData edData = null;
			Gradient gradient = new Gradient(bitmap.Size, pageOrientation);
			int x, y;

			try
			{
				edBitmap = ImageProcessing.ImagePreprocessing.Go(bitmap, Rectangle.Empty, 0, 50, true, false);
				NoiseReduction.Despeckle(edBitmap, NoiseReduction.DespeckleSize.Size4x4, NoiseReduction.DespeckleMode.WhiteSpecklesOnly, NoiseReduction.DespeckleMethod.Regions);
				ImageProcessing.ImagePreprocessing.GetRidOfBorders(edBitmap);
				Symbols symbols = ObjectLocator.FindObjects(edBitmap, Rectangle.Empty);
				MergeObjectsNestedInPictures(symbols);
				symbols.Despeckle(7, 7);

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				edData = edBitmap.LockBits(new Rectangle(0, 0, edBitmap.Width, edBitmap.Height), ImageLockMode.ReadOnly, edBitmap.PixelFormat);

				int stride = bitmapData.Stride;
				int strideEd = edData.Stride;
				int width = bitmap.Width;
				int height = bitmap.Height;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pEdSource = (byte*)edData.Scan0.ToPointer();
					byte* pCurrent;
					int g;

					for (y = 16; y < height - 16; y = y + 16)
					{
						pCurrent = pSource + y * stride + 16;

						for (x = 16; x < width - 16; x = x + 16)
						{
							if (RasterProcessing.PercentageWhite(pEdSource, strideEd, x - 16, y - 16, 32, 32) == 0)
							{
								if (IsBackgroundArea8bpp(pCurrent, stride, threshold))
								{
									if (AreaIsNotInsideSymbol(symbols, x, y))
									{
										g = GetAreaAverage8bpp(pCurrent, stride);

										gradient.Add(x, y, new ColorDelta(background - g, background - g, background - g));
									}
								}
							}
							pCurrent += 16;
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
				if (edData != null)
					edBitmap.UnlockBits(edData);
				if (edBitmap != null)
					edBitmap.Dispose();
			}

#if SAVE_RESULTS
			gradient.DrawToFile(Debug.SaveToDir + @"\gradient1.png");
#endif
			gradient.Finish();
#if SAVE_RESULTS
			gradient.DrawToFile(Debug.SaveToDir + @"\gradient2.png");
#endif
			return gradient.GetArray();
		}
		#endregion

		#region Fix32bpp()
		private static void Fix32bpp(Bitmap bitmap, ColorDelta[,] gradient, Color threshold, int brightnessDelta)
		{
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;
				int width = (bitmap.Width <= gradient.GetLength(1)) ? bitmap.Width : gradient.GetLength(1);
				int height = (bitmap.Height <= gradient.GetLength(0)) ? bitmap.Height : gradient.GetLength(0);

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					for (int y = 0; y < height; y++)
					{
						pCurrent = pSource + y * stride;

						for (int x = 0; x < width; x++)
						{
							if (*pCurrent > threshold.B || pCurrent[1] > threshold.G || pCurrent[2] > threshold.R)
							{
								*pCurrent = (byte)Math.Max(0, Math.Min(255, *pCurrent + gradient[y, x].B + brightnessDelta));
								pCurrent[1] = (byte)Math.Max(0, Math.Min(255, pCurrent[1] + gradient[y, x].G + brightnessDelta));
								pCurrent[2] = (byte)Math.Max(0, Math.Min(255, pCurrent[2] + gradient[y, x].R + brightnessDelta));
							}
							else
							{
								*pCurrent = (byte)Math.Max(0, Math.Min(255, *pCurrent + gradient[y, x].B * (*pCurrent / (float)threshold.B) + brightnessDelta));
								pCurrent[1] = (byte)Math.Max(0, Math.Min(255, pCurrent[1] + gradient[y, x].G * (pCurrent[1] / (float)threshold.G) + brightnessDelta));
								pCurrent[2] = (byte)Math.Max(0, Math.Min(255, pCurrent[2] + gradient[y, x].R * (pCurrent[2] / (float)threshold.R) + brightnessDelta));
							}

							pCurrent += 4;
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

		#region Fix24bpp()
		private static void Fix24bpp(Bitmap bitmap, ColorDelta[,] gradient, Color threshold, int brightnessDelta)
		{
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;
				int width = (bitmap.Width <= gradient.GetLength(1)) ? bitmap.Width : gradient.GetLength(1);
				int height = (bitmap.Height <= gradient.GetLength(0)) ? bitmap.Height : gradient.GetLength(0);

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					for (int y = 0; y < height; y++)
					{
						pCurrent = pSource + y * stride;

						for (int x = 0; x < width; x++)
						{
							/*if (*pCurrent > threshold.B || pCurrent[1] > threshold.G || pCurrent[2] > threshold.R)
							{
								*pCurrent = (byte)Math.Max(0, Math.Min(255, *pCurrent + gradient[y, x].B + brightnessDelta));
								pCurrent[1] = (byte)Math.Max(0, Math.Min(255, pCurrent[1] + gradient[y, x].G + brightnessDelta));
								pCurrent[2] = (byte)Math.Max(0, Math.Min(255, pCurrent[2] + gradient[y, x].R + brightnessDelta));
							}
							else
							{
								*pCurrent = (byte)Math.Max(0, Math.Min(255, *pCurrent + gradient[y, x].B * (*pCurrent / (float)threshold.B) + brightnessDelta));
								pCurrent[1] = (byte)Math.Max(0, Math.Min(255, pCurrent[1] + gradient[y, x].G * (pCurrent[1] / (float)threshold.G) + brightnessDelta));
								pCurrent[2] = (byte)Math.Max(0, Math.Min(255, pCurrent[2] + gradient[y, x].R * (pCurrent[2] / (float)threshold.R) + brightnessDelta));
							}*/
							if (*pCurrent > threshold.B)
								*pCurrent = (byte)Math.Max(0, Math.Min(255, *pCurrent + gradient[y, x].B + brightnessDelta));
							else
								*pCurrent = (byte)Math.Max(0, Math.Min(255, *pCurrent + gradient[y, x].B * (*pCurrent / (float)threshold.B) + brightnessDelta));
							
							if (pCurrent[1] > threshold.G)
								pCurrent[1] = (byte)Math.Max(0, Math.Min(255, pCurrent[1] + gradient[y, x].G + brightnessDelta));
							else
								pCurrent[1] = (byte)Math.Max(0, Math.Min(255, pCurrent[1] + gradient[y, x].G * (pCurrent[1] / (float)threshold.G) + brightnessDelta));
							
							if (pCurrent[2] > threshold.R)
								pCurrent[2] = (byte)Math.Max(0, Math.Min(255, pCurrent[2] + gradient[y, x].R + brightnessDelta));
							else
								pCurrent[2] = (byte)Math.Max(0, Math.Min(255, pCurrent[2] + gradient[y, x].R * (pCurrent[2] / (float)threshold.R) + brightnessDelta));
							
							pCurrent += 3;
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

		#region Fix8bpp()
		private static void Fix8bpp(Bitmap bitmap, ColorDelta[,] gradient, byte threshold, int brightnessDelta)
		{
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;
				int width = (bitmap.Width <= gradient.GetLength(1)) ? bitmap.Width : gradient.GetLength(1);
				int height = (bitmap.Height <= gradient.GetLength(0)) ? bitmap.Height : gradient.GetLength(0);

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					for (int y = 0; y < height; y++)
					{
						pCurrent = pSource + y * stride;

						for (int x = 0; x < width; x++)
						{
							if (*pCurrent > threshold)
							{
								*pCurrent = (byte)Math.Max(0, Math.Min(255, *pCurrent + gradient[y, x].B + brightnessDelta));
							}
							else
							{
								*pCurrent = (byte)Math.Max(0, Math.Min(255, *pCurrent + gradient[y, x].B * (*pCurrent / (float)threshold) + brightnessDelta));
							}

							pCurrent ++;
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

		#region IsBackgroundArea32bpp()
		private unsafe static bool IsBackgroundArea32bpp(byte* pCurrent, int stride, Color threshold)
		{
			for (int y = -16; y <= 16; y = y + 4)
				for (int x = -16; x <= 16; x = x + 4)
					if (pCurrent[y * stride + x * 4] < threshold.B || pCurrent[y * stride + x * 4 + 1] < threshold.G || pCurrent[y * stride + x * 4 + 2] < threshold.R)
						return false;

			return true;
		}
		#endregion

		#region IsBackgroundArea24bpp()
		private unsafe static bool IsBackgroundArea24bpp(byte* pCurrent, int stride, Color threshold)
		{
			for (int y = -16; y <= 16; y = y + 4)
				for (int x = -16; x <= 16; x = x + 4)
					if (pCurrent[y * stride + x*3] < threshold.B || pCurrent[y * stride + x*3+1] < threshold.G || pCurrent[y * stride + x*3+2] < threshold.R)
						return false;

			return true;
		}
		#endregion

		#region IsBackgroundArea8bpp()
		private unsafe static bool IsBackgroundArea8bpp(byte* pCurrent, int stride, byte threshold)
		{
			for (int y = -16; y <= 16; y = y + 4)
				for (int x = -16; x <= 16; x = x + 4)
					if (pCurrent[y * stride + x] < threshold)
						return false;

			return true;
		}
		#endregion

		#region AreaIsNotInsideSymbol()
		private unsafe static bool AreaIsNotInsideSymbol(Symbols symbols, int x, int y)
		{
			foreach (Symbol symbol in symbols)
				if (Rectangle.Intersect(new Rectangle(x - 16, y - 16, 33, 33), symbol.Rectangle) != Rectangle.Empty)
					return false;

			return true;
		}
		#endregion

		#region GetAreaAverage32bpp()
		private unsafe static int GetAreaAverage32bpp(byte* pCurrent, int stride)
		{
			int g = 0;
			for (int i = -8; i <= 8; i++)
				g += pCurrent[-32 + i * stride] + pCurrent[-28 + i * stride] +
					pCurrent[-24 + i * stride] + pCurrent[-20 + i * stride] + pCurrent[-16 + i * stride] + pCurrent[-12 + i * stride] +
					pCurrent[-8 + i * stride] + pCurrent[-4 + i * stride] + pCurrent[0 + i * stride] + pCurrent[4 + i * stride] +
					pCurrent[8 + i * stride] + pCurrent[12 + i * stride] + pCurrent[16 + i * stride] + pCurrent[20 + i * stride] +
					pCurrent[24 + i * stride] + pCurrent[28 + i * stride] + pCurrent[32 + i * stride];

			return g / 289;
		}
		#endregion

		#region GetAreaAverage24bpp()
		private unsafe static int GetAreaAverage24bpp(byte* pCurrent, int stride)
		{
			int g = 0;
			for (int i = -8; i <= 8; i++)
				g += pCurrent[-24 + i * stride] + pCurrent[-21 + i * stride] +
					pCurrent[-18 + i * stride] + pCurrent[-15 + i * stride] + pCurrent[-12 + i * stride] + pCurrent[-9 + i * stride] +
					pCurrent[-6 + i * stride] + pCurrent[-3 + i * stride] + pCurrent[0 + i * stride] + pCurrent[3 + i * stride] +
					pCurrent[6 + i * stride] + pCurrent[9 + i * stride] + pCurrent[12 + i * stride] + pCurrent[15 + i * stride] +
					pCurrent[18 + i * stride] + pCurrent[21 + i * stride] + pCurrent[24 + i * stride];

			return g / 289;
		}
		#endregion

		#region GetAreaAverage8bpp()
		private unsafe static int GetAreaAverage8bpp(byte* pCurrent, int stride)
		{
			int g = 0;
			for (int i = -8; i <= 8; i++)
				g += pCurrent[-8 + i * stride] + pCurrent[-7 + i * stride] +
					pCurrent[-6 + i * stride] + pCurrent[-5 + i * stride] + pCurrent[-4 + i * stride] + pCurrent[-3 + i * stride] +
					pCurrent[-2 + i * stride] + pCurrent[-1 + i * stride] + pCurrent[0 + i * stride] + pCurrent[1 + i * stride] +
					pCurrent[2 + i * stride] + pCurrent[3 + i * stride] + pCurrent[4 + i * stride] + pCurrent[5 + i * stride] +
					pCurrent[6 + i * stride] + pCurrent[7 + i * stride] + pCurrent[8 + i * stride];

			return g / 289;
		}
		#endregion

		#region MergeObjectsNestedInPictures()
		private static void MergeObjectsNestedInPictures(Symbols symbols)
		{
			bool repeat;

			do
			{
				repeat = false;
				
				for (int i = symbols.Count - 1; i >= 0; i--)
				{
					if (symbols[i].IsPicture)
					{
						Symbol picture = symbols[i];

						for (int j = symbols.Count - 1; j >= 0; j--)
						{
							if (i != j)
							{
								Rectangle rect = Rectangle.Intersect(picture.Rectangle, symbols[j].Rectangle);

								if ((rect.Width * rect.Height > (picture.Width * picture.Height) / 10) || (rect.Width * rect.Height > (symbols[j].Width * symbols[j].Height) / 10))
								{
									if (i < j)
									{
										symbols[i].Merge(symbols[j]);
										symbols.RemoveAt(j);
									}
									else
									{
										symbols[j].Merge(picture);
										symbols.RemoveAt(i);
										symbols[j].ObjectType = Symbol.Type.Picture;

										if (symbols[j].IsPicture)
											repeat = true;
									}
								}
							}
						}
					}
				}
			} while (repeat);
		}
		#endregion


		#region GetLineHistograms()
		static uint[][] GetLineHistograms(Bitmap bitmap)
		{
			uint[][] histograms = new uint[(int)Math.Ceiling(bitmap.Width / 8.0)][];

			BitmapData bitmapData = null;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;
				int widthEight = width / 8;

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				unsafe
				{
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
					int stride = bitmapData.Stride;

					for (int x = 0; x < widthEight; x++)
					{
						histograms[x] = new uint[256];

						for (int y = 0; y < height; y++)
						{
							for (int i = 0; i < 8; i++)
								histograms[x][pOrig[y * stride + ((x * 8) + i)]]++;
						}
					}

					int restColumns = width % 8;

					if (restColumns > 0)
					{
						histograms[widthEight] = new uint[256];

						for (int y = 0; y < height; y++)
						{
							for (int i = 0; i < restColumns; i++)
								histograms[widthEight][pOrig[y * stride + ((widthEight * 8) + i)]]++;
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			return histograms;
		}
		#endregion

		#region GetThresholds()
		static uint[] GetThresholds(uint[][] histograms)
		{
			uint[] thresholds = new uint[histograms.GetLength(0)];

			int width = thresholds.Length;

			for (int x = 0; x < width; x++)
			{
				uint[] h = histograms[x];
				byte threshold = Histogram.GetOtsuThreshold(h);

				thresholds[x] = threshold;
			}

			return thresholds;
		}
		#endregion

		#region GetAverages()
		static uint[] GetAverages(Bitmap bitmap, uint[] thresholds)
		{
			uint[] averages = new uint[thresholds.Length];

			BitmapData bitmapData = null;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;
				int widthEight = width / 8;

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				unsafe
				{
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
					int stride = bitmapData.Stride;

					for (int x = 0; x < widthEight; x++)
					{
						int sum = 0;
						int count = 0;

						for (int y = 0; y < height; y++)
						{
							for (int i = 0; i < 8; i++)
							{
								if (pOrig[y * stride + ((x * 8) + i)] > thresholds[x])
								{
									sum += pOrig[y * stride + ((x * 8) + i)];
									count++;
								}
							}
						}

						if (count > 0)
							averages[x] = (uint)(sum / count);
					}

					int restColumns = width % 8;

					if (restColumns > 0)
					{
						int sum = 0;
						int count = 0;

						for (int y = 0; y < height; y++)
						{
							for (int i = 0; i < restColumns; i++)
							{
								if (pOrig[y * stride + ((widthEight * 8) + i)] > thresholds[widthEight])
								{
									sum += pOrig[y * stride + ((widthEight * 8) + i)];
									count++;
								}
							}
						}

						if (count > 0)
							averages[widthEight] = (uint)(sum / count);
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			return averages;
		}
		#endregion

		#region GetMedians()
		static uint[] GetMedians(Bitmap bitmap, uint[] thresholds)
		{
			uint[] averages = new uint[thresholds.Length];

			BitmapData bitmapData = null;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;
				int widthEight = width / 8;

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				unsafe
				{
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
					int stride = bitmapData.Stride;

					for (int x = 0; x < widthEight; x++)
					{
						uint[] array = new uint[256];

						for (int y = 0; y < height; y++)
							for (int i = 0; i < 8; i++)
								if (pOrig[y * stride + ((x * 8) + i)] > thresholds[x])
									array[pOrig[y * stride + ((x * 8) + i)]]++;

						averages[x] = Misc.GetMedianIndex(array);
					}

					int restColumns = width % 8;

					if (restColumns > 0)
					{
						uint[] array = new uint[256];

						for (int y = 0; y < height; y++)
							for (int i = 0; i < restColumns; i++)
								if (pOrig[y * stride + ((widthEight * 8) + i)] > thresholds[widthEight])
									array[pOrig[y * stride + ((widthEight * 8) + i)]]++;

						averages[widthEight] = Misc.GetMedianIndex(array);
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			return averages;
		}
		#endregion

		#region FixImageLightDistribution()
		static uint[] FixImageLightDistribution(Bitmap bitmap, uint[] thresholds, uint[] averages, double average)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;

				//get averages and thresholds for each column
				uint[] localAverages = new uint[width];
				uint[] localThresholds = new uint[width];

				localAverages[0] = averages[0];
				localAverages[1] = averages[0];
				localAverages[2] = averages[0];
				localThresholds[0] = thresholds[0];
				localThresholds[1] = thresholds[0];
				localThresholds[2] = thresholds[0];

				for (int x = 3; x < width; x++)
				{
					int eight = x / 8;

					switch (x % 8)
					{
						case 0:
							localAverages[x] = (3 * averages[eight - 1] + 4 * averages[eight]) / 7;
							localThresholds[x] = (3 * thresholds[eight - 1] + 4 * thresholds[eight]) / 7;
							break;
						case 1:
							localAverages[x] = (2 * averages[eight - 1] + 5 * averages[eight]) / 7;
							localThresholds[x] = (2 * thresholds[eight - 1] + 5 * thresholds[eight]) / 7;
							break;
						case 2:
							localAverages[x] = (1 * averages[eight - 1] + 6 * averages[eight]) / 7;
							localThresholds[x] = (1 * thresholds[eight - 1] + 6 * thresholds[eight]) / 7;
							break;
						case 3:
							localAverages[x] = averages[eight];
							localThresholds[x] = thresholds[eight];
							break;
						case 4:
							localAverages[x] = averages[eight];
							localThresholds[x] = thresholds[eight];
							break;
						case 5:
							if (eight < averages.Length - 1)
							{
								localAverages[x] = (1 * averages[eight + 1] + 6 * averages[eight]) / 7;
								localThresholds[x] = (1 * thresholds[eight + 1] + 6 * thresholds[eight]) / 7;
							}
							else
							{
								localAverages[x] = averages[eight];
								localThresholds[x] = thresholds[eight];
							} break;
						case 6:
							if (eight < averages.Length - 1)
							{
								localAverages[x] = (2 * averages[eight + 1] + 5 * averages[eight]) / 7;
								localThresholds[x] = (2 * thresholds[eight + 1] + 5 * thresholds[eight]) / 7;
							}
							else
							{
								localAverages[x] = averages[eight];
								localThresholds[x] = thresholds[eight];
							} break;
						case 7:
							if (eight < averages.Length - 1)
							{
								localAverages[x] = (3 * averages[eight + 1] + 4 * averages[eight]) / 7;
								localThresholds[x] = (3 * thresholds[eight + 1] + 4 * thresholds[eight]) / 7;
							}
							else
							{
								localAverages[x] = averages[eight];
								localThresholds[x] = thresholds[eight];
							}
							break;
					}
				}

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				unsafe
				{
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
					int stride = bitmapData.Stride;

					for (int x = 0; x < width; x++)
					{
						uint localAverage = localAverages[x];
						uint localThreshold = localThresholds[x];

						if (localAverage != average /*&& localThreshold < localAverage && localThreshold < average*/)
						{

							if (average - localAverage < 40)
							{
								double ratio = (average) / (localAverage);
								/*if (ratio > 1.5)
									ratio = 1.5;*/

								for (int y = 0; y < height; y++)
								{
									//if (pOrig[y * stride + x] > localThreshold)
									{
										int newValue = (int)(pOrig[y * stride + x] * ratio);

										if (newValue > 255)
											pOrig[y * stride + x] = 255;
										else
											pOrig[y * stride + x] = (byte)newValue;
									}
								}
							}
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			return averages;
		}
		#endregion

		#region FixImageLightDistribution()
		/*static uint[] FixImageLightDistribution(Bitmap bitmap, uint[] thresholds, uint[] averages, double average)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;

				//get averages and thresholds for each column
				uint[] localAverages = new uint[width];
				uint[] localThresholds = new uint[width];

				localAverages[0] = averages[0];
				localAverages[1] = averages[0];
				localAverages[2] = averages[0];
				localThresholds[0] = thresholds[0];
				localThresholds[1] = thresholds[0];
				localThresholds[2] = thresholds[0];

				for (int x = 3; x < width; x++)
				{
					int eight = x / 8;

					switch (x % 8)
					{
						case 0:
							localAverages[x] = (3 * averages[eight - 1] + 4 * averages[eight]) / 7;
							localThresholds[x] = (3 * thresholds[eight - 1] + 4 * thresholds[eight]) / 7;
							break;
						case 1:
							localAverages[x] = (2 * averages[eight - 1] + 5 * averages[eight]) / 7;
							localThresholds[x] = (2 * thresholds[eight - 1] + 5 * thresholds[eight]) / 7;
							break;
						case 2:
							localAverages[x] = (1 * averages[eight - 1] + 6 * averages[eight]) / 7;
							localThresholds[x] = (1 * thresholds[eight - 1] + 6 * thresholds[eight]) / 7;
							break;
						case 3:
							localAverages[x] = averages[eight];
							localThresholds[x] = thresholds[eight];
							break;
						case 4:
							localAverages[x] = averages[eight];
							localThresholds[x] = thresholds[eight];
							break;
						case 5:
							if (eight < averages.Length - 1)
							{
								localAverages[x] = (1 * averages[eight + 1] + 6 * averages[eight]) / 7;
								localThresholds[x] = (1 * thresholds[eight + 1] + 6 * thresholds[eight]) / 7;
							}
							else
							{
								localAverages[x] = averages[eight];
								localThresholds[x] = thresholds[eight];
							} break;
						case 6:
							if (eight < averages.Length - 1)
							{
								localAverages[x] = (2 * averages[eight + 1] + 5 * averages[eight]) / 7;
								localThresholds[x] = (2 * thresholds[eight + 1] + 5 * thresholds[eight]) / 7;
							}
							else
							{
								localAverages[x] = averages[eight];
								localThresholds[x] = thresholds[eight];
							} break;
						case 7:
							if (eight < averages.Length - 1)
							{
								localAverages[x] = (3 * averages[eight + 1] + 4 * averages[eight]) / 7;
								localThresholds[x] = (3 * thresholds[eight + 1] + 4 * thresholds[eight]) / 7;
							}
							else
							{
								localAverages[x] = averages[eight];
								localThresholds[x] = thresholds[eight];
							}
							break;
					}
				}

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				unsafe
				{
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
					int stride = bitmapData.Stride;

					for (int x = 0; x < width; x++)
					{
						uint localAverage = localAverages[x];
						uint localThreshold = localThresholds[x];

						if (localAverage != average ) //&& localThreshold < localAverage && localThreshold < average)
						{

							if (average - localAverage < 40)
							{
								double ratio = (average) / (localAverage);
								//if (ratio > 1.5)
								//	ratio = 1.5;

								for (int y = 0; y < height; y++)
								{
									//if (pOrig[y * stride + x] > localThreshold)
									{
										int newValue = (int)(pOrig[y * stride + x] * ratio);

										if (newValue > 255)
											pOrig[y * stride + x] = 255;
										else
											pOrig[y * stride + x] = (byte)newValue;
									}
								}
							}
						}
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			return averages;
		}*/
		#endregion

		#region GetValues()
		static byte[] GetValues(double average, int localAverage)
		{
				byte[] values = new byte[256];
				double ratio = (average) / (localAverage);

				if (ratio > 1)
				{
					for (int i = Math.Max(0, localAverage - 20); i < 256; i++)
						values[i] = (byte)((i * ratio <= 255) ? i * ratio : 255);

					for (int i = Math.Max(0, localAverage - 40); i < Math.Max(0, localAverage - 20); i++)
					{
						
						
						values[i] = (byte)((i * ratio <= 255) ? i * ratio : 255);
					}

					for (int i = 0; i < Math.Max(0, localAverage - 40); i++)
						values[i] = (byte)i;
				}
				else
				{
					for (int i = 0; i < 256; i++)
						values[i] = (byte)(i * ratio);
				}

			return values;
		}
		#endregion

		#endregion

	}
}
