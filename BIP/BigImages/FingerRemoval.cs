using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections ;
using System.Collections.Generic;

using ImageProcessing.PageObjects;
using BIP.Geometry;

namespace ImageProcessing.BigImages
{
	/// <summary>
	/// Summary description for FingerRemoval.
	/// max finger size is 2" width by 3.5" height 
	/// </summary>
	public class FingerRemoval
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public FingerRemoval()
		{
		}
		#endregion

		#region private classes

		#region FingerZone
		class FingerZone
		{
			public readonly ImageProcessing.IpSettings.Finger Finger;
			public readonly Rectangle ZoneRect;
			public readonly Rectangle FingerRect;
			public readonly Bitmap OriginalBitmap;

			#region constructor
			public FingerZone(ImageProcessing.IpSettings.Finger finger, Rectangle sourceZoneRect, Rectangle sourceFingerRect, Bitmap originalBitmap)
			{
				this.Finger = finger;
				this.ZoneRect = sourceZoneRect;
				this.FingerRect = sourceFingerRect;
				this.OriginalBitmap = originalBitmap;
			}
			#endregion

			#region enum Orientation
			enum Orientation
			{
				Left = 0x01,
				Top = 0x02,
				Right = 0x04,
				Bottom = 0x08,
				TopBottom = Top | Bottom,
				LeftRight = Left | Right,
				TopLeft = Top | Left,
				TopRight = Top | Right,
				BottomLeft = Bottom | Left,
				BottomRight = Bottom | Right,
				Unknown = 0
			}
			#endregion

			//PUBLIC METHODS
			#region public methods

			#region Dispose()
			public void Dispose()
			{
				/*if (OriginalBitmap != null)
				{
					OriginalBitmap.Dispose();
					OriginalBitmap = null;
				}*/
			}
			#endregion

