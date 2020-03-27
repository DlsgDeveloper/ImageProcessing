using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing.Rasters.Corners
{
	public class Corner
	{
		public int X { get; private set; }
		public int Y { get; private set; }

		public Corner(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}
	}
}
