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
	///       &lt;attribute name="idref" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="condreq" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="req" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="tag" type="{http://www.w3.org/2001/XMLSchema}int" />
	///       &lt;attribute name="name" type="{http://www.w3.org/2001/XMLSchema}string" />
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "block")]
	public sealed class Block : IFindable
	{
		private List<object> _content;

		/// <summary>
		/// Gets the value of the content property.
		/// <p/>
		/// <p/>
		/// This accessor method returns a reference to the live list,
		/// not a snapshot. Therefore any modification you make to the
		/// returned list will be present inside the JAXB object.
		/// This is why there is not a <c>Set</c> method for the content property.
		/// <p/>
		/// <p/>
		/// For example, to add a new item, do as follows:
		/// <pre>
		///    getContent().add(newItem);
		/// </pre>
		/// <p/>
		/// <p/>
		/// <p/>
		/// Objects of the following type(s) are allowed in the list
		/// <seealso cref="Comment "/>
		/// <seealso cref="string "/>
		/// </summary>
		//[XmlText]
		[XmlAnyElement("content")]
		public List<object> Content => _content ?? (_content = new List<object>());

		/// <summary>
		/// Gets or sets the value of the idref property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="string "/>
		/// </value>
		[XmlAttribute("idref")]
		public string Idref { get; set; }

		/// <summary>
		/// Gets or sets the value of the condreq property.
		/// </summary>
		/// <value>
		///   possible object is <seealso cref="string "/>
		/// </value>
		[XmlAttribute("condreq")] 
		public string Condreq { get; set; }

		/// <summary>
		/// Gets or sets the value of the req property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="string "/>
		/// </value>
		[XmlAttribute("req")]
		public string Req { get; set; }

		/// <summary>
		/// Gets or sets the value of the tag property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="int"/>
		/// </value>
		[XmlAttribute("tag")] 
		public int Tag { get; set; }

		/// <summary>
		/// Gets or sets the value of the name property.
		/// </summary>
		/// <value>
		///   allowed object is
		///   <seealso cref="string "/>
		/// </value>
		[XmlAttribute("name")]
		public string Name { set; get; }

		public override string ToString()
		{
			return "Block{content=" + Content + ", idref='" + Idref + "', condreq='" + Condreq + "', req='" + Req + "', tag=" + Tag + "', name='" + Name + "'}";
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is Block block))
			{
				return false;
			}

			return !(!Condreq?.Equals(block.Condreq) ?? block.Condreq != null) 
						&& !(!Content?.ListEquals(block.Content) ?? block.Content != null) 
						&& !(!Idref?.Equals(block.Idref) ?? block.Idref != null) 
						&& !(!Name?.Equals(block.Name) ?? block.Name != null) 
						&& !(!Req?.Equals(block.Req) ?? block.Req != null) 
						&& Tag == block.Tag;
		}

		public override int GetHashCode()
		{
			var result = Content.GetHashCode();
			result = 31 * result + (Idref?.GetHashCode() ?? 0);
			result = 31 * result + (Condreq?.GetHashCode() ?? 0);
			result = 31 * result + (Req?.GetHashCode() ?? 0);
			result = 31 * result + Tag.GetHashCode();
			result = 31 * result + (Name?.GetHashCode() ?? 0);
			return result;
		}

		public int CompareTo(object o)
		{
			if (!(o is Block block))
			{
				return -1;
			}

			if (Tag != block.Tag)
			{
				return Tag - block.Tag;
			}

			if (Name != null)
			{
				return string.Compare(Name, block.Name, StringComparison.Ordinal);
			}

			if (Idref != null)
			{
				return string.Compare(Idref, block.Idref, StringComparison.Ordinal);
			}

			return -1;
		}
	}
}