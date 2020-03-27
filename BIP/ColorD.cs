using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing
{
	/// <summary>
	/// values are in interval <0, 255>
	/// </summary>
	public struct ColorD
	{
		public double Red;
		public double Green;
		public double Blue;

		public ColorD(double red, double green, double blue)
		{
			this.Red = red;
			this.Green = green;
			this.Blue = blue;
		}
	}
}
