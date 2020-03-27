using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;

using ImageComponent.InteropServices;


namespace ImageComponent.Metadata
{
	#region class Rational
	public struct Rational
	{
		public UInt32 Numerator;
		public UInt32 Denominator;

		public Rational(UInt32 numerator, UInt32 denominator)
		{
			this.Numerator = numerator;
			this.Denominator = denominator;
		}

		public Rational(UInt64 value)
		{		
			this.Numerator = (UInt32)value & 0xFFFF;
			this.Denominator = (UInt32)(value >> 32);
		}

		public UInt64 ToUInt64()
		{
			return (UInt64)((this.Denominator << 32) + this.Numerator);
		}

		public override string ToString()
		{
			return string.Format("{0}//{1}", this.Numerator.ToString(), this.Denominator.ToString());
		}
	}
	#endregion

	#region class SRational
	public struct SRational
	{
		public Int32 Numerator;
		public Int32 Denominator;

		public SRational(Int32 numerator, Int32 denominator)
		{
			this.Numerator = numerator;
			this.Denominator = denominator;
		}

		public SRational(Int64 value)
		{		
			this.Numerator = (Int32)value & 0xFFFF;
			this.Denominator = (Int32)(value >> 32);
		}

		public Int64 ToInt64()
		{
			return (Int64)((this.Denominator << 32) + this.Numerator);
		}

		public override string ToString()
		{
			return string.Format("{0}//{1}", this.Numerator.ToString(), this.Denominator.ToString());
		}
	}
	#endregion

	#region class PropertyBase
	public class PropertyBase
	{
		private bool		defined;
		private ExifTags			tagId;

		public  ExifTags TagId {get{return tagId;}}

		public PropertyBase(ExifTags tagId)
		{
			this.defined = false;
			this.tagId = tagId;
		}

		public bool Defined
		{
			get { return defined; }
			set { this.defined = value; }
		}

		public void Reset()
		{
			this.Defined = false;
		}

		public virtual void ExportToPropertyItem(PropertyItem propertyItem)
		{
			throw new Exception("Metadata.PropertyBase, ExportToPropertyItem(): must be overriden!");
		}

		public virtual void ImportFromPropertyItem(PropertyItem propertyItem)
		{
			throw new Exception("Metadata.PropertyBase, ImportFromPropertyItem(): must be overriden!");
		}

		public virtual void ImportFromPROPVARIANT(PropVariant prop)
		{
			throw new Exception("Metadata.PropertyBase, ImportFromPROPVARIANT(): must be overriden!");
		}

		public virtual PropVariant ExportToPROPVARIANT()
		{
			throw new Exception("Metadata.PropertyBase, ExportToPROPVARIANT(): must be overriden!");
		}

		public override string ToString()
		{
			throw new Exception("Metadata.PropertyBase, ToString(): must be overriden!");
		}
	}
	#endregion

	#region class LongProperty
	public class LongProperty : PropertyBase
	{
		private UInt32 value;

		public LongProperty(ExifTags tagId) : base(tagId)
		{
			this.value = 0;
		}

		public  UInt32 Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}

		public override void ExportToPropertyItem(PropertyItem propertyItem)
		{
			propertyItem.Id = (int) this.TagId;
			propertyItem.Type = (short)4;
			propertyItem.Len = 4;
			propertyItem.Value = BitConverter.GetBytes(value);
		}

		public override void ImportFromPropertyItem(PropertyItem propertyItem)
		{
			if (propertyItem.Type == 4)
				this.Value = BitConverter.ToUInt32(propertyItem.Value, 0);
			else if (propertyItem.Type == 3)
				this.Value = BitConverter.ToUInt16(propertyItem.Value, 0);
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to LONG!", propertyItem.Type));
		}

