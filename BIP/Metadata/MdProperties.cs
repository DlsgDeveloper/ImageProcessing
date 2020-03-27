using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;

namespace BIP.Metadata
{
	
	#region FieldType
	public enum FieldType :ushort
	{
		Byte = 1,
		Ascii = 2,
		Int16 = 3,
		Int32 = 4,
		Rational = 5,
		Sbyte = 6,
		UndefByte = 7,
		SInt16 = 8,
		SInt32 = 9,
		SRational = 10,
		Float = 11,
		Double = 12
	}
	#endregion


	#region struct Rational
	public struct Rational
	{
		public UInt32 Numerator;
		public UInt32 Denominator;

		public Rational(UInt32 numerator, UInt32 denominator)
		{
			this.Numerator = numerator;
			this.Denominator = denominator;
		}
	}
	#endregion

	#region struct SRational
	public struct SRational
	{
		public Int32 Numerator;
		public Int32 Denominator;

		public SRational(Int32 numerator, Int32 denominator)
		{
			this.Numerator = numerator;
			this.Denominator = denominator;
		}
	}
	#endregion

	#region class PropertyBase
	public class PropertyBase
	{
		bool defined = false;
		public readonly ExifTags TagId;

		public PropertyBase(ExifTags tagId)
		{
			this.TagId = tagId;
		}

		public bool Defined
		{
			get { return defined; }
			protected set { this.defined = value; }
		}

		public void Reset()
		{
			this.Defined = false;
		}

		public virtual void ExportToPropertyItem(PropertyItem property)
		{
			throw new Exception("BIP.Metadata.PropertyBase, ExportToPropertyItem(): must be overriden!");
		}

		public virtual void ImportFromPropertyItem(PropertyItem property)
		{
			throw new Exception("BIP.Metadata.PropertyBase, ImportFromPropertyItem(): must be overriden!");
		}

		public virtual void ImportFromImageComponentMetadata(ImageComponent.Metadata.PropertyBase imageComponentProperty)
		{
			throw new Exception("BIP.Metadata.PropertyBase, ImportFromImageComponentMetadata(): must be overriden!");
		}

		public override string ToString()
		{
			throw new Exception("BIP.Metadata.PropertyBase, ToString(): must be overriden!");
		}
	}
	#endregion

	#region class LongProperty
	public class LongProperty : PropertyBase
	{
		UInt32 value = 0;

		public LongProperty(ExifTags tagId) : base(tagId) { }

		public UInt32 Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}

		public override void ExportToPropertyItem(PropertyItem property)
		{
			property.Id = (int) this.TagId;
			property.Type = (short)4;
			property.Len = 4;
			property.Value = BitConverter.GetBytes(value);
		}

		public override void ImportFromPropertyItem(PropertyItem property)
		{
			if (property.Type == 4)
				this.Value = BitConverter.ToUInt32(property.Value, 0);
			else if (property.Type == 3)
				this.Value = BitConverter.ToUInt16(property.Value, 0);
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to LONG!", property.Type));
		}

		public override void ImportFromImageComponentMetadata(ImageComponent.Metadata.PropertyBase property) 
		{
			if (property is ImageComponent.Metadata.LongProperty)
				this.Value = ((ImageComponent.Metadata.LongProperty)property).Value;
			else if (property is ImageComponent.Metadata.ShortProperty)
				this.Value = ((ImageComponent.Metadata.ShortProperty)property).Value;
			else
				throw new Exception(string.Format("Can't import from ImageComponent.Metadata.PropertyBase type '{0}' to LONG!", property.TagId));
		}

