namespace Commander.Events.Kafka.ObjectValues
{ 
    using System;
    using System.Reflection;

    /// <summary>
    /// Exeption Serilization
    /// </summary>
    [Serializable]
    public class ErrorModel 
    {
        public string Message { get; set; }
        public string StackTrace { get; set; }  
        public string TypeId { get; set; }
        public ErrorModel() 
        {
            Message = StackTrace = TypeId = string.Empty;
        }

        public ErrorModel(Exception ex) 
        {
            Message = ex.Message;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            StackTrace = ex.StackTrace.ToString();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            TypeId = ex.GetType().AssemblyQualifiedName.ToString();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        public Exception GetExceptionTrace()
        {
            var exception = (Exception) Activator.CreateInstance(Type.GetType(TypeId));
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            typeof(Exception)
                .GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic )
                .SetValue(exception, Message);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            typeof(Exception)
                .GetField("_stackTraceString", BindingFlags.Instance | BindingFlags.NonPublic )
                .SetValue(exception, StackTrace);
#pragma warning disable CS8603 // Possible null reference return.
            return exception;
#pragma warning restore CS8603 // Possible null reference return.
        }
    }
}