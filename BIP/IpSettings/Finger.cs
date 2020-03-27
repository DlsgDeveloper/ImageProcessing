using System;
using System.Drawing;

using BIP.Geometry;


namespace ImageProcessing.IpSettings
{
	/// <summary>
	/// ImageProcessing.IpSettings.Finger is linked with ImageProcessing.IpSettings.ItPage, in contrast with
	/// ImageProcessing.Finger linked with ImageProcessing.IpSettings.ItImage, but coordinates are global - from image
	/// </summary>
	public class Finger
	{
		ImageProcessing.IpSettings.ItPage		page;
		RatioRect	rectNotSkewed;
		float		confidence = 1.0F;
		bool		locked = false;
		RatioSize	minSize = new RatioSize(0.01, 0.01);
		Anchors		anchors = Anchors.None;

		public delegate void ChangedHnd(ImageProcessing.IpSettings.Finger finger, ChangeType type);
		public delegate void FingerHnd(ImageProcessing.IpSettings.Finger finger);
		public delegate void VoidHnd();
		
		public		event ChangedHnd	Changed;
		internal	event FingerHnd		RemoveRequest;

		#region constructor
		/// <summary>
		/// 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="pageRectNotSkewed">Image ratio rect, not page rect</param>
		private Finger(ImageProcessing.IpSettings.ItPage page, RatioRect imageRectNotSkewed)
		{
			this.page = page;
			this.rectNotSkewed = imageRectNotSkewed;
			this.rectNotSkewed.Intersect(new RatioRect(0, 0, 1, 1));
			
			//this.rectNotSkewed.Intersect(page.ClipRect);
			if (rectNotSkewed.X < page.Clip.RectangleNotSkewed.X)
				rectNotSkewed.X = page.Clip.RectangleNotSkewed.X;
			else if (rectNotSkewed.X > page.Clip.RectangleNotSkewed.Right)
				rectNotSkewed.X = page.Clip.RectangleNotSkewed.Right;
			if (rectNotSkewed.Y < page.Clip.RectangleNotSkewed.Y)
				rectNotSkewed.Y = page.Clip.RectangleNotSkewed.Y;
			else if (rectNotSkewed.Y > page.Clip.RectangleNotSkewed.Bottom)
				rectNotSkewed.Y = page.Clip.RectangleNotSkewed.Bottom;
			if (rectNotSkewed.Width < 0)
				rectNotSkewed.Width = 0;
			else if (rectNotSkewed.Right > page.Clip.RectangleNotSkewed.Right)
				rectNotSkewed.Right = page.Clip.RectangleNotSkewed.Right;
			if (rectNotSkewed.Height < 0)
				rectNotSkewed.Height = 0;
			else if (rectNotSkewed.Bottom > page.Clip.RectangleNotSkewed.Bottom)
				rectNotSkewed.Bottom = page.Clip.RectangleNotSkewed.Bottom;

			// min size is 1/8 of an inch
			minSize = new RatioSize(1.0 / (8 * page.ItImage.InchSize.Width), 1.0 / (8 * page.ItImage.InchSize.Height));

			SetAnchors();
		}
		#endregion

		#region enum ChangeType
		public enum ChangeType
		{
			Confidence,
			Move,
			Resize
		}
		#endregion

		#region enum Anchors
		[Flags]
		public enum Anchors : byte
		{
			None = 0,
			Left = 1,
			Top = 2,
			Right = 4,
			Bottom = 8
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		public ImageProcessing.IpSettings.ItPage	Page		{ get { return this.page; } }

		public RatioPoint PointUL { get { return page.Clip.TransferUnskewedToSkewedPoint(new RatioPoint(RectangleNotSkewed.Left, RectangleNotSkewed.Top)); } }
		public RatioPoint PointUR { get { return page.Clip.TransferUnskewedToSkewedPoint(new RatioPoint(RectangleNotSkewed.Right, RectangleNotSkewed.Top)); } }
		public RatioPoint PointLL { get { return page.Clip.TransferUnskewedToSkewedPoint(new RatioPoint(RectangleNotSkewed.Left, RectangleNotSkewed.Bottom)); } }
		public RatioPoint PointLR { get { return page.Clip.TransferUnskewedToSkewedPoint(new RatioPoint(RectangleNotSkewed.Right, RectangleNotSkewed.Bottom)); } }

		public RatioPoint[] Points { get { return new RatioPoint[] { PointUL, PointUR, PointLR, PointLL }; } }

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
		public RatioRect RectangleNotSkewed	
		{
			get{return this.rectNotSkewed;} 
		}
		#endregion

