using ImageProcessing.PageObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


namespace ImageProcessing.ObjectsRecognition
{
	public class ObjectLocator
	{
		//CLASSES		
		#region classes

		#region class ParagraphGap
		public class ParagraphGap : IComparable
		{
			public int Distance;
			public ImageProcessing.PageObjects.Paragraph Paragraph1;
			public ImageProcessing.PageObjects.Paragraph Paragraph2;

			public ParagraphGap(ImageProcessing.PageObjects.Paragraph paragraph1, ImageProcessing.PageObjects.Paragraph paragraph2, int distance)
			{
				this.Paragraph1 = paragraph1;
				this.Paragraph2 = paragraph2;
				this.Distance = distance;
			}

			public int CompareTo(object paragraph)
			{
				if (this.Distance < ((ObjectLocator.ParagraphGap)paragraph).Distance)
					return 1;

				if (this.Distance == ((ObjectLocator.ParagraphGap)paragraph).Distance)
					return 0;

				return -1;
			}
		}
		#endregion

		#region class ParagraphGaps
		public class ParagraphGaps : List<ParagraphGap>
		{
		}
		#endregion

		#region struct ParagraphStruct
		[StructLayout(LayoutKind.Sequential)]
		public struct ParagraphStruct
		{
			public readonly int WordsCount;
			public readonly int X;
			public readonly int Y;
			public readonly int Right;
			public readonly int Bottom;
			public ParagraphStruct(int wordsCount, int x, int y, int right, int bottom)
			{
				this.WordsCount = wordsCount;
				this.X = x;
				this.Y = y;
				this.Right = right;
				this.Bottom = bottom;
			}
		}
		#endregion

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region FindObjects()
		/// <summary>
		/// includes objects recognition
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="clip"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static ImageProcessing.PageObjects.Symbols FindObjects(Bitmap bitmap, Rectangle clip, int flags)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

			try
			{
				if (bitmap.PixelFormat != PixelFormat.Format1bppIndexed)
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);

				ImageProcessing.PageObjects.Symbols objects = FindObjects1bpp(bitmap, clip, true, true);
#if SAVE_RESULTS
				objects.DrawToFile(Debug.SaveToDir + "Symbols.png", bitmap.Size);
#endif

#if DEBUG
				Console.WriteLine(string.Format("PageObjects.ObjectLocator, FindObjects(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), objects.Count));
#endif

				RecognizePageObjects(objects, bitmap);

#if DEBUG
				Console.WriteLine(string.Format("PageObjects.ObjectLocator, FindObjects(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), objects.Count));
#endif

#if SAVE_RESULTS
				objects.DrawToFile(Debug.SaveToDir + "Symbols.png", bitmap.Size);
#endif

				return objects;
			}
			catch (Exception ex)
			{
				throw new Exception("PageObjects.ObjectLocator, FindObjects(): " + ex.Message);
			}
		}
		#endregion

		#region FindObjects1bpp()
		public static unsafe ImageProcessing.PageObjects.Symbols FindObjects1bpp(Bitmap bitmapOrig, Rectangle clip, bool computeObjectMaps, bool despeckle)
		{
			Bitmap bitmap = ImageCopier.Copy(bitmapOrig);
			
			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

			BitmapData bmpData = null;
			ImageProcessing.PageObjects.Symbols symbols = new ImageProcessing.PageObjects.Symbols()
			{
				new ImageProcessing.PageObjects.Symbol(0, 0)
			};

			int x, y;

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			try
			{
				bmpData = bitmap.LockBits(clip, ImageLockMode.ReadOnly, bitmap.PixelFormat);
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
							ImageProcessing.PageObjects.Symbol currentObject;
							ImageProcessing.PageObjects.Symbol prevObject;
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
								currentObject = new ImageProcessing.PageObjects.Symbol(x, y);
								symbols.Add(currentObject);
								groupId[x] = symbols.Count - 1;
							}
						}
					}
					upGroupId = groupId;
				}

#if DEBUG
				Console.WriteLine("FindObjects1bpp(), Seeking Objects: {0}", DateTime.Now.Subtract(start).ToString());
#endif

				symbols.RemoveAt(0);

