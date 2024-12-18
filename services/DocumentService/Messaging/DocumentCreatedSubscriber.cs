using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DocumentService.Models;
using EClaim.Shared.Events;
using DocumentService.Repositories;
using EClaim.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentService.Messaging
{
    public class DocumentCreatedSubscriber : IEventSubscriber, IDisposable
    {
        private IModel _channel;
        private readonly IConnection _connection;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DocumentCreatedSubscriber> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _queueName;
        private readonly string _exchange;
        private readonly string _routingKey;

        public DocumentCreatedSubscriber(IConnection connection, IServiceScopeFactory scopeFactory, ILogger<DocumentCreatedSubscriber> logger, IConfiguration configuration)
        {
            _connection = connection;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;

            // Load RabbitMQ configuration
            var rabbitConfig = configuration.GetSection("RabbitMQ");
            _exchange = rabbitConfig["Exchange"];
            _queueName = rabbitConfig["Queue"];
            _routingKey = rabbitConfig["RoutingKey"];

            if (string.IsNullOrWhiteSpace(_queueName) || string.IsNullOrWhiteSpace(_exchange) || string.IsNullOrWhiteSpace(_routingKey))
            {
                throw new ArgumentNullException("RabbitMQ configuration is incomplete. Ensure Exchange, Queue, and RoutingKey are specified.");
            }

            EnsureChannel(); // Initialize the channel and declare queue
        }

        private void EnsureChannel()
        {
            if (_channel == null || _channel.IsClosed)
            {
                _channel = _connection.CreateModel();

                try
                {
                    _channel.ExchangeDeclare(_exchange, ExchangeType.Direct, durable: true);
                    _channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false);
                    _channel.QueueBind(_queueName, _exchange, _routingKey);

                    // Queue for processed document notifications
                    _channel.QueueDeclare(queue: "document-processed-queue", durable: true, exclusive: false, autoDelete: false);
                    _channel.QueueBind(queue: "document-processed-queue", exchange: _exchange, routingKey: "document.processed");

                    _logger.LogInformation($"Declared exchange '{_exchange}', queue '{_queueName}' for processing, and 'document-processed-queue' for processed events.");
                    _logger.LogInformation($"Declared exchange '{_exchange}', queue '{_queueName}', and binding with routing key '{_routingKey}'.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to declare exchange or queue: {ex.Message}");
                    throw;
                }
            }
        }

        public void StartListening()
        {
            EnsureChannel(); // Ensure the channel is available before listening

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var claimEvent = JsonSerializer.Deserialize<ClaimCreatedEvent>(message);
                    _logger.LogInformation($"Received claim created event for Claim ID: {claimEvent.ClaimId}");

                    // Create a scope for processing each message
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                        await HandleDocumentProcessing(claimEvent, documentRepository);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing message: {ex.Message}");
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);
            _logger.LogInformation($"Started listening for messages on queue '{_queueName}'.");
        }

        private async Task HandleDocumentProcessing(ClaimCreatedEvent claimEvent, IDocumentRepository documentRepository)
        {
            _logger.LogInformation($"Processing document for Claim ID: {claimEvent.ClaimId}");

            var newDocument = new Document
            {
                Title = $"Document for Claim {claimEvent.ClaimId}",
                FilePath = $"/local-storage/documents/{claimEvent.ClaimId}-document.pdf", // Example file path
                ContentType = "application/pdf", // Assume a default MIME type for now
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OwnerId = claimEvent.ClaimantId // Assuming `ClaimantId` is available in the event
            };
            await documentRepository.CreateDocumentAsync(newDocument);
            _logger.LogInformation($"Document created in database for Claim ID: {claimEvent.ClaimId}");
            PublishDocumentProcessedEvent(claimEvent.ClaimId, newDocument.Id.ToString(), claimEvent.CorrelationId);
        }

        private void PublishDocumentProcessedEvent(string claimId, string documentId, string correlationId)
        {
            var processedEvent = new DocumentProcessedEvent
            {
                ClaimId = claimId,
                DocumentId = documentId,
                CorrelationId = correlationId, // Ensuring traceability with CorrelationId
            };

            var message = JsonSerializer.Serialize(processedEvent);
            var body = Encoding.UTF8.GetBytes(message);

            // Publish the event to RabbitMQ
            _channel.BasicPublish(
                exchange: _exchange,
                routingKey: "document.processed",  // Routing key for processed documents
                basicProperties: null,
                body: body
            );

            _logger.LogInformation($"Published DocumentProcessedEvent for Document ID: {documentId}, Claim ID: {claimId}");
        }


        public void Dispose()
        {
            if (_channel != null && !_channel.IsClosed)
            {
                _channel.Close();
                _channel.Dispose();
            }
        }


    }
}
