namespace Fundo.Application.Applications;

public sealed record SubmitApplicationResult
{
    public bool IsApproved { get; }
    public string? Reason { get; }
    public Guid? CustomerId { get; }
    public Guid? ApplicationId { get; }

    private SubmitApplicationResult(bool isApproved, string? reason, Guid? customerId, Guid? applicationId)
    {
        IsApproved = isApproved;
        Reason = reason;
        CustomerId = customerId;
        ApplicationId = applicationId;
    }

    public static SubmitApplicationResult Approved(Guid customerId, Guid applicationId) =>
        new(true, null, customerId, applicationId);

    public static SubmitApplicationResult Denied(string reason) =>
        new(false, reason, null, null);
}
