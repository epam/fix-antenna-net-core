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

using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Common.Logging;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

using HdrHistogram;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Epam.FixAntenna.Samples.Latency.Sender
{
	public sealed class RoundTripTester : IFixSessionListener
	{
		private static readonly ILog Log = LogFactory.GetLog(typeof(RoundTripTester));
		private const int NoOfMessages = 100_000;
		private const int WarmUpCycleCount = 50_000;
		private static readonly FixMessage WarmUpMessage = RawFixUtil.GetFixMessage(Encoding.UTF8.GetBytes("8=FIX.4.2\u00019=114\u000135=W\u000149=TESTI\u000156=TESTA\u000134=14\u000152=20030204-09:25:43\u0001262=0001\u000155=TESTA\u0001268=2\u0001269=0\u0001270=10\u0001271=50\u0001269=1\u0001270=11\u0001271=40\u000110=116\u0001"));
		private static string ResultsDir = "Results";

		private IFixSession Session { get; set; }

		private int _counter = 0;
		private int _warmUpCounter = 0;
		private long _start;
		private static LongHistogram _latency;
		private FixMessage _workMessage;
		private readonly byte[] _workMessageType = Encoding.UTF8.GetBytes("8");
		private int _sentPercent;

		public static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				PrintUsageAndExit();
			}

			var host = args[0];
			var port = -1;
			try
			{
				port = int.Parse(args[1]);
			}
			catch (FormatException)
			{
				Console.WriteLine("Incorrect port");
				PrintUsageAndExit();
			}

			Init(host, port);
		}

		private static void Init(string host, int port)
		{
			_latency = new LongHistogram(TimeStamp.Seconds(10), 3);
			var tester = new RoundTripTester();
			var details = new SessionParameters
			{
				FixVersion = FixVersion.Fix42,
				Host = host,
				HeartbeatInterval = 30,
				Port = port,
				SenderCompId = "SenderCompId",
				TargetCompId = "TargetCompId"
			};

			var pmu = new PreparedMessageUtil(details);

			tester._workMessage = pmu.PrepareMessageFromString(Encoding.UTF8.GetBytes(
				"8=FIX.4.2\u00019=153\u000135=D\u000149=SenderCompId\u000156=TargetCompId" +
				"\u000134=1\u000150=30737\u000197=Y\u000152=20000809-20:20:50\u000111=90001008\u00011=10030003\u000121=2\u0001" +
				"55=TESTA\u000154=1\u000138=4000\u000140=2\u000159=0\u000144=30\u0001109=111\u000147=I\u000160=20000809-18:20:32\u000110=061\u0001"), "D", new MessageStructure());

			tester.Session = details.CreateNewFixSession();
			tester.Session.SetFixSessionListener(tester);

			tester.InitializeSession();
		}

		private void InitializeSession()
		{
			Session.Connect();
			WarmUpEnvironment();
		}

		private void WarmUpEnvironment()
		{
			Log.Info("Warm up...");
			for (var i = 0; i < WarmUpCycleCount; i++)
			{
				Session.SendMessage(WarmUpMessage);
			}
			Log.Info("Warm up completed.");
		}


		private void SendMessage()
		{
			_start = Stopwatch.GetTimestamp();
			Session.SendMessage(_workMessage);
		}

		public void OnSessionStateChange(SessionState sessionState)
		{
			Log.Info("Session state has been changed:" + sessionState);
			if (SessionState.IsDisconnected(sessionState))
			{
				Session.Dispose();
			}
		}

		public void OnNewMessage(FixMessage message)
		{
			try
			{
				var elapsed = Stopwatch.GetTimestamp() - _start;
				if (_workMessageType.SequenceEqual(message.MsgType))
				{
					_latency.RecordValue(elapsed);
					_counter++;

					if (_counter == NoOfMessages)
					{
						StoreResults(_latency);
						var mean = _latency.GetMean() / OutputScalingFactor.TimeStampToMicroseconds;
						var max = _latency.GetMaxValue() / OutputScalingFactor.TimeStampToMicroseconds;
						Log.Info($"Avg: {{{mean}}} us/op");
						Log.Info($"Max: {{{max}}} us/op");
						Session.Disconnect("User request");
					}
					else
					{
						var currentPercent = 100 * _counter / NoOfMessages;
						if (currentPercent - _sentPercent >= 5)
						{
							_sentPercent = currentPercent;
							Console.Write($"  {currentPercent}% messages sent.");
							Console.CursorLeft = 0;
						}
						SendMessage();
					}
				}
				else if (WarmUpMessage.MsgType.SequenceEqual(message.MsgType))
				{
					_warmUpCounter++;
					if (_warmUpCounter == WarmUpCycleCount)
					{
						SendMessage();
					}
				}
			}
			catch (Exception e)
			{
				Log.Error(e.Message, e);
			}
		}

		private static void PrintUsageAndExit()
		{
			Console.WriteLine("Sender host port");
			Environment.Exit(1);
		}

		private static void StoreResults(LongHistogram data)
		{
			Directory.CreateDirectory(ResultsDir);
			var fileName = $"Latency-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.hgrm";
			var path = Path.Combine(ResultsDir, fileName);

			using (var writer = File.CreateText(path))
			{
				data.OutputPercentileDistribution(writer, outputValueUnitScalingRatio: OutputScalingFactor.TimeStampToMicroseconds);
			}
		}
	}
}