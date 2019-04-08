﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTraderBroker.Clients;
using StockTraderBroker.DB;
using StockTraderBroker.Models;

namespace StockTraderBroker.Logic
{
    public interface ISellShares
    {
        Task<List<ShareTradingInfo>> AddSellRequestAsync(SellRequestModel sellRequestModel);
        Task<List<SellRequestModel>> GetSaleRequestsForSpecificOwnerAndStock(Guid ownerId, long stockId);
        Task<List<SellRequestModel>> GetSaleRequestsForSpecificOwner(Guid ownerId);
    }

    public class SellShares : ISellShares
    {
        private readonly IMapper _mapper;
        private readonly ITransaction _transaction;
        private readonly IPublicShareOwnerControlClient _publicShareOwnerControlClient;
        private readonly IBankClient _bankClient;
        private readonly StockTraderBrokerContext _context;
        private readonly ILogger<SellShares> _logger;

        public SellShares(StockTraderBrokerContext context, ILogger<SellShares> logger, IMapper mapper, ITransaction transaction, IPublicShareOwnerControlClient publicShareOwnerControlClient, IBankClient bankClient)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _transaction = transaction;
            _publicShareOwnerControlClient = publicShareOwnerControlClient;
            _bankClient = bankClient;
        }

        public async Task<List<SellRequestModel>> GetSaleRequestsForSpecificOwnerAndStock(Guid ownerId, long stockId)
        {
            var sellRequests = await _context.SellRequests.Where(request => request.StockId == stockId && request.AccountId == ownerId).ToListAsync();
            return _mapper.Map<List<SellRequestModel>>(sellRequests);
        }
        public async Task<List<SellRequestModel>> GetSaleRequestsForSpecificOwner(Guid ownerId)
        {
            var sellRequests = await _context.SellRequests.Where(request => request.AccountId == ownerId).ToListAsync();
            return _mapper.Map<List<SellRequestModel>>(sellRequests);
        }

        public async Task<List<ShareTradingInfo>> AddSellRequestAsync(SellRequestModel sellRequestModel)
        {
            var shareTradingInfos = new List<ShareTradingInfo>();
            var buyerListOrderedByPrice = GetBuyerList(sellRequestModel);

            foreach (var buyerRequest in buyerListOrderedByPrice)
            {
                var shareTradingInfo = await SellAsManySharesAsPossible(buyerRequest, sellRequestModel);
                shareTradingInfos.Add(shareTradingInfo);
                if (sellRequestModel.AmountOfShares == 0)
                    break;
            }

            // Add the rest to the database
            if (sellRequestModel.AmountOfShares != 0)
            {
                var sellRequest = _mapper.Map<SellRequest>(sellRequestModel);
                _context.SellRequests.Add(sellRequest);
            }
            _context.SaveChanges();

            return shareTradingInfos;
        }

        private IEnumerable<BuyRequest> GetBuyerList(SellRequestModel sellRequestModel)
        {
            return _context.BuyRequests.Where(buyRequest =>
                                buyRequest.StockId == sellRequestModel.StockId &&
                                buyRequest.TimeOut > DateTime.Now &&
                                buyRequest.Price >= sellRequestModel.Price).OrderByDescending(sellRequest => sellRequest.Price).ToList();
        }

        private async Task<ShareTradingInfo> SellAsManySharesAsPossible(BuyRequest buyRequest, SellRequestModel sellRequestModel)
        {
            var sharesToSell = CalculateSharesToSeller(buyRequest, sellRequestModel);
            sellRequestModel.AmountOfShares -= sharesToSell;

            await _transaction.CreateTransactionAsync(sellRequestModel.Price, sharesToSell, sellRequestModel.AccountId, buyRequest.ReserveId, buyRequest.AccountId, buyRequest.StockId);
            _logger.LogWarning($"This is Anders's test {sharesToSell} and {buyRequest.AmountOfShares}");
            if (sharesToSell == buyRequest.AmountOfShares) await _bankClient.RemoveReservation(buyRequest.ReserveId, "jwtToken");

            var ownershipRequest = new OwnershipRequest
            {
                Amount = sharesToSell,
                Buyer = buyRequest.AccountId,
                Seller = sellRequestModel.AccountId
            };
            await _publicShareOwnerControlClient.ChangeOwnership(ownershipRequest, buyRequest.StockId, "jwtToken");

            var lastTradedValueRequest = new LastTradedValueRequest{Id = buyRequest.StockId, Value = sellRequestModel.Price};
            await _publicShareOwnerControlClient.UpdateLastTradedValue(lastTradedValueRequest, buyRequest.StockId, "jwtToken");
            return new ShareTradingInfo { Price = buyRequest.Price, Amount = sharesToSell };
        }

        private int CalculateSharesToSeller(BuyRequest buyRequest, SellRequestModel sellRequestModel)
        {
            int sharesToSell;
            if (buyRequest.AmountOfShares > sellRequestModel.AmountOfShares)
            {
                _context.Attach(buyRequest);
                buyRequest.AmountOfShares = buyRequest.AmountOfShares - sellRequestModel.AmountOfShares;
                sharesToSell = sellRequestModel.AmountOfShares;
            }
            else
            {
                _context.Remove(buyRequest);
                sharesToSell = buyRequest.AmountOfShares;
                _bankClient.RemoveReservation(buyRequest.ReserveId, "jwtToken");
            }

            return sharesToSell;
        }
    }
}
