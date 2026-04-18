namespace MovieCatalogApiTests.DTOs;

public class LoginResponseDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;
}