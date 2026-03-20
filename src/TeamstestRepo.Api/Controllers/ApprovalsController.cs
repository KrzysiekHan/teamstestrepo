using Microsoft.AspNetCore.Mvc;
using TeamstestRepo.Api.Models;
using TeamstestRepo.Api.Services;

namespace TeamstestRepo.Api.Controllers;

[ApiController]
[Route("approvals")]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly ILogger<ApprovalsController> _logger;

    public ApprovalsController(IApprovalService approvalService, ILogger<ApprovalsController> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }

    /// <summary>
    /// Przyjmuje żądanie zatwierdzenia z zewnętrznego systemu i wysyła formularz do pracownika w Teams.
    /// </summary>
    /// <remarks>Wymaga nagłówka X-Api-Key.</remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApprovalCreatedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateApproval(
        [FromBody] ApprovalRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Approval request received for process {ProcessId}", request.ProcessId);

        await _approvalService.InitiateApprovalAsync(request, cancellationToken);

        return Accepted(new ApprovalCreatedResponse
        {
            ProcessId = request.ProcessId,
            Message = $"Approval card sent to user {request.UserId}."
        });
    }
}
