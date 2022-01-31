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
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.NetCore.Message.Storage
{
	internal class TagValueStorage
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(TagValueStorage));
		private TagValue[] _fields;

		public TagValueStorage(int initialSize)
		{
			_fields = new TagValue[initialSize];
		}

		public virtual void Enlarge(int ratio)
		{
			var newCache = new TagValue[_fields.Length * ratio];
			Array.Copy(_fields, 0, newCache, 0, _fields.Length);
			_fields = newCache;
		}

		public virtual void EnlargeTo(int size)
		{
			if (_fields.Length >= size)
			{
				if (Log.IsTraceEnabled)
				{
					Log.Trace("Skip resizing. New size is smaller than the existing one. Existing: " +
							_fields.Length + ", new:" + size);
				}

				return;
			}

			var newCache = new TagValue[size];
			Array.Copy(_fields, 0, newCache, 0, _fields.Length);

			_fields = newCache;
		}

		public TagValue this[int index]
		{
			get => _fields[index];
			set => _fields[index] = value;
		}

		public virtual void InvalidateCachedField(int i)
		{
			var field = _fields[i];
			if (field != null)
			{
				if (field.IsFromPool)
				{
					ByteArrayPool.ReturnObj(field.Buffer); //TODO: check this!
					RawFixUtil.ReturnObj(field);
				}

				_fields[i] = null;
			}
		}

		//TBD! do accurate!
		public virtual void Shift(int index, int offset, int filledSize)
		{
			if (index < filledSize - 1)
			{
				Array.Copy(_fields, index, _fields, index + offset, filledSize - index - 1);
			}
		}

		public virtual void ShiftBack(int index, int offset, int filledSize)
		{
			Array.Copy(_fields, index + offset, _fields, index, filledSize - index - offset);
			for (var i = 1; i <= offset; i++)
			{
				_fields[filledSize - i] = null;
			}
		}
	}
}