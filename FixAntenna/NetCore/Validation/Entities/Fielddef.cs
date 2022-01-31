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
	///       &lt;sequence>
	///         &lt;element ref="{}item" maxOccurs="unbounded" minOccurs="0"/>
	///         &lt;element ref="{}valblock" minOccurs="0"/>
	///         &lt;element ref="{}multi" minOccurs="0"/>
	///         &lt;element ref="{}alias" minOccurs="0"/>
	///         &lt;element ref="{}range" minOccurs="0"/>
	///         &lt;element ref="{}descr"/>
	///       &lt;/sequence>
	///       &lt;attribute name="tag" type="{http://www.w3.org/2001/XMLSchema}short" />
	///       &lt;attribute name="name" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="type" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="lenfield" type="{http://www.w3.org/2001/XMLSchema}short" />
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "fielddef")]
	public class Fielddef : IFindable
	{
		/// <summary>
		/// Gets the value of the item property.
		/// <p/>
		/// <p/>
		/// For example, to add a new item, do as follows:
		/// <pre>
		///    Item.Add(newItem);
		/// </pre>
		/// <p/>
		/// <p/>
		/// <p/>
		/// Objects of the following type(s) are allowed in the list
		/// <seealso cref="Entities.Item "/>
		/// </summary>
		[XmlElement(ElementName = "item")]
		public List<Item> Item { get; } = new List<Item>();

		/// <summary>
		/// Gets or sets the value of the valblock property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="Entities.Valblock "/> </returns>
		[XmlElement(ElementName = "valblock")]
		public Valblock Valblock { get; set; }

		/// <summary>
		/// Gets or sets the value of the multi property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="Entities.Multi "/> </returns>
		[XmlElement(ElementName = "multi")]
		public Multi Multi { get; set; }

		/// <summary>
		/// Gets or sets the value of the alias property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlElement(ElementName = "alias")]
		public string Alias { get; set; }

		/// <summary>
		/// Gets or sets the value of the range property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="Entities.Range "/> </returns>
		[XmlElement(ElementName = "range")]
		public Range Range { get; set; }

		/// <summary>
		/// Gets or sets the value of the descr property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="Entities.Descr "/> </returns>
		[XmlElement(ElementName = "descr", IsNullable = false)]
		public Descr Descr { get; set; }

		/// <summary>
		/// Gets or sets the value of the lenfield property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="int "/> </returns>
		[XmlAttribute("lenfield")]
		public int Lenfield { get; set; }

		/// <summary>
		/// Gets or sets the value of the name property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlAttribute("name")]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the value of the tag property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="int "/> </returns>
		[XmlAttribute("tag")]
		public int Tag { get; set; }

		/// <summary>
		/// Gets or sets the value of the type property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlAttribute("type")]
		public string Type { get; set; }

		public virtual int CompareTo(object o)
		{
			var fielddef = (Fielddef)o;
			if (fielddef == null)
			{
				return -1;
			}

			return Tag - fielddef.Tag;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is Fielddef fielddef))
			{
				return false;
			}

			if (!Alias?.Equals(fielddef.Alias) ?? fielddef.Alias != null)
			{
				return false;
			}

			if (!Descr?.Equals(fielddef.Descr) ?? fielddef.Descr != null)
			{
				return false;
			}

			if (!Item?.ListEquals(fielddef.Item) ?? fielddef.Item != null)
			{
				return false;
			}

			if (Lenfield != fielddef.Lenfield)
			{
				return false;
			}

			if (!Multi?.Equals(fielddef.Multi) ?? fielddef.Multi != null)
			{
				return false;
			}

			if (!Name?.Equals(fielddef.Name) ?? fielddef.Name != null)
			{
				return false;
			}

			if (!Range?.Equals(fielddef.Range) ?? fielddef.Range != null)
			{
				return false;
			}

			if (Tag != fielddef.Tag)
			{
				return false;
			}

			if (!Type?.Equals(fielddef.Type) ?? fielddef.Type != null)
			{
				return false;
			}

			if (!Valblock?.Equals(fielddef.Valblock) ?? fielddef.Valblock != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = Item?.GetHashCode() ?? 0;
			result = 31 * result + Valblock?.GetHashCode() ?? 0;
			result = 31 * result + Multi?.GetHashCode() ?? 0;
			result = 31 * result + Alias?.GetHashCode() ?? 0;
			result = 31 * result + Range?.GetHashCode() ?? 0;
			result = 31 * result + Descr?.GetHashCode() ?? 0;
			result = 31 * result + Tag.GetHashCode();
			result = 31 * result + Name?.GetHashCode() ?? 0;
			result = 31 * result + Type?.GetHashCode() ?? 0;
			result = 31 * result + Lenfield.GetHashCode();
			return result;
		}

		public override string ToString()
		{
			return "Fielddef{" + "item=" + Item + ", valblock=" + Valblock + ", multi=" + Multi + ", alias='" + Alias +
					'\'' + ", range=" + Range + ", descr=" + Descr + ", tag=" + Tag + ", name='" + Name + '\'' +
					", type='" + Type + '\'' + ", lenfield=" + Lenfield + '}';
		}
	}
}