using Authentication.Application.IServices;
using Authentication.Models.Entity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Data;

public class ApplicationDbContext : IdentityDbContext<User, ApplicationRole, string>
{
    private readonly ITenantService _tenantService;

    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<StudentProfile> StudentProfiles { get; set; } = null!;
    public DbSet<TeacherProfile> TeacherProfiles { get; set; } = null!;
    public DbSet<RegistrarProfile> RegistrarProfiles {get; set;} = null!;
    public DbSet<Classroom> Classrooms { get; set; } = null!;
    public DbSet<ClassroomTeacher> ClassroomTeachers { get; set; } = null!;
    public DbSet<Attendance> Attendances { get; set; } = null!;
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options, 
        ITenantService tenantService) : base(options) 
    {
        _tenantService = tenantService;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // 1. Initialize default ASP.NET Core Identity tables first
        base.OnModelCreating(builder);

        // Capture resolved tenant ID context locally to ensure predictable EF translation
        // var currentTenantId = _tenantService.GetCurrentTenantId();

        // 2. Configure Relationships
        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentProfile>()
            .HasOne(s => s.User)
            .WithOne(u => u.StudentProfile)
            .HasForeignKey<StudentProfile>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TeacherProfile>()
            .HasOne(t => t.User)
            .WithOne() // Or configure virtual navigation property on User if needed
            .HasForeignKey<TeacherProfile>(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<RegistrarProfile>()
            .HasOne(r => r.User)
            .WithOne()
            .HasForeignKey<RegistrarProfile>(r =>r.UserId)
            .OnDelete(DeleteBehavior.Cascade);        

        // Relationships
        builder.Entity<ClassroomTeacher>()
            .HasOne(ct => ct.Classroom)
            .WithMany(c => c.ClassroomTeachers)
            .HasForeignKey(ct => ct.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ClassroomTeacher>()
            .HasOne(ct => ct.Teacher)
            .WithMany()
            .HasForeignKey(ct => ct.TeacherId)
            .OnDelete(DeleteBehavior.Restrict); // don't delete teacher if unassigned

        builder.Entity<StudentProfile>()
            .HasOne(s => s.Classroom)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.ClassroomId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Attendance>()
            .HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Attendance>()
            .HasOne(a => a.RecordedBy)
            .WithMany()
            .HasForeignKey(a => a.RecordedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

       

        // 3. Resilient Multi-Tenancy Global Query Filters (Prevents Migration/Seeding Startup Crashes)
        // The null-coalesce fallback (u.TenantId == u.TenantId) only fires   
            builder.Entity<User>()
                .HasQueryFilter(u =>
                    _tenantService.GetCurrentTenantId() == null
                    || u.TenantId == _tenantService.GetCurrentTenantId());

            builder.Entity<ApplicationRole>()
                .HasQueryFilter(r =>
                    _tenantService.GetCurrentTenantId() == null
                    || r.TenantId == _tenantService.GetCurrentTenantId());

            builder.Entity<RefreshToken>()
                .HasQueryFilter(rt =>
                    _tenantService.GetCurrentTenantId() == null
                    || rt.TenantId == _tenantService.GetCurrentTenantId());

            builder.Entity<StudentProfile>()
                .HasQueryFilter(s =>
                    _tenantService.GetCurrentTenantId() == null
                    || s.TenantId == _tenantService.GetCurrentTenantId());

            builder.Entity<TeacherProfile>()
                .HasQueryFilter(t =>
                    _tenantService.GetCurrentTenantId() == null
                    || t.TenantId == _tenantService.GetCurrentTenantId());

            builder.Entity<RegistrarProfile>()
                .HasQueryFilter(r =>
                    _tenantService.GetCurrentTenantId() == null
                    || r.TenantId == _tenantService.GetCurrentTenantId());
            builder.Entity<Classroom>()
                    .HasQueryFilter(c => _tenantService.GetCurrentTenantId() == null
                        || c.TenantId == _tenantService.GetCurrentTenantId());

            builder.Entity<ClassroomTeacher>()
                .HasQueryFilter(ct => _tenantService.GetCurrentTenantId() == null
                    || ct.TenantId == _tenantService.GetCurrentTenantId());

            builder.Entity<Attendance>()
                .HasQueryFilter(a => _tenantService.GetCurrentTenantId() == null
                    || a.TenantId == _tenantService.GetCurrentTenantId());

        // =========================================================================
        // STEP A: Turn off uniqueness on Identity's default single-column indexes
        // =========================================================================
        builder.Entity<User>().HasIndex(u => u.NormalizedUserName).IsUnique(false);
        builder.Entity<User>().HasIndex(u => u.NormalizedEmail).IsUnique(false);
        builder.Entity<ApplicationRole>().HasIndex(r => r.NormalizedName).IsUnique(false);

        // FIX: Removed the duplicate, un-named configuration of StudentProfile index from here.

        // =========================================================================
        // STEP B: Create distinct Multi-Tenant Composite Indexes with Unique Names
        // =========================================================================
        builder.Entity<User>()
            .HasIndex(u => new { u.TenantId, u.NormalizedUserName })
            .IsUnique()
            .HasDatabaseName("IX_User_Tenant_UserName"); 

        builder.Entity<User>()
            .HasIndex(u => new { u.TenantId, u.NormalizedEmail })
            .IsUnique()
            .HasDatabaseName("IX_User_Tenant_Email");

        builder.Entity<ApplicationRole>()
            .HasIndex(r => new { r.TenantId, r.NormalizedName })
            .IsUnique()
            .HasDatabaseName("IX_Role_Tenant_NormalizedName");

        builder.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.TenantId, rt.Token })
            .IsUnique()
            .HasDatabaseName("IX_RefreshToken_Tenant_Token");

        builder.Entity<StudentProfile>()
            .HasIndex(s => new { s.TenantId, s.StudentNumber })
            .IsUnique()
            .HasDatabaseName("IX_StudentProfile_Tenant_StudentNumber");

        builder.Entity<TeacherProfile>()
            .HasIndex(t => new { t.TenantId, t.EmployeeNumber })
            .IsUnique()
            .HasDatabaseName("IX_TeacherProfile_Tenant_EmployeeNumber");
            
        builder.Entity<RegistrarProfile>()
            .HasIndex(r => new{r.TenantId, r.EmployeeNumber})
            .IsUnique()
            .HasDatabaseName("IX_RegistrarProfile_Tenant_EmployeeNumber");
         // Unique indexes
        builder.Entity<Classroom>()
            .HasIndex(c => new { c.TenantId, c.Grade, c.Stream, c.AcademicYear })
            .IsUnique()
            .HasDatabaseName("IX_Classroom_Tenant_Grade_Stream_Year");

        builder.Entity<ClassroomTeacher>()
            .HasIndex(ct => new { ct.TenantId, ct.ClassroomId, ct.TeacherId })
            .IsUnique()
            .HasDatabaseName("IX_ClassroomTeacher_Tenant_Classroom_Teacher");

        builder.Entity<Attendance>()
            .HasIndex(a => new { a.TenantId, a.ClassroomId, a.StudentId, a.Date })
            .IsUnique()
            .HasDatabaseName("IX_Attendance_Tenant_Classroom_Student_Date");
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
        var currentTenantId = _tenantService.GetCurrentTenantId();

        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
        {
            if (entry.State == EntityState.Added)
            {
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