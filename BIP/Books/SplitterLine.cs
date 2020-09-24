using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIP.Books
{
	public class SplitterLine
	{
		public System.Drawing.Point PointTop { get; }
		public System.Drawing.Point PointBottom { get; }
		public double Confidence { get; }

		public SplitterLine(System.Drawing.Point pointTop, System.Drawing.Point pointBottom, double confidence)
		{
			this.PointTop = pointTop;
			this.PointBottom = pointBottom;
			this.Confidence = confidence;
		}


		// PUBLIC METHODS
		#region public methods

		#region ToString()
		public override string ToString()
		{
			return string.Format("Top: [{0},{1}], Bottom: [{2},{3}], Conf: {4}", this.PointTop.X, this.PointTop.Y, this.PointBottom.X, this.PointBottom.Y, this.Confidence);
		}
		#endregion

		#endregion
	}
}
