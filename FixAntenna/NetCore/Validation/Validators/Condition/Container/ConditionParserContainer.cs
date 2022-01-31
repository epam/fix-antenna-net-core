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

namespace Epam.FixAntenna.NetCore.Validation.Validators.Condition.Container
{
	internal sealed class ConditionParserContainer
	{
		private int _carriage;
		private string _condition;

		public ConditionParserContainer(string condition, int carriage)
		{
			_carriage = carriage;
			_condition = condition;
		}

		public string GetCondition()
		{
			return _condition;
		}

		public void SetCondition(string condition)
		{
			_condition = condition;
		}

		public int GetCarriage()
		{
			return _carriage;
		}

		public void SetCarriage(int carriage)
		{
			_carriage = carriage;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}

			if (o == null || GetType() != o.GetType())
			{
				return false;
			}

			var that = (ConditionParserContainer)o;

			if (_carriage != that._carriage)
			{
				return false;
			}

			if (_condition != null ? !_condition.Equals(that._condition) : that._condition != null)
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			var result = _condition != null ? _condition.GetHashCode() : 0;
			result = 31 * result + _carriage;
			return result;
		}
	}
}