namespace Commander.Events.Kafka.Contracts
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Contract Interface for Publish Handler
    /// </summary>
    /// <typeparam name="TRequest">Domain Model</typeparam>
    public interface IPublisher<TRequest> : IDisposable
    {
        Task Publish(TRequest request, CancellationToken ctx, string? topic_name = default);
    }
}