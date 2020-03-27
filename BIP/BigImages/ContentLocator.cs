using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing.ObjectsRecognition;

namespace ImageProcessing.BigImages
{
	/// <summary>
	/// Locates and returns either 1 shape of content of document - portrait or 2 shapes if image is landscape and book binding was found.
	/// 
	/// 1) it creates 100 dpi grayscale image.
	/// 2) it creates edge map out of it
	/// 3) it combines both above to detect paper, objects, and crap around it
	/// 4) going outside in, it detects crap
	/// 5) when eliminating crap goint outside in, it finds and returns border of the content.
	/// </summary>

	public class ContentLocator
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public ContentLocator()
		{
		}
		#endregion


		//PUBLIC METHODS
		#region public methods

		#region GetContent()
		public DocumentContent GetContent(ImageProcessing.BigImages.ItDecoder itDecoder)
		{
			Bitmap reducedImage = null;
			Bitmap edgeBitmap = null;

			try
			{
				reducedImage = GetReducedGrayImage(itDecoder);

#if SAVE_RESULTS
				reducedImage.Save(Debug.SaveToDir + "801 reduced.png", ImageFormat.Png);
#endif

				edgeBitmap = GetEdgeMap(reducedImage);

#if SAVE_RESULTS
				edgeBitmap.Save(Debug.SaveToDir + "802 edges.png", ImageFormat.Png);
#endif

				SmoothEdges(edgeBitmap);

#if SAVE_RESULTS
				edgeBitmap.Save(Debug.SaveToDir + "804 edges smoothed.png", ImageFormat.Png);
#endif

				DespeckleEdges1x1(edgeBitmap);

#if SAVE_RESULTS
				edgeBitmap.Save(Debug.SaveToDir + "805 edges despeckled.png", ImageFormat.Png);
#endif


#if SAVE_RESULTS
				Bitmap merged = MergeAutoColorAndEdges(reducedImage, edgeBitmap);
				merged.Save(Debug.SaveToDir + "808 merged color and edges.png", ImageFormat.Png);

				merged.Dispose();
#endif

				byte[,] mask = GetBitmapsMask(reducedImage, edgeBitmap);

				if (reducedImage.Width > reducedImage.Height)
					return GetBookContent(mask);
				else
					return GetDocumentContent(mask);

				/*
				ImageProcessing.Transforms.Fourier fourier = new ImageProcessing.Transforms.Fourier();

				fourier.LoadBitmap(smoothEdgesHighlited);

#if SAVE_RESULTS
				fourier.DrawToFile(Debug.SaveToDir + "806 fourier.png", ImageProcessing.Transforms.Fourier.DrawingComponent.Real);
#endif

				fourier.DespeckleMexicanHat13x13();

#if SAVE_RESULTS
				fourier.DrawToFile(Debug.SaveToDir + "807 fourier after despeckle.png", ImageProcessing.Transforms.Fourier.DrawingComponent.Real);
#endif

				Bitmap cleanedBitmap = fourier.GetBitmap();
#if SAVE_RESULTS
				cleanedBitmap.Save(Debug.SaveToDir + "808 edges after fourier.png", ImageFormat.Png);
#endif
				*/
			}
			finally
			{
				if (reducedImage != null)
					reducedImage.Dispose();
				if (edgeBitmap != null)
					edgeBitmap.Dispose();
			}
		}
		#endregion

		#endregion


		//INTERNAL METHODS
		#region internal methods

		#region GetEdgeMap()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		internal static Bitmap GetEdgeMap(Bitmap source)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				result = new Bitmap(source.Width, source.Height, PixelFormat.Format8bppIndexed);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				int clipHeight = result.Height - 1;
				int clipWidth = result.Width - 1;
				int x, y;
				int deltaMin, max;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					for (y = 1; y < clipHeight; y++)
					{
						for (x = 1; x < clipWidth; x++)
						{		
							byte x1 = pSource[(y - 1) * sStride + x - 1];
							byte x2 = pSource[(y - 1) * sStride + x];
							byte x3 = pSource[(y - 1) * sStride + x + 1];
							byte x4 = pSource[(y) * sStride + x - 1];
							byte x5 = pSource[(y) * sStride + x + 1];
							byte x6 = pSource[(y + 1) * sStride + x - 1];
							byte x7 = pSource[(y + 1) * sStride + x];
							byte x8 = pSource[(y + 1) * sStride + x + 1];

							max = (x1 > x2) ? x1 : x2;
							int max2 = ((x3 > x4) ? x3 : x4);
							int max3 = ((x5 > x6) ? x5 : x6);
							int max4 = ((x7 > x8) ? x7 : x8);

							if (max > max2)
							{
								if (max > max3)
								{
									if (max < max4)
										max = max4;
								}
								else
									max = (max3 > max4) ? max3 : max4;
							}
							else
							{
								if (max2 > max3)
									max = (max2 > max4) ? max2 : max4;
								else
									max = (max3 > max4) ? max3 : max4;
							}

							if (max - pSource[(y) * sStride + x] > 0)
								pResult[y * rStride + x] = (byte)(max - pSource[(y) * sStride + x]);
						}
					}

					//top
					y = 0;

					for (x = 1; x < clipWidth; x++)
					{
						deltaMin = 0;

						if (deltaMin < (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]);

						pResult[y * rStride + x] = (byte)deltaMin;
					}

					//left
					x = 0;
					for (y = 1; y < clipHeight; y++)
					{
						deltaMin = 0;

						if (deltaMin < (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]);

						pResult[y * rStride + x] = (byte)deltaMin;
					}

					//bottom
					y = clipHeight;

					for (x = 1; x < clipWidth; x++)
					{
						deltaMin = 0;

						if (deltaMin < (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]);

						pResult[y * rStride + x] = (byte)deltaMin;
					}

					//right
					x = clipWidth;
					for (y = 1; y < clipHeight; y++)
					{
						deltaMin = 0;

						if (deltaMin < (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]);

						pResult[y * rStride + x] = (byte)deltaMin;
					}
				}

				result.Palette = ImageProcessing.Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
				if (source.HorizontalResolution > 0 && source.VerticalResolution > 0)
					result.SetResolution(source.HorizontalResolution, source.VerticalResolution);
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

		#region GetEdgeMap()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