			#region GetFixedBitmap()
			/// <summary>
			/// Returns fixed bitmap with size = FingerRect.Size. Finger is erased by using neighbour area.
			/// If source is black/white bitmap, 
			///		it returns white bitmap.
			/// else
			///		If there are top and bottom at least 20 pixels height, it uses just that;
			///		else if there are 2 sides at least 20 pixels wide, it interpolates that;
			///		else it uses 2 valid areas if they exist, else it uses 1 valid area if it exists, else it returns black bitmap.
			/// </summary>
			/// <returns></returns>
			public unsafe Bitmap GetFixedBitmap()
			{
#if SAVE_RESULTS
				this.OriginalBitmap.Save(Debug.SaveToDir + @"\Finger Source.png", ImageFormat.Png);
#endif
				
				Bitmap result = new Bitmap(FingerRect.Width, FingerRect.Height, OriginalBitmap.PixelFormat);
				BitmapData sourceData = null;
				BitmapData resultData = null;

				try
				{
					sourceData = OriginalBitmap.LockBits(new Rectangle(Point.Empty, OriginalBitmap.Size), ImageLockMode.ReadOnly, OriginalBitmap.PixelFormat);
					resultData = result.LockBits(new Rectangle(Point.Empty, result.Size), ImageLockMode.WriteOnly, OriginalBitmap.PixelFormat);

					int strideResult = resultData.Stride;
					int strideSource = sourceData.Stride;

					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					//if black-white, return white bitmap
					if (OriginalBitmap.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						for (int y = 0; y < resultData.Height; y++)
							for (int x = 0; x < resultData.Stride; x++)
								pResult[y * strideResult + x] = 0xFF;
					}
					else
					{
						Rectangle rect = new Rectangle(FingerRect.Left - ZoneRect.Left, FingerRect.Top - ZoneRect.Top, FingerRect.Width, FingerRect.Height);
						
						bool isTopValid = FingerRect.Top - ZoneRect.Top >= 20;
						bool isBottomValid = ZoneRect.Bottom - FingerRect.Bottom >= 20;
						bool isLeftValid = FingerRect.Left - ZoneRect.Left >= 20;
						bool isRightValid = ZoneRect.Right - FingerRect.Right >= 20;
						Orientation orientation = Orientation.Unknown;
						Rectangle leftRect = Rectangle.FromLTRB(Math.Max(0, rect.Left - 20), rect.Top, rect.Left, rect.Bottom);
						Rectangle topRect = Rectangle.FromLTRB(rect.X, Math.Max(0, rect.Top - 20), rect.Right, rect.Top);
						Rectangle rightRect = Rectangle.FromLTRB(rect.Right, rect.Top, Math.Min(rect.Right + 20, ZoneRect.Right - ZoneRect.Left), rect.Bottom);
						Rectangle bottomRect = Rectangle.FromLTRB(rect.X, rect.Bottom, rect.Right, Math.Min(ZoneRect.Bottom - ZoneRect.Top, rect.Bottom + 20));

						if (isLeftValid)
							orientation |= Orientation.Left;
						if (isTopValid)
							orientation |= Orientation.Top;
						if (isRightValid)
							orientation |= Orientation.Right;
						if (isBottomValid)
							orientation |= Orientation.Bottom;

						if ((orientation & Orientation.TopBottom) == Orientation.TopBottom)
						{
							if (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppArgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppRgb)
							{
								int bytesPerPixel = (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
								
								for (int y = 0; y < resultData.Height; y++)
								{
									double	ratio = (y / (double)resultData.Height);
									int		topY = topRect.Top + (y % topRect.Height);
									int		bottomY = (bottomRect.Top + (y % bottomRect.Height));
									
									for (int x = 0; x < resultData.Width; x++)
									{
										pResult[y * strideResult + x * bytesPerPixel] = (byte)((1.0 - ratio) * pSource[topY * strideSource + ((topRect.Left + x) * bytesPerPixel)] +
											ratio * pSource[bottomY * strideSource + ((bottomRect.Left + x) * bytesPerPixel)]);
										pResult[y * strideResult + x * bytesPerPixel + 1] = (byte)((1.0 - ratio) * pSource[topY * strideSource + ((topRect.Left + x) * bytesPerPixel + 1)] +
											ratio * pSource[bottomY * strideSource + ((bottomRect.Left + x) * bytesPerPixel + 1)]);
										pResult[y * strideResult + x * bytesPerPixel + 2] = (byte)((1.0 - ratio) * pSource[topY * strideSource + ((topRect.Left + x) * bytesPerPixel + 2)] +
											ratio * pSource[bottomY * strideSource + ((bottomRect.Left + x) * bytesPerPixel + 2)]);
									}
								}
							}
							else
							{
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Stride; x++)
										pResult[y * strideResult + x] = (byte)((1.0 - y / (double)resultData.Height) * pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x)] +
											(y / (double)resultData.Height) * pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x)]);
							}
						}
						else if ((orientation & Orientation.LeftRight) == Orientation.LeftRight)
						{
							if (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppArgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppRgb)
							{
								int bytesPerPixel = (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
								
								for (int y = 0; y < resultData.Height; y++)
								{
									for (int x = 0; x < resultData.Width; x++)
									{
										pResult[y * strideResult + x * bytesPerPixel] = (byte)((1.0 - x / (double)resultData.Width) * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel] +
											(x / (double)resultData.Width) * pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width)) * bytesPerPixel]);
										pResult[y * strideResult + x * bytesPerPixel + 1] = (byte)((1.0 - x / (double)resultData.Width) * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 1] +
											(x / (double)resultData.Width) * pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width)) * bytesPerPixel + 1]);
										pResult[y * strideResult + x * bytesPerPixel + 2] = (byte)((1.0 - x / (double)resultData.Width) * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 2] +
											(x / (double)resultData.Width) * pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width)) * bytesPerPixel + 2]);
									}
								}
							}
							else
							{
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Stride; x++)
										pResult[y * strideResult + x] = (byte)((1.0 - x / (double)resultData.Width) * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width))] +
											(x / (double)resultData.Width) * pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width))]);
							}
						}
						else if ((orientation & Orientation.TopLeft) == Orientation.TopLeft)
						{
							if (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppArgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppRgb)
							{
								int bytesPerPixel = (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
								double ratioT, ratioL;

								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Width; x++)
									{
										ratioT = x / (double)(x + y);
										ratioL = y / (double)(x + y);

										pResult[y * strideResult + x * bytesPerPixel + 0] = (byte)(ratioT * pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x) * bytesPerPixel + 0] +
											ratioL * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 0]);
										pResult[y * strideResult + x * bytesPerPixel + 1] = (byte)(ratioT * pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x) * bytesPerPixel + 1] +
											ratioL * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 1]);
										pResult[y * strideResult + x * bytesPerPixel + 2] = (byte)(ratioT * pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x) * bytesPerPixel + 2] +
											ratioL * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 2]);
										/*
										pResult[y * strideResult + x * bytesPerPixel + 0] = (byte)(0.5 * pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x) * bytesPerPixel + 0] +
											0.5 * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 0]);
										pResult[y * strideResult + x * bytesPerPixel + 1] = (byte)(0.5 * pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x) * bytesPerPixel + 1] +
											0.5 * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 1]);
										pResult[y * strideResult + x * bytesPerPixel + 2] = (byte)(0.5 * pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x) * bytesPerPixel + 2] +
											0.5 * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 2]);
										*/
									}
							}
							else
							{
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Stride; x++)
										pResult[y * strideResult + x] = (byte)(0.5 * pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x)] +
											0.5 * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width))]);
							}
						}
						else if ((orientation & Orientation.TopRight) == Orientation.TopRight)
						{
							if (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppArgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppRgb)
							{
								int bytesPerPixel = (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
								double ratioT, ratioR;
	
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Width; x++)
									{
										ratioT = (resultData.Width - x) / (double)((resultData.Width - x) + y);
										ratioR = y / (double)((resultData.Width - x) + y);

										pResult[y * strideResult + x * bytesPerPixel + 0] = (byte)(ratioT * pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x) * bytesPerPixel + 0] +
											ratioR * pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width)) * bytesPerPixel + 0]);
										pResult[y * strideResult + x * bytesPerPixel + 1] = (byte)(ratioT * pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x) * bytesPerPixel + 1] +
											ratioR * pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width)) * bytesPerPixel + 1]);
										pResult[y * strideResult + x * bytesPerPixel + 2] = (byte)(ratioT * pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x) * bytesPerPixel + 2] +
											ratioR * pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width)) * bytesPerPixel + 2]);
									}
							}
							else
							{
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Stride; x++)
										pResult[y * strideResult + x] = (byte)(0.5 * pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x)] +
											0.5 * pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width))]);
							}
						}
						else if ((orientation & Orientation.BottomLeft) == Orientation.BottomLeft)
						{
							if (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppArgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppRgb)
							{
								int bytesPerPixel = (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
								double ratioB, ratioL;
								
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Width; x++)
									{
										ratioB = x / (double)(x + (resultData.Height - y));
										ratioL = (resultData.Height - y) / (double)(x + (resultData.Height - y));

										pResult[y * strideResult + x * bytesPerPixel + 0] = (byte)(ratioB * pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x) * bytesPerPixel + 0] +
											ratioL * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 0]);
										pResult[y * strideResult + x * bytesPerPixel + 1] = (byte)(ratioB * pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x) * bytesPerPixel + 1] +
											ratioL * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 1]);
										pResult[y * strideResult + x * bytesPerPixel + 2] = (byte)(ratioB * pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x) * bytesPerPixel + 2] +
											ratioL * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 2]);
									}
							}
							else
							{
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Stride; x++)
										pResult[y * strideResult + x] = (byte)(0.5 * pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x)] +
											0.5 * pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width))]);
							}
						}
						else if ((orientation & Orientation.BottomRight) == Orientation.BottomRight)
						{
							if (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppArgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppRgb)
							{
								int bytesPerPixel = (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
								double ratioB, ratioR;
		
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Width; x++)
									{
										ratioB = (resultData.Width - x) / (double)((resultData.Width - x) + (resultData.Height - y));
										ratioR = (resultData.Height - y) / (double)((resultData.Width - x) + (resultData.Height - y));

										pResult[y * strideResult + x * bytesPerPixel + 0] = (byte)(ratioB * pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x) * bytesPerPixel + 0] +
											ratioR * pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width)) * bytesPerPixel + 0]);
										pResult[y * strideResult + x * bytesPerPixel + 1] = (byte)(ratioB * pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x) * bytesPerPixel + 1] +
											ratioR * pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width)) * bytesPerPixel + 1]);
										pResult[y * strideResult + x * bytesPerPixel + 2] = (byte)(ratioB * pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x) * bytesPerPixel + 2] +
											ratioR * pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width)) * bytesPerPixel + 2]);
									}
							}
							else
							{
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Stride; x++)
										pResult[y * strideResult + x] = (byte)(0.5 * pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x)] +
											0.5 * pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width))]);
							}
						}
						else if ((orientation & Orientation.Top) == Orientation.Top)
						{
							if (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppArgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppRgb)
							{
								int bytesPerPixel = (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
								
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Width; x++)
									{
										pResult[y * strideResult + x * bytesPerPixel + 0] = pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x) * bytesPerPixel + 0];
										pResult[y * strideResult + x * bytesPerPixel + 1] = pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x) * bytesPerPixel + 1];
										pResult[y * strideResult + x * bytesPerPixel + 2] = pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x) * bytesPerPixel + 2];
									}
							}
							else
							{
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Stride; x++)
										pResult[y * strideResult + x] = pSource[(topRect.Top + (y % topRect.Height)) * strideSource + (topRect.Left + x)];
							}
						}
						else if ((orientation & Orientation.Bottom) == Orientation.Bottom)
						{
							if (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppArgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppRgb)
							{
								int bytesPerPixel = (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
								
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Width; x++)
									{
										pResult[y * strideResult + x * bytesPerPixel + 0] = pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x) * bytesPerPixel + 0];
										pResult[y * strideResult + x * bytesPerPixel + 1] = pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x) * bytesPerPixel + 1];
										pResult[y * strideResult + x * bytesPerPixel + 2] = pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x) * bytesPerPixel + 2];
									}
							}
							else
							{
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Stride; x++)
										pResult[y * strideResult + x] = pSource[(bottomRect.Top + (y % bottomRect.Height)) * strideSource + (bottomRect.Left + x)];
							}
						}
						else if ((orientation & Orientation.Left) == Orientation.Left)
						{
							if (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppArgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppRgb)
							{
								int bytesPerPixel = (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
								
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Width; x++)
									{
										pResult[y * strideResult + x * bytesPerPixel + 0] = pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 0];
										pResult[y * strideResult + x * bytesPerPixel + 1] = pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 1];
										pResult[y * strideResult + x * bytesPerPixel + 2] = pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width)) * bytesPerPixel + 2];
									}
							}
							else
							{
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Stride; x++)
										pResult[y * strideResult + x] = pSource[(leftRect.Top + y) * strideSource + (leftRect.Left + (x % leftRect.Width))];
							}
						}
						else if ((orientation & Orientation.Right) == Orientation.Right)
						{
							if (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppArgb || this.OriginalBitmap.PixelFormat == PixelFormat.Format32bppRgb)
							{
								int bytesPerPixel = (this.OriginalBitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
								
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Width; x++)
									{
										pResult[y * strideResult + x * bytesPerPixel + 0] = pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width)) * bytesPerPixel + 0];
										pResult[y * strideResult + x * bytesPerPixel + 1] = pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width)) * bytesPerPixel + 1];
										pResult[y * strideResult + x * bytesPerPixel + 2] = pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width)) * bytesPerPixel + 2];
									}
							}
							else
							{
								for (int y = 0; y < resultData.Height; y++)
									for (int x = 0; x < resultData.Stride; x++)
										pResult[y * strideResult + x] = pSource[(rightRect.Top + y) * strideSource + (rightRect.Left + (x % rightRect.Width))];
							}
						}
					}
				}
				catch (Exception ex)
				{
					throw ex;
				}
				finally
				{
					if (sourceData != null)
						OriginalBitmap.UnlockBits(sourceData);
					if (resultData != null)
						result.UnlockBits(resultData);
				}

				result.SetResolution(OriginalBitmap.HorizontalResolution, OriginalBitmap.VerticalResolution);

				if (OriginalBitmap.Palette != null && OriginalBitmap.Palette.Entries.Length > 0)
					result.Palette = OriginalBitmap.Palette;

