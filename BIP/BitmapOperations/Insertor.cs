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
			Rectangle sourceRect = new Rectangle(location.X, location.Y, insertedImage.Width, insertedImage.Height);

			if (sourceRect.IntersectsWith(new Rectangle(0, 0, dest.Width, dest.Height)))
			{
				Rectangle insertedRect;

				if (sourceRect.X >= 0 && sourceRect.Y >= 0 && sourceRect.Right <= dest.Width && sourceRect.Bottom <= dest.Height)
				{
					insertedRect = new Rectangle(0, 0, insertedImage.Width, insertedImage.Height);
				}
				else
				{
					sourceRect.Intersect(new Rectangle(0, 0, dest.Width, dest.Height));

					int clipRectX = Math.Max(0, sourceRect.X - location.X);
					int clipRectY = Math.Max(0, sourceRect.Y - location.Y);
					int clipRectW = Math.Min(sourceRect.Width, insertedImage.Width - clipRectX);
					int clipRectH = Math.Min(sourceRect.Height, insertedImage.Height - clipRectY);

					insertedRect = new Rectangle(clipRectX, clipRectY, clipRectW, clipRectH);
				}

				BitmapData destinationBitmapData = null;
				BitmapData insertedBitmapData = null;

				try
				{
					destinationBitmapData = dest.LockBits(sourceRect, ImageLockMode.WriteOnly, dest.PixelFormat);
					insertedBitmapData = insertedImage.LockBits(insertedRect, ImageLockMode.ReadOnly, insertedImage.PixelFormat);

					if (insertedImage.PixelFormat == PixelFormat.Format24bppRgb && dest.PixelFormat == PixelFormat.Format8bppIndexed)
						Insert24bppInto8bpp(destinationBitmapData, insertedBitmapData);
					else if (insertedImage.PixelFormat == PixelFormat.Format24bppRgb && dest.PixelFormat == PixelFormat.Format24bppRgb)
						Insert24bppInto24bpp(destinationBitmapData, insertedBitmapData);
					else if (insertedImage.PixelFormat == PixelFormat.Format32bppArgb && dest.PixelFormat == PixelFormat.Format8bppIndexed)
						Insert32bppInto8bpp(destinationBitmapData, insertedBitmapData);
					else if (insertedImage.PixelFormat == PixelFormat.Format32bppArgb && dest.PixelFormat == PixelFormat.Format24bppRgb)
						Insert32bppInto24bpp(destinationBitmapData, insertedBitmapData);
					else if (insertedImage.PixelFormat == PixelFormat.Format32bppArgb && dest.PixelFormat == PixelFormat.Format32bppArgb)
						Insert32bppInto32bpp(destinationBitmapData, insertedBitmapData);
					else if (insertedImage.PixelFormat == PixelFormat.Format1bppIndexed && dest.PixelFormat == PixelFormat.Format8bppIndexed)
						Insert1bppInto8bpp(destinationBitmapData, insertedBitmapData);
					else if (insertedImage.PixelFormat == PixelFormat.Format1bppIndexed && dest.PixelFormat == PixelFormat.Format24bppRgb)
						Insert1bppInto24bpp(destinationBitmapData, insertedBitmapData);
					else if (insertedImage.PixelFormat == PixelFormat.Format1bppIndexed && (dest.PixelFormat == PixelFormat.Format32bppRgb || dest.PixelFormat == PixelFormat.Format32bppArgb))
						Insert1bppInto32bpp(destinationBitmapData, insertedBitmapData);
					else
						throw new Exception(string.Format("Insertor, Insert(): Unsupported insertion of '{0}' to '{1}'.", insertedImage.PixelFormat, dest.PixelFormat));
				}
				finally
				{
					if (destinationBitmapData != null)
						dest.UnlockBits(destinationBitmapData);
					if (insertedBitmapData != null)
						insertedImage.UnlockBits(insertedBitmapData);
				}
			}
		}
		#endregion

		#endregion


		// PRIVATE METHODS
		#region private methods

		#region Insert24bppInto8bpp()
		static unsafe void Insert24bppInto8bpp(BitmapData destinationBitmapData, BitmapData insertedBitmapData)
		{
			byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
			byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

			int strideD = destinationBitmapData.Stride;
			int strideI = insertedBitmapData.Stride;

			int width = insertedBitmapData.Width;
			int height = insertedBitmapData.Height;

			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
					pDestination[y * strideD + x] = (byte)(0.11 * pInserted[y * strideI + x * 3 + 0] + 0.59 * pInserted[y * strideI + x * 3 + 1] + 0.3 * pInserted[y * strideI + x * 3 + 2]);
		}
		#endregion

		#region Insert24bppInto24bpp()
		static unsafe void Insert24bppInto24bpp(BitmapData destinationBitmapData, BitmapData insertedBitmapData)
		{
			byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
			byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

			int strideD = destinationBitmapData.Stride;
			int strideI = insertedBitmapData.Stride;

			int width = insertedBitmapData.Width;
			int height = insertedBitmapData.Height;

			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					pDestination[y * strideD + x * 3 + 0] = pInserted[y * strideI + x * 3 + 0];
					pDestination[y * strideD + x * 3 + 1] = pInserted[y * strideI + x * 3 + 1];
					pDestination[y * strideD + x * 3 + 2] = pInserted[y * strideI + x * 3 + 2];
				}
		}
		#endregion

		#region Insert32bppInto8bpp()
		static unsafe void Insert32bppInto8bpp(BitmapData destinationBitmapData, BitmapData insertedBitmapData)
		{
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
		}
		#endregion

		#region Insert32bppInto24bpp()
		static unsafe void Insert32bppInto24bpp(BitmapData destinationBitmapData, BitmapData insertedBitmapData)
		{
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
		#endregion

		#region Insert32bppInto32bpp()
		static unsafe void Insert32bppInto32bpp(BitmapData destinationBitmapData, BitmapData insertedBitmapData)
		{
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
		#endregion

		#region Insert1bppInto8bpp()
		static unsafe void Insert1bppInto8bpp(BitmapData destinationBitmapData, BitmapData insertedBitmapData)
		{
			byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
			byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

			int strideD = destinationBitmapData.Stride;
			int strideI = insertedBitmapData.Stride;

			int width = insertedBitmapData.Width;
			int height = insertedBitmapData.Height;

			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					if ((pInserted[y * strideI + x / 8] & (0x80 >> (x & 0x07))) > 0)
						pDestination[y * strideD + x] = (byte)255;
					else
						pDestination[y * strideD + x] = (byte)0;
				}
		}
		#endregion

		#region Insert1bppInto24bpp()
		static unsafe void Insert1bppInto24bpp(BitmapData destinationBitmapData, BitmapData insertedBitmapData)
		{
			byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
			byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

			int strideD = destinationBitmapData.Stride;
			int strideI = insertedBitmapData.Stride;

			int width = insertedBitmapData.Width;
			int height = insertedBitmapData.Height;

			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					if ((pInserted[y * strideI + x / 8] & (0x80 >> (x & 0x07))) > 0)
					{
						pDestination[y * strideD + x * 3 + 0] = (byte)255;
						pDestination[y * strideD + x * 3 + 1] = (byte)255;
						pDestination[y * strideD + x * 3 + 2] = (byte)255;
					}
					else
					{
						pDestination[y * strideD + x * 3 + 0] = (byte)0;
						pDestination[y * strideD + x * 3 + 1] = (byte)0;
						pDestination[y * strideD + x * 3 + 2] = (byte)0;
					}
				}
		}
		#endregion

		#region Insert1bppInto32bpp()
		static unsafe void Insert1bppInto32bpp(BitmapData destinationBitmapData, BitmapData insertedBitmapData)
		{
			byte* pDestination = (byte*)destinationBitmapData.Scan0.ToPointer();
			byte* pInserted = (byte*)insertedBitmapData.Scan0.ToPointer();

			int strideD = destinationBitmapData.Stride;
			int strideI = insertedBitmapData.Stride;

			int width = insertedBitmapData.Width;
			int height = insertedBitmapData.Height;

			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					if ((pInserted[y * strideI + x / 8] & (0x80 >> (x & 0x07))) > 0)
					{
						pDestination[y * strideD + x * 3 + 0] = (byte)255;
						pDestination[y * strideD + x * 3 + 1] = (byte)255;
						pDestination[y * strideD + x * 3 + 2] = (byte)255;
						pDestination[y * strideD + x * 3 + 3] = (byte)255;
					}
					else
					{
						pDestination[y * strideD + x * 3 + 0] = (byte)0;
						pDestination[y * strideD + x * 3 + 1] = (byte)0;
						pDestination[y * strideD + x * 3 + 2] = (byte)0;
						pDestination[y * strideD + x * 3 + 3] = (byte)255;
					}
				}
		}
		#endregion

		#endregion

	}
}
