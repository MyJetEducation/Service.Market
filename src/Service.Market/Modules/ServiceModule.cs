using Autofac;
using Microsoft.Extensions.Logging;
using Service.MarketProduct.Client;
using Service.UserTokenAccount.Client;

namespace Service.Market.Modules
{
	public class ServiceModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterMarketProductClient(Program.Settings.MarketProductServiceUrl, Program.LogFactory.CreateLogger(typeof (MarketProductClientFactory)));
			builder.RegisterMarketProductClient(Program.Settings.UserTokenAccountServiceUrl, Program.LogFactory.CreateLogger(typeof (UserTokenAccountClientFactory)));
		}
	}
}