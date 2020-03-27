using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing.Rasters.Corners
{
	public class ObjectCorners
	{
		public Corner UlCorner { get; set; }
		public Corner UrCorner { get; set; }
		public Corner LlCorner { get; set; }
		public Corner LrCorner { get; set; }

		public ObjectCorners(int width, int height)
		{
			this.UlCorner = new Corner(0, 0);
			this.UrCorner = new Corner(width - 1, 0);
			this.LlCorner = new Corner(0, height - 1);
			this.LrCorner = new Corner(width - 1, height - 1);
		}
	}
}
