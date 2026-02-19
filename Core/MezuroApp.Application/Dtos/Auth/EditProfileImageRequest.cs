using Microsoft.AspNetCore.Http;

namespace MezuroApp.Application.Dtos.Auth;

public class EditProfileImageRequest
{
    public IFormFile Image { get; set; }
}
