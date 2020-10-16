using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing.Books.TedsBookCurveCorrection
{
	public class SingleLineRegion
	{
		public System.Drawing.Point			ImagePoint { get; }
		public System.Drawing.Rectangle		ImageClip { get; }
		public double						AverageGray { get; set; } = 255;

		/// <summary>
		/// In Radians, BestAngle 180 / Math.PI to get it in degrees.
		/// </summary>
		public double						BestAngle { get; set; } = 0;

		public SingleLineRegion(Point imagePoint, System.Drawing.Rectangle imageClip)
		{
			this.ImagePoint = imagePoint;
			this.ImageClip = imageClip;
		}
	}
}
