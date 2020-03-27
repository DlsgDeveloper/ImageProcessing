using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.BitmapOperations
{
	/// <summary>
	/// Inserts image into another.
	/// </summary>
	public class Insertor
	{

		// PUBLIC METHODS
		#region public methods

		#region Insert()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="insertedImage"></param>
		/// <param name="location">Upper left corner.</param>
		public static unsafe void Insert(Bitmap dest, Bitmap insertedImage, Point location)
		{
			if (insertedImage.PixelFormat == PixelFormat.Format24bppRgb && dest.PixelFormat == PixelFormat.Format8bppIndexed)
				Insert24bppInto8bpp(dest, insertedImage, location);
			else if (insertedImage.PixelFormat == PixelFormat.Format24bppRgb && dest.PixelFormat == PixelFormat.Format24bppRgb)
				Insert24bppInto24bpp(dest, insertedImage, location);
			else if (insertedImage.PixelFormat == PixelFormat.Format32bppArgb && dest.PixelFormat == PixelFormat.Format8bppIndexed)
				Insert32bppInto8bpp(dest, insertedImage, location);
			else if (insertedImage.PixelFormat == PixelFormat.Format32bppArgb && dest.PixelFormat == PixelFormat.Format24bppRgb)
				Insert32bppInto24bpp(dest, insertedImage, location);
			else if (insertedImage.PixelFormat == PixelFormat.Format32bppArgb && dest.PixelFormat == PixelFormat.Format32bppArgb)
				Insert32bppInto32bpp(dest, insertedImage, location);
			else
				throw new Exception(string.Format("Insertor, Insert(): Unsupported insertion of '{0}' to '{1}'.", insertedImage.PixelFormat, dest.PixelFormat));
		}
		#endregion

		#endregion


		// PRIVATE METHODS
		#region private methods

		#region Insert24bppInto8bpp()
		static unsafe void Insert24bppInto8bpp(Bitmap dest, Bitmap insertedImage, Point location)
		{
			BitmapData destinationBitmapData = null;
			BitmapData insertedBitmapData = null;

			try
			{
				Rectangle rect = new Rectangle(location.X, location.Y, insertedImage.Width, insertedImage.Height);

				if (rect.X >= 0 && rect.Y >= 0 && rect.Right <= dest.Width && rect.Bottom <= dest.Height)
				{
					destinationBitmapData = dest.LockBits(rect, ImageLockMode.WriteOnly, dest.PixelFormat);
					insertedBitmapData = insertedImage.LockBits(new Rectangle(0, 0, insertedImage.Width, insertedImage.Height), ImageLockMode.ReadOnly, insertedImage.PixelFormat);

					byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
					byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

					int strideD = destinationBitmapData.Stride;
					int strideI = insertedBitmapData.Stride;

					int width = insertedBitmapData.Width;
					int height = insertedBitmapData.Height;

					for (int y = 0; y < height; y++)
						for (int x = 0; x < width; x++)
						{
							try
							{
								pDestination[y * strideD + x] = (byte)(0.11 * pInserted[y * strideI + x * 3 + 0] + 0.59 * pInserted[y * strideI + x * 3 + 1] + 0.3 * pInserted[y * strideI + x * 3 + 2]);
							}
							catch (Exception)
							{
								throw;
							}
						}
				}
				else if (rect.IntersectsWith(new Rectangle(0, 0, dest.Width, dest.Height)))
				{
					destinationBitmapData = dest.LockBits(new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat);
					insertedBitmapData = insertedImage.LockBits(new Rectangle(0, 0, insertedImage.Width, insertedImage.Height), ImageLockMode.ReadOnly, insertedImage.PixelFormat);

					byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
					byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

					int strideD = destinationBitmapData.Stride;
					int strideI = insertedBitmapData.Stride;

					int xFrom = Math.Max(0, rect.Left);
					int xTo = Math.Min(dest.Width, rect.Right);
					int yFrom = Math.Max(0, rect.Top);
					int yTo = Math.Min(dest.Height, rect.Bottom);

					for (int y = yFrom; y < yTo; y++)
						for (int x = xFrom; x < xTo; x++)
						{
							try
							{
								pDestination[y * strideD + x] = (byte)(0.11 * pInserted[y * strideI + x * 3 + 0] + 0.59 * pInserted[y * strideI + x * 3 + 1] + 0.3 * pInserted[y * strideI + x * 3 + 2]);
							}
							catch (Exception)
							{
								throw;
							}
						}
				}
			}
			finally
			{
				if (destinationBitmapData != null)
					dest.UnlockBits(destinationBitmapData);
				if (insertedBitmapData != null)
					insertedImage.UnlockBits(insertedBitmapData);
			}
		}
		#endregion

		#region Insert24bppInto24bpp()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="insertedImage"></param>
		/// <param name="location">Upper left corner.</param>
		static unsafe void Insert24bppInto24bpp(Bitmap dest, Bitmap insertedImage, Point location)
		{
			BitmapData destinationBitmapData = null;
			BitmapData insertedBitmapData = null;

			try
			{
				Rectangle rect = new Rectangle(location.X, location.Y, insertedImage.Width, insertedImage.Height);

				if (rect.X >= 0 && rect.Y >= 0 && rect.Right <= dest.Width && rect.Bottom <= dest.Height)
				{
					destinationBitmapData = dest.LockBits(rect, ImageLockMode.WriteOnly, dest.PixelFormat);
					insertedBitmapData = insertedImage.LockBits(new Rectangle(0, 0, insertedImage.Width, insertedImage.Height), ImageLockMode.ReadOnly, insertedImage.PixelFormat);

					byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
					byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

					int strideD = destinationBitmapData.Stride;
					int strideI = insertedBitmapData.Stride;

					int width = insertedBitmapData.Width;
					int height = insertedBitmapData.Height;

					for (int y = 0; y < height; y++)
						for (int x = 0; x < width; x++)
						{
							try
							{
								pDestination[y * strideD + x * 3 + 0] = pInserted[y * strideI + x * 3 + 0];
								pDestination[y * strideD + x * 3 + 1] = pInserted[y * strideI + x * 3 + 1];
								pDestination[y * strideD + x * 3 + 2] = pInserted[y * strideI + x * 3 + 2];
							}
							catch (Exception)
							{
								throw;
							}
						}
				}
				else if (rect.IntersectsWith(new Rectangle(0, 0, dest.Width, dest.Height)))
				{					
					destinationBitmapData = dest.LockBits(new Rectangle(0,0,dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat);
					insertedBitmapData = insertedImage.LockBits(new Rectangle(0, 0, insertedImage.Width, insertedImage.Height), ImageLockMode.ReadOnly, insertedImage.PixelFormat);

					byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
					byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

					int strideD = destinationBitmapData.Stride;
					int strideI = insertedBitmapData.Stride;

					int xFrom = Math.Max(0, rect.Left);
					int xTo = Math.Min(dest.Width, rect.Right);
					int yFrom = Math.Max(0, rect.Top);
					int yTo = Math.Min(dest.Height, rect.Bottom);

					for (int y = yFrom; y < yTo; y++)
						for (int x = xFrom; x < xTo; x++)
						{
							try
							{
								pDestination[y * strideD + x * 3 + 0] = pInserted[y * strideI + x * 3 + 0];
								pDestination[y * strideD + x * 3 + 1] = pInserted[y * strideI + x * 3 + 1];
								pDestination[y * strideD + x * 3 + 2] = pInserted[y * strideI + x * 3 + 2];
							}
							catch (Exception)
							{
								throw;
							}
						}
				}
			}
			finally
			{
				if (destinationBitmapData != null)
					dest.UnlockBits(destinationBitmapData);
				if (insertedBitmapData != null)
					insertedImage.UnlockBits(insertedBitmapData);
			}
		}
		#endregion

		#region Insert32bppInto8bpp()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="insertedImage"></param>
		/// <param name="location">Upper left corner.</param>
		static unsafe void Insert32bppInto8bpp(Bitmap dest, Bitmap insertedImage, Point location)
		{
			BitmapData destinationBitmapData = null;
			BitmapData insertedBitmapData = null;

			try
			{
				Rectangle rect = new Rectangle(location.X, location.Y, insertedImage.Width, insertedImage.Height);

				if (rect.X >= 0 && rect.Y >= 0 && rect.Right <= dest.Width && rect.Bottom <= dest.Height)
				{
					destinationBitmapData = dest.LockBits(rect, ImageLockMode.WriteOnly, dest.PixelFormat);
					insertedBitmapData = insertedImage.LockBits(new Rectangle(0, 0, insertedImage.Width, insertedImage.Height), ImageLockMode.ReadOnly, insertedImage.PixelFormat);

					byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
					byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

					int strideD = destinationBitmapData.Stride;
					int strideI = insertedBitmapData.Stride;

					int width = insertedBitmapData.Width;
					int height = insertedBitmapData.Height;
					double opaque;
					double r, g, b, gray;

					for (int y = 0; y < height; y++)
						for (int x = 0; x < width; x++)
						{
							try
							{
								opaque = (pInserted[y * strideI + x * 4 + 3] / 255.0);
								b = pDestination[y * strideD + x] + ((pInserted[y * strideI + x * 4 + 0] - pDestination[y * strideD + x]) * opaque);
								g = pDestination[y * strideD + x] + ((pInserted[y * strideI + x * 4 + 1] - pDestination[y * strideD + x]) * opaque);
								r = pDestination[y * strideD + x] + ((pInserted[y * strideI + x * 4 + 2] - pDestination[y * strideD + x]) * opaque);
								gray = (0.3 * r + 0.59 * g + 0.11 * b);

								if (gray < 0)
									pDestination[y * strideD + x] = 0;
								else if (gray > 255)
									pDestination[y * strideD + x] = 255;
								else
									pDestination[y * strideD + x] = (byte)gray;
							}
							catch (Exception)
							{
								throw;
							}
						}
				}
				else if (rect.IntersectsWith(new Rectangle(0, 0, dest.Width, dest.Height)))
				{
					destinationBitmapData = dest.LockBits(new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat);
					insertedBitmapData = insertedImage.LockBits(new Rectangle(0, 0, insertedImage.Width, insertedImage.Height), ImageLockMode.ReadOnly, insertedImage.PixelFormat);

					byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
					byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

					int strideD = destinationBitmapData.Stride;
					int strideI = insertedBitmapData.Stride;

					int xFrom = Math.Max(0, rect.Left);
					int xTo = Math.Min(dest.Width, rect.Right);
					int yFrom = Math.Max(0, rect.Top);
					int yTo = Math.Min(dest.Height, rect.Bottom);

					int sourceX = xFrom - rect.Left;
					int sourceY = yFrom - rect.Top;

					double opaque;
					double r, g, b, gray;

					for (int y = yFrom; y < yTo; y++)
					{
						byte* pInsertedCurrent = pInserted + (((y - yFrom) + sourceY) * strideI) + (sourceX * 4);

						for (int x = xFrom; x < xTo; x++)
						{
							try
							{
								opaque = pInsertedCurrent[3] / 255.0;
								b = pDestination[y * strideD + x] + ((pInsertedCurrent[0] - pDestination[y * strideD + x]) * opaque);
								g = pDestination[y * strideD + x] + ((pInsertedCurrent[1] - pDestination[y * strideD + x]) * opaque);
								r = pDestination[y * strideD + x] + ((pInsertedCurrent[2] - pDestination[y * strideD + x]) * opaque);
								gray = (0.3 * r + 0.59 * g + 0.11 * b);

								if (gray < 0)
									pDestination[y * strideD + x] = 0;
								else if (gray > 255)
									pDestination[y * strideD + x] = 255;
								else
									pDestination[y * strideD + x] = (byte)gray;

								pInsertedCurrent += 4;
							}
							catch (Exception)
							{
								throw;
							}
						}
					}
				}
			}
			finally
			{
				if (destinationBitmapData != null)
					dest.UnlockBits(destinationBitmapData);
				if (insertedBitmapData != null)
					insertedImage.UnlockBits(insertedBitmapData);
			}
		}
		#endregion

		#region Insert32bppInto24bpp()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="insertedImage"></param>
		/// <param name="location">Upper left corner.</param>
		static unsafe void Insert32bppInto24bpp(Bitmap dest, Bitmap insertedImage, Point location)
		{
			BitmapData destinationBitmapData = null;
			BitmapData insertedBitmapData = null;

			try
			{
				Rectangle rect = new Rectangle(location.X, location.Y, insertedImage.Width, insertedImage.Height);

				if (rect.X >= 0 && rect.Y >= 0 && rect.Right <= dest.Width && rect.Bottom <= dest.Height)
				{
					destinationBitmapData = dest.LockBits(rect, ImageLockMode.WriteOnly, dest.PixelFormat);
					insertedBitmapData = insertedImage.LockBits(new Rectangle(0, 0, insertedImage.Width, insertedImage.Height), ImageLockMode.ReadOnly, insertedImage.PixelFormat);

					byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
					byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

					int strideD = destinationBitmapData.Stride;
					int strideI = insertedBitmapData.Stride;

					int width = insertedBitmapData.Width;
					int height = insertedBitmapData.Height;
					double opaque;

					for (int y = 0; y < height; y++)
						for (int x = 0; x < width; x++)
						{
							try
							{
								opaque = (pInserted[y * strideI + x * 4 + 3] / 255.0);
								pDestination[y * strideD + x * 3 + 0] = (byte)(pDestination[y * strideD + x * 3 + 0] + ((pInserted[y * strideI + x * 4 + 0] - pDestination[y * strideD + x * 3 + 0]) * opaque));
								pDestination[y * strideD + x * 3 + 1] = (byte)(pDestination[y * strideD + x * 3 + 1] + ((pInserted[y * strideI + x * 4 + 1] - pDestination[y * strideD + x * 3 + 1]) * opaque));
								pDestination[y * strideD + x * 3 + 2] = (byte)(pDestination[y * strideD + x * 3 + 2] + ((pInserted[y * strideI + x * 4 + 2] - pDestination[y * strideD + x * 3 + 2]) * opaque));
							}
							catch (Exception)
							{
								throw;
							}
						}
				}
				else if (rect.IntersectsWith(new Rectangle(0, 0, dest.Width, dest.Height)))
				{
					destinationBitmapData = dest.LockBits(new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat);
					insertedBitmapData = insertedImage.LockBits(new Rectangle(0, 0, insertedImage.Width, insertedImage.Height), ImageLockMode.ReadOnly, insertedImage.PixelFormat);

					byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
					byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

					int strideD = destinationBitmapData.Stride;
					int strideI = insertedBitmapData.Stride;

					int xFrom = Math.Max(0, rect.Left);
					int xTo = Math.Min(dest.Width, rect.Right);
					int yFrom = Math.Max(0, rect.Top);
					int yTo = Math.Min(dest.Height, rect.Bottom);

					int sourceX = xFrom - rect.Left;
					int sourceY = yFrom - rect.Top;
					
					double opaque;

					for (int y = yFrom; y < yTo; y++)
					{
						byte* pInsertedCurrent = pInserted + (((y - yFrom) + sourceY) * strideI) + (sourceX * 4);				
						
						for (int x = xFrom; x < xTo; x++)
						{
							try
							{
								opaque = pInsertedCurrent[3] / 255.0;
								pDestination[y * strideD + x * 3 + 0] = (byte)(pDestination[y * strideD + x * 3 + 0] + ((pInsertedCurrent[0] - pDestination[y * strideD + x * 3 + 0]) * opaque));
								pDestination[y * strideD + x * 3 + 1] = (byte)(pDestination[y * strideD + x * 3 + 1] + ((pInsertedCurrent[1] - pDestination[y * strideD + x * 3 + 1]) * opaque));
								pDestination[y * strideD + x * 3 + 2] = (byte)(pDestination[y * strideD + x * 3 + 2] + ((pInsertedCurrent[2] - pDestination[y * strideD + x * 3 + 2]) * opaque));

								pInsertedCurrent += 4;
							}
							catch (Exception)
							{
								throw;
							}
						}
					}
				}
			}
			finally
			{
				if (destinationBitmapData != null)
					dest.UnlockBits(destinationBitmapData);
				if (insertedBitmapData != null)
					insertedImage.UnlockBits(insertedBitmapData);
			}
		}
		#endregion

		#region Insert32bppInto32bpp()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="insertedImage"></param>
		/// <param name="location">Upper left corner.</param>
		static unsafe void Insert32bppInto32bpp(Bitmap dest, Bitmap insertedImage, Point location)
		{
			BitmapData destinationBitmapData = null;
			BitmapData insertedBitmapData = null;

			try
			{
				Rectangle rect = new Rectangle(location.X, location.Y, insertedImage.Width, insertedImage.Height);

				if (rect.X >= 0 && rect.Y >= 0 && rect.Right <= dest.Width && rect.Bottom <= dest.Height)
				{
					destinationBitmapData = dest.LockBits(rect, ImageLockMode.WriteOnly, dest.PixelFormat);
					insertedBitmapData = insertedImage.LockBits(new Rectangle(0, 0, insertedImage.Width, insertedImage.Height), ImageLockMode.ReadOnly, insertedImage.PixelFormat);

					byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
					byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

					int strideD = destinationBitmapData.Stride;
					int strideI = insertedBitmapData.Stride;

					int width = insertedBitmapData.Width;
					int height = insertedBitmapData.Height;

					double opaqueS, opaqueR;
					double temp;

					for (int y = 0; y < height; y++)
						for (int x = 0; x < width; x++)
						{
							opaqueS = pInserted[y * strideI + x * 4 + 3];

							if (opaqueS > 0)
							{
								if (opaqueS == 255)
								{
									pDestination[y * strideD + x * 4 + 0] = pInserted[y * strideI + x * 4 + 0];
									pDestination[y * strideD + x * 4 + 1] = pInserted[y * strideI + x * 4 + 1];
									pDestination[y * strideD + x * 4 + 2] = pInserted[y * strideI + x * 4 + 2];
									pDestination[y * strideD + x * 4 + 3] = pInserted[y * strideI + x * 4 + 3];
								}
								else
								{
									opaqueS = opaqueS / 255.0;
									opaqueR = pDestination[y * strideD + x * 4 + 3] / 255.0;
									temp = (1 - opaqueS) * opaqueR;
									pDestination[y * strideD + x * 4 + 0] = (byte)((pDestination[y * strideD + x * 4 + 0] * temp + pInserted[y * strideI + x * 4 + 0] * opaqueS) / (opaqueS + (temp)));
									pDestination[y * strideD + x * 4 + 1] = (byte)((pDestination[y * strideD + x * 4 + 1] * temp + pInserted[y * strideI + x * 4 + 1] * opaqueS) / (opaqueS + (temp)));
									pDestination[y * strideD + x * 4 + 2] = (byte)((pDestination[y * strideD + x * 4 + 2] * temp + pInserted[y * strideI + x * 4 + 2] * opaqueS) / (opaqueS + (temp)));
									pDestination[y * strideD + x * 4 + 3] = (byte)((opaqueS + temp) * 255);
								}
							}
						}
				}
				else if (rect.IntersectsWith(new Rectangle(0, 0, dest.Width, dest.Height)))
				{
					destinationBitmapData = dest.LockBits(new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat);
					insertedBitmapData = insertedImage.LockBits(new Rectangle(0, 0, insertedImage.Width, insertedImage.Height), ImageLockMode.ReadOnly, insertedImage.PixelFormat);

					byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
					byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

					int strideD = destinationBitmapData.Stride;
					int strideI = insertedBitmapData.Stride;

					int xFrom = Math.Max(0, rect.Left);
					int xTo = Math.Min(dest.Width, rect.Right);
					int yFrom = Math.Max(0, rect.Top);
					int yTo = Math.Min(dest.Height, rect.Bottom);

					int sourceX = xFrom - rect.Left;
					int sourceY = yFrom - rect.Top;
					
					double opaqueS, opaqueR;

					for (int y = yFrom; y < yTo; y++)
					{
						byte* pInsertedCurrent = pInserted + (((y - yFrom) + sourceY) * strideI) + (sourceX * 4);

						for (int x = xFrom; x < xTo; x++)
						{
							opaqueS = pInsertedCurrent[3];

							if (opaqueS > 0)
							{
								if (opaqueS == 255)
								{
									pDestination[y * strideD + x * 4 + 0] = pInsertedCurrent[0];
									pDestination[y * strideD + x * 4 + 1] = pInsertedCurrent[1];
									pDestination[y * strideD + x * 4 + 2] = pInsertedCurrent[2];
									pDestination[y * strideD + x * 4 + 3] = pInsertedCurrent[3];
								}
								else
								{
									opaqueS = opaqueS / 255.0;
									opaqueR = pDestination[y * strideD + x * 4 + 3] / 255.0;
									pDestination[y * strideD + x * 4 + 0] = (byte)((pDestination[y * strideD + x * 4 + 0] * (1 - opaqueS) * opaqueR + pInsertedCurrent[0] * opaqueS) / (opaqueS + ((1 - opaqueS) * opaqueR)));
									pDestination[y * strideD + x * 4 + 1] = (byte)((pDestination[y * strideD + x * 4 + 1] * (1 - opaqueS) * opaqueR + pInsertedCurrent[1] * opaqueS) / (opaqueS + ((1 - opaqueS) * opaqueR)));
									pDestination[y * strideD + x * 4 + 2] = (byte)((pDestination[y * strideD + x * 4 + 2] * (1 - opaqueS) * opaqueR + pInsertedCurrent[2] * opaqueS) / (opaqueS + ((1 - opaqueS) * opaqueR)));
									pDestination[y * strideD + x * 4 + 3] = (byte)((opaqueS + (1 - opaqueS) * opaqueR) * 255);
								}
							}

							pInsertedCurrent += 4;
						}
					}
				}
			}
			finally
			{
				if (destinationBitmapData != null)
					dest.UnlockBits(destinationBitmapData);
				if (insertedBitmapData != null)
					insertedImage.UnlockBits(insertedBitmapData);
			}
		}
		#endregion

		#endregion

	}
}
