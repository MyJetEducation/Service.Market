using System.Runtime.Serialization;

namespace Service.Market.Grpc.Models
{
	[DataContract]
	public class ProductGrpcResponse
	{
		[DataMember(Order = 1)]
		public ProductGrpcModel[] Products { get; set; }
	}
}