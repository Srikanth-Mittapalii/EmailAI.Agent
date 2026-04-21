using EmailAI.Agent.Agents;
using EmailAI.Agent.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EmailAI.Agent.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly AgentOrchestrator _orchestrator;

        public QueryController(AgentOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        [HttpPost]
        public async Task<IActionResult> QueryEmails([FromBody] QueryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { error = "Query cannot be empty" });

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var response = await _orchestrator.ProcessQuery(request.Query, userId, request.Limit);
            return Ok(response);
        }
    }
}
