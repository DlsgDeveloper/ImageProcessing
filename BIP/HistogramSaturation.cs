using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for HistogramSaturation.
	/// </summary>
	public class HistogramSaturation
	{
		uint[]		array = new uint[256] ;
		byte		extreme = 0 ;
		byte		minimum;
		byte		maximum;

	
		Form		form = null ;
		TextBox		textBoxPosition = null ;
		TextBox		textBoxValue = null ;

		float		graphZoom = 2;

		// indicates how many percent of the pixels will be clipped off while deciding minimum and maximum
		float		minMaxClip = 0.01F;

		
		#region constructor
		public HistogramSaturation(Bitmap image)
			: this(image, Rectangle.FromLTRB(0, 0, image.Width, image.Height))
		{
		}

		public HistogramSaturation(Bitmap image, Rectangle clip)
		{
			BitmapData		bitmapData = null ;

			try
			{
				bitmapData = image.LockBits(clip, ImageLockMode.ReadOnly, image.PixelFormat); 
				Load(bitmapData) ;
			}
			finally
			{
				if(bitmapData != null)
					image.UnlockBits(bitmapData) ;
			}
		}


		public HistogramSaturation(BitmapData bitmapData)
		{
			Load(bitmapData);
		}
		#endregion

		//	PUBLIC PROPERTIES
		#region public properties

		public byte		Extreme			{ get { return this.extreme ; } }
		public uint[]	Array			{ get { return this.array; } }
		public byte		Minimum			{ get { return this.minimum; } }
		public byte		Maximum			{ get { return this.maximum; } }

		#endregion

		
		//	PUBLIC METHODS
		#region public methods

		#region Show()
		public void Show()
		{
			if(this.form == null)
			{
				this.form = new Form() ;
				this.form.Name = "HistogramSaturation" ;
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

				Button		buttonPlus = new Button();
				Button		buttonMinus = new Button();
			
				this.form.Controls.Add(this.textBoxPosition) ;
				this.form.Controls.Add(this.textBoxValue) ;
				this.form.Controls.AddRange(new Control[] {labelThresholdR, labelExtremeR, label2ndExtremeR, labelMaxDeltaR}) ;
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
		private void Load(BitmapData bitmapData)
		{
			switch (bitmapData.PixelFormat)
			{
				case PixelFormat.Format32bppRgb:
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format24bppRgb:
					GetHistogram24or32bpp(bitmapData);
					break;
				case PixelFormat.Format8bppIndexed:
				case PixelFormat.Format1bppIndexed:
					GetHistogram8or1bpp(bitmapData);
					break;
				default:
					throw new IpException(ErrorCode.ErrorUnsupportedFormat);
			}

			short i;
			uint[] arrayTmp = new uint[256];


			//local extreme
			for (i = 1; i < 256; i++)
			{
				if (array[extreme] < array[i])
					extreme = (byte)i;
			}

			// second local extreme
			for (i = 0; i < 256; i++)
			{
				if ((i < extreme - 60) || (i > extreme + 60))
					arrayTmp[i] = array[i];
				else
					arrayTmp[i] = 0;
			}

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
			/*long totalPixels = 0;
			for (i = 0; i < 256; i++)
				totalPixels += array[i];

			double limitThreshold = totalPixels / bitmapData.Width;

			//minimum
			this.minimum = 0;
			for (i = 0; i < 256; i++)
				if (array[i] > limitThreshold)
				{
					this.minimum = (byte)i;
					break;
				}

			//maximum
			this.maximum = 255;
			for (i = 255; i >= 0; i--)
				if (array[i] > limitThreshold)
				{
					this.maximum = (byte)i;
					break;
				}*/
		}
		#endregion

		#region GetHistogram24or32bpp()
		private void GetHistogram24or32bpp(BitmapData bitmapData)
		{
			uint[]		arrayTmp = new uint[256] ;

			int			stride = bitmapData.Stride;
			int			width = bitmapData.Width;
			int			height = bitmapData.Height;

			int			xJump = (width / 1500) + 1;
			int			yJump = (height / 1500) + 1;
			int			xJumpBytes = (bitmapData.PixelFormat == PixelFormat.Format24bppRgb) ? xJump * 3 : xJump * 4;

			unsafe
			{
				byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
				byte* pCurrent;

				for (int y = 0; y < height; y = y + yJump)
				{
					pCurrent = pOrig + y * stride;

					for (int x = 0; x < width; x = x + xJump)
					{
						//if (pCurrent[2] > 120 || pCurrent[1] > 120 || pCurrent[0] > 120)
						{
							arrayTmp[(byte)(Hsl.GetSaturation(pCurrent[2], pCurrent[1], pCurrent[0]) * 255)]++;
						}
							
						pCurrent += xJumpBytes;
					}
				}
			}

			//smoothing
			array = SmoothAndCopyArray(arrayTmp);
		}
		#endregion

		#region GetHistogram8bpp()
		private void GetHistogram8or1bpp(BitmapData bitmapData)
		{
			uint[] arrayTmp = new uint[256];

			arrayTmp[0] = (uint) (bitmapData.Width * bitmapData.Height);
		}
		#endregion

		#region SmoothAndCopyArray()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="arrayS">Source array.</param>
		/// <returns></returns>
		private static uint[] SmoothAndCopyArray(uint[] arrayS)
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
					this.textBoxValue.Text = string.Format("{0}", array[index]);
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

		#endregion
	}
}
