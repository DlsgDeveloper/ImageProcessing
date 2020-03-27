using BIP.Geometry;
using ImageProcessing.ImageFile;
using ImageProcessing.Languages;
using ImageProcessing.PageObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;


namespace ImageProcessing.IpSettings
{
	public class ItImage : IDisposable
	{
		// Fields
		//FileInfo			file = null;
		FileInfo			gRasterFile = null;
		ImageInfo			imageInfo;
		BigImages.ItDecoder	itBitmap = null;
		InchSize			sizeInInches;
		bool				isLandscapeImage;

		ImageProcessing.IpSettings.ItImageSettings	imageSettings = new ImageProcessing.IpSettings.ItImageSettings();
		PageObjects.PageObjects						pageObjects = new ImageProcessing.PageObjects.PageObjects(); 

		//private bool		twoPages = false;
		private ImageProcessing.IpSettings.ItPage		pageL;
		private ImageProcessing.IpSettings.ItPage		pageR;
		
		private int			wThresholdDelta = 0;
		private int			minDelta = 20;
		private bool		recreatePageObjects = true;

		private PostProcessing postProcessing = new PostProcessing();

		public delegate void VoidHnd();

		public event ItPropertiesChangedHnd		Changed;
		public event ProgressHnd				ExecutionProgressChanged;

		//
		bool changedSinceLastReset = false;

	
		#region constructor
		private ItImage()
		{
			this.imageSettings.SettingsChanged += new ImageProcessing.IpSettings.ItImageSettings.VoidHnd(Settings_Changed);
		}

		public ItImage(FileInfo file)
			:this()
		{
			//this.file = file;
			this.imageInfo = new ImageProcessing.ImageFile.ImageInfo(file);
			this.itBitmap = new ImageProcessing.BigImages.ItDecoder(file.FullName);
			this.sizeInInches = new InchSize(this.imageInfo.Width / (double)this.imageInfo.DpiH, this.imageInfo.Height / (double)this.imageInfo.DpiV);
			this.isLandscapeImage = this.imageInfo.Width >= this.imageInfo.Height;

			//bool twoPages = (imageInfo.Width > imageInfo.Height);
			this.pageL = new ImageProcessing.IpSettings.ItPage(this, true);
			this.pageR = new ImageProcessing.IpSettings.ItPage(this, (imageInfo.Width > imageInfo.Height));

			this.pageL.ExecutionProgressChanged += new ProgressHnd(Page_ExecutionProgressChanged);
			this.pageR.ExecutionProgressChanged += new ProgressHnd(Page_ExecutionProgressChanged);

			this.OpticsCenter = 0.5;

			this.Reset(TwoPages);

			this.pageL.Changed += new ItPage.PageChangedHnd(Anything_Changed);
			this.pageR.Changed += new ItPage.PageChangedHnd(Anything_Changed);

			this.pageL.RemoveRequest += new ImageProcessing.IpSettings.ItPage.ItPageHnd(Page_RemoveRequest);
			this.pageR.RemoveRequest += new ImageProcessing.IpSettings.ItPage.ItPageHnd(Page_RemoveRequest);
		}

		public ItImage(FileInfo file, bool twoPages)
			: this()
		{
			//this.file = file;
			this.imageInfo = new ImageProcessing.ImageFile.ImageInfo(file);
			this.itBitmap = new ImageProcessing.BigImages.ItDecoder(file.FullName);
			this.sizeInInches = new InchSize(this.imageInfo.Width / (double)this.imageInfo.DpiH, this.imageInfo.Height / (double)this.imageInfo.DpiV);
			this.isLandscapeImage = this.imageInfo.Width >= this.imageInfo.Height;

			this.pageL = new ImageProcessing.IpSettings.ItPage(this, true);
			this.pageR = new ImageProcessing.IpSettings.ItPage(this, twoPages);

			//this.twoPages = twoPages;
			this.OpticsCenter = 0.5;

			this.Reset(this.TwoPages);

			this.pageL.Changed += new ItPage.PageChangedHnd(Anything_Changed);
			this.pageR.Changed += new ItPage.PageChangedHnd(Anything_Changed);

			this.pageL.RemoveRequest += new ImageProcessing.IpSettings.ItPage.ItPageHnd(Page_RemoveRequest);
			this.pageR.RemoveRequest += new ImageProcessing.IpSettings.ItPage.ItPageHnd(Page_RemoveRequest);
		}
		#endregion

		~ItImage()
		{
			Dispose();
		}

		#region TiffCompression
		public enum TiffCompression
		{
			None,
			G4,
			LZW
		}
		#endregion

