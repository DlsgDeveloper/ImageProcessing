using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIP.Geometry
{
	public class ImageCornersD
	{
		ImagePointD _pul, _pur, _pll, _plr;

		public ImagePointD Pul { get { return _pul; } }
		public ImagePointD Pur { get { return _pur; } }
		public ImagePointD Pll { get { return _pll; } }
		public ImagePointD Plr { get { return _plr; } }


		#region constructor
		public ImageCornersD(ImagePointD pUL, ImagePointD pUR, ImagePointD pLL, ImagePointD pLR)
		{
			_pul = pUL;
			_pur = pUR;
			_pll = pLL;
			_plr = pLR;
		}

		public ImageCornersD(System.Drawing.Point pUL, System.Drawing.Point pUR, System.Drawing.Point pLL, System.Drawing.Point pLR)
		{
			_pul = new ImagePointD(pUL.X, pUL.Y);
			_pur = new ImagePointD(pUR.X, pUR.Y);
			_pll = new ImagePointD(pLL.X, pLL.Y);
			_plr = new ImagePointD(pLR.X, pLR.Y);
		}
		#endregion


		// PUBLIC PROPERTIES
		#region public properties

		public double Width
		{
			get
			{
				double min = Math.Min(Math.Min(this.Pul.X, this.Pur.X), Math.Min(this.Pll.X, this.Plr.X));
				double max = Math.Max(Math.Max(this.Pul.X, this.Pur.X), Math.Max(this.Pll.X, this.Plr.X));
				return max - min;
			}
		}

		public double Height
		{
			get
			{
				double min = Math.Min(Math.Min(this.Pul.Y, this.Pur.Y), Math.Min(this.Pll.Y, this.Plr.Y));
				double max = Math.Max(Math.Max(this.Pul.Y, this.Pur.Y), Math.Max(this.Pll.Y, this.Plr.Y));
				return max - min;
			}
		}

		public ImageRect Clip
		{
			get
			{
				double x = Math.Min(Math.Min(this.Pul.X, this.Pur.X), Math.Min(this.Pll.X, this.Plr.X));
				double r = Math.Max(Math.Max(this.Pul.X, this.Pur.X), Math.Max(this.Pll.X, this.Plr.X));
				double y = Math.Min(Math.Min(this.Pul.Y, this.Pur.Y), Math.Min(this.Pll.Y, this.Plr.Y));
				double b = Math.Max(Math.Max(this.Pul.Y, this.Pur.Y), Math.Max(this.Pll.Y, this.Plr.Y));

				return new ImageRect(x, y, r - x, b - y);
			}
		}

		public ImagePointD CenterPoint
		{
			get
			{
				double x = Math.Min(Math.Min(this.Pul.X, this.Pur.X), Math.Min(this.Pll.X, this.Plr.X));
				double r = Math.Max(Math.Max(this.Pul.X, this.Pur.X), Math.Max(this.Pll.X, this.Plr.X));
				double y = Math.Min(Math.Min(this.Pul.Y, this.Pur.Y), Math.Min(this.Pll.Y, this.Plr.Y));
				double b = Math.Max(Math.Max(this.Pul.Y, this.Pur.Y), Math.Max(this.Pll.Y, this.Plr.Y));

				return new ImagePointD(Convert.ToInt32((x + r) / 2.0), Convert.ToInt32((y + b) / 2.0));
			}
		}

		public double Angle
		{
			get
			{
				return Rotation.GetAngle(this.Pul, this.Pur);
			}
		}

		#endregion


		// PUBLIC METHODS
		#region public methods

		#region Offset()
		public void Offset(int offsetX, int offsetY)
		{
			_pul.Offset(offsetX, offsetY);
			_pur.Offset(offsetX, offsetY);
			_pll.Offset(offsetX, offsetY);
			_plr.Offset(offsetX, offsetY);
		}
		#endregion

		#region Rotate()
		public void Rotate(double centerX, double centerY, double angle)
		{

			_pul = Rotation.RotatePoint(_pul.X, _pul.Y, centerX, centerY, angle);
			_pur = Rotation.RotatePoint(_pur.X, _pur.Y, centerX, centerY, angle);
			_pll = Rotation.RotatePoint(_pll.X, _pll.Y, centerX, centerY, angle);
			_plr = Rotation.RotatePoint(_plr.X, _plr.Y, centerX, centerY, angle);
		}
		#endregion

		#region GetImageClip()
		public ImageClip GetImageClip()
		{
			double angle = this.Angle;

			if (angle != 0)
			{
				ImagePointD center = this.CenterPoint;

				ImagePointD pUL = Rotation.RotatePoint(_pul, center, -angle);
				ImagePointD pUR = Rotation.RotatePoint(_pur, center, -angle);
				ImagePointD pLL = Rotation.RotatePoint(_pll, center, -angle);

				ImageClip c = new ImageClip(pUL.X, pUL.Y, (pUR.X - pUL.X), (pLL.Y - pUL.Y), this.Angle);

				return c;
			}
			else
			{
				ImageClip c = new ImageClip(_pul.X, _pul.Y, (_pur.X - _pul.X), (_pll.Y - _pul.Y), this.Angle);

				return c;
			}
		}
		#endregion
		
		#region GetRatioClip()
		public RatioClip GetRatioClip(double bitmapW, double bitmapH)
		{
			double angle = this.Angle;

			if (angle != 0)
			{
				ImagePointD center = this.CenterPoint;

				ImagePointD pUL = Rotation.RotatePoint(_pul, center, -angle);
				ImagePointD pUR = Rotation.RotatePoint(_pur, center, -angle);
				ImagePointD pLL = Rotation.RotatePoint(_pll, center, -angle);

				RatioClip c = new RatioClip(pUL.X / bitmapW, pUL.Y / bitmapH, (pUR.X - pUL.X) / bitmapW, (pLL.Y - pUL.Y) / bitmapH, this.Angle);

				return c;
			}
			else
			{
				RatioClip c = new RatioClip(_pul.X / bitmapW, _pul.Y / bitmapH, (_pur.X - _pul.X) / bitmapW, (_pll.Y - _pul.Y) / bitmapH, this.Angle);

				return c;
			}
		}
		#endregion

		#endregion

	}
}
