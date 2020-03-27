using System;
using System.Collections;
using System.Drawing;
using System.Linq;

using BIP.Geometry;
using System.Collections.Generic;
using ImageProcessing.Languages;


namespace ImageProcessing.IpSettings
{
	/// <summary>
	/// Summary description for Curve.
	/// </summary>
	public class Curve
	{
		ImageProcessing.IpSettings.ItPage itPage;
		
		BfPoints bfPoints = new BfPoints();
		bool								isTopCurve;
		//ImageProcessing.IpSettings.Clip		clip;
		double								minPointsDistance = 0.02;
		int									defaultPointsCount = 8;

		public event ImageProcessing.IpSettings.BfPoint.BfPointHnd PointAdding;
		public event ImageProcessing.IpSettings.BfPoint.BfPointHnd PointAdded;
		public event ImageProcessing.IpSettings.BfPoint.BfPointHnd PointRemoving;
		public event ImageProcessing.IpSettings.BfPoint.BfPointHnd PointRemoved;
		public event ImageProcessing.IpSettings.BfPoint.VoidHnd Clearing;
		public event ImageProcessing.IpSettings.BfPoint.VoidHnd Cleared;

		public event	ImageProcessing.IpSettings.ItImage.VoidHnd Changed;
		bool			raiseChangedEvent = true;
		bool			changed = false;


		#region constructor
		private Curve()
		{
			this.bfPoints.PointAdding += delegate(ImageProcessing.IpSettings.BfPoint bfPoint) { if (PointAdding != null) PointAdding(bfPoint); };
			this.bfPoints.PointAdded += delegate(ImageProcessing.IpSettings.BfPoint bfPoint) { if(PointAdded != null) PointAdded(bfPoint); };
			this.bfPoints.PointRemoving += delegate(ImageProcessing.IpSettings.BfPoint bfPoint) { if(PointRemoving != null) PointRemoving(bfPoint); };
			this.bfPoints.PointRemoved += delegate(ImageProcessing.IpSettings.BfPoint bfPoint) { if(PointRemoved != null) PointRemoved(bfPoint); };
			this.bfPoints.Clearing += delegate() { if(Clearing != null) Clearing(); };
			this.bfPoints.Cleared += delegate() { if(Cleared != null) Cleared(); };
			
			this.bfPoints.Changed += new ItImage.VoidHnd(BfPoints_Changed);
		}

		public Curve(ImageProcessing.IpSettings.ItPage itPage, RatioPoint[] curvePoints, bool isTopCurve)
			:this()
		{
			this.isTopCurve = isTopCurve;
			this.itPage = itPage;

			this.minPointsDistance = 0.25 / this.itPage.ItImage.InchSize.Width;

			SetPoints(curvePoints);
		}

