using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StockTraderBroker.Clients;
using StockTraderBroker.Exceptions;
using StockTraderBroker.Models;

namespace StockTraderBroker.Logic
{
    public interface ITransaction
    {
        Task CreateTransactionAsync(double price, int amount, Guid sellerId, Guid reservationId,
            Guid buyerId, long stockId);
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

        public async Task CreateTransactionAsync(double price, int amount, Guid sellerId, Guid reservationId, Guid buyerId, long stockId)
        {
            // Tax
            var stockTaxRequest = new StockTaxRequest
            {
                Price = price,
                Amount = amount,
                ReservationId = reservationId,
                Buyer = buyerId,
                Seller = sellerId,
                StockId = stockId
            };
            var validationResultTaxer = await _tobinTaxerClient.PostStockTax(stockTaxRequest, "jwtToken");
            if (!validationResultTaxer.Valid)
            {
                _logger.LogWarning("Failed to pay taxes with the following error {Error}", validationResultTaxer.ErrorMessage);
                throw new ValidationException(validationResultTaxer.ErrorMessage;
            }
            _logger.LogInformation("Payed taxes, {@stockTaxRequest}", stockTaxRequest);

            // Transfer money from buyer to seller
            var totalAmount = price * amount;
            var transferRequest = new TransferRequest
            {
                Amount = totalAmount,
                FromAccountId = buyerId,
                ReservationId = reservationId,
                ToAccountId = sellerId
            };
            var validationResultTransfer = await _bankClient.CreateTransfer(transferRequest, "jwtToken");
            if (!validationResultTransfer.Valid)
            {
                _logger.LogWarning("Failed to transfer monet from buyer to seller {@transferRequest}", transferRequest);
                throw new ValidationException(validationResultTransfer.ErrorMessage);
            }
            _logger.LogInformation("transferred from money from buyer to seller {@transferRequest}", transferRequest);
        }
    }
}