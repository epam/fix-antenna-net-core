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
using System.Xml.Serialization;
using Epam.FixAntenna.NetCore.Common.Xml;
using Epam.FixAntenna.NetCore.Helpers;

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
	///   &lt;complexContent>
	///     &lt;restriction base="{http://www.w3.org/2001/XMLSchema}anyType">
	///       &lt;choice maxOccurs="unbounded" minOccurs="0">
	///         &lt;element ref="{}fieldref"/>
	///         &lt;element ref="{}msgref"/>
	///         &lt;element ref="{}ul"/>
	///       &lt;/choice>
	///       &lt;attribute name="colspan" type="{http://www.w3.org/2001/XMLSchema}byte" />
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "td")]
	public class Td : IFindable
	{
		/// <summary>
		/// Gets or sets the value of the colspan property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="byte "/> </returns>
		[XmlAttribute("colspan")]
		public byte Colspan { get; set; }

		/// <summary>
		/// Gets the value of the content property.
		/// <p/>
		/// <p/>
		/// For example, to add a new item, do as follows:
		/// <pre>
		///    Content.Add(newItem);
		/// </pre>
		/// <p/>
		/// <p/>
		/// <p/>
		/// Objects of the following type(s) are allowed in the list
		/// <seealso cref="string "/>
		/// <seealso cref="Ul "/>
		/// <seealso cref="Msgref "/>
		/// <seealso cref="Fieldref "/>
		/// </summary>
		[XmlAnyElement("content")]
		public List<object> Content { get; } = new List<object>();

		public virtual int CompareTo(object o)
		{
			return 0;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is Td td))
			{
				return false;
			}

			if (Colspan != td.Colspan)
			{
				return false;
			}

			if (!Content?.ListEquals(td.Content) ?? td.Content != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = Content != null ? Content.GetHashCode() : 0;
			result = 31 * result + Colspan.GetHashCode();
			return result;
		}

		public override string ToString()
		{
			return "Td{" + "content=" + Content + ", colspan=" + Colspan + '}';
		}
	}
}