using System;
using System.Drawing;
using System.Collections.Generic;

using ImageProcessing.PageObjects;
using System.Drawing.Imaging;

namespace ImageProcessing
{
	public class ItPage
	{
		PageBookfolding bookfolding;
		Clip			clip;
		Fingers			fingers;
		ItImage			itImage;

		bool locked = false;
		Size minSize = new Size(200,200);

		public delegate void PageChangedHnd(ItPage itPage);
		public delegate void ItPageHnd(ItPage itPage);
		
		public event EventHandler		Invalidated;
		public event PageChangedHnd		PageChanged;
		internal event ItPageHnd		RemoveRequest;


		#region constructor
		public ItPage(ItImage itImage)
			: this(itImage, new Rectangle(Point.Empty, itImage.ImageSize))
		{
		}

		public ItPage(ItImage itImage, Rectangle clip)
		{
			this.itImage = itImage;
			this.clip = new Clip(clip);
			this.bookfolding = new PageBookfolding(this, 1.0F);
			this.fingers = new Fingers();

			this.clip.ClipChanged += new Clip.ClipChangedHnd(Clip_Changed);
			this.fingers.Changed += new ItImage.VoidHnd(Fingers_Changed);
			this.bookfolding.Changed += new ItImage.VoidHnd(Bookfolding_Changed);
		}
		#endregion

		#region PageLayout
		public enum PageLayout
		{
			Left,
			Right,
			SinglePage
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties

		public ItImage		ItImage { get { return this.itImage; } }
		public int			GlobalOpticsCenter { get { return this.itImage.OpticsCenter; }}
		public int			LocalOpticsCenter { get { return ((this.clip != null) ? (this.itImage.OpticsCenter - this.clip.RectangleNotSkewed.Y) : this.itImage.OpticsCenter); } }
		public Rectangle	ImageRect { get { return new Rectangle(Point.Empty, this.ImageSize); } }
		public Size			ImageSize { get { return this.itImage.ImageSize; } }

		public bool				ClipSpecified { get { return ((this.clip.Skew != 0.0) || (this.clip.RectangleNotSkewed.Size != this.ImageSize)); } }
		public bool				ClipContentSpecified { get { return this.clip.ContentDefined; } }
		public Clip				Clip { get { return this.clip; } }
		public Rectangle		ClipRect { get { return this.clip.RectangleNotSkewed; } }
		public double			Skew { get { return this.clip.Skew; } }
		public PageBookfolding	Bookfolding { get { return this.bookfolding; } }
		public Fingers			Fingers { get { return this.fingers; } }

		public bool			IsSkewed { get { return this.clip.IsSkewed; } }
		public bool			IsCurved { get { return ((this.bookfolding != null) ? this.bookfolding.IsCurved : false); } }

		public Symbols		LoneObjects { get { return this.itImage.LoneSymbols.GetObjectsInClip(this.clip.RectangleNotSkewed); } }
		public Delimiters	Delimiters { get { return this.itImage.Delimiters.GetDelimitersInClip(this.clip.RectangleNotSkewed); } }
		//public Paragraphs	Paragraphs { get { return ObjectLocator.FindParagraphs(this.LoneObjects, this.Words); } }
		public Pictures		Pictures { get { return this.itImage.Pictures.GetPicturesInClip(this.clip.RectangleNotSkewed); } }
		public Words		Words { get { return this.itImage.Words.GetWordsInClip(this.clip.RectangleNotSkewed); } }
		public Lines		Lines { get { return this.itImage.Lines.GetLinesInClip(this.clip.RectangleNotSkewed); } }
		public Paragraphs	Paragraphs{get{return this.itImage.Paragraphs.GetParagraphsInClip(this.clip.RectangleNotSkewed); } }
		public int?			ColumnsWidth{get{return GetColumnsWidth();} }

		#region ColumnsCount
		public int ColumnsCount
		{
			get
			{
				int? columnsWidth = ColumnsWidth;

				if (columnsWidth.HasValue)
				{
					int columnsCount = Convert.ToInt32(this.clip.RectangleNotSkewed.Width / (double)columnsWidth);

					if (columnsCount >= 1 && columnsCount <= 4)
						return columnsCount;
				}
				
				return 1;
			}
		}
		#endregion

