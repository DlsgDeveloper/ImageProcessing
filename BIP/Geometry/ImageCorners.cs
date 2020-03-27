using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIP.Geometry
{
	public class ImageCorners
	{
		ImagePoint _pul, _pur, _pll, _plr;

		public ImagePoint Pul { get { return _pul; } }
		public ImagePoint Pur { get { return _pur; } }
		public ImagePoint Pll { get { return _pll; } }
		public ImagePoint Plr { get { return _plr; } }


		#region constructor
		public ImageCorners(ImagePoint pUL, ImagePoint pUR, ImagePoint pLL, ImagePoint pLR)
		{
			_pul = pUL;
			_pur = pUR;
			_pll = pLL;
			_plr = pLR;
		}

		public ImageCorners(System.Drawing.Point pUL, System.Drawing.Point pUR, System.Drawing.Point pLL, System.Drawing.Point pLR)
		{
			_pul = new ImagePoint(pUL.X, pUL.Y);
			_pur = new ImagePoint(pUR.X, pUR.Y);
			_pll = new ImagePoint(pLL.X, pLL.Y);
			_plr = new ImagePoint(pLR.X, pLR.Y);
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

		public ImagePoint CenterPoint
		{
			get
			{
				double x = Math.Min(Math.Min(this.Pul.X, this.Pur.X), Math.Min(this.Pll.X, this.Plr.X));
				double r = Math.Max(Math.Max(this.Pul.X, this.Pur.X), Math.Max(this.Pll.X, this.Plr.X));
				double y = Math.Min(Math.Min(this.Pul.Y, this.Pur.Y), Math.Min(this.Pll.Y, this.Plr.Y));
				double b = Math.Max(Math.Max(this.Pul.Y, this.Pur.Y), Math.Max(this.Pll.Y, this.Plr.Y));

				return new ImagePoint(Convert.ToInt32((x + r) / 2.0), Convert.ToInt32((y + b) / 2.0));
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

			_pul = Rotation.RotatePoint(_pul, centerX, centerY, angle);
			_pur = Rotation.RotatePoint(_pur, centerX, centerY, angle);
			_pll = Rotation.RotatePoint(_pll, centerX, centerY, angle);
			_plr = Rotation.RotatePoint(_plr, centerX, centerY, angle);
		}
		#endregion

		#region GetRatioClip()
		public RatioClip GetRatioClip(double bitmapW, double bitmapH)
		{
			double angle = this.Angle;

			if (angle != 0)
			{
				ImagePoint center = this.CenterPoint;

				ImagePoint pUL = Rotation.RotatePoint(_pul, center, -angle);
				ImagePoint pUR = Rotation.RotatePoint(_pur, center, -angle);
				ImagePoint pLL = Rotation.RotatePoint(_pll, center, -angle);

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
