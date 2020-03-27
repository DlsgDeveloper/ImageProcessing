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

	public class HistogramGrayscale
	{
		uint[]		array = new uint[256] ;
		byte		extreme = 0 ;
		byte		secondExtreme = 0 ;
		byte		threshold ;
		byte		maxDelta = 0 ;
		byte		mean = 127;
	
		Form		form = null ;
		TextBox		textBoxPosition = null ;
		TextBox		textBoxValue = null ;

		float minMaxClip = 0.05F;

		byte minimum;
		byte maximum;
		float graphZoom = 2;


		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		

		#region constructor
		/// <summary>
		/// Call to get progress monitoring on the histogram computation. Call 'Compute' to get values.
		/// </summary>
		public HistogramGrayscale()
		{
		}

		public HistogramGrayscale(string filePath)
			:this(filePath, Rectangle.Empty)
		{
		}

		public HistogramGrayscale(string filePath, Rectangle clip)
		{
			using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(filePath))
			{
				Compute(itDecoder, clip);
			}
		}

		public HistogramGrayscale(Bitmap image)
			: this(image, Rectangle.Empty)
		{
		}

		public HistogramGrayscale(Bitmap image, Rectangle clip)
		{
			Load(image, clip);
		}
		#endregion

		//	PUBLIC PROPERTIES
		#region public properties

		public byte		Extreme			{ get { return this.extreme ; } }
		public byte		SecondExtreme	{ get { return this.secondExtreme; } }
		public byte		MaxDelta		{ get { return this.maxDelta; } }
		public uint[]	Array			{ get { return this.array; } }

		//public byte Threshold { get { return GetBackgroundThreshold(array); } }
		public byte		Threshold		{ get { return this.threshold; } }
		public byte		Darkest5Percent { get { return this.minimum; } }
		public byte		Lightest5Percent { get { return this.maximum; } }

		#region Background
		public byte Background
		{
			get
			{
				byte g = ImageProcessing.Histogram.GetOtsuBackground(array);

				return g;
			}
		}
		#endregion

		#region Mean
		/// <summary>
		/// value <0, 255>
		/// </summary>
		public double Mean
		{
			get
			{
				long sum = 0;
				long count = 0;
				
				for(int i = 0; i < 256; i++)
				{
					sum += array[i] * i;
					count += array[i];
				}

				return Convert.ToByte(sum / count);
			}
		}
		#endregion

		#region Darkest
		public byte Darkest
		{
			get
			{
				for (int i = 0; i < 256; i++)
					if (array[i] > 0)
						return (byte) i;

				return 0;
			}
		}
		#endregion

		#region Lightest
		public byte Lightest
		{
			get
			{
				for (int i = 255; i >= 0; i--)
					if (array[i] > 0)
						return (byte)i;

				return 255;
			}
		}
		#endregion

		#endregion

		
		//	PUBLIC METHODS
		#region public methods

		#region Compute()
		public void Compute(ImageProcessing.BigImages.ItDecoder itDecoder)
		{
			Compute(itDecoder, Rectangle.FromLTRB(0, 0, itDecoder.Width, itDecoder.Height));
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

				Label		labelThreshold = new Label() ;
				Label		labelExtreme = new Label() ;
				Label		label2ndExtreme = new Label() ;
				Label		labelMaxDelta = new Label() ;

				Button		buttonPlus = new Button();
				Button		buttonMinus = new Button();
			
				this.form.Controls.Add(this.textBoxPosition) ;
				this.form.Controls.Add(this.textBoxValue) ;
				this.form.Controls.AddRange(new Control[] {labelThreshold, labelExtreme, label2ndExtreme, labelMaxDelta }) ;
				this.form.Controls.Add(buttonPlus);
				this.form.Controls.Add(buttonMinus);

				this.textBoxPosition.Bounds = new Rectangle(790, 30, 110, 20) ;
				this.textBoxValue.Bounds = new Rectangle(790, 50, 110, 20) ;
				
				labelThreshold.Bounds = new Rectangle(790, 170, 110, 20) ;
				labelExtreme.Bounds = new Rectangle(790, 190, 110, 20) ;
				label2ndExtreme.Bounds = new Rectangle(790, 210, 110, 20) ;
				labelMaxDelta.Bounds = new Rectangle(790, 230, 110, 20) ;

				labelThreshold.Text = string.Format(BIPStrings.Threshold_STR+" R: {0}",  this.Threshold) ;
				labelExtreme.Text = string.Format(BIPStrings.Extreme_STR+" R: {0}",  this.Extreme) ;
				label2ndExtreme.Text = string.Format(BIPStrings.NdExtreme_STR+" R: {0}",  this.SecondExtreme) ;
				labelMaxDelta.Text = string.Format(BIPStrings.MinRange_STR+" R: {0}",  this.MaxDelta) ;

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

		#endregion

		//	PRIVATE METHODS
		#region private methods

		#region Load()
		private void Load(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			switch (itDecoder.PixelFormat)
			{
				case PixelFormat.Format32bppRgb:
				case PixelFormat.Format32bppArgb:
					GetHistogram32bpp(itDecoder, clip);
					break;
				case PixelFormat.Format24bppRgb:
					GetHistogram24bpp(itDecoder, clip);
					break;
				case PixelFormat.Format8bppIndexed:
					GetHistogram8bpp(itDecoder, clip);
					break;
				case PixelFormat.Format1bppIndexed:
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
					case PixelFormat.Format24bppRgb:
					case PixelFormat.Format8bppIndexed:
					case PixelFormat.Format1bppIndexed:
						GetHistogram(bitmapData);
						break;
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat);
				}
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
			}

			DoTheComputations();
		}
		#endregion

		#region DoTheComputations()
		private void DoTheComputations()
		{
			short i;
			uint[] arrayTmp = new uint[256];

			//local extreme
			for (i = 1; i < 256; i++)
				if (array[extreme] < array[i])
					extreme = (byte)i;

			// second local extreme
			for (i = 0; i < 256; i++)
			{
				if ((i < extreme - 60) || (i > extreme + 60))
					arrayTmp[i] = array[i];
				else
					arrayTmp[i] = 0;
			}

			for (i = 0; i < 256; i++)
				if (arrayTmp[secondExtreme] < arrayTmp[i])
					secondExtreme = (byte)i;

			//threshold			
			this.threshold = ImageProcessing.Histogram.GetOtsuThreshold(this.array);

			//max delta
			byte min = (byte)(((extreme < secondExtreme) ? extreme : secondExtreme) + 2);
			byte max = (byte)(((extreme >= secondExtreme) ? extreme : secondExtreme) - 2);
			uint maxDeltaValue = 0;

			maxDelta = min;

			for (i = (short)(min + 1); i < max; i++)
			{
				if (maxDeltaValue < ((int)(array[i + 1] - array[i - 1])))
				{
					maxDeltaValue = (uint)array[i + 1] - array[i - 1];
					maxDelta = (byte)i;
				}
			}

			//means
			long sum = 0;
			long count = 0;

			for (i = 0; i < 256; i++)
			{
				sum += array[i] * i;
				count += array[i];
			}

			this.mean = Convert.ToByte(sum / count);

			//minimum and maximum
			long totalPixels = 0;
			for (i = 0; i < 256; i++)
				totalPixels += array[i];

			double jumpOver = totalPixels * minMaxClip;
			uint pixelsCount;

			//minimum
			this.minimum = 0;
			pixelsCount = 0;
			for (i = 0; i < 256; i++)
			{
				if (pixelsCount + array[i] > jumpOver)
				{
					this.minimum = (byte)i;
					break;
				}
				else
					pixelsCount += array[i];
			}

			//maximum
			this.maximum = 255;
			pixelsCount = 0;
			for (i = 255; i >= 0; i--)
			{
				if (pixelsCount + array[i] > jumpOver)
				{
					this.maximum = (byte)i;
					break;
				}
				else
					pixelsCount += array[i];
			}
		}
		#endregion

		#region GetHistogram32bpp()
		private void GetHistogram32bpp(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			uint[]		arrayTmp = new uint[256] ;

			for(int i = 0; i < 256 ; i++)
			{
				arrayTmp[i] = 0 ;
			}

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
						double gray;

						for (int y = 0; y < height; y = y + yJump)
						{
							pCurrent = pOrig + y * stride;

							for (int x = 0; x < width; x = x + xJump)
							{
								//pixels are stored in order: blue, green, red
								gray = 0.299 * pCurrent[2] + 0.587 * pCurrent[1] + 0.114 * *pCurrent;

								arrayTmp[(byte)gray]++;

								pCurrent += xJumpBytes;
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

			//smoothing
			array = ImageProcessing.Histogram.GetSmoothArray(arrayTmp);
		}
		#endregion

		#region GetHistogram24bpp()
		private void GetHistogram24bpp(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			uint[]		arrayTmp = new uint[256] ;

			for(int i = 0; i < 256 ; i++)
			{
				arrayTmp[i] = 0 ;
			}

			unsafe
			{
				Bitmap bitmap = null;
				BitmapData bitmapData = null;

				int stripHeightMax = Misc.GetStripHeightMax(itDecoder);

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
						double	gray;

						for (int y = 0; y < height; y = y + yJump)
						{
							pCurrent = pOrig + y * stride;

							for (int x = 0; x < width; x = x + xJump)
							{
								//pixels are stored in order: blue, green, red
								gray = 0.299 * pCurrent[2] + 0.587 * pCurrent[1] + 0.114 * *pCurrent;

								arrayTmp[(byte)gray]++;

								pCurrent += xJumpBytes;
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

			//smoothing
			array = ImageProcessing.Histogram.GetSmoothArray(arrayTmp);
		}
		#endregion
	
		#region GetHistogram8bpp()
		private void GetHistogram8bpp(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			uint[] arrayTmp = new uint[256];

			unsafe
			{
				Bitmap bitmap = null;
				BitmapData bitmapData = null;

				int stripHeightMax = Misc.GetStripHeightMax(itDecoder);

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
									arrayTmp[*pCurrent]++;
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
									
									arrayTmp[c.R]++;
									pCurrent += xJumpBytes;
								}
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

			//smoothing
			array = ImageProcessing.Histogram.GetSmoothArray(arrayTmp);
		}
		#endregion

		#region GetHistogram1bpp()
		private void GetHistogram1bpp(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			byte color;

			for (int i = 0; i < array.Length; i++)
				array[i] = 0;

			unsafe
			{
				Bitmap bitmap = null;
				BitmapData bitmapData = null;

				int stripHeightMax = Misc.GetStripHeightMax(itDecoder);

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
										array[255]++;
									else
										array[0]++;
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
		}
		#endregion

		#region Form_Paint()
		private void Form_Paint(object sender, PaintEventArgs e)
		{
			Rectangle	rect = new Rectangle(form.ClientRectangle.Location, new Size( form.ClientRectangle.Width - 1, form.ClientRectangle.Height - 1));
			double		zoom = (double)(form.ClientSize.Height - 2) / (array[extreme] / graphZoom);
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
				e.Graphics.DrawLine(pen, i, rect.Height - (int)((double)array[i] * zoom), i, rect.Height) ;
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
					this.textBoxValue.Text = string.Format("{0}", (e.X < 260) ? array[index] : 0);
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

		#region GetHistogram()
		private void GetHistogram(BitmapData bitmapData)
		{
			uint[] arrayTmp = new uint[256];

			int stride = bitmapData.Stride;
			int width = bitmapData.Width;
			int height = bitmapData.Height;

			int xJump = (width / 1500) + 1;
			int yJump = (height / 1500) + 1;

			unsafe
			{
				byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
				byte* pCurrent;

				if (bitmapData.PixelFormat == PixelFormat.Format24bppRgb)
				{
					int xJumpBytes = xJump * 3;

					for (int y = 0; y < height; y = y + yJump)
					{
						pCurrent = pOrig + y * stride;

						for (int x = 0; x < width; x = x + xJump)
						{
							//pixels are stored in order: blue, green, red
							arrayTmp[(byte)(0.299 * pCurrent[2] + 0.587 * pCurrent[1] + 0.114 * pCurrent[0])]++;

							pCurrent += xJumpBytes;
						}
					}
				}
				else if (bitmapData.PixelFormat == PixelFormat.Format32bppArgb || bitmapData.PixelFormat == PixelFormat.Format32bppRgb)
				{
					int xJumpBytes = xJump * 4;

					for (int y = 0; y < height; y = y + yJump)
					{
						pCurrent = pOrig + y * stride;

						for (int x = 0; x < width; x = x + xJump)
						{
							//pixels are stored in order: blue, green, red
							arrayTmp[(byte)(0.299 * pCurrent[2] + 0.587 * pCurrent[1] + 0.114 * pCurrent[0])]++;

							pCurrent += xJumpBytes;
						}
					}
				}
				else if (bitmapData.PixelFormat == PixelFormat.Format8bppIndexed)
				{
					int xJumpBytes = xJump;

					for (int y = 0; y < height; y = y + yJump)
					{
						pCurrent = pOrig + y * stride;

						for (int x = 0; x < width; x = x + xJump)
						{
							arrayTmp[*pCurrent]++;
							pCurrent += xJumpBytes;
						}
					}
				}
				else if (bitmapData.PixelFormat == PixelFormat.Format1bppIndexed)
				{
					byte color, i;
					
					width = bitmapData.Width / 8;

					for (int y = 0; y < height; y++)
						for (int x = 0; x < width; x++)
						{
							color = pOrig[y * stride + x];

							for (i = 0; i < 8; i++)
							{
								if (((color >> i) & 0x1) == 1)
									array[255]++;
								else
									array[0]++;
							}
						}
				}
				else
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}

			//smoothing
			array = ImageProcessing.Histogram.GetSmoothArray(arrayTmp);
		}
		#endregion

		#endregion
	}
}
