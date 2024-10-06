public static class SeedData
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using (var context = serviceProvider.GetRequiredService<ClaimDbContext>())
        {
            // Check if any Claims exist
            if (!context.Claims.Any())
            {
                context.Claims.AddRange(
                    new Claim
                    {
                        PolicyNumber = "POLICY001",
                        ClaimStatus = "Pending",
                        ClaimAmount = 1500.00M,
                        DateOfClaim = DateTime.UtcNow.AddDays(-10) // Use UtcNow
                    },
                    new Claim
                    {
                        PolicyNumber = "POLICY002",
                        ClaimStatus = "Approved",
                        ClaimAmount = 2500.00M,
                        DateOfClaim = DateTime.UtcNow.AddDays(-5) // Use UtcNow
                    }
                );
                context.SaveChanges();
            }
        }
    }
}
