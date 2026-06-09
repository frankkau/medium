namespace Authentication.Models.Entity;

public class Tenant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty; // e.g., "school1" for "school1.yourdomain.com"
    public bool IsActive { get; set; } = true;
}
