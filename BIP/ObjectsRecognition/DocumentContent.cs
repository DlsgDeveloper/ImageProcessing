using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.ObjectsRecognition
{
	public class DocumentContent
	{
		PointF? pTmiddle = null;
		PointF? pBmiddle = null;
		int dpi;
		byte[,] mask;
		bool[,] borderMask;


		#region constructor
		public DocumentContent(byte[,] mask, bool[,] borderMask, int dpi)
		{
			this.mask = mask;
			this.borderMask = borderMask;
			this.dpi = dpi;
		}

		public DocumentContent(byte[,] mask, bool[,] borderMask, Point pTmiddle, Point pBmiddle, int dpi)
			: this(mask, borderMask, dpi)
		{
			this.pTmiddle = new PointF(pTmiddle.X / (float)mask.GetLength(1), pTmiddle.Y / (float)mask.GetLength(0));
			this.pBmiddle = new PointF(pBmiddle.X / (float)mask.GetLength(1), pBmiddle.Y / (float)mask.GetLength(0)); 
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties

		public bool TwoPagesBook { get { return (pTmiddle != null && pBmiddle != null); } }

		/// <summary>
		/// returns top bookfold point on image in percents [<0,1>, <0,1>]
		/// </summary>
		public PointF PointT { get { return pTmiddle.Value; } }
		/// <summary>
		/// returns top bookfold point on image in percents [<0,1>, <0,1>]
		/// </summary>
		public PointF PointB { get { return pBmiddle.Value; } }

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region GetRidOfBorder()
		/// <summary>
		/// 1 bit per pixel bitmap 
		/// </summary>
		/// <param name="bitmap">1 bit per pixel bitmap </param>
		public unsafe void GetRidOfBorder(Bitmap bitmap)
		{
			int cellSize = (int) Math.Ceiling(bitmap.HorizontalResolution / dpi);
			BitmapData bitmapData = null;
			float ratio = bitmap.HorizontalResolution / dpi;

			try
			{
				int x, y;
				int width = bitmap.Width;
				int height = bitmap.Height;
				int xMax = (int)Math.Min(width, ratio * borderMask.GetLength(1) - 1);
				int yMax = (int)Math.Min(height, ratio * borderMask.GetLength(0) - 1);

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
				int stride = bitmapData.Stride;
				//
				for (y = 0; y < yMax; y++)
				{
					for (x = 0; x < xMax; x++)
					{
						if ((pSource[y * stride + x / 8] & (0x80 >> (x & 0x07))) > 0)
						{
							if (borderMask[(int)(y / ratio), (int)(x / ratio)])
								pSource[y * stride + x / 8] &= (byte)~(0x80 >> (x & 0x07));
						}
					}
				}

				// right
				for (y = 0; y < yMax; y++)
					for (x = xMax; x < width; x++)
						pSource[y * stride + x / 8] &= (byte)~(0x80 >> (x & 0x07));

				// bottom
				for (y = yMax; y < height; y++)
					for (x = 0; x < stride; x++)
						pSource[y * stride + x / 8] = 0;
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region DrawMaskToFile()
		private unsafe void DrawMaskToFile(string file)
		{
			int width = mask.GetLength(1);
			int height = mask.GetLength(0);

			Bitmap bitmap = null;
			BitmapData bitmapData = null;

			try
			{
				bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
				int stride = bitmapData.Stride;

				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						pSource[y * stride + x] = mask[y, x];
					}
				}

			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			bitmap.Palette = Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
			bitmap.Save(file, ImageFormat.Png);
			bitmap.Dispose();
		}
		#endregion

		#region DrawBorderToFile()
		private unsafe void DrawBorderToFile(string file, bool[,] mask)
		{
			int width = mask.GetLength(1);
			int height = mask.GetLength(0);

			Bitmap bitmap = null;
			BitmapData bitmapData = null;

			try
			{
				bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
				int stride = bitmapData.Stride;

				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						if (mask[y, x])
							pSource[y * stride + x] = 255;
					}
				}

			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			bitmap.Palette = Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
			bitmap.Save(file, ImageFormat.Png);
			bitmap.Dispose();
		}
		#endregion

		#endregion
	}
}
