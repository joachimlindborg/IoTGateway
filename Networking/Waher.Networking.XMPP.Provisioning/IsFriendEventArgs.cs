﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Content.Xml;

namespace Waher.Networking.XMPP.Provisioning
{
	/// <summary>
	/// Range of a rule change
	/// </summary>
	public enum RuleRange
	{
		/// <summary>
		/// Applies to caller only.
		/// </summary>
		Caller,

		/// <summary>
		/// Applies to the caller domain.
		/// </summary>
		Domain,

		/// <summary>
		/// Appplies to all future requests.
		/// </summary>
		All
	}

	/// <summary>
	/// Delegate for IsFriend callback methods.
	/// </summary>
	/// <param name="Sender">Sender</param>
	/// <param name="e">Event arguments.</param>
	public delegate void IsFriendEventHandler(object Sender, IsFriendEventArgs e);

	/// <summary>
	/// Event arguments for IsFriend events.
	/// </summary>
	public class IsFriendEventArgs : QuestionEventArgs
	{
		/// <summary>
		/// Event arguments for IsFriend events.
		/// </summary>
		/// <param name="Client">Provisioning Client used.</param>
		/// <param name="e">Message with request.</param>
		public IsFriendEventArgs(ProvisioningClient Client, MessageEventArgs e)
			: base(Client, e)
		{
		}

		/// <summary>
		/// Accepts the friendship.
		/// </summary>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		public void Accept(IqResultEventHandler Callback, object State)
		{
			this.Respond(true, RuleRange.Caller, Callback, State);
		}

		/// <summary>
		/// Rejects the friendship.
		/// </summary>
		public void Reject(IqResultEventHandler Callback, object State)
		{
			this.Respond(false, RuleRange.Caller, Callback, State);
		}

		/// <summary>
		/// Accepts the friendship, and similar future requests from the entire remote domain.
		/// </summary>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		public void AcceptForEntireDomain(IqResultEventHandler Callback, object State)
		{
			this.Respond(true, RuleRange.Domain, Callback, State);
		}

		/// <summary>
		/// Rejects the friendship, and similar future requests from the entire remote domain.
		/// </summary>
		public void RejectForEntireDomain(IqResultEventHandler Callback, object State)
		{
			this.Respond(false, RuleRange.Domain, Callback, State);
		}

		/// <summary>
		/// Accepts the friendship, and all future requests.
		/// </summary>
		/// <param name="Callback">Callback method to call when response is received.</param>
		/// <param name="State">State object to pass on to the callback method.</param>
		public void AcceptForAll(IqResultEventHandler Callback, object State)
		{
			this.Respond(true, RuleRange.All, Callback, State);
		}

		/// <summary>
		/// Rejects the friendship, and all future requests.
		/// </summary>
		public void RejectForAll(IqResultEventHandler Callback, object State)
		{
			this.Respond(false, RuleRange.All, Callback, State);
		}

		private void Respond(bool IsFriend, RuleRange Range, IqResultEventHandler Callback, object State)
		{
			this.Client.IsFriendResponse(this.JID, this.RemoteJID, this.Key, IsFriend, Range, Callback, State);
		}

	}
}
