using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;
using ImageProcessing.Languages;
using System.Drawing;

namespace ImageProcessing.BigImages
{
	public class ItEncoder : IDisposable
	{
		ImageComponent.ImageEncoder encoder;
		

		#region constructor
		/*public ItEncoder(string file, System.Drawing.Imaging.ImageFormat imageFormat, System.Drawing.Imaging.PixelFormat pixelFormat, int width, int height,
			ImageProcessing.IpSettings.ItImage.TiffCompression tiffCompression, byte quality, double dpiX, double dpiY)
		{
			ImageComponent.PixelFormat icPixelFormat = GetPixelFormat(pixelFormat);

			encoder = new ImageComponent.ImageEncoder();

			if (imageFormat == System.Drawing.Imaging.ImageFormat.Tiff)
			{
				ImageComponent.TiffCompression icTiffCompression = GetTiffCompression(tiffCompression);
				encoder.OpenTiff(file, width, height, icPixelFormat, dpiX, dpiY, icTiffCompression);
			}
			else if (imageFormat == System.Drawing.Imaging.ImageFormat.Png)
			{
				encoder.OpenPng(file, width, height, icPixelFormat, dpiX, dpiY);
			}
			else if (imageFormat == System.Drawing.Imaging.ImageFormat.Gif)
			{
				encoder.OpenGif(file, width, height, icPixelFormat, dpiX, dpiY);
			}
			else
			{
				encoder.OpenJpeg(file, width, height, icPixelFormat, dpiX, dpiY, quality);
			}
		}*/

