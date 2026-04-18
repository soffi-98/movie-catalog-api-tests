namespace MovieCatalogApiTests.DTOs;

public class ApiResponseDto
{
    public string Msg { get; set; } = string.Empty;

    public MovieDto Movie { get; set; } = new MovieDto();
}