using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.PageObjects
{
	public class Paragraph : IComparable, IPageObject
	{
		public readonly Words	Words = new Words();
		public readonly Symbols Symbols = new Symbols();
		Lines					lines = null;
		
		int			x = int.MaxValue;
		int			y = int.MaxValue;
		int			right = int.MinValue;
		int			bottom = int.MinValue;
		int?		fontSize = null;

		#region constructor
		public Paragraph(Symbol symbol)
		{
			this.Symbols.Add(symbol);

			this.x = symbol.X;
			this.y = symbol.Y;
			this.right = symbol.Right;
			this.bottom = symbol.Bottom;
		}

		public Paragraph(Word word)
		{
			this.Words.Add(word);
			word.Paragraph = this;

			this.x = word.X;
			this.y = word.Y;
			this.right = word.Right;
			this.bottom = word.Bottom;
		}

		/*public Paragraph(Words words)
		{
			if(words.Count > 0)
			{
				this.Words.Add(words[0]);
				words[0].Paragraph = this;

				this.x = words[0].X;
				this.y = words[0].Y;
				this.right = words[0].Right;
				this.bottom = words[0].Bottom;

				for(int i = 1; i < words.Count; i++)
					AddWord(words[i]);
			}
		}*/

		/*public Paragraph(Rectangle rect)
		{
			this.x = rect.X;
			this.y = rect.Y;
			this.right = rect.Right;
			this.bottom = rect.Bottom;
		}*/
		#endregion

		#region class PageComparer
		public class PageComparer : IComparer<Paragraph>
		{
			public int Compare(Paragraph p1, Paragraph p2)
			{
				int maxX = (p1.X > p2.X) ? p1.X : p2.X;
				int minR = (p1.Right < p2.Right) ? p1.Right : p2.Right;

				if( (minR - maxX) > (p1.Width * .2) || (minR - maxX) > (p2.Width * .2))
				{
					//paragraphs are 1 above and 1 below
					if(p1.Y > p2.Y)
						return 1;
					else if(p1.Y < p2.Y)
						return -1;
					else 
						return 0;
				}
				else
				{
					if(p1.X > p2.X)
						return 1;
					else if(p1.X < p2.X)
						return -1;
					else 
						return 0;
				}
			}
		} 
		#endregion


		#region class HorizontalComparer
		public class HorizontalComparer : IComparer<Paragraph>
		{
			public int Compare(Paragraph symbol1, Paragraph symbol2)
			{
				if (symbol1.X > symbol2.X)
					return 1;
				if (symbol1.X < symbol2.X)
					return -1;
				else
					return 0;
			}
		}
		#endregion

		#region class VerticalComparer
		public class VerticalComparer : IComparer<Paragraph>
		{
			public int Compare(Paragraph symbol1, Paragraph symbol2)
			{
				if (symbol1.Y > symbol2.Y)
					return 1;
				if (symbol1.Y < symbol2.Y)
					return -1;
				else
					return 0;
			}
		}
		#endregion
	
		//PUBLIC PROPERTIES
		#region public properties
		public int			X		{get {return this.x;} }
		public int			Y		{get {return this.y;} }
		public int			Right	{get {return this.right;} }
		public int			Bottom	{get {return this.bottom;} }
		public int			Width	{get {return this.Right - this.x;} }
		public int			Height	{get {return this.Bottom - this.y;} }
		public Rectangle	Rectangle{get {return Rectangle.FromLTRB(x, y, right, bottom);} }
		public DelimiterZone Zone { get { return (Words.Count > 0) ? Words[0].Zone : ((Symbols.Count > 0) ? Symbols[0].Zone : null); } }
		
		public int			FontSize 
		{ 
			get 
			{
				if (fontSize.HasValue == false)
					fontSize = Words.FontSize;

				return fontSize.Value; 
			} 
		}

		public Lines Lines
		{
			get
			{
				if (this.lines == null)
				{
					this.lines = new Lines();

					foreach (Word word in this.Words)
						if (word.Line != null && lines.Contains(word.Line) == false)
							lines.Add(word.Line);
				}

				return this.lines;
			}
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region IsLineParagraph()
		public bool IsLineParagraph()
		{				
			this.Words.Sort(new Word.HorizontalComparer());
				
			for(int i = 1; i < this.Words.Count; i++)
			{
				Word word1 = this.Words[i-1];
				Word word2 = this.Words[i];
					
				if( (word1.Bottom < word2.Y) || (word1.Y > word2.Bottom) )
					return false;
			}

			return true;
		}
		#endregion

		#region AddWord()
		public void AddWord(Word word)
		{
			this.Words.Add(word);
			word.Paragraph = this;
			this.fontSize = null;

			if(this.x > word.X)
				this.x = word.X;
			if(this.y > word.Y)
				this.y = word.Y;
			if(this.right < word.Right)
				this.right = word.Right;
			if(this.bottom < word.Bottom)
				this.bottom = word.Bottom;

			this.lines = null;
		}
		#endregion

		#region AddSymbol()
		public void AddSymbol(Symbol symbol)
		{
			this.Symbols.Add(symbol);

			if (this.x > symbol.X)
				this.x = symbol.X;
			if (this.y > symbol.Y)
				this.y = symbol.Y;
			if (this.right < symbol.Right)
				this.right = symbol.Right;
			if (this.bottom < symbol.Bottom)
				this.bottom = symbol.Bottom;

			this.lines = null;
		}
		#endregion

		#region Merge()
		public void Merge(Paragraph paragraph)
		{
			this.fontSize = null;

			foreach (Word word in paragraph.Words)
				if (word.Paragraph != this)
					this.AddWord(word);

			foreach (Symbol symbol in paragraph.Symbols)
				this.Symbols.Add(symbol);

			if (this.x > paragraph.X)
				this.x = paragraph.X;
			if (this.y > paragraph.Y)
				this.y = paragraph.Y;
			if (this.right < paragraph.Right)
				this.right = paragraph.Right;
			if (this.bottom < paragraph.Bottom)
				this.bottom = paragraph.Bottom;

			this.lines = null;
		}
		#endregion

		#region IComparable Members

		public int CompareTo(object obj)
		{
			if(this.Y < ((Paragraph) obj).Y )
				return -1;
			else if(this.Y == ((Paragraph) obj).Y )
				return 0;
				
			return 1;
		}

		#endregion

		#region GetObjectToTheLeft()
		public IPageObject GetObjectToTheLeft(Word w)
		{
			if ((w.Zone == this.Zone) && (w.FirstLetter != null))
			{
				foreach (Word word in this.Words)
					if ((word.Right < w.X) && (word.LastLetter != null) && Arithmetic.AreInLine(w.FirstLetter.Rectangle, word.LastLetter.Rectangle, 0.5))
						if ((w.X - word.Right) < (this.FontSize * 3))
							return word;

				foreach (Symbol symbol in this.Symbols)
					if ((symbol.Right < w.X) && Arithmetic.AreInLine(w.FirstLetter.Rectangle, symbol.Rectangle, 0.5))
						if ((w.X - symbol.Right) < (this.FontSize * 3))
							return symbol;
			}

			return null;
		}

		public IPageObject GetObjectToTheLeft(Symbol s)
		{
			if ((s != null) && (s.Zone == this.Zone))
			{
				foreach (Word word in this.Words)
					if ((word.Right < s.X) && (word.LastLetter != null) && Arithmetic.AreInLine(s.Rectangle, word.LastLetter.Rectangle, 0.5))
						if ((s.X - word.Right) < (this.FontSize * 3))
							return word;

				foreach (Symbol symbol in this.Symbols)
					if ((symbol.Right < s.X) && Arithmetic.AreInLine(s.Rectangle, symbol.Rectangle, 0.5))
						if ((s.X - symbol.Right) < (this.FontSize * 3))
							return symbol;
			}

			return null;
		}
		#endregion

		#region GetObjectToTheRight()
		public IPageObject GetObjectToTheRight(Word w)
		{
			if ((w.Zone == this.Zone) && (w.LastLetter != null))
			{
				foreach (Word word in this.Words)
					if ((word.X > w.Right) && (word.FirstLetter != null) && Arithmetic.AreInLine(w.LastLetter.Rectangle, word.FirstLetter.Rectangle, 0.5))
						if ((word.X - w.Right) < (this.FontSize * 3))
							return word;

				foreach (Symbol symbol in this.Symbols)
					if ((symbol.X > w.Right) && Arithmetic.AreInLine(w.LastLetter.Rectangle, symbol.Rectangle, 0.5))
						if ((symbol.X - w.Right) < (this.FontSize * 3))
							return symbol;
			}

			return null;
		}

		public IPageObject GetObjectToTheRight(Symbol s)
		{
			if ((s.Zone == this.Zone))
			{
				foreach (Word word in this.Words)
					if ((word.X > s.Right) && (word.FirstLetter != null) && Arithmetic.AreInLine(s.Rectangle, word.FirstLetter.Rectangle, 0.5))
						if ((word.X - s.Right) < (this.FontSize * 3))
							return word;

				foreach (Symbol symbol in this.Symbols)
					if ((symbol.X > s.Right) && Arithmetic.AreInLine(s.Rectangle, symbol.Rectangle, 0.5))
						if ((symbol.X - s.Right) < (this.FontSize * 3))
							return symbol;
			}

			return null;
		}
		#endregion

		#region GetObjectAbove()
		public IPageObject GetObjectAbove(Word w)
		{
			IPageObject bestCandidate = null;

			if ((w.Zone == this.Zone))
			{
				int shortestDistance = int.MaxValue;

				foreach (Word word in this.Words)
				{
					if ((word.Bottom <= w.Y) && Arithmetic.AreInLine(word.X, word.Right, w.X, w.Right))
					{
						int distance = w.Y - word.Bottom;

						if (distance < (this.FontSize * 4) && distance < shortestDistance)
						{
							bestCandidate = word;
							shortestDistance = distance;
						}
					}
				}

				foreach (Symbol symbol in this.Symbols)
				{
					if ((symbol.Bottom <= w.Y) && Arithmetic.AreInLine(symbol.X, symbol.Right, w.X, w.Right))
					{
						int distance = w.Y - symbol.Bottom;

						if (distance < (this.FontSize * 4) && distance < shortestDistance)
						{
							bestCandidate = symbol;
							shortestDistance = distance;
						}
					}
				}
			}

			return bestCandidate;
		}
	
		public IPageObject GetObjectAbove(Symbol s)
		{
			IPageObject bestCandidate = null;

			if ((s.Zone == this.Zone))
			{
				int shortestDistance = int.MaxValue;

				foreach (Word word in this.Words)
				{
					if ((word.Bottom <= s.Y) && Arithmetic.AreInLine(word.X, word.Right, s.X, s.Right))
					{
						int distance = s.Y - word.Bottom;

						if (distance < (this.FontSize * 4) && distance < shortestDistance)
						{
							bestCandidate = word;
							shortestDistance = distance;
						}
					}
				}

				foreach (Symbol symbol in this.Symbols)
				{
					if ((symbol.Bottom <= s.Y) && Arithmetic.AreInLine(symbol.X, symbol.Right, s.X, s.Right))
					{
						int distance = s.Y - symbol.Bottom;

						if (distance < (this.FontSize * 4) && distance < shortestDistance)
						{
							bestCandidate = symbol;
							shortestDistance = distance;
						}
					}
				}
			}

			return bestCandidate;
		}
		#endregion

		#region GetObjectsBelow()
		public IPageObject GetObjectsBelow(Word w)
		{
			IPageObject bestCandidate = null;

			if ((w.Zone == this.Zone))
			{
				int shortestDistance = int.MaxValue;

				foreach (Word word in this.Words)
				{
					if ((word.Y >= w.Bottom) && Arithmetic.AreInLine(word.X, word.Right, w.X, w.Right))
					{
						int distance = word.Y - w.Bottom;

						if (distance < (this.FontSize * 4) && distance < shortestDistance)
						{
							bestCandidate = word;
							shortestDistance = distance;
						}
					}
				}

				foreach (Symbol symbol in this.Symbols)
				{
					if ((symbol.Y >= w.Bottom) && Arithmetic.AreInLine(symbol.X, symbol.Right, w.X, w.Right))
					{
						int distance = symbol.Y - w.Bottom;

						if (distance < (this.FontSize * 4) && distance < shortestDistance)
						{
							bestCandidate = symbol;
							shortestDistance = distance;
						}
					}
				}
			}

			return bestCandidate;
		}

		public IPageObject GetObjectsBelow(Symbol s)
		{
			IPageObject bestCandidate = null;

			if ((s.Zone == this.Zone))
			{
				int shortestDistance = int.MaxValue;

				foreach (Word word in this.Words)
				{
					if ((word.Y >= s.Bottom) && Arithmetic.AreInLine(word.X, word.Right, s.X, s.Right))
					{
						int distance = word.Y - s.Bottom;

						if (distance < (this.FontSize * 4) && distance < shortestDistance)
						{
							bestCandidate = word;
							shortestDistance = distance;
						}
					}
				}

				foreach (Symbol symbol in this.Symbols)
				{
					if ((symbol.Y >= s.Bottom) && Arithmetic.AreInLine(symbol.X, symbol.Right, s.X, s.Right))
					{
						int distance = symbol.Y - s.Bottom;

						if (distance < (this.FontSize * 4) && distance < shortestDistance)
						{
							bestCandidate = symbol;
							shortestDistance = distance;
						}
					}
				}
			}

			return bestCandidate;
		}
		#endregion

		#region ReEvanuateLines()
		internal void ReEvanuateLines(PageObjects pageObjects)
		{
			bool changed = false;
			Lines pLines = new Lines();

			do
			{
				// get paragraph lines
				foreach (Line line in pageObjects.Lines)
				{
					bool added = false;

					foreach (Word pWord in this.Words)
						if (line.Words.Contains(pWord))
						{
							pLines.Add(line);
							added = true;
							break;
						}

					if (added == false)
					{
						foreach (Symbol pSymbol in this.Symbols)
							if (line.Symbols.Contains(pSymbol))
							{
								pLines.Add(line);
								break;
							}
					}
				}

				changed = MergeParagraphLinesWithWordsAndSymbols(pageObjects, pLines);
			} while (changed);

			this.lines = null;
		}
		#endregion

		#region ResetLines()
		public void ResetLines()
		{
			this.lines = null;
		}
		#endregion

		#region DrawToImage()
		public void DrawToImage(Bitmap bitmap, Color color)
		{
#if SAVE_RESULTS
			BitmapData bmpData = null;

			try
			{
				this.Lines.DrawToImage(bitmap, color);

				bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

				foreach (Word word in this.Words)
					if (word.Line == null)
						word.DrawToImage(color, bmpData);

				foreach (Symbol symbol in this.Symbols)
				{
					if (GetLine(symbol, this.Lines) == null)
						symbol.ObjectMap.DrawToImage(color, bmpData);
				}
			}
			catch { }
			finally
			{
				if (bmpData != null)
					bitmap.UnlockBits(bmpData);

				GC.Collect();
			}
#endif
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region MergeParagraphLinesWithWordsAndSymbols()
		/// <summary>
		/// returns true if lines set changed
		/// </summary>
		/// <param name="pageObjects"></param>
		/// <param name="paragraphLines"></param>
		/// <returns></returns>
		private bool MergeParagraphLinesWithWordsAndSymbols(PageObjects pageObjects, Lines paragraphLines)
		{
			int		fontSize = this.Words.FontSize;
			bool	changed = false;

			this.Symbols.SortHorizontally();
			this.Words.SortHorizontally();

			//add unlined words to the lines
			for (int i = this.Words.Count - 1; i >= 0; i--)
			{
				if (this.Words[i].Line == null)
				{
					Word word = this.Words[i];
					int distanceLw, distanceLs, distanceRw, distanceRs;
					Word adjacentLw = GetWordLeftAdjacent(word, i, fontSize, out distanceLw);
					Symbol adjacentLs = GetSymbolLeftAdjacent(word, fontSize, out distanceLs);
					Word adjacentRw = GetWordRightAdjacent(word, i, fontSize, out distanceRw);
					Symbol adjacentRs = GetSymbolRightAdjacent(word, fontSize, out distanceRs);

					Line leftLine = null, rightLine = null;

					//left side
					if ((adjacentLw != null && adjacentLs != null && distanceLw <= distanceLs) || (adjacentLw != null && adjacentLs == null))
					{
						leftLine = adjacentLw.Line;

						//add word
						if (leftLine != null)
							leftLine.AddWord(word);
						else
						{
							leftLine = new Line(adjacentLw, word);
							pageObjects.Lines.Add(leftLine);
							paragraphLines.Add(leftLine);
						}
					}
					else if ((adjacentLw != null && adjacentLs != null && distanceLw > distanceLs) || (adjacentLw == null && adjacentLs != null))
					{
						//add symbol
						leftLine = GetLine(adjacentLs, paragraphLines);

						if (leftLine != null)
							leftLine.AddWord(word);
						else
						{
							leftLine = new Line(word, adjacentLs);
							pageObjects.Lines.Add(leftLine);
							paragraphLines.Add(leftLine);
						}
					}

					// right side
					if ((adjacentRw != null && adjacentRs != null && distanceRw <= distanceRs) || (adjacentRw != null && adjacentRs == null))
					{
						// there is a word and symbol to the left
						rightLine = adjacentRw.Line;

						//add word
						if (rightLine != null)
							rightLine.AddWord(word);
						else
						{
							rightLine = new Line(adjacentRw, word);
							pageObjects.Lines.Add(rightLine);
							paragraphLines.Add(rightLine);
						}
					}
					else if ((adjacentRw != null && adjacentRs != null && distanceRw > distanceRs) || (adjacentRw == null && adjacentRs != null))
					{
						//add symbol
						rightLine = GetLine(adjacentRs, paragraphLines);

						if (rightLine != null)
							rightLine.AddWord(word);
						else
						{
							rightLine = new Line(word, adjacentRs);
							pageObjects.Lines.Add(rightLine);
							paragraphLines.Add(rightLine);
						}
					}

					if (leftLine != null && rightLine != null && leftLine != rightLine)
					{
						leftLine.Merge(rightLine);
						pageObjects.Lines.Remove(rightLine);
						paragraphLines.Remove(rightLine);
					}

					if (word.Line != null)
						changed = true;
				}
			}

			//add unlined symbols to the lines
			for (int i = this.Symbols.Count - 1; i >= 0; i--)
			{
				if (GetLine(this.Symbols[i], paragraphLines) == null)
				{
					Symbol	symbol = this.Symbols[i];
					int		distanceLw, distanceLs, distanceRw, distanceRs;
					Word	adjacentLw = GetWordLeftAdjacent(symbol, fontSize, out distanceLw);
					Symbol	adjacentLs = GetSymbolLeftAdjacent(symbol, i, fontSize, out distanceLs);
					Word	adjacentRw = GetWordRightAdjacent(symbol, fontSize, out distanceRw);
					Symbol	adjacentRs = GetSymbolRightAdjacent(symbol, i, fontSize, out distanceRs);

					Line leftLine = null, rightLine = null;
					bool removeSymbolFromUnusedWords = false;

					//left side
					if ((adjacentLw != null && adjacentLs != null && distanceLw <= distanceLs) || (adjacentLw != null && adjacentLs == null))
					{
						// there is a symbol and symbol to the left
						leftLine = adjacentLw.Line;
						removeSymbolFromUnusedWords = true;

						//add symbol
						if (leftLine != null)
							leftLine.AddSymbol(symbol);
						else
						{
							leftLine = new Line(adjacentLw, symbol);
							pageObjects.Lines.Add(leftLine);
							paragraphLines.Add(leftLine);
						}
					}
					else if ((adjacentLw != null && adjacentLs != null && distanceLw > distanceLs) || (adjacentLw == null && adjacentLs != null))
					{
						//add symbol
						leftLine = GetLine(adjacentLs, paragraphLines);

						if (leftLine != null)
						{
							leftLine.AddSymbol(symbol);
							removeSymbolFromUnusedWords = true;
						}
						/*else
						{
							leftLine = new Line(symbol, adjacentLs);
							pageObjects.Lines.Add(leftLine);
							paragraphLines.Add(leftLine);
							removeSymbolFromUnusedWords = true;
						}*/
					}

					// right side
					if ((adjacentRw != null && adjacentRs != null && distanceRw <= distanceRs) || (adjacentRw != null && adjacentRs == null))
					{
						//add symbol
						rightLine = adjacentRw.Line;
						removeSymbolFromUnusedWords = true;

						if (rightLine != null)
							rightLine.AddSymbol(symbol);
						else
						{
							rightLine = new Line(adjacentRw, symbol);
							pageObjects.Lines.Add(rightLine);
							paragraphLines.Add(leftLine);
						}
					}
					else if ((adjacentRw != null && adjacentRs != null && distanceRw > distanceRs) || (adjacentRw == null && adjacentRs != null))
					{
						//add symbol
						rightLine = GetLine(adjacentRs, paragraphLines);

						if (rightLine != null)
						{
							rightLine.AddSymbol(symbol);
							removeSymbolFromUnusedWords = true;
						}
						/*else
						{
							rightLine = new Line(symbol, adjacentRs);
							pageObjects.Lines.Add(rightLine);
							paragraphLines.Add(leftLine);
							removeSymbolFromUnusedWords = true;
						}*/
					}

					if (leftLine != null && rightLine != null && leftLine != rightLine)
					{
						leftLine.Merge(rightLine);
						pageObjects.Lines.Remove(rightLine);
						paragraphLines.Remove(rightLine);
					}

					if (removeSymbolFromUnusedWords)
						changed = true;
				}
			}

			//merge lines
			if(MergeLinesInParagraph(pageObjects, paragraphLines))
				changed = true;

			return changed;
		}
		#endregion

		#region GetWordLeftAdjacent()
		private Word GetWordLeftAdjacent(Word theWord, int index, int fontSize, out int distance)
		{
			distance = int.MaxValue;

			for (int i = index - 1; i >= 0; i--)
			{
				if (theWord.X > this.Words[i].Right)
				{
					Word testedWord = this.Words[i];

					Symbol s1 = testedWord.LastLetter;
					Symbol s2 = theWord.FirstLetter;

					if ((theWord.Zone == testedWord.Zone) && (s1 != null) && (s2 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							distance = theWord.X - testedWord.Right;

							if (distance < (fontSize * 2.8))
								return testedWord;
							else
								return null;
						}
					}
				}
			}

			return null;
		}

		private Word GetWordLeftAdjacent(Symbol symbol, int fontSize, out int distance)
		{
			distance = int.MaxValue;

			for (int i = this.Words.Count - 1; i >= 0; i--)
			{
				if (symbol.X > this.Words[i].Right)
				{
					Word testedWord = this.Words[i];

					Symbol s1 = testedWord.LastLetter;
					Symbol s2 = symbol;

					if ((symbol.Zone == testedWord.Zone) && (s1 != null) && (s2 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							distance = symbol.X - testedWord.Right;

							if (distance < (fontSize * 2.8))
								return testedWord;
							else
								return null;
						}
					}
				}
			}

			return null;
		}
		#endregion

		#region GetWordRightAdjacent()
		private Word GetWordRightAdjacent(Word theWord, int index, int fontSize, out int distance)
		{
			distance = int.MaxValue;

			for (int i = index + 1; i < this.Words.Count; i++)
			{
				if (theWord.Right < this.Words[i].X)
				{
					Word testedWord = this.Words[i];

					Symbol s1 = theWord.LastLetter;
					Symbol s2 = testedWord.FirstLetter;

					if ((theWord.Zone == testedWord.Zone) && (s1 != null) && (s2 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							distance = testedWord.X - theWord.Right;

							if (distance < (fontSize * 2.8))
								return testedWord;
							else
								return null;
						}
					}
				}
			}

			return null;
		}

		private Word GetWordRightAdjacent(Symbol symbol, int fontSize, out int distance)
		{
			distance = int.MaxValue;

			for (int i = 0; i < this.Words.Count; i++)
			{
				if (symbol.Right < this.Words[i].X)
				{
					Word testedWord = this.Words[i];

					Symbol s1 = symbol;
					Symbol s2 = testedWord.FirstLetter;

					if ((symbol.Zone == testedWord.Zone) && (s1 != null) && (s2 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							distance = testedWord.X - symbol.Right;

							if (distance < (fontSize * 2.8))
								return testedWord;
							else
								return null;
						}
					}
				}
			}

			return null;
		}
		#endregion

		#region GetSymbolLeftAdjacent()
		private Symbol GetSymbolLeftAdjacent(Word word, int fontSize, out int distance)
		{
			distance = int.MaxValue;

			for (int i = this.Symbols.Count - 1; i >= 0; i--)
			{
				if (word.X > this.Symbols[i].Right)
				{
					Symbol testedSymbol = this.Symbols[i];
					Symbol s1 = word.FirstLetter;

					if ((word.Zone == testedSymbol.Zone) && (s1 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, testedSymbol.Rectangle, 0.5))
						{
							distance = word.X - testedSymbol.Right;

							if (distance < (fontSize * 2.8))
								return testedSymbol;
							else
								return null;
						}
					}
				}
			}

			return null;
		}

		private Symbol GetSymbolLeftAdjacent(Symbol symbol, int index, int fontSize, out int distance)
		{
			distance = int.MaxValue;

			for (int i = index - 1; i >= 0; i--)
			{
				if (symbol.X > this.Symbols[i].Right)
				{
					Symbol testedSymbol = this.Symbols[i];
					Symbol s1 = symbol;

					if ((symbol.Zone == testedSymbol.Zone) && (s1 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, testedSymbol.Rectangle, 0.5))
						{
							distance = symbol.X - testedSymbol.Right;

							if (distance < (fontSize * 2.8))
								return testedSymbol;
							else
								return null;
						}
					}
				}
			}

			return null;
		}
		#endregion

		#region GetSymbolRightAdjacent()
		private Symbol GetSymbolRightAdjacent(Word word, int fontSize, out int distance)
		{
			distance = int.MaxValue;
			
			for (int i = 0; i < this.Symbols.Count; i++)
			{
				if (word.Right < this.Symbols[i].X)
				{
					Symbol s1 = word.LastLetter;
					Symbol testedSymbol = this.Symbols[i];

					if ((word.Zone == testedSymbol.Zone) && (s1 != null) && (testedSymbol != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, testedSymbol.Rectangle, 0.5))
						{
							distance = testedSymbol.X - word.Right;

							if (distance < (fontSize * 2.8))
								return testedSymbol;
							else
								return null;
						}
					}
				}
			}

			return null;
		}

		private Symbol GetSymbolRightAdjacent(Symbol symbol, int index, int fontSize, out int distance)
		{
			distance = int.MaxValue;

			for (int i = index + 1; i < this.Symbols.Count; i++)
			{
				if (symbol.Right < this.Symbols[i].X)
				{
					Symbol s1 = symbol;
					Symbol testedSymbol = this.Symbols[i];

					if ((symbol.Zone == testedSymbol.Zone) && (s1 != null) && (testedSymbol != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, testedSymbol.Rectangle, 0.5))
						{
							distance = testedSymbol.X - symbol.Right;

							if (distance < (fontSize * 2.8))
								return testedSymbol;
							else
								return null;
						}
					}
				}
			}

			return null;
		}
		#endregion

		#region GetLine()
		private Line GetLine(Symbol symbol, Lines lines)
		{
			foreach (Line line in lines)
				if (line.Symbols.Contains(symbol))
					return line;

			return null;
		}
		#endregion

		#region MergeLinesInParagraph()
		private bool MergeLinesInParagraph(PageObjects pageObjects, Lines paragraphLines)
		{
			bool changed = false;
			paragraphLines.SortVertically();

			Lines linesToCheck = new Lines();
			Lines linesToCheckTmp = new Lines();

			linesToCheck.AddRange(paragraphLines);

			while (linesToCheck.Count > 0)
			{
				for (int i = paragraphLines.Count - 2; i >= 0; i--)
				{
					Line line1 = paragraphLines[i];

					for (int k = linesToCheck.Count - 1; k >= 0; k--)
					{
						if (line1 != linesToCheck[k])
						{
							Line line2 = linesToCheck[k];

							if (Arithmetic.AreInLine(line1.Rectangle, line2.Rectangle))
							{
								int j = paragraphLines.IndexOf(line2);

								if (AreLinesInLine(line1, line2) || IsLineNestedInsideAnotherLine(line1, line2))
								{
									line1.Merge(line2);
									linesToCheck.RemoveAt(k);
									paragraphLines.RemoveAt(j);
									pageObjects.Lines.Remove(line2);

									if (j < i)
										i--;

									if (linesToCheckTmp.Contains(line1) == false)
										linesToCheckTmp.Add(line1);
									if (linesToCheckTmp.Contains(line2))
										linesToCheckTmp.Remove(line2);

									changed = true;
								}
							}
						}
					}
				}

				linesToCheck.Clear();
				linesToCheck.AddRange(linesToCheckTmp);
				linesToCheckTmp.Clear();
			}

			return changed;
		}
		#endregion

		#region AreLinesInLine()
		private bool AreLinesInLine(Line line1, Line line2)
		{
			if (line1.Right < line2.X)
			{
				Rectangle? r1 = line1.LastSymbolRect;
				Rectangle? r2 = line2.FirstSymbolRect;

				if ((r1 != null) && (r2 != null) && Arithmetic.AreInLine(r1.Value, r2.Value, 0.5) && (r2.Value.X - r1.Value.Right < line1.FontSize * 2.8))
					return true;
			}
			else if (line2.Right < line1.X)
			{
				Rectangle? r1 = line1.FirstSymbolRect;
				Rectangle? r2 = line2.LastSymbolRect;

				if ((r1 != null) && (r2 != null) && Arithmetic.AreInLine(r1.Value, r1.Value, 0.5) && (r1.Value.X - r2.Value.Right < line1.FontSize * 2.8))
					return true;
			}

			return false;
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
						if (!AreWordsInLine(words[i], wordL))
							break;

						return true;
					}
				}

				for (i = 0; i < (words.Count - 1); i++)
				{
					if (words[i].X > wordR.Right)
					{
						if (AreWordsInLine(words[i], wordR))
							return true;

						break;
					}
				}
			}

			return false;
		}
		#endregion

		#region AreWordsInLine()
		private static bool AreWordsInLine(Word wL, Word wR)
		{
			Symbol s1 = (wL.X < wR.X) ? wL.LastLetter : wR.LastLetter;
			Symbol s2 = (wL.X < wR.X) ? wR.FirstLetter : wL.FirstLetter;

			if ((wL.Zone == wR.Zone) && (s1 != null) && (s2 != null) && Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
			{
				int distance = s2.X - s1.Right;
				int smallerHeight = (wL.ShortestLetterHeight < wR.ShortestLetterHeight) ? wL.ShortestLetterHeight : wR.ShortestLetterHeight;

				if (distance < (smallerHeight * 2.8))
					return true;
			}

			return false;
		}
		#endregion

		#endregion


	}
}
