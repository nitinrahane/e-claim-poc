// EClaim.Shared/Events/ClaimCreatedEvent.cs
using EClaim.Shared.Interfaces;

namespace EClaim.Shared.Events
{
    public class ClaimCreatedEvent : ICorrelatedEvent
    {
        public string ClaimId { get; set; }        
        public DateTime CreatedAt { get; set; }        
        public string CorrelationId { get; set; }
        public string ClaimantId { get; set; }
    }
}
