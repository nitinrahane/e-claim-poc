// EClaim.Shared/Messaging/IEventPublisher.cs
using EClaim.Shared.Interfaces;
namespace EClaim.Shared.Messaging
{
    public interface IEventPublisher
    {
        void Publish<T>(T @event, string exchange, string routingKey) where T : class, ICorrelatedEvent;
    }
}
