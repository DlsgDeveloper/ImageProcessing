using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessingApp
{
	class BookMaker
	{
		public delegate void ProgressHnd(double progress);
		public event ProgressHnd ProgressChanged;

		#region constructor
		public BookMaker()
		{

		}
		#endregion

	
		#region Run()
		public void Run(int width, int height, List<FileInfo> files, Rectangle clip, string destFolder, Point locationL, Point locationR)
		{			
			for (int i = 178; i < files.Count; i = i + 2)
			{
				Bitmap bitmapL = new Bitmap(files[i].FullName);
				Bitmap bitmapR = null;

				if (clip != Rectangle.Empty)
				{
					clip = Rectangle.Intersect(clip, new Rectangle(0, 0, bitmapL.Width, bitmapL.Height));

					Bitmap crop = ImageProcessing.ImageCopier.Copy(bitmapL, clip);
					bitmapL.Dispose();
					bitmapL = crop;
				}

				if (i < files.Count - 1)
				{
					bitmapR = new Bitmap(files[i + 1].FullName);

					if (clip != Rectangle.Empty)
					{
						clip = Rectangle.Intersect(clip, new Rectangle(0, 0, bitmapR.Width, bitmapR.Height));

						Bitmap crop = ImageProcessing.ImageCopier.Copy(bitmapR, clip);
						bitmapR.Dispose();
						bitmapR = crop;
					}
				}

				using (Bitmap dest = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
				{
					MakeBitmapWhite(dest);
					ImageProcessing.BitmapOperations.Insertor.Insert(dest, bitmapL, locationL);

					if (bitmapR != null)
						ImageProcessing.BitmapOperations.Insertor.Insert(dest, bitmapR, locationR);

					dest.Save(string.Format("{0}\\{1}.png", destFolder, (i / 2).ToString("000")), ImageFormat.Png);
				}

				bitmapL.Dispose();

				if (bitmapR != null)
					bitmapR.Dispose();

				if (ProgressChanged != null)
					ProgressChanged((i + 2.0) / files.Count);
			}
		}
		#endregion


		// PRIVATE METHODS
		#region private methods

		#region MakeBitmapWhite()
		private static void MakeBitmapWhite(Bitmap source)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			int resultWidth = source.Width;
			int resultHeight = source.Height;
			int x, y;

			BitmapData sourceData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);

			int sStride = sourceData.Stride;

			try
			{
				unsafe
				{
					byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
					byte* pOrig;

					for (y = 0; y < resultHeight - 3; y++)
					{
						pOrig = pOrigScan0 + y * sStride;

						for (x = 0; x < sStride; x++)
						{
							*(pOrig++) = 0xFF;
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
			}

#if DEBUG
			//Console.WriteLine("MakeBitmapWhite(): " + DateTime.Now.Subtract(start).ToString());
#endif
		}
		#endregion

		#endregion

	}

}
