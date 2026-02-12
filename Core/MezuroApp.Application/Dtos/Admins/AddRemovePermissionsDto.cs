namespace MezuroApp.Application.Dtos.Admins;

public class AddRemovePermissionsDto
{
    public string Id { get; set; }
    public IEnumerable<string> Add { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Remove { get; set; } = Array.Empty<string>();
}
