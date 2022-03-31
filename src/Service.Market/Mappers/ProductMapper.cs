using Service.Market.Grpc.Models;
using Service.MarketProduct.Grpc.Models;

namespace Service.Market.Mappers
{
	public static class ProductMapper
	{
		public static ProductGrpcModel ToGrpcModel(this MarketProductGrpcModel model) => new ProductGrpcModel
		{
			Product = model.ProductType,
			Category = model.Category,
			Price = model.Price
		};
	}
}