		public override void ImportFromPROPVARIANT(PropVariant prop)
		{
			if (prop.Value.GetType().Equals(typeof(UInt32)))
				this.Value = (UInt32)prop.Value;
			else if (prop.Value.GetType().Equals(typeof(UInt16)))
				this.Value = (UInt16)prop.Value;
			else if (prop.Value.GetType().Equals(typeof(Int16)))
				this.Value = (UInt32)((Int16)prop.Value);
			else if (prop.Value.GetType().Equals(typeof(Int32)))
				this.Value = (UInt32)((Int32)prop.Value);
			else if (prop.GetUnmanagedType() == System.Runtime.InteropServices.VarEnum.VT_UI4 || prop.GetUnmanagedType() == System.Runtime.InteropServices.VarEnum.VT_UI2)
				this.Value = (UInt32)prop.Value;
			//else if(prop.vt == VT_UI2)
			//	this.Value = prop.uiVal;
			else
				throw new Exception(string.Format("Can't import from PropVariant type '{0}' to LONG!", prop.GetUnmanagedType()));
		}

		public override PropVariant ExportToPROPVARIANT()
		{
			return new PropVariant(this.Value);
			//prop.vt = VT_UI2;
			//prop.uiVal = this.Value;
		}

		public override string ToString() 
		{					
			return string.Format("LONG 0x{0:X} {1}: Value = {2}", (int) this.TagId, this.TagId.ToString(), this.value.ToString());
		}
	}
	#endregion

	#region class ShortProperty
	public class ShortProperty : PropertyBase
	{
		private UInt16 value;

		public ShortProperty(ExifTags tagId) : base(tagId) 
		{ 
			this.value = 0;
		}

		public UInt16 Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}

		public override void ExportToPropertyItem(PropertyItem propertyItem) 
		{
			propertyItem.Id = (int)this.TagId;
			propertyItem.Type = (short)3;
			propertyItem.Len = 2;
			propertyItem.Value = BitConverter.GetBytes(value);
		}

		public override void ImportFromPropertyItem(PropertyItem propertyItem) 
		{
			if (propertyItem.Type == 3)
				this.Value = BitConverter.ToUInt16(propertyItem.Value, 0);
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to SHORT!", propertyItem.Type));
		}

		public override void ImportFromPROPVARIANT(PropVariant prop) 
		{
			if (prop.GetUnmanagedType() == System.Runtime.InteropServices.VarEnum.VT_UI2)
				this.Value = (UInt16) prop.Value;
			else
				throw new Exception(string.Format("Can't import from PropVariant type '{0}' to SHORT!", prop.GetUnmanagedType()));
		}

		public override PropVariant ExportToPROPVARIANT() 
		{
			return new PropVariant(this.value);
			//prop.vt = VT_UI2;
			//prop.uiVal = this.Value;
		}

		public override string ToString() 
		{
			return string.Format("SHORT 0x{0:X} {1}: Value = {2}", (int) this.TagId, this.TagId.ToString(), this.value);
		}
	}
	#endregion

	#region class LongArrayProperty
	public class LongArrayProperty : PropertyBase
	{
		private UInt32[] value;

		public LongArrayProperty(ExifTags tagId)
			: base(tagId)
		{
			this.value = new UInt32[0];
		}

		public LongArrayProperty(ExifTags tagId, int length)
			: base(tagId)
		{
			this.value = new UInt32[length];
		}

		public UInt32[] Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}

		public override void ExportToPropertyItem(PropertyItem propertyItem)
		{
			propertyItem.Id = (int)this.TagId;
			propertyItem.Type = (short)4;
			propertyItem.Len = sizeof(UInt32) * value.Length;
			propertyItem.Value = new System.Byte[propertyItem.Len];

			for (int i = 0; i < value.Length; i++)
				Array.Copy(BitConverter.GetBytes(value[i]), 0, propertyItem.Value, i * sizeof(UInt32), 2);
		}

		#region ImportFromPropertyItem()
		public override void ImportFromPropertyItem(PropertyItem propertyItem)
		{
			if (propertyItem.Type == 4)
			{
				this.Value = new UInt32[propertyItem.Len / 4];

				for (int i = 0; i < this.Value.Length; i++)
					this.Value[i] = BitConverter.ToUInt32(propertyItem.Value, i * 4);
			}
			else if (propertyItem.Type == 3)
			{
				this.Value = new UInt32[propertyItem.Len / 2];

				for (int i = 0; i < this.Value.Length; i++)
					this.Value[i] = BitConverter.ToUInt16(propertyItem.Value, i * 2);
			}
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to LONG ARRAY!", propertyItem.Type));
		}
		#endregion

		#region ImportFromPROPVARIANT()
		public override void ImportFromPROPVARIANT(PropVariant prop)
		{
			if (prop.Value.GetType().Equals(typeof(UInt32[])))
			{
				this.Value = (UInt32[])prop.Value;
			}
			else if (prop.Value.GetType().Equals(typeof(UInt16[])))
			{
				this.Value = (UInt32[])prop.Value;
			}
			else if (prop.Value.GetType().Equals(typeof(UInt32)) || prop.Value.GetType().Equals(typeof(UInt16)))
			{
				this.Value = new UInt32[1];
				this.Value[0] = (UInt32)prop.Value;
			}
			else
				throw new Exception(string.Format("Can't import from PropVariant type '{0}' to LONG ARRAY!", prop.GetUnmanagedType()));
		}
		#endregion

		#region ExportToPROPVARIANT()
		public override PropVariant ExportToPROPVARIANT()
		{
			return new PropVariant(this.value);
			//prop.marshalType = ImageComponent.InteropServices.PropVariantMarshalType.Blob;// = (VT_VECTOR | VT_UI4);		
			//prop.value = this.Value;
		}
		#endregion 

		#region ToString()
		public override string ToString()
		{
			string s = "";

			for (int i = 0; i < this.value.Length; i++)
				s += this.value[i].ToString() + " ";

			return string.Format("LONG ARRAY 0x{0:X} {1}: Value = {2}", (int) this.TagId, this.TagId.ToString(), s);
		}
		#endregion

	}
	#endregion

	#region class ShortArrayProperty
	public class ShortArrayProperty : PropertyBase
	{
		private UInt16[]	value;

		#region constructor
		public ShortArrayProperty(ExifTags tagId)
			: base(tagId)
		{
			this.value = new UInt16[0];
		}

		public ShortArrayProperty(ExifTags tagId, int length)
			: base(tagId)
		{
			this.value = new UInt16[length];
		}
		#endregion 

		#region Value
		public  UInt16[] Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}
		#endregion 

		#region ExportToPropertyItem()
		public override void ExportToPropertyItem(PropertyItem propertyItem)
		{
			propertyItem.Id = (int)this.TagId;
			propertyItem.Type = (short)3;
			propertyItem.Len = sizeof(UInt16) * value.Length;
			propertyItem.Value = new System.Byte[propertyItem.Len];

			for (int i = 0; i < value.Length; i++)
				Array.Copy(BitConverter.GetBytes(value[i]), 0, propertyItem.Value, i * 2, 2);
		}
		#endregion 

		#region ImportFromPropertyItem()
		public override void ImportFromPropertyItem(PropertyItem propertyItem)
		{
			if (propertyItem.Type == 3)
			{
				this.Value = new UInt16[propertyItem.Len / 2];

				for (int i = 0; i < this.Value.Length; i++)
					this.Value[i] = BitConverter.ToUInt16(propertyItem.Value, i * 2);
			}
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to SHORT ARRAY!", propertyItem.Type));
		}
		#endregion 

		#region ImportFromPROPVARIANT()
		public override void ImportFromPROPVARIANT(PropVariant prop)
		{
			if (prop.Value.GetType().Equals(typeof(UInt16[])))
			{
				this.Value = (UInt16[])prop.Value;
			}
			else if (prop.Value.GetType().Equals(typeof(UInt16)))
			{
				this.Value = new UInt16[1];
				this.Value[0] = (UInt16)prop.Value;
			}
			else
				throw new Exception(string.Format("Can't import from PropVariant type '{0}' to SHORT ARRAY!", prop.GetUnmanagedType()));
		}
		#endregion 

		#region ExportToPROPVARIANT()
		public override PropVariant ExportToPROPVARIANT()
		{
			return new PropVariant(this.Value);
			
			/*prop.GetUnmanagedType() = (VT_VECTOR | VT_UI2);
			prop.caui.cElems = this.Value.Length;

			int i = sizeof(USHORT);

			prop.caui.pElems = (USHORT*)malloc (sizeof(USHORT) * this.Value.Length);
			for(int i = 0; i < this.Value.Length; i++)
				prop.caui.pElems[i] = this.Value[i];*/
		}
		#endregion 

		#region ToString()
		public override string ToString()
		{
			string s = "";

			for (int i = 0; i < this.value.Length; i++)
				s += this.value[i].ToString() + " ";
			
			return string.Format("SHORT ARRAY 0x{0:X} {1}: Value = {2}", (int) this.TagId, this.TagId.ToString(), s);
		}
		#endregion 
	}
	#endregion

	#region class RationalProperty
	public class RationalProperty : PropertyBase
	{
		private Rational value = new Rational(0, 0);

		#region constructor
		public RationalProperty(ExifTags tagId) 
			: base(tagId) 
		{ 
		}
		#endregion

		#region Value
		public Rational Value 
		{ 
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}
		#endregion

		#region ExportToPropertyItem()
		public override void ExportToPropertyItem(PropertyItem propertyItem) 
		{
			propertyItem.Id = (int)this.TagId;
			propertyItem.Type = (short)5;
			propertyItem.Len = 8;
			propertyItem.Value = new System.Byte[8];

			Array.Copy( BitConverter.GetBytes(value.Numerator), propertyItem.Value, 4);
			Array.Copy( BitConverter.GetBytes(value.Denominator), 0, propertyItem.Value, 4, 4);
		}
		#endregion

		#region ImportFromPropertyItem()
		public override void ImportFromPropertyItem(PropertyItem propertyItem) 
		{
			if (propertyItem.Type == 5)
				this.Value = new Rational(BitConverter.ToUInt32(propertyItem.Value, 0), BitConverter.ToUInt32(propertyItem.Value, 4));
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to RATIONAL!", propertyItem.Type));
		}
		#endregion

		#region ImportFromPROPVARIANT()
		public override void ImportFromPROPVARIANT(PropVariant prop) 
		{
			if (prop.Value.GetType().Equals(typeof(UInt64)))
				this.Value = new Rational((UInt64)prop.Value);
			else
				throw new Exception(string.Format("Can't import from PropVariant type '{0}' to RATIONAL!", prop.GetUnmanagedType()));
		}
		#endregion

		#region ExportToPROPVARIANT()
		public override PropVariant ExportToPROPVARIANT() 
		{
			return new PropVariant(this.Value.ToUInt64());
			/*prop.GetUnmanagedType() = VT_UI8;
			prop.uhVal.LowPart = this.Value.Numerator;
			prop.uhVal.HighPart = this.Value.Denominator;*/
		}
		#endregion

		#region ToString()
		public override string ToString() 
		{
			return string.Format("RATIONAL 0x{0:X} {1}: Value = {2}/{3}", (int) this.TagId, this.TagId.ToString(), this.value.Numerator, this.value.Denominator);
		}
		#endregion

	}
	#endregion

	#region class RationalArrayProperty
	public class RationalArrayProperty : PropertyBase
	{
		private Rational[]	value;

		#region constructor
		public RationalArrayProperty(ExifTags tagId)
			: base(tagId)
		{
			this.value = new Rational[0];
		}

		public RationalArrayProperty(ExifTags tagId, int length)
			: base(tagId)
		{
			this.value = new Rational[length];
		}
		#endregion 

		#region Value
		public Rational[] Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}
		#endregion 

		#region ExportToPropertyItem()
		public override void ExportToPropertyItem(PropertyItem propertyItem) 
		{
			propertyItem.Id = (int)this.TagId;
			propertyItem.Type = (short)5;
			propertyItem.Len = value.Length * 8;

			byte[] buffer = new byte[propertyItem.Len];

			for (int i = 0; i < value.Length; i++)
			{
				Array.Copy(BitConverter.GetBytes(value[i].Numerator), 0, propertyItem.Value, i * 8, 4);
				Array.Copy(BitConverter.GetBytes(value[i].Denominator), 0, propertyItem.Value, i * 8 + 4, 4);
			}
			
			propertyItem.Value = buffer;
		}
		#endregion 

		#region ImportFromPropertyItem()
		public override void ImportFromPropertyItem(PropertyItem propertyItem) 
		{
			if (propertyItem.Type == 5)
			{
				Rational[] v = new Rational[propertyItem.Len / 8];

				for (int i = 0; i < v.Length; i++)
					v[i] = new Rational(BitConverter.ToUInt32(propertyItem.Value, i*8), BitConverter.ToUInt32(propertyItem.Value, i*8+4));

				this.Value = v;
			}
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to RATIONAL ARRAY!", propertyItem.Type));
		}
		#endregion 

		#region ImportFromPROPVARIANT()
		public override void ImportFromPROPVARIANT(PropVariant prop) 
		{
			if(prop.Value.GetType().Equals(typeof(UInt64[])))
			{				
				UInt64[] v = (UInt64[])prop.Value;
				this.Value = new Rational[v.Length];

				for(int i = 0; i < v.Length; i++)
					this.Value[i] = new Rational(v[i]);
			}
			else if(prop.Value.GetType().Equals(typeof(UInt64)))
			{
				this.Value = new Rational[1];
				this.Value[0] = new Rational((UInt64)prop.Value);
			}
			else
				throw new Exception(string.Format("Can't import from PropVariant type '{0}' to RATIONAL ARRAY!", prop.GetUnmanagedType()));
		}
		#endregion 

		#region ExportToPROPVARIANT()
		public override PropVariant ExportToPROPVARIANT()    
		{
			UInt64[] v = new UInt64[this.Value.Length];

			for (int i = 0; i < v.Length; i++)
				v[i] = this.Value[i].ToUInt64();
			
			return new PropVariant(v);
			
			/*prop.vt = (VT_VECTOR | VT_UI8);
			prop.cauh.cElems = this.Value.Length;

			prop.cauh.pElems = (ULARGE_INTEGER*)malloc (sizeof(ULARGE_INTEGER) * this.Value.Length);
			for(int i = 0; i < this.Value.Length; i++)
			{
				prop.cauh.pElems.LowPart = this.Value[i].Numerator;
				prop.cauh.pElems.HighPart = this.Value[i].Denominator;
			}*/
		}
		#endregion 

		#region ToString()
		public override string ToString()
		{
			return string.Format("RATIONAL ARRAY 0x{0:X} {1}: Value = {2}", (int) this.TagId, this.TagId.ToString(), this.value);
		}
		#endregion 

	}
	#endregion

	#region class SLongProperty
	public class SLongProperty : PropertyBase
	{
		private Int32 value;

		#region constructor
		public SLongProperty(ExifTags tagId) 
			: base(tagId) 
		{ 
			value = 0;
		}
		#endregion

		#region Value
		public Int32 Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}
		#endregion

		#region ExportToPropertyItem()
		public override void ExportToPropertyItem(PropertyItem propertyItem)
		{
			propertyItem.Id = (int)this.TagId;
			propertyItem.Type = (short)9;
			propertyItem.Len = 4;
			propertyItem.Value = BitConverter.GetBytes(value);
		}
		#endregion

		#region ImportFromPropertyItem()
		public override void ImportFromPropertyItem(PropertyItem propertyItem)
		{
			if (propertyItem.Type == 9)
				this.Value = BitConverter.ToInt32(propertyItem.Value, 0);
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to SLONG!", propertyItem.Type));
		}
		#endregion

		#region ImportFromPROPVARIANT()
		public override void ImportFromPROPVARIANT(PropVariant prop)
		{
			if (prop.Value.GetType().Equals(typeof(Int32)) || prop.Value.GetType().Equals(typeof(Int16)))
				this.Value = (Int32)prop.Value;
			//else if(prop.vt == VT_UI2)
			//	this.Value = prop.uiVal;
			else
				throw new Exception(string.Format("Can't import from PropVariant type '{0}' to SLONG!", prop.GetUnmanagedType()));
		}
		#endregion

		#region ExportToPROPVARIANT()
		public override PropVariant ExportToPROPVARIANT()
		{
			return new PropVariant(this.Value);
			/*prop.vt = VT_I4;
			prop.lVal = this.Value;*/
		}
		#endregion

		#region ToString()
		public override string ToString()
		{
			return string.Format("SLONG 0x{0:X} {1}: Value = {2}", (int) this.TagId, this.TagId.ToString(), this.value);
		}
		#endregion

	}
	#endregion

	#region class SRationalProperty
	public class SRationalProperty : PropertyBase
	{
		private SRational value = new SRational(0, 0);

		#region constructor
		public SRationalProperty(ExifTags tagId) 
			: base(tagId) 
		{ 
		}
		#endregion

		#region Value
		public SRational Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}
		#endregion

		#region ExportToPropertyItem()
		public override void ExportToPropertyItem(PropertyItem propertyItem)
		{
			propertyItem.Id = (int)this.TagId;
			propertyItem.Type = (short)10;
			propertyItem.Len = 8;
			propertyItem.Value = new byte[8];

			Array.Copy(BitConverter.GetBytes(value.Numerator), propertyItem.Value, 4);
			Array.Copy(BitConverter.GetBytes(value.Denominator), 0, propertyItem.Value, 4, 4);
		}
		#endregion

		#region ImportFromPropertyItem()
		public override void ImportFromPropertyItem(PropertyItem propertyItem)
		{
			if (propertyItem.Type == 10)
				this.Value = new SRational(BitConverter.ToInt32(propertyItem.Value, 0), BitConverter.ToInt32(propertyItem.Value, 4));
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to SRATIONAL!", propertyItem.Type));
		}
		#endregion

		#region ImportFromPROPVARIANT()
		public override void ImportFromPROPVARIANT(PropVariant prop)
		{
			if(prop.Value.GetType().Equals(typeof(Int64)))
				this.Value = new SRational((Int64)prop.Value);
			else
				throw new Exception(string.Format("Can't import from PropVariant type '{0}' to SRATIONAL!", prop.GetUnmanagedType()));
		}
		#endregion

		#region ExportToPROPVARIANT()
		public override PropVariant ExportToPROPVARIANT()
		{
			return new PropVariant(this.Value.ToInt64());

			/*prop.GetUnmanagedType() = VT_I8;
			prop.hVal.LowPart = this.Value.Numerator;
			prop.hVal.HighPart = this.Value.Denominator;*/
		}
		#endregion

		#region ToString()
		public override string ToString()
		{
			return string.Format("SRATIONAL 0x{0:X} {1}: Value = {2}/{3}", (int) this.TagId, this.TagId.ToString(), this.value.Numerator, this.value.Denominator);
		}
		#endregion

	}
	#endregion

	#region class UndefinedProperty
	//byte array propertyItem
	public class UndefinedProperty : PropertyBase
	{
		private byte[] value;

		#region constructor
		public UndefinedProperty(ExifTags tagId) 
			: base(tagId)
		{
			this.value = new byte[0];
		}

		public UndefinedProperty(ExifTags tagId, int length) 
			: base(tagId) 
		{
			this.value = new byte[length];
		}
		#endregion 

		#region Value
		public byte[] Value
		{
			 get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}
		#endregion 

		#region ExportToPropertyItem()
		public override void ExportToPropertyItem(PropertyItem propertyItem)
		{
			propertyItem.Id = (int)this.TagId;
			propertyItem.Type = (short)7;
			propertyItem.Len = value.Length;
			propertyItem.Value = Value;
		}
		#endregion 

		#region ImportFromPropertyItem()
		public override void ImportFromPropertyItem(PropertyItem propertyItem)
		{
			if (propertyItem.Type == 7)
				this.Value = propertyItem.Value;
			else if (propertyItem.Type == 2)
				this.Value = propertyItem.Value;
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to UNDEFINED!", propertyItem.Type));
		}
		#endregion 

		#region ImportFromPROPVARIANT()
		public override void ImportFromPROPVARIANT(PropVariant prop)
		{
			if(prop.Value.GetType().Equals(typeof(string)))
			{
				this.Value = System.Text.Encoding.ASCII.GetBytes((string)prop.Value);
			}
			else if (prop.Value.GetType().Equals(typeof(byte[])))
			{
				this.Value = new byte[((byte[])prop.Value).Length];
				Array.Copy((byte[])prop.Value, this.Value, ((byte[])prop.Value).Length);
			}
			else
				throw new Exception(string.Format("Can't import from PropVariant type '{0}' to UNDEFINED!", prop.GetUnmanagedType()));
		}
		#endregion 

		#region ExportToPROPVARIANT()
		public override PropVariant ExportToPROPVARIANT()
		{
			return new PropVariant(this.Value, PropVariantMarshalType.Blob);
			
			/*prop.GetUnmanagedType() = VT_LPSTR;
			prop.pszVal = (CHAR*) malloc(value.Length + 1);
			
			for(int i = 0; i < this.value.Length; i++)
				prop.pszVal[i] = this.value[i];

			prop.pszVal[value.Length] = '\0';*/
		}
		#endregion 

		#region ToString()
		public override string ToString()
		{			
			return string.Format("UNDEFINED 0x{0:X} {1}: Value = {2}", (int)this.TagId, this.TagId.ToString(), System.Text.Encoding.ASCII.GetString(this.Value));
		}
		#endregion 

		#region SetString()
		public void SetString(string str)
		{
			this.Value = Encoding.ASCII.GetBytes(str);
		}
		#endregion 

	}
	#endregion

	#region class AsciiProperty
	public class AsciiProperty : PropertyBase
	{
		private byte[] value;

		public AsciiProperty(ExifTags tagId)
			: base(tagId)
		{
			this.value = new byte[0];
		}

		public AsciiProperty(ExifTags tagId, int length)
			: base(tagId)
		{
			this.value = new byte[length];
		}

		public string Value
		{
			get { return Encoding.ASCII.GetString(this.value); }
			set
			{
				this.Defined = true;
				this.value = Encoding.ASCII.GetBytes(value);
			}
		}

		public override void ExportToPropertyItem(PropertyItem propertyItem)
		{
			propertyItem.Id = (int)this.TagId;
			propertyItem.Type = (short)2;
			propertyItem.Len = value.Length;
			propertyItem.Value = value;
		}

		public override void ImportFromPropertyItem(PropertyItem propertyItem)
		{
			if (propertyItem.Type == 2)
			{
				this.value = propertyItem.Value;
				this.Defined = true;
			}
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to ASCII!", propertyItem.Type));
		}

		public override void ImportFromPROPVARIANT(PropVariant prop)
		{
			if (prop.Value.GetType().Equals(typeof(string)))
			{
				this.Value = (string)prop.Value; 
			}
			else if (prop.Value.GetType().Equals(typeof(byte[])))
			{
				byte[] array  = new byte[((byte[])prop.Value).Length];
				Array.Copy((byte[])prop.Value, array, ((byte[])prop.Value).Length);
				this.Value = System.Text.Encoding.Unicode.GetString(array);
			}

			else
				throw new Exception(string.Format("Can't import from PropVariant type '{0}' to ASCII!", prop.GetUnmanagedType()));
		}

		#region ExportToPROPVARIANT()
		public override PropVariant ExportToPROPVARIANT()
		{
			return new PropVariant(this.Value, PropVariantMarshalType.Ascii);
			
			/*prop.GetUnmanagedType() = VT_LPSTR;
			prop.pszVal = (CHAR*) malloc(value.Length + 1);
			
			for(int i = 0; i < this.value.Length; i++)
				prop.pszVal[i] = this.value[i];

			prop.pszVal[value.Length] = '\0';*/
		}
		#endregion

		public override string ToString()
		{
			return string.Format("ASCII 0x{0:X} {1}: Value = {2}", (int)this.TagId, this.TagId.ToString(), this.Value);
		}

		public byte[] GetBytes()
		{
			return this.value;
		}

	}
	#endregion
}
