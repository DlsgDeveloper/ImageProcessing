using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessingApp
{
	class BitmapsWithReverseBits
	{
		public static bool HasReverseBits()
		{
			string path1 = @"C:\delete\image.bmp";
			string result1 = @"C:\delete\result1.tif";

			ImageProcessing.ImageFile.ImageInfo info1 = new ImageProcessing.ImageFile.ImageInfo(path1);
			Console.WriteLine(info1.ToString());

			using (Bitmap b1 = new Bitmap(path1))
			{
				PropertyItem item = (PropertyItem)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(PropertyItem));

				byte[] value = new byte[2] {0, 0};

				item.Id = 262;
				item.Len = 1;
				item.Type = 3;
				item.Value = value;

				b1.SetPropertyItem(item); 

				b1.Save(result1, ImageFormat.Tiff);
			}
			
			ImageProcessing.ImageFile.ImageInfo info2 = new ImageProcessing.ImageFile.ImageInfo(result1);
			Console.WriteLine(info2.ToString());

			using (Bitmap b2 = new Bitmap(result1))
			{
				b2.PropertyItems[5].Value[0] = 0;
				b2.Save(result1, ImageFormat.Tiff);
			}

			return false;
		}

		public static bool SaveTiff()
		{
			string result1 = @"C:\delete\result1.tif";
			string result2 = @"C:\delete\result2.tif";

			ImageProcessing.ImageFile.ImageInfo info1 = new ImageProcessing.ImageFile.ImageInfo(result1);
			Console.WriteLine(info1.ToString());

			/*using (Bitmap b = new Bitmap(result1))
			{
				PropertyItem item = (PropertyItem)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(PropertyItem));

				byte[] value = new byte[2] { 0, 0 };

				item.Id = 500;
				item.Len = 2;
				item.Type = 3;
				item.Value = value;

				b.SetPropertyItem(item); 
				
				b.Save(result2, ImageFormat.Tiff);
			}*/

			using (Bitmap b = new Bitmap(result1))
			{
				PropertyItem item = b.GetPropertyItem(262);

				byte[] value = new byte[2] { 0, 0 };

				item.Value = value;

				b.SetPropertyItem(item);

				b.Save(result2, ImageFormat.Tiff);
			} 
			
			return false;
		}

	}
}
