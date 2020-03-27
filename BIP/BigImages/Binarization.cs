using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.BigImages
{
	public partial class Binarization
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public Binarization()
		{
		}
		#endregion

		#region class BinarizationParameters
		public class BinarizationParameters
		{
			public readonly int ThresholdDelta = 0;
			public readonly byte? ThresholdR = null;
			public readonly byte? ThresholdG = null;
			public readonly byte? ThresholdB = null;
			public readonly double Contrast = 0;
			public readonly ColorD HistogramMean = new ColorD(127, 127, 127);

			public BinarizationParameters()
			{
			}

			public BinarizationParameters(int thresholdDelta)
			{
				this.ThresholdDelta = thresholdDelta;
			}

			/// <summary>
			/// thresholds from interval [0,255]
			/// </summary>
			/// <param name="thresholdR"></param>
			/// <param name="thresholdG"></param>
			/// <param name="thresholdB"></param>
			public BinarizationParameters(byte thresholdR, byte thresholdG, byte thresholdB)
			{
				this.ThresholdR = thresholdR;
				this.ThresholdG = thresholdG;
				this.ThresholdB = thresholdB;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="contrast">In interval [-1, 1]</param>
			/// <param name="histogramMean">RGB values in interval [0, 255]</param>
			/// <returns></returns>
			public BinarizationParameters(double contrast, ColorD histogramMean)
			{
				this.Contrast = contrast;
				this.HistogramMean = histogramMean;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="thresholdR">In interval [1, 254]</param>
			/// <param name="thresholdG">In interval [1, 254]</param>
			/// <param name="thresholdB">In interval [1, 254]</param>
			/// <param name="contrast">In interval [-1, 1]</param>
			/// <param name="histogramMean">RGB values in interval [0, 255]</param>
			/// <returns></returns>
			public BinarizationParameters(byte thresholdR, byte thresholdG, byte thresholdB, double contrast, ColorD histogramMean)
			{
				this.ThresholdR = thresholdR;
				this.ThresholdG = thresholdG;
				this.ThresholdB = thresholdB;
				this.Contrast = contrast;
				this.HistogramMean = histogramMean;
			}
		}
		#endregion

		#region SaveBitonalBitmapToFile()
		private void SaveBitonalBitmapToFile(Bitmap bitonalBitmap, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			ImageProcessing.BigImages.ItEncoder itEncoder = null;
			int width = bitonalBitmap.Width;
			int height = bitonalBitmap.Height;

			try
			{
				itEncoder = new ImageProcessing.BigImages.ItEncoder(destFile, imageFormat, PixelsFormat.FormatBlackWhite,
					width, height, bitonalBitmap.HorizontalResolution, bitonalBitmap.VerticalResolution);

				unsafe
				{
					BitmapData bitmapData = null;

					try
					{
						bitmapData = bitonalBitmap.LockBits(new Rectangle(0, 0, bitonalBitmap.Width, bitonalBitmap.Height), ImageLockMode.ReadOnly, bitonalBitmap.PixelFormat);
						itEncoder.Write(height, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
					}
					finally
					{
						if (bitmapData != null)
							bitonalBitmap.UnlockBits(bitmapData);
					}
				}
			}
			catch (Exception ex)
			{
				try { if (itEncoder != null) itEncoder.Dispose(); }
				catch { }
				finally { itEncoder = null; }

				try { if (File.Exists(destFile)) File.Delete(destFile); }
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

		#region FireProgressEvent()
		private void FireProgressEvent(float progress)
		{
			if (ProgressChanged != null)
				ProgressChanged(progress);
		}
		#endregion

	}
}
