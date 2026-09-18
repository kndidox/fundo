using System.Text.Json;
using Fundo.Application.Outbox;
using Fundo.Application.Persistence;
using Fundo.Domain.Entities;
using Fundo.Domain.Outbox;
using Fundo.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Application.Applications;

/// <summary>
/// Orchestrates one form submission: runs it through the rule engine, then — only when
/// approved — upserts the Customer and LoanApplication and records the outbox event, all in
/// one <see cref="IFundoDbContext.SaveChangesAsync"/> call so the three writes succeed or fail together.
/// </summary>
public class SubmitApplicationUseCase
{
    private readonly RuleEngine _ruleEngine;
    private readonly IFundoDbContext _db;
    private readonly TimeProvider _timeProvider;

    public SubmitApplicationUseCase(RuleEngine ruleEngine, IFundoDbContext db, TimeProvider timeProvider)
    {
        _ruleEngine = ruleEngine;
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<SubmitApplicationResult> ExecuteAsync(ApplicationSubmission submission, CancellationToken cancellationToken)
    {
        var ruleResult = _ruleEngine.Evaluate(submission);
        if (ruleResult.IsDenied)
        {
            return SubmitApplicationResult.Denied(ruleResult.Reason!);
        }

        var now = _timeProvider.GetUtcNow();

        var existingCustomer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Ssn == submission.Ssn, cancellationToken);
        var isReturningCustomer = existingCustomer is not null;

        var customer = existingCustomer;
        if (customer is null)
        {
            customer = Customer.Create(
                submission.FirstName,
                submission.LastName,
                submission.AddressLine1,
                submission.AddressLine2,
                submission.City,
                submission.State,
                submission.ZipCode,
                submission.CompanyName,
                submission.Ssn,
                now);
            _db.Customers.Add(customer);
        }
        else
        {
            customer.UpdateDetails(
                submission.FirstName,
                submission.LastName,
                submission.AddressLine1,
                submission.AddressLine2,
                submission.City,
                submission.State,
                submission.ZipCode,
                submission.CompanyName,
                now);
        }

        var existingApplication = await _db.LoanApplications
            .FirstOrDefaultAsync(a => a.CustomerId == customer.Id, cancellationToken);

        var application = existingApplication;
        if (application is null)
        {
            application = LoanApplication.Create(customer.Id, submission.RequestedAmount, now);
            _db.LoanApplications.Add(application);
        }
        else
        {
            application.UpdateRequestedAmount(submission.RequestedAmount, now);
        }

        var payload = new ExternalLoanApplicationPayload(
            customer.Id,
            application.Id,
            customer.FirstName,
            customer.LastName,
            customer.AddressLine1,
            customer.AddressLine2,
            customer.City,
            customer.State,
            customer.ZipCode,
            customer.CompanyName,
            customer.Ssn,
            application.RequestedAmount);

        var eventType = isReturningCustomer
            ? OutboxEventType.CustomerApplicationUpdated
            : OutboxEventType.CustomerApplicationCreated;

        var outboxEvent = OutboxEvent.Create(eventType, customer.Ssn, JsonSerializer.Serialize(payload), now);
        _db.OutboxEvents.Add(outboxEvent);

        // Single SaveChanges call = single DB transaction: customer, application and the
        // outbox event are written atomically, or none of them are if this throws.
        await _db.SaveChangesAsync(cancellationToken);

        return SubmitApplicationResult.Approved(customer.Id, application.Id);
    }
}
