using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing;
using System.IO;

namespace TestApp
{
	class Resizing
	{
		#region constructor
		public Resizing()
		{

		}
		#endregion


		#region Resize()
		public unsafe static void Resize()
		{
			string source32bpp = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Rotation\result32bpp.png";
			string result32bpp = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Resizing\result32bpp.png";

			using (Bitmap b = new Bitmap(source32bpp))
			{
				DateTime start = DateTime.Now;
				using (Bitmap result = ImageProcessing.Resizing.Resize(b, Rectangle.Empty, 1.89, ImageProcessing.Resizing.ResizeMode.Quality))
				{
					Console.WriteLine("Resize: " + DateTime.Now.Subtract(start).ToString());

					result.Save(result32bpp, ImageFormat.Png);
				}
			}
		}
		#endregion

		#region ResizeDir()
		public unsafe static void ResizeDir()
		{
			DirectoryInfo	sourceDir = new DirectoryInfo(@"C:\delete\00");
			DirectoryInfo	destDir = new DirectoryInfo(@"C:\delete\00\resized");
			List<FileInfo>	list = sourceDir.GetFiles("FN-000004.tiff").ToList();
			
			destDir.Create();

			for (int i = 0; i < list.Count; i++)
			{
				FileInfo file = list[i];

				using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(file.FullName))
				{
					ImageProcessing.BigImages.Resizing resizing = new ImageProcessing.BigImages.Resizing();
					double zoom = Math.Min(1.0, 300.0 / itDecoder.DpiX);
					// Record the results...
					if (itDecoder.PixelFormat == System.Drawing.Imaging.PixelFormat.Format1bppIndexed)
					{
						resizing.Resize(itDecoder, Path.Combine(destDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + ".png"), new ImageProcessing.FileFormat.Png(), zoom);
					}
					else
					{
						resizing.Resize(itDecoder, Path.Combine(destDir.FullName, Path.GetFileNameWithoutExtension(file.Name) + ".png"), new ImageProcessing.FileFormat.Png(), zoom);
					}
				}
			}
		}
		#endregion

		#region ResizeAndResampleBigImage()
		public unsafe static void ResizeAndResampleBigImage()
		{
			string source = @"C:\Users\jirka.stybnar\TestRun\Big Images\24 bpp 1200dpi.jpg";
			string dest = @"C:\Users\jirka.stybnar\TestRun\Big Images\result.jpg";

			DateTime start = DateTime.Now;

			ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source);
			ImageProcessing.BigImages.ResizingAndResampling resizing = new ImageProcessing.BigImages.ResizingAndResampling();

