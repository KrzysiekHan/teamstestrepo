namespace TeamstestRepo.Api.Models;

public sealed class ApprovalCreatedResponse
{
    public required string ProcessId { get; init; }
    public required string Message { get; init; }
}
