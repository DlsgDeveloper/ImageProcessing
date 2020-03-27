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
	public class EdgeDetector
	{
		// Fields
		static byte _backThres = 215;		// background threshold
		static byte _objectThres = 100;				// object threshold - for dithering objects

		#region enum Operator
		/// <summary>
		/// Laplacian is not that good, sobel is better. Mexican17x17 is the best, but very slow
		/// </summary>
		public enum Operator
		{
			Laplacian446a,
			Sobel,
			MexicanHat5x5,
			MexicanHat7x7,
			MexicanHat17x17
		}
		#endregion

		#region enum RotatingMaskType
		public enum RotatingMaskType
		{
			/// <summary>
			///  1  2  1
			///  0  0  0
			/// -1 -2 -1
			/// </summary>
			Kirsch,
			/// <summary>
			/// 
			/// </summary>
			Jirka
		}
		#endregion


		//PUBLIC METHODS
		#region public methods

		#region BinarizeLaplacian()
		/// <summary>
		/// Generates bitonal array, each bit represents 1 pixel. Pixels with all colors over RGB threshold are skipped.
		/// [ 2,-1, 2]
		///	[-1,-4,-1]
		///	[ 2,-1, 2]
		/// </summary>
		/// <param name="source"></param>
		/// <param name="rThreshold"></param>
		/// <param name="gThreshold"></param>
		/// <param name="bThreshold"></param>
		/// <param name="minDelta">Sum for all RGB</param>
		/// <param name="highlightObjects">If true and pixel and it's 4 neighbours are below object threshold (_objectThres) in all RGB, make it selected.</param>
		/// <returns></returns>
		public static Bitmap BinarizeLaplacian(Bitmap source, int rThreshold, int gThreshold, int bThreshold, int minDelta, bool highlightObjects)
		{
			return BinarizeLaplacian(source, Rectangle.Empty, rThreshold, gThreshold, bThreshold, minDelta, highlightObjects);
		}

		/// <summary>
		/// Generates bitonal image. Pixels with all colors over RGB threshold are skipped.
		/// [ 2,-1, 2]
		///	[-1,-4,-1]
		///	[ 2,-1, 2]
		/// </summary>
		/// <param name="source"></param>
		/// <param name="clip"></param>
		/// <param name="rThreshold"></param>
		/// <param name="gThreshold"></param>
		/// <param name="bThreshold"></param>
		/// <param name="minDelta"></param>
		/// <param name="highlightObjects">If true and pixel and it's 4 neighbours are below object threshold (_objectThres) in all RGB, make it selected.</param>
		/// <returns></returns>
		public static Bitmap BinarizeLaplacian(Bitmap source, Rectangle clip, int rThreshold, int gThreshold, int bThreshold, int minDelta, bool highlightObjects)
		{
			if (source == null)
				return null;

			if (clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, source.Width, source.Height);
			else
				clip = Rectangle.Intersect(clip, new Rectangle(0, 0, source.Width, source.Height));

			Bitmap result = null;

			try
			{
				switch (source.PixelFormat)
				{
					case PixelFormat.Format8bppIndexed:
						result = BinarizeLaplacian8bpp(source, clip, rThreshold, minDelta, highlightObjects, Operator.Laplacian446a);
						break;
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format32bppRgb:
					case PixelFormat.Format32bppArgb:
						result = BinarizeLaplacian24or32bpp(source, clip, rThreshold, gThreshold, bThreshold, minDelta, highlightObjects, Operator.Laplacian446a);
						break;
					case PixelFormat.Format1bppIndexed:
						result = BinarizeLaplacian1bpp(source, clip, highlightObjects);
						break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}

				if (result != null)
				{
					Misc.SetBitmapResolution(result, source.HorizontalResolution, source.VerticalResolution);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("EdgeDetector, Get(): " + ex.Message);
			}

			return result;
		}
		#endregion

		#region Binarize()
		/// <summary>
		/// Generates bitonal image by applying trotating mask.
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="clip"></param>
		/// <param name="maskType"></param>
		/// <returns></returns>
		public static Bitmap Binarize(Bitmap bitmap, Rectangle clip, RotatingMaskType maskType)
		{
			if (bitmap == null)
				throw new IpException(ErrorCode.ErrorNoImageLoaded);

			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

			if ((clip.Width < 3) || (clip.Height < 3))
				throw new IpException(ErrorCode.InvalidParameter);
			
			Bitmap result = BinarizeByRotatingMask(bitmap, clip, maskType);

			return result;
		}
		#endregion

		#region Get()
		/// <summary>
		/// Returns not normalized grayscale image, 255 is maximum edge, 0 is flat area. 
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="clip"></param>
		/// <returns></returns>
		public static Bitmap Get(Bitmap bitmap, Rectangle clip, Operator edgeDetectionOperator)
		{
			if (bitmap == null)
			throw new IpException(ErrorCode.ErrorNoImageLoaded);

			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

			if ((clip.Width < 3) || (clip.Height < 3))
				throw new IpException(ErrorCode.InvalidParameter);

			Bitmap result;

			switch (edgeDetectionOperator)
			{
				case Operator.Sobel: result = Sobel(bitmap, clip); break;
				case Operator.Laplacian446a: result = Laplacian(bitmap, clip); break;
				case Operator.MexicanHat5x5:
				case Operator.MexicanHat7x7:
				case Operator.MexicanHat17x17: 
					result = MexicanHat(bitmap, clip, edgeDetectionOperator); break;
				default: throw new Exception("Unsupported edge detection operator '" + edgeDetectionOperator.ToString() + "'! ");
			}

#if SAVE_RESULTS
			result.Save(Debug.SaveToDir + "02 Edge Detection.png", ImageFormat.Png);
#endif
			return result;
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region GetConvolutionMask()
		private static int[,] GetConvolutionMask(Operator mask)
		{
			int[,] maskArray = null;

			switch (mask)
			{
				case Operator.MexicanHat5x5:
					{
						maskArray = new[,]
						{
							{00,00,-1,00,00},
							{00,-1,-2,-1,00},
							{-1,-2,16,-2,-1},
							{00,-1,-2,-1,00},
							{00,00,-1,00,00}
						};
					} break;

				case Operator.MexicanHat7x7:
					{
						maskArray = new[,]
						{
							{00,00,-1,-1,-1,00,00},
							{00,-2,-3,-3,-3,-2,00},
							{-1,-3,05,05,05,-3,-1},
							{-1,-3,05,16,05,-3,-1},
							{-1,-3,05,05,05,-3,-1},
							{00,-2,-3,-3,-3,-2,00},
							{00,00,-1,-1,-1,00,00}
						};
					} break;

				case Operator.MexicanHat17x17:
					{
						maskArray = new[,]
						{
							{00,00,00,00,00,00,-1,-1,-1,-1,-1,00,00,00,00,00,00},
							{00,00,00,00,-1,-1,-1,-1,-1,-1,-1,-1,-1,00,00,00,00},
							{00,00,-1,-1,-1,-2,-3,-3,-3,-3,-3,-2,-1,-1,-1,00,00},
							{00,00,-1,-1,-2,-3,-3,-3,-3,-3,-3,-3,-2,-1,-1,00,00},
							{00,-1,-1,-2,-3,-3,-3,-2,-3,-2,-3,-3,-3,-2,-1,-1,00},
							{00,-1,-2,-3,-3,-3,00,02,04,02,00,-3,-3,-3,-2,-1,00},
							{-1,-1,-3,-3,-3,00,04,10,12,10,04,00,-3,-3,-3,-1,-1},
							{-1,-1,-3,-3,-2,02,10,18,21,18,10,02,-2,-3,-3,-1,-1},
							{-1,-1,-3,-3,-3,04,12,21,24,21,12,04,-3,-3,-3,-1,-1},
							{-1,-1,-3,-3,-2,02,10,18,21,18,10,02,-2,-3,-3,-1,-1},
							{-1,-1,-3,-3,-3,00,04,10,12,10,04,00,-3,-3,-3,-1,-1},
							{00,-1,-2,-3,-3,-3,00,02,04,02,00,-3,-3,-3,-2,-1,00},
							{00,-1,-1,-2,-3,-3,-3,-2,-3,-2,-3,-3,-3,-2,-1,-1,00},
							{00,00,-1,-1,-2,-3,-3,-3,-3,-3,-3,-3,-2,-1,-1,00,00},
							{00,00,-1,-1,-1,-2,-3,-3,-3,-3,-3,-2,-1,-1,-1,00,00},
							{00,00,00,00,-1,-1,-1,-1,-1,-1,-1,-1,-1,00,00,00,00},
							{00,00,00,00,00,00,-1,-1,-1,-1,-1,00,00,00,00,00,00}
						};
					} break;

				default:
					{
						maskArray = new int[,]{{2,-1,2},
											{-1,-4,-1},
											{2,-1,2}};
					} break;
			}

			return maskArray;
		}
		#endregion

		#region GetConvolutionMaskForImageEdges()
		private static int[,] GetConvolutionMaskForImageEdges(Operator mask)
		{
			int[,] maskArray = null;

			switch (mask)
			{
				default:
					maskArray = new int[,]{{3,-1,3},
											{-1,-3,-1},
											{3,-1,3}};
					break;
			}

			return maskArray;
		}
		#endregion

		#region BinarizeLaplacian1bpp()
		private static Bitmap BinarizeLaplacian1bpp(Bitmap source, Rectangle clip, bool highlightObjects)
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

				int blackPoints;
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				int clipHeight = result.Height - 1;
				int clipWidth = result.Width - 1;
				int x, y;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS;
					byte* pCurrentR;

					for (y = 1; y < clipHeight; y++)
					{
						pCurrentS = pSource + y * sStride;
						pCurrentR = pResult + y * rStride;

						for (x = 1; x < clipWidth; x++)
						{
							blackPoints = 0;

							if ((pCurrentS[(x - 1) / 8 - sStride] & (byte)(0x80 >> ((x - 1) % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[x / 8 - sStride] & (byte)(0x80 >> (x % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[(x + 1) / 8 - sStride] & (byte)(0x80 >> ((x + 1) % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[(x - 1) / 8] & (byte)(0x80 >> ((x - 1) % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[x / 8] & (byte)(0x80 >> (x % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[(x + 1) / 8] & (byte)(0x80 >> ((x + 1) % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[(x - 1) / 8 + sStride] & (byte)(0x80 >> ((x - 1) % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[x / 8 + sStride] & (byte)(0x80 >> (x % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[(x + 1) / 8 + sStride] & (byte)(0x80 >> ((x + 1) % 8))) == 0)
								blackPoints++;

							if (blackPoints > 2)
							{
								//if black area, do dithering
								if (blackPoints < 9)
									pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								else if (highlightObjects && ((x + y) % 2) == 0)
								{
									pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								}
							}
						}
					}

					//top
					y = 0;
					pCurrentS = pSource + y * sStride;
					pCurrentR = pResult + y * rStride;

					for (x = 1; x < clipWidth; x++)
					{
						blackPoints = 0;

						if ((pCurrentS[(x - 1) / 8] & (byte)(0x80 >> ((x - 1) % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[x / 8] & (byte)(0x80 >> (x % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[(x + 1) / 8] & (byte)(0x80 >> ((x + 1) % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[(x - 1) / 8 + sStride] & (byte)(0x80 >> ((x - 1) % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[x / 8 + sStride] & (byte)(0x80 >> (x % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[(x + 1) / 8 + sStride] & (byte)(0x80 >> ((x + 1) % 8))) == 0)
							blackPoints++;

						if (blackPoints > 2)
						{
							//if black area, do dithering
							if (blackPoints < 6)
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
							else if (highlightObjects && ((x + y) % 2) == 0)
							{
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
							}
						}
					}

					//left
					for (y = 1; y < clipHeight; y++)
					{
						pCurrentS = pSource + y * sStride;
						pCurrentR = pResult + y * rStride;

						x = 0;

						blackPoints = 0;

						if ((pCurrentS[x / 8 - sStride] & (byte)(0x80 >> (x % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[(x + 1) / 8 - sStride] & (byte)(0x80 >> ((x + 1) % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[x / 8] & (byte)(0x80 >> (x % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[(x + 1) / 8] & (byte)(0x80 >> ((x + 1) % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[x / 8 + sStride] & (byte)(0x80 >> (x % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[(x + 1) / 8 + sStride] & (byte)(0x80 >> ((x + 1) % 8))) == 0)
							blackPoints++;

						if (blackPoints > 2)
						{
							//if black area, do dithering
							if (blackPoints < 6)
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
							else if (highlightObjects && ((x + y) % 2) == 0)
							{
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
							}
						}
					}

					//right
					for (y = 1; y < clipHeight; y++)
					{
						pCurrentS = pSource + y * sStride;
						pCurrentR = pResult + y * rStride;

						x = clipWidth;
						blackPoints = 0;

						if ((pCurrentS[(x - 1) / 8 - sStride] & (byte)(0x80 >> ((x - 1) % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[x / 8 - sStride] & (byte)(0x80 >> (x % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[(x - 1) / 8] & (byte)(0x80 >> ((x - 1) % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[x / 8] & (byte)(0x80 >> (x % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[(x - 1) / 8 + sStride] & (byte)(0x80 >> ((x - 1) % 8))) == 0)
							blackPoints++;
						if ((pCurrentS[x / 8 + sStride] & (byte)(0x80 >> (x % 8))) == 0)
							blackPoints++;

						if (blackPoints > 2)
						{
							//if black area, do dithering
							if (blackPoints < 6)
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
							else if (highlightObjects && ((x + y) % 2) == 0)
							{
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
							}
						}
					}

					//bottom
					y = clipHeight;
					{
						pCurrentS = pSource + y * sStride;
						pCurrentR = pResult + y * rStride;

						for (x = 1; x < clipWidth; x++)
						{
							blackPoints = 0;

							if ((pCurrentS[(x - 1) / 8 - sStride] & (byte)(0x80 >> ((x - 1) % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[x / 8 - sStride] & (byte)(0x80 >> (x % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[(x + 1) / 8 - sStride] & (byte)(0x80 >> ((x + 1) % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[(x - 1) / 8] & (byte)(0x80 >> ((x - 1) % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[x / 8] & (byte)(0x80 >> (x % 8))) == 0)
								blackPoints++;
							if ((pCurrentS[(x + 1) / 8] & (byte)(0x80 >> ((x + 1) % 8))) == 0)
								blackPoints++;

							if (blackPoints > 2)
							{
								//if black area, do dithering
								if (blackPoints < 6)
									pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								else if (highlightObjects && ((x + y) % 2) == 0)
								{
									pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								}
							}
						}
					}


				}

#if DEBUG
				Console.WriteLine("EdgeDetector " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + (DateTime.Now.Subtract(start)).ToString());
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

		#region BinarizeLaplacian24or32bpp()
		private static Bitmap BinarizeLaplacian24or32bpp(Bitmap source, Rectangle clip, int rThreshold, int gThreshold, int bThreshold,
			int minDelta, bool highlightObjects, Operator edgeOperator)
		{
			Bitmap result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

#if DEBUG
				DateTime start = DateTime.Now;
#endif

				//histogram.Show() ;
				int delta;
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				int clipHeight = result.Height - 1;
				int clipWidth = result.Width - 1;
				int x, y;
				int[,] mask = GetConvolutionMask(edgeOperator);
				int[,] edgeMaskArray = GetConvolutionMaskForImageEdges(edgeOperator);
				int jump = (source.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS;
					byte* pCurrentR;

					for (y = 1; y < clipHeight; y++)
					{
						pCurrentS = pSource + y * sStride + jump;
						pCurrentR = pResult + y * rStride;

						for (x = 1; x < clipWidth; x++)
						{
							if (*pCurrentS < bThreshold || pCurrentS[1] < gThreshold || pCurrentS[2] < rThreshold)
							{
								delta = mask[0, 0] * pCurrentS[-sStride - jump] + mask[0, 1] * pCurrentS[-sStride] + mask[0, 2] * pCurrentS[-sStride + jump] +
									mask[1, 0] * pCurrentS[-jump] + mask[1, 1] * pCurrentS[0] + mask[1, 2] * pCurrentS[jump] +
									mask[2, 0] * pCurrentS[sStride - jump] + mask[2, 1] * pCurrentS[sStride] + mask[2, 2] * pCurrentS[sStride + jump];

								if (delta > minDelta)
								{
									pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								}
								else
								{
									delta = mask[0, 0] * pCurrentS[-sStride - jump + 1] + mask[0, 1] * pCurrentS[-sStride + 1] + mask[0, 2] * pCurrentS[-sStride + jump + 1] +
										mask[1, 0] * pCurrentS[-jump + 1] + mask[1, 1] * pCurrentS[1] + mask[1, 2] * pCurrentS[jump + 1] +
										mask[2, 0] * pCurrentS[sStride - jump + 1] + mask[2, 1] * pCurrentS[sStride + 1] + mask[2, 2] * pCurrentS[sStride + jump + 1];

									if (delta > minDelta)
									{
										pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									}
									else
									{
										delta = mask[0, 0] * pCurrentS[-sStride - jump + 2] + mask[0, 1] * pCurrentS[-sStride + 2] + mask[0, 2] * pCurrentS[-sStride + jump + 2] +
											mask[1, 0] * pCurrentS[-jump + 2] + mask[1, 1] * pCurrentS[2] + mask[1, 2] * pCurrentS[jump + 2] +
											mask[2, 0] * pCurrentS[sStride - jump + 2] + mask[2, 1] * pCurrentS[sStride + 2] + mask[2, 2] * pCurrentS[sStride + jump + 2];

										if (delta > minDelta)
										{
											pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
										}
										else if (highlightObjects)
										{
											if ((pCurrentS[0] < _objectThres || pCurrentS[1] < _objectThres || pCurrentS[2] < _objectThres) &&
												(pCurrentS[-sStride] < _objectThres && pCurrentS[-sStride + 1] < _objectThres && pCurrentS[-sStride + 2] < _objectThres) &&
												(pCurrentS[sStride] < _objectThres && pCurrentS[sStride + 1] < _objectThres && pCurrentS[sStride + 2] < _objectThres) &&
												(pCurrentS[-jump] < _objectThres && pCurrentS[-jump+1] < _objectThres && pCurrentS[-jump+2] < _objectThres) &&
												(pCurrentS[jump] < _objectThres && pCurrentS[jump+1] < _objectThres && pCurrentS[jump+2] < _objectThres))
												pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
										}

									}
								}
							}

							pCurrentS += jump;
						}
					}

					//top
					y = 0;
					pCurrentS = pSource + y * sStride + jump;
					pCurrentR = pResult + y * rStride;

					for (x = 1; x < clipWidth; x++)
					{
						if (*pCurrentS < bThreshold || pCurrentS[1] < gThreshold || pCurrentS[2] < rThreshold)
						{
							delta = edgeMaskArray[1, 0] * pCurrentS[-jump] + edgeMaskArray[1, 1] * pCurrentS[0] + edgeMaskArray[1, 2] * pCurrentS[jump] +
								edgeMaskArray[2, 0] * pCurrentS[sStride - jump] + edgeMaskArray[2, 1] * pCurrentS[sStride] + edgeMaskArray[2, 2] * pCurrentS[sStride + jump];

							if (delta > minDelta)
							{
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								pCurrentS += jump;
							}
							else
							{
								pCurrentS++;
								delta = edgeMaskArray[1, 0] * pCurrentS[-jump] + edgeMaskArray[1, 1] * pCurrentS[0] + edgeMaskArray[1, 2] * pCurrentS[jump] +
									edgeMaskArray[2, 0] * pCurrentS[sStride - jump] + edgeMaskArray[2, 1] * pCurrentS[sStride] + edgeMaskArray[2, 2] * pCurrentS[sStride + jump];

								if (delta > minDelta)
								{
									pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									pCurrentS += 2;
								}
								else
								{
									pCurrentS++;
									delta = edgeMaskArray[1, 0] * pCurrentS[-jump] + edgeMaskArray[1, 1] * pCurrentS[0] + edgeMaskArray[1, 2] * pCurrentS[jump] +
										edgeMaskArray[2, 0] * pCurrentS[sStride - jump] + edgeMaskArray[2, 1] * pCurrentS[sStride] + edgeMaskArray[2, 2] * pCurrentS[sStride + jump];

									if (delta > minDelta)
										pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									else if (highlightObjects)
									{
										if ((pCurrentS[0] < _objectThres || pCurrentS[1] < _objectThres || pCurrentS[2] < _objectThres) &&
											(pCurrentS[sStride] < _objectThres && pCurrentS[sStride + 1] < _objectThres && pCurrentS[sStride + 2] < _objectThres) &&
											(pCurrentS[-jump] < _objectThres && pCurrentS[-2] < _objectThres && pCurrentS[-1] < _objectThres) &&
											(pCurrentS[jump] < _objectThres && pCurrentS[4] < _objectThres && pCurrentS[5] < _objectThres))
											pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									}

									pCurrentS++;
								}
							}
						}
						else
						{
							pCurrentS += jump;
						}
					}

					//bottom
					y = clipHeight;
					pCurrentS = pSource + y * sStride + jump;
					pCurrentR = pResult + y * rStride;

					for (x = 1; x < clipWidth; x++)
					{
						if (*pCurrentS < bThreshold || pCurrentS[1] < gThreshold || pCurrentS[2] < rThreshold)
						{
							delta = edgeMaskArray[0, 0] * pCurrentS[-sStride - jump] + edgeMaskArray[0, 1] * pCurrentS[-sStride] + edgeMaskArray[0, 2] * pCurrentS[-sStride + jump] +
								edgeMaskArray[1, 0] * pCurrentS[-jump] + edgeMaskArray[1, 1] * pCurrentS[0] + edgeMaskArray[1, 2] * pCurrentS[jump];

							if (delta > minDelta)
							{
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								pCurrentS += jump;
							}
							else
							{
								pCurrentS++;
								delta = edgeMaskArray[0, 0] * pCurrentS[-sStride - jump] + edgeMaskArray[0, 1] * pCurrentS[-sStride] + edgeMaskArray[0, 2] * pCurrentS[-sStride + jump] +
									edgeMaskArray[1, 0] * pCurrentS[-jump] + edgeMaskArray[1, 1] * pCurrentS[0] + edgeMaskArray[1, 2] * pCurrentS[jump];

								if (delta > minDelta)
								{
									pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									pCurrentS += 2;
								}
								else
								{
									pCurrentS++;
									delta = edgeMaskArray[0, 0] * pCurrentS[-sStride - jump] + edgeMaskArray[0, 1] * pCurrentS[-sStride] + edgeMaskArray[0, 2] * pCurrentS[-sStride + jump] +
										edgeMaskArray[1, 0] * pCurrentS[-jump] + edgeMaskArray[1, 1] * pCurrentS[0] + edgeMaskArray[1, 2] * pCurrentS[jump];

									if (delta > minDelta)
										pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									else if (highlightObjects)
									{
										if ((pCurrentS[0] < _objectThres || pCurrentS[1] < _objectThres || pCurrentS[2] < _objectThres) &&
											(pCurrentS[-sStride] < _objectThres && pCurrentS[-sStride + 1] < _objectThres && pCurrentS[-sStride + 2] < _objectThres) &&
											(pCurrentS[-jump] < _objectThres && pCurrentS[-2] < _objectThres && pCurrentS[-1] < _objectThres) &&
											(pCurrentS[jump] < _objectThres && pCurrentS[4] < _objectThres && pCurrentS[5] < _objectThres))
											pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									}

									pCurrentS++;
								}
							}
						}
						else
						{
							pCurrentS += jump;
						}
					}

					//left
					x = 0;
					for (y = 1; y < clipHeight; y++)
					{
						pCurrentS = pSource + y * sStride;
						pCurrentR = pResult + y * rStride;

						if (*pCurrentS < bThreshold || pCurrentS[1] < gThreshold || pCurrentS[2] < rThreshold)
						{
							delta = edgeMaskArray[0, 1] * pCurrentS[-sStride] + edgeMaskArray[0, 2] * pCurrentS[-sStride + jump] +
								edgeMaskArray[1, 1] * pCurrentS[0] + edgeMaskArray[1, 2] * pCurrentS[jump] +
								edgeMaskArray[2, 1] * pCurrentS[sStride] + edgeMaskArray[2, 2] * pCurrentS[sStride + jump];

							if (delta > minDelta)
							{
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
							}
							else
							{
								pCurrentS++;
								delta = edgeMaskArray[0, 1] * pCurrentS[-sStride] + edgeMaskArray[0, 2] * pCurrentS[-sStride + jump] +
									edgeMaskArray[1, 1] * pCurrentS[0] + edgeMaskArray[1, 2] * pCurrentS[jump] +
									edgeMaskArray[2, 1] * pCurrentS[sStride] + edgeMaskArray[2, 2] * pCurrentS[sStride + jump];

								if (delta > minDelta)
								{
									pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								}
								else
								{
									pCurrentS++;
									delta = edgeMaskArray[0, 1] * pCurrentS[-sStride] + edgeMaskArray[0, 2] * pCurrentS[-sStride + jump] +
										edgeMaskArray[1, 1] * pCurrentS[0] + edgeMaskArray[1, 2] * pCurrentS[jump] +
										edgeMaskArray[2, 1] * pCurrentS[sStride] + edgeMaskArray[2, 2] * pCurrentS[sStride + jump];

									if (delta > minDelta)
										pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									else if (highlightObjects)
									{
										if ((pCurrentS[0] < _objectThres || pCurrentS[1] < _objectThres || pCurrentS[2] < _objectThres) &&
											(pCurrentS[-sStride] < _objectThres && pCurrentS[-sStride + 1] < _objectThres && pCurrentS[-sStride + 2] < _objectThres) &&
											(pCurrentS[sStride] < _objectThres && pCurrentS[sStride + 1] < _objectThres && pCurrentS[sStride + 2] < _objectThres) &&
											(pCurrentS[jump] < _objectThres && pCurrentS[4] < _objectThres && pCurrentS[5] < _objectThres))
											pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									}

								}
							}
						}
					}

					//right
					x = clipWidth;
					for (y = 1; y < clipHeight; y++)
					{
						pCurrentS = pSource + y * sStride + clipWidth * jump;
						pCurrentR = pResult + y * rStride;

						if (*pCurrentS < bThreshold || pCurrentS[1] < gThreshold || pCurrentS[2] < rThreshold)
						{
							delta = edgeMaskArray[0, 0] * pCurrentS[-sStride - jump] + edgeMaskArray[0, 1] * pCurrentS[-sStride] +
								edgeMaskArray[1, 0] * pCurrentS[-jump] + edgeMaskArray[1, 1] * pCurrentS[0] +
								edgeMaskArray[2, 0] * pCurrentS[sStride - jump] + edgeMaskArray[2, 1] * pCurrentS[sStride];

							if (delta > minDelta)
							{
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
							}
							else
							{
								pCurrentS++;
								delta = edgeMaskArray[0, 0] * pCurrentS[-sStride - jump] + edgeMaskArray[0, 1] * pCurrentS[-sStride] +
									edgeMaskArray[1, 0] * pCurrentS[-jump] + edgeMaskArray[1, 1] * pCurrentS[0] +
									edgeMaskArray[2, 0] * pCurrentS[sStride - jump] + edgeMaskArray[2, 1] * pCurrentS[sStride];

								if (delta > minDelta)
								{
									pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								}
								else
								{
									pCurrentS++;
									delta = edgeMaskArray[0, 0] * pCurrentS[-sStride - jump] + edgeMaskArray[0, 1] * pCurrentS[-sStride] +
										edgeMaskArray[1, 0] * pCurrentS[-jump] + edgeMaskArray[1, 1] * pCurrentS[0] +
										edgeMaskArray[2, 0] * pCurrentS[sStride - jump] + edgeMaskArray[2, 1] * pCurrentS[sStride];

									if (delta > minDelta)
										pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									else if (highlightObjects)
									{
										if ((pCurrentS[0] < _objectThres || pCurrentS[1] < _objectThres || pCurrentS[2] < _objectThres) &&
											(pCurrentS[-sStride] < _objectThres && pCurrentS[-sStride + 1] < _objectThres && pCurrentS[-sStride + 2] < _objectThres) &&
											(pCurrentS[sStride] < _objectThres && pCurrentS[sStride + 1] < _objectThres && pCurrentS[sStride + 2] < _objectThres) &&
											(pCurrentS[-jump] < _objectThres && pCurrentS[-2] < _objectThres && pCurrentS[-1] < _objectThres))
											pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									}
								}
							}
						}
					}
				}

#if DEBUG
				Console.WriteLine("EdgeDetector " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + (DateTime.Now.Subtract(start)).ToString());
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

		#region BinarizeLaplacian8bpp()
		private static Bitmap BinarizeLaplacian8bpp(Bitmap source, Rectangle clip, int threshold, int minDelta,
			bool highlightObjects, Operator mask)
		{
			Bitmap result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				sourceData = source.LockBits(clip, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

#if DEBUG
				DateTime start = DateTime.Now;
#endif

				//histogram.Show() ;
				int delta;
				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				int clipHeight = result.Height - 1;
				int clipWidth = result.Width - 1;
				int x, y;
				int[,] maskArray = GetConvolutionMask(mask); ;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS;
					byte* pCurrentR;

					for (y = 1; y < clipHeight; y++)
					{
						pCurrentS = pSource + y * sStride + 1;
						pCurrentR = pResult + y * rStride;

						for (x = 1; x < clipWidth; x++)
						{
							if (*pCurrentS < threshold)
							{
								delta = maskArray[0, 0] * pCurrentS[-sStride - 1] + maskArray[0, 1] * pCurrentS[-sStride] + maskArray[0, 2] * pCurrentS[-sStride + 1] +
									maskArray[1, 0] * pCurrentS[-1] + maskArray[1, 1] * pCurrentS[0] + maskArray[1, 2] * pCurrentS[+1] +
									maskArray[2, 0] * pCurrentS[sStride - 1] + maskArray[2, 1] * pCurrentS[sStride] + maskArray[2, 2] * pCurrentS[sStride + 1];

								if (delta > minDelta || delta < minDelta)
								{
									pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								}
								else
								{
									if (highlightObjects && x > 6 && x < clipWidth - 7 && y > 6 && y < clipHeight - 7)
									{
										if (*pCurrentS < _objectThres &&
												pCurrentS[-sStride * 3] < _objectThres && pCurrentS[-sStride * 6] < _objectThres &&
												pCurrentS[sStride * 3] < _objectThres && pCurrentS[sStride * 6] < _objectThres &&
												pCurrentS[-3] < _objectThres && pCurrentS[-6] < _objectThres &&
												pCurrentS[3] < _objectThres && pCurrentS[6] < _objectThres)
											pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
									}
								}
							}

							pCurrentS++;
						}
					}

					//top
					y = 0;
					pCurrentS = pSource + y * sStride + 1;
					pCurrentR = pResult + y * rStride;

					for (x = 1; x < clipWidth; x++)
					{
						if (*pCurrentS < threshold)
						{
							delta = -1 * pCurrentS[-1] - 3 * pCurrentS[0] - 1 * pCurrentS[+1] +
								3 * pCurrentS[sStride - 1] - 1 * pCurrentS[sStride] + 3 * pCurrentS[sStride + 1];

							if (delta > minDelta)
							{
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
							}
							else
							{
								if (highlightObjects && x > 6 && x < clipWidth - 7 && y > 6 && y < clipHeight - 7)
								{
									if (*pCurrentS < _objectThres &&
											pCurrentS[sStride * 3] < _objectThres && pCurrentS[sStride * 6] < _objectThres &&
											pCurrentS[-3] < _objectThres && pCurrentS[-6] < _objectThres &&
											pCurrentS[3] < _objectThres && pCurrentS[6] < _objectThres)
										pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								}
							}
						}

						pCurrentS++;
					}

					//left
					x = 0;
					for (y = 1; y < clipHeight; y++)
					{
						pCurrentS = pSource + y * sStride;
						pCurrentR = pResult + y * rStride;

						if (*pCurrentS < threshold)
						{
							delta = -1 * pCurrentS[-sStride] + 3 * pCurrentS[-sStride + 1]
								- 3 * pCurrentS[0] - 1 * pCurrentS[+1]
								- 1 * pCurrentS[sStride] + 3 * pCurrentS[sStride + 1];

							if (delta > minDelta)
							{
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
							}
							else
							{
								if (highlightObjects && x > 6 && x < clipWidth - 7 && y > 6 && y < clipHeight - 7)
								{
									if (*pCurrentS < _objectThres &&
											pCurrentS[-sStride * 3] < _objectThres && pCurrentS[-sStride * 6] < _objectThres &&
											pCurrentS[sStride * 3] < _objectThres && pCurrentS[sStride * 6] < _objectThres &&
											pCurrentS[3] < _objectThres && pCurrentS[6] < _objectThres)
										pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								}
							}
						}
					}

					//bottom
					y = clipHeight;
					pCurrentS = pSource + y * sStride + 1;
					pCurrentR = pResult + y * rStride;

					for (x = 1; x < clipWidth; x++)
					{
						if (*pCurrentS < threshold)
						{
							delta = 3 * pCurrentS[-sStride - 1] - 1 * pCurrentS[-sStride] + 3 * pCurrentS[-sStride + 1]
								- 1 * pCurrentS[-1] - 3 * pCurrentS[0] - 1 * pCurrentS[+1];

							if (delta > minDelta)
							{
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
							}
							else
							{
								if (highlightObjects && x > 6 && x < clipWidth - 7 && y > 6 && y < clipHeight - 7)
								{
									if (*pCurrentS < _objectThres &&
											pCurrentS[-sStride * 3] < _objectThres && pCurrentS[-sStride * 6] < _objectThres &&
											pCurrentS[-3] < _objectThres && pCurrentS[-6] < _objectThres &&
											pCurrentS[3] < _objectThres && pCurrentS[6] < _objectThres)
										pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								}
							}
						}

						pCurrentS++;
					}

					//right
					x = clipWidth;
					for (y = 1; y < clipHeight; y++)
					{
						pCurrentS = pSource + y * sStride + clipWidth;
						pCurrentR = pResult + y * rStride;

						if (*pCurrentS < threshold)
						{
							delta = 3 * pCurrentS[-sStride - 1] - 1 * pCurrentS[-sStride]
								- 1 * pCurrentS[-1] - 3 * pCurrentS[0]
								+ 3 * pCurrentS[sStride - 1] - 1 * pCurrentS[sStride];

							if (delta > minDelta)
							{
								pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
							}
							else
							{
								if (highlightObjects && x > 6 && x < clipWidth - 7 && y > 6 && y < clipHeight - 7)
								{
									if (*pCurrentS < _objectThres &&
											pCurrentS[-sStride * 3] < _objectThres && pCurrentS[-sStride * 6] < _objectThres &&
											pCurrentS[sStride * 3] < _objectThres && pCurrentS[sStride * 6] < _objectThres &&
											pCurrentS[-3] < _objectThres && pCurrentS[-6] < _objectThres)
										pCurrentR[x / 8] |= (byte)(0x80 >> (x % 8));
								}
							}
						}
					}
				}

#if DEBUG
				Console.WriteLine("EdgeDetector " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + (DateTime.Now.Subtract(start)).ToString());
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

		#region BinarizeByRotatingMask()
		private static unsafe Bitmap BinarizeByRotatingMask(Bitmap bitmap, Rectangle clip, RotatingMaskType maskType)
		{
			byte backThresholdR;
			byte backThresholdG;
			byte backThresholdB;
			byte textThresholdR;
			byte textThresholdG;
			byte textThresholdB;
			
			try
			{
				Histogram histogram = new Histogram(bitmap, clip);
				//histogram.Show();

				Color c = histogram.GetOtsuBackground();
				backThresholdR = (byte) (c.R);
				backThresholdG = (byte) (c.G);
				backThresholdB = (byte) (c.B);

				textThresholdR = (byte)(histogram.ThresholdR - 20);
				textThresholdG = (byte)(histogram.ThresholdG - 20);
				textThresholdB = (byte)(histogram.ThresholdB - 20);
			}
			catch
			{
				backThresholdR = _backThres;
				backThresholdG = _backThres;
				backThresholdB = _backThres;
				textThresholdR = _objectThres;
				textThresholdG = _objectThres;
				textThresholdB = _objectThres;
			}

			byte backThresholdGray = Histogram.ToGray(backThresholdR, backThresholdG, backThresholdB);
			byte textThresholdGray = Histogram.ToGray(textThresholdR, textThresholdG, textThresholdB);
			
			Bitmap result = null;
			BitmapData bitmapData = null;
			BitmapData resultData = null;
			try
			{
				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat);
				result = new Bitmap(bitmapData.Width, bitmapData.Height, PixelFormat.Format1bppIndexed);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				
				int widthMinus1 = bitmapData.Width - 1;
				int heightMinus1 = bitmapData.Height - 1;
				int width = resultData.Width;
				int height = resultData.Height;

				int stride = bitmapData.Stride;
				int resultStride = resultData.Stride;
				int threshold = 8;
				int x, y;
				int gray;

				byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();

				switch (bitmap.PixelFormat)
				{
					case PixelFormat.Format24bppRgb:
						{
							if (maskType == RotatingMaskType.Jirka)
							{
								for (y = 1; y < heightMinus1; y++)
									for (x = 1; x < widthMinus1; x++)
									{
										if ((*(pOrig + y * stride + x * 3) < textThresholdB) || (*(pOrig + y * stride + x * 3 + 1) < textThresholdG) || (*(pOrig + y * stride + x * 3 + 2) < textThresholdR))
											pResult[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 0x07));
										else
										{
											gray = Histogram.ToGray(*(pOrig + y * stride + x * 3 + 2), *(pOrig + y * stride + x * 3 + 1), *(pOrig + y * stride + x * 3));

											//if (((*(pOrig + y * stride + x * 3) < backThresholdB) || (*(pOrig + y * stride + x * 3 + 1) < backThresholdG) || (*(pOrig + y * stride + x * 3 + 2) < backThresholdR)) && Jirka24bppEdge(pOrig + y * stride + x * 3, stride, threshold))
											if (gray < backThresholdGray && Jirka24bppEdge(pOrig + y * stride + x * 3, stride, threshold))
												pResult[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 0x07));
										}
									}
							}
							else
							{
								for (y = 1; y < heightMinus1; y++)
									for (x = 1; x < widthMinus1; x++)
									{
										if ((*(pOrig + y * stride + x * 3) < textThresholdB) || (*(pOrig + y * stride + x * 3 + 1) < textThresholdG) || (*(pOrig + y * stride + x * 3 + 2) < textThresholdR))
											pResult[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 0x07));
										else if ((*(pOrig + y * stride + x * 3) < backThresholdB) && (*(pOrig + y * stride + x * 3 + 1) < backThresholdG) && (*(pOrig + y * stride + x * 3 + 2) < backThresholdR) && Kirsch24bppEdge(pOrig + y * stride + x * 3, stride, threshold))
											pResult[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 0x07));
									}
							}
						} break;
					
					case PixelFormat.Format8bppIndexed:
						{
							if (maskType == RotatingMaskType.Jirka)
							{
								for (y = 1; y < heightMinus1; y++)
									for (x = 1; x < widthMinus1; x++)
									{
										if (*(pOrig + y * stride + x) < textThresholdR)
											pResult[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 0x07));
										else if ((*(pOrig + y * stride + x) < backThresholdB) && Jirka8bppEdge(pOrig + y * stride + x, stride, threshold))
											pResult[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 0x07));
									}
							}
							else
							{
								for (y = 1; y < heightMinus1; y++)
									for (x = 1; x < widthMinus1; x++)
									{
										if (*(pOrig + y * stride + x) < textThresholdR)
											pResult[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 0x07));
										else if ((*(pOrig + y * stride + x) < backThresholdB) && Kirsch8bppEdge(pOrig + y * stride + x, stride, threshold))
											pResult[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 0x07));
									}
							}
						} break;

					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
						{
							if (maskType == RotatingMaskType.Jirka)
							{
								for (y = 1; y < heightMinus1; y++)
									for (x = 1; x < widthMinus1; x++)
									{
										if ((*(pOrig + y * stride + x * 4) < textThresholdB) || (*(pOrig + y * stride + x * 4 + 1) < textThresholdG) || (*(pOrig + y * stride + x * 4 + 2) < textThresholdR))
											pResult[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 0x07));
										else if ((*(pOrig + y * stride + x * 4) < backThresholdB) && (*(pOrig + y * stride + x * 4 + 1) < backThresholdG) && (*(pOrig + y * stride + x * 4 + 2) < backThresholdR) && Jirka32bppEdge(pOrig + y * stride + x * 4, stride, threshold))
											pResult[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 0x07));
									}
							}
							else
							{
								for (y = 1; y < heightMinus1; y++)
									for (x = 1; x < widthMinus1; x++)
									{
										if ((*(pOrig + y * stride + x * 4) < textThresholdB) || (*(pOrig + y * stride + x * 4 + 1) < textThresholdG) || (*(pOrig + y * stride + x * 4 + 2) < textThresholdR))
											pResult[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 0x07));
										else if ((*(pOrig + y * stride + x * 4) < _backThres) && (*(pOrig + y * stride + x * 4 + 1) < _backThres) && (*(pOrig + y * stride + x * 4 + 2) < _backThres) && Kirsch32bppEdge(pOrig + y * stride + x * 4, stride, threshold))
											pResult[y * resultStride + x / 8] |= (byte)(0x80 >> (x & 0x07));
									}
							}
						} break;

					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}

				//top line is identical to second line
				for (x = 0; x < resultStride; x++)
					pResult[x] = pResult[resultStride + x];
				//bottom line is identical to second bottom line
				for (x = 0; x < resultStride; x++)
					pResult[(height - 1) * resultStride + x] = pResult[(height - 2) * resultStride + x];
				//left columnh is identical to second column
				for (y = 0; y < height; y++)
					pResult[y * resultStride] |= (byte)((pResult[y * resultStride] & 0x40) << 1);
				//right columnh is identical to second right column
				//for (y = 0; y < height; y++)
					//pResult[y * resultStride + (width - 1) / 8] |= (byte)((pResult[y * resultStride + (width - 2) / 8] & (0x80 >> ((width - 2) & 0x07))) >> 1);
				if (((width - 1) % 8) == 0)
					for (y = 0; y < height; y++)
						pResult[y * resultStride + (width - 1) / 8] |= (byte)((pResult[y * resultStride + (width - 2) / 8] & 0x01) << 7);
				else
					for (y = 0; y < height; y++)
						pResult[y * resultStride + (width - 1) / 8] |= (byte)((pResult[y * resultStride + (width - 2) / 8] & (0x80 >> ((width - 2) & 0x07))) >> 1);
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}

			return result;
		}
		#endregion

		#region Jirka24bppEdge()
		/// <summary>
		/// operator: [[1,2,1],[-1,-2,-1],[-1,-2,-1]] rotating
		/// </summary>
		/// <param name="scan0"></param>
		/// <param name="stride"></param>
		/// <param name="threshold"></param>
		/// <returns></returns>
		private static unsafe bool Jirka24bppEdge(byte* s0, int strd, int trshold)
		{
			
			//top
			if ((s0[-strd - 3] - s0[-3] > trshold) && (s0[-strd] - s0[0] > trshold) && (s0[-strd + 3] - s0[+3] > trshold) && (s0[-3] - s0[strd - 3] > trshold) && (s0[0] - s0[strd] > trshold) && (s0[+3] - s0[strd + 3] > trshold) && (s0[-strd - 3] - s0[0] > 0) && (s0[-strd + 3] - s0[0] > 0) && (s0[- 3] - s0[strd] > 0) && (s0[+ 3] - s0[strd] > 0))
				return true;
			if ((s0[-strd - 2] - s0[-2] > trshold) && (s0[-strd + 1] - s0[1] > trshold) && (s0[-strd + 4] - s0[+4] > trshold) && (s0[-2] - s0[strd - 2] > trshold) && (s0[+1] - s0[strd + 1] > trshold) && (s0[+4] - s0[strd + 4] > trshold) && (s0[-strd - 2] - s0[1] > 0) && (s0[-strd + 4] - s0[1] > 0) && (s0[-2] - s0[strd+1] > 0) && (s0[+4] - s0[strd+1] > 0))
				return true;
			if ((s0[-strd - 1] - s0[-1] > trshold) && (s0[-strd + 2] - s0[2] > trshold) && (s0[-strd + 5] - s0[+5] > trshold) && (s0[-1] - s0[strd - 1] > trshold) && (s0[+2] - s0[strd + 2] > trshold) && (s0[+5] - s0[strd + 5] > trshold) && (s0[-strd - 1] - s0[2] > 0) && (s0[-strd + 5] - s0[2] > 0) && (s0[-1] - s0[strd+2] > 0) && (s0[+5] - s0[strd+2] > 0))
				return true;

			//right
			if ((s0[-strd + 3] - s0[-strd] > trshold) && (s0[3] - s0[0] > trshold) && (s0[strd + 3] - s0[strd] > trshold) && (s0[-strd] - s0[-strd - 3] > trshold) && (s0[0] - s0[-3] > trshold) && (s0[strd] - s0[strd - 3] > trshold))
				return true;
			if ((s0[-strd + 4] - s0[-strd + 1] > trshold) && (s0[4] - s0[1] > trshold) && (s0[strd + 4] - s0[strd + 1] > trshold) && (s0[-strd + 1] - s0[-strd - 2] > trshold) && (s0[1] - s0[-2] > trshold) && (s0[strd + 1] - s0[strd - 2] > trshold))
				return true;
			if ((s0[-strd + 5] - s0[-strd + 2] > trshold) && (s0[5] - s0[2] > trshold) && (s0[strd + 5] - s0[strd + 2] > trshold) && (s0[-strd + 2] - s0[-strd - 4] > trshold) && (s0[2] - s0[-1] > trshold) && (s0[strd + 2] - s0[strd - 1] > trshold))
				return true;

			//bottom
			if ((s0[strd - 3] - s0[-3] > trshold) && (s0[strd] - s0[0] > trshold) && (s0[strd + 3] - s0[+3] > trshold) && (s0[-3] - s0[-strd - 3] > trshold) && (s0[0] - s0[-strd] > trshold) && (s0[+3] - s0[-strd + 3] > trshold) && (s0[strd - 3] - s0[0] > 0) && (s0[strd + 3] - s0[0] > 0) && (s0[- 3] - s0[-strd] > 0) && (s0[+ 3] - s0[-strd] > 0))
				return true;
			if ((s0[strd - 2] - s0[-2] > trshold) && (s0[strd + 1] - s0[1] > trshold) && (s0[strd + 4] - s0[+4] > trshold) && (s0[-2] - s0[-strd - 2] > trshold) && (s0[+1] - s0[-strd + 1] > trshold) && (s0[+4] - s0[-strd + 4] > trshold) && (s0[strd - 2] - s0[1] > 0) && (s0[strd + 4] - s0[1] > 0) && (s0[-2] - s0[-strd+1] > 0) && (s0[+4] - s0[-strd+1] > 0))
				return true;
			if ((s0[strd - 1] - s0[-1] > trshold) && (s0[strd + 2] - s0[2] > trshold) && (s0[strd + 5] - s0[+5] > trshold) && (s0[-1] - s0[-strd - 1] > trshold) && (s0[+2] - s0[-strd + 2] > trshold) && (s0[+5] - s0[-strd + 5] > trshold) && (s0[strd - 1] - s0[2] > 0) && (s0[strd + 5] - s0[2] > 0) && (s0[-1] - s0[-strd+2] > 0) && (s0[+5] - s0[-strd+2] > 0))
				return true;

			//left
			if ((s0[-strd - 3] - s0[-strd] > trshold) && (s0[-3] - s0[0] > trshold) && (s0[strd - 3] - s0[strd] > trshold) && (s0[-strd] - s0[-strd + 3] > trshold) && (s0[0] - s0[+3] > trshold) && (s0[strd] - s0[strd + 3] > trshold))
				return true;
			if ((s0[-strd - 2] - s0[-strd + 1] > trshold) && (s0[-2] - s0[1] > trshold) && (s0[strd - 2] - s0[strd + 1] > trshold) && (s0[-strd +1] - s0[-strd + 4] > trshold) && (s0[1] - s0[+4] > trshold) && (s0[strd +1] - s0[strd + 4] > trshold))
				return true;
			if ((s0[-strd - 1] - s0[-strd + 2] > trshold) && (s0[-1] - s0[2] > trshold) && (s0[strd - 1] - s0[strd + 2] > trshold) && (s0[-strd +2] - s0[-strd + 5] > trshold) && (s0[2] - s0[+5] > trshold) && (s0[strd +2] - s0[strd + 5] > trshold))
				return true;

			//upper left
			if ((s0[-strd - 3] - s0[0] > trshold) && (s0[-3] - s0[strd] > trshold) && (s0[0] - s0[strd + 3] > trshold) && (s0[-strd] - s0[3] > trshold) && (s0[-strd + 3] - s0[3] > 0) && (s0[strd - 3] - s0[strd] > 0))
				return true;
			if ((s0[-strd - 2] - s0[1] > trshold) && (s0[-2] - s0[strd + 1] > trshold) && (s0[1] - s0[strd + 4] > trshold) && (s0[-strd + 1] - s0[4] > trshold) && (s0[-strd + 4] - s0[4] > 0) && (s0[strd - 2] - s0[strd+1] > 0))
				return true;
			if ((s0[-strd - 1] - s0[2] > trshold) && (s0[-1] - s0[strd + 2] > trshold) && (s0[2] - s0[strd + 5] > trshold) && (s0[-strd + 2] - s0[5] > trshold) && (s0[-strd + 5] - s0[5] > 0) && (s0[strd - 1] - s0[strd+2] > 0))
				return true;

			//upper right
			if ((s0[-strd + 3] - s0[0] > trshold) && (s0[-strd] - s0[-3] > trshold) && (s0[0] - s0[strd - 3] > trshold) && (s0[3] - s0[strd] > trshold) && (s0[-strd - 3] - s0[-3] > 0) && (s0[strd + 3] - s0[strd] > 0))
				return true;
			if ((s0[-strd + 4] - s0[1] > trshold) && (s0[-strd + 1] - s0[-2] > trshold) && (s0[1] - s0[strd - 2] > trshold) && (s0[4] - s0[strd + 1] > trshold) && (s0[-strd - 2] - s0[-2] > 0) && (s0[strd + 4] - s0[strd+1] > 0))
				return true;
			if ((s0[-strd + 5] - s0[2] > trshold) && (s0[-strd + 2] - s0[-1] > trshold) && (s0[2] - s0[strd - 1] > trshold) && (s0[5] - s0[strd + 2] > trshold) && (s0[-strd - 1] - s0[-1] > 0) && (s0[strd + 5] - s0[strd+2] > 0))
				return true;

			//lower left
			if ((s0[strd - 3] - s0[0] > trshold) && (s0[-3] - s0[-strd] > trshold) && (s0[0] - s0[strd + 3] > trshold) && (s0[strd] - s0[3] > trshold) && (s0[-strd - 3] - s0[-strd] > 0) && (s0[strd + 3] - s0[3] > 0))
				return true;
			if ((s0[strd - 2] - s0[1] > trshold) && (s0[-2] - s0[-strd + 1] > trshold) && (s0[1] - s0[strd + 4] > trshold) && (s0[strd + 1] - s0[4] > trshold) && (s0[-strd - 2] - s0[-strd+1] > 0) && (s0[strd + 4] - s0[4] > 0))
				return true;
			if ((s0[strd - 1] - s0[2] > trshold) && (s0[-1] - s0[-strd + 2] > trshold) && (s0[2] - s0[strd + 5] > trshold) && (s0[strd + 2] - s0[5] > trshold) && (s0[-strd - 1] - s0[-strd+2] > 0) && (s0[strd + 5] - s0[5] > 0))
				return true;

			//lower right
			if ((s0[strd + 3] - s0[0] > trshold) && (s0[strd] - s0[-3] > trshold) && (s0[strd + 3] - s0[0] > trshold) && (s0[3] - s0[-strd] > trshold) && (s0[strd - 3] - s0[-3] > 0) && (s0[-strd + 3] - s0[strd] > 0))
				return true;
			if ((s0[strd + 4] - s0[1] > trshold) && (s0[strd + 1] - s0[-2] > trshold) && (s0[strd + 4] - s0[1] > trshold) && (s0[4] - s0[-strd + 1] > trshold) && (s0[strd - 2] - s0[-2] > 0) && (s0[-strd + 4] - s0[strd+1] > 0))
				return true;
			if ((s0[strd + 5] - s0[2] > trshold) && (s0[strd + 2] - s0[-1] > trshold) && (s0[strd + 5] - s0[2] > trshold) && (s0[5] - s0[-strd + 2] > trshold) && (s0[strd - 1] - s0[-1] > 0) && (s0[-strd + 5] - s0[strd+2] > 0))
				return true;

			return false;
			

			//return ((((((((s0[-strd - 3] - s0[-3]) > trshold) && ((s0[-strd] - s0[0]) > trshold)) && (((s0[-strd + 3] - s0[3]) > trshold) && ((s0[-3] - s0[strd - 3]) > trshold))) && ((((s0[0] - s0[strd]) > trshold) && ((s0[3] - s0[strd + 3]) > trshold)) && (((s0[-strd - 3] - s0[0]) > 0) && ((s0[-strd + 3] - s0[0]) > 0)))) && ((s0[-3] - s0[strd]) > 0)) && ((s0[3] - s0[strd]) > 0)) || ((((((((s0[-strd - 2] - s0[-2]) > trshold) && ((s0[-strd + 1] - s0[1]) > trshold)) && (((s0[-strd + 4] - s0[4]) > trshold) && ((s0[-2] - s0[strd - 2]) > trshold))) && ((((s0[1] - s0[strd + 1]) > trshold) && ((s0[4] - s0[strd + 4]) > trshold)) && (((s0[-strd - 2] - s0[1]) > 0) && ((s0[-strd + 4] - s0[1]) > 0)))) && ((s0[-2] - s0[strd + 1]) > 0)) && ((s0[4] - s0[strd + 1]) > 0)) || ((((((((s0[-strd - 1] - s0[-1]) > trshold) && ((s0[-strd + 2] - s0[2]) > trshold)) && (((s0[-strd + 5] - s0[5]) > trshold) && ((s0[-1] - s0[strd - 1]) > trshold))) && ((((s0[2] - s0[strd + 2]) > trshold) && ((s0[5] - s0[strd + 5]) > trshold)) && (((s0[-strd - 1] - s0[2]) > 0) && ((s0[-strd + 5] - s0[2]) > 0)))) && ((s0[-1] - s0[strd + 2]) > 0)) && ((s0[5] - s0[strd + 2]) > 0)) || (((((((s0[-strd + 3] - s0[-strd]) > trshold) && ((s0[3] - s0[0]) > trshold)) && (((s0[strd + 3] - s0[strd]) > trshold) && ((s0[-strd] - s0[-strd - 3]) > trshold))) && ((s0[0] - s0[-3]) > trshold)) && ((s0[strd] - s0[strd - 3]) > trshold)) || (((((((s0[-strd + 4] - s0[-strd + 1]) > trshold) && ((s0[4] - s0[1]) > trshold)) && (((s0[strd + 4] - s0[strd + 1]) > trshold) && ((s0[-strd + 1] - s0[-strd - 2]) > trshold))) && ((s0[1] - s0[-2]) > trshold)) && ((s0[strd + 1] - s0[strd - 2]) > trshold)) || (((((((s0[-strd + 5] - s0[-strd + 2]) > trshold) && ((s0[5] - s0[2]) > trshold)) && (((s0[strd + 5] - s0[strd + 2]) > trshold) && ((s0[-strd + 2] - s0[-strd - 4]) > trshold))) && ((s0[2] - s0[-1]) > trshold)) && ((s0[strd + 2] - s0[strd - 1]) > trshold)) || ((((((((s0[strd - 3] - s0[-3]) > trshold) && ((s0[strd] - s0[0]) > trshold)) && (((s0[strd + 3] - s0[3]) > trshold) && ((s0[-3] - s0[-strd - 3]) > trshold))) && ((((s0[0] - s0[-strd]) > trshold) && ((s0[3] - s0[-strd + 3]) > trshold)) && (((s0[strd - 3] - s0[0]) > 0) && ((s0[strd + 3] - s0[0]) > 0)))) && ((s0[-3] - s0[strd]) > 0)) && ((s0[3] - s0[strd]) > 0)) || ((((((((s0[strd - 2] - s0[-2]) > trshold) && ((s0[strd + 1] - s0[1]) > trshold)) && (((s0[strd + 4] - s0[4]) > trshold) && ((s0[-2] - s0[-strd - 2]) > trshold))) && ((((s0[1] - s0[-strd + 1]) > trshold) && ((s0[4] - s0[-strd + 4]) > trshold)) && (((s0[strd - 2] - s0[1]) > 0) && ((s0[strd + 4] - s0[1]) > 0)))) && ((s0[-2] - s0[strd + 1]) > 0)) && ((s0[4] - s0[strd + 1]) > 0)) || ((((((((s0[strd - 1] - s0[-1]) > trshold) && ((s0[strd + 2] - s0[2]) > trshold)) && (((s0[strd + 5] - s0[5]) > trshold) && ((s0[-1] - s0[-strd - 1]) > trshold))) && ((((s0[2] - s0[-strd + 2]) > trshold) && ((s0[5] - s0[-strd + 5]) > trshold)) && (((s0[strd - 1] - s0[2]) > 0) && ((s0[strd + 5] - s0[2]) > 0)))) && ((s0[-1] - s0[strd + 2]) > 0)) && ((s0[5] - s0[strd + 2]) > 0)) || (((((((s0[-strd - 3] - s0[-strd]) > trshold) && ((s0[-3] - s0[0]) > trshold)) && (((s0[strd - 3] - s0[strd]) > trshold) && ((s0[-strd] - s0[-strd + 3]) > trshold))) && ((s0[0] - s0[3]) > trshold)) && ((s0[strd] - s0[strd + 3]) > trshold)) || (((((((s0[-strd - 2] - s0[-strd + 1]) > trshold) && ((s0[-2] - s0[1]) > trshold)) && (((s0[strd - 2] - s0[strd + 1]) > trshold) && ((s0[-strd + 1] - s0[-strd + 4]) > trshold))) && ((s0[1] - s0[4]) > trshold)) && ((s0[strd + 1] - s0[strd + 4]) > trshold)) || (((((((s0[-strd - 1] - s0[-strd + 2]) > trshold) && ((s0[-1] - s0[2]) > trshold)) && (((s0[strd - 1] - s0[strd + 2]) > trshold) && ((s0[-strd + 2] - s0[-strd + 5]) > trshold))) && ((s0[2] - s0[5]) > trshold)) && ((s0[strd + 2] - s0[strd + 5]) > trshold)) || (((((((s0[-strd - 3] - s0[0]) > trshold) && ((s0[-3] - s0[strd]) > trshold)) && (((s0[0] - s0[strd + 3]) > trshold) && ((s0[-strd] - s0[3]) > trshold))) && ((s0[-strd + 3] - s0[3]) > 0)) && ((s0[strd - 3] - s0[strd]) > 0)) || (((((((s0[-strd - 2] - s0[1]) > trshold) && ((s0[-2] - s0[strd + 1]) > trshold)) && (((s0[1] - s0[strd + 4]) > trshold) && ((s0[-strd + 1] - s0[4]) > trshold))) && ((s0[-strd + 4] - s0[4]) > 0)) && ((s0[strd - 2] - s0[strd + 1]) > 0)) || (((((((s0[-strd - 1] - s0[2]) > trshold) && ((s0[-1] - s0[strd + 2]) > trshold)) && (((s0[2] - s0[strd + 5]) > trshold) && ((s0[-strd + 2] - s0[5]) > trshold))) && ((s0[-strd + 5] - s0[5]) > 0)) && ((s0[strd - 1] - s0[strd + 2]) > 0)) || (((((((s0[-strd + 3] - s0[0]) > trshold) && ((s0[-strd] - s0[-3]) > trshold)) && (((s0[0] - s0[strd - 3]) > trshold) && ((s0[3] - s0[strd]) > trshold))) && ((s0[-strd - 3] - s0[-3]) > 0)) && ((s0[strd + 3] - s0[strd]) > 0)) || (((((((s0[-strd + 4] - s0[1]) > trshold) && ((s0[-strd + 1] - s0[-2]) > trshold)) && (((s0[1] - s0[strd - 2]) > trshold) && ((s0[4] - s0[strd + 1]) > trshold))) && ((s0[-strd - 2] - s0[-2]) > 0)) && ((s0[strd + 4] - s0[strd + 1]) > 0)) || (((((((s0[-strd + 5] - s0[2]) > trshold) && ((s0[-strd + 2] - s0[-1]) > trshold)) && (((s0[2] - s0[strd - 1]) > trshold) && ((s0[5] - s0[strd + 2]) > trshold))) && ((s0[-strd - 1] - s0[-1]) > 0)) && ((s0[strd + 5] - s0[strd + 2]) > 0)) || (((((((s0[strd - 3] - s0[0]) > trshold) && ((s0[-3] - s0[-strd]) > trshold)) && (((s0[0] - s0[strd + 3]) > trshold) && ((s0[strd] - s0[3]) > trshold))) && ((s0[-strd - 3] - s0[-strd]) > 0)) && ((s0[strd + 3] - s0[3]) > 0)) || (((((((s0[strd - 2] - s0[1]) > trshold) && ((s0[-2] - s0[-strd + 1]) > trshold)) && (((s0[1] - s0[strd + 4]) > trshold) && ((s0[strd + 1] - s0[4]) > trshold))) && ((s0[-strd - 2] - s0[-strd + 1]) > 0)) && ((s0[strd + 4] - s0[4]) > 0)) || (((((((s0[strd - 1] - s0[2]) > trshold) && ((s0[-1] - s0[-strd + 2]) > trshold)) && (((s0[2] - s0[strd + 5]) > trshold) && ((s0[strd + 2] - s0[5]) > trshold))) && ((s0[-strd - 1] - s0[-strd + 2]) > 0)) && ((s0[strd + 5] - s0[5]) > 0)) || (((((((s0[strd + 3] - s0[0]) > trshold) && ((s0[strd] - s0[-3]) > trshold)) && (((s0[strd + 3] - s0[0]) > trshold) && ((s0[3] - s0[-strd]) > trshold))) && ((s0[strd - 3] - s0[-3]) > 0)) && ((s0[-strd + 3] - s0[strd]) > 0)) || (((((((s0[strd + 4] - s0[1]) > trshold) && ((s0[strd + 1] - s0[-2]) > trshold)) && (((s0[strd + 4] - s0[1]) > trshold) && ((s0[4] - s0[-strd + 1]) > trshold))) && ((s0[strd - 2] - s0[-2]) > 0)) && ((s0[-strd + 4] - s0[strd + 1]) > 0)) || ((((((s0[strd + 5] - s0[2]) > trshold) && ((s0[strd + 2] - s0[-1]) > trshold)) && (((s0[strd + 5] - s0[2]) > trshold) && ((s0[5] - s0[-strd + 2]) > trshold))) && ((s0[strd - 1] - s0[-1]) > 0)) && ((s0[-strd + 5] - s0[strd + 2]) > 0)))))))))))))))))))))))));
		}
		#endregion

		#region Jirka32bppEdge()
		/// <summary>
		/// operator: [[1,2,1],[-1,-2,-1],[-1,-2,-1]] rotating
		/// </summary>
		/// <param name="scan0"></param>
		/// <param name="stride"></param>
		/// <param name="threshold"></param>
		/// <returns></returns>
		private unsafe static bool Jirka32bppEdge(byte* s0, int strd, int trshold)
		{
			
			//top
			if ((s0[-strd - 4] - s0[-4] > trshold) && (s0[-strd] - s0[0] > trshold) && (s0[-strd + 4] - s0[+4] > trshold) && (s0[-4] - s0[strd - 4] > trshold) && (s0[0] - s0[strd] > trshold) && (s0[+4] - s0[strd + 4] > trshold) && (s0[-strd - 4] - s0[0] > 0) && (s0[-strd + 4] - s0[0] > 0) && (s0[-4] - s0[strd] > 0) && (s0[+4] - s0[strd] > 0))
				return true;
			if ((s0[-strd - 3] - s0[-3] > trshold) && (s0[-strd + 1] - s0[1] > trshold) && (s0[-strd + 5] - s0[+5] > trshold) && (s0[-3] - s0[strd - 3] > trshold) && (s0[+1] - s0[strd + 1] > trshold) && (s0[+5] - s0[strd + 5] > trshold) && (s0[-strd - 3] - s0[1] > 0) && (s0[-strd + 5] - s0[1] > 0) && (s0[-3] - s0[strd + 1] > 0) && (s0[+5] - s0[strd + 1] > 0))
				return true;
			if ((s0[-strd - 2] - s0[-2] > trshold) && (s0[-strd + 2] - s0[2] > trshold) && (s0[-strd + 6] - s0[+6] > trshold) && (s0[-2] - s0[strd - 2] > trshold) && (s0[+2] - s0[strd + 2] > trshold) && (s0[+6] - s0[strd + 6] > trshold) && (s0[-strd - 2] - s0[2] > 0) && (s0[-strd + 6] - s0[2] > 0) && (s0[-2] - s0[strd + 2] > 0) && (s0[+6] - s0[strd + 2] > 0))
				return true;

			//right
			if ((s0[-strd + 4] - s0[-strd] > trshold) && (s0[4] - s0[0] > trshold) && (s0[strd + 4] - s0[strd] > trshold) && (s0[-strd] - s0[-strd - 4] > trshold) && (s0[0] - s0[-4] > trshold) && (s0[strd] - s0[strd - 4] > trshold))
				return true;
			if ((s0[-strd + 5] - s0[-strd + 1] > trshold) && (s0[5] - s0[1] > trshold) && (s0[strd + 5] - s0[strd + 1] > trshold) && (s0[-strd + 1] - s0[-strd - 3] > trshold) && (s0[1] - s0[-4] > trshold) && (s0[strd + 1] - s0[strd - 3] > trshold))
				return true;
			if ((s0[-strd + 6] - s0[-strd + 2] > trshold) && (s0[6] - s0[2] > trshold) && (s0[strd + 6] - s0[strd + 2] > trshold) && (s0[-strd + 2] - s0[-strd - 2] > trshold) && (s0[2] - s0[-2] > trshold) && (s0[strd + 2] - s0[strd - 2] > trshold))
				return true;

			//bottom
			if ((s0[strd - 4] - s0[-4] > trshold) && (s0[strd] - s0[0] > trshold) && (s0[strd + 4] - s0[+4] > trshold) && (s0[-4] - s0[-strd - 4] > trshold) && (s0[0] - s0[-strd] > trshold) && (s0[+4] - s0[-strd + 4] > trshold) && (s0[strd - 4] - s0[0] > 0) && (s0[strd + 4] - s0[0] > 0) && (s0[-4] - s0[-strd] > 0) && (s0[+4] - s0[-strd] > 0))
				return true;
			if ((s0[strd - 3] - s0[-3] > trshold) && (s0[strd + 1] - s0[1] > trshold) && (s0[strd + 5] - s0[+5] > trshold) && (s0[-3] - s0[-strd - 3] > trshold) && (s0[+1] - s0[-strd + 1] > trshold) && (s0[+5] - s0[-strd + 5] > trshold) && (s0[strd - 3] - s0[1] > 0) && (s0[strd + 5] - s0[1] > 0) && (s0[-3] - s0[-strd + 1] > 0) && (s0[+5] - s0[-strd + 1] > 0))
				return true;
			if ((s0[strd - 2] - s0[-2] > trshold) && (s0[strd + 2] - s0[2] > trshold) && (s0[strd + 6] - s0[+6] > trshold) && (s0[-2] - s0[-strd - 2] > trshold) && (s0[+2] - s0[-strd + 2] > trshold) && (s0[+6] - s0[-strd + 6] > trshold) && (s0[strd - 2] - s0[2] > 0) && (s0[strd + 6] - s0[2] > 0) && (s0[-2] - s0[-strd + 2] > 0) && (s0[+6] - s0[-strd + 2] > 0))
				return true;

			//left
			if ((s0[-strd - 4] - s0[-strd] > trshold) && (s0[-4] - s0[0] > trshold) && (s0[strd - 4] - s0[strd] > trshold) && (s0[-strd] - s0[-strd + 4] > trshold) && (s0[0] - s0[+4] > trshold) && (s0[strd] - s0[strd + 4] > trshold))
				return true;
			if ((s0[-strd - 3] - s0[-strd + 1] > trshold) && (s0[-3] - s0[1] > trshold) && (s0[strd - 3] - s0[strd + 1] > trshold) && (s0[-strd + 1] - s0[-strd + 5] > trshold) && (s0[1] - s0[+5] > trshold) && (s0[strd + 1] - s0[strd + 5] > trshold))
				return true;
			if ((s0[-strd - 2] - s0[-strd + 2] > trshold) && (s0[-2] - s0[2] > trshold) && (s0[strd - 2] - s0[strd + 2] > trshold) && (s0[-strd + 2] - s0[-strd + 6] > trshold) && (s0[2] - s0[+6] > trshold) && (s0[strd + 2] - s0[strd + 6] > trshold))
				return true;

			//upper left
			if ((s0[-strd - 4] - s0[0] > trshold) && (s0[-4] - s0[strd] > trshold) && (s0[0] - s0[strd + 4] > trshold) && (s0[-strd] - s0[4] > trshold) && (s0[-strd + 4] - s0[4] > 0) && (s0[strd - 4] - s0[strd] > 0))
				return true;
			if ((s0[-strd - 3] - s0[1] > trshold) && (s0[-3] - s0[strd + 1] > trshold) && (s0[1] - s0[strd + 5] > trshold) && (s0[-strd + 1] - s0[5] > trshold) && (s0[-strd + 5] - s0[5] > 0) && (s0[strd - 3] - s0[strd + 1] > 0))
				return true;
			if ((s0[-strd - 2] - s0[2] > trshold) && (s0[-2] - s0[strd + 2] > trshold) && (s0[2] - s0[strd + 6] > trshold) && (s0[-strd + 2] - s0[6] > trshold) && (s0[-strd + 6] - s0[6] > 0) && (s0[strd - 2] - s0[strd + 2] > 0))
				return true;

			//upper right
			if ((s0[-strd + 4] - s0[0] > trshold) && (s0[-strd] - s0[-4] > trshold) && (s0[0] - s0[strd - 4] > trshold) && (s0[4] - s0[strd] > trshold) && (s0[-strd - 4] - s0[-4] > 0) && (s0[strd + 4] - s0[strd] > 0))
				return true;
			if ((s0[-strd + 5] - s0[1] > trshold) && (s0[-strd + 1] - s0[-3] > trshold) && (s0[1] - s0[strd - 3] > trshold) && (s0[5] - s0[strd + 1] > trshold) && (s0[-strd - 3] - s0[-3] > 0) && (s0[strd + 5] - s0[strd + 1] > 0))
				return true;
			if ((s0[-strd + 6] - s0[2] > trshold) && (s0[-strd + 2] - s0[-2] > trshold) && (s0[2] - s0[strd - 2] > trshold) && (s0[6] - s0[strd + 2] > trshold) && (s0[-strd - 2] - s0[-2] > 0) && (s0[strd + 6] - s0[strd + 2] > 0))
				return true;

			//lower left
			if ((s0[strd - 4] - s0[0] > trshold) && (s0[-4] - s0[-strd] > trshold) && (s0[0] - s0[strd + 4] > trshold) && (s0[strd] - s0[4] > trshold) && (s0[-strd - 4] - s0[-strd] > 0) && (s0[strd + 4] - s0[4] > 0))
				return true;
			if ((s0[strd - 3] - s0[1] > trshold) && (s0[-3] - s0[-strd + 1] > trshold) && (s0[1] - s0[strd + 5] > trshold) && (s0[strd + 1] - s0[5] > trshold) && (s0[-strd - 3] - s0[-strd + 1] > 0) && (s0[strd + 5] - s0[5] > 0))
				return true;
			if ((s0[strd - 2] - s0[2] > trshold) && (s0[-2] - s0[-strd + 2] > trshold) && (s0[2] - s0[strd + 6] > trshold) && (s0[strd + 2] - s0[6] > trshold) && (s0[-strd - 2] - s0[-strd + 2] > 0) && (s0[strd + 6] - s0[6] > 0))
				return true;

			//lower right
			if ((s0[strd + 4] - s0[0] > trshold) && (s0[strd] - s0[-4] > trshold) && (s0[strd + 4] - s0[0] > trshold) && (s0[4] - s0[-strd] > trshold) && (s0[strd - 4] - s0[-4] > 0) && (s0[-strd + 4] - s0[strd] > 0))
				return true;
			if ((s0[strd + 5] - s0[1] > trshold) && (s0[strd + 1] - s0[-3] > trshold) && (s0[strd + 5] - s0[1] > trshold) && (s0[5] - s0[-strd + 1] > trshold) && (s0[strd - 3] - s0[-3] > 0) && (s0[-strd + 5] - s0[strd + 1] > 0))
				return true;
			if ((s0[strd + 6] - s0[2] > trshold) && (s0[strd + 2] - s0[-2] > trshold) && (s0[strd + 6] - s0[2] > trshold) && (s0[6] - s0[-strd + 2] > trshold) && (s0[strd - 2] - s0[-2] > 0) && (s0[-strd + 6] - s0[strd + 2] > 0))
				return true;

			return false;
			
			//return ((((((((s0[-strd - 4] - s0[-4]) > trshold) && ((s0[-strd] - s0[0]) > trshold)) && (((s0[-strd + 4] - s0[4]) > trshold) && ((s0[-4] - s0[strd - 4]) > trshold))) && ((((s0[0] - s0[strd]) > trshold) && ((s0[4] - s0[strd + 4]) > trshold)) && (((s0[-strd - 4] - s0[0]) > 0) && ((s0[-strd + 4] - s0[0]) > 0)))) && ((s0[-4] - s0[strd]) > 0)) && ((s0[4] - s0[strd]) > 0)) || ((((((((s0[-strd - 3] - s0[-3]) > trshold) && ((s0[-strd + 1] - s0[1]) > trshold)) && (((s0[-strd + 5] - s0[5]) > trshold) && ((s0[-3] - s0[strd - 3]) > trshold))) && ((((s0[1] - s0[strd + 1]) > trshold) && ((s0[5] - s0[strd + 5]) > trshold)) && (((s0[-strd - 3] - s0[1]) > 0) && ((s0[-strd + 5] - s0[1]) > 0)))) && ((s0[-3] - s0[strd + 1]) > 0)) && ((s0[5] - s0[strd + 1]) > 0)) || ((((((((s0[-strd - 2] - s0[-2]) > trshold) && ((s0[-strd + 2] - s0[2]) > trshold)) && (((s0[-strd + 6] - s0[6]) > trshold) && ((s0[-2] - s0[strd - 2]) > trshold))) && ((((s0[2] - s0[strd + 2]) > trshold) && ((s0[6] - s0[strd + 6]) > trshold)) && (((s0[-strd - 2] - s0[2]) > 0) && ((s0[-strd + 6] - s0[2]) > 0)))) && ((s0[-2] - s0[strd + 2]) > 0)) && ((s0[6] - s0[strd + 2]) > 0)) || (((((((s0[-strd + 4] - s0[-strd]) > trshold) && ((s0[4] - s0[0]) > trshold)) && (((s0[strd + 4] - s0[strd]) > trshold) && ((s0[-strd] - s0[-strd - 4]) > trshold))) && ((s0[0] - s0[-4]) > trshold)) && ((s0[strd] - s0[strd - 4]) > trshold)) || (((((((s0[-strd + 5] - s0[-strd + 1]) > trshold) && ((s0[5] - s0[1]) > trshold)) && (((s0[strd + 5] - s0[strd + 1]) > trshold) && ((s0[-strd + 1] - s0[-strd - 3]) > trshold))) && ((s0[1] - s0[-4]) > trshold)) && ((s0[strd + 1] - s0[strd - 3]) > trshold)) || (((((((s0[-strd + 6] - s0[-strd + 2]) > trshold) && ((s0[6] - s0[2]) > trshold)) && (((s0[strd + 6] - s0[strd + 2]) > trshold) && ((s0[-strd + 2] - s0[-strd - 2]) > trshold))) && ((s0[2] - s0[-2]) > trshold)) && ((s0[strd + 2] - s0[strd - 2]) > trshold)) || ((((((((s0[strd - 4] - s0[-4]) > trshold) && ((s0[strd] - s0[0]) > trshold)) && (((s0[strd + 4] - s0[4]) > trshold) && ((s0[-4] - s0[-strd - 4]) > trshold))) && ((((s0[0] - s0[-strd]) > trshold) && ((s0[4] - s0[-strd + 4]) > trshold)) && (((s0[strd - 4] - s0[0]) > 0) && ((s0[strd + 4] - s0[0]) > 0)))) && ((s0[-4] - s0[strd]) > 0)) && ((s0[4] - s0[strd]) > 0)) || ((((((((s0[strd - 3] - s0[-3]) > trshold) && ((s0[strd + 1] - s0[1]) > trshold)) && (((s0[strd + 5] - s0[5]) > trshold) && ((s0[-3] - s0[-strd - 3]) > trshold))) && ((((s0[1] - s0[-strd + 1]) > trshold) && ((s0[5] - s0[-strd + 5]) > trshold)) && (((s0[strd - 3] - s0[1]) > 0) && ((s0[strd + 5] - s0[1]) > 0)))) && ((s0[-3] - s0[strd + 1]) > 0)) && ((s0[5] - s0[strd + 1]) > 0)) || ((((((((s0[strd - 2] - s0[-2]) > trshold) && ((s0[strd + 2] - s0[2]) > trshold)) && (((s0[strd + 6] - s0[6]) > trshold) && ((s0[-2] - s0[-strd - 2]) > trshold))) && ((((s0[2] - s0[-strd + 2]) > trshold) && ((s0[6] - s0[-strd + 6]) > trshold)) && (((s0[strd - 2] - s0[2]) > 0) && ((s0[strd + 6] - s0[2]) > 0)))) && ((s0[-2] - s0[strd + 2]) > 0)) && ((s0[6] - s0[strd + 2]) > 0)) || (((((((s0[-strd - 4] - s0[-strd]) > trshold) && ((s0[-4] - s0[0]) > trshold)) && (((s0[strd - 4] - s0[strd]) > trshold) && ((s0[-strd] - s0[-strd + 4]) > trshold))) && ((s0[0] - s0[4]) > trshold)) && ((s0[strd] - s0[strd + 4]) > trshold)) || (((((((s0[-strd - 3] - s0[-strd + 1]) > trshold) && ((s0[-3] - s0[1]) > trshold)) && (((s0[strd - 3] - s0[strd + 1]) > trshold) && ((s0[-strd + 1] - s0[-strd + 5]) > trshold))) && ((s0[1] - s0[5]) > trshold)) && ((s0[strd + 1] - s0[strd + 5]) > trshold)) || (((((((s0[-strd - 2] - s0[-strd + 2]) > trshold) && ((s0[-2] - s0[2]) > trshold)) && (((s0[strd - 2] - s0[strd + 2]) > trshold) && ((s0[-strd + 2] - s0[-strd + 6]) > trshold))) && ((s0[2] - s0[6]) > trshold)) && ((s0[strd + 2] - s0[strd + 6]) > trshold)) || (((((((s0[-strd - 4] - s0[0]) > trshold) && ((s0[-4] - s0[strd]) > trshold)) && (((s0[0] - s0[strd + 4]) > trshold) && ((s0[-strd] - s0[4]) > trshold))) && ((s0[-strd + 4] - s0[4]) > 0)) && ((s0[strd - 4] - s0[strd]) > 0)) || (((((((s0[-strd - 3] - s0[1]) > trshold) && ((s0[-3] - s0[strd + 1]) > trshold)) && (((s0[1] - s0[strd + 5]) > trshold) && ((s0[-strd + 1] - s0[5]) > trshold))) && ((s0[-strd + 5] - s0[5]) > 0)) && ((s0[strd - 3] - s0[strd + 1]) > 0)) || (((((((s0[-strd - 2] - s0[2]) > trshold) && ((s0[-2] - s0[strd + 2]) > trshold)) && (((s0[2] - s0[strd + 6]) > trshold) && ((s0[-strd + 2] - s0[6]) > trshold))) && ((s0[-strd + 6] - s0[6]) > 0)) && ((s0[strd - 2] - s0[strd + 2]) > 0)) || (((((((s0[-strd + 4] - s0[0]) > trshold) && ((s0[-strd] - s0[-4]) > trshold)) && (((s0[0] - s0[strd - 4]) > trshold) && ((s0[4] - s0[strd]) > trshold))) && ((s0[-strd - 4] - s0[-4]) > 0)) && ((s0[strd + 4] - s0[strd]) > 0)) || (((((((s0[-strd + 5] - s0[1]) > trshold) && ((s0[-strd + 1] - s0[-3]) > trshold)) && (((s0[1] - s0[strd - 3]) > trshold) && ((s0[5] - s0[strd + 1]) > trshold))) && ((s0[-strd - 3] - s0[-3]) > 0)) && ((s0[strd + 5] - s0[strd + 1]) > 0)) || (((((((s0[-strd + 6] - s0[2]) > trshold) && ((s0[-strd + 2] - s0[-2]) > trshold)) && (((s0[2] - s0[strd - 2]) > trshold) && ((s0[6] - s0[strd + 2]) > trshold))) && ((s0[-strd - 2] - s0[-2]) > 0)) && ((s0[strd + 6] - s0[strd + 2]) > 0)) || (((((((s0[strd - 4] - s0[0]) > trshold) && ((s0[-4] - s0[-strd]) > trshold)) && (((s0[0] - s0[strd + 4]) > trshold) && ((s0[strd] - s0[4]) > trshold))) && ((s0[-strd - 4] - s0[-strd]) > 0)) && ((s0[strd + 4] - s0[4]) > 0)) || (((((((s0[strd - 3] - s0[1]) > trshold) && ((s0[-3] - s0[-strd + 1]) > trshold)) && (((s0[1] - s0[strd + 5]) > trshold) && ((s0[strd + 1] - s0[5]) > trshold))) && ((s0[-strd - 3] - s0[-strd + 1]) > 0)) && ((s0[strd + 5] - s0[5]) > 0)) || (((((((s0[strd - 2] - s0[2]) > trshold) && ((s0[-2] - s0[-strd + 2]) > trshold)) && (((s0[2] - s0[strd + 6]) > trshold) && ((s0[strd + 2] - s0[6]) > trshold))) && ((s0[-strd - 2] - s0[-strd + 2]) > 0)) && ((s0[strd + 6] - s0[6]) > 0)) || (((((((s0[strd + 4] - s0[0]) > trshold) && ((s0[strd] - s0[-4]) > trshold)) && (((s0[strd + 4] - s0[0]) > trshold) && ((s0[4] - s0[-strd]) > trshold))) && ((s0[strd - 4] - s0[-4]) > 0)) && ((s0[-strd + 4] - s0[strd]) > 0)) || (((((((s0[strd + 5] - s0[1]) > trshold) && ((s0[strd + 1] - s0[-3]) > trshold)) && (((s0[strd + 5] - s0[1]) > trshold) && ((s0[5] - s0[-strd + 1]) > trshold))) && ((s0[strd - 3] - s0[-3]) > 0)) && ((s0[-strd + 5] - s0[strd + 1]) > 0)) || ((((((s0[strd + 6] - s0[2]) > trshold) && ((s0[strd + 2] - s0[-2]) > trshold)) && (((s0[strd + 6] - s0[2]) > trshold) && ((s0[6] - s0[-strd + 2]) > trshold))) && ((s0[strd - 2] - s0[-2]) > 0)) && ((s0[-strd + 6] - s0[strd + 2]) > 0)))))))))))))))))))))))));
		}
		#endregion

		#region Jirka8bppEdge()
		/// <summary>
		/// operator: [[1,2,1],[-1,-2,-1],[-1,-2,-1]] rotating
		/// </summary>
		/// <param name="scan0"></param>
		/// <param name="stride"></param>
		/// <param name="threshold"></param>
		/// <returns></returns>
		private static unsafe bool Jirka8bppEdge(byte* s0, int strd, int trshold)
		{
			
			//top
			if ((s0[-strd - 1] - s0[-1] > trshold) && (s0[-strd] - s0[0] > trshold) && (s0[-strd + 1] - s0[+1] > trshold) && (s0[-1] - s0[strd - 1] > trshold) && (s0[0] - s0[strd] > trshold) && (s0[+1] - s0[strd + 1] > trshold) && (s0[-strd - 1] - s0[0] > 0) && (s0[-strd + 1] - s0[0] > 0) && (s0[-1] - s0[strd] > 0) && (s0[+1] - s0[strd] > 0))
				return true;

			//right
			if ((s0[-strd + 1] - s0[-strd] > trshold) && (s0[1] - s0[0] > trshold) && (s0[strd + 1] - s0[strd] > trshold) && (s0[-strd] - s0[-strd - 1] > trshold) && (s0[0] - s0[-1] > trshold) && (s0[strd] - s0[strd - 1] > trshold))
				return true;

			//bottom
			if ((s0[strd - 1] - s0[-1] > trshold) && (s0[strd] - s0[0] > trshold) && (s0[strd + 1] - s0[+1] > trshold) && (s0[-1] - s0[-strd - 1] > trshold) && (s0[0] - s0[-strd] > trshold) && (s0[+1] - s0[-strd + 1] > trshold) && (s0[strd - 1] - s0[0] > 0) && (s0[strd + 1] - s0[0] > 0) && (s0[-1] - s0[-strd] > 0) && (s0[+1] - s0[-strd] > 0))
				return true;

			//left
			if ((s0[-strd - 1] - s0[-strd] > trshold) && (s0[-1] - s0[0] > trshold) && (s0[strd - 1] - s0[strd] > trshold) && (s0[-strd] - s0[-strd + 1] > trshold) && (s0[0] - s0[+1] > trshold) && (s0[strd] - s0[strd + 1] > trshold))
				return true;

			//upper left
			if ((s0[-strd - 1] - s0[0] > trshold) && (s0[-1] - s0[strd] > trshold) && (s0[0] - s0[strd + 1] > trshold) && (s0[-strd] - s0[1] > trshold) && (s0[-strd + 1] - s0[1] > 0) && (s0[strd - 1] - s0[strd] > 0))
				return true;

			//upper right
			if ((s0[-strd + 1] - s0[0] > trshold) && (s0[-strd] - s0[-1] > trshold) && (s0[0] - s0[strd - 1] > trshold) && (s0[1] - s0[strd] > trshold) && (s0[-strd - 1] - s0[-1] > 0) && (s0[strd + 1] - s0[strd] > 0))
				return true;

			//lower left
			if ((s0[strd - 1] - s0[0] > trshold) && (s0[-1] - s0[-strd] > trshold) && (s0[0] - s0[strd + 1] > trshold) && (s0[strd] - s0[1] > trshold) && (s0[-strd - 1] - s0[-strd] > 0) && (s0[strd + 1] - s0[1] > 0))
				return true;

			//lower right
			if ((s0[strd + 1] - s0[0] > trshold) && (s0[strd] - s0[-1] > trshold) && (s0[strd + 1] - s0[0] > trshold) && (s0[1] - s0[-strd] > trshold) && (s0[strd - 1] - s0[-1] > 0) && (s0[-strd + 1] - s0[strd] > 0))
				return true;

			return false;
			 

			//return ((((((((s0[-strd - 1] - s0[-1]) > trshold) && ((s0[-strd] - s0[0]) > trshold)) && (((s0[-strd + 1] - s0[1]) > trshold) && ((s0[-1] - s0[strd - 1]) > trshold))) && ((((s0[0] - s0[strd]) > trshold) && ((s0[1] - s0[strd + 1]) > trshold)) && (((s0[-strd - 1] - s0[0]) > 0) && ((s0[-strd + 1] - s0[0]) > 0)))) && ((s0[-1] - s0[strd]) > 0)) && ((s0[1] - s0[strd]) > 0)) || (((((((s0[-strd + 1] - s0[-strd]) > trshold) && ((s0[1] - s0[0]) > trshold)) && (((s0[strd + 1] - s0[strd]) > trshold) && ((s0[-strd] - s0[-strd - 1]) > trshold))) && ((s0[0] - s0[-1]) > trshold)) && ((s0[strd] - s0[strd - 1]) > trshold)) || ((((((((s0[strd - 1] - s0[-1]) > trshold) && ((s0[strd] - s0[0]) > trshold)) && (((s0[strd + 1] - s0[1]) > trshold) && ((s0[-1] - s0[-strd - 1]) > trshold))) && ((((s0[0] - s0[-strd]) > trshold) && ((s0[1] - s0[-strd + 1]) > trshold)) && (((s0[strd - 1] - s0[0]) > 0) && ((s0[strd + 1] - s0[0]) > 0)))) && ((s0[-1] - s0[strd]) > 0)) && ((s0[1] - s0[strd]) > 0)) || (((((((s0[-strd - 1] - s0[-strd]) > trshold) && ((s0[-1] - s0[0]) > trshold)) && (((s0[strd - 1] - s0[strd]) > trshold) && ((s0[-strd] - s0[-strd + 1]) > trshold))) && ((s0[0] - s0[1]) > trshold)) && ((s0[strd] - s0[strd + 1]) > trshold)) || (((((((s0[-strd - 1] - s0[0]) > trshold) && ((s0[-1] - s0[strd]) > trshold)) && (((s0[0] - s0[strd + 1]) > trshold) && ((s0[-strd] - s0[1]) > trshold))) && ((s0[-strd + 1] - s0[1]) > 0)) && ((s0[strd - 1] - s0[strd]) > 0)) || (((((((s0[-strd + 1] - s0[0]) > trshold) && ((s0[-strd] - s0[-1]) > trshold)) && (((s0[0] - s0[strd - 1]) > trshold) && ((s0[1] - s0[strd]) > trshold))) && ((s0[-strd - 1] - s0[-1]) > 0)) && ((s0[strd + 1] - s0[strd]) > 0)) || (((((((s0[strd - 1] - s0[0]) > trshold) && ((s0[-1] - s0[-strd]) > trshold)) && (((s0[0] - s0[strd + 1]) > trshold) && ((s0[strd] - s0[1]) > trshold))) && ((s0[-strd - 1] - s0[-strd]) > 0)) && ((s0[strd + 1] - s0[1]) > 0)) || ((((((s0[strd + 1] - s0[0]) > trshold) && ((s0[strd] - s0[-1]) > trshold)) && (((s0[strd + 1] - s0[0]) > trshold) && ((s0[1] - s0[-strd]) > trshold))) && ((s0[strd - 1] - s0[-1]) > 0)) && ((s0[-strd + 1] - s0[strd]) > 0)))))))));
		}
		#endregion

		#region Kirsch24bppEdge()
		private static unsafe bool Kirsch24bppEdge(byte* scan0, int stride, int threshold)
		{
			/*
			if (scan0[-stride - 3] + 2 * scan0[-stride] + scan0[-stride + 3] - scan0[stride - 3] - 2 * scan0[stride] - scan0[stride + 3] > threshold)
				return true;
			if (scan0[-stride - 2] + 2 * scan0[-stride + 1] + scan0[-stride + 4] - scan0[stride - 2] - 2 * scan0[stride + 1] - scan0[stride + 4] > threshold)
				return true;
			if (scan0[-stride - 1] + 2 * scan0[-stride + 2] + scan0[-stride + 5] - scan0[stride - 1] - 2 * scan0[stride + 2] - scan0[stride + 5] > threshold)
				return true;

			if ((scan0[-stride - 3] + 2 * scan0[-stride] + scan0[-stride + 3] - scan0[stride - 3] - 2 * scan0[stride] - scan0[stride + 3]) < -threshold)
				return true;
			if (scan0[-stride - 2] + 2 * scan0[-stride + 1] + scan0[-stride + 4] - scan0[stride - 2] - 2 * scan0[stride + 1] - scan0[stride + 4] < -threshold)
				return true;
			if (scan0[-stride - 1] + 2 * scan0[-stride + 2] + scan0[-stride + 5] - scan0[stride - 1] - 2 * scan0[stride + 2] - scan0[stride + 5] < -threshold)
				return true;

			if (scan0[-stride - 3] + 2 * scan0[-3] + scan0[stride - 3] - scan0[-stride + 3] - 2 * scan0[3] - scan0[stride + 3] > threshold)
				return true;
			if (scan0[-stride - 2] + 2 * scan0[-2] + scan0[stride - 2] - scan0[-stride + 4] - 2 * scan0[4] - scan0[stride + 4] > threshold)
				return true;
			if (scan0[-stride - 1] + 2 * scan0[-1] + scan0[stride - 1] - scan0[-stride + 5] - 2 * scan0[5] - scan0[stride + 5] > threshold)
				return true;

			if (scan0[-stride - 3] + 2 * scan0[-3] + scan0[stride - 3] - scan0[-stride + 3] - 2 * scan0[3] - scan0[stride + 3] < -threshold)
				return true;
			if (scan0[-stride - 2] + 2 * scan0[-2] + scan0[stride - 2] - scan0[-stride + 4] - 2 * scan0[4] - scan0[stride + 4] < -threshold)
				return true;
			if (scan0[-stride - 1] + 2 * scan0[-1] + scan0[stride - 1] - scan0[-stride + 5] - 2 * scan0[5] - scan0[stride + 5] < -threshold)
				return true;

			if (scan0[-stride] + 2 * scan0[-stride + 3] + scan0[3] - scan0[-3] - 2 * scan0[+stride - 3] - scan0[-stride] > threshold)
				return true;
			if (scan0[-stride + 1] + 2 * scan0[-stride + 4] + scan0[4] - scan0[-2] - 2 * scan0[+stride - 2] - scan0[-stride + 1] > threshold)
				return true;
			if (scan0[-stride + 2] + 2 * scan0[-stride + 5] + scan0[5] - scan0[-1] - 2 * scan0[+stride - 1] - scan0[-stride + 2] > threshold)
				return true;

			if (scan0[-stride] + 2 * scan0[-stride + 3] + scan0[3] - scan0[-3] - 2 * scan0[+stride - 3] - scan0[-stride] < -threshold)
				return true;
			if (scan0[-stride + 1] + 2 * scan0[-stride + 4] + scan0[4] - scan0[-2] - 2 * scan0[+stride - 2] - scan0[-stride + 1] < -threshold)
				return true;
			if (scan0[-stride + 2] + 2 * scan0[-stride + 5] + scan0[5] - scan0[-1] - 2 * scan0[+stride - 1] - scan0[-stride + 2] < -threshold)
				return true;

			if (scan0[-3] + 2 * scan0[-stride - 3] + scan0[-stride] - scan0[stride] - 2 * scan0[+stride + 3] - scan0[3] > threshold)
				return true;
			if (scan0[-2] + 2 * scan0[-stride - 2] + scan0[-stride + 1] - scan0[stride + 1] - 2 * scan0[+stride + 4] - scan0[4] > threshold)
				return true;
			if (scan0[-1] + 2 * scan0[-stride - 1] + scan0[-stride + 2] - scan0[stride + 2] - 2 * scan0[+stride + 5] - scan0[5] > threshold)
				return true;

			if (scan0[-3] + 2 * scan0[-stride - 3] + scan0[-stride] - scan0[stride] - 2 * scan0[+stride + 3] - scan0[3] < -threshold)
				return true;
			if (scan0[-2] + 2 * scan0[-stride - 2] + scan0[-stride + 1] - scan0[stride + 1] - 2 * scan0[+stride + 4] - scan0[4] < -threshold)
				return true;
			if (scan0[-1] + 2 * scan0[-stride - 1] + scan0[-stride + 2] - scan0[stride + 2] - 2 * scan0[+stride + 5] - scan0[5] < -threshold)
				return true;

			return false;
			 */
			return (
				((scan0[-stride - 3] + 2 * scan0[-stride] + scan0[-stride + 3] - scan0[stride - 3] - 2 * scan0[stride] - scan0[stride + 3]) > threshold) || 
				((scan0[-stride - 2] + 2 * scan0[-stride + 1] + scan0[-stride + 4] - scan0[stride - 2] - 2 * scan0[stride + 1] - scan0[stride + 4]) > threshold) || 
				((scan0[-stride - 1] + 2 * scan0[-stride + 2] + scan0[-stride + 5] - scan0[stride - 1] - 2 * scan0[stride + 2] - scan0[stride + 5]) > threshold) || 
				((scan0[-stride - 3] + 2 * scan0[-stride] + scan0[-stride + 3] - scan0[stride - 3] - 2 * scan0[stride] - scan0[stride + 3]) < -threshold) || 
				((scan0[-stride - 2] + 2 * scan0[-stride + 1] + scan0[-stride + 4] - scan0[stride - 2] - 2 * scan0[stride + 1] - scan0[stride + 4]) < -threshold) || 
				((scan0[-stride - 1] + 2 * scan0[-stride + 2] + scan0[-stride + 5] - scan0[stride - 1] - 2 * scan0[stride + 2] - scan0[stride + 5]) < -threshold) || 
				((scan0[-stride - 3] + 2 * scan0[-3] + scan0[stride - 3] - scan0[-stride + 3] - 2 * scan0[3] - scan0[stride + 3]) > threshold) || 
				((scan0[-stride - 2] + 2 * scan0[-2] + scan0[stride - 2] - scan0[-stride + 4] - 2 * scan0[4] - scan0[stride + 4]) > threshold) || 
				((scan0[-stride - 1] + 2 * scan0[-1] + scan0[stride - 1] - scan0[-stride + 5] - 2 * scan0[5] - scan0[stride + 5]) > threshold) || 
				((scan0[-stride - 3] + 2 * scan0[-3] + scan0[stride - 3] - scan0[-stride + 3] - 2 * scan0[3] - scan0[stride + 3]) < -threshold) || 
				((scan0[-stride - 2] + 2 * scan0[-2] + scan0[stride - 2] - scan0[-stride + 4] - 2 * scan0[4] - scan0[stride + 4]) < -threshold) || 
				((scan0[-stride - 1] + 2 * scan0[-1] + scan0[stride - 1] - scan0[-stride + 5] - 2 * scan0[5] - scan0[stride + 5]) < -threshold) || 
				((scan0[-stride] + 2 * scan0[-stride + 3] + scan0[3] - scan0[-3] - 2 * scan0[stride - 3] - scan0[-stride]) > threshold) || 
				((scan0[-stride + 1] + 2 * scan0[-stride + 4] + scan0[4] - scan0[-2] - 2 * scan0[stride - 2] - scan0[-stride + 1]) > threshold) || 
				((scan0[-stride + 2] + 2 * scan0[-stride + 5] + scan0[5] - scan0[-1] - 2 * scan0[stride - 1] - scan0[-stride + 2]) > threshold) || 
				((scan0[-stride] + 2 * scan0[-stride + 3] + scan0[3] - scan0[-3] - 2 * scan0[stride - 3] - scan0[-stride]) < -threshold) || 
				((scan0[-stride + 1] + 2 * scan0[-stride + 4] + scan0[4] - scan0[-2] - 2 * scan0[stride - 2] - scan0[-stride + 1]) < -threshold) || 
				((scan0[-stride + 2] + 2 * scan0[-stride + 5] + scan0[5] - scan0[-1] - 2 * scan0[stride - 1] - scan0[-stride + 2]) < -threshold) || 
				((scan0[-3] + 2 * scan0[-stride - 3] + scan0[-stride] - scan0[stride] - 2 * scan0[stride + 3] - scan0[3]) > threshold) || 
				((scan0[-2] + 2 * scan0[-stride - 2] + scan0[-stride + 1] - scan0[stride + 1] - 2 * scan0[stride + 4] - scan0[4]) > threshold) || 
				((scan0[-1] + 2 * scan0[-stride - 1] + scan0[-stride + 2] - scan0[stride + 2] - 2 * scan0[stride + 5] - scan0[5]) > threshold) || 
				((scan0[-3] + 2 * scan0[-stride - 3] + scan0[-stride] - scan0[stride] - 2 * scan0[stride + 3] - scan0[3]) < -threshold) || 
				((scan0[-2] + 2 * scan0[-stride - 2] + scan0[-stride + 1] - scan0[stride + 1] - 2 * scan0[stride + 4] - scan0[4]) < -threshold) || 
				((scan0[-1] + 2 * scan0[-stride - 1] + scan0[-stride + 2] - scan0[stride + 2] - 2 * scan0[stride + 5] - scan0[5]) < -threshold)
				);
		}
		#endregion

		#region Kirsch32bppEdge()
		private static unsafe bool Kirsch32bppEdge(byte* scan0, int stride, int threshold)
		{
			/*
			if (scan0[-stride - 4] + 2 * scan0[-stride] + scan0[-stride + 4] - scan0[stride - 4] - 3 * scan0[stride] - scan0[stride + 4] > threshold)
				return true;
			if (scan0[-stride - 3] + 2 * scan0[-stride + 1] + scan0[-stride + 5] - scan0[stride - 3] - 3 * scan0[stride + 1] - scan0[stride + 5] > threshold)
				return true;
			if (scan0[-stride - 2] + 2 * scan0[-stride + 2] + scan0[-stride + 6] - scan0[stride - 2] - 3 * scan0[stride + 2] - scan0[stride + 6] > threshold)
				return true;

			if ((scan0[-stride - 4] + 2 * scan0[-stride] + scan0[-stride + 4] - scan0[stride - 4] - 3 * scan0[stride] - scan0[stride + 4]) < -threshold)
				return true;
			if (scan0[-stride - 3] + 2 * scan0[-stride + 1] + scan0[-stride + 5] - scan0[stride - 3] - 3 * scan0[stride + 1] - scan0[stride + 5] < -threshold)
				return true;
			if (scan0[-stride - 2] + 2 * scan0[-stride + 2] + scan0[-stride + 6] - scan0[stride - 2] - 3 * scan0[stride + 2] - scan0[stride + 6] < -threshold)
				return true;

			if (scan0[-stride - 4] + 2 * scan0[-4] + scan0[stride - 4] - scan0[-stride + 4] - 3 * scan0[4] - scan0[stride + 4] > threshold)
				return true;
			if (scan0[-stride - 3] + 2 * scan0[-3] + scan0[stride - 3] - scan0[-stride + 5] - 3 * scan0[5] - scan0[stride + 5] > threshold)
				return true;
			if (scan0[-stride - 2] + 2 * scan0[-2] + scan0[stride - 2] - scan0[-stride + 6] - 3 * scan0[6] - scan0[stride + 6] > threshold)
				return true;

			if (scan0[-stride - 4] + 2 * scan0[-4] + scan0[stride - 4] - scan0[-stride + 4] - 3 * scan0[4] - scan0[stride + 4] < -threshold)
				return true;
			if (scan0[-stride - 3] + 2 * scan0[-3] + scan0[stride - 3] - scan0[-stride + 5] - 3 * scan0[5] - scan0[stride + 5] < -threshold)
				return true;
			if (scan0[-stride - 2] + 2 * scan0[-2] + scan0[stride - 2] - scan0[-stride + 6] - 3 * scan0[6] - scan0[stride + 6] < -threshold)
				return true;

			if (scan0[-stride] + 2 * scan0[-stride + 4] + scan0[4] - scan0[-4] - 3 * scan0[+stride - 4] - scan0[-stride] > threshold)
				return true;
			if (scan0[-stride + 1] + 2 * scan0[-stride + 5] + scan0[5] - scan0[-3] - 3 * scan0[+stride - 3] - scan0[-stride + 1] > threshold)
				return true;
			if (scan0[-stride + 2] + 2 * scan0[-stride + 6] + scan0[6] - scan0[-2] - 3 * scan0[+stride - 2] - scan0[-stride + 2] > threshold)
				return true;

			if (scan0[-stride] + 2 * scan0[-stride + 4] + scan0[4] - scan0[-4] - 3 * scan0[+stride - 4] - scan0[-stride] < -threshold)
				return true;
			if (scan0[-stride + 1] + 2 * scan0[-stride + 5] + scan0[5] - scan0[-3] - 3 * scan0[+stride - 3] - scan0[-stride + 1] < -threshold)
				return true;
			if (scan0[-stride + 2] + 2 * scan0[-stride + 6] + scan0[6] - scan0[-2] - 3 * scan0[+stride - 2] - scan0[-stride + 2] < -threshold)
				return true;

			if (scan0[-4] + 2 * scan0[-stride - 4] + scan0[-stride] - scan0[stride] - 3 * scan0[+stride + 4] - scan0[4] > threshold)
				return true;
			if (scan0[-3] + 2 * scan0[-stride - 3] + scan0[-stride + 1] - scan0[stride + 1] - 3 * scan0[+stride + 5] - scan0[5] > threshold)
				return true;
			if (scan0[-2] + 2 * scan0[-stride - 2] + scan0[-stride + 2] - scan0[stride + 2] - 3 * scan0[+stride + 6] - scan0[6] > threshold)
				return true;

			if (scan0[-4] + 2 * scan0[-stride - 4] + scan0[-stride] - scan0[stride] - 3 * scan0[+stride + 4] - scan0[4] < -threshold)
				return true;
			if (scan0[-3] + 2 * scan0[-stride - 3] + scan0[-stride + 1] - scan0[stride + 1] - 3 * scan0[+stride + 5] - scan0[5] < -threshold)
				return true;
			if (scan0[-2] + 2 * scan0[-stride - 2] + scan0[-stride + 2] - scan0[stride + 2] - 3 * scan0[+stride + 6] - scan0[6] < -threshold)
				return true;

			return false;
			 */
			return (((((((scan0[-stride - 4] + (2 * scan0[-stride])) + scan0[-stride + 4]) - scan0[stride - 4]) - (3 * scan0[stride])) - scan0[stride + 4]) > threshold) || (((((((scan0[-stride - 3] + (2 * scan0[-stride + 1])) + scan0[-stride + 5]) - scan0[stride - 3]) - (3 * scan0[stride + 1])) - scan0[stride + 5]) > threshold) || (((((((scan0[-stride - 2] + (2 * scan0[-stride + 2])) + scan0[-stride + 6]) - scan0[stride - 2]) - (3 * scan0[stride + 2])) - scan0[stride + 6]) > threshold) || (((((((scan0[-stride - 4] + (2 * scan0[-stride])) + scan0[-stride + 4]) - scan0[stride - 4]) - (3 * scan0[stride])) - scan0[stride + 4]) < -threshold) || (((((((scan0[-stride - 3] + (2 * scan0[-stride + 1])) + scan0[-stride + 5]) - scan0[stride - 3]) - (3 * scan0[stride + 1])) - scan0[stride + 5]) < -threshold) || (((((((scan0[-stride - 2] + (2 * scan0[-stride + 2])) + scan0[-stride + 6]) - scan0[stride - 2]) - (3 * scan0[stride + 2])) - scan0[stride + 6]) < -threshold) || (((((((scan0[-stride - 4] + (2 * scan0[-4])) + scan0[stride - 4]) - scan0[-stride + 4]) - (3 * scan0[4])) - scan0[stride + 4]) > threshold) || (((((((scan0[-stride - 3] + (2 * scan0[-3])) + scan0[stride - 3]) - scan0[-stride + 5]) - (3 * scan0[5])) - scan0[stride + 5]) > threshold) || (((((((scan0[-stride - 2] + (2 * scan0[-2])) + scan0[stride - 2]) - scan0[-stride + 6]) - (3 * scan0[6])) - scan0[stride + 6]) > threshold) || (((((((scan0[-stride - 4] + (2 * scan0[-4])) + scan0[stride - 4]) - scan0[-stride + 4]) - (3 * scan0[4])) - scan0[stride + 4]) < -threshold) || (((((((scan0[-stride - 3] + (2 * scan0[-3])) + scan0[stride - 3]) - scan0[-stride + 5]) - (3 * scan0[5])) - scan0[stride + 5]) < -threshold) || (((((((scan0[-stride - 2] + (2 * scan0[-2])) + scan0[stride - 2]) - scan0[-stride + 6]) - (3 * scan0[6])) - scan0[stride + 6]) < -threshold) || (((((((scan0[-stride] + (2 * scan0[-stride + 4])) + scan0[4]) - scan0[-4]) - (3 * scan0[stride - 4])) - scan0[-stride]) > threshold) || (((((((scan0[-stride + 1] + (2 * scan0[-stride + 5])) + scan0[5]) - scan0[-3]) - (3 * scan0[stride - 3])) - scan0[-stride + 1]) > threshold) || (((((((scan0[-stride + 2] + (2 * scan0[-stride + 6])) + scan0[6]) - scan0[-2]) - (3 * scan0[stride - 2])) - scan0[-stride + 2]) > threshold) || (((((((scan0[-stride] + (2 * scan0[-stride + 4])) + scan0[4]) - scan0[-4]) - (3 * scan0[stride - 4])) - scan0[-stride]) < -threshold) || (((((((scan0[-stride + 1] + (2 * scan0[-stride + 5])) + scan0[5]) - scan0[-3]) - (3 * scan0[stride - 3])) - scan0[-stride + 1]) < -threshold) || (((((((scan0[-stride + 2] + (2 * scan0[-stride + 6])) + scan0[6]) - scan0[-2]) - (3 * scan0[stride - 2])) - scan0[-stride + 2]) < -threshold) || (((((((scan0[-4] + (2 * scan0[-stride - 4])) + scan0[-stride]) - scan0[stride]) - (3 * scan0[stride + 4])) - scan0[4]) > threshold) || (((((((scan0[-3] + (2 * scan0[-stride - 3])) + scan0[-stride + 1]) - scan0[stride + 1]) - (3 * scan0[stride + 5])) - scan0[5]) > threshold) || (((((((scan0[-2] + (2 * scan0[-stride - 2])) + scan0[-stride + 2]) - scan0[stride + 2]) - (3 * scan0[stride + 6])) - scan0[6]) > threshold) || (((((((scan0[-4] + (2 * scan0[-stride - 4])) + scan0[-stride]) - scan0[stride]) - (3 * scan0[stride + 4])) - scan0[4]) < -threshold) || (((((((scan0[-3] + (2 * scan0[-stride - 3])) + scan0[-stride + 1]) - scan0[stride + 1]) - (3 * scan0[stride + 5])) - scan0[5]) < -threshold) || ((((((scan0[-2] + (2 * scan0[-stride - 2])) + scan0[-stride + 2]) - scan0[stride + 2]) - (3 * scan0[stride + 6])) - scan0[6]) < -threshold))))))))))))))))))))))));
		}
		#endregion

		#region Kirsch8bppEdge()
		private static unsafe bool Kirsch8bppEdge(byte* scan0, int stride, int threshold)
		{
			/*
			if (scan0[-stride - 1] + 2 * scan0[-stride] + scan0[-stride + 1] - scan0[stride - 1] - 2 * scan0[stride] - scan0[stride + 1] > threshold)
				return true;

			if ((scan0[-stride - 1] + 2 * scan0[-stride] + scan0[-stride + 1] - scan0[stride - 1] - 2 * scan0[stride] - scan0[stride + 1]) < -threshold)
				return true;

			if (scan0[-stride - 1] + 2 * scan0[-1] + scan0[stride - 1] - scan0[-stride + 1] - 2 * scan0[1] - scan0[stride + 1] > threshold)
				return true;

			if (scan0[-stride - 1] + 2 * scan0[-1] + scan0[stride - 1] - scan0[-stride + 1] - 2 * scan0[1] - scan0[stride + 1] < -threshold)
				return true;

			if (scan0[-stride] + 2 * scan0[-stride + 1] + scan0[1] - scan0[-1] - 2 * scan0[+stride - 1] - scan0[-stride] > threshold)
				return true;

			if (scan0[-stride] + 2 * scan0[-stride + 1] + scan0[1] - scan0[-1] - 2 * scan0[+stride - 1] - scan0[-stride] < -threshold)
				return true;

			if (scan0[-1] + 2 * scan0[-stride - 1] + scan0[-stride] - scan0[stride] - 2 * scan0[+stride + 1] - scan0[1] > threshold)
				return true;

			if (scan0[-1] + 2 * scan0[-stride - 1] + scan0[-stride] - scan0[stride] - 2 * scan0[+stride + 1] - scan0[1] < -threshold)
				return true;

			return false;
			 */
			return (((((((scan0[-stride - 1] + (2 * scan0[-stride])) + scan0[-stride + 1]) - scan0[stride - 1]) - (2 * scan0[stride])) - scan0[stride + 1]) > threshold) || (((((((scan0[-stride - 1] + (2 * scan0[-stride])) + scan0[-stride + 1]) - scan0[stride - 1]) - (2 * scan0[stride])) - scan0[stride + 1]) < -threshold) || (((((((scan0[-stride - 1] + (2 * scan0[-1])) + scan0[stride - 1]) - scan0[-stride + 1]) - (2 * scan0[1])) - scan0[stride + 1]) > threshold) || (((((((scan0[-stride - 1] + (2 * scan0[-1])) + scan0[stride - 1]) - scan0[-stride + 1]) - (2 * scan0[1])) - scan0[stride + 1]) < -threshold) || (((((((scan0[-stride] + (2 * scan0[-stride + 1])) + scan0[1]) - scan0[-1]) - (2 * scan0[stride - 1])) - scan0[-stride]) > threshold) || (((((((scan0[-stride] + (2 * scan0[-stride + 1])) + scan0[1]) - scan0[-1]) - (2 * scan0[stride - 1])) - scan0[-stride]) < -threshold) || (((((((scan0[-1] + (2 * scan0[-stride - 1])) + scan0[-stride]) - scan0[stride]) - (2 * scan0[stride + 1])) - scan0[1]) > threshold) || ((((((scan0[-1] + (2 * scan0[-stride - 1])) + scan0[-stride]) - scan0[stride]) - (2 * scan0[stride + 1])) - scan0[1]) < -threshold))))))));
		}
		#endregion

		#region Sobel()
		private static Bitmap Sobel(Bitmap source, Rectangle rect)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;
			
			try
			{
				int width = rect.Width;
				int height = rect.Height;
				
				result = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				sourceData = source.LockBits(rect, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, result.PixelFormat);
				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				unsafe
				{
					byte* scanS = (byte*)sourceData.Scan0.ToPointer();
					byte* scanR = (byte*)resultData.Scan0.ToPointer();
					result.Palette = Misc.GetGrayscalePalette();

					switch (source.PixelFormat)
					{
						case PixelFormat.Format8bppIndexed:
							SobelBitmap8Bpp(scanS, strideS, scanR, strideR, width, height);
							break;
						case PixelFormat.Format24bppRgb:
							SobelBitmap24or32bpp(scanS, strideS, scanR, strideR, width, height, 3);
							break;
						case PixelFormat.Format32bppArgb:
						case PixelFormat.Format32bppRgb:
							SobelBitmap24or32bpp(scanS, strideS, scanR, strideR, width, height, 4);
							break;
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

		#region SobelBitmap8Bpp()
		private static unsafe void SobelBitmap8Bpp(byte* scanS, int strideS, byte* scanR, int strideR, int width, int height)
		{
			int x, y;

			for (y = 1; y < height - 1; y++)
			{
				int sYMinus1 = (y - 1) * strideS;
				int sY = y * strideS;
				int sYPlus1 = (y + 1) * strideS;

				for (x = 1; x < width - 1; x++)
				{
					int topEdge = (scanS[sYMinus1 + x - 1] + 2 * scanS[sYMinus1 + x] + scanS[sYMinus1 + x + 1] -
						scanS[sYPlus1 + x - 1] - 2 * scanS[sYPlus1 + x] - scanS[sYPlus1 + x + 1]);
					int leftEdge = (scanS[sYMinus1 + x - 1] + 2 * scanS[sY + x - 1] + scanS[sYPlus1 + x - 1] -
						scanS[sYMinus1 + x + 1] - 2 * scanS[sY + x + 1] - scanS[sYPlus1 + x + 1]);

					if (topEdge < 0)
						topEdge = -topEdge;
					if (leftEdge < 0)
						leftEdge = -leftEdge;

					scanR[y * strideR + x] = (byte)(((topEdge > leftEdge) ? topEdge : leftEdge) / 4);
				}
			}

			//left, right
			for (y = 1; y < height - 1; y++)
			{
				scanR[y * strideR + 0] = scanR[y * strideR + 1];
				scanR[y * strideR + width - 1] = scanR[y * strideR + width - 2];
			}

			//top, bottom
			for (x = 0; x < width; x++)
			{
				scanR[x] = scanR[strideR + x];
				scanR[(height - 1) * strideR + x] = scanR[(height - 2) * strideR + x];
			}
		}
		#endregion	

		#region SobelBitmap24or32bpp()
		private static unsafe void SobelBitmap24or32bpp(byte* scanS, int strideS, byte* scanR, int strideR, int width, int height, int bpp)
		{
			int x, y;
			byte* pCurrent;
			int edge1R, edge1G, edge1B, edge2B, edge2G, edge2R;
			int edgeB, edgeG, edgeR;
			int biggest;
			int sYMinus1, sY, sYPlus1;
			int f, g, h;

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			for (y = 1; y < height - 1; y++)
			{
				sYMinus1 = (y - 1) * strideS;
				sY = y * strideS;
				sYPlus1 = (y + 1) * strideS;
				
				pCurrent = scanS + (y - 1) * strideS;

				x = 1;
				edge2B = (scanS[sYMinus1] + 2 * scanS[sY] + scanS[sYPlus1]);
				edge2G = (scanS[sYMinus1 + 1] + 2 * scanS[sY + 1] + scanS[sYPlus1 + 1]);
				edge2R = (scanS[sYMinus1 + 2] + 2 * scanS[sY + 2] + scanS[sYPlus1 + 2]);
				for (x = 1; x < width - 1; x = x + 2)
				{
					edge1R = edge2R;
					edge1G = edge2G;
					edge1B = edge2B;
					h = (x + 1) * bpp;

					edge2B = (pCurrent[h] + 2 * pCurrent[strideS + h] + pCurrent[2 * strideS + h]);
					edge2G = (pCurrent[h + 1] + 2 * pCurrent[strideS + h + 1] + pCurrent[2 * strideS + h + 1]);
					edge2R = (pCurrent[h + 2] + 2 * pCurrent[strideS + h + 2] + pCurrent[2 * strideS + h + 2]);

					edgeB = (edge1B > edge2B) ? (edge1B - edge2B) : (edge2B - edge1B);
					edgeG = (edge1G > edge2G) ? (edge1G - edge2G) : (edge2G - edge1G);
					edgeR = (edge1R > edge2R) ? (edge1R - edge2R) : (edge2R - edge1R);

					biggest = (edgeB > edgeG) ? ((edgeB > edgeR) ? edgeB : edgeR) : ((edgeG > edgeR) ? edgeG : edgeR);

					scanR[y * strideR + x] = (byte)(biggest / 4);	
				}

				x = 2;
				edge2B = (pCurrent[bpp    ] + 2 * pCurrent[strideS + bpp    ] + pCurrent[2 * strideS + bpp    ]);
				edge2G = (pCurrent[bpp + 1] + 2 * pCurrent[strideS + bpp + 1] + pCurrent[2 * strideS + bpp + 1]);
				edge2R = (pCurrent[bpp + 2] + 2 * pCurrent[strideS + bpp + 2] + pCurrent[2 * strideS + bpp + 2]);
				for (x = 2; x < width - 1; x = x + 2)
				{
					edge1R = edge2R;
					edge1G = edge2G;
					edge1B = edge2B;
					h = (x + 1) * bpp;

					edge2B = (pCurrent[h    ] + 2 * pCurrent[strideS + h    ] + pCurrent[2 * strideS + h    ]);
					edge2G = (pCurrent[h + 1] + 2 * pCurrent[strideS + h + 1] + pCurrent[2 * strideS + h + 1]);
					edge2R = (pCurrent[h + 2] + 2 * pCurrent[strideS + h + 2] + pCurrent[2 * strideS + h + 2]);

					edgeB = (edge1B > edge2B) ? (edge1B - edge2B) : (edge2B - edge1B);
					edgeG = (edge1G > edge2G) ? (edge1G - edge2G) : (edge2G - edge1G);
					edgeR = (edge1R > edge2R) ? (edge1R - edge2R) : (edge2R - edge1R);

					biggest = (edgeB > edgeG) ? ((edgeB > edgeR) ? edgeB : edgeR) : ((edgeG > edgeR) ? edgeG : edgeR);

					scanR[y * strideR + x] = (byte)(biggest / 4);
				}
			}

#if DEBUG
			Console.WriteLine("EdgeDetector, Vertical: " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif

			for (x = 1; x < width - 1; x++)
			{
				f = (x - 1) * bpp;
				g = x * bpp;
				h = (x + 1) * bpp;
				
				y = 1;
				edge2B = (scanS[f    ] + 2 * scanS[g    ] + scanS[h    ]);
				edge2G = (scanS[f + 1] + 2 * scanS[g + 1] + scanS[h + 1]);
				edge2R = (scanS[f + 2] + 2 * scanS[g + 2] + scanS[h + 2]);
				for (y = 1; y < height - 1; y = y + 2)
				{
					pCurrent = scanS + (y + 1) * strideS;
					
					edge1R = edge2R;
					edge1G = edge2G;
					edge1B = edge2B;

					edge2B = (pCurrent[f    ] + 2 * pCurrent[g    ] + pCurrent[h    ]);
					edge2G = (pCurrent[f + 1] + 2 * pCurrent[g + 1] + pCurrent[h + 1]);
					edge2R = (pCurrent[f + 2] + 2 * pCurrent[g + 2] + pCurrent[h + 2]);

					edgeB = (edge1B > edge2B) ? (edge1B - edge2B) : (edge2B - edge1B);
					edgeG = (edge1G > edge2G) ? (edge1G - edge2G) : (edge2G - edge1G);
					edgeR = (edge1R > edge2R) ? (edge1R - edge2R) : (edge2R - edge1R);

					biggest = (edgeB > edgeG) ? ((edgeB > edgeR) ? edgeB : edgeR) : ((edgeG > edgeR) ? edgeG : edgeR);

					if (scanR[y * strideR + x] < biggest / 4)
						scanR[y * strideR + x] = (byte)(biggest / 4);
				}

				y = 2;
				edge2B = (scanS[strideS + f    ] + 2 * scanS[strideS + g    ] + scanS[strideS + h    ]);
				edge2G = (scanS[strideS + f + 1] + 2 * scanS[strideS + g + 1] + scanS[strideS + h + 1]);
				edge2R = (scanS[strideS + f + 2] + 2 * scanS[strideS + g + 2] + scanS[strideS + h + 2]);
				for (y = 2; y < height - 1; y = y + 2)
				{
					pCurrent = scanS + (y + 1) * strideS;

					edge1R = edge2R;
					edge1G = edge2G;
					edge1B = edge2B;

					edge2B = (pCurrent[f    ] + 2 * pCurrent[g    ] + pCurrent[h    ]);
					edge2G = (pCurrent[f + 1] + 2 * pCurrent[g + 1] + pCurrent[h + 1]);
					edge2R = (pCurrent[f + 2] + 2 * pCurrent[g + 2] + pCurrent[h + 2]);

					edgeB = (edge1B > edge2B) ? (edge1B - edge2B) : (edge2B - edge1B);
					edgeG = (edge1G > edge2G) ? (edge1G - edge2G) : (edge2G - edge1G);
					edgeR = (edge1R > edge2R) ? (edge1R - edge2R) : (edge2R - edge1R);

					biggest = (edgeB > edgeG) ? ((edgeB > edgeR) ? edgeB : edgeR) : ((edgeG > edgeR) ? edgeG : edgeR);

					if (scanR[y * strideR + x] < biggest / 4)
						scanR[y * strideR + x] = (byte)(biggest / 4);
				}
			}

#if DEBUG
			Console.WriteLine("EdgeDetector, Horizontal: " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif

			//left, right
			for (y = 1; y < height - 1; y++)
			{
				scanR[y * strideR + 0] = scanR[y * strideR + 1];
				scanR[y * strideR + width - 1] = scanR[y * strideR + width - 2];
			}

			//top, bottom
			for (x = 0; x < width; x++)
			{
				scanR[x] = scanR[strideR + x];
				scanR[(height - 1) * strideR + x] = scanR[(height - 2) * strideR + x];
			}
		}
		#endregion	

		#region Laplacian()
		private static Bitmap Laplacian(Bitmap source, Rectangle rect)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				int width = rect.Width;
				int height = rect.Height;

				result = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				sourceData = source.LockBits(rect, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, result.PixelFormat);
				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				unsafe
				{
					byte* scanS = (byte*)sourceData.Scan0.ToPointer();
					byte* scanR = (byte*)resultData.Scan0.ToPointer();
					result.Palette = Misc.GetGrayscalePalette();

					switch (source.PixelFormat)
					{
						case PixelFormat.Format8bppIndexed:
							LaplacianBitmap8Bpp(scanS, strideS, scanR, strideR, width, height);
							break;
						case PixelFormat.Format24bppRgb:
							LaplacianBitmap24or32bpp(scanS, strideS, scanR, strideR, width, height, 3);
							break;
						case PixelFormat.Format32bppArgb:
						case PixelFormat.Format32bppRgb:
							LaplacianBitmap24or32bpp(scanS, strideS, scanR, strideR, width, height, 4);
							break;
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

		#region LaplacianBitmap8Bpp()
		private static unsafe void LaplacianBitmap8Bpp(byte* scanS, int strideS, byte* scanR, int strideR, int width, int height)
		{
			int x, y;

			for (y = 1; y < height - 1; y++)
			{
				int sYMinus1 = (y - 1) * strideS;
				int sY = y * strideS;
				int sYPlus1 = (y + 1) * strideS;

				for (x = 1; x < width - 1; x++)
				{
					int edge = (2 * scanS[sYMinus1 + x - 1] - scanS[sYMinus1 + x] + 2 * scanS[sYMinus1 + x + 1] -
						scanS[sY + x - 1] - 4 * scanS[sY + x] - scanS[sY + x + 1] + 
						2 * scanS[sYPlus1 + x - 1] - scanS[sYPlus1 + x] + 2 * scanS[sYPlus1 + x + 1]);

					/*if (edge < 0)
						edge = -edge;*/

					if (edge > 0)
					{
						if (edge > 255)
							scanR[y * strideR + x] = 255;
						else
							scanR[y * strideR + x] = (byte)edge;
					}
				}
			}

			//left, right
			for (y = 1; y < height - 1; y++)
			{
				scanR[y * strideR + 0] = scanR[y * strideR + 1];
				scanR[y * strideR + width - 1] = scanR[y * strideR + width - 2];
			}

			//top, bottom
			for (x = 0; x < width; x++)
			{
				scanR[x] = scanR[strideR + x];
				scanR[(height - 1) * strideR + x] = scanR[(height - 2) * strideR + x];
			}
		}
		#endregion

		#region LaplacianBitmap24or32bpp()
		private static unsafe void LaplacianBitmap24or32bpp(byte* scanS, int strideS, byte* scanR, int strideR, int width, int height, int bpp)
		{
			int x, y;
			int sYMinus1, sY, sYPlus1;
			int biggest = 0;
			int edgeB, edgeG, edgeR;

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			for (y = 1; y < height - 1; y++)
			{
				sYMinus1 = (y - 1) * strideS;
				sY = y * strideS;
				sYPlus1 = (y + 1) * strideS;

				for (x = 1; x < width - 1; x++)
				{
					edgeB = (2 * scanS[sYMinus1 + (x - 1) * 3] - scanS[sYMinus1 + x * 3] + 2 * scanS[sYMinus1 + (x + 1) * 3] -
						scanS[sY + (x - 1) * 3] - 4 * scanS[sY + x * 3] - scanS[sY + (x + 1) * 3] +
						2 * scanS[sYPlus1 + (x - 1) * 3] - scanS[sYPlus1 + x * 3] + 2 * scanS[sYPlus1 + (x + 1) * 3]);
					edgeG = (2 * scanS[sYMinus1 + (x - 1) * 3 + 1] - scanS[sYMinus1 + x * 3 + 1] + 2 * scanS[sYMinus1 + (x + 1) * 3 + 1] -
						scanS[sY + (x - 1) * 3 + 1] - 4 * scanS[sY + x * 3 + 1] - scanS[sY + (x + 1) * 3 + 1] +
						2 * scanS[sYPlus1 + (x - 1) * 3 + 1] - scanS[sYPlus1 + x * 3 + 1] + 2 * scanS[sYPlus1 + (x + 1) * 3 + 1]);
					edgeR = (2 * scanS[sYMinus1 + (x - 1) * 3 + 2] - scanS[sYMinus1 + x * 3 + 2] + 2 * scanS[sYMinus1 + (x + 1) * 3 + 2] -
						scanS[sY + (x - 1) * 3 + 2] - 4 * scanS[sY + x * 3 + 2] - scanS[sY + (x + 1) * 3 + 2] +
						2 * scanS[sYPlus1 + (x - 1) * 3 + 2] - scanS[sYPlus1 + x * 3 + 2] + 2 * scanS[sYPlus1 + (x + 1) * 3 + 2]);

					biggest = (edgeB > edgeG) ? ((edgeB > edgeR) ? edgeB : edgeR) : ((edgeG > edgeR) ? edgeG : edgeR);
					if (biggest > 255)
						biggest = 255;

					if (biggest > 0)
						scanR[y * strideR + x] = (byte)(biggest);
				}
			}

#if DEBUG
			Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif

			//left, right
			for (y = 1; y < height - 1; y++)
			{
				scanR[y * strideR + 0] = scanR[y * strideR + 1];
				scanR[y * strideR + width - 1] = scanR[y * strideR + width - 2];
			}

			//top, bottom
			for (x = 0; x < width; x++)
			{
				scanR[x] = scanR[strideR + x];
				scanR[(height - 1) * strideR + x] = scanR[(height - 2) * strideR + x];
			}
		}
		#endregion

		#region MexicanHat()
		private static Bitmap MexicanHat(Bitmap source, Rectangle rect, Operator type)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				int width = rect.Width;
				int height = rect.Height;

				result = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				sourceData = source.LockBits(rect, ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, result.PixelFormat);
				int strideS = sourceData.Stride;
				int strideR = resultData.Stride;

				unsafe
				{
					byte* scanS = (byte*)sourceData.Scan0.ToPointer();
					byte* scanR = (byte*)resultData.Scan0.ToPointer();
					result.Palette = Misc.GetGrayscalePalette();

					switch (source.PixelFormat)
					{
						case PixelFormat.Format8bppIndexed:
							switch (type)
							{
								case Operator.MexicanHat5x5: MexicanHat5x5_8Bpp(scanS, strideS, scanR, strideR, width, height); break;
								case Operator.MexicanHat17x17: MexicanHat17x17_8bpp(scanS, strideS, scanR, strideR, width, height); break;
							}
							break;
						case PixelFormat.Format24bppRgb:
							switch (type)
							{
								case Operator.MexicanHat5x5: MexicanHat5x5_24bpp(scanS, strideS, scanR, strideR, width, height); break;
								case Operator.MexicanHat7x7: MexicanHat7x7_24bpp(scanS, strideS, scanR, strideR, width, height); break;
								case Operator.MexicanHat17x17: MexicanHat17x17_24bpp(scanS, strideS, scanR, strideR, width, height); break;
							}
							break;
						case PixelFormat.Format32bppArgb:
						case PixelFormat.Format32bppRgb:
							switch (type)
							{
								case Operator.MexicanHat5x5: MexicanHat5x5_32bpp(scanS, strideS, scanR, strideR, width, height); break;
								case Operator.MexicanHat17x17: MexicanHat17x17_32bpp(scanS, strideS, scanR, strideR, width, height); break;
							}
							
							break;
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

			//Bitmap r = Laplacian(result, new Rectangle(0, 0, result.Width, result.Height));

			//result.Dispose();

			return result;
		}
		#endregion

		#region MexicanHat5x5_8Bpp()
		private static unsafe void MexicanHat5x5_8Bpp(byte* scanS, int strideS, byte* scanR, int strideR, int width, int height)
		{
			int x, y;

			for (y = 2; y < height - 2; y++)
			{
				for (x = 2; x < width - 2; x++)
				{
					int edge = -scanS[(y - 2) * strideS + x]
						- scanS[(y - 1) * strideS + x - 1] - 2 * scanS[(y - 1) * strideS + x] - scanS[(y - 1) * strideS + x + 1]
						- scanS[y * strideS + x - 2] - 2 * scanS[y * strideS + x - 1] + 16 * scanS[y * strideS + x] - 2 * scanS[y * strideS + x + 1] - scanS[y * strideS + x + 2]
						- scanS[(y + 1) * strideS + x - 1] - 2 * scanS[(y + 1) * strideS + x] - scanS[(y + 1) * strideS + x + 1]
						- scanS[(y + 2) * strideS + x];

					if (edge < 0)
						scanR[y * strideR + x] = 0;
					else if (edge > 255)
						scanR[y * strideR + x] = 255;
					else
						scanR[y * strideR + x] = (byte)edge;
				}
			}

			//left, right
			for (y = 2; y < height - 2; y++)
			{
				scanR[y * strideR + 0] = scanR[y * strideR + 2];
				scanR[y * strideR + 1] = scanR[y * strideR + 2];
				scanR[y * strideR + width - 1] = scanR[y * strideR + width - 3];
				scanR[y * strideR + width - 2] = scanR[y * strideR + width - 3];
			}

			//top, bottom
			for (x = 0; x < width; x++)
			{
				scanR[x] = scanR[2 * strideR + x];
				scanR[strideR + x] = scanR[2 * strideR + x];
				scanR[(height - 1) * strideR + x] = scanR[(height - 3) * strideR + x];
				scanR[(height - 2) * strideR + x] = scanR[(height - 3) * strideR + x];
			}
		}
		#endregion

		#region MexicanHat5x5_24bpp()
		private static unsafe void MexicanHat5x5_24bpp(byte* scanS, int strideS, byte* scanR, int strideR, int width, int height)
		{
			int x, y;
			int sYMinus1, sY, sYPlus1;
			int edge = 0;
			int edgeB, edgeG, edgeR;

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			for (y = 2; y < height - 2; y++)
			{
				sYMinus1 = (y - 1) * strideS;
				sY = y * strideS;
				sYPlus1 = (y + 1) * strideS;

				for (x = 2; x < width - 2; x++)
				{
					edgeB = -scanS[(y - 2) * strideS + x * 3]
						- scanS[(y - 1) * strideS + (x - 1) * 3] - 2 * scanS[(y - 1) * strideS + x * 3] - scanS[(y - 1) * strideS + (x + 1) * 3]
						- scanS[y * strideS + (x - 2) * 3] - 2 * scanS[y * strideS + (x - 1) * 3] + 16 * scanS[y * strideS + x * 3] - 2 * scanS[y * strideS + (x + 1) * 3] - scanS[y * strideS + (x + 2) * 3]
						- scanS[(y + 1) * strideS + (x - 1) * 3] - 2 * scanS[(y + 1) * strideS + x * 3] - scanS[(y + 1) * strideS + (x + 1) * 3]
						- scanS[(y + 2) * strideS + x * 3];
					edgeG = -scanS[(y - 2) * strideS + x * 3 + 1]
						- scanS[(y - 1) * strideS + (x - 1) * 3 + 1] - 2 * scanS[(y - 1) * strideS + x * 3 + 1] - scanS[(y - 1) * strideS + (x + 1) * 3 + 1]
						- scanS[y * strideS + (x - 2) * 3 + 1] - 2 * scanS[y * strideS + (x - 1) * 3 + 1] + 16 * scanS[y * strideS + x * 3 + 1] - 2 * scanS[y * strideS + (x + 1) * 3 + 1] - scanS[y * strideS + (x + 2) * 3 + 1]
						- scanS[(y + 1) * strideS + (x - 1) * 3 + 1] - 2 * scanS[(y + 1) * strideS + x * 3 + 1] - scanS[(y + 1) * strideS + (x + 1) * 3 + 1]
						- scanS[(y + 2) * strideS + x * 3 + 1];
					edgeR = -scanS[(y - 2) * strideS + x * 3 + 2]
						- scanS[(y - 1) * strideS + (x - 1) * 3 + 2] - 2 * scanS[(y - 1) * strideS + x * 3 + 2] - scanS[(y - 1) * strideS + (x + 1) * 3 + 2]
						- scanS[y * strideS + (x - 2) * 3 + 2] - 2 * scanS[y * strideS + (x - 1) * 3 + 2] + 16 * scanS[y * strideS + x * 3 + 2] - 2 * scanS[y * strideS + (x + 1) * 3 + 2] - scanS[y * strideS + (x + 2) * 3 + 2]
						- scanS[(y + 1) * strideS + (x - 1) * 3 + 2] - 2 * scanS[(y + 1) * strideS + x * 3 + 2] - scanS[(y + 1) * strideS + (x + 1) * 3 + 2]
						- scanS[(y + 2) * strideS + x * 3 + 2];

					edge = (edgeB + edgeG + edgeR) / 3 + 127;

					if (edge < 0)
						scanR[y * strideR + x] = 0;
					else if (edge > 255)
						scanR[y * strideR + x] = 255;
					else
						scanR[y * strideR + x] = (byte)edge;
				}
			}

#if DEBUG
			Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif

			//left, right
			for (y = 2; y < height - 2; y++)
			{
				scanR[y * strideR + 0] = scanR[y * strideR + 2];
				scanR[y * strideR + 1] = scanR[y * strideR + 2];
				scanR[y * strideR + width - 1] = scanR[y * strideR + width - 3];
				scanR[y * strideR + width - 2] = scanR[y * strideR + width - 3];
			}

			//top, bottom
			for (x = 0; x < width; x++)
			{
				scanR[x] = scanR[2 * strideR + x];
				scanR[strideR + x] = scanR[2 * strideR + x];
				scanR[(height - 1) * strideR + x] = scanR[(height - 3) * strideR + x];
				scanR[(height - 2) * strideR + x] = scanR[(height - 3) * strideR + x];
			}
		}
		#endregion

		#region MexicanHat5x5_32bpp()
		private static unsafe void MexicanHat5x5_32bpp(byte* scanS, int strideS, byte* scanR, int strideR, int width, int height)
		{
			int x, y;
			int sYMinus1, sY, sYPlus1;
			int edge = 0;
			int edgeB, edgeG, edgeR;

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			for (y = 2; y < height - 2; y++)
			{
				sYMinus1 = (y - 1) * strideS;
				sY = y * strideS;
				sYPlus1 = (y + 1) * strideS;

				for (x = 2; x < width - 2; x++)
				{
					edgeB = -scanS[(y - 2) * strideS + x * 4]
						- scanS[(y - 1) * strideS + (x - 1) * 4] - 2 * scanS[(y - 1) * strideS + x * 4] - scanS[(y - 1) * strideS + (x + 1) * 4]
						- scanS[y * strideS + (x - 2) * 4] - 2 * scanS[y * strideS + (x - 1) * 4] + 16 * scanS[y * strideS + x * 4] - 2 * scanS[y * strideS + (x + 1) * 4] - scanS[y * strideS + (x + 2) * 4]
						- scanS[(y + 1) * strideS + (x - 1) * 4] - 2 * scanS[(y + 1) * strideS + x * 4] - scanS[(y + 1) * strideS + (x + 1) * 4]
						- scanS[(y + 2) * strideS + x * 4];
					edgeG = -scanS[(y - 2) * strideS + x * 4 + 1]
						- scanS[(y - 1) * strideS + (x - 1) * 4 + 1] - 2 * scanS[(y - 1) * strideS + x * 4 + 1] - scanS[(y - 1) * strideS + (x + 1) * 4 + 1]
						- scanS[y * strideS + (x - 2) * 4 + 1] - 2 * scanS[y * strideS + (x - 1) * 4 + 1] + 16 * scanS[y * strideS + x * 4 + 1] - 2 * scanS[y * strideS + (x + 1) * 4 + 1] - scanS[y * strideS + (x + 2) * 4 + 1]
						- scanS[(y + 1) * strideS + (x - 1) * 4 + 1] - 2 * scanS[(y + 1) * strideS + x * 4 + 1] - scanS[(y + 1) * strideS + (x + 1) * 4 + 1]
						- scanS[(y + 2) * strideS + x * 4 + 1];
					edgeR = -scanS[(y - 2) * strideS + x * 4 + 2]
						- scanS[(y - 1) * strideS + (x - 1) * 4 + 2] - 2 * scanS[(y - 1) * strideS + x * 4 + 2] - scanS[(y - 1) * strideS + (x + 1) * 4 + 2]
						- scanS[y * strideS + (x - 2) * 4 + 2] - 2 * scanS[y * strideS + (x - 1) * 4 + 2] + 16 * scanS[y * strideS + x * 4 + 2] - 2 * scanS[y * strideS + (x + 1) * 4 + 2] - scanS[y * strideS + (x + 2) * 4 + 2]
						- scanS[(y + 1) * strideS + (x - 1) * 4 + 2] - 2 * scanS[(y + 1) * strideS + x * 4 + 2] - scanS[(y + 1) * strideS + (x + 1) * 4 + 2]
						- scanS[(y + 2) * strideS + x * 4 + 2];

					edge = (edgeB + edgeG + edgeR) / 3 + 127;

					if (edge < 0)
						scanR[y * strideR + x] = 0;
					else if (edge > 255)
						scanR[y * strideR + x] = 255;
					else
						scanR[y * strideR + x] = (byte)edge;
				}
			}

#if DEBUG
			Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif

			//left, right
			for (y = 2; y < height - 2; y++)
			{
				scanR[y * strideR + 0] = scanR[y * strideR + 2];
				scanR[y * strideR + 1] = scanR[y * strideR + 2];
				scanR[y * strideR + width - 1] = scanR[y * strideR + width - 3];
				scanR[y * strideR + width - 2] = scanR[y * strideR + width - 3];
			}

			//top, bottom
			for (x = 0; x < width; x++)
			{
				scanR[x] = scanR[2 * strideR + x];
				scanR[strideR + x] = scanR[2 * strideR + x];
				scanR[(height - 1) * strideR + x] = scanR[(height - 3) * strideR + x];
				scanR[(height - 2) * strideR + x] = scanR[(height - 3) * strideR + x];
			}
		}
		#endregion

		#region MexicanHat7x7_24bpp()
		private static unsafe void MexicanHat7x7_24bpp(byte* scanS, int strideS, byte* scanR, int strideR, int width, int height)
		{
			int x, y;
			int edge = 0;
			int edgeB, edgeG, edgeR;

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			for (y = 3; y < height - 3; y++)
			{
				for (x = 3; x < width - 3; x++)
				{
					edgeB =
					16 * (scanS[y * strideS + x * 3])
					+ 5 * (scanS[(y - 1) * strideS + (x - 1) * 3] + scanS[(y - 1) * strideS + (x + 1) * 3] + scanS[(y + 1) * strideS + (x - 1) * 3] + scanS[(y + 1) * strideS + (x + 1) * 3] + scanS[(y - 1) * strideS + x * 3] + scanS[(y + 1) * strideS + x * 3] + scanS[y * strideS + (x - 1) * 3] + scanS[y * strideS + (x + 1) * 3])
					- 3 * (
						scanS[(y - 2) * strideS + x * 3] + scanS[(y + 2) * strideS + x * 3] + scanS[y * strideS + (x - 2) * 3] + scanS[y * strideS + (x + 2) * 3]
						+ scanS[(y - 2) * strideS + (x - 1) * 3] + scanS[(y - 2) * strideS + (x + 1) * 3] + scanS[(y + 2) * strideS + (x - 1) * 3] + scanS[(y + 2) * strideS + (x + 1) * 3]
						+ scanS[(y - 1) * strideS + (x - 2) * 3] + scanS[(y - 1) * strideS + (x + 2) * 3] + scanS[(y + 1) * strideS + (x - 2) * 3] + scanS[(y + 1) * strideS + (x + 2) * 3])
					- 2 * (scanS[(y - 2) * strideS + (x - 2) * 3] + scanS[(y - 2) * strideS + (x + 2) * 3] + scanS[(y + 2) * strideS + (x - 2) * 3] + scanS[(y + 2) * strideS + (x + 2) * 3])
					- scanS[(y - 3) * strideS + (x - 1) * 3] - scanS[(y - 3) * strideS + x * 3] - scanS[(y - 3) * strideS + (x + 1) * 3]
					- scanS[(y - 1) * strideS + (x - 3) * 3] - scanS[(y - 1) * strideS + (x + 3) * 3] - scanS[y * strideS + (x - 3) * 3] - scanS[y * strideS + (x + 3) * 3] - scanS[(y + 1) * strideS + (x - 3) * 3] - scanS[(y + 1) * strideS + (x + 3) * 3]
					- scanS[(y + 3) * strideS + (x - 1) * 3] - scanS[(y + 3) * strideS + x * 3] - scanS[(y + 3) * strideS + (x + 1) * 3];

					edgeG =
					16 * (scanS[y * strideS + x * 3 + 1])
					+ 5 * (scanS[(y - 1) * strideS + (x - 1) * 3 + 1] + scanS[(y - 1) * strideS + (x + 1) * 3 + 1] + scanS[(y + 1) * strideS + (x - 1) * 3 + 1] + scanS[(y + 1) * strideS + (x + 1) * 3 + 1] + scanS[(y - 1) * strideS + x * 3 + 1] + scanS[(y + 1) * strideS + x * 3 + 1] + scanS[y * strideS + (x - 1) * 3 + 1] + scanS[y * strideS + (x + 1) * 3 + 1])
					- 3 * (
						scanS[(y - 2) * strideS + x * 3 + 1] + scanS[(y + 2) * strideS + x * 3 + 1] + scanS[y * strideS + (x - 2) * 3 + 1] + scanS[y * strideS + (x + 2) * 3 + 1]
						+ scanS[(y - 2) * strideS + (x - 1) * 3 + 1] + scanS[(y - 2) * strideS + (x + 1) * 3 + 1] + scanS[(y + 2) * strideS + (x - 1) * 3 + 1] + scanS[(y + 2) * strideS + (x + 1) * 3 + 1]
						+ scanS[(y - 1) * strideS + (x - 2) * 3 + 1] + scanS[(y - 1) * strideS + (x + 2) * 3 + 1] + scanS[(y + 1) * strideS + (x - 2) * 3 + 1] + scanS[(y + 1) * strideS + (x + 2) * 3 + 1])
					- 2 * (scanS[(y - 2) * strideS + (x - 2) * 3 + 1] + scanS[(y - 2) * strideS + (x + 2) * 3 + 1] + scanS[(y + 2) * strideS + (x - 2) * 3 + 1] + scanS[(y + 2) * strideS + (x + 2) * 3 + 1])
					- scanS[(y - 3) * strideS + (x - 1) * 3 + 1] - scanS[(y - 3) * strideS + x * 3 + 1] - scanS[(y - 3) * strideS + (x + 1) * 3 + 1]
					- scanS[(y - 1) * strideS + (x - 3) * 3 + 1] - scanS[(y - 1) * strideS + (x + 3) * 3 + 1] - scanS[y * strideS + (x - 3) * 3 + 1] - scanS[y * strideS + (x + 3) * 3 + 1] - scanS[(y + 1) * strideS + (x - 3) * 3 + 1] - scanS[(y + 1) * strideS + (x + 3) * 3 + 1]
					- scanS[(y + 3) * strideS + (x - 1) * 3 + 1] - scanS[(y + 3) * strideS + x * 3 + 1] - scanS[(y + 3) * strideS + (x + 1) * 3 + 1];

					edgeR =
					16 * (scanS[y*strideS + x*3+2])
					+ 5 * (scanS[(y-1)*strideS + (x-1)*3+2] + scanS[(y-1)*strideS + (x+1)*3+2] + scanS[(y+1)*strideS + (x-1)*3+2] + scanS[(y+1)*strideS + (x+1)*3+2] + scanS[(y-1)*strideS + x*3+2] + scanS[(y+1)*strideS + x*3+2] + scanS[y*strideS + (x-1)*3+2] + scanS[y*strideS + (x+1)*3+2])
					- 3 * (
						scanS[(y-2)*strideS + x*3+2] + scanS[(y+2)*strideS + x*3+2] + scanS[y*strideS + (x-2)*3+2] + scanS[y*strideS + (x+2)*3+2] 
						+ scanS[(y-2)*strideS + (x-1)*3+2] + scanS[(y-2)*strideS + (x+1)*3+2] + scanS[(y+2)*strideS + (x-1)*3+2] + scanS[(y+2)*strideS + (x+1)*3+2]
						+ scanS[(y-1)*strideS + (x-2)*3+2] + scanS[(y-1)*strideS + (x+2)*3+2] + scanS[(y+1)*strideS + (x-2)*3+2] + scanS[(y+1)*strideS + (x+2)*3+2])
					- 2 * (scanS[(y-2)*strideS + (x-2)*3+2] + scanS[(y-2)*strideS + (x+2)*3+2] + scanS[(y+2)*strideS + (x-2)*3+2] + scanS[(y+2)*strideS + (x+2)*3+2])
					- scanS[(y-3)*strideS + (x-1)*3+2] - scanS[(y-3)*strideS + x*3+2] - scanS[(y-3)*strideS + (x+1)*3+2] 
					- scanS[(y-1)*strideS + (x-3)*3+2] - scanS[(y-1)*strideS + (x+3)*3+2] - scanS[y*strideS + (x-3)*3+2] - scanS[y*strideS + (x+3)*3+2] - scanS[(y+1)*strideS + (x-3)*3+2] - scanS[(y+1)*strideS + (x+3)*3+2]
					- scanS[(y+3)*strideS + (x-1)*3+2] - scanS[(y+3)*strideS + x*3+2] - scanS[(y+3)*strideS + (x+1)*3+2] ;

					edge = (edgeB + edgeG + edgeR) / 60 + 127;

					if (edge < 0)
						scanR[y * strideR + x] = 0;
					else if (edge > 255)
						scanR[y * strideR + x] = 255;
					else
						scanR[y * strideR + x] = (byte)edge;
				}
			}

#if DEBUG
			Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif

			//left, right
			for (y = 3; y < height - 3; y++)
				for (x = 0; x < 3; x++)
				{
					scanR[y * strideR + x] = scanR[y * strideR + 3];
					scanR[y * strideR + width - 1 - x] = scanR[y * strideR + width - 4];
				}

			//top, bottom
			for (x = 0; x < width; x++)
				for (y = 0; y < 3; y++)
				{
					scanR[y * strideR + x] = scanR[8 * strideR + x];
					scanR[(height - 1 - y) * strideR + x] = scanR[(height - 9) * strideR + x];
				}
		}
		#endregion

		#region MexicanHat17x17_8bpp()
		private static unsafe void MexicanHat17x17_8bpp(byte* scanS, int strideS, byte* scanR, int strideR, int width, int height)
		{
			int x, y;
			int edge = 0;

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			for (y = 8; y < height - 8; y++)
			{

				for (x = 8; x < width - 8; x++)
				{
					edge =
																																																									-01 * scanS[(y - 8) * strideS + (x - 2)] - 01 * scanS[(y - 8) * strideS + (x - 1)] - 01 * scanS[(y - 8) * strideS + x] - 01 * scanS[(y - 8) * strideS + (x + 1)] - 01 * scanS[(y - 8) * strideS + (x + 2)]
																																								- 1 * scanS[(y - 7) * strideS + (x - 4)] - 1 * scanS[(y - 7) * strideS + (x - 3)] - 01 * scanS[(y - 7) * strideS + (x - 2)] - 01 * scanS[(y - 7) * strideS + (x - 1)] - 01 * scanS[(y - 7) * strideS + x] - 01 * scanS[(y - 7) * strideS + (x + 1)] - 01 * scanS[(y - 7) * strideS + (x + 2)] - 1 * scanS[(y - 7) * strideS + (x + 3)] - 1 * scanS[(y - 7) * strideS + (x + 4)]
																									      - 1 * scanS[(y - 6) * strideS + (x - 6)] - 1 * scanS[(y - 6) * strideS + (x - 5)] - 1 * scanS[(y - 6) * strideS + (x - 4)] - 2 * scanS[(y - 6) * strideS + (x - 3)] - 03 * scanS[(y - 6) * strideS + (x - 2)] - 03 * scanS[(y - 6) * strideS + (x - 1)] - 03 * scanS[(y - 6) * strideS + x] - 03 * scanS[(y - 6) * strideS + (x + 1)] - 03 * scanS[(y - 6) * strideS + (x + 2)] - 2 * scanS[(y - 6) * strideS + (x + 3)] - 1 * scanS[(y - 6) * strideS + (x + 4)] - 1 * scanS[(y - 6) * strideS + (x + 5)] - 1 * scanS[(y - 6) * strideS + (x + 6)]
																									      - 1 * scanS[(y - 5) * strideS + (x - 6)] - 1 * scanS[(y - 5) * strideS + (x - 5)] - 2 * scanS[(y - 5) * strideS + (x - 4)] - 3 * scanS[(y - 5) * strideS + (x - 3)] - 03 * scanS[(y - 5) * strideS + (x - 2)] - 03 * scanS[(y - 5) * strideS + (x - 1)] - 03 * scanS[(y - 5) * strideS + x] - 03 * scanS[(y - 5) * strideS + (x + 1)] - 03 * scanS[(y - 5) * strideS + (x + 2)] - 3 * scanS[(y - 5) * strideS + (x + 3)] - 2 * scanS[(y - 5) * strideS + (x + 4)] - 1 * scanS[(y - 5) * strideS + (x + 5)] - 1 * scanS[(y - 5) * strideS + (x + 6)]
															     - 1 * scanS[(y - 4) * strideS + (x - 7)] - 1 * scanS[(y - 4) * strideS + (x - 6)] - 2 * scanS[(y - 4) * strideS + (x - 5)] - 3 * scanS[(y - 4) * strideS + (x - 4)] - 3 * scanS[(y - 4) * strideS + (x - 3)] - 03 * scanS[(y - 4) * strideS + (x - 2)] - 02 * scanS[(y - 4) * strideS + (x - 1)] - 03 * scanS[(y - 4) * strideS + x] - 02 * scanS[(y - 4) * strideS + (x + 1)] - 03 * scanS[(y - 4) * strideS + (x + 2)] - 3 * scanS[(y - 4) * strideS + (x + 3)] - 3 * scanS[(y - 4) * strideS + (x + 4)] - 2 * scanS[(y - 4) * strideS + (x + 5)] - 1 * scanS[(y - 4) * strideS + (x + 6)] - 1 * scanS[(y - 4) * strideS + (x + 7)]
																 - 1 * scanS[(y - 3) * strideS + (x - 7)] - 2 * scanS[(y - 3) * strideS + (x - 6)] - 3 * scanS[(y - 3) * strideS + (x - 5)] - 3 * scanS[(y - 3) * strideS + (x - 4)] - 3 * scanS[(y - 3) * strideS + (x - 3)] + 02 * scanS[(y - 3) * strideS + (x - 1)] + 04 * scanS[(y - 3) * strideS + x] + 02 * scanS[(y - 3) * strideS + (x + 1)] - 3 * scanS[(y - 3) * strideS + (x + 3)] - 3 * scanS[(y - 3) * strideS + (x + 4)] - 3 * scanS[(y - 3) * strideS + (x + 5)] - 2 * scanS[(y - 3) * strideS + (x + 6)] - 1 * scanS[(y - 3) * strideS + (x + 7)]
						- 1 * scanS[(y - 2) * strideS + (x - 8)] - 1 * scanS[(y - 2) * strideS + (x - 7)] - 3 * scanS[(y - 2) * strideS + (x - 6)] - 3 * scanS[(y - 2) * strideS + (x - 5)] - 3 * scanS[(y - 2) * strideS + (x - 4)] + 04 * scanS[(y - 2) * strideS + (x - 2)] + 10 * scanS[(y - 2) * strideS + (x - 1)] + 12 * scanS[(y - 2) * strideS + x] + 10 * scanS[(y - 2) * strideS + (x + 1)] + 04 * scanS[(y - 2) * strideS + (x + 2)] - 3 * scanS[(y - 2) * strideS + (x + 4)] - 3 * scanS[(y - 2) * strideS + (x + 5)] - 3 * scanS[(y - 2) * strideS + (x + 6)] - 1 * scanS[(y - 2) * strideS + (x + 7)] - 1 * scanS[(y - 2) * strideS + (x + 8)]
						- 1 * scanS[(y - 1) * strideS + (x - 8)] - 1 * scanS[(y - 1) * strideS + (x - 7)] - 3 * scanS[(y - 1) * strideS + (x - 6)] - 3 * scanS[(y - 1) * strideS + (x - 5)] - 2 * scanS[(y - 1) * strideS + (x - 4)] + 2 * scanS[(y - 1) * strideS + (x - 3)] + 10 * scanS[(y - 1) * strideS + (x - 2)] + 18 * scanS[(y - 1) * strideS + (x - 1)] + 21 * scanS[(y - 1) * strideS + x] + 18 * scanS[(y - 1) * strideS + (x + 1)] + 10 * scanS[(y - 1) * strideS + (x + 2)] + 2 * scanS[(y - 1) * strideS + (x + 3)] - 2 * scanS[(y - 1) * strideS + (x + 4)] - 3 * scanS[(y - 1) * strideS + (x + 5)] - 3 * scanS[(y - 1) * strideS + (x + 6)] - 1 * scanS[(y - 1) * strideS + (x + 7)] - 1 * scanS[(y - 1) * strideS + (x + 8)]
						- 1 * scanS[(y + 0) * strideS + (x - 8)] - 1 * scanS[(y + 0) * strideS + (x - 7)] - 3 * scanS[(y + 0) * strideS + (x - 6)] - 3 * scanS[(y + 0) * strideS + (x - 5)] - 3 * scanS[(y + 0) * strideS + (x - 4)] + 4 * scanS[(y + 0) * strideS + (x - 3)] + 12 * scanS[(y + 0) * strideS + (x - 2)] + 21 * scanS[(y + 0) * strideS + (x - 1)] + 24 * scanS[(y + 0) * strideS + x] + 21 * scanS[(y + 0) * strideS + (x + 1)] + 12 * scanS[(y + 0) * strideS + (x + 2)] + 4 * scanS[(y + 0) * strideS + (x + 3)] - 3 * scanS[(y + 0) * strideS + (x + 4)] - 3 * scanS[(y + 0) * strideS + (x + 5)] - 3 * scanS[(y + 0) * strideS + (x + 6)] - 1 * scanS[(y + 0) * strideS + (x + 7)] - 1 * scanS[(y + 0) * strideS + (x + 8)]
						- 1 * scanS[(y + 1) * strideS + (x - 8)] - 1 * scanS[(y + 1) * strideS + (x - 7)] - 3 * scanS[(y + 1) * strideS + (x - 6)] - 3 * scanS[(y + 1) * strideS + (x - 5)] - 2 * scanS[(y + 1) * strideS + (x - 4)] + 2 * scanS[(y + 1) * strideS + (x - 3)] + 10 * scanS[(y + 1) * strideS + (x - 2)] + 18 * scanS[(y + 1) * strideS + (x - 1)] + 21 * scanS[(y + 1) * strideS + x] + 18 * scanS[(y + 1) * strideS + (x + 1)] + 10 * scanS[(y + 1) * strideS + (x + 2)] + 2 * scanS[(y + 1) * strideS + (x + 3)] - 2 * scanS[(y + 1) * strideS + (x + 4)] - 3 * scanS[(y + 1) * strideS + (x + 5)] - 3 * scanS[(y + 1) * strideS + (x + 6)] - 1 * scanS[(y + 1) * strideS + (x + 7)] - 1 * scanS[(y + 1) * strideS + (x + 8)]
						- 1 * scanS[(y + 2) * strideS + (x - 8)] - 1 * scanS[(y + 2) * strideS + (x - 7)] - 3 * scanS[(y + 2) * strideS + (x - 6)] - 3 * scanS[(y + 2) * strideS + (x - 5)] - 3 * scanS[(y + 2) * strideS + (x - 4)] + 04 * scanS[(y + 2) * strideS + (x - 2)] + 10 * scanS[(y + 2) * strideS + (x - 1)] + 12 * scanS[(y + 2) * strideS + x] + 10 * scanS[(y + 2) * strideS + (x + 1)] + 04 * scanS[(y + 2) * strideS + (x + 2)] - 3 * scanS[(y + 2) * strideS + (x + 4)] - 3 * scanS[(y + 2) * strideS + (x + 5)] - 3 * scanS[(y + 2) * strideS + (x + 6)] - 1 * scanS[(y + 2) * strideS + (x + 7)] - 1 * scanS[(y + 2) * strideS + (x + 8)]
																 - 1 * scanS[(y + 3) * strideS + (x - 7)] - 2 * scanS[(y + 3) * strideS + (x - 6)] - 3 * scanS[(y + 3) * strideS + (x - 5)] - 3 * scanS[(y + 3) * strideS + (x - 4)] - 3 * scanS[(y + 3) * strideS + (x - 3)] + 02 * scanS[(y + 3) * strideS + (x - 1)] + 04 * scanS[(y + 3) * strideS + x] + 02 * scanS[(y + 3) * strideS + (x + 1)] - 3 * scanS[(y + 3) * strideS + (x + 3)] - 3 * scanS[(y + 3) * strideS + (x + 4)] - 3 * scanS[(y + 3) * strideS + (x + 5)] - 2 * scanS[(y + 3) * strideS + (x + 6)] - 1 * scanS[(y + 3) * strideS + (x + 7)]
																 - 1 * scanS[(y + 4) * strideS + (x - 7)] - 1 * scanS[(y + 4) * strideS + (x - 6)] - 2 * scanS[(y + 4) * strideS + (x - 5)] - 3 * scanS[(y + 4) * strideS + (x - 4)] - 3 * scanS[(y + 4) * strideS + (x - 3)] - 03 * scanS[(y + 4) * strideS + (x - 2)] - 02 * scanS[(y + 4) * strideS + (x - 1)] - 03 * scanS[(y + 4) * strideS + x] - 02 * scanS[(y + 4) * strideS + (x + 1)] - 03 * scanS[(y + 4) * strideS + (x + 2)] - 3 * scanS[(y + 4) * strideS + (x + 3)] - 3 * scanS[(y + 4) * strideS + (x + 4)] - 2 * scanS[(y + 4) * strideS + (x + 5)] - 1 * scanS[(y + 4) * strideS + (x + 6)] - 1 * scanS[(y + 4) * strideS + (x + 7)]
																								    	  - 1 * scanS[(y + 5) * strideS + (x - 6)] - 1 * scanS[(y + 5) * strideS + (x - 5)] - 2 * scanS[(y + 5) * strideS + (x - 4)] - 3 * scanS[(y + 5) * strideS + (x - 3)] - 03 * scanS[(y + 5) * strideS + (x - 2)] - 03 * scanS[(y + 5) * strideS + (x - 1)] - 03 * scanS[(y + 5) * strideS + x] - 03 * scanS[(y + 5) * strideS + (x + 1)] - 03 * scanS[(y + 5) * strideS + (x + 2)] - 3 * scanS[(y + 5) * strideS + (x + 3)] - 2 * scanS[(y + 5) * strideS + (x + 4)] - 1 * scanS[(y + 5) * strideS + (x + 5)] - 1 * scanS[(y + 5) * strideS + (x + 6)]
																							              - 1 * scanS[(y + 6) * strideS + (x - 6)] - 1 * scanS[(y + 6) * strideS + (x - 5)] - 1 * scanS[(y + 6) * strideS + (x - 4)] - 2 * scanS[(y + 6) * strideS + (x - 3)] - 03 * scanS[(y + 6) * strideS + (x - 2)] - 03 * scanS[(y + 6) * strideS + (x - 1)] - 03 * scanS[(y + 6) * strideS + x] - 03 * scanS[(y + 6) * strideS + (x + 1)] - 03 * scanS[(y + 6) * strideS + (x + 2)] - 2 * scanS[(y + 6) * strideS + (x + 3)] - 1 * scanS[(y + 6) * strideS + (x + 4)] - 1 * scanS[(y + 6) * strideS + (x + 5)] - 1 * scanS[(y + 6) * strideS + (x + 6)]
																																								- 1 * scanS[(y + 7) * strideS + (x - 4)] - 1 * scanS[(y + 7) * strideS + (x - 3)] - 01 * scanS[(y + 7) * strideS + (x - 2)] - 01 * scanS[(y + 7) * strideS + (x - 1)] - 01 * scanS[(y + 7) * strideS + x] - 01 * scanS[(y + 7) * strideS + (x + 1)] - 01 * scanS[(y + 7) * strideS + (x + 2)] - 1 * scanS[(y + 7) * strideS + (x + 3)] - 1 * scanS[(y + 7) * strideS + (x + 4)]
																																																									- 01 * scanS[(y + 8) * strideS + (x - 2)] - 01 * scanS[(y + 8) * strideS + (x - 1)] - 01 * scanS[(y + 8) * strideS + x] - 01 * scanS[(y + 8) * strideS + (x + 1)] - 01 * scanS[(y + 8) * strideS + (x + 2)]

						;

					edge = edge / 166 + 127;

					if (edge < 0)
						scanR[y * strideR + x] = 0;
					else if (edge > 255)
						scanR[y * strideR + x] = 255;
					else
						scanR[y * strideR + x] = (byte)edge;
				}
			}

#if DEBUG
			Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif

			//left, right
			for (y = 8; y < height - 8; y++)
				for (x = 0; x < 8; x++)
				{
					scanR[y * strideR + x] = scanR[y * strideR + 8];
					scanR[y * strideR + width - 1 - x] = scanR[y * strideR + width - 9];
				}

			//top, bottom
			for (x = 0; x < width; x++)
				for (y = 0; y < 8; y++)
				{
					scanR[y * strideR + x] = scanR[8 * strideR + x];
					scanR[(height - 1 - y) * strideR + x] = scanR[(height - 9) * strideR + x];
				}
		}
		#endregion

		#region MexicanHat17x17_24bpp()
		private static unsafe void MexicanHat17x17_24bpp(byte* scanS, int strideS, byte* scanR, int strideR, int width, int height)
		{
			int x, y;
			int edge = 0;
			int edgeB, edgeG, edgeR;

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			for (y = 8; y < height - 8; y++)
			{

				for (x = 8; x < width - 8; x++)
				{				
					edgeB =
																																																									-01 * scanS[(y - 8) * strideS + (x - 2) * 3] - 01 * scanS[(y - 8) * strideS + (x - 1) * 3] - 01 * scanS[(y - 8) * strideS + x * 3] - 01 * scanS[(y - 8) * strideS + (x + 1) * 3] - 01 * scanS[(y - 8) * strideS + (x + 2) * 3]
																																								- 1 * scanS[(y - 7) * strideS + (x - 4) * 3] - 1 * scanS[(y - 7) * strideS + (x - 3) * 3] - 01 * scanS[(y - 7) * strideS + (x - 2) * 3] - 01 * scanS[(y - 7) * strideS + (x - 1) * 3] - 01 * scanS[(y - 7) * strideS + x * 3] - 01 * scanS[(y - 7) * strideS + (x + 1) * 3] - 01 * scanS[(y - 7) * strideS + (x + 2) * 3] - 1 * scanS[(y - 7) * strideS + (x + 3) * 3] - 1 * scanS[(y - 7) * strideS + (x + 4) * 3]
																							- 1 * scanS[(y - 6) * strideS + (x - 6) * 3] - 1 * scanS[(y - 6) * strideS + (x - 5) * 3] - 1 * scanS[(y - 6) * strideS + (x - 4) * 3] - 2 * scanS[(y - 6) * strideS + (x - 3) * 3] - 03 * scanS[(y - 6) * strideS + (x - 2) * 3] - 03 * scanS[(y - 6) * strideS + (x - 1) * 3] - 03 * scanS[(y - 6) * strideS + x * 3] - 03 * scanS[(y - 6) * strideS + (x + 1) * 3] - 03 * scanS[(y - 6) * strideS + (x + 2) * 3] - 2 * scanS[(y - 6) * strideS + (x + 3) * 3] - 1 * scanS[(y - 6) * strideS + (x + 4) * 3] - 1 * scanS[(y - 6) * strideS + (x + 5) * 3] - 1 * scanS[(y - 6) * strideS + (x + 6) * 3]
																							- 1 * scanS[(y - 5) * strideS + (x - 6) * 3] - 1 * scanS[(y - 5) * strideS + (x - 5) * 3] - 2 * scanS[(y - 5) * strideS + (x - 4) * 3] - 3 * scanS[(y - 5) * strideS + (x - 3) * 3] - 03 * scanS[(y - 5) * strideS + (x - 2) * 3] - 03 * scanS[(y - 5) * strideS + (x - 1) * 3] - 03 * scanS[(y - 5) * strideS + x * 3] - 03 * scanS[(y - 5) * strideS + (x + 1) * 3] - 03 * scanS[(y - 5) * strideS + (x + 2) * 3] - 3 * scanS[(y - 5) * strideS + (x + 3) * 3] - 2 * scanS[(y - 5) * strideS + (x + 4) * 3] - 1 * scanS[(y - 5) * strideS + (x + 5) * 3] - 1 * scanS[(y - 5) * strideS + (x + 6) * 3]
														  - 1 * scanS[(y - 4) * strideS + (x - 7) * 3] - 1 * scanS[(y - 4) * strideS + (x - 6) * 3] - 2 * scanS[(y - 4) * strideS + (x - 5) * 3] - 3 * scanS[(y - 4) * strideS + (x - 4) * 3] - 3 * scanS[(y - 4) * strideS + (x - 3) * 3] - 03 * scanS[(y - 4) * strideS + (x - 2) * 3] - 02 * scanS[(y - 4) * strideS + (x - 1) * 3] - 03 * scanS[(y - 4) * strideS + x * 3] - 02 * scanS[(y - 4) * strideS + (x + 1) * 3] - 03 * scanS[(y - 4) * strideS + (x + 2) * 3] - 3 * scanS[(y - 4) * strideS + (x + 3) * 3] - 3 * scanS[(y - 4) * strideS + (x + 4) * 3] - 2 * scanS[(y - 4) * strideS + (x + 5) * 3] - 1 * scanS[(y - 4) * strideS + (x + 6) * 3] - 1 * scanS[(y - 4) * strideS + (x + 7) * 3]
														  - 1 * scanS[(y - 3) * strideS + (x - 7) * 3] - 2 * scanS[(y - 3) * strideS + (x - 6) * 3] - 3 * scanS[(y - 3) * strideS + (x - 5) * 3] - 3 * scanS[(y - 3) * strideS + (x - 4) * 3] - 3 * scanS[(y - 3) * strideS + (x - 3) * 3] + 02 * scanS[(y - 3) * strideS + (x - 1) * 3] + 04 * scanS[(y - 3) * strideS + x * 3] + 02 * scanS[(y - 3) * strideS + (x + 1) * 3] - 3 * scanS[(y - 3) * strideS + (x + 3) * 3] - 3 * scanS[(y - 3) * strideS + (x + 4) * 3] - 3 * scanS[(y - 3) * strideS + (x + 5) * 3] - 2 * scanS[(y - 3) * strideS + (x + 6) * 3] - 1 * scanS[(y - 3) * strideS + (x + 7) * 3]
						- 1 * scanS[(y - 2) * strideS + (x - 8) * 3] - 1 * scanS[(y - 2) * strideS + (x - 7) * 3] - 3 * scanS[(y - 2) * strideS + (x - 6) * 3] - 3 * scanS[(y - 2) * strideS + (x - 5) * 3] - 3 * scanS[(y - 2) * strideS + (x - 4) * 3] + 04 * scanS[(y - 2) * strideS + (x - 2) * 3] + 10 * scanS[(y - 2) * strideS + (x - 1) * 3] + 12 * scanS[(y - 2) * strideS + x * 3] + 10 * scanS[(y - 2) * strideS + (x + 1) * 3] + 04 * scanS[(y - 2) * strideS + (x + 2) * 3] - 3 * scanS[(y - 2) * strideS + (x + 4) * 3] - 3 * scanS[(y - 2) * strideS + (x + 5) * 3] - 3 * scanS[(y - 2) * strideS + (x + 6) * 3] - 1 * scanS[(y - 2) * strideS + (x + 7) * 3] - 1 * scanS[(y - 2) * strideS + (x + 8) * 3]
						- 1 * scanS[(y - 1) * strideS + (x - 8) * 3] - 1 * scanS[(y - 1) * strideS + (x - 7) * 3] - 3 * scanS[(y - 1) * strideS + (x - 6) * 3] - 3 * scanS[(y - 1) * strideS + (x - 5) * 3] - 2 * scanS[(y - 1) * strideS + (x - 4) * 3] + 2 * scanS[(y - 1) * strideS + (x - 3) * 3] + 10 * scanS[(y - 1) * strideS + (x - 2) * 3] + 18 * scanS[(y - 1) * strideS + (x - 1) * 3] + 21 * scanS[(y - 1) * strideS + x * 3] + 18 * scanS[(y - 1) * strideS + (x + 1) * 3] + 10 * scanS[(y - 1) * strideS + (x + 2) * 3] + 2 * scanS[(y - 1) * strideS + (x + 3) * 3] - 2 * scanS[(y - 1) * strideS + (x + 4) * 3] - 3 * scanS[(y - 1) * strideS + (x + 5) * 3] - 3 * scanS[(y - 1) * strideS + (x + 6) * 3] - 1 * scanS[(y - 1) * strideS + (x + 7) * 3] - 1 * scanS[(y - 1) * strideS + (x + 8) * 3]
						- 1 * scanS[(y + 0) * strideS + (x - 8) * 3] - 1 * scanS[(y + 0) * strideS + (x - 7) * 3] - 3 * scanS[(y + 0) * strideS + (x - 6) * 3] - 3 * scanS[(y + 0) * strideS + (x - 5) * 3] - 3 * scanS[(y + 0) * strideS + (x - 4) * 3] + 4 * scanS[(y + 0) * strideS + (x - 3) * 3] + 12 * scanS[(y + 0) * strideS + (x - 2) * 3] + 21 * scanS[(y + 0) * strideS + (x - 1) * 3] + 24 * scanS[(y + 0) * strideS + x * 3] + 21 * scanS[(y + 0) * strideS + (x + 1) * 3] + 12 * scanS[(y + 0) * strideS + (x + 2) * 3] + 4 * scanS[(y + 0) * strideS + (x + 3) * 3] - 3 * scanS[(y + 0) * strideS + (x + 4) * 3] - 3 * scanS[(y + 0) * strideS + (x + 5) * 3] - 3 * scanS[(y + 0) * strideS + (x + 6) * 3] - 1 * scanS[(y + 0) * strideS + (x + 7) * 3] - 1 * scanS[(y + 0) * strideS + (x + 8) * 3]
						- 1 * scanS[(y + 1) * strideS + (x - 8) * 3] - 1 * scanS[(y + 1) * strideS + (x - 7) * 3] - 3 * scanS[(y + 1) * strideS + (x - 6) * 3] - 3 * scanS[(y + 1) * strideS + (x - 5) * 3] - 2 * scanS[(y + 1) * strideS + (x - 4) * 3] + 2 * scanS[(y + 1) * strideS + (x - 3) * 3] + 10 * scanS[(y + 1) * strideS + (x - 2) * 3] + 18 * scanS[(y + 1) * strideS + (x - 1) * 3] + 21 * scanS[(y + 1) * strideS + x * 3] + 18 * scanS[(y + 1) * strideS + (x + 1) * 3] + 10 * scanS[(y + 1) * strideS + (x + 2) * 3] + 2 * scanS[(y + 1) * strideS + (x + 3) * 3] - 2 * scanS[(y + 1) * strideS + (x + 4) * 3] - 3 * scanS[(y + 1) * strideS + (x + 5) * 3] - 3 * scanS[(y + 1) * strideS + (x + 6) * 3] - 1 * scanS[(y + 1) * strideS + (x + 7) * 3] - 1 * scanS[(y + 1) * strideS + (x + 8) * 3]
						- 1 * scanS[(y + 2) * strideS + (x - 8) * 3] - 1 * scanS[(y + 2) * strideS + (x - 7) * 3] - 3 * scanS[(y + 2) * strideS + (x - 6) * 3] - 3 * scanS[(y + 2) * strideS + (x - 5) * 3] - 3 * scanS[(y + 2) * strideS + (x - 4) * 3] + 04 * scanS[(y + 2) * strideS + (x - 2) * 3] + 10 * scanS[(y + 2) * strideS + (x - 1) * 3] + 12 * scanS[(y + 2) * strideS + x * 3] + 10 * scanS[(y + 2) * strideS + (x + 1) * 3] + 04 * scanS[(y + 2) * strideS + (x + 2) * 3] - 3 * scanS[(y + 2) * strideS + (x + 4) * 3] - 3 * scanS[(y + 2) * strideS + (x + 5) * 3] - 3 * scanS[(y + 2) * strideS + (x + 6) * 3] - 1 * scanS[(y + 2) * strideS + (x + 7) * 3] - 1 * scanS[(y + 2) * strideS + (x + 8) * 3]
														  - 1 * scanS[(y + 3) * strideS + (x - 7) * 3] - 2 * scanS[(y + 3) * strideS + (x - 6) * 3] - 3 * scanS[(y + 3) * strideS + (x - 5) * 3] - 3 * scanS[(y + 3) * strideS + (x - 4) * 3] - 3 * scanS[(y + 3) * strideS + (x - 3) * 3] + 02 * scanS[(y + 3) * strideS + (x - 1) * 3] + 04 * scanS[(y + 3) * strideS + x * 3] + 02 * scanS[(y + 3) * strideS + (x + 1) * 3] - 3 * scanS[(y + 3) * strideS + (x + 3) * 3] - 3 * scanS[(y + 3) * strideS + (x + 4) * 3] - 3 * scanS[(y + 3) * strideS + (x + 5) * 3] - 2 * scanS[(y + 3) * strideS + (x + 6) * 3] - 1 * scanS[(y + 3) * strideS + (x + 7) * 3]
														  - 1 * scanS[(y + 4) * strideS + (x - 7) * 3] - 1 * scanS[(y + 4) * strideS + (x - 6) * 3] - 2 * scanS[(y + 4) * strideS + (x - 5) * 3] - 3 * scanS[(y + 4) * strideS + (x - 4) * 3] - 3 * scanS[(y + 4) * strideS + (x - 3) * 3] - 03 * scanS[(y + 4) * strideS + (x - 2) * 3] - 02 * scanS[(y + 4) * strideS + (x - 1) * 3] - 03 * scanS[(y + 4) * strideS + x * 3] - 02 * scanS[(y + 4) * strideS + (x + 1) * 3] - 03 * scanS[(y + 4) * strideS + (x + 2) * 3] - 3 * scanS[(y + 4) * strideS + (x + 3) * 3] - 3 * scanS[(y + 4) * strideS + (x + 4) * 3] - 2 * scanS[(y + 4) * strideS + (x + 5) * 3] - 1 * scanS[(y + 4) * strideS + (x + 6) * 3] - 1 * scanS[(y + 4) * strideS + (x + 7) * 3]
																							- 1 * scanS[(y + 5) * strideS + (x - 6) * 3] - 1 * scanS[(y + 5) * strideS + (x - 5) * 3] - 2 * scanS[(y + 5) * strideS + (x - 4) * 3] - 3 * scanS[(y + 5) * strideS + (x - 3) * 3] - 03 * scanS[(y + 5) * strideS + (x - 2) * 3] - 03 * scanS[(y + 5) * strideS + (x - 1) * 3] - 03 * scanS[(y + 5) * strideS + x * 3] - 03 * scanS[(y + 5) * strideS + (x + 1) * 3] - 03 * scanS[(y + 5) * strideS + (x + 2) * 3] - 3 * scanS[(y + 5) * strideS + (x + 3) * 3] - 2 * scanS[(y + 5) * strideS + (x + 4) * 3] - 1 * scanS[(y + 5) * strideS + (x + 5) * 3] - 1 * scanS[(y + 5) * strideS + (x + 6) * 3]
																							- 1 * scanS[(y + 6) * strideS + (x - 6) * 3] - 1 * scanS[(y + 6) * strideS + (x - 5) * 3] - 1 * scanS[(y + 6) * strideS + (x - 4) * 3] - 2 * scanS[(y + 6) * strideS + (x - 3) * 3] - 03 * scanS[(y + 6) * strideS + (x - 2) * 3] - 03 * scanS[(y + 6) * strideS + (x - 1) * 3] - 03 * scanS[(y + 6) * strideS + x * 3] - 03 * scanS[(y + 6) * strideS + (x + 1) * 3] - 03 * scanS[(y + 6) * strideS + (x + 2) * 3] - 2 * scanS[(y + 6) * strideS + (x + 3) * 3] - 1 * scanS[(y + 6) * strideS + (x + 4) * 3] - 1 * scanS[(y + 6) * strideS + (x + 5) * 3] - 1 * scanS[(y + 6) * strideS + (x + 6) * 3]
																																								- 1 * scanS[(y + 7) * strideS + (x - 4) * 3] - 1 * scanS[(y + 7) * strideS + (x - 3) * 3] - 01 * scanS[(y + 7) * strideS + (x - 2) * 3] - 01 * scanS[(y + 7) * strideS + (x - 1) * 3] - 01 * scanS[(y + 7) * strideS + x * 3] - 01 * scanS[(y + 7) * strideS + (x + 1) * 3] - 01 * scanS[(y + 7) * strideS + (x + 2) * 3] - 1 * scanS[(y + 7) * strideS + (x + 3) * 3] - 1 * scanS[(y + 7) * strideS + (x + 4) * 3]
																																																									- 01 * scanS[(y + 8) * strideS + (x - 2) * 3] - 01 * scanS[(y + 8) * strideS + (x - 1) * 3] - 01 * scanS[(y + 8) * strideS + x * 3] - 01 * scanS[(y + 8) * strideS + (x + 1) * 3] - 01 * scanS[(y + 8) * strideS + (x + 2) * 3]
											
						;
					
					edgeG =
																																																												-01 * scanS[(y - 8) * strideS + (x - 2) * 3 + 1] - 01 * scanS[(y - 8) * strideS + (x - 1) * 3 + 1] - 01 * scanS[(y - 8) * strideS + x * 3 + 1] - 01 * scanS[(y - 8) * strideS + (x + 1) * 3 + 1] - 01 * scanS[(y - 8) * strideS + (x + 2) * 3 + 1]
																																										- 1 * scanS[(y - 7) * strideS + (x - 4) * 3 + 1] - 1 * scanS[(y - 7) * strideS + (x - 3) * 3 + 1] - 01 * scanS[(y - 7) * strideS + (x - 2) * 3 + 1] - 01 * scanS[(y - 7) * strideS + (x - 1) * 3 + 1] - 01 * scanS[(y - 7) * strideS + x * 3 + 1] - 01 * scanS[(y - 7) * strideS + (x + 1) * 3 + 1] - 01 * scanS[(y - 7) * strideS + (x + 2) * 3 + 1] - 1 * scanS[(y - 7) * strideS + (x + 3) * 3 + 1] - 1 * scanS[(y - 7) * strideS + (x + 4) * 3 + 1]
																								- 1 * scanS[(y - 6) * strideS + (x - 6) * 3 + 1] - 1 * scanS[(y - 6) * strideS + (x - 5) * 3 + 1] - 1 * scanS[(y - 6) * strideS + (x - 4) * 3 + 1] - 2 * scanS[(y - 6) * strideS + (x - 3) * 3 + 1] - 03 * scanS[(y - 6) * strideS + (x - 2) * 3 + 1] - 03 * scanS[(y - 6) * strideS + (x - 1) * 3 + 1] - 03 * scanS[(y - 6) * strideS + x * 3 + 1] - 03 * scanS[(y - 6) * strideS + (x + 1) * 3 + 1] - 03 * scanS[(y - 6) * strideS + (x + 2) * 3 + 1] - 2 * scanS[(y - 6) * strideS + (x + 3) * 3 + 1] - 1 * scanS[(y - 6) * strideS + (x + 4) * 3 + 1] - 1 * scanS[(y - 6) * strideS + (x + 5) * 3 + 1] - 1 * scanS[(y - 6) * strideS + (x + 6) * 3 + 1]
																								- 1 * scanS[(y - 5) * strideS + (x - 6) * 3 + 1] - 1 * scanS[(y - 5) * strideS + (x - 5) * 3 + 1] - 2 * scanS[(y - 5) * strideS + (x - 4) * 3 + 1] - 3 * scanS[(y - 5) * strideS + (x - 3) * 3 + 1] - 03 * scanS[(y - 5) * strideS + (x - 2) * 3 + 1] - 03 * scanS[(y - 5) * strideS + (x - 1) * 3 + 1] - 03 * scanS[(y - 5) * strideS + x * 3 + 1] - 03 * scanS[(y - 5) * strideS + (x + 1) * 3 + 1] - 03 * scanS[(y - 5) * strideS + (x + 2) * 3 + 1] - 3 * scanS[(y - 5) * strideS + (x + 3) * 3 + 1] - 2 * scanS[(y - 5) * strideS + (x + 4) * 3 + 1] - 1 * scanS[(y - 5) * strideS + (x + 5) * 3 + 1] - 1 * scanS[(y - 5) * strideS + (x + 6) * 3 + 1]
															- 1 * scanS[(y - 4) * strideS + (x - 7) * 3 + 1] - 1 * scanS[(y - 4) * strideS + (x - 6) * 3 + 1] - 2 * scanS[(y - 4) * strideS + (x - 5) * 3 + 1] - 3 * scanS[(y - 4) * strideS + (x - 4) * 3 + 1] - 3 * scanS[(y - 4) * strideS + (x - 3) * 3 + 1] - 03 * scanS[(y - 4) * strideS + (x - 2) * 3 + 1] - 02 * scanS[(y - 4) * strideS + (x - 1) * 3 + 1] - 03 * scanS[(y - 4) * strideS + x * 3 + 1] - 02 * scanS[(y - 4) * strideS + (x + 1) * 3 + 1] - 03 * scanS[(y - 4) * strideS + (x + 2) * 3 + 1] - 3 * scanS[(y - 4) * strideS + (x + 3) * 3 + 1] - 3 * scanS[(y - 4) * strideS + (x + 4) * 3 + 1] - 2 * scanS[(y - 4) * strideS + (x + 5) * 3 + 1] - 1 * scanS[(y - 4) * strideS + (x + 6) * 3 + 1] - 1 * scanS[(y - 4) * strideS + (x + 7) * 3 + 1]
															- 1 * scanS[(y - 3) * strideS + (x - 7) * 3 + 1] - 2 * scanS[(y - 3) * strideS + (x - 6) * 3 + 1] - 3 * scanS[(y - 3) * strideS + (x - 5) * 3 + 1] - 3 * scanS[(y - 3) * strideS + (x - 4) * 3 + 1] - 3 * scanS[(y - 3) * strideS + (x - 3) * 3 + 1] + 02 * scanS[(y - 3) * strideS + (x - 1) * 3 + 1] + 04 * scanS[(y - 3) * strideS + x * 3 + 1] + 02 * scanS[(y - 3) * strideS + (x + 1) * 3 + 1] - 3 * scanS[(y - 3) * strideS + (x + 3) * 3 + 1] - 3 * scanS[(y - 3) * strideS + (x + 4) * 3 + 1] - 3 * scanS[(y - 3) * strideS + (x + 5) * 3 + 1] - 2 * scanS[(y - 3) * strideS + (x + 6) * 3 + 1] - 1 * scanS[(y - 3) * strideS + (x + 7) * 3 + 1]
						- 1 * scanS[(y - 2) * strideS + (x - 8) * 3 + 1] - 1 * scanS[(y - 2) * strideS + (x - 7) * 3 + 1] - 3 * scanS[(y - 2) * strideS + (x - 6) * 3 + 1] - 3 * scanS[(y - 2) * strideS + (x - 5) * 3 + 1] - 3 * scanS[(y - 2) * strideS + (x - 4) * 3 + 1] + 04 * scanS[(y - 2) * strideS + (x - 2) * 3 + 1] + 10 * scanS[(y - 2) * strideS + (x - 1) * 3 + 1] + 12 * scanS[(y - 2) * strideS + x * 3 + 1] + 10 * scanS[(y - 2) * strideS + (x + 1) * 3 + 1] + 04 * scanS[(y - 2) * strideS + (x + 2) * 3 + 1] - 3 * scanS[(y - 2) * strideS + (x + 4) * 3 + 1] - 3 * scanS[(y - 2) * strideS + (x + 5) * 3 + 1] - 3 * scanS[(y - 2) * strideS + (x + 6) * 3 + 1] - 1 * scanS[(y - 2) * strideS + (x + 7) * 3 + 1] - 1 * scanS[(y - 2) * strideS + (x + 8) * 3 + 1]
						- 1 * scanS[(y - 1) * strideS + (x - 8) * 3 + 1] - 1 * scanS[(y - 1) * strideS + (x - 7) * 3 + 1] - 3 * scanS[(y - 1) * strideS + (x - 6) * 3 + 1] - 3 * scanS[(y - 1) * strideS + (x - 5) * 3 + 1] - 2 * scanS[(y - 1) * strideS + (x - 4) * 3 + 1] + 2 * scanS[(y - 1) * strideS + (x - 3) * 3 + 1] + 10 * scanS[(y - 1) * strideS + (x - 2) * 3 + 1] + 18 * scanS[(y - 1) * strideS + (x - 1) * 3 + 1] + 21 * scanS[(y - 1) * strideS + x * 3 + 1] + 18 * scanS[(y - 1) * strideS + (x + 1) * 3 + 1] + 10 * scanS[(y - 1) * strideS + (x + 2) * 3 + 1] + 2 * scanS[(y - 1) * strideS + (x + 3) * 3 + 1] - 2 * scanS[(y - 1) * strideS + (x + 4) * 3 + 1] - 3 * scanS[(y - 1) * strideS + (x + 5) * 3 + 1] - 3 * scanS[(y - 1) * strideS + (x + 6) * 3 + 1] - 1 * scanS[(y - 1) * strideS + (x + 7) * 3 + 1] - 1 * scanS[(y - 1) * strideS + (x + 8) * 3 + 1]
						- 1 * scanS[(y + 0) * strideS + (x - 8) * 3 + 1] - 1 * scanS[(y + 0) * strideS + (x - 7) * 3 + 1] - 3 * scanS[(y + 0) * strideS + (x - 6) * 3 + 1] - 3 * scanS[(y + 0) * strideS + (x - 5) * 3 + 1] - 3 * scanS[(y + 0) * strideS + (x - 4) * 3 + 1] + 4 * scanS[(y + 0) * strideS + (x - 3) * 3 + 1] + 12 * scanS[(y + 0) * strideS + (x - 2) * 3 + 1] + 21 * scanS[(y + 0) * strideS + (x - 1) * 3 + 1] + 24 * scanS[(y + 0) * strideS + x * 3 + 1] + 21 * scanS[(y + 0) * strideS + (x + 1) * 3 + 1] + 12 * scanS[(y + 0) * strideS + (x + 2) * 3 + 1] + 4 * scanS[(y + 0) * strideS + (x + 3) * 3 + 1] - 3 * scanS[(y + 0) * strideS + (x + 4) * 3 + 1] - 3 * scanS[(y + 0) * strideS + (x + 5) * 3 + 1] - 3 * scanS[(y + 0) * strideS + (x + 6) * 3 + 1] - 1 * scanS[(y + 0) * strideS + (x + 7) * 3 + 1] - 1 * scanS[(y + 0) * strideS + (x + 8) * 3 + 1]
						- 1 * scanS[(y + 1) * strideS + (x - 8) * 3 + 1] - 1 * scanS[(y + 1) * strideS + (x - 7) * 3 + 1] - 3 * scanS[(y + 1) * strideS + (x - 6) * 3 + 1] - 3 * scanS[(y + 1) * strideS + (x - 5) * 3 + 1] - 2 * scanS[(y + 1) * strideS + (x - 4) * 3 + 1] + 2 * scanS[(y + 1) * strideS + (x - 3) * 3 + 1] + 10 * scanS[(y + 1) * strideS + (x - 2) * 3 + 1] + 18 * scanS[(y + 1) * strideS + (x - 1) * 3 + 1] + 21 * scanS[(y + 1) * strideS + x * 3 + 1] + 18 * scanS[(y + 1) * strideS + (x + 1) * 3 + 1] + 10 * scanS[(y + 1) * strideS + (x + 2) * 3 + 1] + 2 * scanS[(y + 1) * strideS + (x + 3) * 3 + 1] - 2 * scanS[(y + 1) * strideS + (x + 4) * 3 + 1] - 3 * scanS[(y + 1) * strideS + (x + 5) * 3 + 1] - 3 * scanS[(y + 1) * strideS + (x + 6) * 3 + 1] - 1 * scanS[(y + 1) * strideS + (x + 7) * 3 + 1] - 1 * scanS[(y + 1) * strideS + (x + 8) * 3 + 1]
						- 1 * scanS[(y + 2) * strideS + (x - 8) * 3 + 1] - 1 * scanS[(y + 2) * strideS + (x - 7) * 3 + 1] - 3 * scanS[(y + 2) * strideS + (x - 6) * 3 + 1] - 3 * scanS[(y + 2) * strideS + (x - 5) * 3 + 1] - 3 * scanS[(y + 2) * strideS + (x - 4) * 3 + 1] + 04 * scanS[(y + 2) * strideS + (x - 2) * 3 + 1] + 10 * scanS[(y + 2) * strideS + (x - 1) * 3 + 1] + 12 * scanS[(y + 2) * strideS + x * 3 + 1] + 10 * scanS[(y + 2) * strideS + (x + 1) * 3 + 1] + 04 * scanS[(y + 2) * strideS + (x + 2) * 3 + 1] - 3 * scanS[(y + 2) * strideS + (x + 4) * 3 + 1] - 3 * scanS[(y + 2) * strideS + (x + 5) * 3 + 1] - 3 * scanS[(y + 2) * strideS + (x + 6) * 3 + 1] - 1 * scanS[(y + 2) * strideS + (x + 7) * 3 + 1] - 1 * scanS[(y + 2) * strideS + (x + 8) * 3 + 1]
															- 1 * scanS[(y + 3) * strideS + (x - 7) * 3 + 1] - 2 * scanS[(y + 3) * strideS + (x - 6) * 3 + 1] - 3 * scanS[(y + 3) * strideS + (x - 5) * 3 + 1] - 3 * scanS[(y + 3) * strideS + (x - 4) * 3 + 1] - 3 * scanS[(y + 3) * strideS + (x - 3) * 3 + 1] + 02 * scanS[(y + 3) * strideS + (x - 1) * 3 + 1] + 04 * scanS[(y + 3) * strideS + x * 3 + 1] + 02 * scanS[(y + 3) * strideS + (x + 1) * 3 + 1] - 3 * scanS[(y + 3) * strideS + (x + 3) * 3 + 1] - 3 * scanS[(y + 3) * strideS + (x + 4) * 3 + 1] - 3 * scanS[(y + 3) * strideS + (x + 5) * 3 + 1] - 2 * scanS[(y + 3) * strideS + (x + 6) * 3 + 1] - 1 * scanS[(y + 3) * strideS + (x + 7) * 3 + 1]
															- 1 * scanS[(y + 4) * strideS + (x - 7) * 3 + 1] - 1 * scanS[(y + 4) * strideS + (x - 6) * 3 + 1] - 2 * scanS[(y + 4) * strideS + (x - 5) * 3 + 1] - 3 * scanS[(y + 4) * strideS + (x - 4) * 3 + 1] - 3 * scanS[(y + 4) * strideS + (x - 3) * 3 + 1] - 03 * scanS[(y + 4) * strideS + (x - 2) * 3 + 1] - 02 * scanS[(y + 4) * strideS + (x - 1) * 3 + 1] - 03 * scanS[(y + 4) * strideS + x * 3 + 1] - 02 * scanS[(y + 4) * strideS + (x + 1) * 3 + 1] - 03 * scanS[(y + 4) * strideS + (x + 2) * 3 + 1] - 3 * scanS[(y + 4) * strideS + (x + 3) * 3 + 1] - 3 * scanS[(y + 4) * strideS + (x + 4) * 3 + 1] - 2 * scanS[(y + 4) * strideS + (x + 5) * 3 + 1] - 1 * scanS[(y + 4) * strideS + (x + 6) * 3 + 1] - 1 * scanS[(y + 4) * strideS + (x + 7) * 3 + 1]
																								- 1 * scanS[(y + 5) * strideS + (x - 6) * 3 + 1] - 1 * scanS[(y + 5) * strideS + (x - 5) * 3 + 1] - 2 * scanS[(y + 5) * strideS + (x - 4) * 3 + 1] - 3 * scanS[(y + 5) * strideS + (x - 3) * 3 + 1] - 03 * scanS[(y + 5) * strideS + (x - 2) * 3 + 1] - 03 * scanS[(y + 5) * strideS + (x - 1) * 3 + 1] - 03 * scanS[(y + 5) * strideS + x * 3 + 1] - 03 * scanS[(y + 5) * strideS + (x + 1) * 3 + 1] - 03 * scanS[(y + 5) * strideS + (x + 2) * 3 + 1] - 3 * scanS[(y + 5) * strideS + (x + 3) * 3 + 1] - 2 * scanS[(y + 5) * strideS + (x + 4) * 3 + 1] - 1 * scanS[(y + 5) * strideS + (x + 5) * 3 + 1] - 1 * scanS[(y + 5) * strideS + (x + 6) * 3 + 1]
																								- 1 * scanS[(y + 6) * strideS + (x - 6) * 3 + 1] - 1 * scanS[(y + 6) * strideS + (x - 5) * 3 + 1] - 1 * scanS[(y + 6) * strideS + (x - 4) * 3 + 1] - 2 * scanS[(y + 6) * strideS + (x - 3) * 3 + 1] - 03 * scanS[(y + 6) * strideS + (x - 2) * 3 + 1] - 03 * scanS[(y + 6) * strideS + (x - 1) * 3 + 1] - 03 * scanS[(y + 6) * strideS + x * 3 + 1] - 03 * scanS[(y + 6) * strideS + (x + 1) * 3 + 1] - 03 * scanS[(y + 6) * strideS + (x + 2) * 3 + 1] - 2 * scanS[(y + 6) * strideS + (x + 3) * 3 + 1] - 1 * scanS[(y + 6) * strideS + (x + 4) * 3 + 1] - 1 * scanS[(y + 6) * strideS + (x + 5) * 3 + 1] - 1 * scanS[(y + 6) * strideS + (x + 6) * 3 + 1]
																																										- 1 * scanS[(y + 7) * strideS + (x - 4) * 3 + 1] - 1 * scanS[(y + 7) * strideS + (x - 3) * 3 + 1] - 01 * scanS[(y + 7) * strideS + (x - 2) * 3 + 1] - 01 * scanS[(y + 7) * strideS + (x - 1) * 3 + 1] - 01 * scanS[(y + 7) * strideS + x * 3 + 1] - 01 * scanS[(y + 7) * strideS + (x + 1) * 3 + 1] - 01 * scanS[(y + 7) * strideS + (x + 2) * 3 + 1] - 1 * scanS[(y + 7) * strideS + (x + 3) * 3 + 1] - 1 * scanS[(y + 7) * strideS + (x + 4) * 3 + 1]
																																																												- 01 * scanS[(y + 8) * strideS + (x - 2) * 3 + 1] - 01 * scanS[(y + 8) * strideS + (x - 1) * 3 + 1] - 01 * scanS[(y + 8) * strideS + x * 3 + 1] - 01 * scanS[(y + 8) * strideS + (x + 1) * 3 + 1] - 01 * scanS[(y + 8) * strideS + (x + 2) * 3 + 1]
					;
					edgeR =
																																																												-01 * scanS[(y - 8) * strideS + (x - 2) * 3 + 2] - 01 * scanS[(y - 8) * strideS + (x - 1) * 3 + 2] - 01 * scanS[(y - 8) * strideS + x * 3 + 2] - 01 * scanS[(y - 8) * strideS + (x + 1) * 3 + 2] - 01 * scanS[(y - 8) * strideS + (x + 2) * 3 + 2]
																																										- 1 * scanS[(y - 7) * strideS + (x - 4) * 3 + 2] - 1 * scanS[(y - 7) * strideS + (x - 3) * 3 + 2] - 01 * scanS[(y - 7) * strideS + (x - 2) * 3 + 2] - 01 * scanS[(y - 7) * strideS + (x - 1) * 3 + 2] - 01 * scanS[(y - 7) * strideS + x * 3 + 2] - 01 * scanS[(y - 7) * strideS + (x + 1) * 3 + 2] - 01 * scanS[(y - 7) * strideS + (x + 2) * 3 + 2] - 1 * scanS[(y - 7) * strideS + (x + 3) * 3 + 2] - 1 * scanS[(y - 7) * strideS + (x + 4) * 3 + 2]
																								- 1 * scanS[(y - 6) * strideS + (x - 6) * 3 + 2] - 1 * scanS[(y - 6) * strideS + (x - 5) * 3 + 2] - 1 * scanS[(y - 6) * strideS + (x - 4) * 3 + 2] - 2 * scanS[(y - 6) * strideS + (x - 3) * 3 + 2] - 03 * scanS[(y - 6) * strideS + (x - 2) * 3 + 2] - 03 * scanS[(y - 6) * strideS + (x - 1) * 3 + 2] - 03 * scanS[(y - 6) * strideS + x * 3 + 2] - 03 * scanS[(y - 6) * strideS + (x + 1) * 3 + 2] - 03 * scanS[(y - 6) * strideS + (x + 2) * 3 + 2] - 2 * scanS[(y - 6) * strideS + (x + 3) * 3 + 2] - 1 * scanS[(y - 6) * strideS + (x + 4) * 3 + 2] - 1 * scanS[(y - 6) * strideS + (x + 5) * 3 + 2] - 1 * scanS[(y - 6) * strideS + (x + 6) * 3 + 2]
																								- 1 * scanS[(y - 5) * strideS + (x - 6) * 3 + 2] - 1 * scanS[(y - 5) * strideS + (x - 5) * 3 + 2] - 2 * scanS[(y - 5) * strideS + (x - 4) * 3 + 2] - 3 * scanS[(y - 5) * strideS + (x - 3) * 3 + 2] - 03 * scanS[(y - 5) * strideS + (x - 2) * 3 + 2] - 03 * scanS[(y - 5) * strideS + (x - 1) * 3 + 2] - 03 * scanS[(y - 5) * strideS + x * 3 + 2] - 03 * scanS[(y - 5) * strideS + (x + 1) * 3 + 2] - 03 * scanS[(y - 5) * strideS + (x + 2) * 3 + 2] - 3 * scanS[(y - 5) * strideS + (x + 3) * 3 + 2] - 2 * scanS[(y - 5) * strideS + (x + 4) * 3 + 2] - 1 * scanS[(y - 5) * strideS + (x + 5) * 3 + 2] - 1 * scanS[(y - 5) * strideS + (x + 6) * 3 + 2]
															- 1 * scanS[(y - 4) * strideS + (x - 7) * 3 + 2] - 1 * scanS[(y - 4) * strideS + (x - 6) * 3 + 2] - 2 * scanS[(y - 4) * strideS + (x - 5) * 3 + 2] - 3 * scanS[(y - 4) * strideS + (x - 4) * 3 + 2] - 3 * scanS[(y - 4) * strideS + (x - 3) * 3 + 2] - 03 * scanS[(y - 4) * strideS + (x - 2) * 3 + 2] - 02 * scanS[(y - 4) * strideS + (x - 1) * 3 + 2] - 03 * scanS[(y - 4) * strideS + x * 3 + 2] - 02 * scanS[(y - 4) * strideS + (x + 1) * 3 + 2] - 03 * scanS[(y - 4) * strideS + (x + 2) * 3 + 2] - 3 * scanS[(y - 4) * strideS + (x + 3) * 3 + 2] - 3 * scanS[(y - 4) * strideS + (x + 4) * 3 + 2] - 2 * scanS[(y - 4) * strideS + (x + 5) * 3 + 2] - 1 * scanS[(y - 4) * strideS + (x + 6) * 3 + 2] - 1 * scanS[(y - 4) * strideS + (x + 7) * 3 + 2]
															- 1 * scanS[(y - 3) * strideS + (x - 7) * 3 + 2] - 2 * scanS[(y - 3) * strideS + (x - 6) * 3 + 2] - 3 * scanS[(y - 3) * strideS + (x - 5) * 3 + 2] - 3 * scanS[(y - 3) * strideS + (x - 4) * 3 + 2] - 3 * scanS[(y - 3) * strideS + (x - 3) * 3 + 2] + 02 * scanS[(y - 3) * strideS + (x - 1) * 3 + 2] + 04 * scanS[(y - 3) * strideS + x * 3 + 2] + 02 * scanS[(y - 3) * strideS + (x + 1) * 3 + 2] - 3 * scanS[(y - 3) * strideS + (x + 3) * 3 + 2] - 3 * scanS[(y - 3) * strideS + (x + 4) * 3 + 2] - 3 * scanS[(y - 3) * strideS + (x + 5) * 3 + 2] - 2 * scanS[(y - 3) * strideS + (x + 6) * 3 + 2] - 1 * scanS[(y - 3) * strideS + (x + 7) * 3 + 2]
						- 1 * scanS[(y - 2) * strideS + (x - 8) * 3 + 2] - 1 * scanS[(y - 2) * strideS + (x - 7) * 3 + 2] - 3 * scanS[(y - 2) * strideS + (x - 6) * 3 + 2] - 3 * scanS[(y - 2) * strideS + (x - 5) * 3 + 2] - 3 * scanS[(y - 2) * strideS + (x - 4) * 3 + 2] + 04 * scanS[(y - 2) * strideS + (x - 2) * 3 + 2] + 10 * scanS[(y - 2) * strideS + (x - 1) * 3 + 2] + 12 * scanS[(y - 2) * strideS + x * 3 + 2] + 10 * scanS[(y - 2) * strideS + (x + 1) * 3 + 2] + 04 * scanS[(y - 2) * strideS + (x + 2) * 3 + 2] - 3 * scanS[(y - 2) * strideS + (x + 4) * 3 + 2] - 3 * scanS[(y - 2) * strideS + (x + 5) * 3 + 2] - 3 * scanS[(y - 2) * strideS + (x + 6) * 3 + 2] - 1 * scanS[(y - 2) * strideS + (x + 7) * 3 + 2] - 1 * scanS[(y - 2) * strideS + (x + 8) * 3 + 2]
						- 1 * scanS[(y - 1) * strideS + (x - 8) * 3 + 2] - 1 * scanS[(y - 1) * strideS + (x - 7) * 3 + 2] - 3 * scanS[(y - 1) * strideS + (x - 6) * 3 + 2] - 3 * scanS[(y - 1) * strideS + (x - 5) * 3 + 2] - 2 * scanS[(y - 1) * strideS + (x - 4) * 3 + 2] + 2 * scanS[(y - 1) * strideS + (x - 3) * 3 + 2] + 10 * scanS[(y - 1) * strideS + (x - 2) * 3 + 2] + 18 * scanS[(y - 1) * strideS + (x - 1) * 3 + 2] + 21 * scanS[(y - 1) * strideS + x * 3 + 2] + 18 * scanS[(y - 1) * strideS + (x + 1) * 3 + 2] + 10 * scanS[(y - 1) * strideS + (x + 2) * 3 + 2] + 2 * scanS[(y - 1) * strideS + (x + 3) * 3 + 2] - 2 * scanS[(y - 1) * strideS + (x + 4) * 3 + 2] - 3 * scanS[(y - 1) * strideS + (x + 5) * 3 + 2] - 3 * scanS[(y - 1) * strideS + (x + 6) * 3 + 2] - 1 * scanS[(y - 1) * strideS + (x + 7) * 3 + 2] - 1 * scanS[(y - 1) * strideS + (x + 8) * 3 + 2]
						- 1 * scanS[(y + 0) * strideS + (x - 8) * 3 + 2] - 1 * scanS[(y + 0) * strideS + (x - 7) * 3 + 2] - 3 * scanS[(y + 0) * strideS + (x - 6) * 3 + 2] - 3 * scanS[(y + 0) * strideS + (x - 5) * 3 + 2] - 3 * scanS[(y + 0) * strideS + (x - 4) * 3 + 2] + 4 * scanS[(y + 0) * strideS + (x - 3) * 3 + 2] + 12 * scanS[(y + 0) * strideS + (x - 2) * 3 + 2] + 21 * scanS[(y + 0) * strideS + (x - 1) * 3 + 2] + 24 * scanS[(y + 0) * strideS + x * 3 + 2] + 21 * scanS[(y + 0) * strideS + (x + 1) * 3 + 2] + 12 * scanS[(y + 0) * strideS + (x + 2) * 3 + 2] + 4 * scanS[(y + 0) * strideS + (x + 3) * 3 + 2] - 3 * scanS[(y + 0) * strideS + (x + 4) * 3 + 2] - 3 * scanS[(y + 0) * strideS + (x + 5) * 3 + 2] - 3 * scanS[(y + 0) * strideS + (x + 6) * 3 + 2] - 1 * scanS[(y + 0) * strideS + (x + 7) * 3 + 2] - 1 * scanS[(y + 0) * strideS + (x + 8) * 3 + 2]
						- 1 * scanS[(y + 1) * strideS + (x - 8) * 3 + 2] - 1 * scanS[(y + 1) * strideS + (x - 7) * 3 + 2] - 3 * scanS[(y + 1) * strideS + (x - 6) * 3 + 2] - 3 * scanS[(y + 1) * strideS + (x - 5) * 3 + 2] - 2 * scanS[(y + 1) * strideS + (x - 4) * 3 + 2] + 2 * scanS[(y + 1) * strideS + (x - 3) * 3 + 2] + 10 * scanS[(y + 1) * strideS + (x - 2) * 3 + 2] + 18 * scanS[(y + 1) * strideS + (x - 1) * 3 + 2] + 21 * scanS[(y + 1) * strideS + x * 3 + 2] + 18 * scanS[(y + 1) * strideS + (x + 1) * 3 + 2] + 10 * scanS[(y + 1) * strideS + (x + 2) * 3 + 2] + 2 * scanS[(y + 1) * strideS + (x + 3) * 3 + 2] - 2 * scanS[(y + 1) * strideS + (x + 4) * 3 + 2] - 3 * scanS[(y + 1) * strideS + (x + 5) * 3 + 2] - 3 * scanS[(y + 1) * strideS + (x + 6) * 3 + 2] - 1 * scanS[(y + 1) * strideS + (x + 7) * 3 + 2] - 1 * scanS[(y + 1) * strideS + (x + 8) * 3 + 2]
						- 1 * scanS[(y + 2) * strideS + (x - 8) * 3 + 2] - 1 * scanS[(y + 2) * strideS + (x - 7) * 3 + 2] - 3 * scanS[(y + 2) * strideS + (x - 6) * 3 + 2] - 3 * scanS[(y + 2) * strideS + (x - 5) * 3 + 2] - 3 * scanS[(y + 2) * strideS + (x - 4) * 3 + 2] + 04 * scanS[(y + 2) * strideS + (x - 2) * 3 + 2] + 10 * scanS[(y + 2) * strideS + (x - 1) * 3 + 2] + 12 * scanS[(y + 2) * strideS + x * 3 + 2] + 10 * scanS[(y + 2) * strideS + (x + 1) * 3 + 2] + 04 * scanS[(y + 2) * strideS + (x + 2) * 3 + 2] - 3 * scanS[(y + 2) * strideS + (x + 4) * 3 + 2] - 3 * scanS[(y + 2) * strideS + (x + 5) * 3 + 2] - 3 * scanS[(y + 2) * strideS + (x + 6) * 3 + 2] - 1 * scanS[(y + 2) * strideS + (x + 7) * 3 + 2] - 1 * scanS[(y + 2) * strideS + (x + 8) * 3 + 2]
															- 1 * scanS[(y + 3) * strideS + (x - 7) * 3 + 2] - 2 * scanS[(y + 3) * strideS + (x - 6) * 3 + 2] - 3 * scanS[(y + 3) * strideS + (x - 5) * 3 + 2] - 3 * scanS[(y + 3) * strideS + (x - 4) * 3 + 2] - 3 * scanS[(y + 3) * strideS + (x - 3) * 3 + 2] + 02 * scanS[(y + 3) * strideS + (x - 1) * 3 + 2] + 04 * scanS[(y + 3) * strideS + x * 3 + 2] + 02 * scanS[(y + 3) * strideS + (x + 1) * 3 + 2] - 3 * scanS[(y + 3) * strideS + (x + 3) * 3 + 2] - 3 * scanS[(y + 3) * strideS + (x + 4) * 3 + 2] - 3 * scanS[(y + 3) * strideS + (x + 5) * 3 + 2] - 2 * scanS[(y + 3) * strideS + (x + 6) * 3 + 2] - 1 * scanS[(y + 3) * strideS + (x + 7) * 3 + 2]
															- 1 * scanS[(y + 4) * strideS + (x - 7) * 3 + 2] - 1 * scanS[(y + 4) * strideS + (x - 6) * 3 + 2] - 2 * scanS[(y + 4) * strideS + (x - 5) * 3 + 2] - 3 * scanS[(y + 4) * strideS + (x - 4) * 3 + 2] - 3 * scanS[(y + 4) * strideS + (x - 3) * 3 + 2] - 03 * scanS[(y + 4) * strideS + (x - 2) * 3 + 2] - 02 * scanS[(y + 4) * strideS + (x - 1) * 3 + 2] - 03 * scanS[(y + 4) * strideS + x * 3 + 2] - 02 * scanS[(y + 4) * strideS + (x + 1) * 3 + 2] - 03 * scanS[(y + 4) * strideS + (x + 2) * 3 + 2] - 3 * scanS[(y + 4) * strideS + (x + 3) * 3 + 2] - 3 * scanS[(y + 4) * strideS + (x + 4) * 3 + 2] - 2 * scanS[(y + 4) * strideS + (x + 5) * 3 + 2] - 1 * scanS[(y + 4) * strideS + (x + 6) * 3 + 2] - 1 * scanS[(y + 4) * strideS + (x + 7) * 3 + 2]
																								- 1 * scanS[(y + 5) * strideS + (x - 6) * 3 + 2] - 1 * scanS[(y + 5) * strideS + (x - 5) * 3 + 2] - 2 * scanS[(y + 5) * strideS + (x - 4) * 3 + 2] - 3 * scanS[(y + 5) * strideS + (x - 3) * 3 + 2] - 03 * scanS[(y + 5) * strideS + (x - 2) * 3 + 2] - 03 * scanS[(y + 5) * strideS + (x - 1) * 3 + 2] - 03 * scanS[(y + 5) * strideS + x * 3 + 2] - 03 * scanS[(y + 5) * strideS + (x + 1) * 3 + 2] - 03 * scanS[(y + 5) * strideS + (x + 2) * 3 + 2] - 3 * scanS[(y + 5) * strideS + (x + 3) * 3 + 2] - 2 * scanS[(y + 5) * strideS + (x + 4) * 3 + 2] - 1 * scanS[(y + 5) * strideS + (x + 5) * 3 + 2] - 1 * scanS[(y + 5) * strideS + (x + 6) * 3 + 2]
																								- 1 * scanS[(y + 6) * strideS + (x - 6) * 3 + 2] - 1 * scanS[(y + 6) * strideS + (x - 5) * 3 + 2] - 1 * scanS[(y + 6) * strideS + (x - 4) * 3 + 2] - 2 * scanS[(y + 6) * strideS + (x - 3) * 3 + 2] - 03 * scanS[(y + 6) * strideS + (x - 2) * 3 + 2] - 03 * scanS[(y + 6) * strideS + (x - 1) * 3 + 2] - 03 * scanS[(y + 6) * strideS + x * 3 + 2] - 03 * scanS[(y + 6) * strideS + (x + 1) * 3 + 2] - 03 * scanS[(y + 6) * strideS + (x + 2) * 3 + 2] - 2 * scanS[(y + 6) * strideS + (x + 3) * 3 + 2] - 1 * scanS[(y + 6) * strideS + (x + 4) * 3 + 2] - 1 * scanS[(y + 6) * strideS + (x + 5) * 3 + 2] - 1 * scanS[(y + 6) * strideS + (x + 6) * 3 + 2]
																																										- 1 * scanS[(y + 7) * strideS + (x - 4) * 3 + 2] - 1 * scanS[(y + 7) * strideS + (x - 3) * 3 + 2] - 01 * scanS[(y + 7) * strideS + (x - 2) * 3 + 2] - 01 * scanS[(y + 7) * strideS + (x - 1) * 3 + 2] - 01 * scanS[(y + 7) * strideS + x * 3 + 2] - 01 * scanS[(y + 7) * strideS + (x + 1) * 3 + 2] - 01 * scanS[(y + 7) * strideS + (x + 2) * 3 + 2] - 1 * scanS[(y + 7) * strideS + (x + 3) * 3 + 2] - 1 * scanS[(y + 7) * strideS + (x + 4) * 3 + 2]
																																																												- 01 * scanS[(y + 8) * strideS + (x - 2) * 3 + 2] - 01 * scanS[(y + 8) * strideS + (x - 1) * 3 + 2] - 01 * scanS[(y + 8) * strideS + x * 3 + 2] - 01 * scanS[(y + 8) * strideS + (x + 1) * 3 + 2] - 01 * scanS[(y + 8) * strideS + (x + 2) * 3 + 2]
					;

					edge = (edgeB + edgeG + edgeR) / 500 + 127;

					if (edge < 0)
						scanR[y * strideR + x] = 0;
					else if (edge > 255)
						scanR[y * strideR + x] = 255;
					else
						scanR[y * strideR + x] = (byte)edge;
				}
			}

#if DEBUG
			Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif

			//left, right
			for (y = 8; y < height - 8; y++)
				for (x = 0; x < 8; x++)
				{
					scanR[y * strideR + x] = scanR[y * strideR + 8];
					scanR[y * strideR + width - 1 - x] = scanR[y * strideR + width - 9];
				}

			//top, bottom
			for (x = 0; x < width; x++)
				for (y = 0; y < 8; y++)
				{
					scanR[y * strideR + x] = scanR[8 * strideR + x];
					scanR[(height - 1 - y) * strideR + x] = scanR[(height - 9) * strideR + x];
				}
		}
		#endregion

		#region MexicanHat17x17_32bpp()
		private static unsafe void MexicanHat17x17_32bpp(byte* scanS, int strideS, byte* scanR, int strideR, int width, int height)
		{
			int x, y;
			int edge = 0;
			int edgeB, edgeG, edgeR;

			int[,] m = GetConvolutionMask(Operator.MexicanHat17x17);

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			for (y = 8; y < height - 8; y++)
			{

				for (x = 8; x < width - 8; x++)
				{
					edgeB =
																																																									-01 * scanS[(y - 8) * strideS + (x - 2) * 4] - 01 * scanS[(y - 8) * strideS + (x - 1) * 4] - 01 * scanS[(y - 8) * strideS + x * 4] - 01 * scanS[(y - 8) * strideS + (x + 1) * 4] - 01 * scanS[(y - 8) * strideS + (x + 2) * 4]
																																								- 1 * scanS[(y - 7) * strideS + (x - 4) * 4] - 1 * scanS[(y - 7) * strideS + (x - 3) * 4] - 01 * scanS[(y - 7) * strideS + (x - 2) * 4] - 01 * scanS[(y - 7) * strideS + (x - 1) * 4] - 01 * scanS[(y - 7) * strideS + x * 4] - 01 * scanS[(y - 7) * strideS + (x + 1) * 4] - 01 * scanS[(y - 7) * strideS + (x + 2) * 4] - 1 * scanS[(y - 7) * strideS + (x + 3) * 4] - 1 * scanS[(y - 7) * strideS + (x + 4) * 4]
																							- 1 * scanS[(y - 6) * strideS + (x - 6) * 4] - 1 * scanS[(y - 6) * strideS + (x - 5) * 4] - 1 * scanS[(y - 6) * strideS + (x - 4) * 4] - 2 * scanS[(y - 6) * strideS + (x - 3) * 4] - 03 * scanS[(y - 6) * strideS + (x - 2) * 4] - 03 * scanS[(y - 6) * strideS + (x - 1) * 4] - 03 * scanS[(y - 6) * strideS + x * 4] - 03 * scanS[(y - 6) * strideS + (x + 1) * 4] - 03 * scanS[(y - 6) * strideS + (x + 2) * 4] - 2 * scanS[(y - 6) * strideS + (x + 3) * 4] - 1 * scanS[(y - 6) * strideS + (x + 4) * 4] - 1 * scanS[(y - 6) * strideS + (x + 5) * 4] - 1 * scanS[(y - 6) * strideS + (x + 6) * 4]
																							- 1 * scanS[(y - 5) * strideS + (x - 6) * 4] - 1 * scanS[(y - 5) * strideS + (x - 5) * 4] - 2 * scanS[(y - 5) * strideS + (x - 4) * 4] - 3 * scanS[(y - 5) * strideS + (x - 3) * 4] - 03 * scanS[(y - 5) * strideS + (x - 2) * 4] - 03 * scanS[(y - 5) * strideS + (x - 1) * 4] - 03 * scanS[(y - 5) * strideS + x * 4] - 03 * scanS[(y - 5) * strideS + (x + 1) * 4] - 03 * scanS[(y - 5) * strideS + (x + 2) * 4] - 3 * scanS[(y - 5) * strideS + (x + 3) * 4] - 2 * scanS[(y - 5) * strideS + (x + 4) * 4] - 1 * scanS[(y - 5) * strideS + (x + 5) * 4] - 1 * scanS[(y - 5) * strideS + (x + 6) * 4]
														  - 1 * scanS[(y - 4) * strideS + (x - 7) * 4] - 1 * scanS[(y - 4) * strideS + (x - 6) * 4] - 2 * scanS[(y - 4) * strideS + (x - 5) * 4] - 3 * scanS[(y - 4) * strideS + (x - 4) * 4] - 3 * scanS[(y - 4) * strideS + (x - 3) * 4] - 03 * scanS[(y - 4) * strideS + (x - 2) * 4] - 02 * scanS[(y - 4) * strideS + (x - 1) * 4] - 03 * scanS[(y - 4) * strideS + x * 4] - 02 * scanS[(y - 4) * strideS + (x + 1) * 4] - 03 * scanS[(y - 4) * strideS + (x + 2) * 4] - 3 * scanS[(y - 4) * strideS + (x + 3) * 4] - 3 * scanS[(y - 4) * strideS + (x + 4) * 4] - 2 * scanS[(y - 4) * strideS + (x + 5) * 4] - 1 * scanS[(y - 4) * strideS + (x + 6) * 4] - 1 * scanS[(y - 4) * strideS + (x + 7) * 4]
														  - 1 * scanS[(y - 3) * strideS + (x - 7) * 4] - 2 * scanS[(y - 3) * strideS + (x - 6) * 4] - 3 * scanS[(y - 3) * strideS + (x - 5) * 4] - 3 * scanS[(y - 3) * strideS + (x - 4) * 4] - 3 * scanS[(y - 3) * strideS + (x - 3) * 4] + 02 * scanS[(y - 3) * strideS + (x - 1) * 4] + 04 * scanS[(y - 3) * strideS + x * 4] + 02 * scanS[(y - 3) * strideS + (x + 1) * 4] - 3 * scanS[(y - 3) * strideS + (x + 3) * 4] - 3 * scanS[(y - 3) * strideS + (x + 4) * 4] - 3 * scanS[(y - 3) * strideS + (x + 5) * 4] - 2 * scanS[(y - 3) * strideS + (x + 6) * 4] - 1 * scanS[(y - 3) * strideS + (x + 7) * 4]
						- 1 * scanS[(y - 2) * strideS + (x - 8) * 4] - 1 * scanS[(y - 2) * strideS + (x - 7) * 4] - 3 * scanS[(y - 2) * strideS + (x - 6) * 4] - 3 * scanS[(y - 2) * strideS + (x - 5) * 4] - 3 * scanS[(y - 2) * strideS + (x - 4) * 4] + 04 * scanS[(y - 2) * strideS + (x - 2) * 4] + 10 * scanS[(y - 2) * strideS + (x - 1) * 4] + 12 * scanS[(y - 2) * strideS + x * 4] + 10 * scanS[(y - 2) * strideS + (x + 1) * 4] + 04 * scanS[(y - 2) * strideS + (x + 2) * 4] - 3 * scanS[(y - 2) * strideS + (x + 4) * 4] - 3 * scanS[(y - 2) * strideS + (x + 5) * 4] - 3 * scanS[(y - 2) * strideS + (x + 6) * 4] - 1 * scanS[(y - 2) * strideS + (x + 7) * 4] - 1 * scanS[(y - 2) * strideS + (x + 8) * 4]
						- 1 * scanS[(y - 1) * strideS + (x - 8) * 4] - 1 * scanS[(y - 1) * strideS + (x - 7) * 4] - 3 * scanS[(y - 1) * strideS + (x - 6) * 4] - 3 * scanS[(y - 1) * strideS + (x - 5) * 4] - 2 * scanS[(y - 1) * strideS + (x - 4) * 4] + 2 * scanS[(y - 1) * strideS + (x - 3) * 4] + 10 * scanS[(y - 1) * strideS + (x - 2) * 4] + 18 * scanS[(y - 1) * strideS + (x - 1) * 4] + 21 * scanS[(y - 1) * strideS + x * 4] + 18 * scanS[(y - 1) * strideS + (x + 1) * 4] + 10 * scanS[(y - 1) * strideS + (x + 2) * 4] + 2 * scanS[(y - 1) * strideS + (x + 3) * 4] - 2 * scanS[(y - 1) * strideS + (x + 4) * 4] - 3 * scanS[(y - 1) * strideS + (x + 5) * 4] - 3 * scanS[(y - 1) * strideS + (x + 6) * 4] - 1 * scanS[(y - 1) * strideS + (x + 7) * 4] - 1 * scanS[(y - 1) * strideS + (x + 8) * 4]
						- 1 * scanS[(y + 0) * strideS + (x - 8) * 4] - 1 * scanS[(y + 0) * strideS + (x - 7) * 4] - 3 * scanS[(y + 0) * strideS + (x - 6) * 4] - 3 * scanS[(y + 0) * strideS + (x - 5) * 4] - 3 * scanS[(y + 0) * strideS + (x - 4) * 4] + 4 * scanS[(y + 0) * strideS + (x - 3) * 4] + 12 * scanS[(y + 0) * strideS + (x - 2) * 4] + 21 * scanS[(y + 0) * strideS + (x - 1) * 4] + 24 * scanS[(y + 0) * strideS + x * 4] + 21 * scanS[(y + 0) * strideS + (x + 1) * 4] + 12 * scanS[(y + 0) * strideS + (x + 2) * 4] + 4 * scanS[(y + 0) * strideS + (x + 3) * 4] - 3 * scanS[(y + 0) * strideS + (x + 4) * 4] - 3 * scanS[(y + 0) * strideS + (x + 5) * 4] - 3 * scanS[(y + 0) * strideS + (x + 6) * 4] - 1 * scanS[(y + 0) * strideS + (x + 7) * 4] - 1 * scanS[(y + 0) * strideS + (x + 8) * 4]
						- 1 * scanS[(y + 1) * strideS + (x - 8) * 4] - 1 * scanS[(y + 1) * strideS + (x - 7) * 4] - 3 * scanS[(y + 1) * strideS + (x - 6) * 4] - 3 * scanS[(y + 1) * strideS + (x - 5) * 4] - 2 * scanS[(y + 1) * strideS + (x - 4) * 4] + 2 * scanS[(y + 1) * strideS + (x - 3) * 4] + 10 * scanS[(y + 1) * strideS + (x - 2) * 4] + 18 * scanS[(y + 1) * strideS + (x - 1) * 4] + 21 * scanS[(y + 1) * strideS + x * 4] + 18 * scanS[(y + 1) * strideS + (x + 1) * 4] + 10 * scanS[(y + 1) * strideS + (x + 2) * 4] + 2 * scanS[(y + 1) * strideS + (x + 3) * 4] - 2 * scanS[(y + 1) * strideS + (x + 4) * 4] - 3 * scanS[(y + 1) * strideS + (x + 5) * 4] - 3 * scanS[(y + 1) * strideS + (x + 6) * 4] - 1 * scanS[(y + 1) * strideS + (x + 7) * 4] - 1 * scanS[(y + 1) * strideS + (x + 8) * 4]
						- 1 * scanS[(y + 2) * strideS + (x - 8) * 4] - 1 * scanS[(y + 2) * strideS + (x - 7) * 4] - 3 * scanS[(y + 2) * strideS + (x - 6) * 4] - 3 * scanS[(y + 2) * strideS + (x - 5) * 4] - 3 * scanS[(y + 2) * strideS + (x - 4) * 4] + 04 * scanS[(y + 2) * strideS + (x - 2) * 4] + 10 * scanS[(y + 2) * strideS + (x - 1) * 4] + 12 * scanS[(y + 2) * strideS + x * 4] + 10 * scanS[(y + 2) * strideS + (x + 1) * 4] + 04 * scanS[(y + 2) * strideS + (x + 2) * 4] - 3 * scanS[(y + 2) * strideS + (x + 4) * 4] - 3 * scanS[(y + 2) * strideS + (x + 5) * 4] - 3 * scanS[(y + 2) * strideS + (x + 6) * 4] - 1 * scanS[(y + 2) * strideS + (x + 7) * 4] - 1 * scanS[(y + 2) * strideS + (x + 8) * 4]
														  - 1 * scanS[(y + 3) * strideS + (x - 7) * 4] - 2 * scanS[(y + 3) * strideS + (x - 6) * 4] - 3 * scanS[(y + 3) * strideS + (x - 5) * 4] - 3 * scanS[(y + 3) * strideS + (x - 4) * 4] - 3 * scanS[(y + 3) * strideS + (x - 3) * 4] + 02 * scanS[(y + 3) * strideS + (x - 1) * 4] + 04 * scanS[(y + 3) * strideS + x * 4] + 02 * scanS[(y + 3) * strideS + (x + 1) * 4] - 3 * scanS[(y + 3) * strideS + (x + 3) * 4] - 3 * scanS[(y + 3) * strideS + (x + 4) * 4] - 3 * scanS[(y + 3) * strideS + (x + 5) * 4] - 2 * scanS[(y + 3) * strideS + (x + 6) * 4] - 1 * scanS[(y + 3) * strideS + (x + 7) * 4]
														  - 1 * scanS[(y + 4) * strideS + (x - 7) * 4] - 1 * scanS[(y + 4) * strideS + (x - 6) * 4] - 2 * scanS[(y + 4) * strideS + (x - 5) * 4] - 3 * scanS[(y + 4) * strideS + (x - 4) * 4] - 3 * scanS[(y + 4) * strideS + (x - 3) * 4] - 03 * scanS[(y + 4) * strideS + (x - 2) * 4] - 02 * scanS[(y + 4) * strideS + (x - 1) * 4] - 03 * scanS[(y + 4) * strideS + x * 4] - 02 * scanS[(y + 4) * strideS + (x + 1) * 4] - 03 * scanS[(y + 4) * strideS + (x + 2) * 4] - 3 * scanS[(y + 4) * strideS + (x + 3) * 4] - 3 * scanS[(y + 4) * strideS + (x + 4) * 4] - 2 * scanS[(y + 4) * strideS + (x + 5) * 4] - 1 * scanS[(y + 4) * strideS + (x + 6) * 4] - 1 * scanS[(y + 4) * strideS + (x + 7) * 4]
																							- 1 * scanS[(y + 5) * strideS + (x - 6) * 4] - 1 * scanS[(y + 5) * strideS + (x - 5) * 4] - 2 * scanS[(y + 5) * strideS + (x - 4) * 4] - 3 * scanS[(y + 5) * strideS + (x - 3) * 4] - 03 * scanS[(y + 5) * strideS + (x - 2) * 4] - 03 * scanS[(y + 5) * strideS + (x - 1) * 4] - 03 * scanS[(y + 5) * strideS + x * 4] - 03 * scanS[(y + 5) * strideS + (x + 1) * 4] - 03 * scanS[(y + 5) * strideS + (x + 2) * 4] - 3 * scanS[(y + 5) * strideS + (x + 3) * 4] - 2 * scanS[(y + 5) * strideS + (x + 4) * 4] - 1 * scanS[(y + 5) * strideS + (x + 5) * 4] - 1 * scanS[(y + 5) * strideS + (x + 6) * 4]
																							- 1 * scanS[(y + 6) * strideS + (x - 6) * 4] - 1 * scanS[(y + 6) * strideS + (x - 5) * 4] - 1 * scanS[(y + 6) * strideS + (x - 4) * 4] - 2 * scanS[(y + 6) * strideS + (x - 3) * 4] - 03 * scanS[(y + 6) * strideS + (x - 2) * 4] - 03 * scanS[(y + 6) * strideS + (x - 1) * 4] - 03 * scanS[(y + 6) * strideS + x * 4] - 03 * scanS[(y + 6) * strideS + (x + 1) * 4] - 03 * scanS[(y + 6) * strideS + (x + 2) * 4] - 2 * scanS[(y + 6) * strideS + (x + 3) * 4] - 1 * scanS[(y + 6) * strideS + (x + 4) * 4] - 1 * scanS[(y + 6) * strideS + (x + 5) * 4] - 1 * scanS[(y + 6) * strideS + (x + 6) * 4]
																																								- 1 * scanS[(y + 7) * strideS + (x - 4) * 4] - 1 * scanS[(y + 7) * strideS + (x - 3) * 4] - 01 * scanS[(y + 7) * strideS + (x - 2) * 4] - 01 * scanS[(y + 7) * strideS + (x - 1) * 4] - 01 * scanS[(y + 7) * strideS + x * 4] - 01 * scanS[(y + 7) * strideS + (x + 1) * 4] - 01 * scanS[(y + 7) * strideS + (x + 2) * 4] - 1 * scanS[(y + 7) * strideS + (x + 3) * 4] - 1 * scanS[(y + 7) * strideS + (x + 4) * 4]
																																																									- 01 * scanS[(y + 8) * strideS + (x - 2) * 4] - 01 * scanS[(y + 8) * strideS + (x - 1) * 4] - 01 * scanS[(y + 8) * strideS + x * 4] - 01 * scanS[(y + 8) * strideS + (x + 1) * 4] - 01 * scanS[(y + 8) * strideS + (x + 2) * 4]

						;

					edgeG =
																																																												-01 * scanS[(y - 8) * strideS + (x - 2) * 4 + 1] - 01 * scanS[(y - 8) * strideS + (x - 1) * 4 + 1] - 01 * scanS[(y - 8) * strideS + x * 4 + 1] - 01 * scanS[(y - 8) * strideS + (x + 1) * 4 + 1] - 01 * scanS[(y - 8) * strideS + (x + 2) * 4 + 1]
																																										- 1 * scanS[(y - 7) * strideS + (x - 4) * 4 + 1] - 1 * scanS[(y - 7) * strideS + (x - 3) * 4 + 1] - 01 * scanS[(y - 7) * strideS + (x - 2) * 4 + 1] - 01 * scanS[(y - 7) * strideS + (x - 1) * 4 + 1] - 01 * scanS[(y - 7) * strideS + x * 4 + 1] - 01 * scanS[(y - 7) * strideS + (x + 1) * 4 + 1] - 01 * scanS[(y - 7) * strideS + (x + 2) * 4 + 1] - 1 * scanS[(y - 7) * strideS + (x + 3) * 4 + 1] - 1 * scanS[(y - 7) * strideS + (x + 4) * 4 + 1]
																								- 1 * scanS[(y - 6) * strideS + (x - 6) * 4 + 1] - 1 * scanS[(y - 6) * strideS + (x - 5) * 4 + 1] - 1 * scanS[(y - 6) * strideS + (x - 4) * 4 + 1] - 2 * scanS[(y - 6) * strideS + (x - 3) * 4 + 1] - 03 * scanS[(y - 6) * strideS + (x - 2) * 4 + 1] - 03 * scanS[(y - 6) * strideS + (x - 1) * 4 + 1] - 03 * scanS[(y - 6) * strideS + x * 4 + 1] - 03 * scanS[(y - 6) * strideS + (x + 1) * 4 + 1] - 03 * scanS[(y - 6) * strideS + (x + 2) * 4 + 1] - 2 * scanS[(y - 6) * strideS + (x + 3) * 4 + 1] - 1 * scanS[(y - 6) * strideS + (x + 4) * 4 + 1] - 1 * scanS[(y - 6) * strideS + (x + 5) * 4 + 1] - 1 * scanS[(y - 6) * strideS + (x + 6) * 4 + 1]
																								- 1 * scanS[(y - 5) * strideS + (x - 6) * 4 + 1] - 1 * scanS[(y - 5) * strideS + (x - 5) * 4 + 1] - 2 * scanS[(y - 5) * strideS + (x - 4) * 4 + 1] - 3 * scanS[(y - 5) * strideS + (x - 3) * 4 + 1] - 03 * scanS[(y - 5) * strideS + (x - 2) * 4 + 1] - 03 * scanS[(y - 5) * strideS + (x - 1) * 4 + 1] - 03 * scanS[(y - 5) * strideS + x * 4 + 1] - 03 * scanS[(y - 5) * strideS + (x + 1) * 4 + 1] - 03 * scanS[(y - 5) * strideS + (x + 2) * 4 + 1] - 3 * scanS[(y - 5) * strideS + (x + 3) * 4 + 1] - 2 * scanS[(y - 5) * strideS + (x + 4) * 4 + 1] - 1 * scanS[(y - 5) * strideS + (x + 5) * 4 + 1] - 1 * scanS[(y - 5) * strideS + (x + 6) * 4 + 1]
															- 1 * scanS[(y - 4) * strideS + (x - 7) * 4 + 1] - 1 * scanS[(y - 4) * strideS + (x - 6) * 4 + 1] - 2 * scanS[(y - 4) * strideS + (x - 5) * 4 + 1] - 3 * scanS[(y - 4) * strideS + (x - 4) * 4 + 1] - 3 * scanS[(y - 4) * strideS + (x - 3) * 4 + 1] - 03 * scanS[(y - 4) * strideS + (x - 2) * 4 + 1] - 02 * scanS[(y - 4) * strideS + (x - 1) * 4 + 1] - 03 * scanS[(y - 4) * strideS + x * 4 + 1] - 02 * scanS[(y - 4) * strideS + (x + 1) * 4 + 1] - 03 * scanS[(y - 4) * strideS + (x + 2) * 4 + 1] - 3 * scanS[(y - 4) * strideS + (x + 3) * 4 + 1] - 3 * scanS[(y - 4) * strideS + (x + 4) * 4 + 1] - 2 * scanS[(y - 4) * strideS + (x + 5) * 4 + 1] - 1 * scanS[(y - 4) * strideS + (x + 6) * 4 + 1] - 1 * scanS[(y - 4) * strideS + (x + 7) * 4 + 1]
															- 1 * scanS[(y - 3) * strideS + (x - 7) * 4 + 1] - 2 * scanS[(y - 3) * strideS + (x - 6) * 4 + 1] - 3 * scanS[(y - 3) * strideS + (x - 5) * 4 + 1] - 3 * scanS[(y - 3) * strideS + (x - 4) * 4 + 1] - 3 * scanS[(y - 3) * strideS + (x - 3) * 4 + 1] + 02 * scanS[(y - 3) * strideS + (x - 1) * 4 + 1] + 04 * scanS[(y - 3) * strideS + x * 4 + 1] + 02 * scanS[(y - 3) * strideS + (x + 1) * 4 + 1] - 3 * scanS[(y - 3) * strideS + (x + 3) * 4 + 1] - 3 * scanS[(y - 3) * strideS + (x + 4) * 4 + 1] - 3 * scanS[(y - 3) * strideS + (x + 5) * 4 + 1] - 2 * scanS[(y - 3) * strideS + (x + 6) * 4 + 1] - 1 * scanS[(y - 3) * strideS + (x + 7) * 4 + 1]
						- 1 * scanS[(y - 2) * strideS + (x - 8) * 4 + 1] - 1 * scanS[(y - 2) * strideS + (x - 7) * 4 + 1] - 3 * scanS[(y - 2) * strideS + (x - 6) * 4 + 1] - 3 * scanS[(y - 2) * strideS + (x - 5) * 4 + 1] - 3 * scanS[(y - 2) * strideS + (x - 4) * 4 + 1] + 04 * scanS[(y - 2) * strideS + (x - 2) * 4 + 1] + 10 * scanS[(y - 2) * strideS + (x - 1) * 4 + 1] + 12 * scanS[(y - 2) * strideS + x * 4 + 1] + 10 * scanS[(y - 2) * strideS + (x + 1) * 4 + 1] + 04 * scanS[(y - 2) * strideS + (x + 2) * 4 + 1] - 3 * scanS[(y - 2) * strideS + (x + 4) * 4 + 1] - 3 * scanS[(y - 2) * strideS + (x + 5) * 4 + 1] - 3 * scanS[(y - 2) * strideS + (x + 6) * 4 + 1] - 1 * scanS[(y - 2) * strideS + (x + 7) * 4 + 1] - 1 * scanS[(y - 2) * strideS + (x + 8) * 4 + 1]
						- 1 * scanS[(y - 1) * strideS + (x - 8) * 4 + 1] - 1 * scanS[(y - 1) * strideS + (x - 7) * 4 + 1] - 3 * scanS[(y - 1) * strideS + (x - 6) * 4 + 1] - 3 * scanS[(y - 1) * strideS + (x - 5) * 4 + 1] - 2 * scanS[(y - 1) * strideS + (x - 4) * 4 + 1] + 2 * scanS[(y - 1) * strideS + (x - 3) * 4 + 1] + 10 * scanS[(y - 1) * strideS + (x - 2) * 4 + 1] + 18 * scanS[(y - 1) * strideS + (x - 1) * 4 + 1] + 21 * scanS[(y - 1) * strideS + x * 4 + 1] + 18 * scanS[(y - 1) * strideS + (x + 1) * 4 + 1] + 10 * scanS[(y - 1) * strideS + (x + 2) * 4 + 1] + 2 * scanS[(y - 1) * strideS + (x + 3) * 4 + 1] - 2 * scanS[(y - 1) * strideS + (x + 4) * 4 + 1] - 3 * scanS[(y - 1) * strideS + (x + 5) * 4 + 1] - 3 * scanS[(y - 1) * strideS + (x + 6) * 4 + 1] - 1 * scanS[(y - 1) * strideS + (x + 7) * 4 + 1] - 1 * scanS[(y - 1) * strideS + (x + 8) * 4 + 1]
						- 1 * scanS[(y + 0) * strideS + (x - 8) * 4 + 1] - 1 * scanS[(y + 0) * strideS + (x - 7) * 4 + 1] - 3 * scanS[(y + 0) * strideS + (x - 6) * 4 + 1] - 3 * scanS[(y + 0) * strideS + (x - 5) * 4 + 1] - 3 * scanS[(y + 0) * strideS + (x - 4) * 4 + 1] + 4 * scanS[(y + 0) * strideS + (x - 3) * 4 + 1] + 12 * scanS[(y + 0) * strideS + (x - 2) * 4 + 1] + 21 * scanS[(y + 0) * strideS + (x - 1) * 4 + 1] + 24 * scanS[(y + 0) * strideS + x * 4 + 1] + 21 * scanS[(y + 0) * strideS + (x + 1) * 4 + 1] + 12 * scanS[(y + 0) * strideS + (x + 2) * 4 + 1] + 4 * scanS[(y + 0) * strideS + (x + 3) * 4 + 1] - 3 * scanS[(y + 0) * strideS + (x + 4) * 4 + 1] - 3 * scanS[(y + 0) * strideS + (x + 5) * 4 + 1] - 3 * scanS[(y + 0) * strideS + (x + 6) * 4 + 1] - 1 * scanS[(y + 0) * strideS + (x + 7) * 4 + 1] - 1 * scanS[(y + 0) * strideS + (x + 8) * 4 + 1]
						- 1 * scanS[(y + 1) * strideS + (x - 8) * 4 + 1] - 1 * scanS[(y + 1) * strideS + (x - 7) * 4 + 1] - 3 * scanS[(y + 1) * strideS + (x - 6) * 4 + 1] - 3 * scanS[(y + 1) * strideS + (x - 5) * 4 + 1] - 2 * scanS[(y + 1) * strideS + (x - 4) * 4 + 1] + 2 * scanS[(y + 1) * strideS + (x - 3) * 4 + 1] + 10 * scanS[(y + 1) * strideS + (x - 2) * 4 + 1] + 18 * scanS[(y + 1) * strideS + (x - 1) * 4 + 1] + 21 * scanS[(y + 1) * strideS + x * 4 + 1] + 18 * scanS[(y + 1) * strideS + (x + 1) * 4 + 1] + 10 * scanS[(y + 1) * strideS + (x + 2) * 4 + 1] + 2 * scanS[(y + 1) * strideS + (x + 3) * 4 + 1] - 2 * scanS[(y + 1) * strideS + (x + 4) * 4 + 1] - 3 * scanS[(y + 1) * strideS + (x + 5) * 4 + 1] - 3 * scanS[(y + 1) * strideS + (x + 6) * 4 + 1] - 1 * scanS[(y + 1) * strideS + (x + 7) * 4 + 1] - 1 * scanS[(y + 1) * strideS + (x + 8) * 4 + 1]
						- 1 * scanS[(y + 2) * strideS + (x - 8) * 4 + 1] - 1 * scanS[(y + 2) * strideS + (x - 7) * 4 + 1] - 3 * scanS[(y + 2) * strideS + (x - 6) * 4 + 1] - 3 * scanS[(y + 2) * strideS + (x - 5) * 4 + 1] - 3 * scanS[(y + 2) * strideS + (x - 4) * 4 + 1] + 04 * scanS[(y + 2) * strideS + (x - 2) * 4 + 1] + 10 * scanS[(y + 2) * strideS + (x - 1) * 4 + 1] + 12 * scanS[(y + 2) * strideS + x * 4 + 1] + 10 * scanS[(y + 2) * strideS + (x + 1) * 4 + 1] + 04 * scanS[(y + 2) * strideS + (x + 2) * 4 + 1] - 3 * scanS[(y + 2) * strideS + (x + 4) * 4 + 1] - 3 * scanS[(y + 2) * strideS + (x + 5) * 4 + 1] - 3 * scanS[(y + 2) * strideS + (x + 6) * 4 + 1] - 1 * scanS[(y + 2) * strideS + (x + 7) * 4 + 1] - 1 * scanS[(y + 2) * strideS + (x + 8) * 4 + 1]
															- 1 * scanS[(y + 3) * strideS + (x - 7) * 4 + 1] - 2 * scanS[(y + 3) * strideS + (x - 6) * 4 + 1] - 3 * scanS[(y + 3) * strideS + (x - 5) * 4 + 1] - 3 * scanS[(y + 3) * strideS + (x - 4) * 4 + 1] - 3 * scanS[(y + 3) * strideS + (x - 3) * 4 + 1] + 02 * scanS[(y + 3) * strideS + (x - 1) * 4 + 1] + 04 * scanS[(y + 3) * strideS + x * 4 + 1] + 02 * scanS[(y + 3) * strideS + (x + 1) * 4 + 1] - 3 * scanS[(y + 3) * strideS + (x + 3) * 4 + 1] - 3 * scanS[(y + 3) * strideS + (x + 4) * 4 + 1] - 3 * scanS[(y + 3) * strideS + (x + 5) * 4 + 1] - 2 * scanS[(y + 3) * strideS + (x + 6) * 4 + 1] - 1 * scanS[(y + 3) * strideS + (x + 7) * 4 + 1]
															- 1 * scanS[(y + 4) * strideS + (x - 7) * 4 + 1] - 1 * scanS[(y + 4) * strideS + (x - 6) * 4 + 1] - 2 * scanS[(y + 4) * strideS + (x - 5) * 4 + 1] - 3 * scanS[(y + 4) * strideS + (x - 4) * 4 + 1] - 3 * scanS[(y + 4) * strideS + (x - 3) * 4 + 1] - 03 * scanS[(y + 4) * strideS + (x - 2) * 4 + 1] - 02 * scanS[(y + 4) * strideS + (x - 1) * 4 + 1] - 03 * scanS[(y + 4) * strideS + x * 4 + 1] - 02 * scanS[(y + 4) * strideS + (x + 1) * 4 + 1] - 03 * scanS[(y + 4) * strideS + (x + 2) * 4 + 1] - 3 * scanS[(y + 4) * strideS + (x + 3) * 4 + 1] - 3 * scanS[(y + 4) * strideS + (x + 4) * 4 + 1] - 2 * scanS[(y + 4) * strideS + (x + 5) * 4 + 1] - 1 * scanS[(y + 4) * strideS + (x + 6) * 4 + 1] - 1 * scanS[(y + 4) * strideS + (x + 7) * 4 + 1]
																								- 1 * scanS[(y + 5) * strideS + (x - 6) * 4 + 1] - 1 * scanS[(y + 5) * strideS + (x - 5) * 4 + 1] - 2 * scanS[(y + 5) * strideS + (x - 4) * 4 + 1] - 3 * scanS[(y + 5) * strideS + (x - 3) * 4 + 1] - 03 * scanS[(y + 5) * strideS + (x - 2) * 4 + 1] - 03 * scanS[(y + 5) * strideS + (x - 1) * 4 + 1] - 03 * scanS[(y + 5) * strideS + x * 4 + 1] - 03 * scanS[(y + 5) * strideS + (x + 1) * 4 + 1] - 03 * scanS[(y + 5) * strideS + (x + 2) * 4 + 1] - 3 * scanS[(y + 5) * strideS + (x + 3) * 4 + 1] - 2 * scanS[(y + 5) * strideS + (x + 4) * 4 + 1] - 1 * scanS[(y + 5) * strideS + (x + 5) * 4 + 1] - 1 * scanS[(y + 5) * strideS + (x + 6) * 4 + 1]
																								- 1 * scanS[(y + 6) * strideS + (x - 6) * 4 + 1] - 1 * scanS[(y + 6) * strideS + (x - 5) * 4 + 1] - 1 * scanS[(y + 6) * strideS + (x - 4) * 4 + 1] - 2 * scanS[(y + 6) * strideS + (x - 3) * 4 + 1] - 03 * scanS[(y + 6) * strideS + (x - 2) * 4 + 1] - 03 * scanS[(y + 6) * strideS + (x - 1) * 4 + 1] - 03 * scanS[(y + 6) * strideS + x * 4 + 1] - 03 * scanS[(y + 6) * strideS + (x + 1) * 4 + 1] - 03 * scanS[(y + 6) * strideS + (x + 2) * 4 + 1] - 2 * scanS[(y + 6) * strideS + (x + 3) * 4 + 1] - 1 * scanS[(y + 6) * strideS + (x + 4) * 4 + 1] - 1 * scanS[(y + 6) * strideS + (x + 5) * 4 + 1] - 1 * scanS[(y + 6) * strideS + (x + 6) * 4 + 1]
																																										- 1 * scanS[(y + 7) * strideS + (x - 4) * 4 + 1] - 1 * scanS[(y + 7) * strideS + (x - 3) * 4 + 1] - 01 * scanS[(y + 7) * strideS + (x - 2) * 4 + 1] - 01 * scanS[(y + 7) * strideS + (x - 1) * 4 + 1] - 01 * scanS[(y + 7) * strideS + x * 4 + 1] - 01 * scanS[(y + 7) * strideS + (x + 1) * 4 + 1] - 01 * scanS[(y + 7) * strideS + (x + 2) * 4 + 1] - 1 * scanS[(y + 7) * strideS + (x + 3) * 4 + 1] - 1 * scanS[(y + 7) * strideS + (x + 4) * 4 + 1]
																																																												- 01 * scanS[(y + 8) * strideS + (x - 2) * 4 + 1] - 01 * scanS[(y + 8) * strideS + (x - 1) * 4 + 1] - 01 * scanS[(y + 8) * strideS + x * 4 + 1] - 01 * scanS[(y + 8) * strideS + (x + 1) * 4 + 1] - 01 * scanS[(y + 8) * strideS + (x + 2) * 4 + 1]
					;
					edgeR =
																																																												-01 * scanS[(y - 8) * strideS + (x - 2) * 4 + 2] - 01 * scanS[(y - 8) * strideS + (x - 1) * 4 + 2] - 01 * scanS[(y - 8) * strideS + x * 4 + 2] - 01 * scanS[(y - 8) * strideS + (x + 1) * 4 + 2] - 01 * scanS[(y - 8) * strideS + (x + 2) * 4 + 2]
																																										- 1 * scanS[(y - 7) * strideS + (x - 4) * 4 + 2] - 1 * scanS[(y - 7) * strideS + (x - 3) * 4 + 2] - 01 * scanS[(y - 7) * strideS + (x - 2) * 4 + 2] - 01 * scanS[(y - 7) * strideS + (x - 1) * 4 + 2] - 01 * scanS[(y - 7) * strideS + x * 4 + 2] - 01 * scanS[(y - 7) * strideS + (x + 1) * 4 + 2] - 01 * scanS[(y - 7) * strideS + (x + 2) * 4 + 2] - 1 * scanS[(y - 7) * strideS + (x + 3) * 4 + 2] - 1 * scanS[(y - 7) * strideS + (x + 4) * 4 + 2]
																								- 1 * scanS[(y - 6) * strideS + (x - 6) * 4 + 2] - 1 * scanS[(y - 6) * strideS + (x - 5) * 4 + 2] - 1 * scanS[(y - 6) * strideS + (x - 4) * 4 + 2] - 2 * scanS[(y - 6) * strideS + (x - 3) * 4 + 2] - 03 * scanS[(y - 6) * strideS + (x - 2) * 4 + 2] - 03 * scanS[(y - 6) * strideS + (x - 1) * 4 + 2] - 03 * scanS[(y - 6) * strideS + x * 4 + 2] - 03 * scanS[(y - 6) * strideS + (x + 1) * 4 + 2] - 03 * scanS[(y - 6) * strideS + (x + 2) * 4 + 2] - 2 * scanS[(y - 6) * strideS + (x + 3) * 4 + 2] - 1 * scanS[(y - 6) * strideS + (x + 4) * 4 + 2] - 1 * scanS[(y - 6) * strideS + (x + 5) * 4 + 2] - 1 * scanS[(y - 6) * strideS + (x + 6) * 4 + 2]
																								- 1 * scanS[(y - 5) * strideS + (x - 6) * 4 + 2] - 1 * scanS[(y - 5) * strideS + (x - 5) * 4 + 2] - 2 * scanS[(y - 5) * strideS + (x - 4) * 4 + 2] - 3 * scanS[(y - 5) * strideS + (x - 3) * 4 + 2] - 03 * scanS[(y - 5) * strideS + (x - 2) * 4 + 2] - 03 * scanS[(y - 5) * strideS + (x - 1) * 4 + 2] - 03 * scanS[(y - 5) * strideS + x * 4 + 2] - 03 * scanS[(y - 5) * strideS + (x + 1) * 4 + 2] - 03 * scanS[(y - 5) * strideS + (x + 2) * 4 + 2] - 3 * scanS[(y - 5) * strideS + (x + 3) * 4 + 2] - 2 * scanS[(y - 5) * strideS + (x + 4) * 4 + 2] - 1 * scanS[(y - 5) * strideS + (x + 5) * 4 + 2] - 1 * scanS[(y - 5) * strideS + (x + 6) * 4 + 2]
															- 1 * scanS[(y - 4) * strideS + (x - 7) * 4 + 2] - 1 * scanS[(y - 4) * strideS + (x - 6) * 4 + 2] - 2 * scanS[(y - 4) * strideS + (x - 5) * 4 + 2] - 3 * scanS[(y - 4) * strideS + (x - 4) * 4 + 2] - 3 * scanS[(y - 4) * strideS + (x - 3) * 4 + 2] - 03 * scanS[(y - 4) * strideS + (x - 2) * 4 + 2] - 02 * scanS[(y - 4) * strideS + (x - 1) * 4 + 2] - 03 * scanS[(y - 4) * strideS + x * 4 + 2] - 02 * scanS[(y - 4) * strideS + (x + 1) * 4 + 2] - 03 * scanS[(y - 4) * strideS + (x + 2) * 4 + 2] - 3 * scanS[(y - 4) * strideS + (x + 3) * 4 + 2] - 3 * scanS[(y - 4) * strideS + (x + 4) * 4 + 2] - 2 * scanS[(y - 4) * strideS + (x + 5) * 4 + 2] - 1 * scanS[(y - 4) * strideS + (x + 6) * 4 + 2] - 1 * scanS[(y - 4) * strideS + (x + 7) * 4 + 2]
															- 1 * scanS[(y - 3) * strideS + (x - 7) * 4 + 2] - 2 * scanS[(y - 3) * strideS + (x - 6) * 4 + 2] - 3 * scanS[(y - 3) * strideS + (x - 5) * 4 + 2] - 3 * scanS[(y - 3) * strideS + (x - 4) * 4 + 2] - 3 * scanS[(y - 3) * strideS + (x - 3) * 4 + 2] + 02 * scanS[(y - 3) * strideS + (x - 1) * 4 + 2] + 04 * scanS[(y - 3) * strideS + x * 4 + 2] + 02 * scanS[(y - 3) * strideS + (x + 1) * 4 + 2] - 3 * scanS[(y - 3) * strideS + (x + 3) * 4 + 2] - 3 * scanS[(y - 3) * strideS + (x + 4) * 4 + 2] - 3 * scanS[(y - 3) * strideS + (x + 5) * 4 + 2] - 2 * scanS[(y - 3) * strideS + (x + 6) * 4 + 2] - 1 * scanS[(y - 3) * strideS + (x + 7) * 4 + 2]
						- 1 * scanS[(y - 2) * strideS + (x - 8) * 4 + 2] - 1 * scanS[(y - 2) * strideS + (x - 7) * 4 + 2] - 3 * scanS[(y - 2) * strideS + (x - 6) * 4 + 2] - 3 * scanS[(y - 2) * strideS + (x - 5) * 4 + 2] - 3 * scanS[(y - 2) * strideS + (x - 4) * 4 + 2] + 04 * scanS[(y - 2) * strideS + (x - 2) * 4 + 2] + 10 * scanS[(y - 2) * strideS + (x - 1) * 4 + 2] + 12 * scanS[(y - 2) * strideS + x * 4 + 2] + 10 * scanS[(y - 2) * strideS + (x + 1) * 4 + 2] + 04 * scanS[(y - 2) * strideS + (x + 2) * 4 + 2] - 3 * scanS[(y - 2) * strideS + (x + 4) * 4 + 2] - 3 * scanS[(y - 2) * strideS + (x + 5) * 4 + 2] - 3 * scanS[(y - 2) * strideS + (x + 6) * 4 + 2] - 1 * scanS[(y - 2) * strideS + (x + 7) * 4 + 2] - 1 * scanS[(y - 2) * strideS + (x + 8) * 4 + 2]
						- 1 * scanS[(y - 1) * strideS + (x - 8) * 4 + 2] - 1 * scanS[(y - 1) * strideS + (x - 7) * 4 + 2] - 3 * scanS[(y - 1) * strideS + (x - 6) * 4 + 2] - 3 * scanS[(y - 1) * strideS + (x - 5) * 4 + 2] - 2 * scanS[(y - 1) * strideS + (x - 4) * 4 + 2] + 2 * scanS[(y - 1) * strideS + (x - 3) * 4 + 2] + 10 * scanS[(y - 1) * strideS + (x - 2) * 4 + 2] + 18 * scanS[(y - 1) * strideS + (x - 1) * 4 + 2] + 21 * scanS[(y - 1) * strideS + x * 4 + 2] + 18 * scanS[(y - 1) * strideS + (x + 1) * 4 + 2] + 10 * scanS[(y - 1) * strideS + (x + 2) * 4 + 2] + 2 * scanS[(y - 1) * strideS + (x + 3) * 4 + 2] - 2 * scanS[(y - 1) * strideS + (x + 4) * 4 + 2] - 3 * scanS[(y - 1) * strideS + (x + 5) * 4 + 2] - 3 * scanS[(y - 1) * strideS + (x + 6) * 4 + 2] - 1 * scanS[(y - 1) * strideS + (x + 7) * 4 + 2] - 1 * scanS[(y - 1) * strideS + (x + 8) * 4 + 2]
						- 1 * scanS[(y + 0) * strideS + (x - 8) * 4 + 2] - 1 * scanS[(y + 0) * strideS + (x - 7) * 4 + 2] - 3 * scanS[(y + 0) * strideS + (x - 6) * 4 + 2] - 3 * scanS[(y + 0) * strideS + (x - 5) * 4 + 2] - 3 * scanS[(y + 0) * strideS + (x - 4) * 4 + 2] + 4 * scanS[(y + 0) * strideS + (x - 3) * 4 + 2] + 12 * scanS[(y + 0) * strideS + (x - 2) * 4 + 2] + 21 * scanS[(y + 0) * strideS + (x - 1) * 4 + 2] + 24 * scanS[(y + 0) * strideS + x * 4 + 2] + 21 * scanS[(y + 0) * strideS + (x + 1) * 4 + 2] + 12 * scanS[(y + 0) * strideS + (x + 2) * 4 + 2] + 4 * scanS[(y + 0) * strideS + (x + 3) * 4 + 2] - 3 * scanS[(y + 0) * strideS + (x + 4) * 4 + 2] - 3 * scanS[(y + 0) * strideS + (x + 5) * 4 + 2] - 3 * scanS[(y + 0) * strideS + (x + 6) * 4 + 2] - 1 * scanS[(y + 0) * strideS + (x + 7) * 4 + 2] - 1 * scanS[(y + 0) * strideS + (x + 8) * 4 + 2]
						- 1 * scanS[(y + 1) * strideS + (x - 8) * 4 + 2] - 1 * scanS[(y + 1) * strideS + (x - 7) * 4 + 2] - 3 * scanS[(y + 1) * strideS + (x - 6) * 4 + 2] - 3 * scanS[(y + 1) * strideS + (x - 5) * 4 + 2] - 2 * scanS[(y + 1) * strideS + (x - 4) * 4 + 2] + 2 * scanS[(y + 1) * strideS + (x - 3) * 4 + 2] + 10 * scanS[(y + 1) * strideS + (x - 2) * 4 + 2] + 18 * scanS[(y + 1) * strideS + (x - 1) * 4 + 2] + 21 * scanS[(y + 1) * strideS + x * 4 + 2] + 18 * scanS[(y + 1) * strideS + (x + 1) * 4 + 2] + 10 * scanS[(y + 1) * strideS + (x + 2) * 4 + 2] + 2 * scanS[(y + 1) * strideS + (x + 3) * 4 + 2] - 2 * scanS[(y + 1) * strideS + (x + 4) * 4 + 2] - 3 * scanS[(y + 1) * strideS + (x + 5) * 4 + 2] - 3 * scanS[(y + 1) * strideS + (x + 6) * 4 + 2] - 1 * scanS[(y + 1) * strideS + (x + 7) * 4 + 2] - 1 * scanS[(y + 1) * strideS + (x + 8) * 4 + 2]
						- 1 * scanS[(y + 2) * strideS + (x - 8) * 4 + 2] - 1 * scanS[(y + 2) * strideS + (x - 7) * 4 + 2] - 3 * scanS[(y + 2) * strideS + (x - 6) * 4 + 2] - 3 * scanS[(y + 2) * strideS + (x - 5) * 4 + 2] - 3 * scanS[(y + 2) * strideS + (x - 4) * 4 + 2] + 04 * scanS[(y + 2) * strideS + (x - 2) * 4 + 2] + 10 * scanS[(y + 2) * strideS + (x - 1) * 4 + 2] + 12 * scanS[(y + 2) * strideS + x * 4 + 2] + 10 * scanS[(y + 2) * strideS + (x + 1) * 4 + 2] + 04 * scanS[(y + 2) * strideS + (x + 2) * 4 + 2] - 3 * scanS[(y + 2) * strideS + (x + 4) * 4 + 2] - 3 * scanS[(y + 2) * strideS + (x + 5) * 4 + 2] - 3 * scanS[(y + 2) * strideS + (x + 6) * 4 + 2] - 1 * scanS[(y + 2) * strideS + (x + 7) * 4 + 2] - 1 * scanS[(y + 2) * strideS + (x + 8) * 4 + 2]
															- 1 * scanS[(y + 3) * strideS + (x - 7) * 4 + 2] - 2 * scanS[(y + 3) * strideS + (x - 6) * 4 + 2] - 3 * scanS[(y + 3) * strideS + (x - 5) * 4 + 2] - 3 * scanS[(y + 3) * strideS + (x - 4) * 4 + 2] - 3 * scanS[(y + 3) * strideS + (x - 3) * 4 + 2] + 02 * scanS[(y + 3) * strideS + (x - 1) * 4 + 2] + 04 * scanS[(y + 3) * strideS + x * 4 + 2] + 02 * scanS[(y + 3) * strideS + (x + 1) * 4 + 2] - 3 * scanS[(y + 3) * strideS + (x + 3) * 4 + 2] - 3 * scanS[(y + 3) * strideS + (x + 4) * 4 + 2] - 3 * scanS[(y + 3) * strideS + (x + 5) * 4 + 2] - 2 * scanS[(y + 3) * strideS + (x + 6) * 4 + 2] - 1 * scanS[(y + 3) * strideS + (x + 7) * 4 + 2]
															- 1 * scanS[(y + 4) * strideS + (x - 7) * 4 + 2] - 1 * scanS[(y + 4) * strideS + (x - 6) * 4 + 2] - 2 * scanS[(y + 4) * strideS + (x - 5) * 4 + 2] - 3 * scanS[(y + 4) * strideS + (x - 4) * 4 + 2] - 3 * scanS[(y + 4) * strideS + (x - 3) * 4 + 2] - 03 * scanS[(y + 4) * strideS + (x - 2) * 4 + 2] - 02 * scanS[(y + 4) * strideS + (x - 1) * 4 + 2] - 03 * scanS[(y + 4) * strideS + x * 4 + 2] - 02 * scanS[(y + 4) * strideS + (x + 1) * 4 + 2] - 03 * scanS[(y + 4) * strideS + (x + 2) * 4 + 2] - 3 * scanS[(y + 4) * strideS + (x + 3) * 4 + 2] - 3 * scanS[(y + 4) * strideS + (x + 4) * 4 + 2] - 2 * scanS[(y + 4) * strideS + (x + 5) * 4 + 2] - 1 * scanS[(y + 4) * strideS + (x + 6) * 4 + 2] - 1 * scanS[(y + 4) * strideS + (x + 7) * 4 + 2]
																								- 1 * scanS[(y + 5) * strideS + (x - 6) * 4 + 2] - 1 * scanS[(y + 5) * strideS + (x - 5) * 4 + 2] - 2 * scanS[(y + 5) * strideS + (x - 4) * 4 + 2] - 3 * scanS[(y + 5) * strideS + (x - 3) * 4 + 2] - 03 * scanS[(y + 5) * strideS + (x - 2) * 4 + 2] - 03 * scanS[(y + 5) * strideS + (x - 1) * 4 + 2] - 03 * scanS[(y + 5) * strideS + x * 4 + 2] - 03 * scanS[(y + 5) * strideS + (x + 1) * 4 + 2] - 03 * scanS[(y + 5) * strideS + (x + 2) * 4 + 2] - 3 * scanS[(y + 5) * strideS + (x + 3) * 4 + 2] - 2 * scanS[(y + 5) * strideS + (x + 4) * 4 + 2] - 1 * scanS[(y + 5) * strideS + (x + 5) * 4 + 2] - 1 * scanS[(y + 5) * strideS + (x + 6) * 4 + 2]
																								- 1 * scanS[(y + 6) * strideS + (x - 6) * 4 + 2] - 1 * scanS[(y + 6) * strideS + (x - 5) * 4 + 2] - 1 * scanS[(y + 6) * strideS + (x - 4) * 4 + 2] - 2 * scanS[(y + 6) * strideS + (x - 3) * 4 + 2] - 03 * scanS[(y + 6) * strideS + (x - 2) * 4 + 2] - 03 * scanS[(y + 6) * strideS + (x - 1) * 4 + 2] - 03 * scanS[(y + 6) * strideS + x * 4 + 2] - 03 * scanS[(y + 6) * strideS + (x + 1) * 4 + 2] - 03 * scanS[(y + 6) * strideS + (x + 2) * 4 + 2] - 2 * scanS[(y + 6) * strideS + (x + 3) * 4 + 2] - 1 * scanS[(y + 6) * strideS + (x + 4) * 4 + 2] - 1 * scanS[(y + 6) * strideS + (x + 5) * 4 + 2] - 1 * scanS[(y + 6) * strideS + (x + 6) * 4 + 2]
																																										- 1 * scanS[(y + 7) * strideS + (x - 4) * 4 + 2] - 1 * scanS[(y + 7) * strideS + (x - 3) * 4 + 2] - 01 * scanS[(y + 7) * strideS + (x - 2) * 4 + 2] - 01 * scanS[(y + 7) * strideS + (x - 1) * 4 + 2] - 01 * scanS[(y + 7) * strideS + x * 4 + 2] - 01 * scanS[(y + 7) * strideS + (x + 1) * 4 + 2] - 01 * scanS[(y + 7) * strideS + (x + 2) * 4 + 2] - 1 * scanS[(y + 7) * strideS + (x + 3) * 4 + 2] - 1 * scanS[(y + 7) * strideS + (x + 4) * 4 + 2]
																																																												- 01 * scanS[(y + 8) * strideS + (x - 2) * 4 + 2] - 01 * scanS[(y + 8) * strideS + (x - 1) * 4 + 2] - 01 * scanS[(y + 8) * strideS + x * 4 + 2] - 01 * scanS[(y + 8) * strideS + (x + 1) * 4 + 2] - 01 * scanS[(y + 8) * strideS + (x + 2) * 4 + 2]
					;

					edge = (edgeB + edgeG + edgeR) / 500 + 127;

					if (edge < 0)
						scanR[y * strideR + x] = 0;
					else if (edge > 255)
						scanR[y * strideR + x] = 255;
					else
						scanR[y * strideR + x] = (byte)edge;
				}
			}

#if DEBUG
			Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + DateTime.Now.Subtract(start).ToString());
			start = DateTime.Now;
#endif

			//left, right
			for (y = 8; y < height - 8; y++)
				for (x = 0; x < 8; x++)
				{
					scanR[y * strideR + x] = scanR[y * strideR + 8];
					scanR[y * strideR + width - 1 - x] = scanR[y * strideR + width - 9];
				}

			//top, bottom
			for (x = 0; x < width; x++)
				for (y = 0; y < 8; y++)
				{
					scanR[y * strideR + x] = scanR[8 * strideR + x];
					scanR[(height - 1 - y) * strideR + x] = scanR[(height - 9) * strideR + x];
				}
		}
		#endregion

		#endregion
	}

}
