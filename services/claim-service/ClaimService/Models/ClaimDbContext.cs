using Microsoft.EntityFrameworkCore;

public class ClaimDbContext : DbContext
{
    public ClaimDbContext(DbContextOptions<ClaimDbContext> options) : base(options) {}

    public DbSet<Claim> Claims { get; set; }
}
