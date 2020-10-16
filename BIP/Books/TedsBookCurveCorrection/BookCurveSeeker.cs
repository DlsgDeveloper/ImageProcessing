using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing.BigImages;

namespace ImageProcessing.Books.TedsBookCurveCorrection
{
	public class BookCurveSeeker
	{
		double	_regionWidthInInches = 0.5;
		int		_maxAngleToTest = 10;

		double _topOfTheLineThreshold = 1.2;
		double _bottomOfTheLineThreshold = .8;


		// PUBLIC METHODS
		#region public methods

		#region FindCurve()
		/// <summary>
		/// Works on 2 page book image.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="splitterL"></param>
		/// <param name="splitterR"></param>
		/// <returns></returns>
		public List<SingleLineRegion> FindCurve(Bitmap bitmap, int verticalLineX)
		{
			int dpi = Math.Max(200, Convert.ToInt32(bitmap.HorizontalResolution));

			List<SingleLineRegion>	regions = GetLineRegions(bitmap, verticalLineX, dpi);
			List<LineSum>			linesSums = GetLineSums(bitmap, regions[0].ImageClip.Left, regions[0].ImageClip.Right);

			FillInRegions(regions, linesSums);
			List<SingleLineRegion> maximas = GetLocalMaximas(regions);
			EliminateBadCandidates(maximas, dpi);
			FindRightAngles(bitmap, maximas);

			return maximas;
		}
		#endregion

