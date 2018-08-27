﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage.Streams;
#else
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
#endif
using Waher.Content;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Networking.XMPP.Provisioning.Cache;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Things;
using Waher.Things.SensorData;

namespace Waher.Networking.XMPP.Provisioning
{
	/// <summary>
	/// Implements an XMPP provisioning client interface.
	/// 
	/// The interface is defined in the IEEE XMPP IoT extensions:
	/// https://gitlab.com/IEEE-SA/XMPPI/IoT
	/// </summary>
	public class ProvisioningClient : XmppExtension
	{
		private readonly Dictionary<string, CertificateUse> certificates = new Dictionary<string, CertificateUse>();
		private readonly string provisioningServerAddress;
		private string ownerJid = string.Empty;
		private DateTime lastCheck = DateTime.MinValue;
		private Duration cacheUnusedLifetime = new Duration(false, 0, 13, 0, 0, 0, 0);
		private bool managePresenceSubscriptionRequests = true;

		/// <summary>
		/// urn:ieee:iot:prov:t:1.0
		/// </summary>
		public const string NamespaceProvisioningToken = "urn:ieee:iot:prov:t:1.0";

		/// <summary>
		/// urn:ieee:iot:prov:d:1.0
		/// </summary>
		public const string NamespaceProvisioningDevice = "urn:ieee:iot:prov:d:1.0";

		/// <summary>
		/// urn:ieee:iot:prov:o:1.0
		/// </summary>
		public const string NamespaceProvisioningOwner = "urn:ieee:iot:prov:o:1.0";

		/// <summary>
		/// Implements an XMPP provisioning client interface.
		/// 
		/// The interface is defined in the IEEE XMPP IoT extensions:
		/// https://gitlab.com/IEEE-SA/XMPPI/IoT
		/// </summary>
		/// <param name="Client">XMPP Client</param>
		/// <param name="ProvisioningServerAddress">Provisioning Server XMPP Address.</param>
		public ProvisioningClient(XmppClient Client, string ProvisioningServerAddress)
			: this(Client, ProvisioningServerAddress, string.Empty)
		{
		}

		/// <summary>
		/// Implements an XMPP provisioning client interface.
		/// 
		/// The interface is defined in the IEEE XMPP IoT extensions:
		/// https://gitlab.com/IEEE-SA/XMPPI/IoT
		/// </summary>
		/// <param name="Client">XMPP Client</param>
		/// <param name="ProvisioningServerAddress">Provisioning Server XMPP Address.</param>
		/// <param name="OwnerJid">JID of owner, if known.</param>
		public ProvisioningClient(XmppClient Client, string ProvisioningServerAddress, string OwnerJid)
			: base(Client)
		{
			this.provisioningServerAddress = ProvisioningServerAddress;
			this.ownerJid = OwnerJid;

			this.client.RegisterIqGetHandler("tokenChallenge", NamespaceProvisioningToken, this.TokenChallengeHandler, true);

			this.client.RegisterIqSetHandler("clearCache", NamespaceProvisioningDevice, this.ClearCacheHandler, true);
			this.client.RegisterMessageHandler("unfriend", NamespaceProvisioningDevice, this.UnfriendHandler, false);
			this.client.RegisterMessageHandler("friend", NamespaceProvisioningDevice, this.FriendHandler, false);

			this.client.RegisterMessageHandler("isFriend", NamespaceProvisioningOwner, this.IsFriendHandler, true);
			this.client.RegisterMessageHandler("canRead", NamespaceProvisioningOwner, this.CanReadHandler, true);
			this.client.RegisterMessageHandler("canControl", NamespaceProvisioningOwner, this.CanControlHandler, true);

			this.client.OnPresenceSubscribe += Client_OnPresenceSubscribe;
			this.client.OnPresenceUnsubscribe += Client_OnPresenceUnsubscribe;
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();

			this.client.UnregisterIqGetHandler("tokenChallenge", NamespaceProvisioningToken, this.TokenChallengeHandler, true);

			this.client.UnregisterIqSetHandler("clearCache", NamespaceProvisioningDevice, this.ClearCacheHandler, true);
			this.client.UnregisterMessageHandler("unfriend", NamespaceProvisioningDevice, this.UnfriendHandler, false);
			this.client.UnregisterMessageHandler("friend", NamespaceProvisioningDevice, this.FriendHandler, false);

			this.client.UnregisterMessageHandler("isFriend", NamespaceProvisioningOwner, this.IsFriendHandler, true);
			this.client.UnregisterMessageHandler("canRead", NamespaceProvisioningOwner, this.CanReadHandler, true);
			this.client.UnregisterMessageHandler("canControl", NamespaceProvisioningOwner, this.CanControlHandler, true);

			this.client.OnPresenceSubscribe -= Client_OnPresenceSubscribe;
			this.client.OnPresenceUnsubscribe -= Client_OnPresenceUnsubscribe;
		}

		/// <summary>
		/// Implemented extensions.
		/// </summary>
		public override string[] Extensions => new string[] { "XEP-0324" };

		/// <summary>
		/// Provisioning server XMPP address.
		/// </summary>
		public string ProvisioningServerAddress
		{
			get { return this.provisioningServerAddress; }
		}

		/// <summary>
		/// JID of owner, if known or available.
		/// </summary>
		public string OwnerJid
		{
			get { return this.ownerJid; }
			internal set { this.ownerJid = value; }
		}

		/// <summary>
		/// If presence subscription requests should be managed by the client.
		/// </summary>
		public bool ManagePresenceSubscriptionRequests
		{
			get => this.managePresenceSubscriptionRequests;
			set => this.managePresenceSubscriptionRequests = value;
		}

		#region Presence subscriptions

		private void Client_OnPresenceUnsubscribe(object Sender, PresenceEventArgs e)
		{
			if (this.managePresenceSubscriptionRequests)
				e.Accept();
		}

		private void Client_OnPresenceSubscribe(object Sender, PresenceEventArgs e)
		{
			if (this.managePresenceSubscriptionRequests)
			{
				if (string.Compare(e.From, this.provisioningServerAddress, true) == 0)
				{
					Log.Informational("Presence subscription from provisioning server accepted.", this.provisioningServerAddress, this.provisioningServerAddress);
					e.Accept();
				}
				else if (!string.IsNullOrEmpty(this.ownerJid) && string.Compare(e.From, this.ownerJid, true) == 0)
				{
					Log.Informational("Presence subscription from owner accepted.", this.ownerJid, this.provisioningServerAddress);
					e.Accept();
				}
				else
					this.IsFriend(XmppClient.GetBareJID(e.From), this.CheckIfFriendCallback, e);
			}
		}

		private void CheckIfFriendCallback(object Sender, IsFriendResponseEventArgs e2)
		{
			PresenceEventArgs e = (PresenceEventArgs)e2.State;

			if (e2.Ok && e2.Friend)
			{
				Log.Informational("Presence subscription accepted.", e.FromBareJID, this.provisioningServerAddress);
				e.Accept();

				RosterItem Item = this.client.GetRosterItem(e.FromBareJID);
				if (Item == null || Item.State == SubscriptionState.None || Item.State == SubscriptionState.From)
					this.client.RequestPresenceSubscription(e.FromBareJID);
			}
			else
			{
				Log.Notice("Presence subscription declined.", e.FromBareJID, this.provisioningServerAddress);
				e.Decline();
			}
		}

