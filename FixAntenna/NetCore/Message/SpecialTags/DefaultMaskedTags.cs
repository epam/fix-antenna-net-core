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

namespace Epam.FixAntenna.NetCore.Message.SpecialTags
{
	internal class DefaultMaskedTags : IMaskedTags
	{
		public static readonly int[] Tags = { 554, 925 };

		/// <summary>
		/// Static instance of DefaultMaskedTags
		/// </summary>
		public static IMaskedTags Instance = new DefaultMaskedTags();

		/// <inheritdoc/>
		public bool IsTagListed(int tag)
		{
			switch (tag)
			{
				case 554:
				case 925:
					return true;
				default:
					return false;
			}
		}

		/// <inheritdoc/>
		public int[] GetMaskedTags()
		{
			return Tags;
		}
	}
}