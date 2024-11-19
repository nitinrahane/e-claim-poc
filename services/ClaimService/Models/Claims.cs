using System;
using System.Collections.Generic;

namespace ClaimService.Models
{
    public enum ClaimStatus
    {
        Draft,
        Raised,
        Validating,
        Assessed,
        Granted
    }

    public enum ClaimType
    {
        Motor,
        Health,
        Property,
        Travel,
        Life
    }

    public class Claim
    {
        public int Id { get; set; } // Unique identifier for the claim
        public string ClaimantId { get; set; } // User ID of the claimant
        public string? AssessorId { get; set; } // Optional ID of the assigned assessor
        public ClaimType Type { get; set; } = ClaimType.Motor; // Restricted to predefined claim types
        public ClaimStatus Status { get; set; } = ClaimStatus.Draft;
        public decimal? Amount { get; set; } // Amount being claimed
        public string? Description { get; set; } // Additional details about the claim
        public List<Guid> DocumentIds { get; set; } = new List<Guid>(); // References to documents in the DocumentService
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
