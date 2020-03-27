/*
 * BSD Licence:
 * Copyright (c) 2001, 2002 Ben Houston [ ben@exocortex.org ]
 * Exocortex Technologies [ www.exocortex.org ]
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, 
 * this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright 
 * notice, this list of conditions and the following disclaimer in the 
 * documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the <ORGANIZATION> nor the names of its contributors
 * may be used to endorse or promote products derived from this software
 * without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE FOR
 * ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
 * DAMAGE.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

//using Exocortex.Imaging;

namespace ImageProcessing.Transforms
{

	// Comments? Questions? Bugs? Tell Ben Houston at ben@exocortex.org
	// Version: May 4, 2002

	/// <summary>
	/// <p>Static functions for doing various Fourier Operations.</p>
	/// </summary>
	public class Fourier 
	{
		ComplexMap			map;

		Size				bitmapSize = Size.Empty;


		private const int	cMaxLength = 4096;
		private const int	cMinLength	= 1;

		private const int	cMaxBits	= 12;
		private const int	cMinBits	= 0;

		private static int[][] _reverseBits = null;

		private static int			_lookupTabletLength = -1;
		private static double[,][]	_uRLookup = null;
		private static double[,][]	_uILookup = null;
		private static float[,][]	_uRLookupF = null;
		private static float[,][]	_uILookupF = null;

		static private bool			_bufferCFLocked = false;
		static private Complex[]	_bufferCF = new Complex[0];

		//events
		public ImageProcessing.ProgressHnd ProgressChanged;


		#region constructor
		public Fourier() 
		{
		}
		#endregion

		#region enum FourierDirection
		public enum FourierDirection : int
		{
			/// <summary>
			/// Forward direction.  Usually in reference to moving from temporal
			/// representation to frequency representation
			/// </summary>
			Forward = 1,
			/// <summary>
			/// Backward direction. Usually in reference to moving from frequency
			/// representation to temporal representation
			/// </summary>
			Backward = -1,
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
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			this.bitmapSize = bitmap.Size;
			this.map = GetFourierMap(bitmap);

#if DEBUG
			Console.WriteLine("FFT 1: " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif
			FFT2(this.map, FourierDirection.Forward);

#if DEBUG
			Console.WriteLine("FFT 2: " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif
			float scale = 1f / (float)Math.Sqrt(this.map.Width * this.map.Height);

			for (int y = 0; y < this.map.Height; y++)
				for (int x = 0; x < this.map.Width; x++)
				{
					this.map[y, x].Re *= scale;
					this.map[y, x].Im *= scale;
				}
#if DEBUG
			Console.WriteLine("FFT 3: " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif
		}
		#endregion

		#region GetBitmap()
		public Bitmap GetBitmap()
		{
			Bitmap source = null;
			BitmapData sourceData = null;

			FFT2(this.map, FourierDirection.Backward);

			//even indexes are negative
			for (int y = 0; y < this.map.Height; y++)
			{
				Complex[] line = map.Lines[y];

				for (int x = (((y & 0x01) == 0) ? 1 : 0); x < this.map.Width; x = x + 2)
					line[x].Re = -line[x].Re;
			}

			//scale
			float scale = 1f / (float)Math.Sqrt(this.map.Width * this.map.Height);

			for (int y = 0; y < this.map.Height; y++)
				for (int x = 0; x < this.map.Width; x++)
				{
					this.map[y, x].Re *= scale;
					this.map[y, x].Im *= scale;
				}

			int width = this.bitmapSize.Width;
			int height = this.bitmapSize.Height;

			try
			{
				source = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				source.Palette = Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

				int stride = sourceData.Stride;
				int x, y;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();

					for (y = 0; y < height; y++)
					{
						Complex[] line = this.map.Lines[y];

						for (x = 0; x < width; x++)
							pSource[y * stride + x] = (byte) Math.Max(0, Math.Min(255, line[x].Re * 256.0));
					}
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
			}

			return source;
		}
		#endregion

		#region DespeckleAverage3x3()
		/// <summary>
		///  1 1 1
		///  1 1 1
		///  1 1 1
		/// </summary>
		public void DespeckleAverage3x3()
		{
			int width = this.map.Width;
			int height = this.map.Height;
			ComplexMap copy = new ComplexMap(width, height);

			for (int y = 1; y < height - 1; y++)
			{
				for (int x = 1; x < width - 1; x++)
				{
					Complex[] line1 = this.map.GetLine(y - 1);
					Complex[] line2 = this.map.GetLine(y);
					Complex[] line3 = this.map.GetLine(y + 1);

					copy[y, x] = new Complex((line1[x - 1].Re + line1[x].Re + line1[x + 1].Re +
						line2[x - 1].Re + line2[x].Re + line2[x + 1].Re +
						line3[x - 1].Re + line3[x].Re + line3[x + 1].Re) / 9.0F, 
						line2[x].Im);
				}
			}

			//first and last row
			for (int x = 0; x < width; x++)
			{
				copy[0, x] = new Complex(this.map[0, x].Re, this.map[0, x].Im);
				copy[height - 1, x] = new Complex(this.map[height - 1, x].Re, this.map[height - 1, x].Im);
			}

			//first and last column
			for (int y = 0; y < height; y++)
			{
				copy[y, 0] = new Complex(this.map[y, 0].Re, this.map[y, 0].Im);
				copy[y, width - 1] = new Complex(this.map[y, width - 1].Re, this.map[y, width - 1].Im);
			}

			this.map = copy;
		}
		#endregion

		#region DespeckleLaplacian3x3()
		/// <summary>
		///  0  -1  0
		/// -1  +4 -1
		///  0  -1  0
		/// </summary>
		public void DespeckleLaplacian3x3()
		{
			int width = this.map.Width;
			int height = this.map.Height;
			ComplexMap copy = new ComplexMap(width, height);

			for (int y = 1; y < height - 1; y++)
			{
				for (int x = 1; x < width - 1; x++)
				{
					Complex[] line1 = this.map.GetLine(y - 1);
					Complex[] line2 = this.map.GetLine(y);
					Complex[] line3 = this.map.GetLine(y + 1);

					copy[y, x] = new Complex(-line1[x].Re 
						- line2[x - 1].Re + 4 * line2[x].Re - line2[x + 1].Re 
						- line3[x].Re,
						line2[x].Im);
				}
			}

			//first and last row
			for (int x = 0; x < width; x++)
			{
				copy[0, x] = new Complex(this.map[0, x].Re, this.map[0, x].Im);
				copy[height - 1, x] = new Complex(this.map[height - 1, x].Re, this.map[height - 1, x].Im);
			}

			//first and last column
			for (int y = 0; y < height; y++)
			{
				copy[y, 0] = new Complex(this.map[y, 0].Re, this.map[y, 0].Im);
				copy[y, width - 1] = new Complex(this.map[y, width - 1].Re, this.map[y, width - 1].Im);
			}

			this.map = copy;
		}
		#endregion

		#region DespeckleLaplacian5x5()
		/// <summary>
		///  0  0  1  0  0
		///  0  1  2  1  0
		///  1  2 16  2  1
		///  0  1  2  1  0
		///  0  0  1  0  0
		/// </summary>
		public void DespeckleLaplacian5x5()
		{
			int width = this.map.Width;
			int height = this.map.Height;
			ComplexMap copy = new ComplexMap(width, height);

			for (int y = 2; y < height - 2; y++)
			{
				for (int x = 2; x < width - 2; x++)
				{
					Complex[] line1 = this.map.GetLine(y - 2);
					Complex[] line2 = this.map.GetLine(y - 1);
					Complex[] line3 = this.map.GetLine(y);
					Complex[] line4 = this.map.GetLine(y + 1);
					Complex[] line5 = this.map.GetLine(y + 2);

					copy[y, x] = new Complex(
						(+line1[x].Re
						+ line2[x - 1].Re + 2 * line2[x].Re + line2[x + 1].Re
						+ line3[x - 2].Re + 2 * line3[x - 1].Re + 16 * line3[x].Re + 2 * line3[x + 1].Re + line3[x + 2].Re
						+ line4[x - 1].Re + 2 * line4[x].Re + line4[x + 1].Re
						+ line5[x].Re)
						/ 16, line3[x].Im);
				}
			}

			//first and last rows
			for (int x = 0; x < width; x++)
			{
				copy[0, x] = new Complex(this.map[0, x].Re, this.map[0, x].Im);
				copy[1, x] = new Complex(this.map[1, x].Re, this.map[1, x].Im);
				copy[height - 2, x] = new Complex(this.map[height - 2, x].Re, this.map[height - 2, x].Im);
				copy[height - 1, x] = new Complex(this.map[height - 1, x].Re, this.map[height - 1, x].Im);
			}

			//first and last columns
			for (int y = 0; y < height; y++)
			{
				copy[y, 0] = new Complex(this.map[y, 0].Re, this.map[y, 0].Im);
				copy[y, 1] = new Complex(this.map[y, 1].Re, this.map[y, 1].Im);
				copy[y, width - 2] = new Complex(this.map[y, width - 2].Re, this.map[y, width - 2].Im);
				copy[y, width - 1] = new Complex(this.map[y, width - 1].Re, this.map[y, width - 1].Im);
			}

			this.map = copy;
		}
		#endregion

		#region DespeckleMexicanHat13x13()
		/// <summary>
		///  0   0   0   0   0  -1  -1  -1   0   0   0   0   0 
		///  0   0   0  -1  -1  -2  -2  -2  -1  -1   0   0   0 
		///  0   0  -2  -2  -3  -3  -4  -3  -3  -2  -2   0   0 
		///  0  -1  -2  -3  -3  -3  -2  -3  -3  -3  -2  -1   0 
		///  0  -1  -3  -3  -1   4   6   4  -1  -3  -3  -1   0 
		/// -1  -2  -3  -3   4  14  19  14   4  -3  -3  -2  -1 
		/// -1  -2  -4  -2   6  19  24  19   6  -2  -4  -2  -1 
		/// -1  -2  -3  -3   4  14  19  14   4  -3  -3  -2  -1 
		///  0  -1  -3  -3  -1   4   6   4  -1  -3  -3  -1   0 
		///  0  -1  -2  -3  -3  -3  -2  -3  -3  -3  -2  -1   0 
		///  0   0  -2  -2  -3  -3  -4  -3  -3  -2  -2   0   0 
		///  0   0   0  -1  -1  -2  -2  -2  -1  -1   0   0   0 
		///  0   0   0   0   0  -1  -1  -1   0   0   0   0   0 
		/// </summary>
		/*public void DespeckleMexicanHat13x13()
		{
			int width = this.map.Width;
			int height = this.map.Height;
			ComplexMap copy = new ComplexMap(width, height);
			int[,] array = new int[,]{
				{ 0, 0, 0, 0, 0,-1,-1,-1, 0, 0, 0, 0, 0},
				{ 0, 0, 0,-1,-1,-2,-2,-2,-1,-1, 0, 0, 0},
				{ 0, 0,-2,-2,-3,-3,-4,-3,-3,-2,-2, 0, 0},
				{ 0,-1,-2,-3,-3,-3,-2,-3,-3,-3,-2,-1, 0},
				{ 0,-1,-3,-3,-1, 4, 6, 4,-1,-3,-3,-1, 0},
				{-1,-2,-3,-3, 4,14,19,14, 4,-3,-3,-2,-1},
				{-1,-2,-4,-2, 6,19,24,19, 6,-2,-4,-2,-1},
				{-1,-2,-3,-3, 4,14,19,14, 4,-3,-3,-2,-1},
				{ 0,-1,-3,-3,-1, 4, 6, 4,-1,-3,-3,-1, 0},
				{ 0,-1,-2,-3,-3,-3,-2,-3,-3,-3,-2,-1, 0},
				{ 0, 0,-2,-2,-3,-3,-4,-3,-3,-2,-2, 0, 0},
				{ 0, 0, 0,-1,-1,-2,-2,-2,-1,-1, 0, 0, 0},
				{ 0, 0, 0, 0, 0,-1,-1,-1, 0, 0, 0, 0, 0}
				};

			for (int y = 6; y < height - 6; y++)
			{
				for (int x = 6; x < width - 6; x++)
				{
					float sum = 0;

					for (int yA = -6; yA <= 6; yA++)
					{
						Complex[] line = this.map.GetLine(y + yA);

						for (int xA = -6; xA <= 6; xA++)
							sum += array[yA + 6, xA + 6] * line[x + xA].Re;
					}

					copy[y, x] = new Complex(sum, this.map[y, x].Im);
				}
			}
	
			//first and last rows
			for (int y = 0; y < 6; y++)
				for (int x = 0; x < width; x++)
				{
					copy[y, x] = new Complex(this.map[y, x].Re, this.map[y, x].Im);
					copy[height - y - 1, x] = new Complex(this.map[height - y - 1, x].Re, this.map[height - y - 1, x].Im);
				}

			//first and last columns
			for (int x = 0; x < 6; x++)
				for (int y = 0; y < height; y++)
				{
					copy[y, x] = new Complex(this.map[y, x].Re, this.map[y, x].Im);
					copy[y, width - 1 - x] = new Complex(this.map[y, width - 1 - x].Re, this.map[y, width - 1 - x].Im);
				}

			this.map = copy;
		}*/
		#endregion

		#region DespeckleMexicanHat13x13()
		/// <summary>
		///  0   0   0   0   0  -1  -1  -1   0   0   0   0   0 
		///  0   0   0  -1  -1  -2  -2  -2  -1  -1   0   0   0 
		///  0   0  -2  -2  -3  -3  -4  -3  -3  -2  -2   0   0 
		///  0  -1  -2  -3  -3  -3  -2  -3  -3  -3  -2  -1   0 
		///  0  -1  -3  -3  -1   4   6   4  -1  -3  -3  -1   0 
		/// -1  -2  -3  -3   4  14  19  14   4  -3  -3  -2  -1 
		/// -1  -2  -4  -2   6  19  24  19   6  -2  -4  -2  -1 
		/// -1  -2  -3  -3   4  14  19  14   4  -3  -3  -2  -1 
		///  0  -1  -3  -3  -1   4   6   4  -1  -3  -3  -1   0 
		///  0  -1  -2  -3  -3  -3  -2  -3  -3  -3  -2  -1   0 
		///  0   0  -2  -2  -3  -3  -4  -3  -3  -2  -2   0   0 
		///  0   0   0  -1  -1  -2  -2  -2  -1  -1   0   0   0 
		///  0   0   0   0   0  -1  -1  -1   0   0   0   0   0 
		/// </summary>
		public void DespeckleMexicanHat13x13()
		{
			int width = this.map.Width;
			int height = this.map.Height;
			ComplexMap copy = new ComplexMap(width, height);
			int[,] array = new int[,]{
				{ 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0},
				{ 0, 0, 0, 1, 1, 2, 2, 2, 1, 1, 0, 0, 0},
				{ 0, 0, 2, 2, 3, 3, 4, 3, 3, 2, 2, 0, 0},
				{ 0, 1, 2, 3, 3, 3, 2, 3, 3, 3, 2, 1, 0},
				{ 0, 1, 3, 3, 1, 4, 6, 4, 1, 3, 3, 1, 0},
				{ 1, 2, 3, 3, 4,14,19,14, 4, 3, 3, 2, 1},
				{ 1, 2, 4, 2, 6,19,24,19, 6, 2, 4, 2, 1},
				{ 1, 2, 3, 3, 4,14,19,14, 4, 3, 3, 2, 1},
				{ 0, 1, 3, 3, 1, 4, 6, 4, 1, 3, 3, 1, 0},
				{ 0, 1, 2, 3, 3, 3, 2, 3, 3, 3, 2, 1, 0},
				{ 0, 0, 2, 2, 3, 3, 4, 3, 3, 2, 2, 0, 0},
				{ 0, 0, 0, 1, 1, 2, 2, 2, 1, 1, 0, 0, 0},
				{ 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0}
				};

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					float sum = 0;
					int divider = 0;

					for (int yA = -6; yA <= 6; yA++)
					{
						if(y + yA >= 0 && y + yA < height)
						{
						Complex[] line = this.map.GetLine(y + yA);

						for (int xA = -6; xA <= 6; xA++)
							if (x + xA >= 0 && x + xA < width)
							{
								sum += array[yA + 6, xA + 6] * line[x + xA].Re;
								divider += array[yA + 6, xA + 6];
							}
						}
					}

					copy[y, x] = new Complex(sum / divider, this.map[y, x].Im);
				}
			}

			this.map = copy;
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
										pSource[y * stride + x] = (byte)Math.Max(0, Math.Min(255, 256 * Math.Abs(line[x].Re)));
								} break;
							case DrawingComponent.Imaginary:
								{
									for (x = 0; x < width; x++)
										pSource[y * stride + x] = (byte)Math.Max(0, Math.Min(255, 256 * Math.Abs(line[x].Im)));
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
			ComplexMap map = new ComplexMap();
			BitmapData sourceData = null;

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
					return (int)Math.Pow(2, i);

			return (int)Math.Pow(2, 31);
		}
		#endregion

		#region FFT()
		/// <summary>
		/// Compute a 1D fast Fourier transform of a dataset of complex numbers.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="length"></param>
		/// <param name="direction"></param>
		private static void FFT(Complex[] data, int length, FourierDirection direction)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			if (data.Length < length)
			{
				throw new ArgumentOutOfRangeException("length", length, "must be at least as large as 'data.Length' parameter");
			}
			if (Fourier.IsPowerOf2(length) == false)
			{
				throw new ArgumentOutOfRangeException("length", length, "must be a power of 2");
			}

			Fourier.SyncLookupTableLength(length);

			int ln = Fourier.Log2(length);

			// reorder array
			Fourier.ReorderArray(data);

			// successive doubling
			int N = 1;
			int signIndex = (direction == FourierDirection.Forward) ? 0 : 1;

			for (int level = 1; level <= ln; level++)
			{
				int M = N;
				N <<= 1;

				float[] uRLookup = _uRLookupF[level, signIndex];
				float[] uILookup = _uILookupF[level, signIndex];

				for (int j = 0; j < M; j++)
				{
					float uR = uRLookup[j];
					float uI = uILookup[j];

					for (int even = j; even < length; even += N)
					{
						int odd = even + M;

						float r = data[odd].Re;
						float i = data[odd].Im;

						float odduR = r * uR - i * uI;
						float odduI = r * uI + i * uR;

						r = data[even].Re;
						i = data[even].Im;

						data[even].Re = r + odduR;
						data[even].Im = i + odduI;

						data[odd].Re = r - odduR;
						data[odd].Im = i - odduI;
					}
				}
			}

		}
		#endregion

		#region FFT_Quick()
		/// <summary>
		/// Compute a 1D fast Fourier transform of a dataset of complex numbers.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="length"></param>
		/// <param name="direction"></param>
		private static void FFT_Quick(Complex[] data, int length, FourierDirection direction)
		{
			/*if( data == null ) {
				throw new ArgumentNullException( "data" );
			}
			if( data.Length < length ) {
				throw new ArgumentOutOfRangeException( "length", length, "must be at least as large as 'data.Length' parameter" );
			}
			if( Fourier.IsPowerOf2( length ) == false ) {
				throw new ArgumentOutOfRangeException( "length", length, "must be a power of 2" );
			}

			Fourier.SyncLookupTableLength( length );*/

			int ln = Fourier.Log2(length);

			// reorder array
			Fourier.ReorderArray(data);

			// successive doubling
			int N = 1;
			int signIndex = (direction == FourierDirection.Forward) ? 0 : 1;

			for (int level = 1; level <= ln; level++)
			{
				int M = N;
				N <<= 1;

				float[] uRLookup = _uRLookupF[level, signIndex];
				float[] uILookup = _uILookupF[level, signIndex];

				for (int j = 0; j < M; j++)
				{
					float uR = uRLookup[j];
					float uI = uILookup[j];

					for (int even = j; even < length; even += N)
					{
						int odd = even + M;

						float r = data[odd].Re;
						float i = data[odd].Im;

						float odduR = r * uR - i * uI;
						float odduI = r * uI + i * uR;

						r = data[even].Re;
						i = data[even].Im;

						data[even].Re = r + odduR;
						data[even].Im = i + odduI;

						data[odd].Re = r - odduR;
						data[odd].Im = i - odduI;
					}
				}
			}

		}
		#endregion

		#region FFT()
		/// <summary>
		/// Compute a 1D fast Fourier transform of a dataset of complex numbers.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="direction"></param>
		private static void FFT(Complex[] data, FourierDirection direction)
		{
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			Fourier.FFT(data, data.Length, direction);
		}
		#endregion

		#region FFT2()
		/// <summary>
		/// Compute a 2D fast fourier transform on a data set of complex numbers
		/// </summary>
		/// <param name="data"></param>
		/// <param name="xLength"></param>
		/// <param name="yLength"></param>
		/// <param name="direction"></param>
		private static void FFT2(ComplexMap data, FourierDirection direction)
		{
			int width = data.Width;
			int height = data.Height;
			
			if (data == null)
				throw new ArgumentNullException("data");

			if (Fourier.IsPowerOf2(width) == false)
				throw new ArgumentOutOfRangeException("Width ", width, " must be a power of 2");

			if (Fourier.IsPowerOf2(height) == false)
				throw new ArgumentOutOfRangeException("Height ", height, " must be a power of 2");

			if (width > 1)
			{
				Fourier.SyncLookupTableLength(width);
				for (int y = 0; y < height; y++)
				{
					Fourier.FFT(data.Lines[y], width, direction);
				}
			}

			if (height > 1)
			{
				Fourier.SyncLookupTableLength(height);
				for (int x = 0; x < width; x++)
				{
					Complex[] column = new Complex[height];

					for (int y = 0; y < height; y++)
						column[y] = data[y, x];

					Fourier.FFT(column, height, direction);

					for (int y = 0; y < height; y++)
						data[y, x] = column[y];

				}
			}
		}
		#endregion

		#region Swap()
		static private void Swap(ref float a, ref float b) 
		{
			float temp = a;
			a = b;
			b = temp;
		}

		static private void Swap( ref double a, ref double b ) 
		{
			double temp = a;
			a = b;
			b = temp;
		}

		static private void Swap( ref Complex a, ref Complex b ) 
		{
			Complex temp = a;
			a = b;
			b = temp;
		}
		#endregion

		#region IsPowerOf2()
		static private bool	IsPowerOf2( int x ) 
		{
			return	(x & (x - 1)) == 0;
		}
		#endregion

		#region Pow2()
		static private int Pow2(int exponent) 
		{
			if( exponent >= 0 && exponent < 31 ) 
				return	1 << exponent;
			
			return	0;
		}
		#endregion

		#region Log2()
		static private int Log2(int x) 
		{
			if( x <= 65536 )
			{
				if( x <= 256 ) 
				{
					if( x <= 16 ) 
					{
						if( x <= 4 ) 
						{	
							if( x <= 2 ) 
							{
								if( x <= 1 ) 
								{
									return	0;
								}
								return	1;	
							}
							return	2;				
						}
						if( x <= 8 )
							return	3;			
						return	4;				
					}
					if( x <= 64 ) 
					{
						if( x <= 32 )
							return	5;	
						return	6;				
					}
					if( x <= 128 )
						return	7;		
					return	8;				
				}
				if( x <= 4096 ) 
				{	
					if( x <= 1024 )
					{	
						if( x <= 512 )
							return	9;			
						return	10;				
					}
					if( x <= 2048 )
						return	11;			
					return	12;				
				}
				if( x <= 16384 )
				{
					if( x <= 8192 )
						return	13;			
					return	14;				
				}
				if( x <= 32768 )
					return	15;	
				return	16;	
			}
			if( x <= 16777216 ) 
			{
				if( x <= 1048576 ) 
				{
					if( x <= 262144 ) 
					{	
						if( x <= 131072 )
							return	17;			
						return	18;				
					}
					if( x <= 524288 )
						return	19;			
					return	20;				
				}
				if( x <= 4194304 ) {
					if( x <= 2097152 )
						return	21;	
					return	22;				
				}
				if( x <= 8388608 )
					return	23;		
				return	24;				
			}
			if( x <= 268435456 ) 
			{	
				if( x <= 67108864 ) 
				{	
					if( x <= 33554432 )
						return	25;			
					return	26;				
				}
				if( x <= 134217728 )
					return	27;			
				return	28;				
			}
			if( x <= 1073741824 )
			{
				if( x <= 536870912 )
					return	29;			
				return	30;				
			}

			return	31;
		}
		#endregion

		#region ReverseBits()
		/*static private int ReverseBits(int index, int numberOfBits)
		{
			System.Diagnostics.Debug.Assert( numberOfBits >= cMinBits );
			System.Diagnostics.Debug.Assert( numberOfBits <= cMaxBits );

			int reversedIndex = 0;
			for( int i = 0; i < numberOfBits; i ++ ) 
			{
				reversedIndex = ( reversedIndex << 1 ) | ( index & 1 );
				index = ( index >> 1 );
			}

			return reversedIndex;
		}*/
		#endregion
		
		#region GetReversedBits()
		static private int[] GetReversedBits(int numberOfBits)
		{
			System.Diagnostics.Debug.Assert( numberOfBits >= cMinBits );
			System.Diagnostics.Debug.Assert( numberOfBits <= cMaxBits );

			int[][] _reversedBits = new int[cMaxBits][];

			if( _reversedBits[ numberOfBits - 1 ] == null ) 
			{
				int		maxBits = Fourier.Pow2( numberOfBits );
				int[]	reversedBits = new int[ maxBits ];
				
				for( int i = 0; i < maxBits; i ++ ) 
				{
					int oldBits = i;
					int newBits = 0;

					for( int j = 0; j < numberOfBits; j ++ )
					{
						newBits = ( newBits << 1 ) | ( oldBits & 1 );
						oldBits = ( oldBits >> 1 );
					}

					reversedBits[ i ] = newBits;
				}

				_reversedBits[ numberOfBits - 1 ] = reversedBits;
			}
			
			return	_reversedBits[ numberOfBits - 1 ];
		}
		#endregion

		#region ReorderArray()
		static private void ReorderArray(Complex[] data)
		{
			System.Diagnostics.Debug.Assert( data != null );

			int length = data.Length;
			
			System.Diagnostics.Debug.Assert( Fourier.IsPowerOf2( length ) == true );
			System.Diagnostics.Debug.Assert( length >= cMinLength );
			System.Diagnostics.Debug.Assert( length <= cMaxLength );

			int[] reversedBits = Fourier.GetReversedBits( Fourier.Log2( length ) );

			for( int i = 0; i < length; i ++ ) 
			{
				int swap = reversedBits[ i ];
				if( swap > i ) 
				{
					Complex temp = data[ i ];
					data[ i ] = data[ swap ];
					data[ swap ] = temp;
				}
			}
		}
		#endregion

		#region _ReverseBits()
		private static int _ReverseBits(int bits, int n)
		{
			int bitsReversed = 0;

			for (int i = 0; i < n; i++)
			{
				bitsReversed = (bitsReversed << 1) | (bits & 1);
				bits = (bits >> 1);
			}

			return bitsReversed;
		}
		#endregion

		#region InitializeReverseBits()
		private static void InitializeReverseBits(int levels)
		{
			_reverseBits = new int[levels + 1][];

			for( int j = 0; j < ( levels + 1 ); j ++ ) 
			{
				int count = (int) Math.Pow( 2, j );
				_reverseBits[j] = new int[ count ];
				
				for( int i = 0; i < count; i ++ ) 
				{
					_reverseBits[j][i] = _ReverseBits( i, j );
				}
			}
		}
		#endregion

		#region SyncLookupTableLength()
		private static void SyncLookupTableLength(int length)
		{
			System.Diagnostics.Debug.Assert( length < 1024*10 );
			System.Diagnostics.Debug.Assert( length >= 0 );

			if( length > _lookupTabletLength ) 
			{
				int level = (int) Math.Ceiling( Math.Log( length, 2 ) );
				Fourier.InitializeReverseBits( level );
				Fourier.InitializeComplexRotations( level );
				//_cFFTDataF	= new Complex[ Math2.CeilingBase( length, 2 ) ];
				_lookupTabletLength = length;
			}
		}
		#endregion

		#region GetLookupTableLength()
		private static int GetLookupTableLength()
		{
			return	_lookupTabletLength;
		}
		#endregion

		#region ClearLookupTables()
		private static void ClearLookupTables()
		{
			_uRLookup	= null;
			_uILookup	= null;
			_uRLookupF	= null;
			_uILookupF	= null;
			_lookupTabletLength	= -1;
		}
		#endregion

		#region InitializeComplexRotations()
		private static void InitializeComplexRotations(int levels)
		{
			int ln = levels;
			//_wRLookup = new float[ levels + 1, 2 ];
			//_wILookup = new float[ levels + 1, 2 ];
			
			_uRLookup = new double[ levels + 1, 2 ][];
			_uILookup = new double[ levels + 1, 2 ][];

			_uRLookupF = new float[ levels + 1, 2 ][];
			_uILookupF = new float[ levels + 1, 2 ][];

			int N = 1;
			for( int level = 1; level <= ln; level ++ ) {
				int M = N;
				N <<= 1;

				//float scale = (float)( 1 / Math.Sqrt( 1 << ln ) );

				// positive sign ( i.e. [M,0] )
				{
					double	uR = 1;
					double	uI = 0;
					double	angle = (double) Math.PI / M * 1;
					double	wR = (double) Math.Cos( angle );
					double	wI = (double) Math.Sin( angle );

					_uRLookup[level,0] = new double[ M ];
					_uILookup[level,0] = new double[ M ];
					_uRLookupF[level,0] = new float[ M ];
					_uILookupF[level,0] = new float[ M ];

					for( int j = 0; j < M; j ++ ) {
						_uRLookupF[level,0][j] = (float)( _uRLookup[level,0][j] = uR );
						_uILookupF[level,0][j] = (float)( _uILookup[level,0][j] = uI );
						double	uwI = uR*wI + uI*wR;
						uR = uR*wR - uI*wI;
						uI = uwI;
					}
				}
				{


				// negative sign ( i.e. [M,1] )
					double	uR = 1;
                    double	uI = 0;
					double	angle = (double) Math.PI / M * -1;
					double	wR = (double) Math.Cos( angle );
					double	wI = (double) Math.Sin( angle );

					_uRLookup[level,1] = new double[ M ];
					_uILookup[level,1] = new double[ M ];
					_uRLookupF[level,1] = new float[ M ];
					_uILookupF[level,1] = new float[ M ];

					for( int j = 0; j < M; j ++ ) {
						_uRLookupF[level,1][j] = (float)( _uRLookup[level,1][j] = uR );
						_uILookupF[level,1][j] = (float)( _uILookup[level,1][j] = uI );
						double	uwI = uR*wI + uI*wR;
						uR = uR*wR - uI*wI;
						uI = uwI;
					}
				}

			}
		}
		#endregion
		
		#region LockBufferCF()
		static private void LockBufferCF(int length, ref Complex[] buffer)
		{
			System.Diagnostics.Debug.Assert( length >= 0 );
			System.Diagnostics.Debug.Assert( _bufferCFLocked == false );
			
			_bufferCFLocked = true;

			if( length != _bufferCF.Length )
			{
				_bufferCF	= new Complex[ length ];
			}

			buffer =	_bufferCF;
		}
		#endregion

		#region UnlockBufferCF()
		static private void UnlockBufferCF(ref Complex[] buffer)
		{
			System.Diagnostics.Debug.Assert( _bufferCF == buffer );
			System.Diagnostics.Debug.Assert( _bufferCFLocked == true );
			
			_bufferCFLocked = false;
			buffer = null;
		}
		#endregion

		#region LinearFFT()
		private static void LinearFFT(Complex[] data, int start, int inc, int length, FourierDirection direction)
		{
			System.Diagnostics.Debug.Assert( data != null );
			System.Diagnostics.Debug.Assert( start >= 0 );
			System.Diagnostics.Debug.Assert( inc >= 1 );
			System.Diagnostics.Debug.Assert( length >= 1 );
			System.Diagnostics.Debug.Assert( ( start + inc * ( length - 1 ) ) < data.Length );
			
			// copy to buffer
			Complex[]	buffer = null;
			LockBufferCF( length, ref buffer );
			int j = start;
			for( int i = 0; i < length; i ++ ) {
				buffer[ i ] = data[ j ];
				j += inc;
			}

			FFT( buffer, length, direction );

			// copy from buffer
			j = start;
			for( int i = 0; i < length; i ++ ) {
				data[ j ] = buffer[ i ];
				j += inc;
			}
			UnlockBufferCF( ref buffer );
		}
		#endregion

		#region LinearFFT_Quick()
		private static void LinearFFT_Quick(Complex[] data, int start, int inc, int length, FourierDirection direction)
		{
			/*System.Diagnostics.Debug.Assert( data != null );
			System.Diagnostics.Debug.Assert( start >= 0 );
			System.Diagnostics.Debug.Assert( inc >= 1 );
			System.Diagnostics.Debug.Assert( length >= 1 );
			System.Diagnostics.Debug.Assert( ( start + inc * ( length - 1 ) ) < data.Length );	*/
			
			// copy to buffer
			Complex[]	buffer = null;
			LockBufferCF( length, ref buffer );
			int j = start;

			for( int i = 0; i < length; i ++ ) 
			{
				buffer[ i ] = data[ j ];
				j += inc;
			}

			FFT( buffer, length, direction );

			// copy from buffer
			j = start;
			for( int i = 0; i < length; i ++ ) 
			{
				data[ j ] = buffer[ i ];
				j += inc;
			}

			UnlockBufferCF( ref buffer );
		}
		#endregion

		#endregion

	}
}
