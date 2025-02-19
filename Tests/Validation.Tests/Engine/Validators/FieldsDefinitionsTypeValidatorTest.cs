// Copyright (c) 2021 EPAM Systems
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Dictionary;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Entities;
using Epam.FixAntenna.NetCore.Validation.Error;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.NetCore.Validation.Validators;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Fix.Validation.Engine.Validators
{
	[TestFixture]
	internal class FieldsDefinitionsTypeValidatorTest : AbstractValidatorTst
	{
		[SetUp]
		public virtual void Before()
		{
			Validator = GetValidator(FixVersion.Fix50Sp2);
		}

		public override IValidator GetValidator(FixVersion fixVersion)
		{
			var versionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			var fixtContainer = FixVersionContainer.GetFixVersionContainer(FixVersion.Fixt11);
			FixUtil = FixUtilFactory.Instance.GetFixUtil(fixtContainer, versionContainer);
			return new FieldsDefinitionsTypeValidator(FixUtil);
		}

		/// <summary>
		/// create dictionary
		/// </summary>
		private DictionaryTypes Create50Dictionary()
		{
			var versionContainer = FixVersionContainer.GetFixVersionContainer(FixVersion.Fixt11);
			var appVersionContainer = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix50);
			return FixDictionaryFactory.Instance.GetDictionaries(versionContainer, appVersionContainer);
		}

		private FixUtil GetFixUtil(FixVersion fixVersion, FixVersion appVersion)
		{
			var versionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			var appVersionContainer = FixVersionContainer.GetFixVersionContainer(appVersion);
			return FixUtilFactory.Instance.GetFixUtil(versionContainer, appVersionContainer);
		}

		private FixUtil GetFixUtil(FixVersion fixVersion)
		{
			var versionContainer = FixVersionContainer.GetFixVersionContainer(fixVersion);
			return FixUtilFactory.Instance.GetFixUtil(versionContainer);
		}

		[Test]
		public virtual void TestCheckMultiMethod()
		{
			var multi = new Multi();
			var item = new Item();
			item.Val = "1";
			multi.Item.Add(item);

			ClassicAssert.IsTrue(FieldsDefinitionsTypeValidator.CheckMulti(multi, "1"));

			ClassicAssert.IsTrue(FieldsDefinitionsTypeValidator.CheckMulti(multi, "1 1"));

			ClassicAssert.IsFalse(FieldsDefinitionsTypeValidator.CheckMulti(multi, "1 2"));

			item = new Item();
			item.Val = "2";
			multi.Item.Add(item);
			ClassicAssert.IsTrue(FieldsDefinitionsTypeValidator.CheckMulti(multi, "1 2"));

			ClassicAssert.IsFalse(FieldsDefinitionsTypeValidator.CheckMulti(multi, "1\n2"));
		}

		[Test]
		public virtual void TestInvalidTzTimeOnlyField()
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "7");
			fieldList.AddTag(55, "ABC");
			fieldList.AddTag(1079, "200609-13:0x9:0x1+0x5:30");
			fieldList.AddTag(10, "011");

			var fixMessageBuilder = ValidationFixMessageBuilder.CreateBuilder(FixUtil);
			var fixMessage = fixMessageBuilder.BuildValidationFixMessage(fieldList);

			var errorContainer = Validator.Validate("7", fixMessage, false);
			ClassicAssert.That(errorContainer.Errors,
				Does.Contain(GetError(FixErrorCode.IncorrectDataFormatForValue, -1, "7", fieldList.GetTag(1079))));
		}

		[Test]
		public virtual void TestInvalidValueType()
		{
			var fixUtil = GetFixUtil(FixVersion.Fix50);
			var validator = new FieldsDefinitionsTypeValidator(fixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "C");
			fieldList.AddTag(94, "11");

			var errorContainer = validator.Validate("C", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.IsEmpty, "Error occurred, tag has invalid value.");
			ClassicAssert.That(errorContainer.Errors,
				Does.Contain(GetError(FixErrorCode.IncorrectDataFormatForValue, -1, "C", fieldList.GetTag(94))));
		}

		[Test]
		public virtual void TestMonthYear44DayFormat()
		{
			FixUtil = GetFixUtil(FixVersion.Fix44);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIX.4.4");
			fieldList.AddTag(35, "B");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(313, "20100101");
			fieldList.AddTag(55, "1");
			fieldList.AddTag(200, "20100101");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("B", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestMonthYear44MonthFormat()
		{
			FixUtil = GetFixUtil(FixVersion.Fix44);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIX.4.4");
			fieldList.AddTag(35, "B");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(313, "201001");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("B", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestMonthYear44WeeksFormat()
		{
			FixUtil = GetFixUtil(FixVersion.Fix44);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			for (var i = 1; i <= 5; i++)
			{
				var fieldList = new FixMessage();
				fieldList.AddTag(8, "FIX.4.4");
				fieldList.AddTag(35, "B");
				fieldList.AddTag(34, "1");
				fieldList.AddTag(313, "20100" + i + "w" + i);
				fieldList.AddTag(55, "1");
				fieldList.AddTag(200, "20100" + i + "w" + i);
				fieldList.AddTag(10, "10");

				var errorContainer = Validator.Validate("B", CreateValidationMessage(fieldList), false);
				ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
					"Error occurred:" + errorContainer.IsPriorityError);
			}

			for (var i = 1; i <= 5; i++)
			{
				var fieldList = new FixMessage();
				fieldList.AddTag(8, "FIX.4.4");
				fieldList.AddTag(35, "B");
				fieldList.AddTag(34, "1");
				fieldList.AddTag(313, "200" + i + "0" + i + "w" + i);
				fieldList.AddTag(55, "1");
				fieldList.AddTag(200, "200" + i + "0" + i + "w" + i);
				fieldList.AddTag(10, "10");

				var errorContainer = Validator.Validate("B", CreateValidationMessage(fieldList), false);
				ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
					"Error occurred:" + errorContainer.IsPriorityError);
			}
		}

		[Test]
		public virtual void TestOutOfRange()
		{
			var fixUtil = GetFixUtil(FixVersion.Fix50);
			var validator = new FieldsDefinitionsTypeValidator(fixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "3");
			fieldList.AddTag(373, "25");

			var errorContainer = validator.Validate("3", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.IsEmpty, "Error occurred, tag has invalid value.");
			ClassicAssert.That(errorContainer.Errors,
				Does.Contain(GetError(FixErrorCode.ValueIncorrectOutOfRangeForTag, -1, "3", fieldList.GetTag(373))));
		}

		[Test]
		public virtual void testTransport_AE_Message()
		{
			FixUtil = GetFixUtil(FixVersion.Fix50);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "AE");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(1137, "6");
			fieldList.AddTag(1132, "20060901-07:39Z");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(1132, "20060901-02:39-05");
			errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(1132, "20060901-15:39+08");
			errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(1132, "20060901-13:09+05:30");
			errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(1132, "20060901-13:09:05:30");
			errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.Errors.Count == 0, "Error must occurred.");
		}

		[Test]
		public virtual void testTransport_AE_Message5sp1()
		{
			FixUtil = GetFixUtil(FixVersion.Fix50Sp1);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "AE");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(1137, "6");
			fieldList.AddTag(1132, "20060901-07:39Z");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(1132, "20060901-02:39-05");
			errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(1132, "20060901-15:39+08");
			errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(1132, "20060901-13:09+05:30");
			errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(1132, "20060901-13:09:05:30");
			errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.Errors.Count == 0, "Error must occurred.");
		}

		[Test]
		public virtual void testTransport_AE_Message5sp2()
		{
			FixUtil = GetFixUtil(FixVersion.Fix50Sp2);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "AE");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(1137, "6");
			fieldList.AddTag(1132, "20060901-07:39Z");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(1132, "20060901-02:39-05");
			errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(1132, "20060901-15:39+08");
			errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(1132, "20060901-13:09+05:30");
			errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(1132, "20060901-13:09:05:30");
			errorContainer = Validator.Validate("AE", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.Errors.Count == 0, "Error must occurred.");
		}

		[Test]
		public virtual void testTransport_s_Message()
		{
			FixUtil = GetFixUtil(FixVersion.Fix50Sp1);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "s");
			fieldList.AddTag(18, "1");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("s", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void testTransport_V_Message()
		{
			FixUtil = GetFixUtil(FixVersion.Fixt11, FixVersion.Fix50);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "V");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(1137, "8");
			fieldList.AddTag(200, "20081111");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("V", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(200, "20081111");
			errorContainer = Validator.Validate("V", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(200, "200811w1");
			errorContainer = Validator.Validate("V", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(200, "20080229");
			errorContainer = Validator.Validate("V", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(200, "20080231");
			errorContainer = Validator.Validate("V", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.Errors.Count == 0, "Error must occurred.");

			fieldList.Set(200, "200802w8");
			errorContainer = Validator.Validate("V", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.Errors.Count == 0, "Error must occurred.");
		}

		[Test]
		public virtual void testTransport_V_Message43()
		{
			FixUtil = GetFixUtil(FixVersion.Fix43);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIX.4.3");
			fieldList.AddTag(35, "V");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(1137, "8");
			fieldList.AddTag(200, "201001");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("V", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(200, "200811");
			errorContainer = Validator.Validate("V", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(200, "200811");
			errorContainer = Validator.Validate("V", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(200, "200806");
			errorContainer = Validator.Validate("V", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);

			fieldList.Set(200, "20080112");
			errorContainer = Validator.Validate("V", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.Errors.Count == 0, "Error must occurred.");

			fieldList.Set(200, "200802w8");
			errorContainer = Validator.Validate("V", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.Errors.Count == 0, "Error must occurred.");
		}

		[Test]
		public virtual void testTransport_W_Message()
		{
			FixUtil = GetFixUtil(FixVersion.Fixt11, FixVersion.Fix50);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "W");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(1137, "8");
			fieldList.AddTag(276, "1 1 1 1 A 1");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("W", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestTransportHb41()
		{
			FixUtil = GetFixUtil(FixVersion.Fixt11, FixVersion.Fix41);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "0");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(60, "20090311-16:32:10"); // time
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("0", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestTransportHb41InvalidTime()
		{
			FixUtil = GetFixUtil(FixVersion.Fix41);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "0");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(60, "20090311-16:32:10:900"); // time invalid
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("0", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.Errors.Count == 0, "Error must exists");
		}

		[Test]
		public virtual void TestTransportLogon40()
		{
			FixUtil = GetFixUtil(FixVersion.Fixt11, FixVersion.Fix40);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "A");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(1137, "2");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("A", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestTransportLogon41()
		{
			FixUtil = GetFixUtil(FixVersion.Fixt11, FixVersion.Fix41);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "A");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(1137, "3");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("A", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestTransportLogon42()
		{
			FixUtil = GetFixUtil(FixVersion.Fixt11, FixVersion.Fix42);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "A");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(60, "20090311-16:32:10"); // utc timestamp
			fieldList.AddTag(1137, "4");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("A", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestTransportLogon43()
		{
			FixUtil = GetFixUtil(FixVersion.Fixt11, FixVersion.Fix43);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "A");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(1137, "5");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("A", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestTransportLogon44()
		{
			FixUtil = GetFixUtil(FixVersion.Fixt11, FixVersion.Fix44);
			Validator = new FieldsDefinitionsTypeValidator(FixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "A");
			fieldList.AddTag(34, "1");
			fieldList.AddTag(1137, "6");
			fieldList.AddTag(10, "10");

			var errorContainer = Validator.Validate("A", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred:" + errorContainer.IsPriorityError);
		}

		[Test]
		public virtual void TestTzTimeOnlyField()
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "7");
			fieldList.AddTag(55, "ABC");
			fieldList.AddTag(1079, "10:34Z");
			fieldList.AddTag(10, "011");

			var errorContainer = Validator.Validate("7", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.IsEmpty, "Error occurred.");
		}

		[Test]
		public virtual void TestValidateEmptyExecInstField()
		{
			var fixUtil = GetFixUtil(FixVersion.Fix50);
			var validator = new FieldsDefinitionsTypeValidator(fixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "7");
			fieldList.AddTag(18, " ");

			var errorContainer = validator.Validate("7", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.IsEmpty, "Error occurred, tag has invalid value.");
			ClassicAssert.That(errorContainer.Errors,
				Does.Contain(GetError(FixErrorCode.ValueIncorrectOutOfRangeForTag, -1, "7", fieldList.GetTag(18))));
		}

		[Test]
		public virtual void TestValidateEmptyExecInstFieldWithSpaces()
		{
			var fixUtil = GetFixUtil(FixVersion.Fix50);
			var validator = new FieldsDefinitionsTypeValidator(fixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "7");
			fieldList.AddTag(18, "1   ");

			var errorContainer = validator.Validate("7", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.IsEmpty, "Error occurred, tag has invalid value.");
			ClassicAssert.That(errorContainer.Errors,
				Does.Contain(GetError(FixErrorCode.ValueIncorrectOutOfRangeForTag, -1, "7", fieldList.GetTag(18))));
		}

		[Test]
		public virtual void TestValidateEmptyExecInstFieldWithSpaces2()
		{
			var fixUtil = GetFixUtil(FixVersion.Fixt11, FixVersion.Fix50);
			var validator = new FieldsDefinitionsTypeValidator(fixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "7");
			fieldList.AddTag(18, "  1");

			var errorContainer = validator.Validate("7", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsFalse(errorContainer.IsEmpty, "Error occurred, tag has invalid value.");
			ClassicAssert.That(errorContainer.Errors,
				Does.Contain(GetError(FixErrorCode.ValueIncorrectOutOfRangeForTag, -1, "7", fieldList.GetTag(18))));
		}

		[Test]
		public virtual void TestValidateExecInstField()
		{
			var dictionaries = Create50Dictionary();

			// get fix dic collection characteristic
			var fixdic = (Fixdic)dictionaries.Dictionaries[0].Clone();

			var fixUtil = GetFixUtil(FixVersion.Fixt11, FixVersion.Fix50);
			var validator = new FieldsDefinitionsTypeValidator(fixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "7");
			fieldList.AddTag(18, "1 1");

			var errorContainer = validator.Validate("7", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.IsEmpty, "Error occurred:" + errorContainer.IsPriorityError);

			// compare fix dic collection characteristic
			ClassicAssert.IsTrue(fixdic.Equals(dictionaries.Dictionaries[0]), "Fielddef collection must be equals.");
		}

		[Test]
		public virtual void TestValidateExecInstField1()
		{
			var dictionaries = Create50Dictionary();
			var fixdic = (Fixdic)dictionaries.Dictionaries[0].Clone();

			var fixUtil = GetFixUtil(FixVersion.Fix50);
			var validator = new FieldsDefinitionsTypeValidator(fixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "7");
			fieldList.AddTag(18, "1");

			for (var i = 0; i < 10; i++)
			{
				var errorContainer = validator.Validate("7", CreateValidationMessage(fieldList), false);
				ClassicAssert.IsTrue(errorContainer.IsEmpty, "Error exists");
				ClassicAssert.IsTrue(errorContainer.Errors.Count == 0, "Error container must have not any error.");
			}

			ClassicAssert.IsNotNull(fixdic.Equals(dictionaries.Dictionaries[0]), "Fielddef must exists");
		}

		[Test]
		public virtual void TestValidateLanguageField()
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "B");
			fieldList.AddTag(1474, "uk");

			var errorContainer = Validator.Validate("7", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0,
				"Error occurred: " + errorContainer.Errors);
		}

		[Test]
		public virtual void TestValidateMultipleValueExecInstField()
		{
			var fixUtil = GetFixUtil(FixVersion.Fix50);
			var validator = new FieldsDefinitionsTypeValidator(fixUtil);

			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "7");
			fieldList.AddTag(18, "1 2");

			var errorContainer = validator.Validate("7", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.IsEmpty, "Error occurred, tag has invalid value.");
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0, "Error container must have not any error.");
		}

		[Test]
		public virtual void TestValidateXmlDataField()
		{
			var fieldList = new FixMessage();
			fieldList.AddTag(8, "FIXT.1.1");
			fieldList.AddTag(35, "B");
			fieldList.AddTag(1185, "1");

			var errorContainer = Validator.Validate("7", CreateValidationMessage(fieldList), false);
			ClassicAssert.IsTrue(errorContainer.Errors.Count == 0, "Error occurred.");
		}
	}
}