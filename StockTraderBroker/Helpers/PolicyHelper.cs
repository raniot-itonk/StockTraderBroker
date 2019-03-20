using System;
using Flurl.Http;
using Polly;
using Polly.Retry;

namespace StockTraderBroker.Helpers
{
    public class PolicyHelper
    {
        public static AsyncRetryPolicy ThreeRetriesAsync()
        {
            return Policy.Handle<FlurlHttpException>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(3),
                });
        }
    }
}
