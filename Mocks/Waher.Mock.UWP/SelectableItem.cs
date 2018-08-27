﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Windows;
using Windows.UI.Popups;

namespace Waher.Mock
{
	/// <summary>
	/// Abstract base class for selectable items.
	/// </summary>
	public abstract class SelectableItem
	{
		private bool selected = false;

		/// <summary>
		/// Abstract base class for selectable items.
		/// </summary>
		public SelectableItem()
		{
		}

		/// <summary>
		/// Raises an event.
		/// </summary>
		/// <param name="h">Event handler.</param>
		protected async void Raise(EventHandler h)
		{
			if (h != null)
			{
				try
				{
					h(this, new EventArgs());
				}
				catch (Exception ex)
				{
					MessageDialog Dialog = new MessageDialog(ex.Message, "Error");
					await Dialog.ShowAsync();
				}
			}
		}

		/// <summary>
		/// If the node is selected.
		/// </summary>
		public bool IsSelected
		{
			get { return this.selected; }
			set
			{
				if (this.selected != value)
				{
					this.selected = value;

					if (this.selected)
						this.OnSelected();
					else
						this.OnDeselected();
				}
			}
		}

		/// <summary>
		/// Event raised when the node has been selected.
		/// </summary>
		public event EventHandler Selected = null;

		/// <summary>
		/// Event raised when the node has been deselected.
		/// </summary>
		public event EventHandler Deselected = null;

		/// <summary>
		/// Raises the <see cref="Selected"/> event.
		/// </summary>
		protected virtual void OnSelected()
		{
			this.Raise(this.Selected);
		}

		/// <summary>
		/// Raises the <see cref="Deselected"/> event.
		/// </summary>
		protected virtual void OnDeselected()
		{
			this.Raise(this.Deselected);
		}

	}
}
