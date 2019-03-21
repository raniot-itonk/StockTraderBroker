using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using StockTraderBroker.DB;
using StockTraderBroker.Models;

namespace StockTraderBrokerUnitTests
{
    [Collection("Mapper collection")]
    public class SellSharesTests
    {
        private const int DefaultId = 1;

        [Fact]
        public async void AddSellRequest_Sell2ForPrice1Where3WithPrice1AreAdded_1ShareSoldFromFirst2InList()
        {
            // Arrange
            var data = new List<BuyRequest>
            {
                new BuyRequest{ StockId = DefaultId, Price = 1, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)},
                new BuyRequest{ StockId = DefaultId, Price = 1, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)},
                new BuyRequest{ StockId = DefaultId, Price = 1, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)}
            }.OrderByDescending(request => request.Price).ToList();
            var stockSellRequest = new SellRequestInput()
            {
                StockId = DefaultId,
                Price = 1,
                TimeOut = DateTime.Now,
                AmountOfShares = 2
            };

            var sellShares = TestHelper.SetupSellSharesForTest(data);
            // Act
            var shareTradingInfos = await sellShares.AddSellRequestAsync(stockSellRequest);

            // Assert
            Assert.Collection(shareTradingInfos, 
                info => Assert.Equal(1, info.Amount),
                info => Assert.Equal(1, info.Amount));
        }

        [Fact]
        public async System.Threading.Tasks.Task AddSellRequest_Sell2ForPrice1Where1WithPrice1x2x3AreAdded_FirstShareSoldFor3SecondFor2Async()
        {
            // Arrange
            const int price = 2;
            const int amountOfShares = 3;
            var data = new List<BuyRequest>
            {
                new BuyRequest{ StockId = DefaultId, Price = 1, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)},
                new BuyRequest{ StockId = DefaultId, Price = 2, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)},
                new BuyRequest{ StockId = DefaultId, Price = 3, AmountOfShares = 1, TimeOut = DateTime.Now.AddDays(1)}
            }.Where(x => x.Price >= price).OrderByDescending(request => request.Price).ToList();
            var stockSellRequest = new SellRequestInput()
            {
                StockId = DefaultId,
                Price = price,
                TimeOut = DateTime.Now,
                AmountOfShares = amountOfShares
            };

            var sellShares = TestHelper.SetupSellSharesForTest(data);
            // Act
            var shareTradingInfos = await sellShares.AddSellRequestAsync(stockSellRequest);

            // Assert
            Assert.Collection(shareTradingInfos,
                info => Assert.Equal(3, info.Price),
                info => Assert.Equal(2, info.Price));
        }

        [Fact]
        public async System.Threading.Tasks.Task AddSellRequest_Sell2ForPrice1Where3Buying3WithPrice1_First2boughtAsync()
        {
            // Arrange
            const int price = 1;
            const int amountOfShares = 2;
            var data = new List<BuyRequest>
            {
                new BuyRequest{ StockId = DefaultId, Price = 1, AmountOfShares = 3, TimeOut = DateTime.Now.AddDays(1)},
                new BuyRequest{ StockId = DefaultId, Price = 1, AmountOfShares = 3, TimeOut = DateTime.Now.AddDays(1)},
                new BuyRequest{ StockId = DefaultId, Price = 1, AmountOfShares = 3, TimeOut = DateTime.Now.AddDays(1)}
            }.Where(x => x.Price >= price).OrderByDescending(request => request.Price).ToList();
            var stockSellRequest = new SellRequestInput()
            {
                StockId = DefaultId,
                Price = price,
                TimeOut = DateTime.Now,
                AmountOfShares = amountOfShares
            };

            var sellShares = TestHelper.SetupSellSharesForTest(data);
            // Act
            var shareTradingInfos = await sellShares.AddSellRequestAsync(stockSellRequest);

            // Assert
            Assert.Collection(shareTradingInfos,
                info => Assert.Equal(2, info.Amount));
        }
    }
}
