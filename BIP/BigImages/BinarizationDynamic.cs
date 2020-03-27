using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;

namespace ImageProcessing.BigImages
{
	public partial class Binarization
	{

		#region neighbourArrayX
		private int[] neighbourArrayX = new int[]
		{
			1 	,		//0
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			3 	,		
			3 	,		
			3 	,		
			3 	,		
			3 	,		//20
			23	,		
			23	,		
			23	,		
			43	,		
			41	,		
			41	,		
			35	,		
			37	,		
			37	,		
			35	,		
			33	,		
			33	,		
			29	,		
			29	,		
			31	,		
			27	,		
			21	,		
			19	,		
			19	,		
			15	,		//40
			19	,		
			21	,		
			21	,		
			23	,		
			31	,		
			35	,		
			39	,		
			33	,		
			39	,		
			41	,		
			41	,		
			43	,		
			45	,		
			43	,		
			43	,		
			43	,		
			39	,		
			39	,		
			39	,		
			39	,		//60
			39	,		
			39	,		
			39	,		
			41	,		
			39	,		
			39	,		
			45	,		
			47	,		
			45	,		
			45	,		
			45	,		
			49	,		
			51	,		
			25	,		
			25	,		
			21	,		
			21	,		
			23	,		
			21	,		
			19	,		//80
			19	,		
			19	,		
			19	,		
			17	,		
			17	,		
			17	,		
			17	,		
			17	,		
			15	,		
			17	,		
			17	,		
			19	,		
			19	,		
			15	,		
			15	,		
			15	,		
			15	,		
			15	,		
			15	,		
			15	,		//100
			15	,		
			11	,		
			11	,		
			9 	,		
			9 	,		
			9 	,		
			9 	,		
			9 	,		
			9 	,		
			11	,		
			9 	,		
			5 	,		
			9 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		//120
			5 	,		
			9 	,		
			9 	,		
			7 	,		
			7 	,		
			7 	,		
			7 	,		
			7 	,		
			7 	,		
			5 	,		
			7 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		//140
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			3 	,		
			3 	,		
			3 	,		
			3 	,		
			3 	,		
			3 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		//160
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,			//180
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,			//200
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,			//220
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,			//240
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,	
		};
		#endregion

		#region thresArrayX
		private int[] thresArrayX = new int[]
		{
			1  	,	//0
			1  	,	
			1  	,	
			1  	,	
			1  	,	
			1  	,	
			1  	,	
			1  	,	
			1  	,	
			1  	,	
			1  	,	
			1  	,	
			1  	,	
			1  	,	
			1  	,	
			1  	,	
			3  	,	
			2  	,	
			1  	,	
			2  	,	
			1  	,	//20
			101	,	
			101	,	
			101	,	
			549	,	
			535	,	
			535	,	
			442	,	
			523	,	
			523	,	
			468	,	
			416	,	
			416	,	
			362	,	
			362	,	
			367	,	
			325	,	
			253	,	
			233	,	
			233	,	
			194	,	//40
			233	,	
			253	,	
			239	,	
			253	,	
			367	,	
			390	,	
			451	,	
			393	,	
			451	,	
			482	,	
			499	,	
			535	,	
			557	,	
			530	,	
			530	,	
			530	,	
			484	,	
			484	,	
			484	,	
			484	,	//60
			484	,	
			484	,	
			484	,	
			482	,	
			484	,	
			484	,	
			612	,	
			630	,	
			616	,	
			616	,	
			629	,	
			663	,	
			699	,	
			419	,	
			419	,	
			380	,	
			380	,	
			419	,	
			386	,	
			363	,	//80
			363	,	
			363	,	
			389	,	
			325	,	
			353	,	
			353	,	
			353	,	
			344	,	
			291	,	
			350	,	
			344	,	
			371	,	
			371	,	
			307	,	
			330	,	
			330	,	
			330	,	
			340	,	
			347	,	
			347	,	//100
			347	,	
			235	,	
			235	,	
			177	,	
			177	,	
			177	,	
			177	,	
			177	,	
			177	,	
			235	,	
			196	,	
			82 	,	
			177	,	
			82 	,	
			82 	,	
			82 	,	
			82 	,	
			82 	,	
			82 	,	
			82 	,	//120
			82 	,	
			177	,	
			177	,	
			161	,	
			161	,	
			161	,	
			178	,	
			178	,	
			178	,	
			154	,	
			210	,	
			164	,	
			183	,	
			186	,	
			193	,	
			214	,	
			233	,	
			233	,	
			246	,	
			241	,	//140
			256	,	
			265	,	
			276	,	
			267	,	
			267	,	
			278	,	
			267	,	
			189	,	
			196	,	
			196	,	
			196	,	
			200	,	
			200	,	
			58 	,	
			58 	,	
			75 	,	
			75 	,	
			75 	,	
			85 	,	
			85 	,	//160
			85 	,	
			100	,	
			100	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,		//180
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,		//200
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,		//220
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,		//240
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,	
			127	,
		};
		#endregion

