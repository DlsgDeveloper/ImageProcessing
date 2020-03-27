using System;
using System.Collections;
using System.Drawing;
using ImageProcessing.Languages;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Curve.
	/// </summary>
	public class Curve
	{
		BfPoints	bfPoints = new BfPoints();
		bool		isTopCurve;
		Clip		clip;
		int			minPointsDistance = 20;
		int			defaultPointsCount = 8;

		public event BfPoint.PointHnd PointAdding;
		public event BfPoint.PointHnd PointAdded;
		public event BfPoint.PointHnd PointRemoving;
		public event BfPoint.PointHnd PointRemoved;
		public event BfPoint.VoidHnd Clearing;
		public event BfPoint.VoidHnd Cleared;

		public event ItImage.VoidHnd Changed;


		#region constructor
		private Curve()
		{
			this.bfPoints.PointAdding += delegate(BfPoint bfPoint) { if (PointAdding != null) PointAdding(bfPoint); };
			this.bfPoints.PointAdded += delegate(BfPoint bfPoint) { if(PointAdded != null) PointAdded(bfPoint); };
			this.bfPoints.PointRemoving += delegate(BfPoint bfPoint) { if(PointRemoving != null) PointRemoving(bfPoint); };
			this.bfPoints.PointRemoved += delegate(BfPoint bfPoint) { if(PointRemoved != null) PointRemoved(bfPoint); };
			this.bfPoints.Clearing += delegate() { if(Clearing != null) Clearing(); };
			this.bfPoints.Cleared += delegate() { if(Cleared != null) Cleared(); };
			this.bfPoints.Changed += delegate() { if (Changed != null) Changed(); };
		}

		public Curve(Clip clip, Point[] curvePoints, bool isTopCurve)
			:this()
		{
			this.isTopCurve = isTopCurve;
			this.clip = clip;

			SetPoints(curvePoints);
		}

		public Curve(Clip clip, bool isTopCurve)
			: this()
		{
			this.clip = clip;
			this.isTopCurve = isTopCurve;

			Reset();
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		public bool			IsTopCurve { get { return isTopCurve; } }
		public BfPoints		BfPoints { get { return bfPoints; } }
		public Point[]		Points { get { return bfPoints.GetPoints(); } }

		#region PointsNotSkewed
		public Point[] PointsNotSkewed
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
				if (bfPoints.Count > 2)
				{					
					Point p0 = bfPoints[0].Point;
					Point pN = bfPoints[bfPoints.Count - 1].Point;

					if (pN.X - p0.X == 0)
						return false;
					else
					{
						for (int i = 1; i < bfPoints.Count - 1; i++)
						{
							int streightY = p0.Y + (pN.Y - p0.Y) * (bfPoints[i].X - p0.X) / (pN.X - p0.X);

							if (bfPoints[i].Y < streightY - 3 || bfPoints[i].Y > streightY + 3)
								return true;
						}
					}
				}

				return false;
			}
		}
		#endregion

		#region Rectangle
		public Rectangle Rectangle
		{
			get
			{
				Rectangle rectangle = Rectangle.Empty;

				if (bfPoints.Count > 0)
				{
					rectangle = new Rectangle(bfPoints[0].X, bfPoints[0].Y, 0, 0);
					
					foreach (BfPoint bfPoint in bfPoints)
						rectangle = Rectangle.FromLTRB(Math.Min(rectangle.X, bfPoint.X), Math.Min(rectangle.Y, bfPoint.Y),
							Math.Max(rectangle.Right, bfPoint.X), Math.Max(rectangle.Bottom, bfPoint.Y));
				}

				return rectangle;
			}
		}
		#endregion

		#endregion

		//PRIVATE PROPERTIES
		#region private properties

		#region FirstPoint
		private BfPoint FirstPoint
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
		private BfPoint LastPoint
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

		#region Lock()
		public void Lock()
		{
		}
		#endregion

		#region Unlock()
		public void Unlock()
		{
		}
		#endregion

		#region GetArray()
		public static double[] GetArray(Point[] points)
		{
			PointF[] pointsF = new PointF[points.Length];

			for (int i = 0; i < pointsF.Length; i++)
				pointsF[i] = new PointF(points[i].X, points[i].Y);

			return GetArray(pointsF);
		}

		public static double[] GetArray(PointF[] points)
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
				double[] array = new double[(int)(points[1].X - points[0].X)];

				for (int i = 0; i < array.Length; i++)
					array[i] = (points[1].Y - points[0].Y) * (i) / (points[1].X - points[0].X);

				return array;
			}
			else
			{
				return GetCurve(points);
			}
		}

		public double[] GetArray()
		{
			if (this.IsCurved)
			{
				return GetCurve(this.bfPoints.GetPointsF());
			}
			else
			{
				return GetLine();
			}
		}
		#endregion

		#region GetNotAngledArray()	
		public double[] GetNotAngledArray()
		{
			if (this.IsCurved)
			{
				BfPoints p = GetNotSkewedPoints();

				if (p.Count > 0)
				{
					p[0].X = clip.RectangleNotSkewed.X;
					p[p.Count - 1].X = clip.RectangleNotSkewed.Right;
				}

				return GetCurve(p.GetPointsF());
			}
			else
			{
				return GetLine();
			}
		}
		#endregion

		#region GetNotSkewedArray()
		public double[] GetNotSkewedArray()
		{
			if (this.IsCurved)
			{
				BfPoints p = GetNotSkewedPoints();

				if (p.Count > 0)
				{
					p[0].X = clip.RectangleNotSkewed.X;
					p[p.Count - 1].X = clip.RectangleNotSkewed.Right;
				}

				return GetCurve(p.GetPointsF());
			}
			else
			{
				return GetLine(GetNotSkewedPoints());
			}
		}
		#endregion

		#region Clone()
		public Curve Clone(Clip clip)
		{
			Point[]	newPoints = (Point[]) this.Points.Clone();
							
			return new Curve(clip, newPoints, isTopCurve);
		}
		#endregion

		#region ClipChanged()
		public void ClipChanged()
		{
			if (this.IsCurved)
			{
				ValidatePoints();
			}
			else
				Reset();
		}
		#endregion

		#region AddPoint()
		public void AddPoint(Point imagePoint)
		{
			Rectangle	rect = clip.RectangleNotSkewed;
			Point		pNotAngled = this.clip.TransferSkewedToUnskewedPoint(imagePoint);
			BfPoints	p = GetNotSkewedPoints();

			if (rect.Contains(pNotAngled))
			{
				for(int i = 1; i < p.Count; i++)
					if (pNotAngled.X < p[i].X)
					{
						if ((pNotAngled.X - p[i - 1].X > minPointsDistance) && ((i == p.Count - 1) || (p[i].X - pNotAngled.X > minPointsDistance)))
							this.bfPoints.Add(new BfPoint(imagePoint));

						break;
					}
			}

			ValidatePoints();
		}
		#endregion

		#region RemovePoint()
		public bool RemovePoint(BfPoint bfPoint)
		{
			return RemovePoint(this.bfPoints.IndexOf(bfPoint));
		}

		public bool RemovePoint(int index)
		{
			if (index > 0 && index < bfPoints.Count - 1)
			{
				this.bfPoints.RemoveAt(index);
				return true;
			}
			else
				return false;
		}
		#endregion

		#region Shift()
		/// <summary>
		/// Shifts entire curve either up or down.
		/// </summary>
		/// <param name="dy">If bigger than 0, curve is shifted down, otherwise up.</param>
		public void Shift(int dy)
		{
			Rectangle rect = clip.RectangleNotSkewed;
						
			foreach(BfPoint bfPoint in this.bfPoints)
			{
				BfPoint aPoint = TransferSkewedToUnskewedPoint(bfPoint);

				aPoint.Y = Math.Max(rect.Y, Math.Min(rect.Bottom, aPoint.Y + dy));
				aPoint = TransferUnskewedToSkewedPoint(aPoint);

				bfPoint.Set(aPoint.X, aPoint.Y);
			}
		}
		#endregion

		#region SetPoints()
		public void SetPoints(Point[] curvePoints)
		{
			this.bfPoints.Clear();

			foreach (Point point in curvePoints)
				this.bfPoints.Add(new BfPoint(point));

			ValidatePoints();
		}
		#endregion

		#region SetPoint()
		public void SetPoint(BfPoint bfPoint, Point imagePoint)
		{
			SetPoint(this.bfPoints.IndexOf(bfPoint), imagePoint);
		}

		public void SetPoint(int index, Point imagePoint)
		{
			if (index >= 0 && index < this.bfPoints.Count)
			{
				Rectangle rect = clip.RectangleNotSkewed;

				if (index == 0)
				{
					BfPoint firstPoint = FirstPoint;
					imagePoint = this.clip.TransferSkewedToUnskewedPoint(imagePoint);

					if (firstPoint != null)
					{
						Point aPoint = this.clip.TransferUnskewedToSkewedPoint(new Point(rect.X, Math.Max(rect.Y, Math.Min(rect.Bottom, imagePoint.Y))));
						firstPoint.X = aPoint.X;
						firstPoint.Y = aPoint.Y;
					}
				}
				else if (index == this.bfPoints.Count - 1)
				{
					BfPoint lastPoint = LastPoint;

					if (lastPoint != null)
					{
						imagePoint = this.clip.TransferSkewedToUnskewedPoint(imagePoint);
						Point aPoint = this.clip.TransferUnskewedToSkewedPoint(new Point(rect.Right, Math.Max(rect.Y, Math.Min(rect.Bottom, imagePoint.Y))));
						lastPoint.X = aPoint.X;
						lastPoint.Y = aPoint.Y;
					}
				}
				else if (index > 0 && index < this.bfPoints.Count - 1)
				{
					BfPoint bfPoint = bfPoints[index];
					BfPoint previous = bfPoints[index - 1];
					BfPoint next = bfPoints[index + 1];

					if (previous != null)
						imagePoint.X = (imagePoint.X - previous.X > minPointsDistance) ? imagePoint.X : previous.X + minPointsDistance;
					if (next != null)
						imagePoint.X = (next.X - imagePoint.X > minPointsDistance) ? imagePoint.X : next.X - minPointsDistance;

					imagePoint = this.clip.TransferSkewedToUnskewedPoint(imagePoint);
					imagePoint.Y = Math.Max(rect.Y, Math.Min(rect.Bottom, imagePoint.Y));
					imagePoint = this.clip.TransferUnskewedToSkewedPoint(imagePoint);

					bfPoint.X = imagePoint.X;
					bfPoint.Y = imagePoint.Y;
				}
			}
		}
		#endregion

		#region Reset()
		public void Reset()
		{
			Rectangle rect = clip.RectangleNotSkewed;
			
			this.bfPoints.Clear();

			if (isTopCurve)
			{
				for (int i = 0; i < defaultPointsCount; i++)
				{
					BfPoint bfPoint = new BfPoint(Convert.ToInt32(rect.X + i * rect.Width  / (defaultPointsCount - 1.0)), rect.Y);
					bfPoints.Add(TransferUnskewedToSkewedPoint(bfPoint));
				}
			}
			else
			{
				for (int i = 0; i < defaultPointsCount; i++)
				{
					BfPoint bfPoint = new BfPoint(Convert.ToInt32(rect.X + i * rect.Width / (defaultPointsCount - 1.0)), rect.Bottom);
					bfPoints.Add(TransferUnskewedToSkewedPoint(bfPoint));
				}
			}

			bfPoints.MarkEdgePoints();
			//ValidatePoints();
		}
		#endregion

		#region TransferSkewedToUnskewedPoint()
		public BfPoint TransferSkewedToUnskewedPoint(BfPoint bfPoint)
		{
			BfPoint bfPointUnrotated = new BfPoint(this.clip.TransferSkewedToUnskewedPoint(bfPoint.ToPoint()));

			bfPointUnrotated.IsEdgePoint = bfPoint.IsEdgePoint;
			return bfPointUnrotated;
		}
		#endregion

		#region TransferUnskewedToSkewedPoint()
		public BfPoint TransferUnskewedToSkewedPoint(BfPoint point)
		{
			BfPoint bfPointSkewed = new BfPoint(this.clip.TransferUnskewedToSkewedPoint(point.ToPoint()));

			bfPointSkewed.IsEdgePoint = point.IsEdgePoint;
			return bfPointSkewed;
		}
		#endregion

		#region ResizeSettings()
		public void ResizeSettings(double zoom)
		{
			foreach (BfPoint bfPoint in this.bfPoints)
			{
				bfPoint.X = Convert.ToInt32(bfPoint.X * zoom);
				bfPoint.Y = Convert.ToInt32(bfPoint.Y * zoom);
			}

			SetFirstAndLastPoint();
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region GetNotSkewedPoints()
		private BfPoints GetNotSkewedPoints()
		{
			if (this.clip.IsSkewed)
			{
				BfPoints destPoints = new BfPoints();

				for (int i = 0; i < this.bfPoints.Count; i++)
				{
					BfPoint bfPoint = TransferSkewedToUnskewedPoint(this.bfPoints[i]);

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
			Rectangle	rect = clip.RectangleNotSkewed;

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

					BfPoint imagePoint = TransferUnskewedToSkewedPoint(p[i]);
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
			Rectangle rect = clip.RectangleNotSkewed;

			if (this.bfPoints.Count < 2)
			{
				Reset();
			}
			else
			{
				BfPoints p = GetNotSkewedPoints();
				
				BfPoint pFirst = p[0];
				
				if (pFirst.IsEdgePoint == false)
				{
					if (pFirst.X > rect.X + minPointsDistance)
					{
						double y = Convert.ToInt32(Arithmetic.GetY(p[0].Point, p[1].Point, rect.X));
						Point newFirstPoint = new Point(rect.X, (int)Math.Max(rect.Y, Math.Min(rect.Bottom, (int)y)));
						newFirstPoint = this.clip.TransferUnskewedToSkewedPoint(newFirstPoint);
						this.bfPoints.Insert(0, new BfPoint(newFirstPoint));
					}
				}
				else
				{
					if ((p.Count > 2) && Arithmetic.ArePointsInLine(new PointF[] { p[0].Point, p[1].Point, p[2].Point }))
					{
						pFirst.X = rect.X;
						pFirst.Y = Convert.ToInt32(Arithmetic.GetY(p[1].Point, p[2].Point, pFirst.X));
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
				BfPoint pLast = p[p.Count - 1];
				if (pLast.IsEdgePoint == false)
				{
					if (pLast.X < rect.Right - minPointsDistance)
					{
						int i0 = p.Count - 2, i1 = p.Count - 1;
						double y = p[i1].Y + (p[i1].Y - p[i0].Y) * (rect.Right - p[i1].X) / (double)(p[i1].X - p[i0].X);
						Point newLastPoint = new Point(rect.Right, (int)Math.Max(rect.Y, Math.Min(rect.Bottom, (int)y)));
						newLastPoint = this.clip.TransferUnskewedToSkewedPoint(newLastPoint);
						this.bfPoints.Add(new BfPoint(newLastPoint));
					}
				}
				else
				{
					if ((p.Count > 2) && Arithmetic.ArePointsInLine(new PointF[] { p[p.Count - 3].Point, p[p.Count - 2].Point, p[p.Count - 1].Point }))
					{
						pLast.X = rect.Right;
						pLast.Y = Convert.ToInt32(Arithmetic.GetY(p[p.Count - 3].Point, p[p.Count - 2].Point, pLast.X));
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

		#region GetAngledPoints()
		/*private BfPoints GetAngledPoints(BfPoints sourcePoints)
		{
			if(this.clip.IsSkewed)
			{
				BfPoints destPoints = new BfPoints();

				for (int i = 0; i < sourcePoints.Count; i++)
					destPoints.Add(Rotation.RotatePoint(sourcePoints[i].ToPoint(), this.clip.Center, this.clip.Skew));

				return destPoints;
			}
			else
				return (BfPoints) sourcePoints.Clone();
		}*/
		#endregion

		#region GetCurve()
		private static double[] GetCurve(PointF[] points)
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


			double[] array = new double[(int)(points[points.Length - 1].X - points[0].X + 1)];
			array[array.Length - 1] = (int)points[points.Length - 1].Y;

			for (int k = 0; k < points.Length - 1; k++)
			{
				for (int x = (int)X[k]; x < X[k + 1]; x++)
					array[x - (int)X[0]] = (Z[k + 1] * (x - X[k]) * (x - X[k]) * (x - X[k]) / (6 * h[k])) + (Z[k] * (X[k + 1] - x) * (X[k + 1] - x) * (X[k + 1] - x) / (6 * h[k])) + (((Y[k + 1] / h[k]) - (h[k] * Z[k + 1] / 6)) * (x - X[k])) + (((Y[k] / h[k]) - (h[k] * Z[k] / 6)) * (X[k + 1] - x));
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

		#region GetLine()
		private double[] GetLine()
		{
			if (this.bfPoints.Count < 2)
				throw new Exception(BIPStrings.CurveHasLessThan2Points_STR);

			PointF	p1 = this.bfPoints[0].ToPointF();
			PointF	p2 = this.bfPoints[this.bfPoints.Count - 1].ToPointF();
			int		length = (int)(clip.RectangleNotSkewed.Width + 1);
			double	ratio = (length > 0) ? (p2.Y - p1.Y) / (double)length : 0;

			double[] array = new double[length];

			for (int x = 0; x < length; x++)
				array[x] = Convert.ToInt32(p1.Y + ratio * x);

			return array;
		}

		private static double[] GetLine(BfPoints bfPoints)
		{
			if (bfPoints.Count < 2)
				throw new Exception(BIPStrings.CurveHasLessThan2Points_STR);

			PointF p1 = bfPoints[0].ToPointF();
			PointF p2 = bfPoints[bfPoints.Count - 1].ToPointF();
			int length = (int)(p2.X - p1.X + 1);
			double ratio = (length > 0) ? (p2.Y - p1.Y) / (double)length : 0;

			double[] array = new double[length];

			for (int x = 0; x < length; x++)
				array[x] = p1.Y + ratio * x;

			return array;
		}
		#endregion

		#region GetAngle()
		public double GetAngle(Point pC, Point p1, Point p2)
		{
			//cos alpha = (b*b + c*c - a*a) / 2bc
			double b = Math.Sqrt((pC.X - p1.X) * (pC.X - p1.X) + (pC.Y - p1.Y) * (pC.Y - p1.Y));
			double c = Math.Sqrt((pC.X - p2.X) * (pC.X - p2.X) + (pC.Y - p2.Y) * (pC.Y - p2.Y));
			double a = Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));

			double alpha = Math.Acos((b * b + c * c - a * a) / (2 * b * c));

			if (alpha < 0)
				alpha += Math.PI * 2;

			return Math.Abs(alpha - Math.PI);
		}
		#endregion

		#endregion
	}
	
}
