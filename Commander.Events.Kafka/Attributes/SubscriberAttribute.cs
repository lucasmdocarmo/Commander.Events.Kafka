namespace Commander.Events.Kafka.Attributes
{
    using System;

    /// <summary>
    /// Queue Mapping
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class SubscriberAttribute : Attribute
    {
        public SubscriberAttribute(string? queue = null)
        {
            Topic = queue;
        }

        public string? Topic { get; set; }
        internal Type? Request { get; set; }
    }
}
