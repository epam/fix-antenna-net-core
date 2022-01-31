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
	///         &lt;element ref="{}comment" minOccurs="0"/>
	///       &lt;/sequence>
	///       &lt;attribute name="tag" type="{http://www.w3.org/2001/XMLSchema}short" />
	///       &lt;attribute name="name" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="condreq" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="req" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="idref" type="{http://www.w3.org/2001/XMLSchema}string" />
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "field")]
	public class Field : IFindable
	{
		/// <summary>
		/// Gets or sets the value of the condreq property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlAttribute("condreq")]
		public string Condreq { get; set; }

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
		/// <seealso cref="Comment "/>
		/// <seealso cref="string "/>
		/// </summary>
		[XmlAnyElement("content")]
		public List<object> Content { get; } = new List<object>();

		/// <summary>
		/// Gets or sets the value of the idref property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlAttribute("idref")]
		public string Idref { get; set; }

		/// <summary>
		/// Gets or sets the value of the name property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlAttribute("name")]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the value of the req property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlAttribute("req")]
		public string Req { get; set; }

		/// <summary>
		/// Gets or sets the value of the tag property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="int "/> </returns>
		[XmlAttribute("tag")]
		public int Tag { get; set; }

		public virtual int CompareTo(object o)
		{
			if (!(o is Field field))
			{
				return -1;
			}
			
			return Tag - field.Tag;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is Field field))
			{
				return false;
			}

			if (!Condreq?.Equals(field.Condreq) ?? field.Condreq != null)
			{
				return false;
			}

			if (!Content?.ListEquals(field.Content) ?? field.Content != null)
			{
				return false;
			}

			if (!Idref?.Equals(field.Idref) ?? field.Idref != null)
			{
				return false;
			}

			if (!Name?.Equals(field.Name) ?? field.Name != null)
			{
				return false;
			}

			if (!Req?.Equals(field.Req) ?? field.Req != null)
			{
				return false;
			}

			return Tag == field.Tag;
		}

		public override int GetHashCode()
		{
			var result = Content?.GetHashCode() ?? 0;
			result = 31 * result + Tag.GetHashCode();
			result = 31 * result + Name?.GetHashCode() ?? 0;
			result = 31 * result + Condreq?.GetHashCode() ?? 0;
			result = 31 * result + Req?.GetHashCode() ?? 0;
			result = 31 * result + Idref?.GetHashCode() ?? 0;
			return result;
		}

		public override string ToString()
		{
			return "Field{" + "content=" + Content + ", tag=" + Tag + ", name='" + Name + '\'' + ", condreq='" +
					Condreq + '\'' + ", req='" + Req + '\'' + ", idref='" + Idref + '\'' + '}';
		}
	}
}