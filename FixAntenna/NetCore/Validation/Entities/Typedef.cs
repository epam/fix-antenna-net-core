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
	///   &lt;complexContent>
	///     &lt;restriction base="{http://www.w3.org/2001/XMLSchema}anyType">
	///       &lt;sequence>
	///         &lt;element ref="{}descr"/>
	///       &lt;/sequence>
	///       &lt;attribute name="type" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="extends" type="{http://www.w3.org/2001/XMLSchema}string" />
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "typedef")]
	public class Typedef : IFindable
	{
		/// <summary>
		/// Gets or sets the value of the descr property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="Entities.Descr "/> </returns>
		[XmlElement(ElementName = "descr", IsNullable = false)]
		public Descr Descr { get; set; }

		/// <summary>
		/// Gets or sets the value of the extends property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlAttribute(AttributeName = "extends")]
		public string Extends { get; set; }

		/// <summary>
		/// Gets or sets the value of the type property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlAttribute("type")]
		public string Type { get; set; }

		public virtual int CompareTo(object o)
		{
			var typedef = (Typedef)o;
			if (typedef == null)
			{
				return -1;
			}

			return string.Compare(Type, typedef.Type, StringComparison.Ordinal);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is Typedef typedef))
			{
				return false;
			}

			if (!Extends?.Equals(typedef.Extends) ?? typedef.Extends != null)
			{
				return false;
			}

			if (!Descr?.Equals(typedef.Descr) ?? typedef.Descr != null)
			{
				return false;
			}

			if (!Type?.Equals(typedef.Type) ?? typedef.Type != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = Descr?.GetHashCode() ?? 0;
			result = 31 * result + Type?.GetHashCode() ?? 0;
			result = 31 * result + Extends?.GetHashCode() ?? 0;
			return result;
		}

		public override string ToString()
		{
			return "Typedef{" + "descr=" + Descr + ", type='" + Type + '\'' + ", _extends='" + Extends + '\'' + '}';
		}
	}
}