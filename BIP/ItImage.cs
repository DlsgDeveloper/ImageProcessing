using System;
using System.Drawing;
using System.Collections;

using ImageProcessing.PageObjects;
using ImageProcessing.ImageFile;
using System.Collections.Generic;
using System.IO;
using System.Drawing.Imaging;
using ImageProcessing.Languages;

namespace ImageProcessing
{
	public class ItImage : IDisposable
	{
		// Fields
		FileInfo			file = null;
		FileInfo			gRasterFile = null;
		ImageInfo			imageInfo;
		Bitmap				bitmap = null;

		ItImageSettings			imageSettings = new ItImageSettings();
		PageObjects.PageObjects pageObjects = new ImageProcessing.PageObjects.PageObjects(); 

		private bool		twoPages = false;
		private ItPage		pageL;
		private ItPage		pageR;
		
		private int			wThresholdDelta = 0;
		private int			minDelta = 20;
		private bool		recreatePageObjects = true;

		private PostProcessing postProcessing = new PostProcessing();

		public delegate void VoidHnd();
		public delegate void ItImageChangedHnd(ImageProcessing.ItImage itImage);

		public event EventHandler		Invalidated;
		public event ItImageChangedHnd	ItImageChanged;


		#region ScannerType
		public enum ScannerType
		{
			Bookeye2,
			Other
		}
		#endregion

		#region constructor
		public ItImage()
		{
			this.imageSettings.SettingsChanged += new ItImageSettings.VoidHnd(Settings_Changed);
				
			this.imageInfo = new ImageInfo(0, 0, 300, 300, PixelsFormat.Format24bppRgb, null, null);
			
			this.pageL = new ItPage(this, Rectangle.Empty);
			this.pageR = new ItPage(this, Rectangle.Empty);
		}

		public ItImage(FileInfo file)
			: this()
		{
			this.file = file;
			this.imageInfo = new ImageProcessing.ImageFile.ImageInfo(file);

			//this.pageL = new ItPage(this, Rectangle.Empty);
			//this.pageR = new ItPage(this, Rectangle.Empty);

			this.twoPages = (imageInfo.Width > imageInfo.Height);
			this.OpticsCenter = this.imageInfo.Size.Height / 2;

			this.Reset(this.twoPages);

			this.pageL.Invalidated += new EventHandler(Page_Invalidated);
			this.pageR.Invalidated += new EventHandler(Page_Invalidated);

			this.pageL.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
			this.pageR.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
		}

		public ItImage(Bitmap bitmap)
			: this()
		{
			this.bitmap = bitmap;
			this.imageInfo = new ImageProcessing.ImageFile.ImageInfo(bitmap);

			//this.pageL = new ItPage(this, Rectangle.Empty);
			//this.pageR = new ItPage(this, Rectangle.Empty);

			this.twoPages = (imageInfo.Width > imageInfo.Height);
			this.OpticsCenter = this.imageInfo.Size.Height / 2;

			this.Reset(this.twoPages);

			this.pageL.Invalidated += new EventHandler(Page_Invalidated);
			this.pageR.Invalidated += new EventHandler(Page_Invalidated);

			this.pageL.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
			this.pageR.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
		}

		public ItImage(FileInfo file, bool twoPages)
			: this()
		{
			this.file = file;
			this.imageInfo = new ImageProcessing.ImageFile.ImageInfo(file);

			//this.pageL = new ItPage(this, Rectangle.Empty);
			//this.pageR = new ItPage(this, Rectangle.Empty);

			this.twoPages = twoPages;
			this.OpticsCenter = this.imageInfo.Size.Height / 2;

			this.Reset(this.twoPages);

			this.pageL.Invalidated += new EventHandler(Page_Invalidated);
			this.pageR.Invalidated += new EventHandler(Page_Invalidated);

			this.pageL.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
			this.pageR.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
		}
		
