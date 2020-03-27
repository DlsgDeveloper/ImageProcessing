using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BIP.Geometry
{
	public struct RatioRect
	{
		public double X;
		public double Y;
		public double Width;
		public double Height;


		#region constructor
		public RatioRect(double x, double y, double width, double height)
		{
			this.X = (x > 0) ? x : 0;
			this.Y = (y > 0) ? y : 0;
			this.Width = (width > 0) ? width : 0;
			this.Height = (height > 0) ? height : 0;
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties

		public bool IsDefault { get { return !(this.X != 0 || this.Y != 0 || this.Width != 1 || this.Height != 1); } }
		public bool IsEmpty		{ get { return this.X == 0 && this.Y == 0 && this.Width == 0 && this.Height == 0; } }
		public RatioSize Size { get { return new RatioSize(Width, Height); } }
	
		/// <summary>
		/// returns rectangle with values <0,0,0,0>
		/// </summary>
		public static RatioRect Empty { get { return new RatioRect(0, 0, 0, 0); } }

		/// <summary>
		/// returns rectangle with values <0,0,1,1>
		/// </summary>
		public static RatioRect Default { get { return new RatioRect(0, 0, 1, 1); } }

		public double Left
		{
			get { return this.X; }
			set { this.X = (value > 0) ? value : 0; }
		}

		public double Top
		{
			get { return this.Y ; }
			set { this.Y = (value > 0) ? value : 0; }
		}

		public double Right
		{
			get { return this.X + this.Width; }
			set { this.Width = (value - this.X > 0) ? value - this.X : 0; }
		}

		public double Bottom 
		{ 
			get { return this.Y + this.Height; }
			set { this.Height = (value - this.Y > 0) ? value - this.Y : 0; }
		}

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region operator ==
		public static bool operator ==(RatioRect r1, RatioRect r2)
		{
			return !(r1.X != r2.X || r1.Y != r2.Y || r1.Width != r2.Width || r1.Height != r2.Height);
		}
		#endregion

		#region operator !=
		public static bool operator !=(RatioRect r1, RatioRect r2)
		{
			return (r1.X != r2.X || r1.Y != r2.Y || r1.Width != r2.Width || r1.Height != r2.Height);
		}
		#endregion

		#region GetHashCode()
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		#endregion

		#region Equals()
		public override bool Equals(object obj)
		{
			RatioRect r = (RatioRect) obj;
			
			return !(this.X != r.X || this.Y != r.Y || this.Width != r.Width || this.Height != r.Height);
		}
		#endregion

		#region Clone()
		public RatioRect Clone()
		{
			return new RatioRect(this.X, this.Y, this.Width, this.Height);
		}
		#endregion

		#region IntersectWithDefault()
		public void IntersectWithDefault()
		{
			if (this.X < 0)
			{
				this.Width += this.X;
				this.X = 0;
			}
			else if (this.X > 1)
			{
				this.Width += this.X - 1;
				this.X = 1;
			}

			if (this.Y < 0)
			{
				this.Height += this.Y;
				this.Y = 0;
			}
			else if(this.Y > 1)
			{
				this.Height += this.Y - 1;
				this.Y = 1;
			}

			if (this.Right > 1)
				this.Right = 1;
			if (this.Bottom > 1)
				this.Bottom = 1;
		}
		#endregion
	
		#region Intersect()
		public void Intersect(RatioRect r)
		{
			if (((this.X <= r.X && this.Right >= r.X) || (this.X >= r.X && this.X <= r.Right)) && ((this.Y <= r.Y && this.Bottom >= r.Y) || (this.Y >= r.Y && this.Y <= r.Bottom)))
			{
				if (this.X < r.X)
				{
					this.Width -= r.X - this.X;
					this.X = r.X;
				}
				if (this.Y < r.Y)
				{
					this.Height = this.Height - (r.Y - this.Y);
					this.Y = r.Y;
				}
				
				this.Right = (this.Right < r.Right) ? this.Right : r.Right;
				this.Bottom = (this.Bottom < r.Bottom) ? this.Bottom : r.Bottom;
			}
			else
			{
				this.X = 0;
				this.Y = 0;
				this.Right = 0;
				this.Bottom = 0;
			}
		}

		public static RatioRect Intersect(RatioRect r1, RatioRect r2)
		{
			if (((r1.X <= r2.X && r1.Right >= r2.X)) && (r1.Y <= r2.Y && r1.Bottom >= r2.Y) || ((r1.Right >= r2.X && r1.Right <= r2.Right) && (r1.Bottom >= r2.Y && r1.Bottom <= r2.Bottom)))
			{
				return RatioRect.FromLTRB((r1.X > r2.X) ? r1.X : r2.X,
					(r1.Y > r2.Y) ? r1.Y : r2.Y,
					(r1.Right < r2.Right) ? r1.Right : r2.Right,
					(r1.Bottom < r2.Bottom) ? r1.Bottom : r2.Bottom);
			}
			else
			{
				return RatioRect.Empty;
			}
		}
		#endregion

		#region Union()
		public void Union(ImageRect rect)
		{
			if (this.X > rect.X)
			{
				this.Width += this.X - rect.X;
				this.X = rect.X;
			}

			if (this.Y > rect.Y)
			{
				this.Height += this.Y - rect.Y;
				this.Y = rect.Y;
			}

			if ((this.X + this.Width) < (rect.X + rect.Width))
				this.Width = (rect.X + rect.Width) - this.X;

			if ((this.Y + this.Height) < (rect.Y + rect.Height))
				this.Height = (rect.Y + rect.Height) - this.Y;
		}

		public static RatioRect Union(RatioRect r1, RatioRect r2)
		{
			return RatioRect.FromLTRB((r1.X < r2.X) ? r1.X : r2.X,
				(r1.Y < r2.Y) ? r1.Y : r2.Y,
				(r1.Right > r2.Right) ? r1.Right : r2.Right,
				(r1.Bottom > r2.Bottom) ? r1.Bottom : r2.Bottom);
		}
		#endregion

		#region Contains()
		public bool Contains(RatioPoint p)
		{
			return !(this.X > p.X || this.Y > p.Y || this.Right < p.X || this.Bottom < p.Y);
		}
		#endregion

		#region FromLTRB()
		public static RatioRect FromLTRB(double left, double top, double right, double bottom)
		{
			return new RatioRect(left, top, right - left, bottom - top);
		}
		#endregion

		#region Inflate()
		public void Inflate(double dx, double dy)
		{
			this.X -= dx;
			this.Y -= dy;
			this.Width += dx * 2;
			this.Height += dy * 2;
		}

		public static RatioRect Inflate(RatioRect rect, double dx, double dy)
		{
			return new RatioRect(rect.X - dx, rect.Y - dy, rect.Width + dx * 2, rect.Height + dy * 2);
		}
		#endregion

		#region ToString()
		public override string ToString()
		{
			return string.Format("LTRB=[{0}, {1}, {4}, {5}], W={2}, H={3}", this.X.ToString("F3"), this.Y.ToString("F3"), this.Width.ToString("F3"), this.Height.ToString("F3"), this.Right.ToString("F3"), this.Bottom.ToString("F3"));
		}
		#endregion

		#region MoveTo()
		/// <summary>
		/// Returns true if the rect changed.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public bool MoveTo(double x, double y)
		{
			x = Math.Max(0.0, Math.Min(1.0 - this.Width, x));
			y = Math.Max(0.0, Math.Min(1.0 - this.Height, y));

			if (this.X != x || this.Y != y)
			{
				this.X = x;
				this.Y = y;
				return true;
			}

			return false;
		}
		#endregion

		#endregion

	}
}
