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
    public interface ITobinTaxerClient
    {
        Task PostStockTax(StockTaxRequest request, string jwtToken);
    }

    public class TobinTaxerClient : ITobinTaxerClient
    {
        private readonly TobinTaxer _tobinTaxer;

        public TobinTaxerClient(IOptionsMonitor<Services> serviceOption)
        {
            _tobinTaxer = serviceOption.CurrentValue.TobinTaxer ??
                           throw new ArgumentNullException(nameof(serviceOption.CurrentValue.TobinTaxer));
        }
        public async Task PostStockTax(StockTaxRequest request, string jwtToken)
        {
            await PolicyHelper.ThreeRetriesAsync().ExecuteAsync(() =>
                _tobinTaxer.BaseAddress.AppendPathSegment(_tobinTaxer.TobinTaxerPath.StockTax)
                    .WithOAuthBearerToken(jwtToken).PostJsonAsync(request));
        }
    }
}
