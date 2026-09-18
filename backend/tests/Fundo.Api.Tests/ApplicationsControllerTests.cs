using System.Net.Http.Json;
using Fundo.Api.Contracts;
using Xunit;

namespace Fundo.Api.Tests;

public class ApplicationsControllerTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public ApplicationsControllerTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Submit_ValidApplication_IsApproved()
    {
        var request = ValidRequest(ssn: "111-11-1111", state: "CA");

        var response = await _client.PostAsJsonAsync("/api/applications", request);
        var body = await response.Content.ReadFromJsonAsync<SubmitApplicationResponse>();

        response.EnsureSuccessStatusCode();
        Assert.Equal("Approved", body!.Status);
        Assert.NotNull(body.ApplicationId);
        Assert.NotNull(body.CustomerId);
    }

    [Fact]
    public async Task Submit_StateIsNy_IsDenied()
    {
        var request = ValidRequest(ssn: "222-22-2222", state: "NY");

        var response = await _client.PostAsJsonAsync("/api/applications", request);
        var body = await response.Content.ReadFromJsonAsync<SubmitApplicationResponse>();

        response.EnsureSuccessStatusCode();
        Assert.Equal("Denied", body!.Status);
        Assert.Equal("StateNotAllowed", body.Reason);
        Assert.Null(body.ApplicationId);
    }

    [Fact]
    public async Task Submit_BlacklistedSsn_IsDenied()
    {
        // Matches the sample entry seeded in appsettings.json's RuleEngine:BlacklistedSsns.
        var request = ValidRequest(ssn: "123-45-6789", state: "CA");

        var response = await _client.PostAsJsonAsync("/api/applications", request);
        var body = await response.Content.ReadFromJsonAsync<SubmitApplicationResponse>();

        response.EnsureSuccessStatusCode();
        Assert.Equal("Denied", body!.Status);
        Assert.Equal("SsnBlacklisted", body.Reason);
    }

    [Fact]
    public async Task Submit_SameSsnTwice_UpdatesTheSameCustomerAndApplication()
    {
        var ssn = "333-33-3333";

        var firstResponse = await _client.PostAsJsonAsync("/api/applications", ValidRequest(ssn: ssn, amount: 1000m));
        var first = await firstResponse.Content.ReadFromJsonAsync<SubmitApplicationResponse>();

        var secondResponse = await _client.PostAsJsonAsync("/api/applications", ValidRequest(ssn: ssn, amount: 9000m));
        var second = await secondResponse.Content.ReadFromJsonAsync<SubmitApplicationResponse>();

        Assert.Equal("Approved", first!.Status);
        Assert.Equal("Approved", second!.Status);
        Assert.Equal(first.CustomerId, second.CustomerId);
        Assert.Equal(first.ApplicationId, second.ApplicationId);
    }

    private static SubmitApplicationRequest ValidRequest(string ssn, string state = "CA", decimal amount = 5000m) => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        AddressLine1 = "123 Main St",
        City = "Springfield",
        State = state,
        ZipCode = "12345",
        CompanyName = "Acme Inc",
        Ssn = ssn,
        RequestedAmount = amount
    };
}
