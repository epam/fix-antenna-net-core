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

using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.AdminTool.Builder.Util
{
	/// <summary>
	/// Tag helper class.
	/// </summary>
	internal sealed class TagUtil
	{
		/// <summary>
		/// Removes tag from tags list.
		/// </summary>
		/// <param name="tag"> the tag </param>
		/// <param name="tags"> the fix fields </param>
		public static void RemoveTag(int tag, FixMessage tags)
		{
			tags.RemoveTag(tag);
		}

		/// <summary>
		/// Removes list of tags from tagsList.
		/// </summary>
		/// <param name="tags"> the array of tags </param>
		/// <param name="tagsList"> the fix fields </param>
		public static void RemoveTags(int[] tags, FixMessage tagsList)
		{
			if (tags == null || tags.Length == 0)
			{
				return;
			}
			foreach (var tag in tags)
			{
				tagsList.RemoveTag(tag);
			}
		}
	}
}