		// PUBLIC PROPERTIES
		#region public properties
		//internal ImageInfo	FullImageInfo { get { return this.imageInfo; } }
		//public Rectangle	ImageRect { get { return new Rectangle(Point.Empty, this.ImageSize); } }
		//public Size			FullImageSize { get { return this.imageInfo.Size; } }
		public InchSize		InchSize { get { return this.sizeInInches; } }
		public PixelsFormat PixelsFormat { get { return this.imageInfo.PixelsFormat; } }
		public bool			IsLandscapeImage { get { return this.isLandscapeImage; } }

		public bool			ChangedSinceLastReset { get { return this.changedSinceLastReset; } }

		public ImageProcessing.IpSettings.ItPage		Page { get { return this.pageL; } }
		public ImageProcessing.IpSettings.ItPage		PageL { get { return this.pageL; } }
		public ImageProcessing.IpSettings.ItPage		PageR { get { return this.pageR; } }
		
		public Delimiters	Delimiters { get { return this.pageObjects.Delimiters; } }
		public Symbols		AllSymbols { get { return this.pageObjects.AllSymbols; } }
		public Symbols		LoneSymbols { get { return this.pageObjects.LoneSymbols; } }
		public Pictures		Pictures { get { return this.pageObjects.Pictures; } }
		public Words		Words { get { return this.pageObjects.Words; } }
		public Lines		Lines { get { return this.pageObjects.Lines; } }
		public Paragraphs	Paragraphs { get { return this.pageObjects.Paragraphs; } }
		
		public PageObjects.PageObjects PageObjects { get { return this.pageObjects; } }

		public bool			TwoPages 
		{
			get { return this.PageL.IsActive && this.pageR.IsActive; }
		}

		#region OpticsCenter
		public double			OpticsCenter 
		{ 
			get { return this.imageSettings.OpticsCenter; } 
			set { this.imageSettings.OpticsCenter = Math.Max(0, Math.Min(1, value)); }
		}
		#endregion

		#region File
		/*public FileInfo File 
		{ 
			get { return this.file; } 
			set 
			{
				if (this.file != value)
				{
					this.file = value;

					Pages_Changed();
				}
			}
		}*/
		#endregion

		#region IsFixed
		public bool IsFixed 
		{ 
			get { return this.imageSettings.IsFixed; } 
			set 
			{
				if (this.imageSettings.IsFixed != value)
				{
					this.imageSettings.IsFixed = value;

					if (this.imageSettings.IsFixed)
						Reset(false);
				
				}
			}
		}
		#endregion

		#region IsIndependent
		public bool IsIndependent
		{
			get { return this.imageSettings.IsIndependent; }
			set { this.imageSettings.IsIndependent = value; }
		}
		#endregion

		#region Tag
		public object Tag
		{
			get { return this.imageSettings.Tag; }
			set { this.imageSettings.Tag = value; }
		}
		#endregion

		#region PostProcessing
		public PostProcessing PostProcessing 
		{ 
			get 
			{
				if (this.postProcessing == null)
					this.postProcessing = new PostProcessing();
				
				return postProcessing; 
			} 
			set { this.postProcessing = value; }
		}
		#endregion

		#region WhiteThresholdDelta
		public int WhiteThresholdDelta 
		{ 
			get { return wThresholdDelta; }
			set
			{
				if (wThresholdDelta != value)
				{
					wThresholdDelta = value;
					recreatePageObjects = true;
				}
			}
		}
		#endregion

		#region MinDelta
		public int MinDelta
		{
			get { return minDelta; }
			set
			{
				if (minDelta != value)
				{
					minDelta = value;
					recreatePageObjects = true;
				}
			}
		}
		#endregion

		#region Confidence
		public float Confidence
		{
			get
			{
				if (this.TwoPages)
					return Math.Min(this.PageL.Confidence, this.PageR.Confidence);
				else
					return this.Page.Confidence;
			}
		}
		#endregion

		#endregion

		//PRIVATE PROPERTIES
		#region private properties

