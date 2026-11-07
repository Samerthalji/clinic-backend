using System.Net;
using System.Net.Http.Json;
using MedicalClinic.Application.Dtos.AuthrResponseDto;
using Xunit;
using MedicalClinic.Application.Dtos.DoctorResponseDto;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace MedicalClinic.API.Tests.Integration;

// IClassFixture: xUnit creates ONE factory (one running app) and shares it with the tests of this class.
public class AuthIntegrationTests : IClassFixture<Customwebapplicationfactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(Customwebapplicationfactory factory)
    {
        _client = factory.CreateClient();   // the "browser" that talks to the real app
    }
    private async Task<string> GetAdminTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/Login", new TokenRequestModelDto
        {
            Email = Customwebapplicationfactory.AdminEmail,
            Password = Customwebapplicationfactory.AdminPassword
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthModelDto>();
        _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", auth!.Token);
        return auth!.Token;
    }

    [Fact]
    public async Task GetDoctors_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/Doctors");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // This one proves the setup works: settings, in-memory database, tables, seeded roles and seeded admin.
    [Fact]
    public async Task Login_AsSeededAdmin_ReturnsTokenWithAdminRole()
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/Login", new TokenRequestModelDto
        {
            Email = Customwebapplicationfactory.AdminEmail,
            Password = Customwebapplicationfactory.AdminPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthModelDto>();
        Assert.NotNull(auth);
        Assert.True(auth!.IsAuthenticated);
        Assert.False(string.IsNullOrEmpty(auth.Token));
        Assert.Contains("Admin", auth.Roles);
    }
    [Fact]
    public async Task GetDoctors_WithAdminToken_Returns200()
    {
        var token = await GetAdminTokenAsync();
        var response = await _client.GetAsync("/api/Doctors");
        var doctors = await response.Content.ReadFromJsonAsync<List<DoctorDto>>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(doctors);

    }
    [Fact]
    public async Task ToggleDoctor_WhenDoctorMissing_Returns404ProblemDetails()
    {
        var token = await GetAdminTokenAsync();
        var response = await _client.PatchAsync("/api/Doctors/999/toggle-active", null);    
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(404, problemDetails.Status);
    }

}