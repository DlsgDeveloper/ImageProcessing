using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.PageObjects
{
	public class Words : List<Word>
	{
		SortType sortType = SortType.None;

		#region constructor()
		public Words()
		{
		}

		public Words(Symbols symbols, Size imageSize)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			symbols.MergeNesterdObjects();

			foreach (Symbol symbol in symbols)
				if (symbol.Word != null)
					symbol.Word = null;

			Symbols letters = new Symbols();

			foreach (Symbol symbol in symbols)
				if (symbol.IsLetter)
					letters.Add(symbol);

			letters.SortHorizontally();

			for (int i = 1; i < letters.Count; i++)
			{
				int weight;
				Symbol symbol = letters[i];
				Symbol leftAdjacent = letters.GetSymbolToTheLeft(i, out weight);
				Symbol rightAdjacent = letters.GetSymbolToTheRight(i, out weight);

				if (leftAdjacent != null && Word.AreWordCandidates(leftAdjacent, symbol, imageSize))
					AddSymbols(leftAdjacent, symbol);

				if (rightAdjacent != null && Word.AreWordCandidates(symbol, rightAdjacent, imageSize))
					AddSymbols(symbol, rightAdjacent);
			}


			//DrawToFile(Debug.SaveToDir + "Words.png", raster.Size);

			Symbols punctuations = new Symbols();

			foreach (Symbol symbol in symbols)
				if (symbol.IsPunctuation)
					punctuations.Add(symbol);

#if DEBUG
			Console.WriteLine(string.Format("5 Words, constructor(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), this.Count));
#endif

			AddPunctuations(letters, punctuations);
			//DrawToFile(Debug.SaveToDir + "Words.png", imageSize);
			MergeCorruptedLetters();
#if DEBUG
			Console.WriteLine(string.Format("6 Words, constructor(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), this.Count));
#endif
			//DrawToFile(Debug.SaveToDir + "Words.png", imageSize);
			CheckForPunctuations();
			//DrawToFile(Debug.SaveToDir + "Words.png", imageSize);

			for (int i = symbols.Count - 1; i >= 0; i--)
				if (symbols[i].Word != null)
					symbols.RemoveAt(i);
#if DEBUG
			Console.WriteLine(string.Format("Words, constructor(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), this.Count));
#endif
		}
		#endregion

		#region enum SortType
		private enum SortType
		{
			Horizontal,
			Vertical,
			None
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		new public Word this[int index] { get { return (Word)base[index]; } }

		#region FontSize
		public int FontSize
		{
			get
			{
				int[] array = new int[100];
				int mostUsedHeight = 1;

				foreach (Word word in this)
					foreach (Symbol symbol in word.Letters)
						if (symbol.IsLetter && (symbol.Height < 100))
						{
							array[symbol.Height]++;
						}

				for (int i = 2; i < 98; i++)
					if (array[i - 1] + array[i] + array[i + 1] > array[mostUsedHeight - 1] + array[mostUsedHeight] + array[mostUsedHeight + 1])
						mostUsedHeight = i;

				return mostUsedHeight;
			}
		}
		#endregion

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region AddSymbols()
		public void AddSymbols(Symbol symbol1, Symbol symbol2)
		{
			if ((symbol1.Word != null) && (symbol2.Word != null))
			{
				if (symbol1.Word != symbol2.Word)
				{
					Word wordToRemove = symbol2.Word;
					symbol1.Word.Merge(symbol2.Word);

					this.Remove(wordToRemove);
				}
			}
			else if (symbol1.Word != null)
				symbol1.Word.AddSymbol(symbol2);
			else if (symbol2.Word != null)
				symbol2.Word.AddSymbol(symbol1);
			else
				this.Add(new Word(symbol1, symbol2));
		}
		#endregion

		#region CheckForPunctuations()
		public void CheckForPunctuations()
		{
			int textHeight = GetTextHeight();
			
			foreach (Word word in this)
				word.CheckForPunctuations(textHeight);
		}
		#endregion

		#region GetClip()
		public Rectangle GetClip()
		{
			int x = int.MaxValue, y = int.MaxValue, r = int.MinValue, b = int.MinValue;

			foreach (Word word in this)
			{
				if (x > word.X)
					x = word.X;
				if (y > word.Y)
					y = word.Y;
				if (r < word.Right)
					r = word.Right;
				if (b < word.Bottom)
					b = word.Bottom;
			}

			if (x == int.MaxValue || y == int.MaxValue || r == int.MinValue || b == int.MinValue)
				return Rectangle.Empty;
			else
				return Rectangle.FromLTRB(x, y, r, b);
		}
		#endregion

		#region GetSkew()
		public bool GetSkew(out double angle, out double weight)
		{
			int validWordsCount = 0;
			int averageLetterHeight = this.FontSize;

			angle = 0.0;
			weight = 0.0;

			if (averageLetterHeight > 0)
			{
				foreach (Word word in this)
				{
					double angleTmp;
					double weightTmp;

					if (word.GetAngle(averageLetterHeight, out angleTmp, out weightTmp))
					{
						angle += angleTmp * weightTmp;
						weight += weightTmp;
						validWordsCount++;
					}
				}
			}

			angle = angle / weight;
			weight = ((double)validWordsCount) / 70.0;

			return (validWordsCount > 0);
		}
		#endregion
	
		#region GetWordsInClip()
		public Words GetWordsInClip(Rectangle clip)
		{
			Words words = new Words();

			foreach (Word word in this)
			{
				Rectangle intersection = Rectangle.Intersect(clip, word.Rectangle);
	
				if ((intersection.Width * intersection.Height) > ((word.Width * word.Height) / 2))
					words.Add(word);
			}

			return words;
		}
		#endregion

		#region MergeCorruptedLetters()
		public void MergeCorruptedLetters()
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			try
			{
				foreach (Word word in this)
				{
					for (int i = word.Letters.Count - 1; i >= 0; i--)
					{
						for (int j = 0; j < i; j++)
						{
							Symbol o1 = word.Letters[i];
							Symbol o2 = word.Letters[j];

							if ( (o1.X <= o2.X && o1.Right > o2.X - 3) || (o1.X >= o2.X && o1.X < (o2.Right - 3)))
							{
								if ((((o1.Y <= o2.Y) && ((o2.Y - o1.Bottom) < 4)) && (o2.Bottom >= word.Shoulder)) ||
									(((o1.Y >= o2.Y) && ((o1.Y - o2.Bottom) < 4)) && (o1.Bottom >= word.Shoulder)))
								{
									o2.Merge(o1);
									word.Letters.RemoveAt(i);
									word.RefreshSeetAndShoulder();
									break;
								}
								else
								{
									o2 = o1;
								}
							}
						}
					}
				}
			}
			finally
			{
#if DEBUG
				Console.WriteLine(string.Format("Words, MergeCorruptedLetters(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), this.Count));
#endif
			}
		}
		#endregion

		#region AreWordsInLine()
		public static bool AreWordsInLine(Word wL, Word wR)
		{
			Symbol s1 = (wL.X < wR.X) ? wL.LastLetter : wR.LastLetter;
			Symbol s2 = (wL.X < wR.X) ? wR.FirstLetter : wL.FirstLetter;

			if ((wL.Zone == wR.Zone) && (s1 != null) && (s2 != null) && Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
			{
				int distance = s2.X - s1.Right;
				int smallerHeight = (wL.ShortestLetterHeight < wR.ShortestLetterHeight) ? wL.ShortestLetterHeight : wR.ShortestLetterHeight;

				if (distance < (smallerHeight * 2))
					return true;
			}

			return false;
		}
		#endregion

		#region Sort()
		new public void Sort()
		{
			SortHorizontally();
		}
		#endregion

		#region SortHorizontally()
		public void SortHorizontally()
		{
			if (sortType != SortType.Horizontal)
			{
				this.Sort(new Word.HorizontalComparer());
				sortType = SortType.Horizontal;
			}
		}
		#endregion

		#region SortVertically()
		public void SortVertically()
		{
			if (sortType != SortType.Vertical)
			{
				this.Sort(new Word.VerticalComparer());
				sortType = SortType.Vertical;
			}
		}
		#endregion

		#region GetRightWordAdjacent()
		public Word GetRightWordAdjacent(Word theWord, int index)
		{
			if (this.sortType != SortType.Horizontal)
				SortHorizontally();

			for (int i = index + 1; i < this.Count; i++)
			{
				if (this[i].X > theWord.Right)
				{
					Word testedWord = this[i];

					Symbol s1 = (theWord.X < testedWord.X) ? theWord.LastLetter : testedWord.LastLetter;
					Symbol s2 = (theWord.X < testedWord.X) ? testedWord.FirstLetter : theWord.FirstLetter;

					if ((theWord.Zone == testedWord.Zone) && (s1 != null) && (s2 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							int distance = testedWord.X - theWord.Right;
							int smallerHeight = (theWord.ShortestLetterHeight < testedWord.ShortestLetterHeight) ? theWord.ShortestLetterHeight : testedWord.ShortestLetterHeight;

							if (distance < (smallerHeight * 2))
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

		#region GetTextHeight()
		public int GetTextHeight()
		{
			Symbols symbols = new Symbols();

			foreach (Word word in this)
				symbols.AddRange(word.Letters);
			
			return symbols.GetTextHeight();
		}
		#endregion

		#region GetWordToTheLeft()
		internal Word GetWordToTheLeft(Symbol symbol)
		{
			for (int i = this.Count - 1; i >= 0; i--)
			{
				if (this[i].Right < symbol.X)
				{
					Word word = this[i];

					if ((symbol.Zone == word.Zone) && (word.LastLetter != null) && Arithmetic.AreInLine(symbol.Rectangle, word.LastLetter.Rectangle, 0.5))
					{
						int distance = symbol.X - word.Right;
						int smallerHeight = (symbol.Height < word.ShortestLetterHeight) ? symbol.Height : word.ShortestLetterHeight;

						if (distance < (smallerHeight * 2))
							return word;

						return null;
					}
				}
			}
			return null;
		}
		#endregion

		#region GetWordToTheRight()
		internal Word GetWordToTheRight(Symbol symbol)
		{
			for (int i = 0; i < this.Count; i++)
			{
				if (this[i].X > symbol.Right)
				{
					Word word = this[i];

					if ((symbol.Zone == word.Zone) && (word.FirstLetter != null) && Arithmetic.AreInLine(symbol.Rectangle, word.FirstLetter.Rectangle, 0.5))
					{
						int distance = word.X - symbol.Right;
						int smallerHeight = (symbol.Height < word.ShortestLetterHeight) ? symbol.Height : word.ShortestLetterHeight;

						if (distance < (smallerHeight * 2))
							return word;

						return null;
					}
				}
			}
			return null;
		}
		#endregion

		#region DrawToFile()
		public void DrawToFile(string filePath, Size imageSize)
		{
#if SAVE_RESULTS
			Bitmap result = null;
			BitmapData bmpData = null;

			try
			{
				Color color;
				result = Debug.GetBitmap(imageSize);
				int counter = 0;
				Graphics g = Graphics.FromImage(result);

				foreach (Word word in this)
				{
					color = Debug.GetColor(counter++);
					color = Color.FromArgb(100, color.R, color.G, color.B);
					g.FillRectangle(new SolidBrush(color), word.Rectangle);
				}

				bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				foreach (Word word in this)
					word.DrawToImage(Debug.GetColor(counter++), bmpData);
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

				GC.Collect();
			}
#endif
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region AddPunctuations()
		private void AddPunctuations(Symbols letters, Symbols punctuations)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			try
			{
				//bool	change;

				//do
				{
					//change = false;

					letters.Sort(new Symbol.HorizontalComparer());
					punctuations.Sort(new Symbol.HorizontalComparer());
					int index = 0;

					foreach (Symbol p in punctuations)
					{
						Word newWord;
						
						for (int i = index; i < letters.Count; i++)
							if (p.X < letters[i].X)
							{
								index = i;
								break;
							}

						Symbol letterL = GetLetterToTheLeft(letters, p, index);
						Symbol letterR = GetLetterToTheRight(letters, p, index);

						if (letterL != null)
						{
							if (letterL.Word != null)
							{
								if (PunctuationBelongsToTheWord(letterL.Word, p))
									letterL.Word.AddSymbol(p);
							}
							else
							{
								newWord = new Word(letterL);
								
								if (PunctuationBelongsToTheWord(newWord, p))
								{
									newWord.AddSymbol(p);
									Add(newWord);
								}
								else
									letterL.Word = null;
							}
						}
						if (letterR != null)
						{
							if (letterR.Word != null)
							{
								if (PunctuationBelongsToTheWord(letterR.Word, p))
								{
									if ((p.Word == null) || (p.Word == letterR.Word))
									{
										letterR.Word.AddSymbol(p);
									}
									else
									{
										Word wordToDelete = letterR.Word;
										p.Word.Merge(letterR.Word);
										Remove(wordToDelete);

										foreach (Symbol symbol in letters)
											if (symbol.Word == wordToDelete)
												symbol.Word = p.Word;

										foreach (Symbol symbol in punctuations)
											if (symbol.Word == wordToDelete)
												symbol.Word = p.Word;
									}
								}
							}
							else if (p.Word != null)
							{
								if (Word.ShouldLetterBeAttachedToTheWord(p.Word, letterR))
								{
									p.Word.AddSymbol(letterR);
								}
							}
							else
							{
								newWord = new Word(letterR);
								
								if (PunctuationBelongsToTheWord(newWord, p))
								{
									newWord.AddSymbol(p);
									Add(newWord);
								}
								else
									letterR.Word = null;
							}
						}
					}
				}
			}
			finally
			{
#if DEBUG
				Console.WriteLine(string.Format("Zoning, AddPunctuations(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), this.Count));
#endif
			}
		}
		#endregion 

		#region GetLetterToTheLeft()
		private static Symbol GetLetterToTheLeft(Symbols letters, Symbol punctuation, int limit)
		{
			Symbol	closestObject = null;
			int		weight = int.MaxValue;

			if (letters.Count > 0 && (punctuation.X >= letters[0].X))
			{
				for (int i = (limit < (letters.Count - 1)) ? limit : (letters.Count - 1); i >= 0; i--)
				{
					Symbol letter = letters[i];

					if (Arithmetic.AreInLine(letter.Y, letter.Bottom, punctuation.Y, punctuation.Bottom) && ((punctuation.X - letter.Right) < (letter.Height / 3.0)))
					{
						if ((letter.X < punctuation.X) && (letter.Right >= punctuation.X))
						{
							weight = 0;
							return letter;
						}
						
						int currentWeight = (letter.Right - punctuation.X) * (letter.Right - punctuation.X);
						
						if (weight > currentWeight)
						{
							weight = currentWeight;
							closestObject = letter;
						}
						else if (weight == currentWeight)
						{
							int sharedV1 = ((letter.Bottom < punctuation.Bottom) ? letter.Bottom : punctuation.Bottom) - ((letter.Y > punctuation.Y) ? letter.Y : punctuation.Y);
							int sharedV2 = ((closestObject.Bottom < punctuation.Bottom) ? closestObject.Bottom : punctuation.Bottom) - ((closestObject.Y > punctuation.Y) ? closestObject.Y : punctuation.Y);
							
							if (sharedV1 > sharedV2)
								closestObject = letter;
						}
					}
					
					if ((punctuation.X - letter.Right) >= 50)
						break;
				}

				if ((closestObject != null) && (((closestObject.Right - punctuation.X) <= (closestObject.Height / 2.0)) || ((closestObject.Right - punctuation.X) <= (punctuation.Height / 2.0))))
					return closestObject;
			}
			
			return null;
		}
		#endregion

		#region GetLetterToTheRight()
		private static Symbol GetLetterToTheRight(Symbols letters, Symbol punctuation, int limit)
		{
			Symbol closestObject = null;
			int weight = int.MaxValue;

			for (int i = (limit > 0) ? limit : 0; i < letters.Count; i++)
			{
				Symbol letter = letters[i];

				if (Arithmetic.AreInLine(punctuation.Y, punctuation.Bottom, letter.Y, letter.Bottom) && ((letter.X - punctuation.Right) < (letter.Height / 3.0)))
				{
					if ((punctuation.X < letter.X) && (punctuation.Right >= letter.X))
					{
						weight = 0;
						return letter;
					}
					
					int currentWeight = (punctuation.Right - letter.X) * (punctuation.Right - letter.X);
					
					if (weight > currentWeight)
					{
						weight = currentWeight;
						closestObject = letter;
					}
					else
					{
						if (weight != currentWeight)
							return closestObject;
						
						int sharedV1 = ((letter.Bottom < punctuation.Bottom) ? letter.Bottom : punctuation.Bottom) - ((letter.Y > punctuation.Y) ? letter.Y : punctuation.Y);
						int sharedV2 = ((closestObject.Bottom < punctuation.Bottom) ? closestObject.Bottom : punctuation.Bottom) - ((closestObject.Y > punctuation.Y) ? closestObject.Y : punctuation.Y);
						
						if (sharedV1 > sharedV2)
							closestObject = letter;
					}
				}
				if ((letter.X - punctuation.Right) >= 50)
					break;
			}

			if ((closestObject != null) && (((punctuation.Right - closestObject.X) <= (closestObject.Height / 2.0)) || ((punctuation.Right - closestObject.X) <= (punctuation.Height / 2.0))))
				return closestObject;

			return null;
		}
		#endregion

		#region PunctuationBelongsToTheWord()
		private static bool PunctuationBelongsToTheWord(Word word, Symbol punctuation)
		{
			Symbol adjacent;
			Symbol.Type type = GetObjectTypeInternal(word, punctuation);
			
			switch (type)
			{
				case Symbol.Type.Dash:
					punctuation.ObjectType = type;
					//word.AddSymbol(punctuation);
					return true;

				case Symbol.Type.Comma:
					adjacent = GetLeftSymbol(word, punctuation);
					if (!((adjacent != null) && adjacent.IsPunctuation))
					{
						adjacent = GetRightSymbol(word, punctuation);
					}
					if (((adjacent != null) && adjacent.IsPunctuation) && (GetObjectTypeInternal(word, adjacent) == Symbol.Type.Comma))
					{
						punctuation.Merge(adjacent);
						word.Letters.Remove(adjacent);
						punctuation.ObjectType = Symbol.Type.DoubleQuote;
						return true;
					}
					adjacent = GetUpperSymbol(word, punctuation);
					if (((adjacent != null) && adjacent.IsPunctuation) && (GetObjectTypeInternal(word, adjacent) == Symbol.Type.Dot))
					{
						punctuation.Merge(adjacent);
						word.Letters.Remove(adjacent);
						punctuation.ObjectType = Symbol.Type.Semicolon;
						return true;
					}
					punctuation.ObjectType = type;
					return true;

				case Symbol.Type.Quote:
					adjacent = GetLeftSymbol(word, punctuation);
					if (!((adjacent != null) && adjacent.IsPunctuation))
					{
						adjacent = GetRightSymbol(word, punctuation);
					}
					if (((adjacent != null) && adjacent.IsPunctuation) && (GetObjectTypeInternal(word, adjacent) == Symbol.Type.Quote))
					{
						punctuation.Merge(adjacent);
						word.Letters.Remove(adjacent);
						punctuation.ObjectType = Symbol.Type.DoubleQuote;
						return true;
					}
					punctuation.ObjectType = type;
					return true;

				case Symbol.Type.Dot:
					adjacent = GetLowerSymbol(word, punctuation);
					
					if (((adjacent != null) && adjacent.IsPunctuation) && (GetObjectTypeInternal(word, adjacent) == Symbol.Type.Dot))
					{
						punctuation.Merge(adjacent);
						word.Letters.Remove(adjacent);
						punctuation.ObjectType = Symbol.Type.Colon;
						return true;
					}
					
					if (((adjacent != null) && adjacent.IsPunctuation) && (GetObjectTypeInternal(word, adjacent) == Symbol.Type.Comma))
					{
						punctuation.Merge(adjacent);
						word.Letters.Remove(adjacent);
						punctuation.ObjectType = Symbol.Type.Semicolon;
						return true;
					}
					
					adjacent = GetUpperSymbol(word, punctuation);
					
					if (((adjacent != null) && adjacent.IsPunctuation) && (GetObjectTypeInternal(word, adjacent) == Symbol.Type.Dot))
					{
						punctuation.Merge(adjacent);
						word.Letters.Remove(adjacent);
						punctuation.ObjectType = Symbol.Type.Colon;
						return true;
					}
					
					if (((adjacent != null) && adjacent.IsLetter) && (GetRightSymbol(word, punctuation) == null))
					{
						if (((adjacent.Width <= (punctuation.Width * 1.2)) && (adjacent.Width >= (punctuation.Width * 0.8))) && (adjacent.Height > (adjacent.Width * 3f)))
						{
							punctuation.Merge(adjacent);
							word.Letters.Remove(adjacent);
							punctuation.ObjectType = Symbol.Type.Exclamation;
							word.RefreshSeetAndShoulder();
							return true;
						}
						if ((adjacent.Width <= (punctuation.Width * 2)) && (adjacent.Height > (adjacent.Width * 2f)))
						{
							punctuation.Merge(adjacent);
							word.Letters.Remove(adjacent);
							punctuation.ObjectType = Symbol.Type.QuestionMark;
							return true;
						}
					}
					
					punctuation.ObjectType = type;
					return false;
			}
			
			punctuation.ObjectType = Symbol.Type.AnyPunctuation;
			return true;
		}
		#endregion

		#region GetLeftSymbol()
		private static Symbol GetLeftSymbol(Word word, Symbol symbol)
		{
			for (int i = word.Letters.Count - 1; i >= 0; i--)
				if (word.Letters[i].X < symbol.X)
					return word.Letters[i];

			return null;
		}
		#endregion

		#region GetRightSymbol()
		private static Symbol GetRightSymbol(Word word, Symbol symbol)
		{
			for (int i = 0; i < word.Letters.Count; i++)
				if (word.Letters[i].X > symbol.Right)
					return word.Letters[i];

			return null;
		}
		#endregion

		#region GetUpperSymbol
		private static Symbol GetUpperSymbol(Word word, Symbol symbol)
		{
			for (int i = 0; i < word.Letters.Count; i++)
				if ((word.Bottom < symbol.Y) && (((word.Letters[i].X <= symbol.X) && (word.Letters[i].Right > symbol.X)) || ((word.Letters[i].X >= symbol.X) && (word.Letters[i].X < symbol.Right))))
					return word.Letters[i];
			
			return null;
		}
		#endregion

		#region GetLowerSymbol()
		private static Symbol GetLowerSymbol(Word word, Symbol symbol)
		{
			for (int i = 0; i < word.Letters.Count; i++)
				if ((word.X > symbol.Bottom) && (((word.Letters[i].X <= symbol.X) && (word.Letters[i].Right > symbol.X)) || ((word.Letters[i].X >= symbol.X) && (word.Letters[i].X < symbol.Right))))
					return word.Letters[i];

			return null;
		}
		#endregion

		#region GetObjectTypeInternal()
		private static Symbol.Type GetObjectTypeInternal(Word word, Symbol punctuation)
		{
			int punctuationCenter = punctuation.Y + (punctuation.Height / 2);
			int wordH = (word.Seat - word.Shoulder) / 2;
			int wordCenter = word.Shoulder + wordH;
			
			if (punctuationCenter < (wordCenter - (((float)wordH) / 3f)))
			{
				if ((punctuation.Width < wordH) && ((punctuation.Height > (punctuation.Width * 1.2f)) && (punctuation.Pixels > ((punctuation.Height * punctuation.Width) * 0.5f))))
				{
					return Symbol.Type.Quote;
				}
			}
			else if (punctuationCenter > (wordCenter + (((float)wordH) / 4f)))
			{
				if (punctuation.Width < wordH)
				{
					if (((punctuation.Width < (punctuation.Height * 1.2f)) && (punctuation.Height < (punctuation.Width * 1.2f))) && (punctuation.Pixels > ((punctuation.Height * punctuation.Width) * 0.66f)))
					{
						return Symbol.Type.Dot;
					}
					if ((punctuation.Height > (punctuation.Width * 1.2f)) && (punctuation.Pixels > ((punctuation.Height * punctuation.Width) * 0.5f)))
					{
						return Symbol.Type.Comma;
					}
				}
			}
			else if (punctuation.Width < wordH)
			{
				if (((punctuation.Width < (punctuation.Height * 1.2f)) && (punctuation.Height < (punctuation.Width * 1.2f))) && (punctuation.Pixels > ((punctuation.Height * punctuation.Width) * 0.66f)))
				{
					return Symbol.Type.Dot;
				}
			}
			else if ((((punctuation.Height < wordH) && (punctuation.Width > (((float)wordH) / 2f))) && (punctuation.Width > (punctuation.Height * 2f))) && (punctuation.Pixels > ((punctuation.Height * punctuation.Width) * 0.66f)))
			{
				return Symbol.Type.Dash;
			}
			
			return Symbol.Type.AnyPunctuation;
		}
		#endregion

		#endregion

	}

}