		#endregion

		#region Tokens

		/// <summary>
		/// Gets a token for a certicate. This token can be used to identify services, devices or users. The provisioning server will 
		/// challenge the request, and may choose to challenge it further when it is used, to make sure the sender is the correct holder
		/// of the private certificate.
		/// </summary>
		/// <param name="Certificate">Private certificate. Only the public part will be sent to the provisioning server. But the private
		/// part is required in order to be able to respond to challenges sent by the provisioning server.</param>
		/// <param name="Callback">Callback method called, when token is available.</param>
		/// <param name="State">State object that will be passed on to the callback method.</param>
#if WINDOWS_UWP
		public void GetToken(Certificate Certificate, TokenCallback Callback, object State)
#else
		public void GetToken(X509Certificate2 Certificate, TokenCallback Callback, object State)
#endif
		{
			if (!Certificate.HasPrivateKey)
				throw new ArgumentException("Certificate must have private key.", nameof(Certificate));

#if WINDOWS_UWP
			IBuffer Buffer = Certificate.GetCertificateBlob();
			
			CryptographicBuffer.CopyToByteArray(Buffer, out byte[] Bin);
			string Base64 = System.Convert.ToBase64String(Bin);
#else
			byte[] Bin = Certificate.Export(X509ContentType.Cert);
			string Base64 = Convert.ToBase64String(Bin);
#endif
			this.client.SendIqGet(this.provisioningServerAddress, "<getToken xmlns='" + NamespaceProvisioningToken + "'>" + Base64 + "</getToken>",
				this.GetTokenResponse, new object[] { Certificate, Callback, State });
		}

		private void GetTokenResponse(object Sender, IqResultEventArgs e)
		{
			object[] P = (object[])e.State;
#if WINDOWS_UWP
			Certificate Certificate = (Certificate)P[0];
#else
			X509Certificate2 Certificate = (X509Certificate2)P[0];
#endif
			XmlElement E = e.FirstElement;

			if (e.Ok && E != null && E.LocalName == "getTokenChallenge" && E.NamespaceURI == NamespaceProvisioningToken)
			{
				int SeqNr = XML.Attribute(E, "seqnr", 0);
				string Challenge = E.InnerText;
				byte[] Bin = Convert.FromBase64String(Challenge);

#if WINDOWS_UWP
				CryptographicKey Key = PersistedKeyProvider.OpenPublicKeyFromCertificate(Certificate, 
					Certificate.SignatureHashAlgorithmName, CryptographicPadding.RsaPkcs1V15);
				IBuffer Buffer = CryptographicBuffer.CreateFromByteArray(Bin);
				Buffer = CryptographicEngine.Decrypt(Key, Buffer, null);
				CryptographicBuffer.CopyToByteArray(Buffer, out Bin);
				string Response = System.Convert.ToBase64String(Bin);
#else
				Bin = Certificate.GetRSAPrivateKey().Decrypt(Bin, RSAEncryptionPadding.OaepSHA1);
				string Response = Convert.ToBase64String(Bin);
#endif

				this.client.SendIqGet(this.provisioningServerAddress, "<getTokenChallengeResponse xmlns='" + NamespaceProvisioningToken + "' seqnr='" +
					SeqNr.ToString() + "'>" + Response + "</getTokenChallengeResponse>",
					this.GetTokenChallengeResponse, P);
			}
		}

