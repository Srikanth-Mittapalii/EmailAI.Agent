using Microsoft.AspNetCore.Mvc;
using EmailAI.Agent.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace EmailAI.Agent.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly IEnumerable<ILLMService> _llmServices;

        public DebugController(IEnumerable<ILLMService> llmServices)
        {
            _llmServices = llmServices;
        }

        [HttpGet("test-ai")]
        public async Task<IActionResult> TestAI()
        {
            var results = new List<object>();
            foreach (var service in _llmServices)
            {
                try
                {
                    var response = await service.Ask("Hi, respond with only the word 'OK'.");
                    results.Add(new { provider = service.ProviderName, success = true, response = response });
                }
                catch (Exception ex)
                {
                    results.Add(new { provider = service.ProviderName, success = false, error = ex.Message });
                }
            }
            return Ok(results);
        }
    }
}
