using Authentication.Application.IServices;
using Authentication.Models.Dtos;
using Authentication.Models.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Application.Service;

public class TeacherService : ITeacherService
{
    private readonly ITeacherRepository _repository;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITenantService _tenantService;

    public TeacherService(
        ITeacherRepository repository, 
        UserManager<User> userManager,
        RoleManager<ApplicationRole> roleManager,
        ITenantService tenantService)
    {
        _repository = repository;
        _userManager = userManager;
        _roleManager = roleManager;
        _tenantService = tenantService;
    }

    public async Task<IEnumerable<TeacherResponseDto>> GetAllTeachersAsync()
    {
        var teachers = await _repository.GetAllAsync();
        return teachers.Select(t => new TeacherResponseDto(
            t.Id, t.UserId, t.User.Email!, t.User.FullName, t.EmployeeNumber, t.Department, t.HireDate));
    }

    public async Task<TeacherResponseDto> GetTeacherByIdAsync(string id)
    {
        var t = await _repository.GetByIdAsync(id) 
            ?? throw new KeyNotFoundException($"Teacher record with ID '{id}' was not found.");
            
        return new TeacherResponseDto(
            t.Id, t.UserId, t.User.Email!, t.User.FullName, t.EmployeeNumber, t.Department, t.HireDate);
    }

    public async Task<TeacherResponseDto> RegisterTeacherAsync(TeacherUpsertDto dto)
    {
        var currentTenantId = _tenantService.GetCurrentTenantId();
        if (string.IsNullOrEmpty(currentTenantId))
        {
            throw new InvalidOperationException("Active tenant context could not be resolved.");
        }

        // Cross-tenant email collision check
        var userExists = await _userManager.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == dto.Email);
        if (userExists)
        {
            throw new InvalidOperationException("An account with this email address already exists globally.");
        }

        // Ensure "Teacher" role exists for this tenant
        const string targetRole = "Teacher";
        if (!await _roleManager.RoleExistsAsync(targetRole))
        {
            var newRole = new ApplicationRole 
            { 
                Id = Guid.NewGuid().ToString(),
                Name = targetRole,
                NormalizedName = targetRole.ToUpper(),
                TenantId = currentTenantId
            };
            await _roleManager.CreateAsync(newRole);
        }

        // Build base identity entity explicitly passing current TenantId
        var identityUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            TenantId = currentTenantId
        };

        // Use Employee Number explicitly as the password configuration requirement
        var identityResult = await _userManager.CreateAsync(identityUser, dto.EmployeeNumber);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Identity account generation failed: {errors}");
        }

        await _userManager.AddToRoleAsync(identityUser, targetRole);

        var profile = new TeacherProfile
        {
            Id = Guid.NewGuid().ToString(),
            UserId = identityUser.Id,
            EmployeeNumber = dto.EmployeeNumber,
            Department = dto.Department,
            HireDate = dto.HireDate,
            TenantId = currentTenantId
        };

        await _repository.AddAsync(profile);
        await _repository.SaveChangesAsync();

        return new TeacherResponseDto(
            profile.Id, profile.UserId, identityUser.Email, identityUser.FullName, profile.EmployeeNumber, profile.Department, profile.HireDate);
    }

    public async Task UpdateTeacherAsync(string id, TeacherUpsertDto dto)
    {
        var teacher = await _repository.GetByIdAsync(id) 
            ?? throw new KeyNotFoundException($"Teacher with target identifier '{id}' was not found.");

        teacher.User.FullName = dto.FullName;
        teacher.Department = dto.Department;
        teacher.EmployeeNumber = dto.EmployeeNumber;
        teacher.HireDate = dto.HireDate;

        _repository.Update(teacher);
        await _userManager.UpdateAsync(teacher.User);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteTeacherAsync(string id)
    {
        var teacher = await _repository.GetByIdAsync(id) 
            ?? throw new KeyNotFoundException($"Teacher target identification system record '{id}' was not found.");

        _repository.Delete(teacher);
        await _userManager.DeleteAsync(teacher.User);
        await _repository.SaveChangesAsync();
    }
}
