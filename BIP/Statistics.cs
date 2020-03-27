using System;
using System.Collections.Generic;
using System.Text;

namespace ImageProcessing
{
	public class Statistics
	{

		#region GetArithmeticMean()
		/// <summary>
		/// returns average
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		public static double GetArithmeticMean(List<int> values)
		{
			double sum = 0;

			foreach (int val in values)
				sum += val;

			return sum / (values.Count - 1.0);
		}
		#endregion

		#region GetGeometricMean()
		/// <summary>
		///	              1/n
		/// x = (PI(x[i]))
		/// 
		/// example: 3, 3, 5; result = (3 * 3 * 5)^(1/3)
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		public static double GetGeometricMean(List<int> values)
		{
			double sum = 1;

			foreach (int val in values)
			{
				if(val != 0)
					sum *= val;
			}

			return Math.Pow(sum, (1.0 / values.Count));
		}
		#endregion

		#region GetHarmonicMean()
		/// <summary>
		///				            -1
		/// x = (n * SUM(1 / x[i]))
		/// 
		/// example: 3, 3, 5, result = 3 / ( (1/3) + (1/3) + (1/5) )
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		public static double? GetHarmonicMean(List<int> values)
		{
			double sum = 0;

			foreach (int val in values)
			{
				if(val != 0)
					sum += 1.0 / val;
			}

			if (sum > 0)
				return values.Count / sum;
			else
				return null;
		}

		public static double? GetHarmonicMean(List<float> values)
		{
			double sum = 0;

			foreach (float val in values)
			{
				if (val != 0)
					sum += 1.0 / val;
			}

			if (sum > 0)
				return values.Count / sum;
			else
				return null;
		}

		public static double? GetHarmonicMean(List<double> values)
		{
			double sum = 0;

			foreach (double val in values)
			{
				if (val != 0)
					sum += 1.0 / val;
			}

			if (sum > 0)
				return values.Count / sum;
			else
				return null;
		}
		#endregion

	}
}
