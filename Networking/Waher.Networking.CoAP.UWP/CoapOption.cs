﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Waher.Networking.CoAP
{
	/// <summary>
	/// Base class for all CoAP options.
	/// </summary>
	public abstract class CoapOption : ICoapOption
	{
		internal int originalOrder = 0;

		/// <summary>
		/// Base class for all CoAP options.
		/// </summary>
		public CoapOption()
		{
		}

		/// <summary>
		/// Option number.
		/// </summary>
		public abstract int OptionNumber
		{
			get;
		}

		/// <summary>
		/// If the option is critical or not. Messages containing critical options that are not processed, must be discarded.
		/// </summary>
		public abstract bool Critical
		{
			get;
		}

		/// <summary>
		/// Gets the option value.
		/// </summary>
		/// <returns>Binary value. Can be null, if option does not have a value.</returns>
		public abstract byte[] GetValue();

		/// <summary>
		/// Creates a new CoAP option.
		/// </summary>
		/// <param name="Value">Option value.</param>
		/// <returns>Newly created CoAP option.</returns>
		public abstract CoapOption Create(byte[] Value);

		/// <summary>
		/// Transforms an unsigned integer to a variable sized option value.
		/// </summary>
		/// <param name="Value">Value.</param>
		/// <returns>Binary variable length option value.</returns>
		public static byte[] ToBinary(ulong Value)
		{
			ulong Temp = Value;
			int NrBytes = 0;

			while (Temp > 0)
			{
				NrBytes++;
				Temp >>= 8;
			}

			byte[] Result = new byte[NrBytes];

			while (NrBytes > 0)
			{
				NrBytes--;
				Result[NrBytes] = (byte)Value;
				Value >>= 8;
			}

			return Result;
		}

		/// <summary>
		/// Transforms a string to a variable sized option value.
		/// </summary>
		/// <param name="Value">Value.</param>
		/// <returns>Binary variable length option value.</returns>
		public static byte[] ToBinary(string Value)
		{
			return Encoding.UTF8.GetBytes(Value);
		}

		/// <summary>
		/// <see cref="object.ToString()"/>
		/// </summary>
		public override string ToString()
		{
			return this.OptionNumber.ToString() + ". " + this.GetType().Name;
		}

	}
}
