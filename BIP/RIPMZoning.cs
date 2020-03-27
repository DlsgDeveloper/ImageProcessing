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


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for RIPMZoning.
	/// </summary>
	public class RIPMZoning
	{
		#region constructor
		private RIPMZoning()
		{
		}
		#endregion

		//PUBLIC METHODS
		#region GetZones()
		public static Zones GetZones(FileInfo image, Rectangle clip)
		{
			image.Refresh();
			if (image.Exists == false)
				throw new IOException(BIPStrings.ImagePathTo_STR + image.FullName + BIPStrings.IsIncorrect_STR);

			Bitmap bitmap = new Bitmap(image.FullName);
			Zones zones = GetZones(bitmap, clip);
			bitmap.Dispose();

			return zones;
		}

		public static Zones GetZones(Bitmap bitmap, Rectangle clip)
		{
			ItImage itImage = new ItImage(bitmap, false);
			
			itImage.CreatePageObjects(clip);
			int? columnsWidth = GetColumnsWidth(itImage);

			if (columnsWidth.HasValue && columnsWidth >= 1)
			{
				DelimiterZones columns = GetColumns(itImage, columnsWidth.Value);

				if (columns != null)
					MergeParagraphsInTheSameColumn(columns, columnsWidth.Value, itImage.Paragraphs);

				SignObjectsToTheColumns(itImage, columns);
			}

			//itImage.Paragraphs.DrawToFile(Debug.SaveToDir + @"Paragraphs.png", bitmap.Size);
			Zones zones = GetZones(itImage, bitmap.Size);
			//zones.DrawToFile(Debug.SaveToDir + @"Zones.png", bitmap);
			zones.Sort(itImage.Delimiters, bitmap.Size);
			//zones.DrawToFile(Debug.SaveToDir + @"Zones.png", bitmap);
			zones.Inflate(10, bitmap.Size);
			
#if SAVE_RESULTS
			zones.DrawToFile(Debug.SaveToDir + @"Zones.png", bitmap);
#endif		
	
			return zones;
		}
		#endregion

		#region GetZones() old
		/*public static Zones GetZones(Bitmap bitmap, Rectangle clip, int columnsCount)
		{
			if (clip.IsEmpty)
				clip = new Rectangle(Point.Empty, bitmap.Size);

			Bitmap raster = Rasterize(bitmap, clip);

			//raster.Save(Debug.SaveToDir + @"raster.png", ImageFormat.Png);

			Size imageSize = clip.Size;
			int resolution = Convert.ToInt32(raster.HorizontalResolution);
			PageObjects.Symbols objects = PageObjects.ObjectLocator.FindObjects(raster, new Rectangle(0, 0, raster.Width, raster.Height), 1);
			PageObjects.Symbols allSymbols = objects;


			Pictures pictures = new Pictures(objects);
#if SAVE_RESULTS
			pictures.DrawToFile(Debug.SaveToDir + @"08 Pictures.png", imageSize);
#endif

			PageObjects.Delimiters delimiters = GetDelimiters(objects, raster);


#if SAVE_RESULTS
			delimiters.DrawToFile(Debug.SaveToDir + @"03 Delimiters before merge.png", imageSize);
#endif
			delimiters.Merge();
			delimiters.Validate(objects, Math.PI / 180.0 * 5.0);

			PageObjects.Words words = new Words(objects, raster);

			raster.Dispose();
#if SAVE_RESULTS
			words.DrawToFile(Debug.SaveToDir + @"Words.png", imageSize);
#endif

			DelimiterZones delimiterZones = delimiters.GetZones(objects, words, pictures, bitmap.Size);


#if SAVE_RESULTS
			objects.DrawToFile(Debug.SaveToDir + @"02 Symbols.png", imageSize);
			delimiters.DrawToFile(Debug.SaveToDir + @"03 Delimiters.png", imageSize);
			delimiterZones.DrawToFile(Debug.SaveToDir + @"04 Delimiter Zones.png", imageSize);
#endif

			SignDelimiterZones(words, objects, pictures, delimiterZones);
			Lines lines = new PageObjects.Lines(words, objects);
			PageObjects.Paragraphs paragraphs = PageObjects.ObjectLocator.FindParagraphs(objects, words, lines);

#if SAVE_RESULTS
			paragraphs.DrawToFile(Debug.SaveToDir + @"06 Paragraphs.png", imageSize);
#endif

			//PageObjects.ObjectLocator.AddLinesToParagraphs(paragraphs, lines);
			paragraphs.InsertNestedWords(words);

#if SAVE_RESULTS
			paragraphs.DrawToFile(Debug.SaveToDir + @"10 Line Paragraphs.png", imageSize);
#endif

			//ValidateParagraphs(ref paragraphs, resolution, delimiterZones);

			if (columnsCount > 1)
			{
				PageObjects.DelimiterZones columns = GetColumns(delimiters, words, allSymbols, columnsCount);

				if (columns != null)
				{
					MergeParagraphsInTheSameColumn(columns, paragraphs);
				}
			}

			Zones zones = GetZones(paragraphs, pictures, imageSize);
			zones.Sort(delimiters, imageSize);

#if SAVE_RESULTS
			zones.DrawToFile(Debug.SaveToDir + @"11 Zones.png", imageSize);
			objects.DrawToFile(Debug.SaveToDir + @"02 Symbols 2.png", imageSize);
#endif


			return zones;
		}*/
		#endregion

		//PRIVATE METHODS
		#region private methods

		#region GetZones()
		private static Zones GetZones(ItImage itImage, Size imageSize)
		{
			Zones	zones = new Zones();

			foreach (PageObjects.Paragraph paragraph in itImage.Paragraphs)
			{
				if (paragraph.Rectangle.Width > 50)
				{
					Rectangle r = paragraph.Rectangle;
					/*r.Inflate(10, 10);

					r.X = (r.X > 0) ? r.X : 0;
					r.Y = (r.Y > 0) ? r.Y : 0;
					r.Width = (r.Right < imageSize.Width) ? r.Width : imageSize.Width - r.X;
					r.Height = (r.Bottom < imageSize.Height) ? r.Height : imageSize.Height - r.Y;*/

					zones.Add(new Zone(paragraph, r));
				}
			}

			foreach (Picture picture in itImage.Pictures)
			{
				if (picture.ObjectShape.MaxPixelWidth > 50 && picture.ObjectShape.MaxPixelHeight > 50)
				{
					Rectangle r = picture.Rectangle;
					/*r.Inflate(10, 10);

					r.X = (r.X > 0) ? r.X : 0;
					r.Y = (r.Y > 0) ? r.Y : 0;
					r.Width = (r.Right < imageSize.Width) ? r.Width : imageSize.Width - r.X;
					r.Height = (r.Bottom < imageSize.Height) ? r.Height : imageSize.Height - r.Y;*/

					zones.Add(new Zone(picture, r));
				}
			}

			zones.MergeZones(itImage.Page.ColumnsWidth);

			for (int i = zones.Count - 1; i >= 0; i--)
				if (zones[i].Width < 40)
					zones.RemoveAt(i);

			return zones;
		}
		#endregion

		#region GetDelimiters()
		private static PageObjects.Delimiters GetDelimiters(PageObjects.Symbols symbols, Bitmap raster)
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
		}
		#endregion

		#region GetPictureDelimiters()
		private unsafe static PageObjects.Delimiters GetPictureDelimiters(PageObjects.Symbol symbol, BitmapData bmpData)
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
		}
		#endregion

		#region GetCrop()
		private static unsafe Crop GetCrop(PageObjects.Symbol symbol, int[,] array)
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
		}
		#endregion

		#region IsObjectHollow()
		private static unsafe bool IsObjectHollow(PageObjects.Symbol symbol, byte* scan0, int stride)
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
		}
		#endregion

		#region FindCorners()
		private static ObjectByCorners FindCorners(PageObjects.Symbol symbol, int[,] array, Crop crop)
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
		}
		#endregion

		#region GetObjectShape()
		/// <summary>
		/// Returns array of integers of the object on image where 0 - background, -1 object, <1, ...> other objects.
		/// </summary>
		/// <param name="symbol">Continuous image object.</param>
		/// <param name="scan0">Pointer to the upper left corner of entire image.</param>
		/// <param name="stride">Number of bytes of 1 image line.</param>
		/// <returns>Returns array of integers of the object on image where 0 - background, -1 object, <1, ...> other objects.</returns>
		private static unsafe int[,] GetObjectShape(PageObjects.Symbol symbol, byte* scan0, int stride)
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
		}
		#endregion

		#region GetWords()
		private static void GetWords(Words words, Delimiter delimiter, out Words words1, out Words words2, out Words notUsedWords)
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
		}
		#endregion

		#region GetColumns()
		/// <summary>
		/// First, it counts columns width. Then, counting only paragraphs with right width, it assigns those paragraphs to one of the 
		/// virtual columns. Then, based on those lists, it computes average left and right side of each column.
		/// </summary>
		/// <param name="itImage"></param>
		private static PageObjects.DelimiterZones GetColumns(ItImage itImage, int columnsWidth)
		{
			Operations	operations = new Operations(true, 0.2F, false, false, false);
			float		confidence = itImage.Find(operations);

			if (confidence > 0)
			{
				Rectangle	contentClip = itImage.Page.Clip.RectangleNotSkewed;
				int			columnsCount = Convert.ToInt32(contentClip.Width / (double)columnsWidth);

				if (columnsCount > 1 && columnsCount <= 4)
				{
					Paragraphs paragraphs = itImage.Paragraphs;

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

						PageObjects.DelimiterZones columns = new DelimiterZones();

						foreach (Paragraphs paragraphsColumn in paragraphsColumns)
						{
							if (paragraphsColumn.Count > 0)
							{
								int left = GetLeftSideMedian(paragraphsColumn);
								int right = GetRightSideMedian(paragraphsColumn);
								Rectangle zone = Rectangle.FromLTRB(left, contentClip.Top, right, contentClip.Bottom);

								zone.Inflate(50, 0);
								columns.Add(new DelimiterZone(Rectangle.Intersect(zone, contentClip)));
							}
						}

						return columns;
					}
				}
			}

			return null;
		}
		#endregion

		#region GetColumns()
		/*private static PageObjects.DelimiterZones GetColumns(PageObjects.Delimiters delimiters, PageObjects.Words words, PageObjects.Symbols symbols, int columnsCount)
		{
			List<PageObjects.Delimiter> delimiterCandidates = new List<Delimiter>();
			Rectangle clip = symbols.GetClip();

			foreach (PageObjects.Delimiter delimiter in delimiters)
				if (delimiter.IsVertical && delimiter.Height > clip.Height / 2)
					delimiterCandidates.Add(delimiter);

			//delete left and right delimiters
			for (int i = delimiterCandidates.Count - 1; i >= 1; i--)
			{
				if (delimiterCandidates[i].X + delimiterCandidates[i].Width / 2 > clip.X + clip.Width / (columnsCount * 2))
					delimiterCandidates.RemoveAt(i);
				else if (delimiterCandidates[i].X + delimiterCandidates[i].Width / 2 < clip.Right - clip.Width / (columnsCount * 2))
					delimiterCandidates.RemoveAt(i);
			}

			//eliminate delimiters that are close to each other
			if (delimiterCandidates.Count > 0 && delimiterCandidates.Count == columnsCount - 1)
			{
				PageObjects.DelimiterZones columns = new DelimiterZones();
				Point ul = new Point(clip.X, delimiterCandidates[0].Y);
				Point ll = new Point(clip.X, delimiterCandidates[0].Bottom);
				Point ur, lr;

				for (int i = 0; i < delimiterCandidates.Count; i++)
				{
					ur = delimiterCandidates[i].TopPoint;
					lr = delimiterCandidates[i].BottomPoint;

					columns.Add(new PageObjects.DelimiterZone(ul, ur, ll, lr));

					ul = ur;
					ll = lr;
				}

				ur = new Point(clip.Right, delimiterCandidates[delimiterCandidates.Count - 1].Y);
				lr = new Point(clip.Bottom, delimiterCandidates[delimiterCandidates.Count - 1].Bottom);

				columns.Add(new PageObjects.DelimiterZone(ul, ur, ll, lr));

				return columns;
			}

			return null;
		}*/
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
		private static void MergeParagraphsInTheSameColumn(PageObjects.DelimiterZones columns, int columnsWidth, Paragraphs paragraphs)
		{
			for (int i = paragraphs.Count - 2; i >= 1; i--)
				for (int j = paragraphs.Count - 1; j > i; j--)
				{
					if (paragraphs[i].Zone == paragraphs[j].Zone)
					{
						PageObjects.DelimiterZone column1 = columns.GetZone(paragraphs[i].Rectangle);
						PageObjects.DelimiterZone column2 = columns.GetZone(paragraphs[j].Rectangle);

						if (column1 != null && column1 == column2 && Arithmetic.AreInLine(paragraphs[i].Rectangle, paragraphs[j].Rectangle, 0.5))
						{
							if (Rectangle.Union(paragraphs[i].Rectangle, paragraphs[j].Rectangle).Width < columnsWidth * 1.1)
							{
								paragraphs[i].Merge(paragraphs[j]);
								paragraphs.RemoveAt(j);
							}
						}
					}
				}
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

		#region GetColumnsWidth()
		private static int? GetColumnsWidth(ItImage itImage)
		{
			List<Paragraph> columnParagraphs = new List<Paragraph>();
			int square = 0;
			int sum = 0;
			
			foreach (Paragraph paragraph in itImage.Paragraphs)
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

		#region SignObjectsToTheColumns()
		private static void SignObjectsToTheColumns(ItImage itImage, DelimiterZones columns)
		{
			List<IPageObject> columnPageObjects = new List<IPageObject>();

			foreach (Paragraph paragraph in itImage.Paragraphs)
			{
			}
		}
		#endregion

		#endregion
	}
}
