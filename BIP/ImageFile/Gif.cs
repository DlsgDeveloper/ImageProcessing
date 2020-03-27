using ImageProcessing.Languages;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;

namespace ImageProcessing.ImageFile
{
	/// <summary>
	/// Summary description for Gif.
	/// </summary>
	public class Gif
	{
		
		//PUBLIC METHODS
		#region public methods

		#region GetImageInfo()
		/// <summary>
		/// viz: http://www.w3.org/Graphics/GIF/spec-gif89a.txt, section 18
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static ImageInfo GetImageInfo(FileStream reader)
		{
			byte[]	buffer = new byte[13];
			bool isGrayscale = true;
			
			reader.Position = 0;
			reader.Read(buffer, 0, 13);

			int	width = (buffer[7] << 8) + buffer[6];
			int	height = (buffer[9] << 8) + buffer[8];
			int globalColorTableFlag = (buffer[10] >> 7) & 0x01;				// 1 if global color table exists; also if 1, background color exists
			int pixelDepth = ((buffer[10] >> 4) & 7) + 1;						// bits depth
			int sortFlag = (buffer[10] >> 3) & 0x01;							// 1 - if color table is sorted by frequency of colors, 0 - not sorted
			int globalColorTableSize = (int) Math.Pow(2, (buffer[10] & 7) + 1);	// size of color table
			int backgroundColorIndex = buffer[11];								// if 0, should be ignored
			int pixelAspectRatio = buffer[12];	

			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, pixelDepth);

			// to get grayscale information
			if (globalColorTableFlag > 0)
			{
				byte[] paletteBuffer = new byte[globalColorTableSize * 3];

				reader.Position = 13;
				reader.Read(paletteBuffer, 0, globalColorTableSize * 3);

				for (int j = 0; j < globalColorTableSize; j++)
				{
					short red = paletteBuffer[j * 3];
					short green = paletteBuffer[j * 3 + 1];
					short blue = paletteBuffer[j * 3 + 2];

					if ((red != green) || (red != blue))
						isGrayscale = false;
				}
			}

			PixelsFormat pixelsFormat = (isGrayscale) ? PixelsFormat.Format8bppGray : PixelsFormat.Format8bppIndexed;

			return new ImageInfo(width, height, 96, 96, pixelsFormat, Encoding.GetCodecInfo(System.Drawing.Imaging.ImageFormat.Gif), encoderParams);
		}
		#endregion

		#region GetMetadata()
		public static List<string> GetMetadata(FileInfo file)
		{
			System.Windows.Media.Imaging.GifBitmapDecoder gifDecoder = new System.Windows.Media.Imaging.GifBitmapDecoder(new Uri(file.FullName),
				System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.Default);

			System.Windows.Media.Imaging.BitmapMetadata metadata = gifDecoder.Metadata;

			if (metadata != null)
			{
				List<string> list = new List<string>();
				
				object md = metadata.GetQuery("/commenttext");
				list.Add( (string)md);

				return list;
			}
			else
			{
				using (FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					byte[] buffer = new byte[stream.Length];

					stream.Read(buffer, 0, (int)stream.Length);

					int		width = (buffer[7] << 8) + buffer[6];
					int		height = (buffer[9] << 8) + buffer[8];
					bool	globalColorTableFlag = ((buffer[10] & 0x80) > 0);				// true if global color table exists; also if 1, background color exists
					int		pixelDepth = ((buffer[10] >> 4) & 7) + 1;						// bits depth
					bool	sortFlag = ((buffer[10] & 0x08) > 0);							// true - if color table is sorted by frequency of colors, false - not sorted
					int		globalColorTableSize = (int)Math.Pow(2, (buffer[10] & 7) + 1);	// size of color table
					int		backgroundColorIndex = buffer[11];								// if 0, should be ignored
					int		pixelAspectRatio = buffer[12];									// A factor used to calculate an approximation of the aspect ratio of the pixel in the GIF image.

					int		paletteColorTableSize = (globalColorTableFlag) ? globalColorTableSize * 3 : 0;
					int		blockFirstByte = 6 + 7 + paletteColorTableSize;

					//while not end of file
					while (buffer[blockFirstByte] != 0x3B)
					{
						if (buffer[blockFirstByte] == 0x2c)
							blockFirstByte += GetLocalImageLength(buffer, blockFirstByte);
						else if (buffer[blockFirstByte] == 0x21)
						{
							//image
							if (buffer[blockFirstByte + 1] == 0x2C)
								blockFirstByte += GetImageBlockSize(buffer, blockFirstByte);
							//graphics control extension
							else if (buffer[blockFirstByte + 1] == 0xF9)
								blockFirstByte += 8;
							//plain text extension
							else if (buffer[blockFirstByte + 1] == 0x01)
								blockFirstByte += GetPlainTextBlockSize(buffer, blockFirstByte);
							//application block extension
							else if (buffer[blockFirstByte + 1] == 0xFF)
								blockFirstByte += GetApplicationBlockSize(buffer, blockFirstByte);
							//comment block extension
							else if (buffer[blockFirstByte + 1] == 0xFE)
							{
								List<string> comments = GetComments(buffer, ref blockFirstByte);

								return comments;
								//blockFirstByte += GetCommentBlockSize(buffer, blockFirstByte);
							}
							else
								throw new Exception(BIPStrings.ErrorInFile_STR+" '" + file.FullName + "'!");
						}
					}

					return new List<string>();
				}
			}
		}
		#endregion

