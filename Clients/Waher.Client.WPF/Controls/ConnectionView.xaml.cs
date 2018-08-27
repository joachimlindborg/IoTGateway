﻿using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Waher.Events;
using Waher.Networking;
using Waher.Client.WPF.Model;
using Waher.Client.WPF.Dialogs;
using Waher.Client.WPF.Controls.Sniffers;
using Waher.Networking.XMPP;
using Waher.Things.DisplayableParameters;

namespace Waher.Client.WPF.Controls
{
	/// <summary>
	/// Interaction logic for ConnectionView.xaml
	/// </summary>
	public partial class ConnectionView : UserControl, ITabView
	{
		private string fileName = string.Empty;
		private Connections connections;
		private TreeNode selectedNode = null;

		public ConnectionView()
		{
			InitializeComponent();
		}

		public void Load(MainWindow Owner)
		{
			this.connections = new Connections(Owner);
		}

		public void Dispose()
		{
			this.connections.New();
		}

		public MainWindow MainWindow
		{
			get { return MainWindow.FindWindow(this); }
		}

		public Connections Connections
		{
			get { return this.connections; }
		}

		public string FileName
		{
			get { return this.fileName; }
			set
			{
				this.fileName = value;
				if (string.IsNullOrEmpty(this.fileName))
					this.MainWindow.Title = MainWindow.WindowTitle;
				else
					this.MainWindow.Title = this.fileName + " - " + MainWindow.WindowTitle;
			}
		}

		private void ConnectionTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			this.ConnectionListView.Items.Clear();

			GridView GridView;

			if ((GridView = this.ConnectionListView.View as GridView) != null)
			{
				while (GridView.Columns.Count > 2)
					GridView.Columns.RemoveAt(2);
			}

			this.selectedNode = this.ConnectionTree.SelectedItem as TreeNode;
			if (this.selectedNode != null)
			{
				TreeNode[] Children = this.selectedNode.Children;
				Dictionary<string, bool> Headers = null;
				DisplayableParameters Parameters;

				if (Children != null)
				{
					foreach (TreeNode Child in this.selectedNode.Children)
					{
						this.ConnectionListView.Items.Add(Child);

						if (GridView != null)
						{
							Parameters = Child.DisplayableParameters;
							if (Parameters != null)
							{
								foreach (Parameter P in Parameters.Ordered)
								{
									if (P.Id == "NodeId" || P.Id == "Type")
										continue;

									if (Headers == null)
										Headers = new Dictionary<string, bool>();

									if (!Headers.ContainsKey(P.Id))
									{
										Headers[P.Id] = true;

										GridViewColumn Column = new GridViewColumn()
										{
											Header = P.Name,
											Width = double.NaN,
											DisplayMemberBinding = new Binding("DisplayableParameters[" + P.Id + "]")
										};

										GridView.Columns.Add(Column);
									}
								}
							}
						}
					}
				}
			}

			MainWindow MainWindow = MainWindow.FindWindow(this);
			if (MainWindow != null)
				MainWindow.SelectionChanged();
		}

		public bool SaveFile()
		{
			if (string.IsNullOrEmpty(this.fileName))
				return this.SaveNewFile();
			else
			{
				this.connections.Save(this.fileName);
				return true;
			}
		}

