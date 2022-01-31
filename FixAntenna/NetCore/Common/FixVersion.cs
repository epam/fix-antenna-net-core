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

namespace Epam.FixAntenna.NetCore.Common
{
	/// <summary>
	/// FIXVersion type safe enum
	/// </summary>
	public sealed class FixVersion : IComparable
	{
		public static readonly FixVersion Fix40;
		public static readonly FixVersion Fix41;
		public static readonly FixVersion Fix42;
		public static readonly FixVersion Fix43;
		public static readonly FixVersion Fix44;
		public static readonly FixVersion Fix50;
		public static readonly FixVersion Fix50Sp1;
		public static readonly FixVersion Fix50Sp2;
		public static readonly FixVersion Fixt11;
		private static readonly IDictionary<string, FixVersion> Id2FixVersion;
		private static readonly IDictionary<string, FixVersion> MessageVersion2FixVersion;
		private static readonly IDictionary<int, FixVersion> FixtVersion2FixVersion;
		private static readonly IList<FixVersion> Versions;

		static FixVersion()
		{
			Id2FixVersion = new Dictionary<string, FixVersion>(10);
			MessageVersion2FixVersion = new Dictionary<string, FixVersion>(10);
			FixtVersion2FixVersion = new Dictionary<int, FixVersion>(10);
			Versions = new List<FixVersion>();
			Fix40 = new FixVersion("FIX40", "FIX.4.0", 2);
			Fix41 = new FixVersion("FIX41", "FIX.4.1", 3);
			Fix42 = new FixVersion("FIX42", "FIX.4.2", 4);
			Fix43 = new FixVersion("FIX43", "FIX.4.3", 5);
			Fix44 = new FixVersion("FIX44", "FIX.4.4", 6);
			Fix50 = new FixVersion("FIX50", "FIX.5.0", 7);
			Fix50Sp1 = new FixVersion("FIX50SP1", "FIX.5.0SP1", 8);
			Fix50Sp2 = new FixVersion("FIX50SP2", "FIX.5.0SP2", 9);
			Fixt11 = new FixVersion("FIXT11", "FIXT.1.1", -1);
		}

		/// <summary>
		/// Creates the <c>FIXVersion</c>.
		/// </summary>
		/// <param name="id"> FIX version ID </param>
		/// <param name="messageVersion"> the message fix version </param>
		/// <param name="fixVersion"> the number of fix version </param>
		private FixVersion(string id, string messageVersion, int fixVersion)
		{
			Id = id;
			MessageVersion = messageVersion;
			FixtVersion = fixVersion;
			Id2FixVersion[id] = this;
			MessageVersion2FixVersion[messageVersion] = this;
			FixtVersion2FixVersion[FixtVersion] = this;
			Versions.Add(this);
		}

		/// <summary>
		/// Gets supported fix versions.
		/// </summary>
		/// <returns> supported fix version </returns>
		public static IEnumerator<FixVersion> FixVersionEnum => new List<FixVersion>(Versions).GetEnumerator();

		/// <summary>
		/// Checks if this version is FIXT.
		/// </summary>
		/// <returns> true if is, otherwise false </returns>
		public bool IsFixt => MessageVersion.StartsWith("FIXT", StringComparison.Ordinal);

		/// <summary>
		/// Gets the fix version number code.
		/// </summary>
		/// <returns> code of fix version
		///  </returns>
		public int FixtVersion { get; }

		public int CompareTo(object o)
		{
			return CompareTo((FixVersion)o);
		}

		/// <summary>
		/// Returns ID e.g.: FIX40, FIX41
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Returns the message version representation e.g.: FIX.4.0, FIX.4.1
		/// </summary>
		/// <value> messageVersion </value>
		public string MessageVersion { get; }

		/// <summary>
		/// Returns the FIXVersion by messageVersion string (e.g.: FIX.4.0, FIX.4.1)
		/// </summary>
		/// <param name="messageVersion"> message version </param>
		/// <param name="fixtVersion"> FIXT version </param>
		/// <returns> FIXVersion </returns>
		public static FixVersion CreateInstanceByMessageVersion(string messageVersion, int fixtVersion)
		{
			FixVersion fixVersion;
			if (!MessageVersion2FixVersion.ContainsKey(messageVersion))
			{
				fixVersion = new FixVersion(messageVersion, messageVersion, fixtVersion);
				MessageVersion2FixVersion[messageVersion] = fixVersion;
			}
			else
			{
				fixVersion = MessageVersion2FixVersion[messageVersion];
			}

			return fixVersion;
		}

		public static FixVersion ValueOf(string id)
		{
			return Id2FixVersion.GetValueOrDefault(id);
		}

		/// <summary>
		/// Gets <c>FixVersion</c> instance be string
		/// representation of fix session, FIX.4.0 - FIXT.1.1.
		/// </summary>
		/// <param name="messageVersion"> the message version </param>
		public static FixVersion GetInstanceByMessageVersion(string messageVersion)
		{
			if (!MessageVersion2FixVersion.ContainsKey(messageVersion))
			{
				throw new ArgumentException("Invalid version");
			}

			return MessageVersion2FixVersion[messageVersion];
		}

		/// <summary>
		/// Gets fix version by number.
		/// </summary>
		/// <param name="fixNumber"> the ranges value for this
		///  parameter should be from 2 to 9(FIX.4.0 ... FIX.5.0 sp2), the value for FIXT.1.1 should be -1.
		/// </param>
		/// <exception cref="ArgumentException"> if fix session not exists </exception>
		public static FixVersion GetInstanceByFixtVersion(int fixNumber)
		{
			if (!FixtVersion2FixVersion.ContainsKey(fixNumber))
			{
				throw new ArgumentException("Invalid version");
			}

			return FixtVersion2FixVersion[fixNumber];
		}

		public override string ToString()
		{
			return MessageVersion;
		}

		/// <summary>
		/// Compares this object with the specified object for order.
		/// </summary>
		/// <param name="fixVersion"> the fix version to compare
		/// </param>
		/// <returns> a negative integer, zero, or a positive integer as this object
		///		is less than, equal to, or greater than the specified object.
		///  </returns>
		public int CompareTo(FixVersion fixVersion)
		{
			//[-] Fixed 14837: Tag 369 should be include in message automatically if includeLastProcessed=true
			var result = Convert.ToInt32(FixtVersion).CompareTo(fixVersion.FixtVersion);
			if (result == 0)
			{
				return result;
			}

			if (IsFixt)
			{
				return 1;
			}

			if (fixVersion.IsFixt)
			{
				return -1;
			}

			return result;
		}
	}
}