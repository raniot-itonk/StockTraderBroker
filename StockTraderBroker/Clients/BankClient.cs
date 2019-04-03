using System;
using System.Threading.Tasks;
using Flurl;
using Microsoft.Extensions.Options;
using StockTraderBroker.OptionModels;
using Flurl.Http;
using StockTraderBroker.Helpers;
using StockTraderBroker.Models;

namespace StockTraderBroker.Clients
{
    public interface IBankClient
    {
        Task CreateTransfer(TransferRequest request, string jwtToken);
        Task RemoveReservation(Guid id, string jwtToken);
    }

    public class BankClient : IBankClient
    {
        private readonly BankService _bankService;

        public BankClient(IOptionsMonitor<Services> serviceOption)
        {
            _bankService = serviceOption.CurrentValue.BankService ??
                           throw new ArgumentNullException(nameof(serviceOption.CurrentValue.BankService));
        }
        public async Task CreateTransfer(TransferRequest request, string jwtToken)
        {
            await PolicyHelper.ThreeRetriesAsync().ExecuteAsync(() =>
                _bankService.BaseAddress.AppendPathSegment(_bankService.BankPath.Transfer)
                    .WithOAuthBearerToken(jwtToken).PutJsonAsync(request));
        }

        public async Task RemoveReservation(Guid id, string jwtToken)
        {
            await PolicyHelper.ThreeRetriesAsync().ExecuteAsync(() =>
                _bankService.BaseAddress.AppendPathSegments(_bankService.BankPath.Reservation, id)
                    .WithOAuthBearerToken(jwtToken).DeleteAsync());
        }
    }
}
