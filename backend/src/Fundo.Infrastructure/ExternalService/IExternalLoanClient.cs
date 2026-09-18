namespace Fundo.Infrastructure.ExternalService;

/// <summary>Sends an already-serialized outbox payload to the external loan service.</summary>
public interface IExternalLoanClient
{
    Task CreateAsync(string payloadJson, CancellationToken cancellationToken);

    Task UpdateAsync(string ssn, string payloadJson, CancellationToken cancellationToken);
}
