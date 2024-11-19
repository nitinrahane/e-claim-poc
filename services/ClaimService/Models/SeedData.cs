using ClaimService.Models;
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
                        Id = 1,
                        ClaimantId = "user1", // Example claimant ID
                        AssessorId = null, // Not assigned yet
                        Type = ClaimType.Motor,
                        Status = ClaimStatus.Raised,
                        Amount = 1200.50M,
                        Description = "Accident damage to car",
                        DocumentIds = new List<Guid>
                        {
                            Guid.NewGuid(), // Placeholder for document references
                            Guid.NewGuid()
                        },
                        CreatedAt = DateTime.UtcNow.AddDays(-7),
                        UpdatedAt = DateTime.UtcNow.AddDays(-7)
                    },
                    new Claim
                    {
                        Id = 2,
                        ClaimantId = "user2",
                        AssessorId = "manager1", // Assigned to a manager
                        Type = ClaimType.Health,
                        Status = ClaimStatus.Validating,
                        Amount = 3200.75M,
                        Description = "Hospitalization for surgery",
                        DocumentIds = new List<Guid>
                        {
                            Guid.NewGuid() // Placeholder for a single document reference
                        },
                        CreatedAt = DateTime.UtcNow.AddDays(-15),
                        UpdatedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new Claim
                    {
                        Id = 3,
                        ClaimantId = "user3",
                        AssessorId = null, // Not assigned yet
                        Type = ClaimType.Property,
                        Status = ClaimStatus.Draft,
                        Amount = null, // Not yet assessed
                        Description = "Water damage to house due to flooding",
                        DocumentIds = new List<Guid>(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                );
                context.SaveChanges();
            }
        }
    }
}
