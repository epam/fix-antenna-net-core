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
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Threading.Runnable;
using Epam.FixAntenna.NetCore.Configuration;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads
{
	internal abstract class AffinitySupportThread : IDisposable, IWorkerThread
	{
		[DllImport("kernel32.dll")]
		private static extern uint GetCurrentThreadId();

		[DllImport("libc.so.6", SetLastError = true)]
		private static extern int sched_setaffinity(int pid, IntPtr cpusetsize, ref ulong cpuset);

		private static readonly ILog Log = LogFactory.GetLog(typeof(AffinitySupportThread));
		private static readonly int UndefinedAffinity = int.Parse(Config.UndefinedAffinity, CultureInfo.InvariantCulture);

		public Thread WorkerThread { get; }

		public AffinitySupportThread(string name)
		{
			WorkerThread = new Thread(Run) { Name = name };
		}

		protected abstract void Run();

		public virtual void Start()
		{
			WorkerThread.Start();
		}

		public virtual void Join()
		{
			WorkerThread.Join();
		}

		public virtual void Shutdown()
		{
			WorkerThread.Abort();
		}

		/// <summary>
		/// This method pins current thread to the first defined parameter with cpu id.
		/// </summary>
		protected void ApplyAffinity(params int[] affinities)
		{
			var cpuId = GetDefinedAffinity(affinities);
			if (IsAffinityDefined(cpuId) && IsCpuIdValid(cpuId))
			{
				try
				{
					SetCurrentThreadAffinity(cpuId);
					Log.Info($"Thread '{Thread.CurrentThread.Name}' was assigned to CPU '{cpuId}'.");
				}
				catch (Exception e)
				{
					Log.Warn("CPU Affinity will not be applied due to the error: " + e.Message, e);
				}
			}
		}

		/// <summary>
		/// Checks that CPU identifier is in valid range.
		/// </summary>
		/// <param name="cpuId">Zero based identifier of CPU core.</param>
		/// <returns>Returns true, if provided value is in range [0..CpuCount-1], false otherwise. </returns>
		private bool IsCpuIdValid(int cpuId)
		{
			var validCpuId = cpuId >= 0 && cpuId < Environment.ProcessorCount;

			if (!validCpuId)
				Log.Warn($"Incorrect CPU ID: '{cpuId}'. CPU Affinity will not be applied.");

			return validCpuId;
		}

		/// <param name="affinities"> </param>
		/// <returns> first defined affinity or undefined value if all values are undefined </returns>
		private static int GetDefinedAffinity(params int[] affinities)
		{
			foreach (var affinity in affinities)
			{
				if (UndefinedAffinity != affinity)
				{
					return affinity;
				}
			}

			return UndefinedAffinity;
		}

		protected static bool IsAffinityDefined(int affinity)
		{
			return UndefinedAffinity != affinity;
		}

		private static void SetCurrentThreadAffinity(int cpuId)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				var cpuMask = 1UL << cpuId;
				sched_setaffinity(0, new IntPtr(sizeof(ulong)), ref cpuMask);
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				var currentThreadId = GetCurrentThreadId();
				var threads = Process.GetCurrentProcess().Threads;
				foreach (ProcessThread thread in threads)
				{
					if (thread.Id == currentThreadId)
					{
						thread.ProcessorAffinity = new IntPtr(1 << cpuId);
						break;
					}
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
			}
		}
	}
}