﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Waher.Script.Model
{
	/// <summary>
	/// Base class for all binary operators.
	/// </summary>
	public abstract class BinaryOperator : ScriptNode
	{
		/// <summary>
		/// Left operand.
		/// </summary>
		protected ScriptNode left;

		/// <summary>
		/// Right operand.
		/// </summary>
		protected ScriptNode right;

		/// <summary>
		/// Base class for all binary operators.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public BinaryOperator(ScriptNode Left, ScriptNode Right, int Start, int Length, Expression Expression)
			: base(Start, Length, Expression)
		{
			this.left = Left;
			this.right = Right;
		}

		/// <summary>
		/// Left operand.
		/// </summary>
		public ScriptNode LeftOperand
		{
			get { return this.left; }
		}

		/// <summary>
		/// Right operand.
		/// </summary>
		public ScriptNode RightOperand
		{
			get { return this.right; }
		}

		/// <summary>
		/// Default variable name, if any, null otherwise.
		/// </summary>
		public virtual string DefaultVariableName
		{
			get
			{
				if (this.left is IDifferentiable Left &&
					this.right is IDifferentiable Right)
				{
					string s = Left.DefaultVariableName;
					if (s == null)
						return null;
					else if (s == Right.DefaultVariableName)
						return s;
					else
						return null;
				}
				else
					return null;
			}
		}

	}
}
