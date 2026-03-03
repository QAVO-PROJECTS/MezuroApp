namespace MezuroApp.Application.Dtos.Admins;

public class AdminUpdateRequestDto
{
    public string Id { get; set; } = null!; // target admin id (string guid)

    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }

    // superadmin et / geri al
    public bool? MakeSuperAdmin { get; set; }

    // permissions management
    public bool? ReplaceAllPermissions { get; set; } // true olsa: hamısını silib yenisini qoyur
    public List<string>? Permissions { get; set; }   // ReplaceAllPermissions=true üçün
    public List<string>? AddPermissions { get; set; }
    public List<string>? RemovePermissions { get; set; }
}
