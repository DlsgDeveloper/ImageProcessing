using System;
using System.Drawing;
using System.Collections.Generic;
using System.Drawing.Imaging;

using ImageProcessing.PageObjects;
using BIP.Geometry;


namespace ImageProcessing.IpSettings
{
	public class ItPage
	{
		private ImageProcessing.IpSettings.PageBookfold bookfolding;
		private ImageProcessing.IpSettings.Clip			clip;
		private ImageProcessing.IpSettings.Fingers		fingers;
		private ImageProcessing.IpSettings.ItImage		itImage;
		private bool									active = true;

		bool locked = false;
		SizeF minSize = new SizeF(0.1F,0.1F);

		public delegate void PageChangedHnd(ImageProcessing.IpSettings.ItPage itPage, ItProperty type);
		public delegate void ItPageHnd(ImageProcessing.IpSettings.ItPage itPage);

		public event PageChangedHnd		Changed;
		bool							raiseChangedEvent = true;
		bool							changed = false;
		ItProperty						changedPropertyType = ItProperty.None;

		internal event ItPageHnd		RemoveRequest;
		public event ProgressHnd		ExecutionProgressChanged;


		#region constructor
		public ItPage(ImageProcessing.IpSettings.ItImage itImage, bool active)
			: this(itImage, RatioRect.Default, active)
		{
		}

