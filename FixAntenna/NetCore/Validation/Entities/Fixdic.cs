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
	///         &lt;element ref="{}typelist"/>
	///         &lt;element ref="{}fielddic"/>
	///         &lt;element ref="{}msgdic"/>
	///       &lt;/sequence>
	///       &lt;attribute name="fixversion" type="{http://www.w3.org/2001/XMLSchema}float" />
	///       &lt;attribute name="title" type="{http://www.w3.org/2001/XMLSchema}string" />
	///       &lt;attribute name="date" type="{http://www.w3.org/2001/XMLSchema}string" />
	///     &lt;/restriction>
	///   &lt;/complexContent>
	/// &lt;/complexType>
	/// </pre>
	/// </para>
	/// </summary>
	[Serializable]
	[XmlType(TypeName = "fixdic")]
	public class Fixdic : IType
	{
		/// <summary>
		/// Gets or sets the value of the date property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>

		[XmlAttribute(AttributeName = "date")]
		public string Date { get; set; }

		/// <summary>
		/// Gets or sets the value of the typelist property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="Entities.Typelist "/> </returns>
		[XmlElement(ElementName = "typelist", IsNullable = false)]
		public Typelist Typelist { get; set; }

		/// <summary>
		/// Gets or sets the value of the fielddic property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="Entities.Fielddic "/>
		/// </value>
		[XmlElement(ElementName = "fielddic", IsNullable = false)]
		public Fielddic Fielddic { get; set; }

		/// <summary>
		/// Gets or sets the value of the fixversion property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="float "/> </returns>
		[XmlAttribute("fixversion")]
		public string Fixversion { get; set; }

		[XmlAttribute("id")]
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets the value of the msgdic property.
		/// </summary>
		/// <value>
		///   possible object is
		///   <seealso cref="Entities.Msgdic "/>
		/// </value>

		[XmlElement(ElementName = "msgdic", IsNullable = false)]
		public Msgdic Msgdic { get; set; }

		/// <summary>
		/// Gets or sets the value of the title property.
		/// </summary>
		/// <returns> possible object is
		///         <seealso cref="string "/> </returns>

		[XmlAttribute("title")]
		public string Title { get; set; }

		public virtual int CompareTo(object o)
		{
			var fixdic = (Fixdic)o;
			if (fixdic == null)
			{
				return -1;
			}

			return string.Compare(Fixversion, fixdic.Fixversion, StringComparison.Ordinal);
		}

		public virtual object Clone()
		{
			var fixdic = new Fixdic();
			CloneFields(fixdic);
			return fixdic;
		}

		protected void CloneFields(Fixdic fixdic)
		{
			fixdic.Date = Date;
			fixdic.Title = Title;
			fixdic.Fixversion = Fixversion;

			fixdic.Msgdic = new Msgdic();
			fixdic.Msgdic.Blockdef.AddRange(Msgdic.Blockdef);
			fixdic.Msgdic.Msgdef.AddRange(Msgdic.Msgdef);

			fixdic.Typelist = new Typelist();
			if (Typelist != null)
			{
				fixdic.Typelist.Typedef.AddRange(Typelist.Typedef);
			}

			fixdic.Fielddic = new Fielddic();
			if (Fielddic?.Fielddef != null)
			{
				fixdic.Fielddic.Fielddef.AddRange(Fielddic.Fielddef);
			}

			if (Fielddic?.Valblockdef != null)
			{
				fixdic.Fielddic.Valblockdef.AddRange(Fielddic.Valblockdef);
			}
		}

		/// <summary>
		/// Is this dictionary is transport dictionary.
		/// </summary>
		/// <value> always return true for this instance. </value>
		public virtual bool IsFixtDictionary => false;

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (!(o is Fixdic fixdic))
			{
				return false;
			}

			if (!Date?.Equals(fixdic.Date) ?? fixdic.Date != null)
			{
				return false;
			}

			if (!Fielddic?.Equals(fixdic.Fielddic) ?? fixdic.Fielddic != null)
			{
				return false;
			}

			if (!Fixversion?.Equals(fixdic.Fixversion) ?? fixdic.Fixversion != null)
			{
				return false;
			}

			if (!Msgdic?.Equals(fixdic.Msgdic) ?? fixdic.Msgdic != null)
			{
				return false;
			}

			if (!Title?.Equals(fixdic.Title) ?? fixdic.Title != null)
			{
				return false;
			}

			if (!Typelist?.Equals(fixdic.Typelist) ?? fixdic.Typelist != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = Typelist?.GetHashCode() ?? 0;
			result = 31 * result + Fielddic?.GetHashCode() ?? 0;
			result = 31 * result + Msgdic?.GetHashCode() ?? 0;
			result = 31 * result + Fixversion?.GetHashCode() ?? 0;
			result = 31 * result + Title?.GetHashCode() ?? 0;
			result = 31 * result + Date?.GetHashCode() ?? 0;
			return result;
		}

		public override string ToString()
		{
			return "Fixdic{" + "typelist=" + Typelist + ", fielddic=" + Fielddic + ", msgdic=" + Msgdic +
					", fixversion='" + Fixversion + '\'' + ", title='" + Title + '\'' + ", date='" + Date + '\'' + '}';
		}
	}
}