		#region RasterFile
		private FileInfo RasterFile
		{
			get
			{
				if (this.gRasterFile == null)
				{
					this.gRasterFile = new FileInfo(Path.GetTempPath() + Guid.NewGuid() + ".rst");
					this.gRasterFile.Directory.Create();
				}

				this.gRasterFile.Refresh();
				return this.gRasterFile;
			}
		}
		#endregion

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region FromOldItSettings()
		public static ImageProcessing.IpSettings.ItImage FromOldItSettings(ImageProcessing.ItImage itImage)
		{
			BIP.Geometry.RatioSize s = new BIP.Geometry.RatioSize(itImage.ImageSize.Width, itImage.ImageSize.Height);
			ImageProcessing.IpSettings.ItImage bigImage = new ImageProcessing.IpSettings.ItImage(itImage.File, itImage.TwoPages);

			if (itImage.TwoPages)
			{
				bigImage.PageL.Lock();
				bigImage.PageR.Lock();

				bigImage.PageL.Activate(new BIP.Geometry.RatioRect(itImage.PageL.ClipRect.X / s.Width, itImage.PageL.ClipRect.Y / s.Height, itImage.PageL.ClipRect.Width / s.Width, itImage.PageL.ClipRect.Height / s.Height), true);
				bigImage.PageR.Activate(new BIP.Geometry.RatioRect(itImage.PageR.ClipRect.X / s.Width, itImage.PageR.ClipRect.Y / s.Height, itImage.PageR.ClipRect.Width / s.Width, itImage.PageR.ClipRect.Height / s.Height), true);

				bigImage.PageL.SetSkew(itImage.PageL.Clip.Skew, itImage.PageL.Clip.SkewConfidence);
				bigImage.PageR.SetSkew(itImage.PageR.Clip.Skew, itImage.PageR.Clip.SkewConfidence);

				//fingers
				foreach (ImageProcessing.Finger finger in itImage.PageL.Fingers)
				{
					if (finger.RectangleNotSkewed.Width > 0 && finger.RectangleNotSkewed.Height > 0)
					{
						double x = (finger.RectangleNotSkewed.X - itImage.PageL.ClipRect.X) / (double)itImage.PageL.ClipRect.Width;
						double y = (finger.RectangleNotSkewed.Y - itImage.PageL.ClipRect.Y) / (double)itImage.PageL.ClipRect.Height;
						double r = (finger.RectangleNotSkewed.Right - itImage.PageL.ClipRect.X) / (double)itImage.PageL.ClipRect.Width;
						double b = (finger.RectangleNotSkewed.Bottom - itImage.PageL.ClipRect.Y) / (double)itImage.PageL.ClipRect.Height;

						bigImage.PageL.AddFinger(BIP.Geometry.RatioRect.FromLTRB(x, y, r, b));
					}
				}

				foreach (ImageProcessing.Finger finger in itImage.PageR.Fingers)
				{
					if (finger.RectangleNotSkewed.Width > 0 && finger.RectangleNotSkewed.Height > 0)
					{
						double x = (finger.RectangleNotSkewed.X - itImage.PageR.ClipRect.X) / (double)itImage.PageR.ClipRect.Width;
						double y = (finger.RectangleNotSkewed.Y - itImage.PageR.ClipRect.Y) / (double)itImage.PageR.ClipRect.Height;
						double r = (finger.RectangleNotSkewed.Right - itImage.PageR.ClipRect.X) / (double)itImage.PageR.ClipRect.Width;
						double b = (finger.RectangleNotSkewed.Bottom - itImage.PageR.ClipRect.Y) / (double)itImage.PageR.ClipRect.Height;

						bigImage.PageR.AddFinger(BIP.Geometry.RatioRect.FromLTRB(x, y, r, b));
					}
				}

				//bookfold
				if (itImage.PageL.Bookfolding.IsCurved)
				{
					List<RatioPoint> topPoints = new List<RatioPoint>();
					List<RatioPoint> bottomPoints = new List<RatioPoint>();

					foreach (Point p in itImage.PageL.Bookfolding.TopCurve.Points)
						topPoints.Add(new RatioPoint(p.X / s.Width, p.Y / s.Height));

					foreach (Point p in itImage.PageL.Bookfolding.BottomCurve.Points)
						bottomPoints.Add(new RatioPoint(p.X / s.Width, p.Y / s.Height));

					bigImage.PageL.Bookfolding.SetCurves(topPoints.ToArray(), bottomPoints.ToArray(), itImage.PageL.Bookfolding.Confidence);
				}

				if (itImage.PageR.Bookfolding.IsCurved)
				{
					List<RatioPoint> topPoints = new List<RatioPoint>();
					List<RatioPoint> bottomPoints = new List<RatioPoint>();

					foreach (Point p in itImage.PageR.Bookfolding.TopCurve.Points)
						topPoints.Add(new RatioPoint(p.X / s.Width, p.Y / s.Height));

					foreach (Point p in itImage.PageR.Bookfolding.BottomCurve.Points)
						bottomPoints.Add(new RatioPoint(p.X / s.Width, p.Y / s.Height));

					bigImage.PageR.Bookfolding.SetCurves(topPoints.ToArray(), bottomPoints.ToArray(), itImage.PageR.Bookfolding.Confidence);
				}
	
				//optics center
				bigImage.OpticsCenter = itImage.OpticsCenter / s.Height;
				
				bigImage.PageL.Unlock();
				bigImage.PageR.Unlock();
			}
			else
			{
				bigImage.PageL.Lock();
				
				bigImage.PageL.Activate(new BIP.Geometry.RatioRect(itImage.PageL.ClipRect.X / s.Width, itImage.PageL.ClipRect.Y / s.Height, itImage.PageL.ClipRect.Width / s.Width, itImage.PageL.ClipRect.Height / s.Height), true);
				bigImage.PageR.Deactivate();

				bigImage.PageL.SetSkew(itImage.PageL.Clip.Skew, itImage.PageL.Clip.SkewConfidence);

				//fingers
				foreach (ImageProcessing.Finger finger in itImage.PageL.Fingers)
				{
					if (finger.RectangleNotSkewed.Width > 0 && finger.RectangleNotSkewed.Height > 0)
					{
						double x = (finger.RectangleNotSkewed.X - itImage.PageL.ClipRect.X) / (double)itImage.PageL.ClipRect.Width;
						double y = (finger.RectangleNotSkewed.Y - itImage.PageL.ClipRect.Y) / (double)itImage.PageL.ClipRect.Height;
						double r = (finger.RectangleNotSkewed.Right - itImage.PageL.ClipRect.X) / (double)itImage.PageL.ClipRect.Width;
						double b = (finger.RectangleNotSkewed.Bottom - itImage.PageL.ClipRect.Y) / (double)itImage.PageL.ClipRect.Height;

						bigImage.PageL.AddFinger(BIP.Geometry.RatioRect.FromLTRB(x, y, r, b));
					}
				}

				//bookfold
				if (itImage.PageL.Bookfolding.IsCurved)
				{
					Point[] points = itImage.PageL.Bookfolding.TopCurve.Points;
					List<RatioPoint> topPoints = new List<RatioPoint>();
					List<RatioPoint> bottomPoints = new List<RatioPoint>();

					foreach (Point p in itImage.PageL.Bookfolding.TopCurve.Points)
						topPoints.Add(new RatioPoint(p.X / s.Width, p.Y / s.Height));

					foreach (Point p in itImage.PageL.Bookfolding.BottomCurve.Points)
						bottomPoints.Add(new RatioPoint(p.X / s.Width, p.Y / s.Height));

					bigImage.PageL.Bookfolding.SetCurves(topPoints.ToArray(), bottomPoints.ToArray(), itImage.PageL.Bookfolding.Confidence);
				}

				//optics center
				bigImage.OpticsCenter = itImage.OpticsCenter / s.Height;

				bigImage.PageL.Unlock();
			}

			return bigImage;
		}
		#endregion

