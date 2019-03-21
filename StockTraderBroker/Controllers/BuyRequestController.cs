using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StockTraderBroker.Logic;
using StockTraderBroker.Models;

namespace StockTraderBroker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuyRequestController : ControllerBase
    {
        private readonly ILogger<BuyRequestController> _logger;
        private readonly IBuyShares _buyShares;

        public BuyRequestController(ILogger<BuyRequestController> logger, IBuyShares buyShares)
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
