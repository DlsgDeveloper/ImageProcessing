using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;

namespace BIP.Metadata
{
	public class ExifMetadata
	{
		//tags relating to image data structure
		public readonly LongProperty		ImageWidth = new LongProperty(ExifTags.ImageWidth);
		public readonly LongProperty		ImageLength = new LongProperty(ExifTags.ImageLength);
		public readonly ShortArrayProperty	BitsPerSample = new ShortArrayProperty(ExifTags.BitsPerSample, 3);
		public readonly ShortProperty		Compression = new ShortProperty(ExifTags.Compression);
		public readonly ShortProperty		PhotometricInterpretation = new ShortProperty(ExifTags.PhotometricInterpretation);
		public readonly ShortProperty		Orientation = new ShortProperty(ExifTags.Orientation);
		public readonly ShortProperty		SamplesPerPixel = new ShortProperty(ExifTags.SamplesPerPixel);
		public readonly ShortProperty		PlanarConfiguration = new ShortProperty(ExifTags.PlanarConfiguration);
		public readonly ShortArrayProperty	YCbCrSubSampling = new ShortArrayProperty(ExifTags.YCbCrSubSampling, 2);
		public readonly ShortProperty		YCbCrPositioning = new ShortProperty(ExifTags.YCbCrPositioning);
		public readonly RationalProperty	XResolution = new RationalProperty(ExifTags.XResolution);
		public readonly RationalProperty	YResolution = new RationalProperty(ExifTags.YResolution);
		public readonly ShortProperty		ResolutionUnit = new ShortProperty(ExifTags.ResolutionUnit);
		
		//tags relating to recording offset
		public readonly LongArrayProperty	StripOffsets = new LongArrayProperty(ExifTags.StripOffsets);
		public readonly LongProperty		RowsPerStrip = new LongProperty(ExifTags.RowsPerStrip);
		public readonly LongArrayProperty	StripByteCounts = new LongArrayProperty(ExifTags.StripByteCounts);
		public readonly LongProperty		JPEGInterchangeFormat = new LongProperty(ExifTags.JPEGInterchangeFormat);
		public readonly LongProperty		JPEGInterchangeFormatLength = new LongProperty(ExifTags.JPEGInterchangeFormatLength);
		
		//Tags relating to image data characteristics
		public readonly ShortArrayProperty		TransferFunction = new ShortArrayProperty(ExifTags.TransferFunction, 3 * 256);
		public readonly RationalArrayProperty	WhitePoint = new RationalArrayProperty(ExifTags.WhitePoint, 2);
		public readonly RationalArrayProperty	PrimaryChromaticities = new RationalArrayProperty(ExifTags.PrimaryChromaticities, 6);
		public readonly RationalArrayProperty	YCbCrCoefficients = new RationalArrayProperty(ExifTags.YCbCrCoefficients, 3);
		public readonly RationalArrayProperty	ReferenceBlackWhite = new RationalArrayProperty(ExifTags.ReferenceBlackWhite, 6);
		
		//Other tags
		public readonly AsciiProperty DateTime = new AsciiProperty(ExifTags.DateTime, 20);
		public readonly AsciiProperty ImageDescription = new AsciiProperty(ExifTags.ImageDescription);
		public readonly AsciiProperty Make = new AsciiProperty(ExifTags.Make);
		public readonly AsciiProperty Model = new AsciiProperty(ExifTags.Model);
		public readonly AsciiProperty Software = new AsciiProperty(ExifTags.Software);
		public readonly AsciiProperty Artist = new AsciiProperty(ExifTags.Artist);
		public readonly AsciiProperty Copyright = new AsciiProperty(ExifTags.Copyright);

		//Tags Relating to Version
		public readonly UndefinedProperty ExifVersion = new UndefinedProperty(ExifTags.ExifVersion, 4);
		public readonly UndefinedProperty FlashpixVersion = new UndefinedProperty(ExifTags.FlashpixVersion, 4);

		//Tag Relating to Image Data Characteristics
		public readonly ShortProperty ColorSpace = new ShortProperty(ExifTags.ColorSpace);

		//Tags Relating to Image Configuration
		public readonly UndefinedProperty ComponentsConfiguration = new UndefinedProperty(ExifTags.ComponentsConfiguration, 4);
		public readonly RationalProperty CompressedBitsPerPixel = new RationalProperty(ExifTags.CompressedBitsPerPixel);
		public readonly LongProperty PixelXDimension = new LongProperty(ExifTags.PixelXDimension);
		public readonly LongProperty PixelYDimension = new LongProperty(ExifTags.PixelYDimension);

