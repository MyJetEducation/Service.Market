using System.Runtime.Serialization;

namespace Service.Market.Grpc.Models
{
	[DataContract]
	public class TokenAmountGrpcResponse
	{
		[DataMember(Order = 1)]
		public decimal Value { get; set; }
	}
}