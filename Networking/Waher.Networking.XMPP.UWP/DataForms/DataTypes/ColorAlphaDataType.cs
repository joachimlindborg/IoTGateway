﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Waher.Content;

namespace Waher.Networking.XMPP.DataForms.DataTypes
{
	/// <summary>
	/// ColorAlpha Data Type (xdc:colorAlpha)
	/// </summary>
	public class ColorAlphaDataType : DataType
	{
		/// <summary>
		/// Public instance of data type.
		/// </summary>
		public static readonly ColorAlphaDataType Instance = new ColorAlphaDataType();

		/// <summary>
		/// ColorAlpha Data Type (xdc:colorAlpha)
		/// </summary>
		public ColorAlphaDataType()
			: this("xdc:colorAlpha")
		{
		}

		/// <summary>
		/// ColorAlpha Data Type (xdc:colorAlpha)
		/// </summary>
		/// <param name="DataType">Data Type</param>
		public ColorAlphaDataType(string DataType)
			: base(DataType)
		{
		}

		/// <summary>
		/// Parses a string.
		/// </summary>
		/// <param name="Value">String value.</param>
		/// <returns>Parsed value, if possible, null otherwise.</returns>
		public override object Parse(string Value)
		{
			if (Value.Length != 8)
				return null;

			if (!byte.TryParse(Value.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte R))
				return null;

			if (!byte.TryParse(Value.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte G))
				return null;

			if (!byte.TryParse(Value.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte B))
				return null;

			if (!byte.TryParse(Value.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte A))
				return null;

			return new ColorReference(R, G, B, A);
		}
	}
}
