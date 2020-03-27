using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using BIP.Geometry;


namespace ImageProcessing.PageObjects
{
	public class PageObjects
	{
		private Size bitmapSize = Size.Empty;
		private int dpi = 72;
		
		private Symbols pageSymbols = null;
		private Symbols loneSymbols = null;
		private Pictures pictures = null;
		private Delimiters delimiters = null;
		private Words words = null;
		private Lines lines = null;
		private Paragraphs paragraphs = null;

		public PageObjects()
		{
		}

		//PUBLIC PROPERTIES
		#region public properties

		public Symbols		AllSymbols	{ get { return this.pageSymbols; } }
		public Symbols		LoneSymbols { get { return this.loneSymbols; } }
		public Pictures		Pictures	{ get { return this.pictures; } }
		public Delimiters	Delimiters	{ get { return this.delimiters; } }
		public Words		Words		{ get { return this.words; } }
		public Lines		Lines		{ get { return this.lines; } }
		public Paragraphs	Paragraphs	{ get { return this.paragraphs; } }

		public bool			IsDefined	{ get { return (this.pageSymbols != null); } }
		public Size?		BitmapSize	{ get { return this.bitmapSize.IsEmpty ? (Size?)null : this.bitmapSize; } }
		public int			Dpi			{ get { return this.dpi; } }

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Dispose()
		public void Dispose()
		{
			Reset();
		}
		#endregion
	
		#region Reset()
		public void Reset()
		{
			if (pageSymbols != null)
			{
				pageSymbols.Clear();
				pageSymbols = null;
			}

			if (loneSymbols != null)
			{
				loneSymbols.Clear();
				loneSymbols = null;
			}
			if (words != null)
			{
				words.Clear();
				words = null;
			}
			if (lines != null)
			{
				lines.Clear();
				lines = null;
			}
			if (pictures != null)
			{
				pictures.Clear();
				pictures = null;
			}
			if (delimiters != null)
			{
				delimiters.Clear();
				delimiters = null;
			}
			if (this.paragraphs != null)
			{
				this.paragraphs.Clear();
				paragraphs = null;
			}
		}
		#endregion

		#region CreatePageObjects()
		public void CreatePageObjects(Bitmap raster, Paging paging)
		{
			Symbols symbols = ObjectLocator.FindObjects(raster, Rectangle.Empty);

			CreatePageObjects(symbols, raster.Size, Convert.ToInt32(raster.HorizontalResolution), paging);
		}

		public void CreatePageObjects(Symbols symbols, Size rasterSize, int dpi, Paging paging)
		{
			this.bitmapSize = rasterSize;
			this.pageSymbols = symbols;
			this.dpi = dpi;

#if SAVE_RESULTS
			string letter = (paging == Paging.Left) ? " L" : ((paging == Paging.Right) ? " R" : "");
			this.pageSymbols.DrawToFile(Debug.SaveToDir + "200 Symbols" + letter + ".png", rasterSize);
#endif

			this.loneSymbols = new Symbols();
			this.loneSymbols.AddRange(this.pageSymbols);

			this.delimiters = new Delimiters(this.LoneSymbols, rasterSize);

#if SAVE_RESULTS
			this.delimiters.DrawToFile(Debug.SaveToDir + "201 Delimiters" + letter + ".png", rasterSize);
#endif

			this.pictures = new Pictures(this.LoneSymbols);
			if (this.Delimiters.Count > 0)
				this.Pictures.Merge(this.Delimiters, this.LoneSymbols);

#if SAVE_RESULTS
			Debug.DrawToFile(this.Pictures, "202 Pictures" + letter + ".png", rasterSize);
			this.LoneSymbols.DrawToFile(Debug.SaveToDir + "203 Symbols after picture merge" + letter + ".png", rasterSize);
#endif

			this.words = new Words(this.LoneSymbols, rasterSize);

			for (int i = this.LoneSymbols.Count - 1; i >= 0; i--)
				if (!(((this.LoneSymbols[i].IsFrame || this.LoneSymbols[i].IsPicture) || this.LoneSymbols[i].IsLetter) || this.LoneSymbols[i].IsLine))
					this.LoneSymbols.RemoveAt(i);

#if SAVE_RESULTS
			this.Words.DrawToFile(Debug.SaveToDir + "204 Words" + letter + ".png", rasterSize);
			this.LoneSymbols.DrawToFile(Debug.SaveToDir + "205 Lone Symbols" + letter + ".png", rasterSize);
#endif

			this.Delimiters.MergeWithLoneSymbols(this.LoneSymbols);
			this.Pictures.Merge(this.Delimiters, this.LoneSymbols);

#if SAVE_RESULTS
			this.Delimiters.DrawToFile(Debug.SaveToDir + "206 Delimiters" + letter + ".png", rasterSize);
			this.Pictures.DrawToFile(Debug.SaveToDir + "207 Pictures" + letter + ".png", rasterSize);
			this.Pictures.DrawObjectShapes(Debug.SaveToDir + "208 PictureShapes" + letter + ".png", rasterSize);
			this.Pictures.DrawConvexEnvelopes(Debug.SaveToDir + "209 PictureConvexEnvelopes" + letter + ".png", rasterSize);
#endif

			DelimiterZones delimiterZones = this.Delimiters.GetDelimiterZones(LoneSymbols, Words, Pictures, rasterSize);
			SignDelimiterZones(this.Words, this.LoneSymbols, this.Pictures, delimiterZones);

#if SAVE_RESULTS
			delimiterZones.DrawToFile(Debug.SaveToDir + @"210 Delimiter Zones" + letter + ".png", rasterSize);
#endif

			this.lines = new Lines(this.Words, this.LoneSymbols);
			this.Lines.Validate(Delimiters);

#if SAVE_RESULTS
			this.Lines.DrawToFile(Debug.SaveToDir + "211 Lines" + letter + ".png", rasterSize);
#endif

			this.paragraphs = new Paragraphs(LoneSymbols, Words, Lines, rasterSize);

#if SAVE_RESULTS
			Paragraphs.DrawToFile(Debug.SaveToDir + "212 Paragraphs" + letter + ".png", rasterSize);
#endif
		}
		#endregion

