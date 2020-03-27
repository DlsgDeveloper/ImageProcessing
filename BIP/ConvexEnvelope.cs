using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing.PageObjects;

namespace ImageProcessing
{
	public class ConvexEnvelope
	{
		Rectangle	clip;
		ushort?[]	surfaceL;	//stored in local coordinates. To get global coordinates, add location
		ushort?[]	surfaceT;	//stored in local coordinates. To get global coordinates, add location
		ushort?[]	surfaceR;	//stored in local coordinates. To get global coordinates, add location
		ushort?[]	surfaceB;	//stored in local coordinates. To get global coordinates, add location


		#region constructor
		public ConvexEnvelope(Point location, ObjectMap objectMap)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			this.clip = new Rectangle(location.X, location.Y, objectMap.Width, objectMap.Height);
			int width = clip.Width;
			int height = clip.Height;

			surfaceL = new ushort?[height];
			surfaceT = new ushort?[width];
			surfaceR = new ushort?[height];
			surfaceB = new ushort?[width];

			FillSurfaceArrays(objectMap);

#if DEBUG
			TimeSpan span0 = DateTime.Now.Subtract(start);
			start = DateTime.Now;
#endif

			MakeConvex();

#if DEBUG
			//Console.WriteLine("ConvexEnvelope: " + span0.ToString() + ", " + DateTime.Now.Subtract(start).ToString());
#endif
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties

		internal Rectangle Rectangle { get { return this.clip; } }
		/*private ushort?[] SurfaceL { get { return surfaceL; } }
		private ushort?[] SurfaceT { get { return surfaceT; } }
		private ushort?[] SurfaceR { get { return surfaceR; } }
		private ushort?[] SurfaceB { get { return surfaceB; } }*/

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region Merge()
		public void Merge(Point location, ObjectMap objectMap)
		{
			ConvexEnvelope envelope = new ConvexEnvelope(location, objectMap);

			Merge(envelope);
		}

		public void Merge(ConvexEnvelope envelope)
		{
			Rectangle originalClip = clip;
			this.clip = Rectangle.Union(clip, envelope.Rectangle);

			if (originalClip != clip)
			{
				// shift old object map
				int width = clip.Width;
				int height = clip.Height;
				int shiftX = originalClip.X - clip.X;
				int shiftY = originalClip.Y - clip.Y;

				ushort?[] oldSurfaceL = surfaceL;
				ushort?[] oldSurfaceT = surfaceT;
				ushort?[] oldSurfaceR = surfaceR;
				ushort?[] oldSurfaceB = surfaceB;

				surfaceL = new ushort?[height];
				surfaceT = new ushort?[width];
				surfaceR = new ushort?[height];
				surfaceB = new ushort?[width];

				for (int x = 0; x < oldSurfaceT.Length; x++)
				{
					if (oldSurfaceT[x].HasValue)
						surfaceT[x + shiftX] = (ushort?)(oldSurfaceT[x] + shiftY);
					if (oldSurfaceB[x].HasValue)
						surfaceB[x + shiftX] = (ushort?)(oldSurfaceB[x] + shiftY);
				}
				for (int y = 0; y < oldSurfaceL.Length; y++)
				{
					if (oldSurfaceL[y].HasValue)
						surfaceL[y + shiftY] = (ushort?)(oldSurfaceL[y] + shiftX);
					if (oldSurfaceR[y].HasValue)
						surfaceR[y + shiftY] = (ushort?)(oldSurfaceR[y] + shiftX);
				}
			}

			//all new object
			int objShiftX = envelope.Rectangle.X - clip.X;
			int objShiftY = envelope.Rectangle.Y - clip.Y;
			int objWidth = envelope.Rectangle.Width;
			int objHeight = envelope.Rectangle.Height;

			for (int x = 0; x < objWidth; x++)
			{
				if (envelope.surfaceT[x].HasValue)
					if (surfaceT[x + objShiftX].HasValue == false || surfaceT[x + objShiftX] > envelope.surfaceT[x] + objShiftY)
						surfaceT[x + objShiftX] = (ushort)(envelope.surfaceT[x] + objShiftY);

				if (envelope.surfaceB[x].HasValue)
					if (surfaceB[x + objShiftX].HasValue == false || surfaceB[x + objShiftX] < envelope.surfaceB[x] + objShiftY)
						surfaceB[x + objShiftX] = (ushort)(envelope.surfaceB[x] + objShiftY);
			}

			for (int y = 0; y < objHeight; y++)
			{
				if (envelope.surfaceL[y].HasValue)
					if (surfaceL[y + objShiftY].HasValue == false || surfaceL[y + objShiftY] > envelope.surfaceL[y] + objShiftX)
						surfaceL[y + objShiftY] = (ushort)(envelope.surfaceL[y] + objShiftX);

				if (envelope.surfaceR[y].HasValue)
					if (surfaceR[y + objShiftY].HasValue == false || surfaceR[y + objShiftY] < envelope.surfaceR[y] + objShiftX)
						surfaceR[y + objShiftY] = (ushort)(envelope.surfaceR[y] + objShiftX);
			}

			MakeConvex();
		}
		#endregion	

