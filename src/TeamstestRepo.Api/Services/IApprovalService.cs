using TeamstestRepo.Api.Models;

namespace TeamstestRepo.Api.Services;

public interface IApprovalService
{
    /// <summary>
    /// Inicjuje proces zatwierdzania: waliduje żądanie i wysyła kartę do Teams.
    /// </summary>
    Task InitiateApprovalAsync(ApprovalRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Przetwarza decyzję pracownika i wywołuje callbackUrl zewnętrznego systemu.
    /// </summary>
    Task ProcessDecisionAsync(AdaptiveCardPayload payload, string decidedByUserId, CancellationToken cancellationToken = default);
}
