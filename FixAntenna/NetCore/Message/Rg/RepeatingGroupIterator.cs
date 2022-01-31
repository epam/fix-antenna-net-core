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
using System.Collections;
using System.Collections.Generic;

namespace Epam.FixAntenna.NetCore.Message.Rg
{
	internal class RepeatingGroupIterator : IEnumerator<RepeatingGroup.Entry>
	{
		private readonly RepeatingGroup.Entry _entry = RepeatingGroupPool.Entry;
		private readonly RepeatingGroup _group;
		internal int Pointer = -1;

		public RepeatingGroupIterator(RepeatingGroup group)
		{
			_group = group;
		}

		public bool MoveNext()
		{
			if (Pointer + 1 >= _group.Count)
			{
				return false;
			}

			Pointer++;
			return true;
		}

		public void Reset()
		{
			Pointer = -1;
		}

		public RepeatingGroup.Entry Current
		{
			get
			{
				_group.GetEntry(Pointer, _entry);
				return _entry;
			}
		}

		object IEnumerator.Current => Current;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public virtual bool HasNext()
		{
			return Pointer < _group.Count;
		}

		public virtual RepeatingGroup.Entry Next()
		{
			_group.GetEntry(Pointer++, _entry);
			return _entry;
		}

		public virtual void Remove()
		{
			_group.RemoveEntry(Pointer);
		}

		protected virtual void Dispose(bool disposing)
		{
		}
	}
}