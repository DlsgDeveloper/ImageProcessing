using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace ImageProcessingApp
{
	public static class Merging
	{

		#region MergeHorizontally()
		/*public unsafe static void MergeHorizontally()
		{
			string source1 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Merging\UL.png";
			string source2 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Merging\UR.png";
			string source3 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Merging\LL.png";
			string source4 = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Merging\LR.png";
			string resultHoriz = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Merging\resultH.png";
			string resultVert = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Merging\resultV.png";

			using (Bitmap b1 = new Bitmap(source1))
			using (Bitmap b2 = new Bitmap(source2))
			using (Bitmap b3 = new Bitmap(source3))
			using (Bitmap b4 = new Bitmap(source4))
			{
				using(Bitmap resultBitmap = ImageProcessing.Merging.MergeHorizontally(new List<Bitmap>{b1, b2, b3, b4}))
					resultBitmap.Save(resultHoriz, ImageFormat.Png);
				
				using (Bitmap resultBitmap = ImageProcessing.Merging.MergeVertically(new List<Bitmap> { b1, b2, b3, b4 }))
					resultBitmap.Save(resultVert, ImageFormat.Png);
			}
		}*/
		#endregion

		#region MergeHorizontally()
		public unsafe static void MergeHorizontally()
		{
			string source1 = @"C:\delete\01.png";
			string resultHoriz = @"C:\Users\jirka.stybnar\TestRun\ImageProcessing\Merging\resultH.png";

			using (Bitmap b1 = new Bitmap(source1))
			{
				List<Bitmap> b = new List<Bitmap>();

				for (int i = 0; i < 18; i++)
					b.Add(b1);

				using (Bitmap resultBitmap = ImageProcessing.Merging.MergeHorizontally(b))
					resultBitmap.Save(resultHoriz, ImageFormat.Png);
			}
		}
		#endregion

	}
}
