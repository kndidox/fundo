namespace Fundo.Domain.Entities;

public class Customer
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string AddressLine1 { get; private set; } = default!;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = default!;
    public string State { get; private set; } = default!;
    public string ZipCode { get; private set; } = default!;
    public string CompanyName { get; private set; } = default!;
    public string Ssn { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Customer() { }

    public static Customer Create(
        string firstName,
        string lastName,
        string addressLine1,
        string? addressLine2,
        string city,
        string state,
        string zipCode,
        string companyName,
        string ssn,
        DateTimeOffset now)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            AddressLine1 = addressLine1,
            AddressLine2 = addressLine2,
            City = city,
            State = state,
            ZipCode = zipCode,
            CompanyName = companyName,
            Ssn = ssn,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDetails(
        string firstName,
        string lastName,
        string addressLine1,
        string? addressLine2,
        string city,
        string state,
        string zipCode,
        string companyName,
        DateTimeOffset now)
    {
        FirstName = firstName;
        LastName = lastName;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        State = state;
        ZipCode = zipCode;
        CompanyName = companyName;
        UpdatedAt = now;
    }
}
