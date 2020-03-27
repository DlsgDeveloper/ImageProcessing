using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ImageComponent.Metadata
{
	public class ExifMetadata
	{
		int index = -1;


		#region private variables
		//tags relating to image data structure
		LongProperty l_ImageWidth;
		LongProperty l_ImageLength;
		ShortArrayProperty l_BitsPerSample;
		ShortProperty l_Compression;
		ShortProperty l_PhotometricInterpretation;
		ShortProperty l_Orientation;
		ShortProperty l_SamplesPerPixel;
		ShortProperty l_PlanarConfiguration;
		ShortArrayProperty l_YCbCrSubSampling;
		ShortProperty l_YCbCrPositioning;
		RationalProperty l_XResolution;
		RationalProperty l_YResolution;
		ShortProperty l_ResolutionUnit;

		//tags relating to recording offset
		LongArrayProperty l_StripOffsets;
		LongProperty l_RowsPerStrip;
		LongArrayProperty l_StripByteCounts;
		LongProperty l_JPEGInterchangeFormat;
		LongProperty l_JPEGInterchangeFormatLength;

		//Tags relating to image data characteristics
		ShortArrayProperty l_TransferFunction;
		RationalArrayProperty l_WhitePoint;
		RationalArrayProperty l_PrimaryChromaticities;
		RationalArrayProperty l_YCbCrCoefficients;
		RationalArrayProperty l_ReferenceBlackWhite;

		//Other tags
		AsciiProperty l_DateTime;
		AsciiProperty l_ImageDescription;
		AsciiProperty l_Make;
		AsciiProperty l_Model;
		AsciiProperty l_Software;
		AsciiProperty l_Artist;
		AsciiProperty l_Copyright;

		//Tags Relating to Version
		UndefinedProperty l_ExifVersion;
		UndefinedProperty l_FlashpixVersion;

		//Tag Relating to Image Data Characteristics
		ShortProperty l_ColorSpace;

		//Tags Relating to Image Configuration
		UndefinedProperty l_ComponentsConfiguration;
		RationalProperty l_CompressedBitsPerPixel;
		LongProperty l_PixelXDimension;
		LongProperty l_PixelYDimension;

		//Tags Relating to User Information
		UndefinedProperty l_MakerNote;
		UndefinedProperty l_UserComment;

		//Tag Relating to Related File Information
		AsciiProperty l_RelatedSoundFile;

		//Tags Relating to Date and Time
		AsciiProperty l_DateTimeOriginal;
		AsciiProperty l_DateTimeDigitized;
		AsciiProperty l_SubSecTime;
		AsciiProperty l_SubSecTimeOriginal;
		AsciiProperty l_SubSecTimeDigitized;

		//Tags Relating to Picture-Taking Conditions
		RationalProperty l_ExposureTime;
		RationalProperty l_FNumber;
		ShortProperty l_ExposureProgram;
		AsciiProperty l_SpectralSensitivity;
		ShortArrayProperty l_ISOSpeedRatings;
		UndefinedProperty l_OECF;
		SRationalProperty l_ShutterSpeedValue;
		RationalProperty l_ApertureValue;
		SRationalProperty l_BrightnessValue;
		SRationalProperty l_ExposureBiasValue;
		RationalProperty l_MaxApertureValue;
		RationalProperty l_SubjectDistance;
		ShortProperty l_MeteringMode;
		ShortProperty l_LightSource;
		ShortProperty l_Flash;
		RationalProperty l_FocalLength;
		ShortArrayProperty l_SubjectArea;
		RationalProperty l_FlashEnergy;
		UndefinedProperty l_SpatialFrequencyResponse;
		RationalProperty l_FocalPlaneXResolution;
		RationalProperty l_FocalPlaneYResolution;
		ShortProperty l_FocalPlaneResolutionUnit;
		ShortArrayProperty l_SubjectLocation;
		RationalProperty l_ExposureIndex;
		ShortProperty l_SensingMethod;
		UndefinedProperty l_FileSource;
		UndefinedProperty l_SceneType;
		UndefinedProperty l_CFAPattern;
		ShortProperty l_CustomRendered;
		ShortProperty l_ExposureMode;
		ShortProperty l_WhiteBalance;
		RationalProperty l_DigitalZoomRatio;
		ShortProperty l_FocalLengthIn35mmFilm;
		ShortProperty l_SceneCaptureType;
		RationalProperty l_GainControl;
		ShortProperty l_Contrast;
		ShortProperty l_Saturation;
		ShortProperty l_Sharpness;
		UndefinedProperty l_DeviceSettingDescription;
		ShortProperty l_SubjectDistanceRange;

		//Other Tags
		AsciiProperty l_ImageUniqueID;

		List<PropertyBase> ifdProperties;
		List<PropertyBase> ifdExifProperties;
		List<PropertyBase> properties;
		#endregion

		#region public readonly properties
		public LongProperty ImageWidth { get { return this.l_ImageWidth; } }
		public LongProperty ImageLength { get { return this.l_ImageLength; } }
		public ShortArrayProperty BitsPerSample { get { return this.l_BitsPerSample; } }
		public ShortProperty Compression { get { return this.l_Compression; } }
		public ShortProperty PhotometricInterpretation { get { return this.l_PhotometricInterpretation; } }
		public ShortProperty Orientation { get { return this.l_Orientation; } }
		public ShortProperty SamplesPerPixel { get { return this.l_SamplesPerPixel; } }
		public ShortProperty PlanarConfiguration { get { return this.l_PlanarConfiguration; } }
		public ShortArrayProperty YCbCrSubSampling { get { return this.l_YCbCrSubSampling; } }
		public ShortProperty YCbCrPositioning { get { return this.l_YCbCrPositioning; } }
		public RationalProperty XResolution { get { return this.l_XResolution; } }
		public RationalProperty YResolution { get { return this.l_YResolution; } }
		public ShortProperty ResolutionUnit { get { return this.l_ResolutionUnit; } }

		public LongArrayProperty StripOffsets { get { return this.l_StripOffsets; } }
		public LongProperty RowsPerStrip { get { return this.l_RowsPerStrip; } }
		public LongArrayProperty StripByteCounts { get { return this.l_StripByteCounts; } }
		public LongProperty JPEGInterchangeFormat { get { return this.l_JPEGInterchangeFormat; } }
		public LongProperty JPEGInterchangeFormatLength { get { return this.l_JPEGInterchangeFormatLength; } }

		public ShortArrayProperty TransferFunction { get { return this.l_TransferFunction; } }
		public RationalArrayProperty WhitePoint { get { return this.l_WhitePoint; } }
		public RationalArrayProperty PrimaryChromaticities { get { return this.l_PrimaryChromaticities; } }
		public RationalArrayProperty YCbCrCoefficients { get { return this.l_YCbCrCoefficients; } }
		public RationalArrayProperty ReferenceBlackWhite { get { return this.l_ReferenceBlackWhite; } }

		public AsciiProperty DateTime { get { return this.l_DateTime; } }
		public AsciiProperty ImageDescription { get { return this.l_ImageDescription; } }
		public AsciiProperty Make { get { return this.l_Make; } }
		public AsciiProperty Model { get { return this.l_Model; } }
		public AsciiProperty Software { get { return this.l_Software; } }
		public AsciiProperty Artist { get { return this.l_Artist; } }
		public AsciiProperty Copyright { get { return this.l_Copyright; } }

		public UndefinedProperty ExifVersion { get { return this.l_ExifVersion; } }
		public UndefinedProperty FlashpixVersion { get { return this.l_FlashpixVersion; } }

		public ShortProperty ColorSpace { get { return this.l_ColorSpace; } }

		public UndefinedProperty ComponentsConfiguration { get { return this.l_ComponentsConfiguration; } }
		public RationalProperty CompressedBitsPerPixel { get { return this.l_CompressedBitsPerPixel; } }
		public LongProperty PixelXDimension { get { return this.l_PixelXDimension; } }
		public LongProperty PixelYDimension { get { return this.l_PixelYDimension; } }

		public UndefinedProperty MakerNote { get { return this.l_MakerNote; } }
		public UndefinedProperty UserComment { get { return this.l_UserComment; } }

		public AsciiProperty RelatedSoundFile { get { return this.l_RelatedSoundFile; } }

		public AsciiProperty DateTimeOriginal { get { return this.l_DateTimeOriginal; } }
		public AsciiProperty DateTimeDigitized { get { return this.l_DateTimeDigitized; } }
		public AsciiProperty SubSecTime { get { return this.l_SubSecTime; } }
		public AsciiProperty SubSecTimeOriginal { get { return this.l_SubSecTimeOriginal; } }
		public AsciiProperty SubSecTimeDigitized { get { return this.l_SubSecTimeDigitized; } }

		public RationalProperty ExposureTime { get { return this.l_ExposureTime; } }
		public RationalProperty FNumber { get { return this.l_FNumber; } }
		public ShortProperty ExposureProgram { get { return this.l_ExposureProgram; } }
		public AsciiProperty SpectralSensitivity { get { return this.l_SpectralSensitivity; } }
		public ShortArrayProperty ISOSpeedRatings { get { return this.l_ISOSpeedRatings; } }
		public UndefinedProperty OECF { get { return this.l_OECF; } }
		public SRationalProperty ShutterSpeedValue { get { return this.l_ShutterSpeedValue; } }
		public RationalProperty ApertureValue { get { return this.l_ApertureValue; } }
		public SRationalProperty BrightnessValue { get { return this.l_BrightnessValue; } }
		public SRationalProperty ExposureBiasValue { get { return this.l_ExposureBiasValue; } }
		public RationalProperty MaxApertureValue { get { return this.l_MaxApertureValue; } }
		public RationalProperty SubjectDistance { get { return this.l_SubjectDistance; } }
		public ShortProperty MeteringMode { get { return this.l_MeteringMode; } }
		public ShortProperty LightSource { get { return this.l_LightSource; } }
		public ShortProperty Flash { get { return this.l_Flash; } }
		public RationalProperty FocalLength { get { return this.l_FocalLength; } }
		public ShortArrayProperty SubjectArea { get { return this.l_SubjectArea; } }
		public RationalProperty FlashEnergy { get { return this.l_FlashEnergy; } }
		public UndefinedProperty SpatialFrequencyResponse { get { return this.l_SpatialFrequencyResponse; } }
		public RationalProperty FocalPlaneXResolution { get { return this.l_FocalPlaneXResolution; } }
		public RationalProperty FocalPlaneYResolution { get { return this.l_FocalPlaneYResolution; } }
		public ShortProperty FocalPlaneResolutionUnit { get { return this.l_FocalPlaneResolutionUnit; } }
		public ShortArrayProperty SubjectLocation { get { return this.l_SubjectLocation; } }
		public RationalProperty ExposureIndex { get { return this.l_ExposureIndex; } }
		public ShortProperty SensingMethod { get { return this.l_SensingMethod; } }
		public UndefinedProperty FileSource { get { return this.l_FileSource; } }
		public UndefinedProperty SceneType { get { return this.l_SceneType; } }
		public UndefinedProperty CFAPattern { get { return this.l_CFAPattern; } }
		public ShortProperty CustomRendered { get { return this.l_CustomRendered; } }
		public ShortProperty ExposureMode { get { return this.l_ExposureMode; } }
		public ShortProperty WhiteBalance { get { return this.l_WhiteBalance; } }
		public RationalProperty DigitalZoomRatio { get { return this.l_DigitalZoomRatio; } }
		public ShortProperty FocalLengthIn35mmFilm { get { return this.l_FocalLengthIn35mmFilm; } }
		public ShortProperty SceneCaptureType { get { return this.l_SceneCaptureType; } }
		public RationalProperty GainControl { get { return this.l_GainControl; } }
		public ShortProperty Contrast { get { return this.l_Contrast; } }
		public ShortProperty Saturation { get { return this.l_Saturation; } }
		public ShortProperty Sharpness { get { return this.l_Sharpness; } }
		public UndefinedProperty DeviceSettingDescription { get { return this.l_DeviceSettingDescription; } }
		public ShortProperty SubjectDistanceRange { get { return this.l_SubjectDistanceRange; } }

		public AsciiProperty ImageUniqueID { get { return this.l_ImageUniqueID; } }
		#endregion

		#region constructor
		public ExifMetadata()
		{
			//tags relating to image data structure
			this.l_ImageWidth = new LongProperty(ExifTags.ImageWidth);
			this.l_ImageLength = new LongProperty(ExifTags.ImageLength);
			this.l_BitsPerSample = new ShortArrayProperty(ExifTags.BitsPerSample, 3);
			this.l_Compression = new ShortProperty(ExifTags.Compression);
			this.l_PhotometricInterpretation = new ShortProperty(ExifTags.PhotometricInterpretation);
			this.l_Orientation = new ShortProperty(ExifTags.Orientation);
			this.l_SamplesPerPixel = new ShortProperty(ExifTags.SamplesPerPixel);
			this.l_PlanarConfiguration = new ShortProperty(ExifTags.PlanarConfiguration);
			this.l_YCbCrSubSampling = new ShortArrayProperty(ExifTags.YCbCrSubSampling, 2);
			this.l_YCbCrPositioning = new ShortProperty(ExifTags.YCbCrPositioning);
			this.l_XResolution = new RationalProperty(ExifTags.XResolution);
			this.l_YResolution = new RationalProperty(ExifTags.YResolution);
			this.l_ResolutionUnit = new ShortProperty(ExifTags.ResolutionUnit);

			//tags relating to recording offset
			this.l_StripOffsets = new LongArrayProperty(ExifTags.StripOffsets);
			this.l_RowsPerStrip = new LongProperty(ExifTags.RowsPerStrip);
			this.l_StripByteCounts = new LongArrayProperty(ExifTags.StripByteCounts);
			this.l_JPEGInterchangeFormat = new LongProperty(ExifTags.JPEGInterchangeFormat);
			this.l_JPEGInterchangeFormatLength = new LongProperty(ExifTags.JPEGInterchangeFormatLength);

			//Tags relating to image data characteristics
			this.l_TransferFunction = new ShortArrayProperty(ExifTags.TransferFunction, 3 * 256);
			this.l_WhitePoint = new RationalArrayProperty(ExifTags.WhitePoint, 2);
			this.l_PrimaryChromaticities = new RationalArrayProperty(ExifTags.PrimaryChromaticities, 6);
			this.l_YCbCrCoefficients = new RationalArrayProperty(ExifTags.YCbCrCoefficients, 3);
			this.l_ReferenceBlackWhite = new RationalArrayProperty(ExifTags.ReferenceBlackWhite, 6);

			//Other tags
			this.l_DateTime = new AsciiProperty(ExifTags.DateTime, 20);
			this.l_ImageDescription = new AsciiProperty(ExifTags.ImageDescription);
			this.l_Make = new AsciiProperty(ExifTags.Make);
			this.l_Model = new AsciiProperty(ExifTags.Model);
			this.l_Software = new AsciiProperty(ExifTags.Software);
			this.l_Artist = new AsciiProperty(ExifTags.Artist);
			this.l_Copyright = new AsciiProperty(ExifTags.Copyright);

			//Tags Relating to Version
			this.l_ExifVersion = new UndefinedProperty(ExifTags.ExifVersion, 4);
			this.l_FlashpixVersion = new UndefinedProperty(ExifTags.FlashpixVersion, 4);

			//Tag Relating to Image Data Characteristics
			this.l_ColorSpace = new ShortProperty(ExifTags.ColorSpace);

			//Tags Relating to Image Configuration
			this.l_ComponentsConfiguration = new UndefinedProperty(ExifTags.ComponentsConfiguration, 4);
			this.l_CompressedBitsPerPixel = new RationalProperty(ExifTags.CompressedBitsPerPixel);
			this.l_PixelXDimension = new LongProperty(ExifTags.PixelXDimension);
			this.l_PixelYDimension = new LongProperty(ExifTags.PixelYDimension);

			//Tags Relating to User Information
			this.l_MakerNote = new UndefinedProperty(ExifTags.MakerNote);
			this.l_UserComment = new UndefinedProperty(ExifTags.UserComment);

			//Tag Relating to Related File Information
			this.l_RelatedSoundFile = new AsciiProperty(ExifTags.RelatedSoundFile, 13);

			//Tags Relating to Date and Time
			this.l_DateTimeOriginal = new AsciiProperty(ExifTags.DateTimeOriginal, 20);
			this.l_DateTimeDigitized = new AsciiProperty(ExifTags.DateTimeDigitized, 20);
			this.l_SubSecTime = new AsciiProperty(ExifTags.SubsecTime);
			this.l_SubSecTimeOriginal = new AsciiProperty(ExifTags.SubsecTimeOriginal);
			this.l_SubSecTimeDigitized = new AsciiProperty(ExifTags.SubsecTimeDigitized);

			//Tags Relating to Picture-Taking Conditions
			this.l_ExposureTime = new RationalProperty(ExifTags.ExposureTime);
			this.l_FNumber = new RationalProperty(ExifTags.FNumber);
			this.l_ExposureProgram = new ShortProperty(ExifTags.ExposureProgram);
			this.l_SpectralSensitivity = new AsciiProperty(ExifTags.SpectralSensitivity);
			this.l_ISOSpeedRatings = new ShortArrayProperty(ExifTags.ISOSpeedRatings);
			this.l_OECF = new UndefinedProperty(ExifTags.OECF);
			this.l_ShutterSpeedValue = new SRationalProperty(ExifTags.ShutterSpeedValue);
			this.l_ApertureValue = new RationalProperty(ExifTags.ApertureValue);
			this.l_BrightnessValue = new SRationalProperty(ExifTags.BrightnessValue);
			this.l_ExposureBiasValue = new SRationalProperty(ExifTags.ExposureBiasValue);
			this.l_MaxApertureValue = new RationalProperty(ExifTags.MaxApertureValue);
			this.l_SubjectDistance = new RationalProperty(ExifTags.SubjectDistance);
			this.l_MeteringMode = new ShortProperty(ExifTags.MeteringMode);
			this.l_LightSource = new ShortProperty(ExifTags.LightSource);
			this.l_Flash = new ShortProperty(ExifTags.Flash);
			this.l_FocalLength = new RationalProperty(ExifTags.FocalLength);
			this.l_SubjectArea = new ShortArrayProperty(ExifTags.SubjectArea);
			this.l_FlashEnergy = new RationalProperty(ExifTags.FlashEnergy);
			this.l_SpatialFrequencyResponse = new UndefinedProperty(ExifTags.SpatialFrequencyResponse);
			this.l_FocalPlaneXResolution = new RationalProperty(ExifTags.FocalPlaneXResolution);
			this.l_FocalPlaneYResolution = new RationalProperty(ExifTags.FocalPlaneYResolution);
			this.l_FocalPlaneResolutionUnit = new ShortProperty(ExifTags.FocalPlaneResolutionUnit);
			this.l_SubjectLocation = new ShortArrayProperty(ExifTags.SubjectLocation, 2);
			this.l_ExposureIndex = new RationalProperty(ExifTags.ExposureIndex);
			this.l_SensingMethod = new ShortProperty(ExifTags.SensingMethod);
			this.l_FileSource = new UndefinedProperty(ExifTags.FileSource, 1);
			this.l_SceneType = new UndefinedProperty(ExifTags.SceneType, 1);
			this.l_CFAPattern = new UndefinedProperty(ExifTags.CFAPattern);
			this.l_CustomRendered = new ShortProperty(ExifTags.CustomRendered);
			this.l_ExposureMode = new ShortProperty(ExifTags.ExposureMode);
			this.l_WhiteBalance = new ShortProperty(ExifTags.WhiteBalance);
			this.l_DigitalZoomRatio = new RationalProperty(ExifTags.DigitalZoomRatio);
			this.l_FocalLengthIn35mmFilm = new ShortProperty(ExifTags.FocalLengthIn35mmFilm);
			this.l_SceneCaptureType = new ShortProperty(ExifTags.SceneCaptureType);
			this.l_GainControl = new RationalProperty(ExifTags.GainControl);
			this.l_Contrast = new ShortProperty(ExifTags.Contrast);
			this.l_Saturation = new ShortProperty(ExifTags.Saturation);
			this.l_Sharpness = new ShortProperty(ExifTags.Sharpness);
			this.l_DeviceSettingDescription = new UndefinedProperty(ExifTags.DeviceSettingDescription);
			this.l_SubjectDistanceRange = new ShortProperty(ExifTags.SubjectDistanceRange);

			//Other Tags
			this.l_ImageUniqueID = new AsciiProperty(ExifTags.ImageUniqueID, 33);


			this.ifdProperties = new List<PropertyBase>();
			this.ifdProperties.Add(ImageWidth);
			this.ifdProperties.Add(ImageLength);
			this.ifdProperties.Add(BitsPerSample);
			this.ifdProperties.Add(Compression);
			this.ifdProperties.Add(PhotometricInterpretation);
			this.ifdProperties.Add(Orientation);
			this.ifdProperties.Add(SamplesPerPixel);
			this.ifdProperties.Add(PlanarConfiguration);
			this.ifdProperties.Add(YCbCrSubSampling);
			this.ifdProperties.Add(YCbCrPositioning);
			this.ifdProperties.Add(XResolution);
			this.ifdProperties.Add(YResolution);
			this.ifdProperties.Add(ResolutionUnit);

			this.ifdProperties.Add(StripOffsets);
			this.ifdProperties.Add(RowsPerStrip);
			this.ifdProperties.Add(StripByteCounts);
			this.ifdProperties.Add(JPEGInterchangeFormat);
			this.ifdProperties.Add(JPEGInterchangeFormatLength);

			this.ifdProperties.Add(TransferFunction);
			this.ifdProperties.Add(WhitePoint);
			this.ifdProperties.Add(PrimaryChromaticities);
			this.ifdProperties.Add(YCbCrCoefficients);
			this.ifdProperties.Add(ReferenceBlackWhite);

			this.ifdProperties.Add(DateTime);
			this.ifdProperties.Add(ImageDescription);
			this.ifdProperties.Add(Make);
			this.ifdProperties.Add(Model);
			this.ifdProperties.Add(Software);
			this.ifdProperties.Add(Artist);
			this.ifdProperties.Add(Copyright);


			this.ifdExifProperties = new List<PropertyBase>();
			this.ifdExifProperties.Add(ExifVersion);
			this.ifdExifProperties.Add(FlashpixVersion);

			this.ifdExifProperties.Add(ColorSpace);

			this.ifdExifProperties.Add(ComponentsConfiguration);
			this.ifdExifProperties.Add(CompressedBitsPerPixel);
			this.ifdExifProperties.Add(PixelXDimension);
			this.ifdExifProperties.Add(PixelYDimension);

			this.ifdExifProperties.Add(MakerNote);
			this.ifdExifProperties.Add(UserComment);

			this.ifdExifProperties.Add(RelatedSoundFile);

			this.ifdExifProperties.Add(DateTimeOriginal);
			this.ifdExifProperties.Add(DateTimeDigitized);
			this.ifdExifProperties.Add(SubSecTime);
			this.ifdExifProperties.Add(SubSecTimeOriginal);
			this.ifdExifProperties.Add(SubSecTimeDigitized);

			this.ifdExifProperties.Add(ExposureTime);
			this.ifdExifProperties.Add(FNumber);
			this.ifdExifProperties.Add(ExposureProgram);
			this.ifdExifProperties.Add(SpectralSensitivity);
			this.ifdExifProperties.Add(ISOSpeedRatings);
			this.ifdExifProperties.Add(OECF);
			this.ifdExifProperties.Add(ShutterSpeedValue);
			this.ifdExifProperties.Add(ApertureValue);
			this.ifdExifProperties.Add(BrightnessValue);
			this.ifdExifProperties.Add(ExposureBiasValue);
			this.ifdExifProperties.Add(MaxApertureValue);
			this.ifdExifProperties.Add(SubjectDistance);
			this.ifdExifProperties.Add(MeteringMode);
			this.ifdExifProperties.Add(LightSource);
			this.ifdExifProperties.Add(Flash);
			this.ifdExifProperties.Add(FocalLength);
			this.ifdExifProperties.Add(SubjectArea);
			this.ifdExifProperties.Add(FlashEnergy);
			this.ifdExifProperties.Add(SpatialFrequencyResponse);
			this.ifdExifProperties.Add(FocalPlaneXResolution);
			this.ifdExifProperties.Add(FocalPlaneYResolution);
			this.ifdExifProperties.Add(FocalPlaneResolutionUnit);
			this.ifdExifProperties.Add(SubjectLocation);
			this.ifdExifProperties.Add(ExposureIndex);
			this.ifdExifProperties.Add(SensingMethod);
			this.ifdExifProperties.Add(FileSource);
			this.ifdExifProperties.Add(SceneType);
			this.ifdExifProperties.Add(CFAPattern);
			this.ifdExifProperties.Add(CustomRendered);
			this.ifdExifProperties.Add(ExposureMode);
			this.ifdExifProperties.Add(WhiteBalance);
			this.ifdExifProperties.Add(DigitalZoomRatio);
			this.ifdExifProperties.Add(FocalLengthIn35mmFilm);
			this.ifdExifProperties.Add(SceneCaptureType);
			this.ifdExifProperties.Add(GainControl);
			this.ifdExifProperties.Add(Contrast);
			this.ifdExifProperties.Add(Saturation);
			this.ifdExifProperties.Add(Sharpness);
			this.ifdExifProperties.Add(DeviceSettingDescription);
			this.ifdExifProperties.Add(SubjectDistanceRange);

			this.ifdExifProperties.Add(ImageUniqueID);


			this.properties = new List<PropertyBase>();
			this.properties.AddRange(this.ifdProperties);
			this.properties.AddRange(this.ifdExifProperties);
		}
		#endregion

		#region Properties()

		public List<PropertyBase> Properties { get { return properties; } }

		#endregion

		#region GetProperty()
		public PropertyBase GetProperty(int propertyId)
		{
			for (int i = 0; i < this.Properties.Count; i++)
				if ((int)this.Properties[i].TagId == propertyId)
					return this.Properties[i];

			return null;
		}
		#endregion

		#region GetJpegPropertyPath()
		public string GetJpegPropertyPath(PropertyBase propertyBase)
		{
			for (int i = 0; i < this.ifdProperties.Count; i++)
			{
				if (this.ifdProperties[i].TagId == propertyBase.TagId)
					return string.Format("/app1/ifd/{1}ushort={0}{2}", (int)this.ifdProperties[i].TagId, "{", "}");
			}

			for (int i = 0; i < this.ifdExifProperties.Count; i++)
			{
				if (this.ifdExifProperties[i].TagId == propertyBase.TagId)
					return string.Format("/app1/ifd/exif/{1}ushort={0}{2}", (int)this.ifdExifProperties[i].TagId, "{", "}");
			}

			return null;
		}
		#endregion

		#region GetTiffPropertyPath()
		public string GetTiffPropertyPath(PropertyBase propertyBase)
		{
			for (int i = 0; i < this.ifdProperties.Count; i++)
			{
				if (this.ifdProperties[i].TagId == propertyBase.TagId)
					return string.Format("/ifd/{1}ushort={0}{2}", (int)this.ifdProperties[i].TagId, "{", "}");
			}

			return null;
		}
		#endregion

		#region GetPngPropertyPath()
		/* under Windows XP, only 1 property can be stored in /tEXt/{str=*} tag */
		public string GetPngPropertyPath(PropertyBase propertyBase)
		{
			string tagName = null;
			
			/*if(propertyBase.TagId == this.UserComment.TagId)
				tagName = "Comment";
			else if(propertyBase.TagId == this.Artist.TagId)
				tagName = "Author";
			else if(propertyBase.TagId == this.Copyright.TagId)
				tagName = "Copyright";
			else*/ if(propertyBase.TagId == this.ImageDescription.TagId)
				tagName = "Description";
			/*else if(propertyBase.TagId == this.Software.TagId)
				tagName = "Software";
			else if(propertyBase.TagId == this.Model.TagId)
				tagName = "Source";*/

			if(tagName != null)
			{
				index++;
				return string.Format("/[{3}]tEXt/{1}str={0}{2}", tagName, "{", "}", index);
				//return string.Format("/[{3}]tEXt/{1}str={0}{2}", tagName, "{", "}", index);
			}
					
			return null;
		}
		#endregion
	}
}
