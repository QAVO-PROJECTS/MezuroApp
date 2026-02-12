namespace MezuroApp.Domain.HelperEntities;


public class ApiResponse<T>
{
 
    public int StatusCode { get; set; }
    public LocalizedMessage Message { get; set; }
    public T? Data { get; set; }

    public ApiResponse(int statusCode, T? data, LocalizedMessage message)
    {
        StatusCode = statusCode;
        Data = data;
        Message = message;
    }

}
