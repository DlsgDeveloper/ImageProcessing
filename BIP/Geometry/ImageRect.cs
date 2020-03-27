using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIP.Geometry
{
	public class ImageRect
	{
		public double X;
		public double Y;
		public double Width;
		public double Height;


		#region constructor
		public ImageRect(double x, double y, double width, double height)
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
		public bool IsEmpty { get { return this.X == 0 && this.Y == 0 && this.Width == 0 && this.Height == 0; } }
		public ImageSize Size { get { return new ImageSize(Width, Height); } }

		/// <summary>
		/// returns rectangle with values <0,0,0,0>
		/// </summary>
		public static ImageRect Empty { get { return new ImageRect(0, 0, 0, 0); } }

		/// <summary>
		/// returns rectangle with values <0,0,1,1>
		/// </summary>
		public static ImageRect Default { get { return new ImageRect(0, 0, 1, 1); } }

		public double Left
		{
			get { return this.X; }
			set { this.X = (value > 0) ? value : 0; }
		}

		public double Top
		{
			get { return this.Y; }
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
		public static bool operator ==(ImageRect r1, ImageRect r2)
		{
			if (r1 is null && r2 is null)
				return true;
			else if (r1 is null || r2 is null)
				return false;
			else
				return !(r1.X != r2.X || r1.Y != r2.Y || r1.Width != r2.Width || r1.Height != r2.Height);
		}
		#endregion

		#region operator !=
		public static bool operator !=(ImageRect r1, ImageRect r2)
		{
			return !(r1 == r2);
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
			if (obj is ImageRect r)
				return !(this.X != r.X || this.Y != r.Y || this.Width != r.Width || this.Height != r.Height);
			else
				return false;
		}
		#endregion

		#region Clone()
		public ImageRect Clone()
		{
			return new ImageRect(this.X, this.Y, this.Width, this.Height);
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
			else if (this.Y > 1)
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
		public void Intersect(ImageRect r)
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

		public static ImageRect Intersect(ImageRect r1, ImageRect r2)
		{
			if (((r1.X <= r2.X && r1.Right >= r2.X)) && (r1.Y <= r2.Y && r1.Bottom >= r2.Y) || ((r1.Right >= r2.X && r1.Right <= r2.Right) && (r1.Bottom >= r2.Y && r1.Bottom <= r2.Bottom)))
			{
				return ImageRect.FromLTRB((r1.X > r2.X) ? r1.X : r2.X,
					(r1.Y > r2.Y) ? r1.Y : r2.Y,
					(r1.Right < r2.Right) ? r1.Right : r2.Right,
					(r1.Bottom < r2.Bottom) ? r1.Bottom : r2.Bottom);
			}
			else
			{
				return ImageRect.Empty;
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

		public static ImageRect Union(ImageRect r1, ImageRect r2)
		{
			return ImageRect.FromLTRB((r1.X < r2.X) ? r1.X : r2.X,
				(r1.Y < r2.Y) ? r1.Y : r2.Y,
				(r1.Right > r2.Right) ? r1.Right : r2.Right,
				(r1.Bottom > r2.Bottom) ? r1.Bottom : r2.Bottom);
		}
		#endregion

		#region Contains()
		public bool Contains(ImagePoint p)
		{
			return !(this.X > p.X || this.Y > p.Y || this.Right < p.X || this.Bottom < p.Y);
		}
		#endregion

		#region FromLTRB()
		public static ImageRect FromLTRB(double left, double top, double right, double bottom)
		{
			return new ImageRect(left, top, right - left, bottom - top);
		}
		#endregion

		#region FromRatioRect()
		public static ImageRect FromRatioRect(RatioRect ratioRect, double bitmapW, double bitmapH)
		{
			return new ImageRect(ratioRect.X * bitmapW, ratioRect.Y * bitmapH, ratioRect.Width * bitmapW, ratioRect.Height * bitmapH);
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
		public static ImageRect Inflate(ImageRect rect, double dx, double dy)
		{
			return new ImageRect(rect.X - dx, rect.Y - dy, rect.Width + dx * 2, rect.Height + dy * 2);
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
