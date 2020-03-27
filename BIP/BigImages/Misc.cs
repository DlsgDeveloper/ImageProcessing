using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageProcessing.BigImages
{
	/*public class Misc
	{
		public static int minBufferSize = (int)Math.Pow(2, 25);
		public static int bufferSize = (int)Math.Pow(2, 27);
		
		#region GetStripHeightMax()
		public static int GetStripHeightMax(ItDecoder itDecoder)
		{
			return (int)(bufferSize / (itDecoder.Width * BytesPerPixel(itDecoder.PixelsFormat))) / 8 * 8;
		}

		public static int GetStripHeightMax(ItDecoder itDecoder, double zoom)
		{
			if(zoom <= 1)
				return GetStripHeightMax(itDecoder);
			else
				return (int)(bufferSize / (itDecoder.Width * BytesPerPixel(itDecoder.PixelsFormat) * zoom * zoom)) / 8 * 8;
		}

		public static int GetStripHeightMax(ItDecoder itDecoder, ItEncoder itEncoder)
		{
			int decoderStripHeight = (int)(bufferSize / (itDecoder.Width * BytesPerPixel(itDecoder.PixelsFormat))) / 8 * 8;
			int encoderStripHeight = (int)(bufferSize / (itEncoder.Width * BytesPerPixel(itEncoder.PixelsFormat))) / 8 * 8;

			return Math.Max(decoderStripHeight, encoderStripHeight);
		}
		#endregion

		#region BytesPerPixel()
		private static float BytesPerPixel(PixelsFormat pixelsFormat)
		{
			switch (pixelsFormat)
			{
				case PixelsFormat.Format8bppGray: return 1;
				case PixelsFormat.Format8bppIndexed: return 1;
				case PixelsFormat.FormatBlackWhite: return 0.125F;
				case PixelsFormat.Format24bppRgb: return 3;
				case PixelsFormat.Format32bppRgb: return 4;
				case PixelsFormat.Format4bppGray: return .5F;
				default: return 3;
			}
		}
		#endregion

	}*/
}
