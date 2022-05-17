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
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text;

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
		private readonly CopyOnEditArrayList<IExtendedFixSession> _sessions;
		private bool _running;

		/// <summary>
		/// Gets the <see cref="FixSessionManager"/> instance.
		/// </summary>
		public static FixSessionManager Instance { get; } = new FixSessionManager();

		private FixSessionManager()
		{
			_sessions = new CopyOnEditArrayList<IExtendedFixSession>();
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
				_running = false;
				//TODO: shudown schedulers?
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

			lock (_sessions)
			{
				_sessions.Add(session);
				//TODO: check task execution??
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
			lock (_sessions)
			{
				result = _sessions.Remove(session);
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
			var copy = _sessions.RemoveAll();
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
			var readCopy = _sessions.GetReadOnlyCopy();
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
			var readCopy = _sessions.GetReadOnlyCopy();
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
			var readCopy = _sessions.GetReadOnlyCopy();
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
			var readCopy = _sessions.GetReadOnlyCopy();
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
		public IList<IExtendedFixSession> SessionListCopy => _sessions.GetReadOnlyCopy();

		public int SessionsCount => _sessions.Count;

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
			var fixSessions = Instance._sessions.GetReadOnlyCopy();
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
			var fixSessions = Instance._sessions.GetReadOnlyCopy();
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