		#region Dispose()
		public void Dispose()
		{
			this.pageObjects.Dispose();
			DeleteRaster();
		}
		#endregion

		#region CreatePageObjects()
		public void CreatePageObjects(string filePath, BIP.Geometry.RatioRect ratioClip)
		{
			Bitmap source = null;

			try
			{
				if (this.pageObjects.IsDefined == false || recreatePageObjects)
				{
					try
					{						
						source = ImageCopier.LoadFileIndependentImage(filePath);
					}
					catch (Exception ex)
					{
						throw new Exception(BIPStrings.CanTOpenImage_STR + ex.Message);
					}
	
					Bitmap raster = ImagePreprocessing.GoDarkBookfold(source, wThresholdDelta, minDelta);

					if (ratioClip != null && ratioClip.IsEmpty == false)
						ImagePreprocessing.CutOffBorder(raster, ratioClip);

					SaveRaster(raster);

					NoiseReduction.Despeckle(raster, NoiseReduction.DespeckleSize.Size2x2, NoiseReduction.DespeckleMode.WhiteSpecklesOnly, NoiseReduction.DespeckleMethod.Regions);
					ImagePreprocessing.GetRidOfBorders(raster);
					NoiseReduction.Despeckle(raster, NoiseReduction.DespeckleSize.Size4x4, NoiseReduction.DespeckleMode.WhiteSpecklesOnly, NoiseReduction.DespeckleMethod.Regions);

					this.pageObjects.CreatePageObjects(raster, Paging.Both);

					raster.Dispose();
					recreatePageObjects = false;
				}
			}
			finally
			{
				if (source != null)
					source.Dispose();
			}
		}
		#endregion

		#region ReleasePageObjects()
		public void ReleasePageObjects()
		{
			this.pageObjects.Reset();
		}
		#endregion

		#region ImportSettings()
		public void ImportSettings(ImageProcessing.IpSettings.ItImage itImage)
		{
			this.PageL.ImportSettings(itImage.PageL);
			this.PageR.ImportSettings(itImage.PageR);
		}
		#endregion

