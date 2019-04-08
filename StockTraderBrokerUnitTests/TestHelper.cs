using System.Collections.Generic;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using StockTraderBroker.Clients;
using StockTraderBroker.DB;
using StockTraderBroker.Logic;

namespace StockTraderBrokerUnitTests
{
    public class TestHelper
    {
        public static SellShares SetupSellSharesForTest(List<BuyRequest> data)
        {
            var stockTraderBrokerMock = new Mock<StockTraderBrokerContext>(new DbContextOptions<StockTraderBrokerContext>());
            stockTraderBrokerMock.Setup(x => x.BuyRequests).ReturnsDbSet(data);
            stockTraderBrokerMock.Setup(x => x.SellRequests).ReturnsDbSet(new List<SellRequest>());

            var instance = Mapper.Instance;
            var loggerMock = new Mock<ILogger<SellShares>>();
            var transactionMock = new Mock<ITransaction>();
            var publicShareOwnerControlMock = new Mock<IPublicShareOwnerControlClient>();
            var bankClientMock = new Mock<IBankClient>();

            var businessLogic = new SellShares(stockTraderBrokerMock.Object, loggerMock.Object, instance, transactionMock.Object, publicShareOwnerControlMock.Object, bankClientMock.Object);
            return businessLogic;
        }


        public static BuyShares SetupBuySharesForTest(List<SellRequest> data)
        {
            var stockTraderBrokerMock = new Mock<StockTraderBrokerContext>(new DbContextOptions<StockTraderBrokerContext>());
            stockTraderBrokerMock.Setup(x => x.SellRequests).ReturnsDbSet(data);
            stockTraderBrokerMock.Setup(x => x.BuyRequests).ReturnsDbSet(new List<BuyRequest>());

            var instance = Mapper.Instance;
            var loggerMock = new Mock<ILogger<BuyShares>>();
            var transactionMock = new Mock<ITransaction>();
            var publicShareOwnerControlMock = new Mock<IPublicShareOwnerControlClient>();
            var bankClientMock = new Mock<IBankClient>();

            var businessLogic = new BuyShares(stockTraderBrokerMock.Object, loggerMock.Object, instance, transactionMock.Object, publicShareOwnerControlMock.Object, bankClientMock.Object);
            return businessLogic;
        }
    }
}