using Fundo.Api.Contracts;
using Fundo.Application.Applications;
using Fundo.Domain.Rules;
using Microsoft.AspNetCore.Mvc;

namespace Fundo.Api.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly SubmitApplicationUseCase _useCase;

    public ApplicationsController(SubmitApplicationUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost]
    public async Task<ActionResult<SubmitApplicationResponse>> Submit(
        SubmitApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var submission = new ApplicationSubmission(
            request.FirstName,
            request.LastName,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.State.ToUpperInvariant(),
            request.ZipCode,
            request.CompanyName,
            NormalizeSsn(request.Ssn),
            request.RequestedAmount);

        var result = await _useCase.ExecuteAsync(submission, cancellationToken);

        return Ok(new SubmitApplicationResponse
        {
            Status = result.IsApproved ? "Approved" : "Denied",
            Reason = result.Reason,
            ApplicationId = result.ApplicationId,
            CustomerId = result.CustomerId
        });
    }

    private static string NormalizeSsn(string ssn) => new([.. ssn.Where(char.IsDigit)]);
}
