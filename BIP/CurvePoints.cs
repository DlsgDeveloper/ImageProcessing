using System;
using System.Collections;
using System.Drawing;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for CurvePoints.
	/// </summary>
	public class CurvePoints
	{
		Point[]		points = new Point[7];
		
		public CurvePoints(Point[] points)
		{			
			for(int i = 0; i < this.Count; i++)	
				this.points[i] = points[i];
		}

		/*public CurvePoints(PageParams pageParams, bool topCurve)
		{
			Rectangle	clip = pageParams.Clip.Rectangle;
			int			y = (topCurve) ? clip.Y : clip.Bottom;
			
			p0 = new Point(clip.X, y);
			p1 = new Point(clip.X + clip.Width / 6, y);
			p2 = new Point(clip.X + clip.Width / 3, y);
			p3 = new Point(clip.X + clip.Width / 2, y);
			p4 = new Point(clip.X + clip.Width * 2 / 3, y);
			p5 = new Point(clip.X + clip.Width * 5 / 6, y);
			p6 = new Point(clip.Right, y);
		}*/

		//PUBLIC PROPERTIES
		public int		Count			{get{return 7;}}
		public Point	this[int index]	{get{return points[index];} set{points[index] = value;}}
		public Point[]	Points			{get{return points;}}
	
		//PUBLIC METHODS
		#region Clear()
		public void Clear()
		{
			for(int i = 0; i < this.Count; i++)	
				points[i] = Point.Empty;
		}
		#endregion

	}
}
