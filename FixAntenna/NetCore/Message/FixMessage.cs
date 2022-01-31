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

namespace Epam.FixAntenna.NetCore.Message
{
	/// <summary>
	/// The FIX message.
	/// </summary>
	public class FixMessage : FixMessageAdapter, ITagList
	{
		public FixMessage()
		{
		}

		protected internal FixMessage(bool isUserOwned) : base(isUserOwned)
		{
		}

		public ITagList Clone()
		{
			return DeepClone(IsOriginatingFromPool, IsUserOwned);
		}

		public virtual void ReleaseInstance()
		{
			if (GetRepeatingGroupStorage() != null)
			{
				GetRepeatingGroupStorage().ClearRepeatingGroupStorage();
			}

			SetFixVersion(null);
			if (IsOriginatingFromPool)
			{
				RawFixUtil.ReturnObj(this);
			}
		}

		protected internal sealed override FixMessage MakeStandalone()
		{
			if (Standalone)
			{
				return this;
			}

			SwitchToStandalone();
			return this;
		}

		public FixMessage DeepClone(bool borrowFromPool, bool isNewObjectUserOwned)
		{
			var cloned = borrowFromPool
				? FixMessageFactory.NewInstanceFromPool(isNewObjectUserOwned)
				: NewEmptyInstance(isNewObjectUserOwned);

			DeepCopyTo(cloned);
			return cloned;
		}

		protected virtual FixMessage NewEmptyInstance(bool isNewObjectUserOwned)
		{
			return new FixMessage(isNewObjectUserOwned);
		}

		protected sealed override IList<IDictionary<int, TagValue>> NotifyInvalidMessage(int rgTag,
			int rgFirstTag)
		{
			throw new InvalidMessageException(this, "First tag after RG " + rgTag + " tag must be " + rgFirstTag);
		}
	}
}