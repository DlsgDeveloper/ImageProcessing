using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace ImageProcessing.PageObjects
{
	public class Symbols : List<Symbol>
	{
		SortType sortType = SortType.None;


		#region constructor
		public Symbols()
			: base()
		{
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties

		#region FontSize
		public int FontSize
		{
			get
			{
				int[] heights = new int[128];

				foreach (Symbol obj in this)
					if (obj.Height < 128)
						heights[obj.Height]++;

				int maxIndex = 8;
				for (int i = 9; i < 125; i++)
					if (heights[maxIndex - 1] + heights[maxIndex] + heights[maxIndex + 1] < heights[i - 1] + heights[i] + heights[i+1])
						maxIndex = i;

				return maxIndex;
			}
		}
		#endregion

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region Shift()
		public void Shift(int x, int y)
		{
			foreach(Symbol obj in this)
				obj.Shift(x, y);
		}
		#endregion

		#region GetClip()
		public Rectangle GetClip()
		{				
			int		x = int.MaxValue, y = int.MaxValue, r = int.MinValue, b = int.MinValue;
				
			foreach(Symbol obj in this)
			{				
				if(x > obj.X)
					x = obj.X;
				if(y > obj.Y)
					y = obj.Y;
				if(r < obj.Right)
					r = obj.Right;
				if(b < obj.Bottom)
					b = obj.Bottom;
			}

			if(x == int.MaxValue || y == int.MaxValue || r == int.MinValue || b == int.MinValue)
				return Rectangle.Empty;
			else
				return Rectangle.FromLTRB(x,y,r,b);
		}

		public Rectangle GetClip(Rectangle clip)
		{				
			if(clip.IsEmpty)
				return GetClip();
				
			int		x = int.MaxValue, y = int.MaxValue, r = int.MinValue, b = int.MinValue;
				
			foreach(Symbol obj in this)
			{				
				if(Rectangle.Intersect(obj.Rectangle, clip) != Rectangle.Empty)
				{
					if(x > obj.X)
						x = obj.X;
					if(y > obj.Y)
						y = obj.Y;
					if(r < obj.Right)
						r = obj.Right;
					if(b < obj.Bottom)
						b = obj.Bottom;
				}
			}

			if(x == int.MaxValue || y == int.MaxValue || r == int.MinValue || b == int.MinValue)
				return Rectangle.Empty;
			else
				return Rectangle.FromLTRB(x,y,r,b);
		}
		#endregion

		#region GetSymbolsInClip()
		public Symbols GetSymbolsInClip(Rectangle clip, bool onlySymbolsEntireInClip)
		{
			Symbols symbols = new Symbols();

			if (onlySymbolsEntireInClip)
			{
				foreach (Symbol symbol in this)
					if (symbol.X >= clip.X && symbol.Right <= clip.Right && symbol.Y >= clip.Y && symbol.Bottom <= clip.Bottom)
						symbols.Add(symbol);
			}
			else
			{
				foreach (Symbol symbol in this)
					if (Rectangle.Union(clip, symbol.Rectangle) != Rectangle.Empty)
						symbols.Add(symbol);
			}

			return symbols;
		}
		#endregion

		#region GetObjectsInClip()
		public Symbols GetObjectsInClip(Rectangle clip)
		{				
			Symbols	pageSymbols = new Symbols();

			foreach (Symbol symbol in this)
			{
				Rectangle intersection = Rectangle.Intersect(clip, symbol.Rectangle);

				if (intersection.Width * intersection.Height > (symbol.Width * symbol.Height / 2))
					pageSymbols.Add(symbol);
			}

			return pageSymbols;
		}
		#endregion

		#region RemoveLines()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="objects"></param>
		/// <param name="ratio"></param>
		/// <param name="flags">1 horizontal, 2 vertical 3 both</param>
		/// <returns></returns>
		public void RemoveLines(float ratio, int flags)
		{
			try
			{				
				if((flags & 1) == 1 && (flags & 2) == 0)
				{
					for(int i = this.Count - 1; i >= 0; i--)
						if((this[i].Width / (float) this[i].Height) > ratio)
							this.RemoveAt(i);
				}
				else if((flags & 1) == 0 && (flags & 2) == 2)
				{
					for(int i = this.Count - 1; i >= 0; i--)
						if((this[i].Height / (float) this[i].Width) > ratio)
							this.RemoveAt(i);
				}
				else if((flags & 3) == 3)
				{
					for(int i = this.Count - 1; i >= 0; i--)
						if( ((this[i].Width / (float) this[i].Height) > ratio) || ((this[i].Height / (float) this[i].Width) > ratio))
							this.RemoveAt(i);
				}
			}
			finally
			{
			}
		}
		#endregion

		#region GetTextHeight()
		public int GetTextHeight()
		{
			int[]	heights = new int[100];
		
			foreach (Symbol obj in this)
				if(obj.Height < 100)
					heights[obj.Height] ++ ;

			int minHeight = 4;
			for(int i = 5; i < 100; i++)
				if(heights[minHeight] < heights[i])
					minHeight = i;

			return minHeight;
		}
		#endregion

		#region MergeObjectsNestedInPictures()
		/*public void MergeObjectsNestedInPictures()
		{
			bool repeat;

			do
			{
				repeat = false;
				for (int i = this.Count - 1; i >= 0; i--)
				{
					if (this[i].IsPicture)
					{
						Symbol picture = this[i];

						for (int j = this.Count - 1; j >= 0 && j != i; j--)
						{
							Rectangle rect = Rectangle.Intersect(picture.Rectangle, this[j].Rectangle);

							if ((rect.Width * rect.Height > (picture.Width * picture.Height) / 10) || (rect.Width * rect.Height > (this[j].Width * this[j].Height) / 10))
							{
								if (i < j)
								{
									this[i].Merge(this[j]);
									this.RemoveAt(j);
								}
								else
								{
									this[j].Merge(picture);
									this.RemoveAt(i);
									this[j].ObjectType = Symbol.Type.Picture;
								
									if (this[j].IsPicture)
										repeat = true;
								}
							}
						}
					}
				}
			} while (repeat);
		}*/
		#endregion

		#region MergeNesterdObjects()
		public void MergeNesterdObjects()
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			SortHorizontally();

			for (int i = this.Count - 2; i >= 0; i--)
			{
				for (int j = i + 1; j < this.Count; j++)
				{
					if (this[j].X > this[i].Right)
						break;

					Rectangle intersect = Rectangle.Union(this[i].Rectangle, this[j].Rectangle);

					if (intersect == this[i].Rectangle || intersect == this[j].Rectangle)
					{
						this[i].Merge(this[j]);
						this.RemoveAt(j);
						j--;
					}
				}
			}

#if DEBUG
			Console.WriteLine(string.Format("Symbols, MergeNesterdObjects(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), this.Count));
#endif
		}
		#endregion

		#region Despeckle()
		public void Despeckle(int minWidth, int minHeight)
		{
			for (int i = this.Count - 1; i >= 0; i--)
				if (this[i].Width < minWidth || this[i].Height < minHeight)
					RemoveAt(i);
		}
		#endregion

		#region GetSymbolsMask()
		public bool[,] GetSymbolsMask(Size arraySize)
		{
			bool[,] mask = new bool[arraySize.Width, arraySize.Height];
			int x,y;

			foreach (Symbol symbol in this)
			{
				for(y = symbol.Y; y < symbol.Bottom; y++)
					for(x = symbol.X; x < symbol.Right; x++)
						mask[x,y] = true;
			}

			return mask;
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
				this.Sort(new Symbol.HorizontalComparer());
				sortType = SortType.Horizontal;
			}
		}
		#endregion

		#region SortVertically()
		public void SortVertically()
		{
			if (sortType != SortType.Vertical)
			{
				this.Sort(new Symbol.VerticalComparer());
				sortType = SortType.Vertical;
			}
		}
		#endregion

		#region GetSymbolToTheLeft()
		/// <summary>
		/// Maximum distance between characters is symbol height * 2
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="symbolsIndex">Index of the symbol in symbols horizontally sorted list</param>
		/// <param name="weight">Distance between closest symbols</param>
		/// <returns></returns>
		public Symbol GetSymbolToTheLeft(int symbolIndex, out int weight)
		{
			Symbol symbol = this[symbolIndex];
			Symbol closestObject = null;
			int currentWeight;
			int symbolHeight = symbol.Height;

			weight = int.MaxValue;

			if (this.sortType != SortType.Horizontal)
				SortHorizontally();

			for (int i = symbolIndex - 1; i >= 0; i--)
			{
				Symbol aSymbol = this[i];

				if (symbol.X - aSymbol.Right > symbolHeight * 2)
					return closestObject;
				
				if ((symbol.Y >= aSymbol.Y && symbol.Y <= aSymbol.Bottom) || (symbol.Y <= aSymbol.Y && symbol.Bottom >= aSymbol.Y))
				{
					currentWeight = (aSymbol.Right > symbol.X) ? (aSymbol.Right - symbol.X) : (symbol.X - aSymbol.Right);

					if (currentWeight > weight)
						return closestObject;

					if ((symbol.X - aSymbol.Right < symbol.Height && symbol.X - aSymbol.Right < aSymbol.Height))
					{
						if ((aSymbol.X < symbol.X) && (aSymbol.Right >= symbol.X))
						{
							weight = 0;
							return aSymbol;
						}

						if (weight > currentWeight)
						{
							weight = currentWeight;
							closestObject = aSymbol;
						}
						else if (weight == currentWeight)
						{
							int sharedV1 = ((aSymbol.Bottom < symbol.Bottom) ? aSymbol.Bottom : symbol.Bottom) - ((aSymbol.Y > symbol.Y) ? aSymbol.Y : symbol.Y);
							int sharedV2 = ((closestObject.Bottom < symbol.Bottom) ? closestObject.Bottom : symbol.Bottom) - ((closestObject.Y > symbol.Y) ? closestObject.Y : symbol.Y);

							if (sharedV1 > sharedV2)
								closestObject = aSymbol;
						}
					}
				}
			}

			return closestObject;
		}
		#endregion

		#region GetSymbolToTheRight()
		/// <summary>
		/// Maximum distance between characters is symbol height * 2
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="symbolsIndex">Index of the symbol in symbols horizontally sorted list</param>
		/// <param name="weight">Distance between closest symbols</param>
		/// <returns></returns>
		public Symbol GetSymbolToTheRight(int symbolIndex, out int weight)
		{
			Symbol symbol = this[symbolIndex];
			Symbol closestObject = null;
			int symbolHeight = symbol.Height;

			weight = int.MaxValue;

			if (this.sortType != SortType.Horizontal)
				SortHorizontally();

			for (int i = symbolIndex + 1; i < this.Count; i++)
			{
				if (this[i].Right > symbol.Right)
				{
					Symbol	aSymbol = this[i];

					if (aSymbol.X - symbol.Right > symbolHeight * 2)
						return closestObject;

					int		currentWeight = ((aSymbol.X - symbol.Right) > 0) ? (aSymbol.X - symbol.Right) : -(aSymbol.X - symbol.Right);

					if (currentWeight > weight)
						return closestObject;

					if ((aSymbol.X - symbol.Right < aSymbol.Height && aSymbol.X - symbol.Right < symbol.Height) && Arithmetic.AreInLine(symbol.Y, symbol.Bottom, aSymbol.Y, aSymbol.Bottom))
					{
						if ((symbol.X < aSymbol.X) && (symbol.Right >= aSymbol.X))
						{
							weight = 0;
							return aSymbol;
						}

						if (weight > currentWeight)
						{
							weight = currentWeight;
							closestObject = aSymbol;
						}
						else if (weight == currentWeight)
						{
							int sharedV1 = ((aSymbol.Bottom < symbol.Bottom) ? aSymbol.Bottom : symbol.Bottom) - ((aSymbol.Y > symbol.Y) ? aSymbol.Y : symbol.Y);
							int sharedV2 = ((closestObject.Bottom < symbol.Bottom) ? closestObject.Bottom : symbol.Bottom) - ((closestObject.Y > symbol.Y) ? closestObject.Y : symbol.Y);

							if (sharedV1 > sharedV2)
								closestObject = aSymbol;
						}
					}
				}
			}
			return closestObject;
		}
		#endregion

		#region DrawToFile()
		public void DrawToFile(string filePath, Size imageSize)
		{
			Bitmap result = null;
			BitmapData bmpData = null;

			try
			{
				result = Debug.GetBitmap(imageSize);
				
				Graphics g = Graphics.FromImage(result);
				SolidBrush brush = new SolidBrush(Color.FromArgb(100, 120, 120, 120));
				Color color;

				foreach (Symbol symbol in this)
					g.FillRectangle(brush, symbol.Rectangle);

				bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				foreach (Symbol symbol in this)
				{
					switch (symbol.ObjectType)
					{
						case Symbol.Type.Unknown: color = Color.White; break;
						case Symbol.Type.Letter: color = Color.Yellow; break;
						case Symbol.Type.Line: color = Color.Gray; break;
						case Symbol.Type.Picture: color = Color.Green; break;
						case Symbol.Type.Frame: color = Color.Pink; break;
						case Symbol.Type.AnyPunctuation: color = Color.Blue; break;
						default: color = Color.Red; break;
					}

					symbol.DrawToImage(color, bmpData);
				}
			}
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
		}
		#endregion

		#region DrawToImage()
		public void DrawToImage(Bitmap bitmap)
		{
			BitmapData bmpData = null;

			try
			{
				Graphics	g = Graphics.FromImage(bitmap);
				Color		color = Color.Yellow;
				SolidBrush	brush = new SolidBrush(Color.FromArgb(100, color));

				foreach (Symbol symbol in this)
					g.FillRectangle(brush, symbol.Rectangle);

				bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
				foreach (Symbol symbol in this)
				{
					symbol.DrawToImage(color, bmpData);
				}
			}
			finally
			{
				if (bmpData != null)
					bitmap.UnlockBits(bmpData);

				GC.Collect();
			}
		}
		#endregion

		#endregion

	}
}