		public ItImage(Bitmap bitmap, bool twoPages)
			: this()
		{
			this.bitmap = bitmap;
			this.imageInfo = new ImageProcessing.ImageFile.ImageInfo(bitmap);

			//this.pageL = new ItPage(this, Rectangle.Empty);
			//this.pageR = new ItPage(this, Rectangle.Empty);

			this.twoPages = twoPages;
			this.OpticsCenter = this.imageInfo.Size.Height / 2;

			this.Reset(this.twoPages);

			this.pageL.Invalidated += new EventHandler(Page_Invalidated);
			this.pageR.Invalidated += new EventHandler(Page_Invalidated);

			this.pageL.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
			this.pageR.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
		}

		public ItImage(FileInfo file, ScannerType scannerType)
			: this()
		{
			this.file = file;
			this.imageInfo = new ImageProcessing.ImageFile.ImageInfo(file);

			//this.pageL = new ItPage(this, Rectangle.Empty);
			//this.pageR = new ItPage(this, Rectangle.Empty);

			this.twoPages = (imageInfo.Width > imageInfo.Height);
			this.OpticsCenter = (scannerType == ScannerType.Bookeye2) ? (int)(imageInfo.Height - 8.25 * imageInfo.DpiH) : imageInfo.Height / 2;

			this.Reset(this.twoPages);

			this.pageL.Invalidated += new EventHandler(Page_Invalidated);
			this.pageR.Invalidated += new EventHandler(Page_Invalidated);

			this.pageL.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
			this.pageR.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
		}

		public ItImage(FileInfo file, bool twoPages, ScannerType scannerType)
			: this()
		{
			this.file = file;
			this.imageInfo = new ImageProcessing.ImageFile.ImageInfo(file);

			//this.pageL = new ItPage(this, Rectangle.Empty);
			//this.pageR = new ItPage(this, Rectangle.Empty);

			this.twoPages = twoPages;
			this.OpticsCenter = (scannerType == ScannerType.Bookeye2) ? (int)(imageInfo.Height - 8.25 * imageInfo.DpiH) : imageInfo.Height / 2;

			this.Reset(this.twoPages);

			this.pageL.Invalidated += new EventHandler(Page_Invalidated);
			this.pageR.Invalidated += new EventHandler(Page_Invalidated);

			this.pageL.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
			this.pageR.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
		}

		public ItImage(Bitmap bitmap, ScannerType scannerType)
			: this()
		{
			this.bitmap = bitmap;
			this.imageInfo = new ImageProcessing.ImageFile.ImageInfo(bitmap);

			//this.pageL = new ItPage(this, Rectangle.Empty);
			//this.pageR = new ItPage(this, Rectangle.Empty);

			this.twoPages = (imageInfo.Width > imageInfo.Height);
			this.OpticsCenter = (scannerType == ScannerType.Bookeye2) ? (int)(imageInfo.Height - 8.25 * imageInfo.DpiH) : imageInfo.Height / 2;

			this.Reset(this.twoPages);

			this.pageL.Invalidated += new EventHandler(Page_Invalidated);
			this.pageR.Invalidated += new EventHandler(Page_Invalidated);

			this.pageL.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
			this.pageR.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
		}

		public ItImage(Bitmap bitmap, bool twoPages, ScannerType scannerType)
			: this()
		{
			this.bitmap = bitmap;
			this.imageInfo = new ImageProcessing.ImageFile.ImageInfo(bitmap);

			//this.pageL = new ItPage(this, Rectangle.Empty);
			//this.pageR = new ItPage(this, Rectangle.Empty);

			this.twoPages = twoPages;
			this.OpticsCenter = (scannerType == ScannerType.Bookeye2) ? (int)(imageInfo.Height - 8.25 * imageInfo.DpiH) : imageInfo.Height / 2;

			this.Reset(this.twoPages);

			this.pageL.Invalidated += new EventHandler(Page_Invalidated);
			this.pageR.Invalidated += new EventHandler(Page_Invalidated);

			this.pageL.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
			this.pageR.RemoveRequest += new ItPage.ItPageHnd(Page_RemoveRequest);
		}
		#endregion


