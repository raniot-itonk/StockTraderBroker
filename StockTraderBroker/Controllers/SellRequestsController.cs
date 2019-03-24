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
        public async Task<ActionResult> PostSellRequest(SellRequestInput sellRequestInput)
        {
            await _sellShares.AddSellRequestAsync(sellRequestInput);
            _logger.LogInformation("Successfully added sell request {@sellRequestInput}", sellRequestInput);
            return Ok();
        }
    }
}
