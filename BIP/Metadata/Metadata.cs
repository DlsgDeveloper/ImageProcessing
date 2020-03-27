using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;


namespace BIP.Metadata
{
	public class Metadata
	{
		Bitmap bitmap;
		PropertyItem universalProperty;


		#region constructor
		public Metadata(Bitmap bitmap)
		{
			this.bitmap = bitmap;

			if (bitmap.PropertyItems.Length > 0)
				universalProperty = bitmap.PropertyItems[0];
			else
			{
				Bitmap b = new Bitmap(8, 8, PixelFormat.Format24bppRgb);

				using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
				{
					b.Save(stream, ImageFormat.Jpeg);
					b.Dispose();

					b = new Bitmap(stream);
					universalProperty = b.PropertyItems[0];
					b.Dispose();
				}
			}
		}
		#endregion


		//PUBLIC METHODS
		#region public methods

		#region ClearAllMetadata()
		public void ClearAllMetadata()
		{
			for (int i = bitmap.PropertyItems.Length - 1; i >= 0; i--)
				bitmap.RemovePropertyItem(bitmap.PropertyItems[i].Id);
		}
		#endregion

		#region GetExifMetadata()
		public ExifMetadata GetExifMetadata()
		{
			ExifMetadata md = new ExifMetadata();

			foreach (PropertyItem propertyItem in this.bitmap.PropertyItems)
			{
				PropertyBase property = md.GetProperty(propertyItem.Id);

				if(property != null)
					property.ImportFromPropertyItem(propertyItem);
			}

			return md;
		}
		#endregion

		#region SaveExifMetadata()
		public void SaveExifMetadata(ExifMetadata md)
		{
			foreach (BIP.Metadata.PropertyBase property in md.Properties)
			{
				if (property.Defined)
				{
					if (this.bitmap.PropertyIdList.Contains((int)property.TagId))
						this.bitmap.RemovePropertyItem((int)property.TagId);

					property.ExportToPropertyItem(universalProperty);
					bitmap.SetPropertyItem(universalProperty);
				}
			}
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#endregion
	}
}
