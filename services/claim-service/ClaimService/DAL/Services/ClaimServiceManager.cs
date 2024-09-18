public class ClaimServiceManager : IClaimService
{
    private readonly IClaimRepository _claimRepository;

    public ClaimServiceManager(IClaimRepository claimRepository)
    {
        _claimRepository = claimRepository;
    }

    public async Task<IEnumerable<Claim>> GetAllClaims()
    {
        return await _claimRepository.GetAllClaims();
    }

    public async Task<Claim> GetClaimById(int id)
    {
        return await _claimRepository.GetClaimById(id);
    }

    public async Task<Claim> CreateClaim(Claim claim)
    {
        return await _claimRepository.CreateClaim(claim);
    }

    public async Task UpdateClaim(Claim claim)
    {
        await _claimRepository.UpdateClaim(claim);
    }

    public async Task DeleteClaim(int id)
    {
        await _claimRepository.DeleteClaim(id);
    }
}
