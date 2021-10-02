using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tortuga.Chain;

namespace CodeUnderTest
{
	public class ProductLineRepository
	{
		readonly SqlServerDataSource m_DataSource;
		readonly string ProductTable = "Production.Product";

		public ProductLineRepository(SqlServerDataSource dataSource)
		{
			m_DataSource = dataSource;
		}

		public async Task<int> CreateAsync(ProductLine productLine)
		{
			if (productLine == null)
				throw new ArgumentNullException(nameof(productLine), $"{nameof(productLine)} is null.");

			using (var trans = m_DataSource.BeginTransaction())
			{
				productLine.ProductLineKey = await trans.Insert(productLine).ToInt32().ExecuteAsync().ConfigureAwait(false);
				productLine.ApplyKeys();
				await trans.InsertBatch(productLine.Products).ExecuteAsync().ConfigureAwait(false);
				trans.Commit();
			}

			return productLine.ProductLineKey;
		}

		public async Task DeleteAsync(ProductLine productLine)
		{
			if (productLine == null)
				throw new ArgumentNullException(nameof(productLine), $"{nameof(productLine)} is null.");

			using (var trans = m_DataSource.BeginTransaction())
			{
				await trans.DeleteWithFilter<Product>(new { productLine.ProductLineKey }).ExecuteAsync().ConfigureAwait(false);
				await trans.Delete(productLine).ExecuteAsync().ConfigureAwait(false);
				trans.Commit();
			}
		}

		public async Task DeleteByKeyAsync(int productLineKey)
		{
			using (var trans = m_DataSource.BeginTransaction())
			{
				await trans.DeleteWithFilter<Product>(new { productLineKey }).ExecuteAsync().ConfigureAwait(false);
				await trans.DeleteByKey<ProductLine>(productLineKey).ExecuteAsync().ConfigureAwait(false);
				trans.Commit();
			}
		}

		public async Task<IList<ProductLine>> FindByNameAsync(string productLineName, bool includeProducts, CancellationToken cancellationToken = default)
		{
			var results = await m_DataSource.From<ProductLine>(new { productLineName }).ToCollection().ExecuteAsync(cancellationToken).ConfigureAwait(false);
			if (results.Count > 0 && includeProducts)
			{
				var children = await m_DataSource.GetByKeyList(ProductTable, "ProductLineKey",
					results.Select(pl => pl.ProductLineKey)).ToCollection<Product>().ExecuteAsync(cancellationToken).ConfigureAwait(false);
				foreach (var line in results)
					line.Products.AddRange(children.Where(x => x.ProductLineKey == line.ProductLineKey));
			}
			return results;
		}

		public async Task<IList<ProductLine>> GetAllAsync(bool includeProducts, CancellationToken cancellationToken = default)
		{
			var results = await m_DataSource.From<ProductLine>().ToCollection().ExecuteAsync(cancellationToken).ConfigureAwait(false);
			if (includeProducts)
			{
				var children = await m_DataSource.From<Product>().ToCollection().ExecuteAsync(cancellationToken).ConfigureAwait(false);
				foreach (var line in results)
					line.Products.AddRange(children.Where(x => x.ProductLineKey == line.ProductLineKey));
			}
			return results;
		}

		public async Task<ProductLine?> GetByKeyAsync(int productLineKey, bool includeProducts, CancellationToken cancellationToken = default)
		{
			var result = await m_DataSource.GetByKey<ProductLine>(productLineKey).ToObjectOrNull().ExecuteAsync(cancellationToken).ConfigureAwait(false);
			if (result != null && includeProducts)
			{
				var children = await m_DataSource.From<Product>(new { result.ProductLineKey }).ToCollection().ExecuteAsync(cancellationToken).ConfigureAwait(false);
				result.Products.AddRange(children);
			}
			return result;
		}

		public async Task UpdateAsync(Product product)
		{
			if (product == null)
				throw new ArgumentNullException(nameof(product), $"{nameof(product)} is null.");

			await m_DataSource.Update(product).ExecuteAsync().ConfigureAwait(false);
		}

		public async Task UpdateAsync(ProductLine productLine)
		{
			if (productLine == null)
				throw new ArgumentNullException(nameof(productLine), $"{nameof(productLine)} is null.");

			await m_DataSource.Update(productLine).ExecuteAsync().ConfigureAwait(false);
		}

		public async Task UpdateGraphAsync(ProductLine productLine)
		{
			if (productLine == null)
				throw new ArgumentNullException(nameof(productLine), $"{nameof(productLine)} is null.");

			using (var trans = m_DataSource.BeginTransaction())
			{
				//Update parent row
				await trans.Update(productLine).ExecuteAsync().ConfigureAwait(false);

				//Ensure new child rows have their parent's key
				productLine.ApplyKeys();

				//Insert/update the remaining child rows
				foreach (var row in productLine.Products)
					await trans.Upsert(row).ExecuteAsync().ConfigureAwait(false);

				trans.Commit();
			}
		}

		public async Task UpdateGraphWithChildDeletesAsync(ProductLine productLine)
		{
			if (productLine == null)
				throw new ArgumentNullException(nameof(productLine), $"{nameof(productLine)} is null.");

			using (var trans = m_DataSource.BeginTransaction())
			{
				//Update parent row
				await trans.Update(productLine).ExecuteAsync().ConfigureAwait(false);

				//Find the list of child keys to remove
				var oldKeys = await trans.From<Product>(new { productLine.ProductLineKey }).ToInt32Set("ProductKey")
					.ExecuteAsync().ConfigureAwait(false);

				foreach (var key in productLine.Products.Select(x => x.ProductKey))
					oldKeys.Remove(key);

				//Remove the old records
				foreach (var key in oldKeys)
					await trans.DeleteByKey(ProductTable, key).ExecuteAsync().ConfigureAwait(false);

				//Ensure new child rows have their parent's key
				productLine.ApplyKeys();

				//Insert/update the child rows
				foreach (var row in productLine.Products)
					await trans.Upsert(row).ExecuteAsync().ConfigureAwait(false);

				trans.Commit();
			}
		}

		public async Task UpdateGraphWithDeletesAsync(ProductLine productLine, IList<int> productKeysToRemove)
		{
			if (productLine == null)
				throw new ArgumentNullException(nameof(productLine), $"{nameof(productLine)} is null.");

			using (var trans = m_DataSource.BeginTransaction())
			{
				//Update parent row
				await trans.Update(productLine).ExecuteAsync().ConfigureAwait(false);

				//Ensure new child rows have their parent's key
				productLine.ApplyKeys();

				//Insert/update the child rows
				foreach (var row in productLine.Products)
					await trans.Upsert(row).ExecuteAsync().ConfigureAwait(false);

				if (productKeysToRemove?.Count > 0)
					await trans.DeleteByKeyList(ProductTable, productKeysToRemove).ExecuteAsync().ConfigureAwait(false);

				trans.Commit();
			}
		}
	}
}
