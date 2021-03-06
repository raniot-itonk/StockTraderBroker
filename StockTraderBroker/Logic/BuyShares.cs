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
    public class BuyShares : IBuyShares
    {
        private readonly IMapper _mapper;
        private readonly ITransaction _transaction;
        private readonly IPublicShareOwnerControlClient _publicShareOwnerControlClient;
        private readonly IBankClient _bankClient;
        private readonly IRabbitMqClient _rabbitMqClient;
        private readonly StockTraderBrokerContext _context;
        private readonly ILogger<BuyShares> _logger;

        public static readonly Counter BuyRequestsCompleted = Metrics.CreateCounter("BuyRequestsCompleted", "Total amount of buy requests completed fully");
        public static readonly Counter BuySellRequestsRemovedByUser = Metrics.CreateCounter("BuyRequestsRemovedByUser", "Total amount of buy requests removed by the user");

        public BuyShares(StockTraderBrokerContext context, ILogger<BuyShares> logger, IMapper mapper, ITransaction transaction, IPublicShareOwnerControlClient publicShareOwnerControlClient, IBankClient bankClient, IRabbitMqClient rabbitMqClient)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _transaction = transaction;
            _publicShareOwnerControlClient = publicShareOwnerControlClient;
            _bankClient = bankClient;
            _rabbitMqClient = rabbitMqClient;
        }

        public async Task<List<ShareTradingInfo>> AddBuyRequest(BuyRequestModel buyRequestModel)
        {
            var stockName = await _publicShareOwnerControlClient.GetStockName(buyRequestModel.StockId, "jwtToken");
            _rabbitMqClient.SendMessage(new HistoryMessage { Event = "AddedBuyRequest", EventMessage = $"Sent buy request for {stockName} for {buyRequestModel.AmountOfShares} shares", User = buyRequestModel.AccountId, Timestamp = DateTime.UtcNow });
            var shareTradingInfos = new List<ShareTradingInfo>();
            var sellerListOrderedByPrice = GetSellerList(buyRequestModel);

            foreach (var sellRequest in sellerListOrderedByPrice)
            {
                var shareTradingInfo = await BuyAsManySharesAsPossible(buyRequestModel, sellRequest);
                shareTradingInfos.Add( shareTradingInfo);
                if (buyRequestModel.AmountOfShares == 0)
                    break;
            }

            // Add the rest to the database
            if (buyRequestModel.AmountOfShares != 0)
            {
                var buyRequest = _mapper.Map<BuyRequest>(buyRequestModel);
                _context.BuyRequests.Add(buyRequest);
            }
            _context.SaveChanges();

            return shareTradingInfos;
        }

        public async Task RemoveBuyRequest(long id)
        {
            var buyRequest = _context.BuyRequests.FirstOrDefault(x => x.Id == id);
            _context.Remove(buyRequest ?? throw new ValidationException($"Failed to remove buy request with id {id}"));
            await _context.SaveChangesAsync();
            await _bankClient.RemoveReservation(buyRequest.ReserveId, "jwtToken");
            BuySellRequestsRemovedByUser.Inc();
        }

        public async Task<List<BuyRequest>> GetBuyRequestsForSpecificOwner(Guid ownerId)
        {
            return await _context.BuyRequests.Where(request => request.AccountId == ownerId).ToListAsync();
        }

        private IEnumerable<SellRequest> GetSellerList(BuyRequestModel buyRequestModel)
        {
            return _context.SellRequests.Where(sellRequest =>
                                sellRequest.StockId == buyRequestModel.StockId &&
                                sellRequest.TimeOut > DateTime.Now &&
                                sellRequest.Price <= buyRequestModel.Price).OrderBy(sellRequest => sellRequest.Price).ToList();
        }

        private async Task<ShareTradingInfo> BuyAsManySharesAsPossible(BuyRequestModel buyRequestModel, SellRequest sellRequest)
        {
            var sharesToBuy = CalculateSharesToBuy(buyRequestModel, sellRequest);
            buyRequestModel.AmountOfShares -= sharesToBuy;
            await _transaction.CreateTransactionAsync(sellRequest.Price, sharesToBuy, sellRequest.AccountId, buyRequestModel.ReserveId, buyRequestModel.AccountId, buyRequestModel.StockId);
            if (buyRequestModel.AmountOfShares == 0)
            {
                _logger.LogInformation(@"Trying to remove Reservation buyRequestModel {@buyRequestModel} sellRequest {@sellRequest}", buyRequestModel, sellRequest);
                await _bankClient.RemoveReservation(buyRequestModel.ReserveId, "jwtToken");
                BuyRequestsCompleted.Inc();
            }

            var ownershipRequest = new OwnershipRequest
            {
                Amount = sharesToBuy,
                Buyer = buyRequestModel.AccountId,
                Seller = sellRequest.AccountId
            };
            await _publicShareOwnerControlClient.ChangeOwnership(ownershipRequest, buyRequestModel.StockId, "jwtToken");

            var lastTradedValueRequest = new LastTradedValueRequest { Id = buyRequestModel.StockId, Value = sellRequest.Price };
            await _publicShareOwnerControlClient.UpdateLastTradedValue(lastTradedValueRequest, buyRequestModel.StockId, "jwtToken");

            return new ShareTradingInfo { Price = sellRequest.Price, Amount = sharesToBuy };
        }

        private int CalculateSharesToBuy(BuyRequestModel buyRequestModel, SellRequest sellRequest)
        {
            int sharesToBuy;
            if (sellRequest.AmountOfShares > buyRequestModel.AmountOfShares)
            {
                _context.Attach(sellRequest);
                sellRequest.AmountOfShares = sellRequest.AmountOfShares - buyRequestModel.AmountOfShares;
                sharesToBuy = buyRequestModel.AmountOfShares;
            }
            else
            {
                _context.Remove(sellRequest);
                sharesToBuy = sellRequest.AmountOfShares;
                SellShares.SellRequestsCompleted.Inc();
            }

            return sharesToBuy;
        }
    }
}
