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
	///         &lt;element ref="{}field"/>
	///         &lt;element ref="{}descr"/>
	///         &lt;element ref="{}alias"/>
	///         &lt;element ref="{}block"/>
	///         &lt;element ref="{}group"/>
	///       &lt;/choice>
	///       &lt;attribute name="msgtype" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="name" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="admin" type="{http://www.w3.org/2001/XMLSchema}string" />
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "msgdef")]
	public class Msgdef : IFindable
	{
		/// <summary>
		/// Gets or sets the value of the admin property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>

		[XmlAttribute]
		public string Admin { get; set; }

		/// <summary>
		/// Gets the value of the fieldOrDescrOrAlias property.
		/// <p/>
		/// <p/>
		/// For example, to add a new item, do as follows:
		/// <pre>
		///    FieldOrDescrOrAlias.Add(newItem);
		/// </pre>
		/// <p/>
		/// <p/>
		/// <p/>
		/// Objects of the following type(s) are allowed in the list
		/// <seealso cref="Descr "/>
		/// <seealso cref="string "/>
		/// <seealso cref="System.Reflection.FieldInfo "/>
		/// <seealso cref="Block "/>
		/// <seealso cref="Group "/>
		/// </summary>
		[XmlElement(typeof(Group), ElementName = "group")]
		[XmlElement(typeof(Field), ElementName = "field")]
		[XmlElement(typeof(Block), ElementName = "block")]
		[XmlElement(typeof(string), ElementName = "alias")]
		public List<object> FieldOrDescrOrAlias { get; } = new List<object>();

		/// <summary>
		/// Gets or sets the value of the msgtype property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="string "/>
		/// </value>

		[XmlAttribute("msgtype")]
		public string Msgtype { get; set; }

		/// <summary>
		/// Gets or sets the value of the name property.
		/// </summary>
		[XmlAttribute("name")]
		public string Name { get; set; }

		public virtual int CompareTo(object o)
		{
			var msgdef = (Msgdef)o;
			if (msgdef == null)
			{
				return -1;
			}

			return string.Compare(Msgtype, msgdef.Msgtype, StringComparison.Ordinal);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is Msgdef msgdef))
			{
				return false;
			}

			if (!Admin?.Equals(msgdef.Admin) ?? msgdef.Admin != null)
			{
				return false;
			}

			if (!FieldOrDescrOrAlias?.ListEquals(msgdef.FieldOrDescrOrAlias) ?? msgdef.FieldOrDescrOrAlias != null)
			{
				return false;
			}

			if (!Msgtype?.Equals(msgdef.Msgtype) ?? msgdef.Msgtype != null)
			{
				return false;
			}

			if (!Name?.Equals(msgdef.Name) ?? msgdef.Name != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = FieldOrDescrOrAlias?.GetHashCode() ?? 0;
			result = 31 * result + Msgtype?.GetHashCode() ?? 0;
			result = 31 * result + Name?.GetHashCode() ?? 0;
			result = 31 * result + Admin?.GetHashCode() ?? 0;
			return result;
		}

		public override string ToString()
		{
			return "Msgdef{" + "fieldOrDescrOrAlias=" + FieldOrDescrOrAlias + ", msgtype='" + Msgtype + '\'' +
					", name='" + Name + '\'' + ", admin='" + Admin + '\'' + '}';
		}
	}
}