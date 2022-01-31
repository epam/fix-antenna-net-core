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
	///         &lt;element ref="{}blockref" minOccurs="0"/>
	///       &lt;/choice>
	///       &lt;attribute name="val" type="{http://www.w3.org/2001/XMLSchema}string" />
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "item")]
	public class Item : IFindable
	{
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
		/// <seealso cref="Blockref "/>
		/// <seealso cref="Msgref "/>
		/// <seealso cref="Fieldref "/>
		/// </summary>
		[XmlText(typeof(string))]
		public List<object> Content { get; } = new List<object>();

		/// <summary>
		/// Gets or sets the value of the id property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>

		[XmlAttribute("id")]
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets the value of the val property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>

		[XmlAttribute("val")]
		public string Val { get; set; }

		public virtual int CompareTo(object o)
		{
			if (!(o is Item item))
			{
				return -1;
			}

			return string.Compare(Val, item.Val, StringComparison.Ordinal);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (o == null || GetType() != o.GetType())
			{
				return false;
			}

			var item = (Item)o;

			if (!Content?.ListEquals(item.Content) ?? item.Content != null)
			{
				return false;
			}

			if (!Id?.Equals(item.Id) ?? item.Id != null)
			{
				return false;
			}

			if (!Val?.Equals(item.Val) ?? item.Val != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = Content?.GetHashCode() ?? 0;
			result = 31 * result + Val?.GetHashCode() ?? 0;
			result = 31 * result + Id?.GetHashCode() ?? 0;
			return result;
		}

		public override string ToString()
		{
			return "Item{" +
					"content=" + Content +
					", val='" + Val + '\'' +
					", id='" + Id + '\'' +
					'}';
		}
	}
}