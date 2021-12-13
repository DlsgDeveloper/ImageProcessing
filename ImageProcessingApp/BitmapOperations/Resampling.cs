using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace TestApp.BitmapOperations
{
	class Resampling
	{

		#region Resample()
		public static void Resample()
		{
			DirectoryInfo sourceDir = new DirectoryInfo(TestApp.Misc.TestRunDir.FullName + @"\ImageProcessing\Resampling");
			DirectoryInfo destDir = new DirectoryInfo(TestApp.Misc.TestRunDir.FullName + @"\ImageProcessing\Resampling\results");
			List<FileInfo> list = sourceDir.GetFiles().ToList();

			destDir.Create();

			for (int i = 0; i < list.Count; i++)
			{
				FileInfo file = list[i];

				using (Bitmap bitmap = new Bitmap(file.FullName))
				{
					using (Bitmap result = ImageProcessing.Resampling.Resample(bitmap, ImageProcessing.PixelsFormat.Format32bppRgb))
					{
						result.Save(Path.Combine(destDir.FullName, file.Name), ImageFormat.Png);
					}
				}
			}
		}
		#endregion

	}
}
