using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing.Rasters.Corners
{
	public class CornersSearchEngine
	{

		//PUBLIC METHODS
		#region public methods

		#region GetCorners()
		public static ObjectCorners GetCorners(byte[,] array, int width)
		{
			int height = array.GetLength(0);

			int x, y, i;
			bool brk = false;

			ObjectCorners objectByCorners = new ObjectCorners(width, height);

			// upper left
			brk = false;
			for (i = 0; i < width && i < height && brk == false; i++)
			{
				for (x = i, y = 0; x >= 0 && y < height; x--, y++)
				{
					if ((array[y, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0)
					{
						objectByCorners.UlCorner = new Corner(x, y);
						brk = true;
						break;
					}
				}
			}

			// upper right
			brk = false;
			for (i = 0; i < width && i < height && brk == false; i++)
			{
				for (x = width - i - 1, y = 0; x < width && y < height; x++, y++)
				{
					if ((array[y, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0)
					{
						objectByCorners.UrCorner = new Corner(x, y);
						brk = true;
						break;
					}
				}
			}

			// lower left
			brk = false;
			for (i = 0; i < width && i < height && brk == false; i++)
			{
				for (x = i, y = height - 1; x >= 0 && y >= 0; x--, y--)
				{
					if ((array[y, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0)
					{
						objectByCorners.LlCorner = new Corner(x, y);
						brk = true;
						break;
					}
				}
			}

			// lower right
			brk = false;
			for (i = 0; i < width && i < height && brk == false; i++)
			{
				for (x = width - i - 1, y = height - 1; x < width && y >= 0; x++, y--)
				{
					if ((array[y, x / 8] & (byte)(0x80 >> (x & 0x07))) > 0)
					{
						objectByCorners.LrCorner = new Corner(x, y);
						brk = true;
						break;
					}
				}
			}

			return objectByCorners;
		}
		#endregion 

		#endregion
	
	}
}
