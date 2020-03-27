using System;
using System.Collections.Generic;
using System.Text;

namespace ImageProcessing
{
	class ItImageSettings
	{
		bool	isFixed = true;
		bool	isIndependent = true;
		int		opticsCenter;
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
		public int OpticsCenter
		{
			get { return this.opticsCenter; }
			set
			{			
				if (this.opticsCenter != value)
				{
					this.opticsCenter = value;

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


		//PUBLIC METHODS
		#region public methods

		#region ImportSettings()
		public void ImportSettings(ItImageSettings itImageSettings)
		{
			this.isFixed = itImageSettings.IsFixed;
			this.isIndependent = itImageSettings.IsIndependent;
			this.opticsCenter = itImageSettings.opticsCenter;
		}
		#endregion

		#region ResizeSettings()
		public void ResizeSettings(double zoom)
		{
			if (this.IsFixed == false)
			{
				this.opticsCenter = Convert.ToInt32(this.opticsCenter * zoom);
			}
		}
		#endregion

		#endregion

	}
}
