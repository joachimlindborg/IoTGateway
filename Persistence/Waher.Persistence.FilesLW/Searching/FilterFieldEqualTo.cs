﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using F = Waher.Persistence.Filters;
using Waher.Persistence.Files.Serialization;

namespace Waher.Persistence.Files.Searching
{
	/// <summary>
	/// This filter selects objects that have a named field equal to a given value.
	/// </summary>
	public class FilterFieldEqualTo : F.FilterFieldEqualTo, IApplicableFilter
	{
		private readonly string[] constantFields;

		/// <summary>
		/// This filter selects objects that have a named field equal to a given value.
		/// </summary>
		/// <param name="FieldName">Field Name.</param>
		/// <param name="Value">Value.</param>
		public FilterFieldEqualTo(string FieldName, object Value)
			: base(FieldName, Value)
		{
			this.constantFields = new string[] { FieldName };
		}

		/// <summary>
		/// Checks if the filter applies to the object.
		/// </summary>
		/// <param name="Object">Object.</param>
		/// <param name="Serializer">Corresponding object serializer.</param>
		/// <param name="Provider">Files provider.</param>
		/// <returns>If the filter can be applied.</returns>
		public bool AppliesTo(object Object, IObjectSerializer Serializer, FilesProvider Provider)
		{
			if (!Serializer.TryGetFieldValue(this.FieldName, Object, out object Value))
			{
				Type T = Object.GetType();
				if (Serializer.ValueType == T)
					return false;

				if (T == this.prevType)
					Serializer = this.prevSerializer;
				else
				{
					Serializer = this.prevSerializer = Provider.GetObjectSerializer(T);
					this.prevType = T;
				}

				if (!Serializer.TryGetFieldValue(this.FieldName, Object, out Value))
					return false;
			}

			int? ComparisonResult = Comparison.Compare(Value, this.Value);

			return ComparisonResult.HasValue && ComparisonResult.Value == 0;
		}

		private Type prevType = null;
		private IObjectSerializer prevSerializer = null;

		/// <summary>
		/// Gets an array of constant fields. Can return null, if there are no constant fields.
		/// </summary>
		public string[] ConstantFields => this.constantFields;
	}
}
