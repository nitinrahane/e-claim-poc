// EClaim.Shared/Messaging/RabbitMqEventPublisher.cs
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using EClaim.Shared.Interfaces;

namespace EClaim.Shared.Messaging
{
    public class RabbitMqEventPublisher : IEventPublisher
    {
        private readonly IModel _channel;

        public RabbitMqEventPublisher(IModel channel)
        {
            _channel = channel;
        }

        public void Publish<T>(T @event, string exchange, string routingKey) where T : class, ICorrelatedEvent
        {
            var messageBody = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(messageBody);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.Headers = new Dictionary<string, object> { { "CorrelationId", @event.CorrelationId } };

            _channel.BasicPublish(exchange: exchange,
                                  routingKey: routingKey,
                                  basicProperties: properties,
                                  body: body);
        }
    }
}
