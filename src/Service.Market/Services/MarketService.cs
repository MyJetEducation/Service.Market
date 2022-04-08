﻿using System;
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
		private readonly IServiceBusPublisher<ClearEducationProgressServiceBusModel> _clearProgressPublisher;
		private readonly IServiceBusPublisher<NewMascotProductServiceBusModel> _newMascotPublisher;
		private readonly IGrpcServiceProxy<IEducationRetryService> _educationRetryService;

		public MarketService(ILogger<MarketService> logger,
			IGrpcServiceProxy<IUserTokenAccountService> userTokenAccountService,
			IGrpcServiceProxy<IMarketProductService> marketProductService,
			IServiceBusPublisher<ClearEducationProgressServiceBusModel> clearProgressPublisher,
			IGrpcServiceProxy<IEducationRetryService> educationRetryService,
			IServiceBusPublisher<NewMascotProductServiceBusModel> newMascotPublisher)
		{
			_logger = logger;
			_userTokenAccountService = userTokenAccountService;
			_marketProductService = marketProductService;
			_clearProgressPublisher = clearProgressPublisher;
			_educationRetryService = educationRetryService;
			_newMascotPublisher = newMascotPublisher;
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

				return BuyProductGrpcResponse.Fail;
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

			return await ProcessNewProduct(orderProduct, userId);
		}

		private async ValueTask<BuyProductGrpcResponse> ProcessNewProduct(MarketProductType product, string userId)
		{
			if (product == MarketProductType.EducationProgressWipe)
			{
				_logger.LogInformation("Publish ClearEducationProgressServiceBusModel for user {user}, product: {product}", userId, product);

				await _clearProgressPublisher.PublishAsync(new ClearEducationProgressServiceBusModel {UserId = userId});
			}

			if (ProductTypeGroup.RetryPackProductTypes.Contains(product))
			{
				int retryValue = GetRetryValue(product);

				_logger.LogInformation("Calling increase retry count ({count}) for user {user}, product: {product}.", retryValue, userId, product);

				CommonGrpcResponse response = await _educationRetryService.TryCall(service => service.IncreaseRetryCountAsync(new IncreaseRetryCountGrpcRequest
				{
					UserId = userId,
					Value = retryValue
				}));

				if (response.IsSuccess != true)
					return BuyProductGrpcResponse.Fail;
			}

			if (ProductTypeGroup.MascotProductTypes.Contains(product))
			{
				_logger.LogInformation("Publish NewMascotProductServiceBusModel for user {user}, product: {product}", userId, product);

				await _newMascotPublisher.PublishAsync(new NewMascotProductServiceBusModel {UserId = userId, Product = product});
			}

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