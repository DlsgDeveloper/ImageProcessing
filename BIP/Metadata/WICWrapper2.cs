
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

// Hiding Interface members in WIC is intentional in this MIDL compiler generated code.
#pragma warning disable 108

namespace BIP.Metadata
{
	// WIC Generated and Wrapped Classes
	public static partial class WICWrapper
	{

		[ComImport]
		[Guid("520CCA63-51A5-11D3-9144-00104BA11C5E")]
		class MSDiscMasterObj
		{
		}

		// TODO: This should be a static class
		class GUIDS
		{
			public static readonly Guid IID_IJolietDiscMaster = new Guid("E3BC42CE-4E5C-11D3-9144-00104BA11C5E");
			public static readonly Guid IID_IRedbookDiscMaster = new Guid("E3BC42CD-4E5C-11D3-9144-00104BA11C5E");
		}

		[ComImport]
		[Guid("EC9E51C1-4E5D-11D3-9144-00104BA11C5E")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IDiscMasterProgressEvents
		{
			void QueryCancel([MarshalAs(UnmanagedType.I1)] out bool pbCancel);
			void NotifyPnPActivity();
			void NotifyAddProgress(int nCompletedSteps, int nTotalSteps);
			void NotifyBlockProgress(int nCompleted, int nTotal);
			void NotifyTrackProgress(int nCurrentTrack, int nTotalTrack);
			void NotifyPreparingBurn(int nEstimatedSeconds);
			void NotifyClosingDisc(int nEstimatedSeconds);
			void NotifyBurnComplete(uint statusHR);
			void NotifyEraseComplete(uint statusHR);
		}

		[ComImport]
		[Guid("DDF445E1-54BA-11d3-9144-00104BA11C5E")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		unsafe public interface IEnumDiscMasterFormats
		{
			void Next(uint cFormats, out Guid lpiidFormatID, out uint pcFetched);
			void Skip(uint cFormats);
			void Reset();
			void Clone(void** ppEnum);
		}

		[ComImport]
		[Guid("00000000-0000-0000-c000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IUnknown
		{
		}


		[StructLayout(LayoutKind.Explicit)]
		internal struct PROPSPEC
		{
			[FieldOffset(0)]
			public uint ulKind;
			[FieldOffset(4)]
			public PROPKIND __unnamed;
		}

		[StructLayout(LayoutKind.Explicit)]
		unsafe internal struct PROPKIND
		{
			[FieldOffset(0)]
			public uint propid;
			[FieldOffset(0)]
			public char* lpwstr;
		}

		/*  struct tag_inner_PROPVARIANT
		  {
			VARTYPE vt;
			PROPVAR_PAD1   wReserved1;
			PROPVAR_PAD2   wReserved2;
			PROPVAR_PAD3   wReserved3;
			[switch_is((unsigned short) vt)] union*/

		/*[StructLayout(LayoutKind.Explicit, Size = 8, Pack = 8)]
		unsafe public struct PROPVARIANT
		{
			[FieldOffset(0)]
			public ushort vt;
			[FieldOffset(2)]
			public byte wReserved1;
			[FieldOffset(4)]
			public byte wReserved2;
			[FieldOffset(6)]
			public uint wReserved3;
			[FieldOffset(8)]
			public UnionVariable value;
		}*/

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct PROPVARIANT
		{
			public ushort vt;
			public byte wReserved1;
			public byte wReserved2;
			public uint wReserved3;
			public VariantUnion varUnion;
		}

		[StructLayout(LayoutKind.Explicit, Size = 8, Pack = 8)]
		unsafe public struct UnionVariable
		{
			[FieldOffset(0)]
			public sbyte cVal;
			[FieldOffset(0)]
			public byte bVal;
			[FieldOffset(0)]
			public short iVal;
			[FieldOffset(0)]
			public ushort uiVal;
			[FieldOffset(0)]
			public int lVal;
			[FieldOffset(0)]
			public uint ulVal;
			[FieldOffset(0)]
			public int intVal;
			[FieldOffset(0)]
			public uint uintVal;
			[FieldOffset(0)]
			public long hVal;
			[FieldOffset(0)]
			public ulong uhVal;
			[FieldOffset(0)]
			public float fltVal;
			[FieldOffset(0)]
			public double dblVal;
			[FieldOffset(0)]
			public short boolVal;
			[FieldOffset(0)]
			public int scode;
			[FieldOffset(0)]
			public CY cyVal;
			[FieldOffset(0)]
			public double date;
			[FieldOffset(0)]
			public FILETIME filetime;
			[FieldOffset(0)]
			public Guid* puuid;
			[FieldOffset(0)]
			public CLIPDATA* pclipdata;
			[FieldOffset(0)]
			public char* bstrVal;
			[FieldOffset(0)]
			public BSTRBLOB bstrblobVal;
			[FieldOffset(0)]
			public BLOB blob;
			[FieldOffset(0)]
			public sbyte* pszVal;
			[FieldOffset(0)]
			public char* pwszVal;
			[FieldOffset(0)]
			//public void* punkVal;
			public IntPtr punkVal;
			[FieldOffset(0)]
			public void* pdispVal;
			[FieldOffset(0)]
			public void* pStream;
			[FieldOffset(0)]
			public void* pStorage;
			//[FieldOffset(0)]
			//public tagVersionedStream pVersionedStream;
			[FieldOffset(0)]
			public SAFEARRAY* parray;
			[FieldOffset(0)]
			public CAC cac;
			[FieldOffset(0)]
			public CAUB caub;
			[FieldOffset(0)]
			public CAI cai;
			[FieldOffset(0)]
			public CAUI caui;
			[FieldOffset(0)]
			public CAL cal;
			[FieldOffset(0)]
			public CAUL caul;
			[FieldOffset(0)]
			public CAH cah;
			[FieldOffset(0)]
			public CAUH cauh;
			[FieldOffset(0)]
			public CAFLT caflt;
			[FieldOffset(0)]
			public CADBL cadbl;
			[FieldOffset(0)]
			public CABOOL cabool;
			[FieldOffset(0)]
			public CASCODE cascode;
			[FieldOffset(0)]
			public CACY cacy;
			[FieldOffset(0)]
			public CADATE cadate;
			[FieldOffset(0)]
			public CAFILETIME cafiletime;
			[FieldOffset(0)]
			public CACLSID cauuid;
			[FieldOffset(0)]
			public CACLIPDATA caclipdata;
			[FieldOffset(0)]
			public CABSTR cabstr;
			[FieldOffset(0)]
			public CABSTRBLOB cabstrblob;
			[FieldOffset(0)]
			public CALPSTR calpstr;
			[FieldOffset(0)]
			public CALPWSTR calpwstr;
			//[FieldOffset(0)]
			//public CAPROPVARIANT capropvar;
			[FieldOffset(0)]
			public sbyte* pcVal;
			[FieldOffset(0)]
			public byte* pbVal;
			[FieldOffset(0)]
			public short* piVal;
			[FieldOffset(0)]
			public ushort* puiVal;
			[FieldOffset(0)]
			public int* plVal;
			[FieldOffset(0)]
			public uint* pulVal;
			[FieldOffset(0)]
			public int* pintVal;
			[FieldOffset(0)]
			public uint* puintVal;
			[FieldOffset(0)]
			public float* pfltVal;
			[FieldOffset(0)]
			public double* pdblVal;
			[FieldOffset(0)]
			public short* pboolVal;
			[FieldOffset(0)]
			public tagDEC* pdecVal;
			[FieldOffset(0)]
			public int* pscode;
			[FieldOffset(0)]
			public CY* pcyVal;
			[FieldOffset(0)]
			public double* pdate;
			[FieldOffset(0)]
			public char** pbstrVal;
			[FieldOffset(0)]
			public void** ppunkVal;
			[FieldOffset(0)]
			public void** ppdispVal;
			[FieldOffset(0)]
			public SAFEARRAY** pparray;
			//[FieldOffset(0)]
			//public PROPVARIANT* pvarVal;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct CY
		{
			[FieldOffset(0)]
			public LOHI __unnamed;
			[FieldOffset(0)]
			public long int64;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct LOHI
		{
			[FieldOffset(0)]
			public uint Lo;
			[FieldOffset(4)]
			public int Hi;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct FILETIME
		{
			[FieldOffset(0)]
			public uint dwLowDateTime;
			[FieldOffset(4)]
			public uint dwHighDateTime;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CLIPDATA
		{
			[FieldOffset(0)]
			public uint cbSize;
			[FieldOffset(4)]
			public int ulClipFmt;
			[FieldOffset(8)]
			public byte* pClipData;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct BSTRBLOB
		{
			[FieldOffset(0)]
			public uint cbSize;
			[FieldOffset(4)]
			public byte* pData;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct BLOB
		{
			[FieldOffset(0)]
			public uint cbSize;
			[FieldOffset(4)]
			public byte* pBlobData;
		}



		[StructLayout(LayoutKind.Explicit)]
		unsafe struct STATSTG
		{
			[FieldOffset(0)]
			public char* pwcsName;
			[FieldOffset(4)]
			public uint type;
			[FieldOffset(8)]
			public ulong cbSize;
			[FieldOffset(16)]
			public FILETIME mtime;
			[FieldOffset(24)]
			public FILETIME ctime;
			[FieldOffset(32)]
			public FILETIME atime;
			[FieldOffset(40)]
			public uint grfMode;
			[FieldOffset(44)]
			public uint grfLocksSupported;
			[FieldOffset(48)]
			public Guid clsid;
			[FieldOffset(64)]
			public uint grfStateBits;
			[FieldOffset(68)]
			public uint reserved;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct SAFEARRAY
		{
			[FieldOffset(0)]
			public ushort cDims;
			[FieldOffset(2)]
			public ushort fFeatures;
			[FieldOffset(4)]
			public uint cbElements;
			[FieldOffset(8)]
			public uint cLocks;
			[FieldOffset(12)]
			public void* pvData;
			[FieldOffset(16)]
			public SAFEARRAYBOUND_Buffer_1 rgsabound;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct SAFEARRAYBOUND
		{
			[FieldOffset(0)]
			public uint cElements;
			[FieldOffset(4)]
			public int lLbound;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CAC
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public sbyte* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CAUB
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public byte* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CAI
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public short* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CAUI
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public ushort* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CAL
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public int* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CAUL
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public uint* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CAH
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public long* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CAUH
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public ulong* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CAFLT
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public float* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CADBL
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public double* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CABOOL
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public short* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CASCODE
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public int* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CACY
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public CY* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CADATE
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public double* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CAFILETIME
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public FILETIME* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CACLSID
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public Guid* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CACLIPDATA
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public CLIPDATA* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CABSTR
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public char** pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CABSTRBLOB
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public BSTRBLOB* pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CALPSTR
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public sbyte** pElems;
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CALPWSTR
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public char** pElems;
		}

		/*[StructLayout(LayoutKind.Explicit)]
		public unsafe struct CAPROPVARIANT
		{
			[FieldOffset(0)]
			public uint cElems;
			[FieldOffset(4)]
			public PROPVARIANT *pElems;
		}*/

		[StructLayout(LayoutKind.Explicit)]
		public struct tagDEC
		{
			[FieldOffset(0)]
			public ushort wReserved;
			[FieldOffset(0)]
			public LODECTYPE __unnamedLO;
			[FieldOffset(4)]
			public uint Hi32;
			[FieldOffset(4)]
			public HIDECTYPE __unnamedHI;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct LODECTYPE
		{
			[FieldOffset(0)]
			public SCALESIGN __unnamed;
			[FieldOffset(0)]
			public ushort signscale;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct SCALESIGN
		{
			[FieldOffset(0)]
			public byte scale;
			[FieldOffset(1)]
			public byte sign;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct HIDECTYPE
		{
			[FieldOffset(0)]
			public LOMIDTYPE __unnamed;
			[FieldOffset(0)]
			public ulong Lo64;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct LOMIDTYPE
		{
			[FieldOffset(0)]
			public uint Lo32;
			[FieldOffset(4)]
			public uint Mid32;
		}

		[ComImport]
		[Guid("00000139-0000-0000-c000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IEnumSTATPROPSTG
		{
			[PreserveSig]
			unsafe int Next(uint p1, STATPROPSTG* p2, uint* p3);
			[PreserveSig]
			int Skip(uint p1);
			[PreserveSig]
			int Reset();
			[PreserveSig]
			unsafe int Clone(void** p1);
		}

		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct STATPROPSTG
		{
			[FieldOffset(0)]
			public char* lpwstrName;
			[FieldOffset(4)]
			public uint propid;
			[FieldOffset(8)]
			public ushort vt;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct STATPROPSETSTG
		{
			[FieldOffset(0)]
			public Guid fmtid;
			[FieldOffset(16)]
			public Guid clsid;
			[FieldOffset(32)]
			public uint grfFlags;
			[FieldOffset(36)]
			public FILETIME mtime;
			[FieldOffset(44)]
			public FILETIME ctime;
			[FieldOffset(52)]
			public FILETIME atime;
			[FieldOffset(60)]
			public uint dwOSVersion;
		}

		public unsafe struct SAFEARRAYBOUND_Buffer_1
		{
			public SAFEARRAYBOUND firstElement;
			public SAFEARRAYBOUND this[int index]
			{
				get { fixed (SAFEARRAYBOUND* f = &firstElement) return f[index]; }
				set { fixed (SAFEARRAYBOUND* f = &firstElement) f[index] = value; }
			}
		}
	}
}