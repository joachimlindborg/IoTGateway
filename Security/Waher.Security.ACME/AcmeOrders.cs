﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using Waher.Content;

namespace Waher.Security.ACME
{
	/// <summary>
	/// Represents a set of ACME orders.
	/// </summary>
	public class AcmeOrders : AcmeObject
	{
		private readonly Uri[] orders = null;
		private readonly Uri next = null;

		internal AcmeOrders(AcmeClient Client, HttpResponseMessage Response, IEnumerable<KeyValuePair<string, object>> Obj)
			: base(Client)
		{
			this.next = AcmeClient.GetLink(Response, "next");

			foreach (KeyValuePair<string, object> P in Obj)
			{
				switch (P.Key)
				{
					case "orders":
						if (P.Value is Array A)
						{
							List<Uri> Orders = new List<Uri>();

							foreach (object Obj2 in A)
							{
								if (Obj2 is string s)
									Orders.Add(new Uri(s));
							}

							this.orders = Orders.ToArray();
						}
						break;
				}
			}
		}

		/// <summary>
		/// An array of URLs, each identifying an order belonging to the account.
		/// </summary>
		public Uri[] Orders => this.orders;

		/// <summary>
		/// If provided, indicates where further entries can be acquired.
		/// </summary>
		public Uri Next => this.next;
	}
}
