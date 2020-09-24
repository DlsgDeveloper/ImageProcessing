using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIP.Books
{

	public class RegressionPoint
	{
		public int		X { get; }
		public int		Y { get; }
		public double					Confidence { get; }

		public RegressionPoint(int x, int y, double confidence)
		{
			this.X = x;
			this.Y = y;
			this.Confidence = confidence;
		}


		// PUBLIC METHODS
		#region public methods

		#region ToString()
		public override string ToString()
		{
			return string.Format("[{0},{1}], Conf: {2}", this.X, this.Y, this.Confidence);
		}
		#endregion

		#endregion


	}
}
