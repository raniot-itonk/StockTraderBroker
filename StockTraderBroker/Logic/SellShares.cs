﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prometheus;
using StockTraderBroker.Clients;
using StockTraderBroker.DB;
using StockTraderBroker.Exceptions;
using StockTraderBroker.Models;

namespace StockTraderBroker.Logic
{
    public class SellShares : ISellShares
    {
        private readonly IMapper _mapper;
        private readonly ITransaction _transaction;
        private readonly IPublicShareOwnerControlClient _publicShareOwnerControlClient;
        private readonly IBankClient _bankClient;
        private readonly IRabbitMqClient _rabbitMqClient;
        private readonly StockTraderBrokerContext _context;
        private readonly ILogger<SellShares> _logger;

        public static readonly Counter SellRequestsCompleted = Metrics.CreateCounter("SellRequestsCompleted", "Total amount of sell requests completed fully");
        public static readonly Counter SellRequestsRemovedByUser = Metrics.CreateCounter("SellRequestsRemovedByUser", "Total amount of sell requests removed by the user");

        public SellShares(StockTraderBrokerContext context, ILogger<SellShares> logger, IMapper mapper, ITransaction transaction, IPublicShareOwnerControlClient publicShareOwnerControlClient, IBankClient bankClient, IRabbitMqClient rabbitMqClient)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _transaction = transaction;
            _publicShareOwnerControlClient = publicShareOwnerControlClient;
            _bankClient = bankClient;
            _rabbitMqClient = rabbitMqClient;
        }

        public async Task<List<SellRequest>> GetSaleRequestsForSpecificOwnerAndStock(Guid ownerId, long stockId)
        {
            return await _context.SellRequests.Where(request => request.StockId == stockId && request.AccountId == ownerId).ToListAsync();
        }

        public async Task<List<SellRequest>> GetSaleRequestsForSpecificStock(long stockId)
        {
            return await _context.SellRequests.Where(request => request.StockId == stockId).ToListAsync();
        }

        public async Task RemoveSellRequest(long id)
        {
            var sellRequest = _context.SellRequests.FirstOrDefault(x => x.Id == id);
            _context.Remove(sellRequest ?? throw new ValidationException($"Failed to remove sell request with id {id}"));
            await _context.SaveChangesAsync();
            SellRequestsRemovedByUser.Inc();
        }
        public async Task<List<SellRequest>> GetSaleRequestsForSpecificOwner(Guid ownerId)
        {
            return await _context.SellRequests.Where(request => request.AccountId == ownerId).ToListAsync();
        }

        public async Task<List<ShareTradingInfo>> AddSellRequestAsync(SellRequestModel sellRequestModel)
        {
            var stockName = await _publicShareOwnerControlClient.GetStockName(sellRequestModel.StockId, "jwtToken");
            _rabbitMqClient.SendMessage(new HistoryMessage { Event = "AddedBuyRequest", EventMessage = $"Sent sell request for {stockName} for {sellRequestModel.AmountOfShares} shares", User = sellRequestModel.AccountId, Timestamp = DateTime.UtcNow });

            var shareTradingInfos = new List<ShareTradingInfo>();
            var buyerListOrderedByPrice = GetBuyerList(sellRequestModel);

            foreach (var buyerRequest in buyerListOrderedByPrice)
            {
                var shareTradingInfo = await SellAsManySharesAsPossible(buyerRequest, sellRequestModel);
                shareTradingInfos.Add(shareTradingInfo);
                if (sellRequestModel.AmountOfShares == 0)
                {
                    SellRequestsCompleted.Inc();
                    break;
                }
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
            if (sharesToSell == buyRequest.AmountOfShares)
            {
                _logger.LogInformation(@"Trying to remove Reservation buyRequest {@buyRequest} sellRequestModel {@sellRequestModel}", buyRequest, sellRequestModel);
                await _bankClient.RemoveReservation(buyRequest.ReserveId, "jwtToken");
            }

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
                BuyShares.BuyRequestsCompleted.Inc();
            }

            return sharesToSell;
        }
    }
}
