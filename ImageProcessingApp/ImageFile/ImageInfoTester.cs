using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageProcessingApp.ImageFile
{
	class ImageInfoTester
	{

		public static void Go()
		{
			//FileInfo file = new FileInfo(@"C:\delete\02.jpg");
			FileInfo file = new FileInfo(@"C:\delete\00000000.jpg");
			//FileInfo file = new FileInfo(@"C:\delete\01 LZW.tif");
			//FileInfo file = new FileInfo(@"C:\delete\01 None.tif");

			ImageProcessing.ImageFile.ImageInfo info = new ImageProcessing.ImageFile.ImageInfo(file);

			Console.WriteLine("Image Info:");
			Console.WriteLine(string.Format("	Size: [{0} x {1}]", info.Width, info.Height));
			Console.WriteLine(string.Format("	Horiz DPI : {0}, Vert DPI: {1}", info.DpiH, info.DpiV));
			Console.WriteLine(string.Format("	Bits per pixel: {0}", info.BitsPerPixel));
			Console.WriteLine(string.Format("	Pixel Format: {0}", info.PixelFormat.ToString()));
			Console.WriteLine(string.Format("	Pixels Format: {0}", info.PixelsFormat.ToString()));
		}

	}
} 
