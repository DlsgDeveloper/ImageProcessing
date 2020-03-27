using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace ImageProcessing.PageObjects
{
	public class Paragraphs : List<Paragraph>
	{
		SortType sortType = SortType.None;

		#region constructor
		public Paragraphs()
			: base()
		{
		}

		public Paragraphs(Symbols loneSymbols, Words words, Lines pageLines, Size imageSize)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			foreach (Word w in words)
				if (w.Paragraph != null)
					w.Paragraph = null;
			
			Lines lines = pageLines;
			
			//add lines
			for (int i = 0; i < lines.Count; i++)
			{
				Paragraph	paragraph = null;
				Line		line = lines[i];				

				foreach (Word word in line.Words)
					if (word.Paragraph != null)
					{
						paragraph = word.Paragraph;
						break;
					}

				if (paragraph == null)
				{
					paragraph = new Paragraph(line.Words[0]);
					Add(paragraph);
				}

				//DrawToFile(Debug.SaveToDir + "Paragraphs.png", imageSize);
				AddWords(paragraph, line.Words, line.Symbols);
				//DrawToFile(Debug.SaveToDir + "Paragraphs.png", imageSize);

				Lines adjacents = lines.GetVerticalAdjacents(line);

				foreach (Line adjacent in adjacents)
				{
					//same font size
					if (IsTheFontSizeTheSame(line, adjacent))
					{
						if (IsLeftSideOk(line, adjacent))
						{
							if (IsRightSideOk(line, adjacent))
							{
								int linesVerticalDistance;

								if (Lines.GetLineSpacing(line, adjacent, out linesVerticalDistance))
								{
									if (linesVerticalDistance < line.FontSize * 4.0)
									{
										AddWords(paragraph, adjacent.Words, line.Symbols);
									}
								}
							}
						}
					}
				}
			}

			//DrawToFile(Debug.SaveToDir + "Paragraphs.png", imageSize);
			MergeNestedParagraphs();
			//DrawToFile(Debug.SaveToDir + "Paragraphs.png", imageSize);
		
			//add words
			foreach (Word word in words)
			{
				if (word.Paragraph == null)
				{
					Paragraph paragraph = FindParagraph(word);

					if (paragraph != null)
						paragraph.AddWord(word);
					else
						this.Add(new Paragraph(word));
				}
			}

			//DrawToFile(Debug.SaveToDir + "Paragraphs.png", imageSize);
			MergeNestedParagraphs();
			//DrawToFile(Debug.SaveToDir + "Paragraphs.png", imageSize);
			
			//add symbols
			for (int i = loneSymbols.Count - 1; i >= 0; i--)
			{
				Symbol symbol = loneSymbols[i];
				Paragraph paragraph = FindParagraph(symbol);

				if (paragraph != null)
				{
					paragraph.AddSymbol(symbol);
					loneSymbols.RemoveAt(i);
				}
			}

			//DrawToFile(Debug.SaveToDir + "Paragraphs.png", imageSize);
			MergeNestedParagraphs();
			//DrawToFile(Debug.SaveToDir + "Paragraphs.png", imageSize);

#if DEBUG
			Console.WriteLine(string.Format("Paragraphs, constructor(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), this.Count));
#endif
		}
		#endregion


		//PUBLIC METHODS
		#region public methods

		#region MergeNestedParagraphs()
		public void MergeNestedParagraphs()
		{
			int count;

			do
			{
				count = this.Count;

				for (int i = this.Count - 1; i > 0; i--)
				{
					Paragraph paragraph1 = this[i];
					Rectangle rect1 = paragraph1.Rectangle;

					for (int j = 0; j < i; j++)
					{
						Paragraph paragraph2 = this[j];

						Rectangle rect2 = paragraph2.Rectangle;
						Rectangle intersect = Rectangle.Intersect(rect1, rect2);

						if (intersect.Width > 0 && intersect.Height > 0)
						{
							if ((2 * intersect.Width * intersect.Height > rect1.Width * rect1.Height) ||
								(2 * intersect.Width * intersect.Height > rect2.Width * rect2.Height))
							{
								paragraph2.Merge(paragraph1);
								this.RemoveAt(i);
								break;
							}

						}
					}
				}
			} while (this.Count != count);
		}
		#endregion

		#region MergeVertAdjacentParagraphs()
		/*public void MergeVertAdjacentParagraphs()
		{
			int count;

			do
			{
				count = this.Count;

				for (int i = 0; i < this.Count - 1; i++)
				{
					for (int j = i + 1; j < this.Count; j++)
					{
						Paragraph paragraph1 = this[i];
						Paragraph paragraph2 = this[j];

						if (AreVectorsAdjacent(paragraph1.X, paragraph1.Right, paragraph2.X, paragraph2.Right))
						{
							paragraph1.Merge(paragraph2);
							this.RemoveAt(j);
							j--;
						}
					}
				}
			} while (count != this.Count);
		}*/
		#endregion

		#region MergeCloseParagraphs()
		public void MergeCloseParagraphs(int maxDistanceInPixels)
		{
			int count;

			do
			{
				count = this.Count;

				for (int i = this.Count - 1; i > 0; i--)
				{
					for (int j = 0; j < i; j++)
					{
						if (Arithmetic.Distance(this[i].Rectangle, this[j].Rectangle) <= maxDistanceInPixels)
						{
							this[j].Merge(this[i]);
							this.RemoveAt(i);
							goto Mark;
						}
					}

				Mark: ;
				}
			} while (this.Count != count);
		}
		#endregion

		#region MergeShortParagraphs()
		/*public void MergeShortParagraphs()
		{
			int count;

			do
			{
				double averageColumnWidth = AverageColumnWidth();
				count = this.Count;

				for (int i = this.Count - 1; i > 0; i--)
				{
					Paragraph p1 = this[i];
					
					for (int j = 0; j < i; j++)
					{
						Paragraph p2 = this[j];

						if (Arithmetic.AreInLine(p1.Rectangle, p2.Rectangle))
						{
							if (p1.Width + p2.Width < averageColumnWidth)
							{
								//same zone	
								if (p1.Words.Count > 0 && p2.Words.Count > 0 && p1.Words[0].Zone == p2.Words[0].Zone)
								{
									//adjacent paragraph exist covering the horizontal gap 
									int gapL = (p1.Right < p2.Right) ? p1.Right : p2.Right;
									int gapR = (p1.X > p2.X) ? p1.X : p2.X;

									for (int k = 0; k < this.Count - 1; k++)
									{
										Paragraph p3 = this[k];

										if (p3.X <= gapL && p3.Right >= gapR && p3.Words.Count > 0 && p3.Words[0].Zone == p1.Words[0].Zone)
										{
											this[j].Merge(this[i]);
											this.RemoveAt(i);
											goto Mark;
										}
									}
								}
							}
						}
					}

				Mark: ;
				}
			} while (this.Count != count);
		}*/
		#endregion

		#region AverageColumnWidth()
		public int? GetColumnsWidth()
		{
			List<Paragraph> columnParagraphs = new List<Paragraph>();
			int square = 0;
			int sum = 0;

			foreach (Paragraph paragraph in this)
				if (paragraph.IsLineParagraph() == false)
				{
					columnParagraphs.Add(paragraph);
					square += paragraph.Width * paragraph.Height;
					sum += paragraph.Height;
				}

			if (columnParagraphs.Count > 0 && sum > 0)
			{
				double averageWidth = square / sum;

				for (int i = columnParagraphs.Count - 1; i >= 0; i--)
					if (columnParagraphs[i].Width < averageWidth * 0.8 || columnParagraphs[i].Width > averageWidth * 1.2)
						columnParagraphs.RemoveAt(i);

				if (columnParagraphs.Count > 0)
				{
					square = 0;
					sum = 0;

					foreach (Paragraph paragraph in columnParagraphs)
					{
						square += paragraph.Width * paragraph.Height;
						sum += paragraph.Height;
					}

					return square / sum;
				}
			}

			return null;
		}
	
		/*public double AverageColumnWidth()
		{
			double area = 0;
			double height = 0;

			foreach (Paragraph paragraph in this)
			{
				area += paragraph.Width * paragraph.Height;
				height += paragraph.Height;
			}

			return area / height;
		}*/
		#endregion

		#region InsertNestedWords()
		public void InsertNestedWords(Words words)
		{
			foreach(Word word in words)
			{
				if(word.Paragraph == null)
				{
					foreach(Paragraph paragraph in this)
					{
						if(Rectangle.Intersect(word.Rectangle, paragraph.Rectangle) == word.Rectangle)
						{
							paragraph.AddWord(word);
							break;
						}
					}
				}
			}
		}
		#endregion

		#region Add()
		new public void Add(Paragraph paragraph)
		{
			base.Add(paragraph);
			sortType = SortType.None;
		}
		#endregion

		#region RemoveAt()
		new public void RemoveAt(int i)
		{
			foreach(Word word in this[i].Words)
				if(word.Paragraph == this[i])
					word.Paragraph = null;

			base.RemoveAt(i);
		}
		#endregion

		#region Remove()
		new public void Remove(Paragraph paragraph)
		{
			foreach(Word word in paragraph.Words)
				if(word.Paragraph == paragraph)
					word.Paragraph = null;

			base.Remove(paragraph);
		}
		#endregion

		#region GetClip()
		public Rectangle GetClip()
		{
			int x = int.MaxValue, y = int.MaxValue, r = int.MinValue, b = int.MinValue;

			foreach (Paragraph paragraph in this)
			{
				if (x > paragraph.X)
					x = paragraph.X;
				if (y > paragraph.Y)
					y = paragraph.Y;
				if (r < paragraph.Right)
					r = paragraph.Right;
				if (b < paragraph.Bottom)
					b = paragraph.Bottom;
			}

			if (x == int.MaxValue || y == int.MaxValue || r == int.MinValue || b == int.MinValue)
				return Rectangle.Empty;
			else
				return Rectangle.FromLTRB(x, y, r, b);
		}
		#endregion

		#region GetParagraphsInClip()
		public Paragraphs GetParagraphsInClip(Rectangle clip)
		{
			Paragraphs paragraphs = new Paragraphs();

			foreach (Paragraph paragraph in this)
			{
				Rectangle intersection = Rectangle.Intersect(paragraph.Rectangle, clip);

				if ((intersection.Width * intersection.Height) > ((paragraph.Width * paragraph.Height) / 2))
					paragraphs.Add(paragraph);
			}

			return paragraphs;
		}
		#endregion

		#region AreParagraphsAdjacent()
		/*public bool AreParagraphsAdjacent(Paragraph p1, Paragraph p2, int linesSpacing)
		{
			int horizontalPixelShare = Arithmetic.HorizontalPixelsShare(p1.Rectangle, p2.Rectangle);

			if (horizontalPixelShare > p1.Width * 0.8 || horizontalPixelShare > p2.Width * 0.8)
			{
				int y1 = Math.Max(p1.Y, p2.Y);
				int y2 = Math.Min(p1.Bottom, p2.Bottom);

				if (y2 - y1 < linesSpacing * 1.5)
				{
				}
			}
		}*/
		#endregion

		#region SortVertically()
		public void SortHorizontally()
		{
			if (sortType != SortType.Horizontal)
			{
				this.Sort(new Paragraph.HorizontalComparer());
				sortType = SortType.Horizontal;
			}
		}
		#endregion

		#region SortVertically()
		public void SortVertically()
		{
			if (sortType != SortType.Vertical)
			{
				this.Sort(new Paragraph.VerticalComparer());
				sortType = SortType.Vertical;
			}
		}
		#endregion

		#region ResetLines()
		/// <summary>
		/// use to reset paragraphs lines information (when words lines references changed)
		/// </summary>
		public void ResetLines()
		{
			foreach (Paragraph paragraph in this)
				paragraph.ResetLines();
		}
		#endregion

		#region GetLinesSpacing()
		public static int? GetLinesSpacing(Paragraph paragraph1, Paragraph paragraph2)
		{
			Lines lines1 = paragraph1.Lines;
			Lines lines2 = paragraph2.Lines;

			if (lines1.Count > 0 && lines2.Count > 0)
			{
				Line l1 = null;
				Line l2 = null;
				
				lines1.SortVertically();
				lines2.SortVertically();

				if (paragraph1.Y + paragraph1.Height / 2 <= paragraph2.Y + paragraph2.Height / 2)
				{
					l1 = lines1[lines1.Count - 1];
					l2 = lines2[0];
				}
				else
				{
					l1 = lines1[0];
					l2 = lines2[lines2.Count - 1];
				}

				if (Lines.IsAbove(l1, l2))
				{
					int spacing = 0;

					if (Lines.GetLineSpacing(l1, l2, out spacing))
						return spacing;
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

			try
			{
				int counter = 0;
				result = Debug.GetBitmap(imageSize);
				Graphics g = Graphics.FromImage(result);

				foreach (Paragraph paragraph in this)
				{
					Color color = Debug.GetColor(counter++);
					color = Color.FromArgb(100, color.R, color.G, color.B);
					g.FillRectangle(new SolidBrush(color), paragraph.Rectangle);
					paragraph.DrawToImage(result, color);
				}
			}
			catch { }
			finally
			{
				if (result != null)
				{
					result.Save(filePath, ImageFormat.Png);
					result.Dispose();
				}

				GC.Collect();
			}
#endif
		}
		#endregion

		#region DrawToImage()
		public void DrawToImage(Bitmap result)
		{
#if SAVE_RESULTS
			try
			{
				Graphics g = Graphics.FromImage(result);
				Color color = Color.Blue;
				Brush brush = new SolidBrush(Color.FromArgb(100, color));

				foreach (Paragraph paragraph in this)
				{
					g.FillRectangle(brush, paragraph.Rectangle);
					paragraph.DrawToImage(result, color);
				}
			}
			catch { }
			finally
			{
				GC.Collect();
			}
#endif
		}
		#endregion

		#endregion


		//PRIVATE METHODS	
		#region private methods

		#region AddWords()
		private void AddWords(Paragraph paragraph, Words words, Symbols symbols)
		{
			foreach (Word word in words)
			{
				if (word.Paragraph != paragraph)
				{
					if (word.Paragraph == null)
						paragraph.AddWord(word);
					else
					{
						Paragraph pToRemove = word.Paragraph;
						paragraph.Merge(word.Paragraph);
						this.Remove(pToRemove);
					}
				}
			}

			foreach(Symbol symbol in symbols)
				paragraph.AddSymbol(symbol);
		}
		#endregion

		#region IsTheFontSizeTheSame()
		private static bool IsTheFontSizeTheSame(Line line1, Line line2)
		{
			return IsTheFontSizeTheSame(line1.FontSize, line2.FontSize);
		}

		private static bool IsTheFontSizeTheSame(int fontSize1, int fontSize2)
		{
			return ((fontSize1 > fontSize2 * 0.9) && (fontSize1 < fontSize2 * 1.1));
		}
		#endregion

		#region IsLeftSideOk()
		/// <summary>
		/// Returns true if top line left side is where the bottom line left is or top line can be intended by maximum of 10 font sizes.
		/// </summary>
		/// <param name="topLine"></param>
		/// <param name="bottomLine"></param>
		/// <returns></returns>
		private static bool IsLeftSideOk(Line line1, Line line2)
		{
			bool	firstLineIsAboveSecond = Lines.IsAbove(line1, line2);
			double	xTop = (firstLineIsAboveSecond) ? line1.X : line2.X;
			double	xBottom = (firstLineIsAboveSecond) ? line2.X : line1.X;
			int		fontSize = line1.FontSize;

			return ((xTop > xBottom - fontSize) && (xTop < xBottom + 10 * fontSize));
		}
		#endregion

		#region IsRightSideOk()
		/// <summary>
		/// Returns true if top line right side is same or bigger than the bottom one.
		/// </summary>
		/// <param name="topLine"></param>
		/// <param name="bottomLine"></param>
		/// <returns></returns>
		private static bool IsRightSideOk(Line line1, Line line2)
		{
			bool	firstLineIsAboveSecond = Lines.IsAbove(line1, line2);
			double	xTop = (firstLineIsAboveSecond) ? line1.X : line2.X;
			double	xBottom = (firstLineIsAboveSecond) ? line2.X : line1.X;
			int		fontSize = line1.FontSize;

			return (xTop > xBottom - 2 * fontSize);
		}
		#endregion

		#region FindParagraph()
		private Paragraph FindParagraph(Word word)
		{
			Paragraph bestCandidate = null;
			int bestDistance = int.MaxValue;
			
			foreach (Paragraph paragraph in this)
			{
				IPageObject adjacentL = paragraph.GetObjectToTheLeft(word);
				IPageObject adjacentR = paragraph.GetObjectToTheRight(word);
				IPageObject adjacentT = paragraph.GetObjectAbove(word);
				IPageObject adjacentB = paragraph.GetObjectsBelow(word);
				int validAdjacents = 0;

				if (adjacentL != null)
					validAdjacents++;
				if (adjacentR != null)
					validAdjacents++;
				if (adjacentT != null)
					validAdjacents++;
				if (adjacentB != null)
					validAdjacents++;

				if (validAdjacents >= 2)
					return paragraph;
				else if (validAdjacents == 1 && adjacentT != null)
				{
					if (word.X <= paragraph.X + paragraph.FontSize)
					{
						int distance = word.Y - paragraph.Bottom;

						if (bestDistance > distance)
						{
							bestDistance = distance;
							bestCandidate = paragraph;
						}
					}
				}
			}

			return bestCandidate;
		}
		#endregion

		#region FindParagraph()
		private Paragraph FindParagraph(Symbol symbol)
		{
			Paragraph bestCandidate = null;

			foreach (Paragraph paragraph in this)
			{
				IPageObject adjacentL = paragraph.GetObjectToTheLeft(symbol);
				IPageObject adjacentR = paragraph.GetObjectToTheRight(symbol);
				IPageObject adjacentT = paragraph.GetObjectAbove(symbol);
				IPageObject adjacentB = paragraph.GetObjectsBelow(symbol);
				int validAdjacents = 0;

				if (adjacentL != null)
					validAdjacents++;
				if (adjacentR != null)
					validAdjacents++;
				if (adjacentT != null)
					validAdjacents++;
				if (adjacentB != null)
					validAdjacents++;

				if (validAdjacents >= 2)
					return paragraph;
			}

			return bestCandidate;
		}
		#endregion

		#endregion
	}
}
