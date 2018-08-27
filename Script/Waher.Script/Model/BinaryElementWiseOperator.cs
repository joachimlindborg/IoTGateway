﻿using System;
using System.Collections.Generic;
using System.Text;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Exceptions;
using Waher.Script.Objects;

namespace Waher.Script.Model
{
	/// <summary>
	/// Base class for binary element-wise operators.
	/// </summary>
	public abstract class BinaryElementWiseOperator : BinaryScalarOperator
	{
		/// <summary>
		/// Base class for binary element-wise operators.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public BinaryElementWiseOperator(ScriptNode Left, ScriptNode Right, int Start, int Length, Expression Expression)
			: base(Left, Right, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Evaluates the operator.
		/// </summary>
		/// <param name="Left">Left value.</param>
		/// <param name="Right">Right value.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result</returns>
		public override IElement Evaluate(IElement Left, IElement Right, Variables Variables)
		{
			ISet LS = Left.AssociatedSet;
			ISet RS = Right.AssociatedSet;

			if (!LS.Equals(RS))
			{
				IRightModule RM;
				ILeftModule LM;
				bool b;

				try
				{
					b = (RM = LS as IRightModule) != null && RM.ScalarRing.Contains(Right);
				}
				catch (Exception)
				{
					b = false;
					RM = null;
				}

				if (b)
				{
					LinkedList<IElement> Result = new LinkedList<IElement>();

					foreach (IElement LeftChild in Left.ChildElements)
						Result.AddLast(this.Evaluate(LeftChild, Right, Variables));

					return Left.Encapsulate(Result, this);
				}

				try
				{
					b = (LM = RS as ILeftModule) != null && LM.ScalarRing.Contains(Left);
				}
				catch (Exception)
				{
					b = false;
					LM = null;
				}

				if (b)
				{
					LinkedList<IElement> Result = new LinkedList<IElement>();

					foreach (IElement RightChild in Right.ChildElements)
						Result.AddLast(this.Evaluate(Left, RightChild, Variables));

					return Right.Encapsulate(Result, this);
				}
			}

			return base.Evaluate(Left, Right, Variables);
		}
	}
}