			resizing.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);
			resizing.ResizeAndResample(itDecoder, dest, new ImageProcessing.FileFormat.Jpeg(80), PixelsFormat.Format8bppGray, 0.2, 0.2, 0.8, new ColorD(150, 150, 150));
		}
		#endregion

		#region ResizeAndResampleBigImages()
		public unsafe static void ResizeAndResampleBigImages()
		{
			FileInfo[] sources = new DirectoryInfo(@"C:\delete\kic").GetFiles("*.jpg");
			ImageProcessing.BigImages.ResizingAndResampling resizing = new ImageProcessing.BigImages.ResizingAndResampling();
			resizing.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);
			DateTime start = DateTime.Now;

			foreach (FileInfo file in sources)
			{
				using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(file.FullName))
				{
					//resizing.ResizeAndResample(itDecoder, @"C:\delete\kic\jpg\" + file.Name, new ImageProcessing.FileFormat.Jpeg(80), PixelsFormat.Format8bppGray, 1.0 / 3.0, 0, 0, new ColorD(150, 150, 150));
					//resizing.ResizeAndResample(itDecoder, @"C:\delete\kic\png\" + file.Name, new ImageProcessing.FileFormat.Png(), PixelsFormat.Format8bppGray, 1.0 / 3.0, 0, 0, new ColorD(150, 150, 150));
					//resizing.ResizeAndResample(itDecoder, @"C:\delete\kic\tif\" + file.Name, new ImageProcessing.FileFormat.Tiff(ImageProcessing.IpSettings.ItImage.TiffCompression.LZW), PixelsFormat.Format8bppGray, 1.0 / 3.0, 0, 0, new ColorD(150, 150, 150));
					resizing.ResizeAndResample(itDecoder, @"C:\delete\kic\none\" + file.Name, new ImageProcessing.FileFormat.Tiff(ImageProcessing.IpSettings.ItImage.TiffCompression.None), PixelsFormat.Format8bppGray, 1.0 / 3.0, 0, 0, new ColorD(150, 150, 150));
				}
			}
		}
		#endregion

		#region ResizeBigImage()
		public unsafe static void ResizeBigImage()
		{
			string source = @"C:\\ProgramData\\DLSG\\KIC\\Images\\Default User\\Session_2022-01-13_14-37-49\\1004_1.kic";
			string dest = @"C:\\ProgramData\\DLSG\\KIC\\Images\\Default User\\Session_2022-01-13_14-37-49\\1004_1_preview.kic";
			DateTime start = DateTime.Now;

			using (ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source))
			{
				ImageProcessing.BigImages.Resizing resizing = new ImageProcessing.BigImages.Resizing();
				double zoom = 0.5;

				//resizing.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);

				if (itDecoder.PixelFormat == System.Drawing.Imaging.PixelFormat.Format1bppIndexed)
				{
					resizing.Resize(itDecoder, dest, new ImageProcessing.FileFormat.Png(), zoom);
				}
				else
				{
					resizing.Resize(itDecoder, dest, new ImageProcessing.FileFormat.Jpeg(90), zoom);
				}
			}

			Console.WriteLine("KicImageFiles, CreateReducedFiles() 3: {0}", DateTime.Now.Subtract(start));
		}
		#endregion

		#region ResizeBigImages()
		public unsafe static void ResizeBigImages()
		{
			FileInfo[] sources = new DirectoryInfo(@"C:\Users\jirka.stybnar\TestRun\ColorModes").GetFiles();
			string dest = @"C:\Users\jirka.stybnar\TestRun\ColorModes\results\";

			foreach (FileInfo source in sources)
			{
				try
				{
					DateTime start = DateTime.Now;

					ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source.FullName);
					ImageProcessing.BigImages.Resizing resizing = new ImageProcessing.BigImages.Resizing();

					resizing.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);
					resizing.Resize(itDecoder, dest + source.Name, new ImageProcessing.FileFormat.Png(), 1.0235);

					Console.WriteLine(source.Name + ": " + DateTime.Now.Subtract(start).ToString());
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error in " + source.Name + ": " + ex.Message);
				}
			}
		}
		#endregion

		#region ResizeBigImageToBitmap()
		public unsafe static void ResizeBigImageToBitmap()
		{
			string source = @"C:\OpusFreeFlowWorkingData\00000\003\ScanImages\Full\00000003_000001.Tif";
			string dest = @"C:\OpusFreeFlowWorkingData\00000\003\ScanImages\Reduced\00000003_000001.png";

			DateTime start = DateTime.Now;

			ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source);
			ImageProcessing.BigImages.Resizing resizing = new ImageProcessing.BigImages.Resizing();

			resizing.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);
			Bitmap result = resizing.ResizeToBitmap(itDecoder, new Rectangle(500, 500, 10000, 10000), 0.1);

			result.Save(dest, ImageFormat.Png);
		}
		#endregion

		#region ResizeBigImageToGrayscaleBitmap()
		public unsafe static void ResizeBigImageToGrayscaleBitmap()
		{
			try
			{
				string source = @"C:\OpusFreeFlowWorkingData\00000\003\ScanImages\Full\00000003_000001.Tif";
				string dest = @"C:\OpusFreeFlowWorkingData\00000\003\ScanImages\Reduced\00000003_000001.png";

				DateTime start = DateTime.Now;

				ImageProcessing.BigImages.ItDecoder itDecoder = new ImageProcessing.BigImages.ItDecoder(source);
				ImageProcessing.BigImages.ResizingAndResampling resizing = new ImageProcessing.BigImages.ResizingAndResampling();

				resizing.ProgressChanged += new ImageProcessing.ProgressHnd(ProgressChanged);
				/*using (Bitmap result = resizing.ResizeAndResampleToBitmap(itDecoder, Rectangle.Empty, PixelsFormat.Format8bppGray, 0.1))
				{
					result.Save(dest, ImageFormat.Png);
				}*/
				using (Bitmap result = resizing.ResizeAndResampleToBitmap(itDecoder, new Rectangle(500, 500, 10000, 10000), PixelsFormat.Format8bppGray, 0.1))
				{
					result.Save(dest, ImageFormat.Png);
				}

				Console.WriteLine("The End: " + DateTime.Now.Subtract(start).ToString());
				Console.ReadLine();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.ReadLine();
			}
		}
		#endregion


		// PRIVATE METHODS
		#region private methods

		#region ProgressChanged()
		private static void ProgressChanged(float progress)
		{
			Console.WriteLine(string.Format("{0}, {1:00.00}%", DateTime.Now.ToString("HH:mm:ss,ff"), progress * 100.0));
		}
		#endregion

		#endregion

	}
}
