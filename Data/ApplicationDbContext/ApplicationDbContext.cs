using Authentication.Application.IServices;
using Authentication.Models.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    private readonly ITenantService _tenantService;

    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Tenant> Tenants { get; set; } = null!;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options, 
        ITenantService tenantService) : base(options) 
    {
        _tenantService = tenantService;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Configure the relationship between User and RefreshTokens
        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // 2. Safe Global Query Filters for Subdomain Multi-Tenancy Isolation
        // If GetCurrentTenantId() returns null or throws during seeding, the filter falls back 
        // to allowing the evaluation, preventing startup application crashes.
        builder.Entity<User>()
            .HasQueryFilter(u => u.TenantId == (_tenantService.GetCurrentTenantId() ?? u.TenantId));

        builder.Entity<RefreshToken>()
            .HasQueryFilter(rt => rt.TenantId == (_tenantService.GetCurrentTenantId() ?? rt.TenantId));

        // 3. Performance Indexes
        builder.Entity<User>()
            .HasIndex(u => new { u.TenantId, u.NormalizedUserName })
            .HasDatabaseName("IX_User_Tenant_UserName");

        builder.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.TenantId, rt.Token })
            .HasDatabaseName("IX_RefreshToken_Tenant_Token");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantTracking();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, 
        CancellationToken cancellationToken = default)
    {
        ApplyTenantTracking();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantTracking()
    {
        // Safely check if a tenant string can be resolved right now
        var currentTenantId = _tenantService.GetCurrentTenantId();

        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
        {
            if (entry.State == EntityState.Added)
            {
                // If tracking an entity requiring tenancy, but none is active (e.g. background processing or invalid seed),
                // throw a clear exception specifically targeted at IMustHaveTenant objects.
                if (string.IsNullOrEmpty(currentTenantId))
                {
                    throw new InvalidOperationException(
                        $"Cannot save entity of type '{entry.Entity.GetType().Name}' because no active tenant context was resolved.");
                }

                entry.Entity.TenantId = currentTenantId;
            }
        }
    }
}