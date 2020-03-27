using System;
using System.Drawing ;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Lines.
	/// </summary>
	public class Lines
	{
		Line[]		lines ;

		public Lines(Line[] linesArray) 
		{
			lines = linesArray ;
		}

		public Line this [int index]
		{
			get { return lines[index] ; }
			set { lines[index] = value ; }
		}

		public uint Size { get { return (uint) lines.Length ; } }

		#region ResizeLines()
		public static Lines ResizeLines(Lines sourceLines, Size sourceSize, Size destSize)
		{
			Line[]	destLines = new Line[sourceLines.Size] ;

			double	zoomX = (double) destSize.Width / sourceSize.Width ;
			double	zoomY = (double) destSize.Height / sourceSize.Height ;

			destLines[0] = new Line(0, 0, 0, destSize.Height, sourceLines[0].Scale) ;
			destLines[(int) sourceLines.Size - 1] = new Line(destSize.Width, 0, destSize.Width, 
				destSize.Height, sourceLines[(int) sourceLines.Size - 1].Scale) ;

			for(int i = 1; i < sourceLines.Size - 1; i++)
			{
				Line	line = sourceLines[i] ;
				int		c0 = Convert.ToInt32((double) line.Point0.X * zoomX) ;
				int		cN = Convert.ToInt32((double) line.PointN.X * zoomX) ;
				int		r0 = 0 ; //Convert.ToInt32((double) line.Point0.Y * zoomY) ;
				int		rN = destSize.Height ; // Convert.ToInt32((double) line.PointN.Y * zoomY) ;

				destLines[i] = new Line(c0, r0, cN, rN, line.Scale) ;
			}

			return new Lines(destLines) ;
		}
		#endregion

	}
}
