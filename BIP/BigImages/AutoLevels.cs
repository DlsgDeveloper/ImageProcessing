using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using ImageProcessing.Languages;

namespace ImageProcessing.BigImages
{
	/*public class AutoLevels
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public AutoLevels()
		{
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Resample()
		public void Go(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			Go(itDecoder, destPath, imageFormat, System.Drawing.Rectangle.Empty);
		}

		public void Go(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat,
			System.Drawing.Rectangle clip)
		{
			Go(itDecoder, destPath, imageFormat, clip, 0.05, 0.05);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="itDecoder"></param>
		/// <param name="destPath"></param>
		/// <param name="imageFormat"></param>
		/// <param name="clip"></param>
		/// <param name="histogramOffsetMin">How many percent of images to cut from histogram to get minimum threshold. Recommended 0.05.</param>
		/// <param name="histogramOffsetMax">How many percent of images to cut from histogram to get maximum threshold. Recommended 0.05.</param>
		public void Go(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat,
			System.Drawing.Rectangle clip, double histogramOffsetMin, double histogramOffsetMax)
		{
			if ((itDecoder.PixelsFormat == PixelsFormat.FormatBlackWhite) && (imageFormat is ImageProcessing.FileFormat.Jpeg))
				throw new Exception(BIPStrings.CanTCreate1BitPixelJPEGFile_STR);

			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
			else
				clip.Intersect(new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));

			ImageProcessing.BigImages.ItEncoder itEncoder = null;
			
			try
			{
#if DEBUG
				DateTime start = DateTime.Now;
#endif

				ImageProcessing.HistogramGrayscale histogram = new ImageProcessing.HistogramGrayscale();
				
				histogram.Compute(itDecoder, clip);

#if DEBUG
				Console.WriteLine("Getting histogram: " + DateTime.Now.Subtract(start));
				start = DateTime.Now;
#endif

				byte minimum = 0, maximum = 255;
				uint min = 0, max = 0;
				uint totalPixels = 0;

				for (int i = 0; i < 256; i++)
					totalPixels += histogram.Array[i];

				for (int i = 0; i < 256; i++)
				{
					min += histogram.Array[i];

					if (min > totalPixels * histogramOffsetMin)
					{
						minimum = (byte)i;
						break;
					}
				}

				for (int i = 255; i >= 0; i--)
				{
					max += histogram.Array[i];

					if (max > totalPixels * histogramOffsetMax)
					{
						maximum = (byte)i;
						break;
					}
				}

#if DEBUG
				Console.WriteLine("Getting histogram's max and min: " + DateTime.Now.Subtract(start));
				start = DateTime.Now;
#endif


				int width = clip.Width;
				int height = clip.Height;

				itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat,
					width, height, itDecoder.DpiX, itDecoder.DpiY);

				itEncoder.SetPalette(itDecoder);

				int topLine = 0;
				int stripHeightMax = Misc.GetStripHeightMax(itDecoder, itEncoder);

				for (int sourceTopLine = clip.Top; sourceTopLine < clip.Bottom; sourceTopLine += stripHeightMax)
				{
					int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - sourceTopLine);

					Bitmap strip = itDecoder.GetClip(new Rectangle(clip.Left, sourceTopLine, clip.Width, stripHeight));

					ImageProcessing.AutoLevels.RunAlgorithm(strip, minimum, maximum);

					unsafe
					{
						int resizeHeight = (int)Math.Min(strip.Height, itEncoder.Height - topLine);
						BitmapData bitmapData = null;

						try
						{
							bitmapData = strip.LockBits(new Rectangle(0, 0, strip.Width, strip.Height), ImageLockMode.ReadWrite, strip.PixelFormat);
							itEncoder.Write(resizeHeight, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
						}
						finally
						{
							if (bitmapData != null)
								strip.UnlockBits(bitmapData);
						}

						topLine += resizeHeight;
					}

					itDecoder.ReleaseAllocatedMemory(strip);
					strip.Dispose();
					strip = null;

					if (ProgressChanged != null)
						ProgressChanged((sourceTopLine + stripHeight) / (float)itDecoder.Height);
				}
			}
			catch (Exception ex)
			{
				try { if (itEncoder != null) itEncoder.Dispose(); }
				catch { }
				finally { itEncoder = null; }

				try { if (File.Exists(destPath)) File.Delete(destPath); }
				catch { }

				throw ex;
			}
			finally
			{
				if (itEncoder != null)
					itEncoder.Dispose();
			}
		}
		#endregion
	
		#endregion

		//PRIVATE METHODS
		#region private methods

		#endregion


	}*/
}