		public Curve(ImageProcessing.IpSettings.ItPage itPage, bool isTopCurve)
			: this()
		{
			this.itPage = itPage;
			this.isTopCurve = isTopCurve;
			
			this.minPointsDistance = 0.25 / this.itPage.ItImage.InchSize.Width;

			Reset();
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties

		public bool			IsTopCurve { get { return isTopCurve; } }
		public BfPoints		BfPoints { get { return bfPoints; } }
		public RatioPoint[]	Points { get { return bfPoints.GetPoints(); } }

		#region PointsNotSkewed
		public RatioPoint[] PointsNotSkewed
		{
			get
			{
				BfPoints bfPoints = GetNotSkewedPoints();
				return bfPoints.GetPoints();
			}
		}
		#endregion

		#region IsCurved
		public bool IsCurved
		{
			get
			{
				if (bfPoints.Count < 3)
					return false;
				else
				{
					System.Collections.Generic.List<double> angles = new System.Collections.Generic.List<double>();

					for (int i = 1; i < bfPoints.Count; i++)
						if (bfPoints[i].X - bfPoints[i-1].X > 0)
							angles.Add(Math.Atan2(bfPoints[i].Y - bfPoints[i-1].Y, bfPoints[i].X - bfPoints[i-1].X));

					if (angles.Count >= 2)
					{
						double minAngle = angles[0];
						double maxAngle = angles[0];

						foreach (double angle in angles)
						{
							if (minAngle > angle)
								minAngle = angle;
							if (maxAngle < angle)
								maxAngle = angle;
						}

						//max angle difference is 1 degree
						return (maxAngle - minAngle < -(Math.PI / 180.0) || maxAngle - minAngle > (Math.PI / 180.0));
					}
					else 
						return false;
				}
			}
		}
		#endregion

		#region Rectangle
		public RatioRect Rectangle
		{
			get
			{
				RatioRect rectangle = RatioRect.Empty;

				if (bfPoints.Count > 0)
				{
					rectangle = new RatioRect(bfPoints[0].X, bfPoints[0].Y, 0, 0);
					
					foreach (ImageProcessing.IpSettings.BfPoint bfPoint in bfPoints)
						rectangle = RatioRect.FromLTRB(Math.Min(rectangle.X, bfPoint.X), Math.Min(rectangle.Y, bfPoint.Y),
							Math.Max(rectangle.Right, bfPoint.X), Math.Max(rectangle.Bottom, bfPoint.Y));
				}

				return rectangle;
			}
		}
		#endregion

		#region RaiseChangedEvent()
		public bool RaiseChangedEvent 
		{ 
			get { return this.raiseChangedEvent; }
			set
			{
				if (this.raiseChangedEvent != value)
				{
					this.raiseChangedEvent = value;

					if (this.raiseChangedEvent && changed)
					{
						changed = false;

						if (Changed != null)
							Changed();
					}
				}
			}
		}
		#endregion

		#endregion


		//PRIVATE PROPERTIES
		#region private properties

		private ImageProcessing.IpSettings.Clip Clip { get { return this.itPage.Clip; } }

		#region FirstPoint
		private ImageProcessing.IpSettings.BfPoint FirstPoint
		{
			get
			{
				if (this.bfPoints.Count == 0)
					return null;

				return this.bfPoints[0];
			}
		}
		#endregion

		#region LastPoint
		private ImageProcessing.IpSettings.BfPoint LastPoint
		{
			get
			{
				if (this.bfPoints.Count == 0)
					return null;

				return this.bfPoints[this.bfPoints.Count - 1];
			}
		}
		#endregion

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region GetArray()
		/*public static double[] GetArray(RatioPoint[] points)
		{
			RatioPoint[] pointsF = new RatioPoint[points.Length];

			for (int i = 0; i < pointsF.Length; i++)
				pointsF[i] = new RatioPoint(points[i].X, points[i].Y);

			return GetArray(pointsF);
		}*/

		public static double[] GetArray(RatioPoint[] points, Size imageSize)
		{
			if (points.Length == 0)
				return new double[0];
			else if (points.Length == 1)
			{
				double[] array = new double[1];
				array[0] = 0;
				return array;
			}
			else if (points.Length == 2)
			{
				double[] array = new double[Convert.ToInt32((points[1].X - points[0].X) * imageSize.Width)];

				for (int i = 0; i < array.Length; i++)
					array[i] = ((points[1].Y - points[0].Y) * (i) / (points[1].X - points[0].X)) * imageSize.Height;

				return array;
			}
			else
			{
				return GetCurve(points, imageSize);
			}
		}

		public static double[] GetArray(Point[] points)
		{
			if (points.Length == 0)
				return new double[0];

			else if (points.Length == 1)
			{
				double[] array = new double[1];
				array[0] = 0;
				return array;
			}
			else if (points.Length == 2)
			{
				double[] array = new double[(points[1].X - points[0].X)];

				for (int i = 0; i < array.Length; i++)
					array[i] = points[0].Y + ((points[1].Y - points[0].Y) * (i) / (double)(points[1].X - points[0].X));

				return array;
			}
			else
			{
				return GetCurve(points);
			}
		}

		/*public double[] GetArray()
		{
			if (this.IsCurved)
			{
				return GetCurve(this.bfPoints.GetPoints());
			}
			else
			{
				return GetLine();
			}
		}*/
		#endregion

		#region GetImagePoints()
		public Point[] GetImagePoints(Size imageSize)
		{
			Point[] points = new Point[this.BfPoints.Count];

			for (int i = 0; i < this.bfPoints.Count; i++)
				points[i] = new Point(Convert.ToInt32(this.bfPoints[i].X * imageSize.Width), Convert.ToInt32(this.bfPoints[i].Y * imageSize.Height));

			return points;
		}
		#endregion

		#region GetNotAngledArray()	
		public double[] GetNotAngledArray(Size imageSize, int arrayWidth)
		{
			if (this.IsCurved)
			{
				BfPoints p = GetNotSkewedPoints();

				if (p.Count > 0)
				{
					p[0].X = this.Clip.RectangleNotSkewed.X;
					p[p.Count - 1].X =  this.Clip.RectangleNotSkewed.Right;
				}

				double[] curve = GetCurve(p.GetPoints(), imageSize);

				if (curve.Length == arrayWidth)
				{
					return curve;
				}
				else if (curve.Length > arrayWidth)
				{
					double[] c = new double[arrayWidth];

					for (int i = 0; i < arrayWidth; i++)
						c[i] = curve[i];

					return c;
				}
				else
				{
					double[] c = new double[arrayWidth];

					for (int i = 0; i < curve.Length; i++)
						c[i] = curve[i];

					for (int i = curve.Length; i < c.Length; i++)
						c[i] = curve[curve.Length - 1];

					return c;
				}
			}
			else
			{
				return GetNotAngledLine(imageSize, arrayWidth);
			}
		}
		#endregion

		#region GetNotSkewedArray()
		/*public double[] GetNotSkewedArray(Size imageSize)
		{
			if (this.IsCurved)
			{
				BfPoints p = GetNotSkewedPoints();

				if (p.Count > 0)
				{
					p[0].X =  this.Clip.RectangleNotSkewed.X;
					p[p.Count - 1].X =  this.Clip.RectangleNotSkewed.Right;
				}

				return GetCurve(p.GetPoints(), imageSize);
			}
			else
			{
				return GetLine(GetNotSkewedPoints(), imageSize);
			}
		}*/
		#endregion

		#region Clone()
		public Curve Clone(ImageProcessing.IpSettings.ItPage itPage)
		{
			RatioPoint[]	newPoints = (RatioPoint[]) this.Points.Clone();
							
			return new Curve(itPage, newPoints, isTopCurve);
		}
		#endregion

		#region ClipChanged()
		public void ClipChanged()
		{
			this.RaiseChangedEvent = false;

			if (this.IsCurved)
				ValidatePoints();
			else
				Reset();

			this.RaiseChangedEvent = true;
		}
		#endregion

		#region AddPoint()
		public void AddPoint(RatioPoint imagePoint)
		{
			this.RaiseChangedEvent = false;

			RatioRect rect = this.Clip.RectangleNotSkewed;
			RatioPoint		pNotAngled = this.Clip.TransferSkewedToUnskewedPoint(imagePoint);
			BfPoints		p = GetNotSkewedPoints();

			if (rect.Contains(pNotAngled))
			{
				for(int i = 1; i < p.Count; i++)
					if (pNotAngled.X < p[i].X)
					{
						if ((pNotAngled.X - p[i - 1].X > minPointsDistance) && ((i == p.Count - 1) || (p[i].X - pNotAngled.X > minPointsDistance)))
							this.bfPoints.Add(new ImageProcessing.IpSettings.BfPoint(imagePoint));

						break;
					}
			}

			ValidatePoints();
			this.RaiseChangedEvent = true;
		}
		#endregion

		#region RemovePoint()
		public bool RemovePoint(ImageProcessing.IpSettings.BfPoint bfPoint)
		{
			return RemovePoint(this.bfPoints.IndexOf(bfPoint));
		}

		public bool RemovePoint(int index)
		{
			this.RaiseChangedEvent = false;
			
			if (index > 0 && index < bfPoints.Count - 1)
			{
				this.bfPoints.RemoveAt(index);
				return true;
			}
			else
				return false;

			//this.RaiseChangedEvent = true;
		}
		#endregion

		#region Shift()
		/// <summary>
		/// Shifts entire curve either up or down.
		/// </summary>
		/// <param name="dy">If bigger than 0, curve is shifted down, otherwise up.</param>
		public void Shift(double dy)
		{
			this.RaiseChangedEvent = false;
			RatioRect rect = this.Clip.RectangleNotSkewed;
						
			foreach(ImageProcessing.IpSettings.BfPoint bfPoint in this.bfPoints)
			{
				ImageProcessing.IpSettings.BfPoint aPoint = TransferSkewedToUnskewedPoint(bfPoint);

				aPoint.Y = Math.Max(rect.Y, Math.Min(rect.Bottom, aPoint.Y + dy));
				aPoint = TransferUnskewedToSkewedPoint(aPoint);

				bfPoint.Set(aPoint.X, aPoint.Y);
			}
			this.RaiseChangedEvent = true;
		}
		#endregion

		#region SetPoints()
		public void SetPoints(RatioPoint[] curvePoints)
		{
			this.RaiseChangedEvent = false;
			this.bfPoints.Clear();

			List<RatioPoint> points = ValidateEntryPoints(curvePoints);

			foreach (RatioPoint point in points)
				this.bfPoints.Add(new ImageProcessing.IpSettings.BfPoint(point));

			this.bfPoints.MarkEdgePoints();

			ValidatePoints();
			this.RaiseChangedEvent = true;
		}
		#endregion

		#region SetPoint()
		public void SetPoint(ImageProcessing.IpSettings.BfPoint bfPoint, RatioPoint imagePoint)
		{
			SetPoint(this.bfPoints.IndexOf(bfPoint), imagePoint);
		}

		public void SetPoint(int index, RatioPoint imagePoint)
		{
			if (index >= 0 && index < this.bfPoints.Count)
			{
				RatioRect rect =  this.Clip.RectangleNotSkewed;

				if (index == 0)
				{
					ImageProcessing.IpSettings.BfPoint firstPoint = FirstPoint;
					imagePoint = this.Clip.TransferSkewedToUnskewedPoint(imagePoint);

					if (firstPoint != null)
					{
						RatioPoint aPoint = this.Clip.TransferUnskewedToSkewedPoint(new RatioPoint(rect.X, Math.Max(rect.Y, Math.Min(rect.Bottom, imagePoint.Y))));
						//firstPoint.Set(aPoint.X, Math.Max(rect.Y, Math.Min(rect.Bottom, aPoint.Y)));
						firstPoint.Set(aPoint.X, aPoint.Y);
					}
				}
				else if (index == this.bfPoints.Count - 1)
				{
					ImageProcessing.IpSettings.BfPoint lastPoint = LastPoint;
					imagePoint = this.Clip.TransferSkewedToUnskewedPoint(imagePoint);

					if (lastPoint != null)
					{
						RatioPoint aPoint = this.Clip.TransferUnskewedToSkewedPoint(new RatioPoint(rect.Right, Math.Max(rect.Y, Math.Min(rect.Bottom, imagePoint.Y))));
						//lastPoint.Set(aPoint.X, Math.Max(rect.Y, Math.Min(rect.Bottom, aPoint.Y)));
						lastPoint.Set(aPoint.X, aPoint.Y);
					}
				}
				else if (index > 0 && index < this.bfPoints.Count - 1)
				{
					ImageProcessing.IpSettings.BfPoint bfPoint = bfPoints[index];
					ImageProcessing.IpSettings.BfPoint previous = bfPoints[index - 1];
					ImageProcessing.IpSettings.BfPoint next = bfPoints[index + 1];

					if (previous != null)
						imagePoint.X = (imagePoint.X - previous.X > minPointsDistance) ? imagePoint.X : previous.X + minPointsDistance;
					if (next != null)
						imagePoint.X = (next.X - imagePoint.X > minPointsDistance) ? imagePoint.X : next.X - minPointsDistance;

					imagePoint = this.Clip.TransferSkewedToUnskewedPoint(imagePoint);
					imagePoint.Y = Math.Max(rect.Y, Math.Min(rect.Bottom, imagePoint.Y));
					imagePoint = this.Clip.TransferUnskewedToSkewedPoint(imagePoint);

					bfPoint.Set(imagePoint.X, imagePoint.Y);
				}
			}
		}
		#endregion

		#region Reset()
		public void Reset()
		{
			this.RaiseChangedEvent = false;
			RatioRect rect = this.Clip.RectangleNotSkewed;
			
			this.bfPoints.Clear();

			if (isTopCurve)
			{
				for (int i = 0; i < defaultPointsCount; i++)
				{
					ImageProcessing.IpSettings.BfPoint bfPoint = new ImageProcessing.IpSettings.BfPoint(rect.X + i * rect.Width  / (defaultPointsCount - 1.0), rect.Y);
					bfPoints.Add(TransferUnskewedToSkewedPoint(bfPoint));
				}
			}
			else
			{
				for (int i = 0; i < defaultPointsCount; i++)
				{
					ImageProcessing.IpSettings.BfPoint bfPoint = new ImageProcessing.IpSettings.BfPoint(rect.X + i * rect.Width / (defaultPointsCount - 1.0), rect.Bottom);
					bfPoints.Add(TransferUnskewedToSkewedPoint(bfPoint));
				}
			}

			bfPoints.MarkEdgePoints();
			//ValidatePoints();
			this.RaiseChangedEvent = true;
		}
		#endregion

		#region TransferSkewedToUnskewedPoint()
		public ImageProcessing.IpSettings.BfPoint TransferSkewedToUnskewedPoint(ImageProcessing.IpSettings.BfPoint bfPoint)
		{
			ImageProcessing.IpSettings.BfPoint bfPointUnrotated = new ImageProcessing.IpSettings.BfPoint(this.Clip.TransferSkewedToUnskewedPoint(bfPoint.RatioPoint));

			bfPointUnrotated.IsEdgePoint = bfPoint.IsEdgePoint;
			return bfPointUnrotated;
		}
		#endregion

		#region TransferUnskewedToSkewedPoint()
		public ImageProcessing.IpSettings.BfPoint TransferUnskewedToSkewedPoint(ImageProcessing.IpSettings.BfPoint point)
		{
			ImageProcessing.IpSettings.BfPoint bfPointSkewed = new ImageProcessing.IpSettings.BfPoint(this.Clip.TransferUnskewedToSkewedPoint(point.RatioPoint));

			bfPointSkewed.IsEdgePoint = point.IsEdgePoint;
			return bfPointSkewed;
		}
		#endregion

		#region GetPagePoint()
		public RatioPoint GetPagePoint(ImageProcessing.IpSettings.BfPoint point)
		{
			RatioRect pageRect = this.Clip.RectangleNotSkewed;
			RatioPoint pagePoint = this.Clip.TransferSkewedToUnskewedPoint(point.RatioPoint);

			if (pageRect.Width > 0 && pageRect.Height > 0)
				return new RatioPoint((pagePoint.X - pageRect.X) / pageRect.Width, (pagePoint.Y - pageRect.Y) / pageRect.Height);
			else
				return new RatioPoint(0, 0);
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region GetNotSkewedPoints()
		private BfPoints GetNotSkewedPoints()
		{
			if (this.Clip.IsSkewed)
			{
				BfPoints destPoints = new BfPoints();

				for (int i = 0; i < this.bfPoints.Count; i++)
				{
					ImageProcessing.IpSettings.BfPoint bfPoint = TransferSkewedToUnskewedPoint(this.bfPoints[i]);

					destPoints.Add(bfPoint);
				}

				return destPoints;
			}
			else
				return (BfPoints)this.bfPoints.Clone();
		}
		#endregion

		#region ValidatePoints()
		/// <summary>
		/// Deletes point if:
		///    1) Point is edge point and distance to the nearby point is less than 'minPointsDistance'.
		///    2) Point is not edge point and point's X component is out of clip.
		///    3) Point is not edge point and distance between prev and next point is less than 2 * 'minPointsDistance'.
		/// </summary>
		private void ValidatePoints()
		{
			BfPoints	p = GetNotSkewedPoints();
			RatioRect	rect = this.Clip.RectangleNotSkewed;

			for (int i = bfPoints.Count - 1; i >= 0; i--)
			{
				if (p[i].IsEdgePoint && (i == 0) && (p.Count >= 2) && (p[i + 1].X - p[i].X < minPointsDistance))
					this.bfPoints.RemoveAt(i);
				else if (p[i].IsEdgePoint && (i == p.Count - 1) && (p.Count >= 2) && (p[i].X - p[i - 1].X < minPointsDistance))
					this.bfPoints.RemoveAt(i);
				else if ((p[i].IsEdgePoint == false) && (p[i].X < rect.X || p[i].X > rect.Right))
					this.bfPoints.RemoveAt(i);
				else if ((p[i].IsEdgePoint == false) && (i > 0) && (i < this.bfPoints.Count - 1) && (p[i + 1].X - p[i - 1].X < minPointsDistance * 2))
					this.bfPoints.RemoveAt(i);
				else
				{
					p[i].X = Math.Max(rect.X, Math.Min(rect.Right, p[i].X));
					p[i].Y = Math.Max(rect.Y, Math.Min(rect.Bottom, p[i].Y));

					ImageProcessing.IpSettings.BfPoint imagePoint = TransferUnskewedToSkewedPoint(p[i]);
					this.bfPoints[i].X = imagePoint.X;
					this.bfPoints[i].Y = imagePoint.Y;
				}
			}

			SetFirstAndLastPoint();

			this.bfPoints.MarkEdgePoints();
		}
		#endregion

		#region SetFirstAndLastPoint()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="addEdgePointsIfNecessary"></param>
		private void SetFirstAndLastPoint()
		{
			RatioRect rect = this.Clip.RectangleNotSkewed;

			if (this.bfPoints.Count < 2)
			{
				Reset();
			}
			else
			{
				BfPoints p = GetNotSkewedPoints();
				
				ImageProcessing.IpSettings.BfPoint pFirst = p[0];
				
				if (pFirst.IsEdgePoint == false)
				{
					if (pFirst.X > rect.X + minPointsDistance)
					{
						double y = Arithmetic.GetY(p[0].RatioPoint, p[1].RatioPoint, rect.X);
						RatioPoint newFirstPoint = new RatioPoint(rect.X, Math.Max(rect.Y, Math.Min(rect.Bottom, y)));
						newFirstPoint = this.Clip.TransferUnskewedToSkewedPoint(newFirstPoint);
						this.bfPoints.Insert(0, new ImageProcessing.IpSettings.BfPoint(newFirstPoint));
					}
					else
					{
						pFirst.X = rect.X;
						pFirst.IsEdgePoint = true;
					}
				}
				else
				{
					if ((p.Count > 2) && Arithmetic.ArePointsInLine(new RatioPoint[] { p[0].RatioPoint, p[1].RatioPoint, p[2].RatioPoint }))
					{
						pFirst.X = rect.X;
						pFirst.Y = Arithmetic.GetY(p[1].RatioPoint, p[2].RatioPoint, pFirst.X);
						pFirst = TransferUnskewedToSkewedPoint(pFirst);
						this.bfPoints[0].Set(pFirst);
					}
					else
					{
						pFirst.X = rect.X;
						pFirst = TransferUnskewedToSkewedPoint(pFirst);
						this.bfPoints[0].Set(pFirst);
					}
				}

				p = GetNotSkewedPoints();
				ImageProcessing.IpSettings.BfPoint pLast = p[p.Count - 1];
				if (pLast.IsEdgePoint == false)
				{
					if (pLast.X < rect.Right - minPointsDistance)
					{
						int i0 = p.Count - 2, i1 = p.Count - 1;
						double y = p[i1].Y + (p[i1].Y - p[i0].Y) * (rect.Right - p[i1].X) / (double)(p[i1].X - p[i0].X);
						RatioPoint newLastPoint = new RatioPoint(rect.Right, Math.Max(rect.Y, Math.Min(rect.Bottom, y)));
						newLastPoint = this.Clip.TransferUnskewedToSkewedPoint(newLastPoint);
						this.bfPoints.Add(new ImageProcessing.IpSettings.BfPoint(newLastPoint));
					}
					else
					{
						pLast.X = rect.Right;
						pLast.IsEdgePoint = true;
					}
				}
				else
				{
					if ((p.Count > 2) && Arithmetic.ArePointsInLine(new RatioPoint[] { p[p.Count - 3].RatioPoint, p[p.Count - 2].RatioPoint, p[p.Count - 1].RatioPoint }))
					{
						pLast.X = rect.Right;
						pLast.Y = Arithmetic.GetY(p[p.Count - 3].RatioPoint, p[p.Count - 2].RatioPoint, pLast.X);
						pLast = TransferUnskewedToSkewedPoint(pLast);
						this.bfPoints[bfPoints.Count - 1].Set(pLast);
					}
					else
					{
						pLast.X = rect.Right;
						pLast = TransferUnskewedToSkewedPoint(pLast);
						this.bfPoints[bfPoints.Count - 1].Set(pLast);
					}
				}
			}
		}
		#endregion

		#region GetCurve()
		private static double[] GetCurve(RatioPoint[] pointsF, Size imageSize)
		{
			Point[] points = new Point[pointsF.Length];

			for (int i = 0; i < pointsF.Length; i++)
				points[i] = new Point(Convert.ToInt32(pointsF[i].X * imageSize.Width), Convert.ToInt32(pointsF[i].Y * imageSize.Height));

			return GetCurve(points);
		}

		private static double[] GetCurve(Point[] points)
		{
			double[] X = new double[points.Length];
			double[] Y = new double[points.Length];
			double[] h = new double[points.Length - 1];
			double[] A = new double[points.Length - 3];
			double[] B = new double[points.Length - 2];
			double[] C = new double[points.Length - 3];
			double[] D = new double[points.Length - 2];

			for (int i = 0; i < points.Length; i++)
			{
				X[i] = points[i].X;
				Y[i] = points[i].Y;
			}

			for (int i = 0; i < points.Length - 1; i++)
				h[i] = points[i + 1].X - points[i].X;

			for (int i = 1; i < h.Length - 1; i++)
			{
				A[i - 1] = h[i];
				C[i - 1] = h[i];
			}

			for (int i = 1; i < h.Length; i++)
			{
				D[i - 1] = 2 * (h[i - 1] + h[i]);
				B[i - 1] = 6 * (((Y[i + 1] - Y[i]) / h[i]) - ((Y[i] - Y[i - 1]) / h[i - 1]));
			}

			ArrayList listZ = new ArrayList();
			listZ.AddRange(SolveTriDiagonalMatrix(A, B, C, D));
			listZ.Insert(0, 0.0);
			listZ.Add(0.0);
			double[] Z = (double[])listZ.ToArray(typeof(double));


			double[] array = new double[points[points.Length - 1].X - points[0].X + 1];
			array[array.Length - 1] = points[points.Length - 1].Y;

			for (int k = 0; k < points.Length - 1; k++)
			{
				for (int x = (int)X[k]; x < X[k + 1]; x++)
					array[x - (int)X[0]] = (Z[k + 1] * (x - X[k]) * (x - X[k]) * (x - X[k]) / (6 * h[k])) + 
						(Z[k] * (X[k + 1] - x) * (X[k + 1] - x) * (X[k + 1] - x) / (6 * h[k])) + 
						(((Y[k + 1] / h[k]) - (h[k] * Z[k + 1] / 6)) * (x - X[k])) + 
						(((Y[k] / h[k]) - (h[k] * Z[k] / 6)) * (X[k + 1] - x));
			}

			return array;
		}
		#endregion

		#region SolveTriDiagonalMatrix
		private static double[] SolveTriDiagonalMatrix(double[] A, double[] B, double[] C, double[] D)
		{
			int n = B.Length;
			double[] X;

			for (int k = 1; k < n; k++)
			{
				D[k] = D[k] - A[k - 1] * C[k - 1] / D[k - 1];
				B[k] = B[k] - A[k - 1] * B[k - 1] / D[k - 1];
			}

			X = new double[n];
			X[n - 1] = B[n - 1] / D[n - 1];

			for (int k = n - 2; k >= 0; k--)
				X[k] = (B[k] - C[k] * X[k + 1]) / D[k];

			return X;
		}
		#endregion

		#region GetNotAngledLine()
		private double[] GetNotAngledLine(Size imageSize, int arrayLength)
		{
			BfPoints p = GetNotSkewedPoints();

			if (p.Count < 2)
				throw new Exception(BIPStrings.CurveHasLessThan2Points_STR);

			RatioPoint point1 = p[0].RatioPoint;
			RatioPoint point2 = p[p.Count - 1].RatioPoint;
			int p1X = Convert.ToInt32(point1.X * imageSize.Width);
			int p1Y = Convert.ToInt32(point1.Y * imageSize.Height);
			int p2X = Convert.ToInt32(point2.X * imageSize.Width);
			int p2Y = Convert.ToInt32(point2.Y * imageSize.Height);

			double ratio = (p2X - p1X + 1 > 0) ? (p2Y - p1Y) / (double)(p2X - p1X + 1) : 0;

			double[] array = new double[arrayLength];

			for (int x = 0; x < arrayLength; x++)
				array[x] = p1Y + ratio * x;

			return array;
		}
		#endregion

		#region GetAngle()
		/*public double GetAngle(RatioPoint pC, RatioPoint p1, RatioPoint p2)
		{
			//cos alpha = (b*b + c*c - a*a) / 2bc
			double b = Math.Sqrt((pC.X - p1.X) * (pC.X - p1.X) + (pC.Y - p1.Y) * (pC.Y - p1.Y));
			double c = Math.Sqrt((pC.X - p2.X) * (pC.X - p2.X) + (pC.Y - p2.Y) * (pC.Y - p2.Y));
			double a = Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));

			double alpha = Math.Acos((b * b + c * c - a * a) / (2 * b * c));

			if (alpha < 0)
				alpha += Math.PI * 2;

			return Math.Abs(alpha - Math.PI);
		}*/
		#endregion

		#region GetAngledPoints()
		/*private BfPoints GetAngledPoints(BfPoints sourcePoints)
		{
			if(this.Clip.IsSkewed)
			{
				BfPoints destPoints = new BfPoints();

				for (int i = 0; i < sourcePoints.Count; i++)
					destPoints.Add(Rotation.RotatePoint(sourcePoints[i].ToPoint(), this.Clip.Center, this.Clip.Skew));

				return destPoints;
			}
			else
				return (BfPoints) sourcePoints.Clone();
		}*/
		#endregion

		#region ValidateEntryPoints()
		private List<RatioPoint> ValidateEntryPoints(RatioPoint[] curvePoints)
		{
			List<RatioPoint> points = new List<RatioPoint>(curvePoints);
			var sortedPoints = from p in curvePoints orderby p.X ascending select p;
			List<RatioPoint> pSorted = new List<RatioPoint>(sortedPoints);

			for (int i = pSorted.Count() - 2; i >= 1; i--)
				if ((pSorted[i].X - pSorted[i - 1].X) < this.minPointsDistance || (pSorted[i + 1].X - pSorted[i].X) < this.minPointsDistance)
					pSorted.RemoveAt(i);

			return pSorted;
		}
		#endregion

		#region BfPoints_Changed()
		void BfPoints_Changed()
		{
			if (this.RaiseChangedEvent)
			{
				if (Changed != null)
					Changed();
			}
			else
			{
				this.changed = true;
			}
		}
		#endregion

		#endregion

	}
	
}
