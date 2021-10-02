using System.ComponentModel.DataAnnotations.Schema;

namespace CodeUnderTest
{

	[Table("Product", Schema = "Production")]
	public class Product
	{
		public int ProductKey { get; set; }

		public int ProductLineKey { get; set; }
		public string? ProductName { get; set; }
		public decimal? ProductWeight { get; set; }
		public decimal? ShippingWeight { get; set; }
	}
}
