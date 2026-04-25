using EmailAI.Agent.Agents;
using EmailAI.Agent.Models;
using EmailAI.Agent.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EmailAI.Agent.Controllers
{
    public class IngestRequest
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Sender { get; set; }
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly EmailAgent _emailAgent;
        private readonly VectorDbService _vectorDbService;
        private readonly GmailService _gmailService;

        public EmailController(EmailAgent emailAgent, VectorDbService vectorDbService, GmailService gmailService)
        {
            _emailAgent = emailAgent;
            _vectorDbService = vectorDbService;
            _gmailService = gmailService;
        }

        [HttpPost("sync-gmail")]
        public async Task<IActionResult> SyncGmail([FromBody] GmailSyncRequest request)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                int count = await _gmailService.SyncLatestEmails(userId, request.AccessToken, request.Limit);
                return Ok(new { success = true, syncedCount = count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        public class GmailSyncRequest
        {
            public string AccessToken { get; set; } = null!;
            public int Limit { get; set; } = 100;
        }

        [HttpPost("ingest")]
        public async Task<IActionResult> IngestEmail([FromBody] IngestRequest request)
        {
            try
            {
                string subject = request.Subject;
                string body = request.Body;
                string sender = request.Sender ?? "unknown@example.com";
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var email = await _emailAgent.ProcessEmail(subject, body, sender, userId);

                return Ok(new
                {
                    success = true,
                    emailId = email.Id.ToString(),
                    processing = new
                    {
                        category = email.Category,
                        summary = email.Summary,
                        action = email.Action
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{emailId}")]
        public async Task<IActionResult> GetEmail(string emailId)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var email = await _vectorDbService.GetEmailById(emailId, userId);
                return email == null ? NotFound() : Ok(email);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListEmails()
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var emails = await _vectorDbService.GetAllEmails(userId);
                return Ok(emails);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
