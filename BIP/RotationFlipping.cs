using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageProcessing.Languages;

namespace ImageProcessing
{
	public class RotationFlipping
	{
		//PUBLIC METHODS
		#region public methods

		#region Go()
		public static Bitmap Go(Bitmap bitmap, System.Drawing.RotateFlipType rotateFlip)
		{
			Bitmap result;

			if (rotateFlip == RotateFlipType.RotateNoneFlipNone || rotateFlip == RotateFlipType.Rotate180FlipXY)
				result = ImageProcessing.ImageCopier.Copy(bitmap);
			else if (rotateFlip == RotateFlipType.RotateNoneFlipX || rotateFlip == RotateFlipType.Rotate180FlipY)
				result = RotateNoneFlipX(bitmap);
			else if (rotateFlip == RotateFlipType.RotateNoneFlipY || rotateFlip == RotateFlipType.Rotate180FlipX)
				result = RotateNoneFlipY(bitmap);
			else if (rotateFlip == RotateFlipType.RotateNoneFlipXY || rotateFlip == RotateFlipType.Rotate180FlipNone)
				result = RotateNoneFlipXY(bitmap);
			else if (rotateFlip == RotateFlipType.Rotate90FlipNone || rotateFlip == RotateFlipType.Rotate270FlipXY)
				result = Rotate90FlipNone(bitmap);
			else if (rotateFlip == RotateFlipType.Rotate90FlipX || rotateFlip == RotateFlipType.Rotate270FlipY)
				result = Rotate90FlipX(bitmap);
			else if (rotateFlip == RotateFlipType.Rotate90FlipY || rotateFlip == RotateFlipType.Rotate270FlipX)
				result = Rotate90FlipY(bitmap);
			else
				result = Rotate90FlipXY(bitmap);

			if (bitmap.HorizontalResolution > 0 && bitmap.VerticalResolution > 0)
				Misc.SetBitmapResolution(result, bitmap.HorizontalResolution, bitmap.VerticalResolution);

			if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed || bitmap.PixelFormat == PixelFormat.Format4bppIndexed || bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
				result.Palette = bitmap.Palette;

			return result;
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region RotateNoneFlipX()
		private static Bitmap RotateNoneFlipX(Bitmap source)
		{
			Bitmap		dest = null;
			BitmapData	sourceData = null;
			BitmapData	destData = null;

			try
			{
				dest = new Bitmap(source.Width, source.Height, source.PixelFormat);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				destData = dest.LockBits(new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat);

				int width = source.Width;
				int height = source.Height;

				int strideS = sourceData.Stride;
				int strideD = destData.Stride;

				unsafe
				{
					byte* pSourceS = (byte*)sourceData.Scan0.ToPointer();
					byte* pSourceD = (byte*)destData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentD;

					int x, y;

					if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{						
						for (y = 0; y < height; y++)
						{
							pCurrentS = pSourceS + (y * strideS);
							pCurrentD = pSourceD + (y * strideD);

							for (x = 0; x < width; x++)
							{
								pCurrentD[x * 3] = pCurrentS[(width - 1 - x) * 3];
								pCurrentD[x * 3 + 1] = pCurrentS[(width - 1 - x) * 3 + 1];
								pCurrentD[x * 3 + 2] = pCurrentS[(width - 1 - x) * 3 + 2];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{						
						for (y = 0; y < height; y++)
						{
							pCurrentS = pSourceS + (y * strideS);
							pCurrentD = pSourceD + (y * strideD);

							for (x = 0; x < width; x++)
							{
								pCurrentD[x] = pCurrentS[(width - 1 - x)];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						int bite;
						
						for (y = 0; y < height; y++)
						{
							pCurrentS = pSourceS + (y * strideS);
							pCurrentD = pSourceD + (y * strideD);

							for (x = 0; x < width; x++)
							{
								bite = pCurrentS[x / 8] & (byte)(0x80 >> (x & 0x7));
																
								if (bite > 0)
									pCurrentD[(width - 1 - x) / 8] |= (byte)(0x80 >> ((width - 1 - x) & 0x7));
								
							}
						}
					}
					else
						throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
				}

				return dest;
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);

				if (destData != null)
					dest.UnlockBits(destData);
			}
		}		
		#endregion

		#region RotateNoneFlipY()
		private static Bitmap RotateNoneFlipY(Bitmap source)
		{
			Bitmap		dest = null;
			BitmapData	sourceData = null;
			BitmapData	destData = null;

			try
			{
				int width = source.Width;
				int height = source.Height;

				dest = new Bitmap(source.Width, source.Height, source.PixelFormat);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				destData = dest.LockBits(new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat);

				int strideS = sourceData.Stride;
				int strideD = destData.Stride;

				unsafe
				{
					byte* pSourceS = (byte*)sourceData.Scan0.ToPointer();
					byte* pSourceD = (byte*)destData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentD;

					int x, y;
					int bytes = strideS < strideD ? strideS : strideD;

					for (y = 0; y < height; y++)
					{
						pCurrentS = pSourceS + (y * strideS);
						pCurrentD = pSourceD + ((height - y - 1) * strideD);

						for (x = 0; x < bytes; x++)
						{
							pCurrentD[x] = pCurrentS[x];
						}
					}
				}

				return dest;
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);

				if (destData != null)
					dest.UnlockBits(destData);
			}
		}
		#endregion

		#region RotateNoneFlipXY()
		private static Bitmap RotateNoneFlipXY(Bitmap source)
		{
			Bitmap dest = null;
			BitmapData sourceData = null;
			BitmapData destData = null;

			try
			{
				int width = source.Width;
				int height = source.Height;

				dest = new Bitmap(source.Width, source.Height, source.PixelFormat);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				destData = dest.LockBits(new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat);

				int strideS = sourceData.Stride;
				int strideD = destData.Stride;

				unsafe
				{
					byte* pSourceS = (byte*)sourceData.Scan0.ToPointer();
					byte* pSourceD = (byte*)destData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentD;

					int x, y;

					if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < height; y++)
						{
							pCurrentS = pSourceS + (y * strideS);
							pCurrentD = pSourceD + ((height - y - 1) * strideD);

							for (x = 0; x < width; x++)
							{
								pCurrentD[x * 3] = pCurrentS[(width - 1 - x) * 3];
								pCurrentD[x * 3 + 1] = pCurrentS[(width - 1 - x) * 3 + 1];
								pCurrentD[x * 3 + 2] = pCurrentS[(width - 1 - x) * 3 + 2];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < height; y++)
						{
							pCurrentS = pSourceS + (y * strideS);
							pCurrentD = pSourceD + ((height - y - 1) * strideD);

							for (x = 0; x < width; x++)
							{
								pCurrentD[x] = pCurrentS[(width - 1 - x)];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						int bite;

						for (y = 0; y < height; y++)
						{
							pCurrentS = pSourceS + (y * strideS);
							pCurrentD = pSourceD + ((height - y - 1) * strideD);

							for (x = 0; x < width; x++)
							{
								bite = pCurrentS[x / 8] & (byte)(0x80 >> (x & 0x7));

								if (bite > 0)
									pCurrentD[(width - 1 - x) / 8] |= (byte)(0x80 >> ((width - 1 - x) & 0x7));
							}
						}
					}
					else
						throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
				}

				return dest;
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);

				if (destData != null)
					dest.UnlockBits(destData);
			}
		}
		#endregion

		#region Rotate90FlipNone()
		private static Bitmap Rotate90FlipNone(Bitmap source)
		{
			Bitmap dest = null;
			BitmapData sourceData = null;
			BitmapData destData = null;

			try
			{
				dest = new Bitmap(source.Height, source.Width, source.PixelFormat);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				destData = dest.LockBits(new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat);

				int strideS = sourceData.Stride;
				int strideD = destData.Stride;
				int sourceWidth = source.Width;
				int sourceHeight = source.Height;

				unsafe
				{
					byte* pSourceS = (byte*)sourceData.Scan0.ToPointer();
					byte* pSourceD = (byte*)destData.Scan0.ToPointer();
					byte* pCurrentS;
					int x, y;

					if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < sourceHeight; y++)
						{
							pCurrentS = pSourceS + (y * strideS);

							for (x = 0; x < sourceWidth; x++)
							{
								pSourceD[x * strideD + (sourceHeight - 1 - y) * 3] = pCurrentS[x * 3];
								pSourceD[x * strideD + (sourceHeight - 1 - y) * 3 + 1] = pCurrentS[x * 3 + 1];
								pSourceD[x * strideD + (sourceHeight - 1 - y) * 3 + 2] = pCurrentS[x * 3 + 2];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < sourceHeight; y++)
						{
							pCurrentS = pSourceS + (y * strideS);

							for (x = 0; x < sourceWidth; x++)
							{
								pSourceD[x * strideD + (sourceHeight - 1 - y)] = pCurrentS[x];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						int bite;

						for (y = 0; y < sourceHeight; y++)
						{
							pCurrentS = pSourceS + (y * strideS);

							for (x = 0; x < sourceWidth; x++)
							{
								bite = pCurrentS[x / 8] & (byte)(0x80 >> (x & 0x7));

								if (bite > 0)
									pSourceD[x * strideD + (sourceHeight - 1 - y) / 8] |= (byte)(0x80 >> ((sourceHeight - 1 - y) & 0x7));
							}
						}
					}
					else
						throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
				}

				return dest;
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);

				if (destData != null)
					dest.UnlockBits(destData);
			}
		}
		#endregion
	
		#region Rotate90FlipX()
		private static Bitmap Rotate90FlipX(Bitmap source)
		{
			Bitmap dest = null;
			BitmapData sourceData = null;
			BitmapData destData = null;

			try
			{
				dest = new Bitmap(source.Height, source.Width, source.PixelFormat);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				destData = dest.LockBits(new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat);

				int strideS = sourceData.Stride;
				int strideD = destData.Stride;
				int sourceWidth = source.Width;
				int sourceHeight = source.Height;

				unsafe
				{
					byte* pSourceS = (byte*)sourceData.Scan0.ToPointer();
					byte* pSourceD = (byte*)destData.Scan0.ToPointer();
					byte* pCurrentS;
					int x, y;

					if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < sourceHeight; y++)
						{
							pCurrentS = pSourceS + (y * strideS);

							for (x = 0; x < sourceWidth; x++)
							{
								pSourceD[x * strideD + y * 3] = pCurrentS[x * 3];
								pSourceD[x * strideD + y * 3 + 1] = pCurrentS[x * 3 + 1];
								pSourceD[x * strideD + y * 3 + 2] = pCurrentS[x * 3 + 2];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < sourceHeight; y++)
						{
							pCurrentS = pSourceS + (y * strideS);

							for (x = 0; x < sourceWidth; x++)
							{
								pSourceD[x * strideD + y] = pCurrentS[x];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						int bite;

						for (y = 0; y < sourceHeight; y++)
						{
							pCurrentS = pSourceS + (y * strideS);

							for (x = 0; x < sourceWidth; x++)
							{
								bite = pCurrentS[x / 8] & (byte)(0x80 >> (x & 0x7));

								if (bite > 0)
									pSourceD[x * strideD + y / 8] |= (byte)(0x80 >> (y & 0x7));
							}
						}
					}
					else
						throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
				}

				return dest;
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);

				if (destData != null)
					dest.UnlockBits(destData);
			}
		}
		#endregion

		#region Rotate90FlipY()
		private static Bitmap Rotate90FlipY(Bitmap source)
		{
			Bitmap dest = null;
			BitmapData sourceData = null;
			BitmapData destData = null;

			try
			{
				dest = new Bitmap(source.Height, source.Width, source.PixelFormat);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				destData = dest.LockBits(new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat);

				int strideS = sourceData.Stride;
				int strideD = destData.Stride;
				int sourceWidth = source.Width;
				int sourceHeight = source.Height;

				unsafe
				{
					byte* pSourceS = (byte*)sourceData.Scan0.ToPointer();
					byte* pSourceD = (byte*)destData.Scan0.ToPointer();
					byte* pCurrentS;
					int x, y;

					if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < sourceHeight; y++)
						{
							pCurrentS = pSourceS + (y * strideS);

							for (x = 0; x < sourceWidth; x++)
							{
								pSourceD[(sourceWidth - 1 - x) * strideD + (sourceHeight - 1 - y) * 3] = pCurrentS[x * 3];
								pSourceD[(sourceWidth - 1 - x) * strideD + (sourceHeight - 1 - y) * 3 + 1] = pCurrentS[x * 3 + 1];
								pSourceD[(sourceWidth - 1 - x) * strideD + (sourceHeight - 1 - y) * 3 + 2] = pCurrentS[x * 3 + 2];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < sourceHeight; y++)
						{
							pCurrentS = pSourceS + (y * strideS);

							for (x = 0; x < sourceWidth; x++)
							{
								pSourceD[(sourceWidth - 1 - x) * strideD + (sourceHeight - 1 - y)] = pCurrentS[x];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						int bite;

						for (y = 0; y < sourceHeight; y++)
						{
							pCurrentS = pSourceS + (y * strideS);

							for (x = 0; x < sourceWidth; x++)
							{
								bite = pCurrentS[x / 8] & (byte)(0x80 >> (x & 0x7));

								if (bite > 0)
									pSourceD[(sourceWidth - 1 - x) * strideD + (sourceHeight - 1 - y) / 8] |= (byte)(0x80 >> ((sourceHeight - 1 - y) & 0x7));
							}
						}
					}
					else
						throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
				}

				return dest;
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);

				if (destData != null)
					dest.UnlockBits(destData);
			}
		}
		#endregion
	
		#region Rotate90FlipXY()
		private static Bitmap Rotate90FlipXY(Bitmap source)
		{
			Bitmap dest = null;
			BitmapData sourceData = null;
			BitmapData destData = null;

			try
			{
				dest = new Bitmap(source.Height, source.Width, source.PixelFormat);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				destData = dest.LockBits(new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat);

				int strideS = sourceData.Stride;
				int strideD = destData.Stride;
				int sourceWidth = source.Width;
				int sourceHeight = source.Height;

				unsafe
				{
					byte* pSourceS = (byte*)sourceData.Scan0.ToPointer();
					byte* pSourceD = (byte*)destData.Scan0.ToPointer();
					byte* pCurrentS;
					int x, y;

					if (source.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < sourceHeight; y++)
						{
							pCurrentS = pSourceS + (y * strideS);

							for (x = 0; x < sourceWidth; x++)
							{
								pSourceD[(sourceWidth - 1 - x) * strideD + y * 3] = pCurrentS[x * 3];
								pSourceD[(sourceWidth - 1 - x) * strideD + y * 3 + 1] = pCurrentS[x * 3 + 1];
								pSourceD[(sourceWidth - 1 - x) * strideD + y * 3 + 2] = pCurrentS[x * 3 + 2];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < sourceHeight; y++)
						{
							pCurrentS = pSourceS + (y * strideS);

							for (x = 0; x < sourceWidth; x++)
							{
								pSourceD[(sourceWidth - 1 - x) * strideD + y] = pCurrentS[x];
							}
						}
					}
					else if (source.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						int bite;

						for (y = 0; y < sourceHeight; y++)
						{
							pCurrentS = pSourceS + (y * strideS);

							for (x = 0; x < sourceWidth; x++)
							{
								bite = pCurrentS[x / 8] & (byte)(0x80 >> (x & 0x7));

								if (bite > 0)
									pSourceD[(sourceWidth - 1 - x) * strideD + y / 8] |= (byte)(0x80 >> (y & 0x7));
							}
						}
					}
					else
						throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
				}

				return dest;
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);

				if (destData != null)
					dest.UnlockBits(destData);
			}
		}
		#endregion

		#endregion
	}
}