		//Tags Relating to User Information
		public readonly UndefinedProperty MakerNote = new UndefinedProperty(ExifTags.MakerNote);
		public readonly UndefinedProperty UserComment = new UndefinedProperty(ExifTags.UserComment);

		//Tag Relating to Related File Information
		public readonly AsciiProperty RelatedSoundFile = new AsciiProperty(ExifTags.RelatedSoundFile, 13);

		//Tags Relating to Date and Time
		public readonly AsciiProperty DateTimeOriginal = new AsciiProperty(ExifTags.DateTimeOriginal, 20);
		public readonly AsciiProperty DateTimeDigitized = new AsciiProperty(ExifTags.DateTimeDigitized, 20);
		public readonly AsciiProperty SubSecTime = new AsciiProperty(ExifTags.SubsecTime);
		public readonly AsciiProperty SubSecTimeOriginal = new AsciiProperty(ExifTags.SubsecTimeOriginal);
		public readonly AsciiProperty SubSecTimeDigitized = new AsciiProperty(ExifTags.SubsecTimeDigitized);

		//Tags Relating to Picture-Taking Conditions
		public readonly RationalProperty ExposureTime = new RationalProperty(ExifTags.ExposureTime);
		public readonly RationalProperty FNumber = new RationalProperty(ExifTags.FNumber);
		public readonly ShortProperty ExposureProgram = new ShortProperty(ExifTags.ExposureProgram);
		public readonly AsciiProperty SpectralSensitivity = new AsciiProperty(ExifTags.SpectralSensitivity);
		public readonly ShortArrayProperty ISOSpeedRatings = new ShortArrayProperty(ExifTags.ISOSpeedRatings);
		public readonly UndefinedProperty OECF = new UndefinedProperty(ExifTags.OECF);
		public readonly SRationalProperty ShutterSpeedValue = new SRationalProperty(ExifTags.ShutterSpeedValue);
		public readonly RationalProperty ApertureValue = new RationalProperty(ExifTags.ApertureValue);
		public readonly SRationalProperty BrightnessValue = new SRationalProperty(ExifTags.BrightnessValue);
		public readonly SRationalProperty ExposureBiasValue = new SRationalProperty(ExifTags.ExposureBiasValue);
		public readonly RationalProperty MaxApertureValue = new RationalProperty(ExifTags.MaxApertureValue);
		public readonly RationalProperty SubjectDistance = new RationalProperty(ExifTags.SubjectDistance);
		public readonly ShortProperty MeteringMode = new ShortProperty(ExifTags.MeteringMode);
		public readonly ShortProperty LightSource = new ShortProperty(ExifTags.LightSource);
		public readonly ShortProperty Flash = new ShortProperty(ExifTags.Flash);
		public readonly RationalProperty FocalLength = new RationalProperty(ExifTags.FocalLength);
		public readonly ShortArrayProperty SubjectArea = new ShortArrayProperty(ExifTags.SubjectArea);
		public readonly RationalProperty FlashEnergy = new RationalProperty(ExifTags.FlashEnergy);
		public readonly UndefinedProperty SpatialFrequencyResponse = new UndefinedProperty(ExifTags.SpatialFrequencyResponse);
		public readonly RationalProperty FocalPlaneXResolution = new RationalProperty(ExifTags.FocalPlaneXResolution);
		public readonly RationalProperty FocalPlaneYResolution = new RationalProperty(ExifTags.FocalPlaneYResolution);
		public readonly ShortProperty FocalPlaneResolutionUnit = new ShortProperty(ExifTags.FocalPlaneResolutionUnit);
		public readonly ShortArrayProperty SubjectLocation = new ShortArrayProperty(ExifTags.SubjectLocation, 2);
		public readonly RationalProperty ExposureIndex = new RationalProperty(ExifTags.ExposureIndex);
		public readonly ShortProperty SensingMethod = new ShortProperty(ExifTags.SensingMethod);
		public readonly UndefinedProperty FileSource = new UndefinedProperty(ExifTags.FileSource, 1);
		public readonly UndefinedProperty SceneType = new UndefinedProperty(ExifTags.SceneType, 1);
		public readonly UndefinedProperty CFAPattern = new UndefinedProperty(ExifTags.CFAPattern);
		public readonly ShortProperty CustomRendered = new ShortProperty(ExifTags.CustomRendered);
		public readonly ShortProperty ExposureMode = new ShortProperty(ExifTags.ExposureMode);
		public readonly ShortProperty WhiteBalance = new ShortProperty(ExifTags.WhiteBalance);
		public readonly RationalProperty DigitalZoomRatio = new RationalProperty(ExifTags.DigitalZoomRatio);
		public readonly ShortProperty FocalLengthIn35mmFilm = new ShortProperty(ExifTags.FocalLengthIn35mmFilm);
		public readonly ShortProperty SceneCaptureType = new ShortProperty(ExifTags.SceneCaptureType);
		public readonly RationalProperty GainControl = new RationalProperty(ExifTags.GainControl);
		public readonly ShortProperty Contrast = new ShortProperty(ExifTags.Contrast);
		public readonly ShortProperty Saturation = new ShortProperty(ExifTags.Saturation);
		public readonly ShortProperty Sharpness = new ShortProperty(ExifTags.Sharpness);
		public readonly UndefinedProperty DeviceSettingDescription = new UndefinedProperty(ExifTags.DeviceSettingDescription);
		public readonly ShortProperty SubjectDistanceRange = new ShortProperty(ExifTags.SubjectDistanceRange);

