using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using BIP.Geometry;

namespace ImageProcessing
{
	public static class Arithmetic
	{

		#region Distance()
		public static double Distance(Point p1, Point p2)
		{
			return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
		}

		public static double Distance(Point point, double x, double y)
		{
			return Math.Sqrt((point.X - x) * (point.X - x) + (point.Y - y) * (point.Y - y));
		}

		public static int Distance(Rectangle r1, Rectangle r2)
		{
			if (r1.IntersectsWith(r2))
				return 0;

			if ((r1.X >= r2.X && r1.X <= r2.Right) || (r2.X >= r1.X && r2.X <= r1.Right))
			{
				if (r1.Y < r2.Y)
					return r2.Y - r1.Bottom;
				else
					return r1.Y - r2.Bottom;
			}

			if ((r1.Y >= r2.Y && r1.Y <= r2.Bottom) || (r2.Y >= r1.Y && r2.Y <= r1.Bottom))
			{
				if (r1.X < r2.X)
					return r2.X - r1.Right;
				else
					return r1.X - r2.Right;
			}

			double d1 = Arithmetic.Distance(new Point(r1.Right, r1.Bottom), new Point(r2.Left, r1.Top));
			double d2 = Arithmetic.Distance(new Point(r1.Left, r1.Bottom), new Point(r2.Right, r1.Top));
			double d3 = Arithmetic.Distance(new Point(r1.Left, r1.Top), new Point(r2.Right, r1.Bottom));
			double d4 = Arithmetic.Distance(new Point(r1.Right, r1.Top), new Point(r2.Left, r1.Bottom));

			return (int)Math.Min(Math.Min(d1, d2), Math.Min(d3, d4));
		}
		#endregion

		#region GetAngle()
		/// <summary>
		/// Returns angle in radians.
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		public static double GetAngle(Point p1, Point p2)
		{
			return GetAngle(p1.X, p1.Y, p2.X, p2.Y);
		}

		/// <summary>
		/// Returns angle in radians.
		/// </summary>
		/// <param name="x1"></param>
		/// <param name="y1"></param>
		/// <param name="x2"></param>
		/// <param name="y2"></param>
		/// <returns></returns>
		public static double GetAngle(double x1, double y1, double x2, double y2)
		{
			double angle = Math.Atan2(y2 - y1, x2 - x1);

			while (angle > (Math.PI / 2))
				angle -= Math.PI;
			while (angle < -(Math.PI / 2))
				angle += Math.PI;

			return angle;
		}
		#endregion

		#region GetOrientedAngle()
		public static double GetOrientedAngle(Point p1, Point p2)
		{
			double angle = Math.Atan2(p1.Y - p2.Y, p1.X - p2.X);

			if (angle < 0)
				angle += 2 * Math.PI;

			return angle;
		}
		#endregion

		#region Get1stOr2ndSectorAngle()
		public static double Get1stOr2ndSectorAngle(double angle)
		{
			while (angle > Math.PI / 2)
				angle -= Math.PI;
			while (angle < -Math.PI / 2)
				angle += Math.PI;

			return angle;
		}
		#endregion

		#region AreInLine()
		/// <summary>
		/// Function determines if 2 rectangles are next to each other.
		/// </summary>
		/// <param name="r1">First rectangle.</param>
		/// <param name="r2">Second rectangle.</param>
		/// <param name="shareCoefficient">Fraction in range of <0,1>. This number specifies
		/// the minimum portion of shorter rectangle height, that must be in line with the second rectangle.</param>
		/// <returns>True, if in line, False otherwise.</returns>
		public static bool AreInLine(Rectangle r1, Rectangle r2, double shareCoefficient)
		{
			int sharedHeight = Math.Min(r1.Bottom, r2.Bottom) - Math.Max(r1.Y, r2.Y);
			int shorterHeight = Math.Min(r1.Height, r2.Height);

			if (sharedHeight > shorterHeight * shareCoefficient)
				return true;

			return false;
		}
		
		public static bool AreInLine(Rectangle r1, Rectangle r2)
		{
			if ((r1.Y >= r2.Y && r1.Y < r2.Bottom) || (r1.Y <= r2.Y && r1.Bottom > r2.Y))
				return true;
				
			return false;
		}
		
		public static bool AreInLine(int top1, int bottom1, int top2, int bottom2)
		{
			if ((top1 >= top2 && top1 < bottom2) || (top1 <= top2 && bottom1 > top2))
				return true;

			return false;
		}
		#endregion

		#region GetX()
		public static double GetX(Point p1, Point p2, int y)
		{
			if (p1.Y == p2.Y)
				return (p1.X + p2.X) / 2.0;
			else
				return p1.X + (y - p1.Y) * (p2.X - p1.X) / (double)(p2.Y - p1.Y);
		}
		#endregion

		#region GetY()
		public static double GetY(PointF p1, PointF p2, float x)
		{
			if (p1.X == p2.X)
				return (p1.Y + p2.Y) / 2.0;
			else
				return p1.Y + (p2.Y - p1.Y) * (x - p1.X) / (double)(p2.X - p1.X);
		}

		public static double GetY(RatioPoint p1, RatioPoint p2, double x)
		{
			if (p1.X == p2.X)
				return (p1.Y + p2.Y) / 2.0;
			else
				return p1.Y + (p2.Y - p1.Y) * (x - p1.X) / (double)(p2.X - p1.X);
		}
		#endregion

		#region ArePointsInLine()
		public static bool ArePointsInLine(PointF[] points)
		{
			if (points.Length < 3)
				return true;
			else
			{
				PointF p0 = points[0];
				PointF pN = points[points.Length - 1];

				if (pN.X - p0.X == 0)
					return true;
				else
				{
					for (int i = 1; i < points.Length - 1; i++)
					{
						float streightY = p0.Y + (pN.Y - p0.Y) * (points[i].X - p0.X) / (pN.X - p0.X);

						if (points[i].Y < streightY - 3 || points[i].Y > streightY + 3)
							return false;
					}
				}

				return true;
			}
		}

		public static bool ArePointsInLine(RatioPoint[] points)
		{
			if (points.Length < 3)
				return true;
			else
			{
				if (points[points.Length - 1].X - points[0].X == 0)
					return true;
				else
				{
					List<double> angles = new List<double>();

					for (int i = 1; i < points.Length; i++)
						angles.Add(Math.Atan2(points[i].Y - points[0].Y, points[i].X - points[0].X));

					double minAngle = angles[0];
					double maxAngle = angles[0];

					foreach (double angle in angles)
					{
						if (minAngle > angle)
							minAngle = angle;
						if (maxAngle < angle)
							maxAngle = angle;
					}

					//max angle difference is 1 degree
					return (maxAngle - minAngle < (Math.PI / 180.0));
				}
			}
		}
		#endregion

		#region HorizontalPixelsShare()
		public static int HorizontalPixelsShare(Rectangle r1, Rectangle r2)
		{
			int x1 = Math.Max(r1.X, r2.X);
			int x2 = Math.Min(r1.Right, r2.Right);

			if (x2 > x1)
				return x2 - x1;
			else
				return 0;
		}
		#endregion

		#region VerticalPixelsShare()
		public static int VerticalPixelsShare(Rectangle r1, Rectangle r2)
		{
			int y1 = Math.Max(r1.Y, r2.Y);
			int y2 = Math.Min(r1.Bottom, r2.Bottom);

			if (y2 > y1)
				return y2 - y1;
			else
				return 0;
		}
		#endregion

	}
}
