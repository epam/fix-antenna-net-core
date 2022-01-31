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
	///         &lt;element ref="{}field"/>
	///         &lt;element ref="{}descr"/>
	///         &lt;element ref="{}group"/>
	///         &lt;element ref="{}block"/>
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
	[XmlType(TypeName = "blockdef")]
	public sealed class Blockdef : IFindable
	{
		/// <summary>
		/// Gets the value of the fieldOrDescrOrGroup property.
		/// <p/>
		/// <p/>
		/// For example, to add a new item, do as follows:
		/// <pre>
		///    FieldOrDescrOrGroup.Add(newItem);
		/// </pre>
		/// <p/>
		/// <p/>
		/// Objects of the following type(s) are allowed in the list
		/// <seealso cref="Group"/>
		/// <seealso cref="Field"/>
		/// <seealso cref="Block"/>
		/// <seealso cref="Descr"/>
		/// </summary>
		[XmlElement(typeof(Group), ElementName = "group")]
		[XmlElement(typeof(Field), ElementName = "field")]
		[XmlElement(typeof(Block), ElementName = "block")]
		[XmlElement(typeof(Descr), ElementName = "descr")]
		public List<object> FieldOrDescrOrGroup { get; } = new List<object>();

		public int CompareTo(object o)
		{
			var blockdef = (Blockdef)o;
			if (blockdef == null)
			{
				return -1;
			}

			return string.Compare(Id, blockdef.Id, StringComparison.Ordinal);
		}

		/// <summary>
		/// Gets or sets the value of the id property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="string "/>
		/// </value>
		[XmlAttribute("id")]
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets the value of the name property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="string "/>
		/// </value>
		[XmlAttribute("name")]
		public string Name { get; set; }

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is Blockdef blockdef))
			{
				return false;
			}

			if (!FieldOrDescrOrGroup?.Equals(blockdef.FieldOrDescrOrGroup) ?? blockdef.FieldOrDescrOrGroup != null)
			{
				return false;
			}

			if (!Id?.Equals(blockdef.Id) ?? blockdef.Id != null)
			{
				return false;
			}

			if (!Name?.Equals(blockdef.Name) ?? blockdef.Name != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = FieldOrDescrOrGroup != null ? FieldOrDescrOrGroup.GetHashCode() : 0;
			result = 31 * result + Id?.GetHashCode() ?? 0;
			result = 31 * result + Name?.GetHashCode() ?? 0;
			return result;
		}

		public override string ToString()
		{
			return "Blockdef{fieldOrDescrOrGroup=" + FieldOrDescrOrGroup + ", id='" + Id + "', name='" + Name + "'}";
		}
	}
}