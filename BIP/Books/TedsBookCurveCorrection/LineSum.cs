using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing.Books.TedsBookCurveCorrection
{
	public class LineSum
	{
		public int Left;
		public int Right;
		public int Y;
		public double Average;


		public override string ToString()
		{
			return string.Format("[{0},{1}], Avg: {2:0}", (this.Left + this.Right / 2), this.Y, this.Average);
		}

	}
}
