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
using System.Text;
using Epam.FixAntenna.NetCore.Common.Logging;

namespace Epam.FixAntenna.Tester.Comparator
{
	internal abstract class GenericUnorderedComparator : IMessageComparator
	{
		private string _separators;
		private string _etalonMessage;
		protected internal readonly ILog Log = LogFactory.GetLog(typeof(GenericUnorderedComparator));
		protected internal MessageFieldMap EtalonMessageMap;
		protected internal MessageFieldMap CurrentMessageMap;

		public GenericUnorderedComparator()
		{
		}

		public object Clone()
		{
			var clone = (GenericUnorderedComparator)this.MemberwiseClone();
			clone.OnCloned();
			return clone;
		}

		protected virtual void OnCloned()
		{
			EtalonMessageMap = (MessageFieldMap)EtalonMessageMap.Clone();
			CurrentMessageMap = (MessageFieldMap)CurrentMessageMap.Clone();
		}

		public virtual void SetMessageSeparator(string separators)
		{
			this._separators = separators;
		}

		public virtual void SetEtalonMessage(string etalonMessage)
		{
			this._etalonMessage = etalonMessage;
			EtalonMessageMap = GetMessageMap(etalonMessage, _separators);
		}

		public virtual bool IsMessageOk(string message)
		{
			Log.Info("actual message is:" + message.Replace('\x0001', '#'));
			Log.Info("etalon message is:" + _etalonMessage);

			if (string.ReferenceEquals(_etalonMessage, null))
			{
				throw new System.InvalidOperationException("Actual or etalon message wasn't initialized properly.");
			}

			CurrentMessageMap = GetMessageMap(message, "\x0001");
			foreach (string key in EtalonMessageMap.Keys)
			{
				string value = CurrentMessageMap[key];
				string etalon = EtalonMessageMap[key];

				if (!IsTagsEquals(value, etalon))
				{
					Log.Info("not equals: '" + value + "' to '" + etalon + "' For tag:" + key);
					return false;
				}
			}
			return IsTagCountsEquals(CurrentMessageMap.Count);
		}

		public abstract bool IsTagCountsEquals(int tagCount);

		public abstract bool IsTagsEquals(string value, string etalon);

		private MessageFieldMap GetMessageMap(string message, string separator)
		{
			var tokens = message.Split(separator.ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
			var map = new MessageFieldMap(tokens.Length << 1);
			foreach(string field in tokens)
			{
				int index = field.IndexOf('=');
				if (index <= 0)
				{
					throw new System.ArgumentException("Valid pair expected. Got: " + field);
				}
				map[field.Substring(0, index)] = field.Substring(index + 1);
			}
			return map;
		}


		public class MessageFieldMap : CustomDictionary<string, string>, ICloneable
		{
			public MessageFieldMap(int initialCapacity) : base(initialCapacity)
			{
			}

			public object Clone()
			{
				var other = new MessageFieldMap(this.Count << 1);
				foreach (var entry in this)
				{
					other.Add(entry.Key, entry.Value);
				}
				return other;
			}

            /// <summary>
            /// Returns a string representation of this map.  The string
            /// representation consists of a list of key-value mappings in the order
            /// returned by the map's <tt>entrySet</tt> view's iterator, enclosed in
            /// braces (<tt>"{}"</tt>).  Adjacent mappings are separated by the
            /// characters <tt>", "</tt> (comma and space).  Each key-value mapping
            /// is rendered as the key followed by an equals sign (<tt>"="</tt>)
            /// followed by the associated value.  Keys and values are converted to
            /// strings as by <tt>String.valueOf(Object)</tt>.<para>
            /// <p/>
            /// This implementation creates an empty string buffer, appends a left
            /// brace, and iterates over the map's <tt>entrySet</tt> view, appending
            /// the string representation of each <tt>map.entry</tt> in turn.  After
            /// appending each entry except the last, the string <tt>", "</tt> is
            /// appended.  Finally a right brace is appended.  A string is obtained
            /// from the stringbuffer, and returned.
            /// 
            /// </para>
            /// </summary>
            /// <returns> a String representation of this map. </returns>
            public override string ToString()
			{
				StringBuilder buf = new StringBuilder();
				foreach (var entry in this)
				{
					var key = entry.Key;
					var value = entry.Value;
					buf.Append(key).Append('=').Append(value).Append('\x0001');
				}
				return buf.ToString();
			}
		}
	}

}