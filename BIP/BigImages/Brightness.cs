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
	public class Brightness
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public Brightness()
		{
		}
		#endregion

		
		//PUBLIC METHODS
		#region public methods

		#region ChangeBrightness()
		public void ChangeBrightness(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, double brightnessDelta)
		{
			if ((itDecoder.PixelsFormat == PixelsFormat.FormatBlackWhite) && (imageFormat is ImageProcessing.FileFormat.Jpeg))
				throw new Exception(BIPStrings.CanTCreate1BitPixelJPEGFile_STR);

			ImageProcessing.BigImages.ItEncoder itEncoder = null;
			int width = itDecoder.Width;
			int height = itDecoder.Height;

			try
			{
				itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat, width, height, itDecoder.DpiX, itDecoder.DpiY);

				itEncoder.SetPalette(itDecoder);

				int topLine = 0;
				int stripHeightMax = Misc.GetStripHeightMax(itDecoder);

				for (int sourceTopLine = 0; sourceTopLine < itDecoder.Height; sourceTopLine += stripHeightMax)
				{
					int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - sourceTopLine);

					using (Bitmap strip = itDecoder.GetClip(new Rectangle(0, sourceTopLine, itDecoder.Width, stripHeight)))
					{
						if (brightnessDelta != 0)
							ImageProcessing.Brightness.Go(strip, brightnessDelta);

						unsafe
						{

							BitmapData bitmapData = null;
							try
							{
								int resizeHeight = (int)Math.Min(strip.Height, itEncoder.Height - topLine);
								bitmapData = strip.LockBits(new Rectangle(0, 0, strip.Width, strip.Height), ImageLockMode.ReadWrite, strip.PixelFormat);
								itEncoder.Write(resizeHeight, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
								strip.UnlockBits(bitmapData);
								bitmapData = null;
								topLine += resizeHeight;
							}
							finally
							{
								if (bitmapData != null)
									strip.UnlockBits(bitmapData);
							}
						}
					}

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

		#region ChangeBrightnessS2N()
		public void ChangeBrightnessS2N(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, double brightnessDelta)
		{
			if ((itDecoder.PixelsFormat == PixelsFormat.FormatBlackWhite) && (imageFormat is ImageProcessing.FileFormat.Jpeg))
				throw new Exception(BIPStrings.CanTCreate1BitPixelJPEGFile_STR);

			ImageProcessing.BigImages.ItEncoder itEncoder = null;
			int width = itDecoder.Width;
			int height = itDecoder.Height;

			try
			{
				itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat, width, height, itDecoder.DpiX, itDecoder.DpiY);

				itEncoder.SetPalette(itDecoder);

				int topLine = 0;
				int stripHeightMax = Misc.GetStripHeightMax(itDecoder);

				for (int sourceTopLine = 0; sourceTopLine < itDecoder.Height; sourceTopLine += stripHeightMax)
				{
					int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - sourceTopLine);

					using (Bitmap strip = itDecoder.GetClip(new Rectangle(0, sourceTopLine, itDecoder.Width, stripHeight)))
					{
						if (brightnessDelta != 0)
							ImageProcessing.Brightness.GoS2N(strip, brightnessDelta);

						unsafe
						{

							BitmapData bitmapData = null;
							try
							{
								int resizeHeight = (int)Math.Min(strip.Height, itEncoder.Height - topLine);
								bitmapData = strip.LockBits(new Rectangle(0, 0, strip.Width, strip.Height), ImageLockMode.ReadWrite, strip.PixelFormat);
								itEncoder.Write(resizeHeight, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
								strip.UnlockBits(bitmapData);
								bitmapData = null;
								topLine += resizeHeight;
							}
							finally
							{
								if (bitmapData != null)
									strip.UnlockBits(bitmapData);
							}
						}
					}

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


	}
}
