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
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.FixEngine.Session.Util;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.SpecialTags;

namespace Epam.FixAntenna.NetCore.FixEngine.Transport
{
	/// <summary>
	/// The abstract fix transport implementation,
	/// provides the base functionality for subclasses.
	/// </summary>
	internal abstract class AbstractFixTransport : IFixTransport
	{
		protected internal ILog Log;
		protected internal ConfigurationAdapter ConfigAdapter;
		
		private bool _debugEnabled;

		private const int MaxMessageSize = 1024 * 1024;
		private const int OptimalBufferLength = 32 * 1024;

		private IMessageChopper _fixMessageStream;
		protected internal ITransport Transport;

		private string _inLogString;
		private string _outLogString;
		private string _remoteHost;

		private string InLogString => _inLogString ?? (_inLogString = InitLogString("<<"));
		private string OutLogString => _outLogString ?? (_outLogString = InitLogString(">>"));

		private string InitLogString(string direction)
		{
			var showAddress = Config.GlobalConfiguration.GetPropertyAsBoolean(Config.WriteSocketAddressToLog);
			return showAddress ? "[" + RemoteHost + "]" + direction : direction;
		}

		/// <summary>
		/// Creates the <c>AbstractFIXTransport</c>.
		/// </summary>
		protected AbstractFixTransport(ITransport transport, Config configuration)
		{
			Log = LogFactory.GetLog(GetType());
			_debugEnabled = Log.IsDebugEnabled;

			ConfigAdapter = new ConfigurationAdapter(configuration);
			InitTransport(transport);
		}

		/// <summary>
		/// Creates the <c>AbstractFIXTransport</c>.
		/// </summary>
		protected AbstractFixTransport(ITransport transport, SessionParameters parameters)
			: this(transport, parameters.Configuration) {}


		private void InitTransport(ITransport transport)
		{
			Transport = transport ?? throw new ArgumentNullException(nameof(transport));

			var maxMessageSize = (int)ConfigAdapter.MaxMessageSize;
			var validateCheckSum = ConfigAdapter.ValidateCheckSum;
			var markInMessageTime = ConfigAdapter.MarkInMessageTime;

			if (maxMessageSize < 0)
			{
				maxMessageSize = MaxMessageSize;
				if (Log.IsWarnEnabled)
				{
					Log.Warn("Parameter \"" + Config.MaxMessageSize + "\" must be integer and not negative");
				}
			}
			if (ConfigAdapter.ValidateGarbledMessage)
			{
				_fixMessageStream = new FixMessageChopper(transport, maxMessageSize, OptimalBufferLength, validateCheckSum, markInMessageTime);
			}
			else
			{
				_fixMessageStream = new NewMessageChopper(transport, maxMessageSize, OptimalBufferLength, markInMessageTime);
			}

			_fixMessageStream.RawTags = RawFixUtil.CreateRawTags(ConfigAdapter.RawTags);
		}

		/// <inheritdoc />
		public virtual void ReadMessage(MsgBuf buf)
		{
			for (; ;)
			{
				_fixMessageStream.ReadMessage(buf);

				if (_debugEnabled)
				{
					Log.Debug(InLogString + FixMessagePrintableFormatter.ToPrintableString(buf.ToMaskedString()));
				}

				if (_fixMessageStream.IsMessageGarbled)
				{
					var message = buf.FixMessage;
					message?.Clear();
					PrintWarnGarbledMessageDetected(buf);
				}
				else
				{
					break;
				}
			}
		}

		private void PrintWarnGarbledMessageDetected(MsgBuf buf)
		{
			if (Log.IsWarnEnabled)
			{
				Log.Warn(_fixMessageStream.Error.Format(buf, _fixMessageStream.ErrorPosition));
			}
		}

		/// <inheritdoc />
		public virtual void Write(byte[] message)
		{
			Write(message, 0, message.Length);
		}

		/// <inheritdoc />
		public virtual int Write(byte[] message, int offset, int length)
		{
			if (_debugEnabled)
			{
				Log.Debug(OutLogString + FixMessagePrintableFormatter.ToPrintableString(SpecialFixUtil.GetMaskedString(message, offset, length, null, null)));
			}

			return Transport.Write(message, offset, length);
		}

		/// <inheritdoc />
		public virtual int Write(Common.Utils.ByteBuffer message, int offset, int length)
		{
			if (_debugEnabled)
			{
				Log.Debug(OutLogString + FixMessagePrintableFormatter.ToPrintableString(SpecialFixUtil.GetMaskedString(message.GetByteArray(), offset, length, null, null)));
			}

			return Transport.Write(message, offset, length);
		}

		/// <inheritdoc />
		public virtual void WaitUntilReadyToWrite()
		{
			Transport.WaitUntilReadyToWrite();
		}

		/// <inheritdoc />
		public virtual int OptimalBufferSize => OptimalBufferLength;

		/// <inheritdoc />
		public virtual string RemoteHost => _remoteHost ?? (_remoteHost = Transport.RemoteEndPoint?.Address?.AsString());

		/// <inheritdoc />
		public virtual void Close()
		{
			Transport.Close();
		}

		public virtual long MessageReadTimeInTicks => _fixMessageStream.MessageReadTimeInTicks;

		/// <inheritdoc />
		public virtual bool IsBlockingSocket => Transport.IsBlockingSocket;

		protected virtual void Reset()
		{
			_fixMessageStream.Reset();
		}
	}
}