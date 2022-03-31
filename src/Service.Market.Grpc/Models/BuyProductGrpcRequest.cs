using System;
using System.Runtime.Serialization;
using Service.MarketProduct.Domain.Models;

namespace Service.Market.Grpc.Models
{
	[DataContract]
	public class BuyProductGrpcRequest
	{
		[DataMember(Order = 1)]
		public Guid? UserId { get; set; }

		[DataMember(Order = 2)]
		public MarketProductType Product { get; set; }
	}
}