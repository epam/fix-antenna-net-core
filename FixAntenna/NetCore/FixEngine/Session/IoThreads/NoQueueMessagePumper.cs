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
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads.Bean;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Storage.Queue;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads
{
	internal class NoQueueMessagePumper : IMessagePumper
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(NoQueueMessagePumper));

		private IExtendedFixSession _fixSession;
		private IFixMessageFactory _fixMessageFactory;
		private IMessageStorage _outgoingLog;
		private IFixTransport _transport;
		private SessionParameters _sessionParameters;
		private SerializationContext _context;
		private ISessionSequenceManager _sequenceManager;
		private ByteBuffer _messageBuffer;
		private readonly object _lock = new object();
		private readonly IQueue<FixMessageWithType> _queue;
		private bool _statisticEnabled;
		private MessageStatistic _messageStatistic;
		private long _messageProcessedTimestamp;

		private volatile bool _started;

		public Thread WorkerThread => Thread.CurrentThread;

		public NoQueueMessagePumper(IExtendedFixSession extendedFixSession, IQueue<FixMessageWithType> queue, IMessageStorage @out, IFixMessageFactory messageFactory, IFixTransport transport, ISessionSequenceManager sequenceManager)
		{
			_fixSession = extendedFixSession;
			_queue = queue;
			_sessionParameters = extendedFixSession.Parameters;
			_messageBuffer = new ByteBuffer();

			_outgoingLog = @out;
			_fixMessageFactory = messageFactory;
			_transport = transport;
			_sequenceManager = sequenceManager;
		}

		public long Init()
		{
			var configurationAdapter = new ConfigurationAdapter(_sessionParameters.Configuration);
			_statisticEnabled = configurationAdapter.IsMessageStatisticEnabled;
			if (_statisticEnabled)
			{
				_messageStatistic = new MessageStatistic();
			}

			_context = new SerializationContext(_fixMessageFactory);
			return _outgoingLog.Initialize();
		}

		public bool GracefulShutdown { get; set; }

		public void RejectQueueMessages()
		{
			Log.Debug("Reject queue messages");
			try
			{
				Lock();
				while (_queue.TotalSize > 0)
				{
					try
					{
						RejectMessage(_queue.Poll());
					}
					catch (Exception e)
					{
						Log.Error("Failed to reject message in queue", e);
					}
					finally
					{
						_queue.Commit();
					}
				}
			}
			finally
			{
				Unlock();
			}
		}

		public void RejectFirstQueueMessage()
		{
			Log.Debug("Reject first queue message");
			try
			{
				Lock();
				if (_queue.TotalSize > 0)
				{
					try
					{
						RejectMessage(_queue.Poll());
					}
					catch (Exception e)
					{
						Log.Error("Failed to reject message in queue", e);
					}
					finally
					{
						_queue.Commit();
					}
				}
			}
			finally
			{
				Unlock();
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
		public bool IsStatisticEnabled => _statisticEnabled;

		public MessageStatistic Statistic => _messageStatistic;

		public long MessageProcessedTimestamp => Interlocked.Read(ref _messageProcessedTimestamp);

		public bool SendOutOfTurn(string msgType, FixMessage content)
		{
			try
			{
				Lock();
				return SerializeAndSendOrPutInQueueIfNotStarted(msgType, content);
			}
			finally
			{
				Unlock();
			}
		}

		public void Start()
		{
			try
			{
				Lock();
				while (!_queue.IsEmpty)
				{
					var fieldListWithType = _queue.Poll();
					SerializeAndSend(fieldListWithType.MessageType, fieldListWithType.FixMessage, null, false);
					_queue.Commit();
				}

				_started = true;
				UpdateMessageProcessedTimestamp();
			}
			finally
			{
				Unlock();
			}
		}

		public void Shutdown()
		{
			try
			{
				Lock();
				_started = false;
				CloseOutgoingLog();
				Log.Debug("Pumper stopped");
			}
			finally
			{
				Unlock();
			}
		}

		private void CloseOutgoingLog()
		{
			try
			{
				_outgoingLog.Dispose();
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

		public int Send(string type, FixMessage content, FixSessionSendingType optionMask)
		{
			try
			{
				Lock();
				SendOrWait(type, content, null);
				return 0;
			}
			finally
			{
				Unlock();
			}
		}

		public int Send(FixMessage content, ChangesType? allowedChangesType)
		{
			try
			{
				Lock();
				SendOrWait(null, content, allowedChangesType);
				return 0;
			}
			finally
			{
				Unlock();
			}
		}

		public int Send(FixMessage content, ChangesType? allowedChangesType, FixSessionSendingType optionMask)
		{
			return Send(content, allowedChangesType);
		}

		public int Send(string type, FixMessage message)
		{
			Send(type, message, FixSessionSendingType.DefaultSendingOption);
			return 0;
		}

		public void SendMessages(int messageCount)
		{
			throw new InvalidOperationException("Should not be used in the context");
		}

		private bool SendOrWait(string msgType, FixMessage content, ChangesType? changesType)
		{
			try
			{
				while (!_queue.IsAllEmpty)
				{
					Monitor.Wait(_lock);
				}

				while (_started && _queue.OutOfTurnOnlyMode)
				{
					Monitor.Wait(_lock, 1);
				}
			}
			catch (ThreadInterruptedException)
			{
				Log.Warn("Thread was interrupted while waiting for the end of initialization.");
			}

			return SerializeAndSend(msgType, content, changesType, true);
		}

		private bool SerializeAndSendOrPutInQueueIfNotStarted(string msgType, FixMessage content)
		{
			if (_started)
			{
				return SerializeAndSend(msgType, content, null, true);
			}

			return PutInQueue(msgType, content);
		}

		private bool SerializeAndSend(string msgType, FixMessage content, ChangesType? changesType, bool checkState)
		{
			if (checkState && !_started)
			{
				throw new InvalidOperationException("Pumper is not started, it cannot send any messages in this state.");
			}
			try
			{
				var offset = _messageBuffer.Offset;
				if (changesType != null)
				{
					_fixMessageFactory.Serialize(content, changesType, _messageBuffer, _context);
				}
				else
				{
					_fixMessageFactory.Serialize(null, msgType, content, _messageBuffer, _context);
				}

				var len = _messageBuffer.Offset - offset;
				long written = _transport.Write(_messageBuffer, offset, len);
				if (written != len)
				{
					throw new IOException("Transport sent only part of message (" + written + " vs " + len + ")");
				}

				if (msgType != null || changesType != null)
				{
					try
					{
						_sequenceManager.IncrementOutSeqNum();
					}
					catch (IOException e)
					{
						Log.Error("Failed to increment sequence", e);
						return false;
					}
				}

				UpdateMessageProcessedTimestamp();
				_outgoingLog.AppendMessage(_messageBuffer.GetByteArray(), offset, len);

				try
				{
					_fixSession.ExtendedFixSessionListener.OnMessageSent(_messageBuffer.GetByteArray(), offset, len);
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
			catch (IOException e)
			{
				Log.Error("Failed to send message: " + e.Message, e);
				ReportErrorAndShutdown(e);
				throw new MessageNotSentException("Message wasn't sent due to error", e);
			}
			finally
			{
				_messageBuffer.ResetBuffer();
			}

			return true;
		}

		private void ReportErrorAndShutdown(Exception ex)
		{
			var error = "IOError in message pumper. Some messages have not been sent: " + (_queue.TotalSize > 0 ? _queue.TotalSize : 1);
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


		private bool PutInQueue(string msgType, FixMessage content)
		{
			var type = string.IsNullOrEmpty(msgType)
				? content.MsgType
				: msgType.AsByteArray();

			if (type == null || !RawFixUtil.IsSessionLevelType(type))
			{
				throw new InvalidOperationException("Application message not allowed while connection is not initialized");
			}

			return _queue.AddOutOfTurn(FixMessageWithTypePoolFactory.GetFixMessageWithTypeFromPool(content, msgType));
		}

		private void UpdateMessageProcessedTimestamp()
		{
			Interlocked.Exchange(ref _messageProcessedTimestamp, DateTimeHelper.CurrentMilliseconds);
		}

		public virtual void Lock()
		{
			Monitor.Enter(_lock);
		}

		public virtual void Unlock()
		{
			Monitor.Pulse(_lock);
			Monitor.Exit(_lock);
		}

		public void Join()
		{
		}
	}
}