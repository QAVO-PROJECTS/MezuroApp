namespace MezuroApp.Domain.HelperEntities;


public class ApiError
{

    public int StatusCode { get; set; }
    public LocalizedMessage Error { get; set; }

    public ApiError(int statusCode, LocalizedMessage error)
    {
        StatusCode = statusCode;
        Error = error;
    }

}
