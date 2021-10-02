using CodeUnderTest;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Tortuga.Chain;

namespace TestingWithDatabases
{
	[TestClass]
	public class ExampleTests
	{
		static SqlServerDataSource s_PrimaryDataSource;
		static SqlServerDataSource s_SoftDeleteDataSource;
		static ExampleTests()
		{
			var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json").Build();
			var sqlServerConnectionString = configuration.GetSection("ConnectionStrings")["SqlServerTestDatabase"];
			s_PrimaryDataSource = new SqlServerDataSource(sqlServerConnectionString);
			s_SoftDeleteDataSource = s_PrimaryDataSource.WithRules(new Tortuga.Chain.AuditRules.SoftDeleteRule("IsDeleted", true));
		}
		EmployeeClassificationRepository CreateEmployeeClassificationRepository()
		{
			return new EmployeeClassificationRepository(s_SoftDeleteDataSource);
		}

		Task<EmployeeClassification?> GetEmployeeClassificationIgnoringDeletedFlag(int employeeClassificationKey)
		{
			return s_PrimaryDataSource.GetByKey<EmployeeClassification>(employeeClassificationKey).ToObjectOrNull()
				.ExecuteAsync();
		}


		[TestMethod]
		public async Task Example1_Create()
		{
			var repo = CreateEmployeeClassificationRepository();
			var row = new EmployeeClassification()
			{
				EmployeeClassificationName = "Test classification",
			};
			await repo.CreateAsync(row);
		}

		[TestMethod]
		public async Task Example2_Create()
		{
			var repo = CreateEmployeeClassificationRepository();
			var row = new EmployeeClassification()
			{
				EmployeeClassificationName = "Test " + DateTime.Now.Ticks,
			};
			await repo.CreateAsync(row);
		}

		[TestMethod]
		public async Task Example3_Create_And_Read()
		{
			var repo = CreateEmployeeClassificationRepository();
			var row = new EmployeeClassification()
			{
				EmployeeClassificationName = "Test " + DateTime.Now.Ticks,
			};
			var key = await repo.CreateAsync(row);
			Assert.IsTrue(key > 0);

			var echo = await repo.GetByKeyAsync(key);
			Assert.AreEqual(key, echo.EmployeeClassificationKey);
			Assert.AreEqual(row.EmployeeClassificationName, echo.EmployeeClassificationName);
			Assert.AreEqual(row.IsEmployee, echo.IsEmployee);
			Assert.AreEqual(row.IsExempt, echo.IsExempt);
		}

		[TestMethod]
		public async Task Example4_Create_And_Read()
		{
			var repo = CreateEmployeeClassificationRepository();
			var row = new EmployeeClassification()
			{
				EmployeeClassificationName = "Test " + DateTime.Now.Ticks,
			};
			var key = await repo.CreateAsync(row);
			Assert.IsTrue(key != 0, "New key wasn't created or returned");
			row.EmployeeClassificationKey = key;

			var echo = await repo.GetByKeyAsync(key);

			PropertiesAreEqual(row, echo);
		}

		[TestMethod]
		public async Task Example5_Create_And_Update()
		{
			var repo = CreateEmployeeClassificationRepository();
			var version1 = new EmployeeClassification()
			{
				EmployeeClassificationName = "Test " + DateTime.Now.Ticks,
			};
			var key = await repo.CreateAsync(version1);
			Assert.IsTrue(key != 0, "New key wasn't created or returned");
			version1.EmployeeClassificationKey = key;

			var version2 = await repo.GetByKeyAsync(key);
			PropertiesAreEqual(version1, version2, "After created");

			version2.EmployeeClassificationName = "Modified " + DateTime.Now.Ticks;
			await repo.UpdateAsync(version2);

			var version3 = await repo.GetByKeyAsync(key);
			PropertiesAreEqual(version2, version3, "After update");
		}


		[TestMethod]
		public async Task Example6_Create_And_Delete()
		{
			var repo = CreateEmployeeClassificationRepository();
			var version1 = new EmployeeClassification()
			{
				EmployeeClassificationName = "Test " + DateTime.Now.Ticks,
			};
			var key = await repo.CreateAsync(version1);
			Assert.IsTrue(key != 0, "New key wasn't created or returned");
			version1.EmployeeClassificationKey = key;

			var version2 = await repo.GetByKeyOrNullAsync(key);
			Assert.IsNotNull(version2, "Record wasn't created");
			PropertiesAreEqual(version1, version2, "After created");

			await repo.DeleteByKeyAsync(key);

			var version3 = await repo.GetByKeyOrNullAsync(key);
			Assert.IsNull(version3, "Record wasn't deleted");
		}

		[TestMethod]
		public async Task Example7_Create_And_Delete()
		{
			var repo = CreateEmployeeClassificationRepository();
			var version1 = new EmployeeClassification()
			{
				EmployeeClassificationName = "Test " + DateTime.Now.Ticks,
			};
			var key = await repo.CreateAsync(version1);
			Assert.IsTrue(key != 0, "New key wasn't created or returned");
			version1.EmployeeClassificationKey = key;

			var version2 = await repo.GetByKeyAsync(key);
			PropertiesAreEqual(version1, version2, "After created");

			await repo.DeleteByKeyAsync(key);

			try
			{
				await repo.GetByKeyAsync(key);
				Assert.Fail("Expected an exception. Record wasn't deleted");
			}
			catch (MissingDataException)
			{
				//Expected
			}
		}

