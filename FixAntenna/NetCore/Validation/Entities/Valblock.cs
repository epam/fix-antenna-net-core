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
	///     &lt;extension base="&lt;http://www.w3.org/2001/XMLSchema>string">
	///       &lt;attribute name="idref" type="{http://www.w3.org/2001/XMLSchema}string" />
	///     &lt;/extension>
	///   &lt;/simpleContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "valblock")]
	public class Valblock : IFindable
	{
		[XmlAttribute("idref")]
		public string Idref;

		[XmlText]
		public string Value;

		public virtual int CompareTo(object o)
		{
			var valblock = (Valblock)o;
			if (valblock == null)
			{
				return -1;
			}

			return Idref.CompareTo(valblock.Idref);
		}

		/// <summary>
		/// Gets the value of the value property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		public virtual string GetValue()
		{
			return Value;
		}

		/// <summary>
		/// Sets the value of the value property.
		/// </summary>
		/// <param name="value"> allowed object is
		///              <seealso cref="string "/> </param>
		public virtual void SetValue(string value)
		{
			Value = value;
		}

		/// <summary>
		/// Gets the value of the idref property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		public virtual string GetIdref()
		{
			return Idref;
		}

		/// <summary>
		/// Sets the value of the idref property.
		/// </summary>
		/// <param name="value"> allowed object is
		///              <seealso cref="string "/> </param>
		public virtual void SetIdref(string value)
		{
			Idref = value;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is Valblock))
			{
				return false;
			}

			var valblock = (Valblock)o;

			if (!ReferenceEquals(Idref, null) ? !Idref.Equals(valblock.Idref) : !ReferenceEquals(valblock.Idref, null))
			{
				return false;
			}

			if (!ReferenceEquals(Value, null) ? !Value.Equals(valblock.Value) : !ReferenceEquals(valblock.Value, null))
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = !ReferenceEquals(Value, null) ? Value.GetHashCode() : 0;
			result = 31 * result + (!ReferenceEquals(Idref, null) ? Idref.GetHashCode() : 0);
			return result;
		}

		public override string ToString()
		{
			return "Valblock{" + "value='" + Value + '\'' + ", idref='" + Idref + '\'' + '}';
		}
	}
}