using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StockTraderBroker.DB;
using StockTraderBroker.Exceptions;
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
        public async Task<ActionResult<ValidationResult>> PostBuyRequest(BuyRequestModel buyRequestModel)
        {
            try
            {
                buyRequestModel.TimeOut = buyRequestModel.TimeOut.ToUniversalTime();
                await _buyShares.AddBuyRequest(buyRequestModel);
                _logger.LogInformation("Successfully added buy request {@buyRequestModel}", buyRequestModel);
                return new ValidationResult{Valid = true, ErrorMessage = ""};
            }
            catch (ValidationException e)
            {
                return new ValidationResult{Valid = false, ErrorMessage = e.Message};
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ValidationResult>> DeleteBuyRequest(long id)
        {
            try
            {
                await _buyShares.RemoveBuyRequest(id);
                _logger.LogInformation("Successfully Removed buy request with id {id}", id);
                return new ValidationResult { Valid = true, ErrorMessage = "" };
            }
            catch (ValidationException e)
            {
                return new ValidationResult { Valid = false, ErrorMessage = e.Message };
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<BuyRequest>>> GetBuyRequests([FromQuery] Guid ownerId)
        {
            if (ownerId.Equals(Guid.Empty)) return BadRequest("requires OwnerId");

            return await _buyShares.GetBuyRequestsForSpecificOwner(ownerId);
        }
    }
}
