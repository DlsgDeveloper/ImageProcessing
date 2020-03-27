using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Collections.Generic;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class ColorFullness
	{		
		//	PUBLIC METHODS

		public enum ColorMode
		{
			Color = 0,
			Grayscale = 1,
			BW = 2
		}


		#region class Peak
		public class Peak
		{
			byte extreme;
			byte leftLimit;
			byte rightLimit;

			public Peak(byte extreme, byte leftLimit, byte rightLimit)
			{
				this.extreme = extreme;
				this.leftLimit = leftLimit;
				this.rightLimit = rightLimit;
			}

			public byte Extreme { get { return extreme; } set { extreme = value; } }
			public byte LeftLimit { get { return leftLimit; } set { leftLimit = value; } }
			public byte RightLimit { get { return rightLimit; } set { rightLimit = value; } }

			#region GetPeakSize()
			public uint GetPeakSize(uint[] array)
			{
				uint size = 0;

				for (int i = leftLimit; i <= rightLimit; i++)
					size += array[i];

				return size;
			}
			#endregion

			#region IsValid()
			public bool IsValid(uint[] array, int minSize, int minHeight)
			{
				if (leftLimit > 0 && rightLimit < 255)
				{
					if ((array[extreme] - array[leftLimit] < minHeight || array[extreme] - array[rightLimit] < minHeight ||
						array[extreme] < minHeight) && (GetPeakSize(array) < minSize))
					{
						return false;
					}

					return true;
				}
				else if (leftLimit > 0)
				{
					if ((array[extreme] - array[leftLimit] < minHeight || array[extreme] < minHeight) &&
						(GetPeakSize(array) < minSize))
					{
						return false;
					}

					return true;
				}
				else
				{
					if ((array[extreme] < minHeight) && (GetPeakSize(array) < minSize))
					{
						return false;
					}

					return true;
				}
			}
			#endregion

			#region GetHeight()
			public uint GetHeight(uint[] array)
			{
				return (2 * array[extreme] - array[leftLimit] - array[rightLimit]) / 2;
			}
			#endregion

		}
		#endregion

		#region class Peaks
		public class Peaks : List<Peak>
		{
			public Peaks()
				: base()
			{
			}

			new public Peak this[int index]
			{
				get { return (Peak)base[index]; }
				set { base[index] = value; }
			}
		}
		#endregion
		
		#region Get()	
		public static ColorMode Get(Bitmap source)
		{
			return Get(source, Rectangle.Empty) ;
		}

		public static ColorMode Get(string filePath, Rectangle clip) 
		{ 			
			Bitmap		bitmap = new Bitmap(filePath);
			ColorMode	result =  Get(bitmap, clip);
			
			bitmap.Dispose();	
			return result;
		}
		
		public static ColorMode Get(Bitmap bitmap, Rectangle clip)
		{
			if(clip.IsEmpty)
			{
				if(bitmap.Width > 1000 && bitmap.Height > 1000)
					clip = Rectangle.FromLTRB(100, 100, bitmap.Width - 100, bitmap.Height - 100);
				else
					clip = Rectangle.FromLTRB(0, 0, bitmap.Width, bitmap.Height);
			}
			else if(clip.Width == 0 || clip.Height == 0)
				clip = Rectangle.FromLTRB(clip.X, clip.Y, bitmap.Width - clip.X * 2, bitmap.Height - clip.Y * 2);

			try
			{
				switch(bitmap.PixelFormat)
				{
					case PixelFormat.Format1bppIndexed :
						return ColorMode.BW;
					case PixelFormat.Format8bppIndexed :
						return Get8bpp(bitmap, clip) ;
					case PixelFormat.Format24bppRgb :				
						return Get24bpp(bitmap, clip) ;
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}
			}
			catch(Exception ex)
			{
				throw new Exception("ColorFullness, Get(): " + ex.Message ) ;
			}
		}
		#endregion
		
		#region GetStream() 
		/*public unsafe static ColorMode GetStream(byte** firstByte, int* length, Rectangle clip) 
		{ 			
#if DEBUG
			DateTime		enterTime = DateTime.Now ;
#endif
			byte[]			array = new byte[*length];
			Bitmap			bitmap;

			Marshal.Copy(new IntPtr(*firstByte), array, 0, (int) *length);

			MemoryStream	stream = new MemoryStream(array);

			try
			{
				bitmap = new Bitmap(stream) ;
			}
			catch(Exception ex)
			{
				throw new Exception("Can't generate bitmap.\nException: " + ex);
			}
			
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			ColorMode		colorMode = Get(bitmap, clip);
#if DEBUG
			TimeSpan	time = DateTime.Now.Subtract(start) ;

			Console.Write(string.Format("ColorFullness: {0}",  time.ToString())) ;
#endif
								
			bitmap.Dispose();
			stream.Close();

#if DEBUG
			Console.WriteLine(string.Format("Total Time: {0}",  DateTime.Now.Subtract(enterTime).ToString())) ;
#endif

			return colorMode;
		}*/
		#endregion
		
		#region GetMem()
		/*public unsafe static ColorMode GetMem(ref int width, ref int height, ref int stride, PixelFormat pixelFormat, 
			byte** firstByte, ColorPalette palette, Rectangle clip) 
		{ 			
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif
			
			try
			{
				SecurityPermission sp = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
				sp.Assert();

				Bitmap		bitmap = new Bitmap(width, height, stride, pixelFormat, new IntPtr(*firstByte));			
			
				if(bitmap == null)
					throw new Exception("ColorFullness(): Can't create bitmap from present parameters!") ;

				if(palette != null)
					bitmap.Palette = palette;
			
				ColorMode		colorMode = Get(bitmap, clip);

				bitmap.Dispose();
				return colorMode;
			}
			catch(Exception ex)
			{
				throw new Exception("ColorFullness(): " + ex.Message) ;
			}
			finally
			{
#if DEBUG
				TimeSpan	time = DateTime.Now.Subtract(start) ;
				Console.WriteLine(string.Format("RAM Image: {0}", time.ToString()));	
#endif
			}
		}*/
		#endregion
		
		//PRIVATE METHODS
		
		#region Get24bpp()
		private static ColorMode Get24bpp(Bitmap bitmap, Rectangle clip)
		{			
			BitmapData	bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadOnly, bitmap.PixelFormat); 
			int			jump;
			int			clipWidth = clip.Width;
			int			clipHeight = clip.Height;
			int			r, g, b ;
			int			stride = bitmapData.Stride; 
			int			x, y;
			int			colorDiffusion = 30;
			int			colorPixels = 0;
			 
			if(clip.Width > 1000 && clip.Height > 1000)
				jump = 8;
			else if(clip.Width > 500 && clip.Height > 500)
				jump = 4;
			else
				jump = 1;

			short		jumpBytes = (short) (jump * 3);	

			try
			{
				unsafe
				{
					byte*		pSource = (byte*) bitmapData.Scan0.ToPointer(); 
					byte*		pCurrent ;

					for(y = 0; y < clipHeight; y = y + jump) 
					{ 										
						pCurrent = pSource + y * stride;

						for(x = 0; x < clipWidth; x = x + jump) 
						{ 
							b = *pCurrent;
							g = pCurrent[1];
							r = pCurrent[2];

							if((r - g > colorDiffusion) || (r - g < -colorDiffusion) || 
								(b - g > colorDiffusion) || (b - g < -colorDiffusion) ||
								(r - b > colorDiffusion) || (r - b < -colorDiffusion))
							{ 
								colorPixels++;
							}
							
							pCurrent += jumpBytes;
						}
					}
				}

				if(bitmapData != null)
				{
					bitmap.UnlockBits(bitmapData);
					bitmapData = null;
				}

				if( (((float) colorPixels) / ((float) (clip.Width * clip.Height) / (jump * jump))) > .05F )
					return ColorMode.Color;

				Histogram		histogram = new Histogram(bitmap, clip);
				int					peaks = GetNumberOfPeaks(histogram.ArrayR, Histogram.ToGray(histogram.Threshold) , 0.10F, 0.15F);

				if(peaks == 2)
					return ColorMode.BW;
				else
					return ColorMode.Grayscale;
			}
			finally
			{
			}
		}
		#endregion

		#region Get8bpp()
		private static ColorMode Get8bpp(Bitmap bitmap, Rectangle clip)
		{
			Histogram	histogram = new Histogram(bitmap, clip);
			int				peaks = GetNumberOfPeaks(histogram.ArrayR, histogram.Threshold.R, 0.10F, 0.15F);

			if(peaks == 2)
				return ColorMode.BW;
			else
				return ColorMode.Grayscale;
		}
		#endregion

		#region GetNumberOfPeaks()
		private static int GetNumberOfPeaks(uint[] array, byte threshold, float peakMinSizeRatio, float peakMinHeightRatio)
		{
			int		numOfPeaks = 0;

			numOfPeaks += GetNumberOfPeaksInInterval(array, 0, threshold, peakMinSizeRatio, peakMinHeightRatio);
			numOfPeaks += GetNumberOfPeaksInInterval(array, (byte) (threshold + 1), 255, peakMinSizeRatio, peakMinHeightRatio);

			return numOfPeaks;
		}
		#endregion

		#region GetNumberOfPeaksInInterval()
		private static int GetNumberOfPeaksInInterval(uint[] array, byte from, byte to, float peakMinSizeRatio, float peakMinHeightRatio)
		{
			uint peakMinSize;
			uint peakMinHeight;
			uint peakMinRelativeHeight;
			Peaks peaks = new Peaks();
			int i;
			byte peakExtreme;
			byte leftLimit = from;
			byte rightLimit;
			uint totalPoints = 0;
			uint maxValue = 0;

			for (i = from; i <= to; i++)
			{
				totalPoints += array[i];

				if (maxValue < array[i])
					maxValue = array[i];
			}

			peakMinSize = (uint)(totalPoints * peakMinSizeRatio);
			peakMinHeight = (uint)(maxValue * peakMinHeightRatio);
			peakMinRelativeHeight = (uint)(maxValue / 100);

			for (i = from; i <= to; i++)
			{
				if ((i == to) || (array[i] > array[i + 1] && (array[i] > peakMinHeight)))
				{
					peakExtreme = (byte)i;
					i++;

					while (i < to && array[i] >= array[i + 1])
						i++;

					rightLimit = (byte)Math.Min(i, 255);
					peaks.Add(new Peak(peakExtreme, leftLimit, rightLimit));
					leftLimit = (byte)(rightLimit + 1);
				}
			}

			for (i = 0; i < peaks.Count; i++)
			{
				Peak peak = (Peak)peaks[i];

				if (i > 0 && i < peaks.Count - 1)
				{
					if ((peak.GetPeakSize(array) < peakMinSize) ||
						(array[peak.Extreme] - array[peak.LeftLimit] < peakMinRelativeHeight) ||
						(array[peak.Extreme] - array[peak.RightLimit] < peakMinRelativeHeight))
					{
						if (array[peaks[i - 1].Extreme] > array[peaks[i + 1].Extreme])
						{
							if (array[peaks[i - 1].RightLimit] > array[peak.RightLimit] * 3)
							{
								peaks[i - 1].RightLimit = peak.RightLimit;
								peaks[i - 1].Extreme = (array[peaks[i - 1].Extreme] > array[peak.Extreme]) ? peaks[i - 1].Extreme : peak.Extreme;
							}
						}
						else
						{
							if (array[peaks[i + 1].LeftLimit] > array[peak.LeftLimit] * 3)
							{
								peaks[i + 1].LeftLimit = peak.LeftLimit;
								peaks[i + 1].Extreme = (array[peaks[i + 1].Extreme] > array[peak.Extreme]) ? peaks[i + 1].Extreme : peak.Extreme;
							}
						}

						peaks.RemoveAt(i);
						i--;
					}
				}
				else if (i == 0 && peaks.Count > 1)
				{
					if ((peak.GetPeakSize(array) < peakMinSize) ||
						(array[peak.Extreme] - array[peak.RightLimit] < peakMinRelativeHeight))
					{
						if (array[peaks[i + 1].LeftLimit] > array[peak.LeftLimit] * 3)
						{
							peaks[i + 1].LeftLimit = peak.LeftLimit;
							peaks[i + 1].Extreme = (array[peaks[i + 1].Extreme] > array[peak.Extreme]) ? peaks[i + 1].Extreme : peak.Extreme;
						}

						peaks.RemoveAt(i);
						i--;
					}
				}
				else if (i == peaks.Count - 1 && peaks.Count > 1)
				{
					if ((peak.GetPeakSize(array) < peakMinSize) ||
						(array[peak.Extreme] <= peakMinHeight) ||
						(array[peak.Extreme] - array[peak.LeftLimit] < peakMinRelativeHeight))
					{
						if (array[peaks[i - 1].RightLimit] > array[peak.RightLimit] * 3)
						{
							peaks[i - 1].RightLimit = peak.RightLimit;
							peaks[i - 1].Extreme = (array[peaks[i - 1].Extreme] > array[peak.Extreme]) ? peaks[i - 1].Extreme : peak.Extreme;
						}

						peaks.RemoveAt(i);
						i--;
					}
				}
			}

			return peaks.Count;
		}
		#endregion
			
	}
}
