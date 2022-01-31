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

using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Threading.Queue;
using Epam.FixAntenna.NetCore.Common.Threading.Runnable;
using Epam.FixAntenna.NetCore.FixEngine.Manager.Scheduler;
using Epam.FixAntenna.NetCore.FixEngine.Manager.Tasks;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

using ThreadPool = Epam.FixAntenna.NetCore.Common.Threading.ThreadPool;

namespace Epam.FixAntenna.NetCore.FixEngine.Manager
{
	/// <summary>
	/// This singleton contains all registered FIX sessions in this application instance.
	/// Each class in the list backed up by the WeakReference
	/// so it may return bogus values for sessions that just expired and
	/// were removed from the list because of that.
	/// SessionState will be returned as DEAD.
	/// </summary>
	/// <seealso cref="SessionState.Dead">for such sessions.></seealso>
	internal class FixSessionManager
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(FixSessionManager));

		private readonly RarelyChangeList<IFixSessionListListener> _listenersList = new RarelyChangeList<IFixSessionListListener>();

		private ThreadPool _service;
		private FixedRunnablePool<IRunnableObject> _runnablePool;

		private CopyOnEditArrayList<IExtendedFixSession> _list;
		private readonly IList<ISessionManagerTask> _tasks = new List<ISessionManagerTask>();

		private TaskLaunchThread _taskThread;
		private readonly SchedulerManager _schedulerManager = new SchedulerManager();

		private bool _running;

		/// <summary>
		/// Gets the <see cref="FixSessionManager"/> instance.
		/// </summary>
		public static FixSessionManager Instance { get; } = new FixSessionManager();

		/// <summary>
		/// Gets or sets wait time.
		/// </summary>
		public int WaitTime { get; set; } = 1000;

		/// <summary>
		/// Adds a new session manager task.
		/// </summary>
		/// <param name="task"> the task to add </param>
		public void AddTask(ISessionManagerTask task)
		{
			lock (_tasks)
			{
				_tasks.Add(task);
			}
		}

		/// <summary>
		/// Removes the task.
		/// </summary>
		/// <param name="task"> the task to remove </param>
		public bool RemoveTask(ISessionManagerTask task)
		{
			lock (_tasks)
			{
				return _tasks.Remove(task);
			}
		}

		/// <summary>
		/// Added scheduled task.
		/// </summary>
		/// <param name="schedulerTask">     the task </param>
		/// <param name="scheduleTimestamp"> the timestamp </param>
		/// <exception cref="InvalidOperationException"> if task was scheduled. </exception>
		public void ScheduleTask(SchedulerTask schedulerTask, long scheduleTimestamp)
		{
			_schedulerManager.Schedule(schedulerTask, scheduleTimestamp);
		}

		/// <summary>
		/// Added scheduled task.
		/// </summary>
		/// <param name="schedulerTask">     the task </param>
		/// <param name="scheduleTimestamp"> the timestamp </param>
		/// <param name="period">            the period </param>
		/// <exception cref="InvalidOperationException"> if task was scheduled. </exception>
		public void ScheduleTask(SchedulerTask schedulerTask, long scheduleTimestamp, int period)
		{
			_schedulerManager.Schedule(schedulerTask, scheduleTimestamp, period);
		}

		/// <summary>
		/// Remove scheduled task.
		/// </summary>
		public void CancelScheduleTask(SchedulerTask schedulerTask)
		{
			_schedulerManager.Cancel(schedulerTask);
		}

		private FixSessionManager()
		{
			_list = new CopyOnEditArrayList<IExtendedFixSession>();
			
			AddTask(new TestRequestTask());
			AddTask(new InactivityCheckTask());

			_running = true;

			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
		}

		private void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			Shutdown();
		}

		/// <summary>
		/// Initialize new FixSessionManager.
		/// </summary>
		public static void Init()
		{
			PrintEnvironment();
		}

		/// <summary>
		/// Shutdown the FixAntenna engine, i.e. FixSessionManager, LicenseManager, SchedulerManager.
		/// </summary>
		/// <remarks>
		/// FixSessionManager shutting down automatically when the application exits.
		/// Use this method only if you need to force shutdown the engine.
		/// </remarks>
		public void Shutdown()
		{
			if (_running)
			{
				_taskThread?.StopService(true);
				_schedulerManager.Shutdown();
				_running = false;
				NLog.LogManager.Shutdown();
			}
		}
		
		private static void PrintEnvironment()
		{
			var strBuilder = new StringBuilder("System properties:\n");
			var p = GetProperties();
			foreach (var prop in p)
			{
				strBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0} = {1}\n", prop.Key, prop.Value);
			}
			Log.Info(strBuilder.ToString());
		}

		/// <summary>
		/// Returns some of system and environment properties for diagnostic.
		/// </summary>
		/// <returns></returns>
		private static Dictionary<string, string> GetProperties()
		{
			return new Dictionary<string, string> //TODO: casing, review list of properties
			{
				{"file.separator", Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)},
				{"clr.version", Environment.Version.ToString()},
				{"os.arch", System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString()},
				{"os.name", Environment.OSVersion.Platform.ToString()},
				{"os.version", Environment.OSVersion.Version.ToString()},
				{"user.dir", Environment.CurrentDirectory},
				{"user.home", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)},
				{"user.name", Environment.UserName}
			};
		}

		/// <summary>
		/// Registers a new <c>session</c>
		/// </summary>
		/// <param name="session"> </param>
		public void RegisterFixSession(IExtendedFixSession session)
		{
			var sessionId = session.Parameters.SessionId.ToString();
			if (Exists(sessionId))
			{
				throw new DuplicateSessionException("Session already exists. Duplicate sessionID: " + sessionId);
			}

			lock (_list)
			{
				if (_taskThread == null)
				{
					_taskThread = new TaskLaunchThread(this);
					_taskThread.Start();
				}

				_list.Add(session);
				_taskThread.ListIncreaseEvent();
			}

			NotifySessionAdd(session);
		}

		/// <summary>
		/// Removes the <c>session</c>.
		/// </summary>
		/// <param name="session"> </param>
		public void RemoveFixSession(IExtendedFixSession session)
		{
			bool result;
			lock (_list)
			{
				result = _list.Remove(session);
			}
			if (result)
			{
				if (Log.IsInfoEnabled)
				{
					Log.Info("Session disposed: " + session);
				}
				NotifySessionRemoved(session);
			}
		}

		/// <summary>
		/// Remove all sessions.
		/// </summary>
		public void RemoveAllSessions()
		{
			var copy = _list.RemoveAll();
			for (var i = 0; i < copy.Count; i++)
			{
				var session = copy[i];
				if (Log.IsInfoEnabled)
				{
					Log.Info("Session disposed: " + session);
				}
				NotifySessionRemoved(session);
			}
		}

		/// <summary>
		/// Finds the session by sessionID.
		/// </summary>
		/// <param name="sessionId"> the unique session identifier </param>
		public IExtendedFixSession Locate(string sessionId)
		{
			var readCopy = _list.GetReadOnlyCopy();
			for (var i = 0; i < readCopy.Count; i++)
			{
				var session = readCopy[i];
				var parameters = session.Parameters;
				if (parameters.SessionId.ToString().Equals(sessionId, StringComparison.Ordinal))
				{
					return session;
				}
			}
			return null;
		}

		/// <summary>
		/// Finds the session by sessionID.
		/// </summary>
		/// <param name="sessionId"> the unique session identifier </param>
		public IExtendedFixSession Locate(SessionId sessionId)
		{
			var readCopy = _list.GetReadOnlyCopy();
			for (var i = 0; i < readCopy.Count; i++)
			{
				var session = readCopy[i];
				if (session.Parameters.SessionId.Equals(sessionId))
				{
					return session;
				}
			}
			return null;
		}

		public IExtendedFixSession Locate(SessionParameters @params)
		{
			return Locate(@params.SessionId.ToString());
		}

		/// <summary>
		/// TODO: implement ASAP
		/// </summary>
		/// <param name="senderComId"> </param>
		/// <param name="senderSubId"> </param>
		/// <param name="senderLocationId"> </param>
		/// <param name="targetCompId"> </param>
		/// <param name="targetSubId"> </param>
		/// <param name="targetLocationId">
		/// @return </param>
		public IExtendedFixSession Locate(string senderComId, string senderSubId, string senderLocationId, string targetCompId, string targetSubId, string targetLocationId)
		{
			return LocateFirst(senderComId, targetCompId);
		}

		/// <summary>
		/// Finds the session with senderCompID and targetCompID. It's possible that can be several such sessions
		/// but method will return randomly first.
		/// </summary>
		/// <param name="senderCompId"> the sender comp id </param>
		/// <param name="targetCompId"> the target comp id </param>
		public IExtendedFixSession LocateFirst(string senderCompId, string targetCompId)
		{
			var readCopy = _list.GetReadOnlyCopy();
			for (var i = 0; i < readCopy.Count; i++)
			{
				var session = readCopy[i];
				var parameters = session.Parameters;
				if (parameters.SenderCompId.Equals(senderCompId) && parameters.TargetCompId.Equals(targetCompId))
				{
					return session;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns true if session with senderCompID and targetCompID exists.
		/// </summary>
		/// <param name="sessionId"> the unique session identifier </param>
		public bool Exists(string sessionId)
		{
			var readCopy = _list.GetReadOnlyCopy();
			for (var i = 0; i < readCopy.Count; i++)
			{
				var session = readCopy[i];
				var parameters = session.Parameters;
				if (parameters.SessionId.ToString().Equals(sessionId, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets cloned list of sessions.
		/// </summary>
		public IList<IExtendedFixSession> SessionListCopy => _list.GetReadOnlyCopy();

		public int SessionsCount => _list.Count;

		private class TaskLaunchThread
		{
			private readonly FixSessionManager _manager;
			private readonly Thread _thread;

			internal ServiceStatus Status = ServiceStatus.NotInit;

			public TaskLaunchThread(FixSessionManager manager)
			{
				_manager = manager;
				_thread = new Thread(Run) { IsBackground = true, Name = "TaskLaunchThread" };
			}

			public void Init()
			{
				var factory = new RunnableSessionTaskFactory();
				_manager._runnablePool = new FixedRunnablePool<IRunnableObject>(100, 300, factory);

				var queue = new SynchronizeBlockingQueue<IRunnableObject>(500);
				_manager._service = new ThreadPool(10, "SessionManager", queue);
				Status = ServiceStatus.Working;
			}

			private class RunnableSessionTaskFactory : IRunnableFactory<IRunnableObject>
			{
				public IRunnableObject Create(IRunnablePool<IRunnableObject> pool)
				{
					return new RunnableSessionTask(pool);
				}
			}

			public void Start()
			{
				_thread.Start();
			}

			public void Run()
			{
				Init();
				Log.Debug("Task thread started");
				while (!IsStopped())
				{
					if (_manager._list.IsEmpty)
					{
						var needWait = false;
						lock (_manager._list)
						{
							if (_manager._list.IsEmpty)
							{
								needWait = true;
								Status = ServiceStatus.Waiting;
							}
						}

						if (needWait)
						{
							WaitUntilNotify();
						}
					}

					SubmitTasks();
					if (!_manager._list.IsEmpty)
					{
						Sleep();
					}
				}
			}

			public bool IsStopped()
			{
				return Status == ServiceStatus.Stopped;
			}

			public void ListIncreaseEvent()
			{
				lock (this)
				{
					if (Status == ServiceStatus.Waiting)
					{
						Status = ServiceStatus.Working;
						Monitor.PulseAll(this);
					}
				}
			}

			public void WaitUntilNotify()
			{
				lock (this)
				{
					try
					{
						if (Status == ServiceStatus.Waiting)
						{
							Log.Debug("Task thread waiting...");
							Monitor.Wait(this);
							Log.Debug("Task thread awake");
						}
					}
					catch (ThreadInterruptedException)
					{
						Log.Trace("Thread interrupted.");
					}
				}
			}

			public void StopService(bool interrupt)
			{
				lock (this)
				{
					_manager._service.Stop(interrupt);
					Status = ServiceStatus.Stopped;
					Log.Debug("Task thread stopped");
				}
			}

			public void SubmitTasks()
			{
				var readCopy = _manager._list.GetReadOnlyCopy();
				foreach (var extendedFixSession in readCopy)
				{
					lock (_manager._tasks)
					{
						foreach (var task in _manager._tasks)
						{
							SubmitTask(extendedFixSession, task);
						}
					}
				}
			}

			public void SubmitTask(IExtendedFixSession session, ISessionManagerTask task)
			{
				var runnableTask = (RunnableSessionTask) _manager._runnablePool.Get();
				runnableTask.Session = session;
				runnableTask.Task = task;
				try
				{
					_manager._service.Execute(runnableTask);
				}
				catch (ThreadInterruptedException e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn($"Task interrupted. SessionID: {session.Parameters.SessionId}. {e.Message}", e);
					}
					else
					{
						Log.Warn($"Task interrupted. SessionID: {session.Parameters.SessionId}. {e.Message}");
					}
				}
			}

			public void Sleep()
			{
				try
				{
					Thread.Sleep(_manager.WaitTime); // now sleep for awhile
				}
				catch (ThreadInterruptedException)
				{
					// intentionally empty catch block
					Log.Trace("Thread interrupted.");
				}
			}
		}

		internal enum ServiceStatus
		{
			NotInit,
			Working,
			Waiting,
			Stopped
		}

		internal class RunnableSessionTask : RunnableObject
		{
			internal ISessionManagerTask Task;
			internal IExtendedFixSession Session;

			public RunnableSessionTask(IRunnablePool<IRunnableObject> pool) : base(pool)
			{
			}

			protected override void RunTask()
			{
				Task.RunForSession(Session);
			}
		}

		/// <summary>
		/// Register client <see cref="IFixSessionListListener"/>.
		/// </summary>
		/// <param name="listener"> </param>
		public void RegisterSessionManagerListener(IFixSessionListListener listener)
		{
			lock (_listenersList)
			{
				if (!_listenersList.Contain(listener))
				{
					_listenersList.Add(listener);
				}
			}
		}

		/// <summary>
		/// Unregister client <see cref="IFixSessionListListener"/>.
		/// </summary>
		/// <param name="listener"> </param>
		public void UnregisterSessionManagerListener(IFixSessionListListener listener)
		{
			lock (_listenersList)
			{
				_listenersList.Remove(listener);
			}
		}

		public void NotifySessionAdd(IExtendedFixSession fixSession)
		{
			IList<IFixSessionListListener> readOnlyListeners;
			lock (_listenersList)
			{
				readOnlyListeners = _listenersList.ReadOnlyCopy();
			}
			for (var i = 0; i < readOnlyListeners.Count; i++)
			{
				try
				{
					readOnlyListeners[i].OnAddSession(fixSession);
				}
				catch (Exception e)
				{
					Log.Error("Error on call onAddSession. Cause: " + e.Message, e);
				}
			}
			lock (_listenersList)
			{
				_listenersList.ReleaseCopy(readOnlyListeners);
			}
		}

		public void NotifySessionRemoved(IExtendedFixSession fixSession)
		{
			IList<IFixSessionListListener> readOnlyListeners;
			lock (_listenersList)
			{
				readOnlyListeners = _listenersList.ReadOnlyCopy();
			}
			for (var i = 0; i < readOnlyListeners.Count; i++)
			{
				try
				{
					readOnlyListeners[i].OnRemoveSession(fixSession);
				}
				catch (Exception e)
				{
					Log.Error("Error on call onRemoveSession. Cause: " + e.Message, e);
				}
			}
			lock (_listenersList)
			{
				_listenersList.ReleaseCopy(readOnlyListeners);
			}
		}

		/// <summary>
		/// Methods close all sessions.
		/// <p/>
		/// <p/>
		/// </summary>
		public static void CloseAllSession()
		{
			var fixSessions = Instance._list.GetReadOnlyCopy();
			for (var i = 0; i < fixSessions.Count; i++)
			{
				CloseSession(fixSessions[i]);
			}
		}

		private static void CloseSession(IExtendedFixSession session)
		{
			try
			{
				session.Disconnect(DisconnectReason.GetDefault().ToString());
			}
			catch (Exception)
			{
				Log.Trace("Exception while closing session.");
			}
		}

		/// <summary>
		/// Methods dispose all sessions.
		/// <p/>
		/// <p/>
		/// </summary>
		public static void DisposeAllSession()
		{
			var fixSessions = Instance._list.GetReadOnlyCopy();
			for (var i = 0; i < fixSessions.Count; i++)
			{
				Dispose(fixSessions[i]);
			}
		}

		private static void Dispose(IExtendedFixSession session)
		{
			try
			{
				session.Dispose();
			}
			catch (Exception)
			{
				Log.Trace("Exception while disposing session.");
			}
		}

		/// <exception cref="IOException"> </exception>
		public static bool ResetSeqNums(SessionParameters @params)
		{
			var result = true;
			var manager = Instance;
			var session = manager.Locate(@params);
			if (session != null)
			{
				//FIX session exist
				session.ResetSequenceNumbers();
			}
			else
			{
				var storageFactory = ReflectStorageFactory.CreateStorageFactory(@params.Configuration);
				var persistedParams = new SessionParameters();
				var runtimeState = new FixSessionRuntimeState();
				persistedParams.FromProperties(@params.ToProperties());
				if (!storageFactory.LoadSessionParameters(persistedParams, runtimeState))
				{
					Log.Debug("Session " + @params.SessionId + " not fount. Init properties for this session.");
					result = false;
				}
				persistedParams.IncomingSequenceNumber = 1;
				persistedParams.OutgoingSequenceNumber = 1;
				runtimeState.InSeqNum = 1;
				runtimeState.OutSeqNum = 1;
				runtimeState.LastProcessedSeqNum = 0;
				storageFactory.SaveSessionParameters(persistedParams, runtimeState);
			}
			return result;
		}

		/// <exception cref="IOException"> </exception>
		public static bool SetSeqNums(SessionParameters @params, int inSeqNum, int outSeqNum)
		{
			var result = true;
			var manager = Instance;
			var session = manager.Locate(@params);
			if (session != null)
			{
				session.SetSequenceNumbers(inSeqNum, outSeqNum);
			}
			else
			{
				var storageFactory = ReflectStorageFactory.CreateStorageFactory(@params.Configuration);
				var persistedParams = new SessionParameters();
				persistedParams.FromProperties(@params.ToProperties());
				var runtimeState = new FixSessionRuntimeState();
				if (!storageFactory.LoadSessionParameters(persistedParams, runtimeState))
				{
					Log.Debug("Session " + @params.SessionId + " not fount. Init properties for this session.");
					result = false;
				}
				if (inSeqNum >= 0)
				{
					persistedParams.IncomingSequenceNumber = inSeqNum;
					runtimeState.InSeqNum = inSeqNum;

				}
				if (outSeqNum >= 0)
				{
					persistedParams.OutgoingSequenceNumber = outSeqNum;
					runtimeState.OutSeqNum = outSeqNum;
				}
				storageFactory.SaveSessionParameters(persistedParams, runtimeState);
			}
			return result;
		}

		/// <summary>
		/// This class contains two copies of the lists.
		/// </summary>
		internal class RarelyChangeList<TE>
		{
			internal IList<TE> List = new List<TE>();
			internal IList<TE> ReadOnlyCopyConflict = new List<TE>();
			internal int CountUsers;

			
			/// <summary>
			/// Remove item from list and update(or create if the update is not possible) ReadOnly copy
			/// This method is not thread safe.
			/// </summary>
			public virtual bool Remove(TE item)
			{
				var isRemoved = List.Remove(item);
				if (isRemoved)
				{
					if (CountUsers == 0)
					{
						// nobody uses. we can modify
						ReadOnlyCopyConflict.Remove(item);
					}
					else
					{
						// this copy used now. create new copy
						ReadOnlyCopyConflict = new List<TE>(List);
						// reset count for new copy
						CountUsers = 0;
					}
				}
				return isRemoved;
			}

			/// <summary>
			/// Added item to list and update(or create if the update is not possible) ReadOnly copy
			/// This method is not thread safe.
			/// </summary>
			/// <param name="item"> </param>
			public virtual void Add(TE item)
			{
				List.Add(item);
				if (CountUsers == 0)
				{
					// nobody uses. we can modify
					ReadOnlyCopyConflict.Add(item);
				}
				else
				{
					// this copy used now. create new copy
					ReadOnlyCopyConflict = new List<TE>(List);
				}
			}

			/// <summary>
			/// Return ReadOnly copy of original list.
			/// Release the list after use to work properly.
			/// This method is not thread safe.
			/// </summary>
			public virtual IList<TE> ReadOnlyCopy()
			{
				CountUsers++;
				return ReadOnlyCopyConflict;
			}

			/// <summary>
			/// Release the list after use to work properly.
			/// This method is not thread safe. </summary>
			/// <param name="copy"> </param>
			public virtual void ReleaseCopy(IList<TE> copy)
			{
				if (ReadOnlyCopyConflict == copy)
				{
					// this is same copy
					CountUsers--;
				}
			}

			public virtual bool Contain(TE item)
			{
				return List.Contains(item);
			}
		}

		private class CopyOnEditArrayList<TE>
		{
			private bool _changed;
			private readonly object _lock = false;

			private readonly IList<TE> _list = new List<TE>(50);
			private IList<TE> _readOnlyCopy = new List<TE>(50);

			/// <summary>
			/// Remove item from list and set 'true' change flag.
			/// This method is not thread safe.
			/// </summary>
			public bool Remove(TE item)
			{
				var isRemoved = _list.Remove(item);
				if (isRemoved)
				{
					lock (_lock)
					{
						_changed = true;
					}
				}
				return isRemoved;
			}

			/// <summary>
			/// Add item to list and set 'true' change flag.
			/// This method is not thread safe.
			/// </summary>
			/// <param name="item"> </param>
			public void Add(TE item)
			{
				_list.Add(item);
				lock (_lock)
				{
					_changed = true;
				}
			}

			/// <summary>
			/// 
			/// </summary>
			/// <returns> read only copy of original list </returns>
			public IList<TE> GetReadOnlyCopy()
			{
				if (_changed)
				{
					lock (_lock)
					{
						if (!_changed)
						{
							return _readOnlyCopy;
						}

						_readOnlyCopy = _list.ToImmutableList();
						_changed = false;
					}
				}
				return _readOnlyCopy;
			}

			/// <summary>
			/// Clears content of the list.
			/// </summary>
			/// <returns>Read only copy of the list before clearing.</returns>
			public IList<TE> RemoveAll()
			{
				var result = GetReadOnlyCopy();
				_list.Clear();
				lock (_lock)
				{
					_changed = true;
				}
				return result;
			}

			/// <summary>
			/// Returns true if the list is empty.
			/// </summary>
			public bool IsEmpty => _list.Count == 0;

			/// <summary>
			/// Gets count of elements in the list.
			/// </summary>
			public int Count => _list.Count;
		}
	}
}