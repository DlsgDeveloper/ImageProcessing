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
	public class BlackBorderRemoval
	{



		//PUBLIC METHODS
		#region public methods

		#region RemoveBlackBorders()
		public static void RemoveBlackBorders(Bitmap bitmap)
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
					throw new Exception("BlackBorderRemoval, GetRidOfBorders(): " + ex.Message);
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
					line[x] = HasBlackPixels(pCurrent, stride, bmpData.Height, y * 8);
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
							MakeWhite(pCurrent, stride, clipH, y * 8);
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

		#region HasBlackPixels()
		private static unsafe bool HasBlackPixels(byte* pCurrent, int stride, int clipH, int y)
		{
			for (int i = 0; (i < 8) && (i < (clipH - y)); i++)
			{
				if (pCurrent[i * stride] < 0xFF)
				{
					return true;
				}
			}
			return false;
		}
		#endregion

		#region MakeWhite()
		private static unsafe void MakeWhite(byte* pCurrent, int stride, int clipH, int y)
		{
			for (int i = 0; (i < 8) && (i < (clipH - y)); i++)
			{
				pCurrent[i * stride] = 0xFF;
			}
		}
		#endregion

		#endregion

	}

}
