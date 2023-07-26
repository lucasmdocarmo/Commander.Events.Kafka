namespace Commander.Events.Kafka.Entities
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Commander.Events.Kafka.Contracts;

    public class Handler<THandler, TRequest> : IHandler<TRequest>
    {
        public IServiceProvider Services { get; }

        public Expression<Func<THandler, Func<TRequest, CancellationToken, Task>>> Handle { get; }

        public Handler(IServiceProvider services, Expression<Func<THandler, Func<TRequest, CancellationToken, Task>>> handle)
        {
            Services = services;
            Handle = handle;
        }

        public Handler(IServiceProvider services, MethodInfo methodInfo)
            : this(services, h => (req, ctx) => methodInfo.Invoke(h, new object[] { req, ctx} ) as Task)
        {

        }

        /// <summary>
        /// Processes the request
        /// </summary>
        /// <param name="request">Data Transfer Request Object</param>
        /// <returns></returns>
        Task IHandler<TRequest>.Handle(TRequest request, CancellationToken ctx)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            THandler? handlerService = (THandler)Services.GetService(typeof(THandler));
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            var service = handlerService;
            var handler = Handle.Compile().Invoke(service);
            return handler.Invoke(request, ctx);
        }
    }
}
