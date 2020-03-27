using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing.Rasters.Borders
{
	public class BorderPoints : List<BorderPoint>
	{
		//PUBLIC METHODS
		#region public methods

		#region Contains()
		public bool Contains(int x, int y)
		{
			foreach (BorderPoint p in this)
				if (p.X == x && p.Y == y)
					return true;

			return false;
		}
		#endregion

		#endregion
	}
}
