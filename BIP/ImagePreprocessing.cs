using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using ImageProcessing.PageObjects;


namespace ImageProcessing
{
	public class ImagePreprocessing
	{
		// Fields
		private const int HEAP_ZERO_MEMORY = 8;
		private static int ph = GetProcessHeap();

		[DllImport("kernel32")]
		private static extern int GetProcessHeap();
		[DllImport("kernel32")]
		private static extern unsafe void* HeapAlloc(int hHeap, int flags, int size);
		[DllImport("kernel32")]
		private static extern unsafe bool HeapFree(int hHeap, int flags, void* block);
		[DllImport("kernel32")]
		private static extern unsafe void* HeapReAlloc(int hHeap, int flags, void* block, int size);
		[DllImport("kernel32")]
		private static extern unsafe int HeapSize(int hHeap, int flags, void* block);



		//PUBLIC METHODS
		#region public methods

		#region Go()
		public static Bitmap Go(Bitmap bitmap, int wThresholdDelta, int minDelta, bool highlightObjects, bool despeckle)
		{
			return Go(bitmap, Rectangle.Empty, wThresholdDelta, minDelta, highlightObjects, despeckle);
		}

		public static Bitmap Go(Bitmap bitmap, Rectangle clip, int wThresholdDelta, int minDelta, bool highlightObjects, bool despeckle)
		{
			Bitmap pBitmap = null;

			if (clip.IsEmpty)
			{
				clip = Rectangle.FromLTRB(0, 0, bitmap.Width, bitmap.Height);
			}
			else
			{
				clip = Rectangle.Intersect(clip, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
			}
			try
			{
				Bitmap edBitmap = null;

				if (bitmap.Width > 4 && bitmap.Height > 4)
				{
					switch (bitmap.PixelFormat)
					{
						case PixelFormat.Format8bppIndexed:
							pBitmap = ImageCopier.Copy(bitmap, clip);
							Smoothing.UnsharpMasking(pBitmap, Rectangle.Empty, 2);
							//edBitmap = EdgeDetector.Binarize(pBitmap, Rectangle.Empty, EdgeDetector.RotatingMaskType.Jirka);
							
							/*if (bitmap.Width > bitmap.Height)
								LightDistributor.SmoothVertically(bitmap);*/

							edBitmap = BinorizationThreshold.Binorize(pBitmap);
							Inverter.Invert(edBitmap);
							pBitmap.Dispose();
							break;
						case PixelFormat.Format24bppRgb:
							//pBitmap = ImageCopier.Copy(bitmap, clip);
							pBitmap = Resampling.Resample(bitmap, clip, PixelsFormat.Format8bppGray);
							Smoothing.UnsharpMasking(pBitmap, Rectangle.Empty, 2);

							//edBitmap = EdgeDetector.Binarize(pBitmap, Rectangle.Empty, EdgeDetector.RotatingMaskType.Kirsch);
							/*if (bitmap.Width > bitmap.Height)
								LightDistributor.SmoothVertically(bitmap);*/

							edBitmap = BinorizationThreshold.Binorize(pBitmap);
							Inverter.Invert(edBitmap);
							pBitmap.Dispose();
							break;
						case PixelFormat.Format32bppArgb:
						case PixelFormat.Format32bppRgb:
							pBitmap = ImageCopier.Copy(bitmap, clip);
							Smoothing.UnsharpMasking(pBitmap, Rectangle.Empty, 2);
							//edBitmap = EdgeDetector.Binarize(pBitmap, Rectangle.Empty, EdgeDetector.RotatingMaskType.Jirka);
							edBitmap = BinorizationThreshold.Binorize(pBitmap);
							Inverter.Invert(edBitmap);
							pBitmap.Dispose();
							break;
						case PixelFormat.Format1bppIndexed:
							edBitmap = EdgeDetector.BinarizeLaplacian(bitmap, clip, 200, 200, 200, minDelta, highlightObjects);
							break;
						default:
							throw new IpException(ErrorCode.ErrorUnsupportedFormat);
					}

					if (despeckle && edBitmap != null)
					{
						NoiseReduction.Despeckle(edBitmap, NoiseReduction.DespeckleSize.Size4x4, ImageProcessing.NoiseReduction.DespeckleMode.WhiteSpecklesOnly, ImageProcessing.NoiseReduction.DespeckleMethod.Regions);
					}
				}
				else
				{
					edBitmap = BinorizationThreshold.Binorize(bitmap);
				}

				if (edBitmap != null)
				{
					Misc.SetBitmapResolution(edBitmap, bitmap.HorizontalResolution, bitmap.VerticalResolution);
				}
#if SAVE_RESULTS
				edBitmap.Save(Debug.SaveToDir + "01 Preprocessing.png", ImageFormat.Png);
#endif
				return edBitmap;
			}
			catch (Exception ex)
			{
				throw new Exception("ImagePreprocessing, Go(): " + ex.Message);
			}
		}
		#endregion

		#region GoDarkBookfold()
		public static Bitmap GoDarkBookfold(Bitmap bitmap, int wThresholdDelta, int minDelta)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			Bitmap edBitmap = null;
			
			if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
			{
				edBitmap = ImageCopier.Copy(bitmap);
				Inverter.Invert(edBitmap);
			}
			else
			{				
				/*if (bitmap.Width > 500 && bitmap.Height > 500)
				{
					int stop1 = ((bitmap.Width / 3) / 8) * 8;
					int stop2 = ((bitmap.Width * 2 / 3) / 8) * 8;

					Bitmap edBitmap1 = Go(bitmap, new Rectangle(0, 0, stop1 + 8, bitmap.Height), wThresholdDelta, minDelta, true, false);
#if SAVE_RESULTS
					edBitmap1.Save(Debug.SaveToDir + "96.png", ImageFormat.Png);
#endif
					Bitmap edBitmap2 = Go(bitmap, Rectangle.FromLTRB(stop1 - 8, 0, stop2 + 8, bitmap.Height), wThresholdDelta, minDelta, false, false);
#if SAVE_RESULTS
					edBitmap2.Save(Debug.SaveToDir + "97.png", ImageFormat.Png);
#endif
					Bitmap edBitmap3 = Go(bitmap, Rectangle.FromLTRB(stop2 - 8, 0, bitmap.Width, bitmap.Height), wThresholdDelta, minDelta, true, false);
#if SAVE_RESULTS
					edBitmap3.Save(Debug.SaveToDir + "98.png", ImageFormat.Png);
#endif
				
					edBitmap = MergeBitmaps(edBitmap1, edBitmap2, edBitmap3, stop1, stop2, bitmap.Width);
					Misc.SetBitmapResolution(edBitmap, bitmap.HorizontalResolution, bitmap.VerticalResolution);
				}
				else*/
					edBitmap = Go(bitmap, Rectangle.Empty, wThresholdDelta, minDelta, true, false);
			}

#if DEBUG
			Console.WriteLine("Total Image Preprocessing: " + DateTime.Now.Subtract(start).ToString());
#endif
			return edBitmap;
		}
		#endregion

