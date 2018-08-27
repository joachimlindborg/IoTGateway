﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Things.SensorData;

namespace Waher.Networking.XMPP.Provisioning
{
	/// <summary>
	/// Delegate for CanRead callback methods.
	/// </summary>
	/// <param name="Sender">Sender</param>
	/// <param name="e">Event arguments.</param>
	public delegate void CanReadEventHandler(object Sender, CanReadEventArgs e);

	/// <summary>
	/// Event arguments for CanRead events.
	/// </summary>
	public class CanReadEventArgs : NodeQuestionEventArgs
	{
		private ProvisioningClient provisioningClient;
		private FieldType fieldTypes;
		private string[] fields;

		/// <summary>
		/// Event arguments for CanRead events.
		/// </summary>
		/// <param name="ProvisioningClient">XMPP Provisioning Client used.</param>
		/// <param name="e">Message with request.</param>
		public CanReadEventArgs(ProvisioningClient ProvisioningClient, MessageEventArgs e)
			: base(ProvisioningClient, e)
		{
			this.provisioningClient = ProvisioningClient;
			this.fieldTypes = (FieldType)0;

			foreach (XmlAttribute Attr in e.Content.Attributes)
			{
				switch (Attr.LocalName)
				{
					case "all":
						if (CommonTypes.TryParse(Attr.Value, out bool b))
							this.fieldTypes |= FieldType.All;
						break;

					case "m":
						if (CommonTypes.TryParse(Attr.Value, out b))
							this.fieldTypes |= FieldType.Momentary;
						break;

					case "p":
						if (CommonTypes.TryParse(Attr.Value, out b))
							this.fieldTypes |= FieldType.Peak;
						break;

					case "s":
						if (CommonTypes.TryParse(Attr.Value, out b))
							this.fieldTypes |= FieldType.Status;
						break;

					case "c":
						if (CommonTypes.TryParse(Attr.Value, out b))
							this.fieldTypes |= FieldType.Computed;
						break;

					case "i":
						if (CommonTypes.TryParse(Attr.Value, out b))
							this.fieldTypes |= FieldType.Identity;
						break;

					case "h":
						if (CommonTypes.TryParse(Attr.Value, out b))
							this.fieldTypes |= FieldType.Historical;
						break;
				}
			}

			List<string> Fields = null;

			foreach (XmlNode N in e.Content.ChildNodes)
			{
				if (N is XmlElement E && E.LocalName == "f")
				{
					if (Fields == null)
						Fields = new List<string>();

					Fields.Add(XML.Attribute(E, "n"));
				}
			}

			if (Fields != null)
				this.fields = Fields.ToArray();
			else
				this.fields = null;
		}

		/// <summary>
		/// Any field specifications of the original request. If null, all fields are requested.
		/// </summary>
		public string[] Fields
		{
			get { return this.fields; }
		}

		/// <summary>
		/// Any field types available in the original request.
		/// </summary>
		public FieldType FieldTypes
		{
			get { return this.fieldTypes; }
		}

		/// <summary>
		/// Accept readouts from this remote JID.
		/// </summary>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool AcceptAllFromJID(IqResultEventHandler Callback, object State)
		{
			return this.Respond(true, "<fromJid/>", Callback, State);
		}

		/// <summary>
		/// Accept readouts from all entities of the remote domain.
		/// </summary>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool AcceptAllFromDomain(IqResultEventHandler Callback, object State)
		{
			return this.Respond(true, "<fromDomain/>", Callback, State);
		}

		/// <summary>
		/// Accept readouts from this service.
		/// </summary>
		/// <param name="ServiceToken">Which service token is to be accepted.</param>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool AcceptAllFromService(string ServiceToken, IqResultEventHandler Callback, object State)
		{
			return this.Respond(true, "<fromService token='" +XML.Encode(ServiceToken) + "'/>", Callback, State);
		}

		/// <summary>
		/// Accept readouts from this device.
		/// </summary>
		/// <param name="DeviceToken">Which device token is to be accepted.</param>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool AcceptAllFromDevice(string DeviceToken, IqResultEventHandler Callback, object State)
		{
			return this.Respond(true, "<fromDevice token='" + XML.Encode(DeviceToken) + "'/>", Callback, State);
		}

		/// <summary>
		/// Accept readouts from this user.
		/// </summary>
		/// <param name="UserToken">Which user token is to be accepted.</param>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool AcceptAllFromUser(string UserToken, IqResultEventHandler Callback, object State)
		{
			return this.Respond(true, "<fromUser token='" + XML.Encode(UserToken) + "'/>", Callback, State);
		}

		/// <summary>
		/// Accept partial readouts from this remote JID.
		/// </summary>
		/// <param name="Fields">Fields that can be read.</param>
		/// <param name="FieldTypes">Field categories that can be read.</param>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool AcceptPartialFromJID(string[] Fields, FieldType FieldTypes, IqResultEventHandler Callback, object State)
		{
			return this.Respond(true, this.PartialFields(Fields, FieldTypes) + "<fromJid/>", Callback, State);
		}

