﻿using System;
using System.Collections.Generic;
using System.Text;
using Waher.Script.Abstraction.Sets;
using Waher.Script.Abstraction.Elements;

namespace Waher.Script.Objects
{
	/// <summary>
	/// Pseudo-field of double numbers, as an approximation of the field of real numbers.
	/// </summary>
	public sealed class DoubleNumbers : Field, IOrderedSet
	{
        private static readonly int hashCode = typeof(DoubleNumbers).FullName.GetHashCode();

		/// <summary>
		/// Pseudo-field of double numbers, as an approximation of the field of real numbers.
		/// </summary>
		public DoubleNumbers()
		{
		}

		/// <summary>
		/// Instance of the set of complex numbers.
		/// </summary>
		public static readonly DoubleNumbers Instance = new DoubleNumbers();

		/// <summary>
		/// Returns the identity element of the commutative ring with identity.
		/// </summary>
		public override ICommutativeRingWithIdentityElement One
		{
			get { return DoubleNumber.OneElement; }
		}

		/// <summary>
		/// Returns the zero element of the group.
		/// </summary>
		public override IAbelianGroupElement Zero
		{
			get { return DoubleNumber.ZeroElement; }
		}

		/// <summary>
		/// Checks if the set contains an element.
		/// </summary>
		/// <param name="Element">Element.</param>
		/// <returns>If the element is contained in the set.</returns>
		public override bool Contains(IElement Element)
		{
			return Element is DoubleNumber;
		}

		/// <summary>
		/// <see cref="Object.Equals(object)"/>
		/// </summary>
		public override bool Equals(object obj)
		{
			return obj is DoubleNumbers;
		}

		/// <summary>
		/// <see cref="Object.GetHashCode()"/>
		/// </summary>
		public override int GetHashCode()
		{
			return hashCode;
		}

		/// <summary>
		/// Compares two double values.
		/// </summary>
		/// <param name="x">Value 1</param>
		/// <param name="y">Value 2</param>
		/// <returns>Result</returns>
		public int Compare(IElement x, IElement y)
		{
			DoubleNumber d1 = (DoubleNumber)x;
			DoubleNumber d2 = (DoubleNumber)y;

			return d1.Value.CompareTo(d2.Value);
		}

	}
}
