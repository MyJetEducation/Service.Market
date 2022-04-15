using System.Runtime.Serialization;

namespace Service.Market.Grpc.Models
{
	[DataContract]
	public class BuyProductGrpcResponse
	{
		[DataMember(Order = 1)]
		public bool Successful { get; set; }

		[DataMember(Order = 2)]
		public bool InsufficientAccount { get; set; }
		
		[DataMember(Order = 3)]
		public bool ProductNotAvailable { get; set; }
		
		[DataMember(Order = 4)]
		public bool ProductAlreadyPurchased { get; set; }
		
		public static BuyProductGrpcResponse Fail => new() {Successful = false};
		public static BuyProductGrpcResponse Ok => new() {Successful = true};
	}
}