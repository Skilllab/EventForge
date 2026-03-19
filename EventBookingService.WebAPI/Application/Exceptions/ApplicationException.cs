namespace EventBookingService.WebAPI.Application.Exceptions
{
    /// <summary>
    /// Базовый класс для кастом исключений
    /// </summary>
    public abstract class ApplicationException : Exception
    {
        protected ApplicationException(string message) : base(message) { }

        protected ApplicationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
