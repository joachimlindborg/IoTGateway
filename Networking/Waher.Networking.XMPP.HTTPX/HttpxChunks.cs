﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Runtime.Cache;

namespace Waher.Networking.XMPP.HTTPX
{
	internal static class HttpxChunks
	{
		private static Dictionary<XmppClient, int> registrationsPerClient = new Dictionary<XmppClient, int>();
		internal static Cache<string, ChunkRecord> chunkedStreams = null;

		internal static void RegisterChunkReceiver(XmppClient Client)
		{
			lock (registrationsPerClient)
			{
				if (registrationsPerClient.TryGetValue(Client, out int i))
					registrationsPerClient[Client] = i + 1;
				else
				{
					if (registrationsPerClient.Count == 0)
					{
						chunkedStreams = new Cache<string, ChunkRecord>(int.MaxValue, TimeSpan.MaxValue, new TimeSpan(0, 1, 0));
						chunkedStreams.Removed += CacheItem_Removed;
					}

					registrationsPerClient[Client] = 1;
					Client.RegisterMessageHandler("chunk", HttpxClient.Namespace, ChunkReceived, true);
				}
			}
		}

		private static void CacheItem_Removed(object Sender, CacheItemEventArgs<string, ChunkRecord> e)
		{
			e.Value.Dispose();
		}

		internal static void UnregisterChunkReceiver(XmppClient Client)
		{
			lock (registrationsPerClient)
			{
				if (registrationsPerClient.TryGetValue(Client, out int i))
				{
					if (i > 1)
						registrationsPerClient[Client] = i - 1;
					else
					{
						registrationsPerClient.Remove(Client);
						Client.UnregisterMessageHandler("chunk", HttpxClient.Namespace, ChunkReceived, true);

						if (registrationsPerClient.Count == 0)
						{
							chunkedStreams.Clear();
							chunkedStreams.Dispose();
							chunkedStreams = null;
						}
					}
				}
			}
		}

		internal static void ChunkReceived(object Sender, MessageEventArgs e)
		{
			string StreamId = XML.Attribute(e.Content, "streamId");
			string Key = e.From + " " + StreamId;

			if (!chunkedStreams.TryGetValue(Key, out ChunkRecord Rec))
				return;

			int Nr = XML.Attribute(e.Content, "nr", 0);
			if (Nr < 0)
				return;

			bool Last = XML.Attribute(e.Content, "last", false);
			byte[] Data = Convert.FromBase64String(e.Content.InnerText);

			if (!Rec.ChunkReceived(Nr, Last, Data))
			{
				Rec.Dispose();
				chunkedStreams.Remove(Key);
			}
		}
	}
}
