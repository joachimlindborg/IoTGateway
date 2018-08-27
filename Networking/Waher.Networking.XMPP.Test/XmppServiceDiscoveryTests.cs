﻿using System;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Networking.Sniffers;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.ServiceDiscovery;

namespace Waher.Networking.XMPP.Test
{
	[TestClass]
	public class XmppServiceDiscoveryTests : CommunicationTests
	{
		[TestMethod]
		public void ServiceDiscovery_Test_01_Server()
		{
			this.ConnectClients();
			try
			{
				ServiceDiscoveryEventArgs e = this.client1.ServiceDiscovery(this.client1.Domain, 10000);
				this.Print(e);
			}
			finally
			{
				this.DisposeClients();
			}
		}

		private void Print(ServiceDiscoveryEventArgs e)
		{
			Console.Out.WriteLine();
			Console.Out.WriteLine("Identities:");

			foreach (Identity Identity in e.Identities)
				Console.Out.WriteLine(Identity.ToString());

			Console.Out.WriteLine();
			Console.Out.WriteLine("Features:");

			foreach (string Feature in e.Features.Keys)
				Console.Out.WriteLine(Feature);
		}

		[TestMethod]
		public void ServiceDiscovery_Test_02_Account()
		{
			this.ConnectClients();
			try
			{
				ServiceDiscoveryEventArgs e = this.client1.ServiceDiscovery(this.client2.BareJID, 10000);
				this.Print(e);
			}
			finally
			{
				this.DisposeClients();
			}
		}

		[TestMethod]
		public void ServiceDiscovery_Test_03_Client()
		{
			this.ConnectClients();
			try
			{
				ServiceDiscoveryEventArgs e = this.client1.ServiceDiscovery(this.client2.FullJID, 10000);
				this.Print(e);
			}
			finally
			{
				this.DisposeClients();
			}
		}

		[TestMethod]
		public void ServiceDiscovery_Test_04_ServerItems()
		{
			this.ConnectClients();
			try
			{
				ServiceItemsDiscoveryEventArgs e = this.client1.ServiceItemsDiscovery(this.client1.Domain, 10000);
				this.Print(e);
			}
			finally
			{
				this.DisposeClients();
			}
		}

		private void Print(ServiceItemsDiscoveryEventArgs e)
		{
			Console.Out.WriteLine();
			Console.Out.WriteLine("Items:");

			foreach (Item Item in e.Items)
				Console.Out.WriteLine(Item.ToString());
		}

		[TestMethod]
		public void ServiceDiscovery_Test_05_ServerItemFeatures()
		{
			this.ConnectClients();
			try
			{
				ServiceItemsDiscoveryEventArgs e = this.client1.ServiceItemsDiscovery(this.client1.Domain, 10000);

				foreach (Item Item in e.Items)
				{
					ServiceDiscoveryEventArgs e2 = this.client1.ServiceDiscovery(Item.JID, 10000);

					Console.Out.WriteLine();
					Console.Out.WriteLine(Item.ToString());
					Console.Out.WriteLine(new string('=', 80));

					this.Print(e2);
				}
			}
			finally
			{
				this.DisposeClients();
			}
		}

		[TestMethod]
		public void ServiceDiscovery_Test_06_AccountItems()
		{
			this.ConnectClients();
			try
			{
				ServiceItemsDiscoveryEventArgs e = this.client1.ServiceItemsDiscovery(this.client2.BareJID, 10000);
				this.Print(e);
			}
			finally
			{
				this.DisposeClients();
			}
		}

	}
}
