using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageProcessing.Languages;

namespace ImageProcessing.BigImages
{
	public class ResizingAndResampling
	{
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public ResizingAndResampling()
		{
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region ResizeAndResample()
		public void ResizeAndResample(string source, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, ImageProcessing.PixelsFormat pixelsFormat, double zoom, double brightnessDelta, double contrastDelta)
		{
			using (ImageProcessing.BigImages.ItDecoder itDecoder = new ItDecoder(source))
			{
				ResizeAndResample(itDecoder, destPath, imageFormat, pixelsFormat, zoom, brightnessDelta, contrastDelta);
			}
		}

		public void ResizeAndResample(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, ImageProcessing.PixelsFormat pixelsFormat, double zoom)
		{
			ResizeAndResample(itDecoder, destPath, imageFormat, pixelsFormat, zoom, 0, 0);
		}

		public void ResizeAndResample(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, ImageProcessing.PixelsFormat pixelsFormat, double zoom, double brightnessDelta, double contrastDelta)
		{
			if ((pixelsFormat == PixelsFormat.FormatBlackWhite) && (imageFormat is ImageProcessing.FileFormat.Jpeg))
				throw new Exception(BIPStrings.CanTCreate1BitPixelJPEGFile_STR);

			int width = 0;
			int height = 0;

			//getting result size
			int sourceTopLine = 0;
			int stripHeightMax = Misc.GetStripHeightMax(itDecoder, zoom);

			do
			{
				int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - sourceTopLine);

				// if little chunk is left, add it to the current strip.
				if ((itDecoder.Height - stripHeight - sourceTopLine > 0) && (itDecoder.Height - stripHeight - sourceTopLine < ((1.0 / zoom) * 3)))
					stripHeight = itDecoder.Height - sourceTopLine;

				Rectangle rect = new Rectangle(0, sourceTopLine, itDecoder.Width, stripHeight);
				Size resultSize = ImageProcessing.Resizing.GetResultBitmapSize(rect.Size, zoom, Misc.GetPixelFormat(pixelsFormat));

				width = Math.Max(width, resultSize.Width);
				height += resultSize.Height;

				sourceTopLine += stripHeight;
			} while (sourceTopLine < itDecoder.Height);

			try
			{
				using (ImageProcessing.BigImages.ItEncoder itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, pixelsFormat, width, height, itDecoder.DpiX * zoom, itDecoder.DpiY * zoom))
				{
					itEncoder.SetPalette(itDecoder);

					int topLine = 0;

					for (sourceTopLine = 0; sourceTopLine < itDecoder.Height; sourceTopLine += stripHeightMax)
					{
						int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - sourceTopLine);

						// if little chunk is left, add it to the current strip.
						if (itDecoder.Height - stripHeight - sourceTopLine < ((1.0 / zoom) * 3))
						{
							stripHeight = itDecoder.Height - sourceTopLine;
							stripHeightMax = itDecoder.Height - sourceTopLine;
						}

						Bitmap resampled = null;

						using (Bitmap strip = itDecoder.GetClip(new Rectangle(0, sourceTopLine, itDecoder.Width, stripHeight)))
						{
							using (Bitmap resized = ImageProcessing.Resizing.Resize(strip, Rectangle.Empty, zoom, ImageProcessing.Resizing.ResizeMode.Quality))
							{
								itDecoder.ReleaseAllocatedMemory(strip);
								resampled = ImageProcessing.Resampling.Resample(resized, pixelsFormat);
							}
						}

						if (itDecoder.PixelsFormat != PixelsFormat.FormatBlackWhite)
						{
							if (brightnessDelta != 0 && contrastDelta != 0)
								ImageProcessing.BrightnessContrast.Go(resampled, brightnessDelta, contrastDelta);
							else if (brightnessDelta != 0)
								ImageProcessing.Brightness.Go(resampled, brightnessDelta);
							else if (contrastDelta != 0)
								ImageProcessing.Contrast.Go(resampled, contrastDelta);
						}

						unsafe
						{
							int resizeHeight = (int)Math.Min(resampled.Height, itEncoder.Height - topLine);
							BitmapData bitmapData = null;

							try
							{
								bitmapData = resampled.LockBits(new Rectangle(0, 0, resampled.Width, resampled.Height), ImageLockMode.ReadWrite, resampled.PixelFormat);
								itEncoder.Write(resizeHeight, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
							}
							finally
							{
								if (bitmapData != null)
									resampled.UnlockBits(bitmapData);
							}

							topLine += resizeHeight;
						}

						resampled.Dispose();
						resampled = null;

						if (ProgressChanged != null)
							ProgressChanged((sourceTopLine + stripHeight) / (float)itDecoder.Height);
					}
				}
			}
			catch (Exception ex)
			{
				try { if (File.Exists(destPath)) File.Delete(destPath); }
				catch { }

				throw ex;
			}
		}
		#endregion

