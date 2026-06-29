using Authentication.Application.IServices;

namespace Authentication.Models.Entity;


public class  RegistrarProfile : IMustHaveTenant
{
    public string Id { get; set; } = null!;
    public string EmployeeNumber { get; set; } = null!;
    public string Department { get; set; } = null!;
    public DateTime HireDate { get; set; }
    public string TenantId { get; set; } = null!;

    // 1:1 Relationship with Identity User
    public string UserId { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