/*		internal static Bitmap GetEdgeMap(Bitmap source)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				result = new Bitmap(source.Width, source.Height, PixelFormat.Format8bppIndexed);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				int clipHeight = result.Height - 1;
				int clipWidth = result.Width - 1;
				int x, y;
				int deltaMin, max;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					for (y = 1; y < clipHeight; y++)
					{
						for (x = 1; x < clipWidth; x++)
						{														
							byte x1 = pSource[(y - 1) * sStride + x - 1];
							byte x2 = pSource[(y - 1) * sStride + x];
							byte x3 = pSource[(y - 1) * sStride + x + 1];
							byte x4 = pSource[(y) * sStride + x - 1];
							byte x5 = pSource[(y) * sStride + x + 1];
							byte x6 = pSource[(y + 1) * sStride + x - 1];
							byte x7 = pSource[(y + 1) * sStride + x];
							byte x8 = pSource[(y + 1) * sStride + x + 1];

							if (x1 > x2)
							{
								if (x1 > x3)
								{
									if (x1 > x4)
									{
										if (x1 > x5)
										{
											if (x1 > x6)
											{
												if (x1 > x7)
												{
													if (x1 > x8)
														max =  x1;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
											else
											{
												if (x6 > x7)
												{
													if (x6 > x8)
														max =  x6;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
										}
										else
										{
											if (x5 > x6)
											{
												if (x5 > x7)
												{
													if (x5 > x8)
														max =  x5;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
											else
											{
												if (x6 > x7)
												{
													if (x6 > x8)
														max =  x6;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
										}
									}
									else
									{
										if (x4 > x5)
										{
											if (x4 > x6)
											{
												if (x4 > x7)
												{
													if (x4 > x8)
														max =  x4;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
											else
											{
												if (x6 > x7)
												{
													if (x6 > x8)
														max =  x6;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
										}
										else
										{
											if (x5 > x6)
											{
												if (x5 > x7)
												{
													if (x5 > x8)
														max =  x5;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
											else
											{
												if (x6 > x7)
												{
													if (x6 > x8)
														max =  x6;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
										}
									}
								}
								else
								{
									if (x3 > x4)
									{
										if (x3 > x5)
										{
											if (x3 > x6)
											{
												if (x3 > x7)
												{
													if (x3 > x8)
														max =  x3;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
											else
											{
												if (x6 > x7)
												{
													if (x6 > x8)
														max =  x6;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
										}
										else
										{
											if (x5 > x6)
											{
												if (x5 > x7)
												{
													if (x5 > x8)
														max =  x5;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
											else
											{
												if (x6 > x7)
												{
													if (x6 > x8)
														max =  x6;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
										}
									}
									else
									{
										if (x4 > x5)
										{
											if (x4 > x6)
											{
												if (x4 > x7)
												{
													if (x4 > x8)
														max =  x4;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
											else
											{
												if (x6 > x7)
												{
													if (x6 > x8)
														max =  x6;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
										}
										else
										{
											if (x5 > x6)
											{
												if (x5 > x7)
												{
													if (x5 > x8)
														max =  x5;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
											else
											{
												if (x6 > x7)
												{
													if (x6 > x8)
														max =  x6;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
										}
									}
								}
							}
							else
							{
								if (x2 > x3)
								{
									if (x2 > x4)
									{
										if (x2 > x5)
										{
											if (x2 > x6)
											{
												if (x2 > x7)
												{
													if (x2 > x8)
														max =  x2;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
											else
											{
												if (x6 > x7)
												{
													if (x6 > x8)
														max =  x6;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
										}
										else
										{
											if (x5 > x6)
											{
												if (x5 > x7)
												{
													if (x5 > x8)
														max =  x5;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
											else
											{
												if (x6 > x7)
												{
													if (x6 > x8)
														max =  x6;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
										}
									}
									else
									{
										if (x4 > x5)
										{
											if (x4 > x6)
											{
												if (x4 > x7)
												{
													if (x4 > x8)
														max =  x4;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
											else
											{
												if (x6 > x7)
												{
													if (x6 > x8)
														max =  x6;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
										}
										else
										{
											if (x5 > x6)
											{
												if (x5 > x7)
												{
													if (x5 > x8)
														max =  x5;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
											else
											{
												if (x6 > x7)
												{
													if (x6 > x8)
														max =  x6;
													else
														max =  x8;
												}
												else
												{
													if (x7 > x8)
														max =  x7;
													else
														max =  x8;
												}
											}
										}
									}
								}
								else
								{
									{
										if (x3 > x4)
										{
											if (x3 > x5)
											{
												if (x3 > x6)
												{
													if (x3 > x7)
													{
														if (x3 > x8)
															max =  x3;
														else
															max =  x8;
													}
													else
													{
														if (x7 > x8)
															max =  x7;
														else
															max =  x8;
													}
												}
												else
												{
													if (x6 > x7)
													{
														if (x6 > x8)
															max =  x6;
														else
															max =  x8;
													}
													else
													{
														if (x7 > x8)
															max =  x7;
														else
															max =  x8;
													}
												}
											}
											else
											{
												if (x5 > x6)
												{
													if (x5 > x7)
													{
														if (x5 > x8)
															max =  x5;
														else
															max =  x8;
													}
													else
													{
														if (x7 > x8)
															max =  x7;
														else
															max =  x8;
													}
												}
												else
												{
													if (x6 > x7)
													{
														if (x6 > x8)
															max =  x6;
														else
															max =  x8;
													}
													else
													{
														if (x7 > x8)
															max =  x7;
														else
															max =  x8;
													}
												}
											}
										}
										else
										{
											if (x4 > x5)
											{
												if (x4 > x6)
												{
													if (x4 > x7)
													{
														if (x4 > x8)
															max =  x4;
														else
															max =  x8;
													}
													else
													{
														if (x7 > x8)
															max =  x7;
														else
															max =  x8;
													}
												}
												else
												{
													if (x6 > x7)
													{
														if (x6 > x8)
															max =  x6;
														else
															max =  x8;
													}
													else
													{
														if (x7 > x8)
															max =  x7;
														else
															max =  x8;
													}
												}
											}
											else
											{
												if (x5 > x6)
												{
													if (x5 > x7)
													{
														if (x5 > x8)
															max =  x5;
														else
															max =  x8;
													}
													else
													{
														if (x7 > x8)
															max =  x7;
														else
															max =  x8;
													}
												}
												else
												{
													if (x6 > x7)
													{
														if (x6 > x8)
															max =  x6;
														else
															max =  x8;
													}
													else
													{
														if (x7 > x8)
															max =  x7;
														else
															max =  x8;
													}
												}
											}
										}
									}
								}
							}

							if (max - pSource[(y) * sStride + x] > 0)
								pResult[y * rStride + x] = (byte) (max - pSource[(y) * sStride + x]);
						}
					}

					//top
					y = 0;

					for (x = 1; x < clipWidth; x++)
					{
						deltaMin = 0;

						if (deltaMin < (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]);

						pResult[y * rStride + x] = (byte)deltaMin;
					}

					//left
					x = 0;
					for (y = 1; y < clipHeight; y++)
					{
						deltaMin = 0;

						if (deltaMin < (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]);

						pResult[y * rStride + x] = (byte)deltaMin;
					}

					//bottom
					y = clipHeight;

					for (x = 1; x < clipWidth; x++)
					{
						deltaMin = 0;

						if (deltaMin < (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]);

						pResult[y * rStride + x] = (byte)deltaMin;
					}

					//right
					x = clipWidth;
					for (y = 1; y < clipHeight; y++)
					{
						deltaMin = 0;

						if (deltaMin < (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]);

						pResult[y * rStride + x] = (byte)deltaMin;
					}
				}

				result.Palette = Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
				if (source.HorizontalResolution > 0 && source.VerticalResolution > 0)
					result.SetResolution(source.HorizontalResolution, source.VerticalResolution);
				return result;
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}*/
		#endregion
	
		#region GetEdgeMap()
		/*internal static Bitmap GetEdgeMap(Bitmap source)
		{
			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;

			try
			{
				result = new Bitmap(source.Width, source.Height, PixelFormat.Format8bppIndexed);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				int clipHeight = result.Height - 1;
				int clipWidth = result.Width - 1;
				int x, y;
				int deltaMin;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();

					for (y = 1; y < clipHeight; y++)
					{
						for (x = 1; x < clipWidth; x++)
						{
							deltaMin = 0;

							if (deltaMin < (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
								deltaMin = (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
							if (deltaMin < (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]))
								deltaMin = (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]);
							if (deltaMin < (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
								deltaMin = (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]);
							if (deltaMin < (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]))
								deltaMin = (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]);
							if (deltaMin < (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]))
								deltaMin = (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]);
							if (deltaMin < (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
								deltaMin = (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
							if (deltaMin < (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]))
								deltaMin = (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]);
							if (deltaMin < (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
								deltaMin = (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]);

							pResult[y * rStride + x] = (byte)deltaMin;
						}
					}

					//top
					y = 0;

					for (x = 1; x < clipWidth; x++)
					{
						deltaMin = 0;

						if (deltaMin < (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]);

						pResult[y * rStride + x] = (byte)deltaMin;
					}

					//left
					x = 0;
					for (y = 1; y < clipHeight; y++)
					{
						deltaMin = 0;

						if (deltaMin < (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x + 1] - pSource[(y) * sStride + x]);

						pResult[y * rStride + x] = (byte)deltaMin;
					}

					//bottom
					y = clipHeight;

					for (x = 1; x < clipWidth; x++)
					{
						deltaMin = 0;

						if (deltaMin < (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x + 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x + 1] - pSource[(y) * sStride + x]);

						pResult[y * rStride + x] = (byte)deltaMin;
					}

					//right
					x = clipWidth;
					for (y = 1; y < clipHeight; y++)
					{
						deltaMin = 0;

						if (deltaMin < (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y - 1) * sStride + x] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x - 1] - pSource[(y) * sStride + x]);
						if (deltaMin < (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]))
							deltaMin = (pSource[(y + 1) * sStride + x] - pSource[(y) * sStride + x]);

						pResult[y * rStride + x] = (byte)deltaMin;
					}
				}

				result.Palette = Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
				if (source.HorizontalResolution > 0 && source.VerticalResolution > 0)
					result.SetResolution(source.HorizontalResolution, source.VerticalResolution);
				return result;
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
				if (result != null && resultData != null)
					result.UnlockBits(resultData);
			}
		}*/
		#endregion

		#region SmoothEdges()
		/// <summary>
		/// Eliminates edges roughness. It computes roughness in 10 rows and lowers edges by average edge value in the area.
		/// </summary>
		/// <param name="source"></param>
		internal static void SmoothEdges(Bitmap source)
		{
			BitmapData sourceData = null;
			double[] rowsRoughness = new double[source.Height];

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

				int sStride = sourceData.Stride;
				int height = source.Height;
				int width = source.Width;
				int x, y;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();

					for (y = 0; y < height; y++)
						for (x = 0; x < width; x++)
							rowsRoughness[y] += pSource[y * sStride + x];

					for (y = 0; y < height; y++)
						rowsRoughness[y] = rowsRoughness[y] / width;
				}

				double[] rowsSmoothness = new double[source.Height];

				rowsSmoothness[0] = (rowsRoughness[0] + rowsRoughness[1] + rowsRoughness[2] + rowsRoughness[3] + rowsRoughness[4]) / 5;
				rowsSmoothness[1] = (rowsRoughness[0] + rowsRoughness[1] + rowsRoughness[2] + rowsRoughness[3] + rowsRoughness[4] + rowsRoughness[5]) / 6;
				rowsSmoothness[2] = (rowsRoughness[0] + rowsRoughness[1] + rowsRoughness[2] + rowsRoughness[3] + rowsRoughness[4] + rowsRoughness[5] + rowsRoughness[6]) / 7;
				rowsSmoothness[3] = (rowsRoughness[0] + rowsRoughness[1] + rowsRoughness[2] + rowsRoughness[3] + rowsRoughness[4] + rowsRoughness[5] + rowsRoughness[6] + rowsRoughness[7]) / 8;
				rowsSmoothness[rowsSmoothness.Length - 1] = (rowsRoughness[rowsSmoothness.Length - 1] + rowsRoughness[rowsSmoothness.Length - 2] + rowsRoughness[rowsSmoothness.Length - 3] + rowsRoughness[rowsSmoothness.Length - 4] + rowsRoughness[rowsSmoothness.Length - 5]) / 5;
				rowsSmoothness[rowsSmoothness.Length - 2] = (rowsRoughness[rowsSmoothness.Length - 1] + rowsRoughness[rowsSmoothness.Length - 2] + rowsRoughness[rowsSmoothness.Length - 3] + rowsRoughness[rowsSmoothness.Length - 4] + rowsRoughness[rowsSmoothness.Length - 5] + rowsRoughness[rowsSmoothness.Length - 6]) / 6;
				rowsSmoothness[rowsSmoothness.Length - 3] = (rowsRoughness[rowsSmoothness.Length - 1] + rowsRoughness[rowsSmoothness.Length - 2] + rowsRoughness[rowsSmoothness.Length - 3] + rowsRoughness[rowsSmoothness.Length - 4] + rowsRoughness[rowsSmoothness.Length - 5] + rowsRoughness[rowsSmoothness.Length - 6] + rowsRoughness[rowsSmoothness.Length - 7]) / 7;
				rowsSmoothness[rowsSmoothness.Length - 4] = (rowsRoughness[rowsSmoothness.Length - 1] + rowsRoughness[rowsSmoothness.Length - 2] + rowsRoughness[rowsSmoothness.Length - 3] + rowsRoughness[rowsSmoothness.Length - 4] + rowsRoughness[rowsSmoothness.Length - 5] + rowsRoughness[rowsSmoothness.Length - 6] + rowsRoughness[rowsSmoothness.Length - 7] + rowsRoughness[rowsSmoothness.Length - 8]) / 8;

				for (y = 4; y < rowsSmoothness.Length - 4; y++)
					rowsSmoothness[y] = (rowsRoughness[y - 4] + rowsRoughness[y - 3] + rowsRoughness[y - 2] + rowsRoughness[y - 1] + rowsRoughness[y] + rowsRoughness[y + 1] + rowsRoughness[y + 2] + rowsRoughness[y + 3] + rowsRoughness[y + 4]) / 9;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();

					for (y = 0; y < height; y++)
						for (x = 0; x < width; x++)
						{
							if (pSource[y * sStride + x] > rowsSmoothness[y])
								pSource[y * sStride + x] = (byte)(pSource[y * sStride + x] - rowsSmoothness[y]);
							else
								pSource[y * sStride + x] = 0;
							
							//pSource[y * sStride + x] = (byte)(Math.Max(0, pSource[y * sStride + x] - rowsSmoothness[y]));
						}
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
			}
		}
		#endregion

		#region DespeckleEdges1x1()
		/// <summary>
		/// If edge is bigger than edges in neighbour, it adjusts that edge
		/// </summary>
		/// <param name="source"></param>
		internal static void DespeckleEdges1x1(Bitmap source)
		{
			BitmapData sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

				int stride = sourceData.Stride;
				int height = source.Height;
				int width = source.Width;
				int x, y;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();

					for (y = 1; y < height - 1; y++)
						for (x = 1; x < width - 1; x++)
						{
							if (pSource[y * stride + x] >= 10)
							{
								byte max = 0;

								if (max < pSource[(y - 1) * stride + (x - 1)])
									max = pSource[(y - 1) * stride + (x - 1)];
								if (max < pSource[(y - 1) * stride + (x)])
									max = pSource[(y - 1) * stride + (x)];
								if (max < pSource[(y - 1) * stride + (x + 1)])
									max = pSource[(y - 1) * stride + (x + 1)];
								if (max < pSource[(y) * stride + (x - 1)])
									max = pSource[(y) * stride + (x - 1)];
								if (max < pSource[(y) * stride + (x + 1)])
									max = pSource[(y) * stride + (x + 1)];
								if (max < pSource[(y + 1) * stride + (x + 1)])
									max = pSource[(y + 1) * stride + (x + 1)];
								if (max < pSource[(y + 1) * stride + (x + 1)])
									max = pSource[(y + 1) * stride + (x + 1)];
								if (max < pSource[(y + 1) * stride + (x + 1)])
									max = pSource[(y + 1) * stride + (x + 1)];

								if (pSource[y * stride + x] > max)
									pSource[y * stride + x] = max;
							}
						}
				}

			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
			}
		}
		#endregion

		#region MergeColorAndEdges()
		/*internal static unsafe Bitmap MergeColorAndEdges(Bitmap color, Bitmap edges)
		{
			int width = color.Width;
			int height = color.Height;

			Bitmap merge = null;
			BitmapData mergeData = null;
			BitmapData colorData = null;
			BitmapData edgesData = null;

			HistogramGrayscale histogram = new HistogramGrayscale(edges);
			int edgeFloor = 0;
			int edgeCeiling = 255;
			int colorFloor = 120;
			int colorCeiling = 255;
			double colorCeilingMinusColorFloor = colorCeiling - colorFloor;
			double edgeCeilingMinusEdgeFloor = edgeCeiling - edgeFloor;

			try
			{
				merge = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				merge.Palette = Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
				mergeData = merge.LockBits(new Rectangle(0, 0, merge.Width, merge.Height), ImageLockMode.ReadOnly, merge.PixelFormat);
				colorData = color.LockBits(new Rectangle(0, 0, color.Width, color.Height), ImageLockMode.ReadOnly, color.PixelFormat);
				edgesData = edges.LockBits(new Rectangle(0, 0, edges.Width, edges.Height), ImageLockMode.ReadOnly, edges.PixelFormat);

				byte* pSourceM = (byte*)mergeData.Scan0.ToPointer();
				byte* pSourceC = (byte*)colorData.Scan0.ToPointer();
				byte* pSourceE = (byte*)edgesData.Scan0.ToPointer();

				int strideM = mergeData.Stride;
				int strideC = colorData.Stride;
				int strideE = edgesData.Stride;

				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						byte cC = pSourceC[y * strideC + x];
						byte cE = pSourceE[y * strideE + x];
						double c, e;

						if (cC <= 120)
							c = 0;
						else if (cC >= 255)
							c = 1;
						else
							c = ((cC - 120) / colorCeilingMinusColorFloor);

						e = 1.0 - (cE / 255.0);

						pSourceM[y * strideM + x] = (byte)(255 * (1.0 - c * e));
					}
				}

				if (color.HorizontalResolution > 0 && color.VerticalResolution > 0)
					merge.SetResolution(color.HorizontalResolution, color.VerticalResolution);

				return merge;
			}
			finally
			{
				if (mergeData != null)
					merge.UnlockBits(mergeData);
				if (colorData != null)
					color.UnlockBits(colorData);
				if (edgesData != null)
					edges.UnlockBits(edgesData);
			}
		}*/
		#endregion

		#region MergeAutoColorAndEdges()
		internal static unsafe Bitmap MergeAutoColorAndEdges(Bitmap color, Bitmap edges)
		{
			int min, max;
			ImageProcessing.BigImages.ContentLocator.GetMinAndMax(color, out min, out max);
			double ratio = 256.0 / (max - min);

			int width = color.Width;
			int height = color.Height;

			Bitmap merge = null;
			BitmapData mergeData = null;
			BitmapData colorData = null;
			BitmapData edgesData = null;

			ImageProcessing.HistogramGrayscale histogram = new ImageProcessing.HistogramGrayscale(edges);
			int edgeFloor = 0;
			int edgeCeiling = 255;
			int colorFloor = 120;
			int colorCeiling = 255;
			double colorCeilingMinusColorFloor = colorCeiling - colorFloor;
			double edgeCeilingMinusEdgeFloor = edgeCeiling - edgeFloor;

			try
			{
				merge = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				merge.Palette = ImageProcessing.Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
				mergeData = merge.LockBits(new Rectangle(0, 0, merge.Width, merge.Height), ImageLockMode.ReadOnly, merge.PixelFormat);
				colorData = color.LockBits(new Rectangle(0, 0, color.Width, color.Height), ImageLockMode.ReadOnly, color.PixelFormat);
				edgesData = edges.LockBits(new Rectangle(0, 0, edges.Width, edges.Height), ImageLockMode.ReadOnly, edges.PixelFormat);

				byte* pSourceM = (byte*)mergeData.Scan0.ToPointer();
				byte* pSourceC = (byte*)colorData.Scan0.ToPointer();
				byte* pSourceE = (byte*)edgesData.Scan0.ToPointer();

				byte* pCurrentM, pCurrentC;

				int strideM = mergeData.Stride;
				int strideC = colorData.Stride;
				int strideE = edgesData.Stride;
						
				double c, e;
				byte cC;

				for (int y = 0; y < height; y++)
				{
					pCurrentC = pSourceC + y * strideC;
					pCurrentM = pSourceM + y * strideM;

					for (int x = 0; x < width; x++)
					{
						cC = pCurrentC[x];
						e = 1.0 - (pSourceE[y * strideE + x] / 255.0);

						if (cC <= min)
							cC = 0;
						else if (cC >= max)
							cC = 255;
						else
							cC = (byte)((cC - min) * ratio);
						
						if (cC <= 120)
							c = 0;
						else if (cC >= 255)
							c = 1;
						else
							c = ((cC - 120) / colorCeilingMinusColorFloor);
						
						pCurrentM[x] = (byte)(255 * (1.0 - c * e));
					}
				}

				if (color.HorizontalResolution > 0 && color.VerticalResolution > 0)
					merge.SetResolution(color.HorizontalResolution, color.VerticalResolution);

				return merge;
			}
			finally
			{
				if (mergeData != null)
					merge.UnlockBits(mergeData);
				if (colorData != null)
					color.UnlockBits(colorData);
				if (edgesData != null)
					edges.UnlockBits(edgesData);
			}
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region GetReducedGrayImage()
		private unsafe Bitmap GetReducedGrayImage(ImageProcessing.BigImages.ItDecoder itDecoder)
		{
			Bitmap source = null;
			Bitmap resized = null;

			double zoom = 100.0 / (double) Convert.ToInt32(itDecoder.DpiX);
			int stripHeightMax = Misc.GetStripHeightMax(itDecoder);

			if (stripHeightMax < itDecoder.Height)
			{
				List<Bitmap> bitmapsToMerge = new List<Bitmap>();

				for (int stripY = 0; stripY < itDecoder.Height; stripY = stripY + stripHeightMax)
				{
					try
					{
						int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - stripY);

						source = itDecoder.GetClip(new Rectangle(0, stripY, itDecoder.Width, stripHeight));
						resized = ImageProcessing.Resizing.Resize(source, Rectangle.Empty, zoom, ImageProcessing.Resizing.ResizeMode.Fast);

						source.Dispose();
						source = null;
						Bitmap resampled = ImageProcessing.Resampling.Resample(resized, PixelsFormat.Format8bppGray);

						resized.Dispose();
						resized = null;

						bitmapsToMerge.Add(resampled);

						if (ProgressChanged != null)
							ProgressChanged((stripY + stripHeight) / (float)itDecoder.Height);
					}
					finally
					{
						if (source != null)
						{
							source.Dispose();
							source = null;
						}
						itDecoder.ReleaseAllocatedMemory(source);
						if (resized != null)
						{
							resized.Dispose();
							resized = null;
						}
					}
				}

				return ImageProcessing.Merging.MergeVertically(bitmapsToMerge);
			}
			else
			{
				try
				{
					source = itDecoder.GetClip(new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));
					resized = ImageProcessing.Resizing.Resize(source, Rectangle.Empty, zoom, ImageProcessing.Resizing.ResizeMode.Fast);

					source.Dispose();
					source = null;
					Bitmap resampled = ImageProcessing.Resampling.Resample(resized, PixelsFormat.Format8bppGray);

					resized.Dispose();
					resized = null;

					if (ProgressChanged != null)
						ProgressChanged(1);

					return resampled;
				}
				finally
				{
					if (source != null)
					{
						source.Dispose();
						source = null;
					}
					itDecoder.ReleaseAllocatedMemory(source);
					if (resized != null)
					{
						resized.Dispose();
						resized = null;
					}
				}
			}
		}
		#endregion

		#region GetBitmapsMask()
		private static unsafe byte[,] GetBitmapsMask(Bitmap reducedBmp, Bitmap edgesBmp)
		{
			int width = reducedBmp.Width / 4;
			int height = reducedBmp.Height / 4;
			
			//int width = (int)Math.Ceiling(reducedBmp.Width / 4.0);
			//int height = (int)Math.Ceiling(reducedBmp.Height / 4.0);

			byte[,] mask = GetEdgeBitmapMask(edgesBmp);
			Bitmap reducedAutoColors = AutoColors(reducedBmp);

#if SAVE_RESULTS
			DrawToFile(Debug.SaveToDir + "806 edge mask.png", mask);
			reducedAutoColors.Save(Debug.SaveToDir + "807 reduced auto colors.png", ImageFormat.Png);
#endif

			BitmapData reducedData = null;

			try
			{
				reducedData = reducedAutoColors.LockBits(new Rectangle(0, 0, reducedAutoColors.Width, reducedAutoColors.Height), ImageLockMode.ReadOnly, reducedAutoColors.PixelFormat);

				byte* pSource = (byte*)reducedData.Scan0.ToPointer();
				int stride = reducedData.Stride;

				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						int max1 = 255, max2 = 255, max3 = 255, max4 = 255;
						byte g;

						for (int j = 0; j < 4; j++)
							for (int i = 0; i < 4; i++)
							{
								g = pSource[(y * 4 + j) * stride + (x * 4 + i)];

								if (g < max1)
								{
									max4 = max3;
									max3 = max2;
									max2 = max1;
									max1 = g;
								}
								else if (g < max2)
								{
									max4 = max3;
									max3 = max2;
									max2 = g;
								}
								else if (g < max3)
								{
									max4 = max3;
									max3 = g;
								}
								else if (g < max4)
								{
									max4 = g;
								}
							}

						int val = (byte)((max1 + max2 + max3 + max4) / 4);

						if (val < 140)
							mask[y, x] = 255;
						else
							mask[y, x] = (byte)Math.Min(255, mask[y, x] + 255 - val);
					}
				}
			}
			finally
			{
				if (reducedData != null)
					reducedAutoColors.UnlockBits(reducedData);
			}

			reducedAutoColors.Dispose();

