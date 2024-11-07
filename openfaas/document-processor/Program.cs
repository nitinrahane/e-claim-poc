using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DocumentProcessorApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Register additional services here if needed
                })
                .Build();

            Console.WriteLine("Document processor function started.");

            // Retry logic parameters
            int maxRetries = 5;
            int delayBetweenRetriesInSeconds = 5;
            IConnection connection = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var factory = new ConnectionFactory()
                    {
                        HostName = "rabbitmq", // Use "localhost" if RabbitMQ is running locally outside Docker
                        UserName = "guest",
                        Password = "guest",
                        DispatchConsumersAsync = true // Enable async consumer support
                    };

                    connection = factory.CreateConnection();
                    Console.WriteLine("Successfully connected to RabbitMQ.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt} to connect to RabbitMQ failed: {ex.Message}");
                    if (attempt == maxRetries)
                    {
                        Console.WriteLine("Maximum retry attempts reached. Exiting.");
                        return; // Exit if all attempts fail
                    }

                    Console.WriteLine($"Retrying in {delayBetweenRetriesInSeconds} seconds...");
                    await Task.Delay(delayBetweenRetriesInSeconds * 1000);
                }
            }

            using (connection)
            using (var channel = connection.CreateModel())
            {
                string queueName = "document-processed-queue";  // Ensure this matches the queue name in RabbitMQ
                channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

                Console.WriteLine($"Listening for messages on queue: {queueName}");

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"Received message: {message}");

                    try
                    {
                        // Deserialize the message into a DocumentProcessedEvent object
                        var documentEvent = JsonSerializer.Deserialize<DocumentProcessedEvent>(message);
                        await ProcessDocument(documentEvent);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing message: {ex.Message}");
                    }
                };

                channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

                await host.RunAsync();
            }
        }

        // Process the document based on the information in DocumentProcessedEvent
        private static async Task ProcessDocument(DocumentProcessedEvent documentEvent)
        {
            Console.WriteLine($"Processing document with ID: {documentEvent.DocumentId} for Claim ID: {documentEvent.ClaimId}");
            await Task.Delay(1000); // Simulate some processing delay
            Console.WriteLine($"Document with ID: {documentEvent.DocumentId} processed for Claim ID: {documentEvent.ClaimId}");
        }
    }

    // The DocumentProcessedEvent class matches the event structure
    public class DocumentProcessedEvent
    {
        public string DocumentId { get; set; }
        public string ClaimId { get; set; }
        public string CorrelationId { get; set; }
    }
}
