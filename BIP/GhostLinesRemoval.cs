using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	public class GhostLinesRemoval
	{

		#region constructor
		private GhostLinesRemoval()
		{
		}
		#endregion

		//	PUBLIC METHODS

		#region Get()
		public static int[] Get(Bitmap bitmap, byte lowThreshold, byte highThreshold, byte linesToCheck, byte maxDelta)
		{
			ArrayList ghostLinesList = null;

			try
			{

				switch (bitmap.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed:
						{
							ghostLinesList = Get8bppGray(bitmap, lowThreshold, highThreshold, linesToCheck, maxDelta);
						} break;
					case PixelFormat.Format24bppRgb:
						{
							ghostLinesList = Get24bpp(bitmap, lowThreshold, highThreshold, linesToCheck, maxDelta);
						} break;
					case PixelFormat.Format32bppRgb:
					case PixelFormat.Format32bppArgb:
						{
							ghostLinesList = Get32bpp(bitmap, lowThreshold, highThreshold, linesToCheck, maxDelta);
						} break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("GhostLinesRemoval, Get(): " + ex.Message);
			}

			if (ghostLinesList != null)
				return (int[])ghostLinesList.ToArray(typeof(int));
			else
				return new int[0];
		}
		#endregion

		//PRIVATE METHODS

		#region Get32bpp()
		private static ArrayList Get32bpp(Bitmap bitmap, byte lowThreshold, byte highThreshold, byte linesToCheck, byte maxDelta)
		{
			ArrayList ghostLinesList = new ArrayList();
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				
				unsafe
				{
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent = pOrig + 5 * 4;
					byte red, green, blue;

					for (int x = 5; x < bitmapData.Width - 5; x++)
					{
						blue = *pCurrent;
						green = pCurrent[1];
						red = pCurrent[2];

						if ((red < highThreshold && red > lowThreshold) || (green < highThreshold && green > lowThreshold) || (blue < highThreshold && blue > lowThreshold))
						{
							if (IsBackground32bpp(pOrig, bitmapData.Stride, new Rectangle(x - 5, 0, 10, linesToCheck), lowThreshold, highThreshold, maxDelta))
								ghostLinesList.Add(x);
						}

						pCurrent += 4;
					}


					pCurrent = pOrig + (bitmapData.Height - linesToCheck - 1) * bitmapData.Stride + 5 * 4;
					for (int x = 5; x < bitmapData.Width - 5; x++)
					{
						blue = *pCurrent;
						green = pCurrent[1];
						red = pCurrent[2];

						if ((red < highThreshold && red > lowThreshold) || (green < highThreshold && green > lowThreshold) || (blue < highThreshold && blue > lowThreshold))
						{
							if (!ghostLinesList.Contains(x))
								if (IsBackground32bpp(pOrig, bitmapData.Stride, new Rectangle(x - 5, 0, 10, linesToCheck), lowThreshold, highThreshold, maxDelta))
									ghostLinesList.Add(x);
						}

						pCurrent += 4;
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			return ghostLinesList;
		}
		#endregion

		#region Get24bpp()
		private static ArrayList Get24bpp(Bitmap bitmap, byte lowThreshold, byte highThreshold, byte linesToCheck, byte maxDelta)
		{
			ArrayList ghostLinesList = new ArrayList();
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				
				unsafe
				{
					
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent;
					byte red, green, blue;

					pCurrent = pOrig;
					for (int x = 0; x < 8; x++)
					{
						blue = *pCurrent;
						green = pCurrent[1];
						red = pCurrent[2];

						if ((red < highThreshold && red > lowThreshold) || (green < highThreshold && green > lowThreshold) || (blue < highThreshold && blue > lowThreshold))
						{
							if (IsBackground24bpp(pOrig, bitmapData.Stride, new Rectangle(x, 0, 7, linesToCheck), lowThreshold, highThreshold, maxDelta))
								ghostLinesList.Add(x);
						}

						pCurrent += 3;
					}
					
					pCurrent = pOrig + 7 * 3;
					for (int x = 7; x < bitmapData.Width - 7; x++)
					{
						blue = *pCurrent;
						green = pCurrent[1];
						red = pCurrent[2];

						if ((red < highThreshold && red > lowThreshold) || (green < highThreshold && green > lowThreshold) || (blue < highThreshold && blue > lowThreshold))
						{
							if (IsBackground24bpp(pOrig, bitmapData.Stride, new Rectangle(x - 7, 0, 14, linesToCheck), lowThreshold, highThreshold, maxDelta))
								ghostLinesList.Add(x);
						}

						pCurrent += 3;
					}

					pCurrent = pOrig + (bitmapData.Width - 7) * 3;
					for (int x = bitmapData.Width - 7; x < bitmapData.Width; x++)
					{
						blue = *pCurrent;
						green = pCurrent[1];
						red = pCurrent[2];

						if ((red < highThreshold && red > lowThreshold) || (green < highThreshold && green > lowThreshold) || (blue < highThreshold && blue > lowThreshold))
						{
							if (IsBackground24bpp(pOrig, bitmapData.Stride, new Rectangle(x - 7, 0, 7, linesToCheck), lowThreshold, highThreshold, maxDelta))
								ghostLinesList.Add(x);
						}

						pCurrent += 3;
					}

					pCurrent = pOrig + (bitmapData.Height - linesToCheck - 1) * bitmapData.Stride + 7 * 3;
					for (int x = 7; x < bitmapData.Width - 7; x++)
					{
						blue = *pCurrent;
						green = pCurrent[1];
						red = pCurrent[2];

						if ((red < highThreshold && red > lowThreshold) || (green < highThreshold && green > lowThreshold) || (blue < highThreshold && blue > lowThreshold))
						{
							if (!ghostLinesList.Contains(x))
								if (IsBackground24bpp(pOrig, bitmapData.Stride, new Rectangle(x - 7, 0, 14, linesToCheck), lowThreshold, highThreshold, maxDelta))
									ghostLinesList.Add(x);
						}

						pCurrent += 3;
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			return ghostLinesList;
		}
		#endregion

		#region Get8bppGray()
		private static ArrayList Get8bppGray(Bitmap bitmap, byte lowThreshold, byte highThreshold, byte linesToCheck, byte maxDelta)
		{
			ArrayList ghostLinesList = new ArrayList();
			BitmapData bitmapData = null;

			try
			{
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				unsafe
				{
					byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
					byte* pCurrent = pOrig + 5;
					byte gray;

					for (int x = 5; x < bitmapData.Width - 5; x++)
					{
						gray = *pCurrent;

						if (gray < highThreshold && gray > lowThreshold)
						{
							if (IsBackgroundGray(pOrig, bitmapData.Stride, new Rectangle(x - 5, 0, 10, linesToCheck), lowThreshold, highThreshold, maxDelta))
								ghostLinesList.Add(x);
						}

						pCurrent++;
					}

					pCurrent = pOrig + (bitmapData.Height - linesToCheck - 1) * bitmapData.Stride + 5;
					for (int x = 5; x < bitmapData.Width - 5; x++)
					{
						gray = *pCurrent;

						if (gray < highThreshold && gray > lowThreshold)
						{
							if (!ghostLinesList.Contains(x))
								if (IsBackgroundGray(pOrig, bitmapData.Stride, new Rectangle(x - 5, bitmapData.Height - linesToCheck - 1, 10, linesToCheck), lowThreshold, highThreshold, maxDelta))
									ghostLinesList.Add(x);
						}

						pCurrent++;
					}
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			return ghostLinesList;
		}
		#endregion

		#region IsBackground32bpp()
		private static unsafe bool IsBackground32bpp(byte* pOrig, int stride, Rectangle clip, byte lowThreshold, byte highThreshold,
			byte maxDelta)
		{
			short clipX = (short)clip.X;
			short clipY = (short)clip.Y;
			short clipRight = (short)clip.Right;
			short clipBottom = (short)(clip.Bottom - 1);
			int x, y;
			int deltaR, deltaG, deltaB;
			byte* pCurrent;

			maxDelta *= 4;

			for (x = clipX; x < clipRight; x++)
			{
				pCurrent = pOrig + clip.Y * stride + x * 4;

				for (y = clipY; y < clipBottom; y++)
				{
					if ((*pCurrent > highThreshold) || (pCurrent[1] > highThreshold) || (pCurrent[2] > highThreshold))
						return false;

					deltaB = *pCurrent - pCurrent[stride];
					deltaG = pCurrent[1] - pCurrent[stride + 1];
					deltaR = pCurrent[2] - pCurrent[stride + 2];

					if (deltaB > maxDelta || deltaB < -maxDelta || deltaG > maxDelta || deltaG < -maxDelta || deltaR > maxDelta || deltaR < -maxDelta)
						return false;

					pCurrent += stride;
				}
			}

			pCurrent = pOrig + clipBottom * stride + clipX * 4;

			for (x = clipX; x < clipRight; x++)
			{
				if ((*pCurrent > highThreshold) || (pCurrent[1] > highThreshold) || (pCurrent[2] > highThreshold))
					return false;

				pCurrent += 4;
			}

			pCurrent = pOrig + clip.Y * stride + clipX * 4;
			byte blackLines = 0;

			for (x = clipX; x < clipRight; x++)
			{
				if ((*pCurrent < lowThreshold) && (pCurrent[1] < lowThreshold) && (pCurrent[2] < lowThreshold))
					blackLines++;

				pCurrent += 4;
			}

			return (blackLines > clip.Width / 3);
		}
		#endregion

		#region IsBackground24bpp()
		private static unsafe bool IsBackground24bpp(byte* pOrig, int stride, Rectangle clip, byte lowThreshold, byte highThreshold,
			byte maxDelta)
		{
			short clipX = (short)clip.X;
			short clipY = (short)clip.Y;
			short clipRight = (short)clip.Right;
			short clipBottom = (short)(clip.Bottom - 1);
			int x, y;
			int deltaR, deltaG, deltaB;
			byte* pCurrent;

			maxDelta *= 3;

			for (x = clipX; x < clipRight; x++)
			{
				pCurrent = pOrig + clip.Y * stride + x * 3;

				for (y = clipY; y < clipBottom; y++)
				{
					if ((*pCurrent > highThreshold) || (pCurrent[1] > highThreshold) || (pCurrent[2] > highThreshold))
						return false;

					deltaB = *pCurrent - pCurrent[stride];
					deltaG = pCurrent[1] - pCurrent[stride + 1];
					deltaR = pCurrent[2] - pCurrent[stride + 2];

					if (deltaB > maxDelta || deltaB < -maxDelta || deltaG > maxDelta || deltaG < -maxDelta || deltaR > maxDelta || deltaR < -maxDelta)
						return false;

					pCurrent += stride;
				}
			}

			pCurrent = pOrig + clipBottom * stride + clipX * 3;

			for (x = clipX; x < clipRight; x++)
			{
				if ((*pCurrent > highThreshold) || (pCurrent[1] > highThreshold) || (pCurrent[2] > highThreshold))
					return false;

				pCurrent += 3;
			}

			pCurrent = pOrig + clip.Y * stride + clipX * 3;
			byte blackLines = 0;

			for (x = clipX; x < clipRight; x++)
			{
				if ((*pCurrent < lowThreshold) && (pCurrent[1] < lowThreshold) && (pCurrent[2] < lowThreshold))
					blackLines++;

				pCurrent += 3;
			}

			return (blackLines > clip.Width / 3);
		}
		#endregion

		#region IsBackgroundGray()
		private static unsafe bool IsBackgroundGray(byte* pOrig, int stride, Rectangle clip, byte lowThreshold, byte highThreshold,
			byte maxDelta)
		{
			short clipX = (short)clip.X;
			short clipY = (short)clip.Y;
			short clipRight = (short)clip.Right;
			short clipBottom = (short)(clip.Bottom - 1);
			int x, y;
			int delta;

			byte* pCurrent;

			for (x = clipX; x < clipRight; x++)
			{
				pCurrent = pOrig + clip.Y * stride + x;

				for (y = clipY; y < clipBottom; y++)
				{
					if (*pCurrent > highThreshold)
						return false;

					delta = *pCurrent - pCurrent[stride];

					if (delta > maxDelta || delta < -maxDelta)
						return false;

					pCurrent += stride;
				}
			}

			pCurrent = pOrig + clipBottom * stride + clipX;

			for (x = clipX; x < clipRight; x++)
			{
				if (*pCurrent > highThreshold)
					return false;

				pCurrent++;
			}

			pCurrent = pOrig + clip.Y * stride + clipX;
			byte blackLines = 0;

			for (x = clipX; x < clipRight; x++)
			{
				if (*pCurrent < lowThreshold)
					blackLines++;

				pCurrent++;
			}

			return (blackLines > clip.Width / 3);
		}
		#endregion


	}
}
