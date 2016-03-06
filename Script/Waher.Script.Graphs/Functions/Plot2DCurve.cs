﻿using System;
using System.Collections.Generic;
using System.Drawing;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Exceptions;
using Waher.Script.Model;

namespace Waher.Script.Graphs.Functions
{
	public class Plot2DCurve : FunctionMultiVariate
	{
		private static readonly ArgumentType[] argumentTypes5Parameters = new ArgumentType[] { ArgumentType.Vector, ArgumentType.Vector, ArgumentType.Scalar, ArgumentType.Scalar, ArgumentType.Scalar };
		private static readonly ArgumentType[] argumentTypes4Parameters = new ArgumentType[] { ArgumentType.Vector, ArgumentType.Vector, ArgumentType.Scalar, ArgumentType.Scalar };
		private static readonly ArgumentType[] argumentTypes3Parameters = new ArgumentType[] { ArgumentType.Vector, ArgumentType.Vector, ArgumentType.Scalar };
		private static readonly ArgumentType[] argumentTypes2Parameters = new ArgumentType[] { ArgumentType.Vector, ArgumentType.Vector };

		/// <summary>
		/// Plots a two-dimensional curve.
		/// </summary>
		/// <param name="X">X-axis.</param>
		/// <param name="Y">Y-axis.</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		public Plot2DCurve(ScriptNode X, ScriptNode Y, int Start, int Length)
			: base(new ScriptNode[] { X, Y }, argumentTypes2Parameters, Start, Length)
		{
		}

		/// <summary>
		/// Plots a two-dimensional curve.
		/// </summary>
		/// <param name="X">X-axis.</param>
		/// <param name="Y">Y-axis.</param>
		/// <param name="Color">Color</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		public Plot2DCurve(ScriptNode X, ScriptNode Y, ScriptNode Color, int Start, int Length)
			: base(new ScriptNode[] { X, Y, Color }, argumentTypes3Parameters, Start, Length)
		{
		}

		/// <summary>
		/// Plots a two-dimensional curve.
		/// </summary>
		/// <param name="X">X-axis.</param>
		/// <param name="Y">Y-axis.</param>
		/// <param name="Color">Color</param>
		/// <param name="Size">Size</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		public Plot2DCurve(ScriptNode X, ScriptNode Y, ScriptNode Color, ScriptNode Size, int Start, int Length)
			: base(new ScriptNode[] { X, Y, Color, Size }, argumentTypes4Parameters, Start, Length)
		{
		}

		/// <summary>
		/// Plots a two-dimensional curve.
		/// </summary>
		/// <param name="X">X-axis.</param>
		/// <param name="Y">Y-axis.</param>
		/// <param name="Color">Color</param>
		/// <param name="Size">Size</param>
		/// <param name="Tension">Tension</param>
		/// <param name="Start">Start position in script expression.</param>
		/// <param name="Length">Length of expression covered by node.</param>
		public Plot2DCurve(ScriptNode X, ScriptNode Y, ScriptNode Color, ScriptNode Size, ScriptNode Tension, int Start, int Length)
			: base(new ScriptNode[] { X, Y, Color, Size, Tension }, argumentTypes5Parameters, Start, Length)
		{
		}

		/// <summary>
		/// Name of the function
		/// </summary>
		public override string FunctionName
		{
			get { return "plot2dcurve"; }
		}

		/// <summary>
		/// Default Argument names
		/// </summary>
		public override string[] DefaultArgumentNames
		{
			get { return new string[] { "x", "y", "color", "size", "tension" }; }
		}

		/// <summary>
		/// Evaluates the function.
		/// </summary>
		/// <param name="Arguments">Function arguments.</param>
		/// <param name="Variables">Variables collection.</param>
		/// <returns>Function result.</returns>
		public override IElement Evaluate(IElement[] Arguments, Variables Variables)
		{
			IVector X = Arguments[0] as IVector;
			if (X == null)
				throw new ScriptRuntimeException("Expected vector for X argument.", this);

			IVector Y = Arguments[1] as IVector;
			if (Y == null)
				throw new ScriptRuntimeException("Expected vector for Y argument.", this);

			int Dimension = X.Dimension;
			if (Y.Dimension != Dimension)
				throw new ScriptRuntimeException("Vector size mismatch.", this);

			IElement Color = Arguments.Length <= 2 ? null : Arguments[2];
			IElement Size = Arguments.Length <= 3 ? null : Arguments[3];
			IElement Tension = Arguments.Length <= 4 ? null : Arguments[4];

			return new Graph2D(X, Y, this.DrawCurve,
				Color == null ? System.Drawing.Color.Red : Color.AssociatedObjectValue,
				Size == null ? 2.0 : Size.AssociatedObjectValue,
				Tension == null ? 0.5 : Tension.AssociatedObjectValue);
		}

		private void DrawCurve(Graphics Canvas, PointF[] Points, object[] Parameters)
		{
			using (Pen Pen = Graph.ToPen(Parameters[0], Parameters[1]))
			{
				Canvas.DrawCurve(Pen, Points, (float)Expression.ToDouble(Parameters[2]));
			}
		}

	}
}
