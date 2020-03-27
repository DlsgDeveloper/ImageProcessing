using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections ;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Interpolation.
	/// </summary>
	public class Interpolation
	{				
		#region constructor
		private Interpolation()
		{
		}
		#endregion

		#region enum Zoom
		public enum Zoom
		{
			Zoom1to2,
			Zoom2to3,
			Zoom3to4,
			Zoom2to1,
			Zoom3to1,
			Zoom3to2,
			Zoom4to1,
			Zoom4to3,
			Zoom6to1,
			Zoom8to1
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region GetSize()
		public static Size GetSize(int sourceWidth, int sourceHeight, ImageProcessing.Interpolation.Zoom zoom)
		{
			switch (zoom)
			{
				case Zoom.Zoom1to2: return new Size(sourceWidth * 2, sourceHeight * 2);
				case Zoom.Zoom2to3: return new Size(sourceWidth / 2 * 3, sourceHeight / 2 * 3);
				case Zoom.Zoom3to4: return new Size(sourceWidth / 3 * 4, sourceHeight / 3 * 4);
				case Zoom.Zoom2to1: return new Size(sourceWidth / 2, sourceHeight / 2);
				case Zoom.Zoom3to1: return new Size(sourceWidth / 3, sourceHeight / 3);
				case Zoom.Zoom3to2: return new Size(sourceWidth / 3 * 2, sourceHeight / 3 * 2);
				case Zoom.Zoom4to1: return new Size(sourceWidth / 4, sourceHeight / 4);
				case Zoom.Zoom4to3: return new Size(sourceWidth / 4 * 3, sourceHeight / 4 * 3);
				case Zoom.Zoom6to1: return new Size(sourceWidth / 6, sourceHeight / 6);
				case Zoom.Zoom8to1: return new Size(sourceWidth / 8, sourceHeight / 8);
				default: throw new IpException(ErrorCode.InvalidParameter, "Interpolation, GetSize()");
			}
		}
		#endregion

		#region Interpolate()
		public static Bitmap Interpolate(Bitmap source, ImageProcessing.Interpolation.Zoom zoom)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			if(source.PixelFormat != PixelFormat.Format8bppIndexed && source.PixelFormat != PixelFormat.Format24bppRgb)
				throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				int resultWidth = GetSize(source.Width, source.Height, zoom).Width;
				int resultHeight = GetSize(source.Width, source.Height, zoom).Height;
				
				result = new Bitmap(resultWidth, resultHeight, source.PixelFormat);

				sourceData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, resultWidth, resultHeight), ImageLockMode.WriteOnly, result.PixelFormat);

				switch (zoom)
				{
					case Zoom.Zoom3to4: Interpolate3to4(sourceData, resultData); break;
					case Zoom.Zoom2to3: Interpolate2to3(sourceData, resultData); break;
					case Zoom.Zoom1to2: Interpolate1to2(sourceData, resultData); break;
					case Zoom.Zoom2to1: Interpolate2to1(sourceData, resultData); break;
					case Zoom.Zoom3to1: Interpolate3to1(sourceData, resultData); break;
					case Zoom.Zoom3to2: Interpolate3to2(sourceData, resultData); break;
					case Zoom.Zoom4to1: Interpolate4to1(sourceData, resultData); break;
					case Zoom.Zoom4to3: Interpolate4to3(sourceData, resultData); break;
					case Zoom.Zoom6to1: Interpolate6to1(sourceData, resultData); break;
					case Zoom.Zoom8to1: Interpolate8to1(sourceData, resultData); break;
					default: throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			Misc.SetBitmapResolution(result, Convert.ToInt32(source.HorizontalResolution * (result.Width / (float)source.Width)),  Convert.ToInt32(source.VerticalResolution * (result.Height / (float)source.Height)));

			if (result.PixelFormat == PixelFormat.Format8bppIndexed)
				result.Palette = Misc.GetGrayscalePalette();

#if DEBUG
			Console.WriteLine("Interpolate(): " + DateTime.Now.Subtract(start).ToString());
#endif
			return result;
		}
		#endregion
		
		#region Interpolate24bppTo8bpp3to2()
		public static Bitmap Interpolate24bppTo8bpp3to2(Bitmap source)
		{
#if DEBUG
				DateTime	start = DateTime.Now;
#endif
			int			resultWidth = GetSize(source.Width, source.Height, Zoom.Zoom3to2).Width;
			int			resultHeight = GetSize(source.Width, source.Height, Zoom.Zoom3to2).Height;		
			Bitmap		result = new Bitmap(resultWidth, resultHeight, PixelFormat.Format8bppIndexed);
			int			x, y;
			int			red, green, blue;

			BitmapData	sourceData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, resultWidth, resultHeight), ImageLockMode.WriteOnly, result.PixelFormat);

			int		sStride = sourceData.Stride;
			int		rStride = resultData.Stride;

			try
			{
				unsafe
				{
					byte*	pOrigScan0 = (byte*) sourceData.Scan0.ToPointer(); 
					byte*	pDestScan0 = (byte*) resultData.Scan0.ToPointer(); 
					byte*	pOrig ; 
					byte*	pCopy ; 

					for(y = 0; y < resultHeight; y = y + 2) 
					{ 
						pOrig = pOrigScan0 + (y * 3 / 2) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for(x = 0; x < resultWidth; x = x + 2) 
						{ 
							//top row
							blue = (byte) ((4 * pOrig[0] + 2 * pOrig[3] + 2 * pOrig[sStride  ] + pOrig[sStride+3]) / 9);
							green = (byte) ((4 * pOrig[1] + 2 * pOrig[4] + 2 * pOrig[sStride+1] + pOrig[sStride+4]) / 9);
							red = (byte) ((4 * pOrig[2] + 2 * pOrig[5] + 2 * pOrig[sStride+2] + pOrig[sStride+5]) / 9);
							*(pCopy) = (byte) (red * 0.114F + green * 0.587F + blue * 0.299F);


							blue = (byte) ((2 * pOrig[3] + 4 * pOrig[6] + pOrig[sStride+3] + 2 * pOrig[sStride+6]) / 9);
							green = (byte) ((2 * pOrig[4] + 4 * pOrig[7] + pOrig[sStride+4] + 2 * pOrig[sStride+7]) / 9);
							red = (byte) ((2 * pOrig[5] + 4 * pOrig[8] + pOrig[sStride+5] + 2 * pOrig[sStride+8]) / 9);
							*(pCopy+1) = (byte) (red * 0.114F + green * 0.587F + blue * 0.299F);

							blue = (byte) ((2 * pOrig[sStride  ] + pOrig[sStride+3] + 4 * pOrig[2*sStride  ] + 2 * pOrig[2*sStride+3]) / 9);
							green = (byte) ((2 * pOrig[sStride+1] + pOrig[sStride+4] + 4 * pOrig[2*sStride+1] + 2 * pOrig[2*sStride+4]) / 9);
							red = (byte) ((2 * pOrig[sStride+2] + pOrig[sStride+5] + 4 * pOrig[2*sStride+2] + 2 * pOrig[2*sStride+5]) / 9);
							*(pCopy+rStride) = (byte) (red * 0.114F + green * 0.587F + blue * 0.299F);

							blue = (byte) ((pOrig[sStride+3] + 2 * pOrig[sStride+6] + 2 * pOrig[2*sStride+3] + 4 * pOrig[2*sStride+6]) / 9);
							green = (byte) ((pOrig[sStride+4] + 2 * pOrig[sStride+7] + 2 * pOrig[2*sStride+4] + 4 * pOrig[2*sStride+7]) / 9);
							red = (byte) ((pOrig[sStride+5] + 2 * pOrig[sStride+8] + 2 * pOrig[2*sStride+5] + 4 * pOrig[2*sStride+8]) / 9);
							*(pCopy+rStride+1) = (byte) (red * 0.114F + green * 0.587F + blue * 0.299F);

							//next area
							pOrig += 9;
							pCopy += 2;
						}
					}	
				}

			}
			finally
			{
				if(sourceData != null)
					source.UnlockBits(sourceData);
				if(result != null && resultData != null)
					result.UnlockBits(resultData);
			}
			
			Misc.SetBitmapResolution(result, Convert.ToInt32(source.HorizontalResolution * 2 / 3F), Convert.ToInt32(source.VerticalResolution * 2 / 3F));
			result.Palette = Misc.GrayscalePalette;

#if DEBUG
				Console.WriteLine("Interpolate24bppTo8bpp3to2(): " + DateTime.Now.Subtract(start).ToString());
#endif
			return result;
		}
		#endregion
		
		#region Interpolate24bppTo8bpp2to1()
		public static Bitmap Interpolate24bppTo8bpp2to1(Bitmap source)
		{
#if DEBUG
				DateTime	start = DateTime.Now;
#endif
			int			resultWidth = GetSize(source.Width, source.Height, Zoom.Zoom2to1).Width;
			int			resultHeight = GetSize(source.Width, source.Height, Zoom.Zoom2to1).Height;	
			Bitmap		result = new Bitmap(resultWidth, resultHeight, PixelFormat.Format8bppIndexed);
			int			x, y;
			int			red, green, blue;

			BitmapData	sourceData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, resultWidth, resultHeight), ImageLockMode.WriteOnly, result.PixelFormat);

			int		sStride = sourceData.Stride;
			int		rStride = resultData.Stride;

			try
			{
				unsafe
				{
					byte*	pOrigScan0 = (byte*) sourceData.Scan0.ToPointer(); 
					byte*	pDestScan0 = (byte*) resultData.Scan0.ToPointer(); 
					byte*	pOrig ; 
					byte*	pCopy ; 

					for(y = 0; y < resultHeight; y++) 
					{ 
						pOrig = pOrigScan0 + (y * 2) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for(x = 0; x < resultWidth; x++) 
						{ 
							//top row (red * 0.114F + green * 0.587F + blue * 0.299F)
							blue   = *pOrig + pOrig[3] + pOrig[sStride] + pOrig[sStride+3];
							green = pOrig[1] + pOrig[4] + pOrig[sStride+1] + pOrig[sStride+4];
							red = pOrig[2] + pOrig[5] + pOrig[sStride+2] + pOrig[sStride+5];
							*(pCopy) = (byte) (red * 0.0285F + green * 0.14675F + blue * 0.07475F);
							/*blue   = (*pOrig + pOrig[3] + pOrig[sStride] + pOrig[sStride+3]) / 4;
							green = (pOrig[1] + pOrig[4] + pOrig[sStride+1] + pOrig[sStride+4]) / 4;
							red = (pOrig[2] + pOrig[5] + pOrig[sStride+2] + pOrig[sStride+5]) / 4;
							*(pCopy) = (byte) (red * 0.114F + green * 0.587F + blue * 0.299F);*/

							pCopy ++ ;
							pOrig += 6;
						}
					}	
				}

			}
			finally
			{
				if(sourceData != null)
					source.UnlockBits(sourceData);
				if(result != null && resultData != null)
					result.UnlockBits(resultData);
			}
			
			result.Palette = Misc.GrayscalePalette;
			Misc.SetBitmapResolution(result, Convert.ToInt32(source.HorizontalResolution / 2F), Convert.ToInt32(source.VerticalResolution / 2F));

#if DEBUG
				Console.WriteLine("Interpolate24bppTo8bpp2to1(): " + DateTime.Now.Subtract(start).ToString());
#endif
			return result;
		}
		#endregion

		#region Interpolate24bppTo8bpp3to1()
		public static Bitmap Interpolate24bppTo8bpp3to1(Bitmap source)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			int resultWidth = GetSize(source.Width, source.Height, Zoom.Zoom3to1).Width;
			int resultHeight = GetSize(source.Width, source.Height, Zoom.Zoom3to1).Height;
			Bitmap result = new Bitmap(resultWidth, resultHeight, PixelFormat.Format8bppIndexed);
			int x, y;
			int red, green, blue;

			BitmapData sourceData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
			BitmapData resultData = result.LockBits(new Rectangle(0, 0, resultWidth, resultHeight), ImageLockMode.WriteOnly, result.PixelFormat);

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			try
			{
				unsafe
				{
					byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
					byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
					byte* pOrig;
					byte* pCopy;

					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 3) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row (red * 0.114F + green * 0.587F + blue * 0.299F)
							blue = (byte)((*pOrig + pOrig[3] + pOrig[6] + 
								pOrig[sStride] + pOrig[sStride + 3] + pOrig[sStride + 6] + 
								pOrig[2 * sStride] + pOrig[2 * sStride + 3] + pOrig[2 * sStride + 6] 
								) / 9);
							green = (byte)((pOrig[1] + pOrig[4] + pOrig[7] + 
								pOrig[sStride + 1] + pOrig[sStride + 4] + pOrig[sStride + 7] + 
								pOrig[2 * sStride + 1] + pOrig[2 * sStride + 4] + pOrig[2 * sStride + 7] 
								) / 9);
							red = (byte)((pOrig[2] + pOrig[5] + pOrig[8] +
								pOrig[sStride + 2] + pOrig[sStride + 5] + pOrig[sStride + 8] + 
								pOrig[2 * sStride + 2] + pOrig[2 * sStride + 5] + pOrig[2 * sStride + 8] 
								) >> 4);
							*(pCopy) = (byte)(red * 0.114F + green * 0.587F + blue * 0.299F);

							pCopy++;
							pOrig += 9;
						}
					}
				}

			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			result.Palette = Misc.GrayscalePalette;
			Misc.SetBitmapResolution(result, Convert.ToInt32(source.HorizontalResolution / 3F), Convert.ToInt32(source.VerticalResolution / 3F));

#if DEBUG
			Console.WriteLine("Interpolate24bppTo8bpp3to1(): " + DateTime.Now.Subtract(start).ToString());
#endif
			return result;
		}
		#endregion
	
		#region Interpolate24bppTo8bpp4to1()
		public static Bitmap Interpolate24bppTo8bpp4to1(Bitmap source)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			int resultWidth = GetSize(source.Width, source.Height, Zoom.Zoom4to1).Width;
			int resultHeight = GetSize(source.Width, source.Height, Zoom.Zoom4to1).Height;
			Bitmap result = new Bitmap(resultWidth, resultHeight, PixelFormat.Format8bppIndexed);
			int x, y;
			int red, green, blue;

			BitmapData sourceData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
			BitmapData resultData = result.LockBits(new Rectangle(0, 0, resultWidth, resultHeight), ImageLockMode.WriteOnly, result.PixelFormat);

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			try
			{
				unsafe
				{
					byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
					byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
					byte* pOrig;
					byte* pCopy;

					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 4) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row (red * 0.114F + green * 0.587F + blue * 0.299F)
							blue = (byte)((*pOrig + pOrig[3] + pOrig[6] + pOrig[9] +
								pOrig[sStride] + pOrig[sStride + 3] + pOrig[sStride + 6] + pOrig[sStride + 9] +
								pOrig[2 * sStride] + pOrig[2 * sStride + 3] + pOrig[2 * sStride + 6] + pOrig[2 * sStride + 9] +
								pOrig[3 * sStride] + pOrig[3 * sStride + 3] + pOrig[3 * sStride + 6] + pOrig[3 * sStride + 9]) >> 4);
							green = (byte)((pOrig[1] + pOrig[4] + pOrig[7] + pOrig[10] +
								pOrig[sStride + 1] + pOrig[sStride + 4] + pOrig[sStride + 7] + pOrig[sStride + 10] +
								pOrig[2 * sStride + 1] + pOrig[2 * sStride + 4] + pOrig[2 * sStride + 7] + pOrig[2 * sStride + 10] +
								pOrig[3 * sStride + 1] + pOrig[3 * sStride + 4] + pOrig[3 * sStride + 7] + pOrig[3 * sStride + 10]) >> 4);
							red = (byte)((pOrig[2] + pOrig[5] + pOrig[8] + pOrig[11] +
								pOrig[sStride + 2] + pOrig[sStride + 5] + pOrig[sStride + 8] + pOrig[sStride + 11] +
								pOrig[2 * sStride + 2] + pOrig[2 * sStride + 5] + pOrig[2 * sStride + 8] + pOrig[2 * sStride + 11] +
								pOrig[3 * sStride + 2] + pOrig[3 * sStride + 5] + pOrig[3 * sStride + 8] + pOrig[3 * sStride + 11]) >> 4);
							*(pCopy) = (byte)(red * 0.114F + green * 0.587F + blue * 0.299F);

							pCopy++;
							pOrig += 12;
						}
					}
				}

			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			//result.Palette = Misc.GetGrayscalePalette();
			//Misc.SetBitmapResolution(result, 150F, 150F);
			result.Palette = Misc.GrayscalePalette;
			Misc.SetBitmapResolution(result, Convert.ToInt32(source.HorizontalResolution / 4F), Convert.ToInt32(source.VerticalResolution / 4F));

#if DEBUG
			Console.WriteLine("Interpolate24bppTo8bpp4to1(): " + DateTime.Now.Subtract(start).ToString());
#endif
			return result;
		}
		#endregion

		#region Interpolate24bppTo8bpp6to1()
		public static Bitmap Interpolate24bppTo8bpp6to1(Bitmap source)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			int resultWidth = GetSize(source.Width, source.Height, Zoom.Zoom6to1).Width;
			int resultHeight = GetSize(source.Width, source.Height, Zoom.Zoom6to1).Height;
			Bitmap result = new Bitmap(resultWidth, resultHeight, PixelFormat.Format8bppIndexed);
			int x, y;
			int red, green, blue;

			BitmapData sourceData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
			BitmapData resultData = result.LockBits(new Rectangle(0, 0, resultWidth, resultHeight), ImageLockMode.WriteOnly, result.PixelFormat);

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			try
			{
				unsafe
				{
					byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
					byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
					byte* pOrig;
					byte* pCopy;

					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 6) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row (red * 0.114F + green * 0.587F + blue * 0.299F)
							blue = (byte)((*pOrig + pOrig[3] + pOrig[6] + pOrig[9] + pOrig[12] + pOrig[15] +
								pOrig[sStride] + pOrig[sStride + 3] + pOrig[sStride + 6] + pOrig[sStride + 9] + pOrig[sStride + 12] + pOrig[sStride + 15] +
								pOrig[2 * sStride] + pOrig[2 * sStride + 3] + pOrig[2 * sStride + 6] + pOrig[2 * sStride + 9] + pOrig[2 * sStride + 12] + pOrig[2 * sStride + 15] +
								pOrig[3 * sStride] + pOrig[3 * sStride + 3] + pOrig[3 * sStride + 6] + pOrig[3 * sStride + 9] + pOrig[3 * sStride + 12] + pOrig[3 * sStride + 15] +
								pOrig[3 * sStride] + pOrig[4 * sStride + 3] + pOrig[4 * sStride + 6] + pOrig[4 * sStride + 9] + pOrig[4 * sStride + 12] + pOrig[4 * sStride + 15] +
								pOrig[3 * sStride] + pOrig[5 * sStride + 3] + pOrig[5 * sStride + 6] + pOrig[5 * sStride + 9] + pOrig[5 * sStride + 12] + pOrig[5 * sStride + 15])
									/ 36);
							green = (byte)((pOrig[1] + pOrig[4] + pOrig[7] + pOrig[10] + pOrig[13] + pOrig[16] +
								pOrig[sStride + 1] + pOrig[sStride + 4] + pOrig[sStride + 7] + pOrig[sStride + 10] + pOrig[sStride + 13] + pOrig[sStride + 16] +
								pOrig[2 * sStride + 1] + pOrig[2 * sStride + 4] + pOrig[2 * sStride + 7] + pOrig[2 * sStride + 10] + pOrig[2 * sStride + 13] + pOrig[2 * sStride + 16] +
								pOrig[3 * sStride + 1] + pOrig[3 * sStride + 4] + pOrig[3 * sStride + 7] + pOrig[3 * sStride + 10] + pOrig[3 * sStride + 13] + pOrig[3 * sStride + 16] +
								pOrig[4 * sStride + 1] + pOrig[4 * sStride + 4] + pOrig[4 * sStride + 7] + pOrig[4 * sStride + 10] + pOrig[4 * sStride + 13] + pOrig[4 * sStride + 16] +
								pOrig[5 * sStride + 1] + pOrig[5 * sStride + 4] + pOrig[5 * sStride + 7] + pOrig[5 * sStride + 10] + pOrig[5 * sStride + 13] + pOrig[5 * sStride + 16]
								) / 36);
							red = (byte)((pOrig[2] + pOrig[5] + pOrig[8] + pOrig[11] +
								pOrig[sStride + 2] + pOrig[sStride + 5] + pOrig[sStride + 8] + pOrig[sStride + 11] + pOrig[sStride + 14] + pOrig[sStride + 17] +
								pOrig[2 * sStride + 2] + pOrig[2 * sStride + 5] + pOrig[2 * sStride + 8] + pOrig[2 * sStride + 11] + pOrig[2 * sStride + 14] + pOrig[2 * sStride + 17] +
								pOrig[3 * sStride + 2] + pOrig[3 * sStride + 5] + pOrig[3 * sStride + 8] + pOrig[3 * sStride + 11] + pOrig[3 * sStride + 14] + pOrig[3 * sStride + 17] +
								pOrig[4 * sStride + 2] + pOrig[4 * sStride + 5] + pOrig[4 * sStride + 8] + pOrig[4 * sStride + 11] + pOrig[4 * sStride + 14] + pOrig[4 * sStride + 17] +
								pOrig[5 * sStride + 2] + pOrig[5 * sStride + 5] + pOrig[5 * sStride + 8] + pOrig[5 * sStride + 11] + pOrig[5 * sStride + 14] + pOrig[5 * sStride + 17]
								) / 36);
							*(pCopy) = (byte)(red * 0.114F + green * 0.587F + blue * 0.299F);

							pCopy++;
							pOrig += 18;
						}
					}
				}

			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			result.Palette = Misc.GrayscalePalette;
			Misc.SetBitmapResolution(result, Convert.ToInt32(source.HorizontalResolution / 6F), Convert.ToInt32(source.VerticalResolution / 6F));

