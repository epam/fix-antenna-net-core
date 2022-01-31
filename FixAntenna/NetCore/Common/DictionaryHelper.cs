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

namespace Epam.FixAntenna.NetCore.Common
{
	internal static class DictionaryHelper
	{
		/// <summary>
		///  Searches <c>source</c> for <c>key</c> and returns value or <c>default()</c> if <c>key</c> is not found.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="key"></param>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		internal static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			return source.GetValueOrDefault(key, default(TValue));
		}

		/// <summary>
		/// Searches <c>source</c> for <c>key</c> and returns value or <c>defaultValue</c> if <c>key</c> is not found.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		internal static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, TValue defaultValue)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (!source.TryGetValue(key, out var ret))
			{
				return defaultValue;
			}

			return ret;
		}

		/// <summary>
		/// Copies all of the key/value pairs to source array.
		/// If key is already present in <c>source</c> its value is overwritten.  
		/// </summary>
		/// <param name="source"></param>
		/// <param name="collection"></param>
		/// <typeparam name="T1"></typeparam>
		/// <typeparam name="T2"></typeparam>
		/// <exception cref="ArgumentNullException"></exception>
		internal static void PutAll<T1, T2>(this IDictionary<T1, T2> source, IDictionary<T1, T2> collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("Collection is empty.");
			}

			foreach (var keyValuePair in collection)
			{
				source[keyValuePair.Key] = keyValuePair.Value;
			}
		}

		/// <summary>
		/// Adds values to <c>source</c> collection if they are not already present.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="collection"></param>
		/// <typeparam name="T1"></typeparam>
		/// <exception cref="ArgumentNullException"></exception>
		internal static void AddRange<T1>(this ISet<T1> source, ICollection<T1> collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("Collection is empty.");
			}

			foreach (var item in collection)
			{
				if (!source.Contains(item))
				{
					source.Add(item);
				}
			}
		}

		/// <summary>
		/// Removes all of the values of <c>collection</c> from <c>source</c>.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="collection"></param>
		/// <typeparam name="T"></typeparam>
		/// <exception cref="ArgumentNullException"></exception>
		internal static void RemoveAll<T>(this ISet<T> source, ICollection<T> collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("Collection is empty.");
			}

			foreach (var item in collection)
			{
				source.Remove(item);
			}
		}
	}
}