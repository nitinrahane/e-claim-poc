using Microsoft.EntityFrameworkCore;
using ClaimService.Models;

public class ClaimRepository : IClaimRepository
{
    private readonly ClaimDbContext _context;

    public ClaimRepository(ClaimDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Claim>> GetAllClaims()
    {
        return await _context.Claims.ToListAsync();
    }

    public async Task<Claim> GetClaimById(int id)
    {
        return await _context.Claims.FindAsync(id);
    }

    public async Task<Claim> CreateClaim(Claim claim)
    {
        _context.Claims.Add(claim);
        await _context.SaveChangesAsync();
        return claim;
    }

    public async Task UpdateClaim(Claim claim)
    {
        _context.Entry(claim).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteClaim(int id)
    {
        var claim = await _context.Claims.FindAsync(id);
        if (claim != null)
        {
            _context.Claims.Remove(claim);
            await _context.SaveChangesAsync();
        }
    }
}
