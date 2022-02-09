using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace TestApp
{
	class PixelsComparer
	{
		#region Go()
		public static void Go()
		{
			FileInfo sourceFile1 = new FileInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\PixelsComparer\01.png");
			FileInfo sourceFile2 = new FileInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\PixelsComparer\01a.png");
			FileInfo textFile = new FileInfo(@"C:\Users\Jirka.Stybnar.IA-CORP\testRun\PixelsComparer\results.txt");

			using (Bitmap b1 = new Bitmap(sourceFile1.FullName))
			{
				using (Bitmap b2 = new Bitmap(sourceFile2.FullName))
				{
					using (FileStream fileStream = textFile.OpenWrite())
					{
						StreamWriter streamWriter = new StreamWriter(fileStream);

						byte[] array = GetArray(b1, b2);
						for (int i = 0; i < 256; i++)
						{
							//streamWriter.WriteLine($"{i:000}\t{array[i]:000}");
							streamWriter.WriteLine($"{array[i]:000}");
						}

						/*
						PixelChanges pixelChanges = GetPixelChanges(b1, b2);
						for (int i = 0; i < 256; i++)
						{
							List<PixelChange> pixelChangeList = pixelChanges.Where(x => x.OldValue == i).ToList();

							pixelChangeList.Sort((x1, x2) => x1.NewValue.CompareTo(x2.NewValue));

							for (int j = 0; j < pixelChangeList.Count; j++)
								streamWriter.WriteLine($"{i:000}\t{pixelChangeList[j].NewValue:000}\t{pixelChangeList[j].Count:000}");
						}
						*/

						streamWriter.Flush();
						fileStream.Flush();
					}
				}
			}
		}
		#endregion


		// PRIVATE METHODS
		#region private methods

		#region GetArray()
		private static byte[] GetArray(Bitmap bitmap1, Bitmap bitmap2)
		{
			byte[] array = new byte[256];
			BitmapData bitmapData1 = null;
			BitmapData bitmapData2 = null;

			try
			{
				int width = bitmap1.Width;
				int height = bitmap1.Height;

				bitmapData1 = bitmap1.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap1.PixelFormat);
				bitmapData2 = bitmap2.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap2.PixelFormat);

				int stride1= bitmapData1.Stride;
				int stride2 = bitmapData2.Stride;
				int jump = bitmap1.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;

				unsafe
				{
					byte* pSource1 = (byte*)bitmapData1.Scan0.ToPointer();
					byte* pSource2 = (byte*)bitmapData2.Scan0.ToPointer();

					int x, y;
					byte r, g, b;
					byte r2, g2, b2;

					for (y = 0; y < height; y++)
					{

						for (x = 0; x < width; x++)
						{
							b = pSource1[y * stride1 + x * jump];
							g = pSource1[y * stride1 + x * jump + 1];
							r = pSource1[y * stride1 + x * jump + 2];

							b2 = pSource2[y * stride1 + x * jump];
							g2 = pSource2[y * stride1 + x * jump + 1];
							r2 = pSource2[y * stride1 + x * jump + 2];

							array[b] = b2;
							array[g] = g2;
							array[r] = r2;
						}
					}
				}

				return array;
			}
			finally
			{
				if (bitmapData1 != null)
					bitmap1.UnlockBits(bitmapData1);
				if (bitmapData2 != null)
					bitmap2.UnlockBits(bitmapData2);
			}
		}
		#endregion

		#region class PixelChange
		class PixelChange
		{
			public byte OldValue { get; }
			public byte NewValue { get; }
			public int Count { get; set; } = 1;

			public PixelChange(byte oldValue, byte newValue)
			{
				this.OldValue = oldValue;
				this.NewValue = newValue;
			}
		}
		#endregion

		#region class PixelChanges
		class PixelChanges : List<PixelChange>
		{
			public PixelChange Find(byte oldValue, byte newValue)
			{
				PixelChange pixelChange = this.Where(x => x.OldValue == oldValue && x.NewValue == newValue).FirstOrDefault();

				return pixelChange;
			}
		}
		#endregion

		#region GetPixelChanges()
		private static PixelChanges GetPixelChanges(Bitmap bitmap1, Bitmap bitmap2)
		{
			PixelChanges pixelChanges = new PixelChanges();
			BitmapData bitmapData1 = null;
			BitmapData bitmapData2 = null;

			try
			{
				int width = bitmap1.Width;
				int height = bitmap1.Height;

				bitmapData1 = bitmap1.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap1.PixelFormat);
				bitmapData2 = bitmap2.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap2.PixelFormat);

				int stride1 = bitmapData1.Stride;
				int stride2 = bitmapData2.Stride;
				int jump = bitmap1.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;

				unsafe
				{
					byte* pSource1 = (byte*)bitmapData1.Scan0.ToPointer();
					byte* pSource2 = (byte*)bitmapData2.Scan0.ToPointer();

					int x, y;
					byte r, g, b;
					byte r2, g2, b2;
					PixelChange pixelChange;

					for (y = 0; y < height; y++)
					{

						for (x = 0; x < width; x++)
						{
							b = pSource1[y * stride1 + x * jump];
							g = pSource1[y * stride1 + x * jump + 1];
							r = pSource1[y * stride1 + x * jump + 2];

							b2 = pSource2[y * stride1 + x * jump];
							g2 = pSource2[y * stride1 + x * jump + 1];
							r2 = pSource2[y * stride1 + x * jump + 2];

							pixelChange = pixelChanges.Find(b, b2);
							if (pixelChange != null)
								pixelChange.Count++;
							else
								pixelChanges.Add(new PixelChange(b, b2));

							pixelChange = pixelChanges.Find(g, g2);
							if (pixelChange != null)
								pixelChange.Count++;
							else
								pixelChanges.Add(new PixelChange(g, g2));

							pixelChange = pixelChanges.Find(b, b2);
							if (pixelChange != null)
								pixelChange.Count++;
							else
								pixelChanges.Add(new PixelChange(b, b2));
						}
					}
				}

				return pixelChanges;
			}
			finally
			{
				if (bitmapData1 != null)
					bitmap1.UnlockBits(bitmapData1);
				if (bitmapData2 != null)
					bitmap2.UnlockBits(bitmapData2);
			}
		}
		#endregion
		
		#endregion

	}
}
