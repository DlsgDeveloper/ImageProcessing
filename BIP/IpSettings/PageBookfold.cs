using System;
using System.Drawing;
using System.Collections;

using BIP.Geometry;


namespace ImageProcessing.IpSettings
{
	public class PageBookfold
	{
		Curve	curveB;
		Curve	curveT;
		ImageProcessing.IpSettings.ItPage	page;
		float	confidence;

		internal event ImageProcessing.IpSettings.ItImage.VoidHnd Changed;
		bool	raiseChangedEvent = true;
		bool	changed = false;


		#region constructor
		public PageBookfold(ImageProcessing.IpSettings.ItPage page, float confidence)
		{
			this.page = page;
			this.curveT = new Curve(page, true);
			this.curveB = new Curve(page, false);
			this.confidence = confidence;

			this.curveT.Changed += new ImageProcessing.IpSettings.ItImage.VoidHnd(Curve_Changed);
			this.curveB.Changed += new ImageProcessing.IpSettings.ItImage.VoidHnd(Curve_Changed);
		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties
		public bool			IsCurved { get { return ((this.curveT != null && this.curveT.IsCurved) || (this.curveB != null && this.curveB.IsCurved)); } }
		public Curve		TopCurve { get { return this.curveT; } }
		public Curve		BottomCurve { get { return this.curveB; } }

		#region Confidence
		public float Confidence 
		{ 
			get { return this.confidence; } 
			set 
			{ 
				if (this.confidence != value)
				{
					this.confidence = value;

					Curve_Changed();
				}
			} 
		}
		#endregion

		#region RaiseChangedEvent
		public bool RaiseChangedEvent
		{
			get { return this.raiseChangedEvent; }
			set
			{
				if (this.raiseChangedEvent != value)
				{
					this.raiseChangedEvent = value;

					if (this.raiseChangedEvent && changed)
					{
						changed = false;

						if (Changed != null)
							Changed();
					}
				}
			}
		}
		#endregion

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region ClipChanged()
		public void ClipChanged(ImageProcessing.IpSettings.Clip clip)
		{
			this.RaiseChangedEvent = false;

			this.curveT.ClipChanged();
			this.curveB.ClipChanged();

			this.RaiseChangedEvent = true;
		}
		#endregion

		#region Reset()
		public void Reset()
		{
			this.RaiseChangedEvent = false;

			this.curveT.Reset();
			this.curveB.Reset();
			this.Confidence = 1.0F;

			this.RaiseChangedEvent = true;
		}
		#endregion

		#region SetCurvePoint()
		public bool SetCurvePoint(Curve curve, int index, RatioPoint newPoint)
		{
			if (curve == this.curveT)
			{
				this.curveT.SetPoint(index, newPoint);
				return true;
			}
			else if (curve == this.curveB)
			{
				this.curveB.SetPoint(index, newPoint);
				return true;
			}

			return false;
		}
		#endregion

		#region SetCurves()
		public void SetCurves(Curve curveT, Curve curveB, float confidence)
		{
			SetCurves(curveT.Points, curveB.Points, confidence);
		}

		public void SetCurves(RatioPoint[] topPoints, RatioPoint[] bottomPoints, float confidence)
		{
			this.RaiseChangedEvent = false;

			this.curveT.SetPoints(topPoints);
			this.curveB.SetPoints(bottomPoints);
			this.Confidence = confidence;

			this.RaiseChangedEvent = true;
		}
		#endregion

		#region ImportSettings()
		public void ImportSettings(PageBookfold source)
		{
			SetCurves(source.TopCurve.Points, source.BottomCurve.Points, source.Confidence);
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region Curve_Changed()
		void Curve_Changed()
		{
			if (this.RaiseChangedEvent)
			{
				if (Changed != null)
					Changed();
			}
			else
			{
				this.changed = true;
			}
		}
		#endregion

		#endregion

	}
	
}
