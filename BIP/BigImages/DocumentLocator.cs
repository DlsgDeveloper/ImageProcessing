using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections ;

namespace ImageProcessing.BigImages
{
	public class DocumentLocator
	{

		//PUBLIC METHODS
		#region public methods

		#region IsBlackAroundDocument()
		/*public static bool IsBlackAroundDocument(Bitmap bitmap, Rectangle clip, int minDelta)
		{
			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

			int inch = Convert.ToInt32(bitmap.HorizontalResolution);
			Bitmap bmpEdgeDetect = ImageProcessing.BinorizationThreshold.Binorize(bitmap, clip, 200, 200, 200);
			BitmapData bmpData = null;

			try
			{
				bmpData = bmpEdgeDetect.LockBits(new Rectangle(0, 0, bmpEdgeDetect.Width, bmpEdgeDetect.Height), ImageLockMode.ReadOnly, bmpEdgeDetect.PixelFormat);
				return (IsBlackTop(bmpData, inch, inch / 2, 0.6f) && IsBlackLeft(bmpData, inch, inch / 2, 0.6f)) && IsBlackRight(bmpData, inch, inch / 2, 0.6f);
			}
			finally
			{
				if ((bmpEdgeDetect != null) && (bmpData != null))
					bmpEdgeDetect.UnlockBits(bmpData);
			}
		}*/
		#endregion

		#region SeekDocument()
		public static bool SeekDocument(string file, Rectangle clip, int minDelta, out Rectangle documentRect)
		{
			Rectangle rectOriginal = clip;
			ImageProcessing.ImageFile.ImageInfo imageInfo = new ImageProcessing.ImageFile.ImageInfo(file);
			Bitmap bitmap;
			double zoom = 1.0;

			if (clip.IsEmpty == false)
				clip.Intersect(new Rectangle(0, 0, imageInfo.Width, imageInfo.Height));

			if (imageInfo.DpiH > 100)
			{
				ImageProcessing.BigImages.Resizing resizing = new Resizing();
				using (ItDecoder itDecoder = new ItDecoder(file))
				{
					zoom = 100.0 / itDecoder.DpiX;
					bitmap = resizing.ResizeToBitmap(itDecoder, clip, zoom);
					clip = Rectangle.Empty;
				}
			}
			else
			{
				if (clip.IsEmpty)
					bitmap = new Bitmap(file);
				else
				{
					Bitmap tmp = new Bitmap(file);
					bitmap = tmp.Clone(clip, tmp.PixelFormat);

					tmp.Dispose();
					clip = Rectangle.Empty;
				}
			}
			
			if(clip.IsEmpty == false)
				clip.Intersect(new Rectangle(0, 0, bitmap.Width, bitmap.Height));

			int inch = Convert.ToInt32(bitmap.HorizontalResolution);
			Bitmap bmpEdgeDetect = null;
			BitmapData bmpData = null;
			
			try
			{
				ImageProcessing.Histogram histogram = new ImageProcessing.Histogram(bitmap);

				//histogram.Show();
				bmpEdgeDetect = ImageProcessing.BinorizationThreshold.Binorize(bitmap, clip, histogram.ThresholdR, histogram.ThresholdG, histogram.ThresholdB);

#if SAVE_RESULTS
				bmpEdgeDetect.Save(Debug.SaveToDir + "Binorization.png");
#endif

				bmpData = bmpEdgeDetect.LockBits(new Rectangle(0, 0, bmpEdgeDetect.Width, bmpEdgeDetect.Height), ImageLockMode.ReadOnly, bmpEdgeDetect.PixelFormat);
				
				int? top = FindTop(bmpData, inch, inch, 0.6f);
				int? left = FindLeft(bmpData, inch, inch, 0.6f);
				int? right = FindRight(bmpData, inch, inch, 0.6f);
				int? bottom = FindBottom(bmpData, inch, inch, 0.6f);
			
				if (top.HasValue && left.HasValue && right.HasValue && bottom.HasValue)
				{
					documentRect = Rectangle.FromLTRB(left.Value, top.Value, right.Value, bottom.Value);

					if (zoom != 1)
						documentRect = new Rectangle(Convert.ToInt32(documentRect.X / zoom), Convert.ToInt32(documentRect.Y / zoom), Convert.ToInt32(documentRect.Width / zoom), Convert.ToInt32(documentRect.Height / zoom));

					documentRect.Offset(rectOriginal.Location);
					return true;
				}
			}
			finally
			{
				if ((bmpEdgeDetect != null) && (bmpData != null))
					bmpEdgeDetect.UnlockBits(bmpData);
				if (bmpEdgeDetect != null)
					bmpEdgeDetect.Dispose();

				if (bitmap != null)
					bitmap.Dispose();
			}

			documentRect = Rectangle.Empty;
			return false;
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region FindLeft()
		private static unsafe int? FindLeft(BitmapData bmpData, int blockWidth, int blockHeight, float percentageWhite)
		{
			byte* pOrig = (byte*)bmpData.Scan0.ToPointer();
			int stride = bmpData.Stride;

			for (int x = 0; x < (bmpData.Width - blockWidth); x += blockWidth / 3)
				for (int y = 0; y < (bmpData.Height - blockHeight); y += blockHeight / 2)
					if (RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight) >= percentageWhite)
					{
						for (int xSmall = x; xSmall < (x + blockWidth); xSmall += 3)
							for (int ySmall = 0; ySmall < (bmpData.Height - blockHeight); ySmall += blockHeight / 2)
								if (RasterProcessing.PercentageWhite(pOrig, stride, xSmall, ySmall, 5, blockHeight) >= percentageWhite)
									return xSmall;

						return x;
					}

			return null;
		}
		#endregion

