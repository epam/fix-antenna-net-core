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

using System.Threading;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads.Bean
{
	internal class MessageStatistic
	{
		private long _bytesProcessed;
		private long _messagesProcessed;

		public virtual long BytesProcessed => Interlocked.Read(ref _bytesProcessed);

		public virtual long MessagesProcessed => Interlocked.Read(ref _messagesProcessed);

		public virtual void AddMessagesProcessed()
		{
			Interlocked.Increment(ref _messagesProcessed);
		}

		public virtual void AddMessagesProcessed(int messagesProcessed)
		{
			Interlocked.Add(ref _messagesProcessed, messagesProcessed);
		}

		public virtual void AddBytesProcessed(int bytesProcessed)
		{
			Interlocked.Add(ref _bytesProcessed, bytesProcessed);
		}

		public override string ToString()
		{
			return "MessageStatistic{"
						+ ", bytesProcessed=" + Interlocked.Read(ref _bytesProcessed)
						+ ", messagesProcessed=" + Interlocked.Read(ref _messagesProcessed) + '}';
		}
	}
}