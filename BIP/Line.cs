using System;
using System.Drawing;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Line.
	/// </summary>
	public class Line
	{
		Point		point0 = new Point(0, 0);
		Point		pointN = new Point(0, 0);
		float		scale ;   // fScale = 2.000 in flat areas

		public Line(int x0, int y0, int xN, int yN, float lineScale)
		{
			point0.X = x0;
			point0.Y = y0;
			pointN.X = xN;
			pointN.Y = yN;
			scale = lineScale ;
		}

		public Line(Point point1, Point point2, float lineScale)
		{
			point0 = point1;
			pointN = point2;
			scale = lineScale ;
		}

		public Point	Point0 { get { return point0 ; } }
		public Point	PointN { get { return pointN ; } }
		public int		X0 { get { return point0.X ; } }
		public int		Y0 { get { return point0.Y ; } }
		public int		XN { get { return pointN.X ; } }
		public int		YN { get { return pointN.Y ; } }
		public float	Scale { get { return scale ; } }
	}
}
