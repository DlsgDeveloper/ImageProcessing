using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIP.Books
{
	public class LinearRegression
	{
		double _a;
		double _b;
		List<RegressionPoint> _regressionPoints;

		#region constructor
		public LinearRegression(List<RegressionPoint> regressionPoints)
		{
			_regressionPoints = regressionPoints;

			Recompute();
		}
		#endregion


		// PUBLIC PROPERTIES
		#region public properties

		public int PointsCount { get { return _regressionPoints.Count; } }

		#endregion




		// PUBLIC METHODS
		#region public methods

		#region GetXAt()
		public int GetXAt(int y)
		{
			if (_a != 0)
				return Convert.ToInt32(_a * y + _b);
			else
				return _regressionPoints[0].X;
		}
		#endregion

		#region GetSplitterLine()
		public SplitterLine GetSplitterLine(int bitmapW, int bitmapH)
		{
			//y = a * x + b
			//x = (y - b) / a
			double xTop;
			double yTop = 0;
			double xBottom;
			double yBottom = bitmapH;

			if (_a == 0)
			{
				if (_regressionPoints.Count > 0)
				{
					xTop = _regressionPoints[0].X;
					xBottom = _regressionPoints[0].X;
				}
				else
				{
					xTop = bitmapW / 2;
					xBottom = bitmapW / 2;
				}
			}
			else
			{
				xTop = GetXAt(0);
				xBottom = GetXAt(bitmapH);
			}

			SplitterLine splitterLine = new SplitterLine(
				new System.Drawing.Point(Convert.ToInt32(xTop), Convert.ToInt32(yTop)),
				new System.Drawing.Point(Convert.ToInt32(xBottom), Convert.ToInt32(yBottom)),
				Math.Min(1, _regressionPoints.Count / 15)
				);

			return splitterLine;
		}
		#endregion

		#region DeletePointsFurtherThan()
		public void DeletePointsFurtherThan(int maxTolerableHorizontalDistance)
		{
			for (int i = _regressionPoints.Count - 1; i >= 0; i--)
			{
				int regressionLineX = GetXAt(_regressionPoints[i].Y);

				if (Math.Abs(regressionLineX - _regressionPoints[i].X) > maxTolerableHorizontalDistance)
					_regressionPoints.RemoveAt(i);
			}

			Recompute();
		}
		#endregion

		#endregion


		// PRIVATE METHODS
		#region private methods

		#region Recompute()
		/*private void Recompute()
		{
			if (_regressionPoints.Count > 0)
			{
				double sumX = _regressionPoints.Sum(x => x.X * x.Confidence);
				double sumY = _regressionPoints.Sum(x => x.Y * x.Confidence);
				double sumXY = _regressionPoints.Sum(x => x.X * x.Y * x.Confidence);
				double sumX2 = _regressionPoints.Sum(x => x.X * x.X * x.Confidence);
				double sumW = _regressionPoints.Sum(x => x.Confidence);

				if (((sumW * sumX2) - (sumX * sumX)) > 0)
				{
					_a = ((sumW * sumXY) - (sumX * sumY)) / ((sumW * sumX2) - (sumX * sumX));
					_b = ((sumY * sumX2) - (sumX * sumXY)) / ((sumW * sumX2) - (sumX * sumX));
				}
				else
				{
					_a = 0;
					_b = 0;
				}
			}
			else
			{
				_a = 0;
				_b = 0;
			}
		}*/
		#endregion

		#region Recompute()
		private void Recompute()
		{
			if (_regressionPoints.Count > 0)
			{
				//for horizontal line		
				List<RegressionPoint> copy = new List<RegressionPoint>();

				// swapping X and Y - for vertical results
				_regressionPoints.ForEach(x => copy.Add(new RegressionPoint(x.Y, x.X, x.Confidence)));

				int numPoints = copy.Count;
				double meanX = copy.Average(point => point.X);
				double meanY = copy.Average(point => point.Y);

				long sumXSquared = GetSumXX(copy);
				long sumXY = GetSumXY(copy);

				if (((double)sumXSquared / numPoints - meanX * meanX) != 0)
				{
					_a = ((double)sumXY / numPoints - meanX * meanY) / ((double)sumXSquared / numPoints - meanX * meanX);
					_b = -(_a * meanX - meanY);
				}
				else
				{
					_a = 0;
					_b = 0;
				}
			}
			else
			{
				_a = 0;
				_b = 0;
			}
		}
		#endregion

		#region GetSumXX()
		private long GetSumXX(List<RegressionPoint> regressionPoints)
		{
			long sum = 0;

			foreach (RegressionPoint rp in regressionPoints)
				sum += rp.X * rp.X;

			return sum;
		}
		#endregion

		#region GetSumXY()
		private long GetSumXY(List<RegressionPoint> regressionPoints)
		{
			long sum = 0;

			foreach (RegressionPoint rp in regressionPoints)
				sum += rp.X * rp.Y;

			return sum;
		}
		#endregion

		#endregion
	}
}
