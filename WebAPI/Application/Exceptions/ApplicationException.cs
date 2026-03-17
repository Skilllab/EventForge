namespace WebAPI.Application.Exceptions
{
    //Базовый класс для кастом исключений
    public abstract class ApplicationException : Exception
    {
        protected ApplicationException(string message) : base(message) { }

        protected ApplicationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
