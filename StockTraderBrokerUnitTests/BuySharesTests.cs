using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using StockTraderBroker;
using Xunit;
using StockTraderBroker.DB;
using StockTraderBroker.Models;

namespace StockTraderBrokerUnitTests
{
    [Collection("Mapper collection")]
    public class BuySharesTests
    {
        private const int DefaultId = 1;

        [Fact]
        public async void AddBuyRequest_Buy2ForPrice1Where3WithPrice1AreAdded_1ShareBoughtFromFirst2InList()
        {
            // Arrange
            var sellRequests = new List<SellRequest>
            {
                new SellRequest{StockId = DefaultId, Price = 1, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)},
                new SellRequest{StockId = DefaultId, Price = 1, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)},
                new SellRequest{StockId = DefaultId, Price = 1, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)}
            }.OrderBy(request => request.Price).ToList();
            var stockBuyRequest = new BuyRequestModel
            {
                StockId = DefaultId,
                Price = 1,
                TimeOut = DateTime.Now,
                AmountOfShares = 2
            };

            var buyShares = TestHelper.SetupBuySharesForTest(sellRequests);
            // Act
            var shareTradingInfos = await buyShares.AddBuyRequest(stockBuyRequest);

            // Assert
            Assert.Collection(shareTradingInfos, 
                info => Assert.Equal(1, info.Amount),
                info => Assert.Equal(1, info.Amount));
        }

        [Fact]
        public async System.Threading.Tasks.Task AddBuyRequest_Buy3ForPrice2Where3WithPrice1x2x3AreAdded_FirstShareBoughtFor1SecondFor2Async()
        {
            // Arrange
            const int price = 2;
            const int amountOfShares = 3;
            var sellRequests = new List<SellRequest>
            {
                new SellRequest{StockId = DefaultId, Price = 1, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)},
                new SellRequest{StockId = DefaultId, Price = 2, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)},
                new SellRequest{StockId = DefaultId, Price = 3, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)}
            }.Where(x => x.Price <= price).OrderBy(request => request.Price).ToList();
            var stockBuyRequest = new BuyRequestModel
            {
                StockId = DefaultId,
                Price = price,
                TimeOut = DateTime.Now,
                AmountOfShares = amountOfShares
            };

            var buyShares = TestHelper.SetupBuySharesForTest(sellRequests);
            // Act
            var shareTradingInfos = await buyShares.AddBuyRequest(stockBuyRequest);

            // Assert
            Assert.Collection(shareTradingInfos,
                info => Assert.Equal(1, info.Price),
                info => Assert.Equal(2, info.Price));
        }
    }
}
