using System.Runtime.Serialization;

namespace Service.Market.Grpc.Models
{
	[DataContract]
	public class GetProductsGrpcRequest
	{
		[DataMember(Order = 1)]
		public string UserId { get; set; }
	}
}