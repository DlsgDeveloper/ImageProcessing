using System;
using System.Collections;
using System.Drawing;
using System.Collections.Generic;

namespace ImageProcessing.IpSettings
{
	/// <summary>
	/// Summary description for Fingers.
	/// </summary>
	public class Fingers : List<ImageProcessing.IpSettings.Finger>
	{		
		public event ImageProcessing.IpSettings.Finger.FingerHnd	FingerAdding;
		public event ImageProcessing.IpSettings.Finger.FingerHnd	FingerAdded;
		public event ImageProcessing.IpSettings.Finger.FingerHnd	FingerRemoving;
		public event ImageProcessing.IpSettings.Finger.FingerHnd	FingerRemoved;
		public event ImageProcessing.IpSettings.Finger.VoidHnd		Clearing;
		public event ImageProcessing.IpSettings.Finger.VoidHnd		Cleared;

		public event ImageProcessing.IpSettings.ItImage.VoidHnd		Changed;

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

				foreach (ImageProcessing.IpSettings.Finger finger in this)
					if (confidence > finger.Confidence)
						confidence = finger.Confidence;

				return confidence;
			}
			set
			{
				foreach (ImageProcessing.IpSettings.Finger finger in this)
					finger.Confidence = value; ;
			}
		}
		#endregion

		#endregion

		//PUBLIC METHODS
		#region public methods

		#region Add()
		new public void Add(ImageProcessing.IpSettings.Finger finger)
		{
			if (FingerAdding != null)
				FingerAdding(finger);

			base.Add(finger);

			finger.Changed += new Finger.ChangedHnd(Finger_Changed);
			finger.RemoveRequest += new ImageProcessing.IpSettings.Finger.FingerHnd(Remove);

			if (FingerAdded != null)
				FingerAdded(finger);

			if (Changed != null)
				Changed();
		}
		#endregion

		#region AddRange()
		new public void AddRange(IEnumerable<ImageProcessing.IpSettings.Finger> fingers)
		{
			foreach (ImageProcessing.IpSettings.Finger finger in fingers)
				Add(finger);
		}
		#endregion

		#region Remove()
		new public void Remove(ImageProcessing.IpSettings.Finger finger)
		{
			if (FingerRemoving!= null)
				FingerRemoving(finger);

			finger.Changed -= new Finger.ChangedHnd(Finger_Changed);
			finger.RemoveRequest -= new ImageProcessing.IpSettings.Finger.FingerHnd(Remove);
			base.Remove(finger);

			if (FingerRemoved != null)
				FingerRemoved(finger);

			if (Changed != null)
				Changed();
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

			foreach (ImageProcessing.IpSettings.Finger finger in this)
			{
				finger.Changed -= new Finger.ChangedHnd(Finger_Changed);
				finger.RemoveRequest -= new ImageProcessing.IpSettings.Finger.FingerHnd(Remove);
			}

			base.Clear();

			if (Cleared != null)
				Cleared();

			if (Changed != null)
				Changed();
		}
		#endregion

		#region ClipChanged()
		public void ClipChanged(ImageProcessing.IpSettings.Clip clip)
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

		#endregion


		//PRIVATE METHODS
		#region private methods

		#region Finger_Changed()
		void Finger_Changed(Finger finger, Finger.ChangeType type)
		{
			if (Changed != null)
				Changed();
		}
		#endregion

		#endregion

	}
}
