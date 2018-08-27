﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Waher.Persistence.Files.Serialization.NullableTypes
{
	/// <summary>
	/// Serializes a nullable <see cref="UInt32"/> value.
	/// </summary>
	public class NullableUInt32Serializer : NullableValueTypeSerializer
	{
		/// <summary>
		/// Serializes a nullable <see cref="UInt32"/> value.
		/// </summary>
		public NullableUInt32Serializer()
		{
		}

		/// <summary>
		/// What type of object is being serialized.
		/// </summary>
		public override Type ValueType
		{
			get
			{
				return typeof(uint?);
			}
		}

		/// <summary>
		/// Deserializes an object from a binary source.
		/// </summary>
		/// <param name="Reader">Binary deserializer.</param>
		/// <param name="DataType">Optional datatype. If not provided, will be read from the binary source.</param>
		/// <param name="Embedded">If the object is embedded into another.</param>
		/// <returns>Deserialized object.</returns>
		public override object Deserialize(BinaryDeserializer Reader, uint? DataType, bool Embedded)
		{
			if (!DataType.HasValue)
				DataType = Reader.ReadBits(6);

			switch (DataType.Value)
			{
				case ObjectSerializer.TYPE_BOOLEAN: return Reader.ReadBoolean() ? (uint?)1 : (uint?)0;
				case ObjectSerializer.TYPE_BYTE: return (uint?)Reader.ReadByte();
				case ObjectSerializer.TYPE_INT16: return (uint?)Reader.ReadInt16();
				case ObjectSerializer.TYPE_INT32: return (uint?)Reader.ReadInt32();
				case ObjectSerializer.TYPE_INT64: return (uint?)Reader.ReadInt64();
				case ObjectSerializer.TYPE_SBYTE: return (uint?)Reader.ReadSByte();
				case ObjectSerializer.TYPE_UINT16: return (uint?)Reader.ReadUInt16();
				case ObjectSerializer.TYPE_UINT32: return (uint?)Reader.ReadUInt32();
				case ObjectSerializer.TYPE_UINT64: return (uint?)Reader.ReadUInt64();
				case ObjectSerializer.TYPE_DECIMAL: return (uint?)Reader.ReadDecimal();
				case ObjectSerializer.TYPE_DOUBLE: return (uint?)Reader.ReadDouble();
				case ObjectSerializer.TYPE_SINGLE: return (uint?)Reader.ReadSingle();
				case ObjectSerializer.TYPE_STRING: return (uint?)uint.Parse(Reader.ReadString());
				case ObjectSerializer.TYPE_MIN: return uint.MinValue;
				case ObjectSerializer.TYPE_MAX: return uint.MaxValue;
				case ObjectSerializer.TYPE_NULL: return null;
				default: throw new Exception("Expected a nullable UInt32 value.");
			}
		}

		/// <summary>
		/// Serializes an object to a binary destination.
		/// </summary>
		/// <param name="Writer">Binary destination.</param>
		/// <param name="WriteTypeCode">If a type code is to be output.</param>
		/// <param name="Embedded">If the object is embedded into another.</param>
		/// <param name="Value">The actual object to serialize.</param>
		public override void Serialize(BinarySerializer Writer, bool WriteTypeCode, bool Embedded, object Value)
		{
			uint? Value2 = (uint?)Value;

			if (WriteTypeCode)
			{
				if (!Value2.HasValue)
				{
					Writer.WriteBits(ObjectSerializer.TYPE_NULL, 6);
					return;
				}
				else
					Writer.WriteBits(ObjectSerializer.TYPE_UINT32, 6);
			}
			else if (!Value2.HasValue)
				throw new NullReferenceException("Value cannot be null.");

			Writer.Write(Value2.Value);
		}

	}
}
