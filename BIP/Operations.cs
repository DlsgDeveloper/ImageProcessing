using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ImageProcessing
{
	public class Operations
	{
		ContentLocationParams contentLocation = new ContentLocationParams(false, 0.2F, 0.2F, false);
		SkewParams skew = new SkewParams(false);
		BookfoldParams bookfoldCorrection = new BookfoldParams(false);
		ArtifactsParams artifacts = new ArtifactsParams(false, 0.2F);
		CropAndDescewParams cropAndDescew = new CropAndDescewParams(false, 0, 0, Color.FromArgb(200, 200, 200), 0.1F, 0, 0, false, 10, 70, 20, 12);

		bool sameClipSize;


		public Operations(bool contentActive, float contentOffsetInInches, bool skewActive, bool curveCorrectionActive, bool artifactsActive)
		{
			this.contentLocation = new ContentLocationParams(contentActive, contentOffsetInInches, contentOffsetInInches, true);
			this.skew.Active = skewActive;
			this.bookfoldCorrection.Active = curveCorrectionActive;
			this.artifacts.Active = artifactsActive;
		}

		public Operations(ContentLocationParams contentLocationParams, bool skewActive, bool curveCorrectionActive, bool artifactsActive)
		{
			this.contentLocation = contentLocationParams;
			this.skew.Active = skewActive;
			this.bookfoldCorrection.Active = curveCorrectionActive;
			this.artifacts.Active = artifactsActive;
		}

		/// <summary>
		/// Activates Crop And Descew
		/// </summary>
		/// <param name="contentActive"></param>
		/// <param name="contentOffsetInInches"></param>
		/// <param name="skewActive"></param>
		/// <param name="curveCorrectionActive"></param>
		/// <param name="artifactsActive"></param>
		public Operations(bool active, float marginX, float marginY, Color thresholdColor, float minAngle, int offsetX, int offsetY,
				bool ghostLines, byte ghostLinesLowThreshold, byte ghostLinesHighThreshold, byte ghostLinesToCheck, byte glMaxDelta)
		{
			this.cropAndDescew.Active = active;
			this.cropAndDescew.MarginX = marginX;
			this.cropAndDescew.MarginY = marginY;
			this.cropAndDescew.ThresholdColor = thresholdColor;
			this.cropAndDescew.MinAngle = minAngle;
			this.cropAndDescew.OffsetX = offsetX;
			this.cropAndDescew.OffsetY = offsetY;
			this.cropAndDescew.GhostLines = ghostLines;
			this.cropAndDescew.GlLowThreshold = ghostLinesLowThreshold;
			this.cropAndDescew.GlHighThreshold = ghostLinesHighThreshold;
			this.cropAndDescew.GlToCheck = ghostLinesToCheck;
			this.cropAndDescew.GlMaxDelta = glMaxDelta;
		}

		public ContentLocationParams	ContentLocation { get { return contentLocation; } }
		public SkewParams				Skew { get { return skew; } }
		public BookfoldParams			BookfoldCorrection { get { return bookfoldCorrection; } }
		public ArtifactsParams			Artifacts { get { return artifacts; } }
		public CropAndDescewParams		CropAndDescew { get { return cropAndDescew; } }
		public bool						SameClipSize { get { return sameClipSize; } set { this.sameClipSize = value; } }

		public class ContentLocationParams
		{
			public readonly bool	Active;
			public readonly float	OffsetX;
			public readonly float	OffsetY;
			public readonly bool	Seek2Pages;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="active"></param>
			/// <param name="offsetX">distance in inches</param>
			/// <param name="offsetY"> distance in inches</param>
			/// <param name="seek2Pages">set false for portrait images, true for landscape books</param>
			public ContentLocationParams(bool active, float offsetX, float offsetY, bool seek2Pages)
			{
				this.Active = active;
				this.OffsetX = offsetX;
				this.OffsetY = offsetY;
				this.Seek2Pages = seek2Pages;
			}
		}

		public class SkewParams
		{
			public bool Active;

			public SkewParams(bool active)
			{
				this.Active = active;
			}
		}

		public class BookfoldParams
		{
			public bool Active;

			public BookfoldParams(bool active)
			{
				this.Active = active;
			}
		}

		public class ArtifactsParams
		{
			public bool Active;
			public float PercentageWhite;

			public ArtifactsParams(bool active, float percentageWhite)
			{
				this.Active = active;
				this.PercentageWhite = percentageWhite;
			}
		}

		#region class CropAndDescewParams
		public class CropAndDescewParams
		{
			public bool Active = true;
			public float MarginX = 0.0F;
			public float MarginY = 0.0F;
			public Color ThresholdColor;
			public float MinAngle = 0.1F;
			public int OffsetX;
			public int OffsetY;
			public bool GhostLines;
			public byte GlLowThreshold;
			public byte GlHighThreshold;
			public byte GlToCheck;
			public byte GlMaxDelta;


			public CropAndDescewParams(bool active, float marginX, float marginY, Color thresholdColor, float minAngle, int offsetX, int offsetY,
				bool ghostLines, byte ghostLinesLowThreshold, byte ghostLinesHighThreshold, byte ghostLinesToCheck, byte glMaxDelta)
			{
				this.Active = active;
				this.MarginX = marginX;
				this.MarginY = marginY;
				this.ThresholdColor = thresholdColor;
				this.MinAngle = minAngle;
				this.OffsetX = offsetX;
				this.OffsetY = offsetY;
				this.GhostLines = ghostLines;
				this.GlLowThreshold = ghostLinesLowThreshold;
				this.GlHighThreshold = ghostLinesHighThreshold;
				this.GlToCheck = ghostLinesToCheck;
				this.GlMaxDelta = glMaxDelta;
			}
		}
		#endregion

	}
}
