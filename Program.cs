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


builder.Services.AddIdentity<User, ApplicationRole>(options =>{
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
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IRegistrarRepository, RegistrarRepository>();
builder.Services.AddScoped<IRegistrarService, RegistrarService>();
builder.Services.AddScoped<IClassroomService, ClassroomService>();

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
        policy.WithOrigins(            
                "http://localhost:3000",
                "http://192.168.43.15:3000",
                "http://beta.192.168.43.15:3000",
                "http://alpha.192.168.43.15:3000",
                "http://beta.localhost:3000",
                "http://alpha.localhost:3000"
               )
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options.Title = "School Management API";
        options.Theme = ScalarTheme.DeepSpace;   // or .Default .Moon .Purple .Solarized
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.AddPreferredSecuritySchemes("Bearer");    // pre-selects JWT auth in the UI
    });
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // app.MapScalarApiReference();
}

// 1. Move CORS to the absolute top of the pipeline
app.UseCors("AllowNextJs");

// 2. HTTPS redirection comes after CORS handles preflights
app.UseHttpsRedirection();

// 3. Resolve tenants AFTER CORS has verified the request origin
app.UseMiddleware<TenantResolverMiddleware>();

// 4. Finally, process authentication, authorization, and routing
app.UseAuthentication();   
app.UseAuthorization();

app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<User>>();
    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
    var tenantService = services.GetRequiredService<ITenantService>();

    context.Database.Migrate();

    // =========================================================================
    // 1. Seed Tenants (no tenant context needed — Tenants table has no filter)
    // =========================================================================
    if (!context.Tenants.Any())
    {
        context.Tenants.AddRange(
            new Tenant { Id = "alpha", Name = "Alpha School", Subdomain = "alpha", IsActive = true },
            new Tenant { Id = "beta",  Name = "Beta College",  Subdomain = "beta",  IsActive = true }
        );
        await context.SaveChangesAsync();
    }

    // =========================================================================
    // 2. Seed Alpha Tenant — Roles + Admin User
    // =========================================================================
    tenantService.SetTenant("alpha");

    foreach (var role in new[] { "Admin", "Teacher", "Student", "Registrar" })
    {
        if (!await context.Roles.IgnoreQueryFilters()
                .AnyAsync(r => r.NormalizedName == role.ToUpper() && r.TenantId == "alpha"))
        {
            var roleResult = await roleManager.CreateAsync(new ApplicationRole
            {
                Id             = Guid.NewGuid().ToString(),
                Name           = role,
                NormalizedName = role.ToUpper(),
                TenantId       = "alpha"
            });

            if (!roleResult.Succeeded)
                throw new Exception($"[Alpha] Role '{role}' creation failed: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }
    }

    if (!await context.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.UserName == "admin@alpha.com" && u.TenantId == "alpha"))
    {
        var alphaAdmin = new User
        {
            Id       = Guid.NewGuid().ToString(),
            UserName = "admin@alpha.com",
            Email    = "admin@alpha.com",
            FullName = "Alpha Administrator",
            TenantId = "alpha"
        };

        var createResult = await userManager.CreateAsync(alphaAdmin, "Password123!");
        if (!createResult.Succeeded)
            throw new Exception($"[Alpha] User creation failed: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

        var roleResult = await userManager.AddToRoleAsync(alphaAdmin, "Admin");
        if (!roleResult.Succeeded)
            throw new Exception($"[Alpha] Role assignment failed: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
    }

    // =========================================================================
    // 3. Seed Beta Tenant — Roles + Admin User
    // =========================================================================
    tenantService.SetTenant("beta");

    foreach (var role in new[] { "Admin", "Teacher", "Student", "Registrar" })
    {
        if (!await context.Roles.IgnoreQueryFilters()
                .AnyAsync(r => r.NormalizedName == role.ToUpper() && r.TenantId == "beta"))
        {
            var roleResult = await roleManager.CreateAsync(new ApplicationRole
            {
                Id             = Guid.NewGuid().ToString(),
                Name           = role,
                NormalizedName = role.ToUpper(),
                TenantId       = "beta"
            });

            if (!roleResult.Succeeded)
                throw new Exception($"[Beta] Role '{role}' creation failed: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }
    }

    if (!await context.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.UserName == "admin@beta.com" && u.TenantId == "beta"))
    {
        var betaAdmin = new User
        {
            Id       = Guid.NewGuid().ToString(),
            UserName = "admin@beta.com",
            Email    = "admin@beta.com",
            FullName = "Beta Administrator",
            TenantId = "beta"
        };

        var createResult = await userManager.CreateAsync(betaAdmin, "Password123!");
        if (!createResult.Succeeded)
            throw new Exception($"[Beta] User creation failed: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

        var roleResult = await userManager.AddToRoleAsync(betaAdmin, "Admin");
        if (!roleResult.Succeeded)
            throw new Exception($"[Beta] Role assignment failed: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
    }
}

app.Run();