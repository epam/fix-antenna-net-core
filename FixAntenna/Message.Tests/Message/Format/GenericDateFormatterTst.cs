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
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message.Format;
using NUnit.Framework;

namespace Epam.FixAntenna.Message.Tests.Format
{
	internal class GenericDateFormatterTst
	{
		public virtual void CheckFormat(IFixDateFormatter formatter, DateTimeOffset cal, string res)
		{
			var buff = new byte[res.Length];
			formatter.Format(cal, buff, 0);
			var actual = StringHelper.NewString(buff);

			Assert.AreEqual(res, actual, "Formatting date is wrong");
		}
	}
}