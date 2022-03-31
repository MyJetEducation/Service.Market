using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Service.Grpc;
using Service.Market.Grpc;
using Service.Market.Grpc.Models;
using Service.Market.Mappers;
using Service.MarketProduct.Domain.Models;
using Service.MarketProduct.Grpc;
using Service.MarketProduct.Grpc.Models;
using Service.UserTokenAccount.Domain.Models;
using Service.UserTokenAccount.Grpc;
using Service.UserTokenAccount.Grpc.Models;
using ProductGrpcResponse = Service.Market.Grpc.Models.ProductGrpcResponse;

namespace Service.Market.Services
{
	public class MarketService : IMarketService
	{
		private readonly ILogger<MarketService> _logger;
		private readonly IGrpcServiceProxy<IUserTokenAccountService> _userTokenAccountService;
		private readonly IGrpcServiceProxy<IMarketProductService> _marketProductService;

		public MarketService(ILogger<MarketService> logger, IGrpcServiceProxy<IUserTokenAccountService> userTokenAccountService, IGrpcServiceProxy<IMarketProductService> marketProductService)
		{
			_logger = logger;
			_userTokenAccountService = userTokenAccountService;
			_marketProductService = marketProductService;
		}

		public async ValueTask<TokenAmountGrpcResponse> GetTokenAmountAsync(GetTokenAmountGrpcRequest request)
		{
			AccountGrpcResponse response = await _userTokenAccountService.Service.GetAccountAsync(new GetAccountGrpcRequest
			{
				UserId = request.UserId
			});

			return new TokenAmountGrpcResponse
			{
				Value = (response?.Value).GetValueOrDefault()
			};
		}

		public async ValueTask<ProductGrpcResponse> GetProductsAsync(GetProductsGrpcRequest request)
		{
			ProductListGrpcResponse response = await _marketProductService.Service.GetProductListAsync(new GetProductListGrpcRequest());

			return new ProductGrpcResponse
			{
				Products = response?.Products
					.OrderByDescending(model => model.Priority)
					.Select(model => model.ToGrpcModel()).ToArray()
			};
		}

		public async ValueTask<BuyProductGrpcResponse> BuyProductAsync(BuyProductGrpcRequest request)
		{
			MarketProductType orderProduct = request.Product;

			MarketProduct.Grpc.Models.ProductGrpcResponse resonse = await _marketProductService.Service.GetProductAsync(new GetProductGrpcRequest
			{
				ProductType = orderProduct
			});

			MarketProductGrpcModel marketProduct = resonse?.Product;
			if (marketProduct == null)
			{
				_logger.LogError("No answer was recieved when try to get product for request {@request}", request);

				return BuyProductGrpcResponse.Fail;
			}

			if (!marketProduct.IsValid())
			{
				_logger.LogError("Disabled product {orderProduct} ordered for request {@request}", orderProduct, request);

				return BuyProductGrpcResponse.Fail;
			}

			NewOperationGrpcResponse orderResponse = await _userTokenAccountService.TryCall(service => service.NewOperationAsync(new NewOperationGrpcRequest
			{
				UserId = request.UserId,
				Value = marketProduct.Price.GetValueOrDefault(),
				ProductType = orderProduct,
				Movement = TokenOperationMovement.Outcome,
				Source = TokenOperationSource.ProductPurchase
			}));

			TokenOperationResult operationResult = orderResponse.Result;

			if (operationResult != TokenOperationResult.Ok)
			{
				return operationResult switch
				{
					TokenOperationResult.InsufficientAccount => new BuyProductGrpcResponse { InsufficientAccount = true },
					TokenOperationResult.Failed => BuyProductGrpcResponse.Fail,
					_ => BuyProductGrpcResponse.Fail
				};
			}

			//

			return BuyProductGrpcResponse.Ok;
		}
	}
}