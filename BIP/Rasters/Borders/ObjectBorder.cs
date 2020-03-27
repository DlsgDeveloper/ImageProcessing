using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing.Rasters.Borders
{
	public class ObjectBorder
	{
		public readonly BorderPoint		BorderPoint;
		public readonly BorderPoints	BorderPoints = new BorderPoints();


		public ObjectBorder(BorderPoint borderPoint)
		{
			this.BorderPoint = borderPoint;
		}
	}
}
