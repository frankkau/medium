using Authentication.Application.IServices;
using Authentication.Models.Dtos;
using Authentication.Models.Entity;
using Microsoft.AspNetCore.Identity;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _repository;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITenantService _tenantService;

    public StudentService(IStudentRepository repository, UserManager<User> userManager, RoleManager<ApplicationRole> roleManager, ITenantService tenantService)
    {
        _repository = repository;
        _userManager = userManager;
        _roleManager = roleManager;
        _tenantService = tenantService;
    }

    public async Task<IEnumerable<StudentResponseDto>> GetAllStudentsAsync()
    {
        var students = await _repository.GetAllAsync();
        return students.Select(s => new StudentResponseDto(s.Id, s.UserId, s.User.Email!, s.User.FullName, s.StudentNumber, s.DateOfBirth, s.EnrollmentStatus));
    }

    public async Task<StudentResponseDto> GetStudentByIdAsync(string id)
    {
        var student = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Student resource with ID record '{id}' could not be located.");
        return new StudentResponseDto(student.Id, student.UserId, student.User.Email!, student.User.FullName, student.StudentNumber, student.DateOfBirth, student.EnrollmentStatus);
    }

    public async Task<StudentResponseDto> CreateStudentAsync(StudentUpsertDto dto)
{
    // 1. Check if user already exists under this tenant
    var userExists = await _userManager.FindByEmailAsync(dto.Email);
    if (userExists != null)
    {
        throw new InvalidOperationException("An account using that email address already exists in this tenant ecosystem.");
    }

    // 2. BULLETPROOF FIX: Ensure the "Student" role exists for the current tenant on the fly
    const string targetRole = "Student";

    if (!await _roleManager.RoleExistsAsync(targetRole))
    {
        var newRole = new ApplicationRole 
        { 
            Id = Guid.NewGuid().ToString(),
            Name = targetRole,
            NormalizedName = targetRole.ToUpper()
        };
        var roleResult = await _roleManager.CreateAsync(newRole);
        if (!roleResult.Succeeded)
        {
            var roleErrors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to auto-create required Student role context: {roleErrors}");
        }
    }

    // Resolve the active admin's tenant ID from your service layer
    var currentTenantId = _tenantService.GetCurrentTenantId() 
        ?? throw new InvalidOperationException("No active tenant context found. Cannot register user.");

    // 3. Create the Base Identity User
    var identityUser = new User
    {
        Id = Guid.NewGuid().ToString(),
        UserName = dto.Email,
        Email = dto.Email,
        FullName = dto.FullName,
        TenantId = currentTenantId
    };

    var identityResult = await _userManager.CreateAsync(identityUser, dto.StudentNumber);
    if (!identityResult.Succeeded)
    {
        var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Identity management generation sequence failed: {errors}");
    }

    // 4. Safely attach to the validated role
    var roleAssignResult = await _userManager.AddToRoleAsync(identityUser, targetRole);
    if (!roleAssignResult.Succeeded)
    {
        var assignmentErrors = string.Join("; ", roleAssignResult.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Failed assigning user to Student role context: {assignmentErrors}");
    }

    // 5. Build and Save the 1:1 Student Profile Link
    var profile = new StudentProfile
    {
        Id = Guid.NewGuid().ToString(),
        UserId = identityUser.Id,
        StudentNumber = dto.StudentNumber,
        DateOfBirth = dto.DateOfBirth,
        EnrollmentStatus = dto.EnrollmentStatus
    };

    await _repository.AddAsync(profile);
    await _repository.SaveChangesAsync();

    return new StudentResponseDto(
        profile.Id, 
        profile.UserId, 
        identityUser.Email, 
        identityUser.FullName, 
        profile.StudentNumber, 
        profile.DateOfBirth, 
        profile.EnrollmentStatus
    );
}

    public async Task UpdateStudentAsync(string id, StudentUpsertDto dto)
    {
        var student = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Student resource context with target identity '{id}' could not be located.");
        
        student.User.FullName = dto.FullName;
        student.StudentNumber = dto.StudentNumber;
        student.DateOfBirth = dto.DateOfBirth;
        student.EnrollmentStatus = dto.EnrollmentStatus;

        _repository.Update(student);
        await _userManager.UpdateAsync(student.User);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteStudentAsync(string id)
    {
        var student = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Student system index allocation '{id}' could not be located.");
        
        _repository.Delete(student);
        await _userManager.DeleteAsync(student.User);
        await _repository.SaveChangesAsync();
    }
}

// ==========================================
// 5. REST CONTROLLER WITH EXCEPTION HANDLING
// ==========================================