		#region neighbourArrayStar
		private int[] neighbourArrayStar = new int[]
		{
			3 	,   		//0
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		
			3 	,   		//20
			11	,   		
			49	,   		
			41	,   		
			41	,   		
			39	,   		
			37	,   		
			37	,   		
			35	,   		
			35	,   		
			33	,   		
			33	,   		
			33	,   		
			31	,   		
			31	,   		
			31	,   		
			27	,   		
			23	,   		
			19	,   		
			13	,   		
			13	,   		//40
			15	,   		
			17	,   		
			19	,   		
			21	,   		
			49	,   		
			47	,   		
			45	,   		
			45	,   		
			45	,   		
			45	,   		
			43	,   		
			43	,   		
			43	,   		
			43	,   		
			43	,   		
			43	,   		
			43	,   		
			41	,   		
			39	,   		
			39	,   		//60
			43	,   		
			43	,   		
			41	,   		
			43	,   		
			43	,   		
			43	,   		
			43	,   		
			41	,   		
			41	,   		
			41	,   		
			45	,   		
			47	,   		
			47	,   		
			47	,   		
			51	,   		
			23	,   		
			23	,   		
			23	,   		
			23	,   		
			23	,   		//80
			21	,   		
			21	,   		
			21	,   		
			19	,   		
			19	,   		
			19	,   		
			19	,   		
			19	,   		
			19	,   		
			19	,   		
			19	,   		
			19	,   		
			15	,   		
			15	,   		
			17	,   		
			15	,   		
			15	,   		
			15	,   		
			15	,   		
			13	,   		//100
			15	,   		
			13	,   		
			13	,   		
			11	,   		
			11	,   		
			11	,   		
			11	,   		
			11	,   		
			11	,   		
			11	,   		
			11	,   		
			11	,   		
			11	,   		
			7 	,   		
			7 	,   		
			11	,   		
			11	,   		
			11	,   		
			11	,   		
			11	,   		//120
			11	,   		
			11	,   		
			7 	,   		
			7 	,   		
			7 	,   		
			9 	,   		
			9 	,   		
			9 	,   		
			9 	,   		
			7 	,   		
			7 	,   		
			7 	,   		
			7 	,   		
			7 	,   		
			7 	,   		
			9 	,   		
			9 	,   		
			7 	,   		
			9 	,   		
			9 	,   		//140
			7 	,   		
			7 	,   		
			7 	,   		
			7 	,   		
			7 	,		
			7 	,   		
			5 	,   		
			5 	,   		
			5 	,   		
			5 	,   		
			5 	,   		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		//160
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			5 	,		
			3 	,		
			3 	,		
			3 	,			//180
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,			//200
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,			//220
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   			//240
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 	,   		
			1 		
		};
		#endregion