		#region Layout
		public PageLayout Layout
		{
			get
			{
				if (itImage.TwoPages == false)
					return PageLayout.SinglePage;
				else if (this == itImage.PageR)
					return PageLayout.Right;
				else
					return PageLayout.Left;
			}
		}
		#endregion

		#region Confidence
		public float Confidence
		{
			get
			{
				float confidence =  Math.Min(this.clip.ClipConfidence, this.clip.SkewConfidence);

				if (confidence > this.bookfolding.Confidence)
					confidence = this.bookfolding.Confidence;
				if (confidence > this.fingers.Confidence)
					confidence = this.fingers.Confidence;

				return confidence;
			}
		}
		#endregion

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region Lock()
		public void Lock()
		{
			this.locked = true;
		}
		#endregion

		#region Unlock()
		public void Unlock()
		{
			this.locked = false;

			if (this.ClipRect.Width < minSize.Width || this.ClipRect.Height < minSize.Height)
			{
				if (this.RemoveRequest != null)
					this.RemoveRequest(this);
			}
			else
				this.itImage.ValidatePages();
		}
		#endregion

		#region RaiseRemoveRequest()
		public bool RaiseRemoveRequest()
		{
			if (RemoveRequest != null)
			{
				RemoveRequest(this);
				return true;
			}
			else
				return false;
		}
		#endregion

		#region FindContent()
		/*public Rectangle FindContent(Rectangle clip)
		{
			float confidence = 0f;
			Page page = ObjectLocator.FindPage(this.itImage.LoneSymbols, this.itImage.Paragraphs, this.itImage.Pictures, this.itImage.Delimiters, clip, ref confidence);

			if (page != null)
			{
				Rectangle pageRect = page.Rectangle;
				//pageRect.Inflate(offset, offset);
				this.SetClip(Rectangle.Intersect(clip, pageRect), false);
				this.clip.SetContent(pageRect, confidence);
			}
			else
			{
				this.clip.SetClip(clip);
				this.clip.SetContent(null, 0.0F);
			}

			return this.Clip.RectangleNotSkewed;
		}*/
		#endregion

		#region FindSkew()
		public double FindSkew()
		{
			float confidence;
			double angle = Rotation.GetAngleFromObjects(this, this.Words, this.Pictures, this.Delimiters, out confidence);
			this.SetSkew(angle, confidence);
			
			return angle;
		}
		#endregion

		#region FindCurving()
		public void FindCurving()
		{
			CurveCorrection.FindCuring(this);
			Changed();
		}
		#endregion

		#region FindFingers()
		public void FindFingers(Bitmap raster)
		{
			FingerRemoval.FindFingers(raster, this);
			Changed();
		}

		public Fingers FindFingers(Bitmap original, Paging paging, int minDelta, float percentageWhite)
		{
			byte confidence;
			this.fingers = FingerRemoval.FindFingers(original, this, paging, minDelta, percentageWhite, out confidence);
			Changed();
			return this.fingers;
		}
		#endregion

		//clip
		#region SetClip()
		public void SetClip(Rectangle rect, bool fixedClipSize)
		{
			if (fixedClipSize == false)
			{
				rect = Rectangle.Intersect(rect, ImageRect);
			}
			else
			{
				rect.Width = (rect.Width < ImageSize.Width) ? rect.Width : ImageSize.Width;
				rect.Height = (rect.Height < ImageSize.Height) ? rect.Height : ImageSize.Height;
				rect.X = (rect.X < 0) ? 0 : ((rect.Right > ImageSize.Width) ? ImageSize.Width - rect.Width : rect.X);
				rect.Y = (rect.Y < 0) ? 0 : ((rect.Bottom > ImageSize.Height) ? ImageSize.Height - rect.Height : rect.Y);
			}

			this.clip.SetClip(rect);
			this.fingers.ClipChanged(this.clip);
			this.bookfolding.ClipChanged(this.clip);
			Changed();
		}
		#endregion

