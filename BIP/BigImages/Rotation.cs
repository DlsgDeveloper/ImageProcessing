using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;

using ImageProcessing.PageObjects;
using BIP.Geometry;


namespace ImageProcessing.BigImages
{
	public class Rotation
	{
		//events
		public ImageProcessing.ProgressHnd ProgressChanged;

		#region constructor
		public Rotation()
		{
		}
		#endregion


		//PUBLIC METHODS
		#region public methods

		#region GetAngleFromObjects()
		/// <summary>
		/// Returns angle and confidence. Angle is negative in the counter-clockvise direction.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="words"></param>
		/// <param name="pictures"></param>
		/// <param name="delimiters"></param>
		/// <param name="confidence"></param>
		/// <returns></returns>
		public static double GetAngleFromObjects(Size clipSize, Words words, Pictures pictures, Delimiters delimiters, out float confidence)
		{
			double a;
			double w;
			double angle = 0.0;
			double weightSum = 0.0;

			if (pictures.GetSkew(clipSize, out a, out w))
			{
				angle += a * w;
				weightSum += w;
			}

			if (words.GetSkew(out a, out w))
			{
				angle += a * w;
				weightSum += w;
			}

			if (delimiters.GetSkew(clipSize, out a, out w))
			{
				angle += a * w;
				weightSum += w;
			}

			if (weightSum != 0.0)
				angle /= weightSum;

			confidence = (float) Math.Max(1, weightSum);
			return angle;
		}
		#endregion

		#region RotateClip()
		/// <summary>
		/// Desides the angle from Crop And Deskew functionality and executes the result with "blind" corners filled by r,g,b color.
		/// </summary>
		/// <param name="clip">Clip of bitmap to rotate. Rectangle.Empty to rotate entire image.</param>
		/// <param name="r">Color image - red background component; Grayscale image - gray level. Bitonal image - 1 if r > 0.</param>
		/// <param name="g">Color image - green background component.</param>
		/// <param name="b">Color image - blue background component.</param>
		/// <returns></returns>
		public void RotateClip(ImageProcessing.BigImages.ItDecoder itDecoder, string destPath, ImageProcessing.FileFormat.IImageFormat imageFormat,
			ImageProcessing.IpSettings.Clip clip, byte r, byte g, byte b)
		{
			ImageProcessing.BigImages.CropAndDeskew cropAndDeskew = new CropAndDeskew();

			cropAndDeskew.ProgressChanged += delegate(float progress)
			{
				if (ProgressChanged != null)
					ProgressChanged(progress);
			};

			ImageProcessing.CropDeskew.CdObject cdObject = new ImageProcessing.CropDeskew.CdObject(clip.Skew, clip.PointUL, clip.PointUR, clip.PointLL, clip.PointLR, itDecoder.Width / (double)itDecoder.Height);

			cropAndDeskew.Execute(itDecoder, cdObject, destPath, imageFormat, true);
		}
		#endregion

		#region RotatePoint()
		/// <summary>
		/// Rotates point around center point, positive angle is in clockwise direction.
		/// </summary>
		/// <param name="p"></param>
		/// <param name="centerPoint"></param>
		/// <param name="angle"></param>
		/// <returns></returns>
		public static PointF RotatePoint(PointF p, PointF centerPoint, double angle)
		{
			double beta = Math.Atan2((double)(centerPoint.Y - p.Y), (double)(centerPoint.X - p.X));
			double m = Math.Sqrt((double)(((centerPoint.X - p.X) * (centerPoint.X - p.X)) + ((centerPoint.Y - p.Y) * (centerPoint.Y - p.Y))));
			double xShifted = centerPoint.X - (Math.Cos(beta + angle) * m);
			double yShifted = centerPoint.Y - (Math.Sin(beta + angle) * m);
			return new PointF((float)xShifted, (float)yShifted);
		}

		public static RatioPoint RotatePoint(RatioPoint ratioPoint, RatioPoint centroid, double angle, double widthHeightRatio)
		{
			RatioPoint p = new RatioPoint(ratioPoint.X * widthHeightRatio, ratioPoint.Y);
			RatioPoint centerPoint = new RatioPoint(centroid.X * widthHeightRatio, centroid.Y);
			
			double beta = Math.Atan2((double)(centerPoint.Y - p.Y), (double)(centerPoint.X - p.X));
			double m = Math.Sqrt((double)(((centerPoint.X - p.X) * (centerPoint.X - p.X)) + ((centerPoint.Y - p.Y) * (centerPoint.Y - p.Y))));
			double xShifted = centerPoint.X - (Math.Cos(beta + angle) * m);
			double yShifted = centerPoint.Y - (Math.Sin(beta + angle) * m);

			return new RatioPoint(xShifted / widthHeightRatio, yShifted);
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#endregion

	}

}
