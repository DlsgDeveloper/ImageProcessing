using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageProcessing.Languages;

namespace ImageProcessing.BigImages
{
	public class ThumbnailCreator
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public ThumbnailCreator()
		{
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Go()
		/*public void Go(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageFormat imageFormat,
			ImageProcessing.IpSettings.ItImage.TiffCompression tiffCompression, byte jpegQuality, double zoom)
		{
			ImageProcessing.FileFormat.IImageFormat iImageFormat = ImageProcessing.BigImages.ItDecoder.GetImageFormat(imageFormat, tiffCompression, jpegQuality);
			
			Go(itDecoder, destPath, iImageFormat, zoom);
		}*/

		public void Go(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, double zoom)
		{
			if(zoom > 1)
				throw new Exception(BIPStrings.ZoomMustBeEqualOrLessThan1_STR);

			if (itDecoder.PixelFormat != PixelFormat.Format1bppIndexed)
			{
				ImageProcessing.BigImages.Resizing resizing = new Resizing();
				resizing.ProgressChanged += delegate(float progress)
				{
					if (ProgressChanged != null)
						ProgressChanged(progress);
				};

				resizing.Resize(itDecoder, destPath, imageFormat, zoom);
			}
			else
			{
				try
				{
					using (ImageProcessing.BigImages.ItEncoder itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, PixelsFormat.Format8bppIndexed,
							(int)Math.Max(1, itDecoder.Width * zoom), (int)Math.Max(1, itDecoder.Height * zoom), itDecoder.DpiX, itDecoder.DpiY))
					{
						CreateThumbnailFrom1bpp(itDecoder, itEncoder, zoom);
					}

					if (ProgressChanged != null)
						ProgressChanged(1);
				}
				catch (Exception ex)
				{
					try
					{
						if (File.Exists(destPath))
							File.Delete(destPath);
					}
					catch { }

					throw ex;
				}							
			}
		}
		#endregion
	
		#endregion

		//PRIVATE METHODS
		#region private methods

		#region CreateThumbnailFrom1bpp()
		private unsafe void CreateThumbnailFrom1bpp(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, double zoom)
		{
			int pixelsPerSample = (int)Math.Max(1, 1 / zoom);
			int stripHeight = Math.Min(200000000 / itEncoder.Width, 200000000 / itDecoder.Width * 8) / 8 * 8;

			//copying data
			Bitmap source = null;
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			int sourceW = itDecoder.Width;
			int sourceH = itDecoder.Height;

			//strip top in image coordinates
			for (int stripY = 0; stripY < itEncoder.Height; stripY += stripHeight)
			{
				int stripB = Math.Min(itEncoder.Height, stripY + stripHeight);

				try
				{
					result = new Bitmap(itEncoder.Width, stripB - stripY, PixelFormat.Format8bppIndexed);
					resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					Rectangle sourceClip = Rectangle.FromLTRB(0, (int)(stripY / zoom), itDecoder.Width, Math.Min(itDecoder.Height, (int)(stripB / zoom + 1)));
					Rectangle resultClip = Rectangle.FromLTRB(0, stripY, itEncoder.Width, stripB);

					source = itDecoder.GetClip(sourceClip);
					sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					int strideS = sourceData.Stride;
					int strideR = resultData.Stride;

					int whitePixels = 0;
					int x, y, i, j;

					// copy bitmap
					try
					{
						for (y = 0; y < resultData.Height; y++)
						{
							int top = (int)((y + resultClip.Top) / zoom) - sourceClip.Top;

							for (x = 0; x < resultData.Width; x++)
							{
								int left = (int)(x / zoom);
								whitePixels = 0;
								int iRight = (left + pixelsPerSample < sourceW) ? left + pixelsPerSample : sourceW;
								int jBottom = (top + pixelsPerSample < sourceH) ? top + pixelsPerSample : sourceH;

								for (i = left; i < iRight; i++)
									for (j = top; j < jBottom; j++)
									{
										if ((pSource[j * strideS + i / 8] & (0x80 >> (i & 0x7))) > 0)
											whitePixels++;
									}

								pResult[y * strideR + x] = (byte)((whitePixels * 255) / (pixelsPerSample * pixelsPerSample));
							}
						}
					}
					catch (Exception ex)
					{
						throw ex;
					}
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
					if (resultData != null)
					{
						itEncoder.Write(stripB - stripY, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
						result.UnlockBits(resultData);
						resultData = null;
					}
					if (result != null)
					{
						result.Dispose();
						result = null;
					}
				}

				if (ProgressChanged != null)
					ProgressChanged(stripB / (float)itEncoder.Height);
			}
		}
		#endregion

		#endregion

	}
}
