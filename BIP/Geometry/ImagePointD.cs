using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIP.Geometry
{
	/// <summary>
	/// x, y are double
	/// </summary>
	public class ImagePointD : IEquatable<ImagePointD>
	{
		public double X { get; set; }
		public double Y { get; set; }


		public ImagePointD(double x, double y)
		{
			this.X = x;
			this.Y = y;
		}


		//PUBLIC METHODS
		#region public methods

		#region Offset()
		public ImagePointD Clone()
		{
			return new ImagePointD(this.X, this.Y);
		}
		#endregion

		#region Offset()
		public void Offset(double dx, double dy)
		{
			this.X += dx;
			this.Y += dy;
		}
		#endregion

		#region ToString()
		public override string ToString()
		{
			return string.Format("{0}, {1}", this.X.ToString("F3"), this.Y.ToString("F3"));
		}
		#endregion

		#region Equals()
		public override bool Equals(object obj)
		{
			if (obj is ImagePointD ratioPoint)
				return (this.X == ratioPoint.X && this.Y == ratioPoint.Y);
			else
				return false;
		}

		public bool Equals(ImagePointD ratioPoint)
		{
			if (ratioPoint == null)
				return false;

			return (this.X == ratioPoint.X && this.Y == ratioPoint.Y);
		}
		#endregion

		#region GetHashCode()
		public override int GetHashCode()
		{
			return (this.X.GetHashCode() | this.Y.GetHashCode());
		}
		#endregion

		#region operator ==
		public static bool operator ==(ImagePointD p1, ImagePointD p2)
		{
			if (((object)p1) == null || ((object)p2) == null)
				return Object.Equals(p1, p2);

			return p1.Equals(p2);
		}
		#endregion

		#region operator !=
		public static bool operator !=(ImagePointD p1, ImagePointD p2)
		{
			if (((object)p1) == null || ((object)p2) == null)
				return !Object.Equals(p1, p2);

			return !(p1.Equals(p2));
		}
		#endregion

		#endregion

	}
}
