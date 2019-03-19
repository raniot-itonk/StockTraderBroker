using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StockTraderBroker.DB;
using StockTraderBroker.Logic;

namespace StockTraderBroker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuyRequestController : ControllerBase
    {
        private readonly StockTraderBrokerContext _context;
        private readonly ILogger<BuyRequestController> _logger;

        private readonly IBusinessLogic _businessLogic;

        public BuyRequestController(StockTraderBrokerContext context, ILogger<BuyRequestController> logger, IBusinessLogic businessLogic)
        {
            _context = context;
            _logger = logger;
            _businessLogic = businessLogic;
        }

        // Add BuyRequest Request
        //[Authorize("BankingService.UserActions")]
        [HttpPost]
        public async Task<ActionResult> PostStock(BuyRequestInput buyRequestInput)
        {

            _businessLogic.AddBuyRequest(buyRequestInput);

            //var stock = new Stock
            //{
            //    LastTradedValue = 0,
            //    Name = stockObject.Name,
            //    ShareHolders = stockObject.Shares
            //};
            //await _context.Stocks.AddAsync(stock);
            //await _context.SaveChangesAsync();

            //_logger.LogInformation("Added Stock {@Stock}", stock);
            return Ok();
        }
    }

    public class BuyRequestInput
    {
        public Guid AccountId { get; set; }
        public long StockId { get; set; }
        public double Price { get; set; }
        public DateTime TimeOut { get; set; }
        public int AmountOfShares { get; set; }
        public Guid ReserveId { get; set; }
    }
}
