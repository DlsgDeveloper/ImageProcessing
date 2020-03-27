using System;
using System.Collections.Generic;
using System.Text;

namespace ImageProcessing
{
	public class PostProcessing
	{
		Skew skew;
		Despeckle despeckle;
		Rotation rotation;
		BlackBorderRemoval blackBorderRemoval;
		BackgroundRemoval backgroundRemoval;
		Invertion invertion;



		#region constructor
		public PostProcessing()
		{
			this.skew = new Skew();
			this.despeckle = new Despeckle();
			this.rotation = new Rotation();
			this.blackBorderRemoval = new BlackBorderRemoval();
			this.backgroundRemoval = new BackgroundRemoval();
			this.invertion = new Invertion();
		}
		#endregion


		#region class Skew
		public class Skew
		{
			bool isEnabled = false;
			double angle = 0;

			#region constructor
			internal Skew()
			{
			}
			#endregion

			//PUBLIC PROPERTIES
			#region public properties

			public bool IsEnabled { get { return isEnabled; } set { isEnabled = value; } }
			public double Angle { get { return angle; } set { angle = value; } }

			#endregion

			//PUBLIC METHODS
			#region public methods

			#region Reset()
			public void Reset()
			{
				this.IsEnabled = false;
				this.Angle = 0;
			}
			#endregion

			#endregion

		}
		#endregion

		#region class Despeckle
		public class Despeckle
		{
			bool isEnabled = false;
			ImageProcessing.NoiseReduction.DespeckleSize maskSize = NoiseReduction.DespeckleSize.Size2x2;
			ImageProcessing.NoiseReduction.DespeckleMode despeckleMode = ImageProcessing.NoiseReduction.DespeckleMode.WhiteSpecklesOnly;

			#region constructor
			internal Despeckle()
			{
			}
			#endregion

			//PUBLIC PROPERTIES
			#region public properties

			public bool IsEnabled { get { return isEnabled; } set { isEnabled = value; } }
			public ImageProcessing.NoiseReduction.DespeckleSize MaskSize { get { return maskSize; } set { maskSize = value; } }
			public ImageProcessing.NoiseReduction.DespeckleMode DespeckleMode { get { return despeckleMode; } set { despeckleMode = value; } }

			#endregion

			//PUBLIC METHODS
			#region public methods

			#region Reset()
			public void Reset()
			{
				this.IsEnabled = false;
				this.MaskSize = NoiseReduction.DespeckleSize.Size2x2;
				this.DespeckleMode = ImageProcessing.NoiseReduction.DespeckleMode.WhiteSpecklesOnly;
			}
			#endregion

			#endregion

		}
		#endregion

		#region class Rotation
		public class Rotation
		{
			RotationMode angle = 0;

			#region constructor
			internal Rotation()
			{
			}
			#endregion


			#region enum RotationMode
			public enum RotationMode
			{
				NoRotation = 00,
				Rotation90 = 90,
				Rotation180 = 180,
				Rotation270 = 270,
			}
			#endregion


			//PUBLIC PROPERTIES
			#region public properties

			public RotationMode Angle {
				get { return angle; } 
				set 
				{
					if ((int)value == 0x01)
						angle = RotationMode.NoRotation;
					else if ((int)value == 0x02)
						angle = RotationMode.Rotation90;
					else if ((int)value == 0x03)
						angle = RotationMode.Rotation180;
					else if ((int)value == 0x04)
						angle = RotationMode.Rotation270;
					else
						angle = value; 
				} 
			}

			#endregion

			//PUBLIC METHODS
			#region public methods

			#region Reset()
			public void Reset()
			{
				this.Angle = RotationMode.NoRotation;
			}
			#endregion

			#endregion

		}
		#endregion

		#region class BlackBorderRemoval
		public class BlackBorderRemoval
		{
			bool isEnabled = false;

			#region constructor
			internal BlackBorderRemoval()
			{
			}
			#endregion

			//PUBLIC PROPERTIES
			#region public properties

			public bool IsEnabled { get { return isEnabled; } set { isEnabled = value; } }

			#endregion

			//PUBLIC METHODS
			#region public methods

			#region Reset()
			public void Reset()
			{
				this.IsEnabled = false;
			}
			#endregion

			#endregion

		}
		#endregion

		#region class BackgroundRemoval
		public class BackgroundRemoval
		{
			bool isEnabled = false;

			#region constructor
			internal BackgroundRemoval()
			{
			}
			#endregion

			//PUBLIC PROPERTIES
			#region public properties

			public bool IsEnabled { get { return isEnabled; } set { isEnabled = value; } }

			#endregion

			//PUBLIC METHODS
			#region public methods

			#region Reset()
			public void Reset()
			{
				this.IsEnabled = false;
			}
			#endregion

			#endregion

		}
		#endregion

		#region class Invertion
		public class Invertion
		{
			bool isEnabled = false;

			#region constructor
			internal Invertion()
			{
			}
			#endregion

			//PUBLIC PROPERTIES
			#region public properties

			public bool IsEnabled { get { return isEnabled; } set { isEnabled = value; } }

			#endregion

			//PUBLIC METHODS
			#region public methods

			#region Reset()
			public void Reset()
			{
				this.IsEnabled = false;
			}
			#endregion

			#endregion

		}
		#endregion


		//PUBLIC PROPERTIES
		#region public properties

		public bool						IsAnyOptionEnabled { get { return this.skew.IsEnabled || this.despeckle.IsEnabled || this.rotation.Angle != Rotation.RotationMode.NoRotation || this.blackBorderRemoval.IsEnabled || this.backgroundRemoval.IsEnabled || this.invertion.IsEnabled; } }
		public Skew						ItSkew { get { return this.skew; } }
		public Despeckle				ItDespeckle { get { return this.despeckle; } }
		public Rotation					ItRotation { get { return this.rotation; } }
		public BlackBorderRemoval		ItBlackBorderRemoval { get { return this.blackBorderRemoval; } }
		public BackgroundRemoval		ItBackgroundRemoval { get { return this.backgroundRemoval; } }
		public Invertion				ItInvertion { get { return this.invertion; } }

		#endregion


		//PUBLIC METHODS
		#region public methods

		#region Reset()
		public void Reset()
		{
			this.skew.Reset();
			this.despeckle.Reset();
			this.rotation.Reset();
			this.blackBorderRemoval.Reset();
			this.backgroundRemoval.Reset();
			this.invertion.Reset();
		}
		#endregion

		#endregion
	}
}
