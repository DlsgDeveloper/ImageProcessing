using System;
using System.Collections.Generic;
using System.Text;

namespace ImageProcessing.PageObjects
{
	class MiddleLine
	{
		int inch;
		Symbols middleLineSymbols = new Symbols();

		public MiddleLine(ItPage page)
		{
			inch = page.ItImage.ImageInfo.DpiH;
			
			foreach (Symbol symbol in page.LoneObjects)
			{
				if (symbol.X > page.ImageRect.Width / 3 && symbol.Right < page.ImageRect.Width * 2 / 3)
				{
				}
			}
		}

		class MiddleLineCandidates : List<Symbols>
		{
			public MiddleLineCandidates()
			{
			}

			public void AddSymbol(Symbol symbol)
			{
				foreach (Symbols symbols in this)
				{
					foreach (Symbol symbolsSymbol in symbols)
					{
						if (GetHorizontalDistance(symbol, symbolsSymbol) == 0 && GetVerticalDistance(symbol, symbolsSymbol) < 10)
						{
							symbols.Add(symbol);
							return;
						}
					}
				}

				Symbols newSymbols = new Symbols();
				newSymbols.Add(symbol);
				this.Add(newSymbols);
			}

			public void Merge()
			{
				int count;

				do
				{
					count = this.Count;
					for (int i = this.Count - 1; i >= 0; i--)
					{
						Symbols symbols1 = this[i];

						for (int j = 0; j < i; j++)
						{
							Symbols symbols2 = this[j];

							if (AreInLine(symbols1, symbols2))
							{
								symbols2.AddRange(symbols1);
								this.RemoveAt(i);
								break;
							}
						}
					}
				}
				while (this.Count != count);
			}

			private int GetHorizontalDistance(Symbol s1, Symbol s2)
			{
				if (s1.Right < s2.X)
					return s2.X - s1.Right;
				else if (s1.X > s2.Right)
					return s1.X - s2.Right;
				else
					return 0;
			}

			private int GetVerticalDistance(Symbol s1, Symbol s2)
			{
				if (s1.Bottom < s2.Y)
					return s2.Y - s1.Bottom;
				else if (s1.Y > s2.Bottom)
					return s1.Y - s2.Bottom;
				else
					return 0;
			}

			private bool AreInLine(Symbol s1, Symbol s2)
			{
				return (GetHorizontalDistance(s1, s2) == 0 && GetVerticalDistance(s1, s2) < 50);
			}

			private bool AreInLine(Symbols symbols1, Symbols symbols2)
			{
				foreach (Symbol s1 in symbols1)
					foreach (Symbol s2 in symbols2)
						if (AreInLine(s1, s2))
							return true;

				return false;
			}

		}


	}
}
