namespace Authentication.Application.IServices;

public interface IMustHaveTenant
{
    public string TenantId { get; set; }
}
