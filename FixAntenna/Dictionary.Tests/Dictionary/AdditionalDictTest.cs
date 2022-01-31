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

using System.Collections.Generic;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.Configuration;
using Epam.FixAntenna.NetCore.Dictionary;
using Epam.FixAntenna.NetCore.Validation.Entities;
using NUnit.Framework;

namespace Epam.FixAntenna.Fix.Dictionary
{
	public class AdditionalDictTest
	{
		/// <summary>
		/// Bug 16851 - Conditionally required rule should not be used for message validation if the new rule loaded
		/// from additional dictionary
		/// </summary>
		// No additional support yet.
		[Test]
		public virtual void RemoveCondRequired()
		{
			var builder = new DictionaryBuilder();
			var fix40 = new FixVersionContainer("myfix40", FixVersion.Fix40,
				"Com/Dictionary/base40.xml", "Com/Dictionary/noConditional.xml");
			var fixdic = (Fixdic)builder.BuildDictionary(fix40, false);

			var testField = FindMsgTag(fixdic, "0", 1);
			Assert.IsNull(testField.Condreq, "condReq attribute wasn't overridden");
			Assert.IsNull(testField.Name, "name attribute should be cleaned by additional dict");
			Assert.AreEqual("N", testField.Req, "req attribute is different");
		}

		private Field FindMsgTag(Fixdic fixdic, string msgType, int tagId)
		{
			var msgdef = FindMsgDef(fixdic, msgType);
			if (msgdef == null)
			{
				return null;
			}

			IList<object> fieldOrDescrOrAlias = msgdef.FieldOrDescrOrAlias;
			foreach (var obj in fieldOrDescrOrAlias)
			{
				if (obj is Field)
				{
					var field = (Field)obj;
					if (field.Tag == tagId)
					{
						return field;
					}
				}
			}

			return null;
		}

		private Msgdef FindMsgDef(Fixdic fixdic, string msgType)
		{
			IList<Msgdef> msgdefs = fixdic.Msgdic.Msgdef;
			foreach (var msgdef in msgdefs)
			{
				if (msgType.Equals(msgdef.Msgtype))
				{
					return msgdef;
				}
			}

			return null;
		}
	}
}