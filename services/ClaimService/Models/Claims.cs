public class Claim
{
    public int Id { get; set; }
    public string? PolicyNumber { get; set; }
    public string? ClaimStatus { get; set; }
    public decimal ClaimAmount { get; set; }
    public DateTime DateOfClaim { get; set; }
}
