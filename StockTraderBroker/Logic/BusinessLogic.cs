using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using StockTraderBroker.Clients;
using StockTraderBroker.Controllers;
using StockTraderBroker.DB;
using StockTraderBroker.Models;

namespace StockTraderBroker.Logic
{
    public interface IBusinessLogic
    {
        List<ShareTradingInfo> AddBuyRequest(BuyRequestInput buyRequestInput);
    }

    public class BusinessLogic : IBusinessLogic
    {
        private readonly IMapper _mapper;
        private readonly IBankClient _bankClient;
        private readonly StockTraderBrokerContext _context;

        public BusinessLogic(StockTraderBrokerContext context, IMapper mapper, IBankClient bankClient)
        {
            _context = context;
            _mapper = mapper;
            _bankClient = bankClient;
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
            var amount = sharesToBuy * sellRequest.Price;
            CreateTransaction(amount, buyRequestInput.AccountId, buyRequestInput.ReserveId, sellRequest.AccountId);
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
                sharesToBuy = buyRequestInput.AmountOfShares - sellRequest.AmountOfShares;
            }
        }

        public void CreateTransaction(double amount, Guid fromAccountId, Guid reservationId, Guid toAccountId)
        {
            // Tax


            // Transfer money from buyer to seller
            _bankClient.CreateTransfer(new TransferRequest{}, "jwtToken");
        }
        
    }

    public class ShareTradingInfo
    {
        public double Price { get; set; }
        public int Amount { get; set; }
    }
}
