using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ImageProcessing
{
	public class BfPoint : IComparable<BfPoint>
	{
		Point	point;
		bool	isEdgePoint = false;

		public delegate void PointHnd(BfPoint bfPoint);
		public delegate void ChangedHnd(BfPoint bfPoint);
		public delegate void VoidHnd();
		
		public event ChangedHnd Changed;
		internal event ChangedHnd RemoveRequest;


		#region constructor
		public BfPoint(Point point)
		{
			this.point = point;
		}

		public BfPoint(int x, int y)
		{
			this.point = new Point(x,y);
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		public int X
		{
			get { return this.point.X; }
			set { Set(value, this.point.Y); }
		}
		
		public int Y 
		{ 
			get { return this.point.Y; }
			set { Set(this.point.X, value); }
		}
		
		public Point	Point 
		{ 
			get { return this.point; }
			set { Set(this.point.X, this.point.Y); }
		}
		
		public bool		IsEdgePoint 
		{ 
			get { return this.isEdgePoint; } 
			set 
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

		#region ToPoint()
		public Point ToPoint()
		{
			return point;
		}
		#endregion

		#region ToPointF()
		public PointF ToPointF()
		{
			return point;
		}
		#endregion

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
		public int CompareTo(BfPoint bfPoint)
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
		internal void Offset(int dx, int dy)
		{
			point.Offset(dx, dy);

			if (Changed != null)
				Changed(this);
		}
		#endregion

		#region Set()
		internal void Set(int x, int y)
		{
			if (this.point.X != x || this.point.Y != y)
			{
				point.X = x;
				point.Y = y;

				if (Changed != null)
					Changed(this);
			}
		}
		#endregion

		#region Set()
		internal void Set(Point p)
		{
			Set(p.X, p.Y);
		}
		#endregion

		#region Set()
		internal void Set(BfPoint p)
		{
			Set(p.X, p.Y);
		}
		#endregion

		#region Clone()
		internal BfPoint Clone()
		{
			BfPoint bfPoint = new BfPoint(point);

			bfPoint.IsEdgePoint = this.IsEdgePoint;
			return bfPoint;
		}
		#endregion

		#endregion

	}
}
