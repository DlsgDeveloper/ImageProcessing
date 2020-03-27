using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIP.Geometry
{
	public class ImagePoint : IEquatable<ImagePoint>
	{
		public int X { get; set; }
		public int Y { get; set; }


		public ImagePoint(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}


		//PUBLIC METHODS
		#region public methods

		#region Offset()
		public ImagePoint Clone()
		{
			return new ImagePoint(this.X, this.Y);
		}
		#endregion

		#region Offset()
		public void Offset(int dx, int dy)
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
			if (obj is ImagePoint ratioPoint)
				return (this.X == ratioPoint.X && this.Y == ratioPoint.Y);
			else
				return false;
		}

		public bool Equals(ImagePoint ratioPoint)
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
		public static bool operator ==(ImagePoint p1, ImagePoint p2)
		{
			if (((object)p1) == null || ((object)p2) == null)
				return Object.Equals(p1, p2);

			return p1.Equals(p2);
		}
		#endregion

		#region operator !=
		public static bool operator !=(ImagePoint p1, ImagePoint p2)
		{
			if (((object)p1) == null || ((object)p2) == null)
				return !Object.Equals(p1, p2);

			return !(p1.Equals(p2));
		}
		#endregion

		#endregion

	}
}
