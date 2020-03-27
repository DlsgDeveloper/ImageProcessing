using System;
using System.Collections;
using System.IO;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ImageProcessing.ImageFile
{
	/// <summary>
	/// Summary description for Gif.
	/// </summary>
	public class Tiff
	{
		//PUBLIC METHODS
		#region public methods

		#region TagName
		public enum TagName
		{
			NewSubfileType = 0x00FE,
			SubfileType = 0x00FF,
			ImageWidth = 0x0100,
			ImageLength = 0x0101,
			BitsPerSample = 0x0102,
			Compression = 0x0103,
			PhotometricInterpretation = 0x0106,
			Threshholding = 0x0107,
			CellWidth = 0x0108,
			CellLength = 0x0109,
			FillOrder = 0x010A,
			ImageDescription = 0x010E,
			Make = 0x010F,
			Model = 0x0110,
			StripOffsets = 0x0111,
			Orientation = 0x0112,
			SamplesPerPixel = 0x0115,
			RowsPerStrip = 0x0116,
			StripByteCounts = 0x0117,
			MinSampleValue = 0x0118,
			MaxSampleValue = 0x0119,
			XResolution = 0x011A,
			YResolution = 0x011B,
			PlanarConfiguration = 0x011C,
			FreeOffsets = 0x0120,
			FreeByteCounts = 0x0121,
			GrayResponseUnit = 0x0122,
			GrayResponseCurve = 0x0123,
			ResolutionUnit = 0x0128,
			Software = 0x0131,
			DateTime = 0x0132,
			Artist = 0x013B,
			HostComputer = 0x013C,
			ColorMap = 0x0140,
			ExtraSamples = 0x0152,

			Metadata = 0x02BC,

			Copyright = 0x8298,

			ExifOffset = 0x8769
		}
		#endregion

		#region FieldType
		public enum FieldType
		{
			Byte = 1,
			Ascii = 2,
			Int16 = 3,
			Int32 = 4,
			Rational = 5,
			Sbyte = 6,
			UndefByte = 7,
			SInt16 = 8,
			SInt32 = 9,
			SRational = 10,
			Float = 11,
			Double = 12
		}
		#endregion

		#region GetImageInfo()
		public static ImageInfo GetImageInfo(Stream reader)
		{
			int width = 0;
			int height = 0;
			int dpiH = 300;
			int dpiV = 300;
			double? dpiHDouble = null, dpiVDouble = null; 
			int dpiUnit = 0;
			int compression = 1;

			int bitsPerSample = 1;
			int samplesPerPixel = 3;
			bool isGrayscale = true;
			int photometricImplementation;

			byte[] buffer = new byte[8];
			byte[] dpiBuffer = new byte[8];

			reader.Position = 0;
			reader.Read(buffer, 0, 8);

			bool intelTiff = (buffer[0] == 0x49 && buffer[1] == 0x49);
			int offset = (int)ReadFromBuffer(buffer, 4, 4, intelTiff);

			reader.Position = offset;
			reader.Read(buffer, 0, 2);

			int numOfTags = (int)ReadFromBuffer(buffer, 0, 2, intelTiff);

			buffer = new byte[numOfTags * 12];
			reader.Position = offset + 2;
			reader.Read(buffer, 0, numOfTags * 12);

			for (int i = 0; i < numOfTags; i++)
			{
				int tagStart = i * 12;
				TagName tagName = (TagName)(ReadFromBuffer(buffer, tagStart, 2, intelTiff));
				FieldType fieldType = (FieldType)(ReadFromBuffer(buffer, tagStart + 2, 2, intelTiff));
				int valuesCount = (int)ReadFromBuffer(buffer, tagStart + 4, 4, intelTiff);

				switch (tagName)
				{
					case TagName.ImageWidth:
						{
							if (fieldType == FieldType.Int32)
								width = (int)ReadFromBuffer(buffer, tagStart + 8, 4, intelTiff);
							else
								width = (int)ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
					case TagName.ImageLength:
						{
							if (fieldType == FieldType.Int32)
								height = (int)ReadFromBuffer(buffer, tagStart + 8, 4, intelTiff);
							else
								height = (int)ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
					case TagName.BitsPerSample:
						{
							bitsPerSample = (int)ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
					case TagName.SamplesPerPixel:
						{
							samplesPerPixel = (int)ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
					case TagName.ResolutionUnit:
						{
							dpiUnit = (int)ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
					case TagName.XResolution:
						{
							reader.Position = ReadFromBuffer(buffer, tagStart + 8, 4, intelTiff);
							reader.Read(dpiBuffer, 0, 8);
							dpiHDouble = ReadFromBuffer(dpiBuffer, 0, 4, intelTiff) / (double)ReadFromBuffer(dpiBuffer, 4, 4, intelTiff);
							dpiH = Convert.ToInt32(dpiHDouble);
						} break;
					case TagName.YResolution:
						{
							reader.Position = ReadFromBuffer(buffer, tagStart + 8, 4, intelTiff);
							reader.Read(dpiBuffer, 0, 8);
							dpiVDouble = ReadFromBuffer(dpiBuffer, 0, 4, intelTiff) / (double)ReadFromBuffer(dpiBuffer, 4, 4, intelTiff);
							dpiV = Convert.ToInt32(dpiVDouble);
						} break;
					case TagName.Compression:
						{
							compression = (int)ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
					// getting info if palette is grayscale
					case TagName.ColorMap:
						{
							byte[] paletteBuffer = new byte[valuesCount * 2];
							int		colorsCount = valuesCount / 3;
							//int[]	paletteColors = new int[colorsCount];
							
							reader.Position = ReadFromBuffer(buffer, tagStart + 8, 4, intelTiff);
							reader.Read(paletteBuffer, 0, valuesCount * 2);

							for (int j = 0; j < colorsCount; j++)
							{
								short red = paletteBuffer[j * 2 + 1];
								short green = paletteBuffer[colorsCount * 2 + j * 2 + 1];
								short blue = paletteBuffer[colorsCount * 4 + j * 2 + 1];

								//paletteColors[j] = red << 16 + green << 8 + blue;

								if ((red != green) || (red != blue) || (red != j))
								{
									isGrayscale = false;
									break;
								}
							}
						} break;
					case TagName.PhotometricInterpretation:
						{
							photometricImplementation = (int)ReadFromBuffer(buffer, tagStart + 8, 2, intelTiff);
						} break;
#if DEBUG
					case TagName.Metadata:
						{
							byte[] metadataBuffer = new byte[valuesCount];
							reader.Position = ReadFromBuffer(buffer, tagStart + 8, 4, intelTiff);
							reader.Read(metadataBuffer, 0, valuesCount);
							
							string str = "";

							for (int j = 0; j < metadataBuffer.Length; j++)
							{
								if (metadataBuffer[j] != 0)
									str += (char)metadataBuffer[j];
								else
									break;
							}

							Console.WriteLine(str);

						} break;
#endif

				}

				tagStart += 12;
			}

			if (dpiUnit == 1)
			{
				if(dpiH < 2)
					dpiH = 96;
				if(dpiV < 2)
					dpiV = 96;
			}
			else if (dpiUnit == 3)
			{
				dpiH = Convert.ToInt32((dpiHDouble.HasValue) ? dpiHDouble.Value * 2.54 : dpiH * 2.54);
				dpiV = Convert.ToInt32((dpiVDouble.HasValue) ? dpiVDouble.Value * 2.54 : dpiV * 2.54);
			}

			int pixelDepth = (bitsPerSample * samplesPerPixel <= 8) ? (bitsPerSample * samplesPerPixel) : 24;

			EncoderParameters encoderParams = new EncoderParameters(2);
			encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, pixelDepth);

			switch (compression)
			{
				case 3: encoderParams.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionCCITT3); break;
				case 4: encoderParams.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionCCITT4); break;
				case 5: encoderParams.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW); break;
				case 32773: encoderParams.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionRle); break;
				default: encoderParams.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionNone); break;
			}

			return new ImageInfo(width, height, dpiH, dpiV, ImageInfo.GetPixelFormat(pixelDepth, isGrayscale), Encoding.GetCodecInfo(System.Drawing.Imaging.ImageFormat.Tiff), encoderParams);
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

		//PRIVATE METHODS
		#region private methods

		#region ReadFromBuffer()
		private static uint ReadFromBuffer(byte[] buffer, int offset, int length, bool intelTiff)
		{
			uint result = 0;

			if (intelTiff)
			{
				for (int i = 0; i < length; i++)
					result += (uint)(buffer[offset + i] << (i * 8));
			}
			else
			{
				for (int i = 0; i < length; i++)
					result += (uint)(buffer[offset + i] << ((length - i - 1) * 8));
			}

			return result;
		}
		#endregion

		#endregion


	}



}
