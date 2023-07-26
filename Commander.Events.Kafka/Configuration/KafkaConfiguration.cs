using Commander.Events.Kafka.Contracts;
using Commander.Events.Kafka.Extensions.Shared;
using Confluent.Kafka;
using Commander.Events.Kafka.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using Commander.Events.Kafka.Services;
using Commander.Events.Kafka.Attributes;

namespace Commander.Events.Kafka.Configuration
{
    public class KafkaConfiguration : BaseKafkaConfiguration
    {
        private const char _topicNameSeparator = '.';
        private readonly IList<SubscriberAttribute> _subscriberAttributeList = new List<SubscriberAttribute>();
        private string? _bootstrapServers = string.Empty;
        private string? _groupId = string.Empty;

        public IServiceCollection Services { get; }

        /// <summary>
        /// Start config for Kafka Initialization
        /// </summary>
        /// <param name="services">IServiceCollection - Base Interface for .NET Middleware Injection </param>
        public KafkaConfiguration(IServiceCollection services)
        {
            Services = services;
            Services.AddSingleton(typeof(IPublisher<>), typeof(Publisher<>));
            Services.AddSingleton(this);
            Services.AddSingleton(sp => new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                ClientId = Dns.GetHostName(),
                GroupId = _groupId ?? Dns.GetHostName(),
            });

            Services.AddSingleton(sp => new ProducerConfig
            {
                BootstrapServers = _bootstrapServers,
                ClientId = Dns.GetHostName(),
                EnableIdempotence = true,
                CompressionType = CompressionType.Zstd,
            });
        }

        /// <summary>
        /// Manual Params for Injection onto Kafka lib
        /// </summary>
        /// <param name="bootstrapServers">Initial list of brokers as a CSV list of broker host or host:port</param>
        /// <param name="groupId">Nome do Grupo de Identificação de cliente para consumo de tópicos</param>
        public KafkaConfiguration WithParameters(string bootstrapServers, string groupId)
        {
            _bootstrapServers = bootstrapServers;
            _groupId = groupId;
            return this;
        }

        /// <summary>
        /// Indicates WithScope() lifecycle insted of Transient for kafka topics
        /// </summary>
        public KafkaConfiguration WithScope()
        {
            ShouldCreateScope = true;
            return this;
        }
        /// <summary>
        /// Use Env Variables 
        /// KAFKA_BOOTSTRAP_SERVERS: Host
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public KafkaConfiguration WithEnviroments(IConfiguration config)
            => WithParameters(
                config.GetValue<string>("KAFKA_BOOTSTRAP_SERVERS") ?? throw new ArgumentNullException("KAFKA_BOOTSTRAP_SERVERS"),
                config.GetValue<string>("KAFKA_GROUP_ID")
                );

        /// <summary>
        /// Configures Kafka Subscribers
        /// </summary>
        /// <typeparam name="THandler">subscription handler</typeparam>
        /// <typeparam name="TRequest">Data Transfer Request Object</typeparam>
        /// <param name="handler">base handler for processing</param>
        /// <returns>Configuração</returns>
        public KafkaConfiguration AddSubscriber<THandler, TRequest>(Expression<Func<THandler, Func<TRequest, CancellationToken, Task>>> handler)
        {
            Services.AddTransient(typeof(THandler));
            Services.AddTransient<IHandler<TRequest>>(sp => new Handler<THandler, TRequest>(sp, handler));
            Services.AddHostedService<Subscriber<TRequest>>();
            return this;
        }

        /// <summary>
        /// Defines Composition for Queue Names
        /// </summary>
        /// <param name="prefix">Prefix</param>
        /// <param name="sufix">Sufix ( expl: v1)</param>
        /// <param name="deadLetter">Sufix for dlq </param>
        /// <returns></returns>
        public KafkaConfiguration SetQueuenameComposition(string prefix, string sufix = "v1", string deadLetter = "dead-letter")
        {
            PrefixName = prefix;
            SufixName = sufix;
            DeadLetterName = deadLetter;
            return this;
        }

