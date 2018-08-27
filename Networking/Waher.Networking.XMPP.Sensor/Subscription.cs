﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Things;
using Waher.Things.SensorData;

namespace Waher.Networking.XMPP.Sensor
{
	/// <summary>
	/// Maintains the status of a subscription.
	/// </summary>
	public class Subscription
	{
		private Dictionary<IThingReference, bool> nodes = new Dictionary<IThingReference, bool>();
		private Dictionary<string, FieldSubscriptionRule> fields = null;
		private Availability availability = Availability.Online;
		private FieldType fieldTypes;
		private Duration maxAge;
		private Duration minInterval;
		private Duration maxInterval;
		private DateTime lastTrigger = DateTime.Now;
		private string from;
		private string serviceToken;
		private string deviceToken;
		private string userToken;
		private string id;
		private bool active = true;
		private bool supressedTrigger = false;

		/// <summary>
		/// Maintains the status of a subscription.
		/// </summary>
		/// <param name="Id">Request identity.</param>
		/// <param name="From">Subscription made by this JID.</param>
		/// <param name="Nodes">Nodes involved in subscription.</param>
		/// <param name="Fields">Optional field rules.</param>
		/// <param name="FieldTypes">Field types involved in subscription.</param>
		/// <param name="MaxAge">Maximum age of historical data.</param>
		/// <param name="MinInterval">Smallest interval for reporting events. Events are not reported more often than this limit.</param>
		/// <param name="MaxInterval">Largest interval for reporting events. Events are not reported less often than this limit.</param>
		/// <param name="ServiceToken">Service Token.</param>
		/// <param name="DeviceToken">Device Token.</param>
		/// <param name="UserToken">User Token.</param>
		public Subscription(string Id, string From, IThingReference[] Nodes, Dictionary<string, FieldSubscriptionRule> Fields,
			FieldType FieldTypes, Duration MaxAge, Duration MinInterval, Duration MaxInterval, string ServiceToken, string DeviceToken,
			string UserToken)
		{
			this.nodes = new Dictionary<IThingReference, bool>();

			foreach (IThingReference Ref in Nodes)
				this.nodes[Ref] = true;

			this.fields = Fields;
			this.fieldTypes = FieldTypes;
			this.maxAge = MaxAge;
			this.minInterval = MinInterval;
			this.maxInterval = MaxInterval;
			this.from = From;
			this.id = Id;
			this.serviceToken = ServiceToken;
			this.deviceToken = DeviceToken;
			this.userToken = UserToken;
		}

		/// <summary>
		/// Removes a node reference from the subscription.
		/// </summary>
		/// <param name="Reference">Reference to remove.</param>
		/// <returns>If the subscription has become inactive, due to lack of referenced things.</returns>
		public bool RemoveNode(IThingReference Reference)
		{
			lock (this.nodes)
			{
				if (this.nodes.Remove(Reference))
				{
					if (this.nodes.Count == 0)
					{
						this.active = false;
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Nodes in subscription.
		/// </summary>
		public Dictionary<IThingReference, bool>.KeyCollection Nodes
		{
			get { return this.nodes.Keys; }
		}

		/// <summary>
		/// Fields in subscription.
		/// </summary>
		public string[] FieldNames
		{
			get
			{
				if (this.fields == null)
					return new string[0];
				else
				{
					string[] Result = new string[this.fields.Count];
					this.fields.Keys.CopyTo(Result, 0);
					return Result;
				}
			}
		}

		/// <summary>
		/// Node references, for use when requesting a new readout.
		/// </summary>
		public IThingReference[] NodeReferences
		{
			get
			{
				if (this.nodes.Count == 1 && this.nodes.ContainsKey(ThingReference.Empty))
					return null;

				IThingReference[] Result = new IThingReference[this.nodes.Count];
				this.nodes.Keys.CopyTo(Result, 0);
				return Result;
			}
		}

		/// <summary>
		/// Field types involved in subscription.
		/// </summary>
		public FieldType FieldTypes { get { return this.fieldTypes; } }

		/// <summary>
		/// Maximum age of historical data.
		/// </summary>
		public Duration MaxAge { get { return this.maxAge; } }

		/// <summary>
		/// Smallest interval for reporting events. Events are not reported more often than this limit.
		/// </summary>
		public Duration MinInterval { get { return this.minInterval; } }

		/// <summary>
		/// Largest interval for reporting events. Events are not reported less often than this limit.
		/// </summary>
		public Duration MaxInterval { get { return this.maxInterval; } }

		/// <summary>
		/// Subscription made by this JID.
		/// </summary>
		public string From { get { return this.from; } }

		/// <summary>
		/// Request identity.
		/// </summary>
		public string Id { get { return this.id; } }

		/// <summary>
		/// If the subscription is still active.
		/// </summary>
		public bool Active
		{
			get { return this.active; }
			internal set { this.active = value; }
		}

		/// <summary>
		/// Timestamp of when subscription was Last triggered.
		/// </summary>
		public DateTime LastTrigger
		{
			get { return this.lastTrigger; }
			internal set { this.lastTrigger = value; }
		}

		/// <summary>
		/// Availability of the subscriber.
		/// </summary>
		public Availability Availability
		{
			get { return this.availability; }
			internal set { this.availability = value; }
		}

		/// <summary>
		/// Service Token.
		/// </summary>
		public string ServiceToken { get { return this.serviceToken; } }

		/// <summary>
		/// Device Token.
		/// </summary>
		public string DeviceToken { get { return this.deviceToken; } }

		/// <summary>
		/// User Token.
		/// </summary>
		public string UserToken { get { return this.userToken; } }

		/// <summary>
		/// If the subscription has a supressed trigger event, that has not been sent, due to limitations defined by the smallest allowed
		/// event interval, or if the subscriber is offline.
		/// </summary>
		public bool SupressedTrigger
		{
			get { return this.supressedTrigger; }
			internal set { this.supressedTrigger = value; }
		}

		/// <summary>
		/// Checks if the new values triggers a new event.
		/// </summary>
		/// <param name="Values">New momentary vaules.</param>
		/// <returns>If the values triggers a new event.</returns>
		public bool IsTriggered(IEnumerable<Field> Values)
		{
			if (!this.active)
				return false;

			bool Trigger = false;

			if (this.fields != null)
			{
				foreach (Field Field in Values)
				{
					if (!this.fields.TryGetValue(Field.Name, out FieldSubscriptionRule Rule))
						continue;

					if (Rule.TriggerEvent(Field.ReferenceValue))
						Trigger = true;
				}
			}

			DateTime Now = DateTime.Now;
			DateTime TP;

			if (Trigger && this.minInterval != null && this.lastTrigger + this.minInterval > Now)
			{
				this.supressedTrigger = true;
				return false;
			}

			if (this.maxInterval != null && (TP = this.lastTrigger + this.maxInterval) <= Now)
			{
				this.lastTrigger = TP;
				return true;
			}

			if (!Trigger)
				return false;

			this.lastTrigger = Now;
			return true;
		}

	}
}
