using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace ImageProcessing.PageObjects
{
	public class Pages : List<Page>
	{
		
		//PUBLIC METHODS
		#region public methods

		#region GetClip()
		public Rectangle GetClip()
		{
			if (this.Count > 0)
			{
				Rectangle clip = this[0].Rectangle;
				
				for (int i = 1; i < this.Count; i++)
					if (this[i].Rectangle != Rectangle.Empty)
						clip = Rectangle.Union(clip, this[i].Rectangle);
				
				return clip;
			}
			
			return Rectangle.Empty;
		}
		#endregion

		#region Compact()
		public void Compact(Size imageSize)
		{
			Rectangle	clip = GetClip();
			bool		twoPages = (clip.Width > imageSize.Width * 0.5);

			//make sure there are 2 Pages
			if(twoPages)
			{
				//if one of the clips is wider than two thirds of the clip, it is 1-page image. 
				foreach (Page page in this)
				{
					if(page.Width > clip.Width * 0.66)
					{
						twoPages = false;
						break;
					}
				}
			}

			if (twoPages)
			{
				int middlePoint = clip.X + clip.Width / 2;
				Page pageL = null;
				Page pageR = null;

				for (int i = this.Count - 1; i >= 0; i--)
				{
					Page page = this[i];

					if ((page.X + page.Width / 2.0) < middlePoint)
					{
						if (pageL == null)
							pageL = page;
						else
						{
							pageL.Merge(page);
							this.RemoveAt(i);
						}
					}
					else
					{
						if (pageR == null)
							pageR = page;
						else
						{
							pageR.Merge(page);
							this.RemoveAt(i);
						}
					}
				}
			}
			else
			{
				while (this.Count > 1)
				{
					this[0].Merge(this[1]);
					this.RemoveAt(1);
				}
			}

			this.Sort();
		}
		#endregion

		#region LeaveBiggest2Pages()
		/*public void LeaveBiggest2Pages(Rectangle clip)
		{
			int biggestSize = 0;
			int secondBiggestSize = 0;
			int biggestIndex = 0;
			int secondBiggestIndex = 0;
			
			if (this.Count > 1)
			{
				int i;
				
				for (i = 0; i < this.Count; i++)
				{
					int currentSize = this[i].Width * this[i].Height;
					if (biggestSize < currentSize)
					{
						secondBiggestIndex = biggestIndex;
						biggestIndex = i;
						secondBiggestSize = biggestSize;
						biggestSize = currentSize;
					}
					else if (secondBiggestSize < currentSize)
					{
						secondBiggestIndex = i;
						secondBiggestSize = currentSize;
					}
				}
				
				for (i = this.Count - 1; i >= 0; i--)
				{
					if ((i != biggestIndex) && (i != secondBiggestIndex))
					{
						this.RemoveAt(i);
					}
				}
				
				this.Validate2BiggestPages(clip);
				this.MakePagesSameSize(clip);
			}
		}*/
		#endregion

		#region MakePagesSameSize()
		/*public void MakePagesSameSize(Rectangle clip)
		{
			if (this.Count == 2)
			{
				Page p0 = this[0];
				Page p1 = this[1];
				int differenceX = Math.Abs((int)(p0.Width - p1.Width));
				int differenceY = Math.Abs((int)(p0.Height - p1.Height));
				
				if (p0.Width > p1.Width)
				{
					p1.Width = p0.Width;
					p1.X = Math.Max(clip.X, Math.Min((int)(clip.Right - p1.Width), (int)(p1.X - (differenceX / 2))));
				}
				else if (p0.Width < p1.Width)
				{
					p0.Width = p1.Width;
					p0.X = Math.Max(clip.X, Math.Min((int)(clip.Right - p0.Width), (int)(p0.X - (differenceX / 2))));
				}
				
				if (p0.Height > p1.Height)
				{
					if (p1.Y > p0.Y)
						p1.Y = Math.Max(0, (p1.Bottom > p0.Bottom) ? (p1.Bottom - p0.Height) : p0.Y);
					
					p1.Height = Math.Min(p0.Height, clip.Height - p1.Y);
				}
				else if (p1.Height > p0.Height)
				{
					if (p0.Y > p1.Y)
						p0.Y = Math.Max(0, (p0.Bottom > p1.Bottom) ? (p0.Bottom - p1.Height) : p1.Y);

					p0.Height = Math.Min(p1.Height, clip.Height - p0.Y);
				}
			}
		}*/
		#endregion

		#region MergeClosePages()
		/*public void MergeClosePages(int maxDistanceInPixels)
		{
			int count;
			
			do
			{
				count = this.Count;
				
				for (int i = this.Count - 1; i > 0; i--)
				{
					for (int j = 0; j < i; j++)
					{
						if (this.Distance(this[i].Rectangle, this[j].Rectangle) <= maxDistanceInPixels)
						{
							this[j].Merge(this[i]);
							this.RemoveAt(i);
							break;
						}
					}
				}
			}
			while (this.Count != count);
		}*/
		#endregion

		#region MergeNestedPages()
		public void MergeNestedPages()
		{
			int count;
			do
			{
				count = this.Count;
				for (int i = this.Count - 1; i > 0; i--)
				{
					Page page1 = this[i];
					Rectangle rect1 = page1.Rectangle;
					
					for (int j = 0; j < i; j++)
					{
						Page page2 = this[j];
						Rectangle rect2 = page2.Rectangle;
						Rectangle intersect = Rectangle.Intersect(rect1, rect2);
						
						if (((intersect.Width > 0) && (intersect.Height > 0)) && ((((3 * intersect.Width) * intersect.Height) > (rect1.Width * rect1.Height)) || (((3 * intersect.Width) * intersect.Height) > (rect2.Width * rect2.Height))))
						{
							page2.Merge(page1);
							this.RemoveAt(i);
							break;
						}
					}
				}
			}
			while (this.Count != count);
		}
		#endregion

		#region MergeVertAdjacentPages()
		public void MergeVertAdjacentPages()
		{
			int count;
			
			do
			{
				count = this.Count;
				for (int i = this.Count - 2; i >= 0; i--)
				{
					for (int j = this.Count - 1; j > i; j--)
					{
						Page page1 = this[i];
						Page page2 = this[j];

						if (ArePagesAdjacent(page1, page2))
						{
							page1.Merge(page2);
							this.RemoveAt(j);
						}
					}
				}
			}
			while (count != this.Count);
		}
		#endregion

		#region MergeHorizontaly()
		/*public void MergeHorizontaly()
		{
			int count;
			do
			{
				count = this.Count;
				for (int i = 0; i < (this.Count - 1); i++)
				{
					for (int j = i + 1; j < this.Count; j++)
					{
						Page page1 = this[i];
						Page page2 = this[j];
						if (ArePagesAdjacent(page1, page2))
						{
							page1.Merge(page2);
							this.RemoveAt(j);
							break;
						}
					}
				}
			}
			while (count != this.Count);
		}*/
		#endregion

		#region Validate2BiggestPages()
		private void Validate2BiggestPages(Rectangle clip)
		{
			if (this.Count > 1)
			{
				Page p0 = this[0];
				Page p1 = this[1];
				int clipHalfX = clip.X + (clip.Width / 2);
				if (((p0.Center.X < clipHalfX) && (p1.Center.X < clipHalfX)) || ((p0.Center.X > clipHalfX) && (p1.Center.X > clipHalfX)))
				{
					if (p0.Width < (p1.Width / 2))
					{
						this.RemoveAt(0);
					}
					else if (p0.Width > (p1.Width * 2))
					{
						this.RemoveAt(1);
					}
					else
					{
						p0.Merge(p1);
					}
					Page p = this[0];
					int x = (p.Center.X > clipHalfX) ? (clip.Width - p.Right) : (clipHalfX + (clipHalfX - this[0].Right));
					this.Insert((x < p.X) ? 0 : 1, new Page(new Rectangle(x, p.Y, p.Width, p.Height)));
				}
				else if (this[1].X < this[0].X)
				{
					this.RemoveAt(1);
					this.RemoveAt(0);
					this.Insert(0, p1);
					this.Insert(1, p0);
				}
			}
		}
		#endregion 

		#region GetBiggestPage()
		/*public Page GetBiggestPage()
		{
			Page biggestPage = null;
			
			if (this.Count > 0)
			{
				biggestPage = this[0];
				
				for (int i = 1; i < this.Count; i++)
					if ((biggestPage.Width * biggestPage.Height) < (this[i].Width * this[i].Height))
						biggestPage = this[i];
			}
			
			return biggestPage;
		}*/
		#endregion

		#region DrawToFile()
		public void DrawToFile(string filePath, Size imageSize)
		{
#if SAVE_RESULTS
			try
			{
				Bitmap result = Debug.GetBitmap(imageSize);
				int counter = 0;
				Graphics g = Graphics.FromImage(result);

				foreach (Page page in this)
				{
					Color color = Debug.GetColor(counter++);

					g.FillRectangle(new SolidBrush(Color.FromArgb(100, color.R, color.G, color.B)), page.Rectangle);
				}

				result.Save(filePath, ImageFormat.Png);
				result.Dispose();
			}
			catch { }
#endif
		}
		#endregion


		#endregion

		//PRIVATE METHODS
		#region private methods

		#region ArePagesAdjacent()
		private static bool ArePagesAdjacent(Page page1, Page page2)
		{
			int minWidth = (page1.Width < page2.Width) ? page1.Width : page2.Width;
			int leftSharePoint = (page1.X < page2.X) ? page2.X : page1.X;
			int rightSharePoint = (page1.Right < page2.Right) ? page1.Right : page2.Right;
			int shareLength = rightSharePoint - leftSharePoint;
			
			return (shareLength > minWidth * 0.66);
		}
		#endregion

		#endregion


	}
}