		//Other Tags
		public readonly AsciiProperty ImageUniqueID = new AsciiProperty(ExifTags.ImageUniqueID, 33);

		List<PropertyBase> properties;

		#region constructor
		public ExifMetadata()
		{
			this.properties = new List<PropertyBase>()
			{
				ImageWidth ,
				ImageLength ,
				BitsPerSample ,
				Compression ,
				PhotometricInterpretation ,
				Orientation ,
				SamplesPerPixel ,
				PlanarConfiguration ,
				YCbCrSubSampling ,
				YCbCrPositioning ,
				XResolution ,
				YResolution ,
				ResolutionUnit ,
		
				StripOffsets ,
				RowsPerStrip ,
				StripByteCounts ,
				JPEGInterchangeFormat,
				JPEGInterchangeFormatLength,
		
				TransferFunction ,
				WhitePoint ,
				PrimaryChromaticities ,
				YCbCrCoefficients ,
				ReferenceBlackWhite ,
		
				DateTime ,
				ImageDescription ,
				Make ,
				Model ,
				Software ,
				Artist ,
				Copyright, 

				ExifVersion,
				FlashpixVersion,

				ColorSpace,

				ComponentsConfiguration,
				CompressedBitsPerPixel,
				PixelXDimension,
				PixelYDimension,

				MakerNote,
				UserComment,

				RelatedSoundFile,

				DateTimeOriginal,
				DateTimeDigitized,
				SubSecTime,
				SubSecTimeOriginal,
				SubSecTimeDigitized,

				ExposureTime,
				FNumber,
				ExposureProgram,
				SpectralSensitivity,
				ISOSpeedRatings,
				OECF,
				ShutterSpeedValue ,
				ApertureValue ,
				BrightnessValue ,
				ExposureBiasValue ,
				MaxApertureValue ,
				SubjectDistance ,
				MeteringMode ,
				LightSource ,
				Flash ,
				FocalLength ,
				SubjectArea ,
				FlashEnergy ,
				SpatialFrequencyResponse ,
				FocalPlaneXResolution ,
				FocalPlaneYResolution ,
				FocalPlaneResolutionUnit ,
				SubjectLocation ,
				ExposureIndex ,
				SensingMethod ,
				FileSource ,
				SceneType ,
				CFAPattern ,
				CustomRendered ,
				ExposureMode ,
				WhiteBalance ,
				DigitalZoomRatio ,
				FocalLengthIn35mmFilm,
				SceneCaptureType ,
				GainControl ,
				Contrast ,
				Saturation ,
				Sharpness ,
				DeviceSettingDescription ,
				SubjectDistanceRange ,

				ImageUniqueID
			};
		}
		#endregion

		#region constructor
		public ExifMetadata(ImageComponent.Metadata.ExifMetadata exif)
			: this()
		{
			foreach (ImageComponent.Metadata.PropertyBase propertyBase in exif.Properties)
			{
				if (propertyBase.Defined)
				{
					PropertyBase prop = GetProperty((int) propertyBase.TagId);

					if (prop != null)
						prop.ImportFromImageComponentMetadata(propertyBase);
				}
			}
		}
		#endregion

		#region Properties()
		
		public List<PropertyBase> Properties {get{return properties;}}
		
		#endregion

		#region GetProperty()
		public PropertyBase GetProperty(int propertyId)
		{
			foreach(PropertyBase property in this.Properties)
				if ((int)property.TagId == propertyId)
					return property;

			return null;
		}
		#endregion

	}

