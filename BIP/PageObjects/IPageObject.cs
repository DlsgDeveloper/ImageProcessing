using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ImageProcessing.PageObjects
{
	public interface IPageObject
	{
		int X { get;}
		int Y { get;}
		int Width { get;}
		int Height { get;}
		int Right { get;}
		int Bottom { get;}
		DelimiterZone Zone { get;}
	}

	#region enum SortType
	public enum SortType
	{
		Horizontal,
		Vertical,
		None
	}
	#endregion

	#region class RectangleHorizontalComparer
	public class RectangleHorizontalComparer : System.Collections.Generic.IComparer<Rectangle>
	{
		public int Compare(Rectangle r1, Rectangle r2)
		{
			if (r1.X > r2.X)
				return 1;
			if (r1.X < r2.X)
				return -1;

			return 0;
		}
	}
	#endregion


}
