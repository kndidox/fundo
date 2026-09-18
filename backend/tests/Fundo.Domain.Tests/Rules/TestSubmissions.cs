using Fundo.Domain.Rules;

namespace Fundo.Domain.Tests.Rules;

internal static class TestSubmissions
{
    public static ApplicationSubmission Valid(string state = "CA", string ssn = "111223333", decimal amount = 5000m) =>
        new(
            FirstName: "Jane",
            LastName: "Doe",
            AddressLine1: "123 Main St",
            AddressLine2: null,
            City: "Springfield",
            State: state,
            ZipCode: "12345",
            CompanyName: "Acme Inc",
            Ssn: ssn,
            RequestedAmount: amount);
}
