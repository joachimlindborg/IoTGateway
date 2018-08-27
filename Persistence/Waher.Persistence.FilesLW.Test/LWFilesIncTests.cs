﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Persistence.Files.Searching;

#if !LW
namespace Waher.Persistence.Files.Test
#else
using Waher.Persistence.Files;
namespace Waher.Persistence.FilesLW.Test
#endif
{
	[TestClass]
	public class DBFilesIncTests
	{
		[TestMethod]
		public void DBFiles_Inc_Test_01_Boolean()
		{
			object Value = false;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, true);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_02_Byte()
		{
			object Value = (byte)10;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, (byte)11);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_03_Int16()
		{
			object Value = (short)10;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, (short)11);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_04_Int32()
		{
			object Value = (int)10;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, (int)11);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_05_Int64()
		{
			object Value = (long)10;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, (long)11);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_06_SByte()
		{
			object Value = (sbyte)10;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, (sbyte)11);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_07_UInt16()
		{
			object Value = (ushort)10;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, (ushort)11);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_08_UInt32()
		{
			object Value = (uint)10;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, (uint)11);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_09_UInt64()
		{
			object Value = (ulong)10;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, (ulong)11);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_10_Decimal()
		{
			decimal Org = (decimal)10;
			object Value = Org;
			Assert.IsTrue(Comparison.Increment(ref Value));
			Assert.AreNotEqual(Value, Org);
			decimal Diff = (decimal)Value - Org;
			Assert.AreEqual(Org + Diff / 2, Org);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_11_Double()
		{
			double Org = (double)10;
			object Value = Org;
			Assert.IsTrue(Comparison.Increment(ref Value));
			Assert.AreNotEqual(Value, Org);
			double Diff = (double)Value - Org;
			Assert.AreEqual(Org + Diff / 2, Org);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_12_Single()
		{
			float Org = (float)10;
			object Value = Org;
			Assert.IsTrue(Comparison.Increment(ref Value));
			Assert.AreNotEqual(Value, Org);
			float Diff = (float)Value - Org;
			Assert.AreEqual(Org + Diff / 2, Org);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_13_DateTime()
		{
			DateTime Org = DateTime.Now;
			object Value = Org;
			Assert.IsTrue(Comparison.Increment(ref Value));
			Assert.AreNotEqual(Value, Org);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_14_TimeSpan()
		{
			TimeSpan Org = DateTime.Now.TimeOfDay;
			object Value = Org;
			Assert.IsTrue(Comparison.Increment(ref Value));
			Assert.AreNotEqual(Value, Org);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_15_Char()
		{
			object Value = 'A';
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, 'B');
		}

		[TestMethod]
		public void DBFiles_Inc_Test_16_String()
		{
			object Value = "Hello";
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, "Hello\x00");
		}

		[TestMethod]
		public void DBFiles_Inc_Test_17_Guid()
		{
			Guid Guid = System.Guid.NewGuid();
			object Value = Guid;
			Assert.IsTrue(Comparison.Increment(ref Value));
			Assert.AreNotEqual(Value, Guid);
		}
		[TestMethod]
		public void DBFiles_Inc_Test_18_Boolean_Overflow()
		{
			object Value = true;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, 2);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_19_Byte_Overflow()
		{
			object Value = byte.MaxValue;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, 256);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_20_Int16_Overflow()
		{
			object Value = short.MaxValue;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, 32768);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_21_Int32_Overflow()
		{
			object Value = int.MaxValue;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, 0x80000000);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_22_Int64_Overflow()
		{
			object Value = long.MaxValue;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, 0x8000000000000000);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_23_SByte_Overflow()
		{
			object Value = sbyte.MaxValue;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, 128);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_24_UInt16_Overflow()
		{
			object Value = ushort.MaxValue;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, 65536);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_25_UInt32_Overflow()
		{
			object Value = uint.MaxValue;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, 0x100000000);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_26_UInt64_Overflow()
		{
			object Value = ulong.MaxValue;
			Assert.IsTrue(Comparison.Increment(ref Value));
			double d = ulong.MaxValue;
			Assert.IsTrue(Comparison.Increment(ref d));
			AssertEx.Same(Value, d);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_27_Char_Overflow()
		{
			object Value = char.MaxValue;
			Assert.IsTrue(Comparison.Increment(ref Value));
			AssertEx.Same(Value, char.MaxValue + 1);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_28_Decimal_Epsilon()
		{
			decimal Org = (decimal)0;
			object Value = Org;
			Assert.IsTrue(Comparison.Increment(ref Value));
			Assert.AreNotEqual(Value, Org);
			decimal Diff = (decimal)Value - Org;
			Assert.AreEqual(Org + Diff / 2, Org);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_29_Double_Epsilon()
		{
			double Org = (double)0;
			object Value = Org;
			Assert.IsTrue(Comparison.Increment(ref Value));
			Assert.AreNotEqual(Value, Org);
			double Diff = (double)Value - Org;
			Assert.AreEqual(Org + Diff / 2, Org);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_30_Single_Epsilon()
		{
			float Org = (float)0;
			object Value = Org;
			Assert.IsTrue(Comparison.Increment(ref Value));
			Assert.AreNotEqual(Value, Org);
			float Diff = (float)Value - Org;
			Assert.AreEqual(Org + Diff / 2, Org);
		}

		[TestMethod]
		public void DBFiles_Inc_Test_31_DateTimeOffset()
		{
			DateTimeOffset Org = DateTimeOffset.Now;
			object Value = Org;
			Assert.IsTrue(Comparison.Increment(ref Value));
			Assert.AreNotEqual(Value, Org);
		}
	}
}
