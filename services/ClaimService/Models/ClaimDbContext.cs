using Microsoft.EntityFrameworkCore;
using ClaimService.Models;
public class ClaimDbContext : DbContext
{
    public ClaimDbContext(DbContextOptions<ClaimDbContext> options) : base(options) {}

    public DbSet<Claim> Claims { get; set; }
}