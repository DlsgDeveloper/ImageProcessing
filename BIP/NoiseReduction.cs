using System;
using System.Drawing ;
using System.Drawing.Imaging ;

using ImageProcessing.PageObjects;

namespace ImageProcessing
{
	public class NoiseReduction
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;
		float progressChangedSeed = 0;
		float progressChangedRatio = 1;

		#region constructor
		public NoiseReduction()
		{
		}
		#endregion

		#region enum DespeckleMethod
		/// <summary>
		/// Objects - finds objects and if width or height is equal or less than 'maskSize', object is deleted;
		/// Regions - traditional, but slow. if region (3x3 for example) has white around, the whole region gets white. If objects are close to each other, it might not find all of them.
		/// </summary>
		public enum DespeckleMethod
		{
			Objects = 0x01,
			Regions = 0x02
		}
		#endregion

		#region enum DespeckleMode
		public enum DespeckleMode
		{
			BlackSpecklesOnly = 0x01,
			WhiteSpecklesOnly = 0x02,
			BothColors = BlackSpecklesOnly | WhiteSpecklesOnly
		}
		#endregion

		#region enum DespeckleMode
		public enum DespeckleSize
		{
			Size1x1 = 0x01,
			Size2x2 = 0x02,
			Size3x3 = 0x03,
			Size4x4 = 0x04,
			Size5x5 = 0x05,
			Size6x6 = 0x06,
		}
		#endregion


		//	PUBLIC METHODS
		#region public methods

