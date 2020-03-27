using System;
using System.Collections;
using System.Drawing;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for CurveOld.
	/// </summary>
	public class CurveOld
	{
		Point[]			points;
		bool			topCurve;
		Clip			clip;
		
		#region constructor
		public CurveOld(Clip clip, Point[] points, bool topCurve)
		{
			this.topCurve = topCurve;
			this.clip = clip;
			
			SetPoints(points);
		}

		public CurveOld(Clip clip, bool topCurve)
		{
			this.clip = clip;
			this.topCurve = topCurve;
			this.points = new Point[7];

			Reset();
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties
		
		#region Points
		public Point[]		Points 
		{
			get	
			{
				/*if(clip.IsAngled)
				{
					Point[]		pointsCurved = new Point[points.Length];

					for(int i = 0; i < points.Length; i++)
						pointsCurved[i] = Rotation.RotatePoint(points[i], clip.Center, clip.Angle);

					return pointsCurved;
				}
				else*/
					return points;	
			}
		}
		#endregion

		#region IsCurved
		public bool IsCurved
		{
			get
			{
				int		y;
			
				for(int i = 0; i < points.Length - 2; i++)
				{
					if(points[i+1].X - points[i].X != 0)
					{
						y = Convert.ToInt32(points[i].Y + (points[i+1].Y - points[i].Y) * (points[i+2].X - points[i].X) / (points[i+1].X - points[i].X));

						if(points[i+2].Y < y - 2 || points[i+2].Y > y + 2)
							return true;
					}
				}

				/*if(this.clip.IsAngled)
				{
					Point[]	p = GetNotAngledPoints(this.points);
					for(int i = 0; i < p.Length - 1; i++)
					{
						if(p[i].Y != p[i+1].Y)
							return true;
					}
				}
				else
				{
					for(int i = 0; i < points.Length - 1; i++)
					{
						if(points[i].Y != points[i+1].Y)
							return true;
					}
				}*/

				return false;
			}
		}
		#endregion

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region GetArray()
		public double[] GetArray()
		{
			Point[]	p = (Point[]) this.Points.Clone();
			//Point[]	p = GetNotAngledPoints(this.points);
		
			int		h0 = p[2].X - p[1].X;
			int		h1 = p[3].X - p[2].X;
			int		h2 = p[4].X - p[3].X;
			int		h3 = p[5].X - p[4].X;
			int[]	h = new int[] {h0,h0,h1,h2,h3,h3};
			double	b0 = (p[2].Y - p[1].Y) / (double) h0;
			double	b1 = (p[3].Y - p[2].Y) / (double) h1;
			double	b2 = (p[4].Y - p[3].Y) / (double) h2;
			double	b3 = (p[5].Y - p[4].Y) / (double) h3;
			double	z0 = 0;
			double	z1 = ( 6*(b2-b1) -((12*(h1+h2)*(b1-b0))/((double)h1)) - ((3*h3*(b3-b2))/((double)h2+h3)) + ((3*h2*h2*(b1-b0))/(h1*(h2+h3))) ) / 
				( h1 - ((4*(h0+h1)*(h1+h2)) / ((double)h1)) + ((h2*h2*(h0+h1))/(double)(h1*(h2+h3))) );

			double		z2 = (6*(b1-b0)-2*z1*(h0+h1)) / (double) (h1);
			double		z3 = (6*(b3-b2) - h2*z2) / (double)(2*(h2+h3));
			double		z4 = 0;
			double[]	z = new double[] {z0,z0,z1,z2,z3,z4,z4};
			double[]	array = new double[p[6].X - p[0].X + 1];
			double		r1,r2,r3,r4;
			int			x0 = p[0].X;

			for(int i = 1; i < 5; i++)
			{
				double	c = (p[i+1].Y / (double) h[i]) - (h[i] * z[i+1] / (double) 6);
				double	d = (p[i].Y / (double) h[i]) - (h[i] * z[i] / (double) 6);
				
				for(int x = p[i].X; x < p[i+1].X; x++)
				{
					r1 = ((z[i+1] * (x-p[i].X)*(x-p[i].X)*(x-p[i].X)) / ((double) (6*h[i])));
					r2 = ((z[i] * (p[i+1].X - x)*(p[i+1].X - x)*(p[i+1].X - x)) / ((double) (6*h[i])));
					r3 = c*(x-p[i].X);
					r4 = d*(p[i+1].X - x);
					
					array[x-x0] = r1+r2+r3+r4;
				}
			}

			for(int x = p[0].X; x < p[1].X; x++)
				array[x-x0] = p[0].Y + (p[1].Y - p[0].Y)*(x - p[0].X) / ((double)(p[1].X - p[0].X));

			for(int x = p[5].X; x <= p[6].X; x++)
				array[x-x0] = p[5].Y + (p[6].Y - p[5].Y)*(x - p[5].X) / ((double)(p[6].X - p[5].X));

			return array;
		}
		#endregion

		#region GetNotAngledArray()
		public double[] GetNotAngledArray()
		{
			Point[]	p = GetNotAngledPoints(this.points);
		
			int		h0 = p[2].X - p[1].X;
			int		h1 = p[3].X - p[2].X;
			int		h2 = p[4].X - p[3].X;
			int		h3 = p[5].X - p[4].X;
			int[]	h = new int[] {h0,h0,h1,h2,h3,h3};
			double	b0 = (p[2].Y - p[1].Y) / (double) h0;
			double	b1 = (p[3].Y - p[2].Y) / (double) h1;
			double	b2 = (p[4].Y - p[3].Y) / (double) h2;
			double	b3 = (p[5].Y - p[4].Y) / (double) h3;
			double	z0 = 0;
			double	z1 = ( 6*(b2-b1) -((12*(h1+h2)*(b1-b0))/((double)h1)) - ((3*h3*(b3-b2))/((double)h2+h3)) + ((3*h2*h2*(b1-b0))/(h1*(h2+h3))) ) / 
				( h1 - ((4*(h0+h1)*(h1+h2)) / ((double)h1)) + ((h2*h2*(h0+h1))/(double)(h1*(h2+h3))) );

			double		z2 = (6*(b1-b0)-2*z1*(h0+h1)) / (double) (h1);
			double		z3 = (6*(b3-b2) - h2*z2) / (double)(2*(h2+h3));
			double		z4 = 0;
			double[]	z = new double[] {z0,z0,z1,z2,z3,z4,z4};
			double[]	array = new double[p[6].X - p[0].X + 1];
			double		r1,r2,r3,r4;
			int			x0 = p[0].X;

			for(int i = 1; i < 5; i++)
			{
				double	c = (p[i+1].Y / (double) h[i]) - (h[i] * z[i+1] / (double) 6);
				double	d = (p[i].Y / (double) h[i]) - (h[i] * z[i] / (double) 6);
				
				for(int x = p[i].X; x < p[i+1].X; x++)
				{
					r1 = ((z[i+1] * (x-p[i].X)*(x-p[i].X)*(x-p[i].X)) / ((double) (6*h[i])));
					r2 = ((z[i] * (p[i+1].X - x)*(p[i+1].X - x)*(p[i+1].X - x)) / ((double) (6*h[i])));
					r3 = c*(x-p[i].X);
					r4 = d*(p[i+1].X - x);
					
					array[x-x0] = r1+r2+r3+r4;
				}
			}

			for(int x = p[0].X; x < p[1].X; x++)
				array[x-x0] = p[0].Y + (p[1].Y - p[0].Y)*(x - p[0].X) / ((double)(p[1].X - p[0].X));

			for(int x = p[5].X; x <= p[6].X; x++)
				array[x-x0] = p[5].Y + (p[6].Y - p[5].Y)*(x - p[5].X) / ((double)(p[6].X - p[5].X));

			return array;
		}
		#endregion
		
		#region Clone()
		public CurveOld Clone(Clip clip)
		{
			Point[]	newPoints = (Point[]) this.points.Clone();
							
			return new CurveOld(clip, newPoints, topCurve);
		}
		#endregion

		#region ClipChanged()
		public void ClipChanged()
		{
			Rectangle	rect = clip.RectangleNotAngled;

			if(this.clip.IsAngled)
			{
				Point[]	p = GetNotAngledPoints(this.points);

				if(IsCurved == false || rect.X >= p[2].X - 1 || rect.Right <= p[5].X + 1)
				{
					Reset();
					return;
				}

				for(int i = 0; i < p.Length; i++)
				{
					if(p[i].Y < rect.Y)
						p[i].Y = rect.Y;
					if(p[i].Y > rect.Bottom)
						p[i].Y = rect.Bottom;
				}

				this.points = GetAngledPoints(p);
			
			}
			else
			{
				if(IsCurved == false || rect.X >= points[2].X - 1 || rect.Right <= points[5].X + 1)
				{
					Reset();
					return;
				}

				for(int i = 0; i < points.Length; i++)
				{
					if(points[i].Y < rect.Y)
						points[i].Y = rect.Y;
					if(points[i].Y > rect.Bottom)
						points[i].Y = rect.Bottom;
				}
			}

			SetFirstAndLastPoint();
		}
		#endregion

		#region SetPoints()
		public void SetPoints(Point[] points)
		{
			
			if(points.Length == 5)
			{					
				this.points = new Point[7];
				this.points[0] = Point.Empty;
				this.points[6] = Point.Empty;

				for(int i = 1; i < 6; i++)
				{
					this.points[i] = points[i-1];
				}
				
				SetFirstAndLastPoint();
			}
			else
			{
				this.points = points;
			}
		}
		#endregion
		
		#region Shift()
		public void Shift(Size shift)
		{		
			for(int i = 1; i < this.points.Length - 1; i++)
			{
				if(this.clip.IsAngled == false)
				{
					Rectangle	rect = clip.RectangleNotAngled;
					Point		p = this.points[i];
					p.Offset(shift.Width, shift.Height);
			
					p.X = (p.X - this.points[i-1].X >  30) ? p.X : this.points[i-1].X + 30;
					p.X = (p.X - this.points[i+1].X < -30) ? p.X : this.points[i+1].X - 30;
				
					p.Y = (p.Y > rect.Y) ? ((p.Y < rect.Bottom) ? p.Y : rect.Bottom) : rect.Y;

					this.points[i].X = p.X;
					this.points[i].Y = p.Y;
				}
				else
				{
					Rectangle	rect = clip.RectangleNotAngled;
					Point		p = this.points[i];
					p.Offset(shift.Width, shift.Height);

					p.X = (p.X - this.points[i-1].X >  30) ? p.X : this.points[i-1].X + 30;
					p.X = (p.X - this.points[i+1].X < -30) ? p.X : this.points[i+1].X - 30;

					p = Rotation.RotatePoint(p, this.clip.Center, -this.clip.Angle);
					p.Y = (p.Y > rect.Y) ? ((p.Y < rect.Bottom) ? p.Y : rect.Bottom) : rect.Y;
					p = Rotation.RotatePoint(p, this.clip.Center, this.clip.Angle);

					this.points[i] = p;
				}
			}
						
			SetFirstAndLastPoint();
		}
		#endregion

		#region SetPoint()
		public void SetPoint(int index, Point mousePoint)
		{
			if(index > 0 && index < 6)
			{
				if(this.clip.IsAngled == false)
				{
					Rectangle	rect = clip.RectangleNotAngled;
					Point		p0 = this.points[index-1];
					Point		p2 = this.points[index+1];
			
					mousePoint.X = (mousePoint.X - p0.X > 30) ? mousePoint.X : p0.X + 30;
					mousePoint.X = (mousePoint.X - p2.X < -30) ? mousePoint.X : p2.X - 30;
				
					mousePoint.Y = (mousePoint.Y > rect.Y) ? ((mousePoint.Y < rect.Bottom) ? mousePoint.Y : rect.Bottom) : rect.Y;

					this.points[index].X = mousePoint.X;
					this.points[index].Y = mousePoint.Y;
					SetFirstAndLastPoint();
				}
				else
				{
					Rectangle	rect = clip.RectangleNotAngled;
					Point		p0 = this.points[index-1];
					Point		p2 = this.points[index+1];
			
					mousePoint.X = (mousePoint.X - p0.X > 30) ? mousePoint.X : p0.X + 30;
					mousePoint.X = (mousePoint.X - p2.X < -30) ? mousePoint.X : p2.X - 30;

					mousePoint = Rotation.RotatePoint(mousePoint, this.clip.Center, -this.clip.Angle);
					mousePoint.Y = (mousePoint.Y > rect.Y) ? ((mousePoint.Y < rect.Bottom) ? mousePoint.Y : rect.Bottom) : rect.Y;
					mousePoint = Rotation.RotatePoint(mousePoint, this.clip.Center, this.clip.Angle);

					this.points[index] = mousePoint;
					SetFirstAndLastPoint();
				}
			}
		}
		#endregion

		#region Reset()
		public void Reset()
		{
			Rectangle	rect = clip.RectangleNotAngled;
			
			int			y = (topCurve) ? rect.Y : rect.Bottom;
			
			this.points[0] = new Point(rect.X, y); 
			this.points[1] = new Point(rect.X + (rect.Width) / 6, y);
			this.points[2] = new Point(rect.X + (rect.Width) / 3, y);
			this.points[3] = new Point(rect.X + (rect.Width) / 2, y);
			this.points[4] = new Point(rect.X + (rect.Width) * 2 / 3, y);
			this.points[5] = new Point(rect.X + (rect.Width) * 5 / 6, y);
			this.points[6] = new Point(rect.Right, y);

			if(this.clip.IsAngled)
				this.points = GetAngledPoints(this.points);
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods
		
		#region SetFirstAndLastPoint()
		private void SetFirstAndLastPoint()
		{
			Rectangle	rect = clip.RectangleNotAngled;

			if(this.clip.IsAngled == false)
			{
				this.points[0].X = rect.X;
				if(this.points[2].X - this.points[1].X != 0)
					this.points[0].Y = this.points[1].Y - ((this.points[2].Y - this.points[1].Y) * (this.points[1].X - this.points[0].X)) / (this.points[2].X - this.points[1].X);
				else
					this.points[0].Y = this.points[1].Y;
				if(this.points[0].Y < rect.Y)
					this.points[0].Y = rect.Y;
				if(this.points[0].Y > rect.Bottom)
					this.points[0].Y = rect.Bottom;

				this.points[6].X = rect.Right;
				if(this.points[5].X - this.points[4].X != 0)
					this.points[6].Y = this.points[5].Y + ((this.points[5].Y - this.points[4].Y) * (this.points[6].X - this.points[5].X)) / (this.points[5].X - this.points[4].X);
				else
					this.points[6].Y = this.points[5].Y;
				if(this.points[6].Y < rect.Y)
					this.points[6].Y = rect.Y;
				if(this.points[6].Y > rect.Bottom)
					this.points[6].Y = rect.Bottom;
			}
			else
			{
				Point[]		p = GetNotAngledPoints(this.points);

				p[0].X = rect.X;
				if(p[2].X - p[1].X != 0)
					p[0].Y = p[1].Y - ((p[2].Y - p[1].Y) * (p[1].X - p[0].X)) / (p[2].X - p[1].X);
				else
					p[0].Y = p[1].Y;

				if(p[0].Y < rect.Y)
					p[0].Y = rect.Y;
				if(p[0].Y > rect.Bottom)
					p[0].Y = rect.Bottom;

				p[6].X = rect.Right;
				if(p[5].X - p[4].X != 0)
					p[6].Y = p[5].Y + ((p[5].Y - p[4].Y) * (p[6].X - p[5].X)) / (p[5].X - p[4].X);
				else
					p[6].Y = p[5].Y;

				if(p[6].Y < rect.Y)
					p[6].Y = rect.Y;
				if(p[6].Y > rect.Bottom)
					p[6].Y = rect.Bottom;

				this.points = GetAngledPoints(p);
			}
		}
		#endregion

		#region GetNotAngledPoints()
		private Point[] GetNotAngledPoints(Point[] sourcePoints)
		{
			if(this.clip.IsAngled)
			{
				Point[]		destPoints = new Point[sourcePoints.Length];
			
				for(int i = 0; i < destPoints.Length; i++)
					destPoints[i] = Rotation.RotatePoint(sourcePoints[i], this.clip.Center, - this.clip.Angle);

				return destPoints;
			}
			else
				return (Point[]) sourcePoints.Clone();
		}
		#endregion

		#region GetAngledPoints()
		private Point[] GetAngledPoints(Point[] sourcePoints)
		{
			if(this.clip.IsAngled)
			{
				Point[]		destPoints = new Point[sourcePoints.Length];
			
				for(int i = 0; i < destPoints.Length; i++)
					destPoints[i] = Rotation.RotatePoint(sourcePoints[i], this.clip.Center, this.clip.Angle);

				return destPoints;
			}
			else
				return (Point[]) sourcePoints.Clone();
		}
		#endregion

		#endregion
	}
}
