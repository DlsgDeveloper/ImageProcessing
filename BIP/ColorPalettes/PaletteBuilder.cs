using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.ColorPalettes
{
	/// <summary>
	/// Summary description for PaletteBuilder.
	/// </summary>
	public class PaletteBuilder
	{
		int vBoxSize = 8;

		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		

		#region constructor
		public PaletteBuilder()
		{
		}
		#endregion

		//	PUBLIC PROPERTIES
		#region public properties

		#endregion

		
		//	PUBLIC METHODS
		#region public methods

		#region GetPalette256()
		public Color[] GetPalette256(ImageProcessing.BigImages.ItDecoder itDecoder)
		{
			switch (itDecoder.PixelFormat)
			{
				case PixelFormat.Format32bppRgb:
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format24bppRgb:
					uint[, ,]	quantizationCube = GetQuantizationCube(itDecoder);
					Color[]		colors = DoMedianCut(quantizationCube);
					return colors;
				case PixelFormat.Format8bppIndexed:
				case PixelFormat.Format4bppIndexed:
				case PixelFormat.Format1bppIndexed:
					ColorPalette palette = itDecoder.GetPalette();
					if (palette != null)
						return palette.Entries;
					else
						return null;
				default:
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}
		}
		#endregion

		#region GetPalette256()
		public Color[] GetPalette256(Bitmap bitmap)
		{
			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format32bppRgb:
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format24bppRgb:
				case PixelFormat.Format8bppIndexed:
				case PixelFormat.Format4bppIndexed:
					uint[, ,] quantizationCube = GetQuantizationCube(bitmap);
					Color[] colors = DoMedianCut(quantizationCube);
					return colors;
				case PixelFormat.Format1bppIndexed:
					return Misc.GrayscalePalette.Entries;
				default:
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}
		}
		#endregion

		#region GetInversePalette32x32x32()
		public static byte[, ,] GetInversePalette32x32x32(Color[] palette)
		{
			byte[, ,] array = new byte[32, 32, 32];

/*#if DEBUG
			DateTime start = DateTime.Now;
#endif*/

			for (int r = 0; r < 32; r++)
				for (int g = 0; g < 32; g++)
					for (int b = 0; b < 32; b++)
					{
						int rX = r * 8 + 4;
						int gX = g * 8 + 4;
						int bX = b * 8 + 4;
						byte bestPaletteRepresentation = 0;

						double shortestDistance = double.MaxValue;

						for (int i = 0; i < palette.Length; i++)
						{
							double distance = (rX - palette[i].R) * (rX - palette[i].R) + (gX - palette[i].G) * (gX - palette[i].G) + (bX - palette[i].B) * (bX - palette[i].B);

							if (shortestDistance > distance)
							{
								shortestDistance = distance;
								bestPaletteRepresentation = (byte)i;
							}
						}

						array[r, g, b] = bestPaletteRepresentation;
					}

/*#if DEBUG
			Console.WriteLine("GetInversePalette32x32x32(): " + DateTime.Now.Subtract(start).ToString());
#endif*/

			return array;
		}
		#endregion

		#endregion

		//	PRIVATE METHODS
		#region private methods

		#region class Vbox
		private class Vbox
		{
			uint[,,]	array;
			int			locationR, locationG, locationB;
			int			rDimension, gDimension, bDimension;
			uint[]		axisRQuantities, axisGQuantities, axisBQuantities;

			#region constructor
			public Vbox(int locationR, int locationG, int locationB, uint[, ,] quantities)
			{
				this.locationR = locationR;
				this.locationG = locationG;
				this.locationB = locationB;

				rDimension = quantities.GetLength(0);
				gDimension = quantities.GetLength(1);
				bDimension = quantities.GetLength(2);

				array = quantities;

				axisRQuantities = new uint[rDimension];
				axisGQuantities = new uint[gDimension];
				axisBQuantities = new uint[bDimension];

				for (int r = 0; r < rDimension; r++)
					for (int g = 0; g < gDimension; g++)
						for (int b = 0; b < bDimension; b++)
						{
							axisRQuantities[r] += array[r, g, b];
							axisGQuantities[g] += array[r, g, b];
							axisBQuantities[b] += array[r, g, b];
						}
			}
			#endregion

			//PUBLIC PROPERTIES
			#region public properties

			#region Quantity
			public uint Quantity
			{
				get
				{
					uint qantity = 0;
					
					for (int r = 0; r < rDimension; r++)
						for (int g = 0; g < gDimension; g++)
							for (int b = 0; b < bDimension; b++)
								qantity += array[r, g, b];

					return qantity;
				}
			}
			#endregion

			#region IsSplittable
			public bool IsSplittable
			{
				get
				{
					return (IsRSplittable || IsGSplittable || IsBSplittable);
				}
			}
			#endregion

			#endregion

			//PRIVATE PROPERTIES
			#region private properties

			#region IsRSplittable
			private bool IsRSplittable
			{
				get
				{
					int notNullFields = 0;

					for (int i = 0; i < axisRQuantities.Length; i++)
						if (axisRQuantities[i] > 0)
							notNullFields++;

					return notNullFields > 1;
				}
			}
			#endregion

			#region IsGSplittable
			private bool IsGSplittable
			{
				get
				{
					int notNullFields = 0;

					for (int i = 0; i < axisGQuantities.Length; i++)
						if (axisGQuantities[i] > 0)
							notNullFields++;

					return notNullFields > 1;
				}
			}
			#endregion

			#region IsBSplittable
			private bool IsBSplittable
			{
				get
				{
					int notNullFields = 0;

					for (int i = 0; i < axisBQuantities.Length; i++)
						if (axisBQuantities[i] > 0)
							notNullFields++;

					return notNullFields > 1;
				}
			}
			#endregion

			#endregion

			//PUBLIC METHODS
			#region public methods

			#region Split()
			public void Split(out Vbox vBox1, out Vbox vBox2)
			{
				bool rSplittable = IsSplittable, gSplittable = IsGSplittable, bSplittable = IsBSplittable;

				if(rSplittable && (gSplittable == false || rDimension >= gDimension) && (bSplittable == false || rDimension >= bDimension))
					SplitByR(out vBox1, out vBox2);
				else if (gSplittable && (bSplittable == false || gDimension >= bDimension))
					SplitByG(out vBox1, out vBox2);
				else
					SplitByB(out vBox1, out vBox2);
			}
			#endregion

			#region GetMedian()
			public Color GetMedian()
			{
				uint qantity = 0;
				int rMax = 0, gMax = 0, bMax = 0;

				for (int r = 0; r < rDimension; r++)
					for (int g = 0; g < gDimension; g++)
						for (int b = 0; b < bDimension; b++)
							if (qantity < array[r, g, b])
							{
								qantity = array[r, g, b];
								rMax = r;
								gMax = g;
								bMax = b;
							}

				return Color.FromArgb(rMax + locationR, gMax + locationG, bMax + locationB);
			}
			#endregion

			#endregion

			//PRIVATE METHODS
			#region private methods

			#region SplitByR()
			private void SplitByR(out Vbox vBox1, out Vbox vBox2)
			{
				int currentL, currentR;

				GetMedian(axisRQuantities, out currentL, out currentR);

				uint[, ,] array1 = new uint[currentL + 1, gDimension, bDimension];
				uint[, ,] array2 = new uint[rDimension - currentR, gDimension, bDimension];

				for (int r = 0; r <= currentL; r++)
					for (int g = 0; g < gDimension; g++)
						for (int b = 0; b < bDimension; b++)
							array1[r,g,b] = array[r, g, b];
				
				for (int r = currentR; r < rDimension; r++)
					for (int g = 0; g < gDimension; g++)
						for (int b = 0; b < bDimension; b++)
							array2[r - currentR,g,b] = array[r, g, b];

				vBox1 = new Vbox(locationR, locationG, locationB, array1);
				vBox2 = new Vbox(locationR + currentR, locationG, locationB, array2);
			}
			#endregion

			#region SplitByG()
			private void SplitByG(out Vbox vBox1, out Vbox vBox2)
			{
				int currentL, currentR;

				GetMedian(axisGQuantities, out currentL, out currentR);

				uint[, ,] array1 = new uint[rDimension, currentL + 1, bDimension];
				uint[, ,] array2 = new uint[rDimension, gDimension - currentR, bDimension];

				for (int r = 0; r < rDimension; r++)
					for (int g = 0; g <= currentL; g++)
						for (int b = 0; b < bDimension; b++)
							array1[r, g, b] = array[r, g, b];

				for (int r = 0; r < rDimension; r++)
					for (int g = currentR; g < gDimension; g++)
						for (int b = 0; b < bDimension; b++)
							array2[r, g - currentR, b] = array[r, g, b];

				vBox1 = new Vbox(locationR, locationG, locationB, array1);
				vBox2 = new Vbox(locationR, locationG + currentR, locationB, array2);
			}
			#endregion

			#region SplitByB()
			private void SplitByB(out Vbox vBox1, out Vbox vBox2)
			{
				int currentL, currentR;

				GetMedian(axisBQuantities, out currentL, out currentR);

				uint[, ,] array1 = new uint[rDimension, gDimension, currentL + 1];
				uint[, ,] array2 = new uint[rDimension, gDimension, bDimension - currentR];

				for (int r = 0; r < rDimension; r++)
					for (int g = 0; g < gDimension; g++)
						for (int b = 0; b <= currentL; b++)
							array1[r, g, b] = array[r, g, b];

				for (int r = 0; r < rDimension; r++)
					for (int g = 0; g < gDimension; g++)
						for (int b = currentR; b < bDimension; b++)
							array2[r, g, b - currentR] = array[r, g, b];

				vBox1 = new Vbox(locationR, locationG, locationB, array1);
				vBox2 = new Vbox(locationR, locationG, locationB + currentR, array2);
			}
			#endregion

			#region GetMedian()
			private static void GetMedian(uint[] array, out int currentL, out int currentR)
			{
				int left = 0, right = array.Length - 1;

				currentL = left;
				currentR = right;

				uint leftSum = array[currentL];
				uint rightSum = array[currentR];

				while (currentL < currentR - 1)
				{
					if (leftSum <= rightSum)
					{
						currentL++;
						leftSum += array[currentL];
					}
					else
					{
						currentR--;
						rightSum += array[currentR];
					}
				}
			}
			#endregion

			#endregion

		}
		#endregion

		#region GetQuantizationCube()
		private uint[, ,] GetQuantizationCube(ImageProcessing.BigImages.ItDecoder itBitmap)
		{
			uint[, ,]	array = new uint[256 / vBoxSize, 256 / vBoxSize, 256 / vBoxSize];
			int			stripHeight = Math.Max(1, 200000000 / (itBitmap.Width * 3));
			int			width = itBitmap.Width;
			int			height = itBitmap.Height;
			
			unsafe
			{
				Bitmap bitmap = null;
				BitmapData bitmapData = null;

				for (int stripY = 0; stripY < height; stripY = stripY + stripHeight)
				{
					int bottom = Math.Min(stripY + stripHeight, height);
					
					try
					{
						bitmap = itBitmap.GetClip(Rectangle.FromLTRB(0, stripY, width, bottom));
						bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

						int stride = bitmapData.Stride;
						int clipW = bitmapData.Width;
						int clipH = bitmapData.Height;

						int xJump = (width / 1500) + 1;
						int yJump = (height / 1500) + 1;
						
						byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
						byte* pCurrent;

						if (itBitmap.PixelsFormat == PixelsFormat.Format24bppRgb || itBitmap.PixelsFormat == PixelsFormat.Format32bppRgb)
						{
							int bytesPerPixel = (itBitmap.PixelsFormat == PixelsFormat.Format32bppRgb) ? 4 : 3;
							int xJumpBytes = xJump * bytesPerPixel;

							for (int y = 0; y < height; y = y + yJump)
							{
								pCurrent = pOrig + y * stride;

								for (int x = 0; x < width; x = x + xJump)
								{
									array[pCurrent[2] / vBoxSize, pCurrent[1] / vBoxSize, pCurrent[0] / vBoxSize]++;
									pCurrent += xJumpBytes;
								}
							}
						}
						else if (itBitmap.PixelsFormat == PixelsFormat.Format8bppIndexed)
						{
							if (Misc.IsGrayscale(bitmap) == false)
							{
								Color[] entries = bitmap.Palette.Entries;

								for (int y = 0; y < height; y = y + yJump)
								{
									pCurrent = pOrig + y * stride;

									for (int x = 0; x < width; x = x + xJump)
									{
										array[entries[pCurrent[0]].R / vBoxSize, entries[pCurrent[0]].G / vBoxSize, entries[pCurrent[0]].B / vBoxSize]++;
										pCurrent++;
									}
								}
							}
							else
							{
								for (int y = 0; y < height; y = y + yJump)
								{
									pCurrent = pOrig + y * stride;

									for (int x = 0; x < width; x = x + xJump)
									{
										array[pCurrent[0] / vBoxSize, pCurrent[0] / vBoxSize, pCurrent[0] / vBoxSize]++;
										pCurrent++;
									}
								}
							}
						}
						else if (bitmap.PixelFormat == PixelFormat.Format4bppIndexed)
						{
							if (Misc.IsGrayscale(bitmap) == false)
							{
								Color[] entries = bitmap.Palette.Entries;

								for (int y = 0; y < height; y = y + yJump)
								{
									pCurrent = pOrig + y * stride;

									for (int x = 0; x < width; x = x + xJump)
									{
										array[entries[*pCurrent >> 4].R / vBoxSize, entries[*pCurrent >> 4].G / vBoxSize, entries[*pCurrent >> 4].B / vBoxSize]++;
										array[entries[*pCurrent & 0xF].R / vBoxSize, entries[*pCurrent & 0xF].G / vBoxSize, entries[*pCurrent & 0xF].B / vBoxSize]++;
										pCurrent += xJump;
									}
								}
							}
							else
							{
								for (int y = 0; y < height; y = y + yJump)
								{
									pCurrent = pOrig + y * stride;

									for (int x = 0; x < width; x = x + xJump)
									{
										array[(*pCurrent >> 4) * 17 / vBoxSize, (*pCurrent >> 4) * 17 / vBoxSize, (*pCurrent >> 4) * 17 / vBoxSize]++;
										array[(*pCurrent >> 4) * 17 / vBoxSize, (*pCurrent >> 4) * 17 / vBoxSize, (*pCurrent >> 4) * 17 / vBoxSize]++;
										pCurrent += xJump;
									}
								}
							}
						}
					}
					finally
					{
						if (bitmapData != null)
						{
							bitmap.UnlockBits(bitmapData);
							bitmapData = null;
						}

						itBitmap.ReleaseAllocatedMemory(bitmap);
						bitmap = null;
					}

					if (ProgressChanged != null)
						ProgressChanged((bottom) / (float)height);
				}
			}

			return array;
		}
		#endregion

		#region GetQuantizationCube()
		private uint[, ,] GetQuantizationCube(Bitmap bitmap)
		{
			uint[, ,] array = new uint[256 / vBoxSize, 256 / vBoxSize, 256 / vBoxSize];
			int width = bitmap.Width;
			int height = bitmap.Height;
			BitmapData bitmapData = null;

			unsafe
			{
				try
				{
					bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

					int stride = bitmapData.Stride;

					int xJump = (width / 1500) + 1;
					int yJump = (height / 1500) + 1;

					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					if (bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format24bppRgb)
					{
						int bytesPerPixel = (bitmap.PixelFormat == PixelFormat.Format32bppRgb || bitmap.PixelFormat == PixelFormat.Format32bppArgb) ? 4 : 3;
						int xJumpBytes = xJump * bytesPerPixel;

						for (int y = 0; y < height; y = y + yJump)
						{
							pCurrent = pOrig + y * stride;

							for (int x = 0; x < width; x = x + xJump)
							{
								array[pCurrent[2] / vBoxSize, pCurrent[1] / vBoxSize, pCurrent[0] / vBoxSize]++;
								pCurrent += xJumpBytes;
							}
						}
					}
					else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						if (Misc.IsGrayscale(bitmap) == false)
						{
							Color[] entries = bitmap.Palette.Entries;
							
							for (int y = 0; y < height; y = y + yJump)
							{
								pCurrent = pOrig + y * stride;

								for (int x = 0; x < width; x = x + xJump)
								{
									array[entries[pCurrent[0]].R / vBoxSize, entries[pCurrent[0]].G / vBoxSize, entries[pCurrent[0]].B / vBoxSize]++;
									pCurrent += xJump;
								}
							}
						}
						else
						{
							for (int y = 0; y < height; y = y + yJump)
							{
								pCurrent = pOrig + y * stride;

								for (int x = 0; x < width; x = x + xJump)
								{
									array[pCurrent[0] / vBoxSize, pCurrent[0] / vBoxSize, pCurrent[0] / vBoxSize]++;
									pCurrent += xJump;
								}
							}
						}
					}
					else if (bitmap.PixelFormat == PixelFormat.Format4bppIndexed)
					{
						if (Misc.IsGrayscale(bitmap) == false)
						{
							Color[] entries = bitmap.Palette.Entries;

							for (int y = 0; y < height; y = y + yJump)
							{
								pCurrent = pOrig + y * stride;

								for (int x = 0; x < width; x = x + xJump)
								{
									array[entries[*pCurrent >> 4].R / vBoxSize, entries[*pCurrent >> 4].G / vBoxSize, entries[*pCurrent >> 4].B / vBoxSize]++;
									array[entries[*pCurrent & 0xF].R / vBoxSize, entries[*pCurrent & 0xF].G / vBoxSize, entries[*pCurrent & 0xF].B / vBoxSize]++;
									pCurrent += xJump;
								}
							}
						}
						else
						{
							for (int y = 0; y < height; y = y + yJump)
							{
								pCurrent = pOrig + y * stride;

								for (int x = 0; x < width; x = x + xJump)
								{
									array[(*pCurrent >> 4) * 17 / vBoxSize, (*pCurrent >> 4) * 17 / vBoxSize, (*pCurrent >> 4) * 17 / vBoxSize]++;
									array[(*pCurrent >> 4) * 17 / vBoxSize, (*pCurrent >> 4) * 17 / vBoxSize, (*pCurrent >> 4) * 17 / vBoxSize]++;
									pCurrent += xJump;
								}
							}
						}
					}
				}
				finally
				{
					if (bitmapData != null)
					{
						bitmap.UnlockBits(bitmapData);
						bitmapData = null;
					}
				}

				if (ProgressChanged != null)
					ProgressChanged(1);
			}

			return array;
		}
		#endregion

		#region DoMedianCut()
		private Color[] DoMedianCut(uint[, ,] cube)
		{
			List<Vbox>	vBoxes = new List<Vbox>();
			Vbox		vbox = new Vbox(0, 0, 0, cube);

			vBoxes.Add(vbox);

			while (vBoxes.Count < 256)
			{
				//get most quantitative Vbox, that can be split
				Vbox vBoxToSplit = SelectVboxToSplit(vBoxes);

				if (vBoxToSplit != null)
				{
					Vbox vBox1;
					Vbox vBox2;

					vBoxToSplit.Split(out vBox1, out vBox2);

					vBoxes.Remove(vBoxToSplit);
					vBoxes.Add(vBox1);
					vBoxes.Add(vBox2);			
				}
				else
				{
					break;
				}
			}

			List<Color> colorEntries = new List<Color>();

			foreach (Vbox vBox in vBoxes)
			{
				Color c = vBox.GetMedian();
				
				colorEntries.Add(Color.FromArgb(c.R * vBoxSize + vBoxSize/2, c.G * vBoxSize + vBoxSize/2, c.B * vBoxSize + vBoxSize/2));
			}

			return colorEntries.ToArray();
		}
		#endregion

		#region SelectVboxToSplit()
		private Vbox SelectVboxToSplit(List<Vbox> vBoxes)
		{
			Vbox vBox = null;

			foreach (Vbox v in vBoxes)
				if (v.IsSplittable && (vBox == null || v.Quantity > vBox.Quantity))
					vBox = v;

			return vBox;
		}
		#endregion

		#endregion
	}
}
