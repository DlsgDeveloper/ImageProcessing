using System;
using System.Drawing;

using BIP.Geometry;


namespace ImageProcessing.IpSettings
{
	/// <summary>
	/// Summary description for ClipInt.
	/// </summary>
	class ClipInt
	{
		Rectangle	clip;
		double		skew;
		
		
		#region constructor
		public ClipInt(RatioRect rect, double skew, Size imageSize)
		{
			this.clip = new Rectangle(Convert.ToInt32(rect.X * imageSize.Width), Convert.ToInt32(rect.Y * imageSize.Height),
				Convert.ToInt32(rect.Width * imageSize.Width), Convert.ToInt32(rect.Height * imageSize.Height));

			this.skew = skew;
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		public Rectangle	RectangleNotSkewed	{ get { return this.clip;} }
		public bool			IsSkewed			{ get { return (skew > 0.000001 || skew < -0.000001); } }
		public Point		Center				{ get { return new Point(clip.X + clip.Width / 2, clip.Y + clip.Height / 2);} }
		public int			Width				{ get { return clip.Width; } }
		public int			Height				{ get { return clip.Height; } }

		/// <summary>
		/// Skew is positive in the clock-wise direction.
		/// </summary>
		public double Skew { get { return skew; } }

		public Point PointUL { get { return TransferUnskewedToSkewedPoint(new Point(clip.X, clip.Y)); } }
		public Point PointUR { get { return TransferUnskewedToSkewedPoint(new Point(clip.Right, clip.Y)); } }
		public Point PointLL { get { return TransferUnskewedToSkewedPoint(new Point(clip.X, clip.Bottom)); } }
		public Point PointLR { get { return TransferUnskewedToSkewedPoint(new Point(clip.Right, clip.Bottom)); } }
		public Point[] Points { get { return new Point[] { PointUL, PointUR, PointLR, PointLL }; } }

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region TransferSkewedToUnskewedPoint()
		public Point TransferSkewedToUnskewedPoint(Point point)
		{
			if (this.IsSkewed)
				return Point.Round(ImageProcessing.BigImages.Rotation.RotatePoint(point, this.Center, -this.Skew));
			else
				return point;
		}
		#endregion

		#region TransferUnskewedToSkewedPoint()
		public Point TransferUnskewedToSkewedPoint(Point point)
		{
			if (this.IsSkewed)
				return Point.Round(ImageProcessing.BigImages.Rotation.RotatePoint((PointF)point, (PointF)this.Center, this.Skew));
			else
				return point;
		}
		#endregion

		#endregion

	}
}
