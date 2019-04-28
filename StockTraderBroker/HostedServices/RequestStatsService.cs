using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using StockTraderBroker.DB;

namespace StockTraderBroker.HostedServices
{
    public class RequestStatsService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly StockTraderBrokerContext _context;
        private Timer _timer;
        private static readonly Gauge CurrentBuyRequests = Metrics.CreateGauge("TotalBuyRequestsRemovedByTimeout", "Total buy requests removed by timeout");
        private static readonly Gauge CurrentSellRequests = Metrics.CreateGauge("TotalSellRequestsRemovedByTimeout", "Total sell requests removed by timeout");

        public RequestStatsService(ILogger<RequestStatsService> logger, StockTraderBrokerContext context)
        {
            _logger = logger;
            _context = context;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Request Stats services is starting.");

            _timer = new Timer(UpdateRequestStats, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            return Task.CompletedTask;
        }

        private async void UpdateRequestStats(object state)
        {
            var sellRequestsCount = await _context.SellRequests.LongCountAsync();
            var buyRequestsCount = await _context.BuyRequests.LongCountAsync();
            _logger.LogInformation("Current requests - sell: {sellRequests} - buy {buyRequests}", sellRequestsCount, buyRequestsCount);
            CurrentSellRequests.Set(sellRequestsCount);
            CurrentBuyRequests.Set(buyRequestsCount);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Request Stats services is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}