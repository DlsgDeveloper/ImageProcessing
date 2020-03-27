using System;
using System.Drawing;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Clip.
	/// </summary>
	public class Clip
	{
		Rectangle	clip;
		Rectangle?	content = null;
		double		skew;
		float		clipConfidence = 1.0F;
		float		skewConfidence = 1.0F;

		public delegate void ClipChangedHnd(Clip clip);
		public event ClipChangedHnd ClipChanged;
		
		
		#region constructor
		public Clip(Rectangle clip)
			: this(clip.X, clip.Y, clip.Width, clip.Height)
		{
		}

		public Clip(int x, int y, int width, int height)
		{
			this.clip = new Rectangle(x, y, width, height);
			this.skew = 0.0;
			this.skewConfidence = 1.0F;
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties
		
		public Rectangle	RectangleNotSkewed	{ get { return this.clip;} }
		public bool			ContentDefined		{ get { return this.content.HasValue; } }
		public Rectangle	Content				{ get { return (ContentDefined) ? this.content.Value : RectangleNotSkewed; } }
		public bool			IsSkewed			{ get { return (skew > 0.000001 || skew < -0.000001); } }
		public Point		Center				{ get { return new Point(clip.X + clip.Width / 2, clip.Y + clip.Height / 2);} }
		
		public float		ClipConfidence		
		{ 
			get { return this.clipConfidence; } 
			set 
			{
				if (this.clipConfidence != value)
				{
					this.clipConfidence = value;

					ClipChanged?.Invoke(this);
				}
			} 
		 }
		
		public float		SkewConfidence		
		{ 
			get { return this.skewConfidence; } 
			set 
			{
				if (this.skewConfidence != value)
				{
					this.skewConfidence = value;

					ClipChanged?.Invoke(this);
				}
			} 
		}

		/// <summary>
		/// Skew is positive in the clock-wise direction.
		/// </summary>
		public double Skew { get { return skew; } }

		public Point PointUL { get { return TransferUnskewedToSkewedPoint(clip.Location); } }
		public Point PointUR { get { return TransferUnskewedToSkewedPoint(new Point(clip.Right, clip.Y)); } }
		public Point PointLL { get { return TransferUnskewedToSkewedPoint(new Point(clip.X, clip.Bottom)); } }
		public Point PointLR { get { return TransferUnskewedToSkewedPoint(new Point(clip.Right, clip.Bottom)); } }
		public Point[] Points { get { return new Point[] { PointUL, PointUR, PointLR, PointLL }; } }

		#region Overlapping
		public Rectangle Overlapping
		{
			get
			{
				Point	p1 = PointUL;
				Point	p2 = PointUR;
				Point	p3 = PointLL;
				Point	p4 = PointLR;
				
				return new Rectangle(
					Min(p1.X, p2.X, p3.X, p4.X),
					Min(p1.Y, p2.Y, p3.Y, p4.Y),
					Max(p1.X, p2.X, p3.X, p4.X),
					Max(p1.Y, p2.Y, p3.Y, p4.Y));
			}
		}
		#endregion

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region Contains()
		public bool Contains(Point imagePoint)
		{
			return clip.Contains(TransferSkewedToUnskewedPoint(imagePoint));
		}
		#endregion

		#region SetClip()
		internal void SetClip(Rectangle clip)
		{
			if (this.clip != clip)
			{
				this.clip = clip;

				if (content != null)
				{
					content = Rectangle.Intersect(content.Value, this.clip);

					if (content.Value.Width <= 0 || content.Value.Height <= 0)
						content = null;
				}

				ClipChanged?.Invoke(this);
			}
		}
		#endregion

		#region SetSkew()
		internal void SetSkew(double skew, float confidence)
		{
			double newSkew = Math.Min(Math.PI / 4, Math.Max(-Math.PI / 4, skew));

			if (this.skew != newSkew || this.skewConfidence != confidence)
			{
				this.skew = newSkew;
				this.skewConfidence = confidence;

				ClipChanged?.Invoke(this);
			}
		}
		#endregion

		#region SetContent()
		public void SetContent(Rectangle? content, float confidence)
		{
			if (this.content != content || this.clipConfidence != confidence)
			{
				this.content = content;
				this.clipConfidence = confidence;

				ClipChanged?.Invoke(this);
			}
		}
		#endregion

		#region TransferSkewedToUnskewedPoint()
		public Point TransferSkewedToUnskewedPoint(Point point)
		{
			if (this.IsSkewed)
				return Rotation.RotatePoint(point, this.Center, -this.Skew);
			else
				return point;
		}
		#endregion

		#region TransferUnskewedToSkewedPoint()
		public Point TransferUnskewedToSkewedPoint(Point point)
		{
			if (this.IsSkewed)
				return Rotation.RotatePoint(point, this.Center, this.Skew);
			else
				return point;
		}
		#endregion

		#region ResizeSettings()
		public void ResizeSettings(double zoom)
		{
			clip.X = Convert.ToInt32(clip.X * zoom);
			clip.Y = Convert.ToInt32(clip.Y * zoom);
			clip.Width = Convert.ToInt32(clip.Width * zoom);
			clip.Height = Convert.ToInt32(clip.Height * zoom);

			if (content.HasValue)
			{
				content = new Rectangle(Convert.ToInt32(content.Value.X * zoom), Convert.ToInt32(content.Value.Y * zoom), 
					Convert.ToInt32(content.Value.Width * zoom), Convert.ToInt32(content.Value.Height * zoom));
			}
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods
		
		#region Min()
		private int Min(int x1, int x2, int x3, int x4)
		{
			int		min = x1;

			if(min > x2)
				min = x2;
			if(min > x3)
				min = x3;
			if(min > x4)
				min = x4;

			return min;
		}
		#endregion

		#region Max()
		private int Max(int x1, int x2, int x3, int x4)
		{
			int		max = x1;

			if(max < x2)
				max = x2;
			if(max < x3)
				max = x3;
			if(max < x4)
				max = x4;

			return max;
		}
		#endregion

		#endregion
	}
}
