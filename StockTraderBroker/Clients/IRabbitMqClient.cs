using StockTraderBroker.Models;

namespace StockTraderBroker.Clients
{
    public interface IRabbitMqClient
    {
        void SendMessage(HistoryMessage historyMessage);
    }
}