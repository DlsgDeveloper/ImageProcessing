using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.BigImages
{
	public class Merging
	{

		//	PUBLIC METHODS
		#region public methods

		#region MergeHorizontally()
		public static void MergeHorizontally(string source1, string source2, string result, ImageProcessing.FileFormat.IImageFormat imageFormat, int pixelsOverlap)
		{
			using (ItDecoder itDecoder1 = new ItDecoder(source1))
			{
				using (ItDecoder itDecoder2 = new ItDecoder(source2))
				{
					int width = itDecoder1.Width + itDecoder2.Width - pixelsOverlap;
					int height = Math.Min(itDecoder1.Height, itDecoder2.Height);

					using (ImageProcessing.BigImages.ItEncoder itEncoder = new ImageProcessing.BigImages.ItEncoder(result, imageFormat, itDecoder1.PixelsFormat,
								width, height, itDecoder1.DpiX, itDecoder1.DpiY))
					{
						itEncoder.SetPalette(itDecoder1);
						int stripHeightMax = Misc.GetStripHeightMax(itDecoder1) / 16 * 8;

						for (int stripY = 0; stripY < height; stripY = stripY + stripHeightMax)
						{
							int stripHeight = Math.Min(stripHeightMax, height - stripY);
							using (Bitmap bitmap1 = itDecoder1.GetClip(new Rectangle(0, stripY, itDecoder1.Width - pixelsOverlap, stripHeight)))
							{
								using (Bitmap bitmap2 = itDecoder2.GetClip(new Rectangle(0, stripY, itDecoder2.Width, stripHeight)))
								{
									using (Bitmap bitmap = ImageProcessing.Merging.MergeHorizontally(new List<Bitmap>{bitmap1, bitmap2}))
									{
										BitmapData sourceData = null;

										try
										{
											sourceData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

											unsafe
											{
												itEncoder.Write(stripHeight, sourceData.Stride, (byte*)sourceData.Scan0.ToPointer());
											}
										}
										finally
										{
											if (sourceData != null)
												bitmap.UnlockBits(sourceData);
										}
									}

									itDecoder2.ReleaseAllocatedMemory(bitmap2);
									itDecoder1.ReleaseAllocatedMemory(bitmap1);
								}
							}
						}
					}
				}
			}
		}
		#endregion

		#endregion

	}
}