		#region GetAllSymbols()
		public Symbols GetAllSymbols(RatioRect clip)
		{
			if (this.pageSymbols != null && this.bitmapSize.IsEmpty == false)
				return pageSymbols.GetObjectsInClip(new Rectangle((int) (clip.X * this.bitmapSize.Width), (int) (clip.Y * this.bitmapSize.Height),
					(int) (clip.Width * this.bitmapSize.Width), (int) (clip.Height * this.bitmapSize.Height)));
			else
				return new Symbols();
		}
		#endregion

		#region GetLoneSymbols()
		public Symbols GetLoneSymbols(RatioRect clip)
		{
			if (this.loneSymbols != null && this.bitmapSize.IsEmpty == false)
				return this.loneSymbols.GetObjectsInClip(new Rectangle((int)(clip.X * this.bitmapSize.Width), (int)(clip.Y * this.bitmapSize.Height),
					(int)(clip.Width * this.bitmapSize.Width), (int)(clip.Height * this.bitmapSize.Height)));
			else
				return new Symbols();
		}
		#endregion

		#region GetPictures()
		public Pictures GetPictures(RatioRect clip)
		{
			if (this.pictures != null && this.bitmapSize.IsEmpty == false)
				return this.pictures.GetPicturesInClip(new Rectangle((int)(clip.X * this.bitmapSize.Width), (int)(clip.Y * this.bitmapSize.Height),
					(int)(clip.Width * this.bitmapSize.Width), (int)(clip.Height * this.bitmapSize.Height)));
			else
				return new Pictures();
		}
		#endregion

		#region GetDelimiters()
		public Delimiters GetDelimiters(RatioRect clip)
		{
			if (this.delimiters != null && this.bitmapSize.IsEmpty == false)
				return this.delimiters.GetDelimitersInClip(new Rectangle((int)(clip.X * this.bitmapSize.Width), (int)(clip.Y * this.bitmapSize.Height),
					(int)(clip.Width * this.bitmapSize.Width), (int)(clip.Height * this.bitmapSize.Height)));
			else
				return new Delimiters();
		}
		#endregion

		#region GetWords()
		public Words GetWords(RatioRect clip)
		{
			if (this.words != null && this.bitmapSize.IsEmpty == false)
				return this.words.GetWordsInClip(new Rectangle((int)(clip.X * this.bitmapSize.Width), (int)(clip.Y * this.bitmapSize.Height),
					(int)(clip.Width * this.bitmapSize.Width), (int)(clip.Height * this.bitmapSize.Height)));
			else
				return new Words();
		}
		#endregion

		#region GetLines()
		public Lines GetLines(RatioRect clip)
		{
			if (this.lines != null && this.bitmapSize.IsEmpty == false)
				return this.lines.GetLinesInClip(new Rectangle((int)(clip.X * this.bitmapSize.Width), (int)(clip.Y * this.bitmapSize.Height),
					(int)(clip.Width * this.bitmapSize.Width), (int)(clip.Height * this.bitmapSize.Height)));
			else
				return new Lines();
		}
		#endregion

		#region GetParagraphs()
		public Paragraphs GetParagraphs(RatioRect clip)
		{
			if (this.paragraphs != null && this.bitmapSize.IsEmpty == false)
				return this.paragraphs.GetParagraphsInClip(new Rectangle((int)(clip.X * this.bitmapSize.Width), (int)(clip.Y * this.bitmapSize.Height),
					(int)(clip.Width * this.bitmapSize.Width), (int)(clip.Height * this.bitmapSize.Height)));
			else
				return new Paragraphs();
		}
		#endregion

		#region ResizeSettings()
		public void ResizeSettings(double zoom)
		{
		}
		#endregion

		#region DrawToFile()
		public void DrawToFile(string filePath, Bitmap original)
		{
#if SAVE_RESULTS
			try
			{
				Bitmap result = ImageCopier.Get24Bpp(original);

				this.LoneSymbols.DrawToImage(result);
				this.pictures.DrawToImage(result);
				this.Delimiters.DrawToImage(result);
				this.paragraphs.DrawToImage(result);


				result.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
				result.Dispose();
			}
			catch { }
#endif
		}

		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region SignDelimiterZones()
		static void SignDelimiterZones(Words words, Symbols symbols, Pictures pictures, DelimiterZones zones)
		{
			foreach (Word word in words)
				word.Zone = zones.GetZone(word.Rectangle);
			foreach (Picture picture in pictures)
				picture.Zone = zones.GetZone(picture.Rectangle);
			foreach (Symbol symbol in symbols)
				if (symbol.Word == null)
					symbol.Zone = zones.GetZone(symbol.Rectangle);
		}
		#endregion

		#endregion




	}
}
