using System.ComponentModel.DataAnnotations.Schema;
using Tortuga.Chain;

namespace CodeUnderTest
{
	[Table("HR.EmployeeClassification")]
	public class EmployeeClassification
	{
		public int EmployeeClassificationKey { get; set; }
		public string? EmployeeClassificationName { get; set; }
		public bool IsEmployee { get; set; }
		public bool IsExempt { get; set; }
		public bool IsDeleted { get; set; }
	}
}