		// PUBLIC PROPERTIES
		#region public properties
		public ImageInfo	ImageInfo { get { return this.imageInfo; } }
		public Rectangle	ImageRect { get { return new Rectangle(Point.Empty, this.ImageSize); } }
		public Size			ImageSize { get { return this.imageInfo.Size; } }
		public bool			BitmapCreated { get { return this.bitmap != null; } }

		public ItPage		Page { get { return this.pageL; } }
		public ItPage		PageL { get { return this.pageL; } }
		public ItPage		PageR { get { return this.pageR; } }
		
		public Delimiters	Delimiters { get { return this.pageObjects.Delimiters; } }
		public Symbols		AllSymbols { get { return this.pageObjects.AllSymbols; } }
		public Symbols		LoneSymbols { get { return this.pageObjects.LoneSymbols; } }
		public Pictures		Pictures { get { return this.pageObjects.Pictures; } }
		public Words		Words { get { return this.pageObjects.Words; } }
		public Lines		Lines { get { return this.pageObjects.Lines; } }
		public Paragraphs	Paragraphs { get { return this.pageObjects.Paragraphs; } }

		#region TwoPages
		public bool			TwoPages 
		{ 
			get { return this.twoPages; }
			private set
			{
				if(this.twoPages != value)
				{
					this.twoPages = value;

					Settings_Changed();
				}
			}
		}
		#endregion

		#region OpticsCenter
		public int			OpticsCenter 
		{ 
			get { return this.imageSettings.OpticsCenter; } 
			set { this.imageSettings.OpticsCenter = Math.Max(0, Math.Min(this.ImageSize.Height, value)); }
		}
		#endregion

		#region File
		public FileInfo File 
		{ 
			get { return this.file; } 
			set 
			{
				if (this.file != value)
				{
					this.file = value;

					Settings_Changed();
				}
			}
		}
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
			get { return postProcessing; } 
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

		#region Bitmap
		public Bitmap Bitmap
		{
			get
			{
				return CreateBitmap();
			}
			set 
			{ 
				this.bitmap = value;
				
				if (this.bitmap != null)
					this.imageInfo = new ImageInfo(this.bitmap);
			}
		}
		#endregion

		#region Scanner
		public ScannerType Scanner
		{
			set
			{
				this.OpticsCenter = (value == ScannerType.Bookeye2) ? (int)(imageInfo.Height - 8.25 * imageInfo.DpiH) : imageInfo.Height / 2;
			}
		}
		#endregion

