using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessingApp
{
	class Line2D
	{
		public static void Test()
		{
			ImageProcessing.Line2D line = new ImageProcessing.Line2D(1,0,-10);
			int x = 0;
			int y = 0;
			double perpendicularPointX = 3;
			double perpendicularPointY = 6;

			Console.WriteLine(line.ToString());
			Console.WriteLine("For Y = {1}, X = {0}", line.GetX(y), y);
			Console.WriteLine("For X = {0}, Y = {1}", x, line.GetY(x));


			Console.WriteLine();
			Console.WriteLine("Getting perpendicular line going thru [{0}, {1}]", perpendicularPointX, perpendicularPointY);
			ImageProcessing.Line2D lineT = line.GetPerpendicularLine(perpendicularPointX, perpendicularPointY);
			Console.WriteLine(lineT.ToString());
			Console.WriteLine("For Y = {1}, X = {0}", lineT.GetX(y), y);
			Console.WriteLine("For X = {0}, Y = {1}", x, lineT.GetY(x));
		}
	}
}
