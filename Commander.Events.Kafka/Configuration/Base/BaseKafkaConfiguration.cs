
namespace Commander.Events.Kafka.Configuration
{
    public class BaseKafkaConfiguration
    {
        protected string PrefixName { get; set; } = "topic";
        protected string SufixName { get; set; } = "v1";
        protected string DeadLetterName { get; set; } = "dead-letter";
        protected bool ShouldCreateScope { get; set; }

        public bool IsValidScoped() => ShouldCreateScope;
        public string GetSufixName() => SufixName;
        public string GetDeadLetterName() => DeadLetterName;
        public string GetPrefixName() => PrefixName;
        
    }
}