		public override string ToString()
		{
			return string.Format("LONG 0x{0:X} {1}: Value = {2}", (int)this.TagId, this.TagId, this.value);
		}
	}
	#endregion

	#region class ShortProperty
	public class ShortProperty : PropertyBase
	{
		UInt16 value = 0;

		public ShortProperty(ExifTags tagId) : base(tagId) { }

		public UInt16 Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}

		public override void ExportToPropertyItem(PropertyItem property)
		{
			property.Id = (int)this.TagId;
			property.Type = (short)3;
			property.Len = 2;
			property.Value = BitConverter.GetBytes(value);
		}

		public override void ImportFromPropertyItem(PropertyItem property)
		{
			if (property.Type == 3)
				this.Value = BitConverter.ToUInt16(property.Value, 0);
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to SHORT!", property.Type));
		}

		public override void ImportFromImageComponentMetadata(ImageComponent.Metadata.PropertyBase property)
		{
			if (property is ImageComponent.Metadata.ShortProperty)
				this.Value = ((ImageComponent.Metadata.ShortProperty)property).Value;
			else
				throw new Exception(string.Format("Can't import from ImageComponent.Metadata.PropertyBase type '{0}' to SHORT!", property.TagId));
		}

		public override string ToString()
		{
			return string.Format("SHORT 0x{0:X} {1}: Value = {2}", (int)this.TagId, this.TagId, this.value);
		}
	}
	#endregion

	#region class LongArrayProperty
	public class LongArrayProperty : PropertyBase
	{
		UInt32[] value;
		int lenght = -1;

		public LongArrayProperty(ExifTags tagId)
			: base(tagId)
		{
			this.value = new UInt32[0];
		}

		public LongArrayProperty(ExifTags tagId, int lenght)
			: base(tagId)
		{
			this.value = new UInt32[lenght];
			this.lenght = lenght;
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

		public override void ExportToPropertyItem(PropertyItem property)
		{
			property.Id = (int)this.TagId;
			property.Type = (short)4;
			property.Len = 4 * ((lenght >= 0) ? lenght : value.Length);
			property.Value = new byte[property.Len];

			for (int i = 0; i < value.Length; i++)
				Array.Copy(BitConverter.GetBytes(value[i]), 0, property.Value, i * 4, 2);
		}

		public override void ImportFromPropertyItem(PropertyItem property)
		{
			if (property.Type == 4)
			{
				UInt32[] val = new UInt32[property.Len / 4];

				for (int i = 0; i < val.Length; i++)
					val[i] = BitConverter.ToUInt32(property.Value, i * 4);

				this.Value = val;
			}
			else if (property.Type == 3)
			{
				UInt32[] val = new UInt32[property.Len / 2];

				for (int i = 0; i < val.Length; i++)
					val[i] = BitConverter.ToUInt16(property.Value, i * 2);

				this.Value = val;
			}
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to LONG ARRAY!", property.Type));
		}

		public override void ImportFromImageComponentMetadata(ImageComponent.Metadata.PropertyBase property)
		{
			if (property is ImageComponent.Metadata.LongArrayProperty)
				this.Value = ((ImageComponent.Metadata.LongArrayProperty)property).Value;
			else if (property is ImageComponent.Metadata.ShortArrayProperty)
			{
				this.Value = new uint[((ImageComponent.Metadata.ShortArrayProperty)property).Value.Length];

				for (int i = 0; i < ((ImageComponent.Metadata.ShortArrayProperty)property).Value.Length; i++)
					this.Value[i] = ((ImageComponent.Metadata.ShortArrayProperty)property).Value[i];
			}
			else
				throw new Exception(string.Format("Can't import from ImageComponent.Metadata.PropertyBase type '{0}' to LONG ARRAY!", property.TagId));
		}

		public override string ToString()
		{
			string s = "";

			foreach (uint b in this.value)
				s += b.ToString() + " ";

			return string.Format("LONG ARRAY 0x{0:X} {1}: Value = {2}", (int)this.TagId, this.TagId, s);
		}
	}
	#endregion

	#region class ShortArrayProperty
	public class ShortArrayProperty : PropertyBase
	{
		UInt16[]	value;
		int			lenght = -1;

		public ShortArrayProperty(ExifTags tagId)
			: base(tagId)
		{
			this.value = new UInt16[0];
		}

		public ShortArrayProperty(ExifTags tagId, int lenght)
			: base(tagId)
		{
			this.value = new UInt16[lenght];
			this.lenght = lenght;
		}

		public UInt16[] Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}

		public override void ExportToPropertyItem(PropertyItem property)
		{
			property.Id = (int)this.TagId;
			property.Type = (short)3;
			property.Len = 2 * ((lenght >= 0) ? lenght : value.Length);
			property.Value = new byte[property.Len];

			for (int i = 0; i < value.Length; i++)
				Array.Copy(BitConverter.GetBytes(value[i]), 0, property.Value, i * 2, 2);
		}

		public override void ImportFromPropertyItem(PropertyItem property)
		{
			if (property.Type == 3)
			{
				UInt16[] val = new UInt16[property.Len / 2];

				for (int i = 0; i < val.Length; i++)
					val[i] = BitConverter.ToUInt16(property.Value, i * 2);

				this.Value = val;
			}
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to SHORT ARRAY!", property.Type));
		}

		public override void ImportFromImageComponentMetadata(ImageComponent.Metadata.PropertyBase property)
		{
			if (property is ImageComponent.Metadata.ShortArrayProperty)
				this.Value = ((ImageComponent.Metadata.ShortArrayProperty)property).Value;
			else
				throw new Exception(string.Format("Can't import from ImageComponent.Metadata.PropertyBase type '{0}' to SHORT ARRAY!", property.TagId));
		}

		public override string ToString()
		{
			string s = "";

			foreach (ushort b in this.value)
				s += b.ToString() + " ";

			return string.Format("SHORT ARRAY 0x{0:X} {1}: Value = {2}", (int)this.TagId, this.TagId, s);
		}
	}
	#endregion

	#region class RationalProperty
	public class RationalProperty : PropertyBase
	{
		Rational value = new Rational() { Numerator = 0, Denominator = 0 };

		public RationalProperty(ExifTags tagId) : base(tagId) { }

		public Rational Value 
		{ 
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}

		public override void ExportToPropertyItem(PropertyItem property)
		{
			property.Id = (int)this.TagId;
			property.Type = (short)5;
			property.Len = 8;
			property.Value = new byte[8];

			Array.Copy( BitConverter.GetBytes(value.Numerator), property.Value, 4);
			Array.Copy( BitConverter.GetBytes(value.Denominator), 0, property.Value, 4, 4);
		}

		public override void ImportFromPropertyItem(PropertyItem property)
		{
			if (property.Type == 5)
				this.Value = new Rational(BitConverter.ToUInt32(property.Value, 0), BitConverter.ToUInt32(property.Value, 4));
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to RATIONAL!", property.Type));
		}

		public override void ImportFromImageComponentMetadata(ImageComponent.Metadata.PropertyBase property)
		{
			if (property is ImageComponent.Metadata.RationalProperty)
			{
				this.Value = new Rational(((ImageComponent.Metadata.RationalProperty)property).Value.Numerator, 
					((ImageComponent.Metadata.RationalProperty)property).Value.Denominator);
			}
			else
				throw new Exception(string.Format("Can't import from ImageComponent.Metadata.PropertyBase type '{0}' to RATIONAL!", property.TagId));
		}

		public override string ToString()
		{
			return string.Format("RATIONAL 0x{0:X} {1}: Value = {2}/{3}", (int)this.TagId, this.TagId, this.value.Numerator, this.value.Denominator);
		}
	}
	#endregion

	#region class RationalArrayProperty
	public class RationalArrayProperty : PropertyBase
	{
		Rational[] value;
		int lenght = -1;

		public RationalArrayProperty(ExifTags tagId)
			: base(tagId)
		{
			this.value = new Rational[0];
		}

		public RationalArrayProperty(ExifTags tagId, int lenght)
			: base(tagId)
		{
			this.value = new Rational[lenght];
			this.lenght = lenght;
		}

		public Rational[] Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}

		public override void ExportToPropertyItem(PropertyItem property)
		{
			property.Id = (int)this.TagId;
			property.Type = (short)5;
			property.Len = value.Length * 8;

			byte[] buffer = new byte[property.Len];

			for (int i = 0; i < Value.Length; i++)
			{
				Array.Copy(BitConverter.GetBytes(value[i].Numerator), 0, property.Value, i * 8, 4);
				Array.Copy(BitConverter.GetBytes(value[i].Denominator), 0, property.Value, i * 8 + 4, 4);
			}
			
			property.Value = buffer;
		}

		public override void ImportFromPropertyItem(PropertyItem property)
		{
			if (property.Type == 5)
			{
				Rational[] v = new Rational[property.Len / 8];

				for (int i = 0; i < v.Length; i++)
					v[i] = new Rational(BitConverter.ToUInt32(property.Value, i*8), BitConverter.ToUInt32(property.Value, i*8+4));

				this.Value = v;
			}
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to RATIONAL ARRAY!", property.Type));
		}

		public override void ImportFromImageComponentMetadata(ImageComponent.Metadata.PropertyBase property)
		{
			if (property is ImageComponent.Metadata.RationalArrayProperty)
			{
				ImageComponent.Metadata.Rational[] array = ((ImageComponent.Metadata.RationalArrayProperty)property).Value;
				
				this.Value = new Rational[array.Length];

				for (int i = 0; i < array.Length; i++)
				{
					this.value[i].Numerator = array[i].Numerator;
					this.value[i].Denominator = array[i].Denominator;
				}
			}
			else
				throw new Exception(string.Format("Can't import from ImageComponent.Metadata.PropertyBase type '{0}' to RATIONAL ARRAY!", property.TagId));
		}

		public override string ToString()
		{
			return string.Format("RATIONAL ARRAY 0x{0:X} {1}: Value = {2}", (int)this.TagId, this.TagId, this.value);
		}
	}
	#endregion

	#region class SLongProperty
	public class SLongProperty : PropertyBase
	{
		Int32 value = 0;

		public SLongProperty(ExifTags tagId) : base(tagId) { }

		public Int32 Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}

		public override void ExportToPropertyItem(PropertyItem property)
		{
			property.Id = (int)this.TagId;
			property.Type = (short)9;
			property.Len = 4;
			property.Value = BitConverter.GetBytes(value);
		}

		public override void ImportFromPropertyItem(PropertyItem property)
		{
			if (property.Type == 9)
				this.Value = BitConverter.ToInt32(property.Value, 0);
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to SLONG!", property.Type));
		}

		public override void ImportFromImageComponentMetadata(ImageComponent.Metadata.PropertyBase property)
		{
			if (property is ImageComponent.Metadata.SLongProperty)
				this.Value = ((ImageComponent.Metadata.SLongProperty)property).Value;
			else
				throw new Exception(string.Format("Can't import from ImageComponent.Metadata.PropertyBase type '{0}' to SLONG!", property.TagId));
		}

		public override string ToString()
		{
			return string.Format("SLONG 0x{0:X} {1}: Value = {2}", (int)this.TagId, this.TagId, this.value);
		}
	}
	#endregion

	#region class SRationalProperty
	public class SRationalProperty : PropertyBase
	{
		SRational value = new SRational() { Numerator = 0, Denominator = 0 };

		public SRationalProperty(ExifTags tagId) : base(tagId) { }

		public SRational Value
		{
			get { return this.value; }
			set
			{
				this.Defined = true;
				this.value = value;
			}
		}

		public override void ExportToPropertyItem(PropertyItem property)
		{
			property.Id = (int)this.TagId;
			property.Type = (short)10;
			property.Len = 8;
			property.Value = new byte[8];

			Array.Copy(BitConverter.GetBytes(value.Numerator), property.Value, 4);
			Array.Copy(BitConverter.GetBytes(value.Denominator), 0, property.Value, 4, 4);
		}

		public override void ImportFromPropertyItem(PropertyItem property)
		{
			if (property.Type == 10)
				this.Value = new SRational(BitConverter.ToInt32(property.Value, 0), BitConverter.ToInt32(property.Value, 4));
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to SRATIONAL!", property.Type));
		}

		public override void ImportFromImageComponentMetadata(ImageComponent.Metadata.PropertyBase property)
		{
			if (property is ImageComponent.Metadata.SRationalProperty)
			{
				this.Value = new SRational(((ImageComponent.Metadata.SRationalProperty)property).Value.Numerator, 
					((ImageComponent.Metadata.SRationalProperty)property).Value.Denominator);
			}
			else
				throw new Exception(string.Format("Can't import from ImageComponent.Metadata.PropertyBase type '{0}' to SRATIONAL!", property.TagId));
		}

		public override string ToString()
		{
			return string.Format("SRATIONAL 0x{0:X} {1}: Value = {2}/{3}", (int)this.TagId, this.TagId, this.value.Numerator, this.value.Denominator);
		}
	}
	#endregion

	#region class UndefinedProperty
	//byte array property
	public class UndefinedProperty : PropertyBase
	{
		byte[] array = null;
		int lenght = -1;

		public UndefinedProperty(ExifTags tagId)
			: base(tagId)
		{
			this.array = new byte[0];
		}

		public UndefinedProperty(ExifTags tagId, int lenght)
			: base(tagId) 
		{
			this.array = new byte[lenght];
			this.lenght = lenght;
		}

		public byte[] Value
		{
			get { return this.array; }
			set
			{
				this.Defined = true;
				this.array = value;
			}
		}

		public override void ExportToPropertyItem(PropertyItem property)
		{
			property.Id = (int)this.TagId;
			property.Type = (short)7;
			property.Len = array.Length;
			property.Value = Value;
		}

		public override void ImportFromPropertyItem(PropertyItem property)
		{
			if (property.Type == 7)
				this.Value = property.Value;
			else if (property.Type == 2)
				this.Value = property.Value;
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to UNDEFINED!", property.Type));
		}

		public override void ImportFromImageComponentMetadata(ImageComponent.Metadata.PropertyBase property)
		{
			if (property is ImageComponent.Metadata.UndefinedProperty)
				this.Value = ((ImageComponent.Metadata.UndefinedProperty)property).Value;
			else if (property is ImageComponent.Metadata.AsciiProperty)
				this.Value = ((ImageComponent.Metadata.AsciiProperty)property).GetBytes();
			else
				throw new Exception(string.Format("Can't import from ImageComponent.Metadata.PropertyBase type '{0}' to UNDEFINED!", property.TagId));
		}

		public override string ToString()
		{
			return string.Format("UNDEFINED 0x{0:X} {1}: Value = {2}", (int)this.TagId, this.TagId, Encoding.ASCII.GetString(this.Value));
		}

		public void SetString(string str)
		{
			this.Value = Encoding.ASCII.GetBytes(str);
		}

	}
	#endregion

	#region class AsciiProperty
	public class AsciiProperty : PropertyBase
	{
		byte[] array = null;
		int lenght = -1;

		public AsciiProperty(ExifTags tagId) 
			: base(tagId) 
		{
			this.array = new byte[0];
		}

		public AsciiProperty(ExifTags tagId, int lenght)
			: base(tagId)
		{
			this.array = new byte[lenght];
			this.lenght = lenght;
		}

		public string Value
		{
			get { return Encoding.ASCII.GetString(this.array); }
			set
			{
				this.Defined = true;
				this.array = Encoding.ASCII.GetBytes(value);
			}
		}

		public override void ExportToPropertyItem(PropertyItem property)
		{
			property.Id = (int)this.TagId;
			property.Type = (short)2;
			property.Len = array.Length;
			property.Value = array;
		}

		public override void ImportFromPropertyItem(PropertyItem property)
		{
			if (property.Type == 2)
			{
				this.array = property.Value;
				this.Defined = true;
			}
			else
				throw new Exception(string.Format("Can't import from PropertyItem type '{0}' to ASCII!", property.Type));
		}

		public override void ImportFromImageComponentMetadata(ImageComponent.Metadata.PropertyBase property)
		{
			if (property is ImageComponent.Metadata.AsciiProperty)
				this.Value = ((ImageComponent.Metadata.AsciiProperty)property).Value;
			else
				throw new Exception(string.Format("Can't import from ImageComponent.Metadata.PropertyBase type '{0}' to ASCII!", property.TagId));
		}

		public override string ToString()
		{
			return string.Format("ASCII 0x{0:X} {1}: Value = {2}", (int)this.TagId, this.TagId, this.Value);
		}
	}
	#endregion

}