		#region Find()
		public float Find(string filePath, Operations operations)
		{	
			if (this.IsFixed == false)
			{
				if (operations.CropAndDescew.Active)
				{		
					return FindCropAndDeskew(operations.CropAndDescew);
				}
				else
				{		
					if (operations.ContentLocation.Active || operations.Skew.Active || operations.BookfoldCorrection.Active)
						CreatePageObjects(filePath, BIP.Geometry.RatioRect.Empty);
		
					if (operations.ContentLocation.Active)
						FindContent(operations.ContentLocation);

					if (operations.Skew.Active)
						FindSkew(operations.Skew);

					if (operations.BookfoldCorrection.Active)
						FindCurving(operations.BookfoldCorrection);

					if (operations.ContentLocation.Active)
						SetOffsetInch(operations.ContentLocation.OffsetX);

					if (operations.Artifacts.Active)
						FindFingers(filePath, operations.Artifacts);

					GC.Collect();
					return this.Confidence;
				}
			}

			return 1.0F;
		}
		#endregion

		#region Execute()
		public void Execute(string sourceFile, int pageIndex, string destinationFile, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			try
			{
				if (pageIndex == 1)
				{
					if (this.TwoPages == false)
						throw new IpException(ErrorCode.InvalidParameter);

					this.PageR.Execute(sourceFile, destinationFile, imageFormat);
				}
				else
				{
					this.PageL.Execute(sourceFile, destinationFile, imageFormat);
				}
			}
			catch (Exception ex)
			{
				ImageProcessing.IpSettings.ItPage page = (pageIndex > 0) ? this.PageR : this.PageL;
				throw new Exception(string.Format("ImageProcessing.IpSettings.ItImage, GetResult(); index: {0}, TwoPages: {1}, Clip: {2}, Skew: {3}, Error: {4}.", 
					pageIndex, TwoPages, page.ClipRect, page.Skew, ex.Message));
			}
		}
		#endregion

		#region Reset()
		public void Reset(bool twoPages)
		{
			if (twoPages)
			{
				this.PageL.Reset(RatioRect.FromLTRB(0.04, 0.02, 0.48, 0.98));
				this.PageR.Reset(RatioRect.FromLTRB(0.52, 0.02, 0.96, 0.98));

				this.PageL.Activate(RatioRect.FromLTRB(0.04, 0.02, 0.48, 0.98), true);
				this.PageR.Activate(RatioRect.FromLTRB(0.52, 0.02, 0.96, 0.98), true);
			}
			else
			{
				this.PageL.Reset(RatioRect.Default);
				this.PageL.Activate(RatioRect.Default, true);
				this.PageR.Deactivate();
			}
		}
		#endregion

		#region ResetChangedSinceLastReset()
		public void ResetChangedSinceLastReset()
		{
			this.changedSinceLastReset = false;
		}
		#endregion
	
		//PAGES

		#region GetPage()
		public ImageProcessing.IpSettings.ItPage GetPage(RatioPoint imagePoint)
		{
			if (this.TwoPages)
			{
				if (this.PageR.Clip.Contains(imagePoint))
				{
					return this.PageR;
				}
				if (this.PageL.Clip.Contains(imagePoint))
				{
					return this.PageL;
				}
			}
			else
			{
				return this.PageL;
			}
			
			return null;
		}

		public ImageProcessing.IpSettings.ItPage GetPage(RatioRect rect)
		{
			if (this.TwoPages)
			{
				if (RatioRect.Intersect(this.PageR.ClipRect, rect) != RatioRect.Empty)
					return this.PageR;
				if (RatioRect.Intersect(this.PageL.ClipRect, rect) != RatioRect.Empty)
					return this.PageL;
			}
			else
			{
				return this.PageL;
			}
			
			return null;
		}
		#endregion

		#region GetLeftPage()
		public ImageProcessing.IpSettings.ItPage GetLeftPage()
		{
			if (this.TwoPages)
			{
				if (this.PageL.Clip.Center.X <= this.PageR.Clip.Center.X)
					return this.PageL;
				else
					return this.PageR;
			}

			if (this.isLandscapeImage == false || this.PageL.Clip.Center.X <= 0.5)
				return this.PageL;

			return null;
		}
		#endregion

		#region GetRightPage()
		public ImageProcessing.IpSettings.ItPage GetRightPage()
		{
			if (this.TwoPages)
			{
				if (this.PageL.Clip.Center.X <= this.PageR.Clip.Center.X)
					return this.PageR;
				else
					return this.PageL;
			}
			else if (this.isLandscapeImage && this.PageL.Clip.Center.X >= 0.5)
				return this.PageL;

			return null;
		}
		#endregion

