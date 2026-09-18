namespace Fundo.Domain.Outbox;

/// <summary>
/// Recorded in the same DB transaction as the Customer/LoanApplication write so the
/// "publish to the external service" step can never be lost or duplicated relative to
/// the persisted data — a background dispatcher delivers it later (transactional outbox pattern).
/// </summary>
public class OutboxEvent
{
    private const int MaxAttempts = 5;

    public Guid Id { get; private set; }
    public OutboxEventType EventType { get; private set; }
    public string Ssn { get; private set; } = default!;
    public string PayloadJson { get; private set; } = default!;
    public OutboxEventStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    private OutboxEvent() { }

    public static OutboxEvent Create(OutboxEventType eventType, string ssn, string payloadJson, DateTimeOffset now)
    {
        return new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Ssn = ssn,
            PayloadJson = payloadJson,
            Status = OutboxEventStatus.Pending,
            Attempts = 0,
            NextAttemptAt = now,
            CreatedAt = now
        };
    }

    public void MarkSent(DateTimeOffset now)
    {
        Status = OutboxEventStatus.Sent;
        ProcessedAt = now;
        LastError = null;
    }

    public void MarkAttemptFailed(string error, DateTimeOffset now)
    {
        Attempts++;
        LastError = error;

        if (Attempts >= MaxAttempts)
        {
            Status = OutboxEventStatus.Failed;
            ProcessedAt = now;
            return;
        }

        var backoffSeconds = Math.Pow(2, Attempts);
        NextAttemptAt = now.AddSeconds(backoffSeconds);
    }
}
