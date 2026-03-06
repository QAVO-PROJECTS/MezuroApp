using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.WebApi.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = sp.GetRequiredService<UserManager<User>>();

        // 1) Create roles
        string[] roles = { "SuperAdmin", "Admin" };

        foreach (var role in roles)
        {
            if (await roleManager.FindByNameAsync(role) == null)
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
        }

        // 2) SuperAdmin user
        var email = "superadmin1@mezuro.az";
        var superAdmin = await userManager.FindByEmailAsync(email);

        if (superAdmin == null)
        {
            superAdmin = new Admin   // IMPORTANT: here Admin object is created!
            {
                Id = Guid.NewGuid(),
                Email = email,
                UserName = email,
                FirstName = "Super",
                LastName = "Admin",
                EmailConfirmed = true,
                IsSuperAdmin = true,
                PhoneNumber = "+994505555555",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(superAdmin, "Shayis16_");
            if (!result.Succeeded)
                throw new Exception(string.Join("; ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");

            // bütün icazələri ver
            foreach (var perm in Permissions.All())
                await userManager.AddClaimAsync(superAdmin,
                    new Claim(Permissions.ClaimType, perm));
        }
    }
}