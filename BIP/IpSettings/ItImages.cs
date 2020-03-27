using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using BIP.Geometry;



namespace ImageProcessing.IpSettings
{
	public class ItImages : List<ImageProcessing.IpSettings.ItImage>
	{
		public delegate void FindStartedHandle();
		public delegate void FindDoneHandle();
				
		public delegate void ProgressChangedHandle(float progress);
		public delegate void ImageStartedHandle(ImageProcessing.IpSettings.ItImage itImage);
		public delegate void ImageDoneHandle(ImageProcessing.IpSettings.ItImage itImage);
		public delegate void ImageErrorHandle(ImageProcessing.IpSettings.ItImage itImage, Exception ex);
			
		
		public ItImages()
		{
		}

		//PUBLIC METHODS
		#region public methods

		#region MakeDependantClipsSameSize()
		public List<ImageProcessing.IpSettings.ItImage> MakeDependantClipsSameSize(float offsetInch, InchSize size)
		{
			bool twoPagesArticle = IsTwoPageArticle();

			//check if all dependant images are valid
			List<ImageProcessing.IpSettings.ItImage> nonDependentImages = CheckDependency(twoPagesArticle, size);

			double heightRangeInch = GetMaxDependantImageHeightInInches() * 0.05;

			if (twoPagesArticle)
			{
				InchPoint? idealLpLoc = IdealLeftPageLocation();
				InchPoint? idealRpLoc = IdealRightPageLocation();

				foreach (ImageProcessing.IpSettings.ItImage itImage in this)
				{
					if (itImage.IsFixed == false && itImage.IsIndependent == false)
					{
						if (itImage.TwoPages)
						{
							if (idealLpLoc.HasValue)
								SetClip(itImage.PageL, new InchRect(idealLpLoc.Value.X, idealLpLoc.Value.Y, size.Width, size.Height), heightRangeInch, offsetInch);
							else
							{
								itImage.Page.Clip.ClipConfidence = 0.0F;
								SetClip(itImage.PageL, new InchRect(itImage.PageL.ClipRectInch.X, itImage.PageL.ClipRectInch.Y, size.Width, size.Height), heightRangeInch, offsetInch);
							}

							if (idealRpLoc.HasValue)
								SetClip(itImage.PageR, new InchRect(idealRpLoc.Value.X, idealRpLoc.Value.Y, size.Width, size.Height), heightRangeInch, offsetInch);
							else
							{
								itImage.PageR.Clip.ClipConfidence = 0.0F;
								SetClip(itImage.PageR, new InchRect(itImage.PageR.ClipRectInch.X, itImage.PageR.ClipRectInch.Y, size.Width, size.Height), heightRangeInch, offsetInch);
							}
						}
						else if (itImage.PageL.ClipSpecified)
						{
							//left clip
							if (itImage.PageL.Clip.Center.X <= 0.5)
							{
								if (idealLpLoc.HasValue)
									SetClip(itImage.PageL, new InchRect(idealLpLoc.Value.X, idealLpLoc.Value.Y, size.Width, size.Height), heightRangeInch, offsetInch);
								else
								{
									itImage.Page.Clip.ClipConfidence = 0.0F;
									SetClip(itImage.PageL, new InchRect(itImage.PageL.ClipRectInch.X, itImage.PageL.ClipRectInch.Y, size.Width, size.Height), heightRangeInch, offsetInch);
								}
							}
							else
							{
								if (idealRpLoc.HasValue)
									SetClip(itImage.PageL, new InchRect(idealRpLoc.Value.X, idealRpLoc.Value.Y, size.Width, size.Height), heightRangeInch, offsetInch);
								else
								{
									itImage.PageL.Clip.ClipConfidence = 0.0F;
									SetClip(itImage.PageL, new InchRect(itImage.PageL.ClipRectInch.X, itImage.PageL.ClipRectInch.Y, size.Width, size.Height), heightRangeInch, offsetInch);
								}
							}
						}

					}
				}
			}
			else
			{
				InchPoint? idealLoc = IdealLeftPageLocation();

				foreach (ImageProcessing.IpSettings.ItImage itImage in this)
				{
					if (itImage.IsFixed == false && itImage.IsIndependent == false)
					{
						if (idealLoc.HasValue)
							SetClip(itImage.Page, new InchRect(idealLoc.Value.X, idealLoc.Value.Y, size.Width, size.Height), heightRangeInch, offsetInch);
						else
						{
							itImage.Page.Clip.ClipConfidence = 0.0F;
							itImage.Page.SetClipSize(size);
						}

						if (itImage.TwoPages)
						{
							itImage.PageR.Clip.ClipConfidence = 0.0F;
							itImage.PageR.SetClipSize(size);
						}

					}
				}
			}

			return nonDependentImages;
		}
		#endregion
	