#if SAVE_RESULTS
			DrawToFile(Debug.SaveToDir + "809 mask.png", mask); 
#endif

			return mask;
		}
		#endregion

		#region GetEdgeBitmapMask()
		private static unsafe byte[,] GetEdgeBitmapMask(Bitmap edgeBmp)
		{
			int width = edgeBmp.Width / 4;
			int height = edgeBmp.Height / 4;

			//int width = (int)Math.Ceiling(reducedBmp.Width / 4.0);
			//int height = (int)Math.Ceiling(reducedBmp.Height / 4.0);

			byte[,] mask = new byte[height, width];

			BitmapData edgeData = null;

			try
			{
				edgeData = edgeBmp.LockBits(new Rectangle(0, 0, edgeBmp.Width, edgeBmp.Height), ImageLockMode.ReadOnly, edgeBmp.PixelFormat);

				byte* pSource = (byte*)edgeData.Scan0.ToPointer();
				int stride = edgeData.Stride;

				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						int max1 = 0, max2 = 0, max3 = 0, max4 = 0;
						byte g;

						for (int j = 0; j < 4; j++)
							for (int i = 0; i < 4; i++)
							{
								g = pSource[(y * 4 + j) * stride + (x * 4 + i)];

								if (g > max1)
								{
									max4 = max3;
									max3 = max2;
									max2 = max1;
									max1 = g;
								}
								else if (g > max2)
								{
									max4 = max3;
									max3 = max2;
									max2 = g;
								}
								else if (g > max3)
								{
									max4 = max3;
									max3 = g;
								}
								else if (g > max4)
								{
									max4 = g;
								}
							}

						int val = (byte)((max1 + max2 + max3 + max4) / 4);

						if (val > 0)
							mask[y, x] = (byte)val;
					}
				}

			}
			finally
			{
				if (edgeData != null)
					edgeBmp.UnlockBits(edgeData);
			}

			return mask;
		}
		#endregion

		#region DrawToFile()
		private static unsafe void DrawToFile(string file, byte[,] mask)
		{
			int width = mask.GetLength(1);
			int height = mask.GetLength(0);

			Bitmap bitmap = null; 
			BitmapData bitmapData = null;

			try
			{
				bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
				int stride = bitmapData.Stride;

				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						pSource[y * stride + x] = mask[y, x];
					}
				}

			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			bitmap.Palette = ImageProcessing.Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
			bitmap.Save(file, ImageFormat.Png);
			bitmap.Dispose();
		}

		private static unsafe void DrawToFile(string file, bool[,] mask)
		{
			int width = mask.GetLength(1);
			int height = mask.GetLength(0);

			Bitmap bitmap = null;
			BitmapData bitmapData = null;

			try
			{
				bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
				bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

				byte* pSource = (byte*)bitmapData.Scan0.ToPointer();
				int stride = bitmapData.Stride;

				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						if(mask[y, x])
							pSource[y * stride + x] = 255;
					}
				}

			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			bitmap.Palette = ImageProcessing.Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
			bitmap.Save(file, ImageFormat.Png);
			bitmap.Dispose();
		}
		#endregion

		#region GetBookContent()
		private static DocumentContent GetBookContent(byte[,] mask)
		{
			DocumentContent documentContent = null;
			Point pTmiddle, pBmiddle;
			bool[,] borderMask = GetBorderMask(mask);

			if (FindBookfold(mask, out pTmiddle, out pBmiddle))
			{
				int width = mask.GetLength(1);
				int height = mask.GetLength(0);
				//byte[,] maskL = MakeCopy(mask, 0, Math.Max(pT.X, pB.X));
				//byte[,] maskR = MakeCopy(mask, Math.Min(pT.X, pB.X), width);

				/*#if SAVE_RESULTS
								DrawToFile(Debug.SaveToDir + "810 book left mask.png", maskL);
								DrawToFile(Debug.SaveToDir + "811 book right mask.png", maskR);
				#endif*/

				Point pUL, pUR, pLL, pLR;
				Point pTleft, pBleft, pTright, pBright;

				GetCorners(borderMask, out pUL, out pUR, out pLL, out pLR);

				if (FindLeftSize(mask, borderMask, out pTleft, out pBleft))
				{
				}
				else
				{
					pTleft = pUL;
					pBleft = pLL;
				}

				if (FindRightSize(mask, borderMask, out pTright, out pBright))
				{
				}
				else
				{
					pTright = pUL;
					pBright = pLL;
				}

				List<Point> tBorderPoints = GetTopContentPoints(mask, borderMask, pTleft, pBleft, pTmiddle, pBmiddle, pTright, pBright);
				List<Point> bBorderPoints = GetBottomContentPoints(mask, borderMask, pTleft, pBleft, pTmiddle, pBmiddle, pTright, pBright);

				CutOffFans(mask, borderMask, pTleft, pBleft, pTmiddle, pBmiddle, pTright, pBright);
				CutOffTopAndBottom(mask, borderMask, tBorderPoints, bBorderPoints);

#if SAVE_RESULTS
				DrawToFile(Debug.SaveToDir + "811 book mask fans cut off.png", mask);
				DrawToFile(Debug.SaveToDir + "812 book border fans cut off.png", borderMask);
#endif

				documentContent = new DocumentContent(mask, borderMask, pTmiddle, pBmiddle, 25);
			}
			else
			{
				documentContent = new DocumentContent(mask, borderMask, 25);
			}

			return documentContent;
		}
		#endregion

		#region GetDocumentContent()
		private static DocumentContent GetDocumentContent(byte[,] mask)
		{
			//Point pUL, pUR, pLL, pLR;
			bool[,] borderMask = GetBorderMask(mask);
			//Point pTleft, pBleft, pTright, pBright;

			/*GetCorners(borderMask, out pUL, out pUR, out pLL, out pLR);

			if (FindLeftSize(mask, borderMask, out pTleft, out pBleft))
			{
			}
			else
			{
				pTleft = pUL;
				pBleft = pLL;
			}

			if (FindRightSize(mask, borderMask, out pTright, out pBright))
			{
			}
			else
			{
				pTright = pUL;
				pBright = pLL;
			}*/

			return new DocumentContent(mask, borderMask, 25);
		}
		#endregion

		#region FindBookfold()
		/// <summary>
		/// 1) it selects area 60 pixels wide (dpi is 25) and for each column, it computes sum of values between borders
		/// 2) it selects column with the smallest average value 
		/// 3) for best column, it computes best angle
		/// </summary>
		/// <param name="mask"></param>
		/// <param name="pT"></param>
		/// <param name="pB"></param>
		/// <returns></returns>
		private static unsafe bool FindBookfold(byte[,] mask, out Point pT, out Point pB)
		{
			int			width = mask.GetLength(1);
			int			height = mask.GetLength(0);
			int			searchWidth = Math.Min(60, width);
			int			left = (width - searchWidth) / 2;
			double[]	columns = new double[searchWidth];
			bool[,]		borderMask = GetBorderMask(mask);

#if SAVE_RESULTS
			DrawToFile(Debug.SaveToDir + "810 border mask.png", borderMask);
#endif

			pT = Point.Empty;
			pB = Point.Empty;

			// 1)
			for (int x = 0; x < searchWidth; x++)
			{
				int yMin = height / 2;
				int yMax = height / 2;

				while (yMin > 0 && borderMask[yMin, x + left] == false)
					yMin--;
				while (yMax < height && borderMask[yMax, x + left] == false)
					yMax++;

				if (yMax - yMin > height / 2)
				{
					int sum = 0;
					
					for (int y = yMin; y < yMax; y++)
						sum += mask[y, x + left];

					columns[x] = sum / (double)(yMax - yMin);
				}
				else
				{
					columns[x] = 255;
				}
			}

			// 2)
			double min = double.MaxValue;
			int? minIndex = null;

			for (int x = 1; x < searchWidth - 1; x++)
				if ((columns[x - 1] < 255 && columns[x] < 255 && columns[x + 1] < 255) && (min > columns[x - 1] + columns[x] + columns[x + 1]))
				{
					min = columns[x - 1] + columns[x] + columns[x + 1];
					minIndex = x;
				}

			if(minIndex == null)
				return false;

			// 3)
			int bookfoldX = minIndex.Value + left;

			if (searchWidth >= 20)
			{
				double minLineValue = double.MaxValue;

				for (int x = -10; x <= +10; x++)
				{
					Point p1 = GetTopPoint(borderMask, bookfoldX + x, height);
					Point p2 = GetBottomPoint(borderMask, bookfoldX - x, height);

					double? lineValue = GetLineValue(mask, p1, p2, height);

					if (lineValue != null && minLineValue > lineValue)
					{
						minLineValue = lineValue.Value;
						pT = p1;
						pB = p2;
					}
				}

				return (minLineValue < double.MaxValue);
			}
			else
			{
				pT = GetTopPoint(borderMask, bookfoldX, height);
				pB = GetBottomPoint(borderMask, bookfoldX, height);
				return true;
			}
		}
		#endregion

		#region GetTopPoint()
		/// <summary>
		/// returns topmost point in column that is not black
		/// </summary>
		/// <param name="mask"></param>
		/// <param name="x"></param>
		/// <param name="height"></param>
		/// <returns></returns>
		private static Point GetTopPoint(bool[,] borderMask, int x, int height)
		{
			int y = height / 2;

			while (y >= 0 && borderMask[y, x] == false)
				y--;

			return new Point(x, y + 1);
		}
		#endregion

		#region GetBottomPoint()
		/// <summary>
		/// returns bottommost point in column that is not black
		/// </summary>
		/// <param name="mask"></param>
		/// <param name="x"></param>
		/// <param name="height"></param>
		/// <returns></returns>
		private static Point GetBottomPoint(bool[,] mask, int x, int height)
		{
			int y = height / 2;

			while (y < height && mask[y, x] == false)
				y++;

			return new Point(x, y - 1);
		}
		#endregion

		#region GetLineValue()
		/// <summary>
		/// returns vertical line average. Returns null if there is 255 in the line or vertical distance to between p1 and p2 i less than 2/3 of the height.
		/// </summary>
		/// <param name="mask"></param>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		private static double? GetLineValue(byte[,] mask, Point p1, Point p2, int height)
		{
			if (p2.Y - p1.Y > height * 0.66)
			{
				int sum = 0;
				
				for (int y = p1.Y; y <= p2.Y; y++)
				{
					int x = p1.X + (p2.X - p1.X) * (y - p1.Y) / (p2.Y - p1.Y);

					if (mask[y, x] == 255)
						return null;

					sum += mask[y, x];
				}

				return sum / (double)(p2.Y - p1.Y + 1);
			}
			else
				return null;
		}
		#endregion

		#region GetBorderMask()
		/// <summary>
		/// Returns array of booleans. Border point is point from mask that is connected to the border and its value is 255.
		/// Value false means point is not connected to the border, true means it is.
		/// </summary>
		/// <param name="mask"></param>
		/// <returns></returns>
		private static bool[,] GetBorderMask(byte[,] mask)
		{
#if DEBUG
			//DateTime start = DateTime.Now;
#endif
			
			int width = mask.GetLength(1);
			int height = mask.GetLength(0);

			bool[,] array = new bool[height, width];
			List<Point> pointsToCheck = new List<Point>();
			int x, y;

			for (x = 0; x < width; x++)
			{
				y = 0;
				while ( y < height && mask[y, x] == 255)
				{
					array[y, x] = true;
					pointsToCheck.Add(new Point(x,y));
					y++;
				}

				y = height - 1;
				while (y >= 0 && mask[y, x] == 255)
				{
					if (array[y, x] == false)
					{
						array[y, x] = true;
						pointsToCheck.Add(new Point(x, y));
					}
					
					y--;
				}
			}

			for (y = 0; y < height; y++)
			{
				x = 0;
				while (x < width && mask[y, x] == 255)
				{
					if (array[y, x] == false)
					{
						array[y, x] = true;
						pointsToCheck.Add(new Point(x, y));
					}

					x++;
				}

				x = width - 1;
				while (x >= 0 && mask[y, x] == 255)
				{
					if (array[y, x] == false)
					{
						array[y, x] = true;
						pointsToCheck.Add(new Point(x, y));
					}

					x--;
				}
			}

			while (pointsToCheck.Count > 0)
			{
				Point p = pointsToCheck[pointsToCheck.Count - 1];
				pointsToCheck.RemoveAt(pointsToCheck.Count - 1);

				if (p.X > 0 && p.Y > 0 && array[p.Y - 1, p.X - 1] == false && mask[p.Y - 1, p.X - 1] == 255)
				{
					array[p.Y - 1, p.X - 1] = true;
					pointsToCheck.Add(new Point(p.X - 1, p.Y - 1));
				}
				if (p.Y > 0 && array[p.Y - 1, p.X] == false && mask[p.Y - 1, p.X] == 255)
				{
					array[p.Y - 1, p.X] = true;
					pointsToCheck.Add(new Point(p.X, p.Y - 1));
				}
				if (p.X < width - 1 && p.Y > 0 && array[p.Y - 1, p.X + 1] == false && mask[p.Y - 1, p.X + 1] == 255)
				{
					array[p.Y - 1, p.X + 1] = true;
					pointsToCheck.Add(new Point(p.X + 1, p.Y - 1));
				}
				if (p.X > 0 && array[p.Y, p.X - 1] == false && mask[p.Y, p.X - 1] == 255)
				{
					array[p.Y, p.X - 1] = true;
					pointsToCheck.Add(new Point(p.X - 1, p.Y));
				}
				if (p.X < width - 1 && array[p.Y, p.X + 1] == false && mask[p.Y, p.X + 1] == 255)
				{
					array[p.Y, p.X + 1] = true;
					pointsToCheck.Add(new Point(p.X + 1, p.Y));
				}
				if (p.X > 0 && p.Y < height - 1 && array[p.Y + 1, p.X - 1] == false && mask[p.Y + 1, p.X - 1] == 255)
				{
					array[p.Y + 1, p.X - 1] = true;
					pointsToCheck.Add(new Point(p.X - 1, p.Y + 1));
				}
				if (p.Y < height - 1 && array[p.Y + 1, p.X] == false && mask[p.Y + 1, p.X] == 255)
				{
					array[p.Y + 1, p.X] = true;
					pointsToCheck.Add(new Point(p.X, p.Y + 1));
				}
				if (p.X < width - 1 && p.Y < height - 1 && array[p.Y + 1, p.X + 1] == false && mask[p.Y + 1, p.X + 1] == 255)
				{
					array[p.Y + 1, p.X + 1] = true;
					pointsToCheck.Add(new Point(p.X + 1, p.Y + 1));
				}
			}

#if DEBUG
			//Console.WriteLine("GetBorderMask(): " + DateTime.Now.Subtract(start).ToString());
#endif
			return array;
		}
		#endregion

		#region GetCorners()
		private static void GetCorners(bool[,] borderMask, out Point pUL, out Point pUR, out Point pLL, out Point pLR)
		{
			int width = borderMask.GetLength(1);
			int height = borderMask.GetLength(0);
			int x, y, i;
			bool brk = false;

			pUL = new Point(0, 0);
			pUR = new Point(width, 0);
			pLL = new Point(0, height);
			pLR = new Point(width, height);

			// upper left
			brk = false;
			for (i = 0; i < width && i < height && brk == false; i++)
			{
				for (x = i, y = 0; x >= 0 && y < height; x--, y++)
				{
					if (borderMask[y, x] == false)
					{
						pUL = new Point(x, y);
						brk = true;
						break;
					}
				}
			}

			// upper right
			brk = false;
			for (i = 0; i < width && i < height && brk == false; i++)
			{
				for (x = width - i - 1, y = 0; x < width && y < height; x++, y++)
				{
					if (borderMask[y, x] == false)
					{
						pUR = new Point(x, y);
						brk = true;
						break;
					}
				}
			}

			// lower left
			brk = false;
			for (i = 0; i < width && i < height && brk == false; i++)
			{
				for (x = i, y = height - 1; x >= 0 && y >= 0; x--, y--)
				{
					if (borderMask[y, x] == false)
					{
						pLL = new Point(x, y);
						brk = true;
						break;
					}
				}
			}

			// lower right
			brk = false;
			for (i = 0; i < width && i < height && brk == false; i++)
			{
				for (x = width - i - 1, y = height - 1; x < width && y >= 0; x++, y--)
				{
					if (borderMask[y, x] == false)
					{
						pLR = new Point(x, y);
						brk = true;
						break;
					}
				}
			}
		}
		#endregion

		#region FindLeftSize()
		private static unsafe bool FindLeftSize(byte[,] mask, bool[,] borderMask, out Point pT, out Point pB)
		{
			bool found = false;
			double bestAverage = double.MaxValue;

			pT = Point.Empty;
			pB = Point.Empty;

			for (int i = -12; i <= 12; i++)
			{
				Point p1, p2;
				double average;

				if (FindLeftSizeSkewed(mask, borderMask, i, out p1, out p2, out average))
				{
					found = true;

					if (bestAverage > average)
					{
						bestAverage = average;
						pT = p1;
						pB = p2;
					}
				}
			}

			return found;
		}
		#endregion

		#region FindRightSize()
		private static unsafe bool FindRightSize(byte[,] mask, bool[,] borderMask, out Point pT, out Point pB)
		{
			bool found = false;
			double bestAverage = double.MaxValue;

			pT = Point.Empty;
			pB = Point.Empty;

			for (int i = -12; i <= 12; i++)
			{
				Point p1, p2;
				double average;

				if (FindRightSizeSkewed(mask, borderMask, i, out p1, out p2, out average))
				{
					found = true;

					if (bestAverage > average)
					{
						bestAverage = average;
						pT = p1;
						pB = p2;
					}
				}
			}

			return found;
		}
		#endregion

		#region FindLeftSizeSkewed()
		/// <summary>
		/// 1) it computes column averages
		/// 2) it finds least 12 averages in a row
		/// </summary>
		/// <param name="mask"></param>
		/// <param name="borderMask"></param>
		/// <param name="skewInPixels"></param>
		/// <param name="pT"></param>
		/// <param name="pB"></param>
		/// <param name="bestAverage"></param>
		/// <returns></returns>
		private static unsafe bool FindLeftSizeSkewed(byte[,] mask, bool[,] borderMask, int skewInPixels, out Point pT, out Point pB, out double bestAverage)
		{
			int width = mask.GetLength(1);
			int height = mask.GetLength(0);
			int searchWidth = width / 3;
			double[] columns = new double[searchWidth];
			int left = Math.Max(0, - skewInPixels);

			pT = Point.Empty;
			pB = Point.Empty;
			bestAverage = double.MaxValue;

			// 1)
			for (int x = 0; x < searchWidth; x++)
			{
				Point p1 = GetTopPoint(borderMask, x + left, height);
				Point p2 = GetBottomPoint(borderMask, x + left + skewInPixels, height);

				double? lineValue = GetLineValue(mask, p1, p2, height);

				if (lineValue != null)
					columns[x] = lineValue.Value;
				else
					columns[x] = 255;
			}

			// 2)
			double min = double.MaxValue;
			int? minIndex = null;

			for (int x = 6; x < searchWidth - 6; x++)
			{
				bool ok = true;
				double sum = 0;

				for (int i = -6; i <= 6; i++)
				{
					if (columns[x + i] == 255)
					{
						ok = false;
						break;
					}
					else
						sum += columns[x + i];
				}

				sum = sum / 13.0;

				if (ok && (min > sum))
				{
					min = sum;
					minIndex = x;
				}
			}

			if (minIndex == null)
			{
				return false;
			}
			else
			{
				bestAverage = min;
				pT = GetTopPoint(borderMask, minIndex.Value + left, height);
				pB = GetBottomPoint(borderMask, minIndex.Value + left + skewInPixels, height);
				return true;
			}
		}
		#endregion

		#region FindRightSizeSkewed()
		/// <summary>
		/// 1) it computes column averages
		/// 2) it finds least 12 averages in a row
		/// </summary>
		/// <param name="mask"></param>
		/// <param name="borderMask"></param>
		/// <param name="skewInPixels"></param>
		/// <param name="pT"></param>
		/// <param name="pB"></param>
		/// <param name="bestAverage"></param>
		/// <returns></returns>
		private static unsafe bool FindRightSizeSkewed(byte[,] mask, bool[,] borderMask, int skewInPixels, out Point pT, out Point pB, out double bestAverage)
		{
			int width = mask.GetLength(1);
			int height = mask.GetLength(0);
			int searchWidth = width / 3;
			double?[] columns = new double?[searchWidth];
			int left = Math.Min(width - 1 - searchWidth, width - 1 - searchWidth - skewInPixels);

			pT = Point.Empty;
			pB = Point.Empty;
			bestAverage = double.MaxValue;

			// 1)
			for (int x = 0; x < searchWidth; x++)
			{
				Point p1 = GetTopPoint(borderMask, x + left, height);
				Point p2 = GetBottomPoint(borderMask, x + left + skewInPixels, height);

				double? lineValue = GetLineValue(mask, p1, p2, height);

				if (lineValue != null)
					columns[x] = lineValue.Value;
			}

			// 2)
			double min = double.MaxValue;
			int? minIndex = null;

			for (int x = 6; x < searchWidth - 6; x++)
			{
				bool ok = true;
				double sum = 0;

				for (int i = -6; i <= 6; i++)
				{
					if (columns[x + i] == null)
					{
						ok = false;
						break;
					}
					else
						sum += columns[x + i].Value;
				}

				sum = sum / 13.0;

				if (ok && (min > sum))
				{
					min = sum;
					minIndex = x;
				}
			}

			if (minIndex == null)
			{
				return false;
			}
			else
			{
				bestAverage = min;
				pT = GetTopPoint(borderMask, minIndex.Value + left, height);
				pB = GetBottomPoint(borderMask, minIndex.Value + left + skewInPixels, height);
				return true;
			}
		}
		#endregion

		#region CutOffFans()
		/// <summary>
		/// it cuts left and right side of mask and borderMask of
		/// </summary>
		/// <param name="mask"></param>
		/// <param name="borderMask"></param>
		/// <param name="pTleft"></param>
		/// <param name="pBleft"></param>
		/// <param name="pTmiddle"></param>
		/// <param name="pBmiddle"></param>
		/// <param name="pTright"></param>
		/// <param name="pBright"></param>
		private static void CutOffFans(byte[,] mask, bool[,] borderMask, Point pTleft, Point pBleft, Point pTmiddle, Point pBmiddle, Point pTright, Point pBright)
		{
			int width = mask.GetLength(1);
			int height = mask.GetLength(0);

			// upper left corner
			for (int y = pTleft.Y; y >= 0; y--)
			{
				for (int x = pTleft.X; x >= 0; x--)
				{
					mask[y, x] = 255;
					borderMask[y, x] = true;
				}
			}

			// lower left corner
			for (int y = pBleft.Y; y < height; y++)
			{
				for (int x = pBleft.X; x >= 0; x--)
				{
					mask[y, x] = 255;
					borderMask[y, x] = true;
				}
			}

			//left side
			for (int y = pTleft.Y + 1; y < pBleft.Y && y < height; y++)
			{
				// get x
				int xL = pTleft.X + (pBleft.X - pTleft.X) * (y - pTleft.Y) / (pBleft.Y - pTleft.Y);

				for (int x = 0; x <= xL; x++)
				{
					mask[y, x] = 255;
					borderMask[y, x] = true;
				}
			}

			// upper right corner
			for (int y = pTright.Y; y >= 0; y--)
			{
				for(int x = pTright.X; x < width; x++)
				{
					mask[y, x] = 255;
					borderMask[y, x] = true;
				}
			}

			// lower right corner
			for (int y = pBright.Y; y < height; y++)
			{
				for (int x = pBright.X; x < width; x++)
				{
					mask[y, x] = 255;
					borderMask[y, x] = true;
				}
			}

			// right side
			for (int y = pTright.Y + 1; y < pBright.Y; y++)
			{
				int xR = pTright.X + (pBright.X - pTright.X) * (y - pTright.Y) / (pBright.Y - pTright.Y);

				for(int x = width - 1; x >= xR; x--)
				{
					mask[y, x] = 255;
					borderMask[y, x] = true;
				}
			}
		}
		#endregion

		#region CutOffTopAndBottom()
		/// <summary>
		/// it cuts top and bottom of mask and borderMask
		/// </summary>
		/// <param name="mask"></param>
		/// <param name="borderMask"></param>
		/// <param name="pTleft"></param>
		/// <param name="pBleft"></param>
		/// <param name="pTmiddle"></param>
		/// <param name="pBmiddle"></param>
		/// <param name="pTright"></param>
		/// <param name="pBright"></param>
		private static void CutOffTopAndBottom(byte[,] mask, bool[,] borderMask, List<Point> pointsT, List<Point> pointsB)
		{
			int width = mask.GetLength(1);
			int height = mask.GetLength(0);

			// top border
			for (int i = 1; i < pointsT.Count; i++)
			{			
				for (int x = pointsT[i - 1].X; x <= pointsT[i].X; x++)
				{
					int yMax = pointsT[i - 1].Y + (pointsT[i].Y - pointsT[i - 1].Y) * (x - pointsT[i - 1].X) / (pointsT[i].X - pointsT[i - 1].X);

					for (int y = 0; y <= yMax; y++)
					{
						mask[y, x] = 255;
						borderMask[y, x] = true;
					}
				}
			}

			// bottom border
			for (int i = 1; i < pointsB.Count; i++)
			{
				for (int x = pointsB[i - 1].X; x <= pointsB[i].X; x++)
				{
					int yMax = pointsB[i - 1].Y + (pointsB[i].Y - pointsB[i - 1].Y) * (x - pointsB[i - 1].X) / (pointsB[i].X - pointsB[i - 1].X);

					for (int y = yMax; y < height; y++)
					{
						mask[y, x] = 255;
						borderMask[y, x] = true;
					}
				}
			}
		}
		#endregion

		#region GetTopContentPoints()
		private static List<Point> GetTopContentPoints(byte[,] mask, bool[,] borderMask, Point pTleft, Point pBleft, Point pTmiddle, Point pBmiddle, Point pTright, Point pBright)
		{
			int width = mask.GetLength(1);
			int height = mask.GetLength(0);
			List<Point> points = new List<Point>();

			for (int x = pTleft.X + 5; x < pTright.X - 5; x = x + 5)
			{
				int borderY = Math.Max(GetTopPoint(borderMask, x - 1, height).Y, Math.Max(GetTopPoint(borderMask, x, height).Y, GetTopPoint(borderMask, x + 1, height).Y));

				if (borderY < height / 4)
				{
					int bestAverage = mask[borderY, x - 1] + mask[borderY, x] + mask[borderY, x + 1];

					int y = borderY;
					while(y < borderY + 25 && y < height - 1)
					{
						if (bestAverage < mask[y + 1, x - 1] + mask[y + 1, x] + mask[y + 1, x + 1])
						{
							points.Add(new Point(x, y));
							break;
						}

						y++;
						bestAverage = mask[y, x - 1] + mask[y, x] + mask[y, x + 1];
					}
				}
			}

			if (points.Count > 0)
			{
				points.Insert(0, pTleft);
				points.Add(pTright);
			}

			return points;
		}
		#endregion

		#region GetBottomContentPoints()
		private static List<Point> GetBottomContentPoints(byte[,] mask, bool[,] borderMask, Point pTleft, Point pBleft, Point pTmiddle, Point pBmiddle, Point pTright, Point pBright)
		{
			int width = mask.GetLength(1);
			int height = mask.GetLength(0);
			List<Point> points = new List<Point>();

			for (int x = pBleft.X + 5; x < pBright.X - 5; x = x + 5)
			{
				int borderY = Math.Max(GetBottomPoint(borderMask, x - 1, height).Y, Math.Max(GetBottomPoint(borderMask, x, height).Y, GetBottomPoint(borderMask, x + 1, height).Y));

				if (borderY > height * 3 / 4)
				{
					int bestAverage = mask[borderY, x - 1] + mask[borderY, x] + mask[borderY, x + 1];

					int y = borderY;
					while (y > borderY - 25 && y > 0)
					{
						if (bestAverage < mask[y - 1, x - 1] + mask[y - 1, x] + mask[y - 1, x + 1])
						{
							points.Add(new Point(x, y));
							break;
						}

						y--;
						bestAverage = mask[y, x - 1] + mask[y, x] + mask[y, x + 1];
					}
				}
			}

			if (points.Count > 0)
			{
				points.Insert(0, pBleft);
				points.Add(pBright);
			}

			return points;
		}
		#endregion

		#region GetMinAndMax()
		private static void GetMinAndMax(Bitmap source, out int min, out int max)
		{
			BitmapData sourceData = null;

			min = 0;
			max = 255;

			int[] array = new int[256];

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

				int sStride = sourceData.Stride;
				int height = source.Height;
				int width = source.Width;
				int x, y;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();

					for (y = 0; y < height; y++)
						for (x = 0; x < width; x++)
							array[pSource[y * sStride + x]]++;
				}

				for (int i = 0; i < 255; i++)
					if (array[i] > 10)
					{
						min = i;
						break;
					}

				for (int i = 255; i >= 0; i--)
					if (array[i] > 10)
					{
						max = i;
						break;
					}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
			}
		}
		#endregion

		#region AutoColors()
		private static Bitmap AutoColors(Bitmap source)
		{
			int min, max;
			ImageProcessing.BigImages.ContentLocator.GetMinAndMax(source, out min, out max);

			Bitmap result = null;
			BitmapData sourceData = null;
			BitmapData resultData = null;
			double ratio = 256.0 / (max - min);

			try
			{
				result = new Bitmap(source.Width, source.Height, source.PixelFormat);
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.ReadOnly, result.PixelFormat);

				int sStride = sourceData.Stride;
				int rStride = resultData.Stride;
				int height = source.Height;
				int width = source.Width;
				int x, y;

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* pResult = (byte*)resultData.Scan0.ToPointer();
					byte* pCurrentS, pCurrentR;

					for (y = 0; y < height; y++)
					{
						pCurrentS = pSource + y * sStride;
						pCurrentR = pResult + y * rStride;

						for (x = 0; x < width; x++)
						{
							if (pCurrentS[x] <= min)
								pCurrentR[x] = 0;
							else if (pCurrentS[x] >= max)
								pCurrentR[x] = 255;
							else
								pCurrentR[x] = (byte)((pCurrentS[x] - min) * ratio);
						}
					}
				}

				result.Palette = ImageProcessing.Misc.GetGrayscalePalette(PixelFormat.Format8bppIndexed);
				if (source.HorizontalResolution > 0 && source.VerticalResolution > 0)
					result.SetResolution(source.HorizontalResolution, source.VerticalResolution);
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
	
		#endregion

	}
}