		#region FindRight()
		private static unsafe int? FindRight(BitmapData bmpData, int blockWidth, int blockHeight, float percentageWhite)
		{
			byte* pOrig = (byte*)bmpData.Scan0.ToPointer();
			int stride = bmpData.Stride;

			for (int x = bmpData.Width; x >= blockHeight; x -= blockWidth / 3)
				for (int y = 0; y < (bmpData.Height - blockHeight); y += blockHeight / 2)
					if (RasterProcessing.PercentageWhite(pOrig, stride, x - blockWidth, y, blockWidth, blockHeight) >= percentageWhite)
					{
						for (int xSmall = x; xSmall > (x - blockHeight); xSmall -= 3)
							for (int ySmall = 0; ySmall < (bmpData.Height - blockHeight); ySmall += blockHeight / 2)
								if (RasterProcessing.PercentageWhite(pOrig, stride, xSmall - 5, ySmall, 5, blockHeight) >= percentageWhite)
									return xSmall;

						return x;
					}

			return null;
		}
		#endregion

		#region FindTop()
		private static unsafe int? FindTop(BitmapData bmpData, int blockWidth, int blockHeight, float percentageWhite)
		{
			byte* pOrig = (byte*)bmpData.Scan0.ToPointer();
			int stride = bmpData.Stride;
			
			for (int y = 0; y < (bmpData.Height - blockHeight); y += blockHeight / 3)
				for (int x = 0; x < (bmpData.Width - blockWidth); x += blockWidth / 2)
					if (RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight) >= percentageWhite)
					{
						for (int ySmall = y; ySmall < (y + blockHeight - 5); ySmall += 3)
							for (int xSmall = 0; xSmall < (bmpData.Width - blockWidth); xSmall += blockWidth / 2)
								if (RasterProcessing.PercentageWhite(pOrig, stride, xSmall, ySmall, blockWidth, 5) >= percentageWhite)
									return ySmall;

						return y;
					}
			
			return null;
		}
		#endregion

		#region FindBottom()
		private static unsafe int? FindBottom(BitmapData bmpData, int blockWidth, int blockHeight, float percentageWhite)
		{
			byte* pOrig = (byte*)bmpData.Scan0.ToPointer();
			int stride = bmpData.Stride;

			for (int y = (bmpData.Height - blockHeight); y >= 0 ; y -= blockHeight / 3)
				for (int x = 0; x < (bmpData.Width - blockWidth); x += blockWidth / 2)
				{
					if (RasterProcessing.PercentageWhite(pOrig, stride, x, y, blockWidth, blockHeight) >= percentageWhite)
					{
						for (int ySmall = y + blockHeight - 5; ySmall > y; ySmall -= 3)
							for (int xSmall = 0; xSmall < (bmpData.Width - blockWidth); xSmall += blockWidth / 2)
							{
								if (RasterProcessing.PercentageWhite(pOrig, stride, xSmall, ySmall, blockWidth, 5) >= percentageWhite)
									return ySmall;
							}

						return y;
					}
				}

			return null;
		}
		#endregion


		#region IsBlackLeft()
		private static unsafe bool IsBlackLeft(BitmapData bmpData, int blockWidth, int blockHeight, float percentageWhite)
		{
			byte* pOrig = (byte*)bmpData.Scan0.ToPointer();
			int stride = bmpData.Stride;
			for (int y = 0; y < (bmpData.Height - blockHeight); y += blockHeight / 3)
			{
				if (RasterProcessing.PercentageWhite(pOrig, stride, 0, y, blockWidth, blockHeight) >= percentageWhite)
				{
					return false;
				}
			}
			return true;
		}
		#endregion

		#region IsBlackRight()
		private static unsafe bool IsBlackRight(BitmapData bmpData, int blockWidth, int blockHeight, float percentageWhite)
		{
			byte* pOrig = (byte*)bmpData.Scan0.ToPointer();
			int stride = bmpData.Stride;
			for (int y = 0; y < (bmpData.Height - blockHeight); y += blockHeight / 3)
			{
				if (RasterProcessing.PercentageWhite(pOrig, stride, bmpData.Width - blockWidth, y, blockWidth, blockHeight) >= percentageWhite)
				{
					return false;
				}
			}
			return true;
		}
		#endregion

		#region IsBlackTop()
		private static unsafe bool IsBlackTop(BitmapData bmpData, int blockWidth, int blockHeight, float percentageWhite)
		{
			byte* pOrig = (byte*)bmpData.Scan0.ToPointer();
			int stride = bmpData.Stride;
			for (int x = 0; x < (bmpData.Width - blockWidth); x += blockWidth / 2)
			{
				if (RasterProcessing.PercentageWhite(pOrig, stride, x, 0, blockWidth, blockHeight) >= percentageWhite)
				{
					return false;
				}
			}
			return true;
		}
		#endregion

		#endregion

	}

}