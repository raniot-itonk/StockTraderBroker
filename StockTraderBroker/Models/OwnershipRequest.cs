using System;

namespace StockTraderBroker.Models
{
    public class OwnershipRequest
    {
        public Guid Seller { get; set; }
        public Guid Buyer { get; set; }
        public int Amount { get; set; }
    }
}