		#region SetClip()
		/// <summary>
		/// Inflates clip in x, y, r, b directions. If 'fixedClipSize' is true, clip position 
		/// will be chnged that clip can fit in the image. If false, clip will be inflated to the image edges.
		/// </summary>
		/// <param name="dl">delta left</param>
		/// <param name="dt">delta top</param>
		/// <param name="dr">delta right</param>
		/// <param name="db">delta bottom</param>
		/// <param name="fixedClipSize"></param>
		public void SetClip(int dl, int dt, int dr, int db, bool fixedClipSize)
		{
			if (ClipSpecified)
			{
				Rectangle rect = Rectangle.FromLTRB(this.ClipRect.X + dl, this.ClipRect.Y + dt, this.ClipRect.Right + dr, this.ClipRect.Bottom + db);

				SetClip(rect, fixedClipSize);
			}
		}
		#endregion

		#region SetOffset()
		internal void SetOffset(int pixelsOffset)
		{
			if (this.ClipContentSpecified)
			{
				SetClip(Rectangle.Inflate(this.clip.Content, pixelsOffset, pixelsOffset), false);
			}
		}
		#endregion

		#region SetClipSize()
		public void SetClipSize(Size size)
		{
			Rectangle rect = (this.ClipSpecified) ? this.ClipRect : ImageRect;

			int offsetX = (size.Width - rect.Width) / 2;
			int offsetY = (size.Height - rect.Height) / 2;

			rect.Inflate(offsetX, offsetY);
			
			this.SetClip(rect, true);
		}
		#endregion

		#region ResetClip()
		public void ResetClip()
		{
			this.clip.SetClip(new Rectangle(Point.Empty, this.ImageSize));
			this.clip.SetContent(null, 1.0F);
			this.clip.SetSkew(0.0, 1.0F);
			this.fingers.ClipChanged(this.Clip);
			this.bookfolding.ClipChanged(this.clip);
			Changed();
		}
		#endregion

		//skew
		#region SetSkew()
		public void SetSkew(double skew, float confidence)
		{
			if (this.clip.Skew != skew)
			{
				this.clip.SetSkew(skew, confidence);
				this.bookfolding.ClipChanged(this.clip);
			}
			else if(this.clip.SkewConfidence != confidence)
				this.clip.SetSkew(skew, confidence);

			Changed();
		}
		#endregion

		//bookfold
		#region SetPageBookfolding()
		public void SetPageBookfolding(Curve curveT, Curve curveB, float confidence)
		{
			this.bookfolding.SetCurves(curveT,curveB, confidence);
			Changed();
		}
		#endregion

		#region ChangeBookfoldPoint()
		public bool ChangeBookfoldPoint(Curve curve, int index, Point newPoint)
		{
			bool result = this.bookfolding.SetCurvePoint(curve, index, newPoint);
			Changed();
			return result;
		}
		#endregion

		#region ResetPageBookfolding()
		public bool ResetPageBookfolding()
		{
			if (this.bookfolding.IsCurved)
			{
				this.bookfolding.Reset();
				Changed();
				return true;
			}
			else
			{
				Changed();
				return false;
			}
		}
		#endregion

		#region ShiftBookfoldPoints()
		public bool ShiftBookfoldPoints(Curve curve, int dy)
		{
			try
			{
				return this.bookfolding.ShiftCurve(curve, dy);
			}
			finally
			{
				Changed();
			}
		}
		#endregion

		//fingers
		#region AddFinger()
		public void AddFinger(Finger finger)
		{
			/*if (this.fingers.Count >= 2)
			{
				throw new IpException(ErrorCode.FingersToMany);
			}*/
			
			this.fingers.Add(finger);
			Changed();
		}

		public Finger AddFinger(Rectangle rect)
		{
			foreach (Finger finger in this.fingers)
				if (finger.IsIdentical(rect))
					return null;
			
			Rectangle newFingerRect = Rectangle.Intersect(rect, this.ImageRect);
			
			if (!newFingerRect.IsEmpty)
			{
				Finger finger = new Finger(this, newFingerRect);
				if (finger != null)
				{
					this.fingers.Add(finger);
					Changed();
					return finger;
				}
			}

			return null;
		}
		#endregion