		#region DrawResults()
		public void DrawResults(Bitmap bitmap, List<SingleLineRegion> regions)
		{
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

				if (regions.Count > 0)
					DrawVerticalLine(bitmapData, regions[0].ImagePoint.X);

				foreach (SingleLineRegion region in regions)
					DrawResult(bitmapData, region);
			}
			finally
			{
				if ((bitmap != null) && (bitmapData != null))
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#endregion


		// PRIVATE METHODS
		#region private methods

		#region GetLineRegions()
		private List<SingleLineRegion> GetLineRegions(Bitmap bitmap, int lineAt, int dpi)
		{
			int sourceW = bitmap.Width;
			int sourceH = bitmap.Height;

			int y;
			int regionWidth = Convert.ToInt32(Math.Max(100, dpi * _regionWidthInInches));
			int regionHeight = Convert.ToInt32(dpi / 100.0);

			List<SingleLineRegion> list = new List<SingleLineRegion>();

			for (y = regionHeight; y < sourceH - regionHeight; y++)
			{
				SingleLineRegion r = new SingleLineRegion(new Point(lineAt, y),
					new Rectangle(
						Math.Max(0, Math.Min(sourceW - regionWidth - 1, lineAt - (regionWidth / 2))),
						y - regionHeight / 2,
						regionWidth,
						regionHeight
						));

				list.Add(r);
			}

			return list;
		}
		#endregion

		#region GetLineSums()
		private unsafe List<LineSum> GetLineSums(Bitmap bitmap, int left, int right)
		{
			BitmapData bitmapData = null;
			List<LineSum> sums = new List<LineSum>();

			int x, y;
			int height = bitmap.Height;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				double gray;

				int stride = bitmapData.Stride;
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();

				for (y = 0; y < height; y++)
				{
					gray = 0;

					for (x = left; x < right; x++)
						gray += (pSource[(y * stride) + x * 3 + 2] + pSource[(y * stride) + x * 3 + 1] + pSource[(y * stride) + x * 3]);

					sums.Add(new LineSum()
					{
						Left = left,
						Right = right,
						Y = y,
						Average = gray / ((right - left + 1) * 3.0)
					});
				}

				return sums;
			}
			finally
			{
				if ((bitmap != null) && (bitmapData != null))
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region FillInRegions()
		private unsafe void FillInRegions(Bitmap bitmap, List<SingleLineRegion> regions)
		{
			BitmapData bitmapData = null;

			int x, y;
			int sourceW = bitmap.Width;
			int sourceH = bitmap.Height;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				byte* pCurrent;
				double gray;

				int stride = bitmapData.Stride;
				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();

				int pixelBytes = (bitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

				foreach (SingleLineRegion region in regions)
				{
					gray = 0;

					for (y = region.ImageClip.Y; y < region.ImageClip.Bottom; y++)
					{
						pCurrent = (byte*)pSource + y * stride;

						for (x = region.ImageClip.X; x < region.ImageClip.Right; x++)
							gray += (pCurrent[x * pixelBytes + 2] + pCurrent[x * pixelBytes + 1] + pCurrent[x * pixelBytes]) / 3;
					}

					region.AverageGray = gray / (double)(region.ImageClip.Width * region.ImageClip.Height);
				}
			}
			finally
			{
				if ((bitmap != null) && (bitmapData != null))
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region FillInRegions()
		private unsafe void FillInRegions(List<SingleLineRegion> regions, List<LineSum> linesSums)
		{
			int y;

			double gray;

			foreach (SingleLineRegion region in regions)
			{
				gray = 0;

				for (y = region.ImageClip.Y; y < region.ImageClip.Bottom; y++)
					gray += linesSums[y].Average;

				region.AverageGray = gray / (double)(region.ImageClip.Height);

				if (region.ImageClip.Height > 1)
				{
					region.AverageGrayT = (gray - linesSums[region.ImageClip.Top - 1].Average) / (double)(region.ImageClip.Height - 1);
					region.AverageGrayB = (gray - linesSums[region.ImageClip.Bottom + 1].Average) / (double)(region.ImageClip.Height - 1);
				}
				else
				{
					region.AverageGrayT = (gray - linesSums[region.ImageClip.Top - 1].Average);
					region.AverageGrayB = (gray - linesSums[region.ImageClip.Bottom + 1].Average);
				}
			}
		}
		#endregion

		#region GetLocalMaximas()
		/*private List<SingleLineRegion> GetLocalMaximas(List<SingleLineRegion> regions)
		{
			List<SingleLineRegion> maximas = new List<SingleLineRegion>();

			for (int y = 5; y < regions.Count - 5; y++)
			{
				double y0 = regions[y - 2].AverageGray;
				double y1 = regions[y - 1].AverageGray;
				double y2 = regions[y].AverageGray;
				double y3 = regions[y + 1].AverageGray;
				double y4 = regions[y + 2].AverageGray;

				if ((y0 >= y1) && (y1 >= y2) && (y2 <= y3) && (y3 <= y4))
				{
					if ((y0 - y2) > 5 && (y4 - y2) > 5)
					{
						double topBottomRatio = (y0 - y2) / (y4 - y2);

						if (topBottomRatio > _topOfTheLineThreshold)
							regions[y].DataPointType = DataPointType.TopOfTheLine;
						else if (topBottomRatio < _bottomOfTheLineThreshold)
							regions[y].DataPointType = DataPointType.BottomOfTheLine;
						else
							regions[y].DataPointType = DataPointType.Unknown;
					
						maximas.Add(regions[y]);
					}
						
				}
			}

			return maximas;
		}*/
		#endregion

		#region GetLocalMaximasTop()
		private List<SingleLineRegion> GetLocalMaximas(List<SingleLineRegion> regions)
		{
			List<SingleLineRegion> maximas = new List<SingleLineRegion>();

			for (int y = 5; y < regions.Count - 5; y++)
			{
				double y0T = regions[y - 2].AverageGrayT;
				double y1T = regions[y - 1].AverageGrayT;
				double y2T = regions[y].AverageGrayT;
				double y3T = regions[y + 1].AverageGrayT;
				double y4T = regions[y + 2].AverageGrayT;

				double y0B = regions[y - 2].AverageGrayB;
				double y1B = regions[y - 1].AverageGrayB;
				double y2B = regions[y].AverageGrayB;
				double y3B = regions[y + 1].AverageGrayB;
				double y4B = regions[y + 2].AverageGrayB;

				bool isTopPoint = (y0T >= y1T) && (y1T >= y2T) && (y2T <= y3T) && (y3T <= y4T) && ((y0T - y2T) > 10) && ((y4T - y2T) > 10);
				bool isBottomPoint = (y0B >= y1B) && (y1B >= y2B) && (y2B <= y3B) && (y3B <= y4B) && ((y0B - y2B) > 10) && ((y4B - y2B) > 10);

				if (isTopPoint && isBottomPoint)
				{
					double topBottomRatio = (y0T - y2T) / (y4B - y2B);

					if (topBottomRatio > 1)
						regions[y].DataPointType = DataPointType.TopOfTheLine;
					else if (topBottomRatio < 1)
						regions[y].DataPointType = DataPointType.BottomOfTheLine;
					else
						regions[y].DataPointType = DataPointType.Unknown;

					maximas.Add(regions[y]);
				}
				else if (isTopPoint)
				{
					regions[y].DataPointType = DataPointType.TopOfTheLine;
					maximas.Add(regions[y]);
				}
				else if (isBottomPoint)
				{
					regions[y].DataPointType = DataPointType.BottomOfTheLine;
					maximas.Add(regions[y]);
				}
			}

			return maximas;
		}
		#endregion

		#region EliminateBadCandidates()
		private void EliminateBadCandidates(List<SingleLineRegion> regions, int dpi)
		{
			/*List<SingleLineRegion> maximas = new List<SingleLineRegion>();
			int maxLineDistance = dpi / 10;

			for (int i = regions.Count - 1; i >= 0; i--)
			{
				SingleLineRegion r = regions[i];
				bool keep = false;

				if (i > 0 && (r.ImagePoint.Y - regions[i - 1].ImagePoint.Y) <= maxLineDistance)
					keep = true;

				if (i < regions.Count - 1 && (r.ImagePoint.Y - regions[i + 1].ImagePoint.Y) <= maxLineDistance)
					keep = true;

				if (keep == false)
					regions.RemoveAt(i);
			}*/
		}
		#endregion

		#region FindRightAngles()
		private void FindRightAngles(Bitmap bitmap, List<SingleLineRegion> regions)
		{
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				foreach (SingleLineRegion region in regions)
				{
					for (int angle = -_maxAngleToTest; angle <= _maxAngleToTest; angle++)
					{
						double angleInRadians = angle * Math.PI / 180.0;
						double gray = GetGray(bitmapData, region, angleInRadians);

						if (gray < region.AverageGray)
						{
							region.AverageGray = gray;
							region.BestAngle = angleInRadians;
						}
					}
				}

			}
			finally
			{
				if ((bitmap != null) && (bitmapData != null))
					bitmap.UnlockBits(bitmapData);
			}

		}
		#endregion

		#region GetGray()
		/// <summary>
		/// Angle in degrees
		/// </summary>
		/// <param name="region"></param>
		/// <param name="angle"></param>
		/// <param name="bitmapSize"></param>
		/// <returns></returns>
		private unsafe double GetGray(BitmapData bitmapData, SingleLineRegion region, double angleInRadians)
		{
			double slope = Math.Tan(angleInRadians);
			int gray = 0;

			int width = bitmapData.Width;
			int height = bitmapData.Height;

			int stride = bitmapData.Stride;
			byte* pSource = (byte*)bitmapData.Scan0.ToPointer();

			for (int y = region.ImageClip.Y; y < region.ImageClip.Bottom; y++)
			{
				double yCurrent = y - slope * region.ImageClip.Width / 2.0;

				for (int x = region.ImageClip.X; x < region.ImageClip.Right; x++)
				{
					int yCurrentInt = Convert.ToInt32(yCurrent + (x - region.ImageClip.X) * slope);

					if (yCurrentInt < 0 || yCurrentInt >= height)
						gray += 255;
					else
						gray += (pSource[yCurrentInt * stride + x * 3 + 2] + pSource[yCurrentInt * stride + x * 3 + 1] + pSource[yCurrentInt * stride + x * 3]);
				}
			}

			return gray / (region.ImageClip.Width * region.ImageClip.Height * 3.0);
		}
		#endregion

		#region DrawVerticalLine()
		private unsafe void DrawVerticalLine(BitmapData bitmapData, int x)
		{
			int width = bitmapData.Width;
			int height = bitmapData.Height;

			int stride = bitmapData.Stride;
			byte* pSource = (byte*)bitmapData.Scan0.ToPointer();

			for (int y = 0; y < height; y++)
			{
				pSource[y * stride + x * 3 + 2] = 255;
				pSource[y * stride + x * 3 + 1] = 0;
				pSource[y * stride + x * 3] = 0;
			}
		}
		#endregion

		#region DrawResult()
		/// <summary>
		/// Angle in degrees
		/// </summary>
		/// <param name="region"></param>
		/// <param name="angle"></param>
		/// <param name="bitmapSize"></param>
		/// <returns></returns>
		private unsafe void DrawResult(BitmapData bitmapData, SingleLineRegion region)
		{
			double slope = Math.Tan(region.BestAngle);

			int width = bitmapData.Width;
			int height = bitmapData.Height;

			int stride = bitmapData.Stride;
			byte* pSource = (byte*)bitmapData.Scan0.ToPointer();

			for (int y = region.ImagePoint.Y; y <= region.ImagePoint.Y; y++)
			{
				double yCurrent = y - slope * region.ImageClip.Width / 2.0;

				for (int x = region.ImageClip.X; x <= region.ImageClip.Right; x++)
				{
					int yCurrentInt = Convert.ToInt32(yCurrent + (x - region.ImageClip.X) * slope);

					if (yCurrentInt >= 0 && yCurrentInt < height)
					{
						if (region.DataPointType == DataPointType.TopOfTheLine)
						{
							pSource[yCurrentInt * stride + x * 3 + 2] = 255;
							pSource[yCurrentInt * stride + x * 3 + 1] = 0;
							pSource[yCurrentInt * stride + x * 3] = 0;
						}
						else if (region.DataPointType == DataPointType.BottomOfTheLine)
						{
							pSource[yCurrentInt * stride + x * 3 + 2] = 0;
							pSource[yCurrentInt * stride + x * 3 + 1] = 255;
							pSource[yCurrentInt * stride + x * 3] = 0;
						}
						else
						{
							pSource[yCurrentInt * stride + x * 3 + 2] = 0;
							pSource[yCurrentInt * stride + x * 3 + 1] = 0;
							pSource[yCurrentInt * stride + x * 3] = 255;
						}
					}
				}
			}
		}
		#endregion

		#endregion

	}
}
