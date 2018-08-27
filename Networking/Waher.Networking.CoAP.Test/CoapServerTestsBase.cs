﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Content;
using Waher.Networking.Sniffers;
using Waher.Networking.CoAP.ContentFormats;
using Waher.Networking.CoAP.CoRE;
using Waher.Networking.CoAP.Options;
using Waher.Runtime.Inventory;
using Waher.Security.DTLS;

namespace Waher.Networking.CoAP.Test
{
	public abstract class CoapServerTestsBase
	{
		protected CoapEndpoint server;
		protected CoapEndpoint client;
		protected IDtlsCredentials clientCredentials;

		protected const string ResponseTest = "Hello world.";
		protected const string ResponseRoot =
			"************************************************************\r\n" +
			"CoAP RFC 7252\r\n" +
			"************************************************************\r\n" +
			"This server is using the Waher.Networking.CoAP framework\r\n" +
			"published under the following license:\r\n" +
			"https://github.com/PeterWaher/IoTGateway#license\r\n" +
			"\r\n" +
			"(c) 2017 Waher Data AB\r\n" +
			"************************************************************";
		protected const string ResponseLarge =
			"/-------------------------------------------------------------\\\r\n" +
			"|                 RESOURCE BLOCK NO. 1 OF 5                   |\r\n" +
			"|               [each line contains 64 bytes]                 |\r\n" +
			"\\-------------------------------------------------------------/\r\n" +
			"/-------------------------------------------------------------\\\r\n" +
			"|                 RESOURCE BLOCK NO. 2 OF 5                   |\r\n" +
			"|               [each line contains 64 bytes]                 |\r\n" +
			"\\-------------------------------------------------------------/\r\n" +
			"/-------------------------------------------------------------\\\r\n" +
			"|                 RESOURCE BLOCK NO. 3 OF 5                   |\r\n" +
			"|               [each line contains 64 bytes]                 |\r\n" +
			"\\-------------------------------------------------------------/\r\n" +
			"/-------------------------------------------------------------\\\r\n" +
			"|                 RESOURCE BLOCK NO. 4 OF 5                   |\r\n" +
			"|               [each line contains 64 bytes]                 |\r\n" +
			"\\-------------------------------------------------------------/\r\n" +
			"/-------------------------------------------------------------\\\r\n" +
			"|                 RESOURCE BLOCK NO. 5 OF 5                   |\r\n" +
			"|               [each line contains 64 bytes]                 |\r\n" +
			"\\-------------------------------------------------------------/";
		protected const string ResponseHierarchical =
			"</path/sub2>;title=\"Hierarchical link description sub-resource\"," +
			"</path/sub3>;title=\"Hierarchical link description sub-resource\"," +
			"</path/sub1>;title=\"Hierarchical link description sub-resource\"";

		protected virtual void SetupClientServer()
		{
			this.server = new CoapEndpoint(new int[] { CoapEndpoint.DefaultCoapPort }, null, null, null, false, true, new TextWriterSniffer(Console.Out, BinaryPresentationMethod.Hexadecimal));
			this.client = new CoapEndpoint(new int[] { CoapEndpoint.DefaultCoapPort + 2 }, null, null, null, true, false);
			this.clientCredentials = null;
		}

		[TestInitialize]
		public void TestInitialize()
		{
			this.SetupClientServer();

			this.server.Register("/test", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, ResponseTest, 64);
			}, Notifications.None, "Default test resource");