		/// <summary>
		/// Accept partial readouts from all entities of the remote domain.
		/// </summary>
		/// <param name="Fields">Fields that can be read.</param>
		/// <param name="FieldTypes">Field categories that can be read.</param>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool AcceptPartialFromDomain(string[] Fields, FieldType FieldTypes, IqResultEventHandler Callback, object State)
		{
			return this.Respond(true, this.PartialFields(Fields, FieldTypes) + "<fromDomain/>", Callback, State);
		}

		/// <summary>
		/// Accept partial readouts from this service.
		/// </summary>
		/// <param name="Fields">Fields that can be read.</param>
		/// <param name="FieldTypes">Field categories that can be read.</param>
		/// <param name="ServiceToken">Which service token is to be accepted.</param>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool AcceptPartialFromService(string[] Fields, FieldType FieldTypes, string ServiceToken, IqResultEventHandler Callback, object State)
		{
			return this.Respond(true, this.PartialFields(Fields, FieldTypes) + "<fromService token='" + XML.Encode(ServiceToken) + "'/>", Callback, State);
		}

		/// <summary>
		/// Accept partial readouts from this device.
		/// </summary>
		/// <param name="Fields">Fields that can be read.</param>
		/// <param name="FieldTypes">Field categories that can be read.</param>
		/// <param name="DeviceToken">Which device token is to be accepted.</param>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool AcceptPartialFromDevice(string[] Fields, FieldType FieldTypes, string DeviceToken, IqResultEventHandler Callback, object State)
		{
			return this.Respond(true, this.PartialFields(Fields, FieldTypes) + "<fromDevice token='" + XML.Encode(DeviceToken) + "'/>", Callback, State);
		}

		/// <summary>
		/// Accept partial readouts from this user.
		/// </summary>
		/// <param name="Fields">Fields that can be read.</param>
		/// <param name="FieldTypes">Field categories that can be read.</param>
		/// <param name="UserToken">Which user token is to be accepted.</param>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool AcceptPartialFromUser(string[] Fields, FieldType FieldTypes, string UserToken, IqResultEventHandler Callback, object State)
		{
			return this.Respond(true, this.PartialFields(Fields, FieldTypes) + "<fromUser token='" + XML.Encode(UserToken) + "'/>", Callback, State);
		}

		private string PartialFields(string[] Names, FieldType FieldTypes)
		{
			StringBuilder Xml = new StringBuilder();

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

			Xml.Append(">");

			foreach (string Name in Names)
			{
				Xml.Append("<f n='");
				Xml.Append(XML.Encode(Name));
				Xml.Append("</f>");
			}

			Xml.Append("</partial>");

			return Xml.ToString();
		}

		/// <summary>
		/// Reject readouts from this remote JID.
		/// </summary>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool RejectAllFromJID(IqResultEventHandler Callback, object State)
		{
			return this.Respond(false, "<fromJid/>", Callback, State);
		}

		/// <summary>
		/// Reject readouts from all entities of the remote domain.
		/// </summary>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool RejectAllFromDomain(IqResultEventHandler Callback, object State)
		{
			return this.Respond(false, "<fromDomain/>", Callback, State);
		}

		/// <summary>
		/// Reject readouts from this service.
		/// </summary>
		/// <param name="ServiceToken">Which service token is to be rejected.</param>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool RejectAllFromService(string ServiceToken, IqResultEventHandler Callback, object State)
		{
			return this.Respond(false, "<fromService token='" + XML.Encode(ServiceToken) + "'/>", Callback, State);
		}

		/// <summary>
		/// Reject readouts from this device.
		/// </summary>
		/// <param name="DeviceToken">Which device token is to be rejected.</param>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool RejectAllFromDevice(string DeviceToken, IqResultEventHandler Callback, object State)
		{
			return this.Respond(false, "<fromDevice token='" + XML.Encode(DeviceToken) + "'/>", Callback, State);
		}

		/// <summary>
		/// Reject readouts from this user.
		/// </summary>
		/// <param name="UserToken">Which user token is to be rejected.</param>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		/// <returns>If the response could be sent.</returns>
		public bool RejectAllFromUser(string UserToken, IqResultEventHandler Callback, object State)
		{
			return this.Respond(false, "<fromUser token='" + XML.Encode(UserToken) + "'/>", Callback, State);
		}

		private bool Respond(bool CanRead, string RuleXml, IqResultEventHandler Callback, object State)
		{
			StringBuilder Xml = new StringBuilder();

			Xml.Append("<canReadRule xmlns='");
			Xml.Append(ProvisioningClient.NamespaceProvisioningOwner);
			Xml.Append("' jid='");
			Xml.Append(XML.Encode(this.JID));
			Xml.Append("' remoteJid='");
			Xml.Append(XML.Encode(this.RemoteJID));
			Xml.Append("' key='");
			Xml.Append(XML.Encode(this.Key));
			Xml.Append("' result='");
			Xml.Append(CommonTypes.Encode(CanRead));
			Xml.Append("'>");
			Xml.Append(RuleXml);
			Xml.Append("</canRaedRule>");

			RosterItem Item = this.Client.Client[this.FromBareJID];
			if (Item.HasLastPresence && Item.LastPresence.IsOnline)
			{
				this.Client.Client.SendIqSet(Item.LastPresenceFullJid, Xml.ToString(), Callback, State);
				return true;
			}
			else
				return false;
		}

	}
}
