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
	public class Jfif
	{
		//
		// JPEG Marker codes
		//
		// Start of Frame markers, non-differential, Huffman coding

		static byte SOF_0 = 0xc0;    // Baseline DCT
		static byte SOF_1 = 0xc1;    // Extended sequential DCT
		static byte SOF_2 = 0xc2;    // Progressive DCT
		//static byte SOF_3 = 0xc3;    // Lossless (sequential)
		//static byte Huffman = 0xc4;    // Huffman Table

		static byte SOI = 0xd8;    // Start of Image
		static byte EOI = 0xd9;    // End of Image
		static byte SOS = 0xda;    // Start of Scan
		//static byte QT	= 0xdb;    // Quantization table

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
			reader.Position = 0;

			int width = 0;
			int height = 0;
			int dpiH = 72;
			int dpiV = 72;
			int pixelDepth = 3;
			bool dpiDefined = false;

			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 24L);

			reader.Position = 2;

			while (reader.ReadByte() == 0xFF)
			{
				byte marker = (byte)reader.ReadByte();
				int length = (reader.ReadByte() << 8) + reader.ReadByte();

				if (marker == APP_0)
				{
					reader.Position += 7;
					int dpiUnit = reader.ReadByte();						//0: none, 1: inches, 2: cm
					int tempDpiH = (reader.ReadByte() << 8) + reader.ReadByte();
					int tempDpiV = (reader.ReadByte() << 8) + reader.ReadByte();

					if (dpiUnit == 0)
					{
						if (dpiDefined == false)
						{
							dpiH = (tempDpiH == 1) ? 96 : tempDpiH;
							dpiV = (tempDpiV == 1) ? 96 : tempDpiV;
						}
					}
					else if (dpiUnit == 2)
					{
						dpiH = Convert.ToInt32(tempDpiH / 2.54);
						dpiV = Convert.ToInt32(tempDpiV / 2.54);
						dpiDefined = true;
					}
					else
					{
						dpiH = tempDpiH;
						dpiV = tempDpiV;
						dpiDefined = true;
					}

					reader.Position += (length - 2 - 12);
				}
				else if (marker == APP_1 || marker == APP_2 || marker == APP_3 || marker == APP_4 ||
					marker == APP_5 || marker == APP_6 || marker == APP_7 || marker == APP_8 ||
					marker == APP_9 || marker == APP_10 || marker == APP_11 || marker == APP_12 ||
					marker == APP_13 || marker == APP_14 || marker == APP_15)
				{
					reader.Position += (length - 2);
				}
				//else if (marker == SOF_0 || marker == SOF_1 || marker == SOF_2 || marker == SOF_3)
				else if (marker == SOF_0 || marker == SOF_2)
				{
					reader.Position++;
					height = (reader.ReadByte() << 8) + reader.ReadByte();
					width = (reader.ReadByte() << 8) + reader.ReadByte();
					pixelDepth = reader.ReadByte() * 8;

#if ! DEBUG
					PixelsFormat pixelsFormat;

					if (pixelDepth == 8)
						pixelsFormat = PixelsFormat.Format8bppGray;
					else
						pixelsFormat = PixelsFormat.Format24bppRgb;

					//pixelDepth = 0;
					return new ImageInfo(width, height, dpiH, dpiV, pixelsFormat, Encoding.GetCodecInfo(System.Drawing.Imaging.ImageFormat.Jpeg), encoderParams);
#endif
				}
				else if (marker == SOF_1)
				{
					reader.Position += (length - 2);
				}
				else if (marker == SOI)
				{
					//i += 2;

					//while (buffer[i] == 0xff)
					//{
					//	byte marker = buffer[i + 1];
					//	ushort chunkLength = (ushort)((buffer[i + 2] << 8) + buffer[i + 3]);

					//	if (marker == 0xc0)
					//	{
					//		int h = (ushort)((buffer[i + 4] << 8) + buffer[i + 5]);
					//		int w = (ushort)((buffer[i + 6] << 8) + buffer[i + 7]);
					//		System.Drawing.Size size = new System.Drawing.Size(w, h);
					//	}

					//	i += chunkLength + 2;
					//}

					reader.Position += (length - 2);
				}
				else if (marker == SOS)
				{
					reader.Position += (length - 2);

					try
					{
						while (reader.ReadByte() != 0xff || reader.ReadByte() != EOI)
						{
							//if (i == (int)buffer.Length - 1)
							//	throw new Exception(BIPStrings.ImageFileIsCorrupted_STR);
						}
					}
					catch (Exception)
					{
						throw new Exception("JFIF: " + BIPStrings.ImageFileIsCorrupted_STR);
					}
				}
				else if (marker == EOI)
				{
					throw new Exception(BIPStrings.ImageFileIsCorrupted_STR);
				}
				else if (marker == 0) // no mark
				{
					reader.Position++;
				}
				else
				{
					reader.Position += (length - 2);
				}
			}