		#region Inflate()
		/// <summary>
		/// Positive to inflate, negative to shrink
		/// </summary>
		/// <param name="offset"></param>
		public void Inflate(int amount)
		{
			clip.Inflate(amount, amount);

			if (amount > 0)
			{
				ushort?[] newSurfaceL = new ushort?[clip.Height];
				ushort?[] newSurfaceT = new ushort?[clip.Width];
				ushort?[] newSurfaceR = new ushort?[clip.Height];
				ushort?[] newSurfaceB = new ushort?[clip.Width];

				for (int x = 0; x < surfaceT.Length; x++)
				{
					if (surfaceT[x].HasValue)
					{
						newSurfaceT[x + amount] = (ushort)(surfaceT[x]);
						newSurfaceB[x + amount] = (ushort)(surfaceB[x] + 2 * amount);
					}
				}

				for (int y = 0; y < surfaceL.Length; y++)
				{
					if (surfaceL[y].HasValue)
					{
						newSurfaceL[y + amount] = (ushort)(surfaceL[y]);
						newSurfaceR[y + amount] = (ushort)(surfaceR[y] + 2 * amount);
					}
				}

				surfaceL = newSurfaceL;
				surfaceT = newSurfaceT;
				surfaceR = newSurfaceR;
				surfaceB = newSurfaceB;
			}
			else if (amount < 0)
			{
				amount = -amount;

				int			width = surfaceT.Length;
				int			height = surfaceL.Length;
				byte[,]		array = new byte[clip.Height, clip.Width / 8];

				for (int x = 0; x < width; x++)
					if (surfaceT[x].HasValue)
					{
						//surfaceT[x] += (ushort)amount;
						surfaceB[x] -= (ushort) (2 * amount);
					}
				for (int y = 0; y < height; y++)
					if (surfaceL[y].HasValue)
					{
						//surfaceL[y] += (ushort)amount;
						surfaceR[y] -= (ushort)(2*amount);
					}

				for (int x = 0; x < clip.Width; x++)
					for (int y = 0; y < clip.Height; y++)
					{
						if (surfaceT[x + amount].HasValue && surfaceL[y + amount].HasValue &&
							x >= surfaceL[y + amount] && x <= surfaceR[y + amount] &&
							y >= surfaceT[x + amount] && y <= surfaceB[x + amount])
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07)); 
						}
					}

				surfaceL = new ushort?[clip.Height];
				surfaceT = new ushort?[clip.Width];
				surfaceR = new ushort?[clip.Height];
				surfaceB = new ushort?[clip.Width];

