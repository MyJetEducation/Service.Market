using Autofac;
using Microsoft.Extensions.Logging;
using Service.Market.Grpc;
using Service.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.Market.Client
{
    public static class AutofacHelper
    {
        public static void RegisterMarketClient(this ContainerBuilder builder, string grpcServiceUrl, ILogger logger)
        {
            var factory = new MarketClientFactory(grpcServiceUrl, logger);

            builder.RegisterInstance(factory.GetMarketService()).As<IGrpcServiceProxy<IMarketService>>().SingleInstance();
        }
    }
}
