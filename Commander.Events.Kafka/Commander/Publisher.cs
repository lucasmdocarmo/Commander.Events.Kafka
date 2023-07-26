namespace Commander.Events.Kafka.Services
{
    using Confluent.Kafka;
    using global::Commander.Events.Kafka.Configuration;
    using global::Commander.Events.Kafka.Contracts;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class Publisher<TRequest> : IPublisher<TRequest>
    {
        private readonly IProducer<Null, TRequest> _producer;
        private readonly Lazy<string> _topicName;
        private readonly ILogger<Subscriber<TRequest>> _logger;
        public Publisher(KafkaConfiguration config, ProducerConfig producerConfig,
            ILogger<Subscriber<TRequest>> logger)
        {
            _producer = new ProducerBuilder<Null, TRequest>(producerConfig).
                SetValueSerializer(new Serializer<TRequest>()).Build();

            _topicName = new Lazy<string>(() => config.GetTopicName<TRequest>());
            _logger = logger;
        }

        /// <summary>
        /// Event Publishing
        /// </summary>
        public async Task Publish(TRequest request, CancellationToken ctx, string? topic_name = null)
        {
            ctx.ThrowIfCancellationRequested();
            topic_name ??= _topicName.Value;
            await _producer.ProduceAsync(topic_name, new Message<Null, TRequest>
            {
                Value = request
            });

            _logger.LogInformation($"Content Publish onto topic {topic_name}");
        }

        public void Dispose()
        {
            _producer.Dispose();
        }

    }
}