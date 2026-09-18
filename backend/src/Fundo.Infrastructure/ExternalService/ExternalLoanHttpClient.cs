using System.Text;

namespace Fundo.Infrastructure.ExternalService;

public class ExternalLoanHttpClient : IExternalLoanClient
{
    private readonly HttpClient _httpClient;

    public ExternalLoanHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task CreateAsync(string payloadJson, CancellationToken cancellationToken)
    {
        using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync("api/loan-applications", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateAsync(string ssn, string payloadJson, CancellationToken cancellationToken)
    {
        using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PutAsync($"api/loan-applications/{Uri.EscapeDataString(ssn)}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
