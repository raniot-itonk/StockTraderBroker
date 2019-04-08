using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using StockTraderBroker.Clients;
using StockTraderBroker.DB;
using StockTraderBroker.Models;

namespace StockTraderBroker.Logic
{
    public interface IBuyShares
    {
        Task<List<ShareTradingInfo>> AddBuyRequest(BuyRequestInput buyRequestInput);
    }

    public class BuyShares : IBuyShares
    {
        private readonly IMapper _mapper;
        private readonly ITransaction _transaction;
        private readonly IPublicShareOwnerControlClient _publicShareOwnerControlClient;
        private readonly IBankClient _bankClient;
        private readonly StockTraderBrokerContext _context;
        private readonly ILogger<BuyShares> _logger;

        public BuyShares(StockTraderBrokerContext context, ILogger<BuyShares> logger, IMapper mapper, ITransaction transaction, IPublicShareOwnerControlClient publicShareOwnerControlClient, IBankClient bankClient)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _transaction = transaction;
            _publicShareOwnerControlClient = publicShareOwnerControlClient;
            _bankClient = bankClient;
        }

        public async Task<List<ShareTradingInfo>> AddBuyRequest(BuyRequestInput buyRequestInput)
        {
            var shareTradingInfos = new List<ShareTradingInfo>();
            var sellerListOrderedByPrice = GetSellerList(buyRequestInput);

            foreach (var sellRequest in sellerListOrderedByPrice)
            {
                var shareTradingInfo = await BuyAsManySharesAsPossible(buyRequestInput, sellRequest);
                shareTradingInfos.Add( shareTradingInfo);
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

        private async Task<ShareTradingInfo> BuyAsManySharesAsPossible(BuyRequestInput buyRequestInput, SellRequest sellRequest)
        {
            var sharesToBuy = CalculateSharesToBuy(buyRequestInput, sellRequest);
            buyRequestInput.AmountOfShares -= sharesToBuy;
            await _transaction.CreateTransactionAsync(sellRequest.Price, sharesToBuy, sellRequest.AccountId, buyRequestInput.ReserveId, buyRequestInput.AccountId, buyRequestInput.StockId);

            if (sharesToBuy == buyRequestInput.AmountOfShares) await _bankClient.RemoveReservation(buyRequestInput.ReserveId, "jwtToken");

            var ownershipRequest = new OwnershipRequest
            {
                Amount = sharesToBuy,
                Buyer = buyRequestInput.AccountId,
                Seller = sellRequest.AccountId
            };
            await _publicShareOwnerControlClient.ChangeOwnership(ownershipRequest, buyRequestInput.StockId, "jwtToken");

            var lastTradedValueRequest = new LastTradedValueRequest { Id = buyRequestInput.StockId, Value = sellRequest.Price };
            await _publicShareOwnerControlClient.UpdateLastTradedValue(lastTradedValueRequest, buyRequestInput.StockId, "jwtToken");

            return new ShareTradingInfo { Price = sellRequest.Price, Amount = sharesToBuy };
        }

        private int CalculateSharesToBuy(BuyRequestInput buyRequestInput, SellRequest sellRequest)
        {
            int sharesToBuy;
            if (sellRequest.AmountOfShares > buyRequestInput.AmountOfShares)
            {
                _context.Attach(sellRequest);
                sellRequest.AmountOfShares = sellRequest.AmountOfShares - buyRequestInput.AmountOfShares;
                sharesToBuy = buyRequestInput.AmountOfShares;
            }
            else
            {
                _context.Remove(sellRequest);
                sharesToBuy = sellRequest.AmountOfShares;
            }

            return sharesToBuy;
        }
    }
}
