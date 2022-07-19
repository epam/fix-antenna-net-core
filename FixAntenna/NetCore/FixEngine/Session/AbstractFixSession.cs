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

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Manager;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Session.Validation;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Validation;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.FixEngine.Scheduler;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	/// <summary>
	/// The abstract session implementation.
	/// Provides base functional for acceptor and initiator sessions.
	/// </summary>
	internal abstract class AbstractFixSession : IExtendedFixSession, ISessionStateListenSupport
	{
		protected readonly ILog Log;

		private const string DontRedefineType = "";
		private const int Second = 1000;
		private static TimeSpan CheckHbAndTestRequestInterval = TimeSpan.FromSeconds(1);

		private static readonly FixMessage SeqNumResetTag = new FixMessage();

		static AbstractFixSession()
		{
			SeqNumResetTag.AddTag(Tags.ResetSeqNumFlag, true);
		}

		internal AtomicBool AlreadyShuttingDown = true;
		internal AtomicBool AlreadySendingLogout = true;
		protected internal IFixTransport Transport;
		protected internal IQueue<FixMessageWithType> Queue;
		protected internal readonly HandlerChain Listener;
		private long[] _longAttrs = new long[ExtendedFixSessionAttribute.Values().Count];
		private bool[] _boolAttrs = new bool[ExtendedFixSessionAttribute.Values().Count];
		private readonly IDictionary<string, object> _attributes = new ConcurrentDictionary<string, object>();
		private IMessageValidator _messageValidator;
		private long _established;
		protected internal bool Graceful;
		private readonly int _forceLogOffTimeout;
		private readonly int _logoutWaitTimeout;
		private int _reasonableTransmissionTimeMillis;

		private bool _enabledMessageRejecting;

		private FixMessage _testRequest = new FixMessage();
		protected internal TestReqIdTimestamp TestRequestTime = new TestReqIdTimestamp();
		protected internal MutableInteger SentTrNum = new MutableInteger();
		internal IMessageReader Reader;
		internal IMessagePumper Pumper;

		protected internal readonly object SessionLock = new object();

		private FixSessionListenerObserver _sessionStateListenerObserver;

		protected internal readonly ConfigurationAdapter ConfigAdapter;

		private PreparedMessageUtil _preparedMessageUtil;

		private volatile DisconnectReason _disconnectReason;

		private volatile bool _initialized;

		private bool _ignoreResetSeqNumFlagOnReset;
		protected internal SendingMode PreferredSendingMode;
		private readonly IDictionary<string, ConcurrentBag<IExtendedFixSessionAttributeListener>> _attributeListeners = new ConcurrentDictionary<string, ConcurrentBag<IExtendedFixSessionAttributeListener>>();

		private readonly ISet<ITypedFixMessageListener> _outSessionLevelListeners = new HashSet<ITypedFixMessageListener>();

		protected internal SessionTaskScheduler Scheduler;

		/// <summary>
		/// Creates the <c>AbstractFIXSession</c>.
		/// </summary>
		public AbstractFixSession(IFixMessageFactory messageFactory, SessionParameters sessionParameters, HandlerChain fixSessionListener)
		{
			Log = LogFactory.GetLog(GetType());

			FixSessionOutOfSyncListener = new FixSessionOutOfSyncListenerAdapter(this);
			_sessionStateListenerObserver = new FixSessionListenerObserver(this);

			if (Config.MaxTimeoutValue < sessionParameters.HeartbeatInterval)
			{
				// Bug 14419: Very lardge HBI cause antenna to close session
				Log.Error("HBI is too large - " + sessionParameters.HeartbeatInterval);
				throw new Exception("HBI is too large - " + sessionParameters.HeartbeatInterval);
			}

			FixSessionManager.Init();
			MessageFactory = messageFactory;
			ConfigAdapter = new ConfigurationAdapter(sessionParameters.Configuration);
			PreferredSendingMode = ConfigAdapter.PreferredSendingMode;
			StorageFactory = ReflectStorageFactory.CreateStorageFactory(ConfigAdapter.Configuration);
			if (StorageFactory is IInitializable)
			{
				((IInitializable)StorageFactory).Init(sessionParameters);
			}

			if (Log.IsDebugEnabled)
			{
				Log.Debug("Session " + sessionParameters.SessionId + " initialized with " + StorageFactory.GetType().FullName + " storage factory");
			}

			RuntimeState = new FixSessionRuntimeState();
			Parameters = sessionParameters;
			Queue = StorageFactory.CreateQueue(sessionParameters);
			_forceLogOffTimeout = ConfigAdapter.ForceLogoff;
			_logoutWaitTimeout = ConfigAdapter.LogoutWaitTimeout;
			_reasonableTransmissionTimeMillis = ConfigAdapter.HbtReasonableTransmissionTime;

			_enabledMessageRejecting = ConfigAdapter.IsEnableMessageRejecting;

			InitAttributes();
			SequenceManager = GetSequenceManagerFromConfig();

			if (Log.IsDebugEnabled)
			{
				Log.Debug("Session " + sessionParameters.SessionId + " initialized with " + SequenceManager.GetType().FullName + " sequence manager");
			}

			SequenceManager.LoadStoredParameters();

			Listener = fixSessionListener;
			Listener.Session = this;

			_preparedMessageUtil = new PreparedMessageUtil(Parameters);

			_ignoreResetSeqNumFlagOnReset = ConfigAdapter.Configuration.GetPropertyAsBoolean(Config.IgnoreResetSeqNumFlagOnReset, false);

			sessionParameters.PrintConfiguration();
		}

		/// <summary>
		/// For testing purposes
		/// </summary>
		internal AbstractFixSession() {}

		private void InitSessionRuntimeState()
		{
			var configuredOutLogon = Parameters.OutgoingLoginMessage;
			RuntimeState.OutgoingLogon = configuredOutLogon.DeepClone(false, configuredOutLogon.IsUserOwned);
		}

		public virtual ISessionSequenceManager SequenceManager { get; }

		private ISessionSequenceManager GetSequenceManagerFromConfig()
		{
			var sessionSequenceManagerClass = ConfigAdapter.Configuration
				.GetProperty(Config.SessionSequenceManager, typeof(StandardSessionSequenceManager).FullName);

			try
			{
				var reflectUtil = new ReflectUtilEx(Type.GetType(sessionSequenceManagerClass));
				return reflectUtil.GetInstance(new object[] { this }) as ISessionSequenceManager;
			}
			catch (Exception ex)
			{
				Log.Warn("Can not load session sequence manager: " + sessionSequenceManagerClass + ". Cause: " + ex.Message + ". Loaded default StandardSessionSequenceManager.");
				return new StandardSessionSequenceManager(this);
			}
		}

		/// <inheritdoc />
		public virtual IExtendedFixSessionListener ExtendedFixSessionListener { get; private set; } = new ExtendedFixSessionListenerAdapter();

		/// <summary>
		/// Gets storage factory
		/// </summary>
		public IStorageFactory StorageFactory { get; }

		/// <inheritdoc />
		public virtual IRejectMessageListener RejectMessageListener { get; set; } = new RejectFixSessionListenerAdapter();

		/// <inheritdoc />
		public virtual IErrorHandler ErrorHandler { get; set; } = new LoggingErrorHandler();

		/// <inheritdoc />
		public virtual IFixSessionOutOfSyncListener FixSessionOutOfSyncListener { set; get; }

		/// <inheritdoc />
		public IFixSessionSlowConsumerListener SlowConsumerListener { get; set; } = new SlowConsumerListenerAdapter();

		/// <inheritdoc />
		public virtual void MarkShutdownAsGraceful()
		{
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Mark shutdown as graceful session " + SessionId);
			}

			Graceful = true;
			if (Pumper != null) Pumper.GracefulShutdown = true;
			if (Reader != null) Reader.GracefulShutdown = true;
		}

		private string SessionId
		{
			get => Parameters.SessionId.ToString();
		}

		/// <inheritdoc />
		public virtual void ClearQueue()
		{
			if (_enabledMessageRejecting)
			{
				Pumper.RejectQueueMessages();
				Queue.Clear();
				//return true;
			}
			else
			{
				//remove only out-of-turn (session-level) messages
				lock (Queue)
				{
					var storedOutOfTournMode = Queue.OutOfTurnOnlyMode;
					Queue.OutOfTurnOnlyMode = true;
					if (Log.IsDebugEnabled)
					{
						Log.Debug("Clean out-of-turn queue for session " + SessionId);
					}
					while (!Queue.IsEmpty)
					{
						try
						{
							//get priority message
							var msg = Queue.Poll();
							if (msg.FixMessage != null)
							{
								if (Log.IsDebugEnabled)
								{
									Log.Debug("Remove out-of-turn message: " + msg.ToString());
								}
							}
						}
						catch (Exception)
						{
							// ignore
						}
						finally
						{
							Queue.Commit();
						}
					}
					Queue.OutOfTurnOnlyMode = storedOutOfTournMode;
				}
			}
			//return false;
		}

		/// <inheritdoc />
		public virtual int QueuedMessagesCount
		{
			get
			{
				lock (Queue)
				{
					return Queue.TotalSize;
				}
			}
		}

		/// <inheritdoc />
		public virtual void LockSending()
		{
			SetOutOfTurnMode(true);
			if (PreferredSendingMode == SendingMode.SyncNoqueue)
			{
				((NoQueueMessagePumper)Pumper)?.Lock();
			}
		}

		/// <inheritdoc />
		public virtual void UnlockSending()
		{
			SetOutOfTurnMode(false);
			if (PreferredSendingMode == SendingMode.SyncNoqueue)
			{
				((NoQueueMessagePumper)Pumper)?.Unlock();
			}
		}

		/// <inheritdoc />
		public virtual void SetOutOfTurnMode(bool mode)
		{
			Queue.OutOfTurnOnlyMode = mode;
		}

		/// <inheritdoc />
		public virtual void ResetSequenceNumbers()
		{
			ResetSequenceNumbers(false);
		}

		/// <inheritdoc />
		public virtual void ResetSequenceNumbers(bool checkGapFillBefore)
		{
			lock (SessionLock)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Reset sequences for session " + SessionId);
				}
				if (AlreadyShuttingDown || SessionState.IsDisconnected(SessionState))
				{
					//sessionParameters.SetOutgoingSequenceNumber(0);
					//sessionParameters.SetIncomingSequenceNumber(0);
					Parameters.IncomingSequenceNumber = 1;
					Parameters.OutgoingSequenceNumber = 1;
					//reset also RR flags
					SequenceManager.RemoveRangeOfRrSequence();
					SaveSessionParameters();

					if (_initialized)
					{
						MessageFactory.SetSessionParameters(Parameters);
						SequenceManager.Reinit(this);
						SequenceManager.InitSeqNums(RuntimeState.InSeqNum, RuntimeState.OutSeqNum);
					}

					return;
				}
			}
			if (Parameters.FixVersion.CompareTo(FixVersion.Fix40) == 0)
			{
				throw new InvalidOperationException("FIX40 is not resettable while online");
			}
			if (checkGapFillBefore)
			{
				SendTestRequestForSeqNumGap();
			}
			else
			{
				ResetSeqNumsAndSendResetLogon();
			}
		}

		/// <inheritdoc />
		public virtual void SetSequenceNumbers(long inSeqNum, long outSeqNum)
		{
			lock (SessionLock)
			{
				// TODO: make sure that session safely initializes
				if (Log.IsDebugEnabled)
				{
					Log.Debug("set new sequences " + inSeqNum + ":" + outSeqNum + " for session " + SessionId);
				}

				if (!_initialized)
				{
					if (inSeqNum > 0)
					{
						Parameters.IncomingSequenceNumber = inSeqNum;
					}
					if (outSeqNum > 0)
					{
						Parameters.OutgoingSequenceNumber = outSeqNum;
					}
				}
				else
				{
					if (inSeqNum > 0)
					{
						Parameters.IncomingSequenceNumber = inSeqNum;
						RuntimeState.InSeqNum = inSeqNum;
						RuntimeState.LastProcessedSeqNum = inSeqNum - 1;
					}

					if (outSeqNum > 0)
					{
						Parameters.OutgoingSequenceNumber = outSeqNum;
						RuntimeState.OutSeqNum = outSeqNum;
					}
				}

				//reset also RR flags
				SequenceManager.RemoveRangeOfRrSequence();
				SaveSessionParameters();
			}
		}

		private void SendTestRequestForSeqNumGap()
		{
			// sets the flag and send TR
			SetAttribute(ExtendedFixSessionAttribute.TestRequestIsSentForSeqReset.Name, "Y");
			SendTestRequest();
		}

		private void ResetSeqNumsAndSendResetLogon()
		{
			SetAttribute(ExtendedFixSessionAttribute.IntradayLogonIsSent.Name, "Y");
			SequenceManager.ConfigureStateBeforeReset();
			// Bug BBP-283: After sequences reset sent queued message instead Logon
			lock (Queue)
			{
				//set OutOfTurn mode to prevent sending messages with invalid seq
				Queue.OutOfTurnOnlyMode = true;
				if (!_ignoreResetSeqNumFlagOnReset)
				{
					SequenceManager.SetResetSeqNumFlagIntoOutgoingLogon();
				}
				//reset sequences before send logon
				SequenceManager.ApplyOutSeqnum(1);
				Log.Debug("Send reset logon");
				CreateAndSendLogonMessage();
			}
		}

		private void CreateAndSendLogonMessage()
		{
			var message = MessageFactory.CompleteMessage(MsgType.Logon, new FixMessage());
			ClearOutOfTurnQueue();
			SendMessageOutOfTurn(string.Empty, message);
		}

		private void ClearOutOfTurnQueue()
		{
			lock (Queue)
			{
				Queue.ClearOutOfTurn(removedMessage =>
				{
					if (removedMessage.IsApplicationLevelMessage())
					{
						Log.Warn("Application message '" + removedMessage + "' has been removed from 'out of turn' queue.");
					}
					else
					{
						Log.Debug("Message '" + removedMessage + "' has been removed from 'out of turn' queue.");
					}
				});
			}
		}

		/// <inheritdoc />
		public virtual void SetFixSessionListener(IFixSessionListener listener)
		{
			if (listener is IExtendedFixSessionListener sessionListener)
			{
				ExtendedFixSessionListener = sessionListener;
			}

			Listener.SetUserListener(new SessionStateAdapter(_sessionStateListenerObserver, listener));
		}

		/// <inheritdoc />
		public virtual void AddInSessionLevelMessageListener(IFixMessageListener listener)
		{
			Listener.AddInSessionMessageListener(listener);
		}

		/// <inheritdoc />
		public void AddOutSessionLevelMessageListener(ITypedFixMessageListener listener)
		{
			_outSessionLevelListeners.Add(listener);
		}

		/// <inheritdoc />
		public void AddUserGlobalMessageHandler(AbstractUserGlobalMessageHandler userMessageHandler)
		{
			Listener.AddUserGlobalMessageHandler(userMessageHandler);
		}

		/// <inheritdoc />
		public virtual IMessageStorage OutgoingStorage { get; protected set; }

		/// <inheritdoc />
		public virtual IMessageStorage IncomingStorage { get; protected set; }

		/// <inheritdoc />
		public virtual IQueue<FixMessageWithType> MessageQueue => Queue;

		/// <inheritdoc />
		public virtual int OutgoingQueueSize
		{
			get
			{
				lock (Queue)
				{
					return Queue.TotalSize;
				}
			}
		}

		/// <inheritdoc />
		public virtual IList<IEnqueuedFixMessage> GetOutgoingQueueMessages()
		{
			lock (Queue)
			{
				var msgs = Queue.ToArray();
				var outmsgs = new List<IEnqueuedFixMessage>(msgs.Length);
				foreach (FixMessageWithType msg in msgs)
				{
					outmsgs.Add(EnqueuedFixMessage.CopyFrom(msg));
				}
				return outmsgs;
			}
		}

		/// <inheritdoc />
		public virtual byte[] RetrieveSentMessage(long seqNumber)
		{
			return OutgoingStorage.RetrieveMessage(seqNumber);
		}

		/// <inheritdoc />
		public virtual byte[] RetrieveReceivedMessage(long seqNumber)
		{
			return IncomingStorage.RetrieveMessage(seqNumber);
		}

		/// <inheritdoc />
		public virtual void SaveSessionParameters()
		{
			SequenceManager.SaveSessionParameters();
		}

		/// <inheritdoc />
		public abstract void Connect();

		/// <inheritdoc />
		public abstract Task ConnectAsync();

		/// <inheritdoc />
		public abstract void Reject(string reason);

		/// <inheritdoc />
		public virtual SessionParameters Parameters { get; protected set; }

		/// <inheritdoc />
		public virtual IFixMessageFactory MessageFactory { get; }

		/// <inheritdoc />
		public virtual SessionState SessionState
		{
			get => RuntimeState.SessionState;
			set
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Change session " + SessionId + " state: " + SessionState + "->" + value);
				}

				RuntimeState.SessionState = value;
				FireSessionStateChanged(value);
			}
		}

		/// <inheritdoc />
		public FixSessionRuntimeState RuntimeState { get; }

		private void FireSessionStateChanged(SessionState newState)
		{
			ControlOverStateChange(newState);
			NotifySessionStateChanged(newState);
		}

		private void ControlOverStateChange(SessionState newState)
		{
			SessionStatusCheckerThread thread = null;
			switch (newState.EnumValue)
			{
				case SessionState.InnerEnum.WaitingForLogoff:
					{
						var logoutTimeout = _logoutWaitTimeout >= 0 ? _logoutWaitTimeout : Parameters.HeartbeatInterval;
						thread = new SessionStatusCheckerThread(this, logoutTimeout, SessionState.WaitingForLogoff, "Logoff wasn't received");
					}
					break;
				case SessionState.InnerEnum.WaitingForForcedLogoff:
					{
						thread = new SessionStatusCheckerThread(this, _forceLogOffTimeout, SessionState.WaitingForForcedLogoff, "", false);
						break;
					}
				case SessionState.InnerEnum.WaitingForForcedDisconnect:
					{
						thread = new SessionStatusCheckerThread(this, _forceLogOffTimeout, SessionState.WaitingForForcedDisconnect, "", false);
						break;
					}
				case SessionState.InnerEnum.Disconnected:
				case SessionState.InnerEnum.DisconnectedAbnormally:
					OnDisconnect();
					break;
				case SessionState.InnerEnum.Dead:
					UnRegisterSessionTasks();
					break;
			}

			if (thread != null)
			{
				thread.Start();
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Session status checker thread started");
				}
			}
		}

		private void NotifySessionStateChanged(SessionState newState)
		{
			try
			{
				Listener.OnSessionStateChange(newState);
			}
			catch (Exception e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("User listener was thrown exception.", e);
				}
				else
				{
					Log.Warn("User listener was thrown exception. " + e.Message);
				}
			}
		}

		/// <inheritdoc />
		public virtual bool IsStatisticEnabled => Pumper.IsStatisticEnabled && Reader.IsStatisticEnabled;

		public virtual bool IsHbtControlInPumper => PreferredSendingMode == SendingMode.SyncNoqueue;

		/// <inheritdoc />
		public virtual long BytesRead => Reader.IsStatisticEnabled ? Reader.MessageStatistic.BytesProcessed : -1;

		/// <inheritdoc />
		public virtual long IsEstablished => _established;

		/// <inheritdoc />
		public virtual long BytesSent => Pumper.IsStatisticEnabled ? Pumper.Statistic.BytesProcessed : -1;

		/// <inheritdoc />
		public virtual long NoOfInMessages => Reader.IsStatisticEnabled ? Reader.MessageStatistic.MessagesProcessed : -1;

		/// <inheritdoc />
		public virtual long NoOfOutMessages => Pumper.IsStatisticEnabled ? Pumper.Statistic.MessagesProcessed : -1;

		/// <inheritdoc />
		public virtual long LastInMessageTimestamp => Reader.MessageProcessedTimestamp;

		/// <inheritdoc />
		public virtual long LastOutMessageTimestamp => Pumper.MessageProcessedTimestamp;

		/// <summary>
		/// Gets chain of message handlers.
		/// </summary>
		public virtual HandlerChain MessageHandlers => Listener;

		public virtual void OnDisconnect()
		{
			ResetInitialization();
		}

		/// <inheritdoc />
		public virtual void Init()
		{
			if (!_initialized)
			{
				_initialized = true;
				InitSessionRuntimeState();
				InitSessionInternal();
			}
		}

		public virtual void ResetInitialization()
		{
			_initialized = false;
		}

		/// <summary>
		/// Initializes the resources and sends a logon message.
		/// </summary>
		/// <exception cref="IOException"> - if I/O error occurred. </exception>
		public virtual void PrepareForConnect()
		{
			Init();
			CreateAndSendLogonMessage();
		}

		public virtual bool IsResetSeqNumFlagRequiredForInitLogon => false;

		/// <summary>
		/// Initialize session internal.
		/// <p/>
		/// <p/>
		/// The method is created all session object: in/out storage and pumper/reader, etc.
		/// </summary>
		public virtual void InitSessionInternal()
		{
			if (_messageValidator == null)
			{
				//TODO: move to constructor when acceptor will be able to customize dictionary from session parameters
				_messageValidator = SessionValidatorFactory.GetMessageValidator(Parameters);
			}

			Graceful = false;
			AlreadyShuttingDown = false;
			AlreadySendingLogout = false;

			SequenceManager.LoadStoredParameters();
			SequenceManager.InitLastSeqNumResetTimestampOnNewSession();
			ResetTestRequestFlags();

			MessageFactory.SetSessionParameters(Parameters);
			MessageFactory.SetRuntimeState(RuntimeState);

			IncomingStorage?.Dispose();
			IncomingStorage = StorageFactory.CreateIncomingMessageStorage(Parameters);
			Reader = BuildMessageReader(IncomingStorage, Listener, Transport);
			var inStorageSeqNum = Reader.Init(ConfigAdapter);

			OutgoingStorage?.Dispose();
			OutgoingStorage = StorageFactory.CreateOutgoingMessageStorage(Parameters);

			Queue.Initialize();
			Pumper = BuildMessagePumper(ConfigAdapter, Queue, OutgoingStorage, MessageFactory, Transport, SequenceManager);
			var nextOutStorageSeqNum = Pumper.Init();

			Queue.OutOfTurnOnlyMode = true; // turn off when response received
			
			SequenceManager.Reinit(this);
			SequenceManager.InitSeqNums(inStorageSeqNum, nextOutStorageSeqNum);
			SubscribeForAttributeChanges(ExtendedFixSessionAttribute.IsResendRequestProcessed, new ExtendedFixSessionAttributeListener(this));

			if (Scheduler == null)
			{
				Scheduler = new SessionTaskScheduler(Parameters);
			}

			RegisterSessionTasks();
		}

		private class ExtendedFixSessionAttributeListener : IExtendedFixSessionAttributeListener
		{
			private readonly AbstractFixSession _outerInstance;

			public ExtendedFixSessionAttributeListener(AbstractFixSession outerInstance)
			{
				_outerInstance = outerInstance;
			}

			public void OnAttributeSet(object value)
			{
			}

			public void OnAttributeRemoved()
			{
				_outerInstance.Reader.MessageProcessedTimestamp = DateTimeHelper.CurrentMilliseconds;
			}
		}

		public virtual IMessageReader BuildMessageReader(IMessageStorage incomingMessageStorage, HandlerChain listener, IFixTransport transport)
		{
			return new MessageReader(this, incomingMessageStorage, listener, transport);
		}

		public virtual IMessagePumper BuildMessagePumper(ConfigurationAdapter configuration, IQueue<FixMessageWithType> queue, IMessageStorage outgoingMessageStorage,
			IFixMessageFactory messageFactory, IFixTransport transport, ISessionSequenceManager sequenceManager)
		{
			var preferredSendingMode = configuration.PreferredSendingMode;
			var slowConsumerDetectionEnabled = configuration.Configuration.GetPropertyAsBoolean(Config.SlowConsumerDetectionEnabled, false);
			var transportOrWrapper = slowConsumerDetectionEnabled ? new ConsumingControlTransportWrapper(this) : Transport;

			if (preferredSendingMode == SendingMode.Sync)
			{
				if (transport.IsBlockingSocket)
				{
					return new SyncBlockingMessagePumper(this, queue, outgoingMessageStorage, messageFactory, transportOrWrapper, sequenceManager);
				}

				return new SyncMessagePumper(this, queue, outgoingMessageStorage, messageFactory, transportOrWrapper, sequenceManager);
			}

			if (preferredSendingMode == SendingMode.SyncNoqueue)
			{
				return new NoQueueMessagePumper(this, queue, outgoingMessageStorage, messageFactory, transport, sequenceManager);
			}

			return new AsyncMessagePumper(this, queue, outgoingMessageStorage, messageFactory, transportOrWrapper, sequenceManager);
		}

		public virtual void StartSession()
		{
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Starting MessageReader thread. Incoming seq number:" + RuntimeState.InSeqNum);
			}

			Reader.Start();
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Reader started.");
			}

			if (Log.IsDebugEnabled)
			{
				Log.Debug("Starting MessagePumper thread. Outgoing seq number:" + RuntimeState.OutSeqNum);
			}

			Pumper.Start();
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Pumper started");
			}

			_established = DateTimeHelper.CurrentMilliseconds;
		}

		/// <summary>
		/// Disconnect the session.
		/// <para>
		/// The method sends the logof message, if response is not received during the HBI,
		/// shutdown the session.
		/// </para>
		/// </summary>
		/// <param name="reason"> the reason, if parameter is not null, the logof message will be send with 58=reason. </param>
		/// <seealso cref="IFixSession.Disconnect(string)"> </seealso>
		public virtual void Disconnect(string reason)
		{
			Disconnect(DisconnectReason.GetDefault(), reason);
		}

		/// <summary>
		/// Disconnect the session. Async version.
		/// <para>
		/// The method sends the logof message, if response is not received during the HBI,
		/// shutdown the session.
		/// </para>
		/// </summary>
		/// <param name="reason"> the reason, if parameter is not null, the logof message will be send with 58=reason. </param>
		/// <seealso cref="IFixSession.Disconnect(string)"> </seealso>
		public virtual async Task DisconnectAsync(string reason)
		{
			await Task.Run(() => Disconnect(reason)).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public virtual void Disconnect(DisconnectReason reasonType, string reasonDescription)
		{
			Disconnect(reasonType, reasonDescription, true, false, true);
		}

		/// <inheritdoc />
		public virtual void ForcedDisconnect(DisconnectReason reasonType, string reasonDescription, bool continueReading)
		{
			Disconnect(reasonType, reasonDescription, true, true, continueReading);
		}

		public virtual void Disconnect(DisconnectReason reasonType, string reasonDescription, bool isGracefull, bool isForced)
		{
			Disconnect(reasonType, reasonDescription, isGracefull, isForced, true);
		}

		//TODO: change to something like flag set
		public virtual void Disconnect(DisconnectReason reasonType, string reasonDescription, bool isGracefull, bool isForced, bool continueReading)
		{
			Disconnect(reasonType, reasonDescription, isGracefull, isForced, continueReading, true);
		}

		//TODO: change to something like flag set
		public virtual void Disconnect(DisconnectReason reasonType, string reasonDescription, bool isGracefull, bool isForced, bool continueReading, bool sendLogout)
		{
			lock (SessionLock)
			{
				if (isGracefull)
				{
					MarkShutdownAsGraceful();
				}

				if (reasonType != null)
				{
					LastDisconnectReason = reasonType;
				}

				if (sendLogout)
				{
					var sessionState = SessionState;
					if (sessionState == SessionState.Connected || sessionState == SessionState.WaitingForLogon || sessionState == SessionState.LogonReceived)
					{
						if (TryStartSendingLogout())
						{
							SendLogoff(reasonDescription, isForced, continueReading);
						}
					}
					else if (reasonType != DisconnectReason.Reject)
					{
						//don't show warning for
						var message = "Disconnect while not connected (" + SessionState + "): " + reasonDescription;
						Log.Warn(message);
					}
				}
				else
				{
					SetPreLogoffSessionState(isForced, continueReading);
					Log.Info("Session was disconnected without logout message.");
				}
			}
		}

		/// <inheritdoc/>
		public virtual bool TryStartSendingLogout()
		{
			var wasSendingLogout = AlreadySendingLogout.AtomicExchange(true);
			return !wasSendingLogout;
		}

		/// <inheritdoc/>
		public virtual void Shutdown(DisconnectReason reason, bool blocking)
		{
			var wasShuttingDown = AlreadyShuttingDown.AtomicExchange(true);
			if (wasShuttingDown)
			{
				if (blocking)
				{
					Log.Debug($"Waiting for shutdown: {Thread.CurrentThread.Name}");
					WaitUntilPumperShutdown();
					WaitUntilReaderShutdown();
					Log.Debug($"WorkerThread was shutdown: {Thread.CurrentThread.Name}");
					CloseTransport(); // if prev call of shutdown is hang, this call release pumper and reader
				}
			}
			else
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Shutting down:" + this);
				}

				if (Thread.CurrentThread == Pumper?.WorkerThread || Thread.CurrentThread == Reader?.WorkerThread)
				{
					var shutdownWaiter = new Thread(() => InternalShutdown(reason)) { Name = "Shutdown:" + SessionId };
					//temporary remove async read/write thread closing
					shutdownWaiter.Start();
				}
				else
				{
					InternalShutdown(reason);
				}
			}

			SequenceManager.SaveProcessedSeqNumberOnShutdown();
		}

		private void InternalShutdown(DisconnectReason reason)
		{
			lock (SessionLock)
			{
				if (Pumper != null)
				{
					Pumper.Shutdown();
					Log.Debug("Pumper stopped");
				}
				else
				{
					Log.Debug("Pumper not initialized");
				}

				CloseTransport();
				if (Reader != null)
				{
					Reader.Shutdown();
					Log.Debug("Reader stopped");
				}
				else
				{
					Log.Debug("Reader not initialized");
				}

				SequenceManager.SaveProcessedSeqNumberOnShutdown();
				if (SessionState.IsNotDisconnected(SessionState))
				{
					if (reason != null)
					{
						LastDisconnectReason = reason;
					}

					SessionState = Graceful ? SessionState.Disconnected : SessionState.DisconnectedAbnormally;
				}
			}
		}

		/// <summary>
		/// Close the transport.
		/// </summary>
		private void CloseTransport()
		{
			lock (SessionLock)
			{
				try
				{
					Transport?.Close();
				}
				catch (IOException e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Close transport thrown error. Cause: " + e.Message, e);
					}
					else
					{
						Log.Warn("Close transport thrown error. Cause: " + e.Message);
					}
				}
			}
		}

		private void WaitUntilReaderShutdown()
		{
			try
			{
				if (Reader?.WorkerThread != null && Thread.CurrentThread != Reader.WorkerThread)
				{
					while (Reader.WorkerThread.IsAlive)
					{
						Reader.WorkerThread.Join(10);
						try
						{
							Monitor.Wait(this, 10);
						}
						catch (SynchronizationLockException)
						{
							// do nothing
						}
					}
				}
			}
			catch (ThreadInterruptedException)
			{
				// do nothing
			}
		}

		private void WaitUntilPumperShutdown()
		{
			try
			{
				if (Pumper?.WorkerThread != null && Thread.CurrentThread != Pumper.WorkerThread)
				{
					while (Pumper.WorkerThread.IsAlive)
					{
						Pumper.WorkerThread.Join(10);
						try
						{
							Monitor.Wait(this, 10);
						}
						catch (SynchronizationLockException)
						{
							// do nothing
						}
					}
				}
			}
			catch (ThreadInterruptedException)
			{
				// intentionally blank
			}
		}

		/// <summary>
		/// Sends logout over "Out of turn" mode. Only sessions messages will be sends.
		/// </summary>
		private void SendLogoff(string reason, bool forced, bool continueReading)
		{
			Queue.OutOfTurnOnlyMode = true;
			FixMessage list = null;
			if (!string.IsNullOrEmpty(reason))
			{
				list = new FixMessage();
				list.AddTag(Tags.Text, reason);
			}

			SetPreLogoffSessionState(forced, continueReading);
			SendMessageOutOfTurn("5", list);
		}

		public virtual void SetPreLogoffSessionState(bool forced, bool continueReading)
		{
			var newState = SessionState.WaitingForLogoff;
			if (forced)
			{
				newState = continueReading
					? SessionState.WaitingForForcedLogoff
					: SessionState.WaitingForForcedDisconnect;
			}

			SessionState = newState;
		}

		/// <inheritdoc />
		public virtual bool SendMessage(string type, FixMessage content, FixSessionSendingType optionMask)
		{
			return SendMessageAndGetQueueSize(type, content, optionMask) == 0;
		}

		/// <inheritdoc />
		public virtual int SendMessageAndGetQueueSize(string type, FixMessage content, FixSessionSendingType optionMask)
		{
			ValidateMessageType(type, content);
			VerifySessionState();
			return Pumper.Send(type, content, optionMask);
		}

		/// <inheritdoc />
		public virtual bool SendMessage(string type, FixMessage content)
		{
			ValidateMessageType(type, content);
			VerifySessionState();
			return Pumper.Send(type, content, FixSessionSendingType.DefaultSendingOption) == 0;
		}

		private void ValidateMessageType(string type, FixMessage content)
		{
			// System.out.println("Hello");
			if (string.IsNullOrEmpty(type) && (content == null || !content.IsTagExists(Tags.MsgType)))
			{
				throw new ArgumentException("invalid message or message type: " + (content == null ? null : content.ToString()));
			}
		}

		/// <inheritdoc />
		public virtual bool SendAsIs(FixMessage message, FixSessionSendingType options)
		{
			ValidateMessage(message);
			VerifySessionState();
			return Pumper.Send(null, message, options) == 0;
		}

		/// <inheritdoc />
		public virtual bool SendAsIs(FixMessage message)
		{
			return SendAsIs(message, FixSessionSendingType.DefaultSendingOption);
		}

		private void ValidateMessage(FixMessage message)
		{
			if (message == null || !message.IsTagExists(Tags.MsgType))
			{
				throw new ArgumentException("invalid message or message type");
			}
		}

		/// <inheritdoc />
		public virtual bool SendWithChanges(FixMessage content, ChangesType allowedChangesType, FixSessionSendingType options)
		{
			//TODO: make it possible to send message synchronous
			return SendWithChangesAndOptionGetQueueSize(content, allowedChangesType, options) == 0;
		}

		/// <inheritdoc />
		public virtual int SendWithChangesAndGetQueueSize(FixMessage content, ChangesType allowedChangesType, FixSessionSendingType options)
		{
			//TODO: make it possible to send message synchronous
			return SendWithChangesAndOptionGetQueueSize(content, allowedChangesType, options);
		}

		/// <inheritdoc />
		public virtual bool SendWithChanges(FixMessage content, ChangesType? allowedChangesType)
		{
			return SendWithChangesAndOptionGetQueueSize(content, allowedChangesType, FixSessionSendingType.DefaultSendingOption) == 0;
		}

		public virtual int SendWithChangesAndGetQueueSize(FixMessage content, ChangesType allowedChangesType)
		{
			return SendWithChangesAndOptionGetQueueSize(content, allowedChangesType, FixSessionSendingType.DefaultSendingOption);
		}

		private int SendWithChangesAndOptionGetQueueSize(FixMessage content, ChangesType? allowedChangesType, FixSessionSendingType options)
		{
			if (allowedChangesType == null)
			{
				throw new ArgumentNullException("allowedChangesType is null");
			}

			ValidateMessage(content);
			VerifySessionState();
			return Pumper.Send(content, allowedChangesType, options);
		}

		protected internal void VerifySessionState()
		{
			if (SessionState.Dead == SessionState)
			{
				throw new InvalidOperationException("session is disposed");
			}
		}

		/// <inheritdoc />
		public virtual FixMessage PrepareMessage(FixMessage message, MessageStructure structure)
		{
			return _preparedMessageUtil.PrepareMessage(message, structure);
		}

		/// <inheritdoc />
		public virtual FixMessage PrepareMessage(FixMessage message, string type, MessageStructure structure)
		{
			return _preparedMessageUtil.PrepareMessage(message, type, structure);
		}

		/// <inheritdoc />
		public virtual FixMessage PrepareMessage(string msgType, MessageStructure userStructure)
		{
			return _preparedMessageUtil.PrepareMessage(msgType, userStructure);
		}

		/// <inheritdoc />
		public virtual FixMessage PrepareMessageFromString(byte[] message, MessageStructure structure)
		{
			return _preparedMessageUtil.PrepareMessageFromString(message, structure);
		}

		/// <inheritdoc />
		public virtual FixMessage PrepareMessageFromString(byte[] message, string type, MessageStructure structure)
		{
			return _preparedMessageUtil.PrepareMessageFromString(message, type, structure);
		}

		/// <inheritdoc />
		public virtual bool SendMessage(FixMessage message)
		{
			return SendMessage(DontRedefineType, message, FixSessionSendingType.DefaultSendingOption);
		}

		/// <inheritdoc />
		public virtual bool SendMessage(FixMessage message, FixSessionSendingType optionMask)
		{
			return SendMessage(DontRedefineType, message, optionMask);
		}

		/// <inheritdoc />
		public virtual int SendMessageAndGetQueueSize(FixMessage message, FixSessionSendingType optionMask)
		{
			return SendMessageAndGetQueueSize(DontRedefineType, message, optionMask);
		}

		/// <inheritdoc />
		public virtual bool SendMessageOutOfTurn(string type, FixMessage message)
		{
			ValidateMessageType(type, message);

			NotifyOutSessionLevelListeners(type, message);

			return Pumper?.SendOutOfTurn(type, message) ?? false;
		}

		private void NotifyOutSessionLevelListeners(string type, FixMessage message)
		{
			if (_outSessionLevelListeners.Count > 0)
			{
				var msgType = !string.IsNullOrEmpty(type) ? type : message.GetTagValueAsString(Tags.MsgType);

				if (msgType != null && RawFixUtil.IsSessionLevelType(msgType))
				{
					foreach (var listener in _outSessionLevelListeners)
					{
						listener.OnMessage(msgType, message);
					}
				}
			}
		}

		/// <inheritdoc />
		public virtual IMessageValidator MessageValidator
		{
			get { return _messageValidator; }
		}

		/// <inheritdoc />
		public virtual void Dispose()
		{
			Shutdown(DisconnectReason.GetDefault(), true);
			Listener?.Dispose();
			Queue.Dispose();
			FixSessionManager.Instance.RemoveFixSession(this);
			if (SessionState != SessionState.Dead)
			{
				SessionState = SessionState.Dead;
			}

			_sessionStateListenerObserver.RemoveAllListeners();
			if (StorageFactory is IClosable)
			{
				((IClosable)StorageFactory).Close();
			}

			Scheduler?.Shutdown();
			Scheduler = null;
		}

		/// <summary>
		/// Send test request.
		/// </summary>
		public virtual void SendTestRequest()
		{
			var currentTime = DateTimeHelper.CurrentMilliseconds;
			TestRequestTime.Value = currentTime;
			_testRequest.Set(Tags.TestReqID, TestRequestTime.AsByteArray());

			SetAttribute(ExtendedFixSessionAttribute.LastSentTestReqId.Name, TestRequestTime);

			var oldSentTrNum = (MutableInteger)GetAttribute(ExtendedFixSessionAttribute.SentTestReqNumberId.Name);
			if (oldSentTrNum == null)
			{
				SentTrNum.SetNumber(1);
			}
			else
			{
				SentTrNum.SetNumber(oldSentTrNum.GetNumber() + 1);
			}
			SetAttribute(ExtendedFixSessionAttribute.SentTestReqNumberId.Name, SentTrNum);
			if (Log.IsDebugEnabled)
			{
				Log.Trace("TestRequest sent: " + currentTime);
			}

			SendMessageOutOfTurn(MsgType.TestRequest, _testRequest);
		}

		/// <summary>
		/// The method checks if TR send or received.
		/// <para>
		/// If session is not received any messages during HB interval the HB will be send and
		/// If no response received session will be disconnected;
		/// </para>
		/// <para>
		/// This is helper method for session task.
		/// </para>
		/// </summary>
		public virtual void CheckHasSessionSendOrReceivedTestRequest()
		{
			var currentTime = DateTimeHelper.CurrentMilliseconds;
			var noOfMillisAfterLastMessage = currentTime - LastInMessageTimestamp;
			var hbiAndReasonableTransmissionTime = GetHbiPlusReasonableTransmissionTimeMillis();
			if (noOfMillisAfterLastMessage >= hbiAndReasonableTransmissionTime)
			{
				// we didn't receive anything for HBI+RTT seconds
				if (!HasTestRequestBeenSent())
				{ // we didn't sent any test requests yet
					SendTestRequest();
				}
				else
				{
					CheckForTestRequestAnswer(hbiAndReasonableTransmissionTime);
				}
			}
			else if (HasTestRequestBeenSent())
			{
				CheckForTestRequestAnswer(hbiAndReasonableTransmissionTime);
			}
		}

		private void CheckForTestRequestAnswer(int reasonableHeartbeatTime)
		{
			var lastTimeOfAllowedInactivity = DateTimeHelper.CurrentMilliseconds - reasonableHeartbeatTime;
			if (HasTestRequestReplyBeenReceived())
			{
				RestoreSessionAfterReceivedTestRequest();
			}
			else if (LastInMessageTimestamp >= lastTimeOfAllowedInactivity)
			{
				// any message received, session state restored
				Log.Debug("Reset TestRequest flags because messages received after request");
				ResetTestRequestFlags();
			}
			else
			{
				var timestamp = (TestReqIdTimestamp)GetAttribute(ExtendedFixSessionAttribute.LastSentTestReqId.Name); // can't be null because of the if above
				var timeSinceLastHeartbeatSentInMillis = DateTimeHelper.CurrentMilliseconds - timestamp.Value;

				if (timeSinceLastHeartbeatSentInMillis > reasonableHeartbeatTime)
				{
					var curSentTrNum = (MutableInteger)GetAttribute(ExtendedFixSessionAttribute.SentTestReqNumberId.Name);
					var maxAttempts = ConfigAdapter.TestRequestsNumberUponDisconnection;
					if (curSentTrNum == null || curSentTrNum.GetNumber() < maxAttempts)
					{
						SendTestRequest();
					}
					else
					{
						DisconnectTestRequestIsLost();
					}
				}
			}
		}

		public virtual void CheckSessionInactiveAndSendHbt()
		{
			var currentTime = DateTimeHelper.CurrentMilliseconds;
			var noOfMillisAfterLastMessage = currentTime - LastOutMessageTimestamp;
			var hbi = Parameters.HeartbeatInterval * Second;

			if (noOfMillisAfterLastMessage >= hbi)
			{
				SendMessageOutOfTurn("0", null);
			}
		}

		/// <summary>
		/// Disconnect the session if test request doesn't received.
		/// </summary>
		public virtual void DisconnectTestRequestIsLost()
		{
	//        String timestamp = (String) GetAttribute(LastSentTestReqID.Name); // can't be null because of the if above
	//        long timeSinceLastHeartbeatSentInMillis = (System.currentTimeMillis() - Long.parseLong(timestamp));// / SECOND;
	//        final int reasonableHeartbeatTime = getReasonableHeartbeatTimeMillis();
	//        if (timeSinceLastHeartbeatSentInMillis > reasonableHeartbeatTime) {
			var testRequestId = GetAttribute(ExtendedFixSessionAttribute.LastSentTestReqId.Name);
			if (Log.IsWarnEnabled)
			{
				Log.Warn("TestRequest reply for " + testRequestId + " is not received. Session " + SessionId + " will be terminated.");
			}
			RemoveAttribute(ExtendedFixSessionAttribute.LastSentTestReqId.Name);
			RemoveAttribute(ExtendedFixSessionAttribute.LastReceivedTestReqId.Name);
			//disconnect without sending Logout
			Disconnect(DisconnectReason.NoAnswer, "Test request " + testRequestId + " reply hasn't been received within specified time period", false, true, true, false);
		}

		/// <summary>
		/// Returns configured HBI plus configured "reasonable transmission time"
		/// </summary>
		/// <returns>HBI + RTT in milliseconds</returns>
		private int GetHbiPlusReasonableTransmissionTimeMillis()
		{
			var heartbeatIntervalSec = Parameters.HeartbeatInterval;
			return heartbeatIntervalSec * Second + _reasonableTransmissionTimeMillis;
		}


		/// <summary>
		/// Restore the session if test request is received.
		/// </summary>
		public virtual void RestoreSessionAfterReceivedTestRequest()
		{
			Log.Debug("TestRequest reply received");
			ResetTestRequestFlags();
		}

		public virtual void ResetTestRequestFlags()
		{
			RemoveAttribute(ExtendedFixSessionAttribute.LastReceivedTestReqId.Name);
			RemoveAttribute(ExtendedFixSessionAttribute.LastSentTestReqId.Name);
			RemoveAttribute(ExtendedFixSessionAttribute.SentTestReqNumberId.Name);
		}

		private bool HasTestRequestReplyBeenReceived()
		{
			var atrSent = GetAttribute(ExtendedFixSessionAttribute.LastSentTestReqId.Name);
			var atrReceived = GetAttribute(ExtendedFixSessionAttribute.LastReceivedTestReqId.Name);
			if (atrSent == null || atrReceived == null)
			{
				return false;
			}
			var sentReqId = (TestReqIdTimestamp)atrSent;
			var receivedReqId = (TagValue)atrReceived;
			return EqualsArrays(sentReqId.AsByteArray(), 0, sentReqId.AsByteArray().Length, receivedReqId.Buffer, receivedReqId.Offset, receivedReqId.Length);
		}

		private bool EqualsArrays(byte[] b1, int offset1, int length1, byte[] b2, int offset2, int length2)
		{
			if (length1 != length2)
			{
				return false;
			}
			for (var i = 0; i < length1; i++)
			{
				if (b1[offset1 + i] != b2[offset2 + i])
				{
					return false;
				}
			}
			return true;
		}

		private bool HasTestRequestBeenSent()
		{
			return GetAttribute(ExtendedFixSessionAttribute.LastSentTestReqId.Name) != null;
		}

		private void InitAttributes()
		{
			for (var i = 0; i < _longAttrs.Length; i++)
			{
				_longAttrs[i] = Common.Constants.LongNull;
			}
		}

		/// <inheritdoc />
		public virtual void SetAttribute(ExtendedFixSessionAttribute attr, long value)
		{
			_longAttrs[attr.Ordinal()] = value;
		}

		public virtual void RemoveAttribute(ExtendedFixSessionAttribute attr)
		{
			_longAttrs[attr.Ordinal()] = Common.Constants.LongNull;
			_boolAttrs[attr.Ordinal()] = false;
			//RemoveAttribute(attr.Name);
		}

		public virtual void RemoveLongAttribute(ExtendedFixSessionAttribute attr)
		{
			_longAttrs[attr.Ordinal()] = Common.Constants.LongNull;
		}

		/// <inheritdoc />
		public virtual long GetAttributeAsLong(ExtendedFixSessionAttribute attr)
		{
			return _longAttrs[attr.Ordinal()];
		}

		/// <inheritdoc />
		public virtual void SetAttribute(ExtendedFixSessionAttribute attr, bool value)
		{
			_boolAttrs[attr.Ordinal()] = value;
		}

		/// <inheritdoc />
		public virtual bool GetAttributeAsBool(ExtendedFixSessionAttribute attr)
		{
			return _boolAttrs[attr.Ordinal()];
		}

		/// <inheritdoc />
		public virtual void SetAttribute(string key, object value)
		{
			_attributes[key] = value;
			if (_attributeListeners.ContainsKey(key))
			{
				var listeners = _attributeListeners[key];
				foreach (var lstnr in listeners)
				{
					lstnr.OnAttributeSet(value);
				}
			}
		}

		/// <inheritdoc />
		public virtual void SetAttribute(ExtendedFixSessionAttribute key, object @object)
		{
			SetAttribute(key.Name, @object);
		}

		/// <inheritdoc />
		public virtual object GetAttribute(string key)
		{
			return _attributes.GetValueOrDefault(key);
		}

		/// <inheritdoc />
		public virtual object GetAttribute(ExtendedFixSessionAttribute key)
		{
			return GetAttribute(key.Name);
		}

		/// <inheritdoc />
		public virtual object GetAndRemoveAttribute(string key)
		{
			var result = GetAttribute(key);
			if (result != null)
			{
				RemoveAttribute(key);
			}
			return result;
		}

		/// <inheritdoc />
		public virtual void RemoveAttribute(string key)
		{
			_attributes.Remove(key);
			if (_attributeListeners.ContainsKey(key))
			{
				var listeners = _attributeListeners[key];
				foreach (var lstnr in listeners)
				{
					lstnr.OnAttributeRemoved();
				}
			}
		}

		/// <inheritdoc />
		public virtual void SubscribeForAttributeChanges(ExtendedFixSessionAttribute attr, IExtendedFixSessionAttributeListener listener)
		{
			var attrName = attr.Name;
			if (_attributeListeners.ContainsKey(attrName))
			{
				_attributeListeners[attrName].Add(listener);
			}
			else
			{
				_attributeListeners[attrName] = new ConcurrentBag<IExtendedFixSessionAttributeListener>(new[] { listener });
			}
		}

		/// <inheritdoc />
		public virtual void AddSessionStateListener(IFixSessionStateListener stateListener)
		{
			_sessionStateListenerObserver.AddListener(stateListener);
		}

		/// <inheritdoc />
		public virtual void RemoveSessionStateListener(IFixSessionStateListener stateListener)
		{
			_sessionStateListenerObserver.RemoveListener(stateListener);
		}

		public override string ToString()
		{
			return Parameters.SessionId.ToString();
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o == null || GetType() != o.GetType())
			{
				return false;
			}

			var that = (AbstractFixSession)o;

			if (Parameters != null && that.Parameters != null)
			{
				if (!Parameters.SessionId.Equals(that.Parameters.SessionId))
				{
					return false;
				}
			}

			return true;
		}

		public override int GetHashCode()
		{
			return Parameters?.SessionId?.GetHashCode() ?? 0;
		}

		/// <inheritdoc />
		public virtual DisconnectReason LastDisconnectReason
		{
			get { return _disconnectReason; }
			set
			{
				if (Log.IsTraceEnabled)
				{
					Log.Trace("Set disconnect reason " + value + " for session " + SessionId);
				}

				_disconnectReason = value;
			}
		}

		private class ExtendedFixSessionListenerAdapter : IExtendedFixSessionListener
		{
			public virtual void OnMessageReceived(MsgBuf msgBuf)
			{
			}

			public virtual void OnMessageSent(byte[] bytes, int offset, int length)
			{
			}

			public virtual void OnSessionStateChange(SessionState sessionState)
			{
			}

			public virtual void OnNewMessage(FixMessage message)
			{
			}
		}

		private class RejectFixSessionListenerAdapter : IRejectMessageListener
		{
			protected internal static readonly ILog Log = LogFactory.GetLog(typeof(RejectFixSessionListenerAdapter));

			public virtual void OnRejectMessage(FixMessage message)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Message wasn't sent: " + message.ToPrintableString());
				}
			}
		}

		private class FixSessionOutOfSyncListenerAdapter : IFixSessionOutOfSyncListener
		{
			private readonly AbstractFixSession _session;

			public FixSessionOutOfSyncListenerAdapter(AbstractFixSession session)
			{
				_session = session;
			}

			public void OnGapDetected(long lastProcessedSeqNum, long receivedSeqNum)
			{
				if (_session.Log.IsDebugEnabled)
				{
					_session.Log.Debug("Detected incoming sequence gap in session " + _session.SessionId + ": " + lastProcessedSeqNum + "-" + receivedSeqNum);
				}
			}

			public void OnGapClosed(long lastProcessedSeqNum)
			{
				if (_session.Log.IsDebugEnabled)
				{
					_session.Log.Debug("Incoming sequence gap in session " + _session.SessionId + " closed. Last processed seq num is " + lastProcessedSeqNum);
				}
			}

			public void OnResendRequestReceived(long gapStart, long gapEnd)
			{
				if (_session.Log.IsDebugEnabled)
				{
					_session.Log.Debug("Received ResendRequest message for session " + _session.SessionId + " for gap: " + gapStart + "-" + gapEnd);
				}
			}

			public void OnResendRequestProcessed(long gapEnd)
			{
				if (_session.Log.IsDebugEnabled)
				{
					_session.Log.Debug("Received ResendRequest message for session " + _session.SessionId + " processed. Last re-send message sequence " + "is " + gapEnd);
				}
			}

			public void OnResendRequestSent(long gapStart, long gapEnd)
			{
				if (_session.Log.IsDebugEnabled)
				{
					_session.Log.Debug("ResendRequest message with gap (" + gapStart + "-" + gapEnd + ") for session " + _session.SessionId + " was sent");
				}
			}

			public void OnGapFillReceived(FixMessage sequenceResetGapFillMessage)
			{
				if (_session.Log.IsDebugEnabled)
				{
					_session.Log.Debug("GapFill message for session " + _session.SessionId + " is received: " + sequenceResetGapFillMessage.ToPrintableString());
				}
			}

			public void OnGapFillSent(FixMessage sequenceResetGapFillMessage)
			{
				if (_session.Log.IsDebugEnabled)
				{
					_session.Log.Debug("GapFill message for session " + _session.SessionId + " is sent: " + sequenceResetGapFillMessage.ToPrintableString());
				}
			}
		}

		private class FixSessionListenerObserver : IFixSessionListener
		{
			private readonly AbstractFixSession _session;

			public FixSessionListenerObserver(AbstractFixSession session)
			{
				_session = session;
			}

			internal IList<IFixSessionStateListener> FixSessionListeners = new List<IFixSessionStateListener>();
			internal IFixSessionStateListener[] ExtendedFixSessionListeners = new IFixSessionStateListener[0];

			public virtual void AddListener(IFixSessionStateListener extendedFixSessionListener)
			{
				lock (FixSessionListeners)
				{
					FixSessionListeners.Add(extendedFixSessionListener);
					var extendedFixSessionListeners = ((List<IFixSessionStateListener>)FixSessionListeners).ToArray();
					ExtendedFixSessionListeners = extendedFixSessionListeners;
				}
			}

			public virtual void RemoveListener(IFixSessionStateListener extendedFixSessionListener)
			{
				lock (FixSessionListeners)
				{
					FixSessionListeners.Remove(extendedFixSessionListener);

					var extendedFixSessionListeners = ((List<IFixSessionStateListener>)FixSessionListeners).ToArray();

					ExtendedFixSessionListeners = extendedFixSessionListeners;
				}
			}

			/// <inheritdoc />
			public virtual void OnSessionStateChange(SessionState sessionState)
			{
				IFixSessionStateListener[] listeners;
				lock (FixSessionListeners)
				{
					listeners = ExtendedFixSessionListeners;
				}

				foreach (var listener in listeners)
				{
					try
					{
						listener.OnSessionStateChange(new StateEvent(_session, sessionState));
					}
					catch (Exception)
					{
					}
				}
			}

			/// <inheritdoc />
			public virtual void OnNewMessage(FixMessage message)
			{
				// no supported for observers
			}

			public virtual void RemoveAllListeners()
			{
				lock (FixSessionListeners)
				{
					FixSessionListeners.Clear();
					ExtendedFixSessionListeners = Array.Empty<IFixSessionStateListener>();
				}
			}
		}

		private class SlowConsumerListenerAdapter : IFixSessionSlowConsumerListener
		{
			protected internal static readonly ILog Log = LogFactory.GetLog(typeof(SlowConsumerListenerAdapter));

			public virtual void OnSlowConsumerDetected(SlowConsumerReason reason, long expected, long current)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Slow consumer detected: " + reason + "; " + expected + " / " + current);
				}
			}
		}

		private class SessionStateAdapter : IFixSessionListener
		{
			private readonly FixSessionListenerObserver _sessionListenerObserver;
			private readonly IFixSessionListener _listener;

			public SessionStateAdapter(FixSessionListenerObserver sessionListenerObserver, IFixSessionListener listener)
			{
				_sessionListenerObserver = sessionListenerObserver;
				_listener = listener;
			}

			/// <inheritdoc />
			public virtual void OnSessionStateChange(SessionState sessionState)
			{
				_listener.OnSessionStateChange(sessionState); // todo may be move to session observers
				_sessionListenerObserver.OnSessionStateChange(sessionState);
			}

			/// <inheritdoc />
			public virtual void OnNewMessage(FixMessage message)
			{
				_listener.OnNewMessage(message);
			}
		}

		private class ConsumingControlTransportWrapper : IFixTransport
		{
			private readonly AbstractFixSession _session;
			private readonly long _transportWriteDelayThreshold;

			public ConsumingControlTransportWrapper(AbstractFixSession session)
			{
				_session = session ?? throw new ArgumentNullException(nameof(session));
				_transportWriteDelayThreshold = _session.ConfigAdapter.Configuration.GetPropertyAsLong(Config.SlowConsumerWriteDelayThreshold);
			}

			/// <inheritdoc />
			public bool IsBlockingSocket => _session.Transport.IsBlockingSocket;

			/// <inheritdoc />
			public void ReadMessage(MsgBuf buf)
			{
				_session.Transport.ReadMessage(buf);
			}

			/// <inheritdoc />
			public void Write(byte[] message)
			{
				Write(message, 0, message.Length);
			}

			/// <inheritdoc />
			public void WaitUntilReadyToWrite()
			{
				_session.Transport.WaitUntilReadyToWrite();
			}

			/// <inheritdoc />
			public void Close()
			{
				_session.Transport.Close();
			}

			/// <inheritdoc />
			public int OptimalBufferSize => _session.Transport.OptimalBufferSize;

			/// <inheritdoc />
			public string RemoteHost
			{
				get { return _session.Transport.RemoteHost; }
			}

			/// <inheritdoc />
			public int Write(byte[] message, int offset, int length)
			{
				var start = DateTimeHelper.CurrentMilliseconds;
				var written = _session.Transport.Write(message, offset, length);
				Handle(length, written, start, DateTimeHelper.CurrentMilliseconds);
				return written;
			}

			/// <inheritdoc />
			public int Write(ByteBuffer buf, int offset, int length)
			{
				var start = DateTimeHelper.CurrentMilliseconds;
				var written = _session.Transport.Write(buf, offset, length);
				Handle(length, written, start, DateTimeHelper.CurrentMilliseconds);
				return written;
			}

			public void Handle(int toWrite, int written, long begin, long end)
			{
				var delay = end - begin;

				if (delay > _transportWriteDelayThreshold)
				{
					_session.SlowConsumerListener.OnSlowConsumerDetected(SlowConsumerReason.TransportWriteDelay, _transportWriteDelayThreshold, delay);
				}
				if (written < toWrite)
				{
					_session.SlowConsumerListener.OnSlowConsumerDetected(SlowConsumerReason.TransportWriteNotComplete, toWrite, written);
				}

			}
		}

		public void BackupStorages()
		{
			try
			{
				IncomingStorage.BackupStorage(Parameters);
			}
			catch (IOException e)
			{
				Log.Error("Error on backup incoming storage. Cause: " + e.Message, e);
				throw;
			}

			try
			{
				OutgoingStorage.BackupStorage(Parameters);
			}
			catch (IOException e)
			{
				Log.Error("Error on backup outgoing storage. Cause: " + e.Message, e);
				throw;
			}
		}

		/// <inheritdoc />
		public long InSeqNum
		{
			get
			{
				lock (SessionLock)
				{
					if (!_initialized)
					{
						return Parameters.IncomingSequenceNumber > 0
							? Parameters.IncomingSequenceNumber
							: RuntimeState.InSeqNum;
					}

					return RuntimeState.InSeqNum;
				}
			}
			set => SetSequenceNumbers(value, -1);
		}

		/// <inheritdoc />
		public long OutSeqNum
		{
			get
			{
				lock (SessionLock)
				{
					if (!_initialized)
					{
						return Parameters.OutgoingSequenceNumber > 0
							? Parameters.OutgoingSequenceNumber
							: RuntimeState.OutSeqNum;
					}

					return RuntimeState.OutSeqNum;
				}
			}
			set => SetSequenceNumbers(-1, value);
		}

		#region Scheduled tasks and methods
		/// <summary>
		/// Register session`s scheduled tasks:
		///		- Scheduled SeqNum reset task
		///		- HB
		///		- TestRequest tasks
		/// </summary>
		private void RegisterSessionTasks()
		{
			// required jobs
			Scheduler.ScheduleHeartbeat(CheckHbAndTestRequestInterval);
			Scheduler.ScheduleTestRequest(CheckHbAndTestRequestInterval);

			// SeqNum reset if configured
			if (ConfigAdapter.IsResetSeqNumTimeEnabled)
			{
				RegisterSeqResetTask();
			}
		}

		/// <summary>
		/// De-register session scheduled tasks:
		///		- SeqNum reset
		///		- HB
		///		- TestRequest
		///		- Session schedule
		/// </summary>
		private void UnRegisterSessionTasks()
		{
			Scheduler?.DescheduleAllTasks();
		}

		private void RegisterSeqResetTask()
		{
			var resetTimeString = ConfigAdapter.ResetSequenceTime;
			var resetTimeZoneString = ConfigAdapter.ResetSequenceTimeZone;

			if (!TimeSpan.TryParseExact(resetTimeString, @"h\:m\:s", null, out var resetTime))
			{
				resetTime = TimeSpan.Zero;
				Log.Warn($"Cannot parse Time '{resetTimeString}', using {resetTime} instead.");
			}

			if (!DateTimeHelper.TryParseTimeZone(resetTimeZoneString, out var resetTimeZone))
			{
				resetTimeZone = TimeZoneInfo.Utc;
				Log.Warn($"Cannot parse Time Zone '{resetTimeZoneString}', using UTC Time Zone instead.");
			}

			Scheduler.ScheduleSeqReset(resetTime, resetTimeZone);
		}
		#endregion
	}
}
