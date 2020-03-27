using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Diagnostics;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class BinorizationThreshold
	{
		public BinorizationThreshold()
		{
		}

		//	PUBLIC METHODS
		
		#region Binorize()	
		public static Bitmap Binorize(Bitmap source)
		{
			return Binorize(source, 0);
		}
		
		public static Bitmap Binorize(Bitmap source, int thresholdDelta)
		{
			switch(source.PixelFormat)
			{
				case PixelFormat.Format4bppIndexed:
					{
						return Binorize(source, (byte)(128 + thresholdDelta), (byte)(128 + thresholdDelta), (byte)(128 + thresholdDelta));
					}
				case PixelFormat.Format8bppIndexed:	
				{
					Histogram	histogram = new Histogram(source);
					byte		thres = (byte) Math.Max(1, Math.Min(254, histogram.Threshold.R + thresholdDelta));

					return Binorize(source, thres, thres, thres);
				}

				case PixelFormat.Format24bppRgb :
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppRgb:
					{
					Histogram	histogram = new Histogram(source);
					byte			r,g,b;

					r = (byte) ((histogram.ThresholdR + thresholdDelta > 1) ? ((histogram.ThresholdR + thresholdDelta < 254) ? histogram.ThresholdR + thresholdDelta : 254) : 1);
					g = (byte) ((histogram.ThresholdG + thresholdDelta > 1) ? ((histogram.ThresholdG + thresholdDelta < 254) ? histogram.ThresholdG + thresholdDelta : 254) : 1);
					b = (byte) ((histogram.ThresholdB + thresholdDelta > 1) ? ((histogram.ThresholdB + thresholdDelta < 254) ? histogram.ThresholdB + thresholdDelta : 254) : 1);

					return Binorize(source, r, g, b) ;
				}
				default :
					throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
			}
		}

		public static Bitmap Binorize(Bitmap source, byte thresR, byte thresG, byte thresB)
		{
			return Binorize(source, Rectangle.Empty, thresR, thresG, thresB) ;
		}
		
		public static Bitmap Binorize(Bitmap source, Rectangle clip, byte thresR, byte thresG, byte thresB)
		{
			if(source == null)
				return null ;

			if(clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, source.Width, source.Height);
			
			try
			{
				switch(source.PixelFormat)
				{
					case PixelFormat.Format4bppIndexed:
							return Binorize4bpp(source, clip, thresR);
					case PixelFormat.Format8bppIndexed:
						if (Misc.IsGrayscale(source))
							return Binorize8bppGrayscale(source, clip, thresR);
						else
							return Binorize8bpp(source, clip, thresR, thresG, thresB);
					case PixelFormat.Format24bppRgb :				
						return Binorize24bpp(source, clip, thresR, thresG, thresB) ;
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
						return Binorize32bpp(source, clip, thresR, thresG, thresB);
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}
			}
			catch(Exception ex)
			{
				throw new Exception(string.Format("BinorizationThreshold, Binorize(). Width={0}, Height={1}, Clip={2}, r={3}, g={4}, b={5}, pix={6}: {7}",
					source.Width, source.Height, clip, thresR, thresG, thresB, source.PixelFormat, ex.Message));
			}
		}
		#endregion


		//PRIVATE METHODS
		#region private methods

		#region Binorize32bpp()
		private static Bitmap Binorize32bpp(Bitmap source, Rectangle clip, byte thresR, byte thresG, byte thresB)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sourceStride = sourceData.Stride;
				int resultStride = resultData.Stride;
				int threshold = thresR + thresG + thresB;

				int width = sourceData.Width;
				int height = sourceData.Height;
				int x, y;

				unsafe
				{
					byte* pOrig = (byte*)sourceData.Scan0.ToPointer();
					byte* pCopy = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrent;

					for (y = 0; y < height; y++)
					{
						pCurrent = pOrig + (y * sourceStride);

						for (x = 0; x < width; x++)
						{
							//gray = 0.299Red + 0.587Gray + 0.114Blue
							//pixels are stored in order: blue, green, red
							//gray = (int) ((*(pCurrent++)) * 0.114F + (*(pCurrent++)) * 0.587F + (*(pCurrent++)) * 0.299F) ;
							//if((pCurrent[2] > thresR) && (pCurrent[1] > thresG) && (*pCurrent > thresB)) 

							if ((*(pCurrent++) + *(pCurrent++) + *(pCurrent++)) > threshold)
								pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));

							pCurrent++;
						}
					}
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);

				if (source != null && result != null)
					Misc.SetBitmapResolution(result, source.HorizontalResolution, source.VerticalResolution);
			}

			return result;
		}
		#endregion

		#region Binorize24bpp()
		private static Bitmap Binorize24bpp(Bitmap source, Rectangle clip, byte thresR, byte thresG, byte thresB)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sourceStride = sourceData.Stride;
				int resultStride = resultData.Stride;
				int threshold = thresR + thresG + thresB;

				int width = sourceData.Width;
				int height = sourceData.Height;
				int x, y;

				unsafe
				{
					byte* pOrig = (byte*)sourceData.Scan0.ToPointer();
					byte* pCopy = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrent;


					for (y = 0; y < height; y++)
					{
						pCurrent = pOrig + (y * sourceStride);

						for (x = 0; x < width; x++)
						{
							//gray = 0.299Red + 0.587Gray + 0.114Blue
							//pixels are stored in order: blue, green, red
							//gray = (int) ((*(pCurrent++)) * 0.114F + (*(pCurrent++)) * 0.587F + (*(pCurrent++)) * 0.299F) ;
							//if((pCurrent[2] > thresR) && (pCurrent[1] > thresG) && (*pCurrent > thresB)) 

							if ((*(pCurrent++) + *(pCurrent++) + *(pCurrent++)) > threshold)
								pCopy[y * resultStride + (x >> 3)] |= (byte)(0x80 >> (x & 0x7));
						}
					}
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);

				if (source != null && result != null)
					Misc.SetBitmapResolution(result, source.HorizontalResolution, source.VerticalResolution);
			}

			return result;
		}
		#endregion

		#region Binorize8bpp()
		private static Bitmap Binorize8bpp(Bitmap source, Rectangle clip, byte thresR, byte thresG, byte thresB)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sourceStride = sourceData.Stride;
				int resultStride = resultData.Stride;

				int		width = sourceData.Width;
				int		height = sourceData.Height;
				int		x, y;
				Color[] palette = source.Palette.Entries;
				int		threshold = thresR + thresG + thresB;

				unsafe
				{
					byte* pOrig = (byte*)sourceData.Scan0.ToPointer();
					byte* pCopy = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrent;

					for (y = 0; y < height; y++)
					{
						pCurrent = pOrig + y * sourceStride;

						for (x = 0; x < width; x++)
						{
							if ((palette[*pCurrent].R + palette[*pCurrent].G + palette[*pCurrent].B) > threshold)
								pCopy[y * resultData.Stride + (x >> 3)] |= (byte)(0x80 >> (x & 0x07));
							
							pCurrent++;
						}
					}
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);

				if (source != null && result != null)
					Misc.SetBitmapResolution(result, source.HorizontalResolution, source.VerticalResolution);
			}

			return result;
		}
		#endregion

		#region Binorize8bppGrayscale()
		private static Bitmap Binorize8bppGrayscale(Bitmap source, Rectangle clip, byte threshold)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sourceStride = sourceData.Stride;
				int resultStride = resultData.Stride;

				int width = sourceData.Width;
				int height = sourceData.Height;
				int x, y;

				unsafe
				{
					byte* pOrig = (byte*)sourceData.Scan0.ToPointer();
					byte* pCopy = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrent;

					for (y = 0; y < height; y++)
					{
						pCurrent = pOrig + y * sourceStride;

						for (x = 0; x < width; x++)
						{
							if (*(pCurrent++) > threshold)
								pCopy[y * resultData.Stride + (x >> 3)] |= (byte)(0x80 >> (x & 0x07));
						}
					}
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);

				if (source != null && result != null)
					Misc.SetBitmapResolution(result, source.HorizontalResolution, source.VerticalResolution);
			}

			return result;
		}
		#endregion	

		#region Resample4bppTo1bpp()
		private static unsafe Bitmap Binorize4bpp(Bitmap source, Rectangle clip, byte threshold)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			//taking care of palette
			byte[] palette = new byte[16];

			for (int i = 0; i < 16; i++)
				palette[i] = (byte)(source.Palette.Entries[i].R * 0.299F + source.Palette.Entries[i].G * 0.587F + source.Palette.Entries[i].B * 0.114F);

			try
			{
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int width = sourceData.Width;
				int height = sourceData.Height;

				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();
				byte* pCurrentS;

				for (int y = 0; y < height; y++)
				{
					pCurrentS = pSource + y * strideS;

					for (int x = 0; x < width; x = x + 2)
					{
						if ((palette[*pCurrentS >> 4]) > threshold)
							pResult[y * strideR + x / 8] |= (byte)(0x80 >> (x & 7));
						if ((palette[*pCurrentS & 0x0F]) > threshold)
							pResult[y * strideR + (x + 1) / 8] |= (byte)(0x80 >> ((x + 1) & 7));

						pCurrentS++;
					}
				}
			}
			finally
			{
#if DEBUG
				Console.WriteLine(new StackFrame().GetMethod().Name + "(): " + DateTime.Now.Subtract(start).ToString());
#endif
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);

				if (source != null && result != null)
					Misc.SetBitmapResolution(result, source.HorizontalResolution, source.VerticalResolution);
			}

			return result;
		}
		#endregion

		#endregion
	}
}