		#region ChangeClipsSize()
		public void ChangeClipsSize(double dl, double dt, double dr, double db, bool fixedClipSize)
		{
			foreach (ImageProcessing.IpSettings.ItImage itImage in this)
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

		public void ChangeClipsSizeInch(double dl, double dt, double dr, double db, bool fixedClipSize)
		{
			foreach (ImageProcessing.IpSettings.ItImage itImage in this)
			{
				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					if (itImage.TwoPages)
					{
						itImage.PageL.SetClipInch(dl, dt, dr, db, fixedClipSize);
						itImage.PageR.SetClipInch(dl, dt, dr, db, fixedClipSize);
					}
					else
					{
						if (itImage.Page.ClipSpecified)
							itImage.Page.SetClipInch(dl, dt, dr, db, fixedClipSize);
					}
				}
			}
		}

		public void ChangeClipsSize(InchSize newSize, ResizeDirection direction, bool fixedClipSize)
		{
			foreach (ImageProcessing.IpSettings.ItImage itImage in this)
			{
				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					if (itImage.TwoPages)
					{
						itImage.PageL.SetClip(newSize, direction, fixedClipSize);
						itImage.PageR.SetClip(newSize, direction, fixedClipSize);
					}
					else
					{
						if (itImage.Page.ClipSpecified)
							itImage.Page.SetClip(newSize, direction, fixedClipSize);
					}
				}
			}
		}