		#region Confidence
		public float Confidence
		{
			get
			{
				if (this.twoPages)
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

		#region Dispose()
		public void Dispose()
		{
			if (bitmap != null)
			{
				bitmap.Dispose();
				bitmap = null;
			}

			this.pageObjects.Dispose();
			DeleteRaster();
		}
		#endregion

		#region DisposeBitmap()
		public void DisposeBitmap()
		{
			if (bitmap != null)
			{
				bitmap.Dispose();
				bitmap = null;
			}
		}
		#endregion

		#region CreatePageObjects()
		public void CreatePageObjects(Rectangle clip)
		{			
			if (this.pageObjects.IsDefined || recreatePageObjects)
			{
				Bitmap source = this.Bitmap;

				if (source == null)
					throw new IpException(ErrorCode.ErrorNoImageLoaded);
				
				Bitmap raster = ImagePreprocessing.GoDarkBookfold(source, wThresholdDelta, minDelta);

				if (clip.IsEmpty == false)
					ImagePreprocessing.CutOffBorder(raster, clip);

				SaveRaster(raster);

				NoiseReduction.Despeckle(raster, NoiseReduction.DespeckleSize.Size2x2, NoiseReduction.DespeckleMode.WhiteSpecklesOnly, NoiseReduction.DespeckleMethod.Regions);
				ImagePreprocessing.GetRidOfBorders(raster);
				NoiseReduction.Despeckle(raster, NoiseReduction.DespeckleSize.Size4x4, NoiseReduction.DespeckleMode.WhiteSpecklesOnly, NoiseReduction.DespeckleMethod.Regions);

				this.pageObjects.CreatePageObjects(raster, Paging.Both);

				raster.Dispose();
				recreatePageObjects = false;
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
		public void ImportSettings(ItImage itImage)
		{
			this.twoPages = itImage.TwoPages;
			this.imageSettings.ImportSettings(itImage.imageSettings);
			
			if (this.twoPages)
			{
				this.PageL.ImportSettings(itImage.PageL);
				this.PageR.ImportSettings(itImage.PageR);
			}
			else
			{
				this.PageL.ImportSettings(itImage.Page);
				this.PageR.Reset(Rectangle.Empty);
			}
		}
		#endregion

		#region CreateBitmap()
		public Bitmap CreateBitmap()
		{
			if (this.bitmap == null)
			{
				try
				{
					this.bitmap = ImageCopier.LoadFileIndependentImage(file.FullName);
					this.imageInfo = new ImageInfo(this.bitmap);
				}
				catch (Exception ex)
				{
					throw new Exception(BIPStrings.CanTOpenImage_STR + ex.Message);
				}
			}

			return this.bitmap;
		}
		#endregion

		#region Find()
		public float Find(Operations operations)
		{
			if (this.IsFixed == false)
			{
				if (operations.CropAndDescew.Active)
				{
					return FindCropAndDeskew(operations.CropAndDescew);
				}
				else
				{
					bool disposeBitmapWhenDone = (this.bitmap == null);

					if (operations.ContentLocation.Active || operations.Skew.Active || operations.BookfoldCorrection.Active)
						CreatePageObjects(Rectangle.Empty);

					if (operations.ContentLocation.Active)
						FindContent(operations.ContentLocation);
					if (operations.Skew.Active)
						FindSkew(operations.Skew);
					if (operations.BookfoldCorrection.Active)
						FindCurving(operations.BookfoldCorrection);

					if (operations.ContentLocation.Active)
						this.SetOffsetInInches(operations.ContentLocation.OffsetX);

					if (operations.Artifacts.Active)
						FindFingers(operations.Artifacts);

					if (disposeBitmapWhenDone)
						DisposeBitmap();

					GC.Collect();
					return this.Confidence;
				}
			}

			return 1.0F;
		}
		#endregion

		#region GetResult()
		public Bitmap GetResult(int pageIndex)
		{
			try
			{
				if (this.Bitmap == null)
					throw new IpException(ErrorCode.ErrorNoImageLoaded);

				if (pageIndex == 1)
				{
					if (this.twoPages == false)
						throw new IpException(ErrorCode.InvalidParameter);

					return this.PageR.GetResult(this.Bitmap);
				}

				if (this.PageL == null)
					throw new IpException(ErrorCode.InvalidParameter);

				return this.PageL.GetResult(this.Bitmap);
			}
			catch (Exception ex)
			{
				ItPage page = (pageIndex > 0) ? this.PageR : this.PageL;
				throw new Exception(string.Format("ItImage, GetResult(); index: {0}, TwoPages: {1}, Clip: {2}, Skew: {3}, Error: {4}.", 
					pageIndex, twoPages, page.ClipRect, page.Skew, ex.Message));
			}
		}
		#endregion

		#region Reset()
		public bool Reset(bool twoPages)
		{
			bool settingsChanged = (this.twoPages != twoPages);
			this.twoPages = twoPages;
			
			if (this.twoPages)
			{	
				int twoPerc = Convert.ToInt32((float)(this.ImageSize.Width * 0.02f));
				this.PageL.Reset(Rectangle.FromLTRB(twoPerc * 2, twoPerc, (this.ImageSize.Width / 2) - twoPerc, this.ImageSize.Height - twoPerc));
				this.PageR.Reset(Rectangle.FromLTRB((this.ImageSize.Width / 2) + twoPerc, twoPerc, this.ImageSize.Width - (twoPerc * 2), this.ImageSize.Height - twoPerc));
			}
			else
			{
				this.PageL.Reset(new Rectangle(0, 0, this.ImageSize.Width, this.ImageSize.Height));
				this.PageR.Reset(Rectangle.Empty);
			}

			if (settingsChanged)
				Settings_Changed();

			return true;
		}
		#endregion

		//PAGES

		#region GetPage()
		public ItPage GetPage(Point imagePoint)
		{
			if (this.twoPages)
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

		public ItPage GetPage(Rectangle rect)
		{
			if (this.twoPages)
			{
				if (Rectangle.Intersect(this.PageR.ClipRect, rect) != Rectangle.Empty)
					return this.PageR;
				if (Rectangle.Intersect(this.PageL.ClipRect, rect) != Rectangle.Empty)
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
		public ItPage GetLeftPage()
		{
			if (this.twoPages)
			{
				if (this.PageL.Clip.Center.X <= this.PageR.Clip.Center.X)
					return this.PageL;
				else
					return this.PageR;
			}

			if(this.ImageSize.Width < this.ImageSize.Height || this.PageL.Clip.Center.X <= this.ImageSize.Width / 2.0)
				return this.PageL;

			return null;
		}
		#endregion

		#region GetRightPage()
		public ItPage GetRightPage()
		{
			if (this.twoPages)
			{
				if (this.PageL.Clip.Center.X <= this.PageR.Clip.Center.X)
					return this.PageR;
				else
					return this.PageL;
			}
			else if (this.ImageSize.Width >= this.ImageSize.Height && this.PageL.Clip.Center.X > this.ImageSize.Width / 2.0)
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
			if (this.twoPages)
			{
				if (this.PageL.Clip.Center.X > this.PageR.Clip.Center.X)
				{
					ItPage page = this.PageR.Clone();
					this.PageR.ImportSettings(this.PageL);
					this.PageL.ImportSettings(page);
					
					Settings_Changed();
				}
			}
		}
		#endregion

		#region GetPages()
		public List<ItPage> GetPages()
		{
			List<ItPage> list = new List<ItPage>();

			if (this.twoPages)
			{
				list.AddRange(new ItPage[] { this.PageL, this.PageR });
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
		public void RemovePage(ItPage page)
		{
			if ((page != null) && (this.PageL == page || this.PageR == page))
			{
				if (this.TwoPages)
				{
					this.twoPages = false;

					if (page == this.PageL)
					{
						this.PageL.ImportSettings(this.PageR);
						this.PageR.Reset(Rectangle.Empty);
					}
					else
					{
						this.PageR.Reset(Rectangle.Empty);
					}

					if (ItImageChanged != null)
						ItImageChanged(this);
				}
				else
				{
					if(page == this.PageL)
						this.PageL.Reset(this.ImageRect);
				}
			}
		}
		#endregion

		#region SetTo1Clip()
		public ItPage SetTo1Clip(Rectangle rect)
		{		
			this.twoPages = false;
			rect = Rectangle.Intersect(rect, ImageRect);

			//if (rect.Width > 100 && rect.Width > 100)
				this.PageL.SetClip(rect, true);
				
			this.PageR.Reset(Rectangle.Empty);
			return this.PageL;
		}

		public void SetTo1Clip()
		{
			this.twoPages = false;
		}

		/*public void SetToClip(ItPage page, Rectangle rect)
		{
			this.twoPages = false;
			page.SetClip(rect);
		}*/
		#endregion

		#region SetTo2Clips()
		public void SetTo2Clips(Rectangle rectL, Rectangle rectR)
		{
			this.TwoPages = true;
			rectL = Rectangle.Intersect(rectL, ImageRect);
			rectR = Rectangle.Intersect(rectR, ImageRect);

			this.PageL.SetClip(rectL, true);
			this.PageR.SetClip(rectR, true);
		}

		public void SetTo2Clips()
		{
			this.TwoPages = true;
		}

		public void SetTo2Clips(Rectangle rect, bool leftPage)
		{
			this.twoPages = true;

			if (leftPage)
			{
				//ItPage page = this.PageR.Clone();
				this.PageR.ImportSettings(this.PageL);
				//this.PageL.ImportSettings(page);

				this.PageL.SetClip(rect, true);
			}
			else
			{
				this.PageR.SetClip(rect, true);
			}
		}
		#endregion

		#region SetClipsSize()
		public void SetClipsSize(Size size)
		{
			if (this.twoPages)
			{
				this.PageL.SetClipSize(size);
				this.PageR.SetClipSize(size);
			}
			else
				this.Page.SetClipSize(size);
		}
		#endregion

		#region ResetClip()
		public bool ResetClip(ItPage page)
		{
			if (this.twoPages)
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

		public bool ResetClip(Point point)
		{
			if (this.twoPages)
			{
				throw new IpException(ErrorCode.CantRemovePageFrom2PageImage);
			}
			
			ItPage page = this.GetPage(point);
			if ((page != null) && (page == this.PageL))
			{
				this.PageL.ResetClip();
				return true;
			}
			return false;
		}
		#endregion

		#region SetOffset()
		public void SetOffset(int offset)
		{
			if (this.twoPages)
			{
				this.PageL.SetOffset(offset);
				this.PageR.SetOffset(offset);
			}
			else
				this.Page.SetOffset(offset);
		}
		#endregion

		#region SetOffsetInInches()
		public void SetOffsetInInches(float offsetF)
		{
			SetOffset(Convert.ToInt32(offsetF * imageInfo.DpiH));
		}
		#endregion

		//SKEW

		#region SetSkew()
		/*public void SetSkew(ItPage page, double rotation)
		{
			page.SetSkew(rotation);
		}*/
		#endregion


		//CURVE CORRECTION

		#region SetPageBookfolding()
		public void SetPageBookfolding(PageBookfolding bfParamsL, PageBookfolding bfParamsR)
		{
			if (!this.twoPages)
				throw new IpException(ErrorCode.BfJust1PageAllocated);

			this.PageL.SetPageBookfolding(bfParamsL.TopCurve, bfParamsL.BottomCurve, bfParamsL.Confidence);
			this.PageR.SetPageBookfolding(bfParamsR.TopCurve, bfParamsR.BottomCurve, bfParamsR.Confidence);
		}

		public void SetPageBookfolding(ItPage page, PageBookfolding bfParams)
		{
			if (this.twoPages)
				throw new IpException(ErrorCode.ConstructPagesFirst);

			page.SetPageBookfolding(bfParams.TopCurve, bfParams.BottomCurve, bfParams.Confidence);
		}
		#endregion

		#region ChangeBookfoldPoint()
		public bool ChangeBookfoldPoint(ItPage page, Curve curve, int index, Point newPoint)
		{
			if (this.PageL == page)
				return this.PageL.ChangeBookfoldPoint(curve, index, newPoint);

			if ((this.PageR == page) && this.twoPages)
				return this.PageR.ChangeBookfoldPoint(curve, index, newPoint);

			return false;
		}
		#endregion

		#region ResetPageBookfolding()
		public bool ResetPageBookfolding()
		{
			return (this.PageL.ResetPageBookfolding() || this.PageR.ResetPageBookfolding());
		}

		public bool ResetPageBookfolding(ItPage page)
		{
			return page.ResetPageBookfolding();
		}

		public bool ResetPageBookfolding(Point point)
		{
			ItPage page = this.GetPage(point);
			if (page != null)
			{
				return page.ResetPageBookfolding();
			}
			return false;
		}
		#endregion

		#region ShiftBookfoldPoints()
		public bool ShiftBookfoldPoints(ItPage page, Curve curve, int dy)
		{
			if (this.PageL == page)
				return this.PageL.ShiftBookfoldPoints(curve, dy);
			
			if ((this.PageR == page) && this.twoPages)
				return this.PageR.ShiftBookfoldPoints(curve, dy);

			return false;
		}
		#endregion

		//FINGERS	
	
		#region GetFinger()
		public Finger GetFinger(Point point)
		{
			Finger finger = this.PageL.GetFinger(point);
			
			if (finger == null && this.twoPages)
				finger = this.PageR.GetFinger(point);

			return finger;
		}
		#endregion

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
		public void AddFinger(Finger finger)
		{
			ItPage page = GetPage(finger.RectangleNotSkewed);

			if (page != null && page.Fingers.Contains(finger) == false)
				page.AddFinger(finger);
			else
				throw new IpException(ErrorCode.FingerRegionNotInPage);
		}

		public Finger AddFinger(Rectangle rect)
		{
			ItPage itPage = this.GetPage(rect);
			
			if (itPage != null)
				return itPage.AddFinger(rect);
			
			return null;
		}
		#endregion

		#region AddFingers()
		public void AddFingers(ArrayList fingers)
		{
			foreach (Finger finger in fingers)
			{
				ItPage page = this.GetPage(finger.RectangleNotSkewed);

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

		#region ChangeFingerColor()
		public bool ChangeFingerColor(Finger finger, Color color)
		{
			return false;
		}
		#endregion

		#region ChangeFingerRect
		/*public bool ChangeFingerRect(Finger finger, Rectangle rect)
		{
			return (this.PageL.ChangeFingerRect(finger, rect) || this.PageR.ChangeFingerRect(finger, rect));
		}*/
		#endregion

		#region RemoveFinger()
		public bool RemoveFinger(Finger finger)
		{
			return (this.PageL.RemoveFinger(finger) || this.PageR.RemoveFinger(finger));
		}

		public Finger RemoveFinger(Point point)
		{
			Finger finger = this.PageL.RemoveFinger(point);
			
			if (finger == null)
				finger = this.PageR.RemoveFinger(point);
			
			return finger;
		}
		#endregion

		#region ResizeSettings()
		public void ResizeSettings(double zoom)
		{
			if (this.IsFixed == false)
			{
				pageObjects.ResizeSettings(zoom);
				this.imageSettings.ResizeSettings(zoom);
				pageL.ResizeSettings(zoom);

				if (TwoPages)
					pageR.ResizeSettings(zoom);
			}
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
			if (this.twoPages)
			{
				float confidence = 0f;
				Rectangle clip = new Rectangle(Point.Empty, this.ImageSize);

				Pages pages = ObjectLocator.FindPages(this.LoneSymbols, this.Words, this.Pictures, this.Delimiters, this.Paragraphs, this.ImageSize, ref confidence);
				
				if (pages.Count >= 2)
				{
					Rectangle page1Rect = pages[0].Rectangle;

					this.PageL.SetClip(Rectangle.Intersect(clip, page1Rect), false);
					this.PageL.Clip.SetContent(page1Rect, confidence);

					Rectangle page2Rect = pages[1].Rectangle;

					this.PageR.SetClip(Rectangle.Intersect(clip, page2Rect), false);
					this.PageR.Clip.SetContent(page2Rect, confidence);
					
					return confidence;
				}
				if (pages.Count == 1)
				{
					Rectangle pageRect = pages[0].Rectangle;

					this.SetTo1Clip(Rectangle.Intersect(clip, pageRect));
					this.PageL.Clip.SetContent(pageRect, confidence);

					return confidence;
				}

				this.SetTo1Clip(new Rectangle(Point.Empty, this.ImageSize));
				return confidence;
			}
			else if (this.PageL == null)
				throw new IpException(ErrorCode.InvalidParameter);
			else
			{
				//this.PageL.FindContent(new Rectangle(0, 0, this.imageInfo.Width, this.imageInfo.Height));
				
				float confidence = 0f;
				Page page = ObjectLocator.FindPage(this.LoneSymbols, this.Paragraphs, this.Pictures, this.Delimiters, this.ImageRect, ref confidence);

				if (page != null)
				{
					Rectangle pageRect = page.Rectangle;

					this.pageL.SetClip(Rectangle.Intersect(this.ImageRect, pageRect), false);
					this.pageL.Clip.SetContent(pageRect, confidence);
				}
				else
				{
					this.pageL.SetClip(this.ImageRect, false);
					this.pageL.Clip.SetContent(null, 0.0F);
				}
			}
			
			return 1f;
		}
		#endregion

		#region FindSkew()
		private void FindSkew(Operations.SkewParams parameters)
		{
			if (this.twoPages)
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
			if (this.twoPages)
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
		private void FindFingers(Operations.ArtifactsParams parameters)
		{
			if (this.RasterFile.Exists == false)
				CreatePageObjects(Rectangle.Empty);
			
			Bitmap raster = new Bitmap(this.RasterFile.FullName);
			
			if (this.twoPages)
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
		/// <summary>
		/// 
		/// </summary>
		/// <param name="margin">Margin in inches. Negative number shrinks zone</param>
		/// <returns>Returns confidence factor from 0 to 1.</returns>
		private float FindCropAndDeskew(Operations.CropAndDescewParams parameters)
		{
			bool disposeBitmapWhenDone = (this.bitmap == null);

			float confidence = FindCropAndDeskew(this.Bitmap, parameters);

			if (disposeBitmapWhenDone)
				DisposeBitmap();

			return confidence;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="offset">Margin in inches. Negative number shrinks zone</param>
		/// <returns>Returns confidence factor from 0 to 1.</returns>
		private float FindCropAndDeskew(Bitmap source, Operations.CropAndDescewParams parameters)
		{
			byte confidence = 0;
			Rectangle clip = new Rectangle(0, 0, source.Width, source.Height);

			clip.Inflate(-parameters.OffsetX, -parameters.OffsetY);

			ObjectByCorners obj = CropAndDeskew.GetParams(bitmap, parameters.ThresholdColor, true, out confidence, 
				parameters.MinAngle, clip, parameters.GhostLines, parameters.GlLowThreshold, parameters.GlHighThreshold,
				parameters.GlToCheck, parameters.GlMaxDelta, Convert.ToInt16(parameters.MarginX * source.HorizontalResolution),
				Convert.ToInt16(parameters.MarginY * source.VerticalResolution));

			Reset(false);
			Rectangle clipFound = new Rectangle(Rotation.RotatePoint(obj.UlCorner, obj.Centroid, obj.Skew), new Size(obj.Width, obj.Height));
			SetTo1Clip(clipFound);
			this.Page.Clip.SetContent(clipFound, confidence / 100.0F);
			this.Page.SetSkew(obj.Skew, confidence / 100.0F);
			
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
			Console.WriteLine("ItImage, SaveRaster(): {0}", DateTime.Now.Subtract(start).ToString());
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

		#region Page_Invalidated()
		private void Page_Invalidated(object sender, EventArgs args)
		{
			Changed();
		}
		#endregion

		#region Settings_Changed()
		private void Settings_Changed()
		{
			if (ItImageChanged != null)
				ItImageChanged(this);
			
			Changed();
		}
		#endregion

		#region Changed()
		private void Changed()
		{
			if (Invalidated != null)
				Invalidated(this, null);
		}
		#endregion

		#region Page_RemoveRequest()
		void Page_RemoveRequest(ItPage itPage)
		{
			RemovePage(itPage);
		}
		#endregion

		#endregion

	}

}
