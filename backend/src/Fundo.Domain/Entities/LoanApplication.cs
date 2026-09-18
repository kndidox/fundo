namespace Fundo.Domain.Entities;

public class LoanApplication
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal RequestedAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private LoanApplication() { }

    public static LoanApplication Create(Guid customerId, decimal requestedAmount, DateTimeOffset now)
    {
        return new LoanApplication
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            RequestedAmount = requestedAmount,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateRequestedAmount(decimal requestedAmount, DateTimeOffset now)
    {
        RequestedAmount = requestedAmount;
        UpdatedAt = now;
    }
}
