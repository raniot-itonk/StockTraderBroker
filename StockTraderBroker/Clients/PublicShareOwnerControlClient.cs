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
    public interface IPublicShareOwnerControlClient
    {
        Task ChangeOwnership(OwnershipRequest request, long id, string jwtToken);
        Task UpdateLastTradedValue(LastTradedValueRequest request, long id, string jwtToken);
        Task<string> GetStockName(long id, string jwtToken);
    }

    public class PublicShareOwnerControlClient : IPublicShareOwnerControlClient
    {
        private readonly PublicShareOwnerControl _publicShareOwnerControl;

        public PublicShareOwnerControlClient(IOptionsMonitor<Services> serviceOption)
        {
            _publicShareOwnerControl = serviceOption.CurrentValue.PublicShareOwnerControl ??
                           throw new ArgumentNullException(nameof(serviceOption.CurrentValue.PublicShareOwnerControl));
        }
        public async Task ChangeOwnership(OwnershipRequest request, long id, string jwtToken)
        {
            await PolicyHelper.ThreeRetriesAsync().ExecuteAsync(() =>
                _publicShareOwnerControl.BaseAddress
                    .AppendPathSegments(_publicShareOwnerControl.PublicSharePath.Stock,id, "ownership")
                    .WithOAuthBearerToken(jwtToken).PutJsonAsync(request));
        }
        public async Task UpdateLastTradedValue(LastTradedValueRequest request, long id, string jwtToken)
        {
            await PolicyHelper.ThreeRetriesAsync().ExecuteAsync(() =>
                _publicShareOwnerControl.BaseAddress
                    .AppendPathSegments(_publicShareOwnerControl.PublicSharePath.Stock, id, "LastTradedValue")
                    .WithOAuthBearerToken(jwtToken).PutJsonAsync(request));
        }
        public async Task<string> GetStockName(long id, string jwtToken)
        {
            return await PolicyHelper.ThreeRetriesAsync().ExecuteAsync(() =>
                _publicShareOwnerControl.BaseAddress
                    .AppendPathSegments(_publicShareOwnerControl.PublicSharePath.Stock, id, "name")
                    .WithOAuthBearerToken(jwtToken).GetStringAsync());
        }
    }
}
