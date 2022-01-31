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
	///   &lt;simpleContent>
	///     &lt;extension base="&lt;http://www.w3.org/2001/XMLSchema>anyURI">
	///       &lt;attribute name="href" type="{http://www.w3.org/2001/XMLSchema}anyURI" />
	///     &lt;/extension>
	///   &lt;/simpleContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "a")]
	public sealed class A : IFindable
	{
		/// <summary>
		/// Gets or sets the value of the value property.
		/// </summary>
		/// <value>
		/// possible object is <seealso cref="string "/>
		/// </value>
		[XmlText]
		public string Value { get; set; }

		/// <summary>
		/// Gets or sets the value of the href property.
		/// </summary>
		/// <value>
		/// possible object is <seealso cref="string "/>
		/// </value>
		[XmlAttribute("href")]
		public string Href { get; set; }

		public int CompareTo(object obj)
		{
			return 0;
		}

		public override string ToString()
		{
			return "A{" + "value='" + Value + '\'' + ", href='" + Href + '\'' + '}';
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is A a))
			{
				return false;
			}

			return Href.Equals(a.Href) && Value.Equals(a.Value);
		}

		public override int GetHashCode()
		{
			var result = Value.GetHashCode();
			result = 31 * result + Href.GetHashCode();
			return result;
		}
	}
}