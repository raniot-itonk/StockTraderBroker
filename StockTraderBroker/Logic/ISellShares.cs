using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockTraderBroker.DB;
using StockTraderBroker.Models;

namespace StockTraderBroker.Logic
{
    public interface ISellShares
    {
        Task<List<ShareTradingInfo>> AddSellRequestAsync(SellRequestModel sellRequestModel);
        Task<List<SellRequest>> GetSaleRequestsForSpecificOwnerAndStock(Guid ownerId, long stockId);
        Task<List<SellRequest>> GetSaleRequestsForSpecificOwner(Guid ownerId);
        Task<List<SellRequest>> GetSaleRequestsForSpecificStock(long stockId);
        Task RemoveSellRequest(long id);
    }
}