		public ItPage(ImageProcessing.IpSettings.ItImage itImage, RatioRect clip, bool active)
		{
			this.itImage = itImage;
			this.active = active;
			this.clip = new ImageProcessing.IpSettings.Clip(clip, itImage.InchSize.Width / itImage.InchSize.Height);
			this.bookfolding = new ImageProcessing.IpSettings.PageBookfold(this, 1.0F);
			this.fingers = new ImageProcessing.IpSettings.Fingers();

			this.clip.ClipChanged += new ImageProcessing.IpSettings.Clip.ClipChangedHnd(Clip_Changed);
			this.bookfolding.Changed += new ImageProcessing.IpSettings.ItImage.VoidHnd(Bookfolding_Changed);
			this.fingers.Changed += new ItImage.VoidHnd(Fingers_Changed);
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
		public ImageProcessing.IpSettings.ItImage		ItImage { get { return this.itImage; } }
		public double									GlobalOpticsCenter { get { return this.itImage.OpticsCenter; } }
		public double									LocalOpticsCenter { get { return ((this.clip != null) ? (this.itImage.OpticsCenter - this.clip.RectangleNotSkewed.Y) : this.itImage.OpticsCenter); } }

		public bool										ClipSpecified { get { return this.clip.ClipSpecified; } }
		public bool										ClipContentSpecified { get { return this.clip.ContentDefined; } }
		public ImageProcessing.IpSettings.Clip			Clip { get { return this.clip; } }
		public RatioRect								ClipRect { get { return this.clip.RectangleNotSkewed; } }
		public double									Skew { get { return this.clip.Skew; } }
		public ImageProcessing.IpSettings.PageBookfold	Bookfolding { get { return this.bookfolding; } }
		public ImageProcessing.IpSettings.Fingers		Fingers { get { return this.fingers; } }

		public bool			IsSkewed { get { return this.clip.IsSkewed; } }
		public bool			IsCurved { get { return ((this.bookfolding != null) ? this.bookfolding.IsCurved : false); } }

		public Symbols		AllObjects { get { return this.itImage.PageObjects.GetAllSymbols(this.clip.RectangleNotSkewed); } }
		public Symbols		LoneObjects { get { return this.itImage.PageObjects.GetLoneSymbols(this.clip.RectangleNotSkewed); } }
		public Delimiters	Delimiters { get { return this.itImage.PageObjects.GetDelimiters(this.clip.RectangleNotSkewed); } }
		//public Paragraphs	Paragraphs { get { return ObjectLocator.FindParagraphs(this.LoneObjects, this.Words); } }
		public Pictures		Pictures { get { return this.itImage.PageObjects.GetPictures(this.clip.RectangleNotSkewed); } }
		public Words		Words { get { return this.itImage.PageObjects.GetWords(this.clip.RectangleNotSkewed); } }
		public Lines		Lines { get { return this.itImage.PageObjects.GetLines(this.clip.RectangleNotSkewed); } }
		public Paragraphs	Paragraphs { get { return this.itImage.PageObjects.GetParagraphs(this.clip.RectangleNotSkewed); } }
		public int?			ColumnsWidth{get{return GetColumnsWidth();}}
		public Size?		PageObjectsSize { get { return this.itImage.PageObjects.BitmapSize; } }

		public bool				IsActive
		{
			get { return this.active; }
			private set
			{
				if (this.active != value)
				{
					this.active = value;
					RaiseChanged(ItProperty.Clip);
				}
			}
		}
		
		#region ClipRectInch
		public InchRect ClipRectInch 
		{ 
			get 
			{
				return new InchRect(this.clip.RectangleNotSkewed.X * this.itImage.InchSize.Width,
					this.clip.RectangleNotSkewed.Y * this.itImage.InchSize.Height,
					this.clip.RectangleNotSkewed.Width * this.itImage.InchSize.Width,
					this.clip.RectangleNotSkewed.Height * this.itImage.InchSize.Height);
			}
		}
		#endregion

		#region ContentInch
		public InchRect ContentInch
		{
			get
			{
				return new InchRect(this.clip.Content.X * this.itImage.InchSize.Width,
					this.clip.Content.Y * this.itImage.InchSize.Height,
					this.clip.Content.Width * this.itImage.InchSize.Width,
					this.clip.Content.Height * this.itImage.InchSize.Height);
			}
		}
		#endregion

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

		#region RaiseChangedEvent
		public bool RaiseChangedEvent
		{
			get { return this.raiseChangedEvent; }
			set
			{
				if (this.raiseChangedEvent != value)
				{
					this.raiseChangedEvent = value;

					if (this.raiseChangedEvent && changed)
					{
						if (Changed != null)
							Changed(this, this.changedPropertyType);
						
						changed = false;
						this.changedPropertyType = ItProperty.None;
					}
				}
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

		#region Activate()
		public void Activate(RatioRect ratioRect, bool fixedClipSize)
		{
			if (this.IsActive == false || this.ClipRect != ratioRect)
			{
				this.IsActive = true;

				if (ratioRect.IsDefault || ratioRect.IsEmpty)
				{				
					this.ResetClip();
				}
				else
				{
					this.SetClip(ratioRect, fixedClipSize);
				}
			}
		}

		public void Activate(InchRect inchRect, bool fixedClipSize)
		{
			RatioRect ratioRect ;

			if(inchRect.IsEmpty)
				ratioRect = RatioRect.Default;
			else
				ratioRect = new RatioRect(inchRect.X / this.itImage.InchSize.Width, inchRect.Y / this.itImage.InchSize.Height,
				inchRect.Width / this.itImage.InchSize.Width, inchRect.Height / this.itImage.InchSize.Height);
			
			if (this.active == false || this.ClipRect != ratioRect)
			{
				this.IsActive = true;
				this.SetClip(ratioRect, fixedClipSize);
			}
		}
		#endregion

		#region Deactivate()
		public void Deactivate()
		{
			if (this.active == true)
			{
				this.IsActive = false;
				Reset(RatioRect.Default);
			}
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
		public RatioRect FindContent(Size imageSize)
		{
			float confidence = 0f;
			Page page = ImageProcessing.ObjectsRecognition.ObjectLocator.FindPage(this.LoneObjects, this.Paragraphs, this.Pictures, this.Delimiters, new Rectangle(0,0,imageSize.Width, imageSize.Height), ref confidence);

			if (page != null)
			{
				RatioRect pageRect = new RatioRect(page.Rectangle.X / (double)imageSize.Width, page.Rectangle.Y / (double)imageSize.Height, page.Rectangle.Width / (double)imageSize.Width, page.Rectangle.Height / (double)imageSize.Height);
				//pageRect.Inflate(offset, offset);
				this.SetClip(pageRect, false);
				this.clip.SetContent(pageRect, confidence);
			}
			else
			{
				this.clip.SetClip(RatioRect.Default);
				this.clip.SetContent(null, 0.0F);
			}

			return this.clip.RectangleNotSkewed;
		}
		#endregion

		#region FindSkew()
		public double FindSkew()
		{
			float confidence;
			double angle = ImageProcessing.BigImages.Rotation.GetAngleFromObjects(this.PageObjectsSize.Value, this.Words, this.Pictures, this.Delimiters, out confidence);
			SetSkew(angle, confidence);
			
			return angle;
		}
		#endregion

		#region FindCurving()
		public void FindCurving()
		{
			ImageProcessing.BigImages.CurveCorrection.FindCurving(this);
		}
		#endregion

		#region FindFingers()
		public void FindFingers(Bitmap raster)
		{
			ImageProcessing.BigImages.FingerRemoval.FindFingers(raster, this);
		}
		#endregion

		//clip
		#region SetClip()
		public void SetClip(RatioRect rect, bool fixedClipSize)
		{
			this.RaiseChangedEvent = false;

			if (fixedClipSize)
			{
				rect.Width = (rect.Width > 1) ? 1 : ((rect.Width < 0) ? 0 : rect.Width);
				rect.Height = (rect.Height > 1) ? 1 : ((rect.Height < 0) ? 0 : rect.Height);
				rect.X = (rect.X < 0) ? 0 : ((rect.Right > 1) ? 1 - rect.Width : rect.X);
				rect.Y = (rect.Y < 0) ? 0 : ((rect.Bottom > 1) ? 1 - rect.Height : rect.Y);
			}
	
			this.clip.SetClip(rect);
			this.fingers.ClipChanged(this.clip);
			this.bookfolding.ClipChanged(this.clip);

			this.RaiseChangedEvent = true;
		}

		/// <summary>
		/// Inflates clip in x, y, r, b directions. If 'fixedClipSize' is true, clip position 
		/// will be chnged that clip can fit in the image. If false, clip will be inflated to the image edges.
		/// </summary>
		/// <param name="dl">delta left</param>
		/// <param name="dt">delta top</param>
		/// <param name="dr">delta right</param>
		/// <param name="db">delta bottom</param>
		/// <param name="fixedClipSize"></param>
		public void SetClip(double dl, double dt, double dr, double db, bool fixedClipSize)
		{
			if (ClipSpecified)
			{
				RatioRect rect = RatioRect.FromLTRB(this.ClipRect.X + dl, this.ClipRect.Y + dt, this.ClipRect.Right + dr, this.ClipRect.Bottom + db);

				SetClip(rect, fixedClipSize);
			}
		}

		/// <summary>
		/// Inflates clip in x, y, r, b directions. If 'fixedClipSize' is true, clip position 
		/// will be chnged that clip can fit in the image. If false, clip will be inflated to the image edges.
		/// </summary>
		/// <param name="dl">delta left</param>
		/// <param name="dt">delta top</param>
		/// <param name="dr">delta right</param>
		/// <param name="db">delta bottom</param>
		/// <param name="fixedClipSize"></param>
		public void SetClipInch(double dl, double dt, double dr, double db, bool fixedClipSize)
		{
			if (ClipSpecified)
			{
				InchSize imageSize = this.itImage.InchSize;

				RatioRect rect = RatioRect.FromLTRB(this.ClipRect.X + (dl/this.itImage.InchSize.Width), this.ClipRect.Y + (dt/this.itImage.InchSize.Height), this.ClipRect.Right + (dr/this.itImage.InchSize.Width), this.ClipRect.Bottom + (db/this.itImage.InchSize.Height));

				SetClip(rect, fixedClipSize);
			}
		}
	
		public void SetClip(InchSize newSize, ResizeDirection direction, bool fixedClipSize)
		{
			if (ClipSpecified)
			{
				InchSize imageSize = this.itImage.InchSize;
				InchSize clipSize = new InchSize(this.ClipRect.Width * imageSize.Width, this.ClipRect.Height * imageSize.Height);
				double x = this.ClipRect.X * imageSize.Width;
				double y = this.ClipRect.Y * imageSize.Height;
				double r = this.ClipRect.Right * imageSize.Width;
				double b = this.ClipRect.Bottom * imageSize.Height;
				double dx = clipSize.Width - newSize.Width;
				double dy = clipSize.Height - newSize.Height;

				switch (direction)
				{
					case ResizeDirection.All:
						{
							x = x + dx / 2;
							y = y + dy / 2;
							r = r - dx / 2;
							b = b - dy / 2;
						} break;
					case ResizeDirection.W:	x = x + dx; break;
					case ResizeDirection.N: y = y + dy; break;
					case ResizeDirection.E: r = r - dx; break;
					case ResizeDirection.S: b = b - dy; break;
					case ResizeDirection.NW: x = x + dx; y = y + dy; break;
					case ResizeDirection.NE: r = r - dx; y = y + dy; break;
					case ResizeDirection.SE: r = r - dx; b = b - dy; break;
					case ResizeDirection.SW: x = x + dx; b = b - dy; break;
				}

				RatioRect rect = RatioRect.FromLTRB(x / imageSize.Width, y / imageSize.Height, r / imageSize.Width, b / imageSize.Height);

				SetClip(rect, fixedClipSize);
			}
		}
		#endregion

		#region MoveClip()
		public void MoveClip(InchPoint newLocation, bool fixedClipSize)
		{
			if (ClipSpecified)
			{
				InchSize imageSize = this.itImage.InchSize;

				RatioRect rect = new RatioRect(newLocation.X / this.itImage.InchSize.Width, newLocation.Y / this.itImage.InchSize.Height, this.ClipRect.Width, this.ClipRect.Height);

				SetClip(rect, fixedClipSize);
			}
		}
		#endregion

		#region SetOffset()
		internal void SetOffset(double offsetX, double offsetY)
		{
			if (this.ClipContentSpecified)
			{
				SetClip(RatioRect.Inflate(this.clip.Content, offsetX, offsetY), false);
			}
		}
		#endregion

		#region SetClipSize()
		public void SetClipSize(InchSize size)
		{
			RatioSize ratioSize = new RatioSize(size.Width / (double)this.itImage.InchSize.Width, size.Height / (double)this.itImage.InchSize.Height);

			SetClipSize(ratioSize);
		}

		public void SetClipSize(RatioSize size)
		{
			RatioRect rect = (this.ClipSpecified) ? this.ClipRect : new RatioRect(0, 0, 1, 1);

			double offsetX = (size.Width - rect.Width) / 2.0F;
			double offsetY = (size.Height - rect.Height) / 2.0F;

			rect.Inflate(offsetX, offsetY);
			
			this.SetClip(rect, true);
		}
		#endregion

		#region ResetClip()
		public void ResetClip()
		{
			this.RaiseChangedEvent = false;

			this.clip.Reset();
			this.fingers.ClipChanged(this.Clip);
			this.bookfolding.ClipChanged(this.clip);
			
			this.RaiseChangedEvent = true;
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
		}
		#endregion

		//bookfold
		#region SetPageBookfolding()
		public void SetPageBookfolding(Curve curveT, Curve curveB, float confidence)
		{
			this.bookfolding.SetCurves(curveT,curveB, confidence);
		}
		#endregion

		#region ResetPageBookfolding()
		public bool ResetPageBookfolding()
		{
			if (this.bookfolding.IsCurved)
			{
				this.bookfolding.Reset();
				return true;
			}
			else
			{
				return false;
			}
		}
		#endregion

		//fingers
		#region AddFinger()
		public void AddFinger(ImageProcessing.IpSettings.Finger finger)
		{			
			this.fingers.Add(finger);
		}

		public ImageProcessing.IpSettings.Finger AddFinger(RatioRect rect)
		{
			rect.IntersectWithDefault();
			
			foreach (ImageProcessing.IpSettings.Finger finger in this.fingers)
				if (finger.IsIdentical(rect))
					return null;

			if (!rect.IsEmpty)
			{
				ImageProcessing.IpSettings.Finger finger = ImageProcessing.IpSettings.Finger.GetFinger(this, rect, false);
				
				if (finger != null)
				{
					this.fingers.Add(finger);
					return finger;
				}
			}

			return null;
		}
		#endregion

		#region ClearFingers()
		public void ClearFingers()
		{
			this.fingers.Clear();
		}
		#endregion

		#region RemoveFinger()
		public bool RemoveFinger(ImageProcessing.IpSettings.Finger finger)
		{
			if (this.fingers.Contains(finger))
			{
				this.fingers.Remove(finger);
				return true;
			}
		
			return false;
		}
		#endregion

		//misc
		#region ImportSettings()
		public void ImportSettings(ImageProcessing.IpSettings.ItPage itPage)
		{
			this.RaiseChangedEvent = false;

			if (itPage.IsActive)
			{
				this.Fingers.Clear();
				this.Bookfolding.Reset();
				
				this.Activate(itPage.ClipRect, true);
				SetSkew(itPage.Clip.Skew, itPage.clip.SkewConfidence);

				if (itPage.Clip.ContentDefined)
					clip.SetContent(itPage.Clip.Content, itPage.Clip.ClipConfidence);
				else
					clip.SetContent(null, itPage.Clip.ClipConfidence);

				this.bookfolding.ImportSettings(itPage.bookfolding);
				//itPage.ItImage.OpticsCenter = this.GlobalOpticsCenter;

				foreach (ImageProcessing.IpSettings.Finger finger in itPage.Fingers)
				{
					Finger f = ImageProcessing.IpSettings.Finger.GetFinger(this, finger.RectangleNotSkewed, false);

					if(f != null)
						this.Fingers.Add(f);
				}
			}
			else
				Deactivate();

			this.RaiseChangedEvent = true;
		}
		#endregion

		#region Clone()
		internal ImageProcessing.IpSettings.ItPage Clone()
		{
			ImageProcessing.IpSettings.ItPage clone = new ImageProcessing.IpSettings.ItPage(this.ItImage, this.IsActive);
			clone.Clip.SetClip(this.clip.RectangleNotSkewed);
			clone.Clip.SetSkew(this.Clip.Skew, this.clip.SkewConfidence);
			clone.SetPageBookfolding(this.bookfolding.TopCurve, this.bookfolding.BottomCurve, this.bookfolding.Confidence);
			clone.ItImage.OpticsCenter = this.GlobalOpticsCenter;
			
			foreach (ImageProcessing.IpSettings.Finger finger in this.Fingers)
				clone.Fingers.Add(finger.Clone(clone));

			return clone;
		}
		#endregion

		#region Reset()
		public void Reset(RatioRect clip)
		{
			this.RaiseChangedEvent = false;

			this.fingers.Clear();
			this.bookfolding.Reset();
			
			this.clip.SetClip(clip);
			this.clip.SetContent(null, 1.0F);
			this.clip.SetSkew(0.0, 1.0F);
			this.bookfolding.Reset();

			this.RaiseChangedEvent = true;
		}
		#endregion

		#region Execute()
		public void Execute(string sourceFile, string destinationFile, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(sourceFile))
			{
				if (this.bookfolding.IsCurved)
				{
					if (this.IsSkewed)
					{
						ImageProcessing.BigImages.CurveCorrectionAndRotation curveCorrectionAndRotation = new ImageProcessing.BigImages.CurveCorrectionAndRotation();

						curveCorrectionAndRotation.ProgressChanged += delegate(float progress)
						{
							if (this.ExecutionProgressChanged != null)
								this.ExecutionProgressChanged(progress);
						};

						curveCorrectionAndRotation.Execute(itDecoder, destinationFile, imageFormat, this);
					}
					else
					{
						ImageProcessing.BigImages.CurveCorrection2 curveCorrection = new ImageProcessing.BigImages.CurveCorrection2();

						curveCorrection.ProgressChanged += delegate(float progress)
						{
							if (this.ExecutionProgressChanged != null)
								this.ExecutionProgressChanged(progress);
						};

						curveCorrection.Execute(itDecoder, destinationFile, imageFormat, this);
					}
				}
				else if (this.clip.IsSkewed)
				{
					ImageProcessing.BigImages.Rotation rotation = new ImageProcessing.BigImages.Rotation();

					rotation.ProgressChanged += delegate(float progress)
					{
						if (this.ExecutionProgressChanged != null)
							this.ExecutionProgressChanged(progress);
					};

					rotation.RotateClip(itDecoder, destinationFile, imageFormat, this.Clip, 0xff, 0xff, 0xff);
				}
				else if (this.ClipRect != RatioRect.Empty)
				{
					ImageProcessing.BigImages.ImageCopier imageCopier = new ImageProcessing.BigImages.ImageCopier();

					imageCopier.ProgressChanged += delegate(float progress)
					{
						if (this.ExecutionProgressChanged != null)
							this.ExecutionProgressChanged(progress);
					};

					Rectangle clip = new Rectangle(Convert.ToInt32(ClipRect.X * itDecoder.Width), Convert.ToInt32(ClipRect.Y * itDecoder.Height), Convert.ToInt32(ClipRect.Width * itDecoder.Width), Convert.ToInt32(ClipRect.Height * itDecoder.Height));
					imageCopier.Copy(itDecoder, destinationFile, imageFormat, clip);
				}
				else
				{
					ImageProcessing.BigImages.ImageCopier imageCopier = new ImageProcessing.BigImages.ImageCopier();
					imageCopier.ProgressChanged += delegate(float progress)
					{
						if (this.ExecutionProgressChanged != null)
							this.ExecutionProgressChanged(progress);
					};

					imageCopier.Copy(itDecoder, destinationFile, imageFormat);
				}
			}

			//fingers
			if (this.Fingers.Count > 0)
			{

				ImageProcessing.BigImages.FingerRemoval fingerRemoval = new ImageProcessing.BigImages.FingerRemoval();

				fingerRemoval.ProgressChanged += delegate(float progress)
				{
					if (this.ExecutionProgressChanged != null)
						this.ExecutionProgressChanged(progress);
				};

				string tempFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\temp.png";

				if (System.IO.File.Exists(tempFile))
					System.IO.File.Delete(tempFile);

				using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(destinationFile))
				{
					fingerRemoval.EraseFingers(itDecoder, tempFile, imageFormat, this, itDecoder.Size);
				}

				if (System.IO.File.Exists(destinationFile))
					System.IO.File.Delete(destinationFile);

				System.IO.File.Move(tempFile, destinationFile);
			}

			//post processing
			if (itImage.PostProcessing.IsAnyOptionEnabled)
			{
				string tmp = destinationFile + "_temp.png";

				if (System.IO.File.Exists(tmp))
					System.IO.File.Delete(tmp);

				DoPostProcessing(destinationFile, itImage.PostProcessing, tmp, imageFormat);

				if (System.IO.File.Exists(destinationFile))
					System.IO.File.Delete(destinationFile);
				
				System.IO.File.Move(tmp, destinationFile);
			}
		}
		#endregion

		#region GetPagePoint()
		public RatioPoint GetPagePoint(RatioPoint imagePoint)
		{
			RatioPoint  pointNotSkewed = this.clip.TransferSkewedToUnskewedPoint(imagePoint);
			return new RatioPoint((pointNotSkewed.X - this.ClipRect.X) / this.ClipRect.Width, (pointNotSkewed.Y - this.ClipRect.Y) / this.ClipRect.Height);
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region DoPostProcessing()
		private void DoPostProcessing(string sourceFile, PostProcessing postProcessing, string destinationFile, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(sourceFile))
			{
				Bitmap bitmap = itDecoder.GetClip(new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));
				PixelsFormat pixelsFormat = itDecoder.PixelsFormat;

				if (postProcessing.ItDespeckle.IsEnabled && bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format1bppIndexed)
				{
					ImageProcessing.NoiseReduction despeckle = new ImageProcessing.NoiseReduction();
					despeckle.Despeckle(bitmap, postProcessing.ItDespeckle.MaskSize, NoiseReduction.DespeckleMethod.Regions, postProcessing.ItDespeckle.DespeckleMode);
				}

				if (postProcessing.ItBlackBorderRemoval.IsEnabled && bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format1bppIndexed)
					ImageProcessing.BlackBorderRemoval.RemoveBlackBorders(bitmap);

				if (postProcessing.ItBackgroundRemoval.IsEnabled && (bitmap.PixelFormat == PixelFormat.Format24bppRgb || bitmap.PixelFormat == PixelFormat.Format8bppIndexed || bitmap.PixelFormat == PixelFormat.Format32bppArgb || bitmap.PixelFormat == PixelFormat.Format32bppRgb))
					ImageProcessing.BackgroundRemoval.Go(bitmap);

				if (postProcessing.ItRotation.Angle == PostProcessing.Rotation.RotationMode.Rotation90)
					bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
				else if (postProcessing.ItRotation.Angle == PostProcessing.Rotation.RotationMode.Rotation180)
					bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
				else if (postProcessing.ItRotation.Angle == PostProcessing.Rotation.RotationMode.Rotation270)
					bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);

				if (postProcessing.ItInvertion.IsEnabled)
				{
					ImageProcessing.Inverter.Invert(bitmap);

					if (pixelsFormat == PixelsFormat.Format8bppGray)
						pixelsFormat = PixelsFormat.Format8bppIndexed;
				}

				using (ImageProcessing.BigImages.ItEncoder encoder = new ImageProcessing.BigImages.ItEncoder(destinationFile, imageFormat, pixelsFormat, bitmap.Width, bitmap.Height, bitmap.HorizontalResolution, bitmap.VerticalResolution))
				{
					BitmapData bitmapData = null;

					if (bitmap.Palette != null && bitmap.Palette.Entries.Length > 0)
						encoder.SetPalette(bitmap.PixelFormat, bitmap.Palette.Entries);
					
					try
					{
						bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
						unsafe
						{
							encoder.Write(bitmapData.Height, bitmapData.Stride, (byte*)bitmapData.Scan0.ToPointer());
						}

					}
					finally
					{
						if (bitmapData != null)
							bitmap.UnlockBits(bitmapData);
					}
				}
			}
		}
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

		#region Clip_Changed()
		void Clip_Changed(ImageProcessing.IpSettings.Clip clip)
		{
			if (this.locked == false && (this.ClipRect.Width < minSize.Width || this.ClipRect.Height < minSize.Height) && this.RemoveRequest != null)
				this.RemoveRequest(this);
			else
			{
				RaiseChanged(ItProperty.Clip);
			}
		}
		#endregion

		#region Bookfolding_Changed()
		void Bookfolding_Changed()
		{
			RaiseChanged(ItProperty.Bookfold);
		}
		#endregion

		#region Fingers_Changed()
		void Fingers_Changed()
		{
			RaiseChanged(ItProperty.Fingers);
		}
		#endregion

		#region RaiseChanged()
		void RaiseChanged(ItProperty type)
		{
			if (this.RaiseChangedEvent)
			{
				if (Changed != null)
					Changed(this, type);
			}
			else
			{
				this.changed = true;
				this.changedPropertyType |= type;
			}
		}
		#endregion

		#endregion

	}

}
