namespace EClaim.Shared.Interfaces
{
    public interface ICorrelatedEvent
    {
        string CorrelationId { get; set; }
    }
}