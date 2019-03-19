using System;
using Microsoft.EntityFrameworkCore;

namespace StockTraderBroker.DB
{
    public class StockTraderBrokerContext : DbContext
    {
        //public StockTraderBrokerContext()
        //{
        //}

        public StockTraderBrokerContext(DbContextOptions<StockTraderBrokerContext> options)
            : base(options)
        {
        }
        public virtual DbSet<BuyRequest> BuyRequests { get; set; }
        public virtual DbSet<SellRequest> SellRequests { get; set; }
    }

    public class BuyRequest
    {
        public long Id { get; set; }
        public Guid AccountId { get; set; }
        public long StockId { get; set; }
        public double Price { get; set; }
        public DateTime TimeOut { get; set; }
        public int AmountOfShares { get; set; }
        public Guid ReserveId { get; set; }
    }

    public class SellRequest
    {
        public long Id { get; set; }
        public Guid AccountId { get; set; }
        public long StockId { get; set; }
        public double Price { get; set; }
        public DateTime TimeOut { get; set; }
        public int AmountOfShares { get; set; }
    }
}