		private void GetTokenChallengeResponse(object Sender, IqResultEventArgs e)
		{
			object[] P = (object[])e.State;
#if WINDOWS_UWP
			Certificate Certificate = (Certificate)P[0];
#else
			X509Certificate2 Certificate = (X509Certificate2)P[0];
#endif
			TokenCallback Callback = (TokenCallback)P[1];
			object State = P[2];
			XmlElement E = e.FirstElement;
			string Token;

			if (e.Ok && E != null && E.LocalName == "getTokenResponse" && E.NamespaceURI == NamespaceProvisioningToken)
			{
				Token = XML.Attribute(E, "token");

				lock (this.certificates)
				{
					this.certificates[Token] = new CertificateUse(Token, Certificate);
				}
			}
			else
				Token = null;

			TokenEventArgs e2 = new TokenEventArgs(e, State, Token);
			try
			{
				Callback(this, e2);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Tells the client a token has been used, for instance in a sensor data request or control operation. Tokens must be
		/// refreshed when they are used, to make sure the client only responds to challenges of recently used certificates.
		/// </summary>
		/// <param name="Token">Token</param>
		public void TokenUsed(string Token)
		{
			lock (this.certificates)
			{
				if (this.certificates.TryGetValue(Token, out CertificateUse Use))
					Use.LastUse = DateTime.Now;
			}
		}

		/// <summary>
		/// Tells the client a token has been used, for instance in a sensor data request or control operation. Tokens must be
		/// refreshed when they are used, to make sure the client only responds to challenges of recently used certificates.
		/// </summary>
		/// <param name="Token">Token</param>
		/// <param name="RemoteJid">Remote JID of entity sending the token.</param>
		public void TokenUsed(string Token, string RemoteJid)
		{
			lock (this.certificates)
			{
				if (this.certificates.TryGetValue(Token, out CertificateUse Use))
				{
					Use.LastUse = DateTime.Now;
					Use.RemoteCertificateJid = RemoteJid;
				}
				else
					this.certificates[Token] = new CertificateUse(Token, RemoteJid);
			}
		}

		private void TokenChallengeHandler(object Sender, IqEventArgs e)
		{
			XmlElement E = e.Query;
			string Token = XML.Attribute(E, "token");
			string Challenge = E.InnerText;
			CertificateUse Use;

			lock (this.certificates)
			{
				if (!this.certificates.TryGetValue(Token, out Use) || (DateTime.Now - Use.LastUse).TotalMinutes > 1)
					throw new ForbiddenException("Token not recognized.", e.IQ);
			}

			if (Use.LocalCertificate != null)
			{
				byte[] Bin = System.Convert.FromBase64String(Challenge);

#if WINDOWS_UWP
				CryptographicKey Key = PersistedKeyProvider.OpenPublicKeyFromCertificate(Use.LocalCertificate,
					Use.LocalCertificate.SignatureHashAlgorithmName, CryptographicPadding.RsaPkcs1V15);
				IBuffer Buffer = CryptographicBuffer.CreateFromByteArray(Bin);
				Buffer = CryptographicEngine.Decrypt(Key, Buffer, null);
				CryptographicBuffer.CopyToByteArray(Buffer, out Bin);
				string Response = System.Convert.ToBase64String(Bin);
#else
				Bin = Use.LocalCertificate.GetRSAPrivateKey().Decrypt(Bin, RSAEncryptionPadding.OaepSHA1);
				string Response = Convert.ToBase64String(Bin);
#endif

				e.IqResult("<tokenChallengeResponse xmlns='" + NamespaceProvisioningToken + "'>" + Response + "</tokenChallengeResponse>");
			}
			else
				this.client.SendIqGet(Use.RemoteCertificateJid, e.Query.OuterXml, this.ForwardedTokenChallengeResponse, e);
		}

		private void ForwardedTokenChallengeResponse(object Sender, IqResultEventArgs e2)
		{
			IqEventArgs e = (IqEventArgs)e2.State;

			if (e2.Ok)
				e.IqResult(e2.FirstElement.OuterXml);
			else
				e.IqError(e2.ErrorElement.OuterXml);
		}

		/// <summary>
		/// Gets a token for a certicate. This token can be used to identify services, devices or users. The provisioning server will 
		/// challenge the request, and may choose to challenge it further when it is used, to make sure the sender is the correct holder
		/// of the private certificate.
		/// </summary>
		/// <param name="Token">Token corresponding to the requested certificate.</param>
		/// <param name="Callback">Callback method called, when certificate is available.</param>
		/// <param name="State">State object that will be passed on to the callback method.</param>
		public void GetCertificate(string Token, CertificateCallback Callback, object State)
		{
			string Address = this.provisioningServerAddress;
			int i = Token.IndexOf(':');

			if (i>0)
			{
				Address = Token.Substring(0, i);
				Token = Token.Substring(i + 1);
			}

			this.client.SendIqGet(Address, "<getCertificate xmlns='" + NamespaceProvisioningToken + "'>" +
				XML.Encode(Token) + "</getCertificate>", (sender, e) =>
				{
					if (Callback != null)
					{
						try
						{
							byte[] Certificate;
							XmlElement E;

							if (e.Ok && (E = e.FirstElement) != null && E.LocalName == "certificate" && E.NamespaceURI == NamespaceProvisioningToken)
								Certificate = Convert.FromBase64String(e.FirstElement.InnerText);
							else
							{
								e.Ok = false;
								Certificate = null;
							}

							CertificateEventArgs e2 = new CertificateEventArgs(e, State, Certificate);
							Callback(sender, e2);
						}
						catch (Exception ex)
						{
							Log.Critical(ex);
						}
					}
				}, null);
		}

		#endregion

		#region Device side

		/// <summary>
		/// Asks the provisioning server if a JID is a friend or not.
		/// </summary>
		/// <param name="JID">JID</param>
		/// <param name="Callback">Method to call when response is received.</param>
		/// <param name="State">State object to pass to callback method.</param>
		public void IsFriend(string JID, IsFriendCallback Callback, object State)
		{
			if ((!string.IsNullOrEmpty(this.ownerJid) && string.Compare(JID, this.ownerJid, true) == 0) ||
				string.Compare(JID, this.provisioningServerAddress, true) == 0)
			{
				if (Callback != null)
				{
					try
					{
						IqResultEventArgs e0 = new IqResultEventArgs(null, string.Empty, this.client.FullJID, this.provisioningServerAddress, true, State);
						IsFriendResponseEventArgs e = new IsFriendResponseEventArgs(e0, State, JID, true);

						Callback(this.client, e);
					}
					catch (Exception ex)
					{
						Log.Critical(ex);
					}
				}

				return;
			}

			this.CachedIqGet("<isFriend xmlns='" + NamespaceProvisioningDevice + "' jid='" +
				XML.Encode(JID) + "'/>", this.IsFriendCallback, new object[] { Callback, State });
		}

		private void IsFriendCallback(object Sender, IqResultEventArgs e)
		{
			object[] P = (object[])e.State;
			IsFriendCallback Callback = (IsFriendCallback)P[0];
			object State = P[1];
			string JID;
			bool Result;
			XmlElement E = e.FirstElement;

			if (e.Ok && E != null && E.LocalName == "isFriendResponse" && E.NamespaceURI == NamespaceProvisioningDevice)
			{
				JID = XML.Attribute(E, "jid");
				Result = XML.Attribute(E, "result", false);
			}
			else
			{
				Result = false;
				JID = null;
			}

			IsFriendResponseEventArgs e2 = new IsFriendResponseEventArgs(e, State, JID, Result);
			try
			{
				Callback(this, e2);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private void UnfriendHandler(object Sender, MessageEventArgs e)
		{
			if (e.From == this.provisioningServerAddress)
			{
				string Jid = XML.Attribute(e.Content, "jid");

				if (!string.IsNullOrEmpty(Jid))
					this.client.RequestPresenceUnsubscription(Jid);
			}
		}

		private void FriendHandler(object Sender, MessageEventArgs e)
		{
			if (e.From == this.provisioningServerAddress)
			{
				string Jid = XML.Attribute(e.Content, "jid");

				if (!string.IsNullOrEmpty(Jid))
					this.client.RequestPresenceSubscription(Jid);
			}
		}

		private bool Split(string FromBareJid, IEnumerable<IThingReference> Nodes, out IEnumerable<IThingReference> ToCheck,
			out IEnumerable<IThingReference> Permitted)
		{
			if (string.Compare(FromBareJid, this.provisioningServerAddress, true) == 0)
			{
				ToCheck = null;
				Permitted = Nodes;

				return true;
			}
			else if (Nodes == null)
			{
				ToCheck = null;
				Permitted = null;

				return !string.IsNullOrEmpty(this.ownerJid) && string.Compare(FromBareJid, this.ownerJid, true) == 0;
			}
			else
			{
				LinkedList<IThingReference> ToCheck2 = null;
				LinkedList<IThingReference> Permitted2 = null;
				string Owner;
				bool Safe;

				foreach (IThingReference Ref in Nodes)
				{
					if (Ref is ILifeCycleManagement LifeCycleManagement)
					{
						Safe = (!LifeCycleManagement.IsProvisioned) ||
							(!string.IsNullOrEmpty(Owner = LifeCycleManagement.Owner) && string.Compare(FromBareJid, Owner, true) == 0);
					}
					else if (string.IsNullOrEmpty(Ref.NodeId) && string.IsNullOrEmpty(Ref.SourceId) && string.IsNullOrEmpty(Ref.Partition))
						Safe = string.Compare(FromBareJid, this.ownerJid, true) == 0;
					else
						Safe = false;

					if (Safe)
					{
						if (Permitted2 == null)
						{
							ToCheck2 = new LinkedList<IThingReference>();
							Permitted2 = new LinkedList<IThingReference>();

							foreach (IThingReference Ref2 in Nodes)
							{
								if (Ref2 == Ref)
									break;

								ToCheck2.AddLast(Ref2);
							}
						}

						Permitted2.AddLast(Ref);
					}
					else if (ToCheck2 != null)
						ToCheck2.AddLast(Ref);
				}

				if (Permitted2 == null)
				{
					ToCheck = Nodes;
					Permitted = null;

					return false;
				}
				else
				{
					ToCheck = ToCheck2;
					Permitted = Permitted2;

					return ToCheck2.First == null;
				}
			}
		}

		/// <summary>
		/// Checks if a readout can be performed.
		/// </summary>
		/// <param name="RequestFromBareJid">Readout request came from this bare JID.</param>
		/// <param name="FieldTypes">Field types requested.</param>
		/// <param name="Nodes">Any nodes included in the request.</param>
		/// <param name="FieldNames">And field names included in the request. If null, all field names are requested.</param>
		/// <param name="ServiceTokens">Any service tokens provided.</param>
		/// <param name="DeviceTokens">Any device tokens provided.</param>
		/// <param name="UserTokens">Any user tokens provided.</param>
		/// <param name="Callback">Method to call when result is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		public void CanRead(string RequestFromBareJid, FieldType FieldTypes, IEnumerable<IThingReference> Nodes, IEnumerable<string> FieldNames,
			string[] ServiceTokens, string[] DeviceTokens, string[] UserTokens, CanReadCallback Callback, object State)
		{
			if (Split(RequestFromBareJid, Nodes, out IEnumerable<IThingReference> ToCheck, out IEnumerable<IThingReference> Permitted))
			{
				if (Callback != null)
				{
					try
					{
						IThingReference[] Nodes2 = Permitted as IThingReference[];
						if (Nodes2 == null && Permitted != null)
						{
							List<IThingReference> List = new List<IThingReference>();
							List.AddRange(Permitted);
							Nodes2 = List.ToArray();
						}

						string[] FieldNames2 = FieldNames as string[];
						if (FieldNames2 == null && FieldNames != null)
						{
							List<string> List = new List<string>();
							List.AddRange(FieldNames);
							FieldNames2 = List.ToArray();
						}

						IqResultEventArgs e0 = new IqResultEventArgs(null, string.Empty, this.client.FullJID, this.provisioningServerAddress, true, State);
						CanReadResponseEventArgs e = new CanReadResponseEventArgs(e0, State, RequestFromBareJid, true, FieldTypes, Nodes2, FieldNames2);

						Callback(this.client, e);
					}
					catch (Exception ex)
					{
						Log.Critical(ex);
					}
				}

				return;
			}

			StringBuilder Xml = new StringBuilder();

			Xml.Append("<canRead xmlns='");
			Xml.Append(NamespaceProvisioningDevice);
			Xml.Append("' jid='");
			Xml.Append(XML.Encode(RequestFromBareJid));

			this.AppendTokens(Xml, "st", ServiceTokens);
			this.AppendTokens(Xml, "dt", DeviceTokens);
			this.AppendTokens(Xml, "ut", UserTokens);

			if ((FieldTypes & FieldType.All) == FieldType.All)
				Xml.Append("' all='true");
			else
			{
				if (FieldTypes.HasFlag(FieldType.Momentary))
					Xml.Append("' m='true");

				if (FieldTypes.HasFlag(FieldType.Peak))
					Xml.Append("' p='true");

				if (FieldTypes.HasFlag(FieldType.Status))
					Xml.Append("' s='true");

				if (FieldTypes.HasFlag(FieldType.Computed))
					Xml.Append("' c='true");

				if (FieldTypes.HasFlag(FieldType.Identity))
					Xml.Append("' i='true");

				if (FieldTypes.HasFlag(FieldType.Historical))
					Xml.Append("' h='true");
			}

			if (ToCheck == null && FieldNames == null)
				Xml.Append("'/>");
			else
			{
				Xml.Append("'>");

				if (ToCheck != null)
				{
					foreach (IThingReference Node in ToCheck)
						this.AppendNode(Xml, Node);
				}

				if (FieldNames != null)
				{
					foreach (string FieldName in FieldNames)
					{
						Xml.Append("<f n='");
						Xml.Append(XML.Encode(FieldName));
						Xml.Append("'/>");
					}
				}

				Xml.Append("</canRead>");
			}

			this.CachedIqGet(Xml.ToString(), (sender, e) =>
			{
				XmlElement E = e.FirstElement;
				List<IThingReference> Nodes2 = null;
				List<string> Fields2 = null;
				FieldType FieldTypes2 = (FieldType)0;
				string Jid = string.Empty;
				string NodeId;
				string SourceId;
				string Partition;
				bool b;
				bool CanRead;

				if (e.Ok && E.LocalName == "canReadResponse" && E.NamespaceURI == NamespaceProvisioningDevice)
				{
					CanRead = XML.Attribute(E, "result", false);

					foreach (XmlAttribute Attr in E.Attributes)
					{
						switch (Attr.Name)
						{
							case "jid":
								Jid = Attr.Value;
								break;

							case "all":
								if (CommonTypes.TryParse(Attr.Value, out b) && b)
									FieldTypes2 |= FieldType.All;
								break;

							case "h":
								if (CommonTypes.TryParse(Attr.Value, out b) && b)
									FieldTypes2 |= FieldType.Historical;
								break;

							case "m":
								if (CommonTypes.TryParse(Attr.Value, out b) && b)
									FieldTypes2 |= FieldType.Momentary;
								break;

							case "p":
								if (CommonTypes.TryParse(Attr.Value, out b) && b)
									FieldTypes2 |= FieldType.Peak;
								break;

							case "s":
								if (CommonTypes.TryParse(Attr.Value, out b) && b)
									FieldTypes2 |= FieldType.Status;
								break;

							case "c":
								if (CommonTypes.TryParse(Attr.Value, out b) && b)
									FieldTypes2 |= FieldType.Computed;
								break;

							case "i":
								if (CommonTypes.TryParse(Attr.Value, out b) && b)
									FieldTypes2 |= FieldType.Identity;
								break;
						}
					}

					if (CanRead)
					{
						if (Permitted != null)
							Nodes2 = new List<IThingReference>(Permitted);

						foreach (XmlNode N in E.ChildNodes)
						{
							switch (N.LocalName)
							{
								case "nd":
									if (Nodes2 == null)
										Nodes2 = new List<IThingReference>();

									E = (XmlElement)N;
									NodeId = XML.Attribute(E, "id");
									SourceId = XML.Attribute(E, "src");
									Partition = XML.Attribute(E, "pt");

									bool Found = false;

									foreach (IThingReference Ref in Nodes)
									{
										if (Ref.NodeId == NodeId && Ref.SourceId == SourceId && Ref.Partition == Partition)
										{
											Nodes2.Add(Ref);
											Found = true;
											break;
										}
									}

									if (!Found)
										Nodes2.Add(new ThingReference(NodeId, SourceId, Partition));
									break;

								case "f":
									if (Fields2 == null)
										Fields2 = new List<string>();

									Fields2.Add(XML.Attribute((XmlElement)N, "n"));
									break;
							}
						}
					}
					else if (Permitted != null)
					{
						CanRead = true;
						Jid = RequestFromBareJid;
						FieldTypes2 = FieldTypes;
						Nodes2 = new List<IThingReference>(Permitted);

						if (FieldNames != null)
							Fields2 = new List<string>(FieldNames);
					}
				}
				else
					CanRead = false;

				CanReadResponseEventArgs e2 = new CanReadResponseEventArgs(e, State, Jid, CanRead, FieldTypes2, Nodes2?.ToArray(), Fields2?.ToArray());

				try
				{
					Callback(this, e2);
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}

			}, null);
		}

		private void AppendNode(StringBuilder Xml, IThingReference Node)
		{
			Xml.Append("<nd");
			this.AppendNodeInfo(Xml, Node.NodeId, Node.SourceId, Node.Partition);
			Xml.Append("/>");
		}

		private void AppendNodeInfo(StringBuilder Xml, string NodeId, string SourceId, string Partition)
		{
			Xml.Append(" id='");
			Xml.Append(XML.Encode(NodeId));

			if (!string.IsNullOrEmpty(SourceId))
			{
				Xml.Append("' src='");
				Xml.Append(XML.Encode(SourceId));
			}

			if (!string.IsNullOrEmpty(Partition))
			{
				Xml.Append("' pt='");
				Xml.Append(XML.Encode(Partition));
			}

			Xml.Append('\'');
		}

		private void AppendTokens(StringBuilder Xml, string AttributeName, string[] Tokens)
		{
			if (Tokens != null && Tokens.Length > 0)
			{
				Xml.Append("' ");
				Xml.Append(AttributeName);
				Xml.Append("='");

				bool First = true;

				foreach (string Token in Tokens)
				{
					if (First)
						First = false;
					else
						Xml.Append(' ');

					Xml.Append(Token);
				}
			}
		}

		/// <summary>
		/// Checks if a control operation can be performed.
		/// </summary>
		/// <param name="RequestFromBareJid">Readout request came from this bare JID.</param>
		/// <param name="Nodes">Any nodes included in the request.</param>
		/// <param name="ParameterNames">And parameter names included in the request. If null, all parameter names are requested.</param>
		/// <param name="ServiceTokens">Any service tokens provided.</param>
		/// <param name="DeviceTokens">Any device tokens provided.</param>
		/// <param name="UserTokens">Any user tokens provided.</param>
		/// <param name="Callback">Method to call when result is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		public void CanControl(string RequestFromBareJid, IEnumerable<IThingReference> Nodes, IEnumerable<string> ParameterNames,
			string[] ServiceTokens, string[] DeviceTokens, string[] UserTokens, CanControlCallback Callback, object State)
		{
			if (Split(RequestFromBareJid, Nodes, out IEnumerable<IThingReference> ToCheck, out IEnumerable<IThingReference> Permitted))
			{
				if (Callback != null)
				{
					try
					{
						IThingReference[] Nodes2 = Nodes as IThingReference[];
						if (Nodes2 == null && Nodes != null)
						{
							List<IThingReference> List = new List<IThingReference>();
							List.AddRange(Nodes);
							Nodes2 = List.ToArray();
						}

						string[] ParameterNames2 = ParameterNames as string[];
						if (ParameterNames2 == null && ParameterNames != null)
						{
							List<string> List = new List<string>();
							List.AddRange(ParameterNames);
							ParameterNames2 = List.ToArray();
						}

						IqResultEventArgs e0 = new IqResultEventArgs(null, string.Empty, this.client.FullJID, this.provisioningServerAddress, true, State);
						CanControlResponseEventArgs e = new CanControlResponseEventArgs(e0, State, RequestFromBareJid, true, Nodes2, ParameterNames2);

						Callback(this.client, e);
					}
					catch (Exception ex)
					{
						Log.Critical(ex);
					}
				}

				return;
			}

			StringBuilder Xml = new StringBuilder();

			Xml.Append("<canControl xmlns='");
			Xml.Append(NamespaceProvisioningDevice);
			Xml.Append("' jid='");
			Xml.Append(XML.Encode(RequestFromBareJid));

			this.AppendTokens(Xml, "st", ServiceTokens);
			this.AppendTokens(Xml, "dt", DeviceTokens);
			this.AppendTokens(Xml, "ut", UserTokens);

			if (ToCheck == null && ParameterNames == null)
				Xml.Append("'/>");
			else
			{
				Xml.Append("'>");


				if (ToCheck != null)
				{
					foreach (IThingReference Node in ToCheck)
						this.AppendNode(Xml, Node);
				}

				if (ParameterNames != null)
				{
					foreach (string ParameterName in ParameterNames)
					{
						Xml.Append("<parameter name='");
						Xml.Append(XML.Encode(ParameterName));
						Xml.Append("'/>");
					}
				}

				Xml.Append("</canControl>");
			}

			this.CachedIqGet(Xml.ToString(), (sender, e) =>
			{
				XmlElement E = e.FirstElement;
				List<IThingReference> Nodes2 = null;
				List<string> ParameterNames2 = null;
				string Jid = string.Empty;
				string NodeId;
				string SourceId;
				string Partition;
				bool CanControl;

				if (e.Ok && E.LocalName == "canControlResponse" && E.NamespaceURI == NamespaceProvisioningDevice)
				{
					CanControl = XML.Attribute(E, "result", false);

					foreach (XmlAttribute Attr in E.Attributes)
					{
						if (Attr.Name == "jid")
							Jid = Attr.Value;
					}

					if (CanControl)
					{
						if (Permitted != null)
							Nodes2 = new List<IThingReference>(Permitted);

						foreach (XmlNode N in E.ChildNodes)
						{
							switch (N.LocalName)
							{
								case "nd":
									if (Nodes2 == null)
										Nodes2 = new List<IThingReference>();

									E = (XmlElement)N;
									NodeId = XML.Attribute(E, "id");
									SourceId = XML.Attribute(E, "src");
									Partition = XML.Attribute(E, "pt");

									bool Found = false;

									foreach (IThingReference Ref in Nodes)
									{
										if (Ref.NodeId == NodeId && Ref.SourceId == SourceId && Ref.Partition == Partition)
										{
											Nodes2.Add(Ref);
											Found = true;
											break;
										}
									}

									if (!Found)
										Nodes2.Add(new ThingReference(NodeId, SourceId, Partition));
									break;

								case "parameter":
									if (ParameterNames2 == null)
										ParameterNames2 = new List<string>();

									ParameterNames2.Add(XML.Attribute((XmlElement)N, "name"));
									break;
							}
						}
					}
					else if (Permitted != null)
					{
						CanControl = true;
						Jid = RequestFromBareJid;
						Nodes2 = new List<IThingReference>(Permitted);

						if (ParameterNames != null)
							ParameterNames2 = new List<string>(ParameterNames);
					}
				}
				else
					CanControl = false;

				CanControlResponseEventArgs e2 = new CanControlResponseEventArgs(e, State, Jid, CanControl,
					Nodes2?.ToArray(), ParameterNames2?.ToArray());

				try
				{
					Callback(this, e2);
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}

			}, null);
		}

		#endregion

		#region Cached queries

		private Task CachedIqGet(string Xml, IqResultEventHandler Callback, object State)
		{
			return this.CachedIq(Xml, "get", Callback, State);
		}

		private Task CachedIqSet(string Xml, IqResultEventHandler Callback, object State)
		{
			return this.CachedIq(Xml, "set", Callback, State);
		}

		private async Task CachedIq(string Xml, string Method, IqResultEventHandler Callback, object State)
		{
			CachedQuery Query = await Database.FindFirstDeleteRest<CachedQuery>(new FilterAnd(
				new FilterFieldEqualTo("Xml", Xml), new FilterFieldEqualTo("Method", Method)));

			if (Query != null)
			{
				Query.LastUsed = DateTime.Now;
				await Database.Update(Query);

				if (Callback != null)
				{
					try
					{
						XmlDocument Doc = new XmlDocument();
						Doc.LoadXml(Query.Response);

						XmlElement E = Doc.DocumentElement;
						string Type = XML.Attribute(E, "type");
						string Id = XML.Attribute(E, "id");
						string To = XML.Attribute(E, "to");
						string From = XML.Attribute(E, "from");
						bool Ok = (Type == "result");

						IqResultEventArgs e = new IqResultEventArgs(E, Id, To, From, Ok, State);

						Callback(this.client, e);
					}
					catch (Exception ex)
					{
						Log.Critical(ex);
					}
				}
			}
			else
			{
				this.client.SendIq(null, this.provisioningServerAddress, Xml, "get", this.CachedIqCallback, new object[] { Callback, State, Xml, Method },
					this.client.DefaultRetryTimeout, this.client.DefaultNrRetries,
					this.client.DefaultDropOff, this.client.DefaultMaxRetryTimeout);
			}
		}

		private async void CachedIqCallback(object Sender, IqResultEventArgs e)
		{
			try
			{
				object[] P = (object[])e.State;
				IqResultEventHandler Callback = (IqResultEventHandler)P[0];
				object State = P[1];
				string Xml = (string)P[2];
				string Method = (string)P[3];

				CachedQuery Query = new CachedQuery()
				{
					Xml = Xml,
					Method = Method,
					Response = e.Response.OuterXml,
					LastUsed = DateTime.Now
				};

				await Database.Insert(Query);

				if (Callback != null)
				{
					e.State = State;
					Callback(Sender, e);
				}

				await this.DeleteOld();
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private async Task DeleteOld()
		{
			DateTime Now = DateTime.Now;
			if ((Now - this.lastCheck).TotalDays < 1)
				return;

			this.lastCheck = Now;

			DateTime Limit = Now - this.cacheUnusedLifetime;

			foreach (CachedQuery Query in await Database.Find<CachedQuery>(new FilterFieldLesserOrEqualTo("LastUsed", Limit)))
				await Database.Delete(Query);
		}

		/// <summary>
		/// Time unused rules are kept in the rule cache.
		/// (Default is 13 months.)
		/// </summary>
		public Duration CacheUnusedLifetime
		{
			get { return this.cacheUnusedLifetime; }
			set { this.cacheUnusedLifetime = value; }
		}

		private async void ClearCacheHandler(object Sender, IqEventArgs e)
		{
			try
			{
				if (e.From == this.provisioningServerAddress)
				{
					await this.ClearInternalCache();
					e.IqResult(string.Empty);
				}
				else
					e.IqError(new ForbiddenException("Unauthorized sender.", e.IQ));
			}
			catch (Exception ex)
			{
				e.IqError(ex);
			}
		}

		private async void ClearCacheHandler(object Sender, MessageEventArgs e)
		{
			try
			{
				if (e.From == this.provisioningServerAddress)
					await this.ClearInternalCache();
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private Task ClearInternalCache()
		{
			return Database.Clear("CachedProvisioningQueries");
		}

		#endregion

		#region Owner side

		private void IsFriendHandler(object Sender, MessageEventArgs e)
		{
			IsFriendEventHandler h = this.IsFriendQuestion;

			if (h != null)
			{
				try
				{
					h(this, new IsFriendEventArgs(this, e));
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
		}

		/// <summary>
		/// Event is raised when the provisioning server asks the owner if a device is allowed to accept a friendship request.
		/// </summary>
		public event IsFriendEventHandler IsFriendQuestion = null;

		/// <summary>
		/// Sends a response to a previous "Is Friend" question.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="IsFriend">If the response is yes or no.</param>
		/// <param name="Range">The range of the response.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void IsFriendResponse(string JID, string RemoteJID, string Key, bool IsFriend, RuleRange Range, IqResultEventHandler Callback, object State)
		{
			StringBuilder Xml = new StringBuilder();

			Xml.Append("<isFriendRule xmlns='");
			Xml.Append(ProvisioningClient.NamespaceProvisioningOwner);
			Xml.Append("' jid='");
			Xml.Append(XML.Encode(JID));
			Xml.Append("' remoteJid='");
			Xml.Append(XML.Encode(RemoteJID));
			Xml.Append("' key='");
			Xml.Append(XML.Encode(Key));
			Xml.Append("' result='");
			Xml.Append(CommonTypes.Encode(IsFriend));

			if (Range != RuleRange.Caller)
			{
				Xml.Append("' range='");
				Xml.Append(Range.ToString());
			}

			Xml.Append("'/>");

			this.client.SendIqSet(this.provisioningServerAddress, Xml.ToString(), Callback, State);
		}

		private void CanReadHandler(object Sender, MessageEventArgs e)
		{
			CanReadEventHandler h = this.CanReadQuestion;

			if (h != null)
			{
				try
				{
					h(this, new CanReadEventArgs(this, e));
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
		}

		/// <summary>
		/// Event is raised when the provisioning server asks the owner if a device is allowed to be read.
		/// </summary>
		public event CanReadEventHandler CanReadQuestion = null;

		/// <summary>
		/// Sends a response to a previous "Can Read" question, based on the JID of the caller.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to read the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanReadResponseCaller(string JID, string RemoteJID, string Key, bool CanRead, FieldType FieldTypes, string[] FieldNames,
			IThingReference Node, IqResultEventHandler Callback, object State)
		{
			this.CanReadResponse(JID, RemoteJID, Key, CanRead, FieldTypes, FieldNames, Node, "<fromJid/>", Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Read" question, based on the domain of the caller.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to read the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanReadResponseDomain(string JID, string RemoteJID, string Key, bool CanRead, FieldType FieldTypes, string[] FieldNames,
			IThingReference Node, IqResultEventHandler Callback, object State)
		{
			this.CanReadResponse(JID, RemoteJID, Key, CanRead, FieldTypes, FieldNames, Node, "<fromDomain/>", Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Read" question, based on a service token.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to read the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Token">Token.</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanReadResponseService(string JID, string RemoteJID, string Key, bool CanRead, FieldType FieldTypes, string[] FieldNames,
			string Token, IThingReference Node, IqResultEventHandler Callback, object State)
		{
			this.CanReadResponse(JID, RemoteJID, Key, CanRead, FieldTypes, FieldNames, Node, "<fromService token='" + XML.Encode(Token) + "'/>", Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Read" question, based on a device token.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to read the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Token">Token.</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanReadResponseDevice(string JID, string RemoteJID, string Key, bool CanRead, FieldType FieldTypes, string[] FieldNames,
			string Token, IThingReference Node, IqResultEventHandler Callback, object State)
		{
			this.CanReadResponse(JID, RemoteJID, Key, CanRead, FieldTypes, FieldNames, Node, "<fromDevice token='" + XML.Encode(Token) + "'/>", Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Read" question, based on a user token.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to read the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Token">Token.</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanReadResponseUser(string JID, string RemoteJID, string Key, bool CanRead, FieldType FieldTypes, string[] FieldNames,
			string Token, IThingReference Node, IqResultEventHandler Callback, object State)
		{
			this.CanReadResponse(JID, RemoteJID, Key, CanRead, FieldTypes, FieldNames, Node, "<fromUser token='" + XML.Encode(Token) + "'/>", Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Read" question, for all future requests.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to read the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanReadResponseAll(string JID, string RemoteJID, string Key, bool CanRead, FieldType FieldTypes, string[] FieldNames,
			IThingReference Node, IqResultEventHandler Callback, object State)
		{
			this.CanReadResponse(JID, RemoteJID, Key, CanRead, FieldTypes, FieldNames, Node, "<all/>", Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Read" question.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanRead">If the caller is allowed to read the device.</param>
		/// <param name="FieldTypes">Field types allowed.</param>
		/// <param name="FieldNames">Field names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="OriginXml">Origin XML.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		private void CanReadResponse(string JID, string RemoteJID, string Key, bool CanRead, FieldType FieldTypes, string[] FieldNames,
			IThingReference Node, string OriginXml, IqResultEventHandler Callback, object State)
		{
			StringBuilder Xml = new StringBuilder();

			Xml.Append("<canReadRule xmlns='");
			Xml.Append(ProvisioningClient.NamespaceProvisioningOwner);
			Xml.Append("' jid='");
			Xml.Append(XML.Encode(JID));
			Xml.Append("' remoteJid='");
			Xml.Append(XML.Encode(RemoteJID));
			Xml.Append("' key='");
			Xml.Append(XML.Encode(Key));
			Xml.Append("' result='");
			Xml.Append(CommonTypes.Encode(CanRead));
			Xml.Append("'>");

			if (CanRead)
			{
				if (Node != null && (!string.IsNullOrEmpty(Node.NodeId) || !string.IsNullOrEmpty(Node.SourceId) || !string.IsNullOrEmpty(Node.Partition)))
					this.AppendNode(Xml, Node);

				if (FieldTypes != FieldType.All || (FieldNames != null && FieldNames.Length > 0))
				{
					Xml.Append("<partial");

					if (FieldTypes == FieldType.All)
						Xml.Append(" all='true'");
					else
					{
						if ((FieldTypes & FieldType.Momentary) != 0)
							Xml.Append(" m='true'");

						if ((FieldTypes & FieldType.Identity) != 0)
							Xml.Append(" i='true'");

						if ((FieldTypes & FieldType.Status) != 0)
							Xml.Append(" s='true'");

						if ((FieldTypes & FieldType.Computed) != 0)
							Xml.Append(" c='true'");

						if ((FieldTypes & FieldType.Peak) != 0)
							Xml.Append(" p='true'");

						if ((FieldTypes & FieldType.Historical) != 0)
							Xml.Append(" h='true'");
					}

					if (FieldNames == null || FieldNames.Length == 0)
						Xml.Append("/>");
					else
					{
						Xml.Append(">");

						foreach (string FieldName in FieldNames)
						{
							Xml.Append("<f n='");
							Xml.Append(XML.Encode(FieldName));
							Xml.Append("'/>");
						}

						Xml.Append("</partial>");
					}
				}
			}

			Xml.Append(OriginXml);
			Xml.Append("</canReadRule>");

			this.client.SendIqSet(this.provisioningServerAddress, Xml.ToString(), Callback, State);
		}

		private void CanControlHandler(object Sender, MessageEventArgs e)
		{
			CanControlEventHandler h = this.CanControlQuestion;

			if (h != null)
			{
				try
				{
					h(this, new CanControlEventArgs(this, e));
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
		}

		/// <summary>
		/// Event is raised when the provisioning server asks the owner if a device is allowed to be controlled.
		/// </summary>
		public event CanControlEventHandler CanControlQuestion = null;

		/// <summary>
		/// Sends a response to a previous "Can Control" question, based on the JID of the caller.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanControlResponseCaller(string JID, string RemoteJID, string Key, bool CanControl, string[] ParameterNames,
			IThingReference Node, IqResultEventHandler Callback, object State)
		{
			this.CanControlResponse(JID, RemoteJID, Key, CanControl, ParameterNames, Node, "<fromJid/>", Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Control" question, based on the domain of the caller.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanControlResponseDomain(string JID, string RemoteJID, string Key, bool CanControl, string[] ParameterNames,
			IThingReference Node, IqResultEventHandler Callback, object State)
		{
			this.CanControlResponse(JID, RemoteJID, Key, CanControl, ParameterNames, Node, "<fromDomain/>", Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Control" question, based on a service token.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Token">Token.</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanControlResponseService(string JID, string RemoteJID, string Key, bool CanControl, string[] ParameterNames,
			string Token, IThingReference Node, IqResultEventHandler Callback, object State)
		{
			this.CanControlResponse(JID, RemoteJID, Key, CanControl, ParameterNames, Node, "<fromService token='" + XML.Encode(Token) + "'/>", Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Control" question, based on a device token.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Token">Token.</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanControlResponseDevice(string JID, string RemoteJID, string Key, bool CanControl, string[] ParameterNames,
			string Token, IThingReference Node, IqResultEventHandler Callback, object State)
		{
			this.CanControlResponse(JID, RemoteJID, Key, CanControl, ParameterNames, Node, "<fromDevice token='" + XML.Encode(Token) + "'/>", Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Control" question, based on a user token.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Token">Token.</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanControlResponseUser(string JID, string RemoteJID, string Key, bool CanControl, string[] ParameterNames,
			string Token, IThingReference Node, IqResultEventHandler Callback, object State)
		{
			this.CanControlResponse(JID, RemoteJID, Key, CanControl, ParameterNames, Node, "<fromUser token='" + XML.Encode(Token) + "'/>", Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Control" question, for all future requests.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void CanControlResponseAll(string JID, string RemoteJID, string Key, bool CanControl, string[] ParameterNames,
			IThingReference Node, IqResultEventHandler Callback, object State)
		{
			this.CanControlResponse(JID, RemoteJID, Key, CanControl, ParameterNames, Node, "<all/>", Callback, State);
		}

		/// <summary>
		/// Sends a response to a previous "Can Control" question.
		/// </summary>
		/// <param name="JID">JID of device asking the question.</param>
		/// <param name="RemoteJID">JID of caller.</param>
		/// <param name="Key">Key corresponding to request.</param>
		/// <param name="CanControl">If the caller is allowed to control the device.</param>
		/// <param name="ParameterNames">Parameter names allowed</param>
		/// <param name="Node">Optional node reference. Can be null or <see cref="ThingReference.Empty"/>.</param>
		/// <param name="OriginXml">Origin XML.</param>
		/// <param name="Callback">Optional callback method to call, when response to request has been received.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		private void CanControlResponse(string JID, string RemoteJID, string Key, bool CanControl, string[] ParameterNames,
			IThingReference Node, string OriginXml, IqResultEventHandler Callback, object State)
		{
			StringBuilder Xml = new StringBuilder();

			Xml.Append("<canControlRule xmlns='");
			Xml.Append(ProvisioningClient.NamespaceProvisioningOwner);
			Xml.Append("' jid='");
			Xml.Append(XML.Encode(JID));
			Xml.Append("' remoteJid='");
			Xml.Append(XML.Encode(RemoteJID));
			Xml.Append("' key='");
			Xml.Append(XML.Encode(Key));
			Xml.Append("' result='");
			Xml.Append(CommonTypes.Encode(CanControl));
			Xml.Append("'>");

			if (CanControl)
			{
				if (Node != null && (!string.IsNullOrEmpty(Node.NodeId) || !string.IsNullOrEmpty(Node.SourceId) || !string.IsNullOrEmpty(Node.Partition)))
					this.AppendNode(Xml, Node);

				if (ParameterNames != null && ParameterNames.Length > 0)
				{
					Xml.Append("<partial");

					if (ParameterNames == null || ParameterNames.Length == 0)
						Xml.Append("/>");
					else
					{
						Xml.Append(">");

						foreach (string ParameterName in ParameterNames)
						{
							Xml.Append("<p n='");
							Xml.Append(XML.Encode(ParameterName));
							Xml.Append("'/>");
						}

						Xml.Append("</partial>");
					}
				}
			}

			Xml.Append(OriginXml);
			Xml.Append("</canControlRule>");

			this.client.SendIqSet(this.provisioningServerAddress, Xml.ToString(), Callback, State);
		}

		/// <summary>
		/// Clears the rule caches of all owned devices.
		/// </summary>
		public void ClearDeviceCaches()
		{
			this.ClearDeviceCache(null, null, null);
		}

		/// <summary>
		/// Clears the rule caches of all owned devices.
		/// </summary>
		/// <param name="Callback">Method to call when response is returned.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void ClearDeviceCaches(IqResultEventHandler Callback, object State)
		{
			this.ClearDeviceCache(null, Callback, State);
		}

		/// <summary>
		/// Clears the rule cache of a device.
		/// </summary>
		/// <param name="DeviceJID">Bare JID of device whose rule cache is to be cleared.
		/// If null, all owned devices will get their rule caches cleared.</param>
		public void ClearDeviceCache(string DeviceJID)
		{
			this.ClearDeviceCache(DeviceJID, null, null);
		}

		/// <summary>
		/// Clears the rule cache of a device.
		/// </summary>
		/// <param name="DeviceJID">Bare JID of device whose rule cache is to be cleared.
		/// If null, all owned devices will get their rule caches cleared.</param>
		/// <param name="Callback">Method to call when response is returned.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void ClearDeviceCache(string DeviceJID, IqResultEventHandler Callback, object State)
		{
			StringBuilder Xml = new StringBuilder();

			Xml.Append("<clearCache xmlns='");
			Xml.Append(NamespaceProvisioningOwner);

			if (!string.IsNullOrEmpty(DeviceJID))
			{
				Xml.Append("' jid='");
				Xml.Append(XML.Encode(DeviceJID));
			}

			Xml.Append("'/>");

			this.client.SendIqSet(this.provisioningServerAddress, Xml.ToString(), Callback, State);
		}

		/// <summary>
		/// Gets devices owned by the caller.
		/// </summary>
		/// <param name="Offset">Device list offset.</param>
		/// <param name="MaxCount">Maximum number of things to return.</param>
		/// <param name="Callback">Method to call when result has been received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		public void GetDevices(int Offset, int MaxCount, SearchResultEventHandler Callback, object State)
		{
			StringBuilder Request = new StringBuilder();

			Request.Append("<getDevices xmlns='");
			Request.Append(NamespaceProvisioningOwner);
			Request.Append("' offset='");
			Request.Append(Offset.ToString());
			Request.Append("' maxCount='");
			Request.Append(MaxCount.ToString());
			Request.Append("'/>");

			this.client.SendIqGet(this.provisioningServerAddress, Request.ToString(), (sender, e) =>
			{
				ThingRegistryClient.ParseResultSet(Offset, MaxCount, this, e, Callback, State);
			}, null);
		}

		/// <summary>
		/// Deletes te device rules of all owned devices.
		/// </summary>
		public void DeleteDeviceRules()
		{
			this.DeleteDeviceRules(null, string.Empty, string.Empty, string.Empty, null, null);
		}

		/// <summary>
		/// Deletes te device rules all owned devices.
		/// </summary>
		/// <param name="Callback">Method to call when response is returned.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void DeleteDeviceRules(IqResultEventHandler Callback, object State)
		{
			this.DeleteDeviceRules(null, string.Empty, string.Empty, string.Empty, Callback, State);
		}

		/// <summary>
		/// Deletes the rules of a device.
		/// </summary>
		/// <param name="DeviceJID">Bare JID of device whose rules are to be deleted.
		/// If null, all owned devices will get their rules deleted.</param>
		public void DeleteDeviceRules(string DeviceJID)
		{
			this.DeleteDeviceRules(DeviceJID, string.Empty, string.Empty, string.Empty, null, null);
		}

		/// <summary>
		/// Deletes the rules of a device.
		/// </summary>
		/// <param name="DeviceJID">Bare JID of device whose rules are to be deleted.
		/// If null, all owned devices will get their rules deleted.</param>
		/// <param name="Callback">Method to call when response is returned.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void DeleteDeviceRules(string DeviceJID, IqResultEventHandler Callback, object State)
		{
			this.DeleteDeviceRules(DeviceJID, string.Empty, string.Empty, string.Empty, Callback, State);
		}

		/// <summary>
		/// Deletes the rules of a device.
		/// </summary>
		/// <param name="DeviceJID">Bare JID of device whose rules are to be deleted.
		/// If null, all owned devices will get their rules deleted.</param>
		/// <param name="NodeId">Optional Node ID of device.</param>
		/// <param name="SourceId">Optional Source ID of device.</param>
		/// <param name="Partition">Optional Partition of device.</param>
		/// <param name="Callback">Method to call when response is returned.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void DeleteDeviceRules(string DeviceJID, string NodeId, string SourceId, string Partition, IqResultEventHandler Callback, object State)
		{
			StringBuilder Xml = new StringBuilder();

			Xml.Append("<deleteRules xmlns='");
			Xml.Append(NamespaceProvisioningOwner);

			if (!string.IsNullOrEmpty(DeviceJID))
			{
				Xml.Append("' jid='");
				Xml.Append(XML.Encode(DeviceJID));
				Xml.Append('\'');
			}

			this.AppendNodeInfo(Xml, NodeId, SourceId, Partition);
			Xml.Append("/>");

			this.client.SendIqSet(this.provisioningServerAddress, Xml.ToString(), Callback, State);
		}

		#endregion

	}
}
