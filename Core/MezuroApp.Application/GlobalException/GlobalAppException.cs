namespace MezuroApp.Application.GlobalException;

public class GlobalAppException : Exception
{
    public GlobalAppException() : base() { }
    public GlobalAppException(string message) : base(message) { }
    public GlobalAppException(string message, Exception innerException) : base(message, innerException) { }
}