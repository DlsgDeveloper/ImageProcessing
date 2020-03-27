using System;
using System.Drawing;

using BIP.Geometry;


namespace ImageProcessing.IpSettings
{
	/// <summary>
	/// Summary description for Clip.
	/// </summary>
	public class Clip
	{		
		RatioRect clip;
		RatioRect?	content = null;
		double		skew;
		float		clipConfidence = 1.0F;
		float		skewConfidence = 1.0F;
		double		imageWidthHeightRatio = 1;

		public delegate void ClipChangedHnd(ImageProcessing.IpSettings.Clip clip);
		public event ClipChangedHnd ClipChanged;
		
		
		#region constructor
		public Clip(double imageWidthHeightRatio)
			: this(0, 0, 1, 1, imageWidthHeightRatio)
		{
		}

		public Clip(RatioRect clip, double imageWidthHeightRatio)
			: this(clip.X, clip.Y, clip.Width, clip.Height, imageWidthHeightRatio)
		{
		}

		public Clip(double x, double y, double width, double height, double imageWidthHeightRatio)
		{
			this.clip = GetValidatedPageClip(x, y, width, height);
			this.skew = 0.0;
			this.skewConfidence = 1.0F;
			this.imageWidthHeightRatio = imageWidthHeightRatio;
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties
		
		public bool			ClipSpecified		{ get { return this.clip.IsDefault == false || this.skew != 0; } }
		public RatioRect	RectangleNotSkewed	{ get { return this.clip;} }
		public bool			ContentDefined		{ get { return this.content != null; } }
		public RatioRect	Content				{ get { return (ContentDefined) ? this.content.Value : RectangleNotSkewed; } }
		public bool			IsSkewed			{ get { return (skew > 0.000001 || skew < -0.000001); } }
		public RatioPoint	Center				{ get { return new RatioPoint(clip.X + clip.Width / 2.0F, clip.Y + clip.Height / 2.0F);} }

		#region ClipConfidence
		public float		ClipConfidence		
		{ 
			get { return this.clipConfidence; } 
			set 
			{
				if (this.clipConfidence != value)
				{
					this.clipConfidence = value;

					if (ClipChanged != null)
						ClipChanged(this);
				}
			}
		}
		#endregion

		#region SkewConfidence
		public float SkewConfidence		
		{ 
			get { return this.skewConfidence; } 
			set 
			{
				if (this.skewConfidence != value)
				{
					this.skewConfidence = value;

					if (ClipChanged != null)
						ClipChanged(this);
				}
			}
		}
		#endregion

		/// <summary>
		/// Skew is positive in the clock-wise direction.
		/// </summary>
		public double Skew { get { return skew; } }

		public RatioPoint PointUL { get { return TransferUnskewedToSkewedPoint(new RatioPoint(clip.X, clip.Y)); } }
		public RatioPoint PointUR { get { return TransferUnskewedToSkewedPoint(new RatioPoint(clip.Right, clip.Y)); } }
		public RatioPoint PointLL { get { return TransferUnskewedToSkewedPoint(new RatioPoint(clip.X, clip.Bottom)); } }
		public RatioPoint PointLR { get { return TransferUnskewedToSkewedPoint(new RatioPoint(clip.Right, clip.Bottom)); } }
		public RatioPoint[] Points { get { return new RatioPoint[] { PointUL, PointUR, PointLR, PointLL }; } }

		public RatioPoint PointULNotSkewed { get { return new RatioPoint(clip.X, clip.Y); } }
		public RatioPoint PointURNotSkewed { get { return new RatioPoint(clip.Right, clip.Y); } }
		public RatioPoint PointLLNotSkewed { get { return new RatioPoint(clip.X, clip.Bottom); } }
		public RatioPoint PointLRNotSkewed { get { return new RatioPoint(clip.Right, clip.Bottom); } }

		#region Overlapping
		public RatioRect Overlapping
		{
			get
			{
				RatioPoint	p1 = PointUL;
				RatioPoint	p2 = PointUR;
				RatioPoint	p3 = PointLL;
				RatioPoint	p4 = PointLR;
				
				return new RatioRect(
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

		#region Reset()
		public void Reset()
		{
			if (this.clip.IsDefault == false || this.content != null || this.skew != 0 || this.clipConfidence != 1.0F || this.skewConfidence != 1.0F)
			{
				this.clip = new RatioRect(0, 0, 1, 1);
				this.content = null;
				this.skew = 0;
				this.clipConfidence = 1.0F;
				this.skewConfidence = 1.0F;

				if (ClipChanged != null)
					ClipChanged(this);
			}
		}
		#endregion

		#region Contains()
		public bool Contains(RatioPoint imagePoint)
		{
			return clip.Contains(TransferSkewedToUnskewedPoint(imagePoint));
		}
		#endregion

		#region SetClip()
		public void SetClip(RatioRect clip)
		{
			clip = GetValidatedPageClip(clip);
			
			if (this.clip != clip)
			{
				this.clip = clip;

				if (content != null)
				{
					content = RatioRect.Intersect(content.Value, this.clip);

					if (content.Value.Width <= 0 || content.Value.Height <= 0)
						content = null;
				}

				ClipChanged?.Invoke(this);
			}
		}
		#endregion

		#region SetSkew()
		/// <summary>
		/// in radians
		/// </summary>
		/// <param name="skew"></param>
		/// <param name="confidence"></param>
		public void SetSkew(double skew, float confidence)
		{
			double newSkew = Math.Min(Math.PI / 4, Math.Max(-Math.PI / 4, skew));

			if (this.skew != newSkew || this.skewConfidence != confidence)
			{
				this.skew = newSkew;
				this.skewConfidence = confidence;

				if (ClipChanged != null)
					ClipChanged(this);
			}
		}
		#endregion

		#region SetContent()
		public void SetContent(RatioRect? content, float confidence)
		{
			if (this.content != content || this.clipConfidence != confidence)
			{
				this.content = content;
				this.clipConfidence = confidence;

				if (ClipChanged != null)
					ClipChanged(this);
			}
		}
		#endregion

		#region TransferSkewedToUnskewedPoint()
		public RatioPoint TransferSkewedToUnskewedPoint(RatioPoint point)
		{
			if (this.IsSkewed)
				return ImageProcessing.BigImages.Rotation.RotatePoint(point, this.Center, -this.Skew, this.imageWidthHeightRatio);
			else
				return point;
		}
		#endregion

		#region TransferUnskewedToSkewedPoint()
		public RatioPoint TransferUnskewedToSkewedPoint(RatioPoint point)
		{
			if (this.IsSkewed)
				return ImageProcessing.BigImages.Rotation.RotatePoint(point, this.Center, this.Skew, this.imageWidthHeightRatio);
			else
				return point;
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods
		
		#region Min()
		private double Min(double x1, double x2, double x3, double x4)
		{
			double		min = x1;

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
		private double Max(double x1, double x2, double x3, double x4)
		{
			double max = x1;

			if(max < x2)
				max = x2;
			if(max < x3)
				max = x3;
			if(max < x4)
				max = x4;

			return max;
		}
		#endregion

		#region GetValidatedPageClip()
		private RatioRect GetValidatedPageClip(RatioRect clip)
		{
			double x = clip.X, y = clip.Y, right = clip.Right, bottom = clip.Bottom;
			
			if (x < 0)
				x = 0;
			else if (x > 0.98)
				x = 0.98;

			if (y < 0)
				y = 0;
			else if (y > 0.98)
				y = 0.98;

			if (right > 1)
				right = 1;
			if (bottom > 1)
				bottom = 1;
			
			return RatioRect.FromLTRB(x, y, right, bottom);
		}

		private RatioRect GetValidatedPageClip(double x, double y, double width, double height)
		{
			double right = x + width;
			double bottom = y + height;
			
			if (x < 0)
				x = 0;
			else if (x > 0.98)
				x = 0.98;

			if (y < 0)
				y = 0;
			else if (y > 0.98)
				y = 0.98;

			if (right > 1)
				right = 1;
			if (bottom > 1)
				bottom = 1;

			return RatioRect.FromLTRB(x, y, right, bottom);
		}
		#endregion

		#endregion
	}
}
