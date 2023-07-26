namespace Commander.Events.Kafka.Extensions.Shared
{
    using System.Text;

    public static class FriendlyNameExtension
    {
        private static StringBuilder _nameBuilder { get; set; }
        private static string TypeName { get; set; }


        /// <summary>
        /// Creates a friendly name for the topics and queues
        /// </summary>
        /// <param name="type">queue types</param>
        /// <returns>string with contexat name</returns>
        public static StringBuilder GetTypeFriendlyName(Type type)
        {
            _nameBuilder = new StringBuilder();
            TypeName = string.Empty;

            if (type.IsGenericType)
            {
                TypeName = type.Name.Substring(0, type.Name.IndexOf('`'));
            }
            else
            {
                TypeName = type.Name;
            }

            if (type.IsInterface && type.Name.StartsWith("i", StringComparison.InvariantCultureIgnoreCase))
            {
                _nameBuilder.Append(TypeName[1..]);
            }
            else
            {
                _nameBuilder.Append(TypeName);
            }

            if (type.IsGenericType)
            {
                _nameBuilder.Append("-");
                _nameBuilder.Append(GetTypeFriendlyName(type.GetGenericArguments().First()));
            }

            return _nameBuilder;
        }
    }
}
