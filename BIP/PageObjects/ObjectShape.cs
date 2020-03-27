using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.PageObjects
{
	public class ObjectShape
	{
		Point			location;
		int				width;
		int				height;
		ushort[]		surfaceL;	//stored in local coordinates. To get global coordinates, add location
		ushort[]		surfaceT;	//stored in local coordinates. To get global coordinates, add location
		ushort[]		surfaceR;	//stored in local coordinates. To get global coordinates, add location
		ushort[]		surfaceB;	//stored in local coordinates. To get global coordinates, add location
		ObjectByCorners _objectByCorners;	//stored in absolute coordinates


		#region constructor
		public ObjectShape(Point location, ObjectMap objectMap)
		{
#if DEBUG
			//DateTime start = DateTime.Now;
#endif
			
			this.location = location;
			width = objectMap.Width;
			height = objectMap.Height;

			surfaceL = new ushort[height];
			surfaceT = new ushort[width];
			surfaceR = new ushort[height];
			surfaceB = new ushort[width];

			for (int x = 0; x < width; x++)
			{
				surfaceT[x] = ushort.MaxValue;
				surfaceB[x] = ushort.MinValue;
			}
			
			for (int y = 0; y < height; y++)
			{
				surfaceL[y] = ushort.MaxValue;
				surfaceR[y] = ushort.MinValue;
			}

			foreach (SymbolShape shape in objectMap.SymbolShapes)
			{
				foreach (SymbolShapePoint pp in shape.EdgePoints)
				{
					SymbolShapePoint p = new SymbolShapePoint(pp.X - this.location.X, pp.Y - this.location.Y);

					if (surfaceL[p.Y] > p.X)
						surfaceL[p.Y] = (ushort)p.X;
					if (surfaceR[p.Y] < p.X)
						surfaceR[p.Y] = (ushort)p.X;
					if (surfaceT[p.X] > p.Y)
						surfaceT[p.X] = (ushort)p.Y;
					if (surfaceB[p.X] < p.Y)
						surfaceB[p.X] = (ushort)p.Y;
				}
			}

			CompactSurface();

			_objectByCorners = GetObjectByCornersNew(objectMap);	
		}
		#endregion
	

		#region class ObjectByCorners
		public class ObjectByCorners
		{
			// Fields
			public Point Ll;
			public Point Lr;
			public Point Ul;
			public Point Ur;

			// Methods
			public ObjectByCorners(Point ul, Point ur, Point ll, Point lr)
			{
				this.Ul = ul;
				this.Ur = ur;
				this.Ll = ll;
				this.Lr = lr;
			}

			public int Left { get { return Math.Min(Ul.X, Ll.X); } }
			public int Top { get { return Math.Min(Ul.Y, Ur.Y); } }
			public int Right { get { return Math.Max(Ur.X, Lr.X); } }
			public int Bottom { get { return Math.Max(Ll.Y, Lr.Y); } }
			public Rectangle TangentialRectangle { get { return Rectangle.FromLTRB(Left, Top, Right, Bottom); } }			
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties

		public Rectangle Rectangle { get { return new Rectangle(location.X, location.Y, width, height); } }
		public ushort[] SurfaceL { get { return surfaceL; } }
		public ushort[] SurfaceT { get { return surfaceT; } }
		public ushort[] SurfaceR { get { return surfaceR; } }
		public ushort[] SurfaceB { get { return surfaceB; } }

		public ObjectByCorners Corners { get { return _objectByCorners; } }

		#region IsBfValid
		/// <summary>
		/// Validates smoothened middle edge curve  
		/// </summary>
		public bool IsBfValid
		{
			get
			{
				if (this.width > 100)
				{
					int xMin = Math.Max(0, Math.Min(width, _objectByCorners.Left - location.X));
					int xMax = Math.Max(0, Math.Min(width, _objectByCorners.Right - location.X));
					int arrayWidth = xMax - xMin;
					ushort[] surface = new ushort[arrayWidth];
					ushort[] curveSmoothened = new ushort[arrayWidth];

					for (int x = 0; x < arrayWidth; x++)
						surface[x] = (ushort)((surfaceT[x] + surfaceB[x]) / 2);

					for (int x = 2; x < arrayWidth - 2; x++)
						curveSmoothened[x - xMin] = (ushort)((surface[x - 2] + surface[x - 1] + surface[x] + surface[x + 1] + surface[x + 2]) / 5);

					curveSmoothened[0] = (ushort)((surface[0] + surface[0] + surface[0] + surface[1] + surface[2]) / 5);
					curveSmoothened[1] = (ushort)((surface[0] + surface[0] + surface[1] + surface[2] + surface[3]) / 5);
					curveSmoothened[arrayWidth - 2] = (ushort)((surface[arrayWidth - 4] + surface[arrayWidth - 3] + surface[arrayWidth - 2] + surface[arrayWidth - 1] + surface[arrayWidth - 1]) / 5);
					curveSmoothened[arrayWidth - 1] = (ushort)((surface[arrayWidth - 3] + surface[arrayWidth - 2] + surface[arrayWidth - 1] + surface[arrayWidth - 1] + surface[arrayWidth - 1]) / 5);

					return IsValidBfCurve(curveSmoothened);
				}
				else
					return false;
			}
		}
		#endregion

		#region IsTopBfValid
		/// <summary>
		/// Validates smoothened top edge curve  
		/// </summary>
		public bool IsTopBfValid		
		{ 
			get 
			{
				if (this.width > 100)
				{
					int xMin = Math.Max(0, Math.Min(width, _objectByCorners.Ul.X - location.X));
					int xMax = Math.Max(0, Math.Min(width, _objectByCorners.Ur.X - location.X));

					if (xMax - xMin > 100)
					{
						int arrayWidth = xMax - xMin;
						ushort[] curveSmoothened = new ushort[arrayWidth];

						for (int x = xMin + 2; x < xMax - 2; x++)
							curveSmoothened[x - xMin] = (ushort)((surfaceT[x - 2] + surfaceT[x - 1] + surfaceT[x] + surfaceT[x + 1] + surfaceT[x + 2]) / 5);

						curveSmoothened[0] = (ushort)((surfaceT[xMin] + surfaceT[xMin] + surfaceT[xMin] + surfaceT[xMin + 1] + surfaceT[xMin + 2]) / 5);
						curveSmoothened[1] = (ushort)((surfaceT[xMin] + surfaceT[xMin] + surfaceT[xMin + 1] + surfaceT[xMin + 2] + surfaceT[xMin + 3]) / 5);
						curveSmoothened[arrayWidth - 2] = (ushort)((surfaceT[arrayWidth - 4] + surfaceT[arrayWidth - 3] + surfaceT[arrayWidth - 2] + surfaceT[arrayWidth - 1] + surfaceT[arrayWidth - 1]) / 5);
						curveSmoothened[arrayWidth - 1] = (ushort)((surfaceT[arrayWidth - 3] + surfaceT[arrayWidth - 2] + surfaceT[arrayWidth - 1] + surfaceT[arrayWidth - 1] + surfaceT[arrayWidth - 1]) / 5);

						return IsValidBfCurve(curveSmoothened);
					}
				}

				return false;
			}
		}
		#endregion

		#region IsBottomBfValid
		/// <summary>
		/// Validates smoothened bottom edge curve  
		/// </summary>
		public bool IsBottomBfValid
		{
			get
			{
				if (this.width > 100)
				{
					int xMin = Math.Max(0, Math.Min(width, _objectByCorners.Ll.X - location.X));
					int xMax = Math.Max(0, Math.Min(width, _objectByCorners.Lr.X - location.X));

					if (xMax - xMin > 100)
					{
						int arrayWidth = xMax - xMin;
						ushort[] curveSmoothened = new ushort[arrayWidth];

						for (int x = xMin + 2; x < xMax - 2; x++)
							curveSmoothened[x - xMin] = (ushort)((surfaceB[x - 2] + surfaceB[x - 1] + surfaceB[x] + surfaceB[x + 1] + surfaceB[x + 2]) / 5);

						curveSmoothened[0] = (ushort)((surfaceB[xMin] + surfaceB[xMin] + surfaceB[xMin] + surfaceB[xMin + 1] + surfaceB[xMin + 2]) / 5);
						curveSmoothened[1] = (ushort)((surfaceB[xMin] + surfaceB[xMin] + surfaceB[xMin + 1] + surfaceB[xMin + 2] + surfaceB[xMin + 3]) / 5);
						curveSmoothened[arrayWidth - 2] = (ushort)((surfaceB[arrayWidth - 4] + surfaceB[arrayWidth - 3] + surfaceB[arrayWidth - 2] + surfaceB[arrayWidth - 1] + surfaceB[arrayWidth - 1]) / 5);
						curveSmoothened[arrayWidth - 1] = (ushort)((surfaceB[arrayWidth - 3] + surfaceB[arrayWidth - 2] + surfaceB[arrayWidth - 1] + surfaceB[arrayWidth - 1] + surfaceB[arrayWidth - 1]) / 5);

						return IsValidBfCurve(curveSmoothened);
					}
				}

				return false;
			}
		}
		#endregion

		#region MaxPixelWidth
			public int MaxPixelWidth
		{
			get
			{
				int max = 0;
				
				for (int y = 0; y < height; y++)
					if (max < surfaceR[y] - surfaceL[y])
						max = surfaceR[y] - surfaceL[y];

				return max;
			}
		}
		#endregion

		#region MaxPixelHeight
		public int MaxPixelHeight
		{
			get
			{
				int max = 0;

				for (int x = 0; x < width; x++)
					if (max < surfaceB[x] - surfaceT[x])
						max = surfaceB[x] - surfaceT[x];

				return max;
			}
		}
		#endregion

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region GetCrop()
		public Crop GetCrop()
		{
			int		x, y;
			int		minIndex, maxIndex;

			//find left point
			minIndex = 0;
			for (y = 0; y < height; y++)
				if (surfaceL[minIndex] > surfaceL[y])
					minIndex = y;
			
			maxIndex = 0;
			for (y = height - 1; y >= 0; y--)
				if (surfaceL[maxIndex] > surfaceL[y])
					maxIndex = y;

			Point pL = new Point(location.X + surfaceL[minIndex], location.Y + (minIndex + maxIndex) / 2);

			//find right point
			minIndex = 0;
			for (y = 0; y < height; y++)
				if (surfaceR[minIndex] < surfaceR[y])
					minIndex = y;

			maxIndex = 0;
			for (y = height - 1; y >= 0; y--)
				if (surfaceR[maxIndex] < surfaceR[y])
					maxIndex = y;

			Point pR = new Point(location.X + surfaceR[minIndex], location.Y + (minIndex + maxIndex) / 2);

			//find top point
			minIndex = 0;
			for (x = 0; x < width; x++)
				if (surfaceT[minIndex] > surfaceT[x])
					minIndex = x;

			maxIndex = 0;
			for (x = width - 1; x >= 0; x--)
				if (surfaceT[maxIndex] > surfaceT[x])
					maxIndex = x;

			Point pT = new Point(location.X + (minIndex + maxIndex) / 2, location.Y + surfaceT[minIndex]);

			//find right point
			minIndex = 0;
			for (x = 0; x < width; x++)
				if (surfaceB[minIndex] < surfaceB[x])
					minIndex = x;

			maxIndex = 0;
			for (x = width - 1; x >= 0; x--)
				if (surfaceB[maxIndex] < surfaceB[x])
					maxIndex = x;

			Point pB = new Point(location.X + (minIndex + maxIndex) / 2, location.Y + surfaceB[minIndex]);

			return new Crop(pL, pT, pR, pB);
		}
		#endregion

		#region GetSkew()
		public double GetSkew(Size pageSize, out double weight)
		{
			double angleT = Arithmetic.GetAngle(_objectByCorners.Ul, _objectByCorners.Ur);
			double angleB = Arithmetic.GetAngle(_objectByCorners.Ll, _objectByCorners.Lr);

			if ((angleT - (Math.PI / 18) < angleB) && (angleT + (Math.PI / 18) > angleB))
			{
				if ((pageSize.Width * pageSize.Height) > 0)
					weight = Math.Min((double)1.0, (double)(((double)(this.width * this.height)) / ((double)(pageSize.Width * pageSize.Height))));
				else
					weight = 0.0;

				return (angleT + angleB) / 2.0;
			}
			else
			{
				weight = 0;
				return 0;
			}
		}
		#endregion		

		#region GetCurve()
		public Point[] GetCurve()
		{
			if (this.width > this.height)
			{
				Point[] points = new Point[this.width];

				for (int x = 0; x < this.width; x++)
					points[x] = new Point(location.X + x, location.Y + (surfaceT[x] + surfaceB[x]) / 2);

				return points;
			}
			else
			{
				Point[] points = new Point[this.height];

				for (int y = 0; y < this.height; y++)
					points[y] = new Point(location.X + (surfaceL[y] + surfaceR[y]) / 2, location.Y + y);

				return points;
			}
		}
		#endregion

		#region GetBfPoints()
		public Point[] GetBfPoints()
		{
			int		xMin = Math.Max(0, Math.Min(width, _objectByCorners.Left - location.X));
			int		xMax = Math.Max(0, Math.Min(width, _objectByCorners.Right - location.X));
			int		pointsCount = 10;
			float	jump = (xMax - xMin) / (float)pointsCount;

			if (jump > 10)
			{
				Point[] bfPoints = new Point[pointsCount];
				bfPoints[0] = new Point(location.X + xMin, location.Y + (this.surfaceT[xMin] + this.surfaceB[xMin]) / 2);
				bfPoints[pointsCount - 1] = new Point(location.X + xMax, location.Y + (surfaceT[xMax] + surfaceB[xMax]) / 2);

				for (int i = 1; i < pointsCount - 1; i++)
				{
					int index = (int)(i * jump);

					bfPoints[i] = new Point(location.X + index, location.Y + (this.surfaceT[index] + this.surfaceB[index]) / 2);
				}

				return bfPoints;
			}
			else
				return null;
		}
		#endregion

		#region GetTopBfPoints()
		public Point[] GetTopBfPoints()
		{
			int		xMin = _objectByCorners.Ul.X - location.X;
			int		xMax = _objectByCorners.Ur.X - location.X;
			int		pointsCount = 10;
			float	jump = (xMax - xMin) / (float)pointsCount;

			if (jump > 10)
			{
				xMin = Math.Max(0, Math.Min(width, xMin));
				xMax = Math.Max(0, Math.Min(width, xMax));

				List<Point> bfPoints = new List<Point>();
				bfPoints.Add(new Point(location.X + xMin, location.Y + this.surfaceT[xMin]));
				bfPoints.Add(new Point(location.X + xMax, location.Y + surfaceT[xMax]));

				for (int i = 1; i < pointsCount - 1; i++)
				{
					int minIndex = (int)(xMin + i * jump - jump / 4);
					for (int x = minIndex + 1; x < (int)(i * jump + jump / 4); x++)
						if (surfaceT[minIndex] > surfaceT[x])
							minIndex = x;

					if (this.surfaceT[minIndex] < ushort.MaxValue)
						bfPoints.Insert(bfPoints.Count - 1, new Point(location.X + minIndex, location.Y + this.surfaceT[minIndex]));
				}

				if (bfPoints.Count >= 2)
					return bfPoints.ToArray();
			}

			return null;
		}
		#endregion

		#region GetBottomBfPoints()
		public Point[] GetBottomBfPoints()
		{
			int xMin = _objectByCorners.Ll.X - location.X;
			int xMax = _objectByCorners.Lr.X - location.X;
			int pointsCount = 10;
			float jump = (xMax - xMin) / (float)pointsCount;

			if (jump > 10)
			{
				xMin = Math.Max(0, Math.Min(width, xMin));
				xMax = Math.Max(0, Math.Min(width, xMax));

				List<Point> bfPoints = new List<Point>();
				bfPoints.Add(new Point(location.X + xMin, location.Y + this.surfaceB[xMin]));
				bfPoints.Add(new Point(location.X + xMax, location.Y + surfaceB[xMax]));

				for (int i = 1; i < pointsCount - 1; i++)
				{
					int minIndex = (int)(xMin + i * jump - jump / 4);

					for (int x = minIndex + 1; x < (int)(i * jump + jump / 4); x++)
						if (surfaceB[minIndex] < surfaceB[x])
							minIndex = x;

					if(this.surfaceB[minIndex] > ushort.MinValue)
						bfPoints.Insert(bfPoints.Count - 1 , new Point(location.X + minIndex, location.Y + this.surfaceB[minIndex]));
				}
				
				if(bfPoints.Count >= 2)
					return bfPoints.ToArray();
			}

			return null;
		}
		#endregion

		#region Contains()
		public bool Contains(int x, int y)
		{
			Rectangle clip = new Rectangle(this.location.X, this.location.Y, this.width, this.height);

			if (clip.Contains(x, y))
			{
				if (x >= surfaceL[y - clip.Y] + clip.X && x <= surfaceR[y - clip.Y] + clip.X)
					if (y >= surfaceT[x - clip.X] + clip.Y && y <= surfaceB[x - clip.X] + clip.Y)
						return true;
			}

			return false;
		}
		#endregion

		#region DrawToImage()
		public void DrawToImage(Color color, BitmapData bmpData)
		{
			unsafe
			{
				byte* scan0 = (byte*)bmpData.Scan0.ToPointer();
				int stride = bmpData.Stride;

				for (int y = 0; y < height; y++)
					for (int x = 0; x < width; x++)
						if (x >= surfaceL[y] && x <= surfaceR[y] && y >= surfaceT[x] && y <= surfaceB[x])
						{
							*(scan0 + stride * (y + location.Y) + (x + location.X) * 3) = color.B;
							*(scan0 + stride * (y + location.Y) + (x + location.X) * 3 + 1) = color.G;
							*(scan0 + stride * (y + location.Y) + (x + location.X) * 3 + 2) = color.R;
						}
			}
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region CompactSurface()
		void CompactSurface()
		{
			int xL, yT, xR, yB;

			for (int x = 0; x < width; x++)
			{
				if (surfaceT[x] == ushort.MaxValue)
				{
					xL = -1;
					xR = -1;

					for (int tmp = x - 1; tmp >= 0; tmp--)
						if (surfaceT[tmp] != ushort.MaxValue)
						{
							xL = tmp;
							break;
						}

					for (int tmp = x + 1; tmp < width; tmp++)
						if (surfaceT[tmp] != ushort.MaxValue)
						{
							xR = tmp;
							break;
						}

					if (xL != -1 && xR != -1)
						surfaceT[x] = Convert.ToUInt16(surfaceT[xL] + (surfaceT[xR] - surfaceT[xL]) / (xR - xL));
					else if (xL != -1)
						surfaceT[x] = surfaceT[xL];
					else if (xR != -1)
						surfaceT[x] = surfaceT[xR];
				}

				if (surfaceB[x] == ushort.MaxValue)
				{
					xL = -1;
					xR = -1;

					for (int tmp = x - 1; tmp >= 0; tmp--)
						if (surfaceB[tmp] != ushort.MaxValue)
						{
							xL = tmp;
							break;
						}

					for (int tmp = x + 1; tmp < width; tmp++)
						if (surfaceB[tmp] != ushort.MaxValue)
						{
							xR = tmp;
							break;
						}

					if (xL != -1 && xR != -1)
						surfaceB[x] = Convert.ToUInt16(surfaceB[xL] + (surfaceB[xR] - surfaceB[xL]) / (xR - xL));
					else if (xL != -1)
						surfaceB[x] = surfaceB[xL];
					else if (xR != -1)
						surfaceB[x] = surfaceB[xR];
				}
			}

			for (int y = 0; y < height; y++)
			{
				if (surfaceL[y] == ushort.MaxValue)
				{
					yT = -1;
					yB = -1;

					for (int tmp = y - 1; tmp >= 0; tmp--)
						if (surfaceL[tmp] != ushort.MaxValue)
						{
							yT = tmp;
							break;
						}

					for (int tmp = y + 1; tmp < height; tmp++)
						if (surfaceL[tmp] != ushort.MaxValue)
						{
							yB = tmp;
							break;
						}

					if (yT != -1 && yB != -1)
						surfaceL[y] = Convert.ToUInt16(surfaceL[yT] + (surfaceL[yB] - surfaceL[yT]) / (yB - yT));
					else if (yT != -1)
						surfaceL[y] = surfaceL[yT];
					else if (yB != -1)
						surfaceL[y] = surfaceL[yB];
				}

				if (surfaceR[y] == ushort.MaxValue)
				{
					yT = -1;
					yB = -1;

					for (int tmp = y - 1; tmp >= 0; tmp--)
						if (surfaceR[tmp] != ushort.MaxValue)
						{
							yT = tmp;
							break;
						}

					for (int tmp = y + 1; tmp < height; tmp++)
						if (surfaceR[tmp] != ushort.MaxValue)
						{
							yB = tmp;
							break;
						}

					if (yT != -1 && yB != -1)
						surfaceR[y] = Convert.ToUInt16(surfaceR[yT] + (surfaceR[yB] - surfaceR[yT]) / (yB - yT));
					else if (yT != -1)
						surfaceR[y] = surfaceR[yT];
					else if (yB != -1)
						surfaceR[y] = surfaceR[yB];
				}
			}
		}
		#endregion

		#region GetObjectByCornersNew()
		private ObjectByCorners GetObjectByCornersNew(ObjectMap objectMap)
		{
			int right = this.location.X + this.width;
			int bottom = this.location.Y + this.height;
			Point ul = new Point(right, bottom);
			Point ur = new Point(this.location.X, bottom);
			Point ll = new Point(right, this.location.Y);
			Point lr = this.location;

			foreach (SymbolShape shape in objectMap.SymbolShapes)
			{
				foreach (SymbolShapePoint p in shape.EdgePoints)
				{
					if (ul.X + ul.Y > p.X + p.Y)
						ul = new Point(p.X, p.Y);
					if ((right - ur.X + ur.Y) > (right - p.X + p.Y))
						ur = new Point(p.X, p.Y);
					if ((ll.X + bottom - ll.Y) > (p.X + bottom - p.Y))
						ll = new Point(p.X, p.Y);
					if (lr.X + lr.Y < p.X + p.Y)
						lr = new Point(p.X, p.Y);
				}
			}

			return new ObjectByCorners(ul, ur, ll, lr);
		}
		#endregion

		#region GetObjectByCorners()
		private ObjectByCorners GetObjectByCorners(ObjectMap objectMap)
		{
			int x, y, i, j;
			int xMax = objectMap.Width - 1;
			int yMax = objectMap.Height - 1;
			int maxSteps = Math.Max(xMax, yMax);
			Point ul = new Point(0, 0);
			Point ur = new Point(xMax, 0);
			Point ll = new Point(0, yMax);
			Point lr = new Point(xMax, yMax);
			bool stop;

			stop = false;
			for (i = 0; (i <= maxSteps) && (stop == false); i++)
				for (j = 0; j <= i; j++)
				{
					x = j;
					y = i - j;

					if ((x >= 0) && (y >= 0) && (x <= xMax) && (y <= yMax) && objectMap.GetPoint(x, y))
					{
						ul = new Point(x, y);
						stop = true;
						break;
					}
				}

			stop = false;
			for (i = 0; (i <= maxSteps) && (stop == false); i++)
				for (j = 0; j <= i; j++)
				{
					x = xMax - j;
					y = i - j;

					if ((x >= 0) && (y >= 0) && (x <= xMax) && (y <= yMax) && objectMap.GetPoint(x, y))
					{
						ur = new Point(x, y);
						stop = true;
						break;
					}
				}

			stop = false;
			for (i = 0; (i <= maxSteps) && (stop == false); i++)
				for (j = 0; j <= i; j++)
				{
					x = j;
					y = yMax - (i - j);

					if ((x >= 0) && (y >= 0) && (x <= xMax) && (y <= yMax) && objectMap.GetPoint(x, y))
					{
						ll = new Point(x, y);
						stop = true;
						break;
					}
				}

			stop = false;
			for (i = 0; (i <= maxSteps) && (stop == false); i++)
				for (j = 0; j <= i; j++)
				{
					x = xMax - j;
					y = yMax - (i - j);

					if ((x >= 0) && (y >= 0) && (x <= xMax) && (y <= yMax) && objectMap.GetPoint(x, y))
					{
						lr = new Point(x, y);
						stop = true;
						break;
					}
				}

			ul.Offset(this.location);
			ur.Offset(this.location); 
			ll.Offset(this.location); 
			lr.Offset(this.location);

			return new ObjectByCorners(ul, ur, ll, lr);
		}
		#endregion

		#region IsValidBfCurve()
		/// <summary>
		/// False if :
		///		a) smoothenedCurve is null;
		///		b) shorter than 100 pixels;
		///		c) tangent is bigger than 45 degrees in any pixel
		///		d) there is a point where there is not continuous derivation (f(x+1) is different that f(x))
		///		e) if curve is wavy - there is -15 degrees angle within 2 15 degree angles (and the other direction)
		/// </summary>
		/// <param name="points"></param>
		/// <returns></returns>
		private static bool IsValidBfCurve(ushort[] smoothenedCurve)
		{
			if (smoothenedCurve == null || smoothenedCurve.Length < 100)
				return false;

			int width = smoothenedCurve.Length;
			double[] tangentArray = new double[width];

			for (int x = 0; x < width; x++)
			{
				int pLX = (x - 50 > 0) ? x - 50 : 01;
				int pLY = smoothenedCurve[pLX];
				int pRX = (x + 50 < width) ? x + 50 : width - 1;
				int pRY = smoothenedCurve[pRX];

				tangentArray[x] = Math.Atan2(pRY - pLY, pRX - pLX) * 180.0 / Math.PI;
			}

			for (int x = smoothenedCurve.Length - 2; x >= 0; x--)
			{
				//checking angle
				if (tangentArray[x] > 45)
					return false;

				//checking continuous derivation
				if (tangentArray[x] - tangentArray[x + 1] > 5.0 || tangentArray[x] - tangentArray[x + 1] < -5.0)
					return false;
			}

			//check waviness
			for (int x = 0; x < width; x++)
				if (tangentArray[x] > 15)
					for (int x1 = x + 1; x1 < width; x1++)
						if (tangentArray[x1] < -15)
							for (int x2 = x1 + 1; x2 < width; x2++)
								if (tangentArray[x2] > 15)
									return false;

			for (int x = 0; x < width; x++)
				if (tangentArray[x] < -15)
					for (int x1 = x + 1; x1 < width; x1++)
						if (tangentArray[x1] > 15)
							for (int x2 = x1 + 1; x2 < width; x2++)
								if (tangentArray[x2] < -15)
									return false;

			return true;
		}
		#endregion

		#endregion


	}
}
