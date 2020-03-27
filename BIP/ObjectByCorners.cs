using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{

	public class ObjectByCorners
	{
		Point	ulCorner;
		Point	llCorner;
		double	angle;
		int		width;
		int		validCorners = 0;
		float	minAngleToDeskew = 1;
		List<double> validAngles = new List<double>();

		public readonly Edge EdgeL = null;
		public readonly Edge EdgeT = null;
		public readonly Edge EdgeR = null;
		public readonly Edge EdgeB = null;

		#region constructor
		public ObjectByCorners(BitmapData bmpData, Point ulCorner, Point urCorner, Point llCorner, Point lrCorner, Crop clip, float minAngleToDeskew)
		{
			this.ulCorner = ulCorner;
			this.llCorner = llCorner;
			this.minAngleToDeskew = minAngleToDeskew;

			double ulAngle = GetAngle(urCorner, ulCorner, llCorner);
			double urAngle = GetAngle(lrCorner, urCorner, ulCorner);
			double lrAngle = GetAngle(llCorner, lrCorner, urCorner);
			double llAngle = GetAngle(ulCorner, llCorner, lrCorner);

			if (ulAngle != 0)
			{
				EdgeL = new Edge(ulCorner, llCorner);
				EdgeT = new Edge(ulCorner, urCorner);
				validAngles.Add(ulAngle);
				validCorners++;
			}
			if (urAngle != 0)
			{
				EdgeT = new Edge(ulCorner, urCorner);
				EdgeR = new Edge(urCorner, lrCorner);
				validAngles.Add(urAngle);
				validCorners++;
			}
			if (lrAngle != 0)
			{
				EdgeR = new Edge(urCorner, lrCorner);
				EdgeB = new Edge(lrCorner, llCorner);
				validAngles.Add(lrAngle);
				validCorners++;
			}
			if (llAngle != 0)
			{
				EdgeL = new Edge(ulCorner, llCorner);
				EdgeB = new Edge(lrCorner, llCorner);
				validAngles.Add(llAngle);
				validCorners++;
			}

			if (EdgeL != null || EdgeT != null || EdgeR != null || EdgeB != null)
			{
				int validEdgesCount = 0;

				if (EdgeL != null)
				{
					double edgeAngle = EdgeL.Angle - Math.PI / 2;
					while (edgeAngle < -Math.PI / 2)
						edgeAngle += Math.PI;
					while (edgeAngle > Math.PI / 2)
						edgeAngle -= Math.PI;
					angle += edgeAngle;
					validEdgesCount++;
				}

				if (EdgeT != null)
				{
					double edgeAngle = EdgeT.Angle;
					while (edgeAngle < -Math.PI / 2)
						edgeAngle += Math.PI;
					while (edgeAngle > Math.PI / 2)
						edgeAngle -= Math.PI;
					angle += edgeAngle;
					validEdgesCount++;
				}

				if (EdgeR != null)
				{
					double edgeAngle = EdgeR.Angle - Math.PI / 2;
					while (edgeAngle < -Math.PI / 2)
						edgeAngle += Math.PI;
					while (edgeAngle > Math.PI / 2)
						edgeAngle -= Math.PI;
					angle += edgeAngle;
					validEdgesCount++;
				}

				if (EdgeB != null)
				{
					double edgeAngle = EdgeB.Angle;
					while (edgeAngle < -Math.PI / 2)
						edgeAngle += Math.PI;
					while (edgeAngle > Math.PI / 2)
						edgeAngle -= Math.PI;
					angle += edgeAngle;
					validEdgesCount++;
				}

				angle = angle / validEdgesCount;
			}

			if (Math.Abs(angle) < minAngleToDeskew * Math.PI / 180)
			{
				Crop		crop = Crop.GetCrop(bmpData);
				Rectangle	rect = crop.TangentialRectangle;

				this.ulCorner = rect.Location;
				this.llCorner = new Point(rect.Left, rect.Bottom);

				this.width = rect.Width;
				this.angle = 0;
			}
			else
			{
				Crop crop = Crop.GetCrop(bmpData, this.angle);

				Line2D lineL = new Line2D(crop.Left, new PointF(crop.Left.X - (float)Math.Tan(angle) * 500, crop.Left.Y + 500));
				Line2D lineT = new Line2D(crop.Top, new PointF(crop.Top.X + 500, crop.Top.Y + (float)Math.Tan(angle) * 500));
				Line2D lineR = new Line2D(crop.Right, new PointF(crop.Right.X - (float)Math.Tan(angle) * 500, crop.Right.Y + 500));
				Line2D lineB = new Line2D(crop.Bottom, new PointF(crop.Bottom.X + 500, crop.Bottom.Y + (float)Math.Tan(angle) * 500));

				double interceptX = 0, interceptY = 0;
				lineL.InterceptPoint(lineT, ref interceptX, ref interceptY);
				this.ulCorner = new Point(Convert.ToInt32(interceptX), Convert.ToInt32(interceptY));

				lineL.InterceptPoint(lineB, ref interceptX, ref interceptY);
				this.llCorner = new Point(Convert.ToInt32(interceptX), Convert.ToInt32(interceptY));

				lineT.InterceptPoint(lineR, ref interceptX, ref interceptY);

				this.width = Convert.ToInt32(interceptX - this.ulCorner.X);
			}
		}
		#endregion

		#region constructor
		public ObjectByCorners(int[,] array, Point ulCorner, Point urCorner, Point llCorner, Point lrCorner, Crop clip, float minAngleToDeskew)
		{
			this.ulCorner = ulCorner;
			this.llCorner = llCorner;
			this.minAngleToDeskew = minAngleToDeskew;

			double ulAngle = GetAngle(urCorner, ulCorner, llCorner);
			double urAngle = GetAngle(lrCorner, urCorner, ulCorner);
			double lrAngle = GetAngle(llCorner, lrCorner, urCorner);
			double llAngle = GetAngle(ulCorner, llCorner, lrCorner);

			Edge lEdge = new Edge(this.ulCorner, this.llCorner);
			Edge tEdge = new Edge(this.ulCorner, urCorner);
			Edge rEdge = new Edge(urCorner, lrCorner);
			Edge bEdge = new Edge(lrCorner, llCorner);

			if (ulAngle != 0)
			{
				EdgeL = new Edge(this.ulCorner, this.llCorner);
				EdgeT = new Edge(this.ulCorner, urCorner);
				validAngles.Add(ulAngle);
				validCorners++;
			}
			if (urAngle != 0)
			{
				EdgeT = new Edge(this.ulCorner, urCorner);
				EdgeR = new Edge(urCorner, lrCorner);
				validAngles.Add(urAngle);
				validCorners++;
			}
			if (lrAngle != 0)
			{
				EdgeR = new Edge(urCorner, lrCorner);
				EdgeB = new Edge(lrCorner, this.llCorner);
				validAngles.Add(lrAngle);
				validCorners++;
			}
			if (llAngle != 0)
			{
				EdgeL = new Edge(this.ulCorner, this.llCorner);
				EdgeB = new Edge(lrCorner, this.llCorner);
				validAngles.Add(llAngle);
				validCorners++;
			}

			if (EdgeL != null || EdgeT != null || EdgeR != null || EdgeB != null)
			{
				int validEdgesCount = 0;

				if (EdgeL != null)
				{
					double edgeAngle = EdgeL.Angle - Math.PI / 2;
					while (edgeAngle < -Math.PI / 2)
						edgeAngle += Math.PI;
					while (edgeAngle > Math.PI / 2)
						edgeAngle -= Math.PI;
					angle += edgeAngle;
					validEdgesCount++;
				}

				if (EdgeT != null)
				{
					double edgeAngle = EdgeT.Angle;
					while (edgeAngle < -Math.PI / 2)
						edgeAngle += Math.PI;
					while (edgeAngle > Math.PI / 2)
						edgeAngle -= Math.PI;
					angle += edgeAngle;
					validEdgesCount++;
				}

				if (EdgeR != null)
				{
					double edgeAngle = EdgeR.Angle - Math.PI / 2;
					while (edgeAngle < -Math.PI / 2)
						edgeAngle += Math.PI;
					while (edgeAngle > Math.PI / 2)
						edgeAngle -= Math.PI;
					angle += edgeAngle;
					validEdgesCount++;
				}

				if (EdgeB != null)
				{
					double edgeAngle = EdgeB.Angle;
					while (edgeAngle < -Math.PI / 2)
						edgeAngle += Math.PI;
					while (edgeAngle > Math.PI / 2)
						edgeAngle -= Math.PI;
					angle += edgeAngle;
					validEdgesCount++;
				}

				angle = angle / validEdgesCount;
			}

			if (Math.Abs(angle) < minAngleToDeskew * Math.PI / 180)
			{
				Rectangle rect = clip.TangentialRectangle;

				this.ulCorner = rect.Location;
				this.llCorner = new Point(rect.Left, rect.Bottom);

				this.width = rect.Width;
				this.angle = 0;
			}
			else
			{
				Crop crop = Crop.GetCrop(array, this.angle);

				Line2D lineL = new Line2D(crop.Left, new PointF(crop.Left.X - (float)Math.Tan(angle) * 500, crop.Left.Y + 500));
				Line2D lineT = new Line2D(crop.Top, new PointF(crop.Top.X + 500, crop.Top.Y + (float)Math.Tan(angle) * 500));
				Line2D lineR = new Line2D(crop.Right, new PointF(crop.Right.X - (float)Math.Tan(angle) * 500, crop.Right.Y + 500));
				Line2D lineB = new Line2D(crop.Bottom, new PointF(crop.Bottom.X + 500, crop.Bottom.Y + (float)Math.Tan(angle) * 500));

				double interceptX = 0, interceptY = 0;
				lineL.InterceptPoint(lineT, ref interceptX, ref interceptY);
				this.ulCorner = new Point(Convert.ToInt32(interceptX), Convert.ToInt32(interceptY));

				lineL.InterceptPoint(lineB, ref interceptX, ref interceptY);
				this.llCorner = new Point(Convert.ToInt32(interceptX), Convert.ToInt32(interceptY));

				lineT.InterceptPoint(lineR, ref interceptX, ref interceptY);

				this.width = Convert.ToInt32(interceptX - this.ulCorner.X);
			}
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties
		public Point UlCorner { get { return this.ulCorner; } }
		public Point LlCorner { get { return this.llCorner; } }
		public Point LrCorner { get { return new Point(LlCorner.X + width, Convert.ToInt32(LlCorner.Y + Math.Tan(Skew) * Width)); } }
		public Point Centroid { get { return new Point(UlCorner.X + (LrCorner.X - UlCorner.X) / 2, UlCorner.Y + (LrCorner.Y - UlCorner.Y) / 2); } }
		public bool Inclined { get { return this.angle != 0; } }
		
		public int Width { get { return this.width; } }
		public int Height { get { return this.llCorner.Y - this.ulCorner.Y; } }
		public int ValidCorners { get { return this.validCorners; } }
		//public ArrayList	ValidAngles		{ get{return this.validAngles;} }

		/// <summary>
		/// clip skew in radians. Angle bigger than 0 - clockwise, Angle smaller than 0 - counter clockwise
		/// </summary>
		public double Skew { get { return this.angle; } }
		
		#region ValidEdges
		public List<Edge> ValidEdges
		{
			get
			{
				List<Edge> validEdges = new List<Edge>();

				if (EdgeL != null)
					validEdges.Add(EdgeL);
				if (EdgeT != null)
					validEdges.Add(EdgeT);
				if (EdgeR != null)
					validEdges.Add(EdgeR);
				if (EdgeB != null)
					validEdges.Add(EdgeB);

				return validEdges;
			}
		}
		#endregion

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region GetAngle()
		private double GetAngle(Point side1, Point middle, Point side2)
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

		#region Offset()
		public void Offset(int dx, int dy)
		{
			ulCorner.Offset(dx, dy);
			llCorner.Offset(dx, dy);

			if (EdgeL != null)
				EdgeL.Offset(dx, dy);
			if (EdgeT != null)
				EdgeT.Offset(dx, dy);
			if (EdgeR != null)
				EdgeR.Offset(dx, dy);
			if (EdgeB != null)
				EdgeB.Offset(dx, dy);
		}
		#endregion

		#region Inflate()
		public void Inflate(int dx, int dy)
		{
			ulCorner.Offset(-dx, -dy);
			llCorner.Offset(-dx, +dy);

			this.width += 2 * dx;
		}
		#endregion

		#endregion

	}

}
