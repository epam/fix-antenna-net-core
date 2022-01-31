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

using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.Validation.FixMessage.Tree
{
	internal class FixEntry
	{
		public virtual IList<TagValue> Fields { get; }
		public virtual IList<FixRepeatingGroup> RepeatingGroups { get; }

		public FixEntry()
		{
			Fields = new List<TagValue>();
			RepeatingGroups = new List<FixRepeatingGroup>();
		}

		public FixEntry(IList<TagValue> fields, IList<FixRepeatingGroup> repeatingGroups)
		{
			Fields = fields;
			RepeatingGroups = repeatingGroups;
		}
	}
}