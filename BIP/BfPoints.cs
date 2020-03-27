using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ImageProcessing
{
	public class BfPoints : List<BfPoint>
	{
		internal event BfPoint.PointHnd PointAdding;
		internal event BfPoint.PointHnd PointAdded;
		internal event BfPoint.PointHnd PointRemoving;
		internal event BfPoint.PointHnd PointRemoved;
		internal event BfPoint.VoidHnd Clearing;
		internal event BfPoint.VoidHnd Cleared;

		public event ItImage.VoidHnd Changed;

		#region constructor
		public BfPoints()
		{
		}
		#endregion

		#region Add()
		new public void Add(BfPoint bfPoint)
		{
			if (PointAdding != null)
				PointAdding(bfPoint);

			base.Add(bfPoint);
			Sort();

			bfPoint.RemoveRequest += new BfPoint.ChangedHnd(Remove);
			bfPoint.Changed += new BfPoint.ChangedHnd(BfPoint_Changed);

			if (PointAdded != null)
				PointAdded(bfPoint);

			Points_Changed();
		}
		#endregion

		#region AddRange()
		new public void AddRange(IEnumerable<BfPoint> bfPoints)
		{
			foreach (BfPoint bfPoint in bfPoints)
				Add(bfPoint);
		}
		#endregion

		#region Remove()
		new public void Remove(BfPoint bfPoint)
		{
			if (PointRemoving != null)
				PointRemoving(bfPoint);

			bfPoint.RemoveRequest -= new BfPoint.ChangedHnd(Remove);
			bfPoint.Changed -= new BfPoint.ChangedHnd(BfPoint_Changed);
			base.Remove(bfPoint);

			if (PointRemoved != null)
				PointRemoved(bfPoint);

			Points_Changed();
		}
		#endregion

		#region RemoveAt()
		new public void RemoveAt(int index)
		{
			Remove(this[index]);
		}
		#endregion

		#region Clear()
		new public void Clear()
		{
			if (Clearing != null)
				Clearing();

			foreach (BfPoint bfPoint in this)
			{
				bfPoint.RemoveRequest -= new BfPoint.ChangedHnd(Remove);
				bfPoint.Changed -= new BfPoint.ChangedHnd(BfPoint_Changed);
			}

			base.Clear();

			if (Cleared != null)
				Cleared();

			Points_Changed();
		}
		#endregion

		#region GetPoints()
		public Point[] GetPoints()
		{
			Point[] points = new Point[this.Count];

			for (int i = 0; i < this.Count; i++)
				points[i] = this[i].ToPoint();

			return points;
		}
		#endregion

		#region GetPointsF()
		public PointF[] GetPointsF()
		{
			PointF[] points = new PointF[this.Count];

			for (int i = 0; i < this.Count; i++)
				points[i] = this[i].ToPoint();

			return points;
		}
		#endregion

		#region Clone()
		public BfPoints Clone()
		{
			BfPoints bfPoints = new BfPoints();

			foreach (BfPoint bfPoint in this)
				bfPoints.Add(bfPoint.Clone());

			return bfPoints;
		}
		#endregion

		#region MarkEdgePoints()
		public void MarkEdgePoints()
		{
			for (int i = 0; i < this.Count; i++)
				this[i].IsEdgePoint = (i == 0 || i == this.Count - 1);
		}
		#endregion

		#region BfPoint_Changed()
		void BfPoint_Changed(BfPoint bfPoint)
		{
			Points_Changed();
		}
		#endregion

		#region Points_Changed()
		void Points_Changed()
		{
			if (this.Changed != null)
				this.Changed();
		}
		#endregion
	
	
	}
}
