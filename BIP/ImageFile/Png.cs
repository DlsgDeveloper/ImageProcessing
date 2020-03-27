using System;
using System.Collections;
using System.IO;
using System.Drawing.Imaging;
using System.Collections.Generic;
using ImageProcessing.Languages;

namespace ImageProcessing.ImageFile
{
	/// <summary>
	/// Summary description for Gif.
	/// </summary>
	public class Png
	{

		//PUBLIC METHODS
		#region public methods

		#region GetImageInfo()
		public static ImageInfo GetImageInfo(FileStream reader)
		{
			byte[] buffer = new byte[26];
			reader.Position = 0;
			reader.Read(buffer, 0, 26);

			string tagName;
			int dpiH = 96;
			int dpiV = 96;
			int width = (buffer[16] << 24) + (buffer[17] << 16) + (buffer[18] << 8) + buffer[19];
			int height = (buffer[20] << 24) + (buffer[21] << 16) + (buffer[22] << 8) + buffer[23];
			int pixelDepth = buffer[24];			
			int colorType = buffer[25];
			bool isGrayscalePalette = true;

			switch (colorType)
			{
				case 0: break;
				case 2: pixelDepth = pixelDepth * 3; break;
				case 3: break;
				case 4: pixelDepth = pixelDepth * 2; break;
				case 6: pixelDepth = pixelDepth * 4; break;
			}
			
			int		tagStart = 8;
			int		tagLength;
			
			do
			{
				reader.Position = tagStart;
				reader.Read(buffer, 0, 8);
				
				tagLength = (buffer[0] << 24) + (buffer[1] << 16) + (buffer[2] << 8) + buffer[3];
				tagName = String.Format("{0}{1}{2}{3}", new object[] { Convert.ToChar(buffer[4]), Convert.ToChar(buffer[5]), Convert.ToChar(buffer[6]), Convert.ToChar(buffer[7]) });

				if (tagName.ToUpper() == "IHDR" || tagName.ToUpper() == "PHYS" || tagName.ToUpper() == "PLTE" || tagName.ToUpper() == "TRNS")
				{
					byte[] tagBuffer = new byte[tagLength];
					reader.Read(tagBuffer, 0, tagLength);

					if (tagName.ToUpper() == "IHDR")
					{
						int ihdrWidth = (tagBuffer[0] << 24) + (tagBuffer[1] << 16) + (tagBuffer[2] << 8) + tagBuffer[3];
						int ihdrHeight = (tagBuffer[4] << 24) + (tagBuffer[5] << 16) + (tagBuffer[6] << 8) + tagBuffer[7];
						int ihdrBitDepth = tagBuffer[8];
						int ihdrColorType = tagBuffer[9];
						int ihdrCompressionMethod = tagBuffer[10];
						int ihdrFilterMethod = tagBuffer[11];
						int ihdrInterlaceMethod = tagBuffer[12];
					}
					else if (tagName.ToUpper() == "PHYS")
					{
						dpiH = (tagBuffer[0] << 24) + (tagBuffer[1] << 16) + (tagBuffer[2] << 8) + tagBuffer[3];
						dpiV = (tagBuffer[4] << 24) + (tagBuffer[5] << 16) + (tagBuffer[6] << 8) + tagBuffer[7];
						dpiH = Convert.ToInt32(dpiH * 0.0254);
						dpiV = Convert.ToInt32(dpiV * 0.0254);
					}
					// getting info if palette is grayscale
					else if (tagName.ToUpper() == "PLTE")
					{
						int colorsCount = tagLength / 3;
						//int[] paletteColors = new int[colorsCount];

						for (int i = 0; i < colorsCount; i++)
						{
							//paletteColors[i] = tagBuffer[i * 3] << 16 + tagBuffer[i * 3 + 1] << 8 + tagBuffer[i * 3 + 2];

							if ((tagBuffer[i * 3] != tagBuffer[i * 3 + 1]) || (tagBuffer[i * 3] != tagBuffer[i * 3 + 2]))
								isGrayscalePalette = false;
						}
					}
					// transparrency
					else if (tagName.ToUpper() == "TRNS")
					{
						int[] alphas = new int[tagLength];

						for (int i = 0; i < tagLength; i++)
							alphas[i] = tagBuffer[i];
					}
				}

				tagStart += tagLength + 12;
			}
			while (tagName != "IEND" && tagStart < (int)reader.Length);

			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, pixelDepth);

