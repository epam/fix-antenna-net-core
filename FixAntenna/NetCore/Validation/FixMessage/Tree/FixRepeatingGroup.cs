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

namespace Epam.FixAntenna.NetCore.Validation.FixMessage.Tree
{
	internal class FixRepeatingGroup
	{
		public virtual IList<FixEntry> Entries { get; }
		public virtual int TagId { get; }

		public FixRepeatingGroup(int tagId)
		{
			TagId = tagId;
			Entries = new List<FixEntry>();
		}

		public FixRepeatingGroup(int tagId, IList<FixEntry> entries)
		{
			TagId = tagId;
			Entries = entries;
		}
	}
}