        /// <summary>
        /// Add Automatically subscribers with annotation tag [Subscriber]
        /// Subscriber Method needs to be a task like so => Task SomeMethod(Model model, CancellationToken ctx)
        /// </summary>
        /// <param name="assembly">Read assembly</param>
        /// <returns>Configuration concrete implementation</returns>
        public KafkaConfiguration AddSubscribers(Assembly assembly)
        {
            var mappings = assembly.GetTypes()
                .Where(type => type.IsPublic && type.IsClass)
                .Select(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.GetCustomAttributes<Attribute>().Any()))
                .SelectMany(method => method.Select(m => new { method = m, attr = m.GetCustomAttributes<SubscriberAttribute>() }));

            foreach (var mapping in mappings)
            {
                var reflectedClass = mapping.method.ReflectedType;
                var request = mapping.method.GetParameters().FirstOrDefault()?.ParameterType ?? throw new NotSupportedException($"{reflectedClass.Name}.{mapping.method.Name} não é compativel com Subscriber");
                var response = mapping.method.ReturnType;

                if (request == null || request == typeof(CancellationToken))
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    throw new ArgumentException($"{reflectedClass.Name}.{mapping.method.Name} : Corpo da requisição da mensagem é obrigatório.");
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                if (mapping.method.GetParameters().LastOrDefault()?.ParameterType != typeof(CancellationToken))
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    throw new ArgumentException($"{reflectedClass.Name}.{mapping.method.Name} : CancellationToken é um argumento obrigatório.");
#pragma warning restore CS8602 // Dereference of a possibly null reference.

#pragma warning disable CS8604 // Possible null reference argument.
                Services.AddTransient(reflectedClass);
#pragma warning restore CS8604 // Possible null reference argument.
                foreach (var attribute in mapping.attr)
                {
                    attribute.Request = request;
                    Type subscriber, handler, handlerImpl;
                    _subscriberAttributeList.Add(attribute);
                    if (response == typeof(Task))
                    {
                        handler = typeof(IHandler<>).MakeGenericType(request);
                        handlerImpl = typeof(Handler<,>).MakeGenericType(reflectedClass, request);
                        subscriber = typeof(Subscriber<>).MakeGenericType(request);
                    }
                    else
                    {
                        throw new NotSupportedException($"{reflectedClass.Name}.{mapping.method.Name} não é compativel com QueueAttribute");
                    }

                    Services.AddTransient(handler, sp => Activator.CreateInstance(handlerImpl, new object[] { sp, mapping.method }));

                    typeof(ServiceCollectionHostedServiceExtensions)
                        .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), new[] { typeof(IServiceCollection) })
                        .MakeGenericMethod(subscriber)
                        .Invoke(null, new object[] { Services });
                }
                continue;

            }

            return this;
        }

        /// <summary>
        /// Get asyncs mapping for subscriber
        /// </summary>
        /// <typeparam name="TRequest">Data Transfer Request Object</typeparam>
        /// <returns>Subscriber Configs </returns>
        internal SubscriberAttribute GetMaping<TRequest>()
        {
            var subscriber = _subscriberAttributeList.SingleOrDefault(t => t.Request == typeof(TRequest));
            if (subscriber == default)
            {
                _subscriberAttributeList.Add(subscriber = new SubscriberAttribute { Request = typeof(TRequest) });
            }

            subscriber.Topic ??= GetTopicName<TRequest>();
            return subscriber;
        }

        internal string GetTopicName<TRequest>()
        {
            var name = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(PrefixName))
            {
                name.Append(PrefixName);
                name.Append(_topicNameSeparator);
            }
            name.Append(FriendlyNameExtension.GetTypeFriendlyName(typeof(TRequest)));

            if (!string.IsNullOrWhiteSpace(SufixName))
            {
                name.Append(_topicNameSeparator);
                name.Append(SufixName);
            }
            return name.ToString().ToLower();
        }


    }
}