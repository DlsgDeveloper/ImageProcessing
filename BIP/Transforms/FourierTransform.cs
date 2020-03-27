using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	public class FourierTransform
	{
		ComplexMap map;

		//events
		public ImageProcessing.ProgressHnd ProgressChanged;


		#region constructor
		/// <summary>
		/// Creates fourier transform class
		/// </summary>
		public FourierTransform()
		{
		}
		#endregion

		#region enum DrawingComponent
		public enum DrawingComponent
		{
			Real,
			Imaginary,
			PowerSpectrum
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region LoadBitmap()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bitmap">8 bit grayscale image</param>
		public void LoadBitmap(Bitmap bitmap)
		{
			this.map = GetFourierMap(bitmap);

			ComputeFFTHorizontally(this.map);
			ComputeFFTVertically(this.map);

			float scale = 1f / (float)Math.Sqrt(this.map.Width * this.map.Height);

			for (int y = 0; y < this.map.Height; y++)
				for (int x = 0; x < this.map.Width; x++)
				{
					this.map[y, x].Re *= scale;
					this.map[y, x].Im *= scale;
				}
		}
		#endregion

		#region DrawRealToTheFile()
		public void DrawToFile(string filePath, DrawingComponent component)
		{
			Bitmap source = null;
			BitmapData sourceData = null;

			try
			{
				source = new Bitmap(this.map.Width, this.map.Height, PixelFormat.Format8bppIndexed);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

				int stride = sourceData.Stride;
				int width = source.Width;
				int height = source.Height;
				int x, y;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();

					for (y = 0; y < height; y++)
					{
						Complex[] line = this.map.Lines[y];

						switch (component)
						{
							case DrawingComponent.Real:
								{
									for (x = 0; x < width; x++)
										pSource[y * stride + x] = (byte)Math.Max(0, Math.Min(255, 255 * Math.Abs(line[x].Re)));
								} break;
							case DrawingComponent.Imaginary:
								{
									for (x = 0; x < width; x++)
										pSource[y * stride + x] = (byte)Math.Max(0, Math.Min(255, 255 * Math.Abs(line[x].Im)));
								} break;
							default:
								{
									for (x = 0; x < width; x++)
										pSource[y * stride + x] = (byte)Math.Max(0, Math.Min(255, 256 * Math.Sqrt(line[x].Re * line[x].Re + line[x].Im * line[x].Im)));
								} break;
						}
					}
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);

				source.Palette = Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);

				if (source != null)
					source.Save(filePath, ImageFormat.Png);
			}
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region GetFourierMap()
		private ComplexMap GetFourierMap(Bitmap source)
		{
			ComplexMap	map = new ComplexMap();
			BitmapData	sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

				int stride = sourceData.Stride;
				int width = source.Width;
				int height = source.Height;
				int x, y;
				int mapWidth = CeilingPowOf2(width);
				int mapHeight = CeilingPowOf2(height);

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();

					for (y = 0; y < height; y++)
					{
						Complex[] line = new Complex[mapWidth];

						for (x = 0; x < width; x++)
							line[x] = new Complex((float)(pSource[y * stride + x] / 256.0), 0F);

						// fill in the rest of the line with last pixel value
						for (x = width; x < mapWidth; x++)
							line[x] = new Complex(line[width - 1].Re, line[width - 1].Im);

						map.AddLine(line);
					}

					Complex[] lastDataLine = map.GetLine(height - 1);

					// add lines that are not on source
					for (y = height; y < mapHeight; y++)
					{
						Complex[] line = new Complex[mapWidth];

						for (x = 0; x < mapWidth; x++)
							line[x] = new Complex(lastDataLine[x].Re, lastDataLine[x].Im);

						map.AddLine(line);
					}
				}

				//even indexes are negative
				for (y = 0; y < mapHeight; y++)
				{
					Complex[] line = map.Lines[y];

					for (x = (((y & 0x01) == 0) ? 1 : 0); x < mapWidth; x = x + 2)
						line[x].Re = -line[x].Re;
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
			}

			return map;
		}
		#endregion

		#region CeilingPowOf2()
		/// <summary>
		/// returns smallest power of 2 equal or bigger than val. Example: for 5, it returns 8.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		private int CeilingPowOf2(int val)
		{
			for (int i = 0; i < 32; i++)
				if (val <= Math.Pow(2, i))
					return (int) Math.Pow(2, i);

			return (int) Math.Pow(2, 31);
		}
		#endregion

		#region GetExponentOf2()
		/// <summary>
		/// returns exponent of 2. Example: for 32, it returns 6. Val must be power of 2.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		private int GetExponentOf2(int val)
		{
			for (int i = 0; i < 32; i++)
				if (((val >> i) & 0x01) == 0x01)
					return i;

			return 31;
		}
		#endregion

		#region ComputeFFTHorizontally()
		private void ComputeFFTHorizontally(ComplexMap map)
		{
			List<Complex[]> lines = map.Lines;

			foreach (Complex[] line in lines)
			{
				FFT(line);
			}
		}
		#endregion

		#region ComputeFFTVertically()
		private void ComputeFFTVertically(ComplexMap map)
		{
			int width = map.Width;
			int height = map.Height;

			for (int x = 0; x < width; x++)
			{
				Complex[] row = new Complex[height];

				for (int y = 0; y < height; y++)
					row[y] = map[y, x];

				FFT(row);

				for (int y = 0; y < height; y++)
					map[y, x] = row[y];
			}
		}
		#endregion

		#region FFT()
		private void FFT(Complex[] line)
		{
			int length = line.Length;
			int exponent = GetExponentOf2(length);
			int i, j, k, n1, n2;
			double c, s, e, a, t1, t2;

			// bit-reverse
			j = 0; 
			n2 = length / 2;

			for (i = 1; i < length - 1; i++)
			{
				n1 = n2;
				
				while (j >= n1)
				{
					j = j - n1;
					n1 = n1 / 2;
				}
				
				j = j + n1;
				
				if (i < j)
				{
					t1 = line[i].Re;
					line[i].Re = line[j].Re;
					line[j].Re = (float)t1;
					t1 = line[i].Im;
					line[i].Im = line[j].Im;
					line[j].Im = (float)t1;
				}
			}

			//FFT
			n1 = 0; 
			n2 = 1;

			for (i = 0; i < exponent; i++)
			{
				n1 = n2;
				n2 = n2 + n2;
				e = -6.283185307179586 / n2;
				a = 0.0;

				for (j = 0; j < n1; j++)
				{
					c = Math.Cos(a);
					s = Math.Sin(a);
					a = a + e;

					for (k = j; k < length; k = k + n2)
					{
						t1 = c * line[k + n1].Re - s * line[k + n1].Im;
						t2 = s * line[k + n1].Re + c * line[k + n1].Im;
						line[k + n1].Re = (float)(line[k].Re - t1);
						line[k + n1].Im = (float)(line[k].Im - t2);
						line[k].Re = (float)(line[k].Re + t1);
						line[k].Im = (float)(line[k].Im + t2);
					}
				}
			}
		}
		#endregion
	
		#region FFT()
		private void FFT(int n, int m, double[] x, double[] y)
		{
			int i, j, k, n1, n2;
			double c, s, e, a, t1, t2;

			j = 0; /* bit-reverse */
			n2 = n / 2;

			for (i = 1; i < n - 1; i++)
			{
				n1 = n2;
				while (j >= n1)
				{
					j = j - n1;
					n1 = n1 / 2;
				}
				j = j + n1;
				if (i < j)
				{
					t1 = x[i];
					x[i] = x[j];
					x[j] = t1;
					t1 = y[i];
					y[i] = y[j];
					y[j] = t1;
				}
			}

			n1 = 0; /* FFT */
			n2 = 1;

			for (i = 0; i < m; i++)
			{
				n1 = n2;

				n2 = n2 + n2;
				e = -6.283185307179586 / n2;
				a = 0.0;
				for (j = 0; j < n1; j++)
				{
					c = Math.Cos(a);
					s = Math.Sin(a);
					a = a + e;

					for (k = j; k < n; k = k + n2)
					{
						t1 = c * x[k + n1] - s * y[k + n1];
						t2 = s * x[k + n1] + c * y[k + n1];
						x[k + n1] = x[k] - t1;
						y[k + n1] = y[k] - t2;
						x[k] = x[k] + t1;
						y[k] = y[k] + t2;
					}
				}
			}
		}
		#endregion

		#endregion

	}
}