		#region ChangeFingerColor()
		/*public bool ChangeFingerColor(Finger finger, Color color)
		{
			if (this.fingers.Contains(finger))
			{
				finger.Color = color;
				return true;
			}

			return false;
		}*/
		#endregion

		#region ChangeFingerRect()
		/*public bool ChangeFingerRect(Finger finger, Rectangle rect)
		{
			if (this.fingers.Contains(finger))
			{
				finger.SetClip(rect);
				Changed();
				return true;
			}
			
			return false;
		}*/
		#endregion

		#region ClearFingers()
		public bool ClearFingers()
		{
			if ((this.fingers != null) && (this.fingers.Count > 0))
			{
				this.fingers.Clear();
				Changed();
				return true;
			}
			
			return false;
		}
		#endregion

		#region RemoveFinger()
		public bool RemoveFinger(Finger finger)
		{
			if (this.fingers.Contains(finger))
			{
				this.fingers.Remove(finger);
				Changed();
				return true;
			}
		
			return false;
		}

		public Finger RemoveFinger(Point point)
		{
			foreach (Finger finger in this.fingers)
			{
				if (finger.Contains(point))
				{
					this.fingers.Remove(finger);
					Changed();
					return finger;
				}
			}
			
			return null;
		}
		#endregion

		#region GetFinger()
		public Finger GetFinger(Point point)
		{
			foreach (Finger finger in this.fingers)
			{
				if (finger.Contains(point))
				{
					return finger;
				}
			}
			return null;
		}
		#endregion

		//misc
		#region ImportSettings()
		public void ImportSettings(ItPage itPage)
		{
			SetClip(Rectangle.Intersect(itPage.ClipRect, new Rectangle(Point.Empty, itImage.ImageSize)), true);
			SetSkew(itPage.Clip.Skew, itPage.clip.SkewConfidence);

			if (itPage.Clip.ContentDefined)
				clip.SetContent(itPage.Clip.Content, itPage.Clip.ClipConfidence);
			else
				clip.SetContent(null, itPage.Clip.ClipConfidence);

			this.bookfolding.ImportSettings(itPage.bookfolding);
			itPage.ItImage.OpticsCenter = this.GlobalOpticsCenter;

			foreach (Finger finger in this.Fingers)
			{
				Rectangle intersect = Rectangle.Intersect(finger.RectangleNotSkewed, new Rectangle(Point.Empty, itImage.ImageSize));
				
				if(intersect.Width > 50 && intersect.Height > 50)
				itPage.Fingers.Add(new Finger(this, intersect));
			}

		}
		#endregion

		#region Clone()
		internal ItPage Clone()
		{
			ItPage clone = new ItPage(this.ItImage);
			clone.Clip.SetClip(Rectangle.Intersect(this.clip.RectangleNotSkewed, new Rectangle(Point.Empty, itImage.ImageSize)));
			clone.Clip.SetSkew(this.Clip.Skew, this.clip.SkewConfidence);
			clone.SetPageBookfolding(this.bookfolding.TopCurve, this.bookfolding.BottomCurve, this.bookfolding.Confidence);
			clone.ItImage.OpticsCenter = this.GlobalOpticsCenter;
			
			foreach (Finger finger in this.Fingers)
				clone.Fingers.Add(finger.Clone(clone));

			return clone;
		}
		#endregion

		#region Reset()
		public void Reset(Rectangle clip)
		{
			this.fingers.Clear();
			this.bookfolding.Reset();
			
			this.clip.SetClip(clip);
			this.clip.SetContent(null, 1.0F);
			this.clip.SetSkew(0.0, 1.0F);
			this.bookfolding.Reset();
			Changed();
		}
		#endregion

