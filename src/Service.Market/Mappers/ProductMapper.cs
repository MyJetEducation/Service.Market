using System.Linq;
using Service.Market.Grpc.Models;
using Service.MarketProduct.Domain.Models;
using Service.MarketProduct.Grpc.Models;

namespace Service.Market.Mappers
{
	public static class ProductMapper
	{
		public static ProductGrpcModel ToGrpcModel(this MarketProductGrpcModel model, MarketProductType[] userProducts)
		{
			MarketProductType product = model.ProductType;

			return new ProductGrpcModel
			{
				Product = product,
				Category = model.Category,
				Price = model.Price,
				Priority = model.Priority,
				Purchased = userProducts.Contains(product)
			};
		}
	}
}