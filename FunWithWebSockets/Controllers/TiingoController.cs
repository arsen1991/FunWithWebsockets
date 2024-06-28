using FunWithWebSockets.Services;
using Microsoft.AspNetCore.Mvc;

namespace FunWithWebSockets.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TiingoController : ControllerBase
    {
        private readonly TiingoIntegrationService _tiingoIntegrationService;

        public TiingoController(TiingoIntegrationService tiingoIntegrationService)
        {
            _tiingoIntegrationService = tiingoIntegrationService;
        }

        [HttpGet("Tickers")]
        public async Task<ActionResult> TestAsync()
        {
            return Ok(_tiingoIntegrationService.GetAvailableTickers());
        }

        [HttpGet("CurrentPrice/{from}/{to}")]
        public async Task<ActionResult> GetCurrentPriceAsync(string from, string to)
        {
            return Ok(await _tiingoIntegrationService.GetCurrentPriceAsync(from, to));
        }
    }
}
