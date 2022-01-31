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
	///         &lt;element ref="{}th" maxOccurs="unbounded" minOccurs="0"/>
	///         &lt;element ref="{}td" maxOccurs="unbounded" minOccurs="0"/>
	///       &lt;/sequence>
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "tr")]
	public class Tr : IFindable
	{
		/// <summary>
		/// Gets the value of the th property.
		/// <p/>
		/// <p/>
		/// For example, to add a new item, do as follows:
		/// <pre>
		///    Th.Add(newItem);
		/// </pre>
		/// <p/>
		/// <p/>
		/// <p/>
		/// Objects of the following type(s) are allowed in the list
		/// <seealso cref="Entities.Th "/>
		/// </summary>
		[XmlElement(ElementName = "th")]
		public List<Th> Th { get; } = new List<Th>();

		/// <summary>
		/// Gets the value of the td property.
		/// <p/>
		/// <p/>
		/// For example, to add a new item, do as follows:
		/// <pre>
		///    Td.Add(newItem);
		/// </pre>
		/// <p/>
		/// <p/>
		/// <p/>
		/// Objects of the following type(s) are allowed in the list
		/// <seealso cref="Entities.Td "/>
		/// </summary>
		[XmlElement(ElementName = "td")]
		public List<Td> Td { get; } = new List<Td>();

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

			if (!(o is Tr tr))
			{
				return false;
			}

			if (!Td?.ListEquals(tr.Td) ?? tr.Td != null)
			{
				return false;
			}

			if (!Th?.ListEquals(tr.Th) ?? tr.Th != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = Th != null ? Th.GetHashCode() : 0;
			result = 31 * result + (Td != null ? Td.GetHashCode() : 0);
			return result;
		}

		public override string ToString()
		{
			return "Tr{" + "th=" + Th + ", td=" + Td + '}';
		}
	}
}