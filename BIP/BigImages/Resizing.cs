using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageProcessing.Languages;

namespace ImageProcessing.BigImages
{
	public class Resizing
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public Resizing()
		{
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Resize()
		/// <summary>
		/// if source is black/white, result is black/white
		/// </summary>
		public void Resize(string sourceFile, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, double zoom)
		{
			using (ItDecoder itDecoder = new ItDecoder(sourceFile))
			{
				Resize(itDecoder, destPath, imageFormat, zoom, 0, 0, new ColorD(127, 127, 127));
			}
		}


		/// <summary>
		/// if source is black/white, result is black/white
		/// </summary>
		public void Resize(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, double zoom)
		{
			Resize(itDecoder, destPath, imageFormat, zoom, 0, 0, new ColorD(127,127,127));
		}

		/// <summary>
		/// if source is black/white, result is black/white
		/// </summary>
		public void Resize(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, double zoom, double brightnessDelta, double contrastDelta, ColorD histogramMean)
		{
			while (true)
			{
				try
				{
					Resize(itDecoder, destPath, imageFormat, zoom, brightnessDelta, contrastDelta, histogramMean, ImageProcessing.Resizing.ResizeMode.Quality);
					return;
				}
				catch (Exception ex)
				{
					if (ex.Message.ToLower().Contains("out of memory") && (Misc.minBufferSize < Misc.bufferSize))
						Misc.bufferSize = Math.Max(Misc.minBufferSize, Misc.bufferSize / 2);
					else
						throw ex;
				}
			}
		}
		#endregion

		#region ResizeToBitmap()
		/// <summary>
		/// if source is black/white, result is black/white
		/// </summary>
		/// <param name="itDecoder"></param>
		/// <param name="clip"></param>
		/// <param name="zoom"></param>
		/// <returns></returns>
		public Bitmap ResizeToBitmap(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip, double zoom)
		{
			int width = Convert.ToInt32(itDecoder.Width * zoom);
			int height = Convert.ToInt32(itDecoder.Height * zoom);

			try
			{
				if (clip.IsEmpty)
					clip = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
				else
					clip.Intersect(new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));
				
				int stripHeightMax = Misc.GetStripHeightMax(itDecoder, zoom);

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

							Bitmap resize = null;

							GC.Collect();
							using (Bitmap strip = itDecoder.GetClip(new Rectangle(clip.X, sourceTopLine, clip.Width, stripHeight)))
							{
								resize = ImageProcessing.Resizing.Resize(strip, Rectangle.Empty, zoom, ImageProcessing.Resizing.ResizeMode.Quality);
								itDecoder.ReleaseAllocatedMemory(strip);
							}

							bitmapsToMerge.Add(resize);

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

						GC.Collect();
						using (Bitmap strip = itDecoder.GetClip(clip))
						{
							resampled = ImageProcessing.Resizing.Resize(strip, Rectangle.Empty, zoom, ImageProcessing.Resizing.ResizeMode.Quality);
							itDecoder.ReleaseAllocatedMemory(strip);
						}

						if (ProgressChanged != null)
							ProgressChanged(1);

						if (itDecoder.DpiX > 0)
							resampled.SetResolution(itDecoder.DpiX * (float)zoom, itDecoder.DpiY * (float)zoom);
						
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
		#endregion

		#region ResizeFast()
		/// <summary>
		/// if source is black/white, result is black/white
		/// </summary>
		/*public void ResizeFast(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageFormat imageFormat,
			ImageProcessing.IpSettings.ItImage.TiffCompression tiffCompression, byte jpegQuality, double zoom)
		{
			ImageProcessing.FileFormat.IImageFormat iImageFormat = ImageProcessing.BigImages.ItDecoder.GetImageFormat(imageFormat, tiffCompression, jpegQuality);

			ResizeFast(itDecoder, destPath, iImageFormat, zoom);
		}*/

		/// <summary>
		/// if source is black/white, result is black/white
		/// </summary>
		public void ResizeFast(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, double zoom)
		{
			ResizeFast(itDecoder, destPath, imageFormat, zoom, 0, 0, new ColorD(127, 127, 127));
		}

		/// <summary>
		/// if source is black/white, result is black/white
		/// </summary>
		public void ResizeFast(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, double zoom, double brightnessDelta, double contrastDelta, ColorD histogramMean)
		{
			Resize(itDecoder, destPath, imageFormat, zoom, brightnessDelta, contrastDelta, histogramMean, ImageProcessing.Resizing.ResizeMode.Fast);
		}
		#endregion

		#region ResizeToBitmapFast()
		/// <summary>
		/// if source is black/white, result is black/white
		/// </summary>
		/// <param name="itDecoder"></param>
		/// <param name="clip"></param>
		/// <param name="zoom"></param>
		/// <returns></returns>
		public Bitmap ResizeToBitmapFast(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip, double zoom)
		{
			return ResizeToBitmap(itDecoder, clip, zoom, ImageProcessing.Resizing.ResizeMode.Fast);
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region Resize()
		private void Resize(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, double zoom, double brightnessDelta, double contrastDelta, ColorD histogramMean, ImageProcessing.Resizing.ResizeMode resizeMode)
		{
			if ((itDecoder.PixelFormat == PixelFormat.Format1bppIndexed) && (imageFormat is ImageProcessing.FileFormat.Jpeg))
				throw new Exception(BIPStrings.CanTCreate1BitPixelJPEGFile_STR);

			Bitmap resize = null;
			BitmapData bitmapData = null;

			int topLine = 0;
			int stripHeightMax = Misc.GetStripHeightMax(itDecoder, zoom);

			int width = 0;
			int height = 0;

			//getting result size
			int sourceTopLine = 0;

			do
			{
				int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - sourceTopLine);

				// if little chunk is left, add it to the current strip.
				if ((itDecoder.Height - stripHeight - sourceTopLine > 0) && (itDecoder.Height - stripHeight - sourceTopLine < ((1.0 / zoom) * 3)))
					stripHeight = itDecoder.Height - sourceTopLine;

				Rectangle rect = new Rectangle(0, sourceTopLine, itDecoder.Width, stripHeight);
				Size resultSize = ImageProcessing.Resizing.GetResultBitmapSize(rect.Size, zoom, itDecoder.PixelFormat);

				width = Math.Max(width, resultSize.Width);
				height += resultSize.Height;

				sourceTopLine += stripHeight;
			} while (sourceTopLine < itDecoder.Height);

			int step = 0;

			try
			{
				using (ItEncoder itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat, width, height, itDecoder.DpiX * zoom, itDecoder.DpiY * zoom))
				{
					itEncoder.SetPalette(itDecoder);

					step = 1;

					for (sourceTopLine = 0; sourceTopLine < itDecoder.Height; sourceTopLine += stripHeightMax)
					{
						int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - sourceTopLine);

						// if little chunk is left, add it to the current strip.
						if ((itDecoder.Height - stripHeight - sourceTopLine > 0) && (itDecoder.Height - stripHeight - sourceTopLine < ((1.0 / zoom) * 3)))
						{
							stripHeight = itDecoder.Height - sourceTopLine;
							stripHeightMax = itDecoder.Height - sourceTopLine;
						}

						step = 2;

						GC.Collect();
						try
						{
							using (Bitmap strip = itDecoder.GetClip(new Rectangle(0, sourceTopLine, itDecoder.Width, stripHeight)))
							{
								step = 3;
								resize = ImageProcessing.Resizing.Resize(strip, Rectangle.Empty, zoom, resizeMode);
								step = 4;

								itDecoder.ReleaseAllocatedMemory(strip);
							}
						}
						catch (Exception ex)
						{
							throw ex;
						}

						if (itDecoder.PixelsFormat != PixelsFormat.FormatBlackWhite)
						{
							step = 5;
							if (brightnessDelta != 0 && contrastDelta != 0)
							{
								step = 6;
								ImageProcessing.BrightnessContrast.Go(resize, brightnessDelta, contrastDelta, histogramMean);
								step = 7;
							}
							else if (brightnessDelta != 0)
							{
								step = 8;
								ImageProcessing.Brightness.Go(resize, brightnessDelta);
								step = 9;
							}
							else if (contrastDelta != 0)
							{
								step = 10;
								ImageProcessing.Contrast.Go(resize, contrastDelta, histogramMean);
								step = 11;
							}
						}

						unsafe
						{
							step = 12;
							int resizeHeight = (int)Math.Min(resize.Height, itEncoder.Height - topLine);
							bitmapData = resize.LockBits(new Rectangle(0, 0, resize.Width, resize.Height), ImageLockMode.ReadOnly, resize.PixelFormat);
							step = 13;
							itEncoder.Write(resizeHeight, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
							step = 14;
							resize.UnlockBits(bitmapData);
							bitmapData = null;
							topLine += resizeHeight;
						}

						step = 15;
						resize.Dispose();
						resize = null;

						step = 16;
						if (ProgressChanged != null)
							ProgressChanged((sourceTopLine + stripHeight) / (float)itDecoder.Height);
					}
				}
			}
			catch (Exception ex)
			{				
				try { if (File.Exists(destPath)) File.Delete(destPath); }
				catch { }

				throw new Exception(BIPStrings.CanTResizeBitmap_STR + step.ToString() + ": " + ex.Message, ex);
			}
			finally
			{
				if (bitmapData != null)
					resize.UnlockBits(bitmapData);
				if (resize != null)
					resize.Dispose();
			}
		}
		#endregion

		#region ResizeToBitmap()
		/// <summary>
		/// if source is black/white, result is black/white
		/// </summary>
		/// <param name="itDecoder"></param>
		/// <param name="clip"></param>
		/// <param name="zoom"></param>
		/// <returns></returns>
		public Bitmap ResizeToBitmap(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip, double zoom, ImageProcessing.Resizing.ResizeMode resizeMode)
		{
			int width = Convert.ToInt32(itDecoder.Width * zoom);
			int height = Convert.ToInt32(itDecoder.Height * zoom);

			try
			{
				if (clip.IsEmpty)
					clip = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
				else
					clip.Intersect(new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));

				int stripHeightMax = Misc.GetStripHeightMax(itDecoder, zoom);

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

							Bitmap resize = null;

							GC.Collect();
							using (Bitmap strip = itDecoder.GetClip(new Rectangle(clip.X, sourceTopLine, clip.Width, stripHeight)))
							{
								resize = ImageProcessing.Resizing.Resize(strip, Rectangle.Empty, zoom, resizeMode);
								itDecoder.ReleaseAllocatedMemory(strip);
							}

							bitmapsToMerge.Add(resize);

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

						GC.Collect();
						using (Bitmap strip = itDecoder.GetClip(clip))
						{
							resampled = ImageProcessing.Resizing.Resize(strip, Rectangle.Empty, zoom, ImageProcessing.Resizing.ResizeMode.Fast);
							itDecoder.ReleaseAllocatedMemory(strip);
						}

						if (ProgressChanged != null)
							ProgressChanged(1);

						if (itDecoder.DpiX > 0)
							resampled.SetResolution(itDecoder.DpiX * (float)zoom, itDecoder.DpiY * (float)zoom);

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
		#endregion
	
		#endregion

	}
}
