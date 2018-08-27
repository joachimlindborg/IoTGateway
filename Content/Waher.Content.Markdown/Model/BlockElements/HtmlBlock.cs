﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Waher.Content.Markdown.Model.BlockElements
{
	/// <summary>
	/// Represents a block of HTML in a markdown document.
	/// </summary>
	public class HtmlBlock : MarkdownElementChildren
	{
		/// <summary>
		/// Represents a block of HTML in a markdown document.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Children">Child elements.</param>
		public HtmlBlock(MarkdownDocument Document, IEnumerable<MarkdownElement> Children)
			: base(Document, Children)
		{
		}

		/// <summary>
		/// Generates HTML for the markdown element.
		/// </summary>
		/// <param name="Output">HTML will be output here.</param>
		public override void GenerateHTML(StringBuilder Output)
		{
			foreach (MarkdownElement E in this.Children)
				E.GenerateHTML(Output);

			Output.AppendLine();
		}

		/// <summary>
		/// Generates plain text for the markdown element.
		/// </summary>
		/// <param name="Output">Plain text will be output here.</param>
		public override void GeneratePlainText(StringBuilder Output)
		{
			StringBuilder sb = new StringBuilder();
			string s;
			bool First = true;

			foreach (MarkdownElement E in this.Children)
			{
				E.GeneratePlainText(sb);
				s = sb.ToString().TrimStart().Trim(' ');    // Only space at the end, not CRLF
				sb.Clear();

				if (!string.IsNullOrEmpty(s))
				{
					if (First)
						First = false;
					else
						Output.Append(' ');

					Output.Append(s);

					if (s.EndsWith("\n"))
						First = true;
				}
			}

			Output.AppendLine();
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
			Output.WriteStartElement("TextBlock");
			Output.WriteAttributeString("TextWrapping", "Wrap");
			Output.WriteAttributeString("Margin", Settings.ParagraphMargins);
			if (TextAlignment != TextAlignment.Left)
				Output.WriteAttributeString("TextAlignment", TextAlignment.ToString());

			foreach (MarkdownElement E in this.Children)
				E.GenerateXAML(Output, Settings, TextAlignment);

			Output.WriteEndElement();
		}

		/// <summary>
		/// If the element is an inline span element.
		/// </summary>
		internal override bool InlineSpanElement
		{
			get { return false; }
		}

		/// <summary>
		/// Exports the element to XML.
		/// </summary>
		/// <param name="Output">XML Output.</param>
		public override void Export(XmlWriter Output)
		{
			this.Export(Output, "HtmlBlock");
		}
	}
}