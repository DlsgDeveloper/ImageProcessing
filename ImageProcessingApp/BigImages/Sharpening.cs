using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessingApp.BigImages
{
	class Sharpening
	{

		#region Sharpnen()
		public static void Sharpnen()
		{
			string source = @"C:\delete\del\IMG_0012.JPG";
			string result = @"C:\delete\del\Sharpened.png";

			ImageProcessing.BigImages.Sharpening sharpening = new ImageProcessing.BigImages.Sharpening();

			sharpening.ProgressChanged = delegate(float progress) { Misc.LogProgressChanged(progress); };
			sharpening.Laplacian3x3(new ImageProcessing.BigImages.ItDecoder(source), result, new ImageProcessing.FileFormat.Png());
		}
		#endregion

	}
}
