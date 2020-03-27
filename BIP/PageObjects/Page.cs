using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace ImageProcessing.PageObjects
{
	public class Page : IComparable<Page>
	{
		public readonly List<IPageObject> PageObjects = new List<IPageObject>();
		
		int			x = int.MaxValue;
		int			y = int.MaxValue;
		int			right = int.MinValue;
		int			bottom = int.MinValue;


		#region constructor
		public Page(IPageObject pageObject)
		{
			this.PageObjects.Add(pageObject);

			this.x = pageObject.X;
			this.y = pageObject.Y;
			this.right = pageObject.Right;
			this.bottom = pageObject.Bottom;
		}

		public Page(Rectangle rect)
		{
			this.x = rect.X;
			this.y = rect.Y;
			this.right = rect.Right;
			this.bottom = rect.Bottom;
		}
		#endregion

		public int			X		{ get { return this.x; } set { this.right += value - this.x; this.x = value;  } }
		public int			Y		{ get { return this.y; } set { this.bottom += value - this.y; this.y = value; } }
		public int			Right	{ get { return this.right; } /*set { this.right = value; }*/ }
		public int			Bottom	{ get { return this.bottom; } /*set { this.bottom = value; }*/ }
		public int			Width	{ get { return this.Right - this.x; } set { this.right = this.x + value; } }
		public int			Height	{ get { return this.Bottom - this.y; } set { this.bottom = this.y + value; } }
		public Rectangle	Rectangle{get {return Rectangle.FromLTRB(x, y, right, bottom);} }
		public Point		Center	{ get { return new Point(x + (right - x) / 2, y + (bottom - y) / 2); } }

		#region class PageComparer
		/*public class PageComparer : IComparer<Page>
		{
			public int Compare(Page p1, Page p2)
			{
				int maxX = (p1.X > p2.X) ? p1.X : p2.X;
				int minR = (p1.Right < p2.Right) ? p1.Right : p2.Right;

				if( (minR - maxX) > (p1.Width * .2) || (minR - maxX) > (p2.Width * .2))
				{
					//paragraphs are 1 above and 1 below
					if(p1.Y > p2.Y)
						return 1;
					else if(p1.Y < p2.Y)
						return -1;
					else 
						return 0;
				}
				else
				{
					if(p1.X > p2.X)
						return 1;
					else if(p1.X < p2.X)
						return -1;
					else 
						return 0;
				}
			}
		} */
		#endregion

		#region AddObject()
		public void AddObject(IPageObject pageObject)
		{
			this.PageObjects.Add(pageObject);

			if(this.x > pageObject.X)
				this.x = pageObject.X;
			if(this.y > pageObject.Y)
				this.y = pageObject.Y;
			if(this.right < pageObject.Right)
				this.right = pageObject.Right;
			if(this.bottom < pageObject.Bottom)
				this.bottom = pageObject.Bottom;
		}
		#endregion

		#region Merge()
		public void Merge(Page page)
		{
			foreach(IPageObject pageObject in page.PageObjects)
				if(this.PageObjects.Contains(pageObject) == false)
					this.AddObject(pageObject);

			if (this.x > page.X)
				this.x = page.X;
			if (this.y > page.Y)
				this.y = page.Y;
			if (this.right < page.Right)
				this.right = page.Right;
			if (this.bottom < page.Bottom)
				this.bottom = page.Bottom;
		}
		#endregion

		#region IComparable Members

		public int CompareTo(Page page)
		{
			int maxX = (this.X > page.X) ? this.X : page.X;
			int minR = (this.Right < page.Right) ? this.Right : page.Right;

			if ((minR - maxX) > (this.Width * .2) || (minR - maxX) > (page.Width * .2))
			{
				//paragraphs are 1 above and 1 below
				if (this.Y > page.Y)
					return 1;
				else if (this.Y < page.Y)
					return -1;
				else
					return 0;
			}
			else
			{
				if (this.X > page.X)
					return 1;
				else if (this.X < page.X)
					return -1;
				else
					return 0;
			}

			
			/*if (this.Y < page.Y)
				return -1;
			else if (this.Y == page.Y)
				return 0;
				
			return 1;*/
		}

		#endregion
	}
}
