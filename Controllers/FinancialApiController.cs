using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FinancialInstrumentAPI.Services;

namespace FinancialInstrumentAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FinancialApiController : ControllerBase
    {

        private readonly TiingoFxHttp _fxHttp;

        public FinancialApiController(TiingoFxHttp tiingoFxHttp)
        {
            _fxHttp = tiingoFxHttp;
        }

        [HttpGet("instrument_all")]
        public async Task<IActionResult> getInstruments()
        {
            string ticker = "jpyusd,eurusd,gbpusd";

            var response = await _fxHttp.MainRestQueryTop(ticker);
            if (response == null)
            {
                return BadRequest("Failed to retrieve data from the API.");
            }
            return Ok(response);
        }
        [HttpGet("instrument_item/{ticker}")]
        public async Task<IActionResult> getInstruments_item(string ticker)
        {
            var response = await _fxHttp.MainRestQueryTop(ticker);
            if (response == null)
            {
                return BadRequest("Failed to retrieve data from the API.");
            }
            return Ok(response);
        }
        [HttpGet("instrument_btc")]
        public async Task<IActionResult> getInstrumentTop()
        {
            string ticker = "btcusd";

            var response = await _fxHttp.MainRestCrypToQueryTop(ticker);
            if (response == null)
            {
                return BadRequest("Failed to retrieve data from the API.");
            }
            return Ok(response);
        }
    }
}