#if SAVE_RESULTS
				result.Save(Debug.SaveToDir + @"Finger Wanished.png", ImageFormat.Png);
#endif

				return result;
			}
			#endregion

			#endregion

		}
		#endregion

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region FindFingers()
		public static void FindFingers(Bitmap raster, ImageProcessing.IpSettings.ItPage page)
		{
			ImageProcessing.IpSettings.ClipInt clip = new ImageProcessing.IpSettings.ClipInt(page.ClipRect, page.Skew, raster.Size);
			int[,] blocksMap = GetBlocksMap(raster, clip);

			page.Fingers.Clear();
			page.Fingers.AddRange(GetFingers(page, blocksMap, raster.Size));
		}
		#endregion
	
		#region EraseFingers()
		/// <summary>
		/// It works in couple of steps. It works after clipping, deskewing and bookfold correction are done. 
		/// </summary>
		/// <param name="itDecoder">Cropped, deskewed and with bookfold fix</param>
		/// <param name="destPath">Destination file path, must be different than itDecoder path</param>
		/// <param name="imageFormat"></param>
		/// <param name="tiffCompression"></param>
		/// <param name="jpegQuality"></param>
		/// <param name="page"></param>
		public void EraseFingers(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath,
			ImageProcessing.FileFormat.IImageFormat imageFormat, ImageProcessing.IpSettings.ItPage page, Size originalImageSize)
		{
			ImageProcessing.BigImages.ItEncoder itEncoder = new ItEncoder(destPath, imageFormat, itDecoder.PixelsFormat, itDecoder.Width, itDecoder.Height, itDecoder.DpiX, itDecoder.DpiY);

			try
			{
				switch (itDecoder.PixelFormat)
				{
					case PixelFormat.Format32bppRgb:
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format8bppIndexed:
					case PixelFormat.Format1bppIndexed:
						EraseFingers(itDecoder, itEncoder, page, originalImageSize);
						break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			catch (Exception ex)
			{
				try { itEncoder.Dispose(); }
				catch { }

				itEncoder = null;

				try
				{
					if (System.IO.File.Exists(destPath))
						System.IO.File.Delete(destPath);
				}
				catch { }

				throw new Exception("Big Images, EraseFingers, Execute(): " + ex.Message);
			}
			finally
			{
				if (itEncoder != null)
					itEncoder.Dispose();
			}
		}
		#endregion	
		
		#endregion

		//	PRIVATE METHODS
		#region private methods

		#region EraseFingers()
		/// <summary>
		/// 1) It computes fingers regions - inverting transforms of cropping, deskewing and bookfold fix. 
		/// 2) It gets fingers zones from image, adding neighbourhood for erasing data - it remembers, what is image zone and what is neighbourhood.
		/// 3) Working with fingers zones, it erases fingers using the neighbours.
		/// 4) Going downwards by strips, it copies itDecoder to the itEncoder - if there is a finger zone in the strip, finger zone overwrites copy.
		/// </summary>
		/// <param name="itDecoder"></param>
		/// <param name="itEncoder"></param>
		/// <param name="page"></param>
		private unsafe void EraseFingers(ImageProcessing.BigImages.ItDecoder itDecoder, ImageProcessing.BigImages.ItEncoder itEncoder, ImageProcessing.IpSettings.ItPage page, Size originalImageSize)
		{
			List<FingerZone> fingerZones = new List<FingerZone>();

			//getting finger zones
			foreach (ImageProcessing.IpSettings.Finger finger in page.Fingers)
			{
				Rectangle fingerRect = new Rectangle(Convert.ToInt32(finger.PageRect.X * itDecoder.Width), Convert.ToInt32(finger.PageRect.Y * itDecoder.Height), Convert.ToInt32(finger.PageRect.Width * itDecoder.Width), Convert.ToInt32(finger.PageRect.Height * itDecoder.Height));				
				fingerRect.Intersect(new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));
				
				Rectangle fingerZone = Rectangle.Inflate(fingerRect, 20, 20);
				fingerZone.Intersect(new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));

				Bitmap fingerClip = itDecoder.GetClip(fingerZone);
				Bitmap copy = ImageProcessing.ImageCopier.Copy(fingerClip);

				FingerZone zone = new FingerZone(finger, fingerZone, fingerRect, copy);
				fingerZones.Add(zone);
			}

			//copying data
			Bitmap source = null;
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			int width = itDecoder.Width;
			int height = itDecoder.Height;

			int stripHeight = Misc.GetStripHeightMax(itDecoder);

			//strip top in image coordinates
			for (int stripY = 0; stripY < height; stripY += stripHeight)
			{
				int stripB = Math.Min(height, stripY + stripHeight);

				try
				{
					result = new Bitmap(width, stripB - stripY, itDecoder.PixelFormat);
					resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

					Rectangle resultClip = Rectangle.FromLTRB(0, stripY, width, stripB);

					source = itDecoder.GetClip(resultClip);
					sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					int strideS = sourceData.Stride;
					int strideR = resultData.Stride;

					// copy bitmap
					for (int y = 0; y < stripB - stripY; y++)
					{
						for (int x = 0; x < strideR; x++)
						{
							pResult[y * strideR + x] = pSource[y * strideS + x];
						}
					}

					//erase all fingers that overlap
					foreach (FingerZone fingerZone in fingerZones)
					{
						if (Rectangle.Intersect(fingerZone.FingerRect, resultClip) != Rectangle.Empty)
						{
							Bitmap		fingerBitmap = null;
							BitmapData	fingerBitmapData = null;
							
							try
							{
								fingerBitmap = fingerZone.GetFixedBitmap();

#if DEBUG
								fingerBitmap.Save(@"c:\delete\fingerBitmap.png", ImageFormat.Png);
#endif

								fingerBitmapData = fingerBitmap.LockBits(new Rectangle(0,0, fingerBitmap.Width, fingerBitmap.Height), ImageLockMode.ReadOnly, fingerBitmap.PixelFormat);
								
								int		strideF = fingerBitmapData.Stride;
								byte*	pFinger = (byte*)fingerBitmapData.Scan0.ToPointer();

								//color bitmaps
								if (itDecoder.PixelFormat == PixelFormat.Format24bppRgb || itDecoder.PixelFormat == PixelFormat.Format32bppArgb || itDecoder.PixelFormat == PixelFormat.Format32bppRgb)
								{
									int bytesPerPixel = (itDecoder.PixelsFormat == PixelsFormat.Format32bppRgb) ? 4 : 3;

									int x, y;

									// because we are dealing with bitmap strips...
									int yFrom = Math.Max(0, stripY - fingerZone.FingerRect.Top);
									int yTo = Math.Min(stripB - fingerZone.FingerRect.Top, fingerBitmapData.Height);
									int stripFinterTop = fingerZone.FingerRect.Top - stripY;
										
									for (x = 0; x < fingerBitmapData.Width; x++)
									{
										for (y = yFrom; y < yTo; y++)
										{
											pResult[(y + stripFinterTop) * strideR + (x + fingerZone.FingerRect.Left) * bytesPerPixel] = pFinger[y * strideF + x * bytesPerPixel];
											pResult[(y + stripFinterTop) * strideR + (x + fingerZone.FingerRect.Left) * bytesPerPixel + 1] = pFinger[y * strideF + x * bytesPerPixel + 1];
											pResult[(y + stripFinterTop) * strideR + (x + fingerZone.FingerRect.Left) * bytesPerPixel + 2] = pFinger[y * strideF + x * bytesPerPixel + 2];
										}
									}
								}
								//8 bit indexed bitmaps
								else if (itDecoder.PixelFormat == PixelFormat.Format8bppIndexed)
								{
									for (int x = 0; x < fingerBitmapData.Width; x++)
									{
										for (int y = 0; y < fingerBitmapData.Height; y++)
										{
											pResult[(y + fingerZone.FingerRect.Top) * strideR + (x + fingerZone.FingerRect.Left)] = pFinger[y * strideF + x];
										}
									}
								}
								//1 bit indexed bitmaps
								else if (itDecoder.PixelFormat == PixelFormat.Format1bppIndexed)
								{
									for (int x = 0; x < fingerBitmapData.Width; x++)
									{
										for (int y = 0; y < fingerBitmapData.Height; y++)
										{
											pResult[(y + fingerZone.FingerRect.Top) * strideR + (x + fingerZone.FingerRect.Left) / 8] |= (byte)(0x80 >> (x & 0x07));
										}
									}
								}
							}
							finally
							{
								if (fingerBitmapData != null)
								{
									fingerBitmap.UnlockBits(fingerBitmapData);
									fingerBitmapData = null;
								}

								if (fingerBitmap != null)
								{
									fingerBitmap.Dispose();
									fingerBitmap = null;
								}
							}
						}
					}
				}
				finally
				{
					if (sourceData != null)
					{
						source.UnlockBits(sourceData);
						sourceData = null;
					}
					if (source != null)
					{
						source.Dispose();
						source = null;
					}
					if (resultData != null)
					{
						itEncoder.Write(stripB - stripY, resultData.Stride, (byte*)resultData.Scan0.ToPointer());
						result.UnlockBits(resultData);
						resultData = null;
					}
					if (result != null)
					{
						result.Dispose();
						result = null;
					}
				}

				if (ProgressChanged != null)
					ProgressChanged((stripB - stripY) / (float)height);
			}
		}
		#endregion

		#region GetBlocksMap()
		private static int[,] GetBlocksMap(Bitmap bitmap, ImageProcessing.IpSettings.ClipInt page)
		{
			BitmapData	sourceData = null;
			int			width = page.Width;
			int			height = page.Height;
			int[,]		blocksMap = new int[(int)Math.Ceiling(height / 8.0), (int)Math.Ceiling(width / 8.0)];

			try
			{
				int ulCornerX = page.PointUL.X;
				int ulCornerY = page.PointUL.Y;

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
			if ((page.Width % 8) < 7)
			{
				int x = blocksMap.GetLength(1) - 1;

				if(x > 0)
					for (int y = 0; y < blocksMap.GetLength(0); y++)
						blocksMap[y, x] = blocksMap[y, x - 1];
			}

			//fix last row
			if ((page.Height % 8) < 7)
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
		private static ImageProcessing.IpSettings.Fingers GetFingers(ImageProcessing.IpSettings.ItPage page, int[,] blocksMap, Size imageSizeInPixels)
		{
			ImageProcessing.IpSettings.Fingers	fingers = new ImageProcessing.IpSettings.Fingers();
			int									arrayW = blocksMap.GetLength(1);
			int									arrayH = blocksMap.GetLength(0);
			int[,]								array = FindObjects(blocksMap);
			List<int>							usedIndexes = new List<int>();
			double								dpi = page.ItImage.PageObjects.BitmapSize.Value.Width / page.ItImage.InchSize.Width;
			ImageProcessing.IpSettings.ClipInt	clipInt = new ImageProcessing.IpSettings.ClipInt(page.ClipRect, page.Skew, imageSizeInPixels);

			for (int y = 0; y < arrayH; y++)
			{
				if (page.Layout == ImageProcessing.IpSettings.ItPage.PageLayout.Left || page.Layout == ImageProcessing.IpSettings.ItPage.PageLayout.SinglePage)
					if (!((array[y, 0] == 0) || usedIndexes.Contains(array[y, 0])))
						usedIndexes.Add(array[y, 0]);

				if (page.Layout == ImageProcessing.IpSettings.ItPage.PageLayout.Right|| page.Layout == ImageProcessing.IpSettings.ItPage.PageLayout.SinglePage)
					if (!((array[y, arrayW - 1] == 0) || usedIndexes.Contains(array[y, arrayW - 1])))
						usedIndexes.Add(array[y, arrayW - 1]);
			}

			foreach (int usedIndex in usedIndexes)
			{
				int? left = null, top = null, right = null, bottom = null;

				for (int y = 0; y < arrayH; y++)
				{
					for (int x = 0; x < arrayW; x++)
					{
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
					}
				}

				if (left.HasValue)
				{
					Point pUL = clipInt.TransferSkewedToUnskewedPoint(new Point(clipInt.RectangleNotSkewed.X + left.Value * 8, clipInt.RectangleNotSkewed.Y + top.Value * 8));
					Point pLR = clipInt.TransferSkewedToUnskewedPoint(new Point(clipInt.RectangleNotSkewed.X + (right.Value + 1) * 8, clipInt.RectangleNotSkewed.Y + (bottom.Value + 1) * 8));
				
					Rectangle fingerRect = Rectangle.FromLTRB(pUL.X, pUL.Y, pLR.X, pLR.Y);

					fingerRect.Intersect(clipInt.RectangleNotSkewed);

					if ((fingerRect.Width > dpi / 16) && (fingerRect.Height > dpi / 16) && (fingerRect.Width < dpi * 2) && (fingerRect.Height < dpi * 4))
					{
						//fingerRect.Inflate((int)(dpi / 10), (int)(dpi / 8));
						fingerRect.Intersect(clipInt.RectangleNotSkewed);

						//ImageProcessing.IpSettings.Finger finger = ImageProcessing.IpSettings.Finger.GetFinger(page, fingerRect, GetConfidence(page, fingerRect));
						//RatioRect fingerRatioRect = new RatioRect((fingerRect.X - clipInt.RectangleNotSkewed.X) / (double)clipInt.Width, (fingerRect.Y - clipInt.RectangleNotSkewed.Y) / (double)clipInt.Height, fingerRect.Width / (double)clipInt.Width, fingerRect.Height / (double)clipInt.Height);
						RatioRect fingerRatioRect = new RatioRect(fingerRect.X / (double)imageSizeInPixels.Width, fingerRect.Y / (double)imageSizeInPixels.Height, fingerRect.Width / (double)imageSizeInPixels.Width, fingerRect.Height / (double)imageSizeInPixels.Height);
						ImageProcessing.IpSettings.Finger finger = ImageProcessing.IpSettings.Finger.GetFinger(page, fingerRatioRect, false);

						if (finger != null)
						{
							RatioRect r = finger.RectangleNotSkewed;
							r.Inflate((dpi / 10.0) / imageSizeInPixels.Width, (dpi / 8.0) / imageSizeInPixels.Height);
							r.Intersect(page.ClipRect);

							finger.SetClip(r);
							finger.Confidence = GetConfidence(page, fingerRatioRect);

							fingers.Add(finger);
						}
					}
				}
			}

			//merge overlapping fingers
			if (fingers.Count > 1)
			{
				for (int i = fingers.Count - 2; i >= 0; i--)
				{
					for (int j = fingers.Count - 1; j > i; j--)
					{
						if (RatioRect.Intersect(fingers[i].RectangleNotSkewed, fingers[j].RectangleNotSkewed) != RatioRect.Empty)
						{
							RatioRect union = RatioRect.Union(fingers[i].RectangleNotSkewed, fingers[j].RectangleNotSkewed);

							fingers[i].SetClip(union.Left, union.Top, union.Right, union.Bottom);
							fingers[i].Confidence = GetConfidence(page, fingers[i].RectangleNotSkewed);
							fingers.RemoveAt(j);
						}
					}
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
		private static float GetConfidence(ImageProcessing.IpSettings.ItPage page, RatioRect fingerRect)
		{
			double confidenceX = 1.0F;
			double confidenceY = 1.0F;
			double dpi = page.ItImage.PageObjects.BitmapSize.Value.Width / page.ItImage.InchSize.Width;

			if (fingerRect.Width > dpi * 1.0)
				confidenceX = (dpi / 1.5F) / fingerRect.Width;

			if (fingerRect.Height > dpi * 1.5)
				confidenceY = (dpi / 2.0F) / fingerRect.Height;

			return (float)(confidenceX * confidenceY);
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
