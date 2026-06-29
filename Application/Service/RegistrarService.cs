using Authentication.Application.IServices;
using Authentication.Models.Dtos;
using Authentication.Models.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Application.Service;

public class RegistrarService : IRegistrarService
{
     private readonly IRegistrarRepository _repository;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITenantService _tenantService;


    public RegistrarService(
        IRegistrarRepository repository, 
        UserManager<User> userManager,
        RoleManager<ApplicationRole> roleManager,
        ITenantService tenantService)
    {
        _repository = repository;
        _userManager = userManager;
        _roleManager = roleManager;
        _tenantService = tenantService;
    }

   public async Task DeleteRegistrarAsync(string id)
{
    var registrar = await _repository.GetByIdAsync(id)
        ?? throw new KeyNotFoundException($"Registrar '{id}' was not found.");

    // Cascade will handle the profile deletion
    var result = await _userManager.DeleteAsync(registrar.User);
    if (!result.Succeeded)
        throw new InvalidOperationException("Failed to delete registrar account.");
}

    public async Task<IEnumerable<RegistrarResponseDto>> GetAllRegistrarsAsync()
    {
        var registrars = await _repository.GetAllAsync();
        return registrars.Select(t => new RegistrarResponseDto(
            t.Id, t.UserId, t.User.Email!, t.User.FullName, t.EmployeeNumber, t.Department, t.TenantId, t.HireDate
        ));

    }

    public async Task<RegistrarResponseDto> GetRegistrarByIdAsync(string id)
    {
        var t = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Registrar record with ID '{id}' was not found.");
            
        return new RegistrarResponseDto( t.Id, t.UserId, t.User.Email!, t.User.FullName, t.EmployeeNumber, t.Department, t.TenantId, t.HireDate);
    }

    public async Task<RegistrarResponseDto> RegisterRegistrarAsync(RegistrarUpsertDto dto)
    {
         var currentTenantId = _tenantService.GetCurrentTenantId();
        if (string.IsNullOrEmpty(currentTenantId))
        {
            throw new InvalidOperationException("Active tenant context could not be resolved.");
        }
        // Cross-tenant email collision check
        // ✅ Only block duplicates within the same tenant
        var userExists = await _userManager.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == dto.Email && u.TenantId == currentTenantId);
        if (userExists)
            throw new InvalidOperationException("An account with this email already exists in this tenant.");
                const string targetRole = "Registrar";
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

         var profile = new RegistrarProfile
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

        return new RegistrarResponseDto(
            profile.Id, profile.UserId, identityUser.Email, identityUser.FullName, profile.EmployeeNumber, profile.Department, profile.TenantId, profile.HireDate);
    }

    public async Task UpdateRegistrarAsync(string id, RegistrarUpsertDto dto)
    {
       var registrar = await _repository.GetByIdAsync(id) 
            ?? throw new KeyNotFoundException($"Teacher with target identifier '{id}' was not found.");

        registrar.User.FullName = dto.FullName;
        registrar.Department = dto.Department;
        registrar.EmployeeNumber = dto.EmployeeNumber;
        registrar.HireDate = dto.HireDate;

        _repository.Update(registrar);
        await _userManager.UpdateAsync(registrar.User);
        await _repository.SaveChangesAsync();
    }

}