		#region thresArrayStar
		private int[] thresArrayStar = new int[]
		{
			3   	, 		//0
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			3   	, 		
			2   	, 		
			2   	, 		
			2   	, 		
			2   	, 		//20
			52  	, 		
			459 	, 		
			724 	, 		
			749 	, 		
			802 	, 		
			741 	, 		
			741 	, 		
			724 	, 		
			746 	, 		
			704 	, 		
			708 	, 		
			729 	, 		
			690 	, 		
			663 	, 		
			618 	, 		
			523 	, 		
			456 	, 		
			363 	, 		
			246 	, 		
			255 	, 		//40
			275 	, 		
			311 	, 		
			350 	, 		
			428 	, 		
			1553	, 		
			1480	, 		
			1474	, 		
			1474	, 		
			1474	, 		
			1527	, 		
			1470	, 		
			1500	, 		
			1553	, 		
			1563	, 		
			1593	, 		
			1593	, 		
			1593	, 		
			1570	, 		
			1421	, 		
			1421	, 		//60
			1593	, 		
			1593	, 		
			1449	, 		
			1593	, 		
			1593	, 		
			1593	, 		
			1593	, 		
			1570	, 		
			1570	, 		
			1570	, 		
			1745	, 		
			1867	, 		
			1904	, 		
			1904	, 		
			2116	, 		
			1045	, 		
			1084	, 		
			1121	, 		
			1135	, 		
			1136	, 		//80
			1031	, 		
			1085	, 		
			1098	, 		
			1020	, 		
			1043	, 		
			1073	, 		
			1053	, 		
			1053	, 		
			1053	, 		
			1053	, 		
			1053	, 		
			1053	, 		
			955 	, 		
			931 	, 		
			1054	, 		
			967 	, 		
			967 	, 		
			1113	, 		
			1110	, 		
			802 	, 		//100
			1053	, 		
			878 	, 		
			884 	, 		
			663 	, 		
			663 	, 		
			663 	, 		
			663 	, 		
			663 	, 		
			597 	, 		
			597 	, 		
			597 	, 		
			597 	, 		
			597 	, 		
			363 	, 		
			363 	, 		
			597 	, 		
			597 	, 		
			597 	, 		
			597 	, 		
			597 	, 		//120
			597 	, 		
			597 	, 		
			363 	, 		
			363 	, 		
			363 	, 		
			600 	, 		
			600 	, 		
			600 	, 		
			666 	, 		
			622 	, 		
			631 	, 		
			679 	, 		
			712 	, 		
			766 	, 		
			803 	, 		
			1134	, 		
			1144	, 		
			948 	, 		
			1207	, 		
			1268	, 		//140
			1029	, 		
			1070	, 		
			1091	, 		
			1070	, 		
			1091	,		
			1091	, 		
			836 	, 		
			836 	, 		
			882 	, 		
			905 	, 		
			905 	, 		
			905 	,		
			941 	,		
			998 	,		
			1200	,		
			1500	,		
			2000	,		
			2500	,		
			3187	,		
			3187	,		//160
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			3187	,		
			1188	,		
			1188	,		
			1188	,			//180
			198 	,		
			198 	,		
			198 	,		
			198 	,		
			198 	,		
			198 	,		
			198 	,		
			198 	,		
			198 	,		
			198 	,		
			198 	,		
			198 	,		
			198 	,		
			198 	,		
			198 	,		
			170 	,		
			198 	,		
			198 	,		
			198 	,		
			198 	,			//200
			170 	,		
			198 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,			//220
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	,		
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 			//240
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 		
			170 	, 		
			127 	, 		
			127 	
		};
		#endregion


		//	PUBLIC METHODS
		#region public methods

		#region Dynamic()
		public void Dynamic(string file, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat)
		{
			Dynamic(file, destFile, imageFormat, new BinarizationParameters());
		}

		public void Dynamic(string file, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat, BinarizationParameters parameters)
		{
			Dynamic(file, destFile, imageFormat, Rectangle.Empty, parameters);
		}