			return new ImageInfo(width, height, dpiH, dpiV, ImageInfo.GetPixelFormat(pixelDepth, isGrayscalePalette), Encoding.GetCodecInfo(System.Drawing.Imaging.ImageFormat.Png), encoderParams);
		}
		#endregion

		#region GetMetadata()
		/// <summary>
		/// returns hashtable of pairs <string key, string value>
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static Hashtable GetMetadata(FileInfo file)
		{
			using (FileStream reader = new FileStream(file.FullName, FileMode.Open))
			{
				byte[] buffer = new byte[26];
				reader.Position = 0;
				reader.Read(buffer, 0, 26);
				Hashtable metadata = new Hashtable();

				string tagName;
				int tagStart = 8;
				int tagLength;

				do
				{
					reader.Position = tagStart;
					reader.Read(buffer, 0, 8);

					tagLength = (buffer[0] << 24) + (buffer[1] << 16) + (buffer[2] << 8) + buffer[3];
					tagName = String.Format("{0}{1}{2}{3}", new object[] { Convert.ToChar(buffer[4]), Convert.ToChar(buffer[5]), Convert.ToChar(buffer[6]), Convert.ToChar(buffer[7]) });

					if (tagName == "tEXt")
					{
						byte[] tagBuffer = new byte[tagLength];
						reader.Read(tagBuffer, 0, tagLength);
						int indexOfNull = Array.IndexOf(tagBuffer, (byte)0);

						if (indexOfNull > 0)
						{
							byte[] b1 = new byte[indexOfNull];
							byte[] b2 = new byte[tagBuffer.Length - indexOfNull - 1];

							Buffer.BlockCopy(tagBuffer, 0, b1, 0, b1.Length);
							Buffer.BlockCopy(tagBuffer, indexOfNull + 1, b2, 0, b2.Length);

							System.Text.StringBuilder sb = new System.Text.StringBuilder();

							string name = System.Text.Encoding.ASCII.GetString(b1);
							string value = System.Text.Encoding.ASCII.GetString(b2);

							metadata.Add(name, value);
						}

					}

					tagStart += tagLength + 12;
				}
				while (tagName != "IEND" && tagStart < (int)reader.Length);

				return metadata;
			}
		}
		#endregion

