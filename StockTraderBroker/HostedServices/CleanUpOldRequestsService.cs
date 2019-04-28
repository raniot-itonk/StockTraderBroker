using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using StockTraderBroker.Clients;
using StockTraderBroker.DB;

namespace StockTraderBroker.HostedServices
{
    internal class CleanUpOldRequestsService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly StockTraderBrokerContext _context;
        private readonly IBankClient _bankClient;
        private Timer _timer;
        private static readonly Counter BuyRequestsRemovedByTimeout = Metrics.CreateCounter("BuyRequestsRemovedByTimeout", "Total buy requests removed by timeout");
        private static readonly Counter SellRequestsRemovedByTimeout = Metrics.CreateCounter("SellRequestsRemovedByTimeout", "Total sell requests removed by timeout");

        public CleanUpOldRequestsService(ILogger<CleanUpOldRequestsService> logger, StockTraderBrokerContext context, IBankClient bankClient)
        {
            _logger = logger;
            _context = context;
            _bankClient = bankClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Clean up old requests service is starting.");

            _timer = new Timer( RemoveOldRequests, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

            return Task.CompletedTask;
        }

        private async void RemoveOldRequests(object state)
        {
            _logger.LogInformation("Clean up old requests service is working.");
            RemoveBuyRequests();
            RemoveSellRequests();
            await _context.SaveChangesAsync();
        }

        private void RemoveSellRequests()
        {
            var sellRequests = _context.SellRequests.Where(request => request.TimeOut < DateTime.Now.ToUniversalTime()).ToList();
            if (!sellRequests.Any()) return;
            _logger.LogInformation(@"Removed the following sellRequests {@sellRequests}", sellRequests);
            SellRequestsRemovedByTimeout.Inc(sellRequests.Count);
            _context.RemoveRange(sellRequests);
        }

        private void RemoveBuyRequests()
        {
            var buyRequests = _context.BuyRequests.Where(request => request.TimeOut < DateTime.Now.ToUniversalTime()).ToList();
            if (!buyRequests.Any()) return;
            buyRequests.ForEach(request => _bankClient.RemoveReservation(request.ReserveId, "jwtToken"));
            _logger.LogInformation(@"Removed the following buyRequests {@buyRequests} and their reservations",buyRequests);
            BuyRequestsRemovedByTimeout.Inc(buyRequests.Count);
            _context.RemoveRange(buyRequests);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Clean up old requests service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