				if (despeckle)
				{
					//despeckle - remove symbols smaller than 
					int minW = Convert.ToInt32(bitmap.HorizontalResolution / 100.0);
					int minBitFrequencyL = Convert.ToInt32(bitmap.HorizontalResolution / 15.0);
					ImageProcessing.PageObjects.Symbols symbolsNew = new ImageProcessing.PageObjects.Symbols();

					foreach (ImageProcessing.PageObjects.Symbol s in symbols)
						if ((s.Width >= minW) && (s.Pixels >= minBitFrequencyL))
							symbolsNew.Add(s);

					symbols = symbolsNew;
				}

				//shift symbols
				if (clip.Location != Point.Empty)
					symbols.Shift(clip.X, clip.Y);


				if (computeObjectMaps)
				{
#if DEBUG
					start = DateTime.Now;
#endif
					foreach (ImageProcessing.PageObjects.Symbol symbol in symbols)
						symbol.ComputeObjectMap(bmpData);
#if DEBUG
					Console.WriteLine("FindObjects1bpp(), Computing object shapes: {0}", DateTime.Now.Subtract(start).ToString());
#endif
				}


#if SAVE_RESULTS
				symbols.DrawToFile(Debug.SaveToDir + "Symbols.png", bitmapOrig.Size);
#endif
			}
			finally
			{
				if ((bitmap != null) && (bmpData != null))
					bitmap.UnlockBits(bmpData);
			}

			if (bitmap != null)
				bitmap.Dispose();

			return symbols;
		}
		#endregion

		#region FindObjectsInBitArray()
		/// <summary>
		/// doesn't despeckle
		/// </summary>
		/// <param name="bitArrayOrig"></param>
		/// <param name="width"></param>
		/// <param name="computeObjectMaps"></param>
		/// <returns></returns>
		public static unsafe ImageProcessing.PageObjects.Symbols FindObjectsInBitArray(byte[,] bitArrayOrig, int width, bool computeObjectMaps)
		{
			byte[,] bitArray = (byte[,])bitArrayOrig.Clone();
			
			int height = bitArray.GetLength(0);

			ImageProcessing.PageObjects.Symbols symbols = new ImageProcessing.PageObjects.Symbols()
			{
				new ImageProcessing.PageObjects.Symbol(0, 0)
			};

			int x, y;

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			int[] upGroupId = new int[width];
			int[] groupId = new int[width];
			bool[] line = new bool[width];

			for (y = 0; y < height; y++)
			{
				groupId = new int[width];

				for (x = 0; x < ((width / 8) * 8); x = x + 8)
				{
					line[x] = (bitArray[y, x / 8] & 0x80) > 0;
					line[x + 1] = (bitArray[y, x / 8] & 0x40) > 0;
					line[x + 2] = (bitArray[y, x / 8] & 0x20) > 0;
					line[x + 3] = (bitArray[y, x / 8] & 0x10) > 0;
					line[x + 4] = (bitArray[y, x / 8] & 8) > 0;
					line[x + 5] = (bitArray[y, x / 8] & 4) > 0;
					line[x + 6] = (bitArray[y, x / 8] & 2) > 0;
					line[x + 7] = (bitArray[y, x / 8] & 1) > 0;
				}

				for (x = (width / 8) * 8; x < width; x++)
					line[x] = (bitArray[y, x / 8] & (((int)0x80) >> (x % 8))) > 0;

				for (x = 0; x < width; x++)
				{
					if (line[x])
					{
						ImageProcessing.PageObjects.Symbol currentObject;
						ImageProcessing.PageObjects.Symbol prevObject;
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
							currentObject = new ImageProcessing.PageObjects.Symbol(x, y);
							symbols.Add(currentObject);
							groupId[x] = symbols.Count - 1;
						}
					}
				}
				upGroupId = groupId;
			}

#if DEBUG
			Console.WriteLine("FindObjects1bpp(), Seeking Objects: {0}", DateTime.Now.Subtract(start).ToString());
#endif

			symbols.RemoveAt(0);


			if (computeObjectMaps)
			{
#if DEBUG
				start = DateTime.Now;
#endif
				foreach (ImageProcessing.PageObjects.Symbol symbol in symbols)
				{
					symbol.ComputeObjectMap(bitArray, width);
				}
#if DEBUG
				Console.WriteLine("FindObjects1bpp(), Computing object shapes: {0}", DateTime.Now.Subtract(start).ToString());
#endif
			}


#if SAVE_RESULTS
				symbols.DrawToFile(Debug.SaveToDir + "Symbols.png", new Size(width, height));
