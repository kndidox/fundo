namespace Fundo.Domain.Outbox;

public enum OutboxEventStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3
}
