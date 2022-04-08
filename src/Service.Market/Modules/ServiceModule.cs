using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.TcpClient;
using Service.MarketProduct.Client;
using Service.ServiceBus.Models;
using Service.UserTokenAccount.Client;

namespace Service.Market.Modules
{
	public class ServiceModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterMarketProductClient(Program.Settings.MarketProductServiceUrl, Program.LogFactory.CreateLogger(typeof (MarketProductClientFactory)));
			builder.RegisterUserTokenAccountClient(Program.Settings.UserTokenAccountServiceUrl, Program.LogFactory.CreateLogger(typeof (UserTokenAccountClientFactory)));

			var tcpServiceBus = new MyServiceBusTcpClient(() => Program.Settings.ServiceBusWriter, "MyJetEducation Service.Market");

			builder
				.Register(context => new MyServiceBusPublisher<ClearEducationProgressServiceBusModel>(tcpServiceBus, ClearEducationProgressServiceBusModel.TopicName, false))
				.As<IServiceBusPublisher<ClearEducationProgressServiceBusModel>>()
				.SingleInstance();

			tcpServiceBus.Start();
		}
	}
}