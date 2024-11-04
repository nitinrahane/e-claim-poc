
using EClaim.Shared.Interfaces;
namespace EClaim.Shared.Events
{

    public class DocumentProcessedEvent : ICorrelatedEvent
    {
        public string DocumentId { get; set; }
        public string ClaimId { get; set; }
        public string CorrelationId { get; set; }
    }
}