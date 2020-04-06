using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIP.Geometry
{
	public class ImageClip : IEquatable<ImageClip>
	{
		private readonly ImageRect	_clip;
		double		_skew;

		public event EventHandler Changed;


		#region constructor
		public ImageClip(double x, double y, double w, double h, double skew = 0)
			: this(new ImageRect(x, y, w, h), skew)
		{
		}

		public ImageClip(ImageRect clip, double skew)
		{
			_clip = clip.Clone();
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

		public ImageRect Clip { get { return _clip; } }

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

		#region Set()
		/// <summary>
		/// It doesn't check if the rect is outside the image
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="skew"></param>
		public void Set(ImageRect rect, double? skew = null)
		{
			if (_clip != rect || (skew != null && skew.Value != _skew))
			{
				_clip.X = rect.X;
				_clip.Y = rect.Y;
				_clip.Width = rect.Width;
				_clip.Height = rect.Height;
				_skew = skew.Value;

				Changed?.Invoke(this, EventArgs.Empty);
			}
		}
		#endregion

		#region operator ==
		public static bool operator ==(ImageClip r1, ImageClip r2)
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
		public static bool operator !=(ImageClip r1, ImageClip r2)
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
			if (obj is ImageClip r)
				return !(this.X != r.X || this.Y != r.Y || this.Width != r.Width || this.Height != r.Height || this.Skew != r.Skew);
			else
				return false;
		}

		public bool Equals(ImageClip r)
		{
			if (r == null)
				return false;

			return !(this.X != r.X || this.Y != r.Y || this.Width != r.Width || this.Height != r.Height || this.Skew != r.Skew);
		}
		#endregion

		#region Clone()
		public ImageClip Clone()
		{
			return new ImageClip(this.Clip, this.Skew);
		}
		#endregion

		#region FromLTRB()
		public static ImageClip FromLTRB(double l, double t, double r, double b)
		{
			return new ImageClip(l, t, r - l, b - t);
		}
		#endregion

		#region GetImageCorners()
		public ImageCorners GetImageCorners()
		{
			ImageCornersD c = GetImageCornersD();

			return new ImageCorners(
				new ImagePoint(Convert.ToInt32(c.Pul.X), Convert.ToInt32(c.Pul.Y)),
				new ImagePoint(Convert.ToInt32(c.Pur.X), Convert.ToInt32(c.Pur.Y)),
				new ImagePoint(Convert.ToInt32(c.Pll.X), Convert.ToInt32(c.Pll.Y)),
				new ImagePoint(Convert.ToInt32(c.Plr.X), Convert.ToInt32(c.Plr.Y))
				);
		}
		#endregion

		#region GetImageCornersD()
		public ImageCornersD GetImageCornersD()
		{
			double centerX = (_clip.X + _clip.Width / 2.0);
			double centerY = (_clip.Y + _clip.Height / 2.0);

			return new ImageCornersD(
				Rotation.RotatePoint(_clip.X, _clip.Y, centerX, centerY, this.Skew),
				Rotation.RotatePoint(_clip.Right, _clip.Y, centerX, centerY, this.Skew),
				Rotation.RotatePoint(_clip.X, _clip.Bottom, centerX, centerY, this.Skew),
				Rotation.RotatePoint(_clip.Right, _clip.Bottom, centerX, centerY, this.Skew)
				);
		}
		#endregion

		#region GetImageCornersD()
		public ImageCornersD GetImageCornersD(double bitmapW, double bitmapH)
		{
			ImageCorners imageCorners = GetImageCorners();

			return new ImageCornersD(
				new ImagePointD(imageCorners.Pul.X / bitmapW, imageCorners.Pul.Y / bitmapH),
				new ImagePointD(imageCorners.Pur.X / bitmapW, imageCorners.Pur.Y / bitmapH),
				new ImagePointD(imageCorners.Pll.X / bitmapW, imageCorners.Pll.Y / bitmapH),
				new ImagePointD(imageCorners.Plr.X / bitmapW, imageCorners.Plr.Y / bitmapH)
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
