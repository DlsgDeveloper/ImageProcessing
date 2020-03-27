using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Dilation.
	/// </summary>
	public class Dilation
	{				
		#region constructor
		private Dilation()
		{
		}
		#endregion

		#region enum Operator
		public enum Operator
		{
			Cross,
			Full
		}
		#endregion
		
		//	PUBLIC METHODS
		#region public methods

		#region Get()
		public static Bitmap Get(Bitmap source, Rectangle clip, Dilation.Operator dilationOperator)
		{
			if(source == null)
				return null ;

			if(clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, source.Width, source.Height) ;
			else
				clip = Rectangle.Intersect(clip, new Rectangle(0, 0, source.Width, source.Height));


			Bitmap		result = null;
			
			try
			{
				switch(source.PixelFormat)
				{
					case PixelFormat.Format1bppIndexed :
						result = Get1bpp(source, clip, dilationOperator);
						break ;
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}
				
				if(result != null)
				{
					Misc.SetBitmapResolution(result, source.HorizontalResolution, source.VerticalResolution);					
				}
			}
			catch(Exception ex)
			{
				throw new Exception("Dilation, Get(): " + ex.Message ) ;
			}

			return result ;
		}
		#endregion

		#region Go()
		public static void Go(Bitmap source, Rectangle clip, Dilation.Operator dilationOperator)
		{
			if (clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, source.Width, source.Height);
			else
				clip = Rectangle.Intersect(clip, new Rectangle(0, 0, source.Width, source.Height));

			try
			{
				switch (source.PixelFormat)
				{
					case PixelFormat.Format1bppIndexed:
						Go1bpp(source, clip, dilationOperator);
						break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Erosion, Go(): " + ex.Message);
			}
		}
		#endregion

		#endregion

		
		//PRIVATE METHODS

		#region Get1bpp()
		private static Bitmap Get1bpp(Bitmap source, Rectangle clip, Dilation.Operator mask)
		{
			Bitmap result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
#if DEBUG
				DateTime start = DateTime.Now;
#endif
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				int height = result.Height;
				int width = result.Width;
				int x, y;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pUp, pCurrent, pDown;

					for (y = 0; y < height; y++)
					{
						pUp = (y == 0) ? pSource + y * sStride : pSource + (y - 1) * sStride;
						pCurrent = pSource + y * sStride;
						pDown = (y == height - 1) ? pSource + y * sStride : pSource + (y + 1) * sStride;

						for (x = 0; x < width; x = x + 8)
						{
							if (x >= 8)
							{
								if ((pUp[-1] & 0x01) > 0 || (*pUp & 0xC0) > 0 || (pCurrent[-1] & 0x01) > 0 || (*pCurrent & 0xC0) > 0 || (pDown[-1] & 0x01) > 0 || (*pDown & 0xC0) > 0)
									pResult[y * rStride + x / 8] |= 0x80;
							}
							else
							{
								if ((*pUp & 0xC0) > 0 || (*pCurrent & 0xC0) > 0 || (*pDown & 0xC0) > 0)
									pResult[y * rStride + x / 8] |= 0x80;
							}

							if ((*pUp & 0xE0) > 0 || (*pCurrent & 0xE0) > 0 || (*pDown & 0xE0) > 0)
								pResult[y * rStride + x / 8] |= 0x40;
							if ((*pUp & 0x70) > 0 || (*pCurrent & 0x70) > 0 || (*pDown & 0x70) > 0)
								pResult[y * rStride + x / 8] |= 0x20;
							if ((*pUp & 0x38) > 0 || (*pCurrent & 0x38) > 0 || (*pDown & 0x38) > 0)
								pResult[y * rStride + x / 8] |= 0x10;
							if ((*pUp & 0x1C) > 0 || (*pCurrent & 0x1C) > 0 || (*pDown & 0x1C) > 0)
								pResult[y * rStride + x / 8] |= 0x08;
							if ((*pUp & 0x0E) > 0 || (*pCurrent & 0x0E) > 0 || (*pDown & 0x0E) > 0)
								pResult[y * rStride + x / 8] |= 0x04;
							if ((*pUp & 0x07) > 0 || (*pCurrent & 0x07) > 0 || (*pDown & 0x07) > 0)
								pResult[y * rStride + x / 8] |= 0x02;

							if (x + 8 < width)
							{
								if ((*pUp & 0x03) > 0 || (pUp[1] & 0x80) > 0 || (*pCurrent & 0x03) > 0 || (pCurrent[1] & 0x80) > 0 || (*pDown & 0x03) > 0 || (pDown[1] & 0x80) > 0)
									pResult[y * rStride + x / 8] |= 0x01;
							}
							else
							{
								if ((*pUp & 0x03) > 0 || (*pCurrent & 0x03) > 0 || (*pDown & 0x03) > 0)
									pResult[y * rStride + x / 8] |= 0x01;
							}

							pUp++;
							pCurrent++;
							pDown++;
						}
					}
				}


#if DEBUG
				Console.WriteLine("Dilation GetFrom1bpp():" + (DateTime.Now.Subtract(start)).ToString());
#endif
				return result;
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region Go1bpp()
		private static void Go1bpp(Bitmap source, Rectangle clip, Dilation.Operator mask)
		{
			BitmapData sourceData = null;

			try
			{
#if DEBUG
				DateTime start = DateTime.Now;
#endif
				sourceData = source.LockBits(clip, ImageLockMode.ReadWrite, source.PixelFormat);

				int stride = sourceData.Stride;
				int width = sourceData.Width;
				int height = sourceData.Height;
				int x, y;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte[] pCurrent = new byte[stride];
					byte[] pDown = null;

					for (x = 0; x < stride; x++)
						pCurrent[x] = pSource[x];

					if((width % 8) > 0)
						pCurrent[width / 8] &= (byte)(0xFF00 >> (width % 8));

					for (y = 0; y < height; y++)
					{
						int yMinus1 = (y - 1 > 0) ? y - 1 : 0;
						int yPlus1 = (y + 1 < height) ? y + 1 : height - 1;

						if (y < height - 1)
						{
							pDown = new byte[stride];

							for (x = 0; x < stride; x++)
								pDown[x] = pSource[(y + 1) * stride + x];

							if ((width % 8) > 0)
								pDown[width / 8] &= (byte)(0xFF00 >> (width % 8));
						}

						for (x = 0; x < width; x = x + 8)
						{
							if ((pCurrent[x / 8] & 0x80) != 0)
							{
								if (x > 0)
								{
									pSource[yMinus1 * stride + (x - 1) / 8] |= 0x01;
									pSource[y * stride + (x - 1) / 8] |= 0x01;
									pSource[yPlus1 * stride + (x - 1) / 8] |= 0x01;
								}

								pSource[yMinus1 * stride + x / 8] |= 0xC0;
								pSource[y * stride + x / 8] |= 0xC0;
								pSource[yPlus1 * stride + x / 8] |= 0xC0;
							}
							if ((pCurrent[x / 8] & 0x40) != 0)
							{
								pSource[yMinus1 * stride + x / 8] |= 0xE0;
								pSource[y * stride + x / 8] |= 0xE0;
								pSource[yPlus1 * stride + x / 8] |= 0xE0;
							}
							if ((pCurrent[x / 8] & 0x20) != 0)
							{
								pSource[yMinus1 * stride + x / 8] |= 0x70;
								pSource[y * stride + x / 8] |= 0x70;
								pSource[yPlus1 * stride + x / 8] |= 0x70;
							}
							if ((pCurrent[x / 8] & 0x10) != 0)
							{
								pSource[yMinus1 * stride + x / 8] |= 0x38;
								pSource[y * stride + x / 8] |= 0x38;
								pSource[yPlus1 * stride + x / 8] |= 0x38;
							}
							if ((pCurrent[x / 8] & 0x08) != 0)
							{
								pSource[yMinus1 * stride + x / 8] |= 0x1C;
								pSource[y * stride + x / 8] |= 0x1C;
								pSource[yPlus1 * stride + x / 8] |= 0x1C;
							}
							if ((pCurrent[x / 8] & 0x04) != 0)
							{
								pSource[yMinus1 * stride + x / 8] |= 0x0E;
								pSource[y * stride + x / 8] |= 0x0E;
								pSource[yPlus1 * stride + x / 8] |= 0x0E;
							}
							if ((pCurrent[x / 8] & 0x02) != 0)
							{
								pSource[yMinus1 * stride + x / 8] |= 0x07;
								pSource[y * stride + x / 8] |= 0x07;
								pSource[yPlus1 * stride + x / 8] |= 0x07;
							}
							if ((pCurrent[x / 8] & 0x01) != 0)
							{
								pSource[yMinus1 * stride + x / 8] |= 0x03;
								pSource[y * stride + x / 8] |= 0x03;
								pSource[yPlus1 * stride + x / 8] |= 0x03;

								if ((x + 8) / 8 < stride)
								{
									pSource[yMinus1 * stride + (x + 8) / 8] |= 0x80;
									pSource[y * stride + (x + 8) / 8] |= 0x80;
									pSource[yPlus1 * stride + (x + 8) / 8] |= 0x80;
								}
							}
						}

						pCurrent = pDown;
					}

				}

#if DEBUG
				Console.WriteLine("Erosion Go1bpp():" + (DateTime.Now.Subtract(start)).ToString());
#endif
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
			}
		}
		#endregion

		#region GetConvolutionMask()
		private static int[,] GetConvolutionMask(Dilation.Operator mask)
		{
			int[,]	maskArray = null;
			
			switch(mask)
			{
				case Dilation.Operator.Cross:
					maskArray = new int[,]{{0,1,0},
											{1,0,1},
											{0,1,0}};
					break;
				default:
					maskArray = new int[,]{{1,1,1},
											{1,0,1},
											{1,1,1}};
					break;
			}

			return maskArray; 
		}
		#endregion

	}
}
