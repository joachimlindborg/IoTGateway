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
	public class DBFilesRangeTests
	{
		private RangeInfo range;

		[TestInitialize]
		public void TestInitialize()
		{
			this.range = new RangeInfo("Field");
		}

		[TestMethod]
		public void DBFiles_Range_01_Point()
		{
			Assert.IsTrue(this.range.SetPoint(10));
			Assert.IsFalse(this.range.IsRange);
			Assert.AreEqual(this.range.Point, 10);
		}

		[TestMethod]
		public void DBFiles_Range_02_Inconsistent_Point()
		{
			Assert.IsTrue(this.range.SetPoint(10));
			Assert.IsFalse(this.range.SetPoint(20));
		}

		[TestMethod]
		public void DBFiles_Range_03_MinInclusive()
		{
			Assert.IsTrue(this.range.SetMin(10, true, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.IsRange);
			Assert.AreEqual(this.range.Min, 10);
			Assert.IsTrue(this.range.MinInclusive);
		}

		[TestMethod]
		public void DBFiles_Range_04_MinExclusive()
		{
			Assert.IsTrue(this.range.SetMin(10, false, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.IsRange);
			Assert.AreEqual(this.range.Min, 10);
			Assert.IsFalse(this.range.MinInclusive);
		}

		[TestMethod]
		public void DBFiles_Range_05_Complimentary_MinInclusive()
		{
			Assert.IsTrue(this.range.SetMin(10, false, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.SetMin(20, true, out Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.SetMin(15, true, out Smaller));
			Assert.IsFalse(Smaller);
			Assert.IsTrue(this.range.IsRange);
			Assert.AreEqual(this.range.Min, 20);
			Assert.IsTrue(this.range.MinInclusive);
		}

		[TestMethod]
		public void DBFiles_Range_06_Complimentary_Point_MinInclusive()
		{
			Assert.IsTrue(this.range.SetPoint(10));
			Assert.IsTrue(this.range.SetMin(10, true, out bool Smaller));
			Assert.IsFalse(Smaller);
			Assert.IsFalse(this.range.IsRange);
			Assert.AreEqual(this.range.Point, 10);
			Assert.IsNull(this.range.Min);
		}

		[TestMethod]
		public void DBFiles_Range_07_Complimentary_Point_MinInclusive_2()
		{
			Assert.IsTrue(this.range.SetMin(10, true, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.SetPoint(10));
			Assert.IsFalse(this.range.IsRange);
			Assert.AreEqual(this.range.Point, 10);
			Assert.IsNull(this.range.Min);
		}

		[TestMethod]
		public void DBFiles_Range_08_Inconsistent_Point_MinInclusive()
		{
			Assert.IsTrue(this.range.SetPoint(10));
			Assert.IsFalse(this.range.SetMin(15, true, out bool Smaller));
			Assert.IsFalse(Smaller);
		}

		[TestMethod]
		public void DBFiles_Range_09_Inconsistent_Point_MinInclusive_2()
		{
			Assert.IsTrue(this.range.SetMin(15, true, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsFalse(this.range.SetPoint(10));
		}

		[TestMethod]
		public void DBFiles_Range_10_Inconsistent_Point_MinInclusive_3()
		{
			Assert.IsTrue(this.range.SetPoint(10));
			Assert.IsFalse(this.range.SetMin(10, false, out bool Smaller));
			Assert.IsFalse(Smaller);
		}

		[TestMethod]
		public void DBFiles_Range_11_Inconsistent_Point_MinInclusive_4()
		{
			Assert.IsTrue(this.range.SetMin(10, false, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsFalse(this.range.SetPoint(10));
		}

		[TestMethod]
		public void DBFiles_Range_12_MaxInclusive()
		{
			Assert.IsTrue(this.range.SetMax(10, true, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.IsRange);
			Assert.AreEqual(this.range.Max, 10);
			Assert.IsTrue(this.range.MaxInclusive);
		}

		[TestMethod]
		public void DBFiles_Range_13_MaxExclusive()
		{
			Assert.IsTrue(this.range.SetMax(10, false, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.IsRange);
			Assert.AreEqual(this.range.Max, 10);
			Assert.IsFalse(this.range.MaxInclusive);
		}

		[TestMethod]
		public void DBFiles_Range_14_Complimentary_MaxInclusive()
		{
			Assert.IsTrue(this.range.SetMax(20, false, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.SetMax(10, true, out Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.SetMax(15, true, out Smaller));
			Assert.IsFalse(Smaller);
			Assert.IsTrue(this.range.IsRange);
			Assert.AreEqual(this.range.Max, 10);
			Assert.IsTrue(this.range.MaxInclusive);
		}

		[TestMethod]
		public void DBFiles_Range_15_Complimentary_Point_MaxInclusive()
		{
			Assert.IsTrue(this.range.SetPoint(10));
			Assert.IsTrue(this.range.SetMax(10, true, out bool Smaller));
			Assert.IsFalse(Smaller);
			Assert.IsFalse(this.range.IsRange);
			Assert.AreEqual(this.range.Point, 10);
			Assert.IsNull(this.range.Max);
		}

		[TestMethod]
		public void DBFiles_Range_16_Complimentary_Point_MaxInclusive_2()
		{
			Assert.IsTrue(this.range.SetMax(10, true, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.SetPoint(10));
			Assert.IsFalse(this.range.IsRange);
			Assert.AreEqual(this.range.Point, 10);
			Assert.IsNull(this.range.Max);
		}

		[TestMethod]
		public void DBFiles_Range_17_Inconsistent_Point_MaxInclusive()
		{
			Assert.IsTrue(this.range.SetPoint(15));
			Assert.IsFalse(this.range.SetMax(10, true, out bool Smaller));
			Assert.IsFalse(Smaller);
		}

		[TestMethod]
		public void DBFiles_Range_18_Inconsistent_Point_MaxInclusive_2()
		{
			Assert.IsTrue(this.range.SetMax(10, true, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsFalse(this.range.SetPoint(15));
		}

		[TestMethod]
		public void DBFiles_Range_19_Inconsistent_Point_MaxInclusive_3()
		{
			Assert.IsTrue(this.range.SetPoint(10));
			Assert.IsFalse(this.range.SetMax(10, false, out bool Smaller));
			Assert.IsFalse(Smaller);
		}

		[TestMethod]
		public void DBFiles_Range_20_Inconsistent_Point_MaxInclusive_4()
		{
			Assert.IsTrue(this.range.SetMax(10, false, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsFalse(this.range.SetPoint(10));
		}

		[TestMethod]
		public void DBFiles_Range_21_Complimentary_MinMax()
		{
			Assert.IsTrue(this.range.SetMin(10, true, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.SetMax(20, true, out Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.IsRange);
			Assert.AreEqual(this.range.Min, 10);
			Assert.AreEqual(this.range.Max, 20);
			Assert.IsTrue(this.range.MinInclusive);
			Assert.IsTrue(this.range.MaxInclusive);
		}

		[TestMethod]
		public void DBFiles_Range_22_Complimentary_MinMax_2()
		{
			Assert.IsTrue(this.range.SetMin(10, true, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.SetMin(12, false, out Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.SetMin(11, true, out Smaller));
			Assert.IsFalse(Smaller);
			Assert.IsTrue(this.range.SetMax(20, true, out Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.SetMax(18, false, out Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.SetMax(19, true, out Smaller));
			Assert.IsFalse(Smaller);
			Assert.IsTrue(this.range.IsRange);
			Assert.AreEqual(this.range.Min, 12);
			Assert.AreEqual(this.range.Max, 18);
			Assert.IsFalse(this.range.MinInclusive);
			Assert.IsFalse(this.range.MaxInclusive);
		}

		[TestMethod]
		public void DBFiles_Range_23_Inconsistent_MinMax()
		{
			Assert.IsTrue(this.range.SetMin(20, true, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsFalse(this.range.SetMax(10, true, out Smaller));
			Assert.IsTrue(Smaller);
		}

		[TestMethod]
		public void DBFiles_Range_24_Inconsistent_MinMax_2()
		{
			Assert.IsTrue(this.range.SetMax(10, true, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsFalse(this.range.SetMin(20, true, out Smaller));
			Assert.IsTrue(Smaller);
		}

		[TestMethod]
		public void DBFiles_Range_25_Inconsistent_MinMax_3()
		{
			Assert.IsTrue(this.range.SetMax(10, true, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsFalse(this.range.SetMin(10, false, out Smaller));
			Assert.IsTrue(Smaller);
		}

		[TestMethod]
		public void DBFiles_Range_26_Inconsistent_MinMax_4()
		{
			Assert.IsTrue(this.range.SetMax(10, false, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsFalse(this.range.SetMin(10, true, out Smaller));
			Assert.IsTrue(Smaller);
		}

		[TestMethod]
		public void DBFiles_Range_27_Inconsistent_MinMax_5()
		{
			Assert.IsTrue(this.range.SetMax(10, false, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsFalse(this.range.SetMin(10, false, out Smaller));
			Assert.IsTrue(Smaller);
		}

		[TestMethod]
		public void DBFiles_Range_28_MinMax_Point()
		{
			Assert.IsTrue(this.range.SetMax(10, true, out bool Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsTrue(this.range.SetMin(10, true, out Smaller));
			Assert.IsTrue(Smaller);
			Assert.IsFalse(this.range.IsRange);
			Assert.AreEqual(this.range.Point, 10);
			Assert.IsNull(this.range.Min);
			Assert.IsNull(this.range.Max);
		}
	}
}
