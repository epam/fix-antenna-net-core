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
using System.Linq;
using System.Threading;

namespace Epam.FixAntenna.NetCore.FixEngine.Storage.Queue
{
	/// <summary>
	/// Memory queue implementation.
	/// </summary>
	/// <seealso cref="IQueueable"></seealso>
	internal class InMemoryQueue<T> : IQueue<T> where T : IQueueable
	{
		protected internal readonly LinkedList<T> Application;
		protected internal readonly LinkedList<T> Session;

		private volatile bool _outOfTurnMode;
		private volatile bool _shutdown;
		internal bool ApplicationPolled;
		internal bool SessionPolled;

		/// <summary>
		/// Create the memory queue.
		/// </summary>
		public InMemoryQueue()
		{
			Application = new LinkedList<T>();
			Session = new LinkedList<T>();
		}

		/// <inheritdoc/>
		public virtual void Initialize()
		{
			OutOfTurnOnlyMode = false;
			_shutdown = false;
		}

		/// <inheritdoc/>
		public virtual int Size => _outOfTurnMode ? Session.Count : TotalSize;

		/// <inheritdoc/>
		public virtual int TotalSize => Application.Count + Session.Count;

		/// <inheritdoc/>
		public virtual bool IsEmpty => _outOfTurnMode ? Session.Count == 0 : Application.Count == 0 && Session.Count == 0;

		/// <inheritdoc/>
		public virtual bool IsAllEmpty => Application.Count == 0 && Session.Count == 0;

		/// <inheritdoc/>
		public virtual bool Add(T @object)
		{
			Application.AddLast(@object);
			return true;
		}

		/// <inheritdoc/>
		public virtual bool AddOutOfTurn(T @object)
		{
			Session.AddLast(@object);
			return true;
		}

		/// <inheritdoc/>
		public virtual T Poll()
		{
			if (Session.Count > 0)
			{
				SessionPolled = true;
				return Session.FirstOrDefault();
			}

			ApplicationPolled = true;
			return Application.FirstOrDefault();
		}

		// TODO: remove synchronization
		/// <inheritdoc/>
		public virtual bool OutOfTurnOnlyMode
		{
			get
			{
				lock (this)
				{
					return _outOfTurnMode;
				}
			}
			set
			{
				lock (this)
				{
					var switchedOff = _outOfTurnMode != value && !value;
					_outOfTurnMode = value;

					if (Application.Count > 0 && switchedOff)
					{
						// only when mode is off
						Monitor.PulseAll(this); // we have some application messages
					}
				}
			}
		}

		/// <inheritdoc/>
		public virtual void ClearOutOfTurn(Action<T> elementConsumer)
		{
			if (Session.Count > 0)
			{
				for (var i = 0; i < Session.Count; i++)
				{
					var element = Session.First();
					Session.RemoveFirst();
					elementConsumer(element);
				}
			}
		}

		/// <inheritdoc/>
		public virtual void Commit()
		{
			lock (this)
			{
				if (SessionPolled)
				{
					Session.RemoveFirst();
					SessionPolled = false;
				}
				else if (ApplicationPolled)
				{
					Application.RemoveFirst();
					ApplicationPolled = false;
				}
				else
				{
					throw new InvalidOperationException("Nothing was polled nothing to commit");
				}
			}
		}

		/// <inheritdoc/>
		public virtual void Clear()
		{
			Application.Clear();
			Session.Clear();
		}

		/// <inheritdoc/>
		public virtual void Shutdown()
		{
			_shutdown = true;
		}

		/// <inheritdoc/>
		public virtual void NotifyAllApplication()
		{
			if (!_outOfTurnMode)
			{
				lock (this)
				{
					Monitor.PulseAll(this);
				}
			}
		}

		/// <inheritdoc/>
		public virtual void NotifyAllSession()
		{
			lock (this)
			{
				Monitor.PulseAll(this);
			}
		}

		/// <inheritdoc/>
		public virtual IQueueable[] ToArray()
		{
			var sessionArray = Session.ToArray();
			var applicationArray = Application.ToArray();
			var result = new IQueueable[sessionArray.Length + applicationArray.Length];
			Array.Copy(sessionArray, 0, result, 0, sessionArray.Length);
			Array.Copy(applicationArray, 0, result, sessionArray.Length, applicationArray.Length);
			return result;
		}

		/// <summary>
		/// Returns true if queue is shutdown.
		/// </summary>
		public virtual bool IsShutdown => _shutdown;

		/// <summary>
		/// Return true if last commit was application.
		/// </summary>
		public virtual bool IsApplicationCommit => ApplicationPolled;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				Shutdown();
			}
		}
	}
}