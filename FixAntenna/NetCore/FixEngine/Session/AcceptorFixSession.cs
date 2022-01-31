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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine.Acceptor;
using Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler;
using Epam.FixAntenna.NetCore.FixEngine.Transport;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal sealed class AcceptorFixSession : AbstractFixSession
	{
		private static readonly byte[] TrueValue = "Y".AsByteArray();

		public AcceptorFixSession(IFixMessageFactory messageFactory, SessionParameters sessionParameters,
			HandlerChain fixSessionListener, IFixTransport transport) : base(messageFactory, sessionParameters,
			fixSessionListener)
		{
			this.Transport = transport;
		}

		public void ReinitSession(SessionParameters inSessionParameters, IFixTransport transport)
		{
			lock (SessionLock)
			{
				if (SessionState.IsConnected(SessionState))
				{
					throw new DuplicateSessionException("Session " + Parameters.SessionId +
																							" is still alive. Duplicate connection from " + transport.RemoteHost);
				}

				//reset init flag
				ResetInitialization();

				var oldDetails = Parameters;
				CheckNewSessionParameters(oldDetails, inSessionParameters);
				UpdateSessionParameters(inSessionParameters, oldDetails);

				this.Parameters =
					inSessionParameters; //new SessionParameterProxy(inSessionParameters, new SessionParametersProxyAdaptorListener());
				this.Transport = transport;
				inSessionParameters.PrintConfiguration();
			}
		}

		public void UpdateSessionParameters(SessionParameters newParams, SessionParameters oldParams)
		{
			newParams.OutgoingLoginMessage = oldParams.OutgoingLoginMessage;
			newParams.LastSeqNumResetTimestamp = oldParams.LastSeqNumResetTimestamp;
			newParams.FixVersionContainer = oldParams.FixVersionContainer;
			newParams.AppVersionContainer = oldParams.AppVersionContainer;
			newParams.Configuration = oldParams.Configuration;
			newParams.IncomingSequenceNumber = oldParams.IncomingSequenceNumber;
			newParams.OutgoingSequenceNumber = oldParams.OutgoingSequenceNumber;
		}

		private void CheckNewSessionParameters(SessionParameters oldParams, SessionParameters newParams)
		{
			var errors = new List<string>();
			if (!oldParams.IsSimilar(newParams, errors))
			{
				throw new DuplicateSessionException("Duplicate acceptor session attempt: " + newParams.SessionId +
																						". Session details not similar to exist one: " + string.Join(Environment.NewLine, errors));
			}
		}

		/// <inheritdoc />
		public override void Connect()
		{
			lock (SessionLock)
			{
				if (SessionState.IsNotDisconnected(SessionState))
				{
					throw new InvalidOperationException("Cannot connect while:" + SessionState);
				}

				SessionState = SessionState.WaitingForLogon;
				try
				{
					PrepareForConnect();
					try
					{
						// TBD!
						if (Log.IsDebugEnabled)
						{
							Log.Debug(
								"IncomingLoginFixMessage: " + Parameters.IncomingLoginMessage.ToPrintableString());
						}

						var message = new MsgBuf(Parameters.IncomingLoginMessage.AsByteArray());
						//FIXME: it needs to transfer somehow Logon reading time for registered sessions
						message.MessageReadTimeInTicks = Parameters is ParsedSessionParameters parameters
							? parameters.LogonReadTimeTicks
							: DateTimeHelper.CurrentTicks;

						MessageHandlers.OnMessage(message);
					}
					catch (Exception e)
					{
						if (Log.IsDebugEnabled)
						{
							Log.Warn(
								"Invalid login message received:" + Parameters.IncomingLoginMessage.ToPrintableString(),
								e);
						}
						else
						{
							Log.Warn("Invalid login message received:" +
											Parameters.IncomingLoginMessage.ToPrintableString() + ". Reason: " + e.Message);
						}

						var message = Queue.Poll();

						//in case of problems Logout should be sent back to notify other side about the reason
						if (message != null && !MsgType.Logout.Equals(message.MessageType, StringComparison.Ordinal))
						{
							Pumper.RejectFirstQueueMessage();
						}
					}

					StartSession();
				}
				catch (IOException e)
				{
					Shutdown(DisconnectReason.InitConnectionProblem, true);
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Init session failed. Cause: " + e.Message, e);
					}
					else
					{
						Log.Warn("Init session failed. Cause: " + e.Message);
					}
				}
			}
		}

		public override async Task ConnectAsync()
		{
			await Task.Run(() => Connect()).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public override void Reject(string reason)
		{
			if (Log.IsDebugEnabled)
			{
				Log.Debug("Session " + Parameters.SessionId + " rejecting...");
			}

			lock (SessionLock)
			{
				if (SessionState.IsNotDisconnected(SessionState))
				{
					throw new InvalidOperationException("Cannot connect while:" + SessionState);
				}

				SessionState = SessionState.WaitingForLogon;
				try
				{
					InitSessionInternal();
					if (Log.IsDebugEnabled)
					{
						Log.Debug("Starting MessagePumper thread. Outgoing seq number:" + RuntimeState.OutSeqNum);
					}

					Pumper.Start();
					if (Log.IsDebugEnabled)
					{
						Log.Debug("Pumper started");
					}

					SetAttribute(ExtendedFixSessionAttribute.RejectSession, true);
					try
					{
						//TBD! reuse buffer
						var buff = new MsgBuf(Parameters.IncomingLoginMessage.AsByteArray());
						MessageHandlers.OnMessage(buff);
					}
					catch (Exception e)
					{
						if (Log.IsDebugEnabled)
						{
							Log.Warn("Invalid login message received:" + Parameters.IncomingLoginMessage.ToString(),
								e);
						}
						else
						{
							Log.Warn("Invalid login message received:" + Parameters.IncomingLoginMessage.ToString() +
											". Reason: " + e.Message);
						}
					}

					//close incoming message storage manually - reader thread not started
					IncomingStorage.Dispose();
					var sessionState = SessionState;
					if (sessionState != SessionState.WaitingForLogoff
							&& sessionState != SessionState.WaitingForForcedLogoff
							&& sessionState != SessionState.WaitingForForcedDisconnect
							&& sessionState != SessionState.Disconnected
							&& sessionState != SessionState.DisconnectedAbnormally
							&& sessionState != SessionState.Dead)
					{
						Disconnect(DisconnectReason.Reject, reason);
					}
				}
				catch (IOException e)
				{
					if (Log.IsDebugEnabled)
					{
						Log.Warn("Init session failed. Cause: " + e.Message, e);
					}
					else
					{
						Log.Warn("Init session failed. Cause: " + e.Message);
					}
				}
				finally
				{
					RemoveAttribute(ExtendedFixSessionAttribute.RejectSession);
				}
			}
		}

		/// <inheritdoc />
		public override bool IsResetSeqNumFlagRequiredForInitLogon
		{
			get
			{
				var logonMessage = Parameters.IncomingLoginMessage;
				var outgoingFields = Parameters.OutgoingLoginMessage;
				//TBD: ResetSeqNumFlag sending login is separated from resetting sequences
				// we get ResetSeqNumFlag in incoming logon
				if (logonMessage != null && ResetSeqNumFlagIsEnabled(logonMessage) &&
						!(outgoingFields != null && ResetSeqNumFlagIsEnabled(outgoingFields)))
				{
					return true;
				}

				return false;
			}
		}

		/// <inheritdoc />
		private bool ResetSeqNumFlagIsEnabled(FixMessage logonMessage)
		{
			return FixMessageUtil.IsTagValueEquals(logonMessage, Tags.ResetSeqNumFlag, TrueValue);
		}

		/// <inheritdoc />
		public override object GetAndRemoveAttribute(string key)
		{
			var result = GetAttribute(key);
			if (result != null)
			{
				RemoveAttribute(key);
			}

			return result;
		}
	}
}