#if DEBUG
			Console.WriteLine("Interpolate24bppTo8bpp6to1(): " + DateTime.Now.Subtract(start).ToString());
#endif
			return result;
		}
		#endregion

		#region Interpolate24bppTo1bpp()
		/*public static Bitmap Interpolate24bppTo1bpp(Bitmap source)
		{
			return ImageProcessing.DRS.Binorize(source, -20, -50);
		}*/
		#endregion
		
		#region Interpolate24bppTo1bpp3to4()
		/*public static Bitmap Interpolate24bppTo1bpp3to4(Bitmap source)
		{
			Bitmap		intepolation = Interpolate(source, Interpolation.Zoom.Zoom3to2);
			Bitmap		result = Interpolate24bppTo1bpp1to2(intepolation);
			intepolation.Dispose();

			return result;
		}*/
		#endregion

		#region Interpolate24bppTo1bpp1to2()
		/*public static Bitmap Interpolate24bppTo1bpp1to2(Bitmap source)
		{
			return ImageProcessing.DRS2.Get(source, 0, 0, false);	
		}*/
		#endregion

		#region Interpolate24bppTo1bpp2to1()
		/*public static Bitmap Interpolate24bppTo1bpp2to1(Bitmap source)
		{
			Bitmap intepolation = Interpolate24bpp2to1(source);
			Bitmap result = ImageProcessing.BinorizationThreshold.Binorize(intepolation);
			intepolation.Dispose();

			return result;
		}*/
		#endregion
		
		#region Interpolate8bppTo1bpp2to1()
		public static Bitmap Interpolate8bppTo1bpp2to1(Bitmap source, int thresholdDelta)
		{
			Histogram	histogram;

			if(source.Width < 800 || source.Height < 800)
				histogram = new Histogram(source);
			else
				histogram = new Histogram(source, Rectangle.Inflate(new Rectangle(0, 0, source.Width, source.Height), -100, -100));
			
			int			resultWidth = GetSize(source.Width, source.Height, Zoom.Zoom2to1).Width;
			int			resultHeight = GetSize(source.Width, source.Height, Zoom.Zoom2to1).Height;		
			Bitmap		result = new Bitmap(resultWidth, resultHeight, PixelFormat.Format1bppIndexed); 
			BitmapData	sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat ); 
			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat ); 

			int			threshold = (Math.Max(100, Math.Min(240, histogram.Threshold.R + thresholdDelta))) * 4;		
			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride; 
			 
			unsafe
			{
				byte*		pSource = (byte*)sourceData.Scan0.ToPointer(); 
				byte*		pResult = (byte*)resultData.Scan0.ToPointer(); 
				byte*		pCurrent ;

				for(int y = 0; y < resultHeight; y++) 
				{ 					
					pCurrent = pSource + (y * 2 * sStride);

					for(int x = 0; x < resultWidth; x++) 
					{ 		
						if((*pCurrent + pCurrent[1] + pCurrent[sStride] + pCurrent[sStride+1]) > threshold)
							pResult[y * rStride + ( x >> 3 )] |= (byte) (0x80 >> (x & 0x07)); //set the appropriate bit to 1

						pCurrent += 2 ;
					}
				}
			}

			source.UnlockBits(sourceData);
			result.UnlockBits(resultData); 

			Misc.SetBitmapResolution(result, Convert.ToInt32(source.HorizontalResolution / 2), Convert.ToInt32(source.VerticalResolution / 2));

			return result; 
		}
		#endregion
				
		#region Interpolate8bppTo1bpp4to1()
		public static Bitmap Interpolate8bppTo1bpp4to1(Bitmap source, int thresholdDelta)
		{
			Histogram	histogram;

			if(source.Width < 800 || source.Height < 800)
				histogram = new Histogram(source);
			else
				histogram = new Histogram(source, Rectangle.Inflate(new Rectangle(0, 0, source.Width, source.Height), -100, -100));
			
			int			resultWidth = GetSize(source.Width, source.Height, Zoom.Zoom4to1).Width;
			int			resultHeight =  GetSize(source.Width, source.Height, Zoom.Zoom4to1).Height;		
			Bitmap		result = new Bitmap(resultWidth, resultHeight, PixelFormat.Format1bppIndexed); 
			BitmapData	sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat ); 
			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat ); 

			int			threshold = (Math.Max(100, Math.Min(240, Histogram.ToGray(histogram.Threshold) + thresholdDelta))) * 16;		
			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride; 
			 
			unsafe
			{
				byte*		pSource = (byte*)sourceData.Scan0.ToPointer(); 
				byte*		pResult = (byte*)resultData.Scan0.ToPointer(); 
				byte*		pCurrent ;

				for(int y = 0; y < resultHeight; y++) 
				{ 					
					pCurrent = pSource + (y * 4 * sStride);

					for(int x = 0; x < resultWidth; x++) 
					{ 		
						if((*pCurrent + pCurrent[1] + pCurrent[2] + pCurrent[3] +
							pCurrent[sStride] + pCurrent[sStride+1] + pCurrent[sStride+2] + pCurrent[sStride+3] +
							pCurrent[2*sStride] + pCurrent[2*sStride+1] + pCurrent[2*sStride+2] + pCurrent[2*sStride+3] +
							pCurrent[3*sStride] + pCurrent[3*sStride+1] + pCurrent[3*sStride+2] + pCurrent[3*sStride+3]) > threshold)
							pResult[y * rStride + ( x >> 3 )] |= (byte) (0x80 >> (x & 0x07)); //set the appropriate bit to 1

						pCurrent += 4 ;
					}
				}
			}

			source.UnlockBits(sourceData);
			result.UnlockBits(resultData); 

			Misc.SetBitmapResolution(result, Convert.ToInt32(source.HorizontalResolution / 4), Convert.ToInt32(source.VerticalResolution / 4));

			return result; 
		}
		#endregion

		#region Interpolate1bpp4to1()
		public static Bitmap Interpolate1bpp4to1(Bitmap source)
		{
			int			x, y;
			int			sourceWidth = source.Width / 8;
			int			resultWidth = GetSize(source.Width, source.Height, Zoom.Zoom4to1).Width;
			int			resultHeight =  GetSize(source.Width, source.Height, Zoom.Zoom4to1).Height;		
			Bitmap		result = new Bitmap(resultWidth, resultHeight, PixelFormat.Format1bppIndexed);

			BitmapData	sourceData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, resultWidth, resultHeight), ImageLockMode.WriteOnly, result.PixelFormat);

			int		sStride = sourceData.Stride;
			int		rStride = resultData.Stride;

			try
			{
				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer(); 
					byte*	pDest = (byte*) resultData.Scan0.ToPointer(); 
					byte*	pOrig ; 
					byte*	pCopy ; 
					byte	line1;
					byte	line2;
					byte	line3;
					byte	line4;
					int		blackColors;
					byte	divider;

					for(y = 0; y < resultHeight; y++) 
					{ 
						divider = 0x80;
						pOrig = pSource + y * 4 * sStride;
						//pCopy = pDest + y * rStride;

						for(x = 0; x < sourceWidth; x++) 
						{ 
							pCopy = pDest + y * rStride + (x >> 2);

							line1 = *pOrig;
							line2 = *(pOrig + sStride);
							line3 = *(pOrig + sStride * 2);
							line4 = *(pOrig + sStride * 3);

							blackColors = (int) ((line1 >> 7) & 0x1);
							blackColors += (int) ((line1 >> 6) & 0x1);
							blackColors += (int) ((line1 >> 5) & 0x1);
							blackColors += (int) ((line1 >> 4) & 0x1);
							blackColors += (int) ((line2 >> 7) & 0x1);
							blackColors += (int) ((line2 >> 6) & 0x1);
							blackColors += (int) ((line2 >> 5) & 0x1);
							blackColors += (int) ((line2 >> 4) & 0x1);
							blackColors += (int) ((line3 >> 7) & 0x1);
							blackColors += (int) ((line3 >> 6) & 0x1);
							blackColors += (int) ((line3 >> 5) & 0x1);
							blackColors += (int) ((line3 >> 4) & 0x1);
							blackColors += (int) ((line4 >> 7) & 0x1);
							blackColors += (int) ((line4 >> 6) & 0x1);
							blackColors += (int) ((line4 >> 5) & 0x1);
							blackColors += (int) ((line4 >> 4) & 0x1);
							
							if(blackColors > 8)
							{
								*pCopy = (byte) (*pCopy | divider);
							}
							else
							{
								blackColors = 0;
							}
							
							blackColors = (int) ((line1 >> 3) & 0x1);
							blackColors += (int) ((line1 >> 2) & 0x1);
							blackColors += (int) ((line1 >> 1) & 0x1);
							blackColors += (int) ((line1 >> 0) & 0x1);
							blackColors += (int) ((line2 >> 3) & 0x1);
							blackColors += (int) ((line2 >> 2) & 0x1);
							blackColors += (int) ((line2 >> 1) & 0x1);
							blackColors += (int) ((line2 >> 0) & 0x1);
							blackColors += (int) ((line3 >> 3) & 0x1);
							blackColors += (int) ((line3 >> 2) & 0x1);
							blackColors += (int) ((line3 >> 1) & 0x1);
							blackColors += (int) ((line3 >> 0) & 0x1);
							blackColors += (int) ((line4 >> 3) & 0x1);
							blackColors += (int) ((line4 >> 2) & 0x1);
							blackColors += (int) ((line4 >> 1) & 0x1);
							blackColors += (int) ((line4 >> 0) & 0x1);
							if(blackColors > 8)
								*pCopy = (byte) (*pCopy | (divider >> 1));
							else
								blackColors = 0;

							if(divider == 0x2)
								divider = 0x80;
							else
								divider = (byte) (divider >> 2);
							
							pOrig = pOrig + 1;
						}
					}
				}
			}
			finally
			{
				if(sourceData != null)
					source.UnlockBits(sourceData);
				if(result != null && resultData != null)
					result.UnlockBits(resultData);
			}
			
			//Misc.SetBitmapResolution(result, 75F, 75F);
			Misc.SetBitmapResolution(result, Convert.ToInt32(source.HorizontalResolution / 4F), Convert.ToInt32(source.VerticalResolution / 4F));
			return result;
		}
		#endregion

		#region Interpolate1bpp2to1()
		public static Bitmap Interpolate1bpp2to1(Bitmap source)
		{
			int			resultWidth = GetSize(source.Width, source.Height, Zoom.Zoom2to1).Width;
			int			resultHeight = GetSize(source.Width, source.Height, Zoom.Zoom2to1).Height;		
			Bitmap		result = new Bitmap(resultWidth, resultHeight, PixelFormat.Format1bppIndexed);

			BitmapData	sourceData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, resultWidth, resultHeight), ImageLockMode.WriteOnly, result.PixelFormat);

			int		sStride = sourceData.Stride;
			int		rStride = resultData.Stride;

			try
			{
				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer(); 
					byte*	pDest = (byte*) resultData.Scan0.ToPointer(); 
					byte*	pOrig ; 
					byte*	pCopy ; 
					byte	upperLine;
					byte	lowerLine;
					int		blackColors;
					int		resultWidthDevidedBy4 = resultWidth / 4;

					for(int y = 0; y < resultHeight; y++) 
					{ 
						pOrig = pSource + y * 2 * sStride;
						pCopy = pDest + y * rStride;

						for(int x = 0; x < resultWidthDevidedBy4; x = x + 2) 
						{ 
							upperLine = *pOrig;
							lowerLine = *(pOrig + sStride);

							blackColors = (int) ((upperLine >> 7) & 0x1);
							blackColors += (int) ((upperLine >> 6) & 0x1);
							blackColors += (int) ((lowerLine >> 7) & 0x1);
							blackColors += (int) ((lowerLine >> 6) & 0x1);
							if(blackColors > 2)
								*pCopy = (byte) (*pCopy | 0x80);
							
							blackColors = (int) ((upperLine >> 5) & 0x1);
							blackColors += (int) ((upperLine >> 4) & 0x1);
							blackColors += (int) ((lowerLine >> 5) & 0x1);
							blackColors += (int) ((lowerLine >> 4) & 0x1);
							if(blackColors > 2)
								*pCopy = (byte) (*pCopy | 0x40);

							blackColors = (int) ((upperLine >> 3) & 0x1);
							blackColors += (int) ((upperLine >> 2) & 0x1);
							blackColors += (int) ((lowerLine >> 3) & 0x1);
							blackColors += (int) ((lowerLine >> 2) & 0x1);
							if(blackColors > 2)
								*pCopy = (byte) (*pCopy | 0x20);

							blackColors = (int) ((upperLine >> 1) & 0x1);
							blackColors += (int) ((upperLine >> 0) & 0x1);
							blackColors += (int) ((lowerLine >> 1) & 0x1);
							blackColors += (int) ((lowerLine >> 0) & 0x1);
							if(blackColors > 2)
								*pCopy = (byte) (*pCopy | 0x10);

							
							
							upperLine = *(pOrig + 1);
							lowerLine = *(pOrig + 1 + sStride);

							blackColors = (int) ((upperLine >> 7) & 0x1);
							blackColors += (int) ((upperLine >> 6) & 0x1);
							blackColors += (int) ((lowerLine >> 7) & 0x1);
							blackColors += (int) ((lowerLine >> 6) & 0x1);
							if(blackColors > 2)
								*pCopy = (byte) (*pCopy | 0x8);
							
							blackColors = (int) ((upperLine >> 5) & 0x1);
							blackColors += (int) ((upperLine >> 4) & 0x1);
							blackColors += (int) ((lowerLine >> 5) & 0x1);
							blackColors += (int) ((lowerLine >> 4) & 0x1);
							if(blackColors > 2)
								*pCopy = (byte) (*pCopy | 0x4);

							blackColors = (int) ((upperLine >> 3) & 0x1);
							blackColors += (int) ((upperLine >> 2) & 0x1);
							blackColors += (int) ((lowerLine >> 3) & 0x1);
							blackColors += (int) ((lowerLine >> 2) & 0x1);
							if(blackColors > 2)
								*pCopy = (byte) (*pCopy | 0x2);

							blackColors = (int) ((upperLine >> 1) & 0x1);
							blackColors += (int) ((upperLine >> 0) & 0x1);
							blackColors += (int) ((lowerLine >> 1) & 0x1);
							blackColors += (int) ((lowerLine >> 0) & 0x1);
							if(blackColors > 2)
								*pCopy = (byte) (*pCopy | 0x1);

							pOrig = pOrig + 2;
							pCopy = pCopy + 1;
						}
					}
				}
			}
			finally
			{
				if(sourceData != null)
					source.UnlockBits(sourceData);
				if(result != null && resultData != null)
					result.UnlockBits(resultData);
			}
			
			Misc.SetBitmapResolution(result, Convert.ToInt32(source.HorizontalResolution / 2F), Convert.ToInt32(source.VerticalResolution / 2F));
			return result;
		}
		#endregion

		#region Interpolate1bppTo8bpp4to1()
		public static Bitmap Interpolate1bppTo8bpp4to1(Bitmap source)
		{
			int x, y;
			int resultWidth = GetSize(source.Width, source.Height, Zoom.Zoom4to1).Width;
			int resultHeight = GetSize(source.Width, source.Height, Zoom.Zoom4to1).Height;
			Bitmap result = new Bitmap(resultWidth, resultHeight, PixelFormat.Format8bppIndexed);

			BitmapData sourceData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
			BitmapData resultData = result.LockBits(new Rectangle(0, 0, resultWidth, resultHeight), ImageLockMode.WriteOnly, result.PixelFormat);

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			try
			{
				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pDest = (byte*)resultData.Scan0.ToPointer();
					byte* pOrig;
					byte* pCopy;
					byte line1;
					byte line2;
					byte line3;
					byte line4;
					int blackColors;

					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pSource + y * 4 * sStride;
						pCopy = pDest + y * rStride;

						for (x = 0; x < resultWidth; x = x + 2)
						{
							line1 = *pOrig;
							line2 = *(pOrig + sStride);
							line3 = *(pOrig + sStride * 2);
							line4 = *(pOrig + sStride * 3);

							blackColors = (int)((line1 >> 7) & 0x1);
							blackColors += (int)((line1 >> 6) & 0x1);
							blackColors += (int)((line1 >> 5) & 0x1);
							blackColors += (int)((line1 >> 4) & 0x1);
							blackColors += (int)((line2 >> 7) & 0x1);
							blackColors += (int)((line2 >> 6) & 0x1);
							blackColors += (int)((line2 >> 5) & 0x1);
							blackColors += (int)((line2 >> 4) & 0x1);
							blackColors += (int)((line3 >> 7) & 0x1);
							blackColors += (int)((line3 >> 6) & 0x1);
							blackColors += (int)((line3 >> 5) & 0x1);
							blackColors += (int)((line3 >> 4) & 0x1);
							blackColors += (int)((line4 >> 7) & 0x1);
							blackColors += (int)((line4 >> 6) & 0x1);
							blackColors += (int)((line4 >> 5) & 0x1);
							blackColors += (int)((line4 >> 4) & 0x1);

							pCopy[x] = (byte)(blackColors / 16.0 * 255);

							blackColors = (int)((line1 >> 3) & 0x1);
							blackColors += (int)((line1 >> 2) & 0x1);
							blackColors += (int)((line1 >> 1) & 0x1);
							blackColors += (int)((line1 >> 0) & 0x1);
							blackColors += (int)((line2 >> 3) & 0x1);
							blackColors += (int)((line2 >> 2) & 0x1);
							blackColors += (int)((line2 >> 1) & 0x1);
							blackColors += (int)((line2 >> 0) & 0x1);
							blackColors += (int)((line3 >> 3) & 0x1);
							blackColors += (int)((line3 >> 2) & 0x1);
							blackColors += (int)((line3 >> 1) & 0x1);
							blackColors += (int)((line3 >> 0) & 0x1);
							blackColors += (int)((line4 >> 3) & 0x1);
							blackColors += (int)((line4 >> 2) & 0x1);
							blackColors += (int)((line4 >> 1) & 0x1);
							blackColors += (int)((line4 >> 0) & 0x1);

							pCopy[x + 1] = (byte)(blackColors / 16.0 * 255);

							pOrig++;
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			//Misc.SetBitmapResolution(result, 75F, 75F);
			Misc.SetBitmapResolution(result, Convert.ToInt32(source.HorizontalResolution / 4F), Convert.ToInt32(source.VerticalResolution / 4F));
			result.Palette = Misc.GetGrayscalePalette();
			
			return result;
		}
		#endregion

		#region Interpolate1bppTo8bpp2to1()
		public static Bitmap Interpolate1bppTo8bpp2to1(Bitmap source)
		{
			int x, y;
			int resultWidth = GetSize(source.Width, source.Height, Zoom.Zoom2to1).Width;
			int resultHeight = GetSize(source.Width, source.Height, Zoom.Zoom2to1).Height;
			Bitmap result = new Bitmap(resultWidth, resultHeight, PixelFormat.Format8bppIndexed);

			BitmapData sourceData = source.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);
			BitmapData resultData = result.LockBits(new Rectangle(0, 0, resultWidth, resultHeight), ImageLockMode.WriteOnly, result.PixelFormat);

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			try
			{
				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pDest = (byte*)resultData.Scan0.ToPointer();
					byte* pOrig;
					byte* pCopy;
					byte upperLine;
					byte lowerLine;
					int blackColors;
					int resultWidthDevidedBy4 = resultWidth / 4;

					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pSource + y * 2 * sStride;
						pCopy = pDest + y * rStride;

						for (x = 0; x < resultWidth; x = x + 4)
						{
							upperLine = *pOrig;
							lowerLine = *(pOrig + sStride);

							blackColors = (int)((upperLine >> 7) & 0x1);
							blackColors += (int)((upperLine >> 6) & 0x1);
							blackColors += (int)((lowerLine >> 7) & 0x1);
							blackColors += (int)((lowerLine >> 6) & 0x1);
							pCopy[x] = (byte)(blackColors / 4.0 * 255);

							blackColors = (int)((upperLine >> 5) & 0x1);
							blackColors += (int)((upperLine >> 4) & 0x1);
							blackColors += (int)((lowerLine >> 5) & 0x1);
							blackColors += (int)((lowerLine >> 4) & 0x1);
							pCopy[x + 1] = (byte)(blackColors / 4.0 * 255);

							blackColors = (int)((upperLine >> 3) & 0x1);
							blackColors += (int)((upperLine >> 2) & 0x1);
							blackColors += (int)((lowerLine >> 3) & 0x1);
							blackColors += (int)((lowerLine >> 2) & 0x1);
							pCopy[x + 2] = (byte)(blackColors / 4.0 * 255);

							blackColors = (int)((upperLine >> 1) & 0x1);
							blackColors += (int)((upperLine >> 0) & 0x1);
							blackColors += (int)((lowerLine >> 1) & 0x1);
							blackColors += (int)((lowerLine >> 0) & 0x1);
							pCopy[x + 3] = (byte)(blackColors / 4.0 * 255);

							pOrig ++;
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}

			//Misc.SetBitmapResolution(result, 75F, 75F);
			Misc.SetBitmapResolution(result, Convert.ToInt32(source.HorizontalResolution / 4F), Convert.ToInt32(source.VerticalResolution / 4F));
			result.Palette = Misc.GetGrayscalePalette();

			return result;
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region Interpolate3to2()
		private static void Interpolate3to2(BitmapData sourceData, BitmapData resultData)
		{
			int resultWidth = resultData.Width;
			int resultHeight = resultData.Height;
			int x, y;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			unsafe
			{
				byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
				byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
				byte* pOrig;
				byte* pCopy;

				if (resultData.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (y = 0; y < resultHeight; y = y + 2)
					{
						pOrig = pOrigScan0 + (y * 3 / 2) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x = x + 2)
						{
							//top row
							*(pCopy) = (byte)(((pOrig[0] << 2) + 2 * pOrig[3] + 2 * pOrig[sStride] + pOrig[sStride + 3]) / 9);
							*(pCopy + 1) = (byte)(((pOrig[1] << 2) + 2 * pOrig[4] + 2 * pOrig[sStride + 1] + pOrig[sStride + 4]) / 9);
							*(pCopy + 2) = (byte)(((pOrig[2] << 2) + 2 * pOrig[5] + 2 * pOrig[sStride + 2] + pOrig[sStride + 5]) / 9);

							*(pCopy + 3) = (byte)((2 * pOrig[3] + (pOrig[6] << 2) + pOrig[sStride + 3] + 2 * pOrig[sStride + 6]) / 9);
							*(pCopy + 4) = (byte)((2 * pOrig[4] + (pOrig[7] << 2) + pOrig[sStride + 4] + 2 * pOrig[sStride + 7]) / 9);
							*(pCopy + 5) = (byte)((2 * pOrig[5] + (pOrig[8] << 2) + pOrig[sStride + 5] + 2 * pOrig[sStride + 8]) / 9);

							*(pCopy + rStride) = (byte)((2 * pOrig[sStride] + pOrig[sStride + 3] + (pOrig[2 * sStride] << 2) + 2 * pOrig[2 * sStride + 3]) / 9);
							*(pCopy + rStride + 1) = (byte)((2 * pOrig[sStride + 1] + pOrig[sStride + 4] + (pOrig[2 * sStride + 1] << 2) + 2 * pOrig[2 * sStride + 4]) / 9);
							*(pCopy + rStride + 2) = (byte)((2 * pOrig[sStride + 2] + pOrig[sStride + 5] + (pOrig[2 * sStride + 2] << 2) + 2 * pOrig[2 * sStride + 5]) / 9);

							*(pCopy + rStride + 3) = (byte)((pOrig[sStride + 3] + 2 * pOrig[sStride + 6] + 2 * pOrig[2 * sStride + 3] + (pOrig[2 * sStride + 6] << 2)) / 9);
							*(pCopy + rStride + 4) = (byte)((pOrig[sStride + 4] + 2 * pOrig[sStride + 7] + 2 * pOrig[2 * sStride + 4] + (pOrig[2 * sStride + 7] << 2)) / 9);
							*(pCopy + rStride + 5) = (byte)((pOrig[sStride + 5] + 2 * pOrig[sStride + 8] + 2 * pOrig[2 * sStride + 5] + (pOrig[2 * sStride + 8] << 2)) / 9);

							//next area
							pOrig += 9;
							pCopy += 6;
						}
					}
				}
				else if (resultData.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					for (y = 0; y < resultHeight; y = y + 2)
					{
						pOrig = pOrigScan0 + (y * 3 / 2) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x = x + 2)
						{
							//top row
							*(pCopy) = (byte)(((pOrig[0] << 2) + (pOrig[1] << 1) + (pOrig[sStride] << 1) + (pOrig[sStride + 1])) / 9);
							*(pCopy + 1) = (byte)(((pOrig[1] << 1) + (pOrig[2] << 2) + (pOrig[sStride + 1]) + (pOrig[sStride + 2] << 1)) / 9);
							*(pCopy + rStride) = (byte)(((pOrig[sStride] << 1) + (pOrig[sStride + 1]) + (pOrig[2 * sStride] << 2) + (pOrig[2 * sStride + 1] << 1)) / 9);
							*(pCopy + rStride + 1) = (byte)(((pOrig[sStride + 1]) + (pOrig[sStride + 2] << 1) + (pOrig[2 * sStride + 1] << 1) + (pOrig[2 * sStride + 2] << 2)) / 9);

							//next area
							pOrig += 3;
							pCopy += 2;
						}
					}
				}
			}
		}
		#endregion

		#region Interpolate3to4()
		public static void Interpolate3to4(BitmapData sourceData, BitmapData resultData)
		{
			int resultWidth = resultData.Width;
			int resultHeight = resultData.Height;
			int x, y;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			unsafe
			{
				byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
				byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
				byte* pS;
				byte* pR;
				byte* pOrigTmp;
				byte* pCopyTmp;

				if (resultData.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (y = 0; y < resultHeight; y = y + 4)
					{
						pS = pOrigTmp = pOrigScan0 + (y * 3 / 4) * sStride;
						pR = pCopyTmp = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x = x + 4)
						{
							//top row
							*(pR) = (byte)*(pS);
							*(pR + 1) = (byte)*(pS + 1);
							*(pR + 2) = (byte)*(pS + 2);

							*(pR + 3) = (byte)((*(pS) * .3333F) + (*(pS + 3) * .6666F));
							*(pR + 4) = (byte)((*(pS + 1) * .3333F) + (*(pS + 4) * .6666F));
							*(pR + 5) = (byte)((*(pS + 2) * .3333F) + (*(pS + 5) * .6666F));

							*(pR + 6) = (byte)((*(pS + 3) * .6666F) + (*(pS + 6) * .3333F));
							*(pR + 7) = (byte)((*(pS + 4) * .6666F) + (*(pS + 7) * .3333F));
							*(pR + 8) = (byte)((*(pS + 5) * .6666F) + (*(pS + 8) * .3333F));

							*(pR + 9) = (byte)*(pS + 6);
							*(pR + 10) = (byte)*(pS + 7);
							*(pR + 11) = (byte)*(pS + 8);

							//upper center row
							pR = pR + rStride;

							*(pR) = (byte)(*(pS) * .3333F + *(pS + sStride) * .6666F);
							*(pR + 1) = (byte)(*(pS + 1) * .3333F + *(pS + 1 + sStride) * .6666F);
							*(pR + 2) = (byte)(*(pS + 2) * .3333F + *(pS + 2 + sStride) * .6666F);

							*(pR + 3) = (byte)((*(pS) * .1111F) + (*(pS + 3) * .2222F) + (*(pS + sStride) * .2222F) + (*(pS + 3 + sStride) * .4444F));
							*(pR + 4) = (byte)((*(pS + 1) * .1111F) + (*(pS + 4) * .2222F) + (*(pS + 1 + sStride) * .2222F) + (*(pS + 4 + sStride) * .4444F));
							*(pR + 5) = (byte)((*(pS + 2) * .1111F) + (*(pS + 5) * .2222F) + (*(pS + 2 + sStride) * .2222F) + (*(pS + 5 + sStride) * .4444F));

							*(pR + 6) = (byte)((*(pS + 3) * .2222F) + (*(pS + 6) * .1111F) + (*(pS + 3 + sStride) * .4444F) + (*(pS + 6 + sStride) * .2222F));
							*(pR + 7) = (byte)((*(pS + 4) * .2222F) + (*(pS + 7) * .1111F) + (*(pS + 4 + sStride) * .4444F) + (*(pS + 7 + sStride) * .2222F));
							*(pR + 8) = (byte)((*(pS + 5) * .2222F) + (*(pS + 8) * .1111F) + (*(pS + 5 + sStride) * .4444F) + (*(pS + 8 + sStride) * .2222F));

							*(pR + 9) = (byte)(*(pS + 6) * .3333F + *(pS + 6 + sStride) * .6666F);
							*(pR + 10) = (byte)(*(pS + 7) * .3333F + *(pS + 7 + sStride) * .6666F);
							*(pR + 11) = (byte)(*(pS + 8) * .3333F + *(pS + 8 + sStride) * .6666F);

							//lower center row
							pS = pS + sStride;
							pR = pR + rStride;

							*(pR) = (byte)(*(pS) * .6666F + *(pS + sStride) * .3333F);
							*(pR + 1) = (byte)(*(pS + 1) * .6666F + *(pS + 1 + sStride) * .3333F);
							*(pR + 2) = (byte)(*(pS + 2) * .6666F + *(pS + 2 + sStride) * .3333F);

							*(pR + 3) = (byte)((*(pS) * .2222F) + (*(pS + 3) * .4444F) + (*(pS + sStride) * .1111F) + (*(pS + 3 + sStride) * .2222F));
							*(pR + 4) = (byte)((*(pS + 1) * .2222F) + (*(pS + 4) * .4444F) + (*(pS + 1 + sStride) * .1111F) + (*(pS + 4 + sStride) * .2222F));
							*(pR + 5) = (byte)((*(pS + 2) * .2222F) + (*(pS + 5) * .4444F) + (*(pS + 2 + sStride) * .1111F) + (*(pS + 5 + sStride) * .2222F));

							*(pR + 6) = (byte)((*(pS + 3) * .4444F) + (*(pS + 6) * .2222F) + (*(pS + 3 + sStride) * .2222F) + (*(pS + 6 + sStride) * .1111F));
							*(pR + 7) = (byte)((*(pS + 4) * .4444F) + (*(pS + 7) * .2222F) + (*(pS + 4 + sStride) * .2222F) + (*(pS + 7 + sStride) * .1111F));
							*(pR + 8) = (byte)((*(pS + 5) * .4444F) + (*(pS + 8) * .2222F) + (*(pS + 5 + sStride) * .2222F) + (*(pS + 8 + sStride) * .1111F));

							*(pR + 9) = (byte)(*(pS + 6) * .6666F + *(pS + 6 + sStride) * .3333F);
							*(pR + 10) = (byte)(*(pS + 7) * .6666F + *(pS + 7 + sStride) * .3333F);
							*(pR + 11) = (byte)(*(pS + 8) * .6666F + *(pS + 8 + sStride) * .3333F);

							//botton row
							pS = pS + sStride;
							pR = pR + rStride;

							*(pR) = (byte)*(pS);
							*(pR + 1) = (byte)*(pS + 1);
							*(pR + 2) = (byte)*(pS + 2);

							*(pR + 3) = (byte)((*(pS) * .3333F) + (*(pS + 3) * .6666F));
							*(pR + 4) = (byte)((*(pS + 1) * .3333F) + (*(pS + 4) * .6666F));
							*(pR + 5) = (byte)((*(pS + 2) * .3333F) + (*(pS + 5) * .6666F));

							*(pR + 6) = (byte)((*(pS + 3) * .6666F) + (*(pS + 6) * .3333F));
							*(pR + 7) = (byte)((*(pS + 4) * .6666F) + (*(pS + 7) * .3333F));
							*(pR + 8) = (byte)((*(pS + 5) * .6666F) + (*(pS + 8) * .3333F));

							*(pR + 9) = (byte)*(pS + 6);
							*(pR + 10) = (byte)*(pS + 7);
							*(pR + 11) = (byte)*(pS + 8);

							//next area
							pS = pOrigTmp + 9;
							pR = pCopyTmp + 12;
							pOrigTmp = pS;
							pCopyTmp = pR;
						}
					}
				}
				else if (resultData.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					for (y = 0; y < resultHeight; y = y + 4)
					{
						pS = pOrigTmp = pOrigScan0 + (y * 3 / 4) * sStride;
						pR = pCopyTmp = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x = x + 4)
						{
							//top row
							pR[0] = pS[0];
							pR[1] = (byte)(pS[0] * .3333F + pS[1] * .6666F);
							pR[2] = (byte)(pS[1] * .6666F + pS[2] * .3333F);
							pR[3] = pS[2];

							//upper center row
							pR = pR + rStride;

							pR[0] = (byte)(pS[0] * .3333F + pS[sStride] * .6666F);
							pR[1] = (byte)((pS[0] * .1111F) + (pS[1] * .2222F) + (pS[sStride] * .2222F) + (pS[sStride + 1] * .4444F));
							pR[2] = (byte)((pS[1] * .2222F) + (pS[2] * .1111F) + (pS[sStride + 1] * .4444F) + (pS[sStride + 2] * .2222F));
							pR[3] = (byte)(pS[2] * .3333F + pS[sStride + 2] * .6666F);

							//lower center row
							pS = pS + sStride;
							pR = pR + rStride;

							pR[0] = (byte)(pS[0] * .6666F + pS[sStride] * .3333F);
							pR[1] = (byte)((pS[0] * .2222F) + (pS[1] * .4444F) + (pS[sStride] * .1111F) + (pS[sStride + 1] * .2222F));
							pR[2] = (byte)((pS[1] * .4444F) + (pS[2] * .2222F) + (pS[sStride + 1] * .2222F) + (pS[sStride + 2] * .1111F));
							pR[3] = (byte)(pS[2] * .6666F + pS[sStride + 2] * .3333F);

							//botton row
							pS = pS + sStride;
							pR = pR + rStride;

							pR[0] = pS[0];
							pR[1] = (byte)((pS[0] * .3333F) + (pS[1] * .6666F));
							pR[2] = (byte)((pS[1] * .6666F) + (pS[2] * .3333F));
							pR[3] = pS[2];

							//next area
							pS = pOrigTmp + 3;
							pR = pCopyTmp + 4;
							pOrigTmp = pS;
							pCopyTmp = pR;
						}
					}
				}
			}
		}
		#endregion

		#region Interpolate2to3()
		/// <summary>
		/// needs to be improved, inside pixels are interpolated, outside are not and it makes visible edges between outside pixels
		/// </summary>
		public static void Interpolate2to3(BitmapData sourceData, BitmapData resultData)
		{
			int resultWidth = resultData.Width;
			int resultHeight = resultData.Height;
			int x, y;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			unsafe
			{
				byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
				byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
				byte* pOrig;
				byte* pCopy;
				byte* pOrigTmp;
				byte* pCopyTmp;

				#region Format24bppRgb
				if (resultData.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (y = 3; y < resultHeight - 3; y = y + 3)
					{
						pOrig = pOrigTmp = pOrigScan0 + (y * 2 / 3) * sStride + 6;
						pCopy = pCopyTmp = pDestScan0 + y * rStride + 9;

						for (x = 3; x < resultWidth - 3; x = x + 3)
						{
							//top row
							*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-3] + 3 * pOrig[-sStride + 0] + pOrig[-sStride - 3]) >> 4);
							*(pCopy + 1) = (byte)((9 * pOrig[1] + 3 * pOrig[-2] + 3 * pOrig[-sStride + 1] + pOrig[-sStride - 2]) >> 4);
							*(pCopy + 2) = (byte)((9 * pOrig[2] + 3 * pOrig[-1] + 3 * pOrig[-sStride + 2] + pOrig[-sStride - 1]) >> 4);

							*(pCopy + 3) = (byte)((6 * pOrig[0] + 6 * pOrig[3] + 2 * pOrig[-sStride + 0] + 2 * pOrig[-sStride + 3]) >> 4);
							*(pCopy + 4) = (byte)((6 * pOrig[1] + 6 * pOrig[4] + 2 * pOrig[-sStride + 1] + 2 * pOrig[-sStride + 4]) >> 4);
							*(pCopy + 5) = (byte)((6 * pOrig[2] + 6 * pOrig[5] + 2 * pOrig[-sStride + 2] + 2 * pOrig[-sStride + 5]) >> 4);

							*(pCopy + 6) = (byte)((9 * pOrig[3] + 3 * pOrig[+6] + 3 * pOrig[-sStride + 3] + pOrig[-sStride + 6]) >> 4);
							*(pCopy + 7) = (byte)((9 * pOrig[4] + 3 * pOrig[+7] + 3 * pOrig[-sStride + 4] + pOrig[-sStride + 7]) >> 4);
							*(pCopy + 8) = (byte)((9 * pOrig[5] + 3 * pOrig[+8] + 3 * pOrig[-sStride + 5] + pOrig[-sStride + 8]) >> 4);

							//center row
							pCopy = pCopy + rStride;

							*(pCopy + 0) = (byte)((6 * pOrig[0] + 6 * pOrig[sStride + 0] + 2 * pOrig[-3] + 2 * pOrig[sStride - 3]) >> 4);
							*(pCopy + 1) = (byte)((6 * pOrig[1] + 6 * pOrig[sStride + 1] + 2 * pOrig[-2] + 2 * pOrig[sStride - 2]) >> 4);
							*(pCopy + 2) = (byte)((6 * pOrig[2] + 6 * pOrig[sStride + 2] + 2 * pOrig[-1] + 2 * pOrig[sStride - 1]) >> 4);

							*(pCopy + 3) = (byte)((pOrig[0] + pOrig[3] + pOrig[sStride + 0] + pOrig[sStride + 3]) >> 2);
							*(pCopy + 4) = (byte)((pOrig[1] + pOrig[4] + pOrig[sStride + 1] + pOrig[sStride + 4]) >> 2);
							*(pCopy + 5) = (byte)((pOrig[2] + pOrig[5] + pOrig[sStride + 2] + pOrig[sStride + 5]) >> 2);

							*(pCopy + 6) = (byte)((6 * pOrig[3] + 6 * pOrig[sStride + 3] + 2 * pOrig[6] + 2 * pOrig[sStride + 6]) >> 4);
							*(pCopy + 7) = (byte)((6 * pOrig[4] + 6 * pOrig[sStride + 4] + 2 * pOrig[7] + 2 * pOrig[sStride + 7]) >> 4);
							*(pCopy + 8) = (byte)((6 * pOrig[5] + 6 * pOrig[sStride + 5] + 2 * pOrig[8] + 2 * pOrig[sStride + 8]) >> 4);

							//botton row
							pOrig = pOrig + sStride;
							pCopy = pCopy + rStride;

							*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-3] + 3 * pOrig[+sStride + 0] + pOrig[+sStride - 3]) >> 4);
							*(pCopy + 1) = (byte)((9 * pOrig[1] + 3 * pOrig[-2] + 3 * pOrig[+sStride + 1] + pOrig[+sStride - 2]) >> 4);
							*(pCopy + 2) = (byte)((9 * pOrig[2] + 3 * pOrig[-1] + 3 * pOrig[+sStride + 2] + pOrig[+sStride - 1]) >> 4);

							*(pCopy + 3) = (byte)((6 * pOrig[0] + 6 * pOrig[3] + 2 * pOrig[+sStride + 0] + 2 * pOrig[+sStride + 3]) >> 4);
							*(pCopy + 4) = (byte)((6 * pOrig[1] + 6 * pOrig[4] + 2 * pOrig[+sStride + 1] + 2 * pOrig[+sStride + 4]) >> 4);
							*(pCopy + 5) = (byte)((6 * pOrig[2] + 6 * pOrig[5] + 2 * pOrig[+sStride + 2] + 2 * pOrig[+sStride + 5]) >> 4);

							*(pCopy + 6) = (byte)((9 * pOrig[3] + 3 * pOrig[+6] + 3 * pOrig[+sStride + 3] + pOrig[+sStride + 6]) >> 4);
							*(pCopy + 7) = (byte)((9 * pOrig[4] + 3 * pOrig[+7] + 3 * pOrig[+sStride + 4] + pOrig[+sStride + 7]) >> 4);
							*(pCopy + 8) = (byte)((9 * pOrig[5] + 3 * pOrig[+8] + 3 * pOrig[+sStride + 5] + pOrig[+sStride + 8]) >> 4);

							//next area
							pOrig = pOrigTmp + 6;
							pCopy = pCopyTmp + 9;
							pOrigTmp = pOrig;
							pCopyTmp = pCopy;
						}
					}

					//top row
					y = 0;
					{
						pOrig = pOrigTmp = pOrigScan0 + (y * 2 / 3) * sStride + 0;
						pCopy = pCopyTmp = pDestScan0 + y * rStride + 0;

						for (x = 0; x < resultWidth; x = x + 3)
						{
							//top row
							if (x > 0)
							{
								*(pCopy + 0) = (byte)((12 * pOrig[0] + 4 * pOrig[-3]) >> 4);
								*(pCopy + 1) = (byte)((12 * pOrig[1] + 4 * pOrig[-2]) >> 4);
								*(pCopy + 2) = (byte)((12 * pOrig[2] + 4 * pOrig[-1]) >> 4);
							}
							else
							{
								*(pCopy + 0) = pOrig[0];
								*(pCopy + 1) = pOrig[1];
								*(pCopy + 2) = pOrig[2];
							}

							*(pCopy + 3) = (byte)((8 * pOrig[0] + 8 * pOrig[3]) >> 4);
							*(pCopy + 4) = (byte)((8 * pOrig[1] + 8 * pOrig[4]) >> 4);
							*(pCopy + 5) = (byte)((8 * pOrig[2] + 8 * pOrig[5]) >> 4);

							if (x < resultWidth - 3)
							{
								*(pCopy + 6) = (byte)((12 * pOrig[3] + 4 * pOrig[+6]) >> 4);
								*(pCopy + 7) = (byte)((12 * pOrig[4] + 4 * pOrig[+7]) >> 4);
								*(pCopy + 8) = (byte)((12 * pOrig[5] + 4 * pOrig[+8]) >> 4);
							}
							else
							{
								*(pCopy + 6) = pOrig[3];
								*(pCopy + 7) = pOrig[4];
								*(pCopy + 8) = pOrig[5];
							}

							//center row
							pCopy = pCopy + rStride;

							if (x > 0)
							{
								*(pCopy + 0) = (byte)((6 * pOrig[0] + 6 * pOrig[sStride + 0] + 2 * pOrig[-3] + 2 * pOrig[sStride - 3]) >> 4);
								*(pCopy + 1) = (byte)((6 * pOrig[1] + 6 * pOrig[sStride + 1] + 2 * pOrig[-2] + 2 * pOrig[sStride - 2]) >> 4);
								*(pCopy + 2) = (byte)((6 * pOrig[2] + 6 * pOrig[sStride + 2] + 2 * pOrig[-1] + 2 * pOrig[sStride - 1]) >> 4);
							}
							else
							{
								*(pCopy + 0) = (byte)((8 * pOrig[0] + 8 * pOrig[sStride + 0]) >> 4);
								*(pCopy + 1) = (byte)((8 * pOrig[1] + 8 * pOrig[sStride + 1]) >> 4);
								*(pCopy + 2) = (byte)((8 * pOrig[2] + 8 * pOrig[sStride + 2]) >> 4);
							}

							*(pCopy + 3) = (byte)((pOrig[0] + pOrig[3] + pOrig[sStride + 0] + pOrig[sStride + 3]) >> 2);
							*(pCopy + 4) = (byte)((pOrig[1] + pOrig[4] + pOrig[sStride + 1] + pOrig[sStride + 4]) >> 2);
							*(pCopy + 5) = (byte)((pOrig[2] + pOrig[5] + pOrig[sStride + 2] + pOrig[sStride + 5]) >> 2);

							if (x < resultWidth - 3)
							{
								*(pCopy + 6) = (byte)((6 * pOrig[3] + 6 * pOrig[sStride + 3] + 2 * pOrig[6] + 2 * pOrig[sStride + 6]) >> 4);
								*(pCopy + 7) = (byte)((6 * pOrig[4] + 6 * pOrig[sStride + 4] + 2 * pOrig[7] + 2 * pOrig[sStride + 7]) >> 4);
								*(pCopy + 8) = (byte)((6 * pOrig[5] + 6 * pOrig[sStride + 5] + 2 * pOrig[8] + 2 * pOrig[sStride + 8]) >> 4);
							}
							else
							{
								*(pCopy + 6) = (byte)((8 * pOrig[3] + 8 * pOrig[sStride + 3]) >> 4);
								*(pCopy + 7) = (byte)((8 * pOrig[4] + 8 * pOrig[sStride + 4]) >> 4);
								*(pCopy + 8) = (byte)((8 * pOrig[5] + 8 * pOrig[sStride + 5]) >> 4);
							}

							//botton row
							pOrig = pOrig + sStride;
							pCopy = pCopy + rStride;

							if (x > 0)
							{
								*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-3] + 3 * pOrig[+sStride + 0] + pOrig[+sStride - 3]) >> 4);
								*(pCopy + 1) = (byte)((9 * pOrig[1] + 3 * pOrig[-2] + 3 * pOrig[+sStride + 1] + pOrig[+sStride - 2]) >> 4);
								*(pCopy + 2) = (byte)((9 * pOrig[2] + 3 * pOrig[-1] + 3 * pOrig[+sStride + 2] + pOrig[+sStride - 1]) >> 4);
							}
							else
							{
								*(pCopy + 0) = (byte)((12 * pOrig[0] + 4 * pOrig[+sStride + 0]) >> 4);
								*(pCopy + 1) = (byte)((12 * pOrig[1] + 4 * pOrig[+sStride + 1]) >> 4);
								*(pCopy + 2) = (byte)((12 * pOrig[2] + 4 * pOrig[+sStride + 2]) >> 4);
							}

							*(pCopy + 3) = (byte)((6 * pOrig[0] + 6 * pOrig[3] + 2 * pOrig[+sStride + 0] + 2 * pOrig[+sStride + 3]) >> 4);
							*(pCopy + 4) = (byte)((6 * pOrig[1] + 6 * pOrig[4] + 2 * pOrig[+sStride + 1] + 2 * pOrig[+sStride + 4]) >> 4);
							*(pCopy + 5) = (byte)((6 * pOrig[2] + 6 * pOrig[5] + 2 * pOrig[+sStride + 2] + 2 * pOrig[+sStride + 5]) >> 4);

							if (x < resultWidth - 3)
							{
								*(pCopy + 6) = (byte)((9 * pOrig[3] + 3 * pOrig[+6] + 3 * pOrig[+sStride + 3] + pOrig[+sStride + 6]) >> 4);
								*(pCopy + 7) = (byte)((9 * pOrig[4] + 3 * pOrig[+7] + 3 * pOrig[+sStride + 4] + pOrig[+sStride + 7]) >> 4);
								*(pCopy + 8) = (byte)((9 * pOrig[5] + 3 * pOrig[+8] + 3 * pOrig[+sStride + 5] + pOrig[+sStride + 8]) >> 4);
							}
							else
							{
								*(pCopy + 6) = (byte)((12 * pOrig[3] + 4 * pOrig[+sStride + 3]) >> 4);
								*(pCopy + 7) = (byte)((12 * pOrig[4] + 4 * pOrig[+sStride + 4]) >> 4);
								*(pCopy + 8) = (byte)((12 * pOrig[5] + 4 * pOrig[+sStride + 5]) >> 4);
							}

							//next area
							pOrig = pOrigTmp + 6;
							pCopy = pCopyTmp + 9;
							pOrigTmp = pOrig;
							pCopyTmp = pCopy;
						}
					}
					//bottom row
					y = resultHeight - 3;
					{
						pOrig = pOrigTmp = pOrigScan0 + (y * 2 / 3) * sStride + 0;
						pCopy = pCopyTmp = pDestScan0 + y * rStride + 0;

						for (x = 0; x < resultWidth; x = x + 3)
						{
							//top row
							if (x > 0)
							{
								*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-3] + 3 * pOrig[-sStride + 0] + pOrig[-sStride - 3]) >> 4);
								*(pCopy + 1) = (byte)((9 * pOrig[1] + 3 * pOrig[-2] + 3 * pOrig[-sStride + 1] + pOrig[-sStride - 2]) >> 4);
								*(pCopy + 2) = (byte)((9 * pOrig[2] + 3 * pOrig[-1] + 3 * pOrig[-sStride + 2] + pOrig[-sStride - 1]) >> 4);
							}
							else
							{
								*(pCopy + 0) = (byte)((12 * pOrig[0] + 4 * pOrig[-sStride + 0]) >> 4);
								*(pCopy + 1) = (byte)((12 * pOrig[1] + 4 * pOrig[-sStride + 1]) >> 4);
								*(pCopy + 2) = (byte)((12 * pOrig[2] + 4 * pOrig[-sStride + 2]) >> 4);
							}

							*(pCopy + 3) = (byte)((6 * pOrig[0] + 6 * pOrig[3] + 2 * pOrig[-sStride + 0] + 2 * pOrig[-sStride + 3]) >> 4);
							*(pCopy + 4) = (byte)((6 * pOrig[1] + 6 * pOrig[4] + 2 * pOrig[-sStride + 1] + 2 * pOrig[-sStride + 4]) >> 4);
							*(pCopy + 5) = (byte)((6 * pOrig[2] + 6 * pOrig[5] + 2 * pOrig[-sStride + 2] + 2 * pOrig[-sStride + 5]) >> 4);

							if (x < resultWidth - 3)
							{
								*(pCopy + 6) = (byte)((9 * pOrig[3] + 3 * pOrig[+6] + 3 * pOrig[-sStride + 3] + pOrig[-sStride + 6]) >> 4);
								*(pCopy + 7) = (byte)((9 * pOrig[4] + 3 * pOrig[+7] + 3 * pOrig[-sStride + 4] + pOrig[-sStride + 7]) >> 4);
								*(pCopy + 8) = (byte)((9 * pOrig[5] + 3 * pOrig[+8] + 3 * pOrig[-sStride + 5] + pOrig[-sStride + 8]) >> 4);
							}
							else
							{
								*(pCopy + 6) = (byte)((12 * pOrig[3] + 4 * pOrig[-sStride + 3]) >> 4);
								*(pCopy + 7) = (byte)((12 * pOrig[4] + 4 * pOrig[-sStride + 4]) >> 4);
								*(pCopy + 8) = (byte)((12 * pOrig[5] + 4 * pOrig[-sStride + 5]) >> 4);
							}

							//center row
							pCopy = pCopy + rStride;

							if (x > 0)
							{
								*(pCopy + 0) = (byte)((6 * pOrig[0] + 6 * pOrig[sStride + 0] + 2 * pOrig[-3] + 2 * pOrig[sStride - 3]) >> 4);
								*(pCopy + 1) = (byte)((6 * pOrig[1] + 6 * pOrig[sStride + 1] + 2 * pOrig[-2] + 2 * pOrig[sStride - 2]) >> 4);
								*(pCopy + 2) = (byte)((6 * pOrig[2] + 6 * pOrig[sStride + 2] + 2 * pOrig[-1] + 2 * pOrig[sStride - 1]) >> 4);
							}
							else
							{
								*(pCopy + 0) = (byte)((8 * pOrig[0] + 8 * pOrig[sStride + 0]) >> 4);
								*(pCopy + 1) = (byte)((8 * pOrig[1] + 8 * pOrig[sStride + 1]) >> 4);
								*(pCopy + 2) = (byte)((8 * pOrig[2] + 8 * pOrig[sStride + 2]) >> 4);
							}

							*(pCopy + 3) = (byte)((pOrig[0] + pOrig[3] + pOrig[sStride + 0] + pOrig[sStride + 3]) >> 2);
							*(pCopy + 4) = (byte)((pOrig[1] + pOrig[4] + pOrig[sStride + 1] + pOrig[sStride + 4]) >> 2);
							*(pCopy + 5) = (byte)((pOrig[2] + pOrig[5] + pOrig[sStride + 2] + pOrig[sStride + 5]) >> 2);

							if (x < resultWidth - 3)
							{
								*(pCopy + 6) = (byte)((6 * pOrig[3] + 6 * pOrig[sStride + 3] + 2 * pOrig[6] + 2 * pOrig[sStride + 6]) >> 4);
								*(pCopy + 7) = (byte)((6 * pOrig[4] + 6 * pOrig[sStride + 4] + 2 * pOrig[7] + 2 * pOrig[sStride + 7]) >> 4);
								*(pCopy + 8) = (byte)((6 * pOrig[5] + 6 * pOrig[sStride + 5] + 2 * pOrig[8] + 2 * pOrig[sStride + 8]) >> 4);
							}
							else
							{
								*(pCopy + 6) = (byte)((8 * pOrig[3] + 8 * pOrig[sStride + 3]) >> 4);
								*(pCopy + 7) = (byte)((8 * pOrig[4] + 8 * pOrig[sStride + 4]) >> 4);
								*(pCopy + 8) = (byte)((8 * pOrig[5] + 8 * pOrig[sStride + 5]) >> 4);
							}

							//botton row
							pOrig = pOrig + sStride;
							pCopy = pCopy + rStride;

							if (x > 0)
							{
								*(pCopy + 0) = (byte)((12 * pOrig[0] + 4 * pOrig[-3]) >> 4);
								*(pCopy + 1) = (byte)((12 * pOrig[1] + 4 * pOrig[-2]) >> 4);
								*(pCopy + 2) = (byte)((12 * pOrig[2] + 4 * pOrig[-1]) >> 4);
							}
							else
							{
								*(pCopy + 0) = pOrig[0];
								*(pCopy + 1) = pOrig[1];
								*(pCopy + 2) = pOrig[2];
							}

							*(pCopy + 3) = (byte)((8 * pOrig[0] + 8 * pOrig[3]) >> 4);
							*(pCopy + 4) = (byte)((8 * pOrig[1] + 8 * pOrig[4]) >> 4);
							*(pCopy + 5) = (byte)((8 * pOrig[2] + 8 * pOrig[5]) >> 4);

							if (x < resultWidth - 3)
							{
								*(pCopy + 6) = (byte)((12 * pOrig[3] + 4 * pOrig[+6]) >> 4);
								*(pCopy + 7) = (byte)((12 * pOrig[4] + 4 * pOrig[+7]) >> 4);
								*(pCopy + 8) = (byte)((12 * pOrig[5] + 4 * pOrig[+8]) >> 4);
							}
							else
							{
								*(pCopy + 6) = pOrig[3];
								*(pCopy + 7) = pOrig[4];
								*(pCopy + 8) = pOrig[5];
							}

							//next area
							pOrig = pOrigTmp + 6;
							pCopy = pCopyTmp + 9;
							pOrigTmp = pOrig;
							pCopyTmp = pCopy;
						}
					}
					//left column
					for (y = 3; y < resultHeight - 3; y = y + 3)
					{
						pOrig = pOrigScan0 + (y * 2 / 3) * sStride + 0;
						pCopy = pDestScan0 + y * rStride + 0;

						x = 0;
						{
							//top row
							*(pCopy + 0) = (byte)((12 * pOrig[0] + 4 * pOrig[-sStride + 0]) >> 4);
							*(pCopy + 1) = (byte)((12 * pOrig[1] + 4 * pOrig[-sStride + 1]) >> 4);
							*(pCopy + 2) = (byte)((12 * pOrig[2] + 4 * pOrig[-sStride + 2]) >> 4);

							*(pCopy + 3) = (byte)((6 * pOrig[0] + 6 * pOrig[3] + 2 * pOrig[-sStride + 0] + 2 * pOrig[-sStride + 3]) >> 4);
							*(pCopy + 4) = (byte)((6 * pOrig[1] + 6 * pOrig[4] + 2 * pOrig[-sStride + 1] + 2 * pOrig[-sStride + 4]) >> 4);
							*(pCopy + 5) = (byte)((6 * pOrig[2] + 6 * pOrig[5] + 2 * pOrig[-sStride + 2] + 2 * pOrig[-sStride + 5]) >> 4);

							*(pCopy + 6) = (byte)((9 * pOrig[3] + 3 * pOrig[+6] + 3 * pOrig[-sStride + 3] + pOrig[-sStride + 6]) >> 4);
							*(pCopy + 7) = (byte)((9 * pOrig[4] + 3 * pOrig[+7] + 3 * pOrig[-sStride + 4] + pOrig[-sStride + 7]) >> 4);
							*(pCopy + 8) = (byte)((9 * pOrig[5] + 3 * pOrig[+8] + 3 * pOrig[-sStride + 5] + pOrig[-sStride + 8]) >> 4);

							//center row
							pCopy = pCopy + rStride;

							*(pCopy + 0) = (byte)((8 * pOrig[0] + 8 * pOrig[sStride + 0]) >> 4);
							*(pCopy + 1) = (byte)((8 * pOrig[1] + 8 * pOrig[sStride + 1]) >> 4);
							*(pCopy + 2) = (byte)((8 * pOrig[2] + 8 * pOrig[sStride + 2]) >> 4);

							*(pCopy + 3) = (byte)((pOrig[0] + pOrig[3] + pOrig[sStride + 0] + pOrig[sStride + 3]) >> 2);
							*(pCopy + 4) = (byte)((pOrig[1] + pOrig[4] + pOrig[sStride + 1] + pOrig[sStride + 4]) >> 2);
							*(pCopy + 5) = (byte)((pOrig[2] + pOrig[5] + pOrig[sStride + 2] + pOrig[sStride + 5]) >> 2);

							*(pCopy + 6) = (byte)((6 * pOrig[3] + 6 * pOrig[sStride + 3] + 2 * pOrig[6] + 2 * pOrig[sStride + 6]) >> 4);
							*(pCopy + 7) = (byte)((6 * pOrig[4] + 6 * pOrig[sStride + 4] + 2 * pOrig[7] + 2 * pOrig[sStride + 7]) >> 4);
							*(pCopy + 8) = (byte)((6 * pOrig[5] + 6 * pOrig[sStride + 5] + 2 * pOrig[8] + 2 * pOrig[sStride + 8]) >> 4);

							//botton row
							pOrig = pOrig + sStride;
							pCopy = pCopy + rStride;

							*(pCopy + 0) = (byte)((12 * pOrig[0] + 4 * pOrig[+sStride + 0]) >> 4);
							*(pCopy + 1) = (byte)((12 * pOrig[1] + 4 * pOrig[+sStride + 1]) >> 4);
							*(pCopy + 2) = (byte)((12 * pOrig[2] + 4 * pOrig[+sStride + 2]) >> 4);

							*(pCopy + 3) = (byte)((6 * pOrig[0] + 6 * pOrig[3] + 2 * pOrig[+sStride + 0] + 2 * pOrig[+sStride + 3]) >> 4);
							*(pCopy + 4) = (byte)((6 * pOrig[1] + 6 * pOrig[4] + 2 * pOrig[+sStride + 1] + 2 * pOrig[+sStride + 4]) >> 4);
							*(pCopy + 5) = (byte)((6 * pOrig[2] + 6 * pOrig[5] + 2 * pOrig[+sStride + 2] + 2 * pOrig[+sStride + 5]) >> 4);

							*(pCopy + 6) = (byte)((9 * pOrig[3] + 3 * pOrig[+6] + 3 * pOrig[+sStride + 3] + pOrig[+sStride + 6]) >> 4);
							*(pCopy + 7) = (byte)((9 * pOrig[4] + 3 * pOrig[+7] + 3 * pOrig[+sStride + 4] + pOrig[+sStride + 7]) >> 4);
							*(pCopy + 8) = (byte)((9 * pOrig[5] + 3 * pOrig[+8] + 3 * pOrig[+sStride + 5] + pOrig[+sStride + 8]) >> 4);
						}
					}
					//right column
					for (y = 3; y < resultHeight - 3; y = y + 3)
					{
						x = resultWidth - 3;
						pOrig = pOrigScan0 + (y * 2 / 3) * sStride + (x / 3 * 2) * 3;
						pCopy = pDestScan0 + y * rStride + x * 3;

						{
							//top row
							*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-3] + 3 * pOrig[-sStride + 0] + pOrig[-sStride - 3]) >> 4);
							*(pCopy + 1) = (byte)((9 * pOrig[1] + 3 * pOrig[-2] + 3 * pOrig[-sStride + 1] + pOrig[-sStride - 2]) >> 4);
							*(pCopy + 2) = (byte)((9 * pOrig[2] + 3 * pOrig[-1] + 3 * pOrig[-sStride + 2] + pOrig[-sStride - 1]) >> 4);

							*(pCopy + 3) = (byte)((6 * pOrig[0] + 6 * pOrig[3] + 2 * pOrig[-sStride + 0] + 2 * pOrig[-sStride + 3]) >> 4);
							*(pCopy + 4) = (byte)((6 * pOrig[1] + 6 * pOrig[4] + 2 * pOrig[-sStride + 1] + 2 * pOrig[-sStride + 4]) >> 4);
							*(pCopy + 5) = (byte)((6 * pOrig[2] + 6 * pOrig[5] + 2 * pOrig[-sStride + 2] + 2 * pOrig[-sStride + 5]) >> 4);

							*(pCopy + 6) = (byte)((12 * pOrig[3] + 4 * pOrig[-sStride + 3]) >> 4);
							*(pCopy + 7) = (byte)((12 * pOrig[4] + 4 * pOrig[-sStride + 4]) >> 4);
							*(pCopy + 8) = (byte)((12 * pOrig[5] + 4 * pOrig[-sStride + 5]) >> 4);

							//center row
							pCopy = pCopy + rStride;

							*(pCopy + 0) = (byte)((6 * pOrig[0] + 6 * pOrig[sStride + 0] + 2 * pOrig[-3] + 2 * pOrig[sStride - 3]) >> 4);
							*(pCopy + 1) = (byte)((6 * pOrig[1] + 6 * pOrig[sStride + 1] + 2 * pOrig[-2] + 2 * pOrig[sStride - 2]) >> 4);
							*(pCopy + 2) = (byte)((6 * pOrig[2] + 6 * pOrig[sStride + 2] + 2 * pOrig[-1] + 2 * pOrig[sStride - 1]) >> 4);

							*(pCopy + 3) = (byte)((pOrig[0] + pOrig[3] + pOrig[sStride + 0] + pOrig[sStride + 3]) >> 2);
							*(pCopy + 4) = (byte)((pOrig[1] + pOrig[4] + pOrig[sStride + 1] + pOrig[sStride + 4]) >> 2);
							*(pCopy + 5) = (byte)((pOrig[2] + pOrig[5] + pOrig[sStride + 2] + pOrig[sStride + 5]) >> 2);

							*(pCopy + 6) = (byte)((8 * pOrig[3] + 8 * pOrig[sStride + 3]) >> 4);
							*(pCopy + 7) = (byte)((8 * pOrig[4] + 8 * pOrig[sStride + 4]) >> 4);
							*(pCopy + 8) = (byte)((8 * pOrig[5] + 8 * pOrig[sStride + 5]) >> 4);

							//botton row
							pOrig = pOrig + sStride;
							pCopy = pCopy + rStride;

							*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-3] + 3 * pOrig[+sStride + 0] + pOrig[+sStride - 3]) >> 4);
							*(pCopy + 1) = (byte)((9 * pOrig[1] + 3 * pOrig[-2] + 3 * pOrig[+sStride + 1] + pOrig[+sStride - 2]) >> 4);
							*(pCopy + 2) = (byte)((9 * pOrig[2] + 3 * pOrig[-1] + 3 * pOrig[+sStride + 2] + pOrig[+sStride - 1]) >> 4);

							*(pCopy + 3) = (byte)((6 * pOrig[0] + 6 * pOrig[3] + 2 * pOrig[+sStride + 0] + 2 * pOrig[+sStride + 3]) >> 4);
							*(pCopy + 4) = (byte)((6 * pOrig[1] + 6 * pOrig[4] + 2 * pOrig[+sStride + 1] + 2 * pOrig[+sStride + 4]) >> 4);
							*(pCopy + 5) = (byte)((6 * pOrig[2] + 6 * pOrig[5] + 2 * pOrig[+sStride + 2] + 2 * pOrig[+sStride + 5]) >> 4);

							*(pCopy + 6) = (byte)((12 * pOrig[3] + 4 * pOrig[+sStride + 3]) >> 4);
							*(pCopy + 7) = (byte)((12 * pOrig[4] + 4 * pOrig[+sStride + 4]) >> 4);
							*(pCopy + 8) = (byte)((12 * pOrig[5] + 4 * pOrig[+sStride + 5]) >> 4);
						}
					}
				}
				#endregion

				#region Format8bppIndexed
				else if (resultData.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					for (y = 3; y < resultHeight - 3; y = y + 3)
					{
						pOrig = pOrigTmp = pOrigScan0 + (y * 2 / 3) * sStride + 2;
						pCopy = pCopyTmp = pDestScan0 + y * rStride + 3;

						for (x = 3; x < resultWidth - 3; x = x + 3)
						{
							//top row
							*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-1] + 3 * pOrig[-sStride + 0] + pOrig[-sStride - 1]) >> 4);
							*(pCopy + 1) = (byte)((6 * pOrig[0] + 6 * pOrig[1] + 2 * pOrig[-sStride + 0] + 2 * pOrig[-sStride + 1]) >> 4);
							*(pCopy + 2) = (byte)((9 * pOrig[1] + 3 * pOrig[+2] + 3 * pOrig[-sStride + 1] + pOrig[-sStride + 2]) >> 4);

							//center row
							pCopy = pCopy + rStride;

							*(pCopy + 0) = (byte)((6 * pOrig[0] + 6 * pOrig[sStride + 0] + 2 * pOrig[-1] + 2 * pOrig[sStride - 1]) >> 4);
							*(pCopy + 1) = (byte)((pOrig[0] + pOrig[1] + pOrig[sStride + 0] + pOrig[sStride + 1]) >> 2);
							*(pCopy + 2) = (byte)((6 * pOrig[1] + 6 * pOrig[sStride + 1] + 2 * pOrig[2] + 2 * pOrig[sStride + 2]) >> 4);

							//botton row
							pOrig = pOrig + sStride;
							pCopy = pCopy + rStride;

							*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-1] + 3 * pOrig[+sStride + 0] + pOrig[+sStride - 1]) >> 4);
							*(pCopy + 1) = (byte)((6 * pOrig[0] + 6 * pOrig[1] + 2 * pOrig[+sStride + 0] + 2 * pOrig[+sStride + 1]) >> 4);
							*(pCopy + 2) = (byte)((9 * pOrig[1] + 3 * pOrig[+2] + 3 * pOrig[+sStride + 1] + pOrig[+sStride + 2]) >> 4);

							//next area
							pOrig = pOrigTmp + 2;
							pCopy = pCopyTmp + 3;
							pOrigTmp = pOrig;
							pCopyTmp = pCopy;
						}
					}

					//top row
					y = 0;
					{
						pOrig = pOrigTmp = pOrigScan0 + (y * 2 / 3) * sStride + 0;
						pCopy = pCopyTmp = pDestScan0 + y * rStride + 0;

						for (x = 0; x < resultWidth; x = x + 3)
						{
							//top row
							*(pCopy + 0) = (byte)((x > 0) ? ((12 * pOrig[0] + 4 * pOrig[-1]) >> 4) : pOrig[0]);
							*(pCopy + 1) = (byte)((8 * pOrig[0] + 8 * pOrig[1]) >> 4);
							*(pCopy + 2) = (byte)((x < resultWidth - 3) ? ((12 * pOrig[1] + 4 * pOrig[2]) >> 4) : pOrig[1]);

							//center row
							pCopy = pCopy + rStride;

							if (x > 0)
								*(pCopy + 0) = (byte)((6 * pOrig[0] + 6 * pOrig[sStride + 0] + 2 * pOrig[-1] + 2 * pOrig[sStride - 1]) >> 4);
							else
								*(pCopy + 0) = (byte)((8 * pOrig[0] + 8 * pOrig[sStride + 0]) >> 4);

							*(pCopy + 1) = (byte)((pOrig[0] + pOrig[1] + pOrig[sStride + 0] + pOrig[sStride + 1]) >> 2);

							if (x < resultWidth - 3)
								*(pCopy + 2) = (byte)((6 * pOrig[1] + 6 * pOrig[sStride + 1] + 2 * pOrig[2] + 2 * pOrig[sStride + 2]) >> 4);
							else
								*(pCopy + 2) = (byte)((8 * pOrig[1] + 8 * pOrig[sStride + 1]) >> 4);

							//botton row
							pOrig = pOrig + sStride;
							pCopy = pCopy + rStride;

							if (x > 0)
								*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-1] + 3 * pOrig[+sStride + 0] + pOrig[+sStride - 1]) >> 4);
							else
								*(pCopy + 0) = (byte)((12 * pOrig[0] + 4 * pOrig[+sStride + 0]) >> 4);

							*(pCopy + 1) = (byte)((6 * pOrig[0] + 6 * pOrig[1] + 2 * pOrig[+sStride + 0] + 2 * pOrig[+sStride + 1]) >> 4);

							if (x < resultWidth - 3)
								*(pCopy + 2) = (byte)((9 * pOrig[1] + 3 * pOrig[+2] + 3 * pOrig[+sStride + 1] + pOrig[+sStride + 2]) >> 4);
							else
								*(pCopy + 2) = (byte)((12 * pOrig[1] + 4 * pOrig[+sStride + 1]) >> 4);

							//next area
							pOrig = pOrigTmp + 2;
							pCopy = pCopyTmp + 3;
							pOrigTmp = pOrig;
							pCopyTmp = pCopy;
						}
					}
					//bottom row
					y = resultHeight - 3;
					{
						pOrig = pOrigTmp = pOrigScan0 + (y * 2 / 3) * sStride + 0;
						pCopy = pCopyTmp = pDestScan0 + y * rStride + 0;

						for (x = 0; x < resultWidth; x = x + 3)
						{
							//top row
							if (x > 0)
								*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-1] + 3 * pOrig[-sStride + 0] + pOrig[-sStride - 1]) >> 4);
							else
								*(pCopy + 0) = (byte)((12 * pOrig[0] + 4 * pOrig[-sStride + 0]) >> 4);

							*(pCopy + 1) = (byte)((6 * pOrig[0] + 6 * pOrig[1] + 2 * pOrig[-sStride + 0] + 2 * pOrig[-sStride + 1]) >> 4);

							if (x < resultWidth - 3)
								*(pCopy + 2) = (byte)((9 * pOrig[1] + 3 * pOrig[2] + 3 * pOrig[-sStride + 1] + pOrig[-sStride + 2]) >> 4);
							else
								*(pCopy + 2) = (byte)((12 * pOrig[1] + 4 * pOrig[-sStride + 1]) >> 4);

							//center row
							pCopy = pCopy + rStride;

							if (x > 0)
								*(pCopy + 0) = (byte)((6 * pOrig[0] + 6 * pOrig[sStride + 0] + 2 * pOrig[-1] + 2 * pOrig[sStride - 1]) >> 4);
							else
								*(pCopy + 0) = (byte)((8 * pOrig[0] + 8 * pOrig[sStride + 0]) >> 4);

							*(pCopy + 1) = (byte)((pOrig[0] + pOrig[1] + pOrig[sStride + 0] + pOrig[sStride + 1]) >> 2);

							if (x < resultWidth - 3)
								*(pCopy + 2) = (byte)((6 * pOrig[1] + 6 * pOrig[sStride + 1] + 2 * pOrig[2] + 2 * pOrig[sStride + 2]) >> 4);
							else
								*(pCopy + 2) = (byte)((8 * pOrig[1] + 8 * pOrig[sStride + 1]) >> 4);

							//botton row
							pOrig = pOrig + sStride;
							pCopy = pCopy + rStride;

							if (x > 0)
								*(pCopy + 0) = (byte)((12 * pOrig[0] + 4 * pOrig[-1]) >> 4);
							else
								*(pCopy + 0) = pOrig[0];

							*(pCopy + 1) = (byte)((8 * pOrig[0] + 8 * pOrig[1]) >> 4);

							if (x < resultWidth - 3)
								*(pCopy + 2) = (byte)((12 * pOrig[1] + 4 * pOrig[+2]) >> 4);
							else
								*(pCopy + 2) = pOrig[1];

							//next area
							pOrig = pOrigTmp + 2;
							pCopy = pCopyTmp + 3;
							pOrigTmp = pOrig;
							pCopyTmp = pCopy;
						}
					}
					//left column
					for (y = 3; y < resultHeight - 3; y = y + 3)
					{
						pOrig = pOrigScan0 + (y * 2 / 3) * sStride + 0;
						pCopy = pDestScan0 + y * rStride + 0;

						x = 0;
						{
							//top row
							*(pCopy + 0) = (byte)((12 * pOrig[0] + 4 * pOrig[-sStride + 0]) >> 4);
							*(pCopy + 1) = (byte)((6 * pOrig[0] + 6 * pOrig[1] + 2 * pOrig[-sStride + 0] + 2 * pOrig[-sStride + 1]) >> 4);
							*(pCopy + 2) = (byte)((9 * pOrig[1] + 3 * pOrig[2] + 3 * pOrig[-sStride + 1] + pOrig[-sStride + 2]) >> 4);

							//center row
							pCopy = pCopy + rStride;

							*(pCopy + 0) = (byte)((8 * pOrig[0] + 8 * pOrig[sStride + 0]) >> 4);
							*(pCopy + 1) = (byte)((pOrig[0] + pOrig[1] + pOrig[sStride + 0] + pOrig[sStride + 1]) >> 2);
							*(pCopy + 2) = (byte)((6 * pOrig[1] + 6 * pOrig[sStride + 1] + 2 * pOrig[2] + 2 * pOrig[sStride + 2]) >> 4);

							//botton row
							pOrig = pOrig + sStride;
							pCopy = pCopy + rStride;

							*(pCopy + 0) = (byte)((12 * pOrig[0] + 4 * pOrig[+sStride + 0]) >> 4);
							*(pCopy + 1) = (byte)((6 * pOrig[0] + 6 * pOrig[1] + 2 * pOrig[+sStride + 0] + 2 * pOrig[+sStride + 1]) >> 4);
							*(pCopy + 2) = (byte)((9 * pOrig[1] + 3 * pOrig[2] + 3 * pOrig[+sStride + 1] + pOrig[+sStride + 2]) >> 4);
						}
					}
					//right column
					for (y = 3; y < resultHeight - 3; y = y + 3)
					{
						x = resultWidth - 3;
						pOrig = pOrigScan0 + (y * 2 / 3) * sStride + (x / 3 * 2);
						pCopy = pDestScan0 + y * rStride + x;

						{
							//top row
							*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-1] + 3 * pOrig[-sStride + 0] + pOrig[-sStride - 1]) >> 4);
							*(pCopy + 1) = (byte)((6 * pOrig[0] + 6 * pOrig[1] + 2 * pOrig[-sStride + 0] + 2 * pOrig[-sStride + 1]) >> 4);
							*(pCopy + 2) = (byte)((12 * pOrig[1] + 4 * pOrig[-sStride + 1]) >> 4);

							//center row
							pCopy = pCopy + rStride;

							*(pCopy + 0) = (byte)((6 * pOrig[0] + 6 * pOrig[sStride + 0] + 2 * pOrig[-1] + 2 * pOrig[sStride - 1]) >> 4);
							*(pCopy + 1) = (byte)((pOrig[0] + pOrig[1] + pOrig[sStride + 0] + pOrig[sStride + 1]) >> 2);
							*(pCopy + 2) = (byte)((8 * pOrig[1] + 8 * pOrig[sStride + 1]) >> 4);

							//botton row
							pOrig = pOrig + sStride;
							pCopy = pCopy + rStride;

							*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-1] + 3 * pOrig[+sStride + 0] + pOrig[+sStride - 1]) >> 4);
							*(pCopy + 1) = (byte)((6 * pOrig[0] + 6 * pOrig[1] + 2 * pOrig[+sStride + 0] + 2 * pOrig[+sStride + 1]) >> 4);
							*(pCopy + 2) = (byte)((12 * pOrig[1] + 4 * pOrig[+sStride + 1]) >> 4);
						}
					}

					//simple fast method
					/*for (y = 0; y < resultHeight; y = y + 3)
					{
						pOrig = pOrigTmp = pOrigScan0 + (y * 2 / 3) * sStride;
						pCopy = pCopyTmp = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x = x + 3)
						{
							*(pCopy) = (byte)*(pOrig);
							*(pCopy + 1) = (byte)((*(pOrig) + *(pOrig + 1)) >> 1);
							*(pCopy + 2) = (byte)*(pOrig + 1);

							pCopy = pCopy + rStride;

							*(pCopy) = (byte)((*(pOrig) + *(pOrig + sStride)) >> 1);
							*(pCopy + 1) = (byte)((*(pOrig) + *(pOrig + 1) + *(pOrig + sStride) + *(pOrig + 1 + sStride)) >> 2);
							*(pCopy + 2) = (byte)((*(pOrig + 1) + *(pOrig + 1 + sStride)) >> 1);

							pOrig = pOrig + sStride;
							pCopy = pCopy + rStride;

							*(pCopy) = (byte)*(pOrig);
							*(pCopy + 1) = (byte)((*(pOrig) + *(pOrig + 1)) >> 1);
							*(pCopy + 2) = (byte)*(pOrig + 1);

							pOrig = pOrigTmp + 2;
							pCopy = pCopyTmp + 3;
							pOrigTmp = pOrig;
							pCopyTmp = pCopy;
						}
					}*/
				}
				#endregion
			}
		}
		#endregion

		#region Interpolate1to2()
		public static void Interpolate1to2(BitmapData sourceData, BitmapData resultData)
		{
			int sourceWidth = sourceData.Width;
			int sourceHeight = sourceData.Height;
			int resultWidth = resultData.Width;
			int resultHeight = resultData.Height;
			int x, y;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			unsafe
			{
				byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
				byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
				byte* pOrig;
				byte* pCopy;

				if (resultData.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (y = 1; y < sourceHeight - 1; y++)
					{
						pOrig = pOrigScan0 + y * sStride + 3;
						pCopy = pDestScan0 + (y * 2) * rStride + 6;

						for (x = 1; x < sourceWidth - 1; x++)
						{
							//slower
							/*
							//ul
							*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-3] + 3 * pOrig[-sStride + 0] + pOrig[-sStride - 3]) >> 4);
							*(pCopy + 1) = (byte)((9 * pOrig[1] + 3 * pOrig[-2] + 3 * pOrig[-sStride + 1] + pOrig[-sStride - 2]) >> 4);
							*(pCopy + 2) = (byte)((9 * pOrig[2] + 3 * pOrig[-1] + 3 * pOrig[-sStride + 2] + pOrig[-sStride - 1]) >> 4);

							//ur
							*(pCopy + 3) = (byte)((9 * pOrig[0] + 3 * pOrig[+3] + 3 * pOrig[-sStride + 0] + pOrig[-sStride + 3]) >> 4);
							*(pCopy + 4) = (byte)((9 * pOrig[1] + 3 * pOrig[+4] + 3 * pOrig[-sStride + 1] + pOrig[-sStride + 4]) >> 4);
							*(pCopy + 5) = (byte)((9 * pOrig[2] + 3 * pOrig[+5] + 3 * pOrig[-sStride + 2] + pOrig[-sStride + 5]) >> 4);

							//ll
							*(pCopy + rStride + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[-3] + 3 * pOrig[+sStride + 0] + pOrig[+sStride - 3]) >> 4);
							*(pCopy + rStride + 1) = (byte)((9 * pOrig[1] + 3 * pOrig[-2] + 3 * pOrig[+sStride + 1] + pOrig[+sStride - 2]) >> 4);
							*(pCopy + rStride + 2) = (byte)((9 * pOrig[2] + 3 * pOrig[-1] + 3 * pOrig[+sStride + 2] + pOrig[+sStride - 1]) >> 4);

							//lr
							*(pCopy + rStride + 3) = (byte)((9 * pOrig[0] + 3 * pOrig[+3] + 3 * pOrig[+sStride + 0] + pOrig[-sStride + 3]) >> 4);
							*(pCopy + rStride + 4) = (byte)((9 * pOrig[1] + 3 * pOrig[+4] + 3 * pOrig[+sStride + 1] + pOrig[-sStride + 4]) >> 4);
							*(pCopy + rStride + 5) = (byte)((9 * pOrig[2] + 3 * pOrig[+5] + 3 * pOrig[+sStride + 2] + pOrig[-sStride + 5]) >> 4);
							*/

							//faster
							//ul
							*(pCopy + 0) = (byte)((2 * pOrig[0] + pOrig[-3] + pOrig[-sStride + 0]) >> 2);
							*(pCopy + 1) = (byte)((2 * pOrig[1] + pOrig[-2] + pOrig[-sStride + 1]) >> 2);
							*(pCopy + 2) = (byte)((2 * pOrig[2] + pOrig[-1] + pOrig[-sStride + 2]) >> 2);

							//ur
							*(pCopy + 3) = (byte)((2 * pOrig[0] + pOrig[+3] + pOrig[-sStride + 0]) >> 2);
							*(pCopy + 4) = (byte)((2 * pOrig[1] + pOrig[+4] + pOrig[-sStride + 1]) >> 2);
							*(pCopy + 5) = (byte)((2 * pOrig[2] + pOrig[+5] + pOrig[-sStride + 2]) >> 2);

							//ll
							*(pCopy + rStride + 0) = (byte)((2 * pOrig[0] + pOrig[-3] + pOrig[+sStride + 0]) >> 2);
							*(pCopy + rStride + 1) = (byte)((2 * pOrig[1] + pOrig[-2] + pOrig[+sStride + 1]) >> 2);
							*(pCopy + rStride + 2) = (byte)((2 * pOrig[2] + pOrig[-1] + pOrig[+sStride + 2]) >> 2);

							//lr
							*(pCopy + rStride + 3) = (byte)((2 * pOrig[0] + pOrig[+3] + pOrig[+sStride + 0]) >> 2);
							*(pCopy + rStride + 4) = (byte)((2 * pOrig[1] + pOrig[+4] + pOrig[+sStride + 1]) >> 2);
							*(pCopy + rStride + 5) = (byte)((2 * pOrig[2] + pOrig[+5] + pOrig[+sStride + 2]) >> 2);

							//next area
							pOrig += 3;
							pCopy += 6;
						}
					}

					//top row
					y = 0;
					pOrig = pOrigScan0 + y * sStride + 3;
					pCopy = pDestScan0 + (y * 2) * rStride + 6;

					for (x = 1; x < sourceWidth - 1; x++)
					{
						//ul
						*(pCopy + 0) = (byte)((3 * pOrig[0] + pOrig[-3]) >> 2);
						*(pCopy + 1) = (byte)((3 * pOrig[1] + pOrig[-2]) >> 2);
						*(pCopy + 2) = (byte)((3 * pOrig[2] + pOrig[-1]) >> 2);

						//ur
						*(pCopy + 3) = (byte)((3 * pOrig[0] + pOrig[+3]) >> 2);
						*(pCopy + 4) = (byte)((3 * pOrig[1] + pOrig[+4]) >> 2);
						*(pCopy + 5) = (byte)((3 * pOrig[2] + pOrig[+5]) >> 2);

						//ll
						*(pCopy + rStride + 0) = (byte)((2 * pOrig[0] + pOrig[-3] + pOrig[+sStride + 0]) >> 2);
						*(pCopy + rStride + 1) = (byte)((2 * pOrig[1] + pOrig[-2] + pOrig[+sStride + 1]) >> 2);
						*(pCopy + rStride + 2) = (byte)((2 * pOrig[2] + pOrig[-1] + pOrig[+sStride + 2]) >> 2);

						//lr
						*(pCopy + rStride + 3) = (byte)((2 * pOrig[0] + pOrig[+3] + pOrig[+sStride + 0]) >> 2);
						*(pCopy + rStride + 4) = (byte)((2 * pOrig[1] + pOrig[+4] + pOrig[+sStride + 1]) >> 2);
						*(pCopy + rStride + 5) = (byte)((2 * pOrig[2] + pOrig[+5] + pOrig[+sStride + 2]) >> 2);

						//next area
						pOrig += 3;
						pCopy += 6;
					}

					//bottom row
					y = sourceHeight - 1;
					pOrig = pOrigScan0 + y * sStride + 3;
					pCopy = pDestScan0 + (y * 2) * rStride + 6;

					for (x = 1; x < sourceWidth - 1; x++)
					{
						//ul
						*(pCopy + 0) = (byte)((2 * pOrig[0] + pOrig[-3] + pOrig[-sStride + 0]) >> 2);
						*(pCopy + 1) = (byte)((2 * pOrig[1] + pOrig[-2] + pOrig[-sStride + 1]) >> 2);
						*(pCopy + 2) = (byte)((2 * pOrig[2] + pOrig[-1] + pOrig[-sStride + 2]) >> 2);

						//ur
						*(pCopy + 3) = (byte)((2 * pOrig[0] + pOrig[+3] + pOrig[-sStride + 0]) >> 2);
						*(pCopy + 4) = (byte)((2 * pOrig[1] + pOrig[+4] + pOrig[-sStride + 1]) >> 2);
						*(pCopy + 5) = (byte)((2 * pOrig[2] + pOrig[+5] + pOrig[-sStride + 2]) >> 2);

						//ll
						*(pCopy + rStride + 0) = (byte)((3 * pOrig[0] + pOrig[-3]) >> 2);
						*(pCopy + rStride + 1) = (byte)((3 * pOrig[1] + pOrig[-2]) >> 2);
						*(pCopy + rStride + 2) = (byte)((3 * pOrig[2] + pOrig[-1]) >> 2);

						//lr
						*(pCopy + rStride + 3) = (byte)((3 * pOrig[0] + pOrig[+3]) >> 2);
						*(pCopy + rStride + 4) = (byte)((3 * pOrig[1] + pOrig[+4]) >> 2);
						*(pCopy + rStride + 5) = (byte)((3 * pOrig[2] + pOrig[+5]) >> 2);

						//next area
						pOrig += 3;
						pCopy += 6;
					}

					x = 0;
					//left column
					for (y = 1; y < sourceHeight - 1; y++)
					{
						pOrig = pOrigScan0 + y * sStride;
						pCopy = pDestScan0 + (y * 2) * rStride;

						//ul
						*(pCopy + 0) = (byte)((3 * pOrig[0] + pOrig[-sStride + 0]) >> 2);
						*(pCopy + 1) = (byte)((3 * pOrig[1] + pOrig[-sStride + 1]) >> 2);
						*(pCopy + 2) = (byte)((3 * pOrig[2] + pOrig[-sStride + 2]) >> 2);

						//ur
						*(pCopy + 3) = (byte)((2 * pOrig[0] + pOrig[+3] + pOrig[-sStride + 0]) >> 2);
						*(pCopy + 4) = (byte)((2 * pOrig[1] + pOrig[+4] + pOrig[-sStride + 1]) >> 2);
						*(pCopy + 5) = (byte)((2 * pOrig[2] + pOrig[+5] + pOrig[-sStride + 2]) >> 2);

						//ll
						*(pCopy + rStride + 0) = (byte)((3 * pOrig[0] + pOrig[+sStride + 0]) >> 2);
						*(pCopy + rStride + 1) = (byte)((3 * pOrig[1] + pOrig[+sStride + 1]) >> 2);
						*(pCopy + rStride + 2) = (byte)((3 * pOrig[2] + pOrig[+sStride + 2]) >> 2);

						//lr
						*(pCopy + rStride + 3) = (byte)((2 * pOrig[0] + pOrig[+3] + pOrig[+sStride + 0]) >> 2);
						*(pCopy + rStride + 4) = (byte)((2 * pOrig[1] + pOrig[+4] + pOrig[+sStride + 1]) >> 2);
						*(pCopy + rStride + 5) = (byte)((2 * pOrig[2] + pOrig[+5] + pOrig[+sStride + 2]) >> 2);
					}

					//right column
					x = sourceWidth - 1;
					for (y = 1; y < sourceHeight - 1; y++)
					{
						pOrig = pOrigScan0 + y * sStride + x * 3;
						pCopy = pDestScan0 + (y * 2) * rStride + x * 6;

						//ul
						*(pCopy + 0) = (byte)((2 * pOrig[0] + pOrig[-3] + pOrig[-sStride + 0]) >> 2);
						*(pCopy + 1) = (byte)((2 * pOrig[1] + pOrig[-2] + pOrig[-sStride + 1]) >> 2);
						*(pCopy + 2) = (byte)((2 * pOrig[2] + pOrig[-1] + pOrig[-sStride + 2]) >> 2);

						//ur
						*(pCopy + 3) = (byte)((3 * pOrig[0] + pOrig[-sStride + 0]) >> 2);
						*(pCopy + 4) = (byte)((3 * pOrig[1] + pOrig[-sStride + 1]) >> 2);
						*(pCopy + 5) = (byte)((3 * pOrig[2] + pOrig[-sStride + 2]) >> 2);

						//ll
						*(pCopy + rStride + 0) = (byte)((2 * pOrig[0] + pOrig[-3] + pOrig[+sStride + 0]) >> 2);
						*(pCopy + rStride + 1) = (byte)((2 * pOrig[1] + pOrig[-2] + pOrig[+sStride + 1]) >> 2);
						*(pCopy + rStride + 2) = (byte)((2 * pOrig[2] + pOrig[-1] + pOrig[+sStride + 2]) >> 2);

						//lr
						*(pCopy + rStride + 3) = (byte)((3 * pOrig[0] + pOrig[+sStride + 0]) >> 2);
						*(pCopy + rStride + 4) = (byte)((3 * pOrig[1] + pOrig[+sStride + 1]) >> 2);
						*(pCopy + rStride + 5) = (byte)((3 * pOrig[2] + pOrig[+sStride + 2]) >> 2);
					}

					//CORNERS
					//ul corner
					pOrig = pOrigScan0;
					pCopy = pDestScan0;

					//ul
					*(pCopy + 0) = pOrig[0];
					*(pCopy + 1) = pOrig[1];
					*(pCopy + 2) = pOrig[2];

					//ur
					*(pCopy + 3) = (byte)((3 * pOrig[0] + pOrig[+3]) >> 2);
					*(pCopy + 4) = (byte)((3 * pOrig[1] + pOrig[+4]) >> 2);
					*(pCopy + 5) = (byte)((3 * pOrig[2] + pOrig[+5]) >> 2);

					//ll
					*(pCopy + rStride + 0) = (byte)((3 * pOrig[0] + pOrig[+sStride + 0]) >> 2);
					*(pCopy + rStride + 1) = (byte)((3 * pOrig[1] + pOrig[+sStride + 1]) >> 2);
					*(pCopy + rStride + 2) = (byte)((3 * pOrig[2] + pOrig[+sStride + 2]) >> 2);

					//lr
					*(pCopy + rStride + 3) = (byte)((2 * pOrig[0] + pOrig[+3] + pOrig[+sStride + 0]) >> 2);
					*(pCopy + rStride + 4) = (byte)((2 * pOrig[1] + pOrig[+4] + pOrig[+sStride + 1]) >> 2);
					*(pCopy + rStride + 5) = (byte)((2 * pOrig[2] + pOrig[+5] + pOrig[+sStride + 2]) >> 2);

					//ur corner
					pOrig = pOrigScan0 + (sourceWidth - 1) * 3;
					pCopy = pDestScan0 + (sourceWidth - 1) * 6;

					//ul
					*(pCopy + 0) = (byte)((3 * pOrig[0] + pOrig[-3]) >> 2);
					*(pCopy + 1) = (byte)((3 * pOrig[1] + pOrig[-2]) >> 2);
					*(pCopy + 2) = (byte)((3 * pOrig[2] + pOrig[-1]) >> 2);

					//ur
					*(pCopy + 3) = pOrig[0];
					*(pCopy + 4) = pOrig[1];
					*(pCopy + 5) = pOrig[2];

					//ll
					*(pCopy + rStride + 0) = (byte)((2 * pOrig[0] + pOrig[-3] + pOrig[+sStride + 0]) >> 2);
					*(pCopy + rStride + 1) = (byte)((2 * pOrig[1] + pOrig[-2] + pOrig[+sStride + 1]) >> 2);
					*(pCopy + rStride + 2) = (byte)((2 * pOrig[2] + pOrig[-1] + pOrig[+sStride + 2]) >> 2);

					//lr
					*(pCopy + rStride + 3) = (byte)((3 * pOrig[0] + pOrig[+sStride + 0]) >> 2);
					*(pCopy + rStride + 4) = (byte)((3 * pOrig[1] + pOrig[+sStride + 1]) >> 2);
					*(pCopy + rStride + 5) = (byte)((3 * pOrig[2] + pOrig[+sStride + 2]) >> 2);

					//ll corner 
					pOrig = pOrigScan0 + (sourceHeight - 1) * sStride;
					pCopy = pDestScan0 + (sourceHeight - 1) * rStride * 2;

					//ul
					*(pCopy + 0) = (byte)((3 * pOrig[0] + pOrig[-sStride + 0]) >> 2);
					*(pCopy + 1) = (byte)((3 * pOrig[1] + pOrig[-sStride + 1]) >> 2);
					*(pCopy + 2) = (byte)((3 * pOrig[2] + pOrig[-sStride + 2]) >> 2);

					//ur
					*(pCopy + 3) = (byte)((2 * pOrig[0] + pOrig[+3] + pOrig[-sStride + 0]) >> 2);
					*(pCopy + 4) = (byte)((2 * pOrig[1] + pOrig[+4] + pOrig[-sStride + 1]) >> 2);
					*(pCopy + 5) = (byte)((2 * pOrig[2] + pOrig[+5] + pOrig[-sStride + 2]) >> 2);

					//ll
					*(pCopy + rStride + 0) = pOrig[0];
					*(pCopy + rStride + 1) = pOrig[1];
					*(pCopy + rStride + 2) = pOrig[2];

					//lr
					*(pCopy + rStride + 3) = (byte)((3 * pOrig[0] + pOrig[+3]) >> 2);
					*(pCopy + rStride + 4) = (byte)((3 * pOrig[1] + pOrig[+4]) >> 2);
					*(pCopy + rStride + 5) = (byte)((3 * pOrig[2] + pOrig[+5]) >> 2);

					//lr corner
					pOrig = pOrigScan0 + (sourceHeight - 1) * sStride + (sourceWidth - 1) * 3;
					pCopy = pDestScan0 + (sourceHeight - 1) * rStride * 2 + (sourceWidth - 1) * 6;

					//ul
					*(pCopy + 0) = (byte)((2 * pOrig[0] + pOrig[-3] + pOrig[-sStride + 0]) >> 2);
					*(pCopy + 1) = (byte)((2 * pOrig[1] + pOrig[-2] + pOrig[-sStride + 1]) >> 2);
					*(pCopy + 2) = (byte)((2 * pOrig[2] + pOrig[-1] + pOrig[-sStride + 2]) >> 2);

					//ur
					*(pCopy + 3) = (byte)((3 * pOrig[0] + pOrig[-sStride + 0]) >> 2);
					*(pCopy + 4) = (byte)((3 * pOrig[1] + pOrig[-sStride + 1]) >> 2);
					*(pCopy + 5) = (byte)((3 * pOrig[2] + pOrig[-sStride + 2]) >> 2);

					//ll
					*(pCopy + rStride + 0) = (byte)((3 * pOrig[0] + pOrig[-3]) >> 2);
					*(pCopy + rStride + 1) = (byte)((3 * pOrig[1] + pOrig[-2]) >> 2);
					*(pCopy + rStride + 2) = (byte)((3 * pOrig[2] + pOrig[-1]) >> 2);

					//lr
					*(pCopy + rStride + 3) = pOrig[0];
					*(pCopy + rStride + 4) = pOrig[1];
					*(pCopy + rStride + 5) = pOrig[2];
				}
				else if (resultData.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					for (y = 1; y < sourceHeight - 1; y++)
					{
						pOrig = pOrigScan0 + y * sStride + 1;
						pCopy = pDestScan0 + (y * 2) * rStride + 2;

						for (x = 1; x < sourceWidth - 1; x++)
						{
							*(pCopy + 0) = (byte)((2 * pOrig[0] + pOrig[-1] + pOrig[-sStride + 0]) >> 2);
							*(pCopy + 1) = (byte)((2 * pOrig[0] + pOrig[+1] + pOrig[-sStride + 0]) >> 2);
							*(pCopy + rStride + 0) = (byte)((2 * pOrig[0] + pOrig[-1] + pOrig[+sStride + 0]) >> 2);
							*(pCopy + rStride + 1) = (byte)((2 * pOrig[0] + pOrig[+1] + pOrig[+sStride + 0]) >> 2);

							//next area
							pOrig += 1;
							pCopy += 2;
						}
					}

					//top row
					y = 0;
					pOrig = pOrigScan0 + y * sStride + 1;
					pCopy = pDestScan0 + (y * 2) * rStride + 2;

					for (x = 1; x < sourceWidth - 1; x++)
					{
						*(pCopy + 0) = (byte)((3 * pOrig[0] + pOrig[-1]) >> 2);
						*(pCopy + 1) = (byte)((3 * pOrig[0] + pOrig[+1]) >> 2);
						*(pCopy + rStride + 0) = (byte)((2 * pOrig[0] + pOrig[-1] + pOrig[+sStride + 0]) >> 2);
						*(pCopy + rStride + 1) = (byte)((2 * pOrig[0] + pOrig[+1] + pOrig[+sStride + 0]) >> 2);

						//next area
						pOrig += 1;
						pCopy += 2;
					}

					//bottom row
					y = sourceHeight - 1;
					pOrig = pOrigScan0 + y * sStride + 1;
					pCopy = pDestScan0 + (y * 2) * rStride + 2;

					for (x = 1; x < sourceWidth - 1; x++)
					{
						*(pCopy + 0) = (byte)((2 * pOrig[0] + pOrig[-1] + pOrig[-sStride + 0]) >> 2);
						*(pCopy + 1) = (byte)((2 * pOrig[0] + pOrig[+1] + pOrig[-sStride + 0]) >> 2);
						*(pCopy + rStride + 0) = (byte)((3 * pOrig[0] + pOrig[-1]) >> 2);
						*(pCopy + rStride + 1) = (byte)((3 * pOrig[0] + pOrig[+1]) >> 2);

						//next area
						pOrig += 1;
						pCopy += 2;
					}

					x = 0;
					//left column
					for (y = 1; y < sourceHeight - 1; y++)
					{
						pOrig = pOrigScan0 + y * sStride;
						pCopy = pDestScan0 + (y * 2) * rStride;

						*(pCopy + 0) = (byte)((3 * pOrig[0] + pOrig[-sStride + 0]) >> 2);
						*(pCopy + 1) = (byte)((2 * pOrig[0] + pOrig[+1] + pOrig[-sStride + 0]) >> 2);
						*(pCopy + rStride + 0) = (byte)((3 * pOrig[0] + pOrig[+sStride + 0]) >> 2);
						*(pCopy + rStride + 1) = (byte)((2 * pOrig[0] + pOrig[+1] + pOrig[+sStride + 0]) >> 2);
					}

					//right column
					x = sourceWidth - 1;
					for (y = 1; y < sourceHeight - 1; y++)
					{
						pOrig = pOrigScan0 + y * sStride + x * 1;
						pCopy = pDestScan0 + (y * 2) * rStride + x * 2;

						*(pCopy + 0) = (byte)((2 * pOrig[0] + pOrig[-1] + pOrig[-sStride + 0]) >> 2);
						*(pCopy + 1) = (byte)((3 * pOrig[0] + pOrig[-sStride + 0]) >> 2);
						*(pCopy + rStride + 0) = (byte)((2 * pOrig[0] + pOrig[-1] + pOrig[+sStride + 0]) >> 2);
						*(pCopy + rStride + 1) = (byte)((3 * pOrig[0] + pOrig[+sStride + 0]) >> 2);
					}

					//CORNERS
					//ul corner
					pOrig = pOrigScan0;
					pCopy = pDestScan0;

					*(pCopy + 0) = pOrig[0];
					*(pCopy + 1) = (byte)((3 * pOrig[0] + pOrig[+1]) >> 2);
					*(pCopy + rStride + 0) = (byte)((3 * pOrig[0] + pOrig[+sStride + 0]) >> 2);
					*(pCopy + rStride + 1) = (byte)((2 * pOrig[0] + pOrig[+1] + pOrig[+sStride + 0]) >> 2);

					//ur corner
					pOrig = pOrigScan0 + (sourceWidth - 1) * 1;
					pCopy = pDestScan0 + (sourceWidth - 1) * 2;

					*(pCopy + 0) = (byte)((3 * pOrig[0] + pOrig[-1]) >> 2);
					*(pCopy + 1) = pOrig[0];
					*(pCopy + rStride + 0) = (byte)((2 * pOrig[0] + pOrig[-1] + pOrig[+sStride + 0]) >> 2);
					*(pCopy + rStride + 1) = (byte)((3 * pOrig[0] + pOrig[+sStride + 0]) >> 2);

					//ll corner 
					pOrig = pOrigScan0 + (sourceHeight - 1) * sStride;
					pCopy = pDestScan0 + (sourceHeight - 1) * rStride * 2;

					*(pCopy + 0) = (byte)((3 * pOrig[0] + pOrig[-sStride + 0]) >> 2);
					*(pCopy + 1) = (byte)((2 * pOrig[0] + pOrig[+1] + pOrig[-sStride + 0]) >> 2);
					*(pCopy + rStride + 0) = pOrig[0];
					*(pCopy + rStride + 1) = (byte)((3 * pOrig[0] + pOrig[+1]) >> 2);

					//lr corner
					pOrig = pOrigScan0 + (sourceHeight - 1) * sStride + (sourceWidth - 1) * 1;
					pCopy = pDestScan0 + (sourceHeight - 1) * rStride * 2 + (sourceWidth - 1) * 2;

					*(pCopy + 0) = (byte)((2 * pOrig[0] + pOrig[-1] + pOrig[-sStride + 0]) >> 2);
					*(pCopy + 1) = (byte)((3 * pOrig[0] + pOrig[-sStride + 0]) >> 2);
					*(pCopy + rStride + 0) = (byte)((3 * pOrig[0] + pOrig[-1]) >> 2);
					*(pCopy + rStride + 1) = pOrig[0];
				}
			}
		}
		#endregion

		#region Interpolate2to1()
		public static void Interpolate2to1(BitmapData sourceData, BitmapData resultData)
		{
			int resultWidth = resultData.Width;
			int resultHeight = resultData.Height;
			int x, y;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			unsafe
			{
				byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
				byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
				byte* pOrig;
				byte* pCopy;

				if (resultData.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 2) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row
							*(pCopy) = (byte)((*pOrig + pOrig[3] + pOrig[sStride] + pOrig[sStride + 3]) >> 2);
							*(pCopy + 1) = (byte)((pOrig[1] + pOrig[4] + pOrig[sStride + 1] + pOrig[sStride + 4]) >> 2);
							*(pCopy + 2) = (byte)((pOrig[2] + pOrig[5] + pOrig[sStride + 2] + pOrig[sStride + 5]) >> 2);

							pCopy += 3;
							pOrig += 6;
						}
					}
				}
				else if (resultData.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 2) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row
							*(pCopy) = (byte)((*pOrig + pOrig[1] + pOrig[sStride] + pOrig[sStride + 1]) >> 2);

							pCopy += 1;
							pOrig += 2;
						}
					}
				}
			}
		}
		#endregion

		#region Interpolate4to1()
		public static void Interpolate4to1(BitmapData sourceData, BitmapData resultData)
		{
			int resultWidth = resultData.Width;
			int resultHeight = resultData.Height;
			int x, y;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			unsafe
			{
				byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
				byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
				byte* pOrig;
				byte* pCopy;

				if (resultData.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 4) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row
							*(pCopy) = (byte)((*pOrig + pOrig[3] + pOrig[6] + pOrig[9] +
								pOrig[sStride] + pOrig[sStride + 3] + pOrig[sStride + 6] + pOrig[sStride + 9] +
								pOrig[2 * sStride] + pOrig[2 * sStride + 3] + pOrig[2 * sStride + 6] + pOrig[2 * sStride + 9] +
								pOrig[3 * sStride] + pOrig[3 * sStride + 3] + pOrig[3 * sStride + 6] + pOrig[3 * sStride + 9]) >> 4);
							*(pCopy + 1) = (byte)((pOrig[1] + pOrig[4] + pOrig[7] + pOrig[10] +
								pOrig[sStride + 1] + pOrig[sStride + 4] + pOrig[sStride + 7] + pOrig[sStride + 10] +
								pOrig[2 * sStride + 1] + pOrig[2 * sStride + 4] + pOrig[2 * sStride + 7] + pOrig[2 * sStride + 10] +
								pOrig[3 * sStride + 1] + pOrig[3 * sStride + 4] + pOrig[3 * sStride + 7] + pOrig[3 * sStride + 10]) >> 4);
							*(pCopy + 2) = (byte)((pOrig[2] + pOrig[5] + pOrig[8] + pOrig[11] +
								pOrig[sStride + 2] + pOrig[sStride + 5] + pOrig[sStride + 8] + pOrig[sStride + 11] +
								pOrig[2 * sStride + 2] + pOrig[2 * sStride + 5] + pOrig[2 * sStride + 8] + pOrig[2 * sStride + 11] +
								pOrig[3 * sStride + 2] + pOrig[3 * sStride + 5] + pOrig[3 * sStride + 8] + pOrig[3 * sStride + 11]) >> 4);

							pCopy += 3;
							pOrig += 12;
						}
					}
				}
				else if (resultData.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 4) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row
							*(pCopy) = (byte)((*pOrig + pOrig[1] + pOrig[2] + pOrig[3] +
								pOrig[sStride] + pOrig[sStride + 1] + pOrig[sStride + 2] + pOrig[sStride + 3] +
								pOrig[2 * sStride] + pOrig[2 * sStride + 1] + pOrig[2 * sStride + 2] + pOrig[2 * sStride + 3] +
								pOrig[3 * sStride] + pOrig[3 * sStride + 1] + pOrig[3 * sStride + 2] + pOrig[3 * sStride + 3]) >> 4);

							pCopy += 1;
							pOrig += 4;
						}
					}
				}
			}
		}
		#endregion

		#region Interpolate3to1()
		public static void Interpolate3to1(BitmapData sourceData, BitmapData resultData)
		{
			int resultWidth = resultData.Width;
			int resultHeight = resultData.Height;
			int x, y;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			unsafe
			{
				byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
				byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
				byte* pOrig;
				byte* pCopy;

				if (resultData.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 3) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row
							*(pCopy) = (byte)((*pOrig + pOrig[3] + pOrig[6] +
								pOrig[sStride] + pOrig[sStride + 3] + pOrig[sStride + 6] +
								pOrig[2 * sStride] + pOrig[2 * sStride + 3] + pOrig[2 * sStride + 6]
								) / 9);
							*(pCopy + 1) = (byte)((pOrig[1] + pOrig[4] + pOrig[7] +
								pOrig[sStride + 1] + pOrig[sStride + 4] + pOrig[sStride + 7] +
								pOrig[2 * sStride + 1] + pOrig[2 * sStride + 4] + pOrig[2 * sStride + 7]
								) / 9);
							*(pCopy + 2) = (byte)((pOrig[2] + pOrig[5] + pOrig[8] +
								pOrig[sStride + 2] + pOrig[sStride + 5] + pOrig[sStride + 8] +
								pOrig[2 * sStride + 2] + pOrig[2 * sStride + 5] + pOrig[2 * sStride + 8]
								) / 9);

							pCopy += 3;
							pOrig += 9;
						}
					}
				}
				else if (resultData.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 3) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row
							*(pCopy) = (byte)((*pOrig + pOrig[1] + pOrig[2] +
								pOrig[sStride] + pOrig[sStride + 1] + pOrig[sStride + 2] +
								pOrig[2 * sStride] + pOrig[2 * sStride + 1] + pOrig[2 * sStride + 2]
								) / 9);

							pCopy += 1;
							pOrig += 3;
						}
					}
				}
			}
		}
		#endregion

		#region Interpolate4to3()
		public static void Interpolate4to3(BitmapData sourceData, BitmapData resultData)
		{
			int resultWidth = resultData.Width;
			int resultHeight = resultData.Height;
			int x, y;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			unsafe
			{
				byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
				byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
				byte* pOrig;
				byte* pCopy;

				if (resultData.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (y = 0; y < resultHeight; y = y + 3)
					{
						pOrig = pOrigScan0 + (y * 4 / 3) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x = x + 3)
						{
							//top row
							*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[3] + 3 * pOrig[sStride + 0] + pOrig[sStride + 3]) >> 4);
							*(pCopy + 1) = (byte)((9 * pOrig[1] + 3 * pOrig[4] + 3 * pOrig[sStride + 1] + pOrig[sStride + 4]) >> 4);
							*(pCopy + 2) = (byte)((9 * pOrig[2] + 3 * pOrig[5] + 3 * pOrig[sStride + 2] + pOrig[sStride + 5]) >> 4);

							*(pCopy + 3) = (byte)((5 * pOrig[3] + 5 * pOrig[6] + 3 * pOrig[sStride + 3] + 3 * pOrig[sStride + 6]) >> 4);
							*(pCopy + 4) = (byte)((5 * pOrig[4] + 5 * pOrig[7] + 3 * pOrig[sStride + 4] + 3 * pOrig[sStride + 7]) >> 4);
							*(pCopy + 5) = (byte)((5 * pOrig[5] + 5 * pOrig[8] + 3 * pOrig[sStride + 5] + 3 * pOrig[sStride + 8]) >> 4);

							*(pCopy + 6) = (byte)((9 * pOrig[09] + 3 * pOrig[6] + 3 * pOrig[sStride + 09] + pOrig[sStride + 6]) >> 4);
							*(pCopy + 7) = (byte)((9 * pOrig[10] + 3 * pOrig[7] + 3 * pOrig[sStride + 10] + pOrig[sStride + 7]) >> 4);
							*(pCopy + 8) = (byte)((9 * pOrig[11] + 3 * pOrig[8] + 3 * pOrig[sStride + 11] + pOrig[sStride + 8]) >> 4);

							//center row
							*(pCopy + rStride + 0) = (byte)((5 * pOrig[sStride + 0] + 5 * pOrig[2 * sStride + 0] + 3 * pOrig[sStride + 3] + 3 * pOrig[2 * sStride + 3]) >> 4);
							*(pCopy + rStride + 1) = (byte)((5 * pOrig[sStride + 1] + 5 * pOrig[2 * sStride + 1] + 3 * pOrig[sStride + 4] + 3 * pOrig[2 * sStride + 4]) >> 4);
							*(pCopy + rStride + 2) = (byte)((5 * pOrig[sStride + 2] + 5 * pOrig[2 * sStride + 2] + 3 * pOrig[sStride + 5] + 3 * pOrig[2 * sStride + 5]) >> 4);

							*(pCopy + rStride + 3) = (byte)((pOrig[sStride + 3] + pOrig[sStride + 6] + pOrig[2 * sStride + 3] + pOrig[2 * sStride + 6]) >> 2);
							*(pCopy + rStride + 4) = (byte)((pOrig[sStride + 4] + pOrig[sStride + 7] + pOrig[2 * sStride + 4] + pOrig[2 * sStride + 7]) >> 2);
							*(pCopy + rStride + 5) = (byte)((pOrig[sStride + 5] + pOrig[sStride + 8] + pOrig[2 * sStride + 5] + pOrig[2 * sStride + 8]) >> 2);

							*(pCopy + rStride + 6) = (byte)((5 * pOrig[sStride + 09] + 5 * pOrig[2 * sStride + 09] + 3 * pOrig[sStride + 6] + 3 * pOrig[2 * sStride + 6]) >> 4);
							*(pCopy + rStride + 7) = (byte)((5 * pOrig[sStride + 10] + 5 * pOrig[2 * sStride + 10] + 3 * pOrig[sStride + 7] + 3 * pOrig[2 * sStride + 7]) >> 4);
							*(pCopy + rStride + 8) = (byte)((5 * pOrig[sStride + 11] + 5 * pOrig[2 * sStride + 11] + 3 * pOrig[sStride + 8] + 3 * pOrig[2 * sStride + 8]) >> 4);

							//bottom row
							*(pCopy + 2 * rStride + 0) = (byte)((9 * pOrig[3 * sStride + 0] + 3 * pOrig[2 * sStride + 0] + 3 * pOrig[3 * sStride + 3] + pOrig[2 * sStride + 3]) >> 4);
							*(pCopy + 2 * rStride + 1) = (byte)((9 * pOrig[3 * sStride + 1] + 3 * pOrig[2 * sStride + 1] + 3 * pOrig[3 * sStride + 4] + pOrig[2 * sStride + 4]) >> 4);
							*(pCopy + 2 * rStride + 2) = (byte)((9 * pOrig[3 * sStride + 2] + 3 * pOrig[2 * sStride + 2] + 3 * pOrig[3 * sStride + 5] + pOrig[2 * sStride + 5]) >> 4);

							*(pCopy + 2 * rStride + 3) = (byte)((5 * pOrig[3 * sStride + 3] + 5 * pOrig[3 * sStride + 6] + 3 * pOrig[2 * sStride + 3] + 3 * pOrig[2 * sStride + 6]) >> 4);
							*(pCopy + 2 * rStride + 4) = (byte)((5 * pOrig[3 * sStride + 4] + 5 * pOrig[3 * sStride + 7] + 3 * pOrig[2 * sStride + 4] + 3 * pOrig[2 * sStride + 7]) >> 4);
							*(pCopy + 2 * rStride + 5) = (byte)((5 * pOrig[3 * sStride + 5] + 5 * pOrig[3 * sStride + 8] + 3 * pOrig[2 * sStride + 5] + 3 * pOrig[2 * sStride + 8]) >> 4);

							*(pCopy + 2 * rStride + 6) = (byte)((9 * pOrig[3 * sStride + 09] + 3 * pOrig[2 * sStride + 09] + 3 * pOrig[3 * sStride + 6] + pOrig[2 * sStride + 6]) >> 4);
							*(pCopy + 2 * rStride + 7) = (byte)((9 * pOrig[3 * sStride + 10] + 3 * pOrig[2 * sStride + 10] + 3 * pOrig[3 * sStride + 7] + pOrig[2 * sStride + 7]) >> 4);
							*(pCopy + 2 * rStride + 8) = (byte)((9 * pOrig[3 * sStride + 11] + 3 * pOrig[2 * sStride + 11] + 3 * pOrig[3 * sStride + 8] + pOrig[2 * sStride + 8]) >> 4);

							//next area
							pOrig += 12;
							pCopy += 9;
						}
					}
				}
				else if (resultData.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					for (y = 0; y < resultHeight; y = y + 3)
					{
						pOrig = pOrigScan0 + (y * 4 / 3) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x = x + 3)
						{
							//top row
							*(pCopy + 0) = (byte)((9 * pOrig[0] + 3 * pOrig[1] + 3 * pOrig[sStride + 0] + pOrig[sStride + 1]) >> 4);
							*(pCopy + 1) = (byte)((5 * pOrig[1] + 5 * pOrig[2] + 3 * pOrig[sStride + 1] + 3 * pOrig[sStride + 2]) >> 4);
							*(pCopy + 2) = (byte)((9 * pOrig[3] + 3 * pOrig[2] + 3 * pOrig[sStride + 3] + pOrig[sStride + 2]) >> 4);

							//center row
							*(pCopy + rStride + 0) = (byte)((5 * pOrig[sStride + 0] + 5 * pOrig[2 * sStride + 0] + 3 * pOrig[sStride + 1] + 3 * pOrig[2 * sStride + 1]) >> 4);
							*(pCopy + rStride + 1) = (byte)((    pOrig[sStride + 1] +     pOrig[sStride + 2] +     pOrig[2 * sStride + 1] +     pOrig[2 * sStride + 2]) >> 2);
							*(pCopy + rStride + 2) = (byte)((5 * pOrig[sStride + 3] + 5 * pOrig[2 * sStride + 3] + 3 * pOrig[sStride + 2] + 3 * pOrig[2 * sStride + 2]) >> 4);

							//bottom row
							*(pCopy + 2 * rStride + 0) = (byte)((9 * pOrig[3 * sStride + 0] + 3 * pOrig[2 * sStride + 0] + 3 * pOrig[3 * sStride + 1] +     pOrig[2 * sStride + 1]) >> 4);
							*(pCopy + 2 * rStride + 1) = (byte)((5 * pOrig[3 * sStride + 1] + 5 * pOrig[3 * sStride + 2] + 3 * pOrig[2 * sStride + 1] + 3 * pOrig[2 * sStride + 2]) >> 4);
							*(pCopy + 2 * rStride + 2) = (byte)((9 * pOrig[3 * sStride + 3] + 3 * pOrig[2 * sStride + 3] + 3 * pOrig[3 * sStride + 2] +     pOrig[2 * sStride + 2]) >> 4);

							//next area
							pOrig += 4;
							pCopy += 3;
						}
					}
				}
			}
		}
		#endregion

		#region Interpolate6to1()
		public static void Interpolate6to1(BitmapData sourceData, BitmapData resultData)
		{
			int resultWidth = resultData.Width;
			int resultHeight = resultData.Height;
			int x, y;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			unsafe
			{
				byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
				byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
				byte* pOrig;
				byte* pCopy;

				if (resultData.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 6) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row
							*(pCopy) = (byte)((*pOrig + pOrig[3] + pOrig[6] + pOrig[9] + pOrig[12] + pOrig[15] +
								pOrig[sStride] + pOrig[sStride + 3] + pOrig[sStride + 6] + pOrig[sStride + 9] + pOrig[sStride + 12] + pOrig[sStride + 15] +
								pOrig[2 * sStride] + pOrig[2 * sStride + 3] + pOrig[2 * sStride + 6] + pOrig[2 * sStride + 9] + pOrig[2 * sStride + 12] + pOrig[2 * sStride + 15] +
								pOrig[3 * sStride] + pOrig[3 * sStride + 3] + pOrig[3 * sStride + 6] + pOrig[3 * sStride + 9] + pOrig[3 * sStride + 12] + pOrig[3 * sStride + 15] +
								pOrig[4 * sStride] + pOrig[4 * sStride + 3] + pOrig[4 * sStride + 6] + pOrig[4 * sStride + 9] + pOrig[4 * sStride + 12] + pOrig[4 * sStride + 15] +
								pOrig[5 * sStride] + pOrig[5 * sStride + 3] + pOrig[5 * sStride + 6] + pOrig[5 * sStride + 9] + pOrig[5 * sStride + 12] + pOrig[5 * sStride + 15])
									/ 36);
							*(pCopy + 1) = (byte)((pOrig[1] + pOrig[4] + pOrig[7] + pOrig[10] + pOrig[13] + pOrig[16] +
								pOrig[sStride + 1] + pOrig[sStride + 4] + pOrig[sStride + 7] + pOrig[sStride + 10] + pOrig[sStride + 13] + pOrig[sStride + 16] +
								pOrig[2 * sStride + 1] + pOrig[2 * sStride + 4] + pOrig[2 * sStride + 7] + pOrig[2 * sStride + 10] + pOrig[2 * sStride + 13] + pOrig[2 * sStride + 16] +
								pOrig[3 * sStride + 1] + pOrig[3 * sStride + 4] + pOrig[3 * sStride + 7] + pOrig[3 * sStride + 10] + pOrig[3 * sStride + 13] + pOrig[3 * sStride + 16] +
								pOrig[4 * sStride + 1] + pOrig[4 * sStride + 4] + pOrig[4 * sStride + 7] + pOrig[4 * sStride + 10] + pOrig[4 * sStride + 13] + pOrig[4 * sStride + 16] +
								pOrig[5 * sStride + 1] + pOrig[5 * sStride + 4] + pOrig[5 * sStride + 7] + pOrig[5 * sStride + 10] + pOrig[5 * sStride + 13] + pOrig[5 * sStride + 16]
								) / 36);
							*(pCopy + 2) = (byte)((pOrig[2] + pOrig[5] + pOrig[8] + pOrig[11] +
								pOrig[sStride + 2] + pOrig[sStride + 5] + pOrig[sStride + 8] + pOrig[sStride + 11] + pOrig[sStride + 14] + pOrig[sStride + 17] +
								pOrig[2 * sStride + 2] + pOrig[2 * sStride + 5] + pOrig[2 * sStride + 8] + pOrig[2 * sStride + 11] + pOrig[2 * sStride + 14] + pOrig[2 * sStride + 17] +
								pOrig[3 * sStride + 2] + pOrig[3 * sStride + 5] + pOrig[3 * sStride + 8] + pOrig[3 * sStride + 11] + pOrig[3 * sStride + 14] + pOrig[3 * sStride + 17] +
								pOrig[4 * sStride + 2] + pOrig[4 * sStride + 5] + pOrig[4 * sStride + 8] + pOrig[4 * sStride + 11] + pOrig[4 * sStride + 14] + pOrig[4 * sStride + 17] +
								pOrig[5 * sStride + 2] + pOrig[5 * sStride + 5] + pOrig[5 * sStride + 8] + pOrig[5 * sStride + 11] + pOrig[5 * sStride + 14] + pOrig[5 * sStride + 17]
								) / 36);

							pCopy += 3;
							pOrig += 18;
						}
					}
				}
				else if (resultData.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 6) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row
							*(pCopy) = (byte)((*pOrig + pOrig[1] + pOrig[2] + pOrig[3] + pOrig[4] + pOrig[5] +
								pOrig[sStride] + pOrig[sStride + 1] + pOrig[sStride + 2] + pOrig[sStride + 3] + pOrig[sStride + 4] + pOrig[sStride + 5] +
								pOrig[2 * sStride] + pOrig[2 * sStride + 1] + pOrig[2 * sStride + 2] + pOrig[2 * sStride + 3] + pOrig[2 * sStride + 4] + pOrig[2 * sStride + 5] +
								pOrig[3 * sStride] + pOrig[3 * sStride + 1] + pOrig[3 * sStride + 2] + pOrig[3 * sStride + 3] + pOrig[3 * sStride + 4] + pOrig[3 * sStride + 5] +
								pOrig[4 * sStride] + pOrig[4 * sStride + 1] + pOrig[4 * sStride + 2] + pOrig[4 * sStride + 3] + pOrig[4 * sStride + 4] + pOrig[4 * sStride + 5] +
								pOrig[5 * sStride] + pOrig[5 * sStride + 1] + pOrig[5 * sStride + 2] + pOrig[5 * sStride + 3] + pOrig[5 * sStride + 4] + pOrig[5 * sStride + 5])
									/ 36);

							pCopy += 1;
							pOrig += 6;
						}
					}
				}
			}
		}
		#endregion

		#region Interpolate8to1()
		public static void Interpolate8to1(BitmapData sourceData, BitmapData resultData)
		{
			int resultWidth = resultData.Width;
			int resultHeight = resultData.Height;
			int x, y;

			int sStride = sourceData.Stride;
			int rStride = resultData.Stride;

			unsafe
			{
				byte* pOrigScan0 = (byte*)sourceData.Scan0.ToPointer();
				byte* pDestScan0 = (byte*)resultData.Scan0.ToPointer();
				byte* pOrig;
				byte* pCopy;

				if (resultData.PixelFormat == PixelFormat.Format24bppRgb)
				{
					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 8) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row
							*(pCopy) = (byte)((
								pOrig[0] + pOrig[3] + pOrig[6] + pOrig[9] + pOrig[12] + pOrig[15] + pOrig[18] + pOrig[21] +
								pOrig[sStride] + pOrig[sStride + 3] + pOrig[sStride + 6] + pOrig[sStride + 9] + pOrig[sStride + 12] + pOrig[sStride + 15] + pOrig[sStride + 18] + pOrig[sStride + 21] +
								pOrig[2 * sStride] + pOrig[2 * sStride + 3] + pOrig[2 * sStride + 6] + pOrig[2 * sStride + 9] + pOrig[2 * sStride + 12] + pOrig[2 * sStride + 15] + pOrig[2 * sStride + 18] + pOrig[2 * sStride + 21] +
								pOrig[3 * sStride] + pOrig[3 * sStride + 3] + pOrig[3 * sStride + 6] + pOrig[3 * sStride + 9] + pOrig[3 * sStride + 12] + pOrig[3 * sStride + 15] + pOrig[3 * sStride + 18] + pOrig[3 * sStride + 21] +
								pOrig[4 * sStride] + pOrig[4 * sStride + 3] + pOrig[4 * sStride + 6] + pOrig[4 * sStride + 9] + pOrig[4 * sStride + 12] + pOrig[4 * sStride + 15] + pOrig[4 * sStride + 18] + pOrig[4 * sStride + 21] +
								pOrig[5 * sStride] + pOrig[5 * sStride + 3] + pOrig[5 * sStride + 6] + pOrig[5 * sStride + 9] + pOrig[5 * sStride + 12] + pOrig[5 * sStride + 15] + pOrig[5 * sStride + 18] + pOrig[5 * sStride + 21] +
								pOrig[6 * sStride] + pOrig[6 * sStride + 3] + pOrig[6 * sStride + 6] + pOrig[6 * sStride + 9] + pOrig[6 * sStride + 12] + pOrig[6 * sStride + 15] + pOrig[6 * sStride + 18] + pOrig[6 * sStride + 21] +
								pOrig[7 * sStride] + pOrig[7 * sStride + 3] + pOrig[7 * sStride + 6] + pOrig[7 * sStride + 9] + pOrig[7 * sStride + 12] + pOrig[7 * sStride + 15] + pOrig[7 * sStride + 18] + pOrig[7 * sStride + 21]
								) >> 6);
							*(pCopy + 1) = (byte)((
								pOrig[1] + pOrig[4] + pOrig[7] + pOrig[10] + pOrig[13] + pOrig[16] + pOrig[19] + pOrig[22] +
								pOrig[sStride + 1] + pOrig[sStride + 4] + pOrig[sStride + 7] + pOrig[sStride + 10] + pOrig[sStride + 13] + pOrig[sStride + 16] + pOrig[sStride + 19] + pOrig[sStride + 22] +
								pOrig[2 * sStride + 1] + pOrig[2 * sStride + 4] + pOrig[2 * sStride + 7] + pOrig[2 * sStride + 10] + pOrig[2 * sStride + 13] + pOrig[2 * sStride + 16] + pOrig[2 * sStride + 19] + pOrig[2 * sStride + 22] +
								pOrig[3 * sStride + 1] + pOrig[3 * sStride + 4] + pOrig[3 * sStride + 7] + pOrig[3 * sStride + 10] + pOrig[3 * sStride + 13] + pOrig[3 * sStride + 16] + pOrig[3 * sStride + 19] + pOrig[3 * sStride + 22] +
								pOrig[4 * sStride + 1] + pOrig[4 * sStride + 4] + pOrig[4 * sStride + 7] + pOrig[4 * sStride + 10] + pOrig[4 * sStride + 13] + pOrig[4 * sStride + 16] + pOrig[4 * sStride + 19] + pOrig[4 * sStride + 22] +
								pOrig[5 * sStride + 1] + pOrig[5 * sStride + 4] + pOrig[5 * sStride + 7] + pOrig[5 * sStride + 10] + pOrig[5 * sStride + 13] + pOrig[5 * sStride + 16] + pOrig[5 * sStride + 19] + pOrig[5 * sStride + 22] +
								pOrig[6 * sStride + 1] + pOrig[6 * sStride + 4] + pOrig[6 * sStride + 7] + pOrig[6 * sStride + 10] + pOrig[6 * sStride + 13] + pOrig[6 * sStride + 16] + pOrig[6 * sStride + 19] + pOrig[6 * sStride + 22] +
								pOrig[7 * sStride + 1] + pOrig[7 * sStride + 4] + pOrig[7 * sStride + 7] + pOrig[7 * sStride + 10] + pOrig[7 * sStride + 13] + pOrig[7 * sStride + 16] + pOrig[7 * sStride + 19] + pOrig[7 * sStride + 22]
								) >> 6);
							*(pCopy + 2) = (byte)((
								pOrig[2] + pOrig[5] + pOrig[8] + pOrig[11] + pOrig[14] + pOrig[17] + pOrig[20] + pOrig[23] +
								pOrig[sStride + 2] + pOrig[sStride + 5] + pOrig[sStride + 8] + pOrig[sStride + 11] + pOrig[sStride + 14] + pOrig[sStride + 17] + pOrig[sStride + 20] + pOrig[sStride + 23] +
								pOrig[2 * sStride + 2] + pOrig[2 * sStride + 5] + pOrig[2 * sStride + 8] + pOrig[2 * sStride + 11] + pOrig[2 * sStride + 14] + pOrig[2 * sStride + 17] + pOrig[2 * sStride + 20] + pOrig[2 * sStride + 23] +
								pOrig[3 * sStride + 2] + pOrig[3 * sStride + 5] + pOrig[3 * sStride + 8] + pOrig[3 * sStride + 11] + pOrig[3 * sStride + 14] + pOrig[3 * sStride + 17] + pOrig[3 * sStride + 20] + pOrig[3 * sStride + 23] +
								pOrig[4 * sStride + 2] + pOrig[4 * sStride + 5] + pOrig[4 * sStride + 8] + pOrig[4 * sStride + 11] + pOrig[4 * sStride + 14] + pOrig[4 * sStride + 17] + pOrig[4 * sStride + 20] + pOrig[4 * sStride + 23] +
								pOrig[5 * sStride + 2] + pOrig[5 * sStride + 5] + pOrig[5 * sStride + 8] + pOrig[5 * sStride + 11] + pOrig[5 * sStride + 14] + pOrig[5 * sStride + 17] + pOrig[5 * sStride + 20] + pOrig[5 * sStride + 23] +
								pOrig[6 * sStride + 2] + pOrig[6 * sStride + 5] + pOrig[6 * sStride + 8] + pOrig[6 * sStride + 11] + pOrig[6 * sStride + 14] + pOrig[6 * sStride + 17] + pOrig[6 * sStride + 20] + pOrig[6 * sStride + 23] +
								pOrig[7 * sStride + 2] + pOrig[7 * sStride + 5] + pOrig[7 * sStride + 8] + pOrig[7 * sStride + 11] + pOrig[7 * sStride + 14] + pOrig[7 * sStride + 17] + pOrig[7 * sStride + 20] + pOrig[7 * sStride + 23]
								) >> 6);

							pCopy += 3;
							pOrig += 24;
						}
					}
				}
				else if (resultData.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					for (y = 0; y < resultHeight; y++)
					{
						pOrig = pOrigScan0 + (y * 8) * sStride;
						pCopy = pDestScan0 + y * rStride;

						for (x = 0; x < resultWidth; x++)
						{
							//top row
							*(pCopy) = (byte)((
								pOrig[0] + pOrig[1] + pOrig[2] + pOrig[3] + pOrig[4] + pOrig[5] + pOrig[6] + pOrig[7] +
								pOrig[sStride] + pOrig[sStride + 1] + pOrig[sStride + 2] + pOrig[sStride + 3] + pOrig[sStride + 4] + pOrig[sStride + 5] + pOrig[sStride + 6] + pOrig[sStride + 7] +
								pOrig[2 * sStride] + pOrig[2 * sStride + 1] + pOrig[2 * sStride + 2] + pOrig[2 * sStride + 3] + pOrig[2 * sStride + 4] + pOrig[2 * sStride + 5] + pOrig[2 * sStride + 6] + pOrig[2 * sStride + 7] +
								pOrig[3 * sStride] + pOrig[3 * sStride + 1] + pOrig[3 * sStride + 2] + pOrig[3 * sStride + 3] + pOrig[3 * sStride + 4] + pOrig[3 * sStride + 5] + pOrig[3 * sStride + 6] + pOrig[3 * sStride + 7] +
								pOrig[4 * sStride] + pOrig[4 * sStride + 1] + pOrig[4 * sStride + 2] + pOrig[4 * sStride + 3] + pOrig[4 * sStride + 4] + pOrig[4 * sStride + 5] + pOrig[4 * sStride + 6] + pOrig[4 * sStride + 7] +
								pOrig[5 * sStride] + pOrig[5 * sStride + 1] + pOrig[5 * sStride + 2] + pOrig[5 * sStride + 3] + pOrig[5 * sStride + 4] + pOrig[5 * sStride + 5] + pOrig[5 * sStride + 6] + pOrig[5 * sStride + 7] +
								pOrig[6 * sStride] + pOrig[6 * sStride + 1] + pOrig[6 * sStride + 2] + pOrig[6 * sStride + 3] + pOrig[6 * sStride + 4] + pOrig[6 * sStride + 5] + pOrig[6 * sStride + 6] + pOrig[6 * sStride + 7] +
								pOrig[7 * sStride] + pOrig[7 * sStride + 1] + pOrig[7 * sStride + 2] + pOrig[7 * sStride + 3] + pOrig[7 * sStride + 4] + pOrig[7 * sStride + 5] + pOrig[7 * sStride + 6] + pOrig[7 * sStride + 7]
								) >> 6);

							pCopy += 1;
							pOrig += 8;
						}
					}
				}
			}
		}
		#endregion

		#endregion

	}

}
