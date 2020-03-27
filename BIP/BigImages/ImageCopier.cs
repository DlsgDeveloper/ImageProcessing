using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.BigImages
{
	/// <summary>
	/// Summary description for Histogram.
	/// </summary>
	public class ImageCopier
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public ImageCopier()
		{
		}
		#endregion

		//	PUBLIC METHODS
		#region public methods

		#region Copy()
		public void Copy(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			Copy(itDecoder, destPath, imageFormat, Rectangle.Empty);
		}

		public void Copy(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, Rectangle clip)
		{
			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
			else
				clip = Rectangle.Intersect(clip, new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));

			ImageProcessing.BigImages.ItEncoder itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat,
				clip.Width, clip.Height, itDecoder.DpiX, itDecoder.DpiY);

			itEncoder.SetPalette(itDecoder);

			Bitmap source = null;
			BitmapData sourceData = null;

			int stripHeightMax = Misc.GetStripHeightMax(itDecoder);

			try
			{
				for (int stripY = clip.Y; stripY < clip.Bottom; stripY = stripY + stripHeightMax)
				{
					try
					{
						int stripHeight = Math.Min(stripHeightMax, clip.Bottom - stripY);
						source = itDecoder.GetClip(new Rectangle(clip.X, stripY, clip.Width, stripHeight));
						sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

						unsafe
						{
							itEncoder.Write(stripHeight, sourceData.Stride, (byte*)sourceData.Scan0.ToPointer());
						}

						if (ProgressChanged != null)
							ProgressChanged((stripY + stripHeight) / (float)itDecoder.Height);
					}
					finally
					{
						if (sourceData != null)
						{
							source.UnlockBits(sourceData);
							sourceData = null;
						}
						if (source != null)
						{
							source.Dispose();
							source = null;
						}
						itDecoder.ReleaseAllocatedMemory(source);
					}
				}

				if (ProgressChanged != null)
					ProgressChanged(1);
			}
			finally
			{
				itEncoder.Dispose();
			}
		}
		#endregion

		#endregion

	}
}