		public void Dynamic(string file, string destFile, ImageProcessing.FileFormat.IImageFormat imageFormat, Rectangle clip, BinarizationParameters parameters)
		{
			using (ImageProcessing.BigImages.ItDecoder itDecoder = new ItDecoder(file))
			{
				using (Bitmap bitonalBitmap = DynamicToBitmap(itDecoder, clip, parameters))
				{
					SaveBitonalBitmapToFile(bitonalBitmap, destFile, imageFormat);
				}
			}
		}
		#endregion

		#region DynamicToBitmap()
		public Bitmap DynamicToBitmap(ImageProcessing.BigImages.ItDecoder itDecoder)
		{
			return DynamicToBitmap(itDecoder, new BinarizationParameters());
		}

		public Bitmap DynamicToBitmap(ImageProcessing.BigImages.ItDecoder itDecoder, BinarizationParameters parameters)
		{
			return DynamicToBitmap(itDecoder, Rectangle.Empty, parameters);
		}

		public Bitmap DynamicToBitmap(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip, BinarizationParameters parameters)
		{
			// fix clip if necessary
			if (clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, itDecoder.Width, itDecoder.Height);
			else
				clip.Intersect(Rectangle.FromLTRB(0, 0, itDecoder.Width, itDecoder.Height));
			
			try
			{
				switch (itDecoder.PixelFormat)
				{
					case PixelFormat.Format4bppIndexed:
					case PixelFormat.Format8bppIndexed:
					case PixelFormat.Format24bppRgb :
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
						return BinarizeInternal(itDecoder, clip);
					default:
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}
			}
			catch(Exception ex)
			{
				throw new Exception("Binarization, DynamicToBitmap(): " + ex.Message);
			}
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods
	
		#region BinarizeInternal()
		private Bitmap BinarizeInternal(ImageProcessing.BigImages.ItDecoder itDecoder, Rectangle clip)
		{
			Bitmap result = null;

			try
			{
				int stripHeightMax = Misc.GetStripHeightMax(itDecoder);
				
				result = new Bitmap(clip.Width, clip.Height, PixelFormat.Format1bppIndexed);

				for (int stripY = clip.Top; stripY < clip.Bottom; stripY += stripHeightMax)
				{
					Bitmap source = null;

					int stripH = Math.Min(stripHeightMax, clip.Bottom - stripY);
					
					try
					{
						int sourceTop = Math.Max(0, stripY - 24);
						int sourceBottom = Math.Min(stripH + (stripY - sourceTop) + 24, clip.Bottom);
						
						source = itDecoder.GetClip(new Rectangle(clip.X, sourceTop, clip.Width, sourceBottom));

						Binarize(source, sourceTop, result, stripY, stripH);
					}
					finally
					{					
						itDecoder.ReleaseAllocatedMemory(source);
						
						if (source != null)
						{
							source.Dispose();
							source = null;
						}
					}

					FireProgressEvent((stripY + stripH) / (float)itDecoder.Height);
				}
				
				if (result != null)
					ImageProcessing.Misc.SetBitmapResolution(result, itDecoder.DpiX, itDecoder.DpiY);

				return result;
			}
			catch (Exception ex)
			{
				if (result != null)
				{
					result.Dispose();
					result = null;
				}

				throw ex;
			}
			finally
			{
			}
		}
		#endregion

		#region Binarize()
		private void Binarize(Bitmap source, int sourceTop, Bitmap bitonal, int bitonalTop, int bitonalHeight)
		{
			Bitmap edge = null;
			BitmapData sourceData = null;
			BitmapData edgeData = null;
			BitmapData bitonalData = null;

			try
			{
				int width = source.Width;
				edge = ImageProcessing.EdgeDetector.Get(source, Rectangle.Empty, EdgeDetector.Operator.Sobel);

				sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				edgeData = edge.LockBits(new Rectangle(0, 0, edge.Width, edge.Height), ImageLockMode.ReadOnly, edge.PixelFormat);
				bitonalData = bitonal.LockBits(new Rectangle(0, bitonalTop, bitonal.Width, bitonalHeight), ImageLockMode.WriteOnly, bitonal.PixelFormat);

#if DEBUG
				DateTime start = DateTime.Now;
#endif

				switch (sourceData.PixelFormat)
				{
					case PixelFormat.Format24bppRgb:
						BinarizeColor(sourceData, sourceTop, bitonalData, bitonalTop, bitonalHeight, edgeData, 3);
						break;
					case PixelFormat.Format32bppArgb:
					case PixelFormat.Format32bppRgb:
						BinarizeColor(sourceData, sourceTop, bitonalData, bitonalTop, bitonalHeight, edgeData, 4);
						break;
					case PixelFormat.Format8bppIndexed:
						BinarizeGrayscale(sourceData, sourceTop, bitonalData, bitonalTop, bitonalHeight, edgeData);
						break;
				}

#if DEBUG
				Console.WriteLine("Dynamic Binorization: " + DateTime.Now.Subtract(start).ToString());
#endif
			}
			finally
			{
				if (bitonalData != null)
					bitonal.UnlockBits(bitonalData);
				if (edgeData != null)
					edge.UnlockBits(edgeData);
				if (sourceData != null)
					source.UnlockBits(sourceData);
				if (edge != null)
					edge.Dispose();
			}
		}
		#endregion

		#region BinarizeGrayscale()
		private void BinarizeGrayscale(BitmapData sourceData, int sourceTop, BitmapData bitonalData, int bitonalTop, int bitonalHeight, BitmapData edgeData)
		{
			int width = sourceData.Width;

			int strideS = sourceData.Stride;
			int strideE = edgeData.Stride;
			int strideB = bitonalData.Stride;
			int x, y;
			int g, sum, count;

			unsafe
			{
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pEdge = (byte*)edgeData.Scan0.ToPointer();
				byte* pBitonal = (byte*)bitonalData.Scan0.ToPointer();
				int maxNeighbourArea = 51 / 2;
				int edgeHeight = edgeData.Height;

				for (y = maxNeighbourArea; y < bitonalHeight - maxNeighbourArea; y++)
				{
					for (x = maxNeighbourArea; x < width - maxNeighbourArea; x++)
					{
						int sourceY = bitonalTop - sourceTop + y;
						g = pSource[sourceY * strideS + x];

						if (g > 180)
							pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));

						else if ((g <= 75 && g >= 65) || (g <= 58 && g >= 23)) // Star
						{
							sum = 0;

							for (int i = neighbourArrayX[g] / 2; i > 0; i--)
								sum += (int)(pEdge[(y - i) * strideE + (x - i)] + pEdge[(y - i) * strideE + (x + i)] + pEdge[(y + i) * strideE + (x - i)] + pEdge[(y + i) * strideE + (x + i)] + pEdge[(y - i) * strideE + x] + pEdge[(y + i) * strideE + x] + pEdge[y * strideE + (x - i)] + pEdge[y * strideE + (x + i)]);

							sum += pEdge[y * strideE + x];

							if (thresArrayX[g] > sum)
								pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
						else // X
						{
							sum = 0;

							for (int i = neighbourArrayX[g] / 2; i > 0; i--)
								sum += (int)(pEdge[(y - i) * strideE + (x - i)] + pEdge[(y - i) * strideE + (x + i)] + pEdge[(y + i) * strideE + (x - i)] + pEdge[(y + i) * strideE + (x + i)]);

							sum += pEdge[y * strideE + x];

							if (thresArrayX[g] > sum)
								pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
					}
				}

				//top
				for (y = 0; y < maxNeighbourArea; y++)
				{
					for (x = maxNeighbourArea; x < width - maxNeighbourArea; x++)
					{
						int sourceY = bitonalTop - sourceTop + y;
						g = pSource[sourceY * strideS + x];

						if (g > 180)
							pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));

						else // X
						{
							sum = 0;
							count = 0;

							for (int i = neighbourArrayX[g] / 2; i > 0; i--)
							{
								sum += (int)(pEdge[(y + i) * strideE + (x - i)] + pEdge[(y + i) * strideE + (x + i)]);

								if (y - i >= 0)
								{
									sum += (int)(pEdge[(y - i) * strideE + (x - i)] + pEdge[(y - i) * strideE + (x + i)]);
									count += 4;
								}
								else
									count += 2;
							}

							sum += pEdge[y * strideE + x];
							count++;
							sum = (int)(sum * ((neighbourArrayX[g] * neighbourArrayX[g]) / (double)count));

							if (thresArrayX[g] > sum)
								pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
					}
				}

				//bottom
				for (y = bitonalHeight - maxNeighbourArea; y < bitonalHeight; y++)
				{
					for (x = maxNeighbourArea; x < width - maxNeighbourArea; x++)
					{
						int sourceY = bitonalTop - sourceTop + y;
						g = pSource[sourceY * strideS + x];

						if (g > 180)
							pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));

						else // X
						{
							sum = 0;
							count = 0;

							for (int i = neighbourArrayX[g] / 2; i > 0; i--)
							{
								sum += (int)(pEdge[(y - i) * strideE + (x - i)] + pEdge[(y - i) * strideE + (x + i)]);

								if (y + i < bitonalHeight)
								{
									sum += (int)(pEdge[(y + i) * strideE + (x - i)] + pEdge[(y + i) * strideE + (x + i)]);
									count += 4;
								}
								else
									count += 2;
							}

							sum += pEdge[y * strideE + x];
							count++;
							sum = (int)(sum * ((neighbourArrayX[g] * neighbourArrayX[g]) / (double)count));

							if (thresArrayX[g] > sum)
								pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
					}
				}

				//left
				for (y = 0; y < bitonalHeight; y++)
				{
					for (x = 0; x < maxNeighbourArea; x++)
					{
						int sourceY = bitonalTop - sourceTop + y;
						g = pSource[sourceY * strideS + x];

						if (g > 180)
							pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));

						else // X
						{
							sum = 0;
							count = 0;

							for (int i = neighbourArrayX[g] / 2; i > 0; i--)
							{
								if (y + i < bitonalHeight)
								{
									if (x + i < width)
									{
										sum += pEdge[(y + i) * strideE + (x + i)];
										count++;
									}
									if (x - i >= 0)
									{
										sum += pEdge[(y + i) * strideE + (x - i)];
										count++;
									}
								}
								if (y - i > 0)
								{
									if (x + i < width)
									{
										sum += pEdge[(y - i) * strideE + (x + i)];
										count++;
									}
									if (x - i >= 0)
									{
										sum += pEdge[(y - i) * strideE + (x - i)];
										count++;
									}
								}
							}

							sum += pEdge[y * strideE + x];
							count++;
							sum = (int)(sum * ((neighbourArrayX[g] * neighbourArrayX[g]) / (double)count));

							if (thresArrayX[g] > sum)
								pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
					}
				}