		#region GoDarkBookfold()
		/*public static Bitmap GoDarkBookfold(Bitmap bitmap, int wThresholdDelta, int minDelta)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			Bitmap edBitmap = null;
			
			if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
			{
				edBitmap = ImageCopier.Copy(bitmap);
				Inverter.Invert(edBitmap);
			}
			else
			{				
				if (bitmap.Width > 500 && bitmap.Height > 500)
				{
					int stop1 = ((bitmap.Width / 3) / 8) * 8;
					int stop2 = ((bitmap.Width * 2 / 3) / 8) * 8;

					Bitmap edBitmap1 = Go(bitmap, new Rectangle(0, 0, stop1 + 8, bitmap.Height), wThresholdDelta, minDelta, true, false);
#if SAVE_RESULTS
					edBitmap1.Save(Debug.SaveToDir + "96.png", ImageFormat.Png);
#endif
					Bitmap edBitmap2 = Go(bitmap, Rectangle.FromLTRB(stop1 - 8, 0, stop2 + 8, bitmap.Height), wThresholdDelta, minDelta, false, false);
#if SAVE_RESULTS
					edBitmap2.Save(Debug.SaveToDir + "97.png", ImageFormat.Png);
#endif
					Bitmap edBitmap3 = Go(bitmap, Rectangle.FromLTRB(stop2 - 8, 0, bitmap.Width, bitmap.Height), wThresholdDelta, minDelta, true, false);
#if SAVE_RESULTS
					edBitmap3.Save(Debug.SaveToDir + "98.png", ImageFormat.Png);
#endif
				
					edBitmap = MergeBitmaps(edBitmap1, edBitmap2, edBitmap3, stop1, stop2, bitmap.Width);
				}
				else
					edBitmap = Go(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), wThresholdDelta, minDelta, true, false);

				Misc.SetBitmapResolution(edBitmap, bitmap.HorizontalResolution, bitmap.VerticalResolution);
			}
#if SAVE_RESULTS
			edBitmap.Save(Debug.SaveToDir + "01 Preprocessing Dark.png", ImageFormat.Png);
#endif

#if DEBUG
			Console.WriteLine(DateTime.Now.Subtract(start).ToString());
#endif
			return edBitmap;
		}*/
		#endregion
	
