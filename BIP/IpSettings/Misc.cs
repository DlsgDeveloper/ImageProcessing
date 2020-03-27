using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing.IpSettings
{
	
	public delegate void ItPropertiesChangedHnd(ItImage itImage, ItProperty type);

	
	[Flags]
	public enum ItProperty
	{
		None = 0x00,

		/// <summary>
		/// clip size, skew
		/// </summary>
		Clip = 0x02,

		/// <summary>
		/// curve, points location
		/// </summary>
		Bookfold = 0x04,

		/// <summary>
		/// added, removed, changed size, ...
		/// </summary>
		Fingers = 0x08,

		ImageSettings = 0x10
	}

}
