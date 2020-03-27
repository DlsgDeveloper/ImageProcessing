using System;
using System.Drawing ;
using System.Drawing.Imaging ;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;


namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class CropAndDescewNew
	{		
		static int ph = GetProcessHeap();

		// Heap API flags
		const int HEAP_ZERO_MEMORY = 0x00000008;

		[DllImport("kernel32")]
		static extern int GetProcessHeap();
		[DllImport("kernel32")]
		static unsafe extern void* HeapAlloc(int hHeap, int flags, int size);
		[DllImport("kernel32")]
		static unsafe extern bool HeapFree(int hHeap, int flags, void* block);
		[DllImport("kernel32")]
		static unsafe extern void* HeapReAlloc(int hHeap, int flags, void* block, int size);
		[DllImport("kernel32")]
		static unsafe extern int HeapSize(int hHeap, int flags, void* block);

		#region constructor
		private CropAndDescewNew()
		{
		}
		#endregion

		#region class Crop
		class Crop
		{
			Point	left;
			Point	top;
			Point	right;
			Point	bottom;

			public Crop(int width, int height)
			{
				this.left = new Point(0, height / 2);
				this.top = new Point(width / 2, 0);
				this.right = new Point(width, height / 2);
				this.bottom = new Point(width / 2, height);
			}

			public Point	Left	{ get{return left;} set{this.left = value;}}
			public Point	Top		{ get{return top;} set{this.top = value;} }
			public Point	Right	{ get{return right;} set{this.right = value;} }
			public Point	Bottom	{ get{return bottom;} set{this.bottom = value;} }		
		}
		#endregion

		#region class Edge
		class Edge
		{
			Point		point1;
			Point		point2;

			public Edge()
			{
				this.point1 = Point.Empty;
				this.point2 = Point.Empty;
			}

			public Edge(Point point1, Point point2)
			{
				this.point1 = point1;
				this.point2 = point2;
			}

			public Point	Point1		{ get{return point1;} }
			public Point	Point2		{ get{return point2;} }
			public bool		Exists		{ get{return (!point1.IsEmpty || !point2.IsEmpty);} }
			public int		Width		{ get{return (point2.X > point1.X) ? point2.X - point1.X : point1.X - point2.X;} }
			public int		Height		{ get{return (point2.Y > point1.Y) ? point2.Y - point1.Y : point1.Y - point2.Y;} }

			public double Length
			{
				get
				{
					if(this.Exists)
						return Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2));

					return 0;
				}
			}

			public double Angle
			{
				get
				{
					if(this.Exists)
						return Math.Atan2(point2.Y - point1.Y, point2.X - point1.X) * (180 / Math.PI);

					return 0;
				}
			}
		}
		#endregion 

		#region class Rect
		class Rect
		{
			Point		ulCorner;
			Point		urCorner;
			Point		llCorner;
			Point		lrCorner;
			double		angle;
			int			width;
			int			validCorners = 0;
			float		minAngleToDescew = 1;

			public Rect(Point ulCorner, Point urCorner, Point llCorner, Point lrCorner, Crop clip, float minAngleToDescew)
			{
				this.ulCorner = ulCorner;
				this.urCorner = urCorner;
				this.llCorner = llCorner;
				this.lrCorner = lrCorner;
				this.minAngleToDescew = minAngleToDescew;

				double	ulAngle = GetAngle(urCorner, ulCorner, llCorner);
				double	urAngle = GetAngle(lrCorner, urCorner, ulCorner);
				double	lrAngle = GetAngle(llCorner, lrCorner, urCorner);
				double	llAngle = GetAngle(ulCorner, llCorner, lrCorner);

				Edge	lEdge = new Edge(this.ulCorner, this.llCorner);
				Edge	tEdge = new Edge(this.ulCorner, this.urCorner);
				Edge	rEdge = new Edge(this.urCorner, this.lrCorner);
				Edge	bEdge = new Edge(this.lrCorner, this.llCorner);

				ArrayList		validEdges = new ArrayList();
				ArrayList		validAngles = new ArrayList();

				double	lAngle = 0;
				double	tAngle = 0;
				double	rAngle = 0;
				double	bAngle = 0;

				if(ulAngle != 0)
				{
					validEdges.Add(lEdge);
					validEdges.Add(tEdge);
					validAngles.Add(ulAngle);
					validCorners++;
				}

				if(urAngle != 0)
				{
					if(!validEdges.Contains(tEdge))
						validEdges.Add(tEdge);
					
					validEdges.Add(rEdge);
					validAngles.Add(urAngle);
					validCorners++;
				}

				if(lrAngle != 0)
				{
					if(!validEdges.Contains(rEdge))
						validEdges.Add(rEdge);
					
					validEdges.Add(bEdge);
					validAngles.Add(lrAngle);
					validCorners++;
				}

				if(llAngle != 0)
				{
					if(!validEdges.Contains(bEdge))
						validEdges.Add(bEdge);
					
					if(!validEdges.Contains(lEdge))
						validEdges.Add(lEdge);
					validAngles.Add(llAngle);
					validCorners++;
				}
			
				if(validEdges.Count > 0)
				{					
					if(validEdges.Contains(lEdge))
					{
						lAngle = lEdge.Angle - 90;
						if(lAngle > 90)
							lAngle = lAngle - 180;
						else if(lAngle < -90)
							lAngle = lAngle + 180;

						angle += lAngle;
					}
					
					if(validEdges.Contains(tEdge))
					{
						tAngle = tEdge.Angle;
						if(tAngle > 90)
							tAngle = tAngle - 180;
						else if(tAngle < -90)
							tAngle = tAngle + 180;

						angle += tAngle;
					}

					if(validEdges.Contains(rEdge))
					{
						rAngle = rEdge.Angle - 90;
						if(rAngle > 90)
							rAngle = rAngle - 180;
						else if(rAngle < -90)
							rAngle = rAngle + 180;

						angle += rAngle;
					}

					if(validEdges.Contains(bEdge))
					{
						bAngle = bEdge.Angle;
						if(bAngle > 90)
							bAngle = bAngle - 180;
						else if(bAngle < -90)
							bAngle = bAngle + 180;

						angle += bAngle;
					}

					angle = angle / validEdges.Count;
				}

				if(Math.Abs(angle) < minAngleToDescew)
				{
					Rectangle	rect = GetRectangle(clip);

					this.ulCorner = rect.Location;
					this.urCorner = new Point(rect.Right, rect.Top);
					this.llCorner = new Point(rect.Left, rect.Bottom);
					this.lrCorner = new Point(rect.Right, rect.Bottom);

					this.width = this.urCorner.X - this.ulCorner.X;
					this.angle = 0;
				}
				else
				{					
					if(validAngles.Contains(ulAngle))
					{
						if(!validAngles.Contains(llAngle))
						{
							/*double	gamma = Math.Atan2(clip.Bottom.X - ulCorner.X, clip.Bottom.Y - ulCorner.Y) * (180/Math.PI);
							double	beta = angle + gamma;
							int		z = (int) Math.Abs(Math.Sin(beta * (Math.PI/180)) * Math.Sqrt(Math.Pow(clip.Bottom.X - ulCorner.X, 2) + Math.Pow(clip.Bottom.Y - ulCorner.Y, 2)));
							int		cornerY = (int) (clip.Bottom.Y -(Math.Sin(angle * (Math.PI/180)) * z));
							int		cornerX;*/

							double	cornerX, cornerY;
							
							if(validEdges.Contains(lEdge) && validEdges.Contains(rEdge))
								cornerY = ulCorner.Y + Math.Max(lEdge.Height, rEdge.Height);
							else if(validEdges.Contains(lEdge))
								cornerY = ulCorner.Y + lEdge.Height;
							else
								cornerY = ulCorner.Y + rEdge.Height;

							if(cornerY < clip.Bottom.Y)
							{
								double	x1, y1, z1, y2;

								x1 = clip.Bottom.X - ulCorner.X;
								y1 = clip.Bottom.Y - ulCorner.Y;
								double	gamma = Math.Atan2(x1, y1);
								double	beta = gamma - Math.Abs((angle * (Math.PI/180)));
								
								z1 = Math.Sqrt(x1*x1+y1*y1);
								y2 = Math.Cos(beta) * z1;
								
								cornerY = ulCorner.Y + Math.Cos(angle * (Math.PI/180)) * y2;
							}

							if(angle != 0 && Math.Abs(clip.Bottom.Y - cornerY) > 3)
								cornerX = ulCorner.X - Math.Tan((angle) * (Math.PI / 180)) * (cornerY - ulCorner.Y);
							else
								cornerX = lEdge.Point1.X;

							this.llCorner = new Point((int)cornerX, (int)cornerY);
						}
					}					
					else if(validAngles.Contains(urAngle))
					{
						double	 cornerX;
						
						if(validEdges.Contains(tEdge) && validEdges.Contains(bEdge))
							cornerX = urCorner.X - Math.Max(tEdge.Width, bEdge.Width);
						else if(validEdges.Contains(tEdge))
							cornerX = urCorner.X - tEdge.Width;
						else
							cornerX = urCorner.X - bEdge.Width;

						if(cornerX < clip.Left.X)
							cornerX = clip.Left.X;

						// L R corner
						/*if(!validAngles.Contains(lrAngle))
						{
							double	beta = Math.Atan2(clip.Bottom.X - urCorner.X, clip.Bottom.Y - urCorner.Y) * (180/Math.PI);
							double	gamma = beta - angle;
							int		z = (int) Math.Abs(Math.Sin(beta * (Math.PI/180)) * Math.Sqrt(Math.Pow(clip.Bottom.X - urCorner.X, 2) + Math.Pow(clip.Bottom.Y - urCorner.Y, 2)));

							cornerY = (int) (clip.Bottom.Y + (Math.Sin(angle * (Math.PI/180)) * z));
							cornerY = Math.Min(cornerY, clip.Bottom.Y);

							if(angle != 0 && Math.Abs(cornerY - clip.Bottom.Y) > 3)
								cornerX = (int) (clip.Bottom.X + (cornerY - clip.Bottom.Y) / Math.Tan(angle * (Math.PI / 180)));
							else
								cornerX = rEdge.Point1.X;

							this.lrCorner = new Point(cornerX, cornerY);
						}

						//U L corner
						cornerY = urCorner.Y - Convert.ToInt32((Math.Tan(angle * (Math.PI / 180))) * (this.width));
						this.ulCorner = new Point(urCorner.X - this.width, cornerY);

						//L L Corner
						if(!validAngles.Contains(llAngle))
							this.llCorner = new Point(lrCorner.X - this.width, ulCorner.Y + (lrCorner.Y - urCorner.Y));*/
					}			
					//L L Angle
					else if(validAngles.Contains(llAngle))
					{						
						//double	beta = Math.Atan2(clip.Top.X - llCorner.X, clip.Top.Y - llCorner.Y) * (180/Math.PI);
						//double	gamma = beta + angle;
						//double	leg = Math.Cos(gamma * (Math.PI / 180)) * Math.Sqrt(Math.Pow(llCorner.X - clip.Top.X, 2) + Math.Pow(llCorner.Y - clip.Top.Y, 2));

						//double	cornerX = Math.Sin(angle * (Math.PI / 180)) * leg; 
						//double	cornerY = Math.Sqrt(leg * leg - cornerX * cornerX);
						//this.ulCorner = new Point(llCorner.X - (int) cornerX, llCorner.Y - (int) cornerY);
						double	cornerX, cornerY;

						if(validEdges.Contains(lEdge) && validEdges.Contains(rEdge))
							cornerY = llCorner.Y - Math.Max(lEdge.Height, rEdge.Height);
						else if(validEdges.Contains(lEdge))
							cornerY = llCorner.Y - lEdge.Height;
						else
							cornerY = llCorner.Y - rEdge.Height;

						if(angle != 0 && Math.Abs(clip.Bottom.Y - cornerY) > 3)
							cornerX = Convert.ToInt32(ulCorner.X -  Math.Tan((angle) * (Math.PI / 180)) * (cornerY - ulCorner.Y));
						else
							cornerX = lEdge.Point1.X;

						this.ulCorner = new Point((int) cornerX, (int) cornerY);
					}
					else if(validAngles.Contains(lrAngle))
					{						
						double	beta = Math.Atan2(lrCorner.Y - clip.Left.Y, lrCorner.X - clip.Left.X) * (180/Math.PI);
						double	gamma = beta - angle;
						double	leg = Math.Sqrt(Math.Pow(lrCorner.X - clip.Left.X, 2) + Math.Pow(lrCorner.Y - clip.Left.Y, 2)) * Math.Cos(gamma * (Math.PI / 180));
						double	cornerY = Math.Sin(angle * (Math.PI / 180)) * leg;
						double	cornerX = cornerY / Math.Tan(angle * (Math.PI / 180));
						
						this.llCorner = new Point(lrCorner.X - (int) cornerX, lrCorner.Y - (int) cornerY);
						this.width = lrCorner.X - llCorner.X;
						
						//get ul corner
						beta = Math.Atan2(clip.Top.X - llCorner.X, clip.Top.Y - llCorner.Y) * (180/Math.PI);
						gamma = beta - angle;
						leg = Math.Cos(gamma * (Math.PI / 180)) * Math.Sqrt(Math.Pow(llCorner.X - clip.Top.X, 2) + Math.Pow(llCorner.Y - clip.Top.Y, 2));

						cornerX = Math.Sin(angle * (Math.PI / 180)) * leg; 
						cornerY = Math.Sqrt(leg * leg + cornerX * cornerX);

						this.ulCorner = new Point(llCorner.X - (int) cornerX, llCorner.Y - (int) cornerY);
					}
					else
					{
						Rectangle	rect = GetRectangle(clip);

						this.ulCorner = rect.Location;
						this.urCorner = new Point(rect.Right, rect.Top);
						this.llCorner = new Point(rect.Left, rect.Bottom);
						this.lrCorner = new Point(rect.Right, rect.Bottom);

						this.width = this.urCorner.X - this.ulCorner.X;
						this.angle = 0;
					}

					if(validEdges.Contains(tEdge) && validEdges.Contains(bEdge))
						this.width = (tEdge.Width > bEdge.Width) ? tEdge.Width : bEdge.Width;
					else if(validEdges.Contains(tEdge))
						this.width = tEdge.Width;
					else
						this.width = bEdge.Width;
				}
			}

			public Point		UlCorner		{ get{return this.ulCorner;} }
			public Point		LlCorner		{ get{return this.llCorner;} }
			public bool			Inclined		{ get{return this.angle != 0;} }
			public double		Angle			{ get{return this.angle;} }
			public int			Width			{ get{return this.width;} }
			public int			Height			{ get{return this.llCorner.Y - this.ulCorner.Y;} }
			public int			ValidCorners	{ get{return this.validCorners;} }
						
			private double GetAngle(Point side1, Point middle, Point side2)
			{
				double angle = (Math.Atan2(side1.X - middle.X, side1.Y - middle.Y) - Math.Atan2(side2.X - middle.X, side2.Y - middle.Y)) * (180 / Math.PI);
				angle = (angle > 0) ? angle : 360 + angle;
				
				return (Math.Abs(angle - 90) <= 1) ? angle : 0;
			}

			private double GetTotalAngle(Point side1, Point middle, Point side2)
			{
				return 0;
			}
		}
		#endregion 

		#region enum ActionType
		enum ActionType
		{
			Nothing,
			Crop,
			CropAndDescewNew
		}
		#endregion 

		//	PUBLIC METHODS

		#region GoMem()
		public unsafe static int GoMem(ref int width, ref int height, ref int stride, PixelFormat pixelFormat, 
			byte** firstByte, ColorPalette palette, Color threshold,
			float minAngleToDescew, Rectangle clip) 
		{ 	
			return GoMem(ref width, ref height, ref stride, pixelFormat, firstByte, palette, threshold,
				minAngleToDescew, clip, false, 10, 60, 20, 12);
		}

		public unsafe static int GoMem(ref int width, ref int height, ref int stride, PixelFormat pixelFormat, 
			byte** firstByte, ColorPalette palette, Color threshold,
			float minAngleToDescew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta) 
		{ 			
			IntPtr		scan0 = new IntPtr(*firstByte);
			Bitmap		bitmap = new Bitmap(width, height, stride, pixelFormat, scan0);			
			if(bitmap == null)
				return 0 ;

			int			confidence = 0;	

			if(clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, width, height);
			else if(clip.Width == 0 || clip.Height == 0)
				clip = Rectangle.FromLTRB(clip.X, clip.Y, bitmap.Width - clip.X, bitmap.Height - clip.Y);

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif
			
			try
			{
				switch(bitmap.PixelFormat)
				{
					case PixelFormat.Format24bppRgb :				
						confidence = CropAndDescew24bppMem(bitmap, ref width, ref height, ref stride, ref scan0, threshold, true, 
							minAngleToDescew, clip, removeGhostLines, lowThreshold, highThreshold, linesToCheck, maxDelta);
						break ;
					case PixelFormat.Format8bppIndexed :
						bitmap.Palette = palette;
						confidence = CropAndDescew8bppMem(bitmap, ref width, ref height, ref stride, ref scan0, threshold, true, 
							minAngleToDescew, clip, removeGhostLines, lowThreshold, highThreshold, linesToCheck, maxDelta);
						break ;
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}
			}
			catch(Exception ex)
			{
#if DEBUG
				Console.WriteLine("CropAndDescewNew(): " + ex.Message) ;
#endif
				ex = ex;
				return 0;
			}
			
			*firstByte = (byte*) scan0.ToPointer();
			bitmap = null;
#if DEBUG
			TimeSpan	time = DateTime.Now.Subtract(start) ;
			Console.WriteLine(string.Format("RAM Image: {0}, Confidence:{1}%", time.ToString(), confidence));			
#endif
			return confidence;
		}
		#endregion
		
		#region GoStream() 
		public unsafe static int GoStream(byte** firstByte, int* length, Color threshold, short jpegCompression, 
			float minAngleToDescew, Rectangle clip) 
		{
			return GoStream(firstByte, length, threshold, jpegCompression, minAngleToDescew, clip, false, 10, 60, 20, 12);
		}
		
		public unsafe static int GoStream(byte** firstByte, int* length, Color threshold, short jpegCompression, 
			float minAngleToDescew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta) 
		{ 			
			byte			confidence = 0;

			DateTime		enterTime = DateTime.Now ;
			byte[]			array = new byte[*length];
			Marshal.Copy(new IntPtr(*firstByte), array, 0, (int) *length);
			Bitmap			bitmap;

			MemoryStream	stream = new MemoryStream(array);
			try
			{
				bitmap = new Bitmap(stream) ;
			}
			catch(Exception ex)
			{
				throw new Exception("Can't generate bitmap.\nException " + ex);
			}

#if DEBUG
			DateTime	start = DateTime.Now ;
#endif

			Bitmap		result = Go(bitmap, threshold, true, out confidence, minAngleToDescew, clip, removeGhostLines, 
				lowThreshold, highThreshold, linesToCheck, maxDelta, 1);
			
#if DEBUG
			TimeSpan	time = DateTime.Now.Subtract(start) ;
			Console.Write(string.Format("Crop & Descew: {0}, Confidence:{1}%",  time.ToString(), confidence)) ;
#endif
	
			ImageCodecInfo		codecInfo = Encoding.GetCodecInfo(bitmap);
			EncoderParameters	encoderParams = Encoding.GetEncoderParams(bitmap, jpegCompression) ;
				
			bitmap.Dispose();
			stream.Close();
			array = null;
			GC.Collect();

			MemoryStream	resultStream = new MemoryStream();
			try
			{
				result.Save(resultStream, codecInfo, encoderParams);
			}
			catch(Exception ex)
			{
				throw new Exception(string.Format("Can't save bitmap to stream.\nException: {0}\nStream : {1}\n" + 
					"Codec Info: {2}\nEncoder: {3}", ex.Message, (resultStream != null) ? "Exists": "null", 
					(codecInfo != null) ? codecInfo.CodecName : "null",
					(encoderParams != null) ? encoderParams.Param[0].ToString() : "null") );
			}
			GC.Collect();

			*length = (int) resultStream.Length;
			*firstByte = (byte*) HeapAlloc(ph, HEAP_ZERO_MEMORY, (int) resultStream.Length);
			Marshal.Copy(resultStream.ToArray(), 0, new IntPtr(*firstByte), (int) resultStream.Length);

			result.Dispose() ;
			resultStream.Close();
#if DEBUG
			Console.WriteLine(string.Format(" Total Time: {0}",  DateTime.Now.Subtract(enterTime).ToString())) ;
#endif
			GC.Collect();
			return confidence;
		}
		#endregion
				
		#region Go()
		public static int Go(string source, string dest, Color threshold, short jpegCompression, 
			float minAngleToDescew, Rectangle clip, int flags) 
		{ 			
			int result = Go(source, dest, threshold, jpegCompression, minAngleToDescew, clip, false, 10, 50, 20, 12, flags);
			GC.Collect();
			return result;
		}

		public static int Go(string source, string dest, Color threshold, short jpegCompression, 
			float minAngleToDescew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, int flags) 
		{ 			
			FileInfo	sourceFile = new FileInfo(source);
			FileInfo	destFile = new FileInfo(dest);

			if((sourceFile.Attributes & FileAttributes.Directory) > 0)
				return CropAndDescewDir(new DirectoryInfo(sourceFile.FullName), new DirectoryInfo(destFile.FullName), 
					threshold, jpegCompression, minAngleToDescew, clip, removeGhostLines, lowThreshold, highThreshold, 
					linesToCheck, maxDelta, flags);
			else
				return CropAndDescewFile(sourceFile, destFile, threshold, jpegCompression, minAngleToDescew, 
					clip, removeGhostLines, lowThreshold, highThreshold, linesToCheck, maxDelta, flags);
		}

		public static Bitmap Go(Bitmap bitmap, Color threshold, bool backDark, out byte confidence, 
			float minAngleToDescew, Rectangle clip, int flags)
		{
			return Go(bitmap, threshold, backDark, out confidence, minAngleToDescew, clip, false, 10, 60, 20, 12, flags);
		}
		
		public static Bitmap Go(Bitmap bmpSource, Color threshold, bool backDark, out byte confidence, 
			float minAngleToDescew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, int flags)
		{
			confidence = 0;
			
			if(bmpSource == null)
				return null ;

			if(clip.IsEmpty)
				clip = Rectangle.FromLTRB(0, 0, bmpSource.Width, bmpSource.Height);
			else if(clip.Width <= 0 || clip.Height <= 0)
				clip = Rectangle.FromLTRB(clip.X, clip.Y, bmpSource.Width - clip.X, bmpSource.Height - clip.Y);

			Bitmap		bmpResult = null ;
			
			try
			{				
				switch(bmpSource.PixelFormat)
				{
					case PixelFormat.Format24bppRgb :				
						bmpResult = CropAndDescew24bpp(bmpSource, threshold, backDark, out confidence, minAngleToDescew, clip, removeGhostLines, lowThreshold,
							highThreshold, linesToCheck, maxDelta, flags) ;
						break ;
					case PixelFormat.Format8bppIndexed :				
						bmpResult = CropAndDescew8bpp(bmpSource, threshold, backDark, out confidence, minAngleToDescew, clip, removeGhostLines, lowThreshold,
							highThreshold, linesToCheck, maxDelta, flags) ;
						break ;
					default :
						throw new IpException(ErrorCode.ErrorUnsupportedFormat) ;
				}

				if(bmpResult != null)
				{
					try
					{
						bmpResult.SetResolution(bmpSource.HorizontalResolution, bmpSource.VerticalResolution);
					}
					catch(Exception ex)
					{
						throw new Exception("SetResolution() Exception.\nHorizontal: " + bmpSource.HorizontalResolution + 
							"\nVertical: " + bmpSource.VerticalResolution + "\n" + ex.Message);
					}

					try
					{
						if(bmpSource.Palette != null)
							bmpResult.Palette = bmpSource.Palette ;
					}
					catch
					{
						throw new Exception("Palette Copying Exception.");
					}
					
					try
					{
						int[]		propItems = bmpSource.PropertyIdList ;

						foreach(int	propItem in propItems)
							bmpResult.SetPropertyItem(bmpSource.GetPropertyItem(propItem)) ;
					}
					catch(Exception ex)
					{
						throw new Exception("Image Property Copy Exception.\n" + ex.Message);
					}
				}
			}
			catch(Exception ex)
			{
				string	error = (bmpSource != null) ? "Bitmap: Exists" : "Bitmap: null";

				error += "Pixel Format: " + bmpSource.PixelFormat.ToString();
				error += "Threshold: " + threshold.ToString();
				error += "Confidence: " + confidence.ToString();
				error += "Angle: " + minAngleToDescew.ToString();
				error += "Clip: " + clip.ToString();
				error += "Exception: " + ex.Message;
				
				throw new Exception("CropAndDescewNew(): " + error + "\n") ;
			}

			return bmpResult ;
		}
		#endregion

		//PRIVATE METHODS
		
		#region CropAndDescewDir()
		private static int CropAndDescewDir(DirectoryInfo sourceDir, DirectoryInfo destDir, Color color, 
			short jpegCompression, float minAngleToDescew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, int flags)
		{
			ArrayList	sources = new ArrayList(); 
			Bitmap		bitmap;
			TimeSpan	span = new TimeSpan(0);
			DateTime	totalTimeStart = DateTime.Now ;
			byte		confidence = 0;

			destDir.Create();

			sources.AddRange(sourceDir.GetFiles("*.tif"));
			sources.AddRange(sourceDir.GetFiles("*.jpg"));
			sources.AddRange(sourceDir.GetFiles("*.png"));
			sources.AddRange(sourceDir.GetFiles("*.bmp"));
			sources.AddRange(sourceDir.GetFiles("*.gif"));

			foreach(FileInfo file in sources)
			{
				bitmap = new Bitmap(file.FullName) ;
				DateTime	start = DateTime.Now ;
				Bitmap		result = Go(bitmap, color, true, out confidence, minAngleToDescew, clip, removeGhostLines, lowThreshold, highThreshold, linesToCheck, maxDelta, flags);
				TimeSpan	time = DateTime.Now.Subtract(start) ;
				span = span.Add(time);
				ImageCodecInfo		codecInfo = Encoding.GetCodecInfo(bitmap);
				EncoderParameters	encoderParams = Encoding.GetEncoderParams(bitmap, jpegCompression) ;

#if DEBUG
				Console.WriteLine(string.Format("{0}: {1}, Confidence:{2}%",  file.FullName, time.ToString(), confidence)) ;
#endif
				bitmap.Dispose() ;

				if(File.Exists(destDir.FullName +  @"\" + file.Name))
					File.Delete(destDir.FullName +  @"\" + file.Name);
				
				result.Save(destDir.FullName +  @"\" + file.Name, codecInfo, encoderParams) ;
				result.Dispose() ;
			}

#if DEBUG
			Console.WriteLine("Total time: " + span.ToString());
			Console.WriteLine("Total all time: " + DateTime.Now.Subtract(totalTimeStart).ToString());
#endif
			return confidence;
		}
		#endregion

		#region CropAndDescewFile()
		private static int CropAndDescewFile(FileInfo sourceFile, FileInfo resultFile, Color color, 
			short jpegCompression, float minAngleToDescew, Rectangle clip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, int flags)
		{			
			byte			confidence;
			Bitmap			bitmap = new Bitmap(sourceFile.FullName) ;

			DateTime	start = DateTime.Now ;

			Bitmap		result = Go(bitmap, color, true, out confidence, minAngleToDescew, clip, removeGhostLines, lowThreshold, 
				highThreshold, linesToCheck, maxDelta, flags);
			TimeSpan	time = DateTime.Now.Subtract(start) ;

#if DEBUG
			Console.WriteLine(string.Format("{0}: {1}, Confidence:{2}%",  sourceFile.FullName, time.ToString(), confidence)) ;
#endif
				
			ImageCodecInfo		codecInfo = Encoding.GetCodecInfo(bitmap);
			EncoderParameters	encoderParams = Encoding.GetEncoderParams(bitmap, jpegCompression) ;
				
			bitmap.Dispose() ;

			if(resultFile.Exists)
				resultFile.Delete();
				
			result.Save(resultFile.FullName, codecInfo, encoderParams) ;

			result.Dispose() ;
			return confidence;
		}
		#endregion
		
		#region CropAndDescew24bpp()
		private static Bitmap CropAndDescew24bpp(Bitmap sourceBmp, Color threshold, bool backDark, 
			out byte confidence, float minAngleToDescew, Rectangle imageClip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, int flags)
		{
			BitmapData	sourceData = null;
			
			try
			{
				Rectangle	imageRect = new Rectangle(0, 0, sourceBmp.Width, sourceBmp.Height);
				Bitmap		bwBitmap = new Bitmap(sourceBmp.Width, sourceBmp.Height, PixelFormat.Format1bppIndexed);
				BitmapData	bwData = bwBitmap.LockBits(imageRect, ImageLockMode.ReadWrite, bwBitmap.PixelFormat); 

				sourceData = sourceBmp.LockBits(imageRect, ImageLockMode.ReadOnly, sourceBmp.PixelFormat);
				BinorizationThreshold.Binorize24bpp(sourceData, bwData, imageClip, threshold.R, threshold.G, threshold.B);

				if(removeGhostLines)
				{
					int[]		ghostLines = GhostLinesRemoval.Get(sourceData, lowThreshold, highThreshold, linesToCheck, maxDelta);

					if(ghostLines.Length > 0)
						RemoveGhostLines(bwData, ghostLines);
				}
				
				NoiseReduction.Despeckle1x11bpp(bwData, imageRect);
				NoiseReduction.Despeckle2x21bpp(bwData, imageRect);
				NoiseReduction.Despeckle3x31bpp(bwData, imageRect);
				NoiseReduction.Despeckle4x41bpp(bwData, imageRect);
				NoiseReduction.Despeckle2x21bpp(bwData, imageRect);
			
				Rect		clip = FindClip(bwData, minAngleToDescew);
				Bitmap		resultBmp = null;

				bwBitmap.UnlockBits(bwData);
				//bwBitmap.Save(@"c:\temp\CoverBW.tif", ImageFormat.Tiff);

				if(clip.Inclined)
				{
					if(IsEntireClipInsideSource(sourceBmp.Size, clip))
					{
						if((flags & 1) == 0)
							resultBmp = GetClip24bpp(sourceData, clip);
						else
							resultBmp = GetClip24bppQuality(sourceData, clip);

						switch(clip.ValidCorners)
						{
							case 0: confidence = 0; break;
							case 1: confidence = 30; break;
							case 2:
							case 3: confidence = 80; break;
							default: confidence = 100; break;
						}
					}
					else
					{
						if((flags & 1) == 0)
							resultBmp = GetClipCheckBorders24bpp(sourceData, clip);
						else
							resultBmp = GetClipCheckBorders24bppQuality(sourceData, clip);

						switch(clip.ValidCorners)
						{
							case 0: confidence = 0; break;
							case 1: confidence = 20; break;
							default: confidence = 50; break;
						}
					}
				}
				else
				{
					Rectangle	rect = new Rectangle(clip.UlCorner.X, clip.UlCorner.Y, clip.Width, clip.Height);

					if ((rect.Location != imageClip.Location) || (rect.Size != imageClip.Size))
					{
						if(rect.X == imageClip.X)
						{
							rect.Width = rect.Width + rect.X;
							rect.X = 0;
						}
						if(rect.Y == imageClip.Y)
						{
							rect.Height = rect.Height + rect.Y;
							rect.Y = 0;
						}

						if(rect.Right >= imageClip.Right)
							rect.Width = sourceBmp.Width - rect.X;

						if(rect.Bottom >= imageClip.Bottom)
							rect.Height = sourceBmp.Height - rect.Y;
								
						resultBmp = ImageProcessing.CopyImage.Copy(sourceBmp, sourceData, rect);
					}
					else
						resultBmp = ImageProcessing.CopyImage.Copy(sourceBmp, sourceData);

					switch(clip.ValidCorners)
					{
						case 0: confidence = 0; break;
						case 1: confidence = 30; break;
						case 2:
						case 3: confidence = 80; break;
						default: confidence = 100; break;
					}
				}

				return resultBmp; 
			}
			finally
			{
				if(sourceData != null)
					sourceBmp.UnlockBits(sourceData);
			}
		}
		#endregion

		#region CropAndDescew24bppMem()
		private static int CropAndDescew24bppMem(Bitmap sourceBmp, ref int width, ref int height, ref int stride, 
			ref IntPtr scan0, Color threshold, bool backDark, float minAngleToDescew, Rectangle imageClip, 
			bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta)
		{
			/*BitmapData	sourceData = null;
			
			try
			{
				byte		confidence = 0;
				Rectangle	imageRect = new Rectangle(0, 0, sourceBmp.Width, sourceBmp.Height);
				Bitmap		bwBitmap = new Bitmap(sourceBmp.Width, sourceBmp.Height, PixelFormat.Format1bppIndexed);
				BitmapData	bwData = bwBitmap.LockBits(imageRect, ImageLockMode.ReadWrite, bwBitmap.PixelFormat); 

				sourceData = sourceBmp.LockBits(imageRect, ImageLockMode.ReadOnly, sourceBmp.PixelFormat); 
				
				BinorizationThreshold.Binorize24bpp(sourceData, bwData, imageClip, threshold.R, threshold.G, threshold.B);

				if(removeGhostLines)
				{
					int[]	ghostLines = GhostLinesRemoval.Get(sourceData, lowThreshold, highThreshold, linesToCheck, maxDelta);

					if(ghostLines.Length > 0)
						RemoveGhostLines(bwData, ghostLines);
				}

				NoiseReduction.Despeckle1x11bpp(bwData, imageRect);
				NoiseReduction.Despeckle2x21bpp(bwData, imageRect);
				NoiseReduction.Despeckle3x31bpp(bwData, imageRect);
				NoiseReduction.Despeckle4x41bpp(bwData, imageRect);
				NoiseReduction.Despeckle2x21bpp(bwData, imageRect);
			
				Rect		clip = FindClip(bwData, minAngleToDescew);

				bwBitmap.UnlockBits(bwData);
				//bwBitmap.Save(@"D:\Projects\tns\result3\0000bw.jpg", ImageFormat.Jpeg);

				if(clip.Inclined)
				{
					if(IsEntireClipInsideSource(sourceBmp.Size, clip))
					{
						if(GetClip24bppMem(sourceData, clip, ref width, ref height, ref stride, ref scan0))
						{
							switch(clip.ValidCorners)
							{
								case 0: confidence = 0; break;
								case 1: confidence = 30; break;
								case 2:
								case 3: confidence = 80; break;
								default: confidence = 100; break;
							}
						}
					}
					else
					{
						if(GetClipCheckBorders24bppMem(sourceData, clip, ref width, ref height, ref stride, ref scan0))
						{
							switch(clip.ValidCorners)
							{
								case 0: confidence = 0; break;
								case 1: confidence = 20; break;
								default: confidence = 50; break;
							}
						}
					}
				}
				else
				{
					Rectangle	rect = new Rectangle(clip.UlCorner.X, clip.UlCorner.Y, clip.Width, clip.Height);

					if ((rect.Location != imageClip.Location) || (rect.Size != imageClip.Size))
					{
						if (rect.X == imageClip.X)
						{
							rect.Width = rect.Width + rect.X;
							rect.X = 0;
						}
						if (rect.Y == imageClip.Y)
						{
							rect.Height = rect.Height + rect.Y;
							rect.Y = 0;
						}

						if (rect.Right >= imageClip.Right)
							rect.Width = sourceBmp.Width - rect.X;

						if (rect.Bottom >= imageClip.Bottom)
							rect.Height = sourceBmp.Height - rect.Y;
					}
					else
						rect = imageRect;

					width = rect.Width;
					height = rect.Height;
					stride = GetStride(width, sourceData.PixelFormat);
					scan0 = CopyData(sourceData, rect);
					
					switch(clip.ValidCorners)
					{
						case 0: confidence = 0; break;
						case 1: confidence = 30; break;
						case 2:
						case 3: confidence = 80; break;
						default: confidence = 100; break;
					}
				}

				return confidence; 
			}
			finally
			{
				if(sourceData != null)
					sourceBmp.UnlockBits(sourceData);
			}*/
			return 0;
		}
		#endregion
		
		#region CropAndDescew8bpp()
		private static Bitmap CropAndDescew8bpp(Bitmap sourceBmp, Color threshold, bool backDark, 
			out byte confidence, float minAngleToDescew, Rectangle imageClip, bool removeGhostLines, byte lowThreshold,
			byte highThreshold, byte linesToCheck, byte maxDelta, int flags)
		{
			BitmapData	sourceData = null;

			try
			{
				Rectangle	imageRect = new Rectangle(0, 0, sourceBmp.Width, sourceBmp.Height);
				Bitmap		bwBitmap = new Bitmap(sourceBmp.Width, sourceBmp.Height, PixelFormat.Format1bppIndexed);
				BitmapData	bwData = bwBitmap.LockBits(imageRect, ImageLockMode.ReadWrite, bwBitmap.PixelFormat); 

				sourceData = sourceBmp.LockBits(imageRect, ImageLockMode.ReadOnly, sourceBmp.PixelFormat); 

				if(ImageInfo.IsPaletteGrayscale(sourceBmp.Palette.Entries))
					BinorizationThreshold.Binorize8bppGrayscale(sourceData, bwData, imageClip, threshold.R);
				else
					BinorizationThreshold.Binorize8bpp(sourceData, sourceBmp.Palette.Entries, bwData, imageClip, threshold.R);

				if(removeGhostLines)
				{
					int[]		ghostLines = GhostLinesRemoval.Get(sourceData, lowThreshold, highThreshold, linesToCheck, maxDelta);

					if(ghostLines.Length > 0)
						RemoveGhostLines(bwData, ghostLines);
				}
				
				NoiseReduction.Despeckle1x11bpp(bwData, imageRect);
				NoiseReduction.Despeckle2x21bpp(bwData, imageRect);
				NoiseReduction.Despeckle3x31bpp(bwData, imageRect);
				NoiseReduction.Despeckle4x41bpp(bwData, imageRect);
				NoiseReduction.Despeckle2x21bpp(bwData, imageRect);
				NoiseReduction.Despeckle1x11bpp(bwData, imageRect);
			
				Rect		clip = FindClip(bwData, minAngleToDescew);
				Bitmap		resultBmp = null;

				bwBitmap.UnlockBits(bwData);
				//bwBitmap.Save(@"D:\Projects\tns\result4\0000bw.jpg", ImageFormat.Jpeg);

				if(clip.Inclined)
				{
					if(IsEntireClipInsideSource(sourceBmp.Size, clip))
					{
						if((flags & 1) == 0)
							resultBmp = GetClip8bpp(sourceData, clip);
						else
							resultBmp = GetClip8bppQuality(sourceData, clip);

						switch(clip.ValidCorners)
						{
							case 0: confidence = 0; break;
							case 1: confidence = 30; break;
							case 2:
							case 3: confidence = 80; break;
							default: confidence = 100; break;
						}
					}
					else
					{
						if((flags & 1) == 0)
							resultBmp = GetClipCheckBorders8bpp(sourceData, clip);
						else
							resultBmp = GetClipCheckBorders8bppQuality(sourceData, clip);

						switch(clip.ValidCorners)
						{
							case 0: confidence = 0; break;
							case 1: confidence = 20; break;
							default: confidence = 50; break;
						}
					}
				}
				else
				{
					Rectangle	rect = new Rectangle(clip.UlCorner.X, clip.UlCorner.Y, clip.Width, clip.Height);

					if ((rect.Location != imageClip.Location) || (rect.Size != imageClip.Size))
					{
						if(rect.X == imageClip.X)
						{
							rect.Width = rect.Width + rect.X;
							rect.X = 0;
						}
						if(rect.Y == imageClip.Y)
						{
							rect.Height = rect.Height + rect.Y;
							rect.Y = 0;
						}

						if(rect.Right >= imageClip.Right)
							rect.Width = sourceBmp.Width - rect.X;

						if(rect.Bottom >= imageClip.Bottom)
							rect.Height = sourceBmp.Height - rect.Y;

						resultBmp = ImageProcessing.CopyImage.Copy(sourceBmp, sourceData, rect);
					}
					else
						resultBmp = ImageProcessing.CopyImage.Copy(sourceBmp, sourceData);

					switch(clip.ValidCorners)
					{
						case 0: confidence = 0; break;
						case 1: confidence = 30; break;
						case 2:
						case 3: confidence = 80; break;
						default: confidence = 100; break;
					}
				}

				resultBmp.Palette = sourceBmp.Palette;
				return resultBmp; 
			}
			finally
			{
				if(sourceData != null)
					sourceBmp.UnlockBits(sourceData);
			}
		}
		#endregion

		#region CropAndDescew8bppMem()
		private static int CropAndDescew8bppMem(Bitmap sourceBmp, ref int width, ref int height, 
			ref int stride, ref IntPtr scan0, Color threshold, bool backDark, float minAngleToDescew, Rectangle imageClip, 
			bool removeGhostLines, byte lowThreshold, byte highThreshold, byte linesToCheck, byte maxDelta)
		{
			/*int			confidence = 0;
			BitmapData	sourceData = null;

			try
			{
				Rectangle	imageRect = new Rectangle(0, 0, sourceBmp.Width, sourceBmp.Height);
				Bitmap		bwBitmap = new Bitmap(sourceBmp.Width, sourceBmp.Height, PixelFormat.Format1bppIndexed);
				BitmapData	bwData = bwBitmap.LockBits(imageRect, ImageLockMode.ReadWrite, bwBitmap.PixelFormat); 

				sourceData = sourceBmp.LockBits(imageRect, ImageLockMode.ReadOnly, sourceBmp.PixelFormat); 

				//BinorizationThreshold.Binorize8bpp(sourceData, sourceBmp.Palette.Entries, bwData, imageClip, threshold.R);
				if(ImageInfo.IsPaletteGrayscale(sourceBmp.Palette.Entries))
					BinorizationThreshold.Binorize8bppGrayscale(sourceData, bwData, imageClip, threshold.R);
				else
					BinorizationThreshold.Binorize8bpp(sourceData, sourceBmp.Palette.Entries, bwData, imageClip, threshold.R);


				if(removeGhostLines)
				{
					int[]		ghostLines = GhostLinesRemoval.Get(sourceData, lowThreshold, highThreshold, linesToCheck, maxDelta);

					if(ghostLines.Length > 0)
						RemoveGhostLines(bwData, ghostLines);
				}
				
				NoiseReduction.Despeckle1x11bpp(bwData, imageRect);
				NoiseReduction.Despeckle2x21bpp(bwData, imageRect);
				NoiseReduction.Despeckle3x31bpp(bwData, imageRect);
				NoiseReduction.Despeckle4x41bpp(bwData, imageRect);
				NoiseReduction.Despeckle2x21bpp(bwData, imageRect);
				NoiseReduction.Despeckle1x11bpp(bwData, imageRect);
			
				Rect		clip = FindClip(bwData, minAngleToDescew);

				bwBitmap.UnlockBits(bwData);
				//bwBitmap.Save(@"D:\Projects\tns\result4\0000bw.jpg", ImageFormat.Jpeg);

				if(clip.Inclined)
				{
					if(IsEntireClipInsideSource(sourceBmp.Size, clip))
					{
						if(GetClip8bppMem(sourceData, clip, ref width, ref height, ref stride, ref scan0))
						{
							switch(clip.ValidCorners)
							{
								case 0: confidence = 0; break;
								case 1: confidence = 30; break;
								case 2:
								case 3: confidence = 80; break;
								default: confidence = 100; break;
							}
						}
					}
					else
					{
						if(GetClipCheckBorders8bppMem(sourceData, clip, ref width, ref height, ref stride, ref scan0))
						{
							switch(clip.ValidCorners)
							{
								case 0: confidence = 0; break;
								case 1: confidence = 20; break;
								default: confidence = 50; break;
							}
						}
					}
				}
				else
				{
					Rectangle	rect = new Rectangle(clip.UlCorner.X, clip.UlCorner.Y, clip.Width, clip.Height);

					if ((rect.Location != imageClip.Location) || (rect.Size != imageClip.Size))
					{
						if (rect.X == imageClip.X)
						{
							rect.Width = rect.Width + rect.X;
							rect.X = 0;
						}
						if (rect.Y == imageClip.Y)
						{
							rect.Height = rect.Height + rect.Y;
							rect.Y = 0;
						}

						if (rect.Right >= imageClip.Right)
							rect.Width = sourceBmp.Width - rect.X;

						if (rect.Bottom >= imageClip.Bottom)
							rect.Height = sourceBmp.Height - rect.Y;
					}
					else
						rect = imageRect;
					
					width = rect.Width;
					height = rect.Height;
					stride = GetStride(width, sourceData.PixelFormat);
					scan0 = CopyData(sourceData, rect);

					switch(clip.ValidCorners)
					{
						case 0: confidence = 0; break;
						case 1: confidence = 30; break;
						case 2:
						case 3: confidence = 80; break;
						default: confidence = 100; break;
					}
				}
			}
			finally
			{
				if(sourceData != null)
					sourceBmp.UnlockBits(sourceData);
			}

			return confidence;*/
			return 0;
		}
		#endregion

		#region FindClip()
		private static Rect FindClip(BitmapData bmpData, float minAngleToDescew)
		{
			Crop		clip = FindSmallestClip(bmpData);
			Rectangle	rect = GetRectangle(clip);
			rect.Inflate(8, 8);
			rect.X = Math.Max(0, rect.X);
			rect.Y = Math.Max(0, rect.Y);
			rect.Width = Math.Min(bmpData.Width - rect.X, rect.Width);
			rect.Height = Math.Min(bmpData.Height - rect.Y, rect.Height);

			Point		ulCorner = FindUlCorner(bmpData, rect.Location);
			Point		urCorner = FindUrCorner(bmpData, new Point(rect.Right, rect.Top));
			Point		llCorner = FindLlCorner(bmpData, new Point(rect.Left, rect.Bottom));
			Point		lrCorner = FindLrCorner(bmpData, new Point(rect.Right, rect.Bottom));

			Rect		crop = new Rect(ulCorner, urCorner, llCorner, lrCorner, clip, minAngleToDescew);

			Crop	crop2 = FindSmallestClip(bmpData, crop.Angle);
			crop2 = crop2;

			return crop;
		}
		#endregion
		
		#region FindSmallestClip()
		private static Crop FindSmallestClip(BitmapData bmpData)
		{
			Crop		clip = new Crop(bmpData.Width, bmpData.Height);			
			int			width = bmpData.Width;
			int			height = bmpData.Height;			
			int			stride = bmpData.Stride; 
			int			x, y;

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte*	pCurrent ;
				int		whiteCount;
				byte	mask;

				for(y = 0; y < height; y++) 
				{ 
					pCurrent = pSource + (y * stride);
					
					for(x = 0; x < width; x = x + 8)
					{
						whiteCount = 0;
						for(int i = 0; i < 8; i++)
							whiteCount += ((*pCurrent >> i) & 1);

						if(whiteCount > 4)
						{
							clip.Top = new Point(x, y);
							y = height;
							break;
						}
						else
							pCurrent++;
					}
				}

				for(y = height - 1; y > clip.Top.Y; y--) 
				{ 
					pCurrent = pSource + (y * stride);
					
					for(x = 0; x < width; x = x + 8)
					{
						whiteCount = 0;
						for(int i = 0; i < 8; i++)
							whiteCount += ((*pCurrent >> i) & 1);

						if(whiteCount > 4)
						{
							clip.Bottom = new Point(x, y+1);
							y = -1;
							break;
						}
						else
							pCurrent++;
					}
				}

				int		bottom = (height - 8 < clip.Bottom.Y) ? height - 8 : clip.Bottom.Y;

				for(x = 0; x < width; x++) 
				{ 
					mask = (byte) (0x80 >> (x % 8));
					pCurrent = pSource + (clip.Top.Y * stride) + (x >> 3);
					
					for(y = clip.Top.Y; y < bottom; y = y + 8)
					{
						whiteCount = 0;
						for(int i = 0; i < 8; i++)
							whiteCount += ((*(pCurrent + i * stride)) & mask);

						if(whiteCount > 4)
						{
							clip.Left = new Point(x, y);
							x = width;
							break;
						}
						else
							pCurrent += 8 * stride;
					}
				}

				for(x = width - 1; x > clip.Left.X; x--) 
				{ 
					mask = (byte) (0x80 >> (x % 8));
					pCurrent = pSource + (clip.Top.Y * stride) + (x >> 3);
					
					for(y = clip.Top.Y; y < bottom; y = y + 8)
					{
						whiteCount = 0;
						for(int i = 0; i < 8; i++)
							whiteCount += ((*(pCurrent + i * stride)) & mask);

						if(whiteCount > 4)
						{
							clip.Right = new Point(x+1, y);
							x = -1;
							break;
						}
						else
							pCurrent += 8 * stride;
					}
				}
	
				return clip; 
			}
		}
		#endregion

		#region FindSmallestClip()
		private static Crop FindSmallestClip(BitmapData bmpData, double angle)
		{
			if(angle < 0.000001 && angle > -0.000001)
				return FindSmallestClip(bmpData);				
			
			Crop		clip = new Crop(bmpData.Width, bmpData.Height);			
			int			width = bmpData.Width;
			int			height = bmpData.Height;			
			int			stride = bmpData.Stride; 
			int			x, y;
			
			double	xJump = 1 / Math.Tan(angle * Math.PI / 180);
			double	yJump = Math.Tan(angle * Math.PI / 180);

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte*	pCurrent ;
				int		whiteCount;
				byte	mask;

				//top
				clip.Top = FindClipTop(bmpData, angle);
				clip.Bottom = FindClipBottom(bmpData, angle);

				int		bottom = (height - 8 < clip.Bottom.Y) ? height - 8 : clip.Bottom.Y;

				for(x = 0; x < width; x++) 
				{ 
					mask = (byte) (0x80 >> (x % 8));
					pCurrent = pSource + (clip.Top.Y * stride) + (x >> 3);
					
					for(y = clip.Top.Y; y < bottom; y = y + 8)
					{
						whiteCount = 0;
						for(int i = 0; i < 8; i++)
							whiteCount += ((*(pCurrent + i * stride)) & mask);

						if(whiteCount > 2)
						{
							clip.Left = new Point(x, y);
							x = width;
							break;
						}
						else
							pCurrent += 8 * stride;
					}
				}

				for(x = width - 1; x > clip.Left.X; x--) 
				{ 
					mask = (byte) (0x80 >> (x % 8));
					pCurrent = pSource + (clip.Top.Y * stride) + (x >> 3);
					
					for(y = clip.Top.Y; y < bottom; y = y + 8)
					{
						whiteCount = 0;
						for(int i = 0; i < 8; i++)
							whiteCount += ((*(pCurrent + i * stride)) & mask);

						if(whiteCount > 2)
						{
							clip.Right = new Point(x+1, y);
							x = -1;
							break;
						}
						else
							pCurrent += 8 * stride;
					}
				}
	
				return clip; 
			}
		}
		#endregion
		
		#region IsClipDescewed()
		private static bool IsClipDescewed(BitmapData bmpData, Crop clip)
		{
			return false;			
		}
		#endregion

		#region GetSmallestRectangle()
		private static Crop GetSmallestRectangle(BitmapData bmpData, Crop clip)
		{
			return new Crop(0, 0);			
		}
		#endregion

		#region GetRectangle()
		private static Rectangle GetRectangle(Crop clip)
		{
			return Rectangle.FromLTRB(clip.Left.X, clip.Top.Y, clip.Right.X, clip.Bottom.Y);			
		}
		#endregion

		#region FindUlCorner()
		private static Point FindUlCorner(BitmapData bmpData, Point startPoint)
		{
			int			maxSteps = Math.Min(bmpData.Width - startPoint.X, bmpData.Height - startPoint.Y);
			int			stride = bmpData.Stride; 
			int			x, y;

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte	mask;
				int		i = 0, j = 0;

				while(i < maxSteps)
				{

					for(j = 0; j <= i; j++)
					{
						x = startPoint.X + j;
						y = startPoint.Y + (i - j);

						mask = (byte) (0x80 >> (x % 8));
						if( (x >= 0) && (y >= 0) && (x < bmpData.Width - 8) && (y < bmpData.Height) && (*(pSource + (y * stride) + (x >> 3)) & mask) > 0)
							return new Point(x, y);
					}

					i++;
				}
			}
			
			return startPoint; 
		}
		#endregion

		#region FindUrCorner()
		private static Point FindUrCorner(BitmapData bmpData, Point startPoint)
		{
			int			maxSteps = Math.Min(startPoint.X, bmpData.Height - startPoint.Y);
			int			stride = bmpData.Stride; 
			int			x, y;

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte	mask;
				int		i = 0, j = 0;

				while(i < maxSteps)
				{

					for(j = 0; j <= i; j++)
					{
						x = startPoint.X - j;
						y = startPoint.Y + (i - j);

						mask = (byte) (0x80 >> (x % 8));
						if( (x >= 0) && (y >= 0) && (x < bmpData.Width - 8) && (y < bmpData.Height) && (*(pSource + (y * stride) + (x >> 3)) & mask) > 0)
							return new Point(x, y);
					}

					i++;
				}
			}
			
			return startPoint; 
		}
		#endregion

		#region FindLlCorner()
		private static Point FindLlCorner(BitmapData bmpData, Point startPoint)
		{
			int			maxSteps = Math.Min(bmpData.Width - startPoint.X, startPoint.Y);
			int			stride = bmpData.Stride; 
			int			x, y;

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte	mask;
				int		i = 0, j = 0;

				while(i < maxSteps)
				{

					for(j = 0; j <= i; j++)
					{
						x = startPoint.X + j;
						y = startPoint.Y - (i - j);

						mask = (byte) (0x80 >> (x % 8));
						if( (x >= 0) && (y >= 0) && (x < bmpData.Width - 8) && (y < bmpData.Height) && (*(pSource + (y * stride) + (x >> 3)) & mask) > 0)
							return new Point(x, y);
					}

					i++;
				}
			}
			
			return startPoint; 
		}
		#endregion

		#region FindLrCorner()
		private static Point FindLrCorner(BitmapData bmpData, Point startPoint)
		{
			int			maxSteps = Math.Min(startPoint.X, startPoint.Y);
			int			stride = bmpData.Stride; 
			int			x, y;

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte	mask;
				int		i = 0, j = 0;

				while(i < maxSteps)
				{

					for(j = 0; j <= i; j++)
					{
						x = startPoint.X - j;
						y = startPoint.Y - (i - j);

						mask = (byte) (0x80 >> (x % 8));
						if( (x >= 0) && (y >= 0) && (x < bmpData.Width - 8) && (y < bmpData.Height) && (*(pSource + (y * stride) + (x >> 3)) & mask) > 0)
							return new Point(x, y);
					}

					i++;
				}
			}
			
			return startPoint; 
		}
		#endregion

		#region GetClip24bpp()
		private static Bitmap GetClip24bpp(BitmapData sourceData, Rect clip)
		{
			int			x, y;
			int			width = clip.Width;
			int			height = clip.Height;
			
			Bitmap		result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride; 

			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			try
			{
				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					int		sourceXOffset = ulCornerX;
					int		sourceYOffset = clip.UlCorner.Y;
					int		currentXOffset = sourceXOffset;
					int		currentYOffset = sourceYOffset;
					double	xJump =  (clip.LlCorner.X - ulCornerX) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Angle * Math.PI / 180));
					int		yTmp;
				
					byte*	pOrigCurrent;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						currentXOffset = (int) (y * xJump);
						pOrigCurrent = pSource + (y + ulCornerY) * sStride + (ulCornerX + currentXOffset) * 3;
						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							*(pCopyCurrent++) = *(pOrigCurrent++);
							*(pCopyCurrent++) = *(pOrigCurrent++);
							*(pCopyCurrent++) = *(pOrigCurrent++);

							yTmp = Math.Max(0, (y + ulCornerY + ((int) (yJump * x))));
							pOrigCurrent = pSource + yTmp * sStride + (ulCornerX + currentXOffset + x) * 3;
							//pOrigCurrent = pSource + (y + clip.UlCorner.Y + ((int) (yJump * x))) * sStride + currentXOffset * 3;
						}

					}
				}
			}
			catch(Exception ex)
			{
				ex = ex;
			}

			result.UnlockBits(resultData);

			return result;
		}
		#endregion
		
		#region GetClip24bppQuality()
		private static Bitmap GetClip24bppQuality(BitmapData sourceData, Rect clip)
		{
			int			x, y;
			int			width = clip.Width;
			int			height = clip.Height;
			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			Bitmap		result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride; 

			try
			{
				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					double	sourceX;
					double	sourceY;
					double	xJump =  (clip.LlCorner.X - ulCornerX) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Angle * Math.PI / 180));
					double	xRest, yRest;
				
					byte*	pOrigCurrent;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						sourceX = ulCornerX + y * xJump;
						sourceY = ulCornerY + y;

						xRest = sourceX - (int) sourceX;
						if(xRest < 0)
							xRest += 1;

						if(xRest < 0.000001)
							xRest = 0;
						if(xRest > .999999)
						{
							sourceX = (int) sourceX + 1;
							xRest = 0;
						}

						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							yRest = sourceY - (int) sourceY;
							if(yRest < 0)
								yRest += 1;

							if(yRest < 0.000001)
								yRest = 0;
							if(yRest > .999999)
							{
								sourceY = (int) sourceX + 1;
								yRest = 0;
							}
							
							pOrigCurrent = pSource + (int) sourceY * sStride + ((int) sourceX) * 3;
							
							if(xRest == 0)
							{
								if(yRest == 0)
								{
									*(pCopyCurrent++) = *(pOrigCurrent++);
									*(pCopyCurrent++) = *(pOrigCurrent++);
									*(pCopyCurrent++) = *(pOrigCurrent++);
								}
								else
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
									pOrigCurrent++;
								}
							}
							else
							{
								if(yRest == 0)
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-xRest) + pOrigCurrent[3] * xRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-xRest) + pOrigCurrent[3] * xRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-xRest) + pOrigCurrent[3] * xRest);
									pOrigCurrent++;
								}
								else
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-yRest) * (1-xRest) + 
										pOrigCurrent[3] * xRest * (1-yRest) +
										pOrigCurrent[sStride] * (1-xRest) * yRest + 
										pOrigCurrent[sStride+3] * xRest * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-yRest) * (1-xRest) + 
										pOrigCurrent[3] * xRest * (1-yRest) +
										pOrigCurrent[sStride] * (1-xRest) * yRest + 
										pOrigCurrent[sStride+3] * xRest * yRest);
									pOrigCurrent++;
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-yRest) * (1-xRest) + 
										pOrigCurrent[3] * xRest * (1-yRest) +
										pOrigCurrent[sStride] * (1-xRest) * yRest + 
										pOrigCurrent[sStride+3] * xRest * yRest);
									pOrigCurrent++;
								}
							}
							

							sourceX += 1;
							sourceY += yJump;
						}

					}
				}
			}
			catch(Exception ex)
			{
				ex = ex;
			}

			result.UnlockBits(resultData);

			return result;
		}
		#endregion
		
		#region GetClip24bppMem()
		/*private static bool GetClip24bppMem(BitmapData sourceData, Rect clip, ref int width, ref int height, 
			ref int rStride, ref IntPtr scan0)
		{
			int			x, y;
			int			sStride = sourceData.Stride; 
			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;
			
			width = clip.Width;
			height = clip.Height;
			rStride = GetStride(width, sourceData.PixelFormat); 
			
			try
			{
				unsafe
				{
					scan0 = new IntPtr(AllocHeapMemory(rStride * height));
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult =  (byte*) scan0.ToPointer();

					int		sourceXOffset = clip.UlCorner.X;
					int		sourceYOffset = clip.UlCorner.Y;
					int		currentXOffset = sourceXOffset;
					int		currentYOffset = sourceYOffset;
					double	xJump =  (clip.LlCorner.X - ulCornerX) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Angle * Math.PI / 180));
					int		yTmp;
				
					byte*	pOrigCurrent;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						currentXOffset = (int) (y * xJump);
						pOrigCurrent = pSource + (y + ulCornerY) * sStride + (ulCornerX + currentXOffset) * 3;
						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							*(pCopyCurrent++) = *(pOrigCurrent++);
							*(pCopyCurrent++) = *(pOrigCurrent++);
							*(pCopyCurrent++) = *(pOrigCurrent++);

							yTmp = Math.Max(0, (y + ulCornerY + ((int) (yJump * x))));
							pOrigCurrent = pSource + yTmp * sStride + (ulCornerX + currentXOffset + x) * 3;
						}

					}
				}
			}
			catch(Exception ex)
			{
				ex = ex;
				return false;
			}
		
			return true;
		}*/
		#endregion

		#region GetClip8bpp()
		private static Bitmap GetClip8bpp(BitmapData sourceData, Rect clip)
		{
			int			x, y;
			int			width = clip.Width;
			int			height = clip.Height;
			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			Bitmap		result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride; 

			try
			{
				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					int		sourceXOffset = ulCornerX;
					int		sourceYOffset = ulCornerY;
					int		currentXOffset = sourceXOffset;
					int		currentYOffset = sourceYOffset;
					double	xJump =  (clip.LlCorner.X - ulCornerX) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Angle * Math.PI / 180));
					int		yTmp;
				
					byte*	pOrigCurrent;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						currentXOffset = (int) (y * xJump);
						pOrigCurrent = pSource + (y + ulCornerY) * sStride + (ulCornerX + currentXOffset);
						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							*(pCopyCurrent++) = *(pOrigCurrent++);

							yTmp = y + ulCornerY + ((int) (yJump * x));
							pOrigCurrent = pSource + yTmp * sStride + (ulCornerX + currentXOffset + x);
						}

					}
				}
			}
			catch(Exception ex)
			{
				ex = ex;
			}

			result.UnlockBits(resultData);

			return result;
		}
		#endregion

		#region GetClip8bppQuality()
		private static Bitmap GetClip8bppQuality(BitmapData sourceData, Rect clip)
		{
			int			x, y;
			int			width = clip.Width;
			int			height = clip.Height;
			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			Bitmap		result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride; 

			try
			{
				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					double	sourceX;
					double	sourceY;
					double	xJump =  (clip.LlCorner.X - ulCornerX) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Angle * Math.PI / 180));
					double	xRest, yRest;
				
					byte*	pOrigCurrent;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						sourceX = ulCornerX + y * xJump;
						sourceY = ulCornerY + y;

						xRest = sourceX - (int) sourceX;
						if(xRest < 0)
							xRest += 1;

						if(xRest < 0.000001)
							xRest = 0;
						if(xRest > .999999)
						{
							sourceX = (int) sourceX + 1;
							xRest = 0;
						}

						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							yRest = sourceY - (int) sourceY;
							if(yRest < 0)
								yRest += 1;

							if(yRest < 0.000001)
								yRest = 0;
							if(yRest > .999999)
							{
								sourceY = (int) sourceX + 1;
								yRest = 0;
							}
							
							pOrigCurrent = pSource + (int) sourceY * sStride + (int) sourceX;
							
							if(xRest == 0)
							{
								if(yRest == 0)
								{
									*(pCopyCurrent++) = *pOrigCurrent;
								}
								else
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
								}
							}
							else
							{
								if(yRest == 0)
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-xRest) + pOrigCurrent[1] * xRest);
								}
								else
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-yRest) * (1-xRest) + 
										pOrigCurrent[1] * xRest * (1-yRest) +
										pOrigCurrent[sStride] * (1-xRest) * yRest + 
										pOrigCurrent[sStride+1] * xRest * yRest);
								}
							}
							

							sourceX += 1;
							sourceY += yJump;
						}

					}
				}
			}
			catch(Exception ex)
			{
				ex = ex;
			}

			result.UnlockBits(resultData);

			return result;
		}
		#endregion

		#region GetClip8bppMem()
		/*private static bool GetClip8bppMem(BitmapData sourceData, Rect clip, ref int width, ref int height, 
			ref int rStride, ref IntPtr scan0)
		{
			int			x, y;
			int			sStride = sourceData.Stride; 
			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			width = clip.Width;
			height = clip.Height;
			rStride = GetStride(width, sourceData.PixelFormat); 

			try
			{
				unsafe
				{
					scan0 = new IntPtr(AllocHeapMemory(rStride * height));
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) scan0.ToPointer();

					int		sourceXOffset = ulCornerX;
					int		sourceYOffset = ulCornerY;
					int		currentXOffset = sourceXOffset;
					int		currentYOffset = sourceYOffset;
					double	xJump =  (clip.LlCorner.X - ulCornerX) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Angle * Math.PI / 180));
					int		yTmp;
				
					byte*	pOrigCurrent;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						currentXOffset = (int) (y * xJump);
						pOrigCurrent = pSource + (y + ulCornerY) * sStride + (ulCornerX + currentXOffset);
						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							*(pCopyCurrent++) = *(pOrigCurrent++);

							yTmp = y + ulCornerY + ((int) (yJump * x));
							pOrigCurrent = pSource + yTmp * sStride + (ulCornerX + currentXOffset + x);
						}

					}
				}
			}
			catch(Exception ex)
			{
				ex = ex;
				return false;
			}

			return true;
		}*/
		#endregion

		#region GetClipCheckBorders24bpp()
		private static Bitmap GetClipCheckBorders24bpp(BitmapData sourceData, Rect clip)
		{
			int			x, y;
			int			width = clip.Width;
			int			height = clip.Height;
			Bitmap		result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);

			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride; 

			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			int			sourceWidth = sourceData.Width;
			int			sourceHeight = sourceData.Height;

			try
			{
				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					int		sourceXOffset = ulCornerX;
					int		sourceYOffset = ulCornerY;
					int		currentXOffset = sourceXOffset;
					int		currentYOffset = sourceYOffset;
					double	xJump =  (clip.LlCorner.X - clip.UlCorner.X) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Angle * Math.PI / 180));
					int		yTmp, xTmp;
					bool	canReadFromSource;
				
					byte*	pOrigCurrent = pSource;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						currentXOffset = (int) (y * xJump);
						
						yTmp = y + ulCornerY;
						xTmp = ulCornerX + currentXOffset;
						
						if((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight)) 
						{
							pOrigCurrent = pSource + yTmp * sStride + xTmp * 3;
							canReadFromSource = true;
						}
						else
							canReadFromSource = false;

						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							if(canReadFromSource)
							{
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
							}
							else
								pCopyCurrent += 3;

							yTmp = y + ulCornerY + ((int) (yJump * x));
							xTmp = ulCornerX + currentXOffset + x;
							//pOrigCurrent = pSource + yTmp * sStride + (clip.UlCorner.X + currentXOffset + x) * 3;

							if((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight))
							{
								pOrigCurrent = pSource + yTmp * sStride + xTmp * 3;
								canReadFromSource = true;
							}
							else
							{
								canReadFromSource = false;
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				ex = ex;
			}

			result.UnlockBits(resultData);

			return result;
		}
		#endregion

		#region GetClipCheckBorders24bppQuality()
		private static Bitmap GetClipCheckBorders24bppQuality(BitmapData sourceData, Rect clip)
		{
			int			x, y;
			int			width = clip.Width;
			int			height = clip.Height;
			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			Bitmap		result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride; 

			try
			{
				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					double	sourceX;
					double	sourceY;
					double	xJump =  (clip.LlCorner.X - ulCornerX) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Angle * Math.PI / 180));
					double	xRest, yRest;
				
					byte*	pOrigCurrent;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						sourceX = ulCornerX + y * xJump;
						sourceY = ulCornerY + y;

						xRest = sourceX - (int) sourceX;
						if(xRest < 0)
							xRest += 1;

						if(xRest < 0.000001)
							xRest = 0;
						if(xRest > .999999)
						{
							sourceX = (int) sourceX + 1;
							xRest = 0;
						}

						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							if(sourceX >= 0 && sourceX < sourceData.Width && sourceY >= 0 && sourceY < sourceData.Height)
							{
								yRest = sourceY - (int) sourceY;
								if(yRest < 0)
									yRest += 1;

								if(yRest < 0.000001)
									yRest = 0;
								if(yRest > .999999)
								{
									sourceY = (int) sourceX + 1;
									yRest = 0;
								}
							
								pOrigCurrent = pSource + (int) sourceY * sStride + ((int) sourceX) * 3;
							
								if(xRest == 0)
								{
									if(yRest == 0)
									{
										*(pCopyCurrent++) = *(pOrigCurrent++);
										*(pCopyCurrent++) = *(pOrigCurrent++);
										*(pCopyCurrent++) = *(pOrigCurrent++);
									}
									else
									{
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
										pOrigCurrent++;
									}
								}
								else
								{
									if(yRest == 0)
									{
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-xRest) + pOrigCurrent[3] * xRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-xRest) + pOrigCurrent[3] * xRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-xRest) + pOrigCurrent[3] * xRest);
										pOrigCurrent++;
									}
									else
									{
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-yRest) * (1-xRest) + 
											pOrigCurrent[3] * xRest * (1-yRest) +
											pOrigCurrent[sStride] * (1-xRest) * yRest + 
											pOrigCurrent[sStride+3] * xRest * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-yRest) * (1-xRest) + 
											pOrigCurrent[3] * xRest * (1-yRest) +
											pOrigCurrent[sStride] * (1-xRest) * yRest + 
											pOrigCurrent[sStride+3] * xRest * yRest);
										pOrigCurrent++;
										*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-yRest) * (1-xRest) + 
											pOrigCurrent[3] * xRest * (1-yRest) +
											pOrigCurrent[sStride] * (1-xRest) * yRest + 
											pOrigCurrent[sStride+3] * xRest * yRest);
										pOrigCurrent++;
									}
								}
							}
							else
							{
								pCopyCurrent += 3;
							}
							
							sourceX += 1;
							sourceY += yJump;
						}

					}
				}
			}
			catch(Exception ex)
			{
				ex = ex;
			}

			result.UnlockBits(resultData);

			return result;
		}
		#endregion

		#region GetClipCheckBorders24bppMem()
		/*private static bool GetClipCheckBorders24bppMem(BitmapData sourceData, Rect clip, ref int width, 
			ref int height, ref int rStride, ref IntPtr scan0)
		{
			try
			{
				int			x, y;
				int			sStride = sourceData.Stride; 

				width = clip.Width;
				height = clip.Height;
				rStride = GetStride(width, sourceData.PixelFormat);

				int			ulCornerX = clip.UlCorner.X;
				int			ulCornerY = clip.UlCorner.Y;

				int			sourceWidth = sourceData.Width;
				int			sourceHeight = sourceData.Height;

				unsafe
				{
					scan0 = new IntPtr(AllocHeapMemory(rStride * height));
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) scan0.ToPointer();

					int		sourceXOffset = ulCornerX;
					int		sourceYOffset = ulCornerY;
					int		currentXOffset = sourceXOffset;
					int		currentYOffset = sourceYOffset;
					double	xJump =  (clip.LlCorner.X - clip.UlCorner.X) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Angle * Math.PI / 180));
					int		yTmp, xTmp;
					bool	canReadFromSource;
				
					byte*	pOrigCurrent = pSource;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						currentXOffset = (int) (y * xJump);
						
						yTmp = y + ulCornerY;
						xTmp = ulCornerX + currentXOffset;
						
						if((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight)) 
						{
							pOrigCurrent = pSource + yTmp * sStride + xTmp * 3;
							canReadFromSource = true;
						}
						else
							canReadFromSource = false;

						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							if(canReadFromSource)
							{
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
							}
							else
								pCopyCurrent += 3;

							yTmp = y + ulCornerY + ((int) (yJump * x));
							xTmp = ulCornerX + currentXOffset + x;
							//pOrigCurrent = pSource + yTmp * sStride + (clip.UlCorner.X + currentXOffset + x) * 3;

							if((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight))
							{
								pOrigCurrent = pSource + yTmp * sStride + xTmp * 3;
								canReadFromSource = true;
							}
							else
							{
								canReadFromSource = false;
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				ex = ex;
				return false;
			}

			return true;
		}*/
		#endregion

		#region GetClipCheckBorders8bpp()
		private static Bitmap GetClipCheckBorders8bpp(BitmapData sourceData, Rect clip)
		{
			int			x, y;
			int			width = clip.Width;
			int			height = clip.Height;
			Bitmap		result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);

			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride; 

			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			int			sourceWidth = sourceData.Width;
			int			sourceHeight = sourceData.Height;

			try
			{
				unsafe
				{
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) resultData.Scan0.ToPointer();

					int		sourceXOffset = clip.UlCorner.X;
					int		sourceYOffset = clip.UlCorner.Y;
					int		currentXOffset = sourceXOffset;
					int		currentYOffset = sourceYOffset;
					double	xJump =  (clip.LlCorner.X - clip.UlCorner.X) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Angle * Math.PI / 180));
					int		yTmp, xTmp;
					bool	canReadFromSource;
				
					byte*	pOrigCurrent = pSource;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						currentXOffset = (int) (y * xJump);
						
						yTmp = y + ulCornerY;
						xTmp = ulCornerX + currentXOffset;
						
						if((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight)) 
						{
							pOrigCurrent = pSource + yTmp * sStride + xTmp;
							canReadFromSource = true;
						}
						else
							canReadFromSource = false;

						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							if(canReadFromSource)
								*(pCopyCurrent++) = *(pOrigCurrent++);
							else
								pCopyCurrent++;

							yTmp = y + ulCornerY + ((int) (yJump * x));
							xTmp = ulCornerX + currentXOffset + x;
							//pOrigCurrent = pSource + yTmp * sStride + (clip.UlCorner.X + currentXOffset + x) * 3;

							if((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight))
							{
								pOrigCurrent = pSource + yTmp * sStride + xTmp;
								canReadFromSource = true;
							}
							else
							{
								canReadFromSource = false;
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				ex = ex;
			}

			result.UnlockBits(resultData);

			return result;
		}
		#endregion

		#region GetClipCheckBorders8bppQuality()
		private static Bitmap GetClipCheckBorders8bppQuality(BitmapData sourceData, Rect clip)
		{
			int			x, y;
			int			width = clip.Width;
			int			height = clip.Height;
			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			Bitmap		result = new Bitmap(clip.Width, clip.Height, sourceData.PixelFormat);
			BitmapData	resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

			int			sStride = sourceData.Stride; 
			int			rStride = resultData.Stride; 

			unsafe
			{
				byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
				byte*	pResult = (byte*) resultData.Scan0.ToPointer();

				double	sourceX;
				double	sourceY;
				double	xJump =  (clip.LlCorner.X - ulCornerX) / (double) clip.Height;
				double	yJump =  (Math.Tan(clip.Angle * Math.PI / 180));
				double	xRest, yRest;
				
				byte*	pOrigCurrent;
				byte*	pCopyCurrent;

				for(y = 0; y < height; y++)
				{
					sourceX = ulCornerX + y * xJump;
					sourceY = ulCornerY + y;

					xRest = sourceX - (int) sourceX;
					if(xRest < 0)
						xRest += 1;

					if(xRest < 0.000001)
						xRest = 0;
					if(xRest > .999999)
					{
						sourceX = (int) sourceX + 1;
						xRest = 0;
					}

					pCopyCurrent = pResult + y * rStride;

					for(x = 0; x < width; x++)
					{
						if(sourceX >= 0 && sourceX < sourceData.Width && sourceY >= 0 && sourceY < sourceData.Height)
						{
							yRest = sourceY - (int) sourceY;
							if(yRest < 0)
								yRest += 1;

							if(yRest < 0.000001)
								yRest = 0;
							if(yRest > .999999)
							{
								sourceY = (int) sourceX + 1;
								yRest = 0;
							}
							
							pOrigCurrent = pSource + (int) sourceY * sStride + (int) sourceX;
							
							if(xRest == 0)
							{
								if(yRest == 0)
								{
									*(pCopyCurrent++) = *pOrigCurrent;
								}
								else
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1 - yRest) + pOrigCurrent[sStride] * yRest);
								}
							}
							else
							{
								if(yRest == 0)
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-xRest) + pOrigCurrent[1] * xRest);
								}
								else
								{
									*(pCopyCurrent++) = Convert.ToByte(*pOrigCurrent * (1-yRest) * (1-xRest) + 
										pOrigCurrent[1] * xRest * (1-yRest) +
										pOrigCurrent[sStride] * (1-xRest) * yRest + 
										pOrigCurrent[sStride+1] * xRest * yRest);
								}
							}
						}
						else
						{
							pCopyCurrent++;
						}
							

						sourceX += 1;
						sourceY += yJump;
					}

				}
			}


			result.UnlockBits(resultData);

			return result;
		}
		#endregion

		#region GetClipCheckBorders8bppMem()
		/*private static bool GetClipCheckBorders8bppMem(BitmapData sourceData, Rect clip, ref int width, 
			ref int height, ref int rStride, ref IntPtr scan0)
		{
			int			x, y;
			int			sStride = sourceData.Stride; 
			
			width = clip.Width;
			height = clip.Height;

			rStride = GetStride(width, sourceData.PixelFormat);

			int			ulCornerX = clip.UlCorner.X;
			int			ulCornerY = clip.UlCorner.Y;

			int			sourceWidth = sourceData.Width;
			int			sourceHeight = sourceData.Height;

			try
			{
				unsafe
				{
					scan0 = new IntPtr(AllocHeapMemory(rStride * height));
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult = (byte*) scan0.ToPointer();

					int		sourceXOffset = clip.UlCorner.X;
					int		sourceYOffset = clip.UlCorner.Y;
					int		currentXOffset = sourceXOffset;
					int		currentYOffset = sourceYOffset;
					double	xJump =  (clip.LlCorner.X - clip.UlCorner.X) / (double) clip.Height;
					double	yJump =  (Math.Tan(clip.Angle * Math.PI / 180));
					int		yTmp, xTmp;
					bool	canReadFromSource;
				
					byte*	pOrigCurrent = pSource;
					byte*	pCopyCurrent;

					for(y = 0; y < height; y++)
					{
						currentXOffset = (int) (y * xJump);
						
						yTmp = y + ulCornerY;
						xTmp = ulCornerX + currentXOffset;
						
						if((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight)) 
						{
							pOrigCurrent = pSource + yTmp * sStride + xTmp;
							canReadFromSource = true;
						}
						else
							canReadFromSource = false;

						pCopyCurrent = pResult + y * rStride;

						for(x = 0; x < width; x++)
						{
							if(canReadFromSource)
								*(pCopyCurrent++) = *(pOrigCurrent++);
							else
								pCopyCurrent++;

							yTmp = y + ulCornerY + ((int) (yJump * x));
							xTmp = ulCornerX + currentXOffset + x;
							//pOrigCurrent = pSource + yTmp * sStride + (clip.UlCorner.X + currentXOffset + x) * 3;

							if((xTmp >= 0) && (xTmp < sourceWidth) && (yTmp >= 0) && (yTmp < sourceHeight))
							{
								pOrigCurrent = pSource + yTmp * sStride + xTmp;
								canReadFromSource = true;
							}
							else
							{
								canReadFromSource = false;
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				ex = ex;
				return false;
			}

			return true;
		}*/
		#endregion

		#region IsEntireClipInsideSource()
		private static bool IsEntireClipInsideSource(Size size, Rect clip)
		{
			if(clip.UlCorner.X < 0 || clip.UlCorner.Y < 0)
				return false;
			
			if(clip.LlCorner.X < 0 || clip.LlCorner.Y < 0 || clip.LlCorner.X >= size.Width || clip.LlCorner.Y >= size.Height)
				return false;

			Point	urCorner = new Point(clip.UlCorner.X + clip.Width, Convert.ToInt32(clip.UlCorner.Y + Math.Tan(clip.Angle * Math.PI/180) * clip.Height));

			if(urCorner.X < 0 || urCorner.X >= size.Width || urCorner.Y < 0 || urCorner.Y >= size.Height)
				return false;

			Point	lrCorner = new Point(urCorner.X + clip.LlCorner.X - clip.UlCorner.X, urCorner.Y + clip.LlCorner.Y - clip.UlCorner.Y);

			if(lrCorner.X < 0 || lrCorner.X >= size.Width || lrCorner.Y < 0 || lrCorner.Y >= size.Height)
				return false;

			return true;
		}
		#endregion

		#region AllocHeapMemory()
		private unsafe static void* AllocHeapMemory(int size) 
		{
			void*	result = HeapAlloc(ph, HEAP_ZERO_MEMORY, size);
			
			if (result == null) 
				throw new OutOfMemoryException();

			return result;
		}
		#endregion

		#region CopyData()
		private static IntPtr CopyData(BitmapData sourceData, Rectangle clip)
		{
			int			width = clip.Width;
			int			height = clip.Height;
			int			stride = sourceData.Stride; 
			int			x, y;
			int			clipX = clip.X;
			int			clipY = clip.Y;
			int			sStride = sourceData.Stride; 
			int			rStride = GetStride(width, sourceData.PixelFormat);
			IntPtr		scan0 = IntPtr.Zero;
			
			try
			{
				unsafe
				{
					scan0 = new IntPtr(AllocHeapMemory(rStride * height));
					
					byte*	pSource = (byte*) sourceData.Scan0.ToPointer();
					byte*	pResult =  (byte*) scan0.ToPointer();
				
					byte*	pOrigCurrent;
					byte*	pCopyCurrent;

					if(sourceData.PixelFormat == PixelFormat.Format24bppRgb)
					{
						for(y = 0; y < height; y++)
						{
							pOrigCurrent = pSource + (clipY + y) * sStride + clipX * 3;
							pCopyCurrent = pResult + y * rStride;

							for(x = 0; x < width; x++)
							{
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
								*(pCopyCurrent++) = *(pOrigCurrent++);
							}
						}
					}
					else if(sourceData.PixelFormat == PixelFormat.Format8bppIndexed)
					{
						for(y = 0; y < height; y++)
						{
							pOrigCurrent = pSource + (clipY + y) * sStride + clipX;
							pCopyCurrent = pResult + y * rStride;

							for(x = 0; x < width; x++)
							{
								*(pCopyCurrent++) = *(pOrigCurrent++);
							}
						}
					}
					else
					{
						return IntPtr.Zero;
					}
				}
			}
			catch(Exception ex)
			{
				ex = ex;
				return IntPtr.Zero;
			}
		
			return scan0;
		}
		#endregion

		#region GetStride()
		private unsafe static int GetStride(int width, PixelFormat pixelFormat) 
		{
			Bitmap		bitmap = new Bitmap(width, 1, pixelFormat);
			BitmapData	bmpData = bitmap.LockBits(Rectangle.FromLTRB(0, 0, width, 1), ImageLockMode.WriteOnly, pixelFormat); 			
			int			stride = bmpData.Stride;

			bitmap.UnlockBits(bmpData);
			bitmap.Dispose();
			
			return stride;
		}
		#endregion

		#region RemoveGhostLines
		private static unsafe void RemoveGhostLines(BitmapData bitmapData, int[] ghostLines)
		{
			byte*	pSource = (byte*) bitmapData.Scan0.ToPointer();
			byte*	pCurrent;
			byte	tmp;

			foreach(int ghostLine in ghostLines)
			{
				pCurrent = pSource + ghostLine / 8;

				for(int y = 0; y < bitmapData.Height; y++)
				{
					if(*pCurrent > 0)
					{
						tmp = (byte) (0x80 >> (ghostLine & 0x07));
						*pCurrent = (byte) (*pCurrent & (~ tmp ));
					}

					pCurrent += bitmapData.Stride;
				}
			}
		}
		#endregion

		#region FindClipTop()
		private static Point FindClipTop(BitmapData bmpData, double angle)
		{
			int			width = bmpData.Width;
			int			height = bmpData.Height;			
			int			stride = bmpData.Stride; 
			int			x, y;
			double		currentY;
			
			//double	xJump = 1 / Math.Tan(angle * Math.PI / 180);
			double	yJump = Math.Abs(Math.Tan(angle * Math.PI / 180));

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte*	pCurrent ;
				int		whiteCount;

				//top
				if(angle < 0)
				{
					for(y = 0; y < height; y++) 
					{ 
						currentY = y;
					
						for(x = 0; x < width && currentY >= 0; x = x + 8)
						{
							pCurrent = pSource + ((int)currentY * stride) + (x / 8);
							
							if(*pCurrent > 0)
							{
								whiteCount = 0;

								for(int i = 0; i < 8; i++)
									whiteCount += ((*pCurrent >> i) & 1);

								if(whiteCount > 3)
								{
									for(int i = 0; i < 8; i++)
										if(((*pCurrent >> i) & 1) == 1)
											return new Point(x + i, (int)currentY);
								}
							}
							
							currentY -= 8 * yJump;	
						}

						if(currentY >= 0)
						{
							x = (width / 8) * 8;
							pCurrent = pSource + ((int)currentY * stride) + (x / 8);

							if(*pCurrent > 0)
							{
								int		xMax = width - x;

								for(int i = 0; i < xMax; i++)
									if(((*pCurrent >> i) & 1) == 1)
										return new Point(x + i, (int)currentY);
							}
						}
					}
				}
				else
				{
					for(y = 0; y < height; y++) 
					{ 
						currentY = y;
					
						x = (width / 8) * 8;
						pCurrent = pSource + ((int)currentY * stride) + (x / 8);

						int		xMax = width - x;

						if(*pCurrent > 0)
						{
							for(int i = 0; i < xMax; i++)
								if(((*pCurrent >> i) & 1) == 1)
									return new Point(x + i, (int)currentY);
						}
						
						currentY -= xMax * yJump;

						for(x = (width / 8) * 8; x >= 0 && currentY >= 0; x = x - 8)
						{
							pCurrent = pSource + ((int)currentY * stride) + (x / 8);

							if(*pCurrent > 0)
							{
								whiteCount = 0;
								for(int i = 0; i < 8; i++)
									whiteCount += ((*pCurrent >> i) & 1);

								if(whiteCount > 3)
								{
									for(int i = 0; i < 8; i++)
										if(((*pCurrent >> i) & 1) == 1)
											return new Point(x + i, (int)currentY);
								}
							}

							currentY -= 8 * yJump;
						}
					}
				}
			}
			
			return new Point(bmpData.Width, 0);
		}
		#endregion

		#region FindClipBottom()
		private static Point FindClipBottom(BitmapData bmpData, double angle)
		{
			int			width = bmpData.Width;
			int			height = bmpData.Height;			
			int			stride = bmpData.Stride; 
			int			x, y;
			double		currentY;
			
			//double	xJump = 1 / Math.Tan(angle * Math.PI / 180);
			double		yJump = Math.Abs(Math.Tan(angle * Math.PI / 180));

			unsafe
			{
				byte*	pSource = (byte*)bmpData.Scan0.ToPointer(); 
				byte*	pCurrent ;
				int		whiteCount;

				//top
				if(angle > 0)
				{
					for(y = height - 1; y >= 0; y--) 
					{ 
						currentY = y;
					
						for(x = 0; x < width && currentY < height; x = x + 8)
						{
							pCurrent = pSource + ((int)currentY * stride) + (x / 8);
							
							if(*pCurrent > 0)
							{
								whiteCount = 0;

								for(int i = 0; i < 8; i++)
									whiteCount += ((*pCurrent >> i) & 1);

								if(whiteCount > 3)
								{
									for(int i = 0; i < 8; i++)
										if(((*pCurrent >> i) & 1) == 1)
											return new Point(x + i, (int)currentY);
								}
							}

							currentY += 8 * yJump;	
						}

						if(currentY >= 0)
						{
							x = (width / 8) * 8;
							pCurrent = pSource + ((int)currentY * stride) + (x / 8);

							if(*pCurrent > 0)
							{
								int		xMax = width - x;

								for(int i = 0; i < xMax; i++)
									if(((*pCurrent >> i) & 1) == 1)
										return new Point(x + i, (int)currentY);
							}
						}
					}
				}
				else
				{
					for(y = height - 1; y >= 0; y--) 
					{ 
						currentY = y;
					
						x = (width / 8) * 8;
						pCurrent = pSource + ((int)currentY * stride) + (x / 8);

							int		xMax = width - x;

						if(*pCurrent > 0)
						{
							for(int i = 0; i < xMax; i++)
								if(((*pCurrent >> i) & 1) == 1)
									return new Point(x + i, (int)currentY);
						}

						currentY += xMax * yJump;
						
						for(x = (width / 8) * 8; x >= 0 && currentY < height; x = x - 8)
						{
							pCurrent = pSource + ((int)currentY * stride) + (x / 8);

							if(*pCurrent > 0)
							{
								whiteCount = 0;
								for(int i = 0; i < 8; i++)
									whiteCount += ((*pCurrent >> i) & 1);

								if(whiteCount > 3)
								{
									for(int i = 0; i < 8; i++)
										if(((*pCurrent >> i) & 1) == 1)
											return new Point(x + i, (int)currentY);
								}
							}

							currentY += 8 * yJump;
						}
					}
				}
			}
			
			return new Point(bmpData.Width, 0);
		}
		#endregion
	
	}
}