		#region PageRect
		public RatioRect PageRect
		{
			get 
			{ 
				RatioRect pageRect = this.page.ClipRect;

				if (pageRect.Width > 0 && pageRect.Height > 0)
					return new RatioRect((this.rectNotSkewed.X - pageRect.X) / pageRect.Width,
						(this.rectNotSkewed.Y - pageRect.Y) / pageRect.Height,
						this.rectNotSkewed.Width / pageRect.Width,
						this.rectNotSkewed.Height / pageRect.Height);
				else
					return new RatioRect(0, 0, 0, 0);
			}
		}
		#endregion

		#region ConvexHull
		/*public RatioRect ConvexHull
		{
			get
			{
				RatioPoint ul = PointUL;
				RatioPoint ur = PointUR;
				RatioPoint ll = PointLL;
				RatioPoint lr = PointLR;

				double l = Math.Min((ul.X < ur.X ? ul.X : ur.X), (ll.X < lr.X ? ll.X : lr.X));
				double t = Math.Min((ul.Y < ur.Y ? ul.Y : ur.Y), (ll.Y < lr.Y ? ll.Y : lr.Y));
				double r = Math.Max((ul.X > ur.X ? ul.X : ur.X), (ll.X > lr.X ? ll.X : lr.X));
				double b = Math.Max((ul.Y > ur.Y ? ul.Y : ur.Y), (ll.Y > lr.Y ? ll.Y : lr.Y));
			
				return RatioRect.FromLTRB(l, t, r, b);
			}
		}*/
		#endregion

		#region EdgeAnchors
		/*public Anchors EdgeAnchors
		{
			get{return this.anchors;}
			set
			{
				if (this.anchors != value)
				{
					this.anchors = value;

					AdjustFingerByAnchors();

					if (Changed != null)
						Changed(this, ChangeType.Resize);
				}
			}
		}*/
		#endregion

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region constructor
		/// <summary>
		/// 
		/// </summary>
		/// <param name="page"></param>
		/// <param name="pageRectNotSkewed">Image ratio rect, not page rect</param>
		public static Finger GetFinger(ImageProcessing.IpSettings.ItPage page, RatioRect imageRectNotSkewed, bool locked)
		{
			Finger finger = new Finger(page, imageRectNotSkewed);
			
			if(locked == false && (finger.RectangleNotSkewed.Width < finger.minSize.Width || finger.RectangleNotSkewed.Height < finger.minSize.Height))
				return null;

			if (locked)
				finger.Lock();

			return finger;
		}
		#endregion

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
			else
			{
				RatioRect clip = this.rectNotSkewed;
				
				SetAnchors();

				if (clip != this.rectNotSkewed && Changed != null)
					Changed(this, ChangeType.Resize);
			}
		}
		#endregion

		#region Clone()
		public ImageProcessing.IpSettings.Finger Clone(ImageProcessing.IpSettings.ItPage page)
		{
			return new ImageProcessing.IpSettings.Finger(page, this.RectangleNotSkewed);
		}
		#endregion

		#region Contains()
		/*public bool Contains(RatioPoint p)
		{
			if(this.page.Skew != 0)
				p = page.Clip.TransferSkewedToUnskewedPoint(p);
			
			return RectangleNotSkewed.Contains(p);
		}*/
		#endregion