		#region ValidatePages()
		/// <summary>
		/// This function makes sure that clip to the left is PageL and clip to the right is PageR
		/// </summary>
		/// <returns></returns>
		public void ValidatePages()
		{
			if (this.TwoPages)
			{
				if (this.PageL.Clip.Center.X > this.PageR.Clip.Center.X)
				{
					ImageProcessing.IpSettings.ItPage page = this.PageR.Clone();
					this.PageR.ImportSettings(this.PageL);
					this.PageL.ImportSettings(page);
					
					Settings_Changed();
				}
			}
		}
		#endregion

		#region GetPages()
		public List<ImageProcessing.IpSettings.ItPage> GetPages()
		{
			List<ImageProcessing.IpSettings.ItPage> list = new List<ImageProcessing.IpSettings.ItPage>();

			if (this.TwoPages)
			{
				list.AddRange(new ImageProcessing.IpSettings.ItPage[] { this.PageL, this.PageR });
				return list;
			}
			else
			{
				list.Add(this.PageL);
				return list;
			}
		}
		#endregion

		#region RemovePage()
		public void RemovePage(ImageProcessing.IpSettings.ItPage page)
		{
			if ((page != null) && (this.PageL == page || this.PageR == page))
			{
				if (this.TwoPages)
				{
					if (page == this.PageL)
					{
						this.PageL.ImportSettings(this.PageR);
						this.PageR.Deactivate();
					}
					else
					{
						this.PageR.Deactivate();
					}
				}
				else
				{
					if(page == this.PageL)
						this.PageL.Reset(RatioRect.Default);
				}
			}
		}
		#endregion

		#region SetClipsSize()
		public void SetClipsSize(InchSize size)
		{
			if (this.TwoPages)
			{
				this.PageL.SetClipSize(size);
				this.PageR.SetClipSize(size);
			}
			else
				this.Page.SetClipSize(size);
		}
		#endregion

		#region ResetClip()
		public bool ResetClip(ImageProcessing.IpSettings.ItPage page)
		{
			if (this.TwoPages)
			{
				throw new IpException(ErrorCode.CantRemovePageFrom2PageImage);
			}
			if (page == this.PageL)
			{
				this.PageL.ResetClip();
				return true;
			}
			return false;
		}
		#endregion

		#region SetOffset()
		public void SetOffset(double offsetX, double offsetY)
		{
			if (this.TwoPages)
			{
				this.PageL.SetOffset(offsetX, offsetY);
				this.PageR.SetOffset(offsetX, offsetY);
			}
			else
				this.Page.SetOffset(offsetX, offsetY);
		}
		#endregion

		#region SetOffsetInch()
		public void SetOffsetInch(double offset)
		{
			SetOffset(offset / InchSize.Width, offset / InchSize.Height);
		}
		#endregion

		//CURVE CORRECTION

		#region SetPageBookfolding()
		public void SetPageBookfolding(ImageProcessing.IpSettings.PageBookfold bfParamsL, ImageProcessing.IpSettings.PageBookfold bfParamsR)
		{
			if (!this.TwoPages)
				throw new IpException(ErrorCode.BfJust1PageAllocated);

			this.PageL.SetPageBookfolding(bfParamsL.TopCurve, bfParamsL.BottomCurve, bfParamsL.Confidence);
			this.PageR.SetPageBookfolding(bfParamsR.TopCurve, bfParamsR.BottomCurve, bfParamsR.Confidence);
		}

		public void SetPageBookfolding(ImageProcessing.IpSettings.ItPage page, ImageProcessing.IpSettings.PageBookfold bfParams)
		{
			if (this.TwoPages)
				throw new IpException(ErrorCode.ConstructPagesFirst);

			page.SetPageBookfolding(bfParams.TopCurve, bfParams.BottomCurve, bfParams.Confidence);
		}
		#endregion

		#region ResetPageBookfolding()
		public bool ResetPageBookfolding()
		{
			return (this.PageL.ResetPageBookfolding() || this.PageR.ResetPageBookfolding());
		}

		public bool ResetPageBookfolding(ImageProcessing.IpSettings.ItPage page)
		{
			return page.ResetPageBookfolding();
		}
		#endregion

		#region ShiftBookfoldPoints()
		/*public bool ShiftBookfoldPoints(ImageProcessing.IpSettings.ItPage page, Curve curve, int dy)
		{
			if (this.PageL == page)
				return this.PageL.ShiftBookfoldPoints(curve, dy);
			
			if ((this.PageR == page) && this.twoPages)
				return this.PageR.ShiftBookfoldPoints(curve, dy);

			return false;
		}*/
		#endregion

		//FINGERS	

		#region GetFingers()
		public Fingers GetFingers()
		{
			Fingers fingers = new Fingers();
			
			fingers.AddRange(this.PageL.Fingers);
			fingers.AddRange(this.PageR.Fingers);
			
			return fingers;
		}
		#endregion

