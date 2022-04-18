using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.ServiceBus;
using Service.Core.Client.Models;
using Service.EducationRetry.Grpc;
using Service.EducationRetry.Grpc.Models;
using Service.Grpc;
using Service.Market.Grpc;
using Service.Market.Grpc.Models;
using Service.Market.Mappers;
using Service.MarketProduct.Domain.Models;
using Service.MarketProduct.Grpc;
using Service.MarketProduct.Grpc.Models;
using Service.ServiceBus.Models;
using Service.UserMascotRepository.Grpc;
using Service.UserMascotRepository.Grpc.Models;
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
		private readonly IGrpcServiceProxy<IEducationRetryService> _educationRetryService;
		private readonly IGrpcServiceProxy<IUserMascotRepositoryService> _userMascotRepository;

		private readonly IServiceBusPublisher<ClearEducationProgressServiceBusModel> _clearProgressPublisher;
		private readonly IServiceBusPublisher<ClearEducationUiProgressServiceBusModel> _clearUiProgressPublisher;
		private readonly IServiceBusPublisher<MarketProductPurchasedServiceBusModel> _newProductPublisher;

		public MarketService(ILogger<MarketService> logger,
			IGrpcServiceProxy<IUserTokenAccountService> userTokenAccountService,
			IGrpcServiceProxy<IMarketProductService> marketProductService,
			IServiceBusPublisher<ClearEducationProgressServiceBusModel> clearProgressPublisher,
			IGrpcServiceProxy<IEducationRetryService> educationRetryService,
			IServiceBusPublisher<MarketProductPurchasedServiceBusModel> newProductPublisher,
			IServiceBusPublisher<ClearEducationUiProgressServiceBusModel> clearUiProgressPublisher,
			IGrpcServiceProxy<IUserMascotRepositoryService> userMascotRepository)
		{
			_logger = logger;
			_userTokenAccountService = userTokenAccountService;
			_marketProductService = marketProductService;
			_clearProgressPublisher = clearProgressPublisher;
			_educationRetryService = educationRetryService;
			_newProductPublisher = newProductPublisher;
			_clearUiProgressPublisher = clearUiProgressPublisher;
			_userMascotRepository = userMascotRepository;
		}

		public async ValueTask<ProductGrpcResponse> GetProductsAsync(GetProductsGrpcRequest request)
		{
			string userId = request.UserId;

			ProductListGrpcResponse response = await _marketProductService.Service.GetProductListAsync(new GetProductListGrpcRequest());

			MascotProductsGrpcResponse userMascotProductsResponse = await _userMascotRepository.Service.GetMascotProducts(new GetMascotProductsGrpcRequest {UserId = userId});
			if (userMascotProductsResponse == null)
				_logger.LogError("Can't get user {user} mascot products for request {@request}", userId, request);

			MarketProductType[] userMascotProducts = userMascotProductsResponse?.Products ?? Array.Empty<MarketProductType>();

			return new ProductGrpcResponse
			{
				Products = response?.Products
					.OrderByDescending(model => model.Priority)
					.Select(model => model.ToGrpcModel(userMascotProducts))
					.ToArray()
			};
		}

		public async ValueTask<BuyProductGrpcResponse> BuyProductAsync(BuyProductGrpcRequest request)
		{
			MarketProductType orderProduct = request.Product;
			string userId = request.UserId;

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

				return new BuyProductGrpcResponse {Successful = false, ProductNotAvailable = true};
			}

			MascotProductsGrpcResponse userMascotProductsResponse = await _userMascotRepository.Service.GetMascotProducts(new GetMascotProductsGrpcRequest {UserId = userId});
			if (userMascotProductsResponse == null)
			{
				_logger.LogError("Can't get user {user} mascot products for request {@request}", userId, request);

				return BuyProductGrpcResponse.Fail;
			}

			if (ProductTypeGroup.MascotProductTypes.Contains(orderProduct) && userMascotProductsResponse.Products.Contains(orderProduct))
			{
				_logger.LogError("User {user} already has product {product}, can't buy as duplicate, request {@request}", userId, orderProduct, request);

				return new BuyProductGrpcResponse {Successful = false, ProductAlreadyPurchased = true};
			}

			var operation = new NewOperationGrpcRequest
			{
				UserId = userId,
				Value = marketProduct.Price.GetValueOrDefault(),
				ProductType = orderProduct,
				Movement = TokenOperationMovement.Outcome,
				Source = TokenOperationSource.ProductPurchase
			};

			NewOperationGrpcResponse orderResponse = await _userTokenAccountService.TryCall(service => service.NewOperationAsync(operation));
			TokenOperationResult operationResult = orderResponse.Result;
			if (operationResult != TokenOperationResult.Ok)
			{
				_logger.LogError("New operation {@operation} for user {user} failed.", operation, userId);

				return operationResult switch
				{
					TokenOperationResult.InsufficientAccount => new BuyProductGrpcResponse {InsufficientAccount = true},
					TokenOperationResult.Failed => BuyProductGrpcResponse.Fail,
					_ => BuyProductGrpcResponse.Fail
					};
			}

			return await ProcessNewProduct(marketProduct, userId, orderResponse.Value.GetValueOrDefault());
		}

		private async ValueTask<BuyProductGrpcResponse> ProcessNewProduct(MarketProductGrpcModel product, string userId, decimal account)
		{
			MarketProductType productType = product.ProductType;

			if (productType == MarketProductType.EducationProgressWipe)
			{
				_logger.LogInformation("Publish ClearEducationProgressServiceBusModel for user {user}, product: {product}", userId, productType);

				await _clearProgressPublisher.PublishAsync(new ClearEducationProgressServiceBusModel
				{
					UserId = userId,
					ClearProgress = true,
					ClearAchievements = true,
					ClearHabits = true,
					ClearKnowledge = true,
					ClearRetry = true,
					ClearSkills = true,
					ClearStatuses = true,
					ClearUserTime = true
				});

				await _clearUiProgressPublisher.PublishAsync(new ClearEducationUiProgressServiceBusModel
				{
					UserId = userId
				});
			}

			if (ProductTypeGroup.RetryPackProductTypes.Contains(productType))
			{
				int retryValue = GetRetryValue(productType);

				_logger.LogInformation("Calling increase retry count ({count}) for user {user}, product: {product}.", retryValue, userId, productType);

				CommonGrpcResponse response = await _educationRetryService.TryCall(service => service.IncreaseRetryCountAsync(new IncreaseRetryCountGrpcRequest
				{
					UserId = userId,
					Value = retryValue
				}));

				if (response.IsSuccess != true)
					return BuyProductGrpcResponse.Fail;
			}

			if (ProductTypeGroup.MascotProductTypes.Contains(productType))
			{
			}

			var busModel = new MarketProductPurchasedServiceBusModel
			{
				UserId = userId, 
				Product = productType,
				ProductPrice = product.Price.GetValueOrDefault(),
				AccountValue = account
			};

			_logger.LogInformation("Publish MarketProductPurchasedServiceBusModel: {@busModel}", busModel);

			await _newProductPublisher.PublishAsync(busModel);

			return BuyProductGrpcResponse.Ok;
		}

		private static int GetRetryValue(MarketProductType product) =>
			product switch {
				MarketProductType.RetryPack1 => 1,
				MarketProductType.RetryPack10 => 10,
				MarketProductType.RetryPack25 => 25,
				MarketProductType.RetryPack100 => 100,
				_ => throw new Exception($"Can't get retry value count from product {product}")
				};
	}
}