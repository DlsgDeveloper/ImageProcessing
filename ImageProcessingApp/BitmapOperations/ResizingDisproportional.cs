using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace ImageProcessingApp.BitmapOperations
{
	public static class ResizingDisproportional
	{

		#region Resize()
		public unsafe static void Resize()
		{
			string source = @"C:\delete\top1.png";
			string result = @"C:\delete\top2.png";

			using (Bitmap b = new Bitmap(source))
			{
				using(Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 2793, 105, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(result, ImageFormat.Png);
			}
		}
		#endregion

		#region Resize2()
		public unsafe static void Resize2()
		{
			string sourceHoriz = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Horizontal.png";
			string resultHoriz1 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\HorizontalResult1.png";
			string resultHoriz2 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\HorizontalResult2.png";

			string sourceVert = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Vertical.png";
			string resultVert1 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\VerticalResult1.png";
			string resultVert2 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\VerticalResult2.png";

			string sourceSquare32 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square32.png";
			string resultSquare32_1 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square32Result1.png";
			string resultSquare32_2 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square32Result2.png";
			string resultSquare32_3 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square32Result3.png";
			string resultSquare32_4 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square32Result4.png";

			string sourceSquare24 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square24.jpg";
			string resultSquare24_1 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square24Result1.png";
			string resultSquare24_2 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square24Result2.png";
			string resultSquare24_3 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square24Result3.png";
			string resultSquare24_4 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square24Result4.png";

			string sourceSquare8 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square8.tif";
			string resultSquare8_1 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square8Result1.png";
			string resultSquare8_2 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square8Result2.png";
			string resultSquare8_3 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square8Result3.png";
			string resultSquare8_4 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\BitmapOperations\ResizingDisproportional\Square8Result4.png";

			using (Bitmap b = new Bitmap(sourceHoriz))
			{
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 400, 50, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultHoriz1, ImageFormat.Png);
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 900, 50, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultHoriz2, ImageFormat.Png);
			}

			using (Bitmap b = new Bitmap(sourceVert))
			{
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 50, 400, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultVert1, ImageFormat.Png);
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 50, 900, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultVert2, ImageFormat.Png);
			}

			using (Bitmap b = new Bitmap(sourceSquare32))
			{
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 500, 500, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultSquare32_1, ImageFormat.Png);
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 2000, 2000, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultSquare32_2, ImageFormat.Png);
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 500, 2000, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultSquare32_3, ImageFormat.Png);
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 2000, 500, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultSquare32_4, ImageFormat.Png);
			}

			using (Bitmap b = new Bitmap(sourceSquare24))
			{
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 789, 789, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultSquare24_1, ImageFormat.Png);
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 1234, 1234, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultSquare24_2, ImageFormat.Png);
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 789, 1234, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultSquare24_3, ImageFormat.Png);
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 1234, 789, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultSquare24_4, ImageFormat.Png);
			}

			using (Bitmap b = new Bitmap(sourceSquare8))
			{
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 789, 789, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultSquare8_1, ImageFormat.Png);
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 1234, 1234, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultSquare8_2, ImageFormat.Png);
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 789, 1234, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultSquare8_3, ImageFormat.Png);
				using (Bitmap resultBitmap = ImageProcessing.BitmapOperations.ResizingDisproportional.Resize(b, 1234, 789, ImageProcessing.BitmapOperations.ResizingDisproportional.ResizeMode.Quality))
					resultBitmap.Save(resultSquare8_4, ImageFormat.Png);
			}

		}
		#endregion

	}
}
