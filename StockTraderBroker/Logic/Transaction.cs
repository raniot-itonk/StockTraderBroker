using System;
using Microsoft.Extensions.Logging;
using StockTraderBroker.Clients;
using StockTraderBroker.Models;

namespace StockTraderBroker.Logic
{
    public interface ITransaction
    {
        void CreateTransaction(double price, int amount, Guid fromAccountId, Guid reservationId,
            Guid toAccountId, long stockId);
    }

    public class Transaction : ITransaction
    {
        private readonly ILogger<Transaction> _logger;
        private readonly ITobinTaxerClient _tobinTaxerClient;
        private readonly IBankClient _bankClient;

        public Transaction(ILogger<Transaction> logger,ITobinTaxerClient tobinTaxerClient, IBankClient bankClient)
        {
            _logger = logger;
            _tobinTaxerClient = tobinTaxerClient;
            _bankClient = bankClient;
        }

        public void CreateTransaction(double price, int amount, Guid fromAccountId, Guid reservationId,
            Guid toAccountId, long stockId)
        {
            // Tax
            var stockTaxRequest = new StockTaxRequest
            {
                Price = price,
                Amount = amount,
                ReservationId = reservationId,
                Buyer = toAccountId,
                Seller = fromAccountId,
                StockId = stockId
            };
            _tobinTaxerClient.PostStockTax(stockTaxRequest, "jwtToken");
            _logger.LogInformation("Payed taxes, {@stockTaxRequest}", stockTaxRequest);

            // Transfer money from buyer to seller
            var totalAmount = price * amount;
            var transferRequest = new TransferRequest
            {
                Amount = totalAmount,
                FromAccountId = fromAccountId,
                ReservationId = reservationId,
                ToAccountId = toAccountId
            };
            _bankClient.CreateTransfer(transferRequest, "jwtToken");
            _logger.LogInformation("transferred from money from buyer to seller {@transferRequest}", transferRequest);
        }
    }
}