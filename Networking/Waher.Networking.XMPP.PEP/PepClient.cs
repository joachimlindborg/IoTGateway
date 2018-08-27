﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Waher.Events;
using Waher.Networking.XMPP.PubSub;
using Waher.Runtime.Inventory;

namespace Waher.Networking.XMPP.PEP
{
	/// <summary>
	/// Client managing the Personal Eventing Protocol (XEP-0163).
	/// https://xmpp.org/extensions/xep-0163.html
	/// </summary>
	public class PepClient : XmppExtension
	{
		private static Dictionary<string, IPersonalEvent> personalEventTypes = GetPersonalEventTypes();

		private PubSubClient pubSubClient;
		private readonly string pubSubComponentAddress;
		private readonly Dictionary<Type, PersonalEventNotificationEventHandler[]> handlers = new Dictionary<Type, PersonalEventNotificationEventHandler[]>();
		private readonly bool hasPubSubComponent;

		/// <summary>
		/// Client managing the Personal Eventing Protocol (XEP-0163).
		/// https://xmpp.org/extensions/xep-0163.html
		/// </summary>
		/// <param name="Client">XMPP Client to use.</param>
		public PepClient(XmppClient Client)
			: this(Client, string.Empty)
		{
		}

		/// <summary>
		/// Client managing the Personal Eventing Protocol (XEP-0163).
		/// https://xmpp.org/extensions/xep-0163.html
		/// </summary>
		/// <param name="Client">XMPP Client to use.</param>
		/// <param name="PubSubComponentAddress">Default Publish/Subscribe component address.</param>
		public PepClient(XmppClient Client, string PubSubComponentAddress)
			: base(Client)
		{
			this.pubSubComponentAddress = PubSubComponentAddress;
			this.pubSubClient = new PubSubClient(Client, this.pubSubComponentAddress);
			this.hasPubSubComponent = !string.IsNullOrEmpty(this.pubSubComponentAddress);

			this.pubSubClient.ItemNotification += PubSubClient_ItemNotification;
			this.PubSubClient.ItemRetracted += PubSubClient_ItemRetracted;
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public override void Dispose()
		{
			if (this.pubSubClient != null)
			{
				this.pubSubClient.ItemNotification -= PubSubClient_ItemNotification;
				this.PubSubClient.ItemRetracted -= PubSubClient_ItemRetracted;

				this.pubSubClient.Dispose();
				this.pubSubClient = null;
			}

			LinkedList<IPersonalEvent> ToUnregister = new LinkedList<IPersonalEvent>();

			lock (this.handlers)
			{
				foreach (Type T in this.handlers.Keys)
				{
					try
					{
						ToUnregister.AddLast((IPersonalEvent)Activator.CreateInstance(T));
					}
					catch (Exception ex)
					{
						Log.Critical(ex);
					}
				}

				this.handlers.Clear();
			}

			foreach (IPersonalEvent PersonalEvent in ToUnregister)
				this.client.UnregisterFeature(PersonalEvent.Namespace + "+notify");

			base.Dispose();
		}

		/// <summary>
		/// <see cref="PubSubClient"/> used for the Personal Eventing Protocol. Use this client to perform administrative tasks
		/// of the PEP service.
		/// </summary>
		public PubSubClient PubSubClient
		{
			get { return this.pubSubClient; }
		}

		/// <summary>
		/// Implemented extensions.
		/// </summary>
		public override string[] Extensions => new string[] { "XEP-0163" };

		#region Personal Eventing Protocol (XEP-0163)

		/// <summary>
		/// Publishes an item on a node.
		/// </summary>
		/// <param name="Node">Node name.</param>
		/// <param name="Callback">Method to call when operation completes.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void Publish(string Node, ItemResultEventHandler Callback, object State)
		{
			this.pubSubClient?.Publish(string.Empty, Node, string.Empty, Callback, State);
		}

		/// <summary>
		/// Publishes an item on a node.
		/// </summary>
		/// <param name="Node">Node name.</param>
		/// <param name="PayloadXml">Payload XML.</param>
		/// <param name="Callback">Method to call when operation completes.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void Publish(string Node, string PayloadXml, ItemResultEventHandler Callback, object State)
		{
			this.pubSubClient?.Publish(string.Empty, Node, string.Empty, PayloadXml, Callback, State);
		}

		/// <summary>
		/// Publishes an item on a node.
		/// </summary>
		/// <param name="PersonalEvent">Personal event.</param>
		/// <param name="Callback">Method to call when operation completes.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void Publish(IPersonalEvent PersonalEvent, ItemResultEventHandler Callback, object State)
		{
			string ItemId = PersonalEvent.ItemId;

			if (ItemId == null)
				this.pubSubClient?.Publish(string.Empty, PersonalEvent.Node, string.Empty, PersonalEvent.PayloadXml, Callback, State);
			else
				this.pubSubClient?.Publish(string.Empty, PersonalEvent.Node, ItemId, PersonalEvent.PayloadXml, Callback, State);
		}

		/// <summary>
		/// Publishes an item on a node.
		/// </summary>
		/// <param name="Node">Node name.</param>
		/// <param name="ItemId">Item identity, if available. If used, and an existing item
		/// is available with that identity, it will be updated with the new content.</param>
		/// <param name="PayloadXml">Payload XML.</param>
		/// <param name="Callback">Method to call when operation completes.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void Publish(string Node, string ItemId, string PayloadXml, ItemResultEventHandler Callback, object State)
		{
			this.pubSubClient?.Publish(string.Empty, Node, ItemId, PayloadXml, Callback, State);
		}

		private void PubSubClient_ItemNotification(object Sender, ItemNotificationEventArgs e)
		{
			if (this.hasPubSubComponent && e.From.IndexOf('@') < 0)
			{
				try
				{
					this.NonPepItemNotification?.Invoke(this, e);
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
			else
			{
				if (string.Compare(e.FromBareJID, this.client.BareJID, true) != 0)
				{
					RosterItem Item = this.client[e.FromBareJID];
					if (Item == null || (Item.State != SubscriptionState.Both && Item.State != SubscriptionState.To))
						return;
				}

				IPersonalEvent PersonalEvent = null;

				foreach (XmlNode N in e.Item.ChildNodes)
				{
					if (N is XmlElement E && personalEventTypes.TryGetValue(E.LocalName + " " + E.NamespaceURI, out IPersonalEvent PersonalEvent2))
					{
						PersonalEvent = PersonalEvent2.Parse(E);
						break;
					}
				}

				if (PersonalEvent != null)
				{
					PersonalEventNotificationEventHandler[] Handlers;

					lock (this.handlers)
					{
						if (!this.handlers.TryGetValue(PersonalEvent.GetType(), out Handlers))
							return;
					}

					PersonalEventNotificationEventArgs e2 = new PersonalEventNotificationEventArgs(PersonalEvent, this, e);

					foreach (PersonalEventNotificationEventHandler Handler in Handlers)
					{
						try
						{
							Handler.Invoke(this, e2);
						}
						catch (Exception ex)
						{
							Log.Critical(ex);
						}
					}
				}
			}
		}

		private void PubSubClient_ItemRetracted(object Sender, ItemNotificationEventArgs e)
		{
			if (this.hasPubSubComponent && e.From.IndexOf('@') < 0)
			{
				try
				{
					this.NonPepItemRetraction?.Invoke(this, e);
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
		}

		/// <summary>
		/// Event raised when an item notification from the publish/subscribe component that is not related to PEP has been received.
		/// </summary>
		public event ItemNotificationEventHandler NonPepItemNotification = null;

		/// <summary>
		/// Event raised when an item retraction from the publish/subscribe component that is not related to PEP has been received.
		/// </summary>
		public event ItemNotificationEventHandler NonPepItemRetraction = null;

		private static Dictionary<string, IPersonalEvent> GetPersonalEventTypes()
		{
			Types.OnInvalidated += Types_OnInvalidated;
			return GetPersonalEventTypes2();
		}

		private static Dictionary<string, IPersonalEvent> GetPersonalEventTypes2()
		{
			Dictionary<string, IPersonalEvent> Result = new Dictionary<string, IPersonalEvent>();

			foreach (Type T in Types.GetTypesImplementingInterface(typeof(IPersonalEvent)))
			{
				if (T.GetTypeInfo().IsAbstract)
					continue;

				try
				{
					IPersonalEvent PersonalEvent = (IPersonalEvent)Activator.CreateInstance(T);
					Result[PersonalEvent.LocalName + " " + PersonalEvent.Namespace] = PersonalEvent;
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}

			return Result;
		}

		private static void Types_OnInvalidated(object sender, EventArgs e)
		{
			personalEventTypes = GetPersonalEventTypes2();
		}

		/// <summary>
		/// Registers an event handler of a specific type of personal events.
		/// </summary>
		/// <param name="PersonalEventType">Type of personal event.</param>
		/// <param name="Handler">Event handler.</param>
		public void RegisterHandler(Type PersonalEventType, PersonalEventNotificationEventHandler Handler)
		{
			if (!typeof(IPersonalEvent).GetTypeInfo().IsAssignableFrom(PersonalEventType.GetTypeInfo()))
				throw new ArgumentException("Not a personal event type.", nameof(PersonalEventType));

			IPersonalEvent PersonalEvent = (IPersonalEvent)Activator.CreateInstance(PersonalEventType);

			lock (this.handlers)
			{
				if (!this.handlers.TryGetValue(PersonalEventType, out PersonalEventNotificationEventHandler[] Handlers))
					Handlers = null;

				if (Handlers == null)
					Handlers = new PersonalEventNotificationEventHandler[] { Handler };
				else
				{
					int c = Handlers.Length;
					PersonalEventNotificationEventHandler[] Handlers2 = new PersonalEventNotificationEventHandler[c + 1];
					Array.Copy(Handlers, 0, Handlers2, 0, c);
					Handlers2[c] = Handler;
					Handlers = Handlers2;
				}

				this.handlers[PersonalEventType] = Handlers;
			}

			this.client.RegisterFeature(PersonalEvent.Namespace + "+notify");
		}

		/// <summary>
		/// Unregisters an event handler of a specific type of personal events.
		/// </summary>
		/// <param name="PersonalEventType">Type of personal event.</param>
		/// <param name="Handler">Event handler.</param>
		/// <returns>If the event handler was found and removed.</returns>
		public bool UnregisterHandler(Type PersonalEventType, PersonalEventNotificationEventHandler Handler)
		{
			lock (this.handlers)
			{
				if (!this.handlers.TryGetValue(PersonalEventType, out PersonalEventNotificationEventHandler[] Handlers))
					return false;

				List<PersonalEventNotificationEventHandler> List = new List<PersonalEventNotificationEventHandler>();
				List.AddRange(Handlers);

				if (!List.Remove(Handler))
					return false;

				if (List.Count == 0)
					this.handlers.Remove(PersonalEventType);
				else
				{
					Handlers = List.ToArray();
					this.handlers[PersonalEventType] = Handlers;
				}

				this.client.UnregisterFeature(PersonalEventType.Namespace + "+notify");

				return true;
			}
		}

		#endregion

		#region XEP-0080: User Location

		/// <summary>
		/// Publishes a personal user location.
		/// </summary>
		/// <param name="Location">User location</param>
		public void Publish(UserLocation Location)
		{
			this.Publish(Location, null, null);
		}

		/// <summary>
		/// Event raised when a user location personal event has been received.
		/// </summary>
		public event UserLocationEventHandler OnUserLocation
		{
			add
			{
				if (this.onUserLocation == null)
					this.RegisterHandler(typeof(UserLocation), this.UserLocationEventHandler);

				this.onUserLocation += value;
			}

			remove
			{
				this.onUserLocation -= value;

				if (this.onUserLocation == null)
					this.UnregisterHandler(typeof(UserLocation), this.UserLocationEventHandler);
			}
		}

		private UserLocationEventHandler onUserLocation = null;

		private void UserLocationEventHandler(object Sender, PersonalEventNotificationEventArgs e)
		{
			if (e.PersonalEvent is UserLocation UserLocation)
			{
				try
				{
					this.onUserLocation?.Invoke(this, new UserLocationEventArguments(UserLocation, e));
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
		}

		#endregion

		#region XEP-0084: User Avatar

		/// <summary>
		/// Publishes a personal user avatar.
		/// </summary>
		/// <param name="Images">Images of different types, representing the same avatar.</param>
		public void Publish(params UserAvatarImage[] Images)
		{
			List<UserAvatarReference> References = new List<UserAvatarReference>();

			foreach (UserAvatarImage Image in Images)
			{
				UserAvatarData Data = new UserAvatarData()
				{
					Data = Image.Data
				};

				this.Publish(Data, null, null);

				References.Add(new UserAvatarReference()
				{
					Bytes = Image.Data.Length,
					Height = Image.Height,
					Id = Data.ItemId,
					Type = Image.ContentType,
					URL = Image.URL,
					Width = Image.Width
				});
			}

			this.Publish(new UserAvatarMetaData(References.ToArray()), null, null);
		}

		/// <summary>
		/// Event raised when a user location personal event has been received.
		/// </summary>
		public event UserAvatarMetaDataEventHandler OnUserAvatarMetaData
		{
			add
			{
				if (this.onUserAvatarMetaData == null)
					this.RegisterHandler(typeof(UserAvatarMetaData), this.UserAvatarMetaDataEventHandler);

				this.onUserAvatarMetaData += value;
			}

			remove
			{
				this.onUserAvatarMetaData -= value;

				if (this.onUserAvatarMetaData == null)
					this.UnregisterHandler(typeof(UserAvatarMetaData), this.UserAvatarMetaDataEventHandler);
			}
		}

		private UserAvatarMetaDataEventHandler onUserAvatarMetaData = null;

		private void UserAvatarMetaDataEventHandler(object Sender, PersonalEventNotificationEventArgs e)
		{
			if (e.PersonalEvent is UserAvatarMetaData UserAvatarMetaData)
			{
				try
				{
					this.onUserAvatarMetaData?.Invoke(this, new UserAvatarMetaDataEventArguments(UserAvatarMetaData, e));
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
		}

		/// <summary>
		/// Gets an avatar published by a user using the Personal Eventing Protocol
		/// </summary>
		/// <param name="UserBareJid">Bare JID of user publishing the avatar.</param>
		/// <param name="Reference">Avatar reference, selected from	an <see cref="UserAvatarMetaData"/> event.</param>
		/// <param name="Callback">Method to call when avatar has been retrieved.</param>
		/// <param name="State">State object to pass on to callback method.</param>
		public void GetUserAvatarData(string UserBareJid, UserAvatarReference Reference, UserAvatarImageEventHandler Callback, object State)
		{
			this.pubSubClient.GetItems(UserBareJid, UserAvatarData.AvatarDataNamespace, new string[] { Reference.Id }, (sender, e) =>
			{
				UserAvatarImage Image = null;

				if (e.Ok)
				{
					foreach (PubSubItem Item in e.Items)
					{
						if (Item.ItemId == Reference.Id)
						{
							foreach (XmlNode N in Item.Item)
							{
								if (N is XmlElement E && E.LocalName == "data" && E.NamespaceURI == UserAvatarData.AvatarDataNamespace)
								{
									Image = new UserAvatarImage()
									{
										Data = Convert.FromBase64String(E.InnerText),
										ContentType = Reference.Type,
										Height = Reference.Height,
										URL = Reference.URL,
										Width = Reference.Width
									};

									break;
								}
							}
						}
					}
				}

				Callback?.Invoke(this, new UserAvatarImageEventArguments(Image, e));

			}, State);
		}

		#endregion

		#region XEP-0107: User Mood

		/// <summary>
		/// Publishes a personal user mood.
		/// </summary>
		/// <param name="Mood">Mood</param>
		/// <param name="Text">Custom</param>
		public void Publish(UserMoods Mood, string Text)
		{
			this.Publish(new UserMood()
			{
				Mood = Mood,
				Text = Text
			}, null, null);
		}

		/// <summary>
		/// Event raised when a user mood personal event has been received.
		/// </summary>
		public event UserMoodEventHandler OnUserMood
		{
			add
			{
				if (this.onUserMood == null)
					this.RegisterHandler(typeof(UserMood), this.UserMoodEventHandler);

				this.onUserMood += value;
			}

			remove
			{
				this.onUserMood -= value;

				if (this.onUserMood == null)
					this.UnregisterHandler(typeof(UserMood), this.UserMoodEventHandler);
			}
		}

		private UserMoodEventHandler onUserMood = null;

		private void UserMoodEventHandler(object Sender, PersonalEventNotificationEventArgs e)
		{
			if (e.PersonalEvent is UserMood UserMood)
			{
				try
				{
					this.onUserMood?.Invoke(this, new UserMoodEventArguments(UserMood, e));
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
		}

		#endregion

		#region XEP-0108: User Activity

		/// <summary>
		/// Publishes a personal user activity.
		/// </summary>
		/// <param name="GeneralActivity">General activity</param>
		/// <param name="SpecificActivity">Specific activity</param>
		/// <param name="Text">Custom</param>
		public void Publish(UserGeneralActivities GeneralActivity, UserSpecificActivities SpecificActivity, string Text)
		{
			this.Publish(new UserActivity()
			{
				GeneralActivity = GeneralActivity,
				SpecificActivity = SpecificActivity,
				Text = Text
			}, null, null);
		}

		/// <summary>
		/// Event raised when a user activity personal event has been received.
		/// </summary>
		public event UserActivityEventHandler OnUserActivity
		{
			add
			{
				if (this.onUserActivity == null)
					this.RegisterHandler(typeof(UserActivity), this.UserActivityEventHandler);

				this.onUserActivity += value;
			}

			remove
			{
				this.onUserActivity -= value;

				if (this.onUserActivity == null)
					this.UnregisterHandler(typeof(UserActivity), this.UserActivityEventHandler);
			}
		}

		private UserActivityEventHandler onUserActivity = null;

		private void UserActivityEventHandler(object Sender, PersonalEventNotificationEventArgs e)
		{
			if (e.PersonalEvent is UserActivity UserActivity)
			{
				try
				{
					this.onUserActivity?.Invoke(this, new UserActivityEventArguments(UserActivity, e));
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
		}

		#endregion

		#region XEP-0118: User Tune

		/// <summary>
		/// Publishes a personal user activity.
		/// </summary>
		/// <param name="Tune">User tune</param>
		public void Publish(UserTune Tune)
		{
			this.Publish(Tune, null, null);
		}

		/// <summary>
		/// Event raised when a user tune personal event has been received.
		/// </summary>
		public event UserTuneEventHandler OnUserTune
		{
			add
			{
				if (this.onUserTune == null)
					this.RegisterHandler(typeof(UserTune), this.UserTuneEventHandler);

				this.onUserTune += value;
			}

			remove
			{
				this.onUserTune -= value;

				if (this.onUserTune == null)
					this.UnregisterHandler(typeof(UserTune), this.UserTuneEventHandler);
			}
		}

		private UserTuneEventHandler onUserTune = null;

		private void UserTuneEventHandler(object Sender, PersonalEventNotificationEventArgs e)
		{
			if (e.PersonalEvent is UserTune UserTune)
			{
				try
				{
					this.onUserTune?.Invoke(this, new UserTuneEventArguments(UserTune, e));
				}
				catch (Exception ex)
				{
					Log.Critical(ex);
				}
			}
		}

		#endregion

	}
}
