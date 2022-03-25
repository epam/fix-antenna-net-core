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

using System;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;
using Epam.FixAntenna.NetCore.Validation.Utils;
using Epam.FixAntenna.Validation.Tests.Engine.Validation.Additional.Util;

using NUnit.Framework;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Additional
{
	[TestFixture]
	public class AdditionalTest
	{
		[SetUp]
		public virtual void Before()
		{
		}

		[TearDown]
		public virtual void After()
		{
			new ShootDownHook(this).Clear();
		}

		private const string D1 =
			"8=FIX.4.2\u00019=123\u000135=D\u000149=TESTI\u000156=TESTA\u000134=4\u000150=30737\u000197=Y\u000152=20030204-0x8:46:14\u000155=TESTB\u000154=1\u000160=20060217-10:00:00\u000138=4000\u000140=1\u00019702=1\u00019717=1\u000110=113\u0001";

		private const string D2 =
			"8=FIX.4.2\u00019=123\u000135=D\u000149=TESTI\u000156=TESTA\u000134=4\u000150=30737\u000197=Y\u000152=20030204-0x8:46:14\u000111=90001008\u000155=TESTB\u000154=1\u000160=20060217-10:00:00\u000138=4000\u000140=1\u00019702=1\u00019717=1\u000110=113\u0001";

		private ValidationEngine _validationEngine;

		public class ShootDownHook
		{
			private readonly AdditionalTest _outerInstance;

			public ShootDownHook(AdditionalTest outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public virtual void Clear()
			{
				ValidationEngine.PreloadDictionary(FixVersion.Fix42, "fixdic42.xml", true); // restore standard fix dic
				try
				{
					var f = FixUtilFactory.Instance;
					f.ClearResources();
				}
				catch (Exception)
				{
				}
			}
		}

		[Test]
		[Ignore("Additional dictionaries is not supported yet.")]
		public virtual void TestOrderWith11TagIfNotRequired()
		{
			ValidationEngine.PreloadDictionary(FixVersion.Fix42,
				"resources/additional/additional42_11_tag_not_required.xml", true);
			_validationEngine = ValidatorEngineHelper.CreateValidator(FixVersion.Fix42);
			var content = RawFixUtil.GetFixMessage(D2.AsByteArray());
			var errorContainer = _validationEngine.ValidateFixMessage("D", content);
			Assert.IsTrue(errorContainer.IsEmpty);
		}

		[Test]
		[Ignore("Additional dictionaries is not supported yet.")]
		public virtual void TestOrderWith11TagIfRequired()
		{
			ValidationEngine.PreloadDictionary(FixVersion.Fix42,
				"resources/additional/additional42_11_tag_required.xml", true);
			_validationEngine = ValidatorEngineHelper.CreateValidator(FixVersion.Fix42);
			var content = RawFixUtil.GetFixMessage(D2.AsByteArray());
			var errorContainer = _validationEngine.ValidateFixMessage("D", content);
			Assert.IsTrue(errorContainer.IsEmpty);
		}

		[Test]
		[Ignore("Additional dictionaries is not supported yet.")]
		public virtual void TestOrderWithout11TagIfNotRequired()
		{
			ValidationEngine.PreloadDictionary(FixVersion.Fix42,
				"resources/additional/additional42_11_tag_not_required.xml", true);
			_validationEngine = ValidatorEngineHelper.CreateValidator(FixVersion.Fix42);
			var content = RawFixUtil.GetFixMessage(D1.AsByteArray());
			var errorContainer = _validationEngine.ValidateFixMessage("D", content);
			Assert.IsTrue(errorContainer.IsEmpty);
		}

		[Test]
		[Ignore("Additional dictionaries is not supported yet.")]
		public virtual void TestOrderWithout11TagIfRequired()
		{
			ValidationEngine.PreloadDictionary(FixVersion.Fix42,
				"resources/additional/additional42_11_tag_required.xml", true);
			_validationEngine = ValidatorEngineHelper.CreateValidator(FixVersion.Fix42);
			var content = RawFixUtil.GetFixMessage(D1.AsByteArray());
			var errorContainer = _validationEngine.ValidateFixMessage("D", content);
			Assert.IsFalse(errorContainer.IsEmpty);
		}
	}
}