		#region IsIdentical()
		public bool IsIdentical(RatioRect rect)
		{
			return (RatioRect.Intersect(rect, page.Clip.RectangleNotSkewed) == this.RectangleNotSkewed);
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
		public void Move(double dx, double dy)
		{
			RatioRect r = this.rectNotSkewed;

			r.X += dx;
			r.Y += dy;

			if (r.X < this.Page.ClipRect.X)
				r.X = this.Page.ClipRect.X;
			if (r.Y < this.Page.ClipRect.Y)
				r.Y = this.Page.ClipRect.Y;
			if (r.Right > this.Page.ClipRect.Right)
				r.X = this.Page.ClipRect.Right - r.Width;
			if (r.Bottom > this.Page.ClipRect.Bottom)
				r.Y = this.Page.ClipRect.Bottom - r.Height;

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
		public void Resize(double dl, double dt, double dr, double db)
		{
			double left = Math.Max(this.Page.ClipRect.X, Math.Min(this.Page.ClipRect.Right, this.rectNotSkewed.X + dl));
			double top = Math.Max(this.Page.ClipRect.Y, Math.Min(this.Page.ClipRect.Bottom, this.rectNotSkewed.Y + dt));
			double right = Math.Max(this.Page.ClipRect.X, Math.Min(this.Page.ClipRect.Right, this.rectNotSkewed.Right + dr));
			double bottom = Math.Max(this.Page.ClipRect.Y, Math.Min(this.Page.ClipRect.Bottom, this.rectNotSkewed.Bottom + db));

			if (left > right)
			{
				double tmp = left;
				left = right;
				right = tmp;
			}

			if (top > bottom)
			{
				double tmp = top;
				top = bottom;
				bottom = tmp;
			}

			RatioRect r = RatioRect.FromLTRB(left, top, right, bottom);
			
			if (this.rectNotSkewed != r)
			{
				this.rectNotSkewed = r;

				if (this.locked == false && (this.rectNotSkewed.Width < minSize.Width || this.rectNotSkewed.Height < minSize.Height))
				{
					if (this.RemoveRequest != null)
						this.RemoveRequest(this);
				}
				else if (Changed != null)
					Changed(this, ChangeType.Resize);
			}
		}
		#endregion

		#region SetClip()
		public void SetClip(double left, double top, double right, double bottom)
		{
			SetClip(RatioRect.FromLTRB(left, top, right, bottom));
		}

		public void SetClip(RatioRect r)
		{
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

			r.Intersect(this.Page.ClipRect);

			if (this.rectNotSkewed != r)
			{
				this.rectNotSkewed = r;
				SetAnchors();

				if (this.locked == false && (this.rectNotSkewed.Width < minSize.Width || this.rectNotSkewed.Height < minSize.Height))
				{
					if (this.RemoveRequest != null)
						this.RemoveRequest(this);
				}
				else if (Changed != null)
					Changed(this, ChangeType.Resize);
			}
		}
		#endregion

		#region ClipChanged()
		public void ClipChanged()
		{
			RatioRect rect = this.RectangleNotSkewed;

			this.rectNotSkewed.Intersect(this.page.ClipRect);
			AdjustFingerByAnchors();

			if (locked == false && (this.RectangleNotSkewed.Width < this.minSize.Width || this.RectangleNotSkewed.Height < this.minSize.Height))
			{
				if (this.RemoveRequest != null)
					RemoveRequest(this);
			}
			else
			{
				if (rect != this.RectangleNotSkewed && Changed != null)
					Changed(this, ChangeType.Resize);
			}
		}
		#endregion

		#region ToString()
		public override string ToString()
		{
			return string.Format("X={0:0.000} Y={1:0.000} Width={2:0.000} Height={3:0.000} Right={4:0.000} Bottom={5:0.000}", rectNotSkewed.X, rectNotSkewed.Y, rectNotSkewed.Width, rectNotSkewed.Height, rectNotSkewed.Right, rectNotSkewed.Bottom);
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region SetAnchors()
		/// <summary>
		/// Makes finger edges is close to the page clip anchored to that edge.
		/// </summary>
		private void SetAnchors()
		{
			if (this.locked == false)
			{
				Anchors a = Anchors.None;
				RatioRect pageRect = page.ClipRect;

				if (this.rectNotSkewed.X < pageRect.X + minSize.Width)
				{
					a |= Anchors.Left;
				}
				if (this.rectNotSkewed.Y < pageRect.Y + minSize.Height)
				{
					a |= Anchors.Top;
				}
				if (this.rectNotSkewed.Right > pageRect.Right - minSize.Width)
				{
					a |= Anchors.Right;
				}
				if (this.rectNotSkewed.Bottom > pageRect.Bottom - minSize.Height)
				{
					a |= Anchors.Bottom;
				}

				this.anchors = a;
				AdjustFingerByAnchors();
			}
		}
		#endregion

		#region AdjustFingerByAnchors()
		private void AdjustFingerByAnchors()
		{
			if (this.locked == false)
			{
				RatioRect pageRect = page.Clip.RectangleNotSkewed;

				if ((this.anchors & Anchors.Left) > 0)
				{
					rectNotSkewed.Width = rectNotSkewed.Width + (rectNotSkewed.X - pageRect.X);
					rectNotSkewed.X = pageRect.X;
				}
				if ((this.anchors & Anchors.Top) > 0)
				{
					rectNotSkewed.Height = rectNotSkewed.Height + (rectNotSkewed.Y - pageRect.Y);
					rectNotSkewed.Y = pageRect.Y;
				}
				if ((this.anchors & Anchors.Right) > 0)
				{
					rectNotSkewed.Right = pageRect.Right;
				}
				if ((this.anchors & Anchors.Bottom) > 0)
				{
					rectNotSkewed.Bottom = pageRect.Bottom;
				}
			}
		}
		#endregion

		#endregion

	}
}
