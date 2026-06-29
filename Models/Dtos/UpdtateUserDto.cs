namespace Authentication.Models.Dtos;

public class UpdtateUserDto
{
    public string? Email {get; set;}
    public string? FullName {get; set;}
    public List<string> Roles {get; set;} = new();
}
