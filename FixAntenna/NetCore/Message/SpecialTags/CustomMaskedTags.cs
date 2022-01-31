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
using System.Collections.Generic;
using System.Linq;

namespace Epam.FixAntenna.NetCore.Message.SpecialTags
{
	internal class CustomMaskedTags : IMaskedTags
	{
		private readonly int[] _maskedTags;

		private CustomMaskedTags(HashSet<int> tags)
		{
			_maskedTags = new int[tags.Count];
			tags.CopyTo(_maskedTags);
			Array.Sort(_maskedTags);
		}

		/// <summary>
		/// Creates instance of CustomMaskedTags, always include default masked tags: 554, 925.
		/// </summary>
		/// <param name="tags"></param>
		private CustomMaskedTags(string tags) : this(ParseTags(tags)) {}

		/// <summary>
		/// Creates instance of CustomMaskedTags, always include default masked tags: 554, 925.
		/// </summary>
		/// <param name="tags"></param>
		/// <returns></returns>
		public static IMaskedTags Create(string tags)
		{
			var customTags = new CustomMaskedTags(tags);
			return customTags.GetMaskedTags().SequenceEqual(DefaultMaskedTags.Tags) ? DefaultMaskedTags.Instance : customTags;
		}

		/// <inheritdoc/>
		public bool IsTagListed(int tag)
		{
			return Array.BinarySearch(_maskedTags, tag) >= 0;
		}

		/// <inheritdoc/>
		public int[] GetMaskedTags()
		{
			return _maskedTags;
		}

		private static HashSet<int> ParseTags(string stringTags)
		{
			var parts = stringTags.Split(new []{'.', ',', ' '}, StringSplitOptions.RemoveEmptyEntries);
			var tags = new HashSet<int>();
			foreach (var tag in parts)
			{
				if (int.TryParse(tag.Trim(), out var parsedTag))
					tags.Add(parsedTag);
			}
			// append default tags
			tags.UnionWith(DefaultMaskedTags.Tags);
			return tags;
		}
	}
}