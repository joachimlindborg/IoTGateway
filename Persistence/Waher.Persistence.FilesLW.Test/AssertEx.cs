﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if !LW
namespace Waher.Persistence.Files.Test
#else
using Waher.Persistence.Files;
namespace Waher.Persistence.FilesLW.Test
#endif
{
	public static class AssertEx
	{
		private static void MakeCompatible(ref object Left, ref object Right)
		{
			if (Left != null && Right != null && Left.GetType() != Right.GetType())
			{
				if (Left is long)
					Right = System.Convert.ToInt64(Right);
				else if (Right is long)
					Left = System.Convert.ToInt64(Left);
				else if (Left is int)
					Right = System.Convert.ToInt32(Right);
				else if (Right is int)
					Left = System.Convert.ToInt32(Left);
			}
		}

		public static void Same(object Left, object Right)
		{
			if (Left == null && Right == null)
				return;

			Assert.IsNotNull(Left);
			Assert.IsNotNull(Right);

			MakeCompatible(ref Left, ref Right);
			if (Left is IComparable l)
				Assert.IsTrue(l.CompareTo(Right) == 0);
			else if (Right is IComparable r)
				Assert.IsTrue(r.CompareTo(Left) == 0);
			else if (Left is Array al && Right is Array ar)
			{
				int i, c;
				Assert.AreEqual(c = al.Length, ar.Length, "Array lengths differ.");

				for (i = 0; i < c; i++)
					Same(al.GetValue(i), ar.GetValue(i));
			}
			else
			{
				Assert.AreEqual(Left, Right, "Values differ: " +
					Left.ToString() + "(" + Left.GetType().FullName + "), " +
					Right.ToString() + "(" + Right.GetType().FullName + ")");
			}
		}

		public static void NotSame(object Left, object Right)
		{
			MakeCompatible(ref Left, ref Right);
			if (Left is IComparable l)
				Assert.IsTrue(l.CompareTo(Right) != 0);
			else if (Right is IComparable r)
				Assert.IsTrue(r.CompareTo(Left) != 0);
			else
				Assert.AreNotEqual(Left, Right);
		}

		public static void Less(object Left, object Right)
		{
			MakeCompatible(ref Left, ref Right);

			if (Left is IComparable l)
				Assert.IsTrue(l.CompareTo(Right) < 0);
			else if (Right is IComparable r)
				Assert.IsTrue(r.CompareTo(Left) > 0);
			else
				Assert.Fail("Values not comparable.");
		}

		public static void Greater(object Left, object Right)
		{
			MakeCompatible(ref Left, ref Right);

			if (Left is IComparable l)
				Assert.IsTrue(l.CompareTo(Right) > 0);
			else if (Right is IComparable r)
				Assert.IsTrue(r.CompareTo(Left) < 0);
			else
				Assert.Fail("Values not comparable.");
		}

		public static void LessOrEqual(object Left, object Right)
		{
			MakeCompatible(ref Left, ref Right);

			if (Left is IComparable l)
				Assert.IsTrue(l.CompareTo(Right) <= 0);
			else if (Right is IComparable r)
				Assert.IsTrue(r.CompareTo(Left) >= 0);
			else
				Assert.Fail("Values not comparable.");
		}

		public static void GreaterOrEqual(object Left, object Right)
		{
			MakeCompatible(ref Left, ref Right);

			if (Left is IComparable l)
				Assert.IsTrue(l.CompareTo(Right) >= 0);
			else if (Right is IComparable r)
				Assert.IsTrue(r.CompareTo(Left) <= 0);
			else
				Assert.Fail("Values not comparable.");
		}
	}
}