				ObjectMap objectMap = new ObjectMap(new Rectangle(0, 0, clip.Width, clip.Height), array);
				FillSurfaceArrays(objectMap);
			}

			MakeConvex();
		}
		#endregion

		#region Contains()
		public bool Contains(Point p)
		{
			return Contains(p.X, p.Y);
		}

		public bool Contains(int x, int y)
		{
			if (clip.Contains(x, y))
			{
				if (surfaceL[y - clip.Y].HasValue && surfaceT[x - clip.X].HasValue)
					if (x >= surfaceL[y - clip.Y] + clip.X && x <= surfaceR[y - clip.Y] + clip.X)
						if (y >= surfaceT[x - clip.X] + clip.Y && y <= surfaceB[x - clip.X] + clip.Y)
							return true;
			}

			return false;
		}

		public bool Contains(Rectangle rect)
		{
			if (Rectangle.Intersect(this.clip, rect) != Rectangle.Empty)
			{
				for (int x = rect.X; x <= rect.Right; x++)
				{
					if (Contains(x, rect.Y))
						return true;
					if (Contains(x, rect.Bottom))
						return true;
				}

				for (int y = rect.Y; y <= rect.Bottom; y++)
				{
					if (Contains(rect.X, y))
						return true;
					if (Contains(rect.Right, y))
						return true;
				}

				Point? validPoint = GetAnyValidPoint();

				if (validPoint.HasValue && rect.Contains(validPoint.Value))
					return true;
			}

			return false;
		}
		#endregion

		#region InterceptsWith()
		public bool InterceptsWith(ConvexEnvelope convexEnvelope)
		{	
			if (Rectangle.Intersect(this.Rectangle, convexEnvelope.Rectangle) != Rectangle.Empty)
			{
				int xFrom = (this.Rectangle.X > convexEnvelope.Rectangle.X) ? this.Rectangle.X : convexEnvelope.Rectangle.X;
				int xTo = (this.Rectangle.Right < convexEnvelope.Rectangle.Right) ? this.Rectangle.Right : convexEnvelope.Rectangle.Right;
				int thisClipX = this.clip.X;
				int thisClipY = this.clip.Y;
				int paramClipX = convexEnvelope.Rectangle.X;
				int paramClipY = convexEnvelope.Rectangle.Y;

				for (int x = xFrom; x < xTo; x++)
				{
					if (surfaceT[xFrom - this.clip.X].HasValue && convexEnvelope.surfaceT[xFrom - convexEnvelope.clip.X].HasValue)
					{
						if ((surfaceT[x - thisClipX] + thisClipY <= convexEnvelope.surfaceT[x - paramClipX] + paramClipY &&
							surfaceB[x - thisClipX] + thisClipY >= convexEnvelope.surfaceT[x - paramClipX] + paramClipY) ||
							(surfaceT[x - thisClipX] + thisClipY >= convexEnvelope.surfaceT[x - paramClipX] + paramClipY &&
							surfaceT[x - thisClipX] + thisClipY <= convexEnvelope.surfaceB[x - paramClipX] + paramClipY))
							return true;
					}
				}
			}

			return false;
		}

		public bool InterceptsWith(PageObjects.ObjectShape objectShape)
		{
			if (Rectangle.Intersect(this.Rectangle, objectShape.Rectangle) != Rectangle.Empty)
			{
				int xFrom = (this.Rectangle.X > objectShape. Rectangle.X) ? this.Rectangle.X : objectShape.Rectangle.X;
				int xTo = (this.Rectangle.Right < objectShape.Rectangle.Right) ? this.Rectangle.Right : objectShape.Rectangle.Right;
				int thisClipX = this.clip.X;
				int thisClipY = this.clip.Y;
				int paramClipX = objectShape.Rectangle.X;
				int paramClipY = objectShape.Rectangle.Y;

				for (int x = xFrom; x < xTo; x++)
				{
					if (surfaceT[xFrom - this.clip.X].HasValue)
					{
						if ((surfaceT[x - thisClipX] + thisClipY <= objectShape.SurfaceT[x - paramClipX] + paramClipY &&
							surfaceB[x - thisClipX] + thisClipY >= objectShape.SurfaceT[x - paramClipX] + paramClipY) ||
							(surfaceT[x - thisClipX] + thisClipY >= objectShape.SurfaceT[x - paramClipX] + paramClipY &&
							surfaceT[x - thisClipX] + thisClipY <= objectShape.SurfaceB[x - paramClipX] + paramClipY))
							return true;
					}
				}
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

				for (int y = 0; y < clip.Height; y++)
					for (int x = 0; x < clip.Width; x++)
						if (surfaceL[y].HasValue && x >= surfaceL[y] && 
							surfaceR[y].HasValue && x <= surfaceR[y] && 
							surfaceT[x].HasValue && y >= surfaceT[x] && 
							surfaceB[x].HasValue && y <= surfaceB[x])
						{
							if (x + clip.X >= 0 && x + clip.X < bmpData.Width && y + clip.Y >= 0 && y + clip.Y < bmpData.Height)
							{
								*(scan0 + stride * (y + clip.Y) + (x + clip.X) * 3) = color.B;
								*(scan0 + stride * (y + clip.Y) + (x + clip.X) * 3 + 1) = color.G;
								*(scan0 + stride * (y + clip.Y) + (x + clip.X) * 3 + 2) = color.R;
							}
						}
			}
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region MakeConvex()
		void MakeConvex()
		{
			int xL, yT;
			
			//top curve
			xL = 0;
			while (xL < clip.Width - 1)
			{
				if (surfaceT[xL].HasValue)
				{
					double? bestAngle = null;
					int xRIndex = -1;

					for (int xR = clip.Width - 1; xR > xL; xR--)
					{
						if (surfaceT[xR].HasValue)
						{
							double angle = Math.Atan2((int)(surfaceT[xR] - surfaceT[xL]), xR - xL);

							while (angle < -Math.PI / 2)
								angle = angle + Math.PI;
							while (angle > Math.PI / 2)
								angle = angle - Math.PI;

							if (bestAngle.HasValue == false || bestAngle > angle)
							{
								bestAngle = angle;
								xRIndex = xR;
							}
						}
					}

					if (xRIndex >= 0)
					{
						AddLineToSurface(new Point(xL, (int)surfaceT[xL]), new Point(xRIndex, (int)surfaceT[xRIndex]));
						xL = xRIndex;
					}
					else
						xL++;
				}
				else
					xL++;
			}

			//bottom curve
			xL = 0;
			while (xL < clip.Width - 1)
			{
				if (surfaceB[xL].HasValue)
				{
					double? bestAngle = null;
					int xRIndex = -1;

					for (int xR = clip.Width - 1; xR > xL; xR--)
					{
						if (surfaceB[xR].HasValue)
						{
							double angle = Math.Atan2((int)(surfaceB[xR] - surfaceB[xL]), xR - xL);

							while (angle < -Math.PI / 2)
								angle = angle + Math.PI;
							while (angle > Math.PI / 2)
								angle = angle - Math.PI;

							if (bestAngle.HasValue == false || bestAngle < angle)
							{
								bestAngle = angle;
								xRIndex = xR;
							}
						}
					}

					if (xRIndex >= 0)
					{
						AddLineToSurface(new Point(xL, (int)surfaceB[xL]), new Point(xRIndex, (int)surfaceB[xRIndex]));
						xL = xRIndex;
					}
					else
						xL++;
				}
				else
					xL++;
			}

			//left curve
			yT = 0;
			while (yT < clip.Height - 1)
			{
				if (surfaceL[yT].HasValue)
				{
					double? bestAngle = null;
					int yBIndex = -1;

					for (int yB = clip.Height - 1; yB > yT; yB--)
					{
						if (surfaceL[yB].HasValue)
						{
							double angle = Math.Atan2(yB - yT, (int)(surfaceL[yB] - surfaceL[yT]));

							while (angle < 0)
								angle = angle + Math.PI;
							while (angle > Math.PI)
								angle = angle - Math.PI;

							if (bestAngle.HasValue == false || bestAngle < angle)
							{
								bestAngle = angle;
								yBIndex = yB;
							}
						}
					}

					if (yBIndex >= 0)
					{
						AddLineToSurface(new Point((int)surfaceL[yT], yT), new Point((int)surfaceL[yBIndex], yBIndex));
						yT = yBIndex;
					}
					else
						yT++;
				}
				else
					yT++;
			}

			//right curve
			yT = 0;
			while (yT < clip.Height - 1)
			{
				if (surfaceR[yT].HasValue)
				{
					double? bestAngle = null;
					int yBIndex = -1;

					for (int yB = clip.Height - 1; yB > yT; yB--)
					{
						if (surfaceR[yB].HasValue)
						{
							double angle = Math.Atan2(yB - yT, (int)(surfaceR[yB] - surfaceR[yT]));

							while (angle < 0)
								angle = angle + Math.PI;
							while (angle > Math.PI)
								angle = angle - Math.PI;

							if (bestAngle.HasValue == false || bestAngle > angle)
							{
								bestAngle = angle;
								yBIndex = yB;
							}
						}
					}

					if (yBIndex >= 0)
					{
						AddLineToSurface(new Point((int)surfaceR[yT], yT), new Point((int)surfaceR[yBIndex], yBIndex));
						yT = yBIndex;
					}
					else
						yT++;
				}
				else
					yT++;
			}
		}
		#endregion

		#region AddLineToSurface()
		void AddLineToSurface(Point p1, Point p2)
		{
			List<Point> linePoints = GetLinePoints(p1, p2);

			foreach (Point p in linePoints)
			{
				if (surfaceL[p.Y].HasValue == false || surfaceL[p.Y] > p.X)
					surfaceL[p.Y] = (ushort)p.X;
				if (surfaceR[p.Y].HasValue == false || surfaceR[p.Y] < p.X)
					surfaceR[p.Y] = (ushort)p.X;
				if (surfaceT[p.X].HasValue == false || surfaceT[p.X] > p.Y)
					surfaceT[p.X] = (ushort)p.Y;
				if (surfaceB[p.X].HasValue == false || surfaceB[p.X] < p.Y)
					surfaceB[p.X] = (ushort)p.Y;
			}
		}
		#endregion

		#region GetLinePoints()
		/// <summary>
		/// Returns all integer points between p1, p2, excluding both of them.
		/// There are not 'holes' in the points. It is not guaranteed that points would be somehow sorted. 
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		List<Point> GetLinePoints(Point p1, Point p2)
		{
			List<Point> linePoints = new List<Point>();
			
			if (Math.Abs(p1.X - p2.X) > Math.Abs(p1.Y - p2.Y))
			{
				int minX = (p1.X < p2.X) ? p1.X : p2.X;
				int maxX = (p1.X > p2.X) ? p1.X : p2.X;

				for (int x = minX; x <= maxX; x++)
				{
					int y = Convert.ToInt32(Arithmetic.GetY(p1, p2, x));
					linePoints.Add(new Point(x, y));
				}
			}
			else
			{
				int minY = (p1.Y < p2.Y) ? p1.Y : p2.Y;
				int maxY = (p1.Y > p2.Y) ? p1.Y : p2.Y;

				for (int y = minY; y <= maxY; y++)
				{
					int x = Convert.ToInt32(Arithmetic.GetX(p1, p2, y));
					linePoints.Add(new Point(x, y));
				}
			}

			return linePoints;
		}
		#endregion

		#region GetAnyValidPoint()
		Point? GetAnyValidPoint()
		{
			for (int x = 0; x < clip.Width; x++)
				for (int y = 0; y < clip.Height; y++)
					if (surfaceL[y].HasValue && surfaceR[y].HasValue && surfaceT[y].HasValue && surfaceB[y].HasValue)
						return new Point(clip.X + surfaceL[y].Value, clip.Y + surfaceT[x].Value);

			return null;
		}
		#endregion

		#region FillSurfaceArrays
		private void FillSurfaceArrays(ObjectMap objectMap)
		{
			foreach (SymbolShape shape in objectMap.SymbolShapes)
			{
				foreach (SymbolShapePoint pp in shape.EdgePoints)
				{
					SymbolShapePoint p = new SymbolShapePoint(pp.X - this.clip.X, pp.Y - this.clip.Y);

					if (surfaceL[p.Y].HasValue == false || surfaceL[p.Y] > p.X)
						surfaceL[p.Y] = (ushort)p.X;
					if (surfaceR[p.Y].HasValue == false || surfaceR[p.Y] < p.X)
						surfaceR[p.Y] = (ushort)p.X;
					if (surfaceT[p.X].HasValue == false || surfaceT[p.X] > p.Y)
						surfaceT[p.X] = (ushort)p.Y;
					if (surfaceB[p.X].HasValue == false || surfaceB[p.X] < p.Y)
						surfaceB[p.X] = (ushort)p.Y;
				}
			}

		}
		#endregion

		#endregion
	}
}
