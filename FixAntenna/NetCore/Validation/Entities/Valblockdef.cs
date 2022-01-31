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
	///         &lt;element ref="{}item"/>
	///         &lt;element ref="{}range"/>
	///         &lt;element ref="{}descr" minOccurs="0"/>
	///       &lt;/choice>
	///       &lt;attribute name="id" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="name" type="{http://www.w3.org/2001/XMLSchema}string" />
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "valblockdef")]
	public class Valblockdef : IFindable
	{
		/// <summary>
		/// Gets or stes the value of the id property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlAttribute("id")]
		public string Id { get; set; }

		/// <summary>
		/// Gets the value of the itemOrRangeOrDescr property.
		/// <p/>
		/// <p/>
		/// For example, to add a new item, do as follows:
		/// <pre>
		///    ItemOrRangeOrDescr.Add(newItem);
		/// </pre>
		/// <p/>
		/// <p/>
		/// <p/>
		/// Objects of the following type(s) are allowed in the list
		/// <seealso cref="Range "/>
		/// <seealso cref="Descr "/>
		/// <seealso cref="Item "/>
		/// </summary>
		[XmlElement(typeof(Range), ElementName = "range")]
		[XmlElement(typeof(Multi), ElementName = "multi")]
		[XmlElement(typeof(Item), ElementName = "item")]
		[XmlElement(typeof(Descr), ElementName = "descr")]
		public List<object> ItemOrRangeOrDescr { get; } = new List<object>();

		/// <summary>
		/// Gets or sets the value of the name property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="string "/>
		/// </value>
		[XmlAttribute("name")]
		public string Name { get; set; }

		public virtual int CompareTo(object o)
		{
			var valblock = (Valblockdef)o;
			if (valblock == null)
			{
				return -1;
			}

			return string.Compare(Id, valblock.Id, StringComparison.Ordinal);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is Valblockdef valblockdef))
			{
				return false;
			}

			if (!Id?.Equals(valblockdef.Id) ?? valblockdef.Id != null)
			{
				return false;
			}

			if (!ItemOrRangeOrDescr?.ListEquals(valblockdef.ItemOrRangeOrDescr) ?? valblockdef.ItemOrRangeOrDescr != null)
			{
				return false;
			}

			if (!Name?.Equals(valblockdef.Name) ?? valblockdef.Name != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = ItemOrRangeOrDescr?.GetHashCode() ?? 0;
			result = 31 * result + Id?.GetHashCode() ?? 0;
			result = 31 * result + Name?.GetHashCode() ?? 0;
			return result;
		}

		public override string ToString()
		{
			return "Valblockdef{" + "itemOrRangeOrDescr=" + ItemOrRangeOrDescr + ", id='" + Id + '\'' + ", name='" +
					Name + '\'' + '}';
		}
	}
}