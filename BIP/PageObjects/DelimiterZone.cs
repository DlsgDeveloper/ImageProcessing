using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ImageProcessing.PageObjects
{
	public class DelimiterZone
	{
		Point pUL;
		Point pUR;
		Point pLL;
		Point pLR;

		#region constructor
		public DelimiterZone(Point pUL, Point pUR, Point pLL, Point pLR)
		{
			this.pUL = pUL;
			this.pUR = pUR;
			this.pLL = pLL;
			this.pLR = pLR;
		}

		public DelimiterZone(Rectangle rect)
		{
			this.pUL = new Point(rect.X, rect.Y);
			this.pUR = new Point(rect.Right, rect.Y);
			this.pLL = new Point(rect.X, rect.Bottom);
			this.pLR = new Point(rect.Right, rect.Bottom);
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		public Point Pul { get { return pUL; } }
		public Point Pur { get { return pUR; } }
		public Point Pll { get { return pLL; } }
		public Point Plr { get { return pLR; } }
		public int	Area { get { return (pLR.X - pUL.X) * (pLR.Y - pUL.Y); } }

		#region Path
		public GraphicsPath Path 
		{ 
			get 
			{
				GraphicsPath path = new GraphicsPath();
				path.AddLine(pUL, pUR);
				path.AddLine(pUR, pLR);
				path.AddLine(pLR, pLL);
				return path;
			} 
		}
		#endregion

		#region PathToDraw()
		public GraphicsPath PathToDraw
		{
			get
			{
				GraphicsPath path = new GraphicsPath();
				path.AddLine(pUL.X + 2, pUL.Y + 2, pUR.X - 2, pUR.Y + 2);
				path.AddLine(pUR.X - 2, pUR.Y + 2, pLR.X - 2, pLR.Y - 2);
				path.AddLine(pLR.X - 2, pLR.Y - 2, pLL.X + 2, pLL.Y - 2);
				return path;
			}
		}
		#endregion

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region AreIdentical()
		public static bool AreIdentical(DelimiterZone zone1, DelimiterZone zone2)
		{
			if (zone1 == null && zone2 == null)
				return true;
			if ((zone1 == null && zone2 != null) || (zone1 != null && zone2 == null))
				return false;
			
			if ((Arithmetic.Distance(zone1.Pul, zone2.Pul) > 50) || (Arithmetic.Distance(zone1.Pur, zone2.Pur) > 50) ||
				(Arithmetic.Distance(zone1.Pll, zone2.Pll) > 50) || (Arithmetic.Distance(zone1.Plr, zone2.Plr) > 50))
				return false;

			return true;
		}
		#endregion

		#region GetHashCode()
		public override int GetHashCode()
		{
			return 0;
		}
		#endregion

		#region ToString()
		public override string ToString()
		{
			return string.Format("Zone, [{0:0000},{1:0000}],[{2:0000},{3:0000}], W: {4:0000}, H: {5:0000}", pUL.X, pUL.Y, pLR.X, pLR.Y, pLR.X - pUL.X, pLR.Y - pUL.Y);
		}
		#endregion

		#region ValidCorners()
		public double SharedAreaInPercent(Rectangle rect)
		{
			bool containsUL = Contains(rect.Location);
			bool containsUR = Contains(new Point(rect.Right, rect.Y));
			bool containsLL = Contains(new Point(rect.X, rect.Bottom));
			bool containsLR = Contains(new Point(rect.Right, rect.Bottom));

			if (containsUL && containsUR && containsLL && containsLR)
				return 1;
			else if ((containsUL == false && containsUR == false && containsLL == false && containsLR == false))
				return 0;
			else
			{
				List<Point> intersectionObject = GetIntersection(rect, containsUL, containsUR, containsLL, containsLR);
				double sharedArea = GetObjectArea(intersectionObject.ToArray());
				return sharedArea / (rect.Width * rect.Height);
			}			
		}
		#endregion

		#region Contains()
		public bool Contains(Point point)
		{
			//4-corner object contains point, if the angle between point and corner is between 
			//angles of 2 neighbourhood sides for 2 opposite corners.
			double angleP, angle1, angle2 ;

			angleP = Arithmetic.GetOrientedAngle(point, pUL);
			angle1 = Arithmetic.GetOrientedAngle(pUR, pUL);
			angle2 = Arithmetic.GetOrientedAngle(pLL, pUL);

			if (angle1 > angle2)
				angle2 += Math.PI * 2;
			if (angleP < angle1)
				angleP += Math.PI * 2;

			if ((angleP < angle1) || (angleP > angle2))
				return false;

			angleP = Arithmetic.GetOrientedAngle(point, pLR);
			angle1 = Arithmetic.GetOrientedAngle(pUR, pLR);
			angle2 = Arithmetic.GetOrientedAngle(pLL, pLR);

			if (((angleP < angle1) && (angleP < angle2)) || ((angleP > angle1) && (angleP > angle2)))
				return false;

			return true;
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region GetIntersection()
		private List<Point> GetIntersection(Rectangle rect, bool containsUL, bool containsUR, bool containsLL, bool containsLR)
		{
			List<Point> sharedPoints = new List<Point>();
			Point rUL = rect.Location;
			Point rUR = new Point(rect.Right, rect.Y);
			Point rLL = new Point(rect.X, rect.Bottom);
			Point rLR = new Point(rect.Right, rect.Bottom);

			if (containsUL)
				sharedPoints.Add(rUL);
			if (containsUR)
				sharedPoints.Add(rUR);
			if (containsLL)
				sharedPoints.Add(rLL);
			if (containsLR)
				sharedPoints.Add(rLR);

			if (rect.Contains(pUL))
				sharedPoints.Add(pUL);
			if (rect.Contains(pUR))
				sharedPoints.Add(pUR);
			if (rect.Contains(pLL))
				sharedPoints.Add(pLL);
			if (rect.Contains(pLR))
				sharedPoints.Add(pLR);

			double x = 0, y = 0;
			Point[] zonePoints = new Point[] { pUL, pUR, pLR, pLL, pUL };
			Point[] rectPoints = new Point[] { rUL, rUR, rLR, rLL, rUL };

			for(int i = 0; i < 4; i++)
				for(int j = 0; j < 4; j++)
					if (GetInterceptPoint(zonePoints[i], zonePoints[i + 1], rectPoints[j], rectPoints[j + 1], ref x, ref y))
						sharedPoints.Add(new Point(Convert.ToInt32(x), Convert.ToInt32(y)));

			return sharedPoints;
		}
		#endregion

		#region GetInterceptPoint()
		private bool GetInterceptPoint(Point p1, Point p2, Point p3, Point p4, ref double x, ref double y)
		{
			Line2D line1 = new Line2D(p1, p2);
			Line2D line2 = new Line2D(p3, p4);

			if (line1.InterceptPoint(line2, ref x, ref y))
			{
				if ((x >= Math.Min(p1.X, p2.X) && x <= Math.Max(p1.X, p2.X)) && (x >= Math.Min(p3.X, p4.X) && x <= Math.Max(p3.X, p4.X)) &&
					(y >= Math.Min(p1.Y, p2.Y) && y <= Math.Max(p1.Y, p2.Y)) && (y >= Math.Min(p3.Y, p4.Y) && y <= Math.Max(p3.Y, p4.Y)))
				{
					return true;
				}
			}

			return false;
		}
		#endregion

		#region GetObjectArea()
		private double GetObjectArea(Point[] points)
		{
			double content = 0;
			if (points == null || points.Length < 3)
				return 0;

			List<AnglePoint> anglePoints = new List<AnglePoint>();

			for (int i = 0; i < points.Length; i++)
			{
				double angle = Math.Atan2(points[i].Y - points[0].Y, points[i].X - points[0].X);

				anglePoints.Add(new AnglePoint(angle, points[i]));
			}

			anglePoints.Sort();

			for (int i = 1; i < points.Length - 1; i++)
				content += GetTriangleArea(anglePoints[0].Point, anglePoints[i].Point, anglePoints[i + 1].Point);

			return content;
		}
		#endregion

		#region GetTriangleArea()
		public static double GetTriangleArea(Point p1, Point p2, Point p3)
		{
			double side = Arithmetic.Distance(p1, p2);
			double angle12 = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);// Arithmetic.GetAngle(p1, p2);
			double angle13 = Math.Atan2(p3.Y - p1.Y, p3.X - p1.X); //Arithmetic.GetAngle(p1, p3);
			double angle = Math.Abs(angle12 - angle13);

			/*while (angle > Math.PI)
				angle -= Math.PI * 2;
			while (angle < -Math.PI)
				angle += Math.PI * 2;*/

			double distance = Math.Sin(angle) * Arithmetic.Distance(p1, p3);

			return Math.Abs(side * distance / 2.0);
		}
		#endregion

		#region struct AnglePoint
		private class AnglePoint : IComparable
		{
			public double Angle;
			public Point Point;

			public AnglePoint(double angle, Point point)
			{
				this.Angle = angle;
				this.Point = point;
			}

			public int CompareTo(object anglePoint)
			{			
				if (this.Angle < ((AnglePoint)anglePoint).Angle)
					return -1;
				else if (this.Angle > ((AnglePoint)anglePoint).Angle)
					return 1;
				else
					return 0;
			}

		}
		#endregion

		#endregion

	}
}
