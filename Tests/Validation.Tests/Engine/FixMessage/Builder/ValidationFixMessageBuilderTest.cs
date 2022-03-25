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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.FixMessage;
using Epam.FixAntenna.NetCore.Validation.Utils;
using NUnit.Framework;

namespace Epam.FixAntenna.Validation.Tests.Engine.FIXMessage.Builder
{
	[TestFixture]
	public class ValidationFixMessageBuilderTest
	{
		[SetUp]
		public void SetUp()
		{
			var fixVersionContainer = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix44);
			_fixUtil = FixUtilFactory.Instance.GetFixUtil(fixVersionContainer);
			_builder = ValidationFixMessageBuilder.CreateBuilder(_fixUtil);
		}

		private const string Message =
			"8=FIX.4.2\u00019=244\u000135=i\u000149=TESTI\u000156=TESTA\u000134=4\u000152=20030204-0x8:56:41\u0001117=1116226\u0001296=2\u0001302=0001\u0001311=TESTA\u0001304=5\u0001295=2\u0001299=11\u000155=TESTB\u0001132=11\u0001299=12\u000155=TESTC\u0001133=12\u0001302=0002\u0001311=TESTB\u0001304=5\u0001295=3\u0001299=21\u000155=TESTD\u0001132=13\u0001299=22\u000155=TESTE\u0001133=14\u0001299=23\u000155=TESTF\u0001133=15\u000110=0x1D\u0001";

		private FixUtil _fixUtil;
		private ValidationFixMessageBuilder _builder;

		[Test]
		public void TestGetValidationMessage()
		{
			var fields = RawFixUtil.GetFixMessage(Message.AsByteArray());
			IValidationFixMessage validationFixMessage = _builder.BuildValidationFixMessage(fields);
			Assert.AreEqual(((ValidationFixMessage)validationFixMessage).FullFixMessage.Length,
				validationFixMessage.GetMessageSize());
		}
	}
}