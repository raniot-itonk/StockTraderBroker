using System;

namespace StockTraderBroker.Models
{
    public class BuyRequestInput
    {
        public Guid AccountId { get; set; }
        public long StockId { get; set; }
        public double Price { get; set; }
        public DateTime TimeOut { get; set; }
        public int AmountOfShares { get; set; }
        public Guid ReserveId { get; set; }
    }
}