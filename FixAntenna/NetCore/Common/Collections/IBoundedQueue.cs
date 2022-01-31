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

namespace Epam.FixAntenna.NetCore.Common.Collections
{
	internal interface IBoundedQueue<TE> where TE : class
	{
		bool Add(TE e);

		bool Offer(TE e);

		TE Remove();

		TE Poll();

		TE Element();

		TE Peek();

		bool IsEmpty { get; }

		bool IsFull { get; }

		int Size { get; }

		int MaxSize { get; }
	}
}