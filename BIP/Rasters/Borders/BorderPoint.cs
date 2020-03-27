using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing.Rasters.Borders
{
	public class BorderPoint
	{
		public ushort X;
		public ushort Y;

		public BorderPoint(int x, int y)
		{
			this.X = (ushort)x;
			this.Y = (ushort)y;
		}

		public BorderPoint(ushort x, ushort y)
		{
			this.X = x;
			this.Y = y;
		}

		public void Shift(int dx, int dy)
		{
			this.X = (ushort)((this.X + dx < 0) ? 0 : ((this.X + dx > 65535) ? 65535 : this.X + dx));
			this.Y = (ushort)((this.Y + dy < 0) ? 0 : ((this.Y + dy > 65535) ? 65535 : this.Y + dy));
		}

		public override string ToString()
		{
			return "X = " + X.ToString() + ", Y = " + Y.ToString();
		}
	}
}
