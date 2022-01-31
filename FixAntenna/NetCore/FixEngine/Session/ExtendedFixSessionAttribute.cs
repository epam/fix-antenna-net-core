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
using Epam.FixAntenna.NetCore.Configuration;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	/// <summary>
	/// Enum define the session all attributes.
	/// </summary>
	/// <seealso cref="IExtendedFixSession.GetAttribute(string)"></seealso>
	/// <seealso cref="IExtendedFixSession.SetAttribute(string, object)"></seealso>
	/// <seealso cref="IExtendedFixSession.RemoveAttribute(string)"></seealso>
	internal sealed class ExtendedFixSessionAttribute
	{
		/// <summary>
		/// Engine automatically set the next processed seq num when the message received
		/// </summary>
		//IncomingSeqNum,

		/// <summary>
		/// The last RR seq num occurred when engine send RR and after every saves it to file.
		/// </summary>
		public static readonly ExtendedFixSessionAttribute LastRrSeqNum = new ExtendedFixSessionAttribute("LastRRSeqNum", InnerEnum.LastRrSeqNum);

		/// <summary>
		/// The <c>DeleteLastProcessedSeqNumFromFile</c> is present in session attributes only when
		/// the last processed seq num is load from session parameters file. If this flag is set the session should
		/// update the appropriate parameter in properties file.
		/// </summary>
		public static readonly ExtendedFixSessionAttribute DeleteLastProcessedSeqNumFromFile = new ExtendedFixSessionAttribute("DeleteLastProcessedSeqNumFromFile", InnerEnum.DeleteLastProcessedSeqNumFromFile);

		/// <summary>
		/// This constant signals that test request sent by fix session.
		/// </summary>
		public static readonly ExtendedFixSessionAttribute LastSentTestReqId = new ExtendedFixSessionAttribute("LastSentTestReqID", InnerEnum.LastSentTestReqId);

		/// <summary>
		/// This constant signals that how many test request sent by fix session.
		/// </summary>
		public static readonly ExtendedFixSessionAttribute SentTestReqNumberId = new ExtendedFixSessionAttribute("SentTestReqNumberID", InnerEnum.SentTestReqNumberId);

		/// <summary>
		/// This constant signals that test request had been received by fix session.
		/// </summary>
		public static readonly ExtendedFixSessionAttribute LastReceivedTestReqId = new ExtendedFixSessionAttribute("LastReceivedTestReqID", InnerEnum.LastReceivedTestReqId);

		/// <summary>
		/// This is only for internal use.
		/// </summary>
		public static readonly ExtendedFixSessionAttribute IsSendResetSeqNum = new ExtendedFixSessionAttribute("IsSendResetSeqNum", InnerEnum.IsSendResetSeqNum);

		/// <summary>
		/// This is only for internal use.
		/// Start of sequence range.
		/// </summary>
		public static readonly ExtendedFixSessionAttribute StartOfRrRange = new ExtendedFixSessionAttribute("StartOfRRRange", InnerEnum.StartOfRrRange);

		/// <summary>
		/// This is only for internal use.
		/// End of sequence range.
		/// </summary>
		public static readonly ExtendedFixSessionAttribute EndOfRrRange = new ExtendedFixSessionAttribute("EndOfRRRange", InnerEnum.EndOfRrRange);

		/// <summary>
		/// This is only for internal use.
		/// Indicates the first incoming logon sequence number.
		/// </summary>
		public static readonly ExtendedFixSessionAttribute StartRrSeqNum = new ExtendedFixSessionAttribute("StartRRSeqNum", InnerEnum.StartRrSeqNum);

		/// <summary>
		/// This is only for internal use.
		/// Indicates the session is in state Reject
		/// </summary>
		public static readonly ExtendedFixSessionAttribute RejectSession = new ExtendedFixSessionAttribute("RejectSession", InnerEnum.RejectSession);
		/// <summary>
		/// This is only for internal use.
		/// Indicates that logon has seq to high.
		/// </summary>
		//LogonSeqNumToHigh(),

		/// <summary>
		/// This is only for internal use.
		/// Indicates that TR was send for SR.
		/// </summary>
		public static readonly ExtendedFixSessionAttribute TestRequestIsSentForSeqReset = new ExtendedFixSessionAttribute("TestRequestIsSentForSeqReset", InnerEnum.TestRequestIsSentForSeqReset);

		/// <summary>
		/// This is only for internal use.
		/// Indicates that logon was send for SR.
		/// </summary>
		public static readonly ExtendedFixSessionAttribute IntradayLogonIsSent = new ExtendedFixSessionAttribute("IntradayLogonIsSent", InnerEnum.IntradayLogonIsSent);


		public static readonly ExtendedFixSessionAttribute IsResendRequestProcessed = new ExtendedFixSessionAttribute("IsResendRequestProcessed", InnerEnum.IsResendRequestProcessed);

		/// <summary>
		/// This attribute contains how many similar ResendRequests were sent, to prevent infinite resent loop. </summary>
		/// <seealso cref="Config.AllowedCountOfSimilarRr"></seealso>
		public static readonly ExtendedFixSessionAttribute SimilarRrCounter = new ExtendedFixSessionAttribute("SimilarRRCounter", InnerEnum.SimilarRrCounter);

		/// <summary>
		/// This attribute contains range (startSeqNum-EndSeqNum) of last sent ResendRequest. It used to detect sending
		/// similar RR. </summary>
		/// <seealso cref="Config.AllowedCountOfSimilarRr"> </seealso>
		public static readonly ExtendedFixSessionAttribute LastRrRange = new ExtendedFixSessionAttribute("LastRRRange", InnerEnum.LastRrRange);

		/// <summary>
		/// This attribute contains original sequence number.
		/// </summary>
		public static readonly ExtendedFixSessionAttribute SequenceWasDecremented = new ExtendedFixSessionAttribute("SequenceWasDecremented", InnerEnum.SequenceWasDecremented);

		public static readonly ExtendedFixSessionAttribute RequestedEndRrRange =
			new ExtendedFixSessionAttribute("RequestedEndRrRange", InnerEnum.RequestedEndRrRange);

		private static readonly IList<ExtendedFixSessionAttribute> ValueList = new List<ExtendedFixSessionAttribute>();

		static ExtendedFixSessionAttribute()
		{
			ValueList.Add(LastRrSeqNum);
			ValueList.Add(DeleteLastProcessedSeqNumFromFile);
			ValueList.Add(LastSentTestReqId);
			ValueList.Add(SentTestReqNumberId);
			ValueList.Add(LastReceivedTestReqId);
			ValueList.Add(IsSendResetSeqNum);
			ValueList.Add(StartOfRrRange);
			ValueList.Add(EndOfRrRange);
			ValueList.Add(StartRrSeqNum);
			ValueList.Add(RejectSession);
			ValueList.Add(TestRequestIsSentForSeqReset);
			ValueList.Add(IntradayLogonIsSent);
			ValueList.Add(IsResendRequestProcessed);
			ValueList.Add(SimilarRrCounter);
			ValueList.Add(LastRrRange);
			ValueList.Add(SequenceWasDecremented);
			ValueList.Add(RequestedEndRrRange);
		}

		private ExtendedFixSessionAttribute(string name, InnerEnum value)
		{
			Name = name;
			_innerEnumValue = value;
			_ordinalValue = _nextOrdinal++;
		}

		internal enum InnerEnum
		{
			LastRrSeqNum,
			DeleteLastProcessedSeqNumFromFile,
			LastSentTestReqId,
			SentTestReqNumberId,
			LastReceivedTestReqId,
			IsSendResetSeqNum,
			StartOfRrRange,
			EndOfRrRange,
			StartRrSeqNum,
			RejectSession,
			TestRequestIsSentForSeqReset,
			IntradayLogonIsSent,
			IsResendRequestProcessed,
			SimilarRrCounter,
			LastRrRange,
			SequenceWasDecremented,
			RequestedEndRrRange
		}

		private readonly InnerEnum _innerEnumValue;
		private readonly int _ordinalValue;
		private static int _nextOrdinal = 0;

		public const string YesValue = "Y";

		public static IList<ExtendedFixSessionAttribute> Values()
		{
			return ValueList;
		}

		public int Ordinal()
		{
			return _ordinalValue;
		}

		public string Name { get; }

		public override string ToString()
		{
			return Name;
		}

		public static ExtendedFixSessionAttribute ValueOf(string name)
		{
			foreach (var enumInstance in ExtendedFixSessionAttribute.ValueList)
			{
				if (enumInstance.Name == name)
				{
					return enumInstance;
				}
			}
			throw new System.ArgumentException(name);
		}
	}
}