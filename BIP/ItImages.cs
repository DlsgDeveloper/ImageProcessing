using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ImageProcessing
{
	public class ItImages : List<ItImage>
	{
		public delegate void FindStartedHandle();
		public delegate void FindDoneHandle();
		
		public event FindStartedHandle	FindStarted;
		public event FindDoneHandle		FindDone;
		
		public delegate void ProgressChangedHandle(float progress);
		public delegate void ImageStartedHandle(ItImage itImage);
		public delegate void ImageDoneHandle(ItImage itImage);
		public delegate void ImageErrorHandle(ItImage itImage, Exception ex);
		
		public event ProgressChangedHandle	ProgressChanged;
		public event ImageStartedHandle		ImageStarted;
		public event ImageDoneHandle		ImageDone;
		public event ImageErrorHandle		ImageError;
	
		
		public ItImages()
		{
		}

		//PUBLIC METHODS
		#region public methods

		#region Find()
		public void Find(Operations operations)
		{
			if (FindStarted != null)
				FindStarted();
			
			for (int i = 0; i < this.Count; i++)
			{
				ItImage itImage = this[i];
				
				try
				{
					if (ImageStarted != null)
						ImageStarted(itImage);

					if (itImage.IsFixed == false)
					{
						bool disposeBitmapWhenDone = (itImage.BitmapCreated == false);
						itImage.CreateBitmap();

						itImage.Find(operations);

						if (disposeBitmapWhenDone)
							itImage.DisposeBitmap();
					}

					if (ImageDone != null)
						ImageDone(itImage);
				}
				catch(Exception ex)
				{
					if (ImageError != null)
						ImageError(itImage, ex);
				}

				if (ProgressChanged != null)
					ProgressChanged((float)i / this.Count);
			}

			if (FindDone != null)
				FindDone();
		}
		#endregion

		#region MakeClipsSameSize()
		/// <summary>
		/// First, it decides if there should be 1 or 2 pages and adjusts images to be so. 
		/// Then, it figures out the size.
		/// Then it finds best default location and for all pages, that are below or under 
		/// default top, it sets the confidence low.
		/// </summary>
		/// <param name="offsetInPixels"></param>
		public void MakeClipsSameSize(float offsetInInches)
		{
			bool twoPagesArticle = IsTwoPageArticle();
			Size? size = GetArticleContentSize();
			
			if (size.HasValue)
			{
				//make the same number of pages
				//MakeSameNumberOfPages(twoPagesArticle, size.Value);

				double validHeightRange = GetMaxImageHeight() * 0.05;

				if (twoPagesArticle)
				{
					Point? idealLeftPageLocation = IdealLeftPageLocation();
					Point? idealRightPageLocation = IdealRightPageLocation();

					foreach (ItImage itImage in this)
					{
						if (itImage.IsFixed == false && itImage.IsIndependent == false)
						{
							if (itImage.TwoPages)
							{
								if (idealLeftPageLocation.HasValue)
									SetClip(itImage.PageL, new Rectangle(idealLeftPageLocation.Value, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								else
								{
									itImage.Page.Clip.ClipConfidence = 0.0F;
									SetClip(itImage.PageL, new Rectangle(itImage.PageL.ClipRect.Location, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								}

								if (idealRightPageLocation.HasValue)
									SetClip(itImage.PageR, new Rectangle(idealRightPageLocation.Value, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								else
								{
									itImage.PageR.Clip.ClipConfidence = 0.0F;
									SetClip(itImage.PageR, new Rectangle(itImage.PageR.ClipRect.Location, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								}
							}
							else
							{
								//page is on the left side
								if (itImage.GetRightPage() != null && idealRightPageLocation.HasValue)
										SetClip(itImage.PageL, new Rectangle(idealRightPageLocation.Value, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								else if (idealLeftPageLocation.HasValue)
									SetClip(itImage.PageL, new Rectangle(idealRightPageLocation.Value, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								else
								{
									itImage.Page.Clip.ClipConfidence = 0.0F;
									SetClip(itImage.PageL, new Rectangle(itImage.PageL.ClipRect.Location, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								}
							}
						}
					}
				}
				else
				{
					Point? idealPageLocation = IdealLeftPageLocation();

					foreach (ItImage itImage in this)
					{
						if (itImage.IsFixed == false && itImage.IsIndependent == false)
						{
							if (idealPageLocation.HasValue)
								SetClip(itImage.Page, new Rectangle(idealPageLocation.Value, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
							else
								itImage.Page.Clip.ClipConfidence = 0.0F;
						}
					}
				}
			}
		}
		#endregion

		#region MakeDependantClipsSameSize()
		public List<ItImage> MakeDependantClipsSameSize(float offsetInInches, Size size)
		{
			bool twoPagesArticle = IsTwoPageArticle();

			//make the same number of pages
			List<ItImage> exceptions = CheckIfImagesCanBeDependent(twoPagesArticle, size);

			double validHeightRange = GetMaxDependantImageHeight() * 0.05;

			if (twoPagesArticle)
			{
				Point? idealLeftPageLocation = IdealLeftPageLocation();
				Point? idealRightPageLocation = IdealRightPageLocation();

				foreach (ItImage itImage in this)
				{
					if (itImage.IsFixed == false && itImage.IsIndependent == false)
					{
						if (itImage.TwoPages)
						{
							if (idealLeftPageLocation.HasValue)
								SetClip(itImage.PageL, new Rectangle(idealLeftPageLocation.Value, size), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
							else
							{
								itImage.Page.Clip.ClipConfidence = 0.0F;
								SetClip(itImage.PageL, new Rectangle(itImage.PageL.ClipRect.Location, size), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
							}

							if (idealRightPageLocation.HasValue)
								SetClip(itImage.PageR, new Rectangle(idealRightPageLocation.Value, size), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
							else
							{
								itImage.PageR.Clip.ClipConfidence = 0.0F;
								SetClip(itImage.PageR, new Rectangle(itImage.PageR.ClipRect.Location, size), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
							}
						}
						else if (itImage.PageL.ClipSpecified)
						{
							//left clip
							if (itImage.PageL.Clip.Center.X <= itImage.ImageSize.Width / 2)
							{
								if (idealLeftPageLocation.HasValue)
									SetClip(itImage.PageL, new Rectangle(idealLeftPageLocation.Value, size), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								else
								{
									itImage.Page.Clip.ClipConfidence = 0.0F;
									SetClip(itImage.PageL, new Rectangle(itImage.PageL.ClipRect.Location, size), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								}
							}
							else
							{
								if (idealRightPageLocation.HasValue)
									SetClip(itImage.PageL, new Rectangle(idealRightPageLocation.Value, size), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								else
								{
									itImage.PageL.Clip.ClipConfidence = 0.0F;
									SetClip(itImage.PageL, new Rectangle(itImage.PageL.ClipRect.Location, size), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								}
							}
						}

					}
				}
			}
			else
			{
				Point? idealPageLocation = IdealLeftPageLocation();

				foreach (ItImage itImage in this)
				{
					if (itImage.IsFixed == false && itImage.IsIndependent == false)
					{
						if (idealPageLocation.HasValue)
							SetClip(itImage.Page, new Rectangle(idealPageLocation.Value, size), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
						else
						{
							itImage.Page.SetClipSize(size);
							itImage.Page.Clip.ClipConfidence = 0.0F;
						}
					}
				}
			}

			return exceptions;
		}
		#endregion

		#region MakeDependantClipsSameSize()
		/*public List<ItImage> MakeDependantClipsSameSize(float offsetInInches, Size? size)
		{
			bool twoPagesArticle = IsTwoPageArticle();

			if (size.HasValue == false)
				size = GetArticleContentSize(twoPagesArticle);

			if (size.HasValue)
			{
				//make the same number of pages
				List<ItImage> exceptions = CheckIfImagesCanBeDependent(twoPagesArticle, size.Value);

				double validHeightRange = GetMaxDependantImageHeight() * 0.05;

				if (twoPagesArticle)
				{
					Point? idealLeftPageLocation = IdealLeftPageLocation();
					Point? idealRightPageLocation = IdealRightPageLocation();

					foreach (ItImage itImage in this)
					{
						if (itImage.IsFixed == false && itImage.IsIndependent == false)
						{
							if (itImage.TwoPages)
							{
								if (idealLeftPageLocation.HasValue)
									SetClip(itImage.PageL, new Rectangle(idealLeftPageLocation.Value, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								else
								{
									itImage.Page.Clip.ClipConfidence = 0.0F;
									SetClip(itImage.PageL, new Rectangle(itImage.PageL.ClipRect.Location, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								}

								if (idealRightPageLocation.HasValue)
									SetClip(itImage.PageR, new Rectangle(idealRightPageLocation.Value, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								else
								{
									itImage.PageR.Clip.ClipConfidence = 0.0F;
									SetClip(itImage.PageR, new Rectangle(itImage.PageR.ClipRect.Location, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
								}
							}
							else if (itImage.PageL.ClipSpecified)
							{
								//left clip
								if (itImage.PageL.Clip.Center.X <= itImage.ImageSize.Width / 2)
								{
									if (idealLeftPageLocation.HasValue)
										SetClip(itImage.PageL, new Rectangle(idealLeftPageLocation.Value, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
									else
									{
										itImage.Page.Clip.ClipConfidence = 0.0F;
										SetClip(itImage.PageL, new Rectangle(itImage.PageL.ClipRect.Location, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
									}
								}
								else
								{
									if (idealRightPageLocation.HasValue)
										SetClip(itImage.PageL, new Rectangle(idealRightPageLocation.Value, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
									else
									{
										itImage.PageL.Clip.ClipConfidence = 0.0F;
										SetClip(itImage.PageL, new Rectangle(itImage.PageL.ClipRect.Location, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
									}
								}
							}

						}
					}
				}
				else
				{
					Point? idealPageLocation = IdealLeftPageLocation();

					foreach (ItImage itImage in this)
					{
						if (itImage.IsFixed == false && itImage.IsIndependent == false)
						{
							if (idealPageLocation.HasValue)
								SetClip(itImage.Page, new Rectangle(idealPageLocation.Value, size.Value), validHeightRange, Convert.ToInt32(offsetInInches * itImage.ImageInfo.DpiH));
							else
							{
								itImage.Page.SetClipSize(size.Value);
								itImage.Page.Clip.ClipConfidence = 0.0F;
							}
						}
					}
				}

				return exceptions;
			}
			else
			{
				foreach (ItImage itImage in this)
					if (itImage.IsFixed == false && itImage.IsIndependent == false)
					{
						throw new Exception("Can't find correct pages size!");
					}

				return null;
			}
		}*/
		#endregion
	
		#region ChangeClipsSize()
		public void ChangeClipsSize(int dl, int dt, int dr, int db, bool fixedClipSize)
		{
			foreach (ItImage itImage in this)
			{
				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					if (itImage.TwoPages)
					{
						itImage.PageL.SetClip(dl, dt, dr, db, fixedClipSize);
						itImage.PageR.SetClip(dl, dt, dr, db, fixedClipSize);
					}
					else
					{
						if (itImage.Page.ClipSpecified)
							itImage.Page.SetClip(dl, dt, dr, db, fixedClipSize);
					}
				}
			}
		}

		public void ChangeClipsSize(int width, int height, bool fixedClipSize)
		{
			foreach (ItImage itImage in this)
			{
				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					if (itImage.TwoPages)
					{
						itImage.PageL.SetClip(new Rectangle(itImage.PageL.ClipRect.X, itImage.PageL.ClipRect.Y, width, height), fixedClipSize);
						itImage.PageR.SetClip(new Rectangle(itImage.PageR.ClipRect.X, itImage.PageR.ClipRect.Y, width, height), fixedClipSize);
					}
					else
					{
						if (itImage.Page.ClipSpecified)
							itImage.Page.SetClip(new Rectangle(itImage.Page.ClipRect.X, itImage.Page.ClipRect.Y, width, height), fixedClipSize);
					}
				}
			}
		}
		#endregion

		#region MoveClips()
		public void MoveClips(ItImage firstImage, bool leftPage, int x, int y)
		{
			for (int i = (firstImage != null) ? this.IndexOf(firstImage) : 0; i < this.Count; i++)
			{
				ItImage itImage = this[i];

				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					ItPage itPage = (leftPage) ? itImage.GetLeftPage() : itImage.GetRightPage();

					if (itPage != null && itPage.ClipSpecified)
					{
						Rectangle clip = itPage.ClipRect;
						clip.Location = new Point(x, y);
						itPage.SetClip(clip, true);
					}
				}
			}
		}
		#endregion

		#region SkewClips()
		public void SkewClips(ItImage firstImage, bool leftPage, double skew)
		{
			for (int i = (firstImage != null) ? this.IndexOf(firstImage) : 0; i < this.Count; i++)
			{
				ItImage itImage = this[i];

				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					ItPage itPage = (leftPage) ? this[i].GetLeftPage() : this[i].GetRightPage();

					if (itPage != null && itPage.ClipSpecified)					
						itPage.SetSkew(skew, 1.0F);
				}
			}
		}
		#endregion

		#region ResetSettings()
		public void ResetSettings()
		{
			Size? clipSize = GetDependantClipsSize();

			//applying clip
			foreach (ItImage itImage in this)
			{
				if (itImage.IsFixed == false)
				{
					if (itImage.IsIndependent)
					{
						itImage.Reset(itImage.ImageSize.Width >= itImage.ImageSize.Height);
					}
					else
					{
						itImage.Reset(itImage.ImageSize.Width >= itImage.ImageSize.Height);

						if (clipSize.HasValue)
							itImage.SetClipsSize(clipSize.Value);
					}
				}
			}
		}
		#endregion
	
		#region ResetItImage()
		public void ResetItImage(ItImage itImage)
		{
			if (itImage.IsFixed == false)
			{
				if (itImage.IsIndependent)
				{
					itImage.Reset(itImage.ImageSize.Width > itImage.ImageSize.Height);
				}
				else
				{
					int index = this.IndexOf(itImage);

					for (int j = 1; j < Math.Max(index, this.Count - index); j++)
					{
						int i = index - j;
						if (i >= 0 && this[i].IsFixed == false && this[i].IsIndependent == false && this[i].Page.ClipSpecified && this[i].Page.ClipRect.Size != Size.Empty)
						{
							Size size = this[i].Page.ClipRect.Size;

							if (itImage.PageL.ClipSpecified)
								itImage.PageL.SetClipSize(size);
							if (itImage.PageR.ClipSpecified)
								itImage.PageR.SetClipSize(size);
							//itImage.ImportSettings(this[i]);
							return;
						}

						i = index + j;
						if (i < this.Count && this[i].IsFixed == false && this[i].IsIndependent == false && this[i].Page.ClipSpecified && this[i].Page.ClipRect.Size != Size.Empty)
						{
							Size size = this[i].Page.ClipRect.Size;

							if (itImage.PageL.ClipSpecified)
								itImage.PageL.SetClipSize(size);
							if (itImage.PageR.ClipSpecified)
								itImage.PageR.SetClipSize(size);
							//itImage.ImportSettings(this[i]);
							return;
						}
					}

					itImage.Reset(itImage.TwoPages);
				}
			}
		}
		#endregion

		#region GetDependantClipsSize()
		public Size? GetDependantClipsSize()
		{
			foreach (ItImage itImage in this)
				if (itImage.IsFixed == false && itImage.IsIndependent == false && itImage.PageL.ClipSpecified)
					return itImage.PageL.ClipRect.Size;

			return null;
		}
		#endregion

		#region GetArticleContentSize()
		public Size? GetArticleContentSize()
		{
			bool twoPagesArticle = IsTwoPageArticle();
			Size? size = null;

			//getting sizes list
			List<Size> sizes = new List<Size>();

			for (int i = 0; i < this.Count; i++)
			{
				ItImage itImage = this[i];

				//disqualify first and last pages, if different than the rest
				if (twoPagesArticle == false || (i > 0 && i < this.Count - 1) || itImage.ImageSize.Width > itImage.ImageSize.Height)
				{
					if (itImage.IsFixed == false && itImage.IsIndependent == false)
					{
						if (itImage.PageL.Clip.ContentDefined)
							sizes.Add(itImage.PageL.Clip.Content.Size);						
						if (itImage.TwoPages && itImage.PageR.Clip.ContentDefined)
							sizes.Add(itImage.PageR.Clip.Content.Size);
					}
				}
			}

			//computing ideal size
			foreach (Size clipSize in sizes)
			{
				if (size.HasValue == false)
					size = clipSize;
				else
					size = new Size(Math.Max(size.Value.Width, clipSize.Width), Math.Max(size.Value.Height, clipSize.Height));
			}

			return size;
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region MakeSameNumberOfPages()
		private void MakeSameNumberOfPages(bool twoPagesArticle, Size clipSize)
		{
			List<ItImage> exceptions = CheckIfImagesCanBeDependent(twoPagesArticle, clipSize);
			
			if (twoPagesArticle)
			{			
				foreach (ItImage itImage in this)
				{
					if (itImage.IsFixed == false && itImage.TwoPages == false && itImage.IsIndependent == false)
					{
						if (itImage.Page.ClipSpecified == true)
						{
							Rectangle clip = itImage.Page.Clip.RectangleNotSkewed;

							if (clip.Width > itImage.ImageSize.Width / 2.0)
							{
								itImage.SetTo2Clips(new Rectangle(clip.X, clip.Y, clip.Width / 2, clip.Height), new Rectangle(clip.X + clip.Width / 2, clip.Y, clip.Width / 2, clip.Height));
							}
							else
							{
								bool leftClip = ((clip.X + clip.Right) / 2.0) > itImage.ImageSize.Width / 2;
								Rectangle newClip = new Rectangle(itImage.ImageSize.Width - clip.X - clip.Width, clip.Top, clip.Width, clip.Bottom);

								itImage.SetTo2Clips(newClip, leftClip);
							}
						}
						else
						{
							itImage.Reset(true);
						}

						itImage.PageL.Clip.ClipConfidence = 0;
						itImage.PageR.Clip.ClipConfidence = 0;
					}
				}
			}
			else
			{
				foreach (ItImage itImage in this)
				{
					if (itImage.IsFixed == false && itImage.IsIndependent == false && itImage.TwoPages)
					{
						Rectangle newClip;

						if (itImage.PageL.ClipContentSpecified && itImage.PageR.ClipContentSpecified)
							newClip = Rectangle.Union(itImage.PageL.Clip.Content, itImage.PageR.Clip.Content);
						else if (itImage.PageL.ClipContentSpecified && itImage.PageR.ClipSpecified)
							newClip = Rectangle.Union(itImage.PageL.Clip.Content, itImage.PageR.ClipRect);
						else if (itImage.PageL.ClipSpecified && itImage.PageR.ClipContentSpecified)
							newClip = Rectangle.Union(itImage.PageL.ClipRect, itImage.PageR.Clip.Content);
						else
							newClip = Rectangle.Union(itImage.PageL.ClipRect, itImage.PageR.ClipRect);

						itImage.SetTo1Clip(newClip);
						itImage.Page.Clip.ClipConfidence = 0;
					}
				}
			}
		}
		#endregion

		#region CheckDependency()
		private List<ItImage> CheckIfImagesCanBeDependent(bool twoPagesArticle, Size clipSize)
		{
			List<ItImage> exceptions = new List<ItImage>();
			
			if (twoPagesArticle)
			{
				foreach (ItImage itImage in this)
				{
					if (itImage.IsFixed == false && itImage.IsIndependent == false)
					{
						if (itImage.ImageSize.Width < itImage.ImageSize.Height && itImage.ImageSize.Width < clipSize.Width * 2)
						{
							itImage.IsIndependent = true;
							exceptions.Add(itImage);
						}
						else if (itImage.ImageSize.Width < clipSize.Width || itImage.ImageSize.Height < clipSize.Height)
						{
							itImage.IsIndependent = true;
							exceptions.Add(itImage);
						}
						else if (itImage.Page.ClipContentSpecified && (itImage.Page.Clip.Content.Width > clipSize.Width || itImage.Page.Clip.Content.Height > clipSize.Height))
						{
							itImage.IsIndependent = true;
							exceptions.Add(itImage);
						}
						else if (itImage.TwoPages && itImage.PageR.ClipContentSpecified && (itImage.PageR.Clip.Content.Width > clipSize.Width || itImage.PageR.Clip.Content.Height > clipSize.Height))
						{
							itImage.IsIndependent = true;
							exceptions.Add(itImage);
						}
					}
				}
			}
			else
			{
				foreach (ItImage itImage in this)
				{
					if (itImage.IsFixed == false && itImage.IsIndependent == false)
					{
						if (itImage.ImageSize.Width < clipSize.Width || itImage.ImageSize.Height < clipSize.Height)
						{
							itImage.IsIndependent = true;
							exceptions.Add(itImage);
						}
						if (itImage.Page.ClipContentSpecified && (itImage.Page.Clip.Content.Width > clipSize.Width || itImage.Page.Clip.Content.Height > clipSize.Height))
						{
							itImage.IsIndependent = true;
							exceptions.Add(itImage);
						}
						if (itImage.TwoPages && itImage.PageR.ClipContentSpecified && (itImage.PageR.Clip.Content.Width > clipSize.Width || itImage.PageR.Clip.Content.Height > clipSize.Height))
						{
							itImage.IsIndependent = true;
							exceptions.Add(itImage);
						}
					}
				}
			}

			return exceptions;
		}
		#endregion

		#region GetMaxImageHeight()
		double GetMaxImageHeight()
		{
			double maxImageHeight = 0;

			foreach (ItImage itImage in this)
				if (maxImageHeight < itImage.ImageSize.Height)
					maxImageHeight = itImage.ImageSize.Height;

			return maxImageHeight;
		}
		#endregion

		#region GetMaxDependantImageHeight()
		double GetMaxDependantImageHeight()
		{
			double maxImageHeight = 0;

			foreach (ItImage itImage in this)
				if(itImage.IsFixed == false && itImage.IsIndependent == false)
					if (maxImageHeight < itImage.ImageSize.Height)
						maxImageHeight = itImage.ImageSize.Height;

			return maxImageHeight;
		}
		#endregion

		#region IdealLeftPageLocation()
		Point? IdealLeftPageLocation()
		{
			List<int> lefts = new List<int>();
			List<int> tops = new List<int>();

			foreach (ItImage itImage in this)
				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					ItPage itPage = itImage.GetLeftPage();

					if (itPage != null && itPage.Clip.ContentDefined)
					{
						lefts.Add(itPage.Clip.Content.X);
						tops.Add(itPage.Clip.Content.Y);
					}
				}

			double? averageLeft = ImageProcessing.Statistics.GetHarmonicMean(lefts);
			double? averageTop = ImageProcessing.Statistics.GetHarmonicMean(tops);

			if (averageLeft.HasValue && averageTop.HasValue)
				return new Point((int)averageLeft, (int)averageTop);

			return null;
		}
		#endregion

		#region IdealRightPageLocation()
		Point? IdealRightPageLocation()
		{
			List<int> lefts = new List<int>();
			List<int> tops = new List<int>();

			foreach (ItImage itImage in this)
			{
				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					ItPage itPage = itImage.GetRightPage();

					if (itPage != null && itPage.Clip.ContentDefined)
					{
						lefts.Add(itPage.Clip.Content.X);
						tops.Add(itPage.Clip.Content.Y);
					}
				}
			}

			double? averageLeft = ImageProcessing.Statistics.GetHarmonicMean(lefts);
			double? averageTop = ImageProcessing.Statistics.GetHarmonicMean(tops);

			if (averageLeft.HasValue && averageTop.HasValue)
				return new Point((int)averageLeft, (int)averageTop);

			return null;
		}
		#endregion

		#region SetClip()
		void SetClip(ItPage page, Rectangle idealClip, double validHeightRange, int offset)
		{
			Rectangle newClip;
			float confidence = page.Clip.ClipConfidence;

			if (page.Clip.ContentDefined)
			{
				int x = page.Clip.Content.X - (idealClip.Width - page.Clip.Content.Width) / 2;
				int y;

				//is content top close to idealClip top
				if (page.Clip.Content.Y < idealClip.Y - validHeightRange)
				{
					y = page.Clip.Content.Y;
					confidence = 0;
				}
				else if (page.Clip.Content.Y > idealClip.Y + validHeightRange)
				{
					y = idealClip.Y;
					confidence = 0.5F;
				}
				else
				{
					y = page.Clip.Content.Y;
				}

				newClip = new Rectangle(x, y, idealClip.Width, idealClip.Height);
			}
			else
			{
				newClip = idealClip;
				confidence = 0;
			}

			newClip.Inflate(offset, offset);

			page.SetClip(newClip, true);
			page.Clip.ClipConfidence = confidence;
		}
		#endregion

		#region IsTwoPageArticle()
		private bool IsTwoPageArticle()
		{
			int notFixedImagesCount = 0;
			int twoPagesImagesCount = 0;

			//first, count pages with 2 pages or single narrow page
			foreach (ItImage itImage in this)
				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					notFixedImagesCount++;

					if (itImage.TwoPages || ((itImage.Page.ClipSpecified) && (itImage.Page.ClipRect.Width < itImage.ImageInfo.Width / 2) && (itImage.ImageInfo.Width > itImage.ImageInfo.Height)))
						twoPagesImagesCount++;
				}

			//deciding if article images have 2 pages
			return (twoPagesImagesCount * 2.0 > notFixedImagesCount);
		}
		#endregion

		#endregion
	}
}
