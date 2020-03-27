using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.PageObjects
{
	public class Zones : List<Zone>
	{
		#region constructor
		public Zones()
		{
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Sort()
		public void Sort(Delimiters delimiters, Size imageSize)
		{
			base.Sort(new Zone.ZoneComparer(this, delimiters, imageSize));
		}
		#endregion

		#region MergeZones()
		public void MergeZones(int? columnWidth)
		{
			int count;

			do
			{
				if (columnWidth.HasValue == false)
					columnWidth = Convert.ToInt32(AverageZoneWidth());
				
				count = this.Count;

				//merge nested zones
				for (int i = this.Count - 2; i > 0; i--)
				{
					for (int j = this.Count - 1; j > i; j--)
					{
						Zone p1 = this[i];
						Zone p2 = this[j];

						if (p1.DelimiterZone == p2.DelimiterZone && (Rectangle.Intersect(p1.Rectangle, p2.Rectangle) == p1.Rectangle || Rectangle.Intersect(p1.Rectangle, p2.Rectangle) == p2.Rectangle))
						{
							this[i].Merge(this[j]);
							this.RemoveAt(i);
						}
					}
				}

				for (int i = this.Count - 2; i > 0; i--)
				{
					Zone p1 = this[i];

					for (int j = this.Count - 1; j > i; j--)
					{
						Zone p2 = this[j];

						if (p1.DelimiterZone == p2.DelimiterZone && p1.Type == p2.Type)
						{
							if (p1.Type == Zone.ZoneType.Paragraph && Arithmetic.AreInLine(p1.Rectangle, p2.Rectangle, .5))
							{
								int x = (p1.Rectangle.X < p2.Rectangle.X) ? p1.Rectangle.X : p2.Rectangle.X;
								int right = (p1.Rectangle.Right > p2.Rectangle.Right) ? p1.Rectangle.Right : p2.Rectangle.Right;

								if (right - x < columnWidth * 1.2)
								{
									//adjacent paragraph exist covering the horizontal gap 
									int gapL = (p1.Rectangle.Right < p2.Rectangle.Right) ? p1.Rectangle.Right : p2.Rectangle.Right;
									int gapR = (p1.Rectangle.X > p2.Rectangle.X) ? p1.Rectangle.X : p2.Rectangle.X;

									for (int k = 0; k < this.Count - 1; k++)
									{
										if (k != i && k != j)
										{
											Zone p3 = this[k];

											if (p3.Rectangle.X <= gapL && p3.Rectangle.Right >= gapR && p3.DelimiterZone == p1.DelimiterZone)
											{
												this[i].Merge(this[j]);
												this.RemoveAt(j);
												break;
											}
										}
									}
								}
							}
							else if (p1.Type == Zone.ZoneType.Picture && Rectangle.Intersect(p1.Rectangle, p2.Rectangle) != Rectangle.Empty)
							{
								//adjacent paragraph exist covering the horizontal gap 
								int gapL = (p1.Rectangle.Right < p2.Rectangle.Right) ? p1.Rectangle.Right : p2.Rectangle.Right;
								int gapR = (p1.Rectangle.X > p2.Rectangle.X) ? p1.Rectangle.X : p2.Rectangle.X;

								for (int k = 0; k < this.Count - 1; k++)
								{
									if (k != i && k != j)
									{
										Zone p3 = this[k];

										if (p3.Rectangle.X <= gapL && p3.Rectangle.Right >= gapR && p3.DelimiterZone == p1.DelimiterZone)
										{
											this[i].Merge(this[j]);
											this.RemoveAt(j);
											break;
										}
									}
								}
							}
						}
					}
				}
			} while (this.Count != count);
		}
		#endregion

		#region Inflate()
		public void Inflate(int margin, Size imageSize)
		{
			Rectangle imageRect = new Rectangle(0, 0, imageSize.Width, imageSize.Height);

			foreach (Zone zone in this)
				zone.Rectangle = Rectangle.Intersect(Rectangle.Inflate(zone.Rectangle, margin, margin), imageRect); 
		}
		#endregion

		#region DrawToFile()
		public void DrawToFile(string filePath, Bitmap original)
		{
#if SAVE_RESULTS
			try
			{
				Bitmap result = ImageCopier.Get24Bpp(original);
				int counter = 0;
				Graphics g = Graphics.FromImage(result);

				foreach (Zone zone in this)
				{
					Color color = Debug.GetColor(counter++);
					g.FillRectangle(new SolidBrush(Color.FromArgb(100, color)), zone.Rectangle);
					AddText(result, zone.Rectangle, counter.ToString());
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

		#region DrawToImage()
		public Bitmap DrawToImage(Bitmap original)
		{
#if SAVE_RESULTS
			try
			{
				Bitmap bitmap = ImageCopier.Get24Bpp(original);
				int counter = 0;
				Graphics g = Graphics.FromImage(bitmap);

				foreach (Zone zone in this)
				{
					Color color = Debug.GetColor(counter++);

					g.FillRectangle(new SolidBrush(Color.FromArgb(100, color)), zone.Rectangle);
					AddText(bitmap, zone.Rectangle, counter.ToString());
				}

				return bitmap;
			}
			catch 
			{ 
				return null; 
			}
			finally
			{
				GC.Collect();
			}
#else
			return null;
#endif
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region AddText()
		private static void AddText(Bitmap bitmap, Rectangle clip, string text)
		{
			Graphics g = Graphics.FromImage(bitmap);
			StringFormat stringFormat = new StringFormat();

			stringFormat.Alignment = StringAlignment.Center;
			stringFormat.LineAlignment = StringAlignment.Center;

			g.DrawRectangle(new Pen(Color.Red, 6), new Rectangle(clip.X + clip.Width / 2 - 60, clip.Y + clip.Height / 2 - 40, 120, 80));
			g.FillRectangle(new SolidBrush(Color.White), new Rectangle(clip.X + clip.Width / 2 - 60, clip.Y + clip.Height / 2 - 40, 120, 80));
			g.DrawString(text, new Font("Arial", 3200 / bitmap.HorizontalResolution), new SolidBrush(Color.Black), clip, stringFormat);
			g.Flush();
		}
		#endregion

		#region AverageZoneWidth()
		private double AverageZoneWidth()
		{
			double area = 0;
			double height = 0;

			foreach (Zone zone in this)
			{
				area += zone.Rectangle.Width * zone.Rectangle.Height;
				height += zone.Rectangle.Height;
			}

			return area / height;
		}
		#endregion

		#endregion
	}
}
