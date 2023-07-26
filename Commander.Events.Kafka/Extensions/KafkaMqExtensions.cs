namespace Microsoft.Extensions.DependencyInjection
{
    using Commander.Events.Kafka.Configuration;

    public static class KafkaMqExtensions
    {

        /// <summary>
        /// Configures and adds Commander Kafka
        /// </summary>
        /// <param name="services">Service Collection</param>
        public static KafkaConfiguration AddKafka(this IServiceCollection services)
        {
            return new KafkaConfiguration(services);
        }
    }
}
