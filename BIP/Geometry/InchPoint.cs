using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BIP.Geometry
{
	public struct InchPoint
	{
		public double X;
		public double Y;

		public InchPoint(double x, double y)
		{
			this.X = x;
			this.Y = y;
		}

		#region ToString()
		public override string ToString()
		{
			return string.Format("{0}, {1}", this.X.ToString("F3"), this.Y.ToString("F3"));
		}
		#endregion

	}
}