		#region Despeckle()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="maskSize"></param>
		/// <param name="quickMethod">if false, after despeckle, it finds objects and deletes all that's size is equal or less than 'maskSize'</param>
		public unsafe void Despeckle(Bitmap bitmap, DespeckleSize maskSize, DespeckleMethod despeckleMethod, DespeckleMode despeckleMode)
		{
			BitmapData bmpData = null;
			float cutProgressBy = (despeckleMode == DespeckleMode.BothColors) ? 2F : 1F;

			try
			{
				bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				if ((despeckleMode & DespeckleMode.WhiteSpecklesOnly) > 0)
				{
					if (despeckleMethod == DespeckleMethod.Regions)
					{
						this.progressChangedRatio = 1F / cutProgressBy;
						DespeckleWhite(bmpData, (int)maskSize);
					}
					else
					{
						this.progressChangedRatio = 0.5F / cutProgressBy;
						Symbols allSymbols = FindObjects1bpp(bmpData);
						this.progressChangedSeed = 0.5F / cutProgressBy;
						
						if (allSymbols.Count > 0)
						{
							byte* scan0 = (byte*)bmpData.Scan0.ToPointer();
							int stride = bmpData.Stride;

							for (int i = 0; i < allSymbols.Count; i++ )
							{
								Symbol symbol = allSymbols[i];
								if (symbol.Width <= (int)maskSize && symbol.Height <= (int)maskSize)
									EraseSymbol(scan0, stride, symbol);

								if ((i % 1000) == 0)
									FireProgressEvent((i + 1F) / allSymbols.Count);
							}
						}
					}
				}

				if ((despeckleMode & DespeckleMode.BlackSpecklesOnly) > 0)
				{
					this.progressChangedSeed = (despeckleMode == DespeckleMode.BothColors) ? 0.5F : 0F;
					
					Inverter.Invert(bmpData);

					if (despeckleMethod == DespeckleMethod.Regions)
					{
						this.progressChangedRatio = 1F / cutProgressBy;
						DespeckleWhite(bmpData, (int)maskSize);
					}
					else
					{
						this.progressChangedRatio = 0.5F / cutProgressBy;
						Symbols allSymbols = FindObjects1bpp(bmpData);
						this.progressChangedSeed += 0.5F / cutProgressBy;

						if (allSymbols.Count > 0)
						{
							byte* scan0 = (byte*)bmpData.Scan0.ToPointer();
							int stride = bmpData.Stride;

							for (int i = 0; i < allSymbols.Count; i++)
							{
								Symbol symbol = allSymbols[i];
								if (symbol.Width <= (int)maskSize && symbol.Height <= (int)maskSize)
									EraseSymbol(scan0, stride, symbol);

								if ((i % 1000) == 0)
									FireProgressEvent((i + 1F) / allSymbols.Count);
							}
						}
					}

					Inverter.Invert(bmpData);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("NoiseReduction, Despeckle()\n" + ex.Message);
			}
			finally
			{
				if (bmpData != null)
					bitmap.UnlockBits(bmpData);

#if SAVE_RESULTS
				bitmap.Save(Debug.SaveToDir + "Noise Reduction after despeckle.png", ImageFormat.Png);
#endif
			}
		}
		#endregion

		#region Despeckle()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="maskSize"></param>
		/// <param name="quickMethod">if false, after despeckle, it finds objects and deletes all that's size is equal or less than 'maskSize'</param>
		public static void Despeckle(Bitmap bitmap, DespeckleSize maskSize, DespeckleMode despeckleMode, DespeckleMethod despeckleMethod)
		{
			NoiseReduction noiseReduction = new NoiseReduction();
			noiseReduction.Despeckle(bitmap, maskSize, despeckleMethod, despeckleMode);
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region DespeckleWhite()
		private unsafe void DespeckleWhite(BitmapData bmpData, int maskSize)
		{
			float despeckleMaskProgressInterval = 1.0F / maskSize;
			
			Despeckle1x1(bmpData);

			FireProgressEvent(1 * despeckleMaskProgressInterval);
			if (maskSize >= 2)
			{
				Despeckle2x2(bmpData);
				FireProgressEvent(2 * despeckleMaskProgressInterval);
			}
			if (maskSize >= 3)
			{
				Despeckle3x3(bmpData);
				FireProgressEvent(3 * despeckleMaskProgressInterval);
			}
			if (maskSize >= 4)
			{
				Despeckle4x4(bmpData);
				FireProgressEvent(4 * despeckleMaskProgressInterval);
			}
			if (maskSize >= 5)
			{
				Despeckle5x5(bmpData);
				FireProgressEvent(5 * despeckleMaskProgressInterval);
			}
			if (maskSize >= 6)
			{
				Despeckle6x6(bmpData);
				FireProgressEvent(6 * despeckleMaskProgressInterval);
			}
			if (maskSize >= 7)
			{
				Despeckle7x7(bmpData);
				FireProgressEvent(7 * despeckleMaskProgressInterval);
			}
			if (maskSize >= 8)
			{
				Despeckle8x8(bmpData);
				FireProgressEvent(8 * despeckleMaskProgressInterval);
			}
		}
		#endregion
	
		#region FindObjects1bpp()
		private unsafe Symbols FindObjects1bpp(BitmapData bmpData)
		{
			Symbols		symbols = new Symbols();

			symbols.Add(new Symbol(0, 0));
			int x, y;

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			try
			{
				int stride = bmpData.Stride;
				int[] upGroupId = new int[bmpData.Width];
				int[] groupId = new int[bmpData.Width];
				bool[] line = new bool[bmpData.Width];
				int width = bmpData.Width;
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();

				for (y = 0; y < bmpData.Height; y++)
				{
					groupId = new int[width];
					byte* pCurrent = pSource + (y * stride);

					for (x = 0; x < ((width / 8) * 8); x = x + 8)
					{
						line[x] = (pCurrent[0] & 0x80) > 0;
						line[x + 1] = (pCurrent[0] & 0x40) > 0;
						line[x + 2] = (pCurrent[0] & 0x20) > 0;
						line[x + 3] = (pCurrent[0] & 0x10) > 0;
						line[x + 4] = (pCurrent[0] & 8) > 0;
						line[x + 5] = (pCurrent[0] & 4) > 0;
						line[x + 6] = (pCurrent[0] & 2) > 0;
						line[x + 7] = (pCurrent[0] & 1) > 0;
						pCurrent++;
					}

					for (x = (width / 8) * 8; x < width; x++)
						line[x] = (pSource[(y * stride) + (x / 8)] & (((int)0x80) >> (x % 8))) > 0;

					for (x = 0; x < width; x++)
					{
						if (line[x])
						{
							Symbol currentObject;
							Symbol prevObject;
							int idSource;
							int idToChange;
							int i;

							if (upGroupId[x] > 0)
							{
								groupId[x] = upGroupId[x];
								currentObject = symbols[groupId[x]];

								if ((x > 0) && (groupId[x - 1] > 0) && (groupId[x] != groupId[x - 1]))
								{
									prevObject = symbols[groupId[x - 1]];
									prevObject.GrowThru(currentObject);
									symbols.RemoveAt(groupId[x]);
									idSource = groupId[x - 1];
									idToChange = groupId[x];

									for (i = 0; i < width; i++)
									{
										if (upGroupId[i] == idToChange)
											upGroupId[i] = idSource;
										if (groupId[i] == idToChange)
											groupId[i] = idSource;
										if (upGroupId[i] > idToChange)
											upGroupId[i]--;
										if (groupId[i] > idToChange)
											groupId[i]--;
									}
								}
								else
								{
									currentObject.Bottom = (currentObject.Bottom > (y + 1)) ? currentObject.Bottom : (y + 1);
									currentObject.Pixels++;
								}
							}
							else if ((x > 0) && (upGroupId[x - 1] > 0))
							{
								groupId[x] = upGroupId[x - 1];
								currentObject = symbols[groupId[x]];

								if ((groupId[x - 1] > 0) && (groupId[x] != groupId[x - 1]))
								{
									prevObject = symbols[groupId[x - 1]];
									prevObject.GrowThru(currentObject);
									symbols.RemoveAt(groupId[x]);
									idSource = groupId[x - 1];
									idToChange = groupId[x];

									for (i = 0; i < width; i++)
									{
										if (upGroupId[i] == idToChange)
											upGroupId[i] = idSource;
										if (groupId[i] == idToChange)
											groupId[i] = idSource;
										if (upGroupId[i] > idToChange)
											upGroupId[i]--;
										if (groupId[i] > idToChange)
											groupId[i]--;
									}
								}
								else
								{
									currentObject.Bottom = (currentObject.Bottom > (y + 1)) ? currentObject.Bottom : (y + 1);
									currentObject.Right = (currentObject.Right > (x + 1)) ? currentObject.Right : (x + 1);
									currentObject.Pixels++;
								}

								if ((((x > 0) && (x < (width - 1))) && ((upGroupId[x + 1] > 0) && !line[x + 1])) && (upGroupId[x + 1] != upGroupId[x - 1]))
								{
									groupId[x] = upGroupId[x + 1];
									currentObject = symbols[groupId[x]];
									prevObject = symbols[upGroupId[x - 1]];
									prevObject.GrowThru(currentObject);
									symbols.RemoveAt(groupId[x]);
									idSource = upGroupId[x - 1];
									idToChange = groupId[x];

									for (i = 0; i < width; i++)
									{
										if (upGroupId[i] == idToChange)
											upGroupId[i] = idSource;
										if (groupId[i] == idToChange)
											groupId[i] = idSource;
										if (upGroupId[i] > idToChange)
											upGroupId[i]--;
										if (groupId[i] > idToChange)
											groupId[i]--;
									}
								}
							}
							else if ((x < (width - 1)) && (upGroupId[x + 1] > 0))
							{
								groupId[x] = upGroupId[x + 1];
								currentObject = symbols[groupId[x]];

								if (((x > 0) && (groupId[x - 1] > 0)) && (groupId[x] != groupId[x - 1]))
								{
									symbols[groupId[x - 1]].GrowThru(currentObject);
									symbols.RemoveAt(groupId[x]);
									idSource = groupId[x - 1];
									idToChange = groupId[x];

									for (i = 0; i < width; i++)
									{
										if (upGroupId[i] == idToChange)
											upGroupId[i] = idSource;
										if (groupId[i] == idToChange)
											groupId[i] = idSource;
										if (upGroupId[i] > idToChange)
											upGroupId[i]--;
										if (groupId[i] > idToChange)
											groupId[i]--;
									}
								}
								else
								{
									currentObject.Bottom = (currentObject.Bottom > (y + 1)) ? currentObject.Bottom : (y + 1);
									currentObject.X = (currentObject.X < x) ? currentObject.X : x;
									currentObject.Pixels++;
								}
							}
							else if ((x > 0) && (groupId[x - 1] > 0))
							{
								groupId[x] = groupId[x - 1];
								currentObject = symbols[groupId[x]];
								currentObject.Right = (currentObject.Right > (x + 1)) ? currentObject.Right : (x + 1);
								currentObject.Pixels++;
							}
							else
							{
								currentObject = new Symbol(x, y);
								symbols.Add(currentObject);
								groupId[x] = symbols.Count - 1;
							}
						}
					}
					upGroupId = groupId;

					if((y % 1000) == 0)
						FireProgressEvent((y + 1.0F) / (bmpData.Height));
				}

#if DEBUG
				Console.WriteLine("FindObjects1bpp(), Seeking Objects: {0}", DateTime.Now.Subtract(start).ToString());
#endif

#if SAVE_RESULTS
				symbols.DrawToFile(Debug.SaveToDir + "Noise Reduction all symbols.png", new Size(bmpData.Width, bmpData.Height));
#endif
			}
			finally
			{				
				symbols.RemoveAt(0);
			}

			return symbols;
		}
		#endregion

		#region EraseSymbol()
		private unsafe void EraseSymbol(byte* pOrig, int stride, Symbol symbol)
		{
			byte[,] array = ImageProcessing.RasterProcessing.GetObjectMap(pOrig, stride, symbol.Rectangle, new Point(symbol.APixelX, symbol.APixelY));

			for (int y = symbol.Height - 1; y >= 0; y--)
			{
				for (int x = symbol.Width - 1; x >= 0; x--)
				{
					if((array[y, x >> 3] | (0x80 >> (x & 0x07))) > 0)
						pOrig[(y + symbol.Y) * stride + (x + symbol.X) / 8] &= (byte)~(0x80 >> ((x + symbol.X) % 8));
				}
			}
		}
		#endregion

		#region Despeckle8x8()
		private void Despeckle8x8(BitmapData bmpData)
		{
			int stride = bmpData.Stride;
			int n1, n2, n3, n4, n5, n6, n7, n8, n9, n10;
			int w = bmpData.Width;
			int h = bmpData.Height;
			int x, y;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				byte* p1, p2, p3, p4, p5, p6, p7, p8, p9, p10;

				for (y = -1; y < h - 8; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = p3 + stride;
					p5 = p4 + stride;
					p6 = p5 + stride;
					p7 = p6 + stride;
					p8 = p7 + stride;
					p9 = p8 + stride;
					p10 = (y < h - 9) ? p9 + stride : p9;

					for (x = 0; x < stride - 1; x++)
					{
						n1 = (y == -1) ? 0 : (int)(((*p1) * 256) + (*(p1 + 1)));
						n2 = (int)(((*p2) * 256) + (*(p2 + 1)));
						n3 = (int)(((*p3) * 256) + (*(p3 + 1)));
						n4 = (int)(((*p4) * 256) + (*(p4 + 1)));
						n5 = (int)(((*p5) * 256) + (*(p5 + 1)));
						n6 = (int)(((*p6) * 256) + (*(p6 + 1)));
						n7 = (int)(((*p7) * 256) + (*(p7 + 1)));
						n8 = (int)(((*p8) * 256) + (*(p8 + 1)));
						n9 = (int)(((*p9) * 256) + (*(p9 + 1)));
						n10 = (y < h - 9) ? (int)(((*p10) * 256) + (*(p10 + 1))) : 0;

						if (((n1 & 0xFFC0) == 0) && ((n10 & 0xFFC0) == 0) && ((n2 & 0x8040) == 0) && ((n3 & 0x8040) == 0) && ((n4 & 0x8040) == 0) && ((n5 & 0x8040) == 0) && ((n6 & 0x8040) == 0) && ((n7 & 0x8040) == 0) && ((n8 & 0x8040) == 0) && ((n9 & 0x8040) == 0))
						{
							*p2 &= 0x80;
							*p3 &= 0x80;
							*p4 &= 0x80;
							*p5 &= 0x80;
							*p6 &= 0x80;
							*p7 &= 0x80;
							*p8 &= 0x80;
							*p9 &= 0x80;
							*(p2 + 1) &= 0x7F;
							*(p3 + 1) &= 0x7F;
							*(p4 + 1) &= 0x7F;
							*(p5 + 1) &= 0x7F;
							*(p6 + 1) &= 0x7F;
							*(p7 + 1) &= 0x7F;
							*(p8 + 1) &= 0x7F;
							*(p9 + 1) &= 0x7F;
						}

						if (((n1 & 0x7FE0) == 0) && ((n10 & 0x7FE0) == 0) && ((n2 & 0x4020) == 0) && ((n3 & 0x4020) == 0) && ((n4 & 0x4020) == 0) && ((n5 & 0x4020) == 0) && ((n6 & 0x4020) == 0) && ((n7 & 0x4020) == 0) && ((n8 & 0x4020) == 0) && ((n9 & 0x4020) == 0))
						{
							*p2 &= 0xC0;
							*p3 &= 0xC0;
							*p4 &= 0xC0;
							*p5 &= 0xC0;
							*p6 &= 0xC0;
							*p7 &= 0xC0;
							*p8 &= 0xC0;
							*p9 &= 0xC0;
							*(p2 + 1) &= 0x3F;
							*(p3 + 1) &= 0x3F;
							*(p4 + 1) &= 0x3F;
							*(p5 + 1) &= 0x3F;
							*(p6 + 1) &= 0x3F;
							*(p7 + 1) &= 0x3F;
							*(p8 + 1) &= 0x3F;
							*(p9 + 1) &= 0x3F;
						}
						if (((n1 & 0x3FF0) == 0) && ((n10 & 0x3FF0) == 0) && ((n2 & 0x2010) == 0) && ((n3 & 0x2010) == 0) && ((n4 & 0x2010) == 0) && ((n5 & 0x2010) == 0) && ((n6 & 0x2010) == 0) && ((n7 & 0x2010) == 0) && ((n8 & 0x2010) == 0) && ((n9 & 0x2010) == 0))
						{
							*p2 &= 0xE0;
							*p3 &= 0xE0;
							*p4 &= 0xE0;
							*p5 &= 0xE0;
							*p6 &= 0xE0;
							*p7 &= 0xE0;
							*p8 &= 0xE0;
							*p9 &= 0xE0;
							*(p2 + 1) &= 0x1F;
							*(p3 + 1) &= 0x1F;
							*(p4 + 1) &= 0x1F;
							*(p5 + 1) &= 0x1F;
							*(p6 + 1) &= 0x1F;
							*(p7 + 1) &= 0x1F;
							*(p8 + 1) &= 0x1F;
							*(p9 + 1) &= 0x1F;
						}
						if (((n1 & 0x1FF8) == 0) && ((n10 & 0x1FF8) == 0) && ((n2 & 0x1008) == 0) && ((n3 & 0x1008) == 0) && ((n4 & 0x1008) == 0) && ((n5 & 0x1008) == 0) && ((n6 & 0x1008) == 0) && ((n7 & 0x1008) == 0) && ((n8 & 0x1008) == 0) && ((n9 & 0x1008) == 0))
						{
							*p2 &= 0xF0;
							*p3 &= 0xF0;
							*p4 &= 0xF0;
							*p5 &= 0xF0;
							*p6 &= 0xF0;
							*p7 &= 0xF0;
							*p8 &= 0xF0;
							*p9 &= 0xF0;
							*(p2 + 1) &= 0xF;
							*(p3 + 1) &= 0xF;
							*(p4 + 1) &= 0xF;
							*(p5 + 1) &= 0xF;
							*(p6 + 1) &= 0xF;
							*(p7 + 1) &= 0xF;
							*(p8 + 1) &= 0xF;
							*(p9 + 1) &= 0xF;
						}
						if (((n1 & 0xFFC) == 0) && ((n10 & 0xFFC) == 0) && ((n2 & 0x804) == 0) && ((n3 & 0x804) == 0) && ((n4 & 0x804) == 0) && ((n5 & 0x804) == 0) && ((n6 & 0x804) == 0) && ((n7 & 0x804) == 0) && ((n8 & 0x804) == 0) && ((n9 & 0x804) == 0))
						{
							*p2 &= 0xF8;
							*p3 &= 0xF8;
							*p4 &= 0xF8;
							*p5 &= 0xF8;
							*p6 &= 0xF8;
							*p7 &= 0xF8;
							*p8 &= 0xF8;
							*p9 &= 0xF8;
							*(p2 + 1) &= 0x7;
							*(p3 + 1) &= 0x7;
							*(p4 + 1) &= 0x7;
							*(p5 + 1) &= 0x7;
							*(p6 + 1) &= 0x7;
							*(p7 + 1) &= 0x7;
							*(p8 + 1) &= 0x7;
							*(p9 + 1) &= 0x7;
						}
						if (((n1 & 0x7FE) == 0) && ((n10 & 0x7FE) == 0) && ((n2 & 0x402) == 0) && ((n3 & 0x402) == 0) && ((n4 & 0x402) == 0) && ((n5 & 0x402) == 0) && ((n6 & 0x402) == 0) && ((n7 & 0x402) == 0) && ((n8 & 0x402) == 0) && ((n9 & 0x402) == 0))
						{
							*p2 &= 0xFC;
							*p3 &= 0xFC;
							*p4 &= 0xFC;
							*p5 &= 0xFC;
							*p6 &= 0xFC;
							*p7 &= 0xFC;
							*p8 &= 0xFC;
							*p9 &= 0xFC;
							*(p2 + 1) &= 0x3;
							*(p3 + 1) &= 0x3;
							*(p4 + 1) &= 0x3;
							*(p5 + 1) &= 0x3;
							*(p6 + 1) &= 0x3;
							*(p7 + 1) &= 0x3;
							*(p8 + 1) &= 0x3;
							*(p9 + 1) &= 0x3;
						}
						if (((n1 & 0x3FF) == 0) && ((n10 & 0x3FF) == 0) && ((n2 & 0x201) == 0) && ((n3 & 0x201) == 0) && ((n4 & 0x201) == 0) && ((n5 & 0x201) == 0) && ((n6 & 0x201) == 0) && ((n7 & 0x201) == 0) && ((n8 & 0x201) == 0) && ((n9 & 0x201) == 0))
						{
							*p2 &= 0xFE;
							*p3 &= 0xFE;
							*p4 &= 0xFE;
							*p5 &= 0xFE;
							*p6 &= 0xFE;
							*p7 &= 0xFE;
							*p8 &= 0xFE;
							*p9 &= 0xFE;
							*(p2 + 1) &= 0x1;
							*(p3 + 1) &= 0x1;
							*(p4 + 1) &= 0x1;
							*(p5 + 1) &= 0x1;
							*(p6 + 1) &= 0x1;
							*(p7 + 1) &= 0x1;
							*(p8 + 1) &= 0x1;
							*(p9 + 1) &= 0x1;
						}
						if (x < stride - 1)
						{
							if (((n1 & 0x1FF) == 0) && ((n10 & 0x1FF) == 0) && ((n2 & 0x100) == 0) && ((n2 & 0x100) == 0) && ((n4 & 0x100) == 0) && ((n5 & 0x100) == 0) && ((n6 & 0x100) == 0) && ((n7 & 0x100) == 0) && ((n8 & 0x100) == 0) && ((n9 & 0x100) == 0) &&
								((p2[2] & 0x80) == 0) && ((p3[2] & 0x80) == 0) && ((p4[2] & 0x80) == 0) && ((p5[2] & 0x80) == 0) && ((p6[2] & 0x80) == 0) && ((p7[2] & 0x80) == 0) && ((p8[2] & 0x80) == 0) && ((p9[2] & 0x80) == 0))
							{
								p2[1] = 0;
								p3[1] = 0;
								p4[1] = 0;
								p5[1] = 0;
								p6[1] = 0;
								p7[1] = 0;
								p8[1] = 0;
								p9[1] = 0;
							}
						}
						else
						{
							if (((n1 & 0x1FF) == 0) && ((n10 & 0x1FF) == 0) && ((n2 & 0x100) == 0) && ((n3 & 0x100) == 0) && ((n4 & 0x100) == 0) && ((n5 & 0x100) == 0) && ((n6 & 0x100) == 0) && ((n7 & 0x100) == 0) && ((n8 & 0x100) == 0) && ((n9 & 0x100) == 0))
							{
								p2[1] = 0;
								p3[1] = 0;
								p4[1] = 0;
								p5[1] = 0;
								p6[1] = 0;
								p7[1] = 0;
								p8[1] = 0;
								p9[1] = 0;
							}
						}

						p1++;
						p2++;
						p3++;
						p4++;
						p5++;
						p6++;
						p7++;
						p8++;
						p9++;
						p10++;
					}
				}

				//first column
				for (y = -1; y < h - 8; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = p3 + stride;
					p5 = p4 + stride;
					p6 = p5 + stride;
					p7 = p6 + stride;
					p8 = p7 + stride;
					p9 = p8 + stride;
					p10 = (y < h - 9) ? p9 + stride : p9;

					n1 = (y == -1) ? 0 : (int)(((*p1) * 256) + (*(p1 + 1)));
					n2 = (int)(((*p2) * 256) + (*(p2 + 1)));
					n3 = (int)(((*p3) * 256) + (*(p3 + 1)));
					n4 = (int)(((*p4) * 256) + (*(p4 + 1)));
					n5 = (int)(((*p5) * 256) + (*(p5 + 1)));
					n6 = (int)(((*p6) * 256) + (*(p6 + 1)));
					n7 = (int)(((*p7) * 256) + (*(p7 + 1)));
					n8 = (int)(((*p8) * 256) + (*(p8 + 1)));
					n9 = (int)(((*p9) * 256) + (*(p9 + 1)));
					n10 = (y < h - 9) ? (int)(((*p10) * 256) + (*(p10 + 1))) : 0;

					if (((n1 & 0xFF80) == 0) && ((n10 & 0xFF80) == 0) && ((n2 & 0x80) == 0) && ((n3 & 0x80) == 0) && ((n4 & 0x80) == 0) && ((n5 & 0x80) == 0) && ((n6 & 0x80) == 0) && ((n7 & 0x80) == 0) && ((n8 & 0x80) == 0) && ((n9 & 0x80) == 0))
					{
						*p2 = 0;
						*p3 = 0;
						*p4 = 0;
						*p5 = 0;
						*p6 = 0;
						*p7 = 0;
						*p8 = 0;
						*p9 = 0;
					}
				}

				//last column
				x = w - 1;
				for (y = -7; y < h - 2; y++)
					if (IsPixelWhite(pSource, stride, x - 8, y, w, h) && IsPixelWhite(pSource, stride, x - 7, y, w, h) && IsPixelWhite(pSource, stride, x - 6, y, w, h) && IsPixelWhite(pSource, stride, x - 5, y, w, h) && IsPixelWhite(pSource, stride, x - 4, y, w, h) && IsPixelWhite(pSource, stride, x - 3, y, w, h) && IsPixelWhite(pSource, stride, x - 2, y, w, h) && IsPixelWhite(pSource, stride, x - 1, y, w, h) && IsPixelWhite(pSource, stride, x, y, w, h) &&
						IsPixelWhite(pSource, stride, x - 8, y + 1, w, h) &&
						IsPixelWhite(pSource, stride, x - 8, y + 2, w, h) &&
						IsPixelWhite(pSource, stride, x - 8, y + 3, w, h) &&
						IsPixelWhite(pSource, stride, x - 8, y + 4, w, h) &&
						IsPixelWhite(pSource, stride, x - 8, y + 5, w, h) &&
						IsPixelWhite(pSource, stride, x - 8, y + 6, w, h) &&
						IsPixelWhite(pSource, stride, x - 8, y + 7, w, h) &&
						IsPixelWhite(pSource, stride, x - 8, y + 8, w, h) &&
						IsPixelWhite(pSource, stride, x - 8, y + 9, w, h) && IsPixelWhite(pSource, stride, x - 7, y + 9, w, h) && IsPixelWhite(pSource, stride, x - 6, y + 9, w, h) && IsPixelWhite(pSource, stride, x - 5, y + 9, w, h) && IsPixelWhite(pSource, stride, x - 4, y + 9, w, h) && IsPixelWhite(pSource, stride, x - 3, y + 9, w, h) && IsPixelWhite(pSource, stride, x - 2, y + 9, w, h) && IsPixelWhite(pSource, stride, x - 1, y + 9, w, h) && IsPixelWhite(pSource, stride, x, y + 9, w, h))
					{
						for (int i = 0; i <= 7; i++)
							for (int j = 1; j <= 8; j++)
								MakePixelWhite(pSource, stride, x - i, y + j, w, h);
					}
			}
		}
		#endregion

		#region Despeckle7x7()
		private void Despeckle7x7(BitmapData bmpData)
		{
			int stride = bmpData.Stride;
			int n1, n2, n3, n4, n5, n6, n7, n8, n9;
			int w = bmpData.Width;
			int h = bmpData.Height;
			int x, y;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				byte* p1, p2, p3, p4, p5, p6, p7, p8, p9;

				for (y = -1; y < h - 7; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = p3 + stride;
					p5 = p4 + stride;
					p6 = p5 + stride;
					p7 = p6 + stride;
					p8 = p7 + stride;
					p9 = (y < h - 8) ? p8 + stride : p8;

					for (x = 0; x < stride - 1; x++)
					{
						n1 = (y == -1) ? 0 : (int)(((*p1) * 256) + (*(p1 + 1)));
						n2 = (int)(((*p2) * 256) + (*(p2 + 1)));
						n3 = (int)(((*p3) * 256) + (*(p3 + 1)));
						n4 = (int)(((*p4) * 256) + (*(p4 + 1)));
						n5 = (int)(((*p5) * 256) + (*(p5 + 1)));
						n6 = (int)(((*p6) * 256) + (*(p6 + 1)));
						n7 = (int)(((*p7) * 256) + (*(p7 + 1)));
						n8 = (int)(((*p8) * 256) + (*(p8 + 1)));
						n9 = (y < h - 8) ? (int)(((*p9) * 256) + (*(p9 + 1))) : 0;

						if (((n1 & 0xFF80) == 0) && ((n9 & 0xFF80) == 0) && ((n2 & 0x8080) == 0) && ((n3 & 0x8080) == 0) && ((n4 & 0x8080) == 0) && ((n5 & 0x8080) == 0) && ((n6 & 0x8080) == 0) && ((n7 & 0x8080) == 0) && ((n8 & 0x8080) == 0))
						{
							*p2 &= 0x80;
							*p3 &= 0x80;
							*p4 &= 0x80;
							*p5 &= 0x80;
							*p6 &= 0x80;
							*p7 &= 0x80;
							*p8 &= 0x80;
						}

						if (((n1 & 0x7FC0) == 0) && ((n9 & 0x7FC0) == 0) && ((n2 & 0x4040) == 0) && ((n3 & 0x4040) == 0) && ((n4 & 0x4040) == 0) && ((n5 & 0x4040) == 0) && ((n6 & 0x4040) == 0) && ((n7 & 0x4040) == 0) && ((n8 & 0x4040) == 0))
						{
							*p2 &= 0xC0;
							*p3 &= 0xC0;
							*p4 &= 0xC0;
							*p5 &= 0xC0;
							*p6 &= 0xC0;
							*p7 &= 0xC0;
							*p8 &= 0xC0;
							*(p2 + 1) &= 0x7F;
							*(p3 + 1) &= 0x7F;
							*(p4 + 1) &= 0x7F;
							*(p5 + 1) &= 0x7F;
							*(p6 + 1) &= 0x7F;
							*(p7 + 1) &= 0x7F;
							*(p8 + 1) &= 0x7F;
						}
						if (((n1 & 0x3FE0) == 0) && ((n9 & 0x3FE0) == 0) && ((n2 & 0x2020) == 0) && ((n3 & 0x2020) == 0) && ((n4 & 0x2020) == 0) && ((n5 & 0x2020) == 0) && ((n6 & 0x2020) == 0) && ((n7 & 0x2020) == 0) && ((n8 & 0x2020) == 0))
						{
							*p2 &= 0xE0;
							*p3 &= 0xE0;
							*p4 &= 0xE0;
							*p5 &= 0xE0;
							*p6 &= 0xE0;
							*p7 &= 0xE0;
							*p8 &= 0xE0;
							*(p2 + 1) &= 0x3F;
							*(p3 + 1) &= 0x3F;
							*(p4 + 1) &= 0x3F;
							*(p5 + 1) &= 0x3F;
							*(p6 + 1) &= 0x3F;
							*(p7 + 1) &= 0x3F;
							*(p8 + 1) &= 0x3F;
						}
						if (((n1 & 0x1FF0) == 0) && ((n9 & 0x1FF0) == 0) && ((n2 & 0x1010) == 0) && ((n3 & 0x1010) == 0) && ((n4 & 0x1010) == 0) && ((n5 & 0x1010) == 0) && ((n6 & 0x1010) == 0) && ((n7 & 0x1010) == 0) && ((n8 & 0x1010) == 0))
						{
							*p2 &= 0xF0;
							*p3 &= 0xF0;
							*p4 &= 0xF0;
							*p5 &= 0xF0;
							*p6 &= 0xF0;
							*p7 &= 0xF0;
							*p8 &= 0xF0;
							*(p2 + 1) &= 0x1F;
							*(p3 + 1) &= 0x1F;
							*(p4 + 1) &= 0x1F;
							*(p5 + 1) &= 0x1F;
							*(p6 + 1) &= 0x1F;
							*(p7 + 1) &= 0x1F;
							*(p8 + 1) &= 0x1F;
						}
						if (((n1 & 0xFF8) == 0) && ((n9 & 0xFF8) == 0) && ((n2 & 0x808) == 0) && ((n3 & 0x808) == 0) && ((n4 & 0x808) == 0) && ((n5 & 0x808) == 0) && ((n6 & 0x808) == 0) && ((n7 & 0x808) == 0) && ((n8 & 0x808) == 0))
						{
							*p2 &= 0xF8;
							*p3 &= 0xF8;
							*p4 &= 0xF8;
							*p5 &= 0xF8;
							*p6 &= 0xF8;
							*p7 &= 0xF8;
							*p8 &= 0xF8;
							*(p2 + 1) &= 0xF;
							*(p3 + 1) &= 0xF;
							*(p4 + 1) &= 0xF;
							*(p5 + 1) &= 0xF;
							*(p6 + 1) &= 0xF;
							*(p7 + 1) &= 0xF;
							*(p8 + 1) &= 0xF;
						}
						if (((n1 & 0x7FC) == 0) && ((n9 & 0x7FC) == 0) && ((n2 & 0x404) == 0) && ((n3 & 0x404) == 0) && ((n4 & 0x404) == 0) && ((n5 & 0x404) == 0) && ((n6 & 0x404) == 0) && ((n7 & 0x404) == 0) && ((n8 & 0x404) == 0))
						{
							*p2 &= 0xFC;
							*p3 &= 0xFC;
							*p4 &= 0xFC;
							*p5 &= 0xFC;
							*p6 &= 0xFC;
							*p7 &= 0xFC;
							*p8 &= 0xFC;
							*(p2 + 1) &= 0x7;
							*(p3 + 1) &= 0x7;
							*(p4 + 1) &= 0x7;
							*(p5 + 1) &= 0x7;
							*(p6 + 1) &= 0x7;
							*(p7 + 1) &= 0x7;
							*(p8 + 1) &= 0x7;
						}
						if (((n1 & 0x3FE) == 0) && ((n9 & 0x3FE) == 0) && ((n2 & 0x202) == 0) && ((n3 & 0x202) == 0) && ((n4 & 0x202) == 0) && ((n5 & 0x202) == 0) && ((n6 & 0x202) == 0) && ((n7 & 0x202) == 0) && ((n8 & 0x202) == 0))
						{
							*p2 &= 0xFE;
							*p3 &= 0xFE;
							*p4 &= 0xFE;
							*p5 &= 0xFE;
							*p6 &= 0xFE;
							*p7 &= 0xFE;
							*p8 &= 0xFE;
							*(p2 + 1) &= 0x3;
							*(p3 + 1) &= 0x3;
							*(p4 + 1) &= 0x3;
							*(p5 + 1) &= 0x3;
							*(p6 + 1) &= 0x3;
							*(p7 + 1) &= 0x3;
							*(p8 + 1) &= 0x3;
						}
						if (((n1 & 0x1FF) == 0) && ((n9 & 0x1FF) == 0) && ((n2 & 0x101) == 0) && ((n3 & 0x101) == 0) && ((n4 & 0x101) == 0) && ((n5 & 0x101) == 0) && ((n6 & 0x101) == 0) && ((n7 & 0x101) == 0) && ((n8 & 0x101) == 0))
						{
							*(p2 + 1) &= 0x1;
							*(p3 + 1) &= 0x1;
							*(p4 + 1) &= 0x1;
							*(p5 + 1) &= 0x1;
							*(p6 + 1) &= 0x1;
							*(p7 + 1) &= 0x1;
							*(p8 + 1) &= 0x1;
						}

						p1++;
						p2++;
						p3++;
						p4++;
						p5++;
						p6++;
						p7++;
						p8++;
						p9++;
					}
				}

				//first row
				for (y = -1; y < h - 7; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = p3 + stride;
					p5 = p4 + stride;
					p6 = p5 + stride;
					p7 = p6 + stride;
					p8 = p7 + stride;
					p9 = (y < h - 8) ? p8 + stride : p8;

					n1 = (y == -1) ? 0 : *p1;
					n2 = *p2;
					n3 = *p3;
					n4 = *p4;
					n5 = *p5;
					n6 = *p6;
					n7 = *p7;
					n8 = *p8;
					n9 = (y < h - 8) ? *p9 : 0;

					if (((n1 & 0xFF) == 0) && ((n9 & 0xFF) == 0) && ((n2 & 0x01) == 0) && ((n3 & 0x01) == 0) && ((n4 & 0x01) == 0) && ((n5 & 0x01) == 0) && ((n6 & 0x01) == 0) && ((n7 & 0x01) == 0) && ((n8 & 0x01) == 0))
					{
						*p2 &= 0x01;
						*p3 &= 0x01;
						*p4 &= 0x01;
						*p5 &= 0x01;
						*p6 &= 0x01;
						*p7 &= 0x01;
						*p8 &= 0x01;
					}
				}

				//last column
				x = w - 1;
				for (y = -6; y < h - 2; y++)
					if (IsPixelWhite(pSource, stride, x - 7, y, w, h) && IsPixelWhite(pSource, stride, x - 6, y, w, h) && IsPixelWhite(pSource, stride, x - 5, y, w, h) && IsPixelWhite(pSource, stride, x - 4, y, w, h) && IsPixelWhite(pSource, stride, x - 3, y, w, h) && IsPixelWhite(pSource, stride, x - 2, y, w, h) && IsPixelWhite(pSource, stride, x - 1, y, w, h) && IsPixelWhite(pSource, stride, x, y, w, h) &&
						IsPixelWhite(pSource, stride, x - 7, y + 1, w, h) &&
						IsPixelWhite(pSource, stride, x - 7, y + 2, w, h) &&
						IsPixelWhite(pSource, stride, x - 7, y + 3, w, h) &&
						IsPixelWhite(pSource, stride, x - 7, y + 4, w, h) &&
						IsPixelWhite(pSource, stride, x - 7, y + 5, w, h) &&
						IsPixelWhite(pSource, stride, x - 7, y + 6, w, h) &&
						IsPixelWhite(pSource, stride, x - 7, y + 7, w, h) &&
						IsPixelWhite(pSource, stride, x - 7, y + 8, w, h) && IsPixelWhite(pSource, stride, x - 6, y + 8, w, h) && IsPixelWhite(pSource, stride, x - 5, y + 8, w, h) && IsPixelWhite(pSource, stride, x - 4, y + 8, w, h) && IsPixelWhite(pSource, stride, x - 3, y + 8, w, h) && IsPixelWhite(pSource, stride, x - 2, y + 8, w, h) && IsPixelWhite(pSource, stride, x - 1, y + 8, w, h) && IsPixelWhite(pSource, stride, x, y + 8, w, h))
					{
						for (int i = 0; i <= 6; i++)
							for (int j = 1; j <= 7; j++)
								MakePixelWhite(pSource, stride, x - i, y + j, w, h);
					}
			}
		}
		#endregion

