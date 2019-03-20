using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using StockTraderBroker.Controllers;
using StockTraderBroker.DB;
using StockTraderBroker.Models;

namespace StockTraderBroker.Logic
{
    public interface ISellShares
    {
        List<ShareTradingInfo> AddSellRequest(SellRequestInput sellRequestInput);
    }

    public class SellShares : ISellShares
    {
        private readonly IMapper _mapper;
        private readonly ITransaction _transaction;
        private readonly StockTraderBrokerContext _context;
        private readonly ILogger<SellShares> _logger;

        public SellShares(StockTraderBrokerContext context, ILogger<SellShares> logger, IMapper mapper, ITransaction transaction)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _transaction = transaction;
        }

        public List<ShareTradingInfo> AddSellRequest(SellRequestInput sellRequestInput)
        {
            var shareTradingInfos = new List<ShareTradingInfo>();
            var buyerListOrderedByPrice = GetBuyerList(sellRequestInput);

            foreach (var buyerRequest in buyerListOrderedByPrice)
            {
                var shareTradingInfo = SellAsManySharesAsPossible(buyerRequest, sellRequestInput);
                shareTradingInfos.Add(shareTradingInfo);
                if (sellRequestInput.AmountOfShares == 0)
                    break;
            }

            // Add the rest to the database
            if (sellRequestInput.AmountOfShares != 0)
            {
                var sellRequest = _mapper.Map<SellRequest>(sellRequestInput);
                _context.SellRequests.Add(sellRequest);
            }
            _context.SaveChanges();

            return shareTradingInfos;
        }

        private IEnumerable<BuyRequest> GetBuyerList(SellRequestInput sellRequestInput)
        {
            return _context.BuyRequests.Where(buyRequest =>
                                buyRequest.StockId == sellRequestInput.StockId &&
                                buyRequest.TimeOut > DateTime.Now &&
                                buyRequest.Price >= sellRequestInput.Price).OrderByDescending(sellRequest => sellRequest.Price).ToList();
        }

        private ShareTradingInfo SellAsManySharesAsPossible(BuyRequest buyRequest, SellRequestInput sellRequestInput)
        {
            CalculateBuyerRemainingSharesAndSharesToSeller(buyRequest, sellRequestInput, out var buyerSharesRemaining, out var sharesToSell);
            buyRequest.AmountOfShares = buyerSharesRemaining;
            sellRequestInput.AmountOfShares -= sharesToSell;
            _transaction.CreateTransaction(buyRequest.Price, sharesToSell, buyRequest.AccountId, buyRequest.ReserveId, sellRequestInput.AccountId, buyRequest.StockId);
            return new ShareTradingInfo { Price = buyRequest.Price, Amount = sharesToSell };
        }

        private void CalculateBuyerRemainingSharesAndSharesToSeller(BuyRequest buyRequest, SellRequestInput sellRequestInput, out int buyerSharesRemaining, out int sharesToSell)
        {
            if (buyRequest.AmountOfShares > sellRequestInput.AmountOfShares)
            {
                _context.Attach(buyRequest);
                buyerSharesRemaining = buyRequest.AmountOfShares - sellRequestInput.AmountOfShares;
                sharesToSell = sellRequestInput.AmountOfShares;
            }
            else
            {
                buyerSharesRemaining = 0;
                _context.Remove(buyRequest);
                sharesToSell = buyRequest.AmountOfShares;
            }
        }
    }
}
