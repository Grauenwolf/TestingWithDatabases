using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CodeUnderTest
{
	[Table("ProductLine", Schema = "Production")]
	public class ProductLine
	{
		public int ProductLineKey { get; set; }

		public string? ProductLineName { get; set; }

		public List<Product> Products { get; } = new List<Product>();


		public void ApplyKeys()
		{
			foreach (var item in Products)
				item.ProductLineKey = ProductLineKey;
		}
	}
}
