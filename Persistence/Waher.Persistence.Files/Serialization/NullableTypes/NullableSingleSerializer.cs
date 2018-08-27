﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Waher.Persistence.Files.Serialization.NullableTypes
{
	/// <summary>
	/// Serializes a nullable <see cref="Single"/> value.
	/// </summary>
	public class NullableSingleSerializer : NullableValueTypeSerializer
	{
		/// <summary>
		/// Serializes a nullable <see cref="Single"/> value.
		/// </summary>
		public NullableSingleSerializer()
		{
		}

		/// <summary>
		/// What type of object is being serialized.
		/// </summary>
		public override Type ValueType
		{
			get
			{
				return typeof(float?);
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
				case ObjectSerializer.TYPE_BOOLEAN: return Reader.ReadBoolean() ? (float?)1 : (float?)0;
				case ObjectSerializer.TYPE_BYTE: return (float?)Reader.ReadByte();
				case ObjectSerializer.TYPE_INT16: return (float?)Reader.ReadInt16();
				case ObjectSerializer.TYPE_INT32: return (float?)Reader.ReadInt32();
				case ObjectSerializer.TYPE_INT64: return (float?)Reader.ReadInt64();
				case ObjectSerializer.TYPE_SBYTE: return (float?)Reader.ReadSByte();
				case ObjectSerializer.TYPE_UINT16: return (float?)Reader.ReadUInt16();
				case ObjectSerializer.TYPE_UINT32: return (float?)Reader.ReadUInt32();
				case ObjectSerializer.TYPE_UINT64: return (float?)Reader.ReadUInt64();
				case ObjectSerializer.TYPE_DECIMAL: return (float?)Reader.ReadDecimal();
				case ObjectSerializer.TYPE_DOUBLE: return (float?)Reader.ReadDouble();
				case ObjectSerializer.TYPE_SINGLE: return (float?)Reader.ReadSingle();
				case ObjectSerializer.TYPE_STRING: return (float?)float.Parse(Reader.ReadString());
				case ObjectSerializer.TYPE_MIN: return float.MinValue;
				case ObjectSerializer.TYPE_MAX: return float.MaxValue;
				case ObjectSerializer.TYPE_NULL: return null;
				default: throw new Exception("Expected a nullable Single value.");
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
			float? Value2 = (float?)Value;

			if (WriteTypeCode)
			{
				if (!Value2.HasValue)
				{
					Writer.WriteBits(ObjectSerializer.TYPE_NULL, 6);
					return;
				}
				else
					Writer.WriteBits(ObjectSerializer.TYPE_SINGLE, 6);
			}
			else if (!Value2.HasValue)
				throw new NullReferenceException("Value cannot be null.");

			Writer.Write(Value2.Value);
		}

	}
}
