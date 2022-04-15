using System.Runtime.Serialization;
using Service.MarketProduct.Domain.Models;

namespace Service.Market.Grpc.Models
{
	[DataContract]
	public class ProductGrpcModel
	{
		[DataMember(Order = 1)]
		public MarketProductType Product { get; set; }

		[DataMember(Order = 2)]
		public MarketProductCategory Category { get; set; }

		[DataMember(Order = 3)]
		public decimal? Price { get; set; }

		[DataMember(Order = 4)]
		public int Priority { get; set; }
	}
}