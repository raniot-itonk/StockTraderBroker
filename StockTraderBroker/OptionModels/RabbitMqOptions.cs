namespace StockTraderBroker.OptionModels
{
    public class RabbitMqOptions
    {
        public string ExchangeName { get; set; }
        public string RoutingKey { get; set; }
        public string QueueName { get; set; }

        public string HostName { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
    }
}
