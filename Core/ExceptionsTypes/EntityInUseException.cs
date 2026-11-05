namespace Core.ExceptionsTypes
{
    public class EntityInUseException : Exception
    {
        public EntityInUseException(string message, Exception innerException) : base(message, innerException) { }
    }
}
