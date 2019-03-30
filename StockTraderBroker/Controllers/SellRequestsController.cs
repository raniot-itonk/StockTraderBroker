using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StockTraderBroker.Logic;
using StockTraderBroker.Models;

namespace StockTraderBroker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellRequestsController : ControllerBase
    {
        private readonly ILogger<BuyRequestsController> _logger;
        private readonly ISellShares _sellShares;

        public SellRequestsController(ILogger<BuyRequestsController> logger, ISellShares sellShares)
        {
            _logger = logger;
            _sellShares = sellShares;
        }

        // Add BuyRequest Request
        //[Authorize("BankingService.UserActions")]
        [HttpPost]
        public async Task<ActionResult> PostSellRequest(SellRequestModel sellRequestModel)
        {
            await _sellShares.AddSellRequestAsync(sellRequestModel);
            _logger.LogInformation("Successfully added sell request {@sellRequestModel}", sellRequestModel);
            return Ok();
        }

        [HttpGet]
        public async Task<ActionResult<List<SellRequestModel>>> GetSellRequests([FromQuery] Guid ownerId, string stockId)
        {
            if (ownerId.Equals(Guid.Empty)) return BadRequest("requires OwnerId");

            if (stockId == null)
            {
                var stockRequestsWithSpecificOwner = await _sellShares.GetSaleRequestsForSpecificOwner(ownerId);
                return stockRequestsWithSpecificOwner;
            }

            var stockRequests = await _sellShares.GetSaleRequestsForSpecificOwnerAndStock(ownerId, long.Parse(stockId));
            _logger.LogInformation("User {User} has {sharesForSale} stocks for sale with stockId {stockId}", ownerId, stockRequests.Sum(request => request.AmountOfShares), stockId);
            return stockRequests;
        }
    }
}