		public bool CheckSaved()
		{
			if (this.connections.Modified)
			{
				switch (MessageBox.Show(MainWindow.FindWindow(this), "You have unsaved changes. Do you want to save these changes before closing the application?",
					"Save unsaved changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
				{
					case MessageBoxResult.Yes:
						if (this.SaveFile())
							break;
						else
							return false;

					case MessageBoxResult.No:
						break;

					case MessageBoxResult.Cancel:
						return false;
				}
			}

			return true;
		}

		public bool SaveNewFile()
		{
			SaveFileDialog Dialog = new SaveFileDialog()
			{
				AddExtension = true,
				CheckPathExists = true,
				CreatePrompt = false,
				DefaultExt = "xml",
				Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
				Title = "Save connection file"
			};

			bool? Result = Dialog.ShowDialog(MainWindow.FindWindow(this));

			if (Result.HasValue && Result.Value)
			{
				this.FileName = Dialog.FileName;
				this.SaveFile();
				return true;
			}
			else
				return false;
		}

		public void Load(string FileName)
		{
			try
			{
				XmlDocument Xml = new XmlDocument();
				Xml.Load(FileName);

				switch (Xml.DocumentElement.LocalName)
				{
					case "ClientConnections":
						this.connections.Load(FileName, Xml);
						this.FileName = FileName;

						this.ConnectionTree.Items.Clear();
						foreach (TreeNode Node in this.connections.RootNodes)
							this.AddNode(Node);
						break;

					case "Sniff":
						TabItem TabItem = MainWindow.NewTab(System.IO.Path.GetFileName(FileName));
						this.MainWindow.Tabs.Items.Add(TabItem);

						SnifferView SnifferView = new SnifferView(null);
						TabItem.Content = SnifferView;

						SnifferView.Sniffer = new TabSniffer(TabItem, SnifferView);

						this.MainWindow.Tabs.SelectedItem = TabItem;

						SnifferView.Load(Xml, FileName);
						break;

					case "Chat":
						TabItem = MainWindow.NewTab(System.IO.Path.GetFileName(FileName));
						this.MainWindow.Tabs.Items.Add(TabItem);

						ChatView ChatView = new ChatView(null);
						ChatView.Input.IsEnabled = false;
						ChatView.SendButton.IsEnabled = false;

						TabItem.Content = ChatView;

						this.MainWindow.Tabs.SelectedItem = TabItem;

						ChatView.Load(Xml, FileName);
						break;

					case "SensorData":
						TabItem = MainWindow.NewTab(System.IO.Path.GetFileName(FileName));
						this.MainWindow.Tabs.Items.Add(TabItem);

						SensorDataView SensorDataView = new SensorDataView(null, null, false);
						TabItem.Content = SensorDataView;

						this.MainWindow.Tabs.SelectedItem = TabItem;

						SensorDataView.Load(Xml, FileName);
						break;

					case "SearchResult":
						TabItem = MainWindow.NewTab(System.IO.Path.GetFileName(FileName));
						this.MainWindow.Tabs.Items.Add(TabItem);

						SearchResultView SearchResultView = new SearchResultView();
						TabItem.Content = SearchResultView;

						this.MainWindow.Tabs.SelectedItem = TabItem;

						SearchResultView.Load(Xml, FileName);
						break;

					case "Script":
						TabItem = MainWindow.NewTab(System.IO.Path.GetFileName(FileName));
						this.MainWindow.Tabs.Items.Add(TabItem);

						ScriptView ScriptView = new ScriptView();
						TabItem.Content = ScriptView;

						this.MainWindow.Tabs.SelectedItem = TabItem;

						ScriptView.Load(Xml, FileName);
						break;

					default:
						throw new Exception("Unrecognized file format.");
				}
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				MessageBox.Show(ex.Message, ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void NewButton_Click(object sender, RoutedEventArgs e)
		{
			if (!this.CheckSaved())
				return;

			this.ConnectionTree.Items.Clear();
			this.connections.New();
			this.FileName = string.Empty;
		}

		public void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			this.SaveFile();
		}

		public void SaveAsButton_Click(object sender, RoutedEventArgs e)
		{
			this.SaveNewFile();
		}

		public void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			if (!this.CheckSaved())
				return;

			try
			{
				OpenFileDialog Dialog = new OpenFileDialog()
				{
					AddExtension = true,
					CheckFileExists = true,
					CheckPathExists = true,
					DefaultExt = "xml",
					Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
					Multiselect = false,
					ShowReadOnly = true,
					Title = "Open connection file"
				};

				bool? Result = Dialog.ShowDialog(MainWindow.FindWindow(this));

				if (Result.HasValue && Result.Value)
					this.Load(Dialog.FileName);
			}
			catch (Exception ex)
			{
				ex = Log.UnnestException(ex);
				MessageBox.Show(ex.Message, "Unable to load file.", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void ConnectTo_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ConnectToForm Dialog = new ConnectToForm()
			{
				Owner = this.MainWindow
			};
			bool? Result = Dialog.ShowDialog();

			if (Result.HasValue && Result.Value)
			{
				if (!int.TryParse(Dialog.XmppPort.Text, out int Port))
					Port = XmppCredentials.DefaultPort;

				XmppAccountNode Node = new XmppAccountNode(this.connections, null, Dialog.XmppServer.Text, 
					(TransportMethod)Dialog.ConnectionMethod.SelectedIndex, Port, Dialog.UrlEndpoint.Text,
					Dialog.AccountName.Text, Dialog.PasswordHash, Dialog.PasswordHashMethod,
					Dialog.TrustServerCertificate.IsChecked.HasValue && Dialog.TrustServerCertificate.IsChecked.Value,
					Dialog.AllowInsecureAuthentication.IsChecked.HasValue && Dialog.AllowInsecureAuthentication.IsChecked.Value);

				this.connections.Add(Node);
				this.AddNode(Node);
			}
		}

		private void AddNode(TreeNode Node)
		{
			this.ConnectionTree.Items.Add(Node);
			this.NodeAdded(null, Node);
		}

		public void NodeAdded(TreeNode Parent, TreeNode ChildNode)
		{
			ChildNode.Updated += this.Node_Updated;
			ChildNode.Added(this.MainWindow);
		}

		public void NodeRemoved(TreeNode Parent, TreeNode ChildNode)
		{
			ChildNode.Updated -= this.Node_Updated;
			ChildNode.Removed(this.MainWindow);

			if (Parent == null)
			{
				this.connections.Delete(ChildNode);
				this.ConnectionTree.Items.Remove(ChildNode);
			}
			else
				Parent.RemoveChild(ChildNode);

			this.ConnectionTree.Items.Refresh();
		}

		private void Node_Updated(object sender, EventArgs e)
		{
			this.Dispatcher.BeginInvoke(new ParameterizedThreadStart(this.RefreshTree), sender);
		}

		private void RefreshTree(object P)
		{
			TreeNode Node = (TreeNode)P;
			this.ConnectionTree.Items.Refresh();
		}

		private void ConnectionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.ConnectionListView_GotFocus(sender, e);
		}

		public TreeNode SelectedNode
		{
			get { return this.selectedNode; }
		}

		private void TreeContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			this.PopulateConextMenu(this.TreeContextMenu);
		}

		private void ListViewContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			this.PopulateConextMenu(this.ListViewContextMenu);
		}

		private void PopulateConextMenu(ContextMenu Menu)
		{
			string Group = string.Empty;

			Menu.Items.Clear();

			if (this.selectedNode != null)
				this.selectedNode.AddContexMenuItems(ref Group, Menu);
		}

		private void ConnectionListView_GotFocus(object sender, RoutedEventArgs e)
		{
			this.selectedNode = this.ConnectionListView.SelectedItem as TreeNode;

			MainWindow MainWindow = MainWindow.FindWindow(this);
			if (MainWindow != null)
				MainWindow.SelectionChanged();
		}

		private void ConnectionTree_GotFocus(object sender, RoutedEventArgs e)
		{
			this.selectedNode = this.ConnectionTree.SelectedItem as TreeNode;

			MainWindow MainWindow = MainWindow.FindWindow(this);
			if (MainWindow != null)
				MainWindow.SelectionChanged();
		}
	}
}
