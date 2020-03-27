using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIP.Geometry
{
	public class RatioCorners
	{
		RatioPoint _pul, _pur, _pll, _plr;

		public RatioPoint Pul { get { return _pul; } }
		public RatioPoint Pur { get { return _pur; } }
		public RatioPoint Pll { get { return _pll; } }
		public RatioPoint Plr { get { return _plr; } }


		#region constructor
		public RatioCorners(RatioPoint pUL, RatioPoint pUR, RatioPoint pLL, RatioPoint pLR)
		{
			_pul = pUL;
			_pur = pUR;
			_pll = pLL;
			_plr = pLR;
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

		public RatioRect Clip
		{
			get
			{
				double x = Math.Min(Math.Min(this.Pul.X, this.Pur.X), Math.Min(this.Pll.X, this.Plr.X));
				double r = Math.Max(Math.Max(this.Pul.X, this.Pur.X), Math.Max(this.Pll.X, this.Plr.X));
				double y = Math.Min(Math.Min(this.Pul.Y, this.Pur.Y), Math.Min(this.Pll.Y, this.Plr.Y));
				double b = Math.Max(Math.Max(this.Pul.Y, this.Pur.Y), Math.Max(this.Pll.Y, this.Plr.Y));

				return new RatioRect(x, y, r - x, b - y);
			}
		}

		#endregion


		// PUBLIC METHODS
		#region public methods

		#region Offset()
		public void Offset(double offsetX, double offsetY)
		{
			_pul.Offset(offsetX, offsetY);
			_pur.Offset(offsetX, offsetY);
			_pll.Offset(offsetX, offsetY);
			_plr.Offset(offsetX, offsetY);
		}
		#endregion

		#endregion

	}

}
