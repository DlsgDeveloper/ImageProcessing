using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace ImageProcessing.PageObjects
{
	public class DelimiterZones : List<DelimiterZone>
	{
		#region constructor
		public DelimiterZones()
		{
		}

		public DelimiterZones(Delimiters delimiters, Size imageSize)
		{
			Delimiters delimitersToProcess = new Delimiters();
			int delimitersCount;

			foreach (Delimiter delimiter in delimiters)
				delimitersToProcess.Add(delimiter);

			Add(new DelimiterZone(new Point(0, 0), new Point(imageSize.Width, 0), new Point(0, imageSize.Height), new Point(imageSize.Width, imageSize.Height)));

			do
			{
				delimitersCount = delimitersToProcess.Count;

				for (int i = delimitersToProcess.Count - 1; i >= 0; i--)
				{
					Delimiter delimiter = delimitersToProcess[i];

					if ((delimiter.AdjacentD1 == null || delimitersToProcess.Contains(delimiter.AdjacentD1) == false) &&
						(delimiter.AdjacentD2 == null || delimitersToProcess.Contains(delimiter.AdjacentD2) == false))
					{
						bool change;
						
						do
						{
							change = false;

							for (int j = this.Count - 1; j >= 0; j--)
							{
								DelimiterZone zone = this[j];
								Point p1 = Point.Empty, p2 = Point.Empty;

								if (DelimiterSplitsZone(zone, delimiter, ref p1, ref p2))
								{
									change = true;

									if (delimiter.IsHorizontal)
									{
										DelimiterZone zone1 = new DelimiterZone(zone.Pul, zone.Pur, p1, p2);
										DelimiterZone zone2 = new DelimiterZone(p1, p2, zone.Pll, zone.Plr);
										this.RemoveAt(j);
										this.Add(zone1);
										this.Add(zone2);
										break;
									}
									else
									{
										DelimiterZone zone1 = new DelimiterZone(zone.Pul, p1, zone.Pll, p2);
										DelimiterZone zone2 = new DelimiterZone(p1, zone.Pur, p2, zone.Plr);
										this.RemoveAt(j);
										this.Add(zone1);
										this.Add(zone2);
										break;
									}
								}
							}
						} while (change);

						delimitersToProcess.RemoveAt(i);
					}
				}
			} while (delimitersCount != delimitersToProcess.Count);

		}
		#endregion

		#region this[]
		/*new public DelimiterZone this[int index]
		{
			get { return (DelimiterZone) base[index]; }
		}*/
		#endregion

		#region Split()
		/*public void Split(Delimiters delimiters)
		{
			bool change = true;

			do
			{
				change = false;

				for (int i = this.Count - 1; i >= 0; i--)
				{
					DelimiterZone	zone = this[i];
					Point			p1 = Point.Empty, p2 = Point.Empty;

					foreach (Delimiter delimiter in delimiters)
					{
						if (DelimiterSplitsZone(zone, delimiter, ref p1, ref p2))
						{
							change = true;

							if (delimiter.IsHorizontal)
							{
								DelimiterZone zone1 = new DelimiterZone(zone.Pul, zone.Pur, p1, p2);
								DelimiterZone zone2 = new DelimiterZone(p1, p2, zone.Pll, zone.Plr);
								this.RemoveAt(i);
								this.Add(zone1);
								this.Add(zone2);
								break;
							}
							else
							{
								DelimiterZone zone1 = new DelimiterZone(zone.Pul, p1, zone.Pll, p2);
								DelimiterZone zone2 = new DelimiterZone(p1, zone.Pur, p2, zone.Plr);
								this.RemoveAt(i);
								this.Add(zone1);
								this.Add(zone2);
								break;
							}
						}
					}
				}
			} while (change);

			RemoveDuplicates();
		}*/
		#endregion

		#region GetZone()
		public DelimiterZone GetZone(Rectangle rect)
		{
			DelimiterZone	bestZone = null;
			double			biggestIntersection = 0;

			foreach (DelimiterZone zone in this)
			{
				double intersection = zone.SharedAreaInPercent(rect);

				if (biggestIntersection < intersection)
				{
					biggestIntersection = intersection;
					bestZone = zone;
				}
				else if ((biggestIntersection > 0) && (biggestIntersection == intersection) && (bestZone == null || bestZone.Area > zone.Area))
				{
					bestZone = zone;
				}
			}

			return bestZone;
		}
		#endregion

		#region DrawToFile()
		public void DrawToFile(string filePath, Size imageSize)
		{
#if SAVE_RESULTS
			try
			{
				Bitmap result = Debug.GetBitmap(imageSize);
				int counter = 0;
				Graphics g = Graphics.FromImage(result);

				foreach (DelimiterZone delimiterZone in this)
				{
					Color color = Debug.GetColor(counter++);
					GraphicsPath path = delimiterZone.PathToDraw;

					g.FillPath(new SolidBrush(Color.FromArgb(150, color)), path);
					//g.DrawLine(new Pen(Color.Yellow, 1), delimiterZone.P1, delimiterZone.P2);
				}

				result.Save(filePath, ImageFormat.Png);
				result.Dispose();
			}
			catch { }
			finally
			{
				GC.Collect();
			}
#endif
		}
		#endregion

		//PRIVATE METHODS
		#region private methods
		
		#region DelimiterSplitsZone()
		private bool DelimiterSplitsZone(DelimiterZone zone, Delimiter delimiter, ref Point p1, ref Point p2)
		{
			Line2D delimiterLine = new Line2D(delimiter.P1, delimiter.P2);
			double x = 0;
			double y = 0;
			Line2D zoneLine;
			bool interceptPointExists;

			if (delimiter.IsHorizontal)
			{
				zoneLine = new Line2D(zone.Pul, zone.Pll);
				interceptPointExists = delimiterLine.InterceptPoint(zoneLine, ref x, ref y);

				if ((interceptPointExists == false) || (x <= delimiter.LeftPoint.X - 2) || (x >= delimiter.RightPoint.X + 2) || (y <= zone.Pul.Y + 2) || (y >= zone.Pll.Y - 2))
						return false;

				p1 = new Point((int)x, (int)y);

				zoneLine = new Line2D(zone.Pur, zone.Plr);
				interceptPointExists = delimiterLine.InterceptPoint(zoneLine, ref x, ref y);
				if ((interceptPointExists == false) || (x <= delimiter.LeftPoint.X - 2) ||
					(x >= delimiter.RightPoint.X + 2) || (y <= zone.Pur.Y + 2) || (y >= zone.Plr.Y - 2))
					return false;

				p2 = new Point((int)x, (int)y);
				return true;
			}
			else
			{
				zoneLine = new Line2D(zone.Pul, zone.Pur);
				interceptPointExists = delimiterLine.InterceptPoint(zoneLine, ref x, ref y);
				
				if ((interceptPointExists == false) || (y <= delimiter.TopPoint.Y - 2) ||
					(y >= delimiter.BottomPoint.Y + 2) || (x <= zone.Pul.X + 2) || (x >= zone.Pur.X - 2))
					return false;

				p1 = new Point((int)x, (int)y);

				zoneLine = new Line2D(zone.Pll, zone.Plr);
				interceptPointExists = delimiterLine.InterceptPoint(zoneLine, ref x, ref y);
				if ((interceptPointExists == false) || (y <= delimiter.TopPoint.Y - 2) ||
					(y >= delimiter.BottomPoint.Y + 2) || (x <= zone.Pll.X + 2) || (x > zone.Plr.X - 2))
					return false;

				p2 = new Point((int)x, (int)y);
				return true;
			}
		}
		#endregion

		#region RemoveDuplicates()
		private void RemoveDuplicates()
		{
			for (int i = this.Count - 2; i > 0; i--)
				for (int j = this.Count - 1; j > i; j--)
					if (DelimiterZone.AreIdentical(this[i], this[j]))
						this.RemoveAt(j);
		}
		#endregion

		#endregion



	}
}
