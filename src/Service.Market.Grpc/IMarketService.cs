using System.ServiceModel;
using System.Threading.Tasks;
using Service.Market.Grpc.Models;

namespace Service.Market.Grpc
{
	[ServiceContract]
	public interface IMarketService
	{
		[OperationContract]
		ValueTask<ProductGrpcResponse> GetProductsAsync(GetProductsGrpcRequest request);

		[OperationContract]
		ValueTask<BuyProductGrpcResponse> BuyProductAsync(BuyProductGrpcRequest request);
	}
}