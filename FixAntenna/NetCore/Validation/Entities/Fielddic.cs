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
	///         &lt;element ref="{}valblockdef" maxOccurs="unbounded" minOccurs="0"/>
	///         &lt;element ref="{}fielddef" maxOccurs="unbounded" minOccurs="0"/>
	///       &lt;/sequence>
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "fielddic")]
	public class Fielddic : IFindable
	{
		/// <summary>
		/// Gets the value of the valblockdef property.
		/// <p/>
		/// <p/>
		/// For example, to add a new item, do as follows:
		/// <pre>
		///    Valblockdef.Add(newItem);
		/// </pre>
		/// <p/>
		/// <p/>
		/// <p/>
		/// Objects of the following type(s) are allowed in the list
		/// <seealso cref="Entities.Valblockdef "/>
		/// </summary>
		[XmlElement(ElementName = "valblockdef")]
		public List<Valblockdef> Valblockdef { get; } = new List<Valblockdef>();

		/// <summary>
		/// Gets the value of the fielddef property.
		/// <p/>
		/// <p/>
		/// For example, to add a new item, do as follows:
		/// <pre>
		///    Fielddef.Add(newItem);
		/// </pre>
		/// <p/>
		/// <p/>
		/// <p/>
		/// Objects of the following type(s) are allowed in the list
		/// <seealso cref="Entities.Fielddef "/>
		/// </summary>
		[XmlElement(ElementName = "fielddef")]
		public List<Fielddef> Fielddef { get; } = new List<Fielddef>();

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

			if (!(o is Fielddic fielddic))
			{
				return false;
			}

			if (!Fielddef?.ListEquals(fielddic.Fielddef) ?? fielddic.Fielddef != null)
			{
				return false;
			}

			if (!Valblockdef?.ListEquals(fielddic.Valblockdef) ?? fielddic.Valblockdef != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = Valblockdef?.GetHashCode() ?? 0;
			result = 31 * result + Fielddef?.GetHashCode() ?? 0;
			return result;
		}

		public override string ToString()
		{
			return "Fielddic{" + "valblockdef=" + Valblockdef + ", fielddef=" + Fielddef + '}';
		}
	}
}