			this.server.Register("/", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, ResponseRoot, 64);
			});

			this.server.Register("/separate", (req, resp) =>
			{
				Task.Run(() =>
				{
					Thread.Sleep(2000);
					resp.Respond(CoapCode.Content, ResponseTest, 64);
				});
			}, Notifications.None, "Resource which cannot be served immediately and which cannot be acknowledged in a piggy-backed way.");

			this.server.Register("/seg1", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, ResponseTest, 64);
			}, Notifications.None, "Long path resource");

			this.server.Register("/seg1/seg2", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, ResponseTest, 64);
			}, Notifications.None, "Long path resource");

			this.server.Register("/seg1/seg2/seg3", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, ResponseTest, 64);
			}, Notifications.None, "Long path resource");

			this.server.Register("/large", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, ResponseLarge, 64);
			}, Notifications.None, "Large resource", new string[] { "block" }, null, null, 1280);

			this.server.Register("/large-separate", (req, resp) =>
			{
				Task.Run(() =>
				{
					Thread.Sleep(2000);
					resp.Respond(CoapCode.Content, ResponseLarge, 64);
				});
			}, Notifications.None, "Large resource", new string[] { "block" }, null, null, 1280);

			this.server.Register("/multi-format", (req, resp) =>
			{
				if (req.IsAcceptable(PlainText.ContentFormatCode))
					resp.Respond(CoapCode.Content, ResponseTest, 64);
				else if (req.IsAcceptable(Xml.ContentFormatCode))
				{
					resp.Respond(CoapCode.Content, "<text>" + ResponseTest + "</text>", 64,
						new CoapOptionContentFormat(Xml.ContentFormatCode));
				}
				else
					throw new CoapException(CoapCode.NotAcceptable);

			}, Notifications.None, "Resource that exists in different content formats (text/plain utf8 and application/xml)",
				null, null, new int[] { PlainText.ContentFormatCode, Xml.ContentFormatCode });

			this.server.Register("/path", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, ResponseHierarchical, 64,
					new CoapOptionContentFormat(CoreLinkFormat.ContentFormatCode));
			}, Notifications.None, "Hierarchical link description entry", null, null,
				new int[] { CoreLinkFormat.ContentFormatCode });

			this.server.Register("/path/sub1", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, "/path/sub1", 64);
			}, Notifications.None, "Hierarchical link description sub-resource");

			this.server.Register("/path/sub2", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, "/path/sub2", 64);
			}, Notifications.None, "Hierarchical link description sub-resource");

			this.server.Register("/path/sub3", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, "/path/sub3", 64);
			}, Notifications.None, "Hierarchical link description sub-resource");

			this.server.Register("/query", (req, resp) =>
			{
				StringBuilder sb = new StringBuilder();
				bool First = true;

				foreach (CoapOption Option in req.Options)
				{
					if (Option is CoapOptionUriQuery Query)
					{
						if (First)
						{
							First = false;
							sb.Append('?');
						}
						else
							sb.Append('&');

						sb.Append(Query.Key);
						sb.Append('=');
						sb.Append(Query.KeyValue);
					}
				}

				resp.Respond(CoapCode.Content, sb.ToString(), 64);

			}, Notifications.None, "Resource accepting query parameters");

			CoapResource Obs = this.server.Register("/obs", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, DateTime.Now.ToString("T"), 64);
			}, Notifications.Acknowledged, "Observable resource which changes every 5 seconds",
				new string[] { "observe" });

			Obs.TriggerAll(new TimeSpan(0, 0, 5));

			Obs = this.server.Register("/obs-large", (req, resp) =>
			{
				string s = DateTime.Now.ToString("T");
				int i = 61 - s.Length;
				s = '|' + new string(' ', i / 2) + s + new string(' ', i - (i / 2)) + "|\r\n";
				string Response =
					"/-------------------------------------------------------------\\\r\n" + s +
					"\\-------------------------------------------------------------/\r\n";
				resp.Respond(CoapCode.Content, Response, 64);

			}, Notifications.Acknowledged, "Observable resource which changes every 5 seconds",
				new string[] { "observe" });

			Obs.TriggerAll(new TimeSpan(0, 0, 5));

			Obs = this.server.Register("/obs-non", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, DateTime.Now.ToString("T"), 64);
			}, Notifications.Unacknowledged, "Observable resource which changes every 5 seconds",
				new string[] { "observe" });

			Obs.TriggerAll(new TimeSpan(0, 0, 5));

			Obs = this.server.Register("/obs-pumping", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, DateTime.Now.ToString("T"), 64);
			}, Notifications.Acknowledged, "Observable resource which changes every 5 seconds",
				new string[] { "observe" });

			Obs.TriggerAll(new TimeSpan(0, 0, 5));

			Obs = this.server.Register("/obs-pumping-non", (req, resp) =>
			{
				resp.Respond(CoapCode.Content, DateTime.Now.ToString("T"), 64);
			}, Notifications.Unacknowledged, "Observable resource which changes every 5 seconds",
				new string[] { "observe" });

			Obs.TriggerAll(new TimeSpan(0, 0, 5));

			this.server.Register("/location-query", null, (req, resp) =>
			{
				resp.Respond(CoapCode.Content, req.Payload, 64);
			}, Notifications.None, "Perform POST transaction with responses containing several Location-Query options (CON mode)");

			this.server.Register("/large-create", null, (req, resp) =>
			{
				resp.Respond(CoapCode.Created, req.Payload, 64);
			}, Notifications.None, "Large resource that can be created using POST method",
				new string[] { "block" }, null, new int[] { 0 }, 2000);

			this.server.Register("/large-post", null, (req, resp) =>
			{
				resp.Respond(CoapCode.Content, req.Payload, 64);
			}, Notifications.None, "Handle POST with two-way blockwise transfer",
				new string[] { "block" });

			this.server.Register(new LargeUpdate());
		}

		private class LargeUpdate : CoapResource, ICoapPutMethod
		{
			public LargeUpdate()
				: base("/large-update")
			{
			}

			public bool AllowsPUT => true;

			public void PUT(CoapMessage Request, CoapResponse Response)
			{
				Response.Respond(CoapCode.Changed, Request.Payload, 64);
			}

			public override Notifications Notifications => Notifications.None;
			public override string Title => "Large resource that can be updated using PUT method";
			public override string[] ResourceTypes => new string[] { "block" };
			public override int[] ContentFormats => new int[] { 0 };
			public override int? MaximumSizeEstimate => 2000;
		}

		[TestCleanup]
		public void TestCleanup()
		{
			try
			{
				this.Cleanup(ref this.client);
			}
			finally
			{
				this.Cleanup(ref this.server);
			}
		}

		private void Cleanup(ref CoapEndpoint Client)
		{
			if (Client != null)
			{
				ulong[] Tokens = Client.GetActiveTokens();
				ushort[] MessageIDs = Client.GetActiveMessageIDs();

				Client.Dispose();
				Client = null;

				Assert.AreEqual(0, Tokens.Length, "There are tokens that have not been unregistered properly.");
				Assert.AreEqual(0, MessageIDs.Length, "There are message IDs that have not been unregistered properly.");
			}
		}

		protected async Task<object> Get(string Uri, params CoapOption[] Options)
		{
			ManualResetEvent Done = new ManualResetEvent(false);
			ManualResetEvent Error = new ManualResetEvent(false);
			object Result = null;

			await this.client.GET(Uri, true, this.clientCredentials, (sender, e) =>
			{
				if (e.Ok)
				{
					Result = e.Message.Decode();
					Done.Set();
				}
				else
					Error.Set();
			}, null, Options);

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 30000));
			Assert.IsNotNull(Result);

			Console.Out.WriteLine(Result.ToString());

			return Result;
		}

		protected async Task<object> Observe(string Uri, params CoapOption[] Options)
		{
			ManualResetEvent Done = new ManualResetEvent(false);
			ManualResetEvent Error = new ManualResetEvent(false);
			object Result = null;
			ulong Token = 0;
			int Count = 0;

			await this.client.Observe(Uri, true, this.clientCredentials, (sender, e) =>
			{
				if (e.Ok)
				{
					Token = e.Message.Token;
					Result = e.Message.Decode();
					Console.Out.WriteLine(Result.ToString());

					Count++;
					if (Count == 3)
						Done.Set();
				}
				else
					Error.Set();
			}, null, Options);

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 30000));
			Assert.IsNotNull(Result);

			Done.Reset();

			await this.client.UnregisterObservation(Uri, true, Token, this.clientCredentials, (sender, e) =>
			{
				if (e.Ok)
					Done.Set();
				else
					Error.Set();

			}, null, Options);

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 5000));

			return Result;
		}

		protected async Task Post(string Uri, byte[] Payload, int BlockSize, params CoapOption[] Options)
		{
			ManualResetEvent Done = new ManualResetEvent(false);
			ManualResetEvent Error = new ManualResetEvent(false);

			await this.client.POST(Uri, true, Payload, BlockSize, this.clientCredentials, (sender, e) =>
			{
				if (e.Ok)
				{
					object Result = e.Message.Decode();
					if (Result != null)
						Console.Out.WriteLine(Result.ToString());

					Done.Set();
				}
				else
					Error.Set();
			}, null, Options);

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 30000));
		}

		protected async Task Put(string Uri, byte[] Payload, int BlockSize, params CoapOption[] Options)
		{
			ManualResetEvent Done = new ManualResetEvent(false);
			ManualResetEvent Error = new ManualResetEvent(false);

			await this.client.PUT(Uri, true, Payload, BlockSize, this.clientCredentials, (sender, e) =>
			{
				if (e.Ok)
				{
					object Result = e.Message.Decode();
					if (Result != null)
						Console.Out.WriteLine(Result.ToString());

					Done.Set();
				}
				else
					Error.Set();
			}, null, Options);

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 30000));
		}

		protected async Task Delete(string Uri, params CoapOption[] Options)
		{
			ManualResetEvent Done = new ManualResetEvent(false);
			ManualResetEvent Error = new ManualResetEvent(false);

			await this.client.DELETE(Uri, true, this.clientCredentials, (sender, e) =>
			{
				if (e.Ok)
				{
					object Result = e.Message.Decode();
					if (Result != null)
						Console.Out.WriteLine(Result.ToString());

					Done.Set();
				}
				else
					Error.Set();
			}, null, Options);

			Assert.AreEqual(0, WaitHandle.WaitAny(new WaitHandle[] { Done, Error }, 30000));
		}
	}
}