		#region SaveMetadata()
		public static void SaveMetadata(FileInfo gifFile, List<string> textChunks)
		{
			using (FileStream stream = new FileStream(gifFile.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
			{
				int iDatIndex = GetTextChunkAvailableIndex(stream);

				byte[] chunks = GetFileChunks(textChunks);
				byte[] buffer = new byte[stream.Length - iDatIndex];

				stream.Seek(iDatIndex, SeekOrigin.Begin);
				stream.Read(buffer, 0, (int)(stream.Length - iDatIndex));

				stream.Seek(iDatIndex, SeekOrigin.Begin);
				stream.Write(chunks, 0, chunks.Length);

				stream.Seek(iDatIndex + chunks.Length, SeekOrigin.Begin);
				stream.Write(buffer, 0, buffer.Length);
			}
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region GetLocalImageLength()
		private static int GetLocalImageLength(byte[] buffer, int blockFirstByte)
		{
			byte[] imageDescriptor = new byte[10];
			ushort imageLeftPosition = (ushort)((buffer[blockFirstByte + 2] << 8) + buffer[blockFirstByte + 1]);
			ushort imageTopPosition = (ushort)((buffer[blockFirstByte + 4] << 8) + buffer[blockFirstByte + 3]);
			ushort imageWidth = (ushort)((buffer[blockFirstByte + 6] << 8) + buffer[blockFirstByte + 5]);
			ushort imageHeight = (ushort)((buffer[blockFirstByte + 8] << 8) + buffer[blockFirstByte + 7]);
			byte packetField = buffer[blockFirstByte + 9];

			bool localColorTableFlag = ((packetField & 0x80) > 0);
			bool interlaceFlag = ((packetField & 0x40) > 0);
			bool localSortFlag = ((packetField & 0x20) > 0);
			//Reserved = 2 bits
			int localColorTableColors = (int)Math.Pow(2, (packetField & 7) + 1);

			int localPaletteColorTableSize = (localColorTableFlag) ? localColorTableColors * 3 : 0;

			int blockLastByte = blockFirstByte + 10 + localPaletteColorTableSize;

			while(buffer[blockLastByte] != 0x00)
			{
				if (buffer[blockLastByte] == 0x21)
				{
					//image
					if (buffer[blockLastByte + 1] == 0x2C)
						blockLastByte += GetImageBlockSize(buffer, blockLastByte);
					//graphics control extension
					else if (buffer[blockLastByte + 1] == 0xF9)
						blockLastByte += 8;
					//plain text extension
					else if (buffer[blockLastByte + 1] == 0x01)
						blockLastByte += GetPlainTextBlockSize(buffer, blockLastByte);
					//application block extension
					else if (buffer[blockLastByte + 1] == 0xFF)
						blockLastByte += GetApplicationBlockSize(buffer, blockLastByte);
					//comment block extension
					else if (buffer[blockLastByte + 1] == 0xFE)
						blockLastByte += GetCommentBlockSize(buffer, blockLastByte);
					else
						throw new Exception(BIPStrings.GIFFileIsCorrupted_STR);
				}
				else
				{
					//image data: first is 1 byte - LZW Minimum Code Size, then blocks.
					//
					int lzwMinimumCodeSize = buffer[blockLastByte];

					blockLastByte++;
					while (buffer[blockLastByte] != 0x00)
					{
						int blockSize = buffer[blockLastByte];
						blockLastByte += blockSize + 1;
					}
				}
			}

			//terminating byte
			blockLastByte += 1;
			return blockLastByte - blockFirstByte;
		}
		#endregion

		#region GetApplicationBlockSize()
		private static int GetApplicationBlockSize(byte[] buffer, int blockFirstByte)
		{
			//header is 11 bytes long
			int dataFirstByte = blockFirstByte + 11;

			while (buffer[dataFirstByte] != 0)
			{
				dataFirstByte += buffer[dataFirstByte] + 1;
			}

			//terminating byte
			dataFirstByte += 1;

			return dataFirstByte - blockFirstByte;
		}
		#endregion

		#region GetCommentBlockSize()
		private static int GetCommentBlockSize(byte[] buffer, int blockFirstByte)
		{
			//header is 11 bytes long
			int dataFirstByte = blockFirstByte + 2;

			while (buffer[dataFirstByte] != 0)
			{
				dataFirstByte += buffer[dataFirstByte] + 1;
			}

			//terminating byte
			dataFirstByte += 1;

			return dataFirstByte - blockFirstByte;
		}
		#endregion

		#region GetPlainTextBlockSize()
		private static int GetPlainTextBlockSize(byte[] buffer, int blockFirstByte)
		{
			//header is 11 bytes long
			int dataFirstByte = blockFirstByte + 15;

			while (buffer[dataFirstByte] != 0)
			{
				dataFirstByte += buffer[dataFirstByte] + 1;
			}

			//terminating byte
			dataFirstByte += 1;

			return dataFirstByte - blockFirstByte;
		}
		#endregion

		#region GetImageBlockSize()
		private static int GetImageBlockSize(byte[] buffer, int blockFirstByte)
		{
			//header is 11 bytes long
			int dataFirstByte = blockFirstByte + 15;

			while (buffer[dataFirstByte] != 0)
			{
				dataFirstByte += buffer[dataFirstByte] + 1;
			}

			//terminating byte
			dataFirstByte += 1;

			return dataFirstByte - blockFirstByte;
		}
		#endregion

		#region GetComments()
		private static List<string> GetComments(byte[] buffer, ref int blockFirstByte)
		{
			List<string> comments = new List<string>();
			
			//header is 11 bytes long
			int dataFirstByte = blockFirstByte + 2;

			while (buffer[dataFirstByte] != 0)
			{
				int size = buffer[dataFirstByte];
				string comment = System.Text.Encoding.ASCII.GetString(buffer, dataFirstByte + 1, size);
				comments.Add(comment);
				
				dataFirstByte += buffer[dataFirstByte] + 1;
			}

			//terminating byte
			dataFirstByte += 1;

			return comments;
		}
		#endregion


		#region GetTextChunkAvailableIndex()
		static int GetTextChunkAvailableIndex(FileStream stream)
		{
			/*bool	cont = true;
			int		iDatIndex = -1;
			byte[]	buffer = new byte[1024];
			int		readFrom = 0;
			int		readCharacters;

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

			return iDatIndex - 4;*/

			return (int)(stream.Length - 1);
		}
		#endregion

		#region GetFileChunks()
		static byte[] GetFileChunks(List<string> textChunks)
		{
			List<byte[]> chunks = new List<byte[]>();
			int byteLength = 0;

			foreach (string textChunk in textChunks)
			{
				byte[] chunk = GetChunkByteArray(textChunk);

				byteLength += chunk.Length;

				chunks.Add(chunk);
			}

			byte[] result = new byte[byteLength + 3];
			int destinationIndex = 2;

			result[0] = 0x21;
			result[1] = 0xFE;
			result[result.Length - 1] = 0x00;

			foreach (byte[] chunk in chunks)
			{
				Array.Copy(chunk, 0, result, destinationIndex, chunk.Length);
				destinationIndex += chunk.Length;
			}

			return result;
		}
		#endregion

		#region GetChunkByteArray()
		static byte[] GetChunkByteArray(string textChunk)
		{
			if (textChunk.Length > 255)
				textChunk = textChunk.Substring(0, 255);

			byte[] chunkDataArray = System.Text.Encoding.ASCII.GetBytes(textChunk);
			byte[] chunk = new byte[chunkDataArray.Length + 1];

			chunk[0] = Convert.ToByte(chunkDataArray.Length);

			Array.Copy(chunkDataArray, 0, chunk, 1, chunkDataArray.Length);

			return chunk;
		}
		#endregion

		#endregion
	}

}
