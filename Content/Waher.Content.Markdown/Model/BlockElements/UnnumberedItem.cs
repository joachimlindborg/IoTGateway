﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Waher.Content.Markdown.Model.SpanElements;

namespace Waher.Content.Markdown.Model.BlockElements
{
	/// <summary>
	/// Represents an unnumbered item in an ordered list.
	/// </summary>
	public class UnnumberedItem : MarkdownElementSingleChild
	{
		private string prefix;

		/// <summary>
		/// Represents an unnumbered item in an ordered list.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Prefix">Prefix, in plain text mode.</param>
		/// <param name="Child">Child element.</param>
		public UnnumberedItem(MarkdownDocument Document, string Prefix, MarkdownElement Child)
			: base(Document, Child)
		{
			this.prefix = Prefix;
		}

		/// <summary>
		/// Prefix, in plain text mode.
		/// </summary>
		public string Prefix
		{
			get { return this.prefix; }
		}

		/// <summary>
		/// Generates HTML for the markdown element.
		/// </summary>
		/// <param name="Output">HTML will be output here.</param>
		public override void GenerateHTML(StringBuilder Output)
		{
			Output.Append("<li");

			if (this.Document.Detail != null)
			{
				if (this.Child is Link)
				{
					if (string.Compare(this.Document.Detail.ResourceName, ((Link)this.Child).Url, true) == 0)
						Output.Append(" class=\"active\"");
				}
				else if (this.Child is LinkReference)
				{
					string Label = ((LinkReference)this.Child).Label;
					SpanElements.Multimedia Multimedia = this.Document.GetReference(Label);

					if (Multimedia != null && Multimedia.Items.Length == 1 && string.Compare(Multimedia.Items[0].Url, this.Document.Detail.ResourceName, true) == 0)
						Output.Append(" class=\"active\"");
				}
			}

			Output.Append('>');
			this.Child.GenerateHTML(Output);
			Output.AppendLine("</li>");
		}

		/// <summary>
		/// Generates plain text for the markdown element.
		/// </summary>
		/// <param name="Output">Plain text will be output here.</param>
		public override void GeneratePlainText(StringBuilder Output)
		{
			Output.Append(this.prefix);

			StringBuilder sb = new StringBuilder();
			this.Child.GeneratePlainText(sb);

			string s = sb.ToString();

			Output.Append(s);

			if (!s.EndsWith("\n"))
				Output.AppendLine();
		}

		/// <summary>
		/// Generates XAML for the markdown element.
		/// </summary>
		/// <param name="Output">XAML will be output here.</param>
		/// <param name="Settings">XAML settings.</param>
		/// <param name="TextAlignment">Alignment of text in element.</param>
		public override void GenerateXAML(XmlWriter Output, XamlSettings Settings, TextAlignment TextAlignment)
		{
			this.Child.GenerateXAML(Output, Settings, TextAlignment);
		}

		/// <summary>
		/// If the element is an inline span element.
		/// </summary>
		internal override bool InlineSpanElement
		{
			get
			{
				return this.Child.InlineSpanElement;
			}
		}

		/// <summary>
		/// Gets margins for content.
		/// </summary>
		/// <param name="Settings">XAML settings.</param>
		/// <param name="TopMargin">Top margin.</param>
		/// <param name="BottomMargin">Bottom margin.</param>
		internal override void GetMargins(XamlSettings Settings, out int TopMargin, out int BottomMargin)
		{
			this.Child.GetMargins(Settings, out TopMargin, out BottomMargin);
		}

		/// <summary>
		/// Exports the element to XML.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		public override void Export(XmlWriter Output)
		{
			Output.WriteStartElement("UnnumberedItem");
			Output.WriteAttributeString("prefix", this.prefix);
			this.ExportChild(Output);
			Output.WriteEndElement();
		}
	}
}
