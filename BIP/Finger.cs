using System;
using System.Drawing;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Finger.
	/// </summary>
	public class Finger
	{
		ItPage		page;
		Rectangle	rectNotSkewed;
		float		confidence = 1.0F;
		bool		locked = false;
		Size		minSize = new Size(50, 50);

		public delegate void ChangedHnd(Finger finger, ChangeType type);
		public delegate void FingerHnd(Finger finger);
		public delegate void VoidHnd();
		
		public		event ChangedHnd	Changed;
		internal	event FingerHnd		RemoveRequest;
		

		public Finger(ItPage page, Rectangle rectNotSkewed)
		{
			this.page = page;
			this.rectNotSkewed = Rectangle.Intersect(rectNotSkewed, page.Clip.RectangleNotSkewed);
		}

		public enum ChangeType
		{
			Confidence,
			Move,
			Resize,
			Remove
		}

		//PUBLIC PROPERTIES
		#region public properties

		public ItPage	Page		{ get { return this.page; } }

		public Point PointUL { get { return page.Clip.TransferUnskewedToSkewedPoint(RectangleNotSkewed.Location); } }
		public Point PointUR { get { return page.Clip.TransferUnskewedToSkewedPoint(new Point(RectangleNotSkewed.Right, RectangleNotSkewed.Y)); } }
		public Point PointLL { get { return page.Clip.TransferUnskewedToSkewedPoint(new Point(RectangleNotSkewed.X, RectangleNotSkewed.Bottom)); } }
		public Point PointLR { get { return page.Clip.TransferUnskewedToSkewedPoint(new Point(RectangleNotSkewed.Right, RectangleNotSkewed.Bottom)); } }

		public Point[] Points { get { return new Point[] { PointUL, PointUR, PointLR, PointLL }; } }

		#region Confidence
		public float Confidence	
		{ 
			get { return this.confidence; } 
			set 
			{ 
				this.confidence = value;

				if (Changed != null)
					Changed(this, ChangeType.Confidence);
			}
		}
		#endregion

		#region RectangleNotSkewed
		public Rectangle RectangleNotSkewed	
		{
			get{return Rectangle.Intersect(this.rectNotSkewed, page.Clip.RectangleNotSkewed);} 
			/*set 
			{
				if(this.rectNotSkewed != value)
				{
					this.rectNotSkewed = value;

					if(rectNotSkewed.X < page.Clip.RectangleNotSkewed.X)
						rectNotSkewed.X = page.Clip.RectangleNotSkewed.X;
					if(rectNotSkewed.Y < page.Clip.RectangleNotSkewed.Y)
						rectNotSkewed.Y = page.Clip.RectangleNotSkewed.Y;
					if(rectNotSkewed.Right > page.Clip.RectangleNotSkewed.Right)
						rectNotSkewed.X = page.Clip.RectangleNotSkewed.Right - rectNotSkewed.Width;
					if(rectNotSkewed.Bottom > page.Clip.RectangleNotSkewed.Bottom)
						rectNotSkewed.Y = page.Clip.RectangleNotSkewed.Bottom - rectNotSkewed.Height;

					if (this.locked == false && (this.rectNotSkewed.Width < minSize.Width || this.rectNotSkewed.Height < minSize.Height))
					{
						if (this.RemoveRequest != null)
							this.RemoveRequest(this);
					}
					else if (Changed != null)
						Changed(this, ChangeType.Resize);
				}
			}*/
		}
		#endregion

		#region ConvexHull
		public Rectangle ConvexHull
		{
			get
			{
				int x = PointUL.X;
				int y = PointUL.Y;
				int r = PointUL.Y;
				int b = PointUL.Y;

				if (x > PointUR.X)
					x = PointUR.X;
				if (y > PointUR.Y)
					y = PointUR.Y;
				if (r < PointUR.X)
					r = PointUR.X;
				if (b < PointUR.Y)
					b = PointUR.Y;

				if (x > PointLL.X)
					x = PointLL.X;
				if (y > PointLL.Y)
					y = PointLL.Y;
				if (r < PointLL.X)
					r = PointLL.X;
				if (b < PointLL.Y)
					b = PointLL.Y;

				if (x > PointLR.X)
					x = PointLR.X;
				if (y > PointLR.Y)
					y = PointLR.Y;
				if (r < PointLR.X)
					r = PointLR.X;
				if (b < PointLR.Y)
					b = PointLR.Y;
				
				return Rectangle.FromLTRB(x, y, r, b);
			}
		}
		#endregion

		#region ResizeSettings()
		public void ResizeSettings(double zoom)
		{
			rectNotSkewed.X = Convert.ToInt32(rectNotSkewed.X * zoom);
			rectNotSkewed.Y = Convert.ToInt32(rectNotSkewed.Y * zoom);
			rectNotSkewed.Width = Convert.ToInt32(rectNotSkewed.Width * zoom);
			rectNotSkewed.Height = Convert.ToInt32(rectNotSkewed.Height * zoom);
		}
		#endregion
	
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Lock()
		public void Lock()
		{
			this.locked = true;
		}
		#endregion

		#region Unlock()
		public void Unlock()
		{
			this.locked = false;

			if (this.rectNotSkewed.Width < minSize.Width || this.rectNotSkewed.Height < minSize.Height)
			{
				if (this.RemoveRequest != null)
					this.RemoveRequest(this);
			}
		}
		#endregion

		#region GetFinger()
		public static Finger GetFinger(ItPage page, Rectangle rectNotSkewed, float confidence)
		{
			if (Rectangle.Intersect(rectNotSkewed, page.Clip.RectangleNotSkewed) != Rectangle.Empty)
			{
				Finger finger = new Finger(page, rectNotSkewed);
				finger.Confidence = confidence;
				return finger;
			}

			return null;
		}	
		#endregion

		#region Clone()
		public Finger Clone(ItPage page)
		{
			return new Finger(page, this.RectangleNotSkewed);
		}
		#endregion

		#region Contains()
		public bool Contains(Point p)
		{
			if(this.page.Skew != 0)
				p = page.Clip.TransferSkewedToUnskewedPoint(p);
			
			return RectangleNotSkewed.Contains(p);
		}
		#endregion

		#region IsIdentical()
		public bool IsIdentical(Rectangle rect)
		{
			return (Rectangle.Intersect(rect, page.Clip.RectangleNotSkewed) == this.RectangleNotSkewed);
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

		#region Move()
		public void Move(int dx, int dy)
		{
			Rectangle r = this.rectNotSkewed;

			r.Offset(dx, dy);

			if (r.X < page.Clip.RectangleNotSkewed.X)
				r.X = page.Clip.RectangleNotSkewed.X;
			if (r.Y < page.Clip.RectangleNotSkewed.Y)
				r.Y = page.Clip.RectangleNotSkewed.Y;
			if (r.Right > page.Clip.RectangleNotSkewed.Right)
				r.X = page.Clip.RectangleNotSkewed.Right - r.Width;
			if (r.Bottom > page.Clip.RectangleNotSkewed.Bottom)
				r.Y = page.Clip.RectangleNotSkewed.Bottom - r.Height;

			if (r != this.rectNotSkewed)
			{
				this.rectNotSkewed = r;

				if (Changed != null)
					Changed(this, ChangeType.Move);
			}
		}
		#endregion

		#region Resize()
		/// <summary>
		/// If clip is smaller than selected size, remove request is fired.
		/// </summary>
		/// <param name="dl">change from left</param>
		/// <param name="dt">change from top</param>
		/// <param name="dr">change from right</param>
		/// <param name="db">change from bottom</param>
		public void Resize(int dl, int dt, int dr, int db)
		{
			Rectangle r = this.rectNotSkewed;

			r.X += dl;
			r.Y += dt;
			r.Width = r.Width - dl + dr;
			r.Height = r.Height - dt + db;

			r.Intersect(page.Clip.RectangleNotSkewed);

			if (r.Width < 0)
			{
				r.X = r.X + r.Width;
				r.Width = -r.Width;
			}

			if (r.Height < 0)
			{
				r.Y = r.Y + r.Height;
				r.Height = -r.Height;
			}

			this.rectNotSkewed = r;

			if (this.locked == false && (this.rectNotSkewed.Width < minSize.Width || this.rectNotSkewed.Height < minSize.Height))
			{
				if (this.RemoveRequest != null)
					this.RemoveRequest(this);
			}
			else if (Changed != null)
				Changed(this, ChangeType.Resize);
		}
		#endregion

		#region SetClip()
		public void SetClip(int left, int top, int right, int bottom)
		{
			SetClip(Rectangle.FromLTRB(left, top, right, bottom));
		}

		public void SetClip(Rectangle r)
		{
			r.Intersect(page.Clip.RectangleNotSkewed);

			if (r.Width < 0)
			{
				r.X = r.X + r.Width;
				r.Width = -r.Width;
			}

			if (r.Height < 0)
			{
				r.Y = r.Y + r.Height;
				r.Height = -r.Height;
			}

			this.rectNotSkewed = r;

			if (this.locked == false && (this.rectNotSkewed.Width < minSize.Width || this.rectNotSkewed.Height < minSize.Height))
			{
				if (this.RemoveRequest != null)
					this.RemoveRequest(this);
			}
			else if (Changed != null)
				Changed(this, ChangeType.Resize);
		}
		#endregion

		#region ClipChanged()
		public void ClipChanged()
		{
			Rectangle newRect = Rectangle.Intersect(this.RectangleNotSkewed, page.ClipRect);

			if (newRect != Rectangle.Empty && newRect.Width > this.minSize.Width && newRect.Height > this.minSize.Height)
			{
				//anchor it to the edge
				if (newRect.X < page.ClipRect.X + 20)
					newRect = Rectangle.FromLTRB(page.ClipRect.X, newRect.Y, newRect.Right, newRect.Bottom);
				if (newRect.Y < page.ClipRect.Y + 20)
					newRect = Rectangle.FromLTRB(newRect.X, page.ClipRect.Y, newRect.Right, newRect.Bottom);
				if (newRect.Right > page.ClipRect.Right - 20)
					newRect = Rectangle.FromLTRB(newRect.X, newRect.Y, page.ClipRect.Right, newRect.Bottom);
				if (newRect.Bottom > page.ClipRect.Bottom - 20)
					newRect = Rectangle.FromLTRB(newRect.X, newRect.Y, newRect.Right, page.ClipRect.Bottom);

				this.SetClip(newRect);
			}
			else
				RemoveRequest(this);
		}
		#endregion


		#endregion
	}
}
