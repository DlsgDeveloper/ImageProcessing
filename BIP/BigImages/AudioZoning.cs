/*
 * 1, Rasterize
 * 2, Get Objects
 * 3, Get rid of noise, lines, large objects
 * 4, get words
 * 5, get paragraphs
 * 6, add not used letters to words
 * 7, get lines
 * 8, consider lines as paragraphs
 * 9, add lone words to paragraphs
 * 10 validate paragraphs
 * 11, sort
 * 12, return
 */


using System;
using System.IO;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;

using ImageProcessing.PageObjects;
using System.Collections.Generic;
using ImageProcessing.Languages;


namespace ImageProcessing.BigImages
{
	public class AudioZoning
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public AudioZoning()
		{
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region GetZones()
		public List<BIP.Geometry.RatioRect> GetZones(string imageFile)
		{
			Zones zones = new Zones();
			//List<BIP.Geometry.RatioRect> zones = new List<BIP.Geometry.RatioRect>();

			if (File.Exists(imageFile) == false)
				throw new IOException(BIPStrings.ImagePathTo_STR + imageFile + BIPStrings.IsIncorrect_STR);

			ImageProcessing.ObjectsRecognition.DocumentContent documentContent;
			Bitmap raster;
			Rectangle imageRect;

			using (ItDecoder itDecoder = new ItDecoder(imageFile))
			{
				ImageProcessing.BigImages.ContentLocator contentLocator = new ImageProcessing.BigImages.ContentLocator();
				documentContent = contentLocator.GetContent(itDecoder);
				imageRect = new Rectangle(0, 0, itDecoder.Width, itDecoder.Height);

				RaiseProgressChanged(0.10F);

				raster = GetRaster(itDecoder, documentContent);
			}

			RaiseProgressChanged(0.70F);

			if (documentContent.TwoPagesBook)
			{
				Symbols symbols = ObjectLocator.FindObjects(raster, Rectangle.Empty);
				Symbols symbolsL, symbolsR;

				SplitSymbols(symbols, documentContent, out symbolsL, out symbolsR, raster.Size);

				PageObjects.PageObjects pageObjectsL = new ImageProcessing.PageObjects.PageObjects();
				PageObjects.PageObjects pageObjectsR = new ImageProcessing.PageObjects.PageObjects();

				pageObjectsL.CreatePageObjects(symbolsL, raster.Size, Convert.ToInt32(raster.HorizontalResolution), ImageProcessing.Paging.Left);
				pageObjectsR.CreatePageObjects(symbolsR, raster.Size, Convert.ToInt32(raster.HorizontalResolution), ImageProcessing.Paging.Right);

				RaiseProgressChanged(0.80F);

#if SAVE_RESULTS
				pageObjectsL.DrawToFile(Debug.SaveToDir + @"301 page objects L.png", new Bitmap(raster.Width, raster.Height, PixelFormat.Format24bppRgb));
				pageObjectsR.DrawToFile(Debug.SaveToDir + @"301 page objects R.png", new Bitmap(raster.Width, raster.Height, PixelFormat.Format24bppRgb));
#endif

				MergeObjectsInColumns(pageObjectsL, raster.Size);
				MergeObjectsInColumns(pageObjectsR, raster.Size);

#if SAVE_RESULTS
				pageObjectsL.DrawToFile(Debug.SaveToDir + @"304 page objects merged L.png", new Bitmap(raster.Width, raster.Height, PixelFormat.Format24bppRgb));
				pageObjectsR.DrawToFile(Debug.SaveToDir + @"304 page objects merged R.png", new Bitmap(raster.Width, raster.Height, PixelFormat.Format24bppRgb));
#endif

				RaiseProgressChanged(0.87F);

				Zones pageLZones = GetPageZones(pageObjectsL, raster.Size);
				Zones pageRZones = GetPageZones(pageObjectsR, raster.Size);

#if SAVE_RESULTS
				pageLZones.DrawToFile(Debug.SaveToDir + @"305 audio zones L.png", new Bitmap(raster.Width, raster.Height, PixelFormat.Format24bppRgb));
				pageRZones.DrawToFile(Debug.SaveToDir + @"305 audio zones R.png", new Bitmap(raster.Width, raster.Height, PixelFormat.Format24bppRgb));
#endif
				
				zones.AddRange(pageLZones);
				zones.AddRange(pageRZones);
			}
			else
			{
				PageObjects.PageObjects pageObjects = new ImageProcessing.PageObjects.PageObjects();
				pageObjects.CreatePageObjects(raster, ImageProcessing.Paging.Both);

				RaiseProgressChanged(0.80F);

#if SAVE_RESULTS
				pageObjects.DrawToFile(Debug.SaveToDir + @"301 page objects.png", new Bitmap(raster.Width, raster.Height, PixelFormat.Format24bppRgb));
#endif

				MergeObjectsInColumns(pageObjects, raster.Size);

#if SAVE_RESULTS
				pageObjects.DrawToFile(Debug.SaveToDir + @"304 page objects merged.png", new Bitmap(raster.Width, raster.Height, PixelFormat.Format24bppRgb));
#endif

				RaiseProgressChanged(0.87F);

				Zones pageZones = GetPageZones(pageObjects, raster.Size);

#if SAVE_RESULTS
				pageZones.DrawToFile(Debug.SaveToDir + @"305 audio zones.png", new Bitmap(raster.Width, raster.Height, PixelFormat.Format24bppRgb));
#endif

				zones.AddRange(pageZones);
			}

			RaiseProgressChanged(0.95F);
			foreach (Zone zone in zones)
				zone.Rectangle = Rectangle.Intersect(Rectangle.Inflate(zone.Rectangle, 10, 10), imageRect);

#if SAVE_RESULTS
			zones.DrawToFile(Debug.SaveToDir + @"305 audio zones.png", new Bitmap(imageFile));
#endif

			List<BIP.Geometry.RatioRect> ratioZones = new List<BIP.Geometry.RatioRect>();
			
			foreach (Zone zone in zones)
				ratioZones.Add(new BIP.Geometry.RatioRect(zone.X / (double)raster.Width, zone.Y / (double)raster.Height, zone.Width / (double)raster.Width, zone.Height / (double)raster.Height));

			if (ratioZones.Count == 0)
				ratioZones.Add(new BIP.Geometry.RatioRect(0, 0, 1, 1));

			RaiseProgressChanged(1.0F);
			return ratioZones;
		}
		#endregion

		#region GetGrayBitmap()
		public unsafe Bitmap GetGrayBitmap(ImageProcessing.BigImages.ItDecoder itDecoder)
		{
			if (itDecoder.PixelsFormat == PixelsFormat.Format8bppGray)
				return new Bitmap(itDecoder.FilePath);

			Bitmap source = null;

			int stripHeightMax = Misc.GetStripHeightMax(itDecoder);

			if (stripHeightMax < itDecoder.Height)
			{
				List<Bitmap> bitmapsToMerge = new List<Bitmap>();

				for (int stripY = 0; stripY < itDecoder.Height; stripY = stripY + stripHeightMax)
				{
					try
					{
						int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - stripY);

						source = itDecoder.GetClip(new Rectangle(0, stripY, itDecoder.Width, stripHeight));

						Bitmap resampled = ImageProcessing.Resampling.Resample(source, PixelsFormat.Format8bppGray);

						source.Dispose();
						source = null;

						bitmapsToMerge.Add(resampled);

						RaiseProgressChanged((stripY + stripHeight) / (float)itDecoder.Height);
					}
					finally
					{
						if (source != null)
						{
							source.Dispose();
							source = null;
						}
						itDecoder.ReleaseAllocatedMemory(null);
					}
				}

				Bitmap merge = ImageProcessing.Merging.MergeVertically(bitmapsToMerge);

				foreach (Bitmap b in bitmapsToMerge)
					b.Dispose();

				if (itDecoder.DpiX > 0)
					merge.SetResolution(itDecoder.DpiX, itDecoder.DpiY);
				return merge;
			}
			else
			{
				try
				{
					source = itDecoder.GetClip(new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));

					Bitmap resampled = ImageProcessing.Resampling.Resample(source, PixelsFormat.Format8bppGray);

					source.Dispose();
					source = null;

					RaiseProgressChanged(1);

					if (itDecoder.DpiX > 0)
						resampled.SetResolution(itDecoder.DpiX, itDecoder.DpiY);
					return resampled;
				}
				finally
				{
					if (source != null)
					{
						source.Dispose();
						source = null;
					}
					itDecoder.ReleaseAllocatedMemory(source);
				}
			}
		}
		#endregion

		#region GetGrayBitmap()
		public unsafe Bitmap GetGrayBitmap(ImageProcessing.BigImages.ItDecoder itDecoder, int desiredDpi)
		{
			Bitmap source = null;
			Bitmap resized = null;

			double zoom = desiredDpi / (double)Convert.ToInt32(itDecoder.DpiX);
			int stripHeightMax = Misc.GetStripHeightMax(itDecoder);

			if (stripHeightMax < itDecoder.Height)
			{
				List<Bitmap> bitmapsToMerge = new List<Bitmap>();

				for (int stripY = 0; stripY < itDecoder.Height; stripY = stripY + stripHeightMax)
				{
					try
					{
						int stripHeight = Math.Min(stripHeightMax, itDecoder.Height - stripY);

						source = itDecoder.GetClip(new Rectangle(0, stripY, itDecoder.Width, stripHeight));
						resized = ImageProcessing.Resizing.Resize(source, Rectangle.Empty, zoom, ImageProcessing.Resizing.ResizeMode.Quality);

						source.Dispose();
						source = null;

						Bitmap resampled = ImageProcessing.Resampling.Resample(resized, PixelsFormat.Format8bppGray);

						resized.Dispose();
						resized = null;

						bitmapsToMerge.Add(resampled);

						RaiseProgressChanged((stripY + stripHeight) / (float)itDecoder.Height);
					}
					finally
					{
						if (source != null)
						{
							source.Dispose();
							source = null;
						}
						itDecoder.ReleaseAllocatedMemory(source);
						if (resized != null)
						{
							resized.Dispose();
							resized = null;
						}
					}
				}

				return ImageProcessing.Merging.MergeVertically(bitmapsToMerge);
			}
			else
			{
				try
				{
					source = itDecoder.GetClip(new Rectangle(0, 0, itDecoder.Width, itDecoder.Height));
					resized = ImageProcessing.Resizing.Resize(source, Rectangle.Empty, zoom, ImageProcessing.Resizing.ResizeMode.Quality);

					source.Dispose();
					source = null;
					Bitmap resampled = ImageProcessing.Resampling.Resample(resized, PixelsFormat.Format8bppGray);

					resized.Dispose();
					resized = null;

					RaiseProgressChanged(1);

					return resampled;
				}
				finally
				{
					if (source != null)
					{
						source.Dispose();
						source = null;
					}
					itDecoder.ReleaseAllocatedMemory(source);
					if (resized != null)
					{
						resized.Dispose();
						resized = null;
					}
				}
			}
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region MergeObjectsInColumns()
		/// <summary>
		/// First, it locates columns. Then, in each column, it merges objects in each column
		/// </summary>
		/// <param name="pageObjects"></param>
		/// <param name="imageSize"></param>
		private static void MergeObjectsInColumns(PageObjects.PageObjects pageObjects, Size imageSize)
		{
			bool changed;

			do
			{
				changed = false;
				int? columnsWidth = pageObjects.Paragraphs.GetColumnsWidth();

				if (columnsWidth.HasValue && columnsWidth >= 1)
				{
					DelimiterZones delimiterZones = GetDelimiterZones(pageObjects, columnsWidth.Value);

					if (delimiterZones != null)
						changed = MergeObjectsInTheSameColumn(delimiterZones, columnsWidth.Value, pageObjects);
					else
						changed = MergeObjectsInTheSameColumn(pageObjects);

					//SignObjectsToTheColumns(pageObjects, delimiterZones);
				}
			} while (changed);
		}
		#endregion

		#region GetPageZones()
		private static Zones GetPageZones(PageObjects.PageObjects pageObjects, Size imageSize)
		{
			int? columnsWidth = pageObjects.Paragraphs.GetColumnsWidth();		
			Zones	zones = new Zones();

			foreach (PageObjects.Paragraph paragraph in pageObjects.Paragraphs)
			{
				if (paragraph.Rectangle.Width > 50)
				{
					Rectangle r = paragraph.Rectangle;

					zones.Add(new Zone(paragraph, r));
				}
			}

			/*foreach (Picture picture in itPage.Pictures)
			{
				if (picture.ObjectShape.MaxPixelWidth > 50 && picture.ObjectShape.MaxPixelHeight > 50)
				{
					Rectangle r = picture.Rectangle;

					zones.Add(new Zone(picture, r));
				}
			}*/

			/*if (zones.Count > 1)
			{
				zones.MergeZones(columnsWidth);

				for (int i = zones.Count - 1; i >= 0; i--)
					if (zones[i].Width < 40)
						zones.RemoveAt(i);

				zones.Sort(pageObjects.Delimiters, imageSize);
			}
			else if (zones.Count == 0)
			{
				Symbols symbols = pageObjects.AllSymbols;
				Zone zone;

				if (symbols.Count > 0)
				{
					int x = symbols[0].X, y = symbols[0].Y, r = symbols[0].Right, b = symbols[0].Bottom;

					for (int i = 1; i < symbols.Count; i++)
					{
						if (x > symbols[i].X)
							x = symbols[i].X;
						if (y > symbols[i].Y)
							y = symbols[i].Y;
						if (r < symbols[i].Right)
							r = symbols[i].Right;
						if (b < symbols[i].Bottom)
							b = symbols[i].Bottom;
					}

					zone = new Zone(Rectangle.FromLTRB(x, y, r, b));
				}
				else
				{
					zone = new Zone(new Rectangle(0, 0, imageSize.Width, imageSize.Height));
				}

				zones.Add(zone);
			}*/
			//zones.Inflate(10, itPage.ItImage.PageObjects.BitmapSize.Value);
			
			return zones;
		}
		#endregion

		#region GetDelimiters()
		/*private static PageObjects.Delimiters GetDelimiters(PageObjects.Symbols symbols, Bitmap raster)
		{
#if DEBUG
			DateTime	start = DateTime.Now ;
#endif
			PageObjects.Delimiters	delimiters = new PageObjects.Delimiters();
			BitmapData	bmpData = null;
		
			try
			{
				bmpData = raster.LockBits(new Rectangle(0, 0, raster.Width, raster.Height), ImageLockMode.ReadOnly, raster.PixelFormat);

				for (int i = symbols.Count - 1; i >= 0; i-- )
				{
					Symbol symbol = symbols[i];

					if (symbol.IsLine)
					{
						PageObjects.Delimiter delimiter = new Delimiter(symbol);

						if (delimiter != null)
						{
							delimiters.Add(delimiter);
							symbols.RemoveAt(i);
							goto Mark;
						}
					}

					if (symbol.IsPicture || symbol.IsFrame)
					{
						PageObjects.Delimiters pictDelimiters = GetPictureDelimiters(symbol, bmpData);

						foreach (PageObjects.Delimiter delimiter in pictDelimiters)
							delimiters.Add(delimiter);
					}

				Mark: ;
				}

				List<double> delimiterAngles = new List<double>();

				foreach (Delimiter delimiter in delimiters)
				{
					double angle = delimiter.Angle;
					while (angle > Math.PI / 4)
						angle -= Math.PI / 2;
					while (angle < - Math.PI / 4)
						angle += Math.PI / 2;
				
					delimiterAngles.Add(angle);
				}

				if (delimiterAngles.Count > 2)
				{
					delimiterAngles.Sort();
					double medianAngle = (double) delimiterAngles[delimiterAngles.Count / 2];

					for (int i = delimiters.Count - 1; i >= 0; i--)
					{
						double angle = delimiters[i].Angle;
						
						while (angle > Math.PI / 4)
							angle -= Math.PI / 2;
						while (angle < -Math.PI / 4)
							angle += Math.PI / 2;

						if (angle - medianAngle > 0.0698 || angle - medianAngle < -0.0698)
						{
							Symbol symbol = new Symbol(delimiters[i].ObjectMap);

							symbol.ObjectType = Symbol.Type.Picture;

							symbols.Add(symbol);
							delimiters.RemoveAt(i);
						}
					}
				}
			}
			finally
			{
				if(bmpData != null)
					raster.UnlockBits(bmpData);
			}
			
#if DEBUG
			Console.WriteLine(string.Format("Zoning, GetDelimiters(): {0}, Count: {1}" , DateTime.Now.Subtract(start).ToString(), delimiters.Count));
#endif
			
			return delimiters;
		}*/
		#endregion

		#region GetPictureDelimiters()
		/*private unsafe static PageObjects.Delimiters GetPictureDelimiters(PageObjects.Symbol symbol, BitmapData bmpData)
		{
			PageObjects.Delimiters	delimiters = new ImageProcessing.PageObjects.Delimiters();
			int[,]					array = GetObjectShape(symbol, (byte*) bmpData.Scan0.ToPointer(), bmpData.Stride);	
			Crop					crop = GetCrop(symbol, array);
			ObjectByCorners			objectByCorners = FindCorners(symbol, array, crop);

			foreach(Edge edge in objectByCorners.ValidEdges)
			{
				if(edge.IsVertical)
					delimiters.Add(new PageObjects.Delimiter(edge.Point1, edge.Point2, PageObjects.Delimiter.Type.Vertical));
				else
					delimiters.Add(new PageObjects.Delimiter(edge.Point1, edge.Point2, PageObjects.Delimiter.Type.Horizontal));
			}
			
			return delimiters;
		}*/
		#endregion

		#region GetCrop()
		/*private static unsafe Crop GetCrop(PageObjects.Symbol symbol, int[,] array)
		{					
			int		width = symbol.Width;
			int		height = symbol.Height;
			int		x, y;
			Point	pL = new Point(int.MaxValue, int.MaxValue);
			Point	pT = new Point(int.MaxValue, int.MaxValue);
			Point	pR = new Point(int.MinValue, int.MinValue);
			Point	pB = new Point(int.MinValue, int.MinValue);

			//find left point
			x = 0;
			for(y = 0; y < height; y++)
				if(array[y,x] == -1)
				{
					pL = new Point(x, y);
					break;
				}

			//find top point
			y = 0;
			for(x = 0; x < width; x++)
				if(array[y,x] == -1)
				{
					pT = new Point(x, y);
					break;
				}

			//find right point
			x = width - 1;
			for(y = 0; y < height; y++)
				if(array[y,x] == -1)
				{
					pR = new Point(x, y);
					break;
				}

			//find bottom point
			y = height - 1;
			for(x = 0; x < width; x++)
				if(array[y,x] == -1)
				{
					pB = new Point(x, y);
					break;
				}

			pL.Offset(symbol.X, symbol.Y);
			pT.Offset(symbol.X, symbol.Y);
			pR.Offset(symbol.X, symbol.Y);
			pB.Offset(symbol.X, symbol.Y);
			return new Crop(pL, pT, pR, pB);
		}*/
		#endregion

		#region IsObjectHollow()
		/*private static unsafe bool IsObjectHollow(PageObjects.Symbol symbol, byte* scan0, int stride)
		{					
			int			xFrom = Convert.ToInt32(symbol.X + symbol.Width * .1F);
			int			xTo = Convert.ToInt32(symbol.X + symbol.Width * .8F);
			int			yFrom = Convert.ToInt32(symbol.Y + symbol.Height * .1F);
			int			yTo = Convert.ToInt32(symbol.Y + symbol.Height * .8F);
			int[,]		array = GetObjectShape(symbol, scan0, stride);
			int			x, y;

			for(y = yFrom; y < yTo; y++)
				for(x = xFrom; x < xTo; x++)
					if(array[y,x] == -1)
						return false;

			return true;
		}*/
		#endregion

		#region FindCorners()
		/*private static ObjectByCorners FindCorners(PageObjects.Symbol symbol, int[,] array, Crop crop)
		{
			int		xMax = array.GetLength(1) - 1;
			int		yMax = array.GetLength(0) - 1;
			int		maxSteps = Math.Max(xMax, yMax);
			int		x, y, i, j;
			Point	ul = new Point(0, 0);
			Point	ur = new Point(xMax, 0);
			Point	ll = new Point(0, yMax);
			Point	lr = new Point(xMax, yMax);

			//ul
			for(i = 0; i <= maxSteps; i++)
			{
				for(j = 0; j <= i; j++)
				{
					x = j;
					y = i - j;

					if( (x >= 0) && (y >= 0) && (x <= xMax) && (y <=yMax) && array[y,x] == -1)
					{
						ul = new Point(x, y);
						j = int.MaxValue - 1;
						i = int.MaxValue - 1;
					}
				}
			}
			
			//ur
			for(i = 0; i <= maxSteps; i++)
			{
				for(j = 0; j <= i; j++)
				{
					x = xMax - j;
					y = i - j;

					if( (x >= 0) && (y >= 0) && (x <= xMax) && (y <=yMax) && array[y,x] == -1)
					{
						ur = new Point(x, y);
						j = int.MaxValue - 1;
						i = int.MaxValue - 1;
					}
				}
			}

			//ll
			for(i = 0; i <= maxSteps; i++)
			{
				for(j = 0; j <= i; j++)
				{
					x = j;
					y = yMax - (i - j);

					if( (x >= 0) && (y >= 0) && (x <= xMax) && (y <=yMax) && array[y,x] == -1)
					{
						ll = new Point(x, y);
						j = int.MaxValue - 1;
						i = int.MaxValue - 1;
					}
				}
			}
			
			//lr
			for(i = 0; i <= maxSteps; i++)
			{
				for(j = 0; j <= i; j++)
				{
					x = xMax - j;
					y = yMax - (i - j);

					if( (x >= 0) && (y >= 0) && (x <= xMax) && (y <=yMax) && array[y,x] == -1)
					{
						lr = new Point(x, y);
						j = int.MaxValue - 1;
						i = int.MaxValue - 1;
					}
				}
			}
			
			ul.Offset(symbol.X, symbol.Y);
			ur.Offset(symbol.X, symbol.Y);
			ll.Offset(symbol.X, symbol.Y);
			lr.Offset(symbol.X, symbol.Y);
			return new ObjectByCorners(array, ul, ur, ll, lr, crop, .1F);
		}*/
		#endregion

		#region GetObjectShape()
		/// <summary>
		/// Returns array of integers of the object on image where 0 - background, -1 object, <1, ...> other objects.
		/// </summary>
		/// <param name="symbol">Continuous image object.</param>
		/// <param name="scan0">Pointer to the upper left corner of entire image.</param>
		/// <param name="stride">Number of bytes of 1 image line.</param>
		/// <returns>Returns array of integers of the object on image where 0 - background, -1 object, <1, ...> other objects.</returns>
		/*private static unsafe int[,] GetObjectShape(PageObjects.Symbol symbol, byte* scan0, int stride)
		{					
			int			width = symbol.Width;
			int			height = symbol.Height;
			int[,]		array = new int[height, width];
			int			x, y;
			int			index = 1;

			//point 0,0
			if((scan0[symbol.Y * stride + (symbol.X) / 8] & (0x80 >> (symbol.X % 8))) > 0)
				array[0,0] = index++;

			//first column
			for(y = 1; y < height; y++)
			{
				if((scan0[(symbol.Y + y) * stride + (symbol.X) / 8] & (0x80 >> (symbol.X % 8))) > 0)
				{
					if(array[y-1, 0] > 0)
						array[y,0] = array[y-1,0];
					else
						array[y,0] = index++;
				}
			}

			//first row
			y = 0;
			for(x = 1; x < width; x++)
			{
				if((scan0[(symbol.Y + y) * stride + (symbol.X + x) / 8] & (0x80 >> ((symbol.X + x) % 8))) > 0)
				{
					if(array[y,x-1] > 0)
						array[y,x] = array[y,x-1];
					else 
						array[y,x] = index++;
				}
			}

			for(y = 1; y < height; y++)
			{
				for(x = 1; x < width; x++)
				{
					if((scan0[(symbol.Y + y) * stride + (symbol.X + x) / 8] & (0x80 >> ((symbol.X + x) % 8))) > 0)
					{
						if(array[y-1,x] > 0)
						{
							array[y,x] = array[y-1,x];
						}
						else if(array[y-1,x-1] > 0)
						{
							array[y,x] = array[y-1,x-1];
						}
						else if(x < width - 1 && array[y-1,x+1] > 0)
						{
							array[y,x] = array[y-1,x+1];
						}
						else if(array[y,x-1] > 0)
						{
							array[y,x] = array[y,x-1];
						}
						else 
							array[y,x] = index++;
					}
				}
			}

			ArrayList	objectPixels = new ArrayList();
			objectPixels.Add(new Point(symbol.APixelX - symbol.X, symbol.APixelY - symbol.Y));
			array[symbol.APixelY - symbol.Y, symbol.APixelX - symbol.X] = -1;

			while(objectPixels.Count > 0)
			{
				Point	pixel = (Point) objectPixels[0];
				objectPixels.RemoveAt(0);

				for(x = ((pixel.X - 1 > 0) ? pixel.X - 1: 0); x <= ((pixel.X + 1 < width) ? pixel.X + 1 : width - 1); x++)
					for(y = ((pixel.Y - 1 > 0) ? pixel.Y - 1: 0); y <= ((pixel.Y + 1 < height) ? pixel.Y + 1 : height - 1); y++)
					{
						if(array[y,x] > 0)
						{
							array[y,x] = -1;
							objectPixels.Add(new Point(x,y));
						}
					}
			}

			return array;
		}*/
		#endregion

		#region GetWords()
		/*private static void GetWords(Words words, Delimiter delimiter, out Words words1, out Words words2, out Words notUsedWords)
		{
			words1 = new Words();
			words2 = new Words();
			notUsedWords = new Words();

			if(delimiter.IsHorizontal)
			{
				foreach(Word word in words)
				{
					int		y = delimiter.P1.Y + Convert.ToInt32(delimiter.GetY(word.XHalf));

					if(y > word.Bottom)
						words1.Add(word);
					else if (y < word.Y)
						words2.Add(word);
					else 
						notUsedWords.Add(word);
				}
			}
			else
			{
				foreach(Word word in words)
				{
					int		x = delimiter.P1.X + Convert.ToInt32(delimiter.GetX(word.Seat));

					if(x > word.X)
						words1.Add(word);
					else if (x < word.Right)
						words2.Add(word);
					else 
						notUsedWords.Add(word);
				}
			}
		}*/
		#endregion

		#region GetDelimiterZones()
		/// <summary>
		/// First, it counts columns width. Then, counting only paragraphs with right width, it assigns those paragraphs to one of the 
		/// virtual columns. Then, based on those lists, it computes average left and right side of each column.
		/// </summary>
		/// <param name="itImage"></param>
		private static PageObjects.DelimiterZones GetDelimiterZones(PageObjects.PageObjects pageObjects, int columnsWidth)
		{
			Rectangle?	c = GetClip(pageObjects.AllSymbols);

			if (c != null)
			{
				Rectangle	contentClip = c.Value;
				int			columnsCount = Convert.ToInt32(contentClip.Width / (double)columnsWidth);

				if (columnsCount > 1 && columnsCount <= 4)
				{
					Paragraphs paragraphs = pageObjects.Paragraphs;

					if (columnsCount > 1 && paragraphs.Count >= 2)
					{
						Paragraphs[] paragraphsColumns = new Paragraphs[columnsCount];
						for (int i = 0; i < columnsCount; i++ )
							paragraphsColumns[i] = new Paragraphs();

						foreach (Paragraph paragraph in paragraphs)
						{
							if (paragraph.Width > columnsWidth * 0.8 && columnsWidth < columnsWidth * 1.2)
							{
								int paragraphMiddle = (paragraph.X + paragraph.Right) / 2;
								int columnIndex = (int)((paragraphMiddle - contentClip.X) * columnsCount / (double)contentClip.Width);

								if (columnIndex >= 0 && columnIndex < columnsCount)
									paragraphsColumns[columnIndex].Add(paragraph);
							}
						}

						PageObjects.DelimiterZones delimiterZones = new DelimiterZones();

						foreach (Paragraphs paragraphsColumn in paragraphsColumns)
						{
							if (paragraphsColumn.Count > 0)
							{
								int left = GetLeftSideMedian(paragraphsColumn);
								int right = GetRightSideMedian(paragraphsColumn);
								Rectangle zone = Rectangle.FromLTRB(left, contentClip.Top, right, contentClip.Bottom);

								zone.Inflate(50, 0);
								delimiterZones.Add(new DelimiterZone(Rectangle.Intersect(zone, contentClip)));
							}
						}

						return delimiterZones;
					}
				}
			}

			return null;
		}
		#endregion

		#region GetClip()
		private static Rectangle? GetClip(PageObjects.Symbols symbols)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			if (symbols.Count > 0)
			{
				int x = symbols[0].X;
				int y = symbols[0].Y;
				int r = symbols[0].Right;
				int b = symbols[0].Bottom;

				for (int i = 1; i < symbols.Count; i++)
				{
					if (x > symbols[i].X)
						x = symbols[i].X;
					if (y > symbols[i].Y)
						y = symbols[i].Y;
					if (r < symbols[i].Right)
						r = symbols[i].Right;
					if (b < symbols[i].Bottom)
						b = symbols[i].Bottom;
				}

				return Rectangle.FromLTRB(x, y, r, b);
			}

			return null;
		}
		#endregion
	
		#region MergeParagraphsInTheSameColumn()
		/// <summary>
		/// It merges 2 paragraphs when:
		/// 1) They are in line;
		/// 2) Their union rectangle width is equal or smaller than columnsWidth;
		/// 3) They are in the same virtual column;
		/// 4) They are in the same zone;
		/// </summary>
		/// <param name="columnsCount"></param>
		/// <param name="columnsWidth"></param>
		/// <param name="contentClip"></param>
		/// <param name="paragraphs"></param>
		private static Paragraphs MergeParagraphsInTheSameColumn(PageObjects.DelimiterZones delimiterZones, int columnsWidth, Paragraphs p)
		{
			Paragraphs paragraphs = p;
			
			for (int i = paragraphs.Count - 2; i >= 1; i--)
				for (int j = paragraphs.Count - 1; j > i; j--)
				{
					if (paragraphs[i].Zone == paragraphs[j].Zone)
					{
						PageObjects.DelimiterZone zone1 = delimiterZones.GetZone(paragraphs[i].Rectangle);
						PageObjects.DelimiterZone zone2 = delimiterZones.GetZone(paragraphs[j].Rectangle);

						if (zone1 != null && zone1 == zone2 && Arithmetic.AreInLine(paragraphs[i].Rectangle, paragraphs[j].Rectangle, 0.5))
						{
							if (Rectangle.Union(paragraphs[i].Rectangle, paragraphs[j].Rectangle).Width < columnsWidth * 1.1)
							{
								paragraphs[i].Merge(paragraphs[j]);
								paragraphs.RemoveAt(j);
							}
						}
					}
				}

			return paragraphs;
		}
		#endregion

		#region MergeObjectsInTheSameColumn()
		/// <summary>
		/// It merges 2 paragraphs when:
		/// 1) They are in line;
		/// 2) Their union rectangle width is equal or smaller than columnsWidth;
		/// 3) They are in the same virtual column;
		/// 4) They are in the same zone;
		/// </summary>
		/// <param name="columnsCount"></param>
		/// <param name="columnsWidth"></param>
		/// <param name="contentClip"></param>
		/// <param name="paragraphs"></param>
		private static bool MergeObjectsInTheSameColumn(PageObjects.DelimiterZones delimiterZones, int columnsWidth, PageObjects.PageObjects pageObjects)
		{
			Paragraphs p = pageObjects.Paragraphs;
			Symbols ls = pageObjects.LoneSymbols;
			Words w = pageObjects.Words;
			bool changed = false;

			// merge paragraphs next to each other in the same column
			for (int i = p.Count - 2; i >= 0; i--)
				for (int j = p.Count - 1; j > i; j--)
				{
					if (p[i].Zone == p[j].Zone)
					{
						PageObjects.DelimiterZone zone1 = delimiterZones.GetZone(p[i].Rectangle);
						PageObjects.DelimiterZone zone2 = delimiterZones.GetZone(p[j].Rectangle);

						if (zone1 != null && zone1 == zone2 && Arithmetic.AreInLine(p[i].Rectangle, p[j].Rectangle, 0.5))
						{
							if (Rectangle.Union(p[i].Rectangle, p[j].Rectangle).Width < columnsWidth * 1.1)
							{
								p[i].Merge(p[j]);
								p.RemoveAt(j);
								changed = true;
							}
						}
					}
				}

			//merge paragraph with symbols
			for (int i = p.Count - 1; i >= 0; i--)
				for (int j = ls.Count - 1; j >= 0; j--)
				{
					if (p[i].Zone == ls[j].Zone)
					{
						PageObjects.DelimiterZone zone1 = delimiterZones.GetZone(p[i].Rectangle);
						PageObjects.DelimiterZone zone2 = delimiterZones.GetZone(ls[j].Rectangle);

						if (zone1 != null && zone1 == zone2 && Arithmetic.AreInLine(p[i].Rectangle, ls[j].Rectangle, 0.5))
						{
							if (Rectangle.Union(p[i].Rectangle, ls[j].Rectangle).Width < columnsWidth * 1.1)
							{
								p[i].AddSymbol(ls[j]);
								ls.RemoveAt(j);
								changed = true;
							}
						}
					}
				}

			//merge paragraphs with unused words
			for (int i = p.Count - 1; i >= 0; i--)
				for (int j = w.Count - 1; j >= 0; j--)
				{
					if (w[j].Paragraph == null)
					{
						if (p[i].Zone == w[j].Zone)
						{
							PageObjects.DelimiterZone zone1 = delimiterZones.GetZone(p[i].Rectangle);
							PageObjects.DelimiterZone zone2 = delimiterZones.GetZone(w[j].Rectangle);

							if (zone1 != null && zone1 == zone2 && Arithmetic.AreInLine(p[i].Rectangle, w[j].Rectangle, 0.5))
							{
								if (Rectangle.Union(p[i].Rectangle, w[j].Rectangle).Width < columnsWidth * 1.1)
								{
									p[i].AddWord(w[j]);
									changed = true;
								}
							}
						}
					}
				}

#if SAVE_RESULTS
			p.DrawToFile(Debug.SaveToDir + @"302 paragraphs after merging.png", new Size(pageObjects.BitmapSize.Value.Width, pageObjects.BitmapSize.Value.Height));
#endif

			// reevaluate Patahraphs Lines
			if (changed)
			{
				foreach (Paragraph paragraph in p)
					paragraph.ReEvanuateLines(pageObjects);
			}

			p.ResetLines();

#if SAVE_RESULTS
			p.DrawToFile(Debug.SaveToDir + @"303 paragraphs after reevaluating.png", new Size(pageObjects.BitmapSize.Value.Width, pageObjects.BitmapSize.Value.Height));
#endif


			// merge adjacent paragraphs if distance between words is standard line distance
			p.SortVertically();

			int? averageSpacing = pageObjects.Lines.GetSpacing(pageObjects.Dpi);

			if (averageSpacing != null)
			{
				for (int i = p.Count - 2; i >= 0; i--)
					for (int j = i + 1; j < p.Count; j++)
					{
						//if more than inch apart, break
						if (p[j].Y > p[i].Bottom + pageObjects.Dpi)
							break;

						if (p[i].Zone == p[j].Zone)
						{
							PageObjects.DelimiterZone zone1 = delimiterZones.GetZone(p[i].Rectangle);
							PageObjects.DelimiterZone zone2 = delimiterZones.GetZone(p[j].Rectangle);

							if (zone1 != null && zone1 == zone2)
							{
								int horizontalPixelShare = Arithmetic.HorizontalPixelsShare(p[i].Rectangle, p[j].Rectangle);

								if (horizontalPixelShare > p[i].Width * 0.8 || horizontalPixelShare > p[j].Width * 0.8)
								{
									if (Rectangle.Union(p[i].Rectangle, p[j].Rectangle).Width < columnsWidth * 1.1)
									{
										int? spacing1 = p[i].Lines.GetSpacing(pageObjects.Dpi);
										int? spacing2 = p[j].Lines.GetSpacing(pageObjects.Dpi);
										int? spacing = Paragraphs.GetLinesSpacing(p[i], p[j]);

										if (spacing != null)
										{
											if (spacing1 != null && spacing2 != null)
											{
												if (spacing < spacing1 * 1.3 && spacing < spacing2 * 1.3)
												{
													p[i].Merge(p[j]);
													p.RemoveAt(j);
													changed = true;
													break;
												}
											}
											else if (spacing1 != null)
											{
												if (spacing < spacing1 * 1.3)
												{
													p[i].Merge(p[j]);
													p.RemoveAt(j);
													changed = true;
													break;
												}
											}
											else if (spacing2 != null)
											{
												if (spacing < spacing2 * 1.3)
												{
													p[i].Merge(p[j]);
													p.RemoveAt(j);
													changed = true;
													break;
												}
											}
											else if (spacing < averageSpacing * 1.3)
											{
												p[i].Merge(p[j]);
												p.RemoveAt(j);
												changed = true;
												break;
											}
										}
									}
								}

								// if overlaping, check symbols
								if (Rectangle.Union(p[i].Rectangle, p[j].Rectangle) != Rectangle.Empty)
								{
									if (ShouldMergeOverlappingParagraphs(p[i], p[j]))
									{
										p[i].Merge(p[j]);
										p.RemoveAt(j);
										changed = true;
										break;
									}
								}
							}
						}
					}
			}

			// merge adjacent paragraphs if distance between words is less than 1/2 of an inch
			p.SortVertically();

			for (int i = p.Count - 2; i >= 0; i--)
				for (int j = i + 1; j < p.Count; j++)
				{
					//if more than inch apart, break
					if (p[j].Y > p[i].Bottom + pageObjects.Dpi)
						break;

					if (p[i].Zone == p[j].Zone)
					{
						PageObjects.DelimiterZone zone1 = delimiterZones.GetZone(p[i].Rectangle);
						PageObjects.DelimiterZone zone2 = delimiterZones.GetZone(p[j].Rectangle);

						if (zone1 != null && zone1 == zone2)
						{
							int horizontalPixelShare = Arithmetic.HorizontalPixelsShare(p[i].Rectangle, p[j].Rectangle);

							if (horizontalPixelShare > p[i].Width * 0.8 || horizontalPixelShare > p[j].Width * 0.8)
							{
								if (Rectangle.Union(p[i].Rectangle, p[j].Rectangle).Width < columnsWidth * 1.1)
								{
									int verticalDistance = Arithmetic.Distance(p[i].Rectangle, p[j].Rectangle);

									if (verticalDistance < pageObjects.Dpi / 2)
									{
										p[i].Merge(p[j]);
										p.RemoveAt(j);
										changed = true;
										break;
									}
								}
							}
						}
					}
				}

			return changed;
		}

		/// <summary>
		/// It merges 2 paragraphs when:
		/// 1) They are in line;
		/// 2) Their union rectangle width is equal or smaller than columnsWidth;
		/// 3) They are in the same virtual column;
		/// 4) They are in the same zone;
		/// </summary>
		/// <param name="columnsCount"></param>
		/// <param name="columnsWidth"></param>
		/// <param name="contentClip"></param>
		/// <param name="paragraphs"></param>
		private static bool MergeObjectsInTheSameColumn(PageObjects.PageObjects pageObjects)
		{
			Paragraphs p = pageObjects.Paragraphs;
			Symbols ls = pageObjects.LoneSymbols;
			Words w = pageObjects.Words;
			bool changed = false;

			// merge paragraphs next to each other
			for (int i = p.Count - 2; i >= 0; i--)
				for (int j = p.Count - 1; j > i; j--)
				{
					if (p[i].Zone == p[j].Zone)
					{
						if (Arithmetic.AreInLine(p[i].Rectangle, p[j].Rectangle, 0.5))
						{
								p[i].Merge(p[j]);
								p.RemoveAt(j);
								changed = true;
						}
					}
				}

			//merge paragraph with symbols
			for (int i = p.Count - 1; i >= 0; i--)
				for (int j = ls.Count - 1; j >= 0; j--)
				{
					if (p[i].Zone == ls[j].Zone)
					{
						if (Arithmetic.AreInLine(p[i].Rectangle, ls[j].Rectangle, 0.5))
						{
							p[i].AddSymbol(ls[j]);
							ls.RemoveAt(j);
							changed = true;
						}
					}
				}

			//merge paragraphs with unused words
			for (int i = p.Count - 1; i >= 0; i--)
				for (int j = w.Count - 1; j >= 0; j--)
				{
					if (w[j].Paragraph == null)
					{
						if (p[i].Zone == w[j].Zone)
						{
							if (Arithmetic.AreInLine(p[i].Rectangle, w[j].Rectangle, 0.5))
							{
								p[i].AddWord(w[j]);
								changed = true;
							}
						}
					}
				}

#if SAVE_RESULTS
			p.DrawToFile(Debug.SaveToDir + @"302 paragraphs after merging.png", new Size(pageObjects.BitmapSize.Value.Width, pageObjects.BitmapSize.Value.Height));
#endif

			// reevaluate Patahraphs Lines
			if (changed)
			{
				foreach (Paragraph paragraph in p)
					paragraph.ReEvanuateLines(pageObjects);
			}

			p.ResetLines();

#if SAVE_RESULTS
			p.DrawToFile(Debug.SaveToDir + @"303 paragraphs after reevaluating.png", new Size(pageObjects.BitmapSize.Value.Width, pageObjects.BitmapSize.Value.Height));
#endif


			// merge adjacent paragraphs if distance between words is standard line distance
			p.SortVertically();

			int? averageSpacing = pageObjects.Lines.GetSpacing(pageObjects.Dpi);

			if (averageSpacing != null)
			{
				for (int i = p.Count - 2; i >= 0; i--)
					for (int j = i + 1; j < p.Count; j++)
					{
						//if more than inch apart, break
						if (p[j].Y > p[i].Bottom + pageObjects.Dpi)
							break;

						if (p[i].Zone == p[j].Zone)
						{
							int horizontalPixelShare = Arithmetic.HorizontalPixelsShare(p[i].Rectangle, p[j].Rectangle);

							if (horizontalPixelShare > p[i].Width * 0.8 || horizontalPixelShare > p[j].Width * 0.8)
							{
								int? spacing1 = p[i].Lines.GetSpacing(pageObjects.Dpi);
								int? spacing2 = p[j].Lines.GetSpacing(pageObjects.Dpi);
								int? spacing = Paragraphs.GetLinesSpacing(p[i], p[j]);

								if (spacing != null)
								{
									if (spacing1 != null && spacing2 != null)
									{
										if (spacing < spacing1 * 1.3 && spacing < spacing2 * 1.3)
										{
											p[i].Merge(p[j]);
											p.RemoveAt(j);
											changed = true;
											break;
										}
									}
									else if (spacing1 != null)
									{
										if (spacing < spacing1 * 1.3)
										{
											p[i].Merge(p[j]);
											p.RemoveAt(j);
											changed = true;
											break;
										}
									}
									else if (spacing2 != null)
									{
										if (spacing < spacing2 * 1.3)
										{
											p[i].Merge(p[j]);
											p.RemoveAt(j);
											changed = true;
											break;
										}
									}
									else if (spacing < averageSpacing * 1.3)
									{
										p[i].Merge(p[j]);
										p.RemoveAt(j);
										changed = true;
										break;
									}
								}
							}

							// if overlaping, check symbols
							if (Rectangle.Union(p[i].Rectangle, p[j].Rectangle) != Rectangle.Empty)
							{
								if (ShouldMergeOverlappingParagraphs(p[i], p[j]))
								{
									p[i].Merge(p[j]);
									p.RemoveAt(j);
									changed = true;
									break;
								}
							}
						}
					}
			}

			// merge adjacent paragraphs if distance between words is less than 1/2 of an inch
			p.SortVertically();

			for (int i = p.Count - 2; i >= 0; i--)
				for (int j = i + 1; j < p.Count; j++)
				{
					//if more than inch apart, break
					if (p[j].Y > p[i].Bottom + pageObjects.Dpi)
						break;

					if (p[i].Zone == p[j].Zone)
					{
						int horizontalPixelShare = Arithmetic.HorizontalPixelsShare(p[i].Rectangle, p[j].Rectangle);

						if (horizontalPixelShare > p[i].Width * 0.8 || horizontalPixelShare > p[j].Width * 0.8)
						{
							int verticalDistance = Arithmetic.Distance(p[i].Rectangle, p[j].Rectangle);

							if (verticalDistance < pageObjects.Dpi / 2)
							{
								p[i].Merge(p[j]);
								p.RemoveAt(j);
								changed = true;
								break;
							}
						}
					}
				}

			return changed;
		}
		#endregion

		#region GetLeftSideMedian()
		private static int GetLeftSideMedian(Paragraphs paragraphs)
		{
			List<int> leftList = new List<int>();

			foreach (Paragraph paragraph in paragraphs)
				leftList.Add(paragraph.X);

			leftList.Sort();

			return leftList[leftList.Count / 2];
		}
		#endregion

		#region GetRightSideMedian()
		private static int GetRightSideMedian(Paragraphs paragraphs)
		{
			List<int> rightList = new List<int>();

			foreach (Paragraph paragraph in paragraphs)
				rightList.Add(paragraph.Right);

			rightList.Sort();

			return rightList[rightList.Count / 2];
		}
		#endregion

		#region SignObjectsToTheColumns()
		/*private static void SignObjectsToTheColumns(PageObjects.PageObjects pageObjects, DelimiterZones delimiterZones)
		{
			List<IPageObject> columnPageObjects = new List<IPageObject>();

			foreach (Paragraph paragraph in pageObjects.Paragraphs)
			{
			}
		}*/
		#endregion

		#region GetRaster()
		private Bitmap GetRaster(ItDecoder itDecoder, ImageProcessing.ObjectsRecognition.DocumentContent documentContent)
		{
			Bitmap grayBitmap = null;
			Bitmap edgeBitmap = null;
			Bitmap mergedBitmap = null;

			try
			{
#if DEBUG
				DateTime total = DateTime.Now;
				DateTime start = DateTime.Now;
#endif
				Bitmap edBitmap = null;

				if (itDecoder.PixelsFormat == PixelsFormat.FormatBlackWhite)
				{
					edBitmap = new Bitmap(itDecoder.FilePath);
					Inverter.Invert(edBitmap);
				}
				else
				{
					grayBitmap = GetGrayBitmap(itDecoder);
					RaiseProgressChanged(0.20F);

#if DEBUG
					Console.WriteLine("GetRaster() 1: " + DateTime.Now.Subtract(start).ToString());
					start = DateTime.Now;
#endif

#if SAVE_RESULTS
					grayBitmap.Save(Debug.SaveToDir + "601 Grayscale.png", ImageFormat.Png);
#endif

					edgeBitmap = ImageProcessing.BigImages.ContentLocator.GetEdgeMap(grayBitmap);
					RaiseProgressChanged(0.25F);

#if DEBUG
					Console.WriteLine("GetRaster() 2: " + DateTime.Now.Subtract(start).ToString());
					start = DateTime.Now;
#endif
#if SAVE_RESULTS
					edgeBitmap.Save(Debug.SaveToDir + "602 Edges.png", ImageFormat.Png);
#endif

					ImageProcessing.BigImages.ContentLocator.SmoothEdges(edgeBitmap);
					RaiseProgressChanged(0.30F);

#if DEBUG
					Console.WriteLine("GetRaster() 3: " + DateTime.Now.Subtract(start).ToString());
					start = DateTime.Now;
#endif
#if SAVE_RESULTS
					edgeBitmap.Save(Debug.SaveToDir + "603 Edges smoothed.png", ImageFormat.Png);
#endif

					ImageProcessing.BigImages.ContentLocator.DespeckleEdges1x1(edgeBitmap);
					RaiseProgressChanged(0.35F);

#if DEBUG
					Console.WriteLine("GetRaster() 4: " + DateTime.Now.Subtract(start).ToString());
					start = DateTime.Now;
#endif
#if SAVE_RESULTS
					edgeBitmap.Save(Debug.SaveToDir + "604 edges despeckled.png", ImageFormat.Png);
#endif

					mergedBitmap = ImageProcessing.BigImages.ContentLocator.MergeAutoColorAndEdges(grayBitmap, edgeBitmap);
					RaiseProgressChanged(0.40F);

					grayBitmap.Dispose();
					grayBitmap = null;

					edgeBitmap.Dispose();
					edgeBitmap = null;

#if DEBUG
					Console.WriteLine("GetRaster() 7: " + DateTime.Now.Subtract(start).ToString());
					start = DateTime.Now;
#endif

#if SAVE_RESULTS
					mergedBitmap.Save(Debug.SaveToDir + "606 merged color and edges.png", ImageFormat.Png);
#endif

					DespeckleEdgeBitmap3x3(mergedBitmap);
					RaiseProgressChanged(0.50F);

#if SAVE_RESULTS
					mergedBitmap.Save(Debug.SaveToDir + "607 merged despeckled.png", ImageFormat.Png);
#endif

#if DEBUG
					Console.WriteLine("GetRaster() 8: " + DateTime.Now.Subtract(start).ToString());
					start = DateTime.Now;
#endif
					edBitmap = RotatingMask_Production(mergedBitmap);
#if DEBUG
					Console.WriteLine("GetRaster() 9: " + DateTime.Now.Subtract(start).ToString());
					start = DateTime.Now;
#endif

					mergedBitmap.Dispose();
					mergedBitmap = null;
				}

#if SAVE_RESULTS
				edBitmap.Save(Debug.SaveToDir + "608 Raster.png", ImageFormat.Png);
#endif

				RaiseProgressChanged(0.60F);

				ImageProcessing.NoiseReduction.Despeckle(edBitmap, NoiseReduction.DespeckleSize.Size4x4, ImageProcessing.NoiseReduction.DespeckleMode.WhiteSpecklesOnly, ImageProcessing.NoiseReduction.DespeckleMethod.Regions);

				RaiseProgressChanged(0.65F);

#if DEBUG
				Console.WriteLine("GetRaster() 10: " + DateTime.Now.Subtract(start).ToString());
				start = DateTime.Now;
#endif
#if SAVE_RESULTS
				edBitmap.Save(Debug.SaveToDir + "609 Raster despeckled.png", ImageFormat.Png);
#endif

				if (documentContent != null)
					documentContent.GetRidOfBorder(edBitmap);

#if DEBUG
				Console.WriteLine("GetRaster() 11: " + DateTime.Now.Subtract(start).ToString());
				start = DateTime.Now;
#endif
#if SAVE_RESULTS
				edBitmap.Save(Debug.SaveToDir + "610 Raster without borders.png", ImageFormat.Png);
#endif

#if DEBUG
				Console.WriteLine("GetRaster(): " + DateTime.Now.Subtract(total).ToString());
#endif

				return edBitmap;
			}
			finally
			{
				if (grayBitmap != null)
				{
					grayBitmap.Dispose();
					grayBitmap = null;
				}

				if (edgeBitmap != null)
				{
					edgeBitmap.Dispose();
					edgeBitmap = null;
				}

				if (mergedBitmap != null)
				{
					mergedBitmap.Dispose();
					mergedBitmap = null;
				}
			}
		}
		#endregion

		#region SplitSymbols()
		private void SplitSymbols(PageObjects.Symbols symbols, ImageProcessing.ObjectsRecognition.DocumentContent documentContent, out PageObjects.Symbols symbolsL, out PageObjects.Symbols symbolsR, Size imageSize)
		{
			double[] bookfoldArray = new double[imageSize.Height + 1];
			double x1 = documentContent.PointT.X * imageSize.Width;
			double y1 = documentContent.PointT.Y * imageSize.Height;
			double x2 = documentContent.PointB.X * imageSize.Width;
			double y2 = documentContent.PointB.Y * imageSize.Height;

			symbolsL = new ImageProcessing.PageObjects.Symbols();
			symbolsR = new ImageProcessing.PageObjects.Symbols();

			for (int y = 0; y < bookfoldArray.Length; y++)
				bookfoldArray[y] = x1 + (x2 - x1) * (y - y1) / (y2 - y1);

			foreach (Symbol symbol in symbols)
			{
				if (symbol.Right < bookfoldArray[symbol.Y] && symbol.Right < bookfoldArray[symbol.Bottom])
					symbolsL.Add(symbol);
				else if (symbol.X > bookfoldArray[symbol.Y] && symbol.X > bookfoldArray[symbol.Bottom])
					symbolsR.Add(symbol);
			}
		}
		#endregion

		#region ShouldMergeOverlappingParagraphs()
		private static bool ShouldMergeOverlappingParagraphs(Paragraph p1, Paragraph p2)
		{
			int fontSize = Math.Min(p1.FontSize, p2.FontSize);

			foreach (Word word in p1.Words)
				if (WordAdjacentExists(word, p2, fontSize))
					return true;
			
			foreach (Symbol symbol in p1.Symbols)
				if (SymbolAdjacentExists(symbol, p2, fontSize))
					return true;

			return false;
		}
		#endregion

		#region WordAdjacentExists()
		/// <summary>
		/// returns true if word not being in paragraph 'p' is supposed to be part of paragraph 'p'
		/// </summary>
		/// <param name="word"></param>
		/// <param name="p"></param>
		/// <param name="fontSize"></param>
		/// <returns></returns>
		private static bool WordAdjacentExists(Word word, Paragraph p, int fontSize)
		{
			// left word adjacent
			for (int i = p.Words.Count - 1; i >= 0; i--)
			{
				if (word.X > p.Words[i].Right)
				{
					Symbol	s1 = p.Words[i].LastLetter;
					Symbol	s2 = word.FirstLetter;

					if ((word.Zone == p.Words[i].Zone) && (s1 != null) && (s2 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							if ((s2.X - s1.Right) < (fontSize * 2.8))
								return true;
							else
								break;
						}
					}
				}
			}

			// right word adjacent
			for (int i = 0; i < p.Words.Count; i++)
			{
				if (word.Right < p.Words[i].X)
				{
					Symbol s1 = word.LastLetter;
					Symbol s2 = p.Words[i].FirstLetter;

					if ((word.Zone == p.Words[i].Zone) && (s1 != null) && (s2 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							if ((s2.X - s1.Right) < (fontSize * 2.8))
								return true;
							else
								break;
						}
					}
				}
			}

			// left symbol adjacent
			for (int i = p.Symbols.Count - 1; i >= 0; i--)
			{
				if (word.X > p.Symbols[i].Right)
				{
					Symbol s1 = p.Symbols[i];
					Symbol s2 = word.FirstLetter;

					if ((word.Zone == s2.Zone) && (s1 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							if ((s2.X - s1.Right) < (fontSize * 2.8))
								return true;
							else
								break;
						}
					}
				}
			}

			// right symbol adjacent
			for (int i = 0; i < p.Symbols.Count; i++)
			{
				if (word.Right < p.Symbols[i].X)
				{
					Symbol s1 = word.LastLetter;
					Symbol s2 = p.Symbols[i];

					if ((word.Zone == s2.Zone) && (s1 != null) && (s2 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							if ((s2.X - s1.Right) < (fontSize * 2.8))
								return true;
							else
								break;
						}
					}
				}
			}

			return false;
		}
		#endregion

		#region SymbolAdjacentExists()
		/// <summary>
		/// returns true if symbol not being in paragraph 'p' is supposed to be part of paragraph 'p'
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="p"></param>
		/// <param name="fontSize"></param>
		/// <returns></returns>
		private static bool SymbolAdjacentExists(Symbol symbol, Paragraph p, int fontSize)
		{
			// left word adjacent
			for (int i = p.Words.Count - 1; i >= 0; i--)
			{
				if (symbol.X > p.Words[i].Right)
				{
					Symbol s1 = p.Words[i].LastLetter;
					Symbol s2 = symbol;

					if ((symbol.Zone == p.Words[i].Zone) && (s1 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							if ((s2.X - s1.Right) < (fontSize * 2.8))
								return true;
							else
								break;
						}
					}
				}
			}

			// right word adjacent
			for (int i = 0; i < p.Words.Count; i++)
			{
				if (symbol.Right < p.Words[i].X)
				{
					Symbol s1 = symbol;
					Symbol s2 = p.Words[i].LastLetter;

					if ((symbol.Zone == p.Words[i].Zone) && (s2 != null))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							if ((s2.X - s1.Right) < (fontSize * 2.8))
								return true;
							else
								break;
						}
					}
				}
			}

			// left symbol adjacent
			for (int i = p.Symbols.Count - 1; i >= 0; i--)
			{
				if (symbol.X > p.Symbols[i].Right)
				{
					Symbol s1 = p.Symbols[i];
					Symbol s2 = symbol;

					if ((symbol.Zone == p.Symbols[i].Zone))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							if ((s2.X - s1.Right) < (fontSize * 2.8))
								return true;
							else
								break;
						}
					}
				}
			}

			// right symbol adjacent
			for (int i = 0; i < p.Symbols.Count; i++ )
			{
				if (symbol.Right < p.Symbols[i].X)
				{
					Symbol s1 = symbol;
					Symbol s2 = p.Symbols[i];

					if ((symbol.Zone == p.Symbols[i].Zone))
					{
						if (Arithmetic.AreInLine(s1.Rectangle, s2.Rectangle, 0.5))
						{
							if ((s2.X - s1.Right) < (fontSize * 2.8))
								return true;
							else
								break;
						}
					}
				}
			}

			return false;
		}
		#endregion

		#region DespeckleEdgeBitmap3x3()
		/// <summary>
		/// Pixel value is smaller from current value and second bigest value in 3x3 neighbourhoud.
		/// </summary>
		/// <param name="source"></param>
		internal static void DespeckleEdgeBitmap3x3(Bitmap source)
		{
			BitmapData sourceData = null;

			try
			{
				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

				int stride = sourceData.Stride;
				int height = source.Height;
				int width = source.Width;
				int x, y;

				byte[] l1 = new byte[width];
				byte[] l2 = new byte[width];

				unsafe
				{
					byte* pSource = (byte*)sourceData.Scan0.ToPointer();
					byte* p1, p2, p3;
					byte max1, max2, g;

					for (x = 0; x < width; x++)
						l1[x] = pSource[x];

					for (y = 1; y < height - 1; y++)
					{
						p1 = pSource + (y - 1) * stride;
						p2 = pSource + (y) * stride;
						p3 = pSource + (y + 1) * stride;
						
						for (x = 1; x < width - 1; x++)
						{
							if (pSource[y * stride + x] >= 10)
							{
								if (p1[x - 1] >= p1[x])
								{
									max1 = p1[x - 1];
									max2 = p1[x];
								}
								else
								{
									max1 = p1[x];
									max2 = p1[x - 1];
								}

								g = p1[x + 1];
								if (max2 < g)
								{
									if (max1 <= g)
									{
										max2 = max1;
										max1 = g;
									}
									else
										max2 = g;
								}

								if ((max2 < p2[x - 1]))
								{
									if (max1 <= p2[x - 1])
									{
										max2 = max1;
										max1 = p2[x - 1];
									}
									else
										max2 = p2[x - 1];
								}

								if (max2 < p2[x + 1])
								{
									if (max1 <= p2[x + 1])
									{
										max2 = max1;
										max1 = p2[x + 1];
									}
									else
										max2 = p2[x + 1];
								}

								//g = p3[x - 1];
								if (max2 < p3[x - 1])
								{
									if (max1 <= p3[x - 1])
									{
										max2 = max1;
										max1 = p3[x - 1];
									}
									else
										max2 = p3[x - 1];
								}

								//g = p3[x];
								if (max2 < p3[x])
								{
									if (max1 <= p3[x])
									{
										max2 = max1;
										max1 = p3[x];
									}
									else
										max2 = p3[x];
								}

								//g = p3[x + 1];
								if (max2 < p3[x + 1])
								{
									if (max1 <= p3[x + 1])
									{
										max2 = max1;
										max1 = p3[x + 1];
									}
									else
										max2 = p3[x + 1];
								}

								if (pSource[y * stride + x] > max2)
									l2[x] = max2;
								else
									l2[x] = pSource[y * stride + x];
							}
							else
								l2[x] = pSource[y * stride + x];
						}

						for (x = 0; x < width; x++)
						{
							pSource[(y - 1) * stride + x] = l1[x];
							l1[x] = l2[x];
						}
					}
				}
			}
			finally
			{
				if (source != null && sourceData != null)
					source.UnlockBits(sourceData);
			}
		}
		#endregion

		#region RotatingMask_Production()
		private static unsafe Bitmap RotatingMask_Production(Bitmap bitmap)
		{
			byte objectThreshold = 235;
			byte backThreshold = 150;
			Rectangle clip = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

			Bitmap result = null;
			BitmapData bitmapData = null;
			BitmapData resultData = null;

			try
			{
				bitmapData = bitmap.LockBits(clip, ImageLockMode.ReadWrite, bitmap.PixelFormat);
				result = new Bitmap(bitmapData.Width, bitmapData.Height, PixelFormat.Format1bppIndexed);
				resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

				int widthMinus1 = bitmapData.Width - 1;
				int heightMinus1 = bitmapData.Height - 1;
				int width = resultData.Width;
				int height = resultData.Height;

				int strideS = bitmapData.Stride;
				int strideR = resultData.Stride;
				int threshold = 8;
				int x, y;

				byte* pOrig = (byte*)bitmapData.Scan0.ToPointer();
				byte* pResult = (byte*)resultData.Scan0.ToPointer();

				for (y = 1; y < heightMinus1; y++)
					for (x = 1; x < widthMinus1; x++)
					{
						if (*(pOrig + y * strideS + x) > objectThreshold)
							pResult[y * strideR + x / 8] |= (byte)(0x80 >> (x & 0x07));
						else if ((*(pOrig + y * strideS + x) > backThreshold) && Jirka8bppEdge(pOrig + y * strideS + x, strideS, threshold))
							pResult[y * strideR + x / 8] |= (byte)(0x80 >> (x & 0x07));
					}

				//top line is identical to second line
				for (x = 0; x < strideR; x++)
					pResult[x] = pResult[strideR + x];
				//bottom line is identical to second bottom line
				for (x = 0; x < strideR; x++)
					pResult[(height - 1) * strideR + x] = pResult[(height - 2) * strideR + x];
				//left columnh is identical to second column
				for (y = 0; y < height; y++)
					pResult[y * strideR] |= (byte)((pResult[y * strideR] & 0x40) << 1);
				//right columnh is identical to second right column
				if (((width - 1) % 8) == 0)
					for (y = 0; y < height; y++)
						pResult[y * strideR + (width - 1) / 8] |= (byte)((pResult[y * strideR + (width - 2) / 8] & 0x01) << 7);
				else
					for (y = 0; y < height; y++)
						pResult[y * strideR + (width - 1) / 8] |= (byte)((pResult[y * strideR + (width - 2) / 8] & (0x80 >> ((width - 2) & 0x07))) >> 1);

				if (bitmap.HorizontalResolution > 0 && bitmap.VerticalResolution > 0)
					result.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

				return result;
			}
			finally
			{
				if (bitmapData != null)
					bitmap.UnlockBits(bitmapData);
				if (resultData != null)
					result.UnlockBits(resultData);
			}
		}
		#endregion

		#region Jirka8bppEdge()
		private static unsafe bool Jirka8bppEdge(byte* scan0, int stride, int threshold)
		{
			// to keep gap between letters
			if ((scan0[-stride - 1] + scan0[-1] + scan0[stride - 1] > scan0[-stride] - 2 * scan0[0] - scan0[stride]) &&
				(scan0[-stride + 1] + scan0[+1] + scan0[stride + 1] > scan0[-stride] - 2 * scan0[0] - scan0[stride]))
				return false;
			
			/*
			if (scan0[-stride - 1] + 2 * scan0[-stride] + scan0[-stride + 1] - scan0[stride - 1] - 2 * scan0[stride] - scan0[stride + 1] > threshold)
				return true;

			if ((scan0[-stride - 1] + 2 * scan0[-stride] + scan0[-stride + 1] - scan0[stride - 1] - 2 * scan0[stride] - scan0[stride + 1]) < -threshold)
				return true;

			if (scan0[-stride - 1] + 2 * scan0[-1] + scan0[stride - 1] - scan0[-stride + 1] - 2 * scan0[1] - scan0[stride + 1] > threshold)
				return true;

			if (scan0[-stride - 1] + 2 * scan0[-1] + scan0[stride - 1] - scan0[-stride + 1] - 2 * scan0[1] - scan0[stride + 1] < -threshold)
				return true;

			if (scan0[-stride] + 2 * scan0[-stride + 1] + scan0[1] - scan0[-1] - 2 * scan0[+stride - 1] - scan0[-stride] > threshold)
				return true;

			if (scan0[-stride] + 2 * scan0[-stride + 1] + scan0[1] - scan0[-1] - 2 * scan0[+stride - 1] - scan0[-stride] < -threshold)
				return true;

			if (scan0[-1] + 2 * scan0[-stride - 1] + scan0[-stride] - scan0[stride] - 2 * scan0[+stride + 1] - scan0[1] > threshold)
				return true;

			if (scan0[-1] + 2 * scan0[-stride - 1] + scan0[-stride] - scan0[stride] - 2 * scan0[+stride + 1] - scan0[1] < -threshold)
				return true;

			return false;
			 */
			
			return (((((((scan0[-stride - 1] + (2 * scan0[-stride])) + scan0[-stride + 1]) - scan0[stride - 1]) - (2 * scan0[stride])) - scan0[stride + 1]) > threshold) || (((((((scan0[-stride - 1] + (2 * scan0[-stride])) + scan0[-stride + 1]) - scan0[stride - 1]) - (2 * scan0[stride])) - scan0[stride + 1]) < -threshold) || (((((((scan0[-stride - 1] + (2 * scan0[-1])) + scan0[stride - 1]) - scan0[-stride + 1]) - (2 * scan0[1])) - scan0[stride + 1]) > threshold) || (((((((scan0[-stride - 1] + (2 * scan0[-1])) + scan0[stride - 1]) - scan0[-stride + 1]) - (2 * scan0[1])) - scan0[stride + 1]) < -threshold) || (((((((scan0[-stride] + (2 * scan0[-stride + 1])) + scan0[1]) - scan0[-1]) - (2 * scan0[stride - 1])) - scan0[-stride]) > threshold) || (((((((scan0[-stride] + (2 * scan0[-stride + 1])) + scan0[1]) - scan0[-1]) - (2 * scan0[stride - 1])) - scan0[-stride]) < -threshold) || (((((((scan0[-1] + (2 * scan0[-stride - 1])) + scan0[-stride]) - scan0[stride]) - (2 * scan0[stride + 1])) - scan0[1]) > threshold) || ((((((scan0[-1] + (2 * scan0[-stride - 1])) + scan0[-stride]) - scan0[stride]) - (2 * scan0[stride + 1])) - scan0[1]) < -threshold))))))));
			
			/*return ( 
				// N direction
				(scan0[-1] + 2 * scan0[0] + scan0[1] - scan0[-stride - 1] - 2 * scan0[-stride] - scan0[-stride + 1] > threshold) || 
				// S direction
				(scan0[-1] + 2 * scan0[0] + scan0[1] - scan0[+stride - 1] - 2 * scan0[+stride] - scan0[+stride + 1] > threshold) ||
				// W direction
				(scan0[-stride] + 2 * scan0[0] + scan0[stride] - scan0[-stride - 1] - 2 * scan0[-1] - scan0[stride - 1] > threshold) || 
				// E direction
				(scan0[-stride] + 2 * scan0[0] + scan0[stride] - scan0[-stride + 1] - 2 * scan0[1] - scan0[stride + 1]  > threshold) || 
				// NW direction
				(scan0[stride - 1] + 2 * scan0[0] + scan0[-stride + 1] - scan0[-1] - 2 * scan0[-stride - 1] - scan0[-stride] > threshold) || 
				// NE direction
				(scan0[-stride - 1] + 2 * scan0[0] + scan0[stride + 1] - scan0[-stride] - 2 * scan0[-stride + 1] - scan0[+1] > threshold) || 
				// SW direction
				(scan0[-stride - 1] + 2 * scan0[0] + scan0[stride + 1] - scan0[-1] - 2 * scan0[-stride - 1] - scan0[-stride] > threshold) || 
				// SE direction
				(scan0[stride - 1] + 2 * scan0[0] + scan0[-stride + 1] - scan0[stride] - 2 * scan0[stride + 1] - scan0[1] > threshold)
				);*/
		}
		#endregion

		#region RaiseProgressChanged()
		private void RaiseProgressChanged(float progress)
		{
			if (ProgressChanged != null)
				ProgressChanged(progress);
		}
		#endregion

		#endregion
	}
}
