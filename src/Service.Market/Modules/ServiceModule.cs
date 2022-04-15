using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.TcpClient;
using Service.EducationRetry.Client;
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
			builder.RegisterEducationRetryClient(Program.Settings.EducationRetryServiceUrl, Program.LogFactory.CreateLogger(typeof(EducationRetryClientFactory)));

			var tcpServiceBus = new MyServiceBusTcpClient(() => Program.Settings.ServiceBusWriter, "MyJetEducation Service.Market");

			builder
				.Register(_ => new MyServiceBusPublisher<ClearEducationProgressServiceBusModel>(tcpServiceBus, ClearEducationProgressServiceBusModel.TopicName, false))
				.As<IServiceBusPublisher<ClearEducationProgressServiceBusModel>>()
				.SingleInstance();
			builder
				.Register(_ => new MyServiceBusPublisher<ClearEducationUiProgressServiceBusModel>(tcpServiceBus, ClearEducationUiProgressServiceBusModel.TopicName, false))
				.As<IServiceBusPublisher<ClearEducationUiProgressServiceBusModel>>()
				.SingleInstance();
			builder
				.Register(context => new MyServiceBusPublisher<NewMascotProductServiceBusModel>(tcpServiceBus, NewMascotProductServiceBusModel.TopicName, false))
				.As<IServiceBusPublisher<NewMascotProductServiceBusModel>>()
				.SingleInstance();

			tcpServiceBus.Start();
		}
	}
}