	#region enum ExifTags
	/// <summary>
	/// All exif tags as per the Exif standard 2.2, JEITA CP-2451
	/// </summary>
	public enum ExifTags : ushort
	{
		// IFD0 items
		ImageWidth = 0x100,
		ImageLength = 0x101,
		BitsPerSample = 0x102,
		Compression = 0x103,
		PhotometricInterpretation = 0x106,
		ImageDescription = 0x10E,
		Make = 0x10F,
		Model = 0x110,
		StripOffsets = 0x111,
		Orientation = 0x112,
		SamplesPerPixel = 0x115,
		RowsPerStrip = 0x116,
		StripByteCounts = 0x117,
		XResolution = 0x11A,
		YResolution = 0x11B,
		PlanarConfiguration = 0x11C,
		ResolutionUnit = 0x128,
		TransferFunction = 0x12D,
		Software = 0x131,
		DateTime = 0x132,
		Artist = 0x13B,
		WhitePoint = 0x13E,
		PrimaryChromaticities = 0x13F,
		JPEGInterchangeFormat = 0x201,
		JPEGInterchangeFormatLength = 0x202,
		YCbCrCoefficients = 0x211,
		YCbCrSubSampling = 0x212,
		YCbCrPositioning = 0x213,
		ReferenceBlackWhite = 0x214,
		Copyright = 0x8298,

		// SubIFD items
		ExposureTime = 0x829A,
		FNumber = 0x829D,
		ExposureProgram = 0x8822,
		SpectralSensitivity = 0x8824,
		ISOSpeedRatings = 0x8827,
		OECF = 0x8828,
		ExifVersion = 0x9000,
		DateTimeOriginal = 0x9003,
		DateTimeDigitized = 0x9004,
		ComponentsConfiguration = 0x9101,
		CompressedBitsPerPixel = 0x9102,
		ShutterSpeedValue = 0x9201,
		ApertureValue = 0x9202,
		BrightnessValue = 0x9203,
		ExposureBiasValue = 0x9204,
		MaxApertureValue = 0x9205,
		SubjectDistance = 0x9206,
		MeteringMode = 0x9207,
		LightSource = 0x9208,
		Flash = 0x9209,
		FocalLength = 0x920A,
		SubjectArea = 0x9214,
		MakerNote = 0x927C,
		UserComment = 0x9286,
		SubsecTime = 0x9290,
		SubsecTimeOriginal = 0x9291,
		SubsecTimeDigitized = 0x9292,
		FlashpixVersion = 0xA000,
		ColorSpace = 0xA001,
		PixelXDimension = 0xA002,
		PixelYDimension = 0xA003,
		RelatedSoundFile = 0xA004,
		FlashEnergy = 0xA20B,
		SpatialFrequencyResponse = 0xA20C,
		FocalPlaneXResolution = 0xA20E,
		FocalPlaneYResolution = 0xA20F,
		FocalPlaneResolutionUnit = 0xA210,
		SubjectLocation = 0xA214,
		ExposureIndex = 0xA215,
		SensingMethod = 0xA217,
		FileSource = 0xA300,
		SceneType = 0xA301,
		CFAPattern = 0xA302,
		CustomRendered = 0xA401,
		ExposureMode = 0xA402,
		WhiteBalance = 0xA403,
		DigitalZoomRatio = 0xA404,
		FocalLengthIn35mmFilm = 0xA405,
		SceneCaptureType = 0xA406,
		GainControl = 0xA407,
		Contrast = 0xA408,
		Saturation = 0xA409,
		Sharpness = 0xA40A,
		DeviceSettingDescription = 0xA40B,
		SubjectDistanceRange = 0xA40C,
		ImageUniqueID = 0xA420,

		// GPS subifd items
		GPSVersionID = 0x0,
		GPSLatitudeRef = 0x1,
		GPSLatitude = 0x2,
		GPSLongitudeRef = 0x3,
		GPSLongitude = 0x4,
		GPSAltitudeRef = 0x5,
		GPSAltitude = 0x6,
		GPSTimeStamp = 0x7,
		GPSSatellites = 0x8,
		GPSStatus = 0x9,
		GPSMeasureMode = 0xA,
		GPSDOP = 0xB,
		GPSSpeedRef = 0xC,
		GPSSpeed = 0xD,
		GPSTrackRef = 0xE,
		GPSTrack = 0xF,
		GPSImgDirectionRef = 0x10,
		GPSImgDirection = 0x11,
		GPSMapDatum = 0x12,
		GPSDestLatitudeRef = 0x13,
		GPSDestLatitude = 0x14,
		GPSDestLongitudeRef = 0x15,
		GPSDestLongitude = 0x16,
		GPSDestBearingRef = 0x17,
		GPSDestBearing = 0x18,
		GPSDestDistanceRef = 0x19,
		GPSDestDistance = 0x1A,
		GPSProcessingMethod = 0x1B,
		GPSAreaInformation = 0x1C,
		GPSDateStamp = 0x1D,
		GPSDifferential = 0x1E
	}
	#endregion


}
