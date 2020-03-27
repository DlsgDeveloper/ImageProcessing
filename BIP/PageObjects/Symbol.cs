using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessing.PageObjects
{
	public class Symbol : IComparable, IPageObject
	{
		private int			x;
		public int			y;
		public int			right;
		public int			bottom;
		public int			Pixels;
		public int			APixelX;
		public int			APixelY;
		private Type		objectType;
		public Word			Word = null;
		public DelimiterZone zone = null;
		
		ObjectMap			objectMap = null;
		ObjectShape			objectShape = null;
		ConvexEnvelope		convexEnvelope = null;

		#region constructor
		public Symbol(int x, int y)
		{
			this.X = x;
			this.Y = y;
			this.Right = x+1;
			this.Bottom = y+1;
			this.Pixels = 1;
			this.APixelX = x;
			this.APixelY = y;
			this.ObjectType = Symbol.Type.Unknown;
		}

		public Symbol(ObjectMap objectMap)
		{
			this.X = objectMap.X;
			this.Y = objectMap.Y;
			this.Right = objectMap.Rectangle.Right;
			this.Bottom = objectMap.Rectangle.Bottom;
			this.Pixels = objectMap.ObjectPixels;
			this.ObjectType = Symbol.Type.Unknown;
			this.objectMap = objectMap;

			Point? p = objectMap.GetAnyObjectPixel();

			if (p != null)
			{
				this.APixelX = p.Value.X;
				this.APixelY = p.Value.Y;
			}
			else
			{
				this.APixelX = this.X;
				this.APixelY = this.Y;
			}
		}
		#endregion

		#region class HorizontalComparer
		public class HorizontalComparer : IComparer<Symbol>
		{
			public int Compare(Symbol symbol1, Symbol symbol2)
			{
				if (symbol1.X > symbol2.X)
					return 1;
				if (symbol1.X < symbol2.X)
					return -1;
				else
					return 0;
			}
		} 
		#endregion

		#region class VerticalComparer
		public class VerticalComparer : IComparer<Symbol>
		{
			public int Compare(Symbol symbol1, Symbol symbol2)
			{
				if (symbol1.Y > symbol2.Y)
					return 1;
				if (symbol1.Y < symbol2.Y)
					return -1;
				else
					return 0;
			}
		} 
		#endregion

		#region enum Type
		[Flags]
			public enum Type : ushort
		{
			Unknown = 0,
			Letter = 1,
			Line = 2,
			Picture = 4,
			Frame = 8,
			Dot = 16,
			Comma = 32,
			Colon = 64,
			Semicolon = 128,
			QuestionMark = 256,
			Exclamation = 512,
			Quote = 1024,
			DoubleQuote = 2048,
			Dash = 4096,
			AnyPunctuation = 8192,
			Punctuation = Dot + Comma + Colon + Semicolon + QuestionMark + Exclamation + Quote + 
				DoubleQuote + Dash + AnyPunctuation,
			NotSure = 32768
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties
		public int			X				{ get { return this.x; } set { this.x = value; } }
		public int			Y				{ get { return this.y; } set { this.y = value; } }
		public int			Right			{ get { return this.right; } set { this.right = value; } }
		public int			Bottom			{ get { return this.bottom; } set{this.bottom = value;}}
		public int			Width			{ get { return Right - X; } }
		public int			Height				{ get{return Bottom-Y;} }
		public double		RectanglePercBlack { get { return (Height * Width > 0) ? Pixels / (double)(Height * Width) : 0; } }
		public Rectangle	Rectangle		{ get{return Rectangle.FromLTRB(X, Y, Right, Bottom);} }
		public int			MiddleX			{ get{return X + Width / 2;} }
		public int			MiddleY			{ get{return Y + Height / 2;} }
		public bool			IsLetter		{ get{return (this.objectType == Symbol.Type.Letter);} }
		public bool			IsPunctuation	{ get{return ((this.objectType & Symbol.Type.Punctuation) > 0);} }
		public bool			IsPicture		{ get{return (this.objectType == Symbol.Type.Picture);} }
		public bool			IsLine			{ get{return (this.objectType == Symbol.Type.Line);} }
		public bool			IsFrame			{ get{return (this.objectType == Symbol.Type.Frame);} }
		public Symbol.Type	ObjectType		{ get { return this.objectType; } set { this.objectType = value; } }
		public ObjectMap	ObjectMap		{ get { return this.objectMap; } }
		public DelimiterZone Zone { get { return zone; } set { this.zone = value; } }

		public ObjectShape	ObjectShape		
		{ 
			get 
			{ 
				if(this.objectShape == null)
					this.objectShape = new ObjectShape(this.Rectangle.Location, this.objectMap);

				return this.objectShape;
			} 
		}

		public ConvexEnvelope ConvexEnvelope
		{
			get
			{
				if (this.convexEnvelope == null)
					this.convexEnvelope = new ConvexEnvelope(this.Rectangle.Location, this.objectMap);
				
				return this.convexEnvelope;
			}
		}
		#endregion


		//PUBLIC METHODS
		#region public methods
		
		#region Merge()
		public void Merge(Symbol symbol)
		{
			if (this.X >= symbol.X)
				this.X = symbol.X;
			if (this.Y > symbol.Y)
				this.Y = symbol.Y;
			if (this.Right < symbol.Right)
				this.Right = symbol.Right;
			if (this.Bottom < symbol.Bottom)
				this.Bottom = symbol.Bottom;

			this.Pixels += symbol.Pixels;

			if ((this.IsLetter || symbol.IsLetter) && (this.IsPunctuation || symbol.IsPunctuation))
				this.ObjectType = Symbol.Type.Letter;

			this.objectMap.Merge(symbol.objectMap);
		}
		#endregion

		#region GrowThru()
		/// <summary>
		/// Similar to Merge, but it doesn't merge ObjectMaps
		/// </summary>
		/// <param name="symbol"></param>
		public void GrowThru(Symbol symbol)
		{
			if (this.X >= symbol.X)
				this.X = symbol.X;
			if (this.Y > symbol.Y)
				this.Y = symbol.Y;
			if (this.Right < symbol.Right)
				this.Right = symbol.Right;
			if (this.Bottom < symbol.Bottom)
				this.Bottom = symbol.Bottom;

			this.Pixels += symbol.Pixels;

			/*if ((this.IsLetter || symbol.IsLetter) && (this.IsPunctuation || symbol.IsPunctuation))
				this.ObjectType = Symbol.Type.Letter;*/
		}
		#endregion

		#region Shift()
		public void Shift(int x, int y)
		{
			this.X += x;
			this.Y += y;
			this.Right += x;
			this.Bottom += y;
		}
		#endregion

		#region CompareTo()
		public int CompareTo(object obj)
		{
			if(this.X < ((Symbol) obj).X )
				return -1;
			else if(this.X == ((Symbol) obj).X )
				return 0;
			return 1;
		}
		#endregion

		#region ComputeObjectMap()
		public void ComputeObjectMap(BitmapData bmpData)
		{
			this.objectMap = ObjectMap.GetObjectMap(bmpData, this.Rectangle, new Point(this.APixelX, this.APixelY));
		}

		public void ComputeObjectMap(byte[,] bitArray, int width)
		{
			this.objectMap = ObjectMap.GetObjectMap(bitArray, width, this.Rectangle, new Point(this.APixelX, this.APixelY));
		}
		#endregion

		#region GetCrop()
		public static unsafe Crop GetCrop(Symbol symbol, int[,] array)
		{
			int width = symbol.Width;
			int height = symbol.Height;
			int x, y;
			Point pL = new Point(int.MaxValue, int.MaxValue);
			Point pT = new Point(int.MaxValue, int.MaxValue);
			Point pR = new Point(int.MinValue, int.MinValue);
			Point pB = new Point(int.MinValue, int.MinValue);

			//find left point
			x = 0;
			for (y = 0; y < height; y++)
				if (array[y, x] == -1)
				{
					pL = new Point(x, y);
					break;
				}

			//find top point
			y = 0;
			for (x = 0; x < width; x++)
				if (array[y, x] == -1)
				{
					pT = new Point(x, y);
					break;
				}

			//find right point
			x = width - 1;
			for (y = 0; y < height; y++)
				if (array[y, x] == -1)
				{
					pR = new Point(x, y);
					break;
				}

			//find bottom point
			y = height - 1;
			for (x = 0; x < width; x++)
				if (array[y, x] == -1)
				{
					pB = new Point(x, y);
					break;
				}

			pL.Offset(symbol.X, symbol.Y);
			pT.Offset(symbol.X, symbol.Y);
			pR.Offset(symbol.X, symbol.Y);
			pB.Offset(symbol.X, symbol.Y);
			return new Crop(pL, pT, pR, pB);
		}
		#endregion

		#region GetDistance()
		public int GetDistance(Symbol symbol)
		{
			return this.ObjectMap.GetDistance(symbol.ObjectMap);
		}
		#endregion

		#region DrawToImage()
		public void DrawToImage(Color color, BitmapData bmpData)
		{
			if (this.objectMap != null)
				this.objectMap.DrawToImage(color, bmpData);
			else
			{
				unsafe
				{
					byte* scan0 = (byte*)bmpData.Scan0.ToPointer();
					int stride = bmpData.Stride;

					for (int y = this.Y; y < this.Bottom; y++)
					{
						for (int x = this.X; x < this.Right; x++)
						{
							scan0[y * stride + x * 3] = color.B;
							scan0[y * stride + x * 3 + 1] = color.G;
							scan0[y * stride + x * 3 + 2] = color.R;
						}
					}
				}
			}
		}
		#endregion

		#region DrawToBitArray()
		public void DrawToBitArray(byte[,] array, int width)
		{
			if (this.objectMap != null)
				this.objectMap.DrawToBitArray(array, width);
			else
			{
				unsafe
				{
					for (int y = this.Y; y < this.Bottom; y++)
					{
						for (int x = this.X; x < this.Right; x++)
						{
							array[y, x / 8] |= (byte)(0x80 >> (x & 0x07));
						}
					}
				}
			}
		}
		#endregion

		#region ToString()
		public override string ToString()
		{
			return string.Format("[{0},{1}] W:{2}, H:{3}, R={4}, B={5}", x, y, right - x, bottom - y, right, bottom);
		}
		#endregion

		#endregion
	}
}
