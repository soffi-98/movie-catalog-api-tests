using System.Net;
using System.Text.Json;
using MovieCatalogApiTests.DTOs;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Collections.Generic;

namespace MovieCatalogApiTests.Tests;

[TestFixture]
public class MovieCatalogTests
{
    private RestClient? client;

    private const string BaseUrl = "http://144.91.123.158:5000/api";
    private static readonly string UserEmail =
     Environment.GetEnvironmentVariable("MOVIE_CATALOG_EMAIL") ?? string.Empty;
    private static readonly string UserPassword =
        Environment.GetEnvironmentVariable("MOVIE_CATALOG_PASSWORD") ?? string.Empty;
    private static string createdMovieId = string.Empty;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Assert.That(UserEmail, Is.Not.Null.And.Not.Empty, "MOVIE_CATALOG_EMAIL is not set.");
        Assert.That(UserPassword, Is.Not.Null.And.Not.Empty, "MOVIE_CATALOG_PASSWORD is not set.");
        var loginClient = new RestClient(new RestClientOptions(BaseUrl));

        var loginRequest = new RestRequest("User/Authentication", Method.Post);
        loginRequest.AddJsonBody(new LoginDto
        {
            Email = UserEmail,
            Password = UserPassword
        });

        var loginResponseRaw = loginClient.Execute(loginRequest);

        Assert.That(loginResponseRaw.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(loginResponseRaw.Content, Is.Not.Null.And.Not.Empty);

        var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(
            loginResponseRaw.Content!,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.That(loginResponse, Is.Not.Null);
        Assert.That(loginResponse!.AccessToken, Is.Not.Null.And.Not.Empty);

        client = new RestClient(new RestClientOptions(BaseUrl)
        {
            Authenticator = new JwtAuthenticator(loginResponse.AccessToken)
        });
    }
    [Test, Order(1)]
    public void CreateMovie_WithRequiredFields_ShouldCreateSuccessfully()
    {
        Assert.That(client, Is.Not.Null);

        var request = new RestRequest("Movie/Create", Method.Post);
        request.AddJsonBody(new MovieDto
        {
            Title = "Test Movie",
            Description = "Test Description"
        });

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        var responseDto = JsonSerializer.Deserialize<ApiResponseDto>(
            response.Content!,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.That(responseDto, Is.Not.Null);
        Assert.That(responseDto!.Movie, Is.Not.Null);
        Assert.That(responseDto.Movie.Id, Is.Not.Null.And.Not.Empty);
        Assert.That(responseDto.Msg, Is.EqualTo("Movie created successfully!"));

        createdMovieId = responseDto.Movie.Id;
    }
    [Test, Order(2)]
    public void EditMovie_WithValidId_ShouldEditSuccessfully()
    {
        Assert.That(client, Is.Not.Null);
        Assert.That(createdMovieId, Is.Not.Null.And.Not.Empty);

        var request = new RestRequest("Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", createdMovieId);
        request.AddJsonBody(new MovieDto
        {
            Title = "Edited Test Movie",
            Description = "Edited Test Description"
        });

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        var responseDto = JsonSerializer.Deserialize<ApiResponseDto>(
            response.Content!,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.That(responseDto, Is.Not.Null);
        Assert.That(responseDto!.Msg, Is.EqualTo("Movie edited successfully!"));
    }
    [Test, Order(3)]
    public void GetAllMovies_ShouldReturnNonEmptyArray()
    {
        Assert.That(client, Is.Not.Null);

        var request = new RestRequest("Catalog/All", Method.Get);

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        var movies = JsonSerializer.Deserialize<List<MovieDto>>(
            response.Content!,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.That(movies, Is.Not.Null);
        Assert.That(movies!.Count, Is.GreaterThan(0));
    }
    [Test, Order(4)]
    public void DeleteMovie_WithValidId_ShouldDeleteSuccessfully()
    {
        Assert.That(client, Is.Not.Null);
        Assert.That(createdMovieId, Is.Not.Null.And.Not.Empty);

        var request = new RestRequest("Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", createdMovieId);

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        var responseDto = JsonSerializer.Deserialize<ApiResponseDto>(
            response.Content!,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.That(responseDto, Is.Not.Null);
        Assert.That(responseDto!.Msg, Is.EqualTo("Movie deleted successfully!"));
    }
    [Test, Order(5)]
    public void CreateMovie_WithoutRequiredFields_ShouldReturnBadRequest()
    {
        Assert.That(client, Is.Not.Null);

        var request = new RestRequest("Movie/Create", Method.Post);
        request.AddJsonBody(new
        {
            posterUrl = "",
            trailerLink = "",
            isWatched = false
        });

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    [Test, Order(6)]
    public void EditMovie_WithInvalidId_ShouldReturnBadRequest()
    {
        Assert.That(client, Is.Not.Null);

        var request = new RestRequest("Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", "invalid-movie-id");
        request.AddJsonBody(new MovieDto
        {
            Title = "Edited Invalid Movie",
            Description = "Edited Invalid Description"
        });

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        var responseDto = JsonSerializer.Deserialize<ApiResponseDto>(
            response.Content!,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.That(responseDto, Is.Not.Null);
        Assert.That(responseDto!.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
    }
    [Test, Order(7)]
    public void DeleteMovie_WithInvalidId_ShouldReturnBadRequest()
    {
        Assert.That(client, Is.Not.Null);

        var request = new RestRequest("Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", "invalid-movie-id");

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        var responseDto = JsonSerializer.Deserialize<ApiResponseDto>(
            response.Content!,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.That(responseDto, Is.Not.Null);
        Assert.That(responseDto!.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        client?.Dispose();
    }
}