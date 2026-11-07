using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MedicalClinic.Application.Dtos.AppointmentsResponedDto;
using MedicalClinic.Application.Dtos.AuthrResponseDto;
using MedicalClinic.Application.Dtos.DoctorResponseDto;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MedicalClinic.API.Tests.Integration;

public class AppointmentFlowIntegrationTests : IClassFixture<Customwebapplicationfactory>
{
    private const string PatientPassword = "Patient@12345";
    private const string DoctorPassword = "Doctor@12345";

    private readonly Customwebapplicationfactory _factory;

    public AppointmentFlowIntegrationTests(Customwebapplicationfactory factory)
    {
        _factory = factory;
    }

    // =====================================================================
    // Test 5: a patient tries to toggle a doctor -> 403
    // =====================================================================
    [Fact]
    public async Task ToggleDoctor_AsPatient_Returns403()
    {
        var patient = await CreatePatientClientAsync();

        // The role check happens BEFORE the controller runs, so the doctor id does not matter.
        var response = await patient.PatchAsync("/api/Doctors/9999/toggle-active", null);

        await AssertStatusAsync(HttpStatusCode.Forbidden, response);
    }

    // =====================================================================
    // Test 6: a patient books an appointment -> 201
    // =====================================================================
    [Fact]
    public async Task CreateAppointment_AsPatient_Returns201WithPendingAppointment()
    {
        var doctorId = await CreateDoctorAsync();
        var patient = await CreatePatientClientAsync();

        var response = await patient.PostAsJsonAsync("/api/Appointments", new CreateAppointmentDto
        {
            DoctorId = doctorId,
            AppointmentDate = Tomorrow(10),
            Notes = "checkup"
        });

        await AssertStatusAsync(HttpStatusCode.Created, response);
        Assert.NotNull(response.Headers.Location);

        var appointment = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(appointment);
        Assert.Equal("Pending", appointment!.Status);
        Assert.Equal(doctorId, appointment.DoctorId);
        Assert.NotEqual(0, appointment.PatientId);   // the patient came from the token, not from the request
    }

    // =====================================================================
    // Test 7: the same doctor and time twice -> 400 (double-booking prevention)
    // =====================================================================
    [Fact]
    public async Task CreateAppointment_SameDoctorAndTimeTwice_SecondReturns400()
    {
        var doctorId = await CreateDoctorAsync();
        var patientA = await CreatePatientClientAsync();
        var patientB = await CreatePatientClientAsync();
        var request = new CreateAppointmentDto { DoctorId = doctorId, AppointmentDate = Tomorrow(10) };

        var first = await patientA.PostAsJsonAsync("/api/Appointments", request);
        await AssertStatusAsync(HttpStatusCode.Created, first);

        var second = await patientB.PostAsJsonAsync("/api/Appointments", request);
        await AssertStatusAsync(HttpStatusCode.BadRequest, second);

        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(400, problem!.Status);
    }

    // =====================================================================
    // Test 8: another patient reads my appointment -> 403
    // =====================================================================
    [Fact]
    public async Task GetAppointment_ByAnotherPatient_Returns403()
    {
        var doctorId = await CreateDoctorAsync();
        var patientA = await CreatePatientClientAsync();
        var patientB = await CreatePatientClientAsync();

        var create = await patientA.PostAsJsonAsync("/api/Appointments", new CreateAppointmentDto
        {
            DoctorId = doctorId,
            AppointmentDate = Tomorrow(10)
        });
        await AssertStatusAsync(HttpStatusCode.Created, create);
        var appointment = await create.Content.ReadFromJsonAsync<AppointmentDto>();

       var ownerRead = await patientA.GetAsync($"/api/Appointments/{appointment!.Id}");
        await AssertStatusAsync(HttpStatusCode.OK, ownerRead);

        var otherRead = await patientB.GetAsync($"/api/Appointments/{appointment.Id}");
        await AssertStatusAsync(HttpStatusCode.Forbidden, otherRead);
    }


    // Helpers


    private static DateTime Tomorrow(int hour) => DateTime.UtcNow.Date.AddDays(1).AddHours(hour);

    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.com";


    private static async Task AssertStatusAsync(HttpStatusCode expected, HttpResponseMessage response)
    {
        if (response.StatusCode != expected)
            Assert.Fail($"Expected {(int)expected} but got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }


    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/Auth/Login",
            new TokenRequestModelDto { Email = email, Password = password });
        await EnsureSuccessAsync(response);

        var auth = await response.Content.ReadFromJsonAsync<AuthModelDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
    }

    // A brand new patient account, already logged in, with its own HttpClient.
    private async Task<HttpClient> CreatePatientClientAsync()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail("patient");

        var response = await client.PostAsJsonAsync("/api/Auth/Register", new RegisterModelDto
        {
            Email = email,
            Username = "p" + Guid.NewGuid().ToString("N")[..12],
            Password = PatientPassword,
            DateOfBirth = new DateTime(2000, 1, 1)
        });
        await EnsureSuccessAsync(response);

        await LoginAsync(client, email, PatientPassword);
        return client;
    }

    // Creates a doctor through the admin endpoint and returns the new doctor's id.
    private async Task<int> CreateDoctorAsync()
    {
        var admin = _factory.CreateClient();
        await LoginAsync(admin, Customwebapplicationfactory.AdminEmail, Customwebapplicationfactory.AdminPassword);

        
        var before = (await admin.GetFromJsonAsync<List<DoctorDto>>("/api/Doctors"))!
            .Select(d => d.Id).ToHashSet();

        var email = UniqueEmail("doctor");
        var response = await admin.PostAsJsonAsync("/api/Auth/RegisterWithRole", new RegisterWithRoleDto
        {
            Email = email,
            Username = "d" + Guid.NewGuid().ToString("N")[..12],
            Password = DoctorPassword,
            DateOfBirth = new DateTime(1985, 1, 1),
            Role = "Doctor"
        });
        await EnsureSuccessAsync(response);

        var after = await admin.GetFromJsonAsync<List<DoctorDto>>("/api/Doctors");
        var doctor = after!.Single(d => !before.Contains(d.Id));

        // booking needs an active doctor
        if (!doctor.IsActive)
        {
            var toggle = await admin.PatchAsync($"/api/Doctors/{doctor.Id}/toggle-active", null);
            await EnsureSuccessAsync(toggle);
        }

        return doctor.Id;
    }
}
