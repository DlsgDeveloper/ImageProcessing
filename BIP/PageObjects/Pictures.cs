using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace ImageProcessing.PageObjects
{
	public class Pictures : List<Picture>
	{
		#region constructor
		public Pictures()
		{
		}

		public Pictures(Symbols symbols)
		{
#if SAVE_RESULTS
			Rectangle rect = symbols.GetClip();
			Size size = new Size(rect.Right, rect.Bottom);
#endif

#if DEBUG
			DateTime start = DateTime.Now;
#endif

			symbols.SortHorizontally();

			//create pictures
			for (int i = symbols.Count - 1; i >= 0; i--)
				if (symbols[i].IsPicture)
				{
					Picture picture = this.GetPicture(symbols[i]);

					if (picture != null)
						picture.AddSymbol(symbols[i]);
					else
						base.Add(new Picture(symbols[i]));

					symbols.RemoveAt(i);
				}
			
			MergeWithSymbols(symbols);

#if DEBUG
			Console.WriteLine(string.Format("Pictures: {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), base.Count));
#endif

#if SAVE_RESULTS
			DrawToFile(Debug.SaveToDir + "Pictures.png", new Size(rect.Right, rect.Bottom));
			DrawObjectShapes(Debug.SaveToDir + "PictureShapes.png", size);
			DrawConvexEnvelopes(Debug.SaveToDir + "PictureConvexEnvelopes.png", size);
#endif
		}
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region ComputeShapes()
		/*public void ComputeShapes(Bitmap bitmap)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			BitmapData bmpData = null;

			try
			{
				bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				
				foreach (Picture picture in this)
					picture.ComputeShape(bmpData);
			}
			finally
			{
				if (bmpData != null)
					bitmap.UnlockBits(bmpData);
			}

#if DEBUG
			System.Reflection.MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
			Console.WriteLine(method.DeclaringType.Name + ", " + method.Name + "(): " + DateTime.Now.Subtract(start).ToString());
#endif
		}*/
		#endregion

		#region GetPicturesInClip()
		public Pictures GetPicturesInClip(Rectangle clip)
		{
			Pictures pictures = new Pictures();

			foreach (Picture picture in this)
			{
				Rectangle intersection = Rectangle.Intersect(clip, picture.Rectangle);

				if ((intersection.Width * intersection.Height) > ((picture.Width * picture.Height) / 2))
					pictures.Add(picture);
			}

			return pictures;
		}
		#endregion

		#region GetSkew()
		public bool GetSkew(Size pageSize, out double angle, out double weight)
		{
			double angleSum = 0.0;
			double weightSum = 0.0;
			int validAngleCount = 0;
			
			foreach (Picture picture in this)
			{
				double a;
				double w;

				if (picture.GetSkew(pageSize, out a, out w))
				{
					angleSum += a * w;
					weightSum += w;
					validAngleCount++;
				}
			}

			if (weightSum > 0.0)
			{
				angle = angleSum / weightSum;
				weight = weightSum;
			}
			else
			{
				angle = 0.0;
				weight = 0.0;
			}

			return (validAngleCount > 0);
		}
		#endregion

		#region Merge()
		/// <summary>
		/// This method assumes that pictures are already merged.
		/// </summary>
		/// <param name="symbols"></param>
		public void Merge(Delimiters delimiters, Symbols symbols)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			int picturesCount, delimitersCount, symbolsCount;

			do
			{
				picturesCount = this.Count;
				delimitersCount = delimiters.Count;
				symbolsCount = symbols.Count;

				//merge with delimiters
				for (int j = this.Count - 1; j >= 0; j--)
				{
					bool recompute = false;

					for (int i = delimiters.Count - 1; i >= 0; i--)
						if (this[j].InterceptsWith(delimiters[i]))
						{
							this[j].AddObjectMapFast(delimiters[i].ObjectMap);
							delimiters.RemoveAt(i);
							recompute = true;
						}

					if (recompute)
						this[j].ComputeShape();
				}

				//merge with symbols
				for (int j = this.Count - 1; j >= 0; j--)
				{
					bool recompute = false;

					for (int i = symbols.Count - 1; i >= 0; i--)
						if (this[j].InterceptsWith(symbols[i]))
						{
							this[j].AddSymbolFast(symbols[i]);
							symbols.RemoveAt(i);
							recompute = true;
						}

					if (recompute)
						this[j].ComputeShape();
				}

				if ((delimitersCount != delimiters.Count) || (symbolsCount != symbols.Count))
				{
					MergePictures();
				}
			} while (picturesCount != this.Count || delimitersCount != delimiters.Count || symbolsCount != symbols.Count);

#if DEBUG
			Console.WriteLine(string.Format("Pictures, MergeWithDelimiters(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), base.Count));
#endif
		}
		#endregion

		#region DrawObjectShapes()
		public void DrawObjectShapes(string filePath, Size imageSize)
		{
#if SAVE_RESULTS
			Bitmap result = null;
			BitmapData bmpData = null;

			try
			{
				result = Debug.GetBitmap(imageSize);

				bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				int counter = 0;
				foreach (Picture picture in this)
				{
					Color color = Debug.GetColor(counter++);

					picture.ObjectShape.DrawToImage(color, bmpData);
				}
			}
			catch { }
			finally
			{
				if (bmpData != null)
				{
					result.UnlockBits(bmpData);
					result.Save(filePath, ImageFormat.Png);
					result.Dispose();
				}
			}
#endif
		}
		#endregion

		#region DrawConvexEnvelopes()
		public void DrawConvexEnvelopes(string filePath, Size imageSize)
		{
#if SAVE_RESULTS
			Bitmap result = null;
			BitmapData bmpData = null;

			try
			{
				result = Debug.GetBitmap(imageSize);

				bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				int counter = 0;
				foreach (Picture picture in this)
				{
					Color color = Debug.GetColor(counter++);

					picture.ConvexEnvelope.DrawToImage(color, bmpData);
				}
			}
			catch { }
			finally
			{
				if (bmpData != null)
				{
					result.UnlockBits(bmpData);
					result.Save(filePath, ImageFormat.Png);
					result.Dispose();
				}
			}
#endif
		}
		#endregion

		#region DrawToFile()
		public void DrawToFile(string filePath, Size imageSize)
		{
#if SAVE_RESULTS
			Bitmap result = null;
			BitmapData bmpData = null;

			try
			{
				result = Debug.GetBitmap(imageSize);
				bmpData = null;

				bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				Color color = Color.Red;
				Brush brush = new SolidBrush(Color.FromArgb(100, color));

				foreach (Picture picture in this)
					picture.DrawToImage(color, bmpData);
			}
			catch { }
			finally
			{
				if (bmpData != null)
				{
					result.UnlockBits(bmpData);
					result.Save(filePath, ImageFormat.Png);
					result.Dispose();
				}

				GC.Collect();
			}
#endif
		}
		#endregion

		#region DrawToImage()
		public void DrawToImage(Bitmap result)
		{
#if SAVE_RESULTS
			BitmapData bmpData = null;

			try
			{
				bmpData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);
				
				foreach (Picture picture in this)
					picture.DrawToImage(Color.Red, bmpData);
			}
			catch { }
			finally
			{
				if (bmpData != null)
					result.UnlockBits(bmpData);

				GC.Collect();
			}
#endif
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region MergeWithSymbols()
		/// <summary>
		/// This method assumes that pictures are already merged.
		/// </summary>
		/// <param name="symbols"></param>
		private void MergeWithSymbols(Symbols symbols)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			int picturesCount, symbolsCount;

			//merge owerlapping pictures
			MergePictures();

			do
			{
				picturesCount = this.Count;
				symbolsCount = symbols.Count;

				//merge with symbols
				for (int j = this.Count - 1; j >= 0; j--)
				{
					bool recompute = false;

					for (int i = symbols.Count - 1; i >= 0; i--)
						if (this[j].InterceptsWith(symbols[i]))
						{
							this[j].AddSymbolFast(symbols[i]);
							symbols.RemoveAt(i);
							recompute = true;
						}

					if (recompute)
						this[j].ComputeShape();
				}

				if (symbolsCount != symbols.Count)
				{
					//merge owerlapping pictures
					MergePictures();
				}

			} while (picturesCount != this.Count || symbolsCount != symbols.Count);

#if DEBUG
			//Console.WriteLine(string.Format("Pictures, MergeWithSymbols(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), base.Count));
#endif
		}
		#endregion

		#region MergePictures()
		private void MergePictures()
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			//merge owerlapping pictures
			for (int i = 0; i < this.Count - 1; i++)
			{
				List<Picture> mergingPictures = new List<Picture>();

				for (int j = i + 1; j < this.Count; j++)
					if (this[i].InterceptsWith(this[j].ConvexEnvelope))
						mergingPictures.Add(this[j]);

				if (mergingPictures.Count > 0)
				{
					mergingPictures.Add(this[i]);
					Picture mergedPicture = Picture.Merge(mergingPictures);

					foreach (Picture p in mergingPictures)
						this.Remove(p);

					this.Insert(i, mergedPicture);

/*#if SAVE_RESULTS
					DrawToFile(Debug.SaveToDir + "Pictures.png", new Size(10000, 8000));
					DrawObjectShapes(Debug.SaveToDir + "PictureShapes.png", new Size(10000, 8000));
					DrawConvexEnvelopes(Debug.SaveToDir + "PictureConvexEnvelopes.png", new Size(10000, 8000));
#endif*/
				}
			}

#if DEBUG
			//Console.WriteLine(string.Format("Pictures, MergePictures(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), base.Count));
#endif
		}
		#endregion
	
		#region MergeWithSymbols()
		/// <summary>
		/// This method assumes that pictures are already merged.
		/// </summary>
		/// <param name="symbols"></param>
		/*private void MergeWithSymbols(Symbols symbols)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif

			int picturesCount, symbolsCount;

			//merge owerlapping pictures
			for (int i = this.Count - 2; i >= 0; i--)
			{
				for (int j = this.Count - 1; j > i; j--)
				{
					if (this[i].InterceptsWith(this[j].ConvexEnvelope))
					{
						this[i].Merge(this[j]);
						this.RemoveAt(j);
					}
				}
			}

			do
			{
				picturesCount = this.Count;
				symbolsCount = symbols.Count;

				//merge with symbols
				for (int j = this.Count - 1; j >= 0; j--)
				{
					bool recompute = false;

					for (int i = symbols.Count - 1; i >= 0; i--)
						if (this[j].InterceptsWith(symbols[i]))
						{
							this[j].AddSymbolFast(symbols[i]);
							symbols.RemoveAt(i);
							recompute = true;
						}

					if (recompute)
						this[j].ComputeShape();
				}

				if (symbolsCount != symbols.Count)
				{
					//merge owerlapping pictures
					for (int i = this.Count - 2; i >= 0; i--)
						for (int j = this.Count - 1; j > i; j--)
						{
							if (this[i].InterceptsWith(this[j].ConvexEnvelope))
							{
								this[i].Merge(this[j]);
								this.RemoveAt(j);
							}
						}
				}

			} while (picturesCount != this.Count || symbolsCount != symbols.Count);

#if DEBUG
			Console.WriteLine(string.Format("Pictures, MergeWithSymbols(): {0}, Count: {1}", DateTime.Now.Subtract(start).ToString(), base.Count));
#endif
		}*/
		#endregion

		#region GetParagraph()
		/*private Paragraph GetParagraph(Symbol symbol, Paragraphs paragraphs)
		{
			foreach (Paragraph paragraph in paragraphs)
				if (Rectangle.Intersect(symbol.Rectangle, paragraph.Rectangle) != Rectangle.Empty)
					return paragraph;

			return null;
		}*/
		#endregion

		#region GetPicture()
		/*private Picture GetPicture(ObjectMap objectMap)
		{
			foreach (Picture picture in this)
				if (Rectangle.Intersect(picture.Rectangle, objectMap.Rectangle) != Rectangle.Empty)
					if (picture.InterceptsWith(objectMap))
						return picture;

			return null;
		}*/

		private Picture GetPicture(Symbol symbol)
		{
			foreach (Picture picture in this)
				if (Rectangle.Intersect(picture.Rectangle, symbol.Rectangle) != Rectangle.Empty)
					if (picture.InterceptsWith(symbol))
						return picture;

			return null;
		}
		#endregion

		#endregion

	}


}
