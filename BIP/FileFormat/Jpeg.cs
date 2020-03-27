using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing.FileFormat
{
	public class Jpeg : ImageProcessing.FileFormat.IImageFormat
	{
		public byte Quality;

		public Jpeg(byte quality)
		{
			this.Quality = quality;
		}
	}
}
