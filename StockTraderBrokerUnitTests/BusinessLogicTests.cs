using System;
using System.Collections.Generic;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Moq;
using Moq.EntityFrameworkCore;
using StockTraderBroker;
using StockTraderBroker.Controllers;
using StockTraderBroker.DB;
using StockTraderBroker.Logic;

namespace StockTraderBrokerUnitTests
{
    public class BusinessLogicTests
    {
        private const int DefaultId = 1;
        private readonly SellRequest _defaultSellRequest = new SellRequest
            {StockId = DefaultId, Price = 1, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)};

        [Fact]
        public void AddBuyRequest_Buy2ForPrice1Where3WithPrice1AreAdded_1ShareBoughtFromFirst2InList()
        {
            var data = new List<SellRequest>
            {
                _defaultSellRequest,
                _defaultSellRequest,
                _defaultSellRequest
            };
            var stockBuyRequest = new BuyRequestInput
            {
                StockId = DefaultId,
                Price = 1,
                TimeOut = DateTime.Now,
                AmountOfShares = 2
            };

            var businessLogic = SetupBusinessLogicForTest(data);
            var shareTradingInfos = businessLogic.AddBuyRequest(stockBuyRequest);

            Assert.Collection(shareTradingInfos, 
                info => Assert.Equal(1, info.Amount),
                info => Assert.Equal(1, info.Amount));
        }

        private static BusinessLogic SetupBusinessLogicForTest(List<SellRequest> data)
        {
            var stockTraderBrokerMock = new Mock<StockTraderBrokerContext>(new DbContextOptions<StockTraderBrokerContext>());
            stockTraderBrokerMock.Setup(x => x.SellRequests).ReturnsDbSet(data);

            Mapper.Initialize(expression => expression.AddProfile(new MappingProfile()));
            var instance = Mapper.Instance;

            var businessLogic = new BusinessLogic(stockTraderBrokerMock.Object, instance);
            return businessLogic;
        }
    }
}
