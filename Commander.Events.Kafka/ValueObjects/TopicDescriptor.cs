namespace Commander.Events.Kafka.ObjectValues
{
    using Commander.Events.Kafka.Attributes;
    using global::Commander.Events.Kafka.Configuration;
    using global::Commander.Events.Kafka.Extensions.Shared;
    using System;
    using System.Linq;
    using System.Text;

    public struct TopicDescriptor
    {
        private const char _queueNameSeparator = '.';

        public TopicDescriptor(KafkaConfiguration config,SubscriberAttribute? attr, 
            string? topicName, Type requestType)
        {
            RequestType = requestType;
            TopicName = topicName ?? (attr?.Topic ?? BuildName(requestType, config.GetPrefixName(),
                config.GetSufixName()));
            DeadLetterTopicName = $"{TopicName}{_queueNameSeparator}{config.GetDeadLetterName()}";
        }


        /// <summary>
        /// Builds the queue name based on parameters
        /// </summary>
        /// <returns>Nome da fila</returns>
        private static string BuildName(Type requestType,  string prefix, string sufix)
        {
            var name = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                name.Append(prefix);
                name.Append(_queueNameSeparator);
            }
            name.Append(FriendlyNameExtension.GetTypeFriendlyName(requestType));
            if (!string.IsNullOrWhiteSpace(sufix))
            {
                name.Append(_queueNameSeparator);
                name.Append(sufix);
            }

            return name.ToString().ToLower();
        }

        public string TopicName { get;  }
        public string DeadLetterTopicName { get;  }
        public Type RequestType { get; }

    }
}