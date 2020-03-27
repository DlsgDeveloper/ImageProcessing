using System;
using System.Collections;
using System.IO;
using System.Drawing.Imaging;
using ImageProcessing.Languages;

namespace ImageProcessing.ImageFile
{
	/// <summary>
	/// Summary description for JFIF.
	/// </summary>
	public class Exif
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


		// PUBLIC METHODS
		#region public methods

		#region GetImageInfo()
		public static ImageInfo GetImageInfo(FileStream reader)
		{
			byte[] buffer = new byte[reader.Length];
			reader.Position = 0;
			reader.Read(buffer, 0, (int)reader.Length);
			reader.Position = 0;

			/*int width = 0;
			int height = 0;
			int dpiH = 72;
			int dpiV = 72;
			int pixelDepth = 3;*/

			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 24L);
			//encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, jpegCompression);


			for (int i = 2; i < (int)reader.Length; i++)
			{
				if (buffer[i] == 0xFF)
				{
					if (buffer[i + 1] == APP_1)
					{
						//tiffHeader
						int		tiffHeaderLength = (buffer[i + 2] << 8) + buffer[i + 3];

						ImageInfo mainInfo = FromTiffTags(reader, 12);

						//IDF0
						ImageInfo idfInfo = FromMainJpegImage(reader, tiffHeaderLength + 2);

						mainInfo = new ImageInfo(idfInfo.Width, idfInfo.Height, mainInfo.DpiH, mainInfo.DpiV, idfInfo.PixelsFormat,
							mainInfo.CodecInfo, mainInfo.EncoderParameters);

						return idfInfo;
					}

					if (buffer[i + 1] == SOI || buffer[i + 1] == SOS)
					{
						i += 2;

						while (buffer[i] != 0xff || buffer[i + 1] != EOI)
						{
							i++;

							if (i == (int)buffer.Length - 1)
								throw new Exception(BIPStrings.ImageFileIsCorrupted_STR);
						}
					}
				}
			}

