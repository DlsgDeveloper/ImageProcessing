using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace ImageProcessing.PageObjects
{
	public class Delimiters : List<Delimiter>
	{
		#region constructor()
		public Delimiters()
		{
		}

		public Delimiters(Symbols symbols, Size imageSize)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			for (int i = symbols.Count - 1; i >= 0; i--)
			{
				if (symbols[i].IsLine)
				{
					base.Add(new Delimiter(symbols[i]));
					symbols.RemoveAt(i);
				}
			}

			Validate(symbols, 5 * Math.PI / 180.0);
			Merge();
			MergeWithSymbols(symbols);
			//DrawToFile(Debug.SaveToDir + "Delimiters.png", imageSize);

#if DEBUG
			Console.WriteLine(string.Format("Delimiters, constructor(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), this.Count));
#endif
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties
		//public Delimiter this[int index] { get { return (Delimiter)base[index]; } }
		#endregion


		//PUBLIC METHODS
		#region public methods

		#region GetDelimitersInClip()
		public Delimiters GetDelimitersInClip(Rectangle clip)
		{
			Delimiters delimiters = new Delimiters();
			
			foreach (Delimiter delimiter in this)
			{
				Rectangle intersection = Rectangle.Intersect(clip, delimiter.Rectangle);
				
				if ((intersection.Width * intersection.Height) > ((delimiter.Rectangle.Width * delimiter.Rectangle.Height) / 2))
					delimiters.Add(delimiter);
			}

			return delimiters;
		}
		#endregion

		#region Distance()
		public static double Distance(Delimiter d1, Delimiter d2)
		{
			double distance;
			Line2D lineD1 = new Line2D((PointF)d1.P1, (PointF)d1.P2);
			Line2D lineD2 = new Line2D((PointF)d2.P1, (PointF)d2.P2);
			double x = 0.0;
			double y = 0.0;

			if (lineD1.InterceptPoint(lineD2, ref x, ref y) && (Contains(d1.Rectangle, x, y) && Contains(d2.Rectangle, x, y)))
				return 0.0;

			double shortestDistance = double.MaxValue;
			if (Line2D.GetPerpendicularLine((PointF)d1.P1, d2.Angle).InterceptPoint(lineD2, ref x, ref y) && Contains(d2.Rectangle, x, y))
			{
				distance = Arithmetic.Distance(d1.P1, x, y);
				
				if (shortestDistance > distance)
					shortestDistance = distance;
			}
			
			if (Line2D.GetPerpendicularLine((PointF)d1.P2, d2.Angle).InterceptPoint(lineD2, ref x, ref y) && Contains(d2.Rectangle, x, y))
			{
				distance = Arithmetic.Distance(d1.P2, x, y);

				if (shortestDistance > distance)
					shortestDistance = distance;
			}
			
			if (Line2D.GetPerpendicularLine((PointF)d2.P1, d1.Angle).InterceptPoint(lineD1, ref x, ref y) && Contains(d1.Rectangle, x, y))
			{
				distance = Arithmetic.Distance(d2.P1, x, y);

				if (shortestDistance > distance)
					shortestDistance = distance;
			}
			
			if (Line2D.GetPerpendicularLine((PointF)d2.P2, d1.Angle).InterceptPoint(lineD1, ref x, ref y) && Contains(d1.Rectangle, x, y))
			{
				distance = Arithmetic.Distance(d2.P2, x, y);

				if (shortestDistance > distance)
					shortestDistance = distance;
			}
			
			if (shortestDistance > Arithmetic.Distance(d1.P1, d2.P1))
				shortestDistance = Arithmetic.Distance(d1.P1, d2.P1);
			if (shortestDistance > Arithmetic.Distance(d1.P1, d2.P2))
				shortestDistance = Arithmetic.Distance(d1.P1, d2.P2);
			if (shortestDistance > Arithmetic.Distance(d1.P2, d2.P1))
				shortestDistance = Arithmetic.Distance(d1.P2, d2.P1);
			if (shortestDistance > Arithmetic.Distance(d1.P2, d2.P2))
				shortestDistance = Arithmetic.Distance(d1.P2, d2.P2);
			if (shortestDistance < 0.5)
				shortestDistance = 0.0;

			return shortestDistance;
		}
		#endregion

		#region GetSkew()
		public bool GetSkew(Size pageSize, out double angle, out double weight)
		{
			double angleSum = 0.0;
			double weightSum = 0.0;
			int validDelimiters = 0;

			foreach (Delimiter delimiter in this)
			{
				if (delimiter.IsHorizontal && (delimiter.Width > 250))
				{
					double w = delimiter.Width / pageSize.Width;
					angleSum += delimiter.Angle * w;
					weightSum += w;
					validDelimiters++;
				}
			}

			if (weightSum > 0.0)
			{
				angle = angleSum / weightSum;
				weight = weightSum;
			}
			else
			{
				angle = 0.0;
				weight = 0.0;
			}

			return (validDelimiters > 0);
		}
		#endregion

		#region GetX()
		public int GetX(Point point, double angle, int y)
		{
			if (y == point.Y)
				return point.X;

			if ((Math.Abs(angle) > -0.0001) && (Math.Abs(angle) < 0.0001))
				throw new Exception("GetX(): can't get x from horizontal line!");

			return Convert.ToInt32((double)(point.X + (((double)(y - point.Y)) / Math.Tan(angle))));
		}
		#endregion

		#region GetY()
		public int GetY(Point point, double angle, int x)
		{
			if (x == point.X)
				return point.Y;

			if ((Math.Abs(angle) > 1.5706963267948966) && (Math.Abs(angle) < 1.5708963267948966))
				throw new Exception("GetY(): can't get y from vertical line!");

			return Convert.ToInt32((double)(point.Y + (Math.Tan(angle) * (x - point.X))));
		}
		#endregion

		#region GetDelimiterZones()
		/// <summary>
		/// Some of the zones might be virtual, it means with no real object on the page. Those delimiters are used for zoning.
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="words"></param>
		/// <param name="pictures"></param>
		/// <param name="imageSize"></param>
		/// <returns></returns>
		public DelimiterZones GetDelimiterZones(Symbols symbols, Words words, Pictures pictures, Size imageSize)
		{
#if DEBUG
			DateTime start = DateTime.Now;
			DateTime startLocal = DateTime.Now;
			System.Reflection.MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
#endif

			//DrawToFile(Debug.SaveToDir + "Delimiters.png", imageSize);
			this.ExtendDelimiters(symbols, words, pictures, imageSize);

#if DEBUG
			//Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "() 1: " + DateTime.Now.Subtract(startLocal).ToString());
			startLocal = DateTime.Now;
#endif

			//DrawToFile(Debug.SaveToDir + "Delimiters.png", imageSize);
			this.RemoveInvalidDelimiters(symbols, words, pictures, imageSize);
			//DrawToFile(Debug.SaveToDir + "Delimiters.png", imageSize);

#if DEBUG
			//Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "() 2: " + DateTime.Now.Subtract(startLocal).ToString());
			startLocal = DateTime.Now;
#endif

			/*for (int i = this.Count - 1; i >= 0; i--)
				if (this[i].IsHorizontal && (this[i].AdjacentD1 != null || this[i].AdjacentD2 != null))
					this.RemoveAt(i);*/
			
			//DrawToFile(Debug.SaveToDir + "Delimiters.png", imageSize);
			this.ExtendDelimiters(symbols, words, pictures, imageSize);

#if DEBUG
			//Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "() 3: " + DateTime.Now.Subtract(startLocal).ToString());
			startLocal = DateTime.Now;
#endif

			
			//DrawToFile(Debug.SaveToDir + "Delimiters.png", imageSize);
			this.RemoveInvalidDelimiters(symbols, words, pictures, imageSize);
			//DrawToFile(Debug.SaveToDir + "Delimiters.png", imageSize);

#if DEBUG
			//Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "() 4: " + DateTime.Now.Subtract(startLocal).ToString());
			startLocal = DateTime.Now;
#endif


			DelimiterZones zones = new DelimiterZones(this, imageSize);

#if DEBUG
			//Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "() 5: " + DateTime.Now.Subtract(startLocal).ToString());
			startLocal = DateTime.Now;
#endif

#if DEBUG
			Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "(): " + DateTime.Now.Subtract(start).ToString());
#endif
			
			return zones;
		}
		#endregion

		#region MergeWithLoneSymbols()
		public void MergeWithLoneSymbols(Symbols symbols)
		{
			int delimitersCount;
			int symbolsCount;

			do
			{
				symbolsCount = symbols.Count;
				delimitersCount = this.Count;

				for (int i = this.Count - 1; i >= 0; i--)
				{
					for (int j = symbols.Count - 1; j >= 0; j--)
					{
						if (AreLined(this[i], symbols[j]))
						{
							this[i].AddSymbol(symbols[j]);
							symbols.RemoveAt(j);
						}
						else if (this[i].ShouldContain(symbols[j]))
						{
							this[i].AddSymbol(symbols[j]);
							symbols.RemoveAt(j);
						}
					}
				}

				Merge();
			}
			while (symbolsCount != symbols.Count || delimitersCount != this.Count);
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
				result = Debug.GetBitmap(imageSize);
				int counter = 0;
				Graphics g = Graphics.FromImage(result);

				foreach (Delimiter delimiter in this)
				{
					Color color = Debug.GetColor(counter++);
					color = Color.FromArgb(100, color.R, color.G, color.B);
					g.FillRectangle(new SolidBrush(color), delimiter.Rectangle);
				}

				bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				foreach (Delimiter delimiter in this)
					delimiter.DrawToImage(Debug.GetColor(counter++), bmpData);
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

		#region DrawToImage()
		public void DrawToImage(Bitmap result)
		{
#if SAVE_RESULTS
			BitmapData bmpData = null;

			try
			{
				Graphics g = Graphics.FromImage(result);
				Color color = Color.Green;
				Brush brush = new SolidBrush(Color.FromArgb(100, color));
				
				foreach (Delimiter delimiter in this)
					g.FillRectangle(brush, delimiter.Rectangle);

				bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				foreach (Delimiter delimiter in this)
					delimiter.DrawToImage(color, bmpData);
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

		#region MergeWithSymbols()
		private void MergeWithSymbols(Symbols symbols)
		{
			int delimitersCount;
			int symbolsCount;

			Delimiters changedDelimiters = new Delimiters();
			Delimiters changedDelimitersTmp = new Delimiters();

			changedDelimiters.AddRange(this);

			do
			{
				symbolsCount = symbols.Count;
				delimitersCount = this.Count;

				changedDelimitersTmp.Clear();

				foreach (Delimiter delimiter in changedDelimiters)
				{
					Symbols adjacents = delimiter.GetAdjacents(symbols);

					foreach (Symbol adjacent in adjacents)
					{
						if (changedDelimitersTmp.Contains(delimiter) == false)
							changedDelimitersTmp.Add(delimiter);

						delimiter.AddSymbol(adjacent);
						symbols.Remove(adjacent);
					}
				}

				for (int i = this.Count - 2; i >= 0; i--)
				{
					for (int j = this.Count - 1; j > i; j--)
					{
						if (AreLined(this[i], this[j]) || IsDoubleLine(this[i], this[j]))
						{
							if (changedDelimitersTmp.Contains(this[i]) == false)
								changedDelimitersTmp.Add(this[i]);

							this[i].Merge(this[j]);
							this.RemoveAt(j);
						}
					}
				}

				changedDelimiters.Clear();
				changedDelimiters.AddRange(changedDelimitersTmp);
			}
			while (changedDelimiters.Count > 0);//(symbolsCount != symbols.Count || delimitersCount != this.Count);

		}
		#endregion

		#region Merge()
		private void Merge()
		{
			int delimitersCount;

			do
			{
				delimitersCount = this.Count;
			
				for (int i = this.Count - 2; i >= 0; i--)
				{
					for (int j = this.Count - 1; j > i; j--)
					{
						//merge double lines into single line
						if (AreLined(this[i], this[j]) || IsDoubleLine(this[i], this[j]))
						{
							this[i].Merge(this[j]);
							this.RemoveAt(j);
						}
					}
				}
			}
			while (delimitersCount != this.Count);
		}
		#endregion

		#region Validate()
		/// <summary>
		/// If delimiter angle is off of the median angle by more than maxAngleDeviation, it 
		/// destroys delimiter and adds it's symbol to symbols
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="maxAngleDeviation">Angle in radians.</param>
		private void Validate(Symbols symbols, double maxAngleDeviation)
		{
			if (this.Count > 4)
			{
				double angle;
				int i;
				List<double> angles = new List<double>();

				foreach (Delimiter delimiter in this)
				{
					angle = delimiter.Angle;
					while (angle > Math.PI / 4)
						angle -= Math.PI / 2;
					while (angle < -Math.PI / 4)
						angle += Math.PI / 2;
					angles.Add(angle);
				}

				double bestAngle = double.MaxValue;
				int bestIndex = 0;
				
				for (i = 0; i < angles.Count; i++)
				{
					angle = 0.0;

					foreach (double angleListItem in angles)
						angle += (angleListItem - angles[i]) * (angleListItem - angles[i]);

					if (bestAngle > angle)
					{
						bestAngle = angle;
						bestIndex = i;
					}
				}
				
				for (i = this.Count - 1; i >= 0; i--)
				{
					if (angles[i] - angles[bestIndex] > maxAngleDeviation || angles[i] - angles[bestIndex] < -maxAngleDeviation)
					{
						Symbol symbol = new Symbol(this[i].ObjectMap);
						
						if (this[i].Width < 20 || this[i].Height < 20)
							symbol.ObjectType = Symbol.Type.Line;
						else
							symbol.ObjectType = Symbol.Type.Picture;

						this.RemoveAt(i);
						symbols.Add(symbol);
					}
				}
			}
		}
		#endregion

		#region AreLined()
		private static bool AreLined(Delimiter d1, Delimiter d2)
		{
			if (d1.DelimiterType == d2.DelimiterType)
			{
				if (d1.DelimiterType == Delimiter.Type.Horizontal)
				{
					/*int maxDistance = 10 * d1.ObjectShape.MaxPixelHeight;

					if (((d1.Right <= d2.X && d2.X - d1.Right < maxDistance) || (d2.Right <= d1.X && d1.X - d2.Right < maxDistance)) && Arithmetic.AreInLine(d1.Y, d1.Bottom, d2.Y, d2.Bottom))
					{
						int h1 = d1.ObjectShape.MaxPixelHeight;
						int h2 = d2.ObjectShape.MaxPixelHeight;

						if ((h1 < 50) && (h2 < 50) && (h1 > h2 * .5) && (h1 < h2 * 2.0))
						{
							if ((d1.Angle - d2.Angle < 0.10471975511965977) && (d1.Angle - d2.Angle > -0.10471975511965977))
								return true;
						}
					}*/
				}
				else
				{
					int maxDistance = 10 * d1.ObjectShape.MaxPixelWidth;

					if (((d1.Bottom <= d2.Y && d2.Y - d1.Bottom < maxDistance) || (d2.Bottom <= d1.Y && d1.Y - d2.Bottom < maxDistance)) && Arithmetic.AreInLine(d1.X, d1.Right, d2.X, d2.Right))
					{
						int h1 = d1.ObjectShape.MaxPixelWidth;
						int h2 = d2.ObjectShape.MaxPixelWidth;

						if ((h1 < 50) && (h2 < 50) && (h1 > h2 * .5) && (h1 < h2 * 2.0))
						{
							if ((d1.Angle - d2.Angle < 0.10471975511965977) && (d1.Angle - d2.Angle > -0.10471975511965977))
								return true;
						}
					}
				}
			}

			return false;
		}
		#endregion

		#region AreLined()
		/// <summary>
		/// Delimiter and symbol should be merged if:
		/// 1) The distance between them is less than 7 pixels
		/// 2) Ale in line
		/// 3) Pixel width (height for horizontal delimiter) of the symbol is equal or 
		/// less than pixel width (height for horizontal delimiter) of delimiter
		/// </summary>
		/// <param name="d"></param>
		/// <param name="symbol"></param>
		/// <returns></returns>
		private static bool AreLined(Delimiter d, Symbol symbol)
		{		
			if (d.IsHorizontal)
			{
				/*int maxDistance = 10 * d.ObjectShape.MaxPixelHeight;		
				
				if (symbol.X < d.X && symbol.Right > d.X - maxDistance && Arithmetic.AreInLine(d.Y, d.Bottom, symbol.Y, symbol.Bottom) && d.ObjectShape.MaxPixelHeight >= symbol.ObjectShape.MaxPixelHeight)
					return true;
				else if (symbol.Right > d.Right && symbol.X < d.Right + maxDistance && Arithmetic.AreInLine(d.Y, d.Bottom, symbol.Y, symbol.Bottom) && d.ObjectShape.MaxPixelHeight >= symbol.ObjectShape.MaxPixelHeight)
					return true;*/
			}
			else
			{
				int maxDistance = 10 * d.ObjectShape.MaxPixelWidth;
				
				if (symbol.Y < d.Y && symbol.Bottom > d.Y - maxDistance && Arithmetic.AreInLine(d.X, d.Right, symbol.X, symbol.Right) && d.ObjectShape.MaxPixelWidth >= symbol.ObjectShape.MaxPixelWidth)
					return true;
				else if (symbol.Bottom > d.Bottom && symbol.Y < d.Bottom + maxDistance && Arithmetic.AreInLine(d.X, d.Right, symbol.X, symbol.Right) && d.ObjectShape.MaxPixelWidth >= symbol.ObjectShape.MaxPixelWidth)
					return true;
			}

			return false;
		}
		#endregion

		#region IsDoubleLine()
		/// <summary>
		/// Delimiters are in line if:
		///  1) They are both horizontal or vertical
		///  2) They share horizontal or vertical space
		///  3) The distance between them is max 4x bigger than thinner delimiter
		///  4) Delimiters deight is max 50 pixels
		///  5) Delimiters have similar width/height
		///  6) Angle of the delimiters is similar
		///  7) In each shared point, distance between delimiters is in range
		/// </summary>
		/// <param name="d1"></param>
		/// <param name="d2"></param>
		/// <returns></returns>
		private static bool IsDoubleLine(Delimiter d1, Delimiter d2)
		{
			if (d1.DelimiterType == d2.DelimiterType)
			{
				if (d1.DelimiterType == Delimiter.Type.Horizontal)
				{
					if (Arithmetic.AreInLine(d1.X, d1.Right, d2.X, d2.Right))
					{
						int distance = Arithmetic.Distance(d1.Rectangle, d2.Rectangle);
						int maxDistance = Math.Min(d1.ObjectShape.MaxPixelHeight, d2.ObjectShape.MaxPixelHeight) * 4;

						if (distance < maxDistance)
						{
							int h1 = d1.ObjectShape.MaxPixelHeight;
							int h2 = d2.ObjectShape.MaxPixelHeight;

							if ((h1 < 50) && (h2 < 50) /*&& (h1 > h2 * .5) && (h1 < h2 * 2.0)*/)
							{
								if ((d1.Angle - d2.Angle < 0.10471975511965977) && (d1.Angle - d2.Angle > -0.10471975511965977))
								{
									//check the distance in every shared point			
									Point[] points1 = d1.ObjectShape.GetCurve();
									Point[] points2 = d2.ObjectShape.GetCurve();

									int xFrom = Math.Max(points1[0].X, points2[0].X);
									int xTo = Math.Min(points1[points1.Length - 1].X, points2[points2.Length - 1].X);

									if (xTo - xFrom > 0)
									{
										int p1Index = xFrom - points1[0].X;
										int p2Index = xFrom - points2[0].X;

										for (int x = 0; x <= xTo - xFrom; x++)
											if ((points1[p1Index + x].Y - points2[p2Index + x].Y > maxDistance) || (points1[p1Index + x].Y - points2[p2Index + x].Y < -maxDistance))
												return false;

										return true;
									}
								}
							}
						}
					}
				}
				else
				{
					if (Arithmetic.AreInLine(d1.Y, d1.Bottom, d2.Y, d2.Bottom))
					{
						int distance = Arithmetic.Distance(d1.Rectangle, d2.Rectangle);
						int maxDistance = Math.Min(d1.ObjectShape.MaxPixelWidth, d2.ObjectShape.MaxPixelWidth) * 4;

						if (distance < maxDistance)
						{
							int h1 = d1.ObjectShape.MaxPixelWidth;
							int h2 = d2.ObjectShape.MaxPixelWidth;

							if ((h1 < 50) && (h2 < 50) /*&& (h1 > h2 * .5) && (h1 < h2 * 2.0)*/)
							{
								if ((d1.Angle - d2.Angle < 0.10471975511965977) && (d1.Angle - d2.Angle > -0.10471975511965977))
								{
									//check the distance in every shared point			
									Point[] points1 = d1.ObjectShape.GetCurve();
									Point[] points2 = d2.ObjectShape.GetCurve();

									int yFrom = Math.Max(points1[0].Y, points2[0].Y);
									int yTo = Math.Min(points1[points1.Length - 1].Y, points2[points2.Length - 1].Y);

									if (yTo - yFrom > 0)
									{
										int p1Index = yFrom - points1[0].Y;
										int p2Index = yFrom - points2[0].Y;

										for (int y = 0; y <= yTo - yFrom; y++)
											if ((points1[p1Index + y].X - points2[p2Index + y].X > maxDistance) || (points1[p1Index + y].X - points2[p2Index + y].X < -maxDistance))
												return false;

										return true;
									}
								}
							}
						}
					}
				}
			}

			return false;
		}
		#endregion

		#region Contains()
		private static bool Contains(Rectangle rect, double x, double y)
		{
			return ((x >= rect.X) && (x <= rect.Right) && (y >= rect.Y) && (y <= rect.Bottom));
		}
		#endregion

		#region ExtendDelimiters()
		private void ExtendDelimiters(Symbols symbols, Words words, Pictures pictures, Size imageSize)
		{
			int count;
			
			do
			{
				count = this.Count;
				
				for (int i = this.Count - 1; i >= 0; i--)
				{
					Delimiter delimiter = this[i];

					if (delimiter.IsVertical)
					{
						bool topSet = false;
						bool bottomSet = false;

						//if (delimiter.Height > imageSize.Height * .3)
						{
							topSet = this.SetTopOfVerticalDelimiter(delimiter, symbols, words, pictures, imageSize);
							bottomSet = this.SetBottomOfVerticalDelimiter(delimiter, symbols, words, pictures, imageSize);
						}

						/*if (!(topSet && bottomSet))
							this.RemoveAt(i);*/
					}
					else
					{
						bool leftSet = false;
						bool rightSet = false;

						if (delimiter.Width > imageSize.Width * 0.3)
						{
							leftSet = this.SetLeftOfHorizontalDelimiter(delimiter, symbols, words, pictures, imageSize);
							rightSet = this.SetRightOfHorizontalDelimiter(delimiter, symbols, words, pictures, imageSize);
						}

						if (leftSet == false || rightSet == false)
							this.RemoveAt(i);
					}
				}
			}
			while (count != this.Count);
		}
		#endregion

		#region FindBottomDelimiter()
		private Point FindBottomDelimiter(Point point, double angle, Symbols symbols, Words words, Pictures pictures, Size imageSize, int xFrom, int xTo, out Delimiter bottomDelimiter)
		{
			bottomDelimiter = null;
			Point newBottom = new Point(this.GetX(point, angle, imageSize.Height), imageSize.Height);
			Line2D line1 = new Line2D((PointF)point, (PointF)newBottom);
			
			foreach (Delimiter delimiter in this)
			{
				if ((delimiter.IsHorizontal && (delimiter.GetY(point.X) > point.Y)) && ((delimiter.X <= xTo) && (delimiter.Right >= xFrom)))
				{
					Line2D line2 = new Line2D((PointF)delimiter.P1, (PointF)delimiter.P2);
					double x = 0.0;
					double y = 0.0;
					if ((line1.InterceptPoint(line2, ref x, ref y) && (((((x >= 0.0) && (y >= 0.0)) && ((x < imageSize.Width) && (y < imageSize.Height))) && (y > (point.Y - 1))) && (y < newBottom.Y))) && this.GetClosestSymbolOnTheWay(delimiter, symbols, words, pictures, (int)x, (int)y).IsEmpty)
					{
						newBottom = new Point(Convert.ToInt32(x), Convert.ToInt32(y));
						bottomDelimiter = delimiter;
					}
				}
			}

			Rectangle symbol = this.GetClosestSymbolOnTheWay(point, angle, symbols, words, pictures, newBottom.X, newBottom.Y);
			
			if (!symbol.IsEmpty)
			{
				newBottom = new Point(this.GetX(point, angle, symbol.Y), symbol.Y);
				bottomDelimiter = null;
			}
			
			return newBottom;
		}
		#endregion

		#region FindLeftDelimiter()
		private Point FindLeftDelimiter(Point point, double angle, Symbols symbols, Words words, Pictures pictures, Size imageSize, int yFrom, int yTo, out Delimiter leftDelimiter)
		{
			leftDelimiter = null;
			Point newLeft = new Point(0, this.GetY(point, angle, 0));
			Line2D line1 = new Line2D((PointF)point, (PointF)newLeft);
			foreach (Delimiter delimiter in this)
			{
				if (((delimiter.X < point.X) && delimiter.IsVertical) && ((delimiter.Y <= yTo) && (delimiter.Bottom >= yFrom)))
				{
					Line2D line2 = new Line2D((PointF)delimiter.P1, (PointF)delimiter.P2);
					double x = 0.0;
					double y = 0.0;
					if ((line1.InterceptPoint(line2, ref x, ref y) && (((((x >= 0.0) && (y >= 0.0)) && ((x < imageSize.Width) && (y < imageSize.Height))) && (x < (point.X + 1))) && (x > newLeft.X))) && this.GetClosestSymbolOnTheWay(delimiter, symbols, words, pictures, (int)x, (int)y).IsEmpty)
					{
						newLeft = new Point(Convert.ToInt32(x), Convert.ToInt32(y));
						leftDelimiter = delimiter;
					}
				}
			}
			Rectangle symbol = this.GetClosestSymbolOnTheWay(point, angle, symbols, words, pictures, newLeft.X, newLeft.Y);
			if (!symbol.IsEmpty)
			{
				newLeft = new Point(symbol.Right, this.GetY(point, angle, symbol.Right));
				leftDelimiter = null;
			}
			return newLeft;
		}
		#endregion

		#region FindRightDelimiter()
		private Point FindRightDelimiter(Point point, double angle, Symbols symbols, Words words, Pictures pictures, Size imageSize, int yFrom, int yTo, out Delimiter rightDelimiter)
		{
			rightDelimiter = null;
			Point rightPoint = new Point(imageSize.Width, this.GetY(point, angle, imageSize.Width));
			Line2D line1 = new Line2D((PointF)point, (PointF)rightPoint);
			foreach (Delimiter delimiter in this)
			{
				if (((delimiter.X > point.X) && delimiter.IsVertical) && ((delimiter.Y <= yTo) && (delimiter.Bottom >= yFrom)))
				{
					Line2D line2 = new Line2D((PointF)delimiter.P1, (PointF)delimiter.P2);
					double x = 0.0;
					double y = 0.0;
					if ((line1.InterceptPoint(line2, ref x, ref y) && (((((x >= 0.0) && (y >= 0.0)) && ((x < imageSize.Width) && (y < imageSize.Height))) && (x > (point.X - 1))) && (x < rightPoint.X))) && this.GetClosestSymbolOnTheWay(delimiter, symbols, words, pictures, (int)x, (int)y).IsEmpty)
					{
						rightPoint = new Point(Convert.ToInt32(x), Convert.ToInt32(y));
						rightDelimiter = delimiter;
					}
				}
			}
			Rectangle symbol = this.GetClosestSymbolOnTheWay(point, angle, symbols, words, pictures, rightPoint.X, rightPoint.Y);
			if (!symbol.IsEmpty)
			{
				rightPoint = new Point(symbol.X, this.GetY(point, angle, symbol.X));
				rightDelimiter = null;
			}
			return rightPoint;
		}
		#endregion

		#region FindTopDelimiter()
		private Point FindTopDelimiter(Point point, double angle, Symbols symbols, Words words, Pictures pictures, Size imageSize, int xFrom, int xTo, out Delimiter topDelimiter)
		{
			topDelimiter = null;
			Point newTop = new Point(this.GetX(point, angle, 0), 0);
			Line2D line1 = new Line2D((PointF)point, (PointF)newTop);
			
			foreach (Delimiter delimiter in this)
			{
				if (delimiter.IsHorizontal && ((delimiter.X <= xTo) && (delimiter.Right >= xFrom)))
				{
					Line2D line2 = new Line2D((PointF)delimiter.P1, (PointF)delimiter.P2);
					double x = 0.0;
					double y = 0.0;
					
					if ((line1.InterceptPoint(line2, ref x, ref y) && (((((x >= 0.0) && (y >= 0.0)) && ((x < imageSize.Width) && (y < imageSize.Height))) && (y < (point.Y + 1))) && (y > newTop.Y))) && this.GetClosestSymbolOnTheWay(delimiter, symbols, words, pictures, (int)x, (int)y).IsEmpty)
					{
						newTop = new Point(Convert.ToInt32(x), Convert.ToInt32(y));
						topDelimiter = delimiter;
					}
				}
			}
			
			Rectangle symbol = this.GetClosestSymbolOnTheWay(point, angle, symbols, words, pictures, newTop.X, newTop.Y);
			if (!symbol.IsEmpty)
			{
				newTop = new Point(this.GetX(point, angle, symbol.Bottom), symbol.Bottom);
				topDelimiter = null;
			}
			
			return newTop;
		}
		#endregion

		#region GetAngle()
		private static double GetAngle(Delimiter d1, Delimiter d2)
		{
			double angle;
			
			if (d1.IsHorizontal)
			{
				if (d1.LeftPoint.X < d2.LeftPoint.X)
					angle = Arithmetic.GetAngle(d1.LeftPoint, d2.RightPoint);
				else
					angle = Arithmetic.GetAngle(d2.LeftPoint, d1.RightPoint);
			}
			else if (d1.TopPoint.Y < d2.TopPoint.Y)
				angle = Arithmetic.GetAngle(d1.TopPoint, d2.BottomPoint);
			else
				angle = Arithmetic.GetAngle(d2.TopPoint, d1.BottomPoint);

			while (angle < -(Math.PI / 4))
				angle += Math.PI;

			return angle;
		}
		#endregion

		#region GetClosestSymbolOnTheWay()
		private Rectangle GetClosestSymbolOnTheWay(Delimiter delimiter, Symbols symbols, Words words, Pictures pictures, int x, int y)
		{
			if (delimiter.IsHorizontal)
			{
				if ((x >= delimiter.LeftPoint.X) && (x <= delimiter.RightPoint.X))
					return Rectangle.Empty;
				if (x < delimiter.LeftPoint.X)
					return this.GetClosestSymbolOnTheWay(delimiter.LeftPoint, delimiter.Angle, symbols, words, pictures, x, y);

				return this.GetClosestSymbolOnTheWay(delimiter.RightPoint, delimiter.Angle, symbols, words, pictures, x, y);
			}
			
			if ((y >= delimiter.TopPoint.Y) && (y <= delimiter.BottomPoint.Y))
				return Rectangle.Empty;
			
			if (y < delimiter.TopPoint.Y)
				return this.GetClosestSymbolOnTheWay(delimiter.TopPoint, delimiter.Angle, symbols, words, pictures, x, y);
			
			return this.GetClosestSymbolOnTheWay(delimiter.BottomPoint, delimiter.Angle, symbols, words, pictures, x, y);
		}
		#endregion

		#region GetClosestSymbolOnTheWay()
		private Rectangle GetClosestSymbolOnTheWay(Point point, double angle, Symbols symbols, Words words, Pictures pictures, int xLimit, int yLimit)
		{
			int i;
			Symbol symbol;
			double x;
			Rectangle result = Rectangle.Empty;
			angle = Arithmetic.Get1stOr2ndSectorAngle(angle);
			
			if ((angle < Math.PI / 4) && (angle > -Math.PI / 4))
			{
				double y;
				if (xLimit < point.X)
				{
					for (i = symbols.Count - 1; i >= 0; i--)
					{
						symbol = symbols[i];
						if (((!symbol.IsPunctuation && (symbol.Right < point.X)) && (symbol.Right > xLimit)) && ((symbol.Word != null) || ((symbol.Width / symbol.Height) < 4)))
						{
							y = this.GetY(point, angle, symbol.Right);
							if ((symbol.Y <= y) && (symbol.Bottom >= y))
							{
								if ((symbol.Height > 10) || (symbol.Width < 15))
								{
									xLimit = symbol.Right;
									result = symbol.Rectangle;
								}
								else
								{
									symbols.RemoveAt(i);
								}
							}
						}
					}
					foreach (Word word in words)
					{
						if ((word.Right < point.X) && (word.Right > xLimit))
						{
							y = this.GetY(point, angle, word.Right);
							if ((word.Y <= y) && (word.Bottom >= y))
							{
								xLimit = word.Right;
								result = word.Rectangle;
							}
						}
					}
					foreach (Picture picture in pictures)
					{
						if ((picture.Right < point.X) && (picture.Right > xLimit))
						{
							y = this.GetY(point, angle, picture.Right);
							if ((picture.Y <= y) && (picture.Bottom >= y))
							{
								xLimit = picture.Right;
								result = picture.Rectangle;
							}
						}
					}
					return result;
				}
				i = symbols.Count - 1;
				while (i >= 0)
				{
					symbol = symbols[i];
					if (((!symbol.IsPunctuation && (symbol.X > point.X)) && (symbol.X < xLimit)) && ((symbol.Word != null) || ((symbol.Width / symbol.Height) < 4)))
					{
						y = this.GetY(point, angle, symbol.X);
						if ((symbol.Y <= y) && (symbol.Bottom >= y))
						{
							if ((symbol.Height > 10) || (symbol.Width < 15))
							{
								xLimit = symbol.X;
								result = symbol.Rectangle;
							}
							else
							{
								symbols.RemoveAt(i);
							}
						}
					}
					i--;
				}
				foreach (Word word in words)
				{
					if ((word.X > point.X) && (word.X < xLimit))
					{
						y = this.GetY(point, angle, word.X);
						if ((word.Y <= y) && (word.Bottom >= y))
						{
							xLimit = word.X;
							result = word.Rectangle;
						}
					}
				}
				foreach (Picture picture in pictures)
				{
					if ((picture.X > point.X) && (picture.X < xLimit))
					{
						y = this.GetY(point, angle, picture.X);
						if ((picture.Y <= y) && (picture.Bottom >= y))
						{
							xLimit = picture.X;
							result = picture.Rectangle;
						}
					}
				}
				return result;
			}
			if (yLimit < point.Y)
			{
				for (i = symbols.Count - 1; i >= 0; i--)
				{
					symbol = symbols[i];
					if ((!symbol.IsPunctuation && (symbol.Bottom < point.Y)) && (symbol.Bottom > yLimit))
					{
						x = this.GetX(point, angle, symbol.Bottom);
						if (((symbol.X <= x) && (symbol.Right >= x)) && ((symbol.Word != null) || ((symbol.Height / symbol.Width) < 4)))
						{
							if ((symbol.Width > 4) || (symbol.Height > 100))
							{
								yLimit = symbol.Bottom;
								result = symbol.Rectangle;
							}
							else
							{
								symbols.RemoveAt(i);
							}
						}
					}
				}
				foreach (Word word in words)
				{
					if ((word.Bottom < point.Y) && (word.Bottom > yLimit))
					{
						x = this.GetX(point, angle, word.Bottom);
						if ((word.X <= x) && (word.Right >= x))
						{
							yLimit = word.Bottom;
							result = word.Rectangle;
						}
					}
				}
				foreach (Picture picture in pictures)
				{
					if ((picture.Bottom < point.Y) && (picture.Bottom > yLimit))
					{
						x = this.GetX(point, angle, picture.Bottom);
						if ((picture.X <= x) && (picture.Right >= x))
						{
							yLimit = picture.Bottom;
							result = picture.Rectangle;
						}
					}
				}
				return result;
			}
			for (i = symbols.Count - 1; i >= 0; i--)
			{
				symbol = symbols[i];
				if (((!symbol.IsPunctuation && (symbol.Y > point.Y)) && (symbol.Y < yLimit)) && ((symbol.Word != null) || ((symbol.Height / symbol.Width) < 4)))
				{
					x = this.GetX(point, angle, symbol.Y);
					if ((symbol.X <= x) && (symbol.Right >= x))
					{
						if ((symbol.Width > 4) || (symbol.Height > 100))
						{
							yLimit = symbol.Y;
							result = symbol.Rectangle;
						}
						else
						{
							symbols.RemoveAt(i);
						}
					}
				}
			}
			foreach (Word word in words)
			{
				if ((word.Y > point.Y) && (word.Y < yLimit))
				{
					x = this.GetX(point, angle, word.Y);
					if ((word.X <= x) && (word.Right >= x))
					{
						yLimit = word.Y;
						result = word.Rectangle;
					}
				}
			}
			foreach (Picture picture in pictures)
			{
				if ((picture.Y > point.Y) && (picture.Y < yLimit))
				{
					x = this.GetX(point, angle, picture.Y);
					if ((picture.X <= x) && (picture.Right >= x))
					{
						yLimit = picture.Y;
						result = picture.Rectangle;
					}
				}
			}
			return result;
		}
		#endregion

		#region GetEndPointsDistance()
		private static double GetEndPointsDistance(Delimiter d1, Delimiter d2)
		{
			Delimiter longerDelimiter;
			Delimiter shorterDelimiter;
			
			if (d1.IsHorizontal)
			{
				longerDelimiter = (d1.Length > d2.Length) ? d1 : d2;
				shorterDelimiter = (longerDelimiter == d1) ? d2 : d1;
				
				if (longerDelimiter.RightPoint.X < shorterDelimiter.LeftPoint.X)
					return (double)(shorterDelimiter.LeftPoint.X - longerDelimiter.RightPoint.X);
				if (shorterDelimiter.RightPoint.X < longerDelimiter.LeftPoint.X)
					return (double)(longerDelimiter.LeftPoint.X - shorterDelimiter.RightPoint.X);

				return 0.0;
			}
			
			longerDelimiter = (d1.Length > d2.Length) ? d1 : d2;
			shorterDelimiter = (longerDelimiter == d1) ? d2 : d1;
			
			if (longerDelimiter.BottomPoint.Y < shorterDelimiter.TopPoint.Y)
				return (double)(shorterDelimiter.TopPoint.Y - longerDelimiter.BottomPoint.Y);

			if (shorterDelimiter.BottomPoint.Y < longerDelimiter.TopPoint.Y)
				return (double)(longerDelimiter.TopPoint.Y - shorterDelimiter.BottomPoint.Y);
			
			return 0.0;
		}
		#endregion

		#region GetOpositeDirectionDistance()
		private static double GetOpositeDirectionDistance(Delimiter d1, Delimiter d2)
		{
			int sharedLength;
			int shorterLineLength;
			double distance1;
			double distance2;

			if (d1.IsHorizontal && d2.IsHorizontal)
			{
				sharedLength = ((d1.RightPoint.X < d2.RightPoint.X) ? d1.RightPoint.X : d2.RightPoint.X) - ((d1.LeftPoint.X > d2.LeftPoint.X) ? d1.LeftPoint.X : d2.LeftPoint.X);
				shorterLineLength = (d1.Width < d2.Width) ? d1.Width : d2.Width;
				
				if (sharedLength > (shorterLineLength * 0.8))
				{
					if (d1.LeftPoint.X <= d2.LeftPoint.X)
						distance1 = Math.Abs((double)(d2.LeftPoint.Y - d1.GetY(d2.LeftPoint.X)));
					else
						distance1 = Math.Abs((double)(d1.LeftPoint.Y - d2.GetY(d1.LeftPoint.X)));
					
					if (d1.RightPoint.X >= d2.RightPoint.X)
						distance2 = Math.Abs((double)(d2.RightPoint.Y - d1.GetY(d2.RightPoint.X)));
					else
						distance2 = Math.Abs((double)(d1.RightPoint.Y - d2.GetY(d1.RightPoint.X)));

					return ((distance1 > distance2) ? distance1 : distance2);
				}
			}
			else if (d1.IsVertical && d2.IsVertical)
			{
				sharedLength = ((d1.BottomPoint.Y < d2.BottomPoint.Y) ? d1.BottomPoint.Y : d2.BottomPoint.Y) - ((d1.TopPoint.Y > d2.TopPoint.Y) ? d1.TopPoint.Y : d2.TopPoint.Y);
				shorterLineLength = (d1.Height < d2.Height) ? d1.Height : d2.Height;
				
				if (sharedLength > (shorterLineLength * 0.8))
				{
					if (d1.TopPoint.Y <= d2.TopPoint.Y)
						distance1 = Math.Abs((double)(d2.TopPoint.X - d1.GetX(d2.TopPoint.Y)));
					else
						distance1 = Math.Abs((double)(d1.TopPoint.X - d2.GetX(d1.TopPoint.Y)));
					
					if (d1.BottomPoint.Y >= d2.BottomPoint.Y)
						distance2 = Math.Abs((double)(d2.BottomPoint.X - d1.GetX(d2.BottomPoint.Y)));
					else
						distance2 = Math.Abs((double)(d1.BottomPoint.X - d2.GetX(d1.BottomPoint.Y)));

					return ((distance1 > distance2) ? distance1 : distance2);
				}
			}
			
			return double.MaxValue;
		}
		#endregion

		#region GetTangents()
		private void GetTangents(Delimiter d, out Delimiter d1, out Delimiter d2)
		{
			d1 = null;
			d2 = null;
			Line2D line = new Line2D((PointF)d.P1, (PointF)d.P2);
			double x = 0.0;
			double y = 0.0;
			double bestDistance1 = double.MaxValue;
			double bestDistance2 = double.MaxValue;
			
			foreach (Delimiter delimiter in this)
			{
				if (delimiter.IsVertical != d.IsVertical)
				{
					Line2D lineD = new Line2D((PointF)delimiter.P1, (PointF)delimiter.P2);
					
					if (line.InterceptPoint(lineD, ref x, ref y) && lineD.IsPointOnLine(x, y))
					{
						double distance1 = Arithmetic.Distance(d.P1, x, y);
						double distance2 = Arithmetic.Distance(d.P2, x, y);
						
						if (distance1 < distance2)
						{
							if (distance1 < bestDistance1)
							{
								bestDistance1 = distance1;
								d1 = delimiter;
							}
						}
						else if (distance2 < bestDistance2)
						{
							bestDistance2 = distance1;
							d2 = delimiter;
						}
					}
				}
			}
		}
		#endregion

		#region RemoveInvalidDelimiters()
		private void RemoveInvalidDelimiters(Symbols symbols, Words words, Pictures pictures, Size imageSize)
		{
			int count;

			do
			{
				count = this.Count;

				for (int i = this.Count - 1; i >= 0; i--)
				{
					Delimiter delimiter = this[i];
					
					if (delimiter.IsHorizontal && ((delimiter.AdjacentD1 == null && delimiter.X > 0) || (delimiter.AdjacentD2 == null && delimiter.Right < imageSize.Width)))
					{
						symbols.Add(new Symbol(delimiter.ObjectMap));
						this.RemoveAt(i);
					}
				}
			} while (this.Count != count);
		}
		#endregion

		#region SetLeftOfHorizontalDelimiter()
		private bool SetLeftOfHorizontalDelimiter(Delimiter delimiter, Symbols symbols, Words words, Pictures pictures, Size imageSize)
		{
			Delimiter delimiterL;
			if ((delimiter.X == 0) || ((delimiter.AdjacentD1 != null) && this.Contains(delimiter.AdjacentD1)))
				return true;
			
			delimiter.AdjacentD1 = null;
			Point pointL = this.FindLeftDelimiter(delimiter.LeftPoint, delimiter.Angle, symbols, words, pictures, imageSize, 0, imageSize.Height, out delimiterL);
			
			if ((delimiterL != null) || (pointL.X == 0))
			{
				delimiter.AdjacentD1 = delimiterL;
				delimiter.LeftPoint = pointL;
				return true;
			}
			
			for (int x = delimiter.X; (x > pointL.X) && (x >= 0); x -= 5)
			{
				Point p = new Point(x, (int)delimiter.GetY(x));
				
				if ((((p.X >= 0) && (p.X <= imageSize.Width)) && (p.Y >= 0)) && (p.Y <= imageSize.Height))
				{
					Delimiter d1;
					Delimiter d2;
					Point pT = this.FindTopDelimiter(new Point(p.X, p.Y - 2), delimiter.Angle + 1.5707963267948966, symbols, words, pictures, imageSize, delimiter.X, delimiter.Right, out d1);
					Point pB = this.FindBottomDelimiter(new Point(p.X, p.Y + 2), delimiter.Angle + 1.5707963267948966, symbols, words, pictures, imageSize, delimiter.X, delimiter.Right, out d2);
					
					if (((d1 != null) || (pT.Y == 0)) && ((d2 != null) || (pB.Y == imageSize.Height)))
					{
						delimiter.AdjacentD1 = new Delimiter(pT, pB, Delimiter.Type.Vertical);
						delimiter.LeftPoint = p;
						return true;
					}
				}
			}
			
			return false;
		}
		#endregion

		#region SetTopOfVerticalDelimiter()
		private bool SetTopOfVerticalDelimiter(Delimiter delimiter, Symbols symbols, Words words, Pictures pictures, Size imageSize)
		{
			Delimiter delimiterT;
			
			if ((delimiter.X == 0) || ((delimiter.AdjacentD1 != null) && this.Contains(delimiter.AdjacentD1)))
				return true;
			
			delimiter.AdjacentD1 = null;
			Point pointT = this.FindTopDelimiter(delimiter.TopPoint, delimiter.Angle, symbols, words, pictures, imageSize, 0, imageSize.Width, out delimiterT);
			
			if ((delimiterT != null) || (pointT.Y == 0))
			{
				delimiter.AdjacentD1 = delimiterT;
				delimiter.TopPoint = pointT;
				return true;
			}
			
			for (int y = delimiter.Y; (y > pointT.Y) && (y >= 0); y -= 5)
			{
				Point p = new Point((int)delimiter.GetX(y), y);
				
				if ((((p.X >= 0) && (p.X <= imageSize.Width)) && (p.Y >= 0)) && (p.Y <= imageSize.Height))
				{
					Delimiter d1;
					Delimiter d2;

					Point pL = this.FindLeftDelimiter(new Point(p.X - 2, p.Y), delimiter.Angle + 1.5707963267948966, symbols, words, pictures, imageSize, delimiter.Y, delimiter.Bottom, out d1);
					Point pR = this.FindRightDelimiter(new Point(p.X + 2, p.Y), delimiter.Angle + 1.5707963267948966, symbols, words, pictures, imageSize, delimiter.Y, delimiter.Bottom, out d2);

					if ((d1 == null) && (d2 == null) && (pL.X == 0) && (pR.X == imageSize.Width))
					{
						Delimiter newDelimiter = new Delimiter(pL, pR, Delimiter.Type.Horizontal);
						delimiter.AdjacentD1 = newDelimiter;
						delimiter.TopPoint = p;

						this.Add(newDelimiter);
						return true;
					}
				}
			}
			
			return false;
		}
		#endregion

		#region SetRightOfHorizontalDelimiter()
		private bool SetRightOfHorizontalDelimiter(Delimiter delimiter, Symbols symbols, Words words, Pictures pictures, Size imageSize)
		{
			Delimiter delimiterR;
			if ((delimiter.Right == imageSize.Width) || ((delimiter.AdjacentD2 != null) && this.Contains(delimiter.AdjacentD2)))
			{
				return true;
			}
			delimiter.AdjacentD2 = null;
			Point pointR = this.FindRightDelimiter(delimiter.RightPoint, delimiter.Angle, symbols, words, pictures, imageSize, 0, imageSize.Height, out delimiterR);
			if ((delimiterR != null) || (pointR.X == imageSize.Width))
			{
				delimiter.AdjacentD2 = delimiterR;
				delimiter.RightPoint = pointR;
				return true;
			}
			for (int x = delimiter.Right; (x < pointR.X) && (x < imageSize.Width); x += 5)
			{
				Point p = new Point(x, (int)delimiter.GetY(x));
				if ((((p.X >= 0) && (p.X <= imageSize.Width)) && (p.Y >= 0)) && (p.Y <= imageSize.Height))
				{
					Delimiter d1;
					Delimiter d2;
					Point pT = this.FindTopDelimiter(new Point(p.X, p.Y - 2), delimiter.Angle + 1.5707963267948966, symbols, words, pictures, imageSize, delimiter.X, delimiter.Right, out d1);
					Point pB = this.FindBottomDelimiter(new Point(p.X, p.Y + 2), delimiter.Angle + 1.5707963267948966, symbols, words, pictures, imageSize, delimiter.X, delimiter.Right, out d2);
					if (((d1 != null) || (pT.Y == 0)) && ((d2 != null) || (pB.Y == imageSize.Height)))
					{
						delimiter.AdjacentD2 = new Delimiter(pT, pB, Delimiter.Type.Vertical);
						delimiter.RightPoint = p;
						return true;
					}
				}
			}
			return false;
		}
		#endregion

		#region SetBottomOfVerticalDelimiter()
		private bool SetBottomOfVerticalDelimiter(Delimiter delimiter, Symbols symbols, Words words, Pictures pictures, Size imageSize)
		{
			Delimiter delimiterB;
			
			if ((delimiter.Bottom == imageSize.Height) || ((delimiter.AdjacentD2 != null) && this.Contains(delimiter.AdjacentD2)))
			{
				return true;
			}
			
			delimiter.AdjacentD2 = null;
			Point pointB = this.FindBottomDelimiter(delimiter.BottomPoint, delimiter.Angle, symbols, words, pictures, imageSize, 0, imageSize.Width, out delimiterB);
			
			if ((delimiterB != null) || (pointB.Y == imageSize.Height))
			{
				delimiter.AdjacentD2 = delimiterB;
				delimiter.BottomPoint = pointB;
				return true;
			}
			
			for (int y = delimiter.Bottom; (y < pointB.Y) && (y < imageSize.Height); y += 5)
			{
				Point p = new Point((int)delimiter.GetX(y), y);
				if ((((p.X >= 0) && (p.X <= imageSize.Width)) && (p.Y >= 0)) && (p.Y <= imageSize.Height))
				{
					Delimiter d1;
					Delimiter d2;
					Point pL = this.FindLeftDelimiter(new Point(p.X - 2, p.Y), delimiter.Angle + 1.5707963267948966, symbols, words, pictures, imageSize, delimiter.Y, delimiter.Bottom, out d1);
					Point pR = this.FindRightDelimiter(new Point(p.X + 2, p.Y), delimiter.Angle + 1.5707963267948966, symbols, words, pictures, imageSize, delimiter.Y, delimiter.Bottom, out d2);
					if (((d1 != null) || (pL.X == 0)) && ((d2 != null) || (pR.X == imageSize.Width)))
					{
						Delimiter newDelimiter = new Delimiter(pL, pR, Delimiter.Type.Horizontal);
						delimiter.AdjacentD2 = newDelimiter;
						delimiter.BottomPoint = p;
						if ((((d1 == null) && (d2 == null)) && (pL.X == 0)) && (pR.X == imageSize.Width))
						{
							this.Add(newDelimiter);
						}
						return true;
					}
				}
			}
			
			return false;
		}
		#endregion

		#endregion

	}

}