#endif

			return symbols;
		}
		#endregion

		#region FindPage()
		public static ImageProcessing.PageObjects.Page FindPage(ImageProcessing.PageObjects.Symbols loneSymbols, ImageProcessing.PageObjects.Paragraphs paragraphs, ImageProcessing.PageObjects.Pictures pictures, Delimiters delimiters, Rectangle clip, ref float confidence)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			ImageProcessing.PageObjects.Pages pages = new ImageProcessing.PageObjects.Pages();
			confidence = 1f;

			foreach (ImageProcessing.PageObjects.Paragraph paragraph in paragraphs)
				pages.Add(new ImageProcessing.PageObjects.Page(paragraph));
			foreach (ImageProcessing.PageObjects.Delimiter delimiter in delimiters)
				if (delimiter.IsVirtual == false)
				{
					Crop crop = delimiter.ObjectShape.GetCrop();
					
					pages.Add(new ImageProcessing.PageObjects.Page(crop.TangentialRectangle));
				}
			foreach (ImageProcessing.PageObjects.Picture picture in pictures)
				pages.Add(new ImageProcessing.PageObjects.Page(picture));
			foreach (ImageProcessing.PageObjects.Symbol lonelySymbol in loneSymbols)
				pages.Add(new ImageProcessing.PageObjects.Page(lonelySymbol.Rectangle));

			pages.MergeNestedPages();
			pages.MergeVertAdjacentPages();

			int oneThirdOfPage = clip.X + (clip.Width / 5);
			int twoThirdsOfPage = clip.X + ((clip.Width * 4) / 5);

			for (int i = pages.Count - 1; i >= 0; i--)
				if ((pages[i].Right < oneThirdOfPage) || (pages[i].X > twoThirdsOfPage))
					pages.RemoveAt(i);

#if SAVE_RESULTS
			pages.DrawToFile(Debug.SaveToDir + "24 Pages.png", new Size(clip.Right, clip.Bottom));
#endif

			for (int i = pages.Count - 1; i >= 0; i--)
				if ((pages[i].Width < (clip.Width * 0.15)) && (pages[i].Height < (clip.Height * 0.15)))
					pages.RemoveAt(i);

			Rectangle pageClip = pages.GetClip();

#if DEBUG
			if (pages.Count == 0)
				Console.WriteLine(string.Format("PageObjects.ObjectLocator, FindPage(): {0}, Page not found", DateTime.Now.Subtract(start).ToString()));
			else
				Console.WriteLine(string.Format("PageObjects.ObjectLocator, FindPage(): {0}, Rectangle: {1}", DateTime.Now.Subtract(start).ToString(), pageClip));
#endif
			
			if (pageClip != Rectangle.Empty)
				return new ImageProcessing.PageObjects.Page(pageClip);
			else
				return new ImageProcessing.PageObjects.Page(clip);
		}
		#endregion

		#region FindPages()
		public static ImageProcessing.PageObjects.Pages FindPages(ImageProcessing.PageObjects.Symbols loneSymbols, Words words, ImageProcessing.PageObjects.Pictures pictures, Delimiters delimiters, ImageProcessing.PageObjects.Paragraphs paragraphs, Size imageSize, ref float confidence)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			ImageProcessing.PageObjects.Pages pages = new ImageProcessing.PageObjects.Pages();
			confidence = 1f;

			foreach (ImageProcessing.PageObjects.Paragraph paragraph in paragraphs)
				pages.Add(new ImageProcessing.PageObjects.Page(paragraph));
			foreach (ImageProcessing.PageObjects.Delimiter delimiter in delimiters)
				if(delimiter.IsVirtual == false)
				pages.Add(new ImageProcessing.PageObjects.Page(delimiter.ObjectShape.GetCrop().TangentialRectangle));
			foreach (ImageProcessing.PageObjects.Picture picture in pictures)
				pages.Add(new ImageProcessing.PageObjects.Page(picture));
			foreach (ImageProcessing.PageObjects.Symbol loneSymbol in loneSymbols)
				pages.Add(new ImageProcessing.PageObjects.Page(loneSymbol.Rectangle));


			pages.MergeNestedPages();			
			pages.MergeVertAdjacentPages();

			int oneThirdOfPage = (imageSize.Width / 5);
			int twoThirdsOfPage = ((imageSize.Width * 4) / 5);
			int lettersHeight = words.FontSize;

			for (int i = pages.Count - 1; i >= 0; i--)
				if ((pages[i].Right < oneThirdOfPage) || (pages[i].X > twoThirdsOfPage) || (pages[i].Width < lettersHeight * 5))
					pages.RemoveAt(i);

			pages.Compact(imageSize);

