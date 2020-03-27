using ImageProcessing.Languages;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Histogram.
	/// </summary>
	public class ImageCopier
	{

		//	PUBLIC METHODS
		#region public methods

		#region Copy()
		public static Bitmap Copy(Bitmap source)
		{
			Bitmap copy = null;

			BitmapData origData = null;
			BitmapData copyData = null;

			try
			{
				copy = new Bitmap(source.Width, source.Height, source.PixelFormat);

				origData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
				copyData = copy.LockBits(new Rectangle(Point.Empty, copy.Size), ImageLockMode.WriteOnly, copy.PixelFormat);

				unsafe
				{
					byte* pOrig = (byte*)origData.Scan0.ToPointer();
					byte* pCopy = (byte*)copyData.Scan0.ToPointer();

					long totalBytes = origData.Stride * source.Height;

					for (int y = 0; y < totalBytes; y++)
						*(pCopy++) = *(pOrig++);
				}
			}
			finally
			{
				if (origData != null)
					source.UnlockBits(origData);
				if (copy != null && copyData != null)
					copy.UnlockBits(copyData);
			}

			if (source.HorizontalResolution > 0 && source.VerticalResolution > 0)
				Misc.SetBitmapResolution(copy, source.HorizontalResolution, source.VerticalResolution);

			if (source.PixelFormat == PixelFormat.Format8bppIndexed || source.PixelFormat == PixelFormat.Format4bppIndexed || source.PixelFormat == PixelFormat.Format1bppIndexed)
				copy.Palette = source.Palette;

			return copy;
		}

		public static Bitmap Copy(Bitmap source, Rectangle clip)
		{
			if (clip.IsEmpty)
				return Copy(source);
			
			clip = Rectangle.Intersect(clip, new Rectangle(0, 0, source.Width, source.Height));
			Bitmap copy = null;
			BitmapData origData = null;
			BitmapData copyData = null;

			try
			{
				origData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);

				copy = new Bitmap(clip.Width, clip.Height, origData.PixelFormat);
				copyData = copy.LockBits(new Rectangle(Point.Empty, clip.Size), ImageLockMode.WriteOnly, copy.PixelFormat);

				int oStride = origData.Stride;
				int cStride = copyData.Stride;

				int clipWidth = clip.Width;
				int clipHeight = clip.Height;
				int x, y;

				unsafe
				{
					byte* pOrig = (byte*)origData.Scan0.ToPointer();
					byte* pCopy = (byte*)copyData.Scan0.ToPointer();
					byte* pOrigCurrent;
					byte* pCopyCurrent;

					if (copy.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						for (y = 0; y < clipHeight; y++)
						{
							pOrigCurrent = pOrig + y * oStride;
							pCopyCurrent = pCopy + y * cStride;

							for (x = 0; x < clipWidth; x = x + 8)
								*(pCopyCurrent++) = *(pOrigCurrent++);
						}
					}
					else if (copy.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for (y = 0; y < clipHeight; y++)
						{
							pOrigCurrent = pOrig + y * oStride;
							pCopyCurrent = pCopy + y * cStride;

							for (x = 0; x < clipWidth; x++)
								*(pCopyCurrent++) = *(pOrigCurrent++);
						}
					}
					else if (copy.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for (y = 0; y < clipHeight; y++)
						{
							pOrigCurrent = pOrig + y * oStride;
							pCopyCurrent = pCopy + y * cStride;

							for (x = 0; x < clipWidth; x++)
							{
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
							}
						}
					}
					else if (copy.PixelFormat == PixelFormat.Format32bppArgb || copy.PixelFormat == PixelFormat.Format32bppRgb)
					{
						for (y = 0; y < clipHeight; y++)
						{
							pOrigCurrent = pOrig + y * oStride;
							pCopyCurrent = pCopy + y * cStride;

							for (x = 0; x < clipWidth; x++)
							{
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
							}
						}
					}
				}

				if (source.HorizontalResolution > 0 && source.VerticalResolution > 0)
					Misc.SetBitmapResolution(copy, source.HorizontalResolution, source.VerticalResolution);

				if (source.PixelFormat == PixelFormat.Format8bppIndexed || source.PixelFormat == PixelFormat.Format4bppIndexed || source.PixelFormat == PixelFormat.Format1bppIndexed)
					copy.Palette = source.Palette;

				return copy;
			}
			finally
			{
				if (origData != null)
					source.UnlockBits(origData);
				if (copyData != null)
					copy.UnlockBits(copyData);
			}
		}
		#endregion

		#region CopyAndDisposeOriginal()
		/// <summary>
		/// it creates copy and disposes 'source';
		/// </summary>
		/// <param name="source"></param>
		/// <param name="clip"></param>
		/// <returns></returns>
		public static Bitmap CopyAndDisposeOriginal(Bitmap source, Rectangle clip)
		{
			clip = Rectangle.Intersect(clip, new Rectangle(0, 0, source.Width, source.Height));

			string tempPath = Path.GetTempPath() + DateTime.Now.ToString("HH-mm-ss--ff") + ".png";

			ImageProcessing.BigImages.ItEncoder.SaveToDisk(source, tempPath, new ImageProcessing.FileFormat.Png(), clip);

			source.Dispose();

			Bitmap crop = ImageProcessing.ImageCopier.LoadFileIndependentImage(tempPath);

			try { File.Delete(tempPath); }
			catch { }

			return crop;
		}
		#endregion

		#region LoadFileIndependentImage()
		public static Bitmap LoadFileIndependentImage(string filePath)
		{
			MemoryFailPoint memoryFailPoint = null;

			GC.Collect();
			//GC.WaitForPendingFinalizers();
			//GC.GetTotalMemory(true);

			FileInfo file = new FileInfo(filePath);
			long fileLength = file.Length;

			ImageFile.ImageInfo imageInfo = new ImageFile.ImageInfo(filePath);

			try
			{
				memoryFailPoint = new MemoryFailPoint((int)Math.Ceiling(fileLength / (double)1048576));
				memoryFailPoint.Dispose();
			}
			catch (OutOfMemoryException ex)
			{
#if DEBUG
				Console.WriteLine(ex);
#endif
				throw new Exception(BIPStrings.ThereIsNotSufficientMemoryToOpenFile_STR + " " + filePath + " " + BIPStrings.FromDisk_STR + "!\n" + ex.Message);
			}

			using (FileStream reader = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				int stride = Misc.GetStride(imageInfo.Width, imageInfo.PixelFormat);
				long memoryNeededToBeAllocated = 4096 + stride * imageInfo.Height + 1024;

				try
				{
					memoryFailPoint = new MemoryFailPoint((int)Math.Ceiling(memoryNeededToBeAllocated / (double)1048576));
				}
				catch (OutOfMemoryException ex)
				{
#if DEBUG
					Console.WriteLine(ex);
#endif
					throw new Exception(BIPStrings.ThereIsNotEnoughMemoryToOpenBitmapFromFile_STR + " " + filePath + "!\n" + ex.Message);
				}

				return new Bitmap(reader);
			}
		}
		#endregion

		#region SaveFileIndependentImage()
		public static void SaveFileIndependentImage(Bitmap bitmap, ImageCodecInfo codecInfo, EncoderParameters encodeParameters, string filePath)
		{
			FileStream writer = null;

			try
			{
				GC.Collect();
				//GC.WaitForPendingFinalizers();
				//GC.GetTotalMemory(true);

				Bitmap clone = Clone(bitmap);
				writer = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
				clone.Save(writer, codecInfo, encodeParameters);
			}
			finally
			{
				if (writer != null)
					writer.Close();
			}
		}
		#endregion

		#region Get24Bpp()
		public static Bitmap Get24Bpp(Bitmap source)
		{
			switch (source.PixelFormat)
			{
				case PixelFormat.Format24bppRgb: return Copy(source);
				case PixelFormat.Format8bppIndexed: return Get24BppFrom8Bpp(source);
				case PixelFormat.Format1bppIndexed: return Get24BppFrom1Bpp(source);
				default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region Clone()
		private static Bitmap Clone(Bitmap source)
		{
			BitmapData data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
			Bitmap copy = new Bitmap(data.Width, data.Height, data.Stride, data.PixelFormat, data.Scan0);

			source.UnlockBits(data);

			Misc.SetBitmapResolution(copy, source.HorizontalResolution, source.VerticalResolution);
			if (source.PixelFormat == PixelFormat.Format1bppIndexed || source.PixelFormat == PixelFormat.Format8bppIndexed || source.PixelFormat == PixelFormat.Format1bppIndexed)
				copy.Palette = source.Palette;

			return copy;
		}
		#endregion

		#region Get24BppFrom8Bpp()
		private static Bitmap Get24BppFrom8Bpp(Bitmap source)
		{
			Bitmap copy = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);

			BitmapData origData = null;
			BitmapData copyData = null;
			byte[] palette = new byte[256];
			int x, y;
			int width = source.Width;
			int height = source.Height;

			for (int i = 0; i < 256; i++)
				palette[i] = source.Palette.Entries[i].R;

			try
			{
				origData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
				copyData = copy.LockBits(new Rectangle(Point.Empty, copy.Size), ImageLockMode.WriteOnly, copy.PixelFormat);

				unsafe
				{
					byte* pOrig = (byte*)origData.Scan0.ToPointer();
					byte* pCopy = (byte*)copyData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentR;

					for (y = 0; y < height; y++)
					{
						pCurrentS = pOrig + y * origData.Stride;
						pCurrentR = pCopy + y * copyData.Stride;

						for (x = 0; x < width; x++)
						{
							pCurrentR[0] = palette[*(pCurrentS++)];
							pCurrentR[1] = pCurrentR[0];
							pCurrentR[2] = pCurrentR[0];

							pCurrentR += 3;
						}
					}
				}
			}
			finally
			{
				if (origData != null)
					source.UnlockBits(origData);
				if (copyData != null)
					copy.UnlockBits(copyData);
			}

			if (source.HorizontalResolution > 0 && source.VerticalResolution > 0)
				Misc.SetBitmapResolution(copy, source.HorizontalResolution, source.VerticalResolution);

			return copy;
		}
		#endregion

		#region Get24BppFrom1Bpp()
		private static Bitmap Get24BppFrom1Bpp(Bitmap source)
		{
			Bitmap copy = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);

			BitmapData origData = null;
			BitmapData copyData = null;

			try
			{
				origData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
				copyData = copy.LockBits(new Rectangle(Point.Empty, copy.Size), ImageLockMode.WriteOnly, copy.PixelFormat);
				int strideS = origData.Stride;
				int strideR = copyData.Stride;
				int x, y;
				int width = source.Width;
				int height = source.Height;

				unsafe
				{
					byte* pOrig = (byte*)origData.Scan0.ToPointer();
					byte* pCopy = (byte*)copyData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentR;

					for (y = 0; y < height; y++)
					{
						pCurrentS = pOrig + y * strideS;
						pCurrentR = pCopy + y * strideR;

						for (x = 0; x < width; x = x + 8)
						{
							if ((*pCurrentS & 0x80) > 0)
							{
								pCurrentR[0] = 255;
								pCurrentR[1] = 255;
								pCurrentR[2] = 255;
							}

							if ((*pCurrentS & 0x40) > 0)
							{
								pCurrentR[3] = 255;
								pCurrentR[4] = 255;
								pCurrentR[5] = 255;
							}

							if ((*pCurrentS & 0x20) > 0)
							{
								pCurrentR[6] = 255;
								pCurrentR[7] = 255;
								pCurrentR[8] = 255;
							}

							if ((*pCurrentS & 0x10) > 0)
							{
								pCurrentR[9] = 255;
								pCurrentR[10] = 255;
								pCurrentR[11] = 255;
							}

							if ((*pCurrentS & 0x08) > 0)
							{
								pCurrentR[12] = 255;
								pCurrentR[13] = 255;
								pCurrentR[14] = 255;
							}

							if ((*pCurrentS & 0x04) > 0)
							{
								pCurrentR[15] = 255;
								pCurrentR[16] = 255;
								pCurrentR[17] = 255;
							}

							if ((*pCurrentS & 0x02) > 0)
							{
								pCurrentR[18] = 255;
								pCurrentR[19] = 255;
								pCurrentR[20] = 255;
							}

							if ((*pCurrentS & 0x01) > 0)
							{
								pCurrentR[21] = 255;
								pCurrentR[22] = 255;
								pCurrentR[23] = 255;
							}
							pCurrentR += 24;

							pCurrentS++;
						}
					}
				}
			}
			finally
			{
				if (origData != null)
					source.UnlockBits(origData);
				if (copyData != null)
					copy.UnlockBits(copyData);
			}

			if (source.HorizontalResolution > 0 && source.VerticalResolution > 0)
				Misc.SetBitmapResolution(copy, source.HorizontalResolution, source.VerticalResolution);

			return copy;
		}
		#endregion

		#endregion


	}
}