		#region GetRidOfBorders()
		public static void GetRidOfBorders(Bitmap bitmap)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			try
			{
				try
				{
					if (bitmap.PixelFormat != PixelFormat.Format1bppIndexed)
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);

					GetRidOfBorders1bpp(bitmap);
				}
				catch (Exception ex)
				{
					throw new Exception("ImagePreprocessing, GetRidOfBorders(): " + ex.Message);
				}
			}
			finally
			{
#if DEBUG
				System.Reflection.MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
				Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "(): " + DateTime.Now.Subtract(start).ToString());
#endif
#if SAVE_RESULTS
				bitmap.Save(Debug.SaveToDir + "02 Preprocessing No Borders.png", ImageFormat.Png);
#endif
			}
		}
		#endregion

		#region GetPageObjects()
		/*public static Symbols GetPageObjects(Bitmap source, int wThresholdDelta, int minDelta)
		{
			Bitmap edBitmap = GoDarkBookfold(source, wThresholdDelta, minDelta);
			GetRidOfBorders(edBitmap);
#if SAVE_RESULTS
			edBitmap.Save(Debug.SaveToDir + "02 Preprocessing No Borders.png", ImageFormat.Png);
#endif
			Symbols pageSymbols = ObjectLocator.FindObjects(edBitmap, Rectangle.Empty, 1);
			edBitmap.Dispose();
#if SAVE_RESULTS
			pageSymbols.DrawToFile(Debug.SaveToDir + "03 Symbols.png", source.Size);
#endif
			return pageSymbols;
		}*/
		#endregion

		#region CutOffBorder()
		public static unsafe void CutOffBorder(Bitmap bitmap, BIP.Geometry.RatioRect ratioClip)
		{
			Rectangle clip = new Rectangle(Convert.ToInt32(ratioClip.X * bitmap.Width), Convert.ToInt32(ratioClip.Y * bitmap.Height), Convert.ToInt32(ratioClip.Width * bitmap.Width), Convert.ToInt32(ratioClip.Height * bitmap.Height));
			CutOffBorder(bitmap, clip);
		}

		public static unsafe void CutOffBorder(Bitmap bitmap, Rectangle clip)
		{
			BitmapData bmpData = null;

			try
			{
				bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
				int stride = bmpData.Stride;

				byte* pSource = (byte*)bmpData.Scan0.ToPointer();

				//top
				for (int y = 0; y < clip.Y; y++)
					for (int x = 0; x < stride; x++)
						pSource[y * stride + x] = 0;

				//bottom
				for (int y = clip.Bottom; y < bitmap.Height; y++)
					for (int x = 0; x < stride; x++)
						pSource[y * stride + x] = 0;

				//left
				for (int y = 0; y < bitmap.Height; y++)
					for (int x = 0; x < clip.X / 8; x++)
						pSource[y * stride + x] = 0;

				int xClip = clip.X / 8;
				byte mask = (byte)(0xFF >> (clip.X & 0x07));
				for (int y = 0; y < bitmap.Height; y++)
					pSource[y * stride + xClip] &= mask;

				//right
				for (int y = 0; y < bitmap.Height; y++)
					for (int x = (clip.Right / 8) + 1; x < stride; x++)
						pSource[y * stride + x] = 0;

				xClip = clip.Right / 8;
				mask = (byte)(0xFF << (clip.X & 0x07));
				for (int y = 0; y < bitmap.Height; y++)
					pSource[y * stride + xClip] &= mask;
			}
			finally
			{
				if (bmpData != null)
					bitmap.UnlockBits(bmpData);
			}
		}
		#endregion


		#endregion

		//PRIVATE METHODS
		#region private methods

		#region FindObjects()
		private static unsafe int[,] FindObjects(BitmapData bmpData)
		{
			int x;
			int y;
			int[,] array = new int[(int)Math.Ceiling((double)(((double)bmpData.Height) / 8.0)), (int)Math.Ceiling((double)(((double)bmpData.Width) / 8.0))];
			int id = 1;
			int stride = bmpData.Stride;
			int arrayW = array.GetLength(1);
			int arrayH = array.GetLength(0);
			bool[] line = new bool[arrayW];
			RasterProcessing.Pairs pairs = new RasterProcessing.Pairs();
			byte* pSource = (byte*)bmpData.Scan0.ToPointer();
			for (y = 0; y < arrayH; y++)
			{
				byte* pCurrent = pSource + ((y * 8) * stride);
				x = 0;
				while (x < arrayW)
				{
					line[x] = HasWhitePixels(pCurrent, stride, bmpData.Height, y * 8);
					pCurrent++;
					x++;
				}
				x = 0;
				while (x < arrayW)
				{
					if (line[x])
					{
						if ((y > 0) && (array[y - 1, x] != 0))
						{
							array[y, x] = array[y - 1, x];
							if (((x > 0) && (array[y, x - 1] != 0)) && (array[y, x] != array[y, x - 1]))
							{
								pairs.Add(array[y, x - 1], array[y, x]);
							}
						}
						else if (((x > 0) && (y > 0)) && (array[y - 1, x - 1] != 0))
						{
							array[y, x] = array[y - 1, x - 1];
							if ((array[y, x - 1] != 0) && (array[y, x] != array[y, x - 1]))
							{
								pairs.Add(array[y, x - 1], array[y, x]);
							}
							if ((((x < (arrayW - 1)) && (array[y - 1, x + 1] != 0)) && !line[x + 1]) && (array[y - 1, x + 1] != array[y - 1, x - 1]))
							{
								pairs.Add(array[y, x], array[y - 1, x + 1]);
							}
						}
						else if (((y > 0) && (x < (arrayW - 1))) && (array[y - 1, x + 1] != 0))
						{
							array[y, x] = array[y - 1, x + 1];
							if (((x > 0) && (array[y, x - 1] != 0)) && (array[y, x] != array[y, x - 1]))
							{
								pairs.Add(array[y, x - 1], array[y, x]);
							}
						}
						else if ((x > 0) && (array[y, x - 1] != 0))
						{
							array[y, x] = array[y, x - 1];
						}
						else
						{
							array[y, x] = id++;
						}
					}
					x++;
				}
			}

			pairs.Compact();
			SortedList<int, int> sortedList = pairs.GetSortedList();
			for (y = 0; y < arrayH; y++)
			{
				for (x = 0; x < arrayW; x++)
				{
					int value;
					if ((array[y, x] != 0) && sortedList.TryGetValue(array[y, x], out value))
					{
						array[y, x] = value;
					}
				}
			}
			return array;
		}
		#endregion

		#region GetRidOfBorders1bpp()
		private static unsafe void GetRidOfBorders1bpp(Bitmap bitmap)
		{
			BitmapData bmpData = null;
			try
			{
				int y;
				bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
				int stride = bmpData.Stride;
				ushort clipW = (ushort)Math.Ceiling((double)(((double)bitmap.Width) / 8.0));
				ushort clipH = (ushort)bitmap.Height;
				int[,] array = FindObjects(bmpData);
				int arrayW = array.GetLength(1);
				int arrayH = array.GetLength(0);
				List<int> usedIndexes = new List<int>();
				int x = 0;
				
				while (x < arrayW)
				{
					if (!((array[0, x] == 0) || usedIndexes.Contains(array[0, x])))
					{
						usedIndexes.Add(array[0, x]);
					}
					if (!((array[arrayH - 1, x] == 0) || usedIndexes.Contains(array[arrayH - 1, x])))
					{
						usedIndexes.Add(array[arrayH - 1, x]);
					}
					x++;
				}
				
				for (y = 0; y < arrayH; y++)
				{
					if (!((array[y, 0] == 0) || usedIndexes.Contains(array[y, 0])))
					{
						usedIndexes.Add(array[y, 0]);
					}
					if (!((array[y, arrayW - 1] == 0) || usedIndexes.Contains(array[y, arrayW - 1])))
					{
						usedIndexes.Add(array[y, arrayW - 1]);
					}
				}
				
				y = 0;
				while (y < arrayH)
				{
					x = 0;
					while (x < arrayW)
					{
						if (usedIndexes.Contains(array[y, x]))
						{
							array[y, x] = -1;
						}
						else
						{
							array[y, x] = 0;
						}
						x++;
					}
					y++;
				}
				
				byte* pSource = (byte*)bmpData.Scan0.ToPointer();
				for (x = 0; x < arrayW; x++)
				{
					for (y = 0; y < arrayH; y++)
					{
						if (array[y, x] == -1)
						{
							byte* pCurrent = (pSource + ((y * 8) * stride)) + x;
							MakeBlack(pCurrent, stride, clipH, y * 8);
						}
					}
				}
			}
			finally
			{
				if (bmpData != null)
					bitmap.UnlockBits(bmpData);
			}
		}
		#endregion

		#region HasWhitePixels()
		private static unsafe bool HasWhitePixels(byte* pCurrent, int stride, int clipH, int y)
		{
			for (int i = 0; (i < 8) && (i < (clipH - y)); i++)
			{
				if (pCurrent[i * stride] > 0)
				{
					return true;
				}
			}
			return false;
		}
		#endregion

		#region MakeBlack()
		private static unsafe void MakeBlack(byte* pCurrent, int stride, int clipH, int y)
		{
			for (int i = 0; (i < 8) && (i < (clipH - y)); i++)
			{
				pCurrent[i * stride] = 0;
			}
		}
		#endregion

		#region MergeBitmaps()
		private static Bitmap MergeBitmaps(Bitmap b1, Bitmap b2, Bitmap b3, int stop1, int stop2, int width)
		{
			Bitmap result = new Bitmap(width, b1.Height, b1.PixelFormat);
			BitmapData sourceData = null;
			BitmapData resultData = null;

#if DEBUG
			DateTime start = DateTime.Now;
#endif
			unsafe
			{
				byte* pSource;
				byte* pResult;
				byte* pSourceCur;
				byte* pResultCur;
				int x, y;

				try
				{
					sourceData = b1.LockBits(new Rectangle(0, 0, b1.Width, b1.Height), ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
					resultData = result.LockBits(new Rectangle(0, 0, sourceData.Width, b1.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					int sStride = sourceData.Stride;
					int rStride = resultData.Stride;

					int byteWidth = sourceData.Width / 8;
					int clipHeight = sourceData.Height;

					pSource = (byte*)sourceData.Scan0.ToPointer();
					pResult = (byte*)resultData.Scan0.ToPointer();

					for (y = 0; y < clipHeight; y++)
					{
						pSourceCur = pSource + (y * sStride);
						pResultCur = pResult + (y * rStride);

						for (x = 0; x < byteWidth; x++)
							*(pResultCur++) = *(pSourceCur++);
					}
				}
				finally
				{
					if (b1 != null && sourceData != null)
						b1.UnlockBits(sourceData);
					if (result != null && resultData != null)
						result.UnlockBits(resultData);
				}
				
				try
				{
					sourceData = b2.LockBits(Rectangle.FromLTRB(8, 0, b2.Width, b2.Height), ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
					resultData = result.LockBits(Rectangle.FromLTRB(stop1, 0, stop2, b2.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					int sStride = sourceData.Stride;
					int rStride = resultData.Stride;

					int byteWidth = (stop2 - stop1) / 8;
					int clipHeight = sourceData.Height;

					pSource = (byte*)sourceData.Scan0.ToPointer();
					pResult = (byte*)resultData.Scan0.ToPointer();

					for (y = 0; y < clipHeight; y++)
					{
						pSourceCur = pSource + (y * sStride);
						pResultCur = pResult + (y * rStride);

						for (x = 0; x < byteWidth; x++)
							*(pResultCur++) = *(pSourceCur++);
					}
				}
				finally
				{
					if (b2 != null && sourceData != null)
						b2.UnlockBits(sourceData);
					if (result != null && resultData != null)
						result.UnlockBits(resultData);
				}
				
				try
				{
					sourceData = b3.LockBits(Rectangle.FromLTRB(8, 0, b3.Width, b3.Height), ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
					resultData = result.LockBits(Rectangle.FromLTRB(stop2, 0, result.Width, b3.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					int sStride = sourceData.Stride;
					int rStride = resultData.Stride;

					int byteWidth = (int)Math.Ceiling(sourceData.Width / 8.0F);
					int clipHeight = sourceData.Height;

					pSource = (byte*)sourceData.Scan0.ToPointer();
					pResult = (byte*)resultData.Scan0.ToPointer();

					for (y = 0; y < clipHeight; y++)
					{
						pSourceCur = pSource + (y * sStride);
						pResultCur = pResult + (y * rStride);

						for (x = 0; x < byteWidth; x++)
							*(pResultCur++) = *(pSourceCur++);
					}
				}
				finally
				{
					if (b3 != null && sourceData != null)
						b3.UnlockBits(sourceData);
					if (result != null && resultData != null)
						result.UnlockBits(resultData);
				}
			}

#if DEBUG
			System.Reflection.MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
			Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "(): " + DateTime.Now.Subtract(start).ToString());
#endif

			return result;
		}
		#endregion

		#endregion

	}

}
