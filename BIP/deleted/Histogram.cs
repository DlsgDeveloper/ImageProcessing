using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Windows.Forms ;
using System.Collections;
using System.Collections.Generic;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Histogram.
	/// </summary>
	/*public class Histogram
	{
		uint[]		array = new uint[256] ;
		byte		extreme = 0 ;
		byte		secondExtreme = 0 ;
		byte		minRange = 0;
		byte		fifthMinRange = 0;
		byte		fifthMaxRange = 255 ;
		byte		maxRange = 0 ;
		byte		threshold = 127 ;
		byte		darkerThreshold = 64 ;
		byte		lighterThreshold = 191 ;
		
		Form		form = null ;
		TextBox		textBoxPosition = null ;
		TextBox		textBoxValue = null ;

		
		#region constructor
		public Histogram(Bitmap image)
			: this(image, Rectangle.FromLTRB(0, 0, image.Width, image.Height))
		{
		}

		public Histogram(Bitmap image, Rectangle clip)
		{
			BitmapData		bitmapData = null ;

			try
			{
				bitmapData = image.LockBits(clip, ImageLockMode.ReadOnly, image.PixelFormat);
				clip.Offset(-clip.X, -clip.Y);
				Load(bitmapData, image.Palette.Entries, clip) ;
			}
			finally
			{
				if(bitmapData != null)
					image.UnlockBits(bitmapData) ;
			}
		}

		public Histogram(BitmapData	bitmapData, Color[] palette)
			: this(bitmapData, palette, Rectangle.FromLTRB(0, 0, bitmapData.Width, bitmapData.Height))
		{
		}

		public Histogram(BitmapData	bitmapData, Color[] palette, Rectangle clip)
		{
			clip.X = Math.Max(0, clip.X) ;
			clip.Y = Math.Max(0, clip.Y) ;
			clip.Width = Math.Min(bitmapData.Width - clip.X, clip.Width) ;
			clip.Height = Math.Min(bitmapData.Height - clip.Y, clip.Height) ;
			
			Load(bitmapData, palette, clip) ;
		}
		#endregion

		//	PUBLIC PROPERTIES
		#region public properties
		public uint[]	Array			{ get { return this.array; } }
		public byte		Extreme			{ get { return this.extreme ; } }
		public byte		SecondExtreme	{ get { return this.secondExtreme ; } }
		public ushort	Size			{ get { return (ushort) array.Length ; } }
		public byte		MinRange		{ get { return this.minRange ; } }
		public byte		MaxRange		{ get { return this.maxRange ; } }
		public byte		FifthMinRange	{ get { return this.fifthMinRange ; } }
		public byte		FifthMaxRange	{ get { return this.fifthMaxRange ; } }
		public byte		Threshold		{ get { return this.threshold ; } }
		public byte		DarkerThreshold	{ get { return this.darkerThreshold ; } }
		public byte		LighterThreshold{ get { return this.lighterThreshold ; } }
		public uint		this[int index]	{ get { return this.array[index] ; } }

		#region MaxDelta
		public byte MaxDelta
		{
			get
			{
				byte	min = (byte) (((extreme < secondExtreme) ? extreme : secondExtreme) + 2) ;
				byte	max = (byte) (((extreme >= secondExtreme) ? extreme : secondExtreme) - 2) ;
				
				byte	maxDeltaIndex = min ;
				uint	maxDelta = 0 ;

				for(int i = min + 1; i < max; i++)
				{
					if(maxDelta < ((int)(array[i + 1] - array[i - 1])))
					{
						maxDelta = (uint) array[i + 1] - array[i - 1] ;
						maxDeltaIndex = (byte) i ;
					}
				}

				return maxDeltaIndex ;
			}
		}
		#endregion

		#endregion
		
		//	PUBLIC METHODS
		#region public methods

		#region GetThreshold()
		public static int GetThreshold(Bitmap image, Rectangle clip)
		{
			Histogram	histogram = new Histogram(image, clip) ;
			return histogram.Threshold ;
		}

		public static int GetThreshold(BitmapData	bitmapData, Color[] palette, Rectangle clip)
		{
			Histogram	histogram = new Histogram(bitmapData, palette, clip) ;
			return histogram.Threshold ;
		}
		#endregion

		#region Show()
		public void Show()
		{
			if(this.form == null)
			{
				this.form = new Form() ;
				this.form.Name = "Histogram" ;
				this.form.StartPosition = FormStartPosition.Manual ;
				this.form.Size = new Size(650, 800) ;
				this.form.MaximumSize = new Size(650, 1200) ;
				this.form.MinimumSize = new Size(650, 200) ;
				this.form.Location = new Point(0, 0) ;
				this.form.FormBorderStyle = FormBorderStyle.SizableToolWindow ;
				this.form.BackColor = Color.White ;

				this.form.Paint += new PaintEventHandler(Form_Paint);

				this.textBoxPosition = new TextBox() ;
				this.textBoxValue = new TextBox() ;
				Label		labelThreshold = new Label() ;
				Label		labelExtreme = new Label() ;
				Label		label2ndExtreme = new Label() ;
				Label		labelMinRange = new Label() ;
				Label		labelMaxRange = new Label() ;
				Label		labelFifthMinRange = new Label() ;
				Label		labelFifthMaxRange = new Label() ;

				this.form.Controls.Add(this.textBoxPosition) ;
				this.form.Controls.Add(this.textBoxValue) ;
				this.form.Controls.AddRange(new Control[] {labelThreshold, labelExtreme, label2ndExtreme, labelMinRange, 
															  labelMaxRange, labelFifthMinRange, labelFifthMaxRange}) ;

				this.textBoxPosition.Bounds = new Rectangle(520, 30, 110, 20) ;
				this.textBoxValue.Bounds = new Rectangle(520, 50, 110, 20) ;
				labelThreshold.Bounds = new Rectangle(520, 80, 110, 20) ;
				labelExtreme.Bounds = new Rectangle(520, 100, 110, 20) ;
				label2ndExtreme.Bounds = new Rectangle(520, 120, 110, 20) ;
				labelMinRange.Bounds = new Rectangle(520, 140, 110, 20) ;
				labelMaxRange.Bounds = new Rectangle(520, 160, 110, 20) ;
				labelFifthMinRange.Bounds = new Rectangle(520, 180, 110, 20) ;
				labelFifthMaxRange.Bounds = new Rectangle(520, 200, 110, 20) ;

				labelThreshold.Text = string.Format("Threshold: {0}",  this.Threshold) ;
				labelExtreme.Text = string.Format("Extreme: {0}",  this.Extreme) ;
				label2ndExtreme.Text = string.Format("2nd Extreme: {0}",  this.SecondExtreme) ;
				labelMinRange.Text = string.Format("Min Range: {0}",  this.MinRange) ;
				labelMaxRange.Text = string.Format("Max Range: {0}",  this.MaxRange) ;
				labelFifthMinRange.Text = string.Format("5th Min Range: {0}",  this.FifthMinRange) ;
				labelFifthMaxRange.Text = string.Format("5th Max Range: {0}",  this.FifthMaxRange) ;

				this.form.MouseMove += new MouseEventHandler(Form_MouseMove);
			}

			form.ShowDialog() ;
		}
		#endregion
		
		#endregion

		//	PRIVATE METHODS
		#region private methods

		#region Load()
		private void Load(BitmapData bitmapData, Color[] palette, Rectangle clip)
		{		
			switch(bitmapData.PixelFormat)
			{
				case PixelFormat.Format32bppRgb :
				case PixelFormat.Format32bppArgb :
					GetHistogram32bpp(bitmapData, clip) ;
					break ;
				case PixelFormat.Format24bppRgb :
					GetHistogram24bpp(bitmapData, clip) ;
					break ;
				case PixelFormat.Format8bppIndexed :
				{
					if(palette == null || Misc.IsPaletteGrayscale(palette))
						GetHistogram8bppGrayscale(bitmapData, clip) ;
					else
						GetHistogram8bpp(bitmapData, palette, clip) ;
				}break ;
				case PixelFormat.Format1bppIndexed :
					GetHistogram1bpp(bitmapData, clip) ;
					break ;
				default :
					throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
			}
			
			//second local extreme
			int		i ;

			//min non 0 value index 
			for(i = 0; i < 256; i++)
				if(array[i] > 0)
				{
					minRange = (byte) i ;
					break ;
				}

			//max non 0 value index 
			for(i = 255; i >= 0; i--)
				if(array[i] > 0)
				{
					maxRange = (byte) i ;
					break ;
				}

			//5th min non 0 value index 
			int		counter = 5 ;
			
			for(i = 0; i < 256; i++)
				if(array[i] > 0)
				{
					if(counter == 0)
					{
						fifthMinRange = (byte) i ;
						break ;
					}
					else
						counter -- ;
				}

			//5th max non 0 value index 
			counter = 5 ;
			
			for(i = 255; i >= 0; i--)
				if(array[i] > 0)
				{
					if(counter == 0)
					{
						fifthMaxRange = (byte) i ;
						break ;
					}
					else
						counter -- ;
				}
									
			//local extremes
			for(i = 1; i < 256; i++)
				if(array[extreme] < array[i])
					extreme = (byte) i ;

			uint[]		arrayTmp = new uint[256] ;
			for(i = 0; i < 256; i++)
				if( (i < extreme - 80) || (i > extreme + 80) )
					arrayTmp[i] = array[i];

			for(i = 0; i < 256; i++)
				if(arrayTmp[secondExtreme] < arrayTmp[i])
					secondExtreme = (byte) i ;

			//threshold			
			threshold = (byte) ((extreme + secondExtreme) / 2) ;

			int			to = Math.Min(extreme, secondExtreme) + 10 ;
			for(i = Math.Max(extreme, secondExtreme) - 10; i > to; i--)
			{
				if((array[i] > array[i + 1]) || (array[i] * 50 < array[extreme]))
				{
					threshold = (byte) i ;
					break ;
				}
			}
			
			darkerThreshold = (byte) Math.Min( (2 * extreme + secondExtreme) / 3, (extreme + 2 * secondExtreme) / 3) ;
			lighterThreshold = (byte) Math.Max( (2 * extreme + secondExtreme) / 3, (extreme + 2 * secondExtreme) / 3) ;
		}
		#endregion
		
		#region GetHistogram32bpp()
		private void GetHistogram32bpp(BitmapData bitmapData, Rectangle clip)
		{
			int			gray ;
			int			stride = bitmapData.Stride; 

			short		clipX = (short) clip.X ;
			short		clipY = (short) clip.Y ;
			short		clipRight = (short) clip.Right ;
			short		clipBottom = (short) clip.Bottom ;

			int			xJump = (clip.Width / 1500) + 1;
			int			yJump = (clip.Height / 1500) + 1;
			int			xJumpBytes = xJump * 4;

			for(int i = 0; i < array.Length ; i++)
				array[i] = 0 ;

			unsafe
			{
				byte*	pOrig = (byte*)bitmapData.Scan0.ToPointer() + clipX * 4; 
				byte*	pCurrent ; 

				for(int y = clipY; y < clipBottom; y += yJump) 
				{ 
					pCurrent = pOrig + y * stride;

					for(int x = clipX; x < clipRight; x += xJump) 
					{ 
						//gray = 0.299Red + 0.587Gray + 0.114Blue
						//pixels are stored in order: blue, green, red, alpha
						gray = (int) (pCurrent[2] * 0.299F + pCurrent[1] * 0.587F + pCurrent[0] * 0.114F) ;
						array[gray] ++ ;
						pCurrent += xJumpBytes;
					} 
				}
			}

			//smoothing
			array = SmoothHistogram(array);
			array = SmoothHistogram(array);
		}
		#endregion

		#region GetHistogram24bpp()
		private void GetHistogram24bpp(BitmapData bitmapData, Rectangle clip)
		{
			int			gray ;
			int			stride = bitmapData.Stride; 

			short		clipX = (short) clip.X ;
			short		clipY = (short) clip.Y ;
			short		clipRight = (short) clip.Right ;
			short		clipBottom = (short) clip.Bottom ;

			int			xJump = (clip.Width / 1500) + 1;
			int			yJump = (clip.Height / 1500) + 1;
			int			xJumpBytes = xJump * 3;

			for(int i = 0; i < array.Length ; i++)
				array[i] = 0 ;

			unsafe
			{
				byte*	pOrig = (byte*)bitmapData.Scan0.ToPointer() + clipX * 3; 
				byte*	pCurrent ; 

				for(int y = clipY; y < clipBottom; y += yJump) 
				{ 
					pCurrent = pOrig + y * stride;

					for(int x = clipX; x < clipRight; x += xJump) 
					{ 
						//gray = 0.299Red + 0.587Gray + 0.114Blue
						//pixels are stored in order: blue, green, red
						gray = (int) (*pCurrent * 0.114F + pCurrent[1] * 0.587F + pCurrent[2] * 0.299F) ;
						array[gray] ++ ;
						pCurrent += xJumpBytes;
					} 
				}
			}

			//smoothing
			array = SmoothHistogram(array);
			array = SmoothHistogram(array);
		}
		#endregion

		#region GetHistogram8bpp()
		private void GetHistogram8bpp(BitmapData bitmapData, Color[] palette, Rectangle clip)
		{
			int			stride = bitmapData.Stride; 
			short		clipX = (short) clip.X ;
			short		clipY = (short) clip.Y ;
			short		clipRight = (short) clip.Right ;
			short		clipBottom = (short) clip.Bottom ;

			short		xJump = (short) ((clip.Width / 1500) + 1);
			short		yJump = (short) ((clip.Height / 1500) + 1);

			for(int i = 0; i < array.Length; i++)
				array[i] = 0 ;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer() + clipX; 
				byte*	pCurrent ; 

				for(int y = clipY; y < clipBottom; y += yJump) 
				{ 
					pCurrent = pOrig + y * stride;

					for(int x = clipX; x < clipRight; x += xJump) 
					{ 
						array[ palette[*pCurrent].R ] ++ ;
						pCurrent += xJump;
					} 
				}
			}

			//smoothing
			array = SmoothHistogram(array);
			array = SmoothHistogram(array);
		}
		#endregion

		#region GetHistogram8bppGrayscale()
		private void GetHistogram8bppGrayscale(BitmapData bitmapData, Rectangle clip)
		{
			int			stride = bitmapData.Stride; 
			short		clipX = (short) clip.X ;
			short		clipY = (short) clip.Y ;
			short		clipRight = (short) clip.Right ;
			short		clipBottom = (short) clip.Bottom ;

			short		xJump = (short) ((clip.Width / 1500) + 1);
			short		yJump = (short) ((clip.Height / 1500) + 1);

			for(int i = 0; i < 256 ; i++)
				array[i] = 0 ;

			unsafe
			{
				byte*	pOrig = (byte*) bitmapData.Scan0.ToPointer() + clipX; 
				byte*	pCurrent ; 

				for(int y = clipY; y < clipBottom; y += yJump) 
				{ 
					pCurrent = pOrig + y * stride;

					for(int x = clipX; x < clipRight; x += xJump) 
					{ 
						array[*pCurrent] ++ ;
						pCurrent += xJump;
					} 
				}
			}

			//smoothing
			array = SmoothHistogram(array);
			array = SmoothHistogram(array);
		}
		#endregion

		#region GetHistogram1bpp()
		private void GetHistogram1bpp(Bitmap image)
		{
			BitmapData	bitmapData = null ;
			
			try
			{
				bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat); 
				GetHistogram1bpp(bitmapData, Rectangle.FromLTRB(0, 0, image.Width, image.Height)) ;
			}
			finally
			{
				image.UnlockBits(bitmapData) ;
			}
		}

		private void GetHistogram1bpp(BitmapData bitmapData, Rectangle clip)
		{
			byte		color ;
			int			stride = bitmapData.Stride ;

			for(int i = 0; i < array.Length ; i++)
				array[i] = 0 ;

			unsafe
			{
				byte*	pOrig = (byte*)bitmapData.Scan0.ToPointer(); 
				int		i ; 

				for(int y = clip.Y; y < clip.Bottom; y++) 
					for(int x = clip.X / 8; x < clip.Right / 8; x++) 
					{ 
						color = pOrig[y * stride + x] ;

						for(i = 0; i < 8; i++)	
						{
							if( ((color >> i) & 0x1) == 1)
								array[255] ++ ;
							else
								array[0] ++ ;
						}
					} 
			}
		}
		#endregion

		#region Form_Paint()
		private void Form_Paint(object sender, PaintEventArgs e)
		{
			Rectangle	rect = new Rectangle(form.ClientRectangle.Location, new Size( form.ClientRectangle.Width - 1, form.ClientRectangle.Height - 1));
			double		zoomAll = (double) (form.ClientSize.Height - 2) / (array[extreme]) ;
			double		zoomDetail = (double) (form.ClientSize.Height - 2) / (array[extreme] / 16) ;
			Pen			pen = new Pen(Color.Black) ;
			Pen			penLines = new Pen(SystemColors.ControlLight) ;

			e.Graphics.DrawRectangle(penLines, 0, 0, 255, rect.Height) ;

			for(int i = 0; i < 32; i++)
				e.Graphics.DrawLine(penLines, i * 8, 0, i * 8, rect.Height) ;

			for(int i = 0; i < 256; i++)
				e.Graphics.DrawLine(pen, i, rect.Height - (int)((double)array[i] * zoomAll), i, rect.Height) ;

			e.Graphics.DrawRectangle(penLines, 258, 0, 255, rect.Height) ;

			for(int i = 0; i < 32; i++)
				e.Graphics.DrawLine(penLines, 258 + i * 8, 0, 258 + i * 8, rect.Height) ;

			for(int i = 0; i < 256; i++)
				e.Graphics.DrawLine(pen, i + 258, rect.Height - (int)((double)array[i] * zoomDetail), i + 258, rect.Height) ;
		}
		#endregion

		#region Form_MouseMove()
		private void Form_MouseMove(object sender, MouseEventArgs e)
		{
			try
			{
				this.textBoxPosition.Text = string.Format("{0}", (e.X < 256) ? e.X : (e.X - 258)) ;
				this.textBoxValue.Text = string.Format("{0}", array[(e.X < 256) ? e.X : (e.X - 258)] ) ;
			}
			catch
			{
			}
		}
		#endregion

		#region SmoothHistogram()
		private static uint[] SmoothHistogram(uint[] origArray)
		{
			uint[]		smoothArray = new uint[256];

			//smoothing
			smoothArray[0] = (origArray[0] + origArray[1] + origArray[2]) / 3 ;
			smoothArray[1] = (origArray[0] + origArray[1] + origArray[2] + origArray[3]) / 4 ;
			smoothArray[255] = (origArray[253] + origArray[254] + origArray[255]) / 3 ;
			smoothArray[254] = (origArray[252] + origArray[253] + origArray[254] + origArray[255]) / 4 ;

			for(byte i = 2; i < 254; i++)
				smoothArray[i] = (origArray[i-2] + origArray[i-1] + origArray[i] + origArray[i+1] + origArray[i+2]) / 5 ;

			return smoothArray;
		}
		#endregion

		#endregion
	}*/
}
