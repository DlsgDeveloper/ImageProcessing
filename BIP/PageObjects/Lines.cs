using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace ImageProcessing.PageObjects
{
	public class Lines : List<Line>
	{
		SortType sortType = SortType.None;

		#region constructor()
		public Lines()
		{
		}

		public Lines(Words words, Symbols symbols)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			int fontSize = words.FontSize;

			foreach (Word w in words)
				if (w.Line != null)
					w.Line = null;

			symbols.SortHorizontally();
			words.Sort(new Word.HorizontalComparer());

			for (int i = 0; i < words.Count; i++)
			{
				Word word = words[i];
				Word rightAdjacent = words.GetRightWordAdjacent(word, i);

				if (rightAdjacent != null)
					this.AddWords(word, rightAdjacent);
			}

			for (int i = symbols.Count - 1; i >= 0; i--)
			{
				Symbol symbol = symbols[i];

				if (symbol.IsLetter && symbol.Word == null)// && symbol.Height > fontSize / 2 && symbol.Height < fontSize * 2)
				{
					if (AdjacentLineExists(symbol))
					{
						Word leftWord = words.GetWordToTheLeft(symbol);
						Word rightWord = words.GetWordToTheRight(symbol);

						if ((leftWord != null) || (rightWord != null))
						{
							symbols.Remove(symbol);

							if (leftWord != null)
							{
								this.AddWordAndSymbol(leftWord, symbol);
								symbol.Zone = leftWord.Zone;

								if (rightWord != null && rightWord.Line != null && rightWord.Line != leftWord.Line)
								{
									Line lineToRemove = rightWord.Line;
									leftWord.Line.Merge(rightWord.Line);
									this.Remove(lineToRemove);
								}
							}
							else if (rightWord != null)
							{
								this.AddWordAndSymbol(rightWord, symbol);
								symbol.Zone = rightWord.Zone;
							}
						}
					}
				}
			}

			Words notUsedWords = new Words();

			foreach (Word word in words)
				if (word.Line == null)
					notUsedWords.Add(word);

			//DrawToFile(Debug.SaveToDir + "Lines.png", clip.Size);
			this.MergeLines(notUsedWords, symbols);

			//DrawToFile(Debug.SaveToDir + "Lines.png", clip.Size);
			Lines titles = GetTitles(words, symbols);
			//DrawToFile(Debug.SaveToDir + "Lines.png", clip.Size);

			this.AddRange(titles);

			//DrawToFile(Debug.SaveToDir + "Lines.png", clip.Size);
			this.MergeLines(notUsedWords, symbols);
			//DrawToFile(Debug.SaveToDir + "Lines.png", clip.Size);

			this.Sort();

#if DEBUG
			Console.WriteLine(string.Format("Lines, constructor: {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), this.Count));
#endif
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Add()
		new public void Add(Line line)
		{
			base.Add(line);
			sortType = SortType.None;
		}
		#endregion

		#region AddWords()
		public void AddWords(Word word1, Word word2)
		{
			if ((word1.Line != null) && (word2.Line != null))
			{
				if (word1.Line != word2.Line)
				{
					Line lineToRemove = word2.Line;
					word1.Line.Merge(word2.Line);
					this.Remove(lineToRemove);
				}
			}
			else if (word1.Line != null)
				word1.Line.AddWord(word2);
			else if (word2.Line != null)
				word2.Line.AddWord(word1);
			else
				this.Add(new Line(word1, word2));

			sortType = SortType.None;
		}
		#endregion

		#region AddWordAndSymbol()
		public void AddWordAndSymbol(Word word, Symbol symbol)
		{
			if (word.Line != null)
				word.Line.AddSymbol(symbol);
			else
				this.Add(new Line(word, symbol));

			sortType = SortType.None;
		}
		#endregion

		#region GetClip()
		public Rectangle GetClip()
		{
			int x = int.MaxValue, y = int.MaxValue, r = int.MinValue, b = int.MinValue;

			foreach (Line line in this)
			{
				if (x > line.X)
					x = line.X;
				if (y > line.Y)
					y = line.Y;
				if (r < line.Right)
					r = line.Right;
				if (b < line.Bottom)
					b = line.Bottom;
			}

			if (x == int.MaxValue || y == int.MaxValue || r == int.MinValue || b == int.MinValue)
				return Rectangle.Empty;
			else
				return Rectangle.FromLTRB(x, y, r, b);
		}
		#endregion

		#region GetLinesInClip()
		public Lines GetLinesInClip(Rectangle clip)
		{
			Lines lines = new Lines();

			foreach (Line line in this)
			{
				Rectangle intersection = Rectangle.Intersect(line.Rectangle, clip);

				if ((intersection.Width * intersection.Height) > ((line.Width * line.Height) / 2))
					lines.Add(line);
			}

			return lines;
		}
		#endregion

		#region GetLinesVerticalIntersectionWidth()
		public static int GetLinesVerticalIntersectionWidth(Line line1, Line line2)
		{
			if (((line1.X <= line2.X) && (line1.Right >= line2.X)) || ((line1.X >= line2.X) && (line1.X <= line2.Right)))
			{
				if (line1.X >= line2.X)
					return line1.X;
				else
					return line2.X;
			}

			return -1;
		}
		#endregion

		#region MergeWithUnusedWords()
		/*public void MergeWithUnusedWords(Words words)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			int wordsCount;

			do
			{
				wordsCount = words.Count;

				for (int i = words.Count - 1; i >= 0; i--)
				{
					Word word = words[i];

					if (word.Line == null)
					{
						for (int j = this.Count - 1; j >= 0; j++)
						{
							Line line = this[j];

							if (AreWordAndLineInLine(line, word))
							{
								line.AddWord(word);
								words.Remove(word);
							}
						}
					}
				}
			} while (wordsCount != words.Count);

#if DEBUG
			System.Reflection.MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
			Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "(): " + DateTime.Now.Subtract(start).ToString());
#endif
		}*/
		#endregion

		#region Validate()
		/// <summary>
		/// Splits lines if there is a deviding delimiter.
		/// </summary>
		/// <param name="delimiters"></param>
		public void Validate(Delimiters delimiters)
		{
			for (int i = this.Count - 1; i >= 0; i--)
			{
				Line line = this[i];
				
				foreach (Delimiter delimiter in delimiters)
				{
					if (delimiter.IsVertical)
					{
						Point joint = GetJoint(line, delimiter);
						
						if (joint.X >= 0)
						{
							Words words = new Words();
							Symbols symbols = new Symbols();

							for (int j = line.Words.Count - 1; j >= 0; j--)
							{
								Word word = line.Words[j];

								if ((word.X <= joint.X) && (word.Right >= joint.X))
								{
									line.RemoveWord(word);
								}
								else if (word.Right > joint.X)
								{
									line.RemoveWord(word);
									words.Add(word);
								}
							}

							for (int j = line.Symbols.Count - 1; j >= 0; j--)
							{
								Symbol symbol = line.Symbols[j];

								if ((symbol.X <= joint.X) && (symbol.Right >= joint.X))
								{
									line.RemoveSymbol(symbol);
								}
								else if (symbol.Right > joint.X)
								{
									line.RemoveSymbol(symbol);
									symbols.Add(symbol);
								}
							}

							if (words.Count > 1)
							{
								Line newLine = new Line(words[0], words[1]);

								for (int j = 2; j < words.Count; j++)
									newLine.AddWord(words[j]);

								foreach (Symbol symbol in symbols)
									newLine.AddSymbol(symbol);

								this.Add(newLine);
							}
						}
					}
				}
				
				if (this[i].Words.Count == 0)
					this.RemoveAt(i);
			}

			sortType = SortType.None;
		}
		#endregion

		#region HorizontalDistance()
		public static int HorizontalDistance(Line line1, Line line2)
		{
			return Math.Max(0, Math.Max(line1.X, line2.X) - Math.Min(line1.Right, line2.Right));
		}
		#endregion

		#region GetLineVerticalAdjacents()
		public Lines GetLineVerticalAdjacents(Line theLine, float maxDistance)
		{
			Lines adjacentLines = new Lines();

			foreach (Line line in this)
			{
				if (line != theLine)
				{
					int verticalShare = GetLinesVerticalIntersectionWidth(line, theLine);

					if (verticalShare >= 0)
					{
						int distance = theLine.GetSeatAt(verticalShare) - line.GetSeatAt(verticalShare);
						
						if ((distance < maxDistance) && (distance > -maxDistance))
							adjacentLines.Add(line);
					}
				}
			}

			return adjacentLines;
		}
		#endregion

		#region GetVerticalAdjacents()
		public Lines GetVerticalAdjacents(Line line)
		{
			Lines lineAdjacents = new Lines();
			float smallestDistance = float.MaxValue;

			foreach (Line aLine in this)
			{
				if (line != aLine)
				{
					int distance;

					if (GetLineSpacing(line, aLine, out distance))
					{
						if (distance > 5 && smallestDistance > distance)
						{
							smallestDistance = distance;
							lineAdjacents.Add(aLine);
						}
					}
				}
			}

			for (int i = lineAdjacents.Count - 1; i >= 0; i--)
			{
				if (line.Words.Count > 0 && lineAdjacents[i].Words.Count > 0 && line.Words[0].Zone != lineAdjacents[i].Words[0].Zone)
					lineAdjacents.RemoveAt(i);
				else
				{
					int verticalShare = GetLinesVerticalIntersectionWidth(line, lineAdjacents[i]);
					int distance = Math.Abs(line.GetSeatAt(verticalShare) - lineAdjacents[i].GetSeatAt(verticalShare));

					if (distance > smallestDistance * 1.1)
						lineAdjacents.RemoveAt(i);
				}
			}

			return lineAdjacents;
		}
		#endregion

		#region GetLineSpacing()
		public static bool GetLineSpacing(Line line1, Line line2, out int distance)
		{
			int verticalShare = GetLinesVerticalIntersectionWidth(line1, line2);

			if (verticalShare >= 0)
			{
				distance = Math.Abs(line1.GetSeatAt(verticalShare) - line2.GetSeatAt(verticalShare));
				return true;
			}

			distance = 0;
			return false;
		}
		#endregion

		#region IsTopLine()
		/// <summary>
		/// Returns true if first line is abobe second line.
		/// </summary>
		/// <param name="line1"></param>
		/// <param name="line2"></param>
		/// <returns></returns>
		public static bool IsAbove(Line line1, Line line2)
		{
			int verticalShare = GetLinesVerticalIntersectionWidth(line1, line2);

			if (verticalShare >= 0)
				return (line1.GetSeatAt(verticalShare) < line2.GetSeatAt(verticalShare));

			return false;
		}
		#endregion

		#region GetSpacing()
		/// <summary>
		/// returns mean spacing
		/// </summary>
		/// <returns></returns>
		public int? GetSpacing(int dpi)
		{
			int distance;
			int[] distancesArray = new int[dpi];
			
			foreach (Line line in this)
			{
				Lines adjacents = GetVerticalAdjacents(line);

				foreach (Line adjacent in adjacents)
				{
					if (GetLineSpacing(line, adjacent, out distance))
						if (distance < dpi)
							distancesArray[distance]++;
				}
			}

			//get array sum
			int sum = 0;
			for (int i = 0; i < dpi; i++)
				sum += distancesArray[i];

			if (sum > 0)
			{
				int sumSoFar = 0;

				for (int i = 0; i < dpi; i++)
				{
					sumSoFar += distancesArray[i];

					if (sumSoFar > sum / 2)
						return i;
				}

				return null;
			}
			else
				return null;
		}
		#endregion

		#region SortHorizontally()
		public void SortHorizontally()
		{
			if (sortType != SortType.Horizontal)
			{
				this.Sort(new Line.HorizontalComparer());
				sortType = SortType.Horizontal;
			}
		}
		#endregion

		#region SortVertically()
		public void SortVertically()
		{
			if (sortType != SortType.Vertical)
			{
				this.Sort(new Line.VerticalComparer());
				sortType = SortType.Vertical;
			}
		}
		#endregion

		#region DrawToFile()
		public void DrawToFile(string filePath, Size imageSize)
		{
#if SAVE_RESULTS
			Bitmap result = null;
			BitmapData bmpData = null;

			this.Sort(new Line.VerticalComparer());

			try
			{
				result = Debug.GetBitmap(imageSize);
				int counter = 0;
				Graphics g = Graphics.FromImage(result);
				Pen linePen = new Pen(Color.White, 3f);

				foreach (Line line in this)
				{
					g.FillRectangle(new SolidBrush(Color.FromArgb(100, Debug.GetColor(counter++))), line.Rectangle);

					List<Rectangle> rects = RectanglesToDraw(line);

					for (int i = 0; i < rects.Count; i++)
					{
						g.DrawLine(linePen, rects[i].X, rects[i].Y, rects[i].Right, rects[i].Y);

						if (i < (rects.Count - 1))
							g.DrawLine(linePen, rects[i].Right, rects[i].Y, rects[i + 1].X, rects[i + 1].Y);
					}
				}

				bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				counter = 0;

				foreach (Line line in this)
					line.DrawToImage(Debug.GetColor(counter++), bmpData);
			}
			catch { }
			finally
			{
				if (bmpData != null)
				{
					result.UnlockBits(bmpData);
					result.Save(filePath, ImageFormat.Png);
					result.Dispose();
				}
			}
#endif
		}
		#endregion

		#region DrawToImage()
		public void DrawToImage(Bitmap result)
		{
#if SAVE_RESULTS
			BitmapData bmpData = null;

			this.Sort(new Line.VerticalComparer());

			try
			{
				int counter = 0;
				Graphics g = Graphics.FromImage(result);
				Pen linePen = new Pen(Color.White, 3f);

				foreach (Line line in this)
				{
					List<Rectangle> rects = RectanglesToDraw(line);

					for (int i = 0; i < rects.Count; i++)
					{
						g.DrawLine(linePen, rects[i].X, rects[i].Y, rects[i].Right, rects[i].Y);

						if (i < (rects.Count - 1))
							g.DrawLine(linePen, rects[i].Right, rects[i].Y, rects[i + 1].X, rects[i + 1].Y);
					}
				}

				bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				counter = 0;

				foreach (Line line in this)
					line.DrawToImage(Debug.GetColor(counter++), bmpData);
			}
			catch { }
			finally
			{
				if (bmpData != null)
					result.UnlockBits(bmpData);

				GC.Collect();
			}
#endif
		}
		
		public void DrawToImage(Bitmap result, Color color)
		{
#if SAVE_RESULTS
			BitmapData bmpData = null;

			try
			{
				this.Sort(new Line.VerticalComparer());

				Graphics g = Graphics.FromImage(result);
				Pen linePen = new Pen(Color.White, 3f);

				foreach (Line line in this)
				{
					List<Rectangle> rects = RectanglesToDraw(line);

					for (int i = 0; i < rects.Count; i++)
					{
						g.DrawLine(linePen, rects[i].X, rects[i].Y, rects[i].Right, rects[i].Y);

						if (i < (rects.Count - 1))
							g.DrawLine(linePen, rects[i].Right, rects[i].Y, rects[i + 1].X, rects[i + 1].Y);
					}
				}

				bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				foreach (Line line in this)
					line.DrawToImage(color, bmpData);
			}
			catch { }
			finally
			{
				if (bmpData != null)
					result.UnlockBits(bmpData);

				GC.Collect();
			}
#endif
		}
		#endregion
	
		#endregion

		//PRIVATE METHODS
		#region private methods

		#region MergeLines()
		private void MergeLines(Words unusedWords, Symbols unusedSymbols)
		{
			this.Sort(new Line.VerticalComparer());
			
			unusedWords.SortHorizontally();

			Lines linesToCheck = new Lines();
			Lines linesToCheckTmp = new Lines();

			foreach (Line line in this)
				linesToCheck.Add(line);

			do
			{
				foreach (Line line in linesToCheck)
				{					
					for (int i = unusedSymbols.Count - 1; i >= 0; i--)
					{
						Symbol symbol = unusedSymbols[i];

						if (AreSymbolAndLineInLine(line, symbol))
						{
							line.AddSymbol(symbol);
							unusedSymbols.RemoveAt(i);

							if (linesToCheckTmp.Contains(line) == false)
								linesToCheckTmp.Add(line);
						}
					}
				}

				for (int j = linesToCheck.Count - 1; j >= 0; j--)
				{
					Line line = linesToCheck[j];
					
					for (int i = unusedWords.Count - 1; i >= 0; i--)
					{
						if (unusedWords[i].X < line.Right + line.FontSize)
						{
							Word word = unusedWords[i];

							if (AreWordAndLineInLine(line, word))
							{
								line.AddWord(word);
								unusedWords.RemoveAt(i);

								if (linesToCheckTmp.Contains(line) == false)
									linesToCheckTmp.Add(line);
							}
						}
					}
				}

				for (int i = this.Count - 2; i >= 0; i--)
				{
					Line line1 = this[i];

					for (int k = linesToCheck.Count - 1; k >= 0; k--)
					{
						if (line1 != linesToCheck[k])
						{
							Line line2 = linesToCheck[k];

							if (Arithmetic.AreInLine(line1.Rectangle, line2.Rectangle))
							{
								int j = this.IndexOf(line2);

								if (i != j)
								{
									if (Words.AreWordsInLine(line1.LastWord, line2.FirstWord) || Words.AreWordsInLine(line2.LastWord, line1.FirstWord))
									{
										line1.Merge(line2);
										linesToCheck.RemoveAt(k);
										this.RemoveAt(j);

										if (j < i)
											i--;

										if (linesToCheckTmp.Contains(line1) == false)
											linesToCheckTmp.Add(line1);
										if (linesToCheckTmp.Contains(line2))
											linesToCheckTmp.Remove(line2);
									}
									else if (AreLinesInLine(line1, line2, i, j))
									{
										line1.Merge(line2);
										linesToCheck.RemoveAt(k);
										this.RemoveAt(j);

										if (j < i)
											i--;

										if (linesToCheckTmp.Contains(line1) == false)
											linesToCheckTmp.Add(line1);
										if (linesToCheckTmp.Contains(line2))
											linesToCheckTmp.Remove(line2);
									}
									else if (IsLineNestedInsideAnotherLine(line1, line2))
									{
										line1.Merge(line2);
										linesToCheck.RemoveAt(k);
										this.RemoveAt(j);

										if (j < i)
											i--;

										if (linesToCheckTmp.Contains(line1) == false)
											linesToCheckTmp.Add(line1);
										if (linesToCheckTmp.Contains(line2))
											linesToCheckTmp.Remove(line2);
									}
								}
							}
						}
					}
				}

				linesToCheck.Clear();

				foreach (Line line in linesToCheckTmp)
					linesToCheck.Add(line);

				linesToCheckTmp.Clear();
			}
			while (linesToCheck.Count > 0);
		}
		#endregion

		#region AreLinesInLine()
		private bool AreLinesInLine(Line line1, Line line2, int index1, int index2)
		{
			if (line1.Zone != line2.Zone)
				return false;
			if (Arithmetic.AreInLine(line1.Rectangle, line2.Rectangle) == false)
				return false;
			if (IsFontSizeSame(line1, line2) == false)
				return false;

			Symbol s1;
			Symbol s2;

			if (line1.Right < line2.X)
			{
				s1 = (line1.LastWord != null) ? line1.LastWord.LastLetter : null;
				s2 = (line2.FirstWord != null) ? line2.FirstWord.FirstLetter : null;

				if ((s1 != null) && (s2 != null) && (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5) == false))
					return false;
			}
			else if (line2.Right < line1.X)
			{
				s1 = (line1.FirstWord != null) ? line1.FirstWord.FirstLetter : null;
				s2 = (line2.LastWord != null) ? line2.LastWord.LastLetter : null;

				if ((s1 != null) && (s2 != null) && (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5) == false))
					return false;
			}
			else
				return false;

			if (line1.X > line2.X)
			{
				Line l = line1;
				line1 = line2;
				line2 = l;
			}

			Line closestLineT, closestLineB;

			int		spaceUp = GetDistanceToClosestAboveLine(line1, line2, out closestLineT, (index1 < index2) ? index1 : index2);
			int		spaceDown = GetDistanceToClosestBelowLine(line1, line2, out closestLineB, (index1 > index2) ? index1 : index2);
			int		symbolHeight = line1.FontSize;

			if (spaceUp < int.MaxValue && spaceDown < int.MaxValue && (spaceDown < spaceUp * 1.2F) && (spaceDown > spaceUp * 0.8F))
				if (closestLineT != null || closestLineB != null)
					return true;

			if (spaceUp < int.MaxValue && closestLineT != null && spaceDown < symbolHeight * 4.0 && IsFontSizeSame(line1, closestLineT))
				if ((line1.X + symbolHeight > closestLineT.X) && (line2.Right - symbolHeight < closestLineT.Right))
					return true;

			if (spaceDown < int.MaxValue && closestLineB != null && spaceDown < symbolHeight * 4.0 && IsFontSizeSame(line1, closestLineB))
				if ((line1.X + symbolHeight > closestLineB.X) && (line2.Right - symbolHeight < closestLineB.Right))
					return true;

			return false;
		}
		#endregion

		#region AreWordAndLineInLine()
		private bool AreWordAndLineInLine(Line line, Word word)
		{
			if (line.Zone != word.Zone)
				return false;
			if (Arithmetic.AreInLine(line.Rectangle, word.Rectangle) == false)
				return false;
			/*if (word.Right < (line.X - line.FontSize * 2))
				return false;
			if (word.X > (line.Right + line.FontSize * 2))
				return false;*/

			Symbol s1;
			Symbol s2;
			int shorterHeight = Math.Min(line.FontSize, word.ShortestLetterHeight);

			if (line.Right <= word.X)
			{
				if (word.X - line.Right > shorterHeight * 4.0)
					return false;
				
				s1 = (line.LastWord != null) ? line.LastWord.LastLetter : null;
				s2 = word.FirstLetter;

				if ((s1 != null) && (s2 != null))
				{
					if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
					{
						if (word.X - line.Right <= shorterHeight * 0.4)
							return true;
					}
					else
						return false;
				}
			}
			else if (word.Right <= line.X)
			{
				if (line.X - word.Right > shorterHeight * 4.0)
					return false;

				s1 = (line.FirstWord != null) ? line.FirstWord.FirstLetter : null;
				s2 = word.LastLetter;

				if (s1 != null && s2 != null)
				{
					if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
					{
						if (line.X - word.Right <= shorterHeight * 0.4)
							return true;
					}
					else
						return false;
				}
			}
			else
				return false;
			
			Line	closestLineT,closestLineB;
			int		spaceUp = GetDistanceToClosestAboveLine(line, word, out closestLineT);
			int		spaceDown = GetDistanceToClosestBelowLine(line, word, out closestLineB);
			int		symbolHeight = line.FontSize;

			if ((spaceUp < symbolHeight * 4) && (spaceDown < symbolHeight * 4) && (spaceDown < spaceUp * 1.2F) && (spaceDown > spaceUp * 0.8F))
				if (closestLineT != null || closestLineB != null)
					return true;

			if ((spaceUp < symbolHeight * 4.0) && closestLineT != null)
				if ((Math.Min(line.X, word.X) + symbolHeight > closestLineT.X) && (Math.Max(line.Right, word.Right) - symbolHeight < closestLineT.Right))
					return true;

			if ((spaceDown < symbolHeight * 4.0) && closestLineB != null)
				if ((Math.Min(line.X, word.X) + symbolHeight > closestLineB.X) && (Math.Max(line.Right, word.Right) - symbolHeight < closestLineB.Right))
					return true;

			return false;
		}
		#endregion

		#region AreSymbolAndLineInLine()
		private bool AreSymbolAndLineInLine(Line line, Symbol symbol)
		{
			if (line.Zone != symbol.Zone)
				return false;
			if (Arithmetic.AreInLine(line.Rectangle, symbol.Rectangle) == false)
				return false;
			if (symbol.Right < (line.X - line.FontSize * 2))
				return false;
			if (symbol.X > (line.Right + line.FontSize * 2))
				return false;

			Symbol lineSymbol;

			if (line.Right <= symbol.X)
			{
				lineSymbol = (line.LastWord != null) ? line.LastWord.LastLetter : null;

				if (lineSymbol != null)
				{
					if(Arithmetic.AreInLine(lineSymbol.Rectangle, symbol.Rectangle, 0.5))
					{
						if (symbol.X - line.Right <= line.FontSize * 0.4)
							return true;
					}
					else
						return false;
				}
			}
			else if (symbol.Right <= line.X)
			{
				lineSymbol = (line.FirstWord != null) ? line.FirstWord.FirstLetter : null;

				if (lineSymbol != null)
				{
					if (Arithmetic.AreInLine(lineSymbol.Rectangle, symbol.Rectangle, 0.5))
					{
						if (line.X - symbol.Right < line.FontSize * 0.4)
							return true;
					}
					else
						return false;
				}
			}
			else
				return false;

			Line closestLineT, closestLineB;
			int spaceUp = GetDistanceToClosestAboveLine(line, symbol, out closestLineT);
			int spaceDown = GetDistanceToClosestBelowLine(line, symbol, out closestLineB);
			int fontSize = line.FontSize;

			if ((spaceUp < fontSize * 4) && (spaceDown < fontSize * 4) && (spaceDown < spaceUp * 1.2F) && (spaceDown > spaceUp * 0.8F))
				if (closestLineT != null || closestLineB != null)
					return true;

			if ((spaceUp < fontSize * 4.0) && closestLineT != null)
				if ((Math.Min(line.X, symbol.X) + fontSize > closestLineT.X) && (Math.Max(line.Right, symbol.Right) - fontSize < closestLineT.Right))
					return true;

			if ((spaceDown < fontSize * 4.0) && closestLineB != null)
				if ((Math.Min(line.X, symbol.X) + fontSize > closestLineB.X) && (Math.Max(line.Right, symbol.Right) - fontSize < closestLineB.Right))
					return true;

			return false;
		}
		#endregion

		#region GetDistanceToClosestAboveLine()
		private int GetDistanceToClosestAboveLine(Line theLine, Word word, out Line closestLine)
		{
			int shortestDistance = int.MaxValue;
			closestLine = null;
			
			foreach (Line line in this)
			{
				if ((line != theLine) && (line.Y < theLine.Y) && (line.Y < word.Y))
				{
					int distance;
					int lineLineVerticalShare = GetLinesVerticalIntersectionWidth(line, theLine);
					int lineWordVerticalShare = GetVerticalShare(line, word);
					
					if (lineLineVerticalShare >= 0)
					{
						distance = theLine.GetSeatAt(lineLineVerticalShare) - line.GetSeatAt(lineLineVerticalShare);
						
						if ((shortestDistance * 1.1) > distance)
						{
							if (lineWordVerticalShare >= 0)
								closestLine = line;
							else if ((distance * 1.1) < shortestDistance)
								closestLine = null;

							shortestDistance = (shortestDistance < distance) ? shortestDistance : distance;
						}
					}
					
					if (lineWordVerticalShare >= 0)
					{
						distance = word.Seat - line.GetSeatAt(lineWordVerticalShare);
						
						if ((shortestDistance * 1.1) > distance)
						{
							if (lineLineVerticalShare >= 0)
								closestLine = line;
							else if ((distance * 1.1) < shortestDistance)
								closestLine = null;

							shortestDistance = (shortestDistance < distance) ? shortestDistance : distance;
						}
					}
				}
			}
			
			return shortestDistance;
		}
		#endregion

		#region GetDistanceToClosestAboveLine()
		private int GetDistanceToClosestAboveLine(Line theLine, Symbol symbol, out Line closestLine)
		{
			int shortestDistance = int.MaxValue;
			closestLine = null;

			foreach (Line line in this)
			{
				if ((line != theLine) && (line.Y < theLine.Y) && (line.Y < symbol.Y))
				{
					int distance;
					int lineLineVerticalShare = GetLinesVerticalIntersectionWidth(line, theLine);
					int lineWordVerticalShare = GetVerticalShare(line, symbol);

					if (lineLineVerticalShare >= 0)
					{
						distance = theLine.GetSeatAt(lineLineVerticalShare) - line.GetSeatAt(lineLineVerticalShare);

						if ((shortestDistance * 1.1) > distance)
						{
							if (lineWordVerticalShare >= 0)
								closestLine = line;
							else if ((distance * 1.1) < shortestDistance)
								closestLine = null;

							shortestDistance = (shortestDistance < distance) ? shortestDistance : distance;
						}
					}

					if (lineWordVerticalShare >= 0)
					{
						distance = symbol.Bottom - line.GetSeatAt(lineWordVerticalShare);

						if ((shortestDistance * 1.1) > distance)
						{
							if (lineLineVerticalShare >= 0)
								closestLine = line;
							else if ((distance * 1.1) < shortestDistance)
								closestLine = null;

							shortestDistance = (shortestDistance < distance) ? shortestDistance : distance;
						}
					}
				}
			}

			return shortestDistance;
		}
		#endregion

		#region GetDistanceToClosestAboveLine()
		private int GetDistanceToClosestAboveLine(Line line1, Line line2, out Line closestLine, int index)
		{
			int shortestDistance = int.MaxValue;
			closestLine = null;
			
			for (int i = index - 1; i >= 0; i--)
			{
				Line line = this[i];
				
				if ((line != line1) && (line != line2) && (line.Y < line1.Y) && (line.Y < line2.Y))
				{
					int distance;
					int verticalShare1 = GetLinesVerticalIntersectionWidth(line, line1);
					int verticalShare2 = GetLinesVerticalIntersectionWidth(line, line2);
					
					if ((verticalShare1 >= 0) && (line.GetSeatAt(verticalShare1) < line1.GetShoulderAt(verticalShare1)))
					{
						distance = line1.GetSeatAt(verticalShare1) - line.GetSeatAt(verticalShare1);
						
						if ((shortestDistance * 1.1) > distance)
						{
							if (verticalShare2 >= 0)
								closestLine = line;
							else if ((distance * 1.1) < shortestDistance)
								closestLine = null;

							shortestDistance = (shortestDistance < distance) ? shortestDistance : distance;
						}
						else if ((distance * 2) > shortestDistance)
						{
							return shortestDistance;
						}
					}

					if ((verticalShare2 >= 0) && (line.GetSeatAt(verticalShare2) < line2.GetShoulderAt(verticalShare2)))
					{
						distance = line2.GetSeatAt(verticalShare2) - line.GetSeatAt(verticalShare2);
						
						if ((shortestDistance * 1.1) > distance)
						{
							if (verticalShare1 >= 0)
								closestLine = line;
							else if ((distance * 1.1) < shortestDistance)
								closestLine = null;

							shortestDistance = (shortestDistance < distance) ? shortestDistance : distance;
						}
						else if ((distance * 2) > shortestDistance)
						{
							return shortestDistance;
						}
					}
				}
			}
			
			return shortestDistance;
		}
		#endregion

		#region GetDistanceToClosestBelowLine()
		private int GetDistanceToClosestBelowLine(Line theLine, Word word, out Line closestLine)
		{
			int shortestDistance = int.MaxValue;
			closestLine = null;

			foreach (Line line in this)
			{
				if (((line != theLine) && (line.Y > theLine.Y)) && (line.Y > word.Y))
				{
					int distance;
					int lineLineVerticalShare = GetLinesVerticalIntersectionWidth(line, theLine);
					int lineWordVerticalShare = GetVerticalShare(line, word);
					
					if (lineLineVerticalShare >= 0)
					{
						distance = line.GetSeatAt(lineLineVerticalShare) - theLine.GetSeatAt(lineLineVerticalShare);
						
						if ((shortestDistance * 1.1) > distance)
						{
							if (lineWordVerticalShare >= 0)
								closestLine = line;
							else if ((distance * 1.1) < shortestDistance)
								closestLine = null;

							shortestDistance = (shortestDistance < distance) ? shortestDistance : distance;
						}
					}
					
					if (lineWordVerticalShare >= 0)
					{
						distance = line.GetSeatAt(lineWordVerticalShare) - word.Seat;
						if ((shortestDistance * 1.1) > distance)
						{
							if (lineLineVerticalShare >= 0)
								closestLine = line;
							else if ((distance * 1.1) < shortestDistance)
								closestLine = null;

							shortestDistance = (shortestDistance < distance) ? shortestDistance : distance;
						}
					}
				}
			}
			return shortestDistance;
		}
		#endregion

		#region GetDistanceToClosestBelowLine()
		private int GetDistanceToClosestBelowLine(Line theLine, Symbol symbol, out Line closestLine)
		{
			int shortestDistance = int.MaxValue;
			closestLine = null;

			foreach (Line line in this)
			{
				if (((line != theLine) && (line.Y > theLine.Y)) && (line.Y > symbol.Y))
				{
					int distance;
					int lineLineVerticalShare = GetLinesVerticalIntersectionWidth(line, theLine);
					int lineWordVerticalShare = GetVerticalShare(line, symbol);

					if (lineLineVerticalShare >= 0)
					{
						distance = line.GetSeatAt(lineLineVerticalShare) - theLine.GetSeatAt(lineLineVerticalShare);

						if ((shortestDistance * 1.1) > distance)
						{
							if (lineWordVerticalShare >= 0)
								closestLine = line;
							else if ((distance * 1.1) < shortestDistance)
								closestLine = null;

							shortestDistance = (shortestDistance < distance) ? shortestDistance : distance;
						}
					}

					if (lineWordVerticalShare >= 0)
					{
						distance = line.GetSeatAt(lineWordVerticalShare) - symbol.Bottom;
						if ((shortestDistance * 1.1) > distance)
						{
							if (lineLineVerticalShare >= 0)
								closestLine = line;
							else if ((distance * 1.1) < shortestDistance)
								closestLine = null;

							shortestDistance = (shortestDistance < distance) ? shortestDistance : distance;
						}
					}
				}
			}
			return shortestDistance;
		}
		#endregion

		#region GetDistanceToClosestBelowLine()
		private int GetDistanceToClosestBelowLine(Line line1, Line line2, out Line closestLine, int index)
		{
			int shortestDistance = int.MaxValue;
			closestLine = null;
			
			for (int i = index; i < this.Count; i++)
			{
				Line line = this[i];
				
				if ((line != line1) && (line != line2) && (line.Y > line1.Y) && (line.Y > line2.Y))
				{
					int distance;
					int verticalShare1 = GetLinesVerticalIntersectionWidth(line, line1);
					int verticalShare2 = GetLinesVerticalIntersectionWidth(line, line2);

					if ((verticalShare1 >= 0) && (line.GetShoulderAt(verticalShare1) > line1.GetSeatAt(verticalShare1)))
					{
						distance = line.GetSeatAt(verticalShare1) - line1.GetSeatAt(verticalShare1);
						
						if ((shortestDistance * 1.1) > distance)
						{
							if (verticalShare2 >= 0)
								closestLine = line;
							else if ((distance * 1.1) < shortestDistance)
								closestLine = null;

							shortestDistance = (shortestDistance < distance) ? shortestDistance : distance;
						}
						else if ((distance * 2) > shortestDistance)
						{
							return shortestDistance;
						}
					}

					if ((verticalShare2 >= 0) && (line.GetShoulderAt(verticalShare2) > line2.GetSeatAt(verticalShare2)))
					{
						distance = line.GetSeatAt(verticalShare2) - line2.GetSeatAt(verticalShare2);
						
						if ((shortestDistance * 1.1) > distance)
						{
							if (verticalShare1 >= 0)
								closestLine = line;
							else if ((distance * 1.1) < shortestDistance)
								closestLine = null;

							shortestDistance = (shortestDistance < distance) ? shortestDistance : distance;
						}
						else if ((distance * 2) > shortestDistance)
						{
							return shortestDistance;
						}
					}
				}
			}

			return shortestDistance;
		}
		#endregion

		#region GetJoint()
		private static Point GetJoint(Line line, Delimiter delimiter)
		{
			if (Rectangle.Intersect(line.Rectangle, delimiter.Rectangle) != Rectangle.Empty)
			{
				Line2D lineDel = new Line2D((PointF)delimiter.P1, (PointF)delimiter.P2);
				Line2D lineTop = new Line2D((double)line.X, (double)line.Y, (double)line.Right, (double)line.Y);
				Line2D lineBottom = new Line2D((double)line.X, (double)line.Bottom, (double)line.Right, (double)line.Bottom);
				double x = 0.0;
				double y = 0.0;
				
				if (lineDel.InterceptPoint(lineTop, ref x, ref y) && line.Rectangle.Contains((int)x, (int)y))
					return new Point((int)x, (int)y);
				
				if (lineDel.InterceptPoint(lineBottom, ref x, ref y) && line.Rectangle.Contains((int)x, (int)y))
					return new Point((int)x, (int)y);
			}
			
			return new Point(-1, -1);
		}
		#endregion

		#region GetVerticalShare()
		private static int GetVerticalShare(Line line, Word word)
		{
			if (((line.X <= word.X) && (line.Right >= word.X)) || ((line.X >= word.X) && (line.X <= word.Right)))
			{
				if (line.X >= word.X)
					return line.X;
				else
					return word.X;
			}

			return -1;
		}
		
		private static int GetVerticalShare(Line line, Symbol symbol)
		{
			if (((line.X <= symbol.X) && (line.Right >= symbol.X)) || ((line.X >= symbol.X) && (line.X <= symbol.Right)))
			{
				if (line.X >= symbol.X)
					return line.X;
				else
					return symbol.X;
			}

			return -1;
		}
		#endregion

		#region IsLineNestedInsideAnotherLine()
		private static bool IsLineNestedInsideAnotherLine(Line line1, Line line2)
		{
			Word wordL = null;
			Word wordR = null;
			Words words = null;
			
			if (Rectangle.Intersect(line2.Rectangle, line1.Rectangle) == line1.Rectangle)
			{
				wordL = line1.FirstWord;
				wordR = line1.LastWord;
				words = line2.Words;
			}
			else if (Rectangle.Intersect(line2.Rectangle, line1.Rectangle) == line2.Rectangle)
			{
				wordL = line2.FirstWord;
				wordR = line2.LastWord;
				words = line1.Words;
			}
			
			if (words != null)
			{
				int i;

				for (i = words.Count - 1; i >= 0; i--)
				{
					if (words[i].Right < wordL.X)
					{
						if (!Words.AreWordsInLine(words[i], wordL))
							break;

						return true;
					}
				}

				for (i = 0; i < words.Count - 1; i++)
				{
					if (words[i].X > wordR.Right)
					{
						if (Words.AreWordsInLine(words[i], wordR))
							return true;

						break;
					}
				}
			}

			return false;
		}
		#endregion

		#region AdjacentLineExists()
		private bool AdjacentLineExists(Symbol symbol)
		{
			foreach (Line line in this)
				if (line.X < symbol.Right && line.Right > symbol.X)
					return true;

			return false;
		}
		#endregion

		#region IsFontSizeSame()
		private static bool IsFontSizeSame(Line line1, Line line2)
		{
			int size1 = line1.FontSize;
			int size2 = line2.FontSize;

			return (size1 > size2 * 0.80) && (size2 > size1 * 0.80);
		}
		#endregion

		#region GetLineContainingSymbol()
		static Line GetLineContainingSymbol(Lines lines, Symbol symbol)
		{
			foreach (Line line in lines)
				if (line.Symbols.Contains(symbol))
					return line;

			return null;
		}
		#endregion

		#region GetTitles()
		/// <summary>
		/// Works with all unused symbols. Assigns each symbol to a group of symbols on the same line.
		/// Then validates if those symbols make line.
		/// </summary>
		/// <param name="words"></param>
		/// <param name="symbols"></param>
		private static Lines GetTitles(Words words, Symbols symbols)
		{
			Symbols symbolsToRemove = new Symbols();
			Lines titles = new Lines();

			symbols.SortHorizontally();

			for (int i = symbols.Count - 1; i >= 0; i--)
			{
				Symbol symbol = symbols[i];

				if (symbol.IsLetter && symbol.Word == null)
				{
					{
						int weight;
						Symbol symbolL = symbols.GetSymbolToTheLeft(i, out weight);
						Symbol symbolR = symbols.GetSymbolToTheRight(i, out weight);

						if (symbolL != null && symbolR != null && Line.AreLineCandidates(symbolL, symbol, symbolR))
						{
							Line lineL = GetLineContainingSymbol(titles, symbolL);
							Line lineC = GetLineContainingSymbol(titles, symbol);
							Line lineR = GetLineContainingSymbol(titles, symbolR);

							Line line = new Line(symbolL, symbol, symbolR);
							titles.Add(line);

							if (lineL != null)
							{
								Line lineToRemove = lineL;
								line.Merge(lineL);
								titles.Remove(lineToRemove);
							}

							if (lineC != null)
							{
								Line lineToRemove = lineC;
								line.Merge(lineC);
								titles.Remove(lineToRemove);
							}

							if (lineR != null)
							{
								Line lineToRemove = lineR;
								line.Merge(lineR);
								titles.Remove(lineToRemove);
							}

							symbolsToRemove.Add(symbolL);
							symbolsToRemove.Add(symbol);
							symbolsToRemove.Add(symbolR);
						}
					}
				}
			}

			foreach (Line title in titles)
				foreach (Symbol symbol in title.Symbols)
					if (symbols.Contains(symbol))
						symbols.Remove(symbol);

			foreach (Line title in titles)
			{
				Word word = new Word(title.Symbols[0]);

				for (int i = 1; i < title.Symbols.Count; i++)
					word.AddSymbol(title.Symbols[i]);

				title.AddWord(word);
				title.Symbols.Clear();
			}

			return titles;
		}
		#endregion

		#region RectanglesToDraw()
		public List<Rectangle> RectanglesToDraw(Line line)
		{
			List<Rectangle> rects = new List<Rectangle>();

			for (int i = 0; i < line.Words.Count; i++)
				rects.Add(Rectangle.FromLTRB(line.Words[i].X, line.Words[i].Seat, line.Words[i].Right, line.Words[i].Seat));

			for (int i = 0; i < line.Symbols.Count; i++)
				rects.Add(Rectangle.FromLTRB(line.Symbols[i].X, line.Symbols[i].Bottom, line.Symbols[i].Right, line.Symbols[i].Bottom));

			rects.Sort(new RectangleHorizontalComparer());

			return rects;
		}
		#endregion
	
		#endregion

	}

}