		#region Despeckle6x6()
		private void Despeckle6x6(BitmapData bmpData)
		{
			int stride = bmpData.Stride;
			int n1, n2, n3, n4, n5, n6, n7, n8;
			int w = bmpData.Width;
			int h = bmpData.Height;
			int x, y;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				byte* p1, p2, p3, p4, p5, p6, p7, p8;

				for (y = -1; y < h - 6; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = p3 + stride;
					p5 = p4 + stride;
					p6 = p5 + stride;
					p7 = p6 + stride;
					p8 = (y < h - 7) ? p7 + stride : p7;

					for (x = 0; x < stride - 1; x++)
					{
						n1 = (y == -1) ? 0 : (int)(((*p1) * 256) + (*(p1 + 1)));
						n2 = (int)(((*p2) * 256) + (*(p2 + 1)));
						n3 = (int)(((*p3) * 256) + (*(p3 + 1)));
						n4 = (int)(((*p4) * 256) + (*(p4 + 1)));
						n5 = (int)(((*p5) * 256) + (*(p5 + 1)));
						n6 = (int)(((*p6) * 256) + (*(p6 + 1)));
						n7 = (int)(((*p7) * 256) + (*(p7 + 1)));
						n8 = (y < h - 7) ? (int)(((*p8) * 256) + (*(p8 + 1))) : 0;

						if (((n1 & 0xFF00) == 0) && ((n8 & 0xFF00) == 0) && ((n2 & 0x8100) == 0) && ((n3 & 0x8100) == 0) && ((n4 & 0x8100) == 0) && ((n5 & 0x8100) == 0) && ((n6 & 0x8100) == 0) && ((n7 & 0x8100) == 0))
						{
							*p2 &= 0x81;
							*p3 &= 0x81;
							*p4 &= 0x81;
							*p5 &= 0x81;
							*p6 &= 0x81;
							*p7 &= 0x81;
						}

						if (((n1 & 0x7F80) == 0) && ((n8 & 0x7F80) == 0) && ((n2 & 0x4080) == 0) && ((n3 & 0x4080) == 0) && ((n4 & 0x4080) == 0) && ((n5 & 0x4080) == 0) && ((n6 & 0x4080) == 0) && ((n7 & 0x4080) == 0))
						{
							*p2 &= 0xC0;
							*p3 &= 0xC0;
							*p4 &= 0xC0;
							*p5 &= 0xC0;
							*p6 &= 0xC0;
							*p7 &= 0xC0;
							*(p2 + 1) &= 0x7F;
							*(p3 + 1) &= 0x7F;
							*(p4 + 1) &= 0x7F;
							*(p5 + 1) &= 0x7F;
							*(p6 + 1) &= 0x7F;
							*(p7 + 1) &= 0x7F;
						}
						if (((n1 & 0x3FC0) == 0) && ((n8 & 0x3FC0) == 0) && ((n2 & 0x2040) == 0) && ((n3 & 0x2040) == 0) && ((n4 & 0x2040) == 0) && ((n5 & 0x2040) == 0) && ((n6 & 0x2040) == 0) && ((n7 & 0x2040) == 0))
						{
							*p2 &= 0xE0;
							*p3 &= 0xE0;
							*p4 &= 0xE0;
							*p5 &= 0xE0;
							*p6 &= 0xE0;
							*p7 &= 0xE0;
							*(p2 + 1) &= 0x7F;
							*(p3 + 1) &= 0x7F;
							*(p4 + 1) &= 0x7F;
							*(p5 + 1) &= 0x7F;
							*(p6 + 1) &= 0x7F;
							*(p7 + 1) &= 0x7F;
						}
						if (((n1 & 0x1FE0) == 0) && ((n8 & 0x1FE0) == 0) && ((n2 & 0x1020) == 0) && ((n3 & 0x1020) == 0) && ((n4 & 0x1020) == 0) && ((n5 & 0x1020) == 0) && ((n6 & 0x1020) == 0) && ((n7 & 0x1020) == 0))
						{
							*p2 &= 0xF0;
							*p3 &= 0xF0;
							*p4 &= 0xF0;
							*p5 &= 0xF0;
							*p6 &= 0xF0;
							*p7 &= 0xF0;
							*(p2 + 1) &= 0x3F;
							*(p3 + 1) &= 0x3F;
							*(p4 + 1) &= 0x3F;
							*(p5 + 1) &= 0x3F;
							*(p6 + 1) &= 0x3F;
							*(p7 + 1) &= 0x3F;
						}
						if (((n1 & 0xFF0) == 0) && ((n8 & 0xFF0) == 0) && ((n2 & 0x810) == 0) && ((n3 & 0x810) == 0) && ((n4 & 0x810) == 0) && ((n5 & 0x810) == 0) && ((n6 & 0x810) == 0) && ((n7 & 0x810) == 0))
						{
							*p2 &= 0xF8;
							*p3 &= 0xF8;
							*p4 &= 0xF8;
							*p5 &= 0xF8;
							*p6 &= 0xF8;
							*p7 &= 0xF8;
							*(p2 + 1) &= 0x1F;
							*(p3 + 1) &= 0x1F;
							*(p4 + 1) &= 0x1F;
							*(p5 + 1) &= 0x1F;
							*(p6 + 1) &= 0x1F;
							*(p7 + 1) &= 0x1F;
						}
						if (((n1 & 0x7F8) == 0) && ((n8 & 0x7F8) == 0) && ((n2 & 0x408) == 0) && ((n3 & 0x408) == 0) && ((n4 & 0x408) == 0) && ((n5 & 0x408) == 0) && ((n6 & 0x408) == 0) && ((n7 & 0x408) == 0))
						{
							*p2 &= 0xFC;
							*p3 &= 0xFC;
							*p4 &= 0xFC;
							*p5 &= 0xFC;
							*p6 &= 0xFC;
							*p7 &= 0xFC;
							*(p2 + 1) &= 0xF;
							*(p3 + 1) &= 0xF;
							*(p4 + 1) &= 0xF;
							*(p5 + 1) &= 0xF;
							*(p6 + 1) &= 0xF;
							*(p7 + 1) &= 0xF;
						}
						if (((n1 & 0x3FC) == 0) && ((n8 & 0x3FC) == 0) && ((n2 & 0x204) == 0) && ((n3 & 0x204) == 0) && ((n4 & 0x204) == 0) && ((n5 & 0x204) == 0) && ((n6 & 0x204) == 0) && ((n7 & 0x204) == 0))
						{
							*p2 &= 0xFE;
							*p3 &= 0xFE;
							*p4 &= 0xFE;
							*p5 &= 0xFE;
							*p6 &= 0xFE;
							*p7 &= 0xFE;
							*(p2 + 1) &= 0x7;
							*(p3 + 1) &= 0x7;
							*(p4 + 1) &= 0x7;
							*(p5 + 1) &= 0x7;
							*(p6 + 1) &= 0x7;
							*(p7 + 1) &= 0x7;
						}
						if (((n1 & 0x1FE) == 0) && ((n8 & 0x1FE) == 0) && ((n2 & 0x102) == 0) && ((n3 & 0x102) == 0) && ((n4 & 0x102) == 0) && ((n5 & 0x102) == 0) && ((n6 & 0x102) == 0) && ((n7 & 0x102) == 0))
						{
							*(p2 + 1) &= 0x3;
							*(p3 + 1) &= 0x3;
							*(p4 + 1) &= 0x3;
							*(p5 + 1) &= 0x3;
							*(p6 + 1) &= 0x3;
							*(p7 + 1) &= 0x3;
						}

						p1++;
						p2++;
						p3++;
						p4++;
						p5++;
						p6++;
						p7++;
						p8++;
					}
				}

				//first comumn
				for (y = -1; y < h - 6; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = p3 + stride;
					p5 = p4 + stride;
					p6 = p5 + stride;
					p7 = p6 + stride;
					p8 = (y < h - 7) ? p7 + stride : p7;

					n1 = (y == -1) ? 0 : *p1;
					n2 = *p2;
					n3 = *p3;
					n4 = *p4;
					n5 = *p5;
					n6 = *p6;
					n7 = *p7;
					n8 = (y < h - 7) ? *p8 : 0;

					if (((n1 & 0xFE) == 0) && ((n8 & 0xFE) == 0) && ((n2 & 0x2) == 0) && ((n3 & 0x2) == 0) && ((n4 & 0x2) == 0) && ((n5 & 0x2) == 0) && ((n6 & 0x2) == 0) && ((n7 & 0x2) == 0))
					{
						*p2 &= 0x3;
						*p3 &= 0x3;
						*p4 &= 0x3;
						*p5 &= 0x3;
						*p6 &= 0x3;
						*p7 &= 0x3;
					}
				}

				//last column
				x = w - 1;
				for (y = -5; y < h - 2; y++)
					if (IsPixelWhite(pSource, stride, x - 6, y, w, h) && IsPixelWhite(pSource, stride, x - 5, y, w, h) && IsPixelWhite(pSource, stride, x - 4, y, w, h) && IsPixelWhite(pSource, stride, x - 3, y, w, h) && IsPixelWhite(pSource, stride, x - 2, y, w, h) && IsPixelWhite(pSource, stride, x - 1, y, w, h) && IsPixelWhite(pSource, stride, x, y, w, h) &&
						IsPixelWhite(pSource, stride, x - 6, y + 1, w, h) &&
						IsPixelWhite(pSource, stride, x - 6, y + 2, w, h) &&
						IsPixelWhite(pSource, stride, x - 6, y + 3, w, h) &&
						IsPixelWhite(pSource, stride, x - 6, y + 4, w, h) &&
						IsPixelWhite(pSource, stride, x - 6, y + 5, w, h) &&
						IsPixelWhite(pSource, stride, x - 6, y + 6, w, h) &&
						IsPixelWhite(pSource, stride, x - 6, y + 7, w, h) && IsPixelWhite(pSource, stride, x - 5, y + 7, w, h) && IsPixelWhite(pSource, stride, x - 4, y + 7, w, h) && IsPixelWhite(pSource, stride, x - 3, y + 7, w, h) && IsPixelWhite(pSource, stride, x - 2, y + 7, w, h) && IsPixelWhite(pSource, stride, x - 1, y + 7, w, h) && IsPixelWhite(pSource, stride, x, y + 7, w, h))
					{
						for (int i = 0; i <= 5; i++)
							for (int j = 1; j <= 6; j++)
								MakePixelWhite(pSource, stride, x - i, y + j, w, h);
					}
			}
		}
		#endregion

		#region Despeckle5x5()
		private void Despeckle5x5(BitmapData bmpData)
		{
			int stride = bmpData.Stride;
			int n1, n2, n3, n4, n5, n6, n7;
			int width = bmpData.Width;
			int height = bmpData.Height;
			int x, y;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				byte* p1, p2, p3, p4, p5, p6, p7;

				for (y = -1; y < height - 5; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = p3 + stride;
					p5 = p4 + stride;
					p6 = p5 + stride;
					p7 = (y < height - 6) ? p6 + stride : p6;

					for (x = 0; x < stride - 1; x++)
					{
						n1 = (y == -1) ? 0 : (int)(((*p1) * 256) + (*(p1 + 1)));
						n2 = (int)(((*p2) * 256) + (*(p2 + 1)));
						n3 = (int)(((*p3) * 256) + (*(p3 + 1)));
						n4 = (int)(((*p4) * 256) + (*(p4 + 1)));
						n5 = (int)(((*p5) * 256) + (*(p5 + 1)));
						n6 = (int)(((*p6) * 256) + (*(p6 + 1)));
						n7 = (y < height - 6) ? (int)(((*p7) * 256) + (*(p7 + 1))) : 0;

						if (((n1 & 0xFE00) == 0) && ((n7 & 0xFE00) == 0) && ((n2 & 0x8200) == 0) && ((n3 & 0x8200) == 0) && ((n4 & 0x8200) == 0) && ((n5 & 0x8200) == 0) && ((n6 & 0x8200) == 0))
						{
							*p2 &= 0x83;
							*p3 &= 0x83;
							*p4 &= 0x83;
							*p5 &= 0x83;
							*p6 &= 0x83;
						}
						if (((n1 & 0x7F00) == 0) && ((n7 & 0x7F00) == 0) && ((n2 & 0x4100) == 0) && ((n3 & 0x4100) == 0) && ((n4 & 0x4100) == 0) && ((n5 & 0x4100) == 0) && ((n6 & 0x4100) == 0))
						{
							*p2 &= 0xC1;
							*p3 &= 0xC1;
							*p4 &= 0xC1;
							*p5 &= 0xC1;
							*p6 &= 0xC1;
						}
						if (((n1 & 0x3F80) == 0) && ((n7 & 0x3F80) == 0) && ((n2 & 0x2080) == 0) && ((n3 & 0x2080) == 0) && ((n4 & 0x2080) == 0) && ((n5 & 0x2080) == 0) && ((n6 & 0x2080) == 0))
						{
							*p2 &= 0xE0;
							*p3 &= 0xE0;
							*p4 &= 0xE0;
							*p5 &= 0xE0;
							*p6 &= 0xE0;
						}
						if (((n1 & 0x1FC0) == 0) && ((n7 & 0x1FC0) == 0) && ((n2 & 0x1040) == 0) && ((n3 & 0x1040) == 0) && ((n4 & 0x1040) == 0) && ((n5 & 0x1040) == 0) && ((n6 & 0x1040) == 0))
						{
							*p2 &= 0xF0;
							*p3 &= 0xF0;
							*p4 &= 0xF0;
							*p5 &= 0xF0;
							*p6 &= 0xF0;
							*(p2 + 1) &= 0x7F;
							*(p3 + 1) &= 0x7F;
							*(p4 + 1) &= 0x7F;
							*(p5 + 1) &= 0x7F;
							*(p6 + 1) &= 0x7F;
						}
						if (((n1 & 0xFE0) == 0) && ((n7 & 0xFE0) == 0) && ((n2 & 0x820) == 0) && ((n3 & 0x820) == 0) && ((n4 & 0x820) == 0) && ((n5 & 0x820) == 0) && ((n6 & 0x820) == 0))
						{
							*p2 &= 0xF8;
							*p3 &= 0xF8;
							*p4 &= 0xF8;
							*p5 &= 0xF8;
							*p6 &= 0xF8;
							*(p2 + 1) &= 0x3F;
							*(p3 + 1) &= 0x3F;
							*(p4 + 1) &= 0x3F;
							*(p5 + 1) &= 0x3F;
							*(p6 + 1) &= 0x3F;
						}
						if (((n1 & 0x7F0) == 0) && ((n7 & 0x7F0) == 0) && ((n2 & 0x410) == 0) && ((n3 & 0x410) == 0) && ((n4 & 0x410) == 0) && ((n5 & 0x410) == 0) && ((n6 & 0x410) == 0))
						{
							*p2 &= 0xFC;
							*p3 &= 0xFC;
							*p4 &= 0xFC;
							*p5 &= 0xFC;
							*p6 &= 0xFC;
							*(p2 + 1) &= 0x1F;
							*(p3 + 1) &= 0x1F;
							*(p4 + 1) &= 0x1F;
							*(p5 + 1) &= 0x1F;
							*(p6 + 1) &= 0x1F;
						}
						if (((n1 & 0x3F8) == 0) && ((n7 & 0x3F8) == 0) && ((n2 & 0x208) == 0) && ((n3 & 0x208) == 0) && ((n4 & 0x208) == 0) && ((n5 & 0x208) == 0) && ((n6 & 0x208) == 0))
						{
							*p2 &= 0xFE;
							*p3 &= 0xFE;
							*p4 &= 0xFE;
							*p5 &= 0xFE;
							*p6 &= 0xFE;
							*(p2 + 1) &= 0xF;
							*(p3 + 1) &= 0xF;
							*(p4 + 1) &= 0xF;
							*(p5 + 1) &= 0xF;
							*(p6 + 1) &= 0xF;
						}
						if (((n1 & 0x1FC) == 0) && ((n7 & 0x1FC) == 0) && ((n2 & 0x104) == 0) && ((n3 & 0x104) == 0) && ((n4 & 0x104) == 0) && ((n5 & 0x104) == 0) && ((n6 & 0x104) == 0))
						{
							*(p2 + 1) &= 0x7;
							*(p3 + 1) &= 0x7;
							*(p4 + 1) &= 0x7;
							*(p5 + 1) &= 0x7;
							*(p6 + 1) &= 0x7;
						}

						p1++;
						p2++;
						p3++;
						p4++;
						p5++;
						p6++;
						p7++;
					}
				}

				//first column
				for (y = -1; y < height - 5; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = p3 + stride;
					p5 = p4 + stride;
					p6 = p5 + stride;
					p7 = (y < height - 6) ? p6 + stride : p6;

					n1 = (y == -1) ? 0 : *p1;
					n2 = *p2;
					n3 = *p3;
					n4 = *p4;
					n5 = *p5;
					n6 = *p6;
					n7 = (y < height - 6) ? *p7 : 0;

					if (((n1 & 0xFC) == 0) && ((n7 & 0xFC) == 0) && ((n2 & 0x4) == 0) && ((n3 & 0x4) == 0) && ((n4 & 0x4) == 0) && ((n5 & 0x4) == 0) && ((n6 & 0x4) == 0))
					{
						*p2 &= 0x7;
						*p3 &= 0x7;
						*p4 &= 0x7;
						*p5 &= 0x7;
						*p6 &= 0x7;
					}
				}

				//last column
				x = width - 1;
				for (y = 0; y < height - 6; y++)
					if (IsPixelWhite(pSource, stride, x - 5, y) && IsPixelWhite(pSource, stride, x - 4, y) && IsPixelWhite(pSource, stride, x - 3, y) && IsPixelWhite(pSource, stride, x - 2, y) && IsPixelWhite(pSource, stride, x - 1, y) && IsPixelWhite(pSource, stride, x, y) &&
						IsPixelWhite(pSource, stride, x - 5, y + 1) &&
						IsPixelWhite(pSource, stride, x - 5, y + 2) &&
						IsPixelWhite(pSource, stride, x - 5, y + 3) &&
						IsPixelWhite(pSource, stride, x - 5, y + 4) &&
						IsPixelWhite(pSource, stride, x - 5, y + 5) &&
						IsPixelWhite(pSource, stride, x - 5, y + 6) && IsPixelWhite(pSource, stride, x - 4, y + 6) && IsPixelWhite(pSource, stride, x - 3, y + 6) && IsPixelWhite(pSource, stride, x - 2, y + 6) && IsPixelWhite(pSource, stride, x - 1, y + 6) && IsPixelWhite(pSource, stride, x, y + 6))
					{
						MakePixelWhite(pSource, stride, x - 4, y + 1);
						MakePixelWhite(pSource, stride, x - 3, y + 1);
						MakePixelWhite(pSource, stride, x - 2, y + 1);
						MakePixelWhite(pSource, stride, x - 1, y + 1);
						MakePixelWhite(pSource, stride, x, y + 1);

						MakePixelWhite(pSource, stride, x - 4, y + 2);
						MakePixelWhite(pSource, stride, x - 3, y + 2);
						MakePixelWhite(pSource, stride, x - 2, y + 2);
						MakePixelWhite(pSource, stride, x - 1, y + 2);
						MakePixelWhite(pSource, stride, x, y + 2);

						MakePixelWhite(pSource, stride, x - 4, y + 3);
						MakePixelWhite(pSource, stride, x - 3, y + 3);
						MakePixelWhite(pSource, stride, x - 2, y + 3);
						MakePixelWhite(pSource, stride, x - 1, y + 3);
						MakePixelWhite(pSource, stride, x, y + 3);

						MakePixelWhite(pSource, stride, x - 4, y + 4);
						MakePixelWhite(pSource, stride, x - 3, y + 4);
						MakePixelWhite(pSource, stride, x - 2, y + 4);
						MakePixelWhite(pSource, stride, x - 1, y + 4);
						MakePixelWhite(pSource, stride, x, y + 4);

						MakePixelWhite(pSource, stride, x - 4, y + 5);
						MakePixelWhite(pSource, stride, x - 3, y + 5);
						MakePixelWhite(pSource, stride, x - 2, y + 5);
						MakePixelWhite(pSource, stride, x - 1, y + 5);
						MakePixelWhite(pSource, stride, x, y + 5);
					}
			}
		}
		#endregion

		#region Despeckle4x4()
		private void Despeckle4x4(BitmapData bmpData)
		{
			int stride = bmpData.Stride;
			int n1, n2, n3, n4, n5, n6;
			int width = bmpData.Width;
			int height = bmpData.Height;
			int x, y;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				byte* p1, p2, p3, p4, p5, p6;

				for (y = -1; y < height - 4; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = p3 + stride;
					p5 = p4 + stride;
					p6 = (y < height - 5) ? p5 + stride : p5;

					for (x = 0; x < stride - 1; x++)
					{
						n1 = (y == -1) ? 0 : (int)(((*p1) * 256) + (*(p1 + 1)));
						n2 = (int)(((*p2) * 256) + (*(p2 + 1)));
						n3 = (int)(((*p3) * 256) + (*(p3 + 1)));
						n4 = (int)(((*p4) * 256) + (*(p4 + 1)));
						n5 = (int)(((*p5) * 256) + (*(p5 + 1)));
						n6 = (y < height - 5) ? (int)(((*p6) * 256) + (*(p6 + 1))) : 0;

						if (((n1 & 0xFC00) == 0) && ((n6 & 0xFC00) == 0) && ((n2 & 0x8400) == 0) && ((n3 & 0x8400) == 0) && ((n4 & 0x8400) == 0) && ((n5 & 0x8400) == 0))
						{
							*p2 &= 0x87;
							*p3 &= 0x87;
							*p4 &= 0x87;
							*p5 &= 0x87;
						}

						if (((n1 & 0x7E00) == 0) && ((n6 & 0x7E00) == 0) && ((n2 & 0x4200) == 0) && ((n3 & 0x4200) == 0) && ((n4 & 0x4200) == 0) && ((n5 & 0x4200) == 0))
						{
							*p2 &= 0xC3;
							*p3 &= 0xC3;
							*p4 &= 0xC3;
							*p5 &= 0xC3;
						}

						if (((n1 & 0x3F00) == 0) && ((n6 & 0x3F00) == 0) && ((n2 & 0x2100) == 0) && ((n3 & 0x2100) == 0) && ((n4 & 0x2100) == 0) && ((n5 & 0x2100) == 0))
						{
							*p2 &= 0xE1;
							*p3 &= 0xE1;
							*p4 &= 0xE1;
							*p5 &= 0xE1;
						}

						if (((n1 & 0x1F80) == 0) && ((n6 & 0x1F80) == 0) && ((n2 & 0x1080) == 0) && ((n3 & 0x1080) == 0) && ((n4 & 0x1080) == 0) && ((n5 & 0x1080) == 0))
						{
							*p2 &= 0xF0;
							*p3 &= 0xF0;
							*p4 &= 0xF0;
							*p5 &= 0xF0;
						}

						if (((n1 & 0xFC0) == 0) && ((n6 & 0xFC0) == 0) && ((n2 & 0x840) == 0) && ((n3 & 0x840) == 0) && ((n4 & 0x840) == 0) && ((n5 & 0x840) == 0))
						{
							*p2 &= 0xF8;
							*p3 &= 0xF8;
							*p4 &= 0xF8;
							*p5 &= 0xF8;
							*(p2 + 1) &= 0x7F;
							*(p3 + 1) &= 0x7F;
							*(p4 + 1) &= 0x7F;
							*(p5 + 1) &= 0x7F;
						}

						if (((n1 & 0x7E0) == 0) && ((n6 & 0x7E0) == 0) && ((n2 & 0x420) == 0) && ((n3 & 0x420) == 0) && ((n4 & 0x420) == 0) && ((n5 & 0x420) == 0))
						{
							*p2 &= 0xFC;
							*p3 &= 0xFC;
							*p4 &= 0xFC;
							*p5 &= 0xFC;
							*(p2 + 1) &= 0x3F;
							*(p3 + 1) &= 0x3F;
							*(p4 + 1) &= 0x3F;
							*(p5 + 1) &= 0x3F;
						}

						if (((n1 & 0x3F0) == 0) && ((n6 & 0x3F0) == 0) && ((n2 & 0x210) == 0) && ((n3 & 0x210) == 0) && ((n4 & 0x210) == 0) && ((n5 & 0x210) == 0))
						{
							*p2 &= 0xFE;
							*p3 &= 0xFE;
							*p4 &= 0xFE;
							*p5 &= 0xFE;
							*(p2 + 1) &= 0x1F;
							*(p3 + 1) &= 0x1F;
							*(p4 + 1) &= 0x1F;
							*(p5 + 1) &= 0x1F;
						}

						if (((n1 & 0x1F8) == 0) && ((n6 & 0x1F8) == 0) && ((n2 & 0x108) == 0) && ((n3 & 0x108) == 0) && ((n4 & 0x108) == 0) && ((n5 & 0x108) == 0))
						{
							*(p2 + 1) &= 0xF;
							*(p3 + 1) &= 0xF;
							*(p4 + 1) &= 0xF;
							*(p5 + 1) &= 0xF;
						}

						p1++;
						p2++;
						p3++;
						p4++;
						p5++;
						p6++;
					}
				}

				//first column
				for (y = -1; y < height - 4; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = p3 + stride;
					p5 = p4 + stride;
					p6 = (y < height - 5) ? p5 + stride : p5;

					n1 = (y == -1) ? 0 : *p1;
					n2 = *p2;
					n3 = *p3;
					n4 = *p4;
					n5 = *p5;
					n6 = (y < height - 5) ? *p6 : 0;

					if (((n1 & 0xF8) == 0) && ((n6 & 0xF8) == 0) && ((n2 & 0x8) == 0) && ((n3 & 0x8) == 0) && ((n4 & 0x8) == 0) && ((n5 & 0x8) == 0))
					{
						*p2 &= 0xF;
						*p3 &= 0xF;
						*p4 &= 0xF;
						*p5 &= 0xF;
					}
				}

				//last column
				x = width - 1;
				for (y = 0; y < height - 6; y++)
					if (IsPixelWhite(pSource, stride, x - 4, y) && IsPixelWhite(pSource, stride, x - 3, y) && IsPixelWhite(pSource, stride, x - 2, y) && IsPixelWhite(pSource, stride, x - 1, y) && IsPixelWhite(pSource, stride, x, y) &&
						IsPixelWhite(pSource, stride, x - 4, y + 1) &&
						IsPixelWhite(pSource, stride, x - 4, y + 2) &&
						IsPixelWhite(pSource, stride, x - 4, y + 3) &&
						IsPixelWhite(pSource, stride, x - 4, y + 4) &&
						IsPixelWhite(pSource, stride, x - 4, y + 5) && IsPixelWhite(pSource, stride, x - 3, y + 5) && IsPixelWhite(pSource, stride, x - 2, y + 5) && IsPixelWhite(pSource, stride, x - 1, y + 5) && IsPixelWhite(pSource, stride, x, y + 5))
					{
						MakePixelWhite(pSource, stride, x - 3, y + 1);
						MakePixelWhite(pSource, stride, x - 2, y + 1);
						MakePixelWhite(pSource, stride, x - 1, y + 1);
						MakePixelWhite(pSource, stride, x, y + 1);

						MakePixelWhite(pSource, stride, x - 3, y + 2);
						MakePixelWhite(pSource, stride, x - 2, y + 2);
						MakePixelWhite(pSource, stride, x - 1, y + 2);
						MakePixelWhite(pSource, stride, x, y + 2);

						MakePixelWhite(pSource, stride, x - 3, y + 3);
						MakePixelWhite(pSource, stride, x - 2, y + 3);
						MakePixelWhite(pSource, stride, x - 1, y + 3);
						MakePixelWhite(pSource, stride, x, y + 3);

						MakePixelWhite(pSource, stride, x - 3, y + 4);
						MakePixelWhite(pSource, stride, x - 2, y + 4);
						MakePixelWhite(pSource, stride, x - 1, y + 4);
						MakePixelWhite(pSource, stride, x, y + 4);
					}
			}
		}
		#endregion

		#region Despeckle3x3()
		private static void Despeckle3x3(BitmapData bmpData)
		{
			int stride = bmpData.Stride;
			int width = bmpData.Width;
			int height = bmpData.Height;
			int x, y;
			int n1, n2, n3, n4, n5;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				byte* p1, p2, p3, p4, p5;

				for (y = -1; y < height - 3; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = p3 + stride;
					p5 = (y < height - 4) ? p4 + stride : p3;

					for (x = 0; x < stride - 1; x++)
					{
						n1 = (y == -1) ? 0 : (int)(((*p1) * 256) + (*(p1 + 1)));
						n2 = (int)(((*p2) * 256) + (*(p2 + 1)));
						n3 = (int)(((*p3) * 256) + (*(p3 + 1)));
						n4 = (int)(((*p4) * 256) + (*(p4 + 1)));
						n5 = (y < height - 4) ? (int)(((*p5) * 256) + (*(p5 + 1))) : 0;

						if (((n1 & 0xF800) == 0) && ((n5 & 0xF800) == 0) && ((n2 & 0x8800) == 0) && ((n3 & 0x8800) == 0) && ((n4 & 0x8800) == 0))
						{
							*p2 &= 0x8F;
							*p3 &= 0x8F;
							*p4 &= 0x8F;
						}

						if (((n1 & 0x7C00) == 0) && ((n5 & 0x7C00) == 0) && ((n2 & 0x4400) == 0) && ((n3 & 0x4400) == 0) && ((n4 & 0x4400) == 0))
						{
							*p2 &= 0xC7;
							*p3 &= 0xC7;
							*p4 &= 0xC7;
						}

						if (((n1 & 0x3E00) == 0) && ((n5 & 0x3E00) == 0) && ((n2 & 0x2200) == 0) && ((n3 & 0x2200) == 0) && ((n4 & 0x2200) == 0))
						{
							*p2 &= 0xE3;
							*p3 &= 0xE3;
							*p4 &= 0xE3;
						}

						if (((n1 & 0x1F00) == 0) && ((n5 & 0x1F00) == 0) && ((n2 & 0x1100) == 0) && ((n3 & 0x1100) == 0) && ((n4 & 0x1100) == 0))
						{
							*p2 &= 0xF1;
							*p3 &= 0xF1;
							*p4 &= 0xF1;
						}

						if (((n1 & 0xF80) == 0) && ((n5 & 0xF80) == 0) && ((n2 & 0x880) == 0) && ((n3 & 0x880) == 0) && ((n4 & 0x880) == 0))
						{
							*p2 &= 0xF8;
							*p3 &= 0xF8;
							*p4 &= 0xF8;
						}

						if (((n1 & 0x7C0) == 0) && ((n5 & 0x7C0) == 0) && ((n2 & 0x440) == 0) && ((n3 & 0x440) == 0) && ((n4 & 0x440) == 0))
						{
							*p2 &= 0xFC;
							*p3 &= 0xFC;
							*p4 &= 0xFC;
							*(p2 + 1) &= 0x7F;
							*(p3 + 1) &= 0x7F;
							*(p4 + 1) &= 0x7F;
						}

						if (((n1 & 0x3E0) == 0) && ((n5 & 0x3E0) == 0) && ((n2 & 0x220) == 0) && ((n3 & 0x220) == 0) && ((n4 & 0x220) == 0))
						{
							*p2 &= 0xFE;
							*p3 &= 0xFE;
							*p4 &= 0xFE;
							*(p2 + 1) &= 0x3F;
							*(p3 + 1) &= 0x3F;
							*(p4 + 1) &= 0x3F;
						}

						if (((n1 & 0x1F0) == 0) && ((n5 & 0x1F0) == 0) && ((n2 & 0x110) == 0) && ((n3 & 0x110) == 0) && ((n4 & 0x110) == 0))
						{
							*(p2 + 1) &= 0x1F;
							*(p3 + 1) &= 0x1F;
							*(p4 + 1) &= 0x1F;
						}

						p1++;
						p2++;
						p3++;
						p4++;
						p5++;
					}
				}

				//first column
				for (y = -1; y < height - 3; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = p3 + stride;
					p5 = (y < height - 4) ? p4 + stride : p3;

					n1 = (y == -1) ? 0 : *p1;
					n2 = *p2;
					n3 = *p3;
					n4 = *p4;
					n5 = (y < height - 4) ? *p5 : 0;

					if (((n1 & 0xF) == 0) && ((n5 & 0xF) == 0) && ((n2 & 0x10) == 0) && ((n3 & 0x10) == 0) && ((n4 & 0x10) == 0))
					{
						*p2 &= 0x1F;
						*p3 &= 0x1F;
						*p4 &= 0x1F;
					}
				}

				//last column
				x = width - 1;
				for (y = 0; y < height - 5; y++)
					if (IsPixelWhite(pSource, stride, x - 3, y) && IsPixelWhite(pSource, stride, x - 2, y) && IsPixelWhite(pSource, stride, x - 1, y) && IsPixelWhite(pSource, stride, x, y) &&
						IsPixelWhite(pSource, stride, x - 3, y + 1) &&
						IsPixelWhite(pSource, stride, x - 3, y + 2) &&
						IsPixelWhite(pSource, stride, x - 3, y + 3) &&
						IsPixelWhite(pSource, stride, x - 3, y + 4) && IsPixelWhite(pSource, stride, x - 2, y + 4) && IsPixelWhite(pSource, stride, x - 1, y + 4) && IsPixelWhite(pSource, stride, x, y + 4))
					{
						MakePixelWhite(pSource, stride, x - 2, y + 1);
						MakePixelWhite(pSource, stride, x - 1, y + 1);
						MakePixelWhite(pSource, stride, x, y + 1);
						MakePixelWhite(pSource, stride, x - 2, y + 2);
						MakePixelWhite(pSource, stride, x - 1, y + 2);
						MakePixelWhite(pSource, stride, x, y + 2);
						MakePixelWhite(pSource, stride, x - 2, y + 3);
						MakePixelWhite(pSource, stride, x - 1, y + 3);
						MakePixelWhite(pSource, stride, x, y + 3);
					}

			}
		}
		#endregion

		#region Despeckle2x2()
		private void Despeckle2x2(BitmapData bmpData)
		{
			int stride = bmpData.Stride;
			int width = bmpData.Width;
			int height = bmpData.Height;
			int n1, n2, n3, n4;
			int x, y;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				byte* p1, p2, p3, p4;

				for (y = -1; y < height - 2; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = (y < height - 3) ? p3 + stride : p2;

					for (x = 0; x < stride - 1; x++)
					{
						n1 = (y == -1) ? 0 : (int)(((*p1) * 256) + (*(p1 + 1)));
						n2 = (int)(((*p2) * 256) + (*(p2 + 1)));
						n3 = (int)(((*p3) * 256) + (*(p3 + 1)));
						n4 = (y < height - 3) ? (int)(((*p4) * 256) + (*(p4 + 1))) : 0;

						if (((n1 & 0xF000) == 0) && ((n4 & 0xF000) == 0) && ((n2 & 0x9000) == 0) && ((n3 & 0x9000) == 0))
						{
							*p2 &= 0x9F;
							*p3 &= 0x9F;
						}

						if (((n1 & 0x7800) == 0) && ((n4 & 0x7800) == 0) && ((n2 & 0x4800) == 0) && ((n3 & 0x4800) == 0))
						{
							*p2 &= 0xCF;
							*p3 &= 0xCF;
						}

						if (((n1 & 0x3C00) == 0) && ((n4 & 0x3C00) == 0) && ((n2 & 0x2400) == 0) && ((n3 & 0x2400) == 0))
						{
							*p2 &= 0xE7;
							*p3 &= 0xE7;
						}

						if (((n1 & 0x1E00) == 0) && ((n4 & 0x1E00) == 0) && ((n2 & 0x1200) == 0) && ((n3 & 0x1200) == 0))
						{
							*p2 &= 0xF3;
							*p3 &= 0xF3;
						}

						if (((n1 & 0x0F00) == 0) && ((n4 & 0x0F00) == 0) && ((n2 & 0x0900) == 0) && ((n3 & 0x0900) == 0))
						{
							*p2 &= 0xF9;
							*p3 &= 0xF9;
						}

						if (((n1 & 0x780) == 0) && ((n4 & 0x780) == 0) && ((n2 & 0x480) == 0) && ((n3 & 0x480) == 0))
						{
							*p2 &= 0xFC;
							*p3 &= 0xFC;
						}

						if (((n1 & 0x3C0) == 0) && ((n4 & 0x3C0) == 0) && ((n2 & 0x240) == 0) && ((n3 & 0x240) == 0))
						{
							*p2 &= 0xFE;
							*p3 &= 0xFE;
							*(p2 + 1) &= 0x7F;
							*(p3 + 1) &= 0x7F;
						}

						if (((n1 & 0x1E0) == 0) && ((n4 & 0x1E0) == 0) && ((n2 & 0x120) == 0) && ((n3 & 0x120) == 0))
						{
							*(p2 + 1) &= 0x3F;
							*(p3 + 1) &= 0x3F;
						}

						p1++;
						p2++;
						p3++;
						p4++;
					}
				}

				//first column
				for (y = -1; y < height - 2; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = p2 + stride;
					p4 = (y < height - 3) ? p3 + stride : p2;

					n1 = (y == -1) ? 0 : *p1;
					n2 = *p2;
					n3 = *p3;
					n4 = (y < height - 3) ? *p4 : 0;

					if (((n1 & 0xE0) == 0) && ((n4 & 0x20) == 0) && ((n2 & 0x20) == 0) && ((n3 & 0x20) == 0))
					{
						*p2 &= 0x3F;
						*p3 &= 0x3F;
					}
				}

				//last column
				x = width - 1;
				for (y = 1; y < height - 2; y++)
					if (IsPixelWhite(pSource, stride, x - 2, y - 1) && IsPixelWhite(pSource, stride, x - 1, y - 1) && IsPixelWhite(pSource, stride, x, y - 1) &&
						IsPixelWhite(pSource, stride, x - 2, y) &&
						IsPixelWhite(pSource, stride, x - 2, y + 1) &&
						IsPixelWhite(pSource, stride, x - 2, y + 2) && IsPixelWhite(pSource, stride, x - 1, y + 2) && IsPixelWhite(pSource, stride, x, y + 2))
					{
						MakePixelWhite(pSource, stride, x - 1, y);
						MakePixelWhite(pSource, stride, x, y);
						MakePixelWhite(pSource, stride, x - 1, y + 1);
						MakePixelWhite(pSource, stride, x, y + 1);
					}

				//ur corner
				x = width - 1;
				y = 0;
				if (IsPixelWhite(pSource, stride, x - 2, y) &&
					IsPixelWhite(pSource, stride, x - 2, y + 1) &&
					IsPixelWhite(pSource, stride, x - 2, y + 2) && IsPixelWhite(pSource, stride, x - 1, y + 2) && IsPixelWhite(pSource, stride, x, y + 2))
				{
					MakePixelWhite(pSource, stride, x - 1, y);
					MakePixelWhite(pSource, stride, x, y);
					MakePixelWhite(pSource, stride, x - 1, y + 1);
					MakePixelWhite(pSource, stride, x, y + 1);
				}

				//lr corner
				x = width - 1;
				y = height - 2;
				if (IsPixelWhite(pSource, stride, x - 2, y - 1) && IsPixelWhite(pSource, stride, x - 1, y - 1) && IsPixelWhite(pSource, stride, x, y - 1) &&
					IsPixelWhite(pSource, stride, x - 2, y) &&
					IsPixelWhite(pSource, stride, x - 2, y + 1))
				{
					MakePixelWhite(pSource, stride, x - 1, y);
					MakePixelWhite(pSource, stride, x, y);
					MakePixelWhite(pSource, stride, x - 1, y + 1);
					MakePixelWhite(pSource, stride, x, y + 1);
				}
			}
		}
		#endregion

		#region Despeckle1x1()
		private static void Despeckle1x1(BitmapData bmpData)
		{
			int stride = bmpData.Stride;
			int width = bmpData.Width;
			int height = bmpData.Height;
			int n1, n2, n3;
			int x, y;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				byte* p1, p2, p3;

				for (y = 0; y < height - 2; y++)
				{
					p1 = pSource + (y * stride);
					p2 = p1 + stride;
					p3 = p2 + stride;

					for (x = 0; x < stride - 1; x++)
					{
						n1 = (p1[0] * 256 + p1[1]);
						n2 = (p2[0] * 256 + p2[1]);
						n3 = (p3[0] * 256 + p3[1]);					

						if (((n1 & 0xE000) == 0) && ((n2 & 0xA000) == 0) && ((n3 & 0xE000) == 0))
							*p2 &= 0xBF;

						if (((n1 & 0x7000) == 0) && ((n2 & 0x5000) == 0) && ((n3 & 0x7000) == 0))
							*p2 &= 0xDF;

						if (((n1 & 0x3800) == 0) && ((n2 & 0x2800) == 0) && ((n3 & 0x3800) == 0))
							*p2 &= 0xEF;

						if (((n1 & 0x1C00) == 0) && ((n2 & 0x1400) == 0) && ((n3 & 0x1C00) == 0))
							*p2 &= 0xF7;

						if (((n1 & 0xE00) == 0) && ((n2 & 0xA00) == 0) && ((n3 & 0xE00) == 0))
							*p2 &= 0xFB;

						if (((n1 & 0x700) == 0) && ((n2 & 0x500) == 0) && ((n3 & 0x700) == 0))
							*p2 &= 0xFD;

						if (((n1 & 0x380) == 0) && ((n2 & 0x280) == 0) && ((n3 & 0x380) == 0))
							*p2 &= 0xFE;

						if (((n1 & 0x1C0) == 0) && ((n2 & 0x140) == 0) && ((n3 & 0x1C0) == 0))
							*(p2 + 1) &= 0x7F;

						p1++;
						p2++;
						p3++;
					}
				}

				//first column
				for (y = 1; y < height - 1; y++)
					if (IsPixelWhite(pSource, stride, 0, y - 1) && IsPixelWhite(pSource, stride, 1, y - 1) && IsPixelWhite(pSource, stride, 1, y) && IsPixelWhite(pSource, stride, 0, y + 1) && IsPixelWhite(pSource, stride, 1, y + 1))
						MakePixelWhite(pSource, stride, 0, y);

				//last column
				x = width - 1;
				for (y = 1; y < height - 1; y++)
					if (IsPixelWhite(pSource, stride, x - 1, y - 1) && IsPixelWhite(pSource, stride, x, y - 1) && IsPixelWhite(pSource, stride, x - 1, y) && IsPixelWhite(pSource, stride, x - 1, y + 1) && IsPixelWhite(pSource, stride, x, y + 1))
						MakePixelWhite(pSource, stride, x, y);

				//first row
				y = 0;
				p2 = pSource;
				p3 = pSource + stride;

				for (x = 0; x < stride - 1; x++)
				{
					n2 = (int)(((*p2) * 256) + (*(p2 + 1)));
					n3 = (int)(((*p3) * 256) + (*(p3 + 1)));

					if (((n2 & 0xA000) == 0) && ((n3 & 0xE000) == 0))
						*p2 &= 0xBF;

					if (((n2 & 0x5000) == 0) && ((n3 & 0x7000) == 0))
						*p2 &= 0xDF;

					if (((n2 & 0x2800) == 0) && ((n3 & 0x3800) == 0))
						*p2 &= 0xEF;

					if (((n2 & 0x1400) == 0) && ((n3 & 0x1C00) == 0))
						*p2 &= 0xF7;

					if (((n2 & 0xA00) == 0) && ((n3 & 0xE00) == 0))
						*p2 &= 0xFB;

					if (((n2 & 0x500) == 0) && ((n3 & 0x700) == 0))
						*p2 &= 0xFD;

					if (((n2 & 0x280) == 0) && ((n3 & 0x380) == 0))
						*p2 &= 0xFE;

					if (((n2 & 0x140) == 0) && ((n3 & 0x1C0) == 0))
						*(p2 + 1) &= 0x7F;

					p2++;
					p3++;
				}

				//last row
				y = height - 1;
				p1 = pSource + ((y-1) * stride);
				p2 = pSource + (y * stride);

				for (x = 0; x < stride - 1; x++)
				{
					n1 = (int)(((*p1) * 256) + (*(p1 + 1)));
					n2 = (int)(((*p2) * 256) + (*(p2 + 1)));

					if (((n1 & 0xE000) == 0) && ((n2 & 0xA000) == 0))
						*p2 &= 0xBF;

					if (((n1 & 0x7000) == 0) && ((n2 & 0x5000) == 0))
						*p2 &= 0xDF;

					if (((n1 & 0x3800) == 0) && ((n2 & 0x2800) == 0))
						*p2 &= 0xEF;

					if (((n1 & 0x1C00) == 0) && ((n2 & 0x1400) == 0))
						*p2 &= 0xF7;

					if (((n1 & 0xE00) == 0) && ((n2 & 0xA00) == 0))
						*p2 &= 0xFB;

					if (((n1 & 0x700) == 0) && ((n2 & 0x500) == 0))
						*p2 &= 0xFD;

					if (((n1 & 0x380) == 0) && ((n2 & 0x280) == 0))
						*p2 &= 0xFE;

					if (((n1 & 0x1C0) == 0) && ((n2 & 0x140) == 0))
						*(p2 + 1) &= 0x7F;

					p1++;
					p2++;
					p3++;
				}

				//corners
				if (IsPixelWhite(pSource, stride, 0, 1) && IsPixelWhite(pSource, stride, 1, 0) && IsPixelWhite(pSource, stride, 1, 1))
					MakePixelWhite(pSource, stride, 0, 0);
				if (IsPixelWhite(pSource, stride, 0, height - 2) && IsPixelWhite(pSource, stride, 1, height - 2) && IsPixelWhite(pSource, stride, 1, height - 1))
					MakePixelWhite(pSource, stride, 0, height - 1);
				if (IsPixelWhite(pSource, stride, width - 2, 0) && IsPixelWhite(pSource, stride, width - 2, 1) && IsPixelWhite(pSource, stride, width - 1, 1))
					MakePixelWhite(pSource, stride, width - 1, 0);
				if (IsPixelWhite(pSource, stride, width - 2, height - 2) && IsPixelWhite(pSource, stride, width - 1, height - 2) && IsPixelWhite(pSource, stride, width - 2, height - 1))
					MakePixelWhite(pSource, stride, width - 1, height - 1);
			}
		}
		#endregion()
	
		#region Despeckle1x1()
		/*private static void Despeckle1x1(BitmapData bmpData)
		{
			int stride = bmpData.Stride;
			int width = bmpData.Width;
			int height = bmpData.Height;
			int n1, n2, n3;
			int x, y;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				byte* p1, p2, p3;

				for (y = -1; y < height - 1; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = (y < height - 2) ? p2 + stride : p2;

					for (x = 0; x < stride - 1; x++)
					{
						n1 = (y == -1) ? 0 : (int)(((*p1) * 256) + (*(p1 + 1)));
						n2 = (int)(((*p2) * 256) + (*(p2 + 1)));
						n3 = (y < height - 2) ? (int)(((*p3) * 256) + (*(p3 + 1))) : 0 ;					

						if (((n1 & 0xE000) == 0) && ((n2 & 0xA000) == 0) && ((n3 & 0xE000) == 0))
							*p2 &= 0xBF;

						if (((n1 & 0x7000) == 0) && ((n2 & 0x5000) == 0) && ((n3 & 0x7000) == 0))
							*p2 &= 0xDF;

						if (((n1 & 0x3800) == 0) && ((n2 & 0x2800) == 0) && ((n3 & 0x3800) == 0))
							*p2 &= 0xEF;

						if (((n1 & 0x1C00) == 0) && ((n2 & 0x1400) == 0) && ((n3 & 0x1C00) == 0))
							*p2 &= 0xF7;

						if (((n1 & 0xE00) == 0) && ((n2 & 0xA00) == 0) && ((n3 & 0xE00) == 0))
							*p2 &= 0xFB;

						if (((n1 & 0x700) == 0) && ((n2 & 0x500) == 0) && ((n3 & 0x700) == 0))
							*p2 &= 0xFD;

						if (((n1 & 0x380) == 0) && ((n2 & 0x280) == 0) && ((n3 & 0x380) == 0))
							*p2 &= 0xFE;

						if (((n1 & 0x1C0) == 0) && ((n2 & 0x140) == 0) && ((n3 & 0x1C0) == 0))
							*(p2 + 1) &= 0x7F;

						p1++;
						p2++;
						p3++;
					}
				}

				//first column
				for (y = 1; y < height - 1; y++)
					if (IsPixelWhite(pSource, stride, 0, y - 1) && IsPixelWhite(pSource, stride, 1, y - 1) && IsPixelWhite(pSource, stride, 1, y) && IsPixelWhite(pSource, stride, 0, y + 1) && IsPixelWhite(pSource, stride, 1, y + 1))
						MakePixelWhite(pSource, stride, 0, y);

				//last column
				x = width - 1;
				for (y = 1; y < height - 1; y++)
					if (IsPixelWhite(pSource, stride, x - 1, y - 1) && IsPixelWhite(pSource, stride, x, y - 1) && IsPixelWhite(pSource, stride, x - 1, y) && IsPixelWhite(pSource, stride, x - 1, y + 1) && IsPixelWhite(pSource, stride, x, y + 1))
						MakePixelWhite(pSource, stride, x, y);

				//corners
				if (IsPixelWhite(pSource, stride, 0, 1) && IsPixelWhite(pSource, stride, 1, 0) && IsPixelWhite(pSource, stride, 1, 1))
					MakePixelWhite(pSource, stride, 0, 0);
				if (IsPixelWhite(pSource, stride, 0, height - 2) && IsPixelWhite(pSource, stride, 1, height - 2) && IsPixelWhite(pSource, stride, 1, height - 1))
					MakePixelWhite(pSource, stride, 0, height - 1);
				if (IsPixelWhite(pSource, stride, width - 2, 0) && IsPixelWhite(pSource, stride, width - 2, 1) && IsPixelWhite(pSource, stride, width - 1, 1))
					MakePixelWhite(pSource, stride, width - 1, 0);
				if (IsPixelWhite(pSource, stride, width - 2, height - 2) && IsPixelWhite(pSource, stride, width - 1, height - 2) && IsPixelWhite(pSource, stride, width - 2, height - 1))
					MakePixelWhite(pSource, stride, width - 1, height - 1);
			}
		}*/
		#endregion

		#region Despeckle1x1Black()
		/*private static void Despeckle1x1Black(BitmapData bmpData)
		{
			int stride = bmpData.Stride;
			int n1, n2, n3;
			int x, y;
			int width = bmpData.Width;
			int height = bmpData.Height;

			unsafe
			{
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				byte* p1, p2, p3;

				for (y = -1; y < height - 1; y++)
				{
					p1 = (y == -1) ? pSource : pSource + (y * stride);
					p2 = (y == -1) ? pSource : p1 + stride;
					p3 = (y < height - 2) ? p2 + stride : p2;

					for (x = 0; x < stride - 1; x++)
					{
						n1 = (y == -1) ? 0xFFFF : (int)(((*p1) * 256) + (*(p1 + 1)));
						n2 = (int)(((*p2) * 256) + (*(p2 + 1)));
						n3 = (y < height - 2) ? (int)(((*p3) * 256) + (*(p3 + 1))) : 0xFFFF;

						if (((n1 & 0xE000) == 0xE000) && ((n2 & 0xA000) == 0xA000) && ((n3 & 0xE000) == 0xE000))
							*p2 |= 0x40;

						if (((n1 & 0x7000) == 0x7000) && ((n2 & 0x5000) == 0x5000) && ((n3 & 0x7000) == 0x7000))
							*p2 |= 0x20;

						if (((n1 & 0x3800) == 0x3800) && ((n2 & 0x2800) == 0x2800) && ((n3 & 0x3800) == 0x3800))
							*p2 |= 0x10;

						if (((n1 & 0x1C00) == 0x1C00) && ((n2 & 0x1400) == 0x1400) && ((n3 & 0x1C00) == 0x1C00))
							*p2 |= 0x8;

						if (((n1 & 0xE00) == 0xE00) && ((n2 & 0xA00) == 0xA00) && ((n3 & 0xE00) == 0xE00))
							*p2 |= 0x4;

						if (((n1 & 0x700) == 0x700) && ((n2 & 0x500) == 0x500) && ((n3 & 0x700) == 0x700))
							*p2 |= 0x2;

						if (((n1 & 0x380) == 0x380) && ((n2 & 0x280) == 0x280) && ((n3 & 0x380) == 0x380))
							*p2 |= 0x1;

						if (((n1 & 0x1C0) == 0x1C0) && ((n2 & 0x140) == 0x140) && ((n3 & 0x1C0) == 0x1C0))
							*(p2 + 1) |= 0x80;

						p1++;
						p2++;
						p3++;
					}
				}

				//first column
				for (y = 1; y <= height - 1; y++)
					if (((pSource[(y - 1) * stride] & 0xC0) == 0xC0) && ((pSource[y * stride] & 0x40) == 0x40) && ((pSource[(y + 1) * stride] & 0xC0) == 0xC0))
						pSource[y * stride] |= 0x80;

				//last column
				x = width - 1;
				for (y = 1; y <= height - 1; y++)
					if (IsPixelWhite(pSource, stride, x, y - 1) && IsPixelWhite(pSource, stride, x - 1, y - 1) && IsPixelWhite(pSource, stride, x - 1, y) && IsPixelWhite(pSource, stride, x, y + 1) && IsPixelWhite(pSource, stride, x - 1, y + 1))
						MakePixelWhite(pSource, stride, x, y);

				//corners
				if (IsPixelWhite(pSource, stride, 1, 0) && IsPixelWhite(pSource, stride, 0, 1) && IsPixelWhite(pSource, stride, 1,1))
					MakePixelWhite(pSource, stride, 0,0);
				if (IsPixelWhite(pSource, stride, width - 2, 0) && IsPixelWhite(pSource, stride, width - 2, 1) && IsPixelWhite(pSource, stride, width - 1, 1))
					MakePixelWhite(pSource, stride, width - 1, 0);
				if (IsPixelWhite(pSource, stride, 0, height - 2) && IsPixelWhite(pSource, stride, 1, height - 2) && IsPixelWhite(pSource, stride, 1, height - 1))
					MakePixelWhite(pSource, stride, 0, height - 1);
				if (IsPixelWhite(pSource, stride, width - 2, height - 2) && IsPixelWhite(pSource, stride, width - 2, height - 1) && IsPixelWhite(pSource, stride, width - 1, height - 2))
					MakePixelWhite(pSource, stride, width - 1, height - 1);
			}
		}*/
		#endregion

		#region FireProgressEvent()
		private void FireProgressEvent(double progress)
		{
			if(ProgressChanged != null)
				ProgressChanged(this.progressChangedSeed + (float)progress * this.progressChangedRatio);
		}
		#endregion

		#region IsPixelWhite()
		private static unsafe bool IsPixelWhite(byte* pSource, int stride, int x, int y)
		{
			return ((pSource[y * stride + x / 8] & (0x80 >> (x & 0x7)) ) > 0);
		}

		private static unsafe bool IsPixelWhite(byte* pSource, int stride, int x, int y, int width, int height)
		{
			if (x < 0 || x >= width)
				return true;
			if (y < 0 || y >= height)
				return true;

			return ((pSource[y * stride + x / 8] & (0x80 >> (x & 0x7))) > 0);
		}
		#endregion

		#region MakePixelWhite()
		private static unsafe void MakePixelWhite(byte* pSource, int stride, int x, int y)
		{
			pSource[y * stride + x / 8] |= (byte)(0x80 >> (x & 0x7));
		}

		private static unsafe void MakePixelWhite(byte* pSource, int stride, int x, int y, int width, int height)
		{
			if(x >= 0 && y >= 0 && x < width && y < height)
				pSource[y * stride + x / 8] |= (byte)(0x80 >> (x & 0x7));
		}
		#endregion

		#endregion

	}
}
