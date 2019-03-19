using System;

namespace StockTraderBroker.Models
{
    public class TransferRequest
    {
        public Guid FromAccountId { get; set; }
        public Guid ReservationId { get; set; }
        public Guid ToAccountId { get; set; }
        public double Amount { get; set; }
    }
}