		#region AddFinger()
		public void AddFinger(ImageProcessing.IpSettings.Finger finger)
		{
			ImageProcessing.IpSettings.ItPage page = GetPage(finger.RectangleNotSkewed);

			if (page != null && page.Fingers.Contains(finger) == false)
				page.AddFinger(finger);
			else
				throw new IpException(ErrorCode.FingerRegionNotInPage);
		}

		/*public ImageProcessing.IpSettings.Finger AddFinger(Rectangle rect)
		{
			ImageProcessing.IpSettings.ItPage itPage = this.GetPage(rect);
			
			if (itPage != null)
				return itPage.AddFinger(rect);
			
			return null;
		}*/
		#endregion

		#region AddFingers()
		public void AddFingers(List<ImageProcessing.IpSettings.Finger> fingers)
		{
			foreach (ImageProcessing.IpSettings.Finger finger in fingers)
			{
				ImageProcessing.IpSettings.ItPage page = this.GetPage(finger.RectangleNotSkewed);

				if (!((page == null) || page.Fingers.Contains(finger)))
					page.AddFinger(finger);
			}
		}
		#endregion

		#region ClearFingers()
		public void ClearFingers()
		{
			this.PageL.ClearFingers();
			this.PageR.ClearFingers();
		}
		#endregion

