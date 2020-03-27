using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BIP.Geometry
{
	public struct InchRect
	{
		public double X;
		public double Y;
		public double Width;
		public double Height;

		public InchRect(double x, double y, double width, double height)
		{
			this.X = x;
			this.Y = y;
			this.Width = width;
			this.Height = height;
		}

		//PUBLIC PROPERTIES
		#region public properties

		public bool IsEmpty { get { return this.X == 0 && this.Y == 0 && this.Width == 0 && this.Height == 0; } }
		public InchSize Size { get { return new InchSize(Width, Height); } }

		/// <summary>
		/// returns rectangle with values <0,0,0,0>
		/// </summary>
		public static InchRect Empty { get { return new InchRect(0, 0, 0, 0); } }

		public double Left
		{
			get { return this.X; }
			set { this.X = value; }
		}

		public double Top
		{
			get { return this.Y ; }
			set { this.Y = value; }
		}

		public double Right
		{
			get { return this.X + this.Width; }
			set { this.Width = value - this.X; }
		}

		public double Bottom 
		{ 
			get { return this.Y + this.Height; }
			set { this.Height = value - this.Y; }
		}
	
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region operator ==
		public static bool operator ==(InchRect r1, InchRect r2)
		{
			return !(r1.X != r2.X || r1.Y != r2.Y || r1.Width != r2.Width || r1.Height != r2.Height);
		}
		#endregion

		#region operator !=
		public static bool operator !=(InchRect r1, InchRect r2)
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
			InchRect r = (InchRect)obj;

			return !(this.X != r.X || this.Y != r.Y || this.Width != r.Width || this.Height != r.Height);
		}
		#endregion

		#region Intersect()
		public void Intersect(InchRect r)
		{
			if (((this.X <= r.X && this.Right >= r.X)) && (this.Y <= r.Y && this.Bottom >= r.Y) || ((this.Right >= r.X && this.Right <= r.Right) && (this.Bottom >= r.Y && this.Bottom <= r.Bottom)))
			{
				this.X = (this.X > r.X) ? this.X : r.X;
				this.Y = (this.Y > r.Y) ? this.Y : r.Y;
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
		#endregion

		#region Intersect()
		public static InchRect Intersect(InchRect r1, InchRect r2)
		{
			if (((r1.X <= r2.X && r1.Right >= r2.X)) && (r1.Y <= r2.Y && r1.Bottom >= r2.Y) || ((r1.Right >= r2.X && r1.Right <= r2.Right) && (r1.Bottom >= r2.Y && r1.Bottom <= r2.Bottom)))
			{
				return InchRect.FromLTRB((r1.X > r2.X) ? r1.X : r2.X,
					(r1.Y > r2.Y) ? r1.Y : r2.Y,
					(r1.Right < r2.Right) ? r1.Right : r2.Right,
					(r1.Bottom < r2.Bottom) ? r1.Bottom : r2.Bottom);
			}
			else
			{
				return InchRect.Empty;
			}
		}
		#endregion

		#region Contains()
		public bool Contains(RatioPoint p)
		{
			return !(this.X > p.X || this.Y > p.Y || this.Right < p.X || this.Bottom < p.Y);
		}
		#endregion

		#region FromLTRB()
		public static InchRect FromLTRB(double left, double top, double right, double bottom)
		{
			return new InchRect(left, top, right - left, bottom - top);
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
		#endregion

		#region Inflate()
		public static InchRect Inflate(InchRect rect, double dx, double dy)
		{
			return new InchRect(rect.X - dx, rect.Y - dy, rect.Width + dx * 2, rect.Height + dy * 2);
		}
		#endregion

		#region ToString()
		public override string ToString()
		{
			return string.Format("LTRB=[{0}, {1}, {4}, {5}], W={2}, H={3}", this.X.ToString("F3"), this.Y.ToString("F3"), this.Width.ToString("F3"), this.Height.ToString("F3"), this.Right.ToString("F3"), this.Bottom.ToString("F3"));
		}
		#endregion

		#endregion
	}
}
