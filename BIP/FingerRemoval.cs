using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections ;
using ImageProcessing.PageObjects;
using System.Collections.Generic;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for FingerRemoval.
	/// max finger size is 2" width by 3.5" height 
	/// </summary>
	public class FingerRemoval
	{		
		//PUBLIC METHODS
		#region public methods

		#region FindFingers()
		public static Fingers FindFingers(Bitmap bitmap, ItPage page, Paging paging, int minDelta, float percWhite, out byte confidence)
		{
			if (page.Skew == 0)
				return FindFingersNotSkewed(bitmap, page, paging, minDelta, percWhite, out confidence);
			else
				return FindFingersSkewed(bitmap, page, paging, minDelta, percWhite, out confidence);
		}

		public static void FindFingers(Bitmap raster, ItPage page)
		{
			int[,] blocksMap = GetBlocksMap(raster, page);

			page.Fingers.Clear();
			page.Fingers.AddRange(GetFingers(page, blocksMap));
		}
		#endregion
	
		#region EraseFingers()
		public static void EraseFingers(Bitmap bitmap, Fingers fingers)
		{
			foreach (Finger finger in fingers)
			{
				if (finger.Page.Skew == 0)
					EraseFinger(bitmap, finger.Page.Clip.RectangleNotSkewed, finger.RectangleNotSkewed);
				else
					EraseFinger(bitmap, finger);
			}
		}
		#endregion

		#region EraseFinger()
		public static void EraseFinger(Bitmap bitmap, Rectangle imageClip, Rectangle fingerClip)
		{
			if (imageClip.IsEmpty)
				imageClip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			else
				imageClip.Intersect(new Rectangle(Point.Empty, bitmap.Size));
			
			fingerClip.Intersect(imageClip);

			
			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format24bppRgb :
					Erase24bpp(bitmap, imageClip, fingerClip);
					break ;
				case PixelFormat.Format8bppIndexed :
					if (Misc.IsGrayscale(bitmap))
						Erase8bpp(bitmap, imageClip, fingerClip);
					else
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
					break;
				case PixelFormat.Format1bppIndexed :
					Erase1bpp(bitmap, fingerClip);
					break ;
				default :
					throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
			}
		}

		public static void EraseFinger(Bitmap bitmap, Finger finger)
		{
			finger.RectangleNotSkewed.Intersect(new Rectangle(0, 0, bitmap.Width, bitmap.Height));

			switch (bitmap.PixelFormat)
			{
				case PixelFormat.Format24bppRgb:
					Erase24bpp(bitmap, finger);
					break;
				case PixelFormat.Format8bppIndexed:
					if (Misc.IsGrayscale(bitmap))
						Erase8bpp(bitmap, finger);
					else
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
					break;
				case PixelFormat.Format1bppIndexed:
					Erase1bpp(bitmap, finger);
					break;
				default:
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}
		}
		#endregion		
		
		#endregion

	
		//	PRIVATE METHODS
		#region private methods

		#region FindFingersNotSkewed()
		private static Fingers FindFingersNotSkewed(Bitmap bitmap, ItPage page, Paging paging, int minDelta,
			float percWhite, out byte confidence)
		{
			Rectangle clip = page.Clip.RectangleNotSkewed;
			clip = Rectangle.FromLTRB(Math.Max(0, clip.X), Math.Max(0, clip.Y), Math.Min(clip.Right, bitmap.Width), Math.Min(clip.Bottom, bitmap.Height)); 

			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

			Bitmap bmpEdgeDetect = null;
			BitmapData bmpData = null;
			Fingers fingers = new Fingers();
			int inch = Convert.ToInt32(bitmap.HorizontalResolution);
			int maxFingerWidth = inch * 2;
			int maxFingerHeight = (int)(inch * 3F);

			confidence = 0;

			if ((paging & Paging.Left) != 0)
			{
				try
				{
					Rectangle fingerRect = Rectangle.FromLTRB(clip.X, clip.Y, (int)Math.Min(clip.Right, clip.X + 3 * inch), clip.Bottom);
					bmpEdgeDetect = ImageProcessing.ImagePreprocessing.Go(bitmap, fingerRect, 0, minDelta, true, true);

					bmpData = bmpEdgeDetect.LockBits(new Rectangle(0, 0, bmpEdgeDetect.Width, bmpEdgeDetect.Height), ImageLockMode.ReadOnly, bmpEdgeDetect.PixelFormat);

					Rectangle leftRect = FindFingerOnLeft(bmpData, percWhite, maxFingerWidth, maxFingerHeight, inch / 4);

					if (leftRect.IsEmpty == false)
					{
						leftRect.Offset(clip.Location);
						leftRect.Intersect(page.ClipRect);

						if (leftRect.IsEmpty == false)
						{
							Finger finger = new Finger(page, leftRect);

							if (finger != null)
								fingers.Add(finger);
						}
					}
				}
				finally
				{
					if (bmpEdgeDetect != null && bmpData != null)
					{
						bmpEdgeDetect.UnlockBits(bmpData);
						bmpData = null;
					}
				}
			}
			if ((paging & Paging.Right) != 0)
			{
				try
				{
					int			fingerRectX = (int)Math.Max(clip.X, clip.Right - 3 * inch);
					Rectangle	fingerRect = Rectangle.FromLTRB(fingerRectX, clip.Y, clip.Right, clip.Bottom);

					bmpEdgeDetect = ImageProcessing.ImagePreprocessing.Go(bitmap, fingerRect, 0, minDelta, true, true);
					bmpData = bmpEdgeDetect.LockBits(new Rectangle(0, 0, bmpEdgeDetect.Width, bmpEdgeDetect.Height), ImageLockMode.ReadOnly, bmpEdgeDetect.PixelFormat);

					Rectangle rightRect = FindFingerOnRight(bmpData, percWhite, maxFingerWidth, maxFingerHeight, inch / 4);

					if (rightRect.IsEmpty == false)
					{
						rightRect.Offset(fingerRectX, clip.Y);
						rightRect.Intersect(page.ClipRect);

						if (rightRect.IsEmpty == false)
						{
							Finger finger = new Finger(page, rightRect);

							if (finger != null)
								fingers.Add(finger);
						}
					}
				}
				finally
				{
					if (bmpEdgeDetect != null && bmpData != null)
					{
						bmpEdgeDetect.UnlockBits(bmpData);
						bmpData = null;
					}
				}
			}

			return fingers;
		}
		#endregion

		#region FindFingersSkewed()
		private static Fingers FindFingersSkewed(Bitmap bitmap, ItPage page, Paging paging, int minDelta, float percWhite, out byte confidence)
		{
			Rectangle clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			Bitmap		bmpEdgeDetect = null;
			BitmapData	bmpData = null;
			Fingers		fingers = new Fingers();
			int			inch = Convert.ToInt32(bitmap.HorizontalResolution);
			int			maxFingerWidth = inch * 2;
			int			maxFingerHeight = (int)(inch * 3F);

			confidence = 0;
			bmpEdgeDetect = ImageProcessing.ImagePreprocessing.Go(bitmap, clip, 0, minDelta, true, false);
			bmpData = bmpEdgeDetect.LockBits(new Rectangle(0, 0, bmpEdgeDetect.Width, bmpEdgeDetect.Height), ImageLockMode.ReadOnly, bmpEdgeDetect.PixelFormat);

			if ((paging & Paging.Left) != 0)
			{
				Rectangle leftRect = FindFingerOnLeft(bmpData, page, percWhite, maxFingerWidth, maxFingerHeight, inch / 4);

				if (leftRect.IsEmpty == false)
				{
					leftRect.Offset(clip.Location);
					leftRect.Intersect(page.ClipRect);

					if (leftRect.IsEmpty == false)
					{
						Finger finger = new Finger(page, leftRect);

						if (finger != null)
							fingers.Add(finger);
					}
				}
			}

			if ((paging & Paging.Right) != 0)
			{
				Rectangle rightRect = FindFingerOnRight(bmpData, page, percWhite, maxFingerWidth, maxFingerHeight, inch / 4);

				if (rightRect.IsEmpty == false)
				{
					rightRect.Offset(clip.Location);
						rightRect.Intersect(page.ClipRect);

						if (rightRect.IsEmpty == false)
						{
							Finger finger = new Finger(page, rightRect);

							if (finger != null)
								fingers.Add(finger);
						}
				}
			}

			return fingers;
		}
		#endregion

		#region FindFingerOnLeft()
		private static Rectangle FindFingerOnLeft(BitmapData bmpData, float percWhite, int maxFinferWidth, int maxFingerHeight, int searchWidth)
		{			
			unsafe
			{
				byte*		scan0 = (byte*) bmpData.Scan0.ToPointer();
				int			yTop = 0, yBottom = 0;
				int			xRight;
				ArrayList	fingerList = new ArrayList();
				
				while(yTop < bmpData.Height && yBottom < bmpData.Height)
				{
					yTop = yBottom;
					yTop = RasterProcessing.FindContentFromTop(scan0, bmpData.Stride, 0, yTop, bmpData.Height, searchWidth, 9, percWhite);

					if(yTop < int.MaxValue)
					{
						yBottom = RasterProcessing.FindBackgroundFromTop(scan0, bmpData.Stride, 0, yTop, bmpData.Height,
							searchWidth, 9, percWhite);

						if( (yBottom < int.MaxValue) && (yBottom - yTop < maxFingerHeight) && (yBottom - yTop > searchWidth) )
						{
							xRight = RasterProcessing.FindBackgroundFromLeft(scan0, bmpData.Stride, yTop,
								0, maxFinferWidth + 9, 9, yBottom - yTop, percWhite);

							if(xRight < maxFinferWidth)
							{
								yTop = (yTop - searchWidth > 0) ? yTop - searchWidth : 0;
								yBottom = (yBottom + searchWidth < bmpData.Height) ? yBottom + searchWidth : bmpData.Height;
								xRight = xRight + searchWidth;
								fingerList.Add(Rectangle.FromLTRB(0, yTop, xRight, yBottom));
							}
						}
					}
				}
			
				return GetBiggestRectangle(fingerList) ;
			}
		}
		#endregion

		#region FindFingerOnRight()
		private static Rectangle FindFingerOnRight(BitmapData bmpData, float percWhite, int maxFinferWidth,  int maxFingerHeight, int searchWidth)
		{
			unsafe
			{
				byte*		scan0 = (byte*) bmpData.Scan0.ToPointer();
				int			yTop = 0, yBottom = 0;
				int			xLeft, xRight = bmpData.Width;
				ArrayList	fingerList = new ArrayList();
				
				while(yTop < bmpData.Height && yBottom < bmpData.Height)
				{
					yTop = yBottom;

					yTop = RasterProcessing.FindContentFromTop(scan0, bmpData.Stride, xRight - searchWidth,
						yTop, bmpData.Height, searchWidth, 9, percWhite);

					if(yTop < int.MaxValue)
					{
						yBottom = RasterProcessing.FindBackgroundFromTop(scan0, bmpData.Stride, 
							xRight - searchWidth, yTop, bmpData.Height, searchWidth, 9, percWhite);

						if( (yBottom < int.MaxValue) && (yBottom - yTop < maxFingerHeight) && (yBottom - yTop > searchWidth) )
						{
							xLeft = RasterProcessing.FindBackgroundFromRight(scan0, bmpData.Stride, yTop,
								xRight - maxFinferWidth, xRight, 9, yBottom - yTop, percWhite);

							if(xRight - xLeft < maxFinferWidth)
							{
								yTop = (yTop - searchWidth > 0) ? yTop - searchWidth : 0;
								yBottom = (yBottom + searchWidth < bmpData.Height) ? yBottom + searchWidth : bmpData.Height;
								xLeft = xLeft - searchWidth;
								fingerList.Add(Rectangle.FromLTRB(xLeft, yTop, xRight, yBottom));

							}
						}
					}
				}
			
				return GetBiggestRectangle(fingerList) ;
			}
		}
		#endregion
				
		#region FindFingerOnLeft()
		private static Rectangle FindFingerOnLeft(BitmapData bmpData, ItPage page, float percWhite, int maxFinferWidth,
			int maxFingerHeight, int searchWidth)
		{
			unsafe
			{
				byte*		scan0 = (byte*)bmpData.Scan0.ToPointer();
				int			xRight;
				ArrayList	fingerList = new ArrayList();
				double		xJump = (page.Clip.PointLL.X - page.Clip.PointUL.X) / (double) (page.Clip.PointLL.Y - page.Clip.PointUL.Y);
				int			xTop = Math.Max(0, Math.Min(bmpData.Width, page.Clip.PointUL.X));
				int			yTop = Math.Max(0, Math.Min(bmpData.Height, page.Clip.PointUL.Y));
				int			xBottom = xTop;
				int			yBottom = yTop;
				int			xMax = bmpData.Width;
				int			yMax = Math.Min(page.Clip.PointLL.Y, bmpData.Height);

				while (yTop < bmpData.Height && yBottom < bmpData.Height)
				{
					yTop = yBottom;
					yTop = FindFromTop(scan0, bmpData.Stride, new Point(xTop, yTop), xMax, yMax, xJump, searchWidth, 9, percWhite, true);

					if (yTop < int.MaxValue)
					{
						xTop = GetX(page.Clip.PointUL, xJump, yTop, bmpData.Width);

						yBottom = FindFromTop(scan0, bmpData.Stride, new Point(xTop, yTop), xMax, yMax, xJump, searchWidth, 9, percWhite, false);

						if ((yBottom < int.MaxValue) && (yBottom - yTop < maxFingerHeight) && (yBottom - yTop > searchWidth))
						{
							xBottom = GetX(page.Clip.PointUL, xJump, yBottom, bmpData.Width);

							xRight = RasterProcessing.FindBackgroundFromLeft(scan0, bmpData.Stride, yTop,
								0, maxFinferWidth + 9, 9, yBottom - yTop, percWhite);

							if (xRight - xBottom < maxFinferWidth)
							{
								Point pUL = page.Clip.TransferSkewedToUnskewedPoint(new Point(xTop, yTop));// Rotation.RotatePoint(new Point(xTop, yTop), page.Clip.Center, -page.Skew);
								Point pLR = page.Clip.TransferSkewedToUnskewedPoint(new Point(xRight, yBottom));// Rotation.RotatePoint(new Point(xRight, yBottom), page.Clip.Center, -page.Skew);

								//inflate finger rectangle
								Rectangle fingerRect = Rectangle.FromLTRB(pUL.X, pUL.Y, pLR.X, pLR.Y);
								fingerRect.Inflate(searchWidth, searchWidth);
								fingerRect.Intersect(page.Clip.RectangleNotSkewed);

								fingerList.Add(fingerRect);
							}
						}
					}
				}

				return GetBiggestRectangle(fingerList);
			}
		}
		#endregion

		#region FindFingerOnRight()
		private static Rectangle FindFingerOnRight(BitmapData bmpData, ItPage page, float percWhite, int maxFinferWidth,
			int maxFingerHeight, int searchWidth)
		{
			unsafe
			{
				byte* scan0 = (byte*)bmpData.Scan0.ToPointer();
				int xLeft;
				ArrayList fingerList = new ArrayList();
				double xJump = (page.Clip.PointLL.X - page.Clip.PointUL.X) / (double)(page.Clip.PointLL.Y - page.Clip.PointUL.Y);
				int xTop = Math.Max(0, Math.Min(bmpData.Width, page.Clip.PointUR.X));
				int yTop = Math.Max(0, Math.Min(bmpData.Height,page.Clip.PointUR.Y));
				int xBottom = xTop;
				int yBottom = yTop;
				int xMax = bmpData.Width;
				int yMax = Math.Min(page.Clip.PointLR.Y, bmpData.Height);

				while (yTop < bmpData.Height && yBottom < bmpData.Height)
				{
					yTop = yBottom;
					yTop = FindFromTop(scan0, bmpData.Stride, new Point(xTop - searchWidth, yTop), xMax, yMax, xJump, searchWidth, 9, percWhite, true);

					if (yTop < int.MaxValue)
					{
						xTop = GetX(page.Clip.PointUR, xJump, yTop, bmpData.Width);

						yBottom = FindFromTop(scan0, bmpData.Stride, new Point(xTop - searchWidth, yTop), xMax, yMax, xJump, searchWidth, 9, percWhite, false);

						if ((yBottom < int.MaxValue) && (yBottom - yTop < maxFingerHeight) && (yBottom - yTop > searchWidth))
						{
							xBottom = GetX(page.Clip.PointUR, xJump, yBottom, bmpData.Width);

							xLeft = RasterProcessing.FindBackgroundFromRight(scan0, bmpData.Stride, yTop,
								xTop - maxFinferWidth, xTop, 9, yBottom - yTop, percWhite);

							if (xBottom - xLeft < maxFinferWidth)
							{
								Point pUR = page.Clip.TransferSkewedToUnskewedPoint(new Point(xTop, yTop));// Rotation.RotatePoint(new Point(xTop, yTop), page.Clip.Center, -page.Skew);
								Point pLL = page.Clip.TransferSkewedToUnskewedPoint(new Point(xLeft, yBottom));// Rotation.RotatePoint(new Point(xLeft, yBottom), page.Clip.Center, -page.Skew);

								//inflate finger rectangle
								Rectangle fingerRect = Rectangle.FromLTRB(pLL.X, pUR.Y, pUR.X, pLL.Y);
								fingerRect.Inflate(searchWidth, searchWidth);
								fingerRect.Intersect(page.Clip.RectangleNotSkewed);

								fingerList.Add(fingerRect);
							}
						}
					}
				}

				return GetBiggestRectangle(fingerList);
			}
		}
		#endregion

		#region Erase24bpp()
		private static void Erase24bpp(Bitmap bitmap, Rectangle imageClip, Rectangle fingerClip)
		{
			BitmapData bmpData = null;

			try
			{
				imageClip.Intersect(new Rectangle(Point.Empty, bitmap.Size));
				fingerClip.Intersect(imageClip);
				bmpData = bitmap.LockBits(fingerClip, ImageLockMode.ReadWrite, bitmap.PixelFormat);
				int stride = bmpData.Stride;
				int heightConstant = Math.Min(fingerClip.Height / 2, 15);

				if (heightConstant == 0)
					throw new IpException(ErrorCode.FingerRegionSizeIsZero);

				unsafe
				{
					byte*	pOrig = (byte*)bmpData.Scan0.ToPointer();
					byte*	pCurrent;
					byte*	pBckgT;
					byte*	pBckgB;
					byte*	pBckgL;
					byte*	pBckgR;
					int		width = fingerClip.Width;
					int		height = fingerClip.Height;

					//top + bottom of clip has background color
					if (fingerClip.Y > imageClip.Y && fingerClip.Bottom < imageClip.Bottom - 1)
					{
						for (int y = 0; y < height; y++)
						{
							pCurrent = pOrig + y * stride;
							pBckgT = pOrig + (y % heightConstant) * stride;
							pBckgB = pOrig + (height - (y % heightConstant)) * stride;

							for (int x = 0; x < width; x++)
							{
								*(pCurrent++) = (byte)((*(pBckgT++) * (height - y) / height) + (*(pBckgB++) * y / height));
								*(pCurrent++) = (byte)((*(pBckgT++) * (height - y) / height) + (*(pBckgB++) * y / height));
								*(pCurrent++) = (byte)((*(pBckgT++) * (height - y) / height) + (*(pBckgB++) * y / height));
							}
						}
					}
					//top + bottom of clip has NOT background color
					else if (fingerClip.Y <= imageClip.Y && fingerClip.Bottom >= imageClip.Bottom)
					{
						for (int y = 0; y < height; y++)
						{
							pCurrent = pOrig + y * stride;
							pBckgL = (fingerClip.X > imageClip.X) ? pOrig + y * stride : pOrig + y * stride + width * 3;
							pBckgR = (fingerClip.Right < imageClip.Right) ? pOrig + y * stride + width * 3 : pBckgL;

							for (int x = 0; x < width; x++)
							{
								*(pCurrent++) = (byte)( (pBckgL[0] * (width - x) / width) + (pBckgR[0] * x / width) );
								*(pCurrent++) = (byte)( (pBckgL[1] * (width - x) / width) + (pBckgR[1] * x / width) );
								*(pCurrent++) = (byte)( (pBckgL[2] * (width - x) / width) + (pBckgR[2] * x / width) );
							}
						}
					}
					//top or bottom of clip has NOT background color
					else
					{
						for (int y = 0; y < height; y++)
						{
							pCurrent = pOrig + y * stride;
							pBckgL = (fingerClip.X > imageClip.X) ? pOrig + y * stride : pOrig + y * stride + width * 3;
							pBckgR = (fingerClip.Right < imageClip.Right) ? pOrig + y * stride + width * 3 : pBckgL;
							pBckgT = (fingerClip.Y > imageClip.Y) ? pOrig + (y % heightConstant) * stride : pOrig + (height - (y % heightConstant)) * stride;

							for (int x = 0; x < width; x++)
							{
								*(pCurrent++) = (byte)(((*(pBckgT++) * 3) + (*pBckgL * (width - x) / width) + (*pBckgR * x / width)) >> 2);
								*(pCurrent++) = (byte)(((*(pBckgT++) * 3) + (pBckgL[1] * (width - x) / width) + (pBckgR[1] * x / width)) >> 2);
								*(pCurrent++) = (byte)(((*(pBckgT++) * 3) + (pBckgL[2] * (width - x) / width) + (pBckgR[2] * x / width)) >> 2);
							}
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

		#region Erase8bpp()
		private static void Erase8bpp(Bitmap bitmap, Rectangle imageClip, Rectangle fingerClip)
		{
			BitmapData bmpData = null;

			try
			{
				imageClip.Intersect(new Rectangle(Point.Empty, bitmap.Size));
				fingerClip.Intersect(imageClip);
				bmpData = bitmap.LockBits(fingerClip, ImageLockMode.ReadWrite, bitmap.PixelFormat);
				int stride = bmpData.Stride;
				int heightConstant = Math.Min(fingerClip.Height / 2, 15);

				if (heightConstant == 0)
					throw new IpException(ErrorCode.FingerRegionSizeIsZero);

				unsafe
				{
					byte* pOrig = (byte*)bmpData.Scan0.ToPointer();
					byte* pCurrent;
					byte* pBckgT;
					byte* pBckgB;
					byte* pBckgL;
					byte* pBckgR;
					int width = fingerClip.Width;
					int height = fingerClip.Height;

					Color[] palette = bitmap.Palette.Entries;
					byte[] paletteInv = new byte[256];

					for (int i = 0; i < 256; i++)
						paletteInv[palette[i].R] = (byte)i;

					//top + bottom of clip has background color
					if (fingerClip.Y > imageClip.Y && fingerClip.Bottom < imageClip.Bottom - 1)
					{
						for (int y = 0; y < height; y++)
						{
							pCurrent = pOrig + y * stride;
							pBckgT = pOrig + (y % heightConstant) * stride;
							pBckgB = pOrig + (height - (y % heightConstant)) * stride;

							for (int x = 0; x < width; x++)
								*(pCurrent++) = paletteInv[(palette[*(pBckgT++)].R * (height - y) / height) + (palette[*(pBckgB++)].R * y / height)];
						}
					}
					//top + bottom of clip has NOT background color
					else if (fingerClip.Y <= imageClip.Y && fingerClip.Bottom >= imageClip.Bottom)
					{
						for (int y = 0; y < height; y++)
						{
							pCurrent = pOrig + y * stride;
							pBckgL = (fingerClip.X > imageClip.X) ? pOrig + y * stride : pOrig + y * stride + width;
							pBckgR = (fingerClip.Right < imageClip.Right) ? pOrig + y * stride + width : pBckgL;

							for (int x = 0; x < width; x++)
								*(pCurrent++) = paletteInv[(palette[*pBckgL].R * (width - x) / width) + (palette[*pBckgR].R * x / width)];
						}
					}
					//top or bottom of clip has NOT background color
					else
					{
						for (int y = 0; y < height; y++)
						{
							pCurrent = pOrig + y * stride;
							pBckgL = (fingerClip.X > imageClip.X) ? pOrig + (y * stride) : pOrig + (y * stride) + width;
							pBckgR = (fingerClip.Right < imageClip.Right) ? pOrig + (y * stride) + width : pBckgL;
							pBckgT = (fingerClip.Y > imageClip.Y) ? pOrig : pOrig + (height * stride);

							for (int x = 0; x < width; x++)
								*(pCurrent++) = paletteInv[((palette[*(pBckgT++)].R * 3) + (palette[*pBckgL].R * (width - x) / width) + (palette[*pBckgR].R * x / width)) >> 2];
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

		#region Erase1bpp()
		private static void Erase1bpp(Bitmap bitmap, Rectangle clip)
		{
			BitmapData bmpData = null;

			try
			{
				clip.Intersect(new Rectangle(Point.Empty, bitmap.Size));
				bmpData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat);
				byte background = 255;
				int width = clip.Width;

				if (bitmap.Palette != null && bitmap.Palette.Entries != null && bitmap.Palette.Entries.Length > 0 && bitmap.Palette.Entries[0].R == 255)
					background = 0;
				
				int stride = bmpData.Stride;

				unsafe
				{
					byte* pOrig = (byte*)bmpData.Scan0.ToPointer();

					for (int y = 0; y < bmpData.Height; y++)
					{
						for (int x = 0; x < width / 8; x++)
							pOrig[y * stride + x] = background;

						for (int x = width / 8 * 8; x < width; x++)
							pOrig[y * stride + x / 8] |= (byte)(0x80 >> (x & 0x07));
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

		#region Erase24bpp()
		private static void Erase24bpp(Bitmap bitmap, Finger finger)
		{
			BitmapData bmpData = null;
			int heightConstant = 15;

			try
			{				
				bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
				int stride = bmpData.Stride;

				int width = finger.PointUR.X - finger.PointUL.X;
				int height = finger.PointLL.Y - finger.PointUL.Y;
				int ulCornerX = finger.PointUL.X;
				int ulCornerY = finger.PointUL.Y;

				byte[,] stripT = new byte[heightConstant, width * 3];
				byte[,] stripB = new byte[heightConstant, width * 3];

				int sourceX;
				double sourceY;
				int sourceYint;
				double xJump = (finger.PointLL.X - ulCornerX) / (double)height;

				unsafe
				{
					byte* pOrig = (byte*)bmpData.Scan0.ToPointer();

					for (int y = ulCornerY; y < ulCornerY + heightConstant; y++)
						for (int x = ulCornerX; x < ulCornerX + width; x++)
						{
							if (x >= 0 && x < bmpData.Width && y >= 0 && y < bmpData.Height)
							{
								stripT[y - ulCornerY, (x - ulCornerX) * 3] = *(pOrig + y * stride + x * 3);
								stripT[y - ulCornerY, (x - ulCornerX) * 3 + 1] = *(pOrig + y * stride + x * 3 + 1);
								stripT[y - ulCornerY, (x - ulCornerX) * 3 + 2] = *(pOrig + y * stride + x * 3 + 2);
							}
						}

					for (int y = finger.PointLL.Y - heightConstant; y < finger.PointLL.Y; y++)
						for (int x = finger.PointLL.X; x < finger.PointLL.X + width; x++)
						{
							if (x >= 0 && x < bmpData.Width && y >= 0 && y < bmpData.Height)
							{
								stripB[y - finger.PointLL.Y + heightConstant, (x - finger.PointLL.X) * 3] = *(pOrig + y * stride + x * 3);
								stripB[y - finger.PointLL.Y + heightConstant, (x - finger.PointLL.X) * 3 + 1] = *(pOrig + y * stride + x * 3 + 1);
								stripB[y - finger.PointLL.Y + heightConstant, (x - finger.PointLL.X) * 3 + 2] = *(pOrig + y * stride + x * 3 + 2);
							}
						}

					for (int y = 0; y < height; y++)
					{
						sourceX = Convert.ToInt32(ulCornerX + y * xJump);
						sourceY = ulCornerY + y;
						sourceYint = Convert.ToInt32(sourceY);

						int pBckgT = (y % heightConstant);
						int pBckgB = (height - y) % heightConstant;

						for (int x = 0; x < width; x++)
						{
							if (x >= 0 && x < bmpData.Width && sourceYint >= 0 && sourceYint < bmpData.Height)
							{
								pOrig[sourceYint * stride + (x + sourceX) * 3] = (byte) ((stripT[pBckgT, x*3] * (height - y) / height) + (stripB[pBckgB, x*3] * y / height));
								pOrig[sourceYint * stride + (x + sourceX) * 3+1] = (byte) ((stripT[pBckgT, x * 3+1] * (height - y) / height) + (stripB[pBckgB, x * 3+1] * y / height));
								pOrig[sourceYint * stride + (x + sourceX) * 3+2] = (byte) ((stripT[pBckgT, x * 3+2] * (height - y) / height) + (stripB[pBckgB, x * 3+2] * y / height));
							}

							if (x >= 0 && x < bmpData.Width && sourceYint + 1 >= 0 && sourceYint + 1 < bmpData.Height && (sourceY - sourceYint > 0.000001))
							{
								pOrig[(sourceYint + 1) * stride + (x + sourceX) * 3] = (byte) ((stripT[pBckgT, x * 3] * (height - y) / height) + (stripB[pBckgB, x * 3] * y / height));
								pOrig[(sourceYint + 1) * stride + (x + sourceX) * 3 + 1] = (byte) ((stripT[pBckgT, x * 3 + 1] * (height - y) / height) + (stripB[pBckgB, x * 3 + 1] * y / height));
								pOrig[(sourceYint + 1) * stride + (x + sourceX) * 3 + 2] = (byte)((stripT[pBckgT, x * 3 + 2] * (height - y) / height) + (stripB[pBckgB, x * 3 + 2] * y / height));
							}

							sourceY -= xJump;
							sourceYint = Convert.ToInt32(sourceY);
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

		#region Erase8bpp()
		private static void Erase8bpp(Bitmap bitmap, Finger finger)
		{
			BitmapData bmpData = null;
			int heightConstant = 15;

			try
			{
				bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
				int stride = bmpData.Stride;

				int width = finger.PointUR.X - finger.PointUL.X;
				int height = finger.PointLL.Y - finger.PointUL.Y;
				int ulCornerX = finger.PointUL.X;
				int ulCornerY = finger.PointUL.Y;

				Color[] palette = bitmap.Palette.Entries;
				byte[] paletteInv = new byte[256];

				for (int i = 0; i < 256; i++)
					paletteInv[palette[i].R] = (byte)i;

				byte[,] stripT = new byte[heightConstant, width];
				byte[,] stripB = new byte[heightConstant, width];

				int sourceX;
				double sourceY;
				int sourceYint;
				double xJump = (finger.PointLL.X - ulCornerX) / (double)height;

				unsafe
				{
					byte* pOrig = (byte*)bmpData.Scan0.ToPointer();
					
					for (int y = ulCornerY; y < ulCornerY + heightConstant; y++)
						for (int x = ulCornerX; x < ulCornerX + width; x++)
							if (x >= 0 && x < bmpData.Width && y >= 0 && y < bmpData.Height)
								stripT[y - ulCornerY, x - ulCornerX] = palette[*(pOrig + y * stride + x)].R;

					for (int y = finger.PointLL.Y - heightConstant; y < finger.PointLL.Y; y++)
						for (int x = finger.PointLL.X; x < finger.PointLL.X + width; x++)
							if (x >= 0 && x < bmpData.Width && y >= 0 && y < bmpData.Height)
								stripB[y - finger.PointLL.Y + heightConstant, x - finger.PointLL.X] = palette[*(pOrig + y * stride + x)].R;


					for (int y = 0; y < height; y++)
					{
						sourceX = Convert.ToInt32(ulCornerX + y * xJump);
						sourceY = ulCornerY + y;
						sourceYint = Convert.ToInt32(sourceY);

						int		pBckgT = (y % heightConstant);
						int		pBckgB = (height - y) % heightConstant;

						for (int x = 0; x < width; x++)
						{
							if (x >= 0 && x < bmpData.Width && sourceYint >= 0 && sourceYint < bmpData.Height)
								pOrig[sourceYint * stride + x + sourceX] = paletteInv[(stripT[pBckgT, x] * (height - y) / height) + (stripB[pBckgB, x] * y / height)];

							if (x >= 0 && x < bmpData.Width && sourceYint + 1 >= 0 && sourceYint + 1 < bmpData.Height && (sourceY - sourceYint > 0.000001))
								pOrig[(sourceYint + 1) * stride + x + sourceX] = paletteInv[(stripT[pBckgT, x] * (height - y) / height) + (stripB[pBckgB, x] * y / height)];

							sourceY -= xJump;
							sourceYint = Convert.ToInt32(sourceY);
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

		#region Erase1bpp()
		private static void Erase1bpp(Bitmap bitmap, Finger finger)
		{
			BitmapData bmpData = null;

			try
			{
				bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

				int		stride = bmpData.Stride;
				double	angle = finger.Page.Skew;

				int x, y;
				int width = finger.PointUR.X - finger.PointUL.X;
				int height = finger.PointLL.Y - finger.PointUL.Y;
				int ulCornerX = finger.PointUL.X;
				int ulCornerY = finger.PointUL.Y;

				int sourceX;
				double sourceY;
				int sourceYint;
				double xJump = (finger.PointLL.X - ulCornerX) / (double)height;

				unsafe
				{
					byte* pOrig = (byte*)bmpData.Scan0.ToPointer();

					for (y = 0; y < height; y++)
					{
						sourceX = Convert.ToInt32(ulCornerX + y * xJump);
						sourceY = ulCornerY + y;
						sourceYint = Convert.ToInt32(sourceY);

						for (x = sourceX - 3; x < sourceX + width + 3; x++)
						{
							if (x >= 0 && x < bmpData.Width && sourceYint >= 0 && sourceYint < bmpData.Height)
								pOrig[sourceYint * stride + x / 8] |= (byte) (0x80 >> (x & 0x07));

							if (x >= 0 && x < bmpData.Width && sourceYint + 1 >= 0 && sourceYint + 1 < bmpData.Height && (sourceY - sourceYint > 0.000001))
								pOrig[(sourceYint + 1) * stride + x / 8] |= (byte)(0x80 >> (x & 0x07));

							sourceY -= xJump;
							sourceYint = Convert.ToInt32(sourceY);
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

		#region GetBiggestRectangle()
		private static Rectangle GetBiggestRectangle(ArrayList rectList)
		{
			if(rectList.Count > 0)
			{
				Rectangle	biggestRect = (Rectangle) rectList[0];

				for(int i = 1; i < rectList.Count; i++)
					if(biggestRect.Width * biggestRect.Height < ((Rectangle) rectList[i]).X * ((Rectangle) rectList[i]).Y)
						biggestRect = (Rectangle) rectList[i];

				return biggestRect;
			}
			else
				return Rectangle.Empty;
		}
		#endregion

		#region FindFromTop()
		public static unsafe int FindFromTop(byte* pOrig, int stride, Point ulPoint, int xMax, int yMax, double xJump, 
			int blockWidth, int blockHeight, float percentageWhite, bool findContent)
		{
			double		x = ulPoint.X;
			int		y;
			int		yJump = Convert.ToInt16(blockHeight / 3);

			if (yJump == 0)
				yJump = 1;


			for (y = ulPoint.Y; y < (yMax - blockHeight); y += yJump)
			{
				if (findContent)
				{
					if (RasterProcessing.PercentageWhite(pOrig, stride, (int) x, y, blockWidth, blockHeight) > percentageWhite)
						return y;
				}
				else
				{
					if (RasterProcessing.PercentageWhite(pOrig, stride, (int)x, y, blockWidth, blockHeight) <= percentageWhite)
						return y;
				}

				x = Math.Max(0, Math.Min(xMax - blockWidth, x + (xJump * yJump)));
			}

			return int.MaxValue;
		}
		#endregion	

		#region GetPointOnLine()
		private static int GetX(Point ulPoint, double xJump, int y, int xMax)
		{
			return (int)Math.Max(0, Math.Min(xMax, ulPoint.X + (y - ulPoint.Y) * xJump));
		}
		#endregion

		#region GetBlocksMap()
		private static int[,] GetBlocksMap(Bitmap bitmap, ItPage page)
		{
			BitmapData	sourceData = null;
			int			width = page.ClipRect.Width;
			int			height = page.ClipRect.Height;
			int[,]		blocksMap = new int[(int)Math.Ceiling(height / 8.0), (int)Math.Ceiling(width / 8.0)];

			try
			{
				int ulCornerX = page.Clip.PointUL.X;
				int ulCornerY = page.Clip.PointUL.Y;

				double xJump = Math.Cos(page.Skew);
				double yJump = Math.Sin(page.Skew);
				
				int sourceW = bitmap.Width;
				int sourceH = bitmap.Height;
				
				sourceData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				
				int sStride = sourceData.Stride;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pSourceCurrent;

					for (int y = 0; y < height; y++)
					{
						double sourceX = ulCornerX - (y * yJump);
						int sourceXInt = (int)sourceX;
						double sourceY = ulCornerY + (y * xJump);
						int sourceYInt = (int)sourceY;

						for (int x = 0; x < width; x++)
						{
							if ((sourceXInt >= 0) && (sourceXInt < sourceW) && (sourceYInt >= 0) && (sourceYInt < sourceH))
							{
								pSourceCurrent = ((pSource + (sourceYInt * sStride)) + (sourceXInt / 8));

								if ((pSourceCurrent[0] & (((int)0x80) >> (sourceXInt & 7))) > 0)
									blocksMap[y / 8, x / 8]++;
							}

							sourceX += xJump;
							sourceXInt = (int)sourceX;
							sourceY += yJump;
							sourceYInt = (int)sourceY;
						}
					}
				}
			}
			finally
			{
				if (sourceData != null)
					bitmap.UnlockBits(sourceData);
			}

			//fix last column
			if ((page.ClipRect.Width % 8) < 7)
			{
				int x = blocksMap.GetLength(1) - 1;

				if(x > 0)
					for (int y = 0; y < blocksMap.GetLength(0); y++)
						blocksMap[y, x] = blocksMap[y, x - 1];
			}

			//fix last row
			if ((page.ClipRect.Height % 8) < 7)
			{
				int y = blocksMap.GetLength(0) - 1;

				if (y > 0)
					for (int x = 0; x < blocksMap.GetLength(1); x++)
						blocksMap[y, x] = blocksMap[y - 1, x];
			}

			//DrawBlocksMapToFile(blocksMap);
			return blocksMap;
		}
		#endregion

		#region GetFingers()
		private static Fingers GetFingers(ItPage page, int[,] blocksMap)
		{
			Fingers		fingers = new Fingers();
			int			arrayW = blocksMap.GetLength(1);
			int			arrayH = blocksMap.GetLength(0);
			int[,]		array = FindObjects(blocksMap);
			List<int>	usedIndexes = new List<int>();
			int			dpi = page.ItImage.ImageInfo.DpiH;

			for (int y = 0; y < arrayH; y++)
			{
				if (page.Layout == ItPage.PageLayout.Left || page.Layout == ItPage.PageLayout.SinglePage)
					if (!((array[y, 0] == 0) || usedIndexes.Contains(array[y, 0])))
						usedIndexes.Add(array[y, 0]);

				if (page.Layout == ItPage.PageLayout.Right|| page.Layout == ItPage.PageLayout.SinglePage)
					if (!((array[y, arrayW - 1] == 0) || usedIndexes.Contains(array[y, arrayW - 1])))
						usedIndexes.Add(array[y, arrayW - 1]);
			}

			foreach (int usedIndex in usedIndexes)
			{
				int? left = null, top = null, right = null, bottom = null;
				
				for (int y = 0; y < arrayH; y++)
					for (int x = 0; x < arrayW; x++)
						if (array[y, x] == usedIndex)
						{
							if (left.HasValue)
							{
								left = (left < x) ? left : x;
								top = (top < y) ? top : y;
								right = (right > x) ? right : x;
								bottom = (bottom > y) ? bottom : y;
							}
							else
							{
								left = x;
								top = y;
								right = x;
								bottom = y;
							}
						}

				if (left.HasValue)
				{
					Point pUL = page.Clip.TransferSkewedToUnskewedPoint(new Point(page.ClipRect.X + left.Value * 8, page.ClipRect.Y + top.Value * 8));
					Point pLR = page.Clip.TransferSkewedToUnskewedPoint(new Point(page.ClipRect.X + (right.Value + 1) * 8, page.ClipRect.Y + (bottom.Value + 1) * 8));
				
					Rectangle fingerRect = Rectangle.FromLTRB(pUL.X, pUL.Y, pLR.X, pLR.Y);

					fingerRect.Intersect(page.ClipRect);

					if ((fingerRect.Width > dpi / 16) && (fingerRect.Height > dpi / 16) && (fingerRect.Width < dpi * 2) && (fingerRect.Height < dpi * 4))
					{
						fingerRect.Inflate(30,50);
						fingerRect.Intersect(page.ClipRect);						

						Finger finger = Finger.GetFinger(page, fingerRect, GetConfidence(page, fingerRect));

						if (finger != null)
							fingers.Add(finger);
					}
				}
			}

			//merge overlapping fingers
			if (fingers.Count > 1)
			{
				for(int i = fingers.Count - 2; i >= 0; i--)
					for(int j = fingers.Count - 1; j > i; j--)
						if (Rectangle.Intersect(fingers[i].RectangleNotSkewed, fingers[j].RectangleNotSkewed) != Rectangle.Empty)
						{
							Rectangle union = Rectangle.Union(fingers[i].RectangleNotSkewed, fingers[j].RectangleNotSkewed);
							
							fingers[i].SetClip(union.Left, union.Top, union.Right, union.Bottom);
							fingers[i].Confidence = GetConfidence(page, fingers[i].RectangleNotSkewed);
							fingers.RemoveAt(j);
						}
			}

			return fingers;
		}
		#endregion

		#region FindObjects()
		private static unsafe int[,] FindObjects(int[,] blocksMap)
		{
			int		x, y;
			int		width = blocksMap.GetLength(1);
			int		height = blocksMap.GetLength(0);
			int[,]	array = new int[height, width];
			int		id = 1;
			RasterProcessing.Pairs pairs = new RasterProcessing.Pairs();

			for (y = 0; y < height; y++)
			{				
				for (x = 0; x < width; x++)
				{
					if (blocksMap[y, x] >= 5)
					{
						if ((y > 0) && (array[y - 1, x] != 0))
						{
							array[y, x] = array[y - 1, x];

							if (((x > 0) && (array[y, x - 1] != 0)) && (array[y, x] != array[y, x - 1]))
								pairs.Add(array[y, x - 1], array[y, x]);
						}
						else if (((x > 0) && (y > 0)) && (array[y - 1, x - 1] != 0))
						{
							array[y, x] = array[y - 1, x - 1];
							
							if ((array[y, x - 1] != 0) && (array[y, x] != array[y, x - 1]))
								pairs.Add(array[y, x - 1], array[y, x]);
							if ((((x < (width - 1)) && (array[y - 1, x + 1] != 0)) && (blocksMap[y, x + 1] < 5)) && (array[y - 1, x + 1] != array[y - 1, x - 1]))
								pairs.Add(array[y, x], array[y - 1, x + 1]);
						}
						else if (((y > 0) && (x < (width - 1))) && (array[y - 1, x + 1] != 0))
						{
							array[y, x] = array[y - 1, x + 1];
							
							if (((x > 0) && (array[y, x - 1] != 0)) && (array[y, x] != array[y, x - 1]))
								pairs.Add(array[y, x - 1], array[y, x]);
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
				}
			}

			pairs.Compact();
			SortedList<int, int> sortedList = pairs.GetSortedList();
			int value;
			
			for (y = 0; y < height; y++)
				for (x = 0; x < width; x++)
					
					if ((array[y, x] != 0) && sortedList.TryGetValue(array[y, x], out value))
						array[y, x] = value;
			
			return array;
		}
		#endregion

		#region DrawBlocksMapToFile()
		private static void DrawBlocksMapToFile(int[,] blocksMap)
		{
#if SAVE_RESULTS
			Bitmap result = null;

			try
			{
				int width = blocksMap.GetLength(1);
				int height = blocksMap.GetLength(0);

				result = new Bitmap(width * 8, height * 8, PixelFormat.Format24bppRgb);
				int counter = 0;
				Graphics g = Graphics.FromImage(result);
				Color color = Debug.GetColor(counter++);

				for (int y = 0; y < height; y++)
					for (int x = 0; x < width; x++)
					{
						if (blocksMap[y, x] >= 5)//color = Color.FromArgb(100, color.R, color.G, color.B);
							g.FillRectangle(new SolidBrush(color), new Rectangle(x * 8, y * 8, 7, 7));
					}

				result.Save(Debug.SaveToDir + "Block Map.png", ImageFormat.Png);
				result.Dispose();
			}
			catch (Exception ex)
			{ 
				Console.WriteLine("DrawBlocksMapToFile() Error: " + ex.Message);
			}
#endif
		}
		#endregion

		#region GetConfidence()
		private static float GetConfidence(ItPage page, Rectangle fingerRect)
		{
			float	confidenceX = 1.0F;
			float	confidenceY = 1.0F;
			int		dpi = page.ItImage.ImageInfo.DpiH;

			/*if (fingerRect.Width < dpi / 4.0)
				confidenceX = fingerRect.Width / (dpi / 2.0F);
			else*/
			if (fingerRect.Width > dpi * 1.0)
				confidenceX = (dpi / 1.5F) / fingerRect.Width;

			/*if (fingerRect.Height < dpi / 3.0)
				confidenceY = fingerRect.Height / (dpi / 2.0F);
			else*/
			if (fingerRect.Height > dpi * 1.5)
				confidenceY = (dpi / 2.0F) / fingerRect.Height;

			return confidenceX * confidenceY;
		}
		#endregion

		#endregion

	}

	#region Paging
	[Flags]
	public enum Paging
	{
		Left = 1,
		Right = 2,
		Both = 3
	}
	#endregion


}