#if SAVE_RESULTS
			pages.DrawToFile(Debug.SaveToDir + "Pages result.png", imageSize);
#endif

#if DEBUG
			Console.WriteLine(string.Format("PageObjects.ObjectLocator, FindPages(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), pages.Count));
#endif
			
			return pages;
		}
		#endregion

		#region GetAverageLineSpacing()
		public static int GetAverageLineSpacing(Words words)
		{
			int i;
			int[] spacingArray = new int[100];
			
			for (i = 0; i < words.Count; i++)
			{
				int spacingT = GetTopAdjacentSpacing(words, words[i], i - 1);
				int spacingB = GetBottomAdjacentSpacing(words, words[i], i + 1);
				
				if ((spacingT > 10) && (spacingT < 100))
					spacingArray[spacingT]++;
				
				if ((spacingB > 10) && (spacingB < 100))
					spacingArray[spacingB]++;
			}
			
			int maxIndex = 0;
			
			for (i = 1; i < 100; i++)
				if (spacingArray[maxIndex] < spacingArray[i])
					maxIndex = i;

			return maxIndex;
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region GetBottomAdjacentSpacing()
		private static int GetBottomAdjacentSpacing(Words words, Word currentWord, int limit)
		{
			for (int i = limit; i < words.Count; i++)
			{
				Word testedWord = words[i];
				if ((((testedWord.X <= currentWord.X) && (testedWord.Right >= currentWord.X)) || ((testedWord.X >= currentWord.X) && (testedWord.X <= currentWord.Right))) && (testedWord.Shoulder > currentWord.Seat))
				{
					return (testedWord.Seat - currentWord.Seat);
				}
			}
			return 0;
		}
		#endregion

		#region GetRightParagraphAdjacent()
		private static ImageProcessing.PageObjects.Paragraph GetRightParagraphAdjacent(ImageProcessing.PageObjects.Paragraphs paragraphs, ImageProcessing.PageObjects.Paragraph p, out int distance)
		{
			ImageProcessing.PageObjects.Paragraph adjacent = null;
			distance = 0x7fffffff;
			for (int i = 0; i < paragraphs.Count; i++)
			{
				ImageProcessing.PageObjects.Paragraph testedP = paragraphs[i];
				if ((testedP != p) && (p.X <= testedP.X))
				{
					int distanceTmp = HorizontalDistanceBetweenParagraphs(p, testedP);
					if (distance > distanceTmp)
					{
						distance = distanceTmp;
						adjacent = testedP;
					}
				}
			}
			return adjacent;
		}
		#endregion

		#region GetTopAdjacentSpacing()
		private static int GetTopAdjacentSpacing(Words words, Word currentWord, int limit)
		{
			for (int i = (limit < (words.Count - 1)) ? limit : (words.Count - 1); i >= 0; i--)
			{
				Word testedWord = words[i];
				if ((((testedWord.X <= currentWord.X) && (testedWord.Right >= currentWord.X)) || ((testedWord.X >= currentWord.X) && (testedWord.X <= currentWord.Right))) && (testedWord.Seat < currentWord.Shoulder))
				{
					return (currentWord.Seat - testedWord.Seat);
				}
			}
			return 0;
		}
		#endregion

		#region GetWordAdjacents()
		private static Words GetWordAdjacents(Words words, Word currentWord, double spacingT, double spacingB, int index)
		{
			int i;
			Word testedWord;
			Words adjacents = new Words();
			for (i = ((index - 1) < (words.Count - 1)) ? (index - 1) : (words.Count - 1); i >= 0; i--)
			{
				testedWord = words[i];
				if (currentWord.Zone == testedWord.Zone)
				{
					if ((currentWord.Seat - testedWord.Seat) > spacingT)
					{
						break;
					}
					if (((testedWord.X <= currentWord.X) && (testedWord.Right >= currentWord.X)) || ((testedWord.X >= currentWord.X) && (testedWord.X <= currentWord.Right)))
					{
						adjacents.Add(testedWord);
					}
				}
			}
			for (i = index + 1; i < words.Count; i++)
			{
				testedWord = words[i];
				if (currentWord.Zone == testedWord.Zone)
				{
					if ((testedWord.Seat - currentWord.Seat) > spacingB)
					{
						return adjacents;
					}
					if (((testedWord.X <= currentWord.X) && (testedWord.Right >= currentWord.X)) || ((testedWord.X >= currentWord.X) && (testedWord.X <= currentWord.Right)))
					{
						adjacents.Add(testedWord);
					}
				}
			}
			return adjacents;
		}
		#endregion

		#region HorizontalDistanceBetweenParagraphs()
		private static int HorizontalDistanceBetweenParagraphs(ImageProcessing.PageObjects.Paragraph p1, ImageProcessing.PageObjects.Paragraph p2)
		{
			if (p1.X < p2.X)
			{
				return (((p2.X - p1.Right) > 0) ? (p2.X - p1.Right) : 0);
			}
			return (((p1.X - p2.Right) > 0) ? (p1.X - p2.Right) : 0);
		}
		#endregion

		#region MergeParagraphsByGaps()
		private static bool MergeParagraphsByGaps(ImageProcessing.PageObjects.Paragraphs paragraphs, ParagraphGaps gaps)
		{
			bool change = false;
			for (int i = 1; i < gaps.Count; i++)
			{
				ImageProcessing.PageObjects.Paragraph paragraph1 = gaps[i].Paragraph1;
				ImageProcessing.PageObjects.Paragraph paragraph2 = gaps[i].Paragraph2;
				paragraph1.Merge(paragraph2);
				foreach (ParagraphGap gap in gaps)
				{
					if (gap != gaps[i])
					{
						if (gap.Paragraph1 == paragraph2)
						{
							gap.Paragraph1 = paragraph1;
						}
						if (gap.Paragraph2 == paragraph2)
						{
							gap.Paragraph2 = paragraph1;
						}
					}
				}
				paragraphs.Remove(paragraph2);
				change = true;
			}
			return change;
		}
		#endregion

		#region RecognizePageObjects()
		private static void RecognizePageObjects(ImageProcessing.PageObjects.Symbols symbols, Bitmap raster)
		{
#if DEBUG
			DateTime startTotal = DateTime.Now;
#endif
			
			int resolution = Convert.ToInt32(raster.HorizontalResolution);
			int minH;
			int minW = Convert.ToInt32(resolution / 100.0);
			int maxW = Convert.ToInt32(resolution / 4.0);
			int maxH = Convert.ToInt32(resolution / 4.0);
			int minBitFrequencyL = Convert.ToInt32(resolution / 15.0);
			int minBitFrequencyP = Convert.ToInt32(resolution / 20.0);
			int ratio = 4;
			int maxLetterW = Convert.ToInt32(resolution / 2.0);
			int maxLetterH = Convert.ToInt32(resolution / 2.0);
			int maxLineWidthOrHeight = Convert.ToInt32(resolution / 20.0);

			if (symbols.Count > 500)
				minH = Convert.ToInt32(symbols.GetTextHeight() * 4.0 / 5.0);
			else
				minH = Convert.ToInt32(resolution / 25.0);

			for (int i = symbols.Count - 1; i >= 0; i--)
			{
				ImageProcessing.PageObjects.Symbol s = symbols[i];
	
				//punctuation
				if ((s.Width > minW) && (s.Height < minH) && ((s.Height / s.Width) < ratio) && ((s.Width / s.Height) < ratio) && (s.Pixels > minBitFrequencyP))
					symbols[i].ObjectType = ImageProcessing.PageObjects.Symbol.Type.Punctuation;
				//lines
				else if ((s.Width > maxW) || (s.Height > maxH))
				{
					//horizontal line
					if ((s.Height / s.Width > ratio) && s.ObjectShape.MaxPixelWidth < maxLineWidthOrHeight)
						symbols[i].ObjectType = ImageProcessing.PageObjects.Symbol.Type.Line;
					else if (((s.Width / s.Height) > ratio) && s.ObjectShape.MaxPixelHeight < maxLineWidthOrHeight)
						symbols[i].ObjectType = ImageProcessing.PageObjects.Symbol.Type.Line;
					else
						symbols[i].ObjectType = ImageProcessing.PageObjects.Symbol.Type.NotSure;
				}
				else
					symbols[i].ObjectType = ImageProcessing.PageObjects.Symbol.Type.Letter;
			}

			symbols.SortHorizontally();

			for (int i = symbols.Count - 1; i >= 0; i--)
			{
				if (symbols[i].ObjectType == ImageProcessing.PageObjects.Symbol.Type.NotSure)
				{
					ImageProcessing.PageObjects.Symbol s = symbols[i];
					if ((s.Width < maxLetterW) && (s.Height < maxLetterH))
					{
						int weight;
						ImageProcessing.PageObjects.Symbol sL = symbols.GetSymbolToTheLeft(i, out weight);
						ImageProcessing.PageObjects.Symbol sR = symbols.GetSymbolToTheRight(i, out weight);
						
						if ((((sL != null) && (sL.Width < maxLetterW) && ((sL.Height < maxLetterH) && (sL.Width < (s.Width * 3)))) && (((sL.Width > (s.Width / 3)) && (sL.Height < (s.Height * 2))) && ((sL.Height > (s.Height / 2)) && (sL.Pixels < (s.Pixels * 4))))) && (sL.Pixels > (s.Pixels / 4)))
							s.ObjectType = ImageProcessing.PageObjects.Symbol.Type.Letter;
						else if (((((sR != null) && (sR.Width < maxLetterW)) && ((sR.Height < maxLetterH) && (sR.Width < (s.Width * 3)))) && (((sR.Width > (s.Width / 3)) && (sR.Height < (s.Height * 2))) && ((sR.Height > (s.Height / 2)) && (sR.Pixels < (s.Pixels * 4))))) && (sR.Pixels > (s.Pixels / 4)))
							s.ObjectType = ImageProcessing.PageObjects.Symbol.Type.Letter;
						else
							s.ObjectType = ImageProcessing.PageObjects.Symbol.Type.NotSure;
					}
					else
					{
						s.ObjectType = ImageProcessing.PageObjects.Symbol.Type.Picture;
					}
				}
			}

			symbols.SortHorizontally();

			for (int i = symbols.Count - 1; i >= 0; i--)
			{
				if (symbols[i].IsLetter || symbols[i].IsPunctuation)
				{
					ImageProcessing.PageObjects.Symbol s = symbols[i];

					for (int j = i + 1; j < symbols.Count; j++)
					{
						if (s.Right < symbols[j].X)
							break;

						if (((symbols[j].Bottom > s.Y) && (symbols[j].Y < s.Bottom)) && (symbols[j].IsLetter || symbols[j].IsPunctuation))
						{
							Rectangle rect = Rectangle.Intersect(s.Rectangle, symbols[j].Rectangle);
							
							if (((rect.Width * rect.Height) > ((s.Width * s.Height) / 2)) || ((rect.Width * rect.Height) > ((symbols[j].Width * symbols[j].Height) / 2)))
							{
								s.Merge(symbols[j]);
								symbols.RemoveAt(j);
								j = j - 1;
							}
						}
					}
				}
			}

			ValidatePicturesAndTitles(symbols, raster);

#if DEBUG
			System.Reflection.MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
			Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "(): " + DateTime.Now.Subtract(startTotal).ToString());
#endif
		}
		#endregion

		#region RemoveBookfoldParagraph()
		private static void RemoveBookfoldParagraph(ImageProcessing.PageObjects.Paragraphs paragraphs, ref ParagraphGaps gaps)
		{
			ParagraphGap gap0 = gaps[0];
			ParagraphGap gap1 = gaps[1];
			if (gap0.Distance < (gap1.Distance * 2))
			{
				ImageProcessing.PageObjects.Paragraph pToRemove;
				if ((gap0.Paragraph1 == gap1.Paragraph1) && (gap0.Paragraph1.Width < 60))
				{
					pToRemove = gap0.Paragraph1;
					gap0.Paragraph1 = gap1.Paragraph2;
					gap0.Distance = HorizontalDistanceBetweenParagraphs(gap0.Paragraph1, gap0.Paragraph2);
					gaps.RemoveAt(1);
					paragraphs.Remove(pToRemove);
				}
				else if ((gap0.Paragraph1 == gap1.Paragraph2) && (gap0.Paragraph1.Width < 60))
				{
					pToRemove = gap0.Paragraph1;
					gap0.Paragraph1 = gap1.Paragraph1;
					gap0.Distance = HorizontalDistanceBetweenParagraphs(gap0.Paragraph1, gap0.Paragraph2);
					gaps.RemoveAt(1);
					paragraphs.Remove(pToRemove);
				}
				else if ((gap0.Paragraph2 == gap1.Paragraph1) && (gap0.Paragraph2.Width < 60))
				{
					pToRemove = gap0.Paragraph2;
					gap0.Paragraph2 = gap1.Paragraph2;
					gap0.Distance = HorizontalDistanceBetweenParagraphs(gap0.Paragraph1, gap0.Paragraph2);
					gaps.RemoveAt(1);
					paragraphs.Remove(pToRemove);
				}
				else if ((gap0.Paragraph2 == gap1.Paragraph2) && (gap0.Paragraph2.Width < 60))
				{
					pToRemove = gap0.Paragraph2;
					gap0.Paragraph2 = gap1.Paragraph1;
					gap0.Distance = HorizontalDistanceBetweenParagraphs(gap0.Paragraph1, gap0.Paragraph2);
					gaps.RemoveAt(1);
					paragraphs.Remove(pToRemove);
				}
			}
		}
		#endregion

		#region ValidatePicturesAndTitles()
		/// <summary>
		/// Checks if symbols are really pictures, not title letters
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="raster"></param>
		/// <returns></returns>
		private static void ValidatePicturesAndTitles(ImageProcessing.PageObjects.Symbols symbols, Bitmap raster)
		{
			List<ImageProcessing.PageObjects.Symbol> symbolsToLetter = new List<ImageProcessing.PageObjects.Symbol>();
			
			for (int i = 0; i < symbols.Count; i++)
			{
				if ((symbols[i].IsPicture || symbols[i].IsLine) && (symbols[i].Width * 2.0 < symbols[i].Height))
				{
					ImageProcessing.PageObjects.Symbol symbolL = symbols.GetSymbolToTheLeft(i, out int weightL);
					ImageProcessing.PageObjects.Symbol symbolR = symbols.GetSymbolToTheRight(i, out int weightR);

					if (symbolL != null && symbolR != null)
					{
						int shortest = Math.Min(symbolL.Height, Math.Min(symbolR.Height, symbols[i].Height));
						int highest = Math.Min(symbolL.Height, Math.Min(symbolR.Height, symbols[i].Height));

						if (highest < shortest * 1.2)
						{
							int widest = Math.Max(symbolL.Width, Math.Max(symbolR.Width, symbols[i].Width));
							//int narrowest = Math.Min(symbolL.Width, Math.Min(symbolR.Width, symbols[i].Width));
							int space1 = Math.Max(0, symbols[i].X - symbolL.Right);
							int space2 = Math.Max(0, symbolR.X - symbols[i].Right);
							int narrowerSpace = Math.Min(space1, space2);

							if ((space1 > space2 * 0.8) && (space1 < space2 * 1.2) && (narrowerSpace < shortest / 2) && (widest < raster.Width / 3))
							{
								//int topMost = Math.Min(symbolL.Bottom, Math.Min(symbols[i].Bottom, symbolR.Bottom));
								//int bottomMost = Math.Max(symbolL.Bottom, Math.Max(symbols[i].Bottom, symbolR.Bottom));

								Point pLBottom = new Point(symbolL.X + symbolL.Width / 2, symbolL.Bottom);
								Point pCBottom = new Point(symbols[i].X + symbols[i].Width / 2, symbols[i].Bottom);
								Point pRBottom = new Point(symbolR.X + symbolR.Width / 2, symbolR.Bottom);
								double centerYProjection = Arithmetic.GetY(pLBottom, pRBottom, pCBottom.X);

								if (centerYProjection > pCBottom.Y - 5 && centerYProjection < pCBottom.Y + 5)
								{
									symbolsToLetter.Add(symbols[i]);
									symbolsToLetter.Add(symbolL);
									symbolsToLetter.Add(symbolR);
								}
							}
						}
					}
					else if (symbolL != null && Word.AreWordCandidates(symbolL, symbols[i], raster.Size))
					{
						symbolsToLetter.Add(symbols[i]);
						symbolsToLetter.Add(symbolL);
					}
					else if (symbolR != null && Word.AreWordCandidates(symbols[i], symbolR, raster.Size))
					{
						symbolsToLetter.Add(symbols[i]);
						symbolsToLetter.Add(symbolR);
					}
				}
			}

			foreach(ImageProcessing.PageObjects.Symbol symbolToLetter in symbolsToLetter)
				symbolToLetter.ObjectType = ImageProcessing.PageObjects.Symbol.Type.Letter;
		}
		#endregion

		#endregion

	}

}
