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
using System.Threading;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Threading.Queue;
using Epam.FixAntenna.NetCore.Common.Threading.Runnable;

namespace Epam.FixAntenna.NetCore.Common.Threading
{
	internal sealed class ThreadPool
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(ThreadPool));

		private readonly ISimpleBlockingQueue<IRunnableObject> _taskQueue;
		private readonly IList<PoolThread> _threads;
		private volatile bool _isStopped;

		public ThreadPool(int noOfThreads, in ISimpleBlockingQueue<IRunnableObject> queue) : this(noOfThreads, null,
			queue)
		{
		}

		public ThreadPool(int noOfThreads, string threadNamePrefix, in ISimpleBlockingQueue<IRunnableObject> queue)
		{
			_taskQueue = queue;
			_threads = new List<PoolThread>(noOfThreads);
			for (var i = 0; i < noOfThreads; i++)
			{
				var threadName = string.IsNullOrEmpty(threadNamePrefix) ? null : $"{threadNamePrefix}-{i}";
				_threads.Add(new PoolThread(this, threadName));
			}
		}

		public void Execute(IRunnableObject task)
		{
			if (_isStopped)
			{
				throw new InvalidOperationException("ThreadPool is stopped");
			}

			_taskQueue.Put(task);
		}

		public void Stop(in bool interrupt)
		{
			_isStopped = true;
			foreach (var thread in _threads)
			{
				thread.StopTread(interrupt);
			}
		}

		private sealed class PoolThread
		{
			private readonly string _name;
			private readonly ThreadPool _pool;
			private readonly Thread _runThread;

			private volatile bool _isStopped;
			private readonly object _sync = new object();

			internal PoolThread(ThreadPool pool, string name)
			{
				_pool = pool;
				_runThread = new Thread(Run) {IsBackground = true};

				if (!string.IsNullOrEmpty(name))
				{
					_name = name;
					_runThread.Name = _name;
				}

				_runThread.Start();
			}

			private void Run()
			{
				while (!_isStopped)
				{
					try
					{
						_pool._taskQueue.Take().Run();
					}
					catch (ThreadInterruptedException)
					{
						Log.Trace($"Thread {_name} was interrupted.");
						break;
					}
					catch (ThreadAbortException)
					{
						Log.Trace($"Thread {_name} was aborted.");
						break;
					}
					catch (Exception e)
					{
						if (Log.IsDebugEnabled)
						{
							Log.Warn("Thread pool exception. Thread name: " + _name + ". Cause " + e.Message, e);
						}
						else
						{
							Log.Warn("Thread pool exception. Thread name: " + _name + ". Cause " + e.Message);
						}

						//but keep pool thread alive.
					}
				}
			}

			public void StopTread(in bool interrupt)
			{
				lock (_sync)
				{
					_isStopped = true;
					if (interrupt)
					{
						_runThread.Interrupt(); //break pool thread out of take() call.
					}
				}
			}
		}
	}
}