using System;
using System.Drawing;
using System.Collections;

namespace ImageProcessing
{
	public class PageBookfolding
	{
		Curve	curveB;
		Curve	curveT;
		ItPage	page;
		float	confidence;

		public event ItImage.VoidHnd Changed;

		#region constructor
		public PageBookfolding(ItPage page, float confidence)
		{
			this.page = page;
			this.curveT = new Curve(page.Clip, true);
			this.curveB = new Curve(page.Clip, false);
			this.confidence = confidence;

			this.curveT.Changed += new ItImage.VoidHnd(Curve_Changed);
			this.curveB.Changed += new ItImage.VoidHnd(Curve_Changed);
		}
		#endregion

		//PUBLIC PROPERTIES
		#region public properties
		public bool IsCurved { get { return ((this.curveT != null && this.curveT.IsCurved) || (this.curveB != null && this.curveB.IsCurved)); } }
		public Curve TopCurve { get { return this.curveT; } }
		public Curve BottomCurve { get { return this.curveB; } }
		public float Confidence { get { return this.confidence; } set { this.confidence = value; } }
		#endregion

		//PUBLIC METHODS
		#region public methods

		#region ClipChanged()
		public void ClipChanged(Clip clip)
		{
			this.curveT.ClipChanged();
			this.curveB.ClipChanged();
		}
		#endregion

		#region Clone()
		/*public PageBookfolding Clone(ItPage page)
		{
			Curve curveT = this.curveT.Clone(page.Clip);
			Curve curveB = this.curveB.Clone(page.Clip);

			return new PageBookfolding(page, curveT, curveB, confidence);
		}*/
		#endregion

		#region Reset()
		public void Reset()
		{
			this.curveT.Reset();
			this.curveB.Reset();
			this.confidence = 1.0F;
		}
		#endregion

		#region SetCurvePoint()
		public bool SetCurvePoint(Curve curve, int index, Point newPoint)
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
			/*this.curveT = curveT;
			this.curveB = curveB;
			this.confidence = confidence;*/

			SetCurves(curveT.Points, curveB.Points, confidence);
		}
		#endregion

		#region SetCurves()
		public void SetCurves(Point[] topPoints, Point[] bottomPoints, float confidence)
		{
			this.curveT.SetPoints(topPoints);
			this.curveB.SetPoints(bottomPoints);
			this.confidence = confidence;
		}
		#endregion

		#region ImportSettings()
		public void ImportSettings(PageBookfolding source)
		{
			SetCurves(source.TopCurve.Points, source.BottomCurve.Points, source.Confidence);
		}
		#endregion

		#region ShiftCurve()
		public bool ShiftCurve(Curve curve, int dy)
		{
			if (curve == this.curveT)
			{
				this.curveT.Shift(dy);
				return true;
			}
			else if (curve == this.curveB)
			{
				this.curveB.Shift(dy);
				return true;
			}

			return false;
		}
		#endregion

		#region ResizeSettings()
		public void ResizeSettings(double zoom)
		{
			curveT.ResizeSettings(zoom);
			curveB.ResizeSettings(zoom);
		}
		#endregion

		#endregion

		//PRIVATE METHODS
		#region private methods

		#region Curve_Changed()
		void Curve_Changed()
		{
			if (Changed != null)
				Changed();
		}
		#endregion

		#endregion

	}
	
}
