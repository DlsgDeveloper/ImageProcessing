using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BIP.Geometry
{
	public struct RatioSize
	{
		public double Width;
		public double Height;

		public RatioSize(double width, double height)
		{
			this.Width = width;
			this.Height = height;
		}

		//PUBLIC PROPERTIES
		#region public properties

		public static RatioSize Empty { get { return new RatioSize(0, 0); } }
		public bool IsEmpty { get { return (this.Width == 0 && this.Height == 0); } }

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region ToString()
		public override string ToString()
		{
			return string.Format("W={0}, H={1}", this.Width.ToString("F3"), this.Height.ToString("F3"));
		}
		#endregion

		#endregion
	}
}
