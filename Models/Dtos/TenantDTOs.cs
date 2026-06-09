namespace Authentication.Models.Dtos;

// namespace Authentication.Application.DTOs;

public record CreateTenantRequest(string Id, string Name, string Subdomain);
public record UpdateTenantRequest(string Name, bool IsActive);
public record TenantResponse(string Id, string Name, string Subdomain, bool IsActive);
