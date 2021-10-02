using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace TestingWithDatabases
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class EmployeeClassificationSourceAttribute : Attribute, ITestDataSource
	{
		public IEnumerable<object[]> GetData(MethodInfo methodInfo)
		{
			for (var isExempt = 0; isExempt < 2; isExempt++)
				for (var isEmployee = 0; isEmployee < 2; isEmployee++)
					yield return new object[] { isExempt == 1, isEmployee == 1 };
		}

		public string GetDisplayName(MethodInfo methodInfo, object[] data)
		{
			return $"IsExempt = {data[0]}, IsEmployee = {data[1]}";
		}
	}
}
