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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.ResourceLoading;
using NUnit.Framework;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test
{
	[TestFixture]
	internal class Validate50Sp2DataTest : GenericValidationTestStub
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(Validate50Sp2DataTest));
		private const bool ErrorShouldOccur = false;

		public override FixInfo GetFixInfo()
		{
			return new FixInfo(FixVersion.Fix50Sp2);
		}

		[Test]
		[TestCaseSource(nameof(GetResourcePaths), new object[] { "fixdatafix50sp2" })]
		public virtual void ValidateAutoData(string resourceName)
		{
			using (var data = ResourceLoader.DefaultLoader.LoadResource(resourceName))
			{
				Validate(resourceName, data, ErrorShouldOccur);
				Assert.IsTrue(Errors.IsEmpty, Errors.Errors.ToReadableString());
				Log.Info("Passed " + resourceName);
			}
		}

		[Test]
		[TestCaseSource(nameof(GetResourcePaths), new object[] { "FIX50sp2." })]
		[Ignore("Ignore CG message - it was removed from protocol")]
		public virtual void ValidateCustomData(string resourceName)
		{
			using (var data = ResourceLoader.DefaultLoader.LoadResource(resourceName))
			{
				Validate(resourceName, data, ErrorShouldOccur);
				Assert.IsTrue(Errors.IsEmpty, Errors.Errors.ToReadableString());
				Log.Info("Passed " + resourceName);
			}
		}

		[Test]
		public virtual void ValidateExecutionReportWithInvalidFieldType()
		{
			Validate("resources/FIX50sp2Invalid/7_invalid_tz_time_only.fix", false, true);
		}
	}
}