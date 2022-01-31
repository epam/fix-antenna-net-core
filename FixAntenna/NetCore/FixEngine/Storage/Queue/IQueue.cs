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

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.Queue
{
	/// <summary>
	/// IQueue interface.
	/// </summary>
	internal interface IQueue<T> : IDisposable where T : IQueueable
	{
		/// <summary>
		/// Initializes queue. That includes restore of previously
		/// saved queue content for persistent queues.
		/// </summary>
		void Initialize();

		/// <summary>
		/// Add object to the queue.
		/// </summary>
		/// <param name="element"> the element </param>
		/// <returns> true if element added successfully. </returns>
		bool Add(T element);

		/// <summary>
		/// Add object to the queue out of turn.
		/// </summary>
		/// <param name="element"> the element </param>
		/// <returns> true if element added successfully. </returns>
		bool AddOutOfTurn(T element);

		/// <summary>
		/// Removes the elements which were added by <seealso cref="AddOutOfTurn"/>. </summary>
		/// <param name="elementConsumer"> callback for each removed message. It is called after removing next message. </param>
		void ClearOutOfTurn(Action<T> elementConsumer);

		bool IsAllEmpty { get; }

		/// <summary>
		/// Poll object from the queue (doesn't remove it from queue yet!).
		/// </summary>
		/// <returns> the head of this queue, or <tt>null</tt> if this queue is empty </returns>
		/// <seealso cref="Commit"> </seealso>
		T Poll();

		/// <summary>
		/// Removes polled object from the queue.
		/// If nothing was polled - throws IllegalStateException
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		void Commit();

		/// <summary>
		/// Checks if queue is currently empty.
		/// </summary>
		/// <value> true if empty </value>
		bool IsEmpty { get; }

		/// <summary>
		/// Returns current queue size.
		/// </summary>
		/// <value> queue size </value>
		int Size { get; }

		/// <summary>
		/// Returns current total queue size. The total size of the usual queue and the OutOfTurn queue.
		/// </summary>
		/// <value> queue size </value>
		int TotalSize { get; }

		/// <summary>
		/// Clears the queue.
		/// </summary>
		void Clear();

		/// <summary>
		/// Gracefully shutdowns queue.
		/// </summary>
		void Shutdown();

		/// <summary>
		/// Out of turn messages only mode.
		/// </summary>
		bool OutOfTurnOnlyMode { get; set; }

		/// <summary>
		/// Notify a new application message exists.
		/// <para>
		/// The methods works only if turn "mode on" is off.
		/// </para>
		/// </summary>
		/// <seealso cref="OutOfTurnOnlyMode"></seealso>
		void NotifyAllApplication();

		/// <summary>
		/// Notify a new session message exists.
		/// </summary>
		void NotifyAllSession();

		/// <summary>
		/// Return all objects in the queue as array. Method has no impact on poll / commit operations.
		/// </summary>
		/// <returns> array of <seealso cref="IQueueable"/> </returns>
		IQueueable[] ToArray();
	}
}