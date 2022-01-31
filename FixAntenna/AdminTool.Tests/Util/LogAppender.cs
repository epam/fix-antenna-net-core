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
using System.Collections.Concurrent;
using System.Text;
using NLog;
using NLog.Targets;
using NUnit.Framework;

namespace Epam.FixAntenna.AdminTool.Tests.Util
{
	[Target("CustomMemoryTarget")]
	internal sealed class LogAppender : TargetWithLayout
	{
		private static ConcurrentBag<string> Errors = new ConcurrentBag<string>();
		private static ConcurrentBag<string> Warnings = new ConcurrentBag<string>();

		private static LogAppender Target;

		static LogAppender()
		{
			var config = LogManager.Configuration;
			Target = new LogAppender();
			config.AddTarget(Target);
			config.AddRule(LogLevel.Warn, LogLevel.Fatal, Target);
			LogManager.Configuration = config;
		}

		public LogAppender()
		{
			Layout = "${message}";
			Name = "Tests";
		}

		protected override void Write(LogEventInfo logEvent)
		{
			if (logEvent.Level < LogLevel.Warn)
			{
				return;
			}

			var logEntry = Layout.Render(logEvent);
			Warnings.Add(logEntry);

			if (logEvent.Level >= LogLevel.Error)
			{
				Errors.Add(logEntry);
			}
		}

		public static string GetWarnings()
		{
			return AsString(Warnings);
		}

		private static string AsString(ConcurrentBag<string> log)
		{
#if NET48
			var sb = new StringBuilder();
			foreach (var line in log)
			{
				sb.AppendLine(line);
			}
			return sb.ToString();
#else
			return new StringBuilder().AppendJoin(Environment.NewLine, log).ToString();
#endif
		}

		public static void Clear()
		{
#if NET48
			Errors = new ConcurrentBag<string>();
			Warnings = new ConcurrentBag<string>();
#else
			Errors.Clear();
			Warnings.Clear();
#endif
		}

		public static void AssertIfErrorExist()
		{
			Assert.IsTrue(Errors.IsEmpty, AsString(Errors));
		}
	}
}