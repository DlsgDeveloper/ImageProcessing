using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;

namespace ImageProcessing.BigImages
{
	public partial class Binarization
	{

		//	PUBLIC METHODS
		#region public methods

		#region LoG()
		public void LoG(string file, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			LoG(file, destFile, imageFormat, Rectangle.Empty, 0);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="file"></param>
		/// <param name="destFile"></param>
		/// <param name="imageFormat"></param>
		/// <param name="clip"></param>
		/// <param name="brightness">[-1, 1] brightness delta. Negatibe makes image darker, positive lighter</param>
		public void LoG(string file, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat, Rectangle clip, double brightness)
		{
			using (Bitmap bitonalBitmap = LoG_ToBitmap(file, clip, brightness))
			{
				SaveBitonalBitmapToFile(bitonalBitmap, destFile, imageFormat);
			}
		}
		#endregion

		#region LoG_ToBitmap()
		public Bitmap LoG_ToBitmap(string file)
		{
			return LoG_ToBitmap(file, Rectangle.Empty, 0);
		}

		public Bitmap LoG_ToBitmap(string file, Rectangle clip, double brightness)
		{
			using (ImageProcessing.BigImages.ItDecoder itDecoder = new ItDecoder(file))
			{
				// fix clip if necessary
				if (clip.IsEmpty)
					clip = Rectangle.FromLTRB(0, 0, itDecoder.Width, itDecoder.Height);
				else
					clip.Intersect(Rectangle.FromLTRB(0, 0, itDecoder.Width, itDecoder.Height));

				try
				{
					switch (itDecoder.PixelFormat)
					{
						case PixelFormat.Format4bppIndexed:
						case PixelFormat.Format8bppIndexed:
						case PixelFormat.Format24bppRgb:
						case PixelFormat.Format32bppArgb:
						case PixelFormat.Format32bppRgb:
							return BinarizeInternal(itDecoder, clip, brightness);
						default:
							throw new IpException(ErrorCode.ErrorUnsupportedFormat);
					}
				}
				catch (Exception ex)
				{
					throw new Exception("Binarization, LoG_ToBitmap(): " + ex.Message);
				}
			}
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods
	
		#region BinarizeInternal()
		private Bitmap BinarizeInternal(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip, double brightness)
		{
			Bitmap result = null;

			try
			{
				int stripHeightMax = Misc.GetStripHeightMax(itDecoder);
				
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);

				for (int stripY = clip.Top; stripY < clip.Bottom; stripY += stripHeightMax)
				{
					Bitmap source = null;

					int stripH = Math.Min(stripHeightMax, clip.Bottom - stripY);
					
					try
					{
						int sourceTop = Math.Max(0, stripY - 24);
						int sourceBottom = Math.Min(stripH + (stripY - sourceTop) + 24, clip.Bottom);
						
						source = itDecoder.GetClip(new Rectangle(clip.X, sourceTop, clip.Width, sourceBottom));

						Binarize(source, sourceTop, result, stripY, stripH, (int)(brightness * 128));
					}
					finally
					{					
						itDecoder.ReleaseAllocatedMemory(source);
						
						if (source != null)
						{
							source.Dispose();
							source = null;
						}
					}

					FireProgressEvent((stripY + stripH) / (float)itDecoder.Height);
				}
				
				if (result != null)
					ImageProcessing.Misc.SetBitmapResolution(result, itDecoder.DpiX, itDecoder.DpiY);

				return result;
			}
			catch (Exception ex)
			{
				if (result != null)
				{
					result.Dispose();
					result = null;
				}

				throw ex;
			}
			finally
			{
			}
		}
		#endregion

		#region Binarize()
		private void Binarize(Bitmap source, int sourceTop, Bitmap bitonal, int bitonalTop, int bitonalHeight, int brightness)
		{
			Bitmap edge = null;
			BitmapData sourceData = null;
			BitmapData edgeData = null;
			BitmapData bitonalData = null;
			int threshold = Math.Max(10, Math.Min(250, 112 + brightness));

			try
			{
				int width = source.Width;
				edge = ImageProcessing.EdgeDetector.Get(source, Rectangle.Empty, EdgeDetector.Operator.MexicanHat7x7);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				edgeData = edge.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, edge.PixelFormat);
				bitonalData = bitonal.LockBits(new Rectangle(0, bitonalTop, bitonal.Width, bitonalHeight), ImageLockMode.WriteOnly, bitonal.PixelFormat);

				int strideS = sourceData.Stride;
				int strideE = edgeData.Stride;
				int strideB = bitonalData.Stride;
				int x, y;

				unsafe
				{
					try
					{
						byte* pSource = (byte*)sourceData.Scan0.ToPointer();
						byte* pEdge = (byte*)edgeData.Scan0.ToPointer();
						byte* pBitonal = (byte*)bitonalData.Scan0.ToPointer();
						int edgeHeight = edge.Height;

#if DEBUG
						DateTime start = DateTime.Now;
#endif
						for (y = 0; y < bitonalHeight; y++)
						{
							for (x = 0; x < width; x++)
							{
								if (pEdge[y * strideE + x] > threshold)
									pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
							}
						}

#if DEBUG
						Console.WriteLine("LoG Binorization: " + DateTime.Now.Subtract(start).ToString());
#endif
					}
					catch (Exception ex)
					{
						throw ex;
					}
				}
			}
			finally
			{
				if (bitonalData != null)
					bitonal.UnlockBits(bitonalData);
				if (edgeData != null)
					edge.UnlockBits(edgeData);
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (edge != null)
					edge.Dispose();
			}
		}
		#endregion

		#endregion
	}
}
