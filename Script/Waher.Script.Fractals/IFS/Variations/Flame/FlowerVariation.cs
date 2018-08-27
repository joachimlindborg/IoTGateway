﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;

namespace Waher.Script.Fractals.IFS.Variations.Flame
{
    public class FlowerVariation : FlameVariationMultipleParameters
    {
        private double holes;
        private double petals;

        public FlowerVariation(ScriptNode holes, ScriptNode petals, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { holes, petals }, new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar },
				  Start, Length, Expression)
		{
			this.holes = 0;
            this.petals = 0;
        }

        private FlowerVariation(double Holes, double Petals, ScriptNode holes, ScriptNode petals, int Start, int Length, Expression Expression)
			: base(new ScriptNode[] { holes, petals }, new ArgumentType[] { ArgumentType.Scalar, ArgumentType.Scalar },
				  Start, Length, Expression)
        {
            this.holes = Holes;
            this.petals = Petals;
        }

		public override string[] DefaultArgumentNames
		{
			get
			{
				return new string[] { "holes", "petals" };
			}
		}

		public override IElement Evaluate(IElement[] Arguments, Variables Variables)
        {
            double Holes = Expression.ToDouble(Arguments[0].AssociatedObjectValue);
            double Petals = Expression.ToDouble(Arguments[1].AssociatedObjectValue);

            return new FlowerVariation(Holes, Petals, this.Arguments[0], this.Arguments[1], this.Start, this.Length, this.Expression);
        }

        public override void Operate(ref double x, ref double y)
        {
            double r1;

            lock (this.gen)
            {
                r1 = this.gen.NextDouble();
            }

            double a = System.Math.Atan2(y, x);
            double r = (r1 - this.holes) * System.Math.Cos(this.petals * a);
            x = r * System.Math.Cos(a);
            y = r * System.Math.Sin(a);
        }

        private Random gen = new Random();

        public override string FunctionName
        {
            get { return "FlowerVariation"; }
        }
    }
}
