﻿using System;
using System.Collections.Generic;
using System.Text;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;

namespace Waher.Script.Operators.Arithmetics
{
	/// <summary>
	/// Faculty operator.
	/// </summary>
	public class Faculty : UnaryDoubleOperator
	{
		/// <summary>
		/// Faculty operator.
		/// </summary>
		/// <param name="Operand">Operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public Faculty(ScriptNode Operand, int Start, int Length, Expression Expression)
			: base(Operand, Start, Length, Expression)
		{
		}

        /// <summary>
        /// Evaluates the double operator.
        /// </summary>
        /// <param name="Operand">Operand.</param>
        /// <returns>Result</returns>
        public override IElement Evaluate(double Operand)
        {
            if (Operand < 0 || Math.Truncate(Operand) != Operand)
                throw new ScriptRuntimeException("Operand must be a non-negative integer.", this);

            double Result = 1;

            while (Operand > 0)
            {
                Result *= Operand;
                if (double.IsInfinity(Result))
                    throw new ScriptRuntimeException("Overflow.", this);

                Operand--;
            }

            return new DoubleNumber(Result);
        }
    }
}
