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
	///         &lt;element ref="{}p"/>
	///         &lt;element ref="{}ul"/>
	///         &lt;element ref="{}dl"/>
	///         &lt;element ref="{}fieldref"/>
	///         &lt;element ref="{}table"/>
	///         &lt;element ref="{}blockref"/>
	///         &lt;element ref="{}ol"/>
	///         &lt;element ref="{}msgref"/>
	///       &lt;/choice>
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "descr")]
	public class Descr : GenericElement<Descr>
	{
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
		/// <seealso cref="string"/>
		/// <seealso cref="Table"/>
		/// <seealso cref="Dl"/>
		/// <seealso cref="Fieldref"/>
		/// <seealso cref="Blockref"/>
		/// <seealso cref="Ul"/>
		/// <seealso cref="Ol"/>
		/// <seealso cref="Msgref"/>
		/// <seealso cref="P"/>
		/// </summary>

		public override string ToString()
		{
			return "Descr{" + "content=" + Content + '}';
		}
	}
}