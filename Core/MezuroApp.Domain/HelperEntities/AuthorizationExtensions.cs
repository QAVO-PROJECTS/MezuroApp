using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MezuroApp.Domain.HelperEntities;
using Microsoft.Extensions.DependencyInjection;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var perm in Permissions.All())
            {
                options.AddPolicy(perm, policy =>
                    policy.RequireClaim(Permissions.ClaimType, perm));
            }
        });

        // SuperAdmin hər şeyi keçir
        services.AddSingleton<IAuthorizationHandler, SuperAdminAuthorizationHandler>();

        return services;
    }
}

public class SuperAdminAuthorizationHandler : AuthorizationHandler<IAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        IAuthorizationRequirement requirement)
    {
        // Role və ya claim ilə yoxlaya bilərik
        if (context.User.IsInRole("SuperAdmin") ||
            context.User.HasClaim("role", "SuperAdmin") ||
            context.User.HasClaim(Permissions.ClaimType, "*")) // istəsən “*” dəstəklə
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}