		public ItEncoder(string file, ImageProcessing.FileFormat.IImageFormat imageFormat, PixelsFormat pixelsFormat, int width, int height,
			double dpiX, double dpiY)
		{
			// photoshop doesn't display TIFF 1bit indexed properly (when PhotometricInterpretation == 3 instead of 1)
			/*if (icPixelFormat == ImageComponent.PixelFormat.Format1bppIndexed)
				icPixelFormat = ImageComponent.PixelFormat.FormatBlackWhite;*/

			//ImageComponent.PixelFormat icPixelFormat = Transactions.GetImageComponentPixelFormat(pixelsFormat);
			
			encoder = new ImageComponent.ImageEncoder();

			if (imageFormat is ImageProcessing.FileFormat.Tiff)
			{
				encoder.OpenTiff(file, (uint)width, (uint)height, pixelsFormat, dpiX, dpiY, ((ImageProcessing.FileFormat.Tiff)imageFormat).Compression);
			}
			else if (imageFormat is ImageProcessing.FileFormat.Png)
			{
				encoder.OpenPng(file, (uint)width, (uint)height, pixelsFormat, dpiX, dpiY);
			}
			else if (imageFormat is ImageProcessing.FileFormat.Gif)
			{
				encoder.OpenGif(file, (uint)width, (uint)height, pixelsFormat, dpiX, dpiY);
			}
			else if (imageFormat is ImageProcessing.FileFormat.Jpeg)
			{
				encoder.OpenJpeg(file, (uint)width, (uint)height, pixelsFormat, dpiX, dpiY, ((ImageProcessing.FileFormat.Jpeg)imageFormat).Quality);
			}
			else 
			{
				throw new Exception(BIPStrings.UnsupportedFileFormat_STR);
			}
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		public int					Stride { get { return (int) encoder.Stride; } }
		public int					Width { get { return (int)encoder.Width; } }
		public int					Height { get { return (int)encoder.Height; } }
		public System.Drawing.Size	Size { get { return new System.Drawing.Size((int)encoder.Width, (int)encoder.Height); } }
		public PixelsFormat			PixelsFormat { get { return encoder.PixelsFormat; } }

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Write()
		/*public unsafe void Write(int height, int stride, byte[] buffer)
		{
			encoder.Write((uint)height, (uint)stride, buffer);
		}*/

		public unsafe void Write(int height, int stride, byte* buffer)
		{
			encoder.Write((uint)height, (uint)stride, buffer);
		}
		#endregion

		#region SaveToDisk()
		/// <summary>
		/// Saves 'bitmap' to the disk, resolution is set based on the bitmap.HorizontalResolution and bitmap.VerticalResolution;
		/// </summary>
		/// <param name="bitmap">source bitmap</param>
		/// <param name="destFile"></param>
		/// <param name="imageFormat"></param>
		/// <param name="pixelsFormat"></param>
		public static unsafe void SaveToDisk(Bitmap bitmap, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			ImageProcessing.BigImages.ItEncoder itEncoder = null;
			int									width = bitmap.Width;
			int									height = bitmap.Height;
			double								dpiX = bitmap.HorizontalResolution;
			double								dpiY = bitmap.VerticalResolution;
			ImageProcessing.PixelsFormat		pixelsFormat = ImageProcessing.Transactions.GetPixelsFormat(bitmap.PixelFormat);

			try
			{
				itEncoder = new ImageProcessing.BigImages.ItEncoder(destFile, imageFormat, pixelsFormat, width, height, dpiX, dpiY);

				unsafe
				{
					BitmapData bitmapData = null;

					try
					{
						bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
						/*int stride = bitmapData.Stride;

						byte* scan0 = (byte*)bitmapData.Scan0.ToPointer();

						for (int y = 0; y < height; y = y + 64)
						{
							int localHeight = Math.Min(y + 64, height) - y;

							itEncoder.Write(localHeight, stride, scan0 + y * stride);
						}*/
							
						itEncoder.Write(height, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
					}
					finally
					{
						if (bitmapData != null)
							bitmap.UnlockBits(bitmapData);
					}
				}
			}
			catch (Exception ex)
			{
				try { if (itEncoder != null) itEncoder.Dispose(); }
				catch { }
				finally { itEncoder = null; }

				try { if (System.IO.File.Exists(destFile)) System.IO.File.Delete(destFile); }
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

		#region SaveToDisk()
		/// <summary>
		/// Saves 'bitmap' to the disk, resolution is set based on the bitmap.HorizontalResolution and bitmap.VerticalResolution;
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="destFile"></param>
		/// <param name="imageFormat"></param>
		/// <param name="clip">Bitmap clip to save</param>
		public static unsafe void SaveToDisk(Bitmap bitmap, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat, Rectangle clip)
		{
			clip = Rectangle.Intersect(clip, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

			ImageProcessing.BigImages.ItEncoder itEncoder = null;
			int									width = clip.Width;
			int									height = clip.Height;
			double								dpiX = bitmap.HorizontalResolution;
			double								dpiY = bitmap.VerticalResolution;
			ImageProcessing.PixelsFormat		pixelsFormat = ImageProcessing.Transactions.GetPixelsFormat(bitmap.PixelFormat);

			try
			{
				itEncoder = new ImageProcessing.BigImages.ItEncoder(destFile, imageFormat, pixelsFormat, width, height, dpiX, dpiY);

				unsafe
				{
					BitmapData bitmapData = null;

					try
					{
						bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadOnly, bitmap.PixelFormat);
						int		stride = bitmapData.Stride;
						byte*	scan0 = (byte*)bitmapData.Scan0.ToPointer();
						int		encoderStride = itEncoder.Stride;

						for (int y = 0; y < height; y++)
						{
							itEncoder.Write(1, encoderStride, scan0 + y * stride);
						}
					}
					finally
					{
						if (bitmapData != null)
							bitmap.UnlockBits(bitmapData);
					}
				}
			}
			catch (Exception ex)
			{
				try { if (itEncoder != null) itEncoder.Dispose(); }
				catch { }
				finally { itEncoder = null; }

				try { if (System.IO.File.Exists(destFile)) System.IO.File.Delete(destFile); }
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

		#region SetPalette()
		public void SetPalette(System.Drawing.Imaging.PixelFormat pixelFormat, System.Drawing.Color[] palette)
		{
			List<uint> entries = new List<uint>();

			for (int i = 0; i < palette.Length; i++)
				entries.Add((uint)((palette[i].R << 16) + (palette[i].G << 8) + (palette[i].B)));

			encoder.SetPalette(GetPixelFormat(pixelFormat), entries);
		}

		public void SetPalette(ImageProcessing.BigImages.ItDecoder itDecoder)
		{
			ColorPalette palette = itDecoder.GetPalette();

			if (palette != null && palette.Entries.Length > 0)
			{
				List<uint> entries = new List<uint>();

				for (int i = 0; i < palette.Entries.Length; i++)
					entries.Add((uint)((palette.Entries[i].R << 16) + (palette.Entries[i].G << 8) + (palette.Entries[i].B)));

				encoder.SetPalette(encoder.PixelFormat, entries);
			}
		}
		#endregion

		#region Dispose()
		public void Dispose()
		{
			encoder.Close();
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region GetTiffCompression()
		ImageComponent.TiffCompression GetTiffCompression(ImageProcessing.IpSettings.ItImage.TiffCompression tiffCompression)
		{
			if (tiffCompression == ImageProcessing.IpSettings.ItImage.TiffCompression.G4)
				return ImageComponent.TiffCompression.WICTiffCompressionCCITT4;
			else if (tiffCompression == ImageProcessing.IpSettings.ItImage.TiffCompression.LZW)
				return ImageComponent.TiffCompression.WICTiffCompressionLZW;
			else
				return ImageComponent.TiffCompression.WICTiffCompressionNone;
		}
		#endregion

		#region GetPixelFormat()
		ImageComponent.PixelFormat GetPixelFormat(System.Drawing.Imaging.PixelFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				case System.Drawing.Imaging.PixelFormat.Undefined: return ImageComponent.PixelFormat.FormatDontCare;
				case System.Drawing.Imaging.PixelFormat.Format1bppIndexed: return ImageComponent.PixelFormat.FormatBlackWhite;
				case System.Drawing.Imaging.PixelFormat.Format4bppIndexed: return ImageComponent.PixelFormat.Format4bppIndexed;
				case System.Drawing.Imaging.PixelFormat.Format8bppIndexed: return ImageComponent.PixelFormat.Format8bppIndexed;
				case System.Drawing.Imaging.PixelFormat.Format16bppRgb555: return ImageComponent.PixelFormat.Format16bppBGR555;
				case System.Drawing.Imaging.PixelFormat.Format16bppRgb565: return ImageComponent.PixelFormat.Format16bppBGR565;
				case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale: return ImageComponent.PixelFormat.Format16bppGray;
				case System.Drawing.Imaging.PixelFormat.Format24bppRgb: return ImageComponent.PixelFormat.Format24bppBGR;
				case System.Drawing.Imaging.PixelFormat.Format32bppRgb: return ImageComponent.PixelFormat.Format32bppBGR;
				case System.Drawing.Imaging.PixelFormat.Format32bppArgb: return ImageComponent.PixelFormat.Format32bppBGRA;
				case System.Drawing.Imaging.PixelFormat.Format32bppPArgb: return ImageComponent.PixelFormat.Format32bppPBGRA;
				case System.Drawing.Imaging.PixelFormat.Format48bppRgb: return ImageComponent.PixelFormat.Format48bppRGB;
				case System.Drawing.Imaging.PixelFormat.Format64bppArgb: return ImageComponent.PixelFormat.Format64bppRGBA;
				case System.Drawing.Imaging.PixelFormat.Format64bppPArgb: return ImageComponent.PixelFormat.Format64bppPRGBA;

				default: return ImageComponent.PixelFormat.FormatDontCare;
			}
		}
		#endregion

		#region GetPixelsFormat()
		PixelsFormat GetPixelsFormat(ImageComponent.PixelFormat pixelFormat)
		{
			switch (pixelFormat)
			{
				case ImageComponent.PixelFormat.Format1bppIndexed: return PixelsFormat.FormatBlackWhite;
				case ImageComponent.PixelFormat.FormatBlackWhite: return PixelsFormat.FormatBlackWhite;
				case ImageComponent.PixelFormat.Format4bppIndexed: return PixelsFormat.Format4bppGray;
				case ImageComponent.PixelFormat.Format4bppGray: return PixelsFormat.Format4bppGray;
				case ImageComponent.PixelFormat.Format8bppGray: return PixelsFormat.Format8bppGray;
				case ImageComponent.PixelFormat.Format8bppIndexed: return PixelsFormat.Format8bppIndexed;
				case ImageComponent.PixelFormat.Format24bppBGR: return PixelsFormat.Format24bppRgb;
				case ImageComponent.PixelFormat.Format32bppBGR:
				case ImageComponent.PixelFormat.Format32bppBGRA:
				case ImageComponent.PixelFormat.Format32bppPBGRA:
					return PixelsFormat.Format32bppRgb;

				default: throw new Exception("ItEncoder, GetPixelsFormat(): Unsupported image format");
			}
		}
		#endregion

		#endregion


	}
}
