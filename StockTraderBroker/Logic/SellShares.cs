using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using StockTraderBroker.Clients;
using StockTraderBroker.Controllers;
using StockTraderBroker.DB;
using StockTraderBroker.Models;

namespace StockTraderBroker.Logic
{
    public interface ISellShares
    {
        Task<List<ShareTradingInfo>> AddSellRequestAsync(SellRequestInput sellRequestInput);
    }

    public class SellShares : ISellShares
    {
        private readonly IMapper _mapper;
        private readonly ITransaction _transaction;
        private readonly IPublicShareOwnerControlClient _publicShareOwnerControlClient;
        private readonly StockTraderBrokerContext _context;
        private readonly ILogger<SellShares> _logger;

        public SellShares(StockTraderBrokerContext context, ILogger<SellShares> logger, IMapper mapper, ITransaction transaction, IPublicShareOwnerControlClient publicShareOwnerControlClient)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _transaction = transaction;
            _publicShareOwnerControlClient = publicShareOwnerControlClient;
        }

        public async Task<List<ShareTradingInfo>> AddSellRequestAsync(SellRequestInput sellRequestInput)
        {
            var shareTradingInfos = new List<ShareTradingInfo>();
            var buyerListOrderedByPrice = GetBuyerList(sellRequestInput);

            foreach (var buyerRequest in buyerListOrderedByPrice)
            {
                var shareTradingInfo = await SellAsManySharesAsPossible(buyerRequest, sellRequestInput);
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

        private async Task<ShareTradingInfo> SellAsManySharesAsPossible(BuyRequest buyRequest, SellRequestInput sellRequestInput)
        {
            var sharesToSell = CalculateSharesToSeller(buyRequest, sellRequestInput);
            sellRequestInput.AmountOfShares -= sharesToSell;
            await _transaction.CreateTransactionAsync(buyRequest.Price, sharesToSell, sellRequestInput.AccountId, buyRequest.ReserveId, buyRequest.AccountId, buyRequest.StockId);

            var ownershipRequest = new OwnershipRequest
            {
                Amount = sharesToSell,
                Buyer = buyRequest.AccountId,
                Seller = sellRequestInput.AccountId
            };
            await _publicShareOwnerControlClient.ChangeOwnership(ownershipRequest, buyRequest.StockId, "jwtToken");
            return new ShareTradingInfo { Price = buyRequest.Price, Amount = sharesToSell };
        }

        private int CalculateSharesToSeller(BuyRequest buyRequest, SellRequestInput sellRequestInput)
        {
            int sharesToSell;
            if (buyRequest.AmountOfShares > sellRequestInput.AmountOfShares)
            {
                _context.Attach(buyRequest);
                buyRequest.AmountOfShares = buyRequest.AmountOfShares - sellRequestInput.AmountOfShares;
                sharesToSell = sellRequestInput.AmountOfShares;
            }
            else
            {
                _context.Remove(buyRequest);
                sharesToSell = buyRequest.AmountOfShares;
            }

            return sharesToSell;
        }
    }
}
