namespace Service.Market.Postgres.Models
{
    public class MarketEntity
    {
        public int? Id { get; set; }

        public DateTime? Date { get; set; }

        public string Value { get; set; }
    }
}
