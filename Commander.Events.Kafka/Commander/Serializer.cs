namespace Commander.Events.Kafka.Services
{
    using Confluent.Kafka;
    using System;
    using System.Text.Json;

    public class Serializer<TRequest> : ISerializer<TRequest>, IDeserializer<TRequest>
    {
        private readonly static JsonSerializerOptions options = new JsonSerializerOptions
        {
#pragma warning disable SYSLIB0020 // Type or member is obsolete
            IgnoreNullValues = true,
#pragma warning restore SYSLIB0020 // Type or member is obsolete
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        public TRequest Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            return JsonSerializer.Deserialize<TRequest>(data, options);  
        }

        public byte[] Serialize(TRequest data, SerializationContext context)
        {
            return JsonSerializer.SerializeToUtf8Bytes(data, typeof(TRequest), options);
        }
    }
}
