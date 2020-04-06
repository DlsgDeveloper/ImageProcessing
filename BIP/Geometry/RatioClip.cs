using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIP.Geometry
{
	public class RatioClip : IEquatable<RatioClip>
	{
		RatioRect _clip = new RatioRect(0, 0, 1, 1);
		double _skew = 0;

		public event EventHandler Changed;


		#region constructor
		public RatioClip()
		{
		}

		public RatioClip(double x, double y, double w, double h, double skew = 0)
			: this(new RatioRect(x, y, w, h), skew)
		{
		}

		public RatioClip(RatioRect clip, double skew)
		{
			_clip = clip.Clone();

			if (_clip.Width > 1 || _clip.Height > 1)
			{
				double max = Math.Max(_clip.Width, _clip.Height);

				_clip.Width = _clip.Width / max;
				_clip.Height = _clip.Height / max;
			}

			_clip.X = Math.Max(0, Math.Min(_clip.X, 1 - _clip.Width));
			_clip.Y = Math.Max(0, Math.Min(_clip.Y, 1 - _clip.Height));

			_skew = skew;
		}
		#endregion


		// PUBLIC PROPERTIES
		#region public properties

		public double X { get { return _clip.X; } }
		public double Y { get { return _clip.Y; } }
		public double Right { get { return _clip.Right; } }
		public double Bottom { get { return _clip.Bottom; } }
		public double Width { get { return _clip.Width; } }
		public double Height { get { return _clip.Height; } }

		public RatioRect Clip { get { return _clip; } }

		#region Skew
		/// <summary>
		/// In radians.
		/// </summary>
		public double Skew
		{
			get { return _skew; }
			set
			{
				if (_skew != value)
				{
					_skew = value;
					Changed?.Invoke(this, EventArgs.Empty);
				}
			}
		}
		#endregion

		public bool IsDefault { get { return _clip.IsDefault && _skew == 0; } }

		#endregion


		// PUBLIC METHODS
		#region public methods

		#region ResizeBy()
		public void ResizeBy(double zoom, bool constrainProportions)
		{
			double x, y, newWidth, newHeight;

			if (constrainProportions)
			{
				double ratio = _clip.Height / _clip.Width;
				newWidth = Math.Max(0.05, Math.Min(1, _clip.Width + zoom));
				newHeight = newWidth * ratio;

				if (newHeight < 0.05)
				{
					newHeight = 0.05;
					newWidth = newWidth / ratio;
				}
				if (newHeight > 1)
				{
					newHeight = 1;
					newWidth = newHeight / ratio;
				}

				x = Math.Max(0.0, Math.Min(1.0 - _clip.Width, _clip.X + (-(newWidth - _clip.Width) / 2.0)));
				y = Math.Max(0.0, Math.Min(1.0 - _clip.Height, _clip.Y + (-(newHeight - _clip.Height) / 2.0)));
			}
			else
			{
				newWidth = Math.Max(0.05, Math.Min(1, _clip.Width + zoom));
				newHeight = Math.Max(0.05, Math.Min(1, _clip.Height + zoom));

				x = Math.Max(0.0, Math.Min(1.0 - _clip.Width, _clip.X + (-(newWidth - _clip.Width) / 2.0)));
				y = Math.Max(0.0, Math.Min(1.0 - _clip.Height, _clip.Y + (-(newHeight - _clip.Height) / 2.0)));
			}

			if (_clip.X != x || _clip.Y != y || _clip.Width != newWidth || _clip.Height != newHeight)
			{
				_clip.X = x;
				_clip.Y = y;
				_clip.Width = newWidth;
				_clip.Height = newHeight;

				Changed?.Invoke(this, EventArgs.Empty);
			}
		}

		public void ResizeBy(double dl, double dt, double dr, double db)
		{
			double x = Math.Max(0.0, Math.Min(0.95, _clip.X + dl));
			double y = Math.Max(0.0, Math.Min(0.95, _clip.Y + dt));
			double width = Math.Max(0.05, Math.Min(1.0 - _clip.X, _clip.Width - dl + dr));
			double height = Math.Max(0.05, Math.Min(1.0 - _clip.Y, _clip.Height - dt + db));

			if (_clip.X != x || _clip.Y != y || _clip.Width != width || _clip.Height != height)
			{
				_clip.X = x;
				_clip.Y = y;
				_clip.Width = width;
				_clip.Height = height;

				Changed?.Invoke(this, EventArgs.Empty);
			}
		}
		#endregion

		#region MoveBy()
		public void MoveBy(double dx, double dy)
		{
			double x = Math.Max(0.0, Math.Min(1.0 - _clip.Width, _clip.X + dx));
			double y = Math.Max(0.0, Math.Min(1.0 - _clip.Height, _clip.Y + dy));

			if (_clip.X != x || _clip.Y != y)
			{
				_clip.X = x;
				_clip.Y = y;
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}
		#endregion

		#region MoveTo()
		public void MoveTo(double x, double y)
		{
			if (_clip.MoveTo(x, y))
				Changed?.Invoke(this, EventArgs.Empty);
		}
		#endregion

		#region Set()
		public void Set(RatioRect rect, double? skew = null)
		{
			rect.X = Math.Max(0.0, Math.Min(0.99, rect.X));
			rect.Y = Math.Max(0.0, Math.Min(0.99, rect.Y));
			rect.Width = Math.Max(0.01, Math.Min(1.0 - rect.X, rect.Width));
			rect.Height = Math.Max(0.01, Math.Min(1.0 - rect.Y, rect.Height));

			if (_clip != rect)
			{
				_clip = rect;
				Changed?.Invoke(this, EventArgs.Empty);
			}

			if (skew != null)
				this.Skew = skew.Value;
		}
		#endregion

		#region operator ==
		public static bool operator ==(RatioClip r1, RatioClip r2)
		{
			if (r1 is null && r2 is null)
				return true;
			else if (r1 is null || r2 is null)
				return false;
			else
				return !(r1.X != r2.X || r1.Y != r2.Y || r1.Width != r2.Width || r1.Height != r2.Height || r1.Skew != r2.Skew);
		}
		#endregion

		#region operator !=
		public static bool operator !=(RatioClip r1, RatioClip r2)
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
			if (obj is RatioClip r)
				return !(this.X != r.X || this.Y != r.Y || this.Width != r.Width || this.Height != r.Height || this.Skew != r.Skew);
			else
				return false;
		}

		public bool Equals(RatioClip r)
		{
			if (r == null)
				return false;

			return !(this.X != r.X || this.Y != r.Y || this.Width != r.Width || this.Height != r.Height || this.Skew != r.Skew);
		}
		#endregion

		#region Clone()
		public RatioClip Clone()
		{
			return new RatioClip(this.Clip, this.Skew);
		}
		#endregion

		#region FromLTRB()
		public static RatioClip FromLTRB(double l, double t, double r, double b)
		{
			return new RatioClip(l, t, r - l, b - t);
		}
		#endregion

		#region GetImageCorners()
		public ImageCornersD GetImageCorners(double bitmapW, double bitmapH)
		{
			double centerX = (_clip.X + _clip.Width / 2.0) * bitmapW;
			double centerY = (_clip.Y + _clip.Height / 2.0) * bitmapH;

			return new ImageCornersD(
				Rotation.RotatePoint(_clip.X * bitmapW, _clip.Y * bitmapH, centerX, centerY, this.Skew),
				Rotation.RotatePoint(_clip.Right * bitmapW, _clip.Y * bitmapH, centerX, centerY, this.Skew),
				Rotation.RotatePoint(_clip.X * bitmapW, _clip.Bottom * bitmapH, centerX, centerY, this.Skew),
				Rotation.RotatePoint(_clip.Right * bitmapW, _clip.Bottom * bitmapH, centerX, centerY, this.Skew)
				);
		}
		#endregion

		#region GetRatioCorners()
		public RatioCorners GetRatioCorners(double bitmapW, double bitmapH)
		{
			ImageCornersD imageCorners = GetImageCorners(bitmapW, bitmapH);

			return new RatioCorners(
				new RatioPoint(imageCorners.Pul.X / bitmapW, imageCorners.Pul.Y / bitmapH),
				new RatioPoint(imageCorners.Pur.X / bitmapW, imageCorners.Pur.Y / bitmapH),
				new RatioPoint(imageCorners.Pll.X / bitmapW, imageCorners.Pll.Y / bitmapH),
				new RatioPoint(imageCorners.Plr.X / bitmapW, imageCorners.Plr.Y / bitmapH)
				);
		}
		#endregion

		#region ToString()
		public override string ToString()
		{
			return string.Format("LTRB: [{0:0.000}, {1:0.000}, {2:0.000}, {3:0.000}], W={4:0.000}, H={5:0.000}, Skew={6:0.00}°", X, Y, Right, Bottom, Width, Height, Skew * 180 / Math.PI);
		}
		#endregion

		#endregion

	}
}
