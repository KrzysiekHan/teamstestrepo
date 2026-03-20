namespace TeamstestRepo.Api.Models;

/// <summary>
/// Decyzja wysyłana do zewnętrznego systemu po akcji pracownika.
/// </summary>
public sealed class ApprovalDecision
{
    public required string ProcessId { get; init; }
    public required DecisionType Decision { get; init; }
    public required string DecidedBy { get; init; }
    public required DateTimeOffset DecidedAt { get; init; }
    public required string Field1 { get; init; }
    public required string Field2 { get; init; }
}

public enum DecisionType
{
    Approve,
    Reject
}