		#region GetResult()
		public Bitmap GetResult(Bitmap source)
		{
			Bitmap result;
			Bitmap copy;
			
			if (source == null)
				throw new IpException(ErrorCode.ErrorNoImageLoaded);

			ItPage page = this;
			
			if (this.Fingers.Count > 0)
			{
				result = ImageCopier.Copy(source);
				FingerRemoval.EraseFingers(result, page.Fingers);

#if SAVE_RESULTS
				try { result.Save(Debug.SaveToDir + "Finger Removal.png", ImageFormat.Png); }
				catch { }
#endif
			}
			else
				result = source;
			
			if (this.bookfolding.IsCurved)
			{
				copy = result;
				
				if (this.IsSkewed)
					result = CurveCorrectionAndRotation.StretchAndRotate(copy, page, 0xff, 0xff, 0xff);
				else
					result = CurveCorrection.GetFromParams(copy, page, 0);
				
				if (copy != source)
					copy.Dispose();
			}
			else if (this.clip.IsSkewed)
			{
				copy = result;
				result = Rotation.RotateClip(result, -page.Clip.Skew, page.Clip.RectangleNotSkewed, 0xff, 0xff, 0xff);
				
				if (copy != source)
					copy.Dispose();
			}
			else if (this.clip.RectangleNotSkewed.Size != source.Size)
			{
				copy = result;
				result = ImageCopier.Copy(copy, page.Clip.RectangleNotSkewed);
				
				if (copy != source)
					copy.Dispose();
			}
			
			if (result == source)
				result = ImageCopier.Copy(source);

			/*if (itImage.PostProcessing != null)
				DoPostProcessing(result, itImage.PostProcessing);*/

			return result;
		}
		#endregion

		#region ResizeSettings()
		public void ResizeSettings(double zoom)
		{
			this.clip.ResizeSettings(zoom);
			this.fingers.ResizeSettings(zoom);
			this.bookfolding.ResizeSettings(zoom);
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region DoPostProcessing()
		/*private void DoPostProcessing(Bitmap image, PostProcessing postProcessing)
		{
			if (postProcessing.BrightnessDelta != 0)
				Brightness.Go(image, postProcessing.BrightnessDelta);

			if (postProcessing.Invert)
				Inverter.Invert(image);
		}*/
		#endregion

		#region GetColumnsWidth()
		private int? GetColumnsWidth()
		{
			List<Paragraph> columnParagraphs = new List<Paragraph>();
			int square = 0;
			int sum = 0;

			foreach (Paragraph paragraph in this.Paragraphs)
				if (paragraph.IsLineParagraph() == false)
				{
					columnParagraphs.Add(paragraph);
					square += paragraph.Width * paragraph.Height;
					sum += paragraph.Height;
				}

			if (columnParagraphs.Count > 0 && sum > 0)
			{
				double averageWidth = square / sum;

				for (int i = columnParagraphs.Count - 1; i >= 0; i--)
					if (columnParagraphs[i].Width < averageWidth * 0.8 || columnParagraphs[i].Width > averageWidth * 1.2)
						columnParagraphs.RemoveAt(i);

				if (columnParagraphs.Count > 0)
				{
					square = 0;
					sum = 0;

					foreach (Paragraph paragraph in columnParagraphs)
					{
						square += paragraph.Width * paragraph.Height;
						sum += paragraph.Height;
					}

					return square / sum;
				}
			}

			return null;
		}
		#endregion

		#region Changed()
		private void Changed()
		{
			if (Invalidated != null)
				Invalidated(this, null);

			if (PageChanged != null)
				PageChanged(this);
		}
		#endregion

		#region Clip_Changed()
		void Clip_Changed(Clip clip)
		{
			if (this.locked == false && (this.ClipRect.Width < minSize.Width || this.ClipRect.Height < minSize.Height) && this.RemoveRequest != null)
				this.RemoveRequest(this);
			else
				Changed();
		}
		#endregion

		#region Fingers_Changed()
		private void Fingers_Changed()
		{
			if (Invalidated != null)
				Invalidated(this, null);
		}
		#endregion

		#region Bookfolding_Changed()
		void Bookfolding_Changed()
		{
			if (Invalidated != null)
				Invalidated(this, null);
		}
		#endregion

		#endregion

	}

}
