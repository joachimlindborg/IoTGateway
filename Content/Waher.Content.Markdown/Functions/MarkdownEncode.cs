﻿using System;
using Waher.Script;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Content.Markdown.Functions
{
	/// <summary>
	/// MarkdownEncode(s)
	/// </summary>
	public class MarkdownEncode : FunctionOneScalarVariable
	{
		/// <summary>
		/// MarkdownEncode(x)
		/// </summary>
		/// <param name="Argument">Argument.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public MarkdownEncode(ScriptNode Argument, int Start, int Length, Expression Expression)
            : base(Argument, Start, Length, Expression)
        {
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName
		{
			get { return "markdownencode"; }
		}

		/// <summary>
		/// Evaluates the function on a scalar argument.
		/// </summary>
		/// <param name="Argument">Function argument.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement EvaluateScalar(string Argument, Variables Variables)
		{
			return new StringValue(EscapeText(Argument));
		}

		/// <summary>
		/// Escapes text for inclusion in a Markdown document.
		/// </summary>
		/// <param name="PlainText">Text to include.</param>
		/// <returns>Escaped text.</returns>
		public static string EscapeText(string PlainText)
		{
			return CommonTypes.Escape(PlainText, specialCharacters, specialCharactersEncoded);
		}

		private static readonly char[] specialCharacters = new char[]
		{
			'*', '_', '~', '\\', '`', '{', '}', '[', ']', '(', ')', '<', '>', '&', '#', '+', '-', '.', '!', '\'', '^', '%', '=', ':', '|'
		};

		private static readonly string[] specialCharactersEncoded = new string[]
		{
			"\\*", "\\_", "\\~", "\\\\", "\\`", "\\{", "\\}", "\\[", "\\]", "\\(", "\\)", "\\<", "\\>", "\\&",
			"\\#", "\\+", "\\-", "\\.", "\\!", "\\\"", "\\^", "\\%", "\\=", "\\:", "&#124;"
		};

	}
}
