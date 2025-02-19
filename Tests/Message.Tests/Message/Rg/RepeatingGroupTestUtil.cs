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
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Rg;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.Message.Tests.Rg
{
	internal class RepeatingGroupTestUtil
	{
		internal static void ValidateRepeatingGroup(FixMessage msg, string groupContent, RepeatingGroup group)
		{
			var tags = groupContent.Split("\u0001", true);
			var leadingTag = Convert.ToInt32(tags[0].Split("=", true)[0]);
			var entriesCount = Convert.ToInt32(tags[0].Split("=", true)[1]);
			var delimTag = Convert.ToInt32(tags[1].Split("=", true)[0]);
			if (group == null)
			{
				group = msg.GetRepeatingGroup(leadingTag);
			}

			ClassicAssert.AreEqual(entriesCount, group.GetLeadingTagValue());

			var currentTagIndex = 1;
			var notSkippedEntries = 0;
			for (var entryIndex = 0; entryIndex < @group.Count; entryIndex++)
			{
				var entry = group.GetEntry(entryIndex);
				if (entry.Count == 0)
				{
					continue;
				}

				notSkippedEntries++;
				do
				{
					var expectedValue = tags[currentTagIndex].Split("=", true)[1];
					string actualValue;
					var tagId = Convert.ToInt32(tags[currentTagIndex].Split("=", true)[0]);
					if (entry.IsGroupTag(tagId))
					{
						var subGroup = entry.GetRepeatingGroup(tagId);
						var groupSize = subGroup.GetLeadingTagValue();
						actualValue = groupSize.ToString();
					}
					else
					{
						actualValue = entry.GetTagValueAsString(tagId);
					}

					ClassicAssert.AreEqual(expectedValue, actualValue, "try get value of tag " + tagId);
					currentTagIndex++;
				} while (currentTagIndex < tags.Length &&
						Convert.ToInt32(tags[currentTagIndex].Split("=", true)[0]) != delimTag);
			}

			ClassicAssert.AreEqual(notSkippedEntries, entriesCount, "Skipped more then actual entry added");
		}
	}
}