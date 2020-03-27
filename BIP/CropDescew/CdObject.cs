using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

using BIP.Geometry;



namespace ImageProcessing.CropDeskew
{

	public class CdObject
	{
		RatioPoint			cornerUl;
		RatioPoint			cornerUr;
		RatioPoint			cornerLl;
		RatioPoint			cornerLr;
		double			angle;
		double			width; 
		double			height; 
		int				validCorners = 0;
		List<double>	validAngles = new List<double>();
		double			widthHeightRatio; // width height ratio, needed for rotation

		public readonly ImageProcessing.CropDeskew.Edge EdgeL = null;
		public readonly ImageProcessing.CropDeskew.Edge EdgeT = null;
		public readonly ImageProcessing.CropDeskew.Edge EdgeR = null;
		public readonly ImageProcessing.CropDeskew.Edge EdgeB = null;

		#region constructor	
		public CdObject(double angle, RatioPoint ulCorner, RatioPoint urCorner, RatioPoint llCorner, RatioPoint lrCorner, double widthHeightRatio)
		{
			this.angle = angle;
			this.cornerUl = ulCorner;
			this.cornerUr = urCorner;
			this.cornerLl = llCorner;
			this.cornerLr = lrCorner;
			this.width = this.cornerUr.X - this.cornerUl.X;
			this.height = this.cornerLl.Y - this.cornerUl.Y;
			this.widthHeightRatio = widthHeightRatio;

			double ulAngle = GetAngle(urCorner, ulCorner, llCorner);
			double urAngle = GetAngle(lrCorner, urCorner, ulCorner);
			double lrAngle = GetAngle(llCorner, lrCorner, urCorner);
			double llAngle = GetAngle(ulCorner, llCorner, lrCorner);

			if (ulAngle != 0)
			{
				EdgeL = new ImageProcessing.CropDeskew.Edge(ulCorner, llCorner);
				EdgeT = new ImageProcessing.CropDeskew.Edge(ulCorner, urCorner);
				validAngles.Add(ulAngle);
				validCorners++;
			}
			if (urAngle != 0)
			{
				EdgeT = new ImageProcessing.CropDeskew.Edge(ulCorner, urCorner);
				EdgeR = new ImageProcessing.CropDeskew.Edge(urCorner, lrCorner);
				validAngles.Add(urAngle);
				validCorners++;
			}
			if (lrAngle != 0)
			{
				EdgeR = new ImageProcessing.CropDeskew.Edge(urCorner, lrCorner);
				EdgeB = new ImageProcessing.CropDeskew.Edge(lrCorner, llCorner);
				validAngles.Add(lrAngle);
				validCorners++;
			}
			if (llAngle != 0)
			{
				EdgeL = new ImageProcessing.CropDeskew.Edge(ulCorner, llCorner);
				EdgeB = new ImageProcessing.CropDeskew.Edge(lrCorner, llCorner);
				validAngles.Add(llAngle);
				validCorners++;
			}
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties
		public RatioPoint	CornerUl { get { return this.cornerUl; } }
		public RatioPoint	CornerLl { get { return this.cornerLl; } }
		public RatioPoint	CornerUr { get { return this.cornerUr; } }
		public RatioPoint	CornerLr { get { return this.cornerLr; } }
		public RatioPoint	Centroid { get { return new RatioPoint(CornerUl.X + (CornerLr.X - CornerUl.X) / 2.0F, CornerUl.Y + (CornerLr.Y - CornerUl.Y) / 2.0F); } }
		public double		WidthHeightRatio { get { return this.widthHeightRatio; } }
		public bool			Inclined { get { return this.angle != 0; } }
		
		public double	Width { get { return this.width; } }
		public double	Height { get { return this.height; } }
		public int		ValidCorners { get { return this.validCorners; } }

		/// <summary>
		/// clip skew in radians. Angle bigger than 0 - clockwise, Angle smaller than 0 - counter clockwise
		/// </summary>
		public double Skew { get { return this.angle; } }

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Offset()
		public void Offset(double dx, double dy)
		{
			cornerUl.X += dx;
			cornerUl.Y += dy;

			if (EdgeL != null)
				EdgeL.Offset((float)dx, (float)dy);
			if (EdgeT != null)
				EdgeT.Offset((float)dx, (float)dy);
			if (EdgeR != null)
				EdgeR.Offset((float)dx, (float)dy);
			if (EdgeB != null)
				EdgeB.Offset((float)dx, (float)dy);
		}
		#endregion

		#region Inflate()
		public void Inflate(double d)
		{
			this.cornerUl.X -= d;
			this.cornerUl.Y -= d;
			this.cornerUr.X += d;
			this.cornerUr.Y -= d;
			this.cornerLl.X -= d;
			this.cornerLl.Y += d;
			this.cornerLr.X += d;
			this.cornerLr.Y += d;

			this.width = this.cornerUr.X - this.cornerUl.X;
			this.height = this.cornerLl.Y - this.cornerUl.Y;
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region GetAngle()
		private double GetAngle(RatioPoint side1, RatioPoint middle, RatioPoint side2)
		{
			double angle = (Math.Atan2(side1.X - middle.X, side1.Y - middle.Y) - Math.Atan2(side2.X - middle.X, side2.Y - middle.Y));

			if (angle < 0)
				angle = -angle;

			while (angle > Math.PI)
				angle -= Math.PI;

			//if angle is > 91 degrees or smaller than 89 degrees, return 0
			if (angle > (Math.PI / 2 + Math.PI / 180) || angle < (Math.PI / 2 - Math.PI / 180))
				return 0;

			return angle;
		}
		#endregion

		#endregion

	}

}
