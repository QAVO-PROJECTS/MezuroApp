namespace MezuroApp.Application.Dtos.Admins;


public class SetPermissionsDto
{
    public string Id { get; set; }
    public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>(); // tamamıyla əvəz edir
}
