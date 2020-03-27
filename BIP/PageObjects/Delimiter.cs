using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace ImageProcessing.PageObjects
{
	public class Delimiter : IPageObject
	{
		// Fields
		private Delimiter adjacentDelimiter1 = null;
		private Delimiter adjacentDelimiter2 = null;
		private Rectangle rectangle;
		private Point p1;
		private Point p2;
		private Type type;
		private ObjectMap		objectMap;
		private ObjectShape		objectShape = null;
		private ConvexEnvelope	convexEnvelope = null;
		private bool isVirtual = false;

		#region construction
		public Delimiter(Symbol symbol)
		{
			this.type = (symbol.Width > symbol.Height) ? Type.Horizontal : Type.Vertical;
			this.rectangle = symbol.Rectangle;

			objectMap = symbol.ObjectMap;

			ComputeShape();

			Crop crop = objectShape.GetCrop();
			if (this.type == Type.Horizontal)
				this.SetPoints(crop.Left, crop.Right);
			else
				this.SetPoints(crop.Top, crop.Bottom);
		}

		public Delimiter(Point p1, Point p2, Delimiter.Type delimiterType)
		{
			this.type = delimiterType;
			this.SetPoints(p1, p2);
			this.rectangle = Rectangle.FromLTRB(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y));
			this.isVirtual = true;

			this.objectMap = GetObjectMap(p1, p2);
			ComputeShape();
		}
		#endregion

		#region enum Type
		[Flags]
		public enum Type : ushort
		{
			Horizontal = 1,
			Vertical = 2
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		public Point	P1		{ get { return this.p1; } set { this.p1 = value; } }
		public Point	P2		{ get { return this.p2; } set { this.p2 = value; } }
		public Rectangle Rectangle { get { return this.rectangle; } }
		public Point	Location { get { return this.rectangle.Location; } }
		public int		X		{ get { return this.Rectangle.X; } }
		public int		Y		{ get { return this.Rectangle.Y; } }
		public int		Width	{ get { return this.Rectangle.Width; } }
		public int		Height	{ get { return this.Rectangle.Height; } }
		public int		Right	{ get { return this.Rectangle.Right; } }
		public int		Bottom	{ get { return this.Rectangle.Bottom; } }
		public bool		IsVirtual { get { return this.isVirtual; } }

		public Type		DelimiterType	{ get { return this.type; } }
		public bool		IsHorizontal	{ get { return (this.DelimiterType == Type.Horizontal); } }
		public bool		IsVertical		{ get { return !this.IsHorizontal; } }

		public Delimiter		AdjacentD1	{ get { return this.adjacentDelimiter1; } set { this.adjacentDelimiter1 = value; } }
		public Delimiter		AdjacentD2	{ get { return this.adjacentDelimiter2; } set { this.adjacentDelimiter2 = value; } }
		public double			Length		{ get { return Math.Sqrt((double)(((this.P1.X - this.P2.X) * (this.P1.X - this.P2.X)) + ((this.P1.Y - this.P2.Y) * (this.P1.Y - this.P2.Y)))); } }
		public DelimiterZone	Zone		{ get { return null; } }

		public Point		LeftPoint { get { return (P1.X < P2.X) ? P1 : P2; } set { if (p1 == LeftPoint) p1 = value; else p2 = value; CheckRectangle(); } }
		public Point		TopPoint { get { return (P1.Y < P2.Y) ? P1 : P2; } set { if (p1 == TopPoint) p1 = value; else p2 = value; CheckRectangle(); } }
		public Point		RightPoint { get { return (P1.X > P2.X) ? P1 : P2; } set { if (p1 == RightPoint) p1 = value; else p2 = value; CheckRectangle(); } }
		public Point		BottomPoint { get { return (P1.Y > P2.Y) ? P1 : P2; } set { if (p1 == BottomPoint) p1 = value; else p2 = value; CheckRectangle(); } }

		public ObjectMap		ObjectMap { get { return this.objectMap; } }
		public ObjectShape		ObjectShape { get { return objectShape; } }
		public ConvexEnvelope	ConvexEnvelope { get { return convexEnvelope; } }

		#region Angle
		public double Angle
		{
			get
			{
				double angle;

				if (this.DelimiterType == Delimiter.Type.Horizontal)
					angle = Arithmetic.GetAngle(LeftPoint, RightPoint);
				else
					angle = Arithmetic.GetAngle(BottomPoint, TopPoint);

				while (angle < -(Math.PI / 4))
					angle += Math.PI;

				return angle;
			}
		}
		#endregion

		public bool			CurveExists { get { return this.objectShape.IsBfValid; } }
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region ComputeShape()
		private void ComputeShape()
		{
			this.objectShape = new ObjectShape(this.Location, this.objectMap);
			this.convexEnvelope = new ConvexEnvelope(this.Location, this.objectMap);
		}
		#endregion

		#region GetCurve()
		/*public Curve GetCurve(ItPage page, bool isTopCurve)
		{
			if (this.objectShape != null)
			{
				if (isTopCurve)
					return this.objectShape.GetTopCurve(page);
				else
					return this.objectShape.GetBottomCurve(page);
			}
			else
				throw new IpException(ErrorCode.ErrorNoImageLoaded, "Delimiter, GetCurve(): objectShape is not loaded!"); 
			//if (this.bfPoints != null)
			//	return new Curve(page.Clip, this.bfPoints, isTopCurve);
			//else
			//	return null;
		}*/
		#endregion

		#region GetBfPoints()
		public Point[] GetBfPoints()
		{
			return this.objectShape.GetBfPoints();
		}
		#endregion

		#region GetX()
		public double GetX(int y)
		{
			if (P1.Y == P2.Y)
				return (P1.X + P2.X) / 2.0;
			else
				return P1.X + (y - P1.Y) * (P2.X - P1.X) / (double)(P2.Y - P1.Y);
		}
		#endregion

		#region GetY()
		public double GetY(int x)
		{
			if (P1.X == P2.X)
				return (P1.Y + P2.Y) / 2.0;
			else
				return P1.Y + (P2.Y - P1.Y) * (x - P1.X) / (double)(P2.X - P1.X);
		}
		#endregion

		#region GetZones()
		/*public DelimiterZones GetZones(Symbols symbols, Size imageSize)
		{
			Point pLL;
			Point pLR;
			Point pUL;
			Point pUR;
			DelimiterZone zone;
			double angle;
			int topDistanceX;
			int bottomDistanceX;
			DelimiterZones zones = new DelimiterZones();

			if (this.IsHorizontal)
			{
				int leftDistanceY;
				int rightDistanceY;
				if ((((this.AdjacentD1 == null) && (this.LeftPoint.X == 0)) && (this.AdjacentD2 == null)) && (this.RightPoint.X == imageSize.Width))
				{
					zones.Add(new DelimiterZone(new Point(0, 0), new Point(imageSize.Width, 0), this.LeftPoint, this.RightPoint));
					zones.Add(new DelimiterZone(this.LeftPoint, this.RightPoint, new Point(0, imageSize.Height), new Point(imageSize.Width, imageSize.Height)));
					return zones;
				}
				if (((this.AdjacentD1 != null) && (this.AdjacentD1.TopPoint.Y < (this.LeftPoint.Y - 50))) || ((this.AdjacentD2 != null) && (this.AdjacentD2.TopPoint.Y < (this.RightPoint.Y - 50))))
				{
					pLL = this.LeftPoint;
					pLR = this.RightPoint;

					if ((this.AdjacentD1 != null) && (this.AdjacentD1.TopPoint.Y < (this.LeftPoint.Y - 50)))
					{
						pUL = this.AdjacentD1.TopPoint;
						if ((this.AdjacentD2 != null) && (this.AdjacentD2.TopPoint.Y < (this.RightPoint.Y - 50)))
						{
							pUR = this.AdjacentD2.TopPoint;
						}
						else if (this.AdjacentD1.AdjacentD1 != null)
						{
							pUR = this.AdjacentD1.AdjacentD1.RightPoint;
						}
						else
						{
							pUR = new Point(pUL.X + (pLR.X - pLL.X), pUL.Y + (pLR.Y - pLL.Y));
						}
					}
					else
					{
						pUR = this.AdjacentD2.TopPoint;
						if (this.AdjacentD2.AdjacentD1 != null)
						{
							pUL = this.AdjacentD2.AdjacentD1.LeftPoint;
						}
						else
						{
							pUL = new Point(pUR.X - (pLR.X - pLL.X), pUR.Y - (pLR.Y - pLL.Y));
						}
					}

					leftDistanceY = pLL.Y - pUL.Y;
					rightDistanceY = pLR.Y - pUR.Y;
					
					if ((leftDistanceY - rightDistanceY) > 50)
					{
						pUL = new Point(pLL.X - (pLR.X - pUR.X), pLL.Y - (pLR.Y - pUR.Y));
					}
					else if ((rightDistanceY - leftDistanceY) > 50)
					{
						pUR = new Point(pLR.X - (pLL.X - pUL.X), pLR.Y - (pLL.Y - pUL.Y));
					}
					
					this.KeepPointsInsideImage(imageSize, ref pUL, ref pUR, ref pLL, ref pLR);
					zone = new DelimiterZone(pUL, pUR, pLL, pLR);
					zones.Add(zone);
				}
				if (((this.AdjacentD1 != null) && (this.AdjacentD1.BottomPoint.Y > (this.LeftPoint.Y + 50))) || ((this.AdjacentD2 != null) && (this.AdjacentD2.BottomPoint.Y > (this.RightPoint.Y + 50))))
				{
					pUL = this.LeftPoint;
					pUR = this.RightPoint;
					
					if ((this.AdjacentD1 != null) && (this.AdjacentD1.BottomPoint.Y > (this.LeftPoint.Y + 50)))
					{
						pLL = this.AdjacentD1.BottomPoint;
						if ((this.AdjacentD2 != null) && (this.AdjacentD2.BottomPoint.Y > (this.RightPoint.Y + 50)))
						{
							pLR = this.AdjacentD2.BottomPoint;
						}
						else if (this.AdjacentD1.AdjacentD2 != null)
						{
							pLR = this.AdjacentD1.AdjacentD2.RightPoint;
						}
						else
						{
							pLR = new Point(pLL.X + (pUR.X - pUL.X), pLL.Y + (pUR.Y - pUL.Y));
						}
					}
					else
					{
						pLR = this.AdjacentD2.BottomPoint;
						if (this.AdjacentD2.AdjacentD2 != null)
						{
							pLL = this.AdjacentD2.AdjacentD2.LeftPoint;
						}
						else
						{
							pLL = new Point(pLR.X - (pUR.X - pUL.X), pLR.Y - (pUR.Y - pUL.Y));
						}
					}
					
					leftDistanceY = pLL.Y - pUL.Y;
					rightDistanceY = pLR.Y - pUR.Y;
					
					if ((leftDistanceY - rightDistanceY) > 50)
					{
						pLL = new Point(pUL.X + (pLR.X - pUR.X), pUL.Y + (pLR.Y - pUR.Y));
					}
					else if ((rightDistanceY - leftDistanceY) > 50)
					{
						pLR = new Point(pUR.X + (pLL.X - pUL.X), pUR.Y + (pLL.Y - pUL.Y));
					}
					
					this.KeepPointsInsideImage(imageSize, ref pUL, ref pUR, ref pLL, ref pLR);
					zone = new DelimiterZone(pUL, pUR, pLL, pLR);
					zones.Add(zone);
				}
				if ((zones.Count == 0) && (((this.AdjacentD1 == null) && (this.AdjacentD2 == null)) && ((this.X == 0) || (this.Right == imageSize.Width))))
				{
					int xL;
					int xR;
					angle = Math.Atan2((double)(this.RightPoint.Y - this.LeftPoint.Y), (double)(this.RightPoint.X - this.LeftPoint.X));
					
					if (this.X == 0)
					{
						pUL = new Point(0, 0);
						pUR = new Point(imageSize.Width, 0);
					}
					else
					{
						xL = Convert.ToInt32((double)(this.LeftPoint.X - (Math.Tan(angle) * this.LeftPoint.Y)));
						xR = Convert.ToInt32((double)(this.RightPoint.X - (Math.Tan(angle) * this.RightPoint.Y)));
						pUL = new Point(xL, 0);
						pUR = new Point(xR, 0);
					}
					
					if (this.X == imageSize.Width)
					{
						pLL = new Point(0, imageSize.Height);
						pLR = new Point(imageSize.Width, imageSize.Height);
					}
					else
					{
						xL = Convert.ToInt32((double)(this.LeftPoint.X + (Math.Tan(angle) * (imageSize.Height - this.LeftPoint.Y))));
						xR = Convert.ToInt32((double)(this.RightPoint.X + (Math.Tan(angle) * (imageSize.Height - this.RightPoint.Y))));
						pLL = new Point(xL, imageSize.Height);
						pLR = new Point(xR, imageSize.Height);
					}
					
					this.KeepPointsInsideImage(imageSize, ref pUL, ref pUR, ref pLL, ref pLR);
					zones.Add(new DelimiterZone(pUL, pUR, this.LeftPoint, this.RightPoint));
					zones.Add(new DelimiterZone(this.LeftPoint, this.RightPoint, pLL, pLR));
				}
				return zones;
			}
			if ((((this.AdjacentD1 == null) && (this.TopPoint.Y == 0)) && (this.AdjacentD2 == null)) && (this.BottomPoint.Y == imageSize.Height))
			{
				zones.Add(new DelimiterZone(new Point(0, 0), this.TopPoint, new Point(0, imageSize.Height), this.BottomPoint));
				zones.Add(new DelimiterZone(this.TopPoint, new Point(imageSize.Width, 0), this.BottomPoint, new Point(imageSize.Width, imageSize.Height)));
				return zones;
			}
			if (((this.AdjacentD1 != null) && (this.AdjacentD1.LeftPoint.X < (this.TopPoint.X - 50))) || ((this.AdjacentD2 != null) && (this.AdjacentD2.LeftPoint.X < (this.BottomPoint.X - 50))))
			{
				pUR = this.TopPoint;
				pLR = this.BottomPoint;
				if ((this.AdjacentD1 != null) && (this.AdjacentD1.LeftPoint.X < (this.TopPoint.X - 50)))
				{
					pUL = this.AdjacentD1.LeftPoint;
					
					if ((this.AdjacentD2 != null) && (this.AdjacentD2.LeftPoint.X < (this.BottomPoint.X - 50)))
					{
						pLL = this.AdjacentD2.LeftPoint;
					}
					else if (this.AdjacentD1.AdjacentD1 != null)
					{
						pLL = this.AdjacentD1.AdjacentD1.BottomPoint;
					}
					else
					{
						pLL = new Point(pUL.X - (pUR.X - pLR.X), pUL.Y - (pUR.Y - pLR.Y));
					}
				}
				else
				{
					pLL = this.AdjacentD2.LeftPoint;
					if (this.AdjacentD2.AdjacentD1 != null)
					{
						pUL = this.AdjacentD2.AdjacentD1.TopPoint;
					}
					else
					{
						pUL = new Point(pLL.X - (pLR.X - pUR.X), pLL.Y - (pLR.Y - pUR.Y));
					}
				}
				
				topDistanceX = pUR.X - pUL.X;
				bottomDistanceX = pLR.X - pLL.X;
				
				if ((topDistanceX - bottomDistanceX) > 50)
				{
					pUL = new Point(pLL.X - (pLR.X - pUR.X), pLL.Y - (pLR.Y - pUR.Y));
				}
				else if ((bottomDistanceX - topDistanceX) > 50)
				{
					pLL = new Point(pUL.X - (pUR.X - pLR.X), pUL.Y - (pUR.Y - pLR.Y));
				}
				
				this.KeepPointsInsideImage(imageSize, ref pUL, ref pUR, ref pLL, ref pLR);
				zone = new DelimiterZone(pUL, pUR, pLL, pLR);
				zones.Add(zone);
			}
			if (((this.AdjacentD1 != null) && (this.AdjacentD1.RightPoint.X > (this.TopPoint.X + 50))) || ((this.AdjacentD2 != null) && (this.AdjacentD2.RightPoint.X > (this.BottomPoint.X + 50))))
			{
				pUL = this.TopPoint;
				pLL = this.BottomPoint;
				
				if ((this.AdjacentD1 != null) && (this.AdjacentD1.RightPoint.X > (this.TopPoint.X + 50)))
				{
					pUR = this.AdjacentD1.RightPoint;
					if ((this.AdjacentD2 != null) && (this.AdjacentD2.RightPoint.X > (this.BottomPoint.X + 50)))
					{
						pLR = this.AdjacentD2.RightPoint;
					}
					else if (this.AdjacentD1.AdjacentD2 != null)
					{
						pLR = this.AdjacentD1.AdjacentD2.BottomPoint;
					}
					else
					{
						pLR = new Point(pUR.X - (pUL.X - pLL.X), pUR.Y - (pUL.Y - pLL.Y));
					}
				}
				else
				{
					pLR = this.AdjacentD2.RightPoint;
					if (this.AdjacentD2.AdjacentD2 != null)
					{
						pUR = this.AdjacentD2.AdjacentD2.TopPoint;
					}
					else
					{
						pUR = new Point(pLR.X - (pLL.X - pUL.X), pLR.Y - (pLL.Y - pUL.Y));
					}
				}
			
				topDistanceX = pUR.X - pUL.X;
				bottomDistanceX = pLR.X - pLL.X;
				
				if ((topDistanceX - bottomDistanceX) > 50)
				{
					pUR = new Point(pLR.X - (pLL.X - pUL.X), pLR.Y - (pLL.Y - pUL.Y));
				}
				else if ((bottomDistanceX - topDistanceX) > 50)
				{
					pLR = new Point(pUR.X - (pUL.X - pLL.X), pUR.Y - (pUL.Y - pLL.Y));
				}
				
				this.KeepPointsInsideImage(imageSize, ref pUL, ref pUR, ref pLL, ref pLR);
				zone = new DelimiterZone(pUL, pUR, pLL, pLR);
				zones.Add(zone);
			}
			if ((zones.Count == 0) && (((this.AdjacentD1 == null) && (this.AdjacentD2 == null)) && ((this.Y == 0) || (this.Bottom == imageSize.Height))))
			{
				int yL;
				int yR;
				angle = Math.Atan2((double)(this.BottomPoint.X - this.TopPoint.X), (double)(this.BottomPoint.Y - this.TopPoint.Y));
				
				if (this.Y == 0)
				{
					pUL = new Point(0, 0);
					pUR = new Point(imageSize.Width, 0);
				}
				else
				{
					yL = Convert.ToInt32((double)(this.TopPoint.Y + (Math.Tan(angle) * this.TopPoint.X)));
					yR = Convert.ToInt32((double)(this.TopPoint.Y - (Math.Tan(angle) * (imageSize.Width - this.TopPoint.X))));
					pUL = new Point(0, yL);
					pUR = new Point(imageSize.Width, yR);
				}
				
				if (this.Bottom == imageSize.Height)
				{
					pLL = new Point(0, imageSize.Height);
					pLR = new Point(imageSize.Width, imageSize.Height);
				}
				else
				{
					yL = Convert.ToInt32((double)(this.BottomPoint.Y + (Math.Tan(angle) * this.BottomPoint.X)));
					yR = Convert.ToInt32((double)(this.BottomPoint.Y - (Math.Tan(angle) * (imageSize.Width - this.BottomPoint.X))));
					pLL = new Point(0, yL);
					pLR = new Point(imageSize.Width, yR);
				}
				
				this.KeepPointsInsideImage(imageSize, ref pUL, ref pUR, ref pLL, ref pLR);
				zones.Add(new DelimiterZone(pUL, this.TopPoint, pLL, this.BottomPoint));
				zones.Add(new DelimiterZone(this.TopPoint, pUR, this.BottomPoint, pLR));
			}
			return zones;
		}*/
		#endregion

		#region Shift()
		/*public void Shift(int dx, int dy)
		{
			this.p1.Offset(dx, dy);
			this.p2.Offset(dx, dy);
		}*/
		#endregion

		#region AddSymbol()
		public void AddSymbol(Symbol symbol)
		{
			this.rectangle = Rectangle.Union(this.rectangle, symbol.Rectangle);
			this.objectMap.Merge(symbol.ObjectMap);
			ComputeShape();

			Crop crop = objectShape.GetCrop();
			if (this.type == Type.Horizontal)
				this.SetPoints(crop.Left, crop.Right);
			else
				this.SetPoints(crop.Top, crop.Bottom);
		}
		#endregion

		#region Merge()
		public void Merge(Delimiter d)
		{
			if (IsHorizontal)
				SetPoints((this.LeftPoint.X < d.LeftPoint.X) ? this.LeftPoint : d.LeftPoint, (this.RightPoint.X > d.RightPoint.X) ? this.RightPoint : d.RightPoint);
			else
				SetPoints((this.TopPoint.Y < d.TopPoint.Y) ? this.TopPoint : d.TopPoint, (this.BottomPoint.Y > d.BottomPoint.Y) ? this.BottomPoint : d.BottomPoint);

			this.rectangle = Rectangle.Union(this.rectangle, d.Rectangle);
			this.objectMap.Merge(d.objectMap);
			ComputeShape();
		}
		#endregion

		#region ShouldContain()
		public bool ShouldContain(Symbol symbol)
		{			
			if (this.IsHorizontal)
			{
				if(symbol.X >= this.X && symbol.Right <= this.Right && this.ObjectShape.MaxPixelHeight >= symbol.ObjectShape.MaxPixelHeight)
				{
					ObjectShape symbolObjectShape = symbol.ObjectShape;
					
					for (int x = symbol.X - this.X; x < symbol.Right - this.X; x++)
					{
						Point[] curve = objectShape.GetCurve();
						
						if (symbolObjectShape.Contains(curve[x].X, curve[x].Y))
							return true;
					}
				}				
			}
			else
			{
				if (symbol.Y >= this.Y && symbol.Bottom <= this.Bottom && this.ObjectShape.MaxPixelWidth >= symbol.ObjectShape.MaxPixelWidth)
				{
					ObjectShape symbolObjectShape = symbol.ObjectShape;

					for (int y = symbol.Y - this.Y; y < symbol.Bottom - this.Y; y++)
					{
						Point[] curve = objectShape.GetCurve();
						
						if (symbolObjectShape.Contains(curve[y].X, curve[y].Y))
							return true;
					}
				}
			}

			return false;
		}
		#endregion

		#region GetAdjacents()
		public Symbols GetAdjacents(Symbols symbols)
		{
			Symbols adjacents = new Symbols();

			/*if (this.IsHorizontal)
			{
				foreach (Symbol symbol in symbols)
				{
					if (Arithmetic.AreInLine(this.Y, this.Bottom, symbol.Y, symbol.Bottom))
					{
						if (symbol.X >= this.X && symbol.Right <= this.Right)
						{
							if ((symbol.Bottom > this.Y - 5) && (symbol.Y < this.Bottom + 5) && ShouldContain(symbol))
							{
								adjacents.Add(symbol);
							}
						}
						else
						{
							if (symbol.X <= this.X || symbol.Right >= this.Right)
							{
								int distance = Arithmetic.Distance(this.Rectangle, symbol.Rectangle);

								if (distance < 5 && this.ObjectShape.MaxPixelHeight >= symbol.ObjectShape.MaxPixelHeight)
								{
									adjacents.Add(symbol);
								}
							}
						}
					}
				}
			}
			else
			{
				foreach (Symbol symbol in symbols)
				{
					if (Arithmetic.AreInLine(this.X, this.Right, symbol.X, symbol.Right))
					{
						if (ShouldContain(symbol))
						{
							adjacents.Add(symbol);
						}
						else
						{
							if (symbol.Y <= this.Y || symbol.Bottom >= this.Bottom)
							{
								int distance = Arithmetic.Distance(this.Rectangle, symbol.Rectangle);

								if (distance < 5 && this.ObjectShape.MaxPixelWidth >= symbol.ObjectShape.MaxPixelWidth)
								{
									adjacents.Add(symbol);
								}
							}
						}
					}
				}
			}*/

			return adjacents;
		}
		#endregion

		#region GetAdjacents()
		public Symbols GetHorizontalAdjacents(Symbols symbolsSortedHorizontally)
		{
			Symbols adjacents = new Symbols();

			foreach (Symbol symbol in symbolsSortedHorizontally)
				{
					if (Arithmetic.AreInLine(this.Y, this.Bottom, symbol.Y, symbol.Bottom))
					{
						if (ShouldContain(symbol))
						{
							adjacents.Add(symbol);
						}
						else
						{
							if (symbol.X <= this.X || symbol.Right >= this.Right)
							{
								int distance = Arithmetic.Distance(this.Rectangle, symbol.Rectangle);

								if (distance < 5 && this.ObjectShape.MaxPixelHeight >= symbol.ObjectShape.MaxPixelHeight)
								{
									adjacents.Add(symbol);
								}
							}
						}
					}
				}

			return adjacents;
		}
		#endregion

		#region DrawToImage()
		public void DrawToImage(Color color, BitmapData bmpData)
		{
			this.objectMap.DrawToImage(color, bmpData);
			GC.Collect();
		}
		#endregion

		#region ToString()
		public override string ToString()
		{
			return string.Format("P1=[{0},{1}], P2=[{2},{3}], {4}", p1.X, p1.Y, p2.X, p2.Y, (type == Type.Horizontal) ? "Horizontal" : "Vertical");
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region SetPoints()
		private void SetPoints(Point point1, Point point2)
		{
			if (this.IsHorizontal)
			{
				this.p1 = (point1.X < point2.X) ? point1 : point2;
				this.p2 = (point1.X >= point2.X) ? point1 : point2;
			}
			else
			{
				this.p1 = (point1.Y < point2.Y) ? point1 : point2;
				this.p2 = (point1.Y >= point2.Y) ? point1 : point2;
			}
		}
		#endregion

		#region GetCurveArray()
		/*private int[] GetCurveArray(int[,] array)
		{
			if (this.curve != null)
				return this.curve;

			if ((this.symbol != null) && this.IsHorizontal)
			{
				int width = this.symbol.Width;
				int height = this.symbol.Height;
				this.curve = new int[width];
				
				for (int x = 0; x < width; x++)
				{
					int topY = -1;
					int bottomY = -1;

					for (int y = 0; y < height; y++ )
						if (array[y, x] == -1)
						{
							topY = y;
							break;
						}

					for (int y = height - 1; y >= 0; y--)
						if (array[y, x] == -1)
						{
							bottomY = y;
							break;
						}

					if (topY != -1)
						this.curve[x] = topY + ((bottomY - topY) / 2);
					else
						this.curve[x] = this.curve[x - 1];
				}

				return this.curve;
			}
			return null;
		}*/
		#endregion

		#region KeepPointsInsideImage()
		private void KeepPointsInsideImage(Size imageSize, ref Point p1, ref Point p2, ref Point p3, ref Point p4)
		{
			if (p1.X < 20)
				p1.X = 0;
			if (p1.X > imageSize.Width - 20)
				p1.X = imageSize.Width;
			if (p1.Y < 20)
				p1.Y = 0;
			if (p1.Y > imageSize.Height - 20)
				p1.Y = imageSize.Height;

			if (p2.X < 20)
				p2.X = 0;
			if (p2.X > imageSize.Width - 20)
				p2.X = imageSize.Width;
			if (p2.Y < 20)
				p2.Y = 0;
			if (p2.Y > imageSize.Height - 20)
				p2.Y = imageSize.Height;

			if (p3.X < 20)
				p3.X = 0;
			if (p3.X > imageSize.Width - 20)
				p3.X = imageSize.Width;
			if (p3.Y < 20)
				p3.Y = 0;
			if (p3.Y > imageSize.Height - 20)
				p3.Y = imageSize.Height;

			if (p4.X < 20)
				p4.X = 0;
			if (p4.X > imageSize.Width - 20)
				p4.X = imageSize.Width;
			if (p4.Y < 20)
				p4.Y = 0;
			if (p4.Y > imageSize.Height - 20)
				p4.Y = imageSize.Height;
		}
		#endregion

		#region CheckRectangle
		private void CheckRectangle()
		{
			Rectangle oldRect = this.rectangle;

			this.rectangle = Rectangle.Union(this.rectangle, Rectangle.FromLTRB(this.LeftPoint.X, this.TopPoint.Y, this.RightPoint.X, this.BottomPoint.Y));

			if (this.rectangle != oldRect)
			{
				this.objectMap = GetObjectMap(this.p1, this.p2);
			}
		}
		#endregion

		#region GetObjectMap()
		private static ObjectMap GetObjectMap(Point p1, Point p2)
		{
			Rectangle		clip = Rectangle.FromLTRB(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y));

			clip.Width++;
			clip.Height++;

			//ObjectMap		objectMap = new ObjectMap(clip);
			List<Point>		points = GetLinePoints(p1, p2);
			byte[,]			array = new byte[clip.Height, clip.Width];

			foreach (Point p in points)
			{
				int x = p.X - clip.X;
				int y = p.Y - clip.Y;
				
				array[y, x / 8] |= (byte)(0x80 >> (x & 0x07)); 
				//objectMap.SetPoint(p.X - clip.X, p.Y - clip.Y, true);
			}

			return new ObjectMap(clip, array);
		}
		#endregion

		#region GetLinePoints()
		/// <summary>
		/// Returns all integer points between p1, p2, excluding both of them.
		/// There are not 'holes' in the points. It is not guaranteed that points would be somehow sorted. 
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		static List<Point> GetLinePoints(Point p1, Point p2)
		{
			List<Point> linePoints = new List<Point>();

			if (Math.Abs(p1.X - p2.X) > Math.Abs(p1.Y - p2.Y))
			{
				int minX = (p1.X < p2.X) ? p1.X : p2.X;
				int maxX = (p1.X > p2.X) ? p1.X : p2.X;

				for (int x = minX; x <= maxX; x++)
				{
					int y = Convert.ToInt32(Arithmetic.GetY(p1, p2, x));
					linePoints.Add(new Point(x, y));
				}
			}
			else
			{
				int minY = (p1.Y < p2.Y) ? p1.Y : p2.Y;
				int maxY = (p1.Y > p2.Y) ? p1.Y : p2.Y;

				for (int y = minY; y <= maxY; y++)
				{
					int x = Convert.ToInt32(Arithmetic.GetX(p1, p2, y));
					linePoints.Add(new Point(x, y));
				}
			}

			return linePoints;
		}
		#endregion

		#endregion

	}

}
