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
using NUnit.Framework; 
using NUnit.Framework.Legacy;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Epam.FixAntenna.TestUtils.Runner
{
	/// <summary>
	/// Specifies that a test should be run until it fails or until a max run attempts is reached
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	internal class RunUntilFailureAttribute : PropertyAttribute, IWrapTestMethod
	{
		private readonly int _maxRunAttempts;

		/// <summary>
		/// Construct a RunUntilFailureAttribute
		/// </summary>
		/// <param name="maxRunAttempts">Max number of attempts to run a test</param>
		public RunUntilFailureAttribute(int maxRunAttempts)
		{
			_maxRunAttempts = maxRunAttempts;
		}

		/// <summary>
		/// The test command for RunUntilFailureAttribute
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		public TestCommand Wrap(TestCommand command)
		{
			return new RunUntilFailureCommand(command, _maxRunAttempts);
		}

		/// <summary>
		/// The test command for RunUntilFailureAttribute
		/// </summary>
		internal class RunUntilFailureCommand : DelegatingTestCommand
		{
			private readonly int _maxRunAttempts;

			/// <summary>
			/// Initializes a new instance of the <see cref="RunUntilFailureCommand"/> class
			/// </summary>
			/// <param name="innerCommand">The inner command</param>
			/// <param name="maxRunAttempts">Max number of runs</param>
			public RunUntilFailureCommand(TestCommand innerCommand, int maxRunAttempts) : base(innerCommand)
			{
				_maxRunAttempts = maxRunAttempts;
			}

			/// <summary>
			/// Runs the test till it fails; result is saved in the supplied TestExecutionContext
			/// </summary>
			/// <param name="context">The context in which the test should run</param>
			/// <returns>A test result</returns>
			public override TestResult Execute(TestExecutionContext context)
			{
				if (context == null)
				{
					throw new ArgumentNullException(nameof(context));
				}

				var attemptNumber = 0;
				do
				{
					context.CurrentResult = innerCommand.Execute(context);
					attemptNumber++;
				} while (context.CurrentResult.ResultState == ResultState.Success && attemptNumber < _maxRunAttempts);

				return context.CurrentResult;
			}
		}
	}
}