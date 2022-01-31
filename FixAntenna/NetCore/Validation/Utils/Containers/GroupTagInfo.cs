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

namespace Epam.FixAntenna.NetCore.Validation.Utils.Containers
{
	internal class GroupTagInfo
	{
		public const int DefaultRootGroupTag = -1;

		private readonly bool _isGroupTagConflict;
		private readonly int _rootGroupTag = DefaultRootGroupTag;

		public GroupTagInfo(bool groupTag, int rootGroupTag)
		{
			_rootGroupTag = rootGroupTag;
			_isGroupTagConflict = groupTag;
		}

		public virtual bool IsGroupTag()
		{
			return _isGroupTagConflict;
		}

		public virtual int GetRootGroupTag()
		{
			return _rootGroupTag;
		}
	}
}