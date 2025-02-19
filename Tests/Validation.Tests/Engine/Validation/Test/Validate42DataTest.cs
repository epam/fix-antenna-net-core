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
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Validation.Tests.Engine.Validation.Test
{
	[TestFixture]
	internal class Validate42DataTest : GenericValidationTestStub
	{
		[TearDown]
		public virtual void After()
		{
			ClassicAssert.IsTrue(Errors.IsEmpty, Errors.Errors.ToReadableString());
		}

		private static readonly ILog Log = LogFactory.GetLog(typeof(Validate42DataTest));
		private const bool ErrorShouldOccur = false;

		public override FixInfo GetFixInfo()
		{
			return new FixInfo(FixVersion.Fix42);
		}

		[Test]
		[TestCaseSource(nameof(GetResourcePaths), new object[] { "fixdatafix42" })]
		public virtual void ValidateAutoData(string resourceName)
		{
			using (var data = ResourceLoader.DefaultLoader.LoadResource(resourceName))
			{
				Validate(data, ErrorShouldOccur);
				Log.Info("Passed " + resourceName);
			}
		}

		[Test]
		[TestCaseSource(nameof(GetResourcePaths), new object[] { "FIX42" })]
		public virtual void ValidateCustomData(string resourceName)
		{
			using (var data = ResourceLoader.DefaultLoader.LoadResource(resourceName))
			{
				Validate(data, ErrorShouldOccur);
				Log.Info("Passed " + resourceName);
			}
		}
	}
}