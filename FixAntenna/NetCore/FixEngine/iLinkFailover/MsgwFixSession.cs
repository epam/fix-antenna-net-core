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
using System.Threading.Tasks;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.FixEngine.Session;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.iLinkFailover
{
	internal class MsgwFixSession : AutoreconnectFixSession
	{
		private TransportWrapper _transportWrapper;
		private volatile bool _switching;

		public MsgwFixSession(IFixMessageFactory factory, SessionParameters sessionParameters, HandlerChain fixSessionListener)
			: base(factory, sessionParameters, fixSessionListener)
		{
			_transportWrapper = new TransportWrapper(this);
		}

		public override void Connect()
		{
			ResetToUndefinedFTI();
			base.Connect();
		}

		public virtual void SwitchFromBackup(MsgwFixSession backupSession, FixMessage message)
		{
			Log.Debug("switching to new primary session(" + Parameters.SessionId + ")...");
			// to be thread safe we switch transport in this thread
			var cloneMsg = (FixMessage)message.Clone();
			var exPrimaryTransport = GetTransport();
			var newPrimaryTransport = backupSession.GetTransport();
			backupSession.SetTransport(exPrimaryTransport);

			// this is thread of backup reader session. mark backup session as shutdown to be sure this thread not read new messages
			backupSession.MarkShutdownAsGraceful();
			backupSession.Shutdown(DisconnectReason.ClosedByCounterparty, true);

			// release this session's thread. and start new thread to switching to new Primary
			var thread = new Thread(() =>
			{
				try
				{
					_switching = true;
					MarkShutdownAsGraceful();
					Shutdown(DisconnectReason.ClosedByCounterparty, true);

					SetTransport(newPrimaryTransport, cloneMsg);
					Connect();
					SessionState = SessionState.Connected;
				}
				catch (IOException e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn(e.Message, e);
					}
					else
					{
						Log.Warn(e.Message);
					}
				}
				finally
				{
					_switching = false;
				}

				Log.Debug("switched");
			}) { Name = Parameters.SessionId + "-switcher" };

			thread.Start();
		}

		public virtual void SetTransportFrom(MsgwFixSession backupSession)
		{
			Log.Info("Changing transport of primary session " + Parameters.SessionId + " to: " + backupSession.GetTransport().RemoteHost);
			var exPrimaryTransport = GetTransport();
			var newPrimaryTransport = backupSession.GetTransport();
			backupSession.SetTransport(exPrimaryTransport);

			var thread = new Thread(() =>
			{
				try
				{
					Log.Debug("shutdown primary session");
					MarkShutdownAsGraceful();
					Shutdown(DisconnectReason.ClosedByCounterparty, true);

					var seqNumOut = RuntimeState.OutSeqNum - 1;
					Log.Debug("revert SeqNum to " + seqNumOut);
					SequenceManager.ApplyOutSeqnum(seqNumOut);

					Log.Debug("connect primary session with new transport");
					SetTransport(newPrimaryTransport);
					Connect();
					Log.Debug("primary session has been connected");
				}
				catch (IOException e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Exception while switching transport or connection primary session: " + e.Message, e);
					}
					else
					{
						Log.Warn("Exception while switching transport or connection primary session: " + e.Message);
					}
				}
			}) { Name = Parameters.SessionId + "-starter" };
			thread.Start();
		}

		private void ResetToUndefinedFTI()
		{
			var sessionParameters = Parameters;
			var senderCompId = sessionParameters.SenderCompId;
			if (senderCompId.EndsWith(Fti.Backup.GetValue(), StringComparison.Ordinal)
					|| senderCompId.EndsWith(Fti.Primary.GetValue(), StringComparison.Ordinal))
			{
				sessionParameters.SenderCompId = senderCompId.Substring(0, senderCompId.Length - 1) + Fti.Undefined.GetValue();
				MessageFactory.SetSessionParameters(sessionParameters);
			}
		}

		public virtual bool IsSwitching => _switching;

		protected override IFixTransport GetTransport(string host, int port, SessionParameters sessionParameters)
		{
			_transportWrapper.Transport = base.GetTransport(host, port, sessionParameters);
			return _transportWrapper;
		}

		public virtual IFixTransport GetTransport()
		{
			InitTransport();
			return _transportWrapper.Transport;
		}

		public override void PrepareForConnect()
		{
			if (IsSwitching)
			{
				// do not send logon msg. just init
				Init();
			}
			else
			{
				base.PrepareForConnect();
			}
		}

		public virtual void SetTransport(IFixTransport transport)
		{
			SetTransport(transport, null);
		}

		public virtual void SetTransport(IFixTransport transport, FixMessage nextMessage)
		{
			_transportWrapper.NextMessage = nextMessage;
			_transportWrapper.Transport = transport;
		}

		internal class TransportWrapper : IFixTransport, IOutgoingFixTransport
		{
			private readonly MsgwFixSession _session;

			public TransportWrapper(MsgwFixSession session)
			{
				_session = session;
			}

			internal FixMessage NextMessage { get; set; }

			public virtual IFixTransport Transport { get; set; }

			public virtual bool IsBlockingSocket => Transport.IsBlockingSocket;

			public virtual void ReadMessage(MsgBuf buf)
			{
				try
				{
					if (NextMessage == null)
					{
						Transport.ReadMessage(buf);
					}
					else
					{
						SetMessageToBuffer(buf);
					}
				}
				catch (IOException)
				{
					if (NextMessage == null)
					{
						throw;
					}
					else
					{
						// ignore Exception
						SetMessageToBuffer(buf);
					}
				}
			}

			public virtual void SetMessageToBuffer(MsgBuf buf)
			{
				var rawMsg = NextMessage.AsByteArray();
				buf.Buffer = rawMsg;
				buf.Length = rawMsg.Length;
				buf.Offset = 0;
				buf.FixMessage.Add(NextMessage);
				NextMessage = null;
			}

			public virtual void Write(byte[] message)
			{
				Transport.Write(message);
			}

			public virtual int Write(byte[] message, int offset, int length)
			{
				return Transport.Write(message, offset, length);
			}

			public virtual int Write(ByteBuffer buf, int offset, int length)
			{
				return Transport.Write(buf, offset, length);
			}

			public virtual void WaitUntilReadyToWrite()
			{
				Transport.WaitUntilReadyToWrite();
			}

			public virtual void Close()
			{
				Transport.Close();
			}

			public int OptimalBufferSize => Transport.OptimalBufferSize;

			public string RemoteHost => Transport.RemoteHost;

			public virtual void Open()
			{
				if (!_session.IsSwitching)
				{
					((IOutgoingFixTransport)Transport).Open();
				}
				else
				{
					_session.Log.Debug("ignore the opening of transport - switching in progress");
				}
			}

			public async Task OpenAsync()
			{
				if (!_session.IsSwitching)
				{
					await ((IOutgoingFixTransport)Transport).OpenAsync().ConfigureAwait(false);
				}
				else
				{
					_session.Log.Debug("ignore the opening of transport - switching in progress");
				}
			}
		}
	}
}