		#region RemoveFinger()
		public bool RemoveFinger(ImageProcessing.IpSettings.Finger finger)
		{
			return (this.PageL.RemoveFinger(finger) || this.PageR.RemoveFinger(finger));
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region FindContent()
		/// <summary>
		/// Returns confidence factor from 0 to 1.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		private float FindContent(Operations.ContentLocationParams parameters)
		{
			if (parameters.Seek2Pages)
			{
				this.pageL.Bookfolding.Reset();
				this.pageR.Bookfolding.Reset();
				this.pageL.Fingers.Clear();
				this.pageR.Fingers.Clear();

				float confidence = 0f;

				Size imageSize = this.pageObjects.BitmapSize.Value;
	
				Pages pages = ObjectLocator.FindPages(this.LoneSymbols, this.Words, this.Pictures, this.Delimiters, this.Paragraphs, imageSize, ref confidence);

				if (pages.Count >= 2)
				{
					RatioRect page1Rect = new RatioRect(pages[0].Rectangle.X / (float)imageSize.Width, pages[0].Rectangle.Y / (float)imageSize.Height, pages[0].Rectangle.Width / (float)imageSize.Width, pages[0].Rectangle.Height / (float)imageSize.Height);
					RatioRect page2Rect = new RatioRect(pages[1].Rectangle.X / (float)imageSize.Width, pages[1].Rectangle.Y / (float)imageSize.Height, pages[1].Rectangle.Width / (float)imageSize.Width, pages[1].Rectangle.Height / (float)imageSize.Height);
	
					this.pageL.Activate(page1Rect, true);
					this.PageR.Activate(page2Rect, true);
				
					this.PageL.Clip.SetContent(page1Rect, confidence);
					this.PageR.Clip.SetContent(page2Rect, confidence);
					
					return confidence;
				}
				if (pages.Count == 1)
				{
					RatioRect pageRect = new RatioRect(pages[0].Rectangle.X / (float)imageSize.Width, pages[0].Rectangle.Y / (float)imageSize.Height, pages[0].Rectangle.Width / (float)imageSize.Width, pages[0].Rectangle.Height / (float)imageSize.Height);

					this.PageL.Activate(pageRect, true);
					this.PageL.Clip.SetContent(pageRect, confidence);
					this.PageR.Deactivate();

					return confidence;
				}

				this.PageL.Activate(new RatioRect(0, 0, 1, 1), true);
				this.PageR.Deactivate();
				return confidence;
			}
			else
			{
				this.pageL.Bookfolding.Reset();
				this.pageR.Bookfolding.Reset();
				this.pageL.Fingers.Clear();
				this.pageR.Fingers.Clear();

				float confidence = 0f;

				Rectangle rect = new Rectangle(Point.Empty, this.pageObjects.BitmapSize.Value);
				Page page = ObjectLocator.FindPage(this.LoneSymbols, this.Paragraphs, this.Pictures, this.Delimiters, rect, ref confidence);

				if (page != null)
				{
					RatioRect pageRect = new RatioRect(page.Rectangle.X / (float)rect.Width, page.Rectangle.Y / (float)rect.Height, page.Rectangle.Width / (float)rect.Width, page.Rectangle.Height / (float)rect.Height);

					this.PageL.Activate(pageRect, true);
					this.PageL.Clip.SetContent(pageRect, confidence);
					this.PageR.Deactivate();

					return confidence;
				}
				else
				{
					this.PageL.Activate(new RatioRect(0, 0, 1, 1), true);
					this.PageR.Deactivate();
					return confidence;
				}
			}	
		}
		#endregion

		#region FindSkew()
		private void FindSkew(Operations.SkewParams parameters)
		{
			if (this.TwoPages)
			{
				this.PageL.FindSkew();
				this.PageR.FindSkew();
			}
			else
			{
				if (this.PageL == null)
				{
					throw new IpException(ErrorCode.InvalidParameter);
				}
				this.PageL.FindSkew();
			}
		}
		#endregion

		#region FindCurving()
		private void FindCurving(Operations.BookfoldParams parameters)
		{
			if (this.TwoPages)
			{
				this.PageL.FindCurving();
				this.PageR.FindCurving();
			}
			else
			{
				if (this.PageL == null)
					throw new IpException(ErrorCode.InvalidParameter);

				this.PageL.FindCurving();
			}
		}
		#endregion

		#region FindFingers()
		private void FindFingers(string filePath, Operations.ArtifactsParams parameters)
		{
			if (this.RasterFile.Exists == false)
				CreatePageObjects(filePath, BIP.Geometry.RatioRect.Empty);
			
			Bitmap raster = new Bitmap(this.RasterFile.FullName);
			
			if (this.TwoPages)
			{
				this.PageL.FindFingers(raster);
				this.PageR.FindFingers(raster);
			}
			else
			{
				this.PageL.FindFingers(raster);
			}

			raster.Dispose();
		}	
		#endregion

		#region FindCropAndDeskew()
		private float FindCropAndDeskew(Operations.CropAndDescewParams parameters)
		{
			byte confidence = 0;
			Rectangle clip = new Rectangle(0, 0, this.itBitmap.Width, this.itBitmap.Height);

			clip.Inflate(-parameters.OffsetX, -parameters.OffsetY);

			ImageProcessing.BigImages.CropAndDeskew cropAndDescew = new ImageProcessing.BigImages.CropAndDeskew();

			ImageProcessing.CropDeskew.CdObject obj = cropAndDescew.GetCdObject(this.itBitmap, parameters.ThresholdColor, true, out confidence, 
				parameters.MinAngle, clip, parameters.GhostLines, parameters.GlLowThreshold, parameters.GlHighThreshold,
				parameters.GlToCheck, parameters.GlMaxDelta, Convert.ToInt16(parameters.MarginX * this.itBitmap.DpiX),
				Convert.ToInt16(parameters.MarginY * this.itBitmap.DpiY));

			Reset(false);
			RatioPoint location = ImageProcessing.BigImages.Rotation.RotatePoint(obj.CornerUl, obj.Centroid, obj.Skew, obj.WidthHeightRatio);
			RatioRect rect = new RatioRect(location.X, location.Y, obj.Width, obj.Height);

			this.PageL.Activate(rect, true);
			this.PageL.Clip.SetContent(rect, confidence / 100.0F);
			this.PageL.SetSkew(obj.Skew, confidence / 100.0F);
			this.PageR.Deactivate();

			return confidence / 100.0F;
		}
		#endregion

		#region SaveRaster()
		void SaveRaster(Bitmap raster)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			FileInfo rasterFile = RasterFile;
			
			DeleteRaster();

			EncoderParameters encoderParams = new EncoderParameters(2);
			encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long)EncoderValue.CompressionCCITT4);
			encoderParams.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, 1L);

			raster.Save(rasterFile.FullName, Encoding.GetCodecInfo(ImageFormat.Tiff), encoderParams);
#if DEBUG
			Console.WriteLine("ImageProcessing.IpSettings.ItImage, SaveRaster(): {0}", DateTime.Now.Subtract(start).ToString());
#endif
		}
		#endregion

		#region DeleteRaster()
		void  DeleteRaster()
		{
			FileInfo rasterFile = RasterFile;

			if (rasterFile.Exists)
				rasterFile.Delete();
		}
		#endregion

		#region Settings_Changed()
		private void Settings_Changed()
		{
			if (Changed != null)
				Changed(this, ItProperty.ImageSettings);

			changedSinceLastReset = true;
		}
		#endregion

		#region Page_RemoveRequest()
		void Page_RemoveRequest(ImageProcessing.IpSettings.ItPage itPage)
		{
			RemovePage(itPage);
		}
		#endregion

		#region Page_ExecutionProgressChanged()
		void Page_ExecutionProgressChanged(float progress)
		{
			if(this.ExecutionProgressChanged != null)
				this.ExecutionProgressChanged(progress);
		}
		#endregion

		#region Anything_Changed()
		void Anything_Changed(ItPage itPage, ItProperty type)
		{
			if (Changed != null)
				Changed(this, type);
			
			changedSinceLastReset = true;
		}
		#endregion

		#endregion

	}

}
