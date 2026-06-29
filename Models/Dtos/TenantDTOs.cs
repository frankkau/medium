using Microsoft.AspNetCore.Mvc;

namespace Authentication.Models.Dtos;

// namespace Authentication.Application.DTOs;

public record CreateTenantRequest(
    string Id, 
    string Name, 
    string Subdomain, 
    string? Motto,
    string? MissionStatement,
    string? VisionStatement,
    string? LogoUrl,
    string? FaviconUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string ContactEmail,
    string? ContactPhone,
    string? PhysicalAddress,    
    // string? Role,    
    bool IsActive
    // DateTime CreateAt
    );
// public record UpdateTenantRequest(string Name, bool IsActive);
public record TenantResponse(
    string Id, 
    string Name, 
    string Subdomain, 
    string? Motto,
    string? MissionStatement,
    string? VisionStatement,
    string? LogoUrl,
    string? FaviconUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? ContactEmail,
    string? ContactPhone,
    string? PhysicalAddress,   
    bool IsActive,
    DateTime CreatedAt 
);


public record TenantCreateResponse(
     string Id, 
    string Name, 
    string Subdomain,
     bool IsActive

);
