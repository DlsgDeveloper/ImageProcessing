using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace ImageProcessing.PageObjects
{
	public class Line : IComparable, IPageObject
	{
		// Fields
		public readonly Words Words = new Words();
		public readonly Symbols Symbols = new Symbols();
		private int x;
		private int y;
		private int right;
		private int bottom;
		private int? fontSize = null;

		#region constructor
		public Line(Word word1, Word word2)
		{
			this.Words.Add(word1);
			word1.Line = this;

			this.x = word1.X;
			this.y = word1.Y;
			this.right = word1.Right;
			this.bottom = word1.Bottom;

			this.AddWord(word2);
		}

		public Line(Word word, Symbol symbol)
		{
			this.Words.Add(word);
			word.Line = this;

			this.x = word.X;
			this.y = word.Y;
			this.right = word.Right;
			this.bottom = word.Bottom;

			this.AddSymbol(symbol);
		}

		public Line(Symbol s1, Symbol s2, Symbol s3)
		{
			this.Symbols.Add(s1);

			this.x = s1.X;
			this.y = s1.Y;
			this.right = s1.Right;
			this.bottom = s1.Bottom;

			this.AddSymbol(s2);
			this.AddSymbol(s3);
		}
		#endregion

		#region class HorizontalComparer
		public class HorizontalComparer : System.Collections.Generic.IComparer<Line>
		{
			// Methods
			public int Compare(Line line1, Line line2)
			{
				if (line1.X > line2.X)
					return 1;
				if (line1.X < line2.X)
					return -1;

				return 0;
			}
		}
		#endregion

		#region class VerticalComparer
		public class VerticalComparer : System.Collections.Generic.IComparer<Line>
		{
			// Methods
			public int Compare(Line line1, Line line2)
			{
				if (line1.Y > line2.Y)
					return 1;
				if (line1.Y < line2.Y)
					return -1;

				return 0;
			}
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		public int				X { get { return this.x; } }
		public int				Y { get { return this.y; } }
		public int				Right { get { return this.right; } }
		public int				Bottom { get { return this.bottom; } }
		public int				Width { get { return (this.right - this.x); } }
		public int				Height { get { return (this.bottom - this.Y); } }
		public Word				FirstWord { get { return (this.Words.Count > 0) ? this.Words[0] : null; } }
		public Word				LastWord { get { return (this.Words.Count > 0) ? this.Words[this.Words.Count - 1] : null; } }
		public Symbol			FirstLetter { get { return ((this.FirstWord != null) ? this.FirstWord.FirstLetter : null); } }
		public Symbol			LastLetter { get { return ((this.LastWord != null) ? this.LastWord.LastLetter : null); } }
		public Rectangle		Rectangle { get { return Rectangle.FromLTRB(this.x, this.y, this.right, this.bottom); } }
		public double			Angle { get { return -Math.Atan2((double)(this.LastWord.Seat - this.FirstWord.Seat), (double)this.Width); } }
		public bool				IsValidBfLine { get { return (this.Words.Count >= 5); } }
		public DelimiterZone	Zone { get { return ((this.Words.Count > 0) ? this.Words[0].Zone : null); } }

		#region FontSize
		public int FontSize
		{
			get
			{
				if (fontSize.HasValue == false)
				{
					int[] fontSizes = new int[100];

					foreach (Word word in this.Words)
						foreach (Symbol symbol in word.Letters)
							if (symbol.IsLetter && (symbol.Height < 100))
								fontSizes[symbol.Height]++;

					int maxIndex = 16;

					for (int i = 6; i < 96; i++)
						if (fontSizes[maxIndex - 1] + fontSizes[maxIndex] + fontSizes[maxIndex + 1] < fontSizes[i - 1] + fontSizes[i] + fontSizes[i + 1])
							maxIndex = i;

					fontSize = maxIndex;
				}

				return fontSize.Value;
			}
		}
		#endregion

		#region FirstSymbolRect
		public Rectangle? FirstSymbolRect
		{
			get
			{
				Symbol firstLetter = FirstLetter;

				if (this.Symbols.Count > 0)
				{
					if (firstLetter != null)
						return (this.Symbols[0].X <= firstLetter.X) ? this.Symbols[0].Rectangle : firstLetter.Rectangle;
					else
						return this.Symbols[0].Rectangle;
				}
				else
				{
					if (firstLetter != null)
						return firstLetter.Rectangle;
					else
						return null;
				} 
			}
		}
		#endregion

		#region LastSymbolRect
		public Rectangle? LastSymbolRect
		{
			get
			{
				Symbol lastLetter = LastLetter;

				if (this.Symbols.Count > 0)
				{
					if (lastLetter != null)
						return (this.Symbols[this.Symbols.Count - 1].Right >= lastLetter.Right) ? this.Symbols[this.Symbols.Count - 1].Rectangle : lastLetter.Rectangle;
					else
						return this.Symbols[this.Symbols.Count - 1].Rectangle;
				}
				else
				{
					if (lastLetter != null)
						return lastLetter.Rectangle;
					else
						return null;
				}
			}
		}
		#endregion


		#endregion

		//PUBLIC METHODS
		#region public methods

		#region AddWord()
		public void AddWord(Word word)
		{
			this.InsertObjectToList(word);
			word.Line = this;

			if (this.x > word.X)
				this.x = word.X;
			if (this.y > word.Y)
				this.y = word.Y;
			if (this.right < word.Right)
				this.right = word.Right;
			if (this.bottom < word.Bottom)
				this.bottom = word.Bottom;

			this.fontSize = null;
		}
		#endregion

		#region AddSymbol()
		public void AddSymbol(Symbol symbol)
		{
			if (this.Symbols.Contains(symbol) == false)
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
			}

			this.fontSize = null;
		}
		#endregion

		#region RemoveWord()
		public void RemoveWord(Word word)
		{
			if (this.Words.Contains(word))
			{
				word.Line = null;
				this.Words.Remove(word);

				ComputeRectangle();

				this.fontSize = null;
			}
		}
		#endregion

		#region RemoveSymbol()
		public void RemoveSymbol(Symbol symbol)
		{
			if (this.Symbols.Contains(symbol))
			{
				this.Symbols.Remove(symbol);
				ComputeRectangle();
				this.fontSize = null;
			}
		}
		#endregion

		#region CompareTo()
		public int CompareTo(object obj)
		{
			if (this.Y < ((Line)obj).Y)
				return -1;
			if (this.Y == ((Line)obj).Y)
				return 0;

			return 1;
		}
		#endregion

		#region GetBfPoints()
		/// <summary>
		/// Returns line bf points. Maximum is 8 points - for smoothness.
		/// </summary>
		/// <returns></returns>
		public Point[] GetBfPoints()
		{
			BfPoints points = new BfPoints();
			int averageLetterHeight = this.Words.FontSize;

			foreach (Word word in this.Words)
			{
				Point p;

				if (word.GetBfPoint(averageLetterHeight, out p))
					points.Add(new BfPoint(p));
			}

			if (points.Count >= 2)
			{
				//reduce duplicating points
				for (int i = points.Count - 1; i > 0; i--)
					if (points[i].X - points[i - 1].X < 10)
					{
						points[i].Y = (points[i].Y + points[i - 1].Y) / 2;
						points.RemoveAt(i - 1);
					}

				if (points.Count > 1 && (points[1].X - points[0].X) < 10)
				{
					points[0].Y = (points[0].Y + points[1].Y) / 2;
					points.RemoveAt(1);
				}

				//reduce to just 8 points, to make curve smoothers
				while (points.Count > 8)
				{
					int index = 1;

					for (int i = 2; i < points.Count - 2; i++)
						if (points[i + 1].X - points[i].X < points[index + 1].X - points[index].X)
							index = i;

					points[index] = new BfPoint(points[index].X + (points[index + 1].X - points[index].X) / 2, points[index].Y + (points[index + 1].Y - points[index].Y) / 2);
					points.RemoveAt(index + 1);
				}

				if (points.Count >= 2)
				{
					points.Sort();
					return points.GetPoints();
				}
			}

			return null;
		}
		#endregion

		#region GetSeatAt()
		public int GetSeatAt(int x)
		{
			Word closestWord = null;
			int minDistance = int.MaxValue;
			
			foreach (Word word in this.Words)
			{
				if (((word.X <= x) && (word.Right >= x)) && (word.Letters.Count > 1))
					return word.Seat;

				if (word.Right < x)
				{
					if ((x - word.Right) < minDistance)
					{
						closestWord = word;
						minDistance = x - word.Right;
					}
				}
				else if ((word.X - x) < minDistance)
				{
					closestWord = word;
					minDistance = word.X - x;
				}
			}
			
			if(closestWord != null)
				return closestWord.Seat;

			
			Symbol closestSymbol = null;
			minDistance = int.MaxValue;

			foreach (Symbol symbol in this.Symbols)
			{
				if ((symbol.X <= x) && (symbol.Right >= x))
					return symbol.Bottom;

				if (symbol.Right < x)
				{
					if ((x - symbol.Right) < minDistance)
					{
						closestSymbol = symbol;
						minDistance = x - symbol.Right;
					}
				}
				else if ((symbol.X - x) < minDistance)
				{
					closestSymbol = symbol;
					minDistance = symbol.X - x;
				}
			}

			if (closestSymbol != null)
				return closestSymbol.Bottom;

			return this.Bottom;
		}
		#endregion

		#region GetShoulderAt()
		public int GetShoulderAt(int x)
		{
			Word closestWord = null;
			int minDistance = int.MaxValue;

			foreach (Word word in this.Words)
			{
				if (((word.X <= x) && (word.Right >= x)) && (word.Letters.Count > 1))
					return word.Shoulder;

				if (word.Right < x)
				{
					if ((x - word.Right) < minDistance)
					{
						closestWord = word;
						minDistance = x - word.Right;
					}
				}
				else if ((word.X - x) < minDistance)
				{
					closestWord = word;
					minDistance = word.X - x;
				}
			}

			if (closestWord != null)
				return closestWord.Shoulder;


			Symbol closestSymbol = null;
			minDistance = int.MaxValue;

			foreach (Symbol symbol in this.Symbols)
			{
				if ((symbol.X <= x) && (symbol.Right >= x))
					return symbol.Y;

				if (symbol.Right < x)
				{
					if ((x - symbol.Right) < minDistance)
					{
						closestSymbol = symbol;
						minDistance = x - symbol.Right;
					}
				}
				else if ((symbol.X - x) < minDistance)
				{
					closestSymbol = symbol;
					minDistance = symbol.X - x;
				}
			}

			if (closestSymbol != null)
				return closestSymbol.Y;

			return this.Bottom;
		}
		#endregion

		#region Merge()
		public void Merge(Line line)
		{
			foreach (Word word in line.Words)
				if (word.Line != this)
					this.AddWord(word);

			foreach (Symbol symbol in line.Symbols)
				this.AddSymbol(symbol);

			this.fontSize = null;
		}
		#endregion

		#region AreLineCandidates()
		/// <summary>
		/// Order of symbols is important. s1 is the symbol to the left, s2 is in the middle, s3 to the right.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <param name="s3"></param>
		/// <param name="bitmap"></param>
		/// <returns></returns>
		public static bool AreLineCandidates(Symbol s1, Symbol s2, Symbol s3)
		{
			if ((s3 != null) && s3.IsLetter)
			{
				int widest = ((s1.Width > s2.Width) ? ((s1.Width > s3.Width) ? s1.Width : s3.Width) : ((s2.Width > s3.Width) ? s2.Width : s3.Width));
				int shortest = (s2.Height < s1.Height) ? ((s2.Height < s3.Height) ? s2.Height : s3.Height) : ((s1.Height < s3.Height) ? s1.Height : s3.Height);
				int tallest = (s2.Height > s1.Height) ? ((s2.Height > s3.Height) ? s2.Height : s3.Height) : ((s1.Height > s3.Height) ? s1.Height : s3.Height);

				if (shortest * 1.2 > tallest)
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
		}
		#endregion

		#region DrawToImage()
		public void DrawToImage(Color color, BitmapData bmpData)
		{
#if SAVE_RESULTS
			try
			{
				foreach (Word word in this.Words)
					foreach (Symbol symbol in word.Letters)
						symbol.ObjectMap.DrawToImage(color, bmpData);

				foreach (Symbol symbol in this.Symbols)
					symbol.ObjectMap.DrawToImage(color, bmpData);
			}
			catch { }
			finally
			{
				GC.Collect();
			}
#endif
		}

		public void DrawToImage(BitmapData bmpData)
		{
#if SAVE_RESULTS
			try
			{
				int index = 0;

				foreach (Word word in this.Words)
					foreach (Symbol symbol in word.Letters)
						symbol.ObjectMap.DrawToImage(Debug.GetColor(index++), bmpData);

				foreach (Symbol symbol in this.Symbols)
					symbol.ObjectMap.DrawToImage(Debug.GetColor(index++), bmpData);
			}
			catch { }
			finally
			{
				GC.Collect();
			}
#endif
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

		#region InsertObjectToList()
		private void InsertObjectToList(Word word)
		{
			for (int i = 0; i < this.Words.Count; i++)
			{
				if (this.Words[i].X > word.X)
				{
					this.Words.Insert(i, word);
					return;
				}
			}

			this.Words.Add(word);
		}
		#endregion

		#region ComputeRectangle()
		public void ComputeRectangle()
		{
			this.x = int.MaxValue;
			this.y = int.MaxValue;
			this.right = int.MinValue;
			this.bottom = int.MinValue;

			foreach (Word word in this.Words)
			{
				if (this.x > word.X)
					this.x = word.X;
				if (this.y > word.Y)
					this.y = word.Y;
				if (this.right < word.Right)
					this.right = word.Right;
				if (this.bottom < word.Bottom)
					this.bottom = word.Bottom;
			}

			foreach (Symbol symbol in this.Symbols)
			{
				if (this.x > symbol.X)
					this.x = symbol.X;
				if (this.y > symbol.Y)
					this.y = symbol.Y;
				if (this.right < symbol.Right)
					this.right = symbol.Right;
				if (this.bottom < symbol.Bottom)
					this.bottom = symbol.Bottom;
			}

		}
		#endregion

		#endregion

	}

}
