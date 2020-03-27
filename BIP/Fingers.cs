using System;
using System.Collections;
using System.Drawing;
using System.Collections.Generic;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for Fingers.
	/// </summary>
	public class Fingers : List<Finger>
	{		
		public event Finger.FingerHnd	FingerAdding;
		public event Finger.FingerHnd	FingerAdded;
		public event Finger.FingerHnd	FingerRemoving;
		public event Finger.FingerHnd	FingerRemoved;
		public event Finger.VoidHnd		Clearing;
		public event Finger.VoidHnd		Cleared;

		public event ItImage.VoidHnd	Changed;

		Size minSize = new Size(50, 50);

		public Fingers()
			: base()
		{
		}

		//PUBLIC PROPERTIES
		#region public properties

		#region Confidence
		public float Confidence
		{
			get
			{
				float confidence = 1.0F;

				foreach (Finger finger in this)
					if (confidence > finger.Confidence)
						confidence = finger.Confidence;

				return confidence;
			}
			set
			{
				foreach (Finger finger in this)
					finger.Confidence = value; ;
			}
		}
		#endregion

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Add()
		new public void Add(Finger finger)
		{
			if (FingerAdding != null)
				FingerAdding(finger);

			base.Add(finger);

			finger.RemoveRequest += new Finger.FingerHnd(Remove);
			finger.Changed += new Finger.ChangedHnd(Finger_Changed);

			if (FingerAdded != null)
				FingerAdded(finger);
		}

		#endregion

		#region AddRange()
		new public void AddRange(IEnumerable<Finger> fingers)
		{
			foreach (Finger finger in fingers)
				Add(finger);
		}
		#endregion

		#region Remove()
		new public void Remove(Finger finger)
		{
			if (FingerRemoving!= null)
				FingerRemoving(finger);

			finger.RemoveRequest -= new Finger.FingerHnd(Remove);
			base.Remove(finger);

			if (FingerRemoved != null)
				FingerRemoved(finger);

			if (this.Changed != null)
				this.Changed();
		}
		#endregion

		#region RemoveAt()
		new public void RemoveAt(int index)
		{
			Remove(this[index]);
		}
		#endregion

		#region Clear()
		new public void Clear()
		{
			if (Clearing != null)
				Clearing();

			foreach (Finger finger in this)
				finger.RemoveRequest -= new Finger.FingerHnd(Remove);

			base.Clear();

			if (Cleared != null)
				Cleared();

			if (this.Changed != null)
				this.Changed();
		}
		#endregion

		#region ClipChanged()
		public void ClipChanged(Clip clip)
		{
			if (clip.RectangleNotSkewed.IsEmpty == false)
			{
				for (int i = this.Count - 1; i >= 0; i--)
				{
					this[i].ClipChanged();
				}
			}
			else
				this.Clear();
		}
		#endregion

		#region ResizeSettings()
		public void ResizeSettings(double zoom)
		{
			foreach (Finger finger in this)
				finger.ResizeSettings(zoom);
		}
		#endregion

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region Finger_Changed()
		void Finger_Changed(Finger finger, Finger.ChangeType type)
		{
			if (this.Changed != null)
				this.Changed();
		}
		#endregion

		#endregion

	}
}
