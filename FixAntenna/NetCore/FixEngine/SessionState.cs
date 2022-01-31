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

using System.Collections.Generic;

namespace Epam.FixAntenna.NetCore.FixEngine
{
	/// <summary>
	/// Session state enum
	/// </summary>
	public sealed class SessionState
	{
		public static readonly SessionState Connecting = new SessionState("CONNECTING", InnerEnum.Connecting);
		public static readonly SessionState WaitingForLogon = new SessionState("WAITING_FOR_LOGON", InnerEnum.WaitingForLogon);
		public static readonly SessionState Connected = new SessionState("CONNECTED", InnerEnum.Connected);
		public static readonly SessionState WaitingForLogoff = new SessionState("WAITING_FOR_LOGOFF", InnerEnum.WaitingForLogoff);
		public static readonly SessionState Disconnected = new SessionState("DISCONNECTED", InnerEnum.Disconnected);
		public static readonly SessionState LogonReceived = new SessionState("LOGON_RECEIVED", InnerEnum.LogonReceived);
		public static readonly SessionState Dead = new SessionState("DEAD", InnerEnum.Dead);
		public static readonly SessionState DisconnectedAbnormally = new SessionState("DISCONNECTED_ABNORMALLY", InnerEnum.DisconnectedAbnormally);
		public static readonly SessionState Reconnecting = new SessionState("RECONNECTING", InnerEnum.Reconnecting);
		/// <summary>
		/// Wait for Logout answer
		/// </summary>
		public static readonly SessionState WaitingForForcedLogoff = new SessionState("WAITING_FOR_FORCED_LOGOFF", InnerEnum.WaitingForForcedLogoff);
		/// <summary>
		/// Wait a bit to close session after sending Logout
		/// </summary>
		public static readonly SessionState WaitingForForcedDisconnect = new SessionState("WAITING_FOR_FORCED_DISCONNECT", InnerEnum.WaitingForForcedDisconnect);

		private static readonly IList<SessionState> ValueList = new List<SessionState>();

		static SessionState()
		{
			ValueList.Add(Connecting);
			ValueList.Add(WaitingForLogon);
			ValueList.Add(Connected);
			ValueList.Add(WaitingForLogoff);
			ValueList.Add(Disconnected);
			ValueList.Add(LogonReceived);
			ValueList.Add(Dead);
			ValueList.Add(DisconnectedAbnormally);
			ValueList.Add(Reconnecting);
			ValueList.Add(WaitingForForcedLogoff);
			ValueList.Add(WaitingForForcedDisconnect);
		}

		public enum InnerEnum
		{
			Connecting,
			WaitingForLogon,
			Connected,
			WaitingForLogoff,
			Disconnected,
			LogonReceived,
			Dead,
			DisconnectedAbnormally,
			Reconnecting,
			WaitingForForcedLogoff,
			WaitingForForcedDisconnect
		}

		public readonly InnerEnum EnumValue;
		private readonly string _nameValue;
		private readonly int _ordinalValue;
		private static int _nextOrdinal = 0;

		private SessionState(string name, InnerEnum innerEnum)
		{
			_nameValue = name;
			_ordinalValue = _nextOrdinal++;
			EnumValue = innerEnum;
		}

		public static bool IsConnected(SessionState sessionState)
		{
			return sessionState == Connected;
		}

		public static bool IsNotDisconnected(SessionState sessionState)
		{
			return sessionState != Disconnected && sessionState != DisconnectedAbnormally;
		}

		/// <summary>
		/// Checks the session`s state and returns true if state is Disconnected or DisconnectedAbnormally.
		/// </summary>
		/// <param name="sessionState">Session state</param>
		/// <returns>true if state is Disconnected or DisconnectedAbnormally, false otherwise.</returns>
		public static bool IsDisconnected(SessionState sessionState) //TODO: Maybe not static but instance method?
		{
			return sessionState == Disconnected || sessionState == DisconnectedAbnormally;
		}

		public static bool IsDisposed(SessionState sessionState)
		{
			return Dead == sessionState;
		}

		public static IList<SessionState> Values()
		{
			return ValueList;
		}

		public int Ordinal()
		{
			return _ordinalValue;
		}

		public override string ToString()
		{
			return _nameValue;
		}

		public static SessionState ValueOf(string name)
		{
			foreach (var enumInstance in SessionState.ValueList)
			{
				if (enumInstance._nameValue == name)
				{
					return enumInstance;
				}
			}

			throw new System.ArgumentException(name);
		}
	}
}