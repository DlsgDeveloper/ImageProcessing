using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Windows.Forms ;
using System.Collections;
using ImageProcessing.PageObjects;
using System.Collections.Generic;
using ImageProcessing.Languages;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Histogram.
	/// </summary>
	public class Histogram
	{
		uint[]		arrayR = new uint[256] ;
		uint[]		arrayG = new uint[256] ;
		uint[]		arrayB = new uint[256] ;
		byte		extremeR = 0 ;
		byte		extremeG = 0 ;
		byte		extremeB = 0 ;
		byte		secondExtremeR = 0 ;
		byte		secondExtremeG = 0 ;
		byte		secondExtremeB = 0 ;
		byte		thresholdR ;
		byte		thresholdG ;
		byte		thresholdB ;
		byte		maxDeltaR = 0 ;
		byte		maxDeltaG = 0 ;
		byte		maxDeltaB = 0 ;
		double		meanR = 127;
		double		meanG = 127;
		double		meanB = 127;

		byte		medianR = 127;
		byte		medianG = 127;
		byte		medianB = 127;

		Form form = null;
		TextBox		textBoxPosition = null ;
		TextBox		textBoxValue = null ;

		float		graphZoom = 2;

		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		
		#region constructor
		/// <summary>
		/// Call to get progress monitoring on the histogram computation. Call 'Compute' to get values.
		/// </summary>
		public Histogram()
		{
		}

		public Histogram(string filePath)
			:this(filePath, Rectangle.Empty)
		{
		}

		public Histogram(string filePath, Rectangle clip)
		{
			using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(filePath))
			{
				Load(itDecoder, clip);
			}
		}

		public Histogram(ImageProcessing.BigImages.ItDecoder itDecoder)
			: this(itDecoder, Rectangle.Empty)
		{
		}

		public Histogram(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			Load(itDecoder, clip);
		}

		public Histogram(Bitmap image)
			: this(image, Rectangle.Empty)
		{
		}

		public Histogram(Bitmap image, Rectangle clip)
		{
			Load(image, clip);
		}
		#endregion


		//	PUBLIC PROPERTIES
		#region public properties

		public byte		ExtremeR		{ get { return this.extremeR ; } }
		public byte		ExtremeG		{ get { return this.extremeG ; } }
		public byte		ExtremeB		{ get { return this.extremeB ; } }
		public Color	Extreme			{ get { return Color.FromArgb(ExtremeR, ExtremeG, ExtremeB); } }
		public byte		SecondExtremeR	{ get { return this.secondExtremeR; } }
		public byte		SecondExtremeG	{ get { return this.secondExtremeG ; } }
		public byte		SecondExtremeB	{ get { return this.secondExtremeB ; } }
		public Color	SecondExtreme	{ get { return Color.FromArgb(SecondExtremeR, SecondExtremeG, SecondExtremeB); } }
		public byte		MaxDeltaR		{ get { return this.maxDeltaR; } }
		public byte		MaxDeltaG		{ get { return this.maxDeltaG ; } }
		public byte		MaxDeltaB		{ get { return this.maxDeltaB ; } }
		public byte		MaxDelta		{ get { return Convert.ToByte((this.MaxDeltaR + this.MaxDeltaG + this.MaxDeltaB) / 3); } }
		public uint[]	ArrayR			{ get { return this.arrayR; } }
		public uint[]	ArrayG			{ get { return this.arrayG ; } }
		public uint[]	ArrayB			{ get { return this.arrayB ; } }

		public byte		ThresholdR		{ get { return this.thresholdR ; } }
		public byte		ThresholdG		{ get { return this.thresholdG ; } }
		public byte		ThresholdB		{ get { return this.thresholdB ; } }
		//public byte ThresholdR { get { return GetBackgroundThreshold(arrayR); } }
		//public byte ThresholdG { get { return GetBackgroundThreshold(arrayG); } }
		//public byte ThresholdB { get { return GetBackgroundThreshold(arrayB); } }
		public Color Threshold { get { return Color.FromArgb(ThresholdR, ThresholdG, ThresholdB); } }

		/// <summary>
		/// average R
		/// </summary>
		public double		MeanR { get { return this.meanR; } }
		/// <summary>
		/// average G
		/// </summary>
		public double		MeanG { get { return this.meanG; } }
		/// <summary>
		/// average B
		/// </summary>
		public double		MeanB { get { return this.meanB; } }

		/// <summary>
		/// Numeric value separating the higher half of a sample from the lower half.
		/// </summary>
		public byte			MedianR { get { return this.medianR; } }
		/// <summary>
		/// Numeric value separating the higher half of a sample from the lower half.
		/// </summary>
		public byte			MedianG { get { return this.medianG; } }
		/// <summary>
		/// Numeric value separating the higher half of a sample from the lower half.
		/// </summary>
		public byte			MedianB { get { return this.medianB; } }

		public ImageProcessing.ColorD Mean { get { return new ImageProcessing.ColorD(this.meanR, this.meanG, this.meanB); } }

		#region DarkestR
		public byte DarkestR
		{
			get
			{
				for (int i = 0; i < 256; i++)
					if (arrayR[i] > 0)
						return (byte) i;

				return 0;
			}
		}
		#endregion

		#region DarkestG
		public byte DarkestG
		{
			get
			{
				for (int i = 0; i < 256; i++)
					if (arrayG[i] > 0)
						return (byte)i;

				return 0;
			}
		}
		#endregion

		#region DarkestB
		public byte DarkestB
		{
			get
			{
				for (int i = 0; i < 256; i++)
					if (arrayB[i] > 0)
						return (byte)i;

				return 0;
			}
		}
		#endregion

		#region LightestR
		public byte LightestR
		{
			get
			{
				for (int i = 255; i >= 0; i--)
					if (arrayR[i] > 0)
						return (byte)i;

				return 255;
			}
		}
		#endregion

		#region LightestG
		public byte LightestG
		{
			get
			{
				for (int i = 255; i >= 0; i--)
					if (arrayG[i] > 0)
						return (byte)i;

				return 255;
			}
		}
		#endregion

		#region LightestB
		public byte LightestB
		{
			get
			{
				for (int i = 255; i >= 0; i--)
					if (arrayB[i] > 0)
						return (byte)i;

				return 255;
			}
		}
		#endregion

		#endregion


		//	PUBLIC METHODS
		#region public methods

		#region GetBlackWhiteHistogram()
		public static Histogram GetBlackWhiteHistogram()
		{
			Histogram h = new Histogram();
			h.arrayR[0] = 1;
			h.arrayG[0] = 1;
			h.arrayB[0] = 1;

			h.extremeR = 255;
			h.extremeG = 255;
			h.extremeB = 255;
			h.secondExtremeR = 0;
			h.secondExtremeG = 0;
			h.secondExtremeB = 0;
			h.thresholdR = 127;
			h.thresholdG = 127;
			h.thresholdB = 127;
			h.maxDeltaR = 0;
			h.maxDeltaG = 0;
			h.maxDeltaB = 0;
			h.meanR = 127;
			h.meanG = 127;
			h.meanB = 127;

			h.medianR = 127;
			h.medianG = 127;
			h.medianB = 127;

			return h;
		}
		#endregion

		#region Compute()
		public void Compute(ImageProcessing.BigImages.ItDecoder itDecoder)
		{
			Compute(itDecoder, Rectangle.Empty);
		}

		public void Compute(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			Load(itDecoder, clip);

			if (ProgressChanged != null)
				ProgressChanged(1);
		}
		#endregion

		#region Show()
		public void Show()
		{
			if(this.form == null)
			{
				this.form = new Form() ;
				this.form.Name = BIPStrings.Histogram_STR ;
				this.form.StartPosition = FormStartPosition.Manual ;
				this.form.Size = new Size(910, 800) ;
				this.form.MaximumSize = new Size(910, 1200) ;
				this.form.MinimumSize = new Size(910, 200) ;
				this.form.Location = new Point(0, 0) ;
				this.form.FormBorderStyle = FormBorderStyle.SizableToolWindow ;
				this.form.BackColor = Color.White ;

				this.form.Paint += new PaintEventHandler(Form_Paint);

				this.textBoxPosition = new TextBox() ;
				this.textBoxValue = new TextBox() ;

				Label		labelThresholdR = new Label() ;
				Label		labelExtremeR = new Label() ;
				Label		label2ndExtremeR = new Label() ;
				Label		labelMaxDeltaR = new Label() ;

				Label		labelThresholdG = new Label() ;
				Label		labelExtremeG = new Label() ;
				Label		label2ndExtremeG = new Label() ;
				Label		labelMaxDeltaG = new Label() ;

				Label		labelThresholdB = new Label() ;
				Label		labelExtremeB = new Label() ;
				Label		label2ndExtremeB = new Label() ;
				Label		labelMaxDeltaB = new Label() ;

				Button		buttonPlus = new Button();
				Button		buttonMinus = new Button();
			
				this.form.Controls.Add(this.textBoxPosition) ;
				this.form.Controls.Add(this.textBoxValue) ;
				this.form.Controls.AddRange(new Control[] {labelThresholdR, labelExtremeR, label2ndExtremeR, labelMaxDeltaR, 
				labelThresholdG, labelExtremeG, label2ndExtremeG, labelMaxDeltaG, 
				labelThresholdB, labelExtremeB, label2ndExtremeB, labelMaxDeltaB, }) ;
				this.form.Controls.Add(buttonPlus);
				this.form.Controls.Add(buttonMinus);

				this.textBoxPosition.Bounds = new Rectangle(790, 30, 110, 20) ;
				this.textBoxValue.Bounds = new Rectangle(790, 50, 110, 20) ;
				
				labelThresholdR.Bounds = new Rectangle(790, 80, 110, 20) ;
				labelExtremeR.Bounds = new Rectangle(790, 100, 110, 20) ;
				label2ndExtremeR.Bounds = new Rectangle(790, 120, 110, 20) ;
				labelMaxDeltaR.Bounds = new Rectangle(790, 140, 110, 20) ;

				labelThresholdR.Bounds = new Rectangle(790, 170, 110, 20) ;
				labelExtremeR.Bounds = new Rectangle(790, 190, 110, 20) ;
				label2ndExtremeR.Bounds = new Rectangle(790, 210, 110, 20) ;
				labelMaxDeltaR.Bounds = new Rectangle(790, 230, 110, 20) ;

				labelThresholdG.Bounds = new Rectangle(790, 260, 110, 20) ;
				labelExtremeG.Bounds = new Rectangle(790, 280, 110, 20) ;
				label2ndExtremeG.Bounds = new Rectangle(790, 300, 110, 20) ;
				labelMaxDeltaG.Bounds = new Rectangle(790, 320, 110, 20) ;

				labelThresholdB.Bounds = new Rectangle(790, 350, 110, 20) ;
				labelExtremeB.Bounds = new Rectangle(790, 370, 110, 20) ;
				label2ndExtremeB.Bounds = new Rectangle(790, 390, 110, 20) ;
				labelMaxDeltaB.Bounds = new Rectangle(790, 410, 110, 20) ;

				labelThresholdR.Text = string.Format(BIPStrings.Threshold_STR+" R: {0}",  this.ThresholdR) ;
				labelExtremeR.Text = string.Format(BIPStrings.Extreme_STR+" R: {0}",  this.ExtremeR) ;
				label2ndExtremeR.Text = string.Format(BIPStrings.NdExtreme_STR+" R: {0}",  this.SecondExtremeR) ;
				labelMaxDeltaR.Text = string.Format(BIPStrings.MinRangeR_STR+"{0}",  this.MaxDeltaR) ;

				labelThresholdG.Text = string.Format(BIPStrings.Threshold_STR+" G: {0}",  this.ThresholdG) ;
				labelExtremeG.Text = string.Format(BIPStrings.Extreme_STR+" G: {0}",  this.ExtremeG) ;
				label2ndExtremeG.Text = string.Format(BIPStrings.NdExtreme_STR+" G: {0}",  this.SecondExtremeG) ;
				labelMaxDeltaG.Text = string.Format(BIPStrings.MinRange_STR+" G: {0}",  this.MaxDeltaG) ;

				labelThresholdB.Text = string.Format(BIPStrings.Threshold_STR+" B: {0}",  this.ThresholdB) ;
				labelExtremeB.Text = string.Format(BIPStrings.Extreme_STR+" B: {0}",  this.ExtremeB) ;
				label2ndExtremeB.Text = string.Format(BIPStrings.NdExtreme_STR+" B: {0}",  this.SecondExtremeB) ;
				labelMaxDeltaB.Text = string.Format(BIPStrings.MinRange_STR+" B: {0}",  this.MaxDeltaB) ;

				buttonPlus.Bounds = new Rectangle(880, 700, 20, 20);
				buttonPlus.Anchor = AnchorStyles.Bottom & AnchorStyles.Right;
				buttonPlus.Text = "+";
				buttonPlus.Visible = true;
				buttonPlus.Click += new EventHandler(Plus_Click);
				buttonMinus.Bounds = new Rectangle(880,730, 20, 20);
				buttonMinus.Anchor = AnchorStyles.Bottom & AnchorStyles.Right;
				buttonMinus.Text = "-";
				buttonMinus.Visible = true;
				buttonMinus.Click += new EventHandler(Minus_Click);

				this.form.MouseMove += new MouseEventHandler(Form_MouseMove);
			}

			form.ShowDialog() ;
		}
		#endregion

		#region ToGray()
		public static byte ToGray(Color color)
		{
			return Convert.ToByte(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
		}

		public static byte ToGray(int r, int g, int b)
		{
			return Convert.ToByte(0.299 * r + 0.587 * g + 0.114 * b);
		}
		#endregion

		#region GetOtsuBackground()
		public Color GetOtsuBackground()
		{
			return Color.FromArgb(GetOtsuBackground(arrayR), GetOtsuBackground(arrayG), GetOtsuBackground(arrayB));
		}
		#endregion

		#region GetHistogramMean()
		public static ColorD GetHistogramMean(Bitmap bitmap)
		{
			Histogram h = new Histogram(bitmap);

			return h.Mean;
		}
		#endregion

		#endregion


		//	PRIVATE METHODS
		#region private methods

		#region Load()
		private void Load(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);
			else
				clip.Intersect(new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));
			
			switch (itDecoder.PixelsFormat)
			{
				case PixelsFormat.Format32bppRgb:
					GetHistogram32bpp(itDecoder, clip);
					break;
				case PixelsFormat.Format24bppRgb:
					GetHistogram24bpp(itDecoder, clip);
					break;
				case PixelsFormat.Format8bppIndexed:
					GetHistogram8bpp(itDecoder, clip);
					break;
				case PixelsFormat.Format8bppGray:
					GetHistogram8bpp(itDecoder, clip);
					break;
				case PixelsFormat.FormatBlackWhite:
					GetHistogram1bpp(itDecoder, clip);
					break;
				default:
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}

			DoTheComputations();
		}

		private void Load(Bitmap bitmap, Rectangle clip)
		{
			BitmapData bitmapData = null;

			if (clip.IsEmpty)
				clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			else
				clip.Intersect(new Rectangle(0, 0, bitmap.Width, bitmap.Height));

			try
			{
				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadOnly, bitmap.PixelFormat);

				switch (bitmapData.PixelFormat)
				{
					case PixelFormat.Format32bppRgb:
					case PixelFormat.Format32bppArgb:
						GetHistogram32bpp(bitmapData);
						break;
					case PixelFormat.Format24bppRgb:
						GetHistogram24bpp(bitmapData);
						break;
					case PixelFormat.Format8bppIndexed:
						if (Misc.IsGrayscale(bitmap))
							GetHistogram8bppGray(bitmapData);
						else
							GetHistogram8bppIndexed(bitmapData, bitmap.Palette.Entries);
						break;
					case PixelFormat.Format1bppIndexed:
						GetHistogram1bpp(bitmapData);
						break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			finally
			{
				if (bitmapData != null)
				{
					bitmap.UnlockBits(bitmapData);
					bitmapData = null;
				}
			}

			DoTheComputations();
		}
		#endregion

		#region DoTheComputations()
		public void DoTheComputations()
		{
			short i;
			uint[] arrayTmp = new uint[256];

			//local extreme
			for (i = 1; i < 256; i++)
			{
				if (arrayR[extremeR] < arrayR[i])
					extremeR = (byte)i;

				if (arrayG[extremeG] < arrayG[i])
					extremeG = (byte)i;

				if (arrayB[extremeB] < arrayB[i])
					extremeB = (byte)i;
			}

			// second local extreme
			for (i = 0; i < 256; i++)
			{
				if ((i < extremeR - 60) || (i > extremeR + 60))
					arrayTmp[i] = arrayR[i];
				else
					arrayTmp[i] = 0;
			}

			for (i = 0; i < 256; i++)
				if (arrayTmp[secondExtremeR] < arrayTmp[i])
					secondExtremeR = (byte)i;

			for (i = 0; i < 256; i++)
			{
				if ((i < extremeG - 60) || (i > extremeG + 60))
					arrayTmp[i] = arrayG[i];
				else
					arrayTmp[i] = 0;
			}

			for (i = 0; i < 256; i++)
				if (arrayTmp[secondExtremeG] < arrayTmp[i])
					secondExtremeG = (byte)i;

			for (i = 0; i < 256; i++)
			{
				if ((i < extremeB - 60) || (i > extremeB + 60))
					arrayTmp[i] = arrayB[i];
				else
					arrayTmp[i] = 0;
			}

			for (i = 0; i < 256; i++)
				if (arrayTmp[secondExtremeB] < arrayTmp[i])
					secondExtremeB = (byte)i;

			//threshold			
			this.thresholdR = ImageProcessing.Histogram.GetOtsuThreshold(this.arrayR);
			this.thresholdG = ImageProcessing.Histogram.GetOtsuThreshold(this.arrayG);
			this.thresholdB = ImageProcessing.Histogram.GetOtsuThreshold(this.arrayB);

			//max delta
			byte min = (byte)(((extremeR < secondExtremeR) ? extremeR : secondExtremeR) + 2);
			byte max = (byte)(((extremeR >= secondExtremeR) ? extremeR : secondExtremeR) - 2);
			uint maxDeltaValue = 0;

			maxDeltaR = min;

			for (i = (short)(min + 1); i < max; i++)
			{
				if (maxDeltaValue < ((int)(arrayR[i + 1] - arrayR[i - 1])))
				{
					maxDeltaValue = (uint)arrayR[i + 1] - arrayR[i - 1];
					maxDeltaR = (byte)i;
				}
			}

			min = (byte)(((extremeG < secondExtremeG) ? extremeG : secondExtremeG) + 2);
			max = (byte)(((extremeG >= secondExtremeG) ? extremeG : secondExtremeG) - 2);
			maxDeltaG = min;
			maxDeltaValue = 0;

			for (i = (short)(min + 1); i < max; i++)
			{
				if (maxDeltaValue < ((int)(arrayG[i + 1] - arrayG[i - 1])))
				{
					maxDeltaValue = (uint)arrayG[i + 1] - arrayG[i - 1];
					maxDeltaG = (byte)i;
				}
			}

			min = (byte)(((extremeB < secondExtremeB) ? extremeB : secondExtremeB) + 2);
			max = (byte)(((extremeB >= secondExtremeB) ? extremeB : secondExtremeB) - 2);
			maxDeltaB = min;
			maxDeltaValue = 0;

			for (i = (short)(min + 1); i < max; i++)
			{
				if (maxDeltaValue < ((int)(arrayB[i + 1] - arrayB[i - 1])))
				{
					maxDeltaValue = (uint)arrayB[i + 1] - arrayB[i - 1];
					maxDeltaB = (byte)i;
				}
			}

			//means
			this.meanR = GetMean(arrayR);
			this.meanG = GetMean(arrayG);
			this.meanB = GetMean(arrayB);

			//medians
			this.medianR = GetMedian(arrayR);
			this.medianG = GetMedian(arrayG);
			this.medianB = GetMedian(arrayB);
		}
		#endregion

		#region GetHistogram32bpp()
		private void GetHistogram32bpp(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			ClearArrays();

			unsafe
			{
				Bitmap bitmap = null;
				BitmapData bitmapData = null;

				int stripHeightMax = ImageProcessing.Misc.GetStripHeightMax(itDecoder);

				for (int stripY = clip.Y; stripY < clip.Bottom; stripY = stripY + stripHeightMax)
				{
					int bottom = Math.Min(stripY + stripHeightMax, clip.Bottom);

					try
					{
						bitmap = itDecoder.GetClip(Rectangle.FromLTRB(clip.X, stripY, clip.Right, bottom));
						bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

						int stride = bitmapData.Stride;
						int width = bitmapData.Width;
						int height = bitmapData.Height;

						int xJump = (width / 1500) + 1;
						int yJump = (height / 1500) + 1;
						int xJumpBytes = xJump * 4;

						byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
						byte* pCurrent;

						for (int y = 0; y < height; y = y + yJump)
						{
							pCurrent = pOrig + y * stride;

							for (int x = 0; x < width; x = x + xJump)
							{
								//pixels are stored in order: blue, green, red
								arrayB[*pCurrent]++;
								arrayG[pCurrent[1]]++;
								arrayR[pCurrent[2]]++;

								pCurrent += xJumpBytes;
							}
						}

						//smoothing
						if (((width / xJump) * (height / yJump)) > 1000)
						{
							arrayR = GetSmoothArray(arrayR);
							arrayG = GetSmoothArray(arrayG);
							arrayB = GetSmoothArray(arrayB);
						}
					}
					finally
					{
						if (bitmapData != null)
						{
							bitmap.UnlockBits(bitmapData);
							bitmapData = null;
						}

						itDecoder.ReleaseAllocatedMemory(bitmap);
						bitmap = null;
					}

					if (ProgressChanged != null)
						ProgressChanged((bottom) / (float)clip.Height);
				}
			}
		}

		private void GetHistogram32bpp(BitmapData bitmapData)
		{
			int			stride = bitmapData.Stride;
			int			width = bitmapData.Width;
			int			height = bitmapData.Height;

			int			xJump = (width / 1500) + 1;
			int			yJump = (height / 1500) + 1;
			int			xJumpBytes = xJump * 4;

			ClearArrays(); 
			
			unsafe
			{
				byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
				byte* pCurrent;

				for (int y = 0; y < height; y = y + yJump)
				{
					pCurrent = pOrig + y * stride;

					for (int x = 0; x < width; x = x + xJump)
					{
						//pixels are stored in order: blue, green, red
						arrayB[*pCurrent]++;
						arrayG[pCurrent[1]]++;
						arrayR[pCurrent[2]]++;

						pCurrent += xJumpBytes;
					}
				}
			}

			//smoothing
			if (((width / xJump) * (height / yJump)) > 1000)
			{
				arrayR = GetSmoothArray(arrayR);
				arrayG = GetSmoothArray(arrayG);
				arrayB = GetSmoothArray(arrayB);
			}
		}
		#endregion

		#region GetHistogram24bpp()
		private void GetHistogram24bpp(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			ClearArrays(); 

			unsafe
			{
				Bitmap bitmap = null;
				BitmapData bitmapData = null;

				int stripHeightMax = ImageProcessing.Misc.GetStripHeightMax(itDecoder);

				for (int stripY = clip.Y; stripY < clip.Bottom; stripY = stripY + stripHeightMax)
				{
					int bottom = Math.Min(stripY + stripHeightMax, clip.Bottom);

					try
					{

						bitmap = itDecoder.GetClip(Rectangle.FromLTRB(clip.X, stripY, clip.Right, bottom));
						bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

						int stride = bitmapData.Stride;
						int width = bitmapData.Width;
						int height = bitmapData.Height;

						int xJump = (width / 1500) + 1;
						int yJump = (height / 1500) + 1;
						int xJumpBytes = xJump * 3;

						byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
						byte* pCurrent;

						for (int y = 0; y < height; y = y + yJump)
						{
							pCurrent = pOrig + y * stride;

							for (int x = 0; x < width; x = x + xJump)
							{
								//pixels are stored in order: blue, green, red
								arrayB[*pCurrent]++;
								arrayG[pCurrent[1]]++;
								arrayR[pCurrent[2]]++;

								pCurrent += xJumpBytes;
							}
						}

						//smoothing
						if (((width / xJump) * (height / yJump)) > 1000)
						{
							arrayR = GetSmoothArray(arrayR);
							arrayG = GetSmoothArray(arrayG);
							arrayB = GetSmoothArray(arrayB);
						}
					}
					finally
					{
						if (bitmapData != null)
						{
							bitmap.UnlockBits(bitmapData);
							bitmapData = null;
						}

						itDecoder.ReleaseAllocatedMemory(bitmap);
						bitmap = null;
					}

					if (ProgressChanged != null)
						ProgressChanged((bottom) / (float)clip.Height);
				}
			}
		}

		private void GetHistogram24bpp(BitmapData bitmapData)
		{
			ClearArrays(); 

			int			stride = bitmapData.Stride;
			int			width = bitmapData.Width;
			int			height = bitmapData.Height;

			int			xJump = (width / 1500) + 1;
			int			yJump = (height / 1500) + 1;
			int			xJumpBytes = xJump * 3;

			unsafe
			{
				byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
				byte* pCurrent;

					for (int y = 0; y < height; y = y + yJump)
					{
						pCurrent = pOrig + y * stride;

						for (int x = 0; x < width; x = x + xJump)
						{
							//pixels are stored in order: blue, green, red
							arrayB[*pCurrent]++;
							arrayG[pCurrent[1]]++;
							arrayR[pCurrent[2]]++;

							pCurrent += xJumpBytes;
						}
					}
			}

			//smoothing
			if (((width / xJump) * (height / yJump)) > 1000)
			{
				arrayR = GetSmoothArray(arrayR);
				arrayG = GetSmoothArray(arrayG);
				arrayB = GetSmoothArray(arrayB);
			}
		}
		#endregion
	
		#region GetHistogram8bpp()
		private void GetHistogram8bpp(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			ClearArrays(); 

			unsafe
			{
				Bitmap bitmap = null;
				BitmapData bitmapData = null;

				int stripHeightMax = ImageProcessing.Misc.GetStripHeightMax(itDecoder);

				for (int stripY = clip.Y; stripY < clip.Bottom; stripY = stripY + stripHeightMax)
				{
					int bottom = Math.Min(stripY + stripHeightMax, clip.Bottom);

					try
					{
						bitmap = itDecoder.GetClip(Rectangle.FromLTRB(clip.X, stripY, clip.Right, bottom));
						bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

						int stride = bitmapData.Stride;
						int width = bitmapData.Width;
						int height = bitmapData.Height;

						int xJump = (width / 1500) + 1;
						int yJump = (height / 1500) + 1;
						int xJumpBytes = xJump;

						byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
						byte* pCurrent;

						if (ImageProcessing.Misc.IsGrayscale(bitmap))
						{
							for (int y = 0; y < height; y = y + yJump)
							{
								pCurrent = pOrig + y * stride;

								for (int x = 0; x < width; x = x + xJump)
								{
									arrayR[*pCurrent]++;
									arrayG[*pCurrent]++;
									arrayB[*pCurrent]++;
									pCurrent += xJumpBytes;
								}
							}
						}
						else
						{
							Color[] entries = bitmap.Palette.Entries;

							for (int y = 0; y < height; y = y + yJump)
							{
								pCurrent = pOrig + y * stride;

								for (int x = 0; x < width; x = x + xJump)
								{
									Color c = entries[*pCurrent];

									arrayR[c.R]++;
									arrayG[c.G]++;
									arrayB[c.B]++;
									pCurrent += xJumpBytes;
								}
							}
						}

						//smoothing
						if (((width / xJump) * (height / yJump)) > 1000)
						{
							arrayR = GetSmoothArray(arrayR);
							arrayG = GetSmoothArray(arrayG);
							arrayB = GetSmoothArray(arrayB);
						}
					}
					finally
					{
						if (bitmapData != null)
						{
							bitmap.UnlockBits(bitmapData);
							bitmapData = null;
						}

						itDecoder.ReleaseAllocatedMemory(bitmap);
						bitmap = null;
					}

					if (ProgressChanged != null)
						ProgressChanged((bottom) / (float)clip.Height);
				}
			}
		}
		#endregion

		#region GetHistogram8bppIndexed()
		private void GetHistogram8bppIndexed(BitmapData bitmapData, Color[] entries)
		{
			ClearArrays(); 

			int stride = bitmapData.Stride;
			int width = bitmapData.Width;
			int height = bitmapData.Height;

			int xJump = (width / 1500) + 1;
			int yJump = (height / 1500) + 1;
			int xJumpBytes = xJump;

			unsafe
			{
				byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
				byte* pCurrent;

				for (int y = 0; y < height; y = y + yJump)
				{
					pCurrent = pOrig + y * stride;

					for (int x = 0; x < width; x = x + xJump)
					{
						//pixels are stored in order: blue, green, red
						Color c = entries[*pCurrent];

						arrayB[c.B]++;
						arrayG[c.G]++;
						arrayR[c.R]++;

						pCurrent += xJumpBytes;
					}
				}
			}

			//smoothing
			if (((width / xJump) * (height / yJump)) > 1000)
			{
				arrayR = GetSmoothArray(arrayR);
				arrayG = GetSmoothArray(arrayG);
				arrayB = GetSmoothArray(arrayB);
			}
		}
		#endregion

		#region GetHistogram8bppGray()
		private void GetHistogram8bppGray(BitmapData bitmapData)
		{
			ClearArrays(); 

			int stride = bitmapData.Stride;
			int width = bitmapData.Width;
			int height = bitmapData.Height;

			int xJump = (width / 1500) + 1;
			int yJump = (height / 1500) + 1;
			int xJumpBytes = xJump;

			unsafe
			{
				byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
				byte* pCurrent;

				for (int y = 0; y < height; y = y + yJump)
				{
					pCurrent = pOrig + y * stride;

					for (int x = 0; x < width; x = x + xJump)
					{
						arrayR[*pCurrent]++;
						pCurrent += xJumpBytes;
					}
				}
			}

			//smoothing
			if (((width / xJump) * (height / yJump)) > 1000)
				arrayR = GetSmoothArray(arrayR);

			for (int i = 0; i <= 255; i++)
			{
				arrayG[i] = arrayR[i];
				arrayB[i] = arrayR[i];
			}
		}
		#endregion
	
		#region GetHistogram1bpp()
		private void GetHistogram1bpp(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			byte color;

			ClearArrays(); 

			unsafe
			{
				Bitmap bitmap = null;
				BitmapData bitmapData = null;

				int stripHeightMax = ImageProcessing.Misc.GetStripHeightMax(itDecoder);

				for (int stripY = clip.Y; stripY < clip.Bottom; stripY = stripY + stripHeightMax)
				{
					int bottom = Math.Min(stripY + stripHeightMax, clip.Bottom);
					
					try
					{

						bitmap = itDecoder.GetClip(Rectangle.FromLTRB(clip.X, stripY, clip.Right, bottom));
						bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

						int width = bitmapData.Width / 8;
						int height = bitmapData.Height;
						int stride = bitmapData.Stride;

						byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
						int i;

						for (int y = 0; y < height; y++)
							for (int x = 0; x < width; x++)
							{
								color = pOrig[y * stride + x];

								for (i = 0; i < 8; i++)
								{
									if (((color >> i) & 0x1) == 1)
										arrayR[255]++;
									else
										arrayR[0]++;
								}
							}
					}
					finally
					{
						if (bitmapData != null)
						{
							bitmap.UnlockBits(bitmapData);
							bitmapData = null;
						}

						itDecoder.ReleaseAllocatedMemory(bitmap);
						bitmap = null;
					}

					if (ProgressChanged != null)
						ProgressChanged((bottom) / (float)clip.Height);
				}
			}

			for (int i = 0; i <= 255; i++)
			{
				arrayG[i] = arrayR[i];
				arrayB[i] = arrayR[i];
			}
		}

		private void GetHistogram1bpp(BitmapData bitmapData)
		{
			byte color;
			int stride = bitmapData.Stride;

			ClearArrays(); 

			int width = bitmapData.Width / 8;
			int height = bitmapData.Height;

			unsafe
			{
				byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
				int i;

				for (int y = 0; y < height; y++)
					for (int x = 0; x < width; x++)
					{
						color = pOrig[y * stride + x];

						for (i = 0; i < 8; i++)
						{
							if (((color >> i) & 0x1) == 1)
								arrayR[255]++;
							else
								arrayR[0]++;
						}
					}
			}

			for (int i = 0; i <= 255; i++)
			{
				arrayG[i] = arrayR[i];
				arrayB[i] = arrayR[i];
			}
		}
		#endregion

		#region Form_Paint()
		private void Form_Paint(object sender, PaintEventArgs e)
		{
			Rectangle	rect = new Rectangle(form.ClientRectangle.Location, new Size( form.ClientRectangle.Width - 1, form.ClientRectangle.Height - 1));
			double zoomR = (double)(form.ClientSize.Height - 2) / (arrayR[extremeR] / graphZoom);
			double zoomG = (double)(form.ClientSize.Height - 2) / (arrayG[extremeG] / graphZoom);
			double zoomB = (double)(form.ClientSize.Height - 2) / (arrayB[extremeB] / graphZoom);
			Pen			pen = new Pen(Color.Black) ;
			Pen			penLines = new Pen(SystemColors.ControlLight) ;

			e.Graphics.DrawRectangle(penLines, 0, 0, 255, rect.Height) ;
			e.Graphics.DrawRectangle(penLines, 260, 0, 255, rect.Height) ;
			e.Graphics.DrawRectangle(penLines, 520, 0, 255, rect.Height) ;

			for(int i = 0; i < 32; i++)
			{
				e.Graphics.DrawLine(penLines, i * 8, 0, i * 8, rect.Height) ;
				e.Graphics.DrawLine(penLines, 260 + i * 8, 0, 260 + i * 8, rect.Height) ;
				e.Graphics.DrawLine(penLines, 520 + i * 8, 0, 520 + i * 8, rect.Height) ;
			}

			for(int i = 0; i < 256; i++)
			{
				e.Graphics.DrawLine(pen, i, rect.Height - (int)((double)arrayR[i] * zoomR), i, rect.Height) ;
				e.Graphics.DrawLine(pen, i + 260, rect.Height - (int)((double)arrayG[i] * zoomG), i + 260, rect.Height) ;
				e.Graphics.DrawLine(pen, i + 520, rect.Height - (int)((double)arrayB[i] * zoomB), i + 520, rect.Height) ;
			}
		}
		#endregion

		#region Form_MouseMove()
		private void Form_MouseMove(object sender, MouseEventArgs e)
		{
			try
			{
				if (((e.X % 260) < 256) && (e.X / 260 < 3))
				{
					int index = Math.Min(255, e.X % 260);
					this.textBoxPosition.Text = string.Format("{0}", index);
					this.textBoxValue.Text = string.Format("{0}", (e.X < 260) ? arrayR[index] : ((e.X < 520) ? arrayG[index] : arrayB[index]));
				}
				else
				{
					this.textBoxPosition.Text = "";
					this.textBoxValue.Text = "";
				}
			}
			catch
			{
			}
		}
		#endregion

		#region Minus_Click()
		void Minus_Click(object sender, EventArgs e)
		{
			graphZoom /= 2;
			form.Refresh();
		}
		#endregion

		#region Plus_Click()
		void Plus_Click(object sender, EventArgs e)
		{
			graphZoom *= 2;
			form.Refresh();
		}
		#endregion

		#region GetSmoothArray()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="arrayS">Source array.</param>
		/// <returns></returns>
		internal static uint[] GetSmoothArray(uint[] arrayS)
		{
			uint[] arrayR = new uint[256];

			arrayR[0] = (3 * arrayS[0] + arrayS[1] + arrayS[2]) / 5;
			arrayR[1] = (2 * arrayS[0] + arrayS[1] + arrayS[2] + arrayS[3]) / 5;
			arrayR[255] = (arrayS[253] + arrayS[254] + 3 * arrayS[255]) / 5;
			arrayR[254] = (arrayS[252] + arrayS[253] + arrayS[254] + 2 * arrayS[255]) / 5;

			for (byte i = 2; i < 254; i++)
				arrayR[i] = (arrayS[i - 2] + arrayS[i - 1] + arrayS[i] + arrayS[i + 1] + arrayS[i + 2]) / 5;

			return arrayR;
		}
		#endregion

		#region GetOtsuThreshold()
		internal static byte GetOtsuThreshold(uint[] array)
		{
			long sum = 0;
			double[] probabilities = new double[256];
			byte maxIndex = 1;
			double maxValue = 0;

			for (int i = 0; i < 256; i++)
				sum += array[i];

			for (int i = 0; i < 256; i++)
				probabilities[i] = array[i] / (double)sum;

			for (int i = 1; i < 256 - 1; i++)
			{
				double probabilitiesL = 0;
				double probabilitiesR = 0;
				double meanL = GetMean(array, 0, i);
				double meanR = GetMean(array, i + 1, 255);

				for (int j = 0; j <= i; j++)
					probabilitiesL += probabilities[j];
				for (int j = i + 1; j < 256; j++)
					probabilitiesR += probabilities[j];

				double localValue = probabilitiesL * probabilitiesR * (meanL - meanR) * (meanL - meanR);

				if (maxValue < localValue)
				{
					maxValue = localValue;
					maxIndex = (byte)i;
				}
			}

			return maxIndex;
		}
		#endregion

		#region GetOtsuBackground()
		/// <summary>
		/// First, it computes OTSU threshold and then it returns average of background (bigger than threshold) values.
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		internal static byte GetOtsuBackground(uint[] array)
		{
			byte threshold = GetOtsuThreshold(array);
			uint sum = 0;
			uint count = 0;

			for (uint i = threshold; i < 256; i++)
			{
				sum += array[i] * i;
				count += array[i];
			}

			return (byte)(sum / count);
		}
		#endregion

		#region GetMean()
		private static double GetMean(uint[] array)
		{
			long sum = 0;
			long count = 0;

			for (int i = 0; i <= 255; i++)
			{
				sum += array[i] * i;
				count += array[i];
			}

			if (count > 0)
				return (double)sum / count;
			else
				return 0;
		}

		private static double GetMean(uint[] array, int startIndex, int endIndex)
		{
			long sum = 0;
			long count = 0;

			for (int i = startIndex; i <= endIndex; i++)
			{
				sum += array[i] * i;
				count += array[i];
			}

			if (count > 0)
				return (double)sum / count;
			else
				return 0;
		}
		#endregion

		#region GetMedian()
		/// <summary>
		/// Median - index of array item separating lower and higher half.
		/// </summary>
		/// <param name="h"></param>
		/// <param name="rect"></param>
		/// <returns></returns>
		private byte GetMedian(uint[] array)
		{
			uint sum = 0;
			uint current = 0;

			for (int i = 0; i < array.Length; i++)
				sum += array[i];

			for (int i = 0; i < array.Length; i++)
			{
				if (current + array[i] >= sum / 2.0)
					return (byte) i;
				else
					current += array[i];
			}

			return 255;
		}
		#endregion

		#region GetStandardDeviation()
		/// <summary>
		/// Median - index of array item separating lower and higher half.
		/// </summary>
		/// <param name="h"></param>
		/// <param name="rect"></param>
		/// <returns></returns>
		private double GetStandardDeviation(uint[] array)
		{
			double mean = GetMean(array);
			double sum = 0;
			uint count = 0;

			for (int i = 0; i < array.Length; i++)
			{
				sum += array[i] * ((i - mean) * (i - mean));
				count += array[i];
			}

			if (count > 0)
				return Math.Sqrt(sum /= count);
			else
				return 127;
		}
		#endregion

		#region ClearArrays()
		private void ClearArrays()
		{
			for (int i = 0; i < 256; i++)
			{
				arrayR[i] = 0;
				arrayG[i] = 0;
				arrayB[i] = 0;
			}
		}
		#endregion

		#endregion
	}
}
