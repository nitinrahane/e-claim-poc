using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;



[Route("api/[controller]")]
[ApiController]
public class ClaimsController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly ILogger<ClaimsController> _logger;

    public ClaimsController(IClaimService claimService, ILogger<ClaimsController> logger)
    {
        _claimService = claimService;
        _logger = logger;
    }

    [HttpGet("public")]
    public IActionResult PublicEndpoint()
    {
        var roles = User.FindAll("role").Select(r => r.Value);
        _logger.LogInformation("User roles: " + string.Join(", ", roles));

        if (!roles.Contains("user-role"))
        {
            return Unauthorized("Insufficient permissions");
        }

       
        return Ok("Claims present");
    }

    // GET: api/claims
    // [Authorize(Policy = "UserPolicy")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Claim>>> GetClaims()
    {
        var claims = await _claimService.GetAllClaims();
        return Ok(claims);
    }

    // GET: api/claims/{id}
    // [Authorize(Policy = "UserPolicy")]
    [HttpGet("{id}")]
    public async Task<ActionResult<Claim>> GetClaim(int id)
    {
        var claim = await _claimService.GetClaimById(id);
        if (claim == null)
        {
            return NotFound();
        }

        return Ok(claim);
    }

    // POST: api/claims
    // [Authorize(Policy = "UserPolicy")]
    [HttpPost]
    public async Task<ActionResult<Claim>> PostClaim(Claim claim)
    {
        var createdClaim = await _claimService.CreateClaim(claim);
        return CreatedAtAction(nameof(GetClaim), new { id = createdClaim.Id }, createdClaim);
    }

    // PUT: api/claims/{id}
    // [Authorize(Policy = "UserPolicy")]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutClaim(int id, Claim claim)
    {
        if (id != claim.Id)
        {
            return BadRequest();
        }

        await _claimService.UpdateClaim(claim);
        return NoContent();
    }

    // DELETE: api/claims/{id}
    // [Authorize(Policy = "AdminPolicy")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClaim(int id)
    {
        await _claimService.DeleteClaim(id);
        return NoContent();
    }
}
