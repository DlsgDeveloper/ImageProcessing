using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	public class ComplexMap
	{
		List<Complex[]> array = new List<Complex[]>();

		public ComplexMap()
		{
		}

		public ComplexMap(int width, int height)
		{			
			for(int i = 0; i < height; i++)
				array.Add(new Complex[width]);
		}

		//PUBLIC PROPERTIES
		#region public properties

		public int Width { get { return (array.Count > 0) ? array[0].Length : 0; } }
		public int Height { get { return array.Count; } }

		public Complex this[int y, int x]
		{
			get { return array[y][x]; }
			set { array[y][x] = value; }
		}

		public List<Complex[]> Lines
		{
			get { return array; }
		}

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region AddLine()
		public void AddLine(Complex[] line)
		{
			array.Add(line);
		}
		#endregion

		#region GetLine()
		public Complex[] GetLine(int lineIndex)
		{
			return array[lineIndex];
		}
		#endregion

		#region DrawRealToTheFile()
		public void DrawRealToTheFile(string filePath)
		{
			Bitmap source = null;
			BitmapData sourceData = null;

			try
			{
				source = new Bitmap(this.Width, this.Height, PixelFormat.Format8bppIndexed);				
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

				int stride = sourceData.Stride;
				int width = source.Width;
				int height = source.Height;
				int x, y;
				float maxNumber = 0; 

				// get biggest real absolute number
				foreach(Complex[] line in array)
				{
					for (int i = 0; i < line.Length; i++)
					{
						if (maxNumber < line[i].Re)
							maxNumber = line[i].Re;
						else if (maxNumber < -line[i].Re)
							maxNumber = -line[i].Re;
					}
				}

				maxNumber *= 255F;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();

					for (y = 0; y < height; y++)
					{
						Complex[] line = this.array[y];

						for (x = 0; x < width; x++)
							pSource[y * stride + x] = (byte)Math.Abs(line[x].Re / maxNumber);
					}
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);

				if (source != null)
					source.Save(filePath, ImageFormat.Png);
			}
		}
		#endregion
	
		#endregion
	}

}
