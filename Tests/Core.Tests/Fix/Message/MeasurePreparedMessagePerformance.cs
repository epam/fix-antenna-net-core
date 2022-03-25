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
using System.Diagnostics;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Utils;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.FixEngine.Session.Impl;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.Fix.Message
{
	public static class MeasurePreparedMessagePerformance
	{
		internal const string RawMsg = "8=FIX.4.2\u00019=1\u000135=D\u000149=BLP\u000156=SCHB\u000134=01\u000150=30737\u000152=20000809-20:20:50\u000111=90001008\u00011=10030003\u000121=2\u000155=TESTA\u000154=1\u000138=4000\u000140=2\u000159=0\u000144=30\u000147=I\u000160=20000809-18:20:32\u000110=000\u0001";


	//	public static void Main(string[] args)
	//	{
	//		MeasurePreparedMessagePerformance.MeasureNormal();
	////        TBD! TBD test
	////        MeasurePreparedMessagePerformance.measurePrepared();
	//	}

		public static void MeasurePrepared()
		{
			var ms = new MessageStructure();
			ms.Reserve(11, 10);
			ms.Reserve(44, 7);
			ms.Reserve(38, 2);
			ms.Reserve(60, "YYYYMMDD-HH:MM:SS.sss".Length);

			var sessionParams = new SessionParameters();
			sessionParams.Port = 12000;
			sessionParams.Host = "localhost";
			sessionParams.TargetCompId = "admin";
			sessionParams.SenderCompId = "admin3";
			sessionParams.SenderSubId = "ssId";
			sessionParams.TargetSubId = "ee";

			var pmu = new PreparedMessageUtil(sessionParams);

			var preparedMessage = pmu.PrepareMessageFromString(RawMsg.AsByteArray(), "D", ms);

			var messageFactory = new StandardMessageFactory();
			messageFactory.SetSessionParameters(sessionParams);

			var buff = new ByteBuffer();

			for (var i = 0; i < 1000000; i++)
			{
				preparedMessage.Set(11, FixTypes.FormatInt(i)); // ClOrdID
				preparedMessage.Set(44, FixTypes.FormatInt(100023)); // Price
				preparedMessage.Set(38, FixTypes.FormatInt(10)); // OrderQty
				preparedMessage.Set(60, FixTypes.FormatDate(DateTime.UtcNow)); // TransactTime

				var copy = preparedMessage.DeepClone(false, false);
				messageFactory.Serialize(null, copy, buff, null);
				buff.ResetBuffer();
			}

			var timer = new Stopwatch();
			timer.Start();
			var iterationCount = 10000000;
			for (var i = 0; i < iterationCount; i++)
			{
				preparedMessage.Set(11, FixTypes.FormatInt(i)); // ClOrdID
				preparedMessage.Set(44, FixTypes.FormatInt(100023)); // Price
				preparedMessage.Set(38, FixTypes.FormatInt(10)); // OrderQty
				preparedMessage.Set(60, FixTypes.FormatDate(DateTime.UtcNow)); // TransactTime

				var copy = preparedMessage.DeepClone(false, false);
				messageFactory.Serialize(null, copy, buff, null);
				buff.ResetBuffer();
			}
			timer.Stop();
			var dela = timer.ElapsedTicks / DateTimeHelper.NanosecondsPerTick;
			var timeForMsg = dela / iterationCount;
			Console.WriteLine("Avg time for 1 prepared msg: " + timeForMsg + "ns");
		}


		public static void MeasureNormal()
		{
			var sessionParams = new SessionParameters();
			sessionParams.Port = 12000;
			sessionParams.Host = "localhost";
			sessionParams.TargetCompId = "admin";
			sessionParams.SenderCompId = "admin3";
			sessionParams.SenderSubId = "ssId";
			sessionParams.TargetSubId = "ee";


			var messageFactory = new StandardMessageFactory();
			messageFactory.SetSessionParameters(sessionParams);
			messageFactory.SetRuntimeState(new FixSessionRuntimeState());

			var calendar = new DateTime();
			var buff = new ByteBuffer();
			var origFixMessage = RawFixUtil.GetFixMessage(RawMsg.AsByteArray());
			for (var i = 0; i < 1000000; i++)
			{
				var fixMessage = origFixMessage.DeepClone(true, true);
				fixMessage.Set(44, FixTypes.FormatInt(100023));
				fixMessage.Set(11, FixTypes.FormatInt(i));
				fixMessage.Set(38, FixTypes.FormatInt(10));
				fixMessage.Set(60, FixTypes.FormatDate(DateTime.UtcNow));

				messageFactory.Serialize("D", fixMessage, buff, null);
				buff.ResetBuffer();
			}

			var timer = new Stopwatch();
			var iterationCount = 10000000;
			timer.Start();
			for (var i = 0; i < iterationCount; i++)
			{
				var fixMessage = origFixMessage.DeepClone(true, true);
				fixMessage.Set(44, FixTypes.FormatInt(100023));
				fixMessage.Set(11, FixTypes.FormatInt(i));
				fixMessage.Set(38, FixTypes.FormatInt(10));

				fixMessage.Set(60, FixTypes.FormatDate(calendar));

				messageFactory.Serialize("D", fixMessage, buff, null);
				buff.ResetBuffer();
			}
			timer.Stop();
			var dela = timer.ElapsedTicks / DateTimeHelper.NanosecondsPerTick;
			var timeForMsg = dela / iterationCount;
			Console.WriteLine("Avg time for 1 normal msg: " + timeForMsg + "ns");
		}
	}
}