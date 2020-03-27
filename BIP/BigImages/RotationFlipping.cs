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
	public class RotationFlipping
	{
		static long maxBufferSize = 10000000;

		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		//PUBLIC METHODS
		#region public methods

		#region Go()
		/// <summary>
		/// Original bitmap is disposed.
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="rotateFlip"></param>
		public static void Go(ref Bitmap bitmap, System.Drawing.RotateFlipType rotateFlip)
		{
			if (rotateFlip == RotateFlipType.RotateNoneFlipNone || rotateFlip == RotateFlipType.Rotate180FlipXY)
			{
			}
			else if (rotateFlip == RotateFlipType.RotateNoneFlipX || rotateFlip == RotateFlipType.Rotate180FlipY)
			{				
				RotateNoneFlipX(bitmap);
			}
			else if (rotateFlip == RotateFlipType.RotateNoneFlipY || rotateFlip == RotateFlipType.Rotate180FlipX)
			{
				RotateNoneFlipY(bitmap);
			}
			else if (rotateFlip == RotateFlipType.RotateNoneFlipXY || rotateFlip == RotateFlipType.Rotate180FlipNone)
			{
				RotateNoneFlipXY(bitmap);
			}
			else if (rotateFlip == RotateFlipType.Rotate90FlipNone || rotateFlip == RotateFlipType.Rotate270FlipXY)
			{
				Rotate90FlipNone(ref bitmap);
			}
			else if (rotateFlip == RotateFlipType.Rotate90FlipX || rotateFlip == RotateFlipType.Rotate270FlipY)
			{
				Rotate90FlipX(ref bitmap);
			}
			else if (rotateFlip == RotateFlipType.Rotate90FlipY || rotateFlip == RotateFlipType.Rotate270FlipX)
			{
				Rotate90FlipY(ref bitmap);
			}
			else
			{
				Rotate90FlipXY(ref bitmap);
			}
		}


		public void Go(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat, System.Drawing.RotateFlipType rotateFlip)
		{
			if (rotateFlip == RotateFlipType.RotateNoneFlipNone || rotateFlip == RotateFlipType.Rotate180FlipXY)
			{
				ImageProcessing.BigImages.ImageCopier copier = new ImageCopier();
				copier.Copy(itDecoder, destPath, imageFormat);
			}
			else if (rotateFlip == RotateFlipType.RotateNoneFlipX || rotateFlip == RotateFlipType.Rotate180FlipY)
			{
				RotateNoneFlipX(itDecoder, destPath, imageFormat);
			}
			else if (rotateFlip == RotateFlipType.RotateNoneFlipY || rotateFlip == RotateFlipType.Rotate180FlipX)
			{
				RotateNoneFlipY(itDecoder, destPath, imageFormat);
			}
			else if (rotateFlip == RotateFlipType.RotateNoneFlipXY || rotateFlip == RotateFlipType.Rotate180FlipNone)
			{
				RotateNoneFlipXY(itDecoder, destPath, imageFormat);
			}
			else if (rotateFlip == RotateFlipType.Rotate90FlipNone || rotateFlip == RotateFlipType.Rotate270FlipXY)
			{
				Rotate90FlipNone(itDecoder, destPath, imageFormat);
			}
			else if (rotateFlip == RotateFlipType.Rotate90FlipX || rotateFlip == RotateFlipType.Rotate270FlipY)
			{
				Rotate90FlipX(itDecoder, destPath, imageFormat);
			}
			else if (rotateFlip == RotateFlipType.Rotate90FlipY || rotateFlip == RotateFlipType.Rotate270FlipX)
			{
				Rotate90FlipY(itDecoder, destPath, imageFormat);
			}
			else
			{
				Rotate90FlipXY(itDecoder, destPath, imageFormat);
			}
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region RotateNoneFlipX()
		private static void RotateNoneFlipX(Bitmap bitmap)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;
				
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height) , ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;

					int x, y;

					if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
					{
						byte r, g, b;
						
						for (y = 0; y < height; y++)
						{
							pCurrent = pSource + (y * stride);

							for (x = 0; x < width / 2; x++)
							{
								b = pCurrent[x * 3];
								g = pCurrent[x * 3 + 1];
								r = pCurrent[x * 3 + 2];
								pCurrent[x * 3] = pCurrent[(width - 1 - x) * 3];
								pCurrent[x * 3 + 1] = pCurrent[(width - 1 - x) * 3 + 1];
								pCurrent[x * 3 + 2] = pCurrent[(width - 1 - x) * 3 + 2];
								pCurrent[(width - 1 - x) * 3] = b;
								pCurrent[(width - 1 - x) * 3 + 1] = g;
								pCurrent[(width - 1 - x) * 3 + 2] = r;
							}
						}
					}
					else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						byte g;
						
						for (y = 0; y < height; y++)
						{
							pCurrent = pSource + (y * stride);

							for (x = 0; x < width / 2; x++)
							{
								g = pCurrent[x];
								pCurrent[x] = pCurrent[(width - 1 - x)];
								pCurrent[(width - 1 - x)] = g;
							}
						}
					}
					else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						int bite1, bite2;
						
						for (y = 0; y < height; y++)
						{
							pCurrent = pSource + (y * stride);

							for (x = 0; x < width / 2; x++)
							{
								bite1 = pCurrent[x / 8] & (byte)(0x80 >> (x & 0x7));
								bite2 = pCurrent[(width - 1 - x) / 8] & (byte)(0x80 >> ((width - 1 - x) & 0x7));
								
								if(bite2 > 0)
									pCurrent[x / 8] |= (byte)(0x80 >> (x & 0x7));
								else
									pCurrent[x / 8] &= (byte)(0xFF7F >> (x & 0x7));
								
								if (bite1 > 0)
									pCurrent[(width - 1 - x) / 8] |= (byte)(0x80 >> ((width - 1 - x) & 0x7));
								else
									pCurrent[(width - 1 - x) / 8] &= (byte)(0xFF7F >> ((width - 1 - x) & 0x7));
								
							}
						}
					}
					else
						throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}		
		#endregion

		#region RotateNoneFlipX()
		void RotateNoneFlipX(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat)
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
						RotateNoneFlipX(strip);
						
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

		#region RotateNoneFlipY()
		private static void RotateNoneFlipY(Bitmap bitmap)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrentT, pCurrentB;

					int x, y;

					if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
					{
						byte r, g, b;

						for (y = 0; y < height / 2; y++)
						{
							pCurrentT = pSource + (y * stride);
							pCurrentB = pSource + ((height - 1 - y) * stride);

							for (x = 0; x < width; x++)
							{
								b = pCurrentT[x * 3];
								g = pCurrentT[x * 3 + 1];
								r = pCurrentT[x * 3 + 2];
								pCurrentT[x * 3] = pCurrentB[x * 3];
								pCurrentT[x * 3 + 1] = pCurrentB[x * 3 + 1];
								pCurrentT[x * 3 + 2] = pCurrentB[x * 3 + 2];
								pCurrentB[x * 3] = b;
								pCurrentB[x * 3 + 1] = g;
								pCurrentB[x * 3 + 2] = r;
							}
						}
					}
					else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						byte g;

						for (y = 0; y < height / 2; y++)
						{
							pCurrentT = pSource + (y * stride);
							pCurrentB = pSource + ((height - 1 - y) * stride);

							for (x = 0; x < width; x++)
							{
								g = pCurrentT[x];
								pCurrentT[x] = pCurrentB[x];
								pCurrentB[x] = g;
							}
						}
					}
					else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						byte b;

						for (y = 0; y < height / 2; y++)
						{
							pCurrentT = pSource + (y * stride);
							pCurrentB = pSource + ((height - 1 - y) * stride);

							for (x = 0; x < stride; x++)
							{
								b = pCurrentT[x];

								pCurrentT[x] = pCurrentB[x];
								pCurrentB[x] = b;
							}
						}
					}
					else
						throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region RotateNoneFlipY()
		void RotateNoneFlipY(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat)
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

				for (int sourceBottomLine = itDecoder.Height; sourceBottomLine > 0; sourceBottomLine -= stripHeightMax)
				{
					int sourceTopLine = Math.Max(0, sourceBottomLine - stripHeightMax);
					int stripHeight = sourceBottomLine - sourceTopLine;

					using (Bitmap strip = itDecoder.GetClip(new Rectangle(0, sourceTopLine, itDecoder.Width, stripHeight)))
					{
						RotateNoneFlipY(strip);

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
						ProgressChanged(1.0F - (sourceTopLine / (float)itDecoder.Height));
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

		#region RotateNoneFlipXY()
		private static void RotateNoneFlipXY(Bitmap bitmap)
		{
			BitmapData bitmapData = null;

			try
			{
				int width = bitmap.Width;
				int height = bitmap.Height;

				bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int stride = bitmapData.Stride;

				unsafe
				{
					byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrentT, pCurrentB;

					int x, y;

					if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
					{
						byte r, g, b;

						for (y = 0; y < height / 2; y++)
						{
							pCurrentT = pSource + (y * stride);
							pCurrentB = pSource + ((height - 1 - y) * stride);

							for (x = 0; x < width / 2; x++)
							{
								b = pCurrentT[x * 3];
								g = pCurrentT[x * 3 + 1];
								r = pCurrentT[x * 3 + 2];
								pCurrentT[x * 3] = pCurrentB[(width - 1 - x) * 3];
								pCurrentT[x * 3 + 1] = pCurrentB[(width - 1 - x) * 3 + 1];
								pCurrentT[x * 3 + 2] = pCurrentB[(width - 1 - x) * 3 + 2];
								pCurrentB[(width - 1 - x) * 3] = b;
								pCurrentB[(width - 1 - x) * 3 + 1] = g;
								pCurrentB[(width - 1 - x) * 3 + 2] = r;

								b = pCurrentT[(width - 1 - x) * 3];
								g = pCurrentT[(width - 1 - x) * 3 + 1];
								r = pCurrentT[(width - 1 - x) * 3 + 2];
								pCurrentT[(width - 1 - x) * 3] = pCurrentB[x * 3];
								pCurrentT[(width - 1 - x) * 3 + 1] = pCurrentB[x * 3 + 1];
								pCurrentT[(width - 1 - x) * 3 + 2] = pCurrentB[x * 3 + 2];
								pCurrentB[x * 3] = b;
								pCurrentB[x * 3 + 1] = g;
								pCurrentB[x * 3 + 2] = r;
							}
						}

						if ((height % 2) == 1)
						{
							y = height / 2;
							pCurrentT = pSource + (y * stride);

							for (x = 0; x < width / 2; x++)
							{
								b = pCurrentT[x * 3];
								g = pCurrentT[x * 3 + 1];
								r = pCurrentT[x * 3 + 2];
								pCurrentT[x * 3] = pCurrentT[(width - 1 - x) * 3];
								pCurrentT[x * 3 + 1] = pCurrentT[(width - 1 - x) * 3 + 1];
								pCurrentT[x * 3 + 2] = pCurrentT[(width - 1 - x) * 3 + 2];
								pCurrentT[(width - 1 - x) * 3] = b;
								pCurrentT[(width - 1 - x) * 3 + 1] = g;
								pCurrentT[(width - 1 - x) * 3 + 2] = r;
							}
						}

						if ((width % 2) == 1)
						{
							x = width / 2;

							for (y = 0; y < height / 2; y++)
							{
								b = pSource[y * stride + x * 3];
								g = pSource[y * stride + x * 3 + 1];
								r = pSource[y * stride + x * 3 + 2];
								pSource[y * stride + x * 3] = pSource[(height - 1 - y) * stride + x * 3];
								pSource[y * stride + x * 3 + 1] = pSource[(height - 1 - y) * stride + x * 3 + 1];
								pSource[y * stride + x * 3 + 2] = pSource[(height - 1 - y) * stride + x * 3 + 2];
								pSource[(height - 1 - y) * stride + x * 3] = b;
								pSource[(height - 1 - y) * stride + x * 3 + 1] = g;
								pSource[(height - 1 - y) * stride + x * 3 + 2] = r;
							}
						}
					}
					else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						byte g;

						for (y = 0; y < height / 2; y++)
						{
							pCurrentT = pSource + (y * stride);
							pCurrentB = pSource + ((height - 1 - y) * stride);

							for (x = 0; x < width / 2; x++)
							{
								g = pCurrentT[x];
								pCurrentT[x] = pCurrentB[(width - 1 - x)];
								pCurrentB[(width - 1 - x)] = g;

								g = pCurrentT[(width - 1 - x)];
								pCurrentT[(width - 1 - x)] = pCurrentB[x];
								pCurrentB[x] = g;
							}
						}

						if ((height % 2) == 1)
						{
							y = height / 2;
							pCurrentT = pSource + (y * stride);

							for (x = 0; x < width / 2; x++)
							{
								g = pCurrentT[x];
								pCurrentT[x] = pCurrentT[(width - 1 - x)];
								pCurrentT[(width - 1 - x)] = g;
							}
						}

						if ((width % 2) == 1)
						{
							x = width / 2;

							for (y = 0; y < height / 2; y++)
							{
								g = pSource[y * stride + x];
								pSource[y * stride + x] = pSource[(height - 1 - y) * stride + x];
								pSource[(height - 1 - y) * stride + x] = g;
							}
						}
					}
					else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						int bite1, bite2;

						for (y = 0; y < height; y++)
						{
							pCurrentT = pSource + (y * stride);
							pCurrentB = pSource + ((height - 1 - y) * stride);

							for (x = 0; x < width / 2; x++)
							{
								bite1 = pCurrentT[x / 8] & (byte)(0x80 >> (x & 0x7));
								bite2 = pCurrentB[(width - 1 - x) / 8] & (byte)(0x80 >> ((width - 1 - x) & 0x7));

								if (bite2 > 0)
									pCurrentT[x / 8] |= (byte)(0x80 >> (x & 0x7));
								else
									pCurrentT[x / 8] &= (byte)(0xFF7F >> (x & 0x7));

								if (bite1 > 0)
									pCurrentB[(width - 1 - x) / 8] |= (byte)(0x80 >> ((width - 1 - x) & 0x7));
								else
									pCurrentB[(width - 1 - x) / 8] &= (byte)(0xFF7F >> ((width - 1 - x) & 0x7));
							}
						}

						if ((width % 2) == 1)
						{
							x = width / 2;

							for (y = 0; y < height / 2; y++)
							{
								pCurrentT = pSource + (y * stride);
								pCurrentB = pSource + ((height - 1 - y) * stride);

								bite1 = pCurrentT[x / 8] & (byte)(0x80 >> (x & 0x7));
								bite2 = pCurrentB[(width - 1 - x) / 8] & (byte)(0x80 >> ((width - 1 - x) & 0x7));

								if (bite2 > 0)
									pCurrentT[x / 8] |= (byte)(0x80 >> (x & 0x7));
								else
									pCurrentT[x / 8] &= (byte)(0xFF7F >> (x & 0x7));

								if (bite1 > 0)
									pCurrentB[(width - 1 - x) / 8] |= (byte)(0x80 >> ((width - 1 - x) & 0x7));
								else
									pCurrentB[(width - 1 - x) / 8] &= (byte)(0xFF7F >> ((width - 1 - x) & 0x7));
							}
						}
					}
					else
						throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}
		}
		#endregion

		#region RotateNoneFlipXY()
		void RotateNoneFlipXY(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat)
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

				for (int sourceBottomLine = itDecoder.Height; sourceBottomLine > 0; sourceBottomLine -= stripHeightMax)
				{
					int sourceTopLine = Math.Max(0, sourceBottomLine - stripHeightMax);
					int stripHeight = sourceBottomLine - sourceTopLine;

					using (Bitmap strip = itDecoder.GetClip(new Rectangle(0, sourceTopLine, itDecoder.Width, stripHeight)))
					{
						RotateNoneFlipXY(strip);

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
						ProgressChanged(1.0F - (sourceTopLine / (float)itDecoder.Height));
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

		#region Rotate90FlipNone()
		private static void Rotate90FlipNone(ref Bitmap bitmap)
		{
			BitmapData bitmapData = null;
			List<Bitmap> bitmapsToMerge = new List<Bitmap>();
			int sourceWidth = bitmap.Width;
			int sourceHeight = bitmap.Height;

			int maxWidth = (int) Math.Max(1, maxBufferSize / (bitmap.Height * ImageProcessing.Misc.BytesPerPixel(bitmap.PixelFormat)));
			int startX = 0;

			while (startX < sourceWidth)
			{
				try
				{
					int stripWidth = Math.Min(maxWidth, sourceWidth - startX);
					bitmapData = bitmap.LockBits(new Rectangle(startX, 0, stripWidth, sourceHeight), ImageLockMode.ReadOnly, bitmap.PixelFormat);
					startX += stripWidth;

					int strideS = bitmapData.Stride;

					Bitmap strip = null;
					BitmapData stripData = null;

					try
					{
						strip = new Bitmap(bitmapData.Height, bitmapData.Width, bitmap.PixelFormat);
						stripData = strip.LockBits(new Rectangle(0, 0, strip.Width, strip.Height), ImageLockMode.WriteOnly, strip.PixelFormat);
						int strideStrip = stripData.Stride;

						unsafe
						{
							byte* pSourceS = (byte*)bitmapData.Scan0.ToPointer();
							byte* pSourceStrip = (byte*)stripData.Scan0.ToPointer();
							byte* pCurrentS;

							int x, y;

							if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
							{
								for (y = 0; y < sourceHeight; y++)
								{
									pCurrentS = pSourceS + (y * strideS);

									for (x = 0; x < stripWidth; x++)
									{
										pSourceStrip[x * strideStrip + (sourceHeight - 1 - y) * 3] = pCurrentS[x * 3];
										pSourceStrip[x * strideStrip + (sourceHeight - 1 - y) * 3 + 1] = pCurrentS[x * 3 + 1];
										pSourceStrip[x * strideStrip + (sourceHeight - 1 - y) * 3 + 2] = pCurrentS[x * 3 + 2];
									}
								}
							}
							else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
							{
								strip.Palette = bitmap.Palette;
								
								for (y = 0; y < sourceHeight; y++)
								{
									pCurrentS = pSourceS + (y * strideS);

									for (x = 0; x < stripWidth; x++)
									{
										pSourceStrip[x * strideStrip + (sourceHeight - 1 - y)] = pCurrentS[x];
									}
								}
							}
							else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
							{
								int bite;

								for (y = 0; y < sourceHeight; y++)
								{
									pCurrentS = pSourceS + (y * strideS);

									for (x = 0; x < stripWidth; x++)
									{
										bite = pCurrentS[x / 8] & (byte)(0x80 >> (x & 0x7));

										if (bite > 0)
											pSourceStrip[x * strideStrip + (sourceHeight - 1 - y) / 8] |= (byte)(0x80 >> ((sourceHeight - 1 - y) & 0x7));
									}
								}
							}
							else
								throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
						}

						bitmapsToMerge.Add(strip);
					}
					finally
					{
						if (stripData != null)
							strip.UnlockBits(stripData);
					}
				}
				finally
				{
					if (bitmapData != null)
						bitmap.UnlockBits(bitmapData);
				}
			}

			bitmap.Dispose();

			Bitmap mergedBitmap = ImageProcessing.Merging.MergeVertically(bitmapsToMerge);

			foreach (Bitmap bitmapToMerge in bitmapsToMerge)
				bitmapToMerge.Dispose();

			bitmap = mergedBitmap;
		}
		#endregion

		#region Rotate90FlipNone()
		private void Rotate90FlipNone(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			if ((itDecoder.PixelsFormat == PixelsFormat.FormatBlackWhite) && (imageFormat is ImageProcessing.FileFormat.Jpeg))
				throw new Exception(BIPStrings.CanTCreate1BitPixelJPEGFile_STR);

			ImageProcessing.BigImages.ItEncoder itEncoder = null;
			int width = itDecoder.Height;
			int height = itDecoder.Width;

			try
			{
				itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat, width, height, itDecoder.DpiX, itDecoder.DpiY);
				itEncoder.SetPalette(itDecoder);

				int sourceWidth = itDecoder.Width;
				int sourceHeight = itDecoder.Height;

				int maxWidth = (int)Math.Max(1, maxBufferSize / (itDecoder.Height * ImageProcessing.Misc.BytesPerPixel(itDecoder.PixelFormat)));
				int startX = 0;

				while (startX < sourceWidth)
				{
					int stripWidth = Math.Min(maxWidth, sourceWidth - startX);
					Bitmap strip = null;

					try
					{
						strip = itDecoder.GetClip(new Rectangle(startX, 0, stripWidth, sourceHeight));

						Rotate90FlipNone(ref strip);

						unsafe
						{
							BitmapData bitmapData = null;

							try
							{
								//int resizeHeight = (int)Math.Min(strip.Height, itEncoder.Height - topLine);
								bitmapData = strip.LockBits(new Rectangle(0, 0, strip.Width, strip.Height), ImageLockMode.ReadWrite, strip.PixelFormat);
								itEncoder.Write(strip.Height, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
								strip.UnlockBits(bitmapData);
								bitmapData = null;
								//topLine += resizeHeight;
							}
							finally
							{
								if (bitmapData != null)
									strip.UnlockBits(bitmapData);
							}
						}
					}
					finally
					{
						if (strip != null)
							strip.Dispose();

					}

					if (ProgressChanged != null)
						ProgressChanged((startX + stripWidth) / (float)itDecoder.Width);

					startX += stripWidth;
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
	
		#region Rotate90FlipX()
		private static void Rotate90FlipX(ref Bitmap bitmap)
		{
			BitmapData bitmapData = null;
			List<Bitmap> bitmapsToMerge = new List<Bitmap>();
			int sourceWidth = bitmap.Width;
			int sourceHeight = bitmap.Height;

			int maxWidth = (int)Math.Max(1, maxBufferSize / (bitmap.Height * ImageProcessing.Misc.BytesPerPixel(bitmap.PixelFormat)));
			int startX = 0;

			while (startX < sourceWidth)
			{
				try
				{
					int stripWidth = Math.Min(maxWidth, sourceWidth - startX);
					bitmapData = bitmap.LockBits(new Rectangle(startX, 0, stripWidth, sourceHeight), ImageLockMode.ReadOnly, bitmap.PixelFormat);
					startX += stripWidth;

					int strideS = bitmapData.Stride;

					Bitmap strip = null;
					BitmapData stripData = null;

					try
					{
						strip = new Bitmap(bitmapData.Height, bitmapData.Width, bitmap.PixelFormat);
						stripData = strip.LockBits(new Rectangle(0, 0, strip.Width, strip.Height), ImageLockMode.WriteOnly, strip.PixelFormat);
						int strideStrip = stripData.Stride;

						unsafe
						{
							byte* pSourceS = (byte*)bitmapData.Scan0.ToPointer();
							byte* pSourceStrip = (byte*)stripData.Scan0.ToPointer();
							byte* pCurrentS;

							int x, y;

							if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
							{
								for (y = 0; y < sourceHeight; y++)
								{
									pCurrentS = pSourceS + (y * strideS);

									for (x = 0; x < stripWidth; x++)
									{
										pSourceStrip[x * strideStrip + y * 3] = pCurrentS[x * 3];
										pSourceStrip[x * strideStrip + y * 3 + 1] = pCurrentS[x * 3 + 1];
										pSourceStrip[x * strideStrip + y * 3 + 2] = pCurrentS[x * 3 + 2];
									}
								}
							}
							else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
							{
								strip.Palette = bitmap.Palette;

								for (y = 0; y < sourceHeight; y++)
								{
									pCurrentS = pSourceS + (y * strideS);

									for (x = 0; x < stripWidth; x++)
									{
										pSourceStrip[x * strideStrip + y] = pCurrentS[x];
									}
								}
							}
							else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
							{
								int bite;

								for (y = 0; y < sourceHeight; y++)
								{
									pCurrentS = pSourceS + (y * strideS);

									for (x = 0; x < stripWidth; x++)
									{
										bite = pCurrentS[x / 8] & (byte)(0x80 >> (x & 0x7));

										if (bite > 0)
											pSourceStrip[x * strideStrip + y / 8] |= (byte)(0x80 >> (y & 0x7));

									}
								}
							}
							else
								throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
						}

						bitmapsToMerge.Add(strip);
					}
					finally
					{
						if (stripData != null)
							strip.UnlockBits(stripData);
					}
				}
				finally
				{
					if (bitmapData != null)
						bitmap.UnlockBits(bitmapData);
				}
			}

			bitmap.Dispose();

			Bitmap mergedBitmap = ImageProcessing.Merging.MergeVertically(bitmapsToMerge);

			foreach (Bitmap bitmapToMerge in bitmapsToMerge)
				bitmapToMerge.Dispose();

			bitmap = mergedBitmap;
		}
		#endregion

		#region Rotate90FlipX()
		private void Rotate90FlipX(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			if ((itDecoder.PixelsFormat == PixelsFormat.FormatBlackWhite) && (imageFormat is ImageProcessing.FileFormat.Jpeg))
				throw new Exception(BIPStrings.CanTCreate1BitPixelJPEGFile_STR);

			ImageProcessing.BigImages.ItEncoder itEncoder = null;
			int width = itDecoder.Height;
			int height = itDecoder.Width;

			try
			{
				itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat, width, height, itDecoder.DpiX, itDecoder.DpiY);
				itEncoder.SetPalette(itDecoder);

				int sourceWidth = itDecoder.Width;
				int sourceHeight = itDecoder.Height;

				int maxWidth = (int)Math.Max(1, maxBufferSize / (itDecoder.Height * ImageProcessing.Misc.BytesPerPixel(itDecoder.PixelFormat)));
				int startX = 0;

				while (startX < sourceWidth)
				{
					int stripWidth = Math.Min(maxWidth, sourceWidth - startX);
					Bitmap strip = null;

					try
					{
						strip = itDecoder.GetClip(new Rectangle(startX, 0, stripWidth, sourceHeight));

						Rotate90FlipX(ref strip);

						unsafe
						{
							BitmapData bitmapData = null;

							try
							{
								//int resizeHeight = (int)Math.Min(strip.Height, itEncoder.Height - topLine);
								bitmapData = strip.LockBits(new Rectangle(0, 0, strip.Width, strip.Height), ImageLockMode.ReadWrite, strip.PixelFormat);
								itEncoder.Write(strip.Height, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
								strip.UnlockBits(bitmapData);
								bitmapData = null;
								//topLine += resizeHeight;
							}
							finally
							{
								if (bitmapData != null)
									strip.UnlockBits(bitmapData);
							}
						}
					}
					finally
					{
						if (strip != null)
							strip.Dispose();

					}

					if (ProgressChanged != null)
						ProgressChanged((startX + stripWidth) / (float)itDecoder.Width);

					startX += stripWidth;
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

		#region Rotate90FlipY()
		private static void Rotate90FlipY(ref Bitmap bitmap)
		{
			BitmapData bitmapData = null;
			List<Bitmap> bitmapsToMerge = new List<Bitmap>();
			int sourceWidth = bitmap.Width;
			int sourceHeight = bitmap.Height;

			int maxWidth = (int)Math.Max(1, maxBufferSize / (bitmap.Height * ImageProcessing.Misc.BytesPerPixel(bitmap.PixelFormat)));
			int startX = 0;

			while (startX < sourceWidth)
			{
				try
				{
					int stripWidth = Math.Min(maxWidth, sourceWidth - startX);
					bitmapData = bitmap.LockBits(new Rectangle(startX, 0, stripWidth, sourceHeight), ImageLockMode.ReadOnly, bitmap.PixelFormat);
					startX += stripWidth;

					int strideS = bitmapData.Stride;

					Bitmap strip = null;
					BitmapData stripData = null;

					try
					{
						strip = new Bitmap(bitmapData.Height, bitmapData.Width, bitmap.PixelFormat);
						stripData = strip.LockBits(new Rectangle(0, 0, strip.Width, strip.Height), ImageLockMode.WriteOnly, strip.PixelFormat);
						int strideStrip = stripData.Stride;

						unsafe
						{
							byte* pSourceS = (byte*)bitmapData.Scan0.ToPointer();
							byte* pSourceStrip = (byte*)stripData.Scan0.ToPointer();
							byte* pCurrentS;

							int x, y;

							if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
							{
								for (y = 0; y < sourceHeight; y++)
								{
									pCurrentS = pSourceS + (y * strideS);

									for (x = 0; x < stripWidth; x++)
									{
										pSourceStrip[(stripWidth - 1 - x) * strideStrip + (sourceHeight - 1 - y) * 3] = pCurrentS[x * 3];
										pSourceStrip[(stripWidth - 1 - x) * strideStrip + (sourceHeight - 1 - y) * 3 + 1] = pCurrentS[x * 3 + 1];
										pSourceStrip[(stripWidth - 1 - x) * strideStrip + (sourceHeight - 1 - y) * 3 + 2] = pCurrentS[x * 3 + 2];
									}
								}
							}
							else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
							{
								strip.Palette = bitmap.Palette;

								for (y = 0; y < sourceHeight; y++)
								{
									pCurrentS = pSourceS + (y * strideS);

									for (x = 0; x < stripWidth; x++)
									{
										pSourceStrip[(stripWidth - 1 - x) * strideStrip + (sourceHeight - 1 - y)] = pCurrentS[x];
									}
								}
							}
							else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
							{
								int bite;

								for (y = 0; y < sourceHeight; y++)
								{
									pCurrentS = pSourceS + (y * strideS);

									for (x = 0; x < stripWidth; x++)
									{
										bite = pCurrentS[x / 8] & (byte)(0x80 >> (x & 0x7));

										if (bite > 0)
											pSourceStrip[(stripWidth - 1 - x) * strideStrip + (sourceHeight - 1 - y) / 8] |= (byte)(0x80 >> ((sourceHeight - 1 - y) & 0x7));
									}
								}
							}
							else
								throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
						}

						bitmapsToMerge.Add(strip);
					}
					finally
					{
						if (stripData != null)
							strip.UnlockBits(stripData);
					}
				}
				finally
				{
					if (bitmapData != null)
						bitmap.UnlockBits(bitmapData);
				}
			}

			bitmap.Dispose();

			List<Bitmap> bitmapsToMergeReversed = new List<Bitmap>();

			for (int i = bitmapsToMerge.Count - 1; i >= 0; i--)
				bitmapsToMergeReversed.Add(bitmapsToMerge[i]);

			Bitmap mergedBitmap = ImageProcessing.Merging.MergeVertically(bitmapsToMergeReversed);

			foreach (Bitmap bitmapToMerge in bitmapsToMergeReversed)
				bitmapToMerge.Dispose();

			bitmap = mergedBitmap;
		}
		#endregion

		#region Rotate90FlipY()
		private void Rotate90FlipY(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			if ((itDecoder.PixelsFormat == PixelsFormat.FormatBlackWhite) && (imageFormat is ImageProcessing.FileFormat.Jpeg))
				throw new Exception(BIPStrings.CanTCreate1BitPixelJPEGFile_STR);

			ImageProcessing.BigImages.ItEncoder itEncoder = null;
			int width = itDecoder.Height;
			int height = itDecoder.Width;

			try
			{
				itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat, width, height, itDecoder.DpiX, itDecoder.DpiY);
				itEncoder.SetPalette(itDecoder);

				int sourceWidth = itDecoder.Width;
				int sourceHeight = itDecoder.Height;

				int maxWidth = (int)Math.Max(1, maxBufferSize / (itDecoder.Height * ImageProcessing.Misc.BytesPerPixel(itDecoder.PixelFormat)));
				int stopX = sourceWidth;

				while (stopX > 0)
				{
					int startX = Math.Max(0, stopX - maxWidth);
					int stripWidth = stopX - startX;
					Bitmap strip = null;

					try
					{
						strip = itDecoder.GetClip(new Rectangle(startX, 0, stripWidth, sourceHeight));

						Rotate90FlipY(ref strip);

						unsafe
						{
							BitmapData bitmapData = null;

							try
							{
								//int resizeHeight = (int)Math.Min(strip.Height, itEncoder.Height - topLine);
								bitmapData = strip.LockBits(new Rectangle(0, 0, strip.Width, strip.Height), ImageLockMode.ReadWrite, strip.PixelFormat);
								itEncoder.Write(strip.Height, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
								strip.UnlockBits(bitmapData);
								bitmapData = null;
								//topLine += resizeHeight;
							}
							finally
							{
								if (bitmapData != null)
									strip.UnlockBits(bitmapData);
							}
						}
					}
					finally
					{
						if (strip != null)
							strip.Dispose();

					}

					if (ProgressChanged != null)
						ProgressChanged(1.0F - (startX / (float)itDecoder.Width));

					stopX -= maxWidth;
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
	
		#region Rotate90FlipXY()
		private static void Rotate90FlipXY(ref Bitmap bitmap)
		{
			BitmapData bitmapData = null;
			List<Bitmap> bitmapsToMerge = new List<Bitmap>();
			int sourceWidth = bitmap.Width;
			int sourceHeight = bitmap.Height;

			int maxWidth = (int)Math.Max(1, maxBufferSize / (bitmap.Height * ImageProcessing.Misc.BytesPerPixel(bitmap.PixelFormat)));
			int startX = 0;

			while (startX < sourceWidth)
			{
				try
				{
					int stripWidth = Math.Min(maxWidth, sourceWidth - startX);
					bitmapData = bitmap.LockBits(new Rectangle(startX, 0, stripWidth, sourceHeight), ImageLockMode.ReadOnly, bitmap.PixelFormat);
					startX += stripWidth;

					int strideS = bitmapData.Stride;

					Bitmap strip = null;
					BitmapData stripData = null;

					try
					{
						strip = new Bitmap(bitmapData.Height, bitmapData.Width, bitmap.PixelFormat);
						stripData = strip.LockBits(new Rectangle(0, 0, strip.Width, strip.Height), ImageLockMode.WriteOnly, strip.PixelFormat);
						int strideStrip = stripData.Stride;

						unsafe
						{
							byte* pSourceS = (byte*)bitmapData.Scan0.ToPointer();
							byte* pSourceStrip = (byte*)stripData.Scan0.ToPointer();
							byte* pCurrentS;

							int x, y;

							if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
							{
								for (y = 0; y < sourceHeight; y++)
								{
									pCurrentS = pSourceS + (y * strideS);

									for (x = 0; x < stripWidth; x++)
									{
										pSourceStrip[(stripWidth - 1 - x) * strideStrip + y * 3] = pCurrentS[x * 3];
										pSourceStrip[(stripWidth - 1 - x) * strideStrip + y * 3 + 1] = pCurrentS[x * 3 + 1];
										pSourceStrip[(stripWidth - 1 - x) * strideStrip + y * 3 + 2] = pCurrentS[x * 3 + 2];
									}
								}
							}
							else if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
							{
								strip.Palette = bitmap.Palette;

								for (y = 0; y < sourceHeight; y++)
								{
									pCurrentS = pSourceS + (y * strideS);

									for (x = 0; x < stripWidth; x++)
									{
										pSourceStrip[(stripWidth - 1 - x) * strideStrip + y] = pCurrentS[x];
									}
								}
							}
							else if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
							{
								int bite;

								for (y = 0; y < sourceHeight; y++)
								{
									pCurrentS = pSourceS + (y * strideS);

									for (x = 0; x < stripWidth; x++)
									{
										bite = pCurrentS[x / 8] & (byte)(0x80 >> (x & 0x7));

										if (bite > 0)
											pSourceStrip[(stripWidth - 1 - x) * strideStrip + y / 8] |= (byte)(0x80 >> (y & 0x7));
									}
								}
							}
							else
								throw new Exception(BIPStrings.UnsupportedPixelFormat_STR);
						}

						bitmapsToMerge.Add(strip);
					}
					finally
					{
						if (stripData != null)
							strip.UnlockBits(stripData);
					}
				}
				finally
				{
					if (bitmapData != null)
						bitmap.UnlockBits(bitmapData);
				}
			}

			bitmap.Dispose();

			List<Bitmap> bitmapsToMergeReversed = new List<Bitmap>();

			for (int i = bitmapsToMerge.Count - 1; i >= 0; i--)
				bitmapsToMergeReversed.Add(bitmapsToMerge[i]);

			Bitmap mergedBitmap = ImageProcessing.Merging.MergeVertically(bitmapsToMergeReversed);

			foreach (Bitmap bitmapToMerge in bitmapsToMergeReversed)
				bitmapToMerge.Dispose();

			bitmap = mergedBitmap;
		}
		#endregion

		#region Rotate90FlipXY()
		private void Rotate90FlipXY(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			if ((itDecoder.PixelsFormat == PixelsFormat.FormatBlackWhite) && (imageFormat is ImageProcessing.FileFormat.Jpeg))
				throw new Exception(BIPStrings.CanTCreate1BitPixelJPEGFile_STR);

			ImageProcessing.BigImages.ItEncoder itEncoder = null;
			int width = itDecoder.Height;
			int height = itDecoder.Width;

			try
			{
				itEncoder = new ImageProcessing.BigImages.ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat, width, height, itDecoder.DpiX, itDecoder.DpiY);
				itEncoder.SetPalette(itDecoder);

				int sourceWidth = itDecoder.Width;
				int sourceHeight = itDecoder.Height;

				int maxWidth = (int)Math.Max(1, maxBufferSize / (itDecoder.Height * ImageProcessing.Misc.BytesPerPixel(itDecoder.PixelFormat)));
				int stopX = sourceWidth;

				while (stopX > 0)
				{
					int startX = Math.Max(0, stopX - maxWidth);
					int stripWidth = stopX - startX;
					Bitmap strip = null;

					try
					{
						strip = itDecoder.GetClip(new Rectangle(startX, 0, stripWidth, sourceHeight));

						Rotate90FlipXY(ref strip);

						unsafe
						{
							BitmapData bitmapData = null;

							try
							{
								//int resizeHeight = (int)Math.Min(strip.Height, itEncoder.Height - topLine);
								bitmapData = strip.LockBits(new Rectangle(0, 0, strip.Width, strip.Height), ImageLockMode.ReadWrite, strip.PixelFormat);
								itEncoder.Write(strip.Height, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
								strip.UnlockBits(bitmapData);
								bitmapData = null;
								//topLine += resizeHeight;
							}
							finally
							{
								if (bitmapData != null)
									strip.UnlockBits(bitmapData);
							}
						}
					}
					finally
					{
						if (strip != null)
							strip.Dispose();

					}

					if (ProgressChanged != null)
						ProgressChanged(1.0F - (startX / (float)itDecoder.Width));

					stopX -= maxWidth;
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
	}
}
