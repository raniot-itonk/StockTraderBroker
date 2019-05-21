using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using StockTraderBroker.Models;
using StockTraderBroker.OptionModels;

namespace StockTraderBroker.Clients
{
    public class RabbitMqClient : IDisposable, IRabbitMqClient
    {
        private readonly ILogger<RabbitMqClient> _logger;
        private readonly RabbitMqOptions _rabbitMqOptions;
        private IModel _channel;
        private IConnection _connection;

        public RabbitMqClient(ILogger<RabbitMqClient> logger, IOptionsMonitor<RabbitMqOptions> rabbitMqOptions)
        {
            _logger = logger;
            _rabbitMqOptions = rabbitMqOptions.CurrentValue;
            SetupRabbitMq();
        }

        private void SetupRabbitMq()
        {
            (_channel, _connection) = CreateRabbitMqChannel();
            DeclareQueueAndExchange();
        }

        private void DeclareQueueAndExchange()
        {
            _channel.ExchangeDeclare(_rabbitMqOptions.ExchangeName, ExchangeType.Direct);
            _channel.QueueDeclare(_rabbitMqOptions.QueueName, false, false, false, null);
            _channel.QueueBind(_rabbitMqOptions.QueueName, _rabbitMqOptions.ExchangeName, _rabbitMqOptions.RoutingKey, null);
        }

        public void SendMessage(HistoryMessage historyMessage)
        {
            var message = JsonConvert.SerializeObject(historyMessage);
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(_rabbitMqOptions.ExchangeName, _rabbitMqOptions.RoutingKey, null, body);
            _logger.LogDebug("Sent message to ");
        }

        private (IModel channel, IConnection connection) CreateRabbitMqChannel()
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqOptions.HostName,
                UserName = _rabbitMqOptions.User,
                Password = _rabbitMqOptions.Password,
                VirtualHost = _rabbitMqOptions.VirtualHost
            };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            return (channel, connection);
        }

        public void Dispose()
        {
            _connection.Close();
            _channel.Close();
        }
    }
}
