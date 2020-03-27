﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BIP.Geometry
{
	public struct InchSize
	{
		public double Width;
		public double Height;

		public InchSize(double width, double height)
		{
			this.Width = width;
			this.Height = height;
		}

		//PUBLIC PROPERTIES
		#region public properties

		public static InchSize Empty { get { return new InchSize(0, 0); } }
		public bool IsEmpty { get { return (this.Width == 0 && this.Height == 0); } }

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region ToString()
		public override string ToString()
		{
			return string.Format("W={0}, H={1}", this.Width.ToString("F3"), this.Height.ToString("F3"));
		}
		#endregion


		#endregion

	}
}
