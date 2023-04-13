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
using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Helpers;
using Epam.FixAntenna.NetCore.Message;

namespace Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.Global
{
	/// <summary>
	/// Throttling checking handler driven by <seealso cref="Config.ThrottleCheckingPeriod"/>
	/// and <seealso cref="Config"/> parameter throttleChecking.msgType.threshold
	/// if <seealso cref="Config.ThrottleCheckingEnabled"/> set to true
	/// </summary>
	internal class ThrottleCheckingHandler : AbstractGlobalMessageHandler
	{
		private bool _isThrottlingEnabled;
		private readonly long _startPoint;
		private long _period;
		private long _start;
		private long _end;

		private readonly IDictionary<ComparableTagValue, FixTypeThrottleInfo> _counters = new Dictionary<ComparableTagValue, FixTypeThrottleInfo>();
		private readonly ComparableTagValue _msgType;

		public ThrottleCheckingHandler()
		{
			_startPoint = DateTimeHelper.CurrentMilliseconds;
			_msgType = new ComparableTagValue();
		}

		/// <inheritdoc />
		public override IExtendedFixSession Session
		{
			set
			{
				base.Session = value;

				_isThrottlingEnabled = GetThrottlingPropertyValue();

				if (_isThrottlingEnabled)
				{
					_period = GetThrottlingPeriod();

					_start = _startPoint + ((int)((DateTimeHelper.CurrentMilliseconds - _startPoint) / _period)) * _period;
					_end = _start + _period;

					_counters.Clear();
				}
			}
		}

		/// <inheritdoc />
		public override void OnNewMessage(FixMessage message)
		{
			if (_isThrottlingEnabled)
			{
				CheckThrottling(message);
			}

			CallNextHandler(message);
		}

		public virtual void CheckThrottling(FixMessage message)
		{
			message.LoadTagValue(Tags.MsgType, _msgType.Tv);
			var info = GetThrottlingInfo(_msgType, DateTimeHelper.CurrentMilliseconds);

			if (info != null && info.IsExceedsLimit)
			{
				Session.Disconnect(DisconnectReason.Throttling, GetDisconnectMessage(info));
			}
		}

		private FixTypeThrottleInfo GetThrottlingInfo(ComparableTagValue msgType, long currentTime)
		{
			var info = _counters.GetValueOrDefault(msgType);
			if (info == null)
			{
				var msgTypeStr = StringHelper.NewString(msgType.Tv.Buffer, msgType.Tv.Offset, msgType.Tv.Length);
				var threshold = GetThreshold(msgTypeStr);
				if (threshold > 0)
				{

					info = new FixTypeThrottleInfo(msgTypeStr, threshold);
					var msgTypeCopy = new ComparableTagValue(msgType.Tv.Buffer, msgType.Tv.Offset, msgType.Tv.Length);
					_counters[msgTypeCopy] = info;
				}
			}

			if (info != null)
			{
				if (currentTime > _end)
				{
					//it need to calculate current period (it may happen that some of then will be without messages at all)
					_start = _startPoint + ((int)((currentTime - _startPoint) / _period)) * _period;
					_end = _start + _period;
					info.Reset();
				}
				info.IncrementCounter();
			}

			return info;
		}

		public virtual long GetThreshold(string msgType)
		{
			return Session.Parameters.Configuration.GetPropertyAsLong(GetMessageTypePropertyName(msgType), -1);
		}

		private string GetDisconnectMessage(FixTypeThrottleInfo info)
		{
			return $"Throttle checking exceeds limit for msgType: {info.MsgType}; {info.PeriodCounter}/{info.Threshold}";
		}

		private string GetMessageTypePropertyName(string msgType)
		{
			return $"throttleChecking.{msgType}.threshold";
		}

		public virtual long GetThrottlingPeriod()
		{
			return Session.Parameters.Configuration.GetPropertyAsLong(Config.ThrottleCheckingPeriod);
		}

		public virtual bool GetThrottlingPropertyValue()
		{
			return Session.Parameters.Configuration.GetPropertyAsBoolean(Config.ThrottleCheckingEnabled);
		}

		private class FixTypeThrottleInfo
		{
			public string MsgType { get; }
			public long Threshold { get; }
			public long PeriodCounter { get; private set; }
			public bool IsExceedsLimit => PeriodCounter > Threshold;

			public FixTypeThrottleInfo(string type, long threshold)
			{
				MsgType = type;
				Threshold = threshold;
				PeriodCounter = 0;
			}

			public void Reset()
			{
				PeriodCounter = 0;
			}

			public void IncrementCounter()
			{
				PeriodCounter++;
			}
		}

		private class ComparableTagValue
		{
			internal readonly TagValue Tv;

			public ComparableTagValue()
			{
				Tv = new TagValue();
			}

			public ComparableTagValue(byte[] buffer, int offset, int length) : this()
			{
				Tv.Value = new byte[length];
				Array.Copy(buffer, offset, Tv.Buffer, 0, length);
			}

			public override int GetHashCode()
			{
				var result = 17;
				if (Tv.Buffer != null)
				{
					for (var i = 0; i < Tv.Length; i++)
					{
						result = 31 * result + Tv.Buffer[Tv.Offset + i];
					}
				}
				return result;
			}

			public override bool Equals(object obj)
			{
				if (obj is ComparableTagValue other)
				{
					return IsEquals(Tv.Buffer, Tv.Offset, Tv.Length, other.Tv.Buffer, other.Tv.Offset, other.Tv.Length);
				}
				return false;
			}

			public bool IsEquals(byte[] src, int srcStart, int srcLen, byte[] dest, int destStart, int destLen)
			{
				if (src == null && dest == null)
				{
					return true;
				}

                if (src == null || dest == null)
                {
                    return false;
                }

                if (srcLen != destLen)
				{
					return false;
				}

				for (var i = 0; i < srcLen; i++)
				{
					if (src[srcStart + i] != dest[destStart + i])
					{
						return false;
					}
				}
				return true;
			}
		}
	}
}