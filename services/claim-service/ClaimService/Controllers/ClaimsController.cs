using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class ClaimsController : ControllerBase
{
    private readonly IClaimService _claimService;

    public ClaimsController(IClaimService claimService)
    {
        _claimService = claimService;
    }

    // GET: api/claims
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Claim>>> GetClaims()
    {
        var claims = await _claimService.GetAllClaims();
        return Ok(claims);
    }

    // GET: api/claims/{id}
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
    [HttpPost]
    public async Task<ActionResult<Claim>> PostClaim(Claim claim)
    {
        var createdClaim = await _claimService.CreateClaim(claim);
        return CreatedAtAction(nameof(GetClaim), new { id = createdClaim.Id }, createdClaim);
    }

    // PUT: api/claims/{id}
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
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClaim(int id)
    {
        await _claimService.DeleteClaim(id);
        return NoContent();
    }
}
