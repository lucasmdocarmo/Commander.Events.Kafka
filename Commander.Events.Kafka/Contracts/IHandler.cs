namespace Commander.Events.Kafka.Contracts
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IHandler<TRequest>
    {
        Task Handle(TRequest request, CancellationToken ctx);
    }
}
