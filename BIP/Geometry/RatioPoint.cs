using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BIP.Geometry
{
	public struct RatioPoint : IEquatable<RatioPoint>
	{
		public double X;
		public double Y;


		#region constructor
		public RatioPoint(double x, double y)
		{
			this.X = x;
			this.Y = y;
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		public bool IsEmpty { get { return (this.X == 0) && (this.Y == 0); } }
		public static RatioPoint Empty { get { return new RatioPoint(0, 0); } }

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region Offset()
		public void Offset(double dx, double dy)
		{
			this.X = Math.Max(0, Math.Min(1, this.X + dx));
			this.Y = Math.Max(0, Math.Min(1, this.Y + dy));
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
			if (obj is RatioPoint ratioPoint)
				return (this.X == ratioPoint.X && this.Y == ratioPoint.Y);
			else
				return false;
		}

		public bool Equals(RatioPoint ratioPoint)
		{
			//if (ratioPoint == null)
			//	return false;

			return (this.X == ratioPoint.X && this.Y == ratioPoint.Y);
		}
		#endregion

		#region Equals()
		public override int GetHashCode()
		{
			return (this.X.GetHashCode() | this.Y.GetHashCode());
		}
		#endregion

		#region Clone()
		public RatioPoint Clone()
		{
			return new RatioPoint(this.X, this.Y);
		}
		#endregion

		#endregion

	}
}