		[TestMethod]
		public async Task Example8_Create_And_Soft_Delete()
		{
			var repo = CreateEmployeeClassificationRepository();
			var version1 = new EmployeeClassification()
			{
				EmployeeClassificationName = "Test " + DateTime.Now.Ticks,
			};
			var key = await repo.CreateAsync(version1);
			Assert.IsTrue(key != 0, "New key wasn't created or returned");
			version1.EmployeeClassificationKey = key;

			var version2 = await repo.GetByKeyOrNullAsync(key);
			Assert.IsNotNull(version2, "Record wasn't created");
			PropertiesAreEqual(version1, version2, "After created");

			await repo.DeleteByKeyAsync(key);

			var version3 = await repo.GetByKeyOrNullAsync(key);
			Assert.IsNull(version3, "Record wasn't deleted");

			var version4 = await GetEmployeeClassificationIgnoringDeletedFlag(key);
			Assert.IsNotNull(version4, "Record was hard deleted");
			Assert.IsTrue(version4.IsDeleted);
		}

		[TestMethod]
		[DataTestMethod, EmployeeClassificationSource]
		public async Task Example9_Create_And_Read(bool isExempt, bool isEmployee)
		{
			var repo = CreateEmployeeClassificationRepository();
			var row = new EmployeeClassification()
			{
				EmployeeClassificationName = "Test " + DateTime.Now.Ticks,
				IsExempt = isExempt,
				IsEmployee = isEmployee
			};
			var key = await repo.CreateAsync(row);
			Assert.IsTrue(key > 0);
			Debug.WriteLine("EmployeeClassificationName: " + key);

			var echo = await repo.GetByKeyAsync(key);
			Assert.AreEqual(key, echo.EmployeeClassificationKey);
			Assert.AreEqual(row.EmployeeClassificationName, echo.EmployeeClassificationName);
			Assert.AreEqual(row.IsEmployee, echo.IsEmployee);
			Assert.AreEqual(row.IsExempt, echo.IsExempt);
		}

		[TestMethod]
		public async Task Example10_Filtered_Read()
		{
			var repo = CreateEmployeeClassificationRepository();

			var matchingSource = new List<EmployeeClassification>();
			for (var i = 0; i < 10; i++)
			{
				var row = new EmployeeClassification()
				{
					EmployeeClassificationName = "Test " + DateTime.Now.Ticks + "_A" + i,
					IsEmployee = true,
					IsExempt = false
				};
				matchingSource.Add(row);
			}

			var nonMatchingSource = new List<EmployeeClassification>();
			for (var i = 0; i < 10; i++)
			{
				var row = new EmployeeClassification()
				{
					EmployeeClassificationName = "Test " + DateTime.Now.Ticks + "_B" + i,
					IsEmployee = false,
					IsExempt = false
				};
				nonMatchingSource.Add(row);
			}
			for (var i = 0; i < 10; i++)
			{
				var row = new EmployeeClassification()
				{
					EmployeeClassificationName = "Test " + DateTime.Now.Ticks + "_C" + i,
					IsEmployee = true,
					IsExempt = true
				};
				nonMatchingSource.Add(row);
			}
			await repo.CreateBatchAsync(matchingSource);
			await repo.CreateBatchAsync(nonMatchingSource);

			var results = await repo.FindWithFilterAsync(isEmployee: true, isExempt: false);

			foreach (var expected in matchingSource)
				Assert.IsTrue(results.Any(x => x.EmployeeClassificationName == expected.EmployeeClassificationName));

			var nonMatchingRecords = results.Where(x => x.IsEmployee == false || x.IsExempt == true).ToList();
			Assert.IsTrue(nonMatchingRecords.Count == 0,
				$"Found unexpected row(s) with the following keys " +
				string.Join(", ", nonMatchingRecords.Take(10).Select(x => x.EmployeeClassificationKey)));
		}



		static void PropertiesAreEqual(EmployeeClassification expected, EmployeeClassification actual, string? stepName = null)
		{
			Assert.IsNotNull(actual, $"Actual value for step {stepName} is null.");
			Assert.IsNotNull(expected, $"Expected value for step {stepName} is null.");

			using (var scope = new AssertionScope(stepName))
			{
				scope.AreEqual(expected.EmployeeClassificationKey, actual.EmployeeClassificationKey, "EmployeeClassificationKey");
				scope.AreEqual(expected.EmployeeClassificationName, actual.EmployeeClassificationName, "EmployeeClassificationName");
				scope.AreEqual(expected.IsEmployee, actual.IsEmployee, "IsEmployee");
				scope.AreEqual(expected.IsExempt, actual.IsExempt, "IsExempt");
			}
		}
	}
}
