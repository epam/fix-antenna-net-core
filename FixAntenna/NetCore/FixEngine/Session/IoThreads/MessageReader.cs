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
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads.Bean;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.FixEngine.Storage;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.IoThreads
{
	/// <summary>
	/// The <c>MessageReader</c> reads message from transport.
	/// </summary>
	/// <seealso cref="IMessageStorage"></seealso>
	/// <seealso cref="IFixTransport"></seealso>
	internal sealed class MessageReader : AffinitySupportThread, IMessageReader
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(MessageReader));
		private const int Second = 1000;

		private readonly IExtendedFixSession _fixSession;
		private readonly IFixTransport _transport;
		private readonly ICompositeMessageHandlerListener _compositeListener;
		private volatile bool _gracefulShutdown;
		private volatile bool _shutdown;
		private long _messageProcessedTimestamp;
		private bool _statisticEnabled;
		private MessageStatistic _messageStatistic;
		private readonly FixMessage _mainMessageForParse = FixMessageFactory.NewInstanceFromPoolForEngineParse();
		private readonly int _shutdownTimeout;

		/// <summary>
		/// Creates <c>MessageReader</c>.
		/// </summary>
		/// <param name="messageStorage">              the message storage </param>
		/// <param name="session">           the session </param>
		/// <param name="compositeFixSessionListener"> the session listener </param>
		/// <param name="transport">                   the transport </param>
		public MessageReader(IExtendedFixSession session, IMessageStorage messageStorage,
			ICompositeMessageHandlerListener compositeFixSessionListener, IFixTransport transport)
			: base("MRThread<:" + session.Parameters.SessionId)
		{
			_fixSession = session;
			IncomingMessageStorage = messageStorage;
			_compositeListener = compositeFixSessionListener;
			_transport = transport;
			_shutdownTimeout = session.Parameters.Configuration.GetPropertyAsInt(Config.ReadingThreadShutdownTimeout);

			if (_shutdownTimeout < 0)
			{
				_shutdownTimeout = session.Parameters.HeartbeatInterval;
			}
		}

		public long Init(ConfigurationAdapter configurationAdapter)
		{
			var number = IncomingMessageStorage.Initialize();

			_statisticEnabled = configurationAdapter.IsMessageStatisticEnabled;
			if (_statisticEnabled)
			{
				_messageStatistic = new MessageStatistic();
			}

			Interlocked.Exchange(ref _messageProcessedTimestamp, DateTimeHelper.CurrentMilliseconds);
			return number;
		}

		private ISessionSequenceManager SequenceManager => ((AbstractFixSession)_fixSession).SequenceManager;

		/// <summary>
		/// Shutdown the reader.
		/// </summary>
		public override void Shutdown()
		{
			if (!WorkerThread.IsAlive)
			{
				CloseStorage();
			}

			_shutdown = true;
			try
			{
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
				catch (ThreadInterruptedException e)
				{
					Log.Debug("Problem with closing of MessageReader", e);
					// intentionally blank
				}

				CloseStorage();
				Log.Debug("Reader stopped");
			}
			catch (Exception e)
			{
				Log.Debug("Problem with closing of MessageReader", e);
			}
		}

		public long MessageProcessedTimestamp
		{
			get => Interlocked.Read(ref _messageProcessedTimestamp);
			set => Interlocked.Exchange(ref _messageProcessedTimestamp, value);
		}

		/// <returns> true is statistic is enabled </returns>
		public bool IsStatisticEnabled => _statisticEnabled;

		/// <summary>
		/// Gets statistic of processed messages.
		/// WARNING: Before the call to ensure that the statistics are included.
		/// </summary>
		/// <value> MessageStatistic </value>
		/// <exception cref="InvalidOperationException"> if <c>statisticEnabled</c> is false </exception>
		/// <seealso cref="IsStatisticEnabled()"> </seealso>
		public MessageStatistic MessageStatistic
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

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			try
			{
				_mainMessageForParse.ReleaseInstance();
			}
			catch (Exception t)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("Can't release instance: " + t.Message, t);
				}
				else
				{
					Log.Warn("Can't release instance: " + t.Message);
				}
			}
		}

		private FixVersionContainer GetFixVersionForMessage()
		{
			var fixVersion = _fixSession.Parameters.FixVersionContainer;
			if (fixVersion.FixVersion == FixVersion.Fixt11)
			{
				fixVersion = _fixSession.Parameters.AppVersionContainer;
			}

			return fixVersion;
		}

		protected override void Run()
		{
			if (Log.IsTraceEnabled)
			{
				Log.Trace("Start MRThread: " + _fixSession);
			}

			var configuration = _fixSession.Parameters.Configuration;

			ApplyAffinity(configuration.GetPropertyAsInt(Config.RecvCpuAffinity), configuration.GetPropertyAsInt(Config.CpuAffinity));
			Thread.BeginThreadAffinity();

			var buf = new MsgBuf();
			buf.FixMessage = _mainMessageForParse;
			_mainMessageForParse.SetFixVersion(GetFixVersionForMessage());

			while (!_shutdown)
			{
				try
				{
					ProcessBufferedMessages();

					_transport.ReadMessage(buf);

					_compositeListener.OnMessage(buf);

					//TBD! do timing only if required
					MessageProcessedTimestamp = DateTimeHelper.CurrentMilliseconds;
					if (_statisticEnabled)
					{
						UpdateStatistic(buf);
					}
				}
				catch (GarbledMessageException ex)
				{
					LogGarbledMessageException(ex, buf);
				}
				catch (SkipMessageException ex)
				{
					LogSkipMessageException(ex, buf);
				}
				catch (ArgumentException ex)
				{
					LogArgumentException(ex, buf);
				}
				catch (SequenceToLowException ex)
				{
					LogSequenceToLowException(ex, buf);
				}
				catch (SystemHandlerException ex)
				{
					LogSystemHandlerException(ex);
				}
				catch (Exception ex)
				{
					if (!GracefulShutdown)
					{
						ReportErrorAndShutdown(ex);
					}

					break;
				}
				finally
				{
					_mainMessageForParse.Clear();
				}
			}

			CloseStorage();
			if (Log.IsTraceEnabled)
			{
				Log.Trace("Stop MRThread: " + _fixSession);
			}

			Thread.EndThreadAffinity();
		}

		private void LogGarbledMessageException(GarbledMessageException ex, MsgBuf buf)
		{
			if (ex.IsCritical())
			{
				// garbled message, ignore and read next one
				_fixSession.ErrorHandler.OnError("Garbled message detected and will be ignored: " + buf, ex);
			}
			else
			{
				_fixSession.ErrorHandler.OnWarn("Garbled message detected and will be ignored: " + buf, ex);
			}
		}

		private static void LogSkipMessageException(SkipMessageException ex, MsgBuf buf)
		{
			if (ex.IsLogToLoggingSystem())
			{
				if (Log.IsDebugEnabled)
				{
					Log.Info("Skip message: " + buf + (ex.Message == null ? "" : ". Reason: " + ex.Message), ex);
				}
				else
				{
					Log.Info("Skip message: " + buf + (ex.Message == null ? "" : ". Reason: " + ex.Message));
				}
			}
		}

		private static void LogArgumentException(ArgumentException ex, MsgBuf buf)
		{
			if (Log.IsDebugEnabled)
			{
				Log.Warn("Invalid message received: " + buf, ex);
			}
			else
			{
				Log.Warn("Invalid message received: " + buf + ". Reason: " + ex.Message);
			}
		}

		private void LogSequenceToLowException(SequenceToLowException ex, MsgBuf buf)
		{
			_fixSession.ErrorHandler.OnError("Invalid message received: " + buf, ex);
		}

		private static void LogSystemHandlerException(SystemHandlerException ex)
		{
			if (Log.IsDebugEnabled)
			{
				Log.Warn("System problem detected: " + ex, ex);
			}
			else
			{
				Log.Warn("System problem detected: " + ex);
			}
		}

		/// <summary>
		/// Handles messages buffered during processing filling the gap
		/// </summary>
		/// <exception cref="Exception"> </exception>
		private void ProcessBufferedMessages()
		{
			var sequenceResendManager = SequenceManager.SeqResendManager;
			if (!sequenceResendManager.IsRrRangeActive && !sequenceResendManager.IsBufferEmpty)
			{
				try
				{
					sequenceResendManager.IsMessageProcessingFromBufferStarted = true;
					do
					{
						var bufferedMessage = sequenceResendManager.TakeMessageFromBuffer();
						if (null != bufferedMessage && !IsIgnoredBufferedMessage(bufferedMessage))
						{
							_compositeListener.OnMessage(new MsgBuf(bufferedMessage.AsByteArray()));
						}

					} while (!sequenceResendManager.IsBufferEmpty);
				}
				finally
				{
					sequenceResendManager.IsMessageProcessingFromBufferStarted = false;
				}
			}
		}

		private bool IsIgnoredBufferedMessage(FixMessage bufferedMessage)
		{
			var expectedSeqNum = SequenceManager.GetExpectedIncomingSeqNumber();
			var msgSeqNum = bufferedMessage.MsgSeqNumber;
			return msgSeqNum < expectedSeqNum;
		}

		private void CloseStorage()
		{
			try
			{
				IncomingMessageStorage.Dispose();
			}
			catch (IOException ex)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn("Incoming log file cannot be closed", ex);
				}
				else
				{
					Log.Warn("Incoming log file cannot be closed. " + ex.Message);
				}
			}
		}

		private void UpdateStatistic(MsgBuf buf)
		{
			_messageStatistic.AddMessagesProcessed();
			_messageStatistic.AddBytesProcessed(buf.Length);
		}

		private void ReportErrorAndShutdown(Exception e)
		{
			var error = "Abrupt session " + _fixSession + " termination.";
			Log.Warn(error);
			try
			{
				_transport.Close();
			}
			catch (Exception ex)
			{
				Log.Error("Error to close transport", ex);
			}

			_fixSession.ErrorHandler.OnError(error, e);
			_fixSession.Shutdown(DisconnectReason.BrokenConnection, false);
		}

		/// <summary>
		/// Gets incoming message storage.
		/// </summary>
		public IMessageStorage IncomingMessageStorage { get; }

		/// <summary>
		/// Sets graceful shutdown flag.
		/// </summary>
		/// <value> the graceful shutdown flag </value>
		public bool GracefulShutdown
		{
			get => _gracefulShutdown;
			set => _gracefulShutdown = value;
		}
	}
}