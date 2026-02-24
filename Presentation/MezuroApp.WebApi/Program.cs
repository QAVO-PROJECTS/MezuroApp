using System.Text;
using FluentValidation.AspNetCore;
using MezuroApp.Application.Mappings;
using MezuroApp.Application.Validations.Auth;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using MezuroApp.Persistance;
using MezuroApp.Persistance.Context;
using MezuroApp.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using MezuroApp.WebApi.Seed;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// FluentValidation
builder.Services.AddControllers()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>())
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value.Errors.Count > 0)
                .SelectMany(kvp => kvp.Value.Errors.Select(e => e.ErrorMessage))
                .ToArray();

            var errorResponse = new
            {
                StatusCode = 400,
                Error = errors
            };

            return new BadRequestObjectResult(errorResponse);
        };
    });

// AutoMapper
builder.Services.AddAutoMapper(typeof(AuthProfileMapping).Assembly);

// DbContext
builder.Services.AddDbContext<MezuroAppDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});

// ===============================
//  Identity (User ⇒ Admin TPH)
// ===============================
builder.Services.AddIdentity<User, IdentityRole<Guid>>(opt =>
{
    opt.Password.RequireNonAlphanumeric = false;
    opt.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<MezuroAppDbContext>()
.AddDefaultTokenProviders();

// ===============================
//  Authentication (JWT)
// ===============================
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"])
        ),
        LifetimeValidator = (_, expireDate, token, _) =>
            token != null ? expireDate > DateTime.UtcNow : false
    };
});

// ===============================
//   Authorization Policies
// ===============================
builder.Services.AddPermissionPolicies();   // <-- BURADA

// Token Lifespan
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(24);
});

// Custom Services
builder.Services.AddServices();

// Mail
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

// Swagger
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo() { Title = "Mezuro API", Version = "v1" });

    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[]{}
        }
    });
});

var app = builder.Build();

// 👇 SUPERADMIN SEED
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

// Swagger UI
// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI();
// }

app.UseHttpsRedirection();

app.UseAuthentication();  // VERY IMPORTANT
app.UseAuthorization();

app.UseAuditLogMiddleware();

// CORS
app.UseCors("corsapp");

app.MapControllers();
// Program.cs – app.Run() çağrılmadan əvvəl
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MezuroAppDbContext>();
    // Boş və ya NULL olanları "User" et – bir dəfəlik
    db.Database.ExecuteSqlRaw(@"
        UPDATE ""AspNetUsers"" 
        SET ""UserType"" = 'User' 
        WHERE ""UserType"" IS NULL OR ""UserType"" = '';
    ");
}


app.Run();