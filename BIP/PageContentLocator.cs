using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	public class PageContentLocator
	{		
		//	PUBLIC METHODS

		#region GetContent()
		/// <summary>
		/// Returns clip surrounding all the objects, not doing any sophistication upon objects, inflated by parameter offset. 
		/// </summary>
		/// <param name="objects">Real objects, like letters, pictures, lines, ...</param>
		/// <param name="clip">Borders of returned clip.</param>
		/// <param name="flag">Not used.</param>
		/// <param name="offset">Offset from content on each side.</param>
		/// <returns></returns>
		/*public static Rectangle GetContent(PageObjects.Symbols objects, Rectangle clip, int dpi, int flag, int offset)
		{
			Rectangle	contentClip;

			objects.RemoveLines(8, 2);

			PageObjects.Symbols symbols = objects.GetSymbolsInClip(clip, false);

			PageObjects.Words words = PageObjects.ObjectLocator.FindWords(objects);
			//PageObjects.Paragraphs paragraphs = PageObjects.ObjectLocator.FindPages(words, clip, out confidence);
			PageObjects.Paragraphs paragraphs = PageObjects.ObjectLocator.FindParagraphs(symbols, words);

			foreach (PageObjects.Symbol symbol in symbols)
			{
				if(symbol.IsLine || symbol.IsPicture)
				{
					PageObjects.Paragraph paragraph = new PageObjects.Paragraph(symbol);

					paragraphs.Add(paragraph);
				}	
			}

			paragraphs.MergeNestedParagraphs();

			paragraphs.MergeVertAdjacentParagraphs();

			paragraphs.MergeCloseParagraphs((int) (dpi * 0.5));
			for (int i = paragraphs.Count - 1; i >= 0; i--)
			{
				if (paragraphs[i].Width < dpi * 2.0)
					paragraphs.RemoveAt(i);
			}

#if SAVE_RESULTS
			paragraphs.DrawToFile(Debug.SaveToDir + @"08 Paragraphs.png", new Size(clip.Right, clip.Bottom));
#endif
			contentClip = paragraphs.GetClip();
			if (contentClip.IsEmpty)
			{
				if (clip.IsEmpty)
					contentClip = symbols.GetClip();
				else
					contentClip = symbols.GetClip(clip);
			}
			
			if(contentClip.IsEmpty)
				contentClip = clip;

			contentClip.Inflate((int) offset, (int) offset);
			if(clip.IsEmpty == false)
				contentClip = Rectangle.Intersect(contentClip, clip);

			return contentClip;
		}*/
		#endregion

								
	}
}
