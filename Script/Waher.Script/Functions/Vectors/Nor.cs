﻿using System;
using System.Numerics;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;
using Waher.Script.Objects;
using Waher.Script.Objects.VectorSpaces;

namespace Waher.Script.Functions.Vectors
{
    /// <summary>
    /// Nor(v)
    /// </summary>
    public class Nor : FunctionOneVectorVariable
    {
        /// <summary>
        /// Nor(v)
        /// </summary>
        /// <param name="Argument">Argument.</param>
        /// <param name="Start">Start position in script expression.</param>
        /// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
        public Nor(ScriptNode Argument, int Start, int Length, Expression Expression)
            : base(Argument, Start, Length, Expression)
        {
        }

        /// <summary>
        /// Name of the function
        /// </summary>
        public override string FunctionName
        {
            get { return "nor"; }
        }

        /// <summary>
        /// Evaluates the function on a vector argument.
        /// </summary>
        /// <param name="Argument">Function argument.</param>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Function result.</returns>
        public override IElement EvaluateVector(IVector Argument, Variables Variables)
        {
            throw new ScriptRuntimeException("Expected a double or boolean vector.", this);
        }

        /// <summary>
        /// Evaluates the function on a vector argument.
        /// </summary>
        /// <param name="Argument">Function argument.</param>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Function result.</returns>
        public override IElement EvaluateVector(DoubleVector Argument, Variables Variables)
        {
            return new DoubleNumber(CalcNor(Argument.Values, this));
        }

        /// <summary>
        /// Calculates the binary NOR of all double-valued elements. Values must be integers.
        /// </summary>
        /// <param name="Values">Values</param>
        /// <param name="Node">Node performing the evaluation.</param>
        /// <returns>Nor(Values)</returns>
        public static double CalcNor(double[] Values, ScriptNode Node)
        {
            double d;
            ulong Result;
            int i, c = Values.Length;
            bool Signed = false;

            if (c == 0)
                throw new ScriptRuntimeException("Empty set of values.", Node);

            d = Values[0];
            if (d < long.MinValue || d > ulong.MaxValue || d != Math.Truncate(d))
                throw new ScriptRuntimeException("Values must be integers.", Node);

            if (d < 0)
            {
                Result = (ulong)((long)d);
                Signed = true;
            }
            else
                Result = (ulong)d;

            if (Result == 0xffffffffffffffff)
                return 0;

            for (i = 1; i < c; i++)
            {
                d = Values[i];
                if (d < long.MinValue || d > ulong.MaxValue || d != Math.Truncate(d))
                    throw new ScriptRuntimeException("Values must be integers.", Node);

                if (d < 0)
                {
                    Result = (ulong)(((long)Result) | ((long)Result));
                    Signed = true;
                }
                else
                    Result |= (ulong)d;

                if (Result == 0xffffffffffffffff)
                    return 0;
            }

            if (Signed)
            {
                if (Result > long.MaxValue)
                    throw new ScriptRuntimeException("Overflow.", Node);
                else
                    return ~(long)Result;
            }
            else
                return ~Result;
        }

        /// <summary>
        /// Evaluates the function on a vector argument.
        /// </summary>
        /// <param name="Argument">Function argument.</param>
        /// <param name="Variables">Variables collection.</param>
        /// <returns>Function result.</returns>
        public override IElement EvaluateVector(BooleanVector Argument, Variables Variables)
        {
            return new BooleanValue(CalcNor(Argument.Values, this));
        }

        /// <summary>
        /// Calculates the logical NOR of all boolean-valued elements.
        /// </summary>
        /// <param name="Values">Values</param>
        /// <param name="Node">Node performing the evaluation.</param>
        /// <returns>Nor(Values)</returns>
        public static bool CalcNor(bool[] Values, ScriptNode Node)
        {
            int i, c = Values.Length;

            if (c == 0)
                throw new ScriptRuntimeException("Empty set of values.", Node);

            for (i = 0; i < c; i++)
            {
                if (Values[i])
                    return false;
            }

            return true;
        }

    }
}
