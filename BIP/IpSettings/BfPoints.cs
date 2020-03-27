using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using BIP.Geometry;


namespace ImageProcessing.IpSettings
{
	public class BfPoints : List<ImageProcessing.IpSettings.BfPoint>
	{
		internal event ImageProcessing.IpSettings.BfPoint.BfPointHnd PointAdding;
		internal event ImageProcessing.IpSettings.BfPoint.BfPointHnd PointAdded;
		internal event ImageProcessing.IpSettings.BfPoint.BfPointHnd PointRemoving;
		internal event ImageProcessing.IpSettings.BfPoint.BfPointHnd PointRemoved;
		internal event ImageProcessing.IpSettings.BfPoint.VoidHnd Clearing;
		internal event ImageProcessing.IpSettings.BfPoint.VoidHnd Cleared;

		internal event ImageProcessing.IpSettings.ItImage.VoidHnd Changed;


		#region constructor
		public BfPoints()
		{
		}
		#endregion


		#region Add()
		new public void Add(ImageProcessing.IpSettings.BfPoint bfPoint)
		{
			if (PointAdding != null)
				PointAdding(bfPoint);

			base.Add(bfPoint);
			Sort();

			bfPoint.RemoveRequest += new ImageProcessing.IpSettings.BfPoint.BfPointHnd(Remove);
			bfPoint.Changed += new ImageProcessing.IpSettings.BfPoint.BfPointHnd(BfPoint_Changed);

			if (PointAdded != null)
				PointAdded(bfPoint);

			Points_Changed();
		}
		#endregion

		#region Insert()
		new public void Insert(int index, ImageProcessing.IpSettings.BfPoint bfPoint)
		{
			if (PointAdding != null)
				PointAdding(bfPoint);

			base.Insert(index, bfPoint);
			Sort();

			bfPoint.RemoveRequest += new ImageProcessing.IpSettings.BfPoint.BfPointHnd(Remove);
			bfPoint.Changed += new ImageProcessing.IpSettings.BfPoint.BfPointHnd(BfPoint_Changed);

			if (PointAdded != null)
				PointAdded(bfPoint);

			Points_Changed();
		}
		#endregion

		#region AddRange()
		new public void AddRange(IEnumerable<ImageProcessing.IpSettings.BfPoint> bfPoints)
		{
			foreach (ImageProcessing.IpSettings.BfPoint bfPoint in bfPoints)
				Add(bfPoint);
		}
		#endregion

		#region Remove()
		new public void Remove(ImageProcessing.IpSettings.BfPoint bfPoint)
		{
			if (PointRemoving != null)
				PointRemoving(bfPoint);

			bfPoint.RemoveRequest -= new ImageProcessing.IpSettings.BfPoint.BfPointHnd(Remove);
			bfPoint.Changed -= new ImageProcessing.IpSettings.BfPoint.BfPointHnd(BfPoint_Changed);
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

			foreach (ImageProcessing.IpSettings.BfPoint bfPoint in this)
			{
				bfPoint.RemoveRequest -= new ImageProcessing.IpSettings.BfPoint.BfPointHnd(Remove);
				bfPoint.Changed -= new ImageProcessing.IpSettings.BfPoint.BfPointHnd(BfPoint_Changed);
			}

			base.Clear();

			if (Cleared != null)
				Cleared();

			Points_Changed();
		}
		#endregion

		#region GetPoints()
		public RatioPoint[] GetPoints()
		{
			RatioPoint[] points = new RatioPoint[this.Count];

			for (int i = 0; i < this.Count; i++)
				points[i] = this[i].RatioPoint;

			return points;
		}
		#endregion

		#region Clone()
		public BfPoints Clone()
		{
			BfPoints bfPoints = new BfPoints();

			foreach (ImageProcessing.IpSettings.BfPoint bfPoint in this)
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
		void BfPoint_Changed(ImageProcessing.IpSettings.BfPoint bfPoint)
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
