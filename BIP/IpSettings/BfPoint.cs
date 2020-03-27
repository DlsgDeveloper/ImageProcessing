using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using BIP.Geometry;


namespace ImageProcessing.IpSettings
{
	public class BfPoint : IComparable<ImageProcessing.IpSettings.BfPoint>
	{
		RatioPoint	point;
		bool	isEdgePoint = false;

		public delegate void BfPointHnd(ImageProcessing.IpSettings.BfPoint bfPoint);
		public delegate void VoidHnd();
	
		internal event BfPointHnd Changed;
		internal event BfPointHnd RemoveRequest;


		#region constructor
		public BfPoint(RatioPoint point)
		{
			this.point = point;
		}

		public BfPoint(double x, double y)
		{
			this.point = new RatioPoint(x, y);
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties

		public RatioPoint RatioPoint { get { return point; } }

		public double X
		{
			get { return this.point.X; }
			internal set { Set(value, this.point.Y); }
		}

		public double Y 
		{ 
			get { return this.point.Y; }
			internal set { Set(this.point.X, value); }
		}
			
		public bool		IsEdgePoint 
		{ 
			get { return this.isEdgePoint; } 
			internal set 
			{
				if (this.isEdgePoint != value)
				{
					this.isEdgePoint = value;

					if (Changed != null)
						Changed(this);
				}
			} 
		}

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region RaiseRemoveRequest()
		public bool RaiseRemoveRequest()
		{
			if (RemoveRequest != null)
			{
				RemoveRequest(this);
				return true;
			}
			else
				return false;
		}
		#endregion

		#region CompareTo()
		public int CompareTo(ImageProcessing.IpSettings.BfPoint bfPoint)
		{
			if (this.X > bfPoint.X)
				return 1;
			else if (this.X < bfPoint.X)
				return -1;
			else
				return 0;
		}
		#endregion

		#endregion


		//INTERNAL PROPERTIES
		#region internal properties

		#region Offset()
		internal void Offset(double dx, double dy)
		{
			point.X += dx;
			point.Y += dy;

			if (Changed != null)
				Changed(this);
		}
		#endregion

		#region Set()
		internal void Set(double x, double y)
		{
			if (this.point.X != x || this.point.Y != y)
			{
				point.X = x;
				point.Y = y;

				if (Changed != null)
					Changed(this);
			}
		}

		internal void Set(RatioPoint p)
		{
			Set(p.X, p.Y);
		}

		internal void Set(ImageProcessing.IpSettings.BfPoint p)
		{
			Set(p.X, p.Y);
		}
		#endregion

		#region Clone()
		internal ImageProcessing.IpSettings.BfPoint Clone()
		{
			BfPoint bfPoint = new BfPoint(point);

			bfPoint.IsEdgePoint = this.IsEdgePoint;
			return bfPoint;
		}
		#endregion

		#endregion

	}
}
