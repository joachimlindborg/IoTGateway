﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Events;

namespace Waher.Runtime.Timing
{
	/// <summary>
	/// Contains information about a scheduled event.
	/// </summary>
	internal class ScheduledEvent
	{
		private readonly DateTime when;
		private readonly ScheduledEventCallback eventMethod;
		private readonly object state;

		/// <summary>
		/// Contains information about a scheduled event.
		/// </summary>
		/// <param name="When">When an event is to be executed.</param>
		/// <param name="EventMethod">Method to call when event is executed.</param>
		/// <param name="State">State object passed on to <paramref name="EventMethod"/>.</param>
		public ScheduledEvent(DateTime When, ScheduledEventCallback EventMethod, object State)
		{
			this.when = When;
			this.eventMethod = EventMethod;
			this.state = State;
		}

		/// <summary>
		/// When an event is to be executed.
		/// </summary>
		public DateTime When { get { return this.when; } }

		/// <summary>
		/// Method to call when event is executed.
		/// </summary>
		public ScheduledEventCallback EventMethod { get { return this.eventMethod; } }

		/// <summary>
		/// State object passed on to <see cref="EventMethod"/>.
		/// </summary>
		public object State { get { return this.state; } }

		/// <summary>
		/// Executes the event.
		/// </summary>
		public void Execute()
		{
			try
			{
				this.eventMethod?.Invoke(this.state);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

	}
}
