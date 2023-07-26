namespace Commander.Events.Kafka.Services
{
    using Confluent.Kafka;
    using global::Commander.Events.Kafka.Configuration;
    using global::Commander.Events.Kafka.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Subscription with kafka
    /// </summary>
    /// <typeparam name="TRequest">Tipo da Requisição</typeparam>
    public class Subscriber<TRequest> : BackgroundService
    {
        private readonly KafkaConfiguration _config;
        private readonly ILogger<Subscriber<TRequest>> _logger;
        private readonly IServiceProvider _services;
        private IConsumer<Ignore, TRequest> _consumer;

        /// <summary>
        /// Subiscription of syncronous events on kafka 
        /// </summary>
        /// <param name="config">Parâmetros de ambiente</param>
        /// <param name="consumer">Connection</param>
        /// <param name="logger">netcore Logger</param>
        public Subscriber(KafkaConfiguration config, ConsumerConfig consumer, ILogger<Subscriber<TRequest>> logger,
            IServiceProvider serviceProvider)
        {
            _consumer = new ConsumerBuilder<Ignore, TRequest>(consumer)
                .SetValueDeserializer(new Serializer<TRequest>())
                .Build();
            _config = config;
            _logger = logger;
            _services = serviceProvider;
        }


        /// <summary>
        /// Message Consumer
        /// </summary>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var topicName = _config.GetTopicName<TRequest>();
            _consumer.Subscribe(topicName);

            return Task.Factory.StartNew(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var message = _consumer.Consume();
                        try
                        {
                            if (_config.IsValidScoped())
                            {
                                using var scope = _services.CreateScope();
                                await CallService(message.Message.Value, scope.ServiceProvider, stoppingToken);
                            }
                            else
                            {
                                await CallService(message.Message.Value, _services, stoppingToken);
                            }
                            _logger?.LogInformation($"Read message {message.Message.Key} with success.");
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError($"Error on read message {message.Message.Key}", ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogInformation("Subscriber canceled by requestor", ex);
                    }
                }
            }, stoppingToken);
        }

        /// <summary>
        /// Call Handler Processer
        /// </summary>
        private static async Task CallService(TRequest @event, IServiceProvider currentService, CancellationToken ctx)
        {
            var requestHandler = currentService.GetService(typeof(IHandler<TRequest>)) as IHandler<TRequest>;
            await requestHandler.Handle(@event, ctx);
        }


        public override void Dispose()
        {
            _consumer.Dispose();
            base.Dispose();
        }
    }
}