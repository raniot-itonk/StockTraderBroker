using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StockTraderBroker.Logic;
using StockTraderBroker.Models;

namespace StockTraderBroker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuyRequestsController : ControllerBase
    {
        private readonly ILogger<BuyRequestsController> _logger;
        private readonly IBuyShares _buyShares;

        public BuyRequestsController(ILogger<BuyRequestsController> logger, IBuyShares buyShares)
        {
            _logger = logger;
            _buyShares = buyShares;
        }

        // Add BuyRequest Request
        //[Authorize("BankingService.UserActions")]
        [HttpPost]
        public async Task<ActionResult> PostBuyRequest(BuyRequestInput buyRequestInput)
        {
             await _buyShares.AddBuyRequest(buyRequestInput);
            _logger.LogInformation("Successfully added buy request {@buyRequestInput}", buyRequestInput);
            return Ok();
        }
    }
}
