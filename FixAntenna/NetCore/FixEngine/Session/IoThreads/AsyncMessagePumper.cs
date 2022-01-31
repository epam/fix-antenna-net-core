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

using Epam.FixAntenna.Constants.Fixt11;
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
using Epam.FixAntenna.NetCore.Message.SpecialTags;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads
{
	internal class AsyncMessagePumper : AffinitySupportThread, IMessagePumper
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(AsyncMessagePumper));
		public static readonly bool TraceEnabled = Log.IsTraceEnabled;

		private const int OneSecondInMs = 1000;

		private int _hbtSeconds;
		private int _shutdownTimeout;
		private int _waitOnSendMillis = OneSecondInMs;
		private IExtendedFixSession _fixSession;
		private readonly IQueue<FixMessageWithType> _queue;
		private IFixMessageFactory _fixMessageFactory;
		private SessionParameters _sessionParameters;
		private ConfigurationAdapter _configurationAdapter;
		private FixSessionRuntimeState _runtimeState;
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

		private ByteBuffer _messageBuffer;
		private int[] _messageStart;
		private int[] _messageEnd;

		internal bool IsTransportBlockingSend;

		private int _dataChunkStart;
		private int _dataChunkEnd;

		protected RawFixUtil.IRawTags _rawTags;
		protected IMaskedTags _maskedTags;

		/// <summary>
		/// Creates the <c>AsyncMessagePumper</c>.
		/// </summary>
		/// <param name="queue">          the output queue </param>
		/// <param name="messageFactory"> the output message storage </param>
		/// <param name="transport">      the transport </param>
		public AsyncMessagePumper(IExtendedFixSession extendedFixSession, IQueue<FixMessageWithType> queue, IMessageStorage @out, IFixMessageFactory messageFactory, IFixTransport transport, ISessionSequenceManager sequenceManager) : base("MPThread>:" + extendedFixSession.Parameters.SessionId)
		{
			_fixSession = extendedFixSession;
			_sessionParameters = extendedFixSession.Parameters;
			_runtimeState = extendedFixSession.RuntimeState;

			_queue = queue;
			OutgoingMessageStorage = @out;

			_hbtSeconds = _sessionParameters.HeartbeatInterval;
			_shutdownTimeout = _sessionParameters.Configuration.GetPropertyAsInt(Config.WritingThreadShutdownTimeout);
			if (_shutdownTimeout < 0)
			{
				_waitOnSendMillis = _hbtSeconds * OneSecondInMs;
			}
			_transport = transport;
			_fixMessageFactory = messageFactory;
			_configurationAdapter = new ConfigurationAdapter(_sessionParameters.Configuration);
			_sequenceManager = sequenceManager;

			_rawTags = RawFixUtil.CreateRawTags(_sessionParameters.Configuration.GetProperty(Config.RawTags));
			_maskedTags = CustomMaskedTags.Create(_sessionParameters.Configuration.GetProperty(Config.MaskedTags));
		}

		public virtual long Init()
		{
			if (_messageBuffer != null)
			{
				//initialized already
				throw new InvalidOperationException("AsyncMessagePumper is initialized already");
			}

			_queueThresholdSize = _configurationAdapter.ThresholdSize;
			_enableMessageRejecting = _configurationAdapter.IsEnableMessageRejecting;
			_maxMessagesToSendInBatch = _configurationAdapter.MaxMessagesToSendInBatch;
			_waitOnSendMillis = _configurationAdapter.GetWaitForQueuingMessages(OneSecondInMs);
			_messageBuffer = new ByteBuffer(100 * _maxMessagesToSendInBatch);
			_messageStart = new int[_maxMessagesToSendInBatch];
			_messageEnd = new int[_maxMessagesToSendInBatch];

			_context = new SerializationContext(_fixMessageFactory);

			_statisticEnabled = _configurationAdapter.IsMessageStatisticEnabled;
			if (_statisticEnabled)
			{
				_messageStatistic = new MessageStatistic();
			}

			Interlocked.Exchange(ref _messageProcessedTimestamp, DateTimeHelper.CurrentMilliseconds);

			IsTransportBlockingSend = _transport.IsBlockingSocket;

			return OutgoingMessageStorage.Initialize();
		}

		public virtual long MessageProcessedTimestamp
		{
			get => Interlocked.Read(ref _messageProcessedTimestamp);
			protected set => Interlocked.Exchange(ref _messageProcessedTimestamp, value);
		}

		/// <returns> true is statistic is enabled </returns>
		public virtual bool IsStatisticEnabled => _statisticEnabled;

		/// <summary>
		/// Gets statistic of processed messages.
		/// WARNING: Before the call to ensure that the statistics are included.
		/// </summary>
		/// <value> MessageStatistic </value>
		/// <exception cref="InvalidOperationException"> if <c>statisticEnabled</c> is false </exception>
		/// <seealso cref="MessageReader.IsStatisticEnabled()"> </seealso>
		public virtual MessageStatistic Statistic
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

		protected virtual bool HasWorkQueued => !_queue.IsEmpty || HasDataChunkToTransfer();

		protected override void Run()
		{
			if (TraceEnabled)
			{
				Log.Trace("Start MPThread: " + _fixSession);
			}

			Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

			try
			{
				var configuration = _sessionParameters.Configuration;

				ApplyAffinity(configuration.GetPropertyAsInt(Config.SendCpuAffinity), configuration.GetPropertyAsInt(Config.CpuAffinity));
				Thread.BeginThreadAffinity();

				while (!_shutdownFlag || HasWorkQueued)
				{
					if (HasDataChunkToTransfer())
					{
						_transport.WaitUntilReadyToWrite();
						WriteToTransport(0);
						continue;
					}

					var bufferedMessageCount = 0;
					lock (_queue)
					{
						if (_queue.IsEmpty && !_shutdownFlag)
						{
							Monitor.PulseAll(_queue);
							SafeWait(_hbtSeconds);
						}

						//get not real queue size but requested count of messages to send
						var queueSize = _queue.Size;
						if (queueSize > 0)
						{
							if (TraceEnabled)
							{
								Log.Trace(_fixSession + " queue size: " + queueSize + " turnOff=" + _queue.OutOfTurnOnlyMode);
							}

							bufferedMessageCount = FillBuffer(queueSize);
						}
						else
						{
							EnqueueHeartbeatToSend();
						}
					}
					if (bufferedMessageCount > 0)
					{
						SendMessages(bufferedMessageCount);
					}
				}
			}
			catch (Exception ex)
			{
				if (!GracefulShutdown)
				{
					ReportErrorAndShutdown(ex);
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

		private void ReportErrorAndShutdown(Exception ex)
		{
			var error = "IOError in message pumper. Some messages have not been sent:" + _queue.TotalSize;
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
					OutgoingMessageStorage.Dispose();
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

		/// <summary>
		/// Sends the heartbeat message.
		/// </summary>
		private void EnqueueHeartbeatToSend()
		{
			if (IsHeartbeatRequired())
			{
				_fixSession.SendMessageOutOfTurn("0", null);
				//sendOutOfTurn("0", null);
			}
		}

		private bool IsHeartbeatRequired()
		{
			return !_shutdownFlag && _hbtSeconds != 0 && SessionState.IsConnected(_fixSession.SessionState);
		}

		private void UpdateStatistic()
		{
			_messageStatistic.AddMessagesProcessed();
			_messageStatistic.AddBytesProcessed(_messageBuffer.Offset);
		}

		public virtual int FillBuffer(int queueSize)
		{
			if (queueSize > _maxMessagesToSendInBatch)
			{
				queueSize = _maxMessagesToSendInBatch;
			}
			var messageCount = 0;
			_messageBuffer.ResetBuffer();
			while (messageCount < queueSize && _messageBuffer.Offset < _transport.OptimalBufferSize)
			{
				_messageStart[messageCount] = _messageBuffer.Offset;
				PollAndPrepareToSend(_messageBuffer);
				_messageEnd[messageCount] = _messageBuffer.Offset;
				messageCount++;
			}
			return messageCount;
		}

		public virtual void SendMessages(int messageCount)
		{
			SendMessages(messageCount, null);
		}

		public virtual bool SendMessages(int messageCount, byte[] externalBuffer)
		{
			var allWritten = WriteToTransport(messageCount, externalBuffer);

			//TODO: timing should be optional
			//if (statisticEnabled) {
			//    setMessageProcessedTimestamp(System.currentTimeMillis());
			//    updateStatistic();
			//}

			for (var i = 0; i < messageCount; i++)
			{
				try
				{
					_fixSession.ExtendedFixSessionListener.OnMessageSent(_messageBuffer.GetByteArray(), _messageStart[i], _messageEnd[i] - _messageStart[i]);
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

			return allWritten;
		}

		public virtual void ScheduleChunkToTransfer(int start, int end)
		{
			_dataChunkStart = start;
			_dataChunkEnd = end;
		}

		private void WriteToTransport(int messageCount)
		{
			WriteToTransport(messageCount, null);
		}

		private bool WriteToTransport(int messageCount, byte[] externalBuffer)
		{
			//TODO extract next block into separate function. Looks like for main thread it should be used without the rest part of code (messageCount = 0, externalBuffer = null)

			int toWrite, written;
			
			if (HasDataChunkToTransfer())
			{
				toWrite = _dataChunkEnd - _dataChunkStart;
				written = _transport.Write(_messageBuffer, _dataChunkStart, toWrite);
				_dataChunkStart += written;

				if (_statisticEnabled)
				{
					_messageStatistic.AddBytesProcessed(written);
				}

				return false;
			}

			toWrite = _messageEnd[messageCount - 1] - _messageStart[0];
			if (externalBuffer != null)
			{
				try
				{
					written = _transport.Write(externalBuffer, _messageStart[0], toWrite);
					if (written < toWrite)
					{
						_messageBuffer.Add(externalBuffer, _messageStart[0] + written, toWrite - written);
						ScheduleChunkToTransfer(_messageStart[0] + written, toWrite);
						return false;
					}
				}
				catch (IOException ex)
				{
					_messageBuffer.Add(externalBuffer, _messageStart[0], toWrite);
					ScheduleChunkToTransfer(_messageStart[0], toWrite);

					var messages = new string[1];
					messages[0] = StringHelper.NewString(externalBuffer, _messageStart[0], toWrite);
					throw new TransportMessagesNotSentException(ex, messages);
				}
			}
			else
			{
				try
				{
					written = _transport.Write(_messageBuffer, _messageStart[0], toWrite);
					if (written < toWrite)
					{
						ScheduleChunkToTransfer(_messageStart[0] + written, toWrite);
						return false;
					}
				}
				catch (IOException ex)
				{
					ScheduleChunkToTransfer(_messageStart[0], toWrite);

					var messages = new string[messageCount];
					for (var i = 0; i < messageCount; i++)
					{
						messages[i] = StringHelper.NewString(_messageBuffer.GetByteArray(), _messageStart[i], _messageEnd[i]);
					}
					throw new TransportMessagesNotSentException(ex, messages);
				}
			}

			MessageProcessedTimestamp = DateTimeHelper.CurrentMilliseconds;
			if (_statisticEnabled)
			{
				//updateStatistic();
				_messageStatistic.AddMessagesProcessed(messageCount);
				_messageStatistic.AddBytesProcessed(written);
			}

			return true;
		}

		//TODO: looks like we need 2 methods - for MsgBuf and for global buff
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
				useOrigMsgBuffer = buf?.Buffer != null;
			}
			if (type != null || messageWithType.ChangesType != null)
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
			ReleaseMessageIfNeeded(fixMessage);
			messageWithType.ReleaseInstance();
		}

		private void ReleaseMessageIfNeeded(FixMessage fixMessage)
		{
			if (fixMessage != null && fixMessage.NeedReleaseAfterSend)
			{
				fixMessage.ReleaseInstance();
			}
		}

		public virtual bool HasDataChunkToTransfer()
		{
			return (_dataChunkEnd - _dataChunkStart) > 0;
		}

		private void PollAndPrepareToSend(ByteBuffer messageBuffer)
		{
			var messageWithType = _queue.Poll();

			var usedOrigBuffer = PrepareToSend(null, messageWithType, messageBuffer);
			_queue.Commit();
			if (!usedOrigBuffer)
			{
				ReleaseMessageIfNeeded(messageWithType);
			}
		}

		private void SaveMessageInOutLog(ByteBuffer messageBuffer, int offset, int length)
		{
			OutgoingMessageStorage.AppendMessage(messageBuffer.GetByteArray(), offset, length);
		}

		private void SaveMessageInOutLog(byte[] buffer, int offset, int length)
		{
			OutgoingMessageStorage.AppendMessage(buffer, offset, length);
		}

		private void SafeWait(int seconds)
		{
			SafeWaitMillis(seconds * OneSecondInMs);
		}

		private void SafeWaitMillis(int milliseconds)
		{
			try
			{
				Monitor.Wait(_queue, TimeSpan.FromMilliseconds(milliseconds));
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
		/// If queue has the messages and if message rejecting is
		/// enabled, the messages are rejecting, <see cref="IRejectMessageListener"/>.
		/// This methods should be called from other thread
		/// </summary>
		public override void Shutdown()
		{
			if (!WorkerThread.IsAlive)
			{
				CloseOutgoingLog();
			}
			_shutdownFlag = true;
			lock (_queue)
			{
				Monitor.PulseAll(_queue);
			}
			try
			{
				if (WorkerThread.IsAlive)
				{
					if (Thread.CurrentThread == WorkerThread)
					{
						WorkerThread.Interrupt();
					}
					else
					{
						WorkerThread.Join(Math.Max(OneSecondInMs, _shutdownTimeout * OneSecondInMs));
						if (WorkerThread.IsAlive)
						{
							WorkerThread.Interrupt();
						}
					}
				}
				else
				{
					CloseOutgoingLog();
				}
				Log.Debug("Pumper stopped");
			}
			catch (ThreadInterruptedException)
			{
				WorkerThread.Interrupt();
				// intentionally blank
			}

			if (_enableMessageRejecting)
			{
				Log.Debug("Pumper reject message");
				RejectQueueMessages();
			}
		}

		/// <summary>
		/// Reject all non send message.
		/// </summary>
		public virtual void RejectQueueMessages()
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

		public virtual void RejectFirstQueueMessage()
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
				var message = PrepareMsgForReject(messageWithType);
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

		private FixMessage PrepareMsgForReject(FixMessageWithType messageWithType)
		{
			var message = messageWithType.FixMessage;
			var msgType = messageWithType.MessageType;
			if (!string.IsNullOrEmpty(msgType))
			{
				var tagIndex = message.GetTagIndex(Tags.MsgType);
				if (tagIndex != FixMessage.NotFound)
				{
					message.SetAtIndex(tagIndex, msgType);
				}
				else
				{
					message.AddTagAtIndex(0, Tags.MsgType, msgType);
				}
			}

			return message;
		}

		public virtual bool SendOutOfTurn(string msgType, FixMessage content)
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

		public virtual int Send(string msgType, FixMessage content)
		{
			return Send(msgType, content, FixSessionSendingType.DefaultSendingOption);
		}


		/// <summary>
		/// Pool the message to queue.
		/// </summary>
		/// <param name="msgType"> the message type </param>
		/// <param name="content"> the message content </param>
		public virtual int Send(string msgType, FixMessage content, FixSessionSendingType optionMask)
		{
			return Send(msgType, content, null, optionMask);
			/*
							synchronized (queue) {
									isQueueTooBig();
									//TODO - create pool for this
									FixMessageWithType messageWithType =
													FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(content, msgType);
									boolean added = queue.Add(messageWithType);
									if (added) {
											queue.NotifyAllApplication();
									} else {
											throw new MessageNotSentException("Message wasn't added to outgoing queue");
									}
									return queue.TotalSize();
							}
			*/
		}

		private int Send(string msgType, FixMessage content, ChangesType? changesType, FixSessionSendingType optionMask)
		{
			lock (_queue)
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
				return _queue.TotalSize;
			}
		}

		/// <summary>
		/// Pool the message to queue.
		/// </summary>
		/// <param name="content">     the message content </param>
		/// <param name="changesType"> the change type </param>
		public virtual int Send(FixMessage content, ChangesType? changesType)
		{
			return Send(null, content, changesType, FixSessionSendingType.DefaultSendingOption);
	/*
	        synchronized (queue) {
	            isQueueTooBig();
	            boolean added = queue.Add(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(content, changesType));
	            if (added) {
	                queue.NotifyAllApplication();
	            } else {
	                throw new MessageNotSentException("Message wasn't added to outgoing queue");
	            }
	            return queue.TotalSize();
	        }
	*/
		}

		public virtual int Send(FixMessage content, ChangesType? changesType, FixSessionSendingType optionMask)
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
				SafeWaitMillis(_waitOnSendMillis);
			}
		}

		/// <summary>
		/// Checks the queue size. The method is blocking.
		/// </summary>
		private void IsQueueTooBigForSessionMessages()
		{
			if (_queueThresholdSize != 0 && _queue.Size > (_queueThresholdSize + 1))
			{
				SafeWaitMillis(_waitOnSendMillis);
			}
		}

		/// <summary>
		/// Gets outgoing message storage.
		/// </summary>
		protected IMessageStorage OutgoingMessageStorage { get; }

		/// <summary>
		/// Sets gracefulShutdown flag.
		/// </summary>
		/// <value> the graceful shutdown flag </value>
		public virtual bool GracefulShutdown { get; set; }
	}
}