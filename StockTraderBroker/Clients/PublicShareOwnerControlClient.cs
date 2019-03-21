using System;
using System.Threading.Tasks;
using Flurl;
using Microsoft.Extensions.Options;
using StockTraderBroker.OptionModels;
using Flurl.Http;
using StockTraderBroker.Helpers;

namespace StockTraderBroker.Clients
{
    public interface IPublicShareOwnerControlClient
    {
        Task ChangeOwnership(OwnershipRequest request, long id, string jwtToken);
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
    }

    public class OwnershipRequest
    {
        public Guid Seller { get; set; }
        public Guid Buyer { get; set; }
        public int Amount { get; set; }
    }
}
