﻿using System;
using System.Collections.Generic;
using System.Text;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Exceptions;
using Waher.Script.Model;

namespace Waher.Script.Operators.Arithmetics
{
	/// <summary>
	/// Division operator.
	/// </summary>
	public class Divide : BinaryOperator, IDifferentiable
	{
		/// <summary>
		/// Division operator.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		/// <param name="Expression">Expression containing script.</param>
		public Divide(ScriptNode Left, ScriptNode Right, int Start, int Length, Expression Expression)
			: base(Left, Right, Start, Length, Expression)
		{
		}

		/// <summary>
		/// Evaluates the node, using the variables provided in the <paramref name="Variables"/> collection.
		/// </summary>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Result.</returns>
		public override IElement Evaluate(Variables Variables)
		{
			IElement Left = this.left.Evaluate(Variables);
			IElement Right = this.right.Evaluate(Variables);

			return EvaluateDivision(Left, Right, this);
		}

		/// <summary>
		/// Divides the right operand from the left one.
		/// </summary>
		/// <param name="Left">Left operand.</param>
		/// <param name="Right">Right operand.</param>
		/// <param name="Node">Node performing the operation.</param>
		/// <returns>Result</returns>
		public static IElement EvaluateDivision(IElement Left, IElement Right, ScriptNode Node)
		{
			IRingElement RE = Right as IRingElement;
			IElement Result;
			IRingElement Temp;

			if (Left is IRingElement LE && RE != null)
			{
				Temp = RE.Invert();
				if (Temp != null)
				{
					Result = LE.MultiplyRight(Temp);
					if (Result != null)
						return Result;

					Result = Temp.MultiplyLeft(LE);
					if (Result != null)
						return Result;
				}
			}

			if (Left.IsScalar)
			{
				if (Right.IsScalar)
				{
					ISet LeftSet = Left.AssociatedSet;
					ISet RightSet = Right.AssociatedSet;

					if (!LeftSet.Equals(RightSet))
					{
						if (!Expression.Upgrade(ref Left, ref LeftSet, ref Right, ref RightSet, Node))
							throw new ScriptRuntimeException("Incompatible operands.", Node);

						LE = Left as IRingElement;
						RE = Right as IRingElement;
						if (LE != null && RE != null)
						{
							Temp = RE.Invert();
							if (Temp != null)
							{
								Result = LE.MultiplyRight(Temp);
								if (Result != null)
									return Result;

								Result = Temp.MultiplyLeft(LE);
								if (Result != null)
									return Result;
							}
						}
					}

					throw new ScriptRuntimeException("Operands cannot be divided.", Node);
				}
				else
				{
					LinkedList<IElement> Elements = new LinkedList<IElement>();

					foreach (IElement RightChild in Right.ChildElements)
						Elements.AddLast(EvaluateDivision(Left, RightChild, Node));

					return Right.Encapsulate(Elements, Node);
				}
			}
			else
			{
				if (Right.IsScalar)
				{
					LinkedList<IElement> Elements = new LinkedList<IElement>();

					foreach (IElement LeftChild in Left.ChildElements)
						Elements.AddLast(EvaluateDivision(LeftChild, Right, Node));

					return Left.Encapsulate(Elements, Node);
				}
				else
				{
					ICollection<IElement> LeftChildren = Left.ChildElements;
					ICollection<IElement> RightChildren = Right.ChildElements;

					if (LeftChildren.Count == RightChildren.Count)
					{
						LinkedList<IElement> Elements = new LinkedList<IElement>();
						IEnumerator<IElement> eLeft = LeftChildren.GetEnumerator();
						IEnumerator<IElement> eRight = RightChildren.GetEnumerator();

						try
						{
							while (eLeft.MoveNext() && eRight.MoveNext())
								Elements.AddLast(EvaluateDivision(eLeft.Current, eRight.Current, Node));
						}
						finally
						{
							eLeft.Dispose();
							eRight.Dispose();
						}

						return Left.Encapsulate(Elements, Node);
					}
					else
					{
						LinkedList<IElement> LeftResult = new LinkedList<IElement>();

						foreach (IElement LeftChild in LeftChildren)
						{
							LinkedList<IElement> RightResult = new LinkedList<IElement>();

							foreach (IElement RightChild in RightChildren)
								RightResult.AddLast(EvaluateDivision(LeftChild, RightChild, Node));

							LeftResult.AddLast(Right.Encapsulate(RightResult, Node));
						}

						return Left.Encapsulate(LeftResult, Node);
					}
				}
			}
		}

		/// <summary>
		/// Differentiates a script node, if possible.
		/// </summary>
		/// <param name="VariableName">Name of variable to differentiate on.</param>
		/// <param name="Variables">Collection of variables.</param>
		/// <returns>Differentiated node.</returns>
		public ScriptNode Differentiate(string VariableName, Variables Variables)
		{
			if (this.left is IDifferentiable Left &&
				this.right is IDifferentiable Right)
			{
				int Start = this.Start;
				int Len = this.Length;
				Expression Expression = this.Expression;

				return new Divide(
					new Subtract(
						new Multiply(
							Left.Differentiate(VariableName, Variables),
							this.right,
							Start, Len, Expression),
						new Multiply(
							this.left,
							Right.Differentiate(VariableName, Variables),
							Start, Len, Expression),
						Start, Len, Expression),
					new Square(
						this.right, Start, Len, Expression),
					Start, Len, Expression);
			}
			else
				throw new ScriptRuntimeException("Factors not differentiable.", this);
		}

	}
}
