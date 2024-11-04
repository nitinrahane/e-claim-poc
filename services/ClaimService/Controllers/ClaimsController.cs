using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EClaim.Shared.Events;
using EClaim.Shared.Messaging;
using System.Security.Claims;
using Elasticsearch.Net.Specification.SecurityApi;

[Route("api/[controller]")]
[ApiController]
public class ClaimsController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly ILogger<ClaimsController> _logger;
    private readonly IEventPublisher _eventPublisher;

    public ClaimsController(IClaimService claimService, ILogger<ClaimsController> logger, IEventPublisher eventPublisher)
    {
        _claimService = claimService;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    // This endpoint is public, no authentication required
    [HttpGet("public")]
    public IActionResult PublicEndpoint()
    {
        var roles = User.FindAll("role").Select(r => r.Value);
        _logger.LogInformation("User roles: " + string.Join(", ", roles));

        // This is a public endpoint, no role-based check is required
        return Ok("This is a public endpoint. No authentication required.");
    }

    [HttpGet]
    [Route("test-log")]
    public IActionResult TestLog()
    {
        _logger.LogInformation("Test log entry generated from API endpoint");
        return Ok("Log entry generated");
    }

    // GET: api/claims
    // This endpoint is for "Customer" role
    [Authorize(Policy = "CustomerPolicy")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Claim>>> GetClaims()
    {
        _logger.LogInformation("Getting claims for customer");
        var claims = await _claimService.GetAllClaims();
        return Ok(claims);
    }

    // GET: api/claims/{id}
    // This endpoint is for "Customer" role
    [Authorize(Policy = "CustomerPolicy")]
    [HttpGet("{id}")]
    public async Task<ActionResult<Claim>> GetClaim(int id)
    {
        _logger.LogInformation($"Getting claim with ID: {id}");
        var claim = await _claimService.GetClaimById(id);
        if (claim == null)
        {
            return NotFound();
        }

        return Ok(claim);
    }

    // POST: api/claims
    // This endpoint is for "Customer" role
    [Authorize(Policy = "CustomerPolicy")]
    [HttpPost]
    public async Task<ActionResult<Claim>> PostClaim(Claim claim)
    {
        _logger.LogInformation($"Creating new claim for customer: {claim}");
        var userId = HttpContext.User.FindFirst("sub")?.Value;

        var createdClaim = await _claimService.CreateClaim(claim);
        var claimCreatedEvent = new ClaimCreatedEvent
        {
            ClaimId = createdClaim.Id.ToString(),
            UserId = GetUserIdFromJwt(),
            CreatedAt = DateTime.UtcNow,
            //  DocumentId = claim.DocumentId,
            CorrelationId = GetCorrelationIdFromHeader()
        };

        _eventPublisher.Publish(claimCreatedEvent, "claims_exchange", "claims.created");


        return CreatedAtAction(nameof(GetClaim), new { id = createdClaim.Id }, createdClaim);
    }

    // PUT: api/claims/{id}
    // This endpoint is for "Customer" role
    [Authorize(Policy = "CustomerPolicy")]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutClaim(int id, Claim claim)
    {
        _logger.LogInformation($"Updating claim with ID: {id}");
        if (id != claim.Id)
        {
            return BadRequest();
        }

        await _claimService.UpdateClaim(claim);
        return NoContent();
    }

    // DELETE: api/claims/{id}
    // This endpoint is for "Admin" role only
    [Authorize(Policy = "AdminPolicy")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClaim(int id)
    {
        _logger.LogInformation($"Deleting claim with ID: {id}");
        await _claimService.DeleteClaim(id);
        return NoContent();
    }

    private string GetUserIdFromJwt()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "UnknownUser";
    }

    private string GetCorrelationIdFromHeader()
    {
        if (Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            return correlationId.ToString();
        }
        return Guid.NewGuid().ToString(); // Generate new Correlation ID if not provided
    }
}
