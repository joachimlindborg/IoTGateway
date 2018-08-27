﻿using System;
using System.Collections.Generic;
using System.Text;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;

namespace Waher.Script.Operators.Assignments
{
	/// <summary>
	/// Add to self operator.
	/// </summary>
	public class AddToSelf : Assignment 
	{
		/// <summary>
		/// Add to self operator.
		/// </summary>
		/// <param name="VariableName">Variable name..</param>
		/// <param name="Operand">Operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public AddToSelf(string VariableName, ScriptNode Operand, int Start, int Length, Expression Expression)
			: base(VariableName, Operand, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(Variables Variables)
		{
            Variable v;

            if (!Variables.TryGetVariable(this.VariableName, out v))
                throw new ScriptRuntimeException("Variable not found.", this);

            IElement E = this.op.Evaluate(Variables);
            E = Operators.Arithmetics.Add.EvaluateAddition(v.ValueElement, E, this);

            Variables[this.VariableName] = E;

            return E;
		}
	}
}
