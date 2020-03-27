using System;
using ImageProcessing.Languages;

namespace ImageProcessing
{
	/// <summary>
	/// Summary description for BfException.
	/// </summary>
	public class IpException : Exception
	{
		ErrorCode	code;
		
		public IpException(ErrorCode errorCode)
			:base(MessageString(errorCode))
		{
			this.code = errorCode;
		}
		public IpException(ErrorCode errorCode, string message)
			:base(message)
		{
			this.code = errorCode;
		}

		public ErrorCode	ErrorCode	{get { return code ; } }

		private static string MessageString(ErrorCode code)
		{			
			switch (code)
			{
				case ErrorCode.OK :	return "OK.";
				case ErrorCode.InvalidParameter : return BIPStrings.InvalidParameterUsed_STR;
				case ErrorCode.ErrorNoImageLoaded : return BIPStrings.NoImageWasLoaded_STR;
				case ErrorCode.ErrorUnsupportedFormat : return BIPStrings.UnsupportedImageFormat_STR;
				case ErrorCode.ErrorUnexpected : return BIPStrings.UnexpectedErrorOccured_STR;

				case ErrorCode.FingersToMany : return BIPStrings.ThereAreAlready2FingersLocated_STR;
				case ErrorCode.FingerRegionNotInPage : return BIPStrings.FingerRegionMustBeInsideImageClip_STR;
				case ErrorCode.FingerRegionSizeIsZero: return BIPStrings.FingerRegionSizeIsZero_STR;
				
				case ErrorCode.PagesToMany : return BIPStrings.ThereAreAlready2PagesInTheImage_STR;
				case ErrorCode.ConstructPagesFirst : return BIPStrings.ConstructPagesFirst_STR;
				case ErrorCode.CantRemovePageFrom2PageImage : return BIPStrings.CanTRemovePageFrom2PageImage_STR;

				case ErrorCode.BfJust1PageAllocated : return BIPStrings.ThereIsOnly1PageInTheImage_STR;

				default : return BIPStrings.MessageCode_STR + code.ToString() + "!" ; 
			}
		}
	}

	public enum ErrorCode : int
	{
		OK = 0 ,
		
		InvalidParameter = 8,
		ErrorNoImageLoaded = 66,
		ErrorUnsupportedFormat = 97, 
		ErrorWrongFilePath = 98,
		ErrorCanNotDeleteFile = 99,
		ErrorCanNotSaveImage = 100,
		ErrorUnexpected = 127,

		FingersToMany = 200,
		FingerRegionNotInPage = 201, 
		FingerRegionSizeIsZero = 202,

		PagesToMany = 232,
		ConstructPagesFirst = 233,
		CantRemovePageFrom2PageImage = 234,

		BfJust1PageAllocated = 264
	}


}
