using System.Text;
// using Authentication.;
using Authentication.Application;
using Authentication.Application.IServices;
using Authentication.Application.Service;
using Authentication.Data;
// using Authentication.Data.ApplicationDbContext;
using Authentication.Models.Entity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITenantAdminService, TenantAdminService>();

builder.Services.AddControllers();
// Program.cs - Make sure this section is exactly like this
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.Zero
    };

    // ← THIS IS VERY IMPORTANT for Cookie-based JWT
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Cookies["accessToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://192.168.43.15:3000")
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseMiddleware<TenantResolverMiddleware>();
// === Middleware Pipeline ===
app.UseHttpsRedirection();

app.UseCors("AllowNextJs");

app.UseAuthentication();   // ← Must come before UseAuthorization()
app.UseAuthorization();

app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<User>>();
    
    // 1. Fetch the TenantService from the current scope
    var tenantService = services.GetRequiredService<ITenantService>();

    context.Database.EnsureCreated();

    // 2. Seed Tenants (Tenants themselves don't implement IMustHaveTenant, so this passes safely)
    if (!context.Tenants.Any())
    {
        context.Tenants.AddRange(
            new Tenant { Id = "alpha-id", Name = "Alpha School", Subdomain = "alpha", IsActive = true },
            new Tenant { Id = "beta-id", Name = "Beta College", Subdomain = "beta", IsActive = true }
        );
        context.SaveChanges();
    }

    // 3. Seed Alpha Users
    if (!context.Users.IgnoreQueryFilters().Any(u => u.UserName == "teacher@alpha.com"))
    {
        // CRITICAL: Explicitly set the tenant state in the service so DbContext can resolve it
        tenantService.SetTenant("alpha"); 

        var alphaUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "teacher@alpha.com",
            Email = "teacher@alpha.com",
            FullName = "Alpha Instructor",
            TenantId = "alpha" 
        };
        
        var result = await userManager.CreateAsync(alphaUser, "Password123!");
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Failed to seed Alpha user: {errors}");
        }
    }

    // 4. Seed Beta Users
    if (!context.Users.IgnoreQueryFilters().Any(u => u.UserName == "admin@beta.com"))
    {
        // CRITICAL: Change the tenant state to beta before running the user manager creation
        tenantService.SetTenant("beta"); 

        var betaUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "admin@beta.com",
            Email = "admin@beta.com",
            FullName = "Beta Administrator",
            TenantId = "beta"
        };
        
        var result = await userManager.CreateAsync(betaUser, "Password123!");
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Failed to seed Beta user: {errors}");
        }
    }
}
app.Run();