				//right
				for (y = 0; y < bitonalHeight; y++)
				{
					for (x = width - maxNeighbourArea; x < width; x++)
					{
						int sourceY = bitonalTop - sourceTop + y;
						g = pSource[sourceY * strideS + x];

						if (g > 180)
							pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));

						else // X
						{
							sum = 0;
							count = 0;

							for (int i = neighbourArrayX[g] / 2; i > 0; i--)
							{
								if (y + i < bitonalHeight)
								{
									if (x + i < width)
									{
										sum += pEdge[(y + i) * strideE + (x + i)];
										count++;
									}
									if (x - i >= 0)
									{
										sum += pEdge[(y + i) * strideE + (x - i)];
										count++;
									}
								}
								if (y - i > 0)
								{
									if (x + i < width)
									{
										sum += pEdge[(y - i) * strideE + (x + i)];
										count++;
									}
									if (x - i >= 0)
									{
										sum += pEdge[(y - i) * strideE + (x - i)];
										count++;
									}
								}
							}

							sum += pEdge[y * strideE + x];
							count++;
							sum = (int)(sum * ((neighbourArrayX[g] * neighbourArrayX[g]) / (double)count));

							if (thresArrayX[g] > sum)
								pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
					}
				}
			}
		}
		#endregion

		#region BinarizeColor()
		private void BinarizeColor(BitmapData sourceData, int sourceTop, BitmapData bitonalData, int bitonalTop, int bitonalHeight, BitmapData edgeData, int bytesPerPizel)
		{
			int width = sourceData.Width;

			int strideS = sourceData.Stride;
			int strideE = edgeData.Stride;
			int strideB = bitonalData.Stride;
			int x, y;
			int g, sum, count;

			unsafe
			{
				byte* pSource = (byte*)sourceData.Scan0.ToPointer();
				byte* pEdge = (byte*)edgeData.Scan0.ToPointer();
				byte* pBitonal = (byte*)bitonalData.Scan0.ToPointer();
				int maxNeighbourArea = 51 / 2;
				int edgeHeight = edgeData.Height;

				for (y = maxNeighbourArea; y < bitonalHeight - maxNeighbourArea; y++)
				{
					for (x = maxNeighbourArea; x < width - maxNeighbourArea; x++)
					{
						int sourceY = bitonalTop - sourceTop + y;

						g = (pSource[sourceY * strideS + x * bytesPerPizel] + pSource[sourceY * strideS + x * bytesPerPizel + 1] + pSource[sourceY * strideS + x * bytesPerPizel + 2]) / 3;

						if (g > 180)
							pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));

						else if ((g <= 75 && g >= 65) || (g <= 58 && g >= 23)) // Star
						{
							sum = 0;

							for (int i = neighbourArrayX[g] / 2; i > 0; i--)
								sum += (int)(pEdge[(y - i) * strideE + (x - i)] + pEdge[(y - i) * strideE + (x + i)] + pEdge[(y + i) * strideE + (x - i)] + pEdge[(y + i) * strideE + (x + i)] + pEdge[(y - i) * strideE + x] + pEdge[(y + i) * strideE + x] + pEdge[y * strideE + (x - i)] + pEdge[y * strideE + (x + i)]);

							sum += pEdge[y * strideE + x];

							if (thresArrayX[g] > sum)
								pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
						else // X
						{
							sum = 0;

							for (int i = neighbourArrayX[g] / 2; i > 0; i--)
								sum += (int)(pEdge[(y - i) * strideE + (x - i)] + pEdge[(y - i) * strideE + (x + i)] + pEdge[(y + i) * strideE + (x - i)] + pEdge[(y + i) * strideE + (x + i)]);

							sum += pEdge[y * strideE + x];

							if (thresArrayX[g] > sum)
								pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
					}
				}

				//top
				for (y = 0; y < maxNeighbourArea; y++)
				{
					for (x = maxNeighbourArea; x < width - maxNeighbourArea; x++)
					{
						int sourceY = bitonalTop - sourceTop + y;
						g = (pSource[sourceY * strideS + x * bytesPerPizel] + pSource[sourceY * strideS + x * bytesPerPizel + 1] + pSource[sourceY * strideS + x * bytesPerPizel + 2]) / 3;

						if (g > 180)
							pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));

						else // X
						{
							sum = 0;
							count = 0;

							for (int i = neighbourArrayX[g] / 2; i > 0; i--)
							{
								sum += (int)(pEdge[(y + i) * strideE + (x - i)] + pEdge[(y + i) * strideE + (x + i)]);

								if (y - i >= 0)
								{
									sum += (int)(pEdge[(y - i) * strideE + (x - i)] + pEdge[(y - i) * strideE + (x + i)]);
									count += 4;
								}
								else
									count += 2;
							}

							sum += pEdge[y * strideE + x];
							count++;
							sum = (int)(sum * ((neighbourArrayX[g] * neighbourArrayX[g]) / (double)count));

							if (thresArrayX[g] > sum)
								pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
					}
				}

				//bottom
				for (y = bitonalHeight - maxNeighbourArea; y < bitonalHeight; y++)
				{
					for (x = maxNeighbourArea; x < width - maxNeighbourArea; x++)
					{
						int sourceY = bitonalTop - sourceTop + y;
						g = (pSource[sourceY * strideS + x * bytesPerPizel] + pSource[sourceY * strideS + x * bytesPerPizel + 1] + pSource[sourceY * strideS + x * bytesPerPizel + 2]) / 3;

						if (g > 180)
							pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));

						else // X
						{
							sum = 0;
							count = 0;

							for (int i = neighbourArrayX[g] / 2; i > 0; i--)
							{
								sum += (int)(pEdge[(y - i) * strideE + (x - i)] + pEdge[(y - i) * strideE + (x + i)]);

								if (y + i < bitonalHeight)
								{
									sum += (int)(pEdge[(y + i) * strideE + (x - i)] + pEdge[(y + i) * strideE + (x + i)]);
									count += 4;
								}
								else
									count += 2;
							}

							sum += pEdge[y * strideE + x];
							count++;
							sum = (int)(sum * ((neighbourArrayX[g] * neighbourArrayX[g]) / (double)count));

							if (thresArrayX[g] > sum)
								pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
					}
				}

				//left
				for (y = 0; y < bitonalHeight; y++)
				{
					for (x = 0; x < maxNeighbourArea; x++)
					{
						int sourceY = bitonalTop - sourceTop + y;

						g = (pSource[sourceY * strideS + x * bytesPerPizel] + pSource[sourceY * strideS + x * bytesPerPizel + 1] + pSource[sourceY * strideS + x * bytesPerPizel + 2]) / 3;

						if (g > 180)
							pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));

						else // X
						{
							sum = 0;
							count = 0;

							for (int i = neighbourArrayX[g] / 2; i > 0; i--)
							{
								if (y + i < bitonalHeight)
								{
									if (x + i < width)
									{
										sum += pEdge[(y + i) * strideE + (x + i)];
										count++;
									}
									if (x - i >= 0)
									{
										sum += pEdge[(y + i) * strideE + (x - i)];
										count++;
									}
								}
								if (y - i > 0)
								{
									if (x + i < width)
									{
										sum += pEdge[(y - i) * strideE + (x + i)];
										count++;
									}
									if (x - i >= 0)
									{
										sum += pEdge[(y - i) * strideE + (x - i)];
										count++;
									}
								}
							}

							sum += pEdge[y * strideE + x];
							count++;
							sum = (int)(sum * ((neighbourArrayX[g] * neighbourArrayX[g]) / (double)count));

							if (thresArrayX[g] > sum)
								pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
					}
				}

				//right
				for (y = 0; y < bitonalHeight; y++)
				{
					for (x = width - maxNeighbourArea; x < width; x++)
					{
						int sourceY = bitonalTop - sourceTop + y;

						g = (pSource[sourceY * strideS + x * bytesPerPizel] + pSource[sourceY * strideS + x * bytesPerPizel + 1] + pSource[sourceY * strideS + x * bytesPerPizel + 2]) / 3;

						if (g > 180)
							pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));

						else // X
						{
							sum = 0;
							count = 0;

							for (int i = neighbourArrayX[g] / 2; i > 0; i--)
							{
								if (y + i < bitonalHeight)
								{
									if (x + i < width)
									{
										sum += pEdge[(y + i) * strideE + (x + i)];
										count++;
									}
									if (x - i >= 0)
									{
										sum += pEdge[(y + i) * strideE + (x - i)];
										count++;
									}
								}
								if (y - i > 0)
								{
									if (x + i < width)
									{
										sum += pEdge[(y - i) * strideE + (x + i)];
										count++;
									}
									if (x - i >= 0)
									{
										sum += pEdge[(y - i) * strideE + (x - i)];
										count++;
									}
								}
							}

							sum += pEdge[y * strideE + x];
							count++;
							sum = (int)(sum * ((neighbourArrayX[g] * neighbourArrayX[g]) / (double)count));

							if (thresArrayX[g] > sum)
								pBitonal[y * strideB + x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
					}
				}
			}
		}
		#endregion

		#endregion
	}
}
