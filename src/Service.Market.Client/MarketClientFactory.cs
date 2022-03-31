using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Service.Market.Grpc;
using Service.Grpc;

namespace Service.Market.Client
{
    [UsedImplicitly]
    public class MarketClientFactory : GrpcClientFactory
    {
        public MarketClientFactory(string grpcServiceUrl, ILogger logger) : base(grpcServiceUrl, logger)
        {
        }

        public IGrpcServiceProxy<IMarketService> GetMarketService() => CreateGrpcService<IMarketService>();
    }
}
