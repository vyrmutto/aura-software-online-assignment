using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicPOS.Tests;

public class IntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public IntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TenantScopingEnforced_PatientInTenantA_NotVisibleFromTenantB()
    {
        var (tenantAId, tenantBId, adminTokenA, _, adminTokenB) = await _factory.SeedTestDataAsync();

        // Create patient in Tenant A
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminTokenA);

        var createResponse = await _client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "TenantA",
            lastName = "Patient",
            phoneNumber = "0999999999"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Verify patient is visible from Tenant A
        var listResponseA = await _client.GetAsync($"/api/patients?tenantId={tenantAId}");
        Assert.Equal(HttpStatusCode.OK, listResponseA.StatusCode);
        var bodyA = await listResponseA.Content.ReadAsStringAsync();
        Assert.Contains("TenantA", bodyA);

        // Switch to Tenant B admin
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminTokenB);

        // Query patients from Tenant B context
        var listResponseB = await _client.GetAsync($"/api/patients?tenantId={tenantBId}");
        Assert.Equal(HttpStatusCode.OK, listResponseB.StatusCode);
        var bodyB = await listResponseB.Content.ReadAsStringAsync();

        // Patient from Tenant A should NOT be visible
        Assert.DoesNotContain("TenantA", bodyB);
    }

    [Fact]
    public async Task TenantIdMismatch_Returns403()
    {
        // Arrange - login as Tenant A but query with Tenant B's tenantId
        var (_, tenantBId, adminTokenA, _, _) = await _factory.SeedTestDataAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminTokenA);

        // Act - try to access Tenant B's patients with Tenant A's token
        var response = await _client.GetAsync($"/api/patients?tenantId={tenantBId}");

        // Assert - should be forbidden due to tenantId mismatch
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DuplicatePhonePrevented_Returns409()
    {
        var (_, _, adminTokenA, _, _) = await _factory.SeedTestDataAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminTokenA);

        var patient = new
        {
            firstName = "Dup",
            lastName = "Phone",
            phoneNumber = "0888888888"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/patients", patient);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await _client.PostAsJsonAsync("/api/patients", patient);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        var body = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("DuplicatePhoneNumber", body);
    }

    [Fact]
    public async Task ViewerCannotCreatePatient_Returns403()
    {
        var (_, _, _, viewerTokenA, _) = await _factory.SeedTestDataAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", viewerTokenA);

        var response = await _client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Should",
            lastName = "Fail",
            phoneNumber = "0777777777"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateAppointmentPrevented_Returns409()
    {
        var (tenantAId, _, adminTokenA, _, _) = await _factory.SeedTestDataAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminTokenA);

        // Create a patient
        var patientResponse = await _client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Appt",
            lastName = "Test",
            phoneNumber = "0666666666"
        });
        Assert.Equal(HttpStatusCode.Created, patientResponse.StatusCode);
        var patientBody = await patientResponse.Content.ReadAsStringAsync();
        var patientDoc = JsonDocument.Parse(patientBody);
        var patientId = patientDoc.RootElement.GetProperty("id").GetString();

        // Get a branch
        var branchesResponse = await _client.GetAsync("/api/branches");
        var branchesBody = await branchesResponse.Content.ReadAsStringAsync();
        var branchesDoc = JsonDocument.Parse(branchesBody);
        var branchId = branchesDoc.RootElement[0].GetProperty("id").GetString();

        var startAt = DateTime.UtcNow.AddDays(7).ToString("o");

        var appointment = new
        {
            branchId,
            patientId,
            startAt
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/appointments", appointment);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await _client.PostAsJsonAsync("/api/appointments", appointment);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        var body = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("DuplicateAppointment", body);
    }
}
