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
	///       &lt;attribute name="minval" type="{http://www.w3.org/2001/XMLSchema}float" />
	///       &lt;attribute name="maxval" type="{http://www.w3.org/2001/XMLSchema}float" />
	///       &lt;attribute name="type" type="{http://www.w3.org/2001/XMLSchema}string" />
	///     &lt;/extension>
	///   &lt;/simpleContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "range")]
	public class Range : IFindable
	{
		/// <summary>
		/// Gets or sets the value of the maxval property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="double "/> </returns>

		[XmlAttribute("maxval")]
		public double Maxval { get; set; }

		/// <summary>
		/// Gets or sets the value of the minval property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="double"/> </returns>
		[XmlAttribute("minval")]
		public double Minval { get; set; }

		/// <summary>
		/// Gets or sets the value of the type property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlAttribute("type")]
		public string Type { get; set; }

		/// <summary>
		/// Gets or sets the value of the value property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>
		[XmlText]
		public string Value { get; set; }

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

			if (!(o is Range range))
			{
				return false;
			}

			if (Maxval != range.Maxval)
			{
				return false;
			}

			if (Minval != range.Minval)
			{
				return false;
			}

			if (!Type?.Equals(range.Type) ?? range.Type != null)
			{
				return false;
			}

			if (!Value?.Equals(range.Value) ?? range.Value != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = Value?.GetHashCode() ?? 0;
			result = 31 * result + Minval.GetHashCode();
			result = 31 * result + Maxval.GetHashCode();
			result = 31 * result + Type?.GetHashCode() ?? 0;
			return result;
		}

		public override string ToString()
		{
			return "Range{" + "value='" + Value + '\'' + ", minval=" + Minval + ", maxval=" + Maxval + ", type='" +
					Type + '\'' + '}';
		}
	}
}