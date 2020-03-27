using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.PageObjects
{
	public class Word : IComparable, IPageObject
	{
		public readonly Symbols Letters = new Symbols();

		int x;
		int y;
		int right;
		int bottom;
		int seat = int.MaxValue;
		int shoulder = int.MinValue;
		public Line Line = null;
		public Paragraph Paragraph = null;
		DelimiterZone zone = null;

		#region constructor
		public Word(Symbol letter)
		{
			this.Letters.Add(letter);
			letter.Word = this;

			this.x = letter.X;
			this.y = letter.Y;
			this.right = letter.Right;
			this.bottom = letter.Bottom;

			this.seat = this.bottom;
			this.shoulder = this.y;

			if (letter.Zone != null)
				this.zone = letter.Zone;
		}

		public Word(Symbol o1, Symbol o2)
			: this(o1)
		{
			this.AddSymbol(o2);

			if (this.zone == null && o1.Zone != null)
				this.zone = o1.Zone;
			else if (this.zone == null && o2.Zone != null)
				this.zone = o2.Zone;
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties
		public int X { get { return this.x; } }
		public int Y { get { return this.y; } }
		public int Right { get { return this.right; } }
		public int Bottom { get { return this.bottom; } }
		public int Width { get { return this.right - this.x; } }
		public int Height { get { return this.bottom - this.y; } }

		public int XHalf { get { return this.X + (this.right - this.x) / 2; } }
		public int Seat { get { return this.seat; } }
		public int Shoulder { get { return this.shoulder; } }

		public bool				IsWord { get { return (this.seat < int.MaxValue); } }
		public Rectangle		Rectangle{get{return Rectangle.FromLTRB(this.x, this.y, this.right, this.bottom);}}
		public DelimiterZone	Zone { get { return zone; } 
			set { this.zone = value; } }

		#region FirstLetter
		public Symbol FirstLetter
		{
			get
			{
				foreach (Symbol symbol in this.Letters)
					if (symbol.IsLetter)
						return symbol;

				return null;
			}
		}
		#endregion

		#region LastLetter
		public Symbol LastLetter
		{
			get
			{
				for (int i = this.Letters.Count - 1; i >= 0; i--)
					if (this.Letters[i].IsLetter)
						return this.Letters[i];

				return null;
			}
		}
		#endregion

		#region ShortestLetterHeight
		public ushort ShortestLetterHeight
		{
			get
			{
				ushort shortestLetterHeight = ushort.MaxValue;
				
				foreach (Symbol symbol in this.Letters)
					if (symbol.IsLetter && (symbol.Height < shortestLetterHeight))
						shortestLetterHeight = (ushort)symbol.Height;
				
				return ((shortestLetterHeight == ushort.MaxValue) ? ((ushort)0) : shortestLetterHeight);
			}
		}
		#endregion

		#endregion

		#region class HorizontalComparer
		public class HorizontalComparer : System.Collections.Generic.IComparer<Word>
		{
			public int Compare(Word word1, Word word2)
			{
				if (word1.X > word2.X)
					return 1;
				if (word1.X < word2.X)
					return -1;

				return 0;
			}
		}
		#endregion

		#region class VerticalComparer
		public class VerticalComparer : System.Collections.Generic.IComparer<Word>
		{
			public int Compare(Word word1, Word word2)
			{
				if (word1.Y > word2.Y)
					return 1;
				if (word1.Y < word2.Y)
					return -1;

				return 0;
			}
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region AddSymbol()
		public void AddSymbol(Symbol symbol)
		{
			this.InsertObjectToList(symbol);
			symbol.Word = this;
			
			if (this.x > symbol.X)
				this.x = symbol.X;
			if (this.y > symbol.Y)
				this.y = symbol.Y;
			if (this.right < symbol.Right)
				this.right = symbol.Right;
			if (this.bottom < symbol.Bottom)
				this.bottom = symbol.Bottom;
			
			if ((symbol.IsLetter && (symbol.Bottom > this.Shoulder)) && (symbol.Y < this.Seat))
			{
				if (this.seat > symbol.Bottom)
					this.seat = symbol.Bottom;
				if (this.shoulder < symbol.Y)
					this.shoulder = symbol.Y;
			}
		}
		#endregion

		#region CheckForPunctuations()
		public void CheckForPunctuations(int textHeight)
		{
			this.CheckForComaBehingWord(textHeight);
			
			if (this.Letters.Count > 2)
			{
				Symbol shortestSymbol = null;
				
				foreach (Symbol symbol in this.Letters)
					if (symbol.IsLetter && ((shortestSymbol == null) || (shortestSymbol.Height > symbol.Height)))
						shortestSymbol = symbol;
				
				if ((shortestSymbol != null) && ((this.Seat - this.Shoulder) < (shortestSymbol.Height / 2)))
				{
					int oldSeat = this.Seat;
					int oldShoulder = this.Shoulder;
					shortestSymbol.ObjectType = Symbol.Type.Quote | Symbol.Type.DoubleQuote | Symbol.Type.QuestionMark | Symbol.Type.Exclamation | Symbol.Type.Dash | Symbol.Type.AnyPunctuation | Symbol.Type.Semicolon | Symbol.Type.Comma | Symbol.Type.Colon | Symbol.Type.Dot;
					
					this.RefreshSeetAndShoulder();
					
					if ((this.Seat != oldSeat) || (this.Shoulder != oldShoulder))
						this.CheckForPunctuations(textHeight);
					else
					{
						shortestSymbol.ObjectType = Symbol.Type.Letter;
						this.RefreshSeetAndShoulder();
					}
				}
			}
		}
		#endregion

		#region CompareTo()
		public int CompareTo(object obj)
		{
			if (this.Y < ((Word)obj).Y)
				return -1;
			if (this.Y == ((Word)obj).Y)
				return 0;

			return 1;
		}
		#endregion

		#region CountHeadsAndLegs()
		public void CountHeadsAndLegs(ref int heads, ref int legs)
		{
			if (this.Letters.Count > 1)
			{
				int regLetterHeight = int.MaxValue;

				foreach (Symbol letter in this.Letters)
					if (letter.IsLetter && (regLetterHeight > letter.Height))
						regLetterHeight = letter.Height;

				regLetterHeight = Convert.ToInt32((float)(regLetterHeight * 1.2f));
				int delta = Convert.ToInt32((float)(regLetterHeight * 0.2f));
				
				foreach (Symbol letter in this.Letters)
					if (letter.Height > regLetterHeight)
					{
						if (letter.Bottom > (this.Seat + delta))
							legs++;
						else
							heads++;
					}
			}
		}
		#endregion

		#region GetAngle()
		/// <summary>
		/// Returns true if word's skew can be decided. 
		/// </summary>
		/// <param name="averageLetterHeight"> Average height of all letters on the page.</param>
		/// <param name="opticsCenter"></param>
		/// <param name="weightRatio">Square of longer distance from top or bottom page edge to the optics center.</param>
		/// <param name="angle"></param>
		/// <param name="weight"></param>
		/// <returns></returns>
		public bool GetAngle(int averageLetterHeight, out double angle, out double weight)
		{
			int i;

			angle = 0.0;
			weight = 0.0;
			
			Symbol letterL = null;
			Symbol letterR = null;
			
			for (i = 0; i < this.Letters.Count; i++)
			{
				if (this.Letters[i].IsLetter && (this.Letters[i].Height < (averageLetterHeight * 1.2f)))
				{
					letterL = this.Letters[i];
					break;
				}
			}
			for (i = this.Letters.Count - 1; i >= 0; i--)
			{
				if (this.Letters[i].IsLetter && (this.Letters[i].Height < (averageLetterHeight * 1.2f)))
				{
					letterR = this.Letters[i];
					break;
				}
			}

			if (letterL != letterR)
			{			
				angle = Arithmetic.GetAngle((double)letterL.X, (double)letterL.Bottom, (double)letterR.Right, (double)letterR.Bottom);
				weight = letterR.MiddleX - letterL.MiddleX;
				return true;
			}

			return false;
		}
		#endregion
	
		#region GetBfPoint()
		public bool GetBfPoint(int averageLetterHeight, out Point bfPoint)
		{
			bfPoint = new Point(this.X + (this.Width / 2), this.Seat);
			if ((this.Letters.Count != 0) && (((this.Letters.Count != 1) || (this.FirstLetter == null)) || (this.FirstLetter.Width <= this.FirstLetter.Height)))
			{
				int i;
				Symbol letterL = null;
				Symbol letterR = null;
				for (i = 0; i < this.Letters.Count; i++)
				{
					if (this.Letters[i].IsLetter && (this.Letters[i].Height < (averageLetterHeight * 1.2f)))
					{
						letterL = this.Letters[i];
						break;
					}
				}
				for (i = this.Letters.Count - 1; i >= 0; i--)
				{
					if (this.Letters[i].IsLetter && (this.Letters[i].Height < (averageLetterHeight * 1.2f)))
					{
						letterR = this.Letters[i];
						break;
					}
				}
				if ((letterL != null) && (letterR != null))
				{
					bfPoint = new Point(letterL.X + ((letterR.X - letterL.X) / 2), letterL.Bottom + ((letterR.Bottom - letterL.Bottom) / 2));
					return true;
				}
			}
			return false;
		}
		#endregion

		#region Merge()
		public void Merge(Word word)
		{
			foreach (Symbol o in word.Letters)
			{
				if (!this.Letters.Contains(o))
					this.AddSymbol(o);

				o.Word = this;
			}
		}
		#endregion

		#region RefreshSeetAndShoulder()
		public void RefreshSeetAndShoulder()
		{
			this.seat = int.MaxValue;
			this.shoulder = int.MinValue;

			foreach (Symbol letter in this.Letters)
			{
				if (letter.IsLetter)
				{
					if (this.seat > letter.Bottom)
						this.seat = letter.Bottom;
					if (this.shoulder < letter.Y)
						this.shoulder = letter.Y;
				}
			}
		}
		#endregion

		#region AreWordCandidates
		public static bool AreWordCandidates(Symbol s1, Symbol s2, Size imageSize)
		{
			int widest = (s1.Width > s2.Width) ? s1.Width : s2.Width;
			int shorter = (s1.Height < s2.Height) ? s1.Height : s2.Height;
			//int tallest = (s1.Height > s2.Height) ? s1.Height : s2.Height;
			int sharedHeight = (((s1.Bottom < s2.Bottom) ? s1.Bottom : s2.Bottom) - ((s1.Y > s2.Y) ? s1.Y : s2.Y));
			int distance = (s2.X - s1.Right > s1.X - s2.Right) ? s2.X - s1.Right : s1.X - s2.Right;

			if ((s1.Width < (s2.Width * 8)) && (s1.Width > (s2.Width / 8)))
				if ((s1.Height < (s2.Height * 2)) && (s1.Height > (s2.Height / 2)))
					//if( (s1.Pixels < (s2.Pixels * 4)) && (s1.Pixels > (s2.Pixels / 4)) )
					if ((shorter < sharedHeight * 2.0) && (distance < shorter * 0.40) && (widest < imageSize.Width / 3))
						return true;

			return false;
		}

		/// <summary>
		/// Order of symbols is important. s1 is the symbol to the left, s2 is in the middle, s3 to the right.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <param name="s3"></param>
		/// <param name="bitmap"></param>
		/// <returns></returns>
		/*public static bool AreWordCandidates(Symbol s1, Symbol s2, Symbol s3, Bitmap bitmap)
		{
			if ((s3 != null) && s3.IsLetter)
			{
				int widest = ((s1.Width > s2.Width) ? ((s1.Width > s3.Width) ? s1.Width : s3.Width) : ((s2.Width > s3.Width) ? s2.Width : s3.Width));
				int shortest = (s2.Height < s1.Height) ? ((s2.Height < s3.Height) ? s2.Height : s3.Height) : ((s1.Height < s3.Height) ? s1.Height : s3.Height);
				int tallest = (s2.Height > s1.Height) ? ((s2.Height > s3.Height) ? s2.Height : s3.Height) : ((s1.Height > s3.Height) ? s1.Height : s3.Height);

				if ( (shortest * 2.0f > tallest) && (widest < bitmap.Width * 0.3F))
				{
					int distance1 = s2.X - s1.Right;
					int distance2 = s3.X - s2.Right;

					if ((distance1 > distance2 * 0.8) && (distance1 < distance2 * 1.2) && (distance1 < shortest) && (distance2 < shortest))
					{
						int difference1 = Math.Abs((int)(s2.Bottom - s1.Bottom));
						int difference2 = Math.Abs((int)(s2.Bottom - s3.Bottom));

						if ((difference1 < shortest * 0.1) && (difference2 < (shortest * 0.1)))
							return true;
					}
				}
			}

			return false;
		}*/
		#endregion

		#region AreWordCandidates
		public static bool ShouldLetterBeAttachedToTheWord(Word word, Symbol s)
		{
			//letter used for comparisons
			Symbol sW = (s.X < word.X) ? word.FirstLetter : word.LastLetter;
			
			if (sW != null)
			{
				int widest = sW.Width;
				int shortest = sW.Height;
				int sharedHeight = (sW != null) ? (((sW.Bottom < s.Bottom) ? sW.Bottom : s.Bottom) - ((sW.Y > s.Y) ? sW.Y : s.Y)) : 0;
				int distance = (s.X - word.Right > word.X - s.Right) ? s.X - word.Right : word.X - s.Right;

				foreach (Symbol letter in word.Letters)
					if (letter.IsLetter)
					{
						if (widest < letter.Width)
							widest = letter.Width;
						if (shortest < letter.Height)
							shortest = letter.Height;
					}

				if ((sW.Width < (s.Width * 8.0)) && (sW.Width > (s.Width / 8.0)))
					if ((word.ShortestLetterHeight < (s.Height * 2.0)) && (sW.Height > (s.Height / 2.0)))
						if ((shortest < sharedHeight * 2.0) && (distance < shortest * 0.40))
							return true;
			}

			return false;
		}
		#endregion

		#region DrawToImage()
		public void DrawToImage(Color color, BitmapData bmpData)
		{	
			foreach (Symbol symbol in Letters)
				symbol.ObjectMap.DrawToImage(color, bmpData);
		}

		public void DrawToImage(BitmapData bmpData)
		{
			int index = 0;

			foreach (Symbol symbol in Letters)
				symbol.ObjectMap.DrawToImage(Debug.GetColor(index++), bmpData);
		}
		#endregion

		#region ToString()
		public override string ToString()
		{
			return string.Format("[{0},{1}] W:{2}, H:{3}, R={4}, B={5}", x, y, right - x, bottom - y, right, bottom);
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region CheckForComaBehingWord()
		private void CheckForComaBehingWord(int textHeight)
		{
			if (((this.Letters.Count > 2) && (this.Letters[this.Letters.Count - 1].Y >= this.Shoulder)) && this.Letters[this.Letters.Count - 1].IsLetter)
			{
				Symbol lastSymbol = this.Letters[this.Letters.Count - 1];
				Symbol prevSymbol = this.Letters[this.Letters.Count - 2];

				if ((lastSymbol.Y > (prevSymbol.Y + (this.Seat - prevSymbol.Y) / 2)) && (lastSymbol.Height < textHeight * .66F))
				{
					lastSymbol.ObjectType = Symbol.Type.Comma;
					this.RefreshSeetAndShoulder();
				}
			}
		}
		#endregion

		#region CheckForDoubleQuotes()
		private void CheckForDoubleQuotes()
		{
			Symbol quote1;
			Symbol quote2;
			Symbol lastLetter;
			
			if (((this.Letters.Count > 2) && this.Letters[0].IsLetter) && this.Letters[1].IsLetter)
			{
				quote1 = this.Letters[0];
				quote2 = this.Letters[1];
				lastLetter = this.Letters[2];
				
				if ((lastLetter.IsLetter && (quote1.Bottom < (lastLetter.Bottom - ((lastLetter.Bottom - this.Shoulder) / 2)))) && (quote2.Bottom < (lastLetter.Bottom - ((lastLetter.Bottom - this.Shoulder) / 2))))
				{
					quote2.Merge(quote1);
					this.Letters.RemoveAt(0);
					quote2.ObjectType = Symbol.Type.DoubleQuote;
					
					this.RefreshSeetAndShoulder();
				}
			}
			
			if (((this.Letters.Count > 2) && this.Letters[this.Letters.Count - 1].IsLetter) && this.Letters[this.Letters.Count - 2].IsLetter)
			{
				quote1 = this.Letters[this.Letters.Count - 1];
				quote2 = this.Letters[this.Letters.Count - 2];
				lastLetter = this.Letters[this.Letters.Count - 2];
				
				if ((lastLetter.IsLetter && (quote1.Bottom < (lastLetter.Bottom - ((lastLetter.Bottom - this.Shoulder) / 2)))) && (quote2.Bottom < (lastLetter.Bottom - ((lastLetter.Bottom - this.Shoulder) / 2))))
				{
					quote2.Merge(quote1);
					this.Letters.RemoveAt(this.Letters.Count - 1);
					quote2.ObjectType = Symbol.Type.DoubleQuote;
					
					this.RefreshSeetAndShoulder();
				}
			}
		}
		#endregion

		#region InsertObjectToList()
		private void InsertObjectToList(Symbol letter)
		{
			for (int i = 0; i < this.Letters.Count; i++)
				if (this.Letters[i].X > letter.X)
				{
					this.Letters.Insert(i, letter);
					return;
				}

			this.Letters.Add(letter);
		}
		#endregion

		#endregion

	}

}
