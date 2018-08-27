﻿using System;
using System.Collections.Generic;
using System.Text;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Operators
{
	/// <summary>
	/// Named function call operator
	/// </summary>
	public class NamedFunctionCall : ScriptNode
	{
		private string functionName;
		private ScriptNode[] arguments;

		/// <summary>
		/// Named function call operator
		/// </summary>
		/// <param name="FunctionName">Function</param>
		/// <param name="Arguments">Arguments</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public NamedFunctionCall(string FunctionName, ScriptNode[] Arguments, int Start, int Length, Expression Expression)
			: base(Start, Length, Expression)
		{
			this.functionName = FunctionName;
			this.arguments = Arguments;
		}

		/// <summary>
		/// Function name.
		/// </summary>
		public string FunctionName
		{
			get { return this.functionName; }
		}

		/// <summary>
		/// Arguments
		/// </summary>
		public ScriptNode[] Arguments
		{
			get { return this.arguments; }
		}

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(Variables Variables)
		{
			string s = this.arguments.Length.ToString();
			LambdaDefinition f;

			if ((!Variables.TryGetVariable(this.functionName + " " + s, out Variable v) &&
			   !Variables.TryGetVariable(this.functionName, out v)) ||
			   ((f = v.ValueElement as LambdaDefinition) == null))
			{
				if (this.arguments.Length == 1)
					throw new ScriptRuntimeException("No function defined having 1 argument named '" + this.functionName + "' found.", this);
				else
					throw new ScriptRuntimeException("No function defined having " + s + " arguments named '" + this.functionName + "' found.", this);
			}

			int i, c = this.arguments.Length;
			IElement[] Arg = new IElement[c];
			ScriptNode Node;

			for (i = 0; i < c; i++)
			{
				Node = this.arguments[i];
				if (Node == null)
					Arg[i] = ObjectValue.Null;
				else
					Arg[i] = Node.Evaluate(Variables);
			}

			return f.Evaluate(Arg, Variables);
		}
	}
}
