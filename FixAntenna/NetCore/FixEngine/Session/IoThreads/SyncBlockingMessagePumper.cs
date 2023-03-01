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
using System.IO;
using System.Threading;

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads.Bean;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads
{
	/// <summary>
	/// The message pumper writes messages to transport.
	/// </summary>
	/// <seealso cref="IQueue{T}"> </seealso>
	/// <seealso cref="FixMessageWithType"> </seealso>
	/// <seealso cref="MessageStorage"> </seealso>
	/// <seealso cref="IFixMessageFactory"> </seealso>
	/// <seealso cref="IFixTransport"> </seealso>
	internal sealed class SyncBlockingMessagePumper : AffinitySupportThread, IMessagePumper
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(SyncBlockingMessagePumper));
		public static readonly bool TraceEnabled = Log.IsTraceEnabled;

		private const int Second = 1000;

		private int _hbtSeconds;
		private int _shutdownTimeout;
		private int _waitOnSendMillis = Second;
		private IExtendedFixSession _fixSession;
		private readonly IQueue<FixMessageWithType> _queue;
		private IMessageStorage _outgoingLog;
		private IFixMessageFactory _fixMessageFactory;
		private SessionParameters _sessionParameters;
		private FixSessionRuntimeState _runtimeState;
		private ConfigurationAdapter _configurationAdapter;
		private IFixTransport _transport;
		private long _messageProcessedTimestamp;
		private bool _statisticEnabled;
		private MessageStatistic _messageStatistic;
		private int _queueThresholdSize;
		private volatile bool _shutdownFlag;
		private bool _enableMessageRejecting;
		private int _maxMessagesToSendInBatch;
		private SerializationContext _context;
		private ISessionSequenceManager _sequenceManager;

		private MessageBufferWorker _messageBufferWorker;

		internal bool IsTransportBlockingSend;

		private readonly SemaphoreSlim _msgBufferSemaphore = new SemaphoreSlim(1);

		/// <summary>
		/// Creates the <c>SyncBlockingMessagePumper</c>.
		/// </summary>
		/// <param name="extendedFixSession"> the session </param>
		/// <param name="queue">              the output queue </param>
		/// <param name="messageFactory">     the output message storage </param>
		/// <param name="transport">          the transport </param>
		public SyncBlockingMessagePumper(IExtendedFixSession extendedFixSession, IQueue<FixMessageWithType> queue, IMessageStorage @out, IFixMessageFactory messageFactory, IFixTransport transport, ISessionSequenceManager sequenceManager) : base("MPThread>:" + extendedFixSession.Parameters.SessionId)
		{
			_fixSession = extendedFixSession;
			_sessionParameters = extendedFixSession.Parameters;
			_runtimeState = extendedFixSession.RuntimeState;

			_queue = queue;
			_outgoingLog = @out;

			_hbtSeconds = _sessionParameters.HeartbeatInterval;
			_shutdownTimeout = _sessionParameters.Configuration.GetPropertyAsInt(Config.WritingThreadShutdownTimeout);
			if (_shutdownTimeout < 0)
			{
				_waitOnSendMillis = _hbtSeconds;
			}
			_transport = transport;
			_fixMessageFactory = messageFactory;
			_configurationAdapter = new ConfigurationAdapter(_sessionParameters.Configuration);
			_sequenceManager = sequenceManager;
		}


		public long Init()
		{
			if (_messageBufferWorker != null)
			{
				//initialized already
				throw new InvalidOperationException("SyncBlockingMessagePumper is initialized already");
			}

			_queueThresholdSize = _configurationAdapter.ThresholdSize;
			_enableMessageRejecting = _configurationAdapter.IsEnableMessageRejecting;
			_maxMessagesToSendInBatch = _configurationAdapter.MaxMessagesToSendInBatch;
			_waitOnSendMillis = _configurationAdapter.GetWaitForQueuingMessages(Second);

			_messageBufferWorker = new MessageBufferWorker(_maxMessagesToSendInBatch);
			_messageBufferWorker.ResetBuffer();

			_context = new SerializationContext(_fixMessageFactory);

			_statisticEnabled = _configurationAdapter.IsMessageStatisticEnabled;
			if (_statisticEnabled)
			{
				_messageStatistic = new MessageStatistic();
			}

			Interlocked.Exchange(ref _messageProcessedTimestamp, DateTimeHelper.CurrentMilliseconds);

			IsTransportBlockingSend = _transport.IsBlockingSocket;

			return _outgoingLog.Initialize();
		}

		public long MessageProcessedTimestamp => Interlocked.Read(ref _messageProcessedTimestamp);

		public void SetMessageProcessedTimestamp(long timestamp)
		{
			Interlocked.Exchange(ref _messageProcessedTimestamp, timestamp);
		}

		/// <returns> true is statistic is enabled </returns>
		public bool IsStatisticEnabled => _statisticEnabled;

		/// <summary>
		/// Gets statistic of processed messages.
		/// WARNING: Before the call to ensure that the statistics are included.
		/// </summary>
		/// <value> MessageStatistic </value>
		/// <exception cref="InvalidOperationException"> if <c>statisticEnabled</c> is false </exception>
		/// <seealso cref="MessageReader.IsStatisticEnabled"> </seealso>
		public MessageStatistic Statistic
		{
			get
			{
				if (_statisticEnabled)
				{
					return _messageStatistic;
				}

				throw new InvalidOperationException("Message statistic is disabled");
			}
		}

		public bool HasWorkQueued()
		{
			return !_queue.IsEmpty;
		}

		public bool HasAnyWorkQueued()
		{
			return !_queue.IsAllEmpty;
		}

		protected override void Run()
		{
			if (TraceEnabled)
			{
				Log.Trace("Start MPThread: " + _fixSession);
			}

			var configuration = _sessionParameters.Configuration;
			ApplyAffinity(configuration.GetPropertyAsInt(Config.SendCpuAffinity), configuration.GetPropertyAsInt(Config.CpuAffinity));

			Thread.BeginThreadAffinity();
			Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

			try
			{
				while (!_shutdownFlag || HasWorkQueued())
				{
					WaitWhileQueueIsEmptyUntilSendingOfHeartbeat();

					var bufferedMessageCount = CheckQueueAndFillBuffer();

					if (bufferedMessageCount > 0)
					{
						SendMessages(bufferedMessageCount);
					}
					else if (bufferedMessageCount == 0)
					{
						EnqueueHeartbeatToSend();
					}
				}
			}
			catch (Exception ex)
			{
				if (!GracefulShutdown)
				{
					ReportErrorAndShutdown(ex);
				}
				else
				{
					var error = "IOError in message pumper. Some messages have not been sent: " + _queue.TotalSize;
					Log.Debug(error, ex);
				}
			}
			finally
			{
				CloseOutgoingLog();
				if (Log.IsTraceEnabled)
				{
					Log.Trace("Stop MPThread: " + _fixSession);
				}
				Thread.EndThreadAffinity();
			}
		}

		/// <returns> buffered messages count or -1 if buffering is blocked by another thread </returns>
		/// <exception cref="IOException"> </exception>
		private int CheckQueueAndFillBuffer()
		{
			var messageCount = 0;
			lock (_queue)
			{
				//get not real queue size but requested count of messages to send
				var queueSize = _queue.Size;

				if (queueSize > 0)
				{
					if (TraceEnabled)
					{
						Log.Trace(_fixSession + " queue size: " + queueSize);
					}

					if (queueSize > _maxMessagesToSendInBatch)
					{
						queueSize = _maxMessagesToSendInBatch;
					}

					var allowBufferWriting = _msgBufferSemaphore.Wait(0);
					if (allowBufferWriting)
					{
						try
						{
							while (messageCount < queueSize && _messageBufferWorker.Offset < _transport.OptimalBufferSize)
							{
								_messageBufferWorker.IncrementCounter();
								_messageBufferWorker.MarkStartOffset();
								PollAndPrepareToSend(_messageBufferWorker.Buffer);
								_messageBufferWorker.MarkEndOffset();
								messageCount++;
							}
						}
						finally
						{
							_msgBufferSemaphore.Release();
						}
					}
					else
					{
						return -1;
					}
				}
			}
			return messageCount;
		}

		private void ReportErrorAndShutdown(Exception ex)
		{
			var error = "IOError in message pumper. Some messages have not been sent: " + _queue.TotalSize;
			_fixSession.ErrorHandler.OnError(error, ex);
			if (Log.IsDebugEnabled)
			{
				Log.Warn(error, ex);
			}
			else
			{
				Log.Warn(error + ". " + ex.Message);
			}
			_fixSession.Shutdown(DisconnectReason.BrokenConnection, false);
		}

		private void CloseOutgoingLog()
		{
			try
			{
				//do synchronization to avoid closing storage during synchronous sending
				lock (_queue)
				{
					_outgoingLog.Dispose();
				}
			}
			catch (IOException e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("Outgoing Log file cannot be closed", e);
				}
				else
				{
					Log.Warn("Outgoing Log file cannot be closed. " + e.Message);
				}
			}
		}

		private void WaitWhileQueueIsEmptyUntilSendingOfHeartbeat()
		{
			lock (_queue)
			{
				if (!HasWorkQueued() && !_shutdownFlag)
				{
					Monitor.PulseAll(_queue);

					var timeout = _hbtSeconds * Second - (DateTimeHelper.CurrentMilliseconds - MessageProcessedTimestamp);
					if (timeout <= 0)
					{
						timeout = _hbtSeconds * Second;
					}
					SafeWaitMilis(timeout);
				}
			}
		}

		/// <summary>
		/// Sends the heartbeat message.
		/// </summary>
		private void EnqueueHeartbeatToSend()
		{
			if (IsHeartbeatRequired())
			{
				_fixSession.SendMessageOutOfTurn("0", null);
			}
		}

		private bool IsHeartbeatRequired()
		{
			return !_shutdownFlag && _hbtSeconds != 0 && SessionState.IsConnected(_fixSession.SessionState) && ((DateTimeHelper.CurrentMilliseconds - MessageProcessedTimestamp) >= (_hbtSeconds * Second));
		}

		private void UpdateStatistic(int messagesProcessed)
		{
			_messageStatistic.AddMessagesProcessed(messagesProcessed);
			_messageStatistic.AddBytesProcessed(_messageBufferWorker.FullLength);
		}

		public void SendMessages(int messageCount)
		{
			try
			{
				_msgBufferSemaphore.Wait();
				SendMessages(messageCount, null);
			}
			catch (ThreadInterruptedException e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("Message sending was interrupting: " + e.Message, e);
				}
				else
				{
					Log.Warn("Message sending was interrupting: " + e.Message);
				}
			}
			finally
			{
				_msgBufferSemaphore.Release();
			}

		}

		private bool SendMessages(int messageCount, byte[] externalBuffer)
		{
			if (_messageBufferWorker.Counter == -1)
			{
				return true;
			}

			var allWritten = WriteToTransport(messageCount, externalBuffer);

			SetMessageProcessedTimestamp(DateTimeHelper.CurrentMilliseconds);
			//TODO: timing should be optional
			if (_statisticEnabled)
			{
				UpdateStatistic(_messageBufferWorker.Counter + 1);
			}

			for (var i = 0; i <= _messageBufferWorker.Counter ; i++)
			{
				try
				{
					_fixSession.ExtendedFixSessionListener.OnMessageSent(_messageBufferWorker.Buffer.GetByteArray(), _messageBufferWorker.GetStartOffset(i), _messageBufferWorker.GetLength(i));
				}
				catch (Exception e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("ExtendedFIXSessionListener::OnMessageSent thrown error. Cause:" + e.Message, e);
					}
					else
					{
						Log.Warn("ExtendedFIXSessionListener::OnMessageSent thrown error. Cause:" + e.Message);
					}
				}
			}

			_messageBufferWorker.ResetBuffer();

			return allWritten;
		}

		private bool WriteToTransport(int messageCount, byte[] externalBuffer)
		{
			var toWrite = _messageBufferWorker.FullLength;
			int written;
			
			try
			{
				written = _transport.Write(_messageBufferWorker.Buffer, _messageBufferWorker.GetStartOffset(0), toWrite);
			}
			catch (IOException ex)
			{
				var messages = new string[_messageBufferWorker.Counter + 1];
				for (var i = 0; i <= _messageBufferWorker.Counter; i++)
				{
					messages[i] = StringHelper.NewString(_messageBufferWorker.Buffer.GetByteArray(), _messageBufferWorker.GetStartOffset(i), _messageBufferWorker.GetLength(i));
				}
				throw new TransportMessagesNotSentException(ex, messages);
			}
			if (written < toWrite)
			{
				throw new IOException("Transport sent only part of message (" + written + " vs " + toWrite + ")");
			}

			return true;
		}

		private void PrepareToSendWithExternalBuff(FixMessageWithType messageWithType, ByteBuffer messageBuffer)
		{
			var offset = messageBuffer.Offset;
			var fixMessage = messageWithType.FixMessage;
			var type = messageWithType.MessageType;
			if (messageWithType.ChangesType != null)
			{
				_fixMessageFactory.Serialize(fixMessage, messageWithType.ChangesType, messageBuffer, _context);
			}
			else
			{
				_fixMessageFactory.Serialize(null, type, fixMessage, messageBuffer, _context);
			}
			if (!ReferenceEquals(type, null) || messageWithType.ChangesType != null)
			{
				_sequenceManager.IncrementOutSeqNum();
			}
			var endPoint = messageBuffer.Offset;
			SaveMessageInOutLog(messageBuffer, offset, endPoint - offset);
		}


		//TODO: looks like we need 2 methods - for MsgBuf and for global buff
		[Obsolete]
		private bool PrepareToSend(MsgBuf buf, FixMessageWithType messageWithType, ByteBuffer messageBuffer)
		{
			var offset = messageBuffer.Offset;
			var useOrigMsgBuffer = false;
			var fixMessage = messageWithType.FixMessage;
			var type = messageWithType.MessageType;
			if (messageWithType.ChangesType != null)
			{
				_fixMessageFactory.Serialize(fixMessage, messageWithType.ChangesType, messageBuffer, _context);
			}
			else
			{
				_fixMessageFactory.Serialize(buf, type, fixMessage, messageBuffer, _context);
				useOrigMsgBuffer = buf != null && buf.Buffer != null;
			}
			if (!ReferenceEquals(type, null) || messageWithType.ChangesType != null)
			{
				_sequenceManager.IncrementOutSeqNum();
			}

			if (useOrigMsgBuffer)
			{
				SaveMessageInOutLog(buf.Buffer, buf.Offset, buf.Length);
				// postpone calling releaseMessageIfNeeded until we are done sending the message buffer to socket
				// useOrigMsgBuffer can be set true only when sending 1 message synchronously (bypassing the queue)
			}
			else
			{
				var endPoint = messageBuffer.Offset;
				SaveMessageInOutLog(messageBuffer, offset, endPoint - offset);
			}

			return useOrigMsgBuffer;
		}

		private void ReleaseMessageIfNeeded(FixMessageWithType messageWithType)
		{
			var fixMessage = messageWithType.FixMessage;
			messageWithType.ReleaseInstance();
			ReleaseMessageIfNeeded(fixMessage);
		}

		private void ReleaseMessageIfNeeded(FixMessage fixMessage)
		{
			if (fixMessage != null && fixMessage.NeedReleaseAfterSend)
			{
				fixMessage.ReleaseInstance();
			}
		}

		private void PollAndPrepareToSend(ByteBuffer messageBuffer)
		{
			var messageWithType = _queue.Poll();
			PrepareToSendWithExternalBuff(messageWithType, messageBuffer);
			_queue.Commit();
			ReleaseMessageIfNeeded(messageWithType);
		}

		private void SaveMessageInOutLog(ByteBuffer messageBuffer, int offset, int length)
		{
			_outgoingLog.AppendMessage(messageBuffer.GetByteArray(), offset, length);
		}

		private void SaveMessageInOutLog(byte[] buffer, int offset, int length)
		{
			_outgoingLog.AppendMessage(buffer, offset, length);
		}

		private void SafeWait(int seconds)
		{
			SafeWaitMilis(seconds * Second);
		}

		private void SafeWaitMilis(int seconds)
		{
			try
			{
				Monitor.Wait(_queue, TimeSpan.FromMilliseconds(seconds));
			}
			catch (ThreadInterruptedException)
			{
				// ignore
			}
		}

		private void SafeWaitMilis(long seconds)
		{
			try
			{
				Monitor.Wait(_queue, TimeSpan.FromMilliseconds(seconds));
			}
			catch (ThreadInterruptedException)
			{
				// ignore
			}
		}

		/// <summary>
		/// Shutdown the pumper.
		/// <p/>
		/// This method calls engine before the session is close.
		/// This is blocked method.
		/// If queue has the messages and if message rejecting is </summary>
		/// enabled, the messages are rejecting, <seealso cref="IRejectMessageListener">.
		/// This methods should be called from other thread </seealso>
		public override void Shutdown()
		{
			_shutdownFlag = true;
			lock (_queue)
			{
				Monitor.PulseAll(_queue);
			}

			try
			{
				if (WorkerThread.IsAlive)
				{
					if (Thread.CurrentThread != WorkerThread)
					{
						WorkerThread.Join(Math.Max(Second, _shutdownTimeout * Second));
						if (WorkerThread.IsAlive)
						{
							WorkerThread.Interrupt();
						}
					}
				}
			}
			catch (ThreadInterruptedException)
			{
				// intentionally blank
			}

			CloseOutgoingLog();
			Log.Debug("Pumper stopped");

			if (_enableMessageRejecting)
			{
				Log.Debug("Pumper reject message");
				RejectQueueMessages();
			}
		}

		/// <summary>
		/// Reject all non send message.
		/// </summary>
		public void RejectQueueMessages()
		{
			Log.Debug("Reject queue messages");
			lock (_queue)
			{
				while (_queue.TotalSize > 0)
				{
					try
					{
						RejectMessage(_queue.Poll());
					}
					catch (Exception)
					{
						// ignore
					}
					finally
					{
						_queue.Commit();
					}
				}
			}
		}

		public void RejectFirstQueueMessage()
		{
			Log.Debug("Reject first queue message");
			lock (_queue)
			{
				if (_queue.TotalSize > 0)
				{
					try
					{
						RejectMessage(_queue.Poll());
					}
					catch (Exception)
					{
						// ignore
					}
					finally
					{
						_queue.Commit();
					}
				}
			}
		}

		private void RejectMessage(FixMessageWithType messageWithType)
		{
			if (messageWithType.FixMessage != null && messageWithType.IsApplicationLevelMessage())
			{
				// process only application level messages
				var message = messageWithType.PrepareMsgForReject();
				if (Log.IsWarnEnabled)
				{
					Log.Warn("Reject message: " + message);
				}

				_fixSession.RejectMessageListener.OnRejectMessage(message);
			}
			else
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Skip session message: " + messageWithType.ToString());
				}
			}
		}


		public bool SendOutOfTurn(string msgType, FixMessage content)
		{
			lock (_queue)
			{
				IsQueueTooBigForSessionMessages();
				var added = _queue.AddOutOfTurn(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(content, msgType));
				if (added)
				{
					_queue.NotifyAllSession();
				}
				else
				{
					throw new MessageNotSentException("Message wasn't added to outgoing queue");
				}

				//message was queued
				return false;
			}
		}

		private readonly FixMessageWithType _tmpMessageWithType = new FixMessageWithType();
		private readonly MsgBuf _tmpMsgBuf = new MsgBuf();

		public int Send(string msgType, FixMessage content)
		{
			return Send(msgType, content, FixSessionSendingType.DefaultSendingOption);
		}


		/// <summary>
		/// Pool the message to queue.
		/// </summary>
		/// <param name="msgType"> the message type </param>
		/// <param name="content"> the message content </param>
		public int Send(string msgType, FixMessage content, FixSessionSendingType optionMask)
		{
			return Send(msgType, content, null, optionMask);
		}


		private int Send(string msgType, FixMessage content, ChangesType? changesType, FixSessionSendingType optionMask)
		{

			try
			{
				bool isUsingOriginalMsgBuffer;

				lock (_queue)
				{

					var sync = (optionMask & FixSessionSendingType.SendSync) != 0;
					var async = (optionMask & FixSessionSendingType.SendAsync) != 0;

					// TBD! We can send messages only for connected session. Make check for session state lighter
					if (optionMask != 0 && (!async || sync) && !HasAnyWorkQueued() && !_queue.OutOfTurnOnlyMode && SessionState.IsConnected(_fixSession.SessionState) && !_shutdownFlag)
					{
						var allowBufferWriting = _msgBufferSemaphore.Wait(0);
						if (allowBufferWriting)
						{
							try
							{
								_tmpMessageWithType.FixMessage = content;
								_tmpMessageWithType.MessageType = msgType;
								_tmpMessageWithType.ChangesType = changesType;
								_tmpMsgBuf.Buffer = null;

								isUsingOriginalMsgBuffer = FillBuffer(_tmpMessageWithType, _tmpMsgBuf);
							}
							catch (IOException)
							{
								AddToQueue(msgType, content, changesType);
								throw;
							}
							finally
							{
								_msgBufferSemaphore.Release();
							}
						}
						else
						{
							//buffer is locked by other process - add messages to queue
							AddToQueue(msgType, content, changesType);
							//message was queued
							return _queue.TotalSize;
						}
					}
					else
					{
						AddToQueue(msgType, content, changesType);
						//message was queued
						return _queue.TotalSize;
					}
				}

				try
				{
					try
					{
						_msgBufferSemaphore.Wait();
						//TODO: and wy we are passing byte[] instead of whole buffer
						SendMessages(1, _tmpMsgBuf.Buffer);

					}
					catch (ThreadInterruptedException e)
					{
						if (Log.IsTraceEnabled)
						{
							Log.Debug("Sync send ERROR: " + e.ToString(), e);
						}
						else if (Log.IsDebugEnabled)
						{
							Log.Debug("Sync send ERROR: " + e.ToString());
						}
					}
					finally
					{
						_msgBufferSemaphore.Release();
						//sending with queue may be postponed by semaphore
						lock (_queue)
						{
							if (!_queue.IsEmpty)
							{
								Monitor.PulseAll(_queue);
							}
						}
					}
				}
				catch (IOException ex)
				{
					if (Log.IsTraceEnabled)
					{
						Log.Debug("Sync send ERROR: " + ex.ToString(), ex);
					}
					else if (Log.IsDebugEnabled)
					{
						Log.Debug("Sync send ERROR: " + ex.ToString());
					}
					lock (_queue)
					{
						//there is problem with transport. Let's start pumper thread to do all "dirty" work
						_queue.OutOfTurnOnlyMode = true;
						Monitor.PulseAll(_queue);
					}
					//throw ex;
				}
				if (isUsingOriginalMsgBuffer)
				{
					//can try to release the message after send is done
					ReleaseMessageIfNeeded(content);
				}

				//message was sent directly only if queue is empty
				return 0;

			}
			catch (IOException ex)
			{
				// solve deadlock:
				// - TestRequestTask lock session first and then lock queue (during sending logon)
				// - here we lock queue first and then we locked session during shutdown.
				// For a moment here we will release queue lock here to avoid deadlock
				ReportErrorAndShutdown(ex);
			}
			catch (Exception ex)
			{
				// close session if there is a problem with sending data
				ReportErrorAndShutdown(ex);
				throw;
			}

			return _queue.TotalSize;
		}

		private void AddToQueue(string msgType, FixMessage content, ChangesType? changesType)
		{
			IsQueueTooBig();
			//TODO - create pool for this
			var messageWithType = changesType == null ? FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(content, msgType) : FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(content, changesType);
			var added = _queue.Add(messageWithType);
			if (added)
			{
				_queue.NotifyAllApplication();
			}
			else
			{
				throw new MessageNotSentException("Message wasn't added to outgoing queue");
			}
		}

		public bool FillBuffer(FixMessageWithType messageWithType, MsgBuf buf)
		{
			_messageBufferWorker.IncrementCounter();

			_messageBufferWorker.MarkStartOffset();
			var isUsingOriginalMsgBuffer = PrepareToSend(buf, messageWithType, _messageBufferWorker.Buffer);
			if (isUsingOriginalMsgBuffer)
			{
				if (buf.Buffer != null)
				{
					_messageBufferWorker.Buffer.Add(buf.Buffer, buf.Offset, buf.Length);
					_messageBufferWorker.MarkEndOffset();
				}
			}
			else
			{
				ReleaseMessageIfNeeded(messageWithType);
				_messageBufferWorker.MarkEndOffset();
			}
			return isUsingOriginalMsgBuffer;
		}


		/// <summary>
		/// Pool the message to queue.
		/// </summary>
		/// <param name="content">     the message content </param>
		/// <param name="changesType"> the change type </param>
		public int Send(FixMessage content, ChangesType? changesType)
		{
			return Send(null, content, changesType, FixSessionSendingType.DefaultSendingOption);
		}

		public int Send(FixMessage content, ChangesType? changesType, FixSessionSendingType optionMask)
		{
			return Send(null, content, changesType, optionMask);
		}

		/// <summary>
		/// Checks the queue size. The method is blocking.
		/// </summary>
		private void IsQueueTooBig()
		{
			if (_queueThresholdSize != 0 && _queue.Size > _queueThresholdSize)
			{
				SafeWaitMilis(_waitOnSendMillis);
			}
		}

		/// <summary>
		/// Checks the queue size. The method is blocking.
		/// </summary>
		private void IsQueueTooBigForSessionMessages()
		{
			if (_queueThresholdSize != 0 && _queue.Size > _queueThresholdSize + 1)
			{
				SafeWaitMilis(_waitOnSendMillis);
			}
		}

		/// <summary>
		/// Gets outgoing message storage.
		/// </summary>
		public IMessageStorage GetOutgoingMessageStorage()
		{
			return _outgoingLog;
		}

		/// <summary>
		/// Sets gracefulShutdown flag.
		/// </summary>
		/// <value> the graceful shutdown flag </value>
		public bool GracefulShutdown { get; set; }

		internal sealed class MessageBufferWorker
		{
			private readonly int[] _messageStart;
			private readonly int[] _messageEnd;

			public ByteBuffer Buffer { get; }

			public int Offset => Buffer.Offset;

			public int Counter { get; private set; }

			public int FullLength => _messageEnd[Counter] - _messageStart[0];

			public MessageBufferWorker(int maxMessagesInBatch)
			{
				Buffer = new ByteBuffer(100 * maxMessagesInBatch);
				_messageStart = new int[maxMessagesInBatch + 2];
				_messageEnd = new int[maxMessagesInBatch + 2];
				Counter = -1;
			}

			public void ResetBuffer()
			{
				Buffer.ResetBuffer();
				Counter = -1;
			}
			
			public void MarkStartOffset()
			{
				_messageStart[Counter] = Buffer.Offset;
			}

			public void MarkEndOffset()
			{
				_messageEnd[Counter] = Buffer.Offset;
			}

			public void IncrementCounter()
			{
				Counter++;
			}

			public int GetStartOffset(int position)
			{
				return _messageStart[position];
			}

			public int GetLength(int position)
			{
				return _messageEnd[position] - _messageStart[position];
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				_msgBufferSemaphore.Dispose();
			}
		}
	}
}