using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockTraderBroker.DB;
using StockTraderBroker.Models;

namespace StockTraderBroker.Logic
{
    public interface IBuyShares
    {
        Task<List<ShareTradingInfo>> AddBuyRequest(BuyRequestModel buyRequestModel);
        Task RemoveBuyRequest(long id);
        Task<List<BuyRequest>> GetBuyRequestsForSpecificOwner(Guid ownerId);
    }
}