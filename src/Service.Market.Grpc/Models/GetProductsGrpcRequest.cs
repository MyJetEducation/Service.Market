using System;
using System.Runtime.Serialization;

namespace Service.Market.Grpc.Models
{
	[DataContract]
	public class GetProductsGrpcRequest
	{
		[DataMember(Order = 1)]
		public Guid? UserId { get; set; }
	}
}