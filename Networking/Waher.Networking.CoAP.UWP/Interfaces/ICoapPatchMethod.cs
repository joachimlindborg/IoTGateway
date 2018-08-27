﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Waher.Networking.CoAP
{
	/// <summary>
	/// PATCH Interface for CoAP resources.
	/// </summary>
	public interface ICoapPatchMethod
	{
		/// <summary>
		/// Executes the PATCH method on the resource.
		/// </summary>
		/// <param name="Request">CoAP Request</param>
		/// <param name="Response">CoAP Response</param>
		/// <exception cref="CoapException">If an error occurred when processing the method.</exception>
		void PATCH(CoapMessage Request, CoapResponse Response);

		/// <summary>
		/// If the PATCH method is allowed.
		/// </summary>
		bool AllowsPATCH
		{
			get;
		}
	}
}
