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
	///         &lt;element ref="{}comment"/>
	///         &lt;element ref="{}group"/>
	///         &lt;element ref="{}block"/>
	///       &lt;/choice>
	///       &lt;attribute name="nofield" type="{http://www.w3.org/2001/XMLSchema}short" />
	///       &lt;attribute name="startfield" type="{http://www.w3.org/2001/XMLSchema}short" />
	///       &lt;attribute name="tag" type="{http://www.w3.org/2001/XMLSchema}short" />
	///       &lt;attribute name="name" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="req" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="condreq" type="{http://www.w3.org/2001/XMLSchema}string" />
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "group")]
	public class Group : IFindable
	{
		/// <summary>
		/// Gets or sets the value of the condreq property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>

		[XmlAttribute]
		public string Condreq { get; set; }

		/// <summary>
		/// Gets the value of the content property.
		/// <p/>
		/// <p/>
		/// For example, to add a new item, do as follows:
		/// <pre>
		///    GetContent().Add(newItem);
		/// </pre>
		/// <p/>
		/// <p/>
		/// <p/>
		/// Objects of the following type(s) are allowed in the list
		/// <seealso cref="System.Reflection.FieldInfo "/>
		/// <seealso cref="Comment "/>
		/// <seealso cref="string "/>
		/// <seealso cref="Block "/>
		/// <seealso cref="Group "/>
		/// </summary>
		[XmlElement(typeof(Group), ElementName = "group")]
		[XmlElement(typeof(Field), ElementName = "field")]
		[XmlElement(typeof(Block), ElementName = "block")]
		public List<object> Content { get; } = new List<object>();

		/// <summary>
		/// Gets or sets the value of the name property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="string "/>
		/// </value>
		[XmlAttribute("name")]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the value of the nofield property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="int "/>
		/// </value>
		[XmlAttribute("nofield")]
		public int Nofield { get; set; }

		/// <summary>
		/// Gets or sets the value of the req property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlAttribute("req")]
		public string Req { get; set; }

		/// <summary>
		/// Gets or sets the value of the startfield property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="int "/>
		/// </value>
		[XmlAttribute("startfield")]
		public int Startfield { get; set; }

		/// <summary>
		/// Gets or sets the value of the tag property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="int "/>
		/// </value>
		[XmlAttribute("tag")]
		public int Tag { get; set; }

		public virtual int CompareTo(object o)
		{
			if (!(o is Group grp))
			{
				return -1;
			}

			return Nofield - grp.Nofield;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is Group grp))
			{
				return false;
			}

			if (!Condreq?.Equals(grp.Condreq) ?? grp.Condreq != null)
			{
				return false;
			}

			if (!Content?.ListEquals(grp.Content) ?? grp.Content != null)
			{
				return false;
			}

			if (!Name?.Equals(grp.Name) ?? grp.Name != null)
			{
				return false;
			}

			if (Nofield != grp.Nofield)
			{
				return false;
			}

			if (!Req?.Equals(grp.Req) ?? grp.Req != null)
			{
				return false;
			}

			if (Startfield != grp.Startfield)
			{
				return false;
			}

			if (Tag != grp.Tag)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = Content?.GetHashCode() ?? 0;
			result = 31 * result + Nofield.GetHashCode();
			result = 31 * result + Startfield.GetHashCode();
			result = 31 * result + Tag.GetHashCode();
			result = 31 * result + Name?.GetHashCode() ?? 0;
			result = 31 * result + Req?.GetHashCode() ?? 0;
			result = 31 * result + Condreq?.GetHashCode() ?? 0;
			return result;
		}

		public override string ToString()
		{
			return "Group{" + "content=" + Content + ", nofield=" + Nofield + ", startfield=" + Startfield + ", tag=" +
					Tag + ", name='" + Name + '\'' + ", req='" + Req + '\'' + ", condreq='" + Condreq + '\'' + '}';
		}
	}
}