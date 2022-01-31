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

namespace Epam.FixAntenna.NetCore.Helpers
{
	internal static class ArrayHelper
	{
		internal static void Fill<T>(this T[] array, T value)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			for (var i = 0; i < array.Length; i++)
			{
				array[i] = value;
			}
		}

		internal static T[] Fill<T>(this T[] array, T value, int offset, int length)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			for (var i = offset; i < offset + length; i++)
			{
				array[i] = value;
			}

			return array;
		}
	}
}
