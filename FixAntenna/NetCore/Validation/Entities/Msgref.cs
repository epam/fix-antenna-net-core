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
using System.Xml.Serialization;
using Epam.FixAntenna.NetCore.Common.Xml;

namespace Epam.FixAntenna.NetCore.Validation.Entities
{
	/// <summary>
	/// <para>Class for anonymous complex type.
	/// <p/>
	/// </para>
	/// <para>The following schema fragment specifies the expected content contained within this class.
	/// <p/>
	/// <pre>
	/// &lt;complexType>
	///   &lt;simpleContent>
	///     &lt;extension base="&lt;http://www.w3.org/2001/XMLSchema>string">
	///       &lt;attribute name="msgtype" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="tag" type="{http://www.w3.org/2001/XMLSchema}short" />
	///     &lt;/extension>
	///   &lt;/simpleContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "msgref")]
	public class Msgref : IFindable
	{
		/// <summary>
		/// Gets or sets the value of the msgtype property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>

		[XmlAttribute("msgtype")]
		public string Msgtype { get; set; }

		/// <summary>
		/// Gets or sets the value of the tag property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="int"/>
		/// </value>

		[XmlAttribute("type")]
		public int Tag { get; set; }

		/// <summary>
		/// Gets or sets the value of the value property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>

		[XmlText]
		public string Value { get; set; }

		public virtual int CompareTo(object o)
		{
			var msgref = (Msgref)o;
			if (msgref == null)
			{
				return -1;
			}

			return string.Compare(Msgtype, msgref.Msgtype, StringComparison.Ordinal);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is Msgref msgref))
			{
				return false;
			}

			if (!Msgtype?.Equals(msgref.Msgtype) ?? msgref.Msgtype != null)
			{
				return false;
			}

			if (Tag != msgref.Tag)
			{
				return false;
			}

			if (!Value?.Equals(msgref.Value) ?? msgref.Value != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = Value?.GetHashCode() ?? 0;
			result = 31 * result + Msgtype?.GetHashCode() ?? 0;
			result = 31 * result + Tag.GetHashCode();
			return result;
		}

		public override string ToString()
		{
			return "Msgref{" + "value='" + Value + '\'' + ", msgtype='" + Msgtype + '\'' + ", tag=" + Tag + '}';
		}
	}
}