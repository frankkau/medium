namespace Authentication.Models.Dtos;

public class UpdateTenantDto
{
    // --- Dynamic Branding & Content ---
    public string? Name { get; set; }
    public string? Motto { get; set; }
    public string? MissionStatement { get; set; }
    public string? VisionStatement { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }

    // --- Dynamic UI Customization (Theme Matching) ---
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }

    // --- Official Infrastructure & Contact Metadata ---
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? PhysicalAddress { get; set; }

    // --- Academic & System State Controls ---
    public string? CurrentAcademicYear { get; set; }
    public string? CurrentTermOrSemester { get; set; }
    public bool? IsActive { get; set; }
}
