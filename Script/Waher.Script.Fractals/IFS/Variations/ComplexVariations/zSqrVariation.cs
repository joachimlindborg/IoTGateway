﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Waher.Script.Abstraction.Elements;
using Waher.Script.Model;

namespace Waher.Script.Fractals.IFS.Variations.ComplexVariations
{
    public class zSqrVariation : FlameVariationZeroParameters
    {
        public zSqrVariation(int Start, int Length, Expression Expression)
            : base(Start, Length, Expression)
        {
        }

        public override void Operate(ref double x, ref double y)
        {
            double x2 = x * x - y * y;
            y = 2 * x * y;
            x = x2;
        }

        public override string FunctionName
        {
            get { return "zSqrVariation"; }
        }
    }
}