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

namespace Epam.FixAntenna.NetCore.Message
{
	internal class DefaultRawTags : RawFixUtil.IRawTags
	{
		public static readonly int[] DefaultRawTagsSorted = Sort(new[]
			{ 96, 91, 213, 349, 351, 353, 355, 357, 359, 361, 363, 365, 446, 619, 622 });

		public bool IsWithinRawTags(int tag)
		{
			switch (tag)
			{
				case 96:
				case 91:
				case 213:
				case 349:
				case 351:
				case 353:
				case 355:
				case 357:
				case 359:
				case 361:
				case 363:
				case 365:
				case 446:
				case 619:
				case 622:
					return true;
				default:
					return false;
			}
		}

		private static int[] Sort(int[] rawTags)
		{
			Array.Sort(rawTags);
			return rawTags;
		}
	}
}