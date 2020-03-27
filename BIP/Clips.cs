using System;
using System.Collections;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Clips.
	/// </summary>
	public class Clips : ArrayList
	{
		public Clips()
			: base()
		{
		}

		new public Clip this[int index]
		{
			get
			{
				if(index >= 0 && index < this.Count)
					return (Clip) base[index] ;
				else 
					return null ;
			}
		}

		#region Add()
		public void Add(Clip clip)
		{
			base.Add(clip);
		}

		[Obsolete("Invalid parameter used!", true)]
		new public void Add(object noCorrect)
		{
		}
		#endregion
	}
}
