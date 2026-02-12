namespace MezuroApp.Domain.Entities;

public class Admin:User
{
    public bool IsSuperAdmin { get; set; } = false;

}