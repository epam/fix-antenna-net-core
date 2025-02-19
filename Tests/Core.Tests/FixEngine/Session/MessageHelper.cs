using System;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;
using Epam.FixAntenna.NetCore.Message.Format;
using NUnit.Framework; 
using NUnit.Framework.Legacy;

namespace Epam.FixAntenna.NetCore.FixEngine.Session
{
	internal static class TestMessageHelper
	{
		public static void ClassicAssertTypeAndSeqNum(FixMessage msg, string type, int seqNum)
		{
			ClassicAssert.AreEqual(type, msg.GetTagValueAsString(35), "Unexpected type in message: " + msg);
			ClassicAssert.AreEqual(seqNum, msg.GetTagAsInt(34), "Unexpected SeqNum in message: " + msg);
		}

		public static byte[] GetNewsMessage(int seqNum)
		{
			var rawMsg = "8=FIX.4.4#9=92#35=B#34=2#49=initiator#56=acceptor#52=20130409-08:09:16.499#148=Hello there:1#33=1#58=line1#10=232#".Replace('#', '\u0001');
			return PrepareMsg(rawMsg, seqNum);
		}

		public static byte[] GetLogonMessage(int seqNum)
		{
			var rawLogon = "8=FIX.4.4#9=72#35=A#34=1#49=initiator#56=acceptor#52=20130409-07:49:00.678#98=0#108=10#10=045#".Replace('#', '\u0001');
			return PrepareMsg(rawLogon, seqNum);
		}

		public static byte[] GetLogonMessage(int seqNum, string additionalFields)
		{
			var rawLogon = ("8=FIX.4.4#9=72#35=A#34=1#49=initiator#56=acceptor#52=20130409-07:49:00.678#98=0#108=10#" + additionalFields + "10=045#").Replace('#', '\u0001');
			return PrepareMsg(rawLogon, seqNum);
		}

		public static byte[] GetLogoutMessage(int seqNum)
		{
			var rawLogout = "8=FIX.4.4#9=75#35=5#34=3#49=initiator#56=acceptor#52=20140319-09:35:10.385#58=User request#10=186#".Replace('#', '\u0001');
			return PrepareMsg(rawLogout, seqNum);
		}

		private static byte[] PrepareMsg(string rawMsg, int seqNum)
		{
			var msg = RawFixUtil.GetFixMessage(rawMsg.AsByteArray());
			msg.SetCalendarValue(52, DateTimeOffset.Now, FixDateFormatterFactory.FixDateType.UtcTimestampWithMillis);
			msg.Set(34, seqNum);
			msg.Set(9, msg.CalculateBodyLength());
			msg.Set(10, FormatChecksum(msg.CalculateChecksum()));
			return msg.AsByteArray();
		}

		private static byte[] FormatChecksum(int checksum)
		{
			var val = new byte[3];
			val[0] = (byte)(checksum / 100 + (byte) '0');
			val[1] = (byte)(((checksum / 10) % 10) + (byte) '0');
			val[2] = (byte)((checksum % 10) + (byte) '0');
			return val;
		}
	}
}
