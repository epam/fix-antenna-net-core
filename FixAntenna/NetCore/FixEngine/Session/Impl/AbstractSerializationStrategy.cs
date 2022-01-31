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

using System.Text;

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.Impl
{
	internal abstract class AbstractSerializationStrategy : ISerializationStrategy
	{
		public abstract void Serialize(FixMessage content, ByteBuffer buffer, SerializationContext context, FixSessionRuntimeState runtimeState);

		protected internal const int SohLength = 1;
		protected internal const char Soh = '\u0001';
		protected internal const char Equal = '=';
		protected internal static readonly byte[] SendingTimeTagValue = "52=".AsByteArray();
		protected internal static readonly byte[] MsgTypeTagValue = "35=".AsByteArray();
		protected internal static readonly byte[] SeqNumTagValue = "34=".AsByteArray();
		protected internal static readonly byte[] LastProcessedTagValue = "369=".AsByteArray();
		protected internal const int CheckSumFieldLength = 7;

		protected internal SessionParameters SessionParameters;

		protected internal byte[] BeginStringBodyLengthHeader;
		protected internal byte[] MessageVersion;
		protected internal byte[] SenderCompId;
		protected internal byte[] TargetCompId;
		protected internal byte[] SenderSubId;
		protected internal byte[] TargetSubId;
		protected internal byte[] SenderLocationId;
		protected internal byte[] TargetLocationId;

		protected internal bool IncludeLastProcessed;
		protected internal int HeartbeatInterval;
		protected internal bool IncludeNextExpectedMsgSeqNum;

		protected internal int MinSeqNumLength;
		private string _seqNumFormat;

		public virtual void Init(SessionParameters sessionParameters)
		{
			this.SessionParameters = sessionParameters;
			if (sessionParameters.FixVersion.MessageVersion != null)
			{
				MessageVersion = sessionParameters.FixVersion.MessageVersion.AsByteArray();
			}

			//TODO: optimize - add to byte[]
			if (sessionParameters.SenderCompId != null)
			{
				SenderCompId = sessionParameters.SenderCompId.AsByteArray();
			}
			if (sessionParameters.TargetCompId != null)
			{
				TargetCompId = sessionParameters.TargetCompId.AsByteArray();
			}
			if (sessionParameters.SenderSubId != null)
			{
				SenderSubId = sessionParameters.SenderSubId.AsByteArray();
			}
			if (sessionParameters.TargetSubId != null)
			{
				TargetSubId = sessionParameters.TargetSubId.AsByteArray();
			}
			if (sessionParameters.SenderLocationId != null)
			{
				SenderLocationId = sessionParameters.SenderLocationId.AsByteArray();
			}
			if (sessionParameters.TargetLocationId != null)
			{
				TargetLocationId = sessionParameters.TargetLocationId.AsByteArray();
			}
			BeginStringBodyLengthHeader = GetBeginStringFieldWithBodyLengthTag();
			IncludeLastProcessed = sessionParameters.IsNeedToIncludeLastProcessed();
			HeartbeatInterval = sessionParameters.HeartbeatInterval;

			var fixVersion = sessionParameters.FixVersion;

			// include only if current session FIXVersion is 4.4 or above
			if (fixVersion.CompareTo(FixVersion.Fix44) >= 0)
			{
				IncludeNextExpectedMsgSeqNum = sessionParameters.Configuration.GetPropertyAsBoolean(Config.HandleSeqnumAtLogon);
			}

			MinSeqNumLength = sessionParameters.Configuration.GetPropertyAsInt(Config.SeqNumLength, 1, 10, true, "Wrong value in parameter SeqNumLength. The padding is disabled.");
			_seqNumFormat = "{0:D" + MinSeqNumLength + "}";
		}

		/// <summary>
		/// Gets header
		/// </summary>
		private byte[] GetBeginStringFieldWithBodyLengthTag()
		{
			var sb = new StringBuilder();
			sb.Append("8=").Append(SessionParameters.FixVersion.MessageVersion).Append(Soh);
			sb.Append("9=");
			return sb.ToString().AsByteArray();
		}

		public virtual byte[] GetLoginHeader(FixSessionRuntimeState runtimeState)
		{
			var sb = new StringBuilder();
			if (SessionParameters.FixVersion == FixVersion.Fixt11)
			{
				sb.Append(Tags.DefaultApplVerID).Append(Equal).Append(SessionParameters.AppVersion.FixtVersion).Append(Soh);
			}
			sb.Append(Tags.EncryptMethod).Append(Equal).Append(0).Append(Soh); // NONE encryption
			sb.Append(Tags.HeartBtInt).Append(Equal).Append(HeartbeatInterval).Append(Soh);
			if (IncludeNextExpectedMsgSeqNum)
			{
				sb.Append(Tags.NextExpectedMsgSeqNum).Append(Equal).AppendFormat(_seqNumFormat, runtimeState.InSeqNum).Append(Soh);
			}
			sb.Append(runtimeState.OutgoingLogon.ToUnmaskedString());
			return sb.ToString().AsByteArray();
		}

		public void WriteChecksumField(byte[] rawMessage, int offset, int length)
		{
			var checksum = RawFixUtil.GetChecksum(rawMessage, offset, length);
			var position = offset + length;
			rawMessage[position++] = (byte)'1';
			rawMessage[position++] = (byte)'0';
			rawMessage[position++] = (byte)'=';

			rawMessage[position++] = (byte)(checksum / 100 + (byte) '0');
			rawMessage[position++] = (byte)(((checksum / 10) % 10) + (byte) '0');
			rawMessage[position++] = (byte)((checksum % 10) + (byte) '0');
			rawMessage[position] = (byte)Soh;
		}
	}
}