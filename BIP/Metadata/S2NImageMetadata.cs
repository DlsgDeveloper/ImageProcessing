using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessing.Languages;

namespace BIP.Metadata
{
	public class S2NImageMetadata
	{
		//
		// JPEG Marker codes
		//
		// Start of Frame markers, non-differential, Huffman coding

		static byte SOF_0 = 0xc0;    // Baseline DCT
		static byte SOF_1 = 0xc1;    // Extended sequential DCT
		static byte SOF_2 = 0xc2;    // Progressive DCT
		static byte SOF_3 = 0xc3;    // Lossless (sequential)

		static byte SOI = 0xd8;    // Start of Image
		static byte EOI = 0xd9;    // End of Image
		static byte SOS = 0xda;    // Start of Scan

		static byte APP_0 = 0xe0;    // Application specific segments
		static byte APP_1 = 0xe1;    // Application specific segments
		static byte APP_2 = 0xe2;    // Application specific segments
		static byte APP_3 = 0xe3;    // Application specific segments
		static byte APP_4 = 0xe4;    // Application specific segments
		static byte APP_5 = 0xe5;    // Application specific segments
		static byte APP_6 = 0xe6;    // Application specific segments
		static byte APP_7 = 0xe7;    // Application specific segments
		static byte APP_8 = 0xe8;    // Application specific segments
		static byte APP_9 = 0xe9;    // Application specific segments
		static byte APP_10 = 0xea;    // Application specific segments
		static byte APP_11 = 0xeb;    // Application specific segments
		static byte APP_12 = 0xec;    // Application specific segments
		static byte APP_13 = 0xed;    // Application specific segments
		static byte APP_14 = 0xee;    // Application specific segments
		static byte APP_15 = 0xef;    // Application specific segments

		//static byte COM = 0xfe;    // Comment



		#region GetS2NScanParametersFromJpeg()
		public static string GetS2NScanParametersFromJpeg(FileInfo file)
		{
			using (FileStream reader = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				byte[] buffer = new byte[reader.Length];
				reader.Position = 0;
				reader.Read(buffer, 0, (int)reader.Length);
				reader.Position = 0;

				for (int i = 2; i < (int)reader.Length; i++)
				{
					if (buffer[i] == 0xFF)
					{
						if (buffer[i + 1] == APP_1 || buffer[i + 1] == APP_2 || buffer[i + 1] == APP_3 || buffer[i + 1] == APP_4 ||
							buffer[i + 1] == APP_5 || buffer[i + 1] == APP_6 || buffer[i + 1] == APP_7 || buffer[i + 1] == APP_8 ||
							buffer[i + 1] == APP_9 || buffer[i + 1] == APP_10 || buffer[i + 1] == APP_11 /*|| buffer[i + 1] == APP_12*/ ||
							buffer[i + 1] == APP_13 || buffer[i + 1] == APP_14 || buffer[i + 1] == APP_15)
						{
							int length = (buffer[i + 2] << 8) + buffer[i + 3];

							i = i + length - 1;
						}
						else if (buffer[i + 1] == SOF_0 || buffer[i + 1] == SOF_1 || buffer[i + 1] == SOF_2 || buffer[i + 1] == SOF_3)
						{
							int length = (buffer[i + 2] << 8) + buffer[i + 3];

							i = i + length - 1;
						}
						else if (buffer[i + 1] == APP_0)
						{
							int length = (buffer[i + 2] << 8) + buffer[i + 3];

							i = i + length - 1;
						}
						else if (buffer[i + 1] == SOI || buffer[i + 1] == SOS)
						{
							i += 2;

							while (buffer[i] != 0xff || buffer[i + 1] != EOI)
							{
								i++;

								if (i == (int)buffer.Length - 1)
									throw new Exception(BIPStrings.ImageFileIsCorrupted_STR);
							}
						}
						else if (buffer[i + 1] == APP_12)
						{
							int length = (buffer[i + 2] << 8) + buffer[i + 3];
							string scanParams = "";

							for (int j = i + 4; j < i + length; j++)
								scanParams += (char)buffer[j];

							return scanParams;
						}
					}
				}

				return null;
			}
		}
		#endregion

		#region GetS2NScanParametersFromTiff()
		public static string GetS2NScanParametersFromTiff(FileInfo file)
		{
			Bitmap bmp = null;

			try
			{
				bmp = new Bitmap(file.FullName);

				if (bmp.PropertyIdList.Contains(0x131))
				{
					foreach (PropertyItem item in bmp.PropertyItems)
						if (item.Id == 0x131)
							return System.Text.Encoding.ASCII.GetString(item.Value);
				}
			}
			finally
			{
				if (bmp != null)
					bmp.Dispose();
			}

			return null;
		}
		#endregion
	}
}