		public void ChangeClipsSize(InchSize newSize, ResizeDirection leftPageDirection, ResizeDirection rightPageDirection, bool fixedClipSize)
		{
			foreach (ImageProcessing.IpSettings.ItImage itImage in this)
			{
				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					if (itImage.TwoPages)
					{
						itImage.PageL.SetClip(newSize, leftPageDirection, fixedClipSize);
						itImage.PageR.SetClip(newSize, rightPageDirection, fixedClipSize);
					}
					else
					{
						if (itImage.Page.ClipSpecified)
							itImage.Page.SetClip(newSize, leftPageDirection, fixedClipSize);
					}
				}
			}
		}
		#endregion

		#region MoveClips()
		/*public void MoveClips(ImageProcessing.IpSettings.ItImage firstImage, bool leftPage, RatioSize move)
		{
			for (int i = (firstImage != null) ? this.IndexOf(firstImage) : 0; i < this.Count; i++)
			{
				ImageProcessing.IpSettings.ItImage itImage = this[i];

				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					ImageProcessing.IpSettings.ItPage itPage = (leftPage) ? itImage.GetLeftPage() : itImage.GetRightPage();

					if (itPage != null && itPage.ClipSpecified)
					{					
						RatioRect clip = new RatioRect(move.Height, move.Height, itPage.ClipRect.Width, itPage.ClipRect.Height);

						itPage.SetClip(clip, true);
					}
				}
			}
		}*/
		#endregion

		#region SkewClips()
		public void SkewClips(ImageProcessing.IpSettings.ItImage firstImage, bool leftPage, double skew)
		{
			for (int i = (firstImage != null) ? this.IndexOf(firstImage) : 0; i < this.Count; i++)
			{
				ImageProcessing.IpSettings.ItImage itImage = this[i];

				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					ImageProcessing.IpSettings.ItPage itPage = (leftPage) ? this[i].GetLeftPage() : this[i].GetRightPage();

					if (itPage != null && itPage.ClipSpecified)					
						itPage.SetSkew(skew, 1.0F);
				}
			}
		}
		#endregion

		#region ResetSettings()
		public void ResetSettings()
		{
			InchSize? clipSize = GetDependantClipsSize();

			//applying clip
			foreach (ImageProcessing.IpSettings.ItImage itImage in this)
			{
				if (itImage.IsFixed == false)
				{
					if (itImage.IsIndependent)
					{
						itImage.Reset(itImage.IsLandscapeImage);
					}
					else
					{
						itImage.Reset(itImage.IsLandscapeImage);

						if (clipSize.HasValue)
							itImage.SetClipsSize(clipSize.Value);
					}
				}
			}
		}
		#endregion

		#region ResetToEmptyPages()
		public void ResetToEmptyPages()
		{
			InchSize? clipSize = GetDependantClipsSize();

			//applying clip
			foreach (ImageProcessing.IpSettings.ItImage itImage in this)
			{
				if (itImage.IsFixed == false)
				{
					if (itImage.IsIndependent)
					{
						itImage.Reset(itImage.IsLandscapeImage);
					}
					else
					{
						itImage.Reset(itImage.IsLandscapeImage);

						if (clipSize.HasValue)
							itImage.SetClipsSize(clipSize.Value);
					}
				}
			}
		}
		#endregion

		#region ResetItImage()
		public void ResetItImage(ImageProcessing.IpSettings.ItImage itImage)
		{
			if (itImage.IsFixed == false)
			{
				if (itImage.IsIndependent)
				{
					itImage.Reset(itImage.IsLandscapeImage);
				}
				else
				{
					int index = this.IndexOf(itImage);

					for (int j = 1; j <= Math.Max(index, this.Count - index); j++)
					{
						int i = index - j;
						if (i >= 0 && this[i].IsFixed == false && this[i].IsIndependent == false && this[i].Page.ClipSpecified)
						{
							itImage.ImportSettings(this[i]);
							return;
						}

						i = index + j;
						if (i < this.Count && this[i].IsFixed == false && this[i].IsIndependent == false && this[i].Page.ClipSpecified)
						{
							itImage.ImportSettings(this[i]);
							return;
						}
					}

					if (itImage.TwoPages)
					{
						double width = Math.Max(itImage.PageL.ClipRectInch.Width, itImage.PageR.ClipRectInch.Width);
						double height = Math.Max(itImage.PageL.ClipRectInch.Height, itImage.PageR.ClipRectInch.Height);
						itImage.PageL.SetClipSize(new InchSize(width, height));
						itImage.PageR.SetClipSize(new InchSize(width, height));
					}
					else
					{
						//itImage.Reset(itImage.TwoPages);
					}
				}
			}
		}
		#endregion

		#region MakeItImageDependant()
		public void MakeItImageDependant(ImageProcessing.IpSettings.ItImage itImage)
		{
			if (itImage.IsFixed == false)
			{
				itImage.IsIndependent = false;
				int index = this.IndexOf(itImage);

				BIP.Geometry.InchSize? clipSize = this.GetDependantClipsSize();

					if (clipSize == null && itImage.TwoPages)
					{
						double width = Math.Max(itImage.PageL.ClipRectInch.Width, itImage.PageR.ClipRectInch.Width);
						double height = Math.Max(itImage.PageL.ClipRectInch.Height, itImage.PageR.ClipRectInch.Height);

							clipSize = new BIP.Geometry.InchSize(width, height);
					}
	
				if (clipSize != null)
				{
					if(itImage.PageL.IsActive)
						itImage.PageL.SetClipSize(clipSize.Value);
					if (itImage.PageR.IsActive)
						itImage.PageR.SetClipSize(clipSize.Value);
				}
			}
		}
		#endregion

		#region GetDependantClipsSize()
		public InchSize? GetDependantClipsSize()
		{
			foreach (ImageProcessing.IpSettings.ItImage itImage in this)
				if (itImage.IsFixed == false && itImage.IsIndependent == false && itImage.PageL.ClipSpecified)
					return itImage.PageL.ClipRectInch.Size;

			return null;
		}
		#endregion

		#region GetArticleContentSizeInInches()
		/// <summary>
		/// returns biggest page size of all pages
		/// </summary>
		/// <returns></returns>
		public InchSize? GetArticleMaxContentSizeInInches()
		{
			return GetArticleMaxContentSizeInInches(IsTwoPageArticle());
			//return GetArticleContentSizeInInches(IsTwoPageArticle());
		}
		#endregion

		#region GetArticleMedianContentSizeInInches()
		/// <summary>
		/// returns width and heights, that is 80% bigges of all widths and heights
		/// </summary>
		/// <returns></returns>
		public InchSize? GetArticleMedianContentSizeInInches()
		{
			return GetArticleMedianContentSizeInInches(IsTwoPageArticle());
			//return GetArticleContentSizeInInches(IsTwoPageArticle());
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region IsTwoPageArticle()
		private bool IsTwoPageArticle()
		{
			int notFixedImagesCount = 0;
			int twoPagesImagesCount = 0;

			//first, count pages with 2 pages or single narrow page
			foreach (ImageProcessing.IpSettings.ItImage itImage in this)
				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					notFixedImagesCount++;

					if (itImage.TwoPages || (itImage.Page.ClipSpecified && itImage.Page.ClipRect.Width < 0.5))
						twoPagesImagesCount++;
				}

			//deciding if article images have 2 pages
			return (twoPagesImagesCount * 2.0 > notFixedImagesCount);
		}
		#endregion

		#region GetArticleMaxContentSizeInInches()
		private InchSize? GetArticleMaxContentSizeInInches(bool twoPagesArticle)
		{
			InchSize? size = null;

			//getting sizes list
			List<InchSize> sizes = new List<InchSize>();

			for (int i = 0; i < this.Count; i++)
			{
				ImageProcessing.IpSettings.ItImage itImage = this[i];

				//disqualify first and last pages, if different than the rest
				if (twoPagesArticle == false || (i > 0 && i < this.Count - 1) || itImage.IsLandscapeImage)
				{
					if (itImage.IsFixed == false/* && itImage.IsIndependent == false*/)
					{
						if (itImage.PageL.ClipSpecified)
							sizes.Add(itImage.PageL.ClipRectInch.Size);
						if (itImage.TwoPages && itImage.PageR.ClipSpecified)
							sizes.Add(itImage.PageR.ClipRectInch.Size);
					}
				}
			}

			//computing ideal size
			foreach (InchSize clipSize in sizes)
			{
				if (size.HasValue == false)
					size = clipSize;
				else
					size = new InchSize(Math.Max(size.Value.Width, clipSize.Width), Math.Max(size.Value.Height, clipSize.Height));
			}

			return size;
		}
		#endregion

		#region GetArticleMedianContentSizeInInches()
		private InchSize? GetArticleMedianContentSizeInInches(bool twoPagesArticle)
		{
			//getting sizes list
			List<InchSize> sizes = new List<InchSize>();

			for (int i = 0; i < this.Count; i++)
			{
				ImageProcessing.IpSettings.ItImage itImage = this[i];

				//disqualify first and last pages, if different than the rest
				if (twoPagesArticle == false || (i > 0 && i < this.Count - 1) || itImage.IsLandscapeImage)
				{
					if (itImage.IsFixed == false/* && itImage.IsIndependent == false*/)
					{
						if (itImage.PageL.ClipSpecified)
							sizes.Add(itImage.PageL.ClipRectInch.Size);
						if (itImage.TwoPages && itImage.PageR.ClipSpecified)
							sizes.Add(itImage.PageR.ClipRectInch.Size);
					}
				}
			}

			if (sizes.Count > 0)
			{
				List<double> widths = new List<double>();
				List<double> heights = new List<double>();

				foreach (InchSize inchSize in sizes)
				{
					widths.Add(inchSize.Width);
					heights.Add(inchSize.Height);
				}

				widths.Sort();
				heights.Sort();

				int indexX = Math.Max(0, Math.Min(widths.Count - 1, (int)(widths.Count * 0.8)));
				int indexY = Math.Max(0, Math.Min(heights.Count - 1, (int)(heights.Count * 0.8)));

				return new InchSize(widths[indexX], heights[indexY]);
			}
			else
				return null;
		}
		#endregion

		#region CheckDependency()
		/// <summary>
		/// returns images that can't be dependent
		/// </summary>
		/// <param name="twoPagesArticle"></param>
		/// <param name="clipSizeInInches"></param>
		/// <returns></returns>
		private List<ItImage> CheckDependency(bool twoPagesArticle, InchSize clipSizeInInches)
		{
			List<ItImage> dependentImages = new List<ItImage>();
			
			if (twoPagesArticle)
			{
				foreach (ImageProcessing.IpSettings.ItImage itImage in this)
				{
					if (itImage.IsFixed == false && itImage.IsIndependent == false)
					{
						if (itImage.IsLandscapeImage == false && itImage.InchSize.Width < clipSizeInInches.Width * 2)
						{
							itImage.IsIndependent = true;
							dependentImages.Add(itImage);
						}
						else if (itImage.InchSize.Width < clipSizeInInches.Width || itImage.InchSize.Height < clipSizeInInches.Height)
						{
							itImage.IsIndependent = true;
							dependentImages.Add(itImage);
						}
						else if (itImage.Page.ClipContentSpecified && (itImage.Page.ContentInch.Width > clipSizeInInches.Width || itImage.Page.ContentInch.Height > clipSizeInInches.Height))
						{
							itImage.IsIndependent = true;
							dependentImages.Add(itImage);
						}
						else if (itImage.TwoPages && itImage.PageR.ClipContentSpecified && (itImage.PageR.ContentInch.Width > clipSizeInInches.Width || itImage.PageR.ContentInch.Height > clipSizeInInches.Height))
						{
							itImage.IsIndependent = true;
							dependentImages.Add(itImage);
						}
					}
				}
			}
			else
			{
				foreach (ImageProcessing.IpSettings.ItImage itImage in this)
				{
					if (itImage.IsFixed == false && itImage.IsIndependent == false)
					{
						if (itImage.InchSize.Width < clipSizeInInches.Width || itImage.InchSize.Height < clipSizeInInches.Height)
						{
							itImage.IsIndependent = true;
							dependentImages.Add(itImage);
						}
						if (itImage.Page.ClipContentSpecified && (itImage.Page.ContentInch.Width > clipSizeInInches.Width || itImage.Page.ContentInch.Height > clipSizeInInches.Height))
						{
							itImage.IsIndependent = true;
							dependentImages.Add(itImage);
						}
						if (itImage.TwoPages && itImage.PageR.ClipContentSpecified && (itImage.PageR.ContentInch.Width > clipSizeInInches.Width || itImage.PageR.ContentInch.Height > clipSizeInInches.Height))
						{
							itImage.IsIndependent = true;
							dependentImages.Add(itImage);
						}
					}
				}
			}

			return dependentImages;
		}
		#endregion

		#region GetMaxImageHeight()
		/*double GetMaxImageHeight()
		{
			double maxImageHeight = 0;

			foreach (ImageProcessing.IpSettings.ItImage itImage in this)
				if (maxImageHeight < itImage.ImageSize.Height)
					maxImageHeight = itImage.ImageSize.Height;

			return maxImageHeight;
		}*/
		#endregion

		#region GetMaxDependantImageHeightInInches()
		double GetMaxDependantImageHeightInInches()
		{
			double maxImageHeight = 0;

			foreach (ImageProcessing.IpSettings.ItImage itImage in this)
				if(itImage.IsFixed == false && itImage.IsIndependent == false)
					if (maxImageHeight < itImage.InchSize.Height)
						maxImageHeight = itImage.InchSize.Height;

			return maxImageHeight;
		}
		#endregion

		#region IdealLeftPageLocation()
		InchPoint? IdealLeftPageLocation()
		{
			List<double> lefts = new List<double>();
			List<double> tops = new List<double>();

			foreach (ImageProcessing.IpSettings.ItImage itImage in this)
				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					ImageProcessing.IpSettings.ItPage itPage = itImage.GetLeftPage();

					if (itPage != null && itPage.ClipContentSpecified)
					{
						lefts.Add(itPage.ContentInch.X);
						tops.Add(itPage.ContentInch.Y);
					}
				}

			double? averageLeft = ImageProcessing.Statistics.GetHarmonicMean(lefts);
			double? averageTop = ImageProcessing.Statistics.GetHarmonicMean(tops);

			if (averageLeft.HasValue && averageTop.HasValue)
				return new InchPoint(averageLeft.Value, averageTop.Value);

			return null;
		}
		#endregion

		#region IdealRightPageLocation()
		InchPoint? IdealRightPageLocation()
		{
			List<double> lefts = new List<double>();
			List<double> tops = new List<double>();

			foreach (ImageProcessing.IpSettings.ItImage itImage in this)
			{
				if (itImage.IsFixed == false && itImage.IsIndependent == false)
				{
					ImageProcessing.IpSettings.ItPage itPage = itImage.GetRightPage();

					if (itPage != null && itPage.ClipContentSpecified)
					{
						lefts.Add(itPage.ContentInch.X);
						tops.Add(itPage.ContentInch.Y);
					}
				}
			}

			double? averageLeft = ImageProcessing.Statistics.GetHarmonicMean(lefts);
			double? averageTop = ImageProcessing.Statistics.GetHarmonicMean(tops);

			if (averageLeft.HasValue && averageTop.HasValue)
				return new InchPoint(averageLeft.Value, averageTop.Value);

			return null;
		}
		#endregion

		#region SetClip()
		void SetClip(ImageProcessing.IpSettings.ItPage page, InchRect idealClip, double validHeightRange, double offsetInInches)
		{		
			InchRect newClip;
			float confidence = page.Clip.ClipConfidence;

			if (page.Clip.ContentDefined)
			{
				InchRect contentRect = page.ContentInch;
				
				double x = contentRect.X - (idealClip.Width - contentRect.Width) / 2;
				double y;

				//is content top close to idealClip top
				if (contentRect.Y < idealClip.Y - validHeightRange)
				{
					y = contentRect.Y;
					confidence = 0;
				}
				else if (contentRect.Y > idealClip.Y + validHeightRange)
				{
					y = idealClip.Y;
					confidence = 0.5F;
				}
				else
				{
					y = contentRect.Y;
				}

				newClip = new InchRect(x, y, idealClip.Width, idealClip.Height);
			}
			else
			{
				newClip = idealClip;
				confidence = 0;
			}

			newClip.Inflate(offsetInInches, offsetInInches);

			RatioRect pageClipRatio = new RatioRect(newClip.X / page.ItImage.InchSize.Width, newClip.Y / page.ItImage.InchSize.Height, newClip.Width / page.ItImage.InchSize.Width, newClip.Height / page.ItImage.InchSize.Height);

			page.SetClip(pageClipRatio, true);
			page.Clip.ClipConfidence = confidence;
		}
		#endregion

		#endregion
	}

	#region ResizeDirection
	public enum ResizeDirection
	{
		All,
		NW,
		N,
		NE,
		E,
		SE,
		S,
		SW,
		W
	}
	#endregion

}
