using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StockTraderBroker.Logic;
using StockTraderBroker.Models;

namespace StockTraderBroker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellRequestController : ControllerBase
    {
        private readonly ILogger<BuyRequestController> _logger;
        private readonly ISellShares _sellShares;

        public SellRequestController(ILogger<BuyRequestController> logger, ISellShares sellShares)
        {
            _logger = logger;
            _sellShares = sellShares;
        }

        // Add BuyRequest Request
        //[Authorize("BankingService.UserActions")]
        [HttpPost]
        public async Task<ActionResult> PostSellRequest(SellRequestInput sellRequestInput)
        {
            _sellShares.AddSellRequest(sellRequestInput);
            _logger.LogInformation("Successfully added sell request {@sellRequestInput}", sellRequestInput);
            return Ok();
        }
    }
}