		#region SaveMetadata()
		public static void SaveMetadata(FileInfo pngFile, List<BIP.Metadata.PngChunks.tEXtChunk> textChunks)
		{
			using (FileStream stream = new FileStream(pngFile.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
			{
				int iDatIndex = GetIDATindex(stream);

				if (iDatIndex > 0)
				{
					byte[] chunks = GetFileChunks(textChunks);
					byte[] buffer = new byte[stream.Length - iDatIndex];

					stream.Seek(iDatIndex, SeekOrigin.Begin);
					stream.Read(buffer, 0, (int)(stream.Length - iDatIndex));

					stream.Seek(iDatIndex, SeekOrigin.Begin);
					stream.Write(chunks, 0, chunks.Length);

					stream.Seek(iDatIndex + chunks.Length, SeekOrigin.Begin);
					stream.Write(buffer, 0, buffer.Length);
				}
				else
					throw new Exception(BIPStrings.CanTFindIDATChunkTheFile_STR+" '" + pngFile.FullName + "' "+BIPStrings.IsEitherCorruptedOrItIsNotPNGFile_STR);
			}
		}
		#endregion

		#endregion
	
		//PRIVATE METHODS
		#region private methods

		#region GetIDATindex()
		static int GetIDATindex(FileStream stream)
		{
			bool cont = true;
			int iDatIndex = -1;
			byte[] buffer = new byte[1024];
			int readFrom = 0;
			int readCharacters;

			while (cont && (readCharacters = stream.Read(buffer, (int)readFrom, 1024)) > 0)
			{
				for (int i = 0; i <= readCharacters - 4; i++)
				{
					if (buffer[i] == 'I' && buffer[i + 1] == 'D' && buffer[i + 2] == 'A' && buffer[i + 3] == 'T')
					{
						cont = false;
						iDatIndex = readFrom + i;
						break;
					}
				}

				readFrom += 1020;
			}

			return iDatIndex - 4;
		}
		#endregion

		#region GetFileChunks()
		static byte[] GetFileChunks(List<BIP.Metadata.PngChunks.tEXtChunk> textChunks)
		{
			List<byte[]> chunks = new List<byte[]>();
			int byteLength = 0;

			foreach (BIP.Metadata.PngChunks.tEXtChunk textChunk in textChunks)
			{
				byte[] chunk = GetChunkByteArray(textChunk);

				byteLength += chunk.Length;

				chunks.Add(chunk);
			}

			byte[] result = new byte[byteLength];
			int destinationIndex = 0;

			foreach (byte[] chunk in chunks)
			{
				Array.Copy(chunk, 0, result, destinationIndex, chunk.Length);
				destinationIndex += chunk.Length;
			}

			return result;
		}
		#endregion

		#region GetChunkByteArray()
		static byte[] GetChunkByteArray(BIP.Metadata.PngChunks.tEXtChunk textChunk)
		{
			byte[] chunkNameArray = System.Text.Encoding.ASCII.GetBytes("tEXt");
			byte[] chunkTypeArray = System.Text.Encoding.ASCII.GetBytes(textChunk.ChunkType.ToString());
			byte[] chunkDataArray = System.Text.Encoding.ASCII.GetBytes(textChunk.ChunkData);
			byte[] dataArray = new byte[chunkTypeArray.Length + chunkDataArray.Length + 1];

			Array.Copy(chunkTypeArray, 0, dataArray, 0, chunkTypeArray.Length);
			dataArray[chunkTypeArray.Length] = 0;
			Array.Copy(chunkDataArray, 0, dataArray, chunkTypeArray.Length + 1, chunkDataArray.Length);

			int byteLength = 4 + 4 + dataArray.Length + 4;
			byte[] chunk = new byte[byteLength];

			byte[] arrayForCrc = new byte[4 + dataArray.Length];
			Array.Copy(chunkNameArray, 0, arrayForCrc, 0, 4);
			Array.Copy(dataArray, 0, arrayForCrc, 4, dataArray.Length);
			byte[] crc = GetCrc(arrayForCrc);

			chunk[0] = (byte)((dataArray.Length >> 24) & 0xFF);
			chunk[1] = (byte)((dataArray.Length >> 16) & 0xFF);
			chunk[2] = (byte)((dataArray.Length >> 8) & 0xFF);
			chunk[3] = (byte)((dataArray.Length & 0xFF));

			//Array.Copy(BitConverter.GetBytes((Int32)byteLength), 0, chunk, 0, 4);
			Array.Copy(chunkNameArray, 0, chunk, 4, 4);
			Array.Copy(dataArray, 0, chunk, 8, dataArray.Length);
			Array.Copy(crc, 0, chunk, chunk.Length - 4, 4);

			return chunk;
		}
		#endregion

		#region GetCrcTable()
		/// <summary>
		/// Calculates an array of 4 bytes containing the calculated CRC.
		/// </summary>
		/// <param name="buf">The raw data on which to calculate the CRC.</param>
		public static byte[] GetCrc(byte[] buffer)
		{
			uint data = 0xFFFFFFFF;
			int n;
			uint[] crcTable = GetCrcTable();

			for (n = 0; n < buffer.Length; n++)
				data = crcTable[(data ^ buffer[n]) & 0xff] ^ (data >> 8);

			data = data ^ 0xFFFFFFFF;

			byte b1 = Convert.ToByte(data >> 24);
			byte b2 = Convert.ToByte(b1 << 8 ^ data >> 16);
			byte b3 = Convert.ToByte(((data >> 16 << 16) ^ (data >> 8 << 8)) >> 8);
			byte b4 = Convert.ToByte((data >> 8 << 8) ^ data);

			return new byte[] { b1, b2, b3, b4 };
		}
		#endregion

		#region GetCrcTable()
		/// <summary>
		/// Creates the CRC table for calculating a 32-bit CRC.
		/// </summary>
		private static uint[] GetCrcTable()
		{
			uint c;
			int k;
			int n;
			uint[] table = new uint[256];

			for (n = 0; n < 256; n++)
			{
				c = (uint)n;

				for (k = 0; k < 8; k++)
				{
					if ((c & 1) == 1)
					{
						c = 0xedb88320 ^ (c >> 1);
					}
					else
					{
						c = c >> 1;
					}
				}
				table[n] = c;
			}

			return table;
		}
		#endregion

		#endregion

	}
}