#if DEBUG
			return new ImageInfo(width, height, dpiH, dpiV, PixelsFormat.Format24bppRgb, Encoding.GetCodecInfo(System.Drawing.Imaging.ImageFormat.Jpeg), encoderParams);
#endif
			throw new Exception(BIPStrings.CanTGetImagePropertiesFromFile_STR);
		}
		#endregion

		#region GetImageInfo()
		/*public static ImageInfo GetImageInfo(FileStream reader)
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

			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 24L);
			//encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, jpegCompression);

			int i = 2;


			while (i < (int)reader.Length)
			{
				if (buffer[i] == 0xFF)
				{
					if (buffer[i + 1] == APP_0)
					{
						int length = (buffer[i + 2] << 8) + buffer[i + 3];
						int dpiUnit = buffer[i + 11];
						dpiH = (buffer[i + 12] << 8) + buffer[i + 13];
						dpiV = (buffer[i + 14] << 8) + buffer[i + 15];

						if (dpiUnit == 2)
						{
							dpiH = Convert.ToInt32(dpiH / 2.54);
							dpiV = Convert.ToInt32(dpiV / 2.54);
						}
						
						i += length + 2;
					}
					else if (buffer[i + 1] == APP_1 || buffer[i + 1] == APP_2 || buffer[i + 1] == APP_3 || buffer[i + 1] == APP_4 ||
						buffer[i + 1] == APP_5 || buffer[i + 1] == APP_6 || buffer[i + 1] == APP_7 || buffer[i + 1] == APP_8 ||
						buffer[i + 1] == APP_9 || buffer[i + 1] == APP_10 || buffer[i + 1] == APP_11 || buffer[i + 1] == APP_12 ||
						buffer[i + 1] == APP_13 || buffer[i + 1] == APP_14 || buffer[i + 1] == APP_15)
					{
						int length = (buffer[i + 2] << 8) + buffer[i + 3];
						i += length + 2;
					}
					//else if (buffer[i + 1] == SOF_0 || buffer[i + 1] == SOF_1 || buffer[i + 1] == SOF_2 || buffer[i + 1] == SOF_3)
					else if (buffer[i + 1] == SOF_0 || buffer[i + 1] == SOF_2 )
					{
						height = (buffer[i + 5] << 8) + buffer[i + 6];
						width = (buffer[i + 7] << 8) + buffer[i + 8];
						pixelDepth = buffer[i + 9] * 8;
						PixelsFormat pixelsFormat;

						if (pixelDepth == 8)
							pixelsFormat = PixelsFormat.Format8bppGray;
						else
							pixelsFormat = PixelsFormat.Format24bppRgb;

						//pixelDepth = 0;
						return new ImageInfo(width, height, dpiH, dpiV, pixelsFormat, Encoding.GetCodecInfo(System.Drawing.Imaging.ImageFormat.Jpeg), encoderParams);
					}
					else if (buffer[i + 1] == SOF_1)
					{
						int length = (buffer[i + 2] << 8) + buffer[i + 3];
						i += length + 2;
					}
					else if (buffer[i + 1] == SOI)
					{
						//i += 2;

						//while (buffer[i] == 0xff)
						//{
						//	byte marker = buffer[i + 1];
						//	ushort chunkLength = (ushort)((buffer[i + 2] << 8) + buffer[i + 3]);

						//	if (marker == 0xc0)
						//	{
						//		int h = (ushort)((buffer[i + 4] << 8) + buffer[i + 5]);
						//		int w = (ushort)((buffer[i + 6] << 8) + buffer[i + 7]);
						//		System.Drawing.Size size = new System.Drawing.Size(w, h);
						//	}

						//	i += chunkLength + 2;
						//}
						
						int length = (buffer[i + 2] << 8) + buffer[i + 3];
						i += length + 2;
					}
					else if (buffer[i + 1] == SOS)
					{
						int length = (buffer[i + 2] << 8) + buffer[i + 3];
						i += length + 2;

						while (buffer[i] != 0xff || buffer[i + 1] != EOI)
						{
							i++;

							if (i == (int)buffer.Length - 1)
								throw new Exception(BIPStrings.ImageFileIsCorrupted_STR);
						}

						length = 0;
						
						//i += length + 2 + 14;
					}
					else if (buffer[i + 1] == EOI)
					{
						i += 2;
					}

					else if (buffer[i + 1] == 0) // no mark
					{
						i++;
					}
					else
					{
						int length = (buffer[i + 2] << 8) + buffer[i + 3];
						i += length + 2;
					}
				}
				else
				{
					i++;
				}
			}

			throw new Exception(BIPStrings.CanTGetImagePropertiesFromFile_STR);
		}*/
		#endregion

		#region GetImageInfo()
		/*public static ImageInfo GetImageInfo(FileStream reader)
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

			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 24L);
			//encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, jpegCompression);

			ImageInfo sof0 = null;
			ImageInfo sof1 = null;


			for (int i = 2; i < (int)reader.Length; i++)
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
					//else if (buffer[i + 1] == SOF_0 || buffer[i + 1] == SOF_1 || buffer[i + 1] == SOF_2 || buffer[i + 1] == SOF_3)
					else if (buffer[i + 1] == SOF_0 || buffer[i + 1] == SOF_2)
					{
						height = (buffer[i + 5] << 8) + buffer[i + 6];
						width = (buffer[i + 7] << 8) + buffer[i + 8];
						pixelDepth = buffer[i + 9] * 8;
						PixelsFormat pixelsFormat;

						if (pixelDepth == 8)
							pixelsFormat = PixelsFormat.Format8bppGray;
						else
							pixelsFormat = PixelsFormat.Format24bppRgb;

						//pixelDepth = 0;
						sof0 = new ImageInfo(width, height, dpiH, dpiV, pixelsFormat, Encoding.GetCodecInfo(System.Drawing.Imaging.ImageFormat.Jpeg), encoderParams);
					}
					else if (buffer[i + 1] == SOF_1)
					{
						height = (buffer[i + 5] << 8) + buffer[i + 6];
						width = (buffer[i + 7] << 8) + buffer[i + 8];
						pixelDepth = buffer[i + 9] * 8;
						PixelsFormat pixelsFormat;

						if (pixelDepth == 8)
							pixelsFormat = PixelsFormat.Format8bppGray;
						else
							pixelsFormat = PixelsFormat.Format24bppRgb;

						//pixelDepth = 0;
						sof1 = new ImageInfo(width, height, dpiH, dpiV, pixelsFormat, Encoding.GetCodecInfo(System.Drawing.Imaging.ImageFormat.Jpeg), encoderParams);
					}
					else if (buffer[i + 1] == APP_0)
					{
						int length = (buffer[i + 2] << 8) + buffer[i + 3];
						int		dpiUnit = buffer[i + 11];
						dpiH = (buffer[i + 12] << 8) + buffer[i + 13];
						dpiV = (buffer[i + 14] << 8) + buffer[i + 15];

						if (dpiUnit == 2)
						{
							dpiH = Convert.ToInt32(dpiH / 2.54);
							dpiV = Convert.ToInt32(dpiV / 2.54);
						}
					}
					else if (buffer[i + 1] == SOI)
					{
						i += 2;

						while (buffer[i] == 0xff)
						{
							byte marker = buffer[i + 1];
							ushort chunkLength = (ushort)((buffer[i + 2] << 8) + buffer[i + 3]);

							if (marker == 0xc0)
							{
								int h = (ushort)((buffer[i + 4] << 8) + buffer[i + 5]);
								int w = (ushort)((buffer[i + 6] << 8) + buffer[i + 7]);
								System.Drawing.Size size = new System.Drawing.Size(w, h);
							}

							i += chunkLength + 2;
						}

						while (buffer[i] != 0xff || buffer[i + 1] != EOI)
						{
							i++;

							if (i == (int)buffer.Length - 1)
								throw new Exception(BIPStrings.ImageFileIsCorrupted_STR);
						}
					}
					else if ( buffer[i + 1] == SOS)
					{
						int length = (buffer[i + 2] << 8) + buffer[i + 3];

						while (buffer[i] != 0xff || buffer[i + 1] != EOI)
						{
							i++;

							if (i == (int)buffer.Length - 1)
								throw new Exception(BIPStrings.ImageFileIsCorrupted_STR);
						}
					}
				}
			}

			if (sof0 != null)
				return sof0;
			if (sof1 != null)
				return sof1;

			throw new Exception(BIPStrings.CanTGetImagePropertiesFromFile_STR);
		}*/
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

	}

}
