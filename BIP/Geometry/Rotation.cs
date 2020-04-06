using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIP.Geometry
{
	public class Rotation
	{

		#region RotatePoint()
		/// <summary>
		/// Rotates point around center point, positive angle is in clockwise direction.
		/// </summary>
		/// <param name="p"></param>
		/// <param name="centerPoint"></param>
		/// <param name="angle"></param>
		/// <returns></returns>
		public static RatioPoint RotatePoint(RatioPoint ratioPoint, RatioPoint centroid, double angle, double widthHeightRatio)
		{
			RatioPoint p = new RatioPoint(ratioPoint.X * widthHeightRatio, ratioPoint.Y);
			RatioPoint centerPoint = new RatioPoint(centroid.X * widthHeightRatio, centroid.Y);

			double beta = Math.Atan2((double)(centerPoint.Y - p.Y), (double)(centerPoint.X - p.X));
			double m = Math.Sqrt((double)(((centerPoint.X - p.X) * (centerPoint.X - p.X)) + ((centerPoint.Y - p.Y) * (centerPoint.Y - p.Y))));
			double xShifted = centerPoint.X - (Math.Cos(beta + angle) * m);
			double yShifted = centerPoint.Y - (Math.Sin(beta + angle) * m);

			return new RatioPoint(xShifted / widthHeightRatio, yShifted);
		}

		/// <summary>
		/// Rotates point around center point, positive angle is in clockwise direction.
		/// </summary>
		/// <param name="sourceWidth"></param>
		/// <param name="sourceHeight"></param>
		/// <param name="ratioPoint"></param>
		/// <param name="centroid"></param>
		/// <param name="angle"></param>
		/// <returns></returns>
		public static ImagePoint RotatePoint(ImagePoint p, ImagePoint centerPoint, double angle)
		{
			return RotatePoint(p, centerPoint.X, centerPoint.Y, angle);
		}

		/// <summary>
		/// Rotates point around center point, positive angle is in clockwise direction.
		/// </summary>
		/// <param name="sourceWidth"></param>
		/// <param name="sourceHeight"></param>
		/// <param name="ratioPoint"></param>
		/// <param name="centroid"></param>
		/// <param name="angle"></param>
		/// <returns></returns>
		public static ImagePoint RotatePoint(ImagePoint p, double centerX, double centerY, double angle)
		{
			ImagePointD pD = RotatePoint(p.X, p.Y, centerX, centerY, angle);

			return new ImagePoint(Convert.ToInt32(pD.X), Convert.ToInt32(pD.Y));
		}

		/// <summary>
		/// Rotates point around center point, positive angle is in clockwise direction.
		/// </summary>
		/// <param name="sourceWidth"></param>
		/// <param name="sourceHeight"></param>
		/// <param name="ratioPoint"></param>
		/// <param name="centroid"></param>
		/// <param name="angle"></param>
		/// <returns></returns>
		public static ImagePointD RotatePoint(ImagePointD p, ImagePointD centerPoint, double angle)
		{
			return RotatePoint(p, centerPoint.X, centerPoint.Y, angle);
		}

		/// <summary>
		/// Rotates point around center point, positive angle is in clockwise direction.
		/// </summary>
		/// <param name="sourceWidth"></param>
		/// <param name="sourceHeight"></param>
		/// <param name="ratioPoint"></param>
		/// <param name="centroid"></param>
		/// <param name="angle"></param>
		/// <returns></returns>
		public static ImagePointD RotatePoint(ImagePointD p, double centerX, double centerY, double angle)
		{
			return RotatePoint(p.X, p.Y, centerX, centerY, angle);
		}

		/// <summary>
		/// Rotates point around center point, positive angle is in clockwise direction.
		/// </summary>
		/// <param name="sourceWidth"></param>
		/// <param name="sourceHeight"></param>
		/// <param name="ratioPoint"></param>
		/// <param name="centroid"></param>
		/// <param name="angle"></param>
		/// <returns></returns>
		public static ImagePointD RotatePoint(double pX, double pY, double centerX, double centerY, double angle)
		{
			double beta = Math.Atan2((double)(centerY - pY), (double)(centerX - pX));
			double m = Math.Sqrt((double)(((centerX - pX) * (centerX - pX)) + ((centerY - pY) * (centerY - pY))));
			double xShifted = centerX - (Math.Cos(beta + angle) * m);
			double yShifted = centerY - (Math.Sin(beta + angle) * m);

			return new ImagePointD(xShifted, yShifted);
		}
		#endregion

		#region GetAngle()
		public static double GetAngle(ImagePoint p1, ImagePoint p2)
		{
			return Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
		}
		#endregion

		#region GetAngle()
		public static double GetAngle(ImagePointD p1, ImagePointD p2)
		{
			return Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
		}
		#endregion

	}

}
