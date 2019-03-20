using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using StockTraderBroker.DB;
using StockTraderBroker.Models;

namespace StockTraderBroker.Logic
{
    public interface IBuyShares
    {
        List<ShareTradingInfo> AddBuyRequest(BuyRequestInput buyRequestInput);
    }

    public class BuyShares : IBuyShares
    {
        private readonly IMapper _mapper;
        private readonly ITransaction _transaction;
        private readonly StockTraderBrokerContext _context;
        private readonly ILogger<BuyShares> _logger;

        public BuyShares(StockTraderBrokerContext context, ILogger<BuyShares> logger, IMapper mapper, ITransaction transaction)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _transaction = transaction;
        }

        public List<ShareTradingInfo> AddBuyRequest(BuyRequestInput buyRequestInput)
        {
            var shareTradingInfos = new List<ShareTradingInfo>();
            var sellerListOrderedByPrice = GetSellerList(buyRequestInput);

            foreach (var sellRequest in sellerListOrderedByPrice)
            {
                var shareTradingInfo = BuyAsManySharesAsPossible(buyRequestInput, sellRequest);
                shareTradingInfos.Add(shareTradingInfo);
                if (buyRequestInput.AmountOfShares == 0)
                    break;
            }

            // Add the rest to the database
            if (buyRequestInput.AmountOfShares != 0)
            {
                var buyRequest = _mapper.Map<BuyRequest>(buyRequestInput);
                _context.BuyRequests.Add(buyRequest);
            }
            _context.SaveChanges();

            return shareTradingInfos;
        }

        private IEnumerable<SellRequest> GetSellerList(BuyRequestInput buyRequestInput)
        {
            return _context.SellRequests.Where(sellRequest =>
                                sellRequest.StockId == buyRequestInput.StockId &&
                                sellRequest.TimeOut > DateTime.Now &&
                                sellRequest.Price <= buyRequestInput.Price).OrderBy(sellRequest => sellRequest.Price).ToList();
        }

        private ShareTradingInfo BuyAsManySharesAsPossible(BuyRequestInput buyRequestInput, SellRequest sellRequest)
        {
            CalculateSellerRemainingSharesAndSharesToBuy(buyRequestInput, sellRequest, out var sellerSharesRemaining, out var sharesToBuy);
            sellRequest.AmountOfShares = sellerSharesRemaining;
            buyRequestInput.AmountOfShares -= sharesToBuy;
            _transaction.CreateTransaction(buyRequestInput.Price, sharesToBuy, buyRequestInput.AccountId, buyRequestInput.ReserveId, sellRequest.AccountId, buyRequestInput.StockId);
            return new ShareTradingInfo { Price = sellRequest.Price, Amount = sharesToBuy };
        }

        private void CalculateSellerRemainingSharesAndSharesToBuy(BuyRequestInput buyRequestInput, SellRequest sellRequest, out int sellerSharesRemaining, out int sharesToBuy)
        {
            if (sellRequest.AmountOfShares > buyRequestInput.AmountOfShares)
            {
                _context.Attach(sellRequest);
                sellerSharesRemaining = sellRequest.AmountOfShares - buyRequestInput.AmountOfShares;
                sharesToBuy = buyRequestInput.AmountOfShares;
            }
            else
            {
                sellerSharesRemaining = 0;
                _context.Remove(sellRequest);
                sharesToBuy = sellRequest.AmountOfShares;
            }
        }
    }
}
