﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System.Windows;
using System.Windows.Input;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Concentrator;
using Waher.Things.SourceEvents;

namespace Waher.Client.WPF.Model.Concentrator
{
	/// <summary>
	/// Represents an XMPP concentrator.
	/// </summary>
	public class XmppConcentrator : XmppContact
	{
		private Dictionary<string, bool> capabilities = null;
		private readonly Dictionary<string, DataSource> dataSources = new Dictionary<string, DataSource>();
		private readonly bool suportsEvents;

		public XmppConcentrator(TreeNode Parent, XmppClient Client, string BareJid, bool SupportsEventSubscripton)
			: base(Parent, Client, BareJid)
		{
			this.suportsEvents = SupportsEventSubscripton;
			this.children = new SortedDictionary<string, TreeNode>()
			{
				{ string.Empty, new Loading(this) }
			};

			this.CheckCapabilities();
		}

		/// <summary>
		/// If event subscription is supported for readable nodes.
		/// </summary>
		public bool SupportsEvents => this.suportsEvents;

		private void CheckCapabilities()
		{
			if (this.capabilities == null)
			{
				string FullJid = this.FullJid;

				if (!string.IsNullOrEmpty(FullJid))
				{
					this.XmppAccountNode.ConcentratorClient.GetCapabilities(FullJid, (sender, e) =>
					{
						if (e.Ok)
						{
							Dictionary<string, bool> Capabilities = new Dictionary<string, bool>();

							foreach (string s in e.Capabilities)
								Capabilities[s] = true;

							this.capabilities = Capabilities;
						}
					}, null);
				}
			}
		}

		public override string TypeName
		{
			get { return "Concentrator"; }
		}

		public string FullJid
		{
			get
			{
				XmppAccountNode AccountNode = this.XmppAccountNode;
				if (AccountNode == null || !AccountNode.IsOnline)
					return null;

				RosterItem Item = AccountNode.Client[this.BareJID];
				PresenceEventArgs e = Item?.LastPresence;

				if (e == null || e.Availability == Availability.Offline)
					return null;
				else
					return e.From;
			}
		}

		private bool loadingChildren = false;

		protected override void LoadChildren()
		{
			if (!this.loadingChildren && !this.IsLoaded)
			{
				string FullJid = this.FullJid;

				if (!string.IsNullOrEmpty(FullJid))
				{
					Mouse.OverrideCursor = Cursors.Wait;

					ConcentratorClient ConcentratorClient = this.XmppAccountNode.ConcentratorClient;

					this.loadingChildren = true;
					ConcentratorClient.GetRootDataSources(FullJid, (sender, e) =>
					{
						this.loadingChildren = false;
						MainWindow.MouseDefault();

						if (e.Ok)
						{
							SortedDictionary<string, TreeNode> Children = new SortedDictionary<string, TreeNode>();

							foreach (DataSourceReference Ref in e.DataSources)
							{
								DataSource DataSource = new DataSource(this, Ref.SourceID, Ref.SourceID, Ref.HasChildren);
								Children[Ref.SourceID] = DataSource;

								DataSource.SubscribeToEvents();
							}

							this.children = Children;

							this.OnUpdated();
							this.NodesAdded(Children.Values, this);
						}
					}, null);
				}
			}

			base.LoadChildren();
		}

		public void NodesAdded(IEnumerable<TreeNode> Nodes, TreeNode Parent)
		{
			Controls.ConnectionView View = this.XmppAccountNode?.View;

			foreach (TreeNode Node in Nodes)
			{
				View?.NodeAdded(Parent, Node);

				if (Node is DataSource DataSource)
				{
					lock (this.dataSources)
					{
						this.dataSources[DataSource.Key] = DataSource;
					}
				}
			}
		}

		public void NodesRemoved(IEnumerable<TreeNode> Nodes, TreeNode Parent)
		{
			Controls.ConnectionView View = this.XmppAccountNode?.View;
			LinkedList<KeyValuePair<TreeNode, TreeNode>> ToRemove = new LinkedList<KeyValuePair<TreeNode, TreeNode>>();

			foreach (TreeNode Node in Nodes)
				ToRemove.AddLast(new KeyValuePair<TreeNode, TreeNode>(Parent, Node));

			while (ToRemove.First != null)
			{
				KeyValuePair<TreeNode, TreeNode> P = ToRemove.First.Value;
				ToRemove.RemoveFirst();

				Parent = P.Key;
				TreeNode Node = P.Value;

				if (Node.HasChildren.HasValue && Node.HasChildren.Value)
				{
					foreach (TreeNode Child in Node.Children)
						ToRemove.AddLast(new KeyValuePair<TreeNode, TreeNode>(Node, Child));
				}

				View?.NodeRemoved(Parent, Node);

				if (Node is DataSource DataSource)
				{
					if (XmppAccountNode.IsOnline)
						DataSource.UnsubscribeFromEvents();

					lock (this.dataSources)
					{
						this.dataSources.Remove(DataSource.Key);
					}
				}
			}
		}

		protected override void UnloadChildren()
		{
			base.UnloadChildren();

			if (!this.IsLoaded)
			{
				if (this.children != null)
					this.NodesRemoved(this.children.Values, this);

				this.children = new SortedDictionary<string, TreeNode>()
				{
					{ string.Empty, new Loading(this) }
				};

				this.OnUpdated();
			}
		}

		internal void ConcentratorClient_OnEvent(object Sender, SourceEventMessageEventArgs EventMessage)
		{
			DataSource Source;

			lock(this.dataSources)
			{
				if (!this.dataSources.TryGetValue(EventMessage.Event.SourceId, out Source))
					return;
			}

			Source.ConcentratorClient_OnEvent(Sender, EventMessage);
		}

	}
}
