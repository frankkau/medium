using System;

namespace Authentication.Models.Entity;

public class Tenant
{
    // --- Core Identity & Routing ---
    public string Id { get; set; } = string.Empty; // Guid or short code (e.g., "alpha")
    public string Name { get; set; } = string.Empty; // e.g., "Alpha International Academy"
    public string Subdomain { get; set; } = string.Empty; // e.g., "alpha"
    public bool IsActive { get; set; } = true;

    // --- Dynamic Branding & Content ---
    public string? Motto { get; set; } // e.g., "Knowledge is Power"
    public string? MissionStatement { get; set; } // Detailed mission vision statement
    public string? VisionStatement { get; set; } // Long-term corporate vision
    public string? LogoUrl { get; set; } // Cloudinary storage URL for the school logo
    public string? FaviconUrl { get; set; } // Browser icon URL customization

    // --- Dynamic UI Customization (Theme Matching) ---
    public string? PrimaryColor { get; set; } // Default Tailwind Indigo-600 Hex
    public string? SecondaryColor { get; set; } // Default dark accent Hex

    // --- Official Infrastructure & Contact Metadata ---
    public string? RegistrationNumber { get; set; } // National Education Board License ID
    public string? ContactEmail { get; set; } // official-admin@school.edu
    public string? ContactPhone { get; set; }
    public string? PhysicalAddress { get; set; }

  
    // --- Audit Logs ---
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}