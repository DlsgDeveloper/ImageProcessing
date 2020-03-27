using System;
using System.Collections.Generic;
using System.Text;

namespace ImageProcessing.IpSettings
{
	class ItImageSettings
	{
		bool	isFixed = true;
		bool	isIndependent = true;
		double	opticsCenter = 0.5;
		object	tag = null;

		public delegate void VoidHnd();

		public event VoidHnd SettingsChanged;

		public ItImageSettings()
		{
		}

		// PUBLIC PROPERTIES
		#region public properties

		#region IsFixed
		public bool IsFixed 
		{ 
			get { return this.isFixed; } 
			set 
			{
				if (this.isFixed != value)
				{
					this.isFixed = value;

					if (SettingsChanged != null)
						SettingsChanged();
				}
			}
		}
		#endregion

		#region IsIndependent
		public bool IsIndependent 
		{
			get { return this.isIndependent; } 
			set 
			{
				if (this.isIndependent != value)
				{
					this.isIndependent = value;

					if (SettingsChanged != null)
						SettingsChanged();
				}
			} 
		}
		#endregion

		#region OpticsCenter
		public double OpticsCenter
		{
			get { return this.opticsCenter; }
			set
			{			
				if (this.opticsCenter != value)
				{
					this.opticsCenter = Math.Max(0, Math.Min(1, value));

					if (SettingsChanged != null)
						SettingsChanged();
				}
			}
		}
		#endregion

		#region Tag
		public object Tag
		{
			get { return this.tag; }
			set
			{
				if (this.tag != value)
				{
					this.tag = value;

					if (SettingsChanged != null)
						SettingsChanged();
				}
			}
		}
		#endregion

		#endregion

	}
}
