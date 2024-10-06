using System.Collections.Generic;
using System.Threading.Tasks;

public interface IClaimService
{
    Task<IEnumerable<Claim>> GetAllClaims();
    Task<Claim> GetClaimById(int id);
    Task<Claim> CreateClaim(Claim claim);
    Task UpdateClaim(Claim claim);
    Task DeleteClaim(int id);
}
