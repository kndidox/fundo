using Fundo.Domain.Outbox;
using Fundo.Infrastructure.ExternalService;
using Fundo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fundo.Infrastructure.Outbox;

/// <summary>
/// Polls the outbox table and delivers pending events to the external service over HTTP —
/// this is the "background event" step, kept out of the request that saves the application.
/// Runs in its own DI scope per poll since <see cref="FundoDbContext"/> is scoped.
/// </summary>
public class OutboxDispatcherService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboxDispatcherService> _logger;

    public OutboxDispatcherService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<OutboxDispatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unexpected error while dispatching outbox events");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
        }
    }

    private async Task DispatchPendingEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FundoDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<IExternalLoanClient>();

        var now = _timeProvider.GetUtcNow();
        var pendingEvents = await db.OutboxEvents
            .Where(e => e.Status == OutboxEventStatus.Pending && e.NextAttemptAt <= now)
            .OrderBy(e => e.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var outboxEvent in pendingEvents)
        {
            await DispatchAsync(outboxEvent, client, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task DispatchAsync(OutboxEvent outboxEvent, IExternalLoanClient client, CancellationToken cancellationToken)
    {
        try
        {
            if (outboxEvent.EventType == OutboxEventType.CustomerApplicationCreated)
            {
                await client.CreateAsync(outboxEvent.PayloadJson, cancellationToken);
            }
            else
            {
                await client.UpdateAsync(outboxEvent.Ssn, outboxEvent.PayloadJson, cancellationToken);
            }

            outboxEvent.MarkSent(_timeProvider.GetUtcNow());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Failed to dispatch outbox event {EventId} (attempt {Attempt})",
                outboxEvent.Id,
                outboxEvent.Attempts + 1);
            outboxEvent.MarkAttemptFailed(ex.Message, _timeProvider.GetUtcNow());
        }
    }
}
