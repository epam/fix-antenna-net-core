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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Acceptor
{
	/// <summary>
	/// Util class for handle first message during logon process.
	/// </summary>
	internal class LogonMessageWaiter
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(LogonMessageWaiter));

		private readonly IFixTransport _fixTransport;
		private readonly FixMessage _logon;
		private readonly int _timeToWait;
		private readonly MsgBuf _message;
		private readonly FixMessage _tempMessage;
		private readonly Thread _workerThread;

		public LogonMessageWaiter(IFixTransport fixTransport, int timeToWait, FixMessage logon)
		{
			_workerThread = new Thread(Run) { Name = "LogonWaiter", IsBackground = true };
			_fixTransport = fixTransport;
			_timeToWait = timeToWait;
			_logon = logon;
			_message = new MsgBuf();
			_tempMessage = new FixMessage();
			_message.FixMessage = _tempMessage;
		}

		private void Run()
		{
			try
			{
				try
				{
					_tempMessage.Clear();
					_fixTransport.ReadMessage(_message);
					// TBD we need add all or set and replace
					_logon.AddAll(_tempMessage);
				}
				catch (IOException)
				{
					CloseTransport();
				}
			}
			catch (ThreadInterruptedException)
			{
				// It is expected.
				// Another thread interrupted this one.
			}
		}

		private void CloseTransport()
		{
			try
			{
				Log.Trace("Trying to close transport...");
				_fixTransport.Close();
			}
			catch (IOException e)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Warn($"Ignoring exception while closing transport: {e}", e);
				}
				else
				{
					Log.Warn($"Ignoring exception while closing transport: {e.Message}");
				}
			}
		}

		/// <summary>
		/// Method starts the thread and wait for first message during specified time.
		/// </summary>
		/// <returns> true if logon received </returns>
		/// <exception cref="ThreadInterruptedException"> if thread is interrupted </exception>
		public bool IsLogonReceived()
		{
			_workerThread.Start();
			if (Log.IsDebugEnabled)
			{
				Log.Debug("LogonWaiter thread started");
			}
			_workerThread.Join(_timeToWait);
			if (_workerThread.IsAlive)
			{
				if (Log.IsWarnEnabled)
				{
					Log.Warn(
						$"Login hasn't been received from {_fixTransport.RemoteHost} after specified timeout ({_timeToWait} ms). Connection is going to be terminated.");
				}
				_workerThread.Interrupt();
				CloseTransport();
				return false;
			}
			if (Log.IsDebugEnabled)
			{
				Log.Debug("First message has been received. LogonWaiter thread terminated.");
			}
			return true;
		}

		public long ReadMessageTimeTicks => _message.MessageReadTimeInTicks;
	}
}