using System;
using System.Collections;
using System.IO;
using System.Drawing.Imaging;

namespace ImageProcessing.ImageFile
{
	/// <summary>
	/// Summary description for Gif.
	/// </summary>
	class Bmp
	{
		#region GetImageInfo()
		public static ImageInfo GetImageInfo(FileStream reader)
		{
			byte[]		buffer = new byte[40];
			
			reader.Position = 14;
			reader.Read(buffer, 0, 40);

			int	width = buffer[4] + (buffer[5] << 8) + (buffer[6] << 16) + (buffer[7] << 24);
			int height = buffer[8] + (buffer[9] << 8) + (buffer[10] << 16) + (buffer[11] << 24);
			int dpiH = buffer[24] + (buffer[25] << 8) + (buffer[26] << 16) + (buffer[27] << 24);
			int dpiV = buffer[28] + (buffer[29] << 8) + (buffer[30] << 16) + (buffer[31] << 24);
			int pixelDepth = buffer[14] + (buffer[15] << 8);
			bool isGrayscale = true;

			// getting info if palette is grayscale
			if (pixelDepth <= 8)
			{
				int colorsCount = buffer[32] + (buffer[33] << 8) + (buffer[34] << 16) + (buffer[35] << 24);

				if (colorsCount == 0)
					colorsCount = (int)Math.Pow(2, pixelDepth);

				byte[]	paletteBuffer = new byte[colorsCount * 4];
				
				reader.Position = 54;
				reader.Read(paletteBuffer, 0, colorsCount * 4);

				for (int j = 0; j < colorsCount; j++)
				{
					short blue = paletteBuffer[j * 4];
					short green = paletteBuffer[j * 4 + 1];
					short red = paletteBuffer[j * 4 + 2];

					if ((red != green) || (red != blue))
						isGrayscale = false;
				}
			}

			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, pixelDepth);

			return new ImageInfo(width, height, Convert.ToInt32(dpiH * 0.0254), Convert.ToInt32(dpiV * 0.0254), ImageInfo.GetPixelFormat(pixelDepth, isGrayscale), Encoding.GetCodecInfo(System.Drawing.Imaging.ImageFormat.Bmp), encoderParams);
		}
		#endregion

	}

}