		#region ResizeAndResampleToBitmap()
		public Bitmap ResizeAndResampleToBitmap(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.PixelsFormat pixelsFormat, double zoom)
		{
			return ResizeAndResampleToBitmap(itDecoder, Rectangle.Empty, pixelsFormat, zoom);
			
		}

		public Bitmap ResizeAndResampleToBitmap(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip, ImageProcessing.PixelsFormat pixelsFormat, double zoom)
		{
			try
			{
				if (clip.IsEmpty)
					clip = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
				else
					clip.Intersect(new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));
				
				int stripHeightMax = Misc.GetStripHeightMax(itDecoder);

				if (stripHeightMax < clip.Height)
				{
					List<Bitmap> bitmapsToMerge = new List<Bitmap>();

					for (int sourceTopLine = clip.Y; sourceTopLine < clip.Bottom; sourceTopLine += stripHeightMax)
					{
						try
						{
							int stripHeight = Math.Min(stripHeightMax, clip.Bottom - sourceTopLine);

							// if little chunk is left, add it to the current strip.
							if (clip.Bottom - stripHeight - sourceTopLine < ((1.0 / zoom) * 3))
							{
								stripHeight = clip.Bottom - sourceTopLine;
								stripHeightMax = clip.Bottom - sourceTopLine;
							}

							Bitmap resampled;

							using (Bitmap strip = itDecoder.GetClip(new Rectangle(clip.X, sourceTopLine, clip.Width, stripHeight)))
							{
								using (Bitmap resized = ImageProcessing.Resizing.Resize(strip, Rectangle.Empty, zoom, ImageProcessing.Resizing.ResizeMode.Quality))
								{
									itDecoder.ReleaseAllocatedMemory(strip);
									resampled = ImageProcessing.Resampling.Resample(resized, pixelsFormat);
								}
							}

							bitmapsToMerge.Add(resampled);

							if (ProgressChanged != null)
								ProgressChanged((sourceTopLine + stripHeight - clip.Y) / (float)clip.Height);
						}
						finally
						{
							itDecoder.ReleaseAllocatedMemory(null);
						}
					}

					Bitmap merge = ImageProcessing.Merging.MergeVertically(bitmapsToMerge);

					foreach (Bitmap b in bitmapsToMerge)
						b.Dispose();

					if (itDecoder.DpiX > 0)
						merge.SetResolution(itDecoder.DpiX * (float)zoom, itDecoder.DpiY * (float)zoom);
					return merge;
				}
				else
				{
					try
					{
						Bitmap resampled;
						
						using (Bitmap strip = itDecoder.GetClip(clip))
						{
							using (Bitmap resized = ImageProcessing.Resizing.Resize(strip, Rectangle.Empty, zoom, ImageProcessing.Resizing.ResizeMode.Quality))
							{
								itDecoder.ReleaseAllocatedMemory(strip);
								resampled = ImageProcessing.Resampling.Resample(resized, ImageProcessing.PixelsFormat.Format8bppGray);
							}
						}
						
						if (itDecoder.DpiX > 0)
							resampled.SetResolution(itDecoder.DpiX * (float)zoom, itDecoder.DpiY * (float)zoom);
						
						if (ProgressChanged != null)
							ProgressChanged(1);

						return resampled;
					}
					finally
					{					
						itDecoder.ReleaseAllocatedMemory(null);
					}
				}
			}
			finally
			{
			}
		}

		public Bitmap ResizeAndResampleToBitmap(Bitmap source, Rectangle clip, ImageProcessing.PixelsFormat pixelsFormat, double zoom)
		{
			try
			{
				if (clip.IsEmpty)
					clip = new Rectangle(0, 0, source.Width, source.Height);
				else
					clip.Intersect(new Rectangle(0, 0, source.Width, source.Height));

				Bitmap resampled;

				using (Bitmap resized = ImageProcessing.Resizing.Resize(source, clip, zoom, ImageProcessing.Resizing.ResizeMode.Quality))
				{
					resampled = ImageProcessing.Resampling.Resample(resized, pixelsFormat);
				}

				return resampled;
			}
			finally
			{
			}
		}
		#endregion
	
		#endregion

		//PRIVATE METHODS
		#region private methods

		#region RaiseProgressChanged()
		private void RaiseProgressChanged(float progress)
		{
			if (ProgressChanged != null)
				ProgressChanged(0.33F);
		}
		#endregion

		#endregion

	}
}