			throw new Exception("Can\'t get image properties from file!");
		}
		#endregion

		#region GetMetadata()
		public unsafe static BIP.Metadata.ExifMetadata GetMetadata(string file)
		{
			ImageComponent.ImageDecoder decoder = new ImageComponent.ImageDecoder(file);
			ImageComponent.Metadata.ExifMetadata exif = decoder.GetTiffMetadata();

			return new BIP.Metadata.ExifMetadata(exif);
		}
		#endregion

		#endregion




		// PRIVATE METHODS
		#region private methods

		#region FromTiffTags()
		private static ImageInfo FromTiffTags(FileStream reader, int firstByte)
		{
			int width = 0;
			int height = 0;
			int dpiH = 300;
			int dpiV = 300;
			double? dpiHDouble = null, dpiVDouble = null;
			int dpiUnit = 0;
			int compression = 1;
			PixelsFormat pixelsFormat = PixelsFormat.Format24bppRgb;

			int bitsPerSample = 1;
			int samplesPerPixel = 3;

			byte[] buffer = new byte[8];
			byte[] dpiBuffer = new byte[8];

			reader.Position = firstByte;
			reader.Read(buffer, 0, 8);

			bool intelTiff = (buffer[0] == 0x49 && buffer[1] == 0x49);
			int offset = ReadFromBuffer(buffer, 4, 4, intelTiff);

			reader.Position = firstByte + offset;
			reader.Read(buffer, 0, 2);

			int numOfTags = ReadFromBuffer(buffer, 0, 2, intelTiff);

			buffer = new byte[numOfTags * 12];
			reader.Position = firstByte + offset + 2;
			reader.Read(buffer, 0, numOfTags * 12);

			for (int i = 0; i < numOfTags; i++)
			{
				int tagStart = i * 12;
				Tiff.TagName tagName = (Tiff.TagName)(ReadFromBuffer(buffer, tagStart, 2, intelTiff));
				Tiff.FieldType fieldType = (Tiff.FieldType)(ReadFromBuffer(buffer, tagStart + 2, 2, intelTiff));

				switch (tagName)
				{
					case Tiff.TagName.ImageWidth:
						{
							if (fieldType == Tiff.FieldType.Int32)
								width = ReadFromBuffer(buffer, tagStart + 8, 4, intelTiff);
							else
								width = ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
					case Tiff.TagName.ImageLength:
						{
							if (fieldType == Tiff.FieldType.Int32)
								height = ReadFromBuffer(buffer, tagStart + 8, 4, intelTiff);
							else
								height = ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
					case Tiff.TagName.BitsPerSample:
						{
							bitsPerSample = ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
					case Tiff.TagName.SamplesPerPixel:
						{
							samplesPerPixel = ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
					case Tiff.TagName.ResolutionUnit:
						{
							dpiUnit = ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
					case Tiff.TagName.XResolution:
						{
							reader.Position = ReadFromBuffer(buffer, tagStart + 8, 4, intelTiff) + firstByte;
							reader.Read(dpiBuffer, 0, 8);
							dpiHDouble = ReadFromBuffer(dpiBuffer, 0, 4, intelTiff) / (double)ReadFromBuffer(dpiBuffer, 4, 4, intelTiff);
							dpiH = Convert.ToInt32(dpiHDouble);
						} break;
					case Tiff.TagName.YResolution:
						{
							reader.Position = ReadFromBuffer(buffer, tagStart + 8, 4, intelTiff) + firstByte;
							reader.Read(dpiBuffer, 0, 8);
							dpiVDouble = ReadFromBuffer(dpiBuffer, 0, 4, intelTiff) / (double)ReadFromBuffer(dpiBuffer, 4, 4, intelTiff);
							dpiV = Convert.ToInt32(dpiVDouble);
						} break;
					case Tiff.TagName.Compression:
						{
							compression = ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
					case Tiff.TagName.ExifOffset:
						{
							//to add implementation of reading Sub IDF
							
							
							//int position = ReadFromBuffer(buffer, tagStart + 8, 4, intelTiff) + firstByte;

							//ImageInfo info = ReadSubIdf(reader, (int)position);

						} break;
				}

				tagStart += 12;
			}

			//(the value 1 is not standard EXIF) 
			//1 = None
			//2 = inches
			//3 = cm
			/*if (dpiUnit == 1)
			{
				dpiH = Convert.ToInt32(dpiH * 2.54);
				dpiV = Convert.ToInt32(dpiV * 2.54);
			}*/
			if (dpiUnit == 1)
			{
				if (dpiH < 2)
					dpiH = 96;
				if (dpiV < 2)
					dpiV = 96;
			}
			else if (dpiUnit == 3)
			{
				dpiH = Convert.ToInt32((dpiHDouble.HasValue) ? dpiHDouble.Value * 2.54 : dpiH * 2.54);
				dpiV = Convert.ToInt32((dpiVDouble.HasValue) ? dpiVDouble.Value * 2.54 : dpiV * 2.54);
			}

			int pixelDepth = (bitsPerSample * samplesPerPixel <= 8) ? (bitsPerSample * samplesPerPixel) : 24;

			if (pixelDepth == 8)
				pixelsFormat = PixelsFormat.Format8bppGray;
			else
				pixelsFormat = PixelsFormat.Format24bppRgb;

			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, pixelDepth);

			return new ImageInfo(width, height, dpiH, dpiV, pixelsFormat, Encoding.GetCodecInfo(System.Drawing.Imaging.ImageFormat.Jpeg), encoderParams);
		}
		#endregion

		#region ReadFromBuffer()
		private static int ReadFromBuffer(byte[] buffer, int offset, int length, bool intelTiff)
		{
			int result = 0;

			if (intelTiff)
			{
				for (int i = 0; i < length; i++)
					result += buffer[offset + i] << (i * 8);
			}
			else
			{
				for (int i = 0; i < length; i++)
					result += buffer[offset + i] << ((length - i - 1) * 8);
			}

			return result;
		}
		#endregion

		#region Read()
		/*private static byte[] Read(byte[] buffer, int offset, int length)
		{
			byte[] copy = new byte[length];

			for (int i = 0; i < length; i++)
				copy[i] = buffer[i + offset];

			return copy;
		}*/
		#endregion

		#region FromMainJpegImage()
		private static ImageInfo FromMainJpegImage(FileStream reader, int firstByte)
		{
			byte[] buffer = new byte[reader.Length];
			reader.Position = 0;
			reader.Read(buffer, 0, (int)reader.Length);
			reader.Position = 0;

			int width = 0;
			int height = 0;
			int dpiH = 72;
			int dpiV = 72;
			int pixelDepth = 3;
			PixelsFormat pixelsFormat = PixelsFormat.Format24bppRgb;

			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 24L);
			//encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, jpegCompression);


			for (int i = 2 + firstByte; i < (int)reader.Length; i++)
			{
				if (buffer[i] == 0xFF)
				{
					if (buffer[i + 1] == APP_1 || buffer[i + 1] == APP_2 || buffer[i + 1] == APP_3 || buffer[i + 1] == APP_4 ||
						buffer[i + 1] == APP_5 || buffer[i + 1] == APP_6 || buffer[i + 1] == APP_7 || buffer[i + 1] == APP_8 ||
						buffer[i + 1] == APP_9 || buffer[i + 1] == APP_10 || buffer[i + 1] == APP_11 || buffer[i + 1] == APP_12 ||
						buffer[i + 1] == APP_13 || buffer[i + 1] == APP_14 || buffer[i + 1] == APP_15)
					{
						int length = (buffer[i + 2] << 8) + buffer[i + 3];

						//i = i + length - 1;
					}
					if (buffer[i + 1] == SOF_0 || buffer[i + 1] == SOF_1 || buffer[i + 1] == SOF_2 || buffer[i + 1] == SOF_3)
					{
						height = (buffer[i + 5] << 8) + buffer[i + 6];
						width = (buffer[i + 7] << 8) + buffer[i + 8];
						pixelDepth = buffer[i + 9] * 8;

						if (pixelDepth == 8)
							pixelsFormat = PixelsFormat.Format8bppGray;
						else
							pixelsFormat = PixelsFormat.Format24bppRgb;
						
						//pixelDepth = 0;
						return new ImageInfo(width, height, dpiH, dpiV, pixelsFormat, Encoding.GetCodecInfo(System.Drawing.Imaging.ImageFormat.Jpeg), encoderParams);
					}
					if (buffer[i + 1] == APP_0)
					{
						int dpiUnit = buffer[i + 11];
						dpiH = (buffer[i + 12] << 8) + buffer[i + 13];
						dpiV = (buffer[i + 14] << 8) + buffer[i + 15];

						if (dpiUnit == 2)
						{
							dpiH = Convert.ToInt32(dpiH / 2.54);
							dpiV = Convert.ToInt32(dpiV / 2.54);
						}
					}
					if (buffer[i + 1] == SOI || buffer[i + 1] == SOS)
					{
						/*i += 2;

						while (buffer[i] != 0xff || buffer[i + 1] != EOI)
						{
							i++;

							if (i == (int)buffer.Length - 1)
								throw new Exception("Image file is corrupted!");
						}*/
					}
					if (buffer[i + 1] == EOI)
					{
						break;
					}
				}
			}

			throw new Exception(BIPStrings.CanTGetImagePropertiesFromFile_STR);
		}
		#endregion


		#endregion



	}

}
