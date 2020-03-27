using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ImageProcessing
{

	public class Edge
	{
		Point point1;
		Point point2;

		public Edge()
		{
			this.point1 = Point.Empty;
			this.point2 = Point.Empty;
		}

		public Edge(Point point1, Point point2)
		{
			this.point1 = point1;
			this.point2 = point2;
		}

		public Point Point1 { get { return point1; } }
		public Point Point2 { get { return point2; } }
		public bool Exists { get { return (!point1.IsEmpty || !point2.IsEmpty); } }
		public bool IsVertical { get { return (Math.Abs(point1.X - point2.X) < Math.Abs(point1.Y - point2.Y)); } }

		public double Length
		{
			get
			{
				if (this.Exists)
					return Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2));

				return 0;
			}
		}

		public double Angle
		{
			get
			{
				if (this.Exists)
					return Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);

				return 0;
			}
		}

		public void Offset(int dx, int dy)
		{
			point1.Offset(dx, dy);
			point2.Offset(dx, dy);
		}

	}

}
