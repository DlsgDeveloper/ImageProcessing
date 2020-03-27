using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BIP.Metadata
{
	public class PngChunks
	{

		#region enum tEXtChunkType
		public enum tEXtChunkType
		{
			Title,
			Author,
			Description,
			Copyright,
			CreationTime,
			Software,
			Disclaimer,
			Warning,
			Source,
			Comment
		}
		#endregion

		#region class tEXtChunk
		public class tEXtChunk
		{
			public readonly tEXtChunkType	ChunkType;
			public readonly string			ChunkData;

			public tEXtChunk(tEXtChunkType chunkType, string chunkData)
			{
				this.ChunkType = chunkType;
				this.ChunkData = chunkData;
			}
		}
		#endregion

	}
}
