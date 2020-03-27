using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.BigImages
{
	public class ItDecoder : IDisposable
	{
		string						filePath;
		ImageFile.ImageInfo			imageInfo;
		Bitmap						bitmap = null;
		ImageComponent.ImageDecoder imageDecoder = null;
		object						bitmapLocker = new object();

		//events
		public ImageProcessing.ProgressHnd ProgressChanged;


		#region constructor
		public ItDecoder(string filePath)
		{
			this.filePath = filePath;

/*#if DEBUG
			DateTime start = DateTime.Now;
#endif*/

			this.imageInfo = new ImageProcessing.ImageFile.ImageInfo(filePath);

/*#if DEBUG
			Console.WriteLine("ImageInfo: " + DateTime.Now.Subtract(start).ToString() + Environment.NewLine +
				"Width = " + this.imageInfo.Width + Environment.NewLine +
				"Height = " + this.imageInfo.Height + Environment.NewLine +
				"DpiX = " + this.imageInfo.DpiH + Environment.NewLine +
				"DpiY = " + this.imageInfo.DpiV + Environment.NewLine +
				"PixelsFormat = " + this.imageInfo.PixelsFormat + Environment.NewLine +
				"PixelFormat = " + this.imageInfo.PixelFormat + Environment.NewLine
				); 
			
			Console.WriteLine("");
			start = DateTime.Now;

			this.imageDecoder = new ImageComponent.ImageDecoder(filePath);
			Console.WriteLine("ImageDecoder: " + DateTime.Now.Subtract(start).ToString() + Environment.NewLine +
				"Width = " + this.imageDecoder.Width + Environment.NewLine +
				"Height = " + this.imageDecoder.Height + Environment.NewLine +
				"DpiX = " + this.imageDecoder.DpiX + Environment.NewLine +
				"DpiY = " + this.imageDecoder.DpiY + Environment.NewLine +
				"PixelsFormat = " + this.imageDecoder.PixelsFormat + Environment.NewLine +
				"PixelFormat = " + this.imageDecoder.PixelFormat + Environment.NewLine +
				"Stride = " + this.imageDecoder.Stride
				);
#endif	*/
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		public string				FilePath	{ get { return filePath; } }
		public ImageFile.ImageInfo	ImageInfo	{ get { return imageInfo; } }

		public int			Width			{ get { return (int)this.ImageDecoder.Width; } }
		public int			Height			{ get { return (int)this.ImageDecoder.Height; } }
		public int			DpiX			{ get { return Convert.ToInt32(this.ImageDecoder.DpiX); } }
		public int			DpiY			{ get { return Convert.ToInt32(this.ImageDecoder.DpiY); } }
		public PixelFormat	PixelFormat		{ get { return Misc.GetPixelFormat(this.ImageDecoder.PixelFormat); } }
		public PixelsFormat PixelsFormat	{ get { return this.ImageDecoder.PixelsFormat; } }
		public Size			Size			{ get { return new System.Drawing.Size(this.Width, this.Height); } }
		public uint			FramesCount		{ get { return this.ImageDecoder.FramesCount; } }

		ImageComponent.ImageDecoder ImageDecoder
		{
			get
			{
				if (this.imageDecoder == null)
					this.imageDecoder = new ImageComponent.ImageDecoder(filePath);

				return this.imageDecoder;
			}
		}

		internal ImageComponent.PixelFormat ImageComponentPixelFormat 
		{ 
			get 
			{
				return this.ImageDecoder.PixelFormat; 
			} 
		}

		public bool IsGrayscale
		{
			get
			{
				if (this.PixelsFormat == PixelsFormat.Format8bppGray || this.PixelsFormat == PixelsFormat.Format4bppGray || this.PixelsFormat == PixelsFormat.FormatBlackWhite)
					return true;

				return false;
			}
		}

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Dispose()
		public void Dispose()
		{
			lock (bitmapLocker)
			{
				Deactivate();
			}
		}
		#endregion

		#region Activate()
		/// <summary>
		/// if bitmap width and height is smaller than 6000 pixels, it loads the bitmap into memory.
		/// </summary>
		public void Activate()
		{
			lock (bitmapLocker)
			{
				if (this.Width < 6000 && this.Height < 6000 && this.bitmap == null)
				{
					this.bitmap = ImageProcessing.ImageCopier.LoadFileIndependentImage(this.filePath);
				}
			}
		}
		#endregion

		#region Deactivate()
		/// <summary>
		/// releases bitmaps from memory
		/// </summary>
		public void Deactivate()
		{
			lock (bitmapLocker)
			{
				if (this.bitmap != null)
				{
					this.bitmap.Dispose();
					this.bitmap = null;
				}
				if (this.imageDecoder != null)
				{
					imageDecoder.Dispose();
					imageDecoder = null;
				}
			}
		}
		#endregion

		#region ReleaseAllocatedMemory()
		/// <summary>
		/// releases bitmaps from memory
		/// </summary>
		public void ReleaseAllocatedMemory(Bitmap bitmap)
		{
			lock (bitmapLocker)
			{
				if (this.bitmap != null && this.bitmap == bitmap)
				{
					this.bitmap.Dispose();
					this.bitmap = null;
				}
				else if(bitmap != null)
					bitmap.Dispose();

				if (this.imageDecoder != null)
				{
					imageDecoder.Dispose();
					imageDecoder = null;
				}
			}
		}
		#endregion

		#region GetClip()
		public Bitmap GetClip(Rectangle rect)
		{
			lock (bitmapLocker)
			{
				if (this.bitmap != null)
				{
					return ImageProcessing.ImageCopier.Copy(this.bitmap, rect);
				}
				else
				{
					int index = 0;
					
					try
					{						
						unsafe
						{
							uint stride;
							index = 100;

							byte[] scan0 = this.ImageDecoder.Read((uint)rect.X, (uint)rect.Y, (uint)rect.Width, (uint)rect.Height, out stride);
							
							index = 1;
							Bitmap b;

							fixed (byte* ptr = scan0)
							{
								b = new Bitmap(rect.Width, rect.Height, (int)stride, GetPixelFormat(this.ImageDecoder.PixelFormat), new IntPtr(ptr));
							}
							index = 2;

							ColorPalette palette = GetPalette();
							index = 3;

							if (palette != null && palette.Entries != null && palette.Entries.Length > 0)
								b.Palette = palette;

							index = 4;
							if (this.imageInfo.DpiH > 0 && this.imageInfo.DpiV > 0)
								b.SetResolution(this.imageInfo.DpiH, this.imageInfo.DpiV);

							index = 5;
							//b.Save(Debug.SaveToDir + "aa.png", ImageFormat.Png);
							return b;
						}
					}
					catch (Exception ex)
					{
						string message = "";
						
						if(this.imageDecoder == null)
							message = string.Format("ItDecoder, GetClip(): Can't strip big bitmap! {0} Index = {1}", ImageProcessing.Misc.GetErrorMessage(ex), index);
						else
							message = string.Format("ItDecoder, GetClip(): Can't strip big bitmap! {0} W: {1}, H: {2}, Rect:[{3}, {4}, {5}, {6}], Index = {7}",
								ImageProcessing.Misc.GetErrorMessage(ex), this.ImageDecoder.Width, this.ImageDecoder.Height, rect.X, rect.Y, rect.Width, rect.Height, index);
						throw new Exception(message, ex);
					}
				}
			}
		}
		#endregion

		#region GetImage()
		public Bitmap GetImage()
		{
			lock (bitmapLocker)
			{
				if (this.bitmap != null)
				{
					return ImageProcessing.ImageCopier.Copy(this.bitmap);
				}
				else
				{
					int index = 0;

					try
					{
						unsafe
						{
							uint stride;
							byte[] scan0 = this.ImageDecoder.Read(0, 0, (uint) this.Width, (uint) this.Height, out stride);

							index = 1;
							Bitmap b;

							fixed (byte* ptr = scan0)
							{
								b = new Bitmap(this.Width, this.Height, (int)stride, GetPixelFormat(this.ImageDecoder.PixelFormat), new IntPtr(ptr));
							}
							index = 2;

							ColorPalette palette = GetPalette();
							index = 3;

							if (palette != null && palette.Entries != null && palette.Entries.Length > 0)
								b.Palette = palette;

							index = 4;
							if (this.imageInfo.DpiH > 0 && this.imageInfo.DpiV > 0)
								b.SetResolution(this.imageInfo.DpiH, this.imageInfo.DpiV);

							index = 5;
							//b.Save(Debug.SaveToDir + "aa.png", ImageFormat.Png);
							return b;
						}
					}
					catch (Exception ex)
					{
						string message = string.Format("ItDecoder, GetImage(): {0} W: {1}, H: {2}, Rect:[{3}, {4}, {5}, {6}], Index = {7}",
							Misc.GetErrorMessage(ex), this.ImageDecoder.Width, this.ImageDecoder.Height, 0, 0, this.Width, this.Height, index);
						throw new Exception(message);
					}
				}
			}
		}
		#endregion

		#region GetImage()
		public Bitmap GetImage(int frameIndex)
		{
			lock (bitmapLocker)
			{
					int index = 0;

					try
					{
						this.ImageDecoder.SelectFrame((uint)frameIndex);
						
						unsafe
						{
							uint width = this.ImageDecoder.Width;
							uint height = this.ImageDecoder.Height;						
							uint stride;

							byte[] scan0 = this.ImageDecoder.Read(0, 0, width, height, out stride);

							index = 1;
							Bitmap b;

							fixed (byte* ptr = scan0)
							{
								b = new Bitmap((int)width, (int)height, (int)stride, GetPixelFormat(this.ImageDecoder.PixelFormat), new IntPtr(ptr));
							}
							index = 2;

							ColorPalette palette = GetPalette();
							index = 3;

							if (palette != null && palette.Entries != null && palette.Entries.Length > 0)
								b.Palette = palette;

							index = 4;
							if (this.imageInfo.DpiH > 0 && this.imageInfo.DpiV > 0)
								b.SetResolution(this.imageInfo.DpiH, this.imageInfo.DpiV);

							index = 5;
							//b.Save(Debug.SaveToDir + "aa.png", ImageFormat.Png);
							return b;
						}
					}
					catch (Exception ex)
					{
						string message = string.Format("ItDecoder, GetImage(frameIndex): {0} W: {1}, H: {2}, Rect:[{3}, {4}, {5}, {6}], Index = {7}",
							Misc.GetErrorMessage(ex), this.ImageDecoder.Width, this.ImageDecoder.Height, 0, 0, this.ImageDecoder.Width, this.ImageDecoder.Height, index);
						throw new Exception(message);
					}
				}
		}
		#endregion

		#region GetPalette()
		public ColorPalette GetPalette()
		{
			if (this.bitmap != null)
				return this.bitmap.Palette;
			else
			{
				bool releaseImageDecoder = (this.imageDecoder == null);
				
				ColorPalette palette = this.ImageDecoder.ColorPalette;

				/*List<uint> paletteColors = imageDecoder.PaletteColors;
				ColorPalette palette = null;

				if (paletteColors != null)
				{
					palette = ImageProcessing.Misc.GetEmptyPalette();

					for (int i = 0; i < paletteColors.Count && i < 256; i++)
						palette.Entries[i] = Color.FromArgb((byte)(paletteColors[i] >> 24), (byte)((paletteColors[i] >> 16) & 0xFF), (byte)((paletteColors[i] >> 8) & 0xFF), (byte)(paletteColors[i] & 0xFF));
				}*/

				if (releaseImageDecoder)
				{
					this.imageDecoder.Dispose();
					this.imageDecoder = null;
				}

				return palette;
			}
		}
		#endregion

		#region GetImageFormat()
		/*public static ImageProcessing.FileFormat.IImageFormat GetImageFormat(System.Drawing.Imaging.ImageFormat imageFormat, 
			ImageProcessing.IpSettings.ItImage.TiffCompression tiffCompression, byte jpegQuality)
		{
			ImageProcessing.FileFormat.IImageFormat iImageFormat = null;
			
			if (imageFormat == System.Drawing.Imaging.ImageFormat.Tiff)
				iImageFormat = new ImageProcessing.FileFormat.Tiff(tiffCompression);
			else if (imageFormat == System.Drawing.Imaging.ImageFormat.Png)
				iImageFormat = new ImageProcessing.FileFormat.Png();
			else if (imageFormat == System.Drawing.Imaging.ImageFormat.Gif)
				iImageFormat = new ImageProcessing.FileFormat.Gif();
			else if (imageFormat == System.Drawing.Imaging.ImageFormat.Bmp)
				iImageFormat = new ImageProcessing.FileFormat.Bmp();
			else
				iImageFormat = new ImageProcessing.FileFormat.Jpeg(jpegQuality);

			return iImageFormat;
		}*/
		#endregion

		#region SelectFrame()
		public void SelectFrame(uint frameIndex)
		{
			this.ImageDecoder.SelectFrame(frameIndex);
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region GetPixelFormat()
		System.Drawing.Imaging.PixelFormat GetPixelFormat(ImageComponent.PixelFormat wicPixelFormat)
		{
			switch (wicPixelFormat)
			{
				case ImageComponent.PixelFormat.FormatDontCare: return System.Drawing.Imaging.PixelFormat.DontCare;
				case ImageComponent.PixelFormat.Format1bppIndexed: return System.Drawing.Imaging.PixelFormat.Format1bppIndexed;
				case ImageComponent.PixelFormat.Format2bppIndexed: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format4bppIndexed: return System.Drawing.Imaging.PixelFormat.Format4bppIndexed;
				case ImageComponent.PixelFormat.Format8bppIndexed: return System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
				case ImageComponent.PixelFormat.FormatBlackWhite: return System.Drawing.Imaging.PixelFormat.Format1bppIndexed;
				case ImageComponent.PixelFormat.Format2bppGray: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format4bppGray: return System.Drawing.Imaging.PixelFormat.Format4bppIndexed;
				case ImageComponent.PixelFormat.Format8bppGray: return System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
				case ImageComponent.PixelFormat.Format16bppBGR555: return System.Drawing.Imaging.PixelFormat.Format16bppRgb555;
				case ImageComponent.PixelFormat.Format16bppBGR565: return System.Drawing.Imaging.PixelFormat.Format16bppRgb565;
				case ImageComponent.PixelFormat.Format16bppGray: return System.Drawing.Imaging.PixelFormat.Format16bppGrayScale;
				case ImageComponent.PixelFormat.Format24bppBGR: return System.Drawing.Imaging.PixelFormat.Format24bppRgb;
				case ImageComponent.PixelFormat.Format24bppRGB: return System.Drawing.Imaging.PixelFormat.Format24bppRgb;
				case ImageComponent.PixelFormat.Format32bppBGR: return System.Drawing.Imaging.PixelFormat.Format32bppRgb;
				case ImageComponent.PixelFormat.Format32bppBGRA: return System.Drawing.Imaging.PixelFormat.Format32bppArgb;
				case ImageComponent.PixelFormat.Format32bppPBGRA: return System.Drawing.Imaging.PixelFormat.Format32bppPArgb;
				case ImageComponent.PixelFormat.Format32bppGrayFloat: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format48bppRGBFixedPoint: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format16bppGrayFixedPoint: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format32bppBGR101010: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format48bppRGB: return System.Drawing.Imaging.PixelFormat.Format48bppRgb;
				case ImageComponent.PixelFormat.Format64bppRGBA: return System.Drawing.Imaging.PixelFormat.Format64bppArgb;
				case ImageComponent.PixelFormat.Format64bppPRGBA: return System.Drawing.Imaging.PixelFormat.Format64bppPArgb;
				case ImageComponent.PixelFormat.Format96bppRGBFixedPoint: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format128bppRGBAFloat: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format128bppPRGBAFloat: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format128bppRGBFloat: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format32bppCMYK: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format64bppRGBAFixedPoint: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format64bppRGBFixedPoint: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format128bppRGBAFixedPoint: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format128bppRGBFixedPoint: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format64bppRGBAHalf: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format64bppRGBHalf: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format48bppRGBHalf: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format32bppRGBE: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format16bppGrayHalf: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format32bppGrayFixedPoint: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format64bppCMYK: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format24bpp3Channels: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format32bpp4Channels: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format40bpp5Channels: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format48bpp6Channels: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format56bpp7Channels: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format64bpp8Channels: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format48bpp3Channels: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format64bpp4Channels: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format80bpp5Channels: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format96bpp6Channels: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format112bpp7Channels: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format128bpp8Channels: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format40bppCMYKAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format80bppCMYKAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format32bpp3ChannelsAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format40bpp4ChannelsAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format48bpp5ChannelsAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format56bpp6ChannelsAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format64bpp7ChannelsAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format72bpp8ChannelsAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format64bpp3ChannelsAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format80bpp4ChannelsAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format96bpp5ChannelsAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format112bpp6ChannelsAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format128bpp7ChannelsAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				case ImageComponent.PixelFormat.Format144bpp8ChannelsAlpha: return System.Drawing.Imaging.PixelFormat.Undefined;
				default: return System.Drawing.Imaging.PixelFormat.Undefined;
			}
		}
		#endregion

		#region MergeBitmapsVertically()
		Bitmap MergeBitmapsVertically(List<Bitmap> bitmaps)
		{
			Bitmap result = null;
			BitmapData resultData = null;

			try
			{
				int width = bitmaps[0].Width;
				int height = 0;
				int y = 0;

				foreach (Bitmap bitmap in bitmaps)
					height += bitmap.Height;

				result = new Bitmap(width, height, bitmaps[0].PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int resultStride = resultData.Stride;

				unsafe
				{
					byte* scanR = (byte*)resultData.Scan0.ToPointer();

					for (int i = 0; i < bitmaps.Count; i++)
					{
						Bitmap source = bitmaps[i];
						BitmapData sourceData = null;

						try
						{
							source = bitmaps[i];
							sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
							int sourceStride = sourceData.Stride;

							byte* scanS = (byte*)sourceData.Scan0.ToPointer();

							for (int yS = 0; yS < sourceData.Height; yS++)
							{
								byte* scanSTemp = scanS + yS * sourceStride;
								byte* scanRTemp = scanR + (y + yS) * resultStride;

								for (int xS = 0; xS < sourceStride; xS++)
								{
									*(scanRTemp++) = *(scanSTemp++);
								}
							}
						}
						finally
						{
							if (sourceData != null)
								source.UnlockBits(sourceData);
						}

						y += source.Height;

						if (ProgressChanged != null)
							ProgressChanged(((i + 1.0F) / bitmaps.Count) / 2.0F + 0.5F);
					}
				}

				result.SetResolution(bitmaps[0].HorizontalResolution, bitmaps[0].VerticalResolution);

				if (bitmaps[0].Palette != null && bitmaps[0].Palette.Entries.Length > 0)
					result.Palette = bitmaps[0].Palette;
			}
			finally
			{
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region MergeBitmapsVertically()
		Bitmap MergeBitmapsVertically(Bitmap source, Bitmap result, int topLine)
		{
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				int width = Math.Min(source.Width, result.Width);
				int height = Math.Min(source.Height, result.Height - topLine);

				sourceData = source.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, topLine, width, height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sourceStride = sourceData.Stride;
				int resultStride = resultData.Stride;

				unsafe
				{
					byte* scanR = (byte*)resultData.Scan0.ToPointer();
					byte* scanS = (byte*)sourceData.Scan0.ToPointer();

					for (int y = 0; y < height; y++)
					{
						byte* scanSTemp = scanS + y * sourceStride;
						byte* scanRTemp = scanR + y * resultStride;

						for (int x = 0; x < sourceStride; x++)
						{
							*(scanRTemp++) = *(scanSTemp++);
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#endregion
	}
}
