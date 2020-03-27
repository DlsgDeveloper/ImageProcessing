using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ImageProcessing.PageObjects
{
	public class Zone
	{
		public Rectangle Rectangle;
		public ZoneType Type;
		public int WordsCount;
		public readonly DelimiterZone DelimiterZone;

		#region constructor
		public Zone(Paragraph paragraph, Rectangle rect)
		{
			Rectangle = rect;
			Type = ZoneType.Paragraph;
			WordsCount = paragraph.Words.Count;
			this.DelimiterZone = paragraph.Zone;
		}

		public Zone(Picture picture, Rectangle rect)
		{
			Rectangle = rect;
			Type = ZoneType.Picture;
			WordsCount = 0;
			this.DelimiterZone = picture.Zone;
		}

		public Zone(Rectangle rect)
		{
			Rectangle = rect;
			Type = ZoneType.EmptyZone;
			WordsCount = 0;
			this.DelimiterZone = null;
		}
		#endregion

		#region class ZoneComparer
		public class ZoneComparer : IComparer<Zone>
		{
			Zones zones;
			Delimiters delimiters;
			Size imageSize;

			public ZoneComparer(Zones zones, Delimiters delimiters, Size imageSize)
			{
				this.zones = zones;
				this.delimiters = delimiters;
				this.imageSize = imageSize;
			}

			public int Compare(Zone zone1, Zone zone2)
			{
				if (zone1.DelimiterZone == zone2.DelimiterZone)
				{
					Rectangle r1 = zone1.Rectangle;
					Rectangle r2 = zone2.Rectangle;

					int sharedWidth = Math.Min(r1.Right, r2.Right) - Math.Max(r1.X, r2.X);
					int narrowerRect = Math.Min(r1.Width, r2.Width);

					if (sharedWidth > narrowerRect * .2)
					{
						//zones are 1 above and 1 below
						if (r1.Y > r2.Y)
							return 1;
						else if (r1.Y < r2.Y)
							return -1;
						else
							return 0;
					}
					else
					{
						//if there is a zone in between them that covers both widths, top one is first, left otherwise
						if (Arithmetic.AreInLine(zone1.Y, zone1.Rectangle.Bottom, zone2.Y, zone2.Rectangle.Bottom))
						{
							if (r1.X > r2.X)
								return 1;
							else if (r1.X < r2.X)
								return -1;
							else
								return 0;
						}
						else
						{
							if (r1.X < r2.X && r1.Y < r2.Y)
								return -1;
							else if (r1.X > r2.X && r1.Y > r2.Y)
								return 1;
							else
							{
								//try to find zone, that lays between zones and covers both widths 
								foreach (Zone zone in this.zones)
								{
									Rectangle r = zone.Rectangle;
									Rectangle rL = (r1.X < r2.X) ? r1 : r2;
									Rectangle rR = (r1.X > r2.X) ? r1 : r2;

									if (zone != zone1 && zone != zone2 && r.Y > Math.Min(r1.Y, r2.Y) && r.Bottom < Math.Max(r1.Bottom, r2.Bottom))
									{
										if ((r.X < rL.X + rL.Width / 2) && (r.Right > rR.X + rR.Width / 2))
										{
											//right zone
											if (r1.X < r2.X)
												return 1;
											else
												return -1;
										}
									}
								}

								if (r1.X > r2.X)
									return 1;
								else if (r1.X < r2.X)
									return -1;
								else
									return 0;
							}
						}				
					}
				}
				else
				{
					//if there is a horizontal delimiter between zones, the top one is 
					foreach (Delimiter delimiter in this.delimiters)
					{
						if (delimiter.IsHorizontal && delimiter.X == 0 && delimiter.Right == imageSize.Width)
						{
							int delimiterMiddleY = delimiter.Y + delimiter.Height / 2;

							if (zone1.CenterPoint.Y < delimiterMiddleY && zone2.CenterPoint.Y > delimiterMiddleY)
								return -1;
							if (zone1.CenterPoint.Y > delimiterMiddleY && zone2.CenterPoint.Y < delimiterMiddleY)
								return 1;
						}
					}

					Rectangle r1, r2;

					if (zone1.DelimiterZone != null)
						r1 = Rectangle.FromLTRB(zone1.DelimiterZone.Pul.X, zone1.DelimiterZone.Pul.Y, zone1.DelimiterZone.Plr.X, zone1.DelimiterZone.Plr.Y);
					else
						r1 = zone1.Rectangle;
					if (zone2.DelimiterZone != null)
						r2 = Rectangle.FromLTRB(zone2.DelimiterZone.Pul.X, zone2.DelimiterZone.Pul.Y, zone2.DelimiterZone.Plr.X, zone2.DelimiterZone.Plr.Y);
					else
						r2 = zone2.Rectangle;

					int sharedWidth = Math.Min(r1.Right, r2.Right) - Math.Max(r1.X, r2.X);
					int narrowerRect = Math.Min(r1.Width, r2.Width);

					if (sharedWidth > narrowerRect * .2)
					{
						//zones are 1 above and 1 below
						if (zone1.Rectangle.Y > zone2.Rectangle.Y)
							return 1;
						else if (zone1.Rectangle.Y < zone2.Rectangle.Y)
							return -1;
						else
							return 0;
					}
					else
					{
						if (r1.X > r2.X)
							return 1;
						else if (r1.X < r2.X)
							return -1;
						else
							return 0;
					}
				}
			}
		}
		#endregion

		#region enum ZoneType
		public enum ZoneType
		{
			Paragraph,
			Picture,
			EmptyZone
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties
		public Point	CenterPoint { get { return new Point(Rectangle.X + Rectangle.Width / 2, Rectangle.Y + Rectangle.Height / 2); } }
		public int		X { get { return Rectangle.X; } }
		public int		Y { get { return Rectangle.Y; } }
		public int		Width { get { return Rectangle.Width; } }
		public int		Height { get { return Rectangle.Height; } }
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Merge()
		public void Merge(Zone zone)
		{
			this.Type = (this.Type == ZoneType.Picture || zone.Type == ZoneType.Picture) ? ZoneType.Picture : ZoneType.Paragraph;

			this.Rectangle = Rectangle.FromLTRB(Math.Min(this.Rectangle.X, zone.Rectangle.X), Math.Min(this.Rectangle.Y, zone.Rectangle.Y),
				Math.Max(this.Rectangle.Right, zone.Rectangle.Right), Math.Max(this.Rectangle.Bottom, zone.Rectangle.Bottom));

			this.WordsCount += zone.WordsCount;
		}
		#endregion

		#endregion

	}
}
