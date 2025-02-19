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
using Epam.FixAntenna.NetCore.Message;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests.Validators
{
	internal abstract class AbstractGetTagValueAsXxxDateValidator : AbstractFixMessageGetAddSetValidator
	{
		public virtual void ClassicAssertCalendarsEquals(DateTimeOffset expected, DateTimeOffset actual, string format)
		{
			ClassicAssert.AreEqual(expected.ToString(format), actual.ToString(format), GetValidatorName());
		}

		public override FixMessage AddTag(FixMessage msg, int tagId, int occurrence)
		{
			return null; //To change body of implemented methods use File | Settings | File Templates.
		}

		public override FixMessage AddTagAtIndex(FixMessage msg, int index, int tagId, int occurrence)
		{
			return null; //To change body of implemented methods use File | Settings | File Templates.
		}

		public override FixMessage SetTag(FixMessage msg, int tagId, int occurrence)
		{
			return null; //To change body of implemented methods use File | Settings | File Templates.
		}

		public override FixMessage SetTagWithOccurrence(FixMessage msg, int tagId, int occurrence)
		{
			return null; //To change body of implemented methods use File | Settings | File Templates.
		}

		public override FixMessage SetTagAtIndex(FixMessage msg, int index, int occurrence)
		{
			return null; //To change body of implemented methods use File | Settings | File Templates.
		}
	}
}