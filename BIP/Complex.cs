using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing
{
	public class Complex
	{
		public float Re;
		public float Im;

		public Complex(float real, float imaginary)
		{
			this.Re = real;
			this.Im = imaginary;
		}

		public override string ToString()
		{
			return String.Format("( {0}, {1}i )", this